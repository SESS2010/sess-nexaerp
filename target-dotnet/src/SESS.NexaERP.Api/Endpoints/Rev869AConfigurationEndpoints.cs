using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Rev869A;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public static class Rev869AConfigurationEndpoints
{
    public static IEndpointRouteBuilder MapRev869AConfigurationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/rev869a/configuration").WithTags("REV869A Configuration").RequireAuthorization();
        group.MapGet("/policies", async (NexaErpDbContext db, CancellationToken ct) => Results.Ok(await db.OrganizationPolicies.AsNoTracking().OrderBy(x => x.OrganizationId).ThenBy(x => x.PolicyCode).ToListAsync(ct)))
            .RequirePagePermission("security.operational-scopes", PagePermissionActions.View);
        group.MapPost("/employee-identities", CreateIdentity).RequirePagePermission("security.employee-identities", PagePermissionActions.Create);
        group.MapPost("/operational-scopes", CreateScope).RequirePagePermission("security.operational-scopes", PagePermissionActions.Create);
        group.MapPost("/uoms", CreateUom).RequirePagePermission("masters.uoms", PagePermissionActions.Create);
        group.MapPost("/uom-conversions", CreateConversion).RequirePagePermission("masters.uom-conversions", PagePermissionActions.Create);
        group.MapPost("/tax-gst", CreateTax).RequirePagePermission("settings.tax-gst", PagePermissionActions.Create);
        group.MapPost("/commercial-values/preview", (ResolveCommercialValueRequest request) => Results.Ok(CommercialValueSnapshot.Calculate(request.CurrencyCode, request.TaxableValue, request.TaxValue, request.FreightAndOtherCharges, request.DiscountValue, request.RoundingScale)))
            .RequirePagePermission("settings.tax-gst", PagePermissionActions.ViewCommercialValues);
        group.MapPost("/vendor-qualifications", CreateVendorQualification).RequirePagePermission("masters.vendor-qualifications", PagePermissionActions.Create);
        group.MapPost("/warehouse-condition-locations", CreateWarehouseConditionLocation).RequirePagePermission("masters.warehouse-condition-locations", PagePermissionActions.Create);
        group.MapPost("/qc-inspection-policies", CreateQcPolicy).RequirePagePermission("qc.inspection-policies", PagePermissionActions.Create);
        return endpoints;
    }

    private static async Task<IResult> CreateIdentity(CreateEmployeeIdentityMappingRequest request, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct)
    {
        if (!string.Equals(request.IdentityType, IdentityTypes.Human, StringComparison.OrdinalIgnoreCase)) return Results.BadRequest(new { message = "REV869A creates employee-linked HUMAN identities only; shared human logins are prohibited." });
        if (string.IsNullOrWhiteSpace(request.Issuer) || string.IsNullOrWhiteSpace(request.Subject) || request.EffectiveTo < request.EffectiveFrom) return Results.BadRequest(new { message = "Issuer, subject and a valid effective range are required." });
        var employeeCode = MasterEndpointHelpers.NormalizeCode(request.EmployeeCode);
        var employee = await db.Employees.SingleOrDefaultAsync(x => x.EmployeeCode == employeeCode, ct);
        if (employee is null || !employee.LoginEnabled || !string.Equals(employee.Status, MasterStatuses.Active, StringComparison.OrdinalIgnoreCase)) return Results.Conflict(new { message = "Identity must map to one active login-enabled employee." });
        var issuer = EmployeeIdentityMapping.NormalizeIssuer(request.Issuer);
        var subject = EmployeeIdentityMapping.NormalizeSubject(request.Subject);
        if (await db.EmployeeIdentityMappings.AnyAsync(x => x.OrganizationId == request.OrganizationId.Trim() && x.IsActive && ((x.Issuer == issuer && x.Subject == subject) || (x.EmployeeId == employee.Id && x.IdentityType == IdentityTypes.Human)), ct)) return Results.Conflict(new { message = "Active issuer/subject or employee identity mapping already exists." });
        var entity = new EmployeeIdentityMapping { OrganizationId = request.OrganizationId.Trim(), Issuer = issuer, Subject = subject, EmployeeId = employee.Id, IdentityType = IdentityTypes.Human, EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo, CreatedBy = user.LoginId };
        db.EmployeeIdentityMappings.Add(entity);
        AddHistory(db, entity.OrganizationId, nameof(EmployeeIdentityMapping), entity.Id, "Create", null, new { entity.Issuer, subjectHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(entity.Subject))), employee.EmployeeCode, entity.EffectiveFrom, entity.EffectiveTo }, request.Remarks, user);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync("Security", "CreateIdentityMapping", nameof(EmployeeIdentityMapping), entity.Id.ToString(), null, new { entity.OrganizationId, entity.Issuer, employee.EmployeeCode }, ct);
        return Results.Created($"/api/v1/rev869a/configuration/employee-identities/{entity.Id}", new { entity.Id });
    }

    private static async Task<IResult> CreateScope(CreateOperationalScopeRequest request, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct)
    {
        if (request.EffectiveTo < request.EffectiveFrom) return Results.BadRequest(new { message = "Invalid scope effective range." });
        var employee = await db.Employees.SingleOrDefaultAsync(x => x.EmployeeCode == MasterEndpointHelpers.NormalizeCode(request.EmployeeCode) && x.Status == MasterStatuses.Active, ct);
        if (employee is null) return Results.Conflict(new { message = "Active employee was not found." });
        Guid? departmentId = null;
        if (!string.IsNullOrWhiteSpace(request.DepartmentCode)) departmentId = await db.Departments.Where(x => x.Code == MasterEndpointHelpers.NormalizeCode(request.DepartmentCode)).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
        Guid? warehouseId = null;
        if (!string.IsNullOrWhiteSpace(request.WarehouseCode)) warehouseId = await db.Warehouses.Where(x => x.WarehouseCode == MasterEndpointHelpers.NormalizeCode(request.WarehouseCode) && x.IsActive).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
        if (request.RackBinId.HasValue)
        {
            var rack = await db.RackBins.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.RackBinId && x.IsActive, ct);
            if (rack is null || warehouseId != rack.WarehouseId) return Results.Conflict(new { message = "Rack/Bin must be active and belong to the scoped warehouse." });
        }
        var entity = new EmployeeOperationalScope { OrganizationId = request.OrganizationId.Trim(), EmployeeId = employee.Id, DepartmentId = departmentId, WarehouseId = warehouseId, RackBinId = request.RackBinId, AllowsPrivilegedCrossScope = request.AllowsPrivilegedCrossScope, EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo, Remarks = request.Remarks.Trim(), CreatedBy = user.LoginId };
        db.EmployeeOperationalScopes.Add(entity);
        AddHistory(db, entity.OrganizationId, nameof(EmployeeOperationalScope), entity.Id, "Create", null, entity, request.Remarks, user);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync("Security", "CreateOperationalScope", nameof(EmployeeOperationalScope), entity.Id.ToString(), null, entity, ct);
        return Results.Created($"/api/v1/rev869a/configuration/operational-scopes/{entity.Id}", new { entity.Id });
    }

    private static async Task<IResult> CreateUom(CreateUomRequest request, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct)
    {
        var code = MasterEndpointHelpers.NormalizeCode(request.Code);
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.MeasurementDimension)) return Results.BadRequest(new { message = "UOM code, name and measurement dimension are required." });
        if (await db.Uoms.AnyAsync(x => x.Code == code, ct)) return Results.Conflict(new { message = "UOM code already exists." });
        var entity = new Uom { Code = code, Name = request.Name.Trim(), MeasurementDimension = MasterEndpointHelpers.NormalizeCode(request.MeasurementDimension), QuantityPrecision = 6, CreatedBy = user.LoginId };
        db.Uoms.Add(entity);
        AddHistory(db, string.Empty, nameof(Uom), entity.Id, "Create", null, entity, "UOM created", user);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync("Masters", "CreateUom", nameof(Uom), entity.Id.ToString(), null, entity, ct);
        return Results.Created($"/api/v1/rev869a/configuration/uoms/{entity.Id}", entity);
    }

    private static async Task<IResult> CreateConversion(CreateUomConversionRequest request, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct)
    {
        var from = await db.Uoms.SingleOrDefaultAsync(x => x.Code == MasterEndpointHelpers.NormalizeCode(request.FromUomCode) && x.IsActive, ct);
        var to = await db.Uoms.SingleOrDefaultAsync(x => x.Code == MasterEndpointHelpers.NormalizeCode(request.ToUomCode) && x.IsActive, ct);
        var dimension = MasterEndpointHelpers.NormalizeCode(request.MeasurementDimension);
        if (from is null || to is null || !UomConversion.IsValid(request.ConversionFactor, 6, from.Id, to.Id, dimension) || !string.Equals(from.MeasurementDimension, dimension, StringComparison.OrdinalIgnoreCase) || !string.Equals(to.MeasurementDimension, dimension, StringComparison.OrdinalIgnoreCase) || request.EffectiveTo < request.EffectiveFrom)
            return Results.BadRequest(new { message = "UOM conversion requires distinct active UOMs in one dimension, a factor greater than zero, six-decimal precision and a valid range." });
        var overlap = await db.UomConversions.AnyAsync(x => x.OrganizationId == request.OrganizationId.Trim() && x.FromUomId == from.Id && x.ToUomId == to.Id && x.IsActive && x.EffectiveFrom <= (request.EffectiveTo ?? DateOnly.MaxValue) && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= request.EffectiveFrom), ct);
        if (overlap) return Results.Conflict(new { message = "An overlapping UOM conversion version exists." });
        var entity = new UomConversion { OrganizationId = request.OrganizationId.Trim(), FromUomId = from.Id, ToUomId = to.Id, MeasurementDimension = dimension, ConversionFactor = request.ConversionFactor, QuantityPrecision = 6, EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo, ApprovalStatus = MasterApprovalStatuses.PendingApproval, CreatedBy = user.LoginId };
        db.UomConversions.Add(entity);
        AddHistory(db, entity.OrganizationId, nameof(UomConversion), entity.Id, "CreateVersion", null, entity, request.Remarks, user);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync("Masters", "CreateUomConversion", nameof(UomConversion), entity.Id.ToString(), null, entity, ct);
        return Results.Created($"/api/v1/rev869a/configuration/uom-conversions/{entity.Id}", new { entity.Id });
    }

    private static async Task<IResult> CreateTax(CreateTaxGstSettingRequest request, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct)
    {
        var rates = new[] { request.GstRate, request.CgstRate, request.SgstRate, request.IgstRate, request.CessRate };
        if (!rates.All(TaxGstSetting.IsValidRate) || !TaxGstSetting.IsValidRange(request.EffectiveFrom, request.EffectiveTo) || request.RoundingScale is < 0 or > 6 || request.CurrencyCode.Trim().Length != 3) return Results.BadRequest(new { message = "Invalid tax rate, effective range, ISO currency code or rounding scale." });
        var organization = request.OrganizationId.Trim(); var jurisdiction = MasterEndpointHelpers.NormalizeCode(request.JurisdictionCode); var hsn = MasterEndpointHelpers.NormalizeCode(request.HsnSacCode); var supply = MasterEndpointHelpers.NormalizeCode(request.SupplyType); var registration = MasterEndpointHelpers.NormalizeCode(request.VendorRegistrationType);
        var overlap = await db.TaxGstSettings.AnyAsync(x => x.OrganizationId == organization && x.JurisdictionCode == jurisdiction && x.HsnSacCode == hsn && x.SupplyType == supply && x.VendorRegistrationType == registration && x.IsActive && x.EffectiveFrom <= (request.EffectiveTo ?? DateOnly.MaxValue) && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= request.EffectiveFrom), ct);
        if (overlap) return Results.Conflict(new { message = "An overlapping effective tax rule exists." });
        var entity = new TaxGstSetting { OrganizationId = organization, JurisdictionCode = jurisdiction, HsnSacCode = hsn, SupplyType = supply, VendorRegistrationType = registration, GstRate = request.GstRate, CgstRate = request.CgstRate, SgstRate = request.SgstRate, IgstRate = request.IgstRate, CessRate = request.CessRate, IsExempt = request.IsExempt, IsReverseCharge = request.IsReverseCharge, CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant(), RoundingScale = request.RoundingScale, EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo, ApprovalStatus = MasterApprovalStatuses.PendingApproval, CreatedBy = user.LoginId };
        db.TaxGstSettings.Add(entity); AddHistory(db, entity.OrganizationId, nameof(TaxGstSetting), entity.Id, "CreateVersion", null, entity, request.Remarks, user);
        await db.SaveChangesAsync(ct); await audit.WriteAsync("Settings", "CreateTaxGstSetting", nameof(TaxGstSetting), entity.Id.ToString(), null, entity, ct);
        return Results.Created($"/api/v1/rev869a/configuration/tax-gst/{entity.Id}", new { entity.Id });
    }

    private static async Task<IResult> CreateVendorQualification(CreateVendorQualificationRequest request, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct)
    {
        var vendor = await db.Vendors.SingleOrDefaultAsync(x => x.VendorCode == MasterEndpointHelpers.NormalizeCode(request.VendorCode), ct);
        if (vendor is null) return Results.Conflict(new { message = "Vendor was not found." });
        Guid? categoryId = null;
        if (!string.IsNullOrWhiteSpace(request.ItemCategoryCode)) categoryId = await db.ItemCategories.Where(x => x.Code == MasterEndpointHelpers.NormalizeCode(request.ItemCategoryCode) && x.IsActive).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
        if (!string.IsNullOrWhiteSpace(request.ItemCategoryCode) && !categoryId.HasValue) return Results.Conflict(new { message = "Active item category was not found." });
        var entity = new VendorQualification { OrganizationId = request.OrganizationId.Trim(), VendorId = vendor.Id, ItemCategoryId = categoryId, QualificationCode = MasterEndpointHelpers.NormalizeCode(request.QualificationCode), EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo, VerificationStatus = MasterApprovalStatuses.PendingApproval, ApprovalStatus = MasterApprovalStatuses.PendingApproval, CreatedBy = user.LoginId };
        db.VendorQualifications.Add(entity); AddHistory(db, entity.OrganizationId, nameof(VendorQualification), entity.Id, "Create", null, entity, request.Remarks, user);
        await db.SaveChangesAsync(ct); await audit.WriteAsync("Masters", "CreateVendorQualification", nameof(VendorQualification), entity.Id.ToString(), null, entity, ct);
        return Results.Created($"/api/v1/rev869a/configuration/vendor-qualifications/{entity.Id}", new { entity.Id });
    }

    private static async Task<IResult> CreateWarehouseConditionLocation(CreateWarehouseConditionLocationRequest request, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct)
    {
        var warehouse = await db.Warehouses.SingleOrDefaultAsync(x => x.WarehouseCode == MasterEndpointHelpers.NormalizeCode(request.WarehouseCode) && x.IsActive, ct);
        var rack = await db.RackBins.SingleOrDefaultAsync(x => x.Id == request.RackBinId && x.IsActive, ct);
        var condition = MasterEndpointHelpers.NormalizeCode(request.ConditionCode);
        if (warehouse is null || rack is null || rack.WarehouseId != warehouse.Id || !InventoryConditionCodes.All.Contains(condition, StringComparer.OrdinalIgnoreCase) || !string.Equals(MasterEndpointHelpers.NormalizeCode(rack.MaterialCondition), condition, StringComparison.Ordinal)) return Results.Conflict(new { message = "Warehouse/RackBin/condition mapping is invalid." });
        var entity = new WarehouseConditionLocation { OrganizationId = request.OrganizationId.Trim(), WarehouseId = warehouse.Id, RackBinId = rack.Id, ConditionCode = condition, CreatedBy = user.LoginId };
        db.WarehouseConditionLocations.Add(entity); AddHistory(db, entity.OrganizationId, nameof(WarehouseConditionLocation), entity.Id, "Create", null, entity, request.Remarks, user);
        await db.SaveChangesAsync(ct); await audit.WriteAsync("Stores", "CreateWarehouseConditionLocation", nameof(WarehouseConditionLocation), entity.Id.ToString(), null, entity, ct);
        return Results.Created($"/api/v1/rev869a/configuration/warehouse-condition-locations/{entity.Id}", new { entity.Id, locationKey = StoreLocationKey.Derive(warehouse.Id, rack.Id) });
    }

    private static async Task<IResult> CreateQcPolicy(CreateQcInspectionPolicyRequest request, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ItemCode) == string.IsNullOrWhiteSpace(request.ItemCategoryCode) || request.SampleSize <= 0 || (request.LowerLimit.HasValue && request.UpperLimit.HasValue && request.UpperLimit < request.LowerLimit)) return Results.BadRequest(new { message = "QC policy requires exactly one item/category, valid limits and positive sample size." });
        Guid? itemId = null; Guid? categoryId = null;
        if (!string.IsNullOrWhiteSpace(request.ItemCode)) itemId = await db.Items.Where(x => x.ItemCode == MasterEndpointHelpers.NormalizeCode(request.ItemCode) && x.IsActive).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
        if (!string.IsNullOrWhiteSpace(request.ItemCategoryCode)) categoryId = await db.ItemCategories.Where(x => x.Code == MasterEndpointHelpers.NormalizeCode(request.ItemCategoryCode) && x.IsActive).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
        var uomId = await db.Uoms.Where(x => x.Code == MasterEndpointHelpers.NormalizeCode(request.MeasurementUomCode) && x.IsActive).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
        if ((!itemId.HasValue && !categoryId.HasValue) || !uomId.HasValue) return Results.Conflict(new { message = "Active QC item/category and measurement UOM are required." });
        var entity = new QcInspectionPolicy { OrganizationId = request.OrganizationId.Trim(), ItemId = itemId, ItemCategoryId = categoryId, ParameterCode = MasterEndpointHelpers.NormalizeCode(request.ParameterCode), MeasurementUomId = uomId.Value, LowerLimit = request.LowerLimit, UpperLimit = request.UpperLimit, InspectionMethod = request.InspectionMethod.Trim(), SampleSize = request.SampleSize, EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo, ApprovalStatus = MasterApprovalStatuses.PendingApproval, CreatedBy = user.LoginId };
        db.QcInspectionPolicies.Add(entity); AddHistory(db, entity.OrganizationId, nameof(QcInspectionPolicy), entity.Id, "CreateVersion", null, entity, request.Remarks, user);
        await db.SaveChangesAsync(ct); await audit.WriteAsync("QC", "CreateInspectionPolicy", nameof(QcInspectionPolicy), entity.Id.ToString(), null, entity, ct);
        return Results.Created($"/api/v1/rev869a/configuration/qc-inspection-policies/{entity.Id}", new { entity.Id });
    }

    private static void AddHistory(NexaErpDbContext db, string organizationId, string type, Guid id, string action, object? before, object? after, string remarks, ICurrentUser user)
    {
        db.ControlledConfigurationHistories.Add(new ControlledConfigurationHistory { OrganizationId = organizationId, EntityType = type, EntityId = id, Action = action, BeforeJson = before is null ? null : JsonSerializer.Serialize(before), AfterJson = after is null ? null : JsonSerializer.Serialize(after), ActorLoginId = user.LoginId, ActorRoleCode = user.RoleCode, Remarks = remarks.Trim(), CorrelationId = $"REV869A_{action.ToUpperInvariant()}_{Guid.NewGuid():N}", CreatedBy = user.LoginId });
    }
}
