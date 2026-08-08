using SESS.NexaERP.Domain.Common;

namespace SESS.NexaERP.Domain.Authorization;

public sealed class PageDefinition : AuditableEntity
{
    public string PageKey { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
