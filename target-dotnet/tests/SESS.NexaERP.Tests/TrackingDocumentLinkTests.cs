using System.Text.Json;
using SESS.NexaERP.Application.Tracking;
using SESS.NexaERP.Infrastructure.Tracking;

namespace SESS.NexaERP.Tests;

public sealed class TrackingDocumentLinkTests
{
    private static readonly Guid Id = Guid.Parse("11111111-2222-4333-8444-555555555555");

    [Theory]
    [InlineData("PR", "/purchase/requisitions/PO%2FSPVT%2F26-27%2F000012")]
    [InlineData("RFQ", "/purchase/rfqs/PO%2FSPVT%2F26-27%2F000012")]
    [InlineData("COMPARISON", "/purchase/comparisons/PO%2FSPVT%2F26-27%2F000012")]
    [InlineData("PO", "/purchase/purchase-orders/PO%2FSPVT%2F26-27%2F000012")]
    [InlineData("QUOTATION", "/purchase/quotations")]
    [InlineData("GATE_ENTRY", "/stores/gate-entries/11111111-2222-4333-8444-555555555555")]
    [InlineData("GRN", "/stores/goods-receipts/11111111-2222-4333-8444-555555555555")]
    [InlineData("QC", "/qc/inspections")]
    [InlineData("MIR", "/stores/material-issue-requests/11111111-2222-4333-8444-555555555555")]
    [InlineData("VENDOR_BILL", "/accounts/vendor-bills/11111111-2222-4333-8444-555555555555")]
    public void Pending_projection_preserves_identity_and_replaces_legacy_links(string type, string expected)
    {
        var source = new EfTrackingService.PendingJson(type, "trial-queue", Id, "PO/SPVT/26-27/000012", "Pending",
            null, null, null, null, null, null, DateTimeOffset.UnixEpoch, 2, 1, true, "/obsolete/route");
        var row = source.ToRow();
        Assert.Equal((type, Id, source.Number), (row.DocType, row.DocumentId, row.Number));
        Assert.Equal(expected, row.Link);
        var history = new TrackingHistory(type, Id, source.Number, "Pending", null, null, null, null, []);
        Assert.Equal(expected, history.Link);
        var json = JsonSerializer.SerializeToElement(history);
        Assert.Equal(Id, json.GetProperty("DocumentId").GetGuid());
        Assert.Equal(source.Number, json.GetProperty("Number").GetString());
    }

    [Fact]
    public void Committed_mocks_cover_every_document_type_and_match_the_wire_contract()
    {
        var folder = Path.Combine(AdvanceMigrationSqlSyntaxTests.FindRepositoryRoot(), "docs", "installation", "tracking-mocks");
        T Read<T>(string name) => JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(folder, name)))!;
        var pending = Read<TrackingPendingPage>("pending.json");
        Assert.Equal(pending.Items.Count, pending.Total);
        Assert.Equal(TrackingDocTypes.All.Order(), pending.Items.Select(x => x.DocType).Distinct().Order());
        Assert.All(pending.Items, row =>
        {
            Assert.NotEqual(Guid.Empty, row.DocumentId);
            Assert.False(string.IsNullOrWhiteSpace(row.Number));
            Assert.Equal(TrackingDocumentLinks.Document(row.DocType, row.DocumentId, row.Number), row.Link);
        });
        using var summary = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "summary.json")));
        foreach (var tile in summary.RootElement.EnumerateArray())
        {
            var queue = tile.GetProperty("Queue").GetString()!;
            Assert.Equal(pending.Items.Count(x => x.Queue == queue), tile.GetProperty("Count").GetInt32());
            Assert.Equal(TrackingDocumentLinks.Queue(queue), tile.GetProperty("Link").GetString());
        }
        foreach (var name in new[] { "history-po.json", "history-qc.json" })
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, name)));
            var history = Read<TrackingHistory>(name);
            Assert.Contains(pending.Items, row => row.DocType == history.DocType && row.DocumentId == history.DocumentId && row.Number == history.Number);
            Assert.Equal(history.Link, document.RootElement.GetProperty("Link").GetString());
        }
    }

    [Fact]
    public void Number_is_a_single_encoded_segment_without_double_decoding()
    {
        Assert.Equal("/purchase/purchase-orders/PO%2FA%20B%3F%23%252F", TrackingDocumentLinks.Document("PO", Id, "PO/A B?#%2F"));
        Assert.Equal("/tracking/pending", TrackingDocumentLinks.Document("UNKNOWN", Id, "//outside.example"));
    }

    [Fact]
    public void Summary_drills_into_the_queue_and_denied_tiles_do_not_link()
    {
        var ready = new TrackingSummaryTile("PO", "po-pending-approval", "POs", "READY", 2, 1, 3);
        Assert.Equal("/tracking/pending?queue=po-pending-approval", ready.Link);
        var denied = ready with { State = "ACCESS_DENIED", Count = null, OverdueCount = null, OldestAgeDays = null };
        Assert.Null(denied.Link);
        Assert.Equal("/tracking/pending?queue=a%26mine%3Dtrue", TrackingDocumentLinks.Queue("a&mine=true"));
    }
}