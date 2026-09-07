using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed partial class EfProductionEngineeringService
{
    public async Task<IReadOnlyList<ProductionBomView>> ListProductionBomsAsync(CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        var rows = await BomQuery().Where(x => x.CompanyId == company.Id)
            .OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        var result = new List<ProductionBomView>(rows.Count);
        foreach (var row in rows) result.Add(await BomViewAsync(row, ct));
        return result;
    }

    public async Task<ProductionBomView?> GetProductionBomAsync(string number, CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        var bom = await BomQuery().SingleOrDefaultAsync(
            x => x.CompanyId == company.Id && x.BomNumber == Code(number), ct);
        return bom is null ? null : await BomViewAsync(bom, ct);
    }

    public async Task<IReadOnlyList<EngineeringDocumentView>> ListEngineeringDocumentsAsync(
        Guid? jobOrderId, string? type, CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        var query = DocumentQuery().Where(x => x.CompanyId == company.Id);
        if (jobOrderId.HasValue) query = query.Where(x => x.JobOrderId == jobOrderId.Value);
        if (!string.IsNullOrWhiteSpace(type)) query = query.Where(x => x.DocumentType == Code(type));
        return (await query.OrderBy(x => x.DocumentNumber).ToListAsync(ct))
            .Select(DocumentView).ToArray();
    }

    public async Task<EngineeringDocumentView?> GetEngineeringDocumentAsync(string number, CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        var document = await DocumentQuery().SingleOrDefaultAsync(
            x => x.CompanyId == company.Id && x.DocumentNumber == Code(number), ct);
        return document is null ? null : DocumentView(document);
    }
}
