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

    private static readonly LifecycleRuleV3[] PhaseARules =
    [
        Rule("registered-authorize-prepare", ControllerLifecycleState.Registered, ControllerOperationV2.AUTHORIZE_PREPARE,
            "Operator", ControllerLifecycleState.Preflight, ControllerLifecycleState.Registered, false, ["target-registration"],
            audit: LifecycleAuditEventV3.PREPARE_AUTHORIZED),
        Rule("preflight-prepare", ControllerLifecycleState.Preflight, ControllerOperationV2.PREPARE,
            "ProvisioningExecutor", ControllerLifecycleState.Provisioning, ControllerLifecycleState.Failed, true, ["preflight"],
            audit: LifecycleAuditEventV3.PREPARE_STARTED, nextAuthorization: AuthorizationGrantStateV3.CONSUMED),
        Rule("provisioning-complete", ControllerLifecycleState.Provisioning, ControllerOperationV2.COMPLETE_PREPARE,
            "ProvisioningExecutor", ControllerLifecycleState.Ready, ControllerLifecycleState.Failed, true, ["action-receipt", "ready-facts"],
            audit: LifecycleAuditEventV3.PREPARE_COMPLETED, requiredAuthorization: AuthorizationGrantStateV3.CONSUMED,
            nextAuthorization: AuthorizationGrantStateV3.CONSUMED),
        Rule("provisioning-fail", ControllerLifecycleState.Provisioning, ControllerOperationV2.FAIL,
            "ProvisioningExecutor", ControllerLifecycleState.Failed, ControllerLifecycleState.Quarantined, true, ["failure-facts"], 0,
            LifecycleAuditEventV3.PREPARE_FAILED, AuthorizationGrantStateV3.CONSUMED, AuthorizationGrantStateV3.CONSUMED),
        Rule("ready-authorize-execute", ControllerLifecycleState.Ready, ControllerOperationV2.AUTHORIZE_EXECUTE,
            "Operator", ControllerLifecycleState.MigrationAuthorized, ControllerLifecycleState.Ready, false, ["migration-plan"],
            audit: LifecycleAuditEventV3.EXECUTE_AUTHORIZED),
        Rule("authorized-execute", ControllerLifecycleState.MigrationAuthorized, ControllerOperationV2.EXECUTE,
            "MigrationExecutor", ControllerLifecycleState.Migrating, ControllerLifecycleState.Quarantined, true, ["preflight"],
            audit: LifecycleAuditEventV3.EXECUTE_STARTED, nextAuthorization: AuthorizationGrantStateV3.CONSUMED),
        Rule("migrating-complete", ControllerLifecycleState.Migrating, ControllerOperationV2.COMPLETE_EXECUTE,
            "MigrationExecutor", ControllerLifecycleState.VerificationPending, ControllerLifecycleState.Failed, true, ["action-receipt", "migration-ledger"],
            audit: LifecycleAuditEventV3.EXECUTE_COMPLETED, requiredAuthorization: AuthorizationGrantStateV3.CONSUMED,
            nextAuthorization: AuthorizationGrantStateV3.CONSUMED),
        Rule("migrating-fail", ControllerLifecycleState.Migrating, ControllerOperationV2.FAIL,
            "MigrationExecutor", ControllerLifecycleState.Failed, ControllerLifecycleState.Quarantined, true, ["failure-facts"], 0,
            LifecycleAuditEventV3.EXECUTE_FAILED, AuthorizationGrantStateV3.CONSUMED, AuthorizationGrantStateV3.CONSUMED),
        Rule("verification-accept", ControllerLifecycleState.VerificationPending, ControllerOperationV2.VERIFY_ACCEPT,
            "AcceptanceVerifier", ControllerLifecycleState.Accepted, ControllerLifecycleState.Failed, true, ["signed-verdict", "evidence-archive"], 0,
            LifecycleAuditEventV3.VERIFICATION_ACCEPTED, AuthorizationGrantStateV3.CONSUMED,
            AuthorizationGrantStateV3.CONSUMED),
        Rule("verification-reject", ControllerLifecycleState.VerificationPending, ControllerOperationV2.VERIFY_REJECT,
            "AcceptanceVerifier", ControllerLifecycleState.Failed, ControllerLifecycleState.Quarantined, true, ["signed-verdict", "evidence-archive"], 0,
            LifecycleAuditEventV3.VERIFICATION_REJECTED, AuthorizationGrantStateV3.CONSUMED,
            AuthorizationGrantStateV3.CONSUMED),
        Rule("any-quarantine", ControllerLifecycleState.Registered, ControllerOperationV2.QUARANTINE,
            "ControlPlaneRuntime", ControllerLifecycleState.Quarantined, ControllerLifecycleState.Quarantined, false,
            ["inconsistency-facts"], 0, LifecycleAuditEventV3.RESOURCE_QUARANTINED,
            anyNonterminal: true),
        Rule("quarantined-authorize-recover", ControllerLifecycleState.Quarantined, ControllerOperationV2.AUTHORIZE_RECOVER,
            "RecoveryApprover", ControllerLifecycleState.RecoveryAuthorized, ControllerLifecycleState.Quarantined, false, ["recovery-plan"],
            audit: LifecycleAuditEventV3.RECOVERY_AUTHORIZED),
        Rule("authorized-recover", ControllerLifecycleState.RecoveryAuthorized, ControllerOperationV2.RECOVER,
            "RecoveryExecutor", ControllerLifecycleState.Recovering, ControllerLifecycleState.Quarantined, true, ["before-facts"],
            audit: LifecycleAuditEventV3.RECOVERY_STARTED, nextAuthorization: AuthorizationGrantStateV3.CONSUMED),
        Rule("recovering-complete", ControllerLifecycleState.Recovering, ControllerOperationV2.COMPLETE_RECOVER,
            "RecoveryExecutor", ControllerLifecycleState.Ready, ControllerLifecycleState.Failed, true, ["action-receipt", "ready-facts"],
            audit: LifecycleAuditEventV3.RECOVERY_COMPLETED, requiredAuthorization: AuthorizationGrantStateV3.CONSUMED,
            nextAuthorization: AuthorizationGrantStateV3.CONSUMED),
        Rule("recovering-fail", ControllerLifecycleState.Recovering, ControllerOperationV2.FAIL,
            "RecoveryExecutor", ControllerLifecycleState.Failed, ControllerLifecycleState.Quarantined, true, ["failure-facts"], 0,
            LifecycleAuditEventV3.RECOVERY_FAILED, AuthorizationGrantStateV3.CONSUMED, AuthorizationGrantStateV3.CONSUMED),
        Rule("accepted-authorize-drop", ControllerLifecycleState.Accepted, ControllerOperationV2.AUTHORIZE_DROP,
            "DropAuthorizer", ControllerLifecycleState.DropAuthorized, ControllerLifecycleState.Accepted, false, ["backup-attestation", "retention-approval"], 0,
            LifecycleAuditEventV3.DROP_AUTHORIZED),
        Rule("failed-authorize-drop", ControllerLifecycleState.Failed, ControllerOperationV2.AUTHORIZE_DROP,
            "DropAuthorizer", ControllerLifecycleState.DropAuthorized, ControllerLifecycleState.Failed, false, ["backup-attestation", "retention-approval"], 0,
            LifecycleAuditEventV3.DROP_AUTHORIZED),
        Rule("quarantined-authorize-drop", ControllerLifecycleState.Quarantined, ControllerOperationV2.AUTHORIZE_DROP,
            "DropAuthorizer", ControllerLifecycleState.DropAuthorized, ControllerLifecycleState.Quarantined, false, ["backup-attestation", "retention-approval"], 0,
            LifecycleAuditEventV3.DROP_AUTHORIZED),
        Rule("authorized-drop", ControllerLifecycleState.DropAuthorized, ControllerOperationV2.DROP,
            "DropExecutor", ControllerLifecycleState.Dropped, ControllerLifecycleState.Quarantined, true, ["drop-authorization", "target-facts"], 0,
            LifecycleAuditEventV3.DROP_COMPLETED, nextAuthorization: AuthorizationGrantStateV3.CONSUMED),
        Rule("dropped-authorize-purge", ControllerLifecycleState.Dropped, ControllerOperationV2.AUTHORIZE_PURGE,
            "PurgeAuthorizer", ControllerLifecycleState.PurgeAuthorized, ControllerLifecycleState.Dropped, false, ["candidate-root", "legal-hold-decision"], 0,
            LifecycleAuditEventV3.PURGE_AUTHORIZED),
        Rule("authorized-purge", ControllerLifecycleState.PurgeAuthorized, ControllerOperationV2.PURGE,
            "PurgeExecutor", ControllerLifecycleState.Purging, ControllerLifecycleState.Dropped, true, ["candidate-root", "purge-authorization"], 0,
            LifecycleAuditEventV3.PURGE_STARTED, nextAuthorization: AuthorizationGrantStateV3.CONSUMED),
        Rule("purging-complete", ControllerLifecycleState.Purging, ControllerOperationV2.COMPLETE_PURGE,
            "PurgeExecutor", ControllerLifecycleState.Purged, ControllerLifecycleState.Dropped, true, ["empty-candidate-proof", "batch-audit"], 0,
            LifecycleAuditEventV3.PURGE_COMPLETED, AuthorizationGrantStateV3.CONSUMED, AuthorizationGrantStateV3.CONSUMED),
        Rule("purging-fail", ControllerLifecycleState.Purging, ControllerOperationV2.FAIL,
            "PurgeExecutor", ControllerLifecycleState.Dropped, ControllerLifecycleState.Quarantined, true, ["failure-facts"], 0,
            LifecycleAuditEventV3.PURGE_FAILED, AuthorizationGrantStateV3.CONSUMED, AuthorizationGrantStateV3.CONSUMED),
        Rule("accepted-authorize-export", ControllerLifecycleState.Accepted, ControllerOperationV2.AUTHORIZE_EXPORT,
            "ExportAuthorizer", ControllerLifecycleState.Accepted, ControllerLifecycleState.Accepted, false, ["minimized-batch-root", "privacy-approval"], 0,
            LifecycleAuditEventV3.EXPORT_AUTHORIZED, requiredExport: ExportAuthorizationSubstateV3.NONE,
            nextExport: ExportAuthorizationSubstateV3.AUTHORIZED),
        Rule("accepted-reauthorize-expired-export", ControllerLifecycleState.Accepted, ControllerOperationV2.AUTHORIZE_EXPORT,
            "ExportAuthorizer", ControllerLifecycleState.Accepted, ControllerLifecycleState.Accepted, false, ["minimized-batch-root", "privacy-approval"], 0,
            LifecycleAuditEventV3.EXPORT_AUTHORIZED, requiredExport: ExportAuthorizationSubstateV3.EXPIRED,
            nextExport: ExportAuthorizationSubstateV3.AUTHORIZED),
        Rule("accepted-reauthorize-failed-export", ControllerLifecycleState.Accepted, ControllerOperationV2.AUTHORIZE_EXPORT,
            "ExportAuthorizer", ControllerLifecycleState.Accepted, ControllerLifecycleState.Accepted, false, ["minimized-batch-root", "privacy-approval"], 0,
            LifecycleAuditEventV3.EXPORT_AUTHORIZED, requiredExport: ExportAuthorizationSubstateV3.FAILED,
            nextExport: ExportAuthorizationSubstateV3.AUTHORIZED),
        Rule("accepted-export", ControllerLifecycleState.Accepted, ControllerOperationV2.EXPORT,
            "ExportExecutor", ControllerLifecycleState.Accepted, ControllerLifecycleState.Accepted, true, ["export-authorization"], 3,
            LifecycleAuditEventV3.EXPORT_STARTED, nextAuthorization: AuthorizationGrantStateV3.CONSUMED,
            requiredExport: ExportAuthorizationSubstateV3.AUTHORIZED,
            nextExport: ExportAuthorizationSubstateV3.DELIVERING),
        Rule("accepted-complete-export", ControllerLifecycleState.Accepted, ControllerOperationV2.COMPLETE_EXPORT,
            "ExportExecutor", ControllerLifecycleState.Accepted, ControllerLifecycleState.Accepted, true, ["delivery-receipt"], 0,
            LifecycleAuditEventV3.EXPORT_DELIVERED, AuthorizationGrantStateV3.CONSUMED, AuthorizationGrantStateV3.CONSUMED,
            ExportAuthorizationSubstateV3.DELIVERING, ExportAuthorizationSubstateV3.DELIVERED),
        Rule("any-cancel-active-authorization", ControllerLifecycleState.Registered, ControllerOperationV2.CANCEL,
            "OriginalAuthorizer", ControllerLifecycleState.Registered, ControllerLifecycleState.Registered, false,
            ["cancellation-reason"], 0, LifecycleAuditEventV3.AUTHORIZATION_CANCELLED,
            AuthorizationGrantStateV3.ACTIVE, AuthorizationGrantStateV3.CANCELLED,
            anyActiveAuthorization: true),
        Rule("any-expire-active-authorization", ControllerLifecycleState.Registered, ControllerOperationV2.EXPIRE,
            "ControlPlaneRuntime", ControllerLifecycleState.Registered, ControllerLifecycleState.Registered, false,
            ["server-time"], 0, LifecycleAuditEventV3.AUTHORIZATION_EXPIRED,
            AuthorizationGrantStateV3.ACTIVE, AuthorizationGrantStateV3.EXPIRED,
            anyActiveAuthorization: true)
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

    public LifecycleRuleV3 RequirePhaseACommand(VerifiedLifecycleCommandV3 command, DateTimeOffset serverNow)
    {
        var snapshot = command.AuthoritativeSnapshot;
        Require(snapshot is not null &&
                !string.IsNullOrWhiteSpace(snapshot.ProviderIdentity) &&
                !string.IsNullOrWhiteSpace(snapshot.ProviderVersion) &&
                snapshot.Scope == command.Authorization.Scope &&
                snapshot.ResourceType == command.Authorization.ResourceType &&
                snapshot.ResourceId == command.Authorization.ResourceId &&
                snapshot.AttemptId == command.AttemptId &&
                snapshot.AttemptNumber >= 0,
            TrustFailureCodeV2.AUTHORIZATION_BINDING_MISMATCH);
        var authoritativeSnapshot = snapshot!;
        Require(authoritativeSnapshot.ResourceVersion == command.CurrentVersion,
            TrustFailureCodeV2.RESOURCE_VERSION_STALE);
        Require(authoritativeSnapshot.LifecycleState == command.CurrentState,
            TrustFailureCodeV2.STATE_TRANSITION_ILLEGAL);
        Require(authoritativeSnapshot.AuthorizationState == command.CurrentAuthorizationState,
            TrustFailureCodeV2.AUTHORIZATION_BINDING_MISMATCH);
        Require(authoritativeSnapshot.ExportState == command.CurrentExportState,
            TrustFailureCodeV2.STATE_TRANSITION_ILLEGAL);
        Require(Equals(authoritativeSnapshot.Lease, command.Lease),
            TrustFailureCodeV2.LEASE_FENCE_STALE);
        PhaseAContractValidator.RequireValid(command.Authorization);
        Require(command.CurrentVersion == command.Authorization.ResourceVersion,
            TrustFailureCodeV2.RESOURCE_VERSION_STALE);
        Require(command.Operation.ToString() == command.Authorization.Operation,
            TrustFailureCodeV2.AUTHORIZATION_BINDING_MISMATCH);
        Require(command.Idempotency.Operation == command.Authorization.Operation &&
                command.Idempotency.OrganizationId == command.Authorization.Scope.OrganizationId &&
                command.Idempotency.DatabaseInstanceId == command.Authorization.Scope.DatabaseInstanceId &&
                command.Idempotency.CanonicalRequestSha256 == command.Authorization.CanonicalRequestSha256,
            TrustFailureCodeV2.AUTHORIZATION_BINDING_MISMATCH);
        Require(command.Authorization.Authorization.NotBefore <= serverNow &&
                (command.Operation == ControllerOperationV2.EXPIRE ||
                 command.Authorization.Authorization.ExpiresAt >= serverNow),
            TrustFailureCodeV2.ENVELOPE_EXPIRED);

        var rule = PhaseARules.SingleOrDefault(item =>
            item.Operation == command.Operation &&
            (item.CurrentState == command.CurrentState ||
             item.AppliesToAnyNonterminalState && command.CurrentState != ControllerLifecycleState.Purged ||
             item.AppliesToAnyActiveAuthorization) &&
            (command.Operation != ControllerOperationV2.AUTHORIZE_EXPORT ||
             item.RequiredExportState == command.CurrentExportState));
        Require(rule is not null, TrustFailureCodeV2.STATE_TRANSITION_ILLEGAL);
        var matchedRule = rule!;
        if (command.Operation == ControllerOperationV2.CANCEL)
        {
            Require(matchedRule.TrustedRole == command.Authorization.Authorization.TrustedRole &&
                    snapshot!.CurrentAuthorization is not null &&
                    snapshot.CurrentAuthorization.State == AuthorizationGrantStateV3.ACTIVE &&
                    snapshot.CurrentAuthorization.GrantAuthorization.AuthenticatedSubject ==
                        command.Authorization.Authorization.AuthenticatedSubject,
                TrustFailureCodeV2.SUBJECT_UNAUTHORIZED);
        }
        else
        {
            Require(matchedRule.TrustedRole == command.Authorization.Authorization.TrustedRole,
                TrustFailureCodeV2.SUBJECT_UNAUTHORIZED);
        }
        if (IsAuthorizationCreation(command.Operation))
        {
            Require(command.CurrentAuthorizationState != AuthorizationGrantStateV3.ACTIVE,
                TrustFailureCodeV2.AUTHORIZATION_BINDING_MISMATCH);
        }
        else if (command.Operation != ControllerOperationV2.QUARANTINE)
        {
            Require(command.CurrentAuthorizationState == matchedRule.RequiredAuthorizationState,
                TrustFailureCodeV2.AUTHORIZATION_BINDING_MISMATCH);
        }
        if (command.Operation is ControllerOperationV2.AUTHORIZE_EXPORT or
            ControllerOperationV2.EXPORT or
            ControllerOperationV2.COMPLETE_EXPORT)
        {
            Require(command.CurrentExportState == matchedRule.RequiredExportState,
                TrustFailureCodeV2.STATE_TRANSITION_ILLEGAL);
        }
        if (matchedRule.AppliesToAnyActiveAuthorization)
        {
            Require(command.CurrentAuthorizationState == AuthorizationGrantStateV3.ACTIVE,
                TrustFailureCodeV2.AUTHORIZATION_BINDING_MISMATCH);
        }
        var expectedGrantOperation = ExpectedGrantOperation(command);
        if (expectedGrantOperation is not null)
        {
            RequireExactStoredGrant(command, expectedGrantOperation, serverNow);
        }
        if (command.Operation == ControllerOperationV2.EXPIRE)
        {
            Require(snapshot!.CurrentAuthorization is not null &&
                    snapshot.CurrentAuthorization.GrantAuthorization.ExpiresAt < serverNow,
                TrustFailureCodeV2.NOT_YET_VALID);
        }

        var evidenceIds = command.RequiredEvidence
            .Select(static requirement => requirement.RequirementId)
            .ToHashSet(StringComparer.Ordinal);
        Require(evidenceIds.SetEquals(matchedRule.RequiredEvidenceIds),
            TrustFailureCodeV2.READER_MISSING);
        Require(command.RequiredEvidence.All(static requirement =>
                !string.IsNullOrWhiteSpace(requirement.ReaderId) &&
                !string.IsNullOrWhiteSpace(requirement.ReaderVersion) &&
                requirement.SchemaVersion == Rev869BPhaseACompatibilityManifest.EvidenceSchemaVersion &&
                requirement.MaximumFacts is > 0 and <= PhaseAContractLimits.MaximumFactsPerObservation &&
                requirement.MaximumBytes is > 0 and <= PhaseAContractLimits.MaximumEvidenceEnvelopeBytes),
            TrustFailureCodeV2.READER_UNAUTHORIZED);
        Require(!matchedRule.RequiresLease || command.Lease is not null,
            TrustFailureCodeV2.LEASE_REQUIRED);
        if (command.Lease is not null)
        {
            Require(command.Lease.ExpiresAt >= serverNow, TrustFailureCodeV2.LEASE_EXPIRED);
            Require(command.Lease.ControllerEpoch > 0 &&
                    command.Lease.ResourceId == command.Authorization.ResourceId &&
                    command.Lease.LeaseId == command.Authorization.LeaseId &&
                    command.Lease.FencingToken == command.Authorization.FencingToken &&
                    command.Lease.HolderSubject == command.Authorization.Authorization.AuthenticatedSubject,
                TrustFailureCodeV2.LEASE_FENCE_STALE);
        }

        return matchedRule.AppliesToAnyActiveAuthorization
            ? matchedRule with
            {
                CurrentState = command.CurrentState,
                NextState = command.CurrentState,
                FailureState = command.CurrentState
            }
            : matchedRule;
    }

    private static bool IsAuthorizationCreation(ControllerOperationV2 operation) =>
        operation.ToString().StartsWith("AUTHORIZE_", StringComparison.Ordinal);

    private static string? ExpectedGrantOperation(VerifiedLifecycleCommandV3 command) =>
        command.Operation switch
        {
            ControllerOperationV2.PREPARE or ControllerOperationV2.COMPLETE_PREPARE =>
                ControllerOperationV2.AUTHORIZE_PREPARE.ToString(),
            ControllerOperationV2.EXECUTE or ControllerOperationV2.COMPLETE_EXECUTE or
                ControllerOperationV2.VERIFY_ACCEPT or ControllerOperationV2.VERIFY_REJECT =>
                ControllerOperationV2.AUTHORIZE_EXECUTE.ToString(),
            ControllerOperationV2.RECOVER or ControllerOperationV2.COMPLETE_RECOVER =>
                ControllerOperationV2.AUTHORIZE_RECOVER.ToString(),
            ControllerOperationV2.DROP => ControllerOperationV2.AUTHORIZE_DROP.ToString(),
            ControllerOperationV2.PURGE or ControllerOperationV2.COMPLETE_PURGE =>
                ControllerOperationV2.AUTHORIZE_PURGE.ToString(),
            ControllerOperationV2.EXPORT or ControllerOperationV2.COMPLETE_EXPORT =>
                ControllerOperationV2.AUTHORIZE_EXPORT.ToString(),
            ControllerOperationV2.FAIL when command.CurrentState == ControllerLifecycleState.Provisioning =>
                ControllerOperationV2.AUTHORIZE_PREPARE.ToString(),
            ControllerOperationV2.FAIL when command.CurrentState == ControllerLifecycleState.Migrating =>
                ControllerOperationV2.AUTHORIZE_EXECUTE.ToString(),
            ControllerOperationV2.FAIL when command.CurrentState == ControllerLifecycleState.Recovering =>
                ControllerOperationV2.AUTHORIZE_RECOVER.ToString(),
            ControllerOperationV2.FAIL when command.CurrentState == ControllerLifecycleState.Purging =>
                ControllerOperationV2.AUTHORIZE_PURGE.ToString(),
            ControllerOperationV2.CANCEL or ControllerOperationV2.EXPIRE =>
                command.AuthoritativeSnapshot?.CurrentAuthorization?.AuthorizedOperation,
            _ => null
        };

    private static void RequireExactStoredGrant(
        VerifiedLifecycleCommandV3 command,
        string expectedGrantOperation,
        DateTimeOffset serverNow)
    {
        var snapshot = command.AuthoritativeSnapshot!;
        var grant = snapshot.CurrentAuthorization;
        var claim = command.StoredGrantClaim;
        Require(snapshot.CurrentAuthorizationMatchCount == 1 &&
                grant is not null &&
                claim is not null &&
                grant == claim &&
                grant.State == command.CurrentAuthorizationState &&
                grant.Scope == snapshot.Scope &&
                grant.ResourceType == snapshot.ResourceType &&
                grant.ResourceId == snapshot.ResourceId &&
                grant.ResourceVersion == snapshot.ResourceVersion &&
                grant.AuthorizedOperation == expectedGrantOperation &&
                grant.GrantAuthorization.Operation == expectedGrantOperation &&
                grant.EvidenceManifestSha256 == command.Authorization.EvidenceManifestSha256 &&
                grant.LeaseId == command.Authorization.LeaseId &&
                grant.FencingToken == command.Authorization.FencingToken &&
                grant.ControllerEpoch == (command.Lease?.ControllerEpoch ?? 0) &&
                grant.GrantAuthorization.NotBefore <= serverNow &&
                (command.Operation == ControllerOperationV2.EXPIRE ||
                 grant.GrantAuthorization.ExpiresAt >= serverNow) &&
                (grant.State == AuthorizationGrantStateV3.CONSUMED) == (grant.ConsumedAt is not null),
            TrustFailureCodeV2.AUTHORIZATION_BINDING_MISMATCH);
    }

    public static IReadOnlyList<LifecycleRuleV3> PhaseARuleSnapshot => PhaseARules;

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

    private static LifecycleRuleV3 Rule(
        string id,
        ControllerLifecycleState current,
        ControllerOperationV2 operation,
        string role,
        ControllerLifecycleState next,
        ControllerLifecycleState failure,
        bool lease,
        IReadOnlyList<string> evidence,
        int retries = 3,
        LifecycleAuditEventV3 audit = LifecycleAuditEventV3.PREPARE_AUTHORIZED,
        AuthorizationGrantStateV3 requiredAuthorization = AuthorizationGrantStateV3.ACTIVE,
        AuthorizationGrantStateV3 nextAuthorization = AuthorizationGrantStateV3.ACTIVE,
        ExportAuthorizationSubstateV3 requiredExport = ExportAuthorizationSubstateV3.NONE,
        ExportAuthorizationSubstateV3 nextExport = ExportAuthorizationSubstateV3.NONE,
        bool anyNonterminal = false,
        bool anyActiveAuthorization = false) =>
        new(
            id,
            current,
            operation,
            role,
            next,
            failure,
            lease,
            evidence,
            retries,
            operation.ToString().StartsWith("AUTHORIZE_", StringComparison.Ordinal)
                ? AuditEventKindV3.AUTHORIZATION_DECIDED
                : AuditEventKindV3.LIFECYCLE_ATTEMPTED,
            audit,
            requiredAuthorization,
            nextAuthorization,
            requiredExport,
            nextExport,
            anyNonterminal,
            anyActiveAuthorization);

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
