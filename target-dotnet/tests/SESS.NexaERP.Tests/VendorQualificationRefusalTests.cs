using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;
using Npgsql;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Rev869A;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task VendorQualificationExpiredResolvedAssignmentReturnsPermissionDeniedWithoutWrites()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("qualification-refusal-business.sql", model.GetService<IMigrator>()
            .GenerateScript("0", model.Database.GetMigrations().Last()));
        server.Execute("qualification-refusal-trial.sql", "\\set expected_database advance_parser\n" +
            File.ReadAllText(Path.Combine(FindRepositoryRoot(), "database", "postgresql", "trial-master-data-apply.sql")));
        server.Execute("qualification-refusal-support.sql", R1PurchaseFlowSupportFixtureSql);
        // A stale resolved principal is rejected by the database even if request-time
        // authorization previously accepted it. Dates are relative to the database clock.
        server.Execute("qualification-refusal-expired.sql", """
            DO $fixture$ DECLARE company uuid; authority uuid; director uuid; BEGIN
              SELECT "Id" INTO STRICT company FROM advance.companies WHERE "Code"='SESS_PVT_LTD';
              SELECT a."Id",a."EmployeeId" INTO STRICT authority,director FROM advance.employee_role_assignments a
                JOIN advance.roles r ON r."Id"=a."RoleId" JOIN advance.employees e ON e."Id"=a."EmployeeId"
                WHERE a."CompanyId"=company AND r."Code"='TECHNICAL_DIRECTOR'
                  AND e."EmployeeCode"='SESS-01' AND a."EffectiveTo" IS NULL;
              PERFORM set_config('sess.role_authority_assignment_id',authority::text,true);
              INSERT INTO advance.employee_role_assignments
                ("Id","CompanyId","EmployeeId","RoleId","EffectiveFrom","EffectiveTo","AssignmentType",
                 "ApprovalStatus","Remarks","CreatedAt","CreatedBy","Version")
              SELECT gen_random_uuid(),company,e."Id",r."Id",CURRENT_DATE-2,CURRENT_DATE-1,'SUPPORT',
                'SeedApproved','Disposable expired-role refusal fixture',now(),'QUALIFICATION-REFUSAL-FIXTURE',0
                FROM advance.employees e CROSS JOIN advance.roles r
                WHERE e."EmployeeCode"='TEST-R1-ACCOUNTS-SUPPORT' AND r."Code"='STORES_EXECUTIVE';
              INSERT INTO advance.employee_role_assignment_events
                ("Id","CompanyId","EmployeeId","ActorEmployeeId","AssignmentId","Operation","ToRoleCode","ToAssignmentType",
                 "NewEffectiveFrom","NewEffectiveTo","EffectiveOn","Reason","ActorLoginId","ActorRoleCode","CreatedAt","CreatedBy","Version")
              SELECT gen_random_uuid(),a."CompanyId",a."EmployeeId",director,a."Id",'QUALIFICATION_REFUSAL_FIXTURE',r."Code",a."AssignmentType",
                a."EffectiveFrom",a."EffectiveTo",a."EffectiveFrom",'Disposable expired-role refusal fixture',
                'QUALIFICATION-REFUSAL-FIXTURE','TECHNICAL_DIRECTOR',now(),'QUALIFICATION-REFUSAL-FIXTURE',0
                FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId"
                WHERE a."CreatedBy"='QUALIFICATION-REFUSAL-FIXTURE';
              PERFORM set_config('sess.role_authority_assignment_id','',true);
            END $fixture$;
            """);
        var options = new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(server.ConnectionString).Options;
        Guid actorId;
        string categoryCode;
        IReadOnlyDictionary<string, EffectiveRoleAssignment> assignments;
        await using (var db = new NexaErpDbContext(options))
        {
            var companyId = await db.Companies.Where(x => x.Code == "SESS_PVT_LTD").Select(x => x.Id).SingleAsync();
            actorId = await db.Employees.Where(x => x.EmployeeCode == R1AccountsSupportEmployeeCode).Select(x => x.Id).SingleAsync();
            var categoryId = await db.Items.Where(x => x.ItemCode == "TRIAL-ITEM-001").Select(x => x.CategoryId).SingleAsync();
            categoryCode = await db.ItemCategories.Where(x => x.Id == categoryId).Select(x => x.Code).SingleAsync();
            assignments = await db.EmployeeRoleAssignments.AsNoTracking().Include(x => x.Role)
                .Where(x => x.CompanyId == companyId && x.EmployeeId == actorId)
                .ToDictionaryAsync(x => TaxWorkflowUser.AssignmentKey(x.EmployeeId, x.Role!.Code),
                    x => new EffectiveRoleAssignment(x.Id, x.Role!.Code, x.AssignmentType));
            db.EmployeeIdentityMappings.Add(Mapping(companyId, actorId, R1AccountsSupportEmployeeCode));
            await db.SaveChangesAsync();
        }
        var password = Guid.NewGuid().ToString("N");
        using var environment = new OrdinaryPrincipalEnvironment(server.ConnectionString, password);
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        var runtime = new NpgsqlConnectionStringBuilder(server.ConnectionString)
        {
            Username = "nexa_erp_runtime", Password = password, Pooling = false
        }.ConnectionString;
        var user = new TaxWorkflowUser(actorId, R1AccountsSupportEmployeeCode, "STORES_EXECUTIVE", assignments);
        await using var host = await PurchaseFlowHost.StartAsync(runtime, user);
        var errors = new QualificationRefusalErrorCapture();
        host.AddErrorCapture(errors);
        await using var beforeDb = new NexaErpDbContext(options);
        var beforeQualifications = await beforeDb.VendorQualifications.CountAsync();
        var beforeHistory = await beforeDb.ControlledConfigurationHistories.CountAsync();
        var beforeCommands = await beforeDb.Database.SqlQuery<int>(
            $"SELECT count(*)::integer AS \"Value\" FROM advance.command_requests").SingleAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/rev869a/configuration/vendor-qualifications")
        {
            Content = JsonContent.Create(new CreateVendorQualificationRequest("SESS_PVT_LTD",
                "TRIAL-VEN-001", categoryCode, "EXPIRED-ROLE-REFUSAL",
                DateOnly.FromDateTime(DateTime.UtcNow), null, "Expired assignment must be refused"))
        };
        request.Headers.Add("Idempotency-Key", "qualification-expired-role-refusal");
        using var response = await host.Client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        var exception = errors.Error;
        Assert.True(response.StatusCode == HttpStatusCode.Forbidden,
            $"Expected 403, got {(int)response.StatusCode}. {body}. Captured error: {exception}");
        using var json = System.Text.Json.JsonDocument.Parse(body);
        Assert.Equal("PERMISSION_DENIED", json.RootElement.GetProperty("Code").GetString());
        await using var afterDb = new NexaErpDbContext(options);
        Assert.Equal(beforeQualifications, await afterDb.VendorQualifications.CountAsync());
        Assert.Equal(beforeHistory, await afterDb.ControlledConfigurationHistories.CountAsync());
        Assert.Equal(beforeCommands, await afterDb.Database.SqlQuery<int>(
            $"SELECT count(*)::integer AS \"Value\" FROM advance.command_requests").SingleAsync());
    }

    private sealed class QualificationRefusalErrorCapture : ILoggerProvider
    {
        public Exception? Error { get; private set; }
        public ILogger CreateLogger(string categoryName) => new CaptureLogger(this, categoryName);
        public void Dispose() { }
        private sealed class CaptureLogger(QualificationRefusalErrorCapture capture, string category) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel level) => level >= LogLevel.Error;
            public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (exception is not null && category.EndsWith("ExceptionHandlingMiddleware", StringComparison.Ordinal))
                    capture.Error = exception;
            }
        }
    }
}
