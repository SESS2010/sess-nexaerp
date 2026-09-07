using Microsoft.EntityFrameworkCore;
using System.Data;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed partial class EfProductionEngineeringService
{
    public async Task<EngineeringDocumentView> CreateEngineeringDocumentAsync(
        CreateEngineeringDocumentRequest request, CancellationToken ct)
    {
        await RequireDesignPreparerAsync(ct);
        var key = Required(request.IdempotencyKey, "IdempotencyKey");
        await ValidateDocumentRevisionAsync(request.Revision, ct);
        var type = Code(request.DocumentType);
        if (type is not ("GA" or "PART"))
            throw new StoresValidationException("DocumentType must be GA or PART.");
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
        var job = await db.JobOrders.SingleOrDefaultAsync(
            x => x.CompanyId == company.Id && x.Id == request.JobOrderId, ct)
            ?? throw new KeyNotFoundException("Job Order was not found in the selected company.");
        var document = new EngineeringDocument { CompanyId = company.Id,
            DocumentNumber = await NextNumberAsync(company.Id, company.Code, type + "DRG", ct),
            DocumentType = type, JobOrderId = job.Id, Title = Required(request.Title, "Title"),
            CreatedBy = user.LoginId };
        var revision = NewDocumentRevision(document, request.Revision, null, 1, key, Fingerprint(request));
        document.Revisions.Add(revision); document.CurrentRevisionId = revision.Id;
        db.EngineeringDocuments.Add(document);
        History(null, null, document, revision, "Create", null, "DRAFT",
            revision.RevisionNote, key);
        await CommitAsync(company.Code, "EngineeringDocument.Create", key, request,
            nameof(EngineeringDocument), document.Id,
            new { document.DocumentNumber, revision.RevisionCode }, ct);
        await tx.CommitAsync(ct); return DocumentView(document);
    }

    private EngineeringDocumentRevision NewDocumentRevision(EngineeringDocument document,
        EngineeringDocumentRevisionInput input, EngineeringDocumentRevision? prior,
        int number, string key, string fingerprint) => new() {
            CompanyId = document.CompanyId, EngineeringDocumentId = document.Id,
            RevisionNumber = number, RevisionCode = Code(input.RevisionCode),
            SupersedesRevisionId = prior?.Id, DrawnByEmployeeId = input.DrawnByEmployeeId,
            CheckedByEmployeeId = input.CheckedByEmployeeId,
            RevisionNote = Required(input.RevisionNote, "RevisionNote"),
            DocumentDate = input.DocumentDate, StorageKey = Required(input.StorageKey, "StorageKey"),
            FileName = Required(input.FileName, "FileName"),
            ContentType = Required(input.ContentType, "ContentType"),
            SizeBytes = input.SizeBytes, Sha256 = Code(input.Sha256),
            IdempotencyKey = key, ContentFingerprint = fingerprint, CreatedBy = user.LoginId
        };
}
