namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>
/// Rewrites an installed PL/pgSQL function from its installed body (never from a source
/// baseline, so CRLF checkouts and later migrations are respected): each fragment must occur
/// exactly once or the block refuses, and the header (argument names, result, security mode,
/// search_path) is taken from pg_proc so nothing else about the function changes.
/// </summary>
internal static class InstalledFunctionSql
{
    internal static string Rewrite(string signature, params (string From, string To)[] replacements)
    {
        var guards = string.Join("\n     OR ", replacements.Select(r =>
            $"(length(body)-length(replace(body,{Quote(r.From)},'')))/length({Quote(r.From)}) <> 1"));
        var body = "body";
        foreach (var (from, to) in replacements) body = $"replace({body},{Quote(from)},{Quote(to)})";
        return MigrationText.Lf($$"""
            DO $rewrite$
            DECLARE body text; header text;
            BEGIN
              SELECT replace(prosrc,E'\r\n',E'\n') INTO STRICT body FROM pg_proc WHERE oid='{{signature}}'::regprocedure;
              IF {{guards}} THEN
                RAISE EXCEPTION '{{signature}} is not at the expected contract; refusing to rewrite it.';
              END IF;
              body := {{body}};
              SELECT 'CREATE OR REPLACE FUNCTION advance.'||proname||'('||pg_get_function_arguments(oid)||') RETURNS '||pg_get_function_result(oid)
                     ||' LANGUAGE plpgsql'||CASE WHEN prosecdef THEN ' SECURITY DEFINER' ELSE '' END
                     ||' SET search_path=pg_catalog,advance AS '
                INTO STRICT header FROM pg_proc WHERE oid='{{signature}}'::regprocedure AND prolang=(SELECT oid FROM pg_language WHERE lanname='plpgsql');
              EXECUTE header || quote_literal(body);
            END $rewrite$;
            """);
    }

    internal static string Quote(string clause) => "'" + clause.Replace("'", "''", StringComparison.Ordinal) + "'";
}
