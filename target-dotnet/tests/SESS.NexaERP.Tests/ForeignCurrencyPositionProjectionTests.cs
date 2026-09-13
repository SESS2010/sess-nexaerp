using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task VendorFinancialReadsIdentifyCurrencyAndNeverAddUnlikeAmounts()
    {
        var observed = false;
        await RunCompletePurchaseFlow(paymentRace: async context =>
        {
            observed = true;
            var company = Guid.Parse("70000000-0000-0000-0000-000000000001");
            var assignments = await Query(context.Options, async db =>
                (await db.EmployeeRoleAssignments.AsNoTracking().Include(x => x.Role)
                    .Where(x => x.CompanyId == company && x.EffectiveTo == null).ToListAsync())
                .ToDictionary(x => TaxWorkflowUser.AssignmentKey(x.EmployeeId, x.Role!.Code),
                    x => new EffectiveRoleAssignment(x.Id, x.Role!.Code, x.AssignmentType)));
            var subject = await Query(context.Options, db => db.EmployeeIdentityMappings
                .Where(x => x.CompanyId == company && x.EmployeeId == context.ApproverId && x.IsActive)
                .Select(x => x.Subject).SingleAsync());
            var actor = new TaxWorkflowUser(context.ApproverId, subject, "ACCOUNTS_MANAGER", assignments);
            await using var host = await PurchaseFlowHost.StartAsync(context.RuntimeConnection, actor, true, true);
            const string root = "/api/v1/accounts/vendor-financial-evidence/";
            var payableResponse = await host.Client.GetAsync(root + "payables?vendorId=" + context.OriginalCommand.VendorId);
            payableResponse.EnsureSuccessStatusCode();
            var positionResponse = await host.Client.GetAsync(root + "vendor-positions");
            positionResponse.EnsureSuccessStatusCode();
            var payableJson = await payableResponse.Content.ReadAsStringAsync();
            var positionJson = await positionResponse.Content.ReadAsStringAsync();
            using var payableDocument = JsonDocument.Parse(payableJson);
            var payableRows = payableDocument.RootElement.EnumerateArray().ToArray();
            Assert.Equal(2, payableRows.Length);
            var usdBill = payableRows.Single(x => x.GetProperty("OutstandingValue").GetDecimal() == 5650m);
            var inrBill = payableRows.Single(x => x.GetProperty("OutstandingValue").GetDecimal() == 118000.01m);
            var fixture = await ReadMixedCurrencyFixture(context.Options, company, context.OriginalCommand.VendorId,
                usdBill.GetProperty("PurchaseOrderId").GetGuid(), inrBill.GetProperty("PurchaseOrderId").GetGuid());
            var evidence = Path.Combine(FindRepositoryRoot(), "local-evidence", "item11");
            Directory.CreateDirectory(evidence);
            await File.WriteAllTextAsync(Path.Combine(evidence, "currency-read-projection.json"),
                JsonSerializer.Serialize(new
                {
                    ActualDomesticPayables = JsonSerializer.Deserialize<JsonElement>(payableJson),
                    ActualDomesticPositions = JsonSerializer.Deserialize<JsonElement>(positionJson),
                    MixedCurrencyReadFixture = JsonSerializer.Deserialize<JsonElement>(fixture),
                    FixtureScope = "Copied read tables and deployed function bodies in an isolated schema. Not governed foreign procurement."
                }, new JsonSerializerOptions { WriteIndented = true }));
            Assert.All(payableRows, row => Assert.Equal("INR", row.GetProperty("CurrencyCode").GetString()));
            using var positionDocument = JsonDocument.Parse(positionJson);
            Assert.All(positionDocument.RootElement.EnumerateArray(),
                row => Assert.Equal("INR", row.GetProperty("CurrencyCode").GetString()));
            using var mixed = JsonDocument.Parse(fixture);
            var rows = mixed.RootElement.EnumerateArray().ToArray();
            Assert.Equal(2, rows.Length);
            var usd = rows.Single(x => x.GetProperty("currencyCode").GetString() == "USD");
            var inr = rows.Single(x => x.GetProperty("currencyCode").GetString() == "INR");
            Assert.Equal(7m, usd.GetProperty("outstandingAdvance").GetDecimal());
            Assert.Equal(5650m, usd.GetProperty("outstandingBills").GetDecimal());
            Assert.Equal(5643m, usd.GetProperty("netPayable").GetDecimal());
            Assert.Equal(11m, inr.GetProperty("outstandingAdvance").GetDecimal());
            Assert.Equal(118000.01m, inr.GetProperty("outstandingBills").GetDecimal());
            Assert.Equal(117989.01m, inr.GetProperty("netPayable").GetDecimal());
            // The source financial tables were only read. Finish their real domestic
            // settlement and the three-band flow through the restricted runtime API.
            return await RunPaymentRace(context);
        });
        Assert.True(observed);
    }

    private static async Task<string> ReadMixedCurrencyFixture(
        DbContextOptions<NexaErpDbContext> options, Guid company, Guid vendor, Guid usdPo, Guid inrPo)
    {
        await using var db = new NexaErpDbContext(options);
        await using var connection = new NpgsqlConnection(db.Database.GetConnectionString());
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        async Task Execute(string sql)
        {
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            await command.ExecuteNonQueryAsync();
        }
        await Execute("CREATE SCHEMA currency_read_fixture;");
        foreach (var table in new[] { "vendor_advances", "vendor_advance_adjustments",
            "vendor_advance_adjustment_restorations", "vendor_advance_reversals",
            "vendor_bills", "vendor_payment_allocations", "purchase_orders", "vendors" })
            await Execute($"CREATE TABLE currency_read_fixture.{table} AS TABLE advance.{table};");
        foreach (var signature in new[] { "advance.payment_due_date(timestamp with time zone,text)",
            "advance.vendor_advance_json(uuid,uuid,boolean)",
            "advance.list_vendor_payables(uuid,uuid,boolean)", "advance.list_vendor_positions(uuid)" })
        {
            await using var definition = new NpgsqlCommand("SELECT pg_get_functiondef(to_regprocedure(@signature))", connection, transaction);
            definition.Parameters.AddWithValue("signature", signature);
            var sql = Assert.IsType<string>(await definition.ExecuteScalarAsync());
            sql = sql.Replace("advance.", "currency_read_fixture.", StringComparison.Ordinal)
                .Replace("SET search_path TO 'pg_catalog', 'advance'",
                    "SET search_path TO 'pg_catalog', 'currency_read_fixture'", StringComparison.Ordinal);
            await Execute(sql);
        }
        await using (var tag = new NpgsqlCommand("""
            UPDATE currency_read_fixture.purchase_orders SET "CurrencyCode"='USD' WHERE "Id"=@usd;
            UPDATE currency_read_fixture.vendor_advances a SET "CurrencyCode"=po."CurrencyCode"
              FROM currency_read_fixture.purchase_orders po WHERE po."Id"=a."PurchaseOrderId";
            """, connection, transaction))
        {
            tag.Parameters.AddWithValue("usd", usdPo);
            await tag.ExecuteNonQueryAsync();
        }
        foreach (var (po, currency, amount) in new[] { (usdPo, "USD", 7m), (inrPo, "INR", 11m) })
        {
            await using var seed = new NpgsqlCommand("""
                INSERT INTO currency_read_fixture.vendor_advances
                SELECT (jsonb_populate_record(NULL::currency_read_fixture.vendor_advances,
                  to_jsonb(a)||jsonb_build_object('Id',@id,'CompanyId',@company,'VendorId',@vendor,
                    'PurchaseOrderId',@po,'CurrencyCode',@currency,'Amount',@amount,
                    'AdvanceNumber','READ-FIXTURE-'||@currency,'IdempotencyKey',@key,
                    'PaymentReference','READ PROJECTION FIXTURE',
                    'EvidenceObjectKey','fixture/not-provider-evidence'))).*
                FROM currency_read_fixture.vendor_advances a LIMIT 1;
                """, connection, transaction);
            seed.Parameters.AddWithValue("id", Guid.NewGuid());
            seed.Parameters.AddWithValue("company", company);
            seed.Parameters.AddWithValue("vendor", vendor);
            seed.Parameters.AddWithValue("po", po);
            seed.Parameters.AddWithValue("currency", currency);
            seed.Parameters.AddWithValue("amount", amount);
            seed.Parameters.AddWithValue("key", Guid.NewGuid().ToString("N"));
            Assert.Equal(1, await seed.ExecuteNonQueryAsync());
        }
        await using var read = new NpgsqlCommand(
            "SELECT currency_read_fixture.list_vendor_positions(@company)::text", connection, transaction);
        read.Parameters.AddWithValue("company", company);
        var result = Assert.IsType<string>(await read.ExecuteScalarAsync());
        await transaction.RollbackAsync(); // Remove all cloned tables/functions and synthetic read evidence.
        return result;
    }
}
