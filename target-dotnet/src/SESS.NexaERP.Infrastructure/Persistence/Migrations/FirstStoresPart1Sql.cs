namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class FirstStoresPart1Sql
{
    internal static string PreUp => AdvanceSchemaSql.Expand(PreUpSql);
    internal static string Up => AdvanceSchemaSql.Expand(UpSql);
    internal static string Down => AdvanceSchemaSql.Expand(DownSql);

    private const string PreUpSql = """
        DO $guard$
        DECLARE required_table text;
        BEGIN
          IF current_setting('server_version_num')::integer < 170000 THEN
            RAISE EXCEPTION 'First Stores Part 1 requires PostgreSQL 17 or later.';
          END IF;
          IF current_database() IN ('postgres','template0','template1') THEN
            RAISE EXCEPTION 'First Stores Part 1 refuses a PostgreSQL administrative database.';
          END IF;
          IF to_regnamespace('__advance_schema__') IS NULL THEN
            RAISE EXCEPTION 'First Stores Part 1 requires the advance schema.';
          END IF;
          FOREACH required_table IN ARRAY ARRAY[
            'companies','employees','roles','employee_role_assignments','items','item_categories',
            'warehouse_condition_locations','purchase_orders','purchase_order_lines','vendors'
          ] LOOP
            IF to_regclass('__advance_schema__.' || required_table) IS NULL THEN
              RAISE EXCEPTION 'First Stores Part 1 requires table %.', required_table;
            END IF;
          END LOOP;
          IF (SELECT count(*) FROM __advance_schema__.companies
              WHERE "Id" IN ('70000000-0000-0000-0000-000000000001','70000000-0000-0000-0000-000000000002')
                AND "IsActive") <> 2 THEN
            RAISE EXCEPTION 'First Stores Part 1 requires both settled companies to be active.';
          END IF;
          IF EXISTS (
            SELECT 1 FROM unnest(ARRAY[
              'business_rule_configuration_versions','item_company_inventory_settings','store_category_routes',
              'stores_document_status_history','gate_entries','gate_entry_lines','notification_events',
              'notification_recipients','notification_delivery_attempts'
            ]) AS x(name)
            WHERE to_regclass('__advance_schema__.' || x.name) IS NOT NULL
          ) THEN
            RAISE EXCEPTION 'First Stores Part 1 refuses a partial or replayed schema.';
          END IF;
        END $guard$;

        LOCK TABLE __advance_schema__.companies IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.employees IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.roles IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.employee_role_assignments IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.items IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.warehouse_condition_locations IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.purchase_orders IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.purchase_order_lines IN SHARE ROW EXCLUSIVE MODE;
        """;

    private const string UpSql = """
        CREATE EXTENSION IF NOT EXISTS btree_gist;
        ALTER TABLE __advance_schema__.store_category_routes
          ADD CONSTRAINT "EX_store_category_routes_one_effective"
          EXCLUDE USING gist (
            "CompanyId" WITH =, "ItemCategoryId" WITH =,
            daterange("EffectiveFrom", coalesce("EffectiveTo", 'infinity'::date), '[]') WITH &&
          ) WHERE ("IsActive");

        CREATE OR REPLACE FUNCTION __advance_schema__.stores_p1_actor_has_role(
          employee_id uuid, company_id uuid, role_code text, at_date date)
        RETURNS boolean LANGUAGE sql STABLE
        SET search_path = pg_catalog, __advance_schema__
        AS $$
          SELECT EXISTS (
            SELECT 1 FROM __advance_schema__.employee_role_assignments a
            JOIN __advance_schema__.roles r ON r."Id"=a."RoleId"
            WHERE a."EmployeeId"=employee_id AND a."CompanyId"=company_id
              AND r."Code"=role_code AND r."IsActive"
              AND a."EffectiveFrom"<=at_date
              AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=at_date)
              AND upper(a."ApprovalStatus") IN ('APPROVED','SEEDAPPROVED')
          )
        $$;

        CREATE OR REPLACE FUNCTION __advance_schema__.stores_p1_configuration_guard()
        RETURNS trigger LANGUAGE plpgsql
        SET search_path = pg_catalog, __advance_schema__
        AS $$
        DECLARE prior __advance_schema__.business_rule_configuration_versions%ROWTYPE;
        BEGIN
          IF TG_OP <> 'INSERT' THEN RAISE EXCEPTION 'Business-rule configuration is append-only.'; END IF;
          IF NOT __advance_schema__.stores_p1_actor_has_role(
              NEW."ChangedByEmployeeId",NEW."CompanyId",NEW."ChangedByRoleCode",NEW."ChangedAt"::date) THEN
            RAISE EXCEPTION 'Configuration actor lacks the recorded authorised role in this company.';
          END IF;
          IF (NEW."ValueType" IN ('INTEGER','DECIMAL') AND jsonb_typeof(NEW."NewValueJson")<>'number')
             OR (NEW."ValueType"='BOOLEAN' AND jsonb_typeof(NEW."NewValueJson")<>'boolean')
             OR (NEW."ValueType"='TEXT' AND jsonb_typeof(NEW."NewValueJson")<>'string') THEN
            RAISE EXCEPTION 'Configuration JSON type does not match ValueType.';
          END IF;
          IF NEW."VersionNumber"=1 THEN
            IF EXISTS (SELECT 1 FROM __advance_schema__.business_rule_configuration_versions
                       WHERE "CompanyId"=NEW."CompanyId" AND "RuleKey"=NEW."RuleKey") THEN
              RAISE EXCEPTION 'A first configuration version already exists.';
            END IF;
          ELSE
            SELECT * INTO prior FROM __advance_schema__.business_rule_configuration_versions
              WHERE "Id"=NEW."PreviousVersionId" FOR SHARE;
            IF NOT FOUND OR prior."CompanyId"<>NEW."CompanyId" OR prior."RuleKey"<>NEW."RuleKey"
               OR prior."VersionNumber"<>NEW."VersionNumber"-1 OR prior."NewValueJson"<>NEW."OldValueJson"
               OR prior."EffectiveFrom">=NEW."EffectiveFrom" THEN
              RAISE EXCEPTION 'Configuration version does not continue the effective-dated chain.';
            END IF;
          END IF;
          RETURN NEW;
        END $$;
        CREATE TRIGGER "TR_business_rule_configuration_guard"
          BEFORE INSERT OR UPDATE OR DELETE ON __advance_schema__.business_rule_configuration_versions
          FOR EACH ROW EXECUTE FUNCTION __advance_schema__.stores_p1_configuration_guard();

        CREATE OR REPLACE FUNCTION __advance_schema__.stores_p1_item_setting_guard()
        RETURNS trigger LANGUAGE plpgsql
        SET search_path = pg_catalog, __advance_schema__
        AS $$
        DECLARE category_code text;
        BEGIN
          IF TG_OP='DELETE' THEN RAISE EXCEPTION 'Item company inventory settings cannot be deleted.'; END IF;
          IF TG_OP='UPDATE' AND
             (NEW."CompanyId",NEW."ItemId",NEW."ErpBarcode",NEW."BarcodeCategoryCode",NEW."BarcodeSequenceNumber",NEW."BarcodeSymbology")
             IS DISTINCT FROM
             (OLD."CompanyId",OLD."ItemId",OLD."ErpBarcode",OLD."BarcodeCategoryCode",OLD."BarcodeSequenceNumber",OLD."BarcodeSymbology") THEN
            RAISE EXCEPTION 'Item barcode identity and provenance are immutable.';
          END IF;
          SELECT upper(left(c."Code",3)) INTO category_code
          FROM __advance_schema__.items i JOIN __advance_schema__.item_categories c ON c."Id"=i."CategoryId"
          WHERE i."Id"=NEW."ItemId";
          IF category_code IS NULL OR category_code<>NEW."BarcodeCategoryCode" THEN
            RAISE EXCEPTION 'Barcode category must match the Item category.';
          END IF;
          IF NEW."ErpBarcode" <> 'SESS-' || NEW."BarcodeCategoryCode" || '-' || NEW."BarcodeSequenceNumber"::text THEN
            RAISE EXCEPTION 'ERP barcode does not match SESS-<CAT>-<serial>.';
          END IF;
          RETURN NEW;
        END $$;
        CREATE TRIGGER "TR_item_company_inventory_setting_guard"
          BEFORE INSERT OR UPDATE OR DELETE ON __advance_schema__.item_company_inventory_settings
          FOR EACH ROW EXECUTE FUNCTION __advance_schema__.stores_p1_item_setting_guard();

        CREATE OR REPLACE FUNCTION __advance_schema__.stores_p1_route_guard()
        RETURNS trigger LANGUAGE plpgsql
        SET search_path = pg_catalog, __advance_schema__
        AS $$
        DECLARE qc text; pending text; accepted text;
        BEGIN
          SELECT "ConditionCode" INTO qc FROM __advance_schema__.warehouse_condition_locations
            WHERE "CompanyId"=NEW."CompanyId" AND "Id"=NEW."QcHoldConditionLocationId";
          SELECT "ConditionCode" INTO pending FROM __advance_schema__.warehouse_condition_locations
            WHERE "CompanyId"=NEW."CompanyId" AND "Id"=NEW."PendingReturnConditionLocationId";
          SELECT "ConditionCode" INTO accepted FROM __advance_schema__.warehouse_condition_locations
            WHERE "CompanyId"=NEW."CompanyId" AND "Id"=NEW."DefaultAcceptedConditionLocationId";
          IF qc IS DISTINCT FROM 'QC_HOLD' OR pending IS DISTINCT FROM 'PENDING_RETURNABLE_DC'
             OR accepted IS DISTINCT FROM 'AVAILABLE' THEN
            RAISE EXCEPTION 'Category route conditions must be QC_HOLD, PENDING_RETURNABLE_DC and AVAILABLE.';
          END IF;
          RETURN NEW;
        END $$;
        CREATE TRIGGER "TR_store_category_route_guard"
          BEFORE INSERT OR UPDATE ON __advance_schema__.store_category_routes
          FOR EACH ROW EXECUTE FUNCTION __advance_schema__.stores_p1_route_guard();

        CREATE OR REPLACE FUNCTION __advance_schema__.stores_p1_gate_header_guard()
        RETURNS trigger LANGUAGE plpgsql
        SET search_path = pg_catalog, __advance_schema__
        AS $$
        DECLARE po_vendor uuid; target __advance_schema__.gate_entries%ROWTYPE;
        BEGIN
          IF TG_OP='DELETE' AND OLD."Status"='FINALIZED' THEN RAISE EXCEPTION 'Finalised Gate Entries are immutable.'; END IF;
          IF TG_OP='DELETE' THEN RETURN OLD; END IF;
          IF TG_OP='UPDATE' AND OLD."Status"='FINALIZED' THEN RAISE EXCEPTION 'Finalised Gate Entries are immutable.'; END IF;
          IF TG_OP='UPDATE' AND
             (NEW."CompanyId",NEW."PurchaseOrderId",NEW."VendorId",NEW."DocumentKind",NEW."ReversesGateEntryId")
             IS DISTINCT FROM
             (OLD."CompanyId",OLD."PurchaseOrderId",OLD."VendorId",OLD."DocumentKind",OLD."ReversesGateEntryId") THEN
            RAISE EXCEPTION 'Gate Entry identity is immutable after creation.';
          END IF;
          SELECT "VendorId" INTO po_vendor FROM __advance_schema__.purchase_orders
            WHERE "CompanyId"=NEW."CompanyId" AND "Id"=NEW."PurchaseOrderId";
          IF po_vendor IS NULL OR po_vendor<>NEW."VendorId" THEN
            RAISE EXCEPTION 'Gate Entry company/vendor must match its PO.';
          END IF;
          IF NEW."DocumentKind"='REVERSAL' THEN
            SELECT * INTO target FROM __advance_schema__.gate_entries WHERE "Id"=NEW."ReversesGateEntryId";
            IF NOT FOUND OR target."Status"<>'FINALIZED' OR target."DocumentKind"<>'NORMAL'
               OR target."CompanyId"<>NEW."CompanyId" OR target."PurchaseOrderId"<>NEW."PurchaseOrderId"
               OR target."VendorId"<>NEW."VendorId" THEN RAISE EXCEPTION 'Gate Entry reversal target is invalid.'; END IF;
          END IF;
          IF NEW."Status"='FINALIZED' THEN
            IF NOT EXISTS (SELECT 1 FROM __advance_schema__.gate_entry_lines WHERE "GateEntryId"=NEW."Id") THEN
              RAISE EXCEPTION 'A Gate Entry cannot finalise without lines.';
            END IF;
            IF NEW."DocumentKind"='REVERSAL' AND EXISTS (
              (SELECT "PurchaseOrderLineId","ItemId","DeliveredQuantity" FROM __advance_schema__.gate_entry_lines WHERE "GateEntryId"=NEW."Id"
               EXCEPT SELECT "PurchaseOrderLineId","ItemId","DeliveredQuantity" FROM __advance_schema__.gate_entry_lines WHERE "GateEntryId"=NEW."ReversesGateEntryId")
              UNION ALL
              (SELECT "PurchaseOrderLineId","ItemId","DeliveredQuantity" FROM __advance_schema__.gate_entry_lines WHERE "GateEntryId"=NEW."ReversesGateEntryId"
               EXCEPT SELECT "PurchaseOrderLineId","ItemId","DeliveredQuantity" FROM __advance_schema__.gate_entry_lines WHERE "GateEntryId"=NEW."Id")
            ) THEN RAISE EXCEPTION 'Gate Entry reversal lines must exactly mirror the target.'; END IF;
          END IF;
          RETURN NEW;
        END $$;
        CREATE TRIGGER "TR_gate_entry_header_guard"
          BEFORE INSERT OR UPDATE OR DELETE ON __advance_schema__.gate_entries
          FOR EACH ROW EXECUTE FUNCTION __advance_schema__.stores_p1_gate_header_guard();

        CREATE OR REPLACE FUNCTION __advance_schema__.stores_p1_gate_line_guard()
        RETURNS trigger LANGUAGE plpgsql
        SET search_path = pg_catalog, __advance_schema__
        AS $$
        DECLARE parent_status text; po_item uuid;
        BEGIN
          SELECT "Status" INTO parent_status FROM __advance_schema__.gate_entries
            WHERE "Id"=coalesce(NEW."GateEntryId",OLD."GateEntryId") FOR SHARE;
          IF parent_status IS DISTINCT FROM 'DRAFT' THEN
            RAISE EXCEPTION 'Gate Entry lines are mutable only while the parent is DRAFT.';
          END IF;
          IF TG_OP='DELETE' THEN RETURN OLD; END IF;
          SELECT "ItemId" INTO po_item FROM __advance_schema__.purchase_order_lines
            WHERE "CompanyId"=NEW."CompanyId" AND "PurchaseOrderId"=NEW."PurchaseOrderId"
              AND "Id"=NEW."PurchaseOrderLineId";
          IF po_item IS NULL OR po_item<>NEW."ItemId" THEN RAISE EXCEPTION 'Gate Entry line Item must match its PO line.'; END IF;
          RETURN NEW;
        END $$;
        CREATE TRIGGER "TR_gate_entry_line_guard"
          BEFORE INSERT OR UPDATE OR DELETE ON __advance_schema__.gate_entry_lines
          FOR EACH ROW EXECUTE FUNCTION __advance_schema__.stores_p1_gate_line_guard();

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
        CREATE TRIGGER "TR_stores_document_status_history_guard"
          BEFORE INSERT OR UPDATE OR DELETE ON __advance_schema__.stores_document_status_history
          FOR EACH ROW EXECUTE FUNCTION __advance_schema__.stores_p1_status_history_guard();

        CREATE OR REPLACE FUNCTION __advance_schema__.stores_p1_notification_event_guard()
        RETURNS trigger LANGUAGE plpgsql
        SET search_path = pg_catalog, __advance_schema__
        AS $$
        BEGIN
          IF TG_OP='DELETE' THEN RAISE EXCEPTION 'Notification events cannot be deleted.'; END IF;
          IF EXISTS (SELECT 1 FROM unnest(NEW."RecipientRoleCodes") r(code) WHERE r.code<>upper(trim(r.code)))
             OR cardinality(NEW."RecipientRoleCodes")<>(SELECT count(DISTINCT x) FROM unnest(NEW."RecipientRoleCodes") x)
             OR EXISTS (SELECT 1 FROM unnest(NEW."RecipientRoleCodes") x
                        WHERE NOT EXISTS (SELECT 1 FROM __advance_schema__.roles r WHERE r."Code"=x AND r."IsActive")) THEN
            RAISE EXCEPTION 'Notification recipient roles must be distinct active canonical role codes.';
          END IF;
          IF TG_OP='UPDATE' THEN
            IF (NEW."CompanyId",NEW."EventType",NEW."SourceEntityType",NEW."SourceEntityId",NEW."RecipientRoleCodes",
                NEW."TitleSnapshot",NEW."BodySnapshot",NEW."DeepLinkSnapshot",NEW."PayloadJson",NEW."NotBeforeAt",
                NEW."IdempotencyKey",NEW."CreatedAt",NEW."CreatedBy")
               IS DISTINCT FROM
               (OLD."CompanyId",OLD."EventType",OLD."SourceEntityType",OLD."SourceEntityId",OLD."RecipientRoleCodes",
                OLD."TitleSnapshot",OLD."BodySnapshot",OLD."DeepLinkSnapshot",OLD."PayloadJson",OLD."NotBeforeAt",
                OLD."IdempotencyKey",OLD."CreatedAt",OLD."CreatedBy") THEN
              RAISE EXCEPTION 'Notification event payload and targeting are immutable.';
            END IF;
            IF NOT (
              (OLD."Status" IN ('SCHEDULED','READY') AND NEW."Status" IN ('READY','ACTIVE','RECIPIENT_BLOCKED','CANCELLED'))
              OR (OLD."Status" IN ('ACTIVE','RECIPIENT_BLOCKED') AND NEW."Status" IN ('ACTIVE','RECIPIENT_BLOCKED','COMPLETED','CANCELLED'))
              OR OLD."Status"=NEW."Status"
            ) THEN RAISE EXCEPTION 'Illegal notification event transition.'; END IF;
          END IF;
          RETURN NEW;
        END $$;
        CREATE TRIGGER "TR_notification_event_guard"
          BEFORE INSERT OR UPDATE OR DELETE ON __advance_schema__.notification_events
          FOR EACH ROW EXECUTE FUNCTION __advance_schema__.stores_p1_notification_event_guard();

        CREATE OR REPLACE FUNCTION __advance_schema__.stores_p1_notification_recipient_guard()
        RETURNS trigger LANGUAGE plpgsql
        SET search_path = pg_catalog, __advance_schema__
        AS $$
        DECLARE event_row __advance_schema__.notification_events%ROWTYPE; role_code text;
        BEGIN
          IF TG_OP='DELETE' THEN RAISE EXCEPTION 'Notification recipients cannot be deleted.'; END IF;
          IF TG_OP='UPDATE' THEN
            IF (NEW."CompanyId",NEW."NotificationEventId",NEW."RecipientEmployeeId",NEW."ResolvedRoleCodes",
                NEW."ResolvedAt",NEW."InAppAvailableAt")
               IS DISTINCT FROM
               (OLD."CompanyId",OLD."NotificationEventId",OLD."RecipientEmployeeId",OLD."ResolvedRoleCodes",
                OLD."ResolvedAt",OLD."InAppAvailableAt") OR OLD."ReadAt" IS NOT NULL THEN
              RAISE EXCEPTION 'Notification recipient identity is immutable and read is one-way.';
            END IF;
            RETURN NEW;
          END IF;
          SELECT * INTO event_row FROM __advance_schema__.notification_events
            WHERE "CompanyId"=NEW."CompanyId" AND "Id"=NEW."NotificationEventId";
          IF NOT FOUND OR NOT (NEW."ResolvedRoleCodes" <@ event_row."RecipientRoleCodes") THEN
            RAISE EXCEPTION 'Resolved roles must be a subset of the event roles.';
          END IF;
          FOREACH role_code IN ARRAY NEW."ResolvedRoleCodes" LOOP
            IF NOT __advance_schema__.stores_p1_actor_has_role(
                NEW."RecipientEmployeeId",NEW."CompanyId",role_code,NEW."ResolvedAt"::date) THEN
              RAISE EXCEPTION 'Notification recipient lacks resolved role % in this company.', role_code;
            END IF;
          END LOOP;
          RETURN NEW;
        END $$;
        CREATE TRIGGER "TR_notification_recipient_guard"
          BEFORE INSERT OR UPDATE OR DELETE ON __advance_schema__.notification_recipients
          FOR EACH ROW EXECUTE FUNCTION __advance_schema__.stores_p1_notification_recipient_guard();

        CREATE OR REPLACE FUNCTION __advance_schema__.stores_p1_notification_attempt_guard()
        RETURNS trigger LANGUAGE plpgsql
        SET search_path = pg_catalog, __advance_schema__
        AS $$
        DECLARE recipient_company uuid; next_attempt integer;
        BEGIN
          IF TG_OP<>'INSERT' THEN RAISE EXCEPTION 'Notification delivery attempts are append-only.'; END IF;
          SELECT "CompanyId" INTO recipient_company FROM __advance_schema__.notification_recipients
            WHERE "Id"=NEW."NotificationRecipientId" FOR SHARE;
          IF recipient_company IS NULL OR recipient_company<>NEW."CompanyId" THEN
            RAISE EXCEPTION 'Delivery attempt company must match its recipient.';
          END IF;
          SELECT coalesce(max("AttemptNumber"),0)+1 INTO next_attempt
          FROM __advance_schema__.notification_delivery_attempts
          WHERE "NotificationRecipientId"=NEW."NotificationRecipientId" AND "Channel"=NEW."Channel";
          IF NEW."AttemptNumber"<>next_attempt THEN
            RAISE EXCEPTION 'Delivery attempt numbers must be sequential per recipient and channel.';
          END IF;
          RETURN NEW;
        END $$;
        CREATE TRIGGER "TR_notification_delivery_attempt_guard"
          BEFORE INSERT OR UPDATE OR DELETE ON __advance_schema__.notification_delivery_attempts
          FOR EACH ROW EXECUTE FUNCTION __advance_schema__.stores_p1_notification_attempt_guard();

        DO $witness$
        BEGIN
          IF (SELECT count(*) FROM __advance_schema__.business_rule_configuration_versions)<>18
             OR EXISTS (
               SELECT 1 FROM (VALUES
                 ('70000000-0000-0000-0000-000000000001'::uuid),
                 ('70000000-0000-0000-0000-000000000002'::uuid)
               ) c(id)
               WHERE (SELECT count(*) FROM __advance_schema__.business_rule_configuration_versions v
                      WHERE v."CompanyId"=c.id AND v."VersionNumber"=1
                        AND v."ChangedByEmployeeId"='3543a705-924a-6599-23be-fb9730a93f06'
                        AND v."ChangedByRoleCode"='TECHNICAL_DIRECTOR')<>9
             )
             OR EXISTS (
               SELECT 1 FROM (VALUES
                 ('70000000-0000-0000-0000-000000000001'::uuid),
                 ('70000000-0000-0000-0000-000000000002'::uuid)
               ) c(id)
               WHERE NOT __advance_schema__.stores_p1_actor_has_role(
                 '3543a705-924a-6599-23be-fb9730a93f06',c.id,'TECHNICAL_DIRECTOR',DATE '2026-08-27')
             )
             OR EXISTS (
               SELECT 1 FROM (VALUES
                 ('SERIAL_CAPTURE_THRESHOLD','5000'::jsonb),
                 ('QC_COMPLETION_DAYS','2'::jsonb),
                 ('EMERGENCY_PURCHASE_COUNT_PER_MONTH','10'::jsonb),
                 ('EMERGENCY_PURCHASE_VALUE_LIMIT','5000'::jsonb),
                 ('EXPENSE_FOOD_PER_PERSON_PER_DAY','300'::jsonb),
                 ('EXPENSE_LODGING_SINGLE_PER_DAY','800'::jsonb),
                 ('EXPENSE_LODGING_DOUBLE_PER_DAY','1200'::jsonb),
                 ('EXPENSE_DAILY_APPROVAL_CAP','5000'::jsonb),
                 ('EXPENSE_TRAVEL_DISTANCE_THRESHOLD_KM','100'::jsonb)
               ) expected(rule_key,rule_value)
               CROSS JOIN (VALUES
                 ('70000000-0000-0000-0000-000000000001'::uuid),
                 ('70000000-0000-0000-0000-000000000002'::uuid)
               ) c(id)
               WHERE NOT EXISTS (
                 SELECT 1 FROM __advance_schema__.business_rule_configuration_versions v
                 WHERE v."CompanyId"=c.id AND v."RuleKey"=expected.rule_key
                   AND v."NewValueJson"=expected.rule_value
               )
             ) THEN RAISE EXCEPTION 'First Stores Part 1 expected exactly 18 authorised initial configuration rows.'; END IF;
          IF (SELECT count(*) FROM __advance_schema__.item_company_inventory_settings)
             +(SELECT count(*) FROM __advance_schema__.store_category_routes)
             +(SELECT count(*) FROM __advance_schema__.stores_document_status_history)
             +(SELECT count(*) FROM __advance_schema__.gate_entries)
             +(SELECT count(*) FROM __advance_schema__.gate_entry_lines)
             +(SELECT count(*) FROM __advance_schema__.notification_events)
             +(SELECT count(*) FROM __advance_schema__.notification_recipients)
             +(SELECT count(*) FROM __advance_schema__.notification_delivery_attempts) <> 0 THEN
            RAISE EXCEPTION 'First Stores Part 1 non-configuration tables must be empty immediately after apply.';
          END IF;
        END $witness$;
        """;

    private const string DownSql = """
        DO $guard$
        DECLARE future_table text;
        BEGIN
          IF current_setting('server_version_num')::integer < 170000 THEN
            RAISE EXCEPTION 'First Stores Part 1 down requires PostgreSQL 17 or later.';
          END IF;
          IF current_database() IN ('postgres','template0','template1') THEN
            RAISE EXCEPTION 'First Stores Part 1 down refuses a PostgreSQL administrative database.';
          END IF;
          IF to_regnamespace('__advance_schema__') IS NULL THEN
            RAISE EXCEPTION 'First Stores Part 1 down requires the advance schema.';
          END IF;
          FOREACH future_table IN ARRAY ARRAY[
            'goods_receipts','goods_receipt_lines','inventory_serials','goods_receipt_line_serials',
            'qc_inspections','qc_inspection_revisions','job_orders','material_issue_requests',
            'delivery_challans','stock_posting_batches'
          ] LOOP
            IF to_regclass('__advance_schema__.' || future_table) IS NOT NULL THEN
              RAISE EXCEPTION 'Remove later Stores migration table % before Part 1.', future_table;
            END IF;
          END LOOP;
          IF (SELECT count(*) FROM __advance_schema__.item_company_inventory_settings)
             +(SELECT count(*) FROM __advance_schema__.store_category_routes)
             +(SELECT count(*) FROM __advance_schema__.stores_document_status_history)
             +(SELECT count(*) FROM __advance_schema__.gate_entries)
             +(SELECT count(*) FROM __advance_schema__.gate_entry_lines)
             +(SELECT count(*) FROM __advance_schema__.notification_events)
             +(SELECT count(*) FROM __advance_schema__.notification_recipients)
             +(SELECT count(*) FROM __advance_schema__.notification_delivery_attempts) <> 0 THEN
            RAISE EXCEPTION 'First Stores Part 1 rollback refuses persisted business data.';
          END IF;
          IF (SELECT count(*) FROM __advance_schema__.business_rule_configuration_versions)<>18
             OR EXISTS (
               SELECT 1 FROM __advance_schema__.business_rule_configuration_versions
               WHERE "VersionNumber"<>1 OR "PreviousVersionId" IS NOT NULL OR "OldValueJson" IS NOT NULL
                  OR "ChangedByEmployeeId"<>'3543a705-924a-6599-23be-fb9730a93f06'
                  OR "ChangedByRoleCode"<>'TECHNICAL_DIRECTOR'
                  OR "ChangeReason"<>'INITIAL_STORES_CONFIGURATION'
                  OR "EffectiveFrom"<>TIMESTAMPTZ '2026-08-27 00:00:00+00'
                  OR "ChangedAt"<>TIMESTAMPTZ '2026-08-27 00:00:00+00'
             )
             OR EXISTS (
               SELECT 1 FROM (VALUES
                 ('SERIAL_CAPTURE_THRESHOLD','5000'::jsonb),
                 ('QC_COMPLETION_DAYS','2'::jsonb),
                 ('EMERGENCY_PURCHASE_COUNT_PER_MONTH','10'::jsonb),
                 ('EMERGENCY_PURCHASE_VALUE_LIMIT','5000'::jsonb),
                 ('EXPENSE_FOOD_PER_PERSON_PER_DAY','300'::jsonb),
                 ('EXPENSE_LODGING_SINGLE_PER_DAY','800'::jsonb),
                 ('EXPENSE_LODGING_DOUBLE_PER_DAY','1200'::jsonb),
                 ('EXPENSE_DAILY_APPROVAL_CAP','5000'::jsonb),
                 ('EXPENSE_TRAVEL_DISTANCE_THRESHOLD_KM','100'::jsonb)
               ) expected(rule_key,rule_value)
               CROSS JOIN (VALUES
                 ('70000000-0000-0000-0000-000000000001'::uuid),
                 ('70000000-0000-0000-0000-000000000002'::uuid)
               ) c(id)
               WHERE NOT EXISTS (
                 SELECT 1 FROM __advance_schema__.business_rule_configuration_versions v
                 WHERE v."CompanyId"=c.id AND v."RuleKey"=expected.rule_key
                   AND v."NewValueJson"=expected.rule_value
               )
             ) THEN RAISE EXCEPTION 'First Stores Part 1 rollback refuses changed configuration evidence.'; END IF;
        END $guard$;

        DROP TRIGGER IF EXISTS "TR_notification_delivery_attempt_guard" ON __advance_schema__.notification_delivery_attempts;
        DROP TRIGGER IF EXISTS "TR_notification_recipient_guard" ON __advance_schema__.notification_recipients;
        DROP TRIGGER IF EXISTS "TR_notification_event_guard" ON __advance_schema__.notification_events;
        DROP TRIGGER IF EXISTS "TR_stores_document_status_history_guard" ON __advance_schema__.stores_document_status_history;
        DROP TRIGGER IF EXISTS "TR_gate_entry_line_guard" ON __advance_schema__.gate_entry_lines;
        DROP TRIGGER IF EXISTS "TR_gate_entry_header_guard" ON __advance_schema__.gate_entries;
        DROP TRIGGER IF EXISTS "TR_store_category_route_guard" ON __advance_schema__.store_category_routes;
        DROP TRIGGER IF EXISTS "TR_item_company_inventory_setting_guard" ON __advance_schema__.item_company_inventory_settings;
        DROP TRIGGER IF EXISTS "TR_business_rule_configuration_guard" ON __advance_schema__.business_rule_configuration_versions;
        ALTER TABLE __advance_schema__.store_category_routes
          DROP CONSTRAINT IF EXISTS "EX_store_category_routes_one_effective";
        DROP FUNCTION IF EXISTS __advance_schema__.stores_p1_notification_attempt_guard();
        DROP FUNCTION IF EXISTS __advance_schema__.stores_p1_notification_recipient_guard();
        DROP FUNCTION IF EXISTS __advance_schema__.stores_p1_notification_event_guard();
        DROP FUNCTION IF EXISTS __advance_schema__.stores_p1_status_history_guard();
        DROP FUNCTION IF EXISTS __advance_schema__.stores_p1_gate_line_guard();
        DROP FUNCTION IF EXISTS __advance_schema__.stores_p1_gate_header_guard();
        DROP FUNCTION IF EXISTS __advance_schema__.stores_p1_route_guard();
        DROP FUNCTION IF EXISTS __advance_schema__.stores_p1_item_setting_guard();
        DROP FUNCTION IF EXISTS __advance_schema__.stores_p1_configuration_guard();
        DROP FUNCTION IF EXISTS __advance_schema__.stores_p1_actor_has_role(uuid,uuid,text,date);
        """;
}
