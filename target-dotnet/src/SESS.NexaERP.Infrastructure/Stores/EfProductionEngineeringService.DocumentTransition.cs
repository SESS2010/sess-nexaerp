using Microsoft.EntityFrameworkCore;
using System.Data;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed partial class EfProductionEngineeringService
{
    public async Task<EngineeringDocumentView> SubmitEngineeringDocumentAsync(
        string number, EngineeringDocumentActionRequest request, CancellationToken ct)
    {
        await RequireDesignPreparerAsync(ct);
        return await TransitionDocumentAsync(number, request, false, ct);
    }

    public async Task<EngineeringDocumentView> ApproveEngineeringDocumentAsync(
        string number, EngineeringDocumentActionRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("approve", "TECHNICAL_DIRECTOR");
        return await TransitionDocumentAsync(number, request, true, ct);
    }

    private async Task<EngineeringDocumentView> TransitionDocumentAsync(
        string number, EngineeringDocumentActionRequest request, bool approve, CancellationToken ct)
    {
        var key = Required(request.IdempotencyKey, "IdempotencyKey");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct);
        var document = await DocumentQuery().SingleOrDefaultAsync(
            x => x.CompanyId == company.Id && x.DocumentNumber == Code(number), ct)
            ?? throw new KeyNotFoundException("Engineering document was not found.");
        var revision = document.Revisions.OrderByDescending(x => x.RevisionNumber).First();
        var expected = approve ? "SUBMITTED" : "DRAFT";
        if (revision.Status != expected)
            throw new StoresConflictException("Drawing revision is not in the required state.");
        if (revision.Version != request.ExpectedVersion)
            throw new DbUpdateConcurrencyException("Drawing revision Version is stale.");
        if (approve && revision.DrawnByEmployeeId == Actor())
            throw new StoresConflictException("Nobody may approve a drawing they prepared.");
        var from = revision.Status;
        revision.Status = approve ? "APPROVED" : "SUBMITTED";
        document.Status = revision.Status;
        revision.Version = checked(revision.Version + 1); document.Version = checked(document.Version + 1);
        if (approve) {
            revision.ApprovedAt = DateTimeOffset.UtcNow;
            revision.ApprovedByEmployeeId = Actor();
            if (revision.SupersedesRevisionId.HasValue) {
                var prior = document.Revisions.Single(x => x.Id == revision.SupersedesRevisionId.Value);
                prior.Status = "SUPERSEDED"; prior.Version = checked(prior.Version + 1);
            }
        } else revision.SubmittedAt = DateTimeOffset.UtcNow;
        var action = approve ? "Approve" : "Submit";
        History(null, null, document, revision, action, from, revision.Status,
            Required(request.Remarks, "Remarks"), key);
        await CommitAsync(company.Code, "EngineeringDocument." + action, key, request,
            "EngineeringDocument", document.Id,
            new { document.DocumentNumber, revision.RevisionCode }, ct);
        await tx.CommitAsync(ct); return DocumentView(document);
    }
}
