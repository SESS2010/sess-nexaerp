using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260914065000_QcDiscrepancyPostingGuard")]
public sealed class QcDiscrepancyPostingGuard:Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(QcDiscrepancyPostingSql.Guard(false));
        migrationBuilder.Sql(QcDiscrepancyPostingSql.Definition(true));
        migrationBuilder.Sql(QcDiscrepancyPostingSql.Guard(true));
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(QcDiscrepancyPostingSql.Guard(true));
        migrationBuilder.Sql("""
            DO $guard$ BEGIN
              IF EXISTS(SELECT 1 FROM advance.qc_inspection_revisions WHERE "Status"='FINALIZED'
                AND "AcceptedQuantity"=0 AND "RejectedQuantity"=0) THEN
                RAISE EXCEPTION 'QC discrepancy rollback refuses retained no-movement inspections.';
              END IF;
            END $guard$;
            """);
        migrationBuilder.Sql(QcDiscrepancyPostingSql.Definition(false));
        migrationBuilder.Sql(QcDiscrepancyPostingSql.Guard(false));
    }
}

internal static class QcDiscrepancyPostingSql
{
    internal const string Signature="advance.stores_p3b_document_posting_guard()";
    internal static string Definition(bool installed)
    {
        var all=StoresSlice3QcConcessionSql.PostUp.Replace("\r\n","\n");
        var start=all.IndexOf("CREATE OR REPLACE FUNCTION "+Signature,StringComparison.Ordinal);
        var end=all.IndexOf("$function$;",start,StringComparison.Ordinal)+"$function$;".Length;
        if(start<0||end<start)throw new InvalidOperationException("QC posting guard template is missing.");
        var definition=all[start..end];
        const string old="""(SELECT count(*) FROM advance.stock_posting_batches WHERE "QcInspectionRevisionId"=NEW."Id" AND "PostingKind"='QC_DISPOSITION')<>1""";
        const string replacement="""
            (SELECT count(*) FROM advance.stock_posting_batches WHERE "QcInspectionRevisionId"=NEW."Id"
              AND "CompanyId"=NEW."CompanyId" AND "PostingKind"='QC_DISPOSITION')
              <>(CASE WHEN NEW."AcceptedQuantity"=0 AND NEW."RejectedQuantity"=0
                AND NEW."DiscrepancyPendingQuantity"=NEW."InspectedQuantity" THEN 0 ELSE 1 END)
            """;
        if(definition.Split(old,StringSplitOptions.None).Length!=2)
            throw new InvalidOperationException("QC posting guard expected exactly one disposition check.");
        return installed?definition.Replace(old,replacement.Replace("\r\n","\n",StringComparison.Ordinal),StringComparison.Ordinal):definition;
    }

    internal static string Guard(bool installed)
    {
        var definition=Definition(installed);
        var first=definition.IndexOf("$function$",StringComparison.Ordinal)+"$function$".Length;
        var last=definition.LastIndexOf("$function$",StringComparison.Ordinal);
        var body=definition[first..last].Replace("'","''",StringComparison.Ordinal);
        return $$"""
            DO $guard$ BEGIN
              IF NOT EXISTS(SELECT 1 FROM pg_proc p WHERE p.oid=to_regprocedure('{{Signature}}')
                AND NOT p.prosecdef AND p.prorettype='trigger'::regtype AND p.provolatile='v'
                AND p.proconfig=ARRAY['search_path=pg_catalog, advance']
                AND p.proowner=(SELECT relowner FROM pg_class WHERE oid='advance.qc_inspection_revisions'::regclass)
                AND replace(p.prosrc,E'\r\n',E'\n')='{{body}}'
                AND NOT EXISTS(SELECT 1 FROM aclexplode(coalesce(p.proacl,acldefault('f',p.proowner))) a
                  WHERE a.grantee<>p.proowner AND
                    (a.is_grantable OR a.grantee<>0 OR to_regrole('nexa_erp_owner') IS NOT NULL)))
                OR NOT EXISTS(SELECT 1 FROM pg_trigger t WHERE t.tgrelid='advance.qc_inspection_revisions'::regclass
                  AND t.tgname='TR_qc_revision_atomic_posting' AND t.tgenabled='O'
                  AND t.tgdeferrable AND t.tginitdeferred AND t.tgfoid=to_regprocedure('{{Signature}}')) THEN
                RAISE EXCEPTION 'QC discrepancy migration refuses changed posting guard authority, body or trigger.';
              END IF;
            END $guard$;
            """;
    }
}
