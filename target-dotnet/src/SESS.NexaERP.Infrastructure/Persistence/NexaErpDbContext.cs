using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Audit;
using SESS.NexaERP.Domain.Employees;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Domain.Purchase;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed partial class NexaErpDbContext(DbContextOptions<NexaErpDbContext> options) : DbContext(options)
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
    public DbSet<EmployeeDepartmentHistory> EmployeeDepartmentHistories => Set<EmployeeDepartmentHistory>();
    public DbSet<EmployeeApprovalHistory> EmployeeApprovalHistories => Set<EmployeeApprovalHistory>();
    public DbSet<EmployeeImportHistory> EmployeeImportHistories => Set<EmployeeImportHistory>();
    public DbSet<ItemCategory> ItemCategories => Set<ItemCategory>();
    public DbSet<ItemSubcategory> ItemSubcategories => Set<ItemSubcategory>();
    public DbSet<Uom> Uoms => Set<Uom>();
    public DbSet<Manufacturer> Manufacturers => Set<Manufacturer>();
    public DbSet<VendorCategory> VendorCategories => Set<VendorCategory>();
    public DbSet<VendorContact> VendorContacts => Set<VendorContact>();
    public DbSet<VendorAddress> VendorAddresses => Set<VendorAddress>();
    public DbSet<CustomerContact> CustomerContacts => Set<CustomerContact>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
    public DbSet<MasterStatusHistory> MasterStatusHistories => Set<MasterStatusHistory>();
    public DbSet<MasterApprovalHistory> MasterApprovalHistories => Set<MasterApprovalHistory>();
    public DbSet<MasterAttachmentMetadata> MasterAttachmentMetadata => Set<MasterAttachmentMetadata>();
    public DbSet<PurchaseRequisition> PurchaseRequisitions => Set<PurchaseRequisition>();
    public DbSet<PurchaseRequisitionLine> PurchaseRequisitionLines => Set<PurchaseRequisitionLine>();
    public DbSet<PurchaseRequisitionStatusHistory> PurchaseRequisitionStatusHistories => Set<PurchaseRequisitionStatusHistory>();
    public DbSet<PurchaseRequisitionApprovalHistory> PurchaseRequisitionApprovalHistories => Set<PurchaseRequisitionApprovalHistory>();
    public DbSet<PurchaseRequisitionAttachment> PurchaseRequisitionAttachments => Set<PurchaseRequisitionAttachment>();
    public DbSet<StockAvailabilityCheck> StockAvailabilityChecks => Set<StockAvailabilityCheck>();
    public DbSet<StockAvailabilityCheckLine> StockAvailabilityCheckLines => Set<StockAvailabilityCheckLine>();
    public DbSet<StockReservation> StockReservations => Set<StockReservation>();
    public DbSet<StockReservationHistory> StockReservationHistories => Set<StockReservationHistory>();
    public DbSet<PurchaseRequirementHandoff> PurchaseRequirementHandoffs => Set<PurchaseRequirementHandoff>();
    public DbSet<PurchaseApprovalRouteSetting> PurchaseApprovalRouteSettings => Set<PurchaseApprovalRouteSetting>();
    public DbSet<PurchaseApprovalWorkflowStep> PurchaseApprovalWorkflowSteps => Set<PurchaseApprovalWorkflowStep>();
    public DbSet<DepartmentApprovalMapping> DepartmentApprovalMappings => Set<DepartmentApprovalMapping>();
    public DbSet<PurchaseNumberSequence> PurchaseNumberSequences => Set<PurchaseNumberSequence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("nexa");
        ConfigureIdentity(modelBuilder);
        ConfigureMasters(modelBuilder);
        ConfigureInventory(modelBuilder);
        ConfigurePurchase(modelBuilder);
        ConfigureAudit(modelBuilder);
        ConfigureAuthorization(modelBuilder);
        ConfigureEmployees(modelBuilder);
        ConfigureRev869A(modelBuilder);
        ConfigureRev869B(modelBuilder);
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
            entity.HasIndex(x => x.PortalOrganizationId);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.IsActive);
            entity.HasIndex(x => x.GstNumber).IsUnique().HasFilter("\"GstNumber\" IS NOT NULL");
            entity.HasIndex(x => new { x.PanNumber, x.LegalCustomerName }).IsUnique().HasFilter("\"PanNumber\" IS NOT NULL");
            entity.Property(x => x.CustomerCode).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(240).IsRequired();
            entity.Property(x => x.LegalCustomerName).HasMaxLength(240).IsRequired();
            entity.Property(x => x.TradeName).HasMaxLength(240);
            entity.Property(x => x.CustomerType).HasMaxLength(80).IsRequired();
            entity.Property(x => x.GstNumber).HasMaxLength(32);
            entity.Property(x => x.PanNumber).HasMaxLength(16);
            entity.Property(x => x.BillingAddress).HasMaxLength(1000);
            entity.Property(x => x.ShippingAddress).HasMaxLength(1000);
            entity.Property(x => x.State).HasMaxLength(80);
            entity.Property(x => x.StateCode).HasMaxLength(8);
            entity.Property(x => x.Country).HasMaxLength(80).IsRequired();
            entity.Property(x => x.ContactPerson).HasMaxLength(160);
            entity.Property(x => x.Phone).HasMaxLength(40);
            entity.Property(x => x.Email).HasMaxLength(254);
            entity.Property(x => x.Industry).HasMaxLength(120);
            entity.Property(x => x.PaymentTerms).HasMaxLength(500);
            entity.Property(x => x.CreditLimit).HasPrecision(18, 2);
            entity.Property(x => x.PortalOrganizationId).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ApprovalStatus).HasMaxLength(60).IsRequired();
            entity.Property(x => x.ApprovedBy).HasMaxLength(160);
            entity.Property(x => x.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<Vendor>(entity =>
        {
            entity.ToTable("vendors");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.VendorCode).IsUnique();
            entity.HasIndex(x => x.VendorStatus);
            entity.HasIndex(x => x.PortalOrganizationId);
            entity.HasIndex(x => x.IsActive);
            entity.HasIndex(x => x.GstNumber).IsUnique().HasFilter("\"GstNumber\" IS NOT NULL");
            entity.HasIndex(x => new { x.PanNumber, x.LegalVendorName }).IsUnique().HasFilter("\"PanNumber\" IS NOT NULL");
            entity.Property(x => x.VendorCode).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(240).IsRequired();
            entity.Property(x => x.LegalVendorName).HasMaxLength(240).IsRequired();
            entity.Property(x => x.TradeName).HasMaxLength(240);
            entity.Property(x => x.VendorType).HasMaxLength(80).IsRequired();
            entity.Property(x => x.GstNumber).HasMaxLength(32);
            entity.Property(x => x.PanNumber).HasMaxLength(16);
            entity.Property(x => x.MsmeNumber).HasMaxLength(80);
            entity.Property(x => x.ContactPerson).HasMaxLength(160);
            entity.Property(x => x.Phone).HasMaxLength(40);
            entity.Property(x => x.Email).HasMaxLength(254);
            entity.Property(x => x.BillingAddress).HasMaxLength(1000);
            entity.Property(x => x.ShippingAddress).HasMaxLength(1000);
            entity.Property(x => x.State).HasMaxLength(80);
            entity.Property(x => x.StateCode).HasMaxLength(8);
            entity.Property(x => x.Country).HasMaxLength(80).IsRequired();
            entity.Property(x => x.MaterialServiceCategories).HasMaxLength(500);
            entity.Property(x => x.ApprovedMakes).HasMaxLength(500);
            entity.Property(x => x.PaymentTerms).HasMaxLength(500);
            entity.Property(x => x.DeliveryTerms).HasMaxLength(500);
            entity.Property(x => x.BankMetadataJson).HasColumnType("jsonb");
            entity.Property(x => x.AttachmentMetadataJson).HasColumnType("jsonb");
            entity.Property(x => x.ApprovalStatus).HasMaxLength(60).IsRequired();
            entity.Property(x => x.VendorStatus).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ApprovedBy).HasMaxLength(160);
            entity.Property(x => x.Version).IsConcurrencyToken();
        });

        ConfigureMasterSupport(modelBuilder);
    }

    private static void ConfigureMasterSupport(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ItemCategory>(entity =>
        {
            entity.ToTable("item_categories");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<ItemSubcategory>(entity =>
        {
            entity.ToTable("item_subcategories");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.CategoryId, x.Code }).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Uom>(entity =>
        {
            entity.ToTable("uoms");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<Manufacturer>(entity =>
        {
            entity.ToTable("manufacturers");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<VendorCategory>(entity =>
        {
            entity.ToTable("vendor_categories");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<VendorContact>(entity =>
        {
            entity.ToTable("vendor_contacts");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.VendorId, x.Email });
            entity.Property(x => x.ContactPerson).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(40);
            entity.Property(x => x.Email).HasMaxLength(254);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.Vendor).WithMany().HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VendorAddress>(entity =>
        {
            entity.ToTable("vendor_addresses");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.VendorId, x.AddressType });
            entity.Property(x => x.AddressType).HasMaxLength(40).IsRequired();
            entity.Property(x => x.AddressLine).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.State).HasMaxLength(80);
            entity.Property(x => x.StateCode).HasMaxLength(8);
            entity.Property(x => x.Country).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.Vendor).WithMany().HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CustomerContact>(entity =>
        {
            entity.ToTable("customer_contacts");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.CustomerId, x.Email });
            entity.Property(x => x.ContactPerson).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(40);
            entity.Property(x => x.Email).HasMaxLength(254);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CustomerAddress>(entity =>
        {
            entity.ToTable("customer_addresses");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.CustomerId, x.AddressType, x.SiteName });
            entity.Property(x => x.AddressType).HasMaxLength(40).IsRequired();
            entity.Property(x => x.AddressLine).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.SiteName).HasMaxLength(160);
            entity.Property(x => x.State).HasMaxLength(80);
            entity.Property(x => x.StateCode).HasMaxLength(8);
            entity.Property(x => x.Country).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MasterStatusHistory>(entity =>
        {
            entity.ToTable("master_status_history");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.MasterType, x.MasterId, x.CreatedAt });
            entity.Property(x => x.MasterType).HasMaxLength(80).IsRequired();
            entity.Property(x => x.MasterCode).HasMaxLength(120).IsRequired();
            entity.Property(x => x.PreviousStatus).HasMaxLength(60);
            entity.Property(x => x.NewStatus).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            entity.Property(x => x.SourceRevision).HasMaxLength(40).IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<MasterApprovalHistory>(entity =>
        {
            entity.ToTable("master_approval_history");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.MasterType, x.MasterId, x.CreatedAt });
            entity.Property(x => x.MasterType).HasMaxLength(80).IsRequired();
            entity.Property(x => x.MasterCode).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(80).IsRequired();
            entity.Property(x => x.FromStatus).HasMaxLength(60).IsRequired();
            entity.Property(x => x.ToStatus).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Remarks).HasMaxLength(500).IsRequired();
            entity.Property(x => x.ActorLoginId).HasMaxLength(160).IsRequired();
            entity.Property(x => x.ActorRoleCode).HasMaxLength(80).IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<MasterAttachmentMetadata>(entity =>
        {
            entity.ToTable("master_attachment_metadata");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.MasterType, x.MasterId });
            entity.Property(x => x.MasterType).HasMaxLength(80).IsRequired();
            entity.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            entity.Property(x => x.StorageKey).HasMaxLength(512).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(120);
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
            entity.HasIndex(x => new { x.Name, x.ManufacturerMake, x.Model, x.PartNumber }).IsUnique().HasFilter("\"PartNumber\" IS NOT NULL");
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.IsActive);
            entity.Property(x => x.ItemCode).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(240).IsRequired();
            entity.Property(x => x.DetailedDescription).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.MaterialType).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Uom).HasMaxLength(32).IsRequired();
            entity.Property(x => x.ManufacturerMake).HasMaxLength(160);
            entity.Property(x => x.Model).HasMaxLength(120);
            entity.Property(x => x.PartNumber).HasMaxLength(120);
            entity.Property(x => x.HsnSacCode).HasMaxLength(20);
            entity.Property(x => x.GstPercentage).HasPrecision(5, 2);
            entity.Property(x => x.TechnicalSpecification).HasMaxLength(2000);
            entity.Property(x => x.DrawingDocumentReference).HasMaxLength(512);
            entity.Property(x => x.Barcode).HasMaxLength(128);
            entity.Property(x => x.BarcodeSymbology).HasMaxLength(40);
            entity.Property(x => x.ImageStorageKey).HasMaxLength(512);
            entity.Property(x => x.ImageFileName).HasMaxLength(260);
            entity.Property(x => x.ImageContentType).HasMaxLength(120);
            entity.Property(x => x.MinimumStock).HasPrecision(18, 3);
            entity.Property(x => x.MaximumStock).HasPrecision(18, 3);
            entity.Property(x => x.ReorderLevel).HasPrecision(18, 3);
            entity.Property(x => x.StandardEstimatedPrice).HasPrecision(18, 2);
            entity.Property(x => x.Status).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ApprovalStatus).HasMaxLength(60).IsRequired();
            entity.Property(x => x.ApprovedBy).HasMaxLength(160);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Subcategory).WithMany().HasForeignKey(x => x.SubcategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.UomMaster).WithMany().HasForeignKey(x => x.UomId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Manufacturer).WithMany().HasForeignKey(x => x.ManufacturerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PreferredVendor).WithMany().HasForeignKey(x => x.PreferredVendorId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_items_minimum_stock_nonnegative", "\"MinimumStock\" >= 0");
                table.HasCheckConstraint("CK_items_maximum_stock_valid", "\"MaximumStock\" >= \"MinimumStock\"");
                table.HasCheckConstraint("CK_items_reorder_level_valid", "\"ReorderLevel\" >= 0 AND \"ReorderLevel\" <= \"MaximumStock\"");
                table.HasCheckConstraint("CK_items_gst_valid", "\"GstPercentage\" >= 0 AND \"GstPercentage\" <= 28");
            });
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.ToTable("warehouses");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.WarehouseCode).IsUnique();
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.IsActive);
            entity.Property(x => x.WarehouseCode).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.WarehouseType).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Location).HasMaxLength(1000);
            entity.Property(x => x.Status).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ApprovalStatus).HasMaxLength(60).IsRequired();
            entity.Property(x => x.ApprovedBy).HasMaxLength(160);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.ResponsibleEmployee).WithMany().HasForeignKey(x => x.ResponsibleEmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RackBin>(entity =>
        {
            entity.ToTable("rack_bins");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.WarehouseId, x.BinCode }).IsUnique();
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.IsActive);
            entity.Property(x => x.BinCode).HasMaxLength(80).IsRequired();
            entity.Property(x => x.RackName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.BinNameNumber).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Zone).HasMaxLength(120);
            entity.Property(x => x.LocationType).HasMaxLength(80).IsRequired();
            entity.Property(x => x.MaterialCondition).HasMaxLength(80).IsRequired();
            entity.Property(x => x.CapacityQuantity).HasPrecision(18, 3);
            entity.Property(x => x.CapacityUom).HasMaxLength(32);
            entity.Property(x => x.Barcode).HasMaxLength(128);
            entity.Property(x => x.Description).HasMaxLength(240);
            entity.Property(x => x.Status).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ApprovalStatus).HasMaxLength(60).IsRequired();
            entity.Property(x => x.ApprovedBy).HasMaxLength(160);
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


    private static void ConfigurePurchase(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PurchaseRequisition>(entity =>
        {
            entity.ToTable("purchase_requisitions");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.PrNumber).IsUnique();
            entity.HasIndex(x => new { x.OrganizationId, x.FinancialYear, x.PrSequence }).IsUnique();
            entity.HasIndex(x => new { x.OrganizationId, x.Status });
            entity.HasIndex(x => x.RequiredByDate);
            entity.Property(x => x.PrNumber).HasMaxLength(80).IsRequired();
            entity.Property(x => x.FinancialYear).HasMaxLength(12).IsRequired();
            entity.Property(x => x.OrganizationId).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Priority).HasMaxLength(40).IsRequired();
            entity.Property(x => x.PurposeJustification).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.CostCentre).HasMaxLength(120);
            entity.Property(x => x.ProjectReference).HasMaxLength(160);
            entity.Property(x => x.ServiceReference).HasMaxLength(160);
            entity.Property(x => x.WorkOrderReference).HasMaxLength(160);
            entity.Property(x => x.CustomerReference).HasMaxLength(160);
            entity.Property(x => x.Status).HasMaxLength(60).IsRequired();
            entity.Property(x => x.EstimatedTotal).HasPrecision(18, 2);
            entity.Property(x => x.ApprovalRoute).HasMaxLength(40).IsRequired();
            entity.Property(x => x.SubmittedBy).HasMaxLength(160);
            entity.Property(x => x.VerifiedBy).HasMaxLength(160);
            entity.Property(x => x.ApprovedBy).HasMaxLength(160);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.RequestingDepartment).WithMany().HasForeignKey(x => x.RequestingDepartmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RequesterEmployee).WithMany().HasForeignKey(x => x.RequesterEmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.DeliveryWarehouse).WithMany().HasForeignKey(x => x.DeliveryWarehouseId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint("CK_purchase_requisitions_estimated_total_nonnegative", "\"EstimatedTotal\" >= 0 AND \"PrSequence\" > 0"));
        });

        modelBuilder.Entity<PurchaseRequisitionLine>(entity =>
        {
            entity.ToTable("purchase_requisition_lines");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.PurchaseRequisitionId, x.LineNumber }).IsUnique();
            entity.Property(x => x.ItemCodeSnapshot).HasMaxLength(80).IsRequired();
            entity.Property(x => x.ItemNameSnapshot).HasMaxLength(240).IsRequired();
            entity.Property(x => x.UomSnapshot).HasMaxLength(32).IsRequired();
            entity.Property(x => x.SpecificationSnapshot).HasMaxLength(2000);
            entity.Property(x => x.RequestedQuantity).HasPrecision(18, 3);
            entity.Property(x => x.EstimatedUnitPriceSnapshot).HasPrecision(18, 2);
            entity.Property(x => x.EstimatedLineTotal).HasPrecision(18, 2);
            entity.Property(x => x.ProjectReference).HasMaxLength(160);
            entity.Property(x => x.MachineReference).HasMaxLength(160);
            entity.Property(x => x.ServiceReference).HasMaxLength(160);
            entity.Property(x => x.OnHandSnapshot).HasPrecision(18, 3);
            entity.Property(x => x.ActiveReservedSnapshot).HasPrecision(18, 3);
            entity.Property(x => x.AvailableSnapshot).HasPrecision(18, 3);
            entity.Property(x => x.InTransitSnapshot).HasPrecision(18, 3);
            entity.Property(x => x.ReservedQuantity).HasPrecision(18, 3);
            entity.Property(x => x.ShortageQuantity).HasPrecision(18, 3);
            entity.Property(x => x.ProcurementHandoffQuantity).HasPrecision(18, 3);
            entity.Property(x => x.LineStatus).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.PurchaseRequisition).WithMany(x => x.Lines).HasForeignKey(x => x.PurchaseRequisitionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PreferredWarehouse).WithMany().HasForeignKey(x => x.PreferredWarehouseId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_pr_lines_requested_qty_positive", "\"RequestedQuantity\" > 0");
                table.HasCheckConstraint("CK_pr_lines_amounts_nonnegative", "\"EstimatedUnitPriceSnapshot\" >= 0 AND \"EstimatedLineTotal\" >= 0 AND \"ReservedQuantity\" >= 0 AND \"ShortageQuantity\" >= 0 AND \"ProcurementHandoffQuantity\" >= 0");
                table.HasCheckConstraint("CK_pr_lines_reconcile_requested", "\"ReservedQuantity\" <= \"RequestedQuantity\" AND \"ShortageQuantity\" = GREATEST(\"RequestedQuantity\" - \"ReservedQuantity\", 0) AND \"ProcurementHandoffQuantity\" = \"ShortageQuantity\"");
            });
        });

        modelBuilder.Entity<PurchaseRequisitionStatusHistory>(entity =>
        {
            entity.ToTable("purchase_requisition_status_history");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.PurchaseRequisitionId, x.CreatedAt });
            entity.Property(x => x.PrNumber).HasMaxLength(80).IsRequired();
            entity.Property(x => x.PreviousStatus).HasMaxLength(60);
            entity.Property(x => x.NewStatus).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.ActorLoginId).HasMaxLength(160).IsRequired();
            entity.Property(x => x.ActorRoleCode).HasMaxLength(80).IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.PurchaseRequisition).WithMany().HasForeignKey(x => x.PurchaseRequisitionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PurchaseRequisitionApprovalHistory>(entity =>
        {
            entity.ToTable("purchase_requisition_approval_history");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.PurchaseRequisitionId, x.CreatedAt });
            entity.Property(x => x.PrNumber).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(80).IsRequired();
            entity.Property(x => x.FromStatus).HasMaxLength(60).IsRequired();
            entity.Property(x => x.ToStatus).HasMaxLength(60).IsRequired();
            entity.Property(x => x.ApprovalRoute).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Remarks).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.ActorLoginId).HasMaxLength(160).IsRequired();
            entity.Property(x => x.ActorRoleCode).HasMaxLength(80).IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.PurchaseRequisition).WithMany().HasForeignKey(x => x.PurchaseRequisitionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PurchaseRequisitionAttachment>(entity =>
        {
            entity.ToTable("purchase_requisition_attachments");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.PurchaseRequisitionId, x.StorageKey }).IsUnique();
            entity.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            entity.Property(x => x.StorageKey).HasMaxLength(512).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(120);
            entity.Property(x => x.UploadedBy).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.PurchaseRequisition).WithMany().HasForeignKey(x => x.PurchaseRequisitionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StockAvailabilityCheck>(entity =>
        {
            entity.ToTable("stock_availability_checks");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.CheckNumber).IsUnique();
            entity.HasIndex(x => x.PurchaseRequisitionId);
            entity.Property(x => x.CheckNumber).HasMaxLength(80).IsRequired();
            entity.Property(x => x.CheckedBy).HasMaxLength(160).IsRequired();
            entity.Property(x => x.ResultStatus).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Remarks).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.PurchaseRequisition).WithMany().HasForeignKey(x => x.PurchaseRequisitionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StockAvailabilityCheckLine>(entity =>
        {
            entity.ToTable("stock_availability_check_lines");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.StockAvailabilityCheckId, x.PurchaseRequisitionLineId, x.LocationKey }).IsUnique();
            entity.HasIndex(x => new { x.PurchaseRequisitionLineId, x.WarehouseId, x.RackBinId });
            entity.Property(x => x.LocationKey).HasMaxLength(120).IsRequired();
            entity.Property(x => x.RequestedQuantity).HasPrecision(18, 3);
            entity.Property(x => x.OnHandQuantity).HasPrecision(18, 3);
            entity.Property(x => x.ActiveReservedQuantity).HasPrecision(18, 3);
            entity.Property(x => x.AvailableQuantity).HasPrecision(18, 3);
            entity.Property(x => x.InTransitQuantity).HasPrecision(18, 3);
            entity.Property(x => x.ReservedQuantity).HasPrecision(18, 3);
            entity.Property(x => x.ShortageQuantity).HasPrecision(18, 3);
            entity.Property(x => x.LineResultStatus).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.StockAvailabilityCheck).WithMany(x => x.Lines).HasForeignKey(x => x.StockAvailabilityCheckId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.PurchaseRequisitionLine).WithMany().HasForeignKey(x => x.PurchaseRequisitionLineId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RackBin).WithMany().HasForeignKey(x => x.RackBinId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint("CK_stock_check_lines_quantities_valid", "\"RequestedQuantity\" > 0 AND \"OnHandQuantity\" >= 0 AND \"ActiveReservedQuantity\" >= 0 AND \"AvailableQuantity\" >= 0 AND \"InTransitQuantity\" >= 0 AND \"ReservedQuantity\" >= 0 AND \"ShortageQuantity\" >= 0 AND \"ReservedQuantity\" <= \"RequestedQuantity\""));
        });

        modelBuilder.Entity<StockReservation>(entity =>
        {
            entity.ToTable("stock_reservations");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ReservationNumber).IsUnique();
            entity.HasIndex(x => new { x.PurchaseRequisitionLineId, x.LocationKey, x.Status }).IsUnique().HasFilter("\"Status\" = 'Active'");
            entity.HasIndex(x => new { x.ItemId, x.WarehouseId, x.RackBinId, x.Status });
            entity.Property(x => x.ReservationNumber).HasMaxLength(80).IsRequired();
            entity.Property(x => x.LocationKey).HasMaxLength(120).IsRequired();
            entity.Property(x => x.ReservedQuantity).HasPrecision(18, 3);
            entity.Property(x => x.Status).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ReservedBy).HasMaxLength(160).IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.PurchaseRequisition).WithMany().HasForeignKey(x => x.PurchaseRequisitionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.PurchaseRequisitionLine).WithMany().HasForeignKey(x => x.PurchaseRequisitionLineId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RackBin).WithMany().HasForeignKey(x => x.RackBinId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint("CK_stock_reservations_qty_positive", "\"ReservedQuantity\" > 0"));
        });

        modelBuilder.Entity<StockReservationHistory>(entity =>
        {
            entity.ToTable("stock_reservation_history");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.StockReservationId, x.CreatedAt });
            entity.Property(x => x.Action).HasMaxLength(80).IsRequired();
            entity.Property(x => x.PreviousStatus).HasMaxLength(40);
            entity.Property(x => x.NewStatus).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Remarks).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.ActorLoginId).HasMaxLength(160).IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.StockReservation).WithMany().HasForeignKey(x => x.StockReservationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PurchaseRequirementHandoff>(entity =>
        {
            entity.ToTable("purchase_requirement_handoffs");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.HandoffNumber).IsUnique();
            entity.HasIndex(x => new { x.PurchaseRequisitionLineId, x.Status }).IsUnique().HasFilter("\"Status\" = 'PendingRFQ'");
            entity.Property(x => x.HandoffNumber).HasMaxLength(80).IsRequired();
            entity.Property(x => x.LocationKey).HasMaxLength(120).IsRequired();
            entity.Property(x => x.HandoffQuantity).HasPrecision(18, 3);
            entity.Property(x => x.Status).HasMaxLength(40).IsRequired();
            entity.Property(x => x.HandoffBy).HasMaxLength(160).IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.PurchaseRequisition).WithMany().HasForeignKey(x => x.PurchaseRequisitionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.PurchaseRequisitionLine).WithMany().HasForeignKey(x => x.PurchaseRequisitionLineId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RackBin).WithMany().HasForeignKey(x => x.RackBinId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint("CK_purchase_handoffs_qty_positive", "\"HandoffQuantity\" > 0"));
        });

        modelBuilder.Entity<PurchaseApprovalRouteSetting>(entity =>
        {
            entity.ToTable("purchase_approval_route_settings");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.RouteCode).IsUnique();
            entity.Property(x => x.RouteCode).HasMaxLength(40).IsRequired();
            entity.Property(x => x.MinimumAmount).HasPrecision(18, 2);
            entity.Property(x => x.MaximumAmount).HasPrecision(18, 2);
            entity.Property(x => x.ApproverRoleCode).HasMaxLength(80);
            entity.Property(x => x.ApproverResolutionType).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.ToTable(table => table.HasCheckConstraint("CK_purchase_route_limits_valid", "\"MinimumAmount\" >= 0 AND (\"MaximumAmount\" IS NULL OR \"MaximumAmount\" >= \"MinimumAmount\")"));
        });


        modelBuilder.Entity<PurchaseApprovalWorkflowStep>(entity =>
        {
            entity.ToTable("purchase_approval_workflow_steps");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.RouteCode, x.StepNumber, x.EffectiveFrom }).IsUnique();
            entity.HasIndex(x => new { x.RouteCode, x.IsActive });
            entity.Property(x => x.RouteCode).HasMaxLength(40).IsRequired();
            entity.Property(x => x.MinimumAmount).HasPrecision(18, 2);
            entity.Property(x => x.MaximumAmount).HasPrecision(18, 2);
            entity.Property(x => x.ApproverResolutionType).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ApproverEmployeeCode).HasMaxLength(40);
            entity.Property(x => x.ApproverRoleCode).HasMaxLength(80);
            entity.Property(x => x.Remarks).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.ToTable(table => table.HasCheckConstraint("CK_purchase_workflow_amounts_valid", "\"MinimumAmount\" >= 0 AND (\"MaximumAmount\" IS NULL OR \"MaximumAmount\" >= \"MinimumAmount\") AND \"StepNumber\" > 0"));
        });

        modelBuilder.Entity<DepartmentApprovalMapping>(entity =>
        {
            entity.ToTable("department_approval_mappings");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.DepartmentId, x.ApprovalRouteCode, x.Scope, x.EffectiveFrom }).IsUnique();
            entity.HasIndex(x => new { x.DepartmentId, x.ApprovalRouteCode, x.Scope, x.IsActive });
            entity.Property(x => x.ApprovalRouteCode).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Scope).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Remarks).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PrimaryApproverEmployee).WithMany().HasForeignKey(x => x.PrimaryApproverEmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AlternateApproverEmployee).WithMany().HasForeignKey(x => x.AlternateApproverEmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint("CK_department_approval_mapping_effective_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\""));
            entity.ToTable(table => table.HasCheckConstraint("CK_department_approval_mapping_manager_route", "\"ApprovalRouteCode\" = 'MANAGER'"));
        });
        modelBuilder.Entity<PurchaseNumberSequence>(entity =>
        {
            entity.ToTable("purchase_number_sequences");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.OrganizationId, x.FinancialYear, x.Prefix }).IsUnique();
            entity.Property(x => x.OrganizationId).HasMaxLength(120).IsRequired();
            entity.Property(x => x.FinancialYear).HasMaxLength(12).IsRequired();
            entity.Property(x => x.Prefix).HasMaxLength(16).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.ToTable(table => table.HasCheckConstraint("CK_purchase_number_sequences_last_number_nonnegative", "\"LastNumber\" >= 0"));
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
            entity.Property(x => x.Result).HasMaxLength(40).IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(120).IsRequired();
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
            entity.HasIndex(x => x.PayrollEmployeeId).IsUnique().HasFilter("\"PayrollEmployeeId\" IS NOT NULL");
            entity.Property(x => x.EmployeeCode).HasMaxLength(40).IsRequired();
            entity.Property(x => x.PayrollEmployeeId).HasMaxLength(40);
            entity.Property(x => x.EmployeeName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.OriginalImportedName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Gender).HasMaxLength(40);
            entity.Property(x => x.Qualification).HasMaxLength(120);
            entity.Property(x => x.EmployeeType).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Grade).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(40).IsRequired();
            entity.Property(x => x.OfficialEmail).HasMaxLength(254);
            entity.Property(x => x.DateOfJoiningAccuracy).HasMaxLength(80);
            entity.Property(x => x.ApproximateDateNote).HasMaxLength(500);
            entity.Property(x => x.FunctionalResponsibility).HasMaxLength(500);
            entity.Property(x => x.WorkLocation).HasMaxLength(120);
            entity.Property(x => x.ManagerScope).HasMaxLength(80);
            entity.Property(x => x.LegacyDepartment).HasMaxLength(120);
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

        modelBuilder.Entity<EmployeeDepartmentHistory>(entity =>
        {
            entity.ToTable("employee_department_history");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.EmployeeId, x.CreatedAt });
            entity.HasIndex(x => x.CorrelationId);
            entity.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            entity.Property(x => x.SourceRevision).HasMaxLength(80).IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.PreviousDepartment).WithMany().HasForeignKey(x => x.PreviousDepartmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.NewDepartment).WithMany().HasForeignKey(x => x.NewDepartmentId).OnDelete(DeleteBehavior.Restrict);
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
        modelBuilder.Entity<EmployeeStatusHistory>().HasData(Rev866SeedData.EmployeeStatusHistories);
        modelBuilder.Entity<AuditLog>().HasData(Rev866SeedData.CorrectiveAuditLogs);
    }
}
