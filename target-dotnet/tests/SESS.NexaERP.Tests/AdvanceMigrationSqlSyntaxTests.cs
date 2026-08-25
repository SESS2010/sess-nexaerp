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
        var migrations = db.Database.GetMigrations().ToArray();
        Assert.Equal(5, migrations.Length);
        var migration = migrations[^1];
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("business-up.sql", migrator.GenerateScript("0", migration));
        server.Execute("business-down.sql", migrator.GenerateScript(migration, "0"));
    }

    [Fact]
    public void MultiCompanyFoundationCatalogueAndRejectionContractsHold()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var db = new NexaErpDbContext(options);
        var migrator = db.GetService<IMigrator>();
        var migration = db.Database.GetMigrations().Single(x => x == "20260824150742_CalibrationPurchasePairItemTypeCorrections");
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("foundation-up.sql", migrator.GenerateScript("0", migration));
        server.Execute("foundation-catalogue-assertions.sql", FoundationCatalogueAssertions);

        server.AssertRejected("reject-duplicate-company.sql",
            """INSERT INTO advance.companies ("Id","Code","LegalName","EntityType","Status","IsActive","CreatedAt","CreatedBy","Version") VALUES(gen_random_uuid(),'SESS_PVT_LTD','Duplicate','PRIVATE_LIMITED','ACTIVE',true,clock_timestamp(),'test',0);""");
        server.AssertRejected("reject-invalid-principal.sql",
            """INSERT INTO advance.user_accounts ("Id","LoginId","DisplayName","PasswordHash","UserType","PrincipalType","RoleId","IsActive","CreatedAt","CreatedBy","Version") SELECT gen_random_uuid(),'invalid-principal','Invalid','x','Internal','EMPLOYEE',r."Id",true,clock_timestamp(),'test',0 FROM advance.roles r LIMIT 1;""");
        server.AssertRejected("reject-second-payroll.sql",
            """INSERT INTO advance.employee_company_assignments ("Id","CompanyId","EmployeeId","AssignmentType","EmployeeCode","EmploymentType","EffectiveFrom","Status","IsActive","CreatedAt","CreatedBy","Version") SELECT gen_random_uuid(),"CompanyId","EmployeeId",'PAYROLL',"EmployeeCode"||'-DUP',"EmploymentType","EffectiveFrom",'ACTIVE',true,clock_timestamp(),'test',0 FROM advance.employee_company_assignments LIMIT 1;""");
        server.AssertRejected("reject-second-primary-department.sql",
            """INSERT INTO advance.employee_department_assignments ("Id","CompanyId","EmployeeCompanyAssignmentId","DepartmentId","DesignationId","AssignmentType","EffectiveFrom","IsPrimary","Status","IsActive","CreatedAt","CreatedBy","Version") SELECT gen_random_uuid(),a."CompanyId",a."EmployeeCompanyAssignmentId",d."Id",a."DesignationId",'PRIMARY',a."EffectiveFrom",true,'ACTIVE',true,clock_timestamp(),'test',0 FROM advance.employee_department_assignments a CROSS JOIN LATERAL (SELECT "Id" FROM advance.departments WHERE "Id"<>a."DepartmentId" LIMIT 1) d WHERE a."IsPrimary" LIMIT 1;""");
        server.AssertRejected("reject-company-code-mismatch.sql",
            """UPDATE advance.organization_policies SET "CompanyId"='70000000-0000-0000-0000-000000000002' WHERE "Id"='50000000-0000-0000-0000-000000000001';""");
        server.AssertRejected("reject-invalid-gstin.sql",
            """INSERT INTO advance.company_gst_registrations ("Id","CompanyId","Gstin","RegisteredLegalName","StateCode","RegistrationType","EffectiveFrom","IsPrimary","IsActive","CreatedAt","CreatedBy","Version") VALUES(gen_random_uuid(),'70000000-0000-0000-0000-000000000001','BAD','Bad GST','33','PRIVATE_LIMITED',DATE '2026-08-24',false,true,clock_timestamp(),'test',0);""");
        server.AssertRejected("reject-invalid-audit-scope.sql",
            """INSERT INTO advance.audit_logs ("Id","CompanyId","Scope","Module","Action","EntityName","EntityId","UserLoginId","Result","CorrelationId","CreatedAt","CreatedBy","Version") VALUES(gen_random_uuid(),NULL,'COMPANY','Test','Test','Test','1','test','Success','test',clock_timestamp(),'test',0);""");
        server.Execute("item-rejection-uom-prerequisite.sql",
            """INSERT INTO advance.uoms ("Id","Code","Name","MeasurementDimension","QuantityPrecision","IsActive","CreatedAt","CreatedBy","Version") VALUES('73000000-0000-0000-0000-000000000001','TEST-NOS','Test Number','COUNT',0,true,clock_timestamp(),'test',0);""");
        server.AssertRejected("reject-invalid-item-type.sql",
            """INSERT INTO advance.items ("Id","ItemCode","IsItemCodeLocked","Name","DetailedDescription","MaterialType","ItemType","IsReturnable","Uom","BaseUomId","GstPercentage","QcRequired","SerialNumberTracking","BatchTracking","ShelfLifeTracking","MinimumStock","MaximumStock","ReorderLevel","Status","ApprovalStatus","IsActive","CreatedAt","CreatedBy","Version") SELECT gen_random_uuid(),'BAD-TYPE',false,'Bad','Bad','Legacy','INVALID',false,u."Code",u."Id",0,false,false,false,false,0,0,0,'Active','Approved',true,clock_timestamp(),'test',0 FROM advance.uoms u LIMIT 1;""");
        server.AssertRejected("reject-nonreturnable-tool.sql",
            """INSERT INTO advance.items ("Id","ItemCode","IsItemCodeLocked","Name","DetailedDescription","MaterialType","ItemType","IsReturnable","Uom","BaseUomId","GstPercentage","QcRequired","SerialNumberTracking","BatchTracking","ShelfLifeTracking","MinimumStock","MaximumStock","ReorderLevel","Status","ApprovalStatus","IsActive","CreatedAt","CreatedBy","Version") SELECT gen_random_uuid(),'BAD-TOOL',false,'Bad','Bad','Legacy','TOOL',false,u."Code",u."Id",0,false,false,false,false,0,0,0,'Active','Approved',true,clock_timestamp(),'test',0 FROM advance.uoms u LIMIT 1;""");
        server.AssertRejected("reject-returnable-nontool.sql",
            """INSERT INTO advance.items ("Id","ItemCode","IsItemCodeLocked","Name","DetailedDescription","MaterialType","ItemType","IsReturnable","Uom","BaseUomId","GstPercentage","QcRequired","SerialNumberTracking","BatchTracking","ShelfLifeTracking","MinimumStock","MaximumStock","ReorderLevel","Status","ApprovalStatus","IsActive","CreatedAt","CreatedBy","Version") SELECT gen_random_uuid(),'BAD-COMPONENT',false,'Bad','Bad','Legacy','COMPONENT',true,u."Code",u."Id",0,false,false,false,false,0,0,0,'Active','Approved',true,clock_timestamp(),'test',0 FROM advance.uoms u LIMIT 1;""");
        server.Execute("foundation-down.sql", migrator.GenerateScript(migration, "0"));
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
        var businessMigrations = business.Database.GetMigrations().ToArray();
        Assert.Equal(5, businessMigrations.Length);
        var businessMigration = businessMigrations[^3];
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
        ALTER DATABASE advance_parser SET advance.rev869b_lease_id = '11111111-1111-1111-1111-111111111111';
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

    private const string FoundationCatalogueAssertions = """
        DO $assert$
        DECLARE value_count integer;
        BEGIN
          SELECT count(*) INTO value_count FROM pg_catalog.pg_tables WHERE schemaname='advance';
          IF value_count<>93 THEN RAISE EXCEPTION 'expected 93 advance tables, found %',value_count; END IF;

          SELECT count(*) INTO value_count FROM advance.companies
           WHERE ("Code","LegalName","EntityType") IN
             (('SESS_PROPRIETORSHIP','Sri Easwari Scientific Solution','PROPRIETORSHIP'),
              ('SESS_PVT_LTD','Sri Easwari Scientific Solution Private Limited','PRIVATE_LIMITED'));
          IF value_count<>2 THEN RAISE EXCEPTION 'company seed mismatch'; END IF;

          SELECT count(*) INTO value_count FROM advance.company_gst_registrations
           WHERE ("Gstin","StateCode") IN (('33APRPA5532K1ZU','33'),('33ABACS5491H1ZA','33'));
          IF value_count<>2 THEN RAISE EXCEPTION 'GST seed mismatch'; END IF;

          SELECT count(*) INTO value_count FROM advance.departments WHERE "IsActive";
          IF value_count<>21 THEN RAISE EXCEPTION 'expected 21 active departments, found %',value_count; END IF;
          SELECT count(*) INTO value_count FROM advance.departments WHERE "IsActive" AND "ParentDepartmentId" IS NULL;
          IF value_count<>17 THEN RAISE EXCEPTION 'expected 17 top-level departments, found %',value_count; END IF;
          SELECT count(*) INTO value_count FROM advance.departments WHERE "Code"='CALIBRATION' AND "IsActive" AND "ParentDepartmentId" IS NULL;
          IF value_count<>1 THEN RAISE EXCEPTION 'Calibration department seed mismatch'; END IF;

          SELECT count(*) INTO value_count FROM advance.employee_company_assignments WHERE "AssignmentType"='PAYROLL' AND "IsActive";
          IF value_count<>39 THEN RAISE EXCEPTION 'expected 39 PAYROLL assignments, found %',value_count; END IF;
          SELECT count(*) INTO value_count FROM advance.employee_company_assignments WHERE "AssignmentType"='WORK';
          IF value_count<>0 THEN RAISE EXCEPTION 'migration must not seed WORK assignments'; END IF;
          SELECT count(*) INTO value_count FROM advance.employee_department_assignments;
          IF value_count<>186 THEN RAISE EXCEPTION 'expected 186 department assignments, found %',value_count; END IF;
          SELECT count(*) INTO value_count FROM advance.employee_department_assignments WHERE "IsPrimary";
          IF value_count<>39 THEN RAISE EXCEPTION 'expected 39 primary department assignments, found %',value_count; END IF;
          SELECT count(*) INTO value_count FROM advance.employee_department_assignments WHERE NOT "IsPrimary";
          IF value_count<>147 THEN RAISE EXCEPTION 'expected 147 secondary department assignments, found %',value_count; END IF;

          SELECT count(*) INTO value_count
          FROM advance.employees e JOIN advance.departments d ON d."Id"=e."DepartmentId"
          JOIN advance.designations g ON g."Id"=e."DesignationId"
          WHERE e."EmployeeCode"='SESS-012' AND d."Code"='PURCHASE' AND g."Code"='PURCHASE_EXECUTIVE';
          IF value_count<>1 THEN RAISE EXCEPTION 'SESS-012 primary mapping mismatch'; END IF;
          SELECT count(*) INTO value_count
          FROM advance.employee_department_assignments a
          JOIN advance.employee_company_assignments c ON c."Id"=a."EmployeeCompanyAssignmentId"
          JOIN advance.departments d ON d."Id"=a."DepartmentId"
          WHERE c."EmployeeCode" IN ('SESS-012','SESS-014') AND NOT a."IsPrimary"
            AND ((c."EmployeeCode"='SESS-012' AND d."Code"='STORES') OR (c."EmployeeCode"='SESS-014' AND d."Code"='PURCHASE'));
          IF value_count<>2 THEN RAISE EXCEPTION 'Stores/Purchase secondary pair mismatch'; END IF;

          SELECT count(*) INTO value_count FROM information_schema.columns
          WHERE table_schema='advance' AND table_name='items' AND column_name='ItemType' AND is_nullable='NO' AND column_default IS NULL;
          IF value_count<>1 THEN RAISE EXCEPTION 'ItemType must be NOT NULL without a default'; END IF;
          SELECT count(*) INTO value_count FROM information_schema.columns
          WHERE table_schema='advance' AND table_name='items' AND column_name='IsReturnable' AND is_nullable='NO' AND column_default='false';
          IF value_count<>1 THEN RAISE EXCEPTION 'IsReturnable default contract mismatch'; END IF;
          SELECT count(*) INTO value_count FROM information_schema.columns
          WHERE table_schema='advance' AND table_name='items' AND column_name='MaterialType';
          IF value_count<>1 THEN RAISE EXCEPTION 'MaterialType was removed unexpectedly'; END IF;

          SELECT count(*) INTO value_count FROM advance.organization_policies
           WHERE "CompanyId"='70000000-0000-0000-0000-000000000001' AND "OrganizationId"='SESS_PVT_LTD';
          IF value_count<>2 THEN RAISE EXCEPTION 'organization policy mapping mismatch'; END IF;
          SELECT count(*) INTO value_count FROM advance.purchase_transaction_approval_policies
           WHERE "CompanyId"='70000000-0000-0000-0000-000000000001' AND "OrganizationId"='SESS_PVT_LTD';
          IF value_count<>3 THEN RAISE EXCEPTION 'approval policy mapping mismatch'; END IF;

          IF NOT EXISTS (
            SELECT 1 FROM pg_catalog.pg_constraint c
            JOIN pg_catalog.pg_class t ON t.oid=c.conrelid
            JOIN pg_catalog.pg_namespace n ON n.oid=t.relnamespace
            WHERE n.nspname='advance' AND t.relname='purchase_orders' AND c.contype='f'
              AND pg_get_constraintdef(c.oid) LIKE 'FOREIGN KEY ("CompanyId", "OrganizationId")%')
          THEN RAISE EXCEPTION 'purchase_orders composite company/code FK missing'; END IF;

          IF NOT EXISTS (
            SELECT 1 FROM pg_catalog.pg_constraint c
            JOIN pg_catalog.pg_class t ON t.oid=c.conrelid
            JOIN pg_catalog.pg_namespace n ON n.oid=t.relnamespace
            WHERE n.nspname='advance' AND t.relname='purchase_requisition_lines' AND c.contype='f'
              AND pg_get_constraintdef(c.oid) LIKE 'FOREIGN KEY ("AssetId")%')
          THEN RAISE EXCEPTION 'MachineReference AssetId FK missing'; END IF;

          IF EXISTS (
            SELECT 1 FROM pg_catalog.pg_attribute a
            JOIN pg_catalog.pg_class t ON t.oid=a.attrelid
            JOIN pg_catalog.pg_namespace n ON n.oid=t.relnamespace
            WHERE n.nspname='advance' AND a.attname='CompanyId' AND a.attnotnull
              AND NOT EXISTS (
                SELECT 1 FROM pg_catalog.pg_index i
                WHERE i.indrelid=t.oid AND (i.indkey::smallint[])[0]=a.attnum))
          THEN RAISE EXCEPTION 'required company scope lacks a CompanyId-leading index'; END IF;
        END $assert$;
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
        return candidates.FirstOrDefault(path => new[] { "initdb", "pg_ctl", "psql", "createdb" }
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
                server.Require(server.Run("createdb", "--host", "127.0.0.1", "--port",
                    server._port.ToString(System.Globalization.CultureInfo.InvariantCulture), "--username", "postgres",
                    "advance_parser"), "createdb");
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
            return Run("psql", "-X", "--quiet", "--set", "ON_ERROR_STOP=1", "--host", "127.0.0.1", "--port",
                _port.ToString(System.Globalization.CultureInfo.InvariantCulture), "--username", "postgres",
                "--dbname", "advance_parser", "--file", file);
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
