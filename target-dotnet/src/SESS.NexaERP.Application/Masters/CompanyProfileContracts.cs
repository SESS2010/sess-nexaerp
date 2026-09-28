namespace SESS.NexaERP.Application.Masters;

/// <summary>R10: a company's legal identity, used by the GST state rule and by every print.</summary>
public sealed record CompanyProfileView(
    string CompanyCode, string LegalName, string? TradeName, string Gstin, string Pan, string StateCode, string State,
    string AddressLine1, string? AddressLine2, string City, string PinCode, string? Phone, string? Email,
    long Version, DateTimeOffset UpdatedAt, string UpdatedBy, bool IsComplete);

/// <summary>Saves the selected company's profile. Version is the current one (0 when there is none yet).</summary>
public sealed record SaveCompanyProfileRequest(
    string LegalName, string? TradeName, string Gstin, string Pan, string StateCode, string State,
    string AddressLine1, string? AddressLine2, string City, string PinCode, string? Phone, string? Email,
    long Version, string Reason);

public sealed record WarehouseStateCodeView(Guid WarehouseId, string WarehouseCode, string WarehouseName, string? StateCode, string EffectiveStateCode, long Version);

public sealed record SaveWarehouseStateCodeRequest(string StateCode, long Version, string Reason);

public interface ICompanyProfileService
{
    /// <summary>The selected company's profile; IsComplete is false, with empty fields, when none is saved.</summary>
    Task<CompanyProfileView> GetAsync(CancellationToken ct);
    Task<CompanyProfileView> SaveAsync(SaveCompanyProfileRequest request, CancellationToken ct);
    Task<IReadOnlyList<WarehouseStateCodeView>> ListWarehouseStatesAsync(CancellationToken ct);
    Task<WarehouseStateCodeView> SaveWarehouseStateAsync(string warehouseCode, SaveWarehouseStateCodeRequest request, CancellationToken ct);
}
