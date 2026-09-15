using System.Security.Cryptography;
using System.Text.RegularExpressions;

// Explicitly listed deployment files only. Never traverse directories or apply restored files automatically.
internal sealed class BackupConfigurationSnapshot : IAsyncDisposable
{
    private readonly List<(string Name, FileStream Stream)> files = [];
    internal static bool IsArtifact(string name) => Regex.IsMatch(name, @"\Aconfiguration-[A-Za-z0-9][A-Za-z0-9._-]{0,99}\z", RegexOptions.CultureInvariant);
    internal static BackupConfigurationSnapshot Open(VerifiedBackupConfiguration config)
    {
        var result = new BackupConfigurationSnapshot();
        try
        {
            foreach (var entry in config.ConfigurationFiles ?? [])
            {
                var name = "configuration-" + entry.Key;
                if (!IsArtifact(name) || result.files.Any(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("Configuration backup names must be unique safe file names.");
                var path = BackupFiles.Canonical(entry.Value);
                // Keep Windows writers/deleters excluded until the verified backup finishes.
                result.files.Add((name, new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)));
            }
            return result;
        }
        catch
        {
            foreach (var file in result.files) file.Stream.Dispose();
            throw;
        }
    }
    internal async Task<BackupFileEvidence[]> CopyAsync(string bundle)
    {
        var evidence = new List<BackupFileEvidence>();
        foreach (var file in files)
        {
            await using (var destination = new FileStream(BackupFiles.Child(bundle, file.Name), FileMode.CreateNew, FileAccess.Write, FileShare.None))
                await file.Stream.CopyToAsync(destination);
            evidence.Add(await BackupFiles.Evidence(bundle, file.Name));
        }
        return evidence.ToArray();
    }
    internal async Task RequireUnchanged(BackupFileEvidence[] evidence)
    {
        foreach (var file in files)
        {
            file.Stream.Position = 0;
            var current = new BackupFileEvidence(file.Name, file.Stream.Length, Convert.ToHexString(await SHA256.HashDataAsync(file.Stream)));
            if (!evidence.Contains(current)) throw new InvalidOperationException("Configuration changed during backup.");
        }
    }
    public async ValueTask DisposeAsync()
    {
        foreach (var file in files) await file.Stream.DisposeAsync();
    }
}