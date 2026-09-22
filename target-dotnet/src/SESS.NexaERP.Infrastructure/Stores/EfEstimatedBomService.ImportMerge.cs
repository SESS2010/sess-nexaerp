using Microsoft.EntityFrameworkCore;
using System.Data;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed partial class EfEstimatedBomService
{
    public async Task<EstimatedBomView> ImportAsync(byte[] content, string idempotencyKey, CancellationToken ct)
    {
        await RequirePreparerAsync(ct);
        if (content.Length == 0) throw new StoresValidationException("Estimated BOM workbook is empty.");
        var parsed = EstimatedBomWorkbook.Read(content);
        var company = await CompanyAsync(ct);
        var jobId = await db.JobOrders.AsNoTracking().Where(x => x.CompanyId == company.Id && x.JobOrderNumber == parsed.JobOrderNumber)
            .Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct) ?? throw new StoresValidationException("Workbook Job Order Number was not found in the selected company.");
        var itemCodes = parsed.Lines.Select(x => x.ItemCode).Distinct().ToArray();
        var items = await db.Items.AsNoTracking().Where(x => itemCodes.Contains(x.ItemCode)).ToDictionaryAsync(x => x.ItemCode, ct);
        var uomCodes = parsed.Lines.Select(x => x.UomCode).Distinct().ToArray();
        var uoms = await db.Uoms.AsNoTracking().Where(x => uomCodes.Contains(x.Code) && x.IsActive).ToDictionaryAsync(x => x.Code, ct);
        var lines = parsed.Lines.Select(x => new EstimatedBomLineInput(
            items.TryGetValue(x.ItemCode, out var item) ? item.Id : throw new StoresValidationException($"Workbook Item Code {x.ItemCode} was not found."),
            uoms.TryGetValue(x.UomCode, out var uom) ? uom.Id : throw new StoresValidationException($"Workbook UOM Code {x.UomCode} is not active."),
            x.Quantity, x.Remarks)).ToArray();
        return await CreateAsync(new(jobId, parsed.RevisionReason, lines, Required(idempotencyKey, "IdempotencyKey")), ct);
    }

    public async Task MergeItemAsync(Guid sourceItemId, MergeItemRequest request, CancellationToken ct)
    {
        // Duplicate merge is one of the two item master decisions reserved to the Technical Director.
        _ = user.RequireRole("approve", "TECHNICAL_DIRECTOR");
        if (sourceItemId == Guid.Empty || request.SurvivorItemId == Guid.Empty || sourceItemId == request.SurvivorItemId)
            throw new StoresValidationException("Distinct source and survivor ItemIds are required.");
        var reason = Required(request.Reason, "Reason"); var key = Required(request.IdempotencyKey, "IdempotencyKey");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct);
        var lockKey = $"ITEM-MERGE:{sourceItemId:N}";
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey},0))", ct);
        var replay = await db.ItemMergeAliases.SingleOrDefaultAsync(x => x.SourceItemId == sourceItemId, ct);
        var terminal = await TerminalItemIdAsync(request.SurvivorItemId, ct);
        if (replay is not null)
        {
            if (replay.SurvivorItemId != terminal) throw new StoresConflictException("Source item is already merged to a different survivor.");
            await tx.CommitAsync(ct); return;
        }
        if (terminal == sourceItemId) throw new StoresConflictException("Item merge would create a cycle.");
        var source = await db.Items.SingleOrDefaultAsync(x => x.Id == sourceItemId, ct) ?? throw new KeyNotFoundException("Source item was not found.");
        var survivor = await db.Items.SingleOrDefaultAsync(x => x.Id == terminal, ct) ?? throw new KeyNotFoundException("Survivor item was not found.");
        if (!survivor.IsActive || survivor.ApprovalStatus != "Approved")
            throw new StoresConflictException("The terminal survivor must be an active Approved item.");
        var draftLines = await db.EstimatedBomLines.Where(x => x.ItemId == sourceItemId &&
            x.EstimatedBomRevision!.Status == "DRAFT").ToListAsync(ct);
        foreach (var line in draftLines) line.ItemId = survivor.Id;
        source.IsActive = false; source.Status = "Merged"; source.Version = checked(source.Version + 1); source.UpdatedAt = DateTimeOffset.UtcNow; source.UpdatedBy = user.LoginId;
        var alias = new ItemMergeAlias { CompanyId = company.Id, SourceItemId = source.Id, SurvivorItemId = survivor.Id, ActorEmployeeId = Actor(),
            ActorRoleCode = user.RoleCode, ResolvedRoleAssignmentId = user.ResolvedRoleAssignmentId!.Value,
            ResolvedRoleAssignmentType = user.ResolvedRoleAssignmentType!, Reason = reason, CreatedBy = user.LoginId };
        db.ItemMergeAliases.Add(alias);
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(company.Code, "Item.Merge", key,
            new { sourceItemId, survivorItemId = survivor.Id, reason });
        var attempt = await Rev869BCommandContextAuthorizer.OpenForPendingChangesAsync(db, user, company.Code, envelope, ct, user.RoleCode)
            ?? throw new InvalidOperationException("Item merge produced no immutable operation slot.");
        await audit.WriteAsync("Inventory", "Merge", nameof(Item), source.Id.ToString(), new { source.ItemCode },
            new { survivor.ItemCode, alias.Id }, ct);
        await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct);
        await tx.CommitAsync(ct);
    }
}
