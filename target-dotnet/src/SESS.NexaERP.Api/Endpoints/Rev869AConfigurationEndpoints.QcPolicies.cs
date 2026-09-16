using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public static partial class Rev869AConfigurationEndpoints
{
    private static Task<IResult> ApproveQcPolicy(Guid policyId, MasterActionRequest request, HttpContext http,
        NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct) =>
        DecideQcPolicy(policyId, request, true, http, db, user, audit, ct);

    private static Task<IResult> RejectQcPolicy(Guid policyId, MasterActionRequest request, HttpContext http,
        NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct) =>
        DecideQcPolicy(policyId, request, false, http, db, user, audit, ct);

    private static async Task<IResult> DecideQcPolicy(Guid policyId, MasterActionRequest request, bool approve,
        HttpContext http, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct)
    {
        _ = user.RequireRole(approve ? "approve" : "reject", "TECHNICAL_DIRECTOR");
        if (!user.EmployeeId.HasValue || string.IsNullOrWhiteSpace(user.OrganizationId)) return Results.Forbid();
        if (string.IsNullOrWhiteSpace(request.Remarks))
            return Results.BadRequest(new { message = "QC policy decision remarks are required." });
        if (!TryGetIdempotencyKey(http, out var key))
            return Results.BadRequest(new { message = "Idempotency-Key header is required." });
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Keep the command and immutable history in the same PostgreSQL transaction ID.
        db.Database.AutoSavepointsEnabled = false;
        var companyId = await db.Companies.Where(x => x.Code == user.OrganizationId && x.IsActive && x.Status == "ACTIVE")
            .Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
        if (!companyId.HasValue) return Results.Forbid();
        var policy = await db.QcInspectionPolicies.SingleOrDefaultAsync(x =>
            x.Id == policyId && x.CompanyId == companyId.Value && x.OrganizationId == user.OrganizationId, ct);
        if (policy is null) return Results.NotFound();
        if (policy.Version != request.Version) return Results.Conflict(new { message = "QC policy version is stale." });
        if (policy.ApprovalStatus != MasterApprovalStatuses.PendingApproval || !policy.IsActive)
            return Results.Conflict(new { message = "Only an active pending QC policy can be decided." });
        var creators = await db.EmployeeIdentityMappings.AsNoTracking()
            .Where(x => x.CompanyId == companyId.Value && x.Subject == policy.CreatedBy)
            .Select(x => x.EmployeeId).Distinct().Take(2).ToListAsync(ct);
        if (creators.Count != 1)
            return Results.Conflict(new { message = "QC policy creator identity is missing or ambiguous; administrator reconciliation is required." });
        if (creators[0] == user.EmployeeId)
            return Results.Conflict(new { message = "The policy preparer cannot decide their own policy." });
        var before = new { policy.ApprovalStatus, policy.IsActive, policy.Version };
        var action = approve ? "Approve" : "Reject";
        policy.ApprovalStatus = approve ? MasterApprovalStatuses.Approved : MasterApprovalStatuses.Rejected;
        policy.IsActive = approve;
        policy.Version = checked(policy.Version + 1);
        policy.UpdatedAt = DateTimeOffset.UtcNow;
        policy.UpdatedBy = user.LoginId;
        var after = new { policy.ApprovalStatus, policy.IsActive, policy.Version, DecisionEmployeeId = user.EmployeeId };
        AddHistory(db, policy.OrganizationId, nameof(QcInspectionPolicy), policy.Id, action, before, after,
            request.Remarks, user, policy.CompanyId);
        var history = db.ChangeTracker.Entries<ControlledConfigurationHistory>()
            .Single(x => x.State == EntityState.Added && x.Entity.EntityId == policyId).Entity;
        history.Version = policy.Version;
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(policy.OrganizationId,
            action + "QcPolicy", key, new { policyId, request });
        var attempt = await Rev869BCommandContextAuthorizer.OpenForDatabaseFunctionAsync(db, user,
            policy.OrganizationId, envelope, "qc_policy_history", nameof(QcInspectionPolicy), policyId,
            action, policy.Version, before.ApprovalStatus, policy.ApprovalStatus, history.CorrelationId, request.Remarks, ct);
        try
        {
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("QC", action + "InspectionPolicy", nameof(QcInspectionPolicy),
                policy.Id.ToString(), before, after, ct);
            await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct, after);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            return Results.Conflict(new { message = "QC policy version is stale." });
        }
        return Results.Ok(new { policy.Id, policy.ApprovalStatus, policy.IsActive, policy.Version });
    }
}
