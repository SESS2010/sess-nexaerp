using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SESS.NexaERP.Api.Middleware;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Identity;

namespace SESS.NexaERP.Tests;

public sealed class MirRequesterBoundaryTests
{
    [Theory]
    [InlineData("/api/v1/session/me",true)]
    [InlineData("/api/v1/employee/my-mirs",true)]
    [InlineData("/api/v1/employee/my-mirs/returns",true)]
    [InlineData("/api/v1/employee/my-mirs-other",false)]
    [InlineData("/api/v1/company/profile",false)]
    [InlineData("/api/v1/inventory/items",false)]
    [InlineData("/api/v1/stores/material-issue-requests",false)]
    [InlineData("/api/v1/stores/material-issues",false)]
    [InlineData("/api/v1/rev869a/configuration/employee-identities",false)]
    [InlineData("/api/v1/unknown-future-module",false)]
    public async Task OnlyPortalAndOwnSessionReachHandlersDespiteForgedRoleAndEmployeeHeaders(string path,bool allowed)
    {
        using var services=new ServiceCollection().AddSingleton<IDateTimeProvider>(new Clock()).AddScoped<OperationBusinessDate>().BuildServiceProvider();
        using var scope=services.CreateScope();
        var context=Context(scope.ServiceProvider,path);
        var resolver=new Resolver([MirRequesterPolicy.Role]);
        var reached=false;
        await new EmployeeIdentityResolutionMiddleware(_=>{reached=true;return Task.CompletedTask;}).InvokeAsync(context,resolver);
        Assert.Equal(allowed,reached);
        Assert.Equal(allowed?200:403,context.Response.StatusCode);
        Assert.Equal(new DateOnly(2026,10,17),resolver.SeenDate);
        Assert.NotNull(context.Items[EmployeeIdentityResolutionMiddleware.ResolutionItemKey]);
    }
    [Fact]
    public async Task StillApprovedBusinessRolesKeepTheirExistingNonPortalBoundary()
    {
        using var services=new ServiceCollection().AddSingleton<IDateTimeProvider>(new Clock()).BuildServiceProvider();
        var context=Context(services,"/api/v1/company/profile"); var reached=false;
        await new EmployeeIdentityResolutionMiddleware(_=>{reached=true;return Task.CompletedTask;})
            .InvokeAsync(context,new Resolver([MirRequesterPolicy.Role,"DESIGN_ENGINEER"]));
        Assert.True(reached); Assert.Equal(200,context.Response.StatusCode);
    }
    [Theory]
    [InlineData("TECHNICAL_DIRECTOR")]
    [InlineData("MIR_APPROVER")]
    public async Task AddingRequesterRoleDoesNotBypassExistingApproverMfa(string approverRole)
    {
        using var services=new ServiceCollection().AddSingleton<IDateTimeProvider>(new Clock()).BuildServiceProvider();
        var context=Context(services,"/api/v1/employee/my-mirs"); var reached=false;
        await new EmployeeIdentityResolutionMiddleware(_=>{reached=true;return Task.CompletedTask;})
            .InvokeAsync(context,new Resolver([MirRequesterPolicy.Role,approverRole]));
        Assert.False(reached); Assert.Equal(403,context.Response.StatusCode);
        Assert.Equal("MFA_REQUIRED",context.Items[StandardErrorEnvelopeMiddleware.AuthenticationFailureKey]);
    }
    [Fact]
    public async Task InactiveOrExpiredResolvedIdentityNeverReachesPortal()
    {
        using var services=new ServiceCollection().AddSingleton<IDateTimeProvider>(new Clock()).BuildServiceProvider();
        var context=Context(services,"/api/v1/employee/my-mirs"); var reached=false;
        await new EmployeeIdentityResolutionMiddleware(_=>{reached=true;return Task.CompletedTask;})
            .InvokeAsync(context,new Resolver([],false));
        Assert.False(reached); Assert.Equal(403,context.Response.StatusCode);
        Assert.Equal("EMPLOYEE_ACCESS_NOT_CONFIGURED",context.Items[StandardErrorEnvelopeMiddleware.AuthenticationFailureKey]);
    }
    private static DefaultHttpContext Context(IServiceProvider services,string path)
    {
        var context=new DefaultHttpContext {RequestServices=services};
        context.Request.Path=path; context.Response.Body=new MemoryStream();
        context.User=new ClaimsPrincipal(new ClaimsIdentity([new("iss","https://portal.test/realms/staff"),new("sub","requester"),
            new("organization_id","SESS_PVT_LTD"),new(ClaimTypes.Role,"TECHNICAL_DIRECTOR")],"Fixture"));
        context.Request.Headers["X-Employee-Id"]=Guid.NewGuid().ToString();
        context.Request.Headers["X-Role"]="TECHNICAL_DIRECTOR";
        return context;
    }
    private sealed class Clock:IDateTimeProvider
    { public DateTimeOffset UtcNow=>DateTimeOffset.Parse("2026-10-16T20:00:00Z"); }
    private sealed class Resolver(IReadOnlyList<string> roles,bool success=true):IEmployeeIdentityResolver
    {
        public DateOnly SeenDate {get;private set;}
        public Task<ResolvedEmployeeIdentity> ResolveAsync(string issuer,string subject,string? organizationId,DateOnly onDate,CancellationToken cancellationToken)
        {
            SeenDate=onDate;
            return Task.FromResult(success?new ResolvedEmployeeIdentity(true,Guid.NewGuid(),Guid.NewGuid(),"SESS_PVT_LTD","TEST-REQUESTER",roles,"Resolved"):
                ResolvedEmployeeIdentity.Failed("Inactive or expired requester."));
        }
#if DEBUG
        public Task<ResolvedEmployeeIdentity> ResolveDevelopmentEmployeeAsync(string employeeCode,string? organizationId,DateOnly onDate,CancellationToken cancellationToken)=>
            ResolveAsync("","",organizationId,onDate,cancellationToken);
#endif
    }
}
