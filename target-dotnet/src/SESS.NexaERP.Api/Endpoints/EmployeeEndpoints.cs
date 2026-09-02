using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Employees;
using SESS.NexaERP.Domain.Employees;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public static class EmployeeEndpoints
{
    public static IEndpointRouteBuilder MapEmployeeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/employees")
            .WithTags("Employees")
            .RequireAuthorization();

        group.MapGet("/", async (NexaErpDbContext db, int? page, int? pageSize, string? search, string? status, CancellationToken cancellationToken) =>
        {
            var paging = MasterEndpointHelpers.NormalizePaging(page, pageSize);
            var query = db.Employees
                .AsNoTracking()
                .Include(employee => employee.Department)
                .Include(employee => employee.Designation)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToUpperInvariant();
                query = query.Where(employee => employee.EmployeeCode.ToUpper().Contains(term) || employee.EmployeeName.ToUpper().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalizedStatus = status.Trim();
                query = query.Where(employee => employee.Status == normalizedStatus);
            }

            var total = await query.CountAsync(cancellationToken);
            var pageEmployees = await query
                .OrderBy(employee => employee.EmployeeCode)
                .Skip(paging.Skip)
                .Take(paging.PageSize)
                .ToListAsync(cancellationToken);
            var employeeIds = pageEmployees.Select(employee => employee.Id).ToArray();
            var skillRows = await db.EmployeeSkills.AsNoTracking()
                .Where(skill => employeeIds.Contains(skill.EmployeeId))
                .OrderBy(skill => skill.Id)
                .Select(skill => new { skill.EmployeeId, SkillName = skill.Skill!.Name })
                .ToListAsync(cancellationToken);
            var skillsByEmployee = skillRows.GroupBy(x => x.EmployeeId)
                .ToDictionary(group => group.Key, group => group.Select(x => x.SkillName).FirstOrDefault() ?? string.Empty);
            var employees = pageEmployees.Select(employee => new EmployeeSummary(
                    employee.Id,
                    employee.EmployeeCode,
                    employee.EmployeeName,
                    employee.EmployeeType,
                    employee.Grade,
                    employee.Department == null ? string.Empty : employee.Department.Name,
                    skillsByEmployee.GetValueOrDefault(employee.Id, string.Empty),
                    employee.Designation == null ? string.Empty : employee.Designation.Name,
                    employee.Status,
                    employee.LoginEnabled,
                    employee.ApprovalStatus,
                    employee.Version)).ToList();

            return Results.Ok(new PagedResponse<EmployeeSummary>(total, paging.PageNumber, paging.PageSize, employees));
        }).RequirePagePermission("employees.master", PagePermissionActions.View);

        group.MapGet("/lookups", async (NexaErpDbContext db, CancellationToken cancellationToken) =>
        {
            var departments = await db.Departments.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
                .Select(x => new MasterLookupItem(x.Code, x.Name)).ToListAsync(cancellationToken);
            var skills = await db.Skills.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
                .Select(x => new MasterLookupItem(x.Code, x.Name)).ToListAsync(cancellationToken);
            var designations = await db.Designations.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
                .Select(x => new MasterLookupItem(x.Code, x.Name)).ToListAsync(cancellationToken);
            return Results.Ok(new EmployeeMasterLookups(departments, skills, designations));
        }).RequirePagePermission("employees.master", PagePermissionActions.View);


        // Quick-add endpoints so the employee form dropdowns can grow their
        // master tables inline.
        group.MapPost("/lookups/departments", (CreateLookupRequest request, NexaErpDbContext db, IAuditWriter audit, ICurrentUser currentUser, CancellationToken ct) =>
            CreateLookupAsync(request, db.Departments, (code, name) => new Department { Code = code, Name = name }, db, audit, currentUser, ct))
            .RequirePagePermission("employees.master", PagePermissionActions.Create);
        group.MapPost("/lookups/skills", (CreateLookupRequest request, NexaErpDbContext db, IAuditWriter audit, ICurrentUser currentUser, CancellationToken ct) =>
            CreateLookupAsync(request, db.Skills, (code, name) => new Skill { Code = code, Name = name }, db, audit, currentUser, ct))
            .RequirePagePermission("employees.master", PagePermissionActions.Create);
        group.MapPost("/lookups/designations", (CreateLookupRequest request, NexaErpDbContext db, IAuditWriter audit, ICurrentUser currentUser, CancellationToken ct) =>
            CreateLookupAsync(request, db.Designations, (code, name) => new Designation { Code = code, Name = name }, db, audit, currentUser, ct))
            .RequirePagePermission("employees.master", PagePermissionActions.Create);

        group.MapGet("/{employeeCode}", async (string employeeCode, NexaErpDbContext db, CancellationToken cancellationToken) =>
        {
            var employee = await db.Employees
                .AsNoTracking()
                .Include(existing => existing.Department)
                .Include(existing => existing.Designation)
                .SingleOrDefaultAsync(existing => existing.EmployeeCode == NormalizeEmployeeCode(employeeCode), cancellationToken);

            return employee is null
                ? Results.NotFound(new { message = "Employee not found." })
                : Results.Ok(await ToDetailAsync(employee, db, cancellationToken));
        }).RequirePagePermission("employees.master", PagePermissionActions.View);

        group.MapPost("/", async (CreateEmployeeRequest request, NexaErpDbContext db, IAuditWriter audit, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        {
            var code = NormalizeEmployeeCode(request.EmployeeCode);
            var name = NormalizeName(request.EmployeeName);
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(request.Remarks))
            {
                return Results.BadRequest(new { message = "Employee code, employee name and remarks are required." });
            }

            if (await db.Employees.AnyAsync(employee => employee.EmployeeCode == code, cancellationToken))
            {
                return Results.Conflict(new { message = $"Duplicate employee code blocked: {code}" });
            }

            var masters = await ResolveMastersAsync(db, request.DepartmentCode, request.SkillCode, request.DesignationCode, cancellationToken);
            if (masters is null)
            {
                return Results.BadRequest(new { message = "Valid department, skill and designation are required." });
            }

            var employee = new Employee
            {
                EmployeeCode = code,
                EmployeeName = name,
                OriginalImportedName = request.EmployeeName,
                EmployeeType = request.EmployeeType.Trim(),
                Grade = request.Grade.Trim(),
                DepartmentId = masters.Value.Department.Id,
                DesignationId = masters.Value.Designation.Id,
                DateOfJoining = request.DateOfJoining,
                OfficialEmail = NormalizeOptional(request.OfficialEmail),
                MobileNumber = NormalizeOptional(request.MobileNumber),
                LoginEnabled = false,
                Status = "Active",
                ApprovalStatus = "Draft",
                IsEmployeeCodeLocked = true,
                CreatedBy = currentUser.LoginId
            };

            db.Employees.Add(employee);
            db.EmployeeSkills.Add(new EmployeeSkill { EmployeeId = employee.Id, SkillId = masters.Value.Skill.Id, CreatedBy = currentUser.LoginId });
            db.EmployeeApprovalHistories.Add(new EmployeeApprovalHistory { EmployeeId = employee.Id, Action = "Create", FromStatus = "None", ToStatus = "Draft", Remarks = request.Remarks.Trim(), CreatedBy = currentUser.LoginId });
            await db.SaveChangesAsync(cancellationToken);
            await audit.WriteAsync("Employees", "Create", nameof(Employee), employee.Id.ToString(), null, employee, cancellationToken);

            return Results.Created($"/api/v1/employees/{employee.EmployeeCode}", await ToDetailAsync(employee, db, cancellationToken));
        }).RequirePagePermission("employees.master", PagePermissionActions.Create);

        group.MapPut("/{employeeCode}", async (string employeeCode, UpdateEmployeeRequest request, NexaErpDbContext db, IAuditWriter audit, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return Results.BadRequest(new { message = "Reason is required for employee update." });
            }

            var employee = await db.Employees.SingleOrDefaultAsync(existing => existing.EmployeeCode == NormalizeEmployeeCode(employeeCode), cancellationToken);
            if (employee is null)
            {
                return Results.NotFound(new { message = "Employee not found." });
            }
            if (request.Version != employee.Version)
            {
                return Results.Conflict(new { message = "Stale employee version. Refresh and retry." });
            }

            var masters = await ResolveMastersAsync(db, request.DepartmentCode, request.SkillCode, request.DesignationCode, cancellationToken);
            if (masters is null)
            {
                return Results.BadRequest(new { message = "Valid department, skill and designation are required." });
            }

            var before = new { employee.EmployeeName, employee.EmployeeType, employee.Grade, employee.DepartmentId, employee.DesignationId, employee.DateOfJoining, employee.OfficialEmail, employee.MobileNumber };
            employee.EmployeeName = NormalizeName(request.EmployeeName);
            employee.EmployeeType = request.EmployeeType.Trim();
            employee.Grade = request.Grade.Trim();
            employee.DepartmentId = masters.Value.Department.Id;
            employee.DesignationId = masters.Value.Designation.Id;
            employee.DateOfJoining = request.DateOfJoining;
            employee.OfficialEmail = NormalizeOptional(request.OfficialEmail);
            employee.MobileNumber = NormalizeOptional(request.MobileNumber);
            employee.UpdatedAt = DateTimeOffset.UtcNow;
            employee.UpdatedBy = currentUser.LoginId;

            var existingSkill = await db.EmployeeSkills.SingleOrDefaultAsync(existing => existing.EmployeeId == employee.Id, cancellationToken);
            if (existingSkill is null)
            {
                db.EmployeeSkills.Add(new EmployeeSkill { EmployeeId = employee.Id, SkillId = masters.Value.Skill.Id, CreatedBy = currentUser.LoginId });
            }
            else
            {
                existingSkill.SkillId = masters.Value.Skill.Id;
                existingSkill.UpdatedAt = DateTimeOffset.UtcNow;
                existingSkill.UpdatedBy = currentUser.LoginId;
            }

            db.EmployeeApprovalHistories.Add(new EmployeeApprovalHistory { EmployeeId = employee.Id, Action = "Update", FromStatus = employee.ApprovalStatus, ToStatus = employee.ApprovalStatus, Remarks = request.Reason.Trim(), CreatedBy = currentUser.LoginId });
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Results.Conflict(new { message = "Employee was changed by another user. Refresh and retry." });
            }
            await db.Entry(employee).ReloadAsync(cancellationToken);
            await audit.WriteAsync("Employees", "Update", nameof(Employee), employee.Id.ToString(), before, employee, cancellationToken);

            return Results.Ok(await ToDetailAsync(employee, db, cancellationToken));
        }).RequirePagePermission("employees.master", PagePermissionActions.Update);

        group.MapPost("/{employeeCode}/submit", async (string employeeCode, EmployeeApprovalRequest request, NexaErpDbContext db, ICurrentUser currentUser, IAuditWriter audit, CancellationToken cancellationToken) =>
            await ChangeApprovalStatusAsync(employeeCode, "Submit", "Submitted", request.Remarks, request.Version, db, currentUser, audit, cancellationToken))
            .RequirePagePermission("employees.master", PagePermissionActions.Submit);

        group.MapPost("/{employeeCode}/approve", async (string employeeCode, EmployeeApprovalRequest request, NexaErpDbContext db, ICurrentUser currentUser, IAuditWriter audit, CancellationToken cancellationToken) =>
            await ChangeApprovalStatusAsync(employeeCode, "Approve", "Approved", request.Remarks, request.Version, db, currentUser, audit, cancellationToken))
            .RequirePagePermission("employees.master", PagePermissionActions.Approve);

        group.MapPost("/{employeeCode}/reject", async (string employeeCode, EmployeeApprovalRequest request, NexaErpDbContext db, ICurrentUser currentUser, IAuditWriter audit, CancellationToken cancellationToken) =>
            await ChangeApprovalStatusAsync(employeeCode, "Reject", "Rejected", request.Remarks, request.Version, db, currentUser, audit, cancellationToken))
            .RequirePagePermission("employees.master", PagePermissionActions.Reject);

        group.MapPost("/{employeeCode}/revise", async (string employeeCode, EmployeeApprovalRequest request, NexaErpDbContext db, ICurrentUser currentUser, IAuditWriter audit, CancellationToken cancellationToken) =>
            await ChangeApprovalStatusAsync(employeeCode, "RequestRevision", "RevisionRequested", request.Remarks, request.Version, db, currentUser, audit, cancellationToken))
            .RequirePagePermission("employees.master", PagePermissionActions.RequestRevision);

        group.MapPost("/{employeeCode}/activate-login", async (string employeeCode, LoginStatusRequest request, NexaErpDbContext db, ICurrentUser currentUser, IAuditWriter audit, CancellationToken cancellationToken) =>
            await ChangeLoginAsync(employeeCode, true, request.Reason, request.Version, db, currentUser, audit, cancellationToken))
            .RequirePagePermission("employees.master", PagePermissionActions.Update);

        group.MapPost("/{employeeCode}/deactivate-login", async (string employeeCode, LoginStatusRequest request, NexaErpDbContext db, ICurrentUser currentUser, IAuditWriter audit, CancellationToken cancellationToken) =>
            await ChangeLoginAsync(employeeCode, false, request.Reason, request.Version, db, currentUser, audit, cancellationToken))
            .RequirePagePermission("employees.master", PagePermissionActions.Deactivate);

        group.MapPost("/{employeeCode}/roles", async (string employeeCode, AssignEmployeeRoleRequest request, NexaErpDbContext db, ICurrentUser currentUser, IAuditWriter audit, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Remarks))
            {
                return Results.BadRequest(new { message = "Remarks are required for role assignment." });
            }

            var employee = await db.Employees.SingleOrDefaultAsync(existing => existing.EmployeeCode == NormalizeEmployeeCode(employeeCode), cancellationToken);
            var roleCode = request.RoleCode.Trim().ToUpperInvariant();
            var role = await db.Roles.SingleOrDefaultAsync(existing => existing.Code == roleCode && existing.IsActive, cancellationToken);
            if (employee is null || role is null)
            {
                return Results.BadRequest(new { message = "Valid employee and active ERP role are required." });
            }

            var duplicate = await db.EmployeeRoleAssignments.AnyAsync(existing => existing.EmployeeId == employee.Id && existing.RoleId == role.Id && existing.EffectiveTo == null, cancellationToken);
            if (duplicate)
            {
                return Results.Conflict(new { message = "Active employee-role mapping already exists." });
            }

            var assignment = new EmployeeRoleAssignment
            {
                EmployeeId = employee.Id,
                RoleId = role.Id,
                EffectiveFrom = request.EffectiveFrom,
                EffectiveTo = request.EffectiveTo,
                ApprovalStatus = "PendingApproval",
                Remarks = request.Remarks.Trim(),
                CreatedBy = currentUser.LoginId
            };
            db.EmployeeRoleAssignments.Add(assignment);
            db.EmployeeApprovalHistories.Add(new EmployeeApprovalHistory { EmployeeId = employee.Id, Action = "AssignRole", FromStatus = "None", ToStatus = "PendingApproval", Remarks = $"{role.Code}: {request.Remarks.Trim()}", CreatedBy = currentUser.LoginId });
            await db.SaveChangesAsync(cancellationToken);
            await audit.WriteAsync("Employees", "AssignRole", nameof(EmployeeRoleAssignment), assignment.Id.ToString(), null, assignment, cancellationToken);

            return Results.Created($"/api/v1/employees/{employee.EmployeeCode}/roles/{assignment.Id}", new EmployeeRoleSummary(assignment.Id, role.Code, role.Name, assignment.EffectiveFrom, assignment.EffectiveTo, assignment.ApprovalStatus, assignment.Remarks));
        }).RequirePagePermission("employees.role-mapping", PagePermissionActions.Create);

        group.MapGet("/{employeeCode}/roles", async (string employeeCode, NexaErpDbContext db, CancellationToken cancellationToken) =>
        {
            var employee = await db.Employees.AsNoTracking().SingleOrDefaultAsync(existing => existing.EmployeeCode == NormalizeEmployeeCode(employeeCode), cancellationToken);
            if (employee is null)
            {
                return Results.NotFound(new { message = "Employee not found." });
            }

            var roles = await db.EmployeeRoleAssignments
                .AsNoTracking()
                .Include(assignment => assignment.Role)
                .Where(assignment => assignment.EmployeeId == employee.Id)
                .OrderByDescending(assignment => assignment.EffectiveFrom)
                .Select(assignment => new EmployeeRoleSummary(assignment.Id, assignment.Role == null ? string.Empty : assignment.Role.Code, assignment.Role == null ? string.Empty : assignment.Role.Name, assignment.EffectiveFrom, assignment.EffectiveTo, assignment.ApprovalStatus, assignment.Remarks))
                .ToListAsync(cancellationToken);

            return Results.Ok(roles);
        }).RequirePagePermission("employees.role-mapping", PagePermissionActions.View);

        group.MapGet("/{employeeCode}/history", async (string employeeCode, NexaErpDbContext db, CancellationToken cancellationToken) =>
        {
            var employee = await db.Employees.AsNoTracking().SingleOrDefaultAsync(existing => existing.EmployeeCode == NormalizeEmployeeCode(employeeCode), cancellationToken);
            if (employee is null)
            {
                return Results.NotFound(new { message = "Employee not found." });
            }

            var history = await db.EmployeeApprovalHistories
                .AsNoTracking()
                .Where(item => item.EmployeeId == employee.Id)
                .OrderByDescending(item => item.CreatedAt)
                .Select(item => new EmployeeHistorySummary(item.Id, item.Action, item.FromStatus, item.ToStatus, item.Remarks, item.CreatedAt, item.CreatedBy))
                .ToListAsync(cancellationToken);

            return Results.Ok(history);
        }).RequirePagePermission("employees.audit-history", PagePermissionActions.ViewAuditHistory);

        return endpoints;
    }

    private static async Task<IResult> ChangeApprovalStatusAsync(string employeeCode, string action, string newStatus, string remarks, uint version, NexaErpDbContext db, ICurrentUser currentUser, IAuditWriter audit, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(remarks))
        {
            return Results.BadRequest(new { message = "Remarks are required." });
        }

        var employee = await db.Employees.SingleOrDefaultAsync(existing => existing.EmployeeCode == NormalizeEmployeeCode(employeeCode), cancellationToken);
        if (employee is null)
        {
            return Results.NotFound(new { message = "Employee not found." });
        }
        if (version != employee.Version) return Results.Conflict(new { message = "Stale employee version. Refresh and retry." });

        var before = new { employee.ApprovalStatus };
        var oldStatus = employee.ApprovalStatus;
        employee.ApprovalStatus = newStatus;
        employee.UpdatedAt = DateTimeOffset.UtcNow;
        employee.UpdatedBy = currentUser.LoginId;
        db.EmployeeApprovalHistories.Add(new EmployeeApprovalHistory { EmployeeId = employee.Id, Action = action, FromStatus = oldStatus, ToStatus = newStatus, Remarks = remarks.Trim(), CreatedBy = currentUser.LoginId });
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return Results.Conflict(new { message = "Employee changed concurrently. Refresh and retry." }); }
        await db.Entry(employee).ReloadAsync(cancellationToken);
        await audit.WriteAsync("Employees", action, nameof(Employee), employee.Id.ToString(), before, employee, cancellationToken);
        return Results.Ok(new { employee.EmployeeCode, employee.ApprovalStatus, employee.Version });
    }

    private static async Task<IResult> ChangeLoginAsync(string employeeCode, bool enabled, string reason, uint version, NexaErpDbContext db, ICurrentUser currentUser, IAuditWriter audit, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Results.BadRequest(new { message = "Reason is required." });
        }

        var employee = await db.Employees.SingleOrDefaultAsync(existing => existing.EmployeeCode == NormalizeEmployeeCode(employeeCode), cancellationToken);
        if (employee is null)
        {
            return Results.NotFound(new { message = "Employee not found." });
        }
        if (version != employee.Version) return Results.Conflict(new { message = "Stale employee version. Refresh and retry." });

        var before = new { employee.LoginEnabled, employee.Status };
        var oldStatus = employee.Status;
        employee.LoginEnabled = enabled;
        employee.Status = enabled ? "Active" : "Inactive";
        employee.UpdatedAt = DateTimeOffset.UtcNow;
        employee.UpdatedBy = currentUser.LoginId;
        db.EmployeeStatusHistories.Add(new EmployeeStatusHistory { EmployeeId = employee.Id, OldStatus = oldStatus, NewStatus = employee.Status, Reason = reason.Trim(), CreatedBy = currentUser.LoginId });
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return Results.Conflict(new { message = "Employee changed concurrently. Refresh and retry." }); }
        await db.Entry(employee).ReloadAsync(cancellationToken);
        await audit.WriteAsync("Employees", enabled ? "ActivateLogin" : "DeactivateLogin", nameof(Employee), employee.Id.ToString(), before, employee, cancellationToken);
        return Results.Ok(new { employee.EmployeeCode, employee.LoginEnabled, employee.Status, employee.Version });
    }

    private static async Task<EmployeeDetail> ToDetailAsync(Employee employee, NexaErpDbContext db, CancellationToken cancellationToken)
    {
        var skillNames = await db.EmployeeSkills
            .AsNoTracking()
            .Include(employeeSkill => employeeSkill.Skill)
            .Where(employeeSkill => employeeSkill.EmployeeId == employee.Id)
            .Select(employeeSkill => employeeSkill.Skill == null ? string.Empty : employeeSkill.Skill.Name)
            .ToListAsync(cancellationToken);

        var roles = await db.EmployeeRoleAssignments
            .AsNoTracking()
            .Include(assignment => assignment.Role)
            .Where(assignment => assignment.EmployeeId == employee.Id)
            .OrderBy(assignment => assignment.Role!.Code)
            .Select(assignment => new EmployeeRoleSummary(assignment.Id, assignment.Role == null ? string.Empty : assignment.Role.Code, assignment.Role == null ? string.Empty : assignment.Role.Name, assignment.EffectiveFrom, assignment.EffectiveTo, assignment.ApprovalStatus, assignment.Remarks))
            .ToListAsync(cancellationToken);

        var departmentName = employee.Department?.Name ?? await db.Departments.Where(department => department.Id == employee.DepartmentId).Select(department => department.Name).SingleAsync(cancellationToken);
        var designationName = employee.Designation?.Name ?? await db.Designations.Where(designation => designation.Id == employee.DesignationId).Select(designation => designation.Name).SingleAsync(cancellationToken);
        return new EmployeeDetail(employee.Id, employee.EmployeeCode, employee.EmployeeName, employee.OriginalImportedName, employee.EmployeeType, employee.Grade, departmentName, skillNames, designationName, employee.Status, employee.DateOfJoining, employee.OfficialEmail, employee.MobileNumber, employee.LoginEnabled, employee.ApprovalStatus, roles, employee.Version);
    }

    private static async Task<(Department Department, Skill Skill, Designation Designation)?> ResolveMastersAsync(NexaErpDbContext db, string departmentCode, string skillCode, string designationCode, CancellationToken cancellationToken)
    {
        var department = await db.Departments.SingleOrDefaultAsync(existing => existing.Code == NormalizeCode(departmentCode) && existing.IsActive, cancellationToken);
        var skill = await db.Skills.SingleOrDefaultAsync(existing => existing.Code == NormalizeCode(skillCode) && existing.IsActive, cancellationToken);
        var designation = await db.Designations.SingleOrDefaultAsync(existing => existing.Code == NormalizeCode(designationCode) && existing.IsActive, cancellationToken);
        return department is null || skill is null || designation is null ? null : (department, skill, designation);
    }

    public sealed record CreateLookupRequest(string Code, string Name);


    private static async Task<IResult> CreateLookupAsync<TEntity>(
        CreateLookupRequest request,
        Microsoft.EntityFrameworkCore.DbSet<TEntity> set,
        Func<string, string, TEntity> factory,
        NexaErpDbContext db,
        IAuditWriter audit,
        ICurrentUser currentUser,
        CancellationToken cancellationToken) where TEntity : SESS.NexaERP.Domain.Common.AuditableEntity
    {
        var code = request.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        var name = request.Name?.Trim() ?? string.Empty;
        if (code.Length == 0 || name.Length == 0)
        {
            return Results.BadRequest(new { message = "Code and name are required." });
        }
        if (await set.AnyAsync(entity => EF.Property<string>(entity, "Code") == code, cancellationToken))
        {
            return Results.Conflict(new { message = $"Duplicate code blocked: {code}" });
        }

        var entity = factory(code, name);
        entity.CreatedBy = currentUser.LoginId;
        set.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("Employees", "CreateLookup", typeof(TEntity).Name, entity.Id.ToString(), null, new { code, name }, cancellationToken);
        return Results.Created($"/api/v1/employees/lookups/{typeof(TEntity).Name.ToLowerInvariant()}s/{entity.Id}", new { entity.Id, Code = code, Name = name });
    }

    private static string NormalizeEmployeeCode(string value) => value.Trim().ToUpperInvariant();

    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();

    private static string NormalizeName(string value) => string.Join(' ', value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
