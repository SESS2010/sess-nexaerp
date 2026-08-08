using SESS.NexaERP.Domain.Common;

namespace SESS.NexaERP.Domain.Employees;

public sealed class EmployeeImportHistory : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string ImportBatch { get; set; } = string.Empty;
    public string SourceEmployeeCode { get; set; } = string.Empty;
    public string SourceEmployeeName { get; set; } = string.Empty;
    public string NormalizedEmployeeName { get; set; } = string.Empty;
    public string SourceJson { get; set; } = string.Empty;
}