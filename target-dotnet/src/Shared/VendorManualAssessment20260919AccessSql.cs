namespace SESS.NexaERP.Database;

internal static class VendorManualAssessment20260919AccessSql
{
    internal const string Provision = """
        DO $ic_acl$ DECLARE signature text; principal text;
        BEGIN
         IF to_regclass('advance.vendor_manual_assessments') IS NOT NULL THEN
          REVOKE ALL ON TABLE advance.vendor_manual_assessments FROM PUBLIC;
          IF to_regrole('nexa_erp_owner') IS NOT NULL THEN ALTER TABLE advance.vendor_manual_assessments OWNER TO nexa_erp_owner; END IF;
          FOREACH principal IN ARRAY ARRAY['nexa_erp_runtime','nexa_erp_bootstrap','nexa_erp_migration'] LOOP
           IF to_regrole(principal) IS NOT NULL THEN EXECUTE format('REVOKE ALL ON TABLE advance.vendor_manual_assessments FROM %I',principal); END IF;
          END LOOP;
          FOREACH signature IN ARRAY ARRAY[
           'advance.guard_vendor_manual_assessment()',
           'advance.vendor_manual_assessment_json(uuid,uuid)',
           'advance.vendor_manual_assessment_history(uuid,uuid)',
           'advance.record_vendor_manual_assessment(uuid,uuid,uuid,bigint,uuid,numeric,numeric,numeric,text,uuid,text,uuid,text,text)'] LOOP
           EXECUTE format('REVOKE ALL ON FUNCTION %s FROM PUBLIC',signature);
           IF to_regrole('nexa_erp_owner') IS NOT NULL THEN EXECUTE format('ALTER FUNCTION %s OWNER TO nexa_erp_owner',signature); END IF;
           FOREACH principal IN ARRAY ARRAY['nexa_erp_runtime','nexa_erp_bootstrap','nexa_erp_migration'] LOOP
            IF to_regrole(principal) IS NOT NULL THEN EXECUTE format('REVOKE ALL ON FUNCTION %s FROM %I',signature,principal); END IF;
           END LOOP;
          END LOOP;
          IF to_regrole('nexa_erp_runtime') IS NOT NULL THEN
           GRANT EXECUTE ON FUNCTION advance.vendor_manual_assessment_json(uuid,uuid),advance.vendor_manual_assessment_history(uuid,uuid),advance.record_vendor_manual_assessment(uuid,uuid,uuid,bigint,uuid,numeric,numeric,numeric,text,uuid,text,uuid,text,text) TO nexa_erp_runtime;
          END IF;
         END IF;
        END $ic_acl$;
        """;

    internal const string Verify = """
        DO $ic_verify$ DECLARE signature text; object_id oid; is_entry boolean;
        BEGIN
         IF to_regclass('advance.vendor_manual_assessments') IS NOT NULL THEN
          object_id:=to_regclass('advance.vendor_manual_assessments');
          IF NOT EXISTS(SELECT 1 FROM pg_class c WHERE c.oid=object_id AND c.relowner='nexa_erp_owner'::regrole
           AND NOT EXISTS(SELECT 1 FROM aclexplode(coalesce(c.relacl,acldefault('r',c.relowner))) a WHERE a.grantee<>c.relowner))
          THEN RAISE EXCEPTION 'Manual vendor assessment table owner or direct ACL is invalid.'; END IF;
          IF NOT EXISTS(SELECT 1 FROM pg_trigger t WHERE t.tgrelid=object_id
           AND t.tgname='trg_vendor_manual_assessment' AND NOT t.tgisinternal
           AND t.tgenabled='O' AND t.tgtype=31
           AND t.tgfoid=to_regprocedure('advance.guard_vendor_manual_assessment()'))
          THEN RAISE EXCEPTION 'Manual vendor assessment append guard is absent or disabled.'; END IF;
          FOREACH signature IN ARRAY ARRAY[
           'advance.guard_vendor_manual_assessment()',
           'advance.vendor_manual_assessment_json(uuid,uuid)',
           'advance.vendor_manual_assessment_history(uuid,uuid)',
           'advance.record_vendor_manual_assessment(uuid,uuid,uuid,bigint,uuid,numeric,numeric,numeric,text,uuid,text,uuid,text,text)'] LOOP
           object_id:=to_regprocedure(signature);
           is_entry:=signature<>'advance.guard_vendor_manual_assessment()';
           IF object_id IS NULL OR NOT EXISTS(SELECT 1 FROM pg_proc p WHERE p.oid=object_id
            AND p.proowner='nexa_erp_owner'::regrole AND p.prosecdef=is_entry
            AND array_length(p.proconfig,1)=1 AND EXISTS(SELECT 1 FROM unnest(p.proconfig) setting
             WHERE regexp_replace(setting,'[[:space:]]','','g')='search_path=pg_catalog,advance')
            AND NOT EXISTS(SELECT 1 FROM aclexplode(coalesce(p.proacl,acldefault('f',p.proowner))) a
             WHERE a.grantee<>p.proowner AND (NOT is_entry OR a.grantee<>'nexa_erp_runtime'::regrole OR a.is_grantable)))
            OR has_function_privilege('nexa_erp_runtime',object_id,'EXECUTE') IS DISTINCT FROM is_entry
           THEN RAISE EXCEPTION 'Manual vendor assessment function owner, search path or ACL is invalid: %',signature; END IF;
          END LOOP;
         ELSIF to_regprocedure('advance.guard_vendor_manual_assessment()') IS NOT NULL
          OR to_regprocedure('advance.vendor_manual_assessment_json(uuid,uuid)') IS NOT NULL
          OR to_regprocedure('advance.vendor_manual_assessment_history(uuid,uuid)') IS NOT NULL
          OR to_regprocedure('advance.record_vendor_manual_assessment(uuid,uuid,uuid,bigint,uuid,numeric,numeric,numeric,text,uuid,text,uuid,text,text)') IS NOT NULL
         THEN RAISE EXCEPTION 'Manual vendor assessment package is partially installed.';
         END IF;
        END $ic_verify$;
        """;
}
