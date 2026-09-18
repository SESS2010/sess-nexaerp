using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private static async Task ProveSeededTechnicalVerification(HttpClient client,
        DbContextOptions<NexaErpDbContext> options, TaxWorkflowUser user,
        Rev869BDocumentResult quote, Guid actor, string employeeCode, string role, string key, bool proveDenials)
    {
        user.Set(actor, employeeCode, role);
        Assert.NotEqual(Guid.Empty, Assert.Single(user.EffectiveRoleAssignments).AssignmentId);
        await AssertResolvedSeedRole(options, user, role);
        await using var db = new NexaErpDbContext(options);
        Assert.False(await db.EmployeeOperationalScopes.AnyAsync(x => x.EmployeeId == actor &&
            x.CreatedBy == "PURCHASE_FLOW_TEST"));
        var detailPath = "/api/v1/purchase/quotations/" + quote.Number;
        var list = await Get<PagedResponse<QuotationListItem>>(client,
            "/api/v1/purchase/quotations?quotationNumber=" + quote.Number);
        Assert.Equal(quote.Id, Assert.Single(list.Items).Id);
        var detail = await Get<JsonElement>(client, detailPath);
        var attachment = await Get<JsonElement>(client, detailPath + "/attachment");
        Assert.Equal(quote.Number, attachment.GetProperty("QuotationNumber").GetString());
        var line = Assert.Single(detail.GetProperty("Lines").EnumerateArray());
        Assert.NotEqual(Guid.Empty, line.GetProperty("ItemId").GetGuid());
        Assert.False(string.IsNullOrWhiteSpace(line.GetProperty("ItemCode").GetString()));
        Assert.True(line.TryGetProperty("Specification", out _));
        if (role == "TECHNICAL_SUPPORT_MANAGER") Assert.False(line.TryGetProperty("UnitRate", out _));
        var request = new Rev869BTechnicalVerificationRequest(line.GetProperty("Id").GetGuid(), true,
            """{"witness":"seeded own-department scope"}""", "Technical verification from actor-readable quotation",
            detail.GetProperty("Version").GetUInt32(), key);
        var path = detailPath + "/technical-verifications";
        if (proveDenials)
        {
            var company = await db.Companies.Where(x => x.Code == "SESS_PVT_LTD").Select(x => x.Id).SingleAsync();
            var memberships = await db.EmployeeCompanyAssignments.Where(x => x.EmployeeId == actor && x.CompanyId == company)
                .Select(x => x.Id).ToArrayAsync();
            var departmentAssignments = await db.EmployeeDepartmentAssignments.Where(x => memberships.Contains(x.EmployeeCompanyAssignmentId) && x.IsActive)
                .Select(x => x.Id).ToArrayAsync();
            Assert.NotEmpty(departmentAssignments);
            await db.EmployeeDepartmentAssignments.Where(x => departmentAssignments.Contains(x.Id))
                .ExecuteUpdateAsync(x => x.SetProperty(p => p.IsActive, false));
            try
            {
                using var hidden = await client.GetAsync(detailPath);
                Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
                using var refused = await client.PostAsJsonAsync(path, request with { IdempotencyKey = key + "-no-department" });
                Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
            }
            finally
            {
                await db.EmployeeDepartmentAssignments.Where(x => departmentAssignments.Contains(x.Id))
                    .ExecuteUpdateAsync(x => x.SetProperty(p => p.IsActive, true));
            }
            user.SetOrganization("SESS_PROPRIETORSHIP");
            try
            {
                using var otherCompany = await client.GetAsync(detailPath);
                Assert.Contains(otherCompany.StatusCode, new[] { HttpStatusCode.Forbidden, HttpStatusCode.NotFound });
            }
            finally { user.SetOrganization("SESS_PVT_LTD"); }
        }
        user.Set(actor, employeeCode, role);
        await Post<Rev869BDocumentResult>(client, path, request);
        await AssertTechnicalEvidence(options, quote.Id, actor);
    }
}
