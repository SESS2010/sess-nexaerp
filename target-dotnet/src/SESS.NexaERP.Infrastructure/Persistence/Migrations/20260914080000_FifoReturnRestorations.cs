using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260914080000_FifoReturnRestorations")]
public sealed class FifoReturnRestorations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(FifoReturnRestorationSql.Guard(false));
        migrationBuilder.Sql("""
            DO $guard$ BEGIN
              IF to_regclass('advance.fifo_cost_restorations') IS NOT NULL
                OR to_regclass('advance.fifo_consumption_creation_order') IS NOT NULL
                OR to_regprocedure('advance.restore_fifo_for_material_return(uuid,uuid,boolean)') IS NOT NULL THEN
                RAISE EXCEPTION 'FIFO restoration migration refuses partially installed evidence.';
              END IF;
              IF EXISTS(SELECT 1 FROM advance.fifo_cost_consumptions c
                JOIN advance.material_issue_lines il ON il."Id"=c."MaterialIssueLineId" AND il."CompanyId"=c."CompanyId"
                JOIN advance.fifo_inventory_cost_layers f ON f."Id"=c."FifoInventoryCostLayerId" AND f."CompanyId"=c."CompanyId"
                WHERE NOT coalesce((SELECT count(DISTINCT m."OwnershipAccountId")=1
                  AND bool_and(m."OwnershipAccountId"=il."OwnershipAccountId")
                  FROM advance.stock_movements m WHERE m."CompanyId"=c."CompanyId" AND m."MovementLeg"='RECEIPT_IN'
                    AND ((f."GoodsReceiptLineId" IS NOT NULL AND m."GoodsReceiptLineId"=f."GoodsReceiptLineId")
                      OR (f."OpeningStockLineId" IS NOT NULL AND m."OpeningStockLineId"=f."OpeningStockLineId"))),false)) THEN
                RAISE EXCEPTION 'Historical FIFO ownership mismatch requires reconciliation; original consumption will not be rewritten.';
              END IF;
            END $guard$;
            """);
        migrationBuilder.Sql(FifoReturnRestorationSql.Ledger);
        migrationBuilder.Sql(FifoReturnRestorationSql.Return(true));
        migrationBuilder.Sql(FifoReturnRestorationSql.Landed(true));
        migrationBuilder.Sql(FifoReturnRestorationSql.Report(true));
        // Set ownership before reconciliation queues deferred completeness checks.
        migrationBuilder.Sql(FifoReturnRestorationSql.Ownership);
        migrationBuilder.Sql("""
            DO $reconcile$
            DECLARE accepted record;
            BEGIN
              FOR accepted IN
                SELECT r."CompanyId",r."Id" FROM advance.material_returns r
                WHERE r."Status"='ACCEPTED'
                ORDER BY (SELECT min(h."OccurredAt") FROM advance.material_return_history h
                  WHERE h."CompanyId"=r."CompanyId" AND h."MaterialReturnId"=r."Id" AND h."Action"='ACCEPT'),r."Id"
              LOOP
                PERFORM advance.restore_fifo_for_material_return(accepted."CompanyId",accepted."Id",true);
              END LOOP;
            END $reconcile$;
            """);

        migrationBuilder.Sql(FifoReturnRestorationSql.Guard(true));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(FifoReturnRestorationSql.Guard(true));
        migrationBuilder.Sql("""
            DO $guard$ BEGIN
              IF EXISTS(SELECT 1 FROM advance.fifo_cost_restorations)
                OR EXISTS(SELECT 1 FROM advance.fifo_consumption_creation_order) THEN
                RAISE EXCEPTION 'FIFO restoration rollback refuses retained costing evidence.';
              END IF;
            END $guard$;
            """);
        migrationBuilder.Sql(FifoReturnRestorationSql.Consume(false));
        migrationBuilder.Sql(FifoReturnRestorationSql.Return(false));
        migrationBuilder.Sql(FifoReturnRestorationSql.Landed(false));
        migrationBuilder.Sql(FifoReturnRestorationSql.Report(false));
        migrationBuilder.Sql("""
            DROP TRIGGER trg_fifo_consumption_creation_order ON advance.fifo_cost_consumptions;
            DROP TRIGGER trg_material_return_fifo_complete ON advance.material_returns;
            DROP TABLE advance.fifo_cost_restorations;
            DROP TABLE advance.fifo_consumption_creation_order;
            DROP FUNCTION advance.guard_fifo_restoration_evidence();
            DROP FUNCTION advance.guard_material_return_fifo_restoration();
            DROP FUNCTION advance.record_fifo_consumption_creation_order();
            DROP FUNCTION advance.restore_fifo_for_material_return(uuid,uuid,boolean);
            """);
        migrationBuilder.Sql(FifoReturnRestorationSql.Guard(false));
    }
}

internal static class FifoReturnRestorationSql
{
    internal const string ConsumeSignature="advance.consume_fifo_for_issue(uuid,uuid,uuid,text,uuid,text,text)";
    internal const string ReturnSignature="advance.post_material_return_acceptance(uuid,uuid,text,text,text,uuid,text)";
    internal const string LandedSignature="advance.allocate_vendor_bill_landed_cost()";
    internal const string ReportSignature="advance.company_report_fifo_valuation"+ControlledCompanyReportSql.Signature;
    internal static string Ledger
    {
        get
        {
            using var stream=typeof(FifoReturnRestorationSql).Assembly
                .GetManifestResourceStream("FifoReturnRestorations.20260914080000.sql")
                ?? throw new InvalidOperationException("FIFO restoration SQL resource is missing.");
            using var reader=new StreamReader(stream);
            return reader.ReadToEnd().Replace("\r\n","\n");
        }
    }
    internal static string Extract(string source,string name,string delimiter="$function$")
    {
        source=source.Replace("\r\n","\n");
        var start=source.IndexOf("CREATE FUNCTION "+name,StringComparison.Ordinal);
        if(start<0)start=source.IndexOf("CREATE OR REPLACE FUNCTION "+name,StringComparison.Ordinal);
        if(start<0)throw new InvalidOperationException("FIFO migration baseline missing: "+name);
        var end=source.IndexOf(delimiter+";",start,StringComparison.Ordinal);
        if(end<0)throw new InvalidOperationException("FIFO migration function ending missing: "+name);
        return source[start..(end+delimiter.Length+1)]
            .Replace("CREATE FUNCTION ","CREATE OR REPLACE FUNCTION ",StringComparison.Ordinal);
    }
    internal static string ReplaceOnce(string source,string before,string after)
    {
        source=source.Replace("\r\n","\n");before=before.Replace("\r\n","\n");after=after.Replace("\r\n","\n");
        if(source.Split(before,StringSplitOptions.None).Length!=2)
            throw new InvalidOperationException("FIFO migration expected exactly one baseline fragment: "+before);
        return source.Replace(before,after,StringComparison.Ordinal);
    }
    internal static string Consume(bool installed)=>Extract(installed?Ledger:VendorBillCostingSql.Up,
        "advance.consume_fifo_for_issue");
    internal static string Return(bool installed)
    {
        var original=Extract(MaterialReturnSql.Up,"advance.post_material_return_acceptance");
        return installed?ReplaceOnce(original,"RETURN QUERY SELECT batch_id,false;",
            "PERFORM advance.restore_fifo_for_material_return(p_company_id,p_return_id,false);\n          RETURN QUERY SELECT batch_id,false;"):original;
    }
    internal static string Landed(bool installed)
    {
        var original=Extract(ImmutableLandedCostAdjustmentsSql.Up,"advance.allocate_vendor_bill_landed_cost");
        if(!installed)return original;
        const string before="""
            SELECT coalesce(sum("Quantity"),0) INTO consumed
            FROM advance.fifo_cost_consumptions
            WHERE "CompanyId"=NEW."CompanyId"
              AND "FifoInventoryCostLayerId"=line_row.layer_id;
            """;
        const string after="""
            PERFORM pg_advisory_xact_lock(hashtextextended('FIFO:'||NEW."CompanyId"||':'||line_row."ItemId",0));
            SELECT coalesce(sum(c."Quantity"-coalesce((SELECT sum(r."Quantity")
              FROM advance.fifo_cost_restorations r WHERE r."CompanyId"=NEW."CompanyId"
                AND r."FifoCostConsumptionId"=c."Id"),0)),0) INTO consumed
            FROM advance.fifo_cost_consumptions c
            WHERE c."CompanyId"=NEW."CompanyId" AND c."FifoInventoryCostLayerId"=line_row.layer_id;
            """;
        original=ReplaceOnce(original,"consumed_adjustment numeric;","consumed_adjustment numeric; lock_item uuid;");
        original=ReplaceOnce(original,"FOR charge_row IN SELECT * FROM advance.vendor_bill_charges","""
            FOR lock_item IN SELECT DISTINCT "ItemId" FROM advance.vendor_bill_lines
              WHERE "CompanyId"=NEW."CompanyId" AND "VendorBillId"=NEW."Id" ORDER BY "ItemId"
            LOOP
              PERFORM pg_advisory_xact_lock(hashtextextended('FIFO:'||NEW."CompanyId"||':'||lock_item,0));
            END LOOP;
            FOR charge_row IN SELECT * FROM advance.vendor_bill_charges
            """);
        return ReplaceOnce(original,before.Replace("\n","\n    ",StringComparison.Ordinal),after);
    }
    internal static string Report(bool installed)
    {
        var original=Extract(ControlledCompanyReportSql.Up,"advance.company_report_fifo_valuation","$company_report$");
        if(!installed)return original;
        const string before="""SELECT sum(c."Quantity") AS quantity FROM advance.fifo_cost_consumptions c""";
        const string after="""
            SELECT sum(c."Quantity"-coalesce((SELECT sum(r."Quantity") FROM advance.fifo_cost_restorations r
              WHERE r."CompanyId"=a.company_id AND r."FifoCostConsumptionId"=c."Id"
                AND (r."EffectiveAt" AT TIME ZONE p_report_timezone)::date<=p_to_date),0)) AS quantity
            FROM advance.fifo_cost_consumptions c
            """;
        if(original.Split(before,StringSplitOptions.None).Length!=3)
            throw new InvalidOperationException("FIFO report must contain exactly two layer projections.");
        original=original.Replace(before,after,StringComparison.Ordinal);
        const string gap="""AND rl."ReturnedQuantityBase">0""";
        return ReplaceOnce(original,gap,"""
            AND rl."ReturnedQuantityBase"<>coalesce((SELECT sum(restored."Quantity")
              FROM advance.fifo_cost_restorations restored
              WHERE restored."CompanyId"=a.company_id AND restored."MaterialReturnLineId"=rl."Id"
                AND (restored."EffectiveAt" AT TIME ZONE p_report_timezone)::date<=p_to_date),0)
            """);
    }
    private static string FunctionGuard(string signature,string definition,string delimiter="$function$",bool privateFunction=false,bool securityDefiner=true)
    {
        var first=definition.IndexOf(delimiter,StringComparison.Ordinal)+delimiter.Length;
        var last=definition.LastIndexOf(delimiter,StringComparison.Ordinal);
        if(first<delimiter.Length||last<=first)throw new InvalidOperationException("Missing FIFO function body.");
        var body=definition[first..last].Replace("'","''",StringComparison.Ordinal);
        return $$"""
            DO $guard$ BEGIN
              IF NOT EXISTS(SELECT 1 FROM pg_proc p WHERE p.oid=to_regprocedure('{{signature}}')
                AND p.proowner=(SELECT relowner FROM pg_class WHERE oid='advance.fifo_inventory_cost_layers'::regclass)
                AND p.prokind='f' AND p.provolatile='v'
                AND p.prolang=(SELECT oid FROM pg_language WHERE lanname='plpgsql')
                AND p.proconfig=ARRAY['search_path=pg_catalog, advance']
                AND p.prosecdef={{(securityDefiner?"true":"false")}}
                AND replace(p.prosrc,E'\r\n',E'\n')='{{body}}'
                AND NOT EXISTS(SELECT 1 FROM aclexplode(coalesce(p.proacl,acldefault('f',p.proowner))) acl
                  WHERE acl.grantee<>p.proowner AND (acl.is_grantable OR acl.grantee=0
                    OR {{(privateFunction?"true":"acl.grantee IS DISTINCT FROM to_regrole('nexa_erp_runtime')::oid")}}))) THEN
                RAISE EXCEPTION 'FIFO restoration migration refuses changed function body or authority: {{signature}}';
              END IF;
            END $guard$;
            """;
    }
    internal static string Guard(bool installed)
    {
        var sql=FunctionGuard(ConsumeSignature,Consume(installed))
            +FunctionGuard(ReturnSignature,Return(installed))
            +FunctionGuard(LandedSignature,Landed(installed))
            +FunctionGuard(ReportSignature,Report(installed),"$company_report$");
        if(installed)
        {
            foreach(var (name,signature) in new[]
            {
                ("restore_fifo_for_material_return","uuid,uuid,boolean"),
                ("record_fifo_consumption_creation_order",""),
                ("guard_fifo_restoration_evidence","")
            })
                sql+=FunctionGuard("advance."+name+"("+signature+")",Extract(Ledger,"advance."+name),privateFunction:true,securityDefiner:false);
            sql+=FunctionGuard("advance.guard_material_return_fifo_restoration()",Extract(Ledger,"advance.guard_material_return_fifo_restoration"),privateFunction:true);
        }
        return sql;
    }
    internal const string Ownership="""
        REVOKE ALL ON advance.fifo_cost_restorations,advance.fifo_consumption_creation_order FROM PUBLIC;
        DO $roles$ BEGIN
          IF to_regrole('nexa_erp_owner') IS NOT NULL THEN
            ALTER TABLE advance.fifo_cost_restorations OWNER TO nexa_erp_owner;
            ALTER TABLE advance.fifo_consumption_creation_order OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.restore_fifo_for_material_return(uuid,uuid,boolean) OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.record_fifo_consumption_creation_order() OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.guard_fifo_restoration_evidence() OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.guard_material_return_fifo_restoration() OWNER TO nexa_erp_owner;
            REVOKE ALL ON advance.fifo_cost_restorations,advance.fifo_consumption_creation_order
              FROM nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
            REVOKE ALL ON FUNCTION advance.restore_fifo_for_material_return(uuid,uuid,boolean),
              advance.record_fifo_consumption_creation_order(),advance.guard_fifo_restoration_evidence(),advance.guard_material_return_fifo_restoration()
              FROM nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
            EXECUTE format('REVOKE ALL ON SEQUENCE %s FROM PUBLIC,nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration',
              pg_get_serial_sequence('advance.fifo_consumption_creation_order','CreationOrdinal'));
          END IF;
        END $roles$;
        """;
}
