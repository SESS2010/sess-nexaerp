#if DEBUG
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.SecurityMigrations;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task DevelopmentControlledCommandPathUsesRealDisposablePostgreSqlPrincipalsAndReversesCleanly()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var model = new NexaErpDbContext(options);
        var businessMigrator = model.GetService<IMigrator>();
        var latest = model.Database.GetMigrations().Last();
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("development-controlled-business-up.sql", businessMigrator.GenerateScript("0", latest));
        server.Execute("development-controlled-role-prerequisites.sql", ExternalRolePrerequisites);

        var securityOptions = new DbContextOptionsBuilder<Rev869BSecurityDbContext>()
            .UseNpgsql(server.ConnectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(Rev869BSecurityDbContext).Assembly.FullName);
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Rev869BSecurity", "advance");
            }).Options;
        using var security = new Rev869BSecurityDbContext(securityOptions);
        var securityMigrator = security.GetService<IMigrator>();
        var securityMigration = Assert.Single(security.Database.GetMigrations());
        server.Execute("development-controlled-security-up.sql", securityMigrator.GenerateScript("0", securityMigration));

        const string runtimePassword = "runtime-development-test-123456789";
        const string auditPassword = "audit-development-test-12345678901";
        using var environment = new EnvironmentScope(
            ("DOTNET_ENVIRONMENT", "Development"),
            ("NexaErp__EnableDevelopmentControlledCommands", "true"),
            ("NexaErp__ExpectedDatabase", "advance_parser"),
            ("ConnectionStrings__NexaErpDevelopmentControlledCommandBootstrap", server.ConnectionString),
            ("NEXAERP_REV869B_APP_RUNTIME_PASSWORD", runtimePassword),
            ("NEXAERP_REV869B_COMMAND_AUDIT_PASSWORD", auditPassword),
            ("REV869B_SERVICE_INSTANCE_FINGERPRINT", new string('a', 64)),
            ("REV869B_OWNERSHIP_LEASE_FINGERPRINT", new string('b', 64)));
        Assert.Equal(0, await DevelopmentControlledCommandPrincipalCommand.RunAsync(["provision"]));

        var runtime = new NpgsqlConnectionStringBuilder(server.ConnectionString)
        { Username = "nexa_rev869b_app_runtime", Password = runtimePassword, Pooling = false }.ConnectionString;
        var audit = new NpgsqlConnectionStringBuilder(server.ConnectionString)
        { Username = "nexa_rev869b_command_audit", Password = auditPassword, Pooling = false }.ConnectionString;
        Environment.SetEnvironmentVariable("REV869B_COMMAND_AUDIT_CONNECTION", audit);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["NexaErp:EnableDevelopmentControlledCommands"] = "true",
            ["NexaErp:ExpectedDatabase"] = "advance_parser",
            ["ConnectionStrings:NexaErp"] = runtime
        }).Build();
        var host = new DevelopmentHostEnvironment();
        DevelopmentControlledCommandPath.Configure(configuration, host);
        Assert.True(Guid.TryParse(Environment.GetEnvironmentVariable("REV869B_EXECUTION_INSTANCE_ID"), out var execution));
        Assert.Equal(DevelopmentControlledCommandPath.ExecutionInstanceId, execution);

        await using (var db = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(runtime).Options))
        {
            var guard = new DatabaseRuntimePrincipalGuard(db, configuration, host, NullLogger<DatabaseRuntimePrincipalGuard>.Instance);
            await guard.ValidateAsync();
            await db.Database.OpenConnectionAsync();
            await using var identity = new NpgsqlCommand("SELECT session_user,current_user", (NpgsqlConnection)db.Database.GetDbConnection());
            await using var reader = await identity.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal("nexa_rev869b_app_runtime", reader.GetString(0));
            Assert.Equal("nexa_rev869b_app_runtime", reader.GetString(1));
        }

        Assert.Equal(0, await DevelopmentControlledCommandPrincipalCommand.RunAsync(["remove"]));
        server.Execute("development-controlled-security-down.sql", securityMigrator.GenerateScript(securityMigration, "0"));
        server.Execute("development-controlled-business-down.sql", businessMigrator.GenerateScript(latest, "0"));
    }

    private sealed class DevelopmentHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "SESS.NexaERP.Tests";
        public string ContentRootPath { get; set; } = FindRepositoryRoot();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class EnvironmentScope : IDisposable
    {
        private readonly Dictionary<string, string?> prior = new(StringComparer.Ordinal);
        public EnvironmentScope(params (string Name, string Value)[] values)
        {
            foreach (var value in values)
            {
                prior[value.Name] = Environment.GetEnvironmentVariable(value.Name);
                Environment.SetEnvironmentVariable(value.Name, value.Value);
            }
            prior["REV869B_COMMAND_AUDIT_CONNECTION"] = Environment.GetEnvironmentVariable("REV869B_COMMAND_AUDIT_CONNECTION");
            prior["REV869B_EXECUTION_INSTANCE_ID"] = Environment.GetEnvironmentVariable("REV869B_EXECUTION_INSTANCE_ID");
        }
        public void Dispose()
        {
            foreach (var value in prior) Environment.SetEnvironmentVariable(value.Key, value.Value);
        }
    }
}
#endif