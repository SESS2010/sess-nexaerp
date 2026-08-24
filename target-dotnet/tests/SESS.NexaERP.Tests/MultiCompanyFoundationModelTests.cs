using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class MultiCompanyFoundationModelTests
{
    [Fact]
    public void SettledExistingClassificationAndEmployeeAssignmentsAreExact()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var db = new NexaErpDbContext(options);
        var existingScoped = db.Model.GetEntityTypes()
            .Count(x => typeof(CompanyScopedAuditableEntity).IsAssignableFrom(x.ClrType) &&
                        x.ClrType.Namespace != "SESS.NexaERP.Domain.Foundation");

        Assert.Equal(43, existingScoped);
        Assert.Equal(29, 72 - existingScoped);
        Assert.Equal(39, MultiCompanyFoundationSeedData.EmployeeCompanyAssignments.Length);
        Assert.All(MultiCompanyFoundationSeedData.EmployeeCompanyAssignments,
            row => Assert.Equal("PAYROLL", row.AssignmentType));
        Assert.Equal(186, MultiCompanyFoundationSeedData.EmployeeDepartmentAssignments.Length);
        Assert.Equal(39, MultiCompanyFoundationSeedData.EmployeeDepartmentAssignments.Count(x => x.IsPrimary));
        Assert.Equal(147, MultiCompanyFoundationSeedData.EmployeeDepartmentAssignments.Count(x => !x.IsPrimary));

        var departments = Rev866SeedData.Departments;
        var active = departments.Where(x => x.IsActive).ToArray();
        Assert.Equal(21, active.Length);
        Assert.Equal(17, active.Count(x => x.ParentDepartmentId is null));
        Assert.Equal(4, active.Count(x => x.ParentDepartmentId is not null));
        Assert.Contains(active, x => x.Code == "CALIBRATION" && x.ParentDepartmentId is null);

        var employees = Rev866SeedData.Employees.ToDictionary(x => x.EmployeeCode);
        var departmentById = departments.ToDictionary(x => x.Id);
        var designationById = Rev866SeedData.Designations.ToDictionary(x => x.Id);
        Assert.Equal("PURCHASE", departmentById[employees["SESS-012"].DepartmentId].Code);
        Assert.Equal("PURCHASE_EXECUTIVE", designationById[employees["SESS-012"].DesignationId].Code);
        Assert.Equal("STORES", departmentById[employees["SESS-014"].DepartmentId].Code);

        var assignments = MultiCompanyFoundationSeedData.EmployeeDepartmentAssignments;
        Assert.Contains(assignments, x => x.EmployeeCompanyAssignmentId == MultiCompanyFoundationSeedData.EmployeeCompanyAssignments.Single(a => a.EmployeeCode == "SESS-012").Id && x.AssignmentType == "SECONDARY" && departmentById[x.DepartmentId].Code == "STORES");
        Assert.Contains(assignments, x => x.EmployeeCompanyAssignmentId == MultiCompanyFoundationSeedData.EmployeeCompanyAssignments.Single(a => a.EmployeeCode == "SESS-014").Id && x.AssignmentType == "SECONDARY" && departmentById[x.DepartmentId].Code == "PURCHASE");
    }

    [Fact]
    public void EveryRequiredCompanyScopeHasCompanyForeignKeyAndLeadingIndex()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var db = new NexaErpDbContext(options);

        foreach (var entity in db.Model.GetEntityTypes()
                     .Where(x => typeof(CompanyScopedAuditableEntity).IsAssignableFrom(x.ClrType)))
        {
            Assert.Contains(entity.GetForeignKeys(), fk =>
                fk.PrincipalEntityType.ClrType.Name == "Company" &&
                fk.Properties[0].Name == "CompanyId");
            Assert.Contains(entity.GetIndexes(), index => index.Properties[0].Name == "CompanyId");
            Assert.Contains(entity.GetKeys(), key =>
                !key.IsPrimaryKey() &&
                key.Properties.Select(x => x.Name).SequenceEqual(["CompanyId", "Id"]));
        }
    }

    [Fact]
    public void ItemTypeAndReturnabilityAreControlledWithoutRetiringMaterialType()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var db = new NexaErpDbContext(options);
        var item = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Item))!;

        Assert.Equal(8, ItemTypes.All.Count);
        Assert.False(item.FindProperty(nameof(Item.ItemType))!.IsNullable);
        Assert.Equal(false, item.FindProperty(nameof(Item.IsReturnable))!.GetDefaultValue());
        Assert.NotNull(item.FindProperty(nameof(Item.MaterialType)));
        Assert.Contains(item.GetCheckConstraints(), x => x.Name == "CK_items_item_type");
        Assert.Contains(item.GetCheckConstraints(), x => x.Name == "CK_items_returnable_tool");
    }
}
