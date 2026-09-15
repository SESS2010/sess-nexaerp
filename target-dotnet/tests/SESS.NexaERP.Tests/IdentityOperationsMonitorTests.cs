using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task IdentityOperationsMonitorDetectsProviderFailureAndStaleIdentityBackup()
    {
        var root=Path.Combine(Path.GetTempPath(),"sess-identity-monitor-"+Guid.NewGuid().ToString("N"));
        BackupFiles.OpenRoot(root);
        var healthy=true;
        var origin="";
        var builder=WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var app=builder.Build();
        app.MapGet("/health/ready",()=>Results.Json(new {status=healthy ? "UP":"DOWN",
            checks=new[]{new {name="Keycloak database connections async health check",status=healthy ? "UP":"DOWN"}}}));
        foreach(var realm in new[]{"staff","approvers"})
        {
            var name=realm;
            app.MapGet("/"+name+"/discovery",()=>Results.Json(new{issuer=origin+"/"+name,jwks_uri=origin+"/"+name+"/keys"}));
            app.MapGet("/"+name+"/keys",()=>Results.Json(new{keys=new[]{new{kid="monitor-only-fixture",kty="RSA"}}}));
        }
        await app.StartAsync();
        origin=app.Urls.Single();
        try
        {
            var erp=Path.Combine(root,"erp.json");
            var identity=Path.Combine(root,"identity.json");
            await WriteBackup(erp,DateTimeOffset.UtcNow);
            await WriteBackup(identity,DateTimeOffset.UtcNow);
            var config=Path.Combine(root,"monitor.json");
            await File.WriteAllTextAsync(config,JsonSerializer.Serialize(new{
                ReadyUrl=origin+"/health/ready",StatusDirectory=root,MaximumBackupAgeHours=26,
                Realms=new[]{"staff","approvers"}.Select(name=>new{Name=name,DiscoveryUrl=origin+"/"+name+"/discovery",Issuer=origin+"/"+name,JwksUrl=origin+"/"+name+"/keys"}),
                BackupResults=new[]{new{Name="ERP",Path=erp},new{Name="Identity",Path=identity}}
            }));
            Assert.Equal(0,await Run());
            Assert.Equal("HEALTHY",ReadState());
            healthy=false;
            Assert.Equal(1,await Run());
            Assert.Equal("ACTION_REQUIRED",ReadState());
            healthy=true;
            await WriteBackup(identity,DateTimeOffset.UtcNow.AddHours(-27));
            Assert.Equal(1,await Run());
            Assert.Equal("ACTION_REQUIRED",ReadState());
            await WriteBackup(identity,DateTimeOffset.UtcNow);
            var configured=System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllTextAsync(config))!;
            configured["BackupResults"]![1]!["TaskName"]="SESS-NexaERP-Absent-Witness-"+Guid.NewGuid().ToString("N");
            await File.WriteAllTextAsync(config,configured.ToJsonString());
            Assert.Equal(1,await Run());
            Assert.Equal("ACTION_REQUIRED",ReadState());
            var html=await File.ReadAllTextAsync(Path.Combine(root,"identity-status.html"));
            Assert.Contains("MONITOR STALE",html);
            Assert.Contains("Identity verified backup: CHECK REQUIRED",html);

            async Task<int> Run()
            {
                var info=new ProcessStartInfo("powershell.exe"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
                foreach(var argument in new[]{"-NoProfile","-NonInteractive","-File",Path.Combine(FindRepositoryRoot(),"tools","Test-IdentityOperations.ps1"),"-ConfigPath",config})
                    info.ArgumentList.Add(argument);
                using var process=Process.Start(info)!;
                var stdout=process.StandardOutput.ReadToEndAsync();
                var stderr=process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                await File.WriteAllTextAsync(Path.Combine(root,"monitor-last.log"),await stdout+await stderr);
                return process.ExitCode;
            }
            string ReadState()
            {
                using var json=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"identity-status.json")));
                return json.RootElement.GetProperty("State").GetString()!;
            }
        }
        finally
        {
            await app.StopAsync();
            BackupFiles.DeleteTree(Path.GetTempPath(),root,BackupFiles.Marker,await File.ReadAllTextAsync(Path.Combine(root,BackupFiles.Marker)));
        }
        static Task WriteBackup(string path,DateTimeOffset time)=>File.WriteAllTextAsync(path,JsonSerializer.Serialize(new{ExitCode=0,FinishedUtc=time.ToString("o")}));
    }
}
