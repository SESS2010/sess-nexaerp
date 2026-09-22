using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260913020000_FitmentIssueHeaderLockOrder")]
public sealed class FitmentIssueHeaderLockOrder : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(FitmentIssueHeaderLockOrderSql.Guard(FitmentIssueHeaderLockOrderSql.Before));
        migrationBuilder.Sql(FitmentIssueHeaderLockOrderSql.After);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(FitmentIssueHeaderLockOrderSql.Guard(FitmentIssueHeaderLockOrderSql.After));
        migrationBuilder.Sql(FitmentIssueHeaderLockOrderSql.Before);
    }
}

internal static class FitmentIssueHeaderLockOrderSql
{
    // The earlier migration helper is an immutable baseline. Do not edit it.
    internal static string Before => ComponentFitmentActualBomSql.LandedCostCompatibleConfirm.Replace("\r\n", "\n");

    internal static string After
    {
        get
        {
            const string oldLookup = """
          SELECT * INTO line_row FROM advance.material_issue_lines
            WHERE "CompanyId"=p_company AND "Id"=p_issue_line FOR UPDATE;
        """;
            const string parentLookup = """
          -- Read the immutable parent reference without taking the child lock.
          SELECT * INTO line_row FROM advance.material_issue_lines
            WHERE "CompanyId"=p_company AND "Id"=p_issue_line;
        """;
            const string afterHeader = """
          SELECT * INTO item_row FROM advance.items WHERE "Id"=line_row."ItemId";
        """;
            const string lockChild = """
          -- Return acceptance locks the issue header before inserting child references.
          -- Take the same order, then revalidate the child's parent under its lock.
          SELECT * INTO line_row FROM advance.material_issue_lines
            WHERE "CompanyId"=p_company AND "Id"=p_issue_line
              AND "MaterialIssueId"=issue_row."Id" FOR UPDATE;
          IF NOT FOUND THEN RAISE EXCEPTION 'MaterialIssueLineId no longer belongs to the locked issue.'; END IF;
          SELECT * INTO item_row FROM advance.items WHERE "Id"=line_row."ItemId";
        """;
            var sql = ReplaceOnce(Before, oldLookup, parentLookup);
            return ReplaceOnce(sql, afterHeader, lockChild);
        }
    }

    private static string ReplaceOnce(string sql, string before, string after)
    {
        before = before.Replace("\r\n", "\n");
        after = after.Replace("\r\n", "\n");
        var index = sql.IndexOf(before, StringComparison.Ordinal);
        if (index < 0 || sql.IndexOf(before, index + before.Length, StringComparison.Ordinal) >= 0)
            throw new InvalidOperationException("The immutable fitment lock-order baseline changed.");
        return sql[..index] + after + sql[(index + before.Length)..];
    }

    internal static string Guard(string expectedSql)
    {
        const string delimiter = "$function$";
        var start = expectedSql.IndexOf(delimiter, StringComparison.Ordinal) + delimiter.Length;
        var end = expectedSql.LastIndexOf(delimiter, StringComparison.Ordinal);
        if (start < delimiter.Length || end <= start)
            throw new InvalidOperationException("Fitment function body was not found.");
        var expectedBody = expectedSql[start..end].Replace("\r\n", "\n").Replace("'", "''");
        return $$"""
            DO $guard$
            DECLARE target regprocedure;
            BEGIN
              IF current_setting('server_version_num')::integer < 170000
                 OR current_database() IN ('postgres','template0','template1') THEN
                RAISE EXCEPTION 'Fitment lock-order migration refuses this cluster or administrative database.';
              END IF;
              target:=to_regprocedure('advance.confirm_component_fitment(uuid,uuid,uuid,numeric,timestamptz,text,uuid,text,text,text,uuid,text,uuid,text,text)');
              IF target IS NULL OR NOT EXISTS(
                SELECT 1 FROM pg_proc WHERE oid=target
                  AND replace(prosrc,E'\r\n',E'\n')='{{expectedBody}}') THEN
                RAISE EXCEPTION 'Fitment lock-order migration refuses an absent or changed function baseline.';
              END IF;
            END $guard$;
            """;
    }
}
