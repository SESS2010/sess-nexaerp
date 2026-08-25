using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Identity;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/identity").WithTags("Identity").RequireAuthorization();

        group.MapGet("/roles", async (NexaErpDbContext db, CancellationToken cancellationToken) =>
        {
            var roles = await db.Roles
                .AsNoTracking()
                .OrderBy(role => role.Code)
                .Select(role => new RoleSummary(role.Id, role.Code, role.Name, role.IsPrivileged, role.IsActive))
                .ToListAsync(cancellationToken);

            return Results.Ok(roles);
        }).RequirePagePermission("identity.roles", PagePermissionActions.View);

        group.MapPost("/roles", async (CreateRoleRequest request, NexaErpDbContext db, IAuditWriter audit, CancellationToken cancellationToken) =>
        {
            var code = NormalizeCode(request.Code);
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new { message = "Role code and name are required." });
            }

            if (await db.Roles.AnyAsync(role => role.Code == code, cancellationToken))
            {
                return Results.Conflict(new { message = $"Duplicate role code blocked: {code}" });
            }

            var role = new Role
            {
                Code = code,
                Name = request.Name.Trim(),
                IsPrivileged = request.IsPrivileged
            };

            db.Roles.Add(role);
            await db.SaveChangesAsync(cancellationToken);
            await audit.WriteAsync("Identity", "Create", nameof(Role), role.Id.ToString(), null, role, cancellationToken);

            return Results.Created($"/api/v1/identity/roles/{role.Id}", new RoleSummary(role.Id, role.Code, role.Name, role.IsPrivileged, role.IsActive));
        }).RequirePagePermission("identity.roles", PagePermissionActions.Create);

        group.MapGet("/users", async (NexaErpDbContext db, CancellationToken cancellationToken) =>
        {
            var users = await db.UserAccounts
                .AsNoTracking()
                .Include(user => user.Role)
                .OrderBy(user => user.LoginId)
                .Select(user => new UserAccountSummary(
                    user.Id,
                    user.LoginId,
                    user.DisplayName,
                    user.Email,
                    user.UserType,
                    user.Role == null ? string.Empty : user.Role.Code,
                    user.MfaRequired,
                    user.IsActive))
                .ToListAsync(cancellationToken);

            return Results.Ok(users);
        }).RequirePagePermission("identity.users", PagePermissionActions.View);

        group.MapPost("/users", async (CreateUserAccountRequest request, NexaErpDbContext db, IAuditWriter audit, CancellationToken cancellationToken) =>
        {
            var loginId = NormalizeLogin(request.LoginId);
            var roleCode = NormalizeCode(request.RoleCode);
            if (string.IsNullOrWhiteSpace(loginId) || string.IsNullOrWhiteSpace(request.DisplayName) || string.IsNullOrWhiteSpace(roleCode))
            {
                return Results.BadRequest(new { message = "Login ID, display name and role code are required." });
            }

            if (await db.UserAccounts.AnyAsync(user => user.LoginId == loginId, cancellationToken))
            {
                return Results.Conflict(new { message = $"Duplicate login ID blocked: {loginId}" });
            }

            var role = await db.Roles.SingleOrDefaultAsync(existing => existing.Code == roleCode && existing.IsActive, cancellationToken);
            if (role is null)
            {
                return Results.BadRequest(new { message = $"Active role not found: {roleCode}" });
            }

            var user = new UserAccount
            {
                LoginId = loginId,
                DisplayName = request.DisplayName.Trim(),
                Email = request.Email.Trim(),
                UserType = request.UserType.Trim(),
                RoleId = role.Id,
                MfaRequired = request.MfaRequired || role.IsPrivileged,
                PasswordHash = "PENDING_IDENTITY_PROVIDER"
            };

            db.UserAccounts.Add(user);
            await db.SaveChangesAsync(cancellationToken);
            await audit.WriteAsync("Identity", "Create", nameof(UserAccount), user.Id.ToString(), null, user, cancellationToken);

            return Results.Created($"/api/v1/identity/users/{user.Id}", new UserAccountSummary(user.Id, user.LoginId, user.DisplayName, user.Email, user.UserType, role.Code, user.MfaRequired, user.IsActive));
        }).RequirePagePermission("identity.users", PagePermissionActions.Create);

        return endpoints;
    }

    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();

    private static string NormalizeLogin(string value) => value.Trim().ToUpperInvariant();
}
