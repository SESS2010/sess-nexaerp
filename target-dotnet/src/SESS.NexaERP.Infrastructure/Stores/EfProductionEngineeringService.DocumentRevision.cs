using Microsoft.EntityFrameworkCore;
using System.Data;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed partial class EfProductionEngineeringService
{
    public async Task<EngineeringDocumentView> CreateEngineeringDocumentRevisionAsync(
        string number, NewEngineeringDocumentRevisionRequest request, CancellationToken ct)
    {
        await RequireDesignPreparerAsync(ct);
        var key = Required(request.IdempotencyKey, "IdempotencyKey");
        await ValidateDocumentRevisionAsync(request.Revision, ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct);
        var replay = await db.EngineeringDocumentRevisions.SingleOrDefaultAsync(
            x => x.CompanyId == company.Id && x.IdempotencyKey == key, ct);
        if (replay is not null) {
            if (replay.ContentFingerprint != Fingerprint(request))
                throw new StoresConflictException("Idempotency key was reused with different drawing content.");
            var old = await DocumentQuery().SingleAsync(x => x.Id == replay.EngineeringDocumentId, ct);
            await tx.CommitAsync(ct); return DocumentView(old);
        }
        var document = await DocumentQuery().SingleOrDefaultAsync(
            x => x.CompanyId == company.Id && x.DocumentNumber == Code(number), ct)
            ?? throw new KeyNotFoundException("Engineering document was not found.");
        if (document.Version != request.ExpectedDocumentVersion)
            throw new DbUpdateConcurrencyException("Engineering document Version is stale.");
        var prior = document.Revisions.OrderByDescending(x => x.RevisionNumber).First();
        if (prior.Status != "APPROVED")
            throw new StoresConflictException("A new drawing revision can only follow an Approved revision.");
        var revision = NewDocumentRevision(document, request.Revision, prior,
            prior.RevisionNumber + 1, key, Fingerprint(request));
        document.Revisions.Add(revision); document.CurrentRevisionId = revision.Id;
        document.Status = "DRAFT"; document.Version = checked(document.Version + 1);
        History(null, null, document, revision, "NewRevision", prior.Status, "DRAFT",
            revision.RevisionNote, key);
        await CommitAsync(company.Code, "EngineeringDocument.NewRevision", key, request,
            "EngineeringDocument", document.Id,
            new { document.DocumentNumber, revision.RevisionCode }, ct);
        await tx.CommitAsync(ct); return DocumentView(document);
    }
}
