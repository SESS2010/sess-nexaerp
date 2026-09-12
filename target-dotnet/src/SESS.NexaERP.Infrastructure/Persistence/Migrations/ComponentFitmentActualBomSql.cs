namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static partial class ComponentFitmentActualBomSql
{
    internal const string Preflight = """
        DO $guard$
        DECLARE found_count integer;
        BEGIN
          IF current_setting('server_version_num')::integer < 170000 THEN
            RAISE EXCEPTION 'Component fitment requires PostgreSQL 17 or later.';
          END IF;
          IF current_database() IN ('postgres','template0','template1') THEN
            RAISE EXCEPTION 'Component fitment refuses a PostgreSQL administrative database.';
          END IF;
          IF to_regclass('advance.material_issue_lines') IS NULL
             OR to_regclass('advance.vendor_bill_cost_allocations') IS NULL
             OR to_regclass('advance.job_orders') IS NULL
             OR to_regprocedure('advance.ordinary_command_context_valid(text,uuid,text,text,text)') IS NULL THEN
            RAISE EXCEPTION 'Component fitment requires Job Order, Issue, accepted-bill costing and ordinary command-ledger baselines.';
          END IF;
          SELECT count(*) INTO found_count FROM pg_roles WHERE rolname IN
            ('nexa_erp_owner','nexa_erp_migration','nexa_erp_bootstrap','nexa_erp_runtime');
          IF found_count NOT IN (0,4) THEN
            RAISE EXCEPTION 'Partial NexaERP principal state; refusing to guess grants or ownership.';
          END IF;
          IF to_regclass('advance.component_fitments') IS NOT NULL
             OR to_regclass('advance.component_fitment_reversals') IS NOT NULL
             OR to_regclass('advance.actual_boms') IS NOT NULL
             OR to_regclass('advance.actual_bom_entries') IS NOT NULL
             OR EXISTS (SELECT 1 FROM advance.page_definitions WHERE "PageKey"='production.component-fitments') THEN
            RAISE EXCEPTION 'Component fitment is partially or already installed.';
          END IF;
        END $guard$;
        """;

    internal static string Up => VendorBillCostingSql.RejectedBillReentryCorrection + Contracts + ExtendedBatchGuard + ExtendedMovementGuard
        + ExtendedReconcileGuard + Governance + ConfirmFunction + ReverseFunction
        + Reconciliation + RoleAndPage + Ownership;

    private const string Contracts = """
        DROP INDEX advance."IX_vendor_bills_CompanyId_BillNumber";
        DROP INDEX advance."IX_vendor_bills_CompanyId_GoodsReceiptId";
        CREATE UNIQUE INDEX "IX_vendor_bills_CompanyId_BillNumber" ON advance.vendor_bills ("CompanyId","BillNumber") WHERE "Status" NOT IN ('REVERSED','REJECTED');
        CREATE UNIQUE INDEX "IX_vendor_bills_CompanyId_GoodsReceiptId" ON advance.vendor_bills ("CompanyId","GoodsReceiptId") WHERE "Status" NOT IN ('REVERSED','REJECTED');
        ALTER TABLE advance.stock_posting_batches DROP CONSTRAINT "CK_stock_posting_batch_kind";
        ALTER TABLE advance.stock_posting_batches ADD CONSTRAINT "CK_stock_posting_batch_kind"
          CHECK ("PostingKind" IN ('GRN_CUSTODY','QC_DISPOSITION','CONCESSION_ACCEPTANCE',
            'MATERIAL_ISSUE','MATERIAL_RETURN','FITMENT_CONSUMPTION','DC_DISPATCH',
            'DC_RETURN_CUSTODY','REVERSAL'));
        ALTER TABLE advance.stock_posting_batches DROP CONSTRAINT "CK_stock_posting_batch_source";
        ALTER TABLE advance.stock_posting_batches ADD CONSTRAINT "CK_stock_posting_batch_source"
          CHECK (("PostingKind"<>'REVERSAL'
              AND num_nonnulls("GoodsReceiptId","QcInspectionRevisionId","MaterialIssueRequestId",
                "MaterialReturnId","ComponentFitmentId","DeliveryChallanId","InventoryCustodyHandoffId",
                "InventoryOwnershipTransferId","InventoryTransformationId","InventoryConcessionId")=1
              AND "ReversesPostingBatchId" IS NULL)
            OR ("PostingKind"='REVERSAL'
              AND num_nonnulls("GoodsReceiptId","QcInspectionRevisionId","MaterialIssueRequestId",
                "MaterialReturnId","ComponentFitmentId","DeliveryChallanId","InventoryCustodyHandoffId",
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
              'RETURN_OUT','CONSUMPTION_OUT','DISPATCH_OUT','RETURN_IN','REVERSAL')
            AND "PostingIdentity" IS NOT NULL
            AND num_nonnulls("GoodsReceiptLineId","QcInspectionRevisionId","MaterialIssueRequestLineId",
              "DeliveryChallanLineId","InventoryCustodyHandoffLineId","InventoryOwnershipTransferLineId",
              "InventoryTransformationInputId","InventoryTransformationOutputId","InventoryConcessionAllocationId")
              + CASE WHEN "QcInspectionLotDispositionId" IS NOT NULL
                           AND "MaterialIssueRequestLineId" IS NULL THEN 1 ELSE 0 END = 1
            AND ("MaterialIssueLineId" IS NULL OR "MaterialIssueRequestLineId" IS NOT NULL)
            AND ("MaterialReturnLineId" IS NULL OR "MaterialIssueLineId" IS NOT NULL)
            AND ("ComponentFitmentId" IS NULL OR "MaterialIssueLineId" IS NOT NULL));
        """;

    private static string ExtendedBatchGuard => MaterialReturnSql.ActiveBatchGuard.Replace(
        "  ELSIF NEW.\"PostingKind\"='MATERIAL_RETURN' THEN",
        """
          ELSIF NEW."PostingKind"='FITMENT_CONSUMPTION' THEN
            expected_type:='COMPONENT_FITMENT';
            SELECT "CompanyId","FitmentNumber" INTO source_company,expected_number
              FROM advance.component_fitments WHERE "Id"=NEW."ComponentFitmentId";
          ELSIF NEW."PostingKind"='MATERIAL_RETURN' THEN
        """);

    private static string ExtendedMovementGuard => MaterialReturnSql.ActiveMovementGuard.Replace(
        "  ELSIF NEW.\"MaterialReturnLineId\" IS NOT NULL THEN",
        """
          ELSIF NEW."ComponentFitmentId" IS NOT NULL THEN
            SELECT f."CompanyId",f."Id",il."ItemId" INTO source_company,source_header,source_item
              FROM advance.component_fitments f
              JOIN advance.material_issue_lines il ON il."Id"=f."MaterialIssueLineId"
              WHERE f."Id"=NEW."ComponentFitmentId";
            IF b."PostingKind" NOT IN ('FITMENT_CONSUMPTION','REVERSAL')
               OR (b."PostingKind"='FITMENT_CONSUMPTION' AND source_header<>b."ComponentFitmentId") THEN
              RAISE EXCEPTION 'Component fitment movement source does not match its batch.';
            END IF;
            IF b."PostingKind"='FITMENT_CONSUMPTION'
               AND NOT (NEW."MovementLeg"='CONSUMPTION_OUT' AND NEW."QuantityOut">0) THEN
              RAISE EXCEPTION 'Fitment requires a consumption-out custody leg.';
            END IF;
          ELSIF NEW."MaterialReturnLineId" IS NOT NULL THEN
        """);

    private static string ExtendedReconcileGuard => ReplaceRequired(
        MaterialReturnSql.ActiveReconcileGuard.Replace("\r\n", "\n", StringComparison.Ordinal),
        "  ELSIF b.\"PostingKind\"='MATERIAL_RETURN' THEN\n    NULL;",
        "  ELSIF b.\"PostingKind\" IN ('MATERIAL_RETURN','FITMENT_CONSUMPTION') THEN\n    NULL;");

    internal static string ActiveBatchGuard => ExtendedBatchGuard;
    internal static string ActiveMovementGuard => ExtendedMovementGuard;
    internal static string ActiveReconcileGuard => ExtendedReconcileGuard;

    private static string ReplaceRequired(string source, string oldValue, string newValue)
    {
        var first = source.IndexOf(oldValue, StringComparison.Ordinal);
        if (first < 0 || source.IndexOf(oldValue, first + oldValue.Length, StringComparison.Ordinal) >= 0)
            throw new InvalidOperationException($"Expected exactly one SQL marker: {oldValue}");
        return source.Replace(oldValue, newValue, StringComparison.Ordinal);
    }

    private const string Governance = """
        CREATE FUNCTION advance.fitment_authority_valid(
          p_company uuid,p_actor uuid,p_role text,p_assignment uuid,p_type text,p_allowed_roles text[])
        RETURNS boolean LANGUAGE plpgsql STABLE SET search_path=pg_catalog,advance AS $function$
        DECLARE organization text;
        BEGIN
          SELECT "Code" INTO organization FROM advance.companies
            WHERE "Id"=p_company AND "IsActive" AND "Status"='ACTIVE';
          RETURN organization IS NOT NULL AND p_type='FULL' AND p_role=ANY(p_allowed_roles)
            AND EXISTS (
              SELECT 1 FROM advance.employee_role_assignments a
              JOIN advance.roles r ON r."Id"=a."RoleId"
              JOIN advance.company_role_activations c
                ON c."CompanyId"=a."CompanyId" AND c."RoleId"=a."RoleId"
              WHERE a."Id"=p_assignment AND a."CompanyId"=p_company
                AND a."EmployeeId"=p_actor AND a."AssignmentType"='FULL'
                AND a."ApprovalStatus" IN ('Approved','SeedApproved')
                AND a."EffectiveFrom"<=CURRENT_DATE
                AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=CURRENT_DATE)
                AND r."Code"=p_role AND r."IsActive" AND r."IsEmployeeAssignable"
                AND c."IsEnabled" AND c."EffectiveFrom"<=CURRENT_DATE
                AND (c."EffectiveTo" IS NULL OR c."EffectiveTo">=CURRENT_DATE))
            AND advance.ordinary_command_context_valid(organization,p_actor,
                current_setting('advance.ordinary_identity_issuer',true),
                current_setting('advance.ordinary_identity_subject',true),p_role);
        END $function$;

        CREATE FUNCTION advance.guard_fitment_actual_bom_evidence()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $function$
        BEGIN
          IF TG_OP<>'INSERT' THEN
            RAISE EXCEPTION '% is immutable; correct fitment with reversal and re-verification.',TG_TABLE_NAME;
          END IF;
          IF coalesce(current_setting('advance.fitment_mutation',true),'')='' THEN
            RAISE EXCEPTION 'Fitment and Actual BOM evidence may be created only through controlled functions.';
          END IF;
          RETURN NEW;
        END $function$;
        CREATE TRIGGER trg_component_fitment_guard BEFORE INSERT OR UPDATE OR DELETE
          ON advance.component_fitments FOR EACH ROW EXECUTE FUNCTION advance.guard_fitment_actual_bom_evidence();
        CREATE TRIGGER trg_component_fitment_reversal_guard BEFORE INSERT OR UPDATE OR DELETE
          ON advance.component_fitment_reversals FOR EACH ROW EXECUTE FUNCTION advance.guard_fitment_actual_bom_evidence();
        CREATE TRIGGER trg_actual_bom_guard BEFORE INSERT OR UPDATE OR DELETE
          ON advance.actual_boms FOR EACH ROW EXECUTE FUNCTION advance.guard_fitment_actual_bom_evidence();
        CREATE TRIGGER trg_actual_bom_entry_guard BEFORE INSERT OR UPDATE OR DELETE
          ON advance.actual_bom_entries FOR EACH ROW EXECUTE FUNCTION advance.guard_fitment_actual_bom_evidence();

        CREATE FUNCTION advance.guard_accepted_bill_with_fitment()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $function$
        BEGIN
          IF OLD."Status"='ACCEPTED' AND NEW."Status"='REVERSED'
             AND EXISTS (
               SELECT 1 FROM advance.actual_bom_entries e
               JOIN advance.vendor_bill_lines l ON l."Id"=e."VendorBillLineId"
               LEFT JOIN advance.component_fitment_reversals r
                 ON r."CompanyId"=e."CompanyId" AND r."ComponentFitmentId"=e."ComponentFitmentId"
               WHERE l."VendorBillId"=OLD."Id" AND e."EntryKind"='FITMENT' AND r."Id" IS NULL) THEN
            RAISE EXCEPTION 'Accepted Vendor Bill cannot be reversed while an active fitment uses its Actual BOM value.';
          END IF;
          RETURN NEW;
        END $function$;
        CREATE TRIGGER trg_vendor_bill_active_fitment_guard BEFORE UPDATE
          ON advance.vendor_bills FOR EACH ROW EXECUTE FUNCTION advance.guard_accepted_bill_with_fitment();
        """;
}
