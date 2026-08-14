namespace SESS.NexaERP.Tests;

/// <summary>
/// Exact contracts consumed by the 34 separately authorized PostgreSQL acceptance bodies.
/// This inventory contains no source-string or label-only tests.
/// </summary>
internal static class Rev869BAcceptanceScenarioInventory
{
    private static Rev869BLifecycleControllerClient.AcceptanceContract S(string id, string setup, string action,
        string initial, string final, int affected = 1, string? sqlState = null, string? databaseObject = null,
        bool zeroRows = false, bool decision = false) => new(id, setup, action, initial, final, sqlState, databaseObject, affected,
            sqlState is not null, zeroRows, decision);

    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract P01 = S("P01", "Externally provisioned exact cluster and control plane", "Run canonical read-only verifier", "ExternalProvisioned", "ExternalVerified");
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract P02 = S("P02", "Pinned cluster with mismatched source or TLS manifest", "Run external preflight", "ExternalProvisioned", "PreflightDenied", 0, "42501", "rev869b_external_manifest");
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract P03 = S("P03", "Control plane with one changed definition or effective grant", "Run canonical verifier", "ExternalProvisioned", "VerificationDenied", 0, "42501", "rev869b_control_plane_catalogue_acl");

    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract L01 = S("L01", "Reserved exact disposable lease", "Provision target through controller", "Reserved", "Ready");
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract L02 = S("L02", "Reserved lease with deterministic interruption at every create phase", "Restart controller reconciliation", "Provisioning", "Ready");
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract L03 = S("L03", "Lease with active lifecycle attempt and barrier", "Start concurrent lifecycle attempt", "Provisioning", "Provisioning", 0, "40001", "UX_rev869b_one_active_lifecycle_attempt");
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract L04 = S("L04", "DropAuthorized lease and stable cleanup attempt", "Drop and finalize exact target and roles", "DropAuthorized", "Finalized");
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract L05 = S("L05", "Ready target with marker or catalogue mismatch", "Verify use and drop denial then quarantine", "Ready", "Quarantined", 0, "42501", "rev869b_target_identity_mismatch");

    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract R01 = S("R01", "Quarantined lease and valid unconsumed management decision", "Consume exact action and recover", "Quarantined", "Finalized", decision: true);
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract R02 = S("R02", "Already consumed recovery decision", "Replay decision with same and changed action", "RecoveryAuthorized", "RecoveryAuthorized", 0, "42501", "rev869b_recovery_decision_replay", decision: true);
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract R03 = S("R03", "CleanupFailed lease and fresh linked recovery decision", "Recover after deterministic cleanup failure", "CleanupFailed", "Finalized", decision: true);

    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract C01 = S("C01", "Registered request and exact runtime transaction", "Commit protected business rows histories receipt and outcome", "AttemptStarted", "Committed");
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract C02 = S("C02", "Committed command with lost response", "Replay same request and read authoritative receipt", "Committed", "Committed");
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract C03 = S("C03", "Registered idempotency key with different request digest", "Replay changed request", "RequestRegistered", "RequestRegistered", 0, "23505", "rev869b_command_request_replay_mismatch");
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract C04 = S("C04", "Started attempt with deterministic receipt insertion fault", "Attempt business commit", "AttemptStarted", "RolledBack", 0, "P0001", "rev869b_command_receipt");
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract C05 = S("C05", "Opened exact command transaction", "Rollback transaction and record exact terminal outcome", "AttemptStarted", "RolledBack");
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract C06 = S("C06", "Attempts interrupted before open after open during commit and after response", "Restart authoritative reconciler", "AttemptStarted", "Abandoned");
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract C07 = S("C07", "One command request with concurrent attempt barrier", "Start two differently bound attempts", "AttemptStarted", "AttemptStarted", 0, "40001", "UX_rev869b_one_active_command_attempt");
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract C08 = S("C08", "Exact attempt plus substituted backend actor organization role or operation", "Open or terminalize substituted binding", "AttemptStarted", "AttemptStarted", 0, "42501", "rev869b_attempt_binding");

    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract G01 = S("G01", "Missing expired or wrong-organization purge authorization", "Start purge", "Approved", "Denied", 0, "42501", "rev869b_purge_authorization_scope", decision: true);
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract G02 = S("G02", "Fresh scoped authorization with verified zero eligible rows", "Freeze candidate batch", "Approved", "ZeroRows", 0, zeroRows: true, decision: true);
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract G03 = S("G03", "Fresh scoped authorization with eligible temporary contexts and durable histories", "Delete exact frozen candidates and commit success", "Started", "Succeeded", decision: true);
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract G04 = S("G04", "Started purge with deterministic candidate drift", "Execute frozen deletion", "Started", "Failed", 0, "40001", "rev869b_purge_candidate_drift", decision: true);
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract G05 = S("G05", "Started purge with deterministic delete or audit fault", "Rollback deletion then record independent failure", "Started", "Failed", 0, "P0001", "rev869b_purge_delete_failpoint", decision: true);
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract G06 = S("G06", "Concurrent start or execute plus prior failed attempt", "Race and issue exact linked retry authorization", "Started", "Failed", 0, "40001", "rev869b_purge_retry_binding", decision: true);

    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract E01 = S("E01", "Approved organization field row as-of and expiry scope", "Prepare immutable minimized batch", "Approved", "Prepared", decision: true);
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract E02 = S("E02", "Prepared export batch", "Insert later ledger row and reread batch", "Prepared", "Prepared", decision: true);
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract E03 = S("E03", "Expired wrong terminal or concurrently active release", "Read or authorize release", "Prepared", "Denied", 0, "42501", "rev869b_export_release_sequence", decision: true);
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract E04 = S("E04", "ReleaseStarted batch with deterministic delivery loss", "Record Interrupted and authorize new release ID", "ReleaseStarted", "Interrupted", decision: true);

    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract A01 = S("A01", "Canonical control-plane and target packages", "Enumerate every ordinary effective privilege", "Installed", "Verified");
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract A02 = S("A02", "Each runtime purge export recovery and arbitrary ordinary principal", "Attempt every protected direct privilege and ungranted function", "Installed", "Denied", 0, "42501", "rev869b_protected_object_acl");

    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract T01 = S("T01", "Controller request with exact isolated opt-in", "Allocate controller-owned fixture", "Reserved", "InUse");
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract T02 = S("T02", "Any scenario with deterministic failure", "Dispose and restart cleanup", "CleanupFailed", "Finalized");
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract T03 = S("T03", "Two independent controller-owned fixtures and barriers", "Run concurrent actors verify isolation and cleanup", "InUse", "Finalized");

    internal static readonly IReadOnlyList<Rev869BLifecycleControllerClient.AcceptanceContract> All =
    [P01,P02,P03,L01,L02,L03,L04,L05,R01,R02,R03,C01,C02,C03,C04,C05,C06,C07,C08,
     G01,G02,G03,G04,G05,G06,E01,E02,E03,E04,A01,A02,T01,T02,T03];
}
