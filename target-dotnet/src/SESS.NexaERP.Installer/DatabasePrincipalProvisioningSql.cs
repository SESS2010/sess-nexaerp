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
              AND c.relname NOT IN ('authentication_bootstrap_state','command_requests','command_receipts','vendor_bills','vendor_bill_lines','vendor_bill_history','vendor_bill_cost_allocations','fifo_inventory_cost_layers','fifo_cost_consumptions','component_fitments','component_fitment_reversals','actual_boms','actual_bom_entries','job_order_fat_custody_explanations','job_order_fat_reconciliations','job_order_fat_reconciliation_lines')
          LOOP
            IF item.relkind IN ('v','m') THEN
              EXECUTE format('GRANT SELECT ON TABLE advance.%I TO nexa_erp_runtime',item.relname);
            ELSE
              EXECUTE format('GRANT SELECT,INSERT,UPDATE ON TABLE advance.%I TO nexa_erp_runtime',item.relname);
            END IF;
          END LOOP;
        END $runtime_grants$;
        GRANT USAGE,SELECT ON ALL SEQUENCES IN SCHEMA advance TO nexa_erp_runtime;
        REVOKE ALL ON TABLE advance.authentication_bootstrap_state FROM nexa_erp_runtime,nexa_erp_bootstrap;

        DO $ceremony_acl$
        BEGIN
          IF to_regprocedure('advance.complete_authentication_bootstrap(text,text)') IS NOT NULL THEN
            EXECUTE 'REVOKE ALL ON FUNCTION advance.complete_authentication_bootstrap(text,text) FROM PUBLIC';
            EXECUTE 'REVOKE ALL ON FUNCTION advance.complete_authentication_bootstrap(text,text) FROM nexa_erp_runtime';
            EXECUTE 'REVOKE ALL ON FUNCTION advance.complete_authentication_bootstrap(text,text) FROM nexa_erp_migration';
            EXECUTE 'GRANT EXECUTE ON FUNCTION advance.complete_authentication_bootstrap(text,text) TO nexa_erp_bootstrap';
          END IF;
        END $ceremony_acl$;

        DO $stores_acl$
        BEGIN
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
        """;

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
              AND NOT (c.relname IN ('command_requests','command_receipts')
                       AND to_regprocedure('advance.register_command_request(text,text,bytea,bytea,uuid,text,text,text,uuid)') IS NOT NULL)
              AND NOT (c.relname IN ('stock_posting_batches','stock_movements')
                       AND to_regprocedure('advance.post_stores_stock_batch(uuid,text,uuid,text,text,text,date,uuid,text,jsonb)') IS NOT NULL)
              AND NOT (c.relname IN ('vendor_bills','vendor_bill_lines','vendor_bill_history','vendor_bill_cost_allocations','fifo_inventory_cost_layers','fifo_cost_consumptions')
                       AND to_regprocedure('advance.create_vendor_bill(uuid,uuid,text,date,jsonb,text,text,text,uuid,text,uuid,text,text)') IS NOT NULL)
              AND NOT (c.relname IN ('component_fitments','component_fitment_reversals','actual_boms','actual_bom_entries','job_order_fat_custody_explanations','job_order_fat_reconciliations','job_order_fat_reconciliation_lines')
                       AND to_regprocedure('advance.confirm_component_fitment(uuid,uuid,uuid,numeric,timestamptz,text,uuid,text,text,text,uuid,text,uuid,text,text)') IS NOT NULL)
              AND (NOT has_table_privilege('nexa_erp_runtime',c.oid,'SELECT')
                   OR NOT has_table_privilege('nexa_erp_runtime',c.oid,'INSERT')
                   OR NOT has_table_privilege('nexa_erp_runtime',c.oid,'UPDATE')
                   OR has_table_privilege('nexa_erp_runtime',c.oid,'DELETE'))
          ) THEN RAISE EXCEPTION 'Runtime table privileges differ from SELECT/INSERT/UPDATE without DELETE.'; END IF;
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
          ELSIF to_regprocedure('advance.commit_command_receipt(uuid,bytea,jsonb,uuid)') IS NOT NULL
             OR to_regclass('advance.command_requests') IS NOT NULL
             OR to_regclass('advance.command_receipts') IS NOT NULL THEN
            RAISE EXCEPTION 'Ordinary command ledger is partially installed.';
          END IF;
        END $verify$;
        """;

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
