namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class ControlledEstimatedBomDraftReplacementSql
{
    internal const string Up = """
        DO $guard$ BEGIN
          IF current_setting('server_version_num')::integer < 170000 THEN RAISE EXCEPTION 'Controlled Estimated BOM draft replacement requires PostgreSQL 17 or later.'; END IF;
          IF current_database() IN ('postgres','template0','template1') THEN RAISE EXCEPTION 'Controlled Estimated BOM draft replacement refuses a PostgreSQL administrative database.'; END IF;
          IF (SELECT count(*) FROM pg_roles WHERE rolname IN ('nexa_erp_owner','nexa_erp_migration','nexa_erp_bootstrap','nexa_erp_runtime')) NOT IN (0,4) THEN
            RAISE EXCEPTION 'Partial NexaERP principal state; controlled Estimated BOM draft replacement requires all four managed roles or none.';
          END IF;
        END $guard$;
        CREATE FUNCTION advance.replace_estimated_bom_draft_lines(
          p_company_id uuid,p_organization text,p_revision_id uuid,p_expected_version bigint,
          p_actor_employee_id uuid,p_identity_issuer text,p_identity_subject text,p_actor_role text,
          p_created_by text,p_lines jsonb)
        RETURNS integer LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
        DECLARE current_status text; current_version bigint; inserted_count integer;
        BEGIN
          IF p_company_id IS NULL OR length(trim(coalesce(p_organization,'')))=0 OR p_revision_id IS NULL OR p_expected_version<0
             OR p_actor_employee_id IS NULL OR length(trim(coalesce(p_identity_issuer,'')))=0
             OR length(trim(coalesce(p_identity_subject,'')))=0 OR length(trim(coalesce(p_actor_role,'')))=0
             OR length(trim(coalesce(p_created_by,'')))=0 THEN
            RAISE EXCEPTION 'Estimated BOM company, organization, revision, Version, identity, role and actor are required.';
          END IF;
          IF NOT advance.ordinary_command_context_valid(trim(p_organization),p_actor_employee_id,
               trim(p_identity_issuer),trim(p_identity_subject),trim(p_actor_role)) THEN
            RAISE EXCEPTION 'Estimated BOM draft replacement requires the current ordinary command transaction.';
          END IF;
          IF jsonb_typeof(p_lines)<>'array' OR jsonb_array_length(p_lines)=0 THEN
            RAISE EXCEPTION 'Estimated BOM requires a complete non-empty line array.';
          END IF;
          SELECT "Status","Version" INTO current_status,current_version
          FROM advance.estimated_bom_revisions
          WHERE "Id"=p_revision_id AND "CompanyId"=p_company_id FOR UPDATE;
          IF NOT FOUND THEN RAISE EXCEPTION 'Estimated BOM revision does not exist in the selected company.'; END IF;
          IF current_status<>'DRAFT' THEN RAISE EXCEPTION 'Only a Draft Estimated BOM revision can be edited.'; END IF;
          IF current_version<>p_expected_version THEN RAISE EXCEPTION 'Estimated BOM revision Version is stale.'; END IF;
          IF EXISTS (SELECT 1 FROM jsonb_to_recordset(p_lines) AS x("lineNumber" integer,"itemId" uuid,"uomId" uuid,"quantity" numeric,"remarks" text)
                     WHERE x."lineNumber"<=0 OR x."itemId" IS NULL OR x."uomId" IS NULL OR x."quantity"<=0)
             OR (SELECT count(*) FROM jsonb_to_recordset(p_lines) AS x("lineNumber" integer))<>(SELECT count(DISTINCT x."lineNumber") FROM jsonb_to_recordset(p_lines) AS x("lineNumber" integer))
             OR (SELECT count(*) FROM jsonb_to_recordset(p_lines) AS x("lineNumber" integer))<>(SELECT max(x."lineNumber") FROM jsonb_to_recordset(p_lines) AS x("lineNumber" integer))
             OR EXISTS (SELECT 1 FROM jsonb_to_recordset(p_lines) AS x("itemId" uuid) WHERE NOT EXISTS
                  (SELECT 1 FROM advance.items i WHERE i."Id"=x."itemId" AND i."IsActive"))
             OR EXISTS (SELECT 1 FROM jsonb_to_recordset(p_lines) AS x("uomId" uuid) WHERE NOT EXISTS
                  (SELECT 1 FROM advance.uoms u WHERE u."Id"=x."uomId" AND u."IsActive")) THEN
            RAISE EXCEPTION 'Every Estimated BOM line must be positive, contiguous and reference an active company item and UOM.';
          END IF;
          DELETE FROM advance.estimated_bom_lines WHERE "EstimatedBomRevisionId"=p_revision_id AND "CompanyId"=p_company_id;
          INSERT INTO advance.estimated_bom_lines
            ("Id","CompanyId","EstimatedBomRevisionId","LineNumber","ItemId","UomId","Quantity","Remarks","CreatedAt","CreatedBy")
          SELECT gen_random_uuid(),p_company_id,p_revision_id,x."lineNumber",x."itemId",x."uomId",x."quantity",
                 nullif(trim(coalesce(x."remarks",'')),''),clock_timestamp(),trim(p_created_by)
          FROM jsonb_to_recordset(p_lines) AS x("lineNumber" integer,"itemId" uuid,"uomId" uuid,"quantity" numeric,"remarks" text)
          ORDER BY x."lineNumber";
          GET DIAGNOSTICS inserted_count=ROW_COUNT;
          RETURN inserted_count;
        END $function$;
        REVOKE ALL ON FUNCTION advance.replace_estimated_bom_draft_lines(uuid,text,uuid,bigint,uuid,text,text,text,text,jsonb) FROM PUBLIC;
        DO $roles$ BEGIN
          IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_runtime') THEN
            ALTER FUNCTION advance.replace_estimated_bom_draft_lines(uuid,text,uuid,bigint,uuid,text,text,text,text,jsonb) OWNER TO nexa_erp_owner;
            REVOKE ALL ON FUNCTION advance.replace_estimated_bom_draft_lines(uuid,text,uuid,bigint,uuid,text,text,text,text,jsonb) FROM nexa_erp_bootstrap,nexa_erp_migration;
            GRANT EXECUTE ON FUNCTION advance.replace_estimated_bom_draft_lines(uuid,text,uuid,bigint,uuid,text,text,text,text,jsonb) TO nexa_erp_runtime;
          END IF;
        END $roles$;
        """;

    internal const string Down = """
        DO $guard$ BEGIN
          IF current_setting('server_version_num')::integer < 170000 THEN RAISE EXCEPTION 'Controlled Estimated BOM draft replacement down requires PostgreSQL 17 or later.'; END IF;
          IF current_database() IN ('postgres','template0','template1') THEN RAISE EXCEPTION 'Controlled Estimated BOM draft replacement down refuses a PostgreSQL administrative database.'; END IF;
        END $guard$;
        DROP FUNCTION advance.replace_estimated_bom_draft_lines(uuid,text,uuid,bigint,uuid,text,text,text,text,jsonb);
        """;
}