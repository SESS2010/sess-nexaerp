using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260914100000_FifoReversedReceiptEligibility")]
public sealed class FifoReversedReceiptEligibility : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(FifoReversedReceiptSql.Guard(false));
        migrationBuilder.Sql("""
            DO $guard$ BEGIN
             IF to_regprocedure('advance.guard_fifo_receipt_reversal()') IS NOT NULL THEN
              RAISE EXCEPTION 'FIFO receipt-reversal guard already exists.';
             END IF;
             IF EXISTS(SELECT 1 FROM advance.goods_receipts r
              JOIN advance.goods_receipt_lines gl ON gl."CompanyId"=r."CompanyId" AND gl."GoodsReceiptId"=r."ReversesGoodsReceiptId"
              JOIN advance.fifo_inventory_cost_layers f ON f."CompanyId"=r."CompanyId" AND f."GoodsReceiptLineId"=gl."Id"
              JOIN advance.fifo_cost_consumptions c ON c."CompanyId"=r."CompanyId" AND c."FifoInventoryCostLayerId"=f."Id"
              WHERE r."DocumentKind"='REVERSAL' AND r."Status"='FINALIZED'
               AND c."Quantity">coalesce((SELECT sum(x."Quantity") FROM advance.fifo_cost_restorations x
                WHERE x."CompanyId"=r."CompanyId" AND x."FifoCostConsumptionId"=c."Id"),0))
              OR EXISTS(SELECT 1 FROM advance.goods_receipts r JOIN advance.vendor_bills b
               ON b."CompanyId"=r."CompanyId" AND b."GoodsReceiptId"=r."ReversesGoodsReceiptId"
               WHERE r."DocumentKind"='REVERSAL' AND r."Status"='FINALIZED' AND b."Status"='ACCEPTED') THEN
              RAISE EXCEPTION 'Historical reversed GRNs retain issue cost or accepted bills; administrator reconciliation is required. No history will be rewritten.';
             END IF;
            END $guard$;
            """);
        migrationBuilder.Sql(FifoReversedReceiptSql.Consume(true));
        migrationBuilder.Sql(FifoReversedReceiptSql.Trigger);
        migrationBuilder.Sql(FifoReversedReceiptSql.Guard(true));
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(FifoReversedReceiptSql.Guard(true));
        migrationBuilder.Sql("""
            DO $guard$ BEGIN
             IF EXISTS(SELECT 1 FROM advance.goods_receipts WHERE "DocumentKind"='REVERSAL' AND "Status"='FINALIZED') THEN
              RAISE EXCEPTION 'FIFO eligibility rollback refuses retained receipt-reversal evidence.';
             END IF;
            END $guard$;
            DROP TRIGGER trg_fifo_receipt_reversal ON advance.goods_receipts;
            DROP FUNCTION advance.guard_fifo_receipt_reversal();
            """);
        migrationBuilder.Sql(FifoReversedReceiptSql.Consume(false));
    }
}
internal static class FifoReversedReceiptSql
{
    internal static string Consume(bool installed)
    {
        var before=FifoReturnRestorationSql.Consume(true);
        return installed?FifoReturnRestorationSql.ReplaceOnce(before,
            """AND coalesce(po."CurrencyCode",o."CurrencyCode")=pool_currency""","""
            AND coalesce(po."CurrencyCode",o."CurrencyCode")=pool_currency
            AND (f."OpeningStockLineId" IS NOT NULL OR (g."DocumentKind"='NORMAL' AND g."Status"='FINALIZED'
              AND NOT EXISTS(SELECT 1 FROM advance.goods_receipts reversed
                WHERE reversed."CompanyId"=p_company AND reversed."ReversesGoodsReceiptId"=g."Id"
                  AND reversed."Status"='FINALIZED')))
            """):before;
    }
    internal static string Guard(bool installed)
    {
        var expected=Consume(installed);
        var start=expected.IndexOf("$function$",StringComparison.Ordinal)+"$function$".Length;
        var end=expected.LastIndexOf("$function$",StringComparison.Ordinal);
        var body=expected[start..end].Replace("'","''",StringComparison.Ordinal);
        return $$"""
            DO $guard$ BEGIN
             IF NOT EXISTS(SELECT 1 FROM pg_proc p WHERE p.oid=to_regprocedure('advance.consume_fifo_for_issue(uuid,uuid,uuid,text,uuid,text,text)')
              AND p.proowner=(SELECT relowner FROM pg_class WHERE oid='advance.fifo_inventory_cost_layers'::regclass)
              AND p.prosecdef AND p.prokind='f' AND p.provolatile='v'
              AND p.proconfig=ARRAY['search_path=pg_catalog, advance']
              AND replace(p.prosrc,E'\r\n',E'\n')='{{body}}'
              AND NOT EXISTS(SELECT 1 FROM aclexplode(coalesce(p.proacl,acldefault('f',p.proowner))) acl
                WHERE acl.grantee<>p.proowner AND (acl.is_grantable OR acl.grantee=0
                 OR acl.grantee IS DISTINCT FROM to_regrole('nexa_erp_runtime')::oid))) THEN
              RAISE EXCEPTION 'FIFO reversed-receipt migration refuses changed allocator or authority.';
             END IF;
            END $guard$;
            """;
    }
    internal const string Trigger="""
        CREATE FUNCTION advance.guard_fifo_receipt_reversal()
        RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
        DECLARE lock_item uuid;
        BEGIN
         IF NEW."DocumentKind"<>'REVERSAL' OR NEW."Status"<>'FINALIZED' THEN RETURN NEW; END IF;
         IF TG_OP='UPDATE' AND OLD."Status"='FINALIZED' THEN RETURN NEW; END IF;
         FOR lock_item IN
          SELECT DISTINCT f."ItemId" FROM advance.fifo_inventory_cost_layers f
          JOIN advance.goods_receipt_lines gl ON gl."CompanyId"=NEW."CompanyId" AND gl."Id"=f."GoodsReceiptLineId"
          WHERE f."CompanyId"=NEW."CompanyId" AND gl."GoodsReceiptId"=NEW."ReversesGoodsReceiptId" ORDER BY f."ItemId"
         LOOP
          PERFORM pg_advisory_xact_lock(hashtextextended('FIFO:'||NEW."CompanyId"||':'||lock_item,0));
         END LOOP;
         IF EXISTS(SELECT 1 FROM advance.vendor_bills b WHERE b."CompanyId"=NEW."CompanyId"
           AND b."GoodsReceiptId"=NEW."ReversesGoodsReceiptId" AND b."Status"='ACCEPTED') THEN
          RAISE EXCEPTION 'GRN reversal requires governed reversal of its accepted bill first.';
         END IF;
         IF EXISTS(SELECT 1 FROM advance.fifo_cost_consumptions c
          JOIN advance.fifo_inventory_cost_layers f ON f."CompanyId"=NEW."CompanyId" AND f."Id"=c."FifoInventoryCostLayerId"
          JOIN advance.goods_receipt_lines gl ON gl."CompanyId"=NEW."CompanyId" AND gl."Id"=f."GoodsReceiptLineId"
          WHERE c."CompanyId"=NEW."CompanyId" AND gl."GoodsReceiptId"=NEW."ReversesGoodsReceiptId"
           AND c."Quantity">coalesce((SELECT sum(r."Quantity") FROM advance.fifo_cost_restorations r
            WHERE r."CompanyId"=NEW."CompanyId" AND r."FifoCostConsumptionId"=c."Id"),0)) THEN
          RAISE EXCEPTION 'GRN reversal is refused while its FIFO layer retains outstanding issue consumption.';
         END IF;
         RETURN NEW;
        END $function$;
        REVOKE ALL ON FUNCTION advance.guard_fifo_receipt_reversal() FROM PUBLIC;
        DO $owner$ BEGIN
         IF to_regrole('nexa_erp_owner') IS NOT NULL THEN
          ALTER FUNCTION advance.guard_fifo_receipt_reversal() OWNER TO nexa_erp_owner;
          REVOKE ALL ON FUNCTION advance.guard_fifo_receipt_reversal() FROM nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
         END IF;
        END $owner$;
        CREATE TRIGGER trg_fifo_receipt_reversal BEFORE INSERT OR UPDATE OF "Status" ON advance.goods_receipts
         FOR EACH ROW EXECUTE FUNCTION advance.guard_fifo_receipt_reversal();
        """;
}
