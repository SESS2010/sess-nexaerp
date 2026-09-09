using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed partial class EfProductionEngineeringService
{
    private async Task<ProductionBomView> BomViewAsync(ProductionBom bom, CancellationToken ct)
    {
        var revision = bom.Revisions.Single(x => x.RevisionNumber == bom.CurrentRevisionNumber);
        var itemIds = revision.Lines.Select(x => x.ItemId).Distinct().ToArray();
        var items = await db.Items.AsNoTracking().Where(x => itemIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var uomIds = revision.Lines.Select(x => x.UomId).Distinct().ToArray();
        var uoms = await db.Uoms.AsNoTracking().Where(x => uomIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var lines = revision.Lines.OrderBy(x => x.LineNumber).Select(x => new ProductionBomLineView(
            x.Id, x.LineNumber, x.ItemId, items[x.ItemId].ItemCode, x.UomId, uoms[x.UomId].Code,
            x.Quantity, x.Remarks, x.PlannedUnitValue, "INR")).ToArray();
        var view = new ProductionBomRevisionView(revision.Id, revision.RevisionNumber,
            revision.SourceEstimatedBomRevisionId, revision.SupersedesRevisionId, revision.Status,
            revision.RevisionReason, revision.PreparedByEmployeeId, revision.SubmittedAt,
            revision.ApprovedAt, revision.ApprovedByEmployeeId, revision.ApprovalReason,
            revision.Version, lines);
        return new(bom.Id, bom.BomNumber, bom.JobOrderId, bom.JobOrder!.JobOrderNumber,
            bom.JobOrder.PinnedProductionBomRevisionId, bom.CurrentRevisionNumber, bom.Status,
            bom.Version, view);
    }

    private static EngineeringDocumentView DocumentView(EngineeringDocument document)
    {
        var revisions = document.Revisions.OrderBy(x => x.RevisionNumber).Select(x =>
            new EngineeringDocumentRevisionView(x.Id, x.RevisionNumber, x.RevisionCode,
                x.SupersedesRevisionId, x.DrawnByEmployeeId, x.CheckedByEmployeeId,
                x.ApprovedByEmployeeId, x.RevisionNote, x.DocumentDate, x.StorageKey, x.FileName,
                x.ContentType, x.SizeBytes, x.Sha256, x.Status, x.SubmittedAt, x.ApprovedAt,
                x.Version)).ToArray();
        return new(document.Id, document.DocumentNumber, document.DocumentType,
            document.JobOrderId, document.JobOrder!.JobOrderNumber, document.Title,
            document.CurrentRevisionId, document.Status, document.Version, revisions);
    }
}
