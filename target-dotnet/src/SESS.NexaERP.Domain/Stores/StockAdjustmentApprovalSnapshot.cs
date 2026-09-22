namespace SESS.NexaERP.Domain.Stores;

public sealed record StockAdjustmentValuedChange(decimal QuantityChange, decimal AcceptedLineValue,
    bool ChangesSerializedIdentity);

/// <summary>
/// Approval facts captured from persisted quantities and accepted valuation evidence.
/// Callers must resolve values and actor authority from governed records before using this policy.
/// This does not authorize database posting or replace the runtime role checks.
/// </summary>
public sealed class StockAdjustmentApprovalSnapshot
{
    private StockAdjustmentApprovalSnapshot(decimal absoluteValue, IReadOnlyList<string> roles,
        IReadOnlyList<Guid> excludedEmployees)
    {
        AbsoluteValue = absoluteValue;
        RequiredRoleCodes = roles;
        ExcludedEmployeeIds = excludedEmployees;
    }

    public decimal AbsoluteValue { get; }
    public IReadOnlyList<string> RequiredRoleCodes { get; }
    public IReadOnlyList<Guid> ExcludedEmployeeIds { get; }

    public static StockAdjustmentApprovalSnapshot Capture(
        IEnumerable<StockAdjustmentValuedChange> persistedChanges, Guid recorderEmployeeId,
        IEnumerable<Guid> counterEmployeeIds, bool isWriteOff,
        StockAdjustmentApprovalSnapshot? originalForReversal = null,
        bool requiresBackdateApproval = false)
    {
        ArgumentNullException.ThrowIfNull(persistedChanges);
        ArgumentNullException.ThrowIfNull(counterEmployeeIds);
        if (recorderEmployeeId == Guid.Empty)
            throw new ArgumentException("The recorder must be identified.", nameof(recorderEmployeeId));
        var changes = persistedChanges.ToArray();
        if (changes.Length == 0 || changes.Any(x => x.AcceptedLineValue < 0
            || (x.QuantityChange == 0 && !x.ChangesSerializedIdentity)))
            throw new ArgumentException("Each adjustment needs a real quantity or serialized identity change and a non-negative accepted valuation.", nameof(persistedChanges));
        if (isWriteOff && changes.Any(x => x.QuantityChange >= 0))
            throw new ArgumentException("A write-off must remove stock, not add stock or only rename an identity.", nameof(persistedChanges));
        var excluded = counterEmployeeIds.Append(recorderEmployeeId).Distinct().ToArray();
        if (excluded.Contains(Guid.Empty))
            throw new ArgumentException("Every counter must be identified.", nameof(counterEmployeeIds));
        // Offsetting additions and removals must never reduce the authority band.
        // Use the accepted persisted line amount, not quantity times a rounded average rate.
        var absoluteValue = changes.Sum(x => x.AcceptedLineValue);
        var requirement = StockAdjustmentApprovalPolicy.Resolve(absoluteValue,
            changes.Any(x => x.ChangesSerializedIdentity), isWriteOff);
        var roles = new HashSet<string>(StringComparer.Ordinal) { requirement.ApproverRoleCode };
        if (requirement.AccountsConcurrenceRequired) roles.Add("ACCOUNTS_MANAGER");
        // Beyond seven days is additional TD authority, even in the MD value band.
        if (requiresBackdateApproval) roles.Add("TECHNICAL_DIRECTOR");
        // Reversing an event retains its authority, including any separate concurrence.
        if (originalForReversal is not null) roles.UnionWith(originalForReversal.RequiredRoleCodes);
        if (roles.Contains("TECHNICAL_DIRECTOR") || roles.Contains("MANAGING_DIRECTOR"))
            roles.Remove("STORES_MANAGER");
        return new(absoluteValue, Array.AsReadOnly(roles.Order(StringComparer.Ordinal).ToArray()),
            Array.AsReadOnly(excluded.Order().ToArray()));
    }

    public void ValidateIndependentDecision(Guid employeeId, string resolvedRoleCode)
    {
        if (employeeId == Guid.Empty || ExcludedEmployeeIds.Contains(employeeId))
            throw new InvalidOperationException("The recorder or a counter cannot approve this adjustment.");
        if (!RequiredRoleCodes.Contains(resolvedRoleCode, StringComparer.Ordinal))
            throw new InvalidOperationException("This resolved role is not an approver for the captured adjustment.");
    }
}
