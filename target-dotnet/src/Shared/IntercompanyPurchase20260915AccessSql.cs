namespace SESS.NexaERP.Database;

// Shared verbatim by this migration and Installer reconciliation; later changes need a new version.
internal static class IntercompanyPurchase20260915AccessSql
{
    internal const string Provision = """
        DO $ic_acl$ DECLARE relation text; signature text; principal text;
        BEGIN
         IF to_regclass('advance.intercompany_purchase_publications') IS NOT NULL THEN
          FOREACH relation IN ARRAY ARRAY['intercompany_purchase_publications','intercompany_purchase_publication_lines'] LOOP
           EXECUTE format('REVOKE ALL ON TABLE advance.%I FROM PUBLIC',relation);
           IF to_regrole('nexa_erp_owner') IS NOT NULL THEN EXECUTE format('ALTER TABLE advance.%I OWNER TO nexa_erp_owner',relation); END IF;
           FOREACH principal IN ARRAY ARRAY['nexa_erp_runtime','nexa_erp_bootstrap','nexa_erp_migration'] LOOP
            IF to_regrole(principal) IS NOT NULL THEN EXECUTE format('REVOKE ALL ON TABLE advance.%I FROM %I',relation,principal); END IF;
           END LOOP;
          END LOOP;
          FOREACH signature IN ARRAY ARRAY[
           'advance.guard_intercompany_purchase_evidence()','advance.intercompany_purchase_scope(uuid,uuid,advance.purchase_orders)',
           'advance.intercompany_purchase_json(uuid,uuid,uuid,text)','advance.intercompany_purchases_page(uuid,uuid,text,integer,integer)',
           'advance.intercompany_purchase_options(uuid,uuid)','advance.publish_intercompany_purchase(uuid,uuid,uuid,uuid,bigint,text,uuid,text,uuid,text,text)'] LOOP
           EXECUTE format('REVOKE ALL ON FUNCTION %s FROM PUBLIC',signature);
           IF to_regrole('nexa_erp_owner') IS NOT NULL THEN EXECUTE format('ALTER FUNCTION %s OWNER TO nexa_erp_owner',signature); END IF;
           FOREACH principal IN ARRAY ARRAY['nexa_erp_runtime','nexa_erp_bootstrap','nexa_erp_migration'] LOOP
            IF to_regrole(principal) IS NOT NULL THEN EXECUTE format('REVOKE ALL ON FUNCTION %s FROM %I',signature,principal); END IF;
           END LOOP;
          END LOOP;
          IF to_regrole('nexa_erp_runtime') IS NOT NULL THEN
           GRANT EXECUTE ON FUNCTION advance.intercompany_purchase_json(uuid,uuid,uuid,text),advance.intercompany_purchases_page(uuid,uuid,text,integer,integer),
            advance.intercompany_purchase_options(uuid,uuid),advance.publish_intercompany_purchase(uuid,uuid,uuid,uuid,bigint,text,uuid,text,uuid,text,text) TO nexa_erp_runtime;
          END IF;
         END IF;
        END $ic_acl$;
        """;

    internal const string Verify = """
        DO $ic_verify$ DECLARE relation text; signature text; object_id oid; is_public_entry boolean;
        BEGIN
         IF to_regclass('advance.intercompany_purchase_publications') IS NOT NULL THEN
          FOREACH relation IN ARRAY ARRAY['intercompany_purchase_publications','intercompany_purchase_publication_lines'] LOOP
           object_id:=to_regclass('advance.'||relation);
           IF object_id IS NULL OR NOT EXISTS(SELECT 1 FROM pg_class c WHERE c.oid=object_id AND c.relowner='nexa_erp_owner'::regrole
             AND NOT EXISTS(SELECT 1 FROM aclexplode(coalesce(c.relacl,acldefault('r',c.relowner))) a WHERE a.grantee<>c.relowner))
           THEN RAISE EXCEPTION 'Intercompany publication table owner or direct ACL is invalid: %',relation; END IF;
          END LOOP;
          FOREACH signature IN ARRAY ARRAY[
           'advance.guard_intercompany_purchase_evidence()','advance.intercompany_purchase_scope(uuid,uuid,advance.purchase_orders)',
           'advance.intercompany_purchase_json(uuid,uuid,uuid,text)','advance.intercompany_purchases_page(uuid,uuid,text,integer,integer)',
           'advance.intercompany_purchase_options(uuid,uuid)','advance.publish_intercompany_purchase(uuid,uuid,uuid,uuid,bigint,text,uuid,text,uuid,text,text)'] LOOP
           object_id:=to_regprocedure(signature);
           is_public_entry:=signature NOT IN('advance.guard_intercompany_purchase_evidence()','advance.intercompany_purchase_scope(uuid,uuid,advance.purchase_orders)');
           IF object_id IS NULL OR NOT EXISTS(SELECT 1 FROM pg_proc p WHERE p.oid=object_id
             AND p.proowner='nexa_erp_owner'::regrole AND p.prosecdef=is_public_entry
             AND array_length(p.proconfig,1)=1 AND EXISTS(SELECT 1 FROM unnest(p.proconfig) setting WHERE regexp_replace(setting,'[[:space:]]','','g')='search_path=pg_catalog,advance')
             AND NOT EXISTS(SELECT 1 FROM aclexplode(coalesce(p.proacl,acldefault('f',p.proowner))) a
              WHERE a.grantee<>p.proowner AND (NOT is_public_entry OR a.grantee<>'nexa_erp_runtime'::regrole OR a.is_grantable)))
             OR has_function_privilege('nexa_erp_runtime',object_id,'EXECUTE') IS DISTINCT FROM is_public_entry
           THEN RAISE EXCEPTION 'Intercompany publication function owner, search path or ACL is invalid: %',signature; END IF;
          END LOOP;
         ELSIF to_regclass('advance.intercompany_purchase_publication_lines') IS NOT NULL
          OR to_regprocedure('advance.intercompany_purchase_options(uuid,uuid)') IS NOT NULL THEN
          RAISE EXCEPTION 'Intercompany publication package is partially installed.';
         END IF;
        END $ic_verify$;
        """;
}
