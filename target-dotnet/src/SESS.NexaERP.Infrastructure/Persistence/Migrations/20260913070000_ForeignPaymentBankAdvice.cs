using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260913070000_ForeignPaymentBankAdvice")]
public sealed class ForeignPaymentBankAdvice : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(ForeignPaymentBankAdviceSql.Guard(ForeignPaymentBankAdviceSql.Before));
        migrationBuilder.Sql(ForeignPaymentBankAdviceSql.After);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(ForeignPaymentBankAdviceSql.Guard(ForeignPaymentBankAdviceSql.After));
        migrationBuilder.Sql(ForeignPaymentBankAdviceSql.Before);
    }
}

internal static class ForeignPaymentBankAdviceSql
{
    internal static string Before => VendorPaymentBillLockOrderSql.After;
    internal static string After
    {
        get
        {
            const string anchor = " IF p_amount<=0 OR p_date IS NULL OR jsonb_typeof(p_lines)<>'array'";
            const string refusal = """
                 IF upper(btrim(p_currency))<>'INR' AND btrim(coalesce(p_evidence,''))='' THEN
                  RAISE EXCEPTION 'Bank advice evidence reference is required for a foreign payment.';
                 END IF;
                """;
            var before = Before;
            var index = before.IndexOf(anchor, StringComparison.Ordinal);
            if (index < 0 || before.IndexOf(anchor, index + anchor.Length, StringComparison.Ordinal) >= 0)
                throw new InvalidOperationException("The immutable payment validation baseline changed.");
            return before[..index] + refusal.Replace("\r\n", "\n") + "\n" + before[index..];
        }
    }
    internal static string Guard(string expected) => """
        DO $guard$
        BEGIN
          IF lower(current_database()) IN ('postgres','template0','template1') THEN
            RAISE EXCEPTION 'Foreign payment migration refuses the protected owner database.';
          END IF;
        END $guard$;
        """ + VendorPaymentBillLockOrderSql.Guard(expected);
}
