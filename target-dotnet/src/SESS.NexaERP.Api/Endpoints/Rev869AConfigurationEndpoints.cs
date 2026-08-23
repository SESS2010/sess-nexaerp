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
        group.MapPost("/vendor-qualifications/{qualificationId:guid}/normalize-legacy", NormalizeLegacyVendorQualification).RequirePagePermission("masters.vendor-qualifications", PagePermissionActions.Verify);
        group.MapPost("/vendor-qualifications/{qualificationId:guid}/verify", VerifyVendorQualification).RequirePagePermission("masters.vendor-qualifications", PagePermissionActions.Verify);
        group.MapPost("/vendor-qualifications/{qualificationId:guid}/approve", ApproveVendorQualification).RequirePagePermission("masters.vendor-qualifications", PagePermissionActions.Approve);
        group.MapPost("/vendor-qualifications/{qualificationId:guid}/reject", RejectVendorQualification).RequirePagePermission("masters.vendor-qualifications", PagePermissionActions.Approve);
        group.MapPost("/vendor-qualifications/{qualificationId:guid}/request-correction", RequestVendorQualificationCorrection).RequirePagePermission("masters.vendor-qualifications", PagePermissionActions.Approve);
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
        if (await db.EmployeeIdentityMappings.AnyAsync(x => x.IsActive && ((x.Issuer == issuer && x.Subject == subject) || (x.OrganizationId == request.OrganizationId.Trim() && x.EmployeeId == employee.Id && x.IdentityType == IdentityTypes.Human)), ct)) return Results.Conflict(new { message = "Active issuer/subject or employee identity mapping already exists." });
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
        var organization = request.OrganizationId.Trim();
        var overlap = await db.EmployeeOperationalScopes.AnyAsync(x => x.OrganizationId == organization && x.EmployeeId == employee.Id && x.DepartmentId == departmentId && x.WarehouseId == warehouseId && x.RackBinId == request.RackBinId && x.OwnRecordsOnly == request.OwnRecordsOnly && x.IsActive && x.EffectiveFrom <= (request.EffectiveTo ?? DateOnly.MaxValue) && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= request.EffectiveFrom), ct);
        if (overlap) return Results.Conflict(new { message = "An overlapping operational scope exists." });
        var entity = new EmployeeOperationalScope { OrganizationId = organization, EmployeeId = employee.Id, DepartmentId = departmentId, WarehouseId = warehouseId, RackBinId = request.RackBinId, OwnRecordsOnly = request.OwnRecordsOnly, AllowsPrivilegedCrossScope = request.AllowsPrivilegedCrossScope, EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo, Remarks = request.Remarks.Trim(), CreatedBy = user.LoginId };
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
        var organization = request.OrganizationId.Trim();
        var jurisdiction = MasterEndpointHelpers.NormalizeCode(request.JurisdictionCode);
        var hsn = MasterEndpointHelpers.NormalizeCode(request.HsnSacCode);
        var supplierState = MasterEndpointHelpers.NormalizeCode(request.SupplierStateCode);
        var placeOfSupplyState = MasterEndpointHelpers.NormalizeCode(request.PlaceOfSupplyStateCode);
        if (!VendorRegistrationTypes.TryParseCanonical(request.VendorRegistrationType, out var registrationType))
            return Results.BadRequest(new { message = "Vendor registration type must be one of the exact supported values." });
        var registration = registrationType.ToCanonicalValue();
        var supply = TaxGstSetting.ResolveSupplyType(supplierState, placeOfSupplyState);
        var candidate = new TaxGstSetting { OrganizationId = organization, JurisdictionCode = jurisdiction, HsnSacCode = hsn, SupplierStateCode = supplierState, PlaceOfSupplyStateCode = placeOfSupplyState, SupplyType = supply, VendorRegistrationType = registration, GstRate = request.GstRate, CgstRate = request.CgstRate, SgstRate = request.SgstRate, IgstRate = request.IgstRate, CessRate = request.CessRate, IsExempt = request.IsExempt, IsReverseCharge = request.IsReverseCharge, CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant(), RoundingScale = request.RoundingScale, EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo, ApprovalStatus = MasterApprovalStatuses.PendingApproval, CreatedBy = user.LoginId };
        if (!rates.All(TaxGstSetting.IsValidRate) || !TaxGstSetting.IsValidRange(request.EffectiveFrom, request.EffectiveTo) || request.RoundingScale is < 0 or > 6 || candidate.CurrencyCode.Length != 3 || string.IsNullOrWhiteSpace(supplierState) || string.IsNullOrWhiteSpace(placeOfSupplyState) || !candidate.HasValidIndiaComponentSplit()) return Results.BadRequest(new { message = "Invalid tax rate, state-based GST component split, effective range, ISO currency code or rounding scale." });
        var overlap = await db.TaxGstSettings.AnyAsync(x => x.OrganizationId == organization && x.JurisdictionCode == jurisdiction && x.HsnSacCode == hsn && x.SupplierStateCode == supplierState && x.PlaceOfSupplyStateCode == placeOfSupplyState && x.VendorRegistrationType == registration && x.IsActive && x.EffectiveFrom <= (request.EffectiveTo ?? DateOnly.MaxValue) && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= request.EffectiveFrom), ct);
        if (overlap) return Results.Conflict(new { message = "An overlapping effective tax rule exists." });
        db.TaxGstSettings.Add(candidate); AddHistory(db, candidate.OrganizationId, nameof(TaxGstSetting), candidate.Id, "CreateVersion", null, candidate, request.Remarks, user);
        await db.SaveChangesAsync(ct); await audit.WriteAsync("Settings", "CreateTaxGstSetting", nameof(TaxGstSetting), candidate.Id.ToString(), null, candidate, ct);
        return Results.Created($"/api/v1/rev869a/configuration/tax-gst/{candidate.Id}", new { candidate.Id });
    }

    private static async Task<IResult> CreateVendorQualification(CreateVendorQualificationRequest request, HttpContext http, NexaErpDbContext db, ICurrentUser user, IRecordScopeAuthorizer scopes, IAuditWriter audit, CancellationToken ct)
    {
        if (!user.IsAuthenticated || !user.EmployeeId.HasValue || string.IsNullOrWhiteSpace(user.OrganizationId))
            return Results.Unauthorized();
        var organization = request.OrganizationId.Trim();
        if (!string.Equals(user.OrganizationId, organization, StringComparison.Ordinal))
            return Results.NotFound();
        if (string.IsNullOrWhiteSpace(request.Remarks))
            return Results.BadRequest(new { message = "Qualification creation remarks are required." });
        var scope = await scopes.AuthorizeAnyAsync(user.EmployeeId.Value, user.RoleCode, organization, DateOnly.FromDateTime(DateTime.UtcNow), ct);
        if (!scope.Allowed)
        {
            await audit.WriteAsync("Security", "Denied", nameof(VendorQualification), organization, null, new { scope.Reason, user.RoleCode }, ct);
            return Results.Forbid();
        }
        if (!TryGetIdempotencyKey(http, out var idempotencyKey)) return Results.BadRequest(new { message = "Idempotency-Key header is required." });
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var vendor = await db.Vendors.SingleOrDefaultAsync(x => x.VendorCode == MasterEndpointHelpers.NormalizeCode(request.VendorCode), ct);
        if (vendor is null) return Results.Conflict(new { message = "Vendor was not found." });
        Guid? categoryId = null;
        if (!string.IsNullOrWhiteSpace(request.ItemCategoryCode)) categoryId = await db.ItemCategories.Where(x => x.Code == MasterEndpointHelpers.NormalizeCode(request.ItemCategoryCode) && x.IsActive).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
        if (!string.IsNullOrWhiteSpace(request.ItemCategoryCode) && !categoryId.HasValue) return Results.Conflict(new { message = "Active item category was not found." });
        var qualificationCode = MasterEndpointHelpers.NormalizeCode(request.QualificationCode);
        if (request.EffectiveTo < request.EffectiveFrom) return Results.BadRequest(new { message = "Invalid vendor qualification effective range." });
        // Retained actorless REV869A rows stay readable and immutable, but every overlapping
        // effective range is blocked. Only a non-overlapping controlled replacement is allowed.
        var overlap = await db.VendorQualifications.AnyAsync(x => x.OrganizationId == organization && x.VendorId == vendor.Id && x.ItemCategoryId == categoryId && x.QualificationCode == qualificationCode && x.IsActive && x.EffectiveFrom <= (request.EffectiveTo ?? DateOnly.MaxValue) && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= request.EffectiveFrom), ct);
        if (overlap) return Results.Conflict(new { message = "An overlapping vendor qualification exists." });
        var entity = new VendorQualification { OrganizationId = organization, VendorId = vendor.Id, ItemCategoryId = categoryId, QualificationCode = qualificationCode, EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo, VerificationStatus = MasterApprovalStatuses.PendingApproval, ApprovalStatus = MasterApprovalStatuses.PendingApproval, CreatedBy = user.LoginId };
        db.VendorQualifications.Add(entity);
        db.ControlledConfigurationHistories.Add(new ControlledConfigurationHistory
        {
            OrganizationId = entity.OrganizationId,
            EntityType = nameof(VendorQualification),
            EntityId = entity.Id,
            Action = "Create",
            AfterJson = JsonSerializer.Serialize(entity),
            ActorLoginId = user.LoginId,
            ActorRoleCode = user.RoleCode,
            Remarks = request.Remarks.Trim(),
            CorrelationId = $"REV869B|QUALIFICATION|{entity.Id:N}|0|CREATE",
            CreatedBy = user.LoginId,
            Version = 0
        });
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(organization, "CreateVendorQualification", idempotencyKey, request);
        var attempt = await Rev869BCommandContextAuthorizer.OpenForPendingChangesAsync(db, user, organization, envelope, ct)
            ?? throw new InvalidOperationException("The controlled change did not produce an exact command attempt.");
        try
        {
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("Masters", "CreateVendorQualification", nameof(VendorQualification), entity.Id.ToString(), null, entity, ct);
            await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await RollbackCommandAttemptAsync(db, transaction, attempt, "BusinessTransactionRolledBack", ct);
            throw;
        }
        return Results.Created($"/api/v1/rev869a/configuration/vendor-qualifications/{entity.Id}", new { entity.Id });
    }

    private static Task<IResult> VerifyVendorQualification(Guid qualificationId, ChangeVendorQualificationLifecycleRequest request, HttpContext http, NexaErpDbContext db, ICurrentUser user, IRecordScopeAuthorizer scopes, IAuditWriter audit, CancellationToken ct) =>
        ChangeVendorQualificationLifecycle(qualificationId, request, "Verify", http, db, user, scopes, audit, ct);

    private static async Task<IResult> NormalizeLegacyVendorQualification(Guid qualificationId, ChangeVendorQualificationLifecycleRequest request, HttpContext http, NexaErpDbContext db, ICurrentUser user, IRecordScopeAuthorizer scopes, IAuditWriter audit, CancellationToken ct)
    {
        if (!user.IsAuthenticated || !user.EmployeeId.HasValue || string.IsNullOrWhiteSpace(user.OrganizationId))
            return Results.Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Remarks))
            return Results.BadRequest(new { message = "Legacy qualification normalization remarks are required." });

        var scope = await scopes.AuthorizeAnyAsync(user.EmployeeId.Value, user.RoleCode, user.OrganizationId,
            DateOnly.FromDateTime(DateTime.UtcNow), ct);
        if (!scope.Allowed)
        {
            await audit.WriteAsync("Security", "Denied", nameof(VendorQualification), qualificationId.ToString(), null,
                new { scope.Reason, user.RoleCode, operation = "NormalizeLegacy" }, ct);
            return Results.Forbid();
        }

        if (!TryGetIdempotencyKey(http, out var idempotencyKey)) return Results.BadRequest(new { message = "Idempotency-Key header is required." });
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var qualification = await db.VendorQualifications.SingleOrDefaultAsync(
            x => x.Id == qualificationId && x.OrganizationId == user.OrganizationId, ct);
        if (qualification is null) return Results.NotFound();
        if (qualification.Version != request.ExpectedVersion)
            return Results.Conflict(new { message = "Vendor qualification version is stale." });
        if (qualification.VerificationStatus != MasterApprovalStatuses.Draft ||
            qualification.ApprovalStatus != MasterApprovalStatuses.Draft ||
            qualification.VerifiedByEmployeeId.HasValue || qualification.ApprovedByEmployeeId.HasValue)
            return Results.Conflict(new { message = "Only a retained actorless Draft qualification can be normalized." });
        if (await db.EmployeeIdentityMappings.AsNoTracking().AnyAsync(x =>
            x.OrganizationId == qualification.OrganizationId && x.Subject == qualification.CreatedBy && x.IsActive, ct))
            return Results.Conflict(new { message = "Only a retained actorless Draft qualification can be normalized." });

        var before = new
        {
            qualification.VerificationStatus,
            qualification.VerifiedByEmployeeId,
            qualification.ApprovalStatus,
            qualification.ApprovedByEmployeeId,
            qualification.CreatedBy,
            qualification.Version
        };
        qualification.VerificationStatus = MasterApprovalStatuses.PendingApproval;
        qualification.ApprovalStatus = MasterApprovalStatuses.PendingApproval;
        qualification.CreatedBy = user.LoginId;
        qualification.Version = checked(qualification.Version + 1);
        qualification.UpdatedAt = DateTimeOffset.UtcNow;
        qualification.UpdatedBy = user.LoginId;
        var correlation = $"REV869B|QUALIFICATION|{qualification.Id:N}|{qualification.Version}|NORMALIZE";
        db.ControlledConfigurationHistories.Add(new ControlledConfigurationHistory
        {
            OrganizationId = qualification.OrganizationId,
            EntityType = nameof(VendorQualification),
            EntityId = qualification.Id,
            Action = "Normalize",
            BeforeJson = JsonSerializer.Serialize(before),
            AfterJson = JsonSerializer.Serialize(new
            {
                qualification.VerificationStatus,
                qualification.VerifiedByEmployeeId,
                qualification.ApprovalStatus,
                qualification.ApprovedByEmployeeId,
                qualification.CreatedBy,
                qualification.Version
            }),
            ActorLoginId = user.LoginId,
            ActorRoleCode = user.RoleCode,
            Remarks = request.Remarks.Trim(),
            CorrelationId = correlation,
            CreatedBy = user.LoginId,
            Version = qualification.Version
        });
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(user.OrganizationId, "NormalizeVendorQualification", idempotencyKey, new { qualificationId, request });
        var attempt = await Rev869BCommandContextAuthorizer.OpenForPendingChangesAsync(db, user, user.OrganizationId, envelope, ct)
            ?? throw new InvalidOperationException("The controlled change did not produce an exact command attempt.");
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            await RollbackCommandAttemptAsync(db, transaction, attempt, "IdempotentReplayOrExplicitRollback", ct);
            return Results.Conflict(new { message = "Vendor qualification version is stale." });
        }
        try
        {
            await audit.WriteAsync("Masters", "NormalizeLegacyVendorQualification", nameof(VendorQualification),
                qualification.Id.ToString(), before, qualification, ct);
            await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await RollbackCommandAttemptAsync(db, transaction, attempt, "BusinessTransactionRolledBack", ct);
            throw;
        }
        return Results.Ok(new { qualification.Id, qualification.VerificationStatus, qualification.ApprovalStatus, qualification.Version });
    }

    private static Task<IResult> ApproveVendorQualification(Guid qualificationId, ChangeVendorQualificationLifecycleRequest request, HttpContext http, NexaErpDbContext db, ICurrentUser user, IRecordScopeAuthorizer scopes, IAuditWriter audit, CancellationToken ct) =>
        ChangeVendorQualificationLifecycle(qualificationId, request, "Approve", http, db, user, scopes, audit, ct);

    private static Task<IResult> RejectVendorQualification(Guid qualificationId, ChangeVendorQualificationLifecycleRequest request, HttpContext http, NexaErpDbContext db, ICurrentUser user, IRecordScopeAuthorizer scopes, IAuditWriter audit, CancellationToken ct) =>
        ChangeVendorQualificationLifecycle(qualificationId, request, "Reject", http, db, user, scopes, audit, ct);

    private static Task<IResult> RequestVendorQualificationCorrection(Guid qualificationId, ChangeVendorQualificationLifecycleRequest request, HttpContext http, NexaErpDbContext db, ICurrentUser user, IRecordScopeAuthorizer scopes, IAuditWriter audit, CancellationToken ct) =>
        ChangeVendorQualificationLifecycle(qualificationId, request, "RequestCorrection", http, db, user, scopes, audit, ct);

    private static async Task<IResult> ChangeVendorQualificationLifecycle(Guid qualificationId, ChangeVendorQualificationLifecycleRequest request, string action, HttpContext http, NexaErpDbContext db, ICurrentUser user, IRecordScopeAuthorizer scopes, IAuditWriter audit, CancellationToken ct)
    {
        if (!user.IsAuthenticated || !user.EmployeeId.HasValue || string.IsNullOrWhiteSpace(user.OrganizationId))
            return Results.Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Remarks))
            return Results.BadRequest(new { message = "Qualification lifecycle remarks are required." });

        if (!TryGetIdempotencyKey(http, out var idempotencyKey)) return Results.BadRequest(new { message = "Idempotency-Key header is required." });
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var qualification = await db.VendorQualifications.SingleOrDefaultAsync(x => x.Id == qualificationId && x.OrganizationId == user.OrganizationId, ct);
        if (qualification is null) return Results.NotFound();
        if (qualification.Version != request.ExpectedVersion)
            return Results.Conflict(new { message = "Vendor qualification version is stale." });

        var creatorEmployeeId = await db.EmployeeIdentityMappings.AsNoTracking()
            .Where(x => x.OrganizationId == qualification.OrganizationId && x.Subject == qualification.CreatedBy && x.IsActive)
            .Select(x => (Guid?)x.EmployeeId).SingleOrDefaultAsync(ct);
        if (!creatorEmployeeId.HasValue || creatorEmployeeId == user.EmployeeId)
            return Results.Conflict(new { message = "Qualification creator cannot verify or approve the same qualification." });
        var scope = await scopes.AuthorizeAsync(user.EmployeeId.Value, user.RoleCode,
            new RecordScopeTarget(qualification.OrganizationId, null, null, null, creatorEmployeeId),
            DateOnly.FromDateTime(DateTime.UtcNow), ct);
        if (!scope.Allowed)
        {
            await audit.WriteAsync("Security", "Denied", nameof(VendorQualification), qualification.Id.ToString(), null,
                new { scope.Reason, user.RoleCode }, ct);
            return Results.Forbid();
        }

        var before = new
        {
            qualification.VerificationStatus,
            qualification.VerifiedByEmployeeId,
            qualification.ApprovalStatus,
            qualification.ApprovedByEmployeeId,
            qualification.IsActive,
            qualification.Version
        };
        if (action == "Verify")
        {
            if (qualification.VerificationStatus != MasterApprovalStatuses.PendingApproval ||
                qualification.ApprovalStatus != MasterApprovalStatuses.PendingApproval || qualification.VerifiedByEmployeeId.HasValue)
                return Results.Conflict(new { message = "Qualification is not awaiting independent verification." });
            qualification.VerificationStatus = MasterApprovalStatuses.Verified;
            qualification.VerifiedByEmployeeId = user.EmployeeId.Value;
        }
        else if (action == "Approve")
        {
            if (qualification.VerificationStatus != MasterApprovalStatuses.Verified || !qualification.VerifiedByEmployeeId.HasValue ||
                qualification.ApprovalStatus != MasterApprovalStatuses.PendingApproval || qualification.ApprovedByEmployeeId.HasValue)
                return Results.Conflict(new { message = "Qualification requires completed independent verification before approval." });
            if (qualification.VerifiedByEmployeeId == user.EmployeeId)
                return Results.Conflict(new { message = "Qualification verifier cannot approve the same qualification." });
            qualification.ApprovalStatus = MasterApprovalStatuses.Approved;
            qualification.ApprovedByEmployeeId = user.EmployeeId.Value;
        }
        else if (action == "Reject")
        {
            var pendingVerification = qualification.VerificationStatus == MasterApprovalStatuses.PendingApproval &&
                qualification.ApprovalStatus == MasterApprovalStatuses.PendingApproval && !qualification.VerifiedByEmployeeId.HasValue;
            var pendingApproval = qualification.VerificationStatus == MasterApprovalStatuses.Verified &&
                qualification.ApprovalStatus == MasterApprovalStatuses.PendingApproval && qualification.VerifiedByEmployeeId.HasValue;
            if ((!pendingVerification && !pendingApproval) || qualification.ApprovedByEmployeeId.HasValue ||
                qualification.VerifiedByEmployeeId == user.EmployeeId)
                return Results.Conflict(new { message = "Qualification can only be rejected by an independent decision actor while pending." });
            qualification.ApprovalStatus = MasterApprovalStatuses.Rejected;
            qualification.ApprovedByEmployeeId = user.EmployeeId.Value;
            qualification.IsActive = false;
        }
        else if (action == "RequestCorrection")
        {
            if ((qualification.VerificationStatus != MasterApprovalStatuses.Verified && qualification.VerificationStatus != MasterApprovalStatuses.Approved) ||
                qualification.ApprovalStatus != MasterApprovalStatuses.Approved || !qualification.VerifiedByEmployeeId.HasValue ||
                !qualification.ApprovedByEmployeeId.HasValue || !qualification.IsActive || qualification.VerifiedByEmployeeId == user.EmployeeId)
                return Results.Conflict(new { message = "Only an active independently verified and approved qualification can be sent for correction." });
            qualification.ApprovalStatus = MasterApprovalStatuses.RevisionRequested;
            qualification.IsActive = false;
        }
        else
            throw new InvalidOperationException("Unsupported qualification lifecycle action.");

        qualification.Version = checked(qualification.Version + 1);
        qualification.UpdatedAt = DateTimeOffset.UtcNow;
        qualification.UpdatedBy = user.LoginId;
        var correlation = $"REV869B|QUALIFICATION|{qualification.Id:N}|{qualification.Version}|{action.ToUpperInvariant()}";
        db.ControlledConfigurationHistories.Add(new ControlledConfigurationHistory
        {
            OrganizationId = qualification.OrganizationId,
            EntityType = nameof(VendorQualification),
            EntityId = qualification.Id,
            Action = action,
            BeforeJson = JsonSerializer.Serialize(before),
            AfterJson = JsonSerializer.Serialize(new
            {
                qualification.VerificationStatus,
                qualification.VerifiedByEmployeeId,
                qualification.ApprovalStatus,
                qualification.ApprovedByEmployeeId,
                qualification.IsActive,
                qualification.Version
            }),
            ActorLoginId = user.LoginId,
            ActorRoleCode = user.RoleCode,
            Remarks = request.Remarks.Trim(),
            CorrelationId = correlation,
            CreatedBy = user.LoginId,
            Version = qualification.Version
        });
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(user.OrganizationId, action + "VendorQualification", idempotencyKey, new { qualificationId, request });
        var attempt = await Rev869BCommandContextAuthorizer.OpenForPendingChangesAsync(db, user, user.OrganizationId, envelope, ct)
            ?? throw new InvalidOperationException("The controlled change did not produce an exact command attempt.");
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            await RollbackCommandAttemptAsync(db, transaction, attempt, "IdempotentReplayOrExplicitRollback", ct);
            return Results.Conflict(new { message = "Vendor qualification version is stale." });
        }
        try
        {
            await audit.WriteAsync("Masters", action + "VendorQualification", nameof(VendorQualification), qualification.Id.ToString(), before, qualification, ct);
            await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await RollbackCommandAttemptAsync(db, transaction, attempt, "BusinessTransactionRolledBack", ct);
            throw;
        }
        return Results.Ok(new { qualification.Id, qualification.VerificationStatus, qualification.ApprovalStatus, qualification.Version });
    }

    private static async Task<IResult> CreateWarehouseConditionLocation(CreateWarehouseConditionLocationRequest request, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct)
    {
        var warehouse = await db.Warehouses.SingleOrDefaultAsync(x => x.WarehouseCode == MasterEndpointHelpers.NormalizeCode(request.WarehouseCode) && x.IsActive, ct);
        var rack = await db.RackBins.SingleOrDefaultAsync(x => x.Id == request.RackBinId && x.IsActive, ct);
        var condition = MasterEndpointHelpers.NormalizeCode(request.ConditionCode);
        if (warehouse is null || rack is null || rack.WarehouseId != warehouse.Id || !InventoryConditionCodes.All.Contains(condition, StringComparer.OrdinalIgnoreCase) || !string.Equals(MasterEndpointHelpers.NormalizeCode(rack.MaterialCondition), condition, StringComparison.Ordinal)) return Results.Conflict(new { message = "Warehouse/RackBin/condition mapping is invalid." });
        if (request.EffectiveTo < request.EffectiveFrom) return Results.BadRequest(new { message = "Invalid warehouse-condition effective range." });
        var organization = request.OrganizationId.Trim();
        var overlap = await db.WarehouseConditionLocations.AnyAsync(x => x.OrganizationId == organization && x.WarehouseId == warehouse.Id && x.RackBinId == rack.Id && x.ConditionCode == condition && x.IsActive && x.EffectiveFrom <= (request.EffectiveTo ?? DateOnly.MaxValue) && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= request.EffectiveFrom), ct);
        if (overlap) return Results.Conflict(new { message = "An overlapping warehouse/RackBin condition mapping exists." });
        var entity = new WarehouseConditionLocation { OrganizationId = organization, WarehouseId = warehouse.Id, RackBinId = rack.Id, ConditionCode = condition, EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo, CreatedBy = user.LoginId };
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
        if (request.EffectiveTo < request.EffectiveFrom) return Results.BadRequest(new { message = "Invalid QC policy effective range." });
        var organization = request.OrganizationId.Trim(); var parameter = MasterEndpointHelpers.NormalizeCode(request.ParameterCode);
        var overlap = await db.QcInspectionPolicies.AnyAsync(x => x.OrganizationId == organization && x.ItemId == itemId && x.ItemCategoryId == categoryId && x.ParameterCode == parameter && x.IsActive && x.EffectiveFrom <= (request.EffectiveTo ?? DateOnly.MaxValue) && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= request.EffectiveFrom), ct);
        if (overlap) return Results.Conflict(new { message = "An overlapping QC policy exists." });
        var entity = new QcInspectionPolicy { OrganizationId = organization, ItemId = itemId, ItemCategoryId = categoryId, ParameterCode = parameter, MeasurementUomId = uomId.Value, LowerLimit = request.LowerLimit, UpperLimit = request.UpperLimit, InspectionMethod = request.InspectionMethod.Trim(), SampleSize = request.SampleSize, EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo, ApprovalStatus = MasterApprovalStatuses.PendingApproval, CreatedBy = user.LoginId };
        db.QcInspectionPolicies.Add(entity); AddHistory(db, entity.OrganizationId, nameof(QcInspectionPolicy), entity.Id, "CreateVersion", null, entity, request.Remarks, user);
        await db.SaveChangesAsync(ct); await audit.WriteAsync("QC", "CreateInspectionPolicy", nameof(QcInspectionPolicy), entity.Id.ToString(), null, entity, ct);
        return Results.Created($"/api/v1/rev869a/configuration/qc-inspection-policies/{entity.Id}", new { entity.Id });
    }

    private static async Task RollbackCommandAttemptAsync(
        NexaErpDbContext db, Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        Rev869BCommandContextAuthorizer.CommandAttemptHandle attempt, string category, CancellationToken ct)
    {
        await transaction.RollbackAsync(ct);
        await Rev869BCommandContextAuthorizer.RecordNoncommitOutcomeAsync(
            (Npgsql.NpgsqlConnection)db.Database.GetDbConnection(), attempt, "RolledBack", category, ct);
    }

    private static bool TryGetIdempotencyKey(HttpContext http, out string key)
    {
        key = http.Request.Headers["Idempotency-Key"].ToString().Trim();
        return key.Length is >= 8 and <= 200;
    }

    private static void AddHistory(NexaErpDbContext db, string organizationId, string type, Guid id, string action, object? before, object? after, string remarks, ICurrentUser user)
    {
        db.ControlledConfigurationHistories.Add(new ControlledConfigurationHistory { OrganizationId = organizationId, EntityType = type, EntityId = id, Action = action, BeforeJson = before is null ? null : JsonSerializer.Serialize(before), AfterJson = after is null ? null : JsonSerializer.Serialize(after), ActorLoginId = user.LoginId, ActorRoleCode = user.RoleCode, Remarks = remarks.Trim(), CorrelationId = $"REV869A_{action.ToUpperInvariant()}_{Guid.NewGuid():N}", CreatedBy = user.LoginId });
    }
}
