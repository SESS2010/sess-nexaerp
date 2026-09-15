using System.Text.Json;

namespace SESS.NexaERP.Tests;

public sealed class VerifiedBackupConfigurationTests
{
    [Fact]
    public async Task ConfigurationBundleRetainsBytesAndRejectsTampering()
    {
        var root=Path.Combine(Path.GetTempPath(),"sess-backup-config-"+Guid.NewGuid().ToString("N"));
        var rootId=BackupFiles.OpenRoot(root);
        try
        {
            var source=Path.Combine(root,"test-key.pem");
            var bytes="DISPOSABLE TLS KEY FIXTURE\n"u8.ToArray();
            await File.WriteAllBytesAsync(source,bytes);
            var id=Guid.NewGuid();
            var bundle=Path.Combine(root,"run-"+id.ToString("N"));
            BackupFiles.CreatePrivateDirectory(bundle);
            var config=new VerifiedBackupConfiguration("TEST","127.0.0.1",6543,"identity","123",root,root,Path.Combine(root,"unused"),new(){["tls.key"]=source});
            await using(var snapshot=BackupConfigurationSnapshot.Open(config))
            {
                var files=await snapshot.CopyAsync(bundle);
                await snapshot.RequireUnchanged(files);
                Assert.Equal(bytes,await File.ReadAllBytesAsync(Path.Combine(bundle,"configuration-tls.key")));
                await File.WriteAllTextAsync(Path.Combine(bundle,"database.dump"),"fixture");
                await File.WriteAllTextAsync(Path.Combine(bundle,"globals.sql"),"fixture");
                var manifest=new VerifiedBackupManifest(2,"VERIFIED",rootId,id,DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,false,"identity","123",
                    new("postgres",170000,"{}","{}",new()),[await BackupFiles.Evidence(bundle,"database.dump"),await BackupFiles.Evidence(bundle,"globals.sql"),..files]);
                await BackupFiles.RequireFiles(bundle,manifest);
                await File.AppendAllTextAsync(Path.Combine(bundle,"configuration-tls.key"),"changed");
                await Assert.ThrowsAsync<InvalidOperationException>(()=>BackupFiles.RequireFiles(bundle,manifest));
                await Assert.ThrowsAsync<InvalidOperationException>(()=>BackupFiles.RequireFiles(bundle,manifest with { Files=[..manifest.Files,files[0]] }));
            }
        }
        finally { BackupFiles.DeleteTree(Path.GetTempPath(),root,BackupFiles.Marker,await File.ReadAllTextAsync(Path.Combine(root,BackupFiles.Marker))); }
    }

    [Fact]
    public void ConfigurationNamesCannotEscapeBundleOrCollide()
    {
        var root=Path.GetTempPath();
        foreach(var name in new[]{"../key","/key","key/child","key\\child",""})
        {
            var config=new VerifiedBackupConfiguration("TEST","127.0.0.1",6543,"identity","123",root,root,root,new(){[name]=Path.Combine(root,"absent")});
            Assert.Throws<InvalidOperationException>(()=>BackupConfigurationSnapshot.Open(config));
        }
    }
}