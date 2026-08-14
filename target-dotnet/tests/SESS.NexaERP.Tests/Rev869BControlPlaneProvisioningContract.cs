
namespace SESS.NexaERP.Tests;

/// <summary>
/// Source-controlled manifest for a future, separately authorized REV869B control-plane provisioning.
/// This type deliberately exposes plan/preflight text only: it never opens a connection or mutates a database.
/// </summary>
internal static class Rev869BControlPlaneProvisioningContract
{
    internal const string Database = "sess_nexaerp_rev869b_control_plane";
    internal const string Owner = "nexa_rev869b_control_plane_owner";
    internal const string ApiRole = "nexa_rev869b_control_plane_api";
    internal const string RecoveryAdministrator = "nexa_rev869b_recovery_administrator";
    internal const string Schema = "nexa";
    internal const string SearchPath = "search_path=pg_catalog, nexa";

    internal enum SafeMode
    {
        GeneratePlanOnly,
        PreflightOnly,
        PostProvisionVerification
    }

    internal sealed record Api(string Name, string IdentityArguments, string ResultType);
    internal sealed record Relation(string Name, string Kind, string OwnerName, string PrimaryKey);

    internal static readonly Api[] Apis =
    [
        new("rev869b_reserve_database_lease",
            "name, text, text, text, text, name, text, text, text, text, name, timestamp with time zone, timestamp with time zone, name, name, text, text, text", "bigint"),
        new("rev869b_complete_database_lease",
            "name, text, text, text, text, name, text, text, text, text, name, timestamp with time zone, timestamp with time zone, name, name, text, text, text, text, text, text, text, timestamp with time zone, text", "bigint"),
        new("rev869b_read_exact_database_lease",
            "name, text, text, text, text, name, text, text, text, text, name, timestamp with time zone, timestamp with time zone, name, name, text, text, text, text", "record"),
        new("rev869b_begin_database_drop",
            "name, text, text, text, text, name, text, text, text, text, name, timestamp with time zone, timestamp with time zone, name, name, text, text, text, text, text, timestamp with time zone, text", "uuid"),
        new("rev869b_record_database_drop_outcome",
            "uuid, name, text, text, text, text, text, text, text, timestamp with time zone, text", "bigint"),
        new("rev869b_consume_recovery_approval",
            "uuid, name, text, text, text, text, name, text, text, text, text, name, timestamp with time zone, timestamp with time zone, name, name, text, text, text, text, text, text, text, text, text, text, timestamp with time zone, timestamp with time zone, text, text, timestamp with time zone, text", "uuid"),
        new("rev869b_record_recovery_outcome",
            "uuid, text, text, text, text, text, timestamp with time zone, text", "bigint")
    ];

    internal static readonly Relation[] Relations =
    [
        new("rev869b_database_leases", "r", Owner, "DatabaseName,RunId"),
        new("rev869b_database_lease_events", "r", Owner, "EventId"),
        new("rev869b_recovery_approvals", "r", Owner, "AuthorizationId"),
        new("rev869b_recovery_attempts", "r", Owner, "AttemptId")
    ];

    internal static string GeneratePlan(SafeMode mode)
    {
        if (mode is not (SafeMode.GeneratePlanOnly or SafeMode.PreflightOnly or SafeMode.PostProvisionVerification))
            throw new InvalidOperationException("Correction 16 exposes no mutating provisioning mode.");

        var lines = new List<string>
        {
            $"MODE={mode}",
            $"DATABASE={Database}",
            $"OWNER={Owner};NOLOGIN;NOSUPERUSER;NOCREATEDB;NOCREATEROLE;NOREPLICATION;NOBYPASSRLS",
            $"API_ROLE={ApiRole};LOGIN;NOINHERIT;CONNECT={Database};SCHEMA_USAGE={Schema};TABLE_DML=NONE",
            $"RECOVERY_ADMIN={RecoveryAdministrator};LOGIN;NOINHERIT;DIRECT_TABLE_DML=NONE",
            "PUBLIC=NO_CONNECT,NO_SCHEMA_CREATE,NO_TABLE_DML,NO_FUNCTION_EXECUTE",
            "DEFAULT_PRIVILEGES=REVOKE_ALL_FROM_PUBLIC",
            "OWNERSHIP=ALL_REGISTRY_RELATIONS_FUNCTIONS_TRIGGERS_BY_CONTROL_PLANE_OWNER",
            "APPEND_ONLY=LEASE_EVENTS,RECOVERY_ATTEMPTS",
            "NO_SILENT_REPAIR=TRUE",
            "NO_AUTOMATIC_DROP_OR_PRIVILEGE_WIDENING=TRUE",
            "SECRETS=EXTERNAL_ONLY_NEVER_IN_PLAN_OR_EVIDENCE"
        };
        lines.AddRange(Apis.Select(x => $"API={Schema}.{x.Name}({x.IdentityArguments});RETURNS={x.ResultType};SECURITY_DEFINER;{SearchPath};OWNER={Owner};EXECUTE={ApiRole};PUBLIC=NONE"));
        lines.AddRange(Relations.Select(x => $"RELATION={Schema}.{x.Name};KIND={x.Kind};OWNER={x.OwnerName};PRIMARY_KEY={x.PrimaryKey};API_ROLE_DML=NONE;PUBLIC=NONE"));
        return string.Join(Environment.NewLine, lines);
    }

    internal static void RequireSafeTarget(string database)
    {
        var forbidden = new[] { "postgres", "template0", "template1", "rev861", "nexaerp", "production", "prod" };
        if (!string.Equals(database, Database, StringComparison.Ordinal) ||
            forbidden.Any(x => database.Contains(x, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Only the exact isolated REV869B control-plane target is permitted.");
    }

    internal const string ExactReadinessSql = """
        WITH expected_api(name,identity_arguments,result_type) AS (VALUES
          ('rev869b_reserve_database_lease','name, text, text, text, text, name, text, text, text, text, name, timestamp with time zone, timestamp with time zone, name, name, text, text, text','bigint'),
          ('rev869b_complete_database_lease','name, text, text, text, text, name, text, text, text, text, name, timestamp with time zone, timestamp with time zone, name, name, text, text, text, text, text, text, text, timestamp with time zone, text','bigint'),
          ('rev869b_read_exact_database_lease','name, text, text, text, text, name, text, text, text, text, name, timestamp with time zone, timestamp with time zone, name, name, text, text, text, text','record'),
          ('rev869b_begin_database_drop','name, text, text, text, text, name, text, text, text, text, name, timestamp with time zone, timestamp with time zone, name, name, text, text, text, text, text, timestamp with time zone, text','uuid'),
          ('rev869b_record_database_drop_outcome','uuid, name, text, text, text, text, text, text, text, timestamp with time zone, text','bigint'),
          ('rev869b_consume_recovery_approval','uuid, name, text, text, text, text, name, text, text, text, text, name, timestamp with time zone, timestamp with time zone, name, name, text, text, text, text, text, text, text, text, text, text, timestamp with time zone, timestamp with time zone, text, text, timestamp with time zone, text','uuid'),
          ('rev869b_record_recovery_outcome','uuid, text, text, text, text, text, timestamp with time zone, text','bigint')
        ), actual_api AS (
          SELECT e.name,p.oid,pg_get_function_identity_arguments(p.oid) identity_arguments,
            pg_get_function_result(p.oid) result_type,pg_get_userbyid(p.proowner) owner,p.prosecdef,
            p.provolatile,p.proparallel,p.proleakproof,p.proconfig,
            has_function_privilege(session_user,p.oid,'EXECUTE') caller_execute,
            has_function_privilege('public',p.oid,'EXECUTE') public_execute
          FROM expected_api e JOIN pg_proc p ON p.proname=e.name
          JOIN pg_namespace n ON n.oid=p.pronamespace AND n.nspname='nexa'
        ), expected_relation(name) AS (VALUES
          ('rev869b_database_leases'),('rev869b_database_lease_events'),
          ('rev869b_recovery_approvals'),('rev869b_recovery_attempts')
        )
        SELECT count(*) FROM pg_database d
        WHERE d.datname=current_database() AND d.datname=@database
          AND pg_get_userbyid(d.datdba)=@owner
          AND (SELECT count(*) FROM actual_api a JOIN expected_api e USING(name)
               WHERE a.identity_arguments=e.identity_arguments AND a.result_type=e.result_type
                 AND a.owner=@owner AND a.prosecdef AND a.provolatile='v' AND a.proparallel='u'
                 AND NOT a.proleakproof AND a.proconfig=ARRAY['search_path=pg_catalog, nexa']::text[]
                 AND a.caller_execute AND NOT a.public_execute)=7
          AND (SELECT count(*) FROM actual_api)=7
          AND (SELECT count(*) FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
               JOIN expected_relation e ON e.name=c.relname
               WHERE n.nspname='nexa' AND c.relkind='r' AND pg_get_userbyid(c.relowner)=@owner)=4
          AND (SELECT count(*) FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
               WHERE n.nspname='nexa' AND c.relname IN
                 ('rev869b_database_leases','rev869b_database_lease_events','rev869b_recovery_approvals','rev869b_recovery_attempts'))=4
          AND (SELECT count(*) FROM pg_trigger t JOIN pg_class c ON c.oid=t.tgrelid
               JOIN pg_namespace n ON n.oid=c.relnamespace
               WHERE n.nspname='nexa' AND c.relname IN ('rev869b_database_lease_events','rev869b_recovery_attempts')
                 AND NOT t.tgisinternal AND t.tgenabled='O'
                 AND pg_get_triggerdef(t.oid) LIKE 'CREATE TRIGGER % BEFORE UPDATE OR DELETE %')=2
          AND NOT has_schema_privilege(session_user,'nexa','CREATE')
          AND NOT has_table_privilege(session_user,'nexa.rev869b_database_leases','SELECT,INSERT,UPDATE,DELETE')
          AND NOT has_table_privilege(session_user,'nexa.rev869b_database_lease_events','SELECT,INSERT,UPDATE,DELETE')
          AND NOT has_table_privilege(session_user,'nexa.rev869b_recovery_approvals','SELECT,INSERT,UPDATE,DELETE')
          AND NOT has_table_privilege(session_user,'nexa.rev869b_recovery_attempts','SELECT,INSERT,UPDATE,DELETE')
        """;
}
