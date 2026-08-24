using SESS.NexaERP.Domain.Common;

namespace SESS.NexaERP.Domain.Audit;

public sealed class AuditLog : AuditableEntity
{
    public Guid? CompanyId { get; set; }
    public string Scope { get; set; } = "GLOBAL";
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string UserLoginId { get; set; } = string.Empty;
    public string Result { get; set; } = "Success";
    public string CorrelationId { get; set; } = string.Empty;
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string? IpAddress { get; set; }
}
