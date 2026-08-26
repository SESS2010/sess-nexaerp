#if DEBUG
internal static class DevelopmentAuthenticationBootstrapCommandSql
{
    internal const string ClusterGuard = """
        SELECT current_setting('server_version_num')::integer,
               current_database(),
               to_regnamespace('advance') IS NOT NULL,
               session_user,
               current_user,
               role.rolsuper,
               pg_catalog.pg_get_userbyid(database.datdba)=session_user,
               pg_catalog.pg_get_userbyid(namespace.nspowner)=session_user,
               to_regprocedure('advance.complete_authentication_bootstrap(text,text)') IS NOT NULL,
               (SELECT count(*) FROM pg_catalog.pg_roles
                 WHERE rolname IN ('nexa_erp_owner','nexa_erp_migration','nexa_erp_bootstrap','nexa_erp_runtime'))
        FROM pg_catalog.pg_roles role
        JOIN pg_catalog.pg_database database ON database.datname=current_database()
        JOIN pg_catalog.pg_namespace namespace ON namespace.nspname='advance'
        WHERE role.rolname=session_user;
        """;

    internal const string AcquireLockAndCreateRole = """
        SELECT pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended('NEXAERP_AUTHENTICATION_BOOTSTRAP_DEVELOPMENT_V1',0));
        DO $guard$
        BEGIN
          IF EXISTS (SELECT 1 FROM pg_catalog.pg_roles
                     WHERE rolname IN ('nexa_erp_owner','nexa_erp_migration','nexa_erp_bootstrap','nexa_erp_runtime')) THEN
            RAISE EXCEPTION 'Development authentication bootstrap requires all four managed database principals to be absent.';
          END IF;
        END $guard$;
        CREATE ROLE nexa_erp_bootstrap NOLOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
        GRANT USAGE ON SCHEMA advance TO nexa_erp_bootstrap;
        GRANT EXECUTE ON FUNCTION advance.complete_authentication_bootstrap(text,text) TO nexa_erp_bootstrap;
        SET LOCAL SESSION AUTHORIZATION nexa_erp_bootstrap;
        """;

    internal const string RestoreAndDropRole = """
        SET LOCAL SESSION AUTHORIZATION DEFAULT;
        REVOKE EXECUTE ON FUNCTION advance.complete_authentication_bootstrap(text,text) FROM nexa_erp_bootstrap;
        REVOKE USAGE ON SCHEMA advance FROM nexa_erp_bootstrap;
        DROP ROLE nexa_erp_bootstrap;
        """;

    internal const string RecoveryWitness = """
        SELECT session_user=@original_session_user,
               current_user=@original_session_user,
               NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='nexa_erp_bootstrap');
        """;
}
#endif
