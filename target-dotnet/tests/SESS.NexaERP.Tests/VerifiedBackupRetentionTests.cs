using System.Text.Json;

namespace SESS.NexaERP.Tests;

public sealed class VerifiedBackupRetentionTests
{
    [Fact]
    public async Task RetentionPreservesNewestTwoWeeklyWindowAndUnverifiedWork()
    {
        var root=Path.Combine(Path.GetTempPath(),"sess-backup-retention-"+Guid.NewGuid().ToString("N"));
        var rootId=BackupFiles.OpenRoot(root);
        var now=DateTimeOffset.UtcNow;
        var newest=await Bundle(root,rootId,now.AddDays(-1),false);
        var second=await Bundle(root,rootId,now.AddDays(-2),false);
        var expiredDaily=await Bundle(root,rootId,now.AddDays(-31),false);
        var weekly=await Bundle(root,rootId,now.AddDays(-83),true);
        var expiredWeekly=await Bundle(root,rootId,now.AddDays(-85),true);
        var incomplete=Path.Combine(root,"run-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(incomplete);
        await File.WriteAllTextAsync(Path.Combine(incomplete,"started.json"),"unfinished witness");
        await VerifiedBackupEngine.Retain(root,rootId,now);
        Assert.True(Directory.Exists(newest));
        Assert.True(Directory.Exists(second));
        Assert.True(Directory.Exists(weekly));
        Assert.True(Directory.Exists(incomplete));
        Assert.False(Directory.Exists(expiredDaily));
        Assert.False(Directory.Exists(expiredWeekly));
        var marker=await File.ReadAllTextAsync(Path.Combine(root,BackupFiles.Marker));
        BackupFiles.DeleteTree(Path.GetTempPath(),root,BackupFiles.Marker,marker);
    }

    [Fact]
    public async Task RetentionProtectsTwoOldBackupsAndRefusesUnexpectedFiles()
    {
        var root=Path.Combine(Path.GetTempPath(),"sess-backup-retention-"+Guid.NewGuid().ToString("N"));
        var rootId=BackupFiles.OpenRoot(root);
        var now=DateTimeOffset.UtcNow;
        var newest=await Bundle(root,rootId,now.AddDays(-100),false);
        var second=await Bundle(root,rootId,now.AddDays(-101),false);
        var oldest=await Bundle(root,rootId,now.AddDays(-102),false);
        var unexpected=Path.Combine(oldest,"personal-note.txt");
        await File.WriteAllTextAsync(unexpected,"Must remain.");
        var exception=await Assert.ThrowsAsync<InvalidOperationException>(()=>VerifiedBackupEngine.Retain(root,rootId,now));
        Assert.Contains("unexpected",exception.Message,StringComparison.Ordinal);
        Assert.Equal("Must remain.",await File.ReadAllTextAsync(unexpected));
        Assert.True(File.Exists(Path.Combine(oldest,"database.dump")));
        Assert.True(Directory.Exists(newest));
        Assert.True(Directory.Exists(second));
        Assert.Throws<InvalidOperationException>(()=>BackupFiles.Child(root,"../outside"));
        var marker=await File.ReadAllTextAsync(Path.Combine(root,BackupFiles.Marker));
        BackupFiles.DeleteTree(Path.GetTempPath(),root,BackupFiles.Marker,marker);
    }

    private static async Task<string> Bundle(string root,Guid rootId,DateTimeOffset date,bool weekly)
    {
        // Policy-only fixtures: these are not database-restorability witnesses.
        var id=Guid.NewGuid();
        var bundle=Path.Combine(root,"run-"+id.ToString("N"));
        Directory.CreateDirectory(bundle);
        await File.WriteAllTextAsync(Path.Combine(bundle,"database.dump"),"retention fixture");
        await File.WriteAllTextAsync(Path.Combine(bundle,"globals.sql"),"retention fixture");
        var evidence=new BackupDatabaseEvidence("postgres",170000,"{}","{}",new(StringComparer.Ordinal));
        var manifest=new VerifiedBackupManifest(1,"VERIFIED",rootId,id,date,date,weekly,"advance_parser","123",evidence,
            [await BackupFiles.Evidence(bundle,"database.dump"),await BackupFiles.Evidence(bundle,"globals.sql")]);
        await File.WriteAllTextAsync(Path.Combine(bundle,"manifest.json"),JsonSerializer.Serialize(manifest,BackupFiles.Json));
        return bundle;
    }
}
