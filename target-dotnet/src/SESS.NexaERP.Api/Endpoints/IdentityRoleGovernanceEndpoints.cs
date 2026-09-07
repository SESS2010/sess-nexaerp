using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Identity;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public static partial class IdentityEndpoints
{
    private static async Task<IResult> UpdateRoleGovernanceAsync(
        string roleCode,
        UpdateRoleGovernanceRequest request,
        NexaErpDbContext db,
        ICurrentUser user,
        IAuditWriter audit,
        CancellationToken ct)
    {
        var audience = NormalizeCode(request.Audience);
        var businessArea = NormalizeCode(request.BusinessArea);
        var replacementCode = string.IsNullOrWhiteSpace(request.ReplacementRoleCode) ? null : NormalizeCode(request.ReplacementRoleCode);
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { message = "Role name is required." });
        }
        if (!RoleAudienceOptions.Contains(audience) || !RoleBusinessAreaOptions.Contains(businessArea))
        {
            return Results.BadRequest(new { message = "Audience and business area must come from role-governance options." });
        }
        if (request.IsEmployeeAssignable && audience != RoleAudiences.InternalEmployee)
        {
            return Results.BadRequest(new { message = "Only INTERNAL_EMPLOYEE roles may be employee-assignable." });
        }
        if ((audience == RoleAudiences.LegacyAlias) != (replacementCode is not null))
        {
            return Results.BadRequest(new { message = "A legacy alias requires one replacement; other audiences cannot have one." });
        }

        var role = await db.Roles.Include(row => row.ReplacementRole)
            .SingleOrDefaultAsync(row => row.Code == NormalizeCode(roleCode), ct);
        if (role is null) return Results.NotFound(new { message = "Role not found." });
        if (role.Version != request.Version) return Results.Conflict(new { message = "Role Version is stale." });
        Role? replacement = null;
        if (replacementCode is not null)
        {
            replacement = await db.Roles.SingleOrDefaultAsync(row => row.Code == replacementCode, ct);
            if (replacement is null || replacement.Id == role.Id || replacement.Audience == RoleAudiences.LegacyAlias)
            {
                return Results.BadRequest(new { message = "Replacement must be a different, canonical role." });
            }
        }

        var before = new { role.Name, role.IsPrivileged, role.IsActive, role.Audience, role.BusinessArea, role.IsEmployeeAssignable, role.ReplacementRoleId, role.Version };
        role.Name = request.Name.Trim();
        role.IsPrivileged = request.IsPrivileged;
        role.IsActive = request.IsActive;
        role.Audience = audience;
        role.BusinessArea = businessArea;
        role.IsEmployeeAssignable = request.IsEmployeeAssignable;
        role.ReplacementRoleId = replacement?.Id;
        role.Version = checked(role.Version + 1);
        role.UpdatedAt = DateTimeOffset.UtcNow;
        role.UpdatedBy = user.LoginId;
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync("Identity", "UpdateGovernance", nameof(Role), role.Id.ToString(), before, role, ct);
        return Results.Ok(new RoleSummary(role.Id, role.Code, role.Name, role.IsPrivileged, role.IsActive,
            role.Audience, role.BusinessArea, role.IsEmployeeAssignable, replacement?.Code, role.Version));
    }

    private static async Task<IResult> UpdateCompanyRoleActivationAsync(
        string roleCode,
        UpdateCompanyRoleActivationRequest request,
        NexaErpDbContext db,
        ICurrentUser user,
        IAuditWriter audit,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(user.OrganizationId)) return Results.Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Remarks) || request.EffectiveTo < request.EffectiveFrom)
        {
            return Results.BadRequest(new { message = "Remarks and a valid effective date range are required." });
        }
        var company = await db.Companies.SingleOrDefaultAsync(
            row => row.Code == user.OrganizationId && row.IsActive, ct);
        var role = await db.Roles.SingleOrDefaultAsync(
            row => row.Code == NormalizeCode(roleCode) && row.IsActive, ct);
        if (company is null || role is null)
        {
            return Results.BadRequest(new { message = "Active company and role are required." });
        }
        if (request.IsEnabled && role.Audience is RoleAudiences.LegacyAlias or RoleAudiences.SystemSecurity)
        {
            return Results.BadRequest(new { message = "Legacy aliases and system-security roles cannot be enabled for company assignment." });
        }

        var activation = await db.CompanyRoleActivations
            .Where(row => row.CompanyId == company.Id && row.RoleId == role.Id)
            .OrderByDescending(row => row.EffectiveFrom).FirstOrDefaultAsync(ct);
        object? before = null;
        if (activation is null)
        {
            if (request.Version is not null) return Results.Conflict(new { message = "Activation does not yet exist; Version must be null." });
            activation = new CompanyRoleActivation { CompanyId = company.Id, RoleId = role.Id, CreatedBy = user.LoginId };
            db.CompanyRoleActivations.Add(activation);
        }
        else if (activation.Version != request.Version)
        {
            return Results.Conflict(new { message = "Company role activation Version is stale." });
        }
        else before = new { activation.IsEnabled, activation.EffectiveFrom, activation.EffectiveTo, activation.Remarks, activation.Version };
        activation.IsEnabled = request.IsEnabled;
        activation.EffectiveFrom = request.EffectiveFrom;
        activation.EffectiveTo = request.EffectiveTo;
        activation.Remarks = request.Remarks.Trim();
        activation.Version = checked(activation.Version + 1);
        activation.UpdatedAt = DateTimeOffset.UtcNow;
        activation.UpdatedBy = user.LoginId;
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync("Identity", "UpdateCompanyRoleActivation", nameof(CompanyRoleActivation),
            activation.Id.ToString(), before, activation, ct);
        return Results.Ok(new CompanyRoleActivationSummary(role.Id, role.Code, role.Name, role.Audience,
            role.BusinessArea, role.IsEmployeeAssignable, activation.Id, activation.IsEnabled,
            activation.EffectiveFrom, activation.EffectiveTo, activation.Remarks, activation.Version));
    }
}
