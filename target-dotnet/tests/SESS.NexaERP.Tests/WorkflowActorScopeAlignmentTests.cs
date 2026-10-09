using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Authorization;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Purchase;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public Task WorkflowActorScopeAlignmentPreservesAllThreeBandsAndRealStoresReadBack() =>
        RunCompletePurchaseFlow(scopeAlignmentWitness: true);

    private static async Task ProveWorkflowActorReadScope(DbContextOptions<NexaErpDbContext> options,
        PurchaseRequisitionDetail pr, PurchaseFlowBand band, HttpClient client, TaxWorkflowUser user, Guid td, Guid md)
    {
        if (!band.Level2EmployeeId.HasValue) return;
        var actor = band.Level2EmployeeId.Value;
        var role = actor == td ? Rev869ARoleCodes.TechnicalDirector : Rev869ARoleCodes.ManagingDirector;
        user.Set(actor, actor == td ? "SESS-01" : "SESS-02", role);
        Assert.Equal(pr.Id, (await Get<PurchaseRequisitionDetail>(client, $"/api/v1/purchase/requisitions/{pr.PrNumber}")).Id);
        var history = await Get<IReadOnlyList<PurchaseRequisitionHistorySummary>>(client,
            $"/api/v1/purchase/requisitions/{pr.PrNumber}/approval-history");
        Assert.Contains(history, h => h.ActorRoleCode == role && h.Action == "Approve");
        using (var update = await client.PutAsJsonAsync($"/api/v1/purchase/requisitions/{pr.PrNumber}",
            new UpdatePurchaseRequisitionRequest(pr.RequiredByDate, pr.Priority, pr.PurposeJustification, pr.DeliveryWarehouseCode,
                pr.CostCentre, pr.ProjectReference, pr.ServiceReference, pr.WorkOrderReference, pr.CustomerReference,
                pr.Lines.Select(l => new PurchaseRequisitionLineRequest(l.ItemCode, l.RequestedQuantity, l.EstimatedUnitPrice,
                    pr.RequiredByDate, pr.DeliveryWarehouseCode, null, null, null)).ToList(), pr.Version)))
            Assert.Contains(update.StatusCode, new[] { HttpStatusCode.Forbidden, HttpStatusCode.NotFound });
        user.RequireRole("purchase.requisitions:view", role);
        await using var db = new NexaErpDbContext(options);
        // Historical approver visibility is deliberately absent from the generic command query.
        Assert.False(await PurchaseRequisitionVisibility.Apply(db.PurchaseRequisitions, user, db)
            .AnyAsync(p => p.Id == pr.Id));
        Assert.True(await PurchaseActorScope.ReadRequisitions(db.PurchaseRequisitions, db, user).AnyAsync(p => p.Id == pr.Id));

        user.SetOrganization("SESS_PROPRIETORSHIP");
        Assert.False(await PurchaseActorScope.ReadRequisitions(db.PurchaseRequisitions, db, user).AnyAsync(p => p.Id == pr.Id));
        user.SetOrganization("SESS_PVT_LTD");
        user.Set(actor == td ? md : td, actor == td ? "SESS-02" : "SESS-01",
            actor == td ? Rev869ARoleCodes.ManagingDirector : Rev869ARoleCodes.TechnicalDirector);
        user.RequireRole("purchase.requisitions:view", actor == td ? Rev869ARoleCodes.ManagingDirector : Rev869ARoleCodes.TechnicalDirector);
        Assert.False(await PurchaseActorScope.ReadRequisitions(db.PurchaseRequisitions, db, user).AnyAsync(p => p.Id == pr.Id));
        user.Set(actor, actor == td ? "SESS-01" : "SESS-02", role);
        user.RequireRole("purchase.requisitions:view", role);

        var scopes = await db.EmployeeOperationalScopes.AsNoTracking()
            .Where(s => s.EmployeeId == actor && s.OrganizationId == "SESS_PVT_LTD").ToListAsync();
        Assert.NotEmpty(scopes);
        var otherWarehouse = await db.Warehouses.Where(w => w.CompanyId == scopes[0].CompanyId &&
            w.WarehouseCode != pr.DeliveryWarehouseCode).Select(w => w.Id).FirstAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            await db.EmployeeOperationalScopes.Where(s => s.EmployeeId == actor && s.OrganizationId == "SESS_PVT_LTD" && s.IsActive)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.EffectiveTo, today).SetProperty(p => p.IsActive, false));
            db.EmployeeOperationalScopes.AddRange(scopes.Select(s => new EmployeeOperationalScope
            {
                CompanyId = s.CompanyId, OrganizationId = s.OrganizationId, EmployeeId = s.EmployeeId,
                DepartmentId = s.DepartmentId, WarehouseId = otherWarehouse, RackBinId = s.RackBinId,
                OwnRecordsOnly = s.OwnRecordsOnly, AllowsPrivilegedCrossScope = false,
                EffectiveFrom = today, IsActive = true, Remarks = "Disposable warehouse denial witness", CreatedBy = "SCOPE_WITNESS"
            }));
            await db.SaveChangesAsync();
            Assert.False(await PurchaseActorScope.ReadRequisitions(db.PurchaseRequisitions, db, user).AnyAsync(p => p.Id == pr.Id));
            await transaction.RollbackAsync();
            db.ChangeTracker.Clear();
        }

        var mappings = await db.EmployeeIdentityMappings.AsNoTracking()
            .Where(i => i.EmployeeId == actor && i.IsActive && i.OrganizationId == "SESS_PVT_LTD").Select(i => i.Id).ToArrayAsync();
        Assert.NotEmpty(mappings);
        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            await db.EmployeeIdentityMappings.Where(i => mappings.Contains(i.Id))
                .ExecuteUpdateAsync(i => i.SetProperty(p => p.EffectiveTo, today).SetProperty(p => p.IsActive, false));
            Assert.False(await PurchaseActorScope.ReadRequisitions(db.PurchaseRequisitions, db, user).AnyAsync(p => p.Id == pr.Id));
            await transaction.RollbackAsync();
        }

        var memberships = await db.EmployeeCompanyAssignments.AsNoTracking()
            .Where(a => a.EmployeeId == actor && a.CompanyId == scopes[0].CompanyId && a.IsActive).Select(a => a.Id).ToArrayAsync();
        Assert.NotEmpty(memberships);
        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            await db.EmployeeCompanyAssignments.Where(a => memberships.Contains(a.Id)).ExecuteUpdateAsync(a => a.SetProperty(p => p.IsActive, false));
            Assert.False(await PurchaseActorScope.ReadRequisitions(db.PurchaseRequisitions, db, user).AnyAsync(p => p.Id == pr.Id));
            await transaction.RollbackAsync();
        }

        var delivery = await db.PurchaseRequisitions.Where(p => p.Id == pr.Id).Select(p => p.DeliveryWarehouseId).SingleAsync();
        Assert.True(await PurchaseActorScope.AllowsAssignedDecisionAsync(db, user, scopes[0].CompanyId, delivery, null, default));
    }
}
