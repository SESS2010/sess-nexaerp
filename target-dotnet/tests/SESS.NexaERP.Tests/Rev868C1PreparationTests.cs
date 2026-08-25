using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
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
    [Fact]
    public void Rev868c1_resume_verifier_is_migration_free_and_main_database_blocked()
    {
        var source = Read("tools", "resume-rev868c1-isolated-workflow-verification-secure.ps1");

        Assert.Contains("sess_nexaerp_rev868_verify", source);
        Assert.Contains("$blockedDatabaseNames", source);
        Assert.Contains("sess_nexaerp", source);
        Assert.Contains("postgres", source);
        Assert.Contains("template0", source);
        Assert.Contains("template1", source);
        Assert.Contains("rev861", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Refusing unsafe database target", source);
        Assert.Contains("Expected migration missing or duplicated", source);
        Assert.DoesNotContain("ef database update", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dotnet ef", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("migrations add", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("migrations remove", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pg_dump", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pg_restore", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("createdb", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dropdb", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rev868c1_resume_verifier_emits_explicit_final_evidence_sql()
    {
        var source = Read("tools", "resume-rev868c1-isolated-workflow-verification-secure.ps1");

        Assert.Contains("function Get-ResumeSql", source);
        Assert.Contains("select \"MigrationId\", count(*)", source);
        Assert.Contains("PR lifecycle status names and counts", source);
        Assert.Contains("PR lifecycle branch verification", source);
        Assert.Contains("Amount routing boundary evidence", source);
        Assert.Contains("Security 401 403 and self approval evidence", source);
        Assert.Contains("Workflow record counts", source);
        Assert.Contains("Stock reconciliation scenario evidence", source);
        Assert.Contains("Quantity reconciliation violation count", source);
        Assert.Contains("Duplicate active reservation violation count", source);
        Assert.Contains("Duplicate PendingRFQ handoff violation count", source);
        Assert.Contains("Missing location evidence counts", source);
        Assert.Contains("REV868C1-UNAUTHENTICATED-401", source);
        Assert.Contains("REV868C1-DIRECT-API-403", source);
        Assert.Contains("REV868C1-SELF-APPROVAL-403", source);
        Assert.DoesNotContain("\"\"MigrationId\"\"", source);
    }

    [Fact]
    public void Rev868c1_resume_verifier_captures_machine_readable_named_test_results()
    {
        var source = Read("tools", "resume-rev868c1-isolated-workflow-verification-secure.ps1");

        Assert.Contains("--logger \"trx;LogFileName=$trxName\"", source);
        Assert.Contains("Get-TestResultSummary", source);
        Assert.Contains("Named test results", source);
        Assert.Contains("UnitTestResult", source);
        Assert.Contains("Test total:", source);
    }
    [Fact]
    public void Rev868c1_resume_sql_classifier_allows_quoted_lifecycle_update_and_rejects_executable_update()
    {
        Assert.True(IsReadOnlySql("select 'Update' as branch;"));
        Assert.False(IsReadOnlySql("update nexa.purchase_requisitions set \"Status\" = 'Approved';"));
    }

    [Fact]
    public void Rev868c1_resume_sql_classifier_rejects_writable_cte_comments_and_multiple_statements()
    {
        Assert.False(IsReadOnlySql("with changed as (update nexa.purchase_requisitions set \"Status\" = 'Approved' returning 1) select * from changed;"));
        Assert.False(IsReadOnlySql("select 1; -- harmless comment\nupdate nexa.purchase_requisitions set \"Status\" = 'Approved';"));
        Assert.False(IsReadOnlySql("select 1; select 2;"));
        Assert.True(IsReadOnlySql("-- update appears in a comment only\nselect 1;"));
    }

    [Fact]
    public void Rev868c1_resume_all_current_evidence_queries_pass_readonly_classification()
    {
        var source = Read("tools", "resume-rev868c1-isolated-workflow-verification-secure.ps1");
        var matches = Regex.Matches(source, "\"(?<name>[^\"]+)\"\\s*=\\s*@\"\\r?\\n(?<sql>.*?)\\r?\\n\"@\\.Trim\\(\\)", RegexOptions.Singleline);
        Assert.True(matches.Count >= 10, "Expected resume verifier SQL evidence queries to be discoverable.");
        foreach (Match match in matches)
        {
            var name = match.Groups["name"].Value;
            var sql = match.Groups["sql"].Value;
            Assert.True(IsReadOnlySql(sql), $"Query '{name}' must classify as read-only.");
        }
    }

    [Fact]
    public void Rev868c1_resume_verifier_wraps_evidence_queries_in_postgresql_read_only_transaction()
    {
        var source = Read("tools", "resume-rev868c1-isolated-workflow-verification-secure.ps1");

        Assert.Contains("Remove-SqlNonExecutableText", source);
        Assert.Contains("begin transaction read only", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("must contain exactly one executable statement", source);
        Assert.Contains("must start with SELECT or a read-only CTE", source);
        Assert.Contains("insert|update|delete|merge|create|alter|drop|truncate|grant|revoke|copy|call|do", source);
    }

    private static bool IsReadOnlySql(string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return false;
        var stripped = RemoveSqlNonExecutableText(sql).Trim();
        if (!stripped.EndsWith(';')) return false;
        var statements = stripped.Split(';').Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
        if (statements.Length != 1) return false;
        var statement = statements[0];
        if (!Regex.IsMatch(statement, @"(?is)^\s*(select|with)\b")) return false;
        return !Regex.IsMatch(statement, @"(?is)\b(insert|update|delete|merge|create|alter|drop|truncate|grant|revoke|copy|call|do|execute|vacuum|analyze|refresh|listen|notify)\b");
    }

    private static string RemoveSqlNonExecutableText(string sql)
    {
        var chars = sql.ToCharArray();
        var i = 0;
        while (i < chars.Length)
        {
            if (chars[i] == '\'')
            {
                chars[i++] = ' ';
                while (i < chars.Length)
                {
                    if (chars[i] == '\'')
                    {
                        chars[i] = ' ';
                        if (i + 1 < chars.Length && chars[i + 1] == '\'') { chars[i + 1] = ' '; i += 2; continue; }
                        i++;
                        break;
                    }
                    chars[i++] = ' ';
                }
                continue;
            }
            if (chars[i] == '"')
            {
                chars[i++] = ' ';
                while (i < chars.Length)
                {
                    var wasQuote = chars[i] == '"';
                    chars[i++] = ' ';
                    if (wasQuote)
                    {
                        if (i < chars.Length && chars[i] == '"') { chars[i++] = ' '; continue; }
                        break;
                    }
                }
                continue;
            }
            if (chars[i] == '-' && i + 1 < chars.Length && chars[i + 1] == '-')
            {
                chars[i++] = ' ';
                chars[i++] = ' ';
                while (i < chars.Length && chars[i] != '\r' && chars[i] != '\n') chars[i++] = ' ';
                continue;
            }
            if (chars[i] == '/' && i + 1 < chars.Length && chars[i + 1] == '*')
            {
                chars[i++] = ' ';
                chars[i++] = ' ';
                while (i < chars.Length)
                {
                    if (chars[i] == '*' && i + 1 < chars.Length && chars[i + 1] == '/') { chars[i++] = ' '; chars[i++] = ' '; break; }
                    chars[i++] = ' ';
                }
                continue;
            }
            i++;
        }
        return new string(chars);
    }
    [Fact]
    public void Rev868c1_resume_verifier_normalizes_strictmode_sensitive_collections()
    {
        var source = Read("tools", "resume-rev868c1-isolated-workflow-verification-secure.ps1");

        Assert.Contains("$statementParts = @($stripped.Split(';')", source);
        Assert.Contains("$output = @(& $psql", source);
        Assert.Contains("$filtered = @($output", source);
        Assert.Contains("$results = @($trx.SelectNodes", source);
        Assert.Contains("$testOutput = @(& $dotnet test", source);
        Assert.Equal(0, CountNormalized(null));
        Assert.Equal(1, CountNormalized("select 1;"));
        Assert.Equal(2, CountNormalized(new[] { "select 1;", "select 2;" }));
    }

    [Fact]
    public void Rev868c1_resume_sql_classifier_handles_zero_one_and_many_dangerous_token_matches()
    {
        Assert.True(IsReadOnlySql("select 1;"));
        Assert.False(IsReadOnlySql("update nexa.purchase_requisitions set \"Status\" = 'Approved';"));
        Assert.False(IsReadOnlySql("update nexa.purchase_requisitions set \"Status\" = 'Approved'; delete from nexa.audit_logs;"));
        Assert.False(IsReadOnlySql(null));
    }

    [Fact]
    public void Rev868c1_resume_failure_reporting_includes_sanitized_script_line()
    {
        var source = Read("tools", "resume-rev868c1-isolated-workflow-verification-secure.ps1");

        Assert.Contains("catch {", source);
        Assert.Contains("$lineNumber = $_.InvocationInfo.ScriptLineNumber", source);
        Assert.Contains("Sanitized failure: true", source);
        Assert.Contains("Failure line: $lineNumber", source);
    }

    private static int CountNormalized(object? value)
    {
        if (value is null) return 0;
        if (value is Array array) return array.Length;
        return new[] { value }.Length;
    }


    [Fact]
    public void Rev868c2_correction_helper_blocks_main_database_and_keeps_readonly_preflight_sql()
    {
        var helper = Read("tools", "apply-rev868c2-approval-route-correction-secure.ps1");

        Assert.Contains("$blockedDatabaseNames", helper);
        Assert.Contains("sess_nexaerp", helper);
        Assert.Contains("postgres", helper);
        Assert.Contains("template0", helper);
        Assert.Contains("template1", helper);
        Assert.Contains("rev861", helper, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PreflightOnly", helper);
        Assert.Contains("Host: $HostName", helper);
        Assert.Contains("Target DB: $Database", helper);
        Assert.Contains("Rejected DBs: sess_nexaerp, postgres, template0, template1, REV861-like names", helper);
        Assert.Contains("Target corrective migration: $correctionMigration", helper);
        Assert.Contains("GeneratePlanOnly", helper);
        Assert.Contains("begin transaction read only", helper, StringComparison.OrdinalIgnoreCase);
        var preflightOnlySection = helper.Substring(helper.IndexOf("function Get-PreflightSql", StringComparison.Ordinal), helper.IndexOf("function Get-PostMigrationSql", StringComparison.Ordinal) - helper.IndexOf("function Get-PreflightSql", StringComparison.Ordinal));
        Assert.Contains("department_approval_mappings_count", preflightOnlySection);
        Assert.Contains("purchase_approval_route_settings_rev868c2_backup", preflightOnlySection);
        Assert.DoesNotContain("conrelid = 'nexa.department_approval_mappings'::regclass", preflightOnlySection);
        Assert.Contains("department_approval_mappings", helper);
        Assert.DoesNotContain("pg_dump", helper, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pg_restore", helper, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(@"(?i)(^|\s)(createdb)(\s|$)", helper);
        Assert.DoesNotMatch(@"(?i)(^|\s)(dropdb)(\s|$)", helper);
    }


    [Fact]
    public void Rev868c2_recovery_aware_preflight_emits_all_partial_artifact_checks_and_fails_closed()
    {
        var helper = Read("tools", "apply-rev868c2-approval-route-correction-secure.ps1");
        var preflightOnlySection = helper.Substring(helper.IndexOf("function Get-PreflightSql", StringComparison.Ordinal), helper.IndexOf("function Get-PostMigrationSql", StringComparison.Ordinal) - helper.IndexOf("function Get-PreflightSql", StringComparison.Ordinal));
        var executionSection = helper[helper.IndexOf("foreach ($entry in $preflightSql.GetEnumerator())", StringComparison.Ordinal)..];

        Assert.Contains("Recovery-aware partial artifact checks", preflightOnlySection);
        Assert.Contains("migration_history_count", preflightOnlySection);
        Assert.Contains("department_approval_mappings_count", preflightOnlySection);
        Assert.Contains("route_backup_table_count", preflightOnlySection);
        Assert.Contains("approver_resolution_type_column_count", preflightOnlySection);
        Assert.Contains("deterministic_route_rows_count", preflightOnlySection);
        Assert.Contains("migration_owned_createdby_rows_count", preflightOnlySection);
        Assert.Contains("safe_retry_state=", preflightOnlySection);
        Assert.Contains("868c2000-0000-0000-0000-000000000001", preflightOnlySection);
        Assert.Contains("REV868C2_ROUTE_CANONICALIZATION", preflightOnlySection);
        Assert.DoesNotMatch(@"(?i)\b(insert|update|delete|merge|create|alter|drop|truncate)\b", preflightOnlySection);

        Assert.Contains("PreflightOnly fails closed if any partial artifact is found", helper);
        Assert.Contains("Assert-RecoverySafeRetry", helper);
        Assert.True(executionSection.IndexOf("Add-Evidence", StringComparison.Ordinal) < executionSection.IndexOf("Assert-RecoverySafeRetry", StringComparison.Ordinal));
        Assert.True(executionSection.IndexOf("Assert-RecoverySafeRetry", StringComparison.Ordinal) < executionSection.IndexOf("if ($PreflightOnly)", StringComparison.Ordinal));
        Assert.Matches("safe_retry_state=PASS", "safe_retry_state=PASS");
        Assert.DoesNotMatch("safe_retry_state=PASS", "safe_retry_state=FAIL");
    }

    [Fact]
    public void Rev868c2_generate_plan_includes_exact_fk_evidence_and_rejects_malformed_fk_pattern()
    {
        var helper = Read("tools", "apply-rev868c2-approval-route-correction-secure.ps1");
        var sql = Read("outputs", "rev868c2_down_fix_up_idempotent.sql");

        Assert.Contains("Plan/Offline FK Evidence", helper);
        Assert.Contains("DepartmentId REFERENCES nexa.departments", helper);
        Assert.Contains("PrimaryApproverEmployeeId REFERENCES nexa.employees", helper);
        Assert.Contains("AlternateApproverEmployeeId REFERENCES nexa.employees", helper);
        Assert.Contains("malformed REFERENCES `\"Id`\".nexa count=$malformedCount", helper);
        Assert.Contains("FOREIGN KEY (\"DepartmentId\") REFERENCES nexa.departments (\"Id\") ON DELETE RESTRICT", sql);
        Assert.Contains("FOREIGN KEY (\"PrimaryApproverEmployeeId\") REFERENCES nexa.employees (\"Id\") ON DELETE RESTRICT", sql);
        Assert.Contains("FOREIGN KEY (\"AlternateApproverEmployeeId\") REFERENCES nexa.employees (\"Id\") ON DELETE RESTRICT", sql);
        Assert.DoesNotContain("REFERENCES \"Id\".nexa", sql);
    }
    [Fact]
    public void Rev868c2_migrations_are_discoverable_by_ef_migrations_assembly_in_order()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=sess_nexaerp_rev868_design_only;Username=design_only")
            .Options;
        using var db = new NexaErpDbContext(options);
        var migrations = db.GetService<IMigrationsAssembly>().Migrations.Keys.ToList();

        var expected = new[]
        {
            "20260824032638_AdvanceInitialBaseline",
            "20260824135450_MultiCompanySharedIdentityFoundation",
            "20260824150742_CalibrationPurchasePairItemTypeCorrections",
            "20260825063221_EmployeeMasterRebuild42"
        };

        foreach (var id in expected)
            Assert.Equal(1, migrations.Count(x => x == id));

        Assert.Equal(expected, migrations);
        Assert.Equal(1, migrations.Count(x => x.Contains("AdvanceInitialBaseline", StringComparison.Ordinal)));

        for (var i = 1; i < expected.Length; i++)
            Assert.True(migrations.IndexOf(expected[i - 1]) < migrations.IndexOf(expected[i]), $"{expected[i - 1]} should appear before {expected[i]}.");
    }

    [Fact]
    public void Rev868c2_helper_target_matches_discoverable_ef_migration_id()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=sess_nexaerp_rev868_design_only;Username=design_only")
            .Options;
        using var db = new NexaErpDbContext(options);
        var migrations = db.GetService<IMigrationsAssembly>().Migrations.Keys.ToList();
        var helper = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260824032638_AdvanceInitialBaseline.Designer.cs");
        var target = Regex.Match(helper, "\\[Migration\\(\"([^\"]+)\"\\)\\]");

        Assert.True(target.Success);
        Assert.Equal("20260824032638_AdvanceInitialBaseline", target.Groups[1].Value);
        Assert.Contains(target.Groups[1].Value, migrations);
        var obsoleteRev868C2Migration = "202608091" + "15500_Rev868C2ApprovalRouteCanonicalization";
        Assert.DoesNotContain(obsoleteRev868C2Migration, migrations);
        Assert.DoesNotContain(obsoleteRev868C2Migration, helper);
    }

    [Fact]
    public void Rev868c2_archived_department_mapping_preserves_nexa_foreign_key_history()
    {
        var migration = Read("outputs", "archive", "ef_nexa_migration_history_pre_advance",
            "20260809123000_Rev868C2DepartmentManagerApprovalMapping.cs");

        Assert.Contains("schema: \"nexa\"", migration);
        Assert.Contains("principalSchema: \"nexa\"", migration);
        Assert.Contains("FK_department_approval_mappings_departments_DepartmentId", migration);
        Assert.Contains("FK_department_approval_mappings_employees_PrimaryApproverEmployeeId", migration);
        Assert.Contains("FK_department_approval_mappings_employees_AlternateApproverEmployeeId", migration);
        Assert.Contains("principalTable: \"departments\"", migration);
        Assert.Equal(2, Regex.Matches(migration, "principalTable: \\\"employees\\\"").Count);
        Assert.Equal(3, Regex.Matches(migration, "onDelete: ReferentialAction.Restrict").Count);
    }

    [Fact]
    public void Rev868c2_offline_sql_uses_correct_nexa_foreign_key_references()
    {
        var sql = Read("outputs", "rev868c2_down_fix_up_idempotent.sql");

        Assert.Contains("REFERENCES nexa.departments (\"Id\")", sql);
        Assert.Equal(2, Regex.Matches(sql, "REFERENCES nexa\\.employees \\(\"Id\"\\)").Count);
        Assert.DoesNotContain("REFERENCES \"Id\".nexa", sql);
        Assert.DoesNotContain("schema \"Id\"", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("REFERENCES departments.nexa", sql);
        Assert.DoesNotContain("REFERENCES employees.nexa", sql);
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
