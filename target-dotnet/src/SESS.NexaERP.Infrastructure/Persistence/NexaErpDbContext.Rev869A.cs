using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Masters;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed partial class NexaErpDbContext
{
    public DbSet<EmployeeIdentityMapping> EmployeeIdentityMappings => Set<EmployeeIdentityMapping>();
    public DbSet<EmployeeOperationalScope> EmployeeOperationalScopes => Set<EmployeeOperationalScope>();
    public DbSet<UomConversion> UomConversions => Set<UomConversion>();
    public DbSet<TaxGstSetting> TaxGstSettings => Set<TaxGstSetting>();
    public DbSet<OrganizationPolicy> OrganizationPolicies => Set<OrganizationPolicy>();
    public DbSet<VendorQualification> VendorQualifications => Set<VendorQualification>();
    public DbSet<ControlledConfigurationHistory> ControlledConfigurationHistories => Set<ControlledConfigurationHistory>();
    public DbSet<WarehouseConditionLocation> WarehouseConditionLocations => Set<WarehouseConditionLocation>();
    public DbSet<QcInspectionPolicy> QcInspectionPolicies => Set<QcInspectionPolicy>();

    private static void ConfigureRev869A(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmployeeIdentityMapping>(entity =>
        {
            entity.ToTable("employee_identity_mappings", table =>
            {
                table.HasCheckConstraint("CK_employee_identity_mapping_type", "\"IdentityType\" IN ('HUMAN','SERVICE')");
                table.HasCheckConstraint("CK_employee_identity_mapping_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OrganizationId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Issuer).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Subject).HasMaxLength(500).IsRequired();
            entity.Property(x => x.IdentityType).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => new { x.OrganizationId, x.Issuer, x.Subject, x.IsActive }).IsUnique().HasFilter("\"IsActive\" = TRUE");
            entity.HasIndex(x => new { x.OrganizationId, x.EmployeeId, x.IdentityType, x.IsActive }).IsUnique().HasFilter("\"IsActive\" = TRUE AND \"IdentityType\" = 'HUMAN'");
            entity.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeOperationalScope>(entity =>
        {
            entity.ToTable("employee_operational_scopes", table => table.HasCheckConstraint("CK_employee_operational_scope_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\""));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OrganizationId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Remarks).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => new { x.OrganizationId, x.EmployeeId, x.DepartmentId, x.WarehouseId, x.RackBinId, x.EffectiveFrom }).IsUnique();
            entity.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RackBin).WithMany().HasForeignKey(x => x.RackBinId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Uom>(entity =>
        {
            entity.Property(x => x.MeasurementDimension).HasMaxLength(50).HasDefaultValue(string.Empty).IsRequired();
            entity.Property(x => x.QuantityPrecision).HasDefaultValue(6).IsRequired();
        });
        modelBuilder.Entity<SESS.NexaERP.Domain.Inventory.Item>()
            .HasOne(x => x.BaseUom).WithMany().HasForeignKey(x => x.BaseUomId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UomConversion>(entity =>
        {
            entity.ToTable("uom_conversions", table =>
            {
                table.HasCheckConstraint("CK_uom_conversion_factor", "\"ConversionFactor\" > 0");
                table.HasCheckConstraint("CK_uom_conversion_precision", "\"QuantityPrecision\" = 6");
                table.HasCheckConstraint("CK_uom_conversion_distinct", "\"FromUomId\" <> \"ToUomId\"");
                table.HasCheckConstraint("CK_uom_conversion_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OrganizationId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.MeasurementDimension).HasMaxLength(50).IsRequired();
            entity.Property(x => x.ConversionFactor).HasPrecision(24, 12);
            entity.Property(x => x.ApprovalStatus).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => new { x.OrganizationId, x.FromUomId, x.ToUomId, x.EffectiveFrom }).IsUnique();
            entity.HasOne(x => x.FromUom).WithMany().HasForeignKey(x => x.FromUomId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ToUom).WithMany().HasForeignKey(x => x.ToUomId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TaxGstSetting>(entity =>
        {
            entity.ToTable("tax_gst_settings", table =>
            {
                table.HasCheckConstraint("CK_tax_gst_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                table.HasCheckConstraint("CK_tax_gst_rates", "\"GstRate\" BETWEEN 0 AND 100 AND \"CgstRate\" BETWEEN 0 AND 100 AND \"SgstRate\" BETWEEN 0 AND 100 AND \"IgstRate\" BETWEEN 0 AND 100 AND \"CessRate\" BETWEEN 0 AND 100");
                table.HasCheckConstraint("CK_tax_gst_rounding", "\"RoundingScale\" BETWEEN 0 AND 6");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OrganizationId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.JurisdictionCode).HasMaxLength(30).IsRequired();
            entity.Property(x => x.HsnSacCode).HasMaxLength(30).IsRequired();
            entity.Property(x => x.SupplyType).HasMaxLength(30).IsRequired();
            entity.Property(x => x.VendorRegistrationType).HasMaxLength(30).IsRequired();
            entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            entity.Property(x => x.ApprovalStatus).HasMaxLength(50).IsRequired();
            entity.Property(x => x.GstRate).HasPrecision(9, 6);
            entity.Property(x => x.CgstRate).HasPrecision(9, 6);
            entity.Property(x => x.SgstRate).HasPrecision(9, 6);
            entity.Property(x => x.IgstRate).HasPrecision(9, 6);
            entity.Property(x => x.CessRate).HasPrecision(9, 6);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => new { x.OrganizationId, x.JurisdictionCode, x.HsnSacCode, x.SupplyType, x.VendorRegistrationType, x.EffectiveFrom }).IsUnique();
        });

        modelBuilder.Entity<OrganizationPolicy>(entity =>
        {
            entity.ToTable("organization_policies", table => table.HasCheckConstraint("CK_organization_policy_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\""));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OrganizationId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.PolicyCode).HasMaxLength(100).IsRequired();
            entity.Property(x => x.PolicyValue).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => new { x.OrganizationId, x.PolicyCode, x.EffectiveFrom }).IsUnique();
        });

        modelBuilder.Entity<Vendor>(entity =>
        {
            entity.Property(x => x.CommercialVerificationStatus).HasMaxLength(50).HasDefaultValue(MasterApprovalStatuses.Draft).IsRequired();
        });
        modelBuilder.Entity<VendorQualification>(entity =>
        {
            entity.ToTable("vendor_qualifications", table => table.HasCheckConstraint("CK_vendor_qualification_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\""));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OrganizationId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.QualificationCode).HasMaxLength(100).IsRequired();
            entity.Property(x => x.VerificationStatus).HasMaxLength(50).IsRequired();
            entity.Property(x => x.ApprovalStatus).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => new { x.OrganizationId, x.VendorId, x.ItemCategoryId, x.QualificationCode, x.EffectiveFrom }).IsUnique();
            entity.HasOne(x => x.Vendor).WithMany().HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ItemCategory).WithMany().HasForeignKey(x => x.ItemCategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WarehouseConditionLocation>(entity =>
        {
            entity.ToTable("warehouse_condition_locations", table => table.HasCheckConstraint("CK_warehouse_condition_code", "\"ConditionCode\" IN ('AVAILABLE','QC_HOLD','REJECTED','QUARANTINE','RETURN_TO_VENDOR','SCRAP')"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OrganizationId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ConditionCode).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => new { x.OrganizationId, x.WarehouseId, x.ConditionCode, x.IsActive }).IsUnique().HasFilter("\"IsActive\" = TRUE");
            entity.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RackBin).WithMany().HasForeignKey(x => x.RackBinId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<QcInspectionPolicy>(entity =>
        {
            entity.ToTable("qc_inspection_policies", table =>
            {
                table.HasCheckConstraint("CK_qc_policy_owner", "(\"ItemId\" IS NOT NULL) <> (\"ItemCategoryId\" IS NOT NULL)");
                table.HasCheckConstraint("CK_qc_policy_limits", "\"LowerLimit\" IS NULL OR \"UpperLimit\" IS NULL OR \"UpperLimit\" >= \"LowerLimit\"");
                table.HasCheckConstraint("CK_qc_policy_sample", "\"SampleSize\" > 0");
                table.HasCheckConstraint("CK_qc_policy_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OrganizationId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ParameterCode).HasMaxLength(100).IsRequired();
            entity.Property(x => x.InspectionMethod).HasMaxLength(200).IsRequired();
            entity.Property(x => x.LowerLimit).HasPrecision(24, 6);
            entity.Property(x => x.UpperLimit).HasPrecision(24, 6);
            entity.Property(x => x.ApprovalStatus).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => new { x.OrganizationId, x.ItemId, x.ItemCategoryId, x.ParameterCode, x.EffectiveFrom }).IsUnique();
            entity.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ItemCategory).WithMany().HasForeignKey(x => x.ItemCategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.MeasurementUom).WithMany().HasForeignKey(x => x.MeasurementUomId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ControlledConfigurationHistory>(entity =>
        {
            entity.ToTable("controlled_configuration_histories");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OrganizationId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(100).IsRequired();
            entity.Property(x => x.BeforeJson).HasColumnType("jsonb");
            entity.Property(x => x.AfterJson).HasColumnType("jsonb");
            entity.Property(x => x.ActorLoginId).HasMaxLength(256).IsRequired();
            entity.Property(x => x.ActorRoleCode).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Remarks).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => new { x.EntityType, x.EntityId, x.CreatedAt });
            entity.HasIndex(x => x.CorrelationId);
        });

        modelBuilder.Entity<Role>().HasData(Rev869ASeedData.Roles);
        modelBuilder.Entity<PageDefinition>().HasData(Rev869ASeedData.Pages);
        modelBuilder.Entity<RolePagePermission>().HasData(Rev869ASeedData.RolePagePermissions);
        modelBuilder.Entity<OrganizationPolicy>().HasData(Rev869ASeedData.OrganizationPolicies);
    }
}
