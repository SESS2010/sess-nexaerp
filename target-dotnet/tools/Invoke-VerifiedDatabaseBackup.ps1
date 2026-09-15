[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$InstallerPath,
    [Parameter(Mandatory=$true)][string]$ConfigPath,
    [Parameter(Mandatory=$true)][string]$CredentialFile,
    [Parameter(Mandatory=$true)][string]$LogDirectory
)
$ErrorActionPreference='Stop'
$exitCode=1
$pointer=[IntPtr]::Zero
$variable=$null
$previous=$null
$log=$null
$statusReady=$false
try {
    foreach($path in @($InstallerPath,$ConfigPath,$CredentialFile,$LogDirectory)) {
        if(-not [IO.Path]::IsPathRooted($path) -or $path.IndexOfAny([char[]]@([char]34,[char]13,[char]10)) -ge 0) { throw 'Absolute paths without quotes or newlines are required.' }
        $current=[IO.Path]::GetFullPath($path)
        while($current) {
            if(Test-Path -LiteralPath $current) {
                if(((Get-Item -LiteralPath $current -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                    throw 'Scheduled backup paths cannot contain reparse points.'
                }
            }
            $current=[IO.Path]::GetDirectoryName($current)
        }
    }
    if(-not (Test-Path -LiteralPath $LogDirectory -PathType Container)) { throw 'Create a protected log directory before scheduling.' }
    $statusReady=$true
    $config=Get-Content -LiteralPath $ConfigPath -Raw | ConvertFrom-Json
    $variable=[string]$config.ConnectionEnvironment
    if($variable -notmatch '\A[A-Za-z_][A-Za-z0-9_]*\z') { throw 'Invalid connection environment variable name.' }
    $secret=Import-Clixml -LiteralPath $CredentialFile
    if($secret -isnot [Security.SecureString]) { throw 'Credential file must contain a SecureString exported by the scheduled Windows account.' }
    $previous=[Environment]::GetEnvironmentVariable($variable,'Process')
    $pointer=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($secret)
    [Environment]::SetEnvironmentVariable($variable,[Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer),'Process')
    $log=Join-Path $LogDirectory ((Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ')+'-'+[Guid]::NewGuid().ToString('N')+'.log')
    # Capture the native exit code explicitly. PowerShell 5 stderr handling must not
    # throw past the status write or leave yesterday's successful result in place.
    $info=[Diagnostics.ProcessStartInfo]::new()
    $info.FileName=$InstallerPath
    $info.Arguments='backup run --config "'+$ConfigPath+'"'
    $info.UseShellExecute=$false
    $info.CreateNoWindow=$true
    $info.RedirectStandardOutput=$true
    $info.RedirectStandardError=$true
    $process=[Diagnostics.Process]::Start($info)
    try {
        $stdout=$process.StandardOutput.ReadToEndAsync()
        $stderr=$process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        $exitCode=$process.ExitCode
        [IO.File]::WriteAllText($log,$stdout.Result+$stderr.Result,[Text.UTF8Encoding]::new($false))
    } finally { $process.Dispose() }
}
catch {
    Write-Error 'Scheduled backup failed. Inspect Task Scheduler result and protected backup logs.' -ErrorAction Continue
    $exitCode=1
}
finally {
    if($variable -and $pointer -ne [IntPtr]::Zero) { [Environment]::SetEnvironmentVariable($variable,$previous,'Process') }
    if($pointer -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer) }
    if($statusReady) {
        $temporary=Join-Path $LogDirectory ('last-run-'+[Guid]::NewGuid().ToString('N')+'.pending')
        try {
            foreach($file in @($temporary,(Join-Path $LogDirectory 'last-run.json'))) {
                if((Test-Path -LiteralPath $file) -and ((Get-Item -LiteralPath $file -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Backup status cannot be a reparse point.' }
            }
            $result=[ordered]@{ FinishedUtc=(Get-Date).ToUniversalTime().ToString('o'); ExitCode=$exitCode; Log=$log }
            $result | ConvertTo-Json | Set-Content -LiteralPath $temporary -Encoding UTF8
            $status=Join-Path $LogDirectory 'last-run.json'
            if(Test-Path -LiteralPath $status) { [IO.File]::Replace($temporary,$status,[System.Management.Automation.Language.NullString]::Value) }
            else { [IO.File]::Move($temporary,$status) }
        }
        catch {
            $exitCode=1
            Write-Error 'Could not record backup status. Treat this run as failed.' -ErrorAction Continue
        }
        finally { if(Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary } }
    }
}
exit $exitCode
