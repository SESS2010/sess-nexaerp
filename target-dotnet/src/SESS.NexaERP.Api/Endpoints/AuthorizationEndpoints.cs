using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public static class AuthorizationEndpoints
{
    public static IEndpointRouteBuilder MapAuthorizationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/authorization")
            .WithTags("Authorization")
            .RequireAuthorization();

        group.MapGet("/pages", async (NexaErpDbContext db, CancellationToken cancellationToken) =>
        {
            var pages = await db.PageDefinitions
                .AsNoTracking()
                .OrderBy(page => page.Module)
                .ThenBy(page => page.PageKey)
                .Select(page => new PageDefinitionSummary(page.Id, page.PageKey, page.Module, page.Title, page.Route, page.IsActive))
                .ToListAsync(cancellationToken);

            return Results.Ok(pages);
        }).RequirePagePermission("authorization.pages", PagePermissionActions.View);

        group.MapPost("/pages", async (CreatePageDefinitionRequest request, NexaErpDbContext db, IAuditWriter audit, CancellationToken cancellationToken) =>
        {
            var pageKey = NormalizeKey(request.PageKey);
            if (string.IsNullOrWhiteSpace(pageKey) || string.IsNullOrWhiteSpace(request.Module) || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Route))
            {
                return Results.BadRequest(new { message = "Page key, module, title and route are required." });
            }

            if (await db.PageDefinitions.AnyAsync(page => page.PageKey == pageKey, cancellationToken))
            {
                return Results.Conflict(new { message = $"Duplicate page key blocked: {pageKey}" });
            }

            var pageDefinition = new PageDefinition
            {
                PageKey = pageKey,
                Module = request.Module.Trim(),
                Title = request.Title.Trim(),
                Route = request.Route.Trim()
            };

            db.PageDefinitions.Add(pageDefinition);
            await db.SaveChangesAsync(cancellationToken);
            await audit.WriteAsync("Authorization", "Create", nameof(PageDefinition), pageDefinition.Id.ToString(), null, pageDefinition, cancellationToken);

            return Results.Created($"/api/v1/authorization/pages/{pageDefinition.Id}", new PageDefinitionSummary(pageDefinition.Id, pageDefinition.PageKey, pageDefinition.Module, pageDefinition.Title, pageDefinition.Route, pageDefinition.IsActive));
        }).RequirePagePermission("authorization.pages", PagePermissionActions.Create);

        group.MapGet("/role-page-permissions", async (NexaErpDbContext db, string? roleCode, CancellationToken cancellationToken) =>
        {
            var normalizedRole = string.IsNullOrWhiteSpace(roleCode) ? null : roleCode.Trim().ToUpperInvariant();
            var query = db.RolePagePermissions
                .AsNoTracking()
                .Include(permission => permission.Role)
                .Include(permission => permission.PageDefinition)
                .AsQueryable();

            if (normalizedRole is not null)
            {
                query = query.Where(permission => permission.Role != null && permission.Role.Code == normalizedRole);
            }

            var permissions = await query
                .OrderBy(permission => permission.Role!.Code)
                .ThenBy(permission => permission.PageDefinition!.PageKey)
                .Select(permission => ToSummary(permission, permission.Role == null ? string.Empty : permission.Role.Code, permission.PageDefinition == null ? string.Empty : permission.PageDefinition.PageKey))
                .ToListAsync(cancellationToken);

            return Results.Ok(permissions);
        }).RequirePagePermission("authorization.role-pages", PagePermissionActions.View);

        group.MapPut("/role-page-permissions", async (UpsertRolePagePermissionRequest request, NexaErpDbContext db, IAuditWriter audit, CancellationToken cancellationToken) =>
        {
            var roleCode = request.RoleCode.Trim().ToUpperInvariant();
            var pageKey = NormalizeKey(request.PageKey);
            var role = await db.Roles.SingleOrDefaultAsync(existing => existing.Code == roleCode && existing.IsActive, cancellationToken);
            var page = await db.PageDefinitions.SingleOrDefaultAsync(existing => existing.PageKey == pageKey && existing.IsActive, cancellationToken);

            if (role is null || page is null)
            {
                return Results.BadRequest(new { message = "Active role and active page are required." });
            }

            var permission = await db.RolePagePermissions.SingleOrDefaultAsync(existing => existing.RoleId == role.Id && existing.PageDefinitionId == page.Id, cancellationToken);
            object? before = permission is null ? null : Snapshot(permission);

            if (permission is null)
            {
                permission = new RolePagePermission
                {
                    RoleId = role.Id,
                    PageDefinitionId = page.Id
                };
                db.RolePagePermissions.Add(permission);
            }

            Apply(permission, request);
            await db.SaveChangesAsync(cancellationToken);
            await audit.WriteAsync("Authorization", "Upsert", nameof(RolePagePermission), permission.Id.ToString(), before, permission, cancellationToken);

            return Results.Ok(ToSummary(permission, role.Code, page.PageKey));
        }).RequirePagePermission("authorization.role-pages", PagePermissionActions.Update);

        return endpoints;
    }

    private static RolePagePermissionSummary ToSummary(RolePagePermission permission, string roleCode, string pageKey) => new(
        permission.Id,
        roleCode,
        pageKey,
        permission.CanView,
        permission.CanCreate,
        permission.CanUpdate,
        permission.CanSubmit,
        permission.CanVerify,
        permission.CanApprove,
        permission.CanReject,
        permission.CanRequestClarification,
        permission.CanRequestRevision,
        permission.CanResubmit,
        permission.CanCancel,
        permission.CanDeactivate,
        permission.CanPrint,
        permission.CanDownload,
        permission.CanExport,
        permission.CanUploadAttachment,
        permission.CanReplaceAttachment,
        permission.CanViewCommercialValues,
        permission.CanViewAuditHistory,
        permission.HasFullControl);

    private static object Snapshot(RolePagePermission permission) => new
    {
        permission.CanView,
        permission.CanCreate,
        permission.CanUpdate,
        permission.CanSubmit,
        permission.CanVerify,
        permission.CanApprove,
        permission.CanReject,
        permission.CanRequestClarification,
        permission.CanRequestRevision,
        permission.CanResubmit,
        permission.CanCancel,
        permission.CanDeactivate,
        permission.CanPrint,
        permission.CanDownload,
        permission.CanExport,
        permission.CanUploadAttachment,
        permission.CanReplaceAttachment,
        permission.CanViewCommercialValues,
        permission.CanViewAuditHistory,
        permission.HasFullControl
    };

    private static void Apply(RolePagePermission permission, UpsertRolePagePermissionRequest request)
    {
        permission.CanView = request.CanView;
        permission.CanCreate = request.CanCreate;
        permission.CanUpdate = request.CanUpdate;
        permission.CanSubmit = request.CanSubmit;
        permission.CanVerify = request.CanVerify;
        permission.CanApprove = request.CanApprove;
        permission.CanReject = request.CanReject;
        permission.CanRequestClarification = request.CanRequestClarification;
        permission.CanRequestRevision = request.CanRequestRevision;
        permission.CanResubmit = request.CanResubmit;
        permission.CanCancel = request.CanCancel;
        permission.CanDeactivate = request.CanDeactivate;
        permission.CanPrint = request.CanPrint;
        permission.CanDownload = request.CanDownload;
        permission.CanExport = request.CanExport;
        permission.CanUploadAttachment = request.CanUploadAttachment;
        permission.CanReplaceAttachment = request.CanReplaceAttachment;
        permission.CanViewCommercialValues = request.CanViewCommercialValues;
        permission.CanViewAuditHistory = request.CanViewAuditHistory;
        permission.HasFullControl = request.HasFullControl;
    }

    private static string NormalizeKey(string value) => value.Trim().ToLowerInvariant();
}
