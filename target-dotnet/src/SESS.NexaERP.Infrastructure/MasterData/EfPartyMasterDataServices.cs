using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.MasterData;

public sealed class EfCustomerMasterDataService(
    NexaErpDbContext db,
    ICurrentUser user,
    IAuditWriter audit,
    IDateTimeProvider clock) : ICustomerMasterDataService
{
    public async Task<IReadOnlyList<MasterDataExportRow>> ExportAsync(MasterDataExportQuery query, CancellationToken cancellationToken)
    {
        var rows = db.Customers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToUpperInvariant();
            rows = rows.Where(x => x.CustomerCode.ToUpper().Contains(term) || x.LegalCustomerName.ToUpper().Contains(term)
                || (x.GstNumber != null && x.GstNumber.ToUpper().Contains(term)));
        }
        if (query.IsActive.HasValue) rows = rows.Where(x => x.IsActive == query.IsActive.Value);
        var descending = string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        rows = string.Equals(query.SortBy, "name", StringComparison.OrdinalIgnoreCase)
            ? descending ? rows.OrderByDescending(x => x.LegalCustomerName).ThenByDescending(x => x.CustomerCode) : rows.OrderBy(x => x.LegalCustomerName).ThenBy(x => x.CustomerCode)
            : descending ? rows.OrderByDescending(x => x.CustomerCode) : rows.OrderBy(x => x.CustomerCode);
        return await rows.Select(x => Export(x)).ToListAsync(cancellationToken);
    }

    public async Task<MasterDataExistingSet> LoadExistingAsync(IReadOnlyCollection<string> normalizedCodes, IReadOnlyCollection<Guid> recordIds, CancellationToken cancellationToken)
    {
        var rows = await db.Customers.AsNoTracking().Where(x => normalizedCodes.Contains(x.CustomerCode) || recordIds.Contains(x.Id)).ToListAsync(cancellationToken);
        var records = rows.Select(Existing).ToArray();
        return new(records.ToDictionary(x => x.NormalizedBusinessCode, StringComparer.Ordinal), records.ToDictionary(x => x.Id));
    }

    public Task<IReadOnlyList<MasterDataPartyIdentityRecord>> LoadIdentityRecordsAsync(IReadOnlyCollection<string> gstins, IReadOnlyCollection<string> pans, CancellationToken cancellationToken) =>
        db.Customers.AsNoTracking().Where(x => (x.GstNumber != null && gstins.Contains(x.GstNumber)) || (x.PanNumber != null && pans.Contains(x.PanNumber)))
            .Select(x => new MasterDataPartyIdentityRecord(x.Id, x.CustomerCode, x.GstNumber, x.PanNumber, x.LegalCustomerName))
            .ToListAsync(cancellationToken).ContinueWith<IReadOnlyList<MasterDataPartyIdentityRecord>>(x => x.Result, cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

    public async Task<MasterDataApplyResult> CreateAsync(UpsertCustomerRequest request, CancellationToken cancellationToken)
    {
        var values = await ValidateAsync(request, null, cancellationToken);
        var now = clock.UtcNow;
        var row = new Customer { CustomerCode = values.Code, IsCustomerCodeLocked = false, CreatedAt = now, CreatedBy = user.LoginId };
        Apply(row, request, values, now, creating: true);
        db.Customers.Add(row);
        db.MasterStatusHistories.Add(new() { MasterType = nameof(Customer), MasterId = row.Id, MasterCode = row.CustomerCode,
            PreviousStatus = null, NewStatus = row.Status, Reason = "Customer draft created by master-data import", SourceRevision = "MASTER_DATA_IMPORT",
            CorrelationId = $"MASTER_DATA_IMPORT_CUSTOMER_CREATE_{Guid.NewGuid():N}", CreatedAt = now, CreatedBy = user.LoginId });
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("Masters", "ImportCreateDraft", nameof(Customer), row.Id.ToString(), null, row, cancellationToken);
        return new(row.Id, row.Version);
    }

    public async Task<MasterDataApplyResult> UpdateAsync(MasterDataExistingRecord existing, UpsertCustomerRequest request, CancellationToken cancellationToken)
    {
        var row = await db.Customers.SingleOrDefaultAsync(x => x.Id == existing.Id, cancellationToken)
            ?? throw new MasterDataNotFoundException("Customer not found.");
        if (row.Version != existing.Version || request.Version != existing.Version) throw new MasterDataConflictException("Stale record version. Refresh and retry.");
        var values = await ValidateAsync(request, row.Id, cancellationToken);
        if (!string.Equals(row.CustomerCode, values.Code, StringComparison.Ordinal)) throw new MasterDataValidationException("Customer business code is immutable through upload.");
        var before = Export(row);
        var now = clock.UtcNow;
        Apply(row, request, values, now, creating: false);
        row.Version = checked(row.Version + 1);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("Masters", "ImportUpdate", nameof(Customer), row.Id.ToString(), before, row, cancellationToken);
        return new(row.Id, row.Version);
    }

    private async Task<(string Code, string? State, string? StateCode, string Country)> ValidateAsync(UpsertCustomerRequest request, Guid? currentId, CancellationToken cancellationToken)
    {
        var code = PartyMasterRules.Code(request.CustomerCode);
        var errors = new List<MasterDataRowError>();
        var location = PartyMasterRules.Location(request.State, request.StateCode, request.Country, errors);
        var gst = PartyMasterRules.UpperOptional(request.GstNumber); var pan = PartyMasterRules.UpperOptional(request.PanNumber);
        PartyMasterRules.ValidateTaxIdentity(gst, pan, location.StateCode, errors);
        if (code.Length is 0 or > 80) errors.Add(PartyMasterRules.Error("CustomerCode", "Customer Code", "INVALID_VALUE", "Customer Code is required and cannot exceed 80 characters.", request.CustomerCode));
        if (string.IsNullOrWhiteSpace(request.LegalCustomerName) || request.LegalCustomerName.Trim().Length > 240) errors.Add(PartyMasterRules.Error("LegalCustomerName", "Legal Customer Name", "INVALID_VALUE", "Legal Customer Name is required and cannot exceed 240 characters.", request.LegalCustomerName));
        if (string.IsNullOrWhiteSpace(request.CustomerType) || request.CustomerType.Trim().Length > 80) errors.Add(PartyMasterRules.Error("CustomerType", "Customer Type", "INVALID_VALUE", "Customer Type is required and cannot exceed 80 characters.", request.CustomerType));
        if (!PartyMasterRules.IsValidEmail(PartyMasterRules.Optional(request.Email))) errors.Add(PartyMasterRules.Error("Email", "Email", "INVALID_FORMAT", "Email format is invalid.", request.Email));
        if (request.CreditPeriodDays < 0) errors.Add(PartyMasterRules.Error("CreditPeriodDays", "Credit Period Days", "INVALID_VALUE", "Credit Period Days cannot be negative.", request.CreditPeriodDays?.ToString(CultureInfo.InvariantCulture)));
        if (request.CreditLimit < 0) errors.Add(PartyMasterRules.Error("CreditLimit", "Credit Limit", "INVALID_VALUE", "Credit Limit cannot be negative.", request.CreditLimit?.ToString(CultureInfo.InvariantCulture)));
        if (errors.Count > 0) throw new MasterDataValidationException(string.Join(" ", errors.Select(x => x.Message).Distinct(StringComparer.Ordinal)));
        if (await db.Customers.AnyAsync(x => x.Id != currentId && (x.CustomerCode == code || (gst != null && x.GstNumber == gst)
            || (pan != null && x.PanNumber == pan && x.LegalCustomerName.ToUpper() == request.LegalCustomerName.Trim().ToUpper())), cancellationToken))
            throw new MasterDataConflictException("Duplicate customer identity blocked.");
        return (code, location.State, location.StateCode, location.Country);
    }

    private void Apply(Customer row, UpsertCustomerRequest request, (string Code, string? State, string? StateCode, string Country) values, DateTimeOffset now, bool creating)
    {
        row.CustomerCode = values.Code; row.LegalCustomerName = request.LegalCustomerName.Trim(); row.Name = row.LegalCustomerName;
        row.TradeName = PartyMasterRules.Optional(request.TradeName); row.CustomerType = request.CustomerType.Trim();
        row.GstNumber = PartyMasterRules.UpperOptional(request.GstNumber); row.PanNumber = PartyMasterRules.UpperOptional(request.PanNumber);
        row.BillingAddress = PartyMasterRules.Optional(request.BillingAddress); row.ShippingAddress = PartyMasterRules.Optional(request.ShippingAddress);
        row.State = values.State; row.StateCode = values.StateCode; row.Country = values.Country;
        row.ContactPerson = PartyMasterRules.Optional(request.ContactPerson); row.Phone = PartyMasterRules.Optional(request.Phone); row.Email = PartyMasterRules.Optional(request.Email);
        row.Industry = PartyMasterRules.Optional(request.Industry); row.PaymentTerms = PartyMasterRules.Optional(request.PaymentTerms);
        row.CreditPeriodDays = request.CreditPeriodDays; row.CreditLimit = request.CreditLimit;
        row.PortalOrganizationId = PartyMasterRules.PortalOrganizationId(values.Code);
        if (!creating) { row.UpdatedAt = now; row.UpdatedBy = user.LoginId; }
    }

    private static MasterDataExportRow Export(Customer x) => new(new Dictionary<string, object?>
    {
        ["RecordId"] = x.Id.ToString(), ["Version"] = x.Version, ["CustomerCode"] = x.CustomerCode,
        ["LegalCustomerName"] = x.LegalCustomerName, ["TradeName"] = x.TradeName, ["CustomerType"] = x.CustomerType,
        ["GstNumber"] = x.GstNumber, ["PanNumber"] = x.PanNumber, ["BillingAddress"] = x.BillingAddress,
        ["ShippingAddress"] = x.ShippingAddress, ["State"] = x.State, ["StateCode"] = x.StateCode, ["Country"] = x.Country,
        ["ContactPerson"] = x.ContactPerson, ["Phone"] = x.Phone, ["Email"] = x.Email, ["Industry"] = x.Industry,
        ["PaymentTerms"] = x.PaymentTerms, ["CreditPeriodDays"] = x.CreditPeriodDays, ["CreditLimit"] = x.CreditLimit,
        ["Status"] = x.Status, ["ApprovalStatus"] = x.ApprovalStatus, ["IsActive"] = x.IsActive
    });

    private static MasterDataExistingRecord Existing(Customer x) => new(x.Id, x.CustomerCode, PartyMasterRules.Code(x.CustomerCode), x.Version,
        Export(x).Values.ToDictionary(x => x.Key, x => Convert.ToString(x.Value, CultureInfo.InvariantCulture), StringComparer.Ordinal));
}

public sealed class EfVendorMasterDataService(
    NexaErpDbContext db,
    ICurrentUser user,
    IAuditWriter audit,
    IPagePermissionService permissions,
    IDateTimeProvider clock) : IVendorMasterDataService
{
    private static readonly string[] RolePriority = ["PURCHASE_HEAD", "IT_MANAGER", "TECHNICAL_DIRECTOR", "MANAGING_DIRECTOR", "MD", "ADMIN"];

    public async Task<IReadOnlyList<MasterDataExportRow>> ExportAsync(MasterDataExportQuery query, CancellationToken cancellationToken)
    {
        var rows = db.Vendors.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search)) { var term = query.Search.Trim().ToUpperInvariant(); rows = rows.Where(x => x.VendorCode.ToUpper().Contains(term) || x.LegalVendorName.ToUpper().Contains(term) || (x.GstNumber != null && x.GstNumber.ToUpper().Contains(term))); }
        if (query.IsActive.HasValue) rows = rows.Where(x => x.IsActive == query.IsActive.Value);
        var descending = string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        rows = string.Equals(query.SortBy, "name", StringComparison.OrdinalIgnoreCase)
            ? descending ? rows.OrderByDescending(x => x.LegalVendorName).ThenByDescending(x => x.VendorCode) : rows.OrderBy(x => x.LegalVendorName).ThenBy(x => x.VendorCode)
            : descending ? rows.OrderByDescending(x => x.VendorCode) : rows.OrderBy(x => x.VendorCode);
        return await rows.Select(x => Export(x)).ToListAsync(cancellationToken);
    }

    public async Task<MasterDataExistingSet> LoadExistingAsync(IReadOnlyCollection<string> normalizedCodes, IReadOnlyCollection<Guid> recordIds, CancellationToken cancellationToken)
    {
        var rows = await db.Vendors.AsNoTracking().Where(x => normalizedCodes.Contains(x.VendorCode) || recordIds.Contains(x.Id)).ToListAsync(cancellationToken);
        var records = rows.Select(Existing).ToArray();
        return new(records.ToDictionary(x => x.NormalizedBusinessCode, StringComparer.Ordinal), records.ToDictionary(x => x.Id));
    }

    public Task<IReadOnlyList<MasterDataPartyIdentityRecord>> LoadIdentityRecordsAsync(IReadOnlyCollection<string> gstins, IReadOnlyCollection<string> pans, CancellationToken cancellationToken) =>
        db.Vendors.AsNoTracking().Where(x => (x.GstNumber != null && gstins.Contains(x.GstNumber)) || (x.PanNumber != null && pans.Contains(x.PanNumber)))
            .Select(x => new MasterDataPartyIdentityRecord(x.Id, x.VendorCode, x.GstNumber, x.PanNumber, x.LegalVendorName))
            .ToListAsync(cancellationToken).ContinueWith<IReadOnlyList<MasterDataPartyIdentityRecord>>(x => x.Result, cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

    public async Task<MasterDataApplyResult> CreateAsync(UpsertVendorRequest request, CancellationToken cancellationToken)
    {
        var values = await ValidateAsync(request, null, cancellationToken); var now = clock.UtcNow;
        var row = new Vendor { VendorCode = values.Code, IsVendorCodeLocked = false, CreatedAt = now, CreatedBy = user.LoginId };
        Apply(row, request, values, now, creating: true); db.Vendors.Add(row);
        db.MasterStatusHistories.Add(new() { MasterType = nameof(Vendor), MasterId = row.Id, MasterCode = row.VendorCode, PreviousStatus = null,
            NewStatus = row.VendorStatus, Reason = "Vendor draft created by master-data import", SourceRevision = "MASTER_DATA_IMPORT",
            CorrelationId = $"MASTER_DATA_IMPORT_VENDOR_CREATE_{Guid.NewGuid():N}", CreatedAt = now, CreatedBy = user.LoginId });
        await db.SaveChangesAsync(cancellationToken); await audit.WriteAsync("Masters", "ImportCreateDraft", nameof(Vendor), row.Id.ToString(), null, row, cancellationToken);
        return new(row.Id, row.Version);
    }

    public async Task<MasterDataApplyResult> UpdateAsync(MasterDataExistingRecord existing, UpsertVendorRequest request, CancellationToken cancellationToken)
    {
        var row = await db.Vendors.SingleOrDefaultAsync(x => x.Id == existing.Id, cancellationToken) ?? throw new MasterDataNotFoundException("Vendor not found.");
        if (row.Version != existing.Version || request.Version != existing.Version) throw new MasterDataConflictException("Stale record version. Refresh and retry.");
        var values = await ValidateAsync(request, row.Id, cancellationToken);
        if (!string.Equals(row.VendorCode, values.Code, StringComparison.Ordinal)) throw new MasterDataValidationException("Vendor business code is immutable through upload.");
        var before = Export(row); var controlledBefore = ControlledSnapshot(row); var controlled = ControlledChanged(row, request);
        var now = clock.UtcNow; Apply(row, request, values, now, creating: false); row.Version = checked(row.Version + 1);
        if (controlled) await AddReverificationAsync(row, controlledBefore, cancellationToken);
        await db.SaveChangesAsync(cancellationToken); await audit.WriteAsync("Masters", "ImportUpdate", nameof(Vendor), row.Id.ToString(), before, row, cancellationToken);
        return new(row.Id, row.Version);
    }

    private async Task<(string Code, string? State, string? StateCode, string Country)> ValidateAsync(UpsertVendorRequest request, Guid? currentId, CancellationToken cancellationToken)
    {
        var code = PartyMasterRules.Code(request.VendorCode); var errors = new List<MasterDataRowError>();
        var location = PartyMasterRules.Location(request.State, request.StateCode, request.Country, errors);
        var gst = PartyMasterRules.UpperOptional(request.GstNumber); var pan = PartyMasterRules.UpperOptional(request.PanNumber);
        PartyMasterRules.ValidateTaxIdentity(gst, pan, location.StateCode, errors);
        if (code.Length is 0 or > 80) errors.Add(PartyMasterRules.Error("VendorCode", "Vendor Code", "INVALID_VALUE", "Vendor Code is required and cannot exceed 80 characters.", request.VendorCode));
        if (string.IsNullOrWhiteSpace(request.LegalVendorName) || request.LegalVendorName.Trim().Length > 240) errors.Add(PartyMasterRules.Error("LegalVendorName", "Legal Vendor Name", "INVALID_VALUE", "Legal Vendor Name is required and cannot exceed 240 characters.", request.LegalVendorName));
        if (string.IsNullOrWhiteSpace(request.VendorType) || request.VendorType.Trim().Length > 80) errors.Add(PartyMasterRules.Error("VendorType", "Vendor Type", "INVALID_VALUE", "Vendor Type is required and cannot exceed 80 characters.", request.VendorType));
        if (request.MsmeStatus && string.IsNullOrWhiteSpace(request.MsmeNumber)) errors.Add(PartyMasterRules.Error("MsmeNumber", "MSME Number", "REQUIRED_WHEN_MSME", "MSME Number is required when MSME Status is TRUE.", request.MsmeNumber));
        if (!PartyMasterRules.IsValidEmail(PartyMasterRules.Optional(request.Email))) errors.Add(PartyMasterRules.Error("Email", "Email", "INVALID_FORMAT", "Email format is invalid.", request.Email));
        if (request.CreditPeriodDays < 0) errors.Add(PartyMasterRules.Error("CreditPeriodDays", "Credit Period Days", "INVALID_VALUE", "Credit Period Days cannot be negative.", request.CreditPeriodDays?.ToString(CultureInfo.InvariantCulture)));
        if (errors.Count > 0) throw new MasterDataValidationException(string.Join(" ", errors.Select(x => x.Message).Distinct(StringComparer.Ordinal)));
        if (await db.Vendors.AnyAsync(x => x.Id != currentId && (x.VendorCode == code || (gst != null && x.GstNumber == gst)
            || (pan != null && x.PanNumber == pan && x.LegalVendorName.ToUpper() == request.LegalVendorName.Trim().ToUpper())), cancellationToken)) throw new MasterDataConflictException("Duplicate vendor identity blocked.");
        return (code, location.State, location.StateCode, location.Country);
    }

    private void Apply(Vendor row, UpsertVendorRequest request, (string Code, string? State, string? StateCode, string Country) values, DateTimeOffset now, bool creating)
    {
        row.VendorCode = values.Code; row.LegalVendorName = request.LegalVendorName.Trim(); row.Name = row.LegalVendorName;
        row.TradeName = PartyMasterRules.Optional(request.TradeName); row.VendorType = request.VendorType.Trim();
        row.GstNumber = PartyMasterRules.UpperOptional(request.GstNumber); row.PanNumber = PartyMasterRules.UpperOptional(request.PanNumber);
        row.MsmeStatus = request.MsmeStatus; row.MsmeNumber = PartyMasterRules.Optional(request.MsmeNumber);
        row.ContactPerson = PartyMasterRules.Optional(request.ContactPerson); row.Phone = PartyMasterRules.Optional(request.Phone); row.Email = PartyMasterRules.Optional(request.Email);
        row.BillingAddress = PartyMasterRules.Optional(request.BillingAddress); row.ShippingAddress = PartyMasterRules.Optional(request.ShippingAddress);
        row.State = values.State; row.StateCode = values.StateCode; row.Country = values.Country;
        row.MaterialServiceCategories = PartyMasterRules.Optional(request.MaterialServiceCategories); row.ApprovedMakes = PartyMasterRules.Optional(request.ApprovedMakes);
        row.PaymentTerms = PartyMasterRules.Optional(request.PaymentTerms); row.DeliveryTerms = PartyMasterRules.Optional(request.DeliveryTerms);
        row.CreditPeriodDays = request.CreditPeriodDays; row.BankMetadataJson = PartyMasterRules.Optional(request.BankMetadataJson);
        row.AttachmentMetadataJson = PartyMasterRules.Optional(request.AttachmentMetadataJson); row.PortalOrganizationId = PartyMasterRules.PortalOrganizationId(values.Code);
        if (!creating) { row.UpdatedAt = now; row.UpdatedBy = user.LoginId; }
    }

    private async Task AddReverificationAsync(Vendor row, object before, CancellationToken cancellationToken)
    {
        var previous = row.ApprovalStatus; row.CommercialVerificationStatus = MasterApprovalStatuses.PendingApproval; row.ApprovalStatus = MasterApprovalStatuses.PendingApproval;
        row.VendorStatus = MasterStatuses.PendingApproval; row.RequiresReverification = true; row.CommercialVerifiedBy = null; row.CommercialVerifiedAt = null; row.ApprovedBy = null; row.ApprovedAt = null;
        var role = await ResolveRoleAsync(cancellationToken); var correlation = $"MASTER_DATA_IMPORT_VENDOR_REVERIFY_{Guid.NewGuid():N}";
        db.MasterApprovalHistories.Add(new() { MasterType = nameof(Vendor), MasterId = row.Id, MasterCode = row.VendorCode,
            Action = "ControlledDetailsChanged", FromStatus = previous, ToStatus = row.ApprovalStatus,
            Remarks = "GST/PAN/commercial details changed by import; Accounts re-verification and final approval required.", ActorLoginId = user.LoginId,
            ActorRoleCode = role, CorrelationId = correlation, CreatedBy = user.LoginId });
        db.ControlledConfigurationHistories.Add(new() { OrganizationId = user.OrganizationId ?? "SESS", EntityType = nameof(Vendor), EntityId = row.Id,
            Action = "ControlledDetailsChanged", BeforeJson = JsonSerializer.Serialize(before), AfterJson = JsonSerializer.Serialize(ControlledSnapshot(row)),
            ActorLoginId = user.LoginId, ActorRoleCode = role, Remarks = "Controlled vendor details changed by master-data import.", CorrelationId = correlation, CreatedBy = user.LoginId });
    }

    private async Task<string> ResolveRoleAsync(CancellationToken cancellationToken)
    {
        foreach (var role in RolePriority)
            if (string.Equals(user.RoleCode, role, StringComparison.OrdinalIgnoreCase)
                && await permissions.HasPermissionAsync([role], "masters.vendors", PagePermissionActions.Update, cancellationToken)) return role;
        throw new UnauthorizedAccessException("No deterministic operational role can update vendor master data.");
    }

    private static bool ControlledChanged(Vendor x, UpsertVendorRequest r) =>
        !string.Equals(x.GstNumber, PartyMasterRules.UpperOptional(r.GstNumber), StringComparison.Ordinal) || !string.Equals(x.PanNumber, PartyMasterRules.UpperOptional(r.PanNumber), StringComparison.Ordinal)
        || !string.Equals(x.PaymentTerms, PartyMasterRules.Optional(r.PaymentTerms), StringComparison.Ordinal) || !string.Equals(x.DeliveryTerms, PartyMasterRules.Optional(r.DeliveryTerms), StringComparison.Ordinal)
        || x.CreditPeriodDays != r.CreditPeriodDays;
    private static object ControlledSnapshot(Vendor x) => new { x.GstNumber, x.PanNumber, x.BankMetadataJson, x.PaymentTerms, x.DeliveryTerms, x.CreditPeriodDays, x.CommercialVerificationStatus, x.ApprovalStatus, x.VendorStatus, x.EffectiveFrom, x.EffectiveTo };

    private static MasterDataExportRow Export(Vendor x) => new(new Dictionary<string, object?>
    {
        ["RecordId"] = x.Id.ToString(), ["Version"] = x.Version, ["VendorCode"] = x.VendorCode,
        ["LegalVendorName"] = x.LegalVendorName, ["TradeName"] = x.TradeName, ["VendorType"] = x.VendorType,
        ["GstNumber"] = x.GstNumber, ["PanNumber"] = x.PanNumber, ["MsmeStatus"] = x.MsmeStatus, ["MsmeNumber"] = x.MsmeNumber,
        ["ContactPerson"] = x.ContactPerson, ["Phone"] = x.Phone, ["Email"] = x.Email, ["BillingAddress"] = x.BillingAddress,
        ["ShippingAddress"] = x.ShippingAddress, ["State"] = x.State, ["StateCode"] = x.StateCode, ["Country"] = x.Country,
        ["MaterialServiceCategories"] = x.MaterialServiceCategories, ["ApprovedMakes"] = x.ApprovedMakes, ["PaymentTerms"] = x.PaymentTerms,
        ["DeliveryTerms"] = x.DeliveryTerms, ["CreditPeriodDays"] = x.CreditPeriodDays, ["AttachmentMetadataJson"] = x.AttachmentMetadataJson,
        ["ApprovalStatus"] = x.ApprovalStatus, ["VendorStatus"] = x.VendorStatus, ["IsActive"] = x.IsActive
    });

    private static MasterDataExistingRecord Existing(Vendor x)
    {
        var values = Export(x).Values.ToDictionary(x => x.Key, x => Convert.ToString(x.Value, CultureInfo.InvariantCulture), StringComparer.Ordinal);
        values["__BankMetadataJson"] = x.BankMetadataJson;
        return new(x.Id, x.VendorCode, PartyMasterRules.Code(x.VendorCode), x.Version, values);
    }
}
