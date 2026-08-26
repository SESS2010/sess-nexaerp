namespace SESS.NexaERP.Application.Purchase;

public sealed record PurchaseApprovalWorkflowStepSnapshot(
    int StepNumber,
    string ResolutionType,
    Guid EmployeeId,
    string EmployeeCode,
    string RoleCode);

public sealed record PurchaseApprovalWorkflowSnapshot(
    string Identity,
    string OrganizationId,
    string RouteCode,
    decimal ApprovalValue,
    DateOnly EffectiveOn,
    IReadOnlyList<PurchaseApprovalWorkflowStepSnapshot> Steps);

public sealed record PurchaseApprovalDecision(
    int ApprovalCycle,
    int StepNumber,
    int RequiredStepCount,
    int CompletedStepCount,
    bool CompletesDocument,
    Guid ResolvedEmployeeId,
    string ResolvedRoleCode,
    string RouteCode,
    string SnapshotIdentity);

public interface IPurchaseApprovalWorkflowService
{
    Task<PurchaseApprovalWorkflowSnapshot> SelectAndSnapshotAsync(
        string organizationId, Guid requestingDepartmentId, decimal amount, CancellationToken ct);

    PurchaseApprovalWorkflowSnapshot ReadSnapshot(string snapshotJson);

    PurchaseApprovalDecision AuthorizeNextStep(
        string snapshotJson, int approvalCycle, int completedStepCount,
        Guid creatorEmployeeId, Guid actorEmployeeId, IReadOnlyList<string> actorRoleCodes,
        Guid? priorStepEmployeeId = null);

    string Serialize(PurchaseApprovalWorkflowSnapshot snapshot);
}

public interface IPurchaseOperationalRoleResolver
{
    string Resolve(string operation, IReadOnlyList<string> effectiveRoleCodes);
}
