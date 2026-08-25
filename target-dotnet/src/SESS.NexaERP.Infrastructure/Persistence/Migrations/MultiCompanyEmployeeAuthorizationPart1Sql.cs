namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class MultiCompanyEmployeeAuthorizationPart1Sql
{
    internal static string PreUp => AdvanceSchemaSql.Expand(PreUpSql);
    internal static string Up => AdvanceSchemaSql.Expand(UpPrefix + MultiCompanyEmployeeAuthorizationPart1Data.EmployeeRoleSql + UpSuffix);
    internal static string Down => AdvanceSchemaSql.Expand(DownSql);

    private const string PreUpSql = """
        DO $cluster_guard$
        BEGIN
          IF current_setting('server_version_num')::integer < 170000 THEN RAISE EXCEPTION 'Multi-company employee authorization Part 1 requires PostgreSQL 17 or later.'; END IF;
          IF current_database() IN ('postgres','template0','template1') THEN RAISE EXCEPTION 'Multi-company employee authorization Part 1 refuses a PostgreSQL administrative database.'; END IF;
          IF to_regnamespace('__advance_schema__') IS NULL THEN RAISE EXCEPTION 'Multi-company employee authorization Part 1 requires the advance schema.'; END IF;
        END $cluster_guard$;

        LOCK TABLE __advance_schema__.companies IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.employees IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.roles IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.employee_company_assignments IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.employee_department_assignments IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.employee_role_assignments IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.employee_operational_scopes IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.employee_identity_mappings IN SHARE ROW EXCLUSIVE MODE;

        DO $preflight$
        DECLARE department_count integer;
        BEGIN
          IF (SELECT count(*) FROM __advance_schema__.companies WHERE "Id" IN ('70000000-0000-0000-0000-000000000001','70000000-0000-0000-0000-000000000002') AND "IsActive") <> 2 THEN
            RAISE EXCEPTION 'Both settled companies must exist and be active.';
          END IF;
          IF (SELECT count(*) FROM __advance_schema__.employees WHERE "EmployeeCode" BETWEEN 'SESS-01' AND 'SESS-42' AND upper("Status")='ACTIVE') <> 42 THEN
            RAISE EXCEPTION 'Expected exactly 42 active SESS-01 through SESS-42 employees.';
          END IF;
          IF EXISTS (
            SELECT 1 FROM __advance_schema__.roles
            WHERE upper(trim("Code")) IN ('PRODUCTION_MANAGER','ACCOUNTS_MANAGER')
              AND "Id" NOT IN ('83000000-0000-0000-0000-000000000001','83000000-0000-0000-0000-000000000002')
          ) THEN RAISE EXCEPTION 'A normalized role-code collision exists for a new manager role.'; END IF;
          IF (SELECT count(*) FROM __advance_schema__.employee_company_assignments
              WHERE "CompanyId"='70000000-0000-0000-0000-000000000001' AND "AssignmentType"='PAYROLL'
                AND "Status"='ACTIVE' AND "IsActive" AND ("EffectiveTo" IS NULL OR "EffectiveTo">=DATE '2026-08-26')) <> 42 THEN
            RAISE EXCEPTION 'Expected exactly 42 active SESS_PVT_LTD PAYROLL assignments.';
          END IF;
          SELECT count(*) INTO department_count FROM __advance_schema__.employee_department_assignments d
          WHERE d."CompanyId"='70000000-0000-0000-0000-000000000001' AND upper(d."Status")='ACTIVE' AND d."IsActive"
            AND d."EffectiveFrom"<=DATE '2026-08-26' AND (d."EffectiveTo" IS NULL OR d."EffectiveTo">=DATE '2026-08-26');
          IF department_count <> 199 THEN
            RAISE EXCEPTION 'Expected exactly 199 active SESS_PVT_LTD department assignments; found %.', department_count;
          END IF;
          IF EXISTS (SELECT 1 FROM __advance_schema__.employee_company_assignments
                     WHERE "CompanyId"='70000000-0000-0000-0000-000000000002' AND "Status"='ACTIVE' AND "IsActive") THEN
            RAISE EXCEPTION 'SESS_PROPRIETORSHIP already has active employee company assignments; refusing a partial replay.';
          END IF;
        END $preflight$;
        """;

    private const string UpPrefix = """
        CREATE TEMP TABLE _mceap1_roles(employee_code text NOT NULL, role_code text NOT NULL, PRIMARY KEY(employee_code,role_code)) ON COMMIT DROP;
        INSERT INTO _mceap1_roles VALUES
        """;

    private const string UpSuffix = """
        ;

        DO $manifest_guard$
        BEGIN
          IF (SELECT count(*) FROM _mceap1_roles)<>44 OR (SELECT count(DISTINCT employee_code) FROM _mceap1_roles)<>42 THEN
            RAISE EXCEPTION 'The employee-role manifest must contain 44 assignments for 42 employees.';
          END IF;
          IF EXISTS (SELECT 1 FROM _mceap1_roles m LEFT JOIN __advance_schema__.employees e ON e."EmployeeCode"=m.employee_code AND upper(e."Status")='ACTIVE' WHERE e."Id" IS NULL) THEN
            RAISE EXCEPTION 'Employee-role manifest references a missing employee.';
          END IF;
          IF EXISTS (SELECT 1 FROM _mceap1_roles m LEFT JOIN __advance_schema__.roles r ON r."Code"=m.role_code AND r."IsActive" WHERE r."Id" IS NULL) THEN
            RAISE EXCEPTION 'Employee-role manifest references a missing active role.';
          END IF;
        END $manifest_guard$;

        UPDATE __advance_schema__.employee_role_assignments a
        SET "EffectiveTo"=DATE '2026-08-25',"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',
            "UpdatedBy"='MULTI_COMPANY_EMPLOYEE_AUTH_PART1',"Version"=a."Version"+1
        FROM __advance_schema__.employees e, __advance_schema__.roles r
        WHERE a."EmployeeId"=e."Id" AND a."RoleId"=r."Id"
          AND a."CompanyId"='70000000-0000-0000-0000-000000000001'
          AND ((e."EmployeeCode"='SESS-14' AND r."Code"='ACCOUNTS_ASSISTANT')
            OR (e."EmployeeCode"='SESS-25' AND r."Code"='PRODUCTION_COORDINATOR'))
          AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=DATE '2026-08-26');

        INSERT INTO __advance_schema__.employee_role_assignments
          ("Id","CompanyId","EmployeeId","RoleId","EffectiveFrom","ApprovalStatus","Remarks","CreatedAt","CreatedBy","Version")
        SELECT md5('MCEAP1|PVT|ROLE|'||m.employee_code||'|'||m.role_code)::uuid,
               '70000000-0000-0000-0000-000000000001',e."Id",r."Id",DATE '2026-08-26','SeedApproved',
               'Settled Part 1 employee-role assignment',TIMESTAMPTZ '2026-08-25 00:00:00+00','MULTI_COMPANY_EMPLOYEE_AUTH_PART1',0
        FROM _mceap1_roles m JOIN __advance_schema__.employees e ON e."EmployeeCode"=m.employee_code
        JOIN __advance_schema__.roles r ON r."Code"=m.role_code
        WHERE NOT EXISTS (
          SELECT 1 FROM __advance_schema__.employee_role_assignments x
          WHERE x."CompanyId"='70000000-0000-0000-0000-000000000001' AND x."EmployeeId"=e."Id" AND x."RoleId"=r."Id"
            AND x."EffectiveFrom"<=DATE '2026-08-26' AND (x."EffectiveTo" IS NULL OR x."EffectiveTo">=DATE '2026-08-26'));

        INSERT INTO __advance_schema__.employee_company_assignments
          ("Id","CompanyId","EmployeeId","AssignmentType","EmployeeCode","EmploymentType","EffectiveFrom","Status","IsActive","CreatedAt","CreatedBy","Version")
        SELECT md5('MCEAP1|PROP|COMPANY|'||e."EmployeeCode")::uuid,'70000000-0000-0000-0000-000000000002',e."Id",'WORK',
               e."EmployeeCode",p."EmploymentType",DATE '2026-08-26','ACTIVE',true,TIMESTAMPTZ '2026-08-25 00:00:00+00','MULTI_COMPANY_EMPLOYEE_AUTH_PART1',0
        FROM __advance_schema__.employees e
        JOIN __advance_schema__.employee_company_assignments p ON p."EmployeeId"=e."Id"
          AND p."CompanyId"='70000000-0000-0000-0000-000000000001' AND p."AssignmentType"='PAYROLL' AND p."Status"='ACTIVE' AND p."IsActive"
        WHERE e."EmployeeCode" BETWEEN 'SESS-01' AND 'SESS-42' AND upper(e."Status")='ACTIVE';

        INSERT INTO __advance_schema__.employee_department_assignments
          ("Id","CompanyId","EmployeeCompanyAssignmentId","DepartmentId","DesignationId","AssignmentType","EffectiveFrom","IsPrimary","Status","IsActive","CreatedAt","CreatedBy","Version")
        SELECT md5('MCEAP1|PROP|DEPT|'||d."Id")::uuid,'70000000-0000-0000-0000-000000000002',w."Id",d."DepartmentId",d."DesignationId",
               d."AssignmentType",DATE '2026-08-26',d."IsPrimary",'ACTIVE',true,TIMESTAMPTZ '2026-08-25 00:00:00+00','MULTI_COMPANY_EMPLOYEE_AUTH_PART1',0
        FROM __advance_schema__.employee_department_assignments d
        JOIN __advance_schema__.employee_company_assignments p ON p."Id"=d."EmployeeCompanyAssignmentId"
        JOIN __advance_schema__.employee_company_assignments w ON w."EmployeeId"=p."EmployeeId"
          AND w."CompanyId"='70000000-0000-0000-0000-000000000002' AND w."AssignmentType"='WORK' AND w."IsActive"
        WHERE d."CompanyId"='70000000-0000-0000-0000-000000000001' AND upper(d."Status")='ACTIVE' AND d."IsActive"
          AND d."EffectiveFrom"<=DATE '2026-08-26' AND (d."EffectiveTo" IS NULL OR d."EffectiveTo">=DATE '2026-08-26');

        INSERT INTO __advance_schema__.employee_role_assignments
          ("Id","CompanyId","EmployeeId","RoleId","EffectiveFrom","ApprovalStatus","Remarks","CreatedAt","CreatedBy","Version")
        SELECT md5('MCEAP1|PROP|ROLE|'||a."Id")::uuid,'70000000-0000-0000-0000-000000000002',a."EmployeeId",a."RoleId",
               DATE '2026-08-26',a."ApprovalStatus",'Cloned active SESS_PVT_LTD role assignment',
               TIMESTAMPTZ '2026-08-25 00:00:00+00','MULTI_COMPANY_EMPLOYEE_AUTH_PART1',0
        FROM __advance_schema__.employee_role_assignments a
        JOIN __advance_schema__.employees e ON e."Id"=a."EmployeeId"
        WHERE a."CompanyId"='70000000-0000-0000-0000-000000000001'
          AND a."EffectiveFrom"<=DATE '2026-08-26' AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=DATE '2026-08-26')
          AND a."ApprovalStatus" IN ('SeedApproved','Approved') AND e."EmployeeCode" BETWEEN 'SESS-01' AND 'SESS-42';

        INSERT INTO __advance_schema__.employee_operational_scopes
          ("Id","CompanyId","OrganizationId","EmployeeId","DepartmentId","OwnRecordsOnly","AllowsPrivilegedCrossScope","EffectiveFrom","IsActive","Remarks","CreatedAt","CreatedBy","Version")
        SELECT md5('MCEAP1|PVT|SCOPE|'||d."Id")::uuid,'70000000-0000-0000-0000-000000000001','SESS_PVT_LTD',p."EmployeeId",d."DepartmentId",
               false,false,DATE '2026-08-26',true,'One scope per active department assignment',
               TIMESTAMPTZ '2026-08-25 00:00:00+00','MULTI_COMPANY_EMPLOYEE_AUTH_PART1',0
        FROM __advance_schema__.employee_department_assignments d
        JOIN __advance_schema__.employee_company_assignments p ON p."Id"=d."EmployeeCompanyAssignmentId"
        WHERE d."CompanyId"='70000000-0000-0000-0000-000000000001' AND upper(d."Status")='ACTIVE' AND d."IsActive"
          AND d."EffectiveFrom"<=DATE '2026-08-26' AND (d."EffectiveTo" IS NULL OR d."EffectiveTo">=DATE '2026-08-26')
          AND NOT EXISTS (
            SELECT 1 FROM __advance_schema__.employee_operational_scopes s
            WHERE s."CompanyId"=d."CompanyId" AND s."EmployeeId"=p."EmployeeId" AND s."DepartmentId"=d."DepartmentId"
              AND s."WarehouseId" IS NULL AND s."RackBinId" IS NULL AND NOT s."OwnRecordsOnly" AND s."IsActive"
              AND s."EffectiveFrom"<=DATE '2026-08-26' AND (s."EffectiveTo" IS NULL OR s."EffectiveTo">=DATE '2026-08-26'));

        INSERT INTO __advance_schema__.employee_operational_scopes
          ("Id","CompanyId","OrganizationId","EmployeeId","DepartmentId","OwnRecordsOnly","AllowsPrivilegedCrossScope","EffectiveFrom","IsActive","Remarks","CreatedAt","CreatedBy","Version")
        SELECT md5('MCEAP1|PROP|SCOPE|'||d."Id")::uuid,'70000000-0000-0000-0000-000000000002','SESS_PROPRIETORSHIP',w."EmployeeId",d."DepartmentId",
               false,false,DATE '2026-08-26',true,'One scope per active department assignment',
               TIMESTAMPTZ '2026-08-25 00:00:00+00','MULTI_COMPANY_EMPLOYEE_AUTH_PART1',0
        FROM __advance_schema__.employee_department_assignments d
        JOIN __advance_schema__.employee_company_assignments w ON w."Id"=d."EmployeeCompanyAssignmentId"
        WHERE d."CompanyId"='70000000-0000-0000-0000-000000000002' AND d."Status"='ACTIVE' AND d."IsActive";

        INSERT INTO __advance_schema__.employee_identity_mappings
          ("Id","CompanyId","OrganizationId","Issuer","Subject","EmployeeId","IdentityType","EffectiveFrom","EffectiveTo","IsActive","CreatedAt","CreatedBy","Version")
        SELECT md5('MCEAP1|PROP|IDENTITY|'||i."Id")::uuid,'70000000-0000-0000-0000-000000000002','SESS_PROPRIETORSHIP',
               i."Issuer",i."Subject",i."EmployeeId",i."IdentityType",i."EffectiveFrom",i."EffectiveTo",true,
               TIMESTAMPTZ '2026-08-25 00:00:00+00','MULTI_COMPANY_EMPLOYEE_AUTH_PART1',0
        FROM __advance_schema__.employee_identity_mappings i
        JOIN __advance_schema__.employee_company_assignments w ON w."EmployeeId"=i."EmployeeId"
          AND w."CompanyId"='70000000-0000-0000-0000-000000000002' AND w."Status"='ACTIVE' AND w."IsActive"
        WHERE i."CompanyId"='70000000-0000-0000-0000-000000000001' AND i."IsActive"
          AND i."EffectiveFrom"<=DATE '2026-08-26' AND (i."EffectiveTo" IS NULL OR i."EffectiveTo">=DATE '2026-08-26');

        DO $acceptance$
        BEGIN
          IF (SELECT count(*) FROM __advance_schema__.employee_company_assignments WHERE "CompanyId"='70000000-0000-0000-0000-000000000002' AND "AssignmentType"='WORK' AND "Status"='ACTIVE' AND "IsActive")<>42 THEN RAISE EXCEPTION 'Expected 42 active WORK assignments.'; END IF;
          IF (SELECT count(*) FROM __advance_schema__.employee_department_assignments WHERE "CompanyId"='70000000-0000-0000-0000-000000000002' AND "Status"='ACTIVE' AND "IsActive")<>199 THEN RAISE EXCEPTION 'Expected 199 active SESS_PROPRIETORSHIP department assignments.'; END IF;
          IF (SELECT count(*) FROM __advance_schema__.employee_operational_scopes WHERE "CompanyId"='70000000-0000-0000-0000-000000000001' AND "IsActive")<>199 OR
             (SELECT count(*) FROM __advance_schema__.employee_operational_scopes WHERE "CompanyId"='70000000-0000-0000-0000-000000000002' AND "IsActive")<>199 THEN RAISE EXCEPTION 'Expected 199 active operational scopes in each company.'; END IF;
          IF EXISTS (SELECT 1 FROM __advance_schema__.employee_operational_scopes WHERE "IsActive" AND "AllowsPrivilegedCrossScope") THEN RAISE EXCEPTION 'Privileged cross-scope must remain false.'; END IF;
          IF (SELECT count(*) FROM __advance_schema__.employee_role_assignments a JOIN __advance_schema__.employees e ON e."Id"=a."EmployeeId" WHERE a."CompanyId"='70000000-0000-0000-0000-000000000001' AND a."EffectiveFrom"<=DATE '2026-08-26' AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=DATE '2026-08-26') AND e."EmployeeCode" BETWEEN 'SESS-01' AND 'SESS-42')<>44 THEN RAISE EXCEPTION 'Expected 44 active role assignments in SESS_PVT_LTD.'; END IF;
          IF (SELECT count(*) FROM __advance_schema__.employee_role_assignments a JOIN __advance_schema__.employees e ON e."Id"=a."EmployeeId" WHERE a."CompanyId"='70000000-0000-0000-0000-000000000002' AND a."EffectiveFrom"<=DATE '2026-08-26' AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=DATE '2026-08-26') AND e."EmployeeCode" BETWEEN 'SESS-01' AND 'SESS-42')<>44 THEN RAISE EXCEPTION 'Expected 44 active role assignments in SESS_PROPRIETORSHIP.'; END IF;
          IF EXISTS (
            SELECT "EmployeeId","RoleId" FROM __advance_schema__.employee_role_assignments WHERE "CompanyId"='70000000-0000-0000-0000-000000000001' AND "EffectiveFrom"<=DATE '2026-08-26' AND ("EffectiveTo" IS NULL OR "EffectiveTo">=DATE '2026-08-26')
            EXCEPT SELECT "EmployeeId","RoleId" FROM __advance_schema__.employee_role_assignments WHERE "CompanyId"='70000000-0000-0000-0000-000000000002' AND "EffectiveFrom"<=DATE '2026-08-26' AND ("EffectiveTo" IS NULL OR "EffectiveTo">=DATE '2026-08-26')
          ) THEN RAISE EXCEPTION 'Role sets differ between companies.'; END IF;
          IF EXISTS (
            SELECT 1 FROM __advance_schema__.employee_identity_mappings p
            WHERE p."CompanyId"='70000000-0000-0000-0000-000000000001' AND p."IsActive"
              AND NOT EXISTS (SELECT 1 FROM __advance_schema__.employee_identity_mappings w WHERE w."CompanyId"='70000000-0000-0000-0000-000000000002' AND w."Issuer"=p."Issuer" AND w."Subject"=p."Subject" AND w."EmployeeId"=p."EmployeeId" AND w."IsActive")
          ) THEN RAISE EXCEPTION 'Every active PVT identity must have a WORK-company mapping.'; END IF;
        END $acceptance$;
        """;

    private const string DownSql = """
        DO $cluster_guard$
        BEGIN
          IF current_setting('server_version_num')::integer < 170000 THEN RAISE EXCEPTION 'Multi-company employee authorization Part 1 Down requires PostgreSQL 17 or later.'; END IF;
          IF current_database() IN ('postgres','template0','template1') THEN RAISE EXCEPTION 'Multi-company employee authorization Part 1 Down refuses a PostgreSQL administrative database.'; END IF;
          IF to_regnamespace('__advance_schema__') IS NULL THEN RAISE EXCEPTION 'Multi-company employee authorization Part 1 Down requires the advance schema.'; END IF;
          IF EXISTS (
            SELECT 1 FROM __advance_schema__.employee_identity_mappings WHERE "CreatedBy"='MULTI_COMPANY_EMPLOYEE_AUTH_PART1' AND ("UpdatedBy" IS NOT NULL OR "Version"<>0)
            UNION ALL SELECT 1 FROM __advance_schema__.employee_operational_scopes WHERE "CreatedBy"='MULTI_COMPANY_EMPLOYEE_AUTH_PART1' AND ("UpdatedBy" IS NOT NULL OR "Version"<>0)
            UNION ALL SELECT 1 FROM __advance_schema__.employee_department_assignments WHERE "CreatedBy"='MULTI_COMPANY_EMPLOYEE_AUTH_PART1' AND ("UpdatedBy" IS NOT NULL OR "Version"<>0)
            UNION ALL SELECT 1 FROM __advance_schema__.employee_role_assignments WHERE "CreatedBy"='MULTI_COMPANY_EMPLOYEE_AUTH_PART1' AND ("UpdatedBy" IS NOT NULL OR "Version"<>0)
            UNION ALL SELECT 1 FROM __advance_schema__.employee_company_assignments WHERE "CreatedBy"='MULTI_COMPANY_EMPLOYEE_AUTH_PART1' AND ("UpdatedBy" IS NOT NULL OR "Version"<>0)
          ) THEN RAISE EXCEPTION 'Part 1 rows were changed after migration; refusing destructive Down.'; END IF;
        END $cluster_guard$;

        ALTER TABLE __advance_schema__.employee_identity_mappings DISABLE TRIGGER trg_rev869a_identity_version_guard;
        ALTER TABLE __advance_schema__.employee_operational_scopes DISABLE TRIGGER trg_rev869a_scope_version_guard;
        DELETE FROM __advance_schema__.employee_identity_mappings WHERE "CreatedBy"='MULTI_COMPANY_EMPLOYEE_AUTH_PART1';
        DELETE FROM __advance_schema__.employee_operational_scopes WHERE "CreatedBy"='MULTI_COMPANY_EMPLOYEE_AUTH_PART1';
        ALTER TABLE __advance_schema__.employee_identity_mappings ENABLE TRIGGER trg_rev869a_identity_version_guard;
        ALTER TABLE __advance_schema__.employee_operational_scopes ENABLE TRIGGER trg_rev869a_scope_version_guard;
        DELETE FROM __advance_schema__.employee_role_assignments WHERE "CreatedBy"='MULTI_COMPANY_EMPLOYEE_AUTH_PART1';
        DELETE FROM __advance_schema__.employee_department_assignments WHERE "CreatedBy"='MULTI_COMPANY_EMPLOYEE_AUTH_PART1';
        DELETE FROM __advance_schema__.employee_company_assignments WHERE "CreatedBy"='MULTI_COMPANY_EMPLOYEE_AUTH_PART1';
        UPDATE __advance_schema__.employee_role_assignments
        SET "EffectiveTo"=NULL,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='MULTI_COMPANY_EMPLOYEE_AUTH_PART1_DOWN',"Version"="Version"+1
        WHERE "UpdatedBy"='MULTI_COMPANY_EMPLOYEE_AUTH_PART1' AND "EffectiveTo"=DATE '2026-08-25';
        """;
}
