#if DEBUG
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace SESS.NexaERP.Infrastructure.Persistence;

public static class DevelopmentControlledCommandPath
{
    public const string EnabledSetting = "NexaErp:EnableDevelopmentControlledCommands";
    public const string ExpectedDatabaseSetting = "NexaErp:ExpectedDatabase";
    private const string AuditConnectionVariable = "REV869B_COMMAND_AUDIT_CONNECTION";
    public static Guid ExecutionInstanceId { get; } = Guid.NewGuid();

    public static void Configure(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!IsEnabled(configuration, environment)) return;

        var runtime = RequireConnection(configuration.GetConnectionString("NexaErp"), "nexa_rev869b_app_runtime");
        var audit = RequireConnection(Environment.GetEnvironmentVariable(AuditConnectionVariable), "nexa_rev869b_command_audit");
        var expectedDatabase = configuration[ExpectedDatabaseSetting];
        if (string.IsNullOrWhiteSpace(expectedDatabase))
            throw new InvalidOperationException($"{ExpectedDatabaseSetting} is required for the development controlled-command path.");
        if (expectedDatabase is "postgres" or "template0" or "template1")
            throw new InvalidOperationException("The development controlled-command path refuses a PostgreSQL administrative database.");
        if (!string.Equals(runtime.Database, expectedDatabase, StringComparison.Ordinal) ||
            !string.Equals(audit.Database, expectedDatabase, StringComparison.Ordinal))
            throw new InvalidOperationException("Both controlled-command connections must target NexaErp:ExpectedDatabase exactly.");
        if (!SameEndpoint(runtime, audit))
            throw new InvalidOperationException("Runtime and command-audit connections must target the same loopback PostgreSQL endpoint.");

        RequireSha256("REV869B_SERVICE_INSTANCE_FINGERPRINT");
        RequireSha256("REV869B_OWNERSHIP_LEASE_FINGERPRINT");
        Environment.SetEnvironmentVariable("REV869B_EXECUTION_INSTANCE_ID", ExecutionInstanceId.ToString("D"));
    }

    public static bool IsEnabled(IConfiguration configuration, IHostEnvironment environment)
    {
        var raw = configuration[EnabledSetting];
        if (raw is null) return false;
        if (!bool.TryParse(raw, out var enabled))
            throw new InvalidOperationException($"{EnabledSetting} must be true or false.");
        if (enabled && !environment.IsDevelopment())
            throw new InvalidOperationException($"{EnabledSetting} can be enabled only in the Development environment.");
        return enabled;
    }

    public static async Task ValidateAuditPrincipalAsync(IConfiguration configuration, CancellationToken cancellationToken)
    {
        var runtime = RequireConnection(configuration.GetConnectionString("NexaErp"), "nexa_rev869b_app_runtime");
        var audit = RequireConnection(Environment.GetEnvironmentVariable(AuditConnectionVariable), "nexa_rev869b_command_audit");
        if (!SameEndpoint(runtime, audit) || !string.Equals(runtime.Database, audit.Database, StringComparison.Ordinal))
            throw new InvalidOperationException("Command-audit principal validation target differs from runtime.");
        audit.Pooling = false;
        await using var connection = new NpgsqlConnection(audit.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("""
            SELECT session_user,current_user,rolsuper,rolcreatedb,rolcreaterole,rolreplication,rolbypassrls,
                   session_user=pg_get_userbyid(d.datdba),
                   coalesce(session_user=pg_get_userbyid(n.nspowner),false)
            FROM pg_roles r
            JOIN pg_database d ON d.datname=current_database()
            LEFT JOIN pg_namespace n ON n.nspname='advance'
            WHERE r.rolname=session_user
            """, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken) ||
            !string.Equals(reader.GetString(0), "nexa_rev869b_command_audit", StringComparison.Ordinal) ||
            !string.Equals(reader.GetString(1), "nexa_rev869b_command_audit", StringComparison.Ordinal) ||
            Enumerable.Range(2, 7).Any(reader.GetBoolean))
            throw new InvalidOperationException("Development command-audit connection is not the real non-administrative nexa_rev869b_command_audit session principal.");
    }

    private static NpgsqlConnectionStringBuilder RequireConnection(string? raw, string expectedUser)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException($"A {expectedUser} connection string is required.");
        var builder = new NpgsqlConnectionStringBuilder(raw);
        if (!IsLiteralLoopback(builder.Host))
            throw new InvalidOperationException("Development controlled-command connections must use a literal loopback host.");
        if (!string.Equals(builder.Username, expectedUser, StringComparison.Ordinal))
            throw new InvalidOperationException($"The connection must log in directly as {expectedUser}; SET ROLE emulation is forbidden.");
        return builder;
    }

    private static bool IsLiteralLoopback(string? host) =>
        string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
        IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address);

    private static bool SameEndpoint(NpgsqlConnectionStringBuilder left, NpgsqlConnectionStringBuilder right) =>
        string.Equals(left.Host, right.Host, StringComparison.OrdinalIgnoreCase) && left.Port == right.Port;

    private static void RequireSha256(string variable)
    {
        var value = Environment.GetEnvironmentVariable(variable);
        if (value is null || value.Length != 64 || value.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidOperationException($"{variable} must retain an exact SHA-256 fingerprint.");
    }
}
#endif