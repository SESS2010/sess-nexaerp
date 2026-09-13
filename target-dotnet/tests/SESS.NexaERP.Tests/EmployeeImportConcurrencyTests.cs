using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Infrastructure.MasterData;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task EmployeeImportAdvancesVersionAndRejectsPreparedStaleUpdate()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("employee-import-schema.sql",model.GetService<IMigrator>()
            .GenerateScript("0",model.Database.GetMigrations().Last()));
        using var environment = new OrdinaryPrincipalEnvironment(server.ConnectionString,"employee-import-runtime-123456789");
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
        var runtime = new NpgsqlConnectionStringBuilder(server.ConnectionString) { Username = "nexa_erp_runtime" }.ConnectionString;
        var options = new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(runtime).Options;
        await using var initial = new NexaErpDbContext(options);
        var employee = await initial.Employees.AsNoTracking().Include(x => x.Department).Include(x => x.Designation)
            .SingleAsync(x => x.EmployeeCode == "SESS-12");
        var user = new IdentityWitnessUser(employee.Id);
        var firstAdapter = new EmployeeMasterDataAdapter(initial,user);
        var existing = (await firstAdapter.LoadExistingAsync(["SESS-12"],[],default)).ById[employee.Id];
        var values = new Dictionary<string,string?> {
            ["EmployeeCode"] = employee.EmployeeCode,
            ["EmployeeName"] = "FIRST IMPORT VERSION WITNESS",
            ["DepartmentCode"] = employee.Department!.Code,
            ["DesignationCode"] = employee.Designation!.Code,
            ["JoiningDate"] = (employee.DateOfJoining ?? new DateOnly(2024,7,5)).ToString("yyyy-MM-dd"),
            ["CompanyAssignments"] = "SESS_PVT_LTD",
            ["LoginEnabled"] = "FALSE"
        };
        var row = new MasterDataRawRow(2,values);
        Assert.Empty(firstAdapter.Validate(row,existing,await firstAdapter.LoadLookupContextAsync([row],default)));
        var first = await firstAdapter.UpdateAsync(existing,row,existing.Version,default);
        await using var staleDb = new NexaErpDbContext(options);
        var staleAdapter = new EmployeeMasterDataAdapter(staleDb,user);
        var stale = new MasterDataRawRow(2,new Dictionary<string,string?>(values) {
            ["EmployeeName"] = "STALE IMPORT MUST NOT OVERWRITE"
        });
        var failure = await Record.ExceptionAsync(() => staleAdapter.UpdateAsync(existing,stale,existing.Version,default));
        await using var afterDb = new NexaErpDbContext(options);
        var after = await afterDb.Employees.AsNoTracking().SingleAsync(x => x.Id == employee.Id);
        var directory = Path.Combine(FindRepositoryRoot(),"local-evidence","item25");
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory,"employee-import-version.json"),JsonSerializer.Serialize(new {
            BeforeVersion = existing.Version,First = first,StaleException = failure?.GetType().FullName,
            After = new { after.Version,after.EmployeeName,after.LoginEnabled },
            Note = "Real PostgreSQL with the restricted runtime adapter; simulates a row prepared before another import commits, not workbook HTTP upload."
        },new JsonSerializerOptions { WriteIndented = true }));
        Assert.Equal(existing.Version+1,first.Version);
        Assert.IsType<MasterDataConflictException>(failure);
        Assert.Equal(first.Version,after.Version);
        Assert.Equal(values["EmployeeName"],after.EmployeeName);
        Assert.Equal(employee.LoginEnabled,after.LoginEnabled);
    }
}
