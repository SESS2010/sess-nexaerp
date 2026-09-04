namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class EmployeeRoleGovernancePhase2Sql
{
    internal const string Prepare = """
        UPDATE advance.employee_role_assignments
        SET "AssignmentType"='PERMANENT'
        WHERE "AssignmentType"='';

        UPDATE advance.employee_role_assignments
        SET "EndReason"=COALESCE(NULLIF(btrim("Remarks"),''),'Historical dated assignment'),
            "EndedAt"=COALESCE("UpdatedAt","CreatedAt"),
            "EndedBy"=COALESCE(NULLIF("UpdatedBy",''),NULLIF("CreatedBy",''),'historical-migration')
        WHERE "EffectiveTo" IS NOT NULL
          AND ("EndReason" IS NULL OR "EndedAt" IS NULL OR "EndedBy" IS NULL);

        UPDATE advance.audit_logs
        SET "ActorRoleCode"='HISTORICAL_UNRECORDED'
        WHERE "ActorRoleCode"='';
        """;

    internal const string Up = """
        UPDATE advance.employee_role_assignments
        SET "AssignmentType"='PERMANENT'
        WHERE "AssignmentType"='';

        UPDATE advance.employee_role_assignments
        SET "EndReason"=COALESCE(NULLIF(btrim("Remarks"),''),'Historical dated assignment'),
            "EndedAt"=COALESCE("UpdatedAt","CreatedAt"),
            "EndedBy"=COALESCE(NULLIF("UpdatedBy",''),NULLIF("CreatedBy",''),'historical-migration')
        WHERE "EffectiveTo" IS NOT NULL
          AND ("EndReason" IS NULL OR "EndedAt" IS NULL OR "EndedBy" IS NULL);

        UPDATE advance.audit_logs
        SET "ActorRoleCode"='HISTORICAL_UNRECORDED'
        WHERE "ActorRoleCode"='';

        WITH confirmed("EmployeeCode","RoleCode") AS (
          VALUES
            ('SESS-01','TECHNICAL_DIRECTOR'),
            ('SESS-02','MANAGING_DIRECTOR'),
            ('SESS-12','IT_MANAGER'),
            ('SESS-14','ACCOUNTS_MANAGER'),
            ('SESS-15','PURCHASE_MANAGER'),
            ('SESS-16','STORES_ASSISTANT'),
            ('SESS-25','PRODUCTION_MANAGER'),
            ('SESS-33','QC_MANAGER'),
            ('SESS-35','STORES_EXECUTIVE'),
            ('SESS-41','STORES_MANAGER')
        ),
        required AS (
          SELECT c."Id" AS "CompanyId",e."Id" AS "EmployeeId",e."EmployeeCode",r."Id" AS "RoleId",r."Code" AS "RoleCode"
          FROM confirmed x
          JOIN advance.employees e ON e."EmployeeCode"=x."EmployeeCode" AND upper(e."Status")='ACTIVE'
          JOIN advance.roles r ON r."Code"=x."RoleCode" AND r."IsActive" AND r."IsEmployeeAssignable"
          JOIN advance.employee_company_assignments eca ON eca."EmployeeId"=e."Id"
            AND eca."IsActive" AND eca."Status"='ACTIVE'
            AND eca."EffectiveFrom"<=DATE '2026-09-04'
            AND (eca."EffectiveTo" IS NULL OR eca."EffectiveTo">=DATE '2026-09-04')
          JOIN advance.companies c ON c."Id"=eca."CompanyId" AND c."IsActive"
          JOIN advance.company_role_activations cra ON cra."CompanyId"=c."Id" AND cra."RoleId"=r."Id"
            AND cra."IsEnabled" AND cra."EffectiveFrom"<=DATE '2026-09-04'
            AND (cra."EffectiveTo" IS NULL OR cra."EffectiveTo">=DATE '2026-09-04')
        )
        INSERT INTO advance.employee_role_assignments
          ("Id","CompanyId","EmployeeId","RoleId","EffectiveFrom","EffectiveTo","AssignmentType","IsPrimary",
           "ApprovalStatus","Remarks","CreatedAt","CreatedBy","Version")
        SELECT md5('phase2-primary|'||x."CompanyId"||'|'||x."EmployeeCode"||'|'||x."RoleCode")::uuid,
               x."CompanyId",x."EmployeeId",x."RoleId",DATE '2026-09-04',NULL,'PERMANENT',FALSE,
               'Approved','Confirmed primary-role baseline',TIMESTAMPTZ '2026-09-04 00:00:00+00',
               'migration-employee-role-governance-phase2',0
        FROM required x
        WHERE NOT EXISTS (
          SELECT 1 FROM advance.employee_role_assignments a
          WHERE a."CompanyId"=x."CompanyId" AND a."EmployeeId"=x."EmployeeId" AND a."RoleId"=x."RoleId"
            AND a."ApprovalStatus" IN ('Approved','SeedApproved')
            AND a."EffectiveFrom"<=DATE '2026-09-04'
            AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=DATE '2026-09-04'));

        WITH required AS (
          SELECT c."Id" AS "CompanyId",e."Id" AS "EmployeeId",r."Id" AS "RoleId"
          FROM advance.employees e
          JOIN advance.employee_company_assignments eca ON eca."EmployeeId"=e."Id"
            AND eca."IsActive" AND eca."Status"='ACTIVE'
            AND eca."EffectiveFrom"<=DATE '2026-09-04'
            AND (eca."EffectiveTo" IS NULL OR eca."EffectiveTo">=DATE '2026-09-04')
          JOIN advance.companies c ON c."Id"=eca."CompanyId" AND c."IsActive"
          JOIN advance.roles r ON r."Code"='SERVICE_COORDINATOR' AND r."IsActive" AND r."IsEmployeeAssignable"
          JOIN advance.company_role_activations cra ON cra."CompanyId"=c."Id" AND cra."RoleId"=r."Id"
            AND cra."IsEnabled" AND cra."EffectiveFrom"<=DATE '2026-09-04'
            AND (cra."EffectiveTo" IS NULL OR cra."EffectiveTo">=DATE '2026-09-04')
          WHERE e."EmployeeCode"='SESS-28' AND upper(e."EmployeeName") LIKE 'VENKAT RAV%'
        )
        INSERT INTO advance.employee_role_assignments
          ("Id","CompanyId","EmployeeId","RoleId","EffectiveFrom","EffectiveTo","AssignmentType","IsPrimary",
           "ApprovalStatus","Remarks","CreatedAt","CreatedBy","Version")
        SELECT md5('phase2-vengat-service-coordinator|'||x."CompanyId")::uuid,
               x."CompanyId",x."EmployeeId",x."RoleId",DATE '2026-09-04',NULL,'PERMANENT',FALSE,
               'Approved','Confirmed VENGAT SERVICE_COORDINATOR assignment',TIMESTAMPTZ '2026-09-04 00:00:00+00',
               'migration-employee-role-governance-phase2',0
        FROM required x
        WHERE NOT EXISTS (
          SELECT 1 FROM advance.employee_role_assignments a
          WHERE a."CompanyId"=x."CompanyId" AND a."EmployeeId"=x."EmployeeId" AND a."RoleId"=x."RoleId"
            AND a."ApprovalStatus" IN ('Approved','SeedApproved')
            AND a."EffectiveFrom"<=DATE '2026-09-04'
            AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=DATE '2026-09-04'));

        INSERT INTO advance.employee_company_role_profiles
          ("Id","CompanyId","EmployeeId","ConfigurationStatus","PrimaryRoleAssignmentId",
           "CreatedAt","CreatedBy","Version")
        SELECT md5('phase2-role-profile|'||eca."CompanyId"||'|'||eca."EmployeeId")::uuid,
               eca."CompanyId",eca."EmployeeId",'PENDING',NULL,
               TIMESTAMPTZ '2026-09-04 00:00:00+00','migration-employee-role-governance-phase2',0
        FROM advance.employee_company_assignments eca
        JOIN advance.employees e ON e."Id"=eca."EmployeeId" AND upper(e."Status")='ACTIVE'
        JOIN advance.companies c ON c."Id"=eca."CompanyId" AND c."IsActive"
        WHERE eca."IsActive" AND eca."Status"='ACTIVE'
          AND eca."EffectiveFrom"<=DATE '2026-09-04'
          AND (eca."EffectiveTo" IS NULL OR eca."EffectiveTo">=DATE '2026-09-04')
        ON CONFLICT ("CompanyId","EmployeeId") DO NOTHING;

        WITH confirmed("EmployeeCode","RoleCode") AS (
          VALUES
            ('SESS-01','TECHNICAL_DIRECTOR'),
            ('SESS-02','MANAGING_DIRECTOR'),
            ('SESS-12','IT_MANAGER'),
            ('SESS-14','ACCOUNTS_MANAGER'),
            ('SESS-15','PURCHASE_MANAGER'),
            ('SESS-16','STORES_ASSISTANT'),
            ('SESS-25','PRODUCTION_MANAGER'),
            ('SESS-33','QC_MANAGER'),
            ('SESS-35','STORES_EXECUTIVE'),
            ('SESS-41','STORES_MANAGER')
        ),
        selected AS (
          SELECT p."Id" AS "ProfileId",a."Id" AS "AssignmentId",
                 row_number() OVER (PARTITION BY p."Id" ORDER BY a."EffectiveFrom" DESC,a."CreatedAt" DESC,a."Id") AS rn
          FROM confirmed x
          JOIN advance.employees e ON e."EmployeeCode"=x."EmployeeCode"
          JOIN advance.employee_company_role_profiles p ON p."EmployeeId"=e."Id"
          JOIN advance.roles r ON r."Code"=x."RoleCode"
          JOIN advance.employee_role_assignments a ON a."CompanyId"=p."CompanyId"
            AND a."EmployeeId"=p."EmployeeId" AND a."RoleId"=r."Id"
            AND a."ApprovalStatus" IN ('Approved','SeedApproved')
            AND a."EffectiveFrom"<=DATE '2026-09-04'
            AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=DATE '2026-09-04')
        ),
        chosen AS (SELECT "ProfileId","AssignmentId" FROM selected WHERE rn=1)
        UPDATE advance.employee_role_assignments a
        SET "IsPrimary"=TRUE
        FROM chosen x
        WHERE a."Id"=x."AssignmentId";

        WITH confirmed("EmployeeCode","RoleCode") AS (
          VALUES
            ('SESS-01','TECHNICAL_DIRECTOR'),
            ('SESS-02','MANAGING_DIRECTOR'),
            ('SESS-12','IT_MANAGER'),
            ('SESS-14','ACCOUNTS_MANAGER'),
            ('SESS-15','PURCHASE_MANAGER'),
            ('SESS-16','STORES_ASSISTANT'),
            ('SESS-25','PRODUCTION_MANAGER'),
            ('SESS-33','QC_MANAGER'),
            ('SESS-35','STORES_EXECUTIVE'),
            ('SESS-41','STORES_MANAGER')
        ),
        selected AS (
          SELECT p."Id" AS "ProfileId",a."Id" AS "AssignmentId",
                 row_number() OVER (PARTITION BY p."Id" ORDER BY a."EffectiveFrom" DESC,a."CreatedAt" DESC,a."Id") AS rn
          FROM confirmed x
          JOIN advance.employees e ON e."EmployeeCode"=x."EmployeeCode"
          JOIN advance.employee_company_role_profiles p ON p."EmployeeId"=e."Id"
          JOIN advance.roles r ON r."Code"=x."RoleCode"
          JOIN advance.employee_role_assignments a ON a."CompanyId"=p."CompanyId"
            AND a."EmployeeId"=p."EmployeeId" AND a."RoleId"=r."Id" AND a."IsPrimary"
        )
        UPDATE advance.employee_company_role_profiles p
        SET "ConfigurationStatus"='CONFIGURED',"PrimaryRoleAssignmentId"=x."AssignmentId","Version"=1,
            "UpdatedAt"=TIMESTAMPTZ '2026-09-04 00:00:00+00',
            "UpdatedBy"='migration-employee-role-governance-phase2'
        FROM selected x
        WHERE x.rn=1 AND p."Id"=x."ProfileId";

        INSERT INTO advance.employee_role_assignment_events
          ("Id","CompanyId","EmployeeId","AssignmentId","Operation","FromRoleCode","ToRoleCode",
           "PreviousRoleRetained","EffectiveOn","Reason","ActorLoginId","ActorRoleCode",
           "CreatedAt","CreatedBy","Version")
        SELECT md5('phase2-primary-confirmed|'||p."CompanyId"||'|'||p."EmployeeId")::uuid,
               p."CompanyId",p."EmployeeId",p."PrimaryRoleAssignmentId",'SET_INITIAL_PRIMARY',NULL,r."Code",
               NULL,DATE '2026-09-04','Primary role confirmed by proprietor',
               'migration-employee-role-governance-phase2','SYSTEM_MIGRATION',
               TIMESTAMPTZ '2026-09-04 00:00:00+00','migration-employee-role-governance-phase2',0
        FROM advance.employee_company_role_profiles p
        JOIN advance.employee_role_assignments a ON a."Id"=p."PrimaryRoleAssignmentId"
        JOIN advance.roles r ON r."Id"=a."RoleId"
        WHERE p."ConfigurationStatus"='CONFIGURED';

        INSERT INTO advance.employee_role_assignment_events
          ("Id","CompanyId","EmployeeId","AssignmentId","Operation","FromRoleCode","ToRoleCode",
           "PreviousRoleRetained","EffectiveOn","Reason","ActorLoginId","ActorRoleCode",
           "CreatedAt","CreatedBy","Version")
        SELECT md5('phase2-vengat-confirmed|'||a."CompanyId")::uuid,
               a."CompanyId",a."EmployeeId",a."Id",'ASSIGN',NULL,'SERVICE_COORDINATOR',
               NULL,DATE '2026-09-04','VENGAT SERVICE_COORDINATOR assignment confirmed by proprietor',
               'migration-employee-role-governance-phase2','SYSTEM_MIGRATION',
               TIMESTAMPTZ '2026-09-04 00:00:00+00','migration-employee-role-governance-phase2',0
        FROM advance.employee_role_assignments a
        JOIN advance.employees e ON e."Id"=a."EmployeeId"
        JOIN advance.roles r ON r."Id"=a."RoleId"
        WHERE e."EmployeeCode"='SESS-28' AND r."Code"='SERVICE_COORDINATOR'
          AND a."CreatedBy"='migration-employee-role-governance-phase2';

        ALTER TABLE advance.employee_role_assignments
          ADD CONSTRAINT "EX_employee_role_assignment_no_overlap"
          EXCLUDE USING gist (
            "CompanyId" WITH =,
            "EmployeeId" WITH =,
            "RoleId" WITH =,
            daterange("EffectiveFrom",COALESCE("EffectiveTo",'infinity'::date),'[]') WITH &&
          )
          WHERE ("ApprovalStatus"<>'Rejected')
          DEFERRABLE INITIALLY DEFERRED;

        ALTER TABLE advance.employee_role_assignments
          ADD CONSTRAINT "EX_employee_role_assignment_one_primary"
          EXCLUDE USING gist (
            "CompanyId" WITH =,
            "EmployeeId" WITH =,
            daterange("EffectiveFrom",COALESCE("EffectiveTo",'infinity'::date),'[]') WITH &&
          )
          WHERE ("IsPrimary" AND "ApprovalStatus" IN ('Approved','SeedApproved'))
          DEFERRABLE INITIALLY DEFERRED;

        CREATE FUNCTION advance.validate_employee_primary_role()
        RETURNS trigger LANGUAGE plpgsql AS $function$
        BEGIN
          IF EXISTS (
            SELECT 1
            FROM advance.employee_company_role_profiles p
            LEFT JOIN advance.employee_role_assignments a
              ON a."Id"=p."PrimaryRoleAssignmentId"
             AND a."CompanyId"=p."CompanyId"
             AND a."EmployeeId"=p."EmployeeId"
             AND a."IsPrimary"
             AND a."ApprovalStatus" IN ('Approved','SeedApproved')
             AND a."EffectiveFrom"<=CURRENT_DATE
             AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=CURRENT_DATE)
            WHERE p."ConfigurationStatus"='CONFIGURED' AND a."Id" IS NULL
          ) THEN
            RAISE EXCEPTION 'Every configured employee/company must reference exactly one effective primary role assignment.';
          END IF;
          RETURN NULL;
        END
        $function$;

        CREATE CONSTRAINT TRIGGER "TR_employee_role_profile_exact_primary"
        AFTER INSERT OR UPDATE OR DELETE ON advance.employee_company_role_profiles
        DEFERRABLE INITIALLY DEFERRED
        FOR EACH ROW EXECUTE FUNCTION advance.validate_employee_primary_role();

        CREATE CONSTRAINT TRIGGER "TR_employee_role_assignment_exact_primary"
        AFTER INSERT OR UPDATE OR DELETE ON advance.employee_role_assignments
        DEFERRABLE INITIALLY DEFERRED
        FOR EACH ROW EXECUTE FUNCTION advance.validate_employee_primary_role();
        """;

    internal const string DownBeforeTables = """
        DROP TRIGGER IF EXISTS "TR_employee_role_assignment_exact_primary" ON advance.employee_role_assignments;
        DROP TRIGGER IF EXISTS "TR_employee_role_profile_exact_primary" ON advance.employee_company_role_profiles;
        DROP FUNCTION IF EXISTS advance.validate_employee_primary_role();
        ALTER TABLE advance.employee_role_assignments DROP CONSTRAINT IF EXISTS "EX_employee_role_assignment_one_primary";
        ALTER TABLE advance.employee_role_assignments DROP CONSTRAINT IF EXISTS "EX_employee_role_assignment_no_overlap";
        """;

    internal const string DownAfterTables = """
        DELETE FROM advance.employee_role_assignments
        WHERE "CreatedBy"='migration-employee-role-governance-phase2';
        """;
}