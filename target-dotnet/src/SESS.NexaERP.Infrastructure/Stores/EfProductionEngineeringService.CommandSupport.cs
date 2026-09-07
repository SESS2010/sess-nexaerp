using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed partial class EfProductionEngineeringService
{
    private void History(ProductionBom? bom, ProductionBomRevision? bomRevision,
        EngineeringDocument? document, EngineeringDocumentRevision? documentRevision,
        string action, string? from, string to, string remarks, string key)
    {
        db.ProductionEngineeringHistories.Add(new() {
            CompanyId = bom?.CompanyId ?? document!.CompanyId,
            ProductionBomId = bom?.Id, ProductionBomRevisionId = bomRevision?.Id,
            EngineeringDocumentId = document?.Id, EngineeringDocumentRevisionId = documentRevision?.Id,
            Action = action, FromStatus = from, ToStatus = to, ActorEmployeeId = Actor(),
            ActorRoleCode = user.RoleCode, ResolvedRoleAssignmentId = user.ResolvedRoleAssignmentId!.Value,
            ResolvedRoleAssignmentType = user.ResolvedRoleAssignmentType!, CorrelationId = key,
            Remarks = remarks, CreatedBy = user.LoginId
        });
    }

    private async Task CommitAsync(string company, string operation, string key, object request,
        string entity, Guid entityId, object result, CancellationToken ct)
    {
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(company, operation, key, request);
        var attempt = await Rev869BCommandContextAuthorizer.OpenForPendingChangesAsync(
            db, user, company, envelope, ct, user.RoleCode)
            ?? throw new InvalidOperationException(operation + " produced no immutable operation slot.");
        await audit.WriteAsync("Production", operation, entity, entityId.ToString(), null, result, ct);
        await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct);
    }

    private async Task<string> NextNumberAsync(Guid companyId, string company, string prefix, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var year = today.Month >= 4 ? $"{today.Year % 100:00}-{(today.Year + 1) % 100:00}"
            : $"{(today.Year - 1) % 100:00}-{today.Year % 100:00}";
        var lockKey = $"NUMBER:{company}:{year}:{prefix}";
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey},0))", ct);
        var sequence = await db.PurchaseNumberSequences.SingleOrDefaultAsync(x =>
            x.CompanyId == companyId && x.OrganizationId == company && x.FinancialYear == year &&
            x.Prefix == prefix && x.IsActive, ct);
        if (sequence is null) {
            sequence = new PurchaseNumberSequence { CompanyId = companyId, OrganizationId = company,
                FinancialYear = year, Prefix = prefix, CreatedBy = user.LoginId };
            db.PurchaseNumberSequences.Add(sequence);
        }
        sequence.LastNumber++;
        return $"{prefix}-{company}-{year}-{sequence.LastNumber:000001}";
    }
}
