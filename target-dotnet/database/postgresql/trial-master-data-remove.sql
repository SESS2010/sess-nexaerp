\set ON_ERROR_STOP on
\if :{?expected_database}
\else
  \echo 'ERROR: expected_database must be supplied.'
  \quit 3
\endif

BEGIN;
SELECT set_config('nexa.trial.expected_database', :'expected_database', true);
DO $guard$
BEGIN
  IF current_setting('server_version_num')::integer < 170000 THEN RAISE EXCEPTION 'Trial data requires PostgreSQL 17 or later.'; END IF;
  IF current_database() <> current_setting('nexa.trial.expected_database') THEN
    RAISE EXCEPTION 'Trial data database mismatch: expected %, connected %.', current_setting('nexa.trial.expected_database'), current_database();
  END IF;
  IF current_database() IN ('postgres','template0','template1') THEN RAISE EXCEPTION 'Trial data refuses PostgreSQL maintenance databases.'; END IF;
  IF to_regnamespace('advance') IS NULL OR (to_regclass('advance."__EFMigrationsHistory"') IS NULL AND to_regclass('public."__EFMigrationsHistory"') IS NULL) THEN
    RAISE EXCEPTION 'Trial data requires a migrated advance database.';
  END IF;
  IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname IN ('nexa_erp_owner','nexa_erp_migration','nexa_erp_bootstrap','nexa_erp_runtime')) THEN
    RAISE EXCEPTION 'Trial data is development-only and refuses a principal-provisioned database.';
  END IF;
END $guard$;
SELECT pg_advisory_xact_lock(hashtextextended('SESS.NexaERP.TRIAL_DATA',0));

-- Both markers are required. RESTRICT failures are deliberate: operational
-- data must be removed first and is never cascaded by this helper.
DELETE FROM advance.store_category_routes WHERE "CreatedBy"='TRIAL_DATA';
ALTER TABLE advance.item_company_inventory_settings DISABLE TRIGGER "TR_item_company_inventory_setting_guard";
ALTER TABLE advance.warehouse_condition_locations DISABLE TRIGGER trg_rev869a_warehouse_condition_version_guard;
DELETE FROM advance.item_company_inventory_settings WHERE "CreatedBy"='TRIAL_DATA';
DELETE FROM advance.warehouse_condition_locations WHERE "CreatedBy"='TRIAL_DATA';
ALTER TABLE advance.item_company_inventory_settings ENABLE TRIGGER "TR_item_company_inventory_setting_guard";
ALTER TABLE advance.warehouse_condition_locations ENABLE TRIGGER trg_rev869a_warehouse_condition_version_guard;
DELETE FROM advance.rack_bins WHERE "CreatedBy"='TRIAL_DATA' AND "BinCode" LIKE 'TRIAL-%';
DELETE FROM advance.warehouses WHERE "CreatedBy"='TRIAL_DATA' AND "WarehouseCode" LIKE 'TRIAL-%';
DELETE FROM advance.items WHERE "CreatedBy"='TRIAL_DATA' AND "ItemCode" LIKE 'TRIAL-%';
DELETE FROM advance.vendors WHERE "CreatedBy"='TRIAL_DATA' AND "VendorCode" LIKE 'TRIAL-%';
DELETE FROM advance.item_subcategories WHERE "CreatedBy"='TRIAL_DATA' AND "Code" LIKE 'TRIAL-%';
DELETE FROM advance.manufacturers WHERE "CreatedBy"='TRIAL_DATA' AND "Code" LIKE 'TRIAL-%';
DELETE FROM advance.item_categories WHERE "CreatedBy"='TRIAL_DATA';
DELETE FROM advance.uoms WHERE "CreatedBy"='TRIAL_DATA' AND "Code" LIKE 'TRIAL-%';

DO $verify$
DECLARE remaining bigint;
BEGIN
  SELECT sum(n) INTO remaining FROM (
    SELECT count(*) n FROM advance.uoms WHERE "CreatedBy"='TRIAL_DATA' AND "Code" LIKE 'TRIAL-%'
    UNION ALL SELECT count(*) FROM advance.store_category_routes WHERE "CreatedBy"='TRIAL_DATA'
    UNION ALL SELECT count(*) FROM advance.item_company_inventory_settings WHERE "CreatedBy"='TRIAL_DATA'
    UNION ALL SELECT count(*) FROM advance.warehouse_condition_locations WHERE "CreatedBy"='TRIAL_DATA'
    UNION ALL SELECT count(*) FROM advance.item_categories WHERE "CreatedBy"='TRIAL_DATA' AND "Code" LIKE 'TRIAL-%'
    UNION ALL SELECT count(*) FROM advance.item_subcategories WHERE "CreatedBy"='TRIAL_DATA' AND "Code" LIKE 'TRIAL-%'
    UNION ALL SELECT count(*) FROM advance.manufacturers WHERE "CreatedBy"='TRIAL_DATA' AND "Code" LIKE 'TRIAL-%'
    UNION ALL SELECT count(*) FROM advance.vendors WHERE "CreatedBy"='TRIAL_DATA' AND "VendorCode" LIKE 'TRIAL-%'
    UNION ALL SELECT count(*) FROM advance.items WHERE "CreatedBy"='TRIAL_DATA' AND "ItemCode" LIKE 'TRIAL-%'
    UNION ALL SELECT count(*) FROM advance.warehouses WHERE "CreatedBy"='TRIAL_DATA' AND "WarehouseCode" LIKE 'TRIAL-%'
    UNION ALL SELECT count(*) FROM advance.rack_bins WHERE "CreatedBy"='TRIAL_DATA' AND "BinCode" LIKE 'TRIAL-%'
  ) counts;
  IF remaining <> 0 THEN RAISE EXCEPTION 'Trial-data removal left % marked rows.',remaining; END IF;
END $verify$;
COMMIT;
\echo 'TRIAL_DATA removal complete: 0 marked rows remain.'
