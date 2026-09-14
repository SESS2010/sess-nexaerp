using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260914045000_PurchaseOrderCancellationHistory")]
public sealed class PurchaseOrderCancellationHistory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(PurchaseOrderCancellationHistorySql.Change(true));
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(PurchaseOrderCancellationHistorySql.Change(false));
    }
}

internal static class PurchaseOrderCancellationHistorySql
{
    internal const string Signature="advance.rev869b_guard_history_insert()";
    private const string Marker="/*ITEM21_PO_CANCELLATION_DIRECTOR*/";
    private const string Anchor="  IF NOT authorized THEN";
    private const string Patch="""
  /*ITEM21_PO_CANCELLATION_DIRECTOR*/
  IF NEW."Action"='Cancel' AND history_entity_type='PurchaseOrder'
    AND TG_TABLE_NAME IN('purchase_order_history','purchase_transaction_status_history') THEN
    SELECT EXISTS(
      SELECT 1 FROM advance.purchase_orders cancelled
      WHERE cancelled."Id"=history_entity_id AND cancelled."Status"='Cancelled'
        AND cancelled."OrganizationId"=parent_org
        AND cancelled.xmin::text::bigint=txid_current()
        AND cancelled."TransitionCorrelationId"=parent_correlation
        AND cancelled."UpdatedBy"=NEW."ActorLoginId"
        AND NEW."ActorRoleCode" IN('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR')
        AND cancelled."CancelledAt" IS NOT NULL
        AND length(btrim(coalesce(cancelled."CancellationReason",'')))>0) INTO authorized;
  END IF;
"""+"\n";
    private static string Quote(string value)=>"'"+value.Replace("'","''",StringComparison.Ordinal)+"'";

    internal static string Guard(bool installed)
    {
        const string bodyAnchor="""body:=replace(fn.prosrc,E'\r\n',E'\n');""";
        var checking=bodyAnchor+"\n"+$$"""
          IF fn.prorettype<>'trigger'::regtype OR fn.prokind<>'f'
            OR fn.prolang<>(SELECT oid FROM pg_language WHERE lanname='plpgsql')
            OR fn.proisstrict OR fn.proleakproof OR fn.proparallel<>'u'
            OR EXISTS(SELECT 1 FROM aclexplode(coalesce(fn.proacl,acldefault('f',fn.proowner))) a
              WHERE a.grantee<>fn.proowner AND(a.is_grantable OR a.grantee<>0 OR to_regrole('nexa_erp_owner') IS NOT NULL))
            OR(SELECT count(DISTINCT t.tgrelid) FROM pg_trigger t
              WHERE t.tgfoid=fn.oid AND t.tgrelid IN('advance.purchase_order_history'::regclass,
                'advance.purchase_transaction_status_history'::regclass)
                AND NOT t.tgisinternal AND t.tgenabled='O' AND t.tgtype=7
                AND t.tgqual IS NULL AND t.tgnargs=0 AND t.tgattr::text='')<>2 THEN
            RAISE EXCEPTION 'PO cancellation refuses changed history function or trigger authority.';
          END IF;
          IF {{(installed ? "true" : "false")}} THEN
            IF position({{Quote(Patch)}} IN body)=0 OR
              position({{Quote(Patch)}} IN substring(body FROM position({{Quote(Patch)}} IN body)+length({{Quote(Patch)}})))>0 THEN
              RAISE EXCEPTION 'PO cancellation refuses a missing or changed director guard.';
            END IF;
            body:=replace(body,{{Quote(Patch)}},'');
          END IF;
          IF position('{{Marker}}' IN body)>0 THEN
            RAISE EXCEPTION 'PO cancellation refuses an unexpected director guard.';
          END IF;
          """;
        var baseline=PurchaseOrderSupersedeHistorySql.Guard(true);
        if(baseline.IndexOf(bodyAnchor,StringComparison.Ordinal)<0||
            baseline.IndexOf(bodyAnchor,baseline.IndexOf(bodyAnchor,StringComparison.Ordinal)+bodyAnchor.Length,StringComparison.Ordinal)>=0)
            throw new InvalidOperationException("History inspection anchor changed.");
        return baseline.Replace(bodyAnchor,checking,StringComparison.Ordinal)
            .Replace("$supersede_guard$","$cancellation_guard$",StringComparison.Ordinal)
            .Replace("PO supersede history","PO cancellation history",StringComparison.Ordinal);
    }

    internal static string Change(bool install)=>Guard(!install)+$$"""
        DO $cancellation_change$
        DECLARE definition text; patch text:={{Quote(Patch)}}; anchor text:={{Quote(Anchor)}};
        BEGIN
          definition:=pg_get_functiondef('{{Signature}}'::regprocedure);
          IF position(anchor IN definition)=0 OR position(anchor IN substring(definition FROM position(anchor IN definition)+length(anchor)))>0 THEN
            RAISE EXCEPTION 'PO cancellation history anchor changed.';
          END IF;
          {{(install ? "definition:=replace(definition,anchor,patch||anchor);" : "definition:=replace(definition,patch,'');")}}
          EXECUTE definition;
        END $cancellation_change$;
        """+Guard(install);
}
