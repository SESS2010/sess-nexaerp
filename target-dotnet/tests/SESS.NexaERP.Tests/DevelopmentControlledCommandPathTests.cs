using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
#if DEBUG
using SESS.NexaERP.Infrastructure.Persistence;
#endif

namespace SESS.NexaERP.Tests;

public sealed class DevelopmentControlledCommandPathTests
{
    private static readonly string Root = FindRoot();

    [Fact]
    public void Source_closes_the_path_at_compile_time_and_requires_real_narrow_principals()
    {
        var path = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "DevelopmentControlledCommandPath.cs");
        var guard = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "DatabaseRuntimePrincipalGuard.cs");
        var installer = Read("src", "SESS.NexaERP.Installer", "DevelopmentControlledCommandPrincipalCommand.cs");
        var program = Read("src", "SESS.NexaERP.Installer", "Program.cs");

        Assert.StartsWith("#if DEBUG", path.TrimStart());
        Assert.StartsWith("#if DEBUG", installer.TrimStart());
        Assert.Contains("DevelopmentControlledCommandPath.IsEnabled", guard, StringComparison.Ordinal);
        Assert.Contains("#if DEBUG", guard, StringComparison.Ordinal);
        Assert.Contains("nexa_rev869b_app_runtime", path, StringComparison.Ordinal);
        Assert.Contains("nexa_rev869b_command_audit", path, StringComparison.Ordinal);
        Assert.Contains("session_user,current_user", path, StringComparison.Ordinal);
        Assert.DoesNotContain("NpgsqlCommand(\"SET ROLE", path, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("NpgsqlCommand(\"SET ROLE", installer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("controlled-command-development-principals", program, StringComparison.Ordinal);
        Assert.Contains("ALTER ROLE nexa_rev869b_app_runtime PASSWORD", installer, StringComparison.Ordinal);
        Assert.Contains("ALTER ROLE nexa_rev869b_command_audit PASSWORD", installer, StringComparison.Ordinal);
    }

#if DEBUG
    [Fact]
    public void Explicit_opt_in_is_refused_outside_development()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [DevelopmentControlledCommandPath.EnabledSetting] = "true"
        }).Build();
        var environment = new StubHostEnvironment { EnvironmentName = Environments.Production };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            DevelopmentControlledCommandPath.IsEnabled(configuration, environment));

        Assert.Contains("only in the Development environment", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Execution_instance_is_nonempty_and_stable_for_the_process()
    {
        var first = DevelopmentControlledCommandPath.ExecutionInstanceId;
        var second = DevelopmentControlledCommandPath.ExecutionInstanceId;
        Assert.NotEqual(Guid.Empty, first);
        Assert.Equal(first, second);
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "SESS.NexaERP.Tests";
        public string ContentRootPath { get; set; } = Root;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
#endif

    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine([Root, .. parts]));

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }
}