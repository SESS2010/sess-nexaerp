namespace SESS.NexaERP.Application.Common;

public interface ICurrentUser
{
    string LoginId { get; }
    string RoleCode { get; }
    string? OrganizationId { get; }
    bool IsAuthenticated { get; }
}
