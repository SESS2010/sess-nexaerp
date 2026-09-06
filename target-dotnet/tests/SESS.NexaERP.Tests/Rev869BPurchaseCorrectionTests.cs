
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class Rev869BPurchaseCorrectionTests
{
    private static readonly string Root = FindRoot();
    private static string Service => Read("src", "SESS.NexaERP.Infrastructure", "Purchase", "EfRev869BPurchaseService.cs") +
        Read("src", "SESS.NexaERP.Infrastructure", "Purchase", "EfRev869BPurchaseService.RfqQuotation.cs") +
        Read("src", "SESS.NexaERP.Infrastructure", "Purchase", "EfRev869BPurchaseService.ComparisonPo.cs") +
        Read("src", "SESS.NexaERP.Infrastructure", "Purchase", "EfRev869BPurchaseService.MaterialFollowUp.cs");
    private static string Api => Read("src", "SESS.NexaERP.Api", "Endpoints", "Rev869BPurchaseEndpoints.cs");
    private static string Migration => Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.cs");
    private static string MigrationInstall => Migration +
        Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "Rev869BDatabaseSafetySql.cs") +
        Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "Rev869BDatabaseLifecycleSql.cs") +
        Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "Rev869BControlledMutationSql.cs");

    [Fact]
    public void EveryCanonicalTransitionAcceptsOnlyItsMatrixEdges()
    {
        AssertMatrix(Rev869BStatusContracts.Rfq, Rev869BStatusContracts.RequireRfq,
            ("Draft", "Issued"), ("Draft", "Cancelled"), ("Issued", "Closed"), ("Issued", "Cancelled"));
        AssertMatrix(Rev869BStatusContracts.Invitation, Rev869BStatusContracts.RequireInvitation,
            ("Issued", "Submitted"), ("Issued", "Withdrawn"), ("Issued", "Cancelled"));
        AssertMatrix(Rev869BStatusContracts.Quotation, Rev869BStatusContracts.RequireQuotation,
            ("Draft", "Submitted"), ("Submitted", "TechnicallyCompliant"), ("Submitted", "TechnicallyRejected"), ("Submitted", "Superseded"), ("Submitted", "Withdrawn"),
            ("TechnicallyCompliant", "Superseded"), ("TechnicallyCompliant", "Withdrawn"),
            ("TechnicallyRejected", "Superseded"), ("TechnicallyRejected", "Withdrawn"), ("TechnicallyRejected", "Rejected"));
        AssertMatrix(Rev869BStatusContracts.Comparison, Rev869BStatusContracts.RequireComparison,
            ("Draft", "PendingApproval"), ("Draft", "Cancelled"), ("PendingApproval", "PendingApproval"), ("PendingApproval", "Approved"), ("PendingApproval", "Rejected"),
            ("PendingApproval", "RevisionRequested"), ("RevisionRequested", "PendingApproval"), ("RevisionRequested", "Cancelled"));
        AssertMatrix(Rev869BStatusContracts.PurchaseOrder, Rev869BStatusContracts.RequirePurchaseOrder,
            ("Draft", "PendingApproval"), ("Draft", "Cancelled"), ("PendingApproval", "PendingApproval"), ("PendingApproval", "Approved"), ("PendingApproval", "Rejected"),
            ("PendingApproval", "Cancelled"), ("Rejected", "RevisionDraft"), ("RevisionDraft", "Resubmitted"), ("RevisionDraft", "Cancelled"),
            ("Resubmitted", "Resubmitted"), ("Resubmitted", "Approved"), ("Resubmitted", "Rejected"), ("Resubmitted", "Cancelled"),
            ("Approved", "Issued"), ("Approved", "Cancelled"), ("Issued", "Superseded"), ("Issued", "Cancelled"));
        AssertMatrix(Rev869BStatusContracts.MaterialFollowUp, Rev869BStatusContracts.RequireMaterialFollowUp,
            ("PendingFollowUp", "InProgress"), ("InProgress", "Completed"));
    }


    [Fact]
    public void CommercialBoundariesAndMaximumAreDeterministic()
    {
        foreach (var pair in new[] { (0m, "MANAGER"), (49999.999999m, "MANAGER"), (50000m, "MANAGER"), (50000.000001m, "TECHNICAL_DIRECTOR"), (499999.999999m, "TECHNICAL_DIRECTOR"), (500000m, "TECHNICAL_DIRECTOR"), (500000.000001m, "MANAGING_DIRECTOR") })
            Assert.Equal(pair.Item2, Rev869BApprovalRoutes.Resolve(pair.Item1, Rev869BSeedData.ApprovalPolicies, new DateOnly(2026, 8, 11), "SESS_PVT_LTD"));
        var maximum = Rev869BCommercialCalculator.Calculate(new(1m, Rev869BCommercialCalculator.MaximumSupportedValue, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 6));
        Assert.Equal(Rev869BCommercialCalculator.MaximumSupportedValue, maximum.TotalPayableValue);
        Assert.Throws<InvalidOperationException>(() => Rev869BCommercialCalculator.Calculate(new(1m, Rev869BCommercialCalculator.MaximumSupportedValue + 1m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 6)));
    }

    [Fact]
    public void EveryMaterialExistingAggregateCommandCarriesExpectedVersion()
    {
        var contracts = new[]
        {
            typeof(Rev869BInviteVendorRequest), typeof(Rev869BSubmitQuotationRequest), typeof(Rev869BTechnicalVerificationRequest),
            typeof(Rev869BCreateComparisonRequest), typeof(Rev869BRecommendComparisonRequest), typeof(Rev869BApprovalActionRequest),
            typeof(Rev869BCreatePurchaseOrderRequest), typeof(Rev869BSubmitPurchaseOrderRequest), typeof(Rev869BIssuePurchaseOrderRequest),
            typeof(Rev869BAmendPurchaseOrderRequest), typeof(Rev869BPoApprovalActionRequest), typeof(Rev869BCancelPurchaseOrderRequest)
        };
        Assert.All(contracts, type => Assert.Contains(type.GetProperties(), property => property.Name.Contains("Version", StringComparison.Ordinal)));
        Assert.Contains("ExecuteUpdateAsync", Service);
        Assert.Contains("x.Version == expected", Service);
        Assert.Contains("throw new DbUpdateConcurrencyException", Service);
        Assert.Contains("catch (DbUpdateConcurrencyException", Api);
    }

    [Fact]
    public void PermissionMatrixHasNoEmptyOrCommercialOnlyLeakageAndPoApproversExist()
    {
        var rows = Rev869BSeedData.RolePagePermissions;
        Assert.Equal(25, rows.Count);
        Assert.All(rows, row => Assert.True(row.CanView || row.CanCreate || row.CanUpdate || row.CanSubmit || row.CanIssue || row.CanVerify || row.CanApprove || row.CanReject || row.CanRequestClarification || row.CanRequestRevision || row.CanResubmit || row.CanCancel || row.CanPrint || row.CanDownload || row.CanExport || row.CanUploadAttachment || row.CanViewCommercialValues || row.CanViewAuditHistory || row.HasFullControl));
        Assert.All(rows.Where(row => row.CanViewCommercialValues || row.CanExport || row.CanViewAuditHistory), row => Assert.True(row.CanView));
        var poPage = Guid.Parse("20000000-0000-0000-0000-000000000012");
        Assert.Equal(2, rows.Count(row => row.PageDefinitionId == poPage && row.CanApprove && row.CanReject));
        var managerPo = rows.Single(row => row.Id == Rev869BSeedData.PermissionId(Rev869ARoleCodes.PurchaseManager, "purchase.po"));
        Assert.True(managerPo.CanView && managerPo.CanCreate && managerPo.CanUpdate && managerPo.CanSubmit && managerPo.CanIssue);
        Assert.False(managerPo.CanResubmit);
        Assert.False(managerPo.CanApprove || managerPo.CanReject || managerPo.CanRequestRevision || managerPo.HasFullControl);
        var mdPo = rows.Single(row => row.Id == Rev869BSeedData.PermissionId(Rev869ARoleCodes.ManagingDirector, "purchase.po"));
        Assert.True(mdPo.CanView && mdPo.CanApprove && mdPo.CanReject && mdPo.CanViewCommercialValues && mdPo.CanViewAuditHistory);
        Assert.False(mdPo.CanCreate || mdPo.CanUpdate || mdPo.CanSubmit || mdPo.CanResubmit || mdPo.CanIssue);
    }


    [Fact]
    public void ApiDistinguishesFailureSemanticsMasksCommercialValuesAndBoundsFollowup()
    {
        Assert.Contains("Rev869BValidationException", Api); Assert.Contains("Results.BadRequest", Api);
        Assert.Contains("Rev869BNotFoundException", Api); Assert.Contains("Results.NotFound", Api);
        Assert.Contains("Rev869BConflictException", Api); Assert.Contains("Results.Conflict", Api);
        Assert.Contains("PagePermissionActions.ViewCommercialValues", Api); Assert.Contains("Lines = row.Lines.Select", Api);
        Assert.Contains("take is < 1 or > 100", Api); Assert.Contains("Take(take)", Api);
        Assert.Contains("audit.WriteAsync(\"Security\", \"Denied\"", Api);
    }


    [Fact]
    public void CurrentDesignTimeModelAndSnapshotHaveNoDifferencesWithoutConnecting()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=rev869b_no_connect;Username=no_connect")
            .Options;
        using var db = new NexaErpDbContext(options);
        var snapshotType = typeof(NexaErpDbContext).Assembly.GetType("SESS.NexaERP.Infrastructure.Persistence.Migrations.NexaErpDbContextModelSnapshot", throwOnError: true)!;
        var snapshot = (ModelSnapshot)Activator.CreateInstance(snapshotType, nonPublic: true)!;
        var current = db.GetService<IDesignTimeModel>().Model;
        var differ = db.GetService<IMigrationsModelDiffer>();
        var initializedSnapshot = db.GetService<IModelRuntimeInitializer>().Initialize(snapshot.Model, designTime: true);
        Assert.Empty(differ.GetDifferences(initializedSnapshot.GetRelationalModel(), current.GetRelationalModel()));
    }

    [Fact]
    public void OrdinaryLedgerAndRetirementGenerateOfflineWithExpectedObjectContracts()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=rev869b_no_connect;Username=no_connect")
            .Options;
        using var db = new NexaErpDbContext(options);
        var migrator = db.GetService<IMigrator>();
        var latest = db.Database.GetMigrations().Last();
        var up = migrator.GenerateScript("0", latest);
        var down = migrator.GenerateScript(latest, "0");

        Assert.Contains("CREATE TABLE advance.command_requests", up, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE advance.command_receipts", up, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE advance.rev869b_retirement_state", up, StringComparison.Ordinal);
        Assert.Contains("FUNCTION advance.register_command_request", up, StringComparison.Ordinal);
        Assert.Contains("FUNCTION advance.commit_command_receipt", up, StringComparison.Ordinal);
        Assert.Contains("Refusing REV869B retirement: partial installation", up, StringComparison.Ordinal);
        Assert.Contains("DROP TABLE advance.rev869b_retirement_state", down, StringComparison.Ordinal);
        Assert.DoesNotContain("DROP EXTENSION", down, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertMatrix(IReadOnlySet<string> statuses, Action<string, string> require, params (string From, string To)[] allowed)
    {
        var edges = allowed.ToHashSet();
        foreach (var from in statuses)
            foreach (var to in statuses)
                if (edges.Contains((from, to))) require(from, to); else Assert.Throws<InvalidOperationException>(() => require(from, to));
        Assert.Throws<InvalidOperationException>(() => require("UNKNOWN", statuses.First()));
    }

    private static IEnumerable<string> ConstraintValues(string name)
    {
        var line = Migration.Split(new[] { "\\r\\n", "\\n" }, StringSplitOptions.None).SingleOrDefault(x => x.Contains($"CheckConstraint(\"{name}\"", StringComparison.Ordinal));
        Assert.False(string.IsNullOrWhiteSpace(line), $"Missing {name}.");
        return Regex.Matches(line!, "'([^']+)'").Select(x => x.Groups[1].Value);
    }

    private static IEnumerable<string> CanonicalConstraintValues(string name)
    {
        var line = Migration.Split((char)10).SingleOrDefault(x => x.Contains(name, StringComparison.Ordinal));
        Assert.False(string.IsNullOrWhiteSpace(line), $"Missing {name}.");
        return Regex.Matches(line!, "'([^']+)'").Select(x => x.Groups[1].Value);
    }

    private static int Count(string source, string value) => source.Split(value, StringSplitOptions.None).Length - 1;
    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine(new[] { Root }.Concat(parts).ToArray()));
    private static string FindRoot() { var directory = new DirectoryInfo(AppContext.BaseDirectory); while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx"))) directory = directory.Parent; return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found."); }
}
