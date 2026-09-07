using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Foundation;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed partial class EfEstimatedBomService(
    NexaErpDbContext db, ICurrentUser user, IAuditWriter audit) : IEstimatedBomService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PagedResponse<EstimatedBomSummary>> ListAsync(int? page, int? pageSize, string? search, string? status, CancellationToken ct)
    {
        var company = await CompanyAsync(ct); var number = Math.Max(page ?? 1, 1); var size = Math.Clamp(pageSize ?? 25, 1, 200);
        var query = db.EstimatedBoms.AsNoTracking().Where(x => x.CompanyId == company.Id);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();
            query = query.Where(x => x.BomNumber.ToUpper().Contains(term) || x.JobOrder!.JobOrderNumber.ToUpper().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim().ToUpperInvariant());
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.CreatedAt).Skip((number - 1) * size).Take(size)
            .Select(x => new EstimatedBomSummary(x.Id, x.BomNumber, x.JobOrderId, x.JobOrder!.JobOrderNumber,
                x.CurrentRevisionNumber, x.Status, x.ApprovedRevisionId, x.CommercialBaselineRevisionId, x.Version)).ToListAsync(ct);
        return new(total, number, size, rows);
    }

    public async Task<EstimatedBomView?> GetAsync(string bomNumber, CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        var bom = await BomQuery().SingleOrDefaultAsync(x => x.CompanyId == company.Id && x.BomNumber == Code(bomNumber), ct);
        return bom is null ? null : await ViewAsync(bom, ct);
    }

    public async Task<IReadOnlyList<EstimatedBomHistoryView>> HistoryAsync(string bomNumber, CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        var bomId = await db.EstimatedBoms.AsNoTracking().Where(x => x.CompanyId == company.Id && x.BomNumber == Code(bomNumber))
            .Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
        if (!bomId.HasValue) throw new KeyNotFoundException("Estimated BOM was not found.");
        return await db.EstimatedBomHistories.AsNoTracking().Where(x => x.CompanyId == company.Id && x.EstimatedBomId == bomId)
            .OrderBy(x => x.CreatedAt).Select(x => new EstimatedBomHistoryView(x.Id, x.EstimatedBomRevisionId, x.Action,
                x.FromStatus, x.ToStatus, x.ActorEmployeeId, x.ActorRoleCode, x.ResolvedRoleAssignmentId,
                x.ResolvedRoleAssignmentType, x.CorrelationId, x.Remarks, x.CreatedAt)).ToListAsync(ct);
    }

    public Task<EstimatedBomWorkbookFile> TemplateAsync(CancellationToken ct) =>
        Task.FromResult(new EstimatedBomWorkbookFile("estimated-bom-template.xlsx", EstimatedBomWorkbook.ContentType,
            EstimatedBomWorkbook.CreateTemplate(DateTimeOffset.UtcNow)));

    private IQueryable<EstimatedBom> BomQuery() => db.EstimatedBoms.Include(x => x.JobOrder).Include(x => x.Revisions).ThenInclude(x => x.Lines);
    private async Task<Company> CompanyAsync(CancellationToken ct)
    {
        var org = user.OrganizationId?.Trim().ToUpperInvariant() ?? throw new UnauthorizedAccessException("Company scope is required.");
        return await db.Companies.SingleAsync(x => x.Code == org && x.IsActive && x.Status == "ACTIVE", ct);
    }
    private Guid Actor() => user.EmployeeId ?? throw new UnauthorizedAccessException("Resolved employee identity is required.");
    private static string Code(string value) => !string.IsNullOrWhiteSpace(value) ? value.Trim().ToUpperInvariant() : throw new StoresValidationException("Code is required.");
    private static string Required(string value, string label) => !string.IsNullOrWhiteSpace(value) ? value.Trim() : throw new StoresValidationException($"{label} is required.");
    private static string Fingerprint(object value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, JsonOptions)))).ToLowerInvariant();
}
