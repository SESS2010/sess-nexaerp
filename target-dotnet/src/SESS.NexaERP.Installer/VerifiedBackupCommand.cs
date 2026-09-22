using System.Text.Json;

internal static class VerifiedBackupCommand
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if(args.Length<3 || args[0] is not ("run" or "verify" or "recover") || args[1]!="--config")
        {
            Console.Error.WriteLine("Usage: backup run --config FILE | backup verify --config FILE --bundle DIRECTORY | backup recover --config FILE --bundle DIRECTORY --destination NEW_DIRECTORY");
            return 2;
        }
        try
        {
            var config=JsonSerializer.Deserialize<VerifiedBackupConfiguration>(
                File.ReadAllText(BackupFiles.Canonical(args[2])),BackupFiles.Json)
                ?? throw new InvalidOperationException("Backup configuration is missing.");
            if(args[0]=="run" && args.Length==3)
            {
                var bundle=await VerifiedBackupEngine.RunAsync(config);
                Console.WriteLine("VERIFIED_BACKUP: "+bundle);
                return 0;
            }
            if(args[0]=="verify" && args.Length==5 && args[3]=="--bundle")
            {
                await VerifiedBackupEngine.VerifyAsync(config,args[4]);
                Console.WriteLine("RESTORE_VERIFIED: "+args[4]);
                return 0;
            }
            if(args[0]=="recover" && args.Length==7 && args[3]=="--bundle" && args[5]=="--destination")
            {
                await VerifiedBackupEngine.VerifyAsync(config,args[4],args[6]);
                Console.WriteLine("RESTORED_VERIFIED_STOPPED: "+args[6]);
                return 0;
            }
            Console.Error.WriteLine("REFUSED: unexpected backup command arguments.");
            return 2;
        }
        catch(Exception exception) when(exception is IOException or InvalidOperationException or
            UnauthorizedAccessException or JsonException or Npgsql.NpgsqlException or ArgumentException)
        {
            // Connection strings and arbitrary PostgreSQL error details must not reach scheduler logs.
            Console.Error.WriteLine("BACKUP_FAILED: "+exception.GetType().Name+". Inspect the manifest, protected backup evidence and configuration; verification or retention may be incomplete.");
            return 1;
        }
    }
}
