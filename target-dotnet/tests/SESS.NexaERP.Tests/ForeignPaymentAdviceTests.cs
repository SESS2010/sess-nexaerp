using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
#if WORKFLOW_WITNESS
    [Fact]
    public async Task ForeignPaymentWithoutBankAdviceIsRefusedBeforePosting()
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
            const string path = "/api/v1/accounts/vendor-financial-evidence/payments";
            var invalid = context.OriginalCommand with
            {
                CurrencyCode = " usd ", EvidenceObjectKey = "   ",
                IdempotencyKey = "foreign-advice-missing-http"
            };
            var before = await ImportAdviceLedgerCounts(context.Options);
            var missing = await TimedRacePost(host.Client, path, invalid);
            var mismatch = await TimedRacePost(host.Client, path, invalid with
            {
                EvidenceObjectKey = "evidence/bank-advice-test",
                IdempotencyKey = "foreign-currency-mismatch-http"
            });
            string? databaseState = null, databaseMessage = null;
            await using (var db = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
                .UseNpgsql(context.RuntimeConnection).Options))
            {
                await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                var request = invalid with { IdempotencyKey = "foreign-advice-missing-sql" };
                _ = actor.RequireRole("approve", "ACCOUNTS_MANAGER");
                var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(
                    actor.OrganizationId!, "VendorPayment.Record", request.IdempotencyKey, request);
                await Rev869BCommandContextAuthorizer.OpenForDatabaseFunctionAsync(
                    db, actor, actor.OrganizationId!, envelope, "vendor_payments", "VendorPayment",
                    request.VendorId, "RECORDED", 0, null, "RECORDED",
                    envelope.RequestFingerprint, "Bank advice refusal witness", default);
                await using var command = new NpgsqlCommand("""
                    SELECT * FROM advance.record_vendor_payment(
                      @company,@vendor,@date,@amount,@currency,@reference,@evidence,@lines,
                      @key,@hash,@actor,@role,@assignment,@type,@login)
                    """, (NpgsqlConnection)db.Database.GetDbConnection(),
                    (NpgsqlTransaction)transaction.GetDbTransaction());
                command.Parameters.AddWithValue("company", company);
                command.Parameters.AddWithValue("vendor", request.VendorId);
                command.Parameters.AddWithValue("date", request.PaidDate);
                command.Parameters.AddWithValue("amount", request.Amount);
                command.Parameters.AddWithValue("currency", request.CurrencyCode);
                command.Parameters.AddWithValue("reference", request.PaymentReference);
                command.Parameters.Add("evidence", NpgsqlDbType.Text).Value = DBNull.Value;
                command.Parameters.AddWithValue("lines", NpgsqlDbType.Jsonb,
                    JsonSerializer.Serialize(request.Allocations, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
                command.Parameters.AddWithValue("key", request.IdempotencyKey);
                command.Parameters.AddWithValue("hash", envelope.RequestFingerprint);
                command.Parameters.AddWithValue("actor", context.ApproverId);
                command.Parameters.AddWithValue("role", actor.RoleCode);
                command.Parameters.AddWithValue("assignment", actor.ResolvedRoleAssignmentId!.Value);
                command.Parameters.AddWithValue("type", actor.ResolvedRoleAssignmentType!);
                command.Parameters.AddWithValue("login", actor.LoginId);
                try { await command.ExecuteScalarAsync(); }
                catch (PostgresException error)
                { databaseState = error.SqlState; databaseMessage = error.MessageText; }
                await transaction.RollbackAsync();
            }
            var after = await ImportAdviceLedgerCounts(context.Options);
            var evidence = Path.Combine(FindRepositoryRoot(), "local-evidence", "item11");
            Directory.CreateDirectory(evidence);
            await File.WriteAllTextAsync(Path.Combine(evidence, "bank-advice-refusal.json"),
                JsonSerializer.Serialize(new { Before = before, Missing = missing, Mismatch = mismatch,
                    DatabaseState = databaseState, DatabaseMessage = databaseMessage, After = after },
                    new JsonSerializerOptions { WriteIndented = true }));
            Assert.Equal(before, after);
            Assert.Equal(HttpStatusCode.BadRequest, missing.Status);
            Assert.Contains("bank advice", missing.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(HttpStatusCode.Conflict, mismatch.Status);
            Assert.Contains("currency must match", mismatch.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("P0001", databaseState);
            Assert.Contains("bank advice", databaseMessage!, StringComparison.OrdinalIgnoreCase);
            // Settle the original domestic bills exactly as this callback's caller expects.
            // The foreign refusal fixture does not manufacture a foreign PO or foreign FIFO cost.
            var payables = await Get<VendorPayableView[]>(host.Client, path.Replace("/payments", "/payables")
                + "?vendorId=" + context.OriginalCommand.VendorId);
            var ids = context.OriginalCommand.Allocations.Select(x => x.VendorBillId).ToArray();
            var allocations = payables.Where(x => ids.Contains(x.VendorBillId))
                .Select(x => new VendorPaymentAllocationInput(x.VendorBillId, x.OutstandingValue)).ToArray();
            var domestic = context.OriginalCommand with
            { Allocations = allocations, Amount = allocations.Sum(x => x.Amount), EvidenceObjectKey = "" };
            var payment = await Post<VendorPaymentView>(host.Client, path, domestic);
            return new PaymentRaceResult(payment, domestic);
        });
        Assert.True(observed);
    }

#endif
    private static Task<string> ImportAdviceLedgerCounts(DbContextOptions<NexaErpDbContext> options) =>
        Query(options, db => db.Database.SqlQueryRaw<string>("""
            SELECT jsonb_build_object(
              'payments',(SELECT count(*) FROM advance.vendor_payments),
              'allocations',(SELECT count(*) FROM advance.vendor_payment_allocations),
              'requests',(SELECT count(*) FROM advance.command_requests),
              'receipts',(SELECT count(*) FROM advance.command_receipts),
              'audits',(SELECT count(*) FROM advance.audit_logs))::text AS "Value"
            """).SingleAsync());
}
