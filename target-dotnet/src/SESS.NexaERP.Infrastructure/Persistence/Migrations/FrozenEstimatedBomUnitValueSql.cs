namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class FrozenEstimatedBomUnitValueSql
{
    private const string OldInsert = """
          INSERT INTO advance.estimated_bom_lines
            ("Id","CompanyId","EstimatedBomRevisionId","LineNumber","ItemId","UomId","Quantity","Remarks","CreatedAt","CreatedBy")
          SELECT gen_random_uuid(),p_company_id,p_revision_id,x."lineNumber",x."itemId",x."uomId",x."quantity",
                 nullif(trim(coalesce(x."remarks",'')),''),clock_timestamp(),trim(p_created_by)
          FROM jsonb_to_recordset(p_lines) AS x("lineNumber" integer,"itemId" uuid,"uomId" uuid,"quantity" numeric,"remarks" text)
          ORDER BY x."lineNumber";
        """;

    private const string NewInsert = """
          INSERT INTO advance.estimated_bom_lines
            ("Id","CompanyId","EstimatedBomRevisionId","LineNumber","ItemId","UomId","Quantity","Remarks","EstimatedUnitValue","EstimatedUnitValueOverridden","CreatedAt","CreatedBy")
          SELECT gen_random_uuid(),p_company_id,p_revision_id,x."lineNumber",x."itemId",x."uomId",x."quantity",
                 nullif(trim(coalesce(x."remarks",'')),''),x."estimatedUnitValue",coalesce(x."estimatedUnitValueOverridden",false),clock_timestamp(),trim(p_created_by)
          FROM jsonb_to_recordset(p_lines) AS x("lineNumber" integer,"itemId" uuid,"uomId" uuid,"quantity" numeric,"remarks" text,"estimatedUnitValue" numeric,"estimatedUnitValueOverridden" boolean)
          ORDER BY x."lineNumber";
        """;

    internal static string Up => Recreate(ControlledEstimatedBomDraftReplacementSql.Up)
        .Replace(OldInsert, NewInsert, StringComparison.Ordinal);

    internal static string Down => Recreate(ControlledEstimatedBomDraftReplacementSql.Up);

    private static string Recreate(string sql) => sql.Replace(
        "CREATE FUNCTION advance.replace_estimated_bom_draft_lines",
        "CREATE OR REPLACE FUNCTION advance.replace_estimated_bom_draft_lines",
        StringComparison.Ordinal);
}