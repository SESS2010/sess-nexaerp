namespace SESS.NexaERP.Application.Rev869A;

public sealed record CreateEmployeeIdentityMappingRequest(string OrganizationId, string Issuer, string Subject, string EmployeeCode, string IdentityType, DateOnly EffectiveFrom, DateOnly? EffectiveTo, string Remarks);
public sealed record CreateOperationalScopeRequest(string OrganizationId, string EmployeeCode, string? DepartmentCode, string? WarehouseCode, Guid? RackBinId, bool OwnRecordsOnly, bool AllowsPrivilegedCrossScope, DateOnly EffectiveFrom, DateOnly? EffectiveTo, string Remarks);
public sealed record CreateUomRequest(string Code, string Name, string MeasurementDimension);
public sealed record CreateUomConversionRequest(string OrganizationId, string FromUomCode, string ToUomCode, string MeasurementDimension, decimal ConversionFactor, DateOnly EffectiveFrom, DateOnly? EffectiveTo, string Remarks);
public sealed record CreateTaxGstSettingRequest(string OrganizationId, string JurisdictionCode, string HsnSacCode, string SupplierStateCode, string PlaceOfSupplyStateCode, string VendorRegistrationType, decimal GstRate, decimal CgstRate, decimal SgstRate, decimal IgstRate, decimal CessRate, bool IsExempt, bool IsReverseCharge, string CurrencyCode, int RoundingScale, DateOnly EffectiveFrom, DateOnly? EffectiveTo, string Remarks);
public sealed record CreateVendorQualificationRequest(string OrganizationId, string VendorCode, string? ItemCategoryCode, string QualificationCode, DateOnly EffectiveFrom, DateOnly? EffectiveTo, string Remarks);
public sealed record ChangeVendorQualificationLifecycleRequest(uint ExpectedVersion, string Remarks);
public sealed record CreateWarehouseConditionLocationRequest(string OrganizationId, string WarehouseCode, Guid RackBinId, string ConditionCode, DateOnly EffectiveFrom, DateOnly? EffectiveTo, string Remarks);
public sealed record CreateQcInspectionPolicyRequest(string OrganizationId, string? ItemCode, string? ItemCategoryCode, string ParameterCode, string MeasurementUomCode, decimal? LowerLimit, decimal? UpperLimit, string InspectionMethod, int SampleSize, DateOnly EffectiveFrom, DateOnly? EffectiveTo, string Remarks);
public sealed record ResolveCommercialValueRequest(string CurrencyCode, decimal TaxableValue, decimal TaxValue, decimal FreightAndOtherCharges, decimal DiscountValue, int RoundingScale);
