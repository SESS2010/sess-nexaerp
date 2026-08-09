using System.Diagnostics;
using SESS.NexaERP.Api.Endpoints;
using SESS.NexaERP.Domain.Purchase;

namespace SESS.NexaERP.Tests;

public sealed class Rev868OvernightRemediationTests
{
    [Fact]
    public void Rev868_incident_report_records_missing_backup_without_false_retroactive_claim()
    {
        var report = Read("outputs", "rev868_missing_backup_incident.md");

        Assert.Contains("without a verified pre-REV868 PostgreSQL backup", report);
        Assert.Contains("original REV868 application helper", report);
        Assert.Contains("did not contain mandatory `pg_dump` backup creation logic", report);
        Assert.Contains("discovered only after migration application", report);
        Assert.Contains("must never be represented as a pre-REV868 backup", report);
        Assert.Contains("pre-REV867 backup must not be represented as a pre-REV868 backup", report);
        Assert.Contains("management-approved compensating controls", report);
    }

    [Fact]
    public void Rev868_post_safety_backup_helper_is_clearly_post_migration_and_secure()
    {
        var source = Read("tools", "create-rev868-post-safety-backup-secure.ps1");

        Assert.Contains("Read-Host -AsSecureString", source);
        Assert.Contains("backups\\post-rev868-safety-baseline", source);
        Assert.Contains("post-REV868 safety baseline", source);
        Assert.Contains("must not be used as pre-REV868 evidence", source);
        Assert.Contains("pg_dump.exe", source);
        Assert.Contains("Get-FileHash", source);
        Assert.Contains("Length -le 0", source);
        Assert.Contains("current_database()", source);
        Assert.Contains("sess_nexaerp", source);
        Assert.Contains("GeneratePlanOnly", source);
        Assert.Contains("Remove-Item Env:\\PGPASSWORD", source);
        Assert.DoesNotContain("database update", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pg_restore", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("drop database", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rev868_readonly_postrun_verifier_contains_select_only_schema_evidence()
    {
        var source = Read("tools", "verify-rev868-postrun-readonly-secure.ps1");

        Assert.Contains("GenerateSqlOnly", source);
        Assert.Contains("current_database()", source);
        Assert.Contains("20260808190920_Rev868PurchaseLocationAllocationCorrection", source);
        Assert.Contains("purchase_number_sequences", source);
        Assert.Contains("stock_reservations", source);
        Assert.Contains("stock_availability_check_lines", source);
        Assert.Contains("purchase_requirement_handoffs", source);
        Assert.Contains("purchase.requisitions", source);
        Assert.Contains("page_definitions", source);
        Assert.Contains("role_page_permissions", source);
        Assert.Contains("\"PageKey\"", source);
        Assert.Contains("\"Title\"", source);
        Assert.Contains("\"PageDefinitionId\"", source);
        Assert.DoesNotContain("page_masters", source);
        Assert.DoesNotContain("\"PageCode\"", source);
        Assert.DoesNotContain("\"PageName\"", source);
        Assert.DoesNotContain("\"PageId\"", source);
        Assert.Contains("select \"MigrationId\"", source);
        Assert.DoesNotContain("\"\"MigrationId\"\"", source);
        Assert.DoesNotContain("database update", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pg_dump", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pg_restore", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("insert into", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("delete from", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("update nexa", source, StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public void Rev868_readonly_postrun_generated_sql_uses_actual_page_mapping_and_count_only_queries()
    {
        var verifier = Find("tools", "verify-rev868-postrun-readonly-secure.ps1");
        var output = RunPowerShell(verifier, "-GenerateSqlOnly");

        Assert.Contains("from nexa.page_definitions p", output);
        Assert.Contains("p.\"PageKey\"", output);
        Assert.Contains("p.\"Title\"", output);
        Assert.Contains("rp.\"PageDefinitionId\" = p.\"Id\"", output);
        Assert.DoesNotContain("page_masters", output);
        Assert.DoesNotContain("\"PageCode\"", output);
        Assert.DoesNotContain("\"PageName\"", output);
        Assert.DoesNotContain("\"PageId\"", output);

        Assert.Contains("-- Safe PR workflow counts", output);
        Assert.Contains("'purchase_requisitions=' || count(*) from nexa.purchase_requisitions", output);
        Assert.Contains("'purchase_requisition_lines=' || count(*) from nexa.purchase_requisition_lines", output);
        Assert.Contains("'stock_availability_checks=' || count(*) from nexa.stock_availability_checks", output);
        Assert.Contains("'stock_availability_check_lines=' || count(*) from nexa.stock_availability_check_lines", output);
        Assert.Contains("'stock_reservations=' || count(*) from nexa.stock_reservations", output);
        Assert.Contains("'stock_reservations_active=' || count(*) from nexa.stock_reservations where \"Status\" = 'Active'", output);
        Assert.Contains("'purchase_requirement_handoffs=' || count(*) from nexa.purchase_requirement_handoffs", output);
        Assert.Contains("'purchase_requirement_handoffs_pending_rfq=' || count(*) from nexa.purchase_requirement_handoffs where \"Status\" = 'PendingRFQ'", output);
        Assert.Contains("'purchase_requisition_status_history=' || count(*) from nexa.purchase_requisition_status_history", output);
        Assert.Contains("'purchase_requisition_approval_history=' || count(*) from nexa.purchase_requisition_approval_history", output);
        Assert.Contains("'stock_reservation_history=' || count(*) from nexa.stock_reservation_history", output);
        Assert.Contains("'audit_logs=' || count(*) from nexa.audit_logs", output);
    }

    [Fact]
    public void Rev868_post_safety_backup_folder_is_ignored_without_hiding_other_outputs()
    {
        var gitignore = Read("..", ".gitignore");
        var lines = gitignore.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        Assert.Contains("target-dotnet/backups/post-rev868-safety-baseline/", lines);
        Assert.DoesNotContain("target-dotnet/backups/", lines);
        Assert.DoesNotContain("target-dotnet/outputs/", lines);
    }

    [Fact]
    public void Rev868_isolated_restore_plan_cannot_target_protected_database_names()
    {
        var source = Read("tools", "plan-rev868-isolated-restore-verification.ps1");

        Assert.Contains("sess_nexaerp_rev868_restore_verify_", source);
        Assert.Contains("sess_nexaerp", source);
        Assert.Contains("postgres", source);
        Assert.Contains("template0", source);
        Assert.Contains("template1", source);
        Assert.Contains("No create/drop/restore operation is executed", source);
        Assert.DoesNotContain("createdb", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("drop database", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pg_restore", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rev868_migration_application_helper_now_requires_backup_before_database_update()
    {
        var source = Read("tools", "apply-rev868-secure.ps1");
        var backupIndex = source.IndexOf("Create-PreRev868Backup", StringComparison.Ordinal);
        var updateIndex = source.IndexOf("ef database update $migrationName", StringComparison.OrdinalIgnoreCase);

        Assert.True(backupIndex >= 0, "REV868 helper must contain backup-before-migration logic.");
        Assert.True(updateIndex > backupIndex, "Backup call must appear before EF database update.");
        Assert.Contains("pg_dump.exe", source);
        Assert.Contains("backups\\postgresql\\pre-rev868", source);
        Assert.Contains("pg_dump failed", source);
        Assert.Contains("zero size", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Get-FileHash", source);
        Assert.Contains("Pre-REV868 backup SHA-256", source);
        Assert.Contains("REV868 helper expected database guard failed", source);
        Assert.DoesNotContain("Write-Host $plainPassword", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rev868_workflow_source_covers_pr_lifecycle_and_history_paths()
    {
        var endpoint = Read("src", "SESS.NexaERP.Api", "Endpoints", "PurchaseRequisitionEndpoints.cs");
        var helper = Read("src", "SESS.NexaERP.Api", "Endpoints", "PurchaseRequisitionEndpointHelpers.cs");
        var support = Read("src", "SESS.NexaERP.Api", "Endpoints", "PurchaseRequisitionSupport.cs");
        var combined = endpoint + helper + support;

        Assert.Contains("CreateDraft", combined);
        Assert.Contains("UpdateDraft", combined);
        Assert.Contains("Submit", combined);
        Assert.Contains("DepartmentVerify", combined);
        Assert.Contains("Approve", combined);
        Assert.Contains("Reject", combined);
        Assert.Contains("RequestRevision", combined);
        Assert.Contains("Resubmit", combined);
        Assert.Contains("Cancel", combined);
        Assert.Contains("Hold", combined);
        Assert.Contains("Remarks are required", combined);
        Assert.Contains("Self approval blocked", combined);
        Assert.Contains("Security", combined);
        Assert.Contains("PurchaseRequisitionStatusHistories", combined);
        Assert.Contains("PurchaseRequisitionApprovalHistories", combined);
        Assert.Contains("RequirePagePermission", combined);
        Assert.Contains("Scope(", combined);
    }

    [Fact]
    public void Rev868_amount_routing_boundaries_are_exact()
    {
        Assert.Equal(PurchaseRequisitionApprovalRoutes.Manager, PurchaseRequisitionEndpoints.RouteFor(0));
        Assert.Equal(PurchaseRequisitionApprovalRoutes.Manager, PurchaseRequisitionEndpoints.RouteFor(50_000));
        Assert.Equal(PurchaseRequisitionApprovalRoutes.TechnicalDirector, PurchaseRequisitionEndpoints.RouteFor(50_001));
        Assert.Equal(PurchaseRequisitionApprovalRoutes.TechnicalDirector, PurchaseRequisitionEndpoints.RouteFor(500_000));
        Assert.Equal(PurchaseRequisitionApprovalRoutes.ManagingDirector, PurchaseRequisitionEndpoints.RouteFor(500_001));
    }

    [Fact]
    public void Rev868_stock_reconciliation_covers_full_partial_and_zero_stock_cases()
    {
        var fullReserved = PurchaseRequisitionEndpoints.ReserveQuantity(10, 10);
        Assert.Equal(10, fullReserved);
        Assert.Equal(0, PurchaseRequisitionEndpoints.ShortageQuantity(10, fullReserved));

        var partialReserved = PurchaseRequisitionEndpoints.ReserveQuantity(10, 4);
        Assert.Equal(4, partialReserved);
        Assert.Equal(6, PurchaseRequisitionEndpoints.ShortageQuantity(10, partialReserved));

        var zeroReserved = PurchaseRequisitionEndpoints.ReserveQuantity(10, 0);
        Assert.Equal(0, zeroReserved);
        Assert.Equal(10, PurchaseRequisitionEndpoints.ShortageQuantity(10, zeroReserved));
    }

    [Fact]
    public void Rev868_stock_check_source_blocks_over_reservation_and_duplicate_location_allocations()
    {
        var support = Read("src", "SESS.NexaERP.Api", "Endpoints", "PurchaseRequisitionSupport.cs");
        var context = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "NexaErpDbContext.cs");
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260808190920_Rev868PurchaseLocationAllocationCorrection.cs");

        Assert.Contains("reservation exceeds requested quantity", support);
        Assert.Contains("duplicate warehouse/bin allocation is not allowed", support);
        Assert.Contains("active reservation already exists for warehouse/bin allocation", support);
        Assert.Contains("PurchaseRequisitionLineId, x.LocationKey, x.Status", context);
        Assert.Contains("IX_stock_reservations_PurchaseRequisitionLineId_LocationKey_St", migration);
        Assert.Contains("CK_pr_lines_reconcile_requested", context);
        Assert.Contains("CK_stock_check_lines_quantities_valid", context);
    }

    [Fact]
    public void Rev868_numbering_source_uses_financial_year_sequence_and_unique_constraints()
    {
        var helper = Read("src", "SESS.NexaERP.Api", "Endpoints", "PurchaseRequisitionEndpointHelpers.cs");
        var context = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "NexaErpDbContext.cs");
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260808190920_Rev868PurchaseLocationAllocationCorrection.cs");

        Assert.Contains("FinancialYear", helper);
        Assert.Contains("PurchaseNumberSequences", helper);
        Assert.Contains("LastNumber", helper);
        Assert.Contains("PrSequence", context);
        Assert.Contains("OrganizationId, x.FinancialYear, x.Prefix", context);
        Assert.Contains("purchase_number_sequences", migration);
        Assert.Contains("IX_purchase_requisitions_OrganizationId_FinancialYear_PrSequen", migration);
    }

    [Fact]
    public void Rev868_rollback_review_records_api_stop_and_data_loss_risk()
    {
        var report = Read("outputs", "rev868_rollback_readiness_review.md");

        Assert.Contains("API/runtime must be stopped", report);
        Assert.Contains("data-loss risk", report, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("EF Down", report);
        Assert.Contains("post-REV868 safety baseline", report);
        Assert.Contains("rev868_offline_rollback_to_rev867c1.sql", report);
    }

    [Fact]
    public void Rev868_generate_plan_modes_do_not_prompt_for_password_or_connect()
    {
        var backupHelper = Find("tools", "create-rev868-post-safety-backup-secure.ps1");
        var restorePlan = Find("tools", "plan-rev868-isolated-restore-verification.ps1");

        var backupOutput = RunPowerShell(backupHelper, "-GeneratePlanOnly");
        Assert.DoesNotContain("Enter PostgreSQL password", backupOutput, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No migration apply/remove/rollback command is present", backupOutput);

        var restoreOutput = RunPowerShell(restorePlan, "-GeneratePlanOnly", "-RestoreDatabase", "sess_nexaerp_rev868_restore_verify_morning", "-ExpectedRestoreDatabase", "sess_nexaerp_rev868_restore_verify_morning");
        Assert.Contains("No create/drop/restore operation is executed", restoreOutput);
        Assert.DoesNotContain("Enter PostgreSQL password", restoreOutput, StringComparison.OrdinalIgnoreCase);
    }

    private static string RunPowerShell(string scriptPath, params string[] args)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        process.StartInfo.ArgumentList.Add("-NoProfile");
        process.StartInfo.ArgumentList.Add("-ExecutionPolicy");
        process.StartInfo.ArgumentList.Add("Bypass");
        process.StartInfo.ArgumentList.Add("-File");
        process.StartInfo.ArgumentList.Add(scriptPath);
        foreach (var arg in args) process.StartInfo.ArgumentList.Add(arg);
        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit(15000), "PowerShell helper plan mode timed out.");
        Assert.True(process.ExitCode == 0, output + Environment.NewLine + error);
        return output + Environment.NewLine + error;
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
