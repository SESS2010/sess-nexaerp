using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>MIR queues use the same selected-company visibility as the MIR read/command module.
/// Source page grants, effective identity/roles, independent approval and other document scopes are unchanged.</summary>
[DbContext(typeof(NexaErpDbContext))]
[Migration("20261003083000_MirPendingScopeAlignment")]
public sealed class MirPendingScopeAlignment : Migration
{
    private const string HistorySignature = "advance.tracking_history(text,uuid,uuid[],text,text,uuid)";
    private const string StoresSignature = "advance.stores_workload(text,uuid,uuid[],text,text,uuid,bigint,integer)";
    private const string HistoryBefore = "SELECT 'stores.material-issue-requests',false,m.\"RequestNumber\"";
    private const string HistoryAfter = "SELECT 'stores.material-issue-requests',true,m.\"RequestNumber\"";
    private const string StoresBefore = "WHERE EXISTS(SELECT 1 FROM scopes scope WHERE";
    private const string StoresAfter = "WHERE s.kind='MIR' OR EXISTS(SELECT 1 FROM scopes scope WHERE";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            LOCK TABLE advance.tracking_queues IN SHARE ROW EXCLUSIVE MODE;
            DO $guard$ BEGIN
              IF (SELECT count(*) FROM advance.tracking_queues WHERE "Queue" IN('mir-approval','mir-unissued')
                AND "DocType"='MIR' AND "PageKey"='stores.material-issue-requests' AND "ScopeRule"='OPERATIONAL')<>2 THEN
                RAISE EXCEPTION 'MIR scope alignment requires the two unchanged operational MIR queues.';
              END IF;
            END $guard$;
            CREATE TABLE advance.r1_mir_pending_scope_backup(component text PRIMARY KEY,before_value text NOT NULL,after_value text NOT NULL);
            REVOKE ALL ON advance.r1_mir_pending_scope_backup FROM PUBLIC;
            INSERT INTO advance.r1_mir_pending_scope_backup
              SELECT 'queue:'||q."Queue",to_jsonb(q)::text,'' FROM advance.tracking_queues q
              WHERE q."Queue" IN('mir-approval','mir-unissued');
            UPDATE advance.tracking_queues SET "ScopeRule"='COMPANY',"UpdatedBy"='MirPendingScopeAlignment'
              WHERE "Queue" IN('mir-approval','mir-unissued');
            UPDATE advance.r1_mir_pending_scope_backup b SET after_value=to_jsonb(q)::text
              FROM advance.tracking_queues q WHERE b.component='queue:'||q."Queue";
            """);
        migrationBuilder.Sql(RewriteInstalledSql(HistorySignature, HistoryBefore, HistoryAfter));
        migrationBuilder.Sql(RewriteInstalledSql(StoresSignature, StoresBefore, StoresAfter));
        // Existing SECURITY DEFINER functions retain their signatures, owner, ACL and SET search_path.
        // No new function or runtime authority is introduced; no installer grant entry is required.
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            LOCK TABLE advance.tracking_queues IN SHARE ROW EXCLUSIVE MODE;
            DO $guard$ BEGIN
              IF EXISTS(SELECT 1 FROM advance.r1_mir_pending_scope_backup b
                LEFT JOIN advance.tracking_queues q ON b.component='queue:'||q."Queue"
                WHERE b.component LIKE 'queue:%' AND to_jsonb(q)::text IS DISTINCT FROM b.after_value)
                OR EXISTS(SELECT 1 FROM advance.r1_mir_pending_scope_backup b
                  WHERE b.component LIKE 'advance.%' AND replace(pg_get_functiondef(b.component::regprocedure),E'\r\n',E'\n')
                    IS DISTINCT FROM b.after_value) THEN
                RAISE EXCEPTION 'MIR scope rollback refuses changed or removed queues/functions.';
              END IF;
            END $guard$;
            DO $restore$ DECLARE saved record; BEGIN
              FOR saved IN SELECT * FROM advance.r1_mir_pending_scope_backup WHERE component LIKE 'advance.%' LOOP
                EXECUTE saved.before_value;
              END LOOP;
            END $restore$;
            UPDATE advance.tracking_queues q SET "ScopeRule"=old."ScopeRule","UpdatedBy"=old."UpdatedBy"
              FROM advance.r1_mir_pending_scope_backup b
              CROSS JOIN LATERAL jsonb_populate_record(NULL::advance.tracking_queues,b.before_value::jsonb) old
              WHERE b.component='queue:'||q."Queue";
            DROP TABLE advance.r1_mir_pending_scope_backup;
            """);
    }

    private static string RewriteInstalledSql(string signature, string before, string after) => $$"""
        DO $rewrite$ DECLARE installed text; rewritten text; BEGIN
          SELECT replace(pg_get_functiondef(p.oid),E'\r\n',E'\n') INTO STRICT installed FROM pg_proc p
            JOIN pg_language l ON l.oid=p.prolang WHERE p.oid='{{signature}}'::regprocedure
              AND l.lanname='sql' AND p.prosecdef;
          IF (length(installed)-length(replace(installed,{{InstalledFunctionSql.Quote(before)}},'')))
            /length({{InstalledFunctionSql.Quote(before)}})<>1 THEN
            RAISE EXCEPTION 'MIR scope alignment refuses an unexpected installed SQL function contract.';
          END IF;
          rewritten:=replace(installed,{{InstalledFunctionSql.Quote(before)}},{{InstalledFunctionSql.Quote(after)}});
          INSERT INTO advance.r1_mir_pending_scope_backup VALUES('{{signature}}',installed,rewritten);
          EXECUTE rewritten;
          UPDATE advance.r1_mir_pending_scope_backup SET after_value=replace(pg_get_functiondef('{{signature}}'::regprocedure),E'\r\n',E'\n')
            WHERE component='{{signature}}';
        END $rewrite$;
        """;
}
