using SESS.NexaERP.ControlPlane.Contracts;

namespace SESS.NexaERP.ControlPlane.Domain;

public sealed class Rev869BControllerStateMachine
{
    private static readonly IReadOnlyDictionary<ControllerLifecycleState, ControllerLifecycleState[]> LegalTransitions =
        new Dictionary<ControllerLifecycleState, ControllerLifecycleState[]>
        {
            [ControllerLifecycleState.Registered] = [ControllerLifecycleState.Preflight, ControllerLifecycleState.Quarantined],
            [ControllerLifecycleState.Preflight] = [ControllerLifecycleState.Provisioning, ControllerLifecycleState.Failed, ControllerLifecycleState.Quarantined],
            [ControllerLifecycleState.Provisioning] = [ControllerLifecycleState.Ready, ControllerLifecycleState.Failed, ControllerLifecycleState.Quarantined],
            [ControllerLifecycleState.Ready] = [ControllerLifecycleState.MigrationAuthorized, ControllerLifecycleState.Quarantined],
            [ControllerLifecycleState.MigrationAuthorized] = [ControllerLifecycleState.Migrating, ControllerLifecycleState.Quarantined],
            [ControllerLifecycleState.Migrating] = [ControllerLifecycleState.VerificationPending, ControllerLifecycleState.Failed, ControllerLifecycleState.Quarantined],
            [ControllerLifecycleState.VerificationPending] = [ControllerLifecycleState.Accepted, ControllerLifecycleState.Failed, ControllerLifecycleState.Quarantined],
            [ControllerLifecycleState.Accepted] = [ControllerLifecycleState.DropAuthorized, ControllerLifecycleState.Quarantined],
            [ControllerLifecycleState.Failed] = [ControllerLifecycleState.Quarantined],
            [ControllerLifecycleState.Quarantined] = [ControllerLifecycleState.RecoveryAuthorized, ControllerLifecycleState.DropAuthorized],
            [ControllerLifecycleState.RecoveryAuthorized] = [ControllerLifecycleState.Recovering],
            [ControllerLifecycleState.Recovering] = [ControllerLifecycleState.Ready, ControllerLifecycleState.Failed, ControllerLifecycleState.Quarantined],
            [ControllerLifecycleState.DropAuthorized] = [ControllerLifecycleState.Dropped],
            [ControllerLifecycleState.Dropped] = [ControllerLifecycleState.PurgeAuthorized],
            [ControllerLifecycleState.PurgeAuthorized] = [ControllerLifecycleState.Purging],
            [ControllerLifecycleState.Purging] = [ControllerLifecycleState.Purged],
            [ControllerLifecycleState.Purged] = []
        };

    public bool CanTransition(ControllerLifecycleState from, ControllerLifecycleState to) =>
        LegalTransitions.TryGetValue(from, out var targets) && targets.Contains(to);

    public void RequireTransition(ControllerLifecycleState from, ControllerLifecycleState to)
    {
        if (!CanTransition(from, to))
        {
            throw new TrustRejectionException(TrustRejectionCode.IllegalTransition, $"Transition {from} -> {to} is not legal.");
        }
    }
}
