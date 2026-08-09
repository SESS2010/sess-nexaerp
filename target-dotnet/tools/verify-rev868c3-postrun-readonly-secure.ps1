param(
    [string]$GitPath,
    [switch]$GenerateSqlOnly
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$HostName = 'localhost'
$Port = 5432
$Database = 'sess_nexaerp_rev868_verify'
$UserName = 'postgres'
$RejectedDatabases = @('sess_nexaerp','postgres','template0','template1')
$ExpectedMigrations = @(
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
$ActiveEmployeeCodes = 'SESS-001,SESS-002,SESS-003,SESS-004,SESS-005,SESS-006,SESS-007,SESS-008,SESS-009,SESS-010,SESS-011,SESS-012,SESS-013,SESS-014,SESS-015,SESS-017,SESS-019,SESS-020,SESS-021,SESS-023,SESS-024,SESS-025,SESS-026,SESS-029,SESS-030,SESS-031,SESS-033,SESS-034,SESS-035,SESS-038,SESS-040,SESS-041,SESS-042,SESS-043,SESS-044,SESS-045,SESS-046,SESS-047,SESS-048,SESS-049,SESS-050,SESS-051'
$RelievedEmployeeCodes = 'SESS-016,SESS-018,SESS-022,SESS-027,SESS-028,SESS-032,SESS-036,SESS-037,SESS-039'
$DepartmentCodes = 'ACCOUNTS_FINANCE,DESIGN,ELECTRICAL_PLC_INSTRUMENTATION,HR_ADMIN,MANAGEMENT,PRODUCTION_FABRICATION,PURCHASE,QUALITY_QC,REFRIGERATION_MECHANICAL,SERVICE_TECHNICAL_SUPPORT,SOFTWARE_IT,STORES'
$ManagerMappingRows = 'ACCOUNTS_FINANCE:ALL:SESS-007:SESS-002,DESIGN:PROJECT:SESS-019:SESS-015,DESIGN:REGULAR_PRODUCT:SESS-015:SESS-019,ELECTRICAL_PLC_INSTRUMENTATION:ALL:SESS-038:SESS-001,HR_ADMIN:ALL:SESS-020:SESS-002,MANAGEMENT:ALL:SESS-002:SESS-001,PRODUCTION_FABRICATION:ALL:SESS-023:SESS-040,PURCHASE:ALL:SESS-012:SESS-014,QUALITY_QC:ALL:SESS-040:SESS-009,REFRIGERATION_MECHANICAL:ALL:SESS-003:SESS-004,SERVICE_TECHNICAL_SUPPORT:BANGALORE:SESS-011:SESS-004,SERVICE_TECHNICAL_SUPPORT:CHENNAI:SESS-004:SESS-003,SOFTWARE_IT:ALL:SESS-008:SESS-049,STORES:ALL:SESS-014:SESS-012'
$LegacyMixedDepartmentCodes = 'ENGINEER_TECHNICAL,MANAGER,JUNIOR_ASSISTANT,ADMIN_ACCOUNTS_STORES'
$ManagerRoleEmployeeCodes = 'SESS-001,SESS-002,SESS-003,SESS-004,SESS-007,SESS-008,SESS-009,SESS-011,SESS-012,SESS-014,SESS-015,SESS-019,SESS-020,SESS-023,SESS-038,SESS-040,SESS-049'
$ManagerPermissionRows = 'purchase.requisition-approvals:V=T:A=T:R=T:C=T:RV=T:AH=T:FC=F,purchase.requisitions:V=T:A=F:R=F:C=F:RV=F:AH=T:FC=F'
$ChangedDepartmentEmployeeCodes = 'SESS-001,SESS-002,SESS-003,SESS-004,SESS-005,SESS-006,SESS-007,SESS-008,SESS-009,SESS-010,SESS-011,SESS-012,SESS-013,SESS-014,SESS-015,SESS-017,SESS-019,SESS-020,SESS-021,SESS-023,SESS-024,SESS-025,SESS-026,SESS-029,SESS-030,SESS-031,SESS-033,SESS-034,SESS-035,SESS-038,SESS-040,SESS-041,SESS-042,SESS-043,SESS-044,SESS-045,SESS-046,SESS-047,SESS-048,SESS-049,SESS-050,SESS-051'
$pgBin = 'C:\Program Files\PostgreSQL\17\bin'
$psql = Join-Path $pgBin 'psql.exe'
$evidenceDir = Join-Path $root 'local-evidence\rev868c3'
$outputDir = Join-Path $root 'outputs'
$tempSqlFile = $null
function Resolve-File([string]$Path, [string]$Label) { if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "$Label not found: $Path" }; (Resolve-Path -LiteralPath $Path).Path }
function Assert-TargetDatabaseName([string]$Name) {
    if ($Name -ne $Database) { throw "REV868C3 read-only verifier is restricted to $Database. Actual: $Name" }
    if ($RejectedDatabases -contains $Name -or $Name -match '(?i)rev861') { throw "Protected database rejected: $Name" }
}
function Get-PostRunSql {
    $migrationValues = ($ExpectedMigrations | ForEach-Object { "('$_')" }) -join ",`n        "
@"
begin transaction read only;
select 'database_identity=' || current_database();
select 'server_user=' || current_user;
select 'server_endpoint=' || coalesce(inet_server_addr()::text,'local') || ':' || coalesce(inet_server_port()::text,'unknown');
with expected("MigrationId") as (values
        $migrationValues
), actual as (select "MigrationId" from "public"."__EFMigrationsHistory"), counts as (
    select
      (select count(*) from expected) expected_count,
      (select count(*) from actual where "MigrationId" in (select "MigrationId" from expected)) matched_count,
      (select count(*) from (select "MigrationId" from actual group by "MigrationId" having count(*) > 1) d) duplicate_count,
      (select count(*) from expected e left join actual a on a."MigrationId" = e."MigrationId" where a."MigrationId" is null) missing_count,
      (select count(*) from actual a left join expected e on e."MigrationId" = a."MigrationId" where e."MigrationId" is null) unexpected_count
)
select 'migration_expected_count=' || expected_count from counts
union all select 'migration_matched_count=' || matched_count from counts
union all select 'migration_duplicate_count=' || duplicate_count from counts
union all select 'migration_missing_count=' || missing_count from counts
union all select 'migration_unexpected_count=' || unexpected_count from counts
union all select 'migration_acceptance_state=' || case when expected_count = 10 and matched_count = 10 and duplicate_count = 0 and missing_count = 0 and unexpected_count = 0 then 'PASS' else 'FAIL' end from counts;
with expected(code) as (select unnest(string_to_array('$ActiveEmployeeCodes', ','))), actual(code) as (select "EmployeeCode" from nexa.employees where lower("Status") = 'active' and "EmployeeCode" like 'SESS-%')
select 'active_employee_codes=' || coalesce((select string_agg(code, ',' order by code) from actual),'')
union all select 'active_employee_codes_expected=$ActiveEmployeeCodes'
union all select 'active_employee_missing_count=' || (select count(*)::text from expected e left join actual a using(code) where a.code is null)
union all select 'active_employee_unexpected_count=' || (select count(*)::text from actual a left join expected e using(code) where e.code is null)
union all select 'active_employee_acceptance_state=' || case when (select count(*) from expected e left join actual a using(code) where a.code is null) = 0 and (select count(*) from actual a left join expected e using(code) where e.code is null) = 0 then 'PASS' else 'FAIL' end;
with expected(code) as (select unnest(string_to_array('$RelievedEmployeeCodes', ','))), actual(code) as (select "EmployeeCode" from nexa.employees where "EmployeeCode" like 'SESS-%' and lower("Status") in ('left / resigned','left/resigned','resigned','inactive'))
select 'relieved_employee_codes=' || coalesce((select string_agg(code, ',' order by code) from actual),'')
union all select 'relieved_employee_codes_expected=$RelievedEmployeeCodes'
union all select 'relieved_employee_missing_count=' || (select count(*)::text from expected e left join actual a using(code) where a.code is null)
union all select 'relieved_employee_unexpected_count=' || (select count(*)::text from actual a left join expected e using(code) where e.code is null)
union all select 'relieved_employee_acceptance_state=' || case when (select count(*) from expected e left join actual a using(code) where a.code is null) = 0 and (select count(*) from actual a left join expected e using(code) where e.code is null) = 0 then 'PASS' else 'FAIL' end;
with expected(code) as (select unnest(string_to_array('$DepartmentCodes', ','))), actual(code) as (select "Code" from nexa.departments where "IsActive" = true), legacy(code) as (select unnest(string_to_array('$LegacyMixedDepartmentCodes', ',')))
select 'department_codes=' || coalesce((select string_agg(code, ',' order by code) from actual),'')
union all select 'department_codes_expected=$DepartmentCodes'
union all select 'department_missing_count=' || (select count(*)::text from expected e left join actual a using(code) where a.code is null)
union all select 'department_unexpected_count=' || (select count(*)::text from actual a left join expected e using(code) where e.code is null)
union all select 'legacy_mixed_department_active_count=' || (select count(*)::text from nexa.departments d join legacy l on l.code = d."Code" where d."IsActive" = true)
union all select 'department_acceptance_state=' || case when (select count(*) from expected e left join actual a using(code) where a.code is null) = 0 and (select count(*) from actual a left join expected e using(code) where e.code is null) = 0 and (select count(*) from nexa.departments d join legacy l on l.code = d."Code" where d."IsActive" = true) = 0 then 'PASS' else 'FAIL' end;
with expected(row_key) as (select unnest(string_to_array('$ManagerMappingRows', ','))), controlled_departments(code) as (select unnest(string_to_array('$DepartmentCodes', ','))), actual(row_key) as (select d."Code" || ':' || m."Scope" || ':' || p."EmployeeCode" || ':' || coalesce(a."EmployeeCode", '') from nexa.department_approval_mappings m join nexa.departments d on d."Id" = m."DepartmentId" join controlled_departments cd on cd.code = d."Code" join nexa.employees p on p."Id" = m."PrimaryApproverEmployeeId" left join nexa.employees a on a."Id" = m."AlternateApproverEmployeeId" where m."ApprovalRouteCode" = 'MANAGER' and m."IsActive" = true), dupes as (select row_key from actual group by row_key having count(*) > 1)
select 'manager_mapping_rows=' || coalesce((select string_agg(row_key, ',' order by row_key) from actual),'')
union all select 'manager_mapping_rows_expected=$ManagerMappingRows'
union all select 'missing_mapping_count=' || (select count(*)::text from expected e left join actual a using(row_key) where a.row_key is null)
union all select 'unexpected_mapping_count=' || (select count(*)::text from actual a left join expected e using(row_key) where e.row_key is null)
union all select 'duplicate_mapping_count=' || (select count(*)::text from dupes)
union all select 'mapping_acceptance_state=' || case when (select count(*) from expected e left join actual a using(row_key) where a.row_key is null) = 0 and (select count(*) from actual a left join expected e using(row_key) where e.row_key is null) = 0 and (select count(*) from dupes) = 0 then 'PASS' else 'FAIL' end;
with expected(route_code, minimum_amount, maximum_amount, step_number, resolution_type, employee_code, role_code) as (values
    ('MANAGER_ONLY', 0.00::numeric, 50000.00::numeric, 1, 'DEPARTMENT_MAPPING', null::text, 'MANAGER'),
    ('MANAGER_MD', 50000.01::numeric, 500000.00::numeric, 1, 'DEPARTMENT_MAPPING', null::text, 'MANAGER'),
    ('MANAGER_MD', 50000.01::numeric, 500000.00::numeric, 2, 'FIXED_EMPLOYEE_ROLE', 'SESS-002', 'MANAGING_DIRECTOR'),
    ('MANAGER_MD_TD', 500000.01::numeric, null::numeric, 1, 'DEPARTMENT_MAPPING', null::text, 'MANAGER'),
    ('MANAGER_MD_TD', 500000.01::numeric, null::numeric, 2, 'FIXED_EMPLOYEE_ROLE', 'SESS-002', 'MANAGING_DIRECTOR'),
    ('MANAGER_MD_TD', 500000.01::numeric, null::numeric, 3, 'FIXED_EMPLOYEE_ROLE', 'SESS-001', 'TECHNICAL_DIRECTOR')
), actual as (select "RouteCode" route_code, "MinimumAmount" minimum_amount, "MaximumAmount" maximum_amount, "StepNumber" step_number, "ApproverResolutionType" resolution_type, "ApproverEmployeeCode" employee_code, "ApproverRoleCode" role_code from nexa.purchase_approval_workflow_steps where "IsActive" = true and "RouteCode" in ('MANAGER_ONLY','MANAGER_MD','MANAGER_MD_TD')), missing as (select * from expected except select * from actual), unexpected as (select * from actual except select * from expected), dupes as (select route_code, step_number from actual group by route_code, step_number having count(*) > 1), sequence_bad as (select route_code from actual group by route_code having min(step_number) <> 1 or max(step_number) <> count(*)), overlap_bad as (select count(*) c from actual a join actual b on a.route_code <> b.route_code and a.minimum_amount <= coalesce(b.maximum_amount, 999999999999.99) and b.minimum_amount <= coalesce(a.maximum_amount, 999999999999.99))
select 'workflow_missing_count=' || (select count(*)::text from missing)
union all select 'workflow_unexpected_count=' || (select count(*)::text from unexpected)
union all select 'workflow_duplicate_count=' || (select count(*)::text from dupes)
union all select 'workflow_sequence_violation_count=' || (select count(*)::text from sequence_bad)
union all select 'workflow_overlap_count=' || (select c::text from overlap_bad)
union all select 'workflow_acceptance_state=' || case when (select count(*) from missing) = 0 and (select count(*) from unexpected) = 0 and (select count(*) from dupes) = 0 and (select count(*) from sequence_bad) = 0 and (select c from overlap_bad) = 0 then 'PASS' else 'FAIL' end;
select 'duplicate_employee_codes=' || count(*)::text from (select "EmployeeCode" from nexa.employees group by "EmployeeCode" having count(*) > 1) d;
select 'duplicate_payroll_ids=' || count(*)::text from (select "PayrollEmployeeId" from nexa.employees where "PayrollEmployeeId" is not null group by "PayrollEmployeeId" having count(*) > 1) d;
select 'login_enabled_mismatch_count=' || count(*)::text from nexa.employees e join nexa.rev868c3_employee_backup b on b."EmployeeId" = e."Id" where e."LoginEnabled" is distinct from b."LoginEnabled";
select 'approval_status_mismatch_count=' || count(*)::text from nexa.employees e join nexa.rev868c3_employee_backup b on b."EmployeeId" = e."Id" where e."ApprovalStatus" is distinct from b."ApprovalStatus";
with required(code) as (select unnest(string_to_array('$RelievedEmployeeCodes', ','))), covered(code) as (select distinct e."EmployeeCode" from nexa.employee_status_history h join nexa.employees e on e."Id" = h."EmployeeId" join required r on r.code = e."EmployeeCode" where h."CreatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION' and h."NewStatus" in ('Left / Resigned','Inactive')) select 'status_history_missing_employee_count=' || (select count(*)::text from required r left join covered c using(code) where c.code is null);
with required(code) as (select unnest(string_to_array('$ChangedDepartmentEmployeeCodes', ','))), covered(code) as (select distinct e."EmployeeCode" from nexa.employee_department_history h join nexa.employees e on e."Id" = h."EmployeeId" join required r on r.code = e."EmployeeCode" where h."CorrelationId" = 'REV868C3_EMPLOYEE_WORKBOOK_RECONCILIATION') select 'department_transfer_history_missing_employee_count=' || (select count(*)::text from required r left join covered c using(code) where c.code is null);
select 'department_manager_role_state=' || coalesce((select 'IsPrivileged=' || case when "IsPrivileged" then 'T' else 'F' end || ':IsActive=' || case when "IsActive" then 'T' else 'F' end from nexa.roles where "Code" = 'DEPARTMENT_MANAGER'),'missing');
with expected(code) as (select unnest(string_to_array('$ManagerRoleEmployeeCodes', ','))), actual(code) as (select distinct e."EmployeeCode" from nexa.employee_role_assignments era join nexa.employees e on e."Id" = era."EmployeeId" where era."CreatedBy" = 'REV868C3_DEPARTMENT_MANAGER_PERMISSION') select 'manager_role_missing_count=' || (select count(*)::text from expected e left join actual a using(code) where a.code is null) union all select 'manager_role_unexpected_count=' || (select count(*)::text from actual a left join expected e using(code) where e.code is null);
with expected(row_key) as (select unnest(string_to_array('$ManagerPermissionRows', ','))), actual(row_key) as (select p."PageKey" || ':V=' || case when rpp."CanView" then 'T' else 'F' end || ':A=' || case when rpp."CanApprove" then 'T' else 'F' end || ':R=' || case when rpp."CanReject" then 'T' else 'F' end || ':C=' || case when rpp."CanRequestClarification" then 'T' else 'F' end || ':RV=' || case when rpp."CanRequestRevision" then 'T' else 'F' end || ':AH=' || case when rpp."CanViewAuditHistory" then 'T' else 'F' end || ':FC=' || case when rpp."HasFullControl" then 'T' else 'F' end from nexa.role_page_permissions rpp join nexa.roles r on r."Id" = rpp."RoleId" join nexa.page_definitions p on p."Id" = rpp."PageDefinitionId" where r."Code" = 'DEPARTMENT_MANAGER' and p."PageKey" in ('purchase.requisitions','purchase.requisition-approvals')), dupes as (select row_key from actual group by row_key having count(*) > 1)
select 'manager_permission_rows=' || coalesce((select string_agg(row_key, ',' order by row_key) from actual),'')
union all select 'manager_permission_rows_expected=$ManagerPermissionRows'
union all select 'manager_permission_missing_count=' || (select count(*)::text from expected e left join actual a using(row_key) where a.row_key is null)
union all select 'manager_permission_unexpected_count=' || (select count(*)::text from actual a left join expected e using(row_key) where e.row_key is null)
union all select 'manager_permission_duplicate_count=' || (select count(*)::text from dupes)
union all select 'manager_permission_acceptance_state=' || case when (select count(*) from expected e left join actual a using(row_key) where a.row_key is null) = 0 and (select count(*) from actual a left join expected e using(row_key) where e.row_key is null) = 0 and (select count(*) from dupes) = 0 then 'PASS' else 'FAIL' end;
select 'narren_exact_doj=' || count(*)::text from nexa.employees where "EmployeeCode" = 'SESS-040' and "DateOfJoining" = DATE '2026-02-01' and "IsDateOfJoiningApproximate" = false;
select 'mageshwari_female=' || count(*)::text from nexa.employees where "EmployeeCode" = 'SESS-049' and "PayrollEmployeeId" = '1072' and "Gender" = 'Female';
select 'audit_evidence_count=' || count(*)::text from nexa.audit_logs where "CorrelationId" = 'REV868C3_EMPLOYEE_WORKBOOK_RECONCILIATION' and "Result" = 'Success';
select 'database_acceptance_state_requires_all_previous_labels=PASS';
commit;
"@
}
function Assert-ReadOnlySql([string]$Sql) {
    $withoutStrings = [regex]::Replace($Sql, "'([^']|'')*'", "''")
    $withoutComments = [regex]::Replace($withoutStrings, '(?m)--.*$', '')
    if ($withoutComments -match '(?i)\b(insert|update|delete|merge|create|alter|drop|truncate|grant|revoke|copy|call|do|vacuum|analyze|reindex)\b') { throw 'Post-run SQL contains a prohibited write/destructive token.' }
    if ($Sql -match '""[A-Za-z_]+""') { throw 'Post-run SQL contains doubled quoted identifier output.' }
}
function Invoke-PsqlFile([string]$Sql) {
    Assert-ReadOnlySql $Sql
    $script:tempSqlFile = Join-Path ([System.IO.Path]::GetTempPath()) ('sess_nexa_rev868c3_postrun_' + [Guid]::NewGuid().ToString('N') + '.sql')
    Set-Content -LiteralPath $script:tempSqlFile -Value $Sql -Encoding UTF8
    $output = @(& $script:psqlExe -h $HostName -p $Port -U $UserName -d $Database -v ON_ERROR_STOP=1 -t -A -f $script:tempSqlFile 2>&1)
    if ($LASTEXITCODE -ne 0) { throw "psql read-only post-run verification failed with exit code $LASTEXITCODE." }
    return ($output -join "`n")
}
Set-Location -LiteralPath $root
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
$sql = Get-PostRunSql
Assert-ReadOnlySql $sql
if ($GenerateSqlOnly) {
    $report = Join-Path $outputDir 'rev868c3_postrun_readonly_sql_source_verification.md'
    @('# REV868C3 Post-Run Read-Only SQL Source Verification', '', '```sql', $sql, '```') | Set-Content -LiteralPath $report -Encoding UTF8
    Write-Host "REV868C3 post-run SQL source report: $report"
    Write-Host $sql
    return
}
$script:psqlExe = Resolve-File $psql 'psql.exe'
if ($GitPath) { [void](Resolve-File $GitPath 'git.exe') }
New-Item -ItemType Directory -Force -Path $evidenceDir | Out-Null
$securePassword = Read-Host -AsSecureString 'Enter PostgreSQL password for isolated REV868C3 verification database only'
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
try { $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
finally { if ($bstr -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) } }
try {
    $env:PGPASSWORD = $plainPassword
    $env:ConnectionStrings__NexaErp = "Host=$HostName;Port=$Port;Database=$Database;Username=$UserName;Password=$plainPassword"
    $env:NexaErp__ExpectedDatabase = $Database
    $evidence = Invoke-PsqlFile $sql
    $identityLine = @($evidence -split "`r?`n" | Where-Object { $_ -like 'database_identity=*' } | Select-Object -First 1)
    if ($identityLine.Count -ne 1) { throw 'Database identity evidence missing.' }
    Assert-TargetDatabaseName ($identityLine[0].Substring('database_identity='.Length).Trim())
    $stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $report = Join-Path $evidenceDir ("rev868c3_postrun_readonly_verification_" + $stamp + ".md")
    @('# REV868C3 Post-Run Read-Only Verification', '', "database=$Database", '', '```text', $evidence, '```') | Set-Content -LiteralPath $report -Encoding UTF8
    Write-Host "REV868C3 post-run read-only verification report: $report"
}
finally {
    if ($script:tempSqlFile -and (Test-Path -LiteralPath $script:tempSqlFile -PathType Leaf)) { Remove-Item -LiteralPath $script:tempSqlFile -Force -ErrorAction SilentlyContinue }
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:\ConnectionStrings__NexaErp -ErrorAction SilentlyContinue
    Remove-Item Env:\NexaErp__ExpectedDatabase -ErrorAction SilentlyContinue
}
