namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class ControlledTaxGstWorkflowSql
{
    internal static string Up => AdvanceSchemaSql.Expand(UpTemplate);
    internal static string Down => AdvanceSchemaSql.Expand(DownTemplate);

    private const string UpTemplate = """
        ALTER TABLE __advance_schema__.tax_gst_settings
          ADD CONSTRAINT "CK_tax_gst_workflow_state" CHECK (
            ("ApprovalStatus"='Pending Approval' AND "DecisionEmployeeId" IS NULL AND "DecisionRoleCode" IS NULL AND "DecisionAt" IS NULL AND "DecisionRemarks" IS NULL AND "IsActive") OR
            ("ApprovalStatus"='Approved' AND "DecisionEmployeeId" IS NOT NULL AND "DecisionRoleCode" IN ('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR') AND "DecisionAt" IS NOT NULL AND length(trim("DecisionRemarks"))>0 AND "DecisionEmployeeId"<>"CreatorEmployeeId" AND "IsActive") OR
            ("ApprovalStatus"='Rejected' AND "DecisionEmployeeId" IS NOT NULL AND "DecisionRoleCode" IN ('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR') AND "DecisionAt" IS NOT NULL AND length(trim("DecisionRemarks"))>0 AND "DecisionEmployeeId"<>"CreatorEmployeeId" AND NOT "IsActive")
          );

        DROP TRIGGER IF EXISTS trg_rev869a_tax_version_guard ON __advance_schema__.tax_gst_settings;

        CREATE OR REPLACE FUNCTION __advance_schema__.tax_gst_guard_controlled_mutation()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,__advance_schema__ AS $tax$
        DECLARE actor_employee uuid:=nullif(current_setting('__advance_schema__.rev869b_actor_employee_id',true),'')::uuid;
          actor_role text:=current_setting('__advance_schema__.rev869b_actor_role',true);
          actor_subject text:=current_setting('__advance_schema__.rev869b_identity_subject',true);
          actor_issuer text:=current_setting('__advance_schema__.rev869b_identity_issuer',true);
          actor_org text:=current_setting('__advance_schema__.rev869b_organization',true);
        BEGIN
          IF TG_OP='DELETE' THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='tax_gst_versions_immutable',
              MESSAGE='GST rule versions cannot be deleted.';
          END IF;
          IF actor_employee IS NULL OR actor_subject IS NULL OR actor_issuer IS NULL OR actor_role IS NULL OR actor_org IS DISTINCT FROM NEW."OrganizationId" THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='tax_gst_signed_context_required',
              MESSAGE='GST rule mutation requires the exact signed command principal.';
          END IF;
          IF __advance_schema__.rev869b_command_context_valid(NEW."OrganizationId"::text,actor_employee,actor_issuer::text,actor_subject::text,actor_role::text) IS NOT TRUE THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='tax_gst_signed_context_required',
              MESSAGE='GST rule mutation requires the exact signed command principal.';
          END IF;
          IF NOT EXISTS (
               SELECT 1 FROM __advance_schema__.employee_identity_mappings m
               WHERE m."OrganizationId"=NEW."OrganizationId" AND m."EmployeeId"=actor_employee
                 AND m."Issuer"=actor_issuer AND m."Subject"=actor_subject AND m."IsActive"
                 AND m."EffectiveFrom"<=statement_timestamp()::date
                 AND (m."EffectiveTo" IS NULL OR m."EffectiveTo">=statement_timestamp()::date))
             OR NOT EXISTS (
               SELECT 1 FROM __advance_schema__.employee_role_assignments a
               JOIN __advance_schema__.roles r ON r."Id"=a."RoleId"
               WHERE a."CompanyId"=NEW."CompanyId" AND a."EmployeeId"=actor_employee
                 AND r."Code"=actor_role AND r."IsActive"
                 AND a."ApprovalStatus" IN ('Approved','SeedApproved')
                 AND a."EffectiveFrom"<=statement_timestamp()::date
                 AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=statement_timestamp()::date)) THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='tax_gst_employee_role_binding',
              MESSAGE='GST workflow requires an effective company identity mapping and exact employee role assignment.';
          END IF;
          IF TG_OP='INSERT' THEN
            IF actor_role IS DISTINCT FROM 'ACCOUNTS_MANAGER' OR NEW."CreatorEmployeeId" IS DISTINCT FROM actor_employee OR
               NEW."CreatedBy" IS DISTINCT FROM actor_subject OR NEW."ApprovalStatus"<>'Pending Approval' OR NOT NEW."IsActive" OR
               NEW."Version"<>0 OR NEW."DecisionEmployeeId" IS NOT NULL OR NEW."DecisionRoleCode" IS NOT NULL OR
               NEW."DecisionAt" IS NOT NULL OR NEW."DecisionRemarks" IS NOT NULL THEN
              RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='tax_gst_creator_binding',
                MESSAGE='GST rule creation requires the exact ACCOUNTS_MANAGER employee and a pristine pending version.';
            END IF;
            IF NEW."SupersedesTaxGstSettingId" IS NOT NULL AND NOT EXISTS (
              SELECT 1 FROM __advance_schema__.tax_gst_settings p
              WHERE p."Id"=NEW."SupersedesTaxGstSettingId" AND p."CompanyId"=NEW."CompanyId"
                AND p."OrganizationId"=NEW."OrganizationId" AND p."ApprovalStatus"='Approved' AND p."IsActive"
                AND p."JurisdictionCode"=NEW."JurisdictionCode" AND p."HsnSacCode"=NEW."HsnSacCode"
                AND p."SupplierStateCode"=NEW."SupplierStateCode" AND p."PlaceOfSupplyStateCode"=NEW."PlaceOfSupplyStateCode"
                AND p."VendorRegistrationType"=NEW."VendorRegistrationType") THEN
              RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='tax_gst_exact_predecessor',
                MESSAGE='A superseding GST rule must reference the exact current approved rule in the same company and applicability key.';
            END IF;
            NEW."CreatedAt":=transaction_timestamp(); NEW."UpdatedAt":=NULL; NEW."UpdatedBy":=NULL;
            RETURN NEW;
          END IF;
          IF OLD."ApprovalStatus"<>'Pending Approval' OR OLD."DecisionEmployeeId" IS NOT NULL THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='tax_gst_decided_version_immutable',
              MESSAGE='Approved and rejected GST rule versions are immutable; supersede with a new version.';
          END IF;
          IF actor_role NOT IN ('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR') OR actor_employee=OLD."CreatorEmployeeId" OR
             NEW."DecisionEmployeeId" IS DISTINCT FROM actor_employee OR NEW."DecisionRoleCode" IS DISTINCT FROM actor_role OR
             NEW."DecisionAt" IS NULL OR length(trim(coalesce(NEW."DecisionRemarks",'')))=0 OR
             NEW."ApprovalStatus" NOT IN ('Approved','Rejected') OR
             NEW."IsActive" IS DISTINCT FROM (NEW."ApprovalStatus"='Approved') OR
             NEW."Version"<>OLD."Version"+1 OR NEW."UpdatedBy" IS DISTINCT FROM actor_subject OR
             (to_jsonb(NEW)-ARRAY['ApprovalStatus','DecisionEmployeeId','DecisionRoleCode','DecisionAt','DecisionRemarks','IsActive','UpdatedAt','UpdatedBy','Version']) <>
             (to_jsonb(OLD)-ARRAY['ApprovalStatus','DecisionEmployeeId','DecisionRoleCode','DecisionAt','DecisionRemarks','IsActive','UpdatedAt','UpdatedBy','Version']) THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='tax_gst_decision_binding',
              MESSAGE='GST approval or rejection requires an independent TD/MD, exact version +1, and no rule-value mutation.';
          END IF;
          NEW."UpdatedAt":=transaction_timestamp();
          RETURN NEW;
        END $tax$;
        REVOKE ALL ON FUNCTION __advance_schema__.tax_gst_guard_controlled_mutation() FROM PUBLIC;

        CREATE TRIGGER trg_tax_gst_controlled_mutation
          BEFORE INSERT OR UPDATE OR DELETE ON __advance_schema__.tax_gst_settings
          FOR EACH ROW EXECUTE FUNCTION __advance_schema__.tax_gst_guard_controlled_mutation();

        CREATE OR REPLACE FUNCTION __advance_schema__.tax_gst_guard_history_insert()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,__advance_schema__ AS $tax$
        DECLARE actor_employee uuid;
        BEGIN
          IF NEW."EntityType"<>'TaxGstSetting' THEN RETURN NEW; END IF;
          actor_employee:=nullif(current_setting('__advance_schema__.rev869b_actor_employee_id',true),'')::uuid;
          IF actor_employee IS NULL OR current_setting('__advance_schema__.rev869b_identity_subject',true) IS NULL OR
             current_setting('__advance_schema__.rev869b_identity_issuer',true) IS NULL OR
             current_setting('__advance_schema__.rev869b_actor_role',true) IS NULL OR
             NEW."ActorLoginId" IS DISTINCT FROM current_setting('__advance_schema__.rev869b_identity_subject',true) OR
             NEW."CreatedBy" IS DISTINCT FROM current_setting('__advance_schema__.rev869b_identity_subject',true) OR
             NEW."ActorRoleCode" IS DISTINCT FROM current_setting('__advance_schema__.rev869b_actor_role',true) THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='tax_gst_history_actor_binding',
              MESSAGE='GST history must use the exact signed command principal.';
          END IF;
          IF __advance_schema__.rev869b_command_context_valid(NEW."OrganizationId"::text,actor_employee,
               current_setting('__advance_schema__.rev869b_identity_issuer',true)::text,
               current_setting('__advance_schema__.rev869b_identity_subject',true)::text,
               current_setting('__advance_schema__.rev869b_actor_role',true)::text) IS NOT TRUE THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='tax_gst_history_actor_binding',
              MESSAGE='GST history must use the exact signed command principal.';
          END IF;
          NEW."CreatedAt":=transaction_timestamp(); NEW."UpdatedAt":=NULL; NEW."UpdatedBy":=NULL;
          RETURN NEW;
        END $tax$;
        REVOKE ALL ON FUNCTION __advance_schema__.tax_gst_guard_history_insert() FROM PUBLIC;

        CREATE TRIGGER trg_tax_gst_history_insert_guard
          BEFORE INSERT ON __advance_schema__.controlled_configuration_histories
          FOR EACH ROW EXECUTE FUNCTION __advance_schema__.tax_gst_guard_history_insert();

        CREATE OR REPLACE FUNCTION __advance_schema__.tax_gst_require_history()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,__advance_schema__ AS $tax$
        DECLARE expected_action text:=CASE WHEN TG_OP='INSERT' THEN 'Create' WHEN NEW."ApprovalStatus"='Approved' THEN 'Approve' ELSE 'Reject' END;
          expected_correlation text; history_id uuid; history_count bigint; history_remarks text; history_summary text;
        BEGIN
          expected_correlation:=format('TAX|%s|%s|%s',replace(NEW."Id"::text,'-',''),NEW."Version",upper(expected_action));
          SELECT count(*),min(h."Id"::text)::uuid,min(h."Remarks")
            INTO history_count,history_id,history_remarks
          FROM __advance_schema__.controlled_configuration_histories h
          WHERE h."EntityType"='TaxGstSetting' AND h."EntityId"=NEW."Id" AND h."OrganizationId"=NEW."OrganizationId"
            AND h."Action"=expected_action AND h."Version"=NEW."Version" AND h."CorrelationId"=expected_correlation
            AND h."ActorLoginId"=current_setting('__advance_schema__.rev869b_identity_subject',true)
            AND h."ActorRoleCode"=current_setting('__advance_schema__.rev869b_actor_role',true)
            AND h."CreatedAt"=transaction_timestamp() AND length(trim(h."Remarks"))>0
            AND ((TG_OP='INSERT' AND h."BeforeJson" IS NULL
                  AND coalesce(h."AfterJson"->>'approvalStatus',h."AfterJson"->>'ApprovalStatus')='Pending Approval'
                  AND coalesce(h."AfterJson"->>'version',h."AfterJson"->>'Version')::bigint=0) OR
                 (TG_OP='UPDATE'
                  AND coalesce(h."BeforeJson"->>'approvalStatus',h."BeforeJson"->>'ApprovalStatus')='Pending Approval'
                  AND coalesce(h."AfterJson"->>'approvalStatus',h."AfterJson"->>'ApprovalStatus')=NEW."ApprovalStatus"
                  AND coalesce(h."BeforeJson"->>'version',h."BeforeJson"->>'Version')::bigint=OLD."Version"
                  AND coalesce(h."AfterJson"->>'version',h."AfterJson"->>'Version')::bigint=NEW."Version"
                  AND coalesce(h."AfterJson"->>'decisionEmployeeId',h."AfterJson"->>'DecisionEmployeeId')::uuid=NEW."DecisionEmployeeId"
                  AND coalesce(h."AfterJson"->>'decisionRoleCode',h."AfterJson"->>'DecisionRoleCode')=NEW."DecisionRoleCode"))
            AND h.xmin::text::bigint=txid_current();
          IF history_count<>1 THEN
            SELECT string_agg(format('action=%s version=%s correlation=%s sameTime=%s xmin=%s currentXid=%s beforeStatus=%s afterStatus=%s beforeVersion=%s afterVersion=%s',
              h."Action",h."Version",h."CorrelationId",h."CreatedAt"=transaction_timestamp(),h.xmin::text,txid_current(),
              coalesce(h."BeforeJson"->>'approvalStatus',h."BeforeJson"->>'ApprovalStatus','<null>'),
              coalesce(h."AfterJson"->>'approvalStatus',h."AfterJson"->>'ApprovalStatus','<null>'),
              coalesce(h."BeforeJson"->>'version',h."BeforeJson"->>'Version','<null>'),
              coalesce(h."AfterJson"->>'version',h."AfterJson"->>'Version','<null>')), '; ')
              INTO history_summary
            FROM __advance_schema__.controlled_configuration_histories h
            WHERE h."EntityType"='TaxGstSetting' AND h."EntityId"=NEW."Id";
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='tax_gst_requires_history',
              MESSAGE=format('GST lifecycle requires one exact same-transaction immutable history row. expected=%s evidence=%s',
                expected_correlation,coalesce(history_summary,'<none>'));
          END IF;
          PERFORM __advance_schema__.rev869b_claim_command_context(
            'tax_history',history_id,'TaxGstSetting',NEW."Id",expected_action,
            CASE WHEN TG_OP='INSERT' THEN 0 ELSE OLD."Version" END,
            CASE WHEN TG_OP='INSERT' THEN NULL ELSE 'Pending Approval' END,
            NEW."ApprovalStatus",expected_correlation,history_remarks);
          RETURN NULL;
        END $tax$;
        REVOKE ALL ON FUNCTION __advance_schema__.tax_gst_require_history() FROM PUBLIC;

        CREATE CONSTRAINT TRIGGER trg_tax_gst_requires_history
          AFTER INSERT OR UPDATE ON __advance_schema__.tax_gst_settings
          DEFERRABLE INITIALLY DEFERRED FOR EACH ROW
          EXECUTE FUNCTION __advance_schema__.tax_gst_require_history();
        """;

    private const string DownTemplate = """
        DROP TRIGGER IF EXISTS trg_tax_gst_requires_history ON __advance_schema__.tax_gst_settings;
        DROP TRIGGER IF EXISTS trg_tax_gst_history_insert_guard ON __advance_schema__.controlled_configuration_histories;
        DROP TRIGGER IF EXISTS trg_tax_gst_controlled_mutation ON __advance_schema__.tax_gst_settings;
        DROP FUNCTION IF EXISTS __advance_schema__.tax_gst_require_history();
        DROP FUNCTION IF EXISTS __advance_schema__.tax_gst_guard_history_insert();
        DROP FUNCTION IF EXISTS __advance_schema__.tax_gst_guard_controlled_mutation();
        ALTER TABLE __advance_schema__.tax_gst_settings DROP CONSTRAINT IF EXISTS "CK_tax_gst_workflow_state";
        CREATE TRIGGER trg_rev869a_tax_version_guard BEFORE UPDATE OR DELETE ON __advance_schema__.tax_gst_settings
          FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869a_guard_controlled_version();
        """;
}
