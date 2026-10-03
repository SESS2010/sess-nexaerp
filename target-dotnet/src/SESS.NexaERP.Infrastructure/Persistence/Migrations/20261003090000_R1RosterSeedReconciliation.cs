using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20261003090000_R1RosterSeedReconciliation")]
public sealed class R1RosterSeedReconciliation : Migration
{
    private const string Marker = "R1RosterSeedReconciliation";
    private const string Desired = """
        CREATE TEMP TABLE r1_roster_desired ON COMMIT DROP AS
        SELECT c."Id" company_id,e."Id" employee_id,r."Id" role_id,v.employee_code,v.role_code
        FROM (VALUES
          ('SESS-01','TECHNICAL_DIRECTOR'),('SESS-02','MANAGING_DIRECTOR'),('SESS-02','CHIEF_FINANCIAL_OFFICER'),
          ('SESS-12','IT_MANAGER'),('SESS-14','ACCOUNTS_MANAGER'),('SESS-15','PURCHASE_MANAGER'),('SESS-15','PURCHASE_EXECUTIVE'),
          ('SESS-16','STORES_ASSISTANT'),('SESS-17','DESIGN_ENGINEER'),('SESS-17','SERVICE_ENGINEER'),
          ('SESS-19','DESIGN_ENGINEER'),('SESS-19','SERVICE_ENGINEER'),('SESS-21','HR_EXECUTIVE'),('SESS-21','HR_MANAGER'),
          ('SESS-25','PRODUCTION_MANAGER'),('SESS-28','ACCOUNTS_ASSISTANT'),('SESS-33','QC_MANAGER'),
          ('SESS-35','STORES_EXECUTIVE'),('SESS-41','STORES_MANAGER')) v(employee_code,role_code)
        JOIN advance.employees e ON e."EmployeeCode"=v.employee_code AND e."Status"='Active'
        JOIN advance.roles r ON r."Code"=v.role_code AND r."IsActive" AND r."IsEmployeeAssignable"
        CROSS JOIN advance.companies c WHERE c."Code" IN('SESS_PVT_LTD','SESS_PROPRIETORSHIP')
          AND c."IsActive" AND c."Status"='ACTIVE';
        """;
    // Permanent rollback journals retain every generation; Down never deletes assignments or events.
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            LOCK TABLE advance.employee_role_assignments,advance.employee_role_assignment_events,
              advance.company_role_activations,advance.employee_company_assignments IN SHARE ROW EXCLUSIVE MODE;
            CREATE TABLE IF NOT EXISTS advance.r1_roster_reconciliation_runs(
              id uuid PRIMARY KEY,applied boolean NOT NULL,created_at timestamptz NOT NULL DEFAULT now(),
              rolled_back_at timestamptz);
            CREATE TABLE IF NOT EXISTS advance.r1_roster_reconciliation_entries(
              run_id uuid NOT NULL REFERENCES advance.r1_roster_reconciliation_runs(id),assignment_id uuid NOT NULL,
              before_row jsonb,after_row jsonb NOT NULL,
              after_event_ids jsonb NOT NULL DEFAULT '[]'::jsonb,introduced boolean NOT NULL DEFAULT false,PRIMARY KEY(run_id,assignment_id));
            CREATE TABLE IF NOT EXISTS advance.r1_roster_reconciliation_activations(
              run_id uuid NOT NULL REFERENCES advance.r1_roster_reconciliation_runs(id),activation_id uuid NOT NULL,
              before_row jsonb NOT NULL,after_row jsonb NOT NULL,PRIMARY KEY(run_id,activation_id));
            REVOKE ALL ON advance.r1_roster_reconciliation_runs,advance.r1_roster_reconciliation_entries,
              advance.r1_roster_reconciliation_activations FROM PUBLIC;
            DO $permissions$ BEGIN
              IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_runtime') THEN
                REVOKE ALL ON advance.r1_roster_reconciliation_runs,advance.r1_roster_reconciliation_entries,
                  advance.r1_roster_reconciliation_activations FROM nexa_erp_runtime;
              END IF;
            END $permissions$;
            """);
        migrationBuilder.Sql(Desired);
        migrationBuilder.Sql($$"""
            DO $reconcile$
            DECLARE run uuid:=gen_random_uuid(); item record; desired record; previous jsonb;
              authority uuid; actor uuid; actor_role text; new_id uuid; changed boolean;
            BEGIN
              IF EXISTS(SELECT 1 FROM advance.r1_roster_reconciliation_runs WHERE applied)
                OR (SELECT count(*) FROM r1_roster_desired)<>38 THEN
                RAISE EXCEPTION 'R1 roster requires the complete approved manifest and no active reconciliation.';
              END IF;
              IF EXISTS(SELECT 1 FROM r1_roster_desired d WHERE
                (SELECT count(*) FROM advance.company_role_activations ca WHERE ca."CompanyId"=d.company_id
                  AND ca."RoleId"=d.role_id AND ca."IsEnabled" AND
                    (ca."EffectiveFrom"<=DATE '2026-10-10' OR (d.role_code='CHIEF_FINANCIAL_OFFICER'
                      AND ca."CreatedBy"='migration-governed-inventory-periods' AND ca."EffectiveTo" IS NULL
                      AND ca."EffectiveFrom"=(ca."CreatedAt" AT TIME ZONE 'UTC')::date
                      AND ((ca."Version"=0 AND ca."UpdatedAt" IS NULL) OR ca."UpdatedBy"='{{Marker}}.rollback')))
                  AND (ca."EffectiveTo" IS NULL OR ca."EffectiveTo">=DATE '2026-10-10'))<>1
                OR (SELECT count(*) FROM advance.employee_company_assignments ca WHERE ca."CompanyId"=d.company_id
                  AND ca."EmployeeId"=d.employee_id AND ca."IsActive" AND ca."Status"='ACTIVE'
                  AND ca."EffectiveFrom"<=DATE '2026-10-10' AND (ca."EffectiveTo" IS NULL OR ca."EffectiveTo">=DATE '2026-10-10'))<>1) THEN
                RAISE EXCEPTION 'R1 roster refuses missing or ambiguous company access/role activation.';
              END IF;
              -- Refuse unknown extras and partial or future assignments rather than silently inventing policy.
              IF EXISTS(SELECT 1 FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId"
                JOIN advance.employees e ON e."Id"=a."EmployeeId"
                WHERE EXISTS(SELECT 1 FROM r1_roster_desired d WHERE d.company_id=a."CompanyId" AND d.employee_id=a."EmployeeId")
                  AND a."ApprovalStatus" IN('Approved','SeedApproved') AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=DATE '2026-10-10')
                  AND NOT EXISTS(SELECT 1 FROM r1_roster_desired d WHERE d.company_id=a."CompanyId" AND d.employee_id=a."EmployeeId" AND d.role_id=a."RoleId")
                  AND (a."EffectiveFrom">DATE '2026-10-09' OR (e."EmployeeCode",r."Code") NOT IN
                    (('SESS-15','STORES_EXECUTIVE'),
                     ('SESS-28','SERVICE_COORDINATOR'),('SESS-41','ACCOUNTS_ASSISTANT')))) THEN
                RAISE EXCEPTION 'R1 roster refuses an unexpected extra assignment; TD review is required.';
              END IF;
              IF EXISTS(SELECT 1 FROM advance.employee_role_assignments a JOIN r1_roster_desired d
                ON d.company_id=a."CompanyId" AND d.employee_id=a."EmployeeId" AND d.role_id=a."RoleId"
                WHERE a."ApprovalStatus" IN('Approved','SeedApproved') AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=DATE '2026-10-10')
                  AND (a."AssignmentType"<>'FULL' OR a."EffectiveTo" IS NOT NULL OR
                    (a."EffectiveFrom">DATE '2026-10-10' AND NOT
                      (d.employee_code='SESS-02' AND d.role_code='CHIEF_FINANCIAL_OFFICER'
                       AND a."CreatedBy"='migration-governed-inventory-periods'
                       AND a."EffectiveFrom"=(a."CreatedAt" AT TIME ZONE 'UTC')::date
                       AND ((a."Version"=0 AND a."UpdatedAt" IS NULL) OR a."UpdatedBy"='{{Marker}}.rollback'))))) THEN
                RAISE EXCEPTION 'R1 roster refuses a changed desired assignment; TD review is required.';
              END IF;
              INSERT INTO advance.r1_roster_reconciliation_runs(id,applied) VALUES(run,true);
              INSERT INTO advance.r1_roster_reconciliation_activations
                SELECT run,ca."Id",to_jsonb(ca),to_jsonb(ca) FROM advance.company_role_activations ca
                JOIN r1_roster_desired d ON d.company_id=ca."CompanyId" AND d.role_id=ca."RoleId"
                WHERE d.role_code='CHIEF_FINANCIAL_OFFICER' AND ca."EffectiveFrom">DATE '2026-10-10';
              UPDATE advance.company_role_activations ca SET "EffectiveFrom"=DATE '2026-10-10',
                "Version"=ca."Version"+1,"UpdatedAt"=now(),"UpdatedBy"='{{Marker}}'
                FROM advance.r1_roster_reconciliation_activations b WHERE b.run_id=run AND b.activation_id=ca."Id";
              UPDATE advance.r1_roster_reconciliation_activations b SET after_row=to_jsonb(ca)
                FROM advance.company_role_activations ca WHERE b.run_id=run AND b.activation_id=ca."Id";
              INSERT INTO advance.r1_roster_reconciliation_entries(run_id,assignment_id,before_row,after_row)
                SELECT run,a."Id",to_jsonb(a),to_jsonb(a) FROM advance.employee_role_assignments a
                WHERE EXISTS(SELECT 1 FROM r1_roster_desired d WHERE d.company_id=a."CompanyId" AND d.employee_id=a."EmployeeId");
              FOR item IN SELECT a."Id",a."CompanyId",a."EmployeeId",a."RoleId",a."EffectiveFrom",a."EffectiveTo",a."AssignmentType",r."Code",to_jsonb(a) original
                FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId"
                WHERE EXISTS(SELECT 1 FROM r1_roster_desired d WHERE d.company_id=a."CompanyId" AND d.employee_id=a."EmployeeId")
                  AND a."ApprovalStatus" IN('Approved','SeedApproved') AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=DATE '2026-10-10')
                  AND (NOT EXISTS(SELECT 1 FROM r1_roster_desired d WHERE d.company_id=a."CompanyId" AND d.employee_id=a."EmployeeId" AND d.role_id=a."RoleId")
                    OR a."EffectiveFrom">DATE '2026-10-10') ORDER BY a."CompanyId",a."Id" LOOP
                SELECT a."Id",a."EmployeeId",r."Code" INTO STRICT authority,actor,actor_role
                  FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId"
                  JOIN advance.employees e ON e."Id"=a."EmployeeId" WHERE a."CompanyId"=item."CompanyId"
                    AND e."EmployeeCode"='SESS-01' AND r."Code"='TECHNICAL_DIRECTOR' AND r."IsActive"
                    AND a."AssignmentType"='FULL' AND a."ApprovalStatus" IN('Approved','SeedApproved')
                    AND a."EffectiveFrom"<=CURRENT_DATE AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=CURRENT_DATE);
                PERFORM set_config('sess.role_authority_assignment_id',authority::text,true);
                IF item."EffectiveFrom">DATE '2026-10-10' THEN
                  UPDATE advance.r1_roster_reconciliation_entries SET introduced=true WHERE run_id=run AND assignment_id=item."Id";
                  -- The merged CFO seed uses CURRENT_DATE: normalize only that untouched late fresh-install baseline.
                  UPDATE advance.employee_role_assignments SET "EffectiveFrom"=DATE '2026-10-10',
                    "Version"="Version"+1,"UpdatedAt"=now(),"UpdatedBy"='{{Marker}}' WHERE "Id"=item."Id";
                ELSE
                  UPDATE advance.employee_role_assignments SET "EffectiveTo"=DATE '2026-10-09',
                    "EndReason"='TD-approved signed R1 roster and 3 Oct exception addendum; extra R1 authority ends 9 Oct',
                    "EndedAt"=now(),"EndedBy"='{{Marker}}',"Version"="Version"+1,"UpdatedAt"=now(),"UpdatedBy"='{{Marker}}'
                    WHERE "Id"=item."Id";
                END IF;
                INSERT INTO advance.employee_role_assignment_events
                  ("Id","CompanyId","EmployeeId","ActorEmployeeId","AssignmentId","Operation","FromRoleCode","ToRoleCode",
                   "FromAssignmentType","ToAssignmentType","PreviousEffectiveFrom","PreviousEffectiveTo",
                   "NewEffectiveFrom","NewEffectiveTo","EffectiveOn","Reason","ActorLoginId","ActorRoleCode","CreatedAt","CreatedBy","Version")
                SELECT gen_random_uuid(),a."CompanyId",a."EmployeeId",actor,a."Id",
                  CASE WHEN item."EffectiveFrom">DATE '2026-10-10' THEN 'R1_SEED_START_ALIGN' ELSE 'R1_ROSTER_END_EXTRA' END,
                  item."Code",item."Code",item."AssignmentType",a."AssignmentType",item."EffectiveFrom",item."EffectiveTo",
                  a."EffectiveFrom",a."EffectiveTo",CASE WHEN item."EffectiveFrom">DATE '2026-10-10' THEN DATE '2026-10-10' ELSE DATE '2026-10-09' END,
                  'TD-approved R1 roster; preserve correct assignments and immutable history','{{Marker}}',actor_role,now(),'{{Marker}}',0
                  FROM advance.employee_role_assignments a WHERE a."Id"=item."Id";
              END LOOP;
              FOR desired IN SELECT d.* FROM r1_roster_desired d WHERE NOT EXISTS
                (SELECT 1 FROM advance.employee_role_assignments a WHERE a."CompanyId"=d.company_id AND a."EmployeeId"=d.employee_id
                 AND a."RoleId"=d.role_id AND a."AssignmentType"='FULL' AND a."ApprovalStatus" IN('Approved','SeedApproved')
                 AND a."EffectiveFrom"<=DATE '2026-10-10' AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=DATE '2026-10-10')) LOOP
                SELECT a."Id",a."EmployeeId",r."Code" INTO STRICT authority,actor,actor_role
                  FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId"
                  JOIN advance.employees e ON e."Id"=a."EmployeeId" WHERE a."CompanyId"=desired.company_id
                    AND e."EmployeeCode"=CASE WHEN desired.employee_code='SESS-01' THEN 'SESS-02' ELSE 'SESS-01' END
                    AND r."Code"=CASE WHEN desired.employee_code='SESS-01' THEN 'MANAGING_DIRECTOR' ELSE 'TECHNICAL_DIRECTOR' END
                    AND r."IsActive" AND a."AssignmentType"='FULL' AND a."ApprovalStatus" IN('Approved','SeedApproved')
                    AND a."EffectiveFrom"<=CURRENT_DATE AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=CURRENT_DATE);
                PERFORM set_config('sess.role_authority_assignment_id',authority::text,true);
                new_id:=NULL;
                SELECT a."Id" INTO new_id FROM advance.employee_role_assignments a WHERE a."CompanyId"=desired.company_id
                  AND a."EmployeeId"=desired.employee_id AND a."RoleId"=desired.role_id AND a."EffectiveFrom"=DATE '2026-10-10'
                  AND a."EffectiveTo"=DATE '2026-10-10' AND a."ApprovalStatus"='Ended'
                  AND a."CreatedBy"='{{Marker}}' AND a."UpdatedBy"='{{Marker}}.rollback';
                IF new_id IS NULL THEN
                  new_id:=gen_random_uuid();
                  INSERT INTO advance.employee_role_assignments
                    ("Id","CompanyId","EmployeeId","RoleId","EffectiveFrom","EffectiveTo","AssignmentType","ApprovalStatus","Remarks","CreatedAt","CreatedBy","Version")
                    VALUES(new_id,desired.company_id,desired.employee_id,desired.role_id,DATE '2026-10-10',NULL,'FULL','SeedApproved',
                      'TD-approved signed R1 roster and 3 Oct approved role exceptions',now(),'{{Marker}}',0);
                ELSE
                  UPDATE advance.employee_role_assignments SET "ApprovalStatus"='SeedApproved',"EffectiveTo"=NULL,
                    "EndReason"=NULL,"EndedAt"=NULL,"EndedBy"=NULL,"Version"="Version"+1,"UpdatedAt"=now(),"UpdatedBy"='{{Marker}}'
                    WHERE "Id"=new_id;
                END IF;
                INSERT INTO advance.employee_role_assignment_events
                  ("Id","CompanyId","EmployeeId","ActorEmployeeId","AssignmentId","Operation","ToRoleCode","ToAssignmentType",
                   "NewEffectiveFrom","EffectiveOn","Reason","ActorLoginId","ActorRoleCode","CreatedAt","CreatedBy","Version")
                  VALUES(gen_random_uuid(),desired.company_id,desired.employee_id,actor,new_id,'R1_ROSTER_ADD_MISSING',desired.role_code,'FULL',
                    DATE '2026-10-10',DATE '2026-10-10','TD-approved signed R1 roster and approved exception pairs','{{Marker}}',actor_role,now(),'{{Marker}}',0);
                INSERT INTO advance.r1_roster_reconciliation_entries(run_id,assignment_id,before_row,after_row,introduced)
                  SELECT run,new_id,NULL,to_jsonb(a),true FROM advance.employee_role_assignments a WHERE a."Id"=new_id
                  ON CONFLICT(run_id,assignment_id) DO UPDATE SET introduced=true;
              END LOOP;
              UPDATE advance.r1_roster_reconciliation_entries b SET after_row=to_jsonb(a),
                after_event_ids=(SELECT coalesce(jsonb_agg(e."Id" ORDER BY e."Id"),'[]'::jsonb)
                  FROM advance.employee_role_assignment_events e WHERE e."AssignmentId"=b.assignment_id)
                FROM advance.employee_role_assignments a WHERE b.run_id=run AND a."Id"=b.assignment_id;
              IF (SELECT count(*) FROM advance.employee_role_assignments a JOIN r1_roster_desired d
                ON d.company_id=a."CompanyId" AND d.employee_id=a."EmployeeId" AND d.role_id=a."RoleId"
                WHERE a."ApprovalStatus" IN('Approved','SeedApproved') AND a."AssignmentType"='FULL'
                  AND a."EffectiveFrom"<=DATE '2026-10-10' AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=DATE '2026-10-10'))<>38 THEN
                RAISE EXCEPTION 'R1 roster reconciliation failed the exact approved cutoff manifest.';
              END IF;
              PERFORM set_config('sess.role_authority_assignment_id','',true);
            END $reconcile$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(Desired);
        migrationBuilder.Sql($$"""
            LOCK TABLE advance.employee_role_assignments,advance.employee_role_assignment_events,
              advance.company_role_activations IN SHARE ROW EXCLUSIVE MODE;
            DO $rollback$
            DECLARE run uuid; item record; authority uuid; actor uuid; actor_role text; previous jsonb; reference record; used boolean;
            BEGIN
              SELECT id INTO STRICT run FROM advance.r1_roster_reconciliation_runs WHERE applied;
              IF EXISTS(SELECT 1 FROM advance.r1_roster_reconciliation_activations b
                LEFT JOIN advance.company_role_activations ca ON ca."Id"=b.activation_id
                WHERE b.run_id=run AND to_jsonb(ca) IS DISTINCT FROM b.after_row) THEN
                RAISE EXCEPTION 'R1 roster rollback refuses changed company role activation.';
              END IF;
              IF EXISTS(SELECT 1 FROM advance.r1_roster_reconciliation_entries b
                LEFT JOIN advance.employee_role_assignments a ON a."Id"=b.assignment_id
                WHERE b.run_id=run AND to_jsonb(a) IS DISTINCT FROM b.after_row)
                OR EXISTS(SELECT 1 FROM advance.employee_role_assignments a WHERE
                  EXISTS(SELECT 1 FROM r1_roster_desired d WHERE d.company_id=a."CompanyId" AND d.employee_id=a."EmployeeId")
                  AND NOT EXISTS(SELECT 1 FROM advance.r1_roster_reconciliation_entries b WHERE b.run_id=run AND b.assignment_id=a."Id")) THEN
                RAISE EXCEPTION 'R1 roster rollback refuses changed, removed or additional assignments.';
              END IF;
              -- New authority cannot be withdrawn once any command/audit/business history has used it.
              FOR reference IN SELECT c.relname,a.attname FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
                JOIN pg_attribute a ON a.attrelid=c.oid WHERE n.nspname='advance' AND c.relkind IN('r','p')
                  AND a.attnum>0 AND NOT a.attisdropped AND a.attname IN('ResolvedRoleAssignmentId','ActorRoleAssignmentId') LOOP
                EXECUTE format('SELECT EXISTS(SELECT 1 FROM advance.%I t JOIN advance.r1_roster_reconciliation_entries b
                  ON b.assignment_id=t.%I WHERE b.run_id=$1 AND b.introduced)',reference.relname,reference.attname) INTO used USING run;
                IF used THEN RAISE EXCEPTION 'R1 roster rollback refuses used new authority; immutable history is retained.'; END IF;
              END LOOP;
              IF EXISTS(SELECT 1 FROM advance.r1_roster_reconciliation_entries b WHERE b.run_id=run
                AND b.after_event_ids IS DISTINCT FROM (SELECT coalesce(jsonb_agg(e."Id" ORDER BY e."Id"),'[]'::jsonb)
                  FROM advance.employee_role_assignment_events e WHERE e."AssignmentId"=b.assignment_id)) THEN
                RAISE EXCEPTION 'R1 roster rollback refuses additional role-administration history.';
              END IF;
              FOR item IN SELECT b.*,a."CompanyId",a."EmployeeId",a."RoleId",a."EffectiveFrom",a."EffectiveTo",a."AssignmentType",r."Code"
                FROM advance.r1_roster_reconciliation_entries b JOIN advance.employee_role_assignments a ON a."Id"=b.assignment_id
                JOIN advance.roles r ON r."Id"=a."RoleId" WHERE b.run_id=run
                  AND (b.before_row IS NULL OR b.before_row IS DISTINCT FROM b.after_row) LOOP
                SELECT a."Id",a."EmployeeId",r."Code" INTO STRICT authority,actor,actor_role
                  FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId"
                  JOIN advance.employees e ON e."Id"=a."EmployeeId" WHERE a."CompanyId"=item."CompanyId"
                    AND e."EmployeeCode"=CASE WHEN item."EmployeeId"=(SELECT "Id" FROM advance.employees WHERE "EmployeeCode"='SESS-01') THEN 'SESS-02' ELSE 'SESS-01' END
                    AND r."Code"=CASE WHEN item."EmployeeId"=(SELECT "Id" FROM advance.employees WHERE "EmployeeCode"='SESS-01') THEN 'MANAGING_DIRECTOR' ELSE 'TECHNICAL_DIRECTOR' END
                    AND r."IsActive" AND a."AssignmentType"='FULL' AND a."ApprovalStatus" IN('Approved','SeedApproved')
                    AND a."EffectiveFrom"<=CURRENT_DATE AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=CURRENT_DATE);
                PERFORM set_config('sess.role_authority_assignment_id',authority::text,true);
                IF item.before_row IS NULL THEN
                  UPDATE advance.employee_role_assignments SET "ApprovalStatus"='Ended',"EffectiveTo"="EffectiveFrom",
                    "EndReason"='R1 roster migration rolled back before authority use; assignment and history retained',
                    "EndedAt"=now(),"EndedBy"='{{Marker}}.rollback',"Version"="Version"+1,
                    "UpdatedAt"=now(),"UpdatedBy"='{{Marker}}.rollback' WHERE "Id"=item.assignment_id;
                ELSE
                  UPDATE advance.employee_role_assignments a SET "EffectiveFrom"=old."EffectiveFrom","EffectiveTo"=old."EffectiveTo",
                    "AssignmentType"=old."AssignmentType","ApprovalStatus"=old."ApprovalStatus","Remarks"=old."Remarks",
                    "EndReason"=old."EndReason","EndedAt"=old."EndedAt","EndedBy"=old."EndedBy",
                    "Version"=a."Version"+1,"UpdatedAt"=now(),"UpdatedBy"='{{Marker}}.rollback'
                    FROM jsonb_populate_record(NULL::advance.employee_role_assignments,item.before_row) old WHERE a."Id"=item.assignment_id;
                END IF;
                INSERT INTO advance.employee_role_assignment_events
                  ("Id","CompanyId","EmployeeId","ActorEmployeeId","AssignmentId","Operation","FromRoleCode","ToRoleCode",
                   "FromAssignmentType","ToAssignmentType","PreviousEffectiveFrom","PreviousEffectiveTo",
                   "NewEffectiveFrom","NewEffectiveTo","EffectiveOn","Reason","ActorLoginId","ActorRoleCode","CreatedAt","CreatedBy","Version")
                SELECT gen_random_uuid(),a."CompanyId",a."EmployeeId",actor,a."Id",'R1_ROSTER_ROLLBACK',item."Code",item."Code",
                  item."AssignmentType",a."AssignmentType",item."EffectiveFrom",item."EffectiveTo",a."EffectiveFrom",a."EffectiveTo",
                  CURRENT_DATE,'Restore previous role validity without deleting assignment/history','{{Marker}}.rollback',actor_role,now(),'{{Marker}}.rollback',0
                  FROM advance.employee_role_assignments a WHERE a."Id"=item.assignment_id;
              END LOOP;
              UPDATE advance.company_role_activations ca SET "EffectiveFrom"=old."EffectiveFrom",
                "Version"=ca."Version"+1,"UpdatedAt"=now(),"UpdatedBy"='{{Marker}}.rollback'
                FROM advance.r1_roster_reconciliation_activations b
                CROSS JOIN LATERAL jsonb_populate_record(NULL::advance.company_role_activations,b.before_row) old
                WHERE b.run_id=run AND ca."Id"=b.activation_id;
              UPDATE advance.r1_roster_reconciliation_runs SET applied=false,rolled_back_at=now() WHERE id=run;
              PERFORM set_config('sess.role_authority_assignment_id','',true);
            END $rollback$;
            """);
    }
}