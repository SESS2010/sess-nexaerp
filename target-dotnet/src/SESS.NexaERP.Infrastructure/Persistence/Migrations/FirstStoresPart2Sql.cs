namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class FirstStoresPart2Sql
{
    internal static string PreUp => AdvanceSchemaSql.Expand(PreUpSql);
    internal static string Up => AdvanceSchemaSql.Expand(UpSql);
    internal static string Down => AdvanceSchemaSql.Expand(DownSql);

    private const string PreUpSql = """
        DO $guard$
        DECLARE required_table text;
        BEGIN
          IF current_setting('server_version_num')::integer < 170000 THEN
            RAISE EXCEPTION 'First Stores Part 2 requires PostgreSQL 17 or later.';
          END IF;
          IF current_database() IN ('postgres','template0','template1') THEN
            RAISE EXCEPTION 'First Stores Part 2 refuses a PostgreSQL administrative database.';
          END IF;
          IF to_regnamespace('__advance_schema__') IS NULL THEN
            RAISE EXCEPTION 'First Stores Part 2 requires the advance schema.';
          END IF;
          FOREACH required_table IN ARRAY ARRAY[
            'business_rule_configuration_versions','item_company_inventory_settings','store_category_routes',
            'stores_document_status_history','gate_entries','gate_entry_lines','purchase_orders',
            'purchase_order_lines','items','item_categories','warehouse_condition_locations'
          ] LOOP
            IF to_regclass('__advance_schema__.' || required_table) IS NULL THEN
              RAISE EXCEPTION 'First Stores Part 2 requires Part 1 table %.', required_table;
            END IF;
          END LOOP;
          IF EXISTS (
            SELECT 1 FROM unnest(ARRAY[
              'goods_receipts','goods_receipt_lines','inventory_serials','goods_receipt_line_serials'
            ]) x(name) WHERE to_regclass('__advance_schema__.' || x.name) IS NOT NULL
          ) THEN RAISE EXCEPTION 'First Stores Part 2 refuses a partial or replayed schema.'; END IF;
          IF (SELECT count(*) FROM __advance_schema__.business_rule_configuration_versions)<>18
             OR (SELECT count(*) FROM __advance_schema__.business_rule_configuration_versions
                 WHERE "RuleKey" IN ('SERIAL_CAPTURE_THRESHOLD','QC_COMPLETION_DAYS'))<>4 THEN
            RAISE EXCEPTION 'First Stores Part 2 requires the witnessed Part 1 configuration manifest.';
          END IF;
        END $guard$;

        LOCK TABLE __advance_schema__.business_rule_configuration_versions IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.gate_entries IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.gate_entry_lines IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.purchase_orders IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.purchase_order_lines IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.stores_document_status_history IN SHARE ROW EXCLUSIVE MODE;
        """;

    private const string UpSql = """
        CREATE OR REPLACE FUNCTION __advance_schema__.stores_p2_normalize_serial(value text)
        RETURNS text LANGUAGE sql IMMUTABLE STRICT PARALLEL SAFE
        SET search_path = pg_catalog
        AS $$ SELECT upper(btrim(regexp_replace(value, '[[:space:]]+', ' ', 'g'))) $$;

        CREATE OR REPLACE FUNCTION __advance_schema__.stores_p2_inventory_serial_guard()
        RETURNS trigger LANGUAGE plpgsql
        SET search_path = pg_catalog, __advance_schema__
        AS $$
        BEGIN
          IF TG_OP<>'INSERT' THEN RAISE EXCEPTION 'Inventory serial identity is immutable.'; END IF;
          IF NEW."NormalizedStoredSerialNumber"<>__advance_schema__.stores_p2_normalize_serial(NEW."StoredSerialNumber") THEN
            RAISE EXCEPTION 'Normalized stored serial does not match the canonical normalization.';
          END IF;
          RETURN NEW;
        END $$;
        CREATE TRIGGER "TR_inventory_serial_guard"
          BEFORE INSERT OR UPDATE OR DELETE ON __advance_schema__.inventory_serials
          FOR EACH ROW EXECUTE FUNCTION __advance_schema__.stores_p2_inventory_serial_guard();

        CREATE OR REPLACE FUNCTION __advance_schema__.stores_p2_goods_receipt_guard()
        RETURNS trigger LANGUAGE plpgsql
        SET search_path = pg_catalog, __advance_schema__
        AS $$
        DECLARE gate_row __advance_schema__.gate_entries%ROWTYPE;
                target __advance_schema__.goods_receipts%ROWTYPE;
                qc_config __advance_schema__.business_rule_configuration_versions%ROWTYPE;
                calculated_hash text;
        BEGIN
          IF TG_OP='DELETE' AND OLD."Status"='FINALIZED' THEN RAISE EXCEPTION 'Finalised GRNs are immutable.'; END IF;
          IF TG_OP='DELETE' THEN RETURN OLD; END IF;
          IF TG_OP='UPDATE' AND OLD."Status"='FINALIZED' THEN RAISE EXCEPTION 'Finalised GRNs are immutable.'; END IF;
          IF TG_OP='UPDATE' AND
             (NEW."CompanyId",NEW."GateEntryId",NEW."PurchaseOrderId",NEW."VendorId",NEW."DocumentKind",NEW."ReversesGoodsReceiptId")
             IS DISTINCT FROM
             (OLD."CompanyId",OLD."GateEntryId",OLD."PurchaseOrderId",OLD."VendorId",OLD."DocumentKind",OLD."ReversesGoodsReceiptId") THEN
            RAISE EXCEPTION 'GRN source identity is immutable after creation.';
          END IF;
          SELECT * INTO gate_row FROM __advance_schema__.gate_entries
            WHERE "CompanyId"=NEW."CompanyId" AND "Id"=NEW."GateEntryId" FOR SHARE;
          IF NOT FOUND OR gate_row."Status"<>'FINALIZED'
             OR gate_row."PurchaseOrderId"<>NEW."PurchaseOrderId" OR gate_row."VendorId"<>NEW."VendorId" THEN
            RAISE EXCEPTION 'GRN requires a finalised Gate Entry with matching company, PO and vendor.';
          END IF;
          IF NEW."VendorDcNumberSnapshot"<>gate_row."VendorDcNumber"
             OR NEW."ModeOfTransportSnapshot"<>gate_row."ModeOfTransport" THEN
            RAISE EXCEPTION 'GRN Gate Entry transport/DC snapshots do not match.';
          END IF;
          SELECT * INTO qc_config FROM __advance_schema__.business_rule_configuration_versions
            WHERE "CompanyId"=NEW."CompanyId" AND "Id"=NEW."QcCompletionDaysConfigVersionId";
          IF NOT FOUND OR qc_config."RuleKey"<>'QC_COMPLETION_DAYS'
             OR (qc_config."NewValueJson"#>>'{}')::integer<>NEW."QcCompletionDaysSnapshot"
             OR qc_config."EffectiveFrom">NEW."CreatedAt" THEN
            RAISE EXCEPTION 'GRN QC deadline snapshot does not match its effective configuration version.';
          END IF;
          calculated_hash:=encode(digest(convert_to(NEW."ConfigurationSnapshotJson"::jsonb::text,'UTF8'),'sha256'),'hex');
          IF calculated_hash<>NEW."ConfigurationSnapshotHash" THEN
            RAISE EXCEPTION 'GRN configuration snapshot hash mismatch.';
          END IF;
          IF NEW."DocumentKind"='REVERSAL' THEN
            SELECT * INTO target FROM __advance_schema__.goods_receipts WHERE "Id"=NEW."ReversesGoodsReceiptId";
            IF NOT FOUND OR target."Status"<>'FINALIZED' OR target."DocumentKind"<>'NORMAL'
               OR (NEW."CompanyId",NEW."GateEntryId",NEW."PurchaseOrderId",NEW."VendorId",NEW."VendorBillNumber",
                   NEW."VendorBillDate",NEW."ConfigurationSnapshotHash",NEW."QcCompletionDaysConfigVersionId",
                   NEW."QcCompletionDaysSnapshot")
                  IS DISTINCT FROM
                  (target."CompanyId",target."GateEntryId",target."PurchaseOrderId",target."VendorId",target."VendorBillNumber",
                   target."VendorBillDate",target."ConfigurationSnapshotHash",target."QcCompletionDaysConfigVersionId",
                   target."QcCompletionDaysSnapshot") THEN
              RAISE EXCEPTION 'GRN reversal must copy the finalised normal target facts.';
            END IF;
          END IF;
          IF NEW."Status"='FINALIZED' THEN
            IF NEW."QcDueAt"<>NEW."FinalizedAt"+make_interval(days=>NEW."QcCompletionDaysSnapshot") THEN
              RAISE EXCEPTION 'GRN QC due time must use the snapshotted limit from finalisation.';
            END IF;
            IF EXISTS (
              (SELECT "Id" FROM __advance_schema__.gate_entry_lines WHERE "GateEntryId"=NEW."GateEntryId"
               EXCEPT SELECT "GateEntryLineId" FROM __advance_schema__.goods_receipt_lines WHERE "GoodsReceiptId"=NEW."Id")
              UNION ALL
              (SELECT "GateEntryLineId" FROM __advance_schema__.goods_receipt_lines WHERE "GoodsReceiptId"=NEW."Id"
               EXCEPT SELECT "Id" FROM __advance_schema__.gate_entry_lines WHERE "GateEntryId"=NEW."GateEntryId")
            ) THEN RAISE EXCEPTION 'Every Gate Entry line must map to exactly one GRN line.'; END IF;
            IF EXISTS (
              SELECT 1 FROM __advance_schema__.goods_receipt_lines l
              WHERE l."GoodsReceiptId"=NEW."Id" AND (
                l."ReceivedQuantity">l."RemainingPoQuantitySnapshot"
                OR (l."SerialCaptureModeSnapshot"='REQUIRED' AND (
                  l."DeliveredQuantitySnapshot"<>trunc(l."DeliveredQuantitySnapshot")
                  OR (SELECT count(*) FROM __advance_schema__.goods_receipt_line_serials s
                      WHERE s."GoodsReceiptLineId"=l."Id" AND s."ReceiptDisposition"='QC_INSPECTION')<>l."ReceivedQuantity"
                  OR (SELECT count(*) FROM __advance_schema__.goods_receipt_line_serials s
                      WHERE s."GoodsReceiptLineId"=l."Id" AND s."ReceiptDisposition"='EXCESS_PENDING_RETURN')<>l."ExcessRejectedQuantity"
                ))
                OR (l."SerialCaptureModeSnapshot"='OPTIONAL'
                    AND EXISTS (SELECT 1 FROM __advance_schema__.goods_receipt_line_serials s WHERE s."GoodsReceiptLineId"=l."Id")
                    AND ((SELECT count(*) FROM __advance_schema__.goods_receipt_line_serials s
                          WHERE s."GoodsReceiptLineId"=l."Id" AND s."ReceiptDisposition"='QC_INSPECTION')<>l."ReceivedQuantity"
                      OR (SELECT count(*) FROM __advance_schema__.goods_receipt_line_serials s
                          WHERE s."GoodsReceiptLineId"=l."Id" AND s."ReceiptDisposition"='EXCESS_PENDING_RETURN')<>l."ExcessRejectedQuantity"))
              )
            ) THEN RAISE EXCEPTION 'GRN serial counts, dispositions or PO quantities are invalid.'; END IF;
            IF to_regclass('__advance_schema__.stock_posting_batches') IS NULL THEN
              RAISE EXCEPTION 'GRN finalisation is disabled until Stores Part 3 installs atomic QC-hold posting.';
            END IF;
          END IF;
          RETURN NEW;
        END $$;
        CREATE TRIGGER "TR_goods_receipt_guard"
          BEFORE INSERT OR UPDATE OR DELETE ON __advance_schema__.goods_receipts
          FOR EACH ROW EXECUTE FUNCTION __advance_schema__.stores_p2_goods_receipt_guard();

        CREATE OR REPLACE FUNCTION __advance_schema__.stores_p2_goods_receipt_line_guard()
        RETURNS trigger LANGUAGE plpgsql
        SET search_path = pg_catalog, __advance_schema__
        AS $$
        DECLARE parent __advance_schema__.goods_receipts%ROWTYPE;
                gate_line __advance_schema__.gate_entry_lines%ROWTYPE;
                po_line __advance_schema__.purchase_order_lines%ROWTYPE;
                item_row __advance_schema__.items%ROWTYPE;
                threshold __advance_schema__.business_rule_configuration_versions%ROWTYPE;
                route __advance_schema__.store_category_routes%ROWTYPE;
                override_mode text;
                prior_received numeric(24,6);
        BEGIN
          SELECT * INTO parent FROM __advance_schema__.goods_receipts
            WHERE "Id"=coalesce(NEW."GoodsReceiptId",OLD."GoodsReceiptId") FOR SHARE;
          IF NOT FOUND OR parent."Status"<>'DRAFT' THEN RAISE EXCEPTION 'GRN lines are mutable only while the GRN is DRAFT.'; END IF;
          IF TG_OP='DELETE' THEN RETURN OLD; END IF;
          SELECT * INTO gate_line FROM __advance_schema__.gate_entry_lines
            WHERE "CompanyId"=NEW."CompanyId" AND "Id"=NEW."GateEntryLineId";
          SELECT * INTO po_line FROM __advance_schema__.purchase_order_lines
            WHERE "CompanyId"=NEW."CompanyId" AND "Id"=NEW."PurchaseOrderLineId" FOR SHARE;
          SELECT * INTO item_row FROM __advance_schema__.items WHERE "Id"=NEW."ItemId";
          IF NOT FOUND OR gate_line."GateEntryId"<>parent."GateEntryId"
             OR gate_line."PurchaseOrderLineId"<>NEW."PurchaseOrderLineId"
             OR gate_line."ItemId"<>NEW."ItemId" OR po_line."PurchaseOrderId"<>parent."PurchaseOrderId"
             OR po_line."ItemId"<>NEW."ItemId" THEN
            RAISE EXCEPTION 'GRN line must match its Gate Entry line, PO line and Item.';
          END IF;
          IF NEW."DeliveredQuantitySnapshot"<>gate_line."DeliveredQuantity"
             OR NEW."PoOrderedQuantitySnapshot"<>po_line."OrderedQuantity"
             OR NEW."ItemCodeSnapshot"<>item_row."ItemCode" OR NEW."ItemNameSnapshot"<>item_row."Name"
             OR NEW."ItemCategoryIdSnapshot" IS DISTINCT FROM item_row."CategoryId"
             OR NEW."HsnSacCodeSnapshot"<>coalesce(item_row."HsnSacCode",'')
             OR NEW."GstPercentageSnapshot"<>item_row."GstPercentage"
             OR NEW."ModelSnapshot" IS DISTINCT FROM item_row."Model"
             OR NEW."ManufacturerPartNumberSnapshot" IS DISTINCT FROM item_row."PartNumber"
             OR NEW."ManufacturerMakeSnapshot" IS DISTINCT FROM item_row."ManufacturerMake"
             OR NEW."UomSnapshot"<>item_row."Uom" THEN
            RAISE EXCEPTION 'GRN line Item or commercial snapshot does not match the source records.';
          END IF;
          SELECT coalesce(sum(l."ReceivedQuantity"),0) INTO prior_received
          FROM __advance_schema__.goods_receipt_lines l
          JOIN __advance_schema__.goods_receipts h ON h."Id"=l."GoodsReceiptId"
          WHERE l."PurchaseOrderLineId"=NEW."PurchaseOrderLineId" AND h."Status"='FINALIZED'
            AND h."DocumentKind"='NORMAL'
            AND NOT EXISTS (SELECT 1 FROM __advance_schema__.goods_receipts r
                            WHERE r."ReversesGoodsReceiptId"=h."Id" AND r."Status"='FINALIZED');
          IF NEW."PriorEffectiveReceivedQuantitySnapshot"<>prior_received
             OR NEW."RemainingPoQuantitySnapshot"<>po_line."OrderedQuantity"-prior_received THEN
            RAISE EXCEPTION 'GRN line received-to-date snapshots are not authoritative.';
          END IF;
          IF NEW."BillWarrantyLimitDate"<>parent."VendorBillDate"+INTERVAL '13 months'
             OR NEW."InitialWarrantyExpiryDate"<>parent."VendorBillDate"+INTERVAL '13 months' THEN
            RAISE EXCEPTION 'GRN line warranty must initially be bill date plus 13 months.';
          END IF;
          SELECT * INTO threshold FROM __advance_schema__.business_rule_configuration_versions
            WHERE "CompanyId"=NEW."CompanyId" AND "Id"=NEW."SerialThresholdConfigVersionId";
          IF NOT FOUND OR threshold."RuleKey"<>'SERIAL_CAPTURE_THRESHOLD'
             OR (threshold."NewValueJson"#>>'{}')::numeric<>NEW."SerialThresholdValueSnapshot"
             OR threshold."EffectiveFrom">parent."CreatedAt" THEN
            RAISE EXCEPTION 'GRN line serial threshold snapshot is invalid.';
          END IF;
          IF NEW."SerialOverrideSettingId" IS NOT NULL THEN
            SELECT "SerialCaptureMode" INTO override_mode FROM __advance_schema__.item_company_inventory_settings
              WHERE "CompanyId"=NEW."CompanyId" AND "Id"=NEW."SerialOverrideSettingId"
                AND "ItemId"=NEW."ItemId" AND "IsActive";
            IF override_mode IS NULL OR override_mode='INHERIT' THEN RAISE EXCEPTION 'Serial override must resolve REQUIRED or OPTIONAL.'; END IF;
          END IF;
          IF NEW."SerialCaptureModeSnapshot" <>
             coalesce(override_mode,CASE WHEN NEW."UnitRateSnapshot">NEW."SerialThresholdValueSnapshot" THEN 'REQUIRED' ELSE 'OPTIONAL' END) THEN
            RAISE EXCEPTION 'Serial capture mode does not match the Item override or unit-rate threshold.';
          END IF;
          SELECT * INTO route FROM __advance_schema__.store_category_routes
            WHERE "CompanyId"=NEW."CompanyId" AND "Id"=NEW."QcRouteIdSnapshot";
          IF NOT FOUND OR route."ItemCategoryId"<>NEW."ItemCategoryIdSnapshot"
             OR route."QcHoldConditionLocationId"<>NEW."QcHoldConditionLocationIdSnapshot"
             OR NOT route."IsActive" OR route."EffectiveFrom">parent."ReceivedAt"::date
             OR (route."EffectiveTo" IS NOT NULL AND route."EffectiveTo"<parent."ReceivedAt"::date) THEN
            RAISE EXCEPTION 'GRN line QC route snapshot is not effective for the Item category.';
          END IF;
          RETURN NEW;
        END $$;
        CREATE TRIGGER "TR_goods_receipt_line_guard"
          BEFORE INSERT OR UPDATE OR DELETE ON __advance_schema__.goods_receipt_lines
          FOR EACH ROW EXECUTE FUNCTION __advance_schema__.stores_p2_goods_receipt_line_guard();

        CREATE OR REPLACE FUNCTION __advance_schema__.stores_p2_line_serial_guard()
        RETURNS trigger LANGUAGE plpgsql
        SET search_path = pg_catalog, __advance_schema__
        AS $$
        DECLARE line_row __advance_schema__.goods_receipt_lines%ROWTYPE;
                header_status text;
                serial_row __advance_schema__.inventory_serials%ROWTYPE;
        BEGIN
          SELECT * INTO line_row FROM __advance_schema__.goods_receipt_lines
            WHERE "Id"=coalesce(NEW."GoodsReceiptLineId",OLD."GoodsReceiptLineId");
          SELECT "Status" INTO header_status FROM __advance_schema__.goods_receipts WHERE "Id"=line_row."GoodsReceiptId" FOR SHARE;
          IF header_status IS DISTINCT FROM 'DRAFT' THEN RAISE EXCEPTION 'GRN serial captures are mutable only while the GRN is DRAFT.'; END IF;
          IF TG_OP='DELETE' THEN RETURN OLD; END IF;
          SELECT * INTO serial_row FROM __advance_schema__.inventory_serials
            WHERE "CompanyId"=NEW."CompanyId" AND "Id"=NEW."InventorySerialId";
          IF NOT FOUND OR line_row."CompanyId"<>NEW."CompanyId" OR line_row."ItemId"<>NEW."ItemId"
             OR serial_row."ItemId"<>NEW."ItemId"
             OR serial_row."StoredSerialNumber"<>NEW."StoredSerialNumberSnapshot" THEN
            RAISE EXCEPTION 'GRN serial company, Item and durable identity must match.';
          END IF;
          IF NEW."EnteredSerialNumber"<>NEW."StoredSerialNumberSnapshot"
             AND NOT (NEW."DisambiguationApplied" AND NEW."DuplicateWarningAcknowledged") THEN
            RAISE EXCEPTION 'A changed stored serial requires acknowledged disambiguation.';
          END IF;
          RETURN NEW;
        END $$;
        CREATE TRIGGER "TR_goods_receipt_line_serial_guard"
          BEFORE INSERT OR UPDATE OR DELETE ON __advance_schema__.goods_receipt_line_serials
          FOR EACH ROW EXECUTE FUNCTION __advance_schema__.stores_p2_line_serial_guard();

        CREATE OR REPLACE FUNCTION __advance_schema__.stores_p2_effective_grn_guard()
        RETURNS trigger LANGUAGE plpgsql
        SET search_path = pg_catalog, __advance_schema__
        AS $$
        BEGIN
          IF NEW."Status"='FINALIZED' AND NEW."DocumentKind"='NORMAL' AND (
            EXISTS (
              SELECT 1 FROM __advance_schema__.goods_receipts x
              WHERE x."Id"<>NEW."Id" AND x."Status"='FINALIZED' AND x."DocumentKind"='NORMAL'
                AND x."GateEntryId"=NEW."GateEntryId"
                AND NOT EXISTS (SELECT 1 FROM __advance_schema__.goods_receipts r
                                WHERE r."ReversesGoodsReceiptId"=x."Id" AND r."Status"='FINALIZED')
            ) OR EXISTS (
              SELECT 1 FROM __advance_schema__.goods_receipts x
              WHERE x."Id"<>NEW."Id" AND x."Status"='FINALIZED' AND x."DocumentKind"='NORMAL'
                AND x."CompanyId"=NEW."CompanyId" AND x."VendorBillNumber"=NEW."VendorBillNumber"
                AND NOT EXISTS (SELECT 1 FROM __advance_schema__.goods_receipts r
                                WHERE r."ReversesGoodsReceiptId"=x."Id" AND r."Status"='FINALIZED')
            )) THEN
            RAISE EXCEPTION 'Only one effective finalised GRN is permitted per Gate Entry and company vendor bill.';
          END IF;
          RETURN NULL;
        END $$;
        CREATE CONSTRAINT TRIGGER "TR_goods_receipt_effective_cardinality"
          AFTER INSERT OR UPDATE ON __advance_schema__.goods_receipts
          DEFERRABLE INITIALLY DEFERRED FOR EACH ROW
          EXECUTE FUNCTION __advance_schema__.stores_p2_effective_grn_guard();

        CREATE OR REPLACE FUNCTION __advance_schema__.stores_p1_status_history_guard()
        RETURNS trigger LANGUAGE plpgsql
        SET search_path = pg_catalog, __advance_schema__
        AS $$
        DECLARE source_company uuid;
        BEGIN
          IF TG_OP<>'INSERT' THEN RAISE EXCEPTION 'Stores document status history is append-only.'; END IF;
          IF num_nonnulls(NEW."GateEntryId",NEW."GoodsReceiptId")<>1 THEN
            RAISE EXCEPTION 'Status history requires exactly one typed source.';
          END IF;
          IF NEW."GateEntryId" IS NOT NULL THEN
            SELECT "CompanyId" INTO source_company FROM __advance_schema__.gate_entries WHERE "Id"=NEW."GateEntryId";
          ELSE
            SELECT "CompanyId" INTO source_company FROM __advance_schema__.goods_receipts WHERE "Id"=NEW."GoodsReceiptId";
          END IF;
          IF source_company IS NULL OR source_company<>NEW."CompanyId" THEN
            RAISE EXCEPTION 'Status history company must match its typed source.';
          END IF;
          IF NOT __advance_schema__.stores_p1_actor_has_role(
              NEW."ActorEmployeeId",NEW."CompanyId",NEW."ActorRoleCode",NEW."OccurredAt"::date) THEN
            RAISE EXCEPTION 'Status actor lacks the recorded role in this company.';
          END IF;
          RETURN NEW;
        END $$;

        DO $witness$
        BEGIN
          IF (SELECT count(*) FROM __advance_schema__.goods_receipts)
             +(SELECT count(*) FROM __advance_schema__.goods_receipt_lines)
             +(SELECT count(*) FROM __advance_schema__.inventory_serials)
             +(SELECT count(*) FROM __advance_schema__.goods_receipt_line_serials)<>0 THEN
            RAISE EXCEPTION 'First Stores Part 2 tables must be empty immediately after apply.';
          END IF;
          IF EXISTS (SELECT 1 FROM __advance_schema__.stores_document_status_history WHERE "GoodsReceiptId" IS NOT NULL) THEN
            RAISE EXCEPTION 'First Stores Part 2 must not invent GRN status history.';
          END IF;
          IF (SELECT count(*) FROM __advance_schema__.business_rule_configuration_versions)<>18 THEN
            RAISE EXCEPTION 'First Stores Part 2 must not change Part 1 configuration rows.';
          END IF;
        END $witness$;
        """;

    private const string DownSql = """
        DO $guard$
        DECLARE future_table text;
        BEGIN
          IF current_setting('server_version_num')::integer < 170000 THEN
            RAISE EXCEPTION 'First Stores Part 2 down requires PostgreSQL 17 or later.';
          END IF;
          IF current_database() IN ('postgres','template0','template1') THEN
            RAISE EXCEPTION 'First Stores Part 2 down refuses a PostgreSQL administrative database.';
          END IF;
          IF to_regnamespace('__advance_schema__') IS NULL THEN
            RAISE EXCEPTION 'First Stores Part 2 down requires the advance schema.';
          END IF;
          FOREACH future_table IN ARRAY ARRAY[
            'qc_inspections','qc_inspection_revisions','job_orders','material_issue_requests',
            'delivery_challans','stock_posting_batches'
          ] LOOP
            IF to_regclass('__advance_schema__.' || future_table) IS NOT NULL THEN
              RAISE EXCEPTION 'Remove later Stores migration table % before Part 2.', future_table;
            END IF;
          END LOOP;
          IF (SELECT count(*) FROM __advance_schema__.goods_receipts)
             +(SELECT count(*) FROM __advance_schema__.goods_receipt_lines)
             +(SELECT count(*) FROM __advance_schema__.inventory_serials)
             +(SELECT count(*) FROM __advance_schema__.goods_receipt_line_serials)<>0
             OR EXISTS (SELECT 1 FROM __advance_schema__.stores_document_status_history WHERE "GoodsReceiptId" IS NOT NULL) THEN
            RAISE EXCEPTION 'First Stores Part 2 rollback refuses persisted GRN, serial or GRN-history data.';
          END IF;
        END $guard$;

        DROP TRIGGER IF EXISTS "TR_goods_receipt_effective_cardinality" ON __advance_schema__.goods_receipts;
        DROP TRIGGER IF EXISTS "TR_goods_receipt_line_serial_guard" ON __advance_schema__.goods_receipt_line_serials;
        DROP TRIGGER IF EXISTS "TR_goods_receipt_line_guard" ON __advance_schema__.goods_receipt_lines;
        DROP TRIGGER IF EXISTS "TR_goods_receipt_guard" ON __advance_schema__.goods_receipts;
        DROP TRIGGER IF EXISTS "TR_inventory_serial_guard" ON __advance_schema__.inventory_serials;
        DROP FUNCTION IF EXISTS __advance_schema__.stores_p2_effective_grn_guard();
        DROP FUNCTION IF EXISTS __advance_schema__.stores_p2_line_serial_guard();
        DROP FUNCTION IF EXISTS __advance_schema__.stores_p2_goods_receipt_line_guard();
        DROP FUNCTION IF EXISTS __advance_schema__.stores_p2_goods_receipt_guard();
        DROP FUNCTION IF EXISTS __advance_schema__.stores_p2_inventory_serial_guard();
        DROP FUNCTION IF EXISTS __advance_schema__.stores_p2_normalize_serial(text);

        CREATE OR REPLACE FUNCTION __advance_schema__.stores_p1_status_history_guard()
        RETURNS trigger LANGUAGE plpgsql
        SET search_path = pg_catalog, __advance_schema__
        AS $$
        DECLARE source_company uuid;
        BEGIN
          IF TG_OP<>'INSERT' THEN RAISE EXCEPTION 'Stores document status history is append-only.'; END IF;
          SELECT "CompanyId" INTO source_company FROM __advance_schema__.gate_entries WHERE "Id"=NEW."GateEntryId";
          IF source_company IS NULL OR source_company<>NEW."CompanyId" THEN
            RAISE EXCEPTION 'Status history company must match its Gate Entry.';
          END IF;
          IF NOT __advance_schema__.stores_p1_actor_has_role(
              NEW."ActorEmployeeId",NEW."CompanyId",NEW."ActorRoleCode",NEW."OccurredAt"::date) THEN
            RAISE EXCEPTION 'Status actor lacks the recorded role in this company.';
          END IF;
          RETURN NEW;
        END $$;
        """;
}
