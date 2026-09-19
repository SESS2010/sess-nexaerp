using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Reporting;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Reporting;

namespace SESS.NexaERP.Infrastructure.Stores;

/// <summary>Live source evidence only. No cross-line quality average, rolling rating or combined vendor score.</summary>
public sealed class EfVendorRatingEvidenceService(NexaErpDbContext db, ICurrentUser user,
    IOptions<ReportCalendarOptions> calendar) : IVendorRatingEvidenceService
{
    public async Task<VendorRatingReceiptPage> ListReceiptsAsync(int page, int pageSize, CancellationToken ct)
    {
        user.RequireRole("view", "QC_MANAGER");
        var organization = user.OrganizationId?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(organization)) throw new UnauthorizedAccessException("Select a company.");
        page = Math.Clamp(page, 1, 100000); pageSize = Math.Clamp(pageSize, 1, 100);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var company = await db.Companies.Where(x => x.Code == organization && x.IsActive && x.Status == "ACTIVE")
            .Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct)
            ?? throw new UnauthorizedAccessException("Selected company is unavailable.");
        var query = db.GoodsReceipts.AsNoTracking().Where(x => x.CompanyId == company
            && x.DocumentKind == "NORMAL" && x.Status == "FINALIZED"
            && !db.GoodsReceipts.Any(reversal => reversal.CompanyId == company
                && reversal.ReversesGoodsReceiptId == x.Id && reversal.Status == "FINALIZED"));
        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.ReceivedAt).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new VendorRatingReceiptOption(x.Id, x.Version, x.GrnNumber, x.VendorId, x.ReceivedAt)).ToListAsync(ct);
        await tx.CommitAsync(ct);
        return new(count, page, pageSize, items.AsReadOnly());
    }

    public async Task<VendorRatingReceiptEvidence?> GetAsync(Guid goodsReceiptId, CancellationToken ct)
    {
        user.RequireRole("view", "QC_MANAGER");
        var organization = user.OrganizationId?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(organization)) throw new UnauthorizedAccessException("Select a company.");
        var zone = TimeZoneInfo.FindSystemTimeZoneById(calendar.Value.CompanyTimeZones.TryGetValue(organization, out var configured)
            ? configured : calendar.Value.DefaultTimeZone);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var company = await db.Companies.Where(x => x.Code == organization && x.IsActive && x.Status == "ACTIVE")
            .Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct)
            ?? throw new UnauthorizedAccessException("Selected company is unavailable.");
        var receipt = await db.GoodsReceipts.AsNoTracking().SingleOrDefaultAsync(x => x.CompanyId == company && x.Id == goodsReceiptId, ct);
        if (receipt is null) { await tx.CommitAsync(ct); return null; }
        if (receipt.DocumentKind != "NORMAL" || receipt.Status != "FINALIZED"
            || await db.GoodsReceipts.AnyAsync(x => x.CompanyId == company && x.ReversesGoodsReceiptId == receipt.Id && x.Status == "FINALIZED", ct))
            throw new StoresConflictException("Current rating evidence requires a finalized, unreversed normal GRN.");
        var received = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(receipt.ReceivedAt, zone).DateTime);
        var lines = await db.GoodsReceiptLines.AsNoTracking().Where(x => x.CompanyId == company && x.GoodsReceiptId == receipt.Id)
            .Include(x => x.PurchaseOrderLine).ThenInclude(x => x!.CommercialComparisonLine)
            .OrderBy(x => x.LineNumber).ToListAsync(ct);
        var lots = await db.GoodsReceiptLineLotAllocations.AsNoTracking()
            .Where(x => x.CompanyId == company && x.GoodsReceiptLine!.GoodsReceiptId == receipt.Id)
            .OrderBy(x => x.LotOrdinal).ThenBy(x => x.Id).ToListAsync(ct);
        var revisions = await db.QcInspectionRevisions.AsNoTracking().Include(x => x.QcInspection)
            .Where(x => x.CompanyId == company && x.QcInspection!.GoodsReceiptLine!.GoodsReceiptId == receipt.Id
                && x.Status == "FINALIZED" && !db.QcInspectionRevisions.Any(next => next.CompanyId == company
                    && next.RevisesRevisionId == x.Id && next.Status == "FINALIZED"))
            .ToListAsync(ct);
        var concessions = await db.InventoryConcessions.AsNoTracking().Include(x => x.Allocations)
            .Where(x => x.CompanyId == company && (x.Status == "APPROVED" || x.Status == "REVERSED")
                && x.QcInspectionRevision!.QcInspection!.GoodsReceiptLine!.GoodsReceiptId == receipt.Id
)
            .ToListAsync(ct);
        var results = new List<VendorRatingLineEvidence>();
        foreach (var line in lines)
        {
            var allocationRows = lots.Where(x => x.GoodsReceiptLineId == line.Id).ToArray();
            if (line.ReceivedQuantity <= 0 || allocationRows.Length == 0 || allocationRows.Sum(x => x.Quantity) != line.ReceivedQuantity)
                throw new StoresConflictException("Retained receipt lot quantities do not reconcile to their line.");
            var lotResults = new List<VendorQcLotEvidence>();
            foreach (var lot in allocationRows)
            {
                var current = revisions.Where(x => x.QcInspection!.GoodsReceiptLineLotAllocationId == lot.Id).ToArray();
                if (current.Length > 1) throw new StoresConflictException("A receipt lot has conflicting current QC revisions.");
                var revision = current.SingleOrDefault();
                var approved = concessions.Where(c => c.Status == "APPROVED" && c.ReversesConcessionId is null)
                    .SelectMany(c => c.Allocations.Where(a => a.GoodsReceiptLineLotAllocationId == lot.Id)
                        .Select(a => new { Header = c, Allocation = a })).ToArray();
                var concessionFacts = new List<VendorConcessionEvidence>();
                foreach (var fact in approved.OrderBy(x => x.Header.Id).ThenBy(x => x.Allocation.Id))
                {
                    var reversed = concessions.Where(x => x.Status == "REVERSED" && x.ReversesConcessionId == fact.Header.Id).ToArray();
                    if (reversed.Length > 1 || fact.Allocation.CompanyId != company || fact.Allocation.Quantity <= 0)
                        throw new StoresConflictException("Concession allocation or reversal evidence is inconsistent.");
                    var reversal = reversed.SingleOrDefault();
                    if (reversal is null && (revision is null || fact.Header.QcInspectionRevisionId != revision.Id))
                        throw new StoresConflictException("Live concessions do not belong to the current retained QC revision.");
                    concessionFacts.Add(new(fact.Header.Id, fact.Header.Version, fact.Header.QcInspectionRevisionId,
                        fact.Allocation.Id, fact.Allocation.Quantity, reversal?.Id, reversal?.Version,
                        reversal is null ? fact.Allocation.Quantity : 0m));
                }
                var concessionQuantity = concessionFacts.Sum(x => x.AppliedQuantity);
                decimal? points = null;
                if (revision is not null)
                {
                    if (revision.InspectedQuantity != lot.Quantity || revision.AcceptedQuantity < 0 || revision.RejectedQuantity < 0
                        || revision.DiscrepancyPendingQuantity < 0 || revision.AcceptedQuantity + revision.RejectedQuantity + revision.DiscrepancyPendingQuantity != lot.Quantity
                        || concessionQuantity < 0 || concessionQuantity > revision.RejectedQuantity)
                        throw new StoresConflictException("QC and concession quantities do not reconcile to their receipt lot.");
                    if (revision.DiscrepancyPendingQuantity == 0)
                        points = VendorRatingMeasurements.Quality(new(line.Id, receipt.Version), new(revision.Id, revision.Version),
                            lot.Quantity, revision.AcceptedQuantity, concessionQuantity).Points;
                }
                lotResults.Add(new(lot.Id, lot.InventoryLotId, lot.Quantity, revision?.Id, revision?.Version, revision?.RevisionNumber,
                    revision?.AcceptedQuantity, revision?.RejectedQuantity, revision?.DiscrepancyPendingQuantity, concessionQuantity,
                    concessionFacts.AsReadOnly(), points, revision is null ? "QC_NOT_RECORDED" : points is null ? "QC_DISCREPANCY_PENDING" : "MEASURED"));
            }
            // Sum evidence within this one line/UOM before dividing, avoiding compounded ratio rounding.
            decimal? quality = lotResults.All(x => x.QualityPoints.HasValue)
                ? VendorRatingMeasurements.QualityMaximumPoints * (lotResults.Sum(x => x.AcceptedWithoutConcession!.Value
                    + x.ConcessionAcceptedQuantity) / line.ReceivedQuantity) : null;
            var poLine = line.PurchaseOrderLine;
            var comparison = poLine?.CommercialComparisonLine;
            if (poLine is null || poLine.CompanyId != company || poLine.PurchaseOrderId != receipt.PurchaseOrderId || poLine.ItemId != line.ItemId
                || comparison is null || comparison.CompanyId != company || comparison.VendorId != receipt.VendorId)
                throw new StoresConflictException("The retained receipt, PO and approved comparison source chain is inconsistent.");
            var snapshot = comparison.DeliverySnapshot;
            DateOnly? committed = null; int? daysLate = null; decimal? delivery = null;
            if (DateOnly.TryParseExact(snapshot, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var agreed) && agreed != default)
            {
                committed = agreed;
                var measured = VendorRatingMeasurements.Delivery(new(receipt.Id, receipt.Version), new(comparison.Id, comparison.Version), agreed, received);
                daysLate = measured.DaysLate; delivery = measured.Points;
            }
            results.Add(new(line.Id, line.LineNumber, line.ItemId, line.ItemCodeSnapshot, line.UomSnapshot, line.ReceivedQuantity,
                lotResults.AsReadOnly(), quality, comparison.Id, comparison.Version, snapshot, committed, daysLate, delivery,
                delivery is null ? "COMMITTED_DATE_UNAVAILABLE" : "MEASURED"));
        }
        if (results.Count == 0) throw new StoresConflictException("The receipt has no retained lines.");
        var sameCommittedDate = results.All(x => x.CommittedDate.HasValue) && results.Select(x => x.CommittedDate).Distinct().Count() == 1;
        decimal? shipmentPoints = sameCommittedDate ? results[0].DeliveryPoints : null;
        var shipmentStatus = sameCommittedDate ? "MEASURED" : results.Any(x => x.CommittedDate is null)
            ? "COMMITTED_DATE_UNAVAILABLE" : "MIXED_COMMITTED_DATES_REQUIRE_APPROVED_AGGREGATION";
        var rule = VendorRatingMeasurements.RuleVersion;
        var fingerprint = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new {
            company, receipt.Id, receipt.Version, receipt.VendorId, receipt.PurchaseOrderId, received, TimeZone = zone.Id, rule,
            Lines = results, shipmentPoints, shipmentStatus }))).ToLowerInvariant();
        await tx.CommitAsync(ct);
        return new(company, receipt.Id, receipt.Version, receipt.VendorId, receipt.PurchaseOrderId, received, zone.Id, rule,
            results.AsReadOnly(), shipmentPoints, shipmentStatus, fingerprint, DateTimeOffset.UtcNow);
    }
}
