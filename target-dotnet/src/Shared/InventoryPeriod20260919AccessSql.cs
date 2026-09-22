namespace SESS.NexaERP.Database;

internal static class InventoryPeriod20260919AccessSql
{
    internal const string Provision = """
        DO $period_acl$ DECLARE signature text; principal text; is_entry boolean;
        BEGIN
         IF to_regclass('advance.inventory_period_events') IS NOT NULL THEN
          REVOKE ALL ON TABLE advance.inventory_period_events FROM PUBLIC;
          IF to_regrole('nexa_erp_owner') IS NOT NULL THEN ALTER TABLE advance.inventory_period_events OWNER TO nexa_erp_owner; END IF;
          FOREACH principal IN ARRAY ARRAY['nexa_erp_runtime','nexa_erp_bootstrap','nexa_erp_migration'] LOOP
           IF to_regrole(principal) IS NOT NULL THEN EXECUTE format('REVOKE ALL ON TABLE advance.inventory_period_events FROM %I',principal); END IF;
          END LOOP;
          FOREACH signature IN ARRAY ARRAY[
           'advance.guard_inventory_period_evidence()',
           'advance.require_inventory_period_authority(uuid,uuid,uuid,text,uuid,text,text)',
           'advance.open_inventory_period(uuid,uuid,text,text,date,date,text,uuid,text,uuid,text,text)',
           'advance.close_inventory_period(uuid,uuid,uuid,bigint,text,uuid,text,uuid,text,text)',
           'advance.inventory_period_json(uuid,uuid)',
           'advance.inventory_periods_json(uuid)'] LOOP
           EXECUTE format('REVOKE ALL ON FUNCTION %s FROM PUBLIC',signature);
           IF to_regrole('nexa_erp_owner') IS NOT NULL THEN EXECUTE format('ALTER FUNCTION %s OWNER TO nexa_erp_owner',signature); END IF;
           FOREACH principal IN ARRAY ARRAY['nexa_erp_runtime','nexa_erp_bootstrap','nexa_erp_migration'] LOOP
            IF to_regrole(principal) IS NOT NULL THEN EXECUTE format('REVOKE ALL ON FUNCTION %s FROM %I',signature,principal); END IF;
           END LOOP;
           is_entry:=signature NOT IN ('advance.guard_inventory_period_evidence()',
            'advance.require_inventory_period_authority(uuid,uuid,uuid,text,uuid,text,text)');
           IF is_entry AND to_regrole('nexa_erp_runtime') IS NOT NULL THEN
            EXECUTE format('GRANT EXECUTE ON FUNCTION %s TO nexa_erp_runtime',signature);
           END IF;
          END LOOP;
         END IF;
        END $period_acl$;
        """;

    internal const string Verify = """
        DO $period_verify$ DECLARE signature text; object_id oid; is_entry boolean;
        BEGIN
         IF to_regclass('advance.inventory_period_events') IS NOT NULL THEN
          object_id:=to_regclass('advance.inventory_period_events');
          IF NOT EXISTS(SELECT 1 FROM pg_class c WHERE c.oid=object_id AND c.relowner='nexa_erp_owner'::regrole
           AND NOT EXISTS(SELECT 1 FROM aclexplode(coalesce(c.relacl,acldefault('r',c.relowner))) a WHERE a.grantee<>c.relowner))
          THEN RAISE EXCEPTION 'Inventory period evidence table owner or direct ACL is invalid.'; END IF;
          FOREACH signature IN ARRAY ARRAY[
           'advance.guard_inventory_period_evidence()',
           'advance.require_inventory_period_authority(uuid,uuid,uuid,text,uuid,text,text)',
           'advance.open_inventory_period(uuid,uuid,text,text,date,date,text,uuid,text,uuid,text,text)',
           'advance.close_inventory_period(uuid,uuid,uuid,bigint,text,uuid,text,uuid,text,text)',
           'advance.inventory_period_json(uuid,uuid)',
           'advance.inventory_periods_json(uuid)'] LOOP
           object_id:=to_regprocedure(signature);
           is_entry:=signature NOT IN ('advance.guard_inventory_period_evidence()',
            'advance.require_inventory_period_authority(uuid,uuid,uuid,text,uuid,text,text)');
           IF object_id IS NULL OR NOT EXISTS(SELECT 1 FROM pg_proc p WHERE p.oid=object_id
            AND p.proowner='nexa_erp_owner'::regrole AND p.prosecdef=is_entry
            AND array_length(p.proconfig,1)=1 AND EXISTS(SELECT 1 FROM unnest(p.proconfig) setting
             WHERE regexp_replace(setting,'[[:space:]]','','g')='search_path=pg_catalog,advance')
            AND NOT EXISTS(SELECT 1 FROM aclexplode(coalesce(p.proacl,acldefault('f',p.proowner))) a
             WHERE a.grantee<>p.proowner AND (NOT is_entry OR a.grantee<>'nexa_erp_runtime'::regrole OR a.is_grantable)))
            OR has_function_privilege('nexa_erp_runtime',object_id,'EXECUTE') IS DISTINCT FROM is_entry
           THEN RAISE EXCEPTION 'Inventory period function owner, search path or ACL is invalid: %',signature; END IF;
          END LOOP;
          IF (SELECT count(*) FROM pg_trigger t WHERE NOT t.tgisinternal AND t.tgenabled='O'
           AND t.tgfoid='advance.guard_inventory_period_evidence()'::regprocedure
           AND ((t.tgrelid='advance.financial_periods'::regclass AND t.tgname='trg_inventory_period_guard')
            OR (t.tgrelid='advance.inventory_period_events'::regclass AND t.tgname='trg_inventory_period_event_guard')))<>2
          THEN RAISE EXCEPTION 'Inventory period evidence guards are missing or disabled.'; END IF;
         ELSIF to_regprocedure('advance.guard_inventory_period_evidence()') IS NOT NULL
          OR to_regprocedure('advance.require_inventory_period_authority(uuid,uuid,uuid,text,uuid,text,text)') IS NOT NULL
          OR to_regprocedure('advance.open_inventory_period(uuid,uuid,text,text,date,date,text,uuid,text,uuid,text,text)') IS NOT NULL
          OR to_regprocedure('advance.close_inventory_period(uuid,uuid,uuid,bigint,text,uuid,text,uuid,text,text)') IS NOT NULL
          OR to_regprocedure('advance.inventory_period_json(uuid,uuid)') IS NOT NULL
          OR to_regprocedure('advance.inventory_periods_json(uuid)') IS NOT NULL
         THEN RAISE EXCEPTION 'Inventory period package is partially installed.';
         END IF;
        END $period_verify$;
        """;
}
