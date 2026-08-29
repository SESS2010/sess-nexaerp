using SESS.NexaERP.Domain.Common;

namespace SESS.NexaERP.Domain.Masters;

public static class CustomerAttachmentKinds
{
    public const string BankLeaf = "BANK_LEAF";
    public const string GstCertificate = "GST_CERTIFICATE";
    public const string MsmeCertificate = "MSME_CERTIFICATE";
    public const string PanCard = "PAN_CARD";

    public static bool IsValid(string value) => value is BankLeaf or GstCertificate or MsmeCertificate or PanCard;
}

/// <summary>
/// Uploaded customer document (GST certificate, cancelled bank cheque leaf,
/// MSME certificate, PAN copy). Content is stored in the database; the
/// customer row references attachments through its AttachmentMetadataJson.
/// </summary>
public sealed class CustomerAttachment : AuditableEntity
{
    public string Kind { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public byte[] Content { get; set; } = [];
}
