namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class OrdinaryImmutableAuditSql
{
    internal const string Ensure = """
        DO $ordinary_audit_preflight$
        DECLARE
          rev_function_exists boolean;
          rev_trigger_exists boolean;
          rev_trigger_bound boolean;
          managed_role_count integer;
        BEGIN
          SELECT to_regprocedure('advance.rev869b_guard_durable_audit_retention()') IS NOT NULL
            INTO rev_function_exists;

          SELECT EXISTS (
            SELECT 1
              FROM pg_trigger t
              JOIN pg_class c ON c.oid=t.tgrelid
              JOIN pg_namespace n ON n.oid=c.relnamespace
             WHERE n.nspname='advance'
               AND c.relname='audit_logs'
               AND t.tgname='trg_rev869b_durable_audit_retention'
               AND NOT t.tgisinternal)
            INTO rev_trigger_exists;

          SELECT EXISTS (
            SELECT 1
              FROM pg_trigger t
              JOIN pg_class c ON c.oid=t.tgrelid
              JOIN pg_namespace n ON n.oid=c.relnamespace
             WHERE n.nspname='advance'
               AND c.relname='audit_logs'
               AND t.tgname='trg_rev869b_durable_audit_retention'
               AND NOT t.tgisinternal
               AND t.tgfoid=to_regprocedure('advance.rev869b_guard_durable_audit_retention()'))
            INTO rev_trigger_bound;

          IF rev_function_exists OR rev_trigger_exists THEN
            IF NOT rev_function_exists OR NOT rev_trigger_exists OR NOT rev_trigger_bound THEN
              RAISE EXCEPTION USING
                ERRCODE='55000',
                MESSAGE='Refusing ordinary audit guard installation: the REV869B durable-audit guard is partially installed or bound incorrectly.';
            END IF;
          END IF;

          SELECT count(*) INTO managed_role_count
            FROM pg_roles
           WHERE rolname IN ('nexa_erp_owner','nexa_erp_migration','nexa_erp_bootstrap','nexa_erp_runtime');
          IF managed_role_count NOT IN (0,4) THEN
            RAISE EXCEPTION USING
              ERRCODE='55000',
              MESSAGE='Refusing ordinary audit guard installation: the ordinary database-principal set is partial.';
          END IF;
        END $ordinary_audit_preflight$;

        CREATE OR REPLACE FUNCTION advance.guard_audit_logs_immutable()
        RETURNS trigger
        LANGUAGE plpgsql
        SET search_path=pg_catalog,advance
        AS $ordinary_audit$
        BEGIN
          RAISE EXCEPTION USING
            ERRCODE='42501',
            SCHEMA='advance',
            TABLE='audit_logs',
            CONSTRAINT='audit_logs_immutable',
            MESSAGE='Durable audit evidence is immutable.';
        END $ordinary_audit$;

        REVOKE ALL ON FUNCTION advance.guard_audit_logs_immutable() FROM PUBLIC;

        DROP TRIGGER IF EXISTS "TR_audit_logs_immutable" ON advance.audit_logs;
        CREATE TRIGGER "TR_audit_logs_immutable"
          BEFORE UPDATE OR DELETE ON advance.audit_logs
          FOR EACH ROW EXECUTE FUNCTION advance.guard_audit_logs_immutable();

        DO $ordinary_audit_owner$
        BEGIN
          IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_owner') THEN
            IF pg_has_role(current_user,'nexa_erp_owner','MEMBER') THEN
              ALTER FUNCTION advance.guard_audit_logs_immutable() OWNER TO nexa_erp_owner;
            ELSE
              RAISE EXCEPTION USING
                ERRCODE='42501',
                MESSAGE='Installing the ordinary audit guard requires membership in nexa_erp_owner when managed principals exist.';
            END IF;
          END IF;
        END $ordinary_audit_owner$;
        """;

    // This security floor is monotonic: rolling EF history back must not make
    // existing audit evidence mutable, even for the interval before re-apply.
    internal const string PreserveOnDown = """
        DO $ordinary_audit_down$
        DECLARE trigger_bound boolean;
        BEGIN
          SELECT EXISTS (
            SELECT 1
              FROM pg_trigger t
              JOIN pg_class c ON c.oid=t.tgrelid
              JOIN pg_namespace n ON n.oid=c.relnamespace
             WHERE n.nspname='advance'
               AND c.relname='audit_logs'
               AND t.tgname='TR_audit_logs_immutable'
               AND NOT t.tgisinternal
               AND t.tgenabled<>'D'
               AND t.tgfoid=to_regprocedure('advance.guard_audit_logs_immutable()'))
            INTO trigger_bound;

          IF to_regprocedure('advance.guard_audit_logs_immutable()') IS NULL OR NOT trigger_bound THEN
            RAISE EXCEPTION USING
              ERRCODE='55000',
              MESSAGE='Refusing migration rollback: the ordinary immutable-audit security floor is missing or incorrectly bound.';
          END IF;
        END $ordinary_audit_down$;
        """;
}
