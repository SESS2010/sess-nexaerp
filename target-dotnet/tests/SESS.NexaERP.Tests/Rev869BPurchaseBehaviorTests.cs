using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SESS.NexaERP.Api.Endpoints;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Purchase;

namespace SESS.NexaERP.Tests;

public sealed class Rev869BPurchaseBehaviorTests
{
    [Fact]
    public void CommercialCalculationUsesTaxableChargesDiscountTaxAndOverflowGuards()
    {
        var result = Rev869BCommercialCalculator.Calculate(new(3m, 100m, 10m, 2m, 3m, 4m, 5m, 9m, 9m, 0m, 1m, 0.005m, 2));
        Assert.Equal(new Rev869BCommercialBreakdown(304m, 10m, 27.36m, 27.36m, 0m, 3.04m, 2m, 3m, 4m, 5m, 0.005m, 361.77m), result);
        Assert.Throws<InvalidOperationException>(() => Rev869BCommercialCalculator.Calculate(
            new(Rev869BCommercialCalculator.MaximumSupportedValue, 2m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 6)));
        Assert.Throws<InvalidOperationException>(() => Rev869BCommercialCalculator.Add(
            Rev869BCommercialCalculator.MaximumSupportedValue, 0.000001m));
    }

    [Fact]
    public void PreIssueSnapshotMustBeApprovedCompleteAndReconciled()
    {
        var po = ValidPurchaseOrder();
        Rev869BPurchaseOrderSnapshot.RequireComplete(po);
        po.TotalPayableValue = 99m;
        Assert.Throws<InvalidOperationException>(() => Rev869BPurchaseOrderSnapshot.RequireComplete(po));
        po.TotalPayableValue = 100m;
        po.Lines[0].TaxRuleSnapshotJson = "{}";
        Assert.Throws<InvalidOperationException>(() => Rev869BPurchaseOrderSnapshot.RequireComplete(po));
    }

    [Theory]
    [InlineData("validation", 400, 0)]
    [InlineData("overflow", 400, 0)]
    [InlineData("missing", 404, 1)]
    [InlineData("concurrency", 409, 1)]
    [InlineData("idempotency", 409, 1)]
    [InlineData("forbidden", 403, 1)]
    [InlineData("unauthenticated", 401, 1)]
    public async Task ApiFailureSemanticsExecuteAndSecurityFailuresAreAudited(string scenario, int expectedStatus, int expectedAudits)
    {
        var authenticated = scenario != "unauthenticated";
        var audit = new CapturingAudit();
        var services = new ServiceCollection()
            .AddSingleton<IAuditWriter>(audit)
            .AddSingleton<ICurrentUser>(new TestCurrentUser(authenticated))
            .BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = services };
        http.Request.Path = "/api/v1/purchase/test";
        Exception failure = scenario switch
        {
            "validation" => new Rev869BValidationException("invalid"),
            "overflow" => new InvalidOperationException("overflow"),
            "missing" => new Rev869BNotFoundException("missing"),
            "concurrency" => new DbUpdateConcurrencyException("stale"),
            "idempotency" => new Rev869BConflictException("key reused"),
            _ => new UnauthorizedAccessException("denied")
        };
        var result = await Rev869BPurchaseEndpoints.Run(
            () => Task.FromException<Rev869BDocumentResult>(failure), http, CancellationToken.None);
        var actualStatus = result.GetType().Name switch
        {
            "ForbidHttpResult" => StatusCodes.Status403Forbidden,
            "UnauthorizedHttpResult" => StatusCodes.Status401Unauthorized,
            _ => Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode
        };
        Assert.Equal(expectedStatus, actualStatus);
        Assert.Equal(expectedAudits, audit.Count);
    }

    [Fact]
    public async Task AuditFailureCannotSilentlyPermitAnAuthorizationDenial()
    {
        var services = new ServiceCollection()
            .AddSingleton<IAuditWriter>(new FailingAudit())
            .AddSingleton<ICurrentUser>(new TestCurrentUser(true))
            .BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = services };
        await Assert.ThrowsAsync<InvalidOperationException>(() => Rev869BPurchaseEndpoints.Run(
            () => Task.FromException<Rev869BDocumentResult>(new UnauthorizedAccessException("denied")), http, CancellationToken.None));
    }

    [Fact]
    public async Task ServiceExecutesFailClosedIdentityRoleAndMalformedRequestBranchesWithoutDatabaseAccess()
    {
        var invalid = new Rev869BCreateRfqRequest(DateTimeOffset.UtcNow.AddHours(1), "INR", false, null, "", []);
        var missingIdentity = Service(new ServiceUser(false, "none"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => missingIdentity.CreateRfqAsync(invalid, CancellationToken.None));
        var wrongRole = Service(new ServiceUser(true, "STORES_EXECUTIVE"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => wrongRole.CreateRfqAsync(invalid, CancellationToken.None));
        var purchaseRole = Service(new ServiceUser(true, "PURCHASE_MANAGER"));
        await Assert.ThrowsAsync<Rev869BValidationException>(() => purchaseRole.CreateRfqAsync(invalid, CancellationToken.None));
        var missingSingleSourceReason = invalid with { IdempotencyKey = "key", IsSingleSource = true, Lines = [new(Guid.NewGuid(), 1m)] };
        await Assert.ThrowsAsync<InvalidOperationException>(() => purchaseRole.CreateRfqAsync(missingSingleSourceReason, CancellationToken.None));
    }

    private static EfRev869BPurchaseService Service(ICurrentUser user)
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=never_connect;Username=never_connect")
            .Options;
        return new EfRev869BPurchaseService(new NexaErpDbContext(options), user, null!, null!, null!, new CapturingAudit());
    }

    private static PurchaseOrder ValidPurchaseOrder() => new()
    {
        Status = Rev869BStatuses.Approved, ApprovalRoute = Rev869BApprovalRoutes.Manager,
        PaymentTermsSnapshot = "30 days", DeliveryTermsSnapshot = "Delivered", WarrantyTermsSnapshot = "12 months",
        TaxableValue = 100m, TotalPayableValue = 100m,
        Lines = [new PurchaseOrderLine
        {
            OrderedQuantity = 1m, ApprovedOutstandingQuantitySnapshot = 1m,
            CommercialSnapshotJson = "{\"total\":100}", TaxRuleSnapshotJson = "{\"rate\":0}", TotalPayableValue = 100m
        }]
    };

    private sealed class CapturingAudit : IAuditWriter
    {
        public int Count { get; private set; }
        public Task WriteAsync(string module, string action, string entityName, string entityId, object? before, object? after, CancellationToken cancellationToken)
        { Count++; return Task.CompletedTask; }
    }

    private sealed class FailingAudit : IAuditWriter
    {
        public Task WriteAsync(string module, string action, string entityName, string entityId, object? before, object? after, CancellationToken cancellationToken)
            => throw new InvalidOperationException("audit unavailable");
    }

    private sealed record ServiceUser(bool IsAuthenticated, string RoleCode) : ICurrentUser
    {
        public string LoginId => IsAuthenticated ? "service-tester" : "";
        public string? OrganizationId => IsAuthenticated ? "SESS" : null;
        public Guid? EmployeeId => IsAuthenticated ? Guid.Parse("10000000-0000-0000-0000-000000000002") : null;
    }

    private sealed record TestCurrentUser(bool IsAuthenticated) : ICurrentUser
    {
        public string LoginId => IsAuthenticated ? "tester" : "";
        public string RoleCode => IsAuthenticated ? "PURCHASE_MANAGER" : "none";
        public string? OrganizationId => IsAuthenticated ? "SESS" : null;
        public Guid? EmployeeId => IsAuthenticated ? Guid.Parse("10000000-0000-0000-0000-000000000001") : null;
    }
}
