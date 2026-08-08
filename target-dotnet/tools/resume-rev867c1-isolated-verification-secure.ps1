[CmdletBinding()]
param(
    [string]$Database = "sess_nexaerp_rev867c1_verify",
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$UserName = "postgres",
    [string]$GitPath = "",
    [switch]$GenerateSqlOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$targetRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$dotnetPath = Join-Path $targetRoot "..\.dotnet10\dotnet.exe"
$psqlPath = "C:\Program Files\PostgreSQL\17\bin\psql.exe"
$reportDir = Join-Path $targetRoot "local-evidence\rev867c1"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$reportFile = Join-Path $reportDir "rev867c1_isolated_resume_$timestamp.md"
$expectedMigrationIds = @(
    "20260808110924_Phase1Foundation",
    "20260808114550_Phase1AuthorizationSeed",
    "20260808123411_Rev866EmployeePermissionMatrix",
    "20260808142353_Rev866CorrectiveStatusPermissionAudit",
    "20260808151207_Rev867MasterFoundation",
    "20260808160435_Rev867C1Corrections"
)
$securePassword = $null
$plainPassword = $null
$testOutput = @()
$buildOutput = @()
$secretScanOutput = "not run"
$evidence = [ordered]@{}

function Write-Section([string]$Text) { Write-Host ""; Write-Host "== $Text ==" }
function Add-Report([string]$Text) { Add-Content -LiteralPath $reportFile -Value $Text -Encoding utf8 }
function Assert-SafePgIdentifier([string]$Name, [string]$Label) { if ($Name -notmatch '^[A-Za-z_][A-Za-z0-9_]{0,62}$') { throw "$Label is not a safe PostgreSQL identifier." } }
function Resolve-ExecutablePath([string]$Path, [string]$Label) { $resolved = Resolve-Path -LiteralPath $Path -ErrorAction Stop; $item = Get-Item -LiteralPath $resolved.Path -ErrorAction Stop; if (-not $item.Exists) { throw "$Label was not found: $Path" }; return $item.FullName }
function Resolve-GitExecutable([string]$ExplicitGitPath) {
    $candidates = New-Object System.Collections.Generic.List[string]
    if (-not [string]::IsNullOrWhiteSpace($ExplicitGitPath)) { $candidates.Add($ExplicitGitPath) }
    $cmd = Get-Command git.exe -ErrorAction SilentlyContinue; if ($cmd -and $cmd.Source) { $candidates.Add($cmd.Source) }
    $candidates.Add("C:\Program Files\Git\cmd\git.exe"); $candidates.Add("C:\Program Files\Git\bin\git.exe"); $candidates.Add("C:\Users\User\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\git\cmd\git.exe")
    foreach ($candidate in $candidates | Select-Object -Unique) { if (Test-Path -LiteralPath $candidate -PathType Leaf) { $item = Get-Item -LiteralPath $candidate; if ($item.Name -ne "git.exe") { throw "Resolved Git path is not git.exe: $($item.FullName)" }; $version = & $item.FullName --version; if ($LASTEXITCODE -eq 0 -and ($version -join "`n") -match '^git version ') { return $item.FullName } } }
    throw "git.exe was not found. Pass -GitPath with a valid git.exe path."
}
function Resolve-RipgrepExecutable { $cmd = Get-Command rg.exe -ErrorAction SilentlyContinue; if ($cmd -and $cmd.Source) { return $cmd.Source }; return $null }
function Invoke-SecretScan([string]$Pattern, [string]$Root) { $rg = Resolve-RipgrepExecutable; if (-not $rg) { return "rg unavailable; manual secret scan required before approval" }; $scan = & $rg --pcre2 -n $Pattern $Root; $code = $LASTEXITCODE; if ($code -eq 0) { throw "Secret scan found prohibited patterns." }; if ($code -gt 1) { throw "Secret scanner failed with exit code $code." }; return "clean via rg.exe ($rg)" }
function Get-ResumeSql {
    $queries = [ordered]@{}
    $queries["Session identity"] = @"
select 'database=' || current_database()
union all select 'user=' || current_user
union all select 'server_addr=' || coalesce(inet_server_addr()::text, 'local_socket')
union all select 'server_port=' || inet_server_port()::text;
"@.Trim()
    $queries["Applied migration IDs"] = @"
select ""MigrationId""
from ""public"".""__EFMigrationsHistory""
order by ""MigrationId"";
"@.Trim()
    $queries["Nexa schema present"] = @"
select case when exists (select 1 from pg_catalog.pg_namespace where nspname = 'nexa') then 'present' else 'absent' end;
"@.Trim()
    $queries["REV867C1 table evidence"] = @"
select table_name
from information_schema.tables
where table_schema = 'nexa'
  and table_name in ('items','vendors','customers','warehouses','rack_bins','master_status_history','master_approval_history','audit_logs')
order by table_name;
"@.Trim()
    $queries["Persistent evidence counts before tests"] = @"
select 'master_status_history=' || count(*) from nexa.master_status_history
union all select 'master_approval_history=' || count(*) from nexa.master_approval_history
union all select 'audit_logs=' || count(*) from nexa.audit_logs;
"@.Trim()
    return $queries
}
function Test-SqlText([string]$Title, [string]$Sql) {
    if ([string]::IsNullOrWhiteSpace($Sql)) { throw "Resume SQL '$Title' is empty." }
    if (-not $Sql.TrimEnd().EndsWith(';')) { throw "Resume SQL '$Title' is missing a statement terminator." }
    if ((([regex]::Matches($Sql, "'")).Count % 2) -ne 0) { throw "Resume SQL '$Title' has unbalanced single quotes." }
}
function Invoke-PsqlRead([string]$Sql) {
    $sqlFile = Join-Path ([System.IO.Path]::GetTempPath()) ("sess_nexa_rev867c1_resume_" + [Guid]::NewGuid().ToString("N") + ".sql")
    try {
        [System.IO.File]::WriteAllText($sqlFile, $Sql, [System.Text.UTF8Encoding]::new($false))
        $old = $ErrorActionPreference
        $ErrorActionPreference = "Continue"
        try { $output = & $psql -h $HostName -p $Port -U $UserName -d $Database -v ON_ERROR_STOP=1 -At -f $sqlFile 2>&1; $exit = $LASTEXITCODE }
        finally { $ErrorActionPreference = $old }
        if ($exit -ne 0) { throw "psql failed with exit code $exit. $((($output | ForEach-Object { $_.ToString() }) -join "`n"))" }
        return ($output -join "`n")
    }
    finally { Remove-Item -LiteralPath $sqlFile -Force -ErrorAction SilentlyContinue }
}
function Add-Evidence([string]$Title, [string]$Sql) { $evidence[$Title] = Invoke-PsqlRead $Sql }
function Write-FailureReport([string]$Message) {
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
    Add-Report "# REV867C1 Isolated Resume Failed"
    Add-Report ""
    Add-Report "- Error: $Message"
    Add-Report "- Expected database: $Database"
    Add-Report "- Build lines captured: $($buildOutput.Count)"
    Add-Report "- Test lines captured: $($testOutput.Count)"
    foreach ($entry in $evidence.GetEnumerator()) { Add-Report "## $($entry.Key)"; Add-Report '```text'; Add-Report ([string]$entry.Value); Add-Report '```' }
    if ($testOutput.Count -gt 0) { Add-Report "## Test stdout/stderr"; Add-Report '```text'; Add-Report (($testOutput | Select-Object -Last 200) -join "`n"); Add-Report '```' }
}
function Write-SuccessReport([string]$GitCommit) {
    Add-Report "# REV867C1 Isolated Resume Verification Report"
    Add-Report ""
    Add-Report "- Source commit: $GitCommit"
    Add-Report "- Expected host: $HostName"
    Add-Report "- Expected port: $Port"
    Add-Report "- Expected database: $Database"
    Add-Report "- Secret scan: $secretScanOutput"
    Add-Report "- OIDC provider/token testing remains pending."
    foreach ($entry in $evidence.GetEnumerator()) { Add-Report "## $($entry.Key)"; Add-Report '```text'; Add-Report ([string]$entry.Value); Add-Report '```' }
    Add-Report "## Test stdout/stderr"
    Add-Report '```text'
    Add-Report (($testOutput | Select-Object -Last 200) -join "`n")
    Add-Report '```'
}

try {
    Write-Section "REV867C1 isolated resume no-secret prechecks"
    Assert-SafePgIdentifier $Database "Verification database name"
    Assert-SafePgIdentifier $UserName "PostgreSQL user name"
    if ($Database -ne "sess_nexaerp_rev867c1_verify" -or $HostName -ne "localhost" -or $Port -ne 5432) { throw "This resume helper is permanently restricted to sess_nexaerp_rev867c1_verify on localhost:5432." }
    $resumeSql = Get-ResumeSql
    foreach ($entry in $resumeSql.GetEnumerator()) { Test-SqlText $entry.Key ([string]$entry.Value) }
    if ($GenerateSqlOnly) {
        foreach ($entry in $resumeSql.GetEnumerator()) { Write-Output "-- $($entry.Key)"; Write-Output ([string]$entry.Value); Write-Output "" }
        return
    }

    $gitExe = Resolve-GitExecutable $GitPath
    $dotnet = Resolve-ExecutablePath $dotnetPath ".NET executable"
    $psql = Resolve-ExecutablePath $psqlPath "psql.exe"
    Set-Location $repoRoot
    $gitStatus = (& $gitExe status --short) -join "`n"
    $gitCommit = (& $gitExe rev-parse HEAD).Trim()
    if ($gitStatus) { throw "Git status is not clean before REV867C1 isolated resume verification." }

    Write-Host "Expected host: $HostName"
    Write-Host "Expected port: $Port"
    Write-Host "Expected database: $Database"
    Write-Host "PostgreSQL user parameter: $UserName"
    Write-Section "REV867C1 isolated resume secure prompt"
    $securePassword = Read-Host -AsSecureString "Enter PostgreSQL password for isolated verification database only"
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    try { $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
    finally { if ($bstr -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) } }
    $env:PGPASSWORD = $plainPassword
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null

    Add-Evidence "Session identity" $resumeSql["Session identity"]
    if ($evidence["Session identity"] -notmatch "database=$Database") { throw "Connected database did not match isolated verification database." }
    if ($evidence["Session identity"] -match "database=sess_nexaerp(\r?\n|$)") { throw "Refusing to run against main development database sess_nexaerp." }
    foreach ($entry in $resumeSql.GetEnumerator()) { if ($entry.Key -ne "Session identity") { Add-Evidence $entry.Key ([string]$entry.Value) } }
    foreach ($migrationId in $expectedMigrationIds) { if ($evidence["Applied migration IDs"] -notmatch [regex]::Escape($migrationId)) { throw "Expected migration ID missing before resume verification: $migrationId" } }
    if ($evidence["Nexa schema present"] -notmatch "present") { throw "nexa schema missing before resume verification." }

    $env:ConnectionStrings__NexaErp = "Host=$HostName;Port=$Port;Database=$Database;Username=$UserName;Password=$plainPassword"
    $env:NexaErp__ExpectedDatabase = $Database
    $env:REV867C1_POSTGRES = $env:ConnectionStrings__NexaErp

    Write-Section "Build, PostgreSQL-backed tests, and secret scan"
    Set-Location $targetRoot
    $buildOutput = & $dotnet build .\SESS.NexaERP.slnx --configuration Release 2>&1
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed." }
    $testOutput = & $dotnet test .\SESS.NexaERP.slnx --configuration Release --no-build --filter "Rev867C1PostgresVerificationTests|Rev867MasterFoundationTests" 2>&1
    if ($LASTEXITCODE -ne 0) { throw "dotnet test failed." }
    $scanWordPattern = 'pass' + 'word|pwd|secret|token'
    $scanPattern = '(?i)\b(' + $scanWordPattern + ')\b\s*[:=]\s*[''"`]?(?!\$|%|\{|<|REDACTED|redacted|your_|change_me|example|placeholder)[^''"`\s;]+'
    $secretScanOutput = Invoke-SecretScan $scanPattern $targetRoot
    Add-Evidence "Persistent evidence counts after tests" $resumeSql["Persistent evidence counts before tests"]
    Write-SuccessReport $gitCommit
    Write-Host "REV867C1 isolated resume verification report: $reportFile"
}
catch {
    Write-Host $_.Exception.Message
    Write-FailureReport $_.Exception.Message
    Write-Host "REV867C1 isolated resume verification failed. Sanitized report: $reportFile"
    throw
}
finally {
    Remove-Item Env:\ConnectionStrings__NexaErp -ErrorAction SilentlyContinue
    Remove-Item Env:\NexaErp__ExpectedDatabase -ErrorAction SilentlyContinue
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:\REV867C1_POSTGRES -ErrorAction SilentlyContinue
    if ($plainPassword) { $plainPassword = $null }
    if ($securePassword) { $securePassword.Dispose() }
}
