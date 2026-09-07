using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Rev869A;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public static partial class Rev869AConfigurationEndpoints
{
    private static async Task<IResult> ListEmployeeIdentities(string? employeeCode, bool? effectiveOnly, int? page, int? pageSize, NexaErpDbContext db, ICurrentUser user, CancellationToken ct)
    {
        var companyId = await CurrentCompanyId(db, user, ct); if (!companyId.HasValue) return Results.Forbid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow); var paging = MasterEndpointHelpers.NormalizePaging(page, pageSize);
        var query = db.EmployeeIdentityMappings.AsNoTracking().Where(x => x.CompanyId == companyId.Value);
        if (!string.IsNullOrWhiteSpace(employeeCode)) { var code = MasterEndpointHelpers.NormalizeCode(employeeCode); query = query.Where(x => x.Employee!.EmployeeCode == code); }
        if (effectiveOnly == true) query = query.Where(x => x.IsActive && x.EffectiveFrom <= today && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= today));
        var total = await query.CountAsync(ct);
        var stored = await query.OrderBy(x => x.Employee!.EmployeeCode).ThenBy(x => x.Issuer).ThenByDescending(x => x.EffectiveFrom)
            .Skip(paging.Skip).Take(paging.PageSize)
            .Select(x => new { x.Id, x.OrganizationId, x.Issuer, x.Subject, x.EmployeeId, EmployeeCode=x.Employee!.EmployeeCode, x.IdentityType, x.EffectiveFrom, x.EffectiveTo, x.IsActive, x.Version, Remarks=db.ControlledConfigurationHistories.Where(h=>h.EntityType==nameof(EmployeeIdentityMapping)&&h.EntityId==x.Id).OrderByDescending(h=>h.CreatedAt).Select(h=>h.Remarks).FirstOrDefault()??string.Empty }).ToListAsync(ct);
        var rows = stored.Select(x => new EmployeeIdentityMappingSummary(x.Id,x.OrganizationId,x.Issuer,
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(x.Subject))),x.EmployeeId,x.EmployeeCode,x.IdentityType,x.EffectiveFrom,x.EffectiveTo,x.IsActive,x.Remarks,x.Version)).ToList();
        return Results.Ok(new PagedResponse<EmployeeIdentityMappingSummary>(total,paging.PageNumber,paging.PageSize,rows));
    }

    private static async Task<IResult> ListOperationalScopes(string? employeeCode, string? departmentCode, string? warehouseCode, bool? effectiveOnly, int? page, int? pageSize, NexaErpDbContext db, ICurrentUser user, CancellationToken ct)
    {
        var companyId = await CurrentCompanyId(db,user,ct); if(!companyId.HasValue)return Results.Forbid();
        var today=DateOnly.FromDateTime(DateTime.UtcNow);var paging=MasterEndpointHelpers.NormalizePaging(page,pageSize);
        var query=db.EmployeeOperationalScopes.AsNoTracking().Where(x=>x.CompanyId==companyId.Value);
        if(!string.IsNullOrWhiteSpace(employeeCode)){var code=MasterEndpointHelpers.NormalizeCode(employeeCode);query=query.Where(x=>x.Employee!.EmployeeCode==code);}
        if(!string.IsNullOrWhiteSpace(departmentCode)){var code=MasterEndpointHelpers.NormalizeCode(departmentCode);query=query.Where(x=>x.Department!.Code==code);}
        if(!string.IsNullOrWhiteSpace(warehouseCode)){var code=MasterEndpointHelpers.NormalizeCode(warehouseCode);query=query.Where(x=>x.Warehouse!.WarehouseCode==code);}
        if(effectiveOnly==true)query=query.Where(x=>x.IsActive&&x.EffectiveFrom<=today&&(!x.EffectiveTo.HasValue||x.EffectiveTo.Value>=today));
        var total=await query.CountAsync(ct);var rows=await query.OrderBy(x=>x.Employee!.EmployeeCode).ThenBy(x=>x.Department!.Code).ThenByDescending(x=>x.EffectiveFrom)
            .Skip(paging.Skip).Take(paging.PageSize).Select(x=>new OperationalScopeSummary(x.Id,x.OrganizationId,x.EmployeeId,x.Employee!.EmployeeCode,x.DepartmentId,x.Department==null?null:x.Department.Code,x.WarehouseId,x.Warehouse==null?null:x.Warehouse.WarehouseCode,x.RackBinId,x.RackBin==null?null:x.RackBin.BinCode,x.OwnRecordsOnly,x.AllowsPrivilegedCrossScope,x.EffectiveFrom,x.EffectiveTo,x.IsActive,x.Remarks,x.Version)).ToListAsync(ct);
        return Results.Ok(new PagedResponse<OperationalScopeSummary>(total,paging.PageNumber,paging.PageSize,rows));
    }

    private static async Task<IResult> ListUomConversions(string? fromUomCode,string? toUomCode,string? measurementDimension,bool? effectiveOnly,int? page,int? pageSize,NexaErpDbContext db,ICurrentUser user,CancellationToken ct)
    {
        var today=DateOnly.FromDateTime(DateTime.UtcNow);var paging=MasterEndpointHelpers.NormalizePaging(page,pageSize);var organization=user.OrganizationId;
        var query=db.UomConversions.AsNoTracking().Where(x=>x.OrganizationId==organization);
        if(!string.IsNullOrWhiteSpace(fromUomCode)){var code=MasterEndpointHelpers.NormalizeCode(fromUomCode);query=query.Where(x=>x.FromUom!.Code==code);}
        if(!string.IsNullOrWhiteSpace(toUomCode)){var code=MasterEndpointHelpers.NormalizeCode(toUomCode);query=query.Where(x=>x.ToUom!.Code==code);}
        if(!string.IsNullOrWhiteSpace(measurementDimension)){var dimension=MasterEndpointHelpers.NormalizeCode(measurementDimension);query=query.Where(x=>x.MeasurementDimension==dimension);}
        if(effectiveOnly==true)query=query.Where(x=>x.IsActive&&x.EffectiveFrom<=today&&(!x.EffectiveTo.HasValue||x.EffectiveTo.Value>=today));
        var total=await query.CountAsync(ct);var rows=await query.OrderBy(x=>x.FromUom!.Code).ThenBy(x=>x.ToUom!.Code).ThenByDescending(x=>x.EffectiveFrom)
            .Skip(paging.Skip).Take(paging.PageSize).Select(x=>new UomConversionSummary(x.Id,x.OrganizationId,x.FromUomId,x.FromUom!.Code,x.ToUomId,x.ToUom!.Code,x.MeasurementDimension,x.ConversionFactor,x.QuantityPrecision,x.EffectiveFrom,x.EffectiveTo,x.ApprovalStatus,x.IsActive,x.FirstUsedAt,
                db.ControlledConfigurationHistories.Where(h=>h.EntityType==nameof(UomConversion)&&h.EntityId==x.Id).OrderByDescending(h=>h.CreatedAt).Select(h=>h.Remarks).FirstOrDefault()??string.Empty,x.Version)).ToListAsync(ct);
        return Results.Ok(new PagedResponse<UomConversionSummary>(total,paging.PageNumber,paging.PageSize,rows));
    }

    private static async Task<IResult> ListTaxGstSettings(string? hsnSacCode,string? approvalStatus,bool? effectiveOnly,int? page,int? pageSize,NexaErpDbContext db,ICurrentUser user,CancellationToken ct)
    {
        var companyId=await CurrentCompanyId(db,user,ct);if(!companyId.HasValue)return Results.Forbid();var today=DateOnly.FromDateTime(DateTime.UtcNow);var paging=MasterEndpointHelpers.NormalizePaging(page,pageSize);
        var query=db.TaxGstSettings.AsNoTracking().Where(x=>x.CompanyId==companyId.Value);
        if(!string.IsNullOrWhiteSpace(hsnSacCode)){var code=MasterEndpointHelpers.NormalizeCode(hsnSacCode);query=query.Where(x=>x.HsnSacCode==code);}
        if(!string.IsNullOrWhiteSpace(approvalStatus)){var status=approvalStatus.Trim();query=query.Where(x=>x.ApprovalStatus==status);}
        if(effectiveOnly==true)query=query.Where(x=>x.IsActive&&x.ApprovalStatus==MasterApprovalStatuses.Approved&&x.EffectiveFrom<=today&&(!x.EffectiveTo.HasValue||x.EffectiveTo.Value>=today));
        var total=await query.CountAsync(ct);var rows=await query.OrderBy(x=>x.HsnSacCode).ThenBy(x=>x.SupplierStateCode).ThenBy(x=>x.PlaceOfSupplyStateCode).ThenByDescending(x=>x.EffectiveFrom)
            .Skip(paging.Skip).Take(paging.PageSize).Select(x=>new TaxGstSettingSummary(x.Id,x.OrganizationId,x.JurisdictionCode,x.HsnSacCode,x.SupplyType,x.SupplierStateCode,x.PlaceOfSupplyStateCode,x.VendorRegistrationType,x.GstRate,x.CgstRate,x.SgstRate,x.IgstRate,x.CessRate,x.IsExempt,x.IsReverseCharge,x.CurrencyCode,x.RoundingScale,x.EffectiveFrom,x.EffectiveTo,x.ApprovalStatus,x.CreatorEmployeeId,x.DecisionEmployeeId,x.DecisionRoleCode,x.DecisionAt,x.DecisionRemarks,x.SupersedesTaxGstSettingId,x.IsActive,
                db.ControlledConfigurationHistories.Where(h=>h.EntityType==nameof(TaxGstSetting)&&h.EntityId==x.Id).OrderByDescending(h=>h.CreatedAt).Select(h=>h.Remarks).FirstOrDefault()??string.Empty,x.Version)).ToListAsync(ct);
        return Results.Ok(new PagedResponse<TaxGstSettingSummary>(total,paging.PageNumber,paging.PageSize,rows));
    }

    private static async Task<IResult> ListVendorQualifications(string? vendorCode,string? itemCategoryCode,string? approvalStatus,bool? effectiveOnly,int? page,int? pageSize,NexaErpDbContext db,ICurrentUser user,CancellationToken ct)
    {
        var companyId=await CurrentCompanyId(db,user,ct);if(!companyId.HasValue)return Results.Forbid();var today=DateOnly.FromDateTime(DateTime.UtcNow);var paging=MasterEndpointHelpers.NormalizePaging(page,pageSize);
        var query=db.VendorQualifications.AsNoTracking().Where(x=>x.CompanyId==companyId.Value);
        if(!string.IsNullOrWhiteSpace(vendorCode)){var code=MasterEndpointHelpers.NormalizeCode(vendorCode);query=query.Where(x=>x.Vendor!.VendorCode==code);}
        if(!string.IsNullOrWhiteSpace(itemCategoryCode)){var code=MasterEndpointHelpers.NormalizeCode(itemCategoryCode);query=query.Where(x=>x.ItemCategory!.Code==code);}
        if(!string.IsNullOrWhiteSpace(approvalStatus)){var status=approvalStatus.Trim();query=query.Where(x=>x.ApprovalStatus==status);}
        if(effectiveOnly==true)query=query.Where(x=>x.IsActive&&x.ApprovalStatus==MasterApprovalStatuses.Approved&&x.EffectiveFrom<=today&&(!x.EffectiveTo.HasValue||x.EffectiveTo.Value>=today));
        var total=await query.CountAsync(ct);var rows=await query.OrderBy(x=>x.Vendor!.VendorCode).ThenBy(x=>x.QualificationCode).ThenByDescending(x=>x.EffectiveFrom)
            .Skip(paging.Skip).Take(paging.PageSize).Select(x=>new VendorQualificationSummary(x.Id,x.OrganizationId,x.VendorId,x.Vendor!.VendorCode,x.ItemCategoryId,x.ItemCategory==null?null:x.ItemCategory.Code,x.QualificationCode,x.EffectiveFrom,x.EffectiveTo,x.VerificationStatus,x.VerifiedByEmployeeId,x.ApprovalStatus,x.ApprovedByEmployeeId,x.IsActive,
                db.ControlledConfigurationHistories.Where(h=>h.EntityType==nameof(VendorQualification)&&h.EntityId==x.Id).OrderByDescending(h=>h.CreatedAt).Select(h=>h.Remarks).FirstOrDefault()??string.Empty,x.Version)).ToListAsync(ct);
        return Results.Ok(new PagedResponse<VendorQualificationSummary>(total,paging.PageNumber,paging.PageSize,rows));
    }

    private static Task<Guid?> CurrentCompanyId(NexaErpDbContext db,ICurrentUser user,CancellationToken ct)=>
        db.Companies.Where(x=>x.Code==user.OrganizationId&&x.IsActive&&x.Status=="ACTIVE").Select(x=>(Guid?)x.Id).SingleOrDefaultAsync(ct);
}