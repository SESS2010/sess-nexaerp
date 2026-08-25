using SESS.NexaERP.Domain.Identity;

namespace SESS.NexaERP.Infrastructure.Persistence;

public static class AuthenticationBootstrapSeed
{
    public static readonly Guid Id = Guid.Parse("81000000-0000-0000-0000-000000000001");

    public static readonly AuthenticationBootstrapState Pending = new()
    {
        Id = Id,
        Status = AuthenticationBootstrapStatuses.Pending,
        CreatedAt = DateTimeOffset.UnixEpoch,
        CreatedBy = "migration-authentication-bootstrap"
    };
}
