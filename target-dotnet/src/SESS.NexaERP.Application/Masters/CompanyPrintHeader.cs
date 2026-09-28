namespace SESS.NexaERP.Application.Masters;

/// <summary>The company's legal identity as printed at the top of a PO or a DC (from R10's company profile).</summary>
public sealed record CompanyPrintHeader(string LegalName, string? TradeName, string Gstin, string Pan, string StateCode, string State,
    string AddressLine1, string? AddressLine2, string City, string PinCode, string? Phone, string? Email);
