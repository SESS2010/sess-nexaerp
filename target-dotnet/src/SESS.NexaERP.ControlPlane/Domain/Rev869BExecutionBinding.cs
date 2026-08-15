using SESS.NexaERP.ControlPlane.Contracts;

namespace SESS.NexaERP.ControlPlane.Domain;

public static class Rev869BExecutionBindingValidator
{
    public static void RequireExact(Rev869BExecutionBinding actual, Rev869BExecutionBinding expected)
    {
        Require(actual.Company == expected.Company, TrustRejectionCode.WrongCompany, "Company scope does not match.");
        Require(actual.ControlPlane == expected.ControlPlane && actual.Target == expected.Target,
            TrustRejectionCode.WrongTargetInstance, "Control-plane or target identity does not match.");
        Require(actual.LeaseId == expected.LeaseId && actual.LeaseVersion == expected.LeaseVersion,
            TrustRejectionCode.WrongLease, "Lease binding does not match.");
        Require(actual.ExecutionId == expected.ExecutionId &&
                actual.OperationId == expected.OperationId &&
                actual.PreparationId == expected.PreparationId &&
                actual.AttemptId == expected.AttemptId &&
                actual.ActionId == expected.ActionId,
            TrustRejectionCode.WrongExecution, "Execution binding does not match.");
        Require(actual.ScenarioId == expected.ScenarioId, TrustRejectionCode.WrongScenario, "Scenario does not match.");
        Require(actual.SubcaseId == expected.SubcaseId, TrustRejectionCode.WrongSubcase, "Subcase does not match.");
        Require(actual.OracleId == expected.OracleId, TrustRejectionCode.WrongOracle, "Oracle does not match.");
    }

    public static void RequireValid(Rev869BExecutionBinding value)
    {
        Require(value.Company.IsNotApplicable
                ? value.Company == CompanyScope.ControlPlaneNotApplicable
                : !string.IsNullOrWhiteSpace(value.Company.CompanyId),
            TrustRejectionCode.WrongCompany, "Company scope is invalid.");
        Require(!string.IsNullOrWhiteSpace(value.ControlPlane.Value) &&
                !string.IsNullOrWhiteSpace(value.Target.Value) &&
                !string.IsNullOrWhiteSpace(value.Target.EnvironmentName) &&
                !string.IsNullOrWhiteSpace(value.Target.DatabaseIdentity),
            TrustRejectionCode.WrongTargetInstance, "Instance identity is incomplete.");
        Require(value.LeaseVersion > 0 && NonEmpty(value.LeaseId), TrustRejectionCode.WrongLease, "Lease is invalid.");
        Require(NonEmpty(value.OperationId, value.PreparationId, value.AttemptId, value.ExecutionId, value.ActionId),
            TrustRejectionCode.WrongExecution, "Execution identifiers are incomplete.");
        Require(NonEmpty(value.ScenarioId), TrustRejectionCode.WrongScenario, "Scenario is missing.");
        Require(NonEmpty(value.SubcaseId), TrustRejectionCode.WrongSubcase, "Subcase is missing.");
        Require(NonEmpty(value.OracleId), TrustRejectionCode.WrongOracle, "Oracle is missing.");
    }

    private static bool NonEmpty(params string[] values) => values.All(static value => !string.IsNullOrWhiteSpace(value));

    private static void Require(bool condition, TrustRejectionCode code, string message)
    {
        if (!condition)
        {
            throw new TrustRejectionException(code, message);
        }
    }
}

public static class ControllerAuthorizationPolicyV1
{
    private static readonly IReadOnlyDictionary<ControllerCommandKind, ControllerRole[]> RequiredRoles =
        new Dictionary<ControllerCommandKind, ControllerRole[]>
        {
            [ControllerCommandKind.Register] = [ControllerRole.RegistryWriter],
            [ControllerCommandKind.BeginPreflight] = [ControllerRole.Operator],
            [ControllerCommandKind.BeginProvisioning] = [ControllerRole.ProvisioningExecutor],
            [ControllerCommandKind.MarkReady] = [ControllerRole.ProvisioningExecutor],
            [ControllerCommandKind.AuthorizeMigration] = [ControllerRole.Operator],
            [ControllerCommandKind.BeginMigration] = [ControllerRole.MigrationExecutor],
            [ControllerCommandKind.RequestVerification] = [ControllerRole.ControlPlaneRuntime],
            [ControllerCommandKind.RecordAcceptance] = [ControllerRole.AcceptanceVerifier],
            [ControllerCommandKind.RecordFailure] = [ControllerRole.AcceptanceVerifier],
            [ControllerCommandKind.Quarantine] = [ControllerRole.ControlPlaneRuntime],
            [ControllerCommandKind.AuthorizeRecovery] = [ControllerRole.RecoveryApprover],
            [ControllerCommandKind.BeginRecovery] = [ControllerRole.RecoveryExecutor],
            [ControllerCommandKind.AuthorizeDrop] = [ControllerRole.Operator],
            [ControllerCommandKind.RecordDropped] = [ControllerRole.ProvisioningExecutor],
            [ControllerCommandKind.AuthorizePurge] = [ControllerRole.PurgeAuthorizer],
            [ControllerCommandKind.BeginPurge] = [ControllerRole.PurgeExecutor],
            [ControllerCommandKind.RecordPurged] = [ControllerRole.PurgeExecutor],
            [ControllerCommandKind.ExportEvidence] = [ControllerRole.ExportReader]
        };

    public static void RequireAuthorized(LifecycleCommandV1 command)
    {
        Rev869BExecutionBindingValidator.RequireValid(command.Binding);
        if (command.Lease.LeaseId != command.Binding.LeaseId ||
            command.Lease.LeaseVersion != command.Binding.LeaseVersion)
        {
            throw new TrustRejectionException(TrustRejectionCode.WrongLease, "Command lease is stale or mismatched.");
        }

        if (!RequiredRoles[command.Kind].Any(command.Authorization.Roles.Contains))
        {
            throw new TrustRejectionException(TrustRejectionCode.UnauthorizedRole, "Subject does not hold the exact operation role.");
        }
    }
}
