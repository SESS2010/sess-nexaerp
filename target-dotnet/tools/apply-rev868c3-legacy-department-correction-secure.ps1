[CmdletBinding()]
param(
    [string]$Database = 'sess_nexaerp_rev868_verify',
    [string]$HostName = 'localhost',
    [int]$Port = 5432,
    [string]$UserName = 'postgres',
    [string]$DotnetEfPath,
    [switch]$GeneratePlanOnly,
    [switch]$PreflightOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$MigrationName = '20260810110000_Rev868C3LegacyMixedDepartmentDeactivationCorrection'
$PrerequisiteMigration = '20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation'
$RequiredMigrations = @(
    '20260808110924_Phase1Foundation',
    '20260808114550_Phase1AuthorizationSeed',
    '20260808123411_Rev866EmployeePermissionMatrix',
    '20260808142353_Rev866CorrectiveStatusPermissionAudit',
    '20260808151207_Rev867MasterFoundation',
    '20260808160435_Rev867C1Corrections',
    '20260808182945_Rev868PurchaseRequisitionFoundation',
    '20260808190920_Rev868PurchaseLocationAllocationCorrection',
    '20260809123000_Rev868C2DepartmentManagerApprovalMapping',
    '20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation'
)
$ExpectedDatabase = 'sess_nexaerp_rev868_verify'
$RejectedDatabases = @('sess_nexaerp','postgres','template0','template1')
$Actor = 'REV868C3_LEGACY_DEPARTMENT_DEACTIVATION_CORRECTION'
$LegacyCodes = @('ENGINEER_TECHNICAL','MANAGER','JUNIOR_ASSISTANT','ADMIN_ACCOUNTS_STORES')
$CleanCodes = @('MANAGEMENT','PURCHASE','STORES','ACCOUNTS_FINANCE','HR_ADMIN','PRODUCTION_FABRICATION','DESIGN','ELECTRICAL_PLC_INSTRUMENTATION','REFRIGERATION_MECHANICAL','SERVICE_TECHNICAL_SUPPORT','SOFTWARE_IT','QUALITY_QC')
$psqlPath = 'C:\Program Files\PostgreSQL\17\bin\psql.exe'
$dotnetPath = 'C:\Program Files\dotnet\dotnet.exe'
$InfrastructureProject = Join-Path $root 'src\SESS.NexaERP.Infrastructure\SESS.NexaERP.Infrastructure.csproj'
$StartupProject = Join-Path $root 'src\SESS.NexaERP.Api\SESS.NexaERP.Api.csproj'
$evidenceDir = Join-Path $root 'local-evidence\rev868c3-legacy-department-correction'
$script:tempSqlFile = $null
$plainPassword = $null
$securePassword = $null

function Assert-TargetDatabaseName([string]$Name) {
    if ($Name -ne $ExpectedDatabase -or $Database -ne $ExpectedDatabase) { throw "REV868C3 corrective helper is restricted to $ExpectedDatabase. Actual: $Name" }
    if ($RejectedDatabases -contains $Name -or $Name -match '(?i)rev861') { throw "Protected database rejected: $Name" }
}
function Resolve-File([string]$Path,[string]$Label) { if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){throw "$Label not found: $Path"}; (Resolve-Path -LiteralPath $Path).Path }
function SqlList([string[]]$Values) { (($Values | ForEach-Object { "'" + ($_ -replace "'","''") + "'" }) -join ',') }
function Get-PreflightSql { $legacy=SqlList $LegacyCodes; $requiredRows=(($RequiredMigrations|ForEach-Object{"('"+($_ -replace "'","''")+"')"})-join ','); @"
begin transaction read only;
with expected_migrations("MigrationId") as (values $requiredRows), actual_migrations as (select "MigrationId" from "public"."__EFMigrationsHistory"), expected_legacy(code) as (select unnest(array[$legacy])), actual_legacy(code) as (select "Code" from nexa.departments where "Code" in ($legacy)), states as (
  select
    (select count(*) from expected_migrations) prerequisite_expected_count,
    (select count(*) from actual_migrations a join expected_migrations e using("MigrationId")) prerequisite_actual_matched_count,
    (select count(*) from expected_migrations e left join actual_migrations a using("MigrationId") where a."MigrationId" is null) prerequisite_missing_count,
    (select count(*) from actual_migrations a left join expected_migrations e using("MigrationId") where e."MigrationId" is null) prerequisite_unexpected_count,
    (select count(*) from (select "MigrationId" from actual_migrations group by "MigrationId" having count(*)>1) d) prerequisite_duplicate_count,
    (select count(*) from actual_migrations where "MigrationId"='$MigrationName') target_correction_migration_count,
    (select count(*) from pg_catalog.pg_class c join pg_catalog.pg_namespace n on n.oid=c.relnamespace where n.nspname='nexa' and c.relname='rev868c3_legacy_department_deactivation_backup') backup_relation_count,
    (select count(*) from nexa.departments where "UpdatedBy"='$Actor') migration_owned_department_count,
    (select count(*) from actual_legacy) legacy_department_count,
    (select count(distinct code) from actual_legacy) legacy_department_distinct_count,
    (select count(*) from expected_legacy e left join actual_legacy a using(code) where a.code is null) legacy_department_missing_count,
    (select count(*) from nexa.departments where "Code" in ($legacy) and "IsActive"=true) active_legacy_department_count,
    (select count(*) from nexa.employees e join nexa.departments d on d."Id"=e."DepartmentId" where e."EmployeeCode" like 'SESS-%' and lower(e."Status")='active' and d."Code" in ($legacy)) active_employee_legacy_department_reference_count,
    (select count(*) from nexa.department_approval_mappings m join nexa.departments d on d."Id"=m."DepartmentId" where m."ApprovalRouteCode"='MANAGER' and m."IsActive"=true and d."Code" in ($legacy)) active_manager_mapping_legacy_department_reference_count,
    (select count(*) from nexa.employees where "EmployeeCode" like 'SESS-%' and lower("Status")='active') total_active_sess_employee_count,
    (select count(*) from nexa.department_approval_mappings where "ApprovalRouteCode"='MANAGER' and "IsActive"=true) total_active_manager_mapping_count,
    (select count(*) from nexa.purchase_requisitions pr join nexa.departments d on d."Id"=pr."RequestingDepartmentId" where d."Code" in ($legacy) and pr."IsActive"=true and pr."Status" not in ('Completed','Rejected','Cancelled')) active_open_pr_legacy_department_reference_count,
    (select count(*) from nexa.purchase_requisitions pr join nexa.departments d on d."Id"=pr."RequestingDepartmentId" where d."Code" in ($legacy) and not (pr."IsActive"=true and pr."Status" not in ('Completed','Rejected','Cancelled'))) historical_pr_legacy_department_reference_count
)
select 'prerequisite_expected_count='||prerequisite_expected_count from states
union all select 'prerequisite_actual_matched_count='||prerequisite_actual_matched_count from states
union all select 'prerequisite_missing_count='||prerequisite_missing_count from states
union all select 'prerequisite_unexpected_count='||prerequisite_unexpected_count from states
union all select 'prerequisite_duplicate_count='||prerequisite_duplicate_count from states
union all select 'target_correction_migration_count='||target_correction_migration_count from states
union all select 'backup_relation_count='||backup_relation_count from states
union all select 'migration_owned_department_count='||migration_owned_department_count from states
union all select 'legacy_department_count='||legacy_department_count from states
union all select 'legacy_department_distinct_count='||legacy_department_distinct_count from states
union all select 'legacy_department_missing_count='||legacy_department_missing_count from states
union all select 'active_legacy_department_count='||active_legacy_department_count from states
union all select 'active_employee_legacy_department_reference_count='||active_employee_legacy_department_reference_count from states
union all select 'active_manager_mapping_legacy_department_reference_count='||active_manager_mapping_legacy_department_reference_count from states
union all select 'total_active_sess_employee_count='||total_active_sess_employee_count from states
union all select 'total_active_manager_mapping_count='||total_active_manager_mapping_count from states
union all select 'active_open_pr_legacy_department_reference_count='||active_open_pr_legacy_department_reference_count from states
union all select 'historical_pr_legacy_department_reference_count='||historical_pr_legacy_department_reference_count from states
union all select 'safe_retry_state='||case when prerequisite_expected_count=10 and prerequisite_actual_matched_count=10 and prerequisite_missing_count=0 and prerequisite_unexpected_count=0 and prerequisite_duplicate_count=0 and target_correction_migration_count=0 and backup_relation_count=0 and migration_owned_department_count=0 and legacy_department_count=4 and legacy_department_distinct_count=4 and legacy_department_missing_count=0 and active_legacy_department_count=4 and active_employee_legacy_department_reference_count=0 and active_manager_mapping_legacy_department_reference_count=0 and total_active_sess_employee_count=42 and total_active_manager_mapping_count=14 and active_open_pr_legacy_department_reference_count=0 then 'PASS' else 'FAIL' end from states
union all select 'preflight_acceptance_state='||case when prerequisite_expected_count=10 and prerequisite_actual_matched_count=10 and prerequisite_missing_count=0 and prerequisite_unexpected_count=0 and prerequisite_duplicate_count=0 and target_correction_migration_count=0 and backup_relation_count=0 and migration_owned_department_count=0 and legacy_department_count=4 and legacy_department_distinct_count=4 and legacy_department_missing_count=0 and active_legacy_department_count=4 and active_employee_legacy_department_reference_count=0 and active_manager_mapping_legacy_department_reference_count=0 and total_active_sess_employee_count=42 and total_active_manager_mapping_count=14 and active_open_pr_legacy_department_reference_count=0 then 'PASS' else 'FAIL' end from states;
commit;
"@ }
function Get-PostVerificationSql { $legacy=SqlList $LegacyCodes; $clean=SqlList $CleanCodes; @"
begin transaction read only;
with expected_clean(code) as (select unnest(array[$clean])), active(code) as (select "Code" from nexa.departments where "IsActive"=true), states as (
  select
    (select count(*) from "public"."__EFMigrationsHistory" where "MigrationId"='$MigrationName') corrective_migration_count,
    (select count(*) from nexa.rev868c3_legacy_department_deactivation_backup) backup_row_count,
    (select count(*) from active a join expected_clean e using(code)) active_clean_department_count,
    (select count(*) from expected_clean e left join active a using(code) where a.code is null) missing_clean_department_count,
    (select count(*) from active a left join expected_clean e using(code) where e.code is null) unexpected_active_department_count,
    (select count(*) from nexa.departments where "Code" in ($legacy) and "IsActive"=true) active_legacy_department_count,
    (select count(*) from nexa.departments where "Code" in ($legacy) and "IsActive"=false) inactive_legacy_department_count,
    (select count(*) from nexa.departments where "Code" in ($legacy) and "IsActive"=false and "UpdatedBy"='$Actor') corrected_legacy_department_count,
    (select count(*) from nexa.employees e join nexa.departments d on d."Id"=e."DepartmentId" where e."EmployeeCode" like 'SESS-%' and lower(e."Status")='active' and d."Code" in ($legacy)) active_employee_legacy_department_reference_count,
    (select count(*) from nexa.department_approval_mappings m join nexa.departments d on d."Id"=m."DepartmentId" where m."ApprovalRouteCode"='MANAGER' and m."IsActive"=true and d."Code" in ($legacy)) active_manager_mapping_legacy_department_reference_count,
    (select count(*) from nexa.employees where "EmployeeCode" like 'SESS-%' and lower("Status")='active') total_active_sess_employee_count,
    (select count(*) from nexa.department_approval_mappings where "ApprovalRouteCode"='MANAGER' and "IsActive"=true) total_active_manager_mapping_count,
    (select count(*) from nexa.purchase_requisitions pr join nexa.departments d on d."Id"=pr."RequestingDepartmentId" where d."Code" in ($legacy) and pr."IsActive"=true and pr."Status" not in ('Completed','Rejected','Cancelled')) active_open_pr_legacy_department_reference_count,
    (select count(*) from nexa.purchase_requisitions pr join nexa.departments d on d."Id"=pr."RequestingDepartmentId" where d."Code" in ($legacy) and not (pr."IsActive"=true and pr."Status" not in ('Completed','Rejected','Cancelled'))) historical_pr_legacy_department_reference_count
)
select 'corrective_migration_count='||corrective_migration_count from states
union all select 'backup_row_count='||backup_row_count from states
union all select 'active_clean_department_count='||active_clean_department_count from states
union all select 'missing_clean_department_count='||missing_clean_department_count from states
union all select 'unexpected_active_department_count='||unexpected_active_department_count from states
union all select 'active_legacy_department_count='||active_legacy_department_count from states
union all select 'inactive_legacy_department_count='||inactive_legacy_department_count from states
union all select 'corrected_legacy_department_count='||corrected_legacy_department_count from states
union all select 'active_employee_legacy_department_reference_count='||active_employee_legacy_department_reference_count from states
union all select 'active_manager_mapping_legacy_department_reference_count='||active_manager_mapping_legacy_department_reference_count from states
union all select 'total_active_sess_employee_count='||total_active_sess_employee_count from states
union all select 'total_active_manager_mapping_count='||total_active_manager_mapping_count from states
union all select 'active_open_pr_legacy_department_reference_count='||active_open_pr_legacy_department_reference_count from states
union all select 'historical_pr_legacy_department_reference_count='||historical_pr_legacy_department_reference_count from states
union all select 'database_acceptance_state='||case when corrective_migration_count=1 and backup_row_count=4 and active_clean_department_count=12 and missing_clean_department_count=0 and unexpected_active_department_count=0 and active_legacy_department_count=0 and inactive_legacy_department_count=4 and corrected_legacy_department_count=4 and active_employee_legacy_department_reference_count=0 and active_manager_mapping_legacy_department_reference_count=0 and total_active_sess_employee_count=42 and total_active_manager_mapping_count=14 and active_open_pr_legacy_department_reference_count=0 then 'PASS' else 'FAIL' end from states;
commit;
"@ }
function Assert-ReadOnlySql([string]$Sql) { $withoutStrings=[regex]::Replace($Sql,"'([^']|'')*'","''"); $withoutQuoted=[regex]::Replace($withoutStrings,'"(""|[^"])*"','""'); if($withoutQuoted -match '(?i)\b(insert|update|delete|merge|create|alter|drop|truncate|grant|revoke|copy|call|do|vacuum|analyze|reindex)\b'){throw 'Verification SQL contains a prohibited write token.'}; if($Sql -notmatch '(?is)^\s*begin\s+transaction\s+read\s+only;.*commit;\s*$'){throw 'Verification SQL must use a read-only transaction.'} }
function Invoke-PsqlReadOnly([string]$Sql) { Assert-ReadOnlySql $Sql; $script:tempSqlFile=Join-Path ([IO.Path]::GetTempPath()) ('rev868c3_legacy_dept_'+[guid]::NewGuid().ToString('N')+'.sql'); Set-Content -LiteralPath $script:tempSqlFile -Value $Sql -Encoding UTF8; $output=@(& $script:psqlExe -h $HostName -p $Port -U $UserName -d $Database -v ON_ERROR_STOP=1 -t -A -f $script:tempSqlFile 2>&1); if($LASTEXITCODE -ne 0){throw "Read-only psql verification failed with exit code $LASTEXITCODE."}; ($output -join "`n") }
function Resolve-DotnetEfDll([string]$Path) { if(-not $Path){throw 'Full apply requires -DotnetEfPath. No migration was attempted.'}; $resolved=Resolve-File $Path 'dotnet-ef.dll'; if([IO.Path]::GetFileName($resolved) -ne 'dotnet-ef.dll'){throw 'Only dotnet-ef.dll is accepted.'}; $approved=(Resolve-Path (Join-Path $env:USERPROFILE '.nuget\packages\dotnet-ef')).Path.TrimEnd('\')+'\'; if(-not $resolved.StartsWith($approved,[StringComparison]::OrdinalIgnoreCase)){throw 'dotnet-ef.dll is outside the approved NuGet package root.'}; $resolved }
function Invoke-CorrectiveMigration { $args=@('database','update',$MigrationName,'--project',$InfrastructureProject,'--startup-project',$StartupProject,'--context','NexaErpDbContext','--framework','net10.0','--configuration','Release'); $output=@(& $script:dotnetExe exec $script:dotnetEfDll @args 2>&1); if($LASTEXITCODE -ne 0){throw "REV868C3 corrective EF update failed with exit code $LASTEXITCODE. Output suppressed."} }
function Write-Plan {
    Write-Host 'REV868C3 corrective GeneratePlanOnly'
    Write-Host "Expected host: $HostName"
    Write-Host "Expected port: $Port"
    Write-Host "Isolated target database: $ExpectedDatabase"
    Write-Host "Protected databases: $($RejectedDatabases -join ', '), all REV861-like names"
    Write-Host "Migration: $MigrationName"
    Write-Host "Required first ten migrations: $($RequiredMigrations -join ', ')"
    Write-Host "Legacy departments: $($LegacyCodes -join ', ')"
    Write-Host 'Only nexa.departments is updated; the migration-owned backup table preserves exact IsActive and audit values.'
    Write-Host 'Historical PR legacy-department references are reported separately and are never deleted or rewritten.'
    Write-Host 'No employee, history, PR, approval, manager mapping, or audit record is deleted or rewritten.'
    Write-Host 'No main-DB, database create/drop, backup, restore, or database cleanup operation is permitted or performed.'
    Write-Host 'Preflight SQL:'
    Write-Host (Get-PreflightSql)
    Write-Host 'Post-verification SQL:'
    Write-Host (Get-PostVerificationSql)
    Write-Host 'Rollback restores exact backed-up IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, and Version values.'
}

Assert-TargetDatabaseName $Database
if($GeneratePlanOnly){Write-Plan;return}
try {
    $script:psqlExe=Resolve-File $psqlPath 'psql.exe'
    $securePassword=Read-Host -AsSecureString 'Enter PostgreSQL password for isolated REV868C3 verification database only'
    $bstr=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    try{$plainPassword=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)}finally{if($bstr -ne [IntPtr]::Zero){[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)}}
    $env:PGPASSWORD=$plainPassword
    $env:ConnectionStrings__NexaErp="Host=$HostName;Port=$Port;Database=$Database;Username=$UserName;Password=$plainPassword"
    $env:NexaErp__ExpectedDatabase=$Database
    $identity=Invoke-PsqlReadOnly "begin transaction read only; select 'database_identity='||current_database(); commit;"
    $identityLines=@($identity -split "`r?`n"|Where-Object{$_ -like 'database_identity=*'})
    if($identityLines.Count -ne 1){throw 'Database identity evidence missing or duplicated.'}
    Assert-TargetDatabaseName ($identityLines[0].Substring('database_identity='.Length))
    $preflight=Invoke-PsqlReadOnly (Get-PreflightSql)
    if($preflight -notmatch '(?m)^safe_retry_state=PASS$' -or $preflight -notmatch '(?m)^preflight_acceptance_state=PASS$'){throw "REV868C3 corrective preflight failed closed.`n$preflight"}
    if($PreflightOnly){Write-Host $preflight;return}
    $script:dotnetExe=Resolve-File $dotnetPath 'dotnet.exe'
    $script:dotnetEfDll=Resolve-DotnetEfDll $DotnetEfPath
    Invoke-CorrectiveMigration
    $post=Invoke-PsqlReadOnly (Get-PostVerificationSql)
    if($post -notmatch '(?m)^database_acceptance_state=PASS$'){throw "REV868C3 corrective post-verification failed closed.`n$post"}
    New-Item -ItemType Directory -Force -Path $evidenceDir | Out-Null
    $report=Join-Path $evidenceDir ('rev868c3_legacy_department_correction_'+(Get-Date -Format 'yyyyMMdd_HHmmss')+'.md')
    $historicalPrEvidence=@(($preflight -split "`r?`n")+($post -split "`r?`n")|Where-Object{$_ -like 'historical_pr_legacy_department_reference_count=*'})
    @('# REV868C3 Legacy Department Corrective Verification','','```text',$preflight,$post,'```','','## Historical PR legacy-department references (read-only; retained)','','```text',($historicalPrEvidence -join "`n"),'```','',"migration=$MigrationName",'database_acceptance_state=PASS') | Set-Content -LiteralPath $report -Encoding UTF8
    Write-Host "REV868C3 corrective report: $report"
}
finally {
    if($script:tempSqlFile -and (Test-Path -LiteralPath $script:tempSqlFile)){Remove-Item -LiteralPath $script:tempSqlFile -Force -ErrorAction SilentlyContinue}
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:\ConnectionStrings__NexaErp -ErrorAction SilentlyContinue
    Remove-Item Env:\NexaErp__ExpectedDatabase -ErrorAction SilentlyContinue
    $plainPassword=$null
    if($securePassword){$securePassword.Dispose()}
}
