using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Sales;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed partial class NexaErpDbContext
{
    public DbSet<CustomerPurchaseOrder> CustomerPurchaseOrders => Set<CustomerPurchaseOrder>();
    public DbSet<CustomerPurchaseOrderLine> CustomerPurchaseOrderLines => Set<CustomerPurchaseOrderLine>();
    public DbSet<CustomerPurchaseOrderRevision> CustomerPurchaseOrderRevisions => Set<CustomerPurchaseOrderRevision>();
    public DbSet<CustomerPoFile> CustomerPoFiles => Set<CustomerPoFile>();
    public DbSet<CustomerPoOption> CustomerPoOptions => Set<CustomerPoOption>();

    private static void ConfigureSales(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerPurchaseOrder>(entity =>
        {
            entity.ToTable("customer_purchase_orders");
            entity.Property(x => x.PoRecordNumber).HasMaxLength(40).IsRequired();
            entity.HasIndex(x => x.PoRecordNumber).IsUnique();
            entity.HasAlternateKey(x => new { x.Id, x.CompanyId });
            entity.Property(x => x.CustomerPoNumber).HasMaxLength(120).IsRequired();
            entity.HasIndex(x => x.CustomerPoNumber);
            entity.Property(x => x.QuoteNumber).HasMaxLength(160);
            entity.Property(x => x.ServiceMode).HasMaxLength(40);
            entity.Property(x => x.SalesType).HasMaxLength(60);
            entity.Property(x => x.WorkStatus).HasMaxLength(40).IsRequired();
            entity.Property(x => x.TotalAmountWithGst).HasPrecision(18, 2);
            entity.Property(x => x.PaymentTerms).HasMaxLength(300);
            entity.Property(x => x.ModeOfDelivery).HasMaxLength(200);
            entity.Property(x => x.FiscalYear).HasMaxLength(10);
            entity.HasIndex(x => x.FiscalYear);
            entity.HasIndex(x => x.WorkStatus);
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict).IsRequired();
            entity.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict).IsRequired();
            entity.Property(x => x.ReferenceNumber).HasMaxLength(160);
            entity.Property(x => x.OtherReferences).HasMaxLength(200);
            entity.Property(x => x.Destination).HasMaxLength(200);
            entity.Property(x => x.DeliveryTerms).HasMaxLength(300);
            entity.Property(x => x.TaxableValue).HasPrecision(18, 2);
            entity.Property(x => x.CgstPercent).HasPrecision(5, 2);
            entity.Property(x => x.CgstAmount).HasPrecision(18, 2);
            entity.Property(x => x.SgstPercent).HasPrecision(5, 2);
            entity.Property(x => x.SgstAmount).HasPrecision(18, 2);
            entity.Property(x => x.IgstPercent).HasPrecision(5, 2);
            entity.Property(x => x.IgstAmount).HasPrecision(18, 2);
            entity.Property(x => x.RoundOff).HasPrecision(18, 2);
            entity.Property(x => x.AmountInWords).HasMaxLength(400);
            entity.Property(x => x.PoFileName).HasMaxLength(260);
            entity.Property(x => x.CurrentRevisionNumber).IsRequired();
            entity.ToTable(table => table.HasCheckConstraint("CK_customer_purchase_orders_current_revision", "\"CurrentRevisionNumber\" >= 1"));
        });

        modelBuilder.Entity<CustomerPurchaseOrderLine>(entity =>
        {
            entity.ToTable("customer_purchase_order_lines");
            entity.Property(x => x.Description).IsRequired();
            entity.Property(x => x.Uom).HasMaxLength(20);
            entity.Property(x => x.Quantity).HasPrecision(18, 3);
            entity.Property(x => x.Rate).HasPrecision(18, 2);
            entity.Property(x => x.DiscountPercent).HasPrecision(5, 2);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.CustomerPurchaseOrderId, x.RevisionNumber, x.SlNo }).IsUnique();
            entity.HasOne(x => x.CustomerPurchaseOrder).WithMany(x => x.Lines)
                .HasForeignKey(x => x.CustomerPurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Revision).WithMany(x => x.Lines)
                .HasForeignKey(x => new { x.CustomerPurchaseOrderId, x.RevisionNumber })
                .HasPrincipalKey(x => new { x.CustomerPurchaseOrderId, x.RevisionNumber })
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CustomerPurchaseOrderRevision>(entity =>
        {
            entity.ToTable("customer_purchase_order_revisions");
            entity.HasAlternateKey(x => new { x.CustomerPurchaseOrderId, x.RevisionNumber });
            entity.Property(x => x.ChangeReason).HasMaxLength(500).IsRequired();
            entity.Property(x => x.SnapshotJson).HasColumnType("jsonb").IsRequired();
            entity.HasOne(x => x.CustomerPurchaseOrder).WithMany(x => x.Revisions)
                .HasForeignKey(x => x.CustomerPurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(table => table.HasCheckConstraint("CK_customer_purchase_order_revisions_number", "\"RevisionNumber\" >= 1"));
        });

        modelBuilder.Entity<CustomerPoFile>(entity =>
        {
            entity.ToTable("customer_po_files");
            entity.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Content).IsRequired();
        });

        modelBuilder.Entity<CustomerPoOption>(entity =>
        {
            entity.ToTable("customer_po_options");
            entity.Property(x => x.Kind).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Value).HasMaxLength(60).IsRequired();
            entity.HasIndex(x => new { x.Kind, x.Value }).IsUnique();
        });
        modelBuilder.Entity<CustomerPoOption>().HasData(SalesSeedData.CustomerPoOptions);
        modelBuilder.Entity<Domain.Authorization.PageDefinition>().HasData(SalesSeedData.Pages);
        modelBuilder.Entity<Domain.Authorization.RolePagePermission>().HasData(SalesSeedData.RolePagePermissions);
    }
}