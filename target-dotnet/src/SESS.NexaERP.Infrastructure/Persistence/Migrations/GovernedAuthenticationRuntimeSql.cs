namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class GovernedAuthenticationRuntimeSql
{
    internal const string Up = """
        DO $guard$
        BEGIN
          IF current_setting('server_version_num')::integer < 170000
             OR current_database() IN ('postgres','template0','template1')
             OR to_regnamespace('advance') IS NULL THEN
            RAISE EXCEPTION 'Governed authentication requires a PostgreSQL 17 application database with advance schema.';
          END IF;
          IF (SELECT count(*) FROM advance.role_page_permissions p
              JOIN advance.roles r ON r."Id"=p."RoleId"
              JOIN advance.page_definitions d ON d."Id"=p."PageDefinitionId"
              WHERE r."Code"='IT_MANAGER' AND d."PageKey"='security.employee-identities'
                AND NOT p."CanDeactivate" AND NOT p."HasFullControl")<>1 THEN
            RAISE EXCEPTION 'Identity revocation permission requires the reviewed IT_MANAGER baseline.';
          END IF;
        END $guard$;
        UPDATE advance.role_page_permissions p
           SET "CanDeactivate"=true,"Version"=p."Version"+1,"UpdatedAt"=clock_timestamp(),
               "UpdatedBy"='GovernedAuthenticationRuntime'
          FROM advance.roles r,advance.page_definitions d
         WHERE p."RoleId"=r."Id" AND p."PageDefinitionId"=d."Id"
           AND r."Code"='IT_MANAGER' AND d."PageKey"='security.employee-identities';

        CREATE FUNCTION advance.govern_authentication_bootstrap(
          p_issuer text,p_subject text,p_rerun boolean,p_retire_development boolean)
        RETURNS jsonb LANGUAGE plpgsql SECURITY DEFINER
        SET search_path=pg_catalog,advance
        AS $function$
        DECLARE
          v_state advance.authentication_bootstrap_state%ROWTYPE;
          v_mapping advance.employee_identity_mappings%ROWTYPE;
          v_employee uuid;
          v_issuer text := rtrim(btrim(p_issuer),'/');
          v_subject text := btrim(p_subject);
          v_company_ids text;
          v_retired_count integer := 0;
        BEGIN
          IF current_setting('server_version_num')::integer < 170000
             OR current_database() IN ('postgres','template0','template1')
             OR to_regnamespace('advance') IS NULL THEN
            RAISE EXCEPTION 'Governed authentication requires a PostgreSQL 17 application database with advance schema.';
          END IF;
          IF session_user<>'nexa_erp_bootstrap' THEN
            RAISE EXCEPTION 'Authentication bootstrap requires the dedicated nexa_erp_bootstrap login.';
          END IF;
          IF current_setting('server_version_num')::integer < 170000
             OR current_database() IN ('postgres','template0','template1') THEN
            RAISE EXCEPTION 'Authentication bootstrap refuses this cluster or administrative database.';
          END IF;
          IF v_issuer IS NULL OR v_subject IS NULL OR v_issuer !~ '^https://[^[:space:]?#]+$'
             OR v_subject='' OR length(v_issuer)>500 OR length(v_subject)>500 THEN
            RAISE EXCEPTION 'A valid HTTPS issuer and stable subject are required.';
          END IF;
          PERFORM pg_advisory_xact_lock(hashtextextended('NEXAERP_AUTHENTICATION_BOOTSTRAP_V1',0));
          SELECT * INTO STRICT v_state FROM advance.authentication_bootstrap_state
            WHERE "Id"='81000000-0000-0000-0000-000000000001' FOR UPDATE;
          SELECT "Id" INTO STRICT v_employee FROM advance.employees
            WHERE "EmployeeCode"='SESS-12' AND "EmployeeName"='SURANTHER P' AND upper("Status")='ACTIVE'
            FOR UPDATE;
          IF EXISTS (SELECT 1 FROM advance.employee_identity_mappings
                     WHERE "Issuer"=v_issuer AND "Subject"=v_subject AND "EmployeeId"<>v_employee) THEN
            RAISE EXCEPTION 'Issuer and subject already identify another employee.';
          END IF;
          IF EXISTS (
            SELECT 1 FROM advance.employee_company_assignments a
            JOIN advance.companies c ON c."Id"=a."CompanyId" AND c."IsActive" AND c."Status"='ACTIVE'
            WHERE a."EmployeeId"=v_employee AND a."IsActive" AND a."Status"='ACTIVE'
              AND a."EffectiveFrom"<=current_date AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=current_date)
            GROUP BY a."CompanyId" HAVING count(*)<>1
          ) THEN RAISE EXCEPTION 'SESS-12 must have exactly one effective employee assignment in each company.'; END IF;
          IF EXISTS (
            SELECT 1 FROM advance.employee_company_assignments a
            WHERE a."EmployeeId"=v_employee AND a."IsActive" AND a."Status"='ACTIVE'
              AND a."EffectiveFrom"<=current_date AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=current_date)
              AND (SELECT count(*) FROM advance.employee_department_assignments d
                   WHERE d."CompanyId"=a."CompanyId" AND d."EmployeeCompanyAssignmentId"=a."Id" AND d."IsActive" AND d."Status"='ACTIVE' AND d."IsPrimary"
                     AND d."EffectiveFrom"<=current_date AND (d."EffectiveTo" IS NULL OR d."EffectiveTo">=current_date))<>1
          ) THEN RAISE EXCEPTION 'SESS-12 must have exactly one effective primary department in each company.'; END IF;
          IF EXISTS (
            SELECT 1 FROM advance.employee_company_assignments a
            JOIN advance.employee_department_assignments d ON d."CompanyId"=a."CompanyId" AND d."EmployeeCompanyAssignmentId"=a."Id"
            WHERE a."EmployeeId"=v_employee AND a."IsActive" AND a."Status"='ACTIVE'
              AND d."IsActive" AND d."Status"='ACTIVE' AND d."EffectiveFrom"<=current_date AND (d."EffectiveTo" IS NULL OR d."EffectiveTo">=current_date)
              AND NOT EXISTS (SELECT 1 FROM advance.employee_operational_scopes s
                WHERE s."CompanyId"=a."CompanyId" AND s."OrganizationId"=(SELECT c."Code" FROM advance.companies c WHERE c."Id"=a."CompanyId")
                  AND s."EmployeeId"=v_employee AND s."DepartmentId"=d."DepartmentId" AND s."WarehouseId" IS NULL AND s."RackBinId" IS NULL
                  AND NOT s."OwnRecordsOnly" AND NOT s."AllowsPrivilegedCrossScope" AND s."IsActive"
                  AND s."EffectiveFrom"<=current_date AND (s."EffectiveTo" IS NULL OR s."EffectiveTo">=current_date))
          ) THEN RAISE EXCEPTION 'SESS-12 operational scopes are incomplete or use the wrong CompanyId.'; END IF;

          IF v_state."Status"='COMPLETED' THEN
            IF NOT p_rerun THEN RAISE EXCEPTION 'Authentication bootstrap already completed; explicit --rerun is required to verify it.'; END IF;
            IF p_retire_development THEN RAISE EXCEPTION 'Completed bootstrap rerun cannot retire or change identities.'; END IF;
            IF v_state."IssuerSha256"<>sha256(convert_to(v_issuer,'UTF8'))
               OR v_state."SubjectSha256"<>sha256(convert_to(v_subject,'UTF8'))
               OR v_state."EmployeeId"<>v_employee THEN
              RAISE EXCEPTION 'Rerun identity differs from the completed ceremony.';
            END IF;
            SELECT string_agg("CompanyId"::text,',' ORDER BY "CompanyId"::text) INTO v_company_ids
              FROM advance.employee_identity_mappings
             WHERE "EmployeeId"=v_employee AND "Issuer"=v_issuer AND "Subject"=v_subject
               AND "IsActive" AND "EffectiveFrom"<=current_date AND ("EffectiveTo" IS NULL OR "EffectiveTo">=current_date);
            IF (SELECT count(*) FROM advance.employee_identity_mappings
                WHERE "EmployeeId"=v_employee AND "Issuer"=v_issuer AND "Subject"=v_subject
                  AND "IsActive" AND "EffectiveFrom"<=current_date AND ("EffectiveTo" IS NULL OR "EffectiveTo">=current_date))<>v_state."CompanyCount"
               OR sha256(convert_to(coalesce(v_company_ids,''),'UTF8'))<>v_state."CompanySetSha256"
               OR NOT EXISTS (SELECT 1 FROM advance.employees WHERE "Id"=v_employee AND "LoginEnabled")
               OR (SELECT count(DISTINCT a."CompanyId") FROM advance.employee_role_assignments a
                   JOIN advance.roles r ON r."Id"=a."RoleId" AND r."Code"='IT_MANAGER' AND r."IsActive"
                   WHERE a."EmployeeId"=v_employee AND a."ApprovalStatus" IN ('Approved','SeedApproved')
                     AND EXISTS (SELECT 1 FROM advance.company_role_activations ca
                         WHERE ca."CompanyId"=a."CompanyId" AND ca."RoleId"=a."RoleId" AND ca."IsEnabled"
                           AND ca."EffectiveFrom"<=current_date AND (ca."EffectiveTo" IS NULL OR ca."EffectiveTo">=current_date))
                     AND EXISTS (SELECT 1 FROM advance.employee_company_assignments ec
                         JOIN advance.companies c ON c."Id"=ec."CompanyId" AND c."IsActive" AND c."Status"='ACTIVE'
                         WHERE ec."EmployeeId"=a."EmployeeId" AND ec."CompanyId"=a."CompanyId"
                           AND ec."IsActive" AND ec."Status"='ACTIVE' AND ec."EffectiveFrom"<=current_date
                           AND (ec."EffectiveTo" IS NULL OR ec."EffectiveTo">=current_date))
                     AND a."EffectiveFrom"<=current_date AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=current_date))<>v_state."CompanyCount" THEN
              RAISE EXCEPTION 'Completed bootstrap access has changed; use governed administration, not rerun repair.';
            END IF;
            RETURN jsonb_build_object('status','VERIFIED','employeeCode','SESS-12',
              'companyCount',v_state."CompanyCount",'identityMappingCount',v_state."CompanyCount",'mutations',0);
          END IF;
          IF v_state."Status"<>'PENDING' OR p_rerun THEN
            RAISE EXCEPTION 'Rerun requires an already completed ceremony.';
          END IF;
          IF p_retire_development THEN
            FOR v_mapping IN SELECT * FROM advance.employee_identity_mappings
                WHERE "Issuer"='urn:nexaerp:development'
                  AND "IdentityType"='HUMAN' AND "IsActive" ORDER BY "Id" FOR UPDATE
            LOOP
              UPDATE advance.employee_identity_mappings
                 SET "IsActive"=false,"EffectiveTo"=greatest(current_date,"EffectiveFrom"),
                     "Version"="Version"+1,"UpdatedAt"=clock_timestamp(),
                     "UpdatedBy"='AUTHENTICATION_BOOTSTRAP_INSTALLER'
               WHERE "Id"=v_mapping."Id";
              INSERT INTO advance.audit_logs
                ("Id","CompanyId","Scope","Module","Action","EntityName","EntityId","UserLoginId",
                 "ActorRoleCode","Result","CorrelationId","CreatedAt","CreatedBy","Version","AfterJson")
              VALUES (gen_random_uuid(),v_mapping."CompanyId",'COMPANY','Security','DevelopmentIdentityRetired',
                'EmployeeIdentityMapping',v_mapping."Id"::text,'nexa_erp_bootstrap','IT_MANAGER','Success',
                'AUTH_RETIRE_'||v_mapping."Id"::text,clock_timestamp(),'AUTHENTICATION_BOOTSTRAP_INSTALLER',0,
                jsonb_build_object('employeeId',v_mapping."EmployeeId",'isActive',false,'reason','Explicit production bootstrap transition')::text);
              v_retired_count := v_retired_count + 1;
            END LOOP;
          END IF;
          RETURN advance.complete_authentication_bootstrap(v_issuer,v_subject)
            || jsonb_build_object('developmentMappingsRetired',v_retired_count);
        END $function$;
        REVOKE ALL ON FUNCTION advance.govern_authentication_bootstrap(text,text,boolean,boolean) FROM PUBLIC;
        DO $acl$
        BEGIN
          IF current_setting('server_version_num')::integer < 170000
             OR current_database() IN ('postgres','template0','template1')
             OR to_regnamespace('advance') IS NULL THEN
            RAISE EXCEPTION 'Governed authentication requires a PostgreSQL 17 application database with advance schema.';
          END IF;
          IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_bootstrap') THEN
            GRANT EXECUTE ON FUNCTION advance.govern_authentication_bootstrap(text,text,boolean,boolean) TO nexa_erp_bootstrap;
          END IF;
        END $acl$;
        """;

    internal const string Down = """
        DO $guard$
        BEGIN
          IF current_setting('server_version_num')::integer < 170000
             OR current_database() IN ('postgres','template0','template1')
             OR to_regnamespace('advance') IS NULL THEN
            RAISE EXCEPTION 'Governed authentication requires a PostgreSQL 17 application database with advance schema.';
          END IF;
          IF EXISTS(SELECT 1 FROM advance.audit_logs
                    WHERE "Action" IN ('DevelopmentIdentityRetired','RevokeIdentityMapping')) THEN
            RAISE EXCEPTION 'Authentication rollback refuses governed identity history.';
          END IF;
          IF (SELECT count(*) FROM advance.role_page_permissions p
              JOIN advance.roles r ON r."Id"=p."RoleId"
              JOIN advance.page_definitions d ON d."Id"=p."PageDefinitionId"
              WHERE r."Code"='IT_MANAGER' AND d."PageKey"='security.employee-identities'
                AND p."CanDeactivate" AND p."UpdatedBy"='GovernedAuthenticationRuntime')<>1 THEN
            RAISE EXCEPTION 'Authentication rollback refuses changed identity permissions.';
          END IF;
        END $guard$;
        DROP FUNCTION advance.govern_authentication_bootstrap(text,text,boolean,boolean);
        UPDATE advance.role_page_permissions p
           SET "CanDeactivate"=false,"Version"=p."Version"+1,"UpdatedAt"=clock_timestamp(),
               "UpdatedBy"='GovernedAuthenticationRuntimeRollback'
          FROM advance.roles r,advance.page_definitions d
         WHERE p."RoleId"=r."Id" AND p."PageDefinitionId"=d."Id"
           AND r."Code"='IT_MANAGER' AND d."PageKey"='security.employee-identities';
        """;
}
