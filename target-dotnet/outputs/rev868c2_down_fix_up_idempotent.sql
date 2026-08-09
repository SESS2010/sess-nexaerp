START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809123000_Rev868C2DepartmentManagerApprovalMapping') THEN
        create table if not exists nexa.purchase_approval_route_settings_rev868c2_backup
        (
            "RouteSettingId" uuid primary key,
            "RouteCode" character varying(40) not null,
            "MinimumAmount" numeric(18,2) not null,
            "MaximumAmount" numeric(18,2) null,
            "ApproverRoleCode" character varying(80) not null,
            "IsActive" boolean not null,
            "UpdatedAt" timestamp with time zone null,
            "UpdatedBy" text null,
            "Version" bigint not null,
            "CapturedAt" timestamp with time zone not null,
            "CapturedBy" text not null
        );

        insert into nexa.purchase_approval_route_settings_rev868c2_backup
            ("RouteSettingId", "RouteCode", "MinimumAmount", "MaximumAmount", "ApproverRoleCode", "IsActive", "UpdatedAt", "UpdatedBy", "Version", "CapturedAt", "CapturedBy")
        select "Id", "RouteCode", "MinimumAmount", "MaximumAmount", "ApproverRoleCode", "IsActive", "UpdatedAt", "UpdatedBy", "Version", TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C2_ROUTE_BACKUP'
        from nexa.purchase_approval_route_settings
        where "RouteCode" in ('Manager','MANAGER','BRANCH_MANAGER','MANAGER_APPROVAL','TD','TechnicalDirector','TECHNICALDIRECTOR','TECHNICAL_DIRECTOR','MD','ManagingDirector','MANAGINGDIRECTOR','MANAGING_DIRECTOR')
        on conflict ("RouteSettingId") do nothing;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809123000_Rev868C2DepartmentManagerApprovalMapping') THEN
    ALTER TABLE nexa.purchase_approval_route_settings ADD "ApproverResolutionType" character varying(40) NOT NULL DEFAULT 'FIXED_ROLE';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809123000_Rev868C2DepartmentManagerApprovalMapping') THEN
    ALTER TABLE nexa.purchase_approval_route_settings ALTER COLUMN "ApproverRoleCode" TYPE character varying(80);
    ALTER TABLE nexa.purchase_approval_route_settings ALTER COLUMN "ApproverRoleCode" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809123000_Rev868C2DepartmentManagerApprovalMapping') THEN
        update nexa.purchase_approval_route_settings
        set "RouteCode" = 'MANAGER',
            "ApproverRoleCode" = null,
            "ApproverResolutionType" = 'DEPARTMENT_MAPPING',
            "MinimumAmount" = 0.00,
            "MaximumAmount" = 50000.00,
            "IsActive" = true,
            "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00',
            "UpdatedBy" = 'REV868C2_ROUTE_CANONICALIZATION'
        where "RouteCode" in ('Manager','MANAGER','BRANCH_MANAGER','MANAGER_APPROVAL')
          and not exists (
              select 1 from nexa.purchase_approval_route_settings r
              where r."RouteCode" = 'MANAGER'
                and r."Id" <> nexa.purchase_approval_route_settings."Id");

        update nexa.purchase_approval_route_settings
        set "RouteCode" = 'TECHNICAL_DIRECTOR',
            "ApproverRoleCode" = 'TECHNICAL_DIRECTOR',
            "ApproverResolutionType" = 'FIXED_ROLE',
            "MinimumAmount" = 50000.01,
            "MaximumAmount" = 500000.00,
            "IsActive" = true,
            "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00',
            "UpdatedBy" = 'REV868C2_ROUTE_CANONICALIZATION'
        where "RouteCode" in ('TD','TechnicalDirector','TECHNICALDIRECTOR','TECHNICAL_DIRECTOR')
          and not exists (
              select 1 from nexa.purchase_approval_route_settings r
              where r."RouteCode" = 'TECHNICAL_DIRECTOR'
                and r."Id" <> nexa.purchase_approval_route_settings."Id");

        update nexa.purchase_approval_route_settings
        set "RouteCode" = 'MANAGING_DIRECTOR',
            "ApproverRoleCode" = 'MANAGING_DIRECTOR',
            "ApproverResolutionType" = 'FIXED_ROLE',
            "MinimumAmount" = 500000.01,
            "MaximumAmount" = null,
            "IsActive" = true,
            "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00',
            "UpdatedBy" = 'REV868C2_ROUTE_CANONICALIZATION'
        where "RouteCode" in ('MD','ManagingDirector','MANAGINGDIRECTOR','MANAGING_DIRECTOR')
          and not exists (
              select 1 from nexa.purchase_approval_route_settings r
              where r."RouteCode" = 'MANAGING_DIRECTOR'
                and r."Id" <> nexa.purchase_approval_route_settings."Id");

        insert into nexa.purchase_approval_route_settings
            ("Id", "RouteCode", "MinimumAmount", "MaximumAmount", "ApproverRoleCode", "ApproverResolutionType", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
        values
            ('868c2000-0000-0000-0000-000000000001', 'MANAGER', 0.00, 50000.00, null, 'DEPARTMENT_MAPPING', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C2_ROUTE_CANONICALIZATION', null, null, 0),
            ('868c2000-0000-0000-0000-000000000002', 'TECHNICAL_DIRECTOR', 50000.01, 500000.00, 'TECHNICAL_DIRECTOR', 'FIXED_ROLE', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C2_ROUTE_CANONICALIZATION', null, null, 0),
            ('868c2000-0000-0000-0000-000000000003', 'MANAGING_DIRECTOR', 500000.01, null, 'MANAGING_DIRECTOR', 'FIXED_ROLE', true, TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C2_ROUTE_CANONICALIZATION', null, null, 0)
        on conflict ("RouteCode") do update set
            "MinimumAmount" = excluded."MinimumAmount",
            "MaximumAmount" = excluded."MaximumAmount",
            "ApproverRoleCode" = excluded."ApproverRoleCode",
            "ApproverResolutionType" = excluded."ApproverResolutionType",
            "IsActive" = true,
            "UpdatedAt" = TIMESTAMPTZ '2026-08-09T00:00:00+00:00',
            "UpdatedBy" = 'REV868C2_ROUTE_CANONICALIZATION';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809123000_Rev868C2DepartmentManagerApprovalMapping') THEN
    CREATE TABLE nexa.department_approval_mappings (
        "Id" uuid NOT NULL,
        "DepartmentId" uuid NOT NULL,
        "ApprovalRouteCode" character varying(40) NOT NULL,
        "PrimaryApproverEmployeeId" uuid NOT NULL,
        "AlternateApproverEmployeeId" uuid,
        "EffectiveFrom" date NOT NULL,
        "EffectiveTo" date,
        "IsActive" boolean NOT NULL,
        "Remarks" character varying(500) NOT NULL,
        "CreatedBy" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedBy" text,
        "UpdatedAt" timestamp with time zone,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_department_approval_mappings" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_department_approval_mapping_effective_dates" CHECK ("EffectiveTo" IS NULL OR "EffectiveTo" >= "EffectiveFrom"),
        CONSTRAINT "CK_department_approval_mapping_manager_route" CHECK ("ApprovalRouteCode" = 'MANAGER'),
        CONSTRAINT "FK_department_approval_mappings_departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Id".nexa (departments) ON DELETE RESTRICT,
        CONSTRAINT "FK_department_approval_mappings_employees_AlternateApproverEmployeeId" FOREIGN KEY ("AlternateApproverEmployeeId") REFERENCES "Id".nexa (employees) ON DELETE RESTRICT,
        CONSTRAINT "FK_department_approval_mappings_employees_PrimaryApproverEmployeeId" FOREIGN KEY ("PrimaryApproverEmployeeId") REFERENCES "Id".nexa (employees) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809123000_Rev868C2DepartmentManagerApprovalMapping') THEN
    CREATE UNIQUE INDEX "IX_department_approval_mappings_DepartmentId_ApprovalRouteCod" ON nexa.department_approval_mappings ("DepartmentId", "ApprovalRouteCode", "EffectiveFrom");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809123000_Rev868C2DepartmentManagerApprovalMapping') THEN
    CREATE INDEX "IX_department_approval_mappings_DepartmentId_ApprovalRouteCod1" ON nexa.department_approval_mappings ("DepartmentId", "ApprovalRouteCode", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809123000_Rev868C2DepartmentManagerApprovalMapping') THEN
    CREATE INDEX "IX_department_approval_mappings_PrimaryApproverEmployeeId" ON nexa.department_approval_mappings ("PrimaryApproverEmployeeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809123000_Rev868C2DepartmentManagerApprovalMapping') THEN
    CREATE INDEX "IX_department_approval_mappings_AlternateApproverEmployeeId" ON nexa.department_approval_mappings ("AlternateApproverEmployeeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260809123000_Rev868C2DepartmentManagerApprovalMapping') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260809123000_Rev868C2DepartmentManagerApprovalMapping', '10.0.10');
    END IF;
END $EF$;
COMMIT;

