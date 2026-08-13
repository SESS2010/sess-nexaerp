using System.Data;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using SESS.NexaERP.Api.Endpoints;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Application.Rev869A;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Audit;
using SESS.NexaERP.Infrastructure.Masters;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Purchase;

namespace SESS.NexaERP.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class Rev869BPostgresSerialCollection
{
    public const string Name = "REV869B PostgreSQL serial";
}

// Compiled now and intentionally NOT RUN. The exact opt-in and database identity checks remain
// mandatory. Each fixture owns an outer serializable transaction; the real service participates
// in it and the fixture proves rollback by checking that every test-owned marker returns to zero.
[Collection(Rev869BPostgresSerialCollection.Name)]
public sealed class Rev869BPostgresApplicationBehaviorTests
{
    [Fact]
    public async Task RealServiceTransactionPersistsParentChildHistoryAndAudit()
    {
        await using var fixture = await OwnedRfqFixture.CreateAsync("service-success", useAmbientTransaction: false);
        var result = await fixture.Service().CreateRfqAsync(fixture.Request("same-command"), default);

        Assert.Equal(Rev869BStatuses.Draft, result.Status);
        await using var verifier = await fixture.OpenIndependentContextAsync();
        Assert.Equal(1, await verifier.RequestForQuotations.AsNoTracking().CountAsync(x => x.Id == result.Id));
        Assert.Equal(1, await verifier.RequestForQuotationLines.AsNoTracking().CountAsync(x => x.RequestForQuotationId == result.Id));
        Assert.Equal(1, await verifier.PurchaseTransactionStatusHistories.AsNoTracking().CountAsync(x => x.EntityId == result.Id));
        Assert.Equal(1, await verifier.AuditLogs.AsNoTracking().CountAsync(x => x.EntityId == result.Id.ToString() && x.Action == "CreateRFQ"));
    }

    [Fact]
    public async Task RealServiceFailureAfterWritesRollsBackEveryAffectedRelation()
    {
        await using var fixture = await OwnedRfqFixture.CreateAsync("audit-rollback", useAmbientTransaction: false);
        var before = await fixture.CaptureOwnedStateFromIndependentContextAsync();
        await Assert.ThrowsAsync<InjectedAuditFailure>(() =>
            fixture.Service(new FailingAuditWriter()).CreateRfqAsync(fixture.Request("failing-command"), default));
        Assert.Equal(before, await fixture.CaptureOwnedStateFromIndependentContextAsync());
    }

    [Fact]
    public async Task RealServiceIdempotentReplayReturnsAuthoritativeOriginalWithoutDuplicates()
    {
        await using var fixture = await OwnedRfqFixture.CreateAsync("idempotent-replay", useAmbientTransaction: false);
        var command = fixture.Request("same-command");
        var first = await fixture.Service().CreateRfqAsync(command, default);
        fixture.Db.ChangeTracker.Clear();
        var second = await fixture.Service().CreateRfqAsync(command, default);

        Assert.Equal(first, second);
        await using var verifier = await fixture.OpenIndependentContextAsync();
        Assert.Equal(1, await verifier.RequestForQuotations.AsNoTracking().CountAsync(x => x.OrganizationId == fixture.Organization));
        Assert.Equal(1, await verifier.RequestForQuotationLines.AsNoTracking().CountAsync(x => x.RequestForQuotation!.OrganizationId == fixture.Organization));
        Assert.Equal(1, await verifier.PurchaseTransactionStatusHistories.AsNoTracking().CountAsync(x => x.OrganizationId == fixture.Organization && x.EntityType == "RFQ"));
        Assert.Equal(1, await verifier.AuditLogs.AsNoTracking().CountAsync(x => x.CreatedBy == fixture.Marker && x.Action == "CreateRFQ"));
    }

    [Fact]
    public async Task RealProtectedServiceDenialHasNoBusinessMutationAndNoCrossOrganizationDisclosure()
    {
        await using var fixture = await OwnedRfqFixture.CreateAsync("scope-denial", useAmbientTransaction: false);
        var before = await fixture.CaptureOwnedStateFromIndependentContextAsync();
        var denied = fixture.Service(scopes: new DenyingScope());

        var error = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            denied.CreateRfqAsync(fixture.Request("denied-command"), default));

        Assert.DoesNotContain(fixture.HandoffId.ToString(), error.Message, StringComparison.OrdinalIgnoreCase);
        var after = await fixture.CaptureOwnedStateFromIndependentContextAsync();
        Assert.Equal(before.Rfqs, after.Rfqs);
        Assert.Equal(before.RfqLines, after.RfqLines);
        Assert.Equal(before.StatusHistories, after.StatusHistories);
        Assert.Equal(before.Audits + 1, after.Audits);
        await using var verifier = await fixture.OpenIndependentContextAsync();
        Assert.Equal(1, await verifier.AuditLogs.AsNoTracking().CountAsync(x => x.CreatedBy == fixture.Marker && x.Action == "Denied" && x.Result == "Failure"));
    }

    [Fact]
    public async Task RealProtectedServicePropagatesAuditWriterFailureWithoutFalseSuccess()
    {
        await using var fixture = await OwnedRfqFixture.CreateAsync("audit-propagation", useAmbientTransaction: false);
        var before = await fixture.CaptureOwnedStateFromIndependentContextAsync();
        await Assert.ThrowsAsync<InjectedAuditFailure>(() => fixture.Service(new FailingAuditWriter())
            .CreateRfqAsync(fixture.Request("audit-failure-command"), default));
        Assert.Equal(before, await fixture.CaptureOwnedStateFromIndependentContextAsync());
    }

    [Fact]
    public async Task TwoIndependentDbContextsConnectionsAndServicesProduceOneAuthoritativeWinner()
    {
        await using var fixture = await OwnedRfqFixture.CreateAsync("concurrent-services", useAmbientTransaction: false);
        var before = await fixture.CaptureOwnedStateFromIndependentContextAsync();
        await using var firstDb = await fixture.OpenIndependentContextAsync();
        await using var secondDb = await fixture.OpenIndependentContextAsync();
        Assert.NotSame(firstDb, secondDb);
        Assert.NotSame(firstDb.Database.GetDbConnection(), secondDb.Database.GetDbConnection());
        var coordinatedStart = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<Rev869BDocumentResult> RunAsync(NexaErpDbContext context)
        {
            await coordinatedStart.Task;
            return await fixture.Service(context).CreateRfqAsync(fixture.Request("collision-key"), default);
        }
        var first = RunAsync(firstDb);
        var second = RunAsync(secondDb);
        coordinatedStart.SetResult();
        var results = await Task.WhenAll(first, second);
        Assert.Equal(results[0].Id, results[1].Id);
        await using var verifier = await fixture.OpenIndependentContextAsync();
        Assert.Equal(1, await verifier.RequestForQuotations.CountAsync(x => x.OrganizationId == fixture.Organization));
        Assert.Equal(1, await verifier.RequestForQuotationLines.CountAsync(x => x.RequestForQuotation!.OrganizationId == fixture.Organization));
        Assert.Equal(1, await verifier.PurchaseTransactionStatusHistories.CountAsync(x => x.OrganizationId == fixture.Organization && x.EntityType == "RFQ" && x.Action == "Create"));
        Assert.Equal(1, await verifier.AuditLogs.CountAsync(x => x.CreatedBy == fixture.Marker && x.Action == "CreateRFQ"));
        Assert.Equal(0, await verifier.RfqVendorInvitations.CountAsync(x => x.RequestForQuotation!.OrganizationId == fixture.Organization));
        Assert.Equal(0, await verifier.VendorQuotations.CountAsync(x => x.OrganizationId == fixture.Organization));
        Assert.Equal(0, await verifier.CommercialComparisons.CountAsync(x => x.OrganizationId == fixture.Organization));
        Assert.Equal(0, await verifier.PurchaseOrders.CountAsync(x => x.OrganizationId == fixture.Organization));
        await Assert.ThrowsAsync<Rev869BConflictException>(() => fixture.Service(secondDb)
            .CreateRfqAsync(fixture.Request("collision-key") with { CurrencyCode = "USD" }, default));
        Assert.Equal(1, await firstDb.RequestForQuotations.CountAsync(x => x.OrganizationId == fixture.Organization));
        var after = await fixture.CaptureOwnedStateFromIndependentContextAsync();
        Assert.Equal(before.Rfqs + 1, after.Rfqs);
        Assert.Equal(before.RfqLines + 1, after.RfqLines);
        Assert.Equal(before.StatusHistories + 1, after.StatusHistories);
        Assert.Equal(before.Audits + 1, after.Audits);
        Assert.Equal(before.NumberSequences + 1, after.NumberSequences);
        Assert.Equal(before.Invitations, after.Invitations);
        Assert.Equal(before.Quotations, after.Quotations);
        Assert.Equal(before.QuotationLines, after.QuotationLines);
        Assert.Equal(before.TechnicalVerifications, after.TechnicalVerifications);
        Assert.Equal(before.Comparisons, after.Comparisons);
        Assert.Equal(before.ComparisonLines, after.ComparisonLines);
        Assert.Equal(before.PurchaseOrders, after.PurchaseOrders);
        Assert.Equal(before.PurchaseOrderLines, after.PurchaseOrderLines);
        Assert.Equal(before.PoHistories, after.PoHistories);
        Assert.Equal(before.FollowUps, after.FollowUps);
    }

    [Fact]
    public async Task AuthenticatedMappedAspNetEndpointTraversesPermissionScopeServiceAndEf()
    {
        await using var fixture = await OwnedRfqFixture.CreateAsync("mapped-endpoint", useAmbientTransaction: false);
        await Rev869BCompleteGraphSeeder.SeedAsync(fixture.DatabaseConnectionString, "mapped-endpoint-security");
        var directOrganization = Rev869BOwnedPostgresDatabase.Organization;
        var directCreator = await fixture.Db.EmployeeIdentityMappings.AsNoTracking()
            .SingleAsync(x => x.OrganizationId == directOrganization && x.Subject == Rev869BOwnedPostgresDatabase.Login);
        var directVerifier = await fixture.Db.EmployeeIdentityMappings.AsNoTracking()
            .SingleAsync(x => x.OrganizationId == directOrganization && x.Subject == "REV869B-VERIFIER");
        var directApprover = await fixture.Db.EmployeeIdentityMappings.AsNoTracking()
            .SingleAsync(x => x.OrganizationId == directOrganization && x.Subject == "REV869B-APPROVER");
        var directQualification = await fixture.Db.VendorQualifications.AsNoTracking()
            .Include(x => x.Vendor).Include(x => x.ItemCategory)
            .SingleAsync(x => x.OrganizationId == directOrganization);
        var directQuotationNumber = await fixture.Db.VendorQuotations.AsNoTracking()
            .Where(x => x.OrganizationId == directOrganization).Select(x => x.QuotationNumber).SingleAsync();
        var directComparisonNumber = await fixture.Db.CommercialComparisons.AsNoTracking()
            .Where(x => x.OrganizationId == directOrganization).Select(x => x.ComparisonNumber).SingleAsync();
        var user = new FixtureUser(fixture.Marker, Rev869ARoleCodes.PurchaseExecutive, fixture.Organization, fixture.ActorId);
        var port = FreePort();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Test" });
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
        builder.Services.AddRouting();
        builder.Services.AddAuthentication(OwnedAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, OwnedAuthenticationHandler>(OwnedAuthenticationHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<ICurrentUser>(user);
        var scopes = new ToggleScope();
        builder.Services.AddSingleton<IRecordScopeAuthorizer>(scopes);
        var permissions = new TogglePagePermissions();
        builder.Services.AddSingleton<IPagePermissionService>(permissions);
        var pipelineAudit = new ToggleAuditWriter(new EfAuditWriter(fixture.Db, user));
        builder.Services.AddSingleton<IAuditWriter>(pipelineAudit);
        builder.Services.AddSingleton(fixture.Db);
        builder.Services.AddSingleton<IRev869BPurchaseService>(fixture.Service(scopes: scopes, currentUser: user));

        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapRev869BPurchaseEndpoints();
        app.MapRev869AConfigurationEndpoints();
        await app.StartAsync();
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
            var unauthenticated = await client.PostAsJsonAsync("/api/v1/purchase/rfqs", fixture.Request("unauthenticated"));
            Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);
            client.DefaultRequestHeaders.Authorization = new("Owned");
            permissions.Allow = false;
            var forbidden = await client.GetAsync("/api/v1/purchase/quotations/NO-SUCH/attachment");
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
            var forbiddenExport = await client.GetAsync("/api/v1/purchase/comparisons/NO-SUCH/export");
            Assert.Equal(HttpStatusCode.Forbidden, forbiddenExport.StatusCode);
            permissions.Allow = true;
            var bad = await client.PostAsJsonAsync("/api/v1/purchase/rfqs", fixture.Request("bad-request") with { CurrencyCode = "" });
            Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
            var missing = await client.PostAsJsonAsync("/api/v1/purchase/rfqs/NO-SUCH/vendors",
                new Rev869BInviteVendorRequest(Guid.Empty, "missing", 0, "missing"));
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
            var response = await client.PostAsJsonAsync("/api/v1/purchase/rfqs", fixture.Request("mapped-command"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<Rev869BDocumentResult>();
            Assert.NotNull(result);
            Assert.Equal(1, await fixture.Db.RequestForQuotations.CountAsync(x => x.Id == result.Id));
            Assert.Equal(1, await fixture.Db.AuditLogs.CountAsync(x => x.EntityId == result.Id.ToString()));
            var auditBeforeScopeDenial = await fixture.Db.AuditLogs.CountAsync();
            scopes.Allow = false;
            var scopeDenied = await client.GetAsync($"/api/v1/purchase/rfqs/{result.Number}");
            Assert.Equal(HttpStatusCode.Forbidden, scopeDenied.StatusCode);
            Assert.True(await fixture.Db.AuditLogs.CountAsync() > auditBeforeScopeDenial);
            scopes.Allow = true;
            user.OrganizationId = fixture.Organization + "-FOREIGN";
            var crossOrganization = await client.GetAsync($"/api/v1/purchase/rfqs/{result.Number}");
            Assert.Equal(HttpStatusCode.NotFound, crossOrganization.StatusCode);
            user.OrganizationId = fixture.Organization;
            permissions.Allow = false;
            pipelineAudit.Fail = true;
            var auditFailure = await client.GetAsync("/api/v1/purchase/quotations/NO-SUCH/attachment");
            Assert.Equal(HttpStatusCode.InternalServerError, auditFailure.StatusCode);
            pipelineAudit.Fail = false;
            permissions.Allow = true;
            var conflict = await client.PostAsJsonAsync("/api/v1/purchase/rfqs", fixture.Request("mapped-command") with { CurrencyCode = "USD" });
            Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);

            user.OrganizationId = directOrganization;
            user.IdentityIssuer = Rev869BOwnedPostgresDatabase.Issuer;
            user.LoginId = Rev869BOwnedPostgresDatabase.Login;
            user.RoleCode = Rev869ARoleCodes.PurchaseExecutive;
            user.ActorId = directCreator.EmployeeId;
            var qualificationRequest = new CreateVendorQualificationRequest(
                directOrganization, directQualification.Vendor!.VendorCode, directQualification.ItemCategory!.Code,
                "MAPPED-Q", new DateOnly(2027, 1, 1), new DateOnly(2027, 12, 31), "mapped create");
            var badQualification = await client.PostAsJsonAsync("/api/v1/rev869a/configuration/vendor-qualifications",
                qualificationRequest with { Remarks = "" });
            Assert.Equal(HttpStatusCode.BadRequest, badQualification.StatusCode);
            var createdQualification = await client.PostAsJsonAsync("/api/v1/rev869a/configuration/vendor-qualifications", qualificationRequest);
            Assert.Equal(HttpStatusCode.Created, createdQualification.StatusCode);
            using var qualificationJson = JsonDocument.Parse(await createdQualification.Content.ReadAsStringAsync());
            var qualificationId = qualificationJson.RootElement.GetProperty("id").GetGuid();

            var creatorVerify = await client.PostAsJsonAsync(
                $"/api/v1/rev869a/configuration/vendor-qualifications/{qualificationId}/verify",
                new ChangeVendorQualificationLifecycleRequest(0, "creator cannot verify"));
            Assert.Equal(HttpStatusCode.Conflict, creatorVerify.StatusCode);
            user.LoginId = "REV869B-VERIFIER";
            user.RoleCode = Rev869ARoleCodes.TechnicalDirector;
            user.ActorId = directVerifier.EmployeeId;
            var staleVerify = await client.PostAsJsonAsync(
                $"/api/v1/rev869a/configuration/vendor-qualifications/{qualificationId}/verify",
                new ChangeVendorQualificationLifecycleRequest(99, "stale"));
            Assert.Equal(HttpStatusCode.Conflict, staleVerify.StatusCode);
            scopes.Allow = false;
            var scopedVerify = await client.PostAsJsonAsync(
                $"/api/v1/rev869a/configuration/vendor-qualifications/{qualificationId}/verify",
                new ChangeVendorQualificationLifecycleRequest(0, "scope denied"));
            Assert.Equal(HttpStatusCode.Forbidden, scopedVerify.StatusCode);
            scopes.Allow = true;
            var verified = await client.PostAsJsonAsync(
                $"/api/v1/rev869a/configuration/vendor-qualifications/{qualificationId}/verify",
                new ChangeVendorQualificationLifecycleRequest(0, "independent verification"));
            Assert.Equal(HttpStatusCode.OK, verified.StatusCode);
            var verifierApprove = await client.PostAsJsonAsync(
                $"/api/v1/rev869a/configuration/vendor-qualifications/{qualificationId}/approve",
                new ChangeVendorQualificationLifecycleRequest(1, "verifier cannot approve"));
            Assert.Equal(HttpStatusCode.Conflict, verifierApprove.StatusCode);
            user.LoginId = "REV869B-APPROVER";
            user.RoleCode = Rev869ARoleCodes.ManagingDirector;
            user.ActorId = directApprover.EmployeeId;
            var approved = await client.PostAsJsonAsync(
                $"/api/v1/rev869a/configuration/vendor-qualifications/{qualificationId}/approve",
                new ChangeVendorQualificationLifecycleRequest(1, "independent approval"));
            Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
            await using (var qualificationVerifier = await fixture.OpenIndependentContextAsync())
            {
                var persisted = await qualificationVerifier.VendorQualifications.AsNoTracking().SingleAsync(x => x.Id == qualificationId);
                Assert.Equal(MasterApprovalStatuses.Verified, persisted.VerificationStatus);
                Assert.Equal(MasterApprovalStatuses.Approved, persisted.ApprovalStatus);
                Assert.Equal<uint>(2, persisted.Version);
                Assert.Equal(3, await qualificationVerifier.ControlledConfigurationHistories.AsNoTracking()
                    .CountAsync(x => x.EntityType == nameof(VendorQualification) && x.EntityId == qualificationId));
            }

            user.LoginId = Rev869BOwnedPostgresDatabase.Login;
            user.RoleCode = Rev869ARoleCodes.PurchaseExecutive;
            user.ActorId = directCreator.EmployeeId;
            permissions.AllowCommercialValues = false;
            var auditBeforeMask = await fixture.Db.AuditLogs.CountAsync();
            var maskedComparison = await client.GetAsync($"/api/v1/purchase/comparisons/{directComparisonNumber}");
            Assert.Equal(HttpStatusCode.OK, maskedComparison.StatusCode);
            var maskedJson = await maskedComparison.Content.ReadAsStringAsync();
            Assert.DoesNotContain("totalPayableValue", maskedJson, StringComparison.OrdinalIgnoreCase);
            Assert.True(await fixture.Db.AuditLogs.CountAsync() > auditBeforeMask);
            permissions.AllowCommercialValues = true;

            var auditBeforeExistingRecordDenial = await fixture.Db.AuditLogs.CountAsync();
            scopes.Allow = false;
            var attachmentDenied = await client.GetAsync($"/api/v1/purchase/quotations/{directQuotationNumber}/attachment");
            Assert.Equal(HttpStatusCode.Forbidden, attachmentDenied.StatusCode);
            var exportDenied = await client.GetAsync($"/api/v1/purchase/comparisons/{directComparisonNumber}/export");
            Assert.Equal(HttpStatusCode.Forbidden, exportDenied.StatusCode);
            Assert.True(await fixture.Db.AuditLogs.CountAsync() >= auditBeforeExistingRecordDenial + 2);
            scopes.Allow = true;

            pipelineAudit.Fail = true;
            var auditRollbackRequest = qualificationRequest with { QualificationCode = "MAPPED-Q-AUDIT", Remarks = "must roll back" };
            var auditRollback = await client.PostAsJsonAsync("/api/v1/rev869a/configuration/vendor-qualifications", auditRollbackRequest);
            Assert.Equal(HttpStatusCode.InternalServerError, auditRollback.StatusCode);
            pipelineAudit.Fail = false;
            await using (var rollbackVerifier = await fixture.OpenIndependentContextAsync())
                Assert.Equal(0, await rollbackVerifier.VendorQualifications.AsNoTracking().CountAsync(x =>
                    x.OrganizationId == directOrganization && x.QualificationCode == "MAPPED-Q-AUDIT"));

            user.OrganizationId = directOrganization + "-FOREIGN";
            var crossOrganizationQualification = await client.PostAsJsonAsync(
                "/api/v1/rev869a/configuration/vendor-qualifications",
                qualificationRequest with { QualificationCode = "MAPPED-Q-FOREIGN" });
            Assert.Equal(HttpStatusCode.NotFound, crossOrganizationQualification.StatusCode);
            user.OrganizationId = directOrganization;
        }
        finally
        {
            await app.StopAsync();
        }
    }

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private sealed class OwnedRfqFixture : IAsyncDisposable
    {
        private const string ExactDatabase = "sess_nexaerp_rev869b_verify";
        private const string ExactOptIn = "ISOLATED_REV869B_BEHAVIOR_TESTS";
        private const string MigrationId = "20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation";
        private readonly Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction;
        private readonly OwnedDatabaseLease databaseLease;
        private readonly long ownedBefore;
        private bool disposed;
        private bool rollbackCompleted;
        private bool transactionDisposed;
        private bool contextDisposed;
        private bool baselineVerified;

        private OwnedRfqFixture(NexaErpDbContext db, Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction, OwnedDatabaseLease databaseLease,
            string scenario, Guid warehouseId, Guid prId, Guid lineId, Guid handoffId, Guid actorId, Guid identityMappingId, long ownedBefore)
        {
            Db = db; this.transaction = transaction; this.databaseLease = databaseLease; Scenario = scenario; WarehouseId = warehouseId;
            PrId = prId; LineId = lineId; HandoffId = handoffId; ActorId = actorId; IdentityMappingId = identityMappingId; this.ownedBefore = ownedBefore;
        }

        public NexaErpDbContext Db { get; }
        public string DatabaseConnectionString => databaseLease.ConnectionString;
        public string Scenario { get; }
        public string Organization => "REV869B-PG-OWNED-" + Scenario.ToUpperInvariant();
        public string Marker => "REV869B-PG-OWNED:" + Scenario;
        public Guid WarehouseId { get; }
        public Guid PrId { get; }
        public Guid LineId { get; }
        public Guid HandoffId { get; }
        public Guid ActorId { get; }
        public Guid IdentityMappingId { get; }

        public static async Task<OwnedRfqFixture> CreateAsync(string scenario, bool useAmbientTransaction = true)
        {
            var databaseLease = await OwnedDatabaseLease.CreateAsync(scenario);
            var connectionString = databaseLease.ConnectionString;
            var options = new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(connectionString).Options;
            var db = new NexaErpDbContext(options);
            var tx = useAmbientTransaction
                ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable)
                : null;
            try
            {
                var warehouseId = DeterministicId(scenario, "warehouse");
                var prId = DeterministicId(scenario, "pr");
                var lineId = DeterministicId(scenario, "line");
                var handoffId = DeterministicId(scenario, "handoff");
                var actorId = Rev866SeedData.Employees.Single(x => x.EmployeeCode == "SESS-008").Id;
                var identityMappingId = DeterministicId(scenario, "identity-mapping");
                var marker = "REV869B-PG-OWNED:" + scenario;
                var organization = "REV869B-PG-OWNED-" + scenario.ToUpperInvariant();
                var ownedBefore = await CountOwnedAsync(db, marker, organization);
                if (ownedBefore != 0 || await db.Warehouses.AnyAsync(x => x.Id == warehouseId) ||
                    await db.PurchaseRequisitions.AnyAsync(x => x.Id == prId) ||
                    await db.PurchaseRequisitionLines.AnyAsync(x => x.Id == lineId) ||
                    await db.PurchaseRequirementHandoffs.AnyAsync(x => x.Id == handoffId))
                    throw new InvalidOperationException("REV869B deterministic fixture collision or unproven earlier rollback.");
                if (!await db.Employees.AnyAsync(x => x.Id == actorId) ||
                    !await db.Items.AnyAsync(x => x.Id == Rev869ASeedData.ApprovedEaItemId))
                    throw new InvalidOperationException("Required exact accepted seed identities are missing.");

                var warehouse = new Warehouse { Id = warehouseId, WarehouseCode = "R869B-" + scenario.ToUpperInvariant(),
                    Name = marker, WarehouseType = "ControlledTest", Status = MasterStatuses.Active,
                    ApprovalStatus = MasterApprovalStatuses.Approved, IsActive = true, CreatedBy = marker };
                var pr = new PurchaseRequisition { Id = prId, PrNumber = "REV869B-PG-PR-" + scenario.ToUpperInvariant(),
                    FinancialYear = "2026-27", PrSequence = DeterministicSequence(scenario),
                    OrganizationId = organization, RequestDate = new DateOnly(2026, 8, 12), RequiredByDate = new DateOnly(2026, 9, 30),
                    Priority = "Normal", PurposeJustification = marker, DeliveryWarehouseId = warehouseId,
                    Status = PurchaseRequisitionStatuses.NotAvailable, EstimatedTotal = 100m, IsActive = true, CreatedBy = marker };
                var line = new PurchaseRequisitionLine { Id = lineId, PurchaseRequisitionId = prId, LineNumber = 1,
                    ItemId = Rev869ASeedData.ApprovedEaItemId, PreferredWarehouseId = warehouseId,
                    ItemCodeSnapshot = Rev869ASeedData.ApprovedEaUomCode + "-ITEM", ItemNameSnapshot = marker,
                    UomSnapshot = Rev869ASeedData.ApprovedEaUomCode, RequestedQuantity = 1m,
                    EstimatedUnitPriceSnapshot = 100m, EstimatedLineTotal = 100m, RequiredDate = pr.RequiredByDate,
                    ShortageQuantity = 1m, ProcurementHandoffQuantity = 1m,
                    LineStatus = PurchaseRequisitionLineStatuses.PurchaseRequired, CreatedBy = marker };
                var handoff = new PurchaseRequirementHandoff { Id = handoffId, PurchaseRequisitionId = prId,
                    PurchaseRequisitionLineId = lineId, ItemId = Rev869ASeedData.ApprovedEaItemId,
                    WarehouseId = warehouseId, LocationKey = warehouseId.ToString("N"), HandoffQuantity = 1m,
                    Status = "PendingRFQ", HandoffNumber = "REV869B-PG-HO-" + scenario.ToUpperInvariant(),
                    HandoffBy = marker, CorrelationId = marker, CreatedBy = marker };
                var identityMapping = new EmployeeIdentityMapping { Id = identityMappingId, OrganizationId = organization,
                    Issuer = "REV869B-TEST-ISSUER", Subject = marker, EmployeeId = actorId,
                    EffectiveFrom = new DateOnly(2026, 1, 1), EffectiveTo = new DateOnly(2027, 12, 31),
                    IsActive = true, CreatedBy = marker };
                db.AddRange(warehouse, pr, line, handoff, identityMapping);
                await db.SaveChangesAsync();
                return new OwnedRfqFixture(db, tx, databaseLease, scenario, warehouseId, prId, lineId, handoffId, actorId, identityMappingId, ownedBefore);
            }
            catch
            {
                try
                {
                    if (tx is not null) { await tx.RollbackAsync(); await tx.DisposeAsync(); }
                    await db.DisposeAsync();
                }
                finally { await databaseLease.DisposeAsync(); }
                throw;
            }
        }

        public Rev869BCreateRfqRequest Request(string key) => new(new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero), "INR", false,
            null, key, [new Rev869BRfqSourceLineRequest(HandoffId, 1m)]);

        public EfRev869BPurchaseService Service(IAuditWriter? audit = null, IRecordScopeAuthorizer? scopes = null, ICurrentUser? currentUser = null)
            => Service(Db, audit, scopes, currentUser);

        public EfRev869BPurchaseService Service(NexaErpDbContext context, IAuditWriter? audit = null, IRecordScopeAuthorizer? scopes = null, ICurrentUser? currentUser = null)
        {
            var activeUser = currentUser ?? new FixtureUser(Marker, Rev869ARoleCodes.PurchaseExecutive, Organization, ActorId);
            return new EfRev869BPurchaseService(context, activeUser, scopes ?? new AllowingScope(),
                new EfVendorQualificationService(context), new EfTaxGstResolver(context), audit ?? new EfAuditWriter(context, activeUser));
        }

        public async Task<NexaErpDbContext> OpenIndependentContextAsync()
        {
            var options = new DbContextOptionsBuilder<NexaErpDbContext>()
                .UseNpgsql(databaseLease.ConnectionString).Options;
            return new NexaErpDbContext(options);
        }

        public Task<long> CountBusinessResultAsync() => CountOwnedAsync(Db, Marker, Organization);

        public async Task<long> CountBusinessResultFromIndependentContextAsync()
        {
            await using var verifier = await OpenIndependentContextAsync();
            return await CountOwnedAsync(verifier, Marker, Organization);
        }

        public async Task<OwnedState> CaptureOwnedStateFromIndependentContextAsync()
        {
            await using var verifier = await OpenIndependentContextAsync();
            return new OwnedState(
                await verifier.RequestForQuotations.LongCountAsync(x => x.OrganizationId == Organization),
                await verifier.RequestForQuotationLines.LongCountAsync(x => x.RequestForQuotation!.OrganizationId == Organization),
                await verifier.RfqVendorInvitations.LongCountAsync(x => x.RequestForQuotation!.OrganizationId == Organization),
                await verifier.VendorQuotations.LongCountAsync(x => x.OrganizationId == Organization),
                await verifier.VendorQuotationLines.LongCountAsync(x => x.VendorQuotation!.OrganizationId == Organization),
                await verifier.QuotationTechnicalVerifications.LongCountAsync(x => x.VendorQuotationLine!.VendorQuotation!.OrganizationId == Organization),
                await verifier.CommercialComparisons.LongCountAsync(x => x.OrganizationId == Organization),
                await verifier.CommercialComparisonLines.LongCountAsync(x => x.CommercialComparison!.OrganizationId == Organization),
                await verifier.PurchaseOrders.LongCountAsync(x => x.OrganizationId == Organization),
                await verifier.PurchaseOrderLines.LongCountAsync(x => x.PurchaseOrder!.OrganizationId == Organization),
                await verifier.PurchaseTransactionStatusHistories.LongCountAsync(x => x.OrganizationId == Organization),
                await verifier.PurchaseTransactionApprovalHistories.LongCountAsync(x => x.CommercialComparison!.OrganizationId == Organization),
                await verifier.PurchaseOrderHistories.LongCountAsync(x => x.PurchaseOrder!.OrganizationId == Organization),
                await verifier.AuditLogs.LongCountAsync(x => x.CreatedBy == Marker),
                await verifier.PurchaseNumberSequences.LongCountAsync(x => x.OrganizationId == Organization),
                await verifier.MaterialFollowUpHandoffs.LongCountAsync(x => x.PurchaseOrder!.OrganizationId == Organization),
                await verifier.EmployeeIdentityMappings.LongCountAsync(x => x.OrganizationId == Organization && x.CreatedBy == Marker),
                await verifier.Warehouses.LongCountAsync(x => x.CreatedBy == Marker),
                await verifier.PurchaseRequisitions.LongCountAsync(x => x.OrganizationId == Organization && x.CreatedBy == Marker),
                await verifier.PurchaseRequisitionLines.LongCountAsync(x => x.CreatedBy == Marker),
                await verifier.PurchaseRequirementHandoffs.LongCountAsync(x => x.CreatedBy == Marker),
                await CaptureExactDatabaseFingerprintAsync(verifier));
        }

        private static async Task<string> CaptureExactDatabaseFingerprintAsync(NexaErpDbContext verifier)
        {
            var connection = verifier.Database.GetDbConnection();
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT concat_ws('|',
                  (SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY t."Id")::text,'[]') FROM nexa.request_for_quotations t),
                  (SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY t."Id")::text,'[]') FROM nexa.request_for_quotation_lines t),
                  (SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY t."Id")::text,'[]') FROM nexa.rfq_vendor_invitations t),
                  (SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY t."Id")::text,'[]') FROM nexa.vendor_quotations t),
                  (SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY t."Id")::text,'[]') FROM nexa.vendor_quotation_lines t),
                  (SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY t."Id")::text,'[]') FROM nexa.quotation_technical_verifications t),
                  (SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY t."Id")::text,'[]') FROM nexa.commercial_comparisons t),
                  (SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY t."Id")::text,'[]') FROM nexa.commercial_comparison_lines t),
                  (SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY t."Id")::text,'[]') FROM nexa.purchase_transaction_approval_history t),
                  (SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY t."Id")::text,'[]') FROM nexa.purchase_orders t),
                  (SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY t."Id")::text,'[]') FROM nexa.purchase_order_lines t),
                  (SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY t."Id")::text,'[]') FROM nexa.purchase_order_history t),
                  (SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY t."Id")::text,'[]') FROM nexa.material_followup_handoffs t),
                  (SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY t."Id")::text,'[]') FROM nexa.purchase_transaction_status_history t),
                  (SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY t."Id")::text,'[]') FROM nexa.purchase_transaction_approval_policies t),
                  (SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY t."Id")::text,'[]') FROM nexa.audit_logs t),
                  (SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY t."Id")::text,'[]') FROM nexa.purchase_number_sequences t),
                  (SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY t."Id")::text,'[]') FROM nexa.employee_identity_mappings t),
                  (SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY t."Id")::text,'[]') FROM nexa.warehouses t),
                  (SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY t."Id")::text,'[]') FROM nexa.purchase_requisitions t),
                  (SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY t."Id")::text,'[]') FROM nexa.purchase_requisition_lines t),
                  (SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY t."Id")::text,'[]') FROM nexa.purchase_requirement_handoffs t))
                  ||'|'||(SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY t."Id")::text,'[]') FROM nexa.vendor_qualifications t)
                  ||'|'||(SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY t."Id")::text,'[]') FROM nexa.controlled_configuration_histories t)
                  ||'|'||(SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY t."Token")::text,'[]') FROM nexa.rev869b_command_contexts t)
                  ||'|'||(SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY t."KeyId")::text,'[]') FROM nexa.rev869b_command_authorities t)
                  ||'|'||(SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY t."Id")::text,'[]') FROM nexa.role_page_permissions t)
                  ||'|'||(SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY t."MigrationId")::text,'[]') FROM nexa."__EFMigrationsHistory" t)
                  ||'|'||(SELECT coalesce(jsonb_agg(jsonb_build_object('schema',n.nspname,'owner',pg_get_userbyid(n.nspowner)) ORDER BY n.nspname)::text,'[]')
                           FROM pg_namespace n WHERE n.nspname='nexa')
                """;
            var canonical = Convert.ToString(await command.ExecuteScalarAsync()) ?? string.Empty;
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        }

        public async ValueTask DisposeAsync()
        {
            if (disposed) return;
            Exception? verificationFailure = null;
            try
            {
                if (transaction is not null)
                {
                    if (!rollbackCompleted)
                    {
                        await transaction.RollbackAsync();
                        rollbackCompleted = true;
                    }
                    if (!transactionDisposed)
                    {
                        await transaction.DisposeAsync();
                        transactionDisposed = true;
                    }
                    if (!contextDisposed)
                    {
                        await Db.DisposeAsync();
                        contextDisposed = true;
                    }
                    if (!baselineVerified)
                    {
                        var options = new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(databaseLease.ConnectionString).Options;
                        await using var verifier = new NexaErpDbContext(options);
                        var after = await CountOwnedAsync(verifier, Marker, Organization);
                        if (after != ownedBefore) throw new InvalidOperationException("REV869B outer rollback did not restore the exact test-owned baseline.");
                        baselineVerified = true;
                    }
                }
                else if (!contextDisposed)
                {
                    Db.ChangeTracker.Clear();
                    await Db.DisposeAsync();
                    contextDisposed = true;
                }
            }
            catch (Exception ex) { verificationFailure = ex; }
            finally { await databaseLease.DisposeAsync(); }
            disposed = true;
            if (verificationFailure is not null) throw verificationFailure;
        }

        public sealed record OwnedState(long Rfqs, long RfqLines, long Invitations, long Quotations, long QuotationLines, long TechnicalVerifications,
            long Comparisons, long ComparisonLines, long PurchaseOrders, long PurchaseOrderLines, long StatusHistories, long ApprovalHistories, long PoHistories,
            long Audits, long NumberSequences, long FollowUps, long IdentityMappings, long Warehouses,
            long PurchaseRequisitions, long PurchaseRequisitionLines, long RequirementHandoffs, string ExactDatabaseFingerprint);

        private static async Task<long> CountOwnedAsync(NexaErpDbContext db, string marker, string organization) =>
            await db.RequestForQuotations.LongCountAsync(x => x.OrganizationId == organization) +
            await db.RequestForQuotationLines.LongCountAsync(x => x.RequestForQuotation!.OrganizationId == organization) +
            await db.PurchaseTransactionStatusHistories.LongCountAsync(x => x.OrganizationId == organization) +
            await db.AuditLogs.LongCountAsync(x => x.CreatedBy == marker) +
            await db.RfqVendorInvitations.LongCountAsync(x => x.RequestForQuotation!.OrganizationId == organization) +
            await db.VendorQuotations.LongCountAsync(x => x.OrganizationId == organization) +
            await db.VendorQuotationLines.LongCountAsync(x => x.VendorQuotation!.OrganizationId == organization) +
            await db.QuotationTechnicalVerifications.LongCountAsync(x => x.VendorQuotationLine!.VendorQuotation!.OrganizationId == organization) +
            await db.CommercialComparisons.LongCountAsync(x => x.OrganizationId == organization) +
            await db.CommercialComparisonLines.LongCountAsync(x => x.CommercialComparison!.OrganizationId == organization) +
            await db.PurchaseTransactionApprovalHistories.LongCountAsync(x => x.CommercialComparison!.OrganizationId == organization) +
            await db.PurchaseOrders.LongCountAsync(x => x.OrganizationId == organization) +
            await db.PurchaseOrderLines.LongCountAsync(x => x.PurchaseOrder!.OrganizationId == organization) +
            await db.PurchaseOrderHistories.LongCountAsync(x => x.PurchaseOrder!.OrganizationId == organization) +
            await db.MaterialFollowUpHandoffs.LongCountAsync(x => x.PurchaseOrder!.OrganizationId == organization) +
            await db.PurchaseNumberSequences.LongCountAsync(x => x.OrganizationId == organization) +
            await db.EmployeeIdentityMappings.LongCountAsync(x => x.OrganizationId == organization && x.CreatedBy == marker) +
            await db.Warehouses.LongCountAsync(x => x.CreatedBy == marker) +
            await db.PurchaseRequisitions.LongCountAsync(x => x.OrganizationId == organization && x.CreatedBy == marker) +
            await db.PurchaseRequisitionLines.LongCountAsync(x => x.CreatedBy == marker) +
            await db.PurchaseRequirementHandoffs.LongCountAsync(x => x.CreatedBy == marker);

        private static Guid DeterministicId(string scenario, string entity)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes("REV869B-PG-OWNED|" + scenario + "|" + entity));
            return new Guid(bytes[..16]);
        }

        private static int DeterministicSequence(string scenario) =>
            BitConverter.ToInt32(SHA256.HashData(Encoding.UTF8.GetBytes("REV869B-PG-SEQUENCE|" + scenario)), 0) & int.MaxValue;

        private sealed class OwnedDatabaseLease : IAsyncDisposable
        {
            private readonly Rev869BTestDatabaseLease lease;
            private OwnedDatabaseLease(Rev869BTestDatabaseLease lease) => this.lease = lease;
            public string ConnectionString => lease.ConnectionString;
            public string DatabaseName => lease.DatabaseName;

            public static async Task<OwnedDatabaseLease> CreateAsync(string scenario)
            {
                var shared = await Rev869BTestDatabaseLease.CreateAsync(scenario, "application");
                return new OwnedDatabaseLease(shared);
            }

            public ValueTask DisposeAsync() => lease.DisposeAsync();
        }

        private static async Task<string> VerifiedConnectionStringAsync()
        {
            if (!string.Equals(Environment.GetEnvironmentVariable("REV869B_POSTGRES_OPT_IN"), ExactOptIn, StringComparison.Ordinal))
                throw new InvalidOperationException($"Set REV869B_POSTGRES_OPT_IN={ExactOptIn} explicitly.");
            var raw = Environment.GetEnvironmentVariable("REV869B_POSTGRES") ??
                throw new InvalidOperationException("REV869B_POSTGRES is required; no fallback is permitted.");
            var builder = new NpgsqlConnectionStringBuilder(raw);
            if (!string.Equals(builder.Database, ExactDatabase, StringComparison.Ordinal))
                throw new InvalidOperationException($"Only the exact isolated database {ExactDatabase} is permitted.");
            await using var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync();
            if (!string.Equals(Convert.ToString(await new NpgsqlCommand("SELECT current_database()", connection).ExecuteScalarAsync()), ExactDatabase, StringComparison.Ordinal))
                throw new InvalidOperationException("Connected database identity is not the exact isolated REV869B database.");
            var command = new NpgsqlCommand("SELECT count(*) FROM nexa.\"__EFMigrationsHistory\" WHERE \"MigrationId\"=@migration", connection);
            command.Parameters.AddWithValue("migration", MigrationId);
            if (Convert.ToInt64(await command.ExecuteScalarAsync()) != 1)
                throw new InvalidOperationException("The retained REV869B migration must be installed exactly once.");
            return builder.ConnectionString;
        }
    }

    private sealed class FixtureUser(string loginId, string roleCode, string? organizationId, Guid actorId) : ICurrentUser
    {
        public string LoginId { get; set; } = loginId;
        public string RoleCode { get; set; } = roleCode;
        public string? OrganizationId { get; set; } = organizationId;
        public Guid ActorId { get; set; } = actorId;
        public string? IdentityIssuer { get; set; } = "REV869B-TEST-ISSUER";
        public string? IdentitySubject => LoginId;
        public bool IsAuthenticated => true;
        public Guid? EmployeeId => ActorId;
    }

    private sealed class AllowingScope : IRecordScopeAuthorizer
    {
        public Task<RecordScopeDecision> AuthorizeAnyAsync(Guid employeeId, string roleCode, string organizationId, DateOnly onDate, CancellationToken ct) => Task.FromResult(new RecordScopeDecision(true, "test-owned exact scope"));
        public Task<RecordScopeDecision> AuthorizeAsync(Guid employeeId, string roleCode, RecordScopeTarget target, DateOnly onDate, CancellationToken ct) => Task.FromResult(new RecordScopeDecision(true, "test-owned exact scope"));
    }

    private sealed class DenyingScope : IRecordScopeAuthorizer
    {
        public Task<RecordScopeDecision> AuthorizeAnyAsync(Guid employeeId, string roleCode, string organizationId, DateOnly onDate, CancellationToken ct) => Task.FromResult(new RecordScopeDecision(false, "record scope denied"));
        public Task<RecordScopeDecision> AuthorizeAsync(Guid employeeId, string roleCode, RecordScopeTarget target, DateOnly onDate, CancellationToken ct) => Task.FromResult(new RecordScopeDecision(false, "record scope denied"));
    }

    private sealed class ToggleScope : IRecordScopeAuthorizer
    {
        public bool Allow { get; set; } = true;
        public Task<RecordScopeDecision> AuthorizeAnyAsync(Guid employeeId, string roleCode, string organizationId, DateOnly onDate, CancellationToken ct) => Task.FromResult(new RecordScopeDecision(Allow, Allow ? "test-owned exact scope" : "record scope denied"));
        public Task<RecordScopeDecision> AuthorizeAsync(Guid employeeId, string roleCode, RecordScopeTarget target, DateOnly onDate, CancellationToken ct) => Task.FromResult(new RecordScopeDecision(Allow, Allow ? "test-owned exact scope" : "record scope denied"));
    }

    private sealed class FailingAuditWriter : IAuditWriter
    {
        public Task WriteAsync(string module, string action, string entityName, string entityId, object? before, object? after, CancellationToken ct) => throw new InjectedAuditFailure();
    }

    private sealed class ToggleAuditWriter(IAuditWriter inner) : IAuditWriter
    {
        public bool Fail { get; set; }
        public Task WriteAsync(string module, string action, string entityName, string entityId, object? before, object? after, CancellationToken ct) =>
            Fail ? Task.FromException(new InjectedAuditFailure()) : inner.WriteAsync(module, action, entityName, entityId, before, after, ct);
    }

    private sealed class InjectedAuditFailure : Exception;

    private sealed class AllowAllPagePermissions : IPagePermissionService
    {
        public Task<bool> HasPermissionAsync(string roleCode, string pageKey, string permission, CancellationToken ct) =>
            Task.FromResult(true);
    }

    private sealed class TogglePagePermissions : IPagePermissionService
    {
        public bool Allow { get; set; } = true;
        public bool AllowCommercialValues { get; set; } = true;
        public Task<bool> HasPermissionAsync(string roleCode, string pageKey, string permission, CancellationToken ct) =>
            Task.FromResult(permission == PagePermissionActions.ViewCommercialValues ? AllowCommercialValues : Allow);
    }

    private sealed class OwnedAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "REV869B-Owned";
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization")) return Task.FromResult(AuthenticateResult.NoResult());
            var identity = new System.Security.Claims.ClaimsIdentity(
                [new(System.Security.Claims.ClaimTypes.NameIdentifier, "REV869B-PG-OWNED")], SchemeName);
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new System.Security.Claims.ClaimsPrincipal(identity), SchemeName)));
        }
    }
}
