using System.Data;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
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
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Inventory;
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
        await using var fixture = await OwnedRfqFixture.CreateAsync("service-success");
        var result = await fixture.Service().CreateRfqAsync(fixture.Request("same-command"), default);

        Assert.Equal(Rev869BStatuses.Draft, result.Status);
        Assert.Equal(1, await fixture.Db.RequestForQuotations.CountAsync(x => x.Id == result.Id));
        Assert.Equal(1, await fixture.Db.RequestForQuotationLines.CountAsync(x => x.RequestForQuotationId == result.Id));
        Assert.Equal(1, await fixture.Db.PurchaseTransactionStatusHistories.CountAsync(x => x.EntityId == result.Id));
        Assert.Equal(1, await fixture.Db.AuditLogs.CountAsync(x => x.EntityId == result.Id.ToString() && x.Action == "CreateRFQ"));
    }

    [Fact]
    public async Task RealServiceFailureAfterWritesRollsBackEveryAffectedRelation()
    {
        await using var fixture = await OwnedRfqFixture.CreateAsync("audit-rollback", useAmbientTransaction: false);
        var before = await fixture.CountBusinessResultAsync();
        await Assert.ThrowsAsync<InjectedAuditFailure>(() =>
            fixture.Service(new FailingAuditWriter()).CreateRfqAsync(fixture.Request("failing-command"), default));
        fixture.Db.ChangeTracker.Clear();
        Assert.Equal(before, await fixture.CountBusinessResultAsync());
    }

    [Fact]
    public async Task RealServiceIdempotentReplayReturnsAuthoritativeOriginalWithoutDuplicates()
    {
        await using var fixture = await OwnedRfqFixture.CreateAsync("idempotent-replay");
        var command = fixture.Request("same-command");
        var first = await fixture.Service().CreateRfqAsync(command, default);
        fixture.Db.ChangeTracker.Clear();
        var second = await fixture.Service().CreateRfqAsync(command, default);

        Assert.Equal(first, second);
        Assert.Equal(1, await fixture.Db.RequestForQuotations.CountAsync(x => x.OrganizationId == fixture.Organization));
        Assert.Equal(1, await fixture.Db.RequestForQuotationLines.CountAsync(x => x.RequestForQuotation!.OrganizationId == fixture.Organization));
        Assert.Equal(1, await fixture.Db.PurchaseTransactionStatusHistories.CountAsync(x => x.OrganizationId == fixture.Organization && x.EntityType == "RFQ"));
        Assert.Equal(1, await fixture.Db.AuditLogs.CountAsync(x => x.CreatedBy == fixture.Marker && x.Action == "CreateRFQ"));
    }

    [Fact]
    public async Task RealProtectedServiceDenialHasNoBusinessMutationAndNoCrossOrganizationDisclosure()
    {
        await using var fixture = await OwnedRfqFixture.CreateAsync("scope-denial");
        var before = await fixture.CountBusinessResultAsync();
        var denied = fixture.Service(scopes: new DenyingScope());

        var error = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            denied.CreateRfqAsync(fixture.Request("denied-command"), default));

        Assert.DoesNotContain(fixture.HandoffId.ToString(), error.Message, StringComparison.OrdinalIgnoreCase);
        fixture.Db.ChangeTracker.Clear();
        Assert.Equal(0, await fixture.Db.RequestForQuotations.CountAsync(x => x.OrganizationId == fixture.Organization));
        Assert.Equal(0, await fixture.Db.RequestForQuotationLines.CountAsync(x => x.RequestForQuotation!.OrganizationId == fixture.Organization));
        Assert.Equal(before + 1, await fixture.CountBusinessResultAsync());
        Assert.Equal(1, await fixture.Db.AuditLogs.CountAsync(x => x.CreatedBy == fixture.Marker && x.Action == "Denied" && x.Result == "Failure"));
    }

    [Fact]
    public async Task RealProtectedServicePropagatesAuditWriterFailureWithoutFalseSuccess()
    {
        await using var fixture = await OwnedRfqFixture.CreateAsync("audit-propagation", useAmbientTransaction: false);
        await Assert.ThrowsAsync<InjectedAuditFailure>(() => fixture.Service(new FailingAuditWriter())
            .CreateRfqAsync(fixture.Request("audit-failure-command"), default));
        fixture.Db.ChangeTracker.Clear();
        Assert.Equal(0, await fixture.CountBusinessResultAsync());
    }

    [Fact]
    public async Task TwoIndependentDbContextsConnectionsAndServicesProduceOneAuthoritativeWinner()
    {
        await using var fixture = await OwnedRfqFixture.CreateAsync("concurrent-services", useAmbientTransaction: false);
        await using var firstDb = await fixture.OpenIndependentContextAsync();
        await using var secondDb = await fixture.OpenIndependentContextAsync();
        Assert.NotSame(firstDb, secondDb);
        Assert.NotSame(firstDb.Database.GetDbConnection(), secondDb.Database.GetDbConnection());
        await using var firstTx = await firstDb.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await using var secondTx = await secondDb.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var provisional = await fixture.Service(firstDb).CreateRfqAsync(fixture.Request("collision-key"), default);
        var competing = fixture.Service(secondDb).CreateRfqAsync(fixture.Request("collision-key"), default);
        await Task.Delay(100);
        Assert.False(competing.IsCompleted, "The independent writer must contend on the authoritative idempotency key.");
        await firstTx.RollbackAsync();

        var winner = await competing;
        Assert.NotEqual(provisional.Id, winner.Id);
        Assert.Equal(1, await secondDb.RequestForQuotations.CountAsync(x => x.OrganizationId == fixture.Organization));
        Assert.Equal(1, await secondDb.RequestForQuotationLines.CountAsync(x => x.RequestForQuotation!.OrganizationId == fixture.Organization));
        await secondTx.RollbackAsync();
    }

    [Fact]
    public async Task AuthenticatedMappedAspNetEndpointTraversesPermissionScopeServiceAndEf()
    {
        await using var fixture = await OwnedRfqFixture.CreateAsync("mapped-endpoint");
        var user = new FixtureUser(fixture.Marker, Rev869ARoleCodes.PurchaseExecutive, fixture.Organization, fixture.ActorId);
        var port = FreePort();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Test" });
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
        builder.Services.AddRouting();
        builder.Services.AddAuthentication(OwnedAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, OwnedAuthenticationHandler>(OwnedAuthenticationHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<ICurrentUser>(user);
        builder.Services.AddSingleton<IRecordScopeAuthorizer, AllowingScope>();
        builder.Services.AddSingleton<IPagePermissionService, AllowAllPagePermissions>();
        builder.Services.AddSingleton<IAuditWriter>(new EfAuditWriter(fixture.Db, user));
        builder.Services.AddSingleton<IRev869BPurchaseService>(fixture.Service());

        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapRev869BPurchaseEndpoints();
        await app.StartAsync();
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
            client.DefaultRequestHeaders.Authorization = new("Owned");
            var response = await client.PostAsJsonAsync("/api/v1/purchase/rfqs", fixture.Request("mapped-command"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<Rev869BDocumentResult>();
            Assert.NotNull(result);
            Assert.Equal(1, await fixture.Db.RequestForQuotations.CountAsync(x => x.Id == result.Id));
            Assert.Equal(1, await fixture.Db.AuditLogs.CountAsync(x => x.EntityId == result.Id.ToString()));
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
        private readonly long ownedBefore;
        private bool disposed;

        private OwnedRfqFixture(NexaErpDbContext db, Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction,
            string scenario, Guid warehouseId, Guid prId, Guid lineId, Guid handoffId, Guid actorId, long ownedBefore)
        {
            Db = db; this.transaction = transaction; Scenario = scenario; WarehouseId = warehouseId;
            PrId = prId; LineId = lineId; HandoffId = handoffId; ActorId = actorId; this.ownedBefore = ownedBefore;
        }

        public NexaErpDbContext Db { get; }
        public string Scenario { get; }
        public string Organization => "REV869B-PG-OWNED-" + Scenario.ToUpperInvariant();
        public string Marker => "REV869B-PG-OWNED:" + Scenario;
        public Guid WarehouseId { get; }
        public Guid PrId { get; }
        public Guid LineId { get; }
        public Guid HandoffId { get; }
        public Guid ActorId { get; }

        public static async Task<OwnedRfqFixture> CreateAsync(string scenario, bool useAmbientTransaction = true)
        {
            var connectionString = await VerifiedConnectionStringAsync();
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
                    FinancialYear = "2026-27", PrSequence = Math.Abs(scenario.GetHashCode(StringComparison.Ordinal)),
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
                db.AddRange(warehouse, pr, line, handoff);
                await db.SaveChangesAsync();
                return new OwnedRfqFixture(db, tx, scenario, warehouseId, prId, lineId, handoffId, actorId, ownedBefore);
            }
            catch
            {
                if (tx is not null) { await tx.RollbackAsync(); await tx.DisposeAsync(); }
                await db.DisposeAsync(); throw;
            }
        }

        public Rev869BCreateRfqRequest Request(string key) => new(DateTimeOffset.UtcNow.AddDays(7), "INR", false,
            null, key, [new Rev869BRfqSourceLineRequest(HandoffId, 1m)]);

        public EfRev869BPurchaseService Service(IAuditWriter? audit = null, IRecordScopeAuthorizer? scopes = null)
            => Service(Db, audit, scopes);

        public EfRev869BPurchaseService Service(NexaErpDbContext context, IAuditWriter? audit = null, IRecordScopeAuthorizer? scopes = null)
        {
            var user = new FixtureUser(Marker, Rev869ARoleCodes.PurchaseExecutive, Organization, ActorId);
            return new EfRev869BPurchaseService(context, user, scopes ?? new AllowingScope(),
                new EfVendorQualificationService(context), new EfTaxGstResolver(context), audit ?? new EfAuditWriter(context, user));
        }

        public async Task<NexaErpDbContext> OpenIndependentContextAsync()
        {
            var options = new DbContextOptionsBuilder<NexaErpDbContext>()
                .UseNpgsql(await VerifiedConnectionStringAsync()).Options;
            return new NexaErpDbContext(options);
        }

        public Task<long> CountBusinessResultAsync() => CountOwnedAsync(Db, Marker, Organization);

        public async ValueTask DisposeAsync()
        {
            if (disposed) return;
            disposed = true;
            if (transaction is not null)
            {
                await transaction.RollbackAsync();
                await transaction.DisposeAsync();
                await Db.DisposeAsync();
                var options = new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(await VerifiedConnectionStringAsync()).Options;
                await using var verifier = new NexaErpDbContext(options);
                var after = await CountOwnedAsync(verifier, Marker, Organization);
                if (after != ownedBefore) throw new InvalidOperationException("REV869B outer rollback did not restore the exact test-owned baseline.");
                return;
            }

            Db.ChangeTracker.Clear();
            var leaked = await CountBusinessResultAsync();
            if (leaked != ownedBefore)
                throw new InvalidOperationException("Service-owned rollback left test-owned REV869B rows.");
            await Db.PurchaseRequirementHandoffs.Where(x => x.Id == HandoffId).ExecuteDeleteAsync();
            await Db.PurchaseRequisitionLines.Where(x => x.Id == LineId).ExecuteDeleteAsync();
            await Db.PurchaseRequisitions.Where(x => x.Id == PrId).ExecuteDeleteAsync();
            await Db.Warehouses.Where(x => x.Id == WarehouseId).ExecuteDeleteAsync();
            await Db.DisposeAsync();
        }

        private static async Task<long> CountOwnedAsync(NexaErpDbContext db, string marker, string organization) =>
            await db.RequestForQuotations.LongCountAsync(x => x.OrganizationId == organization) +
            await db.RequestForQuotationLines.LongCountAsync(x => x.RequestForQuotation!.OrganizationId == organization) +
            await db.PurchaseTransactionStatusHistories.LongCountAsync(x => x.OrganizationId == organization) +
            await db.AuditLogs.LongCountAsync(x => x.CreatedBy == marker);

        private static Guid DeterministicId(string scenario, string entity)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes("REV869B-PG-OWNED|" + scenario + "|" + entity));
            return new Guid(bytes[..16]);
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

    private sealed record FixtureUser(string LoginId, string RoleCode, string? OrganizationId, Guid ActorId) : ICurrentUser
    {
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

    private sealed class FailingAuditWriter : IAuditWriter
    {
        public Task WriteAsync(string module, string action, string entityName, string entityId, object? before, object? after, CancellationToken ct) => throw new InjectedAuditFailure();
    }

    private sealed class InjectedAuditFailure : Exception;

    private sealed class AllowAllPagePermissions : IPagePermissionService
    {
        public Task<bool> HasPermissionAsync(string roleCode, string pageKey, string permission, CancellationToken ct) =>
            Task.FromResult(true);
    }

    private sealed class OwnedAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "REV869B-Owned";
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new System.Security.Claims.ClaimsIdentity(
                [new(System.Security.Claims.ClaimTypes.NameIdentifier, "REV869B-PG-OWNED")], SchemeName);
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new System.Security.Claims.ClaimsPrincipal(identity), SchemeName)));
        }
    }
}
