using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260914030000_ReceiptQuantityAcrossPoRevisions")]
public sealed class ReceiptQuantityAcrossPoRevisions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(ReceiptQuantityAcrossPoRevisionsSql.Guard(false));
        migrationBuilder.Sql(ReceiptQuantityAcrossPoRevisionsSql.Definition(true));
        migrationBuilder.Sql(ReceiptQuantityAcrossPoRevisionsSql.Guard(true));
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(ReceiptQuantityAcrossPoRevisionsSql.Guard(true));
        migrationBuilder.Sql(ReceiptQuantityAcrossPoRevisionsSql.Definition(false));
        migrationBuilder.Sql(ReceiptQuantityAcrossPoRevisionsSql.Guard(false));
    }
}

internal static class ReceiptQuantityAcrossPoRevisionsSql
{
    private static string LinePredecessor
    {
        get
        {
            var source=FirstStoresPart2Sql.Up.Replace("\r\n","\n");
            var start=source.IndexOf("CREATE OR REPLACE FUNCTION advance.stores_p2_goods_receipt_line_guard()",StringComparison.Ordinal);
            var end=source.IndexOf("CREATE TRIGGER",start,StringComparison.Ordinal);
            if(start<0||end<0)throw new InvalidOperationException("Receipt line predecessor was not found.");
            return source[start..end].TrimEnd();
        }
    }
    private static string HeaderPredecessor => StoresGrnSlice2Sql.BuiltInHashGoodsReceiptGuard
        .Replace("\r\n","\n")
        .Replace("NEW.\"QcDueAt\"<>NEW.\"FinalizedAt\"+make_interval(days=>NEW.\"QcCompletionDaysSnapshot\")",
            "NEW.\"QcDueAt\"<>NEW.\"ReceivedAt\"+make_interval(days=>NEW.\"QcCompletionDaysSnapshot\")",StringComparison.Ordinal)
        .Replace("GRN QC due time must use the snapshotted limit from finalisation.",
            "GRN QC due time must use the snapshotted limit from receipt.",StringComparison.Ordinal);

    internal static string Definition(bool installed) => Header(installed)+"\n"+Line(installed);

    private static string Line(bool installed)
    {
        var source=LinePredecessor;
        if(!installed)return source;
        var start=source.IndexOf("SELECT coalesce(sum(l.\"ReceivedQuantity\"),0) INTO prior_received",StringComparison.Ordinal);
        var end=source.IndexOf("IF NEW.\"PriorEffectiveReceivedQuantitySnapshot\"",start,StringComparison.Ordinal);
        if(start<0||end<0)throw new InvalidOperationException("Receipt quantity predecessor was not found.");
        source=source[..start]+"""
            IF EXISTS(
              SELECT 1 FROM advance.purchase_order_lines old_line
              JOIN advance.purchase_orders old_po ON old_po."Id"=old_line."PurchaseOrderId"
                AND old_po."CompanyId"=NEW."CompanyId"
              JOIN advance.purchase_orders current_po ON current_po."Id"=po_line."PurchaseOrderId"
                AND current_po."CompanyId"=NEW."CompanyId" AND current_po."RootPurchaseOrderId"=old_po."RootPurchaseOrderId"
              WHERE old_line."CompanyId"=NEW."CompanyId" AND old_line."CommercialComparisonLineId"=po_line."CommercialComparisonLineId"
                AND(old_line."PurchaseRequisitionLineId" IS DISTINCT FROM po_line."PurchaseRequisitionLineId"
                  OR old_line."PurchaseRequirementHandoffId" IS DISTINCT FROM po_line."PurchaseRequirementHandoffId"
                  OR old_line."ItemId" IS DISTINCT FROM po_line."ItemId" OR old_line."UomSnapshot" IS DISTINCT FROM po_line."UomSnapshot")
            ) THEN
              RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='CK_grn_po_lineage',
                MESSAGE='PO revision line provenance is inconsistent; administrator action is required.';
            END IF;
            SELECT coalesce(sum(l."ReceivedQuantity"),0) INTO prior_received
            FROM advance.goods_receipt_lines l
            JOIN advance.goods_receipts h ON h."Id"=l."GoodsReceiptId" AND h."CompanyId"=NEW."CompanyId"
            JOIN advance.purchase_order_lines old_line ON old_line."Id"=l."PurchaseOrderLineId"
              AND old_line."CompanyId"=NEW."CompanyId" AND old_line."CommercialComparisonLineId"=po_line."CommercialComparisonLineId"
            JOIN advance.purchase_orders old_po ON old_po."Id"=old_line."PurchaseOrderId" AND old_po."CompanyId"=NEW."CompanyId"
            JOIN advance.purchase_orders current_po ON current_po."Id"=po_line."PurchaseOrderId"
              AND current_po."CompanyId"=NEW."CompanyId" AND current_po."RootPurchaseOrderId"=old_po."RootPurchaseOrderId"
            WHERE l."CompanyId"=NEW."CompanyId" AND h."Status"='FINALIZED' AND h."DocumentKind"='NORMAL'
              AND NOT EXISTS(SELECT 1 FROM advance.goods_receipts r WHERE r."CompanyId"=NEW."CompanyId"
                AND r."ReversesGoodsReceiptId"=h."Id" AND r."Status"='FINALIZED');

            """+source[end..];
        return ReplaceOnce(source,"RAISE EXCEPTION 'GRN line received-to-date snapshots are not authoritative.';",
            "RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='CK_grn_root_quantity',MESSAGE='GRN received-to-date quantities must include all PO revisions.';");
    }

    private static string Header(bool installed)
    {
        var source=HeaderPredecessor;
        if(!installed)return source;
        const string anchor="IF NEW.\"Status\"='FINALIZED' AND NEW.\"DocumentKind\"='NORMAL' THEN";
        return ReplaceOnce(source,anchor,anchor+"\n"+"""
            IF EXISTS(
              SELECT 1 FROM advance.goods_receipt_lines l
              JOIN advance.purchase_order_lines ordered_line ON ordered_line."Id"=l."PurchaseOrderLineId" AND ordered_line."CompanyId"=NEW."CompanyId"
              JOIN advance.purchase_orders current_po ON current_po."Id"=ordered_line."PurchaseOrderId" AND current_po."CompanyId"=NEW."CompanyId"
              JOIN advance.purchase_orders old_po ON old_po."RootPurchaseOrderId"=current_po."RootPurchaseOrderId" AND old_po."CompanyId"=NEW."CompanyId"
              JOIN advance.purchase_order_lines old_line ON old_line."PurchaseOrderId"=old_po."Id" AND old_line."CompanyId"=NEW."CompanyId"
                AND old_line."CommercialComparisonLineId"=ordered_line."CommercialComparisonLineId"
              WHERE l."CompanyId"=NEW."CompanyId" AND l."GoodsReceiptId"=NEW."Id"
                AND(old_line."PurchaseRequisitionLineId" IS DISTINCT FROM ordered_line."PurchaseRequisitionLineId"
                  OR old_line."PurchaseRequirementHandoffId" IS DISTINCT FROM ordered_line."PurchaseRequirementHandoffId"
                  OR old_line."ItemId" IS DISTINCT FROM ordered_line."ItemId" OR old_line."UomSnapshot" IS DISTINCT FROM ordered_line."UomSnapshot")
            ) THEN
              RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='CK_grn_po_lineage',
                MESSAGE='PO revision line provenance is inconsistent; administrator action is required.';
            END IF;
            IF EXISTS(
              SELECT 1 FROM advance.goods_receipt_lines l
              JOIN advance.purchase_order_lines ordered_line ON ordered_line."Id"=l."PurchaseOrderLineId" AND ordered_line."CompanyId"=NEW."CompanyId"
              JOIN advance.purchase_orders current_po ON current_po."Id"=ordered_line."PurchaseOrderId" AND current_po."CompanyId"=NEW."CompanyId"
              WHERE l."CompanyId"=NEW."CompanyId" AND l."GoodsReceiptId"=NEW."Id"
                AND(
                  SELECT coalesce(sum(received."ReceivedQuantity"),0)
                  FROM advance.goods_receipt_lines received
                  JOIN advance.goods_receipts h ON h."Id"=received."GoodsReceiptId" AND h."CompanyId"=NEW."CompanyId"
                  JOIN advance.purchase_order_lines old_line ON old_line."Id"=received."PurchaseOrderLineId" AND old_line."CompanyId"=NEW."CompanyId"
                    AND old_line."CommercialComparisonLineId"=ordered_line."CommercialComparisonLineId"
                  JOIN advance.purchase_orders old_po ON old_po."Id"=old_line."PurchaseOrderId" AND old_po."CompanyId"=NEW."CompanyId"
                    AND old_po."RootPurchaseOrderId"=current_po."RootPurchaseOrderId"
                  WHERE received."CompanyId"=NEW."CompanyId" AND
                    (h."Id"=NEW."Id" OR(h."Status"='FINALIZED' AND h."DocumentKind"='NORMAL'
                      AND NOT EXISTS(SELECT 1 FROM advance.goods_receipts reversal WHERE reversal."CompanyId"=NEW."CompanyId"
                        AND reversal."ReversesGoodsReceiptId"=h."Id" AND reversal."Status"='FINALIZED')))
                )>ordered_line."OrderedQuantity"
            ) THEN
              RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='CK_grn_root_quantity',
                MESSAGE='GRN exceeds the remaining quantity across PO revisions; reload the receipt.';
            END IF;
            """);
    }

    private static string ReplaceOnce(string value,string oldValue,string newValue)
    {
        var first=value.IndexOf(oldValue,StringComparison.Ordinal);
        if(first<0||value.IndexOf(oldValue,first+oldValue.Length,StringComparison.Ordinal)>=0)
            throw new InvalidOperationException("Receipt predecessor fragment is missing or ambiguous.");
        return (value[..first]+newValue+value[(first+oldValue.Length)..]).Replace("\r\n","\n",StringComparison.Ordinal);
    }
    private static string Body(string definition)
    {
        var first=definition.IndexOf("AS $$",StringComparison.Ordinal);
        var last=definition.LastIndexOf("$$;",StringComparison.Ordinal);
        if(first<0||last<=first)throw new InvalidOperationException("Receipt function delimiter changed.");
        return definition[(first+5)..last].Replace("'","''",StringComparison.Ordinal);
    }

    internal static string Guard(bool installed)
    {
        var header=Body(Header(installed));
        var line=Body(Line(installed));
        return $$"""
            DO $guard$
            BEGIN
              IF current_setting('server_version_num')::integer<170000 OR lower(current_database())
                IN('postgres','template0','template1') THEN
                RAISE EXCEPTION 'Receipt continuity refuses this cluster or protected database.';
              END IF;
              IF EXISTS(
                SELECT 1 FROM(VALUES
                  ('advance.stores_p2_goods_receipt_guard()','advance.goods_receipts','TR_goods_receipt_guard','{{header}}'),
                  ('advance.stores_p2_goods_receipt_line_guard()','advance.goods_receipt_lines','TR_goods_receipt_line_guard','{{line}}')
                ) expected(signature,table_name,trigger_name,body)
                LEFT JOIN pg_proc p ON p.oid=to_regprocedure(expected.signature)
                WHERE p.oid IS NULL OR p.prosecdef OR p.provolatile<>'v' OR p.prorettype<>'trigger'::regtype
                  OR p.prolang<>(SELECT oid FROM pg_language WHERE lanname='plpgsql') OR p.prokind<>'f'
                  OR p.proisstrict OR p.proleakproof OR p.proparallel<>'u'
                  OR p.proconfig IS DISTINCT FROM ARRAY['search_path=pg_catalog, advance']
                  OR p.proowner IS DISTINCT FROM(SELECT relowner FROM pg_class WHERE oid=to_regclass(expected.table_name))
                  OR replace(p.prosrc,E'\r\n',E'\n')<>expected.body
                  OR EXISTS(SELECT 1 FROM aclexplode(coalesce(p.proacl,acldefault('f',p.proowner))) a
                    WHERE a.grantee<>p.proowner AND(a.is_grantable OR a.grantee<>0 OR to_regrole('nexa_erp_owner') IS NOT NULL))
                  OR NOT EXISTS(SELECT 1 FROM pg_trigger t WHERE t.tgrelid=to_regclass(expected.table_name)
                    AND t.tgname=expected.trigger_name AND NOT t.tgisinternal AND t.tgfoid=p.oid AND t.tgenabled='O'
                    AND t.tgtype=31 AND t.tgqual IS NULL AND t.tgnargs=0 AND t.tgattr::text='' AND t.tgconstraint=0)
              ) THEN
                RAISE EXCEPTION 'Receipt continuity refuses changed predecessor definition, trigger or authority.';
              END IF;
            END $guard$;
            """;
    }
}
