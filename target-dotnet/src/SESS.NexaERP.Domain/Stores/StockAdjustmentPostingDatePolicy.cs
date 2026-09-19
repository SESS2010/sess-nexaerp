namespace SESS.NexaERP.Domain.Stores;

public sealed record StockAdjustmentPostingDateRequirement(bool RequiresTechnicalDirectorApproval,
    int DaysBackdated);

/// <summary>
/// Entry date is the server's inventory-business date, never a client-selected date.
/// Period state and evidence identity must be resolved from retained records. This
/// policy cannot reopen a period or authorize a posting by itself.
/// </summary>
public static class StockAdjustmentPostingDatePolicy
{
    public static StockAdjustmentPostingDateRequirement Resolve(DateOnly effectiveDate,
        DateOnly serverEntryDate, DateOnly periodStart, DateOnly periodEnd, bool periodClosed,
        string? backdateReason, Guid? backdateEvidenceId)
    {
        if (periodEnd < periodStart) throw new ArgumentException("The inventory period range is invalid.");
        if (periodClosed) throw new InvalidOperationException("A closed inventory period cannot receive adjustments or be reopened.");
        if (effectiveDate < periodStart || effectiveDate > periodEnd)
            throw new InvalidOperationException("The effective date must be inside the selected open inventory period.");
        if (effectiveDate > serverEntryDate)
            throw new InvalidOperationException("An actual stock adjustment cannot be posted in the future.");
        var days = serverEntryDate.DayNumber - effectiveDate.DayNumber;
        if (days > 0 && (string.IsNullOrWhiteSpace(backdateReason)
            || backdateEvidenceId is null || backdateEvidenceId == Guid.Empty))
            throw new InvalidOperationException("Every backdate requires its reason and retained evidence.");
        return new(days > 7, days);
    }
}
