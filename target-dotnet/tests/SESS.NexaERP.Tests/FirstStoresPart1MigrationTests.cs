using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class FirstStoresPart1MigrationTests
{
    private static NexaErpDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect")
            .Options;
        return new NexaErpDbContext(options);
    }

    [Fact]
    public void Part1ModelContainsExactlyTheNineApprovedStoresTables()
    {
        using var db = CreateContext();
        var actual = db.Model.GetEntityTypes()
            .Where(x => x.ClrType.Namespace == typeof(GateEntry).Namespace)
            .Select(x => x.GetTableName())
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        string[] expected =
        [
            "business_rule_configuration_versions",
            "gate_entries",
            "gate_entry_lines",
            "item_company_inventory_settings",
            "notification_delivery_attempts",
            "notification_events",
            "notification_recipients",
            "store_category_routes",
            "stores_document_status_history"
        ];

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Part1SeedsNineEffectiveDatedRulesForEachSettledCompany()
    {
        using var db = CreateContext();
        var entity = db.GetService<IDesignTimeModel>().Model
            .FindEntityType(typeof(BusinessRuleConfigurationVersion));
        Assert.NotNull(entity);
        var seed = entity.GetSeedData().ToArray();
        Assert.Equal(18, seed.Length);

        var expectedCompanies = new[]
        {
            Guid.Parse("70000000-0000-0000-0000-000000000001"),
            Guid.Parse("70000000-0000-0000-0000-000000000002")
        };
        var expectedRules = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [StoresConfigurationRuleKeys.SerialCaptureThreshold] = "5000",
            [StoresConfigurationRuleKeys.QcCompletionDays] = "2",
            [StoresConfigurationRuleKeys.EmergencyPurchaseCountPerMonth] = "10",
            [StoresConfigurationRuleKeys.EmergencyPurchaseValueLimit] = "5000",
            [StoresConfigurationRuleKeys.ExpenseFoodPerPersonPerDay] = "300",
            [StoresConfigurationRuleKeys.ExpenseLodgingSinglePerDay] = "800",
            [StoresConfigurationRuleKeys.ExpenseLodgingDoublePerDay] = "1200",
            [StoresConfigurationRuleKeys.ExpenseDailyApprovalCap] = "5000",
            [StoresConfigurationRuleKeys.ExpenseTravelDistanceThresholdKm] = "100"
        };

        foreach (var companyId in expectedCompanies)
        {
            var companySeed = seed.Where(x => Assert.IsType<Guid>(x["CompanyId"]) == companyId).ToArray();
            Assert.Equal(9, companySeed.Length);
            Assert.Equal(expectedRules.Keys.Order(), companySeed.Select(x => Assert.IsType<string>(x["RuleKey"])).Order());
            foreach (var row in companySeed)
            {
                var key = Assert.IsType<string>(row["RuleKey"]);
                Assert.Equal(expectedRules[key], Assert.IsType<string>(row["NewValueJson"]));
                Assert.Equal(1, Assert.IsType<int>(row["VersionNumber"]));
                Assert.Equal("TECHNICAL_DIRECTOR", Assert.IsType<string>(row["ChangedByRoleCode"]));
                Assert.Null(row["PreviousVersionId"]);
                Assert.Null(row["OldValueJson"]);
            }
        }
    }

    [Fact]
    public void Part1MigrationIsLastAndUsesNoPart2OrPart3Tables()
    {
        using var db = CreateContext();
        Assert.Equal(
            "20260827093952_FirstStoresPart1FoundationInboundNotifications",
            db.Database.GetMigrations().Last());

        var tables = db.Model.GetEntityTypes().Select(x => x.GetTableName()).ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain("goods_receipts", tables);
        Assert.DoesNotContain("qc_inspections", tables);
        Assert.DoesNotContain("delivery_challans", tables);
        Assert.DoesNotContain("stock_posting_batches", tables);
    }
}
