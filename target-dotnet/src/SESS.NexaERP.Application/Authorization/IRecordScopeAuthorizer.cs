namespace SESS.NexaERP.Application.Authorization;

public sealed record RecordScopeTarget(string OrganizationId, Guid? DepartmentId, Guid? WarehouseId, Guid? RackBinId, Guid? OwnerEmployeeId = null);

public sealed record RecordScopeDecision(bool Allowed, string Reason);

public interface IRecordScopeAuthorizer
{
    Task<RecordScopeDecision> AuthorizeAnyAsync(Guid employeeId, string roleCode, string organizationId, DateOnly onDate, CancellationToken cancellationToken);
    Task<RecordScopeDecision> AuthorizeAsync(Guid employeeId, string roleCode, RecordScopeTarget target, DateOnly onDate, CancellationToken cancellationToken);
}
