namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class EstimatedBomLifecycleSql
{
    internal const string Up = """
        CREATE OR REPLACE FUNCTION advance.guard_estimated_bom_governance()
        RETURNS trigger LANGUAGE plpgsql AS $$
        DECLARE a record;
        DECLARE organization text;
        BEGIN
          IF TG_OP IN ('UPDATE','DELETE') THEN
            RAISE EXCEPTION 'Estimated BOM history and item merge evidence are immutable.';
          END IF;
          SELECT r."Code", e."AssignmentType" INTO a
          FROM advance.employee_role_assignments e
          JOIN advance.roles r ON r."Id"=e."RoleId"
          WHERE e."Id"=NEW."ResolvedRoleAssignmentId"
            AND e."EmployeeId"=NEW."ActorEmployeeId"
            AND e."CompanyId"=COALESCE(NEW."CompanyId",e."CompanyId")
            AND e."ApprovalStatus"='Approved'
            AND e."EffectiveFrom"<=CURRENT_DATE
            AND (e."EffectiveTo" IS NULL OR e."EffectiveTo">=CURRENT_DATE);
          IF a IS NULL OR a."Code"<>NEW."ActorRoleCode" OR a."AssignmentType"<>NEW."ResolvedRoleAssignmentType" THEN
            RAISE EXCEPTION 'Resolved role assignment evidence does not match a currently effective assignment.';
          END IF;
          SELECT "Code" INTO STRICT organization FROM advance.companies WHERE "Id"=NEW."CompanyId";
          IF NOT advance.ordinary_command_context_valid(
              organization,NEW."ActorEmployeeId",
              current_setting('advance.ordinary_identity_issuer',true),
              current_setting('advance.ordinary_identity_subject',true),
              NEW."ActorRoleCode") THEN
            RAISE EXCEPTION 'Estimated BOM and item merge evidence requires the current ordinary command transaction.';
          END IF;
          IF TG_TABLE_NAME='estimated_bom_history' AND NEW."Action"='Approve' AND a."AssignmentType"='SUPPORT' THEN
            RAISE EXCEPTION 'SUPPORT authority cannot approve an Estimated BOM.';
          END IF;
          IF TG_TABLE_NAME='item_merge_aliases' AND
             (a."AssignmentType"='SUPPORT' OR a."Code" NOT IN ('STORES_MANAGER','PURCHASE_MANAGER')) THEN
            RAISE EXCEPTION 'Item merge requires FULL or TEMPORARY STORES_MANAGER or PURCHASE_MANAGER authority.';
          END IF;
          RETURN NEW;
        END $$;

        CREATE TRIGGER trg_estimated_bom_history_governance
          BEFORE INSERT OR UPDATE OR DELETE ON advance.estimated_bom_history
          FOR EACH ROW EXECUTE FUNCTION advance.guard_estimated_bom_governance();
        CREATE TRIGGER trg_item_merge_alias_governance
          BEFORE INSERT OR UPDATE OR DELETE ON advance.item_merge_aliases
          FOR EACH ROW EXECUTE FUNCTION advance.guard_estimated_bom_governance();

        CREATE OR REPLACE FUNCTION advance.guard_approved_item_for_purchase()
        RETURNS trigger LANGUAGE plpgsql AS $$
        BEGIN
          IF NOT EXISTS (
            SELECT 1 FROM advance.items i
            WHERE i."Id"=NEW."ItemId" AND i."IsActive"
              AND i."ApprovalStatus"='Approved'
              AND NOT EXISTS (SELECT 1 FROM advance.item_merge_aliases a WHERE a."SourceItemId"=i."Id")
          ) THEN
            RAISE EXCEPTION 'Draft, inactive or merged items cannot be purchased or received.';
          END IF;
          RETURN NEW;
        END $$;
        CREATE TRIGGER trg_rfq_line_approved_item
          BEFORE INSERT OR UPDATE OF "ItemId" ON advance.request_for_quotation_lines
          FOR EACH ROW EXECUTE FUNCTION advance.guard_approved_item_for_purchase();
        CREATE TRIGGER trg_po_line_approved_item
          BEFORE INSERT OR UPDATE OF "ItemId" ON advance.purchase_order_lines
          FOR EACH ROW EXECUTE FUNCTION advance.guard_approved_item_for_purchase();
        CREATE TRIGGER trg_grn_line_approved_item
          BEFORE INSERT OR UPDATE OF "ItemId" ON advance.goods_receipt_lines
          FOR EACH ROW EXECUTE FUNCTION advance.guard_approved_item_for_purchase();
        """;

    internal const string Grants = """
        DO $$
        DECLARE page_id uuid := '52000000-0000-0000-0000-000000000001';
        DECLARE inserted_count integer;
        BEGIN
          IF EXISTS (SELECT 1 FROM advance.page_definitions WHERE "PageKey"='design.estimated-bom') THEN
            RAISE EXCEPTION 'Estimated BOM page definition already exists; refusing ambiguous partial installation.';
          END IF;
          INSERT INTO advance.page_definitions
            ("Id","PageKey","Module","Title","Route","IsActive","CreatedAt","CreatedBy","Version")
          VALUES (page_id,'design.estimated-bom','Design','Estimated BOM','/design/estimated-boms',TRUE,now(),'EstimatedBomLifecycleAndItemGovernance',0);

          INSERT INTO advance.role_page_permissions
            ("Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit","CanIssue","CanVerify",
             "CanApprove","CanReject","CanRequestClarification","CanRequestRevision","CanResubmit","CanCancel",
             "CanDeactivate","CanPrint","CanDownload","CanExport","CanUploadAttachment","CanReplaceAttachment",
             "CanViewCommercialValues","CanViewAuditHistory","HasFullControl","CreatedAt","CreatedBy","Version")
          SELECT md5('estimated-bom-role-'||r."Id"::text)::uuid,r."Id",page_id,TRUE,TRUE,TRUE,TRUE,FALSE,FALSE,
             r."Code"='TECHNICAL_DIRECTOR',FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,TRUE,FALSE,FALSE,FALSE,
             FALSE,TRUE,FALSE,now(),'EstimatedBomLifecycleAndItemGovernance',0
          FROM advance.roles r WHERE r."Code" IN ('DESIGN_ENGINEER','TECHNICAL_DIRECTOR') AND r."IsActive";
          GET DIAGNOSTICS inserted_count = ROW_COUNT;
          IF inserted_count<>2 THEN RAISE EXCEPTION 'Expected exactly two Estimated BOM role grants, found %.',inserted_count; END IF;

          INSERT INTO advance.employee_page_permissions
            ("Id","CompanyId","EmployeeId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit",
             "CanDownload","CanViewAuditHistory","CreatedAt","CreatedBy","Version")
          SELECT md5('estimated-bom-employee-'||c."Id"::text||e."Id"::text)::uuid,c."Id",e."Id",page_id,
             TRUE,TRUE,TRUE,TRUE,TRUE,TRUE,now(),'EstimatedBomLifecycleAndItemGovernance',0
          FROM advance.companies c CROSS JOIN advance.employees e
          WHERE c."IsActive" AND c."Status"='ACTIVE' AND e."Status"='Active' AND e."EmployeeCode" IN ('SESS-04','SESS-05');
          GET DIAGNOSTICS inserted_count = ROW_COUNT;
          IF inserted_count<>4 THEN RAISE EXCEPTION 'Expected four named employee/company Estimated BOM grants, found %.',inserted_count; END IF;
        END $$;
        """;

    internal const string Down = """
        DROP TRIGGER IF EXISTS trg_grn_line_approved_item ON advance.goods_receipt_lines;
        DROP TRIGGER IF EXISTS trg_po_line_approved_item ON advance.purchase_order_lines;
        DROP TRIGGER IF EXISTS trg_rfq_line_approved_item ON advance.request_for_quotation_lines;
        DROP FUNCTION IF EXISTS advance.guard_approved_item_for_purchase();
        DROP TRIGGER IF EXISTS trg_item_merge_alias_governance ON advance.item_merge_aliases;
        DROP TRIGGER IF EXISTS trg_estimated_bom_history_governance ON advance.estimated_bom_history;
        DROP FUNCTION IF EXISTS advance.guard_estimated_bom_governance();
        DELETE FROM advance.employee_page_permissions WHERE "CreatedBy"='EstimatedBomLifecycleAndItemGovernance';
        DELETE FROM advance.role_page_permissions WHERE "CreatedBy"='EstimatedBomLifecycleAndItemGovernance';
        DELETE FROM advance.page_definitions
          WHERE "CreatedBy"='EstimatedBomLifecycleAndItemGovernance' AND "PageKey"='design.estimated-bom';
        """;
}
