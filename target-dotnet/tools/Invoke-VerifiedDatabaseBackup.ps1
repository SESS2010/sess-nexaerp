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
try {
    foreach($path in @($InstallerPath,$ConfigPath,$CredentialFile,$LogDirectory)) {
        if(-not [IO.Path]::IsPathRooted($path)) { throw 'Absolute paths are required.' }
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
    $config=Get-Content -LiteralPath $ConfigPath -Raw | ConvertFrom-Json
    $variable=[string]$config.ConnectionEnvironment
    if($variable -notmatch '\A[A-Za-z_][A-Za-z0-9_]*\z') { throw 'Invalid connection environment variable name.' }
    $secret=Import-Clixml -LiteralPath $CredentialFile
    if($secret -isnot [Security.SecureString]) { throw 'Credential file must contain a SecureString exported by the scheduled Windows account.' }
    $previous=[Environment]::GetEnvironmentVariable($variable,'Process')
    $pointer=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($secret)
    [Environment]::SetEnvironmentVariable($variable,[Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer),'Process')
    $log=Join-Path $LogDirectory ((Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ')+'-'+[Guid]::NewGuid().ToString('N')+'.log')
    & $InstallerPath backup run --config $ConfigPath *> $log
    $exitCode=$LASTEXITCODE
    if($null -eq $exitCode) { $exitCode=1 }
    $result=[ordered]@{ FinishedUtc=(Get-Date).ToUniversalTime().ToString('o'); ExitCode=$exitCode; Log=$log }
    $result | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $LogDirectory 'last-run.json') -Encoding UTF8
}
catch {
    # Never print the decrypted connection string or arbitrary command output.
    Write-Error 'Scheduled backup failed. Inspect Task Scheduler result and protected backup logs.' -ErrorAction Continue
    $exitCode=1
}
finally {
    if($variable) { [Environment]::SetEnvironmentVariable($variable,$previous,'Process') }
    if($pointer -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer) }
}
exit $exitCode
