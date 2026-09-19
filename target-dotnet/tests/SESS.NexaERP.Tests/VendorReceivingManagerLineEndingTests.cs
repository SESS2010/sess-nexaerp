using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SESS.NexaERP.Infrastructure.Persistence;
namespace SESS.NexaERP.Tests;
public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task ReceivingManagerMigrationAcceptsCrLfStoredFunctionButRejectsOtherDrift()
    {
        const string previous="20260919130000_VendorManualAssessments";
        const string current="20260919160000_VendorReceivingManagerAssessments";
        const string signature="advance.record_vendor_manual_assessment(uuid,uuid,uuid,bigint,uuid,numeric,numeric,numeric,text,uuid,text,uuid,text,text)";
        using var model=new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator=model.GetService<IMigrator>();
        using var server=DisposablePostgreSql.Start(FindPostgreSqlBin(),databaseName:"sess_manager_crlf");
        server.Execute("receiving-manager-crlf-baseline.sql",migrator.GenerateScript("0",previous));
        await using var connection=new NpgsqlConnection(server.ConnectionString);await connection.OpenAsync();
        async Task<string> Scalar(string sql)
        {
            await using var command=new NpgsqlCommand(sql,connection);
            return (string)(await command.ExecuteScalarAsync())!;
        }
        var definition=await Scalar($"SELECT pg_get_functiondef(to_regprocedure('{signature}'))");
        var original=await Scalar($"SELECT prosrc FROM pg_proc WHERE oid=to_regprocedure('{signature}')");
        var crlf=definition.Replace("\r\n","\n",StringComparison.Ordinal).Replace("\n","\r\n",StringComparison.Ordinal);
        // Send actual CRLF bytes through Npgsql; psql script reading must not erase the witness.
        await using(var command=new NpgsqlCommand(crlf,connection))await command.ExecuteNonQueryAsync();
        var stored=await Scalar($"SELECT prosrc FROM pg_proc WHERE oid=to_regprocedure('{signature}')");
        Assert.Contains("\r\n",stored);
        Assert.Equal(original.Replace("\r\n","\n",StringComparison.Ordinal),stored.Replace("\r\n","\n",StringComparison.Ordinal));
        server.Execute("receiving-manager-crlf-up.sql",migrator.GenerateScript(previous,current));
        server.Execute("receiving-manager-crlf-down.sql",migrator.GenerateScript(current,previous));
        var restored=await Scalar($"SELECT prosrc FROM pg_proc WHERE oid=to_regprocedure('{signature}')");
        Assert.Equal(original.Replace("\r\n","\n",StringComparison.Ordinal),restored.Replace("\r\n","\n",StringComparison.Ordinal));
        const string rule="p_technical BETWEEN 0 AND 15";
        Assert.Contains(rule,definition);
        var changed=definition.Replace(rule,"p_technical BETWEEN 1 AND 15",StringComparison.Ordinal);
        await using(var command=new NpgsqlCommand(changed,connection))await command.ExecuteNonQueryAsync();
        server.AssertRejected("receiving-manager-refuse-rule-drift.sql",migrator.GenerateScript(previous,current),"differs from the reviewed authority contract");
        Assert.Contains("p_technical BETWEEN 1 AND 15",await Scalar($"SELECT prosrc FROM pg_proc WHERE oid=to_regprocedure('{signature}')"));
        server.Execute("receiving-manager-no-partial-crlf-install.sql","""
            DO $assert$ BEGIN
             IF EXISTS(SELECT 1 FROM advance.vendor_manual_assessments)
              OR EXISTS(SELECT 1 FROM advance.role_page_permissions p JOIN advance.roles r ON r."Id"=p."RoleId"
               WHERE r."Code"='PRODUCTION_MANAGER' AND p."PageDefinitionId"=md5('quality.vendor-manual-assessments')::uuid)
             THEN RAISE EXCEPTION 'Rejected definition drift must leave no manager grant or business assessment.'; END IF;
            END $assert$;
            """);
    }
}
