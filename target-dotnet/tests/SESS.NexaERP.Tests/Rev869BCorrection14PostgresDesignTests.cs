namespace SESS.NexaERP.Tests;

/// <summary>
/// Exact contracts consumed by the 34 separately authorized PostgreSQL acceptance bodies.
/// This inventory contains no source-string or label-only tests.
/// </summary>
internal static class Rev869BAcceptanceScenarioInventory
{
    private sealed record ScenarioExpectedResult(int AffectedRows, string? SqlState, string? DatabaseObject,
        bool AllowsZeroRows, bool RequiresDecision, int BeforeCount, int AfterCount, string TerminalOutcome);

    private static Rev869BLifecycleControllerClient.AcceptanceContract S(string id, string setup, string action,
        string initial, string final, ScenarioExpectedResult expected) =>
        new(id, setup, action, initial, final, expected.SqlState, expected.DatabaseObject, expected.AffectedRows,
            expected.SqlState is not null, expected.AllowsZeroRows, expected.RequiresDecision, Identity(id),
            expected.BeforeCount, expected.AfterCount, expected.TerminalOutcome, "Finalized",
            Manifest(id, Identity(id)), Requirements(id, action, EvidenceQuery(id), initial, final, expected));

    private static Rev869BLifecycleControllerClient.ScenarioFixtureManifest Manifest(string id,
        Rev869BLifecycleControllerClient.DatabaseObjectIdentity identity) => new(
            "rev869b/" + id + "/fixture/v1", "rev869b/" + id + "/action/v1", EvidenceQuery(id),
            "rev869b/" + id + "/cleanup/v1", identity, FixtureDdl(id), CleanupDdl(id));

    private static IReadOnlyList<string> FixtureDdl(string id) => id switch
    {
        "C04" => ["CREATE FUNCTION nexa.rev869b_test_c04_receipt_failpoint() RETURNS trigger LANGUAGE plpgsql AS $f$ BEGIN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='C04 receipt failpoint'; END $f$", "CREATE TRIGGER TR_rev869b_command_receipt_failpoint BEFORE INSERT ON nexa.rev869b_command_receipts FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_test_c04_receipt_failpoint()"],
        "G05" => ["CREATE FUNCTION nexa.rev869b_test_g05_purge_delete_failpoint() RETURNS trigger LANGUAGE plpgsql AS $f$ BEGIN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='G05 purge failpoint'; END $f$", "CREATE TRIGGER TR_rev869b_purge_delete_failpoint BEFORE DELETE ON nexa.rev869b_command_contexts FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_test_g05_purge_delete_failpoint()"],
        _ => Array.Empty<string>()
    };

    private static IReadOnlyList<string> CleanupDdl(string id) => id switch
    {
        "C04" => ["DROP TRIGGER TR_rev869b_command_receipt_failpoint ON nexa.rev869b_command_receipts", "DROP FUNCTION nexa.rev869b_test_c04_receipt_failpoint()"],
        "G05" => ["DROP TRIGGER TR_rev869b_purge_delete_failpoint ON nexa.rev869b_command_contexts", "DROP FUNCTION nexa.rev869b_test_g05_purge_delete_failpoint()"],
        _ => Array.Empty<string>()
    };

    private static string EvidenceQuery(string id) => id switch
    {
        "P01" or "P02" or "P03" => "nexa.rev869b_control_plane_catalogue_fingerprint()",
        "L01" or "L02" or "L03" or "L04" or "L05" or "R01" or "R02" or "R03" or "T01" or "T02" or "T03" => "nexa.rev869b_read_lease(uuid)",
        "C01" or "C02" or "C03" or "C04" or "C05" or "C06" or "C07" or "C08" => "nexa.rev869b_reconcile_command_attempt(uuid)",
        "G01" or "G02" or "G03" or "G04" or "G05" or "G06" => "nexa.rev869b_reconcile_purge(uuid)",
        "E01" or "E02" or "E03" or "E04" => "nexa.rev869b_read_prepared_export_batch(uuid,uuid)",
        "A01" or "A02" => "nexa.rev869b_verify_target_catalogue_acl()",
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown REV869B evidence query")
    };

    private static IReadOnlyList<Rev869BLifecycleControllerClient.SubcaseRequirement> Requirements(
        string id, string action, string evidenceSource, string initial, string final, ScenarioExpectedResult expected) =>
        EvidenceKeys(id).Select(key => Requirement(id, key, action, evidenceSource, initial, final, expected)).ToArray();

    private static Rev869BLifecycleControllerClient.SubcaseRequirement Requirement(string id, string key,
        string action, string evidenceSource, string initial, string final, ScenarioExpectedResult expected)
    {
        var terminal = (id, key) switch
        {
            ("L03", "ready-cleanup-race") => "ReadyRaceOneDrop",
            ("L03", "inuse-cleanup-race") => "InUseRaceOneDrop",
            ("L03", "single-dropstarted") => "DropStartedOnce",
            ("L03", "single-drop") => "DropExecutedOnce",
            ("R02", "valid-preserved") => "RecoveryAuthorizedPreserved",
            ("R02", _) => "Denied",
            ("C04", "receipt-failpoint") => "ReceiptInsertFailed",
            ("C04", "business-rollback") => "BusinessAndHistoryRolledBack",
            ("C04", "history-rollback") => "BusinessAndHistoryRolledBack",
            ("C04", "receipt-rollback") => "ReceiptRolledBack",
            ("C04", "durable-noncommit") => "RolledBack",
            ("C06", "before-open") => "Abandoned",
            ("C06", "after-open") => "Abandoned",
            ("C06", "during-commit") => "RolledBack",
            ("C06", "after-response") => "Committed",
            ("G05", "delete-failpoint") => "DeleteFailed",
            ("G05", "deletion-rollback") => "DeletionRolledBack",
            ("G05", "independent-audit") => "Failed",
            ("G06", "concurrent-start") => "OneStartWinner",
            ("G06", "concurrent-execute") => "OneExecutionWinner",
            ("G06", "substituted-policy-denied") => "Denied",
            ("G06", "exact-retry") => "RetryStarted",
            ("E04", "old-release-interrupted") => "Interrupted",
            ("E04", "fresh-release-started") => "ReleaseStarted",
            ("E04", "batch-unchanged") => "PreparedBatchUnchanged",
            ("T03", _) => "AllContractMutationsRejected",
            _ => expected.TerminalOutcome
        };
        var successfulVariant = (id, key) is ("R02", "valid-preserved") or ("G06", "exact-retry");
        var sqlState = successfulVariant ? null : expected.SqlState;
        var databaseObject = successfulVariant ? null : expected.DatabaseObject;
        var affected = successfulVariant ? 1 : expected.AffectedRows;
        var postState = id is "C06" or "E04" ? terminal : final;
        return new(id + ":" + key, action + ":" + key, evidenceSource, initial, postState,
            expected.BeforeCount, expected.AfterCount, sqlState, databaseObject, affected, terminal);
    }

    private static IReadOnlyList<string> EvidenceKeys(string id) => id switch
    {
        "P02" => ["wrong-system-id","wrong-tls-spki","wrong-endpoint","wrong-source","wrong-manifest"],
        "P03" => ["unexpected-role","unexpected-database","unexpected-object","unexpected-grant"],
        "L01" => ["reserved","interrupt-before-role","resume-or-approved-cleanup"],
        "L02" => ["reserved","database-created","roles-created","migration-applied","verified","ready"],
        "L03" => ["ready-cleanup-race","inuse-cleanup-race","single-dropstarted","single-drop"],
        "L04" => ["before-drop","during-drop","after-drop","during-role-cleanup","finalized-once"],
        "L05" => ["mismatch-detected","use-denied","drop-denied","quarantine-authorized","quarantined"],
        "R02" => ["wrong","expired","replayed","foreign","pre-state","action","nonce","valid-preserved"],
        "R03" => ["first-failure","restart","old-decision-denied","fresh-linked-decision","finalized"],
        "C04" => ["receipt-failpoint","business-rollback","history-rollback","receipt-rollback","durable-noncommit"],
        "C06" => ["before-open","after-open","during-commit","after-response"],
        "C08" => ["pool","backend","transaction","actor","organization","version","role","operation"],
        "G01" => ["missing","expired","wrong-target","wrong-batch","wrong-organization"],
        "G05" => ["delete-failpoint","deletion-rollback","independent-audit"],
        "G06" => ["concurrent-start","concurrent-execute","substituted-policy-denied","exact-retry"],
        "E03" => ["expired","wrong-batch","terminal","concurrent"],
        "E04" => ["old-release-interrupted","fresh-release-started","batch-unchanged"],
        "A02" => ["runtime","purge","export","recovery","administrator","ordinary-principal","public"],
        "T03" => ["all-34-actions-mutation-sensitive"],
        _ => [id.ToLowerInvariant()+"-action"]
    };
    private static Rev869BLifecycleControllerClient.DatabaseObjectIdentity Identity(string id) => id switch
    {
        "P01" => new("nexa", "rev869b_control_plane_manifest", "rev869b_control_plane_manifest_pkey", "nexa.rev869b_control_plane_catalogue_fingerprint()", "TR_rev869b_lease_events_immutable"),
        "P02" => new("pg_catalog", "pg_database", string.Empty, "pg_catalog.int4div(integer,integer)", string.Empty),
        "P03" => new("nexa", "rev869b_control_plane_manifest", string.Empty, "pg_catalog.int4div(integer,integer)", "TR_rev869b_lease_events_immutable"),
        "L01" => new("nexa", "rev869b_database_leases", "rev869b_database_leases_pkey", "nexa.rev869b_mark_ready(uuid,bigint,uuid,text,text,text)", "TR_rev869b_lease_events_immutable"),
        "L02" => new("nexa", "rev869b_lifecycle_attempts", "UX_rev869b_one_active_lifecycle_attempt", "nexa.rev869b_begin_provisioning(uuid,bigint,uuid,uuid,uuid,text,text,text,text)", "TR_rev869b_lease_events_immutable"),
        "L03" => new("nexa", "rev869b_lifecycle_attempts", "UX_rev869b_one_active_lifecycle_attempt", "nexa.rev869b_begin_provisioning(uuid,bigint,uuid,uuid,uuid,text,text,text,text)", "TR_rev869b_lease_events_immutable"),
        "L04" => new("nexa", "rev869b_lifecycle_outcomes", "rev869b_lifecycle_outcomes_attemptid_key", "nexa.rev869b_finalize_absent_target(uuid,text,text,text)", "TR_rev869b_lifecycle_outcomes_immutable"),
        "L05" => new("nexa", "rev869b_quarantine_outcomes", "rev869b_quarantine_outcomes_attemptid_key", "nexa.rev869b_record_quarantine(uuid,bigint,uuid,uuid,text,text,text,text)", "TR_rev869b_quarantine_outcomes_immutable"),
        "R01" or "R02" or "R03" => new("nexa", "rev869b_recovery_decisions", "rev869b_recovery_decisions_pkey", "nexa.rev869b_consume_recovery_decision(uuid,bigint,uuid,uuid,text,uuid,uuid,text,text,text,text)", "TR_rev869b_recovery_decisions_immutable"),
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

    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract P01 = S("P01", "Externally provisioned exact cluster and control plane", "Run canonical read-only verifier", "ExternalProvisioned", "ExternalVerified", new(1, null, null, false, false, 1, 1, "ExternalVerified"));
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract P02 = S("P02", "Pinned cluster with mismatched source or TLS manifest", "Run external preflight", "ExternalProvisioned", "PreflightDenied", new(0, "22012", "pg_catalog.int4div(integer,integer)", false, false, 1, 1, "PreflightDenied"));
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract P03 = S("P03", "Control plane with one changed definition or effective grant", "Run canonical verifier", "ExternalProvisioned", "VerificationDenied", new(0, "22012", "pg_catalog.int4div(integer,integer)", false, false, 1, 1, "VerificationDenied"));

    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract L01 = S("L01", "Reserved lease interrupted after reservation before role creation", "Resume the same attempt or execute separately approved cleanup", "Reserved", "Ready", new(1, null, null, false, false, 1, 1, "Ready"));
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract L02 = S("L02", "Reserved lease with deterministic interruption at every create phase", "Restart controller reconciliation", "Provisioning", "Ready", new(1, null, null, false, false, 1, 1, "Ready"));
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract L03 = S("L03", "Ready and InUse leases with two normal cleanup requests at a barrier", "Race cleanup and prove one DropStarted and one DROP", "Ready", "DropStarted", new(0, "40001", "UX_rev869b_one_active_lifecycle_attempt", false, false, 1, 1, "DropStarted"));
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract L04 = S("L04", "DropStarted leases interrupted before during and after DROP and role cleanup", "Restart and reconcile every cleanup boundary to one Finalized", "DropStarted", "Finalized", new(1, null, null, false, false, 1, 1, "Finalized"));
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract L05 = S("L05", "Ready target with marker or catalogue mismatch", "Verify use and drop denial then quarantine", "Ready", "Quarantined", new(0, "42501", "rev869b_target_identity_mismatch", false, false, 1, 1, "Quarantined"));

    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract R01 = S("R01", "Quarantined lease and valid unconsumed management decision", "Consume exact action and recover", "Quarantined", "Finalized", new(1, null, null, false, true, 1, 1, "Finalized"));
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract R02 = S("R02", "Already consumed recovery decision", "Replay decision with same and changed action", "RecoveryAuthorized", "RecoveryAuthorized", new(0, "42501", "rev869b_recovery_decision_replay", false, true, 1, 1, "RecoveryAuthorized"));
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract R03 = S("R03", "CleanupFailed lease and fresh linked recovery decision", "Recover after deterministic cleanup failure", "CleanupFailed", "Finalized", new(1, null, null, false, true, 1, 1, "Finalized"));

    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract C01 = S("C01", "Registered request and exact runtime transaction", "Commit protected business rows histories receipt and outcome", "AttemptStarted", "Committed", new(1, null, null, false, false, 1, 1, "Committed"));
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract C02 = S("C02", "Committed command with lost response", "Replay same request and read authoritative receipt", "Committed", "Committed", new(1, null, null, false, false, 1, 1, "Committed"));
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract C03 = S("C03", "Registered idempotency key with different request digest", "Replay changed request", "RequestRegistered", "RequestRegistered", new(0, "23505", "rev869b_command_request_replay_mismatch", false, false, 1, 1, "RequestRegistered"));
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract C04 = S("C04", "Started attempt with exact fixture trigger TR_rev869b_command_receipt_failpoint", "Attempt business commit", "AttemptStarted", "RolledBack", new(0, "P0001", "TR_rev869b_command_receipt_failpoint", false, false, 1, 1, "RolledBack"));
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract C05 = S("C05", "Opened exact command transaction", "Rollback transaction and record exact terminal outcome", "AttemptStarted", "RolledBack", new(1, null, null, false, false, 1, 1, "RolledBack"));
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract C06 = S("C06", "Attempts interrupted before open after open during commit and after response", "Restart authoritative reconciler", "AttemptStarted", "Reconciled", new(1, null, null, false, false, 1, 1, "FourExactInterruptionOutcomesReconciled"));
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract C07 = S("C07", "One command request with concurrent attempt barrier", "Start two differently bound attempts", "AttemptStarted", "AttemptStarted", new(0, "40001", "rev869b_command_attempt_active", false, false, 1, 1, "AttemptStarted"));
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract C08 = S("C08", "Exact attempt plus substituted backend actor organization role or operation", "Open or terminalize substituted binding", "AttemptStarted", "AttemptStarted", new(0, "42501", "rev869b_attempt_binding", false, false, 1, 1, "AttemptStarted"));

    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract G01 = S("G01", "Missing expired wrong-target wrong-batch or wrong-organization purge authorization", "Start purge", "Approved", "Denied", new(0, "42501", "rev869b_purge_batch_binding", false, true, 1, 1, "Denied"));
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract G02 = S("G02", "Fresh scoped authorization with verified zero eligible rows", "Freeze candidate batch", "Approved", "ZeroRows", new(0, null, null, true, true, 0, 0, "ZeroRows"));
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract G03 = S("G03", "Fresh scoped authorization with eligible temporary contexts and durable histories", "Delete exact frozen candidates and commit success", "Started", "Succeeded", new(1, null, null, false, true, 1, 1, "Succeeded"));
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract G04 = S("G04", "Started purge with deterministic candidate drift", "Execute frozen deletion", "Started", "Failed", new(0, "40001", "rev869b_purge_candidate_drift", false, true, 1, 1, "Failed"));
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract G05 = S("G05", "Started purge with exact fixture trigger TR_rev869b_purge_delete_failpoint", "Rollback deletion then record independent failure", "Started", "Failed", new(0, "P0001", "TR_rev869b_purge_delete_failpoint", false, true, 1, 1, "Failed"));
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract G06 = S("G06", "Concurrent start or execute plus prior failed attempt", "Race then reject substituted retry and accept one monotonic exact retry", "Started", "Failed", new(0, "42501", "rev869b_purge_retry_binding", false, true, 1, 1, "Failed"));

    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract E01 = S("E01", "Approved organization field row as-of and expiry scope", "Prepare immutable minimized batch", "Approved", "Prepared", new(1, null, null, false, true, 1, 1, "Prepared"));
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract E02 = S("E02", "Prepared export batch", "Insert later ledger row and reread batch", "Prepared", "Prepared", new(1, null, null, false, true, 1, 1, "Prepared"));
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract E03 = S("E03", "Expired wrong terminal or concurrently active release", "Read or authorize release", "Prepared", "Denied", new(0, "42501", "rev869b_export_release_sequence", false, true, 1, 1, "Denied"));
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract E04 = S("E04", "ReleaseStarted batch with deterministic delivery loss", "Record Interrupted and authorize new release ID", "ReleaseStarted", "ReleaseStarted", new(1, null, null, false, true, 1, 1, "ReleaseRetrySequenceVerified"));

    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract A01 = S("A01", "Canonical control-plane and target packages", "Enumerate every ordinary effective privilege", "Installed", "Verified", new(1, null, null, false, false, 1, 1, "Verified"));
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract A02 = S("A02", "Each runtime purge export recovery and arbitrary ordinary principal", "Attempt every protected direct privilege and ungranted function", "Installed", "Denied", new(0, "42501", "rev869b_protected_object_acl", false, false, 1, 1, "Denied"));

    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract T01 = S("T01", "Controller request with exact isolated opt-in", "Allocate controller-owned fixture", "Reserved", "InUse", new(1, null, null, false, false, 1, 1, "InUse"));
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract T02 = S("T02", "L04 during-DROP fixture with deterministic controller process failure", "Dispose restart and reconcile the exact surviving cleanup attempt", "CleanupFailed", "Finalized", new(1, null, null, false, false, 1, 1, "Finalized"));
    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract T03 = S("T03", "All 34 pristine source contracts", "Remove each intended action and require its offline mutation test to fail", "SourceComplete", "MutationSensitive", new(1, null, null, false, false, 1, 1, "MutationSensitive"));

    internal static readonly IReadOnlyList<Rev869BLifecycleControllerClient.AcceptanceContract> All =
    [P01,P02,P03,L01,L02,L03,L04,L05,R01,R02,R03,C01,C02,C03,C04,C05,C06,C07,C08,
     G01,G02,G03,G04,G05,G06,E01,E02,E03,E04,A01,A02,T01,T02,T03];
}
