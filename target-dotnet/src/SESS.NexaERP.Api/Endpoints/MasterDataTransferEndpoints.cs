using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Masters;
using Microsoft.Extensions.Options;
using SESS.NexaERP.Infrastructure.MasterData;

namespace SESS.NexaERP.Api.Endpoints;

public static class MasterDataTransferEndpoints
{
    public static IEndpointRouteBuilder MapMasterDataTransferEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/master-data")
            .WithTags("Master Data Transfer")
            .RequireAuthorization()
            .AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope);

        group.MapGet("/{masterKey}/template", async (
            string masterKey,
            IMasterDataTransferService service,
            IMasterDataRegistry registry,
            IPagePermissionService permissions,
            ICurrentUser user,
            IAuditWriter audit,
            HttpContext http,
            CancellationToken ct) =>
        {
            var denied = await RequireAsync(masterKey, [PagePermissionActions.Download], registry, permissions, user, audit, http, ct);
            if (denied is not null) return denied;
            return ToFile(await service.CreateTemplateAsync(masterKey, ct));
        });

        group.MapGet("/{masterKey}/export", async (
            string masterKey,
            string? search,
            bool? isActive,
            string? sortBy,
            string? sortDirection,
            IMasterDataTransferService service,
            IMasterDataRegistry registry,
            IPagePermissionService permissions,
            ICurrentUser user,
            IAuditWriter audit,
            HttpContext http,
            CancellationToken ct) =>
        {
            var denied = await RequireAsync(masterKey, [PagePermissionActions.Export], registry, permissions, user, audit, http, ct);
            if (denied is not null) return denied;
            try { return ToFile(await service.ExportAsync(masterKey, new(search, isActive, sortBy, sortDirection), ct)); }
            catch (MasterDataValidationException ex) { return Results.BadRequest(new { message = ex.Message }); }
        });

        group.MapPost("/{masterKey}/import", async (
            string masterKey,
            IMasterDataTransferService service,
            IMasterDataRegistry registry,
            IPagePermissionService permissions,
            ICurrentUser user,
            IAuditWriter audit,
            IOptions<MasterDataTransferOptions> configuredOptions,
            HttpContext http,
            CancellationToken ct) =>
        {
            var denied = await RequireAsync(masterKey, [PagePermissionActions.Create, PagePermissionActions.Update], registry, permissions, user, audit, http, ct);
            if (denied is not null) return denied;
            try
            {
                if (!http.Request.HasFormContentType)
                    return Results.BadRequest(new { message = "Import requires multipart/form-data." });
                var form = await http.Request.ReadFormAsync(ct);
                var file = form.Files.GetFile("File") ?? form.Files.GetFile("file");
                var mode = form["Mode"].FirstOrDefault() ?? form["mode"].FirstOrDefault();
                var idempotencyKey = form["IdempotencyKey"].FirstOrDefault() ?? form["idempotencyKey"].FirstOrDefault();
                if (file is null) return Results.BadRequest(new { message = "File is required." });
                if (file.Length > configuredOptions.Value.MaxFileBytes)
                    return Results.BadRequest(new { message = $"File exceeds the configured maximum of {configuredOptions.Value.MaxFileBytes} bytes." });
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream, ct);
                var correlationId = Guid.TryParse(http.Request.Headers["X-Correlation-ID"].FirstOrDefault(), out var supplied)
                    ? supplied : Guid.NewGuid();
                var result = await service.ImportAsync(new(
                    masterKey,
                    mode ?? string.Empty,
                    idempotencyKey ?? string.Empty,
                    file.FileName,
                    stream.ToArray(),
                    correlationId), ct);
                return Results.Ok(result);
            }
            catch (MasterDataValidationException ex) { return Results.BadRequest(new { message = ex.Message }); }
            catch (MasterDataConflictException ex) { return Results.Conflict(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        });

        group.MapGet("/imports/{batchId:guid}", async (Guid batchId, IMasterDataTransferService service, CancellationToken ct) =>
        {
            try
            {
                var result = await service.GetImportAsync(batchId, ct);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        });

        group.MapGet("/imports/{batchId:guid}/rows", async (
            Guid batchId,
            int? page,
            int? pageSize,
            IMasterDataTransferService service,
            CancellationToken ct) =>
        {
            try
            {
                var result = await service.GetImportRowsAsync(batchId, page ?? 1, pageSize ?? 100, ct);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        });

        group.MapGet("/imports/{batchId:guid}/errors.xlsx", async (
            Guid batchId,
            IMasterDataTransferService service,
            CancellationToken ct) =>
        {
            try
            {
                var result = await service.CreateErrorWorkbookAsync(batchId, ct);
                return result is null ? Results.NotFound() : ToFile(result);
            }
            catch (MasterDataNotFoundException ex) { return Results.NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        });
        return endpoints;
    }

    private static async Task<IResult?> RequireAsync(
        string masterKey,
        IReadOnlyList<string> required,
        IMasterDataRegistry registry,
        IPagePermissionService permissions,
        ICurrentUser user,
        IAuditWriter audit,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        if (!registry.TryGet(masterKey, out var adapter))
            return Results.NotFound(new { message = $"Master data definition '{masterKey}' is not enabled." });
        foreach (var permission in required)
        {
            if (await permissions.HasPermissionAsync(user.RoleCodes, adapter!.Definition.PageKey, permission, cancellationToken)) continue;
            await audit.WriteAsync("Security", "Denied", adapter.Definition.PageKey, permission, null,
                new { user.EmployeeId, user.RoleCodes, masterKey, path = http.Request.Path.Value, method = http.Request.Method },
                cancellationToken);
            return Results.Forbid();
        }
        return null;
    }

    private static IResult ToFile(MasterDataFileResult file) =>
        Results.File(file.Content, file.ContentType, file.FileName);
}
