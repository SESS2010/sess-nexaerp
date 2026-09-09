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
            x.CompanyId == company.Id && (x.Status == "ISSUED" || x.Status == "PARTIALLY_RETURNED"));
        if (employeeId.HasValue) query = query.Where(x => x.IssuedToEmployeeId == employeeId);
        if (notificationDue == true) query = query.Where(x => x.ReturnDueAt < now);
        if (notificationDue == false) query = query.Where(x => x.ReturnDueAt >= now);
        var balances = query.Select(x => new
        {
            Issue = x,
            Remaining = x.Lines.Sum(l => l.QuantityBase)
                - db.MaterialReturnLines.Where(l => l.MaterialReturn!.MaterialIssueId == x.Id &&
                    l.MaterialReturn.Status == "ACCEPTED").Sum(l => (decimal?)l.ReturnedQuantityBase)!.Value
                - db.ComponentFitments.Where(f => f.MaterialIssueLine!.MaterialIssueId == x.Id &&
                    !db.ComponentFitmentReversals.Any(r => r.CompanyId == f.CompanyId &&
                        r.ComponentFitmentId == f.Id)).Sum(f => (decimal?)f.QuantityBase)!.Value
        });
        return await balances.Where(x => x.Remaining > 0).OrderBy(x => x.Issue.ReturnDueAt)
            .Select(x => new OutstandingEngineerCustodyView(x.Issue.Id, x.Issue.IssueNumber,
                x.Issue.JobOrderId, x.Issue.IssuedToEmployeeId,
                x.Issue.IssuedToEmployee!.EmployeeCode, x.Issue.IssuedAt, x.Issue.ReturnDueAt,
                x.Issue.ReturnDueAt < now, x.Remaining)).ToListAsync(ct);
    }

    public async Task<MaterialReturnPage> ListReturnsAsync(Guid? materialIssueId, string? status,
        int page, int pageSize, CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 200);
        var query = ReturnQuery().Where(x => x.CompanyId == company.Id);
        if (materialIssueId.HasValue) query = query.Where(x => x.MaterialIssueId == materialIssueId);
        if (!string.IsNullOrWhiteSpace(status))
        {
            var value = status.Trim().ToUpperInvariant(); query = query.Where(x => x.Status == value);
        }
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.DeclaredAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new(total, page, pageSize, rows.Select(x => ReturnView(x, false)).ToList());
    }

    public async Task<MaterialReturnView?> GetReturnAsync(Guid id, CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        var row = await ReturnQuery().SingleOrDefaultAsync(x => x.CompanyId == company.Id && x.Id == id, ct);
        return row is null ? null : ReturnView(row, false);
    }

    private async Task<MaterialIssueRequestView> RequestViewAsync(MaterialIssueRequest x, CancellationToken ct)
    {
        var itemIds = x.Lines.Select(l => l.ItemId).Distinct().ToArray();
        var uomIds = x.Lines.Select(l => l.UomId).Distinct().ToArray();
        var items = await db.Items.AsNoTracking().Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => new { i.ItemCode, i.Name }, ct);
        var uoms = await db.Uoms.AsNoTracking().Where(u => uomIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Code, ct);
        var requester = await db.Employees.AsNoTracking().Where(e => e.Id == x.RequestedByEmployeeId)
            .Select(e => new { e.EmployeeCode, e.EmployeeName }).SingleAsync(ct);
        var departmentCode = await db.Departments.AsNoTracking().Where(d => d.Id == x.RequestingDepartmentId)
            .Select(d => d.Code).SingleAsync(ct);
        var decisionLines = await db.MaterialIssueExcessDecisions.AsNoTracking()
            .Where(d => d.CompanyId == x.CompanyId && x.Lines.Select(l => l.Id).Contains(d.MaterialIssueRequestLineId)
                && d.Decision == "APPROVED")
            .Select(d => d.MaterialIssueRequestLineId).ToListAsync(ct);
        return new(x.Id, x.RequestNumber, x.Purpose, x.Situation, x.DestinationType,
            x.JobOrderId, x.CustomerId, x.VendorId, x.DestinationDepartmentId,
            x.DestinationNameSnapshot, x.RequestingDepartmentId, departmentCode,
            x.RequestedByEmployeeId, requester.EmployeeCode, requester.EmployeeName,
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

    private static MaterialReturnView ReturnView(MaterialReturn x, bool replayed) =>
        new(x.Id, x.ReturnNumber, x.MaterialIssueId, x.ReturnedByEmployeeId, x.DeclaredAt,
            x.Status, x.AcceptedAt, x.AcceptedByEmployeeId, x.StockPostingBatchId, x.Version,
            replayed, x.ActorRoleCode, x.ResolvedRoleAssignmentId, x.ResolvedRoleAssignmentType,
            x.AcceptedActorRoleCode, x.AcceptedRoleAssignmentId, x.AcceptedRoleAssignmentType,
            x.Lines.OrderBy(l => l.LineNumber).Select(l => new MaterialReturnLineView(
                l.Id, l.MaterialIssueLineId, l.LineNumber, l.ItemId, l.ReturnedQuantityBase,
                l.ReportedConsumedQuantityBase, l.ReportedStillHeldQuantityBase,
                l.ScanCode, l.InventorySerialId)).ToList());
}
