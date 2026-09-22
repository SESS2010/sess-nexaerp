namespace SESS.NexaERP.Application.Stores;

public sealed record UploadVendorBankAdviceRequest(
    Guid VendorId, string FileName, string ContentType, byte[] Content, string IdempotencyKey);

public sealed record VendorBankAdviceView(
    Guid Id, Guid CompanyId, Guid VendorId, string VendorCode, string VendorName,
    string FileName, string ContentType, long SizeBytes, string ContentSha256,
    string EvidenceObjectKey, DateTimeOffset CreatedAt, bool Replayed);

public sealed record VendorBankAdviceContent(
    string FileName, string ContentType, byte[] Content, string ContentSha256);

public interface IVendorBankAdviceService
{
    Task<VendorBankAdviceView> UploadAsync(UploadVendorBankAdviceRequest request, CancellationToken ct);
    Task<VendorBankAdviceView> GetAsync(Guid id, CancellationToken ct);
    Task<VendorBankAdviceContent> DownloadAsync(Guid id, CancellationToken ct);
}

public static class VendorBankAdviceLimits
{
    public const int MaximumBytes = 5 * 1024 * 1024;
}
