internal static class DatabasePrincipalProvisioningSql
{
    internal const string Plan = """
        Principals:
          nexa_erp_owner      NOLOGIN; owns the database and application objects
          nexa_erp_migration  LOGIN; may SET ROLE to nexa_erp_owner for reviewed upgrades
          nexa_erp_bootstrap  LOGIN; connect/schema usage only until bootstrap functions are installed
          nexa_erp_runtime    LOGIN; SELECT/INSERT/UPDATE on current application tables; no DELETE or DDL
        Secrets are accepted only from NEXAERP_MIGRATION_PASSWORD,
        NEXAERP_BOOTSTRAP_PASSWORD, and NEXAERP_RUNTIME_PASSWORD.
        Replays reconcile ownership and grants but never rotate existing credentials.
        """;

    internal const string ClusterGuard = """
        SELECT current_setting('server_version_num')::integer,
               current_database(),
               to_regnamespace('advance') IS NOT NULL,
               EXISTS(SELECT 1 FROM pg_catalog.pg_roles WHERE rolname=session_user AND rolsuper);
        """;

    internal const string AcquireLock =
        "SELECT pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended('SESS.NexaERP.DatabasePrincipalProvisioning.v1', 0));";

    internal const string Provision = """
        DO $roles$
        DECLARE managed_count integer;
        BEGIN
          SELECT count(*) INTO managed_count FROM pg_catalog.pg_roles
          WHERE rolname IN ('nexa_erp_owner','nexa_erp_migration','nexa_erp_bootstrap','nexa_erp_runtime');
          IF managed_count=0 THEN
            CREATE ROLE nexa_erp_owner NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
            EXECUTE format('CREATE ROLE nexa_erp_migration LOGIN PASSWORD %L NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS',
              current_setting('nexa.installer.migration_password'));
            EXECUTE format('CREATE ROLE nexa_erp_bootstrap LOGIN PASSWORD %L NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS',
              current_setting('nexa.installer.bootstrap_password'));
            EXECUTE format('CREATE ROLE nexa_erp_runtime LOGIN PASSWORD %L NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS',
              current_setting('nexa.installer.runtime_password'));
            GRANT nexa_erp_owner TO nexa_erp_migration WITH INHERIT FALSE, SET TRUE;
          ELSIF managed_count<>4 THEN
            RAISE EXCEPTION 'Partial NexaERP principal state; refusing reconciliation.';
          END IF;
        END $roles$;

        DO $attributes$
        BEGIN
          IF EXISTS(
            SELECT 1 FROM pg_catalog.pg_roles
            WHERE rolname IN ('nexa_erp_owner','nexa_erp_migration','nexa_erp_bootstrap','nexa_erp_runtime')
              AND (rolsuper OR rolcreatedb OR rolcreaterole OR rolreplication OR rolbypassrls)
          ) THEN RAISE EXCEPTION 'A managed NexaERP role has prohibited cluster privileges.'; END IF;
          IF (SELECT rolcanlogin FROM pg_catalog.pg_roles WHERE rolname='nexa_erp_owner') THEN
            RAISE EXCEPTION 'nexa_erp_owner must remain NOLOGIN.';
          END IF;
          IF EXISTS(SELECT 1 FROM pg_catalog.pg_roles
                    WHERE rolname IN ('nexa_erp_migration','nexa_erp_bootstrap','nexa_erp_runtime') AND NOT rolcanlogin) THEN
            RAISE EXCEPTION 'Migration, bootstrap, and runtime principals must be LOGIN roles.';
          END IF;
        END $attributes$;

        DO $ownership$
        DECLARE item record;
        BEGIN
          FOR item IN
            SELECT c.relkind,n.nspname,c.relname
            FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname='advance' AND c.relkind IN ('r','p','v','m','S','f')
              -- Table ownership also transfers its serial/identity sequences.
              -- PostgreSQL refuses a separate owner change on an identity sequence.
              AND (c.relkind<>'S' OR NOT EXISTS(
                SELECT 1 FROM pg_catalog.pg_depend d
                WHERE d.classid='pg_catalog.pg_class'::regclass AND d.objid=c.oid
                  AND d.refclassid='pg_catalog.pg_class'::regclass AND d.refobjsubid>0
                  AND d.deptype IN ('a','i')))
          LOOP
            EXECUTE format(
              CASE item.relkind
                WHEN 'S' THEN 'ALTER SEQUENCE %I.%I OWNER TO nexa_erp_owner'
                WHEN 'v' THEN 'ALTER VIEW %I.%I OWNER TO nexa_erp_owner'
                WHEN 'm' THEN 'ALTER MATERIALIZED VIEW %I.%I OWNER TO nexa_erp_owner'
                WHEN 'f' THEN 'ALTER FOREIGN TABLE %I.%I OWNER TO nexa_erp_owner'
                ELSE 'ALTER TABLE %I.%I OWNER TO nexa_erp_owner'
              END,
              item.nspname,item.relname);
          END LOOP;
          FOR item IN
            SELECT p.oid,p.prokind
            FROM pg_catalog.pg_proc p
            JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
            WHERE n.nspname='advance' AND p.prokind IN ('f','p')
          LOOP
            EXECUTE format(
              CASE item.prokind WHEN 'p' THEN 'ALTER PROCEDURE %s OWNER TO nexa_erp_owner'
                                ELSE 'ALTER FUNCTION %s OWNER TO nexa_erp_owner' END,
              item.oid::regprocedure);
          END LOOP;
          IF to_regclass('public."__EFMigrationsHistory"') IS NOT NULL THEN
            ALTER TABLE public."__EFMigrationsHistory" OWNER TO nexa_erp_owner;
            REVOKE ALL ON TABLE public."__EFMigrationsHistory" FROM PUBLIC,nexa_erp_bootstrap,nexa_erp_runtime;
          END IF;
          ALTER SCHEMA advance OWNER TO nexa_erp_owner;
          EXECUTE format('ALTER DATABASE %I OWNER TO nexa_erp_owner',current_database());
        END $ownership$;

        DO $database_acl$
        BEGIN
          EXECUTE format('REVOKE ALL ON DATABASE %I FROM PUBLIC',current_database());
          EXECUTE format('GRANT CONNECT ON DATABASE %I TO nexa_erp_migration,nexa_erp_bootstrap,nexa_erp_runtime',current_database());
        END $database_acl$;
        REVOKE ALL ON SCHEMA advance FROM PUBLIC,nexa_erp_bootstrap,nexa_erp_runtime;
        GRANT USAGE ON SCHEMA advance TO nexa_erp_bootstrap,nexa_erp_runtime;
        REVOKE ALL ON ALL TABLES IN SCHEMA advance FROM PUBLIC,nexa_erp_bootstrap,nexa_erp_runtime;
        REVOKE ALL ON ALL SEQUENCES IN SCHEMA advance FROM PUBLIC,nexa_erp_bootstrap,nexa_erp_runtime;
        REVOKE ALL ON ALL FUNCTIONS IN SCHEMA advance FROM PUBLIC,nexa_erp_bootstrap,nexa_erp_runtime;

        DO $runtime_grants$
        DECLARE item record;
        BEGIN
          FOR item IN
            SELECT c.relname,c.relkind
            FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname='advance' AND c.relkind IN ('r','p','v','m','f')
              AND c.relname NOT IN ('authentication_bootstrap_state','command_requests','command_receipts','vendor_bills','vendor_bill_lines','vendor_bill_history','vendor_bill_cost_allocations','fifo_inventory_cost_layers','fifo_cost_consumptions','fifo_cost_restorations','fifo_consumption_creation_order','machine_delivery_challans','machine_delivery_signatures','machine_delivery_bom_entries','supplier_invoices','supplier_invoice_lines','supplier_invoice_cancellations','supplier_invoice_receipt_matches','supplier_invoice_bill_links','vendor_bill_charges','vendor_bill_charge_allocations','fifo_landed_cost_adjustments','actual_bom_valuation_adjustments','component_fitments','component_fitment_reversals','actual_boms','actual_bom_entries','job_order_fat_custody_explanations','job_order_fat_reconciliations','job_order_fat_reconciliation_lines','item_company_last_purchases','vendor_advances','vendor_advance_reversals','vendor_advance_adjustments','vendor_advance_adjustment_restorations','vendor_payments','vendor_payment_allocations','vendor_bank_advices')
          LOOP
            IF item.relkind IN ('v','m') THEN
              EXECUTE format('GRANT SELECT ON TABLE advance.%I TO nexa_erp_runtime',item.relname);
            ELSE
              EXECUTE format('GRANT SELECT,INSERT,UPDATE ON TABLE advance.%I TO nexa_erp_runtime',item.relname);
            END IF;
          END LOOP;
        END $runtime_grants$;
        GRANT USAGE,SELECT ON ALL SEQUENCES IN SCHEMA advance TO nexa_erp_runtime;
        DO $item_last_purchase_acl$ BEGIN
          IF to_regclass('advance.item_company_last_purchases') IS NOT NULL THEN
            REVOKE ALL ON TABLE advance.item_company_last_purchases FROM PUBLIC,nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
            GRANT SELECT ON TABLE advance.item_company_last_purchases TO nexa_erp_runtime;
          END IF;
        END $item_last_purchase_acl$;
        REVOKE ALL ON TABLE advance.authentication_bootstrap_state FROM nexa_erp_runtime,nexa_erp_bootstrap;

        DO $opening_stock_acl$
        BEGIN
          IF to_regclass('advance.opening_stock_import_staging_lines') IS NOT NULL
             OR to_regclass('advance.opening_stocks') IS NOT NULL
             OR to_regclass('advance.opening_stock_lines') IS NOT NULL
             OR to_regclass('advance.opening_stock_events') IS NOT NULL
             OR to_regprocedure('advance.stage_opening_stock_import_line(uuid,text,uuid,uuid,uuid,text,text,numeric,numeric,uuid,text,uuid,text,text)') IS NOT NULL
             OR to_regprocedure('advance.record_opening_stock_count(uuid,uuid,date,date,text,text,text,uuid,text,uuid,text,text)') IS NOT NULL
             OR to_regprocedure('advance.confirm_opening_stock_value(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text)') IS NOT NULL
             OR to_regprocedure('advance.authorize_opening_stock(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text)') IS NOT NULL THEN
            IF to_regclass('advance.opening_stock_import_staging_lines') IS NULL
               OR to_regclass('advance.opening_stocks') IS NULL
               OR to_regclass('advance.opening_stock_lines') IS NULL
               OR to_regclass('advance.opening_stock_events') IS NULL
               OR to_regprocedure('advance.stage_opening_stock_import_line(uuid,text,uuid,uuid,uuid,text,text,numeric,numeric,uuid,text,uuid,text,text)') IS NULL
               OR to_regprocedure('advance.record_opening_stock_count(uuid,uuid,date,date,text,text,text,uuid,text,uuid,text,text)') IS NULL
               OR to_regprocedure('advance.confirm_opening_stock_value(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text)') IS NULL
               OR to_regprocedure('advance.authorize_opening_stock(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text)') IS NULL
               OR to_regprocedure('advance.guard_opening_stock_evidence()') IS NULL
               OR to_regprocedure('advance.opening_stock_command_valid(uuid,uuid,text,uuid,text,text,text)') IS NULL THEN
              RAISE EXCEPTION 'Opening Stock security package is incomplete; principal provisioning refused.';
            END IF;
            REVOKE ALL ON advance.opening_stock_import_staging_lines,advance.opening_stocks,advance.opening_stock_lines,advance.opening_stock_events FROM PUBLIC,nexa_erp_runtime,nexa_erp_bootstrap;
            GRANT SELECT ON advance.opening_stock_import_staging_lines,advance.opening_stocks,advance.opening_stock_lines,advance.opening_stock_events TO nexa_erp_runtime;
            REVOKE EXECUTE ON FUNCTION advance.guard_opening_stock_evidence(),advance.opening_stock_command_valid(uuid,uuid,text,uuid,text,text,text) FROM nexa_erp_runtime;
            GRANT EXECUTE ON FUNCTION advance.stage_opening_stock_import_line(uuid,text,uuid,uuid,uuid,text,text,numeric,numeric,uuid,text,uuid,text,text),advance.record_opening_stock_count(uuid,uuid,date,date,text,text,text,uuid,text,uuid,text,text),advance.confirm_opening_stock_value(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text),advance.authorize_opening_stock(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text) TO nexa_erp_runtime;
          END IF;
        END $opening_stock_acl$;

        DO $company_report_acl$
        BEGIN
          IF to_regprocedure('advance.company_report_grni(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text)') IS NOT NULL THEN
            GRANT EXECUTE ON FUNCTION advance.company_report_grni(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text),
              advance.company_report_vendor_purchases(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text),
              advance.company_report_fifo_valuation(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text),
              advance.company_report_pending_approvals(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text),
              advance.company_report_purchase_register(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text)
              TO nexa_erp_runtime;
          END IF;
        END $company_report_acl$;

        DO $purchase_workload_acl$
        BEGIN
          IF to_regprocedure('advance.purchase_workload(text,uuid,uuid[],text,text,text,bigint,integer)') IS NOT NULL THEN
            REVOKE ALL ON FUNCTION advance.purchase_workload(text,uuid,uuid[],text,text,text,bigint,integer)
              FROM PUBLIC,nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
            GRANT EXECUTE ON FUNCTION advance.purchase_workload(text,uuid,uuid[],text,text,text,bigint,integer) TO nexa_erp_runtime;
          END IF;
        END $purchase_workload_acl$;


        DO $purchase_spending_acl$
        BEGIN
          IF to_regprocedure('advance.purchase_spending(text,uuid,uuid[],text,text,date,uuid,uuid,text,uuid,bigint,integer)') IS NOT NULL THEN
            REVOKE ALL ON FUNCTION advance.purchase_spending(text,uuid,uuid[],text,text,date,uuid,uuid,text,uuid,bigint,integer)
              FROM PUBLIC,nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
            GRANT EXECUTE ON FUNCTION advance.purchase_spending(text,uuid,uuid[],text,text,date,uuid,uuid,text,uuid,bigint,integer) TO nexa_erp_runtime;
          END IF;
        END $purchase_spending_acl$;

        DO $purchase_obligations_acl$
        BEGIN
          IF to_regprocedure('advance.purchase_obligations(text,uuid,uuid[],text,text,uuid,text,uuid,bigint,integer)') IS NOT NULL THEN
            REVOKE ALL ON FUNCTION advance.purchase_obligations(text,uuid,uuid[],text,text,uuid,text,uuid,bigint,integer)
              FROM PUBLIC,nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
            GRANT EXECUTE ON FUNCTION advance.purchase_obligations(text,uuid,uuid[],text,text,uuid,text,uuid,bigint,integer) TO nexa_erp_runtime;
          END IF;
        END $purchase_obligations_acl$;

        DO $purchase_open_orders_acl$
        BEGIN
          IF to_regprocedure('advance.purchase_open_orders(text,uuid,uuid[],text,uuid,text,uuid,boolean,bigint,integer)') IS NOT NULL THEN
            REVOKE ALL ON FUNCTION advance.purchase_open_orders(text,uuid,uuid[],text,uuid,text,uuid,boolean,bigint,integer)
              FROM PUBLIC,nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
            GRANT EXECUTE ON FUNCTION advance.purchase_open_orders(text,uuid,uuid[],text,uuid,text,uuid,boolean,bigint,integer) TO nexa_erp_runtime;
          END IF;
        END $purchase_open_orders_acl$;

        DO $stores_workload_acl$
        BEGIN
          IF to_regprocedure('advance.stores_workload(text,uuid,uuid[],text,text,uuid,bigint,integer)') IS NOT NULL THEN
            REVOKE ALL ON FUNCTION advance.stores_workload(text,uuid,uuid[],text,text,uuid,bigint,integer)
              FROM PUBLIC,nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
            GRANT EXECUTE ON FUNCTION advance.stores_workload(text,uuid,uuid[],text,text,uuid,bigint,integer) TO nexa_erp_runtime;
          END IF;
        END $stores_workload_acl$;
        DO $stores_qc_stock_acl$
        BEGIN
          IF to_regprocedure('advance.stores_qc_stock(text,uuid,uuid[],text,text,uuid,bigint,integer)') IS NOT NULL THEN
            REVOKE ALL ON FUNCTION advance.stores_qc_stock(text,uuid,uuid[],text,text,uuid,bigint,integer)
              FROM PUBLIC,nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
            GRANT EXECUTE ON FUNCTION advance.stores_qc_stock(text,uuid,uuid[],text,text,uuid,bigint,integer) TO nexa_erp_runtime;
          END IF;
        END $stores_qc_stock_acl$;


        DO $ceremony_acl$
        BEGIN
          IF to_regprocedure('advance.complete_authentication_bootstrap(text,text)') IS NOT NULL THEN
            EXECUTE 'REVOKE ALL ON FUNCTION advance.complete_authentication_bootstrap(text,text) FROM PUBLIC';
            EXECUTE 'REVOKE ALL ON FUNCTION advance.complete_authentication_bootstrap(text,text) FROM nexa_erp_runtime';
            EXECUTE 'REVOKE ALL ON FUNCTION advance.complete_authentication_bootstrap(text,text) FROM nexa_erp_migration';
            EXECUTE 'GRANT EXECUTE ON FUNCTION advance.complete_authentication_bootstrap(text,text) TO nexa_erp_bootstrap';
          END IF;
          IF to_regprocedure('advance.govern_authentication_bootstrap(text,text,boolean,boolean)') IS NOT NULL THEN
            REVOKE ALL ON FUNCTION advance.govern_authentication_bootstrap(text,text,boolean,boolean)
              FROM PUBLIC,nexa_erp_runtime,nexa_erp_migration;
            GRANT EXECUTE ON FUNCTION advance.govern_authentication_bootstrap(text,text,boolean,boolean) TO nexa_erp_bootstrap;
          END IF;
        END $ceremony_acl$;

        DO $stores_acl$
        BEGIN
          IF to_regprocedure('advance.stores_p1_actor_has_role(uuid,uuid,text,date)') IS NOT NULL THEN
            EXECUTE 'REVOKE ALL ON FUNCTION advance.stores_p1_actor_has_role(uuid,uuid,text,date) FROM PUBLIC,nexa_erp_bootstrap,nexa_erp_migration';
            EXECUTE 'GRANT EXECUTE ON FUNCTION advance.stores_p1_actor_has_role(uuid,uuid,text,date) TO nexa_erp_runtime';
          END IF;
          IF to_regprocedure('advance.post_stores_stock_batch(uuid,text,uuid,text,text,text,date,uuid,text,jsonb)') IS NOT NULL THEN
            REVOKE INSERT,UPDATE,DELETE ON advance.stock_posting_batches,advance.stock_movements FROM nexa_erp_runtime;
            GRANT SELECT ON advance.stock_posting_batches,advance.stock_movements TO nexa_erp_runtime;
            EXECUTE 'REVOKE ALL ON FUNCTION advance.post_stores_stock_batch(uuid,text,uuid,text,text,text,date,uuid,text,jsonb) FROM PUBLIC,nexa_erp_bootstrap,nexa_erp_migration';
            EXECUTE 'GRANT EXECUTE ON FUNCTION advance.post_stores_stock_batch(uuid,text,uuid,text,text,text,date,uuid,text,jsonb) TO nexa_erp_runtime';
          END IF;
          IF to_regprocedure('advance.replace_gate_entry_draft(uuid,uuid,bigint,text,text,text,timestamptz,jsonb,text,jsonb)') IS NOT NULL THEN
            EXECUTE 'REVOKE ALL ON FUNCTION advance.replace_gate_entry_draft(uuid,uuid,bigint,text,text,text,timestamptz,jsonb,text,jsonb) FROM PUBLIC,nexa_erp_bootstrap,nexa_erp_migration';
            EXECUTE 'GRANT EXECUTE ON FUNCTION advance.replace_gate_entry_draft(uuid,uuid,bigint,text,text,text,timestamptz,jsonb,text,jsonb) TO nexa_erp_runtime';
          END IF;          IF to_regprocedure('advance.replace_estimated_bom_draft_lines(uuid,text,uuid,bigint,uuid,text,text,text,text,jsonb)') IS NOT NULL THEN
            EXECUTE 'REVOKE ALL ON FUNCTION advance.replace_estimated_bom_draft_lines(uuid,text,uuid,bigint,uuid,text,text,text,text,jsonb) FROM PUBLIC,nexa_erp_bootstrap,nexa_erp_migration';
            EXECUTE 'GRANT EXECUTE ON FUNCTION advance.replace_estimated_bom_draft_lines(uuid,text,uuid,bigint,uuid,text,text,text,text,jsonb) TO nexa_erp_runtime';
          END IF;
          IF to_regprocedure('advance.finalize_goods_receipt(uuid,uuid,bigint,text,text,text,uuid,text,text)') IS NOT NULL THEN
            EXECUTE 'REVOKE ALL ON FUNCTION advance.finalize_goods_receipt(uuid,uuid,bigint,text,text,text,uuid,text,text) FROM PUBLIC,nexa_erp_bootstrap,nexa_erp_migration';
            EXECUTE 'GRANT EXECUTE ON FUNCTION advance.finalize_goods_receipt(uuid,uuid,bigint,text,text,text,uuid,text,text) TO nexa_erp_runtime';
          END IF;
          IF to_regprocedure('advance.reverse_goods_receipt(uuid,uuid,bigint,text,text,text,text,text,uuid,text,text)') IS NOT NULL THEN
            EXECUTE 'REVOKE ALL ON FUNCTION advance.reverse_goods_receipt(uuid,uuid,bigint,text,text,text,text,text,uuid,text,text) FROM PUBLIC,nexa_erp_bootstrap,nexa_erp_migration';
            EXECUTE 'GRANT EXECUTE ON FUNCTION advance.reverse_goods_receipt(uuid,uuid,bigint,text,text,text,text,text,uuid,text,text) TO nexa_erp_runtime';
          END IF;
          IF to_regprocedure('advance.post_material_issue_custody(uuid,uuid,text,text,text,uuid,text)') IS NOT NULL THEN
            EXECUTE 'REVOKE ALL ON FUNCTION advance.post_material_issue_custody(uuid,uuid,text,text,text,uuid,text) FROM PUBLIC,nexa_erp_bootstrap,nexa_erp_migration';
            EXECUTE 'GRANT EXECUTE ON FUNCTION advance.post_material_issue_custody(uuid,uuid,text,text,text,uuid,text) TO nexa_erp_runtime';
          END IF;
          IF to_regprocedure('advance.post_material_return_acceptance(uuid,uuid,text,text,text,uuid,text)') IS NOT NULL THEN
            EXECUTE 'REVOKE ALL ON FUNCTION advance.post_material_return_acceptance(uuid,uuid,text,text,text,uuid,text) FROM PUBLIC,nexa_erp_bootstrap,nexa_erp_migration';
            EXECUTE 'GRANT EXECUTE ON FUNCTION advance.post_material_return_acceptance(uuid,uuid,text,text,text,uuid,text) TO nexa_erp_runtime';
          END IF;
          IF to_regprocedure('advance.confirm_component_fitment(uuid,uuid,uuid,numeric,timestamptz,text,uuid,text,text,text,uuid,text,uuid,text,text)') IS NOT NULL
             OR to_regprocedure('advance.reverse_component_fitment(uuid,uuid,text,text,text,text,uuid,text,uuid,text,text)') IS NOT NULL THEN
            IF to_regprocedure('advance.confirm_component_fitment(uuid,uuid,uuid,numeric,timestamptz,text,uuid,text,text,text,uuid,text,uuid,text,text)') IS NULL
               OR to_regprocedure('advance.reverse_component_fitment(uuid,uuid,text,text,text,text,uuid,text,uuid,text,text)') IS NULL
               OR to_regclass('advance.component_fitments') IS NULL
               OR to_regclass('advance.component_fitment_reversals') IS NULL
               OR to_regclass('advance.actual_boms') IS NULL
               OR to_regclass('advance.actual_bom_entries') IS NULL THEN
              RAISE EXCEPTION 'Fitment controlled functions are partially installed.';
            END IF;
            REVOKE INSERT,UPDATE,DELETE ON advance.component_fitments,advance.component_fitment_reversals,
              advance.actual_boms,advance.actual_bom_entries FROM nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
            GRANT SELECT ON advance.component_fitments,advance.component_fitment_reversals,
              advance.actual_boms,advance.actual_bom_entries TO nexa_erp_runtime;
            EXECUTE 'REVOKE ALL ON FUNCTION advance.confirm_component_fitment(uuid,uuid,uuid,numeric,timestamptz,text,uuid,text,text,text,uuid,text,uuid,text,text),advance.reverse_component_fitment(uuid,uuid,text,text,text,text,uuid,text,uuid,text,text) FROM PUBLIC,nexa_erp_bootstrap,nexa_erp_migration';
            EXECUTE 'GRANT EXECUTE ON FUNCTION advance.confirm_component_fitment(uuid,uuid,uuid,numeric,timestamptz,text,uuid,text,text,text,uuid,text,uuid,text,text),advance.reverse_component_fitment(uuid,uuid,text,text,text,text,uuid,text,uuid,text,text) TO nexa_erp_runtime';
          END IF;
          IF to_regprocedure('advance.create_fat_custody_explanation(uuid,uuid,uuid,numeric,text,text,text,text,text,uuid,text,uuid,text,text)') IS NOT NULL
             OR to_regclass('advance.job_order_fat_reconciliations') IS NOT NULL THEN
            IF to_regprocedure('advance.reconcile_job_order_fat(uuid,uuid,text,text,text,text,uuid,text,uuid,text,text)') IS NULL
               OR to_regprocedure('advance.fat_authority_valid(uuid,uuid,text,uuid,text,text[])') IS NULL
               OR to_regprocedure('advance.fat_live_balances(uuid,uuid)') IS NULL
               OR to_regprocedure('advance.guard_fat_evidence()') IS NULL
               OR to_regprocedure('advance.guard_job_order_fat_readiness()') IS NULL
               OR to_regclass('advance.job_order_fat_custody_explanations') IS NULL
               OR to_regclass('advance.job_order_fat_reconciliations') IS NULL
               OR to_regclass('advance.job_order_fat_reconciliation_lines') IS NULL THEN
              RAISE EXCEPTION 'FAT readiness authority is partially installed.';
            END IF;
            REVOKE ALL ON advance.job_order_fat_custody_explanations,
              advance.job_order_fat_reconciliations,advance.job_order_fat_reconciliation_lines
              FROM nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
            GRANT SELECT ON advance.job_order_fat_custody_explanations,
              advance.job_order_fat_reconciliations,advance.job_order_fat_reconciliation_lines TO nexa_erp_runtime;
            EXECUTE 'REVOKE ALL ON FUNCTION advance.create_fat_custody_explanation(uuid,uuid,uuid,numeric,text,text,text,text,text,uuid,text,uuid,text,text),advance.reconcile_job_order_fat(uuid,uuid,text,text,text,text,uuid,text,uuid,text,text),advance.fat_authority_valid(uuid,uuid,text,uuid,text,text[]),advance.fat_live_balances(uuid,uuid),advance.guard_fat_evidence(),advance.guard_job_order_fat_readiness() FROM PUBLIC,nexa_erp_bootstrap,nexa_erp_migration';
            EXECUTE 'REVOKE EXECUTE ON FUNCTION advance.fat_authority_valid(uuid,uuid,text,uuid,text,text[]),advance.fat_live_balances(uuid,uuid),advance.guard_fat_evidence(),advance.guard_job_order_fat_readiness() FROM nexa_erp_runtime';
            EXECUTE 'GRANT EXECUTE ON FUNCTION advance.create_fat_custody_explanation(uuid,uuid,uuid,numeric,text,text,text,text,text,uuid,text,uuid,text,text),advance.reconcile_job_order_fat(uuid,uuid,text,text,text,text,uuid,text,uuid,text,text) TO nexa_erp_runtime';
          END IF;
          IF to_regprocedure('advance.create_vendor_bill(uuid,uuid,text,date,jsonb,text,text,text,uuid,text,uuid,text,text)') IS NOT NULL
             OR to_regprocedure('advance.decide_vendor_bill(uuid,uuid,bigint,boolean,text,text,text,text,uuid,text,uuid,text,text)') IS NOT NULL
             OR to_regprocedure('advance.reverse_vendor_bill(uuid,uuid,bigint,text,text,text,text,uuid,text,uuid,text,text)') IS NOT NULL THEN
            IF to_regprocedure('advance.create_vendor_bill(uuid,uuid,text,date,jsonb,text,text,text,uuid,text,uuid,text,text)') IS NULL
               OR to_regprocedure('advance.decide_vendor_bill(uuid,uuid,bigint,boolean,text,text,text,text,uuid,text,uuid,text,text)') IS NULL
               OR to_regprocedure('advance.reverse_vendor_bill(uuid,uuid,bigint,text,text,text,text,uuid,text,uuid,text,text)') IS NULL
               OR to_regprocedure('advance.create_fifo_layers_for_grn(uuid,uuid,uuid,text,uuid,text,text)') IS NULL
               OR to_regprocedure('advance.consume_fifo_for_issue(uuid,uuid,uuid,text,uuid,text,text)') IS NULL
               OR to_regprocedure('advance.get_vendor_bill(uuid,uuid)') IS NULL
               OR to_regprocedure('advance.list_vendor_bills(uuid,text,text,uuid,integer,integer)') IS NULL THEN
              RAISE EXCEPTION 'Vendor Bill/FIFO controlled functions are partially installed.';
            END IF;
            REVOKE ALL ON TABLE advance.vendor_bills,advance.vendor_bill_lines,advance.vendor_bill_history,advance.vendor_bill_cost_allocations,advance.fifo_inventory_cost_layers,advance.fifo_cost_consumptions FROM PUBLIC,nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
            EXECUTE 'REVOKE ALL ON FUNCTION advance.create_vendor_bill(uuid,uuid,text,date,jsonb,text,text,text,uuid,text,uuid,text,text),advance.decide_vendor_bill(uuid,uuid,bigint,boolean,text,text,text,text,uuid,text,uuid,text,text),advance.reverse_vendor_bill(uuid,uuid,bigint,text,text,text,text,uuid,text,uuid,text,text),advance.create_fifo_layers_for_grn(uuid,uuid,uuid,text,uuid,text,text),advance.consume_fifo_for_issue(uuid,uuid,uuid,text,uuid,text,text),advance.get_vendor_bill(uuid,uuid),advance.list_vendor_bills(uuid,text,text,uuid,integer,integer) FROM PUBLIC,nexa_erp_bootstrap,nexa_erp_migration';
            EXECUTE 'GRANT EXECUTE ON FUNCTION advance.create_vendor_bill(uuid,uuid,text,date,jsonb,text,text,text,uuid,text,uuid,text,text),advance.decide_vendor_bill(uuid,uuid,bigint,boolean,text,text,text,text,uuid,text,uuid,text,text),advance.reverse_vendor_bill(uuid,uuid,bigint,text,text,text,text,uuid,text,uuid,text,text),advance.create_fifo_layers_for_grn(uuid,uuid,uuid,text,uuid,text,text),advance.consume_fifo_for_issue(uuid,uuid,uuid,text,uuid,text,text),advance.get_vendor_bill(uuid,uuid),advance.list_vendor_bills(uuid,text,text,uuid,integer,integer) TO nexa_erp_runtime';
          END IF;
          IF to_regclass('advance.fifo_cost_restorations') IS NOT NULL THEN
            IF to_regclass('advance.fifo_consumption_creation_order') IS NULL
              OR to_regprocedure('advance.restore_fifo_for_material_return(uuid,uuid,boolean)') IS NULL
              OR to_regprocedure('advance.record_fifo_consumption_creation_order()') IS NULL
              OR to_regprocedure('advance.guard_fifo_restoration_evidence()') IS NULL THEN
              RAISE EXCEPTION 'FIFO restoration authority is partially installed.';
            END IF;
            REVOKE ALL ON advance.fifo_cost_restorations,advance.fifo_consumption_creation_order
              FROM PUBLIC,nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
            REVOKE ALL ON FUNCTION advance.restore_fifo_for_material_return(uuid,uuid,boolean),
              advance.record_fifo_consumption_creation_order(),advance.guard_fifo_restoration_evidence(),advance.guard_material_return_fifo_restoration()
              FROM PUBLIC,nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
            EXECUTE format('REVOKE ALL ON SEQUENCE %s FROM PUBLIC,nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration',
              pg_get_serial_sequence('advance.fifo_consumption_creation_order','CreationOrdinal'));
          END IF;
          IF to_regprocedure('advance.record_vendor_bill_charges(uuid,uuid,jsonb,jsonb,uuid,text,uuid,text,text)') IS NOT NULL
             OR to_regprocedure('advance.get_actual_bom_landed_valuations(uuid,uuid)') IS NOT NULL
             OR to_regclass('advance.vendor_bill_charges') IS NOT NULL
             OR to_regclass('advance.vendor_bill_charge_allocations') IS NOT NULL
             OR to_regclass('advance.fifo_landed_cost_adjustments') IS NOT NULL
             OR to_regclass('advance.actual_bom_valuation_adjustments') IS NOT NULL THEN
            IF to_regprocedure('advance.record_vendor_bill_charges(uuid,uuid,jsonb,jsonb,uuid,text,uuid,text,text)') IS NULL
               OR to_regprocedure('advance.get_actual_bom_landed_valuations(uuid,uuid)') IS NULL
               OR to_regprocedure('advance.allocate_vendor_bill_landed_cost()') IS NULL
               OR to_regclass('advance.vendor_bill_charges') IS NULL
               OR to_regclass('advance.vendor_bill_charge_allocations') IS NULL
               OR to_regclass('advance.fifo_landed_cost_adjustments') IS NULL
               OR to_regclass('advance.actual_bom_valuation_adjustments') IS NULL THEN
              RAISE EXCEPTION 'Immutable landed-cost authority is partially installed.';
            END IF;
            REVOKE ALL ON TABLE advance.vendor_bill_charges,
              advance.vendor_bill_charge_allocations,advance.fifo_landed_cost_adjustments,
              advance.actual_bom_valuation_adjustments
              FROM PUBLIC,nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
            EXECUTE 'REVOKE ALL ON FUNCTION advance.record_vendor_bill_charges(uuid,uuid,jsonb,jsonb,uuid,text,uuid,text,text),advance.get_actual_bom_landed_valuations(uuid,uuid) FROM PUBLIC,nexa_erp_bootstrap,nexa_erp_migration';
            EXECUTE 'GRANT EXECUTE ON FUNCTION advance.record_vendor_bill_charges(uuid,uuid,jsonb,jsonb,uuid,text,uuid,text,text),advance.get_actual_bom_landed_valuations(uuid,uuid) TO nexa_erp_runtime';
          END IF;
          IF to_regprocedure('advance.record_vendor_advance(uuid,uuid,date,numeric,text,text,text,text,text,uuid,text,uuid,text,text)') IS NOT NULL
             OR to_regprocedure('advance.record_vendor_payment(uuid,uuid,date,numeric,text,text,text,jsonb,text,text,uuid,text,uuid,text,text)') IS NOT NULL
             OR to_regprocedure('advance.list_vendor_advance_purchase_orders(uuid,uuid)') IS NOT NULL
             OR to_regclass('advance.vendor_advances') IS NOT NULL THEN
            IF to_regprocedure('advance.record_vendor_advance(uuid,uuid,date,numeric,text,text,text,text,text,uuid,text,uuid,text,text)') IS NULL
               OR to_regprocedure('advance.reverse_vendor_advance(uuid,uuid,text,text,text,uuid,text,uuid,text,text)') IS NULL
               OR to_regprocedure('advance.record_vendor_payment(uuid,uuid,date,numeric,text,text,text,jsonb,text,text,uuid,text,uuid,text,text)') IS NULL
               OR to_regprocedure('advance.list_vendor_advance_purchase_orders(uuid,uuid)') IS NULL
               OR to_regprocedure('advance.list_vendor_advances(uuid,uuid,uuid,boolean,integer,integer)') IS NULL
               OR to_regprocedure('advance.list_vendor_payments(uuid,uuid,integer,integer)') IS NULL
               OR to_regprocedure('advance.list_vendor_payables(uuid,uuid,boolean)') IS NULL
               OR to_regprocedure('advance.list_vendor_positions(uuid)') IS NULL
               OR to_regclass('advance.vendor_advances') IS NULL
               OR to_regclass('advance.vendor_advance_reversals') IS NULL
               OR to_regclass('advance.vendor_advance_adjustments') IS NULL
               OR to_regclass('advance.vendor_advance_adjustment_restorations') IS NULL
               OR to_regclass('advance.vendor_payments') IS NULL
               OR to_regclass('advance.vendor_payment_allocations') IS NULL THEN
              RAISE EXCEPTION 'Vendor advance/payment authority is partially installed.';
            END IF;
            REVOKE ALL ON TABLE advance.vendor_advances,
              advance.vendor_advance_reversals,advance.vendor_advance_adjustments,
              advance.vendor_advance_adjustment_restorations,advance.vendor_payments,
              advance.vendor_payment_allocations
              FROM PUBLIC,nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
            EXECUTE 'REVOKE ALL ON FUNCTION advance.record_vendor_advance(uuid,uuid,date,numeric,text,text,text,text,text,uuid,text,uuid,text,text),advance.reverse_vendor_advance(uuid,uuid,text,text,text,uuid,text,uuid,text,text),advance.record_vendor_payment(uuid,uuid,date,numeric,text,text,text,jsonb,text,text,uuid,text,uuid,text,text),advance.vendor_advance_json(uuid,uuid,boolean),advance.vendor_payment_json(uuid,uuid,boolean),advance.list_vendor_advance_purchase_orders(uuid,uuid),advance.list_vendor_advances(uuid,uuid,uuid,boolean,integer,integer),advance.list_vendor_payments(uuid,uuid,integer,integer),advance.list_vendor_payables(uuid,uuid,boolean),advance.list_vendor_positions(uuid) FROM PUBLIC,nexa_erp_bootstrap,nexa_erp_migration';
            EXECUTE 'GRANT EXECUTE ON FUNCTION advance.record_vendor_advance(uuid,uuid,date,numeric,text,text,text,text,text,uuid,text,uuid,text,text),advance.reverse_vendor_advance(uuid,uuid,text,text,text,uuid,text,uuid,text,text),advance.record_vendor_payment(uuid,uuid,date,numeric,text,text,text,jsonb,text,text,uuid,text,uuid,text,text),advance.vendor_advance_json(uuid,uuid,boolean),advance.vendor_payment_json(uuid,uuid,boolean),advance.list_vendor_advance_purchase_orders(uuid,uuid),advance.list_vendor_advances(uuid,uuid,uuid,boolean,integer,integer),advance.list_vendor_payments(uuid,uuid,integer,integer),advance.list_vendor_payables(uuid,uuid,boolean),advance.list_vendor_positions(uuid) TO nexa_erp_runtime';
          END IF;
          IF to_regclass('advance.vendor_bank_advices') IS NOT NULL OR EXISTS(SELECT 1 FROM (VALUES ('advance.record_vendor_bank_advice(uuid,uuid,text,text,bytea,text,text,text,uuid,text,uuid,text,text)'),('advance.vendor_bank_advice_json(uuid,uuid,boolean)'),('advance.vendor_bank_advice_content(uuid,uuid)'),('advance.require_vendor_bank_advice(uuid,uuid,text)')) f(name) WHERE to_regprocedure(f.name) IS NOT NULL) THEN
            IF to_regclass('advance.vendor_bank_advices') IS NULL OR EXISTS(SELECT 1 FROM (VALUES ('advance.record_vendor_bank_advice(uuid,uuid,text,text,bytea,text,text,text,uuid,text,uuid,text,text)'),('advance.vendor_bank_advice_json(uuid,uuid,boolean)'),('advance.vendor_bank_advice_content(uuid,uuid)'),('advance.require_vendor_bank_advice(uuid,uuid,text)')) f(name) WHERE to_regprocedure(f.name) IS NULL) THEN
              RAISE EXCEPTION 'Bank advice authority is partially installed.';
            END IF;
            REVOKE ALL ON TABLE advance.vendor_bank_advices FROM PUBLIC,nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
            REVOKE ALL ON FUNCTION advance.record_vendor_bank_advice(uuid,uuid,text,text,bytea,text,text,text,uuid,text,uuid,text,text),advance.vendor_bank_advice_json(uuid,uuid,boolean),advance.vendor_bank_advice_content(uuid,uuid),advance.require_vendor_bank_advice(uuid,uuid,text) FROM PUBLIC,nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
            GRANT EXECUTE ON FUNCTION advance.record_vendor_bank_advice(uuid,uuid,text,text,bytea,text,text,text,uuid,text,uuid,text,text),advance.vendor_bank_advice_json(uuid,uuid,boolean),advance.vendor_bank_advice_content(uuid,uuid) TO nexa_erp_runtime;
          END IF;
          IF EXISTS(SELECT 1 FROM (VALUES ('advance.vendor_po_cash_totals(uuid,uuid,text,uuid)'),('advance.require_vendor_po_cash_limit(uuid,uuid,numeric,text)'),('advance.guard_purchase_order_vendor_cash()')) f(name) WHERE to_regprocedure(f.name) IS NOT NULL)
             OR to_regclass('advance."IX_vendor_advances_company_po"') IS NOT NULL THEN
            IF EXISTS(SELECT 1 FROM (VALUES ('advance.vendor_po_cash_totals(uuid,uuid,text,uuid)'),('advance.require_vendor_po_cash_limit(uuid,uuid,numeric,text)'),('advance.guard_purchase_order_vendor_cash()')) f(name) WHERE to_regprocedure(f.name) IS NULL) THEN
              RAISE EXCEPTION 'Vendor cash authority is partially installed.';
            END IF;
            REVOKE ALL ON FUNCTION advance.vendor_po_cash_totals(uuid,uuid,text,uuid),advance.require_vendor_po_cash_limit(uuid,uuid,numeric,text),advance.guard_purchase_order_vendor_cash() FROM PUBLIC,nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
          END IF;

        END $stores_acl$;

        DO $ordinary_command_acl$
        BEGIN
          IF to_regprocedure('advance.register_command_request(text,text,bytea,bytea,uuid,text,text,text,uuid)') IS NOT NULL
             OR to_regprocedure('advance.commit_command_receipt(uuid,bytea,jsonb,uuid)') IS NOT NULL THEN
            IF to_regprocedure('advance.register_command_request(text,text,bytea,bytea,uuid,text,text,text,uuid)') IS NULL
               OR to_regprocedure('advance.commit_command_receipt(uuid,bytea,jsonb,uuid)') IS NULL
               OR to_regclass('advance.command_requests') IS NULL
               OR to_regclass('advance.command_receipts') IS NULL THEN
              RAISE EXCEPTION 'Ordinary command ledger is partially installed.';
            END IF;
            REVOKE ALL ON TABLE advance.command_requests,advance.command_receipts
              FROM PUBLIC,nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
            EXECUTE 'REVOKE ALL ON FUNCTION advance.register_command_request(text,text,bytea,bytea,uuid,text,text,text,uuid) FROM PUBLIC,nexa_erp_bootstrap,nexa_erp_migration';
            EXECUTE 'REVOKE ALL ON FUNCTION advance.commit_command_receipt(uuid,bytea,jsonb,uuid) FROM PUBLIC,nexa_erp_bootstrap,nexa_erp_migration';
            EXECUTE 'GRANT EXECUTE ON FUNCTION advance.register_command_request(text,text,bytea,bytea,uuid,text,text,text,uuid) TO nexa_erp_runtime';
            EXECUTE 'GRANT EXECUTE ON FUNCTION advance.commit_command_receipt(uuid,bytea,jsonb,uuid) TO nexa_erp_runtime';
            IF to_regprocedure('advance.read_command_receipt(uuid)') IS NOT NULL THEN
              EXECUTE 'REVOKE ALL ON FUNCTION advance.read_command_receipt(uuid) FROM PUBLIC,nexa_erp_bootstrap,nexa_erp_migration';
              EXECUTE 'GRANT EXECUTE ON FUNCTION advance.read_command_receipt(uuid) TO nexa_erp_runtime';
            END IF;
            IF to_regprocedure('advance.ordinary_command_context_valid(text,uuid,text,text,text)') IS NULL
               OR to_regprocedure('advance.ordinary_claim_command_context(text,uuid,text,uuid,text,bigint,text,text,text,text)') IS NULL THEN
              RAISE EXCEPTION 'Ordinary command ledger internal authority functions are partially installed.';
            END IF;
            EXECUTE 'REVOKE ALL ON FUNCTION advance.ordinary_command_context_valid(text,uuid,text,text,text) FROM PUBLIC,nexa_erp_bootstrap,nexa_erp_migration';
            EXECUTE 'REVOKE ALL ON FUNCTION advance.ordinary_claim_command_context(text,uuid,text,uuid,text,bigint,text,text,text,text) FROM PUBLIC,nexa_erp_bootstrap,nexa_erp_migration';
            EXECUTE 'GRANT EXECUTE ON FUNCTION advance.ordinary_command_context_valid(text,uuid,text,text,text) TO nexa_erp_runtime';
            EXECUTE 'GRANT EXECUTE ON FUNCTION advance.ordinary_claim_command_context(text,uuid,text,uuid,text,bigint,text,text,text,text) TO nexa_erp_runtime';
            IF to_regprocedure('advance.rev869b_commercial_snapshot_reconciles(uuid,jsonb,jsonb)') IS NOT NULL THEN
              EXECUTE 'REVOKE ALL ON FUNCTION advance.rev869b_commercial_snapshot_reconciles(uuid,jsonb,jsonb) FROM PUBLIC,nexa_erp_bootstrap,nexa_erp_migration';
              EXECUTE 'GRANT EXECUTE ON FUNCTION advance.rev869b_commercial_snapshot_reconciles(uuid,jsonb,jsonb) TO nexa_erp_runtime';
            END IF;
            IF to_regprocedure('advance.rev869b_qualification_provenance_valid(uuid)') IS NOT NULL THEN
              EXECUTE 'REVOKE ALL ON FUNCTION advance.rev869b_qualification_provenance_valid(uuid) FROM PUBLIC,nexa_erp_bootstrap,nexa_erp_migration';
              EXECUTE 'GRANT EXECUTE ON FUNCTION advance.rev869b_qualification_provenance_valid(uuid) TO nexa_erp_runtime';
            END IF;
          END IF;
        END $ordinary_command_acl$;

        ALTER DEFAULT PRIVILEGES FOR ROLE nexa_erp_owner IN SCHEMA advance REVOKE ALL ON TABLES FROM PUBLIC;
        ALTER DEFAULT PRIVILEGES FOR ROLE nexa_erp_owner IN SCHEMA advance REVOKE ALL ON SEQUENCES FROM PUBLIC;
        ALTER DEFAULT PRIVILEGES FOR ROLE nexa_erp_owner IN SCHEMA advance REVOKE EXECUTE ON FUNCTIONS FROM PUBLIC;
                DO $supplier_acl$
        DECLARE relation text; signature text; principal text;
        BEGIN
         IF to_regclass('advance.supplier_invoices') IS NOT NULL THEN
          FOREACH relation IN ARRAY ARRAY['supplier_invoices','supplier_invoice_lines','supplier_invoice_cancellations','supplier_invoice_receipt_matches','supplier_invoice_bill_links']
          LOOP
           IF to_regclass('advance.'||relation) IS NULL THEN RAISE EXCEPTION 'Supplier invoice package is partially installed.'; END IF;
           EXECUTE format('REVOKE ALL ON TABLE advance.%I FROM PUBLIC',relation);
           IF to_regrole('nexa_erp_owner') IS NOT NULL THEN EXECUTE format('ALTER TABLE advance.%I OWNER TO nexa_erp_owner',relation); END IF;
           FOREACH principal IN ARRAY ARRAY['nexa_erp_runtime','nexa_erp_bootstrap','nexa_erp_migration']
           LOOP
            IF to_regrole(principal) IS NOT NULL THEN EXECUTE format('REVOKE ALL ON TABLE advance.%I FROM %I',relation,principal); END IF;
           END LOOP;
          END LOOP;
          FOREACH signature IN ARRAY ARRAY['advance.guard_supplier_invoice_evidence()','advance.supplier_invoice_command_valid(uuid,uuid,text,uuid,text,text,text)','advance.supplier_invoice_version(uuid,uuid)','advance.reconcile_supplier_invoice_receipts(uuid,uuid,uuid)','advance.match_supplier_invoices_on_receipt()','advance.record_supplier_invoice(uuid,uuid,uuid,text,date,text,jsonb,text,text,bytea,uuid,text,uuid,text,text)','advance.cancel_supplier_invoice(uuid,uuid,bigint,text,uuid,text,uuid,text)','advance.link_supplier_invoice_bill(uuid,uuid,bigint,uuid,uuid,text,uuid,text)','advance.get_supplier_invoice(uuid,uuid)','advance.supplier_invoice_content(uuid,uuid)','advance.company_report_billed_not_received(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text)']
          LOOP
           IF to_regprocedure(signature) IS NULL THEN RAISE EXCEPTION 'Supplier invoice function is missing: %',signature; END IF;
           EXECUTE format('REVOKE ALL ON FUNCTION %s FROM PUBLIC',signature);
           IF to_regrole('nexa_erp_owner') IS NOT NULL THEN EXECUTE format('ALTER FUNCTION %s OWNER TO nexa_erp_owner',signature); END IF;
           FOREACH principal IN ARRAY ARRAY['nexa_erp_runtime','nexa_erp_bootstrap','nexa_erp_migration']
           LOOP
            IF to_regrole(principal) IS NOT NULL THEN EXECUTE format('REVOKE ALL ON FUNCTION %s FROM %I',signature,principal); END IF;
           END LOOP;
          END LOOP;
          IF to_regrole('nexa_erp_runtime') IS NOT NULL THEN
           GRANT EXECUTE ON FUNCTION advance.record_supplier_invoice(uuid,uuid,uuid,text,date,text,jsonb,text,text,bytea,uuid,text,uuid,text,text),advance.cancel_supplier_invoice(uuid,uuid,bigint,text,uuid,text,uuid,text),advance.link_supplier_invoice_bill(uuid,uuid,bigint,uuid,uuid,text,uuid,text),advance.get_supplier_invoice(uuid,uuid),advance.supplier_invoice_content(uuid,uuid),advance.company_report_billed_not_received(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text)
            TO nexa_erp_runtime;
          END IF;
         END IF;
        END $supplier_acl$;
         DO $machine_acl$ DECLARE relation text; signature text; principal text; BEGIN
         IF to_regclass('advance.machine_delivery_challans') IS NOT NULL THEN
          FOREACH relation IN ARRAY ARRAY['machine_delivery_challans','machine_delivery_signatures','machine_delivery_bom_entries'] LOOP
           EXECUTE format('REVOKE ALL ON TABLE advance.%I FROM PUBLIC',relation);
           IF to_regrole('nexa_erp_owner') IS NOT NULL THEN EXECUTE format('ALTER TABLE advance.%I OWNER TO nexa_erp_owner',relation); END IF;
           FOREACH principal IN ARRAY ARRAY['nexa_erp_runtime','nexa_erp_bootstrap','nexa_erp_migration'] LOOP
            IF to_regrole(principal) IS NOT NULL THEN EXECUTE format('REVOKE ALL ON TABLE advance.%I FROM %I',relation,principal); END IF;
           END LOOP;
          END LOOP;
          FOREACH signature IN ARRAY ARRAY['advance.guard_machine_delivery_evidence()','advance.machine_delivery_signature_content(uuid,uuid)','advance.machine_delivery_json(uuid,uuid)',
           'advance.record_machine_delivery(uuid,uuid,uuid,text,jsonb,bytea,uuid,text,uuid,text,text)',
           'advance.company_report_machine_dossier(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text)'] LOOP
           EXECUTE format('REVOKE ALL ON FUNCTION %s FROM PUBLIC',signature);
           IF to_regrole('nexa_erp_owner') IS NOT NULL THEN EXECUTE format('ALTER FUNCTION %s OWNER TO nexa_erp_owner',signature); END IF;
           FOREACH principal IN ARRAY ARRAY['nexa_erp_runtime','nexa_erp_bootstrap','nexa_erp_migration'] LOOP
            IF to_regrole(principal) IS NOT NULL THEN EXECUTE format('REVOKE ALL ON FUNCTION %s FROM %I',signature,principal); END IF;
           END LOOP;
          END LOOP;
          IF to_regrole('nexa_erp_runtime') IS NOT NULL THEN
           GRANT EXECUTE ON FUNCTION advance.machine_delivery_signature_content(uuid,uuid),advance.machine_delivery_json(uuid,uuid),advance.record_machine_delivery(uuid,uuid,uuid,text,jsonb,bytea,uuid,text,uuid,text,text),
            advance.company_report_machine_dossier(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text) TO nexa_erp_runtime;
          END IF;
         END IF;
         END $machine_acl$;

        """ + SESS.NexaERP.Database.IntercompanyRoutes20260915AccessSql.Provision + SESS.NexaERP.Database.IntercompanyPurchase20260915AccessSql.Provision;

    internal const string Verify = """
        DO $verify$
        BEGIN
          IF (SELECT count(*) FROM pg_catalog.pg_roles
              WHERE rolname IN ('nexa_erp_owner','nexa_erp_migration','nexa_erp_bootstrap','nexa_erp_runtime'))<>4 THEN
            RAISE EXCEPTION 'Exactly four managed NexaERP roles are required.';
          END IF;
          IF EXISTS(SELECT 1 FROM pg_catalog.pg_roles
                    WHERE rolname IN ('nexa_erp_owner','nexa_erp_migration','nexa_erp_bootstrap','nexa_erp_runtime')
                      AND (rolsuper OR rolcreatedb OR rolcreaterole OR rolreplication OR rolbypassrls)) THEN
            RAISE EXCEPTION 'Managed role has prohibited cluster privilege.';
          END IF;
          IF (SELECT rolcanlogin FROM pg_catalog.pg_roles WHERE rolname='nexa_erp_owner')
             OR EXISTS(SELECT 1 FROM pg_catalog.pg_roles
                       WHERE rolname IN ('nexa_erp_migration','nexa_erp_bootstrap','nexa_erp_runtime') AND NOT rolcanlogin) THEN
            RAISE EXCEPTION 'Managed LOGIN attributes are invalid.';
          END IF;
          IF NOT EXISTS(
            SELECT 1 FROM pg_catalog.pg_auth_members m
            JOIN pg_catalog.pg_roles granted ON granted.oid=m.roleid
            JOIN pg_catalog.pg_roles member ON member.oid=m.member
            WHERE granted.rolname='nexa_erp_owner' AND member.rolname='nexa_erp_migration'
              AND m.set_option AND NOT m.inherit_option
          ) THEN RAISE EXCEPTION 'Migration-to-owner SET ROLE membership is missing or too broad.'; END IF;
          IF EXISTS(
            SELECT 1 FROM pg_catalog.pg_auth_members m
            JOIN pg_catalog.pg_roles granted ON granted.oid=m.roleid
            JOIN pg_catalog.pg_roles member ON member.oid=m.member
            WHERE granted.rolname='nexa_erp_owner' AND member.rolname IN ('nexa_erp_bootstrap','nexa_erp_runtime')
          ) THEN RAISE EXCEPTION 'Bootstrap or runtime principal must not inherit owner membership.'; END IF;
          IF (SELECT pg_catalog.pg_get_userbyid(datdba) FROM pg_catalog.pg_database WHERE datname=current_database())<>'nexa_erp_owner'
             OR (SELECT pg_catalog.pg_get_userbyid(nspowner) FROM pg_catalog.pg_namespace WHERE nspname='advance')<>'nexa_erp_owner' THEN
            RAISE EXCEPTION 'Database or advance schema ownership was not transferred.';
          END IF;
          IF EXISTS(
            SELECT 1 FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname='advance' AND c.relkind IN ('r','p','v','m','S','f')
              AND pg_catalog.pg_get_userbyid(c.relowner)<>'nexa_erp_owner'
          ) THEN RAISE EXCEPTION 'An advance relation is not owned by nexa_erp_owner.'; END IF;
          IF has_table_privilege('nexa_erp_runtime','advance.authentication_bootstrap_state','SELECT')
             OR has_table_privilege('nexa_erp_runtime','advance.authentication_bootstrap_state','INSERT')
             OR has_table_privilege('nexa_erp_runtime','advance.authentication_bootstrap_state','UPDATE')
             OR has_table_privilege('nexa_erp_runtime','advance.authentication_bootstrap_state','DELETE') THEN
            RAISE EXCEPTION 'Runtime must have no direct bootstrap-state access.';
          END IF;
          IF EXISTS(
            SELECT 1 FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname='advance' AND c.relkind IN ('r','p','f')
              AND c.relname<>'authentication_bootstrap_state'
              AND c.relname NOT IN ('opening_stock_import_staging_lines','opening_stocks','opening_stock_lines','opening_stock_events')
              AND NOT (c.relname IN ('intercompany_purchase_publications','intercompany_purchase_publication_lines')
                       AND to_regprocedure('advance.publish_intercompany_purchase(uuid,uuid,uuid,uuid,bigint,text,uuid,text,uuid,text,text)') IS NOT NULL)
              AND NOT (c.relname IN ('intercompany_routes','intercompany_route_decisions')
                       AND to_regprocedure('advance.record_intercompany_route(uuid,uuid,uuid,text,jsonb,uuid,text,uuid,text,text)') IS NOT NULL)
              AND NOT (c.relname IN ('command_requests','command_receipts')
                       AND to_regprocedure('advance.register_command_request(text,text,bytea,bytea,uuid,text,text,text,uuid)') IS NOT NULL)
              AND NOT (c.relname IN ('stock_posting_batches','stock_movements')
                       AND to_regprocedure('advance.post_stores_stock_batch(uuid,text,uuid,text,text,text,date,uuid,text,jsonb)') IS NOT NULL)
              AND NOT (c.relname IN ('vendor_bills','vendor_bill_lines','vendor_bill_history','vendor_bill_cost_allocations','fifo_inventory_cost_layers','fifo_cost_consumptions','fifo_cost_restorations','fifo_consumption_creation_order','machine_delivery_challans','machine_delivery_signatures','machine_delivery_bom_entries','supplier_invoices','supplier_invoice_lines','supplier_invoice_cancellations','supplier_invoice_receipt_matches','supplier_invoice_bill_links','vendor_bill_charges','vendor_bill_charge_allocations','fifo_landed_cost_adjustments','actual_bom_valuation_adjustments','item_company_last_purchases','vendor_advances','vendor_advance_reversals','vendor_advance_adjustments','vendor_advance_adjustment_restorations','vendor_payments','vendor_payment_allocations','vendor_bank_advices')
                       AND to_regprocedure('advance.create_vendor_bill(uuid,uuid,text,date,jsonb,text,text,text,uuid,text,uuid,text,text)') IS NOT NULL)
              AND NOT (c.relname IN ('component_fitments','component_fitment_reversals','actual_boms','actual_bom_entries','job_order_fat_custody_explanations','job_order_fat_reconciliations','job_order_fat_reconciliation_lines')
                       AND to_regprocedure('advance.confirm_component_fitment(uuid,uuid,uuid,numeric,timestamptz,text,uuid,text,text,text,uuid,text,uuid,text,text)') IS NOT NULL)
              AND (NOT has_table_privilege('nexa_erp_runtime',c.oid,'SELECT')
                   OR NOT has_table_privilege('nexa_erp_runtime',c.oid,'INSERT')
                   OR NOT has_table_privilege('nexa_erp_runtime',c.oid,'UPDATE')
                   OR has_table_privilege('nexa_erp_runtime',c.oid,'DELETE'))
          ) THEN RAISE EXCEPTION 'Runtime table privileges differ from SELECT/INSERT/UPDATE without DELETE.'; END IF;
          IF to_regprocedure('advance.govern_authentication_bootstrap(text,text,boolean,boolean)') IS NOT NULL THEN
            IF NOT has_function_privilege('nexa_erp_bootstrap','advance.govern_authentication_bootstrap(text,text,boolean,boolean)','EXECUTE')
               OR has_function_privilege('nexa_erp_runtime','advance.govern_authentication_bootstrap(text,text,boolean,boolean)','EXECUTE')
               OR has_function_privilege('nexa_erp_migration','advance.govern_authentication_bootstrap(text,text,boolean,boolean)','EXECUTE') THEN
              RAISE EXCEPTION 'Governed authentication ceremony ACL is invalid.';
            END IF;
          END IF;
          IF to_regprocedure('advance.complete_authentication_bootstrap(text,text)') IS NOT NULL THEN
            IF (
              SELECT count(*)
              FROM pg_catalog.pg_proc p
              CROSS JOIN LATERAL pg_catalog.aclexplode(COALESCE(p.proacl,pg_catalog.acldefault('f',p.proowner))) acl
              LEFT JOIN pg_catalog.pg_roles grantee ON grantee.oid=acl.grantee
              WHERE p.oid=to_regprocedure('advance.complete_authentication_bootstrap(text,text)')
                AND acl.privilege_type='EXECUTE' AND acl.grantee<>p.proowner
            )<>1 OR NOT EXISTS(
              SELECT 1
              FROM pg_catalog.pg_proc p
              CROSS JOIN LATERAL pg_catalog.aclexplode(COALESCE(p.proacl,pg_catalog.acldefault('f',p.proowner))) acl
              JOIN pg_catalog.pg_roles grantee ON grantee.oid=acl.grantee
              WHERE p.oid=to_regprocedure('advance.complete_authentication_bootstrap(text,text)')
                AND acl.privilege_type='EXECUTE' AND grantee.rolname='nexa_erp_bootstrap' AND NOT acl.is_grantable
            ) THEN
              RAISE EXCEPTION 'Ceremony function EXECUTE ACL must grant only nexa_erp_bootstrap outside its owner.';
            END IF;
          END IF;
          IF to_regprocedure('advance.post_stores_stock_batch(uuid,text,uuid,text,text,text,date,uuid,text,jsonb)') IS NOT NULL THEN
            IF has_table_privilege('nexa_erp_runtime','advance.stock_posting_batches','INSERT')
               OR has_table_privilege('nexa_erp_runtime','advance.stock_posting_batches','UPDATE')
               OR has_table_privilege('nexa_erp_runtime','advance.stock_posting_batches','DELETE')
               OR has_table_privilege('nexa_erp_runtime','advance.stock_movements','INSERT')
               OR has_table_privilege('nexa_erp_runtime','advance.stock_movements','UPDATE')
               OR has_table_privilege('nexa_erp_runtime','advance.stock_movements','DELETE') THEN
              RAISE EXCEPTION 'Runtime stock ledger mutation must be available only through the controlled posting function.';
            END IF;
            IF NOT has_function_privilege('nexa_erp_runtime','advance.post_stores_stock_batch(uuid,text,uuid,text,text,text,date,uuid,text,jsonb)','EXECUTE')
               OR has_function_privilege('nexa_erp_bootstrap','advance.post_stores_stock_batch(uuid,text,uuid,text,text,text,date,uuid,text,jsonb)','EXECUTE')
               OR has_function_privilege('nexa_erp_migration','advance.post_stores_stock_batch(uuid,text,uuid,text,text,text,date,uuid,text,jsonb)','EXECUTE') THEN
              RAISE EXCEPTION 'Controlled Stores posting function ACL is invalid.';
            END IF;
          END IF;
          IF to_regprocedure('advance.replace_gate_entry_draft(uuid,uuid,bigint,text,text,text,timestamptz,jsonb,text,jsonb)') IS NOT NULL
             AND (NOT has_function_privilege('nexa_erp_runtime','advance.replace_gate_entry_draft(uuid,uuid,bigint,text,text,text,timestamptz,jsonb,text,jsonb)','EXECUTE')
                  OR has_function_privilege('nexa_erp_bootstrap','advance.replace_gate_entry_draft(uuid,uuid,bigint,text,text,text,timestamptz,jsonb,text,jsonb)','EXECUTE')
                  OR has_function_privilege('nexa_erp_migration','advance.replace_gate_entry_draft(uuid,uuid,bigint,text,text,text,timestamptz,jsonb,text,jsonb)','EXECUTE')) THEN
            RAISE EXCEPTION 'Controlled Gate Entry draft function ACL is invalid.';
          END IF;          IF to_regprocedure('advance.replace_estimated_bom_draft_lines(uuid,text,uuid,bigint,uuid,text,text,text,text,jsonb)') IS NOT NULL
             AND (NOT has_function_privilege('nexa_erp_runtime','advance.replace_estimated_bom_draft_lines(uuid,text,uuid,bigint,uuid,text,text,text,text,jsonb)','EXECUTE')
                  OR has_function_privilege('nexa_erp_bootstrap','advance.replace_estimated_bom_draft_lines(uuid,text,uuid,bigint,uuid,text,text,text,text,jsonb)','EXECUTE')
                  OR has_function_privilege('nexa_erp_migration','advance.replace_estimated_bom_draft_lines(uuid,text,uuid,bigint,uuid,text,text,text,text,jsonb)','EXECUTE')) THEN
            RAISE EXCEPTION 'Controlled Estimated BOM draft replacement function ACL is invalid.';
          END IF;
          IF to_regprocedure('advance.finalize_goods_receipt(uuid,uuid,bigint,text,text,text,uuid,text,text)') IS NOT NULL
             AND (NOT has_function_privilege('nexa_erp_runtime','advance.finalize_goods_receipt(uuid,uuid,bigint,text,text,text,uuid,text,text)','EXECUTE')
                  OR has_function_privilege('nexa_erp_bootstrap','advance.finalize_goods_receipt(uuid,uuid,bigint,text,text,text,uuid,text,text)','EXECUTE')
                  OR has_function_privilege('nexa_erp_migration','advance.finalize_goods_receipt(uuid,uuid,bigint,text,text,text,uuid,text,text)','EXECUTE')) THEN
            RAISE EXCEPTION 'Controlled GRN finalization function ACL is invalid.';
          END IF;
          IF to_regprocedure('advance.reverse_goods_receipt(uuid,uuid,bigint,text,text,text,text,text,uuid,text,text)') IS NOT NULL
             AND (NOT has_function_privilege('nexa_erp_runtime','advance.reverse_goods_receipt(uuid,uuid,bigint,text,text,text,text,text,uuid,text,text)','EXECUTE')
                  OR has_function_privilege('nexa_erp_bootstrap','advance.reverse_goods_receipt(uuid,uuid,bigint,text,text,text,text,text,uuid,text,text)','EXECUTE')
                  OR has_function_privilege('nexa_erp_migration','advance.reverse_goods_receipt(uuid,uuid,bigint,text,text,text,text,text,uuid,text,text)','EXECUTE')) THEN
            RAISE EXCEPTION 'Controlled GRN reversal function ACL is invalid.';
          END IF;
          IF to_regprocedure('advance.post_material_issue_custody(uuid,uuid,text,text,text,uuid,text)') IS NOT NULL
             AND (NOT has_function_privilege('nexa_erp_runtime','advance.post_material_issue_custody(uuid,uuid,text,text,text,uuid,text)','EXECUTE')
                  OR has_function_privilege('nexa_erp_bootstrap','advance.post_material_issue_custody(uuid,uuid,text,text,text,uuid,text)','EXECUTE')
                  OR has_function_privilege('nexa_erp_migration','advance.post_material_issue_custody(uuid,uuid,text,text,text,uuid,text)','EXECUTE')) THEN
            RAISE EXCEPTION 'Controlled Material Issue custody function ACL is invalid.';
          END IF;
          IF to_regprocedure('advance.post_material_return_acceptance(uuid,uuid,text,text,text,uuid,text)') IS NOT NULL
             AND (NOT has_function_privilege('nexa_erp_runtime','advance.post_material_return_acceptance(uuid,uuid,text,text,text,uuid,text)','EXECUTE')
                  OR has_function_privilege('nexa_erp_bootstrap','advance.post_material_return_acceptance(uuid,uuid,text,text,text,uuid,text)','EXECUTE')
                  OR has_function_privilege('nexa_erp_migration','advance.post_material_return_acceptance(uuid,uuid,text,text,text,uuid,text)','EXECUTE')) THEN
            RAISE EXCEPTION 'Controlled Material Return acceptance function ACL is invalid.';
          END IF;

          IF EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='dashboards.purchase')
             OR to_regprocedure('advance.purchase_workload(text,uuid,uuid[],text,text,text,bigint,integer)') IS NOT NULL THEN
            IF NOT EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='dashboards.purchase')
              OR NOT EXISTS(SELECT 1 FROM pg_proc p
                WHERE p.oid=to_regprocedure('advance.purchase_workload(text,uuid,uuid[],text,text,text,bigint,integer)')
                  AND p.prosecdef AND p.provolatile='s' AND p.proowner='nexa_erp_owner'::regrole
                  AND p.proconfig=ARRAY['search_path=pg_catalog, advance']
                  AND has_function_privilege('nexa_erp_runtime',p.oid,'EXECUTE')
                  AND NOT EXISTS(SELECT 1 FROM aclexplode(coalesce(p.proacl,acldefault('f',p.proowner))) a
                    WHERE a.grantee<>p.proowner AND(a.grantee<>'nexa_erp_runtime'::regrole OR a.is_grantable))) THEN
              RAISE EXCEPTION 'Purchase workload authority is incomplete or invalid.';
            END IF;
          END IF;

          IF EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='dashboards.purchase-spending')
             OR to_regprocedure('advance.purchase_spending(text,uuid,uuid[],text,text,date,uuid,uuid,text,uuid,bigint,integer)') IS NOT NULL THEN
            IF NOT EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='dashboards.purchase-spending')
              OR NOT EXISTS(SELECT 1 FROM pg_proc p
                WHERE p.oid=to_regprocedure('advance.purchase_spending(text,uuid,uuid[],text,text,date,uuid,uuid,text,uuid,bigint,integer)')
                  AND p.prosecdef AND p.provolatile='s' AND p.proowner='nexa_erp_owner'::regrole
                  AND p.proconfig=ARRAY['search_path=pg_catalog, advance']
                  AND has_function_privilege('nexa_erp_runtime',p.oid,'EXECUTE')
                  AND NOT EXISTS(SELECT 1 FROM aclexplode(coalesce(p.proacl,acldefault('f',p.proowner))) a
                    WHERE a.grantee<>p.proowner AND(a.grantee<>'nexa_erp_runtime'::regrole OR a.is_grantable))) THEN
              RAISE EXCEPTION 'Purchase spending authority is incomplete or invalid.';
            END IF;
          END IF;

          IF EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='dashboards.purchase-obligations')
             OR to_regprocedure('advance.purchase_obligations(text,uuid,uuid[],text,text,uuid,text,uuid,bigint,integer)') IS NOT NULL THEN
            IF NOT EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='dashboards.purchase-obligations')
              OR NOT EXISTS(SELECT 1 FROM pg_proc p
                WHERE p.oid=to_regprocedure('advance.purchase_obligations(text,uuid,uuid[],text,text,uuid,text,uuid,bigint,integer)')
                  AND p.prosecdef AND p.provolatile='s' AND p.proowner='nexa_erp_owner'::regrole
                  AND p.proconfig=ARRAY['search_path=pg_catalog, advance']
                  AND has_function_privilege('nexa_erp_runtime',p.oid,'EXECUTE')
                  AND NOT EXISTS(SELECT 1 FROM aclexplode(coalesce(p.proacl,acldefault('f',p.proowner))) a
                    WHERE a.grantee<>p.proowner AND(a.grantee<>'nexa_erp_runtime'::regrole OR a.is_grantable))) THEN
              RAISE EXCEPTION 'Purchase obligations authority is incomplete or invalid.';
            END IF;
          END IF;

          IF EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='dashboards.purchase-open-orders')
             OR to_regprocedure('advance.purchase_open_orders(text,uuid,uuid[],text,uuid,text,uuid,boolean,bigint,integer)') IS NOT NULL THEN
            IF NOT EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='dashboards.purchase-open-orders')
              OR NOT EXISTS(SELECT 1 FROM pg_proc p
                WHERE p.oid=to_regprocedure('advance.purchase_open_orders(text,uuid,uuid[],text,uuid,text,uuid,boolean,bigint,integer)')
                  AND p.prosecdef AND p.provolatile='s' AND p.proowner='nexa_erp_owner'::regrole
                  AND p.proconfig=ARRAY['search_path=pg_catalog, advance']
                  AND has_function_privilege('nexa_erp_runtime',p.oid,'EXECUTE')
                  AND NOT EXISTS(SELECT 1 FROM aclexplode(coalesce(p.proacl,acldefault('f',p.proowner))) a
                    WHERE a.grantee<>p.proowner AND(a.grantee<>'nexa_erp_runtime'::regrole OR a.is_grantable))) THEN
              RAISE EXCEPTION 'Open purchase orders authority is incomplete or invalid.';
            END IF;
          END IF;

          IF EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='dashboards.stores-workload')
             OR to_regprocedure('advance.stores_workload(text,uuid,uuid[],text,text,uuid,bigint,integer)') IS NOT NULL THEN
            IF NOT EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='dashboards.stores-workload')
              OR NOT EXISTS(SELECT 1 FROM pg_proc p
                WHERE p.oid=to_regprocedure('advance.stores_workload(text,uuid,uuid[],text,text,uuid,bigint,integer)')
                  AND p.prosecdef AND p.provolatile='s' AND p.proowner='nexa_erp_owner'::regrole
                  AND p.proconfig=ARRAY['search_path=pg_catalog, advance']
                  AND has_function_privilege('nexa_erp_runtime',p.oid,'EXECUTE')
                  AND NOT EXISTS(SELECT 1 FROM aclexplode(coalesce(p.proacl,acldefault('f',p.proowner))) a
                    WHERE a.grantee<>p.proowner AND(a.grantee<>'nexa_erp_runtime'::regrole OR a.is_grantable))) THEN
              RAISE EXCEPTION 'Stores document workload authority is incomplete or invalid.';
            END IF;
          END IF;

          IF EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='dashboards.stores-qc-stock')
             OR to_regprocedure('advance.stores_qc_stock(text,uuid,uuid[],text,text,uuid,bigint,integer)') IS NOT NULL THEN
            IF NOT EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='dashboards.stores-qc-stock')
              OR NOT EXISTS(SELECT 1 FROM pg_proc p
                WHERE p.oid=to_regprocedure('advance.stores_qc_stock(text,uuid,uuid[],text,text,uuid,bigint,integer)')
                  AND p.prosecdef AND p.provolatile='s' AND p.proowner='nexa_erp_owner'::regrole
                  AND p.proconfig=ARRAY['search_path=pg_catalog, advance']
                  AND has_function_privilege('nexa_erp_runtime',p.oid,'EXECUTE')
                  AND NOT EXISTS(SELECT 1 FROM aclexplode(coalesce(p.proacl,acldefault('f',p.proowner))) a
                    WHERE a.grantee<>p.proowner AND(a.grantee<>'nexa_erp_runtime'::regrole OR a.is_grantable))) THEN
              RAISE EXCEPTION 'Stores QC stock authority is incomplete or invalid.';
            END IF;
          END IF;

          IF to_regprocedure('advance.confirm_component_fitment(uuid,uuid,uuid,numeric,timestamptz,text,uuid,text,text,text,uuid,text,uuid,text,text)') IS NOT NULL THEN
            IF to_regprocedure('advance.reverse_component_fitment(uuid,uuid,text,text,text,text,uuid,text,uuid,text,text)') IS NULL
               OR NOT has_function_privilege('nexa_erp_runtime','advance.confirm_component_fitment(uuid,uuid,uuid,numeric,timestamptz,text,uuid,text,text,text,uuid,text,uuid,text,text)','EXECUTE')
               OR NOT has_function_privilege('nexa_erp_runtime','advance.reverse_component_fitment(uuid,uuid,text,text,text,text,uuid,text,uuid,text,text)','EXECUTE')
               OR has_function_privilege('nexa_erp_bootstrap','advance.confirm_component_fitment(uuid,uuid,uuid,numeric,timestamptz,text,uuid,text,text,text,uuid,text,uuid,text,text)','EXECUTE')
               OR has_function_privilege('nexa_erp_migration','advance.confirm_component_fitment(uuid,uuid,uuid,numeric,timestamptz,text,uuid,text,text,text,uuid,text,uuid,text,text)','EXECUTE')
               OR EXISTS (SELECT 1 FROM (VALUES ('advance.component_fitments'),('advance.component_fitment_reversals'),('advance.actual_boms'),('advance.actual_bom_entries')) evidence(name)
                  WHERE has_table_privilege('nexa_erp_runtime',evidence.name,'INSERT')
                     OR has_table_privilege('nexa_erp_runtime',evidence.name,'UPDATE')
                     OR has_table_privilege('nexa_erp_runtime',evidence.name,'DELETE')) THEN
              RAISE EXCEPTION 'Fitment controlled function or evidence-table ACL is invalid.';
            END IF;
          ELSIF to_regprocedure('advance.reverse_component_fitment(uuid,uuid,text,text,text,text,uuid,text,uuid,text,text)') IS NOT NULL
             OR to_regclass('advance.component_fitments') IS NOT NULL THEN
            RAISE EXCEPTION 'Fitment security boundary is partially installed.';
          END IF;
          IF to_regprocedure('advance.create_fat_custody_explanation(uuid,uuid,uuid,numeric,text,text,text,text,text,uuid,text,uuid,text,text)') IS NOT NULL THEN
            IF to_regprocedure('advance.reconcile_job_order_fat(uuid,uuid,text,text,text,text,uuid,text,uuid,text,text)') IS NULL
               OR EXISTS (SELECT 1 FROM (VALUES ('advance.job_order_fat_custody_explanations'),
                    ('advance.job_order_fat_reconciliations'),('advance.job_order_fat_reconciliation_lines')) evidence(name)
                  WHERE has_table_privilege('nexa_erp_runtime',evidence.name,'INSERT')
                     OR has_table_privilege('nexa_erp_runtime',evidence.name,'UPDATE')
                     OR has_table_privilege('nexa_erp_runtime',evidence.name,'DELETE'))
               OR NOT has_function_privilege('nexa_erp_runtime','advance.create_fat_custody_explanation(uuid,uuid,uuid,numeric,text,text,text,text,text,uuid,text,uuid,text,text)','EXECUTE')
               OR NOT has_function_privilege('nexa_erp_runtime','advance.reconcile_job_order_fat(uuid,uuid,text,text,text,text,uuid,text,uuid,text,text)','EXECUTE')
               OR has_function_privilege('nexa_erp_runtime','advance.fat_authority_valid(uuid,uuid,text,uuid,text,text[])','EXECUTE')
               OR has_function_privilege('nexa_erp_runtime','advance.fat_live_balances(uuid,uuid)','EXECUTE') THEN
              RAISE EXCEPTION 'FAT readiness EXECUTE-only authority is invalid.';
            END IF;
          ELSIF to_regprocedure('advance.reconcile_job_order_fat(uuid,uuid,text,text,text,text,uuid,text,uuid,text,text)') IS NOT NULL
             OR to_regclass('advance.job_order_fat_reconciliations') IS NOT NULL THEN
            RAISE EXCEPTION 'FAT readiness authority is partially installed.';
          END IF;
          IF to_regprocedure('advance.create_vendor_bill(uuid,uuid,text,date,jsonb,text,text,text,uuid,text,uuid,text,text)') IS NOT NULL THEN
            IF EXISTS (
              SELECT 1 FROM (VALUES ('advance.vendor_bills'),('advance.vendor_bill_lines'),('advance.vendor_bill_history'),('advance.vendor_bill_cost_allocations'),('advance.fifo_inventory_cost_layers'),('advance.fifo_cost_consumptions')) evidence(name)
              WHERE has_table_privilege('nexa_erp_runtime',evidence.name,'SELECT')
                 OR has_table_privilege('nexa_erp_runtime',evidence.name,'INSERT')
                 OR has_table_privilege('nexa_erp_runtime',evidence.name,'UPDATE')
                 OR has_table_privilege('nexa_erp_runtime',evidence.name,'DELETE')) THEN
              RAISE EXCEPTION 'Runtime must have no Vendor Bill/FIFO table privileges.';
            END IF;
            IF NOT has_function_privilege('nexa_erp_runtime','advance.create_vendor_bill(uuid,uuid,text,date,jsonb,text,text,text,uuid,text,uuid,text,text)','EXECUTE')
               OR NOT has_function_privilege('nexa_erp_runtime','advance.decide_vendor_bill(uuid,uuid,bigint,boolean,text,text,text,text,uuid,text,uuid,text,text)','EXECUTE')
               OR NOT has_function_privilege('nexa_erp_runtime','advance.reverse_vendor_bill(uuid,uuid,bigint,text,text,text,text,uuid,text,uuid,text,text)','EXECUTE')
               OR NOT has_function_privilege('nexa_erp_runtime','advance.create_fifo_layers_for_grn(uuid,uuid,uuid,text,uuid,text,text)','EXECUTE')
               OR NOT has_function_privilege('nexa_erp_runtime','advance.consume_fifo_for_issue(uuid,uuid,uuid,text,uuid,text,text)','EXECUTE')
               OR NOT has_function_privilege('nexa_erp_runtime','advance.get_vendor_bill(uuid,uuid)','EXECUTE')
               OR NOT has_function_privilege('nexa_erp_runtime','advance.list_vendor_bills(uuid,text,text,uuid,integer,integer)','EXECUTE')
               OR has_function_privilege('nexa_erp_bootstrap','advance.create_vendor_bill(uuid,uuid,text,date,jsonb,text,text,text,uuid,text,uuid,text,text)','EXECUTE')
               OR has_function_privilege('nexa_erp_migration','advance.create_vendor_bill(uuid,uuid,text,date,jsonb,text,text,text,uuid,text,uuid,text,text)','EXECUTE') THEN
              RAISE EXCEPTION 'Vendor Bill/FIFO controlled function ACL is invalid.';
            END IF;
          ELSIF to_regprocedure('advance.decide_vendor_bill(uuid,uuid,bigint,boolean,text,text,text,text,uuid,text,uuid,text,text)') IS NOT NULL
             OR to_regprocedure('advance.reverse_vendor_bill(uuid,uuid,bigint,text,text,text,text,uuid,text,uuid,text,text)') IS NOT NULL
             OR to_regclass('advance.vendor_bills') IS NOT NULL OR to_regclass('advance.fifo_inventory_cost_layers') IS NOT NULL THEN
            RAISE EXCEPTION 'Vendor Bill/FIFO security boundary is partially installed.';
          END IF;
          IF to_regprocedure('advance.record_vendor_bill_charges(uuid,uuid,jsonb,jsonb,uuid,text,uuid,text,text)') IS NOT NULL
             AND to_regprocedure('advance.get_actual_bom_landed_valuations(uuid,uuid)') IS NOT NULL THEN
            IF EXISTS (
              SELECT 1 FROM (VALUES ('advance.vendor_bill_charges'),
                ('advance.vendor_bill_charge_allocations'),
                ('advance.fifo_landed_cost_adjustments'),
                ('advance.actual_bom_valuation_adjustments')) evidence(name)
              WHERE has_table_privilege('nexa_erp_runtime',evidence.name,'SELECT')
                 OR has_table_privilege('nexa_erp_runtime',evidence.name,'INSERT')
                 OR has_table_privilege('nexa_erp_runtime',evidence.name,'UPDATE')
                 OR has_table_privilege('nexa_erp_runtime',evidence.name,'DELETE'))
               OR NOT has_function_privilege('nexa_erp_runtime',
                 'advance.record_vendor_bill_charges(uuid,uuid,jsonb,jsonb,uuid,text,uuid,text,text)',
                 'EXECUTE')
               OR has_function_privilege('nexa_erp_bootstrap',
                 'advance.record_vendor_bill_charges(uuid,uuid,jsonb,jsonb,uuid,text,uuid,text,text)',
                 'EXECUTE')
               OR has_function_privilege('nexa_erp_migration',
                 'advance.record_vendor_bill_charges(uuid,uuid,jsonb,jsonb,uuid,text,uuid,text,text)',
                 'EXECUTE')
               OR NOT has_function_privilege('nexa_erp_runtime','advance.get_actual_bom_landed_valuations(uuid,uuid)','EXECUTE')
               OR has_function_privilege('nexa_erp_bootstrap','advance.get_actual_bom_landed_valuations(uuid,uuid)','EXECUTE')
               OR has_function_privilege('nexa_erp_migration','advance.get_actual_bom_landed_valuations(uuid,uuid)','EXECUTE') THEN
              RAISE EXCEPTION 'Immutable landed-cost EXECUTE-only authority is invalid.';
            END IF;
          ELSIF to_regprocedure('advance.record_vendor_bill_charges(uuid,uuid,jsonb,jsonb,uuid,text,uuid,text,text)') IS NOT NULL
             OR to_regprocedure('advance.get_actual_bom_landed_valuations(uuid,uuid)') IS NOT NULL
             OR to_regclass('advance.vendor_bill_charges') IS NOT NULL
             OR to_regclass('advance.vendor_bill_charge_allocations') IS NOT NULL
             OR to_regclass('advance.fifo_landed_cost_adjustments') IS NOT NULL
             OR to_regclass('advance.actual_bom_valuation_adjustments') IS NOT NULL THEN
            RAISE EXCEPTION 'Immutable landed-cost authority is partially installed.';
          END IF;
          IF to_regprocedure('advance.record_vendor_advance(uuid,uuid,date,numeric,text,text,text,text,text,uuid,text,uuid,text,text)') IS NOT NULL
             AND to_regprocedure('advance.record_vendor_payment(uuid,uuid,date,numeric,text,text,text,jsonb,text,text,uuid,text,uuid,text,text)') IS NOT NULL
             AND to_regprocedure('advance.reverse_vendor_advance(uuid,uuid,text,text,text,uuid,text,uuid,text,text)') IS NOT NULL
             AND to_regprocedure('advance.list_vendor_advance_purchase_orders(uuid,uuid)') IS NOT NULL
             AND to_regprocedure('advance.list_vendor_advances(uuid,uuid,uuid,boolean,integer,integer)') IS NOT NULL
             AND to_regprocedure('advance.list_vendor_payments(uuid,uuid,integer,integer)') IS NOT NULL
             AND to_regprocedure('advance.list_vendor_payables(uuid,uuid,boolean)') IS NOT NULL
             AND to_regprocedure('advance.list_vendor_positions(uuid)') IS NOT NULL
             AND to_regclass('advance.vendor_advances') IS NOT NULL
             AND to_regclass('advance.vendor_advance_reversals') IS NOT NULL
             AND to_regclass('advance.vendor_advance_adjustments') IS NOT NULL
             AND to_regclass('advance.vendor_advance_adjustment_restorations') IS NOT NULL
             AND to_regclass('advance.vendor_payments') IS NOT NULL
             AND to_regclass('advance.vendor_payment_allocations') IS NOT NULL THEN
            IF EXISTS (
              SELECT 1 FROM (VALUES
                ('advance.vendor_advances'),('advance.vendor_advance_reversals'),
                ('advance.vendor_advance_adjustments'),
                ('advance.vendor_advance_adjustment_restorations'),
                ('advance.vendor_payments'),('advance.vendor_payment_allocations')) evidence(name)
              WHERE has_table_privilege('nexa_erp_runtime',evidence.name,'SELECT')
                 OR has_table_privilege('nexa_erp_runtime',evidence.name,'INSERT')
                 OR has_table_privilege('nexa_erp_runtime',evidence.name,'UPDATE')
                 OR has_table_privilege('nexa_erp_runtime',evidence.name,'DELETE'))
               OR NOT has_function_privilege('nexa_erp_runtime',
                 'advance.record_vendor_advance(uuid,uuid,date,numeric,text,text,text,text,text,uuid,text,uuid,text,text)','EXECUTE')
               OR NOT has_function_privilege('nexa_erp_runtime',
                 'advance.reverse_vendor_advance(uuid,uuid,text,text,text,uuid,text,uuid,text,text)','EXECUTE')
               OR NOT has_function_privilege('nexa_erp_runtime',
                 'advance.record_vendor_payment(uuid,uuid,date,numeric,text,text,text,jsonb,text,text,uuid,text,uuid,text,text)','EXECUTE')
               OR NOT has_function_privilege('nexa_erp_runtime',
                 'advance.list_vendor_advance_purchase_orders(uuid,uuid)','EXECUTE')
               OR has_function_privilege('nexa_erp_bootstrap',
                 'advance.record_vendor_advance(uuid,uuid,date,numeric,text,text,text,text,text,uuid,text,uuid,text,text)','EXECUTE')
               OR has_function_privilege('nexa_erp_migration',
                 'advance.record_vendor_advance(uuid,uuid,date,numeric,text,text,text,text,text,uuid,text,uuid,text,text)','EXECUTE') THEN
              RAISE EXCEPTION 'Vendor advance/payment EXECUTE-only authority is invalid.';
            END IF;
          ELSIF to_regprocedure('advance.record_vendor_advance(uuid,uuid,date,numeric,text,text,text,text,text,uuid,text,uuid,text,text)') IS NOT NULL
             OR to_regprocedure('advance.record_vendor_payment(uuid,uuid,date,numeric,text,text,text,jsonb,text,text,uuid,text,uuid,text,text)') IS NOT NULL
             OR to_regprocedure('advance.list_vendor_advance_purchase_orders(uuid,uuid)') IS NOT NULL
             OR to_regprocedure('advance.reverse_vendor_advance(uuid,uuid,text,text,text,uuid,text,uuid,text,text)') IS NOT NULL
             OR to_regprocedure('advance.list_vendor_advances(uuid,uuid,uuid,boolean,integer,integer)') IS NOT NULL
             OR to_regprocedure('advance.list_vendor_payments(uuid,uuid,integer,integer)') IS NOT NULL
             OR to_regprocedure('advance.list_vendor_payables(uuid,uuid,boolean)') IS NOT NULL
             OR to_regprocedure('advance.list_vendor_positions(uuid)') IS NOT NULL
             OR to_regclass('advance.vendor_advance_reversals') IS NOT NULL
             OR to_regclass('advance.vendor_advance_adjustments') IS NOT NULL
             OR to_regclass('advance.vendor_advance_adjustment_restorations') IS NOT NULL
             OR to_regclass('advance.vendor_payment_allocations') IS NOT NULL
             OR to_regclass('advance.vendor_advances') IS NOT NULL
             OR to_regclass('advance.vendor_payments') IS NOT NULL THEN
            RAISE EXCEPTION 'Vendor advance/payment security boundary is partially installed.';
          END IF;
          IF to_regclass('advance.vendor_bank_advices') IS NOT NULL OR EXISTS(SELECT 1 FROM (VALUES ('advance.record_vendor_bank_advice(uuid,uuid,text,text,bytea,text,text,text,uuid,text,uuid,text,text)'),('advance.vendor_bank_advice_json(uuid,uuid,boolean)'),('advance.vendor_bank_advice_content(uuid,uuid)'),('advance.require_vendor_bank_advice(uuid,uuid,text)')) f(name) WHERE to_regprocedure(f.name) IS NOT NULL) THEN
            IF to_regclass('advance.vendor_bank_advices') IS NULL OR EXISTS(SELECT 1 FROM (VALUES ('advance.record_vendor_bank_advice(uuid,uuid,text,text,bytea,text,text,text,uuid,text,uuid,text,text)'),('advance.vendor_bank_advice_json(uuid,uuid,boolean)'),('advance.vendor_bank_advice_content(uuid,uuid)'),('advance.require_vendor_bank_advice(uuid,uuid,text)')) f(name) WHERE to_regprocedure(f.name) IS NULL) THEN
              RAISE EXCEPTION 'Bank advice authority is partially installed.';
            END IF;
            IF has_table_privilege('nexa_erp_runtime','advance.vendor_bank_advices','SELECT')
               OR has_table_privilege('nexa_erp_runtime','advance.vendor_bank_advices','INSERT')
               OR has_table_privilege('nexa_erp_runtime','advance.vendor_bank_advices','UPDATE')
               OR has_table_privilege('nexa_erp_runtime','advance.vendor_bank_advices','DELETE')
               OR has_function_privilege('nexa_erp_runtime','advance.require_vendor_bank_advice(uuid,uuid,text)','EXECUTE')
               OR EXISTS(SELECT 1 FROM (VALUES ('advance.record_vendor_bank_advice(uuid,uuid,text,text,bytea,text,text,text,uuid,text,uuid,text,text)'),('advance.vendor_bank_advice_json(uuid,uuid,boolean)'),('advance.vendor_bank_advice_content(uuid,uuid)')) f(name)
                 WHERE NOT has_function_privilege('nexa_erp_runtime',f.name,'EXECUTE')
                    OR has_function_privilege('nexa_erp_bootstrap',f.name,'EXECUTE')
                    OR has_function_privilege('nexa_erp_migration',f.name,'EXECUTE'))
               OR EXISTS(SELECT 1 FROM pg_proc p CROSS JOIN LATERAL aclexplode(coalesce(p.proacl,acldefault('f',p.proowner))) a
                 WHERE p.oid IN(SELECT to_regprocedure(f.name) FROM (VALUES ('advance.record_vendor_bank_advice(uuid,uuid,text,text,bytea,text,text,text,uuid,text,uuid,text,text)'),('advance.vendor_bank_advice_json(uuid,uuid,boolean)'),('advance.vendor_bank_advice_content(uuid,uuid)'),('advance.require_vendor_bank_advice(uuid,uuid,text)')) f(name)) AND a.grantee=0) THEN
              RAISE EXCEPTION 'Bank advice EXECUTE-only authority is invalid.';
            END IF;
          END IF;

          IF EXISTS(SELECT 1 FROM (VALUES ('advance.vendor_po_cash_totals(uuid,uuid,text,uuid)'),('advance.require_vendor_po_cash_limit(uuid,uuid,numeric,text)'),('advance.guard_purchase_order_vendor_cash()')) f(name) WHERE to_regprocedure(f.name) IS NOT NULL)
             OR to_regclass('advance."IX_vendor_advances_company_po"') IS NOT NULL THEN
            IF EXISTS(SELECT 1 FROM (VALUES ('advance.vendor_po_cash_totals(uuid,uuid,text,uuid)'),('advance.require_vendor_po_cash_limit(uuid,uuid,numeric,text)'),('advance.guard_purchase_order_vendor_cash()')) f(name)
              LEFT JOIN pg_proc p ON p.oid=to_regprocedure(f.name)
              WHERE p.oid IS NULL OR NOT p.prosecdef OR p.proowner<>'nexa_erp_owner'::regrole
                OR p.proconfig IS DISTINCT FROM ARRAY['search_path=pg_catalog, advance']
                OR has_function_privilege('nexa_erp_runtime',p.oid,'EXECUTE')
                OR has_function_privilege('nexa_erp_bootstrap',p.oid,'EXECUTE')
                OR has_function_privilege('nexa_erp_migration',p.oid,'EXECUTE')
                OR EXISTS(SELECT 1 FROM aclexplode(coalesce(p.proacl,acldefault('f',p.proowner))) a
                  WHERE a.grantee<>p.proowner))
              OR to_regclass('advance."IX_vendor_advances_company_po"') IS NULL
              OR NOT EXISTS(SELECT 1 FROM pg_trigger WHERE tgrelid='advance.purchase_orders'::regclass
                AND tgname='trg_purchase_order_vendor_cash' AND tgenabled='O' AND tgtype=23
                    AND tgqual IS NULL AND tgnargs=0 AND tgattr=''::int2vector AND tgconstraint=0
                AND tgfoid=to_regprocedure('advance.guard_purchase_order_vendor_cash()')) THEN
              RAISE EXCEPTION 'Private vendor cash authority or issuance protection is invalid.';
            END IF;
          END IF;

          IF EXISTS(SELECT 1 FROM (VALUES ('advance.stage_opening_stock_import_line(uuid,text,uuid,uuid,uuid,text,text,numeric,numeric,uuid,text,uuid,text,text)'),('advance.record_opening_stock_count(uuid,uuid,date,date,text,text,text,uuid,text,uuid,text,text)'),('advance.confirm_opening_stock_value(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text)'),('advance.authorize_opening_stock(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text)')) f(name) WHERE to_regprocedure(f.name) IS NOT NULL)
             OR EXISTS(SELECT 1 FROM (VALUES ('advance.opening_stock_import_staging_lines'),('advance.opening_stocks'),('advance.opening_stock_lines'),('advance.opening_stock_events')) e(name) WHERE to_regclass(e.name) IS NOT NULL) THEN
            IF EXISTS(SELECT 1 FROM (VALUES ('advance.stage_opening_stock_import_line(uuid,text,uuid,uuid,uuid,text,text,numeric,numeric,uuid,text,uuid,text,text)'),('advance.record_opening_stock_count(uuid,uuid,date,date,text,text,text,uuid,text,uuid,text,text)'),('advance.confirm_opening_stock_value(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text)'),('advance.authorize_opening_stock(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text)')) f(name)
                LEFT JOIN pg_proc p ON p.oid=to_regprocedure(f.name)
                LEFT JOIN pg_roles owner_role ON owner_role.oid=p.proowner
                WHERE p.oid IS NULL OR NOT p.prosecdef OR owner_role.rolname<>'nexa_erp_owner'
                   OR NOT coalesce(p.proconfig @> ARRAY['search_path=pg_catalog, advance'],false)
                   OR NOT has_function_privilege('nexa_erp_runtime',p.oid,'EXECUTE')
                   OR has_function_privilege('nexa_erp_bootstrap',p.oid,'EXECUTE')
                   OR has_function_privilege('nexa_erp_migration',p.oid,'EXECUTE'))
               OR EXISTS(SELECT 1 FROM (VALUES ('advance.opening_stock_import_staging_lines'),('advance.opening_stocks'),('advance.opening_stock_lines'),('advance.opening_stock_events')) e(name)
                WHERE to_regclass(e.name) IS NULL
                   OR NOT has_table_privilege('nexa_erp_runtime',to_regclass(e.name),'SELECT')
                   OR has_table_privilege('nexa_erp_runtime',to_regclass(e.name),'INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER'))
               OR to_regprocedure('advance.guard_opening_stock_evidence()') IS NULL
               OR to_regprocedure('advance.opening_stock_command_valid(uuid,uuid,text,uuid,text,text,text)') IS NULL
               OR has_function_privilege('nexa_erp_runtime',to_regprocedure('advance.guard_opening_stock_evidence()'),'EXECUTE')
               OR has_function_privilege('nexa_erp_runtime',to_regprocedure('advance.opening_stock_command_valid(uuid,uuid,text,uuid,text,text,text)'),'EXECUTE') THEN
              RAISE EXCEPTION 'Opening Stock controlled commands or read-only evidence ACL is invalid.';
            END IF;
          END IF;
          IF to_regclass('advance.item_company_last_purchases') IS NOT NULL
             AND (NOT has_table_privilege('nexa_erp_runtime','advance.item_company_last_purchases','SELECT')
                  OR has_table_privilege('nexa_erp_runtime','advance.item_company_last_purchases','INSERT')
                  OR has_table_privilege('nexa_erp_runtime','advance.item_company_last_purchases','UPDATE')
                  OR has_table_privilege('nexa_erp_runtime','advance.item_company_last_purchases','DELETE')) THEN
            RAISE EXCEPTION 'Runtime item last-purchase cache must be read-only.';
          END IF;
          IF to_regprocedure('advance.register_command_request(text,text,bytea,bytea,uuid,text,text,text,uuid)') IS NOT NULL THEN
            IF to_regprocedure('advance.commit_command_receipt(uuid,bytea,jsonb,uuid)') IS NULL
               OR to_regclass('advance.command_requests') IS NULL
               OR to_regclass('advance.command_receipts') IS NULL THEN
              RAISE EXCEPTION 'Ordinary command ledger is partially installed.';
            END IF;
            IF EXISTS (
              SELECT 1
              FROM (VALUES ('nexa_erp_runtime'),('nexa_erp_bootstrap'),('nexa_erp_migration')) principal(name)
              CROSS JOIN (VALUES ('advance.command_requests'),('advance.command_receipts')) ledger(name)
              WHERE has_table_privilege(principal.name,ledger.name,'SELECT')
                 OR has_table_privilege(principal.name,ledger.name,'INSERT')
                 OR has_table_privilege(principal.name,ledger.name,'UPDATE')
                 OR has_table_privilege(principal.name,ledger.name,'DELETE')) THEN
              RAISE EXCEPTION 'Managed LOGIN principals must have no command-ledger table access.';
            END IF;
            IF NOT has_function_privilege('nexa_erp_runtime','advance.register_command_request(text,text,bytea,bytea,uuid,text,text,text,uuid)','EXECUTE')
               OR NOT has_function_privilege('nexa_erp_runtime','advance.commit_command_receipt(uuid,bytea,jsonb,uuid)','EXECUTE')
               OR NOT has_function_privilege('nexa_erp_runtime','advance.ordinary_command_context_valid(text,uuid,text,text,text)','EXECUTE')
               OR NOT has_function_privilege('nexa_erp_runtime','advance.ordinary_claim_command_context(text,uuid,text,uuid,text,bigint,text,text,text,text)','EXECUTE')
               OR has_function_privilege('nexa_erp_bootstrap','advance.register_command_request(text,text,bytea,bytea,uuid,text,text,text,uuid)','EXECUTE')
               OR has_function_privilege('nexa_erp_bootstrap','advance.commit_command_receipt(uuid,bytea,jsonb,uuid)','EXECUTE')
               OR has_function_privilege('nexa_erp_migration','advance.register_command_request(text,text,bytea,bytea,uuid,text,text,text,uuid)','EXECUTE')
               OR has_function_privilege('nexa_erp_migration','advance.commit_command_receipt(uuid,bytea,jsonb,uuid)','EXECUTE')
               OR has_function_privilege('nexa_erp_bootstrap','advance.ordinary_command_context_valid(text,uuid,text,text,text)','EXECUTE')
               OR has_function_privilege('nexa_erp_bootstrap','advance.ordinary_claim_command_context(text,uuid,text,uuid,text,bigint,text,text,text,text)','EXECUTE')
               OR has_function_privilege('nexa_erp_migration','advance.ordinary_command_context_valid(text,uuid,text,text,text)','EXECUTE')
               OR has_function_privilege('nexa_erp_migration','advance.ordinary_claim_command_context(text,uuid,text,uuid,text,bigint,text,text,text,text)','EXECUTE') THEN
              RAISE EXCEPTION 'Ordinary command-ledger function ACL is invalid.';
            END IF;
            IF to_regprocedure('advance.read_command_receipt(uuid)') IS NOT NULL AND (
              NOT has_function_privilege('nexa_erp_runtime','advance.read_command_receipt(uuid)','EXECUTE')
              OR has_function_privilege('nexa_erp_bootstrap','advance.read_command_receipt(uuid)','EXECUTE')
              OR has_function_privilege('nexa_erp_migration','advance.read_command_receipt(uuid)','EXECUTE')
              OR NOT EXISTS(SELECT 1 FROM pg_proc p
                WHERE p.oid=to_regprocedure('advance.read_command_receipt(uuid)')
                  AND p.prosecdef AND p.proowner='nexa_erp_owner'::regrole
                  AND p.prorettype='jsonb'::regtype AND array_length(p.proconfig,1)=1
                  AND EXISTS(SELECT 1 FROM unnest(p.proconfig) setting
                    WHERE regexp_replace(setting,'[[:space:]]','','g')='search_path=pg_catalog,advance')
                  AND NOT EXISTS(SELECT 1 FROM aclexplode(coalesce(p.proacl,acldefault('f',p.proowner))) a
                    WHERE a.grantee NOT IN (p.proowner,'nexa_erp_runtime'::regrole) AND a.privilege_type='EXECUTE'))) THEN
              RAISE EXCEPTION 'Ordinary receipt replay function owner, search path or ACL is invalid.';
            END IF;
          ELSIF to_regprocedure('advance.commit_command_receipt(uuid,bytea,jsonb,uuid)') IS NOT NULL
             OR to_regclass('advance.command_requests') IS NOT NULL
             OR to_regclass('advance.command_receipts') IS NOT NULL THEN
            RAISE EXCEPTION 'Ordinary command ledger is partially installed.';
          END IF;
        END $verify$;
        """ + SESS.NexaERP.Database.IntercompanyRoutes20260915AccessSql.Verify + SESS.NexaERP.Database.IntercompanyPurchase20260915AccessSql.Verify;

    internal const string RoleStatus = """
        WITH managed("Ordinal","RoleName") AS (VALUES
          (1,'nexa_erp_owner'),(2,'nexa_erp_migration'),(3,'nexa_erp_bootstrap'),(4,'nexa_erp_runtime')),
        ceremony AS (
          SELECT p.oid,p.proacl,p.proowner
          FROM pg_catalog.pg_proc p
          WHERE p.oid=to_regprocedure('advance.complete_authentication_bootstrap(text,text)'))
        SELECT m."RoleName",r.oid IS NOT NULL,
               r.rolcanlogin,r.rolsuper,r.rolcreatedb,r.rolcreaterole,r.rolreplication,r.rolbypassrls,
               c.oid IS NOT NULL,
               CASE WHEN r.oid IS NULL OR c.oid IS NULL THEN NULL ELSE EXISTS(
                 SELECT 1 FROM pg_catalog.aclexplode(COALESCE(c.proacl,pg_catalog.acldefault('f',c.proowner))) acl
                 WHERE acl.grantee=r.oid AND acl.privilege_type='EXECUTE') END
        FROM managed m
        LEFT JOIN pg_catalog.pg_roles r ON r.rolname=m."RoleName"
        LEFT JOIN ceremony c ON true
        ORDER BY m."Ordinal";
        """;
}
