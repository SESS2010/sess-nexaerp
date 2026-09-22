using SESS.NexaERP.Application.Inventory;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Tests;

public sealed class WorkflowVersionAndReadModelRegressionTests
{
    [Fact]
    public void Mir_projection_humanizes_requester_and_department()
    {
        Assert.Equal(typeof(string), typeof(MaterialIssueRequestView).GetProperty("EmployeeCode")!.PropertyType);
        Assert.Equal(typeof(string), typeof(MaterialIssueRequestView).GetProperty("EmployeeName")!.PropertyType);
        Assert.Equal(typeof(string), typeof(MaterialIssueRequestView).GetProperty("DepartmentCode")!.PropertyType);
        var source = Read("src", "SESS.NexaERP.Infrastructure", "Stores", "EfMaterialIssueService.Read.cs");
        Assert.Contains("requester.EmployeeCode, requester.EmployeeName", source);
        Assert.Contains("departmentCode", source);
    }

    [Fact]
    public void Item_reads_expose_the_base_uom_identifier_without_uom_master_access()
    {
        Assert.Equal(typeof(Guid), typeof(ItemSummary).GetProperty("BaseUomId")!.PropertyType);
        Assert.Equal(typeof(Guid), typeof(ItemDetail).GetProperty("BaseUomId")!.PropertyType);
        var endpoint = Read("src", "SESS.NexaERP.Api", "Endpoints", "InventoryEndpoints.cs");
        Assert.Contains("x.BaseUomId, x.Uom", endpoint);
        Assert.DoesNotContain("masters.uoms", endpoint);
    }

    [Fact]
    public void Mir_every_same_row_mutation_advances_version()
    {
        var commands = Read("src", "SESS.NexaERP.Infrastructure", "Stores", "EfMaterialIssueService.RequestCommands.cs");
        Assert.Equal(2, Count(commands, "request.Version = checked(request.Version + 1);"));
        var issue = Read("src", "SESS.NexaERP.Infrastructure", "Stores", "EfMaterialIssueService.Issue.cs");
        Assert.Contains("request.Version = checked(request.Version + 1);", issue);
    }

    [Theory]
    [InlineData("src/SESS.NexaERP.Infrastructure/Purchase/EfPurchaseRequisitionWorkflowService.cs", "pr.Version = checked(pr.Version + 1);")]
    [InlineData("src/SESS.NexaERP.Infrastructure/Stores/EfGateEntryService.cs", "SetProperty(x=>x.Version,nextVersion)")]
    [InlineData("src/SESS.NexaERP.Infrastructure/Persistence/Migrations/StoresGrnSlice2Sql.cs", "\"Version\"=p_expected_version+1")]
    [InlineData("src/SESS.NexaERP.Infrastructure/Stores/EfQcWorkflowService.cs", "concession.Version++")]
    [InlineData("src/SESS.NexaERP.Infrastructure/Persistence/Migrations/VendorBillCostingSql.cs", "\"Version\"=p_version+1")]
    [InlineData("src/SESS.NexaERP.Infrastructure/Stores/EfJobOrderService.cs", "row.Version++")]
    [InlineData("src/SESS.NexaERP.Infrastructure/Persistence/Migrations/JobOrderFatReadinessCommandSql.cs", "\"Version\"=\"Version\"+1")]
    [InlineData("src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.MaterialFollowUp.cs", ".SetProperty(x => x.Version, next)")]
    [InlineData("src/SESS.NexaERP.Infrastructure/Masters/EfTaxGstWorkflowService.cs", "rule.Version = checked(rule.Version + 1);")]
    [InlineData("src/SESS.NexaERP.Api/Endpoints/MasterEndpointHelpers.cs", "entity.Version = checked(entity.Version + 1);")]
    [InlineData("src/SESS.NexaERP.Infrastructure/Stores/EfMaterialIssueService.Return.cs", "row.AcceptanceRequestFingerprint = hash; row.Version++;")]
    public void Established_mutable_workflows_advance_version(string relativePath, string evidence)
    {
        Assert.Contains(evidence, Read(relativePath.Split('/')));
    }

    [Theory]
    [InlineData("src/SESS.NexaERP.Infrastructure/Stores/EfEstimatedBomService.Commands.cs", "revision.Version = checked(revision.Version + 1); bom.Version = checked(bom.Version + 1);")]
    [InlineData("src/SESS.NexaERP.Infrastructure/Stores/EfProductionEngineeringService.ProductionTransition.cs", "revision.Version = checked(revision.Version + 1); bom.Version = checked(bom.Version + 1);")]
    [InlineData("src/SESS.NexaERP.Infrastructure/Stores/EfProductionEngineeringService.DocumentTransition.cs", "revision.Version = checked(revision.Version + 1); document.Version = checked(document.Version + 1);")]
    [InlineData("src/SESS.NexaERP.Infrastructure/Stores/EfProductionEngineeringService.ProductionPin.cs", "bom.JobOrder.Version = checked(bom.JobOrder.Version + 1);")]
    public void Newly_audited_revision_workflows_advance_both_revision_and_parent_tokens(string relativePath, string evidence)
    {
        Assert.Contains(evidence, Read(relativePath.Split('/')));
    }

    private static int Count(string text, string value) =>
        (text.Length - text.Replace(value, string.Empty, StringComparison.Ordinal).Length) / value.Length;

    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine([Root, .. parts]));
    private static readonly string Root = FindRoot();
    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}