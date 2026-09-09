namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static partial class ComponentFitmentActualBomSql
{
    private const string RoleAndPage = """
        DO $role$
        DECLARE role_id uuid:='51000000-0000-0000-0000-000000000052';
        BEGIN
          IF EXISTS (SELECT 1 FROM advance.roles WHERE "Code"='SERVICE_MANAGER'
              AND (NOT "IsActive" OR NOT "IsEmployeeAssignable" OR "Audience"<>'INTERNAL_EMPLOYEE')) THEN
            RAISE EXCEPTION 'Existing SERVICE_MANAGER role is incompatible with fitment authority.';
          END IF;
          INSERT INTO advance.roles
            ("Id","Code","Name","IsPrivileged","IsActive","Audience","BusinessArea",
             "IsEmployeeAssignable","CreatedAt","CreatedBy","Version")
          SELECT role_id,'SERVICE_MANAGER','Service Manager',false,true,'INTERNAL_EMPLOYEE',
             'SERVICE',true,now(),'ComponentFitmentAndGeneratedActualBom',0
          WHERE NOT EXISTS (SELECT 1 FROM advance.roles WHERE "Code"='SERVICE_MANAGER');
          SELECT "Id" INTO role_id FROM advance.roles WHERE "Code"='SERVICE_MANAGER';
          INSERT INTO advance.company_role_activations
            ("Id","CompanyId","RoleId","IsEnabled","EffectiveFrom","Remarks",
             "CreatedAt","CreatedBy","Version")
          SELECT md5('service-manager-activation-'||c."Id")::uuid,c."Id",role_id,true,
             DATE '2026-09-08','Service management authority for fitment reversal.',
             now(),'ComponentFitmentAndGeneratedActualBom',0
          FROM advance.companies c
          WHERE NOT EXISTS (SELECT 1 FROM advance.company_role_activations a
            WHERE a."CompanyId"=c."Id" AND a."RoleId"=role_id
              AND a."IsEnabled" AND a."EffectiveFrom"<=DATE '2026-09-08'
              AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=DATE '2026-09-08'));
        END $role$;
        DO $page$
        DECLARE page_id uuid:='52000000-0000-0000-0000-000000000009'; affected integer;
        BEGIN
          INSERT INTO advance.page_definitions
            ("Id","PageKey","Module","Title","Route","IsActive","CreatedAt","CreatedBy","Version")
          VALUES (page_id,'production.component-fitments','Production','Component Fitments and Actual BOM',
             '/production/component-fitments',true,now(),'ComponentFitmentAndGeneratedActualBom',0);
          INSERT INTO advance.role_page_permissions
            ("Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit",
             "CanIssue","CanVerify","CanApprove","CanReject","CanRequestClarification",
             "CanRequestRevision","CanResubmit","CanCancel","CanDeactivate","CanPrint",
             "CanDownload","CanExport","CanUploadAttachment","CanReplaceAttachment",
             "CanViewCommercialValues","CanViewAuditHistory","HasFullControl",
             "CreatedAt","CreatedBy","Version")
          SELECT md5('component-fitment-'||r."Id")::uuid,r."Id",page_id,true,
             r."Code" IN ('PRODUCTION_OPERATOR','PRODUCTION_COORDINATOR','PRODUCTION_MANAGER','SERVICE_ENGINEER'),
             false,false,false,false,false,false,false,false,false,
             r."Code" IN ('PRODUCTION_MANAGER','SERVICE_MANAGER'),false,false,false,false,false,
             true,true,true,false,now(),'ComponentFitmentAndGeneratedActualBom',0
          FROM advance.roles r WHERE r."Code" IN
            ('PRODUCTION_OPERATOR','PRODUCTION_COORDINATOR','PRODUCTION_MANAGER','SERVICE_ENGINEER','SERVICE_MANAGER')
            AND r."IsActive";
          GET DIAGNOSTICS affected=ROW_COUNT;
          IF affected<>5 THEN RAISE EXCEPTION 'Expected five fitment role grants, found %.',affected; END IF;
        END $page$;
        """;

    private const string Ownership = """
        REVOKE ALL ON TABLE advance.component_fitments,advance.component_fitment_reversals,
          advance.actual_boms,advance.actual_bom_entries FROM PUBLIC;
        DO $roles$
        BEGIN
          IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_owner') THEN
            ALTER TABLE advance.component_fitments OWNER TO nexa_erp_owner;
            ALTER TABLE advance.component_fitment_reversals OWNER TO nexa_erp_owner;
            ALTER TABLE advance.actual_boms OWNER TO nexa_erp_owner;
            ALTER TABLE advance.actual_bom_entries OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.fitment_authority_valid(uuid,uuid,text,uuid,text,text[]) OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.guard_fitment_actual_bom_evidence() OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.guard_accepted_bill_with_fitment() OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.guard_fitment_stock_posting() OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.confirm_component_fitment(uuid,uuid,uuid,numeric,timestamptz,text,uuid,text,text,text,uuid,text,uuid,text,text) OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.reverse_component_fitment(uuid,uuid,text,text,text,text,uuid,text,uuid,text,text) OWNER TO nexa_erp_owner;
          END IF;
          IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_runtime') THEN
            REVOKE ALL ON advance.component_fitments,advance.component_fitment_reversals,
              advance.actual_boms,advance.actual_bom_entries FROM nexa_erp_runtime;
            GRANT SELECT ON advance.component_fitments,advance.component_fitment_reversals,
              advance.actual_boms,advance.actual_bom_entries TO nexa_erp_runtime;
            GRANT EXECUTE ON FUNCTION advance.confirm_component_fitment(uuid,uuid,uuid,numeric,timestamptz,text,uuid,text,text,text,uuid,text,uuid,text,text) TO nexa_erp_runtime;
            GRANT EXECUTE ON FUNCTION advance.reverse_component_fitment(uuid,uuid,text,text,text,text,uuid,text,uuid,text,text) TO nexa_erp_runtime;
            REVOKE EXECUTE ON FUNCTION advance.confirm_component_fitment(uuid,uuid,uuid,numeric,timestamptz,text,uuid,text,text,text,uuid,text,uuid,text,text) FROM nexa_erp_bootstrap,nexa_erp_migration;
            REVOKE EXECUTE ON FUNCTION advance.reverse_component_fitment(uuid,uuid,text,text,text,text,uuid,text,uuid,text,text) FROM nexa_erp_bootstrap,nexa_erp_migration;
          END IF;
        END $roles$;
        """;

    internal static string Down => """
        DO $guard$
        BEGIN
          IF EXISTS (SELECT 1 FROM advance.component_fitments)
             OR EXISTS (SELECT 1 FROM advance.component_fitment_reversals)
             OR EXISTS (SELECT 1 FROM advance.actual_bom_entries) THEN
            RAISE EXCEPTION 'Fitment rollback refuses operational, reversal or Actual BOM evidence.';
          END IF;
        END $guard$;
        DROP TRIGGER IF EXISTS trg_fitment_movement_reconcile ON advance.stock_movements;
        DROP TRIGGER IF EXISTS trg_fitment_batch_reconcile ON advance.stock_posting_batches;
        DROP FUNCTION IF EXISTS advance.guard_fitment_stock_posting();
        DROP TRIGGER IF EXISTS trg_vendor_bill_active_fitment_guard ON advance.vendor_bills;
        DROP FUNCTION IF EXISTS advance.guard_accepted_bill_with_fitment();
        DROP TRIGGER IF EXISTS trg_actual_bom_entry_guard ON advance.actual_bom_entries;
        DROP TRIGGER IF EXISTS trg_actual_bom_guard ON advance.actual_boms;
        DROP TRIGGER IF EXISTS trg_component_fitment_reversal_guard ON advance.component_fitment_reversals;
        DROP TRIGGER IF EXISTS trg_component_fitment_guard ON advance.component_fitments;
        DROP FUNCTION IF EXISTS advance.guard_fitment_actual_bom_evidence();
        DROP FUNCTION IF EXISTS advance.reverse_component_fitment(uuid,uuid,text,text,text,text,uuid,text,uuid,text,text);
        DROP FUNCTION IF EXISTS advance.confirm_component_fitment(uuid,uuid,uuid,numeric,timestamptz,text,uuid,text,text,text,uuid,text,uuid,text,text);
        DROP FUNCTION IF EXISTS advance.fitment_authority_valid(uuid,uuid,text,uuid,text,text[]);
        DELETE FROM advance.role_page_permissions WHERE "CreatedBy"='ComponentFitmentAndGeneratedActualBom';
        DELETE FROM advance.page_definitions WHERE "CreatedBy"='ComponentFitmentAndGeneratedActualBom';
        DELETE FROM advance.company_role_activations WHERE "CreatedBy"='ComponentFitmentAndGeneratedActualBom';
        DELETE FROM advance.roles r WHERE r."CreatedBy"='ComponentFitmentAndGeneratedActualBom'
          AND NOT EXISTS (SELECT 1 FROM advance.employee_role_assignments a WHERE a."RoleId"=r."Id");
        DROP INDEX advance."IX_vendor_bills_CompanyId_BillNumber";
        DROP INDEX advance."IX_vendor_bills_CompanyId_GoodsReceiptId";
        CREATE UNIQUE INDEX "IX_vendor_bills_CompanyId_BillNumber" ON advance.vendor_bills ("CompanyId","BillNumber") WHERE "Status"<>'REVERSED';
        CREATE UNIQUE INDEX "IX_vendor_bills_CompanyId_GoodsReceiptId" ON advance.vendor_bills ("CompanyId","GoodsReceiptId") WHERE "Status"<>'REVERSED';
        ALTER TABLE advance.stock_posting_batches DROP CONSTRAINT "CK_stock_posting_batch_kind";
        ALTER TABLE advance.stock_posting_batches ADD CONSTRAINT "CK_stock_posting_batch_kind"
          CHECK ("PostingKind" IN ('GRN_CUSTODY','QC_DISPOSITION','CONCESSION_ACCEPTANCE',
            'MATERIAL_ISSUE','MATERIAL_RETURN','DC_DISPATCH','DC_RETURN_CUSTODY','REVERSAL'));
        ALTER TABLE advance.stock_posting_batches DROP CONSTRAINT "CK_stock_posting_batch_source";
        ALTER TABLE advance.stock_posting_batches ADD CONSTRAINT "CK_stock_posting_batch_source"
          CHECK (("PostingKind"<>'REVERSAL'
              AND num_nonnulls("GoodsReceiptId","QcInspectionRevisionId","MaterialIssueRequestId",
                "MaterialReturnId","DeliveryChallanId","InventoryCustodyHandoffId",
                "InventoryOwnershipTransferId","InventoryTransformationId","InventoryConcessionId")=1
              AND "ReversesPostingBatchId" IS NULL)
            OR ("PostingKind"='REVERSAL'
              AND num_nonnulls("GoodsReceiptId","QcInspectionRevisionId","MaterialIssueRequestId",
                "MaterialReturnId","DeliveryChallanId","InventoryCustodyHandoffId",
                "InventoryOwnershipTransferId","InventoryTransformationId","InventoryConcessionId")=0
              AND "ReversesPostingBatchId" IS NOT NULL));
        ALTER TABLE advance.stock_movements DROP CONSTRAINT "CK_stock_movement_v2_contract";
        ALTER TABLE advance.stock_movements ADD CONSTRAINT "CK_stock_movement_v2_contract"
          CHECK ("OwnershipAccountId" IS NOT NULL AND "CustodyAssignmentId" IS NOT NULL
            AND "InventoryProvenanceLayerId" IS NOT NULL AND "WarehouseId" IS NOT NULL
            AND "RackBinId" IS NOT NULL AND "WarehouseConditionLocationId" IS NOT NULL
            AND "ConditionCode" IS NOT NULL AND "StockPostingBatchId" IS NOT NULL
            AND "BatchLineOrdinal">0
            AND "MovementLeg" IN ('RECEIPT_IN','TRANSFER_OUT','TRANSFER_IN','ISSUE_OUT',
              'RETURN_OUT','DISPATCH_OUT','RETURN_IN','REVERSAL')
            AND "PostingIdentity" IS NOT NULL
            AND num_nonnulls("GoodsReceiptLineId","QcInspectionRevisionId","MaterialIssueRequestLineId",
              "DeliveryChallanLineId","InventoryCustodyHandoffLineId","InventoryOwnershipTransferLineId",
              "InventoryTransformationInputId","InventoryTransformationOutputId","InventoryConcessionAllocationId")
              + CASE WHEN "QcInspectionLotDispositionId" IS NOT NULL
                           AND "MaterialIssueRequestLineId" IS NULL THEN 1 ELSE 0 END = 1
            AND ("MaterialIssueLineId" IS NULL OR "MaterialIssueRequestLineId" IS NOT NULL)
            AND ("MaterialReturnLineId" IS NULL OR "MaterialIssueLineId" IS NOT NULL));
        """ + MaterialReturnSql.ActiveBatchGuard + MaterialReturnSql.ActiveMovementGuard
          + MaterialReturnSql.ActiveReconcileGuard;
}