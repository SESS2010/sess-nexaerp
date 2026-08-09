[CmdletBinding()]
param(
    [string]$Database = "sess_nexaerp_rev868_verify",
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$UserName = "postgres",
    [string]$MigrationName = "20260808190920_Rev868PurchaseLocationAllocationCorrection",
    [string]$GitPath = "",
    [switch]$PreflightOnly,
    [switch]$GeneratePlanOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$targetRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$dotnetPath = Join-Path $targetRoot "..\.dotnet10\dotnet.exe"
$pgBin = "C:\Program Files\PostgreSQL\17\bin"
$psqlPath = Join-Path $pgBin "psql.exe"
$reportDir = Join-Path $targetRoot "local-evidence\rev868c1"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$reportFile = Join-Path $reportDir "rev868c1_isolated_workflow_verification_$timestamp.md"
$expectedMigrationIds = @(
    "20260808110924_Phase1Foundation",
    "20260808114550_Phase1AuthorizationSeed",
    "20260808123411_Rev866EmployeePermissionMatrix",
    "20260808142353_Rev866CorrectiveStatusPermissionAudit",
    "20260808151207_Rev867MasterFoundation",
    "20260808160435_Rev867C1Corrections",
    "20260808182945_Rev868PurchaseRequisitionFoundation",
    "20260808190920_Rev868PurchaseLocationAllocationCorrection"
)
$blockedDatabaseNames = @("sess_nexaerp", "postgres", "template0", "template1", "rev861", "sess_rev861", "sess_nexaerp_rev861")
$securePassword = $null
$plainPassword = $null
$evidence = [ordered]@{}

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
function Invoke-SecretScan([string]$Pattern, [string]$Root) { $rg = Resolve-RipgrepExecutable; if (-not $rg) { return "rg unavailable; use committed fallback scan evidence before approval" }; $scan = & $rg --pcre2 -n $Pattern $Root; $code = $LASTEXITCODE; if ($code -eq 0) { throw "Secret scan found prohibited patterns." }; if ($code -gt 1) { throw "Secret scanner failed with exit code $code." }; return "clean via rg.exe ($rg)" }
function Get-PreflightSql {
    [ordered]@{
        "Session identity" = @"
select 'database=' || current_database()
union all select 'user=' || current_user
union all select 'server_addr=' || coalesce(inet_server_addr()::text, 'local_socket')
union all select 'server_port=' || inet_server_port()::text;
"@.Trim()
        "Unsafe target rejection evidence" = @"
select case when current_database() in ('sess_nexaerp','postgres','template0','template1','rev861','sess_rev861','sess_nexaerp_rev861')
then 'blocked_target' else 'not_blocked_name' end;
"@.Trim()
        "Existing schemas" = @"
select nspname
from pg_catalog.pg_namespace
where nspname not like 'pg_toast%'
order by nspname;
"@.Trim()
        "EF history relation lookup" = @"
select n.nspname || '.' || c.relname
from pg_catalog.pg_class c
join pg_catalog.pg_namespace n on n.oid = c.relnamespace
where c.relname = '__EFMigrationsHistory'
order by n.nspname, c.relname;
"@.Trim()
        "Existing migration IDs" = @"
select "MigrationId"
from "public"."__EFMigrationsHistory"
order by "MigrationId";
"@.Trim()
    }
}
function Get-PostVerificationSql {
    [ordered]@{
        "Eight migration IDs" = @"
select "MigrationId", count(*)
from "public"."__EFMigrationsHistory"
where "MigrationId" in (
 '20260808110924_Phase1Foundation',
 '20260808114550_Phase1AuthorizationSeed',
 '20260808123411_Rev866EmployeePermissionMatrix',
 '20260808142353_Rev866CorrectiveStatusPermissionAudit',
 '20260808151207_Rev867MasterFoundation',
 '20260808160435_Rev867C1Corrections',
 '20260808182945_Rev868PurchaseRequisitionFoundation',
 '20260808190920_Rev868PurchaseLocationAllocationCorrection')
group by "MigrationId"
order by "MigrationId";
"@.Trim()
        "Workflow record counts" = @"
select 'purchase_requisitions=' || count(*) from nexa.purchase_requisitions
union all select 'purchase_requisition_lines=' || count(*) from nexa.purchase_requisition_lines
union all select 'stock_availability_checks=' || count(*) from nexa.stock_availability_checks
union all select 'stock_availability_check_lines=' || count(*) from nexa.stock_availability_check_lines
union all select 'stock_reservations=' || count(*) from nexa.stock_reservations
union all select 'active_reservations=' || count(*) from nexa.stock_reservations where "Status" = 'Active'
union all select 'purchase_requirement_handoffs=' || count(*) from nexa.purchase_requirement_handoffs
union all select 'pending_rfq_handoffs=' || count(*) from nexa.purchase_requirement_handoffs where "Status" = 'PendingRFQ'
union all select 'purchase_requisition_status_history=' || count(*) from nexa.purchase_requisition_status_history
union all select 'purchase_requisition_approval_history=' || count(*) from nexa.purchase_requisition_approval_history
union all select 'stock_reservation_history=' || count(*) from nexa.stock_reservation_history
union all select 'audit_logs=' || count(*) from nexa.audit_logs;
"@.Trim()
        "Quantity reconciliation evidence" = @"
select count(*)
from nexa.purchase_requisition_lines
where "RequestedQuantity" <= 0
   or "ReservedQuantity" < 0
   or "ShortageQuantity" < 0
   or "ProcurementHandoffQuantity" < 0
   or "ReservedQuantity" > "RequestedQuantity"
   or "ShortageQuantity" <> greatest("RequestedQuantity" - "ReservedQuantity", 0)
   or "ProcurementHandoffQuantity" <> "ShortageQuantity";
"@.Trim()
        "Duplicate active reservation evidence" = @"
select count(*)
from (
    select "PurchaseRequisitionLineId", "LocationKey", count(*)
    from nexa.stock_reservations
    where "Status" = 'Active'
    group by "PurchaseRequisitionLineId", "LocationKey"
    having count(*) > 1
) d;
"@.Trim()
        "Duplicate PendingRFQ handoff evidence" = @"
select count(*)
from (
    select "PurchaseRequisitionLineId", count(*)
    from nexa.purchase_requirement_handoffs
    where "Status" = 'PendingRFQ'
    group by "PurchaseRequisitionLineId"
    having count(*) > 1
) d;
"@.Trim()
        "Location persistence evidence" = @"
select count(*)
from nexa.stock_reservations
where "WarehouseId" is null or "LocationKey" is null or length("LocationKey") = 0
union all select count(*)
from nexa.stock_availability_check_lines
where "WarehouseId" is null or "LocationKey" is null or length("LocationKey") = 0
union all select count(*)
from nexa.purchase_requirement_handoffs
where "WarehouseId" is null or "LocationKey" is null or length("LocationKey") = 0;
"@.Trim()
    }
}
function Get-ProposedOperations {
    @(
        "1. Confirm committed source and clean Git state before any database action.",
        "2. Connect only to localhost:5432 / sess_nexaerp_rev868_verify using psql after secure password prompt.",
        "3. Reject sess_nexaerp, postgres, template0, template1 and REV861 database names before any EF command.",
        "4. Set process-only ConnectionStrings__NexaErp and NexaErp__ExpectedDatabase=sess_nexaerp_rev868_verify.",
        "5. Apply full EF migration chain through 20260808190920_Rev868PurchaseLocationAllocationCorrection to the isolated verification database only.",
        "6. Run PostgreSQL-backed REV868C1 workflow tests using REV868C1_POSTGRES scoped to the isolated database.",
        "7. Verify PR lifecycle, approval routing, stock reconciliation, security denial audit, uniqueness, and persistent evidence counts.",
        "8. Run build, targeted tests and secret scan; write a sanitized report; clear sensitive environment variables in finally."
    )
}
function Assert-SqlSafe([string]$Title, [string]$Sql) {
    if ([string]::IsNullOrWhiteSpace($Sql)) { throw "SQL '$Title' is empty." }
    if (-not $Sql.TrimEnd().EndsWith(';')) { throw "SQL '$Title' is missing a statement terminator." }
    $singleQuoteCount = ([regex]::Matches($Sql, "'")).Count
    if (($singleQuoteCount % 2) -ne 0) { throw "SQL '$Title' has unbalanced single quotes." }
    if ($Sql -match '(?i)\b(insert|update|delete|drop|create|alter|truncate|restore|copy|grant|revoke)\b') { throw "SQL '$Title' is not read-only." }
}
function Invoke-PsqlRead([string]$Sql) {
    $sqlFile = Join-Path ([System.IO.Path]::GetTempPath()) ("sess_nexa_rev868c1_" + [Guid]::NewGuid().ToString("N") + ".sql")
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
function Write-PlanReport([System.Collections.Specialized.OrderedDictionary]$PreflightSql, [System.Collections.Specialized.OrderedDictionary]$PostSql) {
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
    Add-Report "# REV868C1 Isolated Workflow Verification Plan"
    Add-Report ""
    Add-Report "- Time: $(Get-Date -Format o)"
    Add-Report "- Mode: GeneratePlanOnly"
    Add-Report "- Expected host: $HostName"
    Add-Report "- Expected port: $Port"
    Add-Report "- Expected database: $Database"
    Add-Report "- PostgreSQL user parameter: $UserName"
    Add-Report "- No password requested and no PostgreSQL connection attempted in this mode."
    Add-Report ""
    Add-Report "## Proposed Operations"
    foreach ($operation in Get-ProposedOperations) { Add-Report "- $operation" }
    Add-Report ""
    Add-Report "## Preflight Read-Only SQL"
    foreach ($entry in $PreflightSql.GetEnumerator()) { Add-Report "### $($entry.Key)"; Add-Report '```sql'; Add-Report ([string]$entry.Value); Add-Report '```' }
    Add-Report "## Post-Test Read-Only SQL"
    foreach ($entry in $PostSql.GetEnumerator()) { Add-Report "### $($entry.Key)"; Add-Report '```sql'; Add-Report ([string]$entry.Value); Add-Report '```' }
    Write-Host "REV868C1 isolated workflow verification plan report: $reportFile"
}

try {
    Write-Section "REV868C1 no-secret prechecks"
    Assert-SafePgIdentifier $Database "Verification database name"
    Assert-SafePgIdentifier $UserName "PostgreSQL user name"
    if ($HostName -ne "localhost" -or $Port -ne 5432 -or $Database -ne "sess_nexaerp_rev868_verify") { throw "This helper is permanently restricted to localhost:5432 / sess_nexaerp_rev868_verify." }
    if ($blockedDatabaseNames -contains $Database) { throw "Blocked database target: $Database" }
    if ($Database -match 'rev861') { throw "REV861 database names are blocked." }
    if ($MigrationName -ne "20260808190920_Rev868PurchaseLocationAllocationCorrection") { throw "Only REV868 migration target is allowed." }
    $preflightSql = Get-PreflightSql
    $postSql = Get-PostVerificationSql
    foreach ($entry in $preflightSql.GetEnumerator()) { Assert-SqlSafe $entry.Key ([string]$entry.Value) }
    foreach ($entry in $postSql.GetEnumerator()) { Assert-SqlSafe $entry.Key ([string]$entry.Value) }
    if ($GeneratePlanOnly) {
        Write-PlanReport $preflightSql $postSql
        foreach ($operation in Get-ProposedOperations) { Write-Output "-- $operation" }
        foreach ($entry in $preflightSql.GetEnumerator()) { Write-Output "-- Preflight: $($entry.Key)"; Write-Output ([string]$entry.Value); Write-Output "" }
        foreach ($entry in $postSql.GetEnumerator()) { Write-Output "-- Post-test: $($entry.Key)"; Write-Output ([string]$entry.Value); Write-Output "" }
        return
    }

    $gitExe = Resolve-GitExecutable $GitPath
    $dotnet = Resolve-ExecutablePath $dotnetPath ".NET executable"
    $psql = Resolve-ExecutablePath $psqlPath "psql.exe"
    Set-Location $repoRoot
    $gitStatus = (& $gitExe status --short) -join "`n"
    $gitCommit = (& $gitExe rev-parse HEAD).Trim()
    if ($gitStatus) { throw "Git status is not clean before REV868C1 isolated verification." }

    Write-Host "Expected host: $HostName"
    Write-Host "Expected port: $Port"
    Write-Host "Expected database: $Database"
    Write-Host "PostgreSQL user parameter: $UserName"
    Write-Section "REV868C1 secure isolated verification"
    $securePassword = Read-Host -AsSecureString "Enter PostgreSQL password for isolated REV868C1 verification database only"
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    try { $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
    finally { if ($bstr -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) } }
    $env:PGPASSWORD = $plainPassword
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null

    foreach ($entry in $preflightSql.GetEnumerator()) { Add-Evidence $entry.Key ([string]$entry.Value) }
    if ($evidence["Session identity"] -notmatch "database=$Database") { throw "Connected database did not match isolated verification database." }
    if ($evidence["Session identity"] -match "database=sess_nexaerp(\r?\n|$)") { throw "Refusing to run against main development database sess_nexaerp." }
    if ($PreflightOnly) {
        Add-Report "# REV868C1 Isolated Workflow Preflight Report"
        Add-Report ""
        Add-Report "- Source commit: $gitCommit"
        Add-Report "- Expected host: $HostName"
        Add-Report "- Expected port: $Port"
        Add-Report "- Expected database: $Database"
        Add-Report "- Mode: PreflightOnly; no migration/test data changes performed."
        foreach ($entry in $evidence.GetEnumerator()) { Add-Report "## $($entry.Key)"; Add-Report '```text'; Add-Report ([string]$entry.Value); Add-Report '```' }
        Write-Host "REV868C1 isolated workflow preflight report: $reportFile"
        return
    }

    $env:ConnectionStrings__NexaErp = "Host=$HostName;Port=$Port;Database=$Database;Username=$UserName;Password=$plainPassword"
    $env:NexaErp__ExpectedDatabase = $Database
    $env:REV868C1_POSTGRES = $env:ConnectionStrings__NexaErp

    Write-Section "Apply full migration chain to isolated REV868 verification database only"
    Set-Location $targetRoot
    & $dotnet ef database update $MigrationName --project .\src\SESS.NexaERP.Infrastructure\SESS.NexaERP.Infrastructure.csproj --startup-project .\src\SESS.NexaERP.Api\SESS.NexaERP.Api.csproj --context NexaErpDbContext
    if ($LASTEXITCODE -ne 0) { throw "EF database update failed with exit code $LASTEXITCODE." }

    Write-Section "Run PostgreSQL-backed REV868C1 workflow tests"
    $buildOutput = & $dotnet build .\SESS.NexaERP.slnx --configuration Release 2>&1
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed. $((($buildOutput | ForEach-Object { $_.ToString() }) -join "`n"))" }
    $testOutput = & $dotnet test .\SESS.NexaERP.slnx --configuration Release --no-build --filter "Rev868C1PostgresWorkflowVerificationTests|AuthorizationIntegrationTests" 2>&1
    if ($LASTEXITCODE -ne 0) { throw "dotnet test failed. $((($testOutput | ForEach-Object { $_.ToString() }) -join "`n"))" }
    foreach ($entry in $postSql.GetEnumerator()) { Add-Evidence $entry.Key ([string]$entry.Value) }
    foreach ($migrationId in $expectedMigrationIds) { if ($evidence["Eight migration IDs"] -notmatch [regex]::Escape($migrationId)) { throw "Expected migration missing: $migrationId" } }
    if ($evidence["Quantity reconciliation evidence"].Trim() -ne "0") { throw "Quantity reconciliation violations found." }
    if ($evidence["Duplicate active reservation evidence"].Trim() -ne "0") { throw "Duplicate active reservations found." }
    if ($evidence["Duplicate PendingRFQ handoff evidence"].Trim() -ne "0") { throw "Duplicate pending RFQ handoffs found." }

    $scanWordPattern = 'pass' + 'word|pwd|secret|token'
    $scanPattern = '(?i)\b(' + $scanWordPattern + ')\b\s*[:=]\s*[''"`]?(?!\$|%|\{|<|REDACTED|redacted|your_|change_me|example|placeholder)[^''"`\s;]+'
    $secretScanOutput = Invoke-SecretScan $scanPattern $targetRoot

    Add-Report "# REV868C1 Isolated Workflow Verification Final Report"
    Add-Report ""
    Add-Report "- Source commit: $gitCommit"
    Add-Report "- Expected database: $Database"
    Add-Report "- Migration target: $MigrationName"
    Add-Report "- Secret scan: $secretScanOutput"
    Add-Report "- Real OIDC provider/token testing remains pending."
    foreach ($entry in $evidence.GetEnumerator()) { Add-Report "## $($entry.Key)"; Add-Report '```text'; Add-Report ([string]$entry.Value); Add-Report '```' }
    Add-Report "## Test output"
    Add-Report '```text'
    Add-Report (($testOutput | Select-Object -Last 80) -join "`n")
    Add-Report '```'
    Write-Host "REV868C1 isolated workflow verification final report: $reportFile"
}
finally {
    Remove-Item Env:\ConnectionStrings__NexaErp -ErrorAction SilentlyContinue
    Remove-Item Env:\NexaErp__ExpectedDatabase -ErrorAction SilentlyContinue
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:\REV868C1_POSTGRES -ErrorAction SilentlyContinue
    if ($plainPassword) { $plainPassword = $null }
    if ($securePassword) { $securePassword.Dispose() }
}