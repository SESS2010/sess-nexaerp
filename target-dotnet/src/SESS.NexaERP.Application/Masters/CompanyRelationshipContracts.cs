namespace SESS.NexaERP.Application.Masters;

public sealed record CustomerCompanyRelationshipDetail(
    Guid Id,
    Guid CompanyId,
    Guid CustomerId,
    string? CustomerAssignedSupplierCode,
    string RelationshipStatus,
    Guid? PaymentTermId,
    int? CreditPeriodDays,
    decimal? CreditLimit,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    Guid? ApprovedByEmployeeId,
    DateTimeOffset? ApprovedAt,
    bool IsActive,
    uint Version);

public sealed record UpsertCustomerCompanyRelationshipRequest(
    string? CustomerAssignedSupplierCode,
    string RelationshipStatus,
    Guid? PaymentTermId,
    int? CreditPeriodDays,
    decimal? CreditLimit,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    uint? Version);

public sealed record VendorCompanyRelationshipDetail(
    Guid Id,
    Guid CompanyId,
    Guid VendorId,
    string? VendorAssignedCustomerCode,
    string RelationshipStatus,
    Guid? PaymentTermId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    Guid? ApprovedByEmployeeId,
    DateTimeOffset? ApprovedAt,
    bool IsActive,
    uint Version);

public sealed record UpsertVendorCompanyRelationshipRequest(
    string? VendorAssignedCustomerCode,
    string RelationshipStatus,
    Guid? PaymentTermId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    uint? Version);
