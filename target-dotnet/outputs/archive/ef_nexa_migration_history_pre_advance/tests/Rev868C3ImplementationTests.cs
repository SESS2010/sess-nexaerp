// Inert preserved source: excluded from SESS.NexaERP.Tests by location.
// Original active-test source: tests/SESS.NexaERP.Tests/Rev868C3ImplementationTests.cs
// These assertions describe the archived pre-advance EF migration history beside this folder.
#if false

    [Fact]
    public void Rev868c3_migration_contains_backup_tables_and_exact_rollback_guards()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation.cs");
        var down = migration[migration.IndexOf("protected override void Down", StringComparison.Ordinal)..];

        Assert.Contains("rev868c3_employee_backup", migration);
        Assert.Contains("rev868c3_department_backup", migration);
        Assert.Contains("rev868c3_department_mapping_backup", migration);
        Assert.Contains("on conflict (\"EmployeeCode\") do update", migration);
        Assert.Contains("IX_employees_PayrollEmployeeId", migration);
        Assert.Contains("IsDateOfJoiningApproximate", migration);
        Assert.Contains(Rev868C3EmployeeWorkbookData.ActiveEmployees, x => x.EmployeeCode == "SESS-040" && x.EmployeeName == "NARREN VALENTINO" && x.DateOfJoining == new DateOnly(2026, 2, 1) && !x.DateOfJoiningAccuracy.StartsWith("Approximate", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(Rev868C3EmployeeWorkbookData.ActiveEmployees, x => x.EmployeeCode == "SESS-049" && x.Gender == "Female" && x.PayrollEmployeeId == "1072");
        Assert.Contains("Gender\" = 'Female'", Read("tools", "apply-rev868c3-employee-reconciliation-secure.ps1"));
        Assert.Contains("rollback blocked: employee code integrity failure", down);
        Assert.DoesNotContain("Confidential statutory identifier", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sensitive identifier", migration, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rev868c3_down_removes_only_migration_owned_conflict_indexes()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation.cs");
        var start = migration.IndexOf("private static string DropConflictIndexesSql", StringComparison.Ordinal);
        var end = migration.IndexOf("private static string BuildUpsertSql", start, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start);
        var dropBlock = migration[start..end];

        var ownedIndexes = new[]
        {
            "UX_rev868c3_conflict_departments_code",
            "UX_rev868c3_conflict_designations_code",
            "UX_rev868c3_conflict_employees_employee_code",
            "UX_rev868c3_conflict_roles_code",
            "UX_rev868c3_conflict_role_page_permissions",
            "UX_rev868c3_conflict_employee_role_assignments",
            "UX_rev868c3_conflict_department_approval_mappings",
            "UX_rev868c3_conflict_purchase_approval_workflow_steps"
        };
        foreach (var index in ownedIndexes)
        {
            Assert.Contains($"drop index if exists nexa.\"{index}\"", dropBlock);
        }

        Assert.DoesNotContain("IX_", dropBlock, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("drop index if exists nexa.\"PK_", dropBlock, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rev868c3_migration_persists_approval_workflow_steps()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation.cs");

        Assert.Contains("purchase_approval_workflow_steps", migration);
        Assert.Contains("MANAGER_ONLY", migration);
        Assert.Contains("MANAGER_MD", migration);
        Assert.Contains("MANAGER_MD_TD", migration);
        Assert.Contains("SESS-002", migration);
        Assert.Contains("SESS-001", migration);
        Assert.Contains("FIXED_EMPLOYEE_ROLE", migration);
    }

    [Fact]
    public void Rev868c3_migration_seeds_exact_department_manager_page_permissions()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation.cs");

        Assert.Contains("REV868C3_DEPARTMENT_MANAGER_PERMISSION", migration);
        Assert.Contains("insert into nexa.role_page_permissions", migration);
        Assert.Contains("p.\"PageKey\" = 'purchase.requisitions'", migration);
        Assert.Contains("p.\"PageKey\" = 'purchase.requisition-approvals'", migration);
        Assert.Contains("on conflict (\"RoleId\", \"PageDefinitionId\") do update", migration);
        Assert.Contains("CanApprove", migration);
        Assert.Contains("CanReject", migration);
        Assert.Contains("CanRequestClarification", migration);
        Assert.Contains("CanRequestRevision", migration);
        Assert.Contains("CanViewAuditHistory", migration);
    }


    [Fact]
    public void Rev868c3_designation_insert_satisfies_not_null_is_active_contract()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation.cs");

        Assert.Contains("insert into nexa.designations (\"Id\", \"Code\", \"Name\", \"IsActive\", \"CreatedAt\", \"CreatedBy\", \"UpdatedAt\", \"UpdatedBy\", \"Version\")", migration);
        Assert.Contains("{Sql(designation)}, true, TIMESTAMPTZ", migration);
        Assert.Contains("\"IsActive\" = true", migration);
        Assert.DoesNotContain("insert into nexa.designations (\"Id\", \"Code\", \"Name\", \"CreatedAt\"", migration);
    }

    [Fact]
    public void Rev868c3_migration_insert_schema_contracts_include_required_columns()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation.cs");

        Assert.Contains("insert into nexa.departments (\"Id\", \"Code\", \"Name\", \"IsActive\", \"CreatedAt\", \"CreatedBy\", \"UpdatedAt\", \"UpdatedBy\", \"Version\")", migration);
        Assert.Contains("insert into nexa.designations (\"Id\", \"Code\", \"Name\", \"IsActive\", \"CreatedAt\", \"CreatedBy\", \"UpdatedAt\", \"UpdatedBy\", \"Version\")", migration);
        Assert.Contains("insert into nexa.employees (\"Id\", \"EmployeeCode\", \"PayrollEmployeeId\", \"EmployeeName\", \"OriginalImportedName\", \"Gender\", \"Qualification\", \"DateOfBirth\", \"EmployeeType\", \"Grade\", \"DepartmentId\", \"DesignationId\", \"Status\", \"DateOfJoining\", \"DateOfJoiningAccuracy\", \"IsDateOfJoiningApproximate\", \"ApproximateDateNote\", \"FunctionalResponsibility\", \"WorkLocation\", \"ManagerScope\", \"LegacyDepartment\", \"OfficialEmail\", \"MobileNumber\", \"LoginEnabled\", \"ApprovalStatus\", \"IsEmployeeCodeLocked\", \"CreatedAt\", \"CreatedBy\", \"UpdatedAt\", \"UpdatedBy\", \"Version\")", migration);
        Assert.Contains("insert into nexa.roles (\"Id\", \"Code\", \"Name\", \"IsPrivileged\", \"IsActive\", \"CreatedAt\", \"CreatedBy\", \"UpdatedAt\", \"UpdatedBy\", \"Version\")", migration);
        Assert.Contains("insert into nexa.role_page_permissions (\"Id\", \"RoleId\", \"PageDefinitionId\", \"CanView\", \"CanCreate\", \"CanUpdate\", \"CanSubmit\", \"CanVerify\", \"CanApprove\", \"CanReject\", \"CanRequestClarification\", \"CanRequestRevision\", \"CanResubmit\", \"CanCancel\", \"CanDeactivate\", \"CanPrint\", \"CanDownload\", \"CanExport\", \"CanUploadAttachment\", \"CanReplaceAttachment\", \"CanViewCommercialValues\", \"CanViewAuditHistory\", \"HasFullControl\", \"CreatedAt\", \"CreatedBy\", \"UpdatedAt\", \"UpdatedBy\", \"Version\")", migration);
        Assert.Contains("insert into nexa.employee_role_assignments (\"Id\", \"EmployeeId\", \"RoleId\", \"EffectiveFrom\", \"EffectiveTo\", \"ApprovalStatus\", \"Remarks\", \"CreatedAt\", \"CreatedBy\", \"UpdatedAt\", \"UpdatedBy\", \"Version\")", migration);
        Assert.Contains("insert into nexa.department_approval_mappings (\"Id\", \"DepartmentId\", \"ApprovalRouteCode\", \"Scope\", \"PrimaryApproverEmployeeId\", \"AlternateApproverEmployeeId\", \"EffectiveFrom\", \"EffectiveTo\", \"IsActive\", \"Remarks\", \"CreatedAt\", \"CreatedBy\", \"UpdatedAt\", \"UpdatedBy\", \"Version\")", migration);
        Assert.Contains("insert into nexa.purchase_approval_workflow_steps (\"Id\", \"RouteCode\", \"MinimumAmount\", \"MaximumAmount\", \"StepNumber\", \"ApproverResolutionType\", \"ApproverEmployeeCode\", \"ApproverRoleCode\", \"IsActive\", \"EffectiveFrom\", \"EffectiveTo\", \"Remarks\", \"CreatedAt\", \"CreatedBy\", \"UpdatedAt\", \"UpdatedBy\", \"Version\")", migration);
        Assert.Contains("insert into nexa.employee_status_history (\"Id\", \"EmployeeId\", \"OldStatus\", \"NewStatus\", \"Reason\", \"CreatedAt\", \"CreatedBy\", \"UpdatedAt\", \"UpdatedBy\", \"Version\")", migration);
        Assert.Contains("insert into nexa.employee_department_history (\"Id\", \"EmployeeId\", \"PreviousDepartmentId\", \"NewDepartmentId\", \"Reason\", \"SourceRevision\", \"CorrelationId\", \"CreatedAt\", \"CreatedBy\", \"UpdatedAt\", \"UpdatedBy\", \"Version\")", migration);
        Assert.Contains("insert into nexa.audit_logs (\"Id\", \"Action\", \"AfterJson\", \"BeforeJson\", \"CorrelationId\", \"CreatedAt\", \"CreatedBy\", \"EntityId\", \"EntityName\", \"IpAddress\", \"Module\", \"Result\", \"UpdatedAt\", \"UpdatedBy\", \"UserLoginId\", \"Version\")", migration);
        Assert.DoesNotContain("\"UserRole\"", migration);
        Assert.DoesNotContain("\"OldValue\"", migration);
        Assert.DoesNotContain("\"NewValue\"", migration);
    }

    [Fact]
    public void Rev868c3_raw_sql_inserts_supply_all_required_model_columns()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation.cs");
        var suppliedColumnsByTable = ParseRawInsertColumns(migration);
        var targetTables = new[]
        {
            "departments",
            "designations",
            "employees",
            "roles",
            "role_page_permissions",
            "employee_role_assignments",
            "department_approval_mappings",
            "purchase_approval_workflow_steps",
            "employee_status_history",
            "employee_department_history",
            "audit_logs"
        };

        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=sess_nexaerp_rev868_design_only;Username=design_only")
            .Options;
        using var db = new NexaErpDbContext(options);
        var reportRows = new List<string>();

        foreach (var table in targetTables)
        {
            Assert.True(suppliedColumnsByTable.TryGetValue(table, out var suppliedColumns), $"No REV868C3 raw INSERT columns found for nexa.{table}");
            var requiredColumns = RequiredRawInsertColumns(db, table);
            var missing = requiredColumns.Except(suppliedColumns, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            reportRows.Add($"nexa.{table} | {string.Join(',', requiredColumns.Order(StringComparer.Ordinal))} | {string.Join(',', suppliedColumns.Order(StringComparer.Ordinal))} | {string.Join(',', missing)}");
            Assert.Empty(missing);
        }

        Assert.Contains(reportRows, row => row.StartsWith("nexa.roles |", StringComparison.Ordinal) && row.Contains("IsPrivileged", StringComparison.Ordinal));
    }

    [Fact]
    public void Rev868c3_department_manager_role_is_least_privilege_and_post_verified()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation.cs");
        var helper = Read("tools", "apply-rev868c3-employee-reconciliation-secure.ps1");

        Assert.Contains("'DEPARTMENT_MANAGER', 'Department Manager', false, true", migration);
        Assert.Contains("\"IsPrivileged\" = false", migration);
        Assert.Contains("department_manager_role_state", helper);
        Assert.Contains("manager_role_state_ok", helper);
        Assert.Contains("HasFullControl", helper);
        Assert.Contains("FC=F", helper);
    }


    [Fact]
    public void Rev868c3_employee_reconciliation_uses_actual_master_ids_from_natural_key_lookups()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation.cs");

        Assert.Contains("REV868C3 missing department lookup for employee reconciliation", migration);
        Assert.Contains("REV868C3 missing designation lookup for employee reconciliation", migration);
        Assert.Contains("from (values {Values(Rev868C3EmployeeWorkbookData.Departments.Select(x => x.Code))})", migration);
        Assert.Contains("from (values {Values(designations.Select(Code))})", migration);
        Assert.Contains("select '{Id(\"employee\", employee.EmployeeCode)}'", migration);
        Assert.Contains("from nexa.departments d", migration);
        Assert.Contains("join nexa.designations g on g.\"Code\" = {Sql(Code(employee.HrDesignation))}", migration);
        Assert.Contains("where d.\"Code\" = {Sql(employee.FinalDepartmentCode)}", migration);
        Assert.Contains("d.\"Id\", g.\"Id\", 'Active'", migration);
        Assert.DoesNotContain("'{Id(\"department\", employee.FinalDepartmentCode)}'", migration);
        Assert.DoesNotContain("'{Id(\"designation\", employee.HrDesignation)}'", migration);
    }

    [Fact]
    public void Rev868c3_role_and_permission_reconciliation_uses_actual_role_and_page_ids()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation.cs");

        Assert.Contains("REV868C3 missing DEPARTMENT_MANAGER role lookup", migration);
        Assert.Contains("REV868C3 missing page lookup for department manager permissions", migration);
        Assert.Contains("from nexa.roles r join nexa.page_definitions p on p.\"PageKey\" = 'purchase.requisitions'", migration);
        Assert.Contains("from nexa.roles r join nexa.page_definitions p on p.\"PageKey\" = 'purchase.requisition-approvals'", migration);
        Assert.Contains("select '{Id(\"rev868c3-department-manager-role\", employeeCode)}', e.\"Id\", r.\"Id\"", migration);
        Assert.Contains("join nexa.roles r on r.\"Code\" = 'DEPARTMENT_MANAGER'", migration);
        Assert.DoesNotContain("e.\"Id\", '{Id(\"role\", \"department_manager\")}'", migration);
    }

    [Fact]
    public void Rev868c3_fk_sources_are_audited_for_natural_key_or_persisted_row_lookup()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation.cs");

        Assert.Contains("join nexa.employees p on p.\"EmployeeCode\" = {Sql(mapping.PrimaryManagerCode)}", migration);
        Assert.Contains("join nexa.employees a on a.\"EmployeeCode\" = {Sql(mapping.AlternateManagerCode)}", migration);
        Assert.Contains("where d.\"Code\" = {Sql(mapping.DepartmentCode)}", migration);
        Assert.Contains("select gen_random_uuid(), e.\"Id\", b.\"DepartmentId\", e.\"DepartmentId\"", migration);
        Assert.Contains("left join nexa.rev868c3_employee_backup b on b.\"EmployeeId\" = e.\"Id\"", migration);
        Assert.Contains("Values(IEnumerable<string> values)", migration);
    }
    [Fact]
    public void Rev868c3_upsert_sql_runs_after_scope_aware_mapping_index_creation()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation.cs");

        var addScope = migration.IndexOf("AddColumn<string>(name: \"Scope\"", StringComparison.Ordinal);
        var dropOldIndex = migration.IndexOf("DropIndex(name: \"IX_department_approval_mappings_DepartmentId_ApprovalRouteCod\"", StringComparison.Ordinal);
        var createScopeUniqueIndex = migration.IndexOf("IX_department_approval_mappings_DepartmentId_Route_Scope_From", StringComparison.Ordinal);
        var createScopeActiveIndex = migration.IndexOf("IX_department_approval_mappings_DepartmentId_Route_Scope_Active", StringComparison.Ordinal);
        var upsert = migration.IndexOf("migrationBuilder.Sql(BuildUpsertSql());", StringComparison.Ordinal);

        Assert.True(addScope > 0);
        Assert.True(dropOldIndex > addScope);
        Assert.True(createScopeUniqueIndex > dropOldIndex);
        Assert.True(createScopeActiveIndex > createScopeUniqueIndex);
        Assert.True(upsert > createScopeActiveIndex);
    }
    [Fact]
    public void Rev868c3_migration_version_columns_and_values_are_bigint_compatible()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation.cs");

        Assert.DoesNotContain("table.Column<uint>(type: \"xid\"", migration);
        Assert.Contains("Version = table.Column<long>(type: \"bigint\", nullable: false)", migration);
        Assert.Contains(", 0::bigint)", migration);
        Assert.Contains(", 0::bigint", migration);
        Assert.DoesNotContain("null, null, 0)", migration);
        Assert.DoesNotContain("null, null, 0\r\n", migration);
        Assert.DoesNotContain("null, null, 0\n", migration);
    }
#endif
