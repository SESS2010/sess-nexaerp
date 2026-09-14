using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
#if CONCURRENCY_WITNESS
    [Fact]
    public async Task TwoOpeningCeremoniesForOneCompanyCannotBothIntroduceStock()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        var migrations = model.Database.GetMigrations().ToArray();
        var index = Array.IndexOf(migrations, "20260912064200_GovernedOpeningStockThreeActorCeremony");
        Assert.True(index > 0);
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("opening-race-log-settings.sql",
            "ALTER SYSTEM SET log_error_verbosity='verbose'; SELECT pg_reload_conf();");
        server.Execute("opening-race-predecessor.sql", migrator.GenerateScript("0", migrations[index - 1]));
        server.Execute("opening-race-trial.sql", "\\set expected_database advance_parser\n"
            + File.ReadAllText(Find("database", "postgresql", "trial-master-data-apply.sql")));
        server.Execute("opening-race-active-items.sql", """
            UPDATE advance.items SET "Status"='Active',"ApprovalStatus"='Approved',"IsActive"=true
            WHERE "CreatedBy"='TRIAL_DATA';
            """);
        using var environment = new OrdinaryPrincipalEnvironment(server.ConnectionString, "opening-race-runtime-123456789");
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        server.Execute("opening-race-current-schema.sql", migrator.GenerateScript(migrations[index - 1], migrations[^1]));
        var options = new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(server.ConnectionString).Options;
        var companyId = Guid.Parse("70000000-0000-0000-0000-000000000001");
        var employeeIds = new Dictionary<string, Guid>();
        await using (var seed = new NexaErpDbContext(options))
        {
            foreach (var code in new[] { "SESS-41", "SESS-14", "SESS-01" })
            {
                var employee = await seed.Employees.SingleAsync(row => row.EmployeeCode == code);
                employee.LoginEnabled = true;
                employeeIds.Add(code, employee.Id);
                seed.EmployeeIdentityMappings.Add(Mapping(companyId, employee.Id, code));
            }
            await seed.SaveChangesAsync();
        }
        var assignments = await Query(options, async db =>
            (await db.EmployeeRoleAssignments.AsNoTracking().Include(row => row.Role)
                .Where(row => row.CompanyId == companyId && row.EffectiveTo == null).ToListAsync())
            .ToDictionary(row => TaxWorkflowUser.AssignmentKey(row.EmployeeId, row.Role!.Code),
                row => new EffectiveRoleAssignment(row.Id, row.Role!.Code, row.AssignmentType)));
        TaxWorkflowUser Actor(string code, string role)
        {
            var actor = new TaxWorkflowUser(employeeIds[code], code, role, assignments);
            Assert.All(actor.EffectiveRoleAssignments, assignment =>
            {
                Assert.NotEqual(Guid.Empty, assignment.AssignmentId);
                Assert.Equal("FULL", assignment.AssignmentType);
            });
            return actor;
        }

        // Reuse only the existing controlled staging/import fixture, before COUNT.
        // Each ceremony gets a distinct completed import and fiscal period.
        var stage = WitnessSql[..WitnessSql.IndexOf("DO $count$", StringComparison.Ordinal)]
            .Replace("https://opening.test", "https://issuer.purchase-flow.test", StringComparison.Ordinal)
            + "\nRESET SESSION AUTHORIZATION;";
        server.Execute("opening-race-import-one.sql", stage);
        var secondStage = stage
            .Replace("f1300000-0000-0000-0000-000000000001", "f1300000-0000-0000-0000-000000000002", StringComparison.Ordinal)
            .Replace("f1310000-0000-0000-0000-000000000001", "f1310000-0000-0000-0000-000000000002", StringComparison.Ordinal)
            .Replace("f1320000-0000-0000-0000-000000000001", "f1320000-0000-0000-0000-000000000002", StringComparison.Ordinal)
            .Replace("OPEN-001", "OPEN-002", StringComparison.Ordinal)
            .Replace("OPEN-LOT-001", "OPEN-LOT-002", StringComparison.Ordinal)
            .Replace("'opening-import'", "'opening-import-second'", StringComparison.Ordinal)
            .Replace("repeat('11',32)", "repeat('51',32)", StringComparison.Ordinal)
            .Replace("repeat('12',32)", "repeat('52',32)", StringComparison.Ordinal)
            .Replace("repeat('13',32)", "repeat('53',32)", StringComparison.Ordinal);
        server.Execute("opening-race-import-two.sql", secondStage);

        string Connection(string name) => new NpgsqlConnectionStringBuilder(server.ConnectionString)
        { Username = "nexa_erp_runtime", ApplicationName = name, Pooling = false }.ConnectionString;
        await using var stores = await PurchaseFlowHost.StartAsync(Connection("opening-count"),
            Actor("SESS-41", "STORES_MANAGER"), true, true);
        await using var accounts = await PurchaseFlowHost.StartAsync(Connection("opening-value"),
            Actor("SESS-14", "ACCOUNTS_MANAGER"), true, true);
        var first = await Post<OpeningStockView>(stores.Client, "/api/v1/stores/opening-stock/from-import",
            new CreateOpeningStockFromImportRequest(Guid.Parse("f1300000-0000-0000-0000-000000000001"),
                new DateOnly(2026, 4, 1), new DateOnly(2027, 3, 31), "First physical count", "opening-race-count-one"));
        var second = await Post<OpeningStockView>(stores.Client, "/api/v1/stores/opening-stock/from-import",
            new CreateOpeningStockFromImportRequest(Guid.Parse("f1300000-0000-0000-0000-000000000002"),
                new DateOnly(2027, 4, 1), new DateOnly(2028, 3, 31), "Second physical count", "opening-race-count-two"));
        first = await Post<OpeningStockView>(accounts.Client, $"/api/v1/stores/opening-stock/{first.Id}/confirm-value",
            new OpeningStockTransitionRequest(first.Version, "First value confirmed", "opening-race-value-one"));
        second = await Post<OpeningStockView>(accounts.Client, $"/api/v1/stores/opening-stock/{second.Id}/confirm-value",
            new OpeningStockTransitionRequest(second.Version, "Second value confirmed", "opening-race-value-two"));
        Assert.Equal("VALUED", first.Status);
        Assert.Equal("VALUED", second.Status);
        Assert.False(await Query(options, db => db.StockMovements.AnyAsync(row => row.CompanyId == companyId)));

        // Two independent sessions of the actual sole authorizing role; do not grant
        // another employee Technical Director authority for the sake of the test.
        await using var firstHost = await PurchaseFlowHost.StartAsync(Connection("race-opening-first"),
            Actor("SESS-01", "TECHNICAL_DIRECTOR"), true, true);
        await using var secondHost = await PurchaseFlowHost.StartAsync(Connection("race-opening-second"),
            Actor("SESS-01", "TECHNICAL_DIRECTOR"), true, true);
        await using var observer = new NpgsqlConnection(server.ConnectionString);
        await observer.OpenAsync();
        var gateKey = $"OPENING:{companyId}";
        await using var gate = new NpgsqlCommand("SELECT pg_advisory_lock(hashtextextended(@key,0))", observer);
        gate.Parameters.AddWithValue("key", gateKey);
        await gate.ExecuteNonQueryAsync();
        var firstPath = $"/api/v1/stores/opening-stock/{first.Id}/authorize";
        var secondPath = $"/api/v1/stores/opening-stock/{second.Id}/authorize";
        var firstCommand = new OpeningStockTransitionRequest(first.Version, "First authorization", "opening-race-authorize-one");
        var secondCommand = new OpeningStockTransitionRequest(second.Version, "Second authorization", "opening-race-authorize-two");
        var observations = new List<object>();
        var logStart = server.ReadDiagnosticLog().Length;
        Task<RaceHttpResult>? firstTask = null;
        Task<RaceHttpResult>? secondTask = null;
        string? observationError = null;
        try
        {
            firstTask = TimedRacePost(firstHost.Client, firstPath, firstCommand);
            await ObserveBlockedBackend(observer, "race-opening-first", observer.ProcessID, "authorize_opening_stock", observations);
            secondTask = TimedRacePost(secondHost.Client, secondPath, secondCommand);
            await ObserveBlockedBackend(observer, "race-opening-second", observer.ProcessID, "authorize_opening_stock", observations);
        }
        catch (Exception error) { observationError = error.ToString(); }
        finally
        {
            await using var release = new NpgsqlCommand("SELECT pg_advisory_unlock(hashtextextended(@key,0))", observer);
            release.Parameters.AddWithValue("key", gateKey);
            await release.ExecuteNonQueryAsync();
            if (firstTask is not null) await firstTask;
            if (secondTask is not null) await secondTask;
        }
        var winner = firstTask is null ? null : await firstTask;
        var loser = secondTask is null ? null : await secondTask;
        var retry = await TimedRacePost(secondHost.Client, secondPath, secondCommand);
        var log = server.ReadDiagnosticLog()[logStart..];
        var states = await Query(options, async db => new
        {
            Ceremonies = await db.OpeningStocks.Where(row => row.CompanyId == companyId)
                .OrderBy(row => row.PeriodStart).Select(row => new { row.Id, row.Status, row.Version,
                    row.CountedByEmployeeId, row.ValuedByEmployeeId, row.AuthorizedByEmployeeId }).ToListAsync(),
            Events = await db.OpeningStockEvents.CountAsync(row => row.CompanyId == companyId),
            Batches = await db.StockPostingBatches.CountAsync(row => row.CompanyId == companyId),
            Movements = await db.StockMovements.CountAsync(row => row.CompanyId == companyId),
            Quantity = await db.StockMovements.Where(row => row.CompanyId == companyId).SumAsync(row => row.QuantityIn - row.QuantityOut),
            Layers = await db.FifoInventoryCostLayers.CountAsync(row => row.CompanyId == companyId),
            LayerQuantity = await db.FifoInventoryCostLayers.Where(row => row.CompanyId == companyId).SumAsync(row => row.QuantityReceived),
            LayerValue = await db.FifoInventoryCostLayers.Where(row => row.CompanyId == companyId).SumAsync(row => row.LayerValue)
        });
        var evidence = Path.Combine(FindRepositoryRoot(), "local-evidence", "item25");
        Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence, "opening-company-postgresql.log"), log);
        await File.WriteAllTextAsync(Path.Combine(evidence, "opening-company.json"), JsonSerializer.Serialize(
            new { Observations = observations, ObservationError = observationError, First = winner, Second = loser,
                Retry = retry, States = states }, new JsonSerializerOptions { WriteIndented = true }));
        Assert.True(observationError is null, observationError + $" First: {winner?.Body}; second: {loser?.Body}");
        Assert.DoesNotContain("40P01", log, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deadlock detected", log, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(winner);
        Assert.NotNull(loser);
        Assert.True(winner.Status == HttpStatusCode.OK, winner.Body);
        Assert.True(loser.Status == HttpStatusCode.Conflict, loser.Body);
        Assert.True(retry.Status == HttpStatusCode.Conflict, retry.Body);
        using var conflict = JsonDocument.Parse(loser.Body);
        Assert.Equal("CONCURRENCY_CONFLICT", conflict.RootElement.GetProperty("Code").GetString());
        using var retryConflict = JsonDocument.Parse(retry.Body);
        Assert.Equal("BUSINESS_RULE_CONFLICT", retryConflict.RootElement.GetProperty("Code").GetString());
        Assert.Contains("already has stock movements", retryConflict.RootElement.GetProperty("Detail").GetString());
        var posted = Assert.Single(states.Ceremonies, row => row.Status == "POSTED");
        Assert.Equal(first.Id, posted.Id);
        Assert.Equal(2u, posted.Version);
        Assert.Equal(employeeIds["SESS-41"], posted.CountedByEmployeeId);
        Assert.Equal(employeeIds["SESS-14"], posted.ValuedByEmployeeId);
        Assert.Equal(employeeIds["SESS-01"], posted.AuthorizedByEmployeeId);
        var refused = Assert.Single(states.Ceremonies, row => row.Status == "VALUED");
        Assert.Equal(second.Id, refused.Id);
        Assert.Equal(1u, refused.Version);
        Assert.Null(refused.AuthorizedByEmployeeId);
        Assert.Equal(5, states.Events);
        Assert.Equal(1, states.Batches);
        Assert.Equal(1, states.Movements);
        Assert.Equal(10m, states.Quantity);
        Assert.Equal(1, states.Layers);
        Assert.Equal(10m, states.LayerQuantity);
        Assert.Equal(250m, states.LayerValue);
        var replay = await Post<OpeningStockView>(firstHost.Client, firstPath, firstCommand);
        Assert.True(replay.Replayed);
        Assert.Equal(posted.Id, replay.Id);
    }
#endif
}
