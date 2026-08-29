namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class MasterDataImportFrameworkSql
{
    internal const string Up = """
        CREATE OR REPLACE FUNCTION advance.guard_master_import_row_results()
        RETURNS trigger
        LANGUAGE plpgsql
        SET search_path = pg_catalog
        AS $function$
        DECLARE
          expected_errors jsonb;
          installer_is_superuser boolean;
        BEGIN
          IF TG_OP = 'DELETE' THEN
            RAISE EXCEPTION USING ERRCODE = '55000', MESSAGE = 'master import row results are append-only';
          END IF;

          SELECT r.rolsuper INTO installer_is_superuser
          FROM pg_catalog.pg_roles r
          WHERE r.rolname = session_user;

          SELECT COALESCE(pg_catalog.jsonb_agg(e.value - 'attemptedValue' - 'AttemptedValue'), '[]'::jsonb)
          INTO expected_errors
          FROM pg_catalog.jsonb_array_elements(OLD."ErrorsJson") e(value);

          IF COALESCE(installer_is_superuser, false)
             AND NEW."SubmittedValuesJson" IS NULL
             AND NEW."ErrorsJson" = expected_errors
             AND (pg_catalog.to_jsonb(NEW) - 'SubmittedValuesJson' - 'ErrorsJson')
                 = (pg_catalog.to_jsonb(OLD) - 'SubmittedValuesJson' - 'ErrorsJson')
             AND EXISTS (
               SELECT 1
               FROM advance.master_import_batches b
               WHERE b."Id" = OLD."ImportBatchId"
                 AND b."RetentionExpiresAt" <= pg_catalog.clock_timestamp()
             ) THEN
            RETURN NEW;
          END IF;

          RAISE EXCEPTION USING ERRCODE = '55000',
            MESSAGE = 'master import row results are append-only; only the expired sensitive-value purge is permitted';
        END
        $function$;

        CREATE TRIGGER "TR_master_import_row_results_append_only"
        BEFORE UPDATE OR DELETE ON advance.master_import_row_results
        FOR EACH ROW EXECUTE FUNCTION advance.guard_master_import_row_results();

        CREATE OR REPLACE FUNCTION advance.purge_expired_master_import_sensitive_values()
        RETURNS TABLE("BatchCount" bigint, "RowCount" bigint)
        LANGUAGE plpgsql
        SECURITY DEFINER
        SET search_path = pg_catalog
        AS $function$
        DECLARE
          batch_count bigint;
          row_count bigint;
        BEGIN
          IF NOT EXISTS (
            SELECT 1 FROM pg_catalog.pg_roles r
            WHERE r.rolname = session_user AND r.rolsuper
          ) THEN
            RAISE EXCEPTION USING ERRCODE = '42501',
              MESSAGE = 'master import sensitive-value purge requires the guarded installer superuser session';
          END IF;

          WITH expired AS MATERIALIZED (
            SELECT b."Id"
            FROM advance.master_import_batches b
            WHERE b."RetentionExpiresAt" <= pg_catalog.clock_timestamp()
              AND b."SensitiveValuesPurgedAt" IS NULL
          )
          UPDATE advance.master_import_row_results r
          SET "SubmittedValuesJson" = NULL,
              "ErrorsJson" = (
                SELECT COALESCE(pg_catalog.jsonb_agg(e.value - 'attemptedValue' - 'AttemptedValue'), '[]'::jsonb)
                FROM pg_catalog.jsonb_array_elements(r."ErrorsJson") e(value)
              )
          WHERE r."ImportBatchId" IN (SELECT e."Id" FROM expired e)
            AND (
              r."SubmittedValuesJson" IS NOT NULL
              OR r."ErrorsJson" @? '$[*].attemptedValue'
              OR r."ErrorsJson" @? '$[*].AttemptedValue'
            );
          GET DIAGNOSTICS row_count = ROW_COUNT;

          UPDATE advance.master_import_batches b
          SET "SensitiveValuesPurgedAt" = pg_catalog.clock_timestamp(),
              "UpdatedAt" = pg_catalog.clock_timestamp(),
              "UpdatedBy" = 'master-import-retention-purge',
              "Version" = b."Version" + 1
          WHERE b."RetentionExpiresAt" <= pg_catalog.clock_timestamp()
            AND b."SensitiveValuesPurgedAt" IS NULL;
          GET DIAGNOSTICS batch_count = ROW_COUNT;

          RETURN QUERY SELECT batch_count, row_count;
        END
        $function$;

        REVOKE ALL ON FUNCTION advance.purge_expired_master_import_sensitive_values() FROM PUBLIC;
        """;

    internal const string Down = """
        DROP FUNCTION IF EXISTS advance.purge_expired_master_import_sensitive_values();
        DROP TRIGGER IF EXISTS "TR_master_import_row_results_append_only" ON advance.master_import_row_results;
        DROP FUNCTION IF EXISTS advance.guard_master_import_row_results();
        """;
}
