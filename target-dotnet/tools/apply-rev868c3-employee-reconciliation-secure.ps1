param(
    [string]$GitPath,
    [switch]$GeneratePlanOnly,
    [switch]$PreflightOnly,
    [switch]$ResumeVerifyOnly
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$HostName = 'localhost'
$Port = 5432
$Database = 'sess_nexaerp_rev868_verify'
$UserName = 'postgres'
$MigrationName = '20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation'
$RequiredMigrations = @(
    '20260808110924_Phase1Foundation',
    '20260808114550_Phase1AuthorizationSeed',
    '20260808123411_Rev866EmployeePermissionMatrix',
    '20260808142353_Rev866CorrectiveStatusPermissionAudit',
    '20260808151207_Rev867MasterFoundation',
    '20260808160435_Rev867C1Corrections',
    '20260808182945_Rev868PurchaseRequisitionFoundation',
    '20260808190920_Rev868PurchaseLocationAllocationCorrection',
    '20260809123000_Rev868C2DepartmentManagerApprovalMapping'
)
$RejectedDatabases = @('sess_nexaerp','postgres','template0','template1')
$pgBin = 'C:\Program Files\PostgreSQL\17\bin'
$psql = Join-Path $pgBin 'psql.exe'
$pgDump = Join-Path $pgBin 'pg_dump.exe'
$dotnet = Join-Path $root '..\.dotnet10\dotnet.exe'
$evidenceDir = Join-Path $root 'local-evidence\rev868c3'
$backupDir = Join-Path $root 'backups\postgresql\pre-rev868c3-isolated'
$plainPassword = $null
$securePassword = $null
function Assert-TargetDatabaseName([string]$Name) {
    if ($Name -ne $Database) { throw "REV868C3 helper is restricted to $Database. Actual: $Name" }
    if ($RejectedDatabases -contains $Name -or $Name -match '(?i)rev861') { throw "Protected database rejected: $Name" }
}
function Resolve-File([string]$Path, [string]$Label) { if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "$Label not found: $Path" }; (Resolve-Path -LiteralPath $Path).Path }
function Invoke-Psql([string]$Sql) {
    $sqlFile = Join-Path ([System.IO.Path]::GetTempPath()) ("sess_nexa_rev868c3_" + [Guid]::NewGuid().ToString('N') + '.sql')
    try {
        [System.IO.File]::WriteAllText($sqlFile, $Sql, [System.Text.UTF8Encoding]::new($false))
        $output = & $script:psqlExe -h $HostName -p $Port -U $UserName -d $Database -v ON_ERROR_STOP=1 -At -f $sqlFile 2>&1
        if ($LASTEXITCODE -ne 0) { throw "psql failed with exit code $LASTEXITCODE. $(($output | ForEach-Object { $_.ToString() }) -join "`n")" }
        return ($output | ForEach-Object { $_.ToString() }) -join "`n"
    }
    finally { Remove-Item -LiteralPath $sqlFile -Force -ErrorAction SilentlyContinue }
}
function Get-PreflightSql {
@"
select 'identity|database=' || current_database() || '|user=' || current_user || '|server=' || coalesce(inet_server_addr()::text,'local') || '|port=' || inet_server_port()::text;
select 'migration|' || m."MigrationId" || '|count=' || count(*)::text
from "public"."__EFMigrationsHistory" m
group by m."MigrationId"
order by m."MigrationId";
with artifact_counts as (
    select
        (select count(*) from "public"."__EFMigrationsHistory" where "MigrationId" = '$MigrationName') as rev868c3_history_count,
        (select count(*) from pg_catalog.pg_class c join pg_catalog.pg_namespace n on n.oid = c.relnamespace where n.nspname = 'nexa' and c.relname in ('rev868c3_employee_backup','rev868c3_department_backup','rev868c3_department_mapping_backup')) as backup_table_count,
        (select count(*) from information_schema.columns where table_schema = 'nexa' and table_name = 'employees' and column_name in ('PayrollEmployeeId','Gender','Qualification','DateOfBirth','DateOfJoiningAccuracy','IsDateOfJoiningApproximate','ApproximateDateNote','FunctionalResponsibility','WorkLocation','ManagerScope','LegacyDepartment')) as employee_column_count,
        (select count(*) from information_schema.columns where table_schema = 'nexa' and table_name = 'department_approval_mappings' and column_name = 'Scope') as mapping_scope_column_count
)
select 'rev868c3_history_count=' || rev868c3_history_count::text from artifact_counts
union all select 'backup_table_count=' || backup_table_count::text from artifact_counts
union all select 'employee_column_count=' || employee_column_count::text from artifact_counts
union all select 'mapping_scope_column_count=' || mapping_scope_column_count::text from artifact_counts
union all select 'safe_retry_state=' || case when rev868c3_history_count = 0 and backup_table_count = 0 and employee_column_count = 0 and mapping_scope_column_count = 0 then 'PASS' else 'FAIL' end from artifact_counts;
"@
}
function Get-PostMigrationSql {
@"
select 'identity|database=' || current_database() || '|user=' || current_user || '|server=' || coalesce(inet_server_addr()::text,'local') || '|port=' || inet_server_port()::text;
select 'rev868c3_migration_count=' || count(*)::text from "public"."__EFMigrationsHistory" where "MigrationId" = '$MigrationName';
select 'active_employee_count=' || count(*)::text from nexa.employees where "EmployeeCode" like 'SESS-%' and lower("Status") = 'active';
select 'relieved_history_count=' || count(*)::text from nexa.employees where "EmployeeCode" in ('SESS-016','SESS-018','SESS-022','SESS-027','SESS-028','SESS-032','SESS-036','SESS-037','SESS-039') and lower("Status") in ('left / resigned','inactive');
select 'clean_department_count=' || count(*)::text from nexa.departments where "Code" in ('MANAGEMENT','PURCHASE','STORES','ACCOUNTS_FINANCE','HR_ADMIN','PRODUCTION_FABRICATION','DESIGN','ELECTRICAL_PLC_INSTRUMENTATION','REFRIGERATION_MECHANICAL','SERVICE_TECHNICAL_SUPPORT','SOFTWARE_IT','QUALITY_QC') and "IsActive" = true;
select 'manager_mapping_count=' || count(*)::text from nexa.department_approval_mappings where "ApprovalRouteCode" = 'MANAGER' and "IsActive" = true and "CreatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
select 'new_employee_codes=' || string_agg("EmployeeCode", ',' order by "EmployeeCode") from nexa.employees where "EmployeeCode" between 'SESS-041' and 'SESS-051';
select 'duplicate_employee_codes=' || count(*)::text from (select "EmployeeCode" from nexa.employees group by "EmployeeCode" having count(*) > 1) d;
select 'duplicate_payroll_ids=' || count(*)::text from (select "PayrollEmployeeId" from nexa.employees where "PayrollEmployeeId" is not null group by "PayrollEmployeeId" having count(*) > 1) d;
select 'narren_approximate_doj=' || count(*)::text from nexa.employees where "EmployeeCode" = 'SESS-040' and "DateOfJoining" = DATE '2026-02-09' and "IsDateOfJoiningApproximate" = true;
select 'mageshwari_female=' || count(*)::text from nexa.employees where "EmployeeCode" = 'SESS-049' and "PayrollEmployeeId" = '1072' and "Gender" = 'Female';
select 'audit_evidence_count=' || count(*)::text from nexa.audit_logs where "CorrelationId" = 'REV868C3_EMPLOYEE_WORKBOOK_RECONCILIATION';
"@
}
function Write-Plan {
    Write-Host 'REV868C3 GeneratePlanOnly'
    Write-Host "Host: $HostName"
    Write-Host "Port: $Port"
    Write-Host "Target DB: $Database"
    Write-Host "Rejected DBs: $($RejectedDatabases -join ', '), REV861-like names"
    Write-Host "Prerequisite migrations: $($RequiredMigrations -join ', ')"
    Write-Host "Target migration: $MigrationName"
    Write-Host 'Pre-C3 isolated backup: required before full apply mode'
    Write-Host 'No main DB operation is permitted.'
    Write-Host 'Preflight SQL:'
    Write-Host (Get-PreflightSql)
    Write-Host 'Post-migration/resume SQL:'
    Write-Host (Get-PostMigrationSql)
}
if ($GeneratePlanOnly) { Write-Plan; return }
try {
    Assert-TargetDatabaseName $Database
    $script:psqlExe = Resolve-File $psql 'psql.exe'
    $script:pgDumpExe = Resolve-File $pgDump 'pg_dump.exe'
    $script:dotnetExe = Resolve-File $dotnet '.NET executable'
    $securePassword = Read-Host -AsSecureString 'Enter PostgreSQL password for isolated REV868C3 verification database only'
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    try { $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
    finally { if ($bstr -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) } }
    $env:PGPASSWORD = $plainPassword
    $env:ConnectionStrings__NexaErp = "Host=$HostName;Port=$Port;Database=$Database;Username=$UserName;Password=$plainPassword"
    $env:NexaErp__ExpectedDatabase = $Database
    Write-Host "Expected host: $HostName"
    Write-Host "Expected port: $Port"
    Write-Host "Expected database: $Database"
    Write-Host "PostgreSQL user parameter: $UserName"
    Write-Host "Target migration: $MigrationName"
    $identity = Invoke-Psql "select current_database();"
    Assert-TargetDatabaseName $identity.Trim()
    if ($PreflightOnly) { Write-Host (Invoke-Psql (Get-PreflightSql)); return }
    if ($ResumeVerifyOnly) { Write-Host (Invoke-Psql (Get-PostMigrationSql)); return }
    $preflight = Invoke-Psql (Get-PreflightSql)
    if ($preflight -notmatch 'safe_retry_state=PASS') { throw "REV868C3 safe retry preflight did not pass.`n$preflight" }
    New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
    $stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $backupFile = Join-Path $backupDir ("sess_nexaerp_rev868_verify_pre_rev868c3_$stamp.dump")
    & $script:pgDumpExe -h $HostName -p $Port -U $UserName -d $Database -F c -f $backupFile
    if ($LASTEXITCODE -ne 0) { throw "pg_dump failed with exit code $LASTEXITCODE." }
    & $script:dotnetExe ef database update $MigrationName --project .\src\SESS.NexaERP.Infrastructure\SESS.NexaERP.Infrastructure.csproj --startup-project .\src\SESS.NexaERP.Api\SESS.NexaERP.Api.csproj --context NexaErpDbContext
    if ($LASTEXITCODE -ne 0) { throw "EF database update failed with exit code $LASTEXITCODE." }
    New-Item -ItemType Directory -Force -Path $evidenceDir | Out-Null
    $report = Join-Path $evidenceDir ("rev868c3_employee_reconciliation_" + $stamp + ".md")
    @("# REV868C3 Isolated Verification", "", "Backup file: $backupFile", "", '```text', (Invoke-Psql (Get-PostMigrationSql)), '```') | Set-Content -LiteralPath $report -Encoding UTF8
    Write-Host "REV868C3 report: $report"
}
finally {
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:\ConnectionStrings__NexaErp -ErrorAction SilentlyContinue
    Remove-Item Env:\NexaErp__ExpectedDatabase -ErrorAction SilentlyContinue
    if ($plainPassword) { $plainPassword = $null }
    if ($securePassword) { $securePassword.Dispose() }
}
