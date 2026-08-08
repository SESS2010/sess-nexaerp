[CmdletBinding()]
param(
    [string]$Database = "sess_nexaerp",
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$UserName = "postgres",
    [string]$GitPath = "",
    [switch]$PreflightOnly,
    [switch]$GenerateSqlOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$targetRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$dotnetPath = Join-Path $targetRoot "..\.dotnet10\dotnet.exe"
$psqlPath = "C:\Program Files\PostgreSQL\17\bin\psql.exe"
$reportDir = Join-Path $targetRoot "local-evidence\rev868"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$reportFile = Join-Path $reportDir "rev868_preflight_$timestamp.md"
$migrationName = "20260808182945_Rev868PurchaseRequisitionFoundation"
$securePassword = $null
$plainPassword = $null

function Write-Section([string]$Text) { Write-Host ""; Write-Host "== $Text ==" }
function Add-Report([string]$Text) { Add-Content -LiteralPath $reportFile -Value $Text -Encoding utf8 }
function Assert-SafePgIdentifier([string]$Name, [string]$Label) { if ($Name -notmatch '^[A-Za-z_][A-Za-z0-9_]{0,62}$') { throw "$Label is not a safe PostgreSQL identifier." } }
function Resolve-ExecutablePath([string]$Path, [string]$Label) { $r = Resolve-Path -LiteralPath $Path -ErrorAction Stop; $i = Get-Item -LiteralPath $r.Path -ErrorAction Stop; if (-not $i.Exists) { throw "$Label was not found: $Path" }; return $i.FullName }
function Resolve-GitExecutable([string]$ExplicitGitPath) {
    $candidates = New-Object System.Collections.Generic.List[string]
    if (-not [string]::IsNullOrWhiteSpace($ExplicitGitPath)) { $candidates.Add($ExplicitGitPath) }
    $cmd = Get-Command git.exe -ErrorAction SilentlyContinue; if ($cmd -and $cmd.Source) { $candidates.Add($cmd.Source) }
    $candidates.Add("C:\Program Files\Git\cmd\git.exe"); $candidates.Add("C:\Program Files\Git\bin\git.exe"); $candidates.Add("C:\Users\User\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\git\cmd\git.exe")
    foreach ($candidate in $candidates | Select-Object -Unique) { if (Test-Path -LiteralPath $candidate -PathType Leaf) { $item = Get-Item -LiteralPath $candidate; if ($item.Name -ne "git.exe") { throw "Resolved Git path is not git.exe: $($item.FullName)" }; $version = & $item.FullName --version; if ($LASTEXITCODE -eq 0 -and ($version -join "`n") -match '^git version ') { return $item.FullName } } }
    throw "git.exe was not found. Pass -GitPath with a valid git.exe path."
}
function Get-PreflightSql {
    $queries = [ordered]@{}
    $queries["Session identity"] = @"
select 'database=' || current_database()
union all select 'user=' || current_user
union all select 'server_addr=' || coalesce(inet_server_addr()::text, 'local_socket')
union all select 'server_port=' || inet_server_port()::text;
"@.Trim()
    $queries["Applied migration IDs"] = @"
select "MigrationId"
from "public"."__EFMigrationsHistory"
order by "MigrationId";
"@.Trim()
    $queries["REV868 migration presence"] = @"
select case when exists (
    select 1 from "public"."__EFMigrationsHistory"
    where "MigrationId" = '20260808182945_Rev868PurchaseRequisitionFoundation'
) then 'present' else 'absent' end;
"@.Trim()
    return $queries
}
function Test-SqlText([string]$Title, [string]$Sql) { if ([string]::IsNullOrWhiteSpace($Sql)) { throw "$Title SQL is empty." }; if (-not $Sql.TrimEnd().EndsWith(';')) { throw "$Title SQL has no statement terminator." }; if ((([regex]::Matches($Sql, "'")).Count % 2) -ne 0) { throw "$Title SQL has unbalanced quotes." } }
function Invoke-PsqlRead([string]$Sql) {
    $sqlFile = Join-Path ([System.IO.Path]::GetTempPath()) ("sess_nexa_rev868_" + [Guid]::NewGuid().ToString("N") + ".sql")
    try { [System.IO.File]::WriteAllText($sqlFile, $Sql, [System.Text.UTF8Encoding]::new($false)); $output = & $psql -h $HostName -p $Port -U $UserName -d $Database -v ON_ERROR_STOP=1 -At -f $sqlFile 2>&1; if ($LASTEXITCODE -ne 0) { throw "psql failed with exit code $LASTEXITCODE." }; return ($output -join "`n") }
    finally { Remove-Item -LiteralPath $sqlFile -Force -ErrorAction SilentlyContinue }
}
function Write-SqlOnlyReport([System.Collections.Specialized.OrderedDictionary]$SqlMap) {
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
    Add-Report "# REV868 Secure Helper SQL Source Verification"
    Add-Report ""
    Add-Report "- Mode: GenerateSqlOnly"
    Add-Report "- Expected host: $HostName"
    Add-Report "- Expected port: $Port"
    Add-Report "- Expected database: $Database"
    Add-Report "- Migration target: $migrationName"
    Add-Report "- No password requested and no PostgreSQL connection attempted."
    foreach ($entry in $SqlMap.GetEnumerator()) { Add-Report "## $($entry.Key)"; Add-Report '```sql'; Add-Report ([string]$entry.Value); Add-Report '```' }
    Write-Host "REV868 SQL source report: $reportFile"
}

try {
    Write-Section "REV868 no-secret prechecks"
    Assert-SafePgIdentifier $Database "Database name"
    Assert-SafePgIdentifier $UserName "PostgreSQL user name"
    if ($HostName -ne "localhost" -or $Port -ne 5432) { throw "REV868 helper is restricted to localhost:5432." }
    if ($Database -ne "sess_nexaerp") { throw "REV868 helper expected database guard failed. Approved target is sess_nexaerp only." }
    $sql = Get-PreflightSql
    foreach ($entry in $sql.GetEnumerator()) { Test-SqlText $entry.Key ([string]$entry.Value) }
    if ($GenerateSqlOnly) { Write-SqlOnlyReport $sql; foreach ($entry in $sql.GetEnumerator()) { Write-Output "-- $($entry.Key)"; Write-Output ([string]$entry.Value); Write-Output "" }; return }
    $gitExe = Resolve-GitExecutable $GitPath
    $dotnet = Resolve-ExecutablePath $dotnetPath ".NET executable"
    $psql = Resolve-ExecutablePath $psqlPath "psql.exe"
    Set-Location $repoRoot
    $gitStatus = (& $gitExe status --short) -join "`n"
    $gitCommit = (& $gitExe rev-parse HEAD).Trim()
    if ($gitStatus) { throw "Git status is not clean before REV868 helper execution." }
    Write-Host "Expected host: $HostName"
    Write-Host "Expected port: $Port"
    Write-Host "Expected database: $Database"
    Write-Host "PostgreSQL user parameter: $UserName"
    $securePassword = Read-Host -AsSecureString "Enter PostgreSQL password for REV868 development database only"
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    try { $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
    finally { if ($bstr -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) } }
    $env:PGPASSWORD = $plainPassword
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
    $identity = Invoke-PsqlRead $sql["Session identity"]
    if ($identity -notmatch "database=$Database") { throw "Connected database mismatch." }
    $present = Invoke-PsqlRead $sql["REV868 migration presence"]
    if ($present -match "present") { throw "REV868 migration is already applied. Stop for management review." }
    Add-Report "# REV868 Preflight Report"
    Add-Report "- Source commit: $gitCommit"
    Add-Report "- Identity: $identity"
    Add-Report "- REV868 migration presence: $present"
    if ($PreflightOnly) { Write-Host "REV868 preflight report: $reportFile"; return }
    $env:ConnectionStrings__NexaErp = "Host=$HostName;Port=$Port;Database=$Database;Username=$UserName;Password=$plainPassword"
    $env:NexaErp__ExpectedDatabase = $Database
    Set-Location $targetRoot
    & $dotnet ef database update $migrationName --project .\src\SESS.NexaERP.Infrastructure\SESS.NexaERP.Infrastructure.csproj --startup-project .\src\SESS.NexaERP.Api\SESS.NexaERP.Api.csproj --context NexaErpDbContext
    if ($LASTEXITCODE -ne 0) { throw "REV868 migration application failed." }
    Write-Host "REV868 migration applied. Generate post-run evidence separately."
}
finally {
    Remove-Item Env:\ConnectionStrings__NexaErp -ErrorAction SilentlyContinue
    Remove-Item Env:\NexaErp__ExpectedDatabase -ErrorAction SilentlyContinue
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    if ($plainPassword) { $plainPassword = $null }
    if ($securePassword) { $securePassword.Dispose() }
}


