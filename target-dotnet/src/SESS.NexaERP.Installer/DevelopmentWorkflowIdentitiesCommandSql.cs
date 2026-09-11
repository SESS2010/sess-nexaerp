#if DEBUG
internal static class DevelopmentWorkflowIdentitiesCommandSql
{
    internal const string ClusterGuard = """
        SELECT current_setting('server_version_num')::integer,
               current_database(),
               to_regnamespace('advance') IS NOT NULL
                 AND to_regclass('advance.employee_identity_mappings') IS NOT NULL,
               session_user,
               current_user,
               role.rolsuper,
               pg_catalog.pg_get_userbyid(database.datdba)=session_user,
               pg_catalog.pg_get_userbyid(namespace.nspowner)=session_user,
               (SELECT count(*) FROM pg_catalog.pg_roles
                 WHERE rolname IN ('nexa_erp_owner','nexa_erp_migration','nexa_erp_bootstrap','nexa_erp_runtime')),
               pg_catalog.pg_get_userbyid(database.datdba)='nexa_erp_owner',
               pg_catalog.pg_get_userbyid(namespace.nspowner)='nexa_erp_owner'
        FROM pg_catalog.pg_roles role
        JOIN pg_catalog.pg_database database ON database.datname=current_database()
        JOIN pg_catalog.pg_namespace namespace ON namespace.nspname='advance'
        WHERE role.rolname=session_user;
        """;

    internal const string Provision = """
        DO $lock$ BEGIN
          PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended('NEXAERP_WORKFLOW_IDENTITIES_DEVELOPMENT_V1',0));
        END $lock$;
        DO $provision$
        DECLARE
          expected_codes constant text[] := ARRAY[
            'SESS-01','SESS-02','SESS-04','SESS-12','SESS-14','SESS-15',
            'SESS-16','SESS-25','SESS-33','SESS-35','SESS-41'];
          issuer constant text := 'urn:nexaerp:development';
          expected_employees integer;
          expected_assignments integer;
          managed_count integer;
        BEGIN
          SELECT count(*) INTO managed_count FROM pg_catalog.pg_roles
           WHERE rolname IN ('nexa_erp_owner','nexa_erp_migration','nexa_erp_bootstrap','nexa_erp_runtime');
          IF managed_count NOT IN (0,4) THEN
            RAISE EXCEPTION 'Development workflow identities refuse partial managed-principal state (%/4 roles).',managed_count;
          END IF;
          IF managed_count=4 AND (
               (SELECT rolcanlogin FROM pg_catalog.pg_roles WHERE rolname='nexa_erp_owner')
               OR EXISTS (SELECT 1 FROM pg_catalog.pg_roles
                           WHERE rolname IN ('nexa_erp_migration','nexa_erp_bootstrap','nexa_erp_runtime') AND NOT rolcanlogin)
               OR EXISTS (SELECT 1 FROM pg_catalog.pg_roles
                           WHERE rolname IN ('nexa_erp_owner','nexa_erp_migration','nexa_erp_bootstrap','nexa_erp_runtime')
                             AND (rolsuper OR rolcreatedb OR rolcreaterole OR rolreplication OR rolbypassrls))) THEN
            RAISE EXCEPTION 'Development workflow identities refuse a drifted managed-principal contract.';
          END IF;

          SELECT count(*) INTO expected_employees
          FROM advance.employees
          WHERE "EmployeeCode"=ANY(expected_codes) AND upper("Status")='ACTIVE';
          IF expected_employees<>11 THEN
            RAISE EXCEPTION 'Development workflow identities expected 11 active employees; found %.',expected_employees;
          END IF;
          IF EXISTS (SELECT 1 FROM advance.employees WHERE "EmployeeCode"=ANY(expected_codes) AND NOT "LoginEnabled") THEN
            RAISE EXCEPTION 'Every development workflow employee must be login-enabled before identity provisioning.';
          END IF;

          SELECT count(*) INTO expected_assignments
          FROM advance.employee_company_assignments assignment
          JOIN advance.employees employee ON employee."Id"=assignment."EmployeeId"
          JOIN advance.companies company ON company."Id"=assignment."CompanyId"
          WHERE employee."EmployeeCode"=ANY(expected_codes)
            AND assignment."IsActive" AND company."IsActive";
          IF expected_assignments<>22 THEN
            RAISE EXCEPTION 'Development workflow identities expected 22 active employee-company assignments; found %.',expected_assignments;
          END IF;

          IF EXISTS (
            SELECT 1
            FROM advance.employee_identity_mappings mapping
            JOIN advance.employees employee ON employee."Id"=mapping."EmployeeId"
            WHERE employee."EmployeeCode"=ANY(expected_codes)
              AND mapping."IdentityType"='HUMAN' AND mapping."IsActive"
              AND NOT (
                (mapping."Issuer"=issuer AND mapping."Subject"=employee."EmployeeCode")
                OR (employee."EmployeeCode" IN ('SESS-04','SESS-12')
                    AND mapping."Issuer"=issuer
                    AND mapping."Subject"='dev-'||lower(employee."EmployeeCode")))
          ) THEN
            RAISE EXCEPTION 'An employee already has an unrelated active HUMAN identity; refusing to replace it.';
          END IF;

          IF EXISTS (
            SELECT 1 FROM advance.employee_identity_mappings mapping
            JOIN advance.employees employee ON employee."EmployeeCode"=mapping."Subject"
            WHERE employee."EmployeeCode"=ANY(expected_codes)
              AND mapping."Issuer"=issuer AND mapping."IsActive"
              AND mapping."EmployeeId"<>employee."Id")
          THEN RAISE EXCEPTION 'A development employee subject is already mapped to another employee.'; END IF;

          UPDATE advance.employee_identity_mappings mapping
          SET "EffectiveTo"=current_date,
              "IsActive"=false,
              "UpdatedAt"=clock_timestamp(),
              "UpdatedBy"='DEVELOPMENT_WORKFLOW_IDENTITIES',
              "Version"=mapping."Version"+1
          FROM advance.employees employee
          WHERE employee."Id"=mapping."EmployeeId"
            AND employee."EmployeeCode" IN ('SESS-04','SESS-12')
            AND mapping."Issuer"=issuer
            AND mapping."Subject"='dev-'||lower(employee."EmployeeCode")
            AND mapping."IdentityType"='HUMAN' AND mapping."IsActive";

          INSERT INTO advance.employee_identity_mappings
            ("Id","CompanyId","OrganizationId","Issuer","Subject","EmployeeId","IdentityType",
             "EffectiveFrom","EffectiveTo","IsActive","CreatedAt","CreatedBy","Version")
          SELECT gen_random_uuid(),company."Id",company."Code",issuer,employee."EmployeeCode",employee."Id",'HUMAN',
                 current_date,NULL,true,clock_timestamp(),'DEVELOPMENT_WORKFLOW_IDENTITIES',0
          FROM advance.employees employee
          JOIN advance.employee_company_assignments assignment
            ON assignment."EmployeeId"=employee."Id" AND assignment."IsActive"
          JOIN advance.companies company
            ON company."Id"=assignment."CompanyId" AND company."IsActive"
          WHERE employee."EmployeeCode"=ANY(expected_codes)
            AND NOT EXISTS (
              SELECT 1 FROM advance.employee_identity_mappings mapping
              WHERE mapping."CompanyId"=company."Id" AND mapping."Issuer"=issuer
                AND mapping."Subject"=employee."EmployeeCode" AND mapping."IsActive");

          INSERT INTO advance.audit_logs
            ("Id","CompanyId","Scope","Module","Action","EntityName","EntityId","UserLoginId",
             "ActorRoleCode","Result","CorrelationId","AfterJson","CreatedAt","CreatedBy","Version")
          SELECT gen_random_uuid(),mapping."CompanyId",'COMPANY','Security','DevelopmentIdentityConverged',
                 'EmployeeIdentityMapping',mapping."Id"::text,session_user,'','Success',
                 'DEV_IDENTITY_'||replace(mapping."Id"::text,'-',''),
                 jsonb_build_object('employeeCode',employee."EmployeeCode",'issuer',mapping."Issuer",
                                    'subject',mapping."Subject",'effectiveFrom',mapping."EffectiveFrom")::text,
                 clock_timestamp(),'DEVELOPMENT_WORKFLOW_IDENTITIES',0
          FROM advance.employee_identity_mappings mapping
          JOIN advance.employees employee ON employee."Id"=mapping."EmployeeId"
          WHERE employee."EmployeeCode"=ANY(expected_codes)
            AND mapping."Issuer"=issuer AND mapping."Subject"=employee."EmployeeCode"
            AND mapping."IdentityType"='HUMAN' AND mapping."IsActive"
            AND NOT EXISTS (
              SELECT 1 FROM advance.audit_logs audit
              WHERE audit."Action"='DevelopmentIdentityConverged'
                AND audit."EntityName"='EmployeeIdentityMapping'
                AND audit."EntityId"=mapping."Id"::text);

          INSERT INTO advance.audit_logs
            ("Id","CompanyId","Scope","Module","Action","EntityName","EntityId","UserLoginId",
             "ActorRoleCode","Result","CorrelationId","AfterJson","CreatedAt","CreatedBy","Version")
          SELECT gen_random_uuid(),mapping."CompanyId",'COMPANY','Security','LegacyDevelopmentIdentityClosed',
                 'EmployeeIdentityMapping',mapping."Id"::text,session_user,'','Success',
                 'DEV_IDENTITY_CLOSE_'||replace(mapping."Id"::text,'-',''),
                 jsonb_build_object('employeeCode',employee."EmployeeCode",'issuer',mapping."Issuer",
                                    'subject',mapping."Subject",'effectiveTo',mapping."EffectiveTo")::text,
                 clock_timestamp(),'DEVELOPMENT_WORKFLOW_IDENTITIES',0
          FROM advance.employee_identity_mappings mapping
          JOIN advance.employees employee ON employee."Id"=mapping."EmployeeId"
          WHERE employee."EmployeeCode" IN ('SESS-04','SESS-12')
            AND mapping."Issuer"=issuer
            AND mapping."Subject"='dev-'||lower(employee."EmployeeCode")
            AND mapping."IdentityType"='HUMAN' AND NOT mapping."IsActive"
            AND mapping."UpdatedBy"='DEVELOPMENT_WORKFLOW_IDENTITIES'
            AND NOT EXISTS (
              SELECT 1 FROM advance.audit_logs audit
              WHERE audit."Action"='LegacyDevelopmentIdentityClosed'
                AND audit."EntityName"='EmployeeIdentityMapping'
                AND audit."EntityId"=mapping."Id"::text);
          IF (SELECT count(*) FROM advance.employee_identity_mappings mapping
              JOIN advance.employees employee ON employee."Id"=mapping."EmployeeId"
              WHERE employee."EmployeeCode"=ANY(expected_codes)
                AND mapping."Issuer"=issuer AND mapping."Subject"=employee."EmployeeCode"
                AND mapping."IdentityType"='HUMAN' AND mapping."IsActive"
                AND mapping."EffectiveFrom"<=current_date
                AND (mapping."EffectiveTo" IS NULL OR mapping."EffectiveTo">=current_date))<>22 THEN
            RAISE EXCEPTION 'Development workflow identity verification did not find exactly 22 effective mappings.';
          END IF;
        END $provision$;
        SELECT '11 employees; 22 effective company identity mappings; issuer urn:nexaerp:development; subjects equal employee codes';
        """;
}
#endif