START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation') THEN
        create table if not exists nexa.rev868c3_employee_backup
        (
            "EmployeeId" uuid primary key,
            "EmployeeCode" character varying(40) not null,
            "EmployeeName" character varying(200) not null,
            "OriginalImportedName" character varying(200) not null,
            "EmployeeType" character varying(60) not null,
            "Grade" character varying(60) not null,
            "DepartmentId" uuid not null,
            "DesignationId" uuid not null,
            "Status" character varying(40) not null,
            "DateOfJoining" date null,
            "OfficialEmail" character varying(254) null,
            "MobileNumber" character varying(40) null,
            "LoginEnabled" boolean not null,
            "ApprovalStatus" character varying(60) not null,
            "IsEmployeeCodeLocked" boolean not null,
            "UpdatedAt" timestamp with time zone null,
            "UpdatedBy" text null,
            "Version" bigint not null,
            "CapturedAt" timestamp with time zone not null,
            "CapturedBy" text not null
        );

        create table if not exists nexa.rev868c3_department_backup
        (
            "DepartmentId" uuid primary key,
            "Code" character varying(80) not null,
            "Name" character varying(160) not null,
            "IsActive" boolean not null,
            "UpdatedAt" timestamp with time zone null,
            "UpdatedBy" text null,
            "Version" bigint not null,
            "CapturedAt" timestamp with time zone not null,
            "CapturedBy" text not null
        );

        create table if not exists nexa.rev868c3_department_mapping_backup
        (
            "MappingId" uuid primary key,
            "DepartmentId" uuid not null,
            "ApprovalRouteCode" character varying(40) not null,
            "PrimaryApproverEmployeeId" uuid not null,
            "AlternateApproverEmployeeId" uuid null,
            "EffectiveFrom" date not null,
            "EffectiveTo" date null,
            "IsActive" boolean not null,
            "Remarks" character varying(500) not null,
            "UpdatedAt" timestamp with time zone null,
            "UpdatedBy" text null,
            "Version" bigint not null,
            "CapturedAt" timestamp with time zone not null,
            "CapturedBy" text not null
        );


        create table if not exists nexa.rev868c3_role_backup
        (
            "RoleId" uuid primary key,
            "Code" character varying(64) not null,
            "Name" character varying(160) not null,
            "IsPrivileged" boolean not null,
            "IsActive" boolean not null,
            "CreatedAt" timestamp with time zone not null,
            "CreatedBy" text not null,
            "UpdatedAt" timestamp with time zone null,
            "UpdatedBy" text null,
            "Version" bigint not null,
            "CapturedAt" timestamp with time zone not null,
            "CapturedBy" text not null
        );

        insert into nexa.rev868c3_employee_backup
            ("EmployeeId", "EmployeeCode", "EmployeeName", "OriginalImportedName", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "UpdatedAt", "UpdatedBy", "Version", "CapturedAt", "CapturedBy")
        select "Id", "EmployeeCode", "EmployeeName", "OriginalImportedName", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "UpdatedAt", "UpdatedBy", "Version", TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_BACKUP'
        from nexa.employees
        where "EmployeeCode" like 'SESS-%'
        on conflict ("EmployeeId") do nothing;

        insert into nexa.rev868c3_department_backup
            ("DepartmentId", "Code", "Name", "IsActive", "UpdatedAt", "UpdatedBy", "Version", "CapturedAt", "CapturedBy")
        select "Id", "Code", "Name", "IsActive", "UpdatedAt", "UpdatedBy", "Version", TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_DEPARTMENT_BACKUP'
        from nexa.departments
        where "Code" in ('MANAGEMENT','PURCHASE','STORES','ACCOUNTS_FINANCE','HR_ADMIN','PRODUCTION_FABRICATION','DESIGN','ELECTRICAL_PLC_INSTRUMENTATION','REFRIGERATION_MECHANICAL','SERVICE_TECHNICAL_SUPPORT','SOFTWARE_IT','QUALITY_QC','ENGINEER_TECHNICAL','MANAGER','JUNIOR_ASSISTANT','ADMIN_ACCOUNTS_STORES')
        on conflict ("DepartmentId") do nothing;

        insert into nexa.rev868c3_department_mapping_backup
            ("MappingId", "DepartmentId", "ApprovalRouteCode", "PrimaryApproverEmployeeId", "AlternateApproverEmployeeId", "EffectiveFrom", "EffectiveTo", "IsActive", "Remarks", "UpdatedAt", "UpdatedBy", "Version", "CapturedAt", "CapturedBy")
        select "Id", "DepartmentId", "ApprovalRouteCode", "PrimaryApproverEmployeeId", "AlternateApproverEmployeeId", "EffectiveFrom", "EffectiveTo", "IsActive", "Remarks", "UpdatedAt", "UpdatedBy", "Version", TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_MAPPING_BACKUP'
        from nexa.department_approval_mappings
        where "ApprovalRouteCode" = 'MANAGER'
        on conflict ("MappingId") do nothing;


        insert into nexa.rev868c3_role_backup
            ("RoleId", "Code", "Name", "IsPrivileged", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version", "CapturedAt", "CapturedBy")
        select "Id", "Code", "Name", "IsPrivileged", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version", TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_ROLE_BACKUP'
        from nexa.roles
        where "Code" = 'DEPARTMENT_MANAGER'
        on conflict ("RoleId") do nothing;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation') THEN
    ALTER TABLE nexa.employees ADD "PayrollEmployeeId" character varying(40);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation') THEN
    ALTER TABLE nexa.employees ADD "Gender" character varying(40);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation') THEN
    ALTER TABLE nexa.employees ADD "Qualification" character varying(120);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation') THEN
    ALTER TABLE nexa.employees ADD "DateOfBirth" date;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation') THEN
    ALTER TABLE nexa.employees ADD "DateOfJoiningAccuracy" character varying(80);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation') THEN
    ALTER TABLE nexa.employees ADD "IsDateOfJoiningApproximate" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation') THEN
    ALTER TABLE nexa.employees ADD "ApproximateDateNote" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation') THEN
    ALTER TABLE nexa.employees ADD "FunctionalResponsibility" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation') THEN
    ALTER TABLE nexa.employees ADD "WorkLocation" character varying(120);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation') THEN
    ALTER TABLE nexa.employees ADD "ManagerScope" character varying(80);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation') THEN
    ALTER TABLE nexa.employees ADD "LegacyDepartment" character varying(120);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation') THEN
    ALTER TABLE nexa.department_approval_mappings ADD "Scope" character varying(80) NOT NULL DEFAULT 'ALL';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation') THEN
    CREATE TABLE nexa.employee_department_history (
        "Id" uuid NOT NULL,
        "EmployeeId" uuid NOT NULL,
        "PreviousDepartmentId" uuid,
        "NewDepartmentId" uuid NOT NULL,
        "Reason" character varying(500) NOT NULL,
        "SourceRevision" character varying(80) NOT NULL,
        "CorrelationId" character varying(120) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" text NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "UpdatedBy" text,
        "Version" xid NOT NULL,
        CONSTRAINT "PK_employee_department_history" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_employee_department_history_departments_NewDepartmentId" FOREIGN KEY ("NewDepartmentId") REFERENCES nexa.departments ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_employee_department_history_departments_PreviousDepartmentId" FOREIGN KEY ("PreviousDepartmentId") REFERENCES nexa.departments ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_employee_department_history_employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES nexa.employees ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation') THEN
    CREATE INDEX "IX_employee_department_history_CorrelationId" ON nexa.employee_department_history ("CorrelationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation') THEN
    CREATE INDEX "IX_employee_department_history_EmployeeId_CreatedAt" ON nexa.employee_department_history ("EmployeeId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation') THEN
    CREATE INDEX "IX_employee_department_history_NewDepartmentId" ON nexa.employee_department_history ("NewDepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation') THEN
    CREATE INDEX "IX_employee_department_history_PreviousDepartmentId" ON nexa.employee_department_history ("PreviousDepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation') THEN
    insert into nexa.departments ("Id", "Code", "Name", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('0057b580-1cb1-afa2-8328-5afb1162e77e', 'MANAGEMENT', 'Management', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "IsActive" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.departments ("Id", "Code", "Name", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('dd6ab604-a58e-4884-7df9-2ceb7456df64', 'PURCHASE', 'Purchase', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "IsActive" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.departments ("Id", "Code", "Name", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('97353e2b-c03c-03ad-dad5-07e697b6429f', 'STORES', 'Stores', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "IsActive" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.departments ("Id", "Code", "Name", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('987dba43-484e-54ea-0275-e1e5a71eaaa1', 'ACCOUNTS_FINANCE', 'Accounts / Finance', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "IsActive" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.departments ("Id", "Code", "Name", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('00200094-e834-245d-dbef-f270ad2a7d6c', 'HR_ADMIN', 'HR / Admin', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "IsActive" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.departments ("Id", "Code", "Name", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('42dabdd2-c3c8-5e1a-434a-1467574234c8', 'PRODUCTION_FABRICATION', 'Production / Fabrication', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "IsActive" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.departments ("Id", "Code", "Name", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('2243fe97-0335-bcf7-af29-8c0d5e0bac25', 'DESIGN', 'Design', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "IsActive" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.departments ("Id", "Code", "Name", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('85f29457-1dfe-5040-b6c7-6fb323b47e2e', 'ELECTRICAL_PLC_INSTRUMENTATION', 'Electrical / PLC / Instrumentation', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "IsActive" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.departments ("Id", "Code", "Name", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('b73910b3-3ed3-ecfe-8427-eb8be80995d0', 'REFRIGERATION_MECHANICAL', 'Refrigeration / Mechanical', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "IsActive" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.departments ("Id", "Code", "Name", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('578b5561-26e8-d7b2-24e2-cf8a2ffa284d', 'SERVICE_TECHNICAL_SUPPORT', 'Service / Technical Support', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "IsActive" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.departments ("Id", "Code", "Name", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('93ab36a7-befb-0ec4-a051-6fdcd75966c2', 'SOFTWARE_IT', 'Software / IT', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "IsActive" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.departments ("Id", "Code", "Name", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('dd127bff-444a-14b5-48c3-ff527a182050', 'QUALITY_QC', 'Quality / QC', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "IsActive" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.designations ("Id", "Code", "Name", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('086ab1d4-3404-12b7-c35a-4b77737eb97b', 'TECHNICAL_DIRECTOR', 'Technical Director', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.designations ("Id", "Code", "Name", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('70375a48-3d18-2c30-36c6-74405c7a7834', 'MANAGING_DIRECTOR', 'Managing Director', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.designations ("Id", "Code", "Name", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('bf4b3bb5-65e1-95ca-a25d-e1411af21604', 'REFRIGERATION_ENGINEER', 'Refrigeration Engineer', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.designations ("Id", "Code", "Name", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('6114a635-1a56-0b27-452b-23f75a99091a', 'SR_SERVICE_ENGINEER', 'Sr. Service Engineer', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.designations ("Id", "Code", "Name", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('e4bec48d-a248-c13d-a71a-00a2dd40e35e', 'FABRICATOR', 'Fabricator', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.designations ("Id", "Code", "Name", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('2f148a82-10ab-5801-9ff1-9f510611e5fd', 'ELECTRICAL_ENGINEER', 'Electrical Engineer', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.designations ("Id", "Code", "Name", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('97a5bed6-41e7-16a2-5be8-ffee4f315a85', 'JR_ACCOUNTANT', 'Jr. Accountant', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.designations ("Id", "Code", "Name", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('90c527f8-3ea8-dc72-7283-c80e73a71f5d', 'SOFTWARE_DEVELOPER', 'Software Developer', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.designations ("Id", "Code", "Name", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('c9b4f63d-e6f5-ebd0-a62a-40b1cabbe0d8', 'SERVICE_TECHNICIAN', 'Service Technician', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.designations ("Id", "Code", "Name", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('e95eef0e-cd17-95f9-5f66-60037f952028', 'JR_ENGINEER', 'Jr Engineer', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.designations ("Id", "Code", "Name", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('047739a1-c38c-dba8-18b5-e5570bda686f', 'PURCHASE_INCHARGE', 'Purchase Incharge', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.designations ("Id", "Code", "Name", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('05332774-506b-1a5a-56c7-3c5d37eda081', 'STORE_ASSISTANT', 'Store Assistant', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.designations ("Id", "Code", "Name", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('4c9baa15-c3d4-6b41-d040-f354c5cff307', 'DESIGN_ENGINEER', 'Design Engineer', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.designations ("Id", "Code", "Name", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('ce98eb0b-2746-5aba-e2c6-95dba0a230cc', 'HR_EXECUTIVE', 'HR Executive', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.designations ("Id", "Code", "Name", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('8257c94c-3d24-3262-3a88-0c9b78ad714d', 'HOUSEKEEPING', 'Housekeeping', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.designations ("Id", "Code", "Name", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('96908ceb-4e96-b670-db7e-59b2237f1dec', 'PRODUCTION_COORDINATOR', 'Production Coordinator', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.designations ("Id", "Code", "Name", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('377a1f5f-0df5-637b-965a-d7aba799e152', 'JUNIOR_ACCOUNTANT', 'Junior Accountant', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.designations ("Id", "Code", "Name", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('4c22a815-6a44-3d0b-9bd2-45743fc0a9aa', 'JUNIOR_ENGINEER', 'Junior Engineer', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.designations ("Id", "Code", "Name", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('18ee688d-4cf1-067e-d524-e25b2809d089', 'REFRIGERATION_TECHNICIAN', 'Refrigeration Technician', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.designations ("Id", "Code", "Name", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('7fa60e14-7ae8-efd1-cae7-1c3020372f7f', 'PLC_PROGRAMMER', 'PLC Programmer', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.designations ("Id", "Code", "Name", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('6bb2ba80-3bb2-96ba-4779-3e8a6546e828', 'PRODUCTION_&_QUALITY_INCHARGE', 'Production & Quality Incharge', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.designations ("Id", "Code", "Name", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('03ee584a-cdc5-7ff6-9628-da96730c9815', 'FABRICATION_INCHARGE', 'Fabrication Incharge', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.designations ("Id", "Code", "Name", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('54d9e658-bec2-0fc7-12bb-38a679ca4abf', 'JR_SOFTWARE_DEVELOPER', 'Jr. Software Developer', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.designations ("Id", "Code", "Name", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('aeb41cc9-14a7-7b0e-4f2b-3b1e80ea2b3f', 'STORE_EXECUTIVE', 'Store Executive', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.designations ("Id", "Code", "Name", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('43bea8d3-57ee-bdcd-eea0-7d58205b260f', 'SR_ACCOUNTANT', 'Sr. Accountant', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('3543a705-924a-6599-23be-fb9730a93f06', 'SESS-001', '1001', 'A. PARAMANANTHAM', 'A. PARAMANANTHAM', 'Male', 'TO_CONFIRM', null, 'Permanent', 'Executive', '0057b580-1cb1-afa2-8328-5afb1162e77e', '086ab1d4-3404-12b7-c35a-4b77737eb97b', 'Active', null, 'Missing', false, null, 'Technical Director / CEO; top company authority', 'CHENNAI', 'ALL', 'General', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('73a13e4d-73b6-b86f-738b-71261ad69e71', 'SESS-002', '1002', 'P. ALAGUEASWARI', 'P. ALAGUEASWARI', 'Female', 'TO_CONFIRM', null, 'Permanent', 'Executive', '0057b580-1cb1-afa2-8328-5afb1162e77e', '70375a48-3d18-2c30-36c6-74405c7a7834', 'Active', null, 'Missing', false, null, 'Managing Director; first management approval', 'CHENNAI', 'ALL', 'General', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('bc8570aa-774c-9c38-9b42-ddf8599758f0', 'SESS-003', '1004', 'SATHISHKUMAR M', 'SATHISHKUMAR M', 'Male', 'Degree', DATE '1992-12-12', 'Permanent', 'Executive', 'b73910b3-3ed3-ecfe-8427-eb8be80995d0', 'bf4b3bb5-65e1-95ca-a25d-e1411af21604', 'Active', DATE '2018-06-16', 'Source date', false, null, 'Second-level Service Manager; Refrigeration lead', 'CHENNAI', 'ALL', 'Refrigeration', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('889f9bdc-f246-e914-410d-7102ad10e31d', 'SESS-004', '1003', 'DINESH T', 'DINESH T', 'Male', 'Diploma', DATE '1989-05-07', 'Permanent', 'Executive', '578b5561-26e8-d7b2-24e2-cf8a2ffa284d', '6114a635-1a56-0b27-452b-23f75a99091a', 'Active', DATE '2022-01-16', 'Source date', false, null, 'Technical Support / Chennai Service Manager', 'CHENNAI', 'CHENNAI', 'Refrigeration', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('64382325-5125-141e-057e-7ee3f30b2bd3', 'SESS-005', '1007', 'WASEEM S', 'WASEEM S', 'Male', 'ITI', DATE '1988-06-01', 'Permanent', 'Executive', '42dabdd2-c3c8-5e1a-434a-1467574234c8', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', 'Active', DATE '2023-02-02', 'Source date', false, null, 'Fabricator', 'CHENNAI', 'ALL', 'Fabrication', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('fa72ea80-86c0-5f25-f12c-721e76c1daac', 'SESS-006', '1005', 'NANTHAKUMAR S', 'NANTHAKUMAR S', 'Male', 'Degree', DATE '2000-04-12', 'Permanent', 'Executive', '85f29457-1dfe-5040-b6c7-6fb323b47e2e', '2f148a82-10ab-5801-9ff1-9f510611e5fd', 'Active', DATE '2022-02-01', 'Source date', false, null, 'Pune Branch Incharge', 'PUNE', 'ALL', 'Electrical', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('5fdedc5a-1740-164c-04e9-3c6f2db5417c', 'SESS-007', '1018', 'ALFATHIMA PARVEEN A', 'ALFATHIMA PARVEEN A', 'Female', 'Degree', DATE '2003-03-07', 'Permanent', 'Executive', '987dba43-484e-54ea-0275-e1e5a71eaaa1', '97a5bed6-41e7-16a2-5be8-ffee4f315a85', 'Active', DATE '2022-12-02', 'Source date', false, null, 'Accounts Manager', 'CHENNAI', 'ALL', 'Admin', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('22a9f52a-db35-3ab5-0115-5e399bfbf4b2', 'SESS-008', '1016', 'SURANTHER P', 'SURANTHER P', 'Male', 'Degree', DATE '1992-05-20', 'Permanent', 'Executive', '93ab36a7-befb-0ec4-a051-6fdcd75966c2', '90c527f8-3ea8-dc72-7283-c80e73a71f5d', 'Active', DATE '2024-07-05', 'Source date', false, null, 'IT Manager and Software Developer', 'CHENNAI', 'ALL', 'IT', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('04a820d0-3213-a6c2-9ea1-9a5180efcf37', 'SESS-009', '1010', 'MANIKANDAN.S', 'MANIKANDAN.S', 'Male', 'ITI', DATE '2004-04-19', 'Permanent', 'Executive', '578b5561-26e8-d7b2-24e2-cf8a2ffa284d', 'c9b4f63d-e6f5-ebd0-a62a-40b1cabbe0d8', 'Active', DATE '2024-01-02', 'Source date', false, null, 'Junior QC support; QC alternate only during approved delegation', 'CHENNAI', 'CHENNAI', 'Refrigeration', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('45f0c876-d996-210a-67b3-993b7502d3e5', 'SESS-010', '1013', 'RAJESH KUMAR V', 'RAJESH KUMAR V', 'Male', 'ITI', DATE '1997-11-14', 'Permanent', 'Executive', '85f29457-1dfe-5040-b6c7-6fb323b47e2e', '2f148a82-10ab-5801-9ff1-9f510611e5fd', 'Active', DATE '2024-01-29', 'Source date', false, null, 'Electrical Engineer', 'CHENNAI', 'ALL', 'Electrical', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('9cb99d4e-f1a7-7c9b-62e4-dd838db62c91', 'SESS-011', '1015', 'YESWANTH KUMAR N', 'YESWANTH KUMAR N', 'Male', 'ITI', DATE '1998-09-28', 'Permanent', 'Executive', '578b5561-26e8-d7b2-24e2-cf8a2ffa284d', 'e95eef0e-cd17-95f9-5f66-60037f952028', 'Active', DATE '2024-06-20', 'Source date', false, null, 'Bangalore Service Incharge; PR manager up to INR 50,000 for Bangalore Service', 'BANGALORE', 'BANGALORE', 'Refrigeration', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('be7613f2-52e8-5537-06b2-3e25de92c230', 'SESS-012', '1019', 'PRIYA E', 'PRIYA E', 'Female', 'Degree', DATE '1989-01-29', 'Permanent', 'Executive', 'dd6ab604-a58e-4884-7df9-2ceb7456df64', '047739a1-c38c-dba8-18b5-e5570bda686f', 'Active', DATE '2024-10-21', 'Source date', false, null, 'Purchase Manager; Purchase primary approver', 'CHENNAI', 'ALL', 'Admin', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('131cf31d-0cc0-9b70-da2e-89463c49619e', 'SESS-013', '1006', 'LALU', 'LALU', 'Male', 'ITI', DATE '1995-04-01', 'Permanent', 'Executive', '42dabdd2-c3c8-5e1a-434a-1467574234c8', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', 'Active', DATE '2022-02-01', 'Source date', false, null, 'Fabricator', 'CHENNAI', 'ALL', 'Fabrication', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('93216afd-a239-3124-c23e-32d1ff8a8cee', 'SESS-014', '1020', 'KAMALI SRINIVASAN', 'KAMALI SRINIVASAN', 'Female', 'Degree', DATE '1996-06-03', 'Permanent', 'Executive', '97353e2b-c03c-03ad-dad5-07e697b6429f', '05332774-506b-1a5a-56c7-3c5d37eda081', 'Active', DATE '2024-12-04', 'Source date', false, null, 'Stores Manager; Stores primary approver', 'CHENNAI', 'ALL', 'Admin', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('e2815b6b-d417-6f86-177b-fb4fc46a6045', 'SESS-015', '1021', 'RANJITH E', 'RANJITH E', 'Male', 'Diploma', DATE '2001-07-28', 'Permanent', 'Executive', '2243fe97-0335-bcf7-af29-8c0d5e0bac25', '4c9baa15-c3d4-6b41-d040-f354c5cff307', 'Active', DATE '2024-12-09', 'Source date', false, null, 'Regular Product Design Manager', 'CHENNAI', 'REGULAR_PRODUCT', 'Design', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('f1dbc4aa-d567-616d-5e5c-63fd8f049e68', 'SESS-017', '1025', 'MOHD ASHIQ', 'MOHD ASHIQ', 'Male', 'Degree', DATE '2000-09-14', 'Permanent', 'Executive', '578b5561-26e8-d7b2-24e2-cf8a2ffa284d', 'e95eef0e-cd17-95f9-5f66-60037f952028', 'Active', DATE '2024-12-19', 'Source date', false, null, 'Service Engineer; no normal approval authority', 'CHENNAI', 'CHENNAI', 'Electrical', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('1577c211-a6ed-b6ee-d206-5461ad52c428', 'SESS-019', '1027', 'RANJITH R', 'RANJITH R', 'Male', 'Degree', DATE '1999-04-27', 'Permanent', 'Executive', '2243fe97-0335-bcf7-af29-8c0d5e0bac25', '4c9baa15-c3d4-6b41-d040-f354c5cff307', 'Active', DATE '2025-01-02', 'Source date', false, null, 'Project Design Manager', 'CHENNAI', 'PROJECT', 'Design', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('ff338b63-0eab-59d7-56b1-525e1bedfffd', 'SESS-020', '1035', 'RANJEETH B', 'RANJEETH B', 'Male', 'Degree', DATE '1997-08-09', 'Permanent', 'Executive', '00200094-e834-245d-dbef-f270ad2a7d6c', 'ce98eb0b-2746-5aba-e2c6-95dba0a230cc', 'Active', DATE '2025-04-10', 'Source date', false, null, 'HR/Admin Manager', 'CHENNAI', 'ALL', 'Admin', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('c175d954-417c-1d34-435c-8a5dce05ac78', 'SESS-021', null, 'KRISHNAVENI', 'KRISHNAVENI', 'Female', 'TO_CONFIRM', DATE '1980-02-20', 'Permanent', 'Executive', '00200094-e834-245d-dbef-f270ad2a7d6c', '8257c94c-3d24-3262-3a88-0c9b78ad714d', 'Active', DATE '2024-12-25', 'Source date', false, null, 'Housekeeping', 'CHENNAI', 'ALL', 'Admin', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('2cee437e-777d-514a-0fe0-4299dee7df7d', 'SESS-023', '1039', 'SARATH BABU K', 'SARATH BABU K', 'Male', 'Degree', DATE '1993-08-30', 'Permanent', 'Executive', '42dabdd2-c3c8-5e1a-434a-1467574234c8', '96908ceb-4e96-b670-db7e-59b2237f1dec', 'Active', DATE '2025-05-03', 'Source date', false, null, 'Production Manager / Incharge', 'CHENNAI', 'ALL', 'Production', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('26c37705-e799-8708-119b-1227908d5e0f', 'SESS-024', '1034', 'PRAKASAM B', 'PRAKASAM B', 'Male', 'Diploma', DATE '1976-01-03', 'Permanent', 'Executive', '85f29457-1dfe-5040-b6c7-6fb323b47e2e', '2f148a82-10ab-5801-9ff1-9f510611e5fd', 'Active', DATE '2025-04-10', 'Source date', false, null, 'Electrical Engineer', 'CHENNAI', 'ALL', 'Electrical', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('b6292258-84c4-225f-2571-dc1bc204edb7', 'SESS-025', '1036', 'KARTHIKEYAN M.K', 'KARTHIKEYAN M.K', 'Male', 'Degree', DATE '1992-06-05', 'Permanent', 'Executive', '42dabdd2-c3c8-5e1a-434a-1467574234c8', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', 'Active', DATE '2025-04-21', 'Source date', false, null, 'Fabricator', 'CHENNAI', 'ALL', 'Fabrication', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('4f2518af-f9b1-98ce-fa4b-125f1034e56e', 'SESS-026', '1037', 'SRINIVASAN V', 'SRINIVASAN V', 'Male', 'ITI', DATE '1992-01-22', 'Permanent', 'Executive', '42dabdd2-c3c8-5e1a-434a-1467574234c8', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', 'Active', DATE '2025-04-30', 'Source date', false, null, 'Fabricator', 'CHENNAI', 'ALL', 'Fabrication', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('50a5b3a3-aa3a-8269-a283-149d2a69cf8a', 'SESS-029', '1048', 'SRINIVASAN C', 'SRINIVASAN C', 'Male', 'ITI', DATE '1979-03-29', 'Permanent', 'Executive', 'b73910b3-3ed3-ecfe-8427-eb8be80995d0', 'bf4b3bb5-65e1-95ca-a25d-e1411af21604', 'Active', DATE '2025-07-05', 'Source date', false, null, 'Refrigeration Engineer', 'CHENNAI', 'ALL', 'Refrigeration', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('294a2d76-6b76-66d0-76ce-e8d12c02f0c7', 'SESS-030', '1050', 'MANIKANDAN SOKKALINGAM', 'MANIKANDAN SOKKALINGAM', 'Male', 'Degree', DATE '2004-04-19', 'Permanent', 'Executive', '85f29457-1dfe-5040-b6c7-6fb323b47e2e', '2f148a82-10ab-5801-9ff1-9f510611e5fd', 'Active', DATE '2025-09-01', 'Source date', false, null, 'Electrical Engineer', 'CHENNAI', 'ALL', 'Electrical', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('b42a0911-dc25-c491-e26f-b87a7512a0ed', 'SESS-031', '1053', 'VENKAT RAV', 'VENKAT RAV', 'Male', 'Degree', DATE '2004-04-11', 'Permanent', 'Executive', '987dba43-484e-54ea-0275-e1e5a71eaaa1', '377a1f5f-0df5-637b-965a-d7aba799e152', 'Active', DATE '2025-10-06', 'Source date', false, null, 'Junior Accounts and Service Coordinator', 'CHENNAI', 'ALL', 'Admin', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('a8ffe255-91ff-3c05-8f9f-dfa21826f2d5', 'SESS-033', '1054', 'BLESSON PAUL', 'BLESSON PAUL', 'Male', 'Degree', DATE '2003-05-16', 'Permanent', 'Executive', '578b5561-26e8-d7b2-24e2-cf8a2ffa284d', '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa', 'Active', DATE '2025-10-13', 'Source date', false, null, 'Service Engineer; no normal approval authority', 'CHENNAI', 'CHENNAI', 'Electrical', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('b04acf39-5c81-d23c-89e6-9266d39b0be6', 'SESS-034', '1062', 'MADHAN KUMAR J', 'MADHAN KUMAR J', 'Male', 'ITI', DATE '1992-05-10', 'Permanent', 'Executive', 'b73910b3-3ed3-ecfe-8427-eb8be80995d0', '18ee688d-4cf1-067e-d524-e25b2809d089', 'Active', DATE '2026-01-12', 'Source date', false, null, 'Refrigeration Technician', 'CHENNAI', 'ALL', 'Refrigeration', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('3c926af7-c052-2a69-5cad-b961650d230b', 'SESS-035', '1038', 'VINAYAGAM P', 'VINAYAGAM P', 'Male', 'ITI', DATE '1971-06-03', 'Permanent', 'Executive', '42dabdd2-c3c8-5e1a-434a-1467574234c8', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', 'Active', DATE '2025-05-02', 'Source date', false, null, 'Fabricator', 'CHENNAI', 'ALL', 'Fabrication', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('277fd621-865d-2823-1b5c-e13a9c36eb2a', 'SESS-038', '1058', 'SYED IJAZUDDIN Z', 'SYED IJAZUDDIN Z', 'Male', 'Degree', DATE '1994-05-07', 'Permanent', 'Executive', '85f29457-1dfe-5040-b6c7-6fb323b47e2e', '7fa60e14-7ae8-efd1-cae7-1c3020372f7f', 'Active', DATE '2025-12-17', 'Source date', false, null, 'Electrical / PLC / Instrumentation Manager', 'CHENNAI', 'ALL', 'Programming', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('1c2331fd-06fe-76f7-f9ed-f3e586680b10', 'SESS-040', '1065', 'NARREN VALENTINO', 'NARREN VALENTINO', 'Male', 'Degree', DATE '1994-12-02', 'Permanent', 'Senior Engineer', 'dd127bff-444a-14b5-48c3-ff527a182050', '6bb2ba80-3bb2-96ba-4779-3e8a6546e828', 'Active', DATE '2026-02-01', 'Management confirmed exact date', false, null, 'Quality/QC Incharge and Primary Manager', 'CHENNAI FACTORY', 'ALL', 'Quality', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('6324b21c-f7ca-ff71-051b-34542b4c336e', 'SESS-041', '1017', 'PARAMESHWARAN S', 'PARAMESHWARAN S', 'Male', 'ITI', DATE '1966-04-04', 'Permanent', 'TO_CONFIRM', '42dabdd2-c3c8-5e1a-434a-1467574234c8', '03ee584a-cdc5-7ff6-9628-da96730c9815', 'Active', DATE '2024-03-18', 'Source date', false, null, 'Fabrication Incharge', 'CHENNAI', 'ALL', 'Fabrication', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('64f25b00-2a3a-294b-4b08-b7aa4cedc9d8', 'SESS-042', '1064', 'ILAMPARUTHI D', 'ILAMPARUTHI D', 'Male', 'Degree', DATE '2001-12-02', 'Permanent', 'TO_CONFIRM', '93ab36a7-befb-0ec4-a051-6fdcd75966c2', '54d9e658-bec2-0fc7-12bb-38a679ca4abf', 'Active', DATE '2026-03-09', 'Source date', false, null, 'JR. Software Developer', 'CHENNAI', 'ALL', 'IT', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('98f5c6b7-6109-3664-e3c1-52bde77d32fc', 'SESS-043', '1066', 'BHUVANESH M', 'BHUVANESH M', 'Male', 'Degree', DATE '2005-09-05', 'Permanent', 'TO_CONFIRM', 'b73910b3-3ed3-ecfe-8427-eb8be80995d0', '18ee688d-4cf1-067e-d524-e25b2809d089', 'Active', DATE '2026-05-06', 'Source date', false, null, 'Refrigeration Technician', 'CHENNAI', 'ALL', 'Refrigeration', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('924bc9c5-0a6f-23b1-748c-ba986f94a614', 'SESS-044', '1067', 'SUDALAI K', 'SUDALAI K', 'Male', 'Degree', DATE '1999-10-26', 'Permanent', 'TO_CONFIRM', '97353e2b-c03c-03ad-dad5-07e697b6429f', 'aeb41cc9-14a7-7b0e-4f2b-3b1e80ea2b3f', 'Active', DATE '2026-05-07', 'Source date', false, null, 'Store Executive', 'CHENNAI', 'ALL', 'Purchase & Stores', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('0bb9bccb-dda8-dcda-f1e5-f0bd930370f1', 'SESS-045', '1068', 'MOHAMED ASICK', 'MOHAMED ASICK', 'Male', 'Degree', DATE '2004-07-23', 'Permanent', 'TO_CONFIRM', '85f29457-1dfe-5040-b6c7-6fb323b47e2e', '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa', 'Active', DATE '2026-05-07', 'Source date', false, null, 'Junior Engineer', 'CHENNAI', 'ALL', 'Electrical', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('24f749b2-d0f6-4e78-5d58-dce5e99d9938', 'SESS-046', '1069', 'BARATH KUMAR D.S', 'BARATH KUMAR D.S', 'Male', 'Degree', DATE '1999-10-15', 'Permanent', 'TO_CONFIRM', '85f29457-1dfe-5040-b6c7-6fb323b47e2e', '7fa60e14-7ae8-efd1-cae7-1c3020372f7f', 'Active', DATE '2026-05-11', 'Source date', false, null, 'PLC Programmer', 'CHENNAI', 'ALL', 'Programming', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('8d649ed0-44ad-da2c-d0bf-9aaa269a3ada', 'SESS-047', '1070', 'PANBARASU G', 'PANBARASU G', 'Male', 'ITI', DATE '1992-05-01', 'Permanent', 'TO_CONFIRM', 'b73910b3-3ed3-ecfe-8427-eb8be80995d0', 'bf4b3bb5-65e1-95ca-a25d-e1411af21604', 'Active', DATE '2026-05-15', 'Source date', false, null, 'Refrigeration Engineer', 'CHENNAI', 'ALL', 'Refrigeration', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('b838da16-cb43-41fe-e02f-255adc9b3281', 'SESS-048', '1071', 'SRINIVASAN R', 'SRINIVASAN R', 'Male', 'Degree', DATE '1982-02-24', 'Permanent', 'TO_CONFIRM', '42dabdd2-c3c8-5e1a-434a-1467574234c8', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', 'Active', DATE '2026-05-26', 'Source date', false, null, 'Fabricator', 'CHENNAI', 'ALL', 'Fabrication', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('c2de2e6b-2b8f-6cea-6044-1267ccdcdbcc', 'SESS-049', '1072', 'MAGESHWARI K', 'MAGESHWARI K', 'Female', 'Degree', DATE '2002-04-21', 'Permanent', 'TO_CONFIRM', '93ab36a7-befb-0ec4-a051-6fdcd75966c2', '54d9e658-bec2-0fc7-12bb-38a679ca4abf', 'Active', DATE '2026-06-08', 'Source date', false, null, 'Software/IT Alternate Manager during approved delegation', 'CHENNAI', 'ALL', 'IT', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('02e4b71e-66c9-e3df-5175-0ca990eebc6a', 'SESS-050', '1073', 'KARTHICK E', 'KARTHICK E', 'Male', 'Degree', DATE '1996-03-26', 'Permanent', 'TO_CONFIRM', '987dba43-484e-54ea-0275-e1e5a71eaaa1', '43bea8d3-57ee-bdcd-eea0-7d58205b260f', 'Active', DATE '2026-06-10', 'Source date', false, null, 'Sr. Accountant', 'CHENNAI', 'ALL', 'Admin', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('1388074a-b2b9-696d-7b72-edd46bda3267', 'SESS-051', '1074', 'PUSHPARAJ P', 'PUSHPARAJ P', 'Male', 'ITI', DATE '1985-05-24', 'Permanent', 'TO_CONFIRM', 'b73910b3-3ed3-ecfe-8427-eb8be80995d0', 'bf4b3bb5-65e1-95ca-a25d-e1411af21604', 'Active', DATE '2026-06-10', 'Source date', false, null, 'Refrigeration Engineer', 'CHENNAI', 'ALL', 'Refrigeration', null, null, false, 'SeedApproved', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
    on conflict ("EmployeeCode") do update set
        "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    update nexa.employees
    set "Status" = 'Left / Resigned', "LoginEnabled" = false, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION'
    where "EmployeeCode" = 'SESS-016';
    update nexa.employees
    set "Status" = 'Left / Resigned', "LoginEnabled" = false, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION'
    where "EmployeeCode" = 'SESS-018';
    update nexa.employees
    set "Status" = 'Left / Resigned', "LoginEnabled" = false, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION'
    where "EmployeeCode" = 'SESS-022';
    update nexa.employees
    set "Status" = 'Left / Resigned', "LoginEnabled" = false, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION'
    where "EmployeeCode" = 'SESS-027';
    update nexa.employees
    set "Status" = 'Left / Resigned', "LoginEnabled" = false, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION'
    where "EmployeeCode" = 'SESS-028';
    update nexa.employees
    set "Status" = 'Left / Resigned', "LoginEnabled" = false, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION'
    where "EmployeeCode" = 'SESS-032';
    update nexa.employees
    set "Status" = 'Left / Resigned', "LoginEnabled" = false, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION'
    where "EmployeeCode" = 'SESS-036';
    update nexa.employees
    set "Status" = 'Left / Resigned', "LoginEnabled" = false, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION'
    where "EmployeeCode" = 'SESS-037';
    update nexa.employees
    set "Status" = 'Left / Resigned', "LoginEnabled" = false, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION'
    where "EmployeeCode" = 'SESS-039';
    insert into nexa.roles ("Id", "Code", "Name", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    values ('ee6fe478-0775-eed9-0748-ed0cfad68284', 'DEPARTMENT_MANAGER', 'Department Manager', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_DEPARTMENT_MANAGER_PERMISSION', null, null, 0)
    on conflict ("Code") do update set "Name" = excluded."Name", "IsActive" = true, "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_DEPARTMENT_MANAGER_PERMISSION';
    insert into nexa.employee_role_assignments ("Id", "EmployeeId", "RoleId", "EffectiveFrom", "EffectiveTo", "ApprovalStatus", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    select '788617fb-cd68-f08d-24f0-398659b35a1f', e."Id", 'ee6fe478-0775-eed9-0748-ed0cfad68284', DATE '2026-08-09', null, 'SeedApproved', 'REV868C3 approved department manager approval permission', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_DEPARTMENT_MANAGER_PERMISSION', null, null, 0
    from nexa.employees e where e."EmployeeCode" = 'SESS-012'
    on conflict ("EmployeeId", "RoleId", "EffectiveFrom") do nothing;
    insert into nexa.employee_role_assignments ("Id", "EmployeeId", "RoleId", "EffectiveFrom", "EffectiveTo", "ApprovalStatus", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    select '2f2d7539-2a63-b835-320c-62ca03f08506', e."Id", 'ee6fe478-0775-eed9-0748-ed0cfad68284', DATE '2026-08-09', null, 'SeedApproved', 'REV868C3 approved department manager approval permission', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_DEPARTMENT_MANAGER_PERMISSION', null, null, 0
    from nexa.employees e where e."EmployeeCode" = 'SESS-014'
    on conflict ("EmployeeId", "RoleId", "EffectiveFrom") do nothing;
    insert into nexa.employee_role_assignments ("Id", "EmployeeId", "RoleId", "EffectiveFrom", "EffectiveTo", "ApprovalStatus", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    select '932f00e2-aa67-423e-ed36-bb6e5435b731', e."Id", 'ee6fe478-0775-eed9-0748-ed0cfad68284', DATE '2026-08-09', null, 'SeedApproved', 'REV868C3 approved department manager approval permission', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_DEPARTMENT_MANAGER_PERMISSION', null, null, 0
    from nexa.employees e where e."EmployeeCode" = 'SESS-007'
    on conflict ("EmployeeId", "RoleId", "EffectiveFrom") do nothing;
    insert into nexa.employee_role_assignments ("Id", "EmployeeId", "RoleId", "EffectiveFrom", "EffectiveTo", "ApprovalStatus", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    select '3f059cd7-4359-4730-2e7f-ca3a4032464e', e."Id", 'ee6fe478-0775-eed9-0748-ed0cfad68284', DATE '2026-08-09', null, 'SeedApproved', 'REV868C3 approved department manager approval permission', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_DEPARTMENT_MANAGER_PERMISSION', null, null, 0
    from nexa.employees e where e."EmployeeCode" = 'SESS-002'
    on conflict ("EmployeeId", "RoleId", "EffectiveFrom") do nothing;
    insert into nexa.employee_role_assignments ("Id", "EmployeeId", "RoleId", "EffectiveFrom", "EffectiveTo", "ApprovalStatus", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    select 'bd7e5d64-e0d7-768f-4ba1-d46c6dd0b416', e."Id", 'ee6fe478-0775-eed9-0748-ed0cfad68284', DATE '2026-08-09', null, 'SeedApproved', 'REV868C3 approved department manager approval permission', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_DEPARTMENT_MANAGER_PERMISSION', null, null, 0
    from nexa.employees e where e."EmployeeCode" = 'SESS-020'
    on conflict ("EmployeeId", "RoleId", "EffectiveFrom") do nothing;
    insert into nexa.employee_role_assignments ("Id", "EmployeeId", "RoleId", "EffectiveFrom", "EffectiveTo", "ApprovalStatus", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    select '9ec28dad-4357-8b7d-b1bc-123940862a62', e."Id", 'ee6fe478-0775-eed9-0748-ed0cfad68284', DATE '2026-08-09', null, 'SeedApproved', 'REV868C3 approved department manager approval permission', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_DEPARTMENT_MANAGER_PERMISSION', null, null, 0
    from nexa.employees e where e."EmployeeCode" = 'SESS-023'
    on conflict ("EmployeeId", "RoleId", "EffectiveFrom") do nothing;
    insert into nexa.employee_role_assignments ("Id", "EmployeeId", "RoleId", "EffectiveFrom", "EffectiveTo", "ApprovalStatus", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    select '25c1679b-c065-9d78-eec3-f48e03cfc4b6', e."Id", 'ee6fe478-0775-eed9-0748-ed0cfad68284', DATE '2026-08-09', null, 'SeedApproved', 'REV868C3 approved department manager approval permission', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_DEPARTMENT_MANAGER_PERMISSION', null, null, 0
    from nexa.employees e where e."EmployeeCode" = 'SESS-040'
    on conflict ("EmployeeId", "RoleId", "EffectiveFrom") do nothing;
    insert into nexa.employee_role_assignments ("Id", "EmployeeId", "RoleId", "EffectiveFrom", "EffectiveTo", "ApprovalStatus", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    select 'cf59086b-adc4-a6ef-54e3-deda68f972e0', e."Id", 'ee6fe478-0775-eed9-0748-ed0cfad68284', DATE '2026-08-09', null, 'SeedApproved', 'REV868C3 approved department manager approval permission', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_DEPARTMENT_MANAGER_PERMISSION', null, null, 0
    from nexa.employees e where e."EmployeeCode" = 'SESS-015'
    on conflict ("EmployeeId", "RoleId", "EffectiveFrom") do nothing;
    insert into nexa.employee_role_assignments ("Id", "EmployeeId", "RoleId", "EffectiveFrom", "EffectiveTo", "ApprovalStatus", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    select '5b78a990-14d9-413e-aa10-56ef34e93a61', e."Id", 'ee6fe478-0775-eed9-0748-ed0cfad68284', DATE '2026-08-09', null, 'SeedApproved', 'REV868C3 approved department manager approval permission', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_DEPARTMENT_MANAGER_PERMISSION', null, null, 0
    from nexa.employees e where e."EmployeeCode" = 'SESS-019'
    on conflict ("EmployeeId", "RoleId", "EffectiveFrom") do nothing;
    insert into nexa.employee_role_assignments ("Id", "EmployeeId", "RoleId", "EffectiveFrom", "EffectiveTo", "ApprovalStatus", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    select '5a52ea06-b8c2-ccfe-f405-0240c0312a2a', e."Id", 'ee6fe478-0775-eed9-0748-ed0cfad68284', DATE '2026-08-09', null, 'SeedApproved', 'REV868C3 approved department manager approval permission', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_DEPARTMENT_MANAGER_PERMISSION', null, null, 0
    from nexa.employees e where e."EmployeeCode" = 'SESS-038'
    on conflict ("EmployeeId", "RoleId", "EffectiveFrom") do nothing;
    insert into nexa.employee_role_assignments ("Id", "EmployeeId", "RoleId", "EffectiveFrom", "EffectiveTo", "ApprovalStatus", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    select '3830a94f-6f7a-df85-f10d-9bf258348154', e."Id", 'ee6fe478-0775-eed9-0748-ed0cfad68284', DATE '2026-08-09', null, 'SeedApproved', 'REV868C3 approved department manager approval permission', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_DEPARTMENT_MANAGER_PERMISSION', null, null, 0
    from nexa.employees e where e."EmployeeCode" = 'SESS-001'
    on conflict ("EmployeeId", "RoleId", "EffectiveFrom") do nothing;
    insert into nexa.employee_role_assignments ("Id", "EmployeeId", "RoleId", "EffectiveFrom", "EffectiveTo", "ApprovalStatus", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    select '7fc24e40-f8cb-3ad2-15d7-96b41ae9c601', e."Id", 'ee6fe478-0775-eed9-0748-ed0cfad68284', DATE '2026-08-09', null, 'SeedApproved', 'REV868C3 approved department manager approval permission', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_DEPARTMENT_MANAGER_PERMISSION', null, null, 0
    from nexa.employees e where e."EmployeeCode" = 'SESS-003'
    on conflict ("EmployeeId", "RoleId", "EffectiveFrom") do nothing;
    insert into nexa.employee_role_assignments ("Id", "EmployeeId", "RoleId", "EffectiveFrom", "EffectiveTo", "ApprovalStatus", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    select '35217ced-08e6-dbfc-fafd-d26c8b65b2d1', e."Id", 'ee6fe478-0775-eed9-0748-ed0cfad68284', DATE '2026-08-09', null, 'SeedApproved', 'REV868C3 approved department manager approval permission', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_DEPARTMENT_MANAGER_PERMISSION', null, null, 0
    from nexa.employees e where e."EmployeeCode" = 'SESS-004'
    on conflict ("EmployeeId", "RoleId", "EffectiveFrom") do nothing;
    insert into nexa.employee_role_assignments ("Id", "EmployeeId", "RoleId", "EffectiveFrom", "EffectiveTo", "ApprovalStatus", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    select '8e92d11d-66ab-ebea-164b-d243206b965e', e."Id", 'ee6fe478-0775-eed9-0748-ed0cfad68284', DATE '2026-08-09', null, 'SeedApproved', 'REV868C3 approved department manager approval permission', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_DEPARTMENT_MANAGER_PERMISSION', null, null, 0
    from nexa.employees e where e."EmployeeCode" = 'SESS-011'
    on conflict ("EmployeeId", "RoleId", "EffectiveFrom") do nothing;
    insert into nexa.employee_role_assignments ("Id", "EmployeeId", "RoleId", "EffectiveFrom", "EffectiveTo", "ApprovalStatus", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    select '4af24505-3473-95b5-d7b6-090126da8161', e."Id", 'ee6fe478-0775-eed9-0748-ed0cfad68284', DATE '2026-08-09', null, 'SeedApproved', 'REV868C3 approved department manager approval permission', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_DEPARTMENT_MANAGER_PERMISSION', null, null, 0
    from nexa.employees e where e."EmployeeCode" = 'SESS-008'
    on conflict ("EmployeeId", "RoleId", "EffectiveFrom") do nothing;
    insert into nexa.employee_role_assignments ("Id", "EmployeeId", "RoleId", "EffectiveFrom", "EffectiveTo", "ApprovalStatus", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    select 'c3f0b852-ac3d-ec8e-2e25-be542b7acc3f', e."Id", 'ee6fe478-0775-eed9-0748-ed0cfad68284', DATE '2026-08-09', null, 'SeedApproved', 'REV868C3 approved department manager approval permission', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_DEPARTMENT_MANAGER_PERMISSION', null, null, 0
    from nexa.employees e where e."EmployeeCode" = 'SESS-049'
    on conflict ("EmployeeId", "RoleId", "EffectiveFrom") do nothing;
    insert into nexa.employee_role_assignments ("Id", "EmployeeId", "RoleId", "EffectiveFrom", "EffectiveTo", "ApprovalStatus", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    select 'c89cccbe-bb31-49e8-8812-10409eb3a2d0', e."Id", 'ee6fe478-0775-eed9-0748-ed0cfad68284', DATE '2026-08-09', null, 'SeedApproved', 'REV868C3 approved department manager approval permission', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_DEPARTMENT_MANAGER_PERMISSION', null, null, 0
    from nexa.employees e where e."EmployeeCode" = 'SESS-009'
    on conflict ("EmployeeId", "RoleId", "EffectiveFrom") do nothing;
    insert into nexa.department_approval_mappings ("Id", "DepartmentId", "ApprovalRouteCode", "Scope", "PrimaryApproverEmployeeId", "AlternateApproverEmployeeId", "EffectiveFrom", "EffectiveTo", "IsActive", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    select '717107dc-c441-ec11-e1b0-c277544e239f', d."Id", 'MANAGER', 'ALL', p."Id", a."Id", DATE '2026-08-09', null, true, 'Confirmed by management', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0
    from nexa.departments d
    join nexa.employees p on p."EmployeeCode" = 'SESS-012'
    join nexa.employees a on a."EmployeeCode" = 'SESS-014'
    where d."Code" = 'PURCHASE'
    on conflict ("DepartmentId", "ApprovalRouteCode", "Scope", "EffectiveFrom") do update set
        "PrimaryApproverEmployeeId" = excluded."PrimaryApproverEmployeeId", "AlternateApproverEmployeeId" = excluded."AlternateApproverEmployeeId", "IsActive" = true, "Remarks" = excluded."Remarks", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.department_approval_mappings ("Id", "DepartmentId", "ApprovalRouteCode", "Scope", "PrimaryApproverEmployeeId", "AlternateApproverEmployeeId", "EffectiveFrom", "EffectiveTo", "IsActive", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    select '11aa7aff-b243-bc2f-62e8-365899eac7d6', d."Id", 'MANAGER', 'ALL', p."Id", a."Id", DATE '2026-08-09', null, true, 'Confirmed by management', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0
    from nexa.departments d
    join nexa.employees p on p."EmployeeCode" = 'SESS-014'
    join nexa.employees a on a."EmployeeCode" = 'SESS-012'
    where d."Code" = 'STORES'
    on conflict ("DepartmentId", "ApprovalRouteCode", "Scope", "EffectiveFrom") do update set
        "PrimaryApproverEmployeeId" = excluded."PrimaryApproverEmployeeId", "AlternateApproverEmployeeId" = excluded."AlternateApproverEmployeeId", "IsActive" = true, "Remarks" = excluded."Remarks", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.department_approval_mappings ("Id", "DepartmentId", "ApprovalRouteCode", "Scope", "PrimaryApproverEmployeeId", "AlternateApproverEmployeeId", "EffectiveFrom", "EffectiveTo", "IsActive", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    select '8a990da2-fbef-9761-0b83-db568fcd4177', d."Id", 'MANAGER', 'ALL', p."Id", a."Id", DATE '2026-08-09', null, true, 'MD alternate; duplicate-person approval prohibited', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0
    from nexa.departments d
    join nexa.employees p on p."EmployeeCode" = 'SESS-007'
    join nexa.employees a on a."EmployeeCode" = 'SESS-002'
    where d."Code" = 'ACCOUNTS_FINANCE'
    on conflict ("DepartmentId", "ApprovalRouteCode", "Scope", "EffectiveFrom") do update set
        "PrimaryApproverEmployeeId" = excluded."PrimaryApproverEmployeeId", "AlternateApproverEmployeeId" = excluded."AlternateApproverEmployeeId", "IsActive" = true, "Remarks" = excluded."Remarks", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.department_approval_mappings ("Id", "DepartmentId", "ApprovalRouteCode", "Scope", "PrimaryApproverEmployeeId", "AlternateApproverEmployeeId", "EffectiveFrom", "EffectiveTo", "IsActive", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    select 'fe3d3d86-562a-0ba7-d8d8-9957468cafe6', d."Id", 'MANAGER', 'ALL', p."Id", a."Id", DATE '2026-08-09', null, true, 'MD alternate; duplicate-person approval prohibited', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0
    from nexa.departments d
    join nexa.employees p on p."EmployeeCode" = 'SESS-020'
    join nexa.employees a on a."EmployeeCode" = 'SESS-002'
    where d."Code" = 'HR_ADMIN'
    on conflict ("DepartmentId", "ApprovalRouteCode", "Scope", "EffectiveFrom") do update set
        "PrimaryApproverEmployeeId" = excluded."PrimaryApproverEmployeeId", "AlternateApproverEmployeeId" = excluded."AlternateApproverEmployeeId", "IsActive" = true, "Remarks" = excluded."Remarks", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.department_approval_mappings ("Id", "DepartmentId", "ApprovalRouteCode", "Scope", "PrimaryApproverEmployeeId", "AlternateApproverEmployeeId", "EffectiveFrom", "EffectiveTo", "IsActive", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    select '3d3c659f-de08-ce5f-3f65-2c508cc668f5', d."Id", 'MANAGER', 'ALL', p."Id", a."Id", DATE '2026-08-09', null, true, 'Confirmed by management', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0
    from nexa.departments d
    join nexa.employees p on p."EmployeeCode" = 'SESS-023'
    join nexa.employees a on a."EmployeeCode" = 'SESS-040'
    where d."Code" = 'PRODUCTION_FABRICATION'
    on conflict ("DepartmentId", "ApprovalRouteCode", "Scope", "EffectiveFrom") do update set
        "PrimaryApproverEmployeeId" = excluded."PrimaryApproverEmployeeId", "AlternateApproverEmployeeId" = excluded."AlternateApproverEmployeeId", "IsActive" = true, "Remarks" = excluded."Remarks", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.department_approval_mappings ("Id", "DepartmentId", "ApprovalRouteCode", "Scope", "PrimaryApproverEmployeeId", "AlternateApproverEmployeeId", "EffectiveFrom", "EffectiveTo", "IsActive", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    select '016176ba-ffde-3587-8b77-5e6295e769ec', d."Id", 'MANAGER', 'REGULAR_PRODUCT', p."Id", a."Id", DATE '2026-08-09', null, true, 'Scope-based mapping', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0
    from nexa.departments d
    join nexa.employees p on p."EmployeeCode" = 'SESS-015'
    join nexa.employees a on a."EmployeeCode" = 'SESS-019'
    where d."Code" = 'DESIGN'
    on conflict ("DepartmentId", "ApprovalRouteCode", "Scope", "EffectiveFrom") do update set
        "PrimaryApproverEmployeeId" = excluded."PrimaryApproverEmployeeId", "AlternateApproverEmployeeId" = excluded."AlternateApproverEmployeeId", "IsActive" = true, "Remarks" = excluded."Remarks", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.department_approval_mappings ("Id", "DepartmentId", "ApprovalRouteCode", "Scope", "PrimaryApproverEmployeeId", "AlternateApproverEmployeeId", "EffectiveFrom", "EffectiveTo", "IsActive", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    select '3da3b39f-b3ec-deaa-06db-e8979e80d8a0', d."Id", 'MANAGER', 'PROJECT', p."Id", a."Id", DATE '2026-08-09', null, true, 'Scope-based mapping', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0
    from nexa.departments d
    join nexa.employees p on p."EmployeeCode" = 'SESS-019'
    join nexa.employees a on a."EmployeeCode" = 'SESS-015'
    where d."Code" = 'DESIGN'
    on conflict ("DepartmentId", "ApprovalRouteCode", "Scope", "EffectiveFrom") do update set
        "PrimaryApproverEmployeeId" = excluded."PrimaryApproverEmployeeId", "AlternateApproverEmployeeId" = excluded."AlternateApproverEmployeeId", "IsActive" = true, "Remarks" = excluded."Remarks", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.department_approval_mappings ("Id", "DepartmentId", "ApprovalRouteCode", "Scope", "PrimaryApproverEmployeeId", "AlternateApproverEmployeeId", "EffectiveFrom", "EffectiveTo", "IsActive", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    select '802a1234-aafb-2809-2dd4-c0b12c5bc4ad', d."Id", 'MANAGER', 'ALL', p."Id", a."Id", DATE '2026-08-09', null, true, 'TD alternate; count once at highest applicable stage', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0
    from nexa.departments d
    join nexa.employees p on p."EmployeeCode" = 'SESS-038'
    join nexa.employees a on a."EmployeeCode" = 'SESS-001'
    where d."Code" = 'ELECTRICAL_PLC_INSTRUMENTATION'
    on conflict ("DepartmentId", "ApprovalRouteCode", "Scope", "EffectiveFrom") do update set
        "PrimaryApproverEmployeeId" = excluded."PrimaryApproverEmployeeId", "AlternateApproverEmployeeId" = excluded."AlternateApproverEmployeeId", "IsActive" = true, "Remarks" = excluded."Remarks", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.department_approval_mappings ("Id", "DepartmentId", "ApprovalRouteCode", "Scope", "PrimaryApproverEmployeeId", "AlternateApproverEmployeeId", "EffectiveFrom", "EffectiveTo", "IsActive", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    select '0ecd3883-f852-c17b-6820-69947a76365e', d."Id", 'MANAGER', 'ALL', p."Id", a."Id", DATE '2026-08-09', null, true, 'Confirmed by management', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0
    from nexa.departments d
    join nexa.employees p on p."EmployeeCode" = 'SESS-003'
    join nexa.employees a on a."EmployeeCode" = 'SESS-004'
    where d."Code" = 'REFRIGERATION_MECHANICAL'
    on conflict ("DepartmentId", "ApprovalRouteCode", "Scope", "EffectiveFrom") do update set
        "PrimaryApproverEmployeeId" = excluded."PrimaryApproverEmployeeId", "AlternateApproverEmployeeId" = excluded."AlternateApproverEmployeeId", "IsActive" = true, "Remarks" = excluded."Remarks", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.department_approval_mappings ("Id", "DepartmentId", "ApprovalRouteCode", "Scope", "PrimaryApproverEmployeeId", "AlternateApproverEmployeeId", "EffectiveFrom", "EffectiveTo", "IsActive", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    select '1e7fea52-c219-9143-ff76-ac6f2c0bd8e4', d."Id", 'MANAGER', 'CHENNAI', p."Id", a."Id", DATE '2026-08-09', null, true, 'Location-scope mapping', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0
    from nexa.departments d
    join nexa.employees p on p."EmployeeCode" = 'SESS-004'
    join nexa.employees a on a."EmployeeCode" = 'SESS-003'
    where d."Code" = 'SERVICE_TECHNICAL_SUPPORT'
    on conflict ("DepartmentId", "ApprovalRouteCode", "Scope", "EffectiveFrom") do update set
        "PrimaryApproverEmployeeId" = excluded."PrimaryApproverEmployeeId", "AlternateApproverEmployeeId" = excluded."AlternateApproverEmployeeId", "IsActive" = true, "Remarks" = excluded."Remarks", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.department_approval_mappings ("Id", "DepartmentId", "ApprovalRouteCode", "Scope", "PrimaryApproverEmployeeId", "AlternateApproverEmployeeId", "EffectiveFrom", "EffectiveTo", "IsActive", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    select '2ea5d4d6-5cdf-26a5-7817-6bd3e8bfa42f', d."Id", 'MANAGER', 'BANGALORE', p."Id", a."Id", DATE '2026-08-09', null, true, 'Location-scope mapping', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0
    from nexa.departments d
    join nexa.employees p on p."EmployeeCode" = 'SESS-011'
    join nexa.employees a on a."EmployeeCode" = 'SESS-004'
    where d."Code" = 'SERVICE_TECHNICAL_SUPPORT'
    on conflict ("DepartmentId", "ApprovalRouteCode", "Scope", "EffectiveFrom") do update set
        "PrimaryApproverEmployeeId" = excluded."PrimaryApproverEmployeeId", "AlternateApproverEmployeeId" = excluded."AlternateApproverEmployeeId", "IsActive" = true, "Remarks" = excluded."Remarks", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.department_approval_mappings ("Id", "DepartmentId", "ApprovalRouteCode", "Scope", "PrimaryApproverEmployeeId", "AlternateApproverEmployeeId", "EffectiveFrom", "EffectiveTo", "IsActive", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    select '066a6b96-17c4-69f1-a128-9e12b12b431b', d."Id", 'MANAGER', 'ALL', p."Id", a."Id", DATE '2026-08-09', null, true, 'Mageshwari mapped from updated employee list', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0
    from nexa.departments d
    join nexa.employees p on p."EmployeeCode" = 'SESS-008'
    join nexa.employees a on a."EmployeeCode" = 'SESS-049'
    where d."Code" = 'SOFTWARE_IT'
    on conflict ("DepartmentId", "ApprovalRouteCode", "Scope", "EffectiveFrom") do update set
        "PrimaryApproverEmployeeId" = excluded."PrimaryApproverEmployeeId", "AlternateApproverEmployeeId" = excluded."AlternateApproverEmployeeId", "IsActive" = true, "Remarks" = excluded."Remarks", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.department_approval_mappings ("Id", "DepartmentId", "ApprovalRouteCode", "Scope", "PrimaryApproverEmployeeId", "AlternateApproverEmployeeId", "EffectiveFrom", "EffectiveTo", "IsActive", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    select '839ffbc6-440c-622f-1c75-7a82e9fadf41', d."Id", 'MANAGER', 'ALL', p."Id", a."Id", DATE '2026-08-09', null, true, 'Alternate active only during approved delegation', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0
    from nexa.departments d
    join nexa.employees p on p."EmployeeCode" = 'SESS-040'
    join nexa.employees a on a."EmployeeCode" = 'SESS-009'
    where d."Code" = 'QUALITY_QC'
    on conflict ("DepartmentId", "ApprovalRouteCode", "Scope", "EffectiveFrom") do update set
        "PrimaryApproverEmployeeId" = excluded."PrimaryApproverEmployeeId", "AlternateApproverEmployeeId" = excluded."AlternateApproverEmployeeId", "IsActive" = true, "Remarks" = excluded."Remarks", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
    insert into nexa.department_approval_mappings ("Id", "DepartmentId", "ApprovalRouteCode", "Scope", "PrimaryApproverEmployeeId", "AlternateApproverEmployeeId", "EffectiveFrom", "EffectiveTo", "IsActive", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
    select 'e3a3bf53-12cb-3ddf-1ef4-5d69a7b28129', d."Id", 'MANAGER', 'ALL', p."Id", a."Id", DATE '2026-08-09', null, true, 'Special management route; self-approval prohibited', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0
    from nexa.departments d
    join nexa.employees p on p."EmployeeCode" = 'SESS-002'
    join nexa.employees a on a."EmployeeCode" = 'SESS-001'
    where d."Code" = 'MANAGEMENT'
    on conflict ("DepartmentId", "ApprovalRouteCode", "Scope", "EffectiveFrom") do update set
        "PrimaryApproverEmployeeId" = excluded."PrimaryApproverEmployeeId", "AlternateApproverEmployeeId" = excluded."AlternateApproverEmployeeId", "IsActive" = true, "Remarks" = excluded."Remarks", "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00', "UpdatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
        insert into nexa.employee_status_history ("Id", "EmployeeId", "OldStatus", "NewStatus", "Reason", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
        select gen_random_uuid(), e."Id", b."Status", e."Status", 'REV868C3 employee workbook reconciliation; SourceWorkbook=SESS_NexaERP_Final_Employee_Master_2026-08-09.xlsx', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0
        from nexa.employees e
        left join nexa.rev868c3_employee_backup b on b."EmployeeId" = e."Id"
        where e."EmployeeCode" like 'SESS-%'
          and b."EmployeeId" is not null and b."Status" is distinct from e."Status"
          and not exists (select 1 from nexa.employee_status_history h where h."EmployeeId" = e."Id" and h."Reason" like 'REV868C3 employee workbook reconciliation%');

        insert into nexa.employee_department_history ("Id", "EmployeeId", "PreviousDepartmentId", "NewDepartmentId", "Reason", "SourceRevision", "CorrelationId", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
        select gen_random_uuid(), e."Id", b."DepartmentId", e."DepartmentId", 'REV868C3 approved department reconciliation; SourceWorkbook=SESS_NexaERP_Final_Employee_Master_2026-08-09.xlsx', 'REV868C3', 'REV868C3_EMPLOYEE_WORKBOOK_RECONCILIATION', TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0
        from nexa.employees e
        left join nexa.rev868c3_employee_backup b on b."EmployeeId" = e."Id"
        where e."EmployeeCode" like 'SESS-%'
          and (b."EmployeeId" is null or b."DepartmentId" is distinct from e."DepartmentId")
          and not exists (select 1 from nexa.employee_department_history h where h."EmployeeId" = e."Id" and h."CorrelationId" = 'REV868C3_EMPLOYEE_WORKBOOK_RECONCILIATION');

        insert into nexa.audit_logs ("Id", "UserLoginId", "UserRole", "Module", "EntityName", "EntityId", "Action", "OldValue", "NewValue", "Reason", "Result", "CorrelationId", "IpAddress", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
        values (gen_random_uuid(), 'system-migration', 'SYSTEM', 'Employees', 'EmployeeWorkbook', 'REV868C3', 'ReconcileEmployeeDepartmentManagerWorkbook', null, '{"activeEmployees":42,"relievedEmployees":9,"departments":12,"managerMappings":14}', 'Approved REV868C3 employee workbook source checkpoint', 'Success', 'REV868C3_EMPLOYEE_WORKBOOK_RECONCILIATION', null, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION', null, null, 0)
        on conflict do nothing;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation') THEN
    CREATE UNIQUE INDEX "IX_employees_PayrollEmployeeId" ON nexa.employees ("PayrollEmployeeId") WHERE "PayrollEmployeeId" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation') THEN
    DROP INDEX nexa."IX_department_approval_mappings_DepartmentId_ApprovalRouteCod";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation') THEN
    DROP INDEX nexa."IX_department_approval_mappings_DepartmentId_ApprovalRouteCod1";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation') THEN
    CREATE UNIQUE INDEX "IX_department_approval_mappings_DepartmentId_Route_Scope_From" ON nexa.department_approval_mappings ("DepartmentId", "ApprovalRouteCode", "Scope", "EffectiveFrom");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation') THEN
    CREATE INDEX "IX_department_approval_mappings_DepartmentId_Route_Scope_Active" ON nexa.department_approval_mappings ("DepartmentId", "ApprovalRouteCode", "Scope", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation', '10.0.10');
    END IF;
END $EF$;
COMMIT;

