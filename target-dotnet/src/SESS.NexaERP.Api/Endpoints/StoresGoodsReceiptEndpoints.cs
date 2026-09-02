using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Api.Endpoints;

public static class StoresGoodsReceiptEndpoints
{
    private const string Page="inventory.grn";
    public static IEndpointRouteBuilder MapStoresGoodsReceiptEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var g=endpoints.MapGroup("/api/v1/stores/goods-receipts").WithTags("Stores - Goods Receipts").RequireAuthorization().AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope);
        g.MapPost("/",async(CreateGoodsReceiptRequest r,IGoodsReceiptService s,HttpContext h,CancellationToken ct)=>await Run(()=>s.CreateAsync(r,HeaderKey(h),ct),h)).RequirePagePermission(Page,PagePermissionActions.Create);
        g.MapPut("/{id:guid}",async(Guid id,UpdateGoodsReceiptRequest r,IGoodsReceiptService s,HttpContext h,CancellationToken ct)=>await Run(()=>s.UpdateAsync(id,r,ct),h)).RequirePagePermission(Page,PagePermissionActions.Update);
        g.MapPost("/{id:guid}/finalize",async(Guid id,FinalizeGoodsReceiptRequest r,IGoodsReceiptService s,HttpContext h,CancellationToken ct)=>await Run(()=>s.FinalizeAsync(id,r,ct),h)).RequirePagePermission(Page,PagePermissionActions.Submit);
        g.MapPost("/{id:guid}/reverse",async(Guid id,ReverseGoodsReceiptRequest r,IGoodsReceiptService s,HttpContext h,CancellationToken ct)=>await Run(()=>s.ReverseAsync(id,r,ct),h)).RequirePagePermission(Page,PagePermissionActions.Cancel);
        g.MapGet("/{id:guid}",async(Guid id,IGoodsReceiptService s,HttpContext h,CancellationToken ct)=>await Run(async()=>await s.GetAsync(id,ct)??throw new KeyNotFoundException("GRN was not found."),h)).RequirePagePermission(Page,PagePermissionActions.View);
        g.MapGet("/",async(string? goodsReceiptNumber,string? grnNumber,string? gateEntryNumber,Guid? vendorId,string? status,int? page,int? pageSize,IGoodsReceiptService s,HttpContext h,CancellationToken ct)=>await Run(()=>s.ListAsync(goodsReceiptNumber??grnNumber,gateEntryNumber,vendorId,status,page??1,pageSize??50,ct),h)).RequirePagePermission(Page,PagePermissionActions.View);
        return endpoints;
    }
    private static string HeaderKey(HttpContext h)=>h.Request.Headers.TryGetValue("Idempotency-Key",out var v)&&!string.IsNullOrWhiteSpace(v)?v.ToString():throw new StoresValidationException("Idempotency-Key header is required.");
    private static async Task<IResult> Run<T>(Func<Task<T>> action,HttpContext h){try{return Results.Ok(await action());}catch(StoresValidationException e){return Results.BadRequest(new{message=e.Message});}catch(KeyNotFoundException e){return Results.NotFound(new{message=e.Message});}catch(UnauthorizedAccessException){return h.User.Identity?.IsAuthenticated==true?Results.Forbid():Results.Unauthorized();}catch(StoresConflictException e){return Results.Conflict(new{message=e.Message});}catch(DbUpdateConcurrencyException e){return Results.Conflict(new{message=e.Message});}}
}
