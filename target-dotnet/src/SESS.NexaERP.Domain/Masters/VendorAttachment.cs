using SESS.NexaERP.Domain.Common;

namespace SESS.NexaERP.Domain.Masters;

public static class VendorAttachmentKinds
{
    public const string BankLeaf = "BANK_LEAF";
    public const string GstCertificate = "GST_CERTIFICATE";

    public static bool IsValid(string value) => value is BankLeaf or GstCertificate;
}

/// <summary>
/// Uploaded vendor document (cancelled bank cheque leaf, GST registration
/// certificate). Content is stored in the database; the vendor row references
/// attachments through its AttachmentMetadataJson.
/// </summary>
public sealed class VendorAttachment : AuditableEntity
{
    public string Kind { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public byte[] Content { get; set; } = [];
}
