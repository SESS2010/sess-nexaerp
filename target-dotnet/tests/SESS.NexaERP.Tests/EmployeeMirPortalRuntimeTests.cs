using System.Net;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public Task EmployeeMirPortalEnforcesOwnerBeforeReadPaginationAndReturnAcrossRealPurchaseCycle() =>
        RunCompletePurchaseFlow(portalOwnershipWitness:true);

    private static async Task AssertEmployeePortalOwnership(HttpClient client,TaxWorkflowUser user,
        Guid requester,Guid custodian,MaterialIssueRequestView mir,MaterialIssueView issue,
        MaterialReturnView materialReturn,MaterialReturnView consumableReturn)
    {
        const string root="/api/v1/employee/my-mirs";
        user.Set(requester,"TEST-R1-PURCHASE-SUPPORT",MirRequesterPolicy.Role);
        Assert.NotEqual(Guid.Empty,user.EffectiveRoleAssignments.Single().AssignmentId);
        var own=await Get<MaterialIssueRequestView>(client,$"{root}/{mir.Id}");
        Assert.Equal(requester,own.RequestedByEmployeeId);
        var first=await Get<MaterialIssueRequestPage>(client,$"{root}?page=1&pageSize=1");
        Assert.True(first.Total>0); Assert.Single(first.Items);
        Assert.All(first.Items,row=>Assert.Equal(requester,row.RequestedByEmployeeId));
        using(var foreignIssue=await client.GetAsync($"{root}/issues/{issue.Id}")) Assert.Equal(HttpStatusCode.NotFound,foreignIssue.StatusCode);
        using(var foreignReturn=await client.GetAsync($"{root}/returns/{materialReturn.Id}")) Assert.Equal(HttpStatusCode.NotFound,foreignReturn.StatusCode);
        await AssertPostStatus(client,$"{root}/issues/{issue.Id}/returns",
            new CreateMaterialReturn(DateTimeOffset.UtcNow,[],"foreign-return-refused"),HttpStatusCode.NotFound);
        Assert.Empty(await Get<OutstandingEngineerCustodyView[]>(client,$"{root}/custody"));
        var requesterReturns=await Get<MaterialReturnPage>(client,$"{root}/returns?page=1&pageSize=1");
        Assert.Equal(0,requesterReturns.Total); Assert.Empty(requesterReturns.Items);
        user.Set(custodian,"SESS-05",MirRequesterPolicy.Role);
        Assert.NotEqual(Guid.Empty,user.EffectiveRoleAssignments.Single().AssignmentId);
        using(var foreignMir=await client.GetAsync($"{root}/{mir.Id}")) Assert.Equal(HttpStatusCode.NotFound,foreignMir.StatusCode);
        await AssertPostStatus(client,$"{root}/{mir.Id}/submit",
            new MaterialIssueTransitionRequest(mir.Version,"Not my MIR","foreign-submit-refused"),HttpStatusCode.NotFound);
        var ownedIssue=await Get<EmployeeMirIssue>(client,$"{root}/issues/{issue.Id}");
        Assert.Equal(custodian,ownedIssue.Issue.IssuedToEmployeeId);
        Assert.All(ownedIssue.ReturnableQuantities.Values,quantity=>Assert.True(quantity>=0));
        Assert.Equal(materialReturn.Id,(await Get<MaterialReturnView>(client,$"{root}/returns/{materialReturn.Id}")).Id);
        Assert.Equal(consumableReturn.Id,(await Get<MaterialReturnView>(client,$"{root}/returns/{consumableReturn.Id}")).Id);
        var returns=await Get<MaterialReturnPage>(client,$"{root}/returns?page=1&pageSize=1");
        Assert.Equal(3,returns.Total); Assert.Single(returns.Items);
        var second=await Get<MaterialReturnPage>(client,$"{root}/returns?page=2&pageSize=1");
        Assert.Equal(3,second.Total); Assert.Single(second.Items);
        Assert.NotEqual(returns.Items.Single().Id,second.Items.Single().Id);
        // The base flow also contains the earlier exact-serial return, so all three owned rows must page distinctly.
        var third=await Get<MaterialReturnPage>(client,$"{root}/returns?page=3&pageSize=1");
        Assert.Equal(3,third.Total);Assert.Single(third.Items);
        Assert.Equal(3,new[]{returns.Items.Single().Id,second.Items.Single().Id,third.Items.Single().Id}.Distinct().Count());
        user.SetOrganization("SESS_PROPRIETORSHIP");
        using(var otherCompany=await client.GetAsync($"{root}/issues/{issue.Id}"))
            Assert.Contains(otherCompany.StatusCode,new[]{HttpStatusCode.NotFound,HttpStatusCode.Forbidden});
        user.SetOrganization("SESS_PVT_LTD");
    }
}
