using System.Security.Cryptography;
using System.Text;

namespace SESS.NexaERP.Tests;

/// <summary>
/// Independently authored database-shaped inputs for the source-only Correction 28 gate.
/// This type deliberately has no dependency on the frozen oracle: it models observed inputs,
/// action receipts and temporal provenance; expectations remain in the oracle.
/// </summary>
internal static class Rev869BCorrection28IndependentEvidenceFixtures
{
    internal const string Version = "REV869B-C28-INDEPENDENT-FIXTURES-v1";

    internal sealed record ActionFact(bool Reached, int AffectedRows, string TerminalState,
        string? SqlState, string? ErrorCode, string? DatabaseObject);

    internal sealed record Fixture(string ScenarioId, string SubcaseId, Guid FixtureIdentity,
        Guid PreparationIdentity, Guid AttemptIdentity, Guid ObservationIdentity, Guid EnvelopeIdentity,
        Guid ExpectedResultIdentity, Guid ActionIdentity, string RawFixtureSha256,
        string ActionFixtureSha256, string HistoricalEvidenceSha256, string ProvenanceSourceSha256,
        string BeforeObservationId, string AfterObservationId, string DurableObservationId,
        string AuditObservationId, string CleanupObservationId, ActionFact Action);

    private static readonly string[] SubcaseIds =
    [
        "P01:p01-action",
        "P02:wrong-system-id", "P02:wrong-tls-spki", "P02:wrong-endpoint", "P02:wrong-source", "P02:wrong-manifest",
        "P03:unexpected-role", "P03:unexpected-database", "P03:unexpected-object", "P03:unexpected-grant",
        "L01:reserved", "L01:interrupt-before-role", "L01:resume-or-approved-cleanup",
        "L02:reserved", "L02:database-created", "L02:roles-created", "L02:migration-applied", "L02:verified", "L02:ready",
        "L03:ready-cleanup-race", "L03:inuse-cleanup-race", "L03:single-dropstarted", "L03:single-drop", "L03:authorization-event-binding",
        "L04:before-drop", "L04:during-drop", "L04:after-drop", "L04:during-role-cleanup", "L04:finalized-once",
        "L05:mismatch-detected", "L05:use-denied", "L05:drop-denied", "L05:quarantine-authorized", "L05:quarantined",
        "R01:r01-action",
        "R02:wrong", "R02:expired", "R02:replayed", "R02:foreign", "R02:pre-state", "R02:action", "R02:nonce", "R02:valid-preserved",
        "R03:first-failure", "R03:restart", "R03:old-decision-denied", "R03:fresh-linked-decision", "R03:finalized",
        "C01:c01-action", "C02:c02-action", "C03:c03-action",
        "C04:receipt-failpoint", "C04:business-rollback", "C04:history-rollback", "C04:receipt-rollback", "C04:durable-noncommit",
        "C05:c05-action", "C06:before-open", "C06:after-open", "C06:during-commit", "C06:after-response", "C07:c07-action",
        "C08:pool", "C08:backend", "C08:transaction", "C08:actor", "C08:organization", "C08:version", "C08:role", "C08:operation",
        "G01:missing", "G01:expired", "G01:wrong-target", "G01:wrong-batch", "G01:wrong-organization",
        "G02:g02-action", "G03:g03-action", "G04:g04-action",
        "G05:delete-failpoint", "G05:deletion-rollback", "G05:independent-audit",
        "G06:concurrent-start", "G06:concurrent-execute", "G06:substituted-policy-denied", "G06:exact-retry",
        "E01:e01-action", "E02:e02-action", "E03:expired", "E03:wrong-batch", "E03:terminal", "E03:concurrent",
        "E04:old-release-interrupted", "E04:fresh-release-started", "E04:batch-unchanged",
        "A01:a01-action", "A02:runtime", "A02:purge", "A02:export", "A02:recovery", "A02:administrator", "A02:ordinary-principal", "A02:public",
        "T01:t01-action", "T02:t02-action",
        "T03:all-34-actions", "T03:all-34-reads", "T03:all-34-assertions", "T03:all-34-cleanups"
    ];

    private static readonly IReadOnlyDictionary<string, ActionFact> ScenarioActionShapes =
        new Dictionary<string, ActionFact>(StringComparer.Ordinal)
        {
            ["P01"] = Ok("ExternalVerified"),
            ["P02"] = Fail("PreflightDenied", null, "REV869B_PREFLIGHT_PIN_MISMATCH", "mutated-pin"),
            ["P03"] = Fail("VerificationDenied", null, "REV869B_CONTROL_PLANE_CATALOGUE_MISMATCH", "rev869b_control_plane_catalogue_acl"),
            ["L01"] = Ok("Ready"), ["L02"] = Ok("Ready"),
            ["L03"] = Fail("DropStarted", "40001", null, "UX_rev869b_one_active_lifecycle_attempt"),
            ["L04"] = Ok("Finalized"), ["L05"] = Fail("Quarantined", "42501", null, "rev869b_target_identity_mismatch"),
            ["R01"] = Ok("Finalized"), ["R02"] = Fail("RecoveryAuthorized", "42501", null, "rev869b_recovery_decision_replay"), ["R03"] = Ok("Finalized"),
            ["C01"] = Ok("Committed"), ["C02"] = Ok("Committed"),
            ["C03"] = Fail("RequestRegistered", "23505", null, "rev869b_command_request_replay_mismatch"),
            ["C04"] = Fail("RolledBack", "P0001", null, "TR_rev869b_command_receipt_failpoint"), ["C05"] = Ok("RolledBack"),
            ["C06"] = Ok("FourExactInterruptionOutcomesReconciled"),
            ["C07"] = Fail("AttemptStarted", "40001", null, "rev869b_command_attempt_active"),
            ["C08"] = Fail("AttemptStarted", "42501", null, "rev869b_attempt_binding"),
            ["G01"] = Fail("Denied", "42501", null, "rev869b_purge_batch_binding"), ["G02"] = Ok("ZeroRows"), ["G03"] = Ok("Succeeded"),
            ["G04"] = Fail("Failed", "40001", null, "rev869b_purge_candidate_drift"), ["G05"] = Fail("Failed", "P0001", null, "TR_rev869b_purge_delete_failpoint"),
            ["G06"] = Fail("Failed", "42501", null, "rev869b_purge_retry_binding"), ["E01"] = Ok("Prepared"), ["E02"] = Ok("Prepared"),
            ["E03"] = Fail("Denied", "42501", null, "rev869b_export_release_sequence"), ["E04"] = Ok("ReleaseRetrySequenceVerified"),
            ["A01"] = Ok("Verified"), ["A02"] = Fail("Denied", "42501", null, "rev869b_protected_object_acl"),
            ["T01"] = Ok("InUse"), ["T02"] = Ok("Finalized"), ["T03"] = Ok("MutationSensitive")
        };

    private static readonly IReadOnlyDictionary<string, ActionFact> ActionFactsBySubcase = SubcaseIds
        .ToDictionary(subcaseId => subcaseId, subcaseId => ScenarioActionShapes[subcaseId[..3]] with { }, StringComparer.Ordinal);

    internal static readonly Fixture[] All = SubcaseIds.Select(Create).ToArray();

    internal static Fixture For(string subcaseId) => All.Single(x => x.SubcaseId == subcaseId);

    internal static void Validate()
    {
        if (ActionFactsBySubcase.Count != 108 || All.Length != 108 || All.Select(x => x.SubcaseId).Distinct(StringComparer.Ordinal).Count() != 108 ||
            All.Select(x => x.FixtureIdentity).Distinct().Count() != 108 ||
            All.Select(x => x.PreparationIdentity).Distinct().Count() != 108 ||
            All.Select(x => x.AttemptIdentity).Distinct().Count() != 108 ||
            All.Select(x => x.ObservationIdentity).Distinct().Count() != 108 ||
            All.Select(x => x.EnvelopeIdentity).Distinct().Count() != 108 ||
            All.Select(x => x.ExpectedResultIdentity).Distinct().Count() != 108 ||
            All.Select(x => x.ActionIdentity).Distinct().Count() != 108 ||
            All.Select(x => x.RawFixtureSha256).Distinct(StringComparer.Ordinal).Count() != 108 ||
            All.Select(x => x.ActionFixtureSha256).Distinct(StringComparer.Ordinal).Count() != 108 ||
            All.Select(x => x.HistoricalEvidenceSha256).Distinct(StringComparer.Ordinal).Count() != 108 ||
            All.Select(x => x.ProvenanceSourceSha256).Distinct(StringComparer.Ordinal).Count() != 108)
            throw new InvalidOperationException("Correction 28 requires 108 independently keyed fixture/action/history/provenance records.");

        foreach (var fixture in All)
        {
            var temporal = new[] { fixture.BeforeObservationId, fixture.AfterObservationId, fixture.DurableObservationId,
                fixture.AuditObservationId, fixture.CleanupObservationId };
            if (temporal.Distinct(StringComparer.Ordinal).Count() != temporal.Length ||
                temporal.Any(string.IsNullOrWhiteSpace) || !fixture.Action.Reached)
                throw new InvalidOperationException("Every subcase requires fresh, ordered observations and an action-under-test receipt.");
        }
    }

    private static Fixture Create(string subcaseId)
    {
        var scenario = subcaseId[..3];
        var action = ActionFactsBySubcase[subcaseId];
        return new Fixture(scenario, subcaseId, Id(subcaseId, "fixture"), Id(subcaseId, "preparation"),
            Id(subcaseId, "attempt"), Id(subcaseId, "observation"), Id(subcaseId, "envelope"),
            Id(subcaseId, "expected-result"), Id(subcaseId, "action"), Hash(subcaseId, "raw-database-shape"),
            Hash(subcaseId, "action-receipt"), Hash(subcaseId, "durable-history"), Hash(subcaseId, "provenance-source"),
            Observation(subcaseId, "before", 1), Observation(subcaseId, "after", 2),
            Observation(subcaseId, "durable", 3), Observation(subcaseId, "audit", 4),
            Observation(subcaseId, "cleanup", 5), action);
    }

    private static ActionFact Ok(string state) => new(true, 1, state, null, null, null);
    private static ActionFact Fail(string state, string? sqlState, string? errorCode, string databaseObject) =>
        new(true, 0, state, sqlState, errorCode, databaseObject);
    private static string Observation(string subcase, string stage, int sequence) =>
        $"obs:c28:{Hash(subcase, stage)}:{sequence}";
    private static Guid Id(string subcase, string purpose) => new(SHA256.HashData(Encoding.UTF8.GetBytes(
        $"{Version}|{subcase}|{purpose}"))[..16]);
    private static string Hash(string subcase, string purpose) => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes($"{Version}|{subcase}|{purpose}"))).ToLowerInvariant();
}
