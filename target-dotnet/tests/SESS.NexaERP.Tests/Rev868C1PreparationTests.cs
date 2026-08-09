using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class Rev868C1PreparationTests
{
    [Fact]
    public void Rev868c1_helper_is_restricted_to_isolated_database_and_blocks_main_targets()
    {
        var source = Read("tools", "apply-rev868c1-isolated-workflow-verification-secure.ps1");

        Assert.Contains("sess_nexaerp_rev868_verify", source);
        Assert.Contains("NexaErp__ExpectedDatabase", source);
        Assert.Contains("ConnectionStrings__NexaErp", source);
        Assert.Contains("$blockedDatabaseNames", source);
        Assert.Contains("sess_nexaerp", source);
        Assert.Contains("postgres", source);
        Assert.Contains("template0", source);
        Assert.Contains("template1", source);
        Assert.Contains("rev861", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("This helper is permanently restricted to localhost:5432 / sess_nexaerp_rev868_verify", source);
        Assert.DoesNotContain("Database=sess_nexaerp;", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Host=localhost;Database=sess_nexaerp", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rev868c1_helper_has_preflight_and_plan_modes_without_credentials_or_backup_restore_operations()
    {
        var source = Read("tools", "apply-rev868c1-isolated-workflow-verification-secure.ps1");

        Assert.Contains("PreflightOnly", source);
        Assert.Contains("GeneratePlanOnly", source);
        Assert.Contains("Read-Host -AsSecureString", source);
        Assert.Contains("Remove-Item Env:\\ConnectionStrings__NexaErp", source);
        Assert.Contains("Remove-Item Env:\\PGPASSWORD", source);
        Assert.DoesNotContain("Write-Host $plainPassword", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pg_dump", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pg_restore", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("createdb", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dropdb", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("drop database", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rev868c1_helper_declares_expected_readonly_evidence_queries()
    {
        var source = Read("tools", "apply-rev868c1-isolated-workflow-verification-secure.ps1");

        Assert.Contains("select 'database=' || current_database()", source);
        Assert.Contains("select \"MigrationId\", count(*)", source);
        Assert.Contains("purchase_requisitions=", source);
        Assert.Contains("stock_availability_checks=", source);
        Assert.Contains("stock_reservations=", source);
        Assert.Contains("purchase_requirement_handoffs=", source);
        Assert.Contains("purchase_requisition_status_history=", source);
        Assert.Contains("purchase_requisition_approval_history=", source);
        Assert.Contains("stock_reservation_history=", source);
        Assert.Contains("audit_logs=", source);
        Assert.Contains("Duplicate active reservation evidence", source);
        Assert.Contains("Duplicate PendingRFQ handoff evidence", source);
        Assert.Contains("Quantity reconciliation evidence", source);
        Assert.DoesNotContain("\"\"MigrationId\"\"", source);
    }

    [Fact]
    public void Rev868c1_preflight_handles_empty_database_without_direct_history_query()
    {
        var source = Read("tools", "apply-rev868c1-isolated-workflow-verification-secure.ps1");
        var preflightStart = source.IndexOf("function Get-PreflightSql", StringComparison.Ordinal);
        var migrationHistoryStart = source.IndexOf("function Get-MigrationHistorySql", StringComparison.Ordinal);
        Assert.True(preflightStart >= 0, "Get-PreflightSql must exist.");
        Assert.True(migrationHistoryStart > preflightStart, "Migration history SQL must be separate from preflight SQL.");
        var preflightBlock = source[preflightStart..migrationHistoryStart];

        Assert.Contains("EF history relation lookup", preflightBlock);
        Assert.Contains("pg_catalog.pg_class", preflightBlock);
        Assert.Contains("pg_catalog.pg_namespace", preflightBlock);
        Assert.DoesNotContain("from \"public\".\"__EFMigrationsHistory\"", preflightBlock, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("select \"MigrationId\"", preflightBlock, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Get-MigrationHistorySql", source);
        Assert.Contains("EF history absent; MigrationId query intentionally skipped.", source);
        Assert.Contains("empty_and_safe", source);
        Assert.Contains("Full execution requires empty_and_safe preflight state", source);
    }
    [Fact]
    public void Rev868c1_design_time_factory_requires_connection_and_expected_database()
    {
        var source = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "NexaErpDesignTimeDbContextFactory.cs");

        Assert.Contains("Environment.GetEnvironmentVariable(\"ConnectionStrings__NexaErp\")", source);
        Assert.Contains("Environment.GetEnvironmentVariable(\"NexaErp__ExpectedDatabase\")", source);
        Assert.Contains("Design-time connection database does not match", source);
        Assert.DoesNotContain("Database=sess_nexaerp", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Host=localhost;Database", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rev868c1_design_time_factory_fails_closed_without_env_vars()
    {
        var previousConnection = Environment.GetEnvironmentVariable("ConnectionStrings__NexaErp");
        var previousExpected = Environment.GetEnvironmentVariable("NexaErp__ExpectedDatabase");
        try
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__NexaErp", null);
            Environment.SetEnvironmentVariable("NexaErp__ExpectedDatabase", null);

            var ex = Assert.Throws<InvalidOperationException>(() => new NexaErpDesignTimeDbContextFactory().CreateDbContext([]));
            Assert.Contains("ConnectionStrings__NexaErp", ex.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__NexaErp", previousConnection);
            Environment.SetEnvironmentVariable("NexaErp__ExpectedDatabase", previousExpected);
        }
    }

    [Fact]
    public void Rev868c1_design_time_factory_rejects_wrong_expected_database()
    {
        var previousConnection = Environment.GetEnvironmentVariable("ConnectionStrings__NexaErp");
        var previousExpected = Environment.GetEnvironmentVariable("NexaErp__ExpectedDatabase");
        try
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__NexaErp", "Host=127.0.0.1;Port=1;Database=sess_nexaerp;Username=design_only");
            Environment.SetEnvironmentVariable("NexaErp__ExpectedDatabase", "sess_nexaerp_rev868_verify");

            var ex = Assert.Throws<InvalidOperationException>(() => new NexaErpDesignTimeDbContextFactory().CreateDbContext([]));
            Assert.Contains("does not match", ex.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__NexaErp", previousConnection);
            Environment.SetEnvironmentVariable("NexaErp__ExpectedDatabase", previousExpected);
        }
    }

    [Fact]
    public void Rev868c1_postgres_workflow_test_scaffold_is_isolated_and_covers_required_cases()
    {
        var source = Read("tests", "SESS.NexaERP.Tests", "Rev868C1PostgresWorkflowVerificationTests.cs");

        Assert.Contains("REV868C1_POSTGRES", source);
        Assert.Contains("Database=sess_nexaerp_rev868_verify", source);
        Assert.Contains("Rev868c1_purchase_lifecycle_persists_status_approval_and_audit_evidence", source);
        Assert.Contains("Rev868c1_amount_boundaries_and_route_configuration_are_gap_free", source);
        Assert.Contains("Rev868c1_stock_reconciliation_full_partial_and_zero_stock_persist_location_evidence", source);
        Assert.Contains("Rev868c1_duplicate_active_reservation_and_pending_handoff_are_blocked", source);
        Assert.Contains("Rev868c1_failed_allocation_rollback_leaves_no_partial_evidence", source);
        Assert.Contains("Rev868c1_inactive_master_selection_and_direct_stock_editing_are_source_blocked", source);
        Assert.Contains("REV868C1_POSTGRES must not target sess_nexaerp", source);
    }

    [Fact]
    public void Rev868c1_source_checkpoint_report_records_gap_and_execution_controls()
    {
        var report = Read("outputs", "rev868c1_source_checkpoint_report.md");

        Assert.Contains("REV868C1", report);
        Assert.Contains("sess_nexaerp_rev868_verify", report);
        Assert.Contains("source-only", report, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No PostgreSQL command", report);
        Assert.Contains("REV869", report);
        Assert.Contains("real OIDC", report, StringComparison.OrdinalIgnoreCase);
    }

    private static string Read(params string[] relativeParts) => File.ReadAllText(Find(relativeParts));

    private static string Find(params string[] relativeParts)
    {
        var relativePath = Path.Combine(relativeParts);
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate)) return candidate;
            if (directory.Name.Equals("target-dotnet", StringComparison.OrdinalIgnoreCase)) break;
            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find {relativePath} from {AppContext.BaseDirectory}.");
    }
}
