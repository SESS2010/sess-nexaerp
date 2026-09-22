using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Rev869A;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public sealed record RevokeEmployeeIdentityRequest(uint Version, string Remarks);

public static partial class Rev869AConfigurationEndpoints
{
    private static async Task<IResult> CreateIdentity(CreateEmployeeIdentityMappingRequest request,
        NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.EmployeeCode) ||
            string.IsNullOrWhiteSpace(request.OrganizationId) ||
            string.IsNullOrWhiteSpace(request.Remarks) ||
            string.IsNullOrWhiteSpace(request.Subject) || request.Subject.Length > 500 ||
            string.IsNullOrWhiteSpace(request.Issuer) || request.Issuer.Length > 500 ||
            !Uri.TryCreate(request.Issuer, UriKind.Absolute, out var issuer) ||
            issuer.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(issuer.UserInfo) ||
            !string.IsNullOrEmpty(issuer.Query) || !string.IsNullOrEmpty(issuer.Fragment))
            return Results.BadRequest(new { message = "Employee, company, remarks, an HTTPS OIDC issuer and a stable subject of at most 500 characters are required." });

        return await IdentityTransactionAsync(db,
            () => CreateIdentityCore(request, db, user, audit, ct), ct);
    }

    private static async Task<IResult> RevokeIdentity(Guid identityId, RevokeEmployeeIdentityRequest request,
        NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Remarks))
            return Results.BadRequest(new { message = "Identity revocation remarks are required." });
        return await IdentityTransactionAsync(db, async () =>
        {
            var mapping = await db.EmployeeIdentityMappings.SingleOrDefaultAsync(
                row => row.Id == identityId && row.OrganizationId == user.OrganizationId, ct);
            if (mapping is null) return Results.NotFound();
            if (mapping.Version != request.Version)
                return Results.Conflict(new { message = "Identity mapping Version is stale." });
            if (!mapping.IsActive)
                return Results.Conflict(new { message = "Identity mapping has already been revoked." });
            // Prevent the current administrator accidentally removing their own access.
            if (mapping.EmployeeId == user.EmployeeId)
                return Results.Conflict(new { message = "Another authorized administrator must revoke your identity mapping." });

            var before = new { mapping.IsActive, mapping.EffectiveTo, mapping.Version };
            mapping.IsActive = false;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            mapping.EffectiveTo = mapping.EffectiveFrom > today ? mapping.EffectiveFrom : today;
            mapping.UpdatedAt = DateTimeOffset.UtcNow;
            mapping.UpdatedBy = user.LoginId;
            mapping.Version = checked(mapping.Version + 1);
            AddHistory(db, mapping.OrganizationId, nameof(EmployeeIdentityMapping), mapping.Id,
                "Revoke", before, new { mapping.IsActive, mapping.EffectiveTo, mapping.Version },
                request.Remarks, user, mapping.CompanyId);
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("Security", "RevokeIdentityMapping", nameof(EmployeeIdentityMapping),
                mapping.Id.ToString(), before, new { mapping.IsActive, mapping.EffectiveTo, mapping.Version }, ct);
            return Results.Ok(new { mapping.Id, mapping.IsActive, mapping.EffectiveTo, mapping.Version });
        }, ct);
    }

    private static async Task<IResult> IdentityTransactionAsync(NexaErpDbContext db,
        Func<Task<IResult>> action, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var result = await action();
            await transaction.CommitAsync(ct);
            return result;
        }
        catch (Exception ex) when (
            ex is PostgresException { SqlState: "23505" or "40001" or "40P01" } ||
            ex is DbUpdateException { InnerException: PostgresException { SqlState: "23505" or "40001" or "40P01" } })
        {
            await transaction.RollbackAsync(ct);
            return Results.Conflict(new { message = "The identity mapping changed concurrently or already exists. Reload its current state before retrying." });
        }
    }
}
