using System.Net;
using System.Net.Http.Json;
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
    private sealed class AccountsPrWitnessComplete : Exception { }

    [Theory]
    [InlineData("LOW")]
    [InlineData("TD")]
    [InlineData("MD")]
    public async Task NamedAccountsBackupReadsAndVerifiesMappedPrWithoutPurchaseDepartmentMembership(string targetBand)
    {
        var proved = false;
        await Assert.ThrowsAsync<AccountsPrWitnessComplete>(() => RunCompletePurchaseFlow(
            scopeAlignmentWitness: true,
            workload: async context =>
            {
                if (context.Stage != "PR_SUBMITTED" || context.Band != targetBand) return;
                await using var admin = new NexaErpDbContext(context.Options);
                var pr = await admin.PurchaseRequisitions.AsNoTracking().SingleAsync(p => p.Id == context.DocumentId);
                var karthick = await admin.Employees.SingleAsync(e => e.EmployeeCode == "SESS-41");
                var manager = await admin.Employees.SingleAsync(e => e.EmployeeCode == "SESS-14");
                var role = await admin.Roles.SingleAsync(r => r.Code == "ACCOUNTS_MANAGER");
                var date = admin.BusinessDate;
                var membership = await admin.EmployeeCompanyAssignments.SingleAsync(m =>
                    m.CompanyId == pr.CompanyId && m.EmployeeId == karthick.Id && m.IsActive && m.Status == "ACTIVE");
                var department = await admin.EmployeeDepartmentAssignments.Where(d =>
                    d.CompanyId == pr.CompanyId && d.EmployeeCompanyAssignmentId == membership.Id && d.IsActive && d.IsPrimary)
                    .Select(d => d.DepartmentId).SingleAsync();
                Assert.NotEqual(pr.RequestingDepartmentId, department);
                var assignment = new EmployeeRoleAssignment {
                    CompanyId = pr.CompanyId, EmployeeId = karthick.Id, RoleId = role.Id,
                    EffectiveFrom = date, AssignmentType = "FULL", Remarks = "Disposable named accounts witness"
                };
                await using var setupTx = await admin.Database.BeginTransactionAsync();
                var authority = await admin.EmployeeRoleAssignments.Where(a => a.CompanyId == pr.CompanyId &&
                    a.Role!.Code == "TECHNICAL_DIRECTOR" && a.AssignmentType == "FULL" && a.EffectiveTo == null)
                    .Select(a => a.Id).SingleAsync();
                await admin.Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('sess.role_authority_assignment_id',{authority.ToString()},true)");
                admin.EmployeeRoleAssignments.Add(assignment);
                var mapping = Mapping(pr.CompanyId, karthick.Id, "named-accounts-runtime");
                mapping.IdentityType = AccountsBackupIdentityPolicy.MappingType;
                mapping.EffectiveFrom = date;
                admin.EmployeeIdentityMappings.Add(mapping);
                admin.EmployeeOperationalScopes.Add(new EmployeeOperationalScope {
                    CompanyId = pr.CompanyId, OrganizationId = pr.OrganizationId, EmployeeId = karthick.Id,
                    DepartmentId = department, WarehouseId = pr.DeliveryWarehouseId, IsActive = true,
                    EffectiveFrom = date, OwnRecordsOnly = false, AllowsPrivilegedCrossScope = false,
                    Remarks = "Disposable stores-department accounts read", CreatedBy = "ACCOUNTS_RUNTIME_TEST"
                });
                await admin.SaveChangesAsync();
                await setupTx.CommitAsync();
                var known = new Dictionary<string,EffectiveRoleAssignment> {
                    [TaxWorkflowUser.AssignmentKey(karthick.Id, role.Code)] = new(assignment.Id, role.Code, "FULL")
                };
                var storesAssignment = await admin.EmployeeRoleAssignments.AsNoTracking().SingleAsync(a =>
                    a.CompanyId == pr.CompanyId && a.EmployeeId == karthick.Id && a.Role!.Code == "STORES_MANAGER" &&
                    a.AssignmentType == "FULL" && a.EffectiveFrom <= date && (!a.EffectiveTo.HasValue || a.EffectiveTo >= date));
                known.Add(TaxWorkflowUser.AssignmentKey(karthick.Id, "STORES_MANAGER"), new(storesAssignment.Id, "STORES_MANAGER", "FULL"));
                var staffMapping = await admin.EmployeeIdentityMappings.AsNoTracking().SingleAsync(m =>
                    m.CompanyId == pr.CompanyId && m.EmployeeId == karthick.Id && m.IdentityType == "HUMAN" && m.IsActive);
                var user = new TaxWorkflowUser(karthick.Id, mapping.Subject, role.Code, known);
                user.SetIdentityContext(mapping.Issuer, department);
                user.SetAuthorityProfile(AccountsBackupIdentityPolicy.MappingType, "SESS-41");
                await using var host = await PurchaseFlowHost.StartAsync(context.RuntimeConnection, user,
                    useRealPagePermissions: true, useRealOperationalScopes: true);
                var client = host.Client;
                var session = await Get<SESS.NexaERP.Application.Identity.SessionMe>(client, "/api/v1/session/me");
                Assert.Equal(new[] { "ACCOUNTS_MANAGER" }, session.RoleCodes);
                Assert.Contains("purchase.requisition-approvals:approve", session.Permissions);
                Assert.Contains("purchase.requisitions:verify", session.Permissions);
                Assert.DoesNotContain("masters.items:create", session.Permissions);
                Assert.DoesNotContain("purchase.requisition-approvals:update", session.Permissions);
                Assert.DoesNotContain("purchase.po:issue", session.Permissions);
                Assert.DoesNotContain("stores.material-issues:create", session.Permissions);
                var read = await Get<PurchaseRequisitionDetail>(client, $"/api/v1/purchase/requisitions/{pr.PrNumber}");
                Assert.Equal(pr.Id, read.Id);
                user.SetOrganization("SESS_PROPRIETORSHIP");
                using (var refused = await client.GetAsync($"/api/v1/purchase/requisitions/{pr.PrNumber}"))
                    Assert.Contains(refused.StatusCode, new[] { HttpStatusCode.Forbidden, HttpStatusCode.NotFound });
                user.SetOrganization(pr.OrganizationId);
                var verified = await Post<PurchaseRequisitionDetail>(client, $"/api/v1/purchase/requisitions/{pr.PrNumber}/verify",
                    new PurchaseRequisitionActionRequest("Named independent accounts backup", read.Version, "named-backup-verify-" + targetBand));
                Assert.Equal(PurchaseRequisitionStatuses.PendingApproval, verified.Status);
                using var snapshot = System.Text.Json.JsonDocument.Parse(await admin.PurchaseRequisitions.AsNoTracking()
                    .Where(p => p.Id == pr.Id).Select(p => p.ApprovalWorkflowSnapshotJson).SingleAsync());
                Assert.Equal(manager.Id, snapshot.RootElement.GetProperty("steps")[0].GetProperty("employeeId").GetGuid());
                using (var stale = await client.PostAsJsonAsync($"/api/v1/purchase/requisitions/{pr.PrNumber}/approve",
                    new PurchaseRequisitionActionRequest("Stale named backup", verified.Version - 1, "named-backup-stale-" + targetBand)))
                    Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
                var approved = await Post<PurchaseRequisitionDetail>(client, $"/api/v1/purchase/requisitions/{pr.PrNumber}/approve",
                    new PurchaseRequisitionActionRequest("Independent named accounts L1", verified.Version, "named-backup-l1-" + targetBand));
                Assert.Equal(targetBand == "LOW" ? PurchaseRequisitionStatuses.StockCheckPending : PurchaseRequisitionStatuses.PendingApproval, approved.Status);
                var decision = await admin.PurchaseRequisitionApprovalHistories.AsNoTracking().SingleAsync(h =>
                    h.PurchaseRequisitionId == pr.Id && h.StepNumber == 1 && h.Action == "Approve");
                Assert.Equal(karthick.Id, decision.ResolvedEmployeeId);
                Assert.Equal("ACCOUNTS_MANAGER", decision.ResolvedRoleCode);
                if (targetBand == "LOW") {
                    user.Set(karthick.Id, staffMapping.Subject, "STORES_MANAGER");
                    user.SetIdentityContext(staffMapping.Issuer, department);
                    var staffSession = await Get<SESS.NexaERP.Application.Identity.SessionMe>(client, "/api/v1/session/me");
                    // The signed R1 native manager role has no stock-check Verify grant.
                    // Keep that refusal; do not grant a new stores operation to make a test pass.
                    Assert.DoesNotContain("stores.stock-check:verify", staffSession.Permissions);
                    using (var noStockRead = await client.GetAsync($"/api/v1/stores/stock-check/requisitions/{pr.PrNumber}"))
                        Assert.Equal(HttpStatusCode.Forbidden, noStockRead.StatusCode);
                    // Independently exercise the per-document guard against actual retained L1
                    // history, so even a future/adversarial grant cannot bypass canonical SoD.
                    await using (var dutyTx = await admin.Database.BeginTransactionAsync()) {
                        var retainedPr = await admin.PurchaseRequisitions.AsNoTracking().SingleAsync(p => p.Id == pr.Id);
                        var dutyRefusal = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                            SESS.NexaERP.Infrastructure.Authorization.AccountsDocumentConflictGuard.RequireRequisitionDutyAsync(
                                admin, user, retainedPr, SESS.NexaERP.Domain.Authorization.AccountsDocumentDuty.Recorded, default));
                        Assert.Contains("conflicting stores/accounts capacity", dutyRefusal.Message);
                        await dutyTx.RollbackAsync();
                    }
                    var checksBefore = await admin.StockAvailabilityChecks.CountAsync(c => c.PurchaseRequisitionId == pr.Id);
                    using (var conflict = await client.PostAsJsonAsync($"/api/v1/purchase/requisitions/{pr.PrNumber}/stock-check",
                        new StockCheckRequest("Same employee must not also stock-check", approved.Version, "backup-cross-realm-stock-refused"))) {
                        Assert.Equal(HttpStatusCode.Forbidden, conflict.StatusCode);
                        Assert.Contains("PERMISSION_DENIED", await conflict.Content.ReadAsStringAsync());
                    }
                    Assert.Equal(checksBefore, await admin.StockAvailabilityChecks.CountAsync(c => c.PurchaseRequisitionId == pr.Id));
                    Assert.Equal(approved.Version, await admin.PurchaseRequisitions.Where(p => p.Id == pr.Id).Select(p => p.Version).SingleAsync());
                    user.Set(karthick.Id, mapping.Subject, "ACCOUNTS_MANAGER");
                    user.SetIdentityContext(mapping.Issuer, department);
                    user.SetAuthorityProfile(AccountsBackupIdentityPolicy.MappingType, "SESS-41");
                }
                Assert.False(await admin.EmployeeDepartmentAssignments.AnyAsync(d => d.EmployeeCompanyAssignmentId == membership.Id &&
                    d.DepartmentId == pr.RequestingDepartmentId && d.IsActive));
                Assert.Equal(pr.Id, (await Get<PurchaseRequisitionDetail>(client, $"/api/v1/purchase/requisitions/{pr.PrNumber}")).Id);
                proved = true;
                throw new AccountsPrWitnessComplete();
            }));
        Assert.True(proved);
    }
}
