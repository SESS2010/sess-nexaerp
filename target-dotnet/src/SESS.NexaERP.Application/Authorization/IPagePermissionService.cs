namespace SESS.NexaERP.Application.Authorization;

public interface IPagePermissionService
{
    Task<bool> HasPermissionAsync(string roleCode, string pageKey, string permission, CancellationToken cancellationToken);
}

public static class PagePermissionActions
{
    public const string View = "view";
    public const string Create = "create";
    public const string Update = "update";
    public const string Submit = "submit";
    public const string Issue = "issue";
    public const string Verify = "verify";
    public const string Approve = "approve";
    public const string Reject = "reject";
    public const string RequestClarification = "request-clarification";
    public const string RequestRevision = "request-revision";
    public const string Resubmit = "resubmit";
    public const string Cancel = "cancel";
    public const string Deactivate = "deactivate";
    public const string Print = "print";
    public const string Download = "download";
    public const string Export = "export";
    public const string UploadAttachment = "upload-attachment";
    public const string ReplaceAttachment = "replace-attachment";
    public const string ViewCommercialValues = "view-commercial-values";
    public const string ViewAuditHistory = "view-audit-history";
    public const string FullControl = "full-control";
}
