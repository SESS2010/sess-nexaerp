using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Rev869A;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    // Called by the routine end-to-end witness with real permission and scope services,
    // the ordinary runtime database principal and the seeded QC Manager / TD assignments.
    private static async Task CreateAndDecideQcPolicyThroughApi(HttpClient client,
        DbContextOptions<NexaErpDbContext> options, TaxWorkflowUser user, GoodsReceiptResult grn, Guid qcId, Guid tdId)
    {
        await using (var seed = new NexaErpDbContext(options))
        {
            Assert.True(await seed.EmployeeOperationalScopes.AnyAsync(x => x.EmployeeId == qcId && x.IsActive && x.CreatedBy == "MULTI_COMPANY_EMPLOYEE_AUTH_PART1"));
            Assert.False(await seed.EmployeeOperationalScopes.AnyAsync(x => x.EmployeeId == qcId && x.CreatedBy == "PURCHASE_FLOW_TEST"));
        }
        const string policies = "/api/v1/rev869a/configuration/qc-inspection-policies";
        user.Set(qcId, "SESS-33", "QC_MANAGER");
        Assert.All(user.EffectiveRoleAssignments, x => Assert.NotEqual(Guid.Empty, x.AssignmentId));
        var queue = await Get<PagedResponse<QcQueueItem>>(client, "/api/v1/qc/queue?pageSize=100");
        var lot = Assert.Single(queue.Items, x => x.GrnNumber == grn.GrnNumber);
        var byAllocation = await Get<PagedResponse<QcQueueItem>>(client,
            "/api/v1/qc/queue?allocationId=" + lot.GoodsReceiptLineLotAllocationId);
        Assert.Equal(1, byAllocation.TotalCount);
        Assert.Equal(lot.GoodsReceiptLineLotAllocationId, Assert.Single(byAllocation.Items).GoodsReceiptLineLotAllocationId);
        var byGrn = await Get<PagedResponse<QcQueueItem>>(client, "/api/v1/qc/queue?grnNumber=" + grn.GrnNumber);
        Assert.Equal(1, byGrn.TotalCount);
        Assert.Equal(lot.GoodsReceiptLineLotAllocationId, Assert.Single(byGrn.Items).GoodsReceiptLineLotAllocationId);
        var overdue = await Get<PagedResponse<QcQueueItem>>(client, "/api/v1/qc/queue?overdueOnly=true&pageSize=100");
        Assert.Equal(lot.IsOverdue, overdue.Items.Any(x => x.GoodsReceiptLineLotAllocationId == lot.GoodsReceiptLineLotAllocationId));
        var uoms = await Get<JsonElement>(client, "/api/v1/masters/uoms?pageSize=100&isActive=true");
        var uom = uoms.GetProperty("Items").EnumerateArray().First().GetProperty("Code").GetString()!;
        var request = new CreateQcInspectionPolicyRequest("SESS_PVT_LTD", lot.ItemCode, null,
            "DIMENSIONAL_LIMIT", uom, 0, 10, "Calibrated measurement", 1, new(2026, 1, 1), null,
            "QC prepares measured acceptance criteria");
        var created = await Post<JsonElement>(client, policies, request);
        var id = created.GetProperty("Id").GetGuid();
        var pendingList = await Get<JsonElement>(client, policies + "?effectiveOnly=false");
        var pending = Assert.Single(pendingList.EnumerateArray(), x => x.GetProperty("Id").GetGuid() == id);
        Assert.Equal(MasterApprovalStatuses.PendingApproval, pending.GetProperty("ApprovalStatus").GetString());
        Assert.Equal(lot.ItemCode, pending.GetProperty("ItemCode").GetString());
        var version = pending.GetProperty("Version").GetUInt32();
        var approvePath = policies + "/" + id + "/approve";
        using (var denied = await QcDecisionResponse(client, approvePath, new("QC cannot self-approve", version), "qc-self-approve"))
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        user.Set(tdId, "SESS-01", "TECHNICAL_DIRECTOR");
        Assert.All(user.EffectiveRoleAssignments, x => Assert.NotEqual(Guid.Empty, x.AssignmentId));
        var tdPolicies = await Get<JsonElement>(client, policies + "?effectiveOnly=false");
        Assert.Equal(version, Assert.Single(tdPolicies.EnumerateArray(),
            x => x.GetProperty("Id").GetGuid() == id).GetProperty("Version").GetUInt32());
        user.SetOrganization("SESS_PROPRIETORSHIP");
        using (var wrongCompany = await QcDecisionResponse(client, approvePath, new("Wrong company", version), "qc-cross-company"))
            Assert.Contains(wrongCompany.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden });
        user.SetOrganization("SESS_PVT_LTD");
        using (var stale = await QcDecisionResponse(client, approvePath, new("Stale decision", version + 1), "qc-stale-approve"))
            Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        using (var empty = await QcDecisionResponse(client, approvePath, new("", version), "qc-empty-approve"))
            Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        var approved = await Post<JsonElement>(client, approvePath, new MasterActionRequest("TD approves acceptance limits", version), "qc-policy-approve");
        Assert.Equal(MasterApprovalStatuses.Approved, approved.GetProperty("ApprovalStatus").GetString());
        using (var repeat = await QcDecisionResponse(client, approvePath, new("Already decided", version), "qc-repeat-approve"))
            Assert.Equal(HttpStatusCode.Conflict, repeat.StatusCode);
        await using (var db = new NexaErpDbContext(options))
        {
            var history = await db.ControlledConfigurationHistories.Where(x =>
                x.EntityType == nameof(QcInspectionPolicy) && x.EntityId == id).OrderBy(x => x.Version).ToListAsync();
            Assert.Equal(new[] { "CreateVersion", "Approve" }, history.Select(x => x.Action));
            Assert.Equal("QC_MANAGER", history[0].ActorRoleCode);
            Assert.Equal("TECHNICAL_DIRECTOR", history[1].ActorRoleCode);
            Assert.Equal(1u, history[1].Version);
            Assert.Equal(2, await db.AuditLogs.CountAsync(x => x.EntityName == nameof(QcInspectionPolicy) && x.EntityId == id.ToString()));
        }
        // Rejection is a real exit too; it must not leave an active overlapping policy.
        user.Set(qcId, "SESS-33", "QC_MANAGER");
        var rejectedRequest = request with { ParameterCode = "REJECTED_CRITERION" };
        var rejectedId = (await Post<JsonElement>(client, policies, rejectedRequest)).GetProperty("Id").GetGuid();
        user.Set(tdId, "SESS-01", "TECHNICAL_DIRECTOR");
        var rejected = await Post<JsonElement>(client, policies + "/" + rejectedId + "/reject",
            new MasterActionRequest("Incorrect acceptance criterion", 0), "qc-policy-reject");
        Assert.Equal(MasterApprovalStatuses.Rejected, rejected.GetProperty("ApprovalStatus").GetString());
        Assert.False(rejected.GetProperty("IsActive").GetBoolean());
        user.Set(qcId, "SESS-33", "QC_MANAGER");
        var effective = await Get<JsonElement>(client, policies + "?effectiveOnly=true");
        Assert.Contains(effective.EnumerateArray(), x => x.GetProperty("Id").GetGuid() == id);
        Assert.DoesNotContain(effective.EnumerateArray(), x => x.GetProperty("Id").GetGuid() == rejectedId);
        Assert.True(Assert.Single((await Get<PagedResponse<QcQueueItem>>(client,
            "/api/v1/qc/queue?pageSize=100")).Items, x => x.GrnNumber == grn.GrnNumber).HasEffectivePolicy);
    }

    private static Task<HttpResponseMessage> QcDecisionResponse(HttpClient client, string path, MasterActionRequest body, string key)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", key);
        return client.SendAsync(request);
    }
}
