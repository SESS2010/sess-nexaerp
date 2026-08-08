[CmdletBinding()]
param(
    [string]$Database = "sess_nexaerp",
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$UserName = "postgres",
    [string]$GitPath = "",
    [switch]$GeneratePlanOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$targetRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$pgBin = "C:\Program Files\PostgreSQL\17\bin"
$pgDumpPath = Join-Path $pgBin "pg_dump.exe"
$psqlPath = Join-Path $pgBin "psql.exe"
$backupDir = Join-Path $targetRoot "backups\post-rev868-safety-baseline"
$reportDir = Join-Path $targetRoot "local-evidence\rev868"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = Join-Path $backupDir "$Database`_post_rev868_safety_baseline_$timestamp.dump"
$reportFile = Join-Path $reportDir "rev868_post_safety_backup_$timestamp.md"
$plainPassword = $null
$securePassword = $null

function Add-Report([string]$Text) { Add-Content -LiteralPath $reportFile -Value $Text -Encoding utf8 }
function Assert-SafePgIdentifier([string]$Name, [string]$Label) { if ($Name -notmatch '^[A-Za-z_][A-Za-z0-9_]{0,62}$') { throw "$Label is not a safe PostgreSQL identifier." } }
function Resolve-RequiredFile([string]$Path, [string]$Label) { $item = Get-Item -LiteralPath $Path -ErrorAction Stop; if (-not $item.Exists) { throw "$Label was not found." }; return $item.FullName }
function Get-PlanText {
@"
REV868 post-safety-backup helper plan
Expected host: $HostName
Expected port: $Port
Expected database: $Database
Expected user: $UserName
Backup folder: $backupDir
Backup naming: post-REV868 safety baseline only, never pre-REV868
Operations when not GeneratePlanOnly:
1. Prompt for PostgreSQL password with Read-Host -AsSecureString.
2. Set PGPASSWORD only in this process.
3. Verify current_database() equals sess_nexaerp.
4. Run pg_dump custom-format backup to the post-REV868 safety baseline folder.
5. Verify non-zero backup size.
6. Calculate SHA-256.
7. Write sanitized report.
8. Clear password/environment variables in finally.
No migration apply/remove/rollback command is present.
"@
}
function Invoke-PsqlRead([string]$Sql) {
    $sqlFile = Join-Path ([System.IO.Path]::GetTempPath()) ("sess_nexa_rev868_backup_" + [Guid]::NewGuid().ToString("N") + ".sql")
    try {
        [System.IO.File]::WriteAllText($sqlFile, $Sql, [System.Text.UTF8Encoding]::new($false))
        $output = & $psql -h $HostName -p $Port -U $UserName -d $Database -v ON_ERROR_STOP=1 -At -f $sqlFile 2>&1
        if ($LASTEXITCODE -ne 0) { throw "psql failed with exit code $LASTEXITCODE." }
        return ($output -join "`n")
    }
    finally { Remove-Item -LiteralPath $sqlFile -Force -ErrorAction SilentlyContinue }
}

try {
    Assert-SafePgIdentifier $Database "Database name"
    Assert-SafePgIdentifier $UserName "PostgreSQL user name"
    if ($HostName -ne "localhost" -or $Port -ne 5432 -or $Database -ne "sess_nexaerp") { throw "Helper is restricted to localhost:5432 / sess_nexaerp." }
    if ($GeneratePlanOnly) { Get-PlanText; return }

    $pgDump = Resolve-RequiredFile $pgDumpPath "pg_dump.exe"
    $psql = Resolve-RequiredFile $psqlPath "psql.exe"
    New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null

    Write-Host "Expected host: $HostName"
    Write-Host "Expected port: $Port"
    Write-Host "Expected database: $Database"
    Write-Host "PostgreSQL user parameter: $UserName"
    $securePassword = Read-Host -AsSecureString "Enter PostgreSQL password for post-REV868 safety backup only"
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    try { $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
    finally { if ($bstr -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) } }
    $env:PGPASSWORD = $plainPassword
    $env:NexaErp__ExpectedDatabase = $Database

    $identity = Invoke-PsqlRead "select 'database=' || current_database() union all select 'user=' || current_user union all select 'server_port=' || inet_server_port()::text;"
    if ($identity -notmatch "database=$Database") { throw "Connected database mismatch. Refusing backup." }

    & $pgDump -h $HostName -p $Port -U $UserName -d $Database -F c -f $backupFile
    if ($LASTEXITCODE -ne 0) { throw "pg_dump failed with exit code $LASTEXITCODE." }
    $backupItem = Get-Item -LiteralPath $backupFile
    if ($backupItem.Length -le 0) { throw "Post-REV868 safety backup has zero size." }
    $backupHash = (Get-FileHash -LiteralPath $backupFile -Algorithm SHA256).Hash

    Add-Report "# REV868 Post-Safety Backup Report"
    Add-Report "- Backup type: post-REV868 safety baseline, not pre-REV868"
    Add-Report "- Database: $Database"
    Add-Report "- Identity: $identity"
    Add-Report "- Backup path: $backupFile"
    Add-Report "- Backup bytes: $($backupItem.Length)"
    Add-Report "- Backup SHA-256: $backupHash"
    Add-Report "- Note: this backup was created after REV868 migration application and must not be used as pre-REV868 evidence."
    Write-Host "REV868 post-safety backup report: $reportFile"
    Write-Host "Backup file: $backupFile"
    Write-Host "Backup SHA-256: $backupHash"
}
finally {
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:\NexaErp__ExpectedDatabase -ErrorAction SilentlyContinue
    if ($plainPassword) { $plainPassword = $null }
    if ($securePassword) { $securePassword.Dispose() }
}
