namespace SESS.NexaERP.Domain.Stores;

public sealed record StockAdjustmentApprovalRequirement(string ApproverRoleCode, bool AccountsConcurrenceRequired);

/// <summary>The approved stock-adjustment bands and their explicit exceptions.</summary>
public static class StockAdjustmentApprovalPolicy
{
    public static StockAdjustmentApprovalRequirement Resolve(
        decimal absoluteAdjustmentValue, bool changesSerializedIdentity, bool isWriteOff)
    {
        if (absoluteAdjustmentValue < 0)
            throw new ArgumentOutOfRangeException(nameof(absoluteAdjustmentValue),
                "Approval value must be the non-negative value of the adjustment.");
        if (isWriteOff)
            return new("TECHNICAL_DIRECTOR", true);
        if (changesSerializedIdentity)
            return new("TECHNICAL_DIRECTOR", false);
        return new(absoluteAdjustmentValue < 5000m ? "STORES_MANAGER"
            : absoluteAdjustmentValue <= 100000m ? "TECHNICAL_DIRECTOR" : "MANAGING_DIRECTOR", false);
    }
}
