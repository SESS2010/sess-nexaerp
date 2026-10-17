using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Stores;

namespace SESS.NexaERP.Tests;

public sealed class AccountsBackupActionBoundaryTests
{
    [Theory]
    [InlineData("SESS_PVT_LTD")]
    [InlineData("SESS_PROPRIETORSHIP")]
    public async Task AccountsIdentityCannotCreateMirOrDeclareReturnEvenThroughDirectServiceCall(string company)
    {
        using var db=new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var user=new BackupActor(company);var service=new EfMaterialIssueService(db,user,new NeverAudit());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.CreateRequestAsync(new("DEMO","CONSUMABLE_OFFICE","DEPARTMENT",
            null,null,null,null,"Department",Guid.NewGuid(),new DateOnly(2026,10,17),[],"accounts-create-refused"),default));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.CreateReturnAsync(Guid.NewGuid(),new(DateTimeOffset.UtcNow,[],"accounts-return-refused"),default));
        Assert.False(user.AuthoritySelected);
    }
    [Theory]
    [InlineData("CreateRFQ")]
    [InlineData("InviteVendor")]
    [InlineData("SubmitQuotation")]
    [InlineData("CreateComparison")]
    [InlineData("RecommendComparison")]
    [InlineData("CreatePO")]
    [InlineData("SubmitPO")]
    [InlineData("IssuePO")]
    [InlineData("CancelPO")]
    public void PurchaseMakerAndIssueOperationsNeverBelongToAccountsIdentity(string operation) =>
        Assert.False(AccountsBackupIdentityPolicy.AllowsPurchaseDecisionOperation(operation));
    [Theory]
    [InlineData("ApproveComparison")]
    [InlineData("RejectComparison")]
    [InlineData("ApprovePO")]
    [InlineData("RejectPO")]
    public void DecisionOperationStillRequiresIndependentWorkflowAndDocumentGuards(string operation) =>
        Assert.True(AccountsBackupIdentityPolicy.AllowsPurchaseDecisionOperation(operation));
    [Theory]
    [InlineData("SESS_PVT_LTD")]
    [InlineData("SESS_PROPRIETORSHIP")]
    public async Task AccountsIdentityCannotCountWithdrawOrFinallyAuthorizeOpeningStock(string company)
    {
        using var db=new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var user=new BackupActor(company);var service=new EfOpeningStockService(db,user,new NeverAudit());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.RecordCountAsync(
            new(Guid.NewGuid(),new DateOnly(2026,4,1),new DateOnly(2027,3,31),"not an accounts task","backup-count-refused"),default));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.WithdrawAsync(Guid.NewGuid(),new(0,"refused","backup-withdraw-refused"),default));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.AuthorizeAsync(Guid.NewGuid(),new(0,"refused","backup-authorize-refused"),default));
        Assert.False(user.AuthoritySelected);
    }
    private sealed class BackupActor(string company) : ICurrentUser
    {
        public string IdentityAuthorityType=>AccountsBackupIdentityPolicy.MappingType;
        public string LoginId=>"accounts-test"; public string RoleCode=>"ACCOUNTS_MANAGER";
        public string OrganizationId=>company;public bool IsAuthenticated=>true;public Guid? EmployeeId {get;}=Guid.NewGuid();
        public IReadOnlyList<EffectiveRoleAssignment> EffectiveRoleAssignments {get;}=[new(Guid.NewGuid(),"ACCOUNTS_MANAGER","FULL")];
        public bool AuthoritySelected{get;private set;}
        public void SetResolvedRoleAuthority(ResolvedRoleAuthority value){AuthoritySelected=true;throw new InvalidOperationException("Forbidden creation must be refused before selecting authority or opening a database transaction.");}
    }
    private sealed class NeverAudit : IAuditWriter
    {
        public Task WriteAsync(string module,string action,string entityName,string entityId,object? before,object? after,CancellationToken ct)=>throw new InvalidOperationException("Refused creation must not write business evidence.");
    }
}
