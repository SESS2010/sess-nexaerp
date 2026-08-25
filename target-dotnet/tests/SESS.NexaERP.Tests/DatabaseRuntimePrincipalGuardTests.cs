namespace SESS.NexaERP.Tests;

public sealed class DatabaseRuntimePrincipalGuardTests
{
    private static readonly string Root = FindRoot();

    [Fact]
    public void Startup_guard_is_strict_and_runs_before_the_request_pipeline()
    {
        var guard = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "DatabaseRuntimePrincipalGuard.cs");
        var program = Read("src", "SESS.NexaERP.Api", "Program.cs");
        var registration = Read("src", "SESS.NexaERP.Infrastructure", "DependencyInjection.cs");

        Assert.Contains("session_user", guard, StringComparison.Ordinal);
        Assert.Contains("current_user", guard, StringComparison.Ordinal);
        Assert.Contains("role.rolsuper", guard, StringComparison.Ordinal);
        Assert.Contains("role.rolcreatedb", guard, StringComparison.Ordinal);
        Assert.Contains("role.rolcreaterole", guard, StringComparison.Ordinal);
        Assert.Contains("role.rolreplication", guard, StringComparison.Ordinal);
        Assert.Contains("role.rolbypassrls", guard, StringComparison.Ordinal);
        Assert.Contains("database.datdba", guard, StringComparison.Ordinal);
        Assert.Contains("namespace.nspowner", guard, StringComparison.Ordinal);
        Assert.Contains("pg_catalog.pg_has_role", guard, StringComparison.Ordinal);
        Assert.Contains("nexa_erp_runtime", guard, StringComparison.Ordinal);
        Assert.Contains("AddScoped<DatabaseRuntimePrincipalGuard>()", registration, StringComparison.Ordinal);
        Assert.True(
            program.IndexOf("GetRequiredService<DatabaseRuntimePrincipalGuard>()", StringComparison.Ordinal) <
            program.IndexOf("app.UseMiddleware<ExceptionHandlingMiddleware>()", StringComparison.Ordinal));
    }

    [Fact]
    public void Development_superuser_exemption_is_compile_and_environment_closed()
    {
        var guard = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "DatabaseRuntimePrincipalGuard.cs");

        Assert.Contains("DatabaseSecurity:AllowDevelopmentSuperuser", guard, StringComparison.Ordinal);
        Assert.Contains("#if !DEBUG", guard, StringComparison.Ordinal);
        Assert.Contains("if (settingIsPresent)", guard, StringComparison.Ordinal);
        Assert.Contains("must not be present in a Release build", guard, StringComparison.Ordinal);
        Assert.Contains("allowDevelopmentSuperuser && !environment.IsDevelopment()", guard, StringComparison.Ordinal);
        Assert.Equal(2, Count(guard, "logger.LogCritical("));
        Assert.Contains("allowDevelopmentSuperuser && evidence.IsSuperuser", guard, StringComparison.Ordinal);
    }

    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine([Root, .. parts]));

    private static int Count(string value, string fragment) =>
        value.Split(fragment, StringSplitOptions.None).Length - 1;

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
