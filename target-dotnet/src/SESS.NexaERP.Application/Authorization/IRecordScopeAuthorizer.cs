namespace SESS.NexaERP.Application.Authorization;

public sealed record RecordScopeTarget(string OrganizationId, Guid? DepartmentId, Guid? WarehouseId, Guid? RackBinId);

public sealed record RecordScopeDecision(bool Allowed, string Reason);

public interface IRecordScopeAuthorizer
{
    Task<RecordScopeDecision> AuthorizeAsync(Guid employeeId, string roleCode, RecordScopeTarget target, DateOnly onDate, CancellationToken cancellationToken);
}
