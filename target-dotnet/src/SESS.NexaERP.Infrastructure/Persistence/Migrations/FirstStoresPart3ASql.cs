namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class FirstStoresPart3ASql
{
    internal static string PreUp => AdvanceSchemaSql.Expand(PreUpSql);
    internal static string Up => AdvanceSchemaSql.Expand(UpSql);
    internal static string Down => AdvanceSchemaSql.Expand(DownSql);

    private const string PreUpSql = """
        DO $guard$
        DECLARE required_table text;
        BEGIN
          IF current_setting('server_version_num')::integer < 170000 THEN
            RAISE EXCEPTION 'First Stores Part 3A requires PostgreSQL 17 or later.';
          END IF;
          IF current_database() IN ('postgres','template0','template1') THEN
            RAISE EXCEPTION 'First Stores Part 3A refuses a PostgreSQL administrative database.';
          END IF;
          IF to_regnamespace('__advance_schema__') IS NULL THEN
            RAISE EXCEPTION 'First Stores Part 3A requires the advance schema.';
          END IF;
          FOREACH required_table IN ARRAY ARRAY[
            'business_rule_configuration_versions','stores_document_status_history',
            'goods_receipts','goods_receipt_lines','inventory_serials','goods_receipt_line_serials',
            'qc_inspection_policies','warehouse_condition_locations','stock_movements'
          ] LOOP
            IF to_regclass('__advance_schema__.' || required_table) IS NULL THEN
              RAISE EXCEPTION 'First Stores Part 3A requires earlier table %.', required_table;
            END IF;
          END LOOP;
          IF to_regclass('__advance_schema__.stock_posting_batches') IS NOT NULL THEN
            RAISE EXCEPTION 'First Stores Part 3A must apply before Part 3B.';
          END IF;
          IF EXISTS (SELECT 1 FROM unnest(ARRAY[
            'qc_inspections','qc_inspection_revisions','qc_inspection_parameter_results',
            'qc_inspection_serial_dispositions','job_orders','material_issue_requests',
            'material_issue_request_lines','stores_approval_history','delivery_challans',
            'delivery_challan_lines']) x(name)
            WHERE to_regclass('__advance_schema__.' || x.name) IS NOT NULL) THEN
            RAISE EXCEPTION 'First Stores Part 3A refuses a partial or replayed schema.';
          END IF;
        END $guard$;

        LOCK TABLE __advance_schema__.stores_document_status_history IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.goods_receipts IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.stock_movements IN SHARE ROW EXCLUSIVE MODE;
        """;

    private const string UpSql = """
        -- PART 3A SAFETY CONTRACT: document drafts exist; inventory transitions do not.
        ALTER TABLE __advance_schema__.stores_document_status_history
          DROP CONSTRAINT "CK_stores_document_status_part2_source";
        ALTER TABLE __advance_schema__.stores_document_status_history
          ADD CONSTRAINT "CK_stores_document_status_part3a_source"
          CHECK (num_nonnulls("GateEntryId","GoodsReceiptId","QcInspectionRevisionId",
                              "JobOrderId","MaterialIssueRequestId","DeliveryChallanId")=1);

        ALTER TABLE __advance_schema__.qc_inspections ADD CONSTRAINT "CK_qc_inspection_source"
          CHECK (num_nonnulls("GoodsReceiptLineId","DeliveryChallanLineId")=1);
        ALTER TABLE __advance_schema__.qc_inspection_revisions ADD CONSTRAINT "CK_qc_revision_status"
          CHECK ("Status" IN ('DRAFT','FINALIZED'));
        ALTER TABLE __advance_schema__.qc_inspection_parameter_results ADD CONSTRAINT "CK_qc_parameter_result"
          CHECK ("Result" IN ('PASS','FAIL') AND "SampleOrdinal">0
                 AND "RequiredSampleSizeSnapshot">= "SampleOrdinal"
                 AND num_nonnulls("ObservedNumericValue","ObservedTextValue")=1);
        ALTER TABLE __advance_schema__.qc_inspection_serial_dispositions ADD CONSTRAINT "CK_qc_serial_disposition"
          CHECK ("Disposition" IN ('ACCEPTED','REJECTED'));
        ALTER TABLE __advance_schema__.material_issue_request_lines ADD CONSTRAINT "CK_mir_line_quantity"
          CHECK ("LineNumber">0 AND "RequestedQuantity">0);
        ALTER TABLE __advance_schema__.stores_approval_history ADD CONSTRAINT "CK_stores_approval_source"
          CHECK (num_nonnulls("MaterialIssueRequestId","DeliveryChallanId")=1
                 AND "ApprovalCycle">0 AND "StepNumber">0
                 AND "Action" IN ('APPROVE','REJECT','REQUEST_REVISION'));
        ALTER TABLE __advance_schema__.delivery_challan_lines ADD CONSTRAINT "CK_dc_line_quantity"
          CHECK ("LineNumber">0 AND "Quantity">0 AND ("InventorySerialId" IS NULL OR "Quantity"=1));
        ALTER TABLE __advance_schema__.job_orders ADD CONSTRAINT "CK_job_order_lifecycle"
          CHECK ("Status" IN ('DRAFT','OPEN','INSTALLED','CLOSED')
            AND ("Status" NOT IN ('INSTALLED','CLOSED') OR "InstallationDate" IS NOT NULL)
            AND (("Status"='CLOSED')=("ClosedAt" IS NOT NULL)));
        ALTER TABLE __advance_schema__.material_issue_requests ADD CONSTRAINT "CK_mir_lifecycle"
          CHECK ("Purpose" IN ('FACTORY_ASSEMBLY','PROJECT','SERVICE','WARRANTY','DEMO','SALE','FREE_OF_COST')
            AND "DestinationType" IN ('JOB_ORDER','CUSTOMER','VENDOR','DEPARTMENT','OTHER')
            AND "Status" IN ('DRAFT','SUBMITTED','APPROVED','REJECTED','PARTIALLY_FULFILLED','FULFILLED','REVERSED')
            AND jsonb_typeof("ApprovalRouteSnapshotJson")='object');
        ALTER TABLE __advance_schema__.material_issue_requests ADD CONSTRAINT "CK_mir_destination"
          CHECK (("DestinationType"='JOB_ORDER' AND "JobOrderId" IS NOT NULL AND num_nonnulls("JobOrderId","CustomerId","VendorId","DestinationDepartmentId")=1)
            OR ("DestinationType"='CUSTOMER' AND "CustomerId" IS NOT NULL AND num_nonnulls("JobOrderId","CustomerId","VendorId","DestinationDepartmentId")=1)
            OR ("DestinationType"='VENDOR' AND "VendorId" IS NOT NULL AND num_nonnulls("JobOrderId","CustomerId","VendorId","DestinationDepartmentId")=1)
            OR ("DestinationType"='DEPARTMENT' AND "DestinationDepartmentId" IS NOT NULL AND num_nonnulls("JobOrderId","CustomerId","VendorId","DestinationDepartmentId")=1)
            OR ("DestinationType"='OTHER' AND num_nonnulls("JobOrderId","CustomerId","VendorId","DestinationDepartmentId")=0));
        ALTER TABLE __advance_schema__.delivery_challans ADD CONSTRAINT "CK_dc_lifecycle"
          CHECK ("Direction" IN ('OUTBOUND','INBOUND_RETURN') AND "DcType" IN ('RETURNABLE','NON_RETURNABLE')
            AND "Purpose" IN ('REJECTED_MATERIAL','SUBCONTRACT','DEMO','WARRANTY','BILL_BASED','CUSTOMER_PO_BASED')
            AND "Status" IN ('DRAFT','SUBMITTED','APPROVED','DISPATCHED','OUTSTANDING','PARTIALLY_RETURNED','RECEIVED','CLOSED','REVERSED')
            AND (("Direction"='OUTBOUND' AND "ParentDeliveryChallanId" IS NULL) OR ("Direction"='INBOUND_RETURN' AND "ParentDeliveryChallanId" IS NOT NULL AND "DcType"='RETURNABLE'))
            AND ("Direction"<>'OUTBOUND' OR "DcType"<>'RETURNABLE' OR "ExpectedReturnDate" IS NOT NULL)
            AND (("Purpose" IN ('REJECTED_MATERIAL','SUBCONTRACT','DEMO') AND "DcType"='RETURNABLE') OR ("Purpose" IN ('WARRANTY','BILL_BASED','CUSTOMER_PO_BASED') AND "DcType"='NON_RETURNABLE')));
        ALTER TABLE __advance_schema__.qc_inspection_revisions ADD CONSTRAINT "CK_qc_revision_quantities"
          CHECK ("RevisionNumber">0 AND "InspectedQuantity">=0 AND "AcceptedQuantity">=0 AND "RejectedQuantity">=0
            AND "InspectionShortfallRejectedQuantity">=0 AND "AcceptedQuantity"+"RejectedQuantity">="InspectedQuantity"
            AND (("AcceptedQuantity">0)=("AcceptedConditionLocationId" IS NOT NULL)));

        CREATE OR REPLACE FUNCTION __advance_schema__.stores_p3a_transition_block()
        RETURNS trigger LANGUAGE plpgsql
        SET search_path = pg_catalog, __advance_schema__
        AS $$
        BEGIN
          IF TG_TABLE_NAME='qc_inspection_revisions' AND NEW."Status"='FINALIZED' THEN
            RAISE EXCEPTION 'QC finalisation is disabled until Stores Part 3B installs atomic ledger posting.';
          ELSIF TG_TABLE_NAME='material_issue_requests'
                AND NEW."Status" IN ('PARTIALLY_FULFILLED','FULFILLED','REVERSED') THEN
            RAISE EXCEPTION 'Material issue posting/finalisation is disabled until Stores Part 3B.';
          ELSIF TG_TABLE_NAME='delivery_challans'
                AND NEW."Status" IN ('DISPATCHED','OUTSTANDING','PARTIALLY_RETURNED','RECEIVED','CLOSED','REVERSED') THEN
            RAISE EXCEPTION 'DC dispatch, receipt and finalisation are disabled until Stores Part 3B.';
          END IF;
          RETURN NEW;
        END $$;
        CREATE TRIGGER "TR_qc_revision_part3a_block"
          BEFORE INSERT OR UPDATE ON __advance_schema__.qc_inspection_revisions
          FOR EACH ROW EXECUTE FUNCTION __advance_schema__.stores_p3a_transition_block();
        CREATE TRIGGER "TR_mir_part3a_block"
          BEFORE INSERT OR UPDATE ON __advance_schema__.material_issue_requests
          FOR EACH ROW EXECUTE FUNCTION __advance_schema__.stores_p3a_transition_block();
        CREATE TRIGGER "TR_dc_part3a_block"
          BEFORE INSERT OR UPDATE ON __advance_schema__.delivery_challans
          FOR EACH ROW EXECUTE FUNCTION __advance_schema__.stores_p3a_transition_block();

        CREATE OR REPLACE FUNCTION __advance_schema__.stores_p3a_append_only_guard()
        RETURNS trigger LANGUAGE plpgsql SET search_path = pg_catalog
        AS $$ BEGIN
          IF TG_OP<>'INSERT' THEN RAISE EXCEPTION '% is append-only.', TG_TABLE_NAME; END IF;
          RETURN NEW;
        END $$;
        CREATE TRIGGER "TR_qc_inspection_identity_immutable"
          BEFORE UPDATE OR DELETE ON __advance_schema__.qc_inspections
          FOR EACH ROW EXECUTE FUNCTION __advance_schema__.stores_p3a_append_only_guard();
        CREATE TRIGGER "TR_stores_approval_history_append_only"
          BEFORE UPDATE OR DELETE ON __advance_schema__.stores_approval_history
          FOR EACH ROW EXECUTE FUNCTION __advance_schema__.stores_p3a_append_only_guard();

        CREATE OR REPLACE FUNCTION __advance_schema__.stores_p1_status_history_guard()
        RETURNS trigger LANGUAGE plpgsql
        SET search_path = pg_catalog, __advance_schema__
        AS $$
        DECLARE source_company uuid;
        BEGIN
          IF TG_OP<>'INSERT' THEN RAISE EXCEPTION 'Stores document status history is append-only.'; END IF;
          IF num_nonnulls(NEW."GateEntryId",NEW."GoodsReceiptId",NEW."QcInspectionRevisionId",
                          NEW."JobOrderId",NEW."MaterialIssueRequestId",NEW."DeliveryChallanId")<>1 THEN
            RAISE EXCEPTION 'Status history requires exactly one typed source.';
          END IF;
          IF NEW."GateEntryId" IS NOT NULL THEN SELECT "CompanyId" INTO source_company FROM __advance_schema__.gate_entries WHERE "Id"=NEW."GateEntryId";
          ELSIF NEW."GoodsReceiptId" IS NOT NULL THEN SELECT "CompanyId" INTO source_company FROM __advance_schema__.goods_receipts WHERE "Id"=NEW."GoodsReceiptId";
          ELSIF NEW."QcInspectionRevisionId" IS NOT NULL THEN SELECT "CompanyId" INTO source_company FROM __advance_schema__.qc_inspection_revisions WHERE "Id"=NEW."QcInspectionRevisionId";
          ELSIF NEW."JobOrderId" IS NOT NULL THEN SELECT "CompanyId" INTO source_company FROM __advance_schema__.job_orders WHERE "Id"=NEW."JobOrderId";
          ELSIF NEW."MaterialIssueRequestId" IS NOT NULL THEN SELECT "CompanyId" INTO source_company FROM __advance_schema__.material_issue_requests WHERE "Id"=NEW."MaterialIssueRequestId";
          ELSE SELECT "CompanyId" INTO source_company FROM __advance_schema__.delivery_challans WHERE "Id"=NEW."DeliveryChallanId";
          END IF;
          IF source_company IS NULL OR source_company<>NEW."CompanyId" THEN RAISE EXCEPTION 'Status history company must match its typed source.'; END IF;
          IF NOT __advance_schema__.stores_p1_actor_has_role(NEW."ActorEmployeeId",NEW."CompanyId",NEW."ActorRoleCode",NEW."OccurredAt"::date) THEN
            RAISE EXCEPTION 'Status actor lacks the recorded role in this company.';
          END IF;
          RETURN NEW;
        END $$;

        DO $witness$
        BEGIN
          IF (SELECT count(*) FROM __advance_schema__.qc_inspections)
            +(SELECT count(*) FROM __advance_schema__.qc_inspection_revisions)
            +(SELECT count(*) FROM __advance_schema__.qc_inspection_parameter_results)
            +(SELECT count(*) FROM __advance_schema__.qc_inspection_serial_dispositions)
            +(SELECT count(*) FROM __advance_schema__.job_orders)
            +(SELECT count(*) FROM __advance_schema__.material_issue_requests)
            +(SELECT count(*) FROM __advance_schema__.material_issue_request_lines)
            +(SELECT count(*) FROM __advance_schema__.stores_approval_history)
            +(SELECT count(*) FROM __advance_schema__.delivery_challans)
            +(SELECT count(*) FROM __advance_schema__.delivery_challan_lines)<>0 THEN
            RAISE EXCEPTION 'First Stores Part 3A tables must be empty immediately after apply.';
          END IF;
          IF to_regclass('__advance_schema__.stock_posting_batches') IS NOT NULL THEN
            RAISE EXCEPTION 'Part 3A must not create the Part 3B posting table.';
          END IF;
          IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='TR_qc_revision_part3a_block' AND tgenabled<>'D')
             OR NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='TR_mir_part3a_block' AND tgenabled<>'D')
             OR NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='TR_dc_part3a_block' AND tgenabled<>'D') THEN
            RAISE EXCEPTION 'Part 3A transition blockers are not all active.';
          END IF;
        END $witness$;
        """;

    private const string DownSql = """
        DO $guard$
        BEGIN
          IF current_setting('server_version_num')::integer < 170000 THEN RAISE EXCEPTION 'First Stores Part 3A down requires PostgreSQL 17 or later.'; END IF;
          IF current_database() IN ('postgres','template0','template1') THEN RAISE EXCEPTION 'First Stores Part 3A down refuses a PostgreSQL administrative database.'; END IF;
          IF to_regclass('__advance_schema__.stock_posting_batches') IS NOT NULL THEN RAISE EXCEPTION 'Remove Stores Part 3B before Part 3A.'; END IF;
          IF (SELECT count(*) FROM __advance_schema__.qc_inspections)
            +(SELECT count(*) FROM __advance_schema__.qc_inspection_revisions)
            +(SELECT count(*) FROM __advance_schema__.qc_inspection_parameter_results)
            +(SELECT count(*) FROM __advance_schema__.qc_inspection_serial_dispositions)
            +(SELECT count(*) FROM __advance_schema__.job_orders)
            +(SELECT count(*) FROM __advance_schema__.material_issue_requests)
            +(SELECT count(*) FROM __advance_schema__.material_issue_request_lines)
            +(SELECT count(*) FROM __advance_schema__.stores_approval_history)
            +(SELECT count(*) FROM __advance_schema__.delivery_challans)
            +(SELECT count(*) FROM __advance_schema__.delivery_challan_lines)<>0
            OR EXISTS (SELECT 1 FROM __advance_schema__.stores_document_status_history
                       WHERE "QcInspectionRevisionId" IS NOT NULL OR "JobOrderId" IS NOT NULL
                          OR "MaterialIssueRequestId" IS NOT NULL OR "DeliveryChallanId" IS NOT NULL) THEN
            RAISE EXCEPTION 'First Stores Part 3A rollback refuses persisted document or history data.';
          END IF;
        END $guard$;

        DROP TRIGGER IF EXISTS "TR_qc_revision_part3a_block" ON __advance_schema__.qc_inspection_revisions;
        DROP TRIGGER IF EXISTS "TR_mir_part3a_block" ON __advance_schema__.material_issue_requests;
        DROP TRIGGER IF EXISTS "TR_dc_part3a_block" ON __advance_schema__.delivery_challans;
        DROP TRIGGER IF EXISTS "TR_qc_inspection_identity_immutable" ON __advance_schema__.qc_inspections;
        DROP TRIGGER IF EXISTS "TR_stores_approval_history_append_only" ON __advance_schema__.stores_approval_history;
        DROP FUNCTION IF EXISTS __advance_schema__.stores_p3a_transition_block();
        DROP FUNCTION IF EXISTS __advance_schema__.stores_p3a_append_only_guard();

        ALTER TABLE __advance_schema__.stores_document_status_history DROP CONSTRAINT "CK_stores_document_status_part3a_source";
        ALTER TABLE __advance_schema__.stores_document_status_history ADD CONSTRAINT "CK_stores_document_status_part2_source"
          CHECK (num_nonnulls("GateEntryId","GoodsReceiptId")=1);

        CREATE OR REPLACE FUNCTION __advance_schema__.stores_p1_status_history_guard()
        RETURNS trigger LANGUAGE plpgsql SET search_path = pg_catalog, __advance_schema__ AS $$
        DECLARE source_company uuid;
        BEGIN
          IF TG_OP<>'INSERT' THEN RAISE EXCEPTION 'Stores document status history is append-only.'; END IF;
          IF num_nonnulls(NEW."GateEntryId",NEW."GoodsReceiptId")<>1 THEN RAISE EXCEPTION 'Status history requires exactly one typed source.'; END IF;
          IF NEW."GateEntryId" IS NOT NULL THEN SELECT "CompanyId" INTO source_company FROM __advance_schema__.gate_entries WHERE "Id"=NEW."GateEntryId";
          ELSE SELECT "CompanyId" INTO source_company FROM __advance_schema__.goods_receipts WHERE "Id"=NEW."GoodsReceiptId"; END IF;
          IF source_company IS NULL OR source_company<>NEW."CompanyId" THEN RAISE EXCEPTION 'Status history company must match its typed source.'; END IF;
          IF NOT __advance_schema__.stores_p1_actor_has_role(NEW."ActorEmployeeId",NEW."CompanyId",NEW."ActorRoleCode",NEW."OccurredAt"::date) THEN RAISE EXCEPTION 'Status actor lacks the recorded role in this company.'; END IF;
          RETURN NEW;
        END $$;
        """;
}
