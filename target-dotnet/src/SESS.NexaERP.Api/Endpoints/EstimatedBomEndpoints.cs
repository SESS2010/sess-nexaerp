using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Api.Endpoints;

public static class EstimatedBomEndpoints
{
    private const string Page = "design.estimated-bom";

    public static IEndpointRouteBuilder MapEstimatedBomEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/design/estimated-boms").WithTags("Estimated BOM").RequireAuthorization();
        group.MapGet("/", (IEstimatedBomService service, int? page, int? pageSize, string? search, string? status, CancellationToken ct) =>
            service.ListAsync(page, pageSize, search, status, ct)).RequirePagePermission(Page, PagePermissionActions.View);
        group.MapGet("/{bomNumber}", async (string bomNumber, IEstimatedBomService service, CancellationToken ct) =>
        {
            var result = await service.GetAsync(bomNumber, ct);
            return result is null ? Results.NotFound(new { message = "Estimated BOM was not found." }) : Results.Ok(result);
        }).RequirePagePermission(Page, PagePermissionActions.View);
        group.MapGet("/{bomNumber}/history", (string bomNumber, IEstimatedBomService service, CancellationToken ct) =>
            service.HistoryAsync(bomNumber, ct)).RequirePagePermission(Page, PagePermissionActions.ViewAuditHistory);
        group.MapGet("/workbook/template", async (IEstimatedBomService service, CancellationToken ct) =>
        {
            var file = await service.TemplateAsync(ct); return Results.File(file.Content, file.ContentType, file.FileName);
        }).RequirePagePermission(Page, PagePermissionActions.Download);
        group.MapPost("/", async (CreateEstimatedBomRequest request, IEstimatedBomService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(request, ct);
            return Results.Created($"/api/v1/design/estimated-boms/{result.BomNumber}", result);
        }).RequirePagePermission(Page, PagePermissionActions.Create);
        group.MapPut("/{bomNumber}", (string bomNumber, ReplaceEstimatedBomLinesRequest request, IEstimatedBomService service, CancellationToken ct) =>
            service.ReplaceDraftAsync(bomNumber, request, ct)).RequirePagePermission(Page, PagePermissionActions.Update);
        group.MapPost("/{bomNumber}/submit", (string bomNumber, EstimatedBomActionRequest request, IEstimatedBomService service, CancellationToken ct) =>
            service.SubmitAsync(bomNumber, request, ct)).RequirePagePermission(Page, PagePermissionActions.Submit);
        group.MapPost("/{bomNumber}/approve", (string bomNumber, EstimatedBomActionRequest request, IEstimatedBomService service, CancellationToken ct) =>
            service.ApproveAsync(bomNumber, request, ct)).RequirePagePermission(Page, PagePermissionActions.Approve);
        group.MapPost("/{bomNumber}/return-to-draft", (string bomNumber, EstimatedBomActionRequest request, IEstimatedBomService service, CancellationToken ct) =>
            service.ReturnToDraftAsync(bomNumber, request, ct)).RequirePagePermission(Page, PagePermissionActions.Reject);
        group.MapPost("/{bomNumber}/revisions", (string bomNumber, NewEstimatedBomRevisionRequest request, IEstimatedBomService service, CancellationToken ct) =>
            service.CreateRevisionAsync(bomNumber, request, ct)).RequirePagePermission(Page, PagePermissionActions.Create);
        group.MapPost("/workbook/import", async (HttpRequest request, IEstimatedBomService service, CancellationToken ct) =>
        {
            if (!request.HasFormContentType) return Results.BadRequest(new { message = "multipart/form-data with one workbook file is required." });
            var form = await request.ReadFormAsync(ct); var file = form.Files.GetFile("file");
            if (file is null || file.Length == 0) return Results.BadRequest(new { message = "Workbook file is required." });
            await using var stream = new MemoryStream(); await file.CopyToAsync(stream, ct);
            var key = request.Headers["Idempotency-Key"].ToString();
            var result = await service.ImportAsync(stream.ToArray(), key, ct);
            return Results.Created($"/api/v1/design/estimated-boms/{result.BomNumber}", result);
        }).DisableAntiforgery().RequirePagePermission(Page, PagePermissionActions.Create);
        return endpoints;
    }
}
