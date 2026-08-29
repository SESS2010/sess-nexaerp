namespace SESS.NexaERP.Application.Masters;

public sealed record ReferenceMasterSummary(Guid Id, string Code, string Name, bool IsActive, uint Version);

public sealed record ItemSubcategorySummary(Guid Id, Guid CategoryId, string CategoryCode, string CategoryName, string Code, string Name, bool IsActive, uint Version);

public sealed record UomSummary(Guid Id, string Code, string Name, string MeasurementDimension, int QuantityPrecision, bool IsActive, uint Version);

public sealed record UpsertReferenceMasterRequest(string Code, string Name, uint? Version);

public sealed record UpsertItemSubcategoryRequest(Guid CategoryId, string Code, string Name, uint? Version);

public sealed record UpsertUomMasterRequest(string Code, string Name, string MeasurementDimension, uint? Version, int? QuantityPrecision = null);

public sealed record DeactivateReferenceMasterRequest(string Reason, uint Version);
