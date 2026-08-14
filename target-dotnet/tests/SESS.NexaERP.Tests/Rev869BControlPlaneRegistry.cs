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
}
