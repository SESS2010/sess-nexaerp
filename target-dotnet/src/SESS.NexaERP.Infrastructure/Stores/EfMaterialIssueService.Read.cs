using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed partial class EfMaterialIssueService
{
    public async Task<MaterialIssueRequestPage> ListRequestsAsync(
        string? number, string? status, int page, int pageSize, CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        page = Math.Max(page, 1); pageSize = Math.Clamp(pageSize, 1, 200);
        var query = RequestQuery().Where(x => x.CompanyId == company.Id);
        if (!string.IsNullOrWhiteSpace(number))
        {
            var term = number.Trim().ToUpperInvariant();
            query = query.Where(x => x.RequestNumber.ToUpper().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            var value = status.Trim().ToUpperInvariant();
            query = query.Where(x => x.Status == value);
        }
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var views = new List<MaterialIssueRequestView>(rows.Count);
        foreach (var row in rows) views.Add(await RequestViewAsync(row, ct));
        return new(total, page, pageSize, views);
    }

    public async Task<MaterialIssueRequestView?> GetRequestAsync(Guid id, CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        var row = await RequestQuery().SingleOrDefaultAsync(x => x.CompanyId == company.Id && x.Id == id, ct);
        return row is null ? null : await RequestViewAsync(row, ct);
    }

    public async Task<MaterialIssueView?> GetIssueAsync(Guid id, CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        var issue = await IssueQuery().SingleOrDefaultAsync(x => x.CompanyId == company.Id && x.Id == id, ct);
        return issue is null ? null : IssueView(issue, false);
    }

    public async Task<IReadOnlyList<OutstandingEngineerCustodyView>> OutstandingCustodyAsync(
        Guid? employeeId, bool? notificationDue, CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        var now = DateTimeOffset.UtcNow;
        var query = db.MaterialIssues.AsNoTracking().Where(x =>
            x.CompanyId == company.Id && x.Status == "ISSUED");
        if (employeeId.HasValue) query = query.Where(x => x.IssuedToEmployeeId == employeeId);
        if (notificationDue == true) query = query.Where(x => x.ReturnDueAt < now);
        if (notificationDue == false) query = query.Where(x => x.ReturnDueAt >= now);
        return await query.OrderBy(x => x.ReturnDueAt).Select(x => new OutstandingEngineerCustodyView(
            x.Id, x.IssueNumber, x.JobOrderId, x.IssuedToEmployeeId,
            x.IssuedToEmployee!.EmployeeCode, x.IssuedAt, x.ReturnDueAt,
            x.ReturnDueAt < now, x.Lines.Sum(l => l.QuantityBase))).ToListAsync(ct);
    }

    private async Task<MaterialIssueRequestView> RequestViewAsync(MaterialIssueRequest x, CancellationToken ct)
    {
        var itemIds = x.Lines.Select(l => l.ItemId).Distinct().ToArray();
        var uomIds = x.Lines.Select(l => l.UomId).Distinct().ToArray();
        var items = await db.Items.AsNoTracking().Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => new { i.ItemCode, i.Name }, ct);
        var uoms = await db.Uoms.AsNoTracking().Where(u => uomIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Code, ct);
        var decisionLines = await db.MaterialIssueExcessDecisions.AsNoTracking()
            .Where(d => d.CompanyId == x.CompanyId && x.Lines.Select(l => l.Id).Contains(d.MaterialIssueRequestLineId)
                && d.Decision == "APPROVED")
            .Select(d => d.MaterialIssueRequestLineId).ToListAsync(ct);
        return new(x.Id, x.RequestNumber, x.Purpose, x.Situation, x.DestinationType,
            x.JobOrderId, x.CustomerId, x.VendorId, x.DestinationDepartmentId,
            x.DestinationNameSnapshot, x.RequestingDepartmentId, x.RequestedByEmployeeId,
            x.RequiredDate, x.Status, x.Version, x.Lines.OrderBy(l => l.LineNumber).Select(l =>
                new MaterialIssueRequestLineView(l.Id, l.LineNumber, l.ItemId, items[l.ItemId].ItemCode,
                    items[l.ItemId].Name, l.UomId, uoms[l.UomId], l.RequestedQuantity, l.RequestedBaseQuantity,
                    l.CustomerPurchaseOrderLineId, l.EstimatedBomBaseQuantitySnapshot,
                    l.ProductionBomBaseQuantitySnapshot,
                    l.CustomerPoBaseQuantitySnapshot, l.ExcessBaseQuantitySnapshot,
                    l.ExcessClassification, decisionLines.Contains(l.Id), l.Remarks)).ToList());
    }

    private static MaterialIssueView IssueView(MaterialIssue x, bool replayed) =>
        new(x.Id, x.IssueNumber, x.MaterialIssueRequestId, x.JobOrderId, x.IssuedToEmployeeId,
            x.IssuedAt, x.ReturnDueAt, x.Status, x.StockPostingBatchId,
            x.ActorRoleCode, x.ResolvedRoleAssignmentId, x.ResolvedRoleAssignmentType,
            x.Version, replayed,
            x.Lines.OrderBy(l => l.LineNumber).Select(l => new MaterialIssueLineView(
                l.Id, l.MaterialIssueRequestLineId, l.LineNumber, l.ItemId, l.QuantityBase,
                l.OwnershipAccountId, l.FromCustodyAssignmentId, l.ToCustodyAssignmentId,
                l.InventoryProvenanceLayerId, l.InventoryLotId, l.InventorySerialId,
                l.WarehouseConditionLocationId)).ToList());
}
