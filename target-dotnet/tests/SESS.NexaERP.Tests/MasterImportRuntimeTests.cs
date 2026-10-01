using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.MasterData;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private static async Task ProveUnresolvedMasterImports(HttpClient client,
        DbContextOptions<NexaErpDbContext> options, TaxWorkflowUser user, Guid tdId, Guid storesId)
    {
        await using var db = new NexaErpDbContext(options);
        var batches = await db.MasterImportBatches.CountAsync();
        var results = await db.MasterImportRowResults.CountAsync();
        var staging = await db.OpeningStockImportStagingLines.CountAsync();
        var movements = await db.StockMovements.CountAsync();
        var layers = await db.FifoInventoryCostLayers.CountAsync();
        var openings = await db.OpeningStocks.CountAsync();
        user.Set(tdId, "SESS-01", "TECHNICAL_DIRECTOR");
        await Import(new UomMasterDataDefinition(), new()
        {
            ["Code"] = "IMPORTWITNESS", ["Name"] = "Import witness count",
            ["MeasurementDimension"] = "COUNT", ["QuantityPrecision"] = 0
        });
        var company = await db.Companies.Where(x => x.Code == user.OrganizationId).Select(x => x.Id).SingleAsync();
        var item = await db.Items.AsNoTracking().Where(x => x.IsActive && x.ApprovalStatus == MasterApprovalStatuses.Approved)
            .OrderBy(x => x.ItemCode).FirstAsync();
        var bin = await db.RackBins.AsNoTracking().Include(x => x.Warehouse)
            .Where(x => x.CompanyId == company && x.IsActive && x.Warehouse!.IsActive)
            .OrderBy(x => x.BinCode).FirstAsync();
        user.Set(storesId, "SESS-41", "STORES_MANAGER");
        await Import(new OpeningStockImportDefinition(), new()
        {
            ["LineReference"] = "IMPORT-AUTHORITY-WITNESS", ["ItemCode"] = item.ItemCode,
            ["WarehouseCode"] = bin.Warehouse!.WarehouseCode, ["RackBinCode"] = bin.BinCode,
            ["SerialNumber"] = item.SerialNumberTracking ? "IMPORT-WITNESS-SERIAL" : null,
            ["LotNumber"] = item.BatchTracking ? "IMPORT-WITNESS-LOT" : null,
            ["Quantity"] = 1m, ["Rate"] = 1250m
        });
        Assert.Equal(batches + 2, await db.MasterImportBatches.CountAsync());
        Assert.Equal(results + 2, await db.MasterImportRowResults.CountAsync());
        Assert.Equal(staging + 1, await db.OpeningStockImportStagingLines.CountAsync());
        Assert.Equal(movements, await db.StockMovements.CountAsync());
        Assert.Equal(layers, await db.FifoInventoryCostLayers.CountAsync());
        Assert.Equal(openings, await db.OpeningStocks.CountAsync());

        user.Set(tdId, "SESS-01", "TECHNICAL_DIRECTOR");
        await ProveWholeFileRejection(new UomMasterDataDefinition(), "ATOMICUOM", "QuantityPrecision", 9);
        await ProveWholeFileRejection(new ManufacturerImportDefinition(), "ATOMICMAKE", "Name", "");

        await ProveWholeFileRejection(new VendorMasterDataDefinition(), "ATOMICVENDOR", "Email", "invalid-address",
            new() { ["LegalVendorName"] = "Synthetic vendor for import rollback", ["VendorType"] = "Manufacturer", ["MsmeStatus"] = false, ["Country"] = "India", ["StateCode"] = "33" });
        var activeUom = await db.Uoms.Where(x => x.IsActive).OrderBy(x => x.Code).Select(x => x.Code).FirstAsync();
        var category = await db.ItemCategories.Where(x => x.IsActive).OrderBy(x => x.Code).Select(x => x.Code).FirstAsync();
        await ProveWholeFileRejection(new ItemImportDefinition(), "ATOMICITEM", "UomCode", "MISSING-UOM",
            new() { ["Name"] = "Synthetic item for import rollback", ["HsnSacCode"] = "84189900", ["GstPercentage"] = 18m, ["UomCode"] = activeUom, ["CategoryCode"] = category });

        async Task ProveWholeFileRejection(IMasterDataDefinition definition, string prefix, string badColumn, object badValue,
            Dictionary<string, object?>? supplied = null)
        {
            var good = supplied ?? new Dictionary<string, object?> { ["Name"] = "Synthetic import witness", ["MeasurementDimension"] = "COUNT", ["QuantityPrecision"] = 0 };
            good[definition.BusinessCodeColumnKey] = prefix + "1";
            var bad = new Dictionary<string, object?>(good) { [definition.BusinessCodeColumnKey] = prefix + "2", [badColumn] = badValue };
            async Task<MasterDataImportResult> Upload(Dictionary<string, object?>[] values)
            {
                using var form = new MultipartFormDataContent();
                var bytes = new MasterDataWorkbookService().Create(definition, values.Select(x => new MasterDataExportRow(x)).ToArray(), DateTimeOffset.UtcNow);
                form.Add(new ByteArrayContent(bytes), "File", "atomic.xlsx");
                form.Add(new StringContent(MasterDataImportModes.RejectEntireFile), "Mode");
                form.Add(new StringContent(Guid.NewGuid().ToString()), "IdempotencyKey");
                using var response = await client.PostAsync("/api/v1/master-data/" + definition.MasterKey + "/import", form);
                Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
                return (await response.Content.ReadFromJsonAsync<MasterDataImportResult>())!;
            }
            var rejected = await Upload([good, bad]);
            Assert.Equal(MasterDataImportStatuses.Rejected, rejected.Status);
            Assert.Equal(0, rejected.CreatedRows);
            Assert.Equal(0, rejected.UpdatedRows);
            Assert.Equal(1, rejected.RejectedRows);
            Assert.Equal(1, rejected.NotImportedRows);
            var duplicate = await Upload([good, new Dictionary<string, object?>(good)]);
            Assert.Equal(0, duplicate.CreatedRows);
            Assert.Equal(2, duplicate.RejectedRows);
            // The same good code must still be new after both rejected files.
            var accepted = await Upload([good]);
            Assert.Equal(MasterDataImportStatuses.Completed, accepted.Status);
            Assert.Equal(1, accepted.CreatedRows);
            Assert.Equal(0, accepted.InvalidRows);
        }

        async Task Import(IMasterDataDefinition definition, Dictionary<string, object?> values)
        {
            Assert.Equal("none", user.ForRequest().RoleCode);
            var bytes = new MasterDataWorkbookService().Create(definition, [new(values)], DateTimeOffset.UtcNow);
            Guid? firstBatch = null;
            for (var attempt = 0; attempt < 2; attempt++)
            {
                using var form = new MultipartFormDataContent();
                form.Add(new ByteArrayContent(bytes), "File", "authority-witness.xlsx");
                form.Add(new StringContent(MasterDataImportModes.RejectEntireFile), "Mode");
                form.Add(new StringContent("authority-witness-" + definition.MasterKey), "IdempotencyKey");
                using var response = await client.PostAsync("/api/v1/master-data/" + definition.MasterKey + "/import", form);
                var body = await response.Content.ReadAsStringAsync();
                Assert.True(response.IsSuccessStatusCode, $"{definition.MasterKey}: {response.StatusCode} {body}");
                var result = await response.Content.ReadFromJsonAsync<MasterDataImportResult>();
                Assert.NotNull(result);
                Assert.True(result.CreatedRows == 1 && result.InvalidRows == 0, body);
                Assert.Equal(1, result.TotalRows);
                if (firstBatch.HasValue) Assert.Equal(firstBatch.Value, result.BatchId);
                firstBatch = result.BatchId;
            }
        }
    }
}