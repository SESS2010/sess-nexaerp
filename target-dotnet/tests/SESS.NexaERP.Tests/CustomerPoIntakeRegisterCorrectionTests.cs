using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Domain.Sales;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Persistence.Migrations;

namespace SESS.NexaERP.Tests;

public sealed class CustomerPoIntakeRegisterCorrectionTests
{
    [Fact]
    public void IntakeIdentityIsMandatoryAndAccountsFieldsAreAbsent()
    {
        using var db = Context();
        var model = db.GetService<IDesignTimeModel>().Model;
        var po = model.FindEntityType(typeof(CustomerPurchaseOrder))!;

        Assert.False(po.FindProperty(nameof(CustomerPurchaseOrder.CustomerId))!.IsNullable);
        Assert.False(po.FindProperty(nameof(CustomerPurchaseOrder.CompanyId))!.IsNullable);
        foreach (var removed in new[] { "CustomerName", "InvoiceNumber", "InvoiceDate", "FinalInvoiceDate", "InvoiceFileId", "InvoiceFileName", "PaymentStatus" })
            Assert.Null(po.FindProperty(removed));

        var revision = model.FindEntityType(typeof(CustomerPurchaseOrderRevision))!;
        Assert.Equal("customer_purchase_order_revisions", revision.GetTableName());
        Assert.Equal("jsonb", revision.FindProperty(nameof(CustomerPurchaseOrderRevision.SnapshotJson))!.GetColumnType());
        Assert.Contains(revision.GetKeys(), key => key.Properties.Select(x => x.Name).SequenceEqual([
            nameof(CustomerPurchaseOrderRevision.CustomerPurchaseOrderId),
            nameof(CustomerPurchaseOrderRevision.RevisionNumber)]));
    }

    [Fact]
    public void RevisionsAreAppendOnlyAndPrLinkIsNullableButCompanyBound()
    {
        using var db = Context();
        var model = db.GetService<IDesignTimeModel>().Model;
        var line = model.FindEntityType(typeof(CustomerPurchaseOrderLine))!;
        Assert.Contains(line.GetIndexes(), index => index.IsUnique && index.Properties.Select(x => x.Name).SequenceEqual([
            nameof(CustomerPurchaseOrderLine.CustomerPurchaseOrderId),
            nameof(CustomerPurchaseOrderLine.RevisionNumber),
            nameof(CustomerPurchaseOrderLine.SlNo)]));

        var pr = model.FindEntityType(typeof(PurchaseRequisition))!;
        Assert.True(pr.FindProperty(nameof(PurchaseRequisition.CustomerPurchaseOrderId))!.IsNullable);
        Assert.Contains(pr.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(CustomerPurchaseOrder) &&
            fk.Properties.Select(x => x.Name).SequenceEqual([
                nameof(PurchaseRequisition.CustomerPurchaseOrderId),
                nameof(PurchaseRequisition.CompanyId)]) &&
            fk.PrincipalKey.Properties.Select(x => x.Name).SequenceEqual([
                nameof(CustomerPurchaseOrder.Id),
                nameof(CustomerPurchaseOrder.CompanyId)]));
    }

    [Fact]
    public void MigrationGuardsBothDirectionsBackfillsHistoryAndRefusesSilentAccountsDataLoss()
    {
        var source = ReadMigration();
        Assert.Equal(2, Count(source, "PostgreSqlClusterGuard.Require(migrationBuilder);"));
        Assert.Contains("will not discard legacy Accounts data", source);
        Assert.Contains("repair unmapped rows before retrying", source);
        Assert.Contains("Migration baseline", source);
        Assert.Contains("jsonb_agg(to_jsonb(line)", source);
        Assert.Contains("customer_po_revisions_append_only", source);
        Assert.Contains("customer_po_revision_lines_append_only", source);
        Assert.DoesNotContain("00000000-0000-0000-0000-000000000000", source);
    }

    [Theory]
    [InlineData("Up")]
    [InlineData("Down")]
    public void MigrationClusterGuardRejectsNonPostgreSqlInBothDirections(string methodName)
    {
        var migration = new CorrectCustomerPoIntakeRevisionsAndPrLink();
        var method = migration.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        var error = Assert.Throws<TargetInvocationException>(() => method.Invoke(migration, [new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite")]));
        Assert.IsType<NotSupportedException>(error.InnerException);
    }

    [Fact]
    public void EndpointAppendsRevisionsScopesBySessionAndOwnsNoInvoiceWorkflow()
    {
        var root = FindRoot();
        var endpoint = File.ReadAllText(Path.Combine(root, "src", "SESS.NexaERP.Api", "Endpoints", "CustomerPoEndpoints.cs"));
        Assert.Contains("AppendRevision(entity", endpoint);
        Assert.Contains("entity.CurrentRevisionNumber++", endpoint);
        Assert.Contains("po.Company.Code == user.OrganizationId", endpoint);
        Assert.DoesNotContain("RemoveRange", endpoint);
        Assert.DoesNotContain("/invoice", endpoint, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PaymentStatus", endpoint, StringComparison.Ordinal);
    }

    [Fact]
    public void SchemaNoteDefinesIntakeBoundaryAndCostRollupGaps()
    {
        var note = File.ReadAllText(Path.Combine(FindRoot(), "outputs", "customer_po_intake_register_schema.md"));
        Assert.Contains("NOT the canonical Sales model", note);
        Assert.Contains("accepted vendor bill allocation", note);
        Assert.Contains("Actual BOM", note);
        Assert.Contains("offer revision", note, StringComparison.OrdinalIgnoreCase);
    }

    private static NexaErpDbContext Context() => new(new DbContextOptionsBuilder<NexaErpDbContext>()
        .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);

    private static string ReadMigration() => File.ReadAllText(Directory.GetFiles(
        Path.Combine(FindRoot(), "src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations"),
        "*CorrectCustomerPoIntakeRevisionsAndPrLink.cs").Single());

    private static int Count(string text, string value) =>
        (text.Length - text.Replace(value, string.Empty, StringComparison.Ordinal).Length) / value.Length;

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException();
    }
}