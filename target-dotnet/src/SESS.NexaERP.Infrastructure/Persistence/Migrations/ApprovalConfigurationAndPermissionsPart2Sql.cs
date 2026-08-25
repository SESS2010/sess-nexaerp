namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class ApprovalConfigurationAndPermissionsPart2Sql
{
    internal static string Up => AdvanceSchemaSql.Expand(UpSql);
    internal static string Down => AdvanceSchemaSql.Expand(DownSql);

    private const string UpSql = """
        LOCK TABLE __advance_schema__.companies, __advance_schema__.departments, __advance_schema__.employees,
          __advance_schema__.roles, __advance_schema__.role_page_permissions,
          __advance_schema__.purchase_transaction_approval_policies,
          __advance_schema__.purchase_approval_route_settings,
          __advance_schema__.purchase_approval_workflow_steps,
          __advance_schema__.department_approval_mappings IN SHARE ROW EXCLUSIVE MODE;

        DO $preflight$
        BEGIN
          IF (SELECT count(*) FROM __advance_schema__.companies WHERE "Id" IN
              ('70000000-0000-0000-0000-000000000001','70000000-0000-0000-0000-000000000002') AND "IsActive") <> 2 THEN
            RAISE EXCEPTION 'Part 2 requires both settled active companies.';
          END IF;
          IF (SELECT count(*) FROM __advance_schema__.departments WHERE "Code" IN
              ('PRODUCTION','FABRICATION','REFRIGERATION','ELECTRICAL','PLC_LABVIEW','QC','R_AND_D','MAINTENANCE','DESIGN','CALIBRATION',
               'ACCOUNTS','HR','IT','SALES','MARKETING','SERVICE','AMC','CAMC','STORES','PURCHASE','MANAGEMENT') AND "IsActive") <> 21 THEN
            RAISE EXCEPTION 'Part 2 requires the 21 settled active departments.';
          END IF;
          IF (SELECT count(*) FROM __advance_schema__.employees WHERE "EmployeeCode" IN ('SESS-01','SESS-02','SESS-14','SESS-25') AND upper("Status")='ACTIVE') <> 4 THEN
            RAISE EXCEPTION 'Part 2 requires all four named approvers.';
          END IF;
          IF (SELECT count(*) FROM __advance_schema__.roles WHERE "Code" IN
              ('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR','PRODUCTION_MANAGER','ACCOUNTS_MANAGER','PURCHASE_MANAGER') AND "IsActive") <> 5 THEN
            RAISE EXCEPTION 'Part 2 requires the five settled roles.';
          END IF;
          IF EXISTS (SELECT 1 FROM __advance_schema__.purchase_transaction_approval_policies WHERE "CreatedBy"='APPROVAL_CONFIGURATION_PART2')
             OR EXISTS (SELECT 1 FROM __advance_schema__.purchase_approval_route_settings WHERE "CreatedBy"='APPROVAL_CONFIGURATION_PART2')
             OR EXISTS (SELECT 1 FROM __advance_schema__.purchase_approval_workflow_steps WHERE "CreatedBy"='APPROVAL_CONFIGURATION_PART2')
             OR EXISTS (SELECT 1 FROM __advance_schema__.department_approval_mappings WHERE "CreatedBy"='APPROVAL_CONFIGURATION_PART2') THEN
            RAISE EXCEPTION 'Part 2 configuration already exists; refusing replay.';
          END IF;
        END $preflight$;

        ALTER TABLE __advance_schema__.purchase_transaction_approval_policies DISABLE TRIGGER USER;
        UPDATE __advance_schema__.purchase_transaction_approval_policies
        SET "EffectiveTo"=DATE '2026-08-26',"IsActive"=false,
            "UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='APPROVAL_CONFIGURATION_PART2',"Version"="Version"+1
        WHERE "IsActive" AND ("EffectiveTo" IS NULL OR "EffectiveTo">=DATE '2026-08-27');

        INSERT INTO __advance_schema__.purchase_transaction_approval_policies
          ("Id","CompanyId","OrganizationId","RouteCode","MinimumAmount","MaximumAmount","ApproverRoleCode",
           "EffectiveFrom","EffectiveTo","IsActive","CreatedAt","CreatedBy","Version")
        SELECT md5('ACP2|POLICY|'||c."Code"||'|'||v.route)::uuid,c."Id",c."Code",v.route,v.minimum,v.maximum,NULL,
               DATE '2026-08-27',NULL,true,TIMESTAMPTZ '2026-08-25 00:00:00+00','APPROVAL_CONFIGURATION_PART2',0
        FROM __advance_schema__.companies c
        CROSS JOIN (VALUES
          ('DEPARTMENT_ONLY',0.000000::numeric,4999.999999::numeric),
          ('DEPARTMENT_THEN_TD',5000.000000::numeric,100000.000000::numeric),
          ('DEPARTMENT_THEN_MD',100000.000001::numeric,999999999999999999.999999::numeric)
        ) v(route,minimum,maximum)
        WHERE c."Id" IN ('70000000-0000-0000-0000-000000000001','70000000-0000-0000-0000-000000000002');
        ALTER TABLE __advance_schema__.purchase_transaction_approval_policies ENABLE TRIGGER USER;

        UPDATE __advance_schema__.purchase_approval_route_settings
        SET "IsActive"=false,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',
            "UpdatedBy"='APPROVAL_CONFIGURATION_PART2',"Version"="Version"+1
        WHERE "IsActive";

        INSERT INTO __advance_schema__.purchase_approval_route_settings
          ("Id","CompanyId","RouteCode","MinimumAmount","MaximumAmount","ApproverRoleCode","ApproverResolutionType",
           "IsActive","CreatedAt","CreatedBy","Version")
        SELECT md5('ACP2|ROUTE|'||c."Code"||'|'||v.route)::uuid,c."Id",v.route,v.minimum,v.maximum,NULL,'WORKFLOW_STEPS',
               true,TIMESTAMPTZ '2026-08-25 00:00:00+00','APPROVAL_CONFIGURATION_PART2',0
        FROM __advance_schema__.companies c
        CROSS JOIN (VALUES
          ('DEPARTMENT_ONLY',0.00::numeric,4999.99::numeric),
          ('DEPARTMENT_THEN_TD',5000.00::numeric,100000.00::numeric),
          ('DEPARTMENT_THEN_MD',100000.01::numeric,NULL::numeric)
        ) v(route,minimum,maximum)
        WHERE c."Id" IN ('70000000-0000-0000-0000-000000000001','70000000-0000-0000-0000-000000000002');

        UPDATE __advance_schema__.purchase_approval_workflow_steps
        SET "EffectiveTo"=DATE '2026-08-26',"IsActive"=false,
            "UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='APPROVAL_CONFIGURATION_PART2',"Version"="Version"+1
        WHERE "IsActive" AND ("EffectiveTo" IS NULL OR "EffectiveTo">=DATE '2026-08-27');

        INSERT INTO __advance_schema__.purchase_approval_workflow_steps
          ("Id","CompanyId","RouteCode","MinimumAmount","MaximumAmount","StepNumber","ApproverResolutionType",
           "ApproverEmployeeCode","ApproverRoleCode","IsActive","EffectiveFrom","EffectiveTo","Remarks","CreatedAt","CreatedBy","Version")
        SELECT md5('ACP2|STEP|'||c."Code"||'|'||v.route||'|'||v.step)::uuid,c."Id",v.route,v.minimum,v.maximum,v.step,
               v.resolution,v.employee_code,v.role_code,true,DATE '2026-08-27',NULL,
               'Settled department-first amount approval workflow',TIMESTAMPTZ '2026-08-25 00:00:00+00','APPROVAL_CONFIGURATION_PART2',0
        FROM __advance_schema__.companies c
        CROSS JOIN (VALUES
          ('DEPARTMENT_ONLY',0.00::numeric,4999.99::numeric,1,'DEPARTMENT_MAPPING',NULL::text,NULL::text),
          ('DEPARTMENT_THEN_TD',5000.00::numeric,100000.00::numeric,1,'DEPARTMENT_MAPPING',NULL::text,NULL::text),
          ('DEPARTMENT_THEN_TD',5000.00::numeric,100000.00::numeric,2,'CONFIGURED_ROLE','SESS-01','TECHNICAL_DIRECTOR'),
          ('DEPARTMENT_THEN_MD',100000.01::numeric,NULL::numeric,1,'DEPARTMENT_MAPPING',NULL::text,NULL::text),
          ('DEPARTMENT_THEN_MD',100000.01::numeric,NULL::numeric,2,'CONFIGURED_ROLE','SESS-02','MANAGING_DIRECTOR')
        ) v(route,minimum,maximum,step,resolution,employee_code,role_code)
        WHERE c."Id" IN ('70000000-0000-0000-0000-000000000001','70000000-0000-0000-0000-000000000002');

        UPDATE __advance_schema__.department_approval_mappings
        SET "EffectiveTo"=DATE '2026-08-26',"IsActive"=false,
            "UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='APPROVAL_CONFIGURATION_PART2',"Version"="Version"+1
        WHERE "IsActive" AND ("EffectiveTo" IS NULL OR "EffectiveTo">=DATE '2026-08-27');

        CREATE TEMP TABLE _acp2_department_routes(
          department_code text PRIMARY KEY, employee_code text NOT NULL, role_code text NOT NULL) ON COMMIT DROP;
        INSERT INTO _acp2_department_routes VALUES
          ('PRODUCTION','SESS-25','PRODUCTION_MANAGER'),('FABRICATION','SESS-25','PRODUCTION_MANAGER'),
          ('REFRIGERATION','SESS-25','PRODUCTION_MANAGER'),('ELECTRICAL','SESS-25','PRODUCTION_MANAGER'),
          ('PLC_LABVIEW','SESS-25','PRODUCTION_MANAGER'),('QC','SESS-25','PRODUCTION_MANAGER'),
          ('R_AND_D','SESS-25','PRODUCTION_MANAGER'),('MAINTENANCE','SESS-25','PRODUCTION_MANAGER'),
          ('DESIGN','SESS-25','PRODUCTION_MANAGER'),('CALIBRATION','SESS-25','PRODUCTION_MANAGER'),
          ('ACCOUNTS','SESS-14','ACCOUNTS_MANAGER'),('HR','SESS-14','ACCOUNTS_MANAGER'),
          ('IT','SESS-14','ACCOUNTS_MANAGER'),('SALES','SESS-14','ACCOUNTS_MANAGER'),
          ('MARKETING','SESS-14','ACCOUNTS_MANAGER'),('SERVICE','SESS-14','ACCOUNTS_MANAGER'),
          ('AMC','SESS-14','ACCOUNTS_MANAGER'),('CAMC','SESS-14','ACCOUNTS_MANAGER'),
          ('STORES','SESS-14','ACCOUNTS_MANAGER'),('PURCHASE','SESS-14','ACCOUNTS_MANAGER'),
          ('MANAGEMENT','SESS-14','ACCOUNTS_MANAGER');

        INSERT INTO __advance_schema__.department_approval_mappings
          ("Id","CompanyId","DepartmentId","ApprovalRouteCode","Scope","ApproverRoleCode",
           "PrimaryApproverEmployeeId","AlternateApproverEmployeeId","EffectiveFrom","EffectiveTo","IsActive","Remarks",
           "CreatedAt","CreatedBy","Version")
        SELECT md5('ACP2|DEPARTMENT|'||c."Code"||'|'||m.department_code)::uuid,c."Id",d."Id",'MANAGER','ALL',m.role_code,
               e."Id",NULL,DATE '2026-08-27',NULL,true,'Settled department level-1 approver',
               TIMESTAMPTZ '2026-08-25 00:00:00+00','APPROVAL_CONFIGURATION_PART2',0
        FROM __advance_schema__.companies c CROSS JOIN _acp2_department_routes m
        JOIN __advance_schema__.departments d ON d."Code"=m.department_code AND d."IsActive"
        JOIN __advance_schema__.employees e ON e."EmployeeCode"=m.employee_code AND upper(e."Status")='ACTIVE'
        WHERE c."Id" IN ('70000000-0000-0000-0000-000000000001','70000000-0000-0000-0000-000000000002');

        DO $acceptance$
        BEGIN
          IF (SELECT count(*) FROM __advance_schema__.purchase_transaction_approval_policies WHERE "CreatedBy"='APPROVAL_CONFIGURATION_PART2' AND "IsActive")<>6 THEN RAISE EXCEPTION 'Expected six active Part 2 transaction policies.'; END IF;
          IF (SELECT count(*) FROM __advance_schema__.purchase_approval_route_settings WHERE "CreatedBy"='APPROVAL_CONFIGURATION_PART2' AND "IsActive")<>6 THEN RAISE EXCEPTION 'Expected six active Part 2 route settings.'; END IF;
          IF (SELECT count(*) FROM __advance_schema__.purchase_approval_workflow_steps WHERE "CreatedBy"='APPROVAL_CONFIGURATION_PART2' AND "IsActive")<>10 THEN RAISE EXCEPTION 'Expected ten active Part 2 workflow steps.'; END IF;
          IF (SELECT count(*) FROM __advance_schema__.department_approval_mappings WHERE "CreatedBy"='APPROVAL_CONFIGURATION_PART2' AND "IsActive")<>42 THEN RAISE EXCEPTION 'Expected 42 active Part 2 department mappings.'; END IF;
          IF EXISTS (
            SELECT 1 FROM __advance_schema__.role_page_permissions p JOIN __advance_schema__.roles r ON r."Id"=p."RoleId"
            WHERE r."Code"='PURCHASE_MANAGER' AND (p."CanApprove" OR p."CanReject" OR p."CanRequestRevision" OR p."HasFullControl")
          ) THEN RAISE EXCEPTION 'PURCHASE_MANAGER retains an approval grant.'; END IF;
        END $acceptance$;
        """;

    private const string DownSql = """
        LOCK TABLE __advance_schema__.purchase_transaction_approval_policies,
          __advance_schema__.purchase_approval_route_settings,
          __advance_schema__.purchase_approval_workflow_steps,
          __advance_schema__.department_approval_mappings IN SHARE ROW EXCLUSIVE MODE;

        DO $down_guard$
        BEGIN
          IF EXISTS (
            SELECT 1 FROM __advance_schema__.purchase_transaction_approval_policies WHERE "CreatedBy"='APPROVAL_CONFIGURATION_PART2' AND ("UpdatedBy" IS NOT NULL OR "Version"<>0)
            UNION ALL SELECT 1 FROM __advance_schema__.purchase_approval_route_settings WHERE "CreatedBy"='APPROVAL_CONFIGURATION_PART2' AND ("UpdatedBy" IS NOT NULL OR "Version"<>0)
            UNION ALL SELECT 1 FROM __advance_schema__.purchase_approval_workflow_steps WHERE "CreatedBy"='APPROVAL_CONFIGURATION_PART2' AND ("UpdatedBy" IS NOT NULL OR "Version"<>0)
            UNION ALL SELECT 1 FROM __advance_schema__.department_approval_mappings WHERE "CreatedBy"='APPROVAL_CONFIGURATION_PART2' AND ("UpdatedBy" IS NOT NULL OR "Version"<>0)
          ) THEN RAISE EXCEPTION 'Part 2 configuration changed after migration; refusing destructive Down.'; END IF;
        END $down_guard$;

        ALTER TABLE __advance_schema__.purchase_transaction_approval_policies DISABLE TRIGGER USER;
        DELETE FROM __advance_schema__.purchase_transaction_approval_policies WHERE "CreatedBy"='APPROVAL_CONFIGURATION_PART2';
        UPDATE __advance_schema__.purchase_transaction_approval_policies
        SET "EffectiveTo"=NULL,"IsActive"=true,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',
            "UpdatedBy"='APPROVAL_CONFIGURATION_PART2_DOWN',"Version"="Version"+1
        WHERE "UpdatedBy"='APPROVAL_CONFIGURATION_PART2' AND "EffectiveTo"=DATE '2026-08-26';
        ALTER TABLE __advance_schema__.purchase_transaction_approval_policies ENABLE TRIGGER USER;

        DELETE FROM __advance_schema__.department_approval_mappings WHERE "CreatedBy"='APPROVAL_CONFIGURATION_PART2';
        UPDATE __advance_schema__.department_approval_mappings
        SET "EffectiveTo"=NULL,"IsActive"=true,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',
            "UpdatedBy"='APPROVAL_CONFIGURATION_PART2_DOWN',"Version"="Version"+1
        WHERE "UpdatedBy"='APPROVAL_CONFIGURATION_PART2' AND "EffectiveTo"=DATE '2026-08-26';

        DELETE FROM __advance_schema__.purchase_approval_workflow_steps WHERE "CreatedBy"='APPROVAL_CONFIGURATION_PART2';
        UPDATE __advance_schema__.purchase_approval_workflow_steps
        SET "EffectiveTo"=NULL,"IsActive"=true,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',
            "UpdatedBy"='APPROVAL_CONFIGURATION_PART2_DOWN',"Version"="Version"+1
        WHERE "UpdatedBy"='APPROVAL_CONFIGURATION_PART2' AND "EffectiveTo"=DATE '2026-08-26';

        DELETE FROM __advance_schema__.purchase_approval_route_settings WHERE "CreatedBy"='APPROVAL_CONFIGURATION_PART2';
        UPDATE __advance_schema__.purchase_approval_route_settings
        SET "IsActive"=true,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',
            "UpdatedBy"='APPROVAL_CONFIGURATION_PART2_DOWN',"Version"="Version"+1
        WHERE "UpdatedBy"='APPROVAL_CONFIGURATION_PART2';
        """;
}
