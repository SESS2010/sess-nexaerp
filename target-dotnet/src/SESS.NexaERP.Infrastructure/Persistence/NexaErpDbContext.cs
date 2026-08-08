using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Audit;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Masters;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed class NexaErpDbContext(DbContextOptions<NexaErpDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<RackBin> RackBins => Set<RackBin>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PageDefinition> PageDefinitions => Set<PageDefinition>();
    public DbSet<RolePagePermission> RolePagePermissions => Set<RolePagePermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("nexa");
        ConfigureIdentity(modelBuilder);
        ConfigureMasters(modelBuilder);
        ConfigureInventory(modelBuilder);
        ConfigureAudit(modelBuilder);
        ConfigureAuthorization(modelBuilder);
        SeedFoundation(modelBuilder);
    }

    private static void ConfigureIdentity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("user_accounts");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.LoginId).IsUnique();
            entity.HasIndex(x => x.Email);
            entity.Property(x => x.LoginId).HasMaxLength(160).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(254);
            entity.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(x => x.UserType).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureMasters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("customers");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.CustomerCode).IsUnique();
            entity.HasIndex(x => x.GstNumber).IsUnique().HasFilter("\"GstNumber\" IS NOT NULL");
            entity.Property(x => x.CustomerCode).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(240).IsRequired();
            entity.Property(x => x.GstNumber).HasMaxLength(32);
            entity.Property(x => x.PanNumber).HasMaxLength(16);
            entity.Property(x => x.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<Vendor>(entity =>
        {
            entity.ToTable("vendors");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.VendorCode).IsUnique();
            entity.HasIndex(x => x.GstNumber).IsUnique().HasFilter("\"GstNumber\" IS NOT NULL");
            entity.Property(x => x.VendorCode).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(240).IsRequired();
            entity.Property(x => x.GstNumber).HasMaxLength(32);
            entity.Property(x => x.PanNumber).HasMaxLength(16);
            entity.Property(x => x.ApprovalStatus).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
        });
    }

    private static void ConfigureInventory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>(entity =>
        {
            entity.ToTable("items");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ItemCode).IsUnique();
            entity.HasIndex(x => x.Barcode).IsUnique().HasFilter("\"Barcode\" IS NOT NULL");
            entity.Property(x => x.ItemCode).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(240).IsRequired();
            entity.Property(x => x.Uom).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Barcode).HasMaxLength(128);
            entity.Property(x => x.ImageStorageKey).HasMaxLength(512);
            entity.Property(x => x.MinimumStock).HasPrecision(18, 3);
            entity.Property(x => x.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.ToTable("warehouses");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.WarehouseCode).IsUnique();
            entity.Property(x => x.WarehouseCode).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Location).HasMaxLength(240);
            entity.Property(x => x.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<RackBin>(entity =>
        {
            entity.ToTable("rack_bins");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.WarehouseId, x.BinCode }).IsUnique();
            entity.Property(x => x.BinCode).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(240);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.ToTable("stock_movements");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ItemId, x.PostingDate });
            entity.HasIndex(x => new { x.ReferenceType, x.ReferenceNumber });
            entity.Property(x => x.MovementType).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ReferenceType).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ReferenceNumber).HasMaxLength(120).IsRequired();
            entity.Property(x => x.QuantityIn).HasPrecision(18, 3);
            entity.Property(x => x.QuantityOut).HasPrecision(18, 3);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RackBin).WithMany().HasForeignKey(x => x.RackBinId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureAudit(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.Module, x.EntityName, x.EntityId });
            entity.HasIndex(x => x.CreatedAt);
            entity.Property(x => x.Module).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(80).IsRequired();
            entity.Property(x => x.EntityName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.EntityId).HasMaxLength(120).IsRequired();
            entity.Property(x => x.UserLoginId).HasMaxLength(160).IsRequired();
            entity.Property(x => x.IpAddress).HasMaxLength(80);
            entity.Property(x => x.Version).IsConcurrencyToken();
        });
    }
    private static void ConfigureAuthorization(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PageDefinition>(entity =>
        {
            entity.ToTable("page_definitions");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.PageKey).IsUnique();
            entity.Property(x => x.PageKey).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Module).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Route).HasMaxLength(240).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<RolePagePermission>(entity =>
        {
            entity.ToTable("role_page_permissions");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.RoleId, x.PageDefinitionId }).IsUnique();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.PageDefinition).WithMany().HasForeignKey(x => x.PageDefinitionId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void SeedFoundation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(FoundationSeedData.Roles);
        modelBuilder.Entity<PageDefinition>().HasData(FoundationSeedData.Pages);
    }
}
