namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>Retained mappings identify document creators; only current actors need active mappings.</summary>
internal static class RetainedIdentityHistorySql
{
    internal const string Up = """
        DO $history$
        DECLARE fn record; definition text; old_query text; new_query text; prefix text;
          first_pos integer; last_pos integer; qualification_count integer:=0; history_count integer:=0;
          actor_predicate text; issuer_setting text;
        BEGIN
          IF current_setting('server_version_num')::integer<170000
             OR current_database() IN ('postgres','template0','template1') OR to_regnamespace('advance') IS NULL THEN
            RAISE EXCEPTION 'Retained identity history requires a PostgreSQL 17 application database.';
          END IF;
          FOR fn IN SELECT p.oid,p.proname FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
            WHERE n.nspname='advance' AND p.pronargs=0 AND p.proname IN
              ('ordinary_guard_qualification_lifecycle','rev869b_guard_qualification_lifecycle',
               'ordinary_guard_history_insert','rev869b_guard_history_insert')
          LOOP
            definition:=pg_get_functiondef(fn.oid);
            IF position('ITEM16_CREATOR_HISTORY:' IN definition)>0 THEN
              RAISE EXCEPTION 'Retained creator history is already installed in %.',fn.proname;
            END IF;
            prefix:='SELECT count(*),min(m."EmployeeId"::text)::uuid INTO creator_matches,';
            first_pos:=position(prefix IN definition);
            IF first_pos=0 OR position(prefix IN substring(definition FROM first_pos+length(prefix)))>0 THEN
              RAISE EXCEPTION 'Reviewed creator lookup is absent or ambiguous in %.',fn.proname;
            END IF;
            last_pos:=first_pos+position(';' IN substring(definition FROM first_pos))-1;
            old_query:=substring(definition FROM first_pos FOR last_pos-first_pos+1);
            IF position('m."IsActive"' IN old_query)=0 THEN
              RAISE EXCEPTION 'Unexpected creator lookup in %.',fn.proname;
            END IF;
            IF fn.proname LIKE '%qualification_lifecycle' THEN
              IF position('creator_matches,creator_employee' IN old_query)=0
                 OR position('m."Subject"=OLD."CreatedBy"' IN old_query)=0 THEN
                RAISE EXCEPTION 'Qualification creator contract differs from the reviewed baseline.';
              END IF;
              new_query:='SELECT count(DISTINCT m."EmployeeId"),min(m."EmployeeId"::text)::uuid INTO creator_matches,creator_employee
                FROM advance.employee_identity_mappings m
                WHERE m."CompanyId"=NEW."CompanyId" AND m."OrganizationId"=NEW."OrganizationId"
                  AND m."Subject"=OLD."CreatedBy";';
              qualification_count:=qualification_count+1;
            ELSE
              IF position('creator_matches,parent_creator_employee' IN old_query)=0
                 OR position('m."Subject"=parent_creator' IN old_query)=0 THEN
                RAISE EXCEPTION 'Purchase creator contract differs from the reviewed baseline.';
              END IF;
              new_query:='SELECT count(DISTINCT m."EmployeeId"),min(m."EmployeeId"::text)::uuid INTO creator_matches,parent_creator_employee
                FROM advance.employee_identity_mappings m
                WHERE m."OrganizationId"=parent_org AND m."Subject"=parent_creator;';
              history_count:=history_count+1;
            END IF;
            -- Preserve the exact replaced lookup for a guarded, lossless Down.
            definition:=overlay(definition placing
              '/*ITEM16_CREATOR_HISTORY:'||encode(convert_to(old_query,'UTF8'),'base64')||'*/'||chr(10)||new_query
              FROM first_pos FOR last_pos-first_pos+1);
            IF fn.proname LIKE '%qualification_lifecycle' THEN
              actor_predicate:='m."Subject"=NEW."UpdatedBy" AND m."IsActive"';
              IF position(actor_predicate IN definition)=0 THEN
                RAISE EXCEPTION 'Qualification actor lookup differs from the reviewed baseline.';
              END IF;
              issuer_setting:=CASE WHEN position('advance.ordinary_identity_issuer' IN definition)>0
                THEN 'advance.ordinary_identity_issuer' ELSE 'advance.rev869b_identity_issuer' END;
              definition:=replace(definition,actor_predicate,
                'm."Subject"=NEW."UpdatedBy" AND m."Issuer"=current_setting('''||issuer_setting||''',true) AND m."IsActive"');
            END IF;
            EXECUTE definition;
          END LOOP;
          IF qualification_count=0 OR history_count=0 THEN
            RAISE EXCEPTION 'Current qualification and purchase history guards must both be present.';
          END IF;
        END $history$;
        """;

    internal const string Down = """
        DO $history$
        DECLARE fn record; definition text; first_pos integer; marker_end integer; last_pos integer;
          marker text:='/*ITEM16_CREATOR_HISTORY:'; encoded text; old_query text; count_restored integer:=0;
          issuer_setting text;
        BEGIN
          IF current_setting('server_version_num')::integer<170000
             OR current_database() IN ('postgres','template0','template1') OR to_regnamespace('advance') IS NULL THEN
            RAISE EXCEPTION 'Retained identity rollback requires a PostgreSQL 17 application database.';
          END IF;
          FOR fn IN SELECT p.oid,p.proname FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
            WHERE n.nspname='advance' AND p.pronargs=0 AND p.proname IN
              ('ordinary_guard_qualification_lifecycle','rev869b_guard_qualification_lifecycle',
               'ordinary_guard_history_insert','rev869b_guard_history_insert')
          LOOP
            definition:=pg_get_functiondef(fn.oid);
            first_pos:=position(marker IN definition);
            IF first_pos=0 THEN RAISE EXCEPTION 'Retained creator rollback marker is absent in %.',fn.proname; END IF;
            marker_end:=first_pos+position('*/' IN substring(definition FROM first_pos))-1;
            encoded:=substring(definition FROM first_pos+length(marker) FOR marker_end-first_pos-length(marker));
            old_query:=convert_from(decode(encoded,'base64'),'UTF8');
            last_pos:=first_pos+position(';' IN substring(definition FROM first_pos))-1;
            IF position('count(DISTINCT m."EmployeeId")' IN substring(definition FROM marker_end FOR last_pos-marker_end))=0 THEN
              RAISE EXCEPTION 'Retained creator guard changed after installation.';
            END IF;
            definition:=overlay(definition placing old_query FROM first_pos FOR last_pos-first_pos+1);
            issuer_setting:=CASE WHEN position('advance.ordinary_identity_issuer' IN definition)>0
              THEN 'advance.ordinary_identity_issuer' ELSE 'advance.rev869b_identity_issuer' END;
            IF fn.proname LIKE '%qualification_lifecycle' THEN
              definition:=replace(definition,
                'm."Subject"=NEW."UpdatedBy" AND m."Issuer"=current_setting('''||issuer_setting||''',true) AND m."IsActive"',
                'm."Subject"=NEW."UpdatedBy" AND m."IsActive"');
            END IF;
            EXECUTE definition;
            count_restored:=count_restored+1;
          END LOOP;
          IF count_restored<2 THEN RAISE EXCEPTION 'Retained creator rollback did not restore both guard families.'; END IF;
        END $history$;
        """;
}
