using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Inventory;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Application.Rev869A;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private static async Task ProveLegacyImportedCategoryRepair(DisposablePostgreSql server, HttpClient client,
        DbContextOptions<NexaErpDbContext> options, TaxWorkflowUser user, Guid qcId, Guid tdId)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        var directory = Path.Combine(FindRepositoryRoot(), "database", "postgresql");
        var correctedImport = await File.ReadAllTextAsync(Path.Combine(directory, "legacy-item-import-2026-08-29.sql"));
        // Reproduce the old import, not an alleged customer backup or field migration state.
        var oldImport = correctedImport.Replace("'ELE'", "'ELECTRICALS'", StringComparison.Ordinal)
            .Replace("'FAB'", "'FABRICATION'", StringComparison.Ordinal)
            .Replace("'REF'", "'REFRIGERATION'", StringComparison.Ordinal);
        server.Execute("legacy-category-old-import.sql", oldImport);
        var repair = await File.ReadAllTextAsync(Path.Combine(directory, "reconcile-legacy-item-categories.sql"));
        await using var db = new NexaErpDbContext(options);
        var count = await db.Items.CountAsync(x => x.CreatedBy == "EXCEL_IMPORT");
        var expected = System.Text.RegularExpressions.Regex.Matches(correctedImport, @"(?m)^INSERT INTO advance\.items ").Count;
        Assert.True(expected > 1000, "The regression must exercise the real import catalogue.");
        Assert.Equal(expected, count);
        Assert.Equal(count, await db.Items.CountAsync(x => x.CreatedBy == "EXCEL_IMPORT" && x.Category!.Code.Length != 3));
        var receiptSnapshots = await db.GoodsReceiptLines.AsNoTracking().OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.ItemCategoryIdSnapshot, x.ItemCategoryCodeSnapshot }).ToArrayAsync();
        user.Set(qcId, "SESS-33", "QC_MANAGER");
        const string policies = "/api/v1/rev869a/configuration/qc-inspection-policies";
        var policy = await Post<JsonElement>(client, policies, new CreateQcInspectionPolicyRequest(
            "SESS_PVT_LTD", null, "REFRIGERATION", "IMPORT_REPAIR_GUARD", "TRIAL-NOS", 0, 1,
            "Legacy category conflict witness", 1, new(2026, 1, 1), null, "Prove repair refuses an active legacy policy"));
        server.AssertRejected("legacy-category-conflict.sql", repair, "Active legacy-category QC policy");
        Assert.Equal(0, await db.AuditLogs.CountAsync(x => x.Action == "ReconcileImportedCategory"));
        Assert.Equal(count, await db.Items.CountAsync(x => x.CreatedBy == "EXCEL_IMPORT" && x.Category!.Code.Length != 3));
        user.Set(tdId, "SESS-01", "TECHNICAL_DIRECTOR");
        await Post<JsonElement>(client, policies + "/" + policy.GetProperty("Id").GetGuid() + "/reject",
            new MasterActionRequest("Reject the mistaken legacy category criterion before repair", policy.GetProperty("Version").GetUInt32()),
            "reject-legacy-category-policy");
        server.Execute("legacy-category-repair.sql", repair);
        var canonical = new[] { "ELE", "FAB", "FAS", "MEC", "PLC", "REF" };
        Assert.Equal(count, await db.Items.CountAsync(x => x.CreatedBy == "EXCEL_IMPORT" && canonical.Contains(x.Category!.Code)));
        Assert.Equal(count, await db.AuditLogs.CountAsync(x => x.Action == "ReconcileImportedCategory"));
        Assert.Equal(count, await db.Items.CountAsync(x => x.CreatedBy == "EXCEL_IMPORT" && x.Version == 1));
        var legacyAliases = new[] { "ELECTRICALS", "FABRICATION", "REFRIGERATION" };
        Assert.Equal(3, await db.ItemCategories.CountAsync(x => legacyAliases.Contains(x.Code) && !x.IsActive));
        Assert.Equal(3, await db.AuditLogs.CountAsync(x => x.Action == "RetireImportedCategoryAlias"));
        var choices = await Get<PagedResponse<ReferenceMasterSummary>>(client,
            "/api/v1/masters/item-categories?isActive=true&pageSize=200");
        Assert.DoesNotContain(choices.Items, x => legacyAliases.Contains(x.Code));
        server.Execute("legacy-category-repair-replay.sql", repair);
        server.Execute("legacy-category-corrected-reimport.sql", correctedImport);
        Assert.Equal(count, await db.Items.CountAsync(x => x.CreatedBy == "EXCEL_IMPORT"));
        Assert.Equal(count, await db.AuditLogs.CountAsync(x => x.Action == "ReconcileImportedCategory"));
        Assert.Equal(3, await db.AuditLogs.CountAsync(x => x.Action == "RetireImportedCategoryAlias"));
        Assert.Equal(receiptSnapshots, await db.GoodsReceiptLines.AsNoTracking().OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.ItemCategoryIdSnapshot, x.ItemCategoryCodeSnapshot }).ToArrayAsync());
        // Every imported master must be editable through real seeded authority, not just parse as SQL.
        var importedCodes = await db.Items.Where(x => x.CreatedBy == "EXCEL_IMPORT").OrderBy(x => x.ItemCode).Select(x => x.ItemCode).ToListAsync();
        // Each request edits a different item. The selected TD identity/assignment is
        // stable for this entire block; every request gets its own scoped DbContext.
        await Parallel.ForEachAsync(importedCodes, new ParallelOptions { MaxDegreeOfParallelism = 16 }, async (code, _) =>
        {
            var itemPath = "/api/v1/inventory/item-by-code?code=" + Uri.EscapeDataString(code);
            var detail = await Get<ItemDetail>(client, itemPath);
            var request = JsonSerializer.Deserialize<UpsertItemRequest>(JsonSerializer.Serialize(detail))!
                with { DetailedDescription = detail.DetailedDescription + " [edit witness]" };
            using var edited = await client.PutAsJsonAsync(itemPath, request);
            Assert.True(edited.StatusCode == HttpStatusCode.OK,
                $"Imported item {code} is not editable: {edited.StatusCode} {await edited.Content.ReadAsStringAsync()}");
        });
        timer.Stop();
        var evidence = Path.Combine(FindRepositoryRoot(), "local-evidence", "category-import");
        Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence, "item-import-reachability.json"),
            JsonSerializer.Serialize(new { ImportedItems = count, AllEditedThroughApi = true,
                RepairAudited = true, RetiredImportAliases = 3, RetiredAliasesAbsentFromActiveChoices = true, ReplayIdempotent = true, ReceiptSnapshotsUnchanged = true,
                ElapsedSeconds = timer.Elapsed.TotalSeconds }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
