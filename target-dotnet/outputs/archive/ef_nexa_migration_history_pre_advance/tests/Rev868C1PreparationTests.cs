// Inert preserved source: excluded from SESS.NexaERP.Tests by location.
// Original active-test source: tests/SESS.NexaERP.Tests/Rev868C1PreparationTests.cs
// These assertions describe the archived pre-advance EF migration history beside this folder.
#if false
    [Fact]
    public void Rev868c2_approval_route_correction_sources_are_isolated_and_canonical()
    {
        var helper = Read("tools", "apply-rev868c2-approval-route-correction-secure.ps1");
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260809123000_Rev868C2DepartmentManagerApprovalMapping.cs");
        var resume = Read("tools", "resume-rev868c1-isolated-workflow-verification-secure.ps1");

        Assert.Contains("sess_nexaerp_rev868_verify", helper);
        Assert.Contains("This helper is permanently restricted to localhost:5432 / sess_nexaerp_rev868_verify", helper);
        Assert.Contains("20260809123000_Rev868C2DepartmentManagerApprovalMapping", helper);
        Assert.Contains("ef database update $correctionMigration", helper, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MANAGER", migration);
        Assert.Contains("TECHNICAL_DIRECTOR", migration);
        Assert.Contains("MANAGING_DIRECTOR", migration);
        Assert.Contains("DEPARTMENT_MAPPING", migration);
        Assert.Contains("FIXED_ROLE", migration);
        Assert.Contains("on conflict (\"RouteCode\") do update", migration);
        Assert.Contains("expected_route=", resume);
        Assert.Contains("configured_route=", resume);
        Assert.Contains("canonical_role=", resume);
        Assert.Contains("display=", resume);
        Assert.DoesNotContain("expected=TechnicalDirector|actual=TD", resume);
    }

    [Fact]
    public void Rev868c2_snapshot_and_designer_metadata_include_context_bound_migrations()
    {
        var mappingDesigner = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260809123000_Rev868C2DepartmentManagerApprovalMapping.Designer.cs");
        var snapshot = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "NexaErpDbContextModelSnapshot.cs");

        Assert.Contains("[DbContext(typeof(NexaErpDbContext))]", mappingDesigner);
        Assert.Contains("[Migration(\"20260809123000_Rev868C2DepartmentManagerApprovalMapping\")]", mappingDesigner);
        Assert.Contains("ApproverResolutionType", snapshot);
        Assert.Contains("department_approval_mappings", snapshot);
        Assert.Contains("DepartmentApprovalMapping", snapshot);
    }

    [Fact]
    public void Rev868c2_down_restores_route_rows_before_reinstating_not_null()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260809123000_Rev868C2DepartmentManagerApprovalMapping.cs");
        var upStart = migration.IndexOf("protected override void Up", StringComparison.Ordinal);
        var downStart = migration.IndexOf("protected override void Down", StringComparison.Ordinal);
        Assert.True(upStart >= 0);
        Assert.True(downStart > upStart);
        var up = migration[upStart..downStart];
        var down = migration[downStart..];

        Assert.Contains("purchase_approval_route_settings_rev868c2_backup", up);
        Assert.Contains("insert into nexa.purchase_approval_route_settings_rev868c2_backup", up);
        Assert.Contains("ApproverRoleCode\" character varying(80) not null", up);

        var dropDepartmentMapping = down.IndexOf("DropTable(name: \"department_approval_mappings\"", StringComparison.Ordinal);
        var restoreFromBackup = down.IndexOf("from nexa.purchase_approval_route_settings_rev868c2_backup b", StringComparison.Ordinal);
        var deleteOwnedRows = down.IndexOf("delete from nexa.purchase_approval_route_settings r", StringComparison.Ordinal);
        var nullGuardLookup = down.IndexOf("if exists (select 1 from nexa.purchase_approval_route_settings where \"ApproverRoleCode\" is null)", StringComparison.Ordinal);
        var nullGuardException = down.IndexOf("raise exception 'REV868C2 rollback cannot restore NOT NULL ApproverRoleCode", StringComparison.Ordinal);
        var alterNotNull = down.IndexOf("nullable: false", StringComparison.Ordinal);
        var dropResolutionType = down.IndexOf("DropColumn(", StringComparison.Ordinal);
        var dropBackup = down.IndexOf("drop table if exists nexa.purchase_approval_route_settings_rev868c2_backup", StringComparison.Ordinal);

        Assert.True(dropDepartmentMapping >= 0);
        Assert.True(restoreFromBackup > dropDepartmentMapping);
        Assert.True(deleteOwnedRows > restoreFromBackup);
        Assert.True(nullGuardLookup > deleteOwnedRows);
        Assert.True(nullGuardException > nullGuardLookup);
        Assert.True(alterNotNull > nullGuardException);
        Assert.DoesNotContain("defaultValue: string.Empty", down);
        Assert.True(dropResolutionType > alterNotNull);
        Assert.True(dropBackup > dropResolutionType);
    }

    [Fact]
    public void Rev868c2_down_preserves_preexisting_manager_and_removes_only_migration_owned_routes()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260809123000_Rev868C2DepartmentManagerApprovalMapping.cs");
        var down = migration[migration.IndexOf("protected override void Down", StringComparison.Ordinal)..];

        Assert.Contains("set \"RouteCode\" = b.\"RouteCode\"", down);
        Assert.Contains("\"ApproverRoleCode\" = b.\"ApproverRoleCode\"", down);
        Assert.Contains("\"IsActive\" = b.\"IsActive\"", down);
        Assert.Contains("\"Version\" = b.\"Version\"", down);
        Assert.Contains("\"MinimumAmount\" = b.\"MinimumAmount\"", down);
        Assert.Contains("\"MaximumAmount\" = b.\"MaximumAmount\"", down);
        Assert.Contains("r.\"CreatedBy\" = 'REV868C2_ROUTE_CANONICALIZATION'", down);
        Assert.Contains("not exists (", down);
        Assert.Contains("b.\"RouteSettingId\" = r.\"Id\"", down);
        Assert.DoesNotContain("delete from nexa.purchase_requisitions", down, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("delete from nexa.pr_status_history", down, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("delete from nexa.pr_approval_history", down, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("delete from nexa.audit_logs", down, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("set \"IsActive\" = false", down);
    }
#endif
