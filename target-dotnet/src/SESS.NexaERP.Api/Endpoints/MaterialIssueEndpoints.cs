using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public static class MaterialIssueEndpoints
{
    public static IEndpointRouteBuilder MapMaterialIssueEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var requests = endpoints.MapGroup("/api/v1/stores/material-issue-requests")
            .WithTags("Stores - Material Issue Requests").RequireAuthorization()
            .AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope);
        requests.MapGet("/", (string? number, string? status, int? page, int? pageSize,
            IMaterialIssueService service, CancellationToken ct) =>
            service.ListRequestsAsync(number, status, page ?? 1, pageSize ?? 50, ct))
            .RequirePagePermission("stores.material-issue-requests", PagePermissionActions.View);
        requests.MapGet("/{id:guid}", async (Guid id, IMaterialIssueService service, CancellationToken ct) =>
            await service.GetRequestAsync(id, ct) is { } value ? Results.Ok(value) : Results.NotFound())
            .RequirePagePermission("stores.material-issue-requests", PagePermissionActions.View);
        requests.MapPost("/", (CreateMaterialIssueRequest request, IMaterialIssueService service,
            HttpContext h, CancellationToken ct) => RunCreated(() => service.CreateRequestAsync(request, ct), h))
            .RequirePagePermission("stores.material-issue-requests", PagePermissionActions.Create);
        requests.MapPut("/{id:guid}", (Guid id, UpdateMaterialIssueRequest request,
            IMaterialIssueService service, HttpContext h, CancellationToken ct) =>
            Run(() => service.UpdateRequestAsync(id, request, ct), h))
            .RequirePagePermission("stores.material-issue-requests", PagePermissionActions.Update);
        requests.MapPost("/{id:guid}/submit", (Guid id, MaterialIssueTransitionRequest request,
            IMaterialIssueService service, HttpContext h, CancellationToken ct) => Run(() => service.SubmitAsync(id, request, ct), h))
            .RequirePagePermission("stores.material-issue-requests", PagePermissionActions.Submit);
        requests.MapPost("/{id:guid}/approve", (Guid id, MaterialIssueTransitionRequest request,
            IMaterialIssueService service, HttpContext h, CancellationToken ct) => Run(() => service.ApproveAsync(id, request, ct), h))
            .RequirePagePermission("stores.material-issue-requests", PagePermissionActions.Approve);
        requests.MapPost("/{id:guid}/reject", (Guid id, MaterialIssueTransitionRequest request,
            IMaterialIssueService service, HttpContext h, CancellationToken ct) => Run(() => service.RejectAsync(id, request, ct), h))
            .RequirePagePermission("stores.material-issue-requests", PagePermissionActions.Reject);
        requests.MapPost("/{id:guid}/cancel", (Guid id, MaterialIssueTransitionRequest request,
            IMaterialIssueService service, HttpContext h, CancellationToken ct) => Run(() => service.CancelAsync(id, request, ct), h))
            .RequirePagePermission("stores.material-issue-requests", PagePermissionActions.Cancel);

        var excess = endpoints.MapGroup("/api/v1/stores/material-issue-excess")
            .WithTags("Stores - MIR Excess Decisions").RequireAuthorization()
            .AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope);
        excess.MapPost("/{lineId:guid}/decision", (Guid lineId, MaterialIssueExcessDecisionRequest request,
            IMaterialIssueService service, HttpContext h, CancellationToken ct) =>
            Run(() => service.DecideExcessAsync(lineId, request, ct), h))
            .RequirePagePermission("stores.material-issue-excess", PagePermissionActions.Approve);

        var issues = endpoints.MapGroup("/api/v1/stores/material-issues")
            .WithTags("Stores - Material Issues").RequireAuthorization()
            .AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope);
        issues.MapGet("/{id:guid}", async (Guid id, IMaterialIssueService service,
            NexaErpDbContext db, ICurrentUser user, IPagePermissionService permissions,
            CancellationToken ct) =>
        {
            if (!await CanViewAllIssuesAsync(user, permissions, ct))
            {
                var ownIssue = await db.MaterialIssues.AsNoTracking().AnyAsync(x =>
                    x.Id == id && x.IssuedToEmployeeId == user.EmployeeId
                    && db.Companies.Any(c => c.Id == x.CompanyId
                        && c.Code == user.OrganizationId), ct);
                if (!ownIssue) return Results.Forbid();
            }
            return await service.GetIssueAsync(id, ct) is { } value
                ? Results.Ok(value) : Results.NotFound();
        });
        issues.MapPost("/from-request/{requestId:guid}", (Guid requestId, CreateMaterialIssue request,
            IMaterialIssueService service, HttpContext h, CancellationToken ct) =>
            RunCreated(() => service.IssueAsync(requestId, request, ct), h))
            .RequirePagePermission("stores.material-issues", PagePermissionActions.Issue);
        issues.MapGet("/outstanding-custody", async (Guid? employeeId, bool? notificationDue,
            IMaterialIssueService service, ICurrentUser user, IPagePermissionService permissions,
            CancellationToken ct) =>
        {
            var canViewAll = await CanViewAllIssuesAsync(user, permissions, ct);
            if (!canViewAll && employeeId.HasValue && employeeId != user.EmployeeId)
                return Results.Forbid();
            var scopedEmployeeId = canViewAll ? employeeId : user.EmployeeId;
            return Results.Ok(await service.OutstandingCustodyAsync(
                scopedEmployeeId, notificationDue, ct));
        });
        issues.MapGet("/recipients", (IMaterialIssueService service, CancellationToken ct) =>
            service.ListIssueRecipientsAsync(ct))
            .RequirePagePermission("stores.material-issues", PagePermissionActions.View);
        issues.MapGet("/request-lines/{lineId:guid}/available-serials",
            async (Guid lineId, IMaterialIssueService service, CancellationToken ct) =>
                Results.Ok(await service.AvailableSerialsAsync(lineId, ct)))
            .RequirePagePermission("stores.material-issues", PagePermissionActions.View);

        var returns = endpoints.MapGroup("/api/v1/stores/material-returns")
            .WithTags("Stores - Material Returns").RequireAuthorization()
            .AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope);
        returns.MapGet("/", (Guid? materialIssueId, string? status, int? page, int? pageSize,
            IMaterialIssueService service, CancellationToken ct) =>
            service.ListReturnsAsync(materialIssueId, status, page ?? 1, pageSize ?? 50, ct))
            .RequirePagePermission("stores.material-returns", PagePermissionActions.View);
        returns.MapGet("/{id:guid}", async (Guid id, IMaterialIssueService service, CancellationToken ct) =>
            await service.GetReturnAsync(id, ct) is { } value ? Results.Ok(value) : Results.NotFound())
            .RequirePagePermission("stores.material-returns", PagePermissionActions.View);
        returns.MapPost("/from-issue/{materialIssueId:guid}", (Guid materialIssueId,
            CreateMaterialReturn request, IMaterialIssueService service, HttpContext h, CancellationToken ct) =>
            RunCreated(() => service.CreateReturnAsync(materialIssueId, request, ct), h))
            .RequirePagePermission("stores.material-returns", PagePermissionActions.Create);
        returns.MapPost("/{id:guid}/accept", (Guid id, AcceptMaterialReturn request,
            IMaterialIssueService service, HttpContext h, CancellationToken ct) =>
            Run(() => service.AcceptReturnAsync(id, request, ct), h))
            .RequirePagePermission("stores.material-returns", PagePermissionActions.Approve);
        return endpoints;
    }

    private static Task<IResult> Run<T>(Func<Task<T>> action, HttpContext h) =>
        Run(action, h, value => Results.Ok(value));
    private static async Task<bool> CanViewAllIssuesAsync(
        ICurrentUser user, IPagePermissionService permissions, CancellationToken ct)
    {
        const string page = "stores.material-issues";
        const string action = PagePermissionActions.View;
        if (!user.EmployeeId.HasValue || string.IsNullOrWhiteSpace(user.OrganizationId))
            return false;
        if (RoleAuthorityResolution.IsUniversalEmployeePermission(page, action))
            return true;
        if (await permissions.HasEmployeePermissionAsync(
                user.OrganizationId, user.EmployeeId.Value, page, action, ct))
            return true;
        foreach (var assignment in user.EffectiveRoleAssignments)
        {
            if (await permissions.HasPermissionAsync(
                    [assignment.RoleCode], page, action, ct))
                return true;
        }
        return false;
    }


    private static Task<IResult> RunCreated<T>(Func<Task<T>> action, HttpContext h) =>
        Run(action, h, value => Results.Created(string.Empty, value));

    private static async Task<IResult> Run<T>(Func<Task<T>> action, HttpContext h, Func<T, IResult> success)
    {
        try { return success(await action()); }
        catch (StoresValidationException e) { return Results.BadRequest(new { message = e.Message }); }
        catch (KeyNotFoundException e) { return Results.NotFound(new { message = e.Message }); }
        catch (UnauthorizedAccessException) { return h.User.Identity?.IsAuthenticated == true ? Results.Forbid() : Results.Unauthorized(); }
        catch (StoresConflictException e) { return Results.Conflict(new { message = e.Message }); }
        catch (DbUpdateConcurrencyException e) { return Results.Conflict(new { message = e.Message }); }
    }
}
