using ClosedXML.Excel;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Infrastructure.MasterData;

namespace SESS.NexaERP.Tests;

public sealed class CustomerVendorMasterDataAdapterTests
{
    [Fact]
    public void CustomerContractIsSharedAndExcludesPortalAndRelationshipCodes()
    {
        var definition = new CustomerMasterDataDefinition();
        Assert.Equal("customers", definition.MasterKey);
        Assert.Equal("CustomerCode", definition.BusinessCodeColumnKey);
        Assert.DoesNotContain(definition.Columns, x => x.Key == "PortalOrganizationId");
        Assert.DoesNotContain(definition.Columns, x => x.Key.Contains("AssignedSupplierCode", StringComparison.Ordinal));
        Assert.Equal(PagePermissionActions.ViewCommercialValues, definition.SensitiveResultPermission?.Permission);
    }

    [Fact]
    public void VendorContractAndWorkbookExcludeBankMetadataAndExplainWhy()
    {
        var definition = new VendorMasterDataDefinition();
        Assert.DoesNotContain(definition.Columns, x => x.Key.Contains("Bank", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(definition.WorkbookGuideNotes, x => x.Contains("deliberately excluded", StringComparison.OrdinalIgnoreCase)
            && x.Contains(PagePermissionActions.ViewCommercialValues, StringComparison.Ordinal));
        var bytes = new MasterDataWorkbookService().Create(definition, [], DateTimeOffset.Parse("2026-08-29T00:00:00Z"));
        using var stream = new MemoryStream(bytes); using var workbook = new XLWorkbook(stream);
        Assert.DoesNotContain(workbook.Worksheet("Data").Row(1).CellsUsed(), x => x.GetString().Contains("Bank", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(workbook.Worksheet("Column Guide").CellsUsed(), x => x.GetString().Contains("Bank details", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CustomerDefaultsIndiaDerivesStateAndServerDerivesPortalIdentity()
    {
        var service = new CustomerService(); var adapter = new CustomerMasterDataAdapter(service);
        var row = CustomerRow(state: "Tamil Nadu", stateCode: null, country: null);
        Assert.Empty(adapter.Validate(row, null, await adapter.LoadLookupContextAsync([row], default)));
        await adapter.CreateAsync(row, default);
        Assert.Equal("India", service.Request!.Country);
        Assert.Equal("33", service.Request.StateCode);
        Assert.Equal("CUST-001", service.Request.PortalOrganizationId);
    }

    [Fact]
    public async Task CustomerOptionalTaxIdsAreAllowedButPresentValuesAreValidated()
    {
        var adapter = new CustomerMasterDataAdapter(new CustomerService());
        var blank = CustomerRow();
        Assert.Empty(adapter.Validate(blank, null, await adapter.LoadLookupContextAsync([blank], default)));
        var invalid = CustomerRow(gst: "BAD", pan: "BAD");
        var errors = adapter.Validate(invalid, null, await adapter.LoadLookupContextAsync([invalid], default));
        Assert.Contains(errors, x => x.ColumnKey == "GstNumber" && x.Code == "INVALID_FORMAT");
        Assert.Contains(errors, x => x.ColumnKey == "PanNumber" && x.Code == "INVALID_FORMAT");
    }

    [Fact]
    public void ReuploadWithBlankDerivedFieldsIsMateriallyUnchanged()
    {
        var adapter = new CustomerMasterDataAdapter(new CustomerService());
        var row = CustomerRow(state: "Tamil Nadu", stateCode: null, country: null);
        var values = new CustomerMasterDataDefinition().Columns.ToDictionary(x => x.Key, x => row.Values.TryGetValue(x.Key, out var value) ? value : null, StringComparer.Ordinal);
        values["Country"] = "India"; values["StateCode"] = "33";
        var existing = new MasterDataExistingRecord(Guid.NewGuid(), "CUST-001", "CUST-001", 1, values);
        Assert.True(adapter.IsMateriallyEqual(row, existing));
    }

    [Fact]
    public async Task StateAndGstinMustAgreeWithDerivedStateCode()
    {
        var adapter = new CustomerMasterDataAdapter(new CustomerService());
        var row = CustomerRow(state: "Tamil Nadu", stateCode: "29", gst: "29ABCDE1234F1Z5");
        var errors = adapter.Validate(row, null, await adapter.LoadLookupContextAsync([row], default));
        Assert.Contains(errors, x => x.Code == "STATE_CODE_MISMATCH");
    }

    [Fact]
    public async Task VendorRequiresMsmeNumberWhenMsmeStatusIsTrue()
    {
        var adapter = new VendorMasterDataAdapter(new VendorService()); var row = VendorRow(msme: "TRUE", msmeNumber: null);
        var errors = adapter.Validate(row, null, await adapter.LoadLookupContextAsync([row], default));
        Assert.Contains(errors, x => x.ColumnKey == "MsmeNumber" && x.Code == "REQUIRED_WHEN_MSME");
    }

    [Fact]
    public async Task VendorUpdatePreservesBankMetadataWhileWorkbookCannotSupplyIt()
    {
        var service = new VendorService(); var adapter = new VendorMasterDataAdapter(service); var id = Guid.NewGuid();
        var values = VendorRow().Values.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
        values["__BankMetadataJson"] = """{"account":"protected"}""";
        var existing = new MasterDataExistingRecord(id, "VEND-001", "VEND-001", 7, values);
        await adapter.UpdateAsync(existing, VendorRow(), 7, default);
        Assert.Equal("""{"account":"protected"}""", service.Request!.BankMetadataJson);
        Assert.Equal((uint)7, service.Request.Version);
    }

    [Fact]
    public async Task IdentityChecksAreBulkLoadedOnceAndReportedPerColumn()
    {
        var service = new CustomerService
        {
            Identities = [new(Guid.NewGuid(), "CUST-OLD", "33ABCDE1234F1Z5", null, "Existing Customer")]
        };
        var adapter = new CustomerMasterDataAdapter(service); var row = CustomerRow(gst: "33ABCDE1234F1Z5", state: "Tamil Nadu");
        var context = await adapter.LoadLookupContextAsync([row, CustomerRow(gst: "33ABCDE1234F1Z5", state: "Tamil Nadu") with { SourceRowNumber = 3 }], default);
        var errors = adapter.Validate(row, null, context);
        Assert.Equal(1, service.IdentityLoads);
        Assert.Contains(errors, x => x.ColumnKey == "GstNumber" && x.Code == "DUPLICATE_IDENTITY");
        Assert.Contains(errors, x => x.ColumnKey == "GstNumber" && x.Code == "DUPLICATE_IN_FILE");
    }

    private static MasterDataRawRow CustomerRow(string? state = null, string? stateCode = null, string? country = null, string? gst = null, string? pan = null) =>
        Row(new Dictionary<string, string?> { ["CustomerCode"] = "CUST-001", ["LegalCustomerName"] = "Trial Customer", ["CustomerType"] = "INDUSTRIAL", ["State"] = state, ["StateCode"] = stateCode, ["Country"] = country, ["GstNumber"] = gst, ["PanNumber"] = pan });
    private static MasterDataRawRow VendorRow(string msme = "FALSE", string? msmeNumber = null) =>
        Row(new Dictionary<string, string?> { ["VendorCode"] = "VEND-001", ["LegalVendorName"] = "Trial Vendor", ["VendorType"] = "MATERIAL", ["MsmeStatus"] = msme, ["MsmeNumber"] = msmeNumber });
    private static MasterDataRawRow Row(IReadOnlyDictionary<string, string?> values) => new(2, values);

    private sealed class CustomerService : ICustomerMasterDataService
    {
        public UpsertCustomerRequest? Request { get; private set; }
        public IReadOnlyList<MasterDataPartyIdentityRecord> Identities { get; init; } = [];
        public int IdentityLoads { get; private set; }
        public Task<IReadOnlyList<MasterDataExportRow>> ExportAsync(MasterDataExportQuery query, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<MasterDataExportRow>>([]);
        public Task<MasterDataExistingSet> LoadExistingAsync(IReadOnlyCollection<string> normalizedCodes, IReadOnlyCollection<Guid> recordIds, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<MasterDataPartyIdentityRecord>> LoadIdentityRecordsAsync(IReadOnlyCollection<string> gstins, IReadOnlyCollection<string> pans, CancellationToken cancellationToken) { IdentityLoads++; return Task.FromResult(Identities); }
        public Task<MasterDataApplyResult> CreateAsync(UpsertCustomerRequest request, CancellationToken cancellationToken) { Request = request; return Task.FromResult(new MasterDataApplyResult(Guid.NewGuid(), 0)); }
        public Task<MasterDataApplyResult> UpdateAsync(MasterDataExistingRecord existing, UpsertCustomerRequest request, CancellationToken cancellationToken) { Request = request; return Task.FromResult(new MasterDataApplyResult(existing.Id, existing.Version + 1)); }
    }

    private sealed class VendorService : IVendorMasterDataService
    {
        public UpsertVendorRequest? Request { get; private set; }
        public Task<IReadOnlyList<MasterDataExportRow>> ExportAsync(MasterDataExportQuery query, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<MasterDataExportRow>>([]);
        public Task<MasterDataExistingSet> LoadExistingAsync(IReadOnlyCollection<string> normalizedCodes, IReadOnlyCollection<Guid> recordIds, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<MasterDataPartyIdentityRecord>> LoadIdentityRecordsAsync(IReadOnlyCollection<string> gstins, IReadOnlyCollection<string> pans, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<MasterDataPartyIdentityRecord>>([]);
        public Task<MasterDataApplyResult> CreateAsync(UpsertVendorRequest request, CancellationToken cancellationToken) { Request = request; return Task.FromResult(new MasterDataApplyResult(Guid.NewGuid(), 0)); }
        public Task<MasterDataApplyResult> UpdateAsync(MasterDataExistingRecord existing, UpsertVendorRequest request, CancellationToken cancellationToken) { Request = request; return Task.FromResult(new MasterDataApplyResult(existing.Id, existing.Version + 1)); }
    }
}
