namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class AuthenticationBootstrapCeremonySql
{
    internal static string PreUp => AdvanceSchemaSql.Expand(PreUpSql);
    internal static string Up => AdvanceSchemaSql.Expand(UpSql);
    internal static string Down => AdvanceSchemaSql.Expand(DownSql);

    private const string PreUpSql = """
        DO $guard$
        BEGIN
          IF (SELECT count(*) FROM __advance_schema__.authentication_bootstrap_state
              WHERE "Id"='81000000-0000-0000-0000-000000000001'::uuid AND "Status"='PENDING'
                AND "EmployeeId" IS NULL AND "CompanyId" IS NULL AND "OrganizationId" IS NULL
                AND "IssuerSha256" IS NULL AND "SubjectSha256" IS NULL)<>1 THEN
            RAISE EXCEPTION 'Authentication ceremony migration requires the unused pending bootstrap singleton.';
          END IF;
        END $guard$;
        """;

    private const string UpSql = """
        CREATE FUNCTION __advance_schema__.complete_authentication_bootstrap(p_issuer text,p_subject text)
        RETURNS jsonb
        LANGUAGE plpgsql
        SECURITY DEFINER
        SET search_path=pg_catalog,__advance_schema__
        AS $function$
        DECLARE
          v_state __advance_schema__.authentication_bootstrap_state%ROWTYPE;
          v_employee __advance_schema__.employees%ROWTYPE;
          v_role __advance_schema__.roles%ROWTYPE;
          v_company_count integer;
          v_company_ids text;
          v_company_codes jsonb;
          v_primary_company uuid;
          v_primary_organization text;
          v_issuer text;
          v_subject text;
          v_now timestamptz := clock_timestamp();
        BEGIN
          IF session_user<>'nexa_erp_bootstrap' THEN RAISE EXCEPTION 'Authentication bootstrap requires the dedicated nexa_erp_bootstrap login.'; END IF;
          IF current_database() IN ('postgres','template0','template1') THEN RAISE EXCEPTION 'Authentication bootstrap refuses a PostgreSQL administrative database.'; END IF;
          PERFORM pg_advisory_xact_lock(hashtextextended('NEXAERP_AUTHENTICATION_BOOTSTRAP_V1',0));
          SELECT * INTO STRICT v_state FROM __advance_schema__.authentication_bootstrap_state
            WHERE "Id"='81000000-0000-0000-0000-000000000001'::uuid FOR UPDATE;
          IF v_state."Status"<>'PENDING' THEN RAISE EXCEPTION 'Authentication bootstrap has already been completed and cannot be replayed.'; END IF;

          v_issuer := rtrim(btrim(p_issuer),'/');
          v_subject := btrim(p_subject);
          IF v_issuer='' OR v_subject='' OR length(v_issuer)>500 OR length(v_subject)>500 OR v_issuer !~ '^https://[^[:space:]]+$' THEN
            RAISE EXCEPTION 'Issuer must be an absolute HTTPS OIDC issuer and subject must be non-empty; each is limited to 500 characters.';
          END IF;

          SELECT * INTO STRICT v_employee FROM __advance_schema__.employees
            WHERE "EmployeeCode"='SESS-12' AND "EmployeeName"='SURANTHER P' AND upper("Status")='ACTIVE';
          SELECT * INTO STRICT v_role FROM __advance_schema__.roles WHERE "Code"='IT_MANAGER' AND "IsActive";

          WITH assigned AS (
            SELECT DISTINCT c."Id",c."Code"
            FROM __advance_schema__.employee_company_assignments a
            JOIN __advance_schema__.companies c ON c."Id"=a."CompanyId" AND c."IsActive" AND c."Status"='ACTIVE'
            WHERE a."EmployeeId"=v_employee."Id" AND a."IsActive" AND a."Status"='ACTIVE'
              AND a."EffectiveFrom"<=current_date AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=current_date)
          )
          SELECT count(*),string_agg("Id"::text,',' ORDER BY "Id"::text),jsonb_agg("Code" ORDER BY "Code"),
                 (array_agg("Id" ORDER BY "Code"))[1],(array_agg("Code" ORDER BY "Code"))[1]
            INTO v_company_count,v_company_ids,v_company_codes,v_primary_company,v_primary_organization FROM assigned;
          IF v_company_count<>2 THEN RAISE EXCEPTION 'SESS-12 bootstrap requires exactly the two active company assignments.'; END IF;

          IF EXISTS (
            SELECT 1 FROM __advance_schema__.employee_company_assignments a
            JOIN __advance_schema__.companies c ON c."Id"=a."CompanyId" AND c."IsActive" AND c."Status"='ACTIVE'
            WHERE a."EmployeeId"=v_employee."Id" AND a."IsActive" AND a."Status"='ACTIVE'
              AND a."EffectiveFrom"<=current_date AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=current_date)
            GROUP BY a."CompanyId" HAVING count(*)<>1
          ) THEN RAISE EXCEPTION 'SESS-12 must have exactly one effective employee assignment in each company.'; END IF;
          IF EXISTS (
            SELECT 1 FROM __advance_schema__.employee_company_assignments a
            WHERE a."EmployeeId"=v_employee."Id" AND a."IsActive" AND a."Status"='ACTIVE'
              AND a."EffectiveFrom"<=current_date AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=current_date)
              AND (SELECT count(*) FROM __advance_schema__.employee_department_assignments d
                   WHERE d."CompanyId"=a."CompanyId" AND d."EmployeeCompanyAssignmentId"=a."Id" AND d."IsActive" AND d."Status"='ACTIVE' AND d."IsPrimary"
                     AND d."EffectiveFrom"<=current_date AND (d."EffectiveTo" IS NULL OR d."EffectiveTo">=current_date))<>1
          ) THEN RAISE EXCEPTION 'SESS-12 must have exactly one effective primary department in each company.'; END IF;
          IF EXISTS (
            SELECT 1 FROM __advance_schema__.employee_company_assignments a
            JOIN __advance_schema__.employee_department_assignments d ON d."CompanyId"=a."CompanyId" AND d."EmployeeCompanyAssignmentId"=a."Id"
            WHERE a."EmployeeId"=v_employee."Id" AND a."IsActive" AND a."Status"='ACTIVE'
              AND d."IsActive" AND d."Status"='ACTIVE' AND d."EffectiveFrom"<=current_date AND (d."EffectiveTo" IS NULL OR d."EffectiveTo">=current_date)
              AND NOT EXISTS (SELECT 1 FROM __advance_schema__.employee_operational_scopes s
                WHERE s."CompanyId"=a."CompanyId" AND s."OrganizationId"=(SELECT c."Code" FROM __advance_schema__.companies c WHERE c."Id"=a."CompanyId")
                  AND s."EmployeeId"=v_employee."Id" AND s."DepartmentId"=d."DepartmentId" AND s."WarehouseId" IS NULL AND s."RackBinId" IS NULL
                  AND NOT s."OwnRecordsOnly" AND NOT s."AllowsPrivilegedCrossScope" AND s."IsActive"
                  AND s."EffectiveFrom"<=current_date AND (s."EffectiveTo" IS NULL OR s."EffectiveTo">=current_date))
          ) THEN RAISE EXCEPTION 'SESS-12 operational scopes are incomplete or use the wrong CompanyId.'; END IF;

          IF EXISTS (SELECT 1 FROM __advance_schema__.employee_identity_mappings WHERE "EmployeeId"=v_employee."Id" AND "IsActive")
             OR EXISTS (SELECT 1 FROM __advance_schema__.employee_identity_mappings WHERE "Issuer"=v_issuer AND "Subject"=v_subject AND "IsActive") THEN
            RAISE EXCEPTION 'Authentication bootstrap refuses pre-existing or partial identity mappings.';
          END IF;
          IF EXISTS (SELECT 1 FROM __advance_schema__.employee_role_assignments
                     WHERE "EmployeeId"=v_employee."Id" AND "RoleId"=v_role."Id"
                       AND "EffectiveFrom"<=current_date AND ("EffectiveTo" IS NULL OR "EffectiveTo">=current_date)) THEN
            RAISE EXCEPTION 'Authentication bootstrap refuses a pre-existing or partial IT_MANAGER assignment.';
          END IF;

          UPDATE __advance_schema__.employees SET "LoginEnabled"=true,"UpdatedAt"=v_now,
            "UpdatedBy"='AUTHENTICATION_BOOTSTRAP_INSTALLER',"Version"="Version"+1 WHERE "Id"=v_employee."Id";

          INSERT INTO __advance_schema__.employee_identity_mappings
            ("Id","CompanyId","OrganizationId","Issuer","Subject","EmployeeId","IdentityType","EffectiveFrom","IsActive","CreatedAt","CreatedBy","Version")
          SELECT gen_random_uuid(),c."Id",c."Code",v_issuer,v_subject,v_employee."Id",'HUMAN',current_date,true,v_now,'AUTHENTICATION_BOOTSTRAP_INSTALLER',0
          FROM __advance_schema__.companies c
          JOIN __advance_schema__.employee_company_assignments a ON a."CompanyId"=c."Id" AND a."EmployeeId"=v_employee."Id"
          WHERE c."IsActive" AND c."Status"='ACTIVE' AND a."IsActive" AND a."Status"='ACTIVE'
            AND a."EffectiveFrom"<=current_date AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=current_date);

          INSERT INTO __advance_schema__.employee_role_assignments
            ("Id","CompanyId","EmployeeId","RoleId","EffectiveFrom","ApprovalStatus","Remarks","CreatedAt","CreatedBy","Version")
          SELECT gen_random_uuid(),c."Id",v_employee."Id",v_role."Id",current_date,'SeedApproved',
                 'One-time first administrator bootstrap',v_now,'AUTHENTICATION_BOOTSTRAP_INSTALLER',0
          FROM __advance_schema__.companies c
          JOIN __advance_schema__.employee_company_assignments a ON a."CompanyId"=c."Id" AND a."EmployeeId"=v_employee."Id"
          WHERE c."IsActive" AND c."Status"='ACTIVE' AND a."IsActive" AND a."Status"='ACTIVE'
            AND a."EffectiveFrom"<=current_date AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=current_date);

          INSERT INTO __advance_schema__.audit_logs
            ("Id","CompanyId","Scope","Module","Action","EntityName","EntityId","UserLoginId","Result","CorrelationId","CreatedAt","CreatedBy","Version","AfterJson")
          SELECT gen_random_uuid(),c."Id",'COMPANY','Security','AuthenticationBootstrap','EmployeeIdentityMapping',v_employee."Id"::text,
                 'nexa_erp_bootstrap','Success','AUTH_BOOTSTRAP_'||replace(c."Id"::text,'-',''),v_now,'AUTHENTICATION_BOOTSTRAP_INSTALLER',0,
                 jsonb_build_object('employeeCode','SESS-12','roleCode','IT_MANAGER','organizationId',c."Code")::text
          FROM __advance_schema__.companies c
          JOIN __advance_schema__.employee_company_assignments a ON a."CompanyId"=c."Id" AND a."EmployeeId"=v_employee."Id"
          WHERE c."IsActive" AND c."Status"='ACTIVE' AND a."IsActive" AND a."Status"='ACTIVE'
            AND a."EffectiveFrom"<=current_date AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=current_date);

          UPDATE __advance_schema__.authentication_bootstrap_state SET
            "Status"='COMPLETED',"EmployeeId"=v_employee."Id","CompanyId"=v_primary_company,"OrganizationId"=v_primary_organization,
            "IssuerSha256"=sha256(convert_to(v_issuer,'UTF8')),"SubjectSha256"=sha256(convert_to(v_subject,'UTF8')),
            "CompanyCount"=v_company_count,"CompanySetSha256"=sha256(convert_to(v_company_ids,'UTF8')),
            "CompletedAt"=v_now,"CompletedBy"='nexa_erp_bootstrap',"UpdatedAt"=v_now,"UpdatedBy"='AUTHENTICATION_BOOTSTRAP_INSTALLER',"Version"="Version"+1
          WHERE "Id"=v_state."Id" AND "Version"=v_state."Version";
          IF NOT FOUND THEN RAISE EXCEPTION 'Authentication bootstrap state changed concurrently.'; END IF;

          RETURN jsonb_build_object('status','COMPLETED','employeeCode','SESS-12','employeeName','SURANTHER P','roleCode','IT_MANAGER',
            'companyCount',v_company_count,'companies',v_company_codes,'identityMappingCount',v_company_count,'roleAssignmentCount',v_company_count,
            'loginEnabled',true,'operationalScopes','VERIFIED_EXISTING_COMPANY_SCOPES');
        END $function$;

        REVOKE ALL ON FUNCTION __advance_schema__.complete_authentication_bootstrap(text,text) FROM PUBLIC;
        DO $principal_acl$
        DECLARE
          v_existing_count integer;
          v_missing_roles text;
        BEGIN
          WITH managed("RoleName") AS (VALUES
            ('nexa_erp_owner'),('nexa_erp_migration'),('nexa_erp_bootstrap'),('nexa_erp_runtime'))
          SELECT count(r.rolname),string_agg(m."RoleName",', ' ORDER BY m."RoleName") FILTER (WHERE r.rolname IS NULL)
            INTO v_existing_count,v_missing_roles
          FROM managed m LEFT JOIN pg_catalog.pg_roles r ON r.rolname=m."RoleName";

          IF v_existing_count NOT IN (0,4) THEN
            RAISE EXCEPTION 'Partial NexaERP principal state; missing managed roles: %.',v_missing_roles;
          END IF;
          IF v_existing_count=4 THEN
            EXECUTE 'REVOKE ALL ON FUNCTION __advance_schema__.complete_authentication_bootstrap(text,text) FROM nexa_erp_runtime';
            EXECUTE 'REVOKE ALL ON FUNCTION __advance_schema__.complete_authentication_bootstrap(text,text) FROM nexa_erp_migration';
            EXECUTE 'GRANT EXECUTE ON FUNCTION __advance_schema__.complete_authentication_bootstrap(text,text) TO nexa_erp_bootstrap';
          END IF;
        END $principal_acl$;
        """;

    private const string DownSql = """
        DO $guard$
        BEGIN
          IF (SELECT count(*) FROM __advance_schema__.authentication_bootstrap_state
              WHERE "Id"='81000000-0000-0000-0000-000000000001'::uuid AND "Status"='PENDING'
                AND "CompanyCount" IS NULL AND "CompanySetSha256" IS NULL)<>1 THEN
            RAISE EXCEPTION 'Authentication ceremony rollback refuses a consumed bootstrap.';
          END IF;
          IF EXISTS (SELECT 1 FROM __advance_schema__.employee_identity_mappings WHERE "CreatedBy"='AUTHENTICATION_BOOTSTRAP_INSTALLER')
             OR EXISTS (SELECT 1 FROM __advance_schema__.employee_role_assignments WHERE "CreatedBy"='AUTHENTICATION_BOOTSTRAP_INSTALLER') THEN
            RAISE EXCEPTION 'Authentication ceremony rollback refuses installer-created authorization rows.';
          END IF;
        END $guard$;
        DROP FUNCTION IF EXISTS __advance_schema__.complete_authentication_bootstrap(text,text);
        """;
}
