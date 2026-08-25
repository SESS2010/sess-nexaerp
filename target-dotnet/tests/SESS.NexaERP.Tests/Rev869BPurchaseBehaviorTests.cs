using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SESS.NexaERP.Api.Endpoints;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.Authorization;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Purchase;

namespace SESS.NexaERP.Tests;

public sealed class Rev869BPurchaseBehaviorTests
{
    [Fact]
    public void AuthoritativeCommercialPipelineExecutesRequiredVectors()
    {
        Assert.Equal(100m, Rev869BCommercialCalculator.Calculate(new(2m, 50m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 6)).TotalPayableValue);
        Assert.Equal(118m, Rev869BCommercialCalculator.Calculate(new(1m, 100m, 10m, 5m, 5m, 0m, 0m, 9m, 9m, 0m, 0m, 0m, 6)).TotalPayableValue);
        var intra = Rev869BCommercialCalculator.Calculate(new(1m, 100m, 0m, 0m, 0m, 0m, 0m, 9m, 9m, 0m, 0m, 0m, 6));
        Assert.Equal((9m, 9m, 0m), (intra.CgstValue, intra.SgstValue, intra.IgstValue));
        var inter = Rev869BCommercialCalculator.Calculate(new(1m, 100m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 18m, 0m, 0m, 6));
        Assert.Equal((0m, 0m, 18m), (inter.CgstValue, inter.SgstValue, inter.IgstValue));
        Assert.Equal(0.333333m, Rev869BCommercialCalculator.Calculate(new(1m, 0.333333m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 6)).TotalPayableValue);
        Assert.Throws<InvalidOperationException>(() => Rev869BCommercialCalculator.Calculate(new(1m, 0.3333334m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 6)));
        Assert.Equal(Rev869BCommercialCalculator.MaximumSupportedValue, Rev869BCommercialCalculator.Calculate(new(1m, Rev869BCommercialCalculator.MaximumSupportedValue, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 6)).TotalPayableValue);
        Assert.Throws<InvalidOperationException>(() => Rev869BCommercialCalculator.Calculate(new(-1m, 1m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 6)));
        Assert.Throws<InvalidOperationException>(() => Rev869BCommercialCalculator.Calculate(new(Rev869BCommercialCalculator.MaximumSupportedValue, 2m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 6)));
    }

    [Fact]
    public void CanonicalIdempotencyFingerprintBindsOrganizationOperationKeyPayloadAndVersions()
    {
        var first = Rev869BIdempotencyFingerprint.Create("SESS", "SubmitPO", "key-1", new { Number = "PO-1", Version = 3u, Remarks = " revise " });
        var same = Rev869BIdempotencyFingerprint.Create(" SESS ", "SubmitPO", " key-1 ", new { Remarks = "revise", Version = 3u, Number = "PO-1" });
        var changed = Rev869BIdempotencyFingerprint.Create("SESS", "SubmitPO", "key-1", new { Number = "PO-1", Version = 4u, Remarks = "revise" });
        Assert.Equal(first, same);
        Assert.NotEqual(first, changed);
        Assert.True(Rev869BIdempotencyFingerprint.SameCommand(first, "SESS", "SubmitPO", "key-1"));
        Assert.False(Rev869BIdempotencyFingerprint.SameCommand(first, "OTHER", "SubmitPO", "key-1"));
        var ordered = Rev869BIdempotencyFingerprint.Create("SESS", "CreateRFQ", "key-lines", new { Lines = new[] { new { Id = 2, Quantity = 1m }, new { Id = 1, Quantity = 3m } } });
        var reordered = Rev869BIdempotencyFingerprint.Create("SESS", "CreateRFQ", "key-lines", new { Lines = new[] { new { Quantity = 3m, Id = 1 }, new { Quantity = 1m, Id = 2 } } });
        Assert.Equal(ordered, reordered);
    }
    [Fact]
    public void CommercialCalculationUsesTaxableChargesDiscountTaxAndOverflowGuards()
    {
        var result = Rev869BCommercialCalculator.Calculate(new(3m, 100m, 10m, 2m, 3m, 4m, 5m, 9m, 9m, 0m, 1m, 0.005m, 2));
        Assert.Equal(new Rev869BCommercialBreakdown(304m, 10m, 27.36m, 27.36m, 0m, 3.04m, 2m, 3m, 4m, 5m, 0.005m, 361.77m)
        { GrossAmount = 300m, AssessableValue = 314m }, result);
        Assert.Throws<InvalidOperationException>(() => Rev869BCommercialCalculator.Calculate(
            new(Rev869BCommercialCalculator.MaximumSupportedValue, 2m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 6)));
        Assert.Throws<InvalidOperationException>(() => Rev869BCommercialCalculator.Add(
            Rev869BCommercialCalculator.MaximumSupportedValue, 0.000001m));
    }

    [Fact]
    public void DecimalBoundariesRejectInvalidScaleAndReconcileGstAndPayableExactly()
    {
        var accepted = Rev869BCommercialCalculator.Calculate(new(1.000000m, 100.000000m, 1.000000m, 2.000000m, 3.000000m, 4.000000m, 5.000000m, 9.000000m, 9.000000m, 0.000000m, 1.000000m, -0.000001m, 6)
        { HeaderDiscountValue = 1.000000m, CurrencyCode = "INR", ExchangeRate = 1.000000m });
        Assert.Equal(112m, accepted.TaxableValue);
        Assert.Equal(10.08m, accepted.CgstValue);
        Assert.Equal(10.08m, accepted.SgstValue);
        Assert.Equal(1.12m, accepted.CessValue);
        Assert.Equal(133.279999m, accepted.TotalPayableValue);

        Assert.Throws<InvalidOperationException>(() => Rev869BCommercialCalculator.Calculate(new(1m, 1.0000001m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 6)));
        Assert.Throws<InvalidOperationException>(() => Rev869BCommercialCalculator.Calculate(new(1m, 1m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, -0.0000001m, 6)));
        Assert.Throws<InvalidOperationException>(() => Rev869BCommercialCalculator.Calculate(new(1m, 1m, 0m, 0m, 0m, 0m, 0m, 9.0000001m, 9m, 0m, 0m, 0m, 6)));
        Assert.Throws<InvalidOperationException>(() => Rev869BCommercialCalculator.Ensure(Rev869BCommercialCalculator.MaximumSupportedValue + 0.000001m, "boundary"));
        Assert.Throws<InvalidOperationException>(() => Rev869BCommercialCalculator.Calculate(new(Rev869BCommercialCalculator.MaximumSupportedValue, 1.000001m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 6)));
    }

    [Fact]
    public async Task SevenDecimalInputMapsToHttp400()
    {
        var services = new ServiceCollection().AddSingleton<IAuditWriter>(new CapturingAudit()).AddSingleton<ICurrentUser>(new TestCurrentUser(true)).BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = services };
        var result = await Rev869BPurchaseEndpoints.Run(() =>
        {
            Rev869BCommercialCalculator.Calculate(new(1m, 1.0000001m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 6));
            return Task.FromResult(new Rev869BDocumentResult(Guid.NewGuid(), "never", "never", 0));
        }, http, CancellationToken.None);
        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
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

    [Fact]
    public void PreApprovalSnapshotReconciliationDoesNotRequireApprovedStatusButStillFailsClosed()
    {
        var po = ValidPurchaseOrder();
        po.Status = Rev869BStatuses.Draft;
        Rev869BPurchaseOrderSnapshot.RequireComplete(po, requireApproved: false);
        Assert.Throws<InvalidOperationException>(() => Rev869BPurchaseOrderSnapshot.RequireComplete(po));
        po.ApprovalPolicySnapshotJson = "{";
        Assert.Throws<InvalidOperationException>(() => Rev869BPurchaseOrderSnapshot.RequireComplete(po, requireApproved: false));
    }

    [Fact]
    public async Task MappedAttachmentEndpointExecutesPermissionDenialAndAwaitsAudit()
    {
        var audit = new CapturingAudit();
        var noConnect = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql("Host=127.0.0.1;Port=1;Database=never_connect;Username=never_connect").Options);
        var services = new ServiceCollection().AddRouting().AddLogging().AddSingleton<IAuthenticationService>(new TestAuthenticationService()).AddSingleton<IAuditWriter>(audit)
            .AddSingleton<ICurrentUser>(new TestCurrentUser(true)).AddSingleton<IRecordScopeAuthorizer>(new AllowingScope())
            .AddSingleton<IPagePermissionService>(new DenyingPermission()).AddSingleton(noConnect).BuildServiceProvider();
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddScoped<IRev869BPurchaseService>(_ => null!).AddScoped<NexaErpDbContext>(_ => null!)
            .AddScoped<ICurrentUser>(_ => null!).AddScoped<IRecordScopeAuthorizer>(_ => null!)
            .AddScoped<IPagePermissionService>(_ => null!).AddScoped<IAuditWriter>(_ => null!);
        var app = builder.Build();
        app.MapRev869BPurchaseEndpoints();
        var endpoint = ((IEndpointRouteBuilder)app).DataSources.SelectMany(x => x.Endpoints).OfType<RouteEndpoint>()
            .Single(x => x.RoutePattern.RawText == "/api/v1/purchase/quotations/{number}/attachment");
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Method = "GET"; context.Request.Path = "/api/v1/purchase/quotations/VQ-1/attachment";
        context.Request.RouteValues["number"] = "VQ-1"; context.Response.Body = new MemoryStream();
        var requestDelegate = Assert.IsType<RequestDelegate>(endpoint.RequestDelegate);
        await requestDelegate(context);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.True(audit.Count >= 1);
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
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => purchaseRole.CreateRfqAsync(invalid, CancellationToken.None));
        var purchaseExecutive = Service(new ServiceUser(true, "PURCHASE_EXECUTIVE"));
        await Assert.ThrowsAsync<Rev869BValidationException>(() => purchaseExecutive.CreateRfqAsync(invalid, CancellationToken.None));
        var missingSingleSourceReason = invalid with { IdempotencyKey = "key", IsSingleSource = true, Lines = [new(Guid.NewGuid(), 1m)] };
        await Assert.ThrowsAsync<InvalidOperationException>(() => purchaseExecutive.CreateRfqAsync(missingSingleSourceReason, CancellationToken.None));
    }

    private static EfRev869BPurchaseService Service(ICurrentUser user)
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=never_connect;Username=never_connect")
            .Options;
        return new EfRev869BPurchaseService(new NexaErpDbContext(options), user, null!, null!, null!, new CapturingAudit());
    }

    private static PurchaseOrder ValidPurchaseOrder()
    {
        var organization = "SESS"; var comparisonId = Guid.NewGuid(); var vendorId = Guid.NewGuid(); var received = DateTimeOffset.Parse("2026-08-11T00:00:00Z");
        var input = new Rev869BCommercialInput(1m, 100m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 6);
        var result = Rev869BCommercialCalculator.Calculate(input);
        var tax = new Rev869BTaxRuleSnapshot(Guid.NewGuid(), organization, TaxJurisdictions.IndiaGst, "0000", "INTRASTATE", "TN", "TN", "REGULAR", 0m, 0m, 0m, 0m, 0m, false, false, "INR", 6, new DateOnly(2026, 1, 1), null, MasterApprovalStatuses.Approved, true);
        var commercial = new Rev869BPoCommercialSnapshot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), comparisonId, vendorId, organization, "{\"eligible\":true}", "vendor/quote.pdf", new string('A', 64), Rev869BApprovalRoutes.Manager, received.AddDays(1), received, input, result);
        commercial = commercial with { QuotationRevision = 1, ItemId = Guid.NewGuid(), Quantity = 1m, Uom = string.Concat('E','A'), CurrencyCode = string.Concat('I','N','R'), ExchangeRate = 1m };
        var policyJson = System.Text.Json.JsonSerializer.Serialize(new { organizationId = organization, routeCode = Rev869BApprovalRoutes.Manager, approvalValue = 100m, effectiveOn = new DateOnly(2026, 8, 11) }, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        return new PurchaseOrder { OrganizationId = organization, CommercialComparisonId = comparisonId, VendorId = vendorId,
            Status = Rev869BStatuses.Approved, ApprovalRoute = Rev869BApprovalRoutes.Manager, PaymentTermsSnapshot = "30 days", DeliveryTermsSnapshot = "Delivered", WarrantyTermsSnapshot = "12 months",
            TaxableValue = 100m, TotalPayableValue = 100m, ApprovalPolicySnapshotJson = policyJson, Lines = [new PurchaseOrderLine { ItemId = commercial.ItemId, UomSnapshot = commercial.Uom, OrderedQuantity = 1m, ApprovedOutstandingQuantitySnapshot = 1m,
                CommercialSnapshotJson = System.Text.Json.JsonSerializer.Serialize(commercial, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)),
                TaxRuleSnapshotJson = System.Text.Json.JsonSerializer.Serialize(tax, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)), TotalPayableValue = 100m }] };
    }

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

    private sealed class AllowingScope : IRecordScopeAuthorizer
    {
        public Task<RecordScopeDecision> AuthorizeAnyAsync(Guid employeeId, string roleCode, string organizationId, DateOnly onDate, CancellationToken cancellationToken) => Task.FromResult(new RecordScopeDecision(true, "allowed"));
        public Task<RecordScopeDecision> AuthorizeAsync(Guid employeeId, string roleCode, RecordScopeTarget target, DateOnly onDate, CancellationToken cancellationToken) => Task.FromResult(new RecordScopeDecision(true, "allowed"));
    }
    private sealed class DenyingPermission : IPagePermissionService
    {
        public Task<bool> HasPermissionAsync(IReadOnlyCollection<string> roleCodes, string pageKey, string permission, CancellationToken cancellationToken) => Task.FromResult(false);
    }
    private sealed class TestAuthenticationService : IAuthenticationService
    {
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) => Task.FromResult(AuthenticateResult.NoResult());
        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) { context.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; }
        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) { context.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; }
        public Task SignInAsync(HttpContext context, string? scheme, System.Security.Claims.ClaimsPrincipal principal, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
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
