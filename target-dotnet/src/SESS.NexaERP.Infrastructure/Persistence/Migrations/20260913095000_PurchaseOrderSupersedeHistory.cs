using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260913095000_PurchaseOrderSupersedeHistory")]
public sealed class PurchaseOrderSupersedeHistory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(PurchaseOrderSupersedeHistorySql.Change(true));
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(PurchaseOrderSupersedeHistorySql.Change(false));
    }
}

internal static class PurchaseOrderSupersedeHistorySql
{
    private const string Signature = "advance.rev869b_guard_history_insert()";
    private const string Anchor = "  IF NOT authorized THEN";
    private const string Marker = "/*ITEM11_PO_SUPERSEDE_APPROVAL*/";
    private const string CreatorMarker = "/*ITEM16_CREATOR_HISTORY:";
    private static string CreatorQuery
    {
        get
        {
            var source = RetainedIdentityHistorySql.Up.Replace("\r\n", "\n");
            const string prefix = "new_query:='";
            const string marker = prefix + "SELECT count(DISTINCT m.\"EmployeeId\"),min(m.\"EmployeeId\"::text)::uuid INTO creator_matches,parent_creator_employee";
            var first = source.IndexOf(marker, StringComparison.Ordinal);
            if (first < 0) throw new InvalidOperationException("Retained creator query changed.");
            first += prefix.Length;
            var last = source.IndexOf("';", first, StringComparison.Ordinal);
            if (last <= first) throw new InvalidOperationException("Retained creator query delimiter changed.");
            return source[first..last].Replace("''", "'", StringComparison.Ordinal);
        }
    }
    private static readonly string Patch = """
  /*ITEM11_PO_SUPERSEDE_APPROVAL*/
  IF NOT authorized AND NEW."Action"='Supersede' AND history_entity_type='PurchaseOrder'
    AND TG_TABLE_NAME IN('purchase_order_history','purchase_transaction_status_history') THEN
    SELECT EXISTS(
      SELECT 1 FROM advance.purchase_orders predecessor
      JOIN advance.purchase_orders replacement ON replacement."PreviousVersionId"=predecessor."Id"
      CROSS JOIN LATERAL jsonb_array_elements(replacement."ApprovalWorkflowSnapshotJson"::jsonb->'steps') approval_step
      WHERE predecessor."Id"=history_entity_id AND predecessor."Status"='Superseded'
        AND NOT predecessor."IsCurrentVersion" AND predecessor.xmin::text::bigint=txid_current()
        AND replacement."CompanyId"=predecessor."CompanyId"
        AND replacement."OrganizationId"=predecessor."OrganizationId"
        AND replacement."RootPurchaseOrderId"=predecessor."RootPurchaseOrderId"
        AND replacement."RevisionNumber"=predecessor."RevisionNumber"+1
        AND replacement."Status"='Approved' AND replacement."IsCurrentVersion"
        AND replacement.xmin::text::bigint=txid_current()
        AND parent_correlation=replacement."TransitionCorrelationId"||':prior'
        AND replacement."UpdatedBy"=NEW."ActorLoginId"
        AND replacement."CreatorEmployeeId"<>NEW."ActorEmployeeId"
        AND replacement."CompletedApprovalStepCount"=replacement."RequiredApprovalStepCount"
        AND replacement."RequiredApprovalStepCount">0
        AND replacement."RequiredApprovalStepCount"=jsonb_array_length(replacement."ApprovalWorkflowSnapshotJson"::jsonb->'steps')
        AND (approval_step->>'stepNumber')::integer=replacement."RequiredApprovalStepCount"
        AND (approval_step->>'employeeId')::uuid=NEW."ActorEmployeeId"
        AND approval_step->>'roleCode'=NEW."ActorRoleCode") INTO authorized;
  END IF;
""".Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";

    private static string BeforeBody
    {
        get
        {
            var definition = Rev869BControlledMutationSql.ReconcileHistoryAuthority
                .Replace("rev869b_command_context_valid", "ordinary_command_context_valid", StringComparison.Ordinal)
                .Replace("rev869b_claim_command_context", "ordinary_claim_command_context", StringComparison.Ordinal)
                .Replace("'advance.rev869b_", "'advance.ordinary_", StringComparison.Ordinal)
                .Replace("\r\n", "\n");
            var first = definition.IndexOf("$rev869b$", StringComparison.Ordinal);
            var last = definition.LastIndexOf("$rev869b$", StringComparison.Ordinal);
            if (first < 0 || last <= first) throw new InvalidOperationException("History baseline delimiter changed.");
            return definition[(first + "$rev869b$".Length)..last];
        }
    }
    private static string Quote(string value) => "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";

    internal static string Guard(bool installed) => $$"""
        DO $supersede_guard$
        DECLARE fn record; body text; restored text; comment_text text; old_query text;
          marker_pos integer; query_start integer; query_end integer; patch text:={{Quote(Patch)}};
        BEGIN
          IF current_setting('server_version_num')::integer<170000 OR
            lower(current_database()) IN('postgres','template0','template1') THEN
            RAISE EXCEPTION 'PO supersede history refuses this cluster or protected database.';
          END IF;
          SELECT p.* INTO fn FROM pg_proc p WHERE p.oid=to_regprocedure('{{Signature}}');
          IF NOT FOUND OR fn.prosecdef OR fn.provolatile<>'v'
            OR fn.proconfig IS DISTINCT FROM ARRAY['search_path=pg_catalog, advance']
            OR fn.proowner<>(SELECT relowner FROM pg_class WHERE oid='advance.purchase_order_history'::regclass) THEN
            RAISE EXCEPTION 'PO supersede history refuses changed function authority.';
          END IF;
          body:=replace(fn.prosrc,E'\r\n',E'\n');
          IF {{(installed ? "true" : "false")}} THEN
            IF position(patch IN body)=0 OR position(patch IN substring(body FROM position(patch IN body)+length(patch)))>0 THEN
              RAISE EXCEPTION 'PO supersede history refuses a missing or changed approval guard.';
            END IF;
            body:=replace(body,patch,'');
          END IF;
          IF position('{{Marker}}' IN body)>0 THEN
            RAISE EXCEPTION 'PO supersede history refuses an unexpected approval marker.';
          END IF;
          marker_pos:=position('{{CreatorMarker}}' IN body);
          IF marker_pos=0 OR position('{{CreatorMarker}}' IN substring(body FROM marker_pos+length('{{CreatorMarker}}')))>0 THEN
            RAISE EXCEPTION 'PO supersede history requires the exact retained-creator baseline.';
          END IF;
          comment_text:=substring(body FROM marker_pos FOR position('*/' IN substring(body FROM marker_pos))+1);
          old_query:=replace(convert_from(decode(substring(comment_text FROM length('{{CreatorMarker}}')+1
            FOR length(comment_text)-length('{{CreatorMarker}}')-2),'base64'),'UTF8'),E'\r\n',E'\n');
          query_start:=marker_pos+length(comment_text)+1;
          query_end:=query_start+position(';' IN substring(body FROM query_start))-1;
          IF substring(body FROM marker_pos+length(comment_text) FOR 1)<>chr(10)
            OR substring(body FROM query_start FOR query_end-query_start+1)<>{{Quote(CreatorQuery)}} THEN
            RAISE EXCEPTION 'PO supersede history refuses a changed retained-creator lookup.';
          END IF;
          restored:=substring(body FROM 1 FOR marker_pos-1)||old_query||substring(body FROM query_end+1);
          IF restored<>{{Quote(BeforeBody)}} THEN
            RAISE EXCEPTION 'PO supersede history refuses a changed prior function body.';
          END IF;
        END $supersede_guard$;
        """;

    internal static string Change(bool install) => Guard(!install) + $$"""
        DO $supersede_change$
        DECLARE definition text; patch text:={{Quote(Patch)}}; anchor text:={{Quote(Anchor)}};
        BEGIN
          definition:=replace(pg_get_functiondef('{{Signature}}'::regprocedure),E'\r\n',E'\n');
          IF position(anchor IN definition)=0 OR position(anchor IN substring(definition FROM position(anchor IN definition)+length(anchor)))>0 THEN
            RAISE EXCEPTION 'PO supersede history authority anchor changed.';
          END IF;
          {{(install ? "definition:=replace(definition,anchor,patch||anchor);" : "definition:=replace(definition,patch,'');")}}
          EXECUTE definition;
        END $supersede_change$;
        """ + Guard(install);
}