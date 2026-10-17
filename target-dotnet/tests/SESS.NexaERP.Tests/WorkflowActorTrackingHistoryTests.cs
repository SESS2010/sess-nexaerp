using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private static async Task ProveTrackingActorRefusals(DbContextOptions<NexaErpDbContext> options,
        TaxWorkflowUser user, string docType, Guid documentId, Guid td, Guid md)
    {
        var actor = user.EmployeeId!.Value;
        var role = actor == td ? "TECHNICAL_DIRECTOR" : "MANAGING_DIRECTOR";
        user.RequireRole("tracking.pending:view", role);
        await using var db = new NexaErpDbContext(options);
        var service = new SESS.NexaERP.Infrastructure.Tracking.EfTrackingService(db, user,
            Microsoft.Extensions.Options.Options.Create(new SESS.NexaERP.Infrastructure.Reporting.ReportCalendarOptions()));
        Assert.NotNull(await service.HistoryAsync(docType, documentId, default));
        user.Set(actor == td ? md : td, actor == td ? "SESS-02" : "SESS-01",
            actor == td ? "MANAGING_DIRECTOR" : "TECHNICAL_DIRECTOR");
        Assert.Null(await service.HistoryAsync(docType, documentId, default));
        user.Set(actor, actor == td ? "SESS-01" : "SESS-02", role);
        user.RequireRole("tracking.pending:view", role);
        var scopes = await db.EmployeeOperationalScopes.AsNoTracking().Where(x => x.EmployeeId == actor && x.OrganizationId == "SESS_PVT_LTD" && x.IsActive).ToListAsync();
        Assert.NotEmpty(scopes);
        var company = scopes[0].CompanyId;
        var warehouseId = docType == "PO"
            ? await db.PurchaseOrders.Where(x => x.Id == documentId).Select(x => x.DeliveryWarehouseId).SingleAsync()
            : await db.CommercialComparisons.Where(x => x.Id == documentId).Select(x => x.RequestForQuotation!.DeliveryWarehouseId).SingleAsync();
        var otherWarehouse = await db.Warehouses.Where(x => x.CompanyId == company && x.Id != warehouseId).Select(x => x.Id).FirstAsync();
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            await db.EmployeeOperationalScopes.Where(x => x.EmployeeId == actor && x.OrganizationId == "SESS_PVT_LTD" && x.IsActive)
                .ExecuteUpdateAsync(set => set.SetProperty(x => x.IsActive, false).SetProperty(x => x.EffectiveTo, db.BusinessDate));
            db.EmployeeOperationalScopes.AddRange(scopes.Select(x => new SESS.NexaERP.Domain.Authorization.EmployeeOperationalScope
            {
                CompanyId = x.CompanyId, OrganizationId = x.OrganizationId, EmployeeId = x.EmployeeId, DepartmentId = x.DepartmentId,
                WarehouseId = otherWarehouse, OwnRecordsOnly = x.OwnRecordsOnly, AllowsPrivilegedCrossScope = false,
                EffectiveFrom = db.BusinessDate, IsActive = true, Remarks = "Disposable tracking warehouse refusal", CreatedBy = "TRACKING_WITNESS"
            }));
            await db.SaveChangesAsync();
            Assert.Null(await service.HistoryAsync(docType, documentId, default));
            await tx.RollbackAsync(); db.ChangeTracker.Clear();
        }
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            await db.EmployeeIdentityMappings.Where(x => x.EmployeeId == actor && x.OrganizationId == "SESS_PVT_LTD" && x.IsActive)
                .ExecuteUpdateAsync(set => set.SetProperty(x => x.IsActive, false).SetProperty(x => x.EffectiveTo, db.BusinessDate));
            await Assert.ThrowsAsync<SESS.NexaERP.Application.Reporting.ReportAccessDeniedException>(() => service.HistoryAsync(docType, documentId, default));
            await tx.RollbackAsync();
        }
        user.SetOrganization("SESS_PROPRIETORSHIP");
        try { Assert.Null(await service.HistoryAsync(docType, documentId, default)); }
        catch (SESS.NexaERP.Application.Reporting.ReportAccessDeniedException) { /* No effective selected-company identity/role is a proper refusal. */ }
        finally { user.SetOrganization("SESS_PVT_LTD"); }
        user.Set(actor, actor == td ? "SESS-01" : "SESS-02", role);
    }

    [Fact]
    public void WorkflowActorTrackingHistoryPreservesFunctionMetadataAndRefusesChangedRollback()
    {
        const string head = "20261017180000_WorkflowActorTrackingHistory";
        using var db = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = db.GetService<IMigrator>();
        var migrations = db.Database.GetMigrations().ToArray();
        var previous = migrations[Array.IndexOf(migrations, head) - 1];
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("actor-tracking-before.sql", migrator.GenerateScript("0", previous));
        server.Execute("actor-tracking-snapshot.sql", """
            CREATE TABLE public.actor_tracking_before AS SELECT to_jsonb(p) data FROM pg_proc p
              WHERE p.oid='advance.tracking_history(text,uuid,uuid[],text,text,uuid)'::regprocedure;
            CREATE TABLE public.actor_tracking_grants_before AS SELECT jsonb_agg(to_jsonb(p) ORDER BY p."Id") data
              FROM advance.role_page_permissions p;
            """);
        var up = migrator.GenerateScript(previous, head);
        var down = migrator.GenerateScript(head, previous);
        server.Execute("actor-tracking-up.sql", up);
        server.Execute("actor-tracking-up-metadata.sql", """
            DO $assert$ BEGIN
              IF EXISTS(SELECT 1 FROM public.actor_tracking_before b CROSS JOIN pg_proc p
                WHERE p.oid='advance.tracking_history(text,uuid,uuid[],text,text,uuid)'::regprocedure
                  AND (to_jsonb(p)-'prosrc') IS DISTINCT FROM (b.data-'prosrc')) OR
                (SELECT jsonb_agg(to_jsonb(p) ORDER BY p."Id") FROM advance.role_page_permissions p)
                  IS DISTINCT FROM (SELECT data FROM public.actor_tracking_grants_before) THEN
                RAISE EXCEPTION 'Workflow history changed function metadata or page grants.';
              END IF;
              IF (advance.tracking_history('SESS_PVT_LTD',gen_random_uuid(),'{}','Asia/Kolkata','COMPARISON',gen_random_uuid())->>'found')::boolean THEN
                RAISE EXCEPTION 'Unknown actor must not read a history.';
              END IF;
            END $assert$;
            DO $change$ DECLARE changed text; BEGIN
              SELECT pg_get_functiondef('advance.tracking_history(text,uuid,uuid[],text,text,uuid)'::regprocedure) INTO changed;
              EXECUTE replace(changed,'WITH company AS','WITH /* drift probe */ company AS');
            END $change$;
            """);
        server.AssertRejected("actor-tracking-down-drift-refused.sql", down, "refuses a changed installed definition");
        server.Execute("actor-tracking-restore-up.sql", """
            DO $restore$ DECLARE body text; BEGIN
              SELECT after_value INTO STRICT body FROM advance.r1_workflow_tracking_history_backup;
              EXECUTE body;
            END $restore$;
            """);
        server.Execute("actor-tracking-down.sql", down);
        server.Execute("actor-tracking-restored.sql", """
            DO $assert$ BEGIN
              IF EXISTS(SELECT 1 FROM public.actor_tracking_before b CROSS JOIN pg_proc p
                WHERE p.oid='advance.tracking_history(text,uuid,uuid[],text,text,uuid)'::regprocedure
                  AND to_jsonb(p) IS DISTINCT FROM b.data) THEN
                RAISE EXCEPTION 'Workflow history rollback did not restore the exact function.';
              END IF;
            END $assert$;
            """);
        server.Execute("actor-tracking-reapply.sql", up);
    }
}
