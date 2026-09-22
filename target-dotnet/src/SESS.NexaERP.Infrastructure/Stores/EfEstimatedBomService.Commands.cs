using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed partial class EfEstimatedBomService
{
    public async Task<EstimatedBomView> CreateAsync(CreateEstimatedBomRequest request, CancellationToken ct)
    {
        var actor = Actor(); await RequirePreparerAsync(ct); var key = Required(request.IdempotencyKey, "IdempotencyKey");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({'E' + company.Code + key},0))", ct);
        var replay = await db.EstimatedBomRevisions.SingleOrDefaultAsync(x => x.CompanyId == company.Id && x.IdempotencyKey == key, ct);
        if (replay is not null)
        {
            if (replay.ContentFingerprint != Fingerprint(request)) throw new StoresConflictException("Idempotency key was reused with different Estimated BOM content.");
            var replayBom = await BomQuery().SingleAsync(x => x.Id == replay.EstimatedBomId, ct); await tx.CommitAsync(ct); return await ViewAsync(replayBom, ct);
        }
        var job = await db.JobOrders.SingleOrDefaultAsync(x => x.CompanyId == company.Id && x.Id == request.JobOrderId, ct)
            ?? throw new KeyNotFoundException("Job Order was not found in the selected company.");
        if (job.Status != "OPEN") throw new StoresConflictException("Accounts must confirm the Job Order before an Estimated BOM is created.");
        if (await db.EstimatedBoms.AnyAsync(x => x.CompanyId == company.Id && x.JobOrderId == job.Id, ct))
            throw new StoresConflictException("This Job Order already has an Estimated BOM.");
        var material = await MaterializeLinesAsync(company.Id, request.Lines, ct);
        var bomNumber = await NextNumberAsync(company.Id, company.Code, ct);
        var bom = new EstimatedBom { CompanyId = company.Id, BomNumber = bomNumber, JobOrderId = job.Id,
            CurrentRevisionNumber = 1, Status = "DRAFT", CreatedBy = user.LoginId };
        var revision = new EstimatedBomRevision { CompanyId = company.Id, EstimatedBomId = bom.Id, RevisionNumber = 1,
            Status = "DRAFT", RevisionReason = Required(request.RevisionReason, "RevisionReason"), PreparedByEmployeeId = actor,
            IdempotencyKey = key, ContentFingerprint = Fingerprint(request), CreatedBy = user.LoginId };
        AddLines(revision, company.Id, material); bom.Revisions.Add(revision); db.EstimatedBoms.Add(bom);
        AddHistory(bom, revision, "Create", null, "DRAFT", revision.RevisionReason, key);
        await CommitCommandAsync(company.Code, "EstimatedBom.Create", key, request, bom, revision, ct);
        await tx.CommitAsync(ct); return await ViewAsync(bom, ct);
    }

    public async Task<EstimatedBomView> ReplaceDraftAsync(string bomNumber, ReplaceEstimatedBomLinesRequest request, CancellationToken ct)
    {
        await RequirePreparerAsync(ct); var key = Required(request.IdempotencyKey, "IdempotencyKey");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct); var bom = await BomQuery().SingleOrDefaultAsync(x => x.CompanyId == company.Id && x.BomNumber == Code(bomNumber), ct)
            ?? throw new KeyNotFoundException("Estimated BOM was not found.");
        var revision = Current(bom);
        if (revision.Status != "DRAFT") throw new StoresConflictException("Only a Draft Estimated BOM revision can be edited.");
        if (revision.Version != request.ExpectedVersion) throw new DbUpdateConcurrencyException("Estimated BOM revision Version is stale.");
        var material = await MaterializeLinesAsync(company.Id, request.Lines, ct);
        revision.RevisionReason = Required(request.RevisionReason, "RevisionReason"); revision.ContentFingerprint = Fingerprint(request);
        revision.Version = checked(revision.Version + 1);
        revision.UpdatedAt = DateTimeOffset.UtcNow; revision.UpdatedBy = user.LoginId;
        AddHistory(bom, revision, "Update", "DRAFT", "DRAFT", revision.RevisionReason, key);
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(company.Code, "EstimatedBom.Update", key, request);
        var attempt = await Rev869BCommandContextAuthorizer.OpenForPendingChangesAsync(db, user, company.Code, envelope, ct, user.RoleCode)
            ?? throw new InvalidOperationException("Estimated BOM update produced no immutable operation slot.");
        var lineNumber = 0;
        var lineJson = JsonSerializer.Serialize(material.Select(x => new { lineNumber = ++lineNumber, itemId = x.ItemId,
            uomId = x.UomId, quantity = x.Quantity, remarks = x.Remarks, estimatedUnitValue = x.EstimatedUnitValue,
            estimatedUnitValueOverridden = x.EstimatedUnitValueOverridden, valueSource = x.ValueSource }));
        var replaced = await db.Database.SqlQuery<int>($"SELECT advance.replace_estimated_bom_draft_lines({company.Id},{company.Code},{revision.Id},{request.ExpectedVersion},{Actor()},{user.IdentityIssuer!},{user.IdentitySubject!},{user.RoleCode},{user.LoginId},{lineJson}::jsonb) AS \"Value\"").SingleAsync(ct);
        if (replaced != material.Count) throw new InvalidOperationException("Controlled Estimated BOM draft replacement returned an unexpected line count.");
        await audit.WriteAsync("Design", "EstimatedBom.Update", nameof(EstimatedBom), bom.Id.ToString(), null,
            new { bom.BomNumber, revision.RevisionNumber, revision.Status }, ct);
        await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct);
        await tx.CommitAsync(ct);
        db.ChangeTracker.Clear();
        var refreshed = await BomQuery().SingleAsync(x => x.Id == bom.Id && x.CompanyId == company.Id, ct);
        return await ViewAsync(refreshed, ct);
    }

    public async Task<EstimatedBomView> SubmitAsync(string bomNumber, EstimatedBomActionRequest request, CancellationToken ct)
    {
        await RequirePreparerAsync(ct);
        return await TransitionAsync(bomNumber, request, "SUBMITTED", "Submit", "EstimatedBom.Submit", false, ct);
    }

    public async Task<EstimatedBomView> ApproveAsync(string bomNumber, EstimatedBomActionRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("approve", "TECHNICAL_DIRECTOR");
        return await TransitionAsync(bomNumber, request, "APPROVED", "Approve", "EstimatedBom.Approve", true, ct);
    }

    public async Task<EstimatedBomView> ReturnToDraftAsync(string bomNumber, EstimatedBomActionRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("reject", "TECHNICAL_DIRECTOR");
        return await TransitionAsync(bomNumber, request, "DRAFT", "ReturnToDraft", "EstimatedBom.ReturnToDraft", false, ct, "SUBMITTED");
    }

    private async Task<EstimatedBomView> TransitionAsync(string bomNumber, EstimatedBomActionRequest request,
        string next, string action, string operation, bool approval, CancellationToken ct, string? requiredStatus = null)
    {
        var key = Required(request.IdempotencyKey, "IdempotencyKey");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct); var bom = await BomQuery().SingleOrDefaultAsync(x => x.CompanyId == company.Id && x.BomNumber == Code(bomNumber), ct)
            ?? throw new KeyNotFoundException("Estimated BOM was not found.");
        var revision = Current(bom); var expected = requiredStatus ?? (approval ? "SUBMITTED" : "DRAFT");
        if (revision.Status != expected) throw new StoresConflictException($"Only a {expected} revision can be {action.ToLowerInvariant()}ed.");
        if (revision.Version != request.ExpectedVersion) throw new DbUpdateConcurrencyException("Estimated BOM revision Version is stale.");
        if (approval && revision.PreparedByEmployeeId == Actor()) throw new StoresConflictException("Nobody may approve their own Estimated BOM revision.");
        if (next is "SUBMITTED" or "APPROVED") await ValidateSubmissionAsync(revision, ct);
        if (approval) await FreezeEstimatedValuesAsync(revision, ct);
        var from = revision.Status; revision.Status = next; bom.Status = next;
        revision.Version = checked(revision.Version + 1); bom.Version = checked(bom.Version + 1);
        revision.UpdatedAt = bom.UpdatedAt = DateTimeOffset.UtcNow; revision.UpdatedBy = bom.UpdatedBy = user.LoginId;
        if (approval)
        {
            revision.ApprovedAt = DateTimeOffset.UtcNow; revision.ApprovedByEmployeeId = Actor();
            revision.ApprovalReason = Required(request.Remarks, "Remarks"); bom.ApprovedRevisionId = revision.Id;
            bom.CommercialBaselineRevisionId ??= revision.Id;
        }
        else if (next == "SUBMITTED") revision.SubmittedAt = DateTimeOffset.UtcNow;
        AddHistory(bom, revision, action, from, next, Required(request.Remarks, "Remarks"), key);
        await CommitCommandAsync(company.Code, operation, key, new { bomNumber = bom.BomNumber, request }, bom, revision, ct);
        await tx.CommitAsync(ct); return await ViewAsync(bom, ct);
    }

    public async Task<EstimatedBomView> CreateRevisionAsync(string bomNumber, NewEstimatedBomRevisionRequest request, CancellationToken ct)
    {
        var actor = Actor(); await RequirePreparerAsync(ct); var key = Required(request.IdempotencyKey, "IdempotencyKey");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct); var bom = await BomQuery().SingleOrDefaultAsync(x => x.CompanyId == company.Id && x.BomNumber == Code(bomNumber), ct)
            ?? throw new KeyNotFoundException("Estimated BOM was not found.");
        if (bom.Version != request.ExpectedBomVersion) throw new DbUpdateConcurrencyException("Estimated BOM Version is stale.");
        var source = Current(bom);
        if (source.Status != "APPROVED") throw new StoresConflictException("A new revision can only follow an Approved revision.");
        var revision = new EstimatedBomRevision { CompanyId = company.Id, EstimatedBomId = bom.Id,
            RevisionNumber = checked(bom.CurrentRevisionNumber + 1), Status = "DRAFT",
            RevisionReason = Required(request.RevisionReason, "RevisionReason"), PreparedByEmployeeId = actor,
            IdempotencyKey = key, ContentFingerprint = Fingerprint(request), CreatedBy = user.LoginId };
        foreach (var line in source.Lines.OrderBy(x => x.LineNumber))
        {
            // The new revision carries the approved values and their sources; the preparer re-prices
            // by typing or by accepting the current suggestion, never by silent refresh.
            revision.Lines.Add(new EstimatedBomLine { CompanyId = company.Id, EstimatedBomRevisionId = revision.Id,
                LineNumber = line.LineNumber, ItemId = line.ItemId, UomId = line.UomId, Quantity = line.Quantity,
                Remarks = line.Remarks, EstimatedUnitValue = line.EstimatedUnitValue, EstimatedUnitValueOverridden = line.EstimatedUnitValueOverridden,
                ValueSource = line.ValueSource, CreatedBy = user.LoginId });
        }
        bom.Revisions.Add(revision); db.EstimatedBomRevisions.Add(revision);
        bom.CurrentRevisionNumber = revision.RevisionNumber; bom.Status = "DRAFT"; bom.Version = checked(bom.Version + 1);
        bom.UpdatedAt = DateTimeOffset.UtcNow; bom.UpdatedBy = user.LoginId;
        AddHistory(bom, revision, "NewRevision", source.Status, "DRAFT", revision.RevisionReason, key);
        await CommitCommandAsync(company.Code, "EstimatedBom.NewRevision", key, new { bomNumber = bom.BomNumber, request }, bom, revision, ct);
        await tx.CommitAsync(ct); return await ViewAsync(bom, ct);
    }
}
