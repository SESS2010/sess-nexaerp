// Inert preserved source: excluded from SESS.NexaERP.Tests by location.
// Original active-test source: tests/SESS.NexaERP.Tests/Rev868C3LegacyDepartmentCorrectionTests.cs
// These assertions describe the archived pre-advance EF migration history beside this folder.
#if false

    [Fact]
    public void Corrective_migration_deactivates_only_four_legacy_departments_and_preserves_exact_rollback_values()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", MigrationId + ".cs");
        var up = Section(migration, "protected override void Up", "protected override void Down");
        var down = migration[migration.IndexOf("protected override void Down", StringComparison.Ordinal)..];

        foreach (var code in LegacyDepartments)
        {
            Assert.Contains(code, up);
        }
        Assert.Contains("rev868c3_legacy_department_deactivation_backup", up);
        Assert.Contains("\"IsActive\"", up);
        Assert.Contains("\"CreatedAt\"", up);
        Assert.Contains("\"CreatedBy\"", up);
        Assert.Contains("\"UpdatedAt\"", up);
        Assert.Contains("\"UpdatedBy\"", up);
        Assert.Contains("\"Version\"", up);
        Assert.Contains("get diagnostics affected_count = row_count", up);
        Assert.Contains("affected_count <> 4", up);

        Assert.Contains("set \"IsActive\" = b.\"IsActive\"", down);
        Assert.Contains("\"CreatedAt\" = b.\"CreatedAt\"", down);
        Assert.Contains("\"CreatedBy\" = b.\"CreatedBy\"", down);
        Assert.Contains("\"UpdatedAt\" = b.\"UpdatedAt\"", down);
        Assert.Contains("\"UpdatedBy\" = b.\"UpdatedBy\"", down);
        Assert.Contains("\"Version\" = b.\"Version\"", down);
        Assert.Contains("exact four-row restore was not proven", down);
    }

    [Fact]
    public void Corrective_migration_contains_no_destructive_delete_or_unrelated_record_rewrite()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", MigrationId + ".cs");

        Assert.DoesNotContain("delete from", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("update nexa.employees", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("employee_status_history", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("employee_department_history", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("purchase_requisitions", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("department_approval_mappings", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("audit_logs", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, CountOccurrences(migration, "update nexa.departments"));
    }
#endif
