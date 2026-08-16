using SESS.NexaERP.ControlPlane.Contracts;

namespace SESS.NexaERP.ControlPlane.Domain;

public sealed class Rev869BControllerStateMachine
{
    private sealed record OperationRule(
        ControllerLifecycleState From,
        ControllerOperationV2 Operation,
        string Role,
        ControllerLifecycleState To,
        bool RequiresLease,
        bool RequiresEvidence = true);

    private static readonly OperationRule[] OperationRules =
    [
        new(ControllerLifecycleState.Registered, ControllerOperationV2.AUTHORIZE_PREPARE, "Operator", ControllerLifecycleState.Preflight, false),
        new(ControllerLifecycleState.Preflight, ControllerOperationV2.PREPARE, "ProvisioningExecutor", ControllerLifecycleState.Provisioning, true),
        new(ControllerLifecycleState.Provisioning, ControllerOperationV2.COMPLETE_PREPARE, "ProvisioningExecutor", ControllerLifecycleState.Ready, true),
        new(ControllerLifecycleState.Provisioning, ControllerOperationV2.FAIL, "ProvisioningExecutor", ControllerLifecycleState.Failed, true),
        new(ControllerLifecycleState.Ready, ControllerOperationV2.AUTHORIZE_EXECUTE, "Operator", ControllerLifecycleState.MigrationAuthorized, false),
        new(ControllerLifecycleState.MigrationAuthorized, ControllerOperationV2.EXECUTE, "MigrationExecutor", ControllerLifecycleState.Migrating, true),
        new(ControllerLifecycleState.Migrating, ControllerOperationV2.COMPLETE_EXECUTE, "MigrationExecutor", ControllerLifecycleState.VerificationPending, true),
        new(ControllerLifecycleState.Migrating, ControllerOperationV2.FAIL, "MigrationExecutor", ControllerLifecycleState.Failed, true),
        new(ControllerLifecycleState.VerificationPending, ControllerOperationV2.VERIFY_ACCEPT, "AcceptanceVerifier", ControllerLifecycleState.Accepted, true),
        new(ControllerLifecycleState.VerificationPending, ControllerOperationV2.VERIFY_REJECT, "AcceptanceVerifier", ControllerLifecycleState.Failed, true),
        new(ControllerLifecycleState.Quarantined, ControllerOperationV2.AUTHORIZE_RECOVER, "RecoveryApprover", ControllerLifecycleState.RecoveryAuthorized, false),
        new(ControllerLifecycleState.RecoveryAuthorized, ControllerOperationV2.RECOVER, "RecoveryExecutor", ControllerLifecycleState.Recovering, true),
        new(ControllerLifecycleState.Recovering, ControllerOperationV2.COMPLETE_RECOVER, "RecoveryExecutor", ControllerLifecycleState.Ready, true),
        new(ControllerLifecycleState.Recovering, ControllerOperationV2.FAIL, "RecoveryExecutor", ControllerLifecycleState.Failed, true),
        new(ControllerLifecycleState.Accepted, ControllerOperationV2.AUTHORIZE_DROP, "DropAuthorizer", ControllerLifecycleState.DropAuthorized, false),
        new(ControllerLifecycleState.Failed, ControllerOperationV2.AUTHORIZE_DROP, "DropAuthorizer", ControllerLifecycleState.DropAuthorized, false),
        new(ControllerLifecycleState.Quarantined, ControllerOperationV2.AUTHORIZE_DROP, "DropAuthorizer", ControllerLifecycleState.DropAuthorized, false),
        new(ControllerLifecycleState.DropAuthorized, ControllerOperationV2.DROP, "DropExecutor", ControllerLifecycleState.Dropped, true),
        new(ControllerLifecycleState.Dropped, ControllerOperationV2.AUTHORIZE_PURGE, "PurgeAuthorizer", ControllerLifecycleState.PurgeAuthorized, false),
        new(ControllerLifecycleState.PurgeAuthorized, ControllerOperationV2.PURGE, "PurgeExecutor", ControllerLifecycleState.Purging, true),
        new(ControllerLifecycleState.Purging, ControllerOperationV2.COMPLETE_PURGE, "PurgeExecutor", ControllerLifecycleState.Purged, true),
        new(ControllerLifecycleState.Purging, ControllerOperationV2.FAIL, "PurgeExecutor", ControllerLifecycleState.Dropped, true),
        new(ControllerLifecycleState.Accepted, ControllerOperationV2.AUTHORIZE_EXPORT, "ExportAuthorizer", ControllerLifecycleState.Accepted, false),
        new(ControllerLifecycleState.Accepted, ControllerOperationV2.EXPORT, "ExportExecutor", ControllerLifecycleState.Accepted, true),
        new(ControllerLifecycleState.Accepted, ControllerOperationV2.COMPLETE_EXPORT, "ExportExecutor", ControllerLifecycleState.Accepted, true)
    ];

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

    public ControllerLifecycleState RequireOperation(
        ControllerOperationV2 operation,
        ControllerLifecycleState current,
        ControllerLifecycleState requested,
        string trustedRole,
        bool hasEvidence,
        LeaseFenceV2? lease)
    {
        if (operation == ControllerOperationV2.QUARANTINE && current != ControllerLifecycleState.Purged)
        {
            Require(trustedRole == "ControlPlaneRuntime" && hasEvidence, TrustFailureCodeV2.SUBJECT_UNAUTHORIZED);
            return ControllerLifecycleState.Quarantined;
        }

        var rule = OperationRules.SingleOrDefault(item => item.From == current && item.Operation == operation);
        Require(rule is not null && rule.To == requested, TrustFailureCodeV2.STATE_TRANSITION_ILLEGAL);
        Require(string.Equals(rule!.Role, trustedRole, StringComparison.Ordinal), TrustFailureCodeV2.SUBJECT_UNAUTHORIZED);
        Require(!rule.RequiresEvidence || hasEvidence, TrustFailureCodeV2.READER_MISSING);
        Require(!rule.RequiresLease || lease is not null, TrustFailureCodeV2.LEASE_REQUIRED);
        return rule.To;
    }

    public IReadOnlyCollection<(ControllerLifecycleState State, ControllerOperationV2 Operation)> ListedOperations =>
        OperationRules.Select(static item => (item.From, item.Operation)).ToArray();

    public IReadOnlyCollection<(
        ControllerLifecycleState State,
        ControllerOperationV2 Operation,
        string Role,
        ControllerLifecycleState Next,
        bool RequiresLease)> ListedOperationRules =>
        OperationRules.Select(static item =>
            (item.From, item.Operation, item.Role, item.To, item.RequiresLease)).ToArray();

    public bool RequiresLease(ControllerLifecycleState state, ControllerOperationV2 operation) =>
        OperationRules.SingleOrDefault(item => item.From == state && item.Operation == operation)?.RequiresLease == true;

    public LifecycleResourceStateV2 CreateReplacement(
        LifecycleResourceStateV2 current,
        ControllerOperationV2 operation,
        ControllerLifecycleState requested,
        string trustedRole,
        bool hasEvidence,
        LeaseFenceV2? lease,
        DateTimeOffset authorizationExpiresAt,
        DateTimeOffset now,
        string auditReference)
    {
        Require(hasEvidence, TrustFailureCodeV2.READER_MISSING);
        if (operation == ControllerOperationV2.CANCEL)
        {
            Require(current.AuthorizationStatus == ControllerAuthorizationStatusV2.ACTIVE &&
                    current.ActiveAuthorizerRole == trustedRole &&
                    current.AuthorizationExpiresAt >= now,
                TrustFailureCodeV2.SUBJECT_UNAUTHORIZED);
            return current with
            {
                Version = current.Version + 1,
                LastAuditReference = auditReference,
                AuthorizationStatus = ControllerAuthorizationStatusV2.CANCELLED,
                ExportState = current.ExportState == ExportLifecycleStateV2.AUTHORIZED
                    ? ExportLifecycleStateV2.NONE
                    : current.ExportState
            };
        }

        if (operation == ControllerOperationV2.EXPIRE)
        {
            Require(trustedRole == "ControlPlaneRuntime" &&
                    current.AuthorizationStatus == ControllerAuthorizationStatusV2.ACTIVE &&
                    current.AuthorizationExpiresAt < now,
                TrustFailureCodeV2.SUBJECT_UNAUTHORIZED);
            return current with
            {
                Version = current.Version + 1,
                LastAuditReference = auditReference,
                AuthorizationStatus = ControllerAuthorizationStatusV2.EXPIRED,
                ExportState = current.ExportState == ExportLifecycleStateV2.AUTHORIZED
                    ? ExportLifecycleStateV2.EXPIRED
                    : current.ExportState
            };
        }

        ValidateExportSubstate(current, operation, now);
        var requiredAuthorization = RequiredAuthorizationFor(operation);
        if (requiredAuthorization is not null)
        {
            Require(current.AuthorizationStatus == ControllerAuthorizationStatusV2.ACTIVE &&
                    current.ActiveAuthorizationOperation == requiredAuthorization &&
                    current.AuthorizationExpiresAt >= now,
                TrustFailureCodeV2.SUBJECT_UNAUTHORIZED);
        }

        var nextState = RequireOperation(operation, current.State, requested, trustedRole, hasEvidence, lease);
        var isAuthorization = operation.ToString().StartsWith("AUTHORIZE_", StringComparison.Ordinal);
        return current with
        {
            Version = current.Version + 1,
            State = nextState,
            LastAuditReference = auditReference,
            AuthorizationStatus = isAuthorization
                ? ControllerAuthorizationStatusV2.ACTIVE
                : requiredAuthorization is null
                    ? current.AuthorizationStatus
                    : ControllerAuthorizationStatusV2.CONSUMED,
            ActiveAuthorizationOperation = isAuthorization ? operation.ToString() : current.ActiveAuthorizationOperation,
            ActiveAuthorizerRole = isAuthorization ? trustedRole : current.ActiveAuthorizerRole,
            AuthorizationExpiresAt = isAuthorization ? authorizationExpiresAt : current.AuthorizationExpiresAt,
            ExportState = operation switch
            {
                ControllerOperationV2.AUTHORIZE_EXPORT => ExportLifecycleStateV2.AUTHORIZED,
                ControllerOperationV2.EXPORT => ExportLifecycleStateV2.DELIVERING,
                ControllerOperationV2.COMPLETE_EXPORT => ExportLifecycleStateV2.DELIVERED,
                _ => current.ExportState
            }
        };
    }

    private static string? RequiredAuthorizationFor(ControllerOperationV2 operation) => operation switch
    {
        ControllerOperationV2.PREPARE => ControllerOperationV2.AUTHORIZE_PREPARE.ToString(),
        ControllerOperationV2.EXECUTE => ControllerOperationV2.AUTHORIZE_EXECUTE.ToString(),
        ControllerOperationV2.RECOVER => ControllerOperationV2.AUTHORIZE_RECOVER.ToString(),
        ControllerOperationV2.DROP => ControllerOperationV2.AUTHORIZE_DROP.ToString(),
        ControllerOperationV2.PURGE => ControllerOperationV2.AUTHORIZE_PURGE.ToString(),
        ControllerOperationV2.EXPORT => ControllerOperationV2.AUTHORIZE_EXPORT.ToString(),
        _ => null
    };

    private static void ValidateExportSubstate(
        LifecycleResourceStateV2 current,
        ControllerOperationV2 operation,
        DateTimeOffset now)
    {
        if (operation == ControllerOperationV2.AUTHORIZE_EXPORT)
        {
            Require(current.ExportState is ExportLifecycleStateV2.NONE or ExportLifecycleStateV2.EXPIRED,
                TrustFailureCodeV2.STATE_TRANSITION_ILLEGAL);
        }
        else if (operation == ControllerOperationV2.EXPORT)
        {
            Require(current.ExportState == ExportLifecycleStateV2.AUTHORIZED &&
                    current.AuthorizationExpiresAt >= now,
                TrustFailureCodeV2.STATE_TRANSITION_ILLEGAL);
        }
        else if (operation == ControllerOperationV2.COMPLETE_EXPORT)
        {
            Require(current.ExportState == ExportLifecycleStateV2.DELIVERING,
                TrustFailureCodeV2.STATE_TRANSITION_ILLEGAL);
        }
    }

    private static void Require(bool condition, TrustFailureCodeV2 code)
    {
        if (!condition)
        {
            throw new TrustFailureExceptionV2(code, $"Lifecycle operation rejected: {code}.");
        }
    }
}
