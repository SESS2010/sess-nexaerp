using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Infrastructure.MasterData;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class MasterDataImportFrameworkTests
{
    private readonly UomMasterDataAdapter adapter = new(new NullUomService());

    [Fact]
    public void UomPilotDefinesOneGenericSevenColumnContract()
    {
        var definition = adapter.Definition;
        Assert.Equal("uoms", definition.MasterKey);
        Assert.Equal("masters.uoms", definition.PageKey);
        Assert.Equal("Code", definition.BusinessCodeColumnKey);
        Assert.Null(definition.SensitiveResultPermission);
        Assert.Equal(["RecordId", "Version", "Code", "Name", "MeasurementDimension", "QuantityPrecision", "IsActive"],
            definition.Columns.Select(x => x.Key));
        Assert.False(definition.Columns.Single(x => x.Key == "RecordId").Editable);
        Assert.False(definition.Columns.Single(x => x.Key == "Version").Editable);
        Assert.False(definition.Columns.Single(x => x.Key == "IsActive").Editable);
    }

    [Fact]
    public void WorkbookHasExactSheetsGuideMetadataAndTextCodes()
    {
        var service = new MasterDataWorkbookService();
        var bytes = service.Create(adapter.Definition,
            [new(new Dictionary<string, object?> { ["Code"] = "001", ["Name"] = "Number", ["MeasurementDimension"] = "COUNT", ["QuantityPrecision"] = 0, ["IsActive"] = true })],
            DateTimeOffset.Parse("2026-08-29T00:00:00Z"));

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        Assert.Equal(["Data", "Column Guide", "_Metadata"], workbook.Worksheets.Select(x => x.Name));
        Assert.Equal(XLWorksheetVisibility.VeryHidden, workbook.Worksheet("_Metadata").Visibility);
        Assert.Equal("uoms", workbook.Worksheet("_Metadata").Cell(1, 2).GetString());
        Assert.Equal("001", workbook.Worksheet("Data").Cell(2, 3).GetString());
        Assert.Equal("@", workbook.Worksheet("Data").Column(3).Style.NumberFormat.Format);
        Assert.Equal(adapter.Definition.Columns.Select(x => x.Header),
            Enumerable.Range(1, 7).Select(x => workbook.Worksheet("Data").Cell(1, x).GetString()));
    }

    [Fact]
    public void WorkbookParserRejectsFormulaCellsAndRowsBeyondConfiguredLimit()
    {
        var service = new MasterDataWorkbookService();
        var bytes = service.Create(adapter.Definition,
            [Row("ONE", "One"), Row("TWO", "Two")], DateTimeOffset.UtcNow);
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        workbook.Worksheet("Data").Cell(2, 4).FormulaA1 = "=1+1";
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        Assert.Contains("Formula cells are not allowed", Assert.Throws<MasterDataValidationException>(
            () => service.Read(stream.ToArray(), adapter.Definition, 1000)).Message);
        Assert.Contains("maximum of 1", Assert.Throws<MasterDataValidationException>(
            () => service.Read(bytes, adapter.Definition, 1)).Message);
    }

    [Fact]
    public void ReuploadOfIdenticalCreateWorkbookIsNoChangeWithoutIdentityFields()
    {
        var existing = Existing("NOS", 4);
        var result = Prepare(Raw("NOS", "Numbers", "COUNT", "0"), existing);
        Assert.Empty(result.Errors);
        Assert.Equal(MasterDataIntendedActions.NoChange, result.IntendedAction);
    }

    [Fact]
    public void ExistingChangeRequiresRecordIdAndVersion()
    {
        var result = Prepare(Raw("NOS", "Pieces", "COUNT", "0"), Existing("NOS", 4));
        Assert.Equal(MasterDataIntendedActions.Update, result.IntendedAction);
        Assert.Contains(result.Errors, x => x.Code == "RECORD_ID_REQUIRED");
        Assert.Contains(result.Errors, x => x.Code == "VERSION_REQUIRED");
    }

    [Fact]
    public void CurrentRecordIdAndVersionPermitUpdateButStaleVersionIsRejected()
    {
        var existing = Existing("NOS", 4);
        var current = Prepare(Raw("NOS", "Pieces", "COUNT", "0", existing.Id, 4), existing);
        Assert.Empty(current.Errors);
        Assert.Equal(MasterDataIntendedActions.Update, current.IntendedAction);
        var stale = Prepare(Raw("NOS", "Pieces", "COUNT", "0", existing.Id, 3), existing);
        Assert.Contains(stale.Errors, x => x.Code == "STALE_VERSION");
    }

    [Fact]
    public void RecordIdentityCannotRenameBusinessCode()
    {
        var existing = Existing("NOS", 4);
        var set = Set(existing);
        var result = EfMasterDataTransferService.Prepare(Raw("PCS", "Numbers", "COUNT", "0", existing.Id, 4), adapter, set, null, new HashSet<string>());
        Assert.Contains(result.Errors, x => x.Code == "BUSINESS_CODE_IMMUTABLE");
        Assert.NotEqual(MasterDataIntendedActions.Create, result.IntendedAction);
    }

    [Fact]
    public void DuplicateBusinessCodeAndInvalidPrecisionAreColumnErrorsWithAttemptedValues()
    {
        var result = EfMasterDataTransferService.Prepare(Raw("NOS", "Numbers", "COUNT", "7"), adapter,
            new(new Dictionary<string, MasterDataExistingRecord>(), new Dictionary<Guid, MasterDataExistingRecord>()),
            null, new HashSet<string>(["NOS"], StringComparer.Ordinal));
        Assert.Contains(result.Errors, x => x.Code == "DUPLICATE_IN_FILE" && x.ColumnKey == "Code" && x.AttemptedValue == "NOS");
        Assert.Contains(result.Errors, x => x.Code == "INVALID_VALUE" && x.ColumnKey == "QuantityPrecision" && x.AttemptedValue == "7");
    }

    [Fact]
    public void PersistenceModelHasNinetyDayRetentionAndAppendOnlyResultRelationship()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var db = new NexaErpDbContext(options);
        var batch = db.Model.FindEntityType("SESS.NexaERP.Domain.Masters.MasterImportBatch")!;
        var row = db.Model.FindEntityType("SESS.NexaERP.Domain.Masters.MasterImportRowResult")!;
        Assert.Equal("master_import_batches", batch.GetTableName());
        Assert.Equal("CURRENT_TIMESTAMP + INTERVAL '90 days'", batch.FindProperty("RetentionExpiresAt")!.GetDefaultValueSql());
        Assert.Equal("master_import_row_results", row.GetTableName());
        Assert.Equal(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict, row.GetForeignKeys().Single(x => x.PrincipalEntityType == batch).DeleteBehavior);
    }

    [Fact]
    public async Task SensitiveVendorRowsRequireCommercialValuesInAdditionToAuditHistory()
    {
        var denied = new PermissionService(("masters.vendors", PagePermissionActions.ViewAuditHistory));
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            EfMasterDataTransferService.RequireHistoricalPermissionsAsync(
                new SensitiveVendorDefinition(), false, ["AUDITOR"], denied, CancellationToken.None));
        Assert.Contains("commercial-values", exception.Message);
        Assert.Contains(denied.Requests, x => x == ("masters.vendors", PagePermissionActions.ViewCommercialValues));

        var allowed = new PermissionService(
            ("masters.vendors", PagePermissionActions.ViewAuditHistory),
            ("masters.vendors", PagePermissionActions.ViewCommercialValues));
        await EfMasterDataTransferService.RequireHistoricalPermissionsAsync(
            new SensitiveVendorDefinition(), false, ["AUDITOR"], allowed, CancellationToken.None);
    }

    private MasterDataPreparedRow Prepare(MasterDataRawRow row, MasterDataExistingRecord existing) =>
        EfMasterDataTransferService.Prepare(row, adapter, Set(existing), null, new HashSet<string>());

    private static MasterDataExistingSet Set(MasterDataExistingRecord record) => new(
        new Dictionary<string, MasterDataExistingRecord>(StringComparer.Ordinal) { [record.NormalizedBusinessCode] = record },
        new Dictionary<Guid, MasterDataExistingRecord> { [record.Id] = record });

    private static MasterDataExistingRecord Existing(string code, uint version)
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        return new(id, code, code, version, new Dictionary<string, string?>
        {
            ["Code"] = code, ["Name"] = "Numbers", ["MeasurementDimension"] = "COUNT",
            ["QuantityPrecision"] = "0", ["IsActive"] = "TRUE"
        });
    }

    private static MasterDataRawRow Raw(string code, string name, string dimension, string precision, Guid? id = null, uint? version = null) =>
        new(2, new Dictionary<string, string?>
        {
            ["RecordId"] = id?.ToString(), ["Version"] = version?.ToString(), ["Code"] = code,
            ["Name"] = name, ["MeasurementDimension"] = dimension, ["QuantityPrecision"] = precision, ["IsActive"] = "TRUE"
        });

    private static MasterDataExportRow Row(string code, string name) => new(new Dictionary<string, object?>
    {
        ["Code"] = code, ["Name"] = name, ["MeasurementDimension"] = "COUNT", ["QuantityPrecision"] = 0, ["IsActive"] = true
    });

    private sealed class NullUomService : IUomMasterService
    {
        public Task<IReadOnlyList<UomSummary>> ExportAsync(MasterDataExportQuery query, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MasterDataExistingSet> LoadExistingAsync(IReadOnlyCollection<string> normalizedCodes, IReadOnlyCollection<Guid> recordIds, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<UomSummary> CreateAsync(UpsertUomMasterRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<UomSummary> UpdateAsync(Guid id, UpsertUomMasterRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class SensitiveVendorDefinition : IMasterDataDefinition
    {
        public string MasterKey => "vendors";
        public int TemplateVersion => 1;
        public string PageKey => "masters.vendors";
        public string BusinessCodeColumnKey => "Code";
        public IReadOnlyList<string> OperationalRolePriority => [];
        public MasterDataSensitivePermission? SensitiveResultPermission =>
            new("masters.vendors", PagePermissionActions.ViewCommercialValues);
        public IReadOnlyList<MasterDataColumnDefinition> Columns => [];
    }

    private sealed class PermissionService(params (string Page, string Permission)[] allowed) : IPagePermissionService
    {
        private readonly HashSet<(string Page, string Permission)> grants = allowed.ToHashSet();
        public List<(string Page, string Permission)> Requests { get; } = [];
        public Task<bool> HasPermissionAsync(IReadOnlyCollection<string> roleCodes, string pageKey, string permission, CancellationToken cancellationToken)
        {
            Requests.Add((pageKey, permission));
            return Task.FromResult(grants.Contains((pageKey, permission)));
        }
    }
}
