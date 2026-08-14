namespace SESS.NexaERP.Tests;

/// <summary>
/// Pure model of the controller-owned lifecycle contract. Test processes never connect to the
/// control plane or receive lifecycle-administrator credentials.
/// </summary>
internal static class Rev869BControlPlaneRegistry
{
    internal enum LeaseState
    {
        Reserved, Provisioning, Ready, InUse, DropAuthorized, DropStarted,
        Quarantined, RecoveryAuthorized, CleanupFailed, Finalized
    }

    internal sealed record LeaseSnapshot(Guid LeaseId, long Version, LeaseState State,
        string ClusterSystemIdentifier, string TlsSpkiSha256, string Endpoint,
        string SourceCommit, string ManifestSha256, string TargetDatabase,
        Guid? ActiveAttemptId);

    internal sealed record RecoveryDecision(Guid DecisionId, Guid LeaseId,
        LeaseState ExpectedPreState, string AuthorizedAction, string NonceSha256,
        DateTimeOffset ExpiresAt, DateTimeOffset? ConsumedAt);

    internal sealed record FinalizationEvidence(Guid AttemptId, string AbsenceSha256,
        string RolesCleanupSha256, LeaseState TerminalState);

    internal sealed record LifecycleAttemptAuthority(Guid AttemptId, Guid LeaseId, string Kind,
        Guid ExecutionInstanceId, string ActorId, string ActorIssuer, string Operation,
        Guid RegistrationRequestId, string AuthorityEvidenceSha256);

    internal sealed record QuarantineEvidence(Guid OutcomeId, Guid LeaseId, Guid RequestId,
        Guid AttemptId, long SourceLeaseVersion, long TerminalLeaseVersion, string EvidenceSha256);

    internal static bool AuthorizesQuarantine(LifecycleAttemptAuthority authority,
        LeaseSnapshot lease, Guid requestId, Guid executionInstanceId, string actorId,
        string actorIssuer, string operation) =>
        authority.Kind == "Quarantine" && authority.LeaseId == lease.LeaseId &&
        authority.AttemptId == lease.ActiveAttemptId && authority.RegistrationRequestId == requestId &&
        authority.ExecutionInstanceId == executionInstanceId && executionInstanceId != Guid.Empty &&
        authority.AuthorityEvidenceSha256.Length == 64 && authority.AuthorityEvidenceSha256.All(Uri.IsHexDigit) &&
        string.Equals(authority.ActorId, actorId, StringComparison.Ordinal) &&
        string.Equals(authority.ActorIssuer, actorIssuer, StringComparison.Ordinal) &&
        string.Equals(authority.Operation, operation, StringComparison.Ordinal);

    internal static bool IsLegal(LeaseState from, LeaseState to) => (from, to) switch
    {
        (LeaseState.Reserved, LeaseState.Provisioning) => true,
        (LeaseState.Provisioning, LeaseState.Ready) => true,
        (LeaseState.Ready, LeaseState.InUse) => true,
        (LeaseState.Ready or LeaseState.InUse, LeaseState.DropAuthorized) => true,
        (LeaseState.DropAuthorized or LeaseState.RecoveryAuthorized, LeaseState.DropStarted) => true,
        (LeaseState.Reserved or LeaseState.Provisioning or LeaseState.Ready or LeaseState.InUse,
            LeaseState.Quarantined) => true,
        (LeaseState.Reserved or LeaseState.Provisioning or LeaseState.Quarantined or
            LeaseState.CleanupFailed or LeaseState.DropStarted, LeaseState.RecoveryAuthorized) => true,
        (LeaseState.DropStarted or LeaseState.RecoveryAuthorized, LeaseState.CleanupFailed) => true,
        (LeaseState.DropStarted or LeaseState.RecoveryAuthorized, LeaseState.Finalized) => true,
        _ => false
    };

    internal static bool IsIdempotentFinalization(
        FinalizationEvidence first, FinalizationEvidence replay) =>
        first == replay && first.TerminalState == LeaseState.Finalized;

    internal static bool Authorizes(RecoveryDecision decision, LeaseSnapshot lease,
        Guid attemptId, string requestedAction, DateTimeOffset now) =>
        decision.LeaseId == lease.LeaseId && decision.ExpectedPreState == lease.State &&
        decision.ConsumedAt is null && decision.ExpiresAt > now && attemptId != Guid.Empty &&
        string.Equals(decision.AuthorizedAction, requestedAction, StringComparison.Ordinal) &&
        requestedAction is "DropAndFinalize" or "FinalizeAbsent";

    internal static bool IsExactSetMatch<T>(IEnumerable<T> expected, IEnumerable<T> actual) where T : notnull
    {
        var left = expected.ToHashSet();
        var right = actual.ToHashSet();
        return left.SetEquals(right) && left.Count == expected.Count() && right.Count == actual.Count();
    }
}
