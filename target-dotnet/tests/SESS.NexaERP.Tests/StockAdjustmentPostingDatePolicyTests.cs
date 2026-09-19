using SESS.NexaERP.Domain.Stores;
namespace SESS.NexaERP.Tests;
public sealed class StockAdjustmentPostingDatePolicyTests
{
    private static readonly DateOnly Entry = new(2026, 9, 19);
    private static readonly DateOnly Start = new(2026, 9, 1);
    private static readonly DateOnly End = new(2026, 9, 30);

    [Theory]
    [InlineData(0, false)] [InlineData(1, false)] [InlineData(7, false)] [InlineData(8, true)]
    public void Seven_day_boundary_uses_server_entry_date(int days, bool extraTd)
    {
        var result = StockAdjustmentPostingDatePolicy.Resolve(Entry.AddDays(-days), Entry,
            Start, End, false, "Physical event retained", Guid.NewGuid());
        Assert.Equal(days, result.DaysBackdated);
        Assert.Equal(extraTd, result.RequiresTechnicalDirectorApproval);
    }
    [Fact]
    public void Closed_period_refuses_even_today_or_with_backdate_evidence()
    {
        Assert.Throws<InvalidOperationException>(() => StockAdjustmentPostingDatePolicy.Resolve(
            Entry, Entry, Start, End, true, "Correction", Guid.NewGuid()));
        Assert.Throws<InvalidOperationException>(() => StockAdjustmentPostingDatePolicy.Resolve(
            Entry.AddDays(-8), Entry, Start, End, true, "TD requested", Guid.NewGuid()));
    }
    [Theory]
    [InlineData(-20)] [InlineData(1)]
    public void Outside_period_or_future_date_cannot_be_posted(int offset)
        => Assert.Throws<InvalidOperationException>(() => StockAdjustmentPostingDatePolicy.Resolve(
            Entry.AddDays(offset), Entry, Start, End, false, "Evidence", Guid.NewGuid()));
    [Fact]
    public void Even_one_day_backdating_requires_both_reason_and_evidence()
    {
        Assert.Throws<InvalidOperationException>(() => StockAdjustmentPostingDatePolicy.Resolve(
            Entry.AddDays(-1), Entry, Start, End, false, "", Guid.NewGuid()));
        Assert.Throws<InvalidOperationException>(() => StockAdjustmentPostingDatePolicy.Resolve(
            Entry.AddDays(-1), Entry, Start, End, false, "Reason", null));
        Assert.Throws<InvalidOperationException>(() => StockAdjustmentPostingDatePolicy.Resolve(
            Entry.AddDays(-1), Entry, Start, End, false, "Reason", Guid.Empty));
    }
    [Fact]
    public void Current_period_correction_does_not_require_reopening_original_closed_period()
    {
        var current = StockAdjustmentPostingDatePolicy.Resolve(Entry, Entry, Start, End, false, null, null);
        Assert.False(current.RequiresTechnicalDirectorApproval);
        Assert.Equal(0, current.DaysBackdated);
        // Original-event reference is retained by the correction document; its old
        // physical event date must not be substituted for the current posting date.
    }
    [Fact]
    public void Extra_backdate_authority_preserves_md_and_accounts_requirements()
    {
        var recorder = Guid.NewGuid();
        var high = StockAdjustmentApprovalSnapshot.Capture([new(-1, 100001, false)], recorder, [], false,
            requiresBackdateApproval: true);
        Assert.Equal(new[] { "MANAGING_DIRECTOR", "TECHNICAL_DIRECTOR" }, high.RequiredRoleCodes);
        var loss = StockAdjustmentApprovalSnapshot.Capture([new(-1, 200, false)], recorder, [], true,
            requiresBackdateApproval: true);
        Assert.Equal(new[] { "ACCOUNTS_MANAGER", "TECHNICAL_DIRECTOR" }, loss.RequiredRoleCodes);
        Assert.Throws<InvalidOperationException>(() => high.ValidateIndependentDecision(recorder, "TECHNICAL_DIRECTOR"));
    }
}
