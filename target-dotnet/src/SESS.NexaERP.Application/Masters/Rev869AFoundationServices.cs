using SESS.NexaERP.Domain.Masters;

namespace SESS.NexaERP.Application.Masters;

public interface IUomConversionService
{
    Task<UomConversion> GetApprovedAsync(Guid conversionId, DateOnly onDate, CancellationToken cancellationToken);
}

public sealed record TaxResolutionRequest(
    string OrganizationId,
    string JurisdictionCode,
    string HsnSacCode,
    string SupplierStateCode,
    string PlaceOfSupplyStateCode,
    string VendorRegistrationType,
    DateOnly TransactionDate,
    decimal TaxableValue);

public interface ITaxGstResolver
{
    Task<TaxGstSetting> ResolveAsync(TaxResolutionRequest request, CancellationToken cancellationToken);
}

public interface IVendorQualificationService
{
    Task<bool> IsEligibleAsync(Guid vendorId, string organizationId, Guid? itemCategoryId, DateOnly onDate, CancellationToken cancellationToken);
    Task<string> ResolveFinalApproverRoleAsync(string organizationId, DateOnly onDate, CancellationToken cancellationToken);
}