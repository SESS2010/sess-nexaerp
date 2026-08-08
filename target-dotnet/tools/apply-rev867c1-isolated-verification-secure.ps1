[CmdletBinding()]
param(
    [string]$Database = "sess_nexaerp_rev867c1_verify",
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$UserName = "postgres",
    [string]$MigrationName = "20260808160435_Rev867C1Corrections",
    [string]$GitPath = "",
    [switch]$PreflightOnly,
    [switch]$GenerateSqlOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$targetRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$dotnetPath = Join-Path $targetRoot "..\.dotnet10\dotnet.exe"
$pgBin = "C:\Program Files\PostgreSQL\17\bin"
$psqlPath = Join-Path $pgBin "psql.exe"
$reportDir = Join-Path $targetRoot "local-evidence\rev867c1"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$reportFile = Join-Path $reportDir "rev867c1_isolated_verification_$timestamp.md"
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
function Resolve-RipgrepExecutable { $cmd = Get-Command rg.exe -ErrorAction SilentlyContinue; if ($cmd -and $cmd.Source) { return $cmd.Source }; return $null }
function Invoke-SecretScan([string]$Pattern, [string]$Root) { $rg = Resolve-RipgrepExecutable; if (-not $rg) { return "rg unavailable; manual secret scan required before approval" }; $scan = & $rg --pcre2 -n $Pattern $Root; $code = $LASTEXITCODE; if ($code -eq 0) { throw "Secret scan found prohibited patterns." }; if ($code -gt 1) { throw "Secret scanner failed with exit code $code." }; return "clean via rg.exe ($rg)" }
function Get-PreflightSql {
    $queries = [ordered]@{}
    $queries["Session identity"] = @"
select 'database=' || current_database()
union all select 'user=' || current_user
union all select 'server_addr=' || coalesce(inet_server_addr()::text, 'local_socket')
union all select 'server_port=' || inet_server_port()::text;
"@.Trim()
    $queries["Empty verification database check"] = @"
select case when
    current_database() = 'sess_nexaerp_rev867c1_verify'
    and not exists (select 1 from pg_catalog.pg_namespace where nspname = 'nexa')
    and not exists (
        select 1
        from pg_catalog.pg_class c
        join pg_catalog.pg_namespace n on n.oid = c.relnamespace
        where c.relname = '__EFMigrationsHistory'
    )
then 'empty_and_safe' else 'not_empty_or_wrong_target' end;
"@.Trim()
    $queries["Schemas"] = @"
select nspname
from pg_catalog.pg_namespace
where nspname not like 'pg_toast%'
order by nspname;
"@.Trim()
    $queries["EF history lookup"] = @"
select n.nspname || chr(31) || c.relname
from pg_catalog.pg_class c
join pg_catalog.pg_namespace n on n.oid = c.relnamespace
where c.relname = '__EFMigrationsHistory'
order by n.nspname, c.relname;
"@.Trim()
    return $queries
}
function Get-PostMigrationSql {
    $queries = [ordered]@{}
    $queries["Applied migration IDs"] = @"
select "MigrationId"
from "public"."__EFMigrationsHistory"
order by "MigrationId";
"@.Trim()
    $queries["REV867C1 migration present"] = @"
select case when exists (
    select 1
    from "public"."__EFMigrationsHistory"
    where "MigrationId" = '20260808160435_Rev867C1Corrections'
) then 'present' else 'absent' end;
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
    $queries["REV867C1 column evidence"] = @"
select table_name || '.' || column_name
from information_schema.columns
where table_schema = 'nexa'
  and (
      (table_name = 'items' and column_name in ('ItemCode','StandardEstimatedPrice','ApprovalStatus','xmin'))
      or (table_name = 'vendors' and column_name in ('VendorCode','BankMetadata','PortalOrganizationId','ApprovalStatus','xmin'))
      or (table_name = 'customers' and column_name in ('CustomerCode','CreditLimit','PortalOrganizationId','ApprovalStatus','xmin'))
      or (table_name = 'warehouses' and column_name in ('WarehouseCode','ApprovalStatus','xmin'))
      or (table_name = 'rack_bins' and column_name in ('RackBinCode','ApprovalStatus','xmin'))
      or (table_name in ('master_status_history','master_approval_history','audit_logs'))
  )
order by table_name, column_name;
"@.Trim()
    $queries["Persistent evidence counts"] = @"
select 'master_status_history=' || count(*) from nexa.master_status_history
union all select 'master_approval_history=' || count(*) from nexa.master_approval_history
union all select 'audit_logs=' || count(*) from nexa.audit_logs;
"@.Trim()
    return $queries
}
function Get-ProposedOperations {
    return @(
        "1. Verify Git status is clean and helper is running from committed source.",
        "2. Prompt for PostgreSQL password using Read-Host -AsSecureString.",
        "3. Connect with psql only to localhost:5432 / sess_nexaerp_rev867c1_verify as the supplied PostgreSQL user.",
        "4. Refuse immediately unless current_database() is exactly sess_nexaerp_rev867c1_verify.",
        "5. Refuse if the verification database already contains nexa schema or __EFMigrationsHistory.",
        "6. Set process-only ConnectionStrings__NexaErp to the verification database and NexaErp__ExpectedDatabase to sess_nexaerp_rev867c1_verify.",
        "7. Run dotnet ef database update 20260808160435_Rev867C1Corrections against the corrected design-time factory.",
        "8. Verify all six expected migration IDs in public.__EFMigrationsHistory.",
        "9. Verify nexa schema and REV867C1 master tables/columns.",
        "10. Run PostgreSQL-backed REV867C1 automated tests using REV867C1_POSTGRES scoped to the verification database.",
        "11. Record self-approval 403, persistent denial audit, status/approval history, masking, and organization-isolation evidence from tests and PostgreSQL counts.",
        "12. Run build, targeted tests, and secret scan; write sanitized report; clear sensitive environment variables in finally."
    )
}
function Test-SqlText([string]$Title, [string]$Sql) {
    if ([string]::IsNullOrWhiteSpace($Sql)) { throw "Diagnostic SQL '$Title' is empty." }
    if (-not $Sql.TrimEnd().EndsWith(';')) { throw "Diagnostic SQL '$Title' is missing a statement terminator." }
    $singleQuoteCount = ([regex]::Matches($Sql, "'")).Count
    if (($singleQuoteCount % 2) -ne 0) { throw "Diagnostic SQL '$Title' has unbalanced single quotes." }
}
function Invoke-PsqlRead([string]$Sql) {
    $sqlFile = Join-Path ([System.IO.Path]::GetTempPath()) ("sess_nexa_rev867c1_isolated_" + [Guid]::NewGuid().ToString("N") + ".sql")
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
function Add-SqlReport([System.Collections.Specialized.OrderedDictionary]$SqlMap, [string]$Heading) {
    Add-Report "## $Heading"
    foreach ($entry in $SqlMap.GetEnumerator()) {
        Add-Report "### $($entry.Key)"
        Add-Report '```sql'
        Add-Report ([string]$entry.Value)
        Add-Report '```'
        Add-Report ""
    }
}
function Write-SourceOnlyReport([System.Collections.Specialized.OrderedDictionary]$PreflightSql, [System.Collections.Specialized.OrderedDictionary]$PostSql) {
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
    Add-Report "# REV867C1 Isolated Verification Preflight Source Report"
    Add-Report ""
    Add-Report "- Time: $(Get-Date -Format o)"
    Add-Report "- Mode: GenerateSqlOnly"
    Add-Report "- Expected host: $HostName"
    Add-Report "- Expected port: $Port"
    Add-Report "- Expected database: $Database"
    Add-Report "- PostgreSQL user parameter: $UserName"
    Add-Report "- No password requested and no PostgreSQL connection attempted in this mode."
    Add-Report ""
    Add-Report "## Proposed Database-Changing Operation"
    Add-Report '```text'
    Add-Report "dotnet ef database update $MigrationName --project .\src\SESS.NexaERP.Infrastructure\SESS.NexaERP.Infrastructure.csproj --startup-project .\src\SESS.NexaERP.Api\SESS.NexaERP.Api.csproj --context NexaErpDbContext"
    Add-Report '```'
    Add-Report ""
    Add-Report "## Proposed Execution Plan"
    foreach ($operation in Get-ProposedOperations) { Add-Report "- $operation" }
    Add-Report ""
    Add-SqlReport $PreflightSql "Preflight Read-Only SQL"
    Add-SqlReport $PostSql "Post-Migration Read-Only SQL"
    Write-Host "REV867C1 isolated verification preflight source report: $reportFile"
}

try {
    Write-Section "REV867C1 isolated verification prechecks"
    Assert-SafePgIdentifier $Database "Verification database name"
    Assert-SafePgIdentifier $UserName "PostgreSQL user name"
    if ($Database -ne "sess_nexaerp_rev867c1_verify" -or $HostName -ne "localhost" -or $Port -ne 5432) { throw "This helper is permanently restricted to sess_nexaerp_rev867c1_verify on localhost:5432." }
    if ($MigrationName -ne "20260808160435_Rev867C1Corrections") { throw "Only REV867C1 migration target is allowed." }
    $preflightSql = Get-PreflightSql
    $postSql = Get-PostMigrationSql
    foreach ($entry in $preflightSql.GetEnumerator()) { Test-SqlText $entry.Key ([string]$entry.Value) }
    foreach ($entry in $postSql.GetEnumerator()) { Test-SqlText $entry.Key ([string]$entry.Value) }
    if ($GenerateSqlOnly) {
        Write-SourceOnlyReport $preflightSql $postSql
        foreach ($operation in Get-ProposedOperations) { Write-Output "-- $operation" }
        foreach ($entry in $preflightSql.GetEnumerator()) { Write-Output "-- Preflight: $($entry.Key)"; Write-Output ([string]$entry.Value); Write-Output "" }
        foreach ($entry in $postSql.GetEnumerator()) { Write-Output "-- Post: $($entry.Key)"; Write-Output ([string]$entry.Value); Write-Output "" }
        return
    }

    $gitExe = Resolve-GitExecutable $GitPath
    $dotnet = Resolve-ExecutablePath $dotnetPath ".NET executable"
    $psql = Resolve-ExecutablePath $psqlPath "psql.exe"
    Set-Location $repoRoot
    $gitStatus = (& $gitExe status --short) -join "`n"
    $gitCommit = (& $gitExe rev-parse HEAD).Trim()
    if ($gitStatus) { throw "Git status is not clean before REV867C1 isolated verification." }

    Write-Host "Expected host: $HostName"
    Write-Host "Expected port: $Port"
    Write-Host "Expected database: $Database"
    Write-Host "PostgreSQL user parameter: $UserName"
    Write-Section "REV867C1 isolated verification secure prompt"
    $securePassword = Read-Host -AsSecureString "Enter PostgreSQL password for isolated verification database only"
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    try { $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
    finally { if ($bstr -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) } }
    $env:PGPASSWORD = $plainPassword
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null

    Add-Evidence "Session identity" $preflightSql["Session identity"]
    if ($evidence["Session identity"] -notmatch "database=$Database") { throw "Connected database did not match isolated verification database." }
    if ($evidence["Session identity"] -match "database=sess_nexaerp(\r?\n|$)") { throw "Refusing to run against main development database sess_nexaerp." }
    Add-Evidence "Empty verification database check" $preflightSql["Empty verification database check"]
    if ($evidence["Empty verification database check"] -notmatch "empty_and_safe") { throw "Verification database is not empty or target identity is unsafe." }
    Add-Evidence "Schemas before migration" $preflightSql["Schemas"]
    Add-Evidence "EF history before migration" $preflightSql["EF history lookup"]

    if ($PreflightOnly) {
        Add-Report "# REV867C1 Isolated Verification Preflight Report"
        Add-Report ""
        Add-Report "- Source commit: $gitCommit"
        Add-Report "- Expected host: $HostName"
        Add-Report "- Expected port: $Port"
        Add-Report "- Expected database: $Database"
        Add-Report "- This preflight mode performed read-only checks only."
        Add-Report ""
        foreach ($entry in $evidence.GetEnumerator()) { Add-Report "## $($entry.Key)"; Add-Report '```text'; Add-Report ([string]$entry.Value); Add-Report '```' }
        Write-Host "REV867C1 isolated verification preflight report: $reportFile"
        return
    }

    $env:ConnectionStrings__NexaErp = "Host=$HostName;Port=$Port;Database=$Database;Username=$UserName;Password=$plainPassword"
    $env:NexaErp__ExpectedDatabase = $Database
    $env:REV867C1_POSTGRES = $env:ConnectionStrings__NexaErp

    Write-Section "Apply full migration chain to isolated verification database only"
    Set-Location $targetRoot
    & $dotnet ef database update $MigrationName --project .\src\SESS.NexaERP.Infrastructure\SESS.NexaERP.Infrastructure.csproj --startup-project .\src\SESS.NexaERP.Api\SESS.NexaERP.Api.csproj --context NexaErpDbContext
    if ($LASTEXITCODE -ne 0) { throw "EF database update failed with exit code $LASTEXITCODE." }

    foreach ($entry in $postSql.GetEnumerator()) { Add-Evidence $entry.Key ([string]$entry.Value) }
    foreach ($migrationId in $expectedMigrationIds) {
        if ($evidence["Applied migration IDs"] -notmatch [regex]::Escape($migrationId)) { throw "Expected migration ID missing after isolated verification migration: $migrationId" }
    }
    if ($evidence["REV867C1 migration present"] -notmatch "present") { throw "REV867C1 migration was not present after isolated verification migration." }

    Write-Section "Build, PostgreSQL-backed tests, and secret scan"
    $buildOutput = & $dotnet build .\SESS.NexaERP.slnx --configuration Release 2>&1
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed. $(($buildOutput | ForEach-Object { $_.ToString() }) -join "`n")" }
    $testOutput = & $dotnet test .\SESS.NexaERP.slnx --configuration Release --no-build --filter "Rev867C1PostgresVerificationTests|Rev867MasterFoundationTests" 2>&1
    if ($LASTEXITCODE -ne 0) { throw "dotnet test failed. $(($testOutput | ForEach-Object { $_.ToString() }) -join "`n")" }
    $scanWordPattern = 'pass' + 'word|pwd|secret|token'
    $scanPattern = '(?i)\b(' + $scanWordPattern + ')\b\s*[:=]\s*[''"`]?(?!\$|%|\{|<|REDACTED|redacted|your_|change_me|example|placeholder)[^''"`\s;]+'
    $secretScanOutput = Invoke-SecretScan $scanPattern $targetRoot

    Add-Report "# REV867C1 Isolated Verification Final Report"
    Add-Report ""
    Add-Report "- Source commit: $gitCommit"
    Add-Report "- Expected host: $HostName"
    Add-Report "- Expected port: $Port"
    Add-Report "- Expected database: $Database"
    Add-Report "- Migration target: $MigrationName"
    Add-Report "- Secret scan: $secretScanOutput"
    Add-Report "- OIDC provider/token testing remains pending."
    Add-Report ""
    foreach ($entry in $evidence.GetEnumerator()) { Add-Report "## $($entry.Key)"; Add-Report '```text'; Add-Report ([string]$entry.Value); Add-Report '```' }
    Add-Report "## Test output"
    Add-Report '```text'
    Add-Report (($testOutput | Select-Object -Last 60) -join "`n")
    Add-Report '```'
    Write-Host "REV867C1 isolated verification final report: $reportFile"
}
catch { Write-Host $_.Exception.Message; throw }
finally {
    Remove-Item Env:\ConnectionStrings__NexaErp -ErrorAction SilentlyContinue
    Remove-Item Env:\NexaErp__ExpectedDatabase -ErrorAction SilentlyContinue
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:\REV867C1_POSTGRES -ErrorAction SilentlyContinue
    if ($plainPassword) { $plainPassword = $null }
    if ($securePassword) { $securePassword.Dispose() }
}
