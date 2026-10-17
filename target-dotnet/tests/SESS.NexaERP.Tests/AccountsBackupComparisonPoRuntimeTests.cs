using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Employees;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task NamedAccountsBackupReadsAndDecidesComparisonAndPoButCannotCreateOrIssue()
    {
        var proved = false;
        await Assert.ThrowsAsync<AccountsPrWitnessComplete>(() => RunCompletePurchaseFlow(
            scopeAlignmentWitness: true,
            workload: async context =>
            {
                if (context.Stage != "COMPARISON_APPROVAL" || context.Band != "LOW") return;
                await using var admin = new NexaErpDbContext(context.Options);
                var comparison = await admin.CommercialComparisons.AsNoTracking().SingleAsync(c => c.Id == context.DocumentId);
                var rfq = await admin.RequestForQuotations.AsNoTracking().SingleAsync(r => r.Id == comparison.RequestForQuotationId);
                var (user, karthick, manager) = await PrepareNamedAccountsRuntimeUser(admin, comparison.CompanyId,
                    comparison.OrganizationId, rfq.DeliveryWarehouseId);
                var backupSubject = user.LoginId;
                var backupIssuer = user.IdentityIssuer!;
                var backupDepartment = user.DepartmentId!.Value;
                await using var host = await PurchaseFlowHost.StartAsync(context.RuntimeConnection, user,
                    useRealPagePermissions: true, useRealOperationalScopes: true);
                var client = host.Client;
                var tax = await Post<SESS.NexaERP.Application.Rev869A.TaxGstWorkflowResult>(client,
                    "/api/v1/rev869a/configuration/tax-gst", Request("99999741") with { EffectiveFrom = admin.BusinessDate }, "backup-tax-create");
                Assert.Equal(karthick, tax.CreatorEmployeeId);
                Assert.Null(tax.DecisionEmployeeId);
                Assert.Equal(SESS.NexaERP.Domain.Masters.MasterApprovalStatuses.PendingApproval, tax.ApprovalStatus);
                using (var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/rev869a/configuration/tax-gst/{tax.Id}/approve")) {
                    request.Headers.Add("Idempotency-Key", "backup-tax-no-self-approval");
                    request.Content = JsonContent.Create(new SESS.NexaERP.Application.Rev869A.DecideTaxGstSettingRequest(
                        tax.Version, "Creator cannot self-approve", "backup-tax-no-self-approval"));
                    using var refusal = await client.SendAsync(request);
                    Assert.Equal(HttpStatusCode.Forbidden, refusal.StatusCode);
                }
                Assert.Equal(tax.ApprovalStatus, await admin.TaxGstSettings.Where(t => t.Id == tax.Id).Select(t => t.ApprovalStatus).SingleAsync());
                var detail = await Get<JsonElement>(client, $"/api/v1/purchase/comparisons/{comparison.ComparisonNumber}");
                Assert.Equal(comparison.Id, detail.GetProperty("Id").GetGuid());
                using (var stale = await client.PostAsJsonAsync($"/api/v1/purchase/comparisons/{comparison.ComparisonNumber}/approve",
                    new Rev869BApprovalActionRequest("Stale named backup", comparison.Version - 1, "backup-comparison-stale")))
                    Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
                var approved = await Post<Rev869BDocumentResult>(client, $"/api/v1/purchase/comparisons/{comparison.ComparisonNumber}/approve",
                    new Rev869BApprovalActionRequest("Independent accounts L1", comparison.Version, "backup-comparison-l1"));
                Assert.Equal(Rev869BStatuses.Approved, approved.Status);
                var comparisonDecision = await admin.PurchaseTransactionApprovalHistories.AsNoTracking().SingleAsync(h =>
                    h.CommercialComparisonId == comparison.Id && h.StepNumber == 1 && h.Action == "Approve");
                Assert.Equal(karthick, comparisonDecision.ResolvedEmployeeId);
                Assert.Equal("ACCOUNTS_MANAGER", comparisonDecision.ResolvedRoleCode);
                using (var snapshot = JsonDocument.Parse(comparison.ApprovalWorkflowSnapshotJson))
                    Assert.Equal(manager, snapshot.RootElement.GetProperty("steps")[0].GetProperty("employeeId").GetGuid());
                using (var noMaker = await client.PostAsJsonAsync("/api/v1/purchase/purchase-orders",
                    new Rev869BCreatePurchaseOrderRequest(approved.Number, approved.Version, "backup-no-po-maker")))
                    Assert.Equal(HttpStatusCode.Forbidden, noMaker.StatusCode);
                var owner = await admin.Employees.SingleAsync(e => e.Id == comparison.OwnerEmployeeId);
                var ownerMapping = await admin.EmployeeIdentityMappings.AsNoTracking().SingleAsync(m =>
                    m.CompanyId == comparison.CompanyId && m.EmployeeId == owner.Id && m.IdentityType == "HUMAN" && m.IsActive);
                var membership = await admin.EmployeeCompanyAssignments.SingleAsync(m => m.CompanyId == comparison.CompanyId && m.EmployeeId == owner.Id && m.IsActive);
                var ownerDepartment = await admin.EmployeeDepartmentAssignments.Where(d => d.EmployeeCompanyAssignmentId == membership.Id && d.IsActive && d.IsPrimary)
                    .Select(d => d.DepartmentId).SingleAsync();
                user.Set(owner.Id, ownerMapping.Subject, "PURCHASE_MANAGER", "PURCHASE_MANAGER", "PURCHASE_EXECUTIVE");
                user.SetIdentityContext(ownerMapping.Issuer, ownerDepartment);
                var po = await Post<Rev869BDocumentResult>(client, "/api/v1/purchase/purchase-orders",
                    new Rev869BCreatePurchaseOrderRequest(approved.Number, approved.Version, "independent-maker-po"));
                po = await Post<Rev869BDocumentResult>(client, $"/api/v1/purchase/purchase-orders/{po.Number}/submit",
                    new Rev869BSubmitPurchaseOrderRequest("Independent maker submits", po.Version, "independent-maker-po-submit"));
                user.Set(karthick, backupSubject, "ACCOUNTS_MANAGER");
                user.SetIdentityContext(backupIssuer, backupDepartment);
                user.SetAuthorityProfile(AccountsBackupIdentityPolicy.MappingType, "SESS-41");
                Assert.Equal(po.Id, (await Get<JsonElement>(client, $"/api/v1/purchase/purchase-orders/{po.Number}")).GetProperty("Id").GetGuid());
                using (var stale = await client.PostAsJsonAsync($"/api/v1/purchase/purchase-orders/{po.Number}/approve",
                    new Rev869BPoApprovalActionRequest("Stale accounts decision", po.Version - 1, null, "backup-po-stale")))
                    Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
                po = await Post<Rev869BDocumentResult>(client, $"/api/v1/purchase/purchase-orders/{po.Number}/approve",
                    new Rev869BPoApprovalActionRequest("Independent accounts L1", po.Version, null, "backup-po-l1"));
                Assert.Equal(Rev869BStatuses.Approved, po.Status);
                var poDecision = await admin.PurchaseOrderHistories.AsNoTracking().SingleAsync(h =>
                    h.PurchaseOrderId == po.Id && h.StepNumber == 1 && h.Action == "Approve");
                Assert.Equal(karthick, poDecision.ResolvedEmployeeId);
                Assert.Equal("ACCOUNTS_MANAGER", poDecision.ResolvedRoleCode);
                using (var noIssue = await client.PostAsJsonAsync($"/api/v1/purchase/purchase-orders/{po.Number}/issue",
                    new Rev869BIssuePurchaseOrderRequest("Accounts cannot issue", po.Version, "backup-no-issue")))
                    Assert.Equal(HttpStatusCode.Forbidden, noIssue.StatusCode);
                Assert.Equal(Rev869BStatuses.Approved, await admin.PurchaseOrders.Where(p => p.Id == po.Id).Select(p => p.Status).SingleAsync());
                proved = true;
                throw new AccountsPrWitnessComplete();
            }));
        Assert.True(proved);
    }

    private static async Task<(TaxWorkflowUser User, Guid Karthick, Guid Manager)> PrepareNamedAccountsRuntimeUser(
        NexaErpDbContext db, Guid company, string organization, Guid? warehouse)
    {
        var date = db.BusinessDate;
        var karthick = await db.Employees.SingleAsync(e => e.EmployeeCode == "SESS-41");
        var manager = await db.Employees.SingleAsync(e => e.EmployeeCode == "SESS-14");
        var role = await db.Roles.SingleAsync(r => r.Code == "ACCOUNTS_MANAGER");
        var membership = await db.EmployeeCompanyAssignments.SingleAsync(m => m.CompanyId == company && m.EmployeeId == karthick.Id && m.IsActive && m.Status == "ACTIVE");
        var department = await db.EmployeeDepartmentAssignments.Where(d => d.EmployeeCompanyAssignmentId == membership.Id && d.IsActive && d.IsPrimary)
            .Select(d => d.DepartmentId).SingleAsync();
        await using var tx = await db.Database.BeginTransactionAsync();
        var authority = await db.EmployeeRoleAssignments.Where(a => a.CompanyId == company && a.Role!.Code == "TECHNICAL_DIRECTOR" && a.AssignmentType == "FULL" && a.EffectiveTo == null)
            .Select(a => a.Id).SingleAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('sess.role_authority_assignment_id',{authority.ToString()},true)");
        db.EmployeeRoleAssignments.Add(new EmployeeRoleAssignment {
            CompanyId = company, EmployeeId = karthick.Id, RoleId = role.Id, EffectiveFrom = date,
            AssignmentType = "FULL", Remarks = "Disposable named accounts command witness"
        });
        var subject = "named-accounts-" + Guid.NewGuid().ToString("N");
        var mapping = Mapping(company, karthick.Id, subject);
        mapping.OrganizationId = organization;
        mapping.IdentityType = AccountsBackupIdentityPolicy.MappingType;
        mapping.EffectiveFrom = date;
        db.EmployeeIdentityMappings.Add(mapping);
        db.EmployeeOperationalScopes.Add(new EmployeeOperationalScope {
            CompanyId = company, OrganizationId = organization, EmployeeId = karthick.Id, DepartmentId = department,
            WarehouseId = warehouse, IsActive = true, EffectiveFrom = date, OwnRecordsOnly = false,
            AllowsPrivilegedCrossScope = false, Remarks = "Disposable accounts actor scope", CreatedBy = "ACCOUNTS_RUNTIME_TEST"
        });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        var assignments = (await db.EmployeeRoleAssignments.AsNoTracking().Include(a => a.Role).Where(a =>
            a.CompanyId == company && (a.ApprovalStatus == "Approved" || a.ApprovalStatus == "SeedApproved") &&
            a.EffectiveFrom <= date && (!a.EffectiveTo.HasValue || a.EffectiveTo >= date)).ToListAsync())
            .ToDictionary(a => TaxWorkflowUser.AssignmentKey(a.EmployeeId, a.Role!.Code), a => new EffectiveRoleAssignment(a.Id, a.Role!.Code, a.AssignmentType));
        var user = new TaxWorkflowUser(karthick.Id, subject, "ACCOUNTS_MANAGER", assignments);
        user.SetOrganization(organization);
        user.SetIdentityContext(mapping.Issuer, department);
        user.SetAuthorityProfile(AccountsBackupIdentityPolicy.MappingType, "SESS-41");
        return (user, karthick.Id, manager.Id);
    }
}
