using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Audit;
using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Employees;
using SESS.NexaERP.Domain.Foundation;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Domain.Purchase;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed partial class NexaErpDbContext
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<UserIdentityMapping> UserIdentityMappings => Set<UserIdentityMapping>();
    public DbSet<EmployeeUserBinding> EmployeeUserBindings => Set<EmployeeUserBinding>();
    public DbSet<VendorUserBinding> VendorUserBindings => Set<VendorUserBinding>();
    public DbSet<CustomerUserBinding> CustomerUserBindings => Set<CustomerUserBinding>();
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();
    public DbSet<VendorCompanyRelationship> VendorCompanyRelationships => Set<VendorCompanyRelationship>();
    public DbSet<CustomerCompanyRelationship> CustomerCompanyRelationships => Set<CustomerCompanyRelationship>();
    public DbSet<CompanySite> CompanySites => Set<CompanySite>();
    public DbSet<CompanyGstRegistration> CompanyGstRegistrations => Set<CompanyGstRegistration>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<FinancialPeriod> FinancialPeriods => Set<FinancialPeriod>();
    public DbSet<CostCentre> CostCentres => Set<CostCentre>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<EmployeeCompanyAssignment> EmployeeCompanyAssignments => Set<EmployeeCompanyAssignment>();
    public DbSet<EmployeeDepartmentAssignment> EmployeeDepartmentAssignments => Set<EmployeeDepartmentAssignment>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentRevision> DocumentRevisions => Set<DocumentRevision>();
    public DbSet<DocumentNumberSequence> DocumentNumberSequences => Set<DocumentNumberSequence>();
    public DbSet<PaymentTerm> PaymentTerms => Set<PaymentTerm>();

    private static void ConfigureMultiCompanyFoundation(ModelBuilder modelBuilder)
    {
        ConfigureIdentityFoundation(modelBuilder);
        ConfigureCompanyMasters(modelBuilder);
        ConfigureOperationalFoundation(modelBuilder);

        modelBuilder.Entity<Company>().HasData(MultiCompanyFoundationSeedData.Companies);
        modelBuilder.Entity<CompanyGstRegistration>().HasData(MultiCompanyFoundationSeedData.CompanyGstRegistrations);
        modelBuilder.Entity<Currency>().HasData(MultiCompanyFoundationSeedData.Currencies);
        modelBuilder.Entity<EmployeeCompanyAssignment>().HasData(MultiCompanyFoundationSeedData.EmployeeCompanyAssignments);
        modelBuilder.Entity<EmployeeDepartmentAssignment>().HasData(MultiCompanyFoundationSeedData.EmployeeDepartmentAssignments);
    }

    private static void ConfigureIdentityFoundation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserIdentityMapping>(entity =>
        {
            entity.ToTable("user_identity_mappings", table =>
            {
                table.HasCheckConstraint("CK_user_identity_mapping_kind", "\"IdentityKind\" IN ('HUMAN','SERVICE')");
                table.HasCheckConstraint("CK_user_identity_mapping_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
            });
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.Issuer, x.Subject }).IsUnique();
            entity.HasIndex(x => new { x.UserAccountId, x.IsActive });
            entity.Property(x => x.Issuer).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Subject).HasMaxLength(500).IsRequired();
            entity.Property(x => x.IdentityKind).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.UserAccountId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeUserBinding>(entity =>
        {
            entity.ToTable("employee_user_bindings", table => table.HasCheckConstraint("CK_employee_user_binding_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\""));
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.UserAccountId).IsUnique();
            entity.HasIndex(x => x.EmployeeId).IsUnique();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.UserAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VendorUserBinding>(entity =>
        {
            entity.ToTable("vendor_user_bindings", table => table.HasCheckConstraint("CK_vendor_user_binding_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\""));
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.UserAccountId).IsUnique();
            entity.HasIndex(x => new { x.VendorId, x.IsActive });
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.UserAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Vendor>().WithMany().HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CustomerUserBinding>(entity =>
        {
            entity.ToTable("customer_user_bindings", table => table.HasCheckConstraint("CK_customer_user_binding_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\""));
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.UserAccountId).IsUnique();
            entity.HasIndex(x => new { x.CustomerId, x.IsActive });
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.UserAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserRoleAssignment>(entity =>
        {
            entity.ToTable("user_role_assignments", table =>
            {
                table.HasCheckConstraint("CK_user_role_assignment_audience", "\"Audience\" IN ('INTERNAL','VENDOR','CUSTOMER')");
                table.HasCheckConstraint("CK_user_role_assignment_scope", "(\"Scope\"='GLOBAL' AND \"CompanyId\" IS NULL) OR (\"Scope\"='COMPANY' AND \"CompanyId\" IS NOT NULL)");
                table.HasCheckConstraint("CK_user_role_assignment_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
            });
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UserAccountId, x.RoleId, x.Audience, x.CompanyId, x.EffectiveFrom }).IsUnique().AreNullsDistinct(false);
            entity.HasIndex(x => new { x.UserAccountId, x.Audience, x.IsActive });
            entity.HasIndex(x => new { x.CompanyId, x.UserAccountId, x.IsActive });
            entity.Property(x => x.Audience).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Scope).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.UserAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Role>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCompanyMasters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("companies", table =>
            {
                table.HasCheckConstraint("CK_companies_entity_type", @"""EntityType"" IN ('PROPRIETORSHIP','PRIVATE_LIMITED')");
                table.HasCheckConstraint("CK_companies_status", @"""Status"" IN ('ACTIVE','INACTIVE')");
            });
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.Id, x.Code });
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.LegalName).HasMaxLength(300).IsRequired();
            entity.Property(x => x.EntityType).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<VendorCompanyRelationship>(entity =>
        {
            entity.ToTable("vendor_company_relationships", table =>
                table.HasCheckConstraint("CK_vendor_company_relationship_dates", @"""EffectiveTo"" IS NULL OR ""EffectiveTo"" >= ""EffectiveFrom"""));
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.CompanyId, x.VendorId, x.EffectiveFrom }).IsUnique();
            entity.HasIndex(x => x.VendorAssignedCustomerCode).HasFilter(@"""VendorAssignedCustomerCode"" IS NOT NULL");
            entity.Property(x => x.VendorAssignedCustomerCode).HasMaxLength(80);
            entity.Property(x => x.RelationshipStatus).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne<Vendor>().WithMany().HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<PaymentTerm>().WithMany().HasForeignKey(x => x.PaymentTermId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Employee>().WithMany().HasForeignKey(x => x.ApprovedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CustomerCompanyRelationship>(entity =>
        {
            entity.ToTable("customer_company_relationships", table =>
                table.HasCheckConstraint("CK_customer_company_relationship_dates", @"""EffectiveTo"" IS NULL OR ""EffectiveTo"" >= ""EffectiveFrom"""));
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.CompanyId, x.CustomerId, x.EffectiveFrom }).IsUnique();
            entity.HasIndex(x => x.CustomerAssignedSupplierCode).HasFilter(@"""CustomerAssignedSupplierCode"" IS NOT NULL");
            entity.Property(x => x.CustomerAssignedSupplierCode).HasMaxLength(80);
            entity.Property(x => x.RelationshipStatus).HasMaxLength(30).IsRequired();
            entity.Property(x => x.CreditLimit).HasPrecision(18, 2);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<PaymentTerm>().WithMany().HasForeignKey(x => x.PaymentTermId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Employee>().WithMany().HasForeignKey(x => x.ApprovedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CompanySite>(entity =>
        {
            entity.ToTable("company_sites", table =>
            {
                table.HasCheckConstraint("CK_company_sites_country", @"char_length(""CountryCode"") = 2");
                table.HasCheckConstraint("CK_company_sites_state_code", @"char_length(""StateCode"") = 2");
            });
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.SiteType).HasMaxLength(30).IsRequired();
            entity.Property(x => x.AddressLine1).HasMaxLength(300).IsRequired();
            entity.Property(x => x.AddressLine2).HasMaxLength(300);
            entity.Property(x => x.City).HasMaxLength(100).IsRequired();
            entity.Property(x => x.District).HasMaxLength(100);
            entity.Property(x => x.State).HasMaxLength(100).IsRequired();
            entity.Property(x => x.StateCode).HasMaxLength(2).IsRequired();
            entity.Property(x => x.PostalCode).HasMaxLength(10).IsRequired();
            entity.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();
            entity.Property(x => x.TimeZoneId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<CompanyGstRegistration>(entity =>
        {
            entity.ToTable("company_gst_registrations", table =>
            {
                table.HasCheckConstraint("CK_company_gst_registrations_gstin", @"char_length(""Gstin"") = 15");
                table.HasCheckConstraint("CK_company_gst_registration_dates", @"""EffectiveTo"" IS NULL OR ""EffectiveTo"" >= ""EffectiveFrom""");
            });
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Gstin).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.StateCode, x.EffectiveFrom }).IsUnique();
            entity.Property(x => x.Gstin).HasMaxLength(15).IsRequired();
            entity.Property(x => x.RegisteredLegalName).HasMaxLength(300).IsRequired();
            entity.Property(x => x.StateCode).HasMaxLength(2).IsRequired();
            entity.Property(x => x.RegistrationType).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne<CompanySite>().WithMany().HasForeignKey(x => x.CompanySiteId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Currency>(entity =>
        {
            entity.ToTable("currencies", table =>
            {
                table.HasCheckConstraint("CK_currencies_code", @"char_length(""Code"") = 3");
                table.HasCheckConstraint("CK_currencies_minor_units", @"""MinorUnitDigits"" BETWEEN 0 AND 6");
            });
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.NumericCode).IsUnique().HasFilter(@"""NumericCode"" IS NOT NULL");
            entity.Property(x => x.Code).HasMaxLength(3).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.NumericCode).HasMaxLength(3);
            entity.Property(x => x.Symbol).HasMaxLength(10);
            entity.Property(x => x.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<PaymentTerm>(entity =>
        {
            entity.ToTable("payment_terms", table =>
            {
                table.HasCheckConstraint("CK_payment_terms_days", @"""DueDays"" >= 0 AND (""DiscountDays"" IS NULL OR ""DiscountDays"" >= 0)");
                table.HasCheckConstraint("CK_payment_terms_percentages", @"""AdvancePercentage"" BETWEEN 0 AND 100 AND (""DiscountPercentage"" IS NULL OR ""DiscountPercentage"" BETWEEN 0 AND 100)");
            });
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.AdvancePercentage).HasPrecision(5, 2);
            entity.Property(x => x.DiscountPercentage).HasPrecision(5, 2);
            entity.Property(x => x.Version).IsConcurrencyToken();
        });
    }

    private static void ConfigureOperationalFoundation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FinancialPeriod>(entity =>
        {
            entity.ToTable("financial_periods", table =>
            {
                table.HasCheckConstraint("CK_financial_period_dates", @"""EndDate"" >= ""StartDate""");
                table.HasCheckConstraint("CK_financial_period_status", @"""Status"" IN ('OPEN','CLOSED','LOCKED')");
            });
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.StartDate, x.EndDate }).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.PeriodType).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.ClosedByUserAccountId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CostCentre>(entity =>
        {
            entity.ToTable("cost_centres", table =>
                table.HasCheckConstraint("CK_cost_centre_dates", @"""EffectiveTo"" IS NULL OR ""EffectiveTo"" >= ""EffectiveFrom"""));
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne<CostCentre>().WithMany().HasForeignKey(x => x.ParentCostCentreId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("projects", table =>
                table.HasCheckConstraint("CK_project_dates", @"(""TargetEndDate"" IS NULL OR ""StartDate"" IS NULL OR ""TargetEndDate"" >= ""StartDate"") AND (""ActualEndDate"" IS NULL OR ""StartDate"" IS NULL OR ""ActualEndDate"" >= ""StartDate"")"));
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.CompanyId, x.ProjectCode }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.CustomerId, x.Status });
            entity.Property(x => x.ProjectCode).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(250).IsRequired();
            entity.Property(x => x.ProjectType).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CompanySite>().WithMany().HasForeignKey(x => x.CompanySiteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CostCentre>().WithMany().HasForeignKey(x => x.CostCentreId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Employee>().WithMany().HasForeignKey(x => x.ManagerEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeCompanyAssignment>(entity =>
        {
            entity.ToTable("employee_company_assignments", table =>
            {
                table.HasCheckConstraint("CK_employee_company_assignment_type", @"""AssignmentType"" IN ('PAYROLL','WORK')");
                table.HasCheckConstraint("CK_employee_company_assignment_dates", @"""EffectiveTo"" IS NULL OR ""EffectiveTo"" >= ""EffectiveFrom""");
            });
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.CompanyId, x.EmployeeCode }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.EmployeeId, x.AssignmentType, x.EffectiveFrom }).IsUnique();
            entity.HasIndex(x => x.EmployeeId).IsUnique().HasFilter(@"""AssignmentType"" = 'PAYROLL' AND ""IsActive""");
            entity.Property(x => x.AssignmentType).HasMaxLength(20).IsRequired();
            entity.Property(x => x.EmployeeCode).HasMaxLength(50).IsRequired();
            entity.Property(x => x.PayrollEmployeeId).HasMaxLength(50);
            entity.Property(x => x.EmploymentType).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CompanySite>().WithMany().HasForeignKey(x => x.CompanySiteId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeDepartmentAssignment>(entity =>
        {
            entity.ToTable("employee_department_assignments", table =>
            {
                table.HasCheckConstraint("CK_employee_department_assignment_type", @"""AssignmentType"" IN ('PRIMARY','SECONDARY')");
                table.HasCheckConstraint("CK_employee_department_assignment_dates", @"""EffectiveTo"" IS NULL OR ""EffectiveTo"" >= ""EffectiveFrom""");
                table.HasCheckConstraint("CK_employee_department_assignment_primary", @"(""AssignmentType"" = 'PRIMARY') = ""IsPrimary""");
            });
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.CompanyId, x.EmployeeCompanyAssignmentId, x.DepartmentId, x.EffectiveFrom }).IsUnique();
            entity.HasIndex(x => x.EmployeeCompanyAssignmentId).IsUnique().HasFilter(@"""IsPrimary"" AND ""IsActive""");
            entity.Property(x => x.AssignmentType).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne<EmployeeCompanyAssignment>().WithMany().HasForeignKey(x => x.EmployeeCompanyAssignmentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Designation>().WithMany().HasForeignKey(x => x.DesignationId).OnDelete(DeleteBehavior.Restrict);
        });

        ConfigureAsset(modelBuilder);
        ConfigureDocuments(modelBuilder);
        ConfigureExistingFoundationExtensions(modelBuilder);
    }

    private static void ConfigureAsset(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Asset>(entity =>
        {
            entity.ToTable("assets", table =>
            {
                table.HasCheckConstraint("CK_asset_warranty_dates", @"""WarrantyEndDate"" IS NULL OR ""WarrantyStartDate"" IS NULL OR ""WarrantyEndDate"" >= ""WarrantyStartDate""");
                table.HasCheckConstraint("CK_asset_installation_warranty", @"""WarrantyStartDate"" IS NULL OR ""InstallationDate"" IS NULL OR ""WarrantyStartDate"" >= ""InstallationDate""");
            });
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.CompanyId, x.AssetCode }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.SerialNumber }).IsUnique().HasFilter(@"""SerialNumber"" IS NOT NULL");
            entity.HasIndex(x => new { x.CompanyId, x.CustomerId, x.Status });
            entity.Property(x => x.AssetCode).HasMaxLength(80).IsRequired();
            entity.Property(x => x.AssetType).HasMaxLength(50).IsRequired();
            entity.Property(x => x.SerialNumber).HasMaxLength(160);
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne<Item>().WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CustomerAddress>().WithMany().HasForeignKey(x => x.CustomerAddressId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CompanySite>().WithMany().HasForeignKey(x => x.CompanySiteId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureDocuments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("documents");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.DocumentNumber).IsUnique();
            entity.HasIndex(x => new { x.DocumentType, x.Status });
            entity.Property(x => x.DocumentNumber).HasMaxLength(100).IsRequired();
            entity.Property(x => x.DocumentType).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne<Department>().WithMany().HasForeignKey(x => x.OwnerDepartmentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DocumentRevision>(entity =>
        {
            entity.ToTable("document_revisions", table =>
            {
                table.HasCheckConstraint("CK_document_revision_number", @"""RevisionNumber"" >= 0");
                table.HasCheckConstraint("CK_document_revision_size", @"""SizeBytes"" >= 0");
                table.HasCheckConstraint("CK_document_revision_sha256", @"octet_length(""Sha256"") = 32");
            });
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.DocumentId, x.RevisionNumber }).IsUnique();
            entity.HasIndex(x => new { x.DocumentId, x.RevisionCode }).IsUnique();
            entity.HasIndex(x => x.DocumentId).IsUnique().HasFilter(@"""IsCurrent""");
            entity.Property(x => x.RevisionCode).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(300).IsRequired();
            entity.Property(x => x.StorageKey).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.FileName).HasMaxLength(300).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Sha256).HasMaxLength(32).IsFixedLength().IsRequired();
            entity.Property(x => x.ChangeSummary).HasMaxLength(2000);
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne<Document>().WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.ReleasedByUserAccountId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Document>()
            .HasOne<DocumentRevision>()
            .WithMany()
            .HasForeignKey(x => x.CurrentRevisionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DocumentNumberSequence>(entity =>
        {
            entity.ToTable("document_number_sequences", table =>
            {
                table.HasCheckConstraint("CK_document_number_sequence_padding", @"""PaddingLength"" BETWEEN 1 AND 18");
                table.HasCheckConstraint("CK_document_number_sequence_last", @"""LastNumber"" >= 0");
            });
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.CompanyId, x.DocumentType, x.FinancialPeriodId }).IsUnique();
            entity.Property(x => x.DocumentType).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Prefix).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Suffix).HasMaxLength(50);
            entity.Property(x => x.FormatPattern).HasMaxLength(200);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne<FinancialPeriod>().WithMany().HasForeignKey(x => x.FinancialPeriodId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureExistingFoundationExtensions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasIndex(x => x.ParentDepartmentId);
            entity.HasOne<Department>().WithMany().HasForeignKey(x => x.ParentDepartmentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.Property(x => x.PrincipalType).HasMaxLength(20).HasDefaultValue("INTERNAL").IsRequired();
            entity.HasIndex(x => new { x.PrincipalType, x.IsActive });
            entity.ToTable("user_accounts", table =>
                table.HasCheckConstraint("CK_user_accounts_principal_type", @"""PrincipalType"" IN ('INTERNAL','VENDOR','CUSTOMER','SERVICE')"));
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(x => x.Scope).HasMaxLength(20).HasDefaultValue("GLOBAL").IsRequired();
            entity.HasIndex(x => new { x.CompanyId, x.CreatedAt });
            entity.ToTable("audit_logs", table =>
                table.HasCheckConstraint("CK_audit_logs_scope", @"(""Scope"" = 'GLOBAL' AND ""CompanyId"" IS NULL) OR (""Scope"" = 'COMPANY' AND ""CompanyId"" IS NOT NULL)"));
            entity.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseRequisitionLine>(entity =>
        {
            entity.HasIndex(x => x.AssetId);
            entity.HasOne<Asset>().WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCompanyScope(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(entityType => typeof(CompanyScopedAuditableEntity).IsAssignableFrom(entityType.ClrType)))
        {
            var entity = modelBuilder.Entity(entityType.ClrType);
            entity.HasIndex(nameof(CompanyScopedAuditableEntity.CompanyId));
            entity.HasAlternateKey(nameof(CompanyScopedAuditableEntity.CompanyId), nameof(AuditableEntity.Id));
            var company = entity.HasOne(typeof(Company)).WithMany();
            if (entityType.FindProperty("OrganizationId") is not null)
            {
                company
                    .HasForeignKey(nameof(CompanyScopedAuditableEntity.CompanyId), "OrganizationId")
                    .HasPrincipalKey(nameof(AuditableEntity.Id), nameof(Company.Code))
                    .OnDelete(DeleteBehavior.Restrict);
            }
            else
            {
                company
                    .HasForeignKey(nameof(CompanyScopedAuditableEntity.CompanyId))
                    .OnDelete(DeleteBehavior.Restrict);
            }
        }
    }
}
