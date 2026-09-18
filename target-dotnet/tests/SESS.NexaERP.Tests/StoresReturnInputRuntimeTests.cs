using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private static async Task ProveStoresReturnInputs(HttpClient client, DbContextOptions<NexaErpDbContext> options,
        TaxWorkflowUser user, Guid storesId)
    {
        user.Set(storesId, "SESS-35", "STORES_EXECUTIVE");
        await AssertResolvedSeedRole(options, user, "STORES_EXECUTIVE");
        await using var db = new NexaErpDbContext(options);
        var company = await db.Companies.Where(x => x.Code == "SESS_PVT_LTD").Select(x => x.Id).SingleAsync();
        var serialLine = await db.MaterialIssueLines.AsNoTracking()
            .FirstAsync(x => x.CompanyId == company && x.InventorySerialId != null);
        var serial = await db.InventorySerials.AsNoTracking().SingleAsync(x => x.Id == serialLine.InventorySerialId);
        var issue = await Get<MaterialIssueView>(client, "/api/v1/stores/material-issues/" + serialLine.MaterialIssueId);
        Assert.Equal(serial.StoredSerialNumber, Assert.Single(issue.Lines, x => x.Id == serialLine.Id).StoredSerialNumber);
        // A physical scanner's exact stored code now identifies exactly one exposed line.
        Assert.Equal(serialLine.Id, Assert.Single(issue.Lines, x => x.StoredSerialNumber == serial.StoredSerialNumber).Id);
        var active = await db.ComponentFitments.AsNoTracking().FirstAsync(x => x.CompanyId == company &&
            !db.ComponentFitmentReversals.Any(r => r.CompanyId == x.CompanyId && r.ComponentFitmentId == x.Id));
        var fitments = await Get<PagedResponse<ComponentFitmentSummary>>(client,
            "/api/v1/production/component-fitments?activeOnly=true&jobOrderId=" + active.JobOrderId);
        Assert.Contains(fitments.Items, x => x.Id == active.Id);
        var fittedLine = await db.MaterialIssueLines.AsNoTracking().SingleAsync(x => x.Id == active.MaterialIssueLineId);
        var fittedIssue = await Get<MaterialIssueView>(client, "/api/v1/stores/material-issues/" + fittedLine.MaterialIssueId);
        var expected = await db.ComponentFitments.Where(x => x.CompanyId == company && x.MaterialIssueLineId == fittedLine.Id &&
            !db.ComponentFitmentReversals.Any(r => r.CompanyId == x.CompanyId && r.ComponentFitmentId == x.Id))
            .SumAsync(x => x.QuantityBase);
        Assert.True(expected > 0);
        Assert.Equal(expected, Assert.Single(fittedIssue.Lines, x => x.Id == fittedLine.Id).FittedQuantityBase);
    }
}