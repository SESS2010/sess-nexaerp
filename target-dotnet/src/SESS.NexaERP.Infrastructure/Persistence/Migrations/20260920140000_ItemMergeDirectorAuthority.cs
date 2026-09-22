using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>
/// Finding #24 and the item permission move. The Estimated BOM governance trigger also guards
/// item_merge_aliases, but its Estimated BOM clause reads NEW."Action" in the same expression as
/// the table-name test; PL/pgSQL prepares the whole expression, so on item_merge_aliases (which
/// has no Action column) every insert failed with 42703 and no item merge had ever succeeded.
/// The clause is nested so the column is read only on estimated_bom_history, and the merge clause
/// names the Technical Director as the only merge authority (decision of 20 September 2026).
/// Both edits derive the new body from the installed one and refuse anything unexpected.
/// </summary>
[DbContext(typeof(NexaErpDbContext))]
[Migration("20260920140000_ItemMergeDirectorAuthority")]
public sealed class ItemMergeDirectorAuthority : Migration
{
    private const string PriorBomClause =
        """IF TG_TABLE_NAME='estimated_bom_history' AND NEW."Action" IN ('Approve','ReturnToDraft') AND a."AssignmentType"='SUPPORT' THEN RAISE EXCEPTION 'SUPPORT authority cannot approve or return an Estimated BOM to Draft.'; END IF;""";

    private const string NextBomClause =
        """IF TG_TABLE_NAME='estimated_bom_history' THEN IF NEW."Action" IN ('Approve','ReturnToDraft') AND a."AssignmentType"='SUPPORT' THEN RAISE EXCEPTION 'SUPPORT authority cannot approve or return an Estimated BOM to Draft.'; END IF; END IF;""";

    private const string PriorMergeClause =
        """IF TG_TABLE_NAME='item_merge_aliases' AND (a."AssignmentType"='SUPPORT' OR a."Code" NOT IN ('STORES_MANAGER','PURCHASE_MANAGER')) THEN RAISE EXCEPTION 'Item merge requires FULL or TEMPORARY STORES_MANAGER or PURCHASE_MANAGER authority.'; END IF;""";

    private const string NextMergeClause =
        """IF TG_TABLE_NAME='item_merge_aliases' AND (a."AssignmentType"='SUPPORT' OR a."Code"<>'TECHNICAL_DIRECTOR') THEN RAISE EXCEPTION 'Item merge requires FULL or TEMPORARY TECHNICAL_DIRECTOR authority.'; END IF;""";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(Rewrite(PriorBomClause, NextBomClause, PriorMergeClause, NextMergeClause, rollback: false));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(Rewrite(NextBomClause, PriorBomClause, NextMergeClause, PriorMergeClause, rollback: true));
    }

    private static string Rewrite(string fromBom, string toBom, string fromMerge, string toMerge, bool rollback)
    {
        var evidence = rollback ? """
              IF EXISTS (SELECT 1 FROM advance.item_merge_aliases WHERE "ActorRoleCode"='TECHNICAL_DIRECTOR') THEN
                RAISE EXCEPTION 'Refusing rollback: Technical Director item merge evidence exists.';
              END IF;
            """ : string.Empty;
        return MigrationText.Lf($$"""
            DO $rewrite$
            DECLARE body text; next_body text;
            BEGIN
              IF to_regclass('advance.item_merge_aliases') IS NULL OR to_regclass('advance.estimated_bom_history') IS NULL
                 OR to_regprocedure('advance.guard_estimated_bom_governance()') IS NULL THEN
                RAISE EXCEPTION 'Estimated BOM governance is absent or partial.';
              END IF;
              IF (SELECT count(*) FROM pg_trigger WHERE tgfoid='advance.guard_estimated_bom_governance()'::regprocedure AND NOT tgisinternal
                    AND tgrelid IN ('advance.item_merge_aliases'::regclass,'advance.estimated_bom_history'::regclass)) <> 2 THEN
                RAISE EXCEPTION 'Estimated BOM governance trigger is not attached to both governed tables.';
              END IF;
              {{evidence}}
              SELECT replace(prosrc,E'\r\n',E'\n') INTO STRICT body FROM pg_proc WHERE oid='advance.guard_estimated_bom_governance()'::regprocedure;
              IF (length(body)-length(replace(body,{{Quote(fromBom)}},'')))/length({{Quote(fromBom)}}) <> 1
                 OR (length(body)-length(replace(body,{{Quote(fromMerge)}},'')))/length({{Quote(fromMerge)}}) <> 1 THEN
                RAISE EXCEPTION 'guard_estimated_bom_governance is not at the expected contract; refusing to rewrite it.';
              END IF;
              next_body := replace(replace(body,{{Quote(fromBom)}},{{Quote(toBom)}}),{{Quote(fromMerge)}},{{Quote(toMerge)}});
              EXECUTE format('CREATE OR REPLACE FUNCTION advance.guard_estimated_bom_governance() RETURNS trigger LANGUAGE plpgsql AS %L', next_body);
            END $rewrite$;
            """);
    }

    private static string Quote(string clause) => "'" + clause.Replace("'", "''", StringComparison.Ordinal) + "'";
}
