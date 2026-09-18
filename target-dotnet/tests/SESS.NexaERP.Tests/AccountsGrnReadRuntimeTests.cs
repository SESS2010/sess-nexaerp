using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private static async Task ProveAccountsGrnReads(HttpClient client, DbContextOptions<NexaErpDbContext> options,
        TaxWorkflowUser user, Guid accountsId, GoodsReceiptResult grn)
    {
        user.Set(accountsId, "SESS-14", "ACCOUNTS_MANAGER");
        await AssertResolvedSeedRole(options, user, "ACCOUNTS_MANAGER");
        var list = await Get<GoodsReceiptListResult>(client, "/api/v1/stores/goods-receipts?grnNumber=" + grn.GrnNumber);
        var receipt = Assert.Single(list.Items);
        Assert.Equal(grn.Id, receipt.Id);
        var detail = await Get<GoodsReceiptResult>(client, "/api/v1/stores/goods-receipts/" + receipt.Id);
        Assert.Equal(grn.Lines.Select(x => x.Id).Order(), detail.Lines.Select(x => x.Id).Order());
        Assert.All(detail.Lines, x => Assert.NotEqual(Guid.Empty, x.Id));
        using (var denied = await client.PostAsJsonAsync("/api/v1/stores/goods-receipts/" + receipt.Id + "/finalize",
            new FinalizeGoodsReceiptRequest(receipt.Version, "accounts-read-does-not-grant-submit")))
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        user.SetOrganization("SESS_PROPRIETORSHIP");
        using (var hidden = await client.GetAsync("/api/v1/stores/goods-receipts/" + receipt.Id))
            Assert.Contains(hidden.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden });
        user.SetOrganization("SESS_PVT_LTD");
    }
}