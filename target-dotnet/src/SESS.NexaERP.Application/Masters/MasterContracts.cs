namespace SESS.NexaERP.Application.Masters;

public sealed record CustomerSummary(Guid Id, string CustomerCode, string Name, string? GstNumber, string? PanNumber, bool IsActive);

public sealed record CreateCustomerRequest(string CustomerCode, string Name, string? GstNumber, string? PanNumber);

public sealed record VendorSummary(Guid Id, string VendorCode, string Name, string? GstNumber, string? PanNumber, string ApprovalStatus, bool IsActive);

public sealed record CreateVendorRequest(string VendorCode, string Name, string? GstNumber, string? PanNumber);
