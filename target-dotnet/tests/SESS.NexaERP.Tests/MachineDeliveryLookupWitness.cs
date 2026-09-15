using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    // Part of the existing routine canonical workflow, not another gated full-flow test.
    private static async Task AssertMachineDeliveryJobOrderReachability(MachineDeliveryWitnessContext context)
    {
        var user=context.User;
        var originalEmployee=user.CurrentEmployeeId;
        var originalLogin=user.LoginId;
        var originalRole=user.RoleCode;
        var originalRoles=user.RoleCodes.ToArray();
        var originalCompany=user.CurrentOrganizationId;
        await using var host=await PurchaseFlowHost.StartAsync(context.RuntimeConnection,user,useRealPagePermissions:true);
        await using var db=new NexaErpDbContext(context.Options);
        var job=await db.JobOrders.SingleAsync(j=>j.MachineSerial=="WITNESS-MACHINE-001-CORRECTED");
        var path="/api/v1/stores/machine-deliveries/job-orders?search="+Uri.EscapeDataString(job.MachineSerial)+"&pageSize=1";
        try
        {
            user.Set(context.StoresId,"SESS-35","STORES_EXECUTIVE");
            var matches=await Get<PagedResponse<MachineDeliveryJobOrderCandidate>>(host.Client,path);
            var candidate=Assert.Single(matches.Items);
            Assert.Equal(1,matches.TotalCount);
            Assert.Equal(job.Id,candidate.JobOrderId);
            Assert.Equal(job.JobOrderNumber,candidate.JobOrderNumber);
            Assert.Equal(job.MachineSerial,candidate.MachineSerial);
            Assert.Equal("READY",candidate.FatReadinessStatus);
            var missing=await Get<PagedResponse<MachineDeliveryJobOrderCandidate>>(host.Client,
                "/api/v1/stores/machine-deliveries/job-orders?search="+Guid.NewGuid().ToString("N"));
            Assert.Empty(missing.Items);
            using(var production=await host.Client.GetAsync("/api/v1/production/job-orders/"))
                Assert.Equal(HttpStatusCode.Forbidden,production.StatusCode);
            user.Set(context.AccountsId,"SESS-14","ACCOUNTS_MANAGER");
            using(var denied=await host.Client.GetAsync(path)) Assert.Equal(HttpStatusCode.Forbidden,denied.StatusCode);
            user.Set(context.StoresId,"SESS-35","STORES_EXECUTIVE");
            user.SetOrganization("SESS_PROPRIETORSHIP");
            using(var wrongCompany=await host.Client.GetAsync(path))
            {
                if(wrongCompany.IsSuccessStatusCode)
                {
                    var isolated=await wrongCompany.Content.ReadFromJsonAsync<PagedResponse<MachineDeliveryJobOrderCandidate>>();
                    Assert.NotNull(isolated);
                    Assert.Empty(isolated.Items);
                    Assert.Equal(0,isolated.TotalCount);
                }
                else Assert.Equal(HttpStatusCode.Forbidden,wrongCompany.StatusCode);
            }
        }
        finally
        {
            user.SetOrganization(originalCompany);
            user.Set(originalEmployee,originalLogin,originalRole,originalRoles);
        }
    }
}