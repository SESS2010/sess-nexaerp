namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class StoresSlice3QcConcessionSql
{
    internal const string PreUp = """
        DO $guard$
        BEGIN
          IF current_setting('server_version_num')::integer < 170000 THEN RAISE EXCEPTION 'Stores Slice 3 requires PostgreSQL 17 or later.'; END IF;
          IF current_database() IN ('postgres','template0','template1') THEN RAISE EXCEPTION 'Stores Slice 3 refuses a PostgreSQL administrative database.'; END IF;
          IF to_regclass('advance.stock_movements') IS NULL
             OR to_regclass('advance.qc_inspection_lot_dispositions') IS NULL
             OR to_regclass('advance.inventory_concessions') IS NULL THEN
            RAISE EXCEPTION 'Stores Slice 3 requires Foundations 2 and 3.';
          END IF;
          IF EXISTS (SELECT 1 FROM advance.qc_inspections)
             OR EXISTS (SELECT 1 FROM advance.qc_inspection_revisions)
             OR EXISTS (SELECT 1 FROM advance.inventory_concessions) THEN
            RAISE EXCEPTION 'Stores Slice 3 activation requires zero pre-activation QC and concession rows; no evidence may be rewritten.';
          END IF;
        END $guard$;
        LOCK TABLE advance.qc_inspections, advance.qc_inspection_revisions,
          advance.qc_inspection_lot_dispositions, advance.inventory_concessions,
          advance.inventory_concession_allocations, advance.inventory_concession_allocation_serials
          IN ACCESS EXCLUSIVE MODE;
        LOCK TABLE advance.stock_posting_batches, advance.stock_movements IN ACCESS EXCLUSIVE MODE;
        """;

    private const string UpConstraints = """
        ALTER TABLE advance.qc_inspections DROP CONSTRAINT "CK_qc_inspection_source";
        ALTER TABLE advance.qc_inspections ADD CONSTRAINT "CK_qc_inspection_source"
          CHECK (("GoodsReceiptLineId" IS NOT NULL AND "GoodsReceiptLineLotAllocationId" IS NOT NULL AND "DeliveryChallanLineId" IS NULL)
              OR ("GoodsReceiptLineId" IS NULL AND "GoodsReceiptLineLotAllocationId" IS NULL AND "DeliveryChallanLineId" IS NOT NULL));
        ALTER TABLE advance.qc_inspection_revisions DROP CONSTRAINT "CK_qc_revision_quantities";
        ALTER TABLE advance.qc_inspection_revisions ADD CONSTRAINT "CK_qc_revision_quantities"
          CHECK ("RevisionNumber">0 AND "InspectedQuantity">0 AND "AcceptedQuantity">=0 AND "RejectedQuantity">=0
            AND "DiscrepancyPendingQuantity">=0
            AND "AcceptedQuantity"+"RejectedQuantity"+"DiscrepancyPendingQuantity"="InspectedQuantity"
            AND (("AcceptedQuantity">0)=("AcceptedConditionLocationId" IS NOT NULL)));
        ALTER TABLE advance.stock_posting_batches DROP CONSTRAINT "CK_stock_posting_batch_kind";
        ALTER TABLE advance.stock_posting_batches ADD CONSTRAINT "CK_stock_posting_batch_kind"
          CHECK ("PostingKind" IN ('GRN_CUSTODY','QC_DISPOSITION','CONCESSION_ACCEPTANCE','MATERIAL_ISSUE','DC_DISPATCH','DC_RETURN_CUSTODY','REVERSAL'));
        """;

    internal static string PostUp => string.Join("\n", UpConstraints, BuildControlledPosting(),
        BuildBatchGuard(), BuildMovementGuard(), BuildReconcileGuard(), SafeDocumentPostingGuard, EvidenceGuards);

    private const string SafeDocumentPostingGuard = """
        CREATE OR REPLACE FUNCTION advance.stores_p3b_document_posting_guard()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $function$
        BEGIN
          IF TG_TABLE_NAME='goods_receipts' THEN
            IF NEW."Status"='FINALIZED' AND NEW."DocumentKind"='NORMAL' AND
               (SELECT count(*) FROM advance.stock_posting_batches WHERE "GoodsReceiptId"=NEW."Id" AND "PostingKind"='GRN_CUSTODY')<>1
            THEN RAISE EXCEPTION 'Finalised GRN requires exactly one atomic custody posting batch.'; END IF;
          ELSIF TG_TABLE_NAME='qc_inspection_revisions' THEN
            IF NEW."Status"='FINALIZED' AND
               (SELECT count(*) FROM advance.stock_posting_batches WHERE "QcInspectionRevisionId"=NEW."Id" AND "PostingKind"='QC_DISPOSITION')<>1
            THEN RAISE EXCEPTION 'Finalised QC revision requires exactly one atomic disposition batch.'; END IF;
          ELSIF TG_TABLE_NAME='material_issue_requests' THEN
            IF NEW."Status" IN ('PARTIALLY_FULFILLED','FULFILLED') AND NOT EXISTS
               (SELECT 1 FROM advance.stock_posting_batches WHERE "MaterialIssueRequestId"=NEW."Id" AND "PostingKind"='MATERIAL_ISSUE')
            THEN RAISE EXCEPTION 'Fulfilled material request requires an approved atomic issue batch.'; END IF;
          ELSE
            IF NEW."Status" IN ('DISPATCHED','OUTSTANDING','PARTIALLY_RETURNED','RECEIVED','CLOSED') AND NOT EXISTS
               (SELECT 1 FROM advance.stock_posting_batches WHERE "DeliveryChallanId"=NEW."Id" AND "PostingKind" IN ('DC_DISPATCH','DC_RETURN_CUSTODY'))
            THEN RAISE EXCEPTION 'Delivery Challan inventory transition requires an atomic posting batch.'; END IF;
          END IF;
          RETURN NULL;
        END $function$;
        """;

    internal const string PreDown = """
        DO $guard$
        BEGIN
          IF current_setting('server_version_num')::integer < 170000 THEN RAISE EXCEPTION 'Stores Slice 3 rollback requires PostgreSQL 17 or later.'; END IF;
          IF current_database() IN ('postgres','template0','template1') THEN RAISE EXCEPTION 'Stores Slice 3 rollback refuses a PostgreSQL administrative database.'; END IF;
          IF EXISTS (SELECT 1 FROM advance.qc_inspections)
             OR EXISTS (SELECT 1 FROM advance.qc_inspection_revisions)
             OR EXISTS (SELECT 1 FROM advance.qc_inspection_lot_dispositions)
             OR EXISTS (SELECT 1 FROM advance.inventory_concessions)
             OR EXISTS (SELECT 1 FROM advance.stock_posting_batches WHERE "PostingKind" IN ('QC_DISPOSITION','CONCESSION_ACCEPTANCE')) THEN
            RAISE EXCEPTION 'Stores Slice 3 rollback refuses persisted QC, concession or posting evidence.';
          END IF;
        END $guard$;
        """;

    private const string EvidenceGuards = """
        CREATE OR REPLACE FUNCTION advance.qc_slice3_immutable_guard()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $function$
        BEGIN
          IF TG_OP<>'INSERT' THEN RAISE EXCEPTION '% is immutable; append a correction or reversal.',TG_TABLE_NAME; END IF;
          RETURN NEW;
        END $function$;
        CREATE TRIGGER "TR_qc_revision_immutable" BEFORE UPDATE OR DELETE ON advance.qc_inspection_revisions FOR EACH ROW EXECUTE FUNCTION advance.qc_slice3_immutable_guard();
        CREATE TRIGGER "TR_qc_parameter_immutable" BEFORE UPDATE OR DELETE ON advance.qc_inspection_parameter_results FOR EACH ROW EXECUTE FUNCTION advance.qc_slice3_immutable_guard();
        CREATE TRIGGER "TR_qc_serial_disposition_immutable" BEFORE UPDATE OR DELETE ON advance.qc_inspection_serial_dispositions FOR EACH ROW EXECUTE FUNCTION advance.qc_slice3_immutable_guard();
        CREATE TRIGGER "TR_qc_lot_disposition_immutable" BEFORE UPDATE OR DELETE ON advance.qc_inspection_lot_dispositions FOR EACH ROW EXECUTE FUNCTION advance.qc_slice3_immutable_guard();
        CREATE TRIGGER "TR_provenance_annotation_immutable" BEFORE UPDATE OR DELETE ON advance.inventory_provenance_annotations FOR EACH ROW EXECUTE FUNCTION advance.qc_slice3_immutable_guard();

        CREATE OR REPLACE FUNCTION advance.qc_slice3_concession_guard()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $function$
        DECLARE parent_status text;
        BEGIN
          IF TG_TABLE_NAME='inventory_concessions' THEN
            IF TG_OP='DELETE' OR OLD."Status"<>'DRAFT' OR NEW."Status" NOT IN ('APPROVED','REJECTED')
               OR (OLD."Id",OLD."CompanyId",OLD."QcInspectionRevisionId",OLD."QcInspectionLotDispositionId",
                   OLD."QcInspectionParameterResultId",OLD."RequestedQuantity",OLD."FailedParameterSnapshot",
                   OLD."MeasuredValueSnapshot",OLD."TechnicalAcceptanceReason",OLD."IntendedUse",
                   OLD."CreatedByEmployeeId",OLD."IdempotencyKey",OLD."RequestFingerprint")
                  IS DISTINCT FROM
                  (NEW."Id",NEW."CompanyId",NEW."QcInspectionRevisionId",NEW."QcInspectionLotDispositionId",
                   NEW."QcInspectionParameterResultId",NEW."RequestedQuantity",NEW."FailedParameterSnapshot",
                   NEW."MeasuredValueSnapshot",NEW."TechnicalAcceptanceReason",NEW."IntendedUse",
                   NEW."CreatedByEmployeeId",NEW."IdempotencyKey",NEW."RequestFingerprint") THEN
              RAISE EXCEPTION 'Approved/rejected concessions are immutable; correct by reversal and re-decision.';
            END IF;
            RETURN NEW;
          END IF;
          IF TG_OP='DELETE' THEN RAISE EXCEPTION 'Concession allocations are immutable evidence.'; END IF;
          IF TG_TABLE_NAME='inventory_concession_allocation_serials' THEN
            SELECT c."Status" INTO parent_status FROM advance.inventory_concession_allocations a
              JOIN advance.inventory_concessions c ON c."Id"=a."InventoryConcessionId" AND c."CompanyId"=a."CompanyId"
              WHERE a."CompanyId"=OLD."CompanyId" AND a."Id"=OLD."InventoryConcessionAllocationId";
          ELSE
            SELECT "Status" INTO parent_status FROM advance.inventory_concessions
              WHERE "CompanyId"=OLD."CompanyId" AND "Id"=OLD."InventoryConcessionId";
          END IF;
          IF parent_status<>'DRAFT' THEN RAISE EXCEPTION 'Approved/rejected concession allocations are immutable.'; END IF;
          RETURN NEW;
        END $function$;
        CREATE TRIGGER "TR_inventory_concession_immutable" BEFORE UPDATE OR DELETE ON advance.inventory_concessions FOR EACH ROW EXECUTE FUNCTION advance.qc_slice3_concession_guard();
        CREATE TRIGGER "TR_inventory_concession_allocation_immutable" BEFORE UPDATE OR DELETE ON advance.inventory_concession_allocations FOR EACH ROW EXECUTE FUNCTION advance.qc_slice3_concession_guard();
        CREATE TRIGGER "TR_inventory_concession_serial_immutable" BEFORE UPDATE OR DELETE ON advance.inventory_concession_allocation_serials FOR EACH ROW EXECUTE FUNCTION advance.qc_slice3_concession_guard();

        CREATE OR REPLACE FUNCTION advance.qc_slice3_inherit_annotations()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $function$
        BEGIN
          INSERT INTO advance.inventory_provenance_annotations
            ("Id","CompanyId","InventoryProvenanceLayerId","AnnotationType","AnnotationCode","DetailsJson",
             "InventoryConcessionId","InheritedFromAnnotationId","CreatedAt","CreatedBy")
          SELECT gen_random_uuid(),NEW."CompanyId",NEW."ToProvenanceLayerId",a."AnnotationType",a."AnnotationCode",
                 a."DetailsJson",a."InventoryConcessionId",a."Id",clock_timestamp(),NEW."CreatedBy"
          FROM advance.inventory_provenance_annotations a
          WHERE a."CompanyId"=NEW."CompanyId" AND a."InventoryProvenanceLayerId"=NEW."FromProvenanceLayerId"
          ON CONFLICT ("CompanyId","InventoryProvenanceLayerId","AnnotationType","AnnotationCode") DO NOTHING;
          RETURN NEW;
        END $function$;
        CREATE TRIGGER "TR_inventory_provenance_annotation_inheritance"
          AFTER INSERT ON advance.inventory_provenance_edges FOR EACH ROW EXECUTE FUNCTION advance.qc_slice3_inherit_annotations();
        """;

    private static string BuildControlledPosting()
    {
        var sql = Normalize(Foundation3InventoryProvenanceGenealogySql.ControlledPosting);
        sql = ReplaceRequired(sql,
            "p_posting_kind NOT IN ('GRN_CUSTODY','QC_DISPOSITION','MATERIAL_ISSUE','DC_DISPATCH','DC_RETURN_CUSTODY','REVERSAL')",
            "p_posting_kind NOT IN ('GRN_CUSTODY','QC_DISPOSITION','CONCESSION_ACCEPTANCE','MATERIAL_ISSUE','DC_DISPATCH','DC_RETURN_CUSTODY','REVERSAL')");
        sql = ReplaceRequired(sql,
            Q("ELSIF p_posting_kind IN ('DC_DISPATCH','DC_RETURN_CUSTODY') THEN ref_type:='DELIVERY_CHALLAN'; SELECT ~CompanyId~,~DcNumber~ INTO source_company,ref_number FROM advance.delivery_challans WHERE ~Id~=p_source_id FOR UPDATE;"),
            Q("ELSIF p_posting_kind IN ('DC_DISPATCH','DC_RETURN_CUSTODY') THEN ref_type:='DELIVERY_CHALLAN'; SELECT ~CompanyId~,~DcNumber~ INTO source_company,ref_number FROM advance.delivery_challans WHERE ~Id~=p_source_id FOR UPDATE;\n          ELSIF p_posting_kind='CONCESSION_ACCEPTANCE' THEN ref_type:='INVENTORY_CONCESSION'; SELECT ~CompanyId~,~ConcessionNumber~ INTO source_company,ref_number FROM advance.inventory_concessions WHERE ~Id~=p_source_id FOR UPDATE;"));
        sql = ReplaceRequired(sql,
            Q("~GoodsReceiptId~,~QcInspectionRevisionId~,~MaterialIssueRequestId~,~DeliveryChallanId~,~ReversesPostingBatchId~"),
            Q("~GoodsReceiptId~,~QcInspectionRevisionId~,~MaterialIssueRequestId~,~DeliveryChallanId~,~InventoryConcessionId~,~ReversesPostingBatchId~"));
        sql = ReplaceRequired(sql,
            "CASE WHEN p_posting_kind='REVERSAL' THEN p_source_id END,",
            "CASE WHEN p_posting_kind='CONCESSION_ACCEPTANCE' THEN p_source_id END,\n            CASE WHEN p_posting_kind='REVERSAL' THEN p_source_id END,");
        return sql;
    }

    private static string BuildBatchGuard()
    {
        var sql = ExtractFunction("stores_p3b_batch_insert_guard");
        return ReplaceRequired(sql,
            "          ELSE\n            expected_type:='REVERSAL';",
            Q("          ELSIF NEW.~PostingKind~='CONCESSION_ACCEPTANCE' THEN\n            expected_type:='INVENTORY_CONCESSION';\n            SELECT ~CompanyId~,~ConcessionNumber~ INTO source_company,expected_number FROM advance.inventory_concessions WHERE ~Id~=NEW.~InventoryConcessionId~;\n          ELSE\n            expected_type:='REVERSAL';"));
    }

    private static string BuildMovementGuard()
    {
        var sql = ExtractFunction("stores_p3b_movement_guard");
        sql = ReplaceRequired(sql,
            Q("          ELSIF NEW.~QcInspectionRevisionId~ IS NOT NULL THEN"),
            Q("          ELSIF NEW.~QcInspectionLotDispositionId~ IS NOT NULL THEN"));
        sql = ReplaceRequired(sql,
            Q("FROM advance.qc_inspection_revisions r JOIN advance.qc_inspections i ON i.~Id~=r.~QcInspectionId~\n              LEFT JOIN advance.goods_receipt_lines gl ON gl.~Id~=i.~GoodsReceiptLineId~\n              LEFT JOIN advance.delivery_challan_lines dl ON dl.~Id~=i.~DeliveryChallanLineId~ WHERE r.~Id~=NEW.~QcInspectionRevisionId~;"),
            Q("FROM advance.qc_inspection_lot_dispositions d JOIN advance.qc_inspection_revisions r ON r.~Id~=d.~QcInspectionRevisionId~ JOIN advance.qc_inspections i ON i.~Id~=r.~QcInspectionId~\n              LEFT JOIN advance.goods_receipt_lines gl ON gl.~Id~=i.~GoodsReceiptLineId~\n              LEFT JOIN advance.delivery_challan_lines dl ON dl.~Id~=i.~DeliveryChallanLineId~ WHERE d.~Id~=NEW.~QcInspectionLotDispositionId~;"));
        sql = ReplaceRequired(sql,
            Q("WHERE r.~Id~=NEW.~QcInspectionRevisionId~ AND"),
            Q("JOIN advance.qc_inspection_lot_dispositions d ON d.~QcInspectionRevisionId~=r.~Id~ WHERE d.~Id~=NEW.~QcInspectionLotDispositionId~ AND"));
        var concession = """
          ELSIF NEW."InventoryConcessionAllocationId" IS NOT NULL THEN
            SELECT c."CompanyId",c."Id",gl."ItemId" INTO source_company,source_header,source_item
              FROM advance.inventory_concession_allocations a
              JOIN advance.inventory_concessions c ON c."Id"=a."InventoryConcessionId"
              JOIN advance.goods_receipt_line_lot_allocations la ON la."Id"=a."GoodsReceiptLineLotAllocationId"
              JOIN advance.goods_receipt_lines gl ON gl."Id"=la."GoodsReceiptLineId"
              WHERE a."Id"=NEW."InventoryConcessionAllocationId";
            IF b."PostingKind" NOT IN ('CONCESSION_ACCEPTANCE','REVERSAL')
               OR (b."PostingKind"='CONCESSION_ACCEPTANCE' AND source_header<>b."InventoryConcessionId")
            THEN RAISE EXCEPTION 'Concession movement source does not match its batch.'; END IF;
            IF b."PostingKind"='CONCESSION_ACCEPTANCE' AND
               NOT ((NEW."MovementLeg"='TRANSFER_OUT' AND NEW."ConditionCode"='PENDING_RETURNABLE_DC')
                 OR (NEW."MovementLeg"='TRANSFER_IN' AND NEW."ConditionCode"='AVAILABLE'))
            THEN RAISE EXCEPTION 'Concession must transfer rejected custody to AVAILABLE only.'; END IF;
        """;
        return ReplaceRequired(sql,
            Q("          ELSIF NEW.~MaterialIssueRequestLineId~ IS NOT NULL THEN"),
            concession + Q("\n          ELSIF NEW.~MaterialIssueRequestLineId~ IS NOT NULL THEN"));
    }

    private static string BuildReconcileGuard()
    {
        var sql = SafeReconcileGuard(ExtractFunction("stores_p3b_reconcile_batch"));
        var concession = """
          ELSIF b."PostingKind"='CONCESSION_ACCEPTANCE' THEN
            SELECT "Status" INTO source_status FROM advance.inventory_concessions WHERE "Id"=b."InventoryConcessionId";
            IF source_status<>'APPROVED'
               OR (SELECT coalesce(sum("QuantityIn"),0) FROM advance.stock_movements WHERE "StockPostingBatchId"=b."Id")<>(SELECT "RequestedQuantity" FROM advance.inventory_concessions WHERE "Id"=b."InventoryConcessionId")
               OR (SELECT coalesce(sum("QuantityOut"),0) FROM advance.stock_movements WHERE "StockPostingBatchId"=b."Id")<>(SELECT "RequestedQuantity" FROM advance.inventory_concessions WHERE "Id"=b."InventoryConcessionId")
               OR EXISTS (SELECT 1 FROM advance.stock_movements WHERE "StockPostingBatchId"=b."Id" AND
                    NOT (("MovementLeg"='TRANSFER_OUT' AND "ConditionCode"='PENDING_RETURNABLE_DC')
                      OR ("MovementLeg"='TRANSFER_IN' AND "ConditionCode"='AVAILABLE'))) THEN
              RAISE EXCEPTION 'Concession acceptance batch must exactly reconcile rejected-to-AVAILABLE quantity.';
            END IF;
        """;
        return ReplaceRequired(sql, "          ELSE\n            IF (SELECT count(*)",
            concession + "\n          ELSE\n            IF (SELECT count(*)");
    }

    private static string ExtractFunction(string functionName)
    {
        var sql = Normalize(FirstStoresPart3BSql.Up);
        var start = sql.IndexOf($"CREATE OR REPLACE FUNCTION advance.{functionName}()", StringComparison.Ordinal);
        if (start < 0) throw new InvalidOperationException($"Stores function {functionName} was not found.");
        var triggerEnd = sql.IndexOf("\nCREATE TRIGGER", start, StringComparison.Ordinal);
        var constraintEnd = sql.IndexOf("\nCREATE CONSTRAINT TRIGGER", start, StringComparison.Ordinal);
        var end = triggerEnd < 0 ? constraintEnd : constraintEnd < 0 ? triggerEnd : Math.Min(triggerEnd, constraintEnd);
        if (end < 0) throw new InvalidOperationException($"Stores function {functionName} terminator was not found.");
        return sql[start..end].Trim();
    }

    private static string ReplaceRequired(string source, string oldValue, string newValue)
    {
        if (source.Contains(oldValue, StringComparison.Ordinal))
            return source.Replace(oldValue, newValue, StringComparison.Ordinal);
        var dedentedOld = Dedent(oldValue);
        if (source.Contains(dedentedOld, StringComparison.Ordinal))
            return source.Replace(dedentedOld, Dedent(newValue), StringComparison.Ordinal);
        throw new InvalidOperationException($"Required Stores Slice 3 SQL marker was not found: {oldValue}");
    }

    private static string Normalize(string value) => value.Replace("\r\n", "\n", StringComparison.Ordinal);
    private static string Dedent(string value) => string.Join('\n', value.Split('\n').Select(line => line.StartsWith("        ", StringComparison.Ordinal) ? line[8..] : line));
    private static string Q(string value) => value.Replace('~', '"');
    private static string SafeReconcileGuard(string value) => ReplaceRequired(value,
        Q("  batch_id:=CASE WHEN TG_TABLE_NAME='stock_posting_batches' THEN NEW.~Id~ ELSE NEW.~StockPostingBatchId~ END;"),
        Q("  IF TG_TABLE_NAME='stock_posting_batches' THEN batch_id:=NEW.~Id~; ELSE batch_id:=NEW.~StockPostingBatchId~; END IF;"));

    internal static string BeforeDown => string.Join("\n",
        """
        DROP TRIGGER IF EXISTS "TR_inventory_provenance_annotation_inheritance" ON advance.inventory_provenance_edges;
        DROP TRIGGER IF EXISTS "TR_inventory_concession_serial_immutable" ON advance.inventory_concession_allocation_serials;
        DROP TRIGGER IF EXISTS "TR_inventory_concession_allocation_immutable" ON advance.inventory_concession_allocations;
        DROP TRIGGER IF EXISTS "TR_inventory_concession_immutable" ON advance.inventory_concessions;
        DROP TRIGGER IF EXISTS "TR_provenance_annotation_immutable" ON advance.inventory_provenance_annotations;
        DROP TRIGGER IF EXISTS "TR_qc_lot_disposition_immutable" ON advance.qc_inspection_lot_dispositions;
        DROP TRIGGER IF EXISTS "TR_qc_serial_disposition_immutable" ON advance.qc_inspection_serial_dispositions;
        DROP TRIGGER IF EXISTS "TR_qc_parameter_immutable" ON advance.qc_inspection_parameter_results;
        DROP TRIGGER IF EXISTS "TR_qc_revision_immutable" ON advance.qc_inspection_revisions;
        DROP FUNCTION IF EXISTS advance.qc_slice3_inherit_annotations();
        DROP FUNCTION IF EXISTS advance.qc_slice3_concession_guard();
        DROP FUNCTION IF EXISTS advance.qc_slice3_immutable_guard();
        ALTER TABLE advance.stock_posting_batches DROP CONSTRAINT "CK_stock_posting_batch_kind";
        ALTER TABLE advance.stock_posting_batches ADD CONSTRAINT "CK_stock_posting_batch_kind"
          CHECK ("PostingKind" IN ('GRN_CUSTODY','QC_DISPOSITION','MATERIAL_ISSUE','DC_DISPATCH','DC_RETURN_CUSTODY','REVERSAL'));
        """,
        Foundation3InventoryProvenanceGenealogySql.ControlledPosting,
        ExtractFunction("stores_p3b_batch_insert_guard"),
        ExtractFunction("stores_p3b_movement_guard"),
        SafeReconcileGuard(ExtractFunction("stores_p3b_reconcile_batch")));

    internal const string PostDown = """
        -- Dropping the lot-allocation column can remove its dependent check constraint first.
        ALTER TABLE advance.qc_inspections DROP CONSTRAINT IF EXISTS "CK_qc_inspection_source";
        ALTER TABLE advance.qc_inspections ADD CONSTRAINT "CK_qc_inspection_source"
          CHECK (num_nonnulls("GoodsReceiptLineId","DeliveryChallanLineId")=1);
        ALTER TABLE advance.qc_inspection_revisions DROP CONSTRAINT IF EXISTS "CK_qc_revision_quantities";
        ALTER TABLE advance.qc_inspection_revisions ADD CONSTRAINT "CK_qc_revision_quantities"
          CHECK ("RevisionNumber">0 AND "InspectedQuantity">=0 AND "AcceptedQuantity">=0 AND "RejectedQuantity">=0
            AND "InspectionShortfallRejectedQuantity">=0 AND "AcceptedQuantity"+"RejectedQuantity">="InspectedQuantity"
            AND (("AcceptedQuantity">0)=("AcceptedConditionLocationId" IS NOT NULL)));
        """;
}
