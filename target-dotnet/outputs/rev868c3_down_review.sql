START TRANSACTION;
DROP INDEX nexa."IX_employees_PayrollEmployeeId";

DROP INDEX nexa."IX_department_approval_mappings_DepartmentId_Route_Scope_From";

DROP INDEX nexa."IX_department_approval_mappings_DepartmentId_Route_Scope_Active";

    delete from nexa.employee_status_history where "CreatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION' and "Reason" like 'REV868C3 employee workbook reconciliation%';
    delete from nexa.employee_department_history where "CreatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION' and "CorrelationId" = 'REV868C3_EMPLOYEE_WORKBOOK_RECONCILIATION';
    delete from nexa.audit_logs where "CorrelationId" = 'REV868C3_EMPLOYEE_WORKBOOK_RECONCILIATION' and "CreatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';

    delete from nexa.department_approval_mappings m
    where m."CreatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION'
      and not exists (select 1 from nexa.rev868c3_department_mapping_backup b where b."MappingId" = m."Id");

    update nexa.department_approval_mappings m
    set "DepartmentId" = b."DepartmentId",
        "ApprovalRouteCode" = b."ApprovalRouteCode",
        "PrimaryApproverEmployeeId" = b."PrimaryApproverEmployeeId",
        "AlternateApproverEmployeeId" = b."AlternateApproverEmployeeId",
        "EffectiveFrom" = b."EffectiveFrom",
        "EffectiveTo" = b."EffectiveTo",
        "IsActive" = b."IsActive",
        "Remarks" = b."Remarks",
        "UpdatedAt" = b."UpdatedAt",
        "UpdatedBy" = b."UpdatedBy",
        "Version" = b."Version"
    from nexa.rev868c3_department_mapping_backup b
    where m."Id" = b."MappingId";

    delete from nexa.employee_role_assignments where "CreatedBy" = 'REV868C3_DEPARTMENT_MANAGER_PERMISSION';
    delete from nexa.role_page_permissions where "CreatedBy" = 'REV868C3_DEPARTMENT_MANAGER_PERMISSION';
    delete from nexa.roles r
    where r."Code" = 'DEPARTMENT_MANAGER'
      and r."CreatedBy" = 'REV868C3_DEPARTMENT_MANAGER_PERMISSION'
      and not exists (select 1 from nexa.rev868c3_role_backup b where b."RoleId" = r."Id");

    update nexa.roles r
    set "Code" = b."Code",
        "Name" = b."Name",
        "IsPrivileged" = b."IsPrivileged",
        "IsActive" = b."IsActive",
        "CreatedAt" = b."CreatedAt",
        "CreatedBy" = b."CreatedBy",
        "UpdatedAt" = b."UpdatedAt",
        "UpdatedBy" = b."UpdatedBy",
        "Version" = b."Version"
    from nexa.rev868c3_role_backup b
    where r."Id" = b."RoleId";

    delete from nexa.employees e
    where e."CreatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION'
      and not exists (select 1 from nexa.rev868c3_employee_backup b where b."EmployeeId" = e."Id");

    update nexa.employees e
    set "EmployeeName" = b."EmployeeName",
        "OriginalImportedName" = b."OriginalImportedName",
        "EmployeeType" = b."EmployeeType",
        "Grade" = b."Grade",
        "DepartmentId" = b."DepartmentId",
        "DesignationId" = b."DesignationId",
        "Status" = b."Status",
        "DateOfJoining" = b."DateOfJoining",
        "OfficialEmail" = b."OfficialEmail",
        "MobileNumber" = b."MobileNumber",
        "LoginEnabled" = b."LoginEnabled",
        "ApprovalStatus" = b."ApprovalStatus",
        "IsEmployeeCodeLocked" = b."IsEmployeeCodeLocked",
        "UpdatedAt" = b."UpdatedAt",
        "UpdatedBy" = b."UpdatedBy",
        "Version" = b."Version"
    from nexa.rev868c3_employee_backup b
    where e."Id" = b."EmployeeId";

    update nexa.departments d
    set "Code" = b."Code",
        "Name" = b."Name",
        "IsActive" = b."IsActive",
        "UpdatedAt" = b."UpdatedAt",
        "UpdatedBy" = b."UpdatedBy",
        "Version" = b."Version"
    from nexa.rev868c3_department_backup b
    where d."Id" = b."DepartmentId";

    delete from nexa.departments d
    where d."CreatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION'
      and not exists (select 1 from nexa.rev868c3_department_backup b where b."DepartmentId" = d."Id");

    delete from nexa.designations where "CreatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';

    do $$
    begin
        if exists (select 1 from nexa.employees where "EmployeeCode" is null or length(trim("EmployeeCode")) = 0) then
            raise exception 'REV868C3 rollback blocked: employee code integrity failure';
        end if;
    end $$;

DROP TABLE nexa.employee_department_history;

ALTER TABLE nexa.department_approval_mappings DROP COLUMN "Scope";

ALTER TABLE nexa.employees DROP COLUMN "PayrollEmployeeId";

ALTER TABLE nexa.employees DROP COLUMN "Gender";

ALTER TABLE nexa.employees DROP COLUMN "Qualification";

ALTER TABLE nexa.employees DROP COLUMN "DateOfBirth";

ALTER TABLE nexa.employees DROP COLUMN "DateOfJoiningAccuracy";

ALTER TABLE nexa.employees DROP COLUMN "IsDateOfJoiningApproximate";

ALTER TABLE nexa.employees DROP COLUMN "ApproximateDateNote";

ALTER TABLE nexa.employees DROP COLUMN "FunctionalResponsibility";

ALTER TABLE nexa.employees DROP COLUMN "WorkLocation";

ALTER TABLE nexa.employees DROP COLUMN "ManagerScope";

ALTER TABLE nexa.employees DROP COLUMN "LegacyDepartment";

CREATE UNIQUE INDEX "IX_department_approval_mappings_DepartmentId_ApprovalRouteCod" ON nexa.department_approval_mappings ("DepartmentId", "ApprovalRouteCode", "EffectiveFrom");

CREATE INDEX "IX_department_approval_mappings_DepartmentId_ApprovalRouteCod1" ON nexa.department_approval_mappings ("DepartmentId", "ApprovalRouteCode", "IsActive");

    drop table if exists nexa.rev868c3_department_mapping_backup;
    drop table if exists nexa.rev868c3_role_backup;
    drop table if exists nexa.rev868c3_department_backup;
    drop table if exists nexa.rev868c3_employee_backup;

DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation';

COMMIT;

