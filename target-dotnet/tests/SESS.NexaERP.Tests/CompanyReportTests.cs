using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Reporting;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Reporting;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task CompanyStockReportsEnforceLiveCompanyPermissionsAndExportAuditing()
    {
        using var scriptDb = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        var migrator = scriptDb.GetService<IMigrator>();
        var migrations = scriptDb.Database.GetMigrations().ToArray();
        server.Execute("report-schema.sql",migrator.GenerateScript("0",migrations[^1]));
        var reportIndex = Array.IndexOf(migrations, "20260913010000_CompanyReportPermissions");
        // 20260920180000_ReportExportFollowsView changes the rows this migration owns; it is rolled back
        // first and re-applied last, as an operator would.
        var exportIndex = Array.IndexOf(migrations, "20260920180000_ReportExportFollowsView");
        server.Execute("report-export-down.sql",migrator.GenerateScript(migrations[exportIndex],migrations[exportIndex-1]));
        server.Execute("report-down.sql",migrator.GenerateScript(migrations[reportIndex],migrations[reportIndex-1]));
        server.Execute("report-reapply.sql",migrator.GenerateScript(migrations[reportIndex-1],migrations[reportIndex]));
        server.Execute("report-export-reapply.sql",migrator.GenerateScript(migrations[exportIndex-1],migrations[exportIndex]));
        server.Execute("report-login.sql","""
            UPDATE advance.employees SET "LoginEnabled"=true WHERE "EmployeeCode" IN ('SESS-01','SESS-14');
            """);
        await using var db = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql(server.ConnectionString).Options);
        var employee = await db.Employees.SingleAsync(row => row.EmployeeCode == "SESS-01");
        var company = await db.Companies.SingleAsync(row => row.Code == "SESS_PVT_LTD");
        var assignments = await db.EmployeeRoleAssignments.Include(row => row.Role)
            .Where(row => row.EmployeeId == employee.Id && row.CompanyId == company.Id && row.EffectiveTo == null)
            .ToListAsync();
        var user = new ReportWitnessUser(employee.Id, assignments.Select(row => new EffectiveRoleAssignment(row.Id,row.Role!.Code,row.AssignmentType)).ToArray());
        using var principalEnvironment = new OrdinaryPrincipalEnvironment(server.ConnectionString,"report-runtime-test-123456789");
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
        var runtimeConnection = new Npgsql.NpgsqlConnectionStringBuilder(server.ConnectionString) { Username = "nexa_erp_runtime" }.ConnectionString;
        await using var runtimeDb = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(runtimeConnection).Options);
        var service = new EfCompanyReportService(runtimeDb,user);
        var accounts = await db.Employees.SingleAsync(row => row.EmployeeCode == "SESS-14");
        var accountsAssignments = await db.EmployeeRoleAssignments.Include(row => row.Role)
            .Where(row => row.EmployeeId == accounts.Id && row.CompanyId == company.Id && row.EffectiveTo == null).ToListAsync();
        var accountsService = new EfCompanyReportService(runtimeDb,new ReportWitnessUser(accounts.Id,
            accountsAssignments.Select(row => new EffectiveRoleAssignment(row.Id,row.Role!.Code,row.AssignmentType)).ToArray()));
        var directRead = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => runtimeDb.Database.ExecuteSqlRawAsync("SELECT * FROM advance.vendor_bill_lines LIMIT 1"));
        Assert.Equal("42501",directRead.SqlState);
        var catalogue = await service.ListAsync(default);
        Assert.Contains(catalogue,row => row.Key == "stock-balance");
        foreach (var key in new[] { "stock-balance", "movement-roll-forward", "grni", "vendor-purchases", "engineer-custody", "purchase-register", "pending-approvals", "fifo-valuation", "billed-not-received" })
        {
            var selectedService = key is "grni" or "vendor-purchases" or "fifo-valuation" or "billed-not-received" ? accountsService : service;
            var page = await ObserveSingleReportCommand(() => selectedService.GetAsync(key,new(),default));
            Assert.Equal("SESS_PVT_LTD",page.CompanyCode);
            Assert.Equal(0,page.TotalRows);
            Assert.Empty(page.Rows);
            var file = await ObserveSingleReportCommand(() => selectedService.ExportAsync(key,new(),default));
            using var stream = new MemoryStream(file.Content);
            using var workbook = new XLWorkbook(stream);
            Assert.Equal(4,workbook.Worksheets.Count);
            Assert.False(string.IsNullOrEmpty(workbook.Worksheet("Details").Cell(1,1).GetString()));
        }
        Assert.Equal(9,await db.AuditLogs.CountAsync(row => row.Module == "Reports" && row.Action == "Export"));
        // A previously resolved role assignment is insufficient after its company activation is revoked.
        await db.CompanyRoleActivations.Where(row => row.CompanyId == company.Id)
            .ExecuteUpdateAsync(set => set.SetProperty(row => row.IsEnabled,false));
        Assert.Empty(await service.ListAsync(default));
        await Assert.ThrowsAsync<ReportAccessDeniedException>(() => service.GetAsync("stock-balance",new(),default));
        Assert.Equal(1,await db.AuditLogs.CountAsync(row => row.Module == "Reports" && row.Action == "Denied"));
        user.OrganizationId = "UNKNOWN_COMPANY";
        await Assert.ThrowsAsync<ReportAccessDeniedException>(() => service.GetAsync("stock-balance",new(),default));
        Assert.Equal(1,await db.AuditLogs.CountAsync(row => row.Module == "Reports" && row.Action == "Denied" && row.Scope == "GLOBAL"));
    }


    private static async Task AssertReportsSwitchBetweenAuthorizedCompanies(
        DbContextOptions<NexaErpDbContext> options, string runtimeConnection, Guid directorId, Guid accountsId)
    {
        await using var seed = new NexaErpDbContext(options);
        var primaryId = MultiCompanyFoundationSeedData.SessPvtLtdId;
        var otherId = MultiCompanyFoundationSeedData.SessProprietorshipId;
        foreach (var (employeeId,roleCode,keys) in new[]
        {
            (directorId,"TECHNICAL_DIRECTOR",new[] { "stock-balance","movement-roll-forward","engineer-custody","purchase-register" }),
            (accountsId,"ACCOUNTS_MANAGER",new[] { "vendor-purchases","grni" })
        })
        {
            var primary = await seed.EmployeeRoleAssignments.AsNoTracking().Include(row=>row.Role)
                .SingleAsync(row=>row.EmployeeId==employeeId && row.CompanyId==primaryId &&
                    row.Role!.Code==roleCode && row.EffectiveTo==null);
            // Both companies' memberships, scopes and role assignments already exist in
            // committed seed migrations. Only governed witness movements supply report quantities.
            var otherAssignment = await seed.EmployeeRoleAssignments.AsNoTracking()
                .SingleAsync(row=>row.EmployeeId==employeeId && row.CompanyId==otherId &&
                    row.RoleId==primary.RoleId && row.EffectiveTo==null);
            var originalAssignments = new[] { new EffectiveRoleAssignment(primary.Id,roleCode,primary.AssignmentType) };
            var user = new ReportWitnessUser(employeeId,originalAssignments);
            await using var runtime = new NexaErpDbContext(
                new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(runtimeConnection).Options);
            var service = new EfCompanyReportService(runtime,user);
            var request = new CompanyReportRequest(ToDate:DateOnly.FromDateTime(
                TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow,"Asia/Kolkata").DateTime));
            var original = new Dictionary<string,CompanyReportPage>();
            foreach (var key in keys)
            {
                original[key] = await ObserveSingleReportCommand(()=>service.GetAsync(key,request,default));
                if (key!="grni") Assert.True(original[key].TotalSourceRows>0);
            }

            user.OrganizationId="SESS_PROPRIETORSHIP";
            // A valid company membership does not make a first-company role assignment portable.
            await Assert.ThrowsAsync<ReportAccessDeniedException>(()=>service.GetAsync(keys[0],request,default));
            user.Assignments=[new(otherAssignment.Id,roleCode,"FULL")];
            foreach (var key in keys)
            {
                var empty = await ObserveSingleReportCommand(()=>service.GetAsync(key,request,default));
                Assert.Equal("SESS_PROPRIETORSHIP",empty.CompanyCode);
                Assert.Equal(0,empty.TotalSourceRows);
                Assert.Empty(empty.Rows);
                Assert.Empty(empty.Totals);
            }
            if (roleCode=="ACCOUNTS_MANAGER")
            {
                // The first company's unresolved return credits must not poison another company's FIFO report.
                var emptyFifo=await service.GetAsync("fifo-valuation",request,default);
                Assert.Empty(emptyFifo.Rows);
            }
            user.OrganizationId="SESS_PVT_LTD";
            user.Assignments=originalAssignments;
            foreach (var key in keys)
            {
                var restored=await service.GetAsync(key,request,default);
                Assert.Equal(original[key].TotalSourceRows,restored.TotalSourceRows);
                Assert.Equal(original[key].Totals.Select(row=>row.GetRawText()),restored.Totals.Select(row=>row.GetRawText()));
            }
        }
    }

    private static async Task<T> ObserveSingleReportCommand<T>(Func<Task<T>> action)
    {
        var count = 0;
        using var parent = new System.Diagnostics.Activity("report-command-witness").Start();
        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = source => source.Name == "Npgsql",
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) =>
                System.Diagnostics.ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                if (activity.TraceId == parent.TraceId && activity.GetTagItem("db.query.text") is string)
                    Interlocked.Increment(ref count);
            }
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);
        var result = await action();
        Assert.Equal(1,count);
        return result;
    }

    private sealed class ReportWitnessUser(Guid employee, IReadOnlyList<EffectiveRoleAssignment> assignments) : ICurrentUser
    {
        public string LoginId => "report-witness";
        public string RoleCode => "TECHNICAL_DIRECTOR";
        public string? OrganizationId { get; set; } = "SESS_PVT_LTD";
        public bool IsAuthenticated => true;
        public Guid? EmployeeId => employee;
        public IReadOnlyList<EffectiveRoleAssignment> Assignments { get; set; } = assignments;
        public IReadOnlyList<EffectiveRoleAssignment> EffectiveRoleAssignments => Assignments;
    }
}

public sealed class CompanyReportWorkbookTests
{
    [Theory]
    [InlineData("2026-09-12T18:29:59Z",12)]
    [InlineData("2026-09-12T18:30:00Z",13)]
    public void ReportCalendarUsesConfiguredCompanyDayAcrossUtcBoundary(string timestamp,int expectedDay)
    {
        var options=new ReportCalendarOptions();
        options.CompanyTimeZones.Add("SESS_PVT_LTD","Asia/Kolkata");
        Assert.True(options.IsValid());
        var instant=DateTimeOffset.Parse(timestamp,System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(new DateOnly(2026,9,expectedDay),
            ReportCalendarOptions.DateInZone(instant,options.Resolve("sess_pvt_ltd")));
        Assert.Equal(new DateOnly(2026,9,12),
            ReportCalendarOptions.DateInZone(instant,options.Resolve("OTHER_COMPANY")));
    }

    [Fact]
    public void InvalidReportCalendarConfigurationIsRejected()
    {
        Assert.False(new ReportCalendarOptions { DefaultTimeZone="Not/AZone" }.IsValid());
    }

    [Fact]
    public void CurrentQueueRejectsHistoricalDate()
    {
        Assert.Throws<ReportRequestException>(()=>EfCompanyReportService.Normalize(
            ReportDefinitions.Find("pending-approvals"),new(ToDate:DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1))));
    }

    [Fact]
    public void GenericDrillThroughCanonicalizesUuidDimensions()
    {
        var normalized=EfCompanyReportService.Normalize(ReportDefinitions.Find("purchase-register"),
            new(Group:"{\"itemId\":\"ABCDEF00-1234-1234-1234-123456789ABC\",\"uom\":\"EA\"}"));
        using var json=JsonDocument.Parse(normalized.Group!);
        Assert.Equal("abcdef00-1234-1234-1234-123456789abc",json.RootElement.GetProperty("itemId").GetString());
        Assert.Equal("EA",json.RootElement.GetProperty("uom").GetString());
    }

    [Fact]
    public void ExportRetainsNumericCellsAndLinksToUnderlyingRowsWithoutExecutingItemText()
    {
        using var header = JsonDocument.Parse("""
            {"generatedAt":"2026-09-12T15:30:00+05:30","totalSourceRows":2,
             "totals":[{"uom":"EA","quantity":12.5,"detailStart":1,"detailCount":2}]}
            """);
        using var summary = JsonDocument.Parse("""
            {"itemCode":"=HYPERLINK(unsafe)","quantity":12.5,"detailStart":1,"detailCount":2}
            """);
        using var detail = JsonDocument.Parse("""{"_stockGroupOrdinal":1,"_stockLabels":{"itemCode":"=HYPERLINK(unsafe)","itemName":"Pump","uom":"EA"},"movementId":"source-1","quantityIn":7.5,"quantityOut":0,"netQuantity":7.5}""");
        using var writer = new ReportWorkbook(ReportDefinitions.Find("stock-balance"),"SESS_PVT_LTD",
            new(ToDate:new DateOnly(2026,9,12)),header.RootElement);
        writer.Write(1,summary.RootElement);
        writer.Write(2,detail.RootElement);
        using var repeated = JsonDocument.Parse("""{"_stockGroupOrdinal":1,"_stockLabels":null,"movementId":"source-2","quantityIn":5,"quantityOut":0,"netQuantity":5}""");
        writer.Write(2,repeated.RootElement);
        using var missingLabels=JsonDocument.Parse("""{"_stockGroupOrdinal":2,"_stockLabels":null,"movementId":"source-3"}""");
        Assert.Throws<InvalidOperationException>(()=>writer.Write(2,missingLabels.RootElement));
        using var stream = new MemoryStream(writer.Complete());
        using var workbook = new XLWorkbook(stream);
        Assert.Equal("2026-09-12T10:00:00.0000000+00:00",workbook.Worksheet("About").Cell(4,2).GetString());
        var cell = workbook.Worksheet("Summary").Cell(2,1);
        Assert.Equal("=HYPERLINK(unsafe)",cell.GetString());
        Assert.False(cell.HasFormula);
        var balance = workbook.Worksheet("Summary").Cell(2,11);
        Assert.Equal(12.5m,balance.GetValue<decimal>());
        Assert.True(balance.HasHyperlink);
        Assert.Contains("Details",balance.GetHyperlink().InternalAddress);
        Assert.Equal("source-1",workbook.Worksheet("Details").Cell(2,1).GetString());
        Assert.Equal("source-2",workbook.Worksheet("Details").Cell(3,1).GetString());
        Assert.Equal(7.5m,workbook.Worksheet("Details").Cell(2,16).GetValue<decimal>());
        Assert.Equal(5m,workbook.Worksheet("Details").Cell(3,16).GetValue<decimal>());
        Assert.Equal("=HYPERLINK(unsafe)",workbook.Worksheet("Details").Cell(3,3).GetString());
        Assert.False(workbook.Worksheet("Details").Cell(3,3).HasFormula);
    }

    [Theory]
    [InlineData("{\"itemId\":\"not-a-uuid\"}")]
    [InlineData("{\"companyId\":\"70000000-0000-0000-0000-000000000002\"}")]
    [InlineData("{\"itemId\":null,\"itemId\":null}")]
    [InlineData("[]")]
    public void InvalidDrillSelectionIsRejectedBeforeDatabaseAccess(string selection)
    {
        Assert.Throws<ReportRequestException>(() => EfCompanyReportService.Normalize(
            ReportDefinitions.Find("stock-balance"),new(Group:selection)));
    }
}
