using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Stores;

namespace SESS.NexaERP.Tests;

public sealed class MirDecisionOnlyServiceTests
{
    [Fact]
    public async Task DecisionOnlyRoleCannotCreateMirOrReturnEvenWhenServiceIsCalledDirectly()
    {
        using var db=new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var service=new EfMaterialIssueService(db,new DecisionActor(),new NeverAudit());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.CreateRequestAsync(new CreateMaterialIssueRequest("DEMO","CONSUMABLE_OFFICE","DEPARTMENT",null,null,null,null,
            "Department",Guid.NewGuid(),new DateOnly(2026,10,17),[],"decision-only-create-refused"),default));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.CreateReturnAsync(Guid.NewGuid(),new CreateMaterialReturn(DateTimeOffset.UtcNow,[],"decision-only-return-refused"),default));
    }
    private sealed class DecisionActor : ICurrentUser
    {
        public string LoginId=>"decision-test";public string RoleCode=>MirRequesterPolicy.ApproverRole;public string OrganizationId=>"SESS_PVT_LTD";public bool IsAuthenticated=>true;
        public Guid? EmployeeId {get;}=Guid.NewGuid();
        public IReadOnlyList<EffectiveRoleAssignment> EffectiveRoleAssignments {get;}=[new(Guid.NewGuid(),MirRequesterPolicy.ApproverRole,"FULL")];
        public void SetResolvedRoleAuthority(ResolvedRoleAuthority value)=>throw new InvalidOperationException("Decision-only authority must be rejected before it is selected for creation.");
    }
    private sealed class NeverAudit : IAuditWriter
    {
        public Task WriteAsync(string module,string action,string entityName,string entityId,object? before,object? after,CancellationToken ct)=>throw new InvalidOperationException("Refusal must not persist anything.");
    }
}
