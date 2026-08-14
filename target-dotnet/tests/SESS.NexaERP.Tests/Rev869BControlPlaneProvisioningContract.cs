namespace SESS.NexaERP.Tests;

/// <summary>Exact source contract for an externally provisioned, surviving REV869B control plane.</summary>
internal static class Rev869BControlPlaneProvisioningContract
{
    internal const string Database = "sess_nexaerp_rev869b_control_plane";
    internal const string Owner = "nexa_rev869b_control_plane_owner";

    internal static readonly string[] Roles =
    [
        Owner, "nexa_rev869b_lifecycle_api", "nexa_rev869b_lifecycle_audit",
        "nexa_rev869b_recovery_executor", "nexa_rev869b_management_writer",
        "nexa_rev869b_control_plane_verifier"
    ];

    internal static readonly string[] Relations =
    [
        "rev869b_control_plane_manifest", "rev869b_database_leases",
        "rev869b_database_lease_events", "rev869b_recovery_decisions",
        "rev869b_lifecycle_attempts", "rev869b_lifecycle_outcomes"
    ];

    internal static readonly string[] PurposeSpecificApis =
    [
        "rev869b_reserve_lease", "rev869b_begin_provisioning", "rev869b_mark_ready",
        "rev869b_mark_in_use", "rev869b_authorize_normal_drop", "rev869b_begin_drop",
        "rev869b_register_recovery_decision", "rev869b_consume_recovery_decision",
        "rev869b_record_cleanup_failure", "rev869b_finalize_absent_target",
        "rev869b_read_lease", "rev869b_read_nonterminal_leases"
    ];

    internal static readonly string[] SafeModes =
        ["GeneratePlanOnly", "PreflightOnly", "PostProvisionVerification"];

    internal static void RequireSafeTarget(string database)
    {
        if (!string.Equals(database, Database, StringComparison.Ordinal))
            throw new InvalidOperationException("Only the exact externally provisioned control plane is permitted.");
    }
}
