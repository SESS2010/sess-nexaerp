using System.Data;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Employees;
using SESS.NexaERP.Domain.Employees;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public static partial class EmployeeEndpoints
{
    private static readonly string[] AssignmentTypes =
    [
        EmployeeRoleAssignmentTypes.Permanent,
        EmployeeRoleAssignmentTypes.Temporary,
        EmployeeRoleAssignmentTypes.Cover
    ];

    private static async Task<IResult> AssignRoleAsync(
        string employeeCode,
        AssignEmployeeRoleRequest request,
        NexaErpDbContext db,
        ICurrentUser user,
        IAuditWriter audit,
        CancellationToken ct)
    {
        var assignmentType = NormalizeRoleCode(request.AssignmentType);
        if (!AssignmentTypes.Contains(assignmentType, StringComparer.Ordinal) ||
            string.IsNullOrWhiteSpace(request.Remarks) ||
            request.EffectiveTo < request.EffectiveFrom)
            return Results.BadRequest(new { message = "Assignment type, valid dates and remarks are required." });
        if ((assignmentType == EmployeeRoleAssignmentTypes.Permanent) == request.EffectiveTo.HasValue)
            return Results.BadRequest(new { message = "Permanent assignments must be open-ended; temporary and cover assignments require EffectiveTo." });
        if (request.IsPrimary && (assignmentType != EmployeeRoleAssignmentTypes.Permanent || request.EffectiveTo.HasValue))
            return Results.BadRequest(new { message = "A primary assignment must be permanent and open-ended." });

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var scope = await ResolveRoleScopeAsync(employeeCode, request.EffectiveFrom, db, user, ct);
        if (scope is null) return Results.BadRequest(new { message = "Employee is not active in the selected company on the effective date." });
        var role = await ResolveAssignableRoleAsync(request.RoleCode, scope.CompanyId, request.EffectiveFrom, db, ct);
        if (role is null) return Results.BadRequest(new { message = "The role is not employee-assignable and enabled in this company on the effective date." });
        if (await HasOverlapAsync(scope, role.Id, request.EffectiveFrom, request.EffectiveTo, null, db, ct))
            return Results.Conflict(new { message = "This employee already has an overlapping assignment for that role." });

        var profile = await GetOrCreateProfileAsync(scope, db, user);
        if (request.IsPrimary)
        {
            if (profile.ConfigurationStatus == EmployeeRoleProfileStatuses.Configured)
                return Results.Conflict(new { message = "Use promotion, transfer or change-primary for a configured employee." });
            if (request.ProfileVersion.HasValue && request.ProfileVersion.Value != profile.Version)
                return Results.Conflict(new { message = "Role profile Version is stale." });
        }

        var assignment = NewAssignment(scope, role.Id, request.EffectiveFrom, request.EffectiveTo,
            assignmentType, request.IsPrimary, request.Remarks, user);
        db.EmployeeRoleAssignments.Add(assignment);
        if (request.IsPrimary)
        {
            profile.ConfigurationStatus = EmployeeRoleProfileStatuses.Configured;
            profile.PrimaryRoleAssignmentId = assignment.Id;
            Touch(profile, user);
        }
        AddEvent(db, scope, assignment.Id, request.IsPrimary ? "SET_INITIAL_PRIMARY" : "ASSIGN",
            null, role.Code, null, request.EffectiveFrom, request.Remarks, user);
        await SaveRoleOperationAsync(db, ct);
        await audit.WriteAsync("Employees", request.IsPrimary ? "SetInitialPrimaryRole" : "AssignRole",
            nameof(EmployeeRoleAssignment), assignment.Id.ToString(), null,
            RoleSummary(assignment, role), ct);
        await transaction.CommitAsync(ct);
        return Results.Created($"/api/v1/employees/{scope.EmployeeCode}/roles/{assignment.Id}", RoleSummary(assignment, role));
    }

    private static Task<IResult> AssignTemporaryCoverAsync(
        string employeeCode,
        TemporaryRoleCoverRequest request,
        NexaErpDbContext db,
        ICurrentUser user,
        IAuditWriter audit,
        CancellationToken ct) =>
        AssignRoleAsync(employeeCode,
            new AssignEmployeeRoleRequest(request.RoleCode, request.EffectiveFrom, request.EffectiveTo,
                request.Remarks, EmployeeRoleAssignmentTypes.Cover), db, user, audit, ct);

    private static async Task<IResult> ChangePrimaryRoleAsync(
        string employeeCode,
        string? newRoleCode,
        DateOnly effectiveOn,
        bool keepPrevious,
        string remarks,
        uint profileVersion,
        string operation,
        Guid? existingAssignmentId,
        NexaErpDbContext db,
        ICurrentUser user,
        IAuditWriter audit,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(remarks) || effectiveOn != DateOnly.FromDateTime(DateTime.UtcNow))
            return Results.BadRequest(new { message = "Remarks are required and primary-role changes must take effect today." });

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var scope = await ResolveRoleScopeAsync(employeeCode, effectiveOn, db, user, ct);
        if (scope is null) return Results.BadRequest(new { message = "Employee is not active in the selected company." });
        var profile = await db.EmployeeCompanyRoleProfiles
            .SingleOrDefaultAsync(x => x.CompanyId == scope.CompanyId && x.EmployeeId == scope.EmployeeId, ct);
        if (profile is null || profile.ConfigurationStatus != EmployeeRoleProfileStatuses.Configured ||
            profile.PrimaryRoleAssignmentId is null)
            return Results.Conflict(new { message = "The employee does not yet have a configured primary role." });
        if (profile.Version != profileVersion)
            return Results.Conflict(new { message = "Role profile Version is stale." });

        var previous = await db.EmployeeRoleAssignments.Include(x => x.Role)
            .SingleOrDefaultAsync(x => x.Id == profile.PrimaryRoleAssignmentId &&
                x.CompanyId == scope.CompanyId && x.EmployeeId == scope.EmployeeId, ct);
        if (previous?.Role is null || !previous.IsPrimary)
            return Results.Conflict(new { message = "The configured primary assignment is invalid. Refresh and retry." });

        Role role;
        EmployeeRoleAssignment? target = null;
        if (existingAssignmentId.HasValue)
        {
            target = await db.EmployeeRoleAssignments.Include(x => x.Role)
                .SingleOrDefaultAsync(x => x.Id == existingAssignmentId && x.CompanyId == scope.CompanyId &&
                    x.EmployeeId == scope.EmployeeId && x.EffectiveFrom <= effectiveOn &&
                    (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= effectiveOn) &&
                    (x.ApprovalStatus == "Approved" || x.ApprovalStatus == "SeedApproved"), ct);
            role = target?.Role ?? new Role();
        }
        else
        {
            role = await ResolveAssignableRoleAsync(newRoleCode ?? string.Empty, scope.CompanyId, effectiveOn, db, ct)
                ?? new Role();
        }
        if (role.Id == Guid.Empty)
            return Results.BadRequest(new { message = "The new primary role is not an effective, assignable role in this company." });
        if (role.Id == previous.RoleId)
            return Results.Conflict(new { message = "The selected role is already primary." });

        previous.IsPrimary = false;
        if (!keepPrevious)
            EndAssignment(previous, effectiveOn, remarks, user);

        // Flush the old primary change inside this serializable transaction before activating its replacement.
        // Other transactions cannot observe this state; the deferred constraint validates the committed state.
        await SaveRoleOperationAsync(db, ct);

        EmployeeRoleAssignment replacement;
        if (target is not null)
        {
            if (target.AssignmentType != EmployeeRoleAssignmentTypes.Permanent || target.EffectiveTo.HasValue)
                return Results.Conflict(new { message = "Only an open-ended permanent assignment can become primary." });
            target.IsPrimary = true;
            target.Version = checked(target.Version + 1);
            target.UpdatedAt = DateTimeOffset.UtcNow;
            target.UpdatedBy = user.LoginId;
            replacement = target;
        }
        else
        {
            if (await HasOverlapAsync(scope, role.Id, effectiveOn, null, null, db, ct))
                return Results.Conflict(new { message = "The new role is already held; use change-primary with its AssignmentId." });
            replacement = NewAssignment(scope, role.Id, effectiveOn, null,
                EmployeeRoleAssignmentTypes.Permanent, true, remarks, user);
            db.EmployeeRoleAssignments.Add(replacement);
        }
        profile.PrimaryRoleAssignmentId = replacement.Id;
        Touch(profile, user);
        AddEvent(db, scope, replacement.Id, operation, previous.Role.Code, role.Code,
            keepPrevious, effectiveOn, remarks, user);

        await SaveRoleOperationAsync(db, ct);
        await audit.WriteAsync("Employees", operation, nameof(EmployeeRoleAssignment), replacement.Id.ToString(),
            RoleSummary(previous, previous.Role), RoleSummary(replacement, role), ct);
        await transaction.CommitAsync(ct);
        return Results.Ok(await BuildProfileAsync(scope, db, ct));
    }

    private static async Task<IResult> EndRoleAssignmentAsync(
        string employeeCode,
        Guid assignmentId,
        EndEmployeeRoleAssignmentRequest request,
        NexaErpDbContext db,
        ICurrentUser user,
        IAuditWriter audit,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return Results.BadRequest(new { message = "An end reason is required." });

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var scope = await ResolveRoleScopeAsync(employeeCode, DateOnly.FromDateTime(DateTime.UtcNow), db, user, ct);
        if (scope is null) return Results.NotFound(new { message = "Employee is not active in the selected company." });
        var assignment = await db.EmployeeRoleAssignments.Include(x => x.Role)
            .SingleOrDefaultAsync(x => x.Id == assignmentId && x.CompanyId == scope.CompanyId &&
                x.EmployeeId == scope.EmployeeId, ct);
        if (assignment?.Role is null) return Results.NotFound(new { message = "Role assignment not found." });
        if (assignment.Version != request.Version) return Results.Conflict(new { message = "Role assignment Version is stale." });
        if (assignment.IsPrimary) return Results.Conflict(new { message = "A primary assignment can only end through an atomic promotion, transfer or change-primary operation." });
        if (assignment.EndedAt.HasValue || assignment.EffectiveTo.HasValue)
            return Results.Conflict(new { message = "A dated or ended assignment is immutable." });
        if (request.EffectiveTo < assignment.EffectiveFrom)
            return Results.BadRequest(new { message = "EffectiveTo cannot precede EffectiveFrom." });

        var before = RoleSummary(assignment, assignment.Role);
        EndAssignment(assignment, request.EffectiveTo, request.Reason, user);
        AddEvent(db, scope, assignment.Id, "END_ASSIGNMENT", assignment.Role.Code, null,
            null, request.EffectiveTo, request.Reason, user);
        await SaveRoleOperationAsync(db, ct);
        await audit.WriteAsync("Employees", "EndRoleAssignment", nameof(EmployeeRoleAssignment),
            assignment.Id.ToString(), before, RoleSummary(assignment, assignment.Role), ct);
        await transaction.CommitAsync(ct);
        return Results.Ok(RoleSummary(assignment, assignment.Role));
    }

    private static async Task<IResult> GetRoleProfileAsync(
        string employeeCode, NexaErpDbContext db, ICurrentUser user, CancellationToken ct)
    {
        var scope = await ResolveRoleScopeAsync(employeeCode, DateOnly.FromDateTime(DateTime.UtcNow), db, user, ct);
        if (scope is null) return Results.NotFound(new { message = "Employee is not active in the selected company." });
        return Results.Ok(await BuildProfileAsync(scope, db, ct));
    }

    private static async Task<IResult> GetRoleEventsAsync(
        string employeeCode, NexaErpDbContext db, ICurrentUser user, CancellationToken ct)
    {
        var scope = await ResolveRoleScopeAsync(employeeCode, DateOnly.FromDateTime(DateTime.UtcNow), db, user, ct);
        if (scope is null) return Results.NotFound(new { message = "Employee is not active in the selected company." });
        var events = await db.EmployeeRoleAssignmentEvents.AsNoTracking()
            .Where(x => x.CompanyId == scope.CompanyId && x.EmployeeId == scope.EmployeeId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new EmployeeRoleAssignmentEventSummary(x.Id, x.Operation, x.FromRoleCode, x.ToRoleCode,
                x.PreviousRoleRetained, x.EffectiveOn, x.Reason, x.ActorLoginId, x.ActorRoleCode, x.CreatedAt))
            .ToListAsync(ct);
        return Results.Ok(events);
    }

    private static async Task<EmployeeRoleProfileSummary> BuildProfileAsync(
        EmployeeRoleScope scope, NexaErpDbContext db, CancellationToken ct)
    {
        var profile = await db.EmployeeCompanyRoleProfiles.AsNoTracking()
            .Include(x => x.PrimaryRoleAssignment!).ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.CompanyId == scope.CompanyId && x.EmployeeId == scope.EmployeeId, ct);
        var assignments = await db.EmployeeRoleAssignments.AsNoTracking().Include(x => x.Role)
            .Where(x => x.CompanyId == scope.CompanyId && x.EmployeeId == scope.EmployeeId)
            .OrderByDescending(x => x.EffectiveFrom).ThenBy(x => x.Role!.Code)
            .Select(x => new EmployeeRoleSummary(x.Id, x.Role == null ? string.Empty : x.Role.Code,
                x.Role == null ? string.Empty : x.Role.Name, x.EffectiveFrom, x.EffectiveTo,
                x.ApprovalStatus, x.Remarks, x.AssignmentType, x.IsPrimary, x.EndReason,
                x.EndedAt, x.EndedBy, x.Version))
            .ToListAsync(ct);
        return new(scope.EmployeeCode, scope.CompanyCode,
            profile?.ConfigurationStatus ?? EmployeeRoleProfileStatuses.Pending,
            profile?.PrimaryRoleAssignmentId, profile?.PrimaryRoleAssignment?.Role?.Code,
            profile?.Version ?? 0, assignments);
    }

    private static async Task<EmployeeRoleScope?> ResolveRoleScopeAsync(
        string employeeCode, DateOnly effectiveOn, NexaErpDbContext db, ICurrentUser user, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(user.OrganizationId)) return null;
        var organization = user.OrganizationId.Trim().ToUpperInvariant();
        var employee = await db.Employees.AsNoTracking()
            .SingleOrDefaultAsync(x => x.EmployeeCode == NormalizeEmployeeCode(employeeCode) && x.Status == "Active", ct);
        if (employee is null) return null;
        var company = await db.Companies.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Code == organization && x.IsActive && x.Status == "ACTIVE", ct);
        if (company is null) return null;
        var assigned = await db.EmployeeCompanyAssignments.AsNoTracking().AnyAsync(x =>
            x.CompanyId == company.Id && x.EmployeeId == employee.Id && x.IsActive && x.Status == "ACTIVE" &&
            x.EffectiveFrom <= effectiveOn && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= effectiveOn), ct);
        return assigned ? new(company.Id, company.Code, employee.Id, employee.EmployeeCode) : null;
    }

    private static async Task<Role?> ResolveAssignableRoleAsync(
        string roleCode, Guid companyId, DateOnly effectiveOn, NexaErpDbContext db, CancellationToken ct)
    {
        var code = NormalizeRoleCode(roleCode);
        return await db.Roles.SingleOrDefaultAsync(role => role.Code == code && role.IsActive &&
            role.IsEmployeeAssignable && role.Audience == RoleAudiences.InternalEmployee &&
            db.CompanyRoleActivations.Any(activation => activation.CompanyId == companyId &&
                activation.RoleId == role.Id && activation.IsEnabled &&
                activation.EffectiveFrom <= effectiveOn &&
                (!activation.EffectiveTo.HasValue || activation.EffectiveTo.Value >= effectiveOn)), ct);
    }

    private static async Task<bool> HasOverlapAsync(
        EmployeeRoleScope scope, Guid roleId, DateOnly from, DateOnly? to, Guid? excludeId,
        NexaErpDbContext db, CancellationToken ct)
    {
        var end = to ?? DateOnly.MaxValue;
        return await db.EmployeeRoleAssignments.AsNoTracking().AnyAsync(x =>
            x.CompanyId == scope.CompanyId && x.EmployeeId == scope.EmployeeId && x.RoleId == roleId &&
            (!excludeId.HasValue || x.Id != excludeId.Value) &&
            x.ApprovalStatus != "Rejected" &&
            x.EffectiveFrom <= end && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= from), ct);
    }

    private static async Task<EmployeeCompanyRoleProfile> GetOrCreateProfileAsync(
        EmployeeRoleScope scope, NexaErpDbContext db, ICurrentUser user)
    {
        var profile = await db.EmployeeCompanyRoleProfiles.SingleOrDefaultAsync(x =>
            x.CompanyId == scope.CompanyId && x.EmployeeId == scope.EmployeeId);
        if (profile is not null) return profile;
        profile = new EmployeeCompanyRoleProfile
        {
            CompanyId = scope.CompanyId,
            EmployeeId = scope.EmployeeId,
            ConfigurationStatus = EmployeeRoleProfileStatuses.Pending,
            CreatedBy = user.LoginId
        };
        db.EmployeeCompanyRoleProfiles.Add(profile);
        return profile;
    }

    private static EmployeeRoleAssignment NewAssignment(
        EmployeeRoleScope scope, Guid roleId, DateOnly from, DateOnly? to, string type,
        bool primary, string remarks, ICurrentUser user) => new()
    {
        CompanyId = scope.CompanyId,
        EmployeeId = scope.EmployeeId,
        RoleId = roleId,
        EffectiveFrom = from,
        EffectiveTo = to,
        AssignmentType = type,
        IsPrimary = primary,
        ApprovalStatus = "Approved",
        Remarks = remarks.Trim(),
        CreatedBy = user.LoginId
    };

    private static void EndAssignment(
        EmployeeRoleAssignment assignment, DateOnly effectiveTo, string reason, ICurrentUser user)
    {
        assignment.EffectiveTo = effectiveTo;
        if (effectiveTo <= DateOnly.FromDateTime(DateTime.UtcNow))
            assignment.ApprovalStatus = "Ended";
        assignment.EndReason = reason.Trim();
        assignment.EndedAt = DateTimeOffset.UtcNow;
        assignment.EndedBy = user.LoginId;
        assignment.Version = checked(assignment.Version + 1);
        assignment.UpdatedAt = DateTimeOffset.UtcNow;
        assignment.UpdatedBy = user.LoginId;
    }

    private static void Touch(EmployeeCompanyRoleProfile profile, ICurrentUser user)
    {
        profile.Version = checked(profile.Version + 1);
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        profile.UpdatedBy = user.LoginId;
    }

    private static void AddEvent(
        NexaErpDbContext db, EmployeeRoleScope scope, Guid? assignmentId, string operation,
        string? fromRole, string? toRole, bool? retained, DateOnly effectiveOn, string reason, ICurrentUser user) =>
        db.EmployeeRoleAssignmentEvents.Add(new EmployeeRoleAssignmentEvent
        {
            CompanyId = scope.CompanyId,
            EmployeeId = scope.EmployeeId,
            AssignmentId = assignmentId,
            Operation = operation,
            FromRoleCode = fromRole,
            ToRoleCode = toRole,
            PreviousRoleRetained = retained,
            EffectiveOn = effectiveOn,
            Reason = reason.Trim(),
            ActorLoginId = user.LoginId,
            ActorRoleCode = user.RoleCode,
            CreatedBy = user.LoginId
        });

    private static async Task SaveRoleOperationAsync(NexaErpDbContext db, CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new EmployeeRoleOperationConflictException("Role assignments changed concurrently. Refresh and retry.", ex);
        }
        catch (DbUpdateException ex)
        {
            throw new EmployeeRoleOperationConflictException("The requested role dates overlap or violate the primary-role invariant.", ex);
        }
    }

    private static EmployeeRoleSummary RoleSummary(EmployeeRoleAssignment assignment, Role role) =>
        new(assignment.Id, role.Code, role.Name, assignment.EffectiveFrom, assignment.EffectiveTo,
            assignment.ApprovalStatus, assignment.Remarks, assignment.AssignmentType, assignment.IsPrimary,
            assignment.EndReason, assignment.EndedAt, assignment.EndedBy, assignment.Version);

    private static string NormalizeRoleCode(string value) => value.Trim().ToUpperInvariant();

    private sealed record EmployeeRoleScope(Guid CompanyId, string CompanyCode, Guid EmployeeId, string EmployeeCode);
}

public sealed class EmployeeRoleOperationConflictException(string message, Exception innerException) : Exception(message, innerException);
