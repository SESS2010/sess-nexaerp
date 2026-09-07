using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Stores;

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
        issues.MapGet("/{id:guid}", async (Guid id, IMaterialIssueService service, CancellationToken ct) =>
            await service.GetIssueAsync(id, ct) is { } value ? Results.Ok(value) : Results.NotFound())
            .RequirePagePermission("stores.material-issues", PagePermissionActions.View);
        issues.MapPost("/from-request/{requestId:guid}", (Guid requestId, CreateMaterialIssue request,
            IMaterialIssueService service, HttpContext h, CancellationToken ct) =>
            RunCreated(() => service.IssueAsync(requestId, request, ct), h))
            .RequirePagePermission("stores.material-issues", PagePermissionActions.Issue);
        issues.MapGet("/outstanding-custody", (Guid? employeeId, bool? notificationDue,
            IMaterialIssueService service, CancellationToken ct) =>
            service.OutstandingCustodyAsync(employeeId, notificationDue, ct))
            .RequirePagePermission("stores.material-issues", PagePermissionActions.View);
        return endpoints;
    }

    private static Task<IResult> Run<T>(Func<Task<T>> action, HttpContext h) =>
        Run(action, h, value => Results.Ok(value));

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
