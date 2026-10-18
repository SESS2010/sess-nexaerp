using Microsoft.EntityFrameworkCore;
using Npgsql;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Api.Endpoints;

public static class EmployeeMirPortalEndpoints
{
    public static IEndpointRouteBuilder MapEmployeeMirPortalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group=endpoints.MapGroup("/api/v1/employee/my-mirs").WithTags("Employee - My MIRs").RequireAuthorization()
            .AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope);
        group.MapGet("/",(string? number,string? status,int? page,int? pageSize,IEmployeeMirPortal service,HttpContext h,CancellationToken ct)=>
            Run(()=>service.ListAsync(number,status,page??1,pageSize??50,ct),h)).RequirePagePermission(MirRequesterPolicy.Page,PagePermissionActions.View);
        group.MapGet("/references",(IEmployeeMirPortal service,HttpContext h,CancellationToken ct)=>
            Run(()=>service.ReferencesAsync(ct),h)).RequirePagePermission(MirRequesterPolicy.Page,PagePermissionActions.View);
        group.MapGet("/spare-po-lines",(Guid customerId,IEmployeeMirPortal service,HttpContext h,CancellationToken ct)=>
            Run(()=>service.SpareLinesAsync(customerId,ct),h)).RequirePagePermission(MirRequesterPolicy.Page,PagePermissionActions.View);
        group.MapGet("/items",(string? query,bool? scan,IEmployeeMirPortal service,HttpContext h,CancellationToken ct)=>
            Run(()=>service.SearchItemsAsync(query,scan??false,ct),h)).RequirePagePermission(MirRequesterPolicy.Page,PagePermissionActions.View);
        group.MapGet("/{id:guid}",(Guid id,IEmployeeMirPortal service,HttpContext h,CancellationToken ct)=>
            Run(()=>service.GetAsync(id,ct),h)).RequirePagePermission(MirRequesterPolicy.Page,PagePermissionActions.View);
        group.MapPost("/",(EmployeeMirCreate request,IEmployeeMirPortal service,HttpContext h,CancellationToken ct)=>
            Run(()=>service.CreateAsync(request,ct),h,true)).RequirePagePermission(MirRequesterPolicy.Page,PagePermissionActions.Create);
        group.MapPut("/{id:guid}",(Guid id,EmployeeMirUpdate request,IEmployeeMirPortal service,HttpContext h,CancellationToken ct)=>
            Run(()=>service.UpdateAsync(id,request,ct),h)).RequirePagePermission(MirRequesterPolicy.Page,PagePermissionActions.Update);
        group.MapPost("/{id:guid}/submit",(Guid id,MaterialIssueTransitionRequest request,IEmployeeMirPortal service,HttpContext h,CancellationToken ct)=>
            Run(()=>service.SubmitAsync(id,request,ct),h)).RequirePagePermission(MirRequesterPolicy.Page,PagePermissionActions.Submit);
        group.MapGet("/custody",(IEmployeeMirPortal service,HttpContext h,CancellationToken ct)=>
            Run(()=>service.CustodyAsync(ct),h)).RequirePagePermission(MirRequesterPolicy.Page,PagePermissionActions.View);
        group.MapGet("/issues/{id:guid}",(Guid id,IEmployeeMirPortal service,HttpContext h,CancellationToken ct)=>
            Run(()=>service.GetIssueAsync(id,ct),h)).RequirePagePermission(MirRequesterPolicy.Page,PagePermissionActions.View);
        group.MapGet("/returns",(int? page,int? pageSize,IEmployeeMirPortal service,HttpContext h,CancellationToken ct)=>
            Run(()=>service.ReturnsAsync(page??1,pageSize??50,ct),h)).RequirePagePermission(MirRequesterPolicy.Page,PagePermissionActions.View);
        group.MapGet("/returns/{id:guid}",(Guid id,IEmployeeMirPortal service,HttpContext h,CancellationToken ct)=>
            Run(()=>service.GetReturnAsync(id,ct),h)).RequirePagePermission(MirRequesterPolicy.Page,PagePermissionActions.View);
        group.MapPost("/issues/{id:guid}/returns",(Guid id,CreateMaterialReturn request,IEmployeeMirPortal service,HttpContext h,CancellationToken ct)=>
            Run(()=>service.DeclareReturnAsync(id,request,ct),h,true)).RequirePagePermission(MirRequesterPolicy.Page,PagePermissionActions.Create);
        return endpoints;
    }
    private static async Task<IResult> Run<T>(Func<Task<T>> action,HttpContext h,bool created=false)
    {
        try
        {
            var value=await action();
            return value is null ? Results.NotFound() : created ? Results.Created(string.Empty,value) : Results.Ok(value);
        }
        catch(StoresValidationException e){return Results.BadRequest(new {message=e.Message});}
        catch(KeyNotFoundException){return Results.NotFound();}
        catch(UnauthorizedAccessException){return h.User.Identity?.IsAuthenticated==true?Results.Forbid():Results.Unauthorized();}
        catch(PostgresException e) when(e.SqlState=="42501"){return Results.Forbid();}
        catch(DbUpdateException e) when(e.InnerException is PostgresException { SqlState: "42501" }){return Results.Forbid();}
        catch(StoresConflictException e){return Results.Conflict(new {message=e.Message});}
        catch(DbUpdateConcurrencyException e){return Results.Conflict(new {message=e.Message});}
    }
}
