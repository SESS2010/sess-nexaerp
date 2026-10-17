using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task InstallerCalendarChecksSupportEveryPredecessorAndRefuseHalfInstalledPair()
    {
        const string before="20261011100000_FifoRecoverableTaxCostBasis";
        using var model=new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator=model.GetService<IMigrator>();
        using var server=DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("calendar-installer-before.sql",migrator.GenerateScript("0",before));
        using var environment=new OrdinaryPrincipalEnvironment(server.ConnectionString,"disposable-calendar-"+Guid.NewGuid().ToString("N"));
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","status"]));
        var predecessor=before;
        foreach(var target in new[]{"20261017100000_KolkataCurrentBusinessDates","20261017110000_KolkataEventSnapshotDates","20261017120000_KolkataRecordedEventDates"})
        {
            server.Execute("calendar-installer-"+target+".sql",migrator.GenerateScript(predecessor,target));
            Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
            Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","status"]));
            predecessor=target;
        }
        server.Execute("calendar-installer-half-pair.sql","ALTER FUNCTION advance.business_event_date(timestamptz) RENAME TO business_event_date_drift;");
        Assert.Equal(1,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
        server.Execute("calendar-installer-half-pair-evidence.sql","""
            DO $assert$ BEGIN
              IF to_regprocedure('advance.business_event_date(timestamptz)') IS NOT NULL OR
                 to_regprocedure('advance.business_event_date_drift(timestamptz)') IS NULL OR
                 to_regprocedure('advance.business_calendar_cutover()') IS NULL THEN
                RAISE EXCEPTION 'Failed provision erased or silently repaired the half-pair drift evidence.';
              END IF;
            END $assert$;
            """);
    }
}