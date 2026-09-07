using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Foundation;
using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed partial class EfMaterialIssueService(
    NexaErpDbContext db, ICurrentUser user, IAuditWriter audit) : IMaterialIssueService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] JobSituations =
        ["CHAMBER_MANUFACTURE", "SERVICE_CUSTOMER_PO", "SITE_PROJECT_PO"];

    private IQueryable<MaterialIssueRequest> RequestQuery(bool tracking = false)
    {
        var query = db.MaterialIssueRequests
            .Include(x => x.Lines).ThenInclude(x => x.Item)
            .Include(x => x.Lines).ThenInclude(x => x.Uom);
        return tracking ? query : query.AsNoTracking();
    }

    private IQueryable<MaterialIssue> IssueQuery(bool tracking = false)
    {
        var query = db.MaterialIssues.Include(x => x.Lines);
        return tracking ? query : query.AsNoTracking();
    }

    private IQueryable<MaterialReturn> ReturnQuery(bool tracking = false)
    {
        var query = db.MaterialReturns.Include(x => x.Lines).Include(x => x.MaterialIssue)!.ThenInclude(x => x!.Lines);
        return tracking ? query : query.AsNoTracking();
    }

    private string Organization() => !string.IsNullOrWhiteSpace(user.OrganizationId)
        ? user.OrganizationId.Trim().ToUpperInvariant()
        : throw new UnauthorizedAccessException("Company scope is required.");
    private Guid Actor() => user.EmployeeId ??
        throw new UnauthorizedAccessException("Resolved employee identity is required.");
    private async Task<Company> CompanyAsync(CancellationToken ct) =>
        await db.Companies.SingleOrDefaultAsync(x => x.Code == Organization() && x.IsActive && x.Status == "ACTIVE", ct)
        ?? throw new UnauthorizedAccessException("Selected company is unavailable.");

    private string RequireAny(string operation)
    {
        var roles = user.EffectiveRoleAssignments.Select(x => x.RoleCode).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return user.RequireRole(operation, roles);
    }

    private static string Required(string? value, string field) => !string.IsNullOrWhiteSpace(value)
        ? value.Trim() : throw new StoresValidationException(field + " is required.");
    private static string Code(string? value, string field) => Required(value, field).ToUpperInvariant();
    private static string Fingerprint(object value) => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, JsonOptions)))).ToLowerInvariant();

    private async Task<string> NextNumberAsync(Guid companyId, string prefix, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var year = today.Month >= 4 ? $"{today.Year % 100:00}-{(today.Year + 1) % 100:00}"
            : $"{(today.Year - 1) % 100:00}-{today.Year % 100:00}";
        var organization = Organization();
        var lockKey = $"NUMBER:{organization}:{year}:{prefix}";
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey},0))", ct);
        var sequence = await db.PurchaseNumberSequences.SingleOrDefaultAsync(x =>
            x.CompanyId == companyId && x.OrganizationId == organization &&
            x.FinancialYear == year && x.Prefix == prefix && x.IsActive, ct);
        if (sequence is null)
        {
            sequence = new() { CompanyId = companyId, OrganizationId = organization,
                FinancialYear = year, Prefix = prefix, CreatedBy = user.LoginId };
            db.PurchaseNumberSequences.Add(sequence);
        }
        sequence.LastNumber++;
        sequence.UpdatedAt = DateTimeOffset.UtcNow;
        sequence.UpdatedBy = user.LoginId;
        return $"{prefix}-{organization}-{year}-{sequence.LastNumber:000001}";
    }

    private void History(MaterialIssueRequest? request, MaterialIssue? issue, string action,
        string? from, string to, string reason, string key)
    {
        db.MaterialIssueHistories.Add(new()
        {
            CompanyId = request?.CompanyId ?? issue!.CompanyId,
            MaterialIssueRequestId = request?.Id ?? issue!.MaterialIssueRequestId,
            MaterialIssueId = issue?.Id, Action = action, FromStatus = from, ToStatus = to,
            ActorEmployeeId = Actor(), ActorRoleCode = user.RoleCode,
            ResolvedRoleAssignmentId = user.ResolvedRoleAssignmentId!.Value,
            ResolvedRoleAssignmentType = user.ResolvedRoleAssignmentType!,
            CorrelationId = key, Remarks = reason
        });
    }

    private async Task CommitAsync(string operation, string key, object command,
        string entity, Guid entityId, object result, CancellationToken ct)
    {
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(
            Organization(), operation, key, command);
        var attempt = await Rev869BCommandContextAuthorizer.OpenForPendingChangesAsync(
            db, user, Organization(), envelope, ct, user.RoleCode)
            ?? throw new InvalidOperationException(operation + " produced no immutable operation slot.");
        await audit.WriteAsync("Stores", operation, entity, entityId.ToString(), null, result, ct);
        await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct);
    }
}
