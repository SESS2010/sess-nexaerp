using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Audit;
using SESS.NexaERP.Domain.Employees;
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
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<Designation> Designations => Set<Designation>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeSkill> EmployeeSkills => Set<EmployeeSkill>();
    public DbSet<EmployeeRoleAssignment> EmployeeRoleAssignments => Set<EmployeeRoleAssignment>();
    public DbSet<ReportingRelationship> ReportingRelationships => Set<ReportingRelationship>();
    public DbSet<EmployeeStatusHistory> EmployeeStatusHistories => Set<EmployeeStatusHistory>();
    public DbSet<EmployeeApprovalHistory> EmployeeApprovalHistories => Set<EmployeeApprovalHistory>();
    public DbSet<EmployeeImportHistory> EmployeeImportHistories => Set<EmployeeImportHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("nexa");
        ConfigureIdentity(modelBuilder);
        ConfigureMasters(modelBuilder);
        ConfigureInventory(modelBuilder);
        ConfigureAudit(modelBuilder);
        ConfigureAuthorization(modelBuilder);
        ConfigureEmployees(modelBuilder);
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

    private static void ConfigureEmployees(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("departments");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.ToTable("skills");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<Designation>(entity =>
        {
            entity.ToTable("designations");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("employees");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.EmployeeCode).IsUnique();
            entity.Property(x => x.EmployeeCode).HasMaxLength(40).IsRequired();
            entity.Property(x => x.EmployeeName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.OriginalImportedName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.EmployeeType).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Grade).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(40).IsRequired();
            entity.Property(x => x.OfficialEmail).HasMaxLength(254);
            entity.Property(x => x.MobileNumber).HasMaxLength(40);
            entity.Property(x => x.ApprovalStatus).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Designation).WithMany().HasForeignKey(x => x.DesignationId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeSkill>(entity =>
        {
            entity.ToTable("employee_skills");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.EmployeeId, x.SkillId }).IsUnique();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Skill).WithMany().HasForeignKey(x => x.SkillId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeRoleAssignment>(entity =>
        {
            entity.ToTable("employee_role_assignments");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.EmployeeId, x.RoleId, x.EffectiveFrom }).IsUnique();
            entity.Property(x => x.ApprovalStatus).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Remarks).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReportingRelationship>(entity =>
        {
            entity.ToTable("reporting_relationships");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.EmployeeId, x.EffectiveFrom }).IsUnique();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ReportingManager).WithMany().HasForeignKey(x => x.ReportingManagerEmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.DepartmentHead).WithMany().HasForeignKey(x => x.DepartmentHeadEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeStatusHistory>(entity =>
        {
            entity.ToTable("employee_status_history");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.EmployeeId, x.CreatedAt });
            entity.Property(x => x.OldStatus).HasMaxLength(40).IsRequired();
            entity.Property(x => x.NewStatus).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmployeeApprovalHistory>(entity =>
        {
            entity.ToTable("employee_approval_history");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.EmployeeId, x.CreatedAt });
            entity.Property(x => x.Action).HasMaxLength(80).IsRequired();
            entity.Property(x => x.FromStatus).HasMaxLength(60).IsRequired();
            entity.Property(x => x.ToStatus).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Remarks).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmployeeImportHistory>(entity =>
        {
            entity.ToTable("employee_import_history");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ImportBatch, x.SourceEmployeeCode }).IsUnique();
            entity.Property(x => x.ImportBatch).HasMaxLength(120).IsRequired();
            entity.Property(x => x.SourceEmployeeCode).HasMaxLength(40).IsRequired();
            entity.Property(x => x.SourceEmployeeName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.NormalizedEmployeeName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.SourceJson).HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        });
    }
    private static void SeedFoundation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(FoundationSeedData.Roles);
        modelBuilder.Entity<PageDefinition>().HasData(FoundationSeedData.Pages);
        modelBuilder.Entity<Role>().HasData(Rev866SeedData.AdditionalEmployeeRoles);
        modelBuilder.Entity<RolePagePermission>().HasData(Rev866SeedData.RolePagePermissions);
        modelBuilder.Entity<Department>().HasData(Rev866SeedData.Departments);
        modelBuilder.Entity<Skill>().HasData(Rev866SeedData.Skills);
        modelBuilder.Entity<Designation>().HasData(Rev866SeedData.Designations);
        modelBuilder.Entity<Employee>().HasData(Rev866SeedData.Employees);
        modelBuilder.Entity<EmployeeSkill>().HasData(Rev866SeedData.EmployeeSkills);
        modelBuilder.Entity<EmployeeRoleAssignment>().HasData(Rev866SeedData.EmployeeRoleAssignments);
        modelBuilder.Entity<EmployeeImportHistory>().HasData(Rev866SeedData.EmployeeImportHistories);
    }
}
