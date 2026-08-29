using SESS.NexaERP.Domain.Common;

namespace SESS.NexaERP.Domain.Masters;

public sealed class MasterImportBatch : AuditableEntity
{
    public string MasterKey { get; set; } = string.Empty;
    public int TemplateVersion { get; set; }
    public Guid CompanyId { get; set; }
    public string ImportMode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string FileSha256 { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestFingerprint { get; set; } = string.Empty;
    public Guid UploadedByEmployeeId { get; set; }
    public string UploadedByEmployeeCode { get; set; } = string.Empty;
    public string OperationalRoleCode { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset RetentionExpiresAt { get; set; }
    public DateTimeOffset? SensitiveValuesPurgedAt { get; set; }
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }
    public int CreatedRows { get; set; }
    public int UpdatedRows { get; set; }
    public int UnchangedRows { get; set; }
    public int RejectedRows { get; set; }
    public int NotImportedRows { get; set; }
    public string? FailureSummary { get; set; }
    public Guid CorrelationId { get; set; }
    public List<MasterImportRowResult> RowResults { get; set; } = [];
}

public sealed class MasterImportRowResult : AuditableEntity
{
    public Guid ImportBatchId { get; set; }
    public MasterImportBatch? ImportBatch { get; set; }
    public int SourceRowNumber { get; set; }
    public string? BusinessCode { get; set; }
    public string? NormalizedBusinessCode { get; set; }
    public string? IntendedAction { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public string? SubmittedValuesJson { get; set; }
    public string ErrorsJson { get; set; } = "[]";
    public Guid? ResultRecordId { get; set; }
    public uint? ResultVersion { get; set; }
    public DateTimeOffset ProcessedAt { get; set; }
}
