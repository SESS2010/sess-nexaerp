using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Common;
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
        Assert.Equal(184, MultiCompanyFoundationSeedData.EmployeeDepartmentAssignments.Length);
        Assert.Equal(39, MultiCompanyFoundationSeedData.EmployeeDepartmentAssignments.Count(x => x.IsPrimary));
        Assert.Equal(145, MultiCompanyFoundationSeedData.EmployeeDepartmentAssignments.Count(x => !x.IsPrimary));
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
}
