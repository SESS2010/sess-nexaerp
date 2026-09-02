using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class Foundation3InventoryProvenanceGenealogyTests
{
    private static readonly string[] Tables =
    [
        "inventory_lot_attribute_revisions", "inventory_provenance_layers", "inventory_transformations",
        "inventory_transformation_inputs", "inventory_transformation_outputs", "inventory_provenance_edges",
        "inventory_serial_identity_revisions", "inventory_serial_genealogy_events", "inventory_serial_genealogy_links",
        "qc_inspection_lot_dispositions", "inventory_concessions", "inventory_concession_allocations",
        "inventory_concession_allocation_serials", "inventory_provenance_annotations",
        "inventory_provenance_goods_receipt_lot_origins", "inventory_provenance_custody_case_line_origins",
        "inventory_provenance_transformation_output_origins", "inventory_provenance_qc_disposition_origins",
        "inventory_provenance_concession_allocation_origins"
    ];

    [Fact]
    public void ModelBuildsWithCompleteProvenanceSetAndMandatoryLedgerDimensions()
    {
        using var db = Context();
        var model = db.GetService<IDesignTimeModel>().Model;
        foreach (var table in Tables)
            Assert.Contains(model.GetEntityTypes(), entity => entity.GetTableName() == table);

        var origin = model.FindEntityType(typeof(InventoryProvenanceOrigin))!;
        Assert.Equal(typeof(InventoryProvenanceOrigin), origin.FindPrimaryKey()!.DeclaringEntityType.ClrType);
        Assert.Equal(5, origin.GetDerivedTypes().Count());

        var movement = model.FindEntityType(typeof(StockMovement))!;
        Assert.False(movement.FindProperty(nameof(StockMovement.OwnershipAccountId))!.IsNullable);
        Assert.False(movement.FindProperty(nameof(StockMovement.CustodyAssignmentId))!.IsNullable);
        Assert.False(movement.FindProperty(nameof(StockMovement.InventoryProvenanceLayerId))!.IsNullable);
        Assert.True(movement.FindProperty(nameof(StockMovement.CustodyCaseLineId))!.IsNullable);
        Assert.True(movement.FindProperty(nameof(StockMovement.InventoryLotId))!.IsNullable);
        Assert.True(movement.FindProperty(nameof(StockMovement.InventorySerialId))!.IsNullable);
        Assert.Equal((short)2, movement.FindProperty(nameof(StockMovement.LedgerSchemaVersion))!.GetDefaultValue());
    }

    [Fact]
    public void MigrationGuardsBothDirectionsAndCarriesLedgerChangeInOneFile()
    {
        var path = Directory.GetFiles(
            Path.Combine(FindRoot(), "src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations"),
            "*Foundation3InventoryProvenanceGenealogy.cs").Single();
        var migration = File.ReadAllText(path);
        Assert.Equal(2, Count(migration, "PostgreSqlClusterGuard.Require(migrationBuilder);"));
        Assert.Contains("Foundation3InventoryProvenanceGenealogySql.PreUp", migration, StringComparison.Ordinal);
        Assert.Contains("Foundation3InventoryProvenanceGenealogySql.UpContract", migration, StringComparison.Ordinal);
        Assert.Contains("Foundation3InventoryProvenanceGenealogySql.ControlledPosting", migration, StringComparison.Ordinal);
        Assert.Contains("Foundation3InventoryProvenanceGenealogySql.DownContract", migration, StringComparison.Ordinal);
        Assert.Equal(15, Count(migration, "migrationBuilder.AddColumn<Guid>("));
        Assert.Contains(nameof(StockMovement.OwnershipAccountId), migration, StringComparison.Ordinal);
        Assert.Contains(nameof(StockMovement.CustodyAssignmentId), migration, StringComparison.Ordinal);
        Assert.Contains(nameof(StockMovement.InventoryProvenanceLayerId), migration, StringComparison.Ordinal);
        Assert.DoesNotContain("00000000-0000-0000-0000-000000000000", migration, StringComparison.Ordinal);
    }

    [Fact]
    public void StockLedgerSqlIsFailClosedAndUsesCanonicalFoundation3LockOrder()
    {
        var sql = File.ReadAllText(Path.Combine(FindRoot(), "src", "SESS.NexaERP.Infrastructure",
            "Persistence", "Migrations", "Foundation3InventoryProvenanceGenealogySql.cs"));

        Assert.Contains("requires zero stock_movements", sql, StringComparison.Ordinal);
        Assert.Contains("standalone inventory_lots", sql, StringComparison.Ordinal);
        Assert.Contains("standalone inventory_serials", sql, StringComparison.Ordinal);
        Assert.Contains("Effective lot policy requires InventoryLotId", sql, StringComparison.Ordinal);
        Assert.Contains("Effective serial policy requires InventorySerialId", sql, StringComparison.Ordinal);
        Assert.Contains("foundation3_prepare_grn_legs", sql, StringComparison.Ordinal);
        Assert.Contains("source -> ownership -> custody -> provenance -> item -> lot -> location -> serial", sql, StringComparison.Ordinal);
        Assert.Contains("'10:OWN:'", sql, StringComparison.Ordinal);
        Assert.Contains("'20:CUST:'", sql, StringComparison.Ordinal);
        Assert.Contains("'30:PROV:'", sql, StringComparison.Ordinal);
        Assert.Contains("'50:LOT:'", sql, StringComparison.Ordinal);
        Assert.Contains("'70:SER:'", sql, StringComparison.Ordinal);
        Assert.Contains(nameof(StockMovement.InventoryConcessionAllocationId), sql, StringComparison.Ordinal);
        Assert.Contains("rollback refuses persisted ledger, provenance, genealogy, QC or concession evidence", sql, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Up")]
    [InlineData("Down")]
    public void MigrationRejectsNonPostgreSql(string methodName)
    {
        var migration = new SESS.NexaERP.Infrastructure.Persistence.Migrations.Foundation3InventoryProvenanceGenealogy();
        var method = migration.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        var error = Assert.Throws<TargetInvocationException>(
            () => method.Invoke(migration, [new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite")]));
        Assert.IsType<NotSupportedException>(error.InnerException);
    }

    private static NexaErpDbContext Context() => new(
        new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);

    private static int Count(string text, string value)
    {
        var count = 0;
        for (var offset = 0; (offset = text.IndexOf(value, offset, StringComparison.Ordinal)) >= 0; offset += value.Length)
            count++;
        return count;
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException();
    }
}
