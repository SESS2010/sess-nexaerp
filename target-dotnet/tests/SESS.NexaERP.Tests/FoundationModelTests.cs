using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class FoundationModelTests
{
    [Fact]
    public void Foundation_model_contains_required_master_tables()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=localhost;Database=test;Username=test")
            .Options;

        using var dbContext = new NexaErpDbContext(options);

        var tables = dbContext.Model.GetEntityTypes()
            .Select(entity => entity.GetTableName())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("roles", tables);
        Assert.Contains("user_accounts", tables);
        Assert.Contains("customers", tables);
        Assert.Contains("vendors", tables);
        Assert.Contains("items", tables);
        Assert.Contains("warehouses", tables);
        Assert.Contains("rack_bins", tables);
        Assert.Contains("stock_movements", tables);
        Assert.Contains("audit_logs", tables);
        Assert.Contains("page_definitions", tables);
        Assert.Contains("role_page_permissions", tables);
    }
}
