using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Identity;

namespace SESS.NexaERP.Domain.Authorization;

public sealed class RolePagePermission : AuditableEntity
{
    public Guid RoleId { get; set; }
    public Role? Role { get; set; }
    public Guid PageDefinitionId { get; set; }
    public PageDefinition? PageDefinition { get; set; }
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanSubmit { get; set; }
    public bool CanIssue { get; set; }
    public bool CanVerify { get; set; }
    public bool CanApprove { get; set; }
    public bool CanReject { get; set; }
    public bool CanRequestClarification { get; set; }
    public bool CanRequestRevision { get; set; }
    public bool CanResubmit { get; set; }
    public bool CanCancel { get; set; }
    public bool CanDeactivate { get; set; }
    public bool CanPrint { get; set; }
    public bool CanDownload { get; set; }
    public bool CanExport { get; set; }
    public bool CanUploadAttachment { get; set; }
    public bool CanReplaceAttachment { get; set; }
    public bool CanViewCommercialValues { get; set; }
    public bool CanViewAuditHistory { get; set; }
    public bool HasFullControl { get; set; }
}
