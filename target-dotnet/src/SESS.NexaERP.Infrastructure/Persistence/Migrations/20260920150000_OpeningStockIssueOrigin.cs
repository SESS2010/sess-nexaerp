using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>
/// Finding #26. Every ISSUE_OUT or DISPATCH_OUT stock movement had to carry an origin GRN line
/// (CK_stock_movement_outbound_origin, August), but opening stock (September) enters without a
/// GRN, so no opening-stock quantity could ever be issued: the first MIR against the ceremony's
/// stock failed with a check violation and the API answered 500. Opening stock is now an origin
/// of its own: stock movements and material issue lines carry OriginOpeningStockLineId exactly as
/// they carry OriginGoodsReceiptLineId, the opening receipts are their own origin, the outbound
/// check accepts either origin, and the issue and return postings copy the opening origin from the
/// issue line. Both posting functions are rewritten from their installed bodies and refuse an
/// unexpected body.
/// </summary>
[DbContext(typeof(NexaErpDbContext))]
[Migration("20260920150000_OpeningStockIssueOrigin")]
public sealed class OpeningStockIssueOrigin : Migration
{
    private const string IssueColumnsBefore = @"""MaterialIssueLineId"",""OriginGoodsReceiptLineId"",";
    private const string IssueColumnsAfter = @"""MaterialIssueLineId"",""OriginGoodsReceiptLineId"",""OriginOpeningStockLineId"",";
    private const string IssueValuesBefore = @"l.""MaterialIssueRequestLineId"",l.""Id"",l.""OriginGoodsReceiptLineId"",";
    private const string IssueValuesAfter = @"l.""MaterialIssueRequestLineId"",l.""Id"",l.""OriginGoodsReceiptLineId"",l.""OriginOpeningStockLineId"",";
    private const string ReturnColumnsBefore = @"""OriginGoodsReceiptLineId"",""OwnershipAccountId"",""CustodyAssignmentId"",""InventoryProvenanceLayerId"",";
    private const string ReturnColumnsAfter = @"""OriginGoodsReceiptLineId"",""OriginOpeningStockLineId"",""OwnershipAccountId"",""CustodyAssignmentId"",""InventoryProvenanceLayerId"",";
    private const string ReturnValuesBefore = @"il.""MaterialIssueRequestLineId"",il.""Id"",rl.""Id"",il.""OriginGoodsReceiptLineId"",il.""OwnershipAccountId"",";
    private const string OpeningColumnsBefore = @"""MovementLeg"",""OpeningStockLineId"",""OwnershipAccountId"",";
    private const string OpeningColumnsAfter = @"""MovementLeg"",""OpeningStockLineId"",""OriginOpeningStockLineId"",""OwnershipAccountId"",";
    private const string OpeningValuesBefore = @"'RECEIPT_IN',line.""Id"",ownership_id,";
    private const string OpeningValuesAfter = @"'RECEIPT_IN',line.""Id"",line.""Id"",ownership_id,";
    private const string ReturnValuesAfter = @"il.""MaterialIssueRequestLineId"",il.""Id"",rl.""Id"",il.""OriginGoodsReceiptLineId"",il.""OriginOpeningStockLineId"",il.""OwnershipAccountId"",";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(MigrationText.Lf($$"""
            DO $guard$
            BEGIN
              IF to_regclass('advance.opening_stock_lines') IS NULL OR to_regclass('advance.material_issue_lines') IS NULL
                 OR to_regprocedure('advance.post_material_issue_custody(uuid,uuid,text,text,text,uuid,text)') IS NULL
                 OR to_regprocedure('advance.post_material_return_acceptance(uuid,uuid,text,text,text,uuid,text)') IS NULL
                 OR to_regprocedure('advance.authorize_opening_stock(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text)') IS NULL
                 OR NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='CK_stock_movement_outbound_origin' AND conrelid='advance.stock_movements'::regclass) THEN
                RAISE EXCEPTION 'Opening-stock issue origin requires opening stock, material issue and return postings to be installed.';
              END IF;
            END $guard$;
            ALTER TABLE advance.stock_movements ADD COLUMN "OriginOpeningStockLineId" uuid NULL
              CONSTRAINT "FK_stock_movements_opening_stock_lines_OriginOpeningStockLineId" REFERENCES advance.opening_stock_lines("Id");
            CREATE INDEX "IX_stock_movements_OriginOpeningStockLineId" ON advance.stock_movements("OriginOpeningStockLineId");
            ALTER TABLE advance.material_issue_lines ADD COLUMN "OriginOpeningStockLineId" uuid NULL
              CONSTRAINT "FK_material_issue_lines_opening_stock_lines_OriginOpeningStockLineId" REFERENCES advance.opening_stock_lines("Id");
            -- Opening receipts are their own origin, exactly as GRN receipts are.
            UPDATE advance.stock_movements SET "OriginOpeningStockLineId"="OpeningStockLineId"
              WHERE "OpeningStockLineId" IS NOT NULL AND "MovementLeg"='RECEIPT_IN';
            ALTER TABLE advance.stock_movements DROP CONSTRAINT "CK_stock_movement_outbound_origin";
            ALTER TABLE advance.stock_movements ADD CONSTRAINT "CK_stock_movement_outbound_origin"
              CHECK ("LedgerSchemaVersion"=1 OR "MovementLeg" NOT IN ('ISSUE_OUT','DISPATCH_OUT')
                OR "OriginGoodsReceiptLineId" IS NOT NULL OR "OriginOpeningStockLineId" IS NOT NULL);
            {{Rewrite("advance.post_material_issue_custody(uuid,uuid,text,text,text,uuid,text)", IssueColumnsBefore, IssueColumnsAfter, IssueValuesBefore, IssueValuesAfter)}}
            {{Rewrite("advance.post_material_return_acceptance(uuid,uuid,text,text,text,uuid,text)", ReturnColumnsBefore, ReturnColumnsAfter, ReturnValuesBefore, ReturnValuesAfter)}}
            {{Rewrite("advance.authorize_opening_stock(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text)", OpeningColumnsBefore, OpeningColumnsAfter, OpeningValuesBefore, OpeningValuesAfter)}}
            """));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(MigrationText.Lf($$"""
            DO $guard$
            BEGIN
              IF EXISTS (SELECT 1 FROM advance.stock_movements WHERE "OriginOpeningStockLineId" IS NOT NULL AND "MovementLeg"<>'RECEIPT_IN') THEN
                RAISE EXCEPTION 'Refusing rollback: opening stock has been issued or returned under its own origin.';
              END IF;
            END $guard$;
            {{Rewrite("advance.authorize_opening_stock(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text)", OpeningColumnsAfter, OpeningColumnsBefore, OpeningValuesAfter, OpeningValuesBefore)}}
            {{Rewrite("advance.post_material_issue_custody(uuid,uuid,text,text,text,uuid,text)", IssueColumnsAfter, IssueColumnsBefore, IssueValuesAfter, IssueValuesBefore)}}
            {{Rewrite("advance.post_material_return_acceptance(uuid,uuid,text,text,text,uuid,text)", ReturnColumnsAfter, ReturnColumnsBefore, ReturnValuesAfter, ReturnValuesBefore)}}
            ALTER TABLE advance.stock_movements DROP CONSTRAINT "CK_stock_movement_outbound_origin";
            ALTER TABLE advance.stock_movements ADD CONSTRAINT "CK_stock_movement_outbound_origin"
              CHECK ("LedgerSchemaVersion"=1 OR "MovementLeg" NOT IN ('ISSUE_OUT','DISPATCH_OUT') OR "OriginGoodsReceiptLineId" IS NOT NULL);
            ALTER TABLE advance.material_issue_lines DROP COLUMN "OriginOpeningStockLineId";
            ALTER TABLE advance.stock_movements DROP COLUMN "OriginOpeningStockLineId";
            """));
    }

    // Rewrites one installed function body by replacing two fragments that must each occur exactly once,
    // preserving everything else the installed body carries (later migrations included).
    private static string Rewrite(string signature, string columnsFrom, string columnsTo, string valuesFrom, string valuesTo) => $$"""
        DO $rewrite$
        DECLARE body text; header text;
        BEGIN
          SELECT replace(prosrc,E'\r\n',E'\n') INTO STRICT body FROM pg_proc WHERE oid='{{signature}}'::regprocedure;
          IF (length(body)-length(replace(body,{{Quote(columnsFrom)}},'')))/length({{Quote(columnsFrom)}}) <> 1
             OR (length(body)-length(replace(body,{{Quote(valuesFrom)}},'')))/length({{Quote(valuesFrom)}}) <> 1 THEN
            RAISE EXCEPTION '{{signature}} is not at the expected contract; refusing to rewrite it.';
          END IF;
          body := replace(replace(body,{{Quote(columnsFrom)}},{{Quote(columnsTo)}}),{{Quote(valuesFrom)}},{{Quote(valuesTo)}});
          SELECT 'CREATE OR REPLACE FUNCTION advance.'||proname||'('||pg_get_function_arguments(oid)||') RETURNS '||pg_get_function_result(oid)
                 ||' LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS '
            INTO STRICT header FROM pg_proc WHERE oid='{{signature}}'::regprocedure;
          EXECUTE header || quote_literal(body);
        END $rewrite$;
        """;

    private static string Quote(string clause) => "'" + clause.Replace("'", "''", StringComparison.Ordinal) + "'";
}
