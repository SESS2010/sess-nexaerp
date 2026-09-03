using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class TechnicalDirectorDecisionTests
{
    [Theory]
    [InlineData("STORES_EXECUTIVE")]
    [InlineData("STORE_HEAD")]
    public void StoresOperatorsCanViewAndVerifyStockCheck(string roleCode)
    {
        var role = FoundationSeedData.Roles.Concat(Rev866SeedData.AdditionalEmployeeRoles)
            .Single(x => x.Code == roleCode);
        var page = FoundationSeedData.Pages.Single(x => x.PageKey == "stores.stock-check");
        RolePagePermission permission = Rev866SeedData.RolePagePermissions
            .Single(x => x.RoleId == role.Id && x.PageDefinitionId == page.Id);

        Assert.True(permission.CanView);
        Assert.True(permission.CanVerify);
    }

    [Fact]
    public void MissingPhysicalRackBinReturnsBadRequestMessage()
    {
        var source = Read("src", "SESS.NexaERP.Api", "Endpoints", "PurchaseRequisitionSupport.cs");
        Assert.Contains("catch (InvalidOperationException ex)", source);
        Assert.Contains("return Results.BadRequest(new { message = ex.Message });", source);
        Assert.Contains("Line {line.LineNumber}: a physical Rack/Bin is required.", source);
    }

    [Fact]
    public void StockCheckScopeMigrationGuardsBothDirectionsAndEnsuresPriyaPurchaseScope()
    {
        var source = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260903125259_StockCheckAndPriyaPurchaseScopeV2.cs");
        Assert.Equal(2, Count(source, "current_setting('server_version_num')"));
        Assert.Contains("SESS-15", source);
        Assert.Contains("""d."Code"='PURCHASE'""", source);
        Assert.Contains("8f6a81af-626b-1fe2-7bfd-6a65e20597f8", source);
        Assert.Contains("e4fb4bcd-a855-58f8-4858-8a4e825185dd", source);
    }

    private static int Count(string source, string value) => source.Split(value, StringSplitOptions.None).Length - 1;
    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine([Root, .. parts]));
    private static readonly string Root = FindRoot();
    private static string FindRoot() { var d = new DirectoryInfo(AppContext.BaseDirectory); while (d is not null && !File.Exists(Path.Combine(d.FullName, "SESS.NexaERP.slnx"))) d = d.Parent; return d?.FullName ?? throw new DirectoryNotFoundException("Repository root not found."); }
}
