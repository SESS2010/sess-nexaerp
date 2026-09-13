using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260913030000_VendorPaymentBillLockOrder")]
public sealed class VendorPaymentBillLockOrder : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(VendorPaymentBillLockOrderSql.Guard(VendorPaymentBillLockOrderSql.Before));
        migrationBuilder.Sql(VendorPaymentBillLockOrderSql.After);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(VendorPaymentBillLockOrderSql.Guard(VendorPaymentBillLockOrderSql.After));
        migrationBuilder.Sql(VendorPaymentBillLockOrderSql.Before);
    }
}

internal static class VendorPaymentBillLockOrderSql
{
    internal static string Before
    {
        get
        {
            // Extract the immutable earlier function, without editing its migration.
            var source = VendorAdvancePaymentSql.Up.Replace("\r\n", "\n");
            var start = source.IndexOf("CREATE FUNCTION advance.record_vendor_payment(", StringComparison.Ordinal);
            var end = source.IndexOf("CREATE FUNCTION advance.payment_due_date(", StringComparison.Ordinal);
            if (start < 0 || end <= start)
                throw new InvalidOperationException("The immutable vendor payment function was not found.");
            return source[start..end].Replace("CREATE FUNCTION advance.record_vendor_payment(",
                "CREATE OR REPLACE FUNCTION advance.record_vendor_payment(", StringComparison.Ordinal);
        }
    }

    internal static string After
    {
        get
        {
            const string before = " FOR j IN SELECT * FROM jsonb_array_elements(p_lines) LOOP";
            const string after = """
             -- All payment sessions acquire bill locks in the same order. The original
             -- allocation JSON and request fingerprint remain unchanged.
             FOR j IN SELECT value FROM jsonb_array_elements(p_lines)
               ORDER BY (value->>'vendorBillId')::uuid LOOP
            """;
            var source = Before;
            var start = source.IndexOf(before, StringComparison.Ordinal);
            if (start < 0 || source.IndexOf(before, start + before.Length, StringComparison.Ordinal) >= 0)
                throw new InvalidOperationException("The immutable vendor payment lock loop changed.");
            return source[..start] + after.Replace("\r\n", "\n") + source[(start + before.Length)..];
        }
    }

    internal static string Guard(string expectedSql)
    {
        const string delimiter = "$f$";
        var start = expectedSql.IndexOf(delimiter, StringComparison.Ordinal) + delimiter.Length;
        var end = expectedSql.LastIndexOf(delimiter, StringComparison.Ordinal);
        if (start < delimiter.Length || end <= start)
            throw new InvalidOperationException("Vendor payment function body was not found.");
        var expectedBody = expectedSql[start..end].Replace("\r\n", "\n").Replace("'", "''");
        return $$"""
            DO $guard$
            DECLARE target regprocedure;
            BEGIN
              IF current_setting('server_version_num')::integer < 170000
                 OR current_database() IN ('postgres','template0','template1') THEN
                RAISE EXCEPTION 'Payment lock-order migration refuses this cluster or administrative database.';
              END IF;
              target:=to_regprocedure('advance.record_vendor_payment(uuid,uuid,date,numeric,text,text,text,jsonb,text,text,uuid,text,uuid,text,text)');
              IF target IS NULL OR NOT EXISTS(
                SELECT 1 FROM pg_proc WHERE oid=target AND prosecdef
                  AND array_length(proconfig,1)=1
                  AND EXISTS(SELECT 1 FROM unnest(proconfig) AS settings(setting)
                    WHERE regexp_replace(setting,'[[:space:]]','','g')='search_path=pg_catalog,advance')
                  AND replace(prosrc,E'\r\n',E'\n')='{{expectedBody}}') THEN
                RAISE EXCEPTION 'Payment lock-order migration refuses an absent or changed function baseline.';
              END IF;
            END $guard$;
            """;
    }
}