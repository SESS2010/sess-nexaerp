using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>
/// Decision of the Technical Director, 20 September 2026: an Estimated BOM line is priced by
/// what the engineer typed or by a suggestion the engineer accepted (the last accepted bill's
/// landed rate, else the opening-stock carrying value); nothing is prefilled silently, and the
/// source is recorded so the variance report can tell the two claims apart. Existing lines are
/// classified from what was recorded: an override was typed by the engineer; any other priced
/// line came from the last accepted bill (the only source that existed). Approved lines are
/// immutable by trigger; the classification is added under a suspended trigger and changes
/// nothing else.
/// </summary>
[DbContext(typeof(NexaErpDbContext))]
[Migration("20260920200000_EstimatedBomValueSource")]
public sealed class EstimatedBomValueSource : Migration
{
    private const string Replace = "advance.replace_estimated_bom_draft_lines(uuid,text,uuid,bigint,uuid,text,text,text,text,jsonb)";
    private const string ColumnsBefore = @"""Remarks"",""EstimatedUnitValue"",""EstimatedUnitValueOverridden"",""CreatedAt"",""CreatedBy"")";
    private const string ColumnsAfter = @"""Remarks"",""EstimatedUnitValue"",""EstimatedUnitValueOverridden"",""ValueSource"",""CreatedAt"",""CreatedBy"")";
    private const string ValuesBefore = @"x.""estimatedUnitValue"",coalesce(x.""estimatedUnitValueOverridden"",false),clock_timestamp(),trim(p_created_by)";
    private const string ValuesAfter = @"x.""estimatedUnitValue"",coalesce(x.""estimatedUnitValueOverridden"",false),nullif(x.""valueSource"",''),clock_timestamp(),trim(p_created_by)";
    private const string RecordBefore = @"""estimatedUnitValue"" numeric,""estimatedUnitValueOverridden"" boolean)";
    private const string RecordAfter = @"""estimatedUnitValue"" numeric,""estimatedUnitValueOverridden"" boolean,""valueSource"" text)";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(MigrationText.Lf("""
            ALTER TABLE advance.estimated_bom_lines ADD COLUMN "ValueSource" varchar(30) NULL
              CONSTRAINT "CK_estimated_bom_line_value_source" CHECK ("ValueSource" IS NULL OR "ValueSource" IN ('ENGINEER','LAST_ACCEPTED_BILL','OPENING_STOCK'));
            ALTER TABLE advance.estimated_bom_lines DISABLE TRIGGER USER;
            UPDATE advance.estimated_bom_lines SET "ValueSource"=CASE WHEN "EstimatedUnitValueOverridden" THEN 'ENGINEER' ELSE 'LAST_ACCEPTED_BILL' END
              WHERE "EstimatedUnitValue" IS NOT NULL;
            ALTER TABLE advance.estimated_bom_lines ENABLE TRIGGER USER;
            """) + InstalledFunctionSql.Rewrite(Replace, (ColumnsBefore, ColumnsAfter), (ValuesBefore, ValuesAfter), (RecordBefore, RecordAfter)));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(MigrationText.Lf("""
            DO $guard$
            BEGIN
              IF EXISTS (SELECT 1 FROM advance.estimated_bom_lines WHERE "ValueSource"='OPENING_STOCK') THEN
                RAISE EXCEPTION 'Refusing rollback: Estimated BOM lines have been valued from opening stock.';
              END IF;
            END $guard$;
            """) + InstalledFunctionSql.Rewrite(Replace, (ColumnsAfter, ColumnsBefore), (ValuesAfter, ValuesBefore), (RecordAfter, RecordBefore)) + MigrationText.Lf("""
            ALTER TABLE advance.estimated_bom_lines DROP COLUMN "ValueSource";
            """));
    }
}
