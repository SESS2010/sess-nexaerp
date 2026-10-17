using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Purchase;

namespace SESS.NexaERP.Tests;

public sealed class AccountsBackupWorkflowTests
{
    private static readonly Guid Alfa=Guid.NewGuid(),Td=Guid.NewGuid(),Maker=Guid.NewGuid(),Backup=Guid.NewGuid();
    private static PurchaseApprovalWorkflowSnapshot Snapshot(string firstCode="SESS-14",string resolution="DEPARTMENT_MAPPING")
    {
        var unsigned=new PurchaseApprovalWorkflowSnapshot("","SESS_PVT_LTD","DEPARTMENT_THEN_TD",15000,new(2026,10,17),
            [new(1,resolution,Alfa,firstCode,"ACCOUNTS_MANAGER"),new(2,"CONFIGURED_ROLE",Td,"SESS-01","TECHNICAL_DIRECTOR")]);
        var json=JsonSerializer.Serialize(unsigned,new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return unsigned with {Identity=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant()};
    }
    private static NexaErpDbContext Model()=>new(new DbContextOptionsBuilder<NexaErpDbContext>()
        .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
    [Fact]
    public void NamedBackupTakesOnlyFirstAccountsStepWithoutRewritingSnapshotOrPriorHistory()
    {
        using var db=Model();var user=new BackupUser();var service=new EfPurchaseApprovalWorkflowService(db,user);
        var snapshot=Snapshot();var text=service.Serialize(snapshot);
        var result=service.AuthorizeNextStep(text,1,0,Maker,Backup,["ACCOUNTS_MANAGER"]);
        Assert.Equal(Backup,result.ResolvedEmployeeId);Assert.Equal("ACCOUNTS_MANAGER",result.ResolvedRoleCode);
        Assert.Equal(snapshot.Identity,result.SnapshotIdentity);Assert.Equal(1,result.StepNumber);Assert.Equal(2,result.RequiredStepCount);Assert.False(result.CompletesDocument);
        Assert.Equal(text,service.Serialize(service.ReadSnapshot(text)));Assert.Equal(Alfa,service.ReadSnapshot(text).Steps[0].EmployeeId);
        var final=new EfPurchaseApprovalWorkflowService(db).AuthorizeNextStep(text,1,1,Maker,Td,["TECHNICAL_DIRECTOR"],Backup);
        Assert.True(final.CompletesDocument);Assert.Equal(Td,final.ResolvedEmployeeId);
    }
    [Fact]
    public void BackupSelfApprovalAndFinalStepSubstitutionRemainRefused()
    {
        using var db=Model();var service=new EfPurchaseApprovalWorkflowService(db,new BackupUser());var text=service.Serialize(Snapshot());
        Assert.Throws<UnauthorizedAccessException>(()=>service.AuthorizeNextStep(text,1,0,Backup,Backup,["ACCOUNTS_MANAGER"]));
        Assert.Throws<UnauthorizedAccessException>(()=>service.AuthorizeNextStep(text,1,1,Maker,Backup,["TECHNICAL_DIRECTOR"],Alfa));
        Assert.Throws<UnauthorizedAccessException>(()=>new EfPurchaseApprovalWorkflowService(db).AuthorizeNextStep(text,1,1,Maker,Td,["TECHNICAL_DIRECTOR"],Td));
    }
    [Theory]
    [InlineData("HUMAN","SESS-41","SESS_PVT_LTD","FULL")]
    [InlineData("HUMAN_ACCT_BACKUP","SESS-35","SESS_PVT_LTD","FULL")]
    [InlineData("HUMAN_ACCT_BACKUP","SESS-41","SESS_PROPRIETORSHIP","FULL")]
    [InlineData("HUMAN_ACCT_BACKUP","SESS-41","SESS_PVT_LTD","SUPPORT")]
    public void OtherIdentityEmployeeCompanyOrSupportCannotBecomeTheMappedApprover(string type,string code,string company,string assignment)
    {
        using var db=Model();var user=new BackupUser(type,code,company,assignment);var service=new EfPurchaseApprovalWorkflowService(db,user);
        Assert.Throws<UnauthorizedAccessException>(()=>service.AuthorizeNextStep(service.Serialize(Snapshot()),1,0,Maker,Backup,["ACCOUNTS_MANAGER"]));
    }
    [Theory]
    [InlineData("SESS-04","DEPARTMENT_MAPPING")]
    [InlineData("SESS-14","CONFIGURED_ROLE")]
    public void NoSubstitutionForAnotherNamedEmployeeOrConfiguredDirectorStep(string code,string resolution)
    {
        using var db=Model();var service=new EfPurchaseApprovalWorkflowService(db,new BackupUser());
        Assert.Throws<UnauthorizedAccessException>(()=>service.AuthorizeNextStep(service.Serialize(Snapshot(code,resolution)),1,0,Maker,Backup,["ACCOUNTS_MANAGER"]));
    }
    [Fact]
    public void ExistingCallerWithoutTrustedBackupContextCannotBorrowThisException()
    {
        using var db=Model();var service=new EfPurchaseApprovalWorkflowService(db);
        Assert.Throws<UnauthorizedAccessException>(()=>service.AuthorizeNextStep(service.Serialize(Snapshot()),1,0,Maker,Backup,["ACCOUNTS_MANAGER"]));
        var normal=service.AuthorizeNextStep(service.Serialize(Snapshot()),1,0,Maker,Alfa,["ACCOUNTS_MANAGER"]);
        Assert.Equal(Alfa,normal.ResolvedEmployeeId);
    }
    private sealed class BackupUser(string type="HUMAN_ACCT_BACKUP",string code="SESS-41",string company="SESS_PVT_LTD",string assignment="FULL") : ICurrentUser
    {
        public string IdentityAuthorityType=>type;public string EmployeeCode=>code;public string OrganizationId=>company;
        public bool IsAuthenticated=>true;public Guid? EmployeeId=>Backup;public string LoginId=>"accounts-subject";public string RoleCode=>"ACCOUNTS_MANAGER";
        public string IdentityIssuer=>"https://accounts.test/realms/approvers";public string IdentitySubject=>LoginId;
        public IReadOnlyList<EffectiveRoleAssignment> EffectiveRoleAssignments {get;}=[new(Guid.NewGuid(),"ACCOUNTS_MANAGER",assignment)];
    }
}
