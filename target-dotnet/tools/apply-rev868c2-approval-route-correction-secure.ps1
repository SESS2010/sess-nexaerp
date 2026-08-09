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
function Get-PreflightSql {
    [ordered]@{
        "Session identity" = @"
select 'database=' || current_database()
union all select 'user=' || current_user
union all select 'server_addr=' || coalesce(inet_server_addr()::text, 'local_socket')
union all select 'server_port=' || inet_server_port()::text;
"@.Trim()
        "Protected target verification" = @"
select case
    when current_database() = 'sess_nexaerp_rev868_verify' then 'target_database=PASS'
    else 'target_database=FAIL:' || current_database()
end
union all select 'rejected_databases=sess_nexaerp,postgres,template0,template1,REV861-like names';
"@.Trim()
        "First eight migration prerequisite status" = @"
select expected."MigrationId" || '|count=' || count(h."MigrationId")::text || '|' || case when count(h."MigrationId") = 1 then 'PASS' else 'FAIL' end
from (values
 ('20260808110924_Phase1Foundation'),
 ('20260808114550_Phase1AuthorizationSeed'),
 ('20260808123411_Rev866EmployeePermissionMatrix'),
 ('20260808142353_Rev866CorrectiveStatusPermissionAudit'),
 ('20260808151207_Rev867MasterFoundation'),
 ('20260808160435_Rev867C1Corrections'),
 ('20260808182945_Rev868PurchaseRequisitionFoundation'),
 ('20260808190920_Rev868PurchaseLocationAllocationCorrection')
) expected("MigrationId")
left join "public"."__EFMigrationsHistory" h on h."MigrationId" = expected."MigrationId"
group by expected."MigrationId"
order by expected."MigrationId";
"@.Trim()
        "REV868C2 migration absence" = @"
select '20260809123000_Rev868C2DepartmentManagerApprovalMapping|count=' || count(*)::text || '|' || case when count(*) = 0 then 'ABSENT_PASS' else 'ALREADY_PRESENT_FAIL' end
from "public"."__EFMigrationsHistory"
where "MigrationId" = '20260809123000_Rev868C2DepartmentManagerApprovalMapping';
"@.Trim()
        "Current required source tables" = @"
select required.relname || '=' || case when c.oid is null then 'MISSING' else 'PRESENT' end
from (values
 ('purchase_approval_route_settings'),
 ('purchase_requisitions'),
 ('departments'),
 ('employees'),
 ('employee_role_assignments'),
 ('roles'),
 ('audit_logs')
) required(relname)
left join pg_catalog.pg_class c on c.relname = required.relname
left join pg_catalog.pg_namespace n on n.oid = c.relnamespace and n.nspname = 'nexa'
order by required.relname;
"@.Trim()
    }
}
function Get-PostMigrationSql {
    [ordered]@{
        "Ninth migration status" = @"
select '20260809123000_Rev868C2DepartmentManagerApprovalMapping|count=' || count(*)::text || '|' || case when count(*) = 1 then 'PASS' else 'FAIL' end
from "public"."__EFMigrationsHistory"
where "MigrationId" = '20260809123000_Rev868C2DepartmentManagerApprovalMapping';
"@.Trim()
        "Department approval mapping table evidence" = @"
select 'table=' || case when to_regclass('nexa.department_approval_mappings') is null then 'MISSING' else 'PRESENT' end
union all
select 'column=' || column_name || '|nullable=' || is_nullable || '|type=' || data_type
from information_schema.columns
where table_schema = 'nexa' and table_name = 'department_approval_mappings'
  and column_name in ('DepartmentId','ApprovalRouteCode','PrimaryApproverEmployeeId','AlternateApproverEmployeeId','EffectiveFrom','EffectiveTo','IsActive','CreatedBy','CreatedAt','UpdatedBy','UpdatedAt','Version')
union all
select 'index=' || indexname from pg_indexes where schemaname = 'nexa' and tablename = 'department_approval_mappings'
union all
select 'fk_or_check=' || conname || '|type=' || contype::text from pg_catalog.pg_constraint where conrelid = 'nexa.department_approval_mappings'::regclass
order by 1;
"@.Trim()
        "Three canonical route rows" = @"
select "RouteCode"
    || '|min=' || "MinimumAmount"::text
    || '|max=' || coalesce("MaximumAmount"::text,'NULL')
    || '|resolution=' || "ApproverResolutionType"
    || '|role=' || coalesce("ApproverRoleCode", 'NULL')
    || '|active=' || "IsActive"::text
from nexa.purchase_approval_route_settings
where "RouteCode" in ('MANAGER','TECHNICAL_DIRECTOR','MANAGING_DIRECTOR')
order by "MinimumAmount";
"@.Trim()
        "Route integrity evidence" = @"
with active_routes as (
    select "RouteCode", "MinimumAmount", "MaximumAmount", "ApproverResolutionType", "ApproverRoleCode"
    from nexa.purchase_approval_route_settings
    where "IsActive" = true and "RouteCode" in ('MANAGER','TECHNICAL_DIRECTOR','MANAGING_DIRECTOR')
), ordered as (
    select *, lead("MinimumAmount") over (order by "MinimumAmount") as next_min
    from active_routes
)
select 'active_canonical_routes=' || count(*) || '|' || case when count(*) = 3 then 'PASS' else 'FAIL' end from active_routes
union all select 'duplicate_active_route_codes=' || count(*) from (select "RouteCode" from active_routes group by "RouteCode" having count(*) > 1) d
union all select 'overlapping_ranges=' || count(*) from (
    select a."RouteCode" from active_routes a join active_routes b on a."RouteCode" < b."RouteCode"
    where a."MinimumAmount" <= coalesce(b."MaximumAmount", 999999999999.99)
      and b."MinimumAmount" <= coalesce(a."MaximumAmount", 999999999999.99)
) o
union all select 'currency_gaps_at_decimal_18_2=' || count(*) from ordered where "MaximumAmount" is not null and next_min <> "MaximumAmount" + 0.01
union all select 'first_range_starts_at_zero=' || case when min("MinimumAmount") = 0.00 then 'PASS' else 'FAIL' end from active_routes
union all select 'final_range_has_no_max=' || case when count(*) filter (where "MaximumAmount" is null) = 1 then 'PASS' else 'FAIL' end from active_routes
union all select 'negative_amount_rejected=SOURCE_VALIDATED';
"@.Trim()
        "Amount boundary evidence" = @"
select b.amount::numeric(18,2)::text
    || '|expected_route=' || b.expected_route
    || '|configured_route=' || coalesce(r."RouteCode", 'NO_ROUTE')
    || '|expected_resolution=' || b.expected_resolution
    || '|actual_resolution=' || coalesce(r."ApproverResolutionType", 'NO_RESOLUTION')
    || '|expected_role=' || coalesce(b.expected_role, 'NULL')
    || '|actual_role=' || coalesce(r."ApproverRoleCode", 'NULL')
    || '|' || case when b.expected_route = r."RouteCode" and b.expected_resolution = r."ApproverResolutionType" and coalesce(b.expected_role, 'NULL') = coalesce(r."ApproverRoleCode", 'NULL') then 'PASS' else 'FAIL' end
from (
  values
    (0.00::numeric, 'MANAGER'::text, 'DEPARTMENT_MAPPING'::text, null::text),
    (50000.00::numeric, 'MANAGER'::text, 'DEPARTMENT_MAPPING'::text, null::text),
    (50000.01::numeric, 'TECHNICAL_DIRECTOR'::text, 'FIXED_ROLE'::text, 'TECHNICAL_DIRECTOR'::text),
    (50001.00::numeric, 'TECHNICAL_DIRECTOR'::text, 'FIXED_ROLE'::text, 'TECHNICAL_DIRECTOR'::text),
    (500000.00::numeric, 'TECHNICAL_DIRECTOR'::text, 'FIXED_ROLE'::text, 'TECHNICAL_DIRECTOR'::text),
    (500000.01::numeric, 'MANAGING_DIRECTOR'::text, 'FIXED_ROLE'::text, 'MANAGING_DIRECTOR'::text),
    (500001.00::numeric, 'MANAGING_DIRECTOR'::text, 'FIXED_ROLE'::text, 'MANAGING_DIRECTOR'::text)
) b(amount, expected_route, expected_resolution, expected_role)
left join nexa.purchase_approval_route_settings r
  on r."IsActive" = true
 and b.amount >= r."MinimumAmount"
 and (r."MaximumAmount" is null or b.amount <= r."MaximumAmount")
order by b.amount;
"@.Trim()
        "Department manager mapping coverage" = @"
with active_departments as (
    select "Id" from nexa.departments where "IsActive" = true
), active_mappings as (
    select m.* from nexa.department_approval_mappings m
    where m."ApprovalRouteCode" = 'MANAGER' and m."IsActive" = true
      and m."EffectiveFrom" <= current_date
      and (m."EffectiveTo" is null or m."EffectiveTo" >= current_date)
)
select 'active_departments_requiring_pr_approval=' || count(*) from active_departments
union all select 'active_manager_mappings=' || count(*) from active_mappings
union all select 'departments_missing_manager_mapping=' || count(*) from active_departments d where not exists (select 1 from active_mappings m where m."DepartmentId" = d."Id")
union all select 'duplicate_active_primary_manager_mappings=' || count(*) from (select "DepartmentId" from active_mappings group by "DepartmentId" having count(*) > 1) d
union all select 'inactive_primary_approvers=' || count(*) from active_mappings m join nexa.employees e on e."Id" = m."PrimaryApproverEmployeeId" where e."Status" <> 'Active' or e."LoginEnabled" = false
union all select 'missing_approval_permission=' || count(*) from active_mappings m where not exists (select 1 from nexa.employee_role_assignments era join nexa.roles r on r."Id" = era."RoleId" where era."EmployeeId" = m."PrimaryApproverEmployeeId" and era."ApprovalStatus" = 'SeedApproved' and era."EffectiveFrom" <= current_date and (era."EffectiveTo" is null or era."EffectiveTo" >= current_date) and (r."Code" like '%_MANAGER' or r."Code" in ('MANAGER','DEPARTMENT_MANAGER')))
union all select 'self_mapping_requester_manager_denial=SOURCE_AND_RUNTIME_TEST_REQUIRED'
union all select 'active_delegate_validity_checked=' || count(*) from active_mappings m left join nexa.employees e on e."Id" = m."AlternateApproverEmployeeId" where m."AlternateApproverEmployeeId" is not null and e."Status" = 'Active' and e."LoginEnabled" = true
union all select 'invalid_effective_dates=' || count(*) from nexa.department_approval_mappings where "EffectiveTo" is not null and "EffectiveTo" < "EffectiveFrom";
"@.Trim()
    }
}
function Assert-SqlReadOnly([string]$Title, [string]$Sql) {
    if ([string]::IsNullOrWhiteSpace($Sql) -or -not $Sql.TrimEnd().EndsWith(';')) { throw "SQL '$Title' is invalid." }
    $masked = [regex]::Replace($Sql, "'([^']|'')*'", "'s'")
    if ($masked -match '(?i)\b(insert|update|delete|merge|create|alter|drop|truncate|grant|revoke|copy|call|do)\b') { throw "SQL '$Title' is not read-only." }
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
function Write-PlanReport([System.Collections.Specialized.OrderedDictionary]$PreflightSql, [System.Collections.Specialized.OrderedDictionary]$PostSql) {
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
    Add-Report "# REV868C2 Approval Route Correction Plan"
    Add-Report ""
    Add-Report "- Host: $HostName"
    Add-Report "- Port: $Port"
    Add-Report "- Target DB: $Database"
    Add-Report "- Rejected DBs: sess_nexaerp, postgres, template0, template1, REV861-like names"
    Add-Report "- Prerequisite: existing first 8 migrations exactly once"
    Add-Report "- Target corrective migration: $correctionMigration"
    Add-Report "- Migration that would be applied: $correctionMigration only"
    Add-Report "- Expected migration count after execution: 9"
    Add-Report "- No backup/restore/drop/create operation"
    Add-Report "- No main DB operation"
    Add-Report ""
    Add-Report "## Preflight SQL"
    foreach ($entry in $PreflightSql.GetEnumerator()) { Add-Report "### $($entry.Key)"; Add-Report '```sql'; Add-Report ([string]$entry.Value); Add-Report '```' }
    Add-Report "## Post-Migration SQL"
    foreach ($entry in $PostSql.GetEnumerator()) { Add-Report "### $($entry.Key)"; Add-Report '```sql'; Add-Report ([string]$entry.Value); Add-Report '```' }
    Write-Host "REV868C2 approval route correction plan report: $reportFile"
}
function Write-EvidenceReport([string]$Mode) {
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
    Add-Report "# REV868C2 Approval Route Correction $Mode Report"
    Add-Report ""
    Add-Report "- Host: $HostName"
    Add-Report "- Port: $Port"
    Add-Report "- Target DB: $Database"
    Add-Report "- Rejected DBs: sess_nexaerp, postgres, template0, template1, REV861-like names"
    Add-Report "- Target corrective migration: $correctionMigration"
    Add-Report "- Migration applied by this helper: $correctionMigration only"
    Add-Report "- Expected migration count after execution: 9"
    foreach ($entry in $evidence.GetEnumerator()) { Add-Report "## $($entry.Key)"; Add-Report '```text'; Add-Report ([string]$entry.Value); Add-Report '```' }
    Write-Host "REV868C2 approval route correction report: $reportFile"
}

try {
    Write-Section "REV868C2 no-secret prechecks"
    Assert-SafePgIdentifier $Database "Verification database name"
    Assert-SafePgIdentifier $UserName "PostgreSQL user name"
    if ($HostName -ne "localhost" -or $Port -ne 5432 -or $Database -ne "sess_nexaerp_rev868_verify") { throw "This helper is permanently restricted to localhost:5432 / sess_nexaerp_rev868_verify." }
    if ($blockedDatabaseNames -contains $Database -or $Database -match 'rev861') { throw "Blocked database target: $Database" }
    $preflightSql = Get-PreflightSql
    $postSql = Get-PostMigrationSql
    foreach ($entry in $preflightSql.GetEnumerator()) { Assert-SqlReadOnly $entry.Key ([string]$entry.Value) }
    foreach ($entry in $postSql.GetEnumerator()) { Assert-SqlReadOnly $entry.Key ([string]$entry.Value) }
    if (($preflightSql.Values -join "`n") -match 'department_approval_mappings') { throw "Preflight SQL must not reference post-migration table department_approval_mappings." }
    if ($GeneratePlanOnly) { Write-PlanReport $preflightSql $postSql; return }

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

    foreach ($entry in $preflightSql.GetEnumerator()) { Add-Evidence $entry.Key ([string]$entry.Value) }
    if ($evidence["Session identity"] -notmatch "database=$Database") { throw "Connected database did not match isolated verification database." }
    if ($PreflightOnly) { Write-EvidenceReport "Preflight"; return }

    Set-Location $targetRoot
    & $dotnet ef database update $correctionMigration --project .\src\SESS.NexaERP.Infrastructure\SESS.NexaERP.Infrastructure.csproj --startup-project .\src\SESS.NexaERP.Api\SESS.NexaERP.Api.csproj --context NexaErpDbContext
    if ($LASTEXITCODE -ne 0) { throw "EF database update failed with exit code $LASTEXITCODE." }
    foreach ($entry in $postSql.GetEnumerator()) { Add-Evidence $entry.Key ([string]$entry.Value) }
    Write-EvidenceReport "Final"
}
finally {
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:\ConnectionStrings__NexaErp -ErrorAction SilentlyContinue
    Remove-Item Env:\NexaErp__ExpectedDatabase -ErrorAction SilentlyContinue
    if ($plainPassword) { $plainPassword = $null }
    if ($securePassword) { $securePassword.Dispose() }
}
