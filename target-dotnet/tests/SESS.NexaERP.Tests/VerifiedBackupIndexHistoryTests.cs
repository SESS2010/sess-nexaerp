using System.Text.Json;
using Npgsql;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task VerifiedBackupAcceptsRenamedIndexInternalsButRefusesChangedColumnsAndIndexes()
    {
        using var server=DisposablePostgreSql.Start(FindPostgreSqlBin(),durable:true);
        server.Execute("renamed-index.sql","""
            CREATE TABLE public.backup_identity_fixture(old_name text NOT NULL);
            CREATE UNIQUE INDEX backup_identity_fixture_idx ON public.backup_identity_fixture(old_name);
            ALTER TABLE public.backup_identity_fixture RENAME COLUMN old_name TO current_name;
            INSERT INTO public.backup_identity_fixture VALUES ('retained');
            """);
        await using var source=new NpgsqlConnection(server.ConnectionString);
        await source.OpenAsync();
        Assert.Equal("old_name",await BackupDatabaseSnapshot.Scalar(source,null,
            "SELECT attname::text FROM pg_attribute WHERE attrelid='public.backup_identity_fixture_idx'::regclass AND attnum=1"));
        var baseline=await BackupDatabaseSnapshot.ReadAsync(source,null);
        var connection=new NpgsqlConnectionStringBuilder(server.ConnectionString);
        var system=await BackupDatabaseSnapshot.Scalar(source,null,"SELECT system_identifier::text FROM pg_control_system()");
        var root=Path.Combine(Path.GetTempPath(),"sess-backup-index-"+Guid.NewGuid().ToString("N"));
        BackupFiles.OpenRoot(root);
        var variable="SESS_BACKUP_INDEX_"+Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable(variable,server.ConnectionString);
        try
        {
            var config=new VerifiedBackupConfiguration(variable,"127.0.0.1",connection.Port,"advance_parser",system,
                FindPostgreSqlBin(),Path.Combine(root,"backups"),Path.Combine(root,"work"));
            var bundle=await VerifiedBackupEngine.RunAsync(config);
            var manifest=VerifiedBackupEngine.ReadManifest(config.BackupRoot,BackupFiles.OpenRoot(config.BackupRoot),bundle);
            Assert.Equal("VERIFIED",manifest.State);
            var restored=JsonSerializer.Deserialize<BackupDatabaseEvidence>(await File.ReadAllTextAsync(Path.Combine(bundle,"last-restored-evidence.json")),BackupFiles.Json)!;
            Assert.NotEqual(manifest.DatabaseEvidence.Metadata,restored.Metadata); // Physical index label really differs.
            BackupDatabaseSnapshot.RequireEqual(manifest.DatabaseEvidence,restored);
            Assert.Equal(1,restored.TableCounts["\"public\".\"backup_identity_fixture\""]);

            server.Execute("changed-real-column.sql","ALTER TABLE public.backup_identity_fixture RENAME COLUMN current_name TO changed_name;");
            var changedColumn=await BackupDatabaseSnapshot.ReadAsync(source,null);
            Assert.Throws<InvalidOperationException>(()=>BackupDatabaseSnapshot.RequireEqual(baseline,changedColumn));
            server.Execute("changed-index.sql","""
                ALTER TABLE public.backup_identity_fixture RENAME COLUMN changed_name TO current_name;
                DROP INDEX public.backup_identity_fixture_idx;
                CREATE INDEX backup_identity_fixture_idx ON public.backup_identity_fixture(current_name);
                """);
            var changedIndex=await BackupDatabaseSnapshot.ReadAsync(source,null);
            Assert.Throws<InvalidOperationException>(()=>BackupDatabaseSnapshot.RequireEqual(baseline,changedIndex));
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable,null);
            BackupFiles.DeleteTree(Path.GetTempPath(),root,BackupFiles.Marker,await File.ReadAllTextAsync(Path.Combine(root,BackupFiles.Marker)));
        }
    }
}
