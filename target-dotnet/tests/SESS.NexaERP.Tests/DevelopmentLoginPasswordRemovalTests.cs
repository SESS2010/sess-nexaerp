using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Persistence.Migrations;

namespace SESS.NexaERP.Tests;

public sealed class DevelopmentLoginPasswordRemovalTests
{
    [Fact]
    public void EfModelAndLiveSourceContainNoDevelopmentPasswordStore()
    {
        using var db = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect")
            .Options);

        Assert.Null(db.Model.FindEntityType("SESS.NexaERP.Domain.Identity.DevelopmentLoginPassword"));
        Assert.Null(db.Model.FindEntityType("advance.development_login_passwords"));

        var root = FindRoot();
        Assert.False(File.Exists(Path.Combine(root, "src", "SESS.NexaERP.Domain", "Identity", "DevelopmentLoginPassword.cs")));
        Assert.False(File.Exists(Path.Combine(root, "src", "SESS.NexaERP.Infrastructure", "Persistence", "NexaErpDbContext.DevelopmentLogin.cs")));
        Assert.False(File.Exists(Path.Combine(root, "src", "SESS.NexaERP.Api", "Security", "DevelopmentPasswordHasher.cs")));
        Assert.False(File.Exists(Path.Combine(root, "database", "postgresql", "trial-test-login-user.sql")));
    }

    [Fact]
    public void ReplacementMigrationDropsTheTableAndGuardsBothDirections()
    {
        var root = FindRoot();
        var source = File.ReadAllText(Path.Combine(root, "src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260831150840_RemoveDevelopmentLoginPasswords.cs"));
        Assert.Equal(2, Count(source, "PostgreSqlClusterGuard.Require(migrationBuilder);"));
        Assert.Contains("migrationBuilder.DropTable(", source);
        Assert.Contains("name: \"development_login_passwords\"", source);

        var migration = new RemoveDevelopmentLoginPasswords();
        foreach (var methodName in new[] { "Up", "Down" })
        {
            var method = migration.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!;
            var error = Assert.Throws<TargetInvocationException>(() => method.Invoke(migration, [new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite")]));
            Assert.IsType<NotSupportedException>(error.InnerException);
        }
    }

    [Fact]
    public void DebugTokenPathHasNoPasswordAndEmployeeProvisioningUiIsRemoved()
    {
        var root = FindRoot();
        var endpoint = Read(root, "src", "SESS.NexaERP.Api", "Endpoints", "DevelopmentAuthEndpoints.cs");
        var employeeEndpoint = Read(root, "src", "SESS.NexaERP.Api", "Endpoints", "EmployeeEndpoints.cs");
        var login = Read(root, "src", "SESS.NexaERP.Web", "src", "features", "auth", "LoginPage.tsx");
        var employeePage = Read(root, "src", "SESS.NexaERP.Web", "src", "features", "employees", "EmployeeDetailPage.tsx");

        Assert.StartsWith("#if DEBUG", endpoint);
        Assert.DoesNotContain("Password", endpoint, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("provision-dev-login", employeeEndpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("provisionDevLogin", employeePage, StringComparison.Ordinal);
        Assert.DoesNotContain("type=\"password\"", login, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password:", login, StringComparison.Ordinal);
    }

    private static int Count(string text, string value) =>
        (text.Length - text.Replace(value, string.Empty, StringComparison.Ordinal).Length) / value.Length;

    private static string Read(string root, params string[] parts) => File.ReadAllText(Path.Combine([root, .. parts]));

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException();
    }
}