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
        EmployeeRoleAssignmentTypes.Full,
        EmployeeRoleAssignmentTypes.Support,
        EmployeeRoleAssignmentTypes.Temporary
    ];

    private static readonly string[] ConfigurationAuthorityRoles =
    ["TECHNICAL_DIRECTOR", "MANAGING_DIRECTOR", "IT_MANAGER"];

    private static async Task<IResult> AssignRoleAsync(
        string employeeCode, AssignEmployeeRoleRequest request, NexaErpDbContext db,
        ICurrentUser user, IAuditWriter audit, CancellationToken ct)
    {
        var type = NormalizeRoleCode(request.AssignmentType);
        if (!ValidAssignment(type, request.EffectiveFrom, request.EffectiveTo, request.Remarks, out var error))
            return Results.BadRequest(new { message = error });

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var scope = await ResolveRoleScopeAsync(employeeCode, request.EffectiveFrom, db, user, ct);
        if (scope is null) return Results.BadRequest(new { message = "Employee is not active in the selected company on the effective date." });
        var actorRole = RequireConfigurationAuthority(user, scope.EmployeeId);
        var role = await ResolveAssignableRoleAsync(request.RoleCode, scope.CompanyId, request.EffectiveFrom, db, ct);
        if (role is null) return Results.BadRequest(new { message = "The role is not employee-assignable and enabled in this company on the effective date." });
        if (await HasOverlapAsync(scope, role.Id, request.EffectiveFrom, request.EffectiveTo, null, db, ct))
            return Results.Conflict(new { message = "This employee already has an overlapping assignment for that role." });

        await SetDatabaseActorAsync(db, user, ct);
        var assignment = NewAssignment(scope, role.Id, request.EffectiveFrom, request.EffectiveTo, type, request.Remarks, user);
        db.EmployeeRoleAssignments.Add(assignment);
        AddEvent(db, scope, assignment.Id, "ASSIGN", null, role.Code, null, type,
            null, null, request.EffectiveFrom, request.EffectiveTo, request.Remarks, user, actorRole);
        await SaveRoleOperationAsync(db, ct);
        await audit.WriteAsync("Employees", "AssignRole", nameof(EmployeeRoleAssignment), assignment.Id.ToString(), null,
            new { Assignment = RoleSummary(assignment, role), ActorRoleCode = actorRole }, ct);
        await transaction.CommitAsync(ct);
        return Results.Created($"/api/v1/employees/{scope.EmployeeCode}/roles/{assignment.Id}", RoleSummary(assignment, role));
    }

    private static Task<IResult> AssignTemporaryCoverAsync(
        string employeeCode, TemporaryRoleCoverRequest request, NexaErpDbContext db,
        ICurrentUser user, IAuditWriter audit, CancellationToken ct) =>
        AssignRoleAsync(employeeCode,
            new AssignEmployeeRoleRequest(request.RoleCode, EmployeeRoleAssignmentTypes.Temporary,
                request.EffectiveFrom, request.EffectiveTo, request.Remarks), db, user, audit, ct);

    private static async Task<IResult> ChangeRoleAssignmentAsync(
        string employeeCode, Guid previousAssignmentId, string newRoleCode, string newAssignmentType,
        DateOnly effectiveOn, bool keepPrevious, string remarks, uint previousVersion, string operation,
        NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct)
    {
        var type = NormalizeRoleCode(newAssignmentType);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (type is not (EmployeeRoleAssignmentTypes.Full or EmployeeRoleAssignmentTypes.Support) ||
            string.IsNullOrWhiteSpace(remarks) || effectiveOn < today)
            return Results.BadRequest(new { message = "Promotion/transfer requires FULL or SUPPORT, a present/future effective date and remarks." });

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var scope = await ResolveRoleScopeAsync(employeeCode, effectiveOn, db, user, ct);
        if (scope is null) return Results.BadRequest(new { message = "Employee is not active in the selected company on the effective date." });
        var actorRole = RequireConfigurationAuthority(user, scope.EmployeeId);
        var previous = await db.EmployeeRoleAssignments.Include(x => x.Role).SingleOrDefaultAsync(x =>
            x.Id == previousAssignmentId && x.CompanyId == scope.CompanyId && x.EmployeeId == scope.EmployeeId, ct);
        if (previous?.Role is null) return Results.NotFound(new { message = "Previous role assignment not found." });
        if (previous.Version != previousVersion) return Results.Conflict(new { message = "Previous role assignment Version is stale." });
        if (previous.EffectiveFrom >= effectiveOn || previous.EffectiveTo.HasValue && previous.EffectiveTo.Value < effectiveOn)
            return Results.Conflict(new { message = "The previous assignment is not effective immediately before the requested change." });

        var role = await ResolveAssignableRoleAsync(newRoleCode, scope.CompanyId, effectiveOn, db, ct);
        if (role is null) return Results.BadRequest(new { message = "The new role is not employee-assignable and enabled in this company." });
        if (keepPrevious && role.Id == previous.RoleId)
            return Results.Conflict(new { message = "The same role cannot be retained while adding an overlapping replacement." });
        if (await HasOverlapAsync(scope, role.Id, effectiveOn, null, previous.Id, db, ct))
            return Results.Conflict(new { message = "The replacement role already overlaps another assignment." });

        await SetDatabaseActorAsync(db, user, ct);
        var before = RoleSummary(previous, previous.Role);
        if (!keepPrevious) EndAssignment(previous, effectiveOn.AddDays(-1), remarks, user);
        var replacement = NewAssignment(scope, role.Id, effectiveOn, null, type, remarks, user);
        db.EmployeeRoleAssignments.Add(replacement);
        AddEvent(db, scope, replacement.Id, operation, previous.Role.Code, role.Code,
            previous.AssignmentType, type, previous.EffectiveFrom, previous.EffectiveTo,
            effectiveOn, null, remarks, user, actorRole);
        await SaveRoleOperationAsync(db, ct);
        await audit.WriteAsync("Employees", operation, nameof(EmployeeRoleAssignment), replacement.Id.ToString(),
            new { Assignment = before, Retained = keepPrevious },
            new { Assignment = RoleSummary(replacement, role), ActorRoleCode = actorRole }, ct);
        await transaction.CommitAsync(ct);
        return Results.Ok(await BuildPortfolioAsync(scope, db, ct));
    }

    private static async Task<IResult> EndRoleAssignmentAsync(
        string employeeCode, Guid assignmentId, EndEmployeeRoleAssignmentRequest request,
        NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (string.IsNullOrWhiteSpace(request.Reason) || request.EffectiveTo < today)
            return Results.BadRequest(new { message = "An end reason and a present/future EffectiveTo are required; past history cannot be rewritten." });

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var scope = await ResolveRoleScopeAsync(employeeCode, today, db, user, ct);
        if (scope is null) return Results.NotFound(new { message = "Employee is not active in the selected company." });
        var actorRole = RequireConfigurationAuthority(user, scope.EmployeeId);
        var assignment = await db.EmployeeRoleAssignments.Include(x => x.Role).SingleOrDefaultAsync(x =>
            x.Id == assignmentId && x.CompanyId == scope.CompanyId && x.EmployeeId == scope.EmployeeId, ct);
        if (assignment?.Role is null) return Results.NotFound(new { message = "Role assignment not found." });
        if (assignment.Version != request.Version) return Results.Conflict(new { message = "Role assignment Version is stale." });
        if (request.EffectiveTo < assignment.EffectiveFrom || assignment.EffectiveTo.HasValue && assignment.EffectiveTo.Value <= today)
            return Results.Conflict(new { message = "The assignment is historical or the requested end precedes its start." });
        if (assignment.EffectiveTo.HasValue && request.EffectiveTo > assignment.EffectiveTo.Value)
            return Results.Conflict(new { message = "Ending an assignment cannot extend its existing period." });

        await SetDatabaseActorAsync(db, user, ct);
        var before = RoleSummary(assignment, assignment.Role);
        var priorTo = assignment.EffectiveTo;
        EndAssignment(assignment, request.EffectiveTo, request.Reason, user);
        AddEvent(db, scope, assignment.Id, "END_ASSIGNMENT", assignment.Role.Code, assignment.Role.Code,
            assignment.AssignmentType, assignment.AssignmentType, assignment.EffectiveFrom, priorTo,
            assignment.EffectiveFrom, request.EffectiveTo, request.Reason, user, actorRole);
        await SaveRoleOperationAsync(db, ct);
        await audit.WriteAsync("Employees", "EndRoleAssignment", nameof(EmployeeRoleAssignment), assignment.Id.ToString(),
            before, new { Assignment = RoleSummary(assignment, assignment.Role), ActorRoleCode = actorRole }, ct);
        await transaction.CommitAsync(ct);
        return Results.Ok(RoleSummary(assignment, assignment.Role));
    }

    private static async Task<IResult> GetRolePortfolioAsync(
        string employeeCode, NexaErpDbContext db, ICurrentUser user, CancellationToken ct)
    {
        var scope = await ResolveRoleScopeAsync(employeeCode, DateOnly.FromDateTime(DateTime.UtcNow), db, user, ct);
        return scope is null ? Results.NotFound(new { message = "Employee is not active in the selected company." })
            : Results.Ok(await BuildPortfolioAsync(scope, db, ct));
    }

    private static async Task<IResult> GetRoleEventsAsync(
        string employeeCode, NexaErpDbContext db, ICurrentUser user, CancellationToken ct)
    {
        var scope = await ResolveRoleScopeAsync(employeeCode, DateOnly.FromDateTime(DateTime.UtcNow), db, user, ct);
        if (scope is null) return Results.NotFound(new { message = "Employee is not active in the selected company." });
        var events = await db.EmployeeRoleAssignmentEvents.AsNoTracking()
            .Where(x => x.CompanyId == scope.CompanyId && x.EmployeeId == scope.EmployeeId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new EmployeeRoleAssignmentEventSummary(x.Id, x.AssignmentId, x.Operation,
                x.FromRoleCode, x.ToRoleCode, x.FromAssignmentType, x.ToAssignmentType,
                x.PreviousEffectiveFrom, x.PreviousEffectiveTo, x.NewEffectiveFrom, x.NewEffectiveTo,
                x.Reason, x.ActorEmployeeId, x.ActorLoginId, x.ActorRoleCode, x.CreatedAt))
            .ToListAsync(ct);
        return Results.Ok(events);
    }

    private static async Task<EmployeeRolePortfolioSummary> BuildPortfolioAsync(
        EmployeeRoleScope scope, NexaErpDbContext db, CancellationToken ct)
    {
        var assignments = await db.EmployeeRoleAssignments.AsNoTracking().Include(x => x.Role)
            .Where(x => x.CompanyId == scope.CompanyId && x.EmployeeId == scope.EmployeeId)
            .OrderByDescending(x => x.EffectiveFrom).ThenBy(x => x.Role!.Code)
            .Select(x => new EmployeeRoleSummary(x.Id, x.Role == null ? string.Empty : x.Role.Code,
                x.Role == null ? string.Empty : x.Role.Name, x.EffectiveFrom, x.EffectiveTo,
                x.ApprovalStatus, x.Remarks, x.AssignmentType, x.EndReason, x.EndedAt, x.EndedBy, x.Version))
            .ToListAsync(ct);
        return new(scope.EmployeeCode, scope.CompanyCode, assignments);
    }

    private static async Task<EmployeeRoleScope?> ResolveRoleScopeAsync(
        string employeeCode, DateOnly effectiveOn, NexaErpDbContext db, ICurrentUser user, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(user.OrganizationId)) return null;
        var organization = user.OrganizationId.Trim().ToUpperInvariant();
        var employee = await db.Employees.AsNoTracking().SingleOrDefaultAsync(x =>
            x.EmployeeCode == NormalizeEmployeeCode(employeeCode) && x.Status == "Active", ct);
        if (employee is null) return null;
        var company = await db.Companies.AsNoTracking().SingleOrDefaultAsync(x =>
            x.Code == organization && x.IsActive && x.Status == "ACTIVE", ct);
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
                activation.RoleId == role.Id && activation.IsEnabled && activation.EffectiveFrom <= effectiveOn &&
                (!activation.EffectiveTo.HasValue || activation.EffectiveTo.Value >= effectiveOn)), ct);
    }

    private static async Task<bool> HasOverlapAsync(
        EmployeeRoleScope scope, Guid roleId, DateOnly from, DateOnly? to, Guid? excludeId,
        NexaErpDbContext db, CancellationToken ct)
    {
        var end = to ?? DateOnly.MaxValue;
        return await db.EmployeeRoleAssignments.AsNoTracking().AnyAsync(x =>
            x.CompanyId == scope.CompanyId && x.EmployeeId == scope.EmployeeId && x.RoleId == roleId &&
            (!excludeId.HasValue || x.Id != excludeId.Value) && x.ApprovalStatus != "Rejected" &&
            x.EffectiveFrom <= end && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= from), ct);
    }

    private static bool ValidAssignment(string type, DateOnly from, DateOnly? to, string remarks, out string error)
    {
        error = string.Empty;
        if (!AssignmentTypes.Contains(type, StringComparer.Ordinal) || string.IsNullOrWhiteSpace(remarks) || to < from)
        { error = "Assignment type, valid dates and remarks are required."; return false; }
        if (type == EmployeeRoleAssignmentTypes.Temporary && !to.HasValue)
        { error = "TEMPORARY assignments require EffectiveTo."; return false; }
        if (type != EmployeeRoleAssignmentTypes.Temporary && to.HasValue)
        { error = "FULL and SUPPORT assignments are open-ended and must be ended through the explicit end operation."; return false; }
        return true;
    }

    private static string RequireConfigurationAuthority(ICurrentUser user, Guid targetEmployeeId)
    {
        if (!user.EmployeeId.HasValue) throw new UnauthorizedAccessException("An authenticated employee is required.");
        if (user.EmployeeId.Value == targetEmployeeId)
            throw new UnauthorizedAccessException("Employees may not create, change or end their own role assignments.");
        return user.RequireRole("role-administration", ConfigurationAuthorityRoles);
    }

    private static Task SetDatabaseActorAsync(NexaErpDbContext db, ICurrentUser user, CancellationToken ct) =>
        db.Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('sess.role_authority_assignment_id', {user.ResolvedRoleAssignmentId!.Value.ToString()}, true)", ct);

    private static EmployeeRoleAssignment NewAssignment(
        EmployeeRoleScope scope, Guid roleId, DateOnly from, DateOnly? to, string type,
        string remarks, ICurrentUser user) => new()
    {
        CompanyId = scope.CompanyId, EmployeeId = scope.EmployeeId, RoleId = roleId,
        EffectiveFrom = from, EffectiveTo = to, AssignmentType = type,
        ApprovalStatus = "Approved", Remarks = remarks.Trim(), CreatedBy = user.LoginId
    };

    private static void EndAssignment(EmployeeRoleAssignment assignment, DateOnly effectiveTo, string reason, ICurrentUser user)
    {
        assignment.EffectiveTo = effectiveTo;
        if (effectiveTo < DateOnly.FromDateTime(DateTime.UtcNow)) assignment.ApprovalStatus = "Ended";
        assignment.EndReason = reason.Trim(); assignment.EndedAt = DateTimeOffset.UtcNow; assignment.EndedBy = user.LoginId;
        assignment.Version = checked(assignment.Version + 1); assignment.UpdatedAt = DateTimeOffset.UtcNow; assignment.UpdatedBy = user.LoginId;
    }

    private static void AddEvent(
        NexaErpDbContext db, EmployeeRoleScope scope, Guid? assignmentId, string operation,
        string? fromRole, string? toRole, string? fromType, string? toType,
        DateOnly? previousFrom, DateOnly? previousTo, DateOnly? newFrom, DateOnly? newTo,
        string reason, ICurrentUser user, string actorRole) =>
        db.EmployeeRoleAssignmentEvents.Add(new EmployeeRoleAssignmentEvent
        {
            CompanyId = scope.CompanyId, EmployeeId = scope.EmployeeId, ActorEmployeeId = user.EmployeeId!.Value,
            AssignmentId = assignmentId, Operation = operation, FromRoleCode = fromRole, ToRoleCode = toRole,
            FromAssignmentType = fromType, ToAssignmentType = toType,
            PreviousEffectiveFrom = previousFrom, PreviousEffectiveTo = previousTo,
            NewEffectiveFrom = newFrom, NewEffectiveTo = newTo, Reason = reason.Trim(),
            ActorLoginId = user.LoginId, ActorRoleCode = actorRole, CreatedBy = user.LoginId
        });

    private static async Task SaveRoleOperationAsync(NexaErpDbContext db, CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException ex) { throw new EmployeeRoleOperationConflictException("Role assignments changed concurrently. Refresh and retry.", ex); }
        catch (DbUpdateException ex) { throw new EmployeeRoleOperationConflictException("Role dates overlap or violate assignment governance.", ex); }
    }

    private static EmployeeRoleSummary RoleSummary(EmployeeRoleAssignment assignment, Role role) =>
        new(assignment.Id, role.Code, role.Name, assignment.EffectiveFrom, assignment.EffectiveTo,
            assignment.ApprovalStatus, assignment.Remarks, assignment.AssignmentType,
            assignment.EndReason, assignment.EndedAt, assignment.EndedBy, assignment.Version);

    private static string NormalizeRoleCode(string value) => value.Trim().ToUpperInvariant();
    private sealed record EmployeeRoleScope(Guid CompanyId, string CompanyCode, Guid EmployeeId, string EmployeeCode);
}

public sealed class EmployeeRoleOperationConflictException(string message, Exception innerException) : Exception(message, innerException);
