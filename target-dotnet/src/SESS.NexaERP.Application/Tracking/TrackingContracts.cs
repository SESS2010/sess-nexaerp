namespace SESS.NexaERP.Application.Tracking;

/// <summary>One open item of tracking-lite (R1 contract: docs/installation/R1-tracking-lite-frontend-contract.md).</summary>
public sealed record TrackingPendingRow(string DocType, string Queue, Guid DocumentId, string Number, string Status,
    string? PendingWithRole, string? PendingWithRoleName, string? PendingWithEmployeeCode, string? PendingWithEmployeeName,
    DateTimeOffset WaitingSince, int AgeDays, int OverdueAfterDays, bool IsOverdue, string Link);

/// <summary>An employee who receives a role digest: holds one of the roles in the company and has an official e-mail.</summary>
public sealed record DigestRecipient(Guid EmployeeId, string EmployeeCode, string EmployeeName, string Email, IReadOnlyList<string> RoleCodes);

/// <summary>
/// Tracking-lite for a system job with no signed-in user (the TD's 09:00 digest): every pending row of the company
/// whose "pending with" role is one of the given roles, with no user scope, overdue first.
/// </summary>
public interface ITrackingDigestQuery
{
    Task<IReadOnlyList<TrackingPendingRow>> PendingForRolesAsync(Guid companyId, IReadOnlyCollection<string> roleCodes, CancellationToken ct);
    Task<IReadOnlyList<DigestRecipient>> RecipientsAsync(Guid companyId, IReadOnlyCollection<string> roleCodes, CancellationToken ct);
}

public sealed record TrackingPendingRequest(string? DocType, string? Queue, bool OverdueOnly, bool Mine, int Page, int PageSize);

public sealed record TrackingPendingPage(long Total, int Page, int PageSize, DateTimeOffset GeneratedAt, string TimeZone,
    IReadOnlyList<TrackingPendingRow> Items);

public sealed record TrackingSummaryTile(string DocType, string Queue, string Title, string State,
    int? Count, int? OverdueCount, int? OldestAgeDays)
{
    public string? Link => State == "READY" ? TrackingDocumentLinks.Queue(Queue) : null;
}

public sealed record TrackingEvent(DateTimeOffset At, string Stage, string Action, string? FromStatus, string? ToStatus,
    string? EmployeeCode, string? EmployeeName, string? LoginId, string? RoleCode, string? Remarks);

public sealed record TrackingHistory(string DocType, Guid DocumentId, string Number, string CurrentStatus,
    string? PendingWithRole, DateTimeOffset? WaitingSince, int? AgeDays, bool? IsOverdue, IReadOnlyList<TrackingEvent> Events)
{
    public string Link => TrackingDocumentLinks.Document(DocType, DocumentId, Number);
}

/// <summary>Tracking-lite for the signed-in user: what the dashboards let them see (R1 decision 11).</summary>
public interface ITrackingService
{
    Task<TrackingPendingPage> PendingAsync(TrackingPendingRequest request, CancellationToken ct);
    Task<IReadOnlyList<TrackingSummaryTile>> SummaryAsync(CancellationToken ct);
    /// <summary>Null when the document does not exist or is outside the user's scope (404).</summary>
    Task<TrackingHistory?> HistoryAsync(string docType, Guid documentId, CancellationToken ct);
}

public static class TrackingDocTypes
{
    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
        { "PR", "RFQ", "QUOTATION", "COMPARISON", "PO", "GATE_ENTRY", "GRN", "QC", "MIR", "VENDOR_BILL" };
}
