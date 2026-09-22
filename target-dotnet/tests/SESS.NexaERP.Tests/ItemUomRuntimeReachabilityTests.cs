using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Inventory;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private static async Task ProveSeededUomItemEditing(HttpClient client,
        DbContextOptions<NexaErpDbContext> options, TaxWorkflowUser user, Guid tdId, Guid categoryId)
    {
        user.Set(tdId, "SESS-01", "TECHNICAL_DIRECTOR");
        Assert.NotEqual(Guid.Empty, Assert.Single(user.EffectiveRoleAssignments).AssignmentId);
        // Explicit boundary cases are API-created test data, not claimed as seeded units.
        foreach (var precision in Enumerable.Range(0, 7))
            await Post<UomSummary>(client, "/api/v1/masters/uoms",
                new UpsertUomMasterRequest("PRECISION-" + precision, "Precision boundary " + precision, "LENGTH", null, precision));
        foreach (var precision in new[] { -1, 7 })
        {
            using var invalid = await client.PostAsJsonAsync("/api/v1/masters/uoms",
                new UpsertUomMasterRequest("INVALID-" + precision, "Invalid precision " + precision, "LENGTH", null, precision));
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        }
        await using var db = new NexaErpDbContext(options);
        var uoms = await db.Uoms.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Code).ToListAsync();
        Assert.NotEmpty(uoms);
        Assert.Contains(uoms, x => x.QuantityPrecision == 0);
        Assert.Contains(uoms, x => x.QuantityPrecision is > 0 and < 6);
        Assert.Contains(uoms, x => x.QuantityPrecision == 6);
        foreach (var uom in uoms)
        {
            Assert.InRange(uom.QuantityPrecision, 0, 6);
            Assert.False(string.IsNullOrWhiteSpace(uom.MeasurementDimension));
            var code = "UOM-EDIT-" + uom.Code;
            var request = new UpsertItemRequest(code, "UOM compatibility " + uom.Code,
                "Master precision must survive an item edit", categoryId, null, "CONSUMABLE", "CONSUMABLE", false,
                uom.Code, null, null, null, null, 0m, null, null, false, false, false, false,
                0m, 10m, 1m, null, null, null, null, null, null, null, null);
            var created = await Post<ItemDetail>(client, "/api/v1/inventory/items", request);
            var detail = await Get<ItemDetail>(client, "/api/v1/inventory/items/" + code);
            Assert.Equal(created.Id, detail.Id);
            Assert.Equal(uom.Id, detail.BaseUomId);
            using var response = await client.PutAsJsonAsync("/api/v1/inventory/items/" + code,
                request with { DetailedDescription = "Edited without changing quantity precision", Version = detail.Version });
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"UOM {uom.Code}, precision {uom.QuantityPrecision}: {response.StatusCode} {body}");
            var edited = await Get<ItemDetail>(client, "/api/v1/inventory/items/" + code);
            Assert.Equal("Edited without changing quantity precision", edited.DetailedDescription);
            Assert.Equal(uom.Id, edited.BaseUomId);
            Assert.Equal(uom.QuantityPrecision, await db.Uoms.Where(x => x.Id == uom.Id).Select(x => x.QuantityPrecision).SingleAsync());
        }
    }
}
