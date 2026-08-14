
namespace SESS.NexaERP.Tests;

/// <summary>
/// Source contract for the executable, separately authorized REV869B control-plane package under tools/.
/// This type never provisions; the PowerShell helper owns the explicit execution boundary.
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
        new("rev869b_recovery_attempts", "r", Owner, "AttemptId"),
        new("rev869b_recovery_outcomes", "r", Owner, "OutcomeId")
    ];

    internal static string GeneratePlan(SafeMode mode)
    {
        if (mode is not (SafeMode.GeneratePlanOnly or SafeMode.PreflightOnly or SafeMode.PostProvisionVerification))
            throw new InvalidOperationException("The source contract exposes no mutating provisioning mode.");

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
        lines.Add("EXECUTABLE_BOOTSTRAP=tools/rev869b-control-plane-bootstrap.sql");
        lines.Add("EXECUTABLE_INSTALL=tools/rev869b-control-plane-install.sql");
        lines.Add("EXECUTABLE_VERIFY=tools/rev869b-control-plane-verify.sql");
        lines.Add("EXECUTABLE_ROLLBACK=tools/rev869b-control-plane-rollback.sql");
        lines.Add("EXECUTION_HELPER=tools/manage-rev869b-control-plane-secure.ps1");
        lines.AddRange(Apis.Select(x => $"API={Schema}.{x.Name}({x.IdentityArguments});RETURNS={x.ResultType};SECURITY_DEFINER;{SearchPath};OWNER={Owner};EXECUTE={ApiRole};PUBLIC=NONE"));
        lines.AddRange(Relations.Select(x => $"RELATION={Schema}.{x.Name};KIND={x.Kind};OWNER={x.OwnerName};PRIMARY_KEY={x.PrimaryKey};API_ROLE_DML=NONE;PUBLIC=NONE"));
        return string.Join(Environment.NewLine, lines);
    }

    internal static void RequireSafeTarget(string database)
    {
        if (!string.Equals(database, Database, StringComparison.Ordinal))
            throw new InvalidOperationException("Only the exact isolated REV869B control-plane target is permitted.");
    }

    internal const string ExactReadinessSql = """
        SELECT count(*) FROM pg_database d
        WHERE d.datname=current_database() AND d.datname=@database
          AND pg_get_userbyid(d.datdba)=@owner
          AND nexa.rev869b_verify_exact_control_plane(@database::name,@owner::name,session_user::name)
        """;
}
