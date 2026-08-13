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
    private static readonly string Service = Read("src", "SESS.NexaERP.Infrastructure", "Purchase", "EfRev869BPurchaseService.cs") +
        Read("src", "SESS.NexaERP.Infrastructure", "Purchase", "EfRev869BPurchaseService.RfqQuotation.cs") +
        Read("src", "SESS.NexaERP.Infrastructure", "Purchase", "EfRev869BPurchaseService.ComparisonPo.cs") +
        Read("src", "SESS.NexaERP.Infrastructure", "Purchase", "EfRev869BPurchaseService.MaterialFollowUp.cs");
    private static readonly string Api = Read("src", "SESS.NexaERP.Api", "Endpoints", "Rev869BPurchaseEndpoints.cs");
    private static readonly string Migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.cs");
    private static readonly string MigrationInstall = Migration +
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
            ("Draft", "PendingApproval"), ("Draft", "Cancelled"), ("PendingApproval", "Approved"), ("PendingApproval", "Rejected"),
            ("PendingApproval", "RevisionRequested"), ("RevisionRequested", "PendingApproval"), ("RevisionRequested", "Cancelled"));
        AssertMatrix(Rev869BStatusContracts.PurchaseOrder, Rev869BStatusContracts.RequirePurchaseOrder,
            ("Draft", "PendingApproval"), ("Draft", "Cancelled"), ("PendingApproval", "Approved"), ("PendingApproval", "Rejected"),
            ("PendingApproval", "Cancelled"), ("Rejected", "RevisionDraft"), ("RevisionDraft", "Resubmitted"), ("RevisionDraft", "Cancelled"),
            ("Resubmitted", "Approved"), ("Resubmitted", "Rejected"), ("Resubmitted", "Cancelled"),
            ("Approved", "Issued"), ("Approved", "Cancelled"), ("Issued", "Superseded"), ("Issued", "Cancelled"));
        AssertMatrix(Rev869BStatusContracts.MaterialFollowUp, Rev869BStatusContracts.RequireMaterialFollowUp,
            ("PendingFollowUp", "InProgress"), ("InProgress", "Completed"));
    }

    [Fact]
    public void DatabaseStatusSetsMatchCanonicalAggregateSets()
    {
        Assert.Equal(Rev869BStatusContracts.Quotation.Order(), CanonicalConstraintValues("CK_vendor_quotation_status").Order());
        Assert.Equal(Rev869BStatusContracts.Comparison.Order(), CanonicalConstraintValues("CK_comparison_status").Order());
        Assert.Equal(Rev869BStatusContracts.PurchaseOrder.Order(), CanonicalConstraintValues("CK_purchase_order_status").Order());
        Assert.Equal(Rev869BStatusContracts.MaterialFollowUp.Order(), CanonicalConstraintValues("CK_material_followup_quantity").Where(x => x != "OrderedQuantitySnapshot").Order());
        Assert.DoesNotContain("Recommended", CanonicalConstraintValues("CK_comparison_status"));
        Assert.DoesNotContain("PendingReapproval", Migration + Service);
        Assert.DoesNotContain("PendingTechnicalVerification", Migration + Service);
    }

    [Fact]
    public void CommercialBoundariesAndMaximumAreDeterministic()
    {
        foreach (var pair in new[] { (0m, "MANAGER"), (49999.999999m, "MANAGER"), (50000m, "MANAGER"), (50000.000001m, "TECHNICAL_DIRECTOR"), (499999.999999m, "TECHNICAL_DIRECTOR"), (500000m, "TECHNICAL_DIRECTOR"), (500000.000001m, "MANAGING_DIRECTOR") })
            Assert.Equal(pair.Item2, Rev869BApprovalRoutes.Resolve(pair.Item1, Rev869BSeedData.ApprovalPolicies, new DateOnly(2026, 8, 11), "SESS"));
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
        Assert.Equal(29, rows.Count);
        Assert.All(rows, row => Assert.True(row.CanView || row.CanCreate || row.CanUpdate || row.CanSubmit || row.CanIssue || row.CanVerify || row.CanApprove || row.CanReject || row.CanRequestClarification || row.CanRequestRevision || row.CanResubmit || row.CanCancel || row.CanPrint || row.CanDownload || row.CanExport || row.CanUploadAttachment || row.CanViewCommercialValues || row.CanViewAuditHistory || row.HasFullControl));
        Assert.All(rows.Where(row => row.CanViewCommercialValues || row.CanExport || row.CanViewAuditHistory), row => Assert.True(row.CanView));
        var poPage = Guid.Parse("20000000-0000-0000-0000-000000000012");
        Assert.Equal(2, rows.Count(row => row.PageDefinitionId == poPage && row.CanApprove && row.CanReject));
        var managerPo = rows.Single(row => row.Id == Rev869BSeedData.PermissionId(Rev869ARoleCodes.PurchaseManager, "purchase.po"));
        Assert.True(managerPo.CanView && managerPo.CanCreate && managerPo.CanUpdate && managerPo.CanSubmit && managerPo.CanResubmit && managerPo.CanIssue);
        Assert.False(managerPo.CanApprove || managerPo.CanReject || managerPo.CanRequestRevision || managerPo.HasFullControl);
        var mdPo = rows.Single(row => row.Id == Rev869BSeedData.PermissionId(Rev869ARoleCodes.ManagingDirector, "purchase.po"));
        Assert.True(mdPo.CanView && mdPo.CanApprove && mdPo.CanReject && mdPo.CanViewCommercialValues && mdPo.CanViewAuditHistory);
        Assert.False(mdPo.CanCreate || mdPo.CanUpdate || mdPo.CanSubmit || mdPo.CanResubmit || mdPo.CanIssue);
    }

    [Fact]
    public void MigrationOwnsImmutableAndCrossParentFailClosedGuards()
    {
        Assert.Equal(79, Count(MigrationInstall, "CREATE TRIGGER trg_rev869b_") + Count(MigrationInstall, "CREATE CONSTRAINT TRIGGER trg_rev869b_"));
        Assert.Equal(2, Count(MigrationInstall, "CREATE TRIGGER trg_rev869b_down_"));
        Assert.Contains("rev869b_guard_controlled_snapshot", Migration);
        Assert.Contains("rev869b_enforce_transition", Migration);
        Assert.Contains("Purchase order pre-issue snapshot is incomplete or does not reconcile", Migration);
        Assert.Contains("rev869b_validate_parent_contract", Migration);
        foreach (var message in new[] { "Quotation line parent contract mismatch", "Comparison line parent contract mismatch", "Purchase order parent contract mismatch", "Purchase order line parent contract mismatch", "Material follow-up parent contract mismatch" }) Assert.Contains(message, Migration);
        Assert.Contains("DROP FUNCTION IF EXISTS nexa.rev869b_validate_parent_contract", Migration);
        Assert.Contains("DROP FUNCTION IF EXISTS nexa.rev869b_guard_controlled_snapshot", Migration);
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
    public void MigrationRetainsExactSourceOwnedSeedCountsAndNoBusinessSeeds()
    {
        var normalizedCorrect = Migration.Replace(string.Concat((char)13, (char)10), string.Concat((char)10));
        var permissionInsertCorrect = string.Join((char)10, "migrationBuilder.InsertData(", "                schema: \"nexa\",", "                table: \"role_page_permissions\"");
        var permissionStart = normalizedCorrect.IndexOf(permissionInsertCorrect, StringComparison.Ordinal);
        Assert.True(permissionStart >= 0);
        var permissionBlock = normalizedCorrect[permissionStart..normalizedCorrect.IndexOf("migrationBuilder.Sql(", permissionStart, StringComparison.Ordinal)];
        Assert.Equal(29, Regex.Matches(permissionBlock, @"(?m)^\s*\{ new Guid\(").Count);
        Assert.Contains("DEPARTMENT_MANAGER", Migration);
        foreach (var prohibited in new[] { "INSERT INTO nexa.vendors", "INSERT INTO nexa.employees", "INSERT INTO nexa.vendor_quotations", "INSERT INTO nexa.purchase_orders" }) Assert.DoesNotContain(prohibited, Migration, StringComparison.OrdinalIgnoreCase);
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
    public void RetainedMigrationGeneratedSqlHasExactOfflineSyntaxAndObjectContracts()
    {
        const string rev869A = "20260810120000_Rev869AIdentityMasterScopeFoundation";
        const string rev869B = "20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation";
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=rev869b_no_connect;Username=no_connect")
            .Options;
        using var db = new NexaErpDbContext(options);
        var migrator = db.GetService<IMigrator>();
        var up = migrator.GenerateScript(rev869A, rev869B);
        var down = migrator.GenerateScript(rev869B, rev869A);

        Assert.Contains("""CONSTRAINT "CK_purchase_transaction_policy_dates" CHECK ("EffectiveTo" IS NULL OR "EffectiveTo" >= "EffectiveFrom")""", up);
        Assert.DoesNotContain("""CHECK ("EffectiveTo" IS NULL OR "EffectiveTo" >= "EffectiveFrom)""", up);
        Assert.Equal(22, Regex.Matches(up, @"(?im)^CREATE TABLE nexa\.").Count);
        Assert.Equal(79, Regex.Matches(up, @"(?im)^CREATE (?:CONSTRAINT )?TRIGGER\s+").Count);
        Assert.Equal(32, Regex.Matches(up, @"(?im)^CREATE OR REPLACE FUNCTION\s+nexa\.").Count);
        Assert.Equal(31, Regex.Matches(up, @"(?im)^CREATE OR REPLACE FUNCTION\s+nexa\.([^\s(]+)")
            .Select(x => x.Groups[1].Value).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(0, Regex.Matches(up, @"\$rev869b\$").Count % 2);
        Assert.Equal(0, Regex.Matches(up, @"\$rev869b_extension\$").Count % 2);
        Assert.Equal(0, Regex.Matches(up, @"\$rev869b_owner\$").Count % 2);
        Assert.Equal(0, Regex.Matches(up, @"\$rev869b_grant_owner\$").Count % 2);
        foreach (var function in new[] { "rev869b_open_command_context", "rev869b_claim_command_context",
            "rev869b_register_purge_authorization", "rev869b_begin_purge_execution",
            "rev869b_purge_temporary_security_ledger", "rev869b_record_purge_failure",
            "rev869b_guard_history_insert", "rev869b_guard_qualification_history_insert",
            "rev869b_require_qualification_history", "rev869b_guard_child_insert",
            "rev869b_enforce_transition", "rev869b_enforce_quotation_transition" })
        {
            Assert.Contains($"FUNCTION nexa.{function}", up);
        }
        Assert.Contains("SET search_path=pg_catalog,nexa", up);
        Assert.Contains("SET search_path = pg_catalog, nexa", up);
        Assert.True(down.IndexOf("DROP FUNCTION IF EXISTS nexa.rev869b_provision_command_authority", StringComparison.Ordinal) <
                    down.IndexOf("DROP TABLE IF EXISTS nexa.rev869b_command_contexts", StringComparison.Ordinal));
        Assert.True(down.IndexOf("DROP TABLE IF EXISTS nexa.rev869b_command_contexts", StringComparison.Ordinal) <
                    down.IndexOf("DROP TABLE IF EXISTS nexa.rev869b_command_grants", StringComparison.Ordinal));
        Assert.True(down.IndexOf("DROP TABLE IF EXISTS nexa.rev869b_command_grants", StringComparison.Ordinal) <
                    down.IndexOf("DROP TABLE IF EXISTS nexa.rev869b_command_authorities", StringComparison.Ordinal));
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
