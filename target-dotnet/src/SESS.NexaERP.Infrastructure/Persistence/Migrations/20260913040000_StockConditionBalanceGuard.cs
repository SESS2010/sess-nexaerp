using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260913040000_StockConditionBalanceGuard")]
public sealed class StockConditionBalanceGuard : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(StockConditionBalanceGuardSql.Guard(StockConditionBalanceGuardSql.Before));
        migrationBuilder.Sql(StockConditionBalanceGuardSql.After);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(StockConditionBalanceGuardSql.Guard(StockConditionBalanceGuardSql.After));
        migrationBuilder.Sql(StockConditionBalanceGuardSql.Before);
    }
}

internal static class StockConditionBalanceGuardSql
{
    internal static string Before
    {
        get
        {
            var source = StoresSlice3QcConcessionSql.PostUp.Replace("\r\n", "\n");
            var start = source.IndexOf("CREATE OR REPLACE FUNCTION advance.post_stores_stock_batch(", StringComparison.Ordinal);
            var end = source.IndexOf("REVOKE ALL ON FUNCTION advance.post_stores_stock_batch(", StringComparison.Ordinal);
            if (start < 0 || end <= start)
                throw new InvalidOperationException("The immutable stock posting function was not found.");
            return source[start..end];
        }
    }

    internal static string After
    {
        get
        {
            var source = ReplaceOnce(Before,
                "WHERE l.\"ConditionCode\"='AVAILABLE' AND coalesce((",
                "WHERE coalesce((");
            return ReplaceOnce(source,
                "Posting would drive an AVAILABLE ownership/custody/provenance layer below zero.",
                "Posting would drive an ownership/custody/provenance stock balance below zero.");
        }
    }

    private static string ReplaceOnce(string source, string before, string after)
    {
        var start = source.IndexOf(before, StringComparison.Ordinal);
        if (start < 0 || source.IndexOf(before, start + before.Length, StringComparison.Ordinal) >= 0)
            throw new InvalidOperationException("The immutable stock balance guard changed.");
        return source[..start] + after + source[(start + before.Length)..];
    }

    internal static string Guard(string expectedSql)
    {
        const string delimiter = "$function$";
        var start = expectedSql.IndexOf(delimiter, StringComparison.Ordinal) + delimiter.Length;
        var end = expectedSql.LastIndexOf(delimiter, StringComparison.Ordinal);
        if (start < delimiter.Length || end <= start)
            throw new InvalidOperationException("Stock posting function body was not found.");
        var expectedBody = expectedSql[start..end].Replace("\r\n", "\n").Replace("'", "''");
        return $$"""
            DO $guard$
            DECLARE target regprocedure;
            BEGIN
              IF current_setting('server_version_num')::integer < 170000
                 OR current_database() IN ('postgres','template0','template1') THEN
                RAISE EXCEPTION 'Stock balance migration refuses this cluster or administrative database.';
              END IF;
              target:=to_regprocedure('advance.post_stores_stock_batch(uuid,text,uuid,text,text,text,date,uuid,text,jsonb)');
              IF target IS NULL OR NOT EXISTS(
                SELECT 1 FROM pg_proc WHERE oid=target AND prosecdef
                  AND array_length(proconfig,1)=1
                  AND EXISTS(SELECT 1 FROM unnest(proconfig) AS settings(setting)
                    WHERE regexp_replace(setting,'[[:space:]]','','g')='search_path=pg_catalog,advance')
                  AND replace(prosrc,E'\r\n',E'\n')='{{expectedBody}}') THEN
                RAISE EXCEPTION 'Stock balance migration refuses an absent or changed function baseline.';
              END IF;
            END $guard$;
            """;
    }
}
