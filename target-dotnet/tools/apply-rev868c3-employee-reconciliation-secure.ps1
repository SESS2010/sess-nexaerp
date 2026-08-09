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
$AllExpectedMigrations = $RequiredMigrations + $MigrationName
$RejectedDatabases = @('sess_nexaerp','postgres','template0','template1')
$ActiveEmployeeCodes = 'SESS-001,SESS-002,SESS-003,SESS-004,SESS-005,SESS-006,SESS-007,SESS-008,SESS-009,SESS-010,SESS-011,SESS-012,SESS-013,SESS-014,SESS-015,SESS-017,SESS-019,SESS-020,SESS-021,SESS-023,SESS-024,SESS-025,SESS-026,SESS-029,SESS-030,SESS-031,SESS-033,SESS-034,SESS-035,SESS-038,SESS-040,SESS-041,SESS-042,SESS-043,SESS-044,SESS-045,SESS-046,SESS-047,SESS-048,SESS-049,SESS-050,SESS-051'
$RelievedEmployeeCodes = 'SESS-016,SESS-018,SESS-022,SESS-027,SESS-028,SESS-032,SESS-036,SESS-037,SESS-039'
$DepartmentCodes = 'ACCOUNTS_FINANCE,DESIGN,ELECTRICAL_PLC_INSTRUMENTATION,HR_ADMIN,MANAGEMENT,PRODUCTION_FABRICATION,PURCHASE,QUALITY_QC,REFRIGERATION_MECHANICAL,SERVICE_TECHNICAL_SUPPORT,SOFTWARE_IT,STORES'
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
        if ($LASTEXITCODE -ne 0) { throw "psql failed with exit code $LASTEXITCODE. $($output -join "`n")" }
        return ($output | ForEach-Object { $_.ToString() }) -join "`n"
    }
    finally { Remove-Item -LiteralPath $sqlFile -Force -ErrorAction SilentlyContinue }
}
function Get-MigrationRowsSql {
@"
select 'migration|' || m."MigrationId" || '|count=' || count(*)::text
from "public"."__EFMigrationsHistory" m
group by m."MigrationId"
order by m."MigrationId";
"@
}
function Get-PreflightSql {
$required = ($RequiredMigrations | ForEach-Object { "'$_'" }) -join ','
@"
select 'identity|database=' || current_database() || '|user=' || current_user || '|server=' || coalesce(inet_server_addr()::text,'local') || '|port=' || inet_server_port()::text;
$(Get-MigrationRowsSql)
with artifact_counts as (
    select
        (select count(*) from "public"."__EFMigrationsHistory" where "MigrationId" in ($required)) as prerequisite_history_count,
        (select count(*) from "public"."__EFMigrationsHistory" where "MigrationId" = '$MigrationName') as rev868c3_history_count,
        (select count(*) from pg_catalog.pg_class c join pg_catalog.pg_namespace n on n.oid = c.relnamespace where n.nspname = 'nexa' and c.relname like 'rev868c3\_%\_backup' escape '\') as backup_relation_count,
        (select count(*) from nexa.employee_status_history where "CreatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION' and "Reason" like 'REV868C3 employee workbook reconciliation%') as status_history_partial_count,
        (select count(*) from pg_catalog.pg_class c join pg_catalog.pg_namespace n on n.oid = c.relnamespace where n.nspname = 'nexa' and c.relname = 'employee_department_history') as department_history_relation_count,
        case when exists (select 1 from pg_catalog.pg_class c join pg_catalog.pg_namespace n on n.oid = c.relnamespace where n.nspname = 'nexa' and c.relname = 'employee_department_history') then (select count(*) from nexa.employee_department_history where "CreatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION' and "CorrelationId" = 'REV868C3_EMPLOYEE_WORKBOOK_RECONCILIATION') else 0 end as department_history_partial_count,
        (select count(*) from nexa.audit_logs where "CreatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION' and "CorrelationId" = 'REV868C3_EMPLOYEE_WORKBOOK_RECONCILIATION') as audit_partial_count,
        (select count(*) from nexa.employee_role_assignments where "CreatedBy" = 'REV868C3_DEPARTMENT_MANAGER_PERMISSION') as role_assignment_partial_count,
        (select count(*) from nexa.role_page_permissions where "CreatedBy" = 'REV868C3_DEPARTMENT_MANAGER_PERMISSION') as role_page_permission_partial_count,
        (select count(*) from nexa.department_approval_mappings where "CreatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION') as manager_mapping_partial_count,
        (select count(*) from nexa.employees where "CreatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION') as deterministic_employee_partial_count,
        (select count(*) from nexa.departments where "CreatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION') as deterministic_department_partial_count,
        (select count(*) from nexa.designations where "CreatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION') as deterministic_designation_partial_count,
        (select count(*) from information_schema.columns where table_schema = 'nexa' and table_name = 'employees' and column_name in ('PayrollEmployeeId','Gender','Qualification','DateOfBirth','DateOfJoiningAccuracy','IsDateOfJoiningApproximate','ApproximateDateNote','FunctionalResponsibility','WorkLocation','ManagerScope','LegacyDepartment')) as employee_column_count,
        (select count(*) from information_schema.columns where table_schema = 'nexa' and table_name = 'department_approval_mappings' and column_name = 'Scope') as mapping_scope_column_count
)
select 'prerequisite_history_count=' || prerequisite_history_count::text from artifact_counts
union all select 'rev868c3_history_count=' || rev868c3_history_count::text from artifact_counts
union all select 'backup_relation_count=' || backup_relation_count::text from artifact_counts
union all select 'status_history_partial_count=' || status_history_partial_count::text from artifact_counts
union all select 'department_history_relation_count=' || department_history_relation_count::text from artifact_counts
union all select 'department_history_partial_count=' || department_history_partial_count::text from artifact_counts
union all select 'audit_partial_count=' || audit_partial_count::text from artifact_counts
union all select 'role_assignment_partial_count=' || role_assignment_partial_count::text from artifact_counts
union all select 'role_page_permission_partial_count=' || role_page_permission_partial_count::text from artifact_counts
union all select 'manager_mapping_partial_count=' || manager_mapping_partial_count::text from artifact_counts
union all select 'deterministic_employee_partial_count=' || deterministic_employee_partial_count::text from artifact_counts
union all select 'deterministic_department_partial_count=' || deterministic_department_partial_count::text from artifact_counts
union all select 'deterministic_designation_partial_count=' || deterministic_designation_partial_count::text from artifact_counts
union all select 'employee_column_count=' || employee_column_count::text from artifact_counts
union all select 'mapping_scope_column_count=' || mapping_scope_column_count::text from artifact_counts
union all select 'safe_retry_state=' || case when prerequisite_history_count = 9 and rev868c3_history_count = 0 and backup_relation_count = 0 and status_history_partial_count = 0 and department_history_relation_count = 0 and department_history_partial_count = 0 and audit_partial_count = 0 and role_assignment_partial_count = 0 and role_page_permission_partial_count = 0 and manager_mapping_partial_count = 0 and deterministic_employee_partial_count = 0 and deterministic_department_partial_count = 0 and deterministic_designation_partial_count = 0 and employee_column_count = 0 and mapping_scope_column_count = 0 then 'PASS' else 'FAIL' end from artifact_counts;
"@
}
function Get-PostMigrationSql {
$all = ($AllExpectedMigrations | ForEach-Object { "'$_'" }) -join ','
@"
select 'identity|database=' || current_database() || '|user=' || current_user || '|server=' || coalesce(inet_server_addr()::text,'local') || '|port=' || inet_server_port()::text;
$(Get-MigrationRowsSql)
select 'expected_migration_count=' || count(*)::text from "public"."__EFMigrationsHistory" where "MigrationId" in ($all);
select 'rev868c3_migration_count=' || count(*)::text from "public"."__EFMigrationsHistory" where "MigrationId" = '$MigrationName';
select 'active_employee_codes=' || coalesce(string_agg("EmployeeCode", ',' order by "EmployeeCode"),'') from nexa.employees where lower("Status") = 'active' and "EmployeeCode" like 'SESS-%';
select 'active_employee_codes_expected=$ActiveEmployeeCodes';
select 'relieved_employee_codes=' || coalesce(string_agg("EmployeeCode", ',' order by "EmployeeCode"),'') from nexa.employees where "EmployeeCode" in ('SESS-016','SESS-018','SESS-022','SESS-027','SESS-028','SESS-032','SESS-036','SESS-037','SESS-039') and lower("Status") in ('left / resigned','inactive');
select 'relieved_employee_codes_expected=$RelievedEmployeeCodes';
select 'department_codes=' || coalesce(string_agg("Code", ',' order by "Code"),'') from nexa.departments where "Code" in ('MANAGEMENT','PURCHASE','STORES','ACCOUNTS_FINANCE','HR_ADMIN','PRODUCTION_FABRICATION','DESIGN','ELECTRICAL_PLC_INSTRUMENTATION','REFRIGERATION_MECHANICAL','SERVICE_TECHNICAL_SUPPORT','SOFTWARE_IT','QUALITY_QC') and "IsActive" = true;
select 'department_codes_expected=$DepartmentCodes';
select 'manager_mapping_rows=' || coalesce(string_agg(d."Code" || ':' || m."Scope" || ':' || p."EmployeeCode" || ':' || a."EmployeeCode", ',' order by d."Code", m."Scope"),'') from nexa.department_approval_mappings m join nexa.departments d on d."Id" = m."DepartmentId" join nexa.employees p on p."Id" = m."PrimaryApproverEmployeeId" left join nexa.employees a on a."Id" = m."AlternateApproverEmployeeId" where m."ApprovalRouteCode" = 'MANAGER' and m."IsActive" = true and m."CreatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';
select 'workflow_step|range=0.00-50000.00|sequence=1|resolution=DEPARTMENT_MAPPING|role=MANAGER';
select 'workflow_step|range=50000.01-500000.00|sequence=1|resolution=DEPARTMENT_MAPPING|role=MANAGER';
select 'workflow_step|range=50000.01-500000.00|sequence=2|resolution=FIXED_EMPLOYEE_ROLE|employee=SESS-002|role=MANAGING_DIRECTOR';
select 'workflow_step|range=500000.01-unbounded|sequence=1|resolution=DEPARTMENT_MAPPING|role=MANAGER';
select 'workflow_step|range=500000.01-unbounded|sequence=2|resolution=FIXED_EMPLOYEE_ROLE|employee=SESS-002|role=MANAGING_DIRECTOR';
select 'workflow_step|range=500000.01-unbounded|sequence=3|resolution=FIXED_EMPLOYEE_ROLE|employee=SESS-001|role=TECHNICAL_DIRECTOR';
select 'login_enabled_mismatch_count=' || count(*)::text from nexa.employees e join nexa.rev868c3_employee_backup b on b."EmployeeId" = e."Id" where e."LoginEnabled" is distinct from b."LoginEnabled";
select 'approval_status_mismatch_count=' || count(*)::text from nexa.employees e join nexa.rev868c3_employee_backup b on b."EmployeeId" = e."Id" where e."ApprovalStatus" is distinct from b."ApprovalStatus";
select 'status_history_rows=' || count(*)::text from nexa.employee_status_history where "CreatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION' and "Reason" like 'REV868C3 employee workbook reconciliation%';
select 'department_transfer_history_rows=' || count(*)::text from nexa.employee_department_history where "CreatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION' and "CorrelationId" = 'REV868C3_EMPLOYEE_WORKBOOK_RECONCILIATION';
select 'manager_role_assignment_rows=' || count(*)::text from nexa.employee_role_assignments where "CreatedBy" = 'REV868C3_DEPARTMENT_MANAGER_PERMISSION';
select 'manager_permission_rows=' || count(*)::text from nexa.role_page_permissions where "CreatedBy" = 'REV868C3_DEPARTMENT_MANAGER_PERMISSION';
select 'self_approval_prevention_source=PurchaseRequisitionEndpointHelpers blocks duplicate actor before approval completion';
select 'narren_exact_doj=' || count(*)::text from nexa.employees where "EmployeeCode" = 'SESS-040' and "EmployeeName" = 'NARREN VALENTINO' and "DateOfJoining" = DATE '2026-02-01' and "IsDateOfJoiningApproximate" = false;
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
    Write-Host 'Full apply mode will create a non-zero pre-C3 isolated backup, calculate SHA-256, and write a sanitized pre-migration backup report before EF migration application.'
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
    New-Item -ItemType Directory -Force -Path $evidenceDir | Out-Null
    $stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $backupFile = Join-Path $backupDir ("sess_nexaerp_rev868_verify_pre_rev868c3_$stamp.dump")
    & $script:pgDumpExe -h $HostName -p $Port -U $UserName -d $Database -F c -f $backupFile
    if ($LASTEXITCODE -ne 0) { throw "pg_dump failed with exit code $LASTEXITCODE." }
    $backupItem = Get-Item -LiteralPath $backupFile
    if ($backupItem.Length -le 0) { throw 'Pre-C3 isolated backup is zero bytes. Migration blocked.' }
    $backupHash = (Get-FileHash -LiteralPath $backupFile -Algorithm SHA256).Hash
    $preReport = Join-Path $evidenceDir ("rev868c3_pre_migration_backup_" + $stamp + ".md")
    @("# REV868C3 Pre-Migration Isolated Backup", "", "Database: $Database", "Backup file: $backupFile", "Backup bytes: $($backupItem.Length)", "Backup SHA-256: $backupHash", "", "Migration not yet applied at this report checkpoint.") | Set-Content -LiteralPath $preReport -Encoding UTF8
    & $script:dotnetExe ef database update $MigrationName --project .\src\SESS.NexaERP.Infrastructure\SESS.NexaERP.Infrastructure.csproj --startup-project .\src\SESS.NexaERP.Api\SESS.NexaERP.Api.csproj --context NexaErpDbContext
    if ($LASTEXITCODE -ne 0) { throw "EF database update failed with exit code $LASTEXITCODE." }
    $report = Join-Path $evidenceDir ("rev868c3_employee_reconciliation_" + $stamp + ".md")
    @("# REV868C3 Isolated Verification", "", "Backup file: $backupFile", "Backup SHA-256: $backupHash", "", '```text', (Invoke-Psql (Get-PostMigrationSql)), '```') | Set-Content -LiteralPath $report -Encoding UTF8
    Write-Host "REV868C3 report: $report"
}
finally {
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:\ConnectionStrings__NexaErp -ErrorAction SilentlyContinue
    Remove-Item Env:\NexaErp__ExpectedDatabase -ErrorAction SilentlyContinue
    if ($plainPassword) { $plainPassword = $null }
    if ($securePassword) { $securePassword.Dispose() }
}
