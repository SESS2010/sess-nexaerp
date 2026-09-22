using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Api.Endpoints;

public static class ProductionEngineeringEndpoints
{
    public static IEndpointRouteBuilder MapProductionEngineeringEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var boms = endpoints.MapGroup("/api/v1/production/boms")
            .WithTags("Production BOM").RequireAuthorization();
        boms.MapGet("/", (IProductionEngineeringService s, CancellationToken ct) =>
            s.ListProductionBomsAsync(ct)).RequirePagePermission("production.production-bom", PagePermissionActions.View);
        boms.MapGet("/{number}", async (string number, IProductionEngineeringService s, CancellationToken ct) =>
            await s.GetProductionBomAsync(number, ct) is { } value ? Results.Ok(value) : Results.NotFound())
            .RequirePagePermission("production.production-bom", PagePermissionActions.View);
        boms.MapPost("/", async (CreateProductionBomRequest r, IProductionEngineeringService s, CancellationToken ct) =>
            Results.Created("", await s.CreateProductionBomAsync(r, ct)))
            .RequirePagePermission("production.production-bom", PagePermissionActions.Create);
        boms.MapPut("/{number}", (string number, ReplaceProductionBomRequest r, IProductionEngineeringService s, CancellationToken ct) =>
            s.ReplaceProductionBomAsync(number, r, ct)).RequirePagePermission("production.production-bom", PagePermissionActions.Update);
        boms.MapPost("/{number}/submit", (string number, ProductionBomActionRequest r, IProductionEngineeringService s, CancellationToken ct) =>
            s.SubmitProductionBomAsync(number, r, ct)).RequirePagePermission("production.production-bom", PagePermissionActions.Submit);
        boms.MapPost("/{number}/return-to-draft", (string number, ProductionBomActionRequest r, IProductionEngineeringService s, CancellationToken ct) =>
            s.ReturnProductionBomToDraftAsync(number, r, ct)).RequirePagePermission("production.production-bom", PagePermissionActions.Reject);
        boms.MapPost("/{number}/approve", (string number, ProductionBomActionRequest r, IProductionEngineeringService s, CancellationToken ct) =>
            s.ApproveProductionBomAsync(number, r, ct)).RequirePagePermission("production.production-bom", PagePermissionActions.Approve);
        boms.MapPost("/{number}/revisions", (string number, NewProductionBomRevisionRequest r, IProductionEngineeringService s, CancellationToken ct) =>
            s.CreateProductionBomRevisionAsync(number, r, ct)).RequirePagePermission("production.production-bom", PagePermissionActions.Create);
        boms.MapPost("/{number}/pin", (string number, PinProductionBomRevisionRequest r, IProductionEngineeringService s, CancellationToken ct) =>
            s.PinProductionBomRevisionAsync(number, r, ct)).RequirePagePermission("production.production-bom", PagePermissionActions.Update);

        var docs = endpoints.MapGroup("/api/v1/design/documents")
            .WithTags("Engineering Documents").RequireAuthorization();
        docs.MapGet("/", (Guid? jobOrderId, string? type, IProductionEngineeringService s, CancellationToken ct) =>
            s.ListEngineeringDocumentsAsync(jobOrderId, type, ct)).RequirePagePermission("design.engineering-documents", PagePermissionActions.View);
        docs.MapGet("/{number}", async (string number, IProductionEngineeringService s, CancellationToken ct) =>
            await s.GetEngineeringDocumentAsync(number, ct) is { } value ? Results.Ok(value) : Results.NotFound())
            .RequirePagePermission("design.engineering-documents", PagePermissionActions.View);
        docs.MapPost("/", async (CreateEngineeringDocumentRequest r, IProductionEngineeringService s, CancellationToken ct) =>
            Results.Created("", await s.CreateEngineeringDocumentAsync(r, ct)))
            .RequirePagePermission("design.engineering-documents", PagePermissionActions.Create);
        docs.MapPost("/{number}/revisions", (string number, NewEngineeringDocumentRevisionRequest r, IProductionEngineeringService s, CancellationToken ct) =>
            s.CreateEngineeringDocumentRevisionAsync(number, r, ct)).RequirePagePermission("design.engineering-documents", PagePermissionActions.Create);
        docs.MapPost("/{number}/submit", (string number, EngineeringDocumentActionRequest r, IProductionEngineeringService s, CancellationToken ct) =>
            s.SubmitEngineeringDocumentAsync(number, r, ct)).RequirePagePermission("design.engineering-documents", PagePermissionActions.Submit);
        docs.MapPost("/{number}/return-to-draft", (string number, EngineeringDocumentActionRequest r, IProductionEngineeringService s, CancellationToken ct) =>
            s.ReturnEngineeringDocumentToDraftAsync(number, r, ct)).RequirePagePermission("design.engineering-documents", PagePermissionActions.Reject);
        docs.MapPost("/{number}/approve", (string number, EngineeringDocumentActionRequest r, IProductionEngineeringService s, CancellationToken ct) =>
            s.ApproveEngineeringDocumentAsync(number, r, ct)).RequirePagePermission("design.engineering-documents", PagePermissionActions.Approve);
        return endpoints;
    }
}
