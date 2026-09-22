namespace SESS.NexaERP.Domain.Masters;

public static class InputTaxCreditEligibility
{
    public const string FullyRecoverable = "FULLY_RECOVERABLE";
    public const string Blocked = "BLOCKED";
    public const string PartiallyRecoverable = "PARTIALLY_RECOVERABLE";

    public static decimal RecoveryPercent(string eligibility, decimal? recoverableTaxPercent) => eligibility switch
    {
        FullyRecoverable when recoverableTaxPercent is null => 100m,
        Blocked when recoverableTaxPercent is null => 0m,
        PartiallyRecoverable when recoverableTaxPercent is > 0m and < 100m &&
            decimal.Round(recoverableTaxPercent.Value, 6) == recoverableTaxPercent => recoverableTaxPercent.Value,
        _ => throw new InvalidOperationException("ITC eligibility must be FULLY_RECOVERABLE, BLOCKED or PARTIALLY_RECOVERABLE. Only partial recovery requires a percentage strictly between 0 and 100, with at most six decimals.")
    };
}
