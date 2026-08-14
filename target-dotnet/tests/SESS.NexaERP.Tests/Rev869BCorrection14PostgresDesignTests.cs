namespace SESS.NexaERP.Tests;

/// <summary>
/// Exact contracts consumed by the 34 separately authorized PostgreSQL acceptance bodies.
/// This inventory contains no source-string or label-only tests.
/// </summary>
internal static class Rev869BAcceptanceScenarioInventory
{
    private static Rev869BLifecycleControllerClient.AcceptanceContract S(string id, string setup, string action,
        string initial, string final, int affected = 1, string? sqlState = null, string? databaseObject = null,
        bool zeroRows = false, bool decision = false, int? before = null, int? after = null, string? terminal = null) =>
        new(id, setup, action, initial, final, sqlState, databaseObject, affected, sqlState is not null, zeroRows, decision,
            Identity(id), before ?? (zeroRows ? 0 : 1), after ?? (sqlState is not null ? before ?? 1 : zeroRows ? 0 : 1),
            terminal ?? final, "Finalized");

    private static Rev869BLifecycleControllerClient.DatabaseObjectIdentity Identity(string id) => id switch
    {
        "P01" => new("nexa", "rev869b_control_plane_manifest", "rev869b_control_plane_manifest_pkey", "nexa.rev869b_control_plane_catalogue_fingerprint()", "TR_rev869b_lease_events_immutable"),
        "P02" => new("pg_catalog", "pg_database", string.Empty, "pg_catalog.int4div(integer,integer)", string.Empty),
        "P03" => new("nexa", "rev869b_control_plane_manifest", string.Empty, "pg_catalog.int4div(integer,integer)", "TR_rev869b_lease_events_immutable"),
        "L01" => new("nexa", "rev869b_database_leases", "rev869b_database_leases_pkey", "nexa.rev869b_mark_ready(uuid,bigint,uuid,text,text,text)", "TR_rev869b_lease_events_immutable"),
        "L02" => new("nexa", "rev869b_lifecycle_attempts", "UX_rev869b_one_active_lifecycle_attempt", "nexa.rev869b_begin_provisioning(uuid,bigint,uuid,uuid,text)", "TR_rev869b_lease_events_immutable"),
        "L03" => new("nexa", "rev869b_lifecycle_attempts", "UX_rev869b_one_active_lifecycle_attempt", "nexa.rev869b_begin_provisioning(uuid,bigint,uuid,uuid,text)", "TR_rev869b_lease_events_immutable"),
        "L04" => new("nexa", "rev869b_lifecycle_outcomes", "rev869b_lifecycle_outcomes_attemptid_key", "nexa.rev869b_finalize_absent_target(uuid,text,text,text)", "TR_rev869b_lifecycle_outcomes_immutable"),
        "L05" => new("nexa", "rev869b_quarantine_outcomes", "rev869b_quarantine_outcomes_attemptid_key", "nexa.rev869b_record_quarantine(uuid,bigint,uuid,uuid,uuid,text,text,text,text,text,text,text)", "TR_rev869b_quarantine_outcomes_immutable"),
        "R01" or "R02" or "R03" => new("nexa", "rev869b_recovery_decisions", "rev869b_recovery_decisions_pkey", "nexa.rev869b_consume_recovery_decision(uuid,bigint,uuid,uuid,text,uuid,text)", "TR_rev869b_recovery_decisions_immutable"),
        "C01" or "C02" => new("nexa", "rev869b_command_receipts", "rev869b_command_receipts_attemptid_key", "nexa.rev869b_commit_command_attempt(uuid,bytea,jsonb,uuid)", "TR_rev869b_command_receipts_immutable"),
        "C04" => new("nexa", "rev869b_command_receipts", string.Empty, "nexa.rev869b_commit_command_attempt(uuid,bytea,jsonb,uuid)", "TR_rev869b_command_receipt_failpoint"),
        "C03" => new("nexa", "rev869b_command_requests", "rev869b_command_request_replay_mismatch", "nexa.rev869b_register_command_request(text,text,bytea,bytea,uuid,text,text,text)", string.Empty),
        "C05" or "C06" => new("nexa", "rev869b_command_attempt_outcomes", "rev869b_command_attempt_outcomes_attemptid_key", "nexa.rev869b_record_noncommit_outcome(uuid,uuid,bytea,bytea,text,text,uuid)", "TR_rev869b_command_outcomes_immutable"),
        "C07" => new("nexa", "rev869b_command_attempts", "rev869b_command_attempt_active", "nexa.rev869b_start_command_attempt(uuid,uuid,bytea,bytea,name,integer,bigint)", string.Empty),
        "C08" => new("nexa", "rev869b_command_attempts", "rev869b_attempt_binding", "nexa.rev869b_open_command_attempt(uuid,uuid,text,text,text,text,bytea,jsonb)", string.Empty),
        "G01" or "G06" => new("nexa", "rev869b_purge_authorizations", "rev869b_purge_retry_binding", "nexa.rev869b_register_purge_authorization(uuid,uuid,uuid,uuid,uuid,bytea,text,text,timestamp with time zone,integer,bytea,text,bytea,timestamp with time zone)", "TR_rev869b_purge_events_immutable"),
        "G02" or "G03" or "G04" => new("nexa", "rev869b_purge_attempts", "rev869b_purge_candidate_drift", "nexa.rev869b_execute_purge(uuid)", "TR_rev869b_purge_events_immutable"),
        "G05" => new("nexa", "rev869b_command_contexts", string.Empty, "nexa.rev869b_execute_purge(uuid)", "TR_rev869b_purge_delete_failpoint"),
        "E01" or "E02" => new("nexa", "rev869b_export_batches", "rev869b_export_batches_pkey", "nexa.rev869b_prepare_export_batch(uuid,uuid)", "TR_rev869b_export_rows_immutable"),
        "E03" or "E04" => new("nexa", "rev869b_export_releases", "rev869b_export_release_sequence", "nexa.rev869b_authorize_export_release(uuid,uuid)", "TR_rev869b_export_rows_immutable"),
        "A01" or "A02" => new("nexa", "rev869b_target_catalogue_manifest", "rev869b_target_catalogue_manifest_singleton", "nexa.rev869b_verify_target_catalogue_acl()", string.Empty),
        "T01" or "T02" or "T03" => new("nexa", "rev869b_database_leases", "rev869b_database_leases_targetdatabase_key", "nexa.rev869b_read_lease(uuid)", "TR_rev869b_lease_events_immutable"),
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown REV869B acceptance scenario")
    };

    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract P01 = S("P01", "Externally provisioned exact cluster and control plane", "Run canonical read-only verifier", "ExternalProvisioned", "ExternalVerified");
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract P02 = S("P02", "Pinned cluster with mismatched source or TLS manifest", "Run external preflight", "ExternalProvisioned", "PreflightDenied", 0, "22012", "pg_catalog.int4div(integer,integer)");
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract P03 = S("P03", "Control plane with one changed definition or effective grant", "Run canonical verifier", "ExternalProvisioned", "VerificationDenied", 0, "22012", "pg_catalog.int4div(integer,integer)");

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
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract C04 = S("C04", "Started attempt with exact fixture trigger TR_rev869b_command_receipt_failpoint", "Attempt business commit", "AttemptStarted", "RolledBack", 0, "P0001", "TR_rev869b_command_receipt_failpoint");
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract C05 = S("C05", "Opened exact command transaction", "Rollback transaction and record exact terminal outcome", "AttemptStarted", "RolledBack");
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract C06 = S("C06", "Attempts interrupted before open after open during commit and after response", "Restart authoritative reconciler", "AttemptStarted", "Reconciled", terminal: "CommittedRolledBackOrAbandoned");
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract C07 = S("C07", "One command request with concurrent attempt barrier", "Start two differently bound attempts", "AttemptStarted", "AttemptStarted", 0, "40001", "rev869b_command_attempt_active");
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract C08 = S("C08", "Exact attempt plus substituted backend actor organization role or operation", "Open or terminalize substituted binding", "AttemptStarted", "AttemptStarted", 0, "42501", "rev869b_attempt_binding");

    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract G01 = S("G01", "Missing expired wrong-target wrong-batch or wrong-organization purge authorization", "Start purge", "Approved", "Denied", 0, "42501", "rev869b_purge_batch_binding", decision: true);
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract G02 = S("G02", "Fresh scoped authorization with verified zero eligible rows", "Freeze candidate batch", "Approved", "ZeroRows", 0, zeroRows: true, decision: true);
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract G03 = S("G03", "Fresh scoped authorization with eligible temporary contexts and durable histories", "Delete exact frozen candidates and commit success", "Started", "Succeeded", decision: true);
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract G04 = S("G04", "Started purge with deterministic candidate drift", "Execute frozen deletion", "Started", "Failed", 0, "40001", "rev869b_purge_candidate_drift", decision: true);
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract G05 = S("G05", "Started purge with exact fixture trigger TR_rev869b_purge_delete_failpoint", "Rollback deletion then record independent failure", "Started", "Failed", 0, "P0001", "TR_rev869b_purge_delete_failpoint", decision: true);
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract G06 = S("G06", "Concurrent start or execute plus prior failed attempt", "Race then reject substituted retry and accept one monotonic exact retry", "Started", "Failed", 0, "42501", "rev869b_purge_retry_binding", decision: true);

    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract E01 = S("E01", "Approved organization field row as-of and expiry scope", "Prepare immutable minimized batch", "Approved", "Prepared", decision: true);
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract E02 = S("E02", "Prepared export batch", "Insert later ledger row and reread batch", "Prepared", "Prepared", decision: true);
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract E03 = S("E03", "Expired wrong terminal or concurrently active release", "Read or authorize release", "Prepared", "Denied", 0, "42501", "rev869b_export_release_sequence", decision: true);
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract E04 = S("E04", "ReleaseStarted batch with deterministic delivery loss", "Record Interrupted and authorize new release ID", "ReleaseStarted", "ReleaseStarted", decision: true, terminal: "InterruptedThenReleaseStarted");

    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract A01 = S("A01", "Canonical control-plane and target packages", "Enumerate every ordinary effective privilege", "Installed", "Verified");
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract A02 = S("A02", "Each runtime purge export recovery and arbitrary ordinary principal", "Attempt every protected direct privilege and ungranted function", "Installed", "Denied", 0, "42501", "rev869b_protected_object_acl");

    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract T01 = S("T01", "Controller request with exact isolated opt-in", "Allocate controller-owned fixture", "Reserved", "InUse");
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract T02 = S("T02", "Any scenario with deterministic failure", "Dispose and restart cleanup", "CleanupFailed", "Finalized");
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract T03 = S("T03", "Two independent controller-owned fixtures and barriers", "Run concurrent actors verify isolation and cleanup", "InUse", "Finalized");

    internal static readonly IReadOnlyList<Rev869BLifecycleControllerClient.AcceptanceContract> All =
    [P01,P02,P03,L01,L02,L03,L04,L05,R01,R02,R03,C01,C02,C03,C04,C05,C06,C07,C08,
     G01,G02,G03,G04,G05,G06,E01,E02,E03,E04,A01,A02,T01,T02,T03];
}
