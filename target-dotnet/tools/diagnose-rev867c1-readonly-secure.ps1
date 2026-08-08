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
$pgBin = "C:\Program Files\PostgreSQL\17\bin"
$psqlPath = Join-Path $pgBin "psql.exe"
$reportDir = Join-Path $targetRoot "local-evidence\rev867c1"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$reportFile = Join-Path $reportDir "rev867c1_readonly_diagnostic_$timestamp.md"
$securePassword = $null
$plainPassword = $null
$evidence = [ordered]@{}
$diagnosticSql = $null

function Write-Section([string]$Text) { Write-Host ""; Write-Host "== $Text ==" }
function Add-Report([string]$Text) { Add-Content -LiteralPath $reportFile -Value $Text -Encoding utf8 }
function Assert-SafePgIdentifier([string]$Name, [string]$Label) { if ($Name -notmatch '^[A-Za-z_][A-Za-z0-9_]{0,62}$') { throw "$Label is not a safe PostgreSQL identifier." } }
function Resolve-ExecutablePath([string]$Path, [string]$Label) { $r = Resolve-Path -LiteralPath $Path -ErrorAction Stop; $i = Get-Item -LiteralPath $r.Path -ErrorAction Stop; if (-not $i.Exists) { throw "$Label was not found: $Path" }; return $i.FullName }
function Resolve-GitExecutable([string]$ExplicitGitPath) {
    $candidates = New-Object System.Collections.Generic.List[string]
    if (-not [string]::IsNullOrWhiteSpace($ExplicitGitPath)) { $candidates.Add($ExplicitGitPath) }
    $cmd = Get-Command git.exe -ErrorAction SilentlyContinue; if ($cmd -and $cmd.Source) { $candidates.Add($cmd.Source) }
    $candidates.Add("C:\Program Files\Git\cmd\git.exe"); $candidates.Add("C:\Program Files\Git\bin\git.exe"); $candidates.Add("D:\Git\cmd\git.exe"); $candidates.Add("D:\Git\bin\git.exe"); $candidates.Add("D:\PortableGit\cmd\git.exe"); $candidates.Add("D:\PortableGit\bin\git.exe"); $candidates.Add("C:\Users\User\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\git\cmd\git.exe")
    foreach ($candidate in $candidates | Select-Object -Unique) { if (Test-Path -LiteralPath $candidate -PathType Leaf) { $item = Get-Item -LiteralPath $candidate; if ($item.Name -ne "git.exe") { throw "Resolved Git path is not git.exe: $($item.FullName)" }; $v = & $item.FullName --version; if ($LASTEXITCODE -eq 0 -and ($v -join "`n") -match '^git version ') { return $item.FullName } } }
    throw "git.exe was not found. Pass -GitPath with a valid git.exe path."
}
function Get-DiagnosticSql {
    $queries = [ordered]@{}
    $queries["Session identity"] = @"
select 'database=' || current_database()
union all select 'user=' || current_user
union all select 'server_addr=' || coalesce(inet_server_addr()::text, 'local_socket')
union all select 'server_port=' || inet_server_port()::text;
"@.Trim()
    $queries["Schemas"] = @"
select nspname
from pg_catalog.pg_namespace
where nspname not like 'pg_toast%'
order by nspname;
"@.Trim()
    $queries["Migration/history relations"] = @"
select n.nspname || '.' || c.relname || '|relkind=' || c.relkind::text
from pg_catalog.pg_class c
join pg_catalog.pg_namespace n on n.oid = c.relnamespace
where c.relname ilike '%migration%' or c.relname ilike '%history%'
order by n.nspname, c.relname;
"@.Trim()
    $queries["Exact EF history lookup"] = @"
select n.nspname || chr(31) || c.relname
from pg_catalog.pg_class c
join pg_catalog.pg_namespace n on n.oid = c.relnamespace
where c.relname = '__EFMigrationsHistory'
order by n.nspname, c.relname;
"@.Trim()
    $queries["Public EF history exists"] = @"
select case when exists (
    select 1
    from pg_catalog.pg_class c
    join pg_catalog.pg_namespace n on n.oid = c.relnamespace
    where n.nspname = 'public' and c.relname = '__EFMigrationsHistory'
) then 'found' else 'not_found' end;
"@.Trim()
    $queries["Case-sensitive MigrationId rows"] = @"
select `"MigrationId`"
from `"public`".`"__EFMigrationsHistory`"
order by `"MigrationId`";
"@.Trim()
    return $queries
}
function Test-SqlText([string]$Title, [string]$Sql) {
    if ([string]::IsNullOrWhiteSpace($Sql)) { throw "Diagnostic SQL '$Title' is empty." }
    if (-not $Sql.TrimEnd().EndsWith(';')) { throw "Diagnostic SQL '$Title' is missing a statement terminator." }
    $singleQuoteCount = ([regex]::Matches($Sql, "'")).Count
    if (($singleQuoteCount % 2) -ne 0) { throw "Diagnostic SQL '$Title' has unbalanced single quotes." }
    if ($Sql -match 'to_regclass') { throw "Diagnostic SQL '$Title' must not use to_regclass." }
}
function Write-SqlOnlyReport([System.Collections.Specialized.OrderedDictionary]$SqlMap) {
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
    Add-Report "# REV867C1 Read-Only Diagnostic SQL Source Verification"
    Add-Report ""
    Add-Report "- Time: $(Get-Date -Format o)"
    Add-Report "- Mode: GenerateSqlOnly"
    Add-Report "- Database parameter: $Database"
    Add-Report "- This mode does not request a password and does not connect to PostgreSQL."
    Add-Report "- This helper uses read-only SELECT probes only."
    Add-Report ""
    foreach ($entry in $SqlMap.GetEnumerator()) {
        Add-Report "## $($entry.Key)"
        Add-Report '```sql'
        Add-Report ([string]$entry.Value)
        Add-Report '```'
        Add-Report ""
    }
    Write-Host "REV867C1 generated diagnostic SQL report: $reportFile"
}
function Write-SqlToStdout([System.Collections.Specialized.OrderedDictionary]$SqlMap) {
    foreach ($entry in $SqlMap.GetEnumerator()) {
        Write-Output "-- $($entry.Key)"
        Write-Output ([string]$entry.Value)
        Write-Output ""
    }
}
function Invoke-PsqlRead([string]$Sql) {
    $sqlFile = Join-Path ([System.IO.Path]::GetTempPath()) ("sess_nexa_rev867c1_diag_" + [Guid]::NewGuid().ToString("N") + ".sql")
    try {
        [System.IO.File]::WriteAllText($sqlFile, $Sql, [System.Text.UTF8Encoding]::new($false))
        $old = $ErrorActionPreference
        $ErrorActionPreference = "Continue"
        try { $output = & $psql -h $HostName -p $Port -U $UserName -d $Database -v ON_ERROR_STOP=1 -At -f $sqlFile 2>&1; $exit = $LASTEXITCODE }
        finally { $ErrorActionPreference = $old }
        if ($exit -ne 0) { throw "psql failed with exit code $exit. $(($output | ForEach-Object { $_.ToString() }) -join "`n")" }
        return ($output -join "`n")
    }
    finally { Remove-Item -LiteralPath $sqlFile -Force -ErrorAction SilentlyContinue }
}
function Add-Evidence([string]$Title, [string]$Sql) { $evidence[$Title] = Invoke-PsqlRead $Sql }
function Write-FailureReport([string]$Message) {
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
    Add-Report "# REV867C1 Read-Only Diagnostic Failed"
    Add-Report ""
    Add-Report "- Time: $(Get-Date -Format o)"
    Add-Report "- Error: $Message"
    Add-Report "- Host: $HostName"
    Add-Report "- Port: $Port"
    Add-Report "- Database parameter: $Database"
    Add-Report "- PostgreSQL user parameter: $UserName"
    Add-Report "- Sensitive values are not written by this report."
    if ($diagnosticSql) {
        Add-Report ""
        Add-Report "## Generated SQL"
        foreach ($entry in $diagnosticSql.GetEnumerator()) {
            Add-Report "### $($entry.Key)"
            Add-Report '```sql'
            Add-Report ([string]$entry.Value)
            Add-Report '```'
        }
    }
    Write-Host "REV867C1 read-only diagnostic failed. Sanitized report: $reportFile"
}

try {
    Write-Section "REV867C1 read-only diagnostic prechecks"
    Assert-SafePgIdentifier $Database "Verification database name"
    Assert-SafePgIdentifier $UserName "PostgreSQL user name"
    if ($Database -ne "sess_nexaerp_rev867c1_verify" -or $HostName -ne "localhost" -or $Port -ne 5432) { throw "This diagnostic is restricted to sess_nexaerp_rev867c1_verify on localhost:5432." }
    $diagnosticSql = Get-DiagnosticSql
    foreach ($entry in $diagnosticSql.GetEnumerator()) { Test-SqlText $entry.Key ([string]$entry.Value) }
    if ($GenerateSqlOnly) {
        Write-SqlOnlyReport $diagnosticSql
        Write-SqlToStdout $diagnosticSql
        return
    }

    $gitExe = Resolve-GitExecutable $GitPath
    $psql = Resolve-ExecutablePath $psqlPath "psql.exe"
    Set-Location $repoRoot
    $gitStatus = (& $gitExe status --short) -join "`n"
    $gitCommit = (& $gitExe rev-parse HEAD).Trim()
    if ($gitStatus) { throw "Git status is not clean before REV867C1 read-only diagnostic." }

    Write-Section "REV867C1 read-only PostgreSQL diagnostic"
    $securePassword = Read-Host -AsSecureString "Enter PostgreSQL password for local verification database only"
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    try { $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
    finally { if ($bstr -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) } }
    $env:PGPASSWORD = $plainPassword
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null

    Add-Evidence "Session identity" $diagnosticSql["Session identity"]
    if ($evidence["Session identity"] -notmatch "database=$Database") { throw "Connected database did not match expected verification database." }
    Add-Evidence "Schemas" $diagnosticSql["Schemas"]
    Add-Evidence "Migration/history relations" $diagnosticSql["Migration/history relations"]
    Add-Evidence "Exact EF history lookup" $diagnosticSql["Exact EF history lookup"]
    Add-Evidence "Public EF history exists" $diagnosticSql["Public EF history exists"]
    if ($evidence["Public EF history exists"] -match "not_found") {
        $evidence["Case-sensitive MigrationId rows"] = "skipped: public.__EFMigrationsHistory not found in this connected database"
    } else {
        Add-Evidence "Case-sensitive MigrationId rows" $diagnosticSql["Case-sensitive MigrationId rows"]
    }

    Add-Report "# REV867C1 Read-Only Diagnostic Report"
    Add-Report ""
    Add-Report "- Source commit: $gitCommit"
    Add-Report "- Host parameter: $HostName"
    Add-Report "- Port parameter: $Port"
    Add-Report "- Database parameter: $Database"
    Add-Report "- PostgreSQL user parameter: $UserName"
    Add-Report "- This helper uses read-only psql probes only and does not change database state."
    Add-Report "- Sensitive values are not written by this report."
    Add-Report ""
    Add-Report "## Generated SQL"
    foreach ($entry in $diagnosticSql.GetEnumerator()) {
        Add-Report "### $($entry.Key)"
        Add-Report '```sql'
        Add-Report ([string]$entry.Value)
        Add-Report '```'
        Add-Report ""
    }
    foreach ($entry in $evidence.GetEnumerator()) {
        Add-Report "## $($entry.Key)"
        Add-Report '```text'
        Add-Report ([string]$entry.Value)
        Add-Report '```'
        Add-Report ""
    }
    Write-Host "REV867C1 read-only diagnostic report: $reportFile"
}
catch { Write-FailureReport $_.Exception.Message; Write-Host $_.Exception.Message; throw }
finally {
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    if ($plainPassword) { $plainPassword = $null }
    if ($securePassword) { $securePassword.Dispose() }
}
