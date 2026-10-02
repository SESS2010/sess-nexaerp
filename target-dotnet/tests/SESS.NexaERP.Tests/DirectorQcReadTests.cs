using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Stores;

namespace SESS.NexaERP.Tests;

public sealed class DirectorQcReadTests
{
    [Theory]
    [InlineData("QC_MANAGER", true)]
    [InlineData("TECHNICAL_DIRECTOR", true)]
    [InlineData("MANAGING_DIRECTOR", true)]
    [InlineData("STORES_MANAGER", false)]
    [InlineData("IT_MANAGER", false)]
    public async Task Queue_requires_an_effective_QC_manager_or_director_role(string role, bool allowed)
    {
        var service = new EfQcWorkflowService(null!, new QcReader(role), null!);
        // Invalid pagination proves authorization was passed without connecting to a database.
        if (allowed)
            await Assert.ThrowsAsync<StoresValidationException>(() => service.QueueAsync(null, null, false, 0, 10, default));
        else
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.QueueAsync(null, null, false, 0, 10, default));
    }

    [Theory]
    [InlineData("TECHNICAL_DIRECTOR")]
    [InlineData("MANAGING_DIRECTOR")]
    public async Task Director_read_access_does_not_allow_inspection_finalization(string role)
    {
        var service = new EfQcWorkflowService(null!, new QcReader(role), null!);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.FinalizeAsync(
            new(Guid.NewGuid(), DateTimeOffset.UtcNow, 1, 0, 0, null, [], []), "director-denied", default));
    }

    private sealed class QcReader(string role) : ICurrentUser
    {
        public string LoginId => "qc-read-witness";
        public string RoleCode => role;
        public string OrganizationId => "SESS_PVT_LTD";
        public bool IsAuthenticated => true;
        public Guid? EmployeeId { get; } = Guid.NewGuid();
        public IReadOnlyList<EffectiveRoleAssignment> EffectiveRoleAssignments { get; } =
            [new(Guid.NewGuid(), role, "FULL")];
        public void SetResolvedRoleAuthority(ResolvedRoleAuthority authority) { }
    }
}

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private static async Task ProveDirectorQcQueueReads(HttpClient client, DbContextOptions<NexaErpDbContext> options,
        TaxWorkflowUser user, Guid tdId, QcQueueItem lot)
    {
        await using var db = new NexaErpDbContext(options);
        var mdId = await db.Employees.Where(x => x.EmployeeCode == "SESS-02").Select(x => x.Id).SingleAsync();
        var before = await db.QcInspections.CountAsync();
        foreach (var (employee, code, role) in new[]
        {
            (tdId, "SESS-01", "TECHNICAL_DIRECTOR"), (mdId, "SESS-02", "MANAGING_DIRECTOR")
        })
        {
            user.Set(employee, code, role);
            Assert.DoesNotContain(user.EffectiveRoleAssignments, x => x.RoleCode == "QC_MANAGER");
            Assert.NotEqual(Guid.Empty, Assert.Single(user.EffectiveRoleAssignments).AssignmentId);
            var queue = await Get<PagedResponse<QcQueueItem>>(client,
                "/api/v1/qc/queue?allocationId=" + lot.GoodsReceiptLineLotAllocationId);
            Assert.Equal(lot.GoodsReceiptLineLotAllocationId, Assert.Single(queue.Items).GoodsReceiptLineLotAllocationId);
            using var command = new HttpRequestMessage(HttpMethod.Post, "/api/v1/qc/inspections")
            {
                Content = JsonContent.Create(new FinalizeQcInspectionRequest(lot.GoodsReceiptLineLotAllocationId,
                    DateTimeOffset.UtcNow, lot.Quantity, 0, 0, null, [], []))
            };
            command.Headers.Add("Idempotency-Key", "director-finalize-denied-" + code);
            using var denied = await client.SendAsync(command);
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            user.SetOrganization("SESS_PROPRIETORSHIP");
            using var otherCompany = await client.GetAsync("/api/v1/qc/queue?allocationId=" + lot.GoodsReceiptLineLotAllocationId);
            Assert.Contains(otherCompany.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Forbidden });
            if (otherCompany.IsSuccessStatusCode)
                Assert.Empty((await otherCompany.Content.ReadFromJsonAsync<PagedResponse<QcQueueItem>>())!.Items);
            user.SetOrganization("SESS_PVT_LTD");
        }
        Assert.Equal(before, await db.QcInspections.CountAsync());
    }

    private static async Task ProveDirectorQcInspectionReads(HttpClient client, DbContextOptions<NexaErpDbContext> options,
        TaxWorkflowUser user, Guid tdId, Guid mdId)
    {
        await using var db = new NexaErpDbContext(options);
        var company = await db.Companies.Where(x => x.Code == "SESS_PVT_LTD").Select(x => x.Id).SingleAsync();
        var number = await db.QcInspections.Where(x => x.CompanyId == company).OrderBy(x => x.InspectionNumber)
            .Select(x => x.InspectionNumber).FirstAsync();
        var before = await db.QcInspectionRevisions.CountAsync();
        foreach (var (employee, code, role) in new[]
        {
            (tdId, "SESS-01", "TECHNICAL_DIRECTOR"), (mdId, "SESS-02", "MANAGING_DIRECTOR")
        })
        {
            user.Set(employee, code, role);
            var inspection = await Get<QcInspectionResult>(client, "/api/v1/qc/inspections/" + number);
            Assert.Equal(number, inspection.InspectionNumber);
            using var command = new HttpRequestMessage(HttpMethod.Post, "/api/v1/qc/inspections/" + number + "/corrections")
            {
                Content = JsonContent.Create(new CorrectQcInspectionRequest(inspection.RevisionId, "Denied director correction witness",
                    DateTimeOffset.UtcNow, 1, 0, 0, null, [], []))
            };
            command.Headers.Add("Idempotency-Key", "director-correction-denied-" + code);
            using var denied = await client.SendAsync(command);
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            user.SetOrganization("SESS_PROPRIETORSHIP");
            using var otherCompany = await client.GetAsync("/api/v1/qc/inspections/" + number);
            Assert.Contains(otherCompany.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden });
            user.SetOrganization("SESS_PVT_LTD");
        }
        Assert.Equal(before, await db.QcInspectionRevisions.CountAsync());
    }
}
