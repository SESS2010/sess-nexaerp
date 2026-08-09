[CmdletBinding()]
param(
    [string]$Database = "sess_nexaerp_rev868_verify",
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$UserName = "postgres",
    [string]$GitPath = "",
    [switch]$PreflightOnly,
    [switch]$GeneratePlanOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$targetRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$dotnetPath = Join-Path $targetRoot "..\.dotnet10\dotnet.exe"
$psqlPath = "C:\Program Files\PostgreSQL\17\bin\psql.exe"
$reportDir = Join-Path $targetRoot "local-evidence\rev868c2"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$reportFile = Join-Path $reportDir "rev868c2_approval_route_correction_$timestamp.md"
$correctionMigration = "20260809123000_Rev868C2DepartmentManagerApprovalMapping"
$requiredBefore = @(
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
    $candidates = @($ExplicitGitPath, "C:\Users\User\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\git\cmd\git.exe", "C:\Program Files\Git\cmd\git.exe", "C:\Program Files\Git\bin\git.exe") | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique
    foreach ($candidate in $candidates) { if (Test-Path -LiteralPath $candidate -PathType Leaf) { $item = Get-Item -LiteralPath $candidate; if ($item.Name -ne "git.exe") { throw "Resolved Git path is not git.exe: $($item.FullName)" }; $v = & $item.FullName --version; if ($LASTEXITCODE -eq 0 -and ($v -join "`n") -match '^git version ') { return $item.FullName } } }
    throw "git.exe was not found. Pass -GitPath with a valid git.exe path."
}
function Get-ReadOnlySql {
    [ordered]@{
        "Session identity" = @"
select 'database=' || current_database()
union all select 'user=' || current_user
union all select 'server_addr=' || coalesce(inet_server_addr()::text, 'local_socket')
union all select 'server_port=' || inet_server_port()::text;
"@.Trim()
        "Migration prerequisite status" = @"
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
 '20260808190920_Rev868PurchaseLocationAllocationCorrection',
 '20260809123000_Rev868C2DepartmentManagerApprovalMapping')
group by "MigrationId"
order by "MigrationId";
"@.Trim()
        "Approval route configuration" = @"
select "RouteCode"
    || '|min=' || "MinimumAmount"::text
    || '|max=' || coalesce("MaximumAmount"::text,'NULL')
    || '|role=' || "ApproverRoleCode"
    || '|display=' || case "RouteCode"
        when 'MANAGER' then 'Manager'
        when 'TECHNICAL_DIRECTOR' then 'Technical Director'
        when 'MANAGING_DIRECTOR' then 'Managing Director'
        else "RouteCode"
       end
    || '|active=' || "IsActive"::text
    || '|order=' || row_number() over (order by "MinimumAmount")::text
from nexa.purchase_approval_route_settings
where "RouteCode" in ('MANAGER','TECHNICAL_DIRECTOR','MANAGING_DIRECTOR')
order by "MinimumAmount";
"@.Trim()
        "Amount routing boundary evidence" = @"
select b.amount::text
    || '|expected_route=' || b.expected_route
    || '|configured_route=' || coalesce(r."RouteCode", 'NO_ROUTE')
    || '|canonical_role=' || coalesce(r."ApproverRoleCode", 'NO_ROLE')
    || '|display=' || case coalesce(r."RouteCode", 'NO_ROUTE')
        when 'MANAGER' then 'Manager'
        when 'TECHNICAL_DIRECTOR' then 'Technical Director'
        when 'MANAGING_DIRECTOR' then 'Managing Director'
        else 'Unknown'
       end
    || '|' || case when b.expected_route = r."RouteCode" and b.expected_role = r."ApproverRoleCode" then 'PASS' else 'FAIL' end
from (
  values
    (0::numeric, 'MANAGER'::text, 'DEPARTMENT_MANAGER'::text),
    (50000::numeric, 'MANAGER'::text, 'DEPARTMENT_MANAGER'::text),
    (50000.01::numeric, 'TECHNICAL_DIRECTOR'::text, 'TECHNICAL_DIRECTOR'::text),
    (50001::numeric, 'TECHNICAL_DIRECTOR'::text, 'TECHNICAL_DIRECTOR'::text),
    (500000::numeric, 'TECHNICAL_DIRECTOR'::text, 'TECHNICAL_DIRECTOR'::text),
    (500000.01::numeric, 'MANAGING_DIRECTOR'::text, 'MANAGING_DIRECTOR'::text),
    (500001::numeric, 'MANAGING_DIRECTOR'::text, 'MANAGING_DIRECTOR'::text)
) b(amount, expected_route, expected_role)
left join nexa.purchase_approval_route_settings r
  on r."IsActive" = true
 and b.amount >= r."MinimumAmount"
 and (r."MaximumAmount" is null or b.amount <= r."MaximumAmount")
order by b.amount;
"@.Trim()
        "Department manager mapping coverage" = @"
select 'department_manager_mapping_count=' || count(*)
    || '|active_count=' || count(*) filter (where "IsActive")
    || '|missing_primary=' || count(*) filter (where "PrimaryApproverEmployeeId" is null)
    || '|missing_department=' || count(*) filter (where "DepartmentId" is null)
from "nexa"."department_approval_mappings"
where "ApprovalRouteCode" = 'MANAGER';
"@.Trim()
        "Route gap overlap duplicate disabled evidence" = @"
select 'duplicate_active_routes=' || count(*) from (
    select "RouteCode" from nexa.purchase_approval_route_settings where "IsActive" = true group by "RouteCode" having count(*) > 1
) d
union all select 'overlapping_active_ranges=' || count(*) from (
    select a."Id" from nexa.purchase_approval_route_settings a join nexa.purchase_approval_route_settings b on a."Id" < b."Id"
    where a."IsActive" = true and b."IsActive" = true
      and a."MinimumAmount" <= coalesce(b."MaximumAmount", 999999999999.99)
      and b."MinimumAmount" <= coalesce(a."MaximumAmount", 999999999999.99)
) o
union all select 'inactive_canonical_routes=' || count(*) from nexa.purchase_approval_route_settings where "RouteCode" in ('MANAGER','TECHNICAL_DIRECTOR','MANAGING_DIRECTOR') and "IsActive" = false
union all select 'missing_canonical_routes=' || (3 - count(*)) from nexa.purchase_approval_route_settings where "RouteCode" in ('MANAGER','TECHNICAL_DIRECTOR','MANAGING_DIRECTOR') and "IsActive" = true;
"@.Trim()
    }
}
function Assert-SqlReadOnly([string]$Title, [string]$Sql) {
    if ([string]::IsNullOrWhiteSpace($Sql) -or -not $Sql.TrimEnd().EndsWith(';')) { throw "SQL '$Title' is invalid." }
    if ($Sql -match '(?i)\b(insert|update|delete|merge|create|alter|drop|truncate|grant|revoke|copy|call|do)\b') { throw "SQL '$Title' is not read-only." }
}
function Invoke-PsqlRead([string]$Sql) {
    $sqlFile = Join-Path ([System.IO.Path]::GetTempPath()) ("sess_nexa_rev868c2_" + [Guid]::NewGuid().ToString("N") + ".sql")
    try {
        [System.IO.File]::WriteAllText($sqlFile, "begin transaction read only;`n$Sql`ncommit;", [System.Text.UTF8Encoding]::new($false))
        $output = @(& $psql -h $HostName -p $Port -U $UserName -d $Database -v ON_ERROR_STOP=1 -At -f $sqlFile 2>&1)
        if ($LASTEXITCODE -ne 0) { throw "psql failed with exit code $LASTEXITCODE. $((($output | ForEach-Object { $_.ToString() }) -join "`n"))" }
        return (@($output | Where-Object { $_.ToString() -notin @('BEGIN','COMMIT') }) -join "`n")
    }
    finally { Remove-Item -LiteralPath $sqlFile -Force -ErrorAction SilentlyContinue }
}
function Add-Evidence([string]$Title, [string]$Sql) { $evidence[$Title] = Invoke-PsqlRead $Sql }
function Write-Report([string]$Mode, [System.Collections.Specialized.OrderedDictionary]$Sql) {
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
    Add-Report "# REV868C2 Approval Route Correction $Mode Report"
    Add-Report ""
    Add-Report "- Expected host: $HostName"
    Add-Report "- Expected port: $Port"
    Add-Report "- Expected database: $Database"
    Add-Report "- Migration: $correctionMigration"
    foreach ($entry in $Sql.GetEnumerator()) { Add-Report "## $($entry.Key)"; Add-Report '```sql'; Add-Report ([string]$entry.Value); Add-Report '```' }
    Write-Host "REV868C2 approval route correction report: $reportFile"
}

try {
    Write-Section "REV868C2 no-secret prechecks"
    Assert-SafePgIdentifier $Database "Verification database name"
    Assert-SafePgIdentifier $UserName "PostgreSQL user name"
    if ($HostName -ne "localhost" -or $Port -ne 5432 -or $Database -ne "sess_nexaerp_rev868_verify") { throw "This helper is permanently restricted to localhost:5432 / sess_nexaerp_rev868_verify." }
    if ($blockedDatabaseNames -contains $Database -or $Database -match 'rev861') { throw "Blocked database target: $Database" }
    $sql = Get-ReadOnlySql
    foreach ($entry in $sql.GetEnumerator()) { Assert-SqlReadOnly $entry.Key ([string]$entry.Value) }
    if ($GeneratePlanOnly) { Write-Report "Plan" $sql; foreach ($entry in $sql.GetEnumerator()) { Write-Output "-- $($entry.Key)"; Write-Output ([string]$entry.Value); Write-Output "" }; return }

    $gitExe = Resolve-GitExecutable $GitPath
    $dotnet = Resolve-ExecutablePath $dotnetPath ".NET executable"
    $psql = Resolve-ExecutablePath $psqlPath "psql.exe"
    Set-Location $repoRoot
    $gitStatus = (& $gitExe status --short) -join "`n"
    if ($gitStatus) { throw "Git status is not clean before REV868C2 approval route correction." }

    Write-Host "Expected host: $HostName"
    Write-Host "Expected port: $Port"
    Write-Host "Expected database: $Database"
    Write-Host "PostgreSQL user parameter: $UserName"
    $securePassword = Read-Host -AsSecureString "Enter PostgreSQL password for isolated REV868C2 verification database only"
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    try { $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
    finally { if ($bstr -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) } }
    $env:PGPASSWORD = $plainPassword
    $env:ConnectionStrings__NexaErp = "Host=$HostName;Port=$Port;Database=$Database;Username=$UserName;Password=$plainPassword"
    $env:NexaErp__ExpectedDatabase = $Database
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null

    Add-Evidence "Session identity" $sql["Session identity"]
    if ($evidence["Session identity"] -notmatch "database=$Database") { throw "Connected database did not match isolated verification database." }
    if ($PreflightOnly) { foreach ($entry in $sql.GetEnumerator()) { if ($entry.Key -ne "Session identity") { Add-Evidence $entry.Key ([string]$entry.Value) } }; Write-Report "Preflight" $sql; return }

    Set-Location $targetRoot
    & $dotnet ef database update $correctionMigration --project .\src\SESS.NexaERP.Infrastructure\SESS.NexaERP.Infrastructure.csproj --startup-project .\src\SESS.NexaERP.Api\SESS.NexaERP.Api.csproj --context NexaErpDbContext
    if ($LASTEXITCODE -ne 0) { throw "EF database update failed with exit code $LASTEXITCODE." }
    foreach ($entry in $sql.GetEnumerator()) { if ($entry.Key -ne "Session identity") { Add-Evidence $entry.Key ([string]$entry.Value) } }
    Add-Report "# REV868C2 Approval Route Correction Final Report"
    foreach ($entry in $evidence.GetEnumerator()) { Add-Report "## $($entry.Key)"; Add-Report '```text'; Add-Report ([string]$entry.Value); Add-Report '```' }
    Write-Host "REV868C2 approval route correction final report: $reportFile"
}
finally {
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:\ConnectionStrings__NexaErp -ErrorAction SilentlyContinue
    Remove-Item Env:\NexaErp__ExpectedDatabase -ErrorAction SilentlyContinue
    if ($plainPassword) { $plainPassword = $null }
    if ($securePassword) { $securePassword.Dispose() }
}
