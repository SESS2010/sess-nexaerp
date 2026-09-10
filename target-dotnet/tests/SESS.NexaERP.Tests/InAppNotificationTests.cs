using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private const string InAppNotificationTarget = "20260910094618_InAppNotificationDelivery";

    [Fact]
    public void In_app_notification_acl_applies_reverts_and_reapplies_on_disposable_postgresql()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        var migrations = model.Database.GetMigrations().ToArray();
        var index = Array.IndexOf(migrations, InAppNotificationTarget);
        Assert.True(index > 0);
        var predecessor = migrations[index - 1];
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("notification-predecessor.sql", migrator.GenerateScript("0", predecessor));
        server.Execute("notification-up.sql", migrator.GenerateScript(predecessor, InAppNotificationTarget));
        server.Execute("notification-down.sql", migrator.GenerateScript(InAppNotificationTarget, predecessor));
        server.Execute("notification-reapply.sql", migrator.GenerateScript(predecessor, InAppNotificationTarget));
    }

    [Fact]
    public void Notification_contract_has_scoped_reads_frozen_timers_and_narrow_acl()
    {
        var root = FindRepositoryRoot();
        var migration = File.ReadAllText(Path.Combine(root, "src", "SESS.NexaERP.Infrastructure", "Persistence",
            "Migrations", "20260910094618_InAppNotificationDelivery.cs"));
        var service = File.ReadAllText(Path.Combine(root, "src", "SESS.NexaERP.Infrastructure", "Stores",
            "EfInAppNotificationService.cs"));
        var endpoints = File.ReadAllText(Path.Combine(root, "src", "SESS.NexaERP.Api", "Endpoints",
            "NotificationEndpoints.cs"));
        Assert.Equal(2, migration.Split("PostgreSqlClusterGuard.Require(migrationBuilder);", StringSplitOptions.None).Length - 1);
        Assert.Contains("GRANT EXECUTE ON FUNCTION advance.stores_p1_actor_has_role", migration);
        Assert.DoesNotContain("GRANT SELECT", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("UNUSED_MATERIAL_OVERDUE", service);
        Assert.Contains("QC_AGEING_OVERDUE", service);
        Assert.Contains("ReceivedAt.AddDays", service);
        Assert.Contains("ComponentFitmentReversals", service);
        Assert.Contains("RecipientEmployeeId == scope.EmployeeId", service);
        Assert.Contains("/unread-count", endpoints);
        Assert.Contains("/{recipientId:guid}/read", endpoints);
    }
}
