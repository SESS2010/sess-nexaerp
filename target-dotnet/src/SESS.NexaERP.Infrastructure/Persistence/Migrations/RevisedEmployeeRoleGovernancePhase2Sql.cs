namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class RevisedEmployeeRoleGovernancePhase2Sql
{
    internal static string AuthenticationBootstrapCompatibility => AuthenticationBootstrapCeremonySql.Up
        .Replace("CREATE FUNCTION advance.complete_authentication_bootstrap", "CREATE OR REPLACE FUNCTION advance.complete_authentication_bootstrap", StringComparison.Ordinal)
        .Replace("\"AssignmentType\",\"IsPrimary\",", "\"AssignmentType\",", StringComparison.Ordinal)
        .Replace("current_date,'PERMANENT',true,'SeedApproved'", "current_date,'FULL','SeedApproved'", StringComparison.Ordinal);
    internal const string Prepare = """
        UPDATE advance.employee_role_assignments SET "AssignmentType"='FULL' WHERE "AssignmentType"='';
        UPDATE advance.employee_role_assignments
        SET "EndReason"=COALESCE(NULLIF(btrim("Remarks"),''),'Historical dated assignment'),
            "EndedAt"=COALESCE("UpdatedAt","CreatedAt"),
            "EndedBy"=COALESCE(NULLIF("UpdatedBy",''),NULLIF("CreatedBy",''),'historical-migration')
        WHERE "EffectiveTo" IS NOT NULL AND "AssignmentType"<>'TEMPORARY'
          AND ("EndReason" IS NULL OR "EndedAt" IS NULL OR "EndedBy" IS NULL);
        UPDATE advance.audit_logs SET "ActorRoleCode"='HISTORICAL_UNRECORDED' WHERE "ActorRoleCode"='';
        """;

    internal const string Up = """
        ALTER TABLE advance.employee_role_assignments
          ADD CONSTRAINT "EX_employee_role_assignment_no_overlap"
          EXCLUDE USING gist (
            "CompanyId" WITH =, "EmployeeId" WITH =, "RoleId" WITH =,
            daterange("EffectiveFrom",COALESCE("EffectiveTo",'infinity'::date),'[]') WITH &&
          ) WHERE ("ApprovalStatus" IN ('Approved','SeedApproved'))
          DEFERRABLE INITIALLY DEFERRED;

        INSERT INTO advance.role_page_permissions
          ("Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit","CanIssue","CanVerify",
           "CanApprove","CanReject","CanRequestClarification","CanRequestRevision","CanResubmit","CanCancel",
           "CanDeactivate","CanPrint","CanDownload","CanExport","CanUploadAttachment","CanReplaceAttachment",
           "CanViewCommercialValues","CanViewAuditHistory","HasFullControl","CreatedAt","CreatedBy","Version")
        SELECT md5('revised-phase2-permission|TECHNICAL_SUPPORT_MANAGER|purchase.technical-verification')::uuid,
          r."Id",p."Id",true,false,false,false,false,true,false,false,false,false,false,false,false,false,false,false,false,false,false,false,false,
          TIMESTAMPTZ '2026-09-05 00:00:00+00','migration-revised-role-governance-phase2',0
        FROM advance.roles r CROSS JOIN advance.page_definitions p
        WHERE r."Code"='TECHNICAL_SUPPORT_MANAGER' AND r."IsActive"
          AND p."PageKey"='purchase.technical-verification' AND p."IsActive"
        ON CONFLICT ("RoleId","PageDefinitionId") DO NOTHING;

        WITH desired("EmployeeCode","RoleCode","AssignmentType") AS (
          VALUES
          ('SESS-01','TECHNICAL_DIRECTOR','FULL'),
          ('SESS-02','MANAGING_DIRECTOR','FULL'),
          ('SESS-03','HOUSEKEEPING_ASSISTANT','FULL'),
          ('SESS-04','TECHNICAL_SUPPORT_MANAGER','FULL'),
          ('SESS-05','SERVICE_ENGINEER','FULL'),('SESS-05','TECHNICAL_SUPPORT_MANAGER','SUPPORT'),
          ('SESS-06','ELECTRICAL_ENGINEER','FULL'),('SESS-06','SERVICE_ENGINEER','FULL'),
          ('SESS-07','PRODUCTION_OPERATOR','FULL'),('SESS-08','PRODUCTION_OPERATOR','FULL'),
          ('SESS-09','SERVICE_ENGINEER','FULL'),
          ('SESS-10','ELECTRICAL_ENGINEER','FULL'),('SESS-10','SERVICE_ENGINEER','FULL'),
          ('SESS-11','SERVICE_ENGINEER','FULL'),('SESS-12','IT_MANAGER','FULL'),
          ('SESS-13','PRODUCTION_COORDINATOR','FULL'),('SESS-14','ACCOUNTS_MANAGER','FULL'),
          ('SESS-15','PURCHASE_MANAGER','FULL'),('SESS-15','PURCHASE_EXECUTIVE','FULL'),('SESS-15','STORES_EXECUTIVE','SUPPORT'),
          ('SESS-16','STORES_ASSISTANT','FULL'),
          ('SESS-17','DESIGN_ENGINEER','FULL'),('SESS-17','SERVICE_ENGINEER','FULL'),
          ('SESS-18','ELECTRICAL_ENGINEER','FULL'),('SESS-18','SERVICE_ENGINEER','FULL'),
          ('SESS-19','DESIGN_ENGINEER','FULL'),('SESS-19','SERVICE_ENGINEER','FULL'),
          ('SESS-20','ELECTRICAL_ENGINEER','FULL'),('SESS-20','SERVICE_ENGINEER','FULL'),
          ('SESS-21','HR_MANAGER','FULL'),('SESS-22','PRODUCTION_OPERATOR','FULL'),
          ('SESS-23','PRODUCTION_OPERATOR','FULL'),('SESS-24','PRODUCTION_OPERATOR','FULL'),
          ('SESS-25','PRODUCTION_MANAGER','FULL'),
          ('SESS-26','MAINTENANCE_ENGINEER','FULL'),('SESS-26','SERVICE_ENGINEER','FULL'),
          ('SESS-27','ELECTRICAL_ENGINEER','FULL'),('SESS-27','SERVICE_ENGINEER','FULL'),
          ('SESS-28','ACCOUNTS_ASSISTANT','FULL'),('SESS-28','SERVICE_COORDINATOR','SUPPORT'),
          ('SESS-29','JUNIOR_ENGINEER','FULL'),('SESS-29','SERVICE_ENGINEER','FULL'),
          ('SESS-30','PLC_ENGINEER','FULL'),('SESS-30','SERVICE_ENGINEER','FULL'),
          ('SESS-31','SERVICE_ENGINEER','FULL'),('SESS-32','SOFTWARE_DEVELOPER','FULL'),
          ('SESS-33','QC_MANAGER','FULL'),('SESS-34','SERVICE_ENGINEER','FULL'),
          ('SESS-35','STORES_EXECUTIVE','FULL'),
          ('SESS-36','ELECTRICAL_ENGINEER','FULL'),('SESS-36','SERVICE_ENGINEER','FULL'),
          ('SESS-37','ELECTRICAL_ENGINEER','FULL'),('SESS-37','SERVICE_ENGINEER','FULL'),
          ('SESS-38','SERVICE_ENGINEER','FULL'),('SESS-39','PRODUCTION_OPERATOR','FULL'),
          ('SESS-40','SOFTWARE_DEVELOPER','FULL'),
          ('SESS-41','STORES_MANAGER','FULL'),('SESS-41','ACCOUNTS_ASSISTANT','SUPPORT'),
          ('SESS-42','SERVICE_ENGINEER','FULL')
        ), required AS (
          SELECT c."Id" "CompanyId",e."Id" "EmployeeId",e."EmployeeCode",r."Id" "RoleId",r."Code" "RoleCode",d."AssignmentType"
          FROM desired d
          JOIN advance.employees e ON e."EmployeeCode"=d."EmployeeCode" AND upper(e."Status")='ACTIVE'
          JOIN advance.roles r ON r."Code"=d."RoleCode" AND r."IsActive" AND r."IsEmployeeAssignable"
          JOIN advance.employee_company_assignments eca ON eca."EmployeeId"=e."Id" AND eca."IsActive" AND eca."Status"='ACTIVE'
            AND eca."EffectiveFrom"<=DATE '2026-09-05' AND (eca."EffectiveTo" IS NULL OR eca."EffectiveTo">=DATE '2026-09-05')
          JOIN advance.companies c ON c."Id"=eca."CompanyId" AND c."IsActive"
          JOIN advance.company_role_activations cra ON cra."CompanyId"=c."Id" AND cra."RoleId"=r."Id" AND cra."IsEnabled"
            AND cra."EffectiveFrom"<=DATE '2026-09-05' AND (cra."EffectiveTo" IS NULL OR cra."EffectiveTo">=DATE '2026-09-05')
        )
        INSERT INTO advance.employee_role_assignments
          ("Id","CompanyId","EmployeeId","RoleId","EffectiveFrom","EffectiveTo","AssignmentType",
           "ApprovalStatus","Remarks","CreatedAt","CreatedBy","Version")
        SELECT md5('revised-phase2|'||x."CompanyId"||'|'||x."EmployeeCode"||'|'||x."RoleCode")::uuid,
          x."CompanyId",x."EmployeeId",x."RoleId",DATE '2026-09-05',NULL,x."AssignmentType",
          'Approved','Technical Director confirmed revised Phase 2 assignment',
          TIMESTAMPTZ '2026-09-05 00:00:00+00',
          CASE WHEN x."EmployeeCode"='SESS-12' AND x."RoleCode"='IT_MANAGER'
            THEN 'migration-employee-role-governance-phase2' ELSE 'migration-revised-role-governance-phase2' END,0
        FROM required x
        WHERE NOT EXISTS (
          SELECT 1 FROM advance.employee_role_assignments a
          WHERE a."CompanyId"=x."CompanyId" AND a."EmployeeId"=x."EmployeeId" AND a."RoleId"=x."RoleId"
            AND a."ApprovalStatus" IN ('Approved','SeedApproved')
            AND a."EffectiveFrom"<=DATE '2026-09-05' AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=DATE '2026-09-05'));

        WITH desired("EmployeeCode","RoleCode","AssignmentType") AS (
          VALUES
          ('SESS-05','SERVICE_ENGINEER','FULL'),('SESS-05','TECHNICAL_SUPPORT_MANAGER','SUPPORT'),
          ('SESS-15','PURCHASE_MANAGER','FULL'),('SESS-15','PURCHASE_EXECUTIVE','FULL'),('SESS-15','STORES_EXECUTIVE','SUPPORT'),
          ('SESS-28','ACCOUNTS_ASSISTANT','FULL'),('SESS-28','SERVICE_COORDINATOR','SUPPORT'),
          ('SESS-41','STORES_MANAGER','FULL'),('SESS-41','ACCOUNTS_ASSISTANT','SUPPORT')
        )
        UPDATE advance.employee_role_assignments a
        SET "AssignmentType"=d."AssignmentType"
        FROM desired d,advance.employees e,advance.roles r
        WHERE e."EmployeeCode"=d."EmployeeCode" AND r."Code"=d."RoleCode"
          AND a."EmployeeId"=e."Id" AND a."RoleId"=r."Id"
          AND a."ApprovalStatus" IN ('Approved','SeedApproved')
          AND a."EffectiveFrom"<=DATE '2026-09-05' AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=DATE '2026-09-05');

        WITH desired("EmployeeCode","RoleCode","AssignmentType") AS (
          VALUES
          ('SESS-01','TECHNICAL_DIRECTOR','FULL'),
          ('SESS-02','MANAGING_DIRECTOR','FULL'),
          ('SESS-03','HOUSEKEEPING_ASSISTANT','FULL'),
          ('SESS-04','TECHNICAL_SUPPORT_MANAGER','FULL'),
          ('SESS-05','SERVICE_ENGINEER','FULL'),('SESS-05','TECHNICAL_SUPPORT_MANAGER','SUPPORT'),
          ('SESS-06','ELECTRICAL_ENGINEER','FULL'),('SESS-06','SERVICE_ENGINEER','FULL'),
          ('SESS-07','PRODUCTION_OPERATOR','FULL'),('SESS-08','PRODUCTION_OPERATOR','FULL'),
          ('SESS-09','SERVICE_ENGINEER','FULL'),
          ('SESS-10','ELECTRICAL_ENGINEER','FULL'),('SESS-10','SERVICE_ENGINEER','FULL'),
          ('SESS-11','SERVICE_ENGINEER','FULL'),('SESS-12','IT_MANAGER','FULL'),
          ('SESS-13','PRODUCTION_COORDINATOR','FULL'),('SESS-14','ACCOUNTS_MANAGER','FULL'),
          ('SESS-15','PURCHASE_MANAGER','FULL'),('SESS-15','PURCHASE_EXECUTIVE','FULL'),('SESS-15','STORES_EXECUTIVE','SUPPORT'),
          ('SESS-16','STORES_ASSISTANT','FULL'),
          ('SESS-17','DESIGN_ENGINEER','FULL'),('SESS-17','SERVICE_ENGINEER','FULL'),
          ('SESS-18','ELECTRICAL_ENGINEER','FULL'),('SESS-18','SERVICE_ENGINEER','FULL'),
          ('SESS-19','DESIGN_ENGINEER','FULL'),('SESS-19','SERVICE_ENGINEER','FULL'),
          ('SESS-20','ELECTRICAL_ENGINEER','FULL'),('SESS-20','SERVICE_ENGINEER','FULL'),
          ('SESS-21','HR_MANAGER','FULL'),('SESS-22','PRODUCTION_OPERATOR','FULL'),
          ('SESS-23','PRODUCTION_OPERATOR','FULL'),('SESS-24','PRODUCTION_OPERATOR','FULL'),
          ('SESS-25','PRODUCTION_MANAGER','FULL'),
          ('SESS-26','MAINTENANCE_ENGINEER','FULL'),('SESS-26','SERVICE_ENGINEER','FULL'),
          ('SESS-27','ELECTRICAL_ENGINEER','FULL'),('SESS-27','SERVICE_ENGINEER','FULL'),
          ('SESS-28','ACCOUNTS_ASSISTANT','FULL'),('SESS-28','SERVICE_COORDINATOR','SUPPORT'),
          ('SESS-29','JUNIOR_ENGINEER','FULL'),('SESS-29','SERVICE_ENGINEER','FULL'),
          ('SESS-30','PLC_ENGINEER','FULL'),('SESS-30','SERVICE_ENGINEER','FULL'),
          ('SESS-31','SERVICE_ENGINEER','FULL'),('SESS-32','SOFTWARE_DEVELOPER','FULL'),
          ('SESS-33','QC_MANAGER','FULL'),('SESS-34','SERVICE_ENGINEER','FULL'),
          ('SESS-35','STORES_EXECUTIVE','FULL'),
          ('SESS-36','ELECTRICAL_ENGINEER','FULL'),('SESS-36','SERVICE_ENGINEER','FULL'),
          ('SESS-37','ELECTRICAL_ENGINEER','FULL'),('SESS-37','SERVICE_ENGINEER','FULL'),
          ('SESS-38','SERVICE_ENGINEER','FULL'),('SESS-39','PRODUCTION_OPERATOR','FULL'),
          ('SESS-40','SOFTWARE_DEVELOPER','FULL'),
          ('SESS-41','STORES_MANAGER','FULL'),('SESS-41','ACCOUNTS_ASSISTANT','SUPPORT'),
          ('SESS-42','SERVICE_ENGINEER','FULL')
        )
        UPDATE advance.employee_role_assignments a
        SET "EffectiveTo"=GREATEST(a."EffectiveFrom",DATE '2026-09-04'),"ApprovalStatus"='Ended',
            "EndReason"='Superseded by Technical Director confirmed revised Phase 2 assignment list',
            "EndedAt"=TIMESTAMPTZ '2026-09-05 00:00:00+00',"EndedBy"='migration-revised-role-governance-phase2',
            "UpdatedAt"=TIMESTAMPTZ '2026-09-05 00:00:00+00',"UpdatedBy"='migration-revised-role-governance-phase2-ended',
            "Version"=a."Version"+1
        FROM advance.employees e,advance.roles r
        WHERE a."EmployeeId"=e."Id" AND a."RoleId"=r."Id"
          AND a."ApprovalStatus" IN ('Approved','SeedApproved')
          AND a."EffectiveFrom"<=DATE '2026-09-05' AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=DATE '2026-09-05')
          AND NOT EXISTS (SELECT 1 FROM desired d WHERE d."EmployeeCode"=e."EmployeeCode" AND d."RoleCode"=r."Code");
        INSERT INTO advance.employee_role_assignment_events
          ("Id","CompanyId","EmployeeId","ActorEmployeeId","AssignmentId","Operation","FromRoleCode","ToRoleCode",
           "FromAssignmentType","ToAssignmentType","PreviousEffectiveFrom","PreviousEffectiveTo",
           "NewEffectiveFrom","NewEffectiveTo","EffectiveOn","Reason","ActorLoginId","ActorRoleCode",
           "CreatedAt","CreatedBy","Version")
        SELECT md5('revised-phase2-end-event|'||a."Id")::uuid,a."CompanyId",a."EmployeeId",
          CASE WHEN e."EmployeeCode"='SESS-01' THEN md."Id" ELSE td."Id" END,a."Id",'END_ASSIGNMENT',r."Code",NULL,
          a."AssignmentType",NULL,a."EffectiveFrom",NULL,a."EffectiveFrom",a."EffectiveTo",DATE '2026-09-05',
          a."EndReason",'migration-revised-role-governance-phase2',
          CASE WHEN e."EmployeeCode"='SESS-01' THEN 'MANAGING_DIRECTOR' ELSE 'TECHNICAL_DIRECTOR' END,
          TIMESTAMPTZ '2026-09-05 00:00:00+00','migration-revised-role-governance-phase2',0
        FROM advance.employee_role_assignments a
        JOIN advance.employees e ON e."Id"=a."EmployeeId"
        JOIN advance.roles r ON r."Id"=a."RoleId"
        CROSS JOIN (SELECT "Id" FROM advance.employees WHERE "EmployeeCode"='SESS-01') td
        CROSS JOIN (SELECT "Id" FROM advance.employees WHERE "EmployeeCode"='SESS-02') md
        WHERE a."UpdatedBy"='migration-revised-role-governance-phase2-ended'
        ON CONFLICT ("Id") DO NOTHING;
        INSERT INTO advance.employee_role_assignment_events
          ("Id","CompanyId","EmployeeId","ActorEmployeeId","AssignmentId","Operation","FromRoleCode","ToRoleCode",
           "FromAssignmentType","ToAssignmentType","PreviousEffectiveFrom","PreviousEffectiveTo",
           "NewEffectiveFrom","NewEffectiveTo","EffectiveOn","Reason","ActorLoginId","ActorRoleCode",
           "CreatedAt","CreatedBy","Version")
        SELECT md5('revised-phase2-event|'||a."Id")::uuid,a."CompanyId",a."EmployeeId",
          CASE WHEN e."EmployeeCode"='SESS-01' THEN md."Id" ELSE td."Id" END,a."Id",'BASELINE_CONFIRM',NULL,r."Code",
          NULL,a."AssignmentType",NULL,NULL,a."EffectiveFrom",a."EffectiveTo",DATE '2026-09-05',
          'Technical Director confirmed revised Phase 2 assignment','migration-revised-role-governance-phase2',
          CASE WHEN e."EmployeeCode"='SESS-01' THEN 'MANAGING_DIRECTOR' ELSE 'TECHNICAL_DIRECTOR' END,
          TIMESTAMPTZ '2026-09-05 00:00:00+00','migration-revised-role-governance-phase2',0
        FROM advance.employee_role_assignments a
        JOIN advance.employees e ON e."Id"=a."EmployeeId"
        JOIN advance.roles r ON r."Id"=a."RoleId"
        CROSS JOIN (SELECT "Id" FROM advance.employees WHERE "EmployeeCode"='SESS-01') td
        CROSS JOIN (SELECT "Id" FROM advance.employees WHERE "EmployeeCode"='SESS-02') md
        WHERE a."ApprovalStatus" IN ('Approved','SeedApproved')
          AND a."EffectiveFrom"<=DATE '2026-09-05' AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=DATE '2026-09-05')
          AND EXISTS (
            SELECT 1 FROM (VALUES
              ('SESS-01','TECHNICAL_DIRECTOR'),('SESS-02','MANAGING_DIRECTOR'),('SESS-03','HOUSEKEEPING_ASSISTANT'),
              ('SESS-04','TECHNICAL_SUPPORT_MANAGER'),('SESS-05','SERVICE_ENGINEER'),('SESS-05','TECHNICAL_SUPPORT_MANAGER'),
              ('SESS-06','ELECTRICAL_ENGINEER'),('SESS-06','SERVICE_ENGINEER'),('SESS-07','PRODUCTION_OPERATOR'),
              ('SESS-08','PRODUCTION_OPERATOR'),('SESS-09','SERVICE_ENGINEER'),('SESS-10','ELECTRICAL_ENGINEER'),
              ('SESS-10','SERVICE_ENGINEER'),('SESS-11','SERVICE_ENGINEER'),('SESS-12','IT_MANAGER'),
              ('SESS-13','PRODUCTION_COORDINATOR'),('SESS-14','ACCOUNTS_MANAGER'),('SESS-15','PURCHASE_MANAGER'),
              ('SESS-15','PURCHASE_EXECUTIVE'),('SESS-15','STORES_EXECUTIVE'),('SESS-16','STORES_ASSISTANT'),
              ('SESS-17','DESIGN_ENGINEER'),('SESS-17','SERVICE_ENGINEER'),('SESS-18','ELECTRICAL_ENGINEER'),
              ('SESS-18','SERVICE_ENGINEER'),('SESS-19','DESIGN_ENGINEER'),('SESS-19','SERVICE_ENGINEER'),
              ('SESS-20','ELECTRICAL_ENGINEER'),('SESS-20','SERVICE_ENGINEER'),('SESS-21','HR_MANAGER'),
              ('SESS-22','PRODUCTION_OPERATOR'),('SESS-23','PRODUCTION_OPERATOR'),('SESS-24','PRODUCTION_OPERATOR'),
              ('SESS-25','PRODUCTION_MANAGER'),('SESS-26','MAINTENANCE_ENGINEER'),('SESS-26','SERVICE_ENGINEER'),
              ('SESS-27','ELECTRICAL_ENGINEER'),('SESS-27','SERVICE_ENGINEER'),('SESS-28','ACCOUNTS_ASSISTANT'),
              ('SESS-28','SERVICE_COORDINATOR'),('SESS-29','JUNIOR_ENGINEER'),('SESS-29','SERVICE_ENGINEER'),
              ('SESS-30','PLC_ENGINEER'),('SESS-30','SERVICE_ENGINEER'),('SESS-31','SERVICE_ENGINEER'),
              ('SESS-32','SOFTWARE_DEVELOPER'),('SESS-33','QC_MANAGER'),('SESS-34','SERVICE_ENGINEER'),
              ('SESS-35','STORES_EXECUTIVE'),('SESS-36','ELECTRICAL_ENGINEER'),('SESS-36','SERVICE_ENGINEER'),
              ('SESS-37','ELECTRICAL_ENGINEER'),('SESS-37','SERVICE_ENGINEER'),('SESS-38','SERVICE_ENGINEER'),
              ('SESS-39','PRODUCTION_OPERATOR'),('SESS-40','SOFTWARE_DEVELOPER'),('SESS-41','STORES_MANAGER'),
              ('SESS-41','ACCOUNTS_ASSISTANT'),('SESS-42','SERVICE_ENGINEER')
            ) d("EmployeeCode","RoleCode") WHERE d."EmployeeCode"=e."EmployeeCode" AND d."RoleCode"=r."Code"
          ) ON CONFLICT ("Id") DO NOTHING;

        CREATE OR REPLACE FUNCTION advance.resolve_employee_role_authority(
          actor_employee uuid,actor_company uuid,on_date date,operation text,required_roles text[])
        RETURNS TABLE("AssignmentId" uuid,"RoleCode" text,"AssignmentType" text)
        LANGUAGE plpgsql STABLE AS $f$
        DECLARE support_denied boolean;
        BEGIN
          support_denied := lower(operation) ~ '(^|:)(approve|reject|cancel|reverse|deactivate)'
            OR lower(operation) LIKE '%permission%' OR lower(operation) LIKE '%role-administration%';
          RETURN QUERY
          SELECT a."Id",r."Code"::text,a."AssignmentType"::text
          FROM advance.employee_role_assignments a
          JOIN advance.roles r ON r."Id"=a."RoleId" AND r."IsActive"
          JOIN advance.company_role_activations ca ON ca."CompanyId"=a."CompanyId" AND ca."RoleId"=a."RoleId"
            AND ca."IsEnabled" AND ca."EffectiveFrom"<=on_date AND (ca."EffectiveTo" IS NULL OR ca."EffectiveTo">=on_date)
          WHERE a."EmployeeId"=actor_employee AND a."CompanyId"=actor_company
            AND a."ApprovalStatus" IN ('Approved','SeedApproved')
            AND a."EffectiveFrom"<=on_date AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=on_date)
            AND r."Code"=ANY(required_roles)
            AND (NOT support_denied OR a."AssignmentType"<>'SUPPORT')
          ORDER BY CASE
            WHEN r."Code" LIKE '%ASSISTANT' OR r."Code" LIKE '%OPERATOR' OR r."Code" LIKE 'JUNIOR_%' THEN 10
            WHEN r."Code" LIKE '%EXECUTIVE' OR r."Code" LIKE '%ENGINEER' OR r."Code" LIKE '%COORDINATOR' THEN 20
            WHEN r."Code" LIKE '%MANAGER' THEN 30 WHEN r."Code" LIKE '%DIRECTOR' THEN 40 ELSE 25 END,
            CASE a."AssignmentType" WHEN 'SUPPORT' THEN 0 WHEN 'TEMPORARY' THEN 1 ELSE 2 END,a."Id"
          LIMIT 1;
          IF NOT FOUND THEN
            RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Required role: '||array_to_string(required_roles,' or ')||'.';
          END IF;
        END $f$;

        CREATE FUNCTION advance.guard_employee_role_administration()
        RETURNS trigger LANGUAGE plpgsql AS $f$
        DECLARE target_employee uuid; target_company uuid; authority uuid; actor uuid; actor_type text; actor_role text;
        BEGIN
          target_employee:=COALESCE(NEW."EmployeeId",OLD."EmployeeId"); target_company:=COALESCE(NEW."CompanyId",OLD."CompanyId");
          authority:=nullif(current_setting('sess.role_authority_assignment_id',true),'')::uuid;
          SELECT a."EmployeeId",a."AssignmentType",r."Code" INTO actor,actor_type,actor_role
          FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId"
          WHERE a."Id"=authority AND a."CompanyId"=target_company AND a."ApprovalStatus" IN ('Approved','SeedApproved')
            AND a."EffectiveFrom"<=CURRENT_DATE AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=CURRENT_DATE)
            AND a."AssignmentType" IN ('FULL','TEMPORARY')
            AND r."Code" IN ('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR','IT_MANAGER') AND r."IsActive";
          IF actor IS NULL THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='FULL or effective TEMPORARY configuration authority is required.'; END IF;
          IF actor=target_employee THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Self role assignment is prohibited.'; END IF;
          RETURN COALESCE(NEW,OLD);
        END $f$;
        CREATE TRIGGER "TR_employee_role_administration_guard"
          BEFORE INSERT OR UPDATE OR DELETE ON advance.employee_role_assignments
          FOR EACH ROW EXECUTE FUNCTION advance.guard_employee_role_administration();

        CREATE FUNCTION advance.guard_role_assignment_event_immutable()
        RETURNS trigger LANGUAGE plpgsql AS $f$ BEGIN
          RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Employee role assignment history is immutable.';
        END $f$;
        CREATE TRIGGER "TR_employee_role_assignment_event_immutable"
          BEFORE UPDATE OR DELETE ON advance.employee_role_assignment_events
          FOR EACH ROW EXECUTE FUNCTION advance.guard_role_assignment_event_immutable();

        DO $f$ BEGIN
          IF to_regclass('advance.rev869b_command_requests') IS NOT NULL THEN
            ALTER TABLE advance.rev869b_command_requests ADD COLUMN "ActorRoleAssignmentId" uuid NULL;
            ALTER TABLE advance.rev869b_command_requests
              ADD CONSTRAINT "FK_rev869b_command_request_role_assignment"
              FOREIGN KEY ("ActorRoleAssignmentId") REFERENCES advance.employee_role_assignments("Id") ON DELETE RESTRICT;
          END IF;
        END $f$;

        CREATE OR REPLACE FUNCTION advance.rev869b_register_command_request(
          organization text,operation text,idempotency_sha bytea,request_sha bytea,actor_employee uuid,
          identity_issuer text,identity_subject text,actor_role text,actor_assignment uuid)
        RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
        DECLARE command_id uuid; existing record; company_id uuid;
        BEGIN
          IF session_user<>'nexa_rev869b_command_audit' OR organization='' OR operation=''
             OR octet_length(idempotency_sha)<>32 OR octet_length(request_sha)<>32 THEN
            RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Exact assignment-bound command request required';
          END IF;
          SELECT "Id" INTO company_id FROM advance.companies WHERE "Code"=organization AND "IsActive";
          IF NOT EXISTS (
            SELECT 1 FROM advance.resolve_employee_role_authority(actor_employee,company_id,CURRENT_DATE,operation,ARRAY[actor_role]) x
            WHERE x."AssignmentId"=actor_assignment AND x."RoleCode"=actor_role
          ) THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Resolved command role assignment is not effective.'; END IF;
          SELECT * INTO existing FROM advance.rev869b_command_requests r
          WHERE r."OrganizationId"=organization AND r."Operation"=operation AND r."IdempotencyKeySha256"=idempotency_sha FOR UPDATE;
          IF FOUND THEN
            IF existing."RequestSha256"<>request_sha OR existing."ActorEmployeeId"<>actor_employee OR
               existing."IdentityIssuer"<>identity_issuer OR existing."IdentitySubject"<>identity_subject OR
               existing."ActorRole"<>actor_role OR existing."ActorRoleAssignmentId" IS DISTINCT FROM actor_assignment
            THEN RAISE EXCEPTION USING ERRCODE='23505',CONSTRAINT='rev869b_command_request_replay_mismatch',MESSAGE='Idempotency key reuse mismatch'; END IF;
            RETURN existing."CommandId";
          END IF;
          command_id:=gen_random_uuid();
          INSERT INTO advance.rev869b_command_requests
            ("CommandId","OrganizationId","Operation","IdempotencyKeySha256","RequestSha256","ActorEmployeeId",
             "IdentityIssuer","IdentitySubject","ActorRole","RegisteredAt","RegisteredBy","ActorRoleAssignmentId")
          VALUES(command_id,organization,operation,idempotency_sha,request_sha,actor_employee,identity_issuer,
            identity_subject,actor_role,clock_timestamp(),session_user,actor_assignment);
          RETURN command_id;
        END $f$;
        REVOKE ALL ON FUNCTION advance.rev869b_register_command_request(text,text,bytea,bytea,uuid,text,text,text,uuid) FROM PUBLIC;
        DO $f$ BEGIN
          IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_rev869b_command_audit') THEN
            GRANT EXECUTE ON FUNCTION advance.rev869b_register_command_request(text,text,bytea,bytea,uuid,text,text,text,uuid) TO nexa_rev869b_command_audit;
          END IF;
        END $f$;

        CREATE FUNCTION advance.guard_sensitive_audit_assignment()
        RETURNS trigger LANGUAGE plpgsql AS $f$
        DECLARE assignment_type text; assignment_role text; assignment_company uuid;
        BEGIN
          IF NEW."Result"='Success' AND (
             lower(NEW."Action") ~ '(approve|reject|cancel|reverse|deactivate)'
             OR lower(NEW."Module") LIKE '%permission%' OR lower(NEW."Action") LIKE '%role%assignment%') THEN
            SELECT a."AssignmentType",r."Code",a."CompanyId" INTO assignment_type,assignment_role,assignment_company
            FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId"
            WHERE a."Id"=NEW."ResolvedRoleAssignmentId"
              AND a."ApprovalStatus" IN ('Approved','SeedApproved')
              AND a."EffectiveFrom"<=CURRENT_DATE AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=CURRENT_DATE);
            IF assignment_type IS NULL OR assignment_type='SUPPORT' OR assignment_role<>NEW."ActorRoleCode"
               OR (NEW."CompanyId" IS NOT NULL AND assignment_company<>NEW."CompanyId") THEN
              RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Sensitive operation requires a matching FULL or effective TEMPORARY assignment.';
            END IF;
          END IF;
          RETURN NEW;
        END $f$;
        CREATE TRIGGER "TR_sensitive_audit_assignment_guard"
          BEFORE INSERT ON advance.audit_logs FOR EACH ROW EXECUTE FUNCTION advance.guard_sensitive_audit_assignment();
        """;

    internal const string Down = """
        DROP TRIGGER IF EXISTS "TR_sensitive_audit_assignment_guard" ON advance.audit_logs;
        DROP FUNCTION IF EXISTS advance.guard_sensitive_audit_assignment();
        DROP FUNCTION IF EXISTS advance.rev869b_register_command_request(text,text,bytea,bytea,uuid,text,text,text,uuid);
        DO $f$ BEGIN
          IF to_regclass('advance.rev869b_command_requests') IS NOT NULL THEN
            ALTER TABLE advance.rev869b_command_requests DROP CONSTRAINT IF EXISTS "FK_rev869b_command_request_role_assignment";
            ALTER TABLE advance.rev869b_command_requests DROP COLUMN IF EXISTS "ActorRoleAssignmentId";
          END IF;
        END $f$;
        DROP TRIGGER IF EXISTS "TR_employee_role_assignment_event_immutable" ON advance.employee_role_assignment_events;
        DROP FUNCTION IF EXISTS advance.guard_role_assignment_event_immutable();
        DROP TRIGGER IF EXISTS "TR_employee_role_administration_guard" ON advance.employee_role_assignments;
        DROP FUNCTION IF EXISTS advance.guard_employee_role_administration();
        DROP FUNCTION IF EXISTS advance.resolve_employee_role_authority(uuid,uuid,date,text,text[]);
        ALTER TABLE advance.employee_role_assignments DROP CONSTRAINT IF EXISTS "EX_employee_role_assignment_no_overlap";
        DELETE FROM advance.role_page_permissions WHERE "CreatedBy"='migration-revised-role-governance-phase2';
        DELETE FROM advance.employee_role_assignment_events WHERE "CreatedBy"='migration-revised-role-governance-phase2';
        DELETE FROM advance.employee_role_assignments
        WHERE "CreatedBy"='migration-revised-role-governance-phase2'
           OR ("CreatedBy"='migration-employee-role-governance-phase2'
               AND "Remarks"='Technical Director confirmed revised Phase 2 assignment');
        UPDATE advance.employee_role_assignments
        SET "EffectiveTo"=NULL,"ApprovalStatus"='SeedApproved',"EndReason"=NULL,"EndedAt"=NULL,"EndedBy"=NULL,
            "UpdatedAt"=NULL,"UpdatedBy"=NULL,"Version"=GREATEST("Version"-1,0)
        WHERE "UpdatedBy"='migration-revised-role-governance-phase2-ended';        UPDATE advance.employee_role_assignments SET "AssignmentType"='FULL'
          WHERE "UpdatedBy"='migration-revised-role-governance-phase2';
        """;
}
