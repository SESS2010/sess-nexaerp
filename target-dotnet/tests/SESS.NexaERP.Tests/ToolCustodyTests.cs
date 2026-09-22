using SESS.NexaERP.Domain.Stores;
namespace SESS.NexaERP.Tests;
public sealed class ToolCustodyTests
{
    private static readonly DateOnly Day = new(2026, 9, 19);
    private readonly Guid holder = Guid.NewGuid();
    private readonly Guid stores = Guid.NewGuid();
    private ToolCustody Issue(ToolCustodyKind kind = ToolCustodyKind.Permanent, int? days = null,
        Guid? company = null, string code = "TOOL-001") => ToolCustody.IssueAccepted(Guid.NewGuid(),
        company ?? Guid.NewGuid(), new(Guid.NewGuid(), code, "Torque wrench"), holder, holder, kind, Day, days, stores);

    [Fact]
    public void Eight_tools_are_eight_named_custodies_and_all_block_clearance()
    {
        var records = Enumerable.Range(1, 8).Select(x => Issue(code: $"TOOL-{x:000}")).ToArray();
        var outstanding = ToolResignationClearance.Outstanding(holder, records);
        Assert.Equal(8, outstanding.Count);
        Assert.Equal(8, outstanding.Select(x => x.CustodyId).Distinct().Count());
        Assert.Equal(8, outstanding.Select(x => x.Tool.AssetId).Distinct().Count());
        Assert.All(outstanding, x => Assert.Equal("Torque wrench", x.Tool.ToolName));
        records[0].AcceptReturn(Day, stores, "Returned to Stores");
        Assert.Equal(7, ToolResignationClearance.Outstanding(holder, records).Count);
    }
    [Fact]
    public void Clearance_includes_permanent_and_temporary_custody_across_companies()
    {
        var permanent = Issue(); var temporary = Issue(ToolCustodyKind.Temporary, 7);
        Assert.NotEqual(permanent.CompanyId, temporary.CompanyId);
        Assert.Equal(2, ToolResignationClearance.Outstanding(holder, [permanent, temporary]).Count);
        Assert.Empty(ToolResignationClearance.Outstanding(Guid.NewGuid(), [permanent, temporary]));
    }
    [Fact]
    public void Temporary_issue_uses_days_and_preserves_original_duration_when_extended()
    {
        var record = Issue(ToolCustodyKind.Temporary, 7);
        Assert.Equal(Day.AddDays(7), record.DueOn);
        Assert.False(record.IsOverdue(Day.AddDays(7)));
        Assert.True(record.IsOverdue(Day.AddDays(8)));
        record.Extend(Day.AddDays(8), 3, stores, "Job continued");
        Assert.Equal(7, record.OriginalDurationDays);
        Assert.Equal(Day.AddDays(10), record.DueOn);
        Assert.False(record.IsOverdue(Day.AddDays(8)));
        Assert.Equal(3, record.Events[^1].AdditionalDays);
        Assert.Equal("Job continued", record.Events[^1].Reason);
    }
    [Theory]
    [InlineData(null)] [InlineData(0)] [InlineData(-1)]
    public void Temporary_issue_rejects_missing_or_nonpositive_duration(int? days)
        => Assert.Throws<ArgumentException>(() => Issue(ToolCustodyKind.Temporary, days));
    [Fact]
    public void Permanent_custody_has_no_due_date_and_cannot_be_extended()
    {
        var record = Issue(); Assert.Null(record.DueOn);
        Assert.False(record.IsOverdue(Day.AddYears(10)));
        Assert.Throws<ArgumentException>(() => Issue(ToolCustodyKind.Permanent, 7));
        Assert.Throws<InvalidOperationException>(() => record.Extend(Day, 7, stores, "Reason"));
        Assert.Single(record.Events);
    }
    [Fact]
    public void Damage_replacement_keeps_custody_open_and_names_both_assets_in_history()
    {
        var record = Issue(ToolCustodyKind.Temporary, 7);
        var original = record.Tool; var custody = record.Id;
        var replacement = new ToolIdentity(Guid.NewGuid(), "TOOL-002", "Replacement torque wrench");
        record.ReplaceDamagedAccepted(Day.AddDays(1), replacement, holder, stores, "Ratchet damaged");
        Assert.Equal(custody, record.Id); Assert.Equal(holder, record.HolderEmployeeId);
        Assert.True(record.IsOpen); Assert.Null(record.Closure);
        Assert.Equal(Day.AddDays(7), record.DueOn);
        Assert.Equal(original, record.Events[0].Tool);
        Assert.Equal(original, record.Events[^1].Tool);
        Assert.Equal(replacement, record.Events[^1].Replacement);
        Assert.Null(record.Events[^1].DepreciatedValue);
        Assert.Equal(replacement, Assert.Single(ToolResignationClearance.Outstanding(holder, [record])).Tool);
    }
    [Fact]
    public void Unaccepted_replacement_or_same_asset_cannot_change_custody()
    {
        var record = Issue(); var original = record.Tool;
        Assert.Throws<InvalidOperationException>(() => record.ReplaceDamagedAccepted(Day,
            new(Guid.NewGuid(), "T2", "Wrench"), Guid.NewGuid(), stores, "Damage"));
        Assert.Throws<InvalidOperationException>(() => record.ReplaceDamagedAccepted(Day, original, holder, stores, "Damage"));
        Assert.Equal(original, record.Tool); Assert.Single(record.Events);
    }
    [Fact]
    public void Transfer_requires_both_parties_and_moves_clearance_to_successor()
    {
        var record = Issue(); var successor = Guid.NewGuid();
        Assert.Throws<InvalidOperationException>(() => record.TransferAccepted(Day, stores, successor, successor, "Handover"));
        Assert.Throws<InvalidOperationException>(() => record.TransferAccepted(Day, holder, successor, holder, "Handover"));
        Assert.Equal(holder, record.HolderEmployeeId); Assert.Single(record.Events);
        record.TransferAccepted(Day, holder, successor, successor, "Accepted handover");
        Assert.Empty(ToolResignationClearance.Outstanding(holder, [record]));
        Assert.Single(ToolResignationClearance.Outstanding(successor, [record]));
        Assert.Equal(holder, record.Events[^1].HolderEmployeeId);
        Assert.Equal(successor, record.Events[^1].SuccessorEmployeeId);
    }
    [Theory]
    [InlineData(0)] [InlineData(1250.50)]
    public void Loss_retains_accepted_depreciated_value_custodian_and_both_evidence_references(decimal value)
    {
        var record = Issue(); var valuation = Guid.NewGuid(); var approval = Guid.NewGuid();
        record.WriteOffApprovedLoss(Day, Guid.NewGuid(), value, valuation, approval, "Lost on site");
        Assert.Equal(ToolCustodyClosure.LossWrittenOff, record.Closure);
        var loss = record.Events[^1]; Assert.Equal("LOSS_WRITTEN_OFF", loss.Action);
        Assert.Equal(value, loss.DepreciatedValue); Assert.Equal(holder, loss.HolderEmployeeId);
        Assert.Equal(valuation, loss.ValuationEvidenceId); Assert.Equal(approval, loss.ApprovalEvidenceId);
        Assert.Empty(ToolResignationClearance.Outstanding(holder, [record]));
        Assert.Throws<InvalidOperationException>(() => record.AcceptReturn(Day, stores, "Second closure"));
    }
    [Fact]
    public void Loss_without_retained_valuation_or_approval_cannot_close_custody()
    {
        var record = Issue(); var td = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => record.WriteOffApprovedLoss(Day, td, 20, Guid.Empty, Guid.NewGuid(), "Lost"));
        Assert.Throws<ArgumentException>(() => record.WriteOffApprovedLoss(Day, td, 20, Guid.NewGuid(), Guid.Empty, "Lost"));
        Assert.Throws<ArgumentOutOfRangeException>(() => record.WriteOffApprovedLoss(Day, td, -1, Guid.NewGuid(), Guid.NewGuid(), "Lost"));
        Assert.True(record.IsOpen); Assert.Single(record.Events);
    }
    [Fact]
    public void Returned_temporary_tool_is_not_overdue_and_cannot_be_transferred()
    {
        var record = Issue(ToolCustodyKind.Temporary, 7);
        record.AcceptReturn(Day.AddDays(1), stores, "Finished job");
        Assert.False(record.IsOverdue(Day.AddDays(30)));
        var successor = Guid.NewGuid();
        Assert.Throws<InvalidOperationException>(() => record.TransferAccepted(Day.AddDays(2), holder, successor, successor, "Late handover"));
        Assert.Equal(2, record.Events.Count);
    }
    [Fact]
    public void Action_refusals_do_not_append_history_or_change_due_date()
    {
        var record = Issue(ToolCustodyKind.Temporary, 7);
        Assert.Throws<ArgumentException>(() => record.Extend(Day.AddDays(-1), 1, stores, "Backdated"));
        Assert.Throws<ArgumentException>(() => record.Extend(Day, 1, stores, " "));
        Assert.Throws<ArgumentOutOfRangeException>(() => record.Extend(Day, 0, stores, "No extension"));
        Assert.Single(record.Events); Assert.Equal(Day.AddDays(7), record.DueOn);
    }
}
