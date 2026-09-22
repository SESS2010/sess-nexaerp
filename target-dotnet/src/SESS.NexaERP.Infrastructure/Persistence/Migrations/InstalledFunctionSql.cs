namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>
/// Rewrites an installed PL/pgSQL function from its installed body (never from a source
/// baseline, so CRLF checkouts and later migrations are respected): each fragment must occur
/// exactly the expected number of times (one unless stated) or the block refuses, and the
/// header (argument names, result, security mode, search_path) is taken from pg_proc so nothing
/// else about the function changes.
/// </summary>
internal static class InstalledFunctionSql
{
    internal static string Rewrite(string signature, params (string From, string To)[] replacements) =>
        Rewrite(signature, replacements.Select(r => (r.From, r.To, 1)).ToArray());

    internal static string Rewrite(string signature, params (string From, string To, int Occurrences)[] replacements)
    {
        var guards = string.Join("\n     OR ", replacements.Select(r =>
            $"(length(body)-length(replace(body,{Quote(r.From)},'')))/length({Quote(r.From)}) <> {r.Occurrences}"));
        var body = "body";
        foreach (var (from, to, _) in replacements) body = $"replace({body},{Quote(from)},{Quote(to)})";
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

    /// <summary>Rebuilds one CHECK constraint from its installed definition with a fragment replaced.</summary>
    internal static string RewriteCheck(string table, string constraint, string from, string to, int occurrences = 1) => MigrationText.Lf($$"""
        DO $check$
        DECLARE def text;
        BEGIN
          SELECT pg_get_constraintdef(oid) INTO STRICT def FROM pg_constraint WHERE conname='{{constraint}}' AND conrelid='{{table}}'::regclass;
          IF (length(def)-length(replace(def,{{Quote(from)}},'')))/length({{Quote(from)}}) <> {{occurrences}} THEN
            RAISE EXCEPTION '{{constraint}} is not at the expected contract; refusing to rewrite it.';
          END IF;
          EXECUTE 'ALTER TABLE {{table}} DROP CONSTRAINT "{{constraint}}"';
          EXECUTE 'ALTER TABLE {{table}} ADD CONSTRAINT "{{constraint}}" '||replace(def,{{Quote(from)}},{{Quote(to)}});
        END $check$;
        """);

    /// <summary>
    /// Replaces one CHECK constraint with an explicit expression. Fragment replacement is not used
    /// for IN lists because PostgreSQL re-renders an ARRAY literal differently once it is re-added;
    /// instead the installed definition must contain <paramref name="expected"/> and must not contain
    /// <paramref name="unexpected"/>, or the block refuses.
    /// </summary>
    internal static string ReplaceCheck(string table, string constraint, string expected, string unexpected, string expression) => MigrationText.Lf($$"""
        DO $check$
        DECLARE def text;
        BEGIN
          SELECT pg_get_constraintdef(oid) INTO STRICT def FROM pg_constraint WHERE conname='{{constraint}}' AND conrelid='{{table}}'::regclass;
          IF position({{Quote(expected)}} IN def)=0 OR position({{Quote(unexpected)}} IN def)>0 THEN
            RAISE EXCEPTION '{{constraint}} is not at the expected contract; refusing to replace it.';
          END IF;
          EXECUTE 'ALTER TABLE {{table}} DROP CONSTRAINT "{{constraint}}"';
          EXECUTE 'ALTER TABLE {{table}} ADD CONSTRAINT "{{constraint}}" CHECK ('||{{Quote(expression)}}||')';
        END $check$;
        """);

    internal static string Quote(string clause) => "'" + clause.Replace("'", "''", StringComparison.Ordinal) + "'";
}
