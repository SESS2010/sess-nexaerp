using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Api.Endpoints;

public static class StoresGateEntryEndpoints
{
    private const string Page="inventory.grn";
    public static IEndpointRouteBuilder MapStoresGateEntryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var g=endpoints.MapGroup("/api/v1/stores/gate-entries").WithTags("Stores - Gate Entries").RequireAuthorization().AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope);
        g.MapGet("/purchase-order-candidates",async(IGateEntryService s,HttpContext h,CancellationToken ct)=>await Run(()=>s.ListPurchaseOrderCandidatesAsync(ct),h)).RequirePagePermission(Page,PagePermissionActions.Create);
        g.MapPost("/",async(CreateGateEntryRequest r,IGateEntryService s,HttpContext h,CancellationToken ct)=>await Run(()=>s.CreateAsync(r,HeaderKey(h),ct),h)).RequirePagePermission(Page,PagePermissionActions.Create);
        g.MapPut("/{id:guid}",async(Guid id,UpdateGateEntryRequest r,IGateEntryService s,HttpContext h,CancellationToken ct)=>await Run(()=>s.UpdateAsync(id,r,ct),h)).RequirePagePermission(Page,PagePermissionActions.Update);
        g.MapPost("/{id:guid}/finalize",async(Guid id,FinalizeGateEntryRequest r,IGateEntryService s,HttpContext h,CancellationToken ct)=>await Run(()=>s.FinalizeAsync(id,r,ct),h)).RequirePagePermission(Page,PagePermissionActions.Submit);
        g.MapGet("/{id:guid}",async(Guid id,IGateEntryService s,HttpContext h,CancellationToken ct)=>await Run(async()=>await s.GetAsync(id,ct)??throw new KeyNotFoundException("Gate Entry was not found."),h)).RequirePagePermission(Page,PagePermissionActions.View);
        g.MapGet("/",async(string? gateEntryNumber,string? purchaseOrderNumber,Guid? vendorId,DateOnly? from,DateOnly? to,string? state,int? page,int? pageSize,IGateEntryService s,HttpContext h,CancellationToken ct)=>await Run(()=>s.ListAsync(gateEntryNumber,purchaseOrderNumber,vendorId,from,to,state,page??1,pageSize??50,ct),h)).RequirePagePermission(Page,PagePermissionActions.View);
        return endpoints;
    }
    private static string HeaderKey(HttpContext h)=>h.Request.Headers.TryGetValue("Idempotency-Key",out var v)&&!string.IsNullOrWhiteSpace(v)?v.ToString():throw new StoresValidationException("Idempotency-Key header is required.");
    private static async Task<IResult> Run<T>(Func<Task<T>> action,HttpContext h){try{return Results.Ok(await action());}catch(StoresValidationException e){return Results.BadRequest(new{message=e.Message});}catch(KeyNotFoundException e){return Results.NotFound(new{message=e.Message});}catch(UnauthorizedAccessException){return h.User.Identity?.IsAuthenticated==true?Results.Forbid():Results.Unauthorized();}catch(StoresConflictException e){return Results.Conflict(new{message=e.Message});}catch(DbUpdateConcurrencyException e){return Results.Conflict(new{message=e.Message});}}
}
