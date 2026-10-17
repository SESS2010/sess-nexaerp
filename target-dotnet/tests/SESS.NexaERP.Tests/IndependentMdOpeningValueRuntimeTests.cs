using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Stores;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Theory]
    [InlineData("SESS_PVT_LTD")]
    [InlineData("SESS_PROPRIETORSHIP")]
    public async Task NamedMdIndependentlyReviewsKarthickOpeningCountWithoutOtherAuthority(string organization)
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        var migrations = model.Database.GetMigrations().ToArray();
        var opening = Array.IndexOf(migrations, "20260912064200_GovernedOpeningStockThreeActorCeremony");
        Assert.True(opening > 0);
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("md-review-predecessor.sql", migrator.GenerateScript("0", migrations[opening - 1]));
        server.Execute("md-review-trial.sql", "\\set expected_database advance_parser\n"
            + File.ReadAllText(Find("database", "postgresql", "trial-master-data-apply.sql")));
        server.Execute("md-review-active-items.sql", """
            UPDATE advance.items SET "Status"='Active',"ApprovalStatus"='Approved',"IsActive"=true
            WHERE "CreatedBy"='TRIAL_DATA';
            """);
        using var environment = new OrdinaryPrincipalEnvironment(server.ConnectionString,
            "disposable-md-review-" + Guid.NewGuid().ToString("N"));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        server.Execute("md-review-current-schema.sql", migrator.GenerateScript(migrations[opening - 1], migrations[^1]));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
        var options = new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(server.ConnectionString).Options;
        Guid company;
        Guid otherCompany;
        var employees = new Dictionary<string, Guid>();
        var otherOrganization = organization == "SESS_PVT_LTD" ? "SESS_PROPRIETORSHIP" : "SESS_PVT_LTD";
        await using (var seed = new NexaErpDbContext(options))
        {
            company = await seed.Companies.Where(c => c.Code == organization).Select(c => c.Id).SingleAsync();
            otherCompany = await seed.Companies.Where(c => c.Code == otherOrganization).Select(c => c.Id).SingleAsync();
            foreach (var code in new[] { "SESS-41", "SESS-14", "SESS-01", "SESS-02" })
            {
                var employee = await seed.Employees.SingleAsync(e => e.EmployeeCode == code);
                employee.LoginEnabled = true;
                employees.Add(code, employee.Id);
                var mapping = Mapping(company, employee.Id, code);
                mapping.OrganizationId = organization;
                seed.EmployeeIdentityMappings.Add(mapping);
            }
            var otherMapping = Mapping(otherCompany, employees["SESS-02"], "SESS-02");
            otherMapping.OrganizationId = otherOrganization;
            seed.EmployeeIdentityMappings.Add(otherMapping);
            await seed.SaveChangesAsync();
        }
        async Task<Dictionary<string, EffectiveRoleAssignment>> Assignments(Guid selectedCompany)
        {
            await using var db = new NexaErpDbContext(options);
            var today = db.BusinessDate;
            return (await db.EmployeeRoleAssignments.AsNoTracking().Include(a => a.Role)
                    .Where(a => a.CompanyId == selectedCompany && a.EffectiveFrom <= today
                        && (a.EffectiveTo == null || a.EffectiveTo >= today)
                        && (a.AssignmentType == "FULL" || a.AssignmentType == "TEMPORARY"))
                    .ToListAsync())
                .ToDictionary(a => TaxWorkflowUser.AssignmentKey(a.EmployeeId, a.Role!.Code),
                    a => new EffectiveRoleAssignment(a.Id, a.Role!.Code, a.AssignmentType));
        }
        var assignments = await Assignments(company);
        TaxWorkflowUser Actor(string code, string role)
        {
            var user = new TaxWorkflowUser(employees[code], code, role, assignments);
            user.SetOrganization(organization);
            Assert.All(user.EffectiveRoleAssignments, a => Assert.NotEqual(Guid.Empty, a.AssignmentId));
            return user;
        }
        var runtime = new NpgsqlConnectionStringBuilder(server.ConnectionString)
        { Username = "nexa_erp_runtime", Pooling = false }.ConnectionString;
        var stage = WitnessSql[..WitnessSql.IndexOf("DO $count$", StringComparison.Ordinal)]
            .Replace("https://opening.test", "https://issuer.purchase-flow.test", StringComparison.Ordinal)
            .Replace("SESS_PVT_LTD", organization, StringComparison.Ordinal)
            .Replace("CURRENT_DATE", "advance.current_business_date()", StringComparison.Ordinal)
            + "\nRESET SESSION AUTHORIZATION;";
        server.Execute("md-review-import.sql", stage);
        await using var stores = await PurchaseFlowHost.StartAsync(runtime, Actor("SESS-41", "STORES_MANAGER"), true, true);
        await using var accounts = await PurchaseFlowHost.StartAsync(runtime, Actor("SESS-14", "ACCOUNTS_MANAGER"), true, true);
        await using var md = await PurchaseFlowHost.StartAsync(runtime, Actor("SESS-02", "MANAGING_DIRECTOR"), true, true);
        await using var td = await PurchaseFlowHost.StartAsync(runtime, Actor("SESS-01", "TECHNICAL_DIRECTOR"), true, true);
        var counted = await Post<OpeningStockView>(stores.Client, "/api/v1/stores/opening-stock/from-import",
            new CreateOpeningStockFromImportRequest(Guid.Parse("f1300000-0000-0000-0000-000000000001"),
                new DateOnly(2026, 4, 1), new DateOnly(2027, 3, 31), "Signed independent count sheet", "md-review-count"));
        Assert.Equal("COUNTED", counted.Status);
        Assert.Equal(employees["SESS-41"], counted.CountedBy.EmployeeId);
        var path = $"/api/v1/stores/opening-stock/{counted.Id}/confirm-value";
        var body = new OpeningStockTransitionRequest(counted.Version,
            "ALFATHIMA unavailable; signed ex-tax valuation checked independently", "md-review-value", true);
        async Task<long[]> Snapshot()
        {
            await using var db = new NexaErpDbContext(options);
            // Fresh ordinary deployments deliberately omit the retired optional Rev869B package.
            // Assert its absence on every before/after snapshot; never recreate it just for this test.
            Assert.True(await db.Database.SqlQueryRaw<bool>(
                "SELECT to_regclass('advance.rev869b_command_requests') IS NULL AS \"Value\"").SingleAsync(),
                "The fresh ordinary fixture must not acquire a retired legacy ledger.");
            return [await db.OpeningStocks.CountAsync(), await db.OpeningStockEvents.CountAsync(),
                await db.StockMovements.CountAsync(), await db.FifoInventoryCostLayers.CountAsync(),
                await db.Database.SqlQueryRaw<long>("SELECT count(*) AS \"Value\" FROM advance.command_requests").SingleAsync(),
                await db.Database.SqlQueryRaw<long>("SELECT count(*) AS \"Value\" FROM advance.command_receipts").SingleAsync()];
        }
        async Task Refused(HttpClient client, OpeningStockTransitionRequest request, HttpStatusCode expected)
        {
            var before = await Snapshot();
            using var response = await client.PostAsJsonAsync(path, request);
            Assert.Equal(expected, response.StatusCode);
            Assert.Equal(before, await Snapshot());
        }
        await Refused(accounts.Client, body with { IdempotencyKey = "md-review-wrong-role" }, HttpStatusCode.Forbidden);
        await Refused(td.Client, body with { IdempotencyKey = "md-review-final-actor-cannot-value" }, HttpStatusCode.Forbidden);
        await Refused(md.Client, body with { Version = counted.Version + 1, IdempotencyKey = "md-review-stale" }, HttpStatusCode.Conflict);
        await Refused(md.Client, body with { Reason = " ", IdempotencyKey = "md-review-empty-reason" }, HttpStatusCode.BadRequest);
        var otherMd = new TaxWorkflowUser(employees["SESS-02"], "SESS-02", "MANAGING_DIRECTOR", await Assignments(otherCompany));
        otherMd.SetOrganization(otherOrganization);
        await using (var other = await PurchaseFlowHost.StartAsync(runtime, otherMd, true, true))
            await Refused(other.Client, body with { IdempotencyKey = "md-review-other-company" }, HttpStatusCode.Forbidden);
        var supportAssignments = assignments.ToDictionary(p => p.Key, p => p.Value with { AssignmentType = "SUPPORT" });
        var supportMd = new TaxWorkflowUser(employees["SESS-02"], "SESS-02", "MANAGING_DIRECTOR", supportAssignments);
        supportMd.SetOrganization(organization);
        await using (var support = await PurchaseFlowHost.StartAsync(runtime, supportMd, true, true))
            await Refused(support.Client, body with { IdempotencyKey = "md-review-support" }, HttpStatusCode.Forbidden);
        var valued = await Post<OpeningStockView>(md.Client, path, body);
        Assert.Equal("VALUED", valued.Status);
        Assert.Equal(employees["SESS-02"], valued.ValuedBy!.EmployeeId);
        Assert.Equal("MANAGING_DIRECTOR", valued.ValuedBy.RoleCode);
        Assert.StartsWith("[Independent MD review: ALFATHIMA unavailable; KARTHICK involved] ", valued.ValuedBy.Reason);
        Assert.NotEqual(valued.CountedBy.EmployeeId, valued.ValuedBy.EmployeeId);
        var afterValue = await Snapshot();
        var replay = await Post<OpeningStockView>(md.Client, path, body);
        Assert.True(replay.Replayed);
        Assert.Equal(afterValue, await Snapshot());
        await Refused(md.Client, body with { Version = valued.Version, IdempotencyKey = "md-review-second-capacity" }, HttpStatusCode.Forbidden);
        using (var selfFinal = await md.Client.PostAsJsonAsync($"/api/v1/stores/opening-stock/{valued.Id}/authorize",
                   new OpeningStockTransitionRequest(valued.Version, "Must not authorize own review", "md-review-self-final")))
            Assert.Equal(HttpStatusCode.Forbidden, selfFinal.StatusCode);
        Assert.Equal(afterValue, await Snapshot());
        var posted = await Post<OpeningStockView>(td.Client, $"/api/v1/stores/opening-stock/{valued.Id}/authorize",
            new OpeningStockTransitionRequest(valued.Version, "Three independent actors checked", "md-review-final"));
        Assert.Equal("POSTED", posted.Status);
        Assert.Equal(employees["SESS-01"], posted.AuthorizedBy!.EmployeeId);
        Assert.Equal(3, new[] { posted.CountedBy.EmployeeId, posted.ValuedBy!.EmployeeId, posted.AuthorizedBy.EmployeeId }.Distinct().Count());
        await using (var db = new NexaErpDbContext(options))
        {
            var layer = await db.FifoInventoryCostLayers.SingleAsync(l => l.CompanyId == company);
            Assert.Equal(25m, layer.UnitCost);
            // Both prospective opening instants start at the Kolkata business-day boundary.
            var businessStart = BusinessCalendar.StartOfDate(posted.PeriodEnd);
            Assert.Equal(businessStart, layer.ReceivedAt);
            var custodyId = await db.StockMovements.Where(m => m.OpeningStockLineId == layer.OpeningStockLineId)
                .Select(m => m.CustodyAssignmentId).SingleAsync();
            Assert.NotNull(custodyId);
            Assert.Equal(businessStart, (await db.InventoryCustodyAssignments.SingleAsync(c => c.Id == custodyId)).EffectiveFrom);
            // A later receipt must follow opening, even in the UTC/IST gap, independent of wall clock.
            foreach (var offset in new[] { TimeSpan.Zero, TimeSpan.FromHours(3),
                         new TimeSpan(5, 29, 59), new TimeSpan(5, 30, 0), new TimeSpan(23, 59, 59) })
                Assert.True(layer.ReceivedAt < businessStart.Add(offset).AddTicks(10));
            var mapping = await db.EmployeeIdentityMappings.SingleAsync(m => m.CompanyId == company && m.EmployeeId == employees["SESS-02"] && m.IsActive);
            mapping.IsActive = false; mapping.EffectiveTo = db.BusinessDate; mapping.Version++;
            mapping.UpdatedAt = DateTimeOffset.UtcNow; mapping.UpdatedBy = "INDEPENDENT_MD_IDENTITY_REFUSAL_WITNESS";
            await db.SaveChangesAsync();
        }
        await Refused(md.Client, body, HttpStatusCode.Forbidden); // An old successful key cannot bypass an inactive identity.
        const string openingInstantMigration = "20261018030000_KolkataOpeningStockInstants";
        server.AssertRejected("opening-ist-retained-down.sql",
            migrator.GenerateScript(openingInstantMigration, migrations[Array.IndexOf(migrations, openingInstantMigration) - 1]),
            "refuses retained opening-stock date evidence");
        server.AssertRejected("md-review-retained-down.sql",
            migrator.GenerateScript("20261017150000_IndependentMdOpeningValueReview", "20261017140000_MirIndependentDecisionAuthority"),
            "refuses retained valuation evidence");
        await using (var retained = new NexaErpDbContext(options))
        {
            Assert.Equal("POSTED", (await retained.OpeningStocks.SingleAsync(o => o.Id == posted.Id)).Status);
            Assert.Contains("20261017150000_IndependentMdOpeningValueReview", await retained.Database.GetAppliedMigrationsAsync());
            Assert.Equal(3, await retained.OpeningStockEvents.CountAsync(e => e.OpeningStockId == posted.Id));
            Assert.Single(await retained.FifoInventoryCostLayers.Where(l => l.CompanyId == company).ToListAsync());
        }
    }

    [Theory]
    [InlineData("AUTHORIZE")]
    [InlineData("WITHDRAW")]
    public async Task IndependentReviewFlagCannotBeUsedForAnotherOpeningAction(string action)
    {
        await using var db = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var user = new TaxWorkflowUser(Guid.NewGuid(), "flag-refusal", "TECHNICAL_DIRECTOR");
        var service = new EfOpeningStockService(db, user, null!);
        var request = new OpeningStockTransitionRequest(0, "Independent review only", "flag-refusal", true);
        var error = await Assert.ThrowsAsync<StoresValidationException>(() => action == "AUTHORIZE"
            ? service.AuthorizeAsync(Guid.NewGuid(), request, default)
            : service.WithdrawAsync(Guid.NewGuid(), request, default));
        Assert.Contains("only to the value-review step", error.Message);
    }
}