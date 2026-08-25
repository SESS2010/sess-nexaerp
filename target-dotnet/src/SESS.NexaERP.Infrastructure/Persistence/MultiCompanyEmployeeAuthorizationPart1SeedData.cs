using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Identity;

namespace SESS.NexaERP.Infrastructure.Persistence;

internal static class MultiCompanyEmployeeAuthorizationPart1SeedData
{
    internal static readonly Role[] Roles =
    [
        NewRole("83000000-0000-0000-0000-000000000001", Rev869ARoleCodes.ProductionManager, "Production Manager"),
        NewRole("83000000-0000-0000-0000-000000000002", Rev869ARoleCodes.AccountsManager, "Accounts Manager")
    ];

    private static Role NewRole(string id, string code, string name) => new()
    {
        Id = Guid.Parse(id),
        Code = code,
        Name = name,
        IsPrivileged = true,
        IsActive = true,
        CreatedAt = DateTimeOffset.UnixEpoch,
        CreatedBy = "migration-multi-company-employee-authorization-part1"
    };
}
