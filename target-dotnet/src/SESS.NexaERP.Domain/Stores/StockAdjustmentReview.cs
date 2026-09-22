namespace SESS.NexaERP.Domain.Stores;

public sealed record StockAdjustmentReviewDecision(Guid RevisionId, Guid EmployeeId,
    string RoleCode, Guid RoleAssignmentId, DateTimeOffset DecidedAt, string Reason);

/// <summary>
/// Immutable review of one retained adjustment revision. A service must resolve actual
/// employee authority and scope, retain the revision and decisions, and lock/revalidate
/// the posting transaction. This model alone does not authorize stock or FIFO writes.
/// </summary>
public sealed class StockAdjustmentReview
{
    private readonly StockAdjustmentApprovalSnapshot snapshot;
    private readonly StockAdjustmentReviewDecision[] decisions;

    private StockAdjustmentReview(Guid revisionId, StockAdjustmentApprovalSnapshot snapshot,
        StockAdjustmentReviewDecision[] decisions)
    {
        RevisionId = revisionId;
        this.snapshot = snapshot;
        this.decisions = decisions;
    }

    public Guid RevisionId { get; }
    public IReadOnlyList<StockAdjustmentReviewDecision> Decisions => Array.AsReadOnly(decisions);
    public IReadOnlyList<string> OutstandingRoleCodes => Array.AsReadOnly(
        snapshot.RequiredRoleCodes.Except(decisions.Select(x => x.RoleCode), StringComparer.Ordinal).ToArray());
    public bool IsApproved => OutstandingRoleCodes.Count == 0;

    public static StockAdjustmentReview Begin(Guid retainedRevisionId, StockAdjustmentApprovalSnapshot snapshot)
    {
        if (retainedRevisionId == Guid.Empty)
            throw new ArgumentException("A retained adjustment revision is required.", nameof(retainedRevisionId));
        ArgumentNullException.ThrowIfNull(snapshot);
        return new(retainedRevisionId, snapshot, []);
    }

    public StockAdjustmentReview Approve(Guid expectedRevisionId, Guid resolvedEmployeeId,
        string resolvedRoleCode, Guid resolvedRoleAssignmentId, DateTimeOffset serverDecisionTime, string reason)
    {
        if (expectedRevisionId != RevisionId)
            throw new InvalidOperationException("The reviewed adjustment revision has changed.");
        if (resolvedRoleAssignmentId == Guid.Empty)
            throw new ArgumentException("The resolved role assignment must be retained.", nameof(resolvedRoleAssignmentId));
        if (serverDecisionTime == default || string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A decision requires its server time and reason.");
        snapshot.ValidateIndependentDecision(resolvedEmployeeId, resolvedRoleCode);
        if (decisions.Any(x => x.RoleCode == resolvedRoleCode))
            throw new InvalidOperationException("This revision already has a decision for that required role.");
        // Accounts concurrence is an independent decision, even where an employee holds both roles.
        if (decisions.Any(x => x.EmployeeId == resolvedEmployeeId))
            throw new InvalidOperationException("Separate required decisions need different employees.");
        return new(RevisionId, snapshot, [.. decisions, new(RevisionId, resolvedEmployeeId,
            resolvedRoleCode, resolvedRoleAssignmentId, serverDecisionTime, reason.Trim())]);
    }

    public void RequireApprovedRevision(Guid currentRetainedRevisionId)
    {
        if (currentRetainedRevisionId != RevisionId || !IsApproved)
            throw new InvalidOperationException("Every required independent decision must approve the current retained revision.");
    }
}
