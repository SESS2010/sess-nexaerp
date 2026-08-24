using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.SecurityMigrations;

namespace SESS.NexaERP.Tests;

public sealed class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public void GeneratedBusinessBaselineScriptsAreAcceptedByDisposablePostgreSql()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var db = new NexaErpDbContext(options);
        var migrator = db.GetService<IMigrator>();
        var migration = Assert.Single(db.Database.GetMigrations());
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("business-up.sql", migrator.GenerateScript("0", migration));
        server.Execute("business-down.sql", migrator.GenerateScript(migration, "0"));
    }

    [Fact]
    public void GeneratedSecurityPackageScriptsAreAcceptedByDisposablePostgreSql()
    {
        var connection = "Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect";
        var businessOptions = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql(connection).Options;
        var securityOptions = new DbContextOptionsBuilder<Rev869BSecurityDbContext>()
            .UseNpgsql(
                connection,
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(Rev869BSecurityDbContext).Assembly.FullName);
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Rev869BSecurity", "advance");
                })
            .Options;
        using var business = new NexaErpDbContext(businessOptions);
        using var security = new Rev869BSecurityDbContext(securityOptions);
        var businessMigrator = business.GetService<IMigrator>();
        var securityMigrator = security.GetService<IMigrator>();
        var businessMigration = Assert.Single(business.Database.GetMigrations());
        var securityMigration = Assert.Single(security.Database.GetMigrations());
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("business-up.sql", businessMigrator.GenerateScript("0", businessMigration));
        server.Execute("external-role-prerequisites.sql", ExternalRolePrerequisites);
        server.Execute("security-up.sql", securityMigrator.GenerateScript("0", securityMigration));
        server.Execute("security-down.sql", securityMigrator.GenerateScript(securityMigration, "0"));
        server.Execute("business-down.sql", businessMigrator.GenerateScript(businessMigration, "0"));
    }

    [Theory]
    [InlineData("CREATE TABLE broken (\"Id integer);")]
    [InlineData("SELECT 'unterminated;")]
    [InlineData("SELECT (1;")]
    [InlineData("CREATE FUNCTION broken() RETURNS void LANGUAGE plpgsql AS $b$ BEGIN PERFORM (1; END $b$;")]
    [InlineData("CREATE FUNCTION broken() RETURNS void LANGUAGE plpgsql AS $b$ BEGIN RETURN;")]
    public void PostgreSqlRejectsEachMalformedRegression(string sql)
    {
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.AssertRejected("malformed-regression.sql", sql);
    }

    private const string ExternalRolePrerequisites = """
        CREATE EXTENSION pgcrypto;
        CREATE FUNCTION pg_catalog.digest(bytea,text) RETURNS bytea LANGUAGE sql IMMUTABLE STRICT PARALLEL SAFE
          AS 'SELECT public.digest($1,$2)';
        CREATE FUNCTION pg_catalog.digest(text,text) RETURNS bytea LANGUAGE sql IMMUTABLE STRICT PARALLEL SAFE
          AS 'SELECT public.digest($1,$2)';
        ALTER DATABASE postgres SET advance.rev869b_lease_id = '11111111-1111-1111-1111-111111111111';
        CREATE ROLE nexa_rev869b_security_owner NOLOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
        CREATE ROLE nexa_rev869b_lifecycle_administrator LOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
        CREATE ROLE nexa_rev869b_app_runtime LOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
        CREATE ROLE nexa_rev869b_command_audit LOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
        CREATE ROLE nexa_rev869b_management_writer LOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
        CREATE ROLE nexa_rev869b_purge_worker LOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
        CREATE ROLE nexa_rev869b_purge_audit LOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
        CREATE ROLE nexa_rev869b_export_service LOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
        CREATE ROLE nexa_rev869b_target_verifier LOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
        GRANT nexa_rev869b_security_owner TO nexa_rev869b_lifecycle_administrator;
        """;

    private static string FindPostgreSqlBin()
    {
        var configured = Environment.GetEnvironmentVariable("ADVANCE_POSTGRES_BIN");
        var root = OperatingSystem.IsWindows()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PostgreSQL")
            : "/usr/lib/postgresql";
        var candidates = string.IsNullOrWhiteSpace(configured) ? Array.Empty<string>() : new[] { configured };
        if (Directory.Exists(root)) candidates = candidates.Concat(Directory.GetDirectories(root)
            .OrderDescending().Select(path => Path.Combine(path, "bin"))).ToArray();
        var suffix = OperatingSystem.IsWindows() ? ".exe" : string.Empty;
        return candidates.FirstOrDefault(path => new[] { "initdb", "pg_ctl", "psql" }
                   .All(name => File.Exists(Path.Combine(path, name + suffix))))
               ?? throw new InvalidOperationException("Set ADVANCE_POSTGRES_BIN to a real PostgreSQL bin directory.");
    }

    private sealed class DisposablePostgreSql : IDisposable
    {
        private readonly string _bin;
        private readonly string _root;
        private readonly string _data;
        private readonly int _port;
        private bool _started;

        private DisposablePostgreSql(string bin)
        {
            _bin = bin;
            _root = Path.Combine(Path.GetTempPath(), $"advance-postgresql-parser-{Guid.NewGuid():N}");
            _data = Path.Combine(_root, "data");
            _port = ReservePort();
            Directory.CreateDirectory(_root);
        }

        public static DisposablePostgreSql Start(string bin)
        {
            var server = new DisposablePostgreSql(bin);
            try
            {
                server.Require(server.Run("initdb", "-D", server._data, "--username=postgres", "--auth=trust",
                    "--encoding=UTF8", "--no-locale"), "initdb");
                server.Require(server.Run("pg_ctl", "-D", server._data, "-l", Path.Combine(server._root, "postgres.log"),
                    "-o", $"-h 127.0.0.1 -p {server._port} -c fsync=off -c synchronous_commit=off",
                    "-w", "start"), "pg_ctl start");
                server._started = true;
                return server;
            }
            catch
            {
                server.Dispose();
                throw;
            }
        }

        public void AssertRejected(string name, string sql)
        {
            var result = Psql(name, sql);
            Assert.NotEqual(0, result.Code);
            Assert.Contains("ERROR:", result.Output, StringComparison.OrdinalIgnoreCase);
        }

        public void Execute(string name, string sql) => Require(Psql(name, sql), name);

        private Result Psql(string name, string sql)
        {
            var file = Path.Combine(_root, name);
            File.WriteAllText(file, sql);
            return Run("psql", "-X", "--set", "ON_ERROR_STOP=1", "--host", "127.0.0.1", "--port",
                _port.ToString(System.Globalization.CultureInfo.InvariantCulture), "--username", "postgres",
                "--dbname", "postgres", "--file", file);
        }

        public void Dispose()
        {
            if (_started) Run("pg_ctl", "-D", _data, "-m", "immediate", "-w", "stop");
            var root = Path.GetFullPath(_root);
            var temp = Path.GetFullPath(Path.GetTempPath());
            if (root.StartsWith(temp, StringComparison.OrdinalIgnoreCase) &&
                Path.GetFileName(root).StartsWith("advance-postgresql-parser-", StringComparison.Ordinal) &&
                Directory.Exists(root)) Directory.Delete(root, true);
        }

        private Result Run(string executable, params string[] arguments)
        {
            var suffix = OperatingSystem.IsWindows() ? ".exe" : string.Empty;
            var capture = executable != "pg_ctl" || !arguments.Contains("start", StringComparer.Ordinal);
            var info = new ProcessStartInfo(Path.Combine(_bin, executable + suffix))
            {
                RedirectStandardOutput = capture,
                RedirectStandardError = capture,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            foreach (var argument in arguments) info.ArgumentList.Add(argument);
            using var process = Process.Start(info) ?? throw new InvalidOperationException($"Cannot start {executable}.");
            var output = capture ? process.StandardOutput.ReadToEndAsync() : Task.FromResult(string.Empty);
            var error = capture ? process.StandardError.ReadToEndAsync() : Task.FromResult(string.Empty);
            if (!process.WaitForExit(TimeSpan.FromMinutes(3)))
            {
                process.Kill(true);
                throw new TimeoutException($"{executable} timed out.");
            }
            Task.WaitAll(output, error);
            return new Result(process.ExitCode, output.Result + error.Result);
        }

        private void Require(Result result, string operation) =>
            Assert.True(result.Code == 0,
                $"PostgreSQL rejected {operation}:{Environment.NewLine}{Tail(result.Output)}");

        private static string Tail(string value) => value.Length <= 12000 ? value : value[^12000..];

        private static int ReservePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private sealed record Result(int Code, string Output);
    }
}
