using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private const string MaterialIssueTarget = "20260907134726_MaterialIssueRequestAndCustodyIssue";

    [Fact]
    public void Material_issue_custody_applies_reverts_and_reapplies_on_disposable_postgresql()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        var migrations = model.Database.GetMigrations().ToArray();
        var index = Array.IndexOf(migrations, MaterialIssueTarget);
        Assert.True(index > 0);
        var predecessor = migrations[index - 1];

        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("material-issue-predecessor.sql", migrator.GenerateScript("0", predecessor));
        server.Execute("material-issue-up.sql", migrator.GenerateScript(predecessor, MaterialIssueTarget) + Assertions);
        server.Execute("material-issue-down.sql", migrator.GenerateScript(MaterialIssueTarget, predecessor));
        server.Execute("material-issue-reapply.sql", migrator.GenerateScript(predecessor, MaterialIssueTarget) + Assertions);
    }

    [Fact]
    public async Task Material_issue_changes_are_collected_as_ordinary_command_slots()
    {
        await using var db = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var request = new MaterialIssueRequest { Version = 4 };
        var issue = new MaterialIssue { MaterialIssueRequestId = request.Id, Version = 2 };
        db.MaterialIssueRequests.Add(request);
        db.MaterialIssues.Add(issue);
        db.MaterialIssueHistories.AddRange(
            new MaterialIssueHistory
            {
                MaterialIssueRequestId = request.Id, Action = "APPROVE", ToStatus = "APPROVED",
                CorrelationId = "mir-approve", Remarks = "approved"
            },
            new MaterialIssueHistory
            {
                MaterialIssueRequestId = request.Id, MaterialIssueId = issue.Id,
                Action = "ISSUE", ToStatus = "FULFILLED", CorrelationId = "mir-issue", Remarks = "issued"
            });

        var slots = await Rev869BCommandContextAuthorizer.CollectSlotsAsync(db, CancellationToken.None);

        Assert.Collection(slots.OrderBy(x => x.EntityType),
            slot =>
            {
                Assert.Equal("MaterialIssue", slot.EntityType);
                Assert.Equal(issue.Id, slot.EntityId);
                Assert.Equal(2, slot.ParentVersion);
            },
            slot =>
            {
                Assert.Equal("MaterialIssueRequest", slot.EntityType);
                Assert.Equal(request.Id, slot.EntityId);
                Assert.Equal(4, slot.ParentVersion);
            });
        Assert.All(slots, slot => Assert.Equal("material_issue_history", slot.ClaimKind));
    }

    private const string Assertions = """

        DO $assert$
        BEGIN
          IF to_regclass('advance.material_issues') IS NULL
             OR to_regclass('advance.material_issue_lines') IS NULL
             OR to_regclass('advance.material_issue_excess_decisions') IS NULL
             OR to_regclass('advance.material_issue_history') IS NULL
            THEN RAISE EXCEPTION 'Material Issue tables are missing.'; END IF;
          IF to_regprocedure('advance.post_material_issue_custody(uuid,uuid,text,text,text,uuid,text)') IS NULL
            THEN RAISE EXCEPTION 'Controlled custody posting function is missing.'; END IF;
          IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='trg_material_issue_guard')
             OR NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='trg_material_issue_batch_reconcile')
             OR NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='trg_material_issue_movement_reconcile')
            THEN RAISE EXCEPTION 'Material Issue database guards are missing.'; END IF;
          IF (SELECT count(*) FROM advance.page_definitions WHERE "PageKey" IN
               ('stores.material-issue-requests','stores.material-issues','stores.material-issue-excess'))<>3
            THEN RAISE EXCEPTION 'Expected three Material Issue pages.'; END IF;
          IF (SELECT count(*) FROM advance.role_page_permissions p
                JOIN advance.page_definitions d ON d."Id"=p."PageDefinitionId"
                WHERE d."PageKey" IN
                  ('stores.material-issue-requests','stores.material-issues','stores.material-issue-excess'))
             <> (SELECT count(*)+4 FROM advance.roles WHERE "IsActive" AND "IsEmployeeAssignable")
            THEN RAISE EXCEPTION 'Material Issue role-page grant count is wrong.'; END IF;
        END $assert$;
        """;
}
