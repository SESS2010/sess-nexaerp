namespace SESS.NexaERP.Domain.Stores;

public enum ToolCustodyKind { Permanent, Temporary }
public enum ToolCustodyClosure { Returned, LossWrittenOff }
public sealed record ToolIdentity(Guid AssetId, string AssetCode, string ToolName);
public sealed record ToolCustodyEvent(string Action, DateOnly On, Guid ActorEmployeeId,
    ToolIdentity Tool, Guid HolderEmployeeId, string Reason, ToolIdentity? Replacement = null,
    Guid? SuccessorEmployeeId = null, int? AdditionalDays = null,
    decimal? DepreciatedValue = null, Guid? ValuationEvidenceId = null,
    Guid? ApprovalEvidenceId = null);

/// <summary>
/// Individual custody invariants only. The application must resolve company scope,
/// roles, employee acceptance, approval and valuation evidence from persisted records,
/// and lock both assets before replacing one. This model does not grant authority.
/// </summary>
public sealed class ToolCustody
{
    private readonly List<ToolCustodyEvent> events = [];
    private ToolCustody(Guid id, Guid companyId, ToolIdentity tool, Guid holder,
        ToolCustodyKind kind, DateOnly issuedOn, int? durationDays, Guid storesEmployee)
    {
        Id = RequireId(id); CompanyId = RequireId(companyId);
        Tool = ValidateTool(tool); HolderEmployeeId = RequireId(holder);
        RequireId(storesEmployee);
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (kind == ToolCustodyKind.Temporary && durationDays is not > 0)
            throw new ArgumentException("Temporary issue requires a positive duration in days.");
        if (kind == ToolCustodyKind.Permanent && durationDays is not null)
            throw new ArgumentException("Permanent custody has no duration or due date.");
        Kind = kind; IssuedOn = issuedOn; OriginalDurationDays = durationDays;
        DueOn = durationDays is { } days ? issuedOn.AddDays(days) : null;
        events.Add(new("ISSUED", issuedOn, storesEmployee, tool, holder, "Individual asset accepted by holder."));
    }
    public Guid Id { get; }
    public Guid CompanyId { get; }
    public ToolIdentity Tool { get; private set; }
    public Guid HolderEmployeeId { get; private set; }
    public ToolCustodyKind Kind { get; }
    public DateOnly IssuedOn { get; }
    public int? OriginalDurationDays { get; }
    public DateOnly? DueOn { get; private set; }
    public ToolCustodyClosure? Closure { get; private set; }
    public bool IsOpen => Closure is null;
    public IReadOnlyList<ToolCustodyEvent> Events => events.AsReadOnly();

    // Acceptance identity comes from a retained employee action, not an API selector.
    public static ToolCustody IssueAccepted(Guid id, Guid companyId, ToolIdentity tool,
        Guid holder, Guid acceptedBy, ToolCustodyKind kind, DateOnly issuedOn,
        int? durationDays, Guid storesEmployee)
    {
        if (holder != acceptedBy) throw new InvalidOperationException("The named holder must accept the tool.");
        return new(id, companyId, tool, holder, kind, issuedOn, durationDays, storesEmployee);
    }
    public bool IsOverdue(DateOnly asOf) => IsOpen && DueOn is { } due && asOf > due;

    public void Extend(DateOnly on, int additionalDays, Guid storesEmployee, string reason)
    {
        ValidateAction(on, storesEmployee, reason);
        if (Kind != ToolCustodyKind.Temporary || DueOn is null)
            throw new InvalidOperationException("Only temporary custody can be extended.");
        if (additionalDays <= 0) throw new ArgumentOutOfRangeException(nameof(additionalDays));
        var nextDueOn = DueOn.Value.AddDays(additionalDays);
        events.Add(new("EXTENDED", on, storesEmployee, Tool, HolderEmployeeId, reason.Trim(), AdditionalDays: additionalDays));
        DueOn = nextDueOn;
    }
    public void TransferAccepted(DateOnly on, Guid releasedBy, Guid successor,
        Guid acceptedBy, string reason)
    {
        ValidateAction(on, releasedBy, reason); RequireId(successor);
        if (releasedBy != HolderEmployeeId || acceptedBy != successor || successor == HolderEmployeeId)
            throw new InvalidOperationException("Transfer requires the current holder and a different accepting successor.");
        events.Add(new("TRANSFERRED", on, releasedBy, Tool, HolderEmployeeId, reason.Trim(), SuccessorEmployeeId: successor));
        HolderEmployeeId = successor;
    }
    public void ReplaceDamagedAccepted(DateOnly on, ToolIdentity replacement,
        Guid acceptedBy, Guid storesEmployee, string reason)
    {
        ValidateAction(on, storesEmployee, reason); ValidateTool(replacement);
        if (acceptedBy != HolderEmployeeId) throw new InvalidOperationException("The holder must accept the replacement.");
        if (replacement.AssetId == Tool.AssetId) throw new InvalidOperationException("A different asset must replace the damaged tool.");
        events.Add(new("DAMAGE_REPLACED", on, storesEmployee, Tool, HolderEmployeeId, reason.Trim(), Replacement: replacement));
        Tool = replacement;
        // Same custody, holder and due date. Damage does not close custody as a loss.
    }
    public void AcceptReturn(DateOnly on, Guid storesEmployee, string reason)
    {
        ValidateAction(on, storesEmployee, reason);
        events.Add(new("RETURNED", on, storesEmployee, Tool, HolderEmployeeId, reason.Trim()));
        Closure = ToolCustodyClosure.Returned;
    }
    public void WriteOffApprovedLoss(DateOnly on, Guid technicalDirector,
        decimal acceptedDepreciatedValue, Guid accountsValuationEvidence,
        Guid technicalDirectorApprovalEvidence, string reason)
    {
        ValidateAction(on, technicalDirector, reason);
        if (acceptedDepreciatedValue < 0) throw new ArgumentOutOfRangeException(nameof(acceptedDepreciatedValue));
        RequireId(accountsValuationEvidence); RequireId(technicalDirectorApprovalEvidence);
        events.Add(new("LOSS_WRITTEN_OFF", on, technicalDirector, Tool, HolderEmployeeId, reason.Trim(),
            DepreciatedValue: acceptedDepreciatedValue, ValuationEvidenceId: accountsValuationEvidence,
            ApprovalEvidenceId: technicalDirectorApprovalEvidence));
        Closure = ToolCustodyClosure.LossWrittenOff;
    }
    private void ValidateAction(DateOnly on, Guid employee, string reason)
    {
        if (!IsOpen) throw new InvalidOperationException("Closed custody cannot be changed.");
        RequireId(employee);
        if (on < events[^1].On) throw new ArgumentException("Custody history cannot be backdated.");
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A reason is required.");
    }
    private static Guid RequireId(Guid id) => id != Guid.Empty ? id : throw new ArgumentException("A retained identity is required.");
    private static ToolIdentity ValidateTool(ToolIdentity tool)
    {
        ArgumentNullException.ThrowIfNull(tool); RequireId(tool.AssetId);
        if (string.IsNullOrWhiteSpace(tool.AssetCode) || string.IsNullOrWhiteSpace(tool.ToolName))
            throw new ArgumentException("The individual asset code and tool name are required.");
        return tool;
    }
}
public sealed record OutstandingTool(Guid CustodyId, Guid CompanyId, ToolIdentity Tool,
    ToolCustodyKind Kind, DateOnly? DueOn);
public static class ToolResignationClearance
{
    // Supply all company custody records for the employee, not only a selected company.
    // Login disablement is independent of this business clearance decision.
    public static IReadOnlyList<OutstandingTool> Outstanding(Guid employee,
        IEnumerable<ToolCustody> allCompanyCustodies)
    {
        if (employee == Guid.Empty) throw new ArgumentException("An employee is required.");
        ArgumentNullException.ThrowIfNull(allCompanyCustodies);
        return Array.AsReadOnly(allCompanyCustodies.Where(x => x.IsOpen && x.HolderEmployeeId == employee)
            .Select(x => new OutstandingTool(x.Id, x.CompanyId, x.Tool, x.Kind, x.DueOn))
            .OrderBy(x => x.Tool.ToolName, StringComparer.Ordinal)
            .ThenBy(x => x.Tool.AssetCode, StringComparer.Ordinal).ToArray());
    }
}
