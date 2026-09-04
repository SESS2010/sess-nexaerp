using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Persistence.Migrations;

namespace SESS.NexaERP.Tests;

public sealed class StoresReceiptAuthorizationCorrectionTests
{
    [Fact]
    public void ReceiptReadsUsePagePermissionAndCompanyOnlyWhileEveryMutationKeepsTheOperatorGate()
    {
        var root = FindRoot();
        var gateQueries = Read(root, "src", "SESS.NexaERP.Infrastructure", "Stores", "EfGateEntryService.Queries.cs");
        var gateCommands = Read(root, "src", "SESS.NexaERP.Infrastructure", "Stores", "EfGateEntryService.cs");
        var receipts = Read(root, "src", "SESS.NexaERP.Infrastructure", "Stores", "EfGoodsReceiptService.cs");

        var gateGet = Method(gateQueries, "public async Task<GateEntryResult?> GetAsync", "public async Task<GateEntryListResult> ListAsync");
        var gateList = Method(gateQueries, "public async Task<GateEntryListResult> ListAsync", "private IQueryable<GateEntry> Query");
        AssertReadOnly(gateGet);
        AssertReadOnly(gateList);
        Assert.Equal(4, Count(gateCommands + gateQueries, "RequireReceiptOperatorAsync(ct)"));
        Assert.Contains("CreateAsync", gateCommands);
        Assert.Contains("UpdateAsync", gateCommands);
        Assert.Contains("FinalizeAsync", gateCommands);

        var receiptGet = Method(receipts, "public async Task<GoodsReceiptResult?> GetAsync", "public async Task<GoodsReceiptListResult> ListAsync");
        var receiptList = Method(receipts, "public async Task<GoodsReceiptListResult> ListAsync", "private async Task<List<GoodsReceiptLine>> BuildLines");
        AssertReadOnly(receiptGet);
        AssertReadOnly(receiptList);
        Assert.Equal(4, Count(receipts, "RequireReceiptOperatorAsync(ct)"));
        foreach (var command in new[] { "CreateAsync", "UpdateAsync", "FinalizeAsync", "ReverseAsync" })
            Assert.Contains(command, receipts);
    }

    [Fact]
    public void InventoryGrnMutationGrantsMatchTheTwoReceiptRolesAndViewRemainsBroad()
    {
        var page = FoundationSeedData.Pages.Single(x => x.PageKey == "inventory.grn");
        var roles = FoundationSeedData.Roles
            .Concat(Rev866SeedData.AdditionalEmployeeRoles)
            .Concat(Rev869ASeedData.Roles)
            .Append(AdvanceSeedData.DepartmentManagerRole)
            .GroupBy(x => x.Id)
            .ToDictionary(x => x.Key, x => x.Last().Code);
        var permissions = AdvanceSeedData.RolePagePermissions.Where(x => x.PageDefinitionId == page.Id).ToArray();
        var expectedWriters = new[] { "STORES_ASSISTANT", "STORES_EXECUTIVE" };

        Assert.Equal(expectedWriters, Granted(permissions, roles, x => x.CanCreate));
        Assert.Equal(expectedWriters, Granted(permissions, roles, x => x.CanUpdate));
        Assert.Equal(expectedWriters, Granted(permissions, roles, x => x.CanSubmit));
        Assert.Equal(expectedWriters, Granted(permissions, roles, x => x.CanResubmit));
        Assert.Equal(expectedWriters, Granted(permissions, roles, x => x.CanCancel));
        Assert.Equal(expectedWriters, Granted(permissions, roles, x => x.CanUploadAttachment));
        Assert.Equal(expectedWriters, Granted(permissions, roles, x => x.CanReplaceAttachment));

        var viewers = Granted(permissions, roles, x => x.CanView);
        foreach (var role in new[] { "ADMIN", "MD", "MANAGING_DIRECTOR", "TECHNICAL_DIRECTOR", "STORE_HEAD", "PURCHASE_HEAD", "PRODUCTION_HEAD", "QC_HEAD", "STORES_EXECUTIVE", "STORES_ASSISTANT" })
            Assert.Contains(role, viewers);

        var permissionService = Read(FindRoot(), "src", "SESS.NexaERP.Infrastructure", "Authorization", "EfPagePermissionService.cs");
        Assert.Contains("or \"inventory.grn\"", permissionService);
    }

    [Fact]
    public void DebugImpersonationUsesExistingEmployeesIsAuditedAndIsCompileTimeExcludedFromRelease()
    {
        var root = FindRoot();
        var endpoint = Read(root, "src", "SESS.NexaERP.Api", "Endpoints", "DevelopmentAuthEndpoints.cs");
        var token = Read(root, "src", "SESS.NexaERP.Api", "Security", "DevelopmentTokenService.cs");
        var middleware = Read(root, "src", "SESS.NexaERP.Api", "Middleware", "EmployeeIdentityResolutionMiddleware.cs");
        var resolver = Read(root, "src", "SESS.NexaERP.Infrastructure", "Identity", "EfEmployeeIdentityResolver.cs");
        var program = Read(root, "src", "SESS.NexaERP.Api", "Program.cs");

        Assert.StartsWith("#if DEBUG", endpoint);
        Assert.StartsWith("#if DEBUG", token);
        Assert.Contains("EmployeeCompanyAssignments", endpoint);
        Assert.DoesNotContain("EmployeeIdentityMappings", endpoint);
        Assert.Contains("DevelopmentEmployeeImpersonation", endpoint);
        Assert.Contains("requestedEmployeeCode = code", endpoint);
        Assert.Contains("db.AuditLogs.Add", endpoint);
        Assert.DoesNotContain("Password", endpoint, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ImpersonatedEmployeeCodeClaim", middleware);
        Assert.Contains("ResolveDevelopmentEmployeeAsync", middleware);

        var developmentResolver = Method(resolver, "#if DEBUG", "#endif");
        Assert.Contains("ResolveDevelopmentEmployeeAsync", developmentResolver);
        Assert.Contains("EmployeeCompanyAssignments", developmentResolver);
        Assert.DoesNotContain("EmployeeIdentityMappings", developmentResolver);
        Assert.DoesNotContain("LoginEnabled", developmentResolver);
        Assert.Contains("NexaErp:AllowDevelopmentAuthentication", program);
        Assert.Contains("must not be present in a Release build", program);

        var api = typeof(SESS.NexaERP.Api.Endpoints.StoresGateEntryEndpoints).Assembly;
#if DEBUG
        Assert.NotNull(api.GetType("SESS.NexaERP.Api.Endpoints.DevelopmentAuthEndpoints"));
        Assert.NotNull(api.GetType("SESS.NexaERP.Api.Security.DevelopmentTokenService"));
#else
        Assert.Null(api.GetType("SESS.NexaERP.Api.Endpoints.DevelopmentAuthEndpoints"));
        Assert.Null(api.GetType("SESS.NexaERP.Api.Security.DevelopmentTokenService"));
#endif
    }

    [Theory]
    [InlineData("Up")]
    [InlineData("Down")]
    public void PermissionMigrationGuardsBothDirections(string methodName)
    {
        var migration = new StoresReceiptAuthorizationCorrections();
        var method = migration.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        var error = Assert.Throws<TargetInvocationException>(() => method.Invoke(migration, [new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite")]));
        Assert.IsType<NotSupportedException>(error.InnerException);

        var source = Read(FindRoot(), "src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260901140623_StoresReceiptAuthorizationCorrections.cs");
        Assert.Equal(2, Count(source, "PostgreSqlClusterGuard.Require(migrationBuilder);"));
        Assert.Equal(16, Count(source, "migrationBuilder.UpdateData("));
    }

    private static void AssertReadOnly(string source)
    {
        Assert.DoesNotContain("RequireReceiptOperatorAsync", source);
        Assert.DoesNotContain("RequireScope", source);
        Assert.DoesNotContain("ActorRole", source);
        Assert.DoesNotContain("scopes.AuthorizeAsync", source);
    }

    private static string[] Granted(IEnumerable<RolePagePermission> permissions, IReadOnlyDictionary<Guid, string> roles, Func<RolePagePermission, bool> predicate) =>
        permissions.Where(predicate).Select(x => roles[x.RoleId]).Order(StringComparer.Ordinal).ToArray();

    private static string Method(string source, string startMarker, string endMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Missing method marker: {startMarker}");
        var end = source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        Assert.True(end > start, $"Missing end marker: {endMarker}");
        return source[start..end];
    }

    private static int Count(string text, string value) =>
        (text.Length - text.Replace(value, string.Empty, StringComparison.Ordinal).Length) / value.Length;

    private static string Read(string root, params string[] parts) => File.ReadAllText(Path.Combine([root, .. parts]));

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException();
    }
}
