using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260913090000_GovernedVendorBankAdvice")]
public sealed class GovernedVendorBankAdvice : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(VendorBankAdviceMigrationSql.Guard(false));
        migrationBuilder.Sql(VendorBankAdviceSql.Create);
        migrationBuilder.Sql(VendorBankAdviceMigrationSql.AdvanceAfter);
        migrationBuilder.Sql(VendorBankAdviceMigrationSql.PaymentAfter);
        migrationBuilder.Sql(VendorBankAdviceSql.ConfigureOwnership);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(VendorBankAdviceMigrationSql.Guard(true));
        migrationBuilder.Sql(VendorBankAdviceMigrationSql.RefuseEvidenceLoss);
        migrationBuilder.Sql(VendorBankAdviceMigrationSql.AdvanceBefore);
        migrationBuilder.Sql(VendorBankAdviceMigrationSql.PaymentBefore);
        migrationBuilder.Sql("""
            DROP FUNCTION advance.require_vendor_bank_advice(uuid,uuid,text);
            DROP FUNCTION advance.vendor_bank_advice_content(uuid,uuid);
            DROP FUNCTION advance.vendor_bank_advice_json(uuid,uuid,boolean);
            DROP FUNCTION advance.record_vendor_bank_advice(uuid,uuid,text,text,bytea,text,text,text,uuid,text,uuid,text,text);
            DROP TABLE advance.vendor_bank_advices;
            """);
    }
}

internal static class VendorBankAdviceMigrationSql
{
    internal const string UploadSignature = "advance.record_vendor_bank_advice(uuid,uuid,text,text,bytea,text,text,text,uuid,text,uuid,text,text)";
    internal const string MetadataSignature = "advance.vendor_bank_advice_json(uuid,uuid,boolean)";
    internal const string ContentSignature = "advance.vendor_bank_advice_content(uuid,uuid)";
    internal const string RequireSignature = "advance.require_vendor_bank_advice(uuid,uuid,text)";
    private const string AdvanceSignature = "advance.record_vendor_advance(uuid,uuid,date,numeric,text,text,text,text,text,uuid,text,uuid,text,text)";
    private const string PaymentSignature = "advance.record_vendor_payment(uuid,uuid,date,numeric,text,text,text,jsonb,text,text,uuid,text,uuid,text,text)";
    internal static string PaymentBefore => ForeignPaymentBankAdviceSql.After;
    internal static string AdvanceBefore
    {
        get
        {
            var source = VendorAdvancePaymentSql.Up.Replace("\r\n", "\n");
            var start = source.IndexOf("CREATE FUNCTION advance.record_vendor_advance(", StringComparison.Ordinal);
            var end = source.IndexOf("CREATE FUNCTION advance.reverse_vendor_advance(", StringComparison.Ordinal);
            if (start < 0 || end <= start) throw new InvalidOperationException("Immutable advance function was not found.");
            return source[start..end].Replace("CREATE FUNCTION advance.record_vendor_advance(",
                "CREATE OR REPLACE FUNCTION advance.record_vendor_advance(", StringComparison.Ordinal);
        }
    }
    internal static string AdvanceAfter => InsertBefore(AdvanceBefore,
        " SELECT coalesce(sum(a.\"Amount\"),0) INTO used FROM advance.vendor_advances a",
        """
         IF po."CurrencyCode"<>'INR' OR btrim(p_evidence) LIKE 'bank-advice:%' THEN
          PERFORM advance.require_vendor_bank_advice(p_company,po."VendorId",p_evidence);
         END IF;
        """);
    internal static string PaymentAfter => InsertBefore(PaymentBefore,
        " IF total<>p_amount THEN",
        """
         IF upper(btrim(p_currency))<>'INR' OR btrim(coalesce(p_evidence,'')) LIKE 'bank-advice:%' THEN
          PERFORM advance.require_vendor_bank_advice(p_company,p_vendor,p_evidence);
         END IF;
        """);

    internal const string RefuseEvidenceLoss = """
        DO $guard$ BEGIN
          IF EXISTS(SELECT 1 FROM advance.vendor_bank_advices) THEN
            RAISE EXCEPTION 'Bank advice rollback refuses retained financial documents.';
          END IF;
        END $guard$;
        """;

    internal static string Guard(bool installed)
    {
        var sql = """
            DO $guard$
            BEGIN
              IF current_setting('server_version_num')::integer<170000
                OR lower(current_database()) IN('postgres','template0','template1')
                OR to_regprocedure('pg_catalog.sha256(bytea)') IS NULL THEN
                RAISE EXCEPTION 'Bank advice migration refuses this cluster or protected database.';
              END IF;
            END $guard$;
            """;
        if (!installed)
        {
            sql += """
                DO $guard$ BEGIN
                  IF to_regclass('advance.vendor_bank_advices') IS NOT NULL
                    OR to_regprocedure('advance.record_vendor_bank_advice(uuid,uuid,text,text,bytea,text,text,text,uuid,text,uuid,text,text)') IS NOT NULL
                    OR to_regprocedure('advance.vendor_bank_advice_json(uuid,uuid,boolean)') IS NOT NULL
                    OR to_regprocedure('advance.vendor_bank_advice_content(uuid,uuid)') IS NOT NULL
                    OR to_regprocedure('advance.require_vendor_bank_advice(uuid,uuid,text)') IS NOT NULL THEN
                    RAISE EXCEPTION 'Bank advice migration refuses an existing or partial installation.';
                  END IF;
                END $guard$;
                """ + VendorCurrencyReadSql.Guard(true);
        }
        else
        {
            sql += """
                DO $guard$ BEGIN
                  IF to_regclass('advance.vendor_bank_advices') IS NULL THEN
                    RAISE EXCEPTION 'Bank advice rollback requires the installed evidence table.';
                  END IF;
                  IF NOT EXISTS(SELECT 1 FROM pg_class WHERE oid='advance.vendor_bank_advices'::regclass
                    AND relowner=(SELECT relowner FROM pg_class WHERE oid='advance.vendor_advances'::regclass))
                    OR (SELECT count(*) FROM pg_attribute WHERE attrelid='advance.vendor_bank_advices'::regclass
                      AND attname IN('SizeBytes','ContentSha256','EvidenceObjectKey') AND attgenerated='s')<>3
                    OR NOT EXISTS(SELECT 1 FROM pg_trigger WHERE tgrelid='advance.vendor_bank_advices'::regclass
                      AND tgname='trg_vendor_bank_advices_immutable' AND tgenabled='O'
                      AND tgfoid='advance.guard_vendor_financial_evidence()'::regprocedure) THEN
                    RAISE EXCEPTION 'Bank advice rollback refuses changed evidence protection.';
                  END IF;
                END $guard$;
                """;
            foreach (var (signature, name, runtime) in new[]
            {
                (UploadSignature,"record_vendor_bank_advice",true),
                (MetadataSignature,"vendor_bank_advice_json",true),
                (ContentSignature,"vendor_bank_advice_content",true),
                (RequireSignature,"require_vendor_bank_advice",false)
            }) sql += FunctionGuard(signature, Definition(name), runtime);
        }
        return sql + FunctionGuard(AdvanceSignature, installed ? AdvanceAfter : AdvanceBefore, true)
            + FunctionGuard(PaymentSignature, installed ? PaymentAfter : PaymentBefore, true);
    }

    private static string Definition(string name)
    {
        var sql = VendorBankAdviceSql.Create.Replace("\r\n", "\n");
        var start = sql.IndexOf("CREATE FUNCTION advance." + name + "(", StringComparison.Ordinal);
        if (start < 0) throw new InvalidOperationException("Bank advice function definition was not found.");
        var end = sql.IndexOf("CREATE FUNCTION advance.", start + 1, StringComparison.Ordinal);
        return end < 0 ? sql[start..] : sql[start..end];
    }
    private static string InsertBefore(string source, string marker, string insertion)
    {
        var index = source.IndexOf(marker, StringComparison.Ordinal);
        if (index < 0 || source.IndexOf(marker, index + marker.Length, StringComparison.Ordinal) >= 0)
            throw new InvalidOperationException("Immutable financial evidence validation anchor changed.");
        return source[..index] + insertion.Replace("\r\n", "\n") + "\n" + source[index..];
    }
    private static string FunctionGuard(string signature, string sql, bool runtime)
    {
        var start = sql.IndexOf("$f$", StringComparison.Ordinal);
        var end = sql.LastIndexOf("$f$", StringComparison.Ordinal);
        if (start < 0 || end <= start) throw new InvalidOperationException("Financial function body was not found.");
        var body = sql[(start + 3)..end].Replace("\r\n", "\n").Replace("'", "''");
        var permitted = runtime ? "coalesce(to_regrole('nexa_erp_runtime')::oid,p.proowner)" : "p.proowner";
        return $$"""
            DO $guard$ BEGIN
              IF NOT EXISTS(SELECT 1 FROM pg_proc p WHERE p.oid=to_regprocedure('{{signature}}')
                AND p.prosecdef AND p.proconfig=ARRAY['search_path=pg_catalog, advance']
                AND p.proowner=(SELECT relowner FROM pg_class WHERE oid='advance.vendor_advances'::regclass)
                AND replace(p.prosrc,E'\r\n',E'\n')='{{body}}'
                AND NOT EXISTS(SELECT 1 FROM aclexplode(coalesce(p.proacl,acldefault('f',p.proowner))) a
                  WHERE a.grantee<>p.proowner AND (a.grantee<>{{permitted}} OR a.is_grantable))) THEN
                RAISE EXCEPTION 'Bank advice migration refuses an absent or changed function baseline: {{signature}}.';
              END IF;
            END $guard$;
            """;
    }
}
