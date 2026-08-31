using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public static partial class MasterEndpointHelpers
{
    public const int MaxPageSize = 100;

    public static (int PageNumber, int PageSize, int Skip) NormalizePaging(int? page, int? pageSize)
    {
        var safePage = Math.Max(page ?? 1, 1);
        var safeSize = Math.Clamp(pageSize ?? 25, 1, MaxPageSize);
        return (safePage, safeSize, (safePage - 1) * safeSize);
    }

    public static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();

    public static string NormalizeRequired(string? value) => (value ?? string.Empty).Trim();

    public static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    public static string? NormalizeUpperOptional(string? value)
    {
        var trimmed = value?.Trim().ToUpperInvariant();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    public static bool IsValidGstin(string? gstin)
    {
        return string.IsNullOrWhiteSpace(gstin) || GstinRegex().IsMatch(gstin.Trim().ToUpperInvariant());
    }

    public static bool IsValidPan(string? pan)
    {
        return string.IsNullOrWhiteSpace(pan) || PanRegex().IsMatch(pan.Trim().ToUpperInvariant());
    }

    public static IResult Problem(string message, int statusCode = StatusCodes.Status400BadRequest) => Results.Problem(message, statusCode: statusCode);

    public static IResult RequireRemarks(string remarks)
    {
        return string.IsNullOrWhiteSpace(remarks)
            ? Results.BadRequest(new { message = "Remarks/reason are required for this action." })
            : Results.Ok();
    }

    public static bool IsMismatch(uint expected, uint actual) => expected != actual;

    public static async Task<bool> CanViewCommercialAsync(IPagePermissionService permissionService, ICurrentUser currentUser, string pageKey, CancellationToken cancellationToken)
    {
        return await permissionService.HasPermissionAsync(currentUser.RoleCodes, pageKey, PagePermissionActions.ViewCommercialValues, cancellationToken);
    }

    public static async Task AddStatusHistoryAsync(NexaErpDbContext db, string masterType, Guid masterId, string masterCode, string? previous, string next, string reason, ICurrentUser currentUser, string correlationId, CancellationToken cancellationToken)
    {
        db.MasterStatusHistories.Add(new MasterStatusHistory
        {
            MasterType = masterType,
            MasterId = masterId,
            MasterCode = masterCode,
            PreviousStatus = previous,
            NewStatus = next,
            Reason = reason,
            SourceRevision = "REV867",
            CorrelationId = correlationId,
            CreatedBy = currentUser.LoginId
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public static void AddApprovalHistory(NexaErpDbContext db, string masterType, Guid masterId, string masterCode, string action, string fromStatus, string toStatus, string remarks, ICurrentUser currentUser, string correlationId)
    {
        db.MasterApprovalHistories.Add(new MasterApprovalHistory
        {
            MasterType = masterType,
            MasterId = masterId,
            MasterCode = masterCode,
            Action = action,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            Remarks = remarks,
            ActorLoginId = currentUser.LoginId,
            ActorRoleCode = currentUser.RoleCode,
            CorrelationId = correlationId,
            CreatedBy = currentUser.LoginId
        });
    }

    public static async Task<IResult> GetStatusHistoryAsync(NexaErpDbContext db, string masterType, string code, CancellationToken cancellationToken)
    {
        var rows = await db.MasterStatusHistories.AsNoTracking()
            .Where(row => row.MasterType == masterType && row.MasterCode == code)
            .OrderByDescending(row => row.CreatedAt)
            .Select(row => new MasterStatusHistorySummary(row.Id, row.PreviousStatus, row.NewStatus, row.Reason, row.CreatedAt, row.CorrelationId))
            .ToListAsync(cancellationToken);
        return Results.Ok(rows);
    }

    public static async Task<IResult> GetStatusHistoryByIdAsync(NexaErpDbContext db,string masterType,Guid id,CancellationToken cancellationToken)
    {
        var rows=await db.MasterStatusHistories.AsNoTracking().Where(x=>x.MasterType==masterType&&x.MasterId==id).OrderByDescending(x=>x.CreatedAt).Select(x=>new MasterStatusHistorySummary(x.Id,x.PreviousStatus,x.NewStatus,x.Reason,x.CreatedAt,x.CorrelationId)).ToListAsync(cancellationToken);return Results.Ok(rows);
    }

    public static async Task<IResult> GetApprovalHistoryAsync(NexaErpDbContext db, string masterType, string code, CancellationToken cancellationToken)
    {
        var rows = await db.MasterApprovalHistories.AsNoTracking()
            .Where(row => row.MasterType == masterType && row.MasterCode == code)
            .OrderByDescending(row => row.CreatedAt)
            .Select(row => new MasterHistorySummary(row.Id, row.Action, row.FromStatus, row.ToStatus, row.Remarks, row.ActorLoginId, row.ActorRoleCode, row.CreatedAt, row.CorrelationId))
            .ToListAsync(cancellationToken);
        return Results.Ok(rows);
    }

    public static async Task<IResult> GetApprovalHistoryByIdAsync(NexaErpDbContext db,string masterType,Guid id,CancellationToken cancellationToken)
    {
        var rows=await db.MasterApprovalHistories.AsNoTracking().Where(x=>x.MasterType==masterType&&x.MasterId==id).OrderByDescending(x=>x.CreatedAt).Select(x=>new MasterHistorySummary(x.Id,x.Action,x.FromStatus,x.ToStatus,x.Remarks,x.ActorLoginId,x.ActorRoleCode,x.CreatedAt,x.CorrelationId)).ToListAsync(cancellationToken);return Results.Ok(rows);
    }

    public static async Task<IResult> GetAuditHistoryAsync(NexaErpDbContext db, string entityName, string entityId, CancellationToken cancellationToken, bool redactVendorBankMetadata = false)
    {
        var rows = await db.AuditLogs.AsNoTracking()
            .Where(row => row.EntityName == entityName && row.EntityId == entityId)
            .OrderByDescending(row => row.CreatedAt)
            .Take(200)
            .Select(row => new MasterAuditHistorySummary(row.Id, row.Module, row.Action, row.UserLoginId, row.Result, row.CorrelationId, row.BeforeJson, row.AfterJson, row.CreatedAt))
            .ToListAsync(cancellationToken);
        if (redactVendorBankMetadata)
        {
            rows = rows.Select(row => row with { BeforeJson = RedactVendorBankMetadata(row.BeforeJson), AfterJson = RedactVendorBankMetadata(row.AfterJson) }).ToList();
        }
        return Results.Ok(rows);
    }

    public static string? RedactVendorBankMetadata(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return json;
        try
        {
            var node = JsonNode.Parse(json);
            RedactVendorBankMetadata(node);
            return node?.ToJsonString(new JsonSerializerOptions { PropertyNamingPolicy = null });
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void RedactVendorBankMetadata(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            foreach (var property in obj.ToList())
            {
                if (property.Key.Equals("BankMetadata", StringComparison.OrdinalIgnoreCase) || property.Key.Equals("BankMetadataJson", StringComparison.OrdinalIgnoreCase)) obj[property.Key] = null;
                else RedactVendorBankMetadata(property.Value);
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var child in array) RedactVendorBankMetadata(child);
        }
    }

    public static async Task<IResult> ChangeLifecycleAsync<T>(NexaErpDbContext db, IAuditWriter audit, ICurrentUser currentUser, T entity, string masterType, string code, string action, string nextStatus, string nextApprovalStatus, string remarks, uint version, Action<T, string, string> setStatus, Func<T, string> getStatus, Func<T, string> getApproval, Action<T, string> setApproval, CancellationToken cancellationToken)
        where T : AuditableEntity
    {
        if (string.IsNullOrWhiteSpace(remarks)) return Results.BadRequest(new { message = "Remarks/reason are required for this action." });
        if (IsMismatch(version, entity.Version)) return Results.Conflict(new { message = "Stale record version. Refresh and retry." });
        var before = new { Status = getStatus(entity), ApprovalStatus = getApproval(entity), entity.Version };
        if (action == "Approve" && IsSelfApprovalAttempt(entity, currentUser))
        {
            await audit.WriteAsync("Security", "Denied", masterType, entity.Id.ToString(), before, new { reason = "Self-approval blocked", attemptedAction = action, role = currentUser.RoleCode }, cancellationToken);
            return Results.Forbid();
        }
        var correlationId = $"REV867_{masterType}_{action}_{Guid.NewGuid():N}";
        setStatus(entity, nextStatus, currentUser.LoginId);
        setApproval(entity, nextApprovalStatus);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedBy = currentUser.LoginId;
        AddApprovalHistory(db, masterType, entity.Id, code, action, before.ApprovalStatus, nextApprovalStatus, remarks, currentUser, correlationId);
        db.MasterStatusHistories.Add(new MasterStatusHistory
        {
            MasterType = masterType,
            MasterId = entity.Id,
            MasterCode = code,
            PreviousStatus = before.Status,
            NewStatus = nextStatus,
            Reason = remarks,
            SourceRevision = "REV867",
            CorrelationId = correlationId,
            CreatedBy = currentUser.LoginId
        });
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("Masters", action, masterType, entity.Id.ToString(), before, new { Status = nextStatus, ApprovalStatus = nextApprovalStatus, Reason = remarks, correlationId }, cancellationToken);
        return Results.Ok(new { code, status = nextStatus, approvalStatus = nextApprovalStatus, entity.Version });
    }


    public static bool IsSelfApprovalAttempt(AuditableEntity entity, ICurrentUser currentUser)
    {
        return string.Equals(entity.CreatedBy, currentUser.LoginId, StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(entity.UpdatedBy) && string.Equals(entity.UpdatedBy, currentUser.LoginId, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsCustomerPortalUser(ICurrentUser currentUser) => string.Equals(currentUser.RoleCode, "customer", StringComparison.OrdinalIgnoreCase) || string.Equals(currentUser.RoleCode, "customer_user", StringComparison.OrdinalIgnoreCase);

    public static bool IsVendorPortalUser(ICurrentUser currentUser) => string.Equals(currentUser.RoleCode, "vendor", StringComparison.OrdinalIgnoreCase) || string.Equals(currentUser.RoleCode, "vendor_user", StringComparison.OrdinalIgnoreCase);

    public static IQueryable<Customer> ApplyCustomerOrganizationScope(IQueryable<Customer> query, ICurrentUser currentUser)
    {
        if (!IsCustomerPortalUser(currentUser)) return query;
        var organizationId = NormalizeOptional(currentUser.OrganizationId);
        return organizationId is null ? query.Where(customer => false) : query.Where(customer => customer.PortalOrganizationId == organizationId);
    }

    public static IQueryable<Vendor> ApplyVendorOrganizationScope(IQueryable<Vendor> query, ICurrentUser currentUser)
    {
        if (!IsVendorPortalUser(currentUser)) return query;
        var organizationId = NormalizeOptional(currentUser.OrganizationId);
        return organizationId is null ? query.Where(vendor => false) : query.Where(vendor => vendor.PortalOrganizationId == organizationId);
    }
    [GeneratedRegex(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$")]
    private static partial Regex GstinRegex();

    [GeneratedRegex(@"^[A-Z]{5}[0-9]{4}[A-Z]$")]
    private static partial Regex PanRegex();
}




