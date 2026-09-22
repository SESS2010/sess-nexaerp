using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    /// <summary>
    /// A2: the stock-adjustment migration widens the ledger contract (batch kind, document reference,
    /// third origin, FIFO consumption source), installs the posting function and page grants, and
    /// rolls back to the exact prior contract; the installer's provision/status agree at each step.
    /// </summary>
    [Fact]
    public async Task StockAdjustmentMigrationWidensTheLedgerContractAndRollsBackExactly()
    {
        const string previous = "20260920200000_EstimatedBomValueSource";
        const string target = "20260920210000_StockAdjustmentPosting";
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        Assert.False(model.Database.HasPendingModelChanges());
        var migrator = model.GetService<IMigrator>();
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("stock-adjustment-predecessor.sql", migrator.GenerateScript("0", previous));
        using var environment = new OrdinaryPrincipalEnvironment(server.ConnectionString, "stock-adjustment-runtime-123456789");
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        server.Execute("stock-adjustment-before.sql", ContractBefore);
        server.Execute("stock-adjustment-up.sql", migrator.GenerateScript(previous, target));
        for (var run = 0; run < 2; run++)
        {
            Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
            Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
            server.Execute($"stock-adjustment-after-{run}.sql", ContractAfter);
        }
        server.Execute("stock-adjustment-bad-grant.sql",
            "GRANT EXECUTE ON FUNCTION advance.post_stock_adjustment(uuid,uuid,uuid,text,uuid,text,text,text,text) TO nexa_erp_bootstrap;");
        Assert.Equal(1, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
        server.Execute("stock-adjustment-down.sql", migrator.GenerateScript(target, previous));
        server.Execute("stock-adjustment-restored.sql", ContractBefore);
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
        server.Execute("stock-adjustment-reapply.sql", migrator.GenerateScript(previous, target) + ContractAfter);
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
    }

    private const string ContractBefore = """
        DO $a$ BEGIN
          IF to_regclass('advance.stock_adjustments') IS NOT NULL OR to_regclass('advance.stock_adjustment_lines') IS NOT NULL
             OR to_regclass('advance.stock_adjustment_decisions') IS NOT NULL
             OR to_regprocedure('advance.post_stock_adjustment(uuid,uuid,uuid,text,uuid,text,text,text,text)') IS NOT NULL
             OR to_regprocedure('advance.fifo_carrying_value_preview(uuid,uuid,numeric)') IS NOT NULL
             OR EXISTS(SELECT 1 FROM information_schema.columns WHERE table_schema='advance' AND
                 ((table_name='stock_posting_batches' AND column_name='StockAdjustmentId')
                  OR (table_name='stock_movements' AND column_name IN ('StockAdjustmentLineId','OriginStockAdjustmentLineId'))
                  OR (table_name IN ('fifo_inventory_cost_layers','fifo_cost_consumptions') AND column_name='StockAdjustmentLineId')))
             OR (SELECT is_nullable FROM information_schema.columns WHERE table_schema='advance' AND table_name='fifo_cost_consumptions' AND column_name='MaterialIssueLineId')<>'NO'
             OR pg_get_constraintdef((SELECT oid FROM pg_constraint WHERE conname='CK_stock_posting_batch_kind')) LIKE '%STOCK_ADJUSTMENT%'
             OR pg_get_constraintdef((SELECT oid FROM pg_constraint WHERE conname='CK_fifo_cost_layer')) LIKE '%ADJUSTMENT_STATED%'
             OR pg_get_constraintdef((SELECT oid FROM pg_constraint WHERE conname='CK_stock_movement_outbound_origin')) LIKE '%OriginStockAdjustmentLineId%'
             OR (SELECT prosrc FROM pg_proc WHERE oid='advance.consume_fifo_for_issue(uuid,uuid,uuid,text,uuid,text,text)'::regprocedure) LIKE '%StockAdjustmentLineId%'
             OR (SELECT prosrc FROM pg_proc WHERE oid='advance.company_report_fifo_valuation(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text)'::regprocedure) LIKE '%StockAdjustmentLineId%'
             OR EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='stores.stock-adjustments') THEN
            RAISE EXCEPTION 'Stock adjustment contract is present before the migration or after rollback.';
          END IF;
        END $a$;
        """;

    private const string ContractAfter = """
        DO $a$ BEGIN
          IF to_regclass('advance.stock_adjustments') IS NULL OR to_regclass('advance.stock_adjustment_lines') IS NULL THEN RAISE EXCEPTION 'Stock adjustment contract clause 1 failed: %',$c$to_regclass('advance.stock_adjustments') IS NULL OR to_regclass('advance.stock_adjustment_lines') IS NULL$c$; END IF;
          IF to_regclass('advance.stock_adjustment_decisions') IS NULL THEN RAISE EXCEPTION 'Stock adjustment contract clause 2 failed: %',$c$to_regclass('advance.stock_adjustment_decisions') IS NULL$c$; END IF;
          IF NOT EXISTS(SELECT 1 FROM pg_proc WHERE oid='advance.post_stock_adjustment(uuid,uuid,uuid,text,uuid,text,text,text,text)'::regprocedure AND prosecdef) THEN RAISE EXCEPTION 'Stock adjustment contract clause 3 failed: %',$c$NOT EXISTS(SELECT 1 FROM pg_proc WHERE oid='advance.post_stock_adjustment(uuid,uuid,uuid,text,uuid,text,text,text,text)'::regprocedure AND prosecdef)$c$; END IF;
          IF NOT EXISTS(SELECT 1 FROM pg_proc WHERE oid='advance.fifo_carrying_value_preview(uuid,uuid,numeric)'::regprocedure AND prosecdef) THEN RAISE EXCEPTION 'Stock adjustment contract clause 4 failed: %',$c$NOT EXISTS(SELECT 1 FROM pg_proc WHERE oid='advance.fifo_carrying_value_preview(uuid,uuid,numeric)'::regprocedure AND prosecdef)$c$; END IF;
          IF NOT has_function_privilege('nexa_erp_runtime','advance.post_stock_adjustment(uuid,uuid,uuid,text,uuid,text,text,text,text)','EXECUTE') THEN RAISE EXCEPTION 'Stock adjustment contract clause 5 failed: %',$c$NOT has_function_privilege('nexa_erp_runtime','advance.post_stock_adjustment(uuid,uuid,uuid,text,uuid,text,text,text,text)','EXECUTE')$c$; END IF;
          IF has_function_privilege('nexa_erp_bootstrap','advance.post_stock_adjustment(uuid,uuid,uuid,text,uuid,text,text,text,text)','EXECUTE') THEN RAISE EXCEPTION 'Stock adjustment contract clause 6 failed: %',$c$has_function_privilege('nexa_erp_bootstrap','advance.post_stock_adjustment(uuid,uuid,uuid,text,uuid,text,text,text,text)','EXECUTE')$c$; END IF;
          IF has_function_privilege('nexa_erp_migration','advance.fifo_carrying_value_preview(uuid,uuid,numeric)','EXECUTE') THEN RAISE EXCEPTION 'Stock adjustment contract clause 7 failed: %',$c$has_function_privilege('nexa_erp_migration','advance.fifo_carrying_value_preview(uuid,uuid,numeric)','EXECUTE')$c$; END IF;
          IF NOT has_table_privilege('nexa_erp_runtime','advance.stock_adjustments','INSERT') THEN RAISE EXCEPTION 'Stock adjustment contract clause 8 failed: %',$c$NOT has_table_privilege('nexa_erp_runtime','advance.stock_adjustments','INSERT')$c$; END IF;
          IF (SELECT is_nullable FROM information_schema.columns WHERE table_schema='advance' AND table_name='fifo_cost_consumptions' AND column_name='MaterialIssueLineId')<>'YES' THEN RAISE EXCEPTION 'Stock adjustment contract clause 9 failed: %',$c$(SELECT is_nullable FROM information_schema.columns WHERE table_schema='advance' AND table_name='fifo_cost_consumptions' AND column_name='MaterialIssueLineId')<>'YES'$c$; END IF;
          IF NOT EXISTS(SELECT 1 FROM pg_constraint WHERE conname='CK_fifo_cost_consumption_source') THEN RAISE EXCEPTION 'Stock adjustment contract clause 10 failed: %',$c$NOT EXISTS(SELECT 1 FROM pg_constraint WHERE conname='CK_fifo_cost_consumption_source')$c$; END IF;
          IF pg_get_constraintdef((SELECT oid FROM pg_constraint WHERE conname='CK_stock_posting_batch_kind')) NOT LIKE '%STOCK_ADJUSTMENT%' THEN RAISE EXCEPTION 'Stock adjustment contract clause 11 failed: %',$c$pg_get_constraintdef((SELECT oid FROM pg_constraint WHERE conname='CK_stock_posting_batch_kind')) NOT LIKE '%STOCK_ADJUSTMENT%'$c$; END IF;
          IF pg_get_constraintdef((SELECT oid FROM pg_constraint WHERE conname='CK_stock_posting_batch_source')) NOT LIKE '%StockAdjustmentId%' THEN RAISE EXCEPTION 'Stock adjustment contract clause 12 failed: %',$c$pg_get_constraintdef((SELECT oid FROM pg_constraint WHERE conname='CK_stock_posting_batch_source')) NOT LIKE '%StockAdjustmentId%'$c$; END IF;
          IF pg_get_constraintdef((SELECT oid FROM pg_constraint WHERE conname='CK_stock_movement_v2_contract')) NOT LIKE '%StockAdjustmentLineId%' THEN RAISE EXCEPTION 'Stock adjustment contract clause 13 failed: %',$c$pg_get_constraintdef((SELECT oid FROM pg_constraint WHERE conname='CK_stock_movement_v2_contract')) NOT LIKE '%StockAdjustmentLineId%'$c$; END IF;
          IF pg_get_constraintdef((SELECT oid FROM pg_constraint WHERE conname='CK_stock_movement_outbound_origin')) NOT LIKE '%OriginStockAdjustmentLineId%' THEN RAISE EXCEPTION 'Stock adjustment contract clause 14 failed: %',$c$pg_get_constraintdef((SELECT oid FROM pg_constraint WHERE conname='CK_stock_movement_outbound_origin')) NOT LIKE '%OriginStockAdjustmentLineId%'$c$; END IF;
          IF pg_get_constraintdef((SELECT oid FROM pg_constraint WHERE conname='CK_fifo_cost_layer')) NOT LIKE '%ADJUSTMENT_STATED%' THEN RAISE EXCEPTION 'Stock adjustment contract clause 15 failed: %',$c$pg_get_constraintdef((SELECT oid FROM pg_constraint WHERE conname='CK_fifo_cost_layer')) NOT LIKE '%ADJUSTMENT_STATED%'$c$; END IF;
          IF pg_get_constraintdef((SELECT oid FROM pg_constraint WHERE conname='CK_fifo_cost_layer')) NOT LIKE '%StockAdjustmentLineId%' THEN RAISE EXCEPTION 'Stock adjustment contract clause 16 failed: %',$c$pg_get_constraintdef((SELECT oid FROM pg_constraint WHERE conname='CK_fifo_cost_layer')) NOT LIKE '%StockAdjustmentLineId%'$c$; END IF;
          IF (SELECT (length(prosrc)-length(replace(prosrc,'m."StockAdjustmentLineId"=f."StockAdjustmentLineId"','')))/length('m."StockAdjustmentLineId"=f."StockAdjustmentLineId"') FROM pg_proc WHERE oid='advance.consume_fifo_for_issue(uuid,uuid,uuid,text,uuid,text,text)'::regprocedure)<>1 OR (SELECT (length(prosrc)-length(replace(prosrc,'f."StockAdjustmentLineId" IS NOT NULL','')))/length('f."StockAdjustmentLineId" IS NOT NULL') FROM pg_proc WHERE oid='advance.consume_fifo_for_issue(uuid,uuid,uuid,text,uuid,text,text)'::regprocedure)<>2 THEN RAISE EXCEPTION 'Stock adjustment contract clause 17 failed: consume_fifo_for_issue lacks the adjustment origin or eligibility'; END IF;
          IF (SELECT (length(prosrc)-length(replace(prosrc,'m."StockAdjustmentLineId"=f."StockAdjustmentLineId"','')))/length('m."StockAdjustmentLineId"=f."StockAdjustmentLineId"') FROM pg_proc WHERE oid='advance.company_report_fifo_valuation(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text)'::regprocedure)<>2 OR (SELECT (length(prosrc)-length(replace(prosrc,'f."StockAdjustmentLineId" IS NOT NULL','')))/length('f."StockAdjustmentLineId" IS NOT NULL') FROM pg_proc WHERE oid='advance.company_report_fifo_valuation(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text)'::regprocedure)<>4 THEN RAISE EXCEPTION 'Stock adjustment contract clause 18 failed: company_report_fifo_valuation lacks the adjustment origin or eligibility'; END IF;
          IF (SELECT count(*) FROM pg_trigger WHERE tgrelid IN ('advance.stock_adjustments'::regclass,'advance.stock_adjustment_lines'::regclass,'advance.stock_adjustment_decisions'::regclass) AND NOT tgisinternal)<>3 THEN RAISE EXCEPTION 'Stock adjustment contract clause 19 failed: %',$c$(SELECT count(*) FROM pg_trigger WHERE tgrelid IN ('advance.stock_adjustments'::regclass,'advance.stock_adjustment_lines'::regclass,'advance.stock_adjustment_decisions'::regclass) AND NOT tgisinternal)<>3$c$; END IF;
          IF (SELECT count(*) FROM advance.page_definitions WHERE "PageKey"='stores.stock-adjustments')<>1 THEN RAISE EXCEPTION 'Stock adjustment contract clause 20 failed: %',$c$(SELECT count(*) FROM advance.page_definitions WHERE "PageKey"='stores.stock-adjustments')<>1$c$; END IF;
          IF (SELECT count(*) FROM advance.role_page_permissions p JOIN advance.roles r ON r."Id"=p."RoleId" WHERE p."PageDefinitionId"=md5('stores.stock-adjustments')::uuid AND p."CanView")<>5 THEN RAISE EXCEPTION 'Stock adjustment contract clause 21 failed: %',$c$(SELECT count(*) FROM advance.role_page_permissions p JOIN advance.roles r ON r."Id"=p."RoleId" WHERE p."PageDefinitionId"=md5('stores.stock-adjustments')::uuid AND p."CanView")<>5$c$; END IF;
          IF (SELECT count(*) FROM advance.role_page_permissions p JOIN advance.roles r ON r."Id"=p."RoleId" WHERE p."PageDefinitionId"=md5('stores.stock-adjustments')::uuid AND p."CanApprove" AND r."Code" IN ('STORES_MANAGER','TECHNICAL_DIRECTOR','MANAGING_DIRECTOR','ACCOUNTS_MANAGER'))<>4 THEN RAISE EXCEPTION 'Stock adjustment contract clause 22 failed: %',$c$(SELECT count(*) FROM advance.role_page_permissions p JOIN advance.roles r ON r."Id"=p."RoleId" WHERE p."PageDefinitionId"=md5('stores.stock-adjustments')::uuid AND p."CanApprove" AND r."Code" IN ('STORES_MANAGER','TECHNICAL_DIRECTOR','MANAGING_DIRECTOR','ACCOUNTS_MANAGER'))<>4$c$; END IF;
          IF (SELECT count(*) FROM advance.role_page_permissions p JOIN advance.roles r ON r."Id"=p."RoleId" WHERE p."PageDefinitionId"=md5('stores.stock-adjustments')::uuid AND p."CanCreate" AND r."Code" IN ('STORES_EXECUTIVE','STORES_MANAGER'))<>2 THEN RAISE EXCEPTION 'Stock adjustment contract clause 23 failed: %',$c$(SELECT count(*) FROM advance.role_page_permissions p JOIN advance.roles r ON r."Id"=p."RoleId" WHERE p."PageDefinitionId"=md5('stores.stock-adjustments')::uuid AND p."CanCreate" AND r."Code" IN ('STORES_EXECUTIVE','STORES_MANAGER'))<>2$c$; END IF;
        END $a$;
        """;
}
