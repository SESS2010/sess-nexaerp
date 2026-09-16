using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260916170000_QcPolicyDecisions")]
public sealed class QcPolicyDecisions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            DO $guard$ BEGIN
              IF current_setting('server_version_num')::integer < 170000 OR
                 current_database() IN ('postgres','template0','template1') THEN
                RAISE EXCEPTION 'QC policy decisions require PostgreSQL 17 and a non-system database.';
              END IF;
              IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgrelid='advance.qc_inspection_policies'::regclass
                 AND tgname='trg_rev869a_qc_version_guard'
                 AND tgfoid='advance.rev869a_guard_controlled_version()'::regprocedure AND NOT tgisinternal)
                 OR to_regprocedure('advance.ordinary_command_context_valid(text,uuid,text,text,text)') IS NULL THEN
                RAISE EXCEPTION 'QC policy decisions require the existing immutable-version guard and ordinary command ledger.';
              END IF;
            END $guard$;

            CREATE FUNCTION advance.qc_policy_guard_decision() RETURNS trigger
            LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $qc$
            DECLARE actor uuid:=nullif(current_setting('advance.ordinary_actor_employee_id',true),'')::uuid;
              subject text:=current_setting('advance.ordinary_identity_subject',true);
              issuer text:=current_setting('advance.ordinary_identity_issuer',true);
              role_code text:=current_setting('advance.ordinary_actor_role',true);
            BEGIN
              IF TG_OP='DELETE' THEN RAISE EXCEPTION 'QC policy versions cannot be deleted.'; END IF;
              IF NEW."ApprovalStatus" IS DISTINCT FROM OLD."ApprovalStatus" THEN
                IF OLD."ApprovalStatus"<>'Pending Approval' OR NOT OLD."IsActive"
                  OR NEW."ApprovalStatus" NOT IN ('Approved','Rejected')
                  OR NEW."IsActive" IS DISTINCT FROM (NEW."ApprovalStatus"='Approved')
                  OR NEW."Version"<>OLD."Version"+1 OR NEW."UpdatedAt" IS NULL
                  OR NEW."UpdatedBy" IS DISTINCT FROM subject
                  OR (to_jsonb(NEW)-ARRAY['ApprovalStatus','IsActive','Version','UpdatedAt','UpdatedBy'])
                     IS DISTINCT FROM (to_jsonb(OLD)-ARRAY['ApprovalStatus','IsActive','Version','UpdatedAt','UpdatedBy']) THEN
                  RAISE EXCEPTION 'QC decision may only decide an unchanged active pending version once.';
                END IF;
                IF role_code IS DISTINCT FROM 'TECHNICAL_DIRECTOR' OR
                   advance.ordinary_command_context_valid(NEW."OrganizationId",actor,issuer,subject,role_code) IS NOT TRUE THEN
                  RAISE EXCEPTION USING ERRCODE='42501', MESSAGE='QC policy decisions require the registered Technical Director command.';
                END IF;
                IF (SELECT count(DISTINCT m."EmployeeId") FROM advance.employee_identity_mappings m
                    WHERE m."CompanyId"=OLD."CompanyId" AND m."Subject"=OLD."CreatedBy")<>1
                   OR EXISTS (SELECT 1 FROM advance.employee_identity_mappings m
                    WHERE m."CompanyId"=OLD."CompanyId" AND m."Subject"=OLD."CreatedBy" AND m."EmployeeId"=actor) THEN
                  RAISE EXCEPTION USING ERRCODE='42501', MESSAGE='QC policy decision requires a distinct, unambiguous preparer.';
                END IF;
                RETURN NEW;
              END IF;
              -- Preserve the original close-only rule; never relax other configuration guards.
              IF (to_jsonb(NEW)-ARRAY['EffectiveTo','IsActive','UpdatedAt','UpdatedBy','Version'])
                 IS DISTINCT FROM (to_jsonb(OLD)-ARRAY['EffectiveTo','IsActive','UpdatedAt','UpdatedBy','Version'])
                 OR OLD."EffectiveTo" IS NOT NULL OR NEW."EffectiveTo" IS NULL OR NEW."EffectiveTo"<OLD."EffectiveFrom" THEN
                RAISE EXCEPTION 'Historical effective records are immutable; only a valid first close is allowed.';
              END IF;
              RETURN NEW;
            END $qc$;

            CREATE FUNCTION advance.qc_policy_require_decision_history() RETURNS trigger
            LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $qc$
            BEGIN
              IF NEW."ApprovalStatus" IS DISTINCT FROM OLD."ApprovalStatus" AND
                 (SELECT count(*) FROM advance.controlled_configuration_histories h
                  WHERE h."CompanyId"=NEW."CompanyId" AND h."OrganizationId"=NEW."OrganizationId"
                    AND h."EntityType"='QcInspectionPolicy' AND h."EntityId"=NEW."Id"
                    AND h."Version"=NEW."Version"
                    AND h."Action"=CASE NEW."ApprovalStatus" WHEN 'Approved' THEN 'Approve' ELSE 'Reject' END
                    AND h."ActorRoleCode"='TECHNICAL_DIRECTOR' AND h."ActorLoginId"=NEW."UpdatedBy"
                    AND length(trim(h."Remarks"))>0 AND h.xmin::text::bigint=txid_current()
                    AND h."BeforeJson"::jsonb->>'ApprovalStatus'=OLD."ApprovalStatus"
                    AND h."AfterJson"::jsonb->>'ApprovalStatus'=NEW."ApprovalStatus"
                    AND (h."AfterJson"::jsonb->>'DecisionEmployeeId')::uuid=
                        nullif(current_setting('advance.ordinary_actor_employee_id',true),'')::uuid)<>1 THEN
                RAISE EXCEPTION 'QC policy decision requires exactly one matching append-only history entry.';
              END IF;
              RETURN NULL;
            END $qc$;
            REVOKE ALL ON FUNCTION advance.qc_policy_guard_decision() FROM PUBLIC;
            REVOKE ALL ON FUNCTION advance.qc_policy_require_decision_history() FROM PUBLIC;
            DROP TRIGGER trg_rev869a_qc_version_guard ON advance.qc_inspection_policies;
            CREATE TRIGGER trg_rev869a_qc_version_guard BEFORE UPDATE OR DELETE ON advance.qc_inspection_policies
              FOR EACH ROW EXECUTE FUNCTION advance.qc_policy_guard_decision();
            CREATE CONSTRAINT TRIGGER trg_qc_policy_decision_history AFTER UPDATE ON advance.qc_inspection_policies
              DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION advance.qc_policy_require_decision_history();
            """.Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            DO $guard$ BEGIN
              IF current_setting('server_version_num')::integer < 170000 OR
                 current_database() IN ('postgres','template0','template1') THEN
                RAISE EXCEPTION 'QC policy rollback refuses this database.';
              END IF;
              IF EXISTS (SELECT 1 FROM advance.controlled_configuration_histories
                WHERE "EntityType"='QcInspectionPolicy' AND "Action" IN ('Approve','Reject')) THEN
                RAISE EXCEPTION 'QC policy rollback refuses retained decision evidence.';
              END IF;
            END $guard$;
            DROP TRIGGER trg_qc_policy_decision_history ON advance.qc_inspection_policies;
            DROP TRIGGER trg_rev869a_qc_version_guard ON advance.qc_inspection_policies;
            CREATE TRIGGER trg_rev869a_qc_version_guard BEFORE UPDATE OR DELETE ON advance.qc_inspection_policies
              FOR EACH ROW EXECUTE FUNCTION advance.rev869a_guard_controlled_version();
            DROP FUNCTION advance.qc_policy_require_decision_history();
            DROP FUNCTION advance.qc_policy_guard_decision();
            """.Replace("\r\n", "\n", StringComparison.Ordinal));
    }
}
