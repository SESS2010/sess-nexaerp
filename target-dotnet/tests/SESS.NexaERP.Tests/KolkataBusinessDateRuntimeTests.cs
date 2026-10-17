using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task KolkataBusinessDateMigrationRoundTripsInstalledFunctionContracts()
    {
        const string baseline = "20261011100000_FifoRecoverableTaxCostBasis";
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("kolkata-baseline.sql", model.GetService<IMigrator>().GenerateScript("0", baseline));
        await using var connection = new NpgsqlConnection(server.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT p.oid::regprocedure::text,pg_get_userbyid(p.proowner),p.prosecdef,
                   coalesce(array_to_string(p.proconfig,','),''),pg_get_functiondef(p.oid),
                   p.prosrc ~* '(current_date|AT TIME ZONE ''UTC'')'
            FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
            WHERE n.nspname='advance' AND p.prokind='f' ORDER BY 1
            """;
        var rows = new List<object>();
        var dateFunctions = 0;
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                if (reader.GetBoolean(5)) dateFunctions++;
                rows.Add(new { Signature = reader.GetString(0), Owner = reader.GetString(1), SecurityDefiner = reader.GetBoolean(2),
                    Configuration = reader.GetString(3), Definition = reader.GetString(4), DateCandidate = reader.GetBoolean(5) });
            }
        }
        server.Execute("kolkata-before.sql", """
            CREATE TABLE public.kolkata_function_baseline AS
              SELECT p.oid,pg_get_functiondef(p.oid) definition,p.proowner,p.prosecdef,
                p.proacl::text acl,p.proconfig::text configuration,p.prolang,p.provolatile,p.proparallel
              FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
              WHERE n.nspname='advance' AND p.prokind='f';
            """);
        const string head = "20261017100000_KolkataCurrentBusinessDates";
        var migrator = model.GetService<IMigrator>();
        server.Execute("kolkata-up.sql", migrator.GenerateScript(baseline, head));
        const string unchangedAuthority = """
            DO $assert$ BEGIN
              IF EXISTS(SELECT 1 FROM public.kolkata_function_baseline b JOIN pg_proc p ON p.oid=b.oid
                WHERE p.proowner IS DISTINCT FROM b.proowner OR p.prosecdef IS DISTINCT FROM b.prosecdef
                  OR p.proacl::text IS DISTINCT FROM b.acl OR p.proconfig::text IS DISTINCT FROM b.configuration
                  OR p.prolang IS DISTINCT FROM b.prolang OR p.provolatile IS DISTINCT FROM b.provolatile
                  OR p.proparallel IS DISTINCT FROM b.proparallel) THEN
                RAISE EXCEPTION 'Kolkata date repair changed function authority or execution settings';
              END IF;
            END $assert$;
            """;
        const string noSessionDate = """
            DO $assert$ BEGIN
              IF EXISTS(SELECT 1 FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
                WHERE n.nspname='advance' AND p.prokind='f' AND (p.prosrc ~* '\mcurrent_date\M' OR p.prosrc LIKE '%statement_timestamp()::date%')) THEN
                RAISE EXCEPTION 'Installed function still derives its business date from session timezone';
              END IF;
              IF EXISTS(SELECT 1 FROM public.kolkata_function_baseline b JOIN pg_proc p ON p.oid=b.oid
                WHERE b.definition !~* '\mcurrent_date\M' AND b.definition NOT LIKE '%statement_timestamp()::date%' AND pg_get_functiondef(p.oid)<>b.definition) THEN
                RAISE EXCEPTION 'Date migration changed an unrelated or historical conversion';
              END IF;
            END $assert$;
            """;
        server.Execute("kolkata-contracts.sql", unchangedAuthority + noSessionDate);
        server.Execute("kolkata-clock-contract.sql", """
            DO $assert$ DECLARE original_day date; BEGIN
              IF NOT EXISTS(SELECT 1 FROM pg_proc p WHERE p.oid='advance.current_business_date()'::regprocedure
                  AND NOT p.prosecdef AND p.provolatile='s'
                  AND p.prosrc NOT LIKE '%current_setting%'
                  AND NOT EXISTS(SELECT 1 FROM aclexplode(coalesce(p.proacl,acldefault('f',p.proowner))) a
                    WHERE a.grantee=0 AND a.privilege_type='EXECUTE')) THEN
                RAISE EXCEPTION 'Clock must be read-only, stable, have no caller date override and no public execute grant';
              END IF;
              PERFORM set_config('TimeZone','UTC',true);
              original_day:=advance.current_business_date();
              IF original_day<>(CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Kolkata')::date THEN
                RAISE EXCEPTION 'Database and Kolkata calendar disagree';
              END IF;
              PERFORM set_config('TimeZone','Pacific/Honolulu',true);
              PERFORM set_config('advance.business_date','1900-01-01',true);
              IF advance.current_business_date()<>original_day THEN
                RAISE EXCEPTION 'Caller timezone/date setting changed the business clock';
              END IF;
            END $assert$;
            """);
        server.Execute("kolkata-down.sql", migrator.GenerateScript(head, baseline));
        server.Execute("kolkata-restored.sql", unchangedAuthority + """
            DO $assert$ BEGIN
              IF EXISTS(SELECT 1 FROM public.kolkata_function_baseline b JOIN pg_proc p ON p.oid=b.oid
                WHERE pg_get_functiondef(p.oid)<>b.definition) THEN
                RAISE EXCEPTION 'Kolkata Down did not restore the installed bodies exactly';
              END IF;
            END $assert$;
            """);
        server.Execute("kolkata-up-again.sql", migrator.GenerateScript(baseline, head));
        server.Execute("kolkata-contracts-again.sql", unchangedAuthority + noSessionDate);
        // Baseline 151 is retained as-is: the forward migration must repair installed definitions, not historical files.
        Assert.NotEmpty(rows);
        Assert.True(dateFunctions > 0);
        var destination = Path.Combine(Path.GetTempPath(), "rc26-installed-date-inventory");
        Directory.CreateDirectory(destination);
        await File.WriteAllTextAsync(Path.Combine(destination, "baseline-151-functions.json"),
            System.Text.Json.JsonSerializer.Serialize(new { Baseline = baseline, FunctionCount = rows.Count, DateCandidates = dateFunctions, Functions = rows },
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    }
}