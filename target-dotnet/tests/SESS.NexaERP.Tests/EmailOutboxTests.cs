using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Outbox;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Outbox;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

/// <summary>R1 email-lite: the outbox store (Claude). The sender/worker are the TD's and tested on his branch.</summary>
public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task The_email_outbox_queues_once_retries_to_dead_and_keeps_every_row()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("email-outbox.sql", model.GetService<IMigrator>().GenerateScript("0", model.Database.GetMigrations().Last()));
        await using var db = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(server.ConnectionString).Options);
        var pvt = await db.Companies.Where(c => c.Code == "SESS_PVT_LTD").Select(c => c.Id).SingleAsync();
        var prop = await db.Companies.Where(c => c.Code == "SESS_PROPRIETORSHIP").Select(c => c.Id).SingleAsync();
        var store = new EfEmailOutboxStore(db, new OutboxUser("SESS_PVT_LTD"));

        // Idempotent per company and key; the same key in the other company is a different e-mail.
        var po = Guid.NewGuid();
        var request = new EmailOutboxRequest(pvt, EmailEventTypes.PoIssued, "PurchaseOrder", po, $"PO_ISSUED:{po:N}:1", """{"revisionNumber":1}""", "test");
        var first = await store.EnqueueAsync(request, CancellationToken.None);
        Assert.Equal(first, await store.EnqueueAsync(request, CancellationToken.None));
        Assert.NotEqual(first, await store.EnqueueAsync(request with { CompanyId = prop }, CancellationToken.None));

        // PENDING_COMPOSE is leased by a claim, then composed to QUEUED; a second claim does not see it.
        var composing = Assert.Single(await store.ClaimAsync(EmailStatuses.PendingCompose, 10, CancellationToken.None), x => x.Id == first);
        Assert.Equal(EmailStatuses.PendingCompose, composing.Status);
        Assert.DoesNotContain(await store.ClaimAsync(EmailStatuses.PendingCompose, 10, CancellationToken.None), x => x.Id == first);
        await Assert.ThrowsAsync<StoresValidationException>(() => store.ComposeAsync(first, new([], [], "s", "<p>b</p>", "b"), CancellationToken.None));
        await store.ComposeAsync(first, new(["vendor@example.test"], ["buyer@example.test"], "PO PO-1", "<p>PO</p>", "PO"), CancellationToken.None);

        // QUEUED → SENDING → FAILED with backoff, and DEAD at the fifth attempt; the TD's retry brings it back.
        var sending = Assert.Single(await store.ClaimAsync(EmailStatuses.Queued, 10, CancellationToken.None));
        Assert.Equal((first, EmailStatuses.Sending), (sending.Id, sending.Status));
        await store.MarkFailedAsync(first, "SMTP 421", dead: false, CancellationToken.None);
        server.Execute("backoff.sql", $$"""
            DO $x$ BEGIN
              IF NOT EXISTS(SELECT 1 FROM advance.email_outbox WHERE "Id"='{{first}}' AND "Status"='FAILED' AND "Attempts"=1
                 AND "NextAttemptAt" BETWEEN clock_timestamp()+interval '50 seconds' AND clock_timestamp()+interval '70 seconds')
              THEN RAISE EXCEPTION 'First failure must wait about one minute.'; END IF;
            END $x$;
            """);
        for (var attempt = 2; attempt <= EmailStatuses.MaxAttempts; attempt++)
        {
            server.Execute($"due-{attempt}.sql", $"""UPDATE advance.email_outbox SET "NextAttemptAt"=NULL WHERE "Id"='{first}';""");
            Assert.Single(await store.ClaimAsync(EmailStatuses.Failed, 10, CancellationToken.None));
            await store.MarkFailedAsync(first, $"SMTP failure {attempt}", dead: false, CancellationToken.None);
        }
        var dead = Assert.Single((await store.ListAsync(EmailStatuses.Dead, 1, 50, CancellationToken.None)).Items);
        Assert.Equal((first, 5, "SMTP failure 5"), (dead.Id, dead.Attempts, dead.LastError));
        await store.RetryAsync(first, CancellationToken.None);
        Assert.Single(await store.ClaimAsync(EmailStatuses.Queued, 10, CancellationToken.None));
        await store.MarkSentAsync(first, "<provider-id>", CancellationToken.None);
        await Assert.ThrowsAsync<StoresConflictException>(() => store.MarkSentAsync(first, null, CancellationToken.None));

        // A composed digest queues at once; TEST mode blocks it; a vendor without e-mail is skipped.
        var digest = await store.EnqueueAsync(new(pvt, EmailEventTypes.DigestStores, null, null, "DIGEST_STORES:2026-10-15:x", "{}", "digest",
            new(["storesmanager@example.test"], [], "Stores digest", "<p>3 overdue</p>", "3 overdue")), CancellationToken.None);
        Assert.Single(await store.ClaimAsync(EmailStatuses.Queued, 10, CancellationToken.None), x => x.Id == digest);
        await store.MarkBlockedAsync(digest, "Address not in the allow-list", CancellationToken.None);
        var noVendorMail = await store.EnqueueAsync(request with { SourceEntityId = Guid.NewGuid(), IdempotencyKey = "PO_ISSUED:novendor:1" }, CancellationToken.None);
        await store.SkipAsync(noVendorMail, "Vendor has no e-mail address", CancellationToken.None);

        // The log is company-scoped and carries no bodies; nothing can be deleted.
        var log = await store.ListAsync(null, 1, 50, CancellationToken.None);
        Assert.Equal(3, log.Total);
        Assert.All(log.Items, x => Assert.Equal(pvt, x.CompanyId));
        Assert.Equal(1, (await new EfEmailOutboxStore(db, new OutboxUser("SESS_PROPRIETORSHIP")).ListAsync(null, 1, 50, CancellationToken.None)).Total);
        server.AssertRejected("email-delete.sql", "DELETE FROM advance.email_outbox;", "never deleted");
        server.AssertRejected("email-uncomposed-queued.sql",
            $"""INSERT INTO advance.email_outbox("CompanyId","EventType","IdempotencyKey","Status","CreatedBy") VALUES('{pvt}','TEST','bad','QUEUED','t');""",
            "CK_email_outbox_composed");

        // The admin.email page: TD views and updates, IT Manager views.
        server.Execute("email-page.sql", """
            DO $x$ BEGIN
              IF (SELECT string_agg(r."Code"||':'||p."CanView"||':'||p."CanUpdate", ',' ORDER BY r."Code") FROM advance.role_page_permissions p
                  JOIN advance.roles r ON r."Id"=p."RoleId" WHERE p."PageDefinitionId"=md5('admin.email')::uuid) <> 'IT_MANAGER:true:false,TECHNICAL_DIRECTOR:true:true'
              THEN RAISE EXCEPTION 'admin.email grants are wrong.'; END IF;
            END $x$;
            """);

        // Rolling back must not drop the e-mail log.
        var migrations = model.Database.GetMigrations().ToArray();
        var outbox = Array.IndexOf(migrations, "20260927100000_EmailOutbox");
        server.AssertRejected("email-down.sql", model.GetService<IMigrator>().GenerateScript(migrations[^1], migrations[outbox - 1]),
            "refuses the retained e-mail log");
    }

    private sealed class OutboxUser(string organization) : ICurrentUser
    {
        public string LoginId => "outbox-test";
        public string RoleCode => "TECHNICAL_DIRECTOR";
        public string? OrganizationId => organization;
        public bool IsAuthenticated => true;
        public IReadOnlyList<EffectiveRoleAssignment> EffectiveRoleAssignments => [];
        public void SetResolvedRoleAuthority(ResolvedRoleAuthority authority) { }
    }
}
