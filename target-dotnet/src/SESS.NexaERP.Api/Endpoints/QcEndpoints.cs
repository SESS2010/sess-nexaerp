using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Api.Endpoints;

public static class QcEndpoints
{
    private const string Page="qc.inspection-policies";
    public static IEndpointRouteBuilder MapQcEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var g=endpoints.MapGroup("/api/v1/qc").WithTags("Stores - QC and Concessions").RequireAuthorization().AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope);
        g.MapGet("/queue",async(int? page,int? pageSize,IQcWorkflowService s,HttpContext h,CancellationToken ct)=>await Run(()=>s.QueueAsync(page??1,pageSize??50,ct),h)).RequirePagePermission(Page,PagePermissionActions.View);
        g.MapPost("/inspections",async(FinalizeQcInspectionRequest r,IQcWorkflowService s,HttpContext h,CancellationToken ct)=>await Run(()=>s.FinalizeAsync(r,HeaderKey(h),ct),h)).RequirePagePermission(Page,PagePermissionActions.Create);
        g.MapPost("/inspections/{number}/corrections",async(string number,CorrectQcInspectionRequest r,IQcWorkflowService s,HttpContext h,CancellationToken ct)=>await Run(()=>s.CorrectAsync(number,r,HeaderKey(h),ct),h)).RequirePagePermission(Page,PagePermissionActions.Update);
        g.MapGet("/inspections/{number}",async(string number,IQcWorkflowService s,HttpContext h,CancellationToken ct)=>await Run(async()=>await s.GetAsync(number,ct)??throw new KeyNotFoundException("QC inspection was not found."),h)).RequirePagePermission(Page,PagePermissionActions.View);
        g.MapPost("/concessions",async(CreateInventoryConcessionRequest r,IQcWorkflowService s,HttpContext h,CancellationToken ct)=>await Run(()=>s.CreateConcessionAsync(r,HeaderKey(h),ct),h)).RequirePagePermission(Page,PagePermissionActions.Create);
        g.MapPost("/concessions/{number}/approve",async(string number,ApproveInventoryConcessionRequest r,IQcWorkflowService s,HttpContext h,CancellationToken ct)=>await Run(()=>s.ApproveConcessionAsync(number,r,HeaderKey(h),ct),h)).RequirePagePermission(Page,PagePermissionActions.Approve);
        g.MapPost("/concessions/{number}/reject",async(string number,RejectInventoryConcessionRequest r,IQcWorkflowService s,HttpContext h,CancellationToken ct)=>await Run(()=>s.RejectConcessionAsync(number,r,ct),h)).RequirePagePermission(Page,PagePermissionActions.Approve);
        g.MapPost("/concessions/{number}/reverse",async(string number,ReverseInventoryConcessionRequest r,IQcWorkflowService s,HttpContext h,CancellationToken ct)=>await Run(()=>s.ReverseConcessionAsync(number,r,HeaderKey(h),ct),h)).RequirePagePermission(Page,PagePermissionActions.Cancel);
        g.MapGet("/concessions/{number}",async(string number,IQcWorkflowService s,HttpContext h,CancellationToken ct)=>await Run(async()=>await s.GetConcessionAsync(number,ct)??throw new KeyNotFoundException("Inventory concession was not found."),h)).RequirePagePermission(Page,PagePermissionActions.View);
        return endpoints;
    }
    private static string HeaderKey(HttpContext h)=>h.Request.Headers.TryGetValue("Idempotency-Key",out var v)&&!string.IsNullOrWhiteSpace(v)?v.ToString():throw new StoresValidationException("Idempotency-Key header is required.");
    private static async Task<IResult> Run<T>(Func<Task<T>> action,HttpContext h){try{return Results.Ok(await action());}catch(StoresValidationException e){return Results.BadRequest(new{message=e.Message});}catch(KeyNotFoundException e){return Results.NotFound(new{message=e.Message});}catch(UnauthorizedAccessException){return h.User.Identity?.IsAuthenticated==true?Results.Forbid():Results.Unauthorized();}catch(StoresConflictException e){return Results.Conflict(new{message=e.Message});}catch(DbUpdateConcurrencyException e){return Results.Conflict(new{message=e.Message});}}
}
