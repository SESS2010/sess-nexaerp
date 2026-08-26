internal static class AuthenticationBootstrapCommandSql
{
    internal const string ClusterGuard = """
        SELECT current_setting('server_version_num')::integer,
               current_database(),
               to_regnamespace('advance') IS NOT NULL,
               session_user,
               to_regprocedure('advance.complete_authentication_bootstrap(text,text)') IS NOT NULL;
        """;

    internal const string Complete =
        "SELECT advance.complete_authentication_bootstrap(@issuer,@subject)::text;";
}
