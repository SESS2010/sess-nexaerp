using System.Globalization;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Masters;

namespace SESS.NexaERP.Infrastructure.MasterData;

public sealed class CustomerMasterDataDefinition : IMasterDataDefinition
{
    public string MasterKey => "customers";
    public int TemplateVersion => 1;
    public string PageKey => "masters.customers";
    public string BusinessCodeColumnKey => "CustomerCode";
    public IReadOnlyList<string> OperationalRolePriority { get; } = ["SALES_HEAD", "IT_MANAGER", "TECHNICAL_DIRECTOR", "MANAGING_DIRECTOR", "MD", "ADMIN"];
    public MasterDataSensitivePermission? SensitiveResultPermission => new(PageKey, PagePermissionActions.ViewCommercialValues);
    public IReadOnlyList<string> WorkbookGuideNotes { get; } =
    [
        "PortalOrganizationId is not imported. The server derives it from Customer Code.",
        "Company relationship supplier codes are not part of this shared customer workbook."
    ];
    public IReadOnlyList<MasterDataColumnDefinition> Columns { get; } =
    [
        PartyColumns.ReadOnlyId(), PartyColumns.ReadOnlyVersion(),
        PartyColumns.Text("CustomerCode", "Customer Code", true, 80, "Immutable shared customer business code."),
        PartyColumns.Text("LegalCustomerName", "Legal Customer Name", true, 240, "Customer legal name."),
        PartyColumns.Text("TradeName", "Trade Name", false, 240, "Optional trading name."),
        PartyColumns.Text("CustomerType", "Customer Type", true, 80, "Customer classification."),
        PartyColumns.Text("GstNumber", "GSTIN", false, 32, "Optional; validated only when supplied.", "15-character Indian GSTIN"),
        PartyColumns.Text("PanNumber", "PAN", false, 16, "Optional; validated only when supplied.", "10-character Indian PAN"),
        PartyColumns.Text("BillingAddress", "Billing Address", false, 1000, "Optional billing address."),
        PartyColumns.Text("ShippingAddress", "Shipping Address", false, 1000, "Optional shipping address."),
        PartyColumns.Text("State", "State", false, 80, "Indian GST state code is derived from recognized state names."),
        PartyColumns.Text("StateCode", "State Code", false, 8, "Optional two-digit GST state code; must agree with State."),
        PartyColumns.Text("Country", "Country", false, 80, "Blank is imported as India."),
        PartyColumns.Text("ContactPerson", "Contact Person", false, 160, "Optional primary contact."),
        PartyColumns.Text("Phone", "Phone", false, 40, "Optional phone number."),
        PartyColumns.Text("Email", "Email", false, 254, "Optional email address."),
        PartyColumns.Text("Industry", "Industry", false, 120, "Optional industry."),
        PartyColumns.Text("PaymentTerms", "Payment Terms", false, 500, "Optional payment terms."),
        PartyColumns.Integer("CreditPeriodDays", "Credit Period Days", "Optional non-negative whole number."),
        PartyColumns.Decimal("CreditLimit", "Credit Limit", "Optional non-negative decimal amount."),
        PartyColumns.ReadOnly("Status", "Status", "Governed lifecycle status."),
        PartyColumns.ReadOnly("ApprovalStatus", "Approval Status", "Governed approval status."),
        PartyColumns.ReadOnlyBoolean("IsActive", "Is Active", "Governed lifecycle state.")
    ];
}

public sealed class VendorMasterDataDefinition : IMasterDataDefinition
{
    public string MasterKey => "vendors";
    public int TemplateVersion => 1;
    public string PageKey => "masters.vendors";
    public string BusinessCodeColumnKey => "VendorCode";
    public IReadOnlyList<string> OperationalRolePriority { get; } = ["PURCHASE_HEAD", "IT_MANAGER", "TECHNICAL_DIRECTOR", "MANAGING_DIRECTOR", "MD", "ADMIN"];
    public MasterDataSensitivePermission? SensitiveResultPermission => new(PageKey, PagePermissionActions.ViewCommercialValues);
    public IReadOnlyList<string> WorkbookGuideNotes { get; } =
    [
        "PortalOrganizationId is not imported. The server derives it from Vendor Code.",
        "Bank details / BankMetadataJson are deliberately excluded from the template, export and import. Enter bank details only through the governed UI with masters.vendors:view-commercial-values.",
        "Company relationship customer codes are not part of this shared vendor workbook."
    ];
    public IReadOnlyList<MasterDataColumnDefinition> Columns { get; } =
    [
        PartyColumns.ReadOnlyId(), PartyColumns.ReadOnlyVersion(),
        PartyColumns.Text("VendorCode", "Vendor Code", true, 80, "Immutable shared vendor business code."),
        PartyColumns.Text("LegalVendorName", "Legal Vendor Name", true, 240, "Vendor legal name."),
        PartyColumns.Text("TradeName", "Trade Name", false, 240, "Optional trading name."),
        PartyColumns.Text("VendorType", "Vendor Type", true, 80, "Vendor classification."),
        PartyColumns.Text("GstNumber", "GSTIN", false, 32, "Optional; validated only when supplied.", "15-character Indian GSTIN"),
        PartyColumns.Text("PanNumber", "PAN", false, 16, "Optional; validated only when supplied.", "10-character Indian PAN"),
        new("MsmeStatus", "MSME Status", MasterDataColumnType.Boolean, true, true, true, "TRUE or FALSE", "TRUE, FALSE", null, "Whether the vendor is MSME registered."),
        PartyColumns.Text("MsmeNumber", "MSME Number", false, 80, "Required when MSME Status is TRUE."),
        PartyColumns.Text("ContactPerson", "Contact Person", false, 160, "Optional primary contact."),
        PartyColumns.Text("Phone", "Phone", false, 40, "Optional phone number."),
        PartyColumns.Text("Email", "Email", false, 254, "Optional email address."),
        PartyColumns.Text("BillingAddress", "Billing Address", false, 1000, "Optional billing address."),
        PartyColumns.Text("ShippingAddress", "Shipping Address", false, 1000, "Optional shipping address."),
        PartyColumns.Text("State", "State", false, 80, "Indian GST state code is derived from recognized state names."),
        PartyColumns.Text("StateCode", "State Code", false, 8, "Optional two-digit GST state code; must agree with State."),
        PartyColumns.Text("Country", "Country", false, 80, "Blank is imported as India."),
        PartyColumns.Text("MaterialServiceCategories", "Material / Service Categories", false, 500, "Optional supplied categories."),
        PartyColumns.Text("ApprovedMakes", "Approved Makes", false, 500, "Optional approved makes."),
        PartyColumns.Text("PaymentTerms", "Payment Terms", false, 500, "Optional payment terms."),
        PartyColumns.Text("DeliveryTerms", "Delivery Terms", false, 500, "Optional delivery terms."),
        PartyColumns.Integer("CreditPeriodDays", "Credit Period Days", "Optional non-negative whole number."),
        PartyColumns.Text("AttachmentMetadataJson", "Attachment Metadata JSON", false, null, "Optional attachment metadata JSON."),
        PartyColumns.ReadOnly("ApprovalStatus", "Approval Status", "Governed approval status."),
        PartyColumns.ReadOnly("VendorStatus", "Vendor Status", "Governed lifecycle status."),
        PartyColumns.ReadOnlyBoolean("IsActive", "Is Active", "Governed lifecycle state.")
    ];
}

internal static class PartyColumns
{
    public static MasterDataColumnDefinition ReadOnlyId() => new("RecordId", "Record ID", MasterDataColumnType.Guid, false, true, false, "UUID; blank for new rows", "Existing record UUID", null, "Read-only identity used to detect business-code rename attempts.");
    public static MasterDataColumnDefinition ReadOnlyVersion() => new("Version", "Version", MasterDataColumnType.UnsignedInteger, false, true, false, "Whole number; blank for new rows", "Current exported version", null, "Read-only optimistic concurrency version.");
    public static MasterDataColumnDefinition Text(string key, string header, bool required, int? max, string description, string allowed = "Text") => new(key, header, MasterDataColumnType.Text, required, required, true, max is null ? "Text" : $"Text, maximum {max} characters", allowed, null, description, max);
    public static MasterDataColumnDefinition Integer(string key, string header, string description) => new(key, header, MasterDataColumnType.Integer, false, false, true, "Whole number", "Zero or greater, or blank", null, description);
    public static MasterDataColumnDefinition Decimal(string key, string header, string description) => new(key, header, MasterDataColumnType.Decimal, false, false, true, "Invariant decimal number", "Zero or greater, or blank", null, description);
    public static MasterDataColumnDefinition ReadOnly(string key, string header, string description) => new(key, header, MasterDataColumnType.Text, false, false, false, "Text; blank for new rows", "Current exported value", null, description);
    public static MasterDataColumnDefinition ReadOnlyBoolean(string key, string header, string description) => new(key, header, MasterDataColumnType.Boolean, false, false, false, "TRUE or FALSE; blank for new rows", "TRUE, FALSE", null, description);
}

public sealed record PartyIdentityContext(
    IReadOnlyList<MasterDataPartyIdentityRecord> Records,
    IReadOnlyDictionary<string, int> GstCounts,
    IReadOnlyDictionary<string, int> PanAndNameCounts);

public abstract class PartyMasterDataAdapterBase
{
    protected static string? Value(MasterDataRawRow row, string key) => row.Values.TryGetValue(key, out var value) ? value : null;
    protected static string? Optional(MasterDataRawRow row, string key) => PartyMasterRules.Optional(Value(row, key));
    protected static MasterDataRowError Error(string key, string header, string code, string message, string? attempted) => new(key, header, code, message, attempted);

    protected static void ValidateCommon(MasterDataRawRow row, MasterDataExistingRecord? existing, string codeKey, string nameKey, string typeKey, ICollection<MasterDataRowError> errors)
    {
        Required(row, codeKey, codeKey == "CustomerCode" ? "Customer Code" : "Vendor Code", 80, errors);
        Required(row, nameKey, nameKey == "LegalCustomerName" ? "Legal Customer Name" : "Legal Vendor Name", 240, errors);
        Required(row, typeKey, typeKey == "CustomerType" ? "Customer Type" : "Vendor Type", 80, errors);
        foreach (var field in new (string Key, string Header, int Max)[] { ("TradeName", "Trade Name", 240), ("GstNumber", "GSTIN", 32), ("PanNumber", "PAN", 16), ("BillingAddress", "Billing Address", 1000), ("ShippingAddress", "Shipping Address", 1000), ("State", "State", 80), ("StateCode", "State Code", 8), ("Country", "Country", 80), ("ContactPerson", "Contact Person", 160), ("Phone", "Phone", 40), ("Email", "Email", 254), ("PaymentTerms", "Payment Terms", 500) })
            Maximum(row, field.Key, field.Header, field.Max, errors);
        var location = PartyMasterRules.Location(Value(row, "State"), Value(row, "StateCode"), Value(row, "Country"), errors);
        PartyMasterRules.ValidateTaxIdentity(PartyMasterRules.UpperOptional(Value(row, "GstNumber")), PartyMasterRules.UpperOptional(Value(row, "PanNumber")), location.StateCode, errors);
        if (!PartyMasterRules.IsValidEmail(Optional(row, "Email"))) errors.Add(Error("Email", "Email", "INVALID_FORMAT", "Email format is invalid.", Value(row, "Email")));
        ReadOnly(row, existing, "IsActive", "Is Active", errors);
    }

    protected static void ValidateIdentity(MasterDataRawRow row, MasterDataExistingRecord? existing, PartyIdentityContext? context, string nameKey, ICollection<MasterDataRowError> errors)
    {
        if (context is null) return;
        var gst = PartyMasterRules.UpperOptional(Value(row, "GstNumber"));
        var pan = PartyMasterRules.UpperOptional(Value(row, "PanNumber"));
        var name = Optional(row, nameKey);
        if (gst is not null && context.GstCounts.TryGetValue(gst, out var gstCount) && gstCount > 1)
            errors.Add(Error("GstNumber", "GSTIN", "DUPLICATE_IN_FILE", "GSTIN occurs on more than one submitted row.", gst));
        var panNameKey = PanNameKey(pan, name);
        if (panNameKey is not null && context.PanAndNameCounts.TryGetValue(panNameKey, out var panCount) && panCount > 1)
            errors.Add(Error("PanNumber", "PAN", "DUPLICATE_IN_FILE", "PAN and legal name occur on more than one submitted row.", pan));
        foreach (var match in context.Records.Where(x => x.Id != existing?.Id))
        {
            if (gst is not null && string.Equals(gst, PartyMasterRules.UpperOptional(match.GstNumber), StringComparison.Ordinal))
                errors.Add(Error("GstNumber", "GSTIN", "DUPLICATE_IDENTITY", $"GSTIN already belongs to {match.BusinessCode}.", gst));
            if (pan is not null && string.Equals(pan, PartyMasterRules.UpperOptional(match.PanNumber), StringComparison.Ordinal)
                && string.Equals(name, match.LegalName, StringComparison.OrdinalIgnoreCase))
                errors.Add(Error("PanNumber", "PAN", "DUPLICATE_IDENTITY", $"PAN and legal name already belong to {match.BusinessCode}.", pan));
        }
    }

    protected static async Task<object?> IdentityContextAsync(IReadOnlyList<MasterDataRawRow> rows, Func<IReadOnlyCollection<string>, IReadOnlyCollection<string>, CancellationToken, Task<IReadOnlyList<MasterDataPartyIdentityRecord>>> load, CancellationToken ct)
    {
        var gstins = rows.Select(x => PartyMasterRules.UpperOptional(Value(x, "GstNumber"))).Where(x => x is not null).Cast<string>().Distinct(StringComparer.Ordinal).ToArray();
        var pans = rows.Select(x => PartyMasterRules.UpperOptional(Value(x, "PanNumber"))).Where(x => x is not null).Cast<string>().Distinct(StringComparer.Ordinal).ToArray();
        var gstCounts = rows.Select(x => PartyMasterRules.UpperOptional(Value(x, "GstNumber"))).Where(x => x is not null).Cast<string>()
            .GroupBy(x => x, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.Count(), StringComparer.Ordinal);
        var panNameCounts = rows.Select(x => PanNameKey(PartyMasterRules.UpperOptional(Value(x, "PanNumber")),
                Optional(x, rows.Any(y => y.Values.ContainsKey("LegalCustomerName")) ? "LegalCustomerName" : "LegalVendorName")))
            .Where(x => x is not null).Cast<string>().GroupBy(x => x, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.Count(), StringComparer.Ordinal);
        return new PartyIdentityContext(await load(gstins, pans, ct), gstCounts, panNameCounts);
    }

    private static string? PanNameKey(string? pan, string? name) => pan is null || name is null ? null : $"{pan}|{name.ToUpperInvariant()}";

    protected static bool Same(MasterDataRawRow row, MasterDataExistingRecord existing, IEnumerable<string> keys)
    {
        foreach (var key in keys)
        {
            var submitted = key == "StateCode"
                ? NormalizeMaterial(key, PartyMasterRules.Location(Value(row, "State"), Value(row, "StateCode"), Value(row, "Country")).StateCode)
                : NormalizeMaterial(key, Value(row, key));
            existing.MaterialValues.TryGetValue(key, out var current);
            if (string.IsNullOrWhiteSpace(Value(row, key)) && key is "Status" or "ApprovalStatus" or "VendorStatus" or "IsActive") continue;
            if (!string.Equals(submitted, NormalizeMaterial(key, current), StringComparison.Ordinal)) return false;
        }
        return true;
    }

    protected static string? NormalizeMaterial(string key, string? value)
    {
        if (key == "Country") return PartyMasterRules.Country(value).ToUpperInvariant();
        if (key is "CustomerCode" or "VendorCode" or "GstNumber" or "PanNumber" or "StateCode") return PartyMasterRules.UpperOptional(value);
        if (key is "MsmeStatus" or "IsActive") return bool.TryParse(value, out var flag) ? flag.ToString().ToUpperInvariant() : PartyMasterRules.Optional(value)?.ToUpperInvariant();
        return PartyMasterRules.Optional(value);
    }

    protected static int? OptionalInt(MasterDataRawRow row, string key) => int.TryParse(Value(row, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;
    protected static decimal? OptionalDecimal(MasterDataRawRow row, string key) => decimal.TryParse(Value(row, key), NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : null;
    protected static bool RequiredBool(MasterDataRawRow row, string key) => bool.Parse(Value(row, key)!);

    protected static void NonNegativeInt(MasterDataRawRow row, string key, string header, ICollection<MasterDataRowError> errors)
    {
        var value = Value(row, key); if (string.IsNullOrWhiteSpace(value)) return;
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed < 0) errors.Add(Error(key, header, "INVALID_VALUE", $"{header} must be a non-negative whole number.", value));
    }
    protected static void NonNegativeDecimal(MasterDataRawRow row, string key, string header, ICollection<MasterDataRowError> errors)
    {
        var value = Value(row, key); if (string.IsNullOrWhiteSpace(value)) return;
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) || parsed < 0) errors.Add(Error(key, header, "INVALID_VALUE", $"{header} must be a non-negative invariant decimal.", value));
    }
    protected static void RequiredBoolValue(MasterDataRawRow row, string key, string header, ICollection<MasterDataRowError> errors)
    { var value = Value(row, key); if (!bool.TryParse(value, out _)) errors.Add(Error(key, header, "INVALID_VALUE", $"{header} must be TRUE or FALSE.", value)); }
    protected static void Required(MasterDataRawRow row, string key, string header, int max, ICollection<MasterDataRowError> errors)
    { var value = Value(row, key); if (string.IsNullOrWhiteSpace(value)) errors.Add(Error(key, header, "REQUIRED", $"{header} is required.", value)); else Maximum(row, key, header, max, errors); }
    protected static void Maximum(MasterDataRawRow row, string key, string header, int max, ICollection<MasterDataRowError> errors)
    { var value = Value(row, key); if (value?.Trim().Length > max) errors.Add(Error(key, header, "MAX_LENGTH", $"{header} cannot exceed {max} characters.", value)); }
    protected static void ReadOnly(MasterDataRawRow row, MasterDataExistingRecord? existing, string key, string header, ICollection<MasterDataRowError> errors)
    {
        var value = Value(row, key); if (string.IsNullOrWhiteSpace(value)) return;
        if (existing is null || !existing.MaterialValues.TryGetValue(key, out var current) || !string.Equals(NormalizeMaterial(key, value), NormalizeMaterial(key, current), StringComparison.Ordinal))
            errors.Add(Error(key, header, "READ_ONLY", $"{header} cannot be changed by upload.", value));
    }
}

public sealed class CustomerMasterDataAdapter(ICustomerMasterDataService service) : PartyMasterDataAdapterBase, IMasterDataAdapter
{
    private static readonly string[] MaterialKeys = ["CustomerCode", "LegalCustomerName", "TradeName", "CustomerType", "GstNumber", "PanNumber", "BillingAddress", "ShippingAddress", "State", "StateCode", "Country", "ContactPerson", "Phone", "Email", "Industry", "PaymentTerms", "CreditPeriodDays", "CreditLimit", "Status", "ApprovalStatus", "IsActive"];
    public IMasterDataDefinition Definition { get; } = new CustomerMasterDataDefinition();
    public string NormalizeBusinessCode(string value) => PartyMasterRules.Code(value);
    public Task<IReadOnlyList<MasterDataExportRow>> ExportAsync(MasterDataExportQuery query, CancellationToken ct) => service.ExportAsync(query, ct);
    public Task<MasterDataExistingSet> LoadExistingAsync(IReadOnlyCollection<string> codes, IReadOnlyCollection<Guid> ids, CancellationToken ct) => service.LoadExistingAsync(codes, ids, ct);
    public Task<object?> LoadLookupContextAsync(IReadOnlyList<MasterDataRawRow> rows, CancellationToken ct) => IdentityContextAsync(rows, service.LoadIdentityRecordsAsync, ct);
    public IReadOnlyList<MasterDataRowError> Validate(MasterDataRawRow row, MasterDataExistingRecord? existing, object? lookupContext)
    {
        var errors = new List<MasterDataRowError>(); ValidateCommon(row, existing, "CustomerCode", "LegalCustomerName", "CustomerType", errors);
        Maximum(row, "Industry", "Industry", 120, errors); NonNegativeInt(row, "CreditPeriodDays", "Credit Period Days", errors); NonNegativeDecimal(row, "CreditLimit", "Credit Limit", errors);
        ReadOnly(row, existing, "Status", "Status", errors); ReadOnly(row, existing, "ApprovalStatus", "Approval Status", errors);
        ValidateIdentity(row, existing, lookupContext as PartyIdentityContext, "LegalCustomerName", errors); return errors;
    }
    public bool IsMateriallyEqual(MasterDataRawRow row, MasterDataExistingRecord existing) => Same(row, existing, MaterialKeys);
    public Task<MasterDataApplyResult> CreateAsync(MasterDataRawRow row, CancellationToken ct) => service.CreateAsync(Request(row, null), ct);
    public Task<MasterDataApplyResult> UpdateAsync(MasterDataExistingRecord existing, MasterDataRawRow row, uint expectedVersion, CancellationToken ct) => service.UpdateAsync(existing, Request(row, expectedVersion), ct);
    private static UpsertCustomerRequest Request(MasterDataRawRow r, uint? version)
    {
        var location = PartyMasterRules.Location(Value(r, "State"), Value(r, "StateCode"), Value(r, "Country"));
        var code = PartyMasterRules.Code(Value(r, "CustomerCode"));
        return new(code, Value(r, "LegalCustomerName")!, Optional(r, "TradeName"), Value(r, "CustomerType")!, PartyMasterRules.UpperOptional(Value(r, "GstNumber")), PartyMasterRules.UpperOptional(Value(r, "PanNumber")), Optional(r, "BillingAddress"), Optional(r, "ShippingAddress"), location.State, location.StateCode, location.Country, Optional(r, "ContactPerson"), Optional(r, "Phone"), Optional(r, "Email"), Optional(r, "Industry"), Optional(r, "PaymentTerms"), OptionalInt(r, "CreditPeriodDays"), OptionalDecimal(r, "CreditLimit"), PartyMasterRules.PortalOrganizationId(code), version);
    }
}

public sealed class VendorMasterDataAdapter(IVendorMasterDataService service) : PartyMasterDataAdapterBase, IMasterDataAdapter
{
    private static readonly string[] MaterialKeys = ["VendorCode", "LegalVendorName", "TradeName", "VendorType", "GstNumber", "PanNumber", "MsmeStatus", "MsmeNumber", "ContactPerson", "Phone", "Email", "BillingAddress", "ShippingAddress", "State", "StateCode", "Country", "MaterialServiceCategories", "ApprovedMakes", "PaymentTerms", "DeliveryTerms", "CreditPeriodDays", "AttachmentMetadataJson", "ApprovalStatus", "VendorStatus", "IsActive"];
    public IMasterDataDefinition Definition { get; } = new VendorMasterDataDefinition();
    public string NormalizeBusinessCode(string value) => PartyMasterRules.Code(value);
    public Task<IReadOnlyList<MasterDataExportRow>> ExportAsync(MasterDataExportQuery query, CancellationToken ct) => service.ExportAsync(query, ct);
    public Task<MasterDataExistingSet> LoadExistingAsync(IReadOnlyCollection<string> codes, IReadOnlyCollection<Guid> ids, CancellationToken ct) => service.LoadExistingAsync(codes, ids, ct);
    public Task<object?> LoadLookupContextAsync(IReadOnlyList<MasterDataRawRow> rows, CancellationToken ct) => IdentityContextAsync(rows, service.LoadIdentityRecordsAsync, ct);
    public IReadOnlyList<MasterDataRowError> Validate(MasterDataRawRow row, MasterDataExistingRecord? existing, object? lookupContext)
    {
        var errors = new List<MasterDataRowError>(); ValidateCommon(row, existing, "VendorCode", "LegalVendorName", "VendorType", errors);
        RequiredBoolValue(row, "MsmeStatus", "MSME Status", errors); Maximum(row, "MsmeNumber", "MSME Number", 80, errors);
        if (bool.TryParse(Value(row, "MsmeStatus"), out var msme) && msme && string.IsNullOrWhiteSpace(Value(row, "MsmeNumber"))) errors.Add(Error("MsmeNumber", "MSME Number", "REQUIRED_WHEN_MSME", "MSME Number is required when MSME Status is TRUE.", Value(row, "MsmeNumber")));
        foreach (var field in new (string Key, string Header)[] { ("MaterialServiceCategories", "Material / Service Categories"), ("ApprovedMakes", "Approved Makes"), ("DeliveryTerms", "Delivery Terms") }) Maximum(row, field.Key, field.Header, 500, errors);
        NonNegativeInt(row, "CreditPeriodDays", "Credit Period Days", errors); ReadOnly(row, existing, "ApprovalStatus", "Approval Status", errors); ReadOnly(row, existing, "VendorStatus", "Vendor Status", errors);
        ValidateIdentity(row, existing, lookupContext as PartyIdentityContext, "LegalVendorName", errors); return errors;
    }
    public bool IsMateriallyEqual(MasterDataRawRow row, MasterDataExistingRecord existing) => Same(row, existing, MaterialKeys);
    public Task<MasterDataApplyResult> CreateAsync(MasterDataRawRow row, CancellationToken ct) => service.CreateAsync(Request(row, null, null), ct);
    public Task<MasterDataApplyResult> UpdateAsync(MasterDataExistingRecord existing, MasterDataRawRow row, uint expectedVersion, CancellationToken ct)
    { existing.MaterialValues.TryGetValue("__BankMetadataJson", out var bank); return service.UpdateAsync(existing, Request(row, expectedVersion, bank), ct); }
    private static UpsertVendorRequest Request(MasterDataRawRow r, uint? version, string? bank)
    {
        var location = PartyMasterRules.Location(Value(r, "State"), Value(r, "StateCode"), Value(r, "Country"));
        return new(PartyMasterRules.Code(Value(r, "VendorCode")), Value(r, "LegalVendorName")!, Optional(r, "TradeName"), Value(r, "VendorType")!, PartyMasterRules.UpperOptional(Value(r, "GstNumber")), PartyMasterRules.UpperOptional(Value(r, "PanNumber")), RequiredBool(r, "MsmeStatus"), Optional(r, "MsmeNumber"), Optional(r, "ContactPerson"), Optional(r, "Phone"), Optional(r, "Email"), Optional(r, "BillingAddress"), Optional(r, "ShippingAddress"), location.State, location.StateCode, location.Country, Optional(r, "MaterialServiceCategories"), Optional(r, "ApprovedMakes"), Optional(r, "PaymentTerms"), Optional(r, "DeliveryTerms"), OptionalInt(r, "CreditPeriodDays"), bank, Optional(r, "AttachmentMetadataJson"), version);
    }
}
