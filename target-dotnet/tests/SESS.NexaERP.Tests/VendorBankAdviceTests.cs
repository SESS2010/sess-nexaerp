using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
#if WORKFLOW_WITNESS
    [Fact]
    public async Task BankAdviceUploadIsImmutableScopedReplayableAndAtomicWithPaymentLink()
    {
        var observed = false;
        await RunCompletePurchaseFlow(paymentRace: async context =>
        {
            observed = true;
            var actor = await BankAdviceActor(context.Options);
            await using var host = await PurchaseFlowHost.StartAsync(context.RuntimeConnection, actor, true, true);
            var content = BankAdvicePdfFixture();
            var expectedHash = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
            var first = await SendBankAdvice(host.Client, context.OriginalCommand.VendorId, "bank-advice-first", content);
            Assert.Equal(HttpStatusCode.Created, first.Status);
            var document = JsonSerializer.Deserialize<VendorBankAdviceView>(first.Body)!;
            Assert.False(document.Replayed);
            Assert.Equal(expectedHash, document.ContentSha256);
            Assert.Equal(content.Length, document.SizeBytes);
            Assert.Equal("bank-advice:" + document.Id.ToString("D"), document.EvidenceObjectKey);
            var replay = await SendBankAdvice(host.Client, context.OriginalCommand.VendorId, "bank-advice-first", content);
            Assert.Equal(HttpStatusCode.Created, replay.Status);
            var replayed = JsonSerializer.Deserialize<VendorBankAdviceView>(replay.Body)!;
            Assert.True(replayed.Replayed);
            Assert.Equal(document.Id, replayed.Id);
            var changed = await SendBankAdvice(host.Client, context.OriginalCommand.VendorId,
                "bank-advice-first", content.Concat(new byte[] { 10 }).ToArray());
            Assert.Equal(HttpStatusCode.Conflict, changed.Status);
            var invalid = await SendBankAdvice(host.Client, context.OriginalCommand.VendorId,
                "bank-advice-invalid", Encoding.ASCII.GetBytes("not a PDF"));
            Assert.Equal(HttpStatusCode.BadRequest, invalid.Status);
            var missingHeader = await SendBankAdvice(host.Client, context.OriginalCommand.VendorId, null, content);
            Assert.Equal(HttpStatusCode.BadRequest, missingHeader.Status);
            var beforeFailure = await BankAdviceCounts(context.Options);
            Assert.Equal(new BankAdviceLedgerCounts(1, 1, 1, 1), beforeFailure);
            await using var evidenceDb = new NexaErpDbContext(context.Options);
            await using var owner = new NpgsqlConnection(evidenceDb.Database.GetConnectionString());
            await owner.OpenAsync();
            await using (var inject = new NpgsqlCommand("""
                CREATE FUNCTION advance.bank_advice_audit_failure() RETURNS trigger
                LANGUAGE plpgsql AS $f$ BEGIN
                  IF NEW."Action"='VendorBankAdvice.Upload' THEN
                    RAISE EXCEPTION 'BANK_ADVICE_AUDIT_FAILURE_WITNESS';
                  END IF;
                  RETURN NEW;
                END $f$;
                CREATE TRIGGER bank_advice_audit_failure BEFORE INSERT ON advance.audit_logs
                  FOR EACH ROW EXECUTE FUNCTION advance.bank_advice_audit_failure();
                """, owner)) await inject.ExecuteNonQueryAsync();
            RaceHttpResult failed;
            try
            {
                failed = await SendBankAdvice(host.Client, context.OriginalCommand.VendorId, "bank-advice-retry", content);
            }
            finally
            {
                await using var remove = new NpgsqlCommand("""
                    DROP TRIGGER bank_advice_audit_failure ON advance.audit_logs;
                    DROP FUNCTION advance.bank_advice_audit_failure();
                    """, owner);
                await remove.ExecuteNonQueryAsync();
            }
            Assert.Equal(HttpStatusCode.InternalServerError, failed.Status);
            Assert.Contains("BANK_ADVICE_AUDIT_FAILURE_WITNESS", context.ReadPostgresLog(), StringComparison.Ordinal);
            var afterFailure = await BankAdviceCounts(context.Options);
            Assert.Equal(beforeFailure, afterFailure);
            var retry = await SendBankAdvice(host.Client, context.OriginalCommand.VendorId, "bank-advice-retry", content);
            Assert.Equal(HttpStatusCode.Created, retry.Status);
            var concurrent = await Task.WhenAll(
                SendBankAdvice(host.Client, context.OriginalCommand.VendorId, "bank-advice-concurrent", content),
                SendBankAdvice(host.Client, context.OriginalCommand.VendorId, "bank-advice-concurrent", content));
            Assert.Contains(concurrent, x => x.Status == HttpStatusCode.Created);
            Assert.All(concurrent, x => Assert.True(x.Status is HttpStatusCode.Created or HttpStatusCode.Conflict, x.Body));
            var concurrentReplay = await SendBankAdvice(host.Client, context.OriginalCommand.VendorId, "bank-advice-concurrent", content);
            Assert.Equal(HttpStatusCode.Created, concurrentReplay.Status);
            Assert.True(JsonSerializer.Deserialize<VendorBankAdviceView>(concurrentReplay.Body)!.Replayed);
            var finalCounts = await BankAdviceCounts(context.Options);
            Assert.Equal(new BankAdviceLedgerCounts(3, 3, 3, 3), finalCounts);
            const string root = "/api/v1/accounts/vendor-financial-evidence/";
            var metadata = await Get<VendorBankAdviceView>(host.Client, root + "bank-advices/" + document.Id);
            Assert.Equal(expectedHash, metadata.ContentSha256);
            var byKey = await Get<VendorBankAdviceView>(host.Client, root + "bank-advices/by-key?evidenceObjectKey=" + Uri.EscapeDataString(document.EvidenceObjectKey));
            Assert.Equal(document.Id, byKey.Id);
            var download = await host.Client.GetAsync(root + "bank-advices/" + document.Id + "/content");
            Assert.Equal(HttpStatusCode.OK, download.StatusCode);
            Assert.Equal("application/pdf", download.Content.Headers.ContentType!.MediaType);
            Assert.Equal(content, await download.Content.ReadAsByteArrayAsync());
            var absent = await host.Client.GetAsync(root + "bank-advices/" + Guid.NewGuid());
            Assert.Equal(HttpStatusCode.NotFound, absent.StatusCode);
            await using (var scoped = new NpgsqlCommand(
                "SELECT advance.vendor_bank_advice_json(@company,@id,false)", owner))
            {
                scoped.Parameters.AddWithValue("company", Guid.NewGuid());
                scoped.Parameters.AddWithValue("id", document.Id);
                Assert.IsType<DBNull>(await scoped.ExecuteScalarAsync());
            }
            foreach (var (company, vendor) in new[]
            {
                (Guid.Parse("70000000-0000-0000-0000-000000000001"), Guid.NewGuid()),
                (Guid.NewGuid(), context.OriginalCommand.VendorId)
            })
            {
                await using var mismatch = new NpgsqlCommand(
                    "SELECT advance.require_vendor_bank_advice(@company,@vendor,@key)", owner);
                mismatch.Parameters.AddWithValue("company", company);
                mismatch.Parameters.AddWithValue("vendor", vendor);
                mismatch.Parameters.AddWithValue("key", document.EvidenceObjectKey);
                var error = await Assert.ThrowsAsync<PostgresException>(() => mismatch.ExecuteScalarAsync());
                Assert.Equal("P0001", error.SqlState);
            }
            var missingAdvicePayment = await TimedRacePost(host.Client, root + "payments", context.OriginalCommand with
            {
                EvidenceObjectKey = "bank-advice:" + Guid.NewGuid(),
                IdempotencyKey = "bank-advice-missing-payment"
            });
            Assert.Equal(HttpStatusCode.Conflict, missingAdvicePayment.Status);
            Assert.Contains("retained bank advice", missingAdvicePayment.Body, StringComparison.OrdinalIgnoreCase);
            using (var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
                .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options))
            {
                var down = model.GetService<IMigrator>().GenerateScript(
                    model.Database.GetMigrations().Last(), "20260913080000_ForeignCurrencyFinancialReadModels", MigrationsSqlGenerationOptions.NoTransactions);
                await using var guardConnection = new NpgsqlConnection(owner.ConnectionString);
                await guardConnection.OpenAsync();
                await using var rollback = await guardConnection.BeginTransactionAsync();
                await using var guard = new NpgsqlCommand(down, guardConnection, rollback);
                var error = await Assert.ThrowsAsync<PostgresException>(() => guard.ExecuteNonQueryAsync());
                Assert.Equal("P0001", error.SqlState);
                Assert.Contains("refuses retained financial documents", error.MessageText, StringComparison.OrdinalIgnoreCase);
                await rollback.RollbackAsync();
            }
            Assert.Equal(finalCounts, await BankAdviceCounts(context.Options));
            Assert.DoesNotContain("40P01", context.ReadPostgresLog(), StringComparison.OrdinalIgnoreCase);
            var evidence = Path.Combine(FindRepositoryRoot(), "local-evidence", "item11");
            Directory.CreateDirectory(evidence);
            await File.WriteAllTextAsync(Path.Combine(evidence, "bank-advice-storage.json"),
                JsonSerializer.Serialize(new
                {
                    Document = document, Replay = replayed, Changed = changed, Invalid = invalid,
                    MissingHeader = missingHeader, Failed = failed, Retry = retry,
                    Concurrent = concurrent, ConcurrentReplay = concurrentReplay,
                    BeforeFailure = beforeFailure, AfterFailure = afterFailure, FinalCounts = finalCounts,
                    DownloadBytesMatch = true, WrongCompanyHidden = true,
                    WrongVendorAndCompanyLinkRefused = true, MissingAdvicePayment = missingAdvicePayment,
                    PopulatedRollbackRefused = true
                }, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true }));
            return await RunPaymentRace(context with
            { OriginalCommand = context.OriginalCommand with { EvidenceObjectKey = document.EvidenceObjectKey } });
        });
        Assert.True(observed);
    }

#endif
    private static async Task<TaxWorkflowUser> BankAdviceActor(DbContextOptions<NexaErpDbContext> options)
    {
        var company = Guid.Parse("70000000-0000-0000-0000-000000000001");
        var rows = await Query(options, db => db.EmployeeRoleAssignments.AsNoTracking().Include(x => x.Role)
            .Where(x => x.CompanyId == company && x.EffectiveTo == null).ToListAsync());
        var employee = rows.Where(x => x.Role!.Code == "ACCOUNTS_MANAGER" && x.AssignmentType == "FULL")
            .Select(x => x.EmployeeId).Distinct().Single();
        var assignments = rows.ToDictionary(x => TaxWorkflowUser.AssignmentKey(x.EmployeeId, x.Role!.Code),
            x => new EffectiveRoleAssignment(x.Id, x.Role!.Code, x.AssignmentType));
        var subject = await Query(options, db => db.EmployeeIdentityMappings
            .Where(x => x.CompanyId == company && x.EmployeeId == employee && x.IsActive)
            .Select(x => x.Subject).SingleAsync());
        return new TaxWorkflowUser(employee, subject, "ACCOUNTS_MANAGER", assignments);
    }

    private static async Task<RaceHttpResult> SendBankAdvice(HttpClient client, Guid vendor,
        string? key, byte[] content)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(vendor.ToString()), "vendorId");
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(file, "file", "bank-advice.pdf");
        using var request = new HttpRequestMessage(HttpMethod.Post,
            "/api/v1/accounts/vendor-financial-evidence/bank-advices") { Content = form };
        if (key is not null) request.Headers.Add("Idempotency-Key", key);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        using var response = await client.SendAsync(request);
        return new(response.StatusCode, watch.Elapsed.TotalSeconds, await response.Content.ReadAsStringAsync());
    }

    private sealed record BankAdviceLedgerCounts(long Documents, long Audits, long Requests, long Receipts);

    private static async Task<BankAdviceLedgerCounts> BankAdviceCounts(
        DbContextOptions<NexaErpDbContext> options)
    {
        await using var db = new NexaErpDbContext(options);
        await using var connection = new NpgsqlConnection(db.Database.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT (SELECT count(*) FROM advance.vendor_bank_advices),
              (SELECT count(*) FROM advance.audit_logs WHERE "Action"='VendorBankAdvice.Upload'),
              (SELECT count(*) FROM advance.command_requests WHERE "Operation"='VendorBankAdvice.Upload'),
              (SELECT count(*) FROM advance.command_receipts c JOIN advance.command_requests r
                ON r."CommandId"=c."CommandId" WHERE r."Operation"='VendorBankAdvice.Upload')
            """, connection);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new(reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2), reader.GetInt64(3));
    }

    private static async Task<VendorBankAdviceView> UploadBankAdviceForBackup(
        DbContextOptions<NexaErpDbContext> options, string runtime)
    {
        var actor = await BankAdviceActor(options);
        var vendor = await Query(options, db => db.VendorBills.Where(x => x.Status == "ACCEPTED")
            .Select(x => x.VendorId).FirstAsync());
        await using var host = await PurchaseFlowHost.StartAsync(runtime, actor, true, true);
        var response = await SendBankAdvice(host.Client, vendor, "bank-advice-backup", BankAdvicePdfFixture());
        Assert.True(response.Status == HttpStatusCode.Created, response.Body);
        return JsonSerializer.Deserialize<VendorBankAdviceView>(response.Body)!;
    }

    private static byte[] BankAdvicePdfFixture()
    {
        var pdf = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int>();
        foreach (var body in new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 100 100] /Resources << >> /Contents 4 0 R >>",
            "<< /Length 0 >>\nstream\nendstream"
        })
        {
            offsets.Add(pdf.Length);
            pdf.Append(offsets.Count).Append(" 0 obj\n").Append(body).Append("\nendobj\n");
        }
        var xref = pdf.Length;
        pdf.Append("xref\n0 5\n0000000000 65535 f \n");
        foreach (var offset in offsets)
            pdf.Append(offset.ToString("D10", System.Globalization.CultureInfo.InvariantCulture)).Append(" 00000 n \n");
        pdf.Append("trailer\n<< /Size 5 /Root 1 0 R >>\nstartxref\n").Append(xref).Append("\n%%EOF\n");
        return Encoding.ASCII.GetBytes(pdf.ToString());
    }
}
