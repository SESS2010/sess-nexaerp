using System.Diagnostics;
using System.Text.Json;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task ScheduledBackupFailureReplacesOldSuccessWithoutLeakingConnection()
    {
        var root=Path.Combine(Path.GetTempPath(),"sess-backup-failure-"+Guid.NewGuid().ToString("N"));
        BackupFiles.OpenRoot(root);
        try
        {
            var config=Path.Combine(root,"config.json");
            var credential=Path.Combine(root,"credential.clixml");
            var logs=Path.Combine(root,"logs");
            BackupFiles.CreatePrivateDirectory(logs);
            // Deliberately invalid source identity is refused before any database connection.
            await File.WriteAllTextAsync(config,JsonSerializer.Serialize(new VerifiedBackupConfiguration(
                "SESS_BACKUP_FAILURE_FIXTURE","127.0.0.1",1,"postgres","123",root,Path.Combine(root,"backups"),Path.Combine(root,"work"))));
            var status=Path.Combine(logs,"last-run.json");
            await File.WriteAllTextAsync(status,"{\"ExitCode\":0,\"FinishedUtc\":\"2020-01-01T00:00:00Z\"}");
            var installer=Path.Combine(FindRepositoryRoot(),"src","SESS.NexaERP.Installer","bin",
#if DEBUG
                "Debug",
#else
                "Release",
#endif
                "net10.0","SESS.NexaERP.Installer.exe");
            var wrapper=Path.Combine(FindRepositoryRoot(),"tools","Invoke-VerifiedDatabaseBackup.ps1");
            static string Quote(string value)=>"'"+value.Replace("'","''")+"'";
            const string marker="Never-Print-This-Backup-Witness-Connection";
            var command="ConvertTo-SecureString "+Quote(marker)+" -AsPlainText -Force | Export-Clixml -LiteralPath "+Quote(credential)+
                "; & "+Quote(wrapper)+" -InstallerPath "+Quote(installer)+" -ConfigPath "+Quote(config)+
                " -CredentialFile "+Quote(credential)+" -LogDirectory "+Quote(logs);
            var info=new ProcessStartInfo("powershell.exe"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
            foreach(var argument in new[]{"-NoProfile","-NonInteractive","-Command",command})info.ArgumentList.Add(argument);
            using var process=Process.Start(info)!;
            var stdout=process.StandardOutput.ReadToEndAsync();
            var stderr=process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            var output=await stdout+await stderr;
            Assert.Equal(1,process.ExitCode);
            Assert.DoesNotContain(marker,output);
            using var json=JsonDocument.Parse(await File.ReadAllTextAsync(status));
            Assert.Equal(1,json.RootElement.GetProperty("ExitCode").GetInt32());
            Assert.True(DateTimeOffset.Parse(json.RootElement.GetProperty("FinishedUtc").GetString()!)>DateTimeOffset.UtcNow.AddMinutes(-5));
            var log=json.RootElement.GetProperty("Log").GetString()!;
            Assert.True(File.Exists(log));
            Assert.DoesNotContain(marker,await File.ReadAllTextAsync(log));
        }
        finally { BackupFiles.DeleteTree(Path.GetTempPath(),root,BackupFiles.Marker,await File.ReadAllTextAsync(Path.Combine(root,BackupFiles.Marker))); }
    }
}
