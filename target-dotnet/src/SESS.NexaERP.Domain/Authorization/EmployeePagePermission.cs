using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Employees;

namespace SESS.NexaERP.Domain.Authorization;

public sealed class EmployeePagePermission : CompanyScopedAuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid PageDefinitionId { get; set; }
    public PageDefinition? PageDefinition { get; set; }
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanSubmit { get; set; }
    public bool CanDownload { get; set; }
    public bool CanViewAuditHistory { get; set; }
}
