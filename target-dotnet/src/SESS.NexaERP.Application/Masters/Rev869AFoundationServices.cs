using SESS.NexaERP.Domain.Masters;

namespace SESS.NexaERP.Application.Masters;

public interface IUomConversionService
{
    Task<UomConversion> GetApprovedAsync(Guid conversionId, DateOnly onDate, CancellationToken cancellationToken);
}

public interface ITaxGstResolver
{
    Task<TaxGstSetting> ResolveAsync(string organizationId, string jurisdictionCode, string hsnSacCode, string supplyType, string vendorRegistrationType, DateOnly onDate, CancellationToken cancellationToken);
}

public interface IVendorQualificationService
{
    Task<bool> IsEligibleAsync(Guid vendorId, string organizationId, Guid? itemCategoryId, DateOnly onDate, CancellationToken cancellationToken);
    Task<string> ResolveFinalApproverRoleAsync(string organizationId, DateOnly onDate, CancellationToken cancellationToken);
}
