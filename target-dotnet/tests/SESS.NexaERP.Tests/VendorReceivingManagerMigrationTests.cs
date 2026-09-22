using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SESS.NexaERP.Infrastructure.Persistence;
namespace SESS.NexaERP.Tests;
public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task ReceivingManagerAssessmentMigrationRoundTripsWithOneGrantAndNoBusinessRows()
    {
        const string previous="20260919130000_VendorManualAssessments";
        const string current="20260919160000_VendorReceivingManagerAssessments";
        const string deploy="SET SESSION AUTHORIZATION nexa_erp_migration; SET ROLE nexa_erp_owner;\n";
        using var model=new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator=model.GetService<IMigrator>();
        using var server=DisposablePostgreSql.Start(FindPostgreSqlBin(),databaseName:"sess_nexa_erp");
        server.Execute("receiving-manager-previous.sql",migrator.GenerateScript("0",previous));
        using var environment=new OrdinaryPrincipalEnvironment(server.ConnectionString,"receiving-manager-disposable-only");
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
        await using var owner=new NpgsqlConnection(server.ConnectionString);await owner.OpenAsync();
        async Task<string> FunctionBody()
        {
            await using var command=new NpgsqlCommand("SELECT prosrc FROM pg_proc WHERE oid=to_regprocedure('advance.record_vendor_manual_assessment(uuid,uuid,uuid,bigint,uuid,numeric,numeric,numeric,text,uuid,text,uuid,text,text)')",owner);
            return (string)(await command.ExecuteScalarAsync())!;
        }
        var original=await FunctionBody();
        // Configuration drift must never be cloned into the new receiving-manager grant.
        const string drift = "UPDATE advance.role_page_permissions p SET \"CanApprove\"={0} FROM advance.roles r WHERE r.\"Id\"=p.\"RoleId\" AND r.\"Code\"='QC_MANAGER' AND p.\"PageDefinitionId\"=md5('quality.vendor-manual-assessments')::uuid;";
        server.Execute("receiving-manager-grant-drift.sql",string.Format(drift,"true"));
        server.AssertRejected("receiving-manager-refuse-broader-grant.sql",deploy+migrator.GenerateScript(previous,current),"established manual-assessment permission source");
        Assert.Equal(original,await FunctionBody());
        server.Execute("receiving-manager-restore-grant.sql",string.Format(drift,"false"));
        server.Execute("receiving-manager-up.sql",deploy+migrator.GenerateScript(previous,current));
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","status"]));
        var extended=await FunctionBody();Assert.NotEqual(original,extended);
        server.Execute("receiving-manager-row-effects.sql","""
            DO $assert$ BEGIN
             IF EXISTS(SELECT 1 FROM advance.vendor_manual_assessments) OR EXISTS(SELECT 1 FROM advance.stock_movements)
              OR (SELECT count(*) FROM advance.role_page_permissions WHERE "PageDefinitionId"=md5('quality.vendor-manual-assessments')::uuid)<>2
              OR NOT EXISTS(SELECT 1 FROM advance.role_page_permissions p JOIN advance.roles r ON r."Id"=p."RoleId"
               WHERE p."PageDefinitionId"=md5('quality.vendor-manual-assessments')::uuid AND r."Code"='PRODUCTION_MANAGER'
                AND p."CanView" AND p."CanCreate" AND p."CanViewAuditHistory" AND NOT p."CanApprove" AND NOT p."HasFullControl" AND NOT p."CanViewCommercialValues")
             THEN RAISE EXCEPTION 'Receiving-manager install must add only its scoped grant and no business rows.'; END IF;
            END $assert$;
            """);
        server.AssertRejected("receiving-manager-private-read.sql","SET SESSION AUTHORIZATION nexa_erp_runtime; SELECT * FROM advance.vendor_manual_assessments;","permission denied");
        server.Execute("receiving-manager-down.sql",deploy+migrator.GenerateScript(current,previous));
        Assert.Equal(original,await FunctionBody());
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","status"]));
        server.Execute("receiving-manager-reapply.sql",deploy+migrator.GenerateScript(previous,current));
        Assert.Equal(extended,await FunctionBody());
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","status"]));
    }
}
