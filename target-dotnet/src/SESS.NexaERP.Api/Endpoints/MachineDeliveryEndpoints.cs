using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Stores;
namespace SESS.NexaERP.Api.Endpoints;
public static class MachineDeliveryEndpoints
{
 public static IEndpointRouteBuilder MapMachineDeliveryEndpoints(this IEndpointRouteBuilder endpoints)
 {
  var group=endpoints.MapGroup("/api/v1/stores/machine-deliveries").WithTags("Machine delivery challans").RequireAuthorization()
   .AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope);
  group.MapGet("/job-orders",(int? page,int? pageSize,string? search,IMachineDeliveryService service,CancellationToken ct)=>
    service.JobOrdersAsync(page,pageSize,search,ct))
   .RequirePagePermission("stores.machine-deliveries",PagePermissionActions.Issue);
  group.MapPost("/",(DispatchMachineRequest request,IMachineDeliveryService service,CancellationToken ct)=>service.DispatchAsync(request,ct))
   .RequirePagePermission("stores.machine-deliveries",PagePermissionActions.Issue);
  group.MapPost("/{id:guid}/signature",(Guid id,SignMachineDeliveryRequest request,IMachineDeliveryService service,CancellationToken ct)=>service.SignAsync(id,request,ct))
   .RequirePagePermission("stores.machine-deliveries",PagePermissionActions.Issue);
  group.MapGet("/{id:guid}",async(Guid id,IMachineDeliveryService service,CancellationToken ct)=>
    await service.GetAsync(id,ct) is {} value ? Results.Ok(value):Results.NotFound())
   .RequirePagePermission("stores.machine-deliveries",PagePermissionActions.View);
  group.MapGet("/{id:guid}/signature-evidence",async(Guid id,IMachineDeliveryService service,CancellationToken ct)=>
    await service.SignatureAsync(id,ct) is {} file ? Results.File(file.Content,file.ContentType,file.FileName):Results.NotFound())
   .RequirePagePermission("reports.machine-dossier",PagePermissionActions.View);
  return endpoints;
 }
}
