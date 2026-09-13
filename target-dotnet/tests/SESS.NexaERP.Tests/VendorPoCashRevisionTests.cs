using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task PurchaseOrderRevisionCannotResetCashOrIssueBelowLegacyPayments()
    {
        var observed = false;
        await RunCompletePurchaseFlow(mixedRun: async context =>
        {
            observed = true;
            var actor = await BankAdviceActor(context.Options);
            var accounts = actor.EmployeeId!.Value;
            var company = Guid.Parse("70000000-0000-0000-0000-000000000001");
            var prior = await Query(context.Options, db => db.PurchaseOrders.AsNoTracking().Include(x => x.Lines)
                .Where(x => x.CompanyId == company && x.IsCurrentVersion && x.Status == "Issued" &&
                    x.TotalPayableValue == 5900m).SingleAsync());
            async Task UseActor(Guid employee, string role)
            {
                var subject = await Query(context.Options, db => db.EmployeeIdentityMappings
                    .Where(x => x.CompanyId == company && x.EmployeeId == employee && x.IsActive)
                    .Select(x => x.Subject).SingleAsync());
                actor.Set(employee, subject, role);
            }
            await using var host = await PurchaseFlowHost.StartAsync(context.RuntimeConnection, actor, true, true);
            const string financial = "/api/v1/accounts/vendor-financial-evidence/";
            var payables = await Get<VendorPayableView[]>(host.Client, financial + "payables");
            var payable = Assert.Single(payables, x => x.PurchaseOrderId == prior.Id);
            await Post<VendorPaymentView>(host.Client, financial + "payments",
                new RecordVendorPaymentRequest(prior.VendorId, DateOnly.FromDateTime(DateTime.UtcNow),
                    payable.OutstandingValue, "INR", "CASH-REVISION-SETTLE", "evidence/cash-revision-settle",
                    [new(payable.VendorBillId, payable.OutstandingValue)], "cash-revision-settle"));

            // Exercise a real upgrade from the witnessed predecessor defect.
            // No financial row is inserted, updated or deleted directly by this test.
            await using var evidenceDb = new NexaErpDbContext(context.Options);
            await using var source = new NpgsqlConnection(evidenceDb.Database.GetConnectionString());
            await source.OpenAsync();
            using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
                .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
            var migrator = model.GetService<IMigrator>();
            const string previous = "20260913090000_GovernedVendorBankAdvice";
            const string current = "20260913100000_VendorPoCashCap";
            await using (var down = new NpgsqlCommand(migrator.GenerateScript(current, previous), source))
                await down.ExecuteNonQueryAsync();
            var legacy = await Post<VendorAdvanceView>(host.Client, financial + "advances",
                new RecordVendorAdvanceRequest(prior.Id, DateOnly.FromDateTime(DateTime.UtcNow), 1m, "INR",
                    "LEGACY-OVER-CASH", "evidence/legacy-over-cash", "cash-revision-legacy"));
            await using (var up = new NpgsqlCommand(migrator.GenerateScript(previous, current), source))
                await up.ExecuteNonQueryAsync();

            var approvalScopeSetups = new List<object>();
            var approvalSteps = new List<object>();
            async Task EnsureApprovalScope(Guid employee, string role)
            {
                var beforeScope = await Query(context.Options, setup =>
                    new SESS.NexaERP.Infrastructure.Authorization.EfRecordScopeAuthorizer(setup).AuthorizeAsync(
                        employee, role, new SESS.NexaERP.Application.Authorization.RecordScopeTarget(
                            "SESS_PVT_LTD", prior.RequestingDepartmentId, prior.DeliveryWarehouseId, null, prior.OwnerEmployeeId),
                        DateOnly.FromDateTime(DateTime.UtcNow), default));
                if (beforeScope.Allowed) return;
                // Explicit disposable authority fixture, never a financial-row edit or scope bypass.
                // Grant only the department/warehouse needed by the actual configured approver.
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var setupResult = await Query(context.Options, async setup =>
                {
                    var membership = await setup.EmployeeCompanyAssignments.SingleAsync(x => x.CompanyId == company &&
                        x.EmployeeId == employee && x.IsActive && x.Status == "ACTIVE" && x.EffectiveFrom <= today &&
                        (!x.EffectiveTo.HasValue || x.EffectiveTo >= today));
                    var assignment = await setup.EmployeeDepartmentAssignments.SingleOrDefaultAsync(x => x.CompanyId == company &&
                        x.EmployeeCompanyAssignmentId == membership.Id && x.DepartmentId == prior.RequestingDepartmentId &&
                        x.IsActive && x.EffectiveFrom <= today && (!x.EffectiveTo.HasValue || x.EffectiveTo >= today));
                    var introduced = assignment is null;
                    if (assignment is null)
                    {
                        assignment = new SESS.NexaERP.Domain.Foundation.EmployeeDepartmentAssignment
                        {
                            CompanyId = company, EmployeeCompanyAssignmentId = membership.Id,
                            DepartmentId = prior.RequestingDepartmentId ?? throw new InvalidOperationException("Witness PO requires a requesting department."),
                            DesignationId = await setup.Employees.Where(x => x.Id == employee).Select(x => x.DesignationId).SingleAsync(),
                            AssignmentType = "SECONDARY", IsPrimary = false, EffectiveFrom = today,
                            Status = "ACTIVE", IsActive = true, CreatedBy = "CASH_REVISION_REFERENCE_FIXTURE"
                        };
                        setup.EmployeeDepartmentAssignments.Add(assignment);
                        await setup.SaveChangesAsync();
                    }
                    return new { DepartmentAssignmentId = assignment.Id, IntroducedDepartmentFixture = introduced,
                        DepartmentCode = await setup.Departments.Where(x => x.Id == prior.RequestingDepartmentId).Select(x => x.Code).SingleAsync(),
                        WarehouseCode = await setup.Warehouses.Where(x => x.Id == prior.DeliveryWarehouseId).Select(x => x.WarehouseCode).SingleAsync(),
                        EmployeeCode = await setup.Employees.Where(x => x.Id == employee).Select(x => x.EmployeeCode).SingleAsync(),
                        AdministratorId = await setup.Employees.Where(x => x.EmployeeCode == "SESS-12").Select(x => x.Id).SingleAsync() };
                });
                await UseActor(setupResult.AdministratorId, "IT_MANAGER");
                var scopeGrant = await Post<JsonElement>(host.Client, "/api/v1/rev869a/configuration/operational-scopes",
                    new SESS.NexaERP.Application.Rev869A.CreateOperationalScopeRequest("SESS_PVT_LTD",
                        setupResult.EmployeeCode, setupResult.DepartmentCode, setupResult.WarehouseCode,
                        null, false, false, today, null, "Disposable cash revision: mapped IT approver"));
                approvalScopeSetups.Add(new { EmployeeId = employee, Role = role, BeforeScope = beforeScope,
                    ReferenceSetup = setupResult, GovernedScopeGrant = scopeGrant });
                var evidence = Path.Combine(FindRepositoryRoot(), "local-evidence", "item11");
                Directory.CreateDirectory(evidence);
                await File.WriteAllTextAsync(Path.Combine(evidence, "po-cash-revision-approval-scopes.json"),
                    JsonSerializer.Serialize(approvalScopeSetups, new JsonSerializerOptions { WriteIndented = true }));
            }

            await UseActor(prior.OwnerEmployeeId, "PURCHASE_MANAGER");
            var poPath = "/api/v1/purchase/purchase-orders/" + Uri.EscapeDataString(prior.PoNumber);
            var revision = await Post<Rev869BDocumentResult>(host.Client, poPath + "/amend",
                new Rev869BAmendPurchaseOrderRequest("Terms-only cash continuity witness",
                    prior.PaymentTermsSnapshot, prior.DeliveryTermsSnapshot, prior.WarrantyTermsSnapshot,
                    prior.Version, "cash-revision-amend"));
            revision = await Post<Rev869BDocumentResult>(host.Client, poPath + "/submit",
                new Rev869BSubmitPurchaseOrderRequest("Submit terms-only revision", revision.Version, "cash-revision-submit"));
            var snapshot = await Query(context.Options, db => db.PurchaseOrders.Where(x => x.Id == revision.Id)
                .Select(x => x.ApprovalWorkflowSnapshotJson).SingleAsync());
            using (var workflow = JsonDocument.Parse(snapshot))
            {
                foreach (var step in workflow.RootElement.GetProperty("steps").EnumerateArray()
                    .OrderBy(x => x.GetProperty("stepNumber").GetInt32()))
                {
                    await EnsureApprovalScope(step.GetProperty("employeeId").GetGuid(), step.GetProperty("roleCode").GetString()!);
                    await UseActor(step.GetProperty("employeeId").GetGuid(), step.GetProperty("roleCode").GetString()!);
                    var priorVersion = await Query(context.Options, db => db.PurchaseOrders.Where(x => x.Id == prior.Id)
                        .Select(x => x.Version).SingleAsync());
                    var approvalResponse = await TimedRacePost(host.Client, poPath + "/approve",
                        new Rev869BPoApprovalActionRequest("Approve terms-only revision", revision.Version,
                            priorVersion, "cash-revision-approve-" + step.GetProperty("stepNumber").GetInt32()));
                    approvalSteps.Add(new { Step = step.Clone(), Response = approvalResponse });
                    var approvalEvidence = Path.Combine(FindRepositoryRoot(), "local-evidence", "item11");
                    Directory.CreateDirectory(approvalEvidence);
                    await File.WriteAllTextAsync(Path.Combine(approvalEvidence, "po-cash-revision-approvals.json"),
                        JsonSerializer.Serialize(approvalSteps, new JsonSerializerOptions { WriteIndented = true }));
                    Assert.True(approvalResponse.Status == HttpStatusCode.OK,
                        step.GetRawText() + " returned " + approvalResponse.Body);
                    revision = JsonSerializer.Deserialize<Rev869BDocumentResult>(approvalResponse.Body)!;
                }
            }
            Assert.Equal("Approved", revision.Status);
            await UseActor(prior.OwnerEmployeeId, "PURCHASE_MANAGER");
            async Task<JsonElement> Snapshot()
            {
                await using var command = new NpgsqlCommand("""
                    SELECT jsonb_build_object('Status',p."Status",'Version',p."Version",
                      'RootPurchaseOrderId',p."RootPurchaseOrderId",'Value',p."TotalPayableValue",
                      'Handoffs',(SELECT count(*) FROM advance.material_followup_handoffs h WHERE h."PurchaseOrderId"=p."Id"),
                      'History',(SELECT count(*) FROM advance.purchase_order_history h WHERE h."PurchaseOrderId"=p."Id"),
                      'Audits',(SELECT count(*) FROM advance.audit_logs),
                      'BusinessAudits',(SELECT count(*) FROM advance.audit_logs WHERE "Result"='Success'),
                      'DeniedAudits',(SELECT count(*) FROM advance.audit_logs WHERE "Result"='Failure' AND "Action"='Denied'),
                      'Requests',(SELECT count(*) FROM advance.command_requests),
                      'Receipts',(SELECT count(*) FROM advance.command_receipts),
                      'CashPaid',cash."AdvanceAmount"+cash."BillPaymentAmount")::text
                    FROM advance.purchase_orders p
                    CROSS JOIN LATERAL advance.vendor_po_cash_totals(
                      p."CompanyId",p."RootPurchaseOrderId",p."CurrencyCode",p."VendorId") cash
                    WHERE p."Id"=@id
                    """, source);
                command.Parameters.AddWithValue("id", revision.Id);
                using var result = JsonDocument.Parse((string)(await command.ExecuteScalarAsync())!);
                return result.RootElement.Clone();
            }
            var before = await Snapshot();
            Assert.Equal(5901m, before.GetProperty("CashPaid").GetDecimal());
            var issue = new Rev869BIssuePurchaseOrderRequest("Issue after cash reconciliation",
                revision.Version, "cash-revision-issue");
            var refused = await TimedRacePost(host.Client, poPath + "/issue", issue);
            var afterRefusal = await Snapshot();
            var refusalEvidence = Path.Combine(FindRepositoryRoot(), "local-evidence", "item11");
            Directory.CreateDirectory(refusalEvidence);
            await File.WriteAllTextAsync(Path.Combine(refusalEvidence, "po-cash-revision-refusal.json"),
                JsonSerializer.Serialize(new { Before = before, Refused = refused, AfterRefusal = afterRefusal },
                    new JsonSerializerOptions { WriteIndented = true }));
            Assert.True(refused.Status == HttpStatusCode.Conflict, refused.Body);
            Assert.Contains("below already paid vendor cash", refused.Body, StringComparison.OrdinalIgnoreCase);
            foreach (var property in before.EnumerateObject())
            {
                if (property.Name is "Audits" or "DeniedAudits")
                    Assert.Equal(property.Value.GetInt64() + 1, afterRefusal.GetProperty(property.Name).GetInt64());
                else
                    Assert.Equal(property.Value.GetRawText(), afterRefusal.GetProperty(property.Name).GetRawText());
            }

            await UseActor(accounts, "ACCOUNTS_MANAGER");
            await Post<VendorAdvanceView>(host.Client, financial + "advances/" + legacy.Id + "/reverse",
                new ReverseVendorAdvanceRequest("Returned duplicate legacy advance in the disposable witness",
                    "cash-revision-return"));
            var beforeRetry = await Snapshot();
            Assert.Equal(5900m, beforeRetry.GetProperty("CashPaid").GetDecimal());
            await UseActor(prior.OwnerEmployeeId, "PURCHASE_MANAGER");
            var issued = await Post<Rev869BDocumentResult>(host.Client, poPath + "/issue", issue);
            Assert.Equal("Issued", issued.Status);
            var afterIssue = await Snapshot();
            foreach (var key in new[] { "Version", "History", "Audits", "Requests", "Receipts" })
                Assert.Equal(beforeRetry.GetProperty(key).GetInt64() + 1, afterIssue.GetProperty(key).GetInt64());
            Assert.Equal(prior.Lines.Count, afterIssue.GetProperty("Handoffs").GetInt64());
            Assert.Equal(prior.RootPurchaseOrderId, afterIssue.GetProperty("RootPurchaseOrderId").GetGuid());
            var replay = await Post<Rev869BDocumentResult>(host.Client, poPath + "/issue", issue);
            Assert.Equal(issued, replay);
            Assert.Equal(afterIssue.GetRawText(), (await Snapshot()).GetRawText());

            await UseActor(accounts, "ACCOUNTS_MANAGER");
            var eligible = await Get<VendorAdvancePurchaseOrderOption[]>(host.Client, financial + "advance-purchase-orders");
            Assert.DoesNotContain(eligible, x => x.PurchaseOrderId == revision.Id);
            var anotherAdvance = await TimedRacePost(host.Client, financial + "advances",
                new RecordVendorAdvanceRequest(revision.Id, DateOnly.FromDateTime(DateTime.UtcNow), 1m, "INR",
                    "NEW-REVISION-OVER-CASH", "evidence/new-revision-over-cash", "cash-new-revision-refused"));
            Assert.Equal(HttpStatusCode.Conflict, anotherAdvance.Status);
            Assert.Equal(afterIssue.GetRawText(), (await Snapshot()).GetRawText());
            var evidence = Path.Combine(FindRepositoryRoot(), "local-evidence", "item11");
            Directory.CreateDirectory(evidence);
            await File.WriteAllTextAsync(Path.Combine(evidence, "po-cash-revision.json"),
                JsonSerializer.Serialize(new { Prior = prior.Id, Revision = revision.Id, LegacyAdvance = legacy,
                    Before = before, Refused = refused, AfterRefusal = afterRefusal, BeforeRetry = beforeRetry,
                    Issued = issued, AfterIssue = afterIssue, Replay = replay, AnotherAdvance = anotherAdvance,
                    HistoryPreserved = true, ApprovalScopeSetups = approvalScopeSetups, ApprovalSteps = approvalSteps }, new JsonSerializerOptions { WriteIndented = true }));
        });
        Assert.True(observed);
    }
}
