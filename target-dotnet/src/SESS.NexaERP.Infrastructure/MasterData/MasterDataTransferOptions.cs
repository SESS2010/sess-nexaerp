namespace SESS.NexaERP.Infrastructure.MasterData;

public sealed class MasterDataTransferOptions
{
    public const string SectionName = "MasterDataTransfer";
    public int MaxRows { get; set; } = 1000;
    public long MaxFileBytes { get; set; } = 20 * 1024 * 1024;
    public long MaxExpandedBytes { get; set; } = 100 * 1024 * 1024;
    public int SensitiveRowRetentionDays { get; set; } = 90;
}
