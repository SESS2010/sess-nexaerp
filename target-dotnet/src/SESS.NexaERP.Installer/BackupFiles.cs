using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using Npgsql;

internal static class BackupFiles
{
    internal static readonly JsonSerializerOptions Json=new() { WriteIndented=true };
    internal const string Marker=".sess-verified-backups.json";
    internal static string Canonical(string path)
    {
        if(!Path.IsPathFullyQualified(path)) throw new InvalidOperationException("Backup paths must be absolute.");
        var full=Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        for(var current=new DirectoryInfo(full);current is not null;current=current.Parent)
            if(current.Exists && (current.Attributes&FileAttributes.ReparsePoint)!=0)
                throw new InvalidOperationException("Backup paths cannot contain reparse points.");
        if(File.Exists(full) && (File.GetAttributes(full)&FileAttributes.ReparsePoint)!=0)
            throw new InvalidOperationException("Backup files cannot be reparse points.");
        return full;
    }
    internal static string Child(string root,string name)
    {
        var canonical=Canonical(root);
        var child=Canonical(Path.Combine(canonical,name));
        if(!child.StartsWith(canonical+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Backup target escaped its owned directory.");
        return child;
    }
    internal static void CreatePrivateDirectory(string path)
    {
        path=Canonical(path);
        Directory.CreateDirectory(path);
        if(OperatingSystem.IsWindows())
        {
            using var identity=WindowsIdentity.GetCurrent();
            var security=new DirectorySecurity();
            security.SetAccessRuleProtection(true,false);
            foreach(var sid in new[]{identity.User!,new SecurityIdentifier(WellKnownSidType.LocalSystemSid,null)})
                security.AddAccessRule(new FileSystemAccessRule(sid,FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit|InheritanceFlags.ObjectInherit,PropagationFlags.None,AccessControlType.Allow));
            new DirectoryInfo(path).SetAccessControl(security);
        }
        else File.SetUnixFileMode(path,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
    }
    internal static Guid OpenRoot(string path)
    {
        path=Canonical(path);
        if(!Directory.Exists(path)) CreatePrivateDirectory(path);
        var marker=Child(path,Marker);
        if(!File.Exists(marker))
        {
            if(Directory.EnumerateFileSystemEntries(path).Any())
                throw new InvalidOperationException("Backup root is not empty and has no ownership marker.");
            CreatePrivateDirectory(path);
            using var stream=new FileStream(marker,FileMode.CreateNew,FileAccess.Write,FileShare.None);
            var id=Guid.NewGuid();
            JsonSerializer.Serialize(stream,new RootMarker(1,id),Json);
            stream.Flush(true);
            return id;
        }
        var value=JsonSerializer.Deserialize<RootMarker>(File.ReadAllText(marker));
        if(value is null || value.Format!=1 || value.Id==Guid.Empty)
            throw new InvalidOperationException("Invalid backup root ownership marker.");
        return value.Id;
    }
    internal sealed record RootMarker(int Format,Guid Id);
    internal static void AtomicJson<T>(string file,T value)
    {
        var temporary=file+".pending";
        Canonical(file); Canonical(temporary);
        using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None))
        {
            JsonSerializer.Serialize(stream,value,Json);
            stream.Flush(true);
        }
        File.Move(temporary,file,false);
    }
    internal static async Task<BackupFileEvidence> Evidence(string folder,string name)
    {
        var path=Child(folder,name);
        await using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
        var hash=Convert.ToHexString(await SHA256.HashDataAsync(stream));
        return new(name,stream.Length,hash);
    }
    internal static async Task RequireFiles(string bundle,VerifiedBackupManifest manifest)
    {
        if(manifest.Format!=1 || manifest.State!="VERIFIED"
            || manifest.Files.Length!=2
            || !manifest.Files.Select(x=>x.Name).Order().SequenceEqual(new[]{"database.dump","globals.sql"}))
            throw new InvalidOperationException("Invalid verified backup manifest.");
        foreach(var expected in manifest.Files)
            if(expected!=await Evidence(bundle,expected.Name))
                throw new InvalidOperationException("Backup hash or size verification failed.");
    }
    internal static void DeleteTree(string ownedParent,string child,string marker,string expectedMarker)
    {
        var target=Child(ownedParent,Path.GetFileName(child));
        if(!string.Equals(target,Canonical(child),StringComparison.OrdinalIgnoreCase)
            || File.ReadAllText(Child(target,marker))!=expectedMarker)
            throw new InvalidOperationException("Owned cleanup marker mismatch.");
        // Check every entry before deletion; never traverse links out of the owned tree.
        void Validate(string dir)
        {
            foreach(var entry in Directory.EnumerateFileSystemEntries(dir))
            {
                Canonical(entry);
                if((File.GetAttributes(entry)&FileAttributes.ReparsePoint)!=0)
                    throw new InvalidOperationException("Cleanup refuses a reparse point.");
                if(Directory.Exists(entry)) Validate(entry);
            }
        }
        Validate(target);
        Directory.Delete(target,true);
    }
}

internal static class BackupProcess
{
    internal static async Task Run(string bin,string tool,string log,NpgsqlConnectionStringBuilder? connection,
        string passwordFile,params string[] arguments)
    {
        var executable=BackupFiles.Child(bin,tool+(OperatingSystem.IsWindows()?".exe":""));
        if(!File.Exists(executable)) throw new InvalidOperationException("A required PostgreSQL tool is missing: "+tool);
        var capture=tool!="pg_ctl" || !arguments.Contains("start",StringComparer.Ordinal);
        var info=new ProcessStartInfo(executable)
        {
            UseShellExecute=false,CreateNoWindow=true,
            RedirectStandardOutput=capture,RedirectStandardError=capture
        };
        foreach(var key in info.Environment.Keys.Where(x=>x.StartsWith("PG",StringComparison.Ordinal)).ToArray())
            info.Environment.Remove(key);
        info.Environment["PGPASSFILE"]=passwordFile;
        info.Environment["PGCONNECT_TIMEOUT"]="15";
        if(connection is not null)
        {
            if(!string.IsNullOrEmpty(connection.Password)) info.Environment["PGPASSWORD"]=connection.Password;
            info.Environment["PGSSLMODE"]=connection.SslMode switch {
                SslMode.Disable=>"disable",SslMode.Allow=>"allow",SslMode.Prefer=>"prefer",
                SslMode.Require=>"require",SslMode.VerifyCA=>"verify-ca",SslMode.VerifyFull=>"verify-full",
                _=>throw new InvalidOperationException("Unsupported PostgreSQL TLS mode.") };
            if(!string.IsNullOrEmpty(connection.RootCertificate)) info.Environment["PGSSLROOTCERT"]=connection.RootCertificate;
            if(!string.IsNullOrEmpty(connection.SslCertificate)) info.Environment["PGSSLCERT"]=connection.SslCertificate;
            if(!string.IsNullOrEmpty(connection.SslKey)) info.Environment["PGSSLKEY"]=connection.SslKey;
            if(!string.IsNullOrEmpty(connection.SslPassword))
                throw new InvalidOperationException("Encrypted client keys require an explicitly supported backup credential mechanism.");
            foreach(var arg in new[]{"--host",connection.Host!,"--port",connection.Port.ToString(),
                "--username",connection.Username!,"--no-password"})
                info.ArgumentList.Add(arg);
        }
        foreach(var arg in arguments) info.ArgumentList.Add(arg);
        using var process=Process.Start(info)??throw new InvalidOperationException("Cannot start PostgreSQL backup tool.");
        var stdout=capture?process.StandardOutput.ReadToEndAsync():Task.FromResult("");
        var stderr=capture?process.StandardError.ReadToEndAsync():Task.FromResult("");
        using var timeout=new CancellationTokenSource(TimeSpan.FromHours(2));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch
        {
            if(!process.HasExited) process.Kill(true);
            throw new InvalidOperationException("PostgreSQL backup tool did not finish within its allowed time: "+tool);
        }
        await File.AppendAllTextAsync(log,tool+" exit="+process.ExitCode+Environment.NewLine+
            await stdout+await stderr);
        if(process.ExitCode!=0) throw new InvalidOperationException("PostgreSQL backup tool failed: "+tool+". See the protected tools log.");
    }
    internal static int ReservePort()
    {
        while(true)
        {
            var listener=new TcpListener(IPAddress.Loopback,0);
            listener.Start();
            var port=((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            if(port!=5432) return port;
        }
    }
}
