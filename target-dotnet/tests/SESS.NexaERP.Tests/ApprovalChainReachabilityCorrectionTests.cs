using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence.Migrations;

namespace SESS.NexaERP.Tests;

public sealed class ApprovalChainReachabilityCorrectionTests
{
    private const string MigrationId = "20260903075214_ApprovalChainReachabilityAndVisibilityCorrections";

    [Fact]
    public void MigrationIsPostgreSqlGuardedAndFailsClosedAroundCleanupAndPermissionDrift()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", MigrationId + ".cs");
        var sql = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations",
            "ApprovalChainReachabilityCorrectionsSql.cs");

        Assert.Equal(2, Count(migration, "PostgreSqlClusterGuard.Require(migrationBuilder);"));
        Assert.Contains("PR-2026-27-000011 is no longer the reported SESS-01 draft orphan", sql);
        Assert.Contains("has progressed or accumulated evidence; refusing deletion", sql);
        Assert.Contains("Mapped approver permission drift remains", sql);
        Assert.Contains("purchase.requisitions", sql);
        Assert.Contains("purchase.commercial-comparisons", sql);
        Assert.Contains("purchase.po", sql);
        Assert.Contains("CanView", sql);
        Assert.Contains("CanVerify", sql);
        Assert.Contains("CanApprove", sql);
        Assert.Contains("changed after migration; refusing destructive Down", sql);
    }

    [Fact]
    public void MigrationClusterGuardRejectsNonPostgreSqlInBothDirections()
    {
        foreach (var provider in new[] { "SqlServer", string.Empty })
        {
            var migration = new TestMigration();
            Assert.Throws<NotSupportedException>(() => migration.ApplyUp(new MigrationBuilder(provider)));
            Assert.Throws<NotSupportedException>(() => migration.ApplyDown(new MigrationBuilder(provider)));
        }
    }

    [Fact]
    public void FrontendUsesPrOwnedLookupsAndDoesNotBorrowRestrictedMasterPages()
    {
        var api = Read("src", "SESS.NexaERP.Web", "src", "api", "purchase.ts");
        Assert.Contains("/lookups/departments", api);
        Assert.Contains("/lookups/warehouses", api);
        Assert.Contains("/lookups/items", api);
        Assert.DoesNotContain("/employees/lookups", api);
        Assert.DoesNotContain("/inventory/warehouses", api);
        Assert.DoesNotContain("/inventory/items", api);
    }

    private sealed class TestMigration : ApprovalChainReachabilityAndVisibilityCorrections
    {
        internal void ApplyUp(MigrationBuilder builder) => Up(builder);
        internal void ApplyDown(MigrationBuilder builder) => Down(builder);
    }

    private static int Count(string source, string value) =>
        source.Split(value, StringSplitOptions.None).Length - 1;

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { FindRoot() }.Concat(parts).ToArray()));

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
