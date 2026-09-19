using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using Xunit;
namespace SESS.NexaERP.Tests;

public sealed class VendorManualAssessmentAuthorityTests
{
    [Theory]
    [InlineData(true,true,true,true,false,"QC_MANAGER")]
    [InlineData(true,true,false,true,false,"PRODUCTION_MANAGER")]
    [InlineData(true,true,false,false,false,null)]
    [InlineData(true,false,false,true,false,null)]
    [InlineData(false,true,true,true,false,"PRODUCTION_MANAGER")]
    [InlineData(false,false,true,true,true,null)]
    [InlineData(true,true,false,false,true,"QC_MANAGER")]
    [InlineData(false,true,false,false,true,"PRODUCTION_MANAGER")]
    [InlineData(true,false,true,false,false,"QC_MANAGER")]
    [InlineData(false,true,true,false,false,null)]
    public async Task ReceiptScopeRequiresTheSelectedRolesOwnPageGrant(bool qc, bool pm, bool qcGrant,
        bool pmGrant, bool employeeGrant, string? expected)
    {
        var assignments=new List<EffectiveRoleAssignment>();
        if(qc)assignments.Add(new(Guid.NewGuid(),"QC_MANAGER","FULL"));
        if(pm)assignments.Add(new(Guid.NewGuid(),"PRODUCTION_MANAGER","TEMPORARY"));
        var user=new Actor(assignments);
        var permissions=new Grants(qcGrant,pmGrant,employeeGrant);
        foreach(var operation in new[]{"view","create"})
        {
            if(expected is null)
            {
                await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>VendorManualAssessmentAuthority.RequireRoleAsync(user,permissions,operation,default));
                Assert.Null(user.ResolvedRoleAssignmentId);
            }
            else
            {
                Assert.Equal(expected,await VendorManualAssessmentAuthority.RequireRoleAsync(user,permissions,operation,default));
                Assert.Equal(assignments.Single(x=>x.RoleCode==expected).AssignmentId,user.ResolvedRoleAssignmentId);
                Assert.Equal(expected,user.RoleCode);
            }
        }
    }
    [Fact]
    public async Task EmptyAssignmentIdentityCannotAcquireUnrestrictedQcScope()
    {
        var manager=new EffectiveRoleAssignment(Guid.NewGuid(),"PRODUCTION_MANAGER","TEMPORARY");
        var user=new Actor([new(Guid.Empty,"QC_MANAGER","FULL"),manager]);
        Assert.Equal("PRODUCTION_MANAGER",await VendorManualAssessmentAuthority.RequireRoleAsync(user,new Grants(true,true,false),"create",default));
        Assert.Equal(manager.AssignmentId,user.ResolvedRoleAssignmentId);
    }
    private sealed class Actor(IReadOnlyList<EffectiveRoleAssignment> assignments):ICurrentUser
    {
        private ResolvedRoleAuthority? authority;
        public string LoginId=>"authority-regression";
        public string RoleCode=>authority?.RoleCode??"none";
        public string OrganizationId=>"SESS_PRIVATE_LIMITED";
        public bool IsAuthenticated=>true;
        public Guid? EmployeeId{get;}=Guid.NewGuid();
        public IReadOnlyList<EffectiveRoleAssignment> EffectiveRoleAssignments=>assignments;
        public Guid? ResolvedRoleAssignmentId=>authority?.AssignmentId;
        public string? ResolvedRoleAssignmentType=>authority?.AssignmentType;
        public void SetResolvedRoleAuthority(ResolvedRoleAuthority value)=>authority=value;
    }
    private sealed class Grants(bool qc,bool pm,bool employee):IPagePermissionService
    {
        public Task<bool> HasPermissionAsync(IReadOnlyCollection<string> roles,string page,string operation,CancellationToken ct)
        {
            Assert.Equal("quality.vendor-manual-assessments",page);
            Assert.Contains(operation,new[]{"view","create"});
            return Task.FromResult(Assert.Single(roles) switch {"QC_MANAGER"=>qc,"PRODUCTION_MANAGER"=>pm,_=>false});
        }
        public Task<bool> HasEmployeePermissionAsync(string organization,Guid employeeId,string page,string operation,CancellationToken ct)
            =>Task.FromResult(employee);
    }
}
