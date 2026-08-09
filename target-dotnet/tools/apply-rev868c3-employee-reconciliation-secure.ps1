param(
    [string]$GitPath,
    [switch]$GeneratePlanOnly,
    [switch]$PreflightOnly,
    [switch]$ResumeVerifyOnly,
    [string]$DotnetEfPath
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
$ManagerMappingRows = 'ACCOUNTS_FINANCE:ALL:SESS-007:SESS-002,DESIGN:PROJECT:SESS-019:SESS-015,DESIGN:REGULAR_PRODUCT:SESS-015:SESS-019,ELECTRICAL_PLC_INSTRUMENTATION:ALL:SESS-038:SESS-001,HR_ADMIN:ALL:SESS-020:SESS-002,MANAGEMENT:ALL:SESS-002:SESS-001,PRODUCTION_FABRICATION:ALL:SESS-023:SESS-040,PURCHASE:ALL:SESS-012:SESS-014,QUALITY_QC:ALL:SESS-040:SESS-009,REFRIGERATION_MECHANICAL:ALL:SESS-003:SESS-004,SERVICE_TECHNICAL_SUPPORT:BANGALORE:SESS-011:SESS-004,SERVICE_TECHNICAL_SUPPORT:CHENNAI:SESS-004:SESS-003,SOFTWARE_IT:ALL:SESS-008:SESS-049,STORES:ALL:SESS-014:SESS-012'
$LegacyMixedDepartmentCodes = 'ENGINEER_TECHNICAL,MANAGER,JUNIOR_ASSISTANT,ADMIN_ACCOUNTS_STORES'
$ManagerRoleEmployeeCodes = 'SESS-001,SESS-002,SESS-003,SESS-004,SESS-007,SESS-008,SESS-009,SESS-011,SESS-012,SESS-014,SESS-015,SESS-019,SESS-020,SESS-023,SESS-038,SESS-040,SESS-049'
$ManagerPermissionRows = 'purchase.requisition-approvals:V=T:A=T:R=T:C=T:RV=T:AH=T:FC=F,purchase.requisitions:V=T:A=F:R=F:C=F:RV=F:AH=T:FC=F'
$ChangedDepartmentEmployeeCodes = 'SESS-001,SESS-002,SESS-003,SESS-004,SESS-005,SESS-006,SESS-007,SESS-008,SESS-009,SESS-010,SESS-011,SESS-012,SESS-013,SESS-014,SESS-015,SESS-017,SESS-019,SESS-020,SESS-021,SESS-023,SESS-024,SESS-025,SESS-026,SESS-029,SESS-030,SESS-031,SESS-033,SESS-034,SESS-035,SESS-038,SESS-040,SESS-041,SESS-042,SESS-043,SESS-044,SESS-045,SESS-046,SESS-047,SESS-048,SESS-049,SESS-050,SESS-051'
$TargetedTestNames = @(
    'Rev868c3_unauthenticated_request_returns_401',
    'Rev868c3_unauthorized_role_returns_403',
    'Rev868c3_creator_self_approval_returns_403',
    'Rev868c3_duplicate_approver_is_prevented',
    'Rev868c3_missing_department_manager_fails_closed',
    'Rev868c3_manager_md_td_approval_sequence_is_enforced'
)
$pgBin = 'C:\Program Files\PostgreSQL\17\bin'
$psql = Join-Path $pgBin 'psql.exe'
$pgDump = Join-Path $pgBin 'pg_dump.exe'
$dotnet = Join-Path $root '..\.dotnet10\dotnet.exe'
$InfrastructureProject = Join-Path $root 'src\SESS.NexaERP.Infrastructure\SESS.NexaERP.Infrastructure.csproj'
$StartupProject = Join-Path $root 'src\SESS.NexaERP.Api\SESS.NexaERP.Api.csproj'
$TargetFramework = 'net10.0'
$BuildConfiguration = 'Release'
$evidenceDir = Join-Path $root 'local-evidence\rev868c3'
$trxDir = Join-Path $evidenceDir 'test-results'
$backupDir = Join-Path $root 'backups\postgresql\pre-rev868c3-isolated'
$plainPassword = $null
$securePassword = $null
$script:dotnetEfInvocation = $null
function Assert-TargetDatabaseName([string]$Name) {
    if ($Name -ne $Database) { throw "REV868C3 helper is restricted to $Database. Actual: $Name" }
    if ($RejectedDatabases -contains $Name -or $Name -match '(?i)rev861') { throw "Protected database rejected: $Name" }
}
function Resolve-File([string]$Path, [string]$Label) { if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "$Label not found: $Path" }; (Resolve-Path -LiteralPath $Path).Path }
function Assert-SdkStyleProject([string]$ProjectPath, [string]$Label) {
    $resolved = Resolve-File $ProjectPath $Label
    $firstLine = (Get-Content -LiteralPath $resolved -TotalCount 1)
    if ($firstLine -notmatch '<Project\s+Sdk=') { throw "$Label is not an SDK-style project: $resolved" }
    return $resolved
}
function Get-EfProjectArgs {
    @(
        '--project', $script:InfrastructureProjectPath,
        '--startup-project', $script:StartupProjectPath,
        '--context', 'NexaErpDbContext',
        '--framework', $TargetFramework,
        '--configuration', $BuildConfiguration
    )
}
function Resolve-DotnetEfInvocation([string]$DotnetExe, [string]$ExplicitPath) {
    if ($ExplicitPath) {
        $resolved = Resolve-File $ExplicitPath 'dotnet-ef executable'
        $leaf = [System.IO.Path]::GetFileName($resolved)
        if ($leaf -eq 'dotnet-ef.dll') {
            $approvedPackageRoot = (Resolve-Path -LiteralPath (Join-Path $env:USERPROFILE '.nuget\packages\dotnet-ef') -ErrorAction Stop).Path.TrimEnd('\') + '\'
            if (-not $resolved.StartsWith($approvedPackageRoot, [StringComparison]::OrdinalIgnoreCase)) { throw "dotnet-ef.dll must be under approved NuGet package root: $approvedPackageRoot" }
            if ($resolved -match '\.\.($|[\\/])') { throw 'dotnet-ef.dll path traversal is rejected.' }
            return [pscustomobject]@{ Mode = 'DotnetExec'; Command = $resolved }
        }
        if ($leaf -eq 'dotnet-ef.exe') { return [pscustomobject]@{ Mode = 'Executable'; Command = $resolved } }
        throw "Invalid dotnet-ef executable name: $leaf"
    }

    $manifest = Join-Path $root '.config\dotnet-tools.json'
    if (Test-Path -LiteralPath $manifest -PathType Leaf) {
        $manifestText = Get-Content -LiteralPath $manifest -Raw
        if ($manifestText -match 'dotnet-ef') { return [pscustomobject]@{ Mode = 'ToolManifest'; Command = 'dotnet-ef' } }
    }

    $packageRoot = Join-Path $env:USERPROFILE '.nuget\packages\dotnet-ef'
    if (Test-Path -LiteralPath $packageRoot -PathType Container) {
        $dll = @(Get-ChildItem -LiteralPath $packageRoot -Recurse -Filter 'dotnet-ef.dll' -File -ErrorAction SilentlyContinue | Sort-Object FullName -Descending | Select-Object -First 1)
        if ($dll.Count -eq 1) { return [pscustomobject]@{ Mode = 'DotnetExec'; Command = $dll[0].FullName } }
    }

    $globalCandidates = @(Join-Path $env:USERPROFILE '.dotnet\tools\dotnet-ef.exe')
    foreach ($candidate in $globalCandidates) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) { return [pscustomobject]@{ Mode = 'Executable'; Command = (Resolve-Path -LiteralPath $candidate).Path } }
    }

    throw 'dotnet-ef tooling is unavailable. Install/restore the repository tool manifest or pass -DotnetEfPath to a valid dotnet-ef executable. No password was requested and no backup/migration was attempted.'
}
function Invoke-DotnetEfTool([string[]]$EfArgs) {
    if ($null -eq $script:dotnetEfInvocation) { throw 'dotnet-ef invocation has not been resolved.' }
    Push-Location -LiteralPath $root
    try {
        if ($script:dotnetEfInvocation.Mode -eq 'ToolManifest') { & $script:dotnetExe tool run dotnet-ef -- @EfArgs; return }
        if ($script:dotnetEfInvocation.Mode -eq 'DotnetExec') { & $script:dotnetExe exec $script:dotnetEfInvocation.Command @EfArgs; return }
        & $script:dotnetEfInvocation.Command @EfArgs
    }
    finally { Pop-Location }
}
function Test-DotnetEfTool {
    $output = @(Invoke-DotnetEfTool @('--version') 2>&1)
    if ($LASTEXITCODE -ne 0) { throw "dotnet-ef resolved but failed version check. $($output -join ' ')" }
    $versionText = ($output | ForEach-Object { $_.ToString() }) -join ' '
    if ($versionText -notmatch '\b10\.\d+\.\d+\b') { throw "dotnet-ef version is not compatible with EF Core 10. Output: $versionText" }
}
function Get-SanitizedEfFailure([object[]]$Output, [int]$ExitCode, [string]$Phase) {
    $raw = ($Output | ForEach-Object { $_.ToString() }) -join "`n"
    $rawBytes = [System.Text.Encoding]::UTF8.GetBytes($raw)
    $fingerprint = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($rawBytes))
    $exceptionType = if ($raw -match '(?m)^([A-Za-z0-9_.]+Exception)(:|\s|$)') { $Matches[1] } elseif ($raw -match '\b(PostgresException|NpgsqlException|DbUpdateException|InvalidOperationException)\b') { $Matches[1] } else { 'unknown' }
    $sqlState = if ($raw -match 'SQLSTATE\s*\[?([0-9A-Z]{5})\]?|PostgresException \(([0-9A-Z]{5})\)') { if ($Matches[1]) { $Matches[1] } else { $Matches[2] } } elseif ($raw -match '\b(23502|23503|23505|23514|42P01|42703|42P10)\b') { $Matches[1] } else { 'unknown' }
    $schema = if ($raw -match '(?i)schema\s+"([A-Za-z0-9_]+)"|SchemaName:\s*([A-Za-z0-9_]+)') { if ($Matches[1]) { $Matches[1] } else { $Matches[2] } } else { 'unknown' }
    $table = if ($raw -match '(?i)table\s+"([A-Za-z0-9_]+)"|TableName:\s*([A-Za-z0-9_]+)') { if ($Matches[1]) { $Matches[1] } else { $Matches[2] } } else { 'unknown' }
    $column = if ($raw -match '(?i)column\s+"([A-Za-z0-9_]+)"|ColumnName:\s*([A-Za-z0-9_]+)') { if ($Matches[1]) { $Matches[1] } else { $Matches[2] } } else { 'unknown' }
    $constraint = if ($raw -match '(?i)constraint\s+"([A-Za-z0-9_]+)"|ConstraintName:\s*([A-Za-z0-9_]+)') { if ($Matches[1]) { $Matches[1] } else { $Matches[2] } } else { 'unknown' }
    $messageCategory = if ($raw -match '(?i)no unique or exclusion constraint matching the ON CONFLICT specification|ON CONFLICT') { 'on_conflict_index_mismatch' } elseif ($raw -match '(?i)not-null|null value') { 'not_null' } elseif ($raw -match '(?i)foreign key') { 'foreign_key' } elseif ($raw -match '(?i)Unable to retrieve project metadata') { 'ef_project_metadata' } elseif ($raw -match '(?i)does not exist') { 'missing_object' } else { 'unclassified' }
    $category = switch ($sqlState) {
        '23502' { 'not_null_violation'; break }
        '23503' { 'foreign_key_violation'; break }
        '23505' { 'unique_violation'; break }
        '23514' { 'check_violation'; break }
        '42P01' { 'undefined_table'; break }
        '42703' { 'undefined_column'; break }
        '42P10' { 'on_conflict_index_mismatch'; break }
        default { if ($messageCategory -ne 'unclassified') { $messageCategory } else { 'ef_migration_failure' } }
    }
    "exit_code=$ExitCode; phase=$Phase; exception_type=$exceptionType; sqlstate=$sqlState; schema=$schema; table=$table; column=$column; constraint=$constraint; message_category=$messageCategory; category=$category; raw_output_sha256=$fingerprint"
}
function Invoke-EfDatabaseUpdateSanitized {
    $output = @(Invoke-DotnetEfTool (@('database','update',$MigrationName) + (Get-EfProjectArgs)) 2>&1)
    if ($LASTEXITCODE -ne 0) {
        $safe = Get-SanitizedEfFailure $output $LASTEXITCODE 'database_update_rev868c3'
        throw "EF database update failed. $safe"
    }
}
function Test-EfProjectMetadata {
    $previousConnection = $env:ConnectionStrings__NexaErp
    $previousExpected = $env:NexaErp__ExpectedDatabase
    try {
        $env:ConnectionStrings__NexaErp = "Host=127.0.0.1;Port=1;Database=$Database;Username=metadata_check"
        $env:NexaErp__ExpectedDatabase = $Database
        $args = @('migrations','list','--no-connect') + (Get-EfProjectArgs)
        $output = @(Invoke-DotnetEfTool $args 2>&1)
        if ($LASTEXITCODE -ne 0) { throw "EF project metadata/migration discovery failed before password prompt. $($output -join ' ')" }
        $text = ($output | ForEach-Object { $_.ToString() }) -join "`n"
        if ($text -notmatch [regex]::Escape($MigrationName)) { throw "EF migration discovery did not include $MigrationName." }
    }
    finally {
        if ($null -eq $previousConnection) { Remove-Item Env:\ConnectionStrings__NexaErp -ErrorAction SilentlyContinue } else { $env:ConnectionStrings__NexaErp = $previousConnection }
        if ($null -eq $previousExpected) { Remove-Item Env:\NexaErp__ExpectedDatabase -ErrorAction SilentlyContinue } else { $env:NexaErp__ExpectedDatabase = $previousExpected }
    }
}
function Find-ExistingValidPreC3Backup {
    if (-not (Test-Path -LiteralPath $backupDir -PathType Container)) { return $null }
    $items = @(Get-ChildItem -LiteralPath $backupDir -Filter 'sess_nexaerp_rev868_verify_pre_rev868c3_*.dump' -File -ErrorAction SilentlyContinue | Where-Object { $_.Length -gt 0 } | Sort-Object LastWriteTime -Descending)
    if ($items.Count -eq 0) { return $null }
    return $items[0]
}
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
        (select count(*) from pg_catalog.pg_class c join pg_catalog.pg_namespace n on n.oid = c.relnamespace where n.nspname = 'nexa' and c.relname = 'purchase_approval_workflow_steps') as workflow_step_relation_count,
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
union all select 'workflow_step_relation_count=' || workflow_step_relation_count::text from artifact_counts
union all select 'audit_partial_count=' || audit_partial_count::text from artifact_counts
union all select 'role_assignment_partial_count=' || role_assignment_partial_count::text from artifact_counts
union all select 'role_page_permission_partial_count=' || role_page_permission_partial_count::text from artifact_counts
union all select 'manager_mapping_partial_count=' || manager_mapping_partial_count::text from artifact_counts
union all select 'deterministic_employee_partial_count=' || deterministic_employee_partial_count::text from artifact_counts
union all select 'deterministic_department_partial_count=' || deterministic_department_partial_count::text from artifact_counts
union all select 'deterministic_designation_partial_count=' || deterministic_designation_partial_count::text from artifact_counts
union all select 'employee_column_count=' || employee_column_count::text from artifact_counts
union all select 'mapping_scope_column_count=' || mapping_scope_column_count::text from artifact_counts
union all select 'safe_retry_state=' || case when prerequisite_history_count = 9 and rev868c3_history_count = 0 and backup_relation_count = 0 and status_history_partial_count = 0 and department_history_relation_count = 0 and workflow_step_relation_count = 0 and audit_partial_count = 0 and role_assignment_partial_count = 0 and role_page_permission_partial_count = 0 and manager_mapping_partial_count = 0 and deterministic_employee_partial_count = 0 and deterministic_department_partial_count = 0 and deterministic_designation_partial_count = 0 and employee_column_count = 0 and mapping_scope_column_count = 0 then 'PASS' else 'FAIL' end from artifact_counts;
"@
}
function Get-PostMigrationSql {
$all = ($AllExpectedMigrations | ForEach-Object { "'$_'" }) -join ','
@"
select 'identity|database=' || current_database() || '|user=' || current_user || '|server=' || coalesce(inet_server_addr()::text,'local') || '|port=' || inet_server_port()::text;
$(Get-MigrationRowsSql)
select 'expected_migration_count=' || count(*)::text from "public"."__EFMigrationsHistory" where "MigrationId" in ($all);
select 'rev868c3_migration_count=' || count(*)::text from "public"."__EFMigrationsHistory" where "MigrationId" = '$MigrationName';
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
union all select 'unexpected_mapping_scope_count=' || (select count(*)::text from actual a left join expected e using(row_key) where e.row_key is null)
union all select 'unexpected_mapping_approver_count=' || (select count(*)::text from actual a left join expected e using(row_key) where e.row_key is null)
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
with expected(row_key) as (select unnest(string_to_array('$ManagerPermissionRows', ','))), actual(row_key) as (
    select p."PageKey" || ':V=' || case when rpp."CanView" then 'T' else 'F' end || ':A=' || case when rpp."CanApprove" then 'T' else 'F' end || ':R=' || case when rpp."CanReject" then 'T' else 'F' end || ':C=' || case when rpp."CanRequestClarification" then 'T' else 'F' end || ':RV=' || case when rpp."CanRequestRevision" then 'T' else 'F' end || ':AH=' || case when rpp."CanViewAuditHistory" then 'T' else 'F' end || ':FC=' || case when rpp."HasFullControl" then 'T' else 'F' end
    from nexa.role_page_permissions rpp
    join nexa.roles r on r."Id" = rpp."RoleId"
    join nexa.page_definitions p on p."Id" = rpp."PageDefinitionId"
    where r."Code" = 'DEPARTMENT_MANAGER'
      and p."PageKey" in ('purchase.requisitions','purchase.requisition-approvals')
), dupes as (select row_key from actual group by row_key having count(*) > 1)
select 'manager_permission_rows=' || coalesce((select string_agg(row_key, ',' order by row_key) from actual),'')
union all select 'manager_permission_rows_expected=$ManagerPermissionRows'
union all select 'manager_permission_missing_count=' || (select count(*)::text from expected e left join actual a using(row_key) where a.row_key is null)
union all select 'manager_permission_unexpected_count=' || (select count(*)::text from actual a left join expected e using(row_key) where e.row_key is null)
union all select 'manager_permission_duplicate_count=' || (select count(*)::text from dupes)
union all select 'manager_permission_acceptance_state=' || case when (select count(*) from expected e left join actual a using(row_key) where a.row_key is null) = 0 and (select count(*) from actual a left join expected e using(row_key) where e.row_key is null) = 0 and (select count(*) from dupes) = 0 then 'PASS' else 'FAIL' end;
select 'narren_exact_doj=' || count(*)::text from nexa.employees where "EmployeeCode" = 'SESS-040' and "EmployeeName" = 'NARREN VALENTINO' and "DateOfJoining" = DATE '2026-02-01' and "IsDateOfJoiningApproximate" = false;
select 'mageshwari_female=' || count(*)::text from nexa.employees where "EmployeeCode" = 'SESS-049' and "PayrollEmployeeId" = '1072' and "Gender" = 'Female';
select 'audit_evidence_count=' || count(*)::text from nexa.audit_logs where "CorrelationId" = 'REV868C3_EMPLOYEE_WORKBOOK_RECONCILIATION' and "Result" = 'Success';
with conditions as (
    select
      (select count(*) from "public"."__EFMigrationsHistory" where "MigrationId" in ($all)) = 10 as migrations_ok,
      (with expected(code) as (select unnest(string_to_array('$ActiveEmployeeCodes', ','))), actual(code) as (select "EmployeeCode" from nexa.employees where lower("Status") = 'active' and "EmployeeCode" like 'SESS-%') select (select count(*) from expected e left join actual a using(code) where a.code is null) = 0 and (select count(*) from actual a left join expected e using(code) where e.code is null) = 0) as active_employees_ok,
      (with expected(code) as (select unnest(string_to_array('$RelievedEmployeeCodes', ','))), actual(code) as (select "EmployeeCode" from nexa.employees where "EmployeeCode" like 'SESS-%' and lower("Status") in ('left / resigned','left/resigned','resigned','inactive')) select (select count(*) from expected e left join actual a using(code) where a.code is null) = 0 and (select count(*) from actual a left join expected e using(code) where e.code is null) = 0) as relieved_employees_ok,
      (with expected(code) as (select unnest(string_to_array('$DepartmentCodes', ','))), actual(code) as (select "Code" from nexa.departments where "IsActive" = true), legacy(code) as (select unnest(string_to_array('$LegacyMixedDepartmentCodes', ','))) select (select count(*) from expected e left join actual a using(code) where a.code is null) = 0 and (select count(*) from actual a left join expected e using(code) where e.code is null) = 0 and (select count(*) from nexa.departments d join legacy l on l.code = d."Code" where d."IsActive" = true) = 0) as departments_ok,
      (with expected(row_key) as (select unnest(string_to_array('$ManagerMappingRows', ','))), controlled_departments(code) as (select unnest(string_to_array('$DepartmentCodes', ','))), actual(row_key) as (select d."Code" || ':' || m."Scope" || ':' || p."EmployeeCode" || ':' || coalesce(a."EmployeeCode", '') from nexa.department_approval_mappings m join nexa.departments d on d."Id" = m."DepartmentId" join controlled_departments cd on cd.code = d."Code" join nexa.employees p on p."Id" = m."PrimaryApproverEmployeeId" left join nexa.employees a on a."Id" = m."AlternateApproverEmployeeId" where m."ApprovalRouteCode" = 'MANAGER' and m."IsActive" = true), dupes as (select row_key from actual group by row_key having count(*) > 1) select (select count(*) from expected e left join actual a using(row_key) where a.row_key is null) = 0 and (select count(*) from actual a left join expected e using(row_key) where e.row_key is null) = 0 and (select count(*) from dupes) = 0) as mappings_ok,
      (with expected(route_code, minimum_amount, maximum_amount, step_number, resolution_type, employee_code, role_code) as (values ('MANAGER_ONLY', 0.00::numeric, 50000.00::numeric, 1, 'DEPARTMENT_MAPPING', null::text, 'MANAGER'), ('MANAGER_MD', 50000.01::numeric, 500000.00::numeric, 1, 'DEPARTMENT_MAPPING', null::text, 'MANAGER'), ('MANAGER_MD', 50000.01::numeric, 500000.00::numeric, 2, 'FIXED_EMPLOYEE_ROLE', 'SESS-002', 'MANAGING_DIRECTOR'), ('MANAGER_MD_TD', 500000.01::numeric, null::numeric, 1, 'DEPARTMENT_MAPPING', null::text, 'MANAGER'), ('MANAGER_MD_TD', 500000.01::numeric, null::numeric, 2, 'FIXED_EMPLOYEE_ROLE', 'SESS-002', 'MANAGING_DIRECTOR'), ('MANAGER_MD_TD', 500000.01::numeric, null::numeric, 3, 'FIXED_EMPLOYEE_ROLE', 'SESS-001', 'TECHNICAL_DIRECTOR')), actual as (select "RouteCode" route_code, "MinimumAmount" minimum_amount, "MaximumAmount" maximum_amount, "StepNumber" step_number, "ApproverResolutionType" resolution_type, "ApproverEmployeeCode" employee_code, "ApproverRoleCode" role_code from nexa.purchase_approval_workflow_steps where "IsActive" = true and "RouteCode" in ('MANAGER_ONLY','MANAGER_MD','MANAGER_MD_TD')), missing as (select * from expected except select * from actual), unexpected as (select * from actual except select * from expected), dupes as (select route_code, step_number from actual group by route_code, step_number having count(*) > 1), sequence_bad as (select route_code from actual group by route_code having min(step_number) <> 1 or max(step_number) <> count(*)), overlap_bad as (select count(*) c from actual a join actual b on a.route_code <> b.route_code and a.minimum_amount <= coalesce(b.maximum_amount, 999999999999.99) and b.minimum_amount <= coalesce(a.maximum_amount, 999999999999.99)) select (select count(*) from missing) = 0 and (select count(*) from unexpected) = 0 and (select count(*) from dupes) = 0 and (select count(*) from sequence_bad) = 0 and (select c from overlap_bad) = 0) as workflow_ok,
      (select count(*) from nexa.employees e join nexa.rev868c3_employee_backup b on b."EmployeeId" = e."Id" where e."LoginEnabled" is distinct from b."LoginEnabled") = 0 as login_ok,
      (select count(*) from nexa.employees e join nexa.rev868c3_employee_backup b on b."EmployeeId" = e."Id" where e."ApprovalStatus" is distinct from b."ApprovalStatus") = 0 as approval_ok,
      (select count(*) from nexa.employees where "EmployeeCode" = 'SESS-040' and "EmployeeName" = 'NARREN VALENTINO' and "DateOfJoining" = DATE '2026-02-01' and "IsDateOfJoiningApproximate" = false) = 1 as narren_ok,
      (select count(*) from nexa.employees where "EmployeeCode" = 'SESS-049' and "PayrollEmployeeId" = '1072' and "Gender" = 'Female') = 1 as mageshwari_ok,
      (select count(*) from (select "EmployeeCode" from nexa.employees group by "EmployeeCode" having count(*) > 1) d) = 0 as dup_employee_ok,
      (select count(*) from (select "PayrollEmployeeId" from nexa.employees where "PayrollEmployeeId" is not null group by "PayrollEmployeeId" having count(*) > 1) d) = 0 as dup_payroll_ok,
      (with required(code) as (select unnest(string_to_array('$RelievedEmployeeCodes', ','))), covered(code) as (select distinct e."EmployeeCode" from nexa.employee_status_history h join nexa.employees e on e."Id" = h."EmployeeId" join required r on r.code = e."EmployeeCode" where h."CreatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION' and h."NewStatus" in ('Left / Resigned','Inactive')) select count(*) from required r left join covered c using(code) where c.code is null) = 0 as status_history_ok,
      (with required(code) as (select unnest(string_to_array('$ChangedDepartmentEmployeeCodes', ','))), covered(code) as (select distinct e."EmployeeCode" from nexa.employee_department_history h join nexa.employees e on e."Id" = h."EmployeeId" join required r on r.code = e."EmployeeCode" where h."CorrelationId" = 'REV868C3_EMPLOYEE_WORKBOOK_RECONCILIATION') select count(*) from required r left join covered c using(code) where c.code is null) = 0 as department_history_ok,
      (with expected(code) as (select unnest(string_to_array('$ManagerRoleEmployeeCodes', ','))), actual(code) as (select distinct e."EmployeeCode" from nexa.employee_role_assignments era join nexa.employees e on e."Id" = era."EmployeeId" where era."CreatedBy" = 'REV868C3_DEPARTMENT_MANAGER_PERMISSION') select (select count(*) from expected e left join actual a using(code) where a.code is null) = 0 and (select count(*) from actual a left join expected e using(code) where e.code is null) = 0) as manager_roles_ok,
      (select count(*) from nexa.roles where "Code" = 'DEPARTMENT_MANAGER' and "IsPrivileged" = false and "IsActive" = true) = 1 as manager_role_state_ok,
      (with expected(row_key) as (select unnest(string_to_array('$ManagerPermissionRows', ','))), actual(row_key) as (select p."PageKey" || ':V=' || case when rpp."CanView" then 'T' else 'F' end || ':A=' || case when rpp."CanApprove" then 'T' else 'F' end || ':R=' || case when rpp."CanReject" then 'T' else 'F' end || ':C=' || case when rpp."CanRequestClarification" then 'T' else 'F' end || ':RV=' || case when rpp."CanRequestRevision" then 'T' else 'F' end || ':AH=' || case when rpp."CanViewAuditHistory" then 'T' else 'F' end || ':FC=' || case when rpp."HasFullControl" then 'T' else 'F' end from nexa.role_page_permissions rpp join nexa.roles r on r."Id" = rpp."RoleId" join nexa.page_definitions p on p."Id" = rpp."PageDefinitionId" where r."Code" = 'DEPARTMENT_MANAGER' and p."PageKey" in ('purchase.requisitions','purchase.requisition-approvals')), dupes as (select row_key from actual group by row_key having count(*) > 1) select (select count(*) from expected e left join actual a using(row_key) where a.row_key is null) = 0 and (select count(*) from actual a left join expected e using(row_key) where e.row_key is null) = 0 and (select count(*) from dupes) = 0) as role_permissions_ok,
      (select count(*) from nexa.audit_logs where "CorrelationId" = 'REV868C3_EMPLOYEE_WORKBOOK_RECONCILIATION' and "Result" = 'Success') > 0 as audit_ok
)
select 'database_acceptance_state=' || case when migrations_ok and active_employees_ok and relieved_employees_ok and departments_ok and mappings_ok and workflow_ok and login_ok and approval_ok and narren_ok and mageshwari_ok and dup_employee_ok and dup_payroll_ok and status_history_ok and department_history_ok and manager_roles_ok and manager_role_state_ok and role_permissions_ok and audit_ok then 'PASS' else 'FAIL' end from conditions;
"@
}
function Get-TestResultSummary([string]$TrxPath) {
    [xml]$trx = Get-Content -LiteralPath $TrxPath
    $ns = New-Object System.Xml.XmlNamespaceManager($trx.NameTable)
    $ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
    $counters = $trx.TestRun.ResultSummary.Counters
    $results = @($trx.SelectNodes('//t:UnitTestResult', $ns) | ForEach-Object { "$($_.testName)|$($_.outcome)" })
    return [pscustomobject]@{
        Total = $counters.total
        Passed = $counters.passed
        Failed = $counters.failed
        Skipped = $counters.notExecuted
        Results = $results
    }
}
function Assert-RequiredTargetedTestsPassed($Summary) {
    $missing = New-Object System.Collections.Generic.List[string]
    $failed = New-Object System.Collections.Generic.List[string]
    foreach ($required in $TargetedTestNames) {
        $matches = @($Summary.Results | Where-Object { $_ -like "*$required|*" })
        if ($matches.Count -eq 0) { $missing.Add($required); continue }
        if (-not ($matches | Where-Object { $_ -like '*|Passed' })) { $failed.Add($required) }
    }
    if ($missing.Count -gt 0 -or $failed.Count -gt 0) {
        throw "REV868C3 targeted PostgreSQL test evidence incomplete. Missing: $($missing -join ', '). Failed: $($failed -join ', ')."
    }
}
function Format-RequiredTargetedTestEvidence($Summary) {
    $lines = New-Object System.Collections.Generic.List[string]
    foreach ($required in $TargetedTestNames) {
        $matches = @($Summary.Results | Where-Object { $_ -like "*$required|*" })
        if ($matches.Count -eq 0) { $lines.Add("targeted_test|$required|Missing") }
        else { foreach ($match in $matches) { $lines.Add("targeted_test|$match") } }
    }
    return ($lines -join "`n")
}
function Write-Plan {
    Write-Host 'REV868C3 GeneratePlanOnly'
    Write-Host "Host: $HostName"
    Write-Host "Port: $Port"
    Write-Host "Target DB: $Database"
    Write-Host "Rejected DBs: $($RejectedDatabases -join ', '), REV861-like names"
    Write-Host "Prerequisite migrations: $($RequiredMigrations -join ', ')"
    Write-Host "Target migration: $MigrationName"
    Write-Host 'No-secret prechecks resolve dotnet-ef and validate EF project metadata/migration discovery before password prompt or backup creation.'
    Write-Host "Infrastructure project: $InfrastructureProject"
    Write-Host "Startup project: $StartupProject"
    Write-Host "EF framework/configuration: $TargetFramework / $BuildConfiguration"
    Write-Host 'Full apply mode reuses the latest valid non-zero pre-C3 isolated backup when present; otherwise it creates one before EF migration application.'
    Write-Host 'No main DB operation is permitted.'
    Write-Host 'Preflight SQL:'
    Write-Host (Get-PreflightSql)
    Write-Host 'Post-migration/resume SQL:'
    Write-Host (Get-PostMigrationSql)
    Write-Host 'Full execution final report requires database_acceptance_state=PASS, test_acceptance_state=PASS, and overall_acceptance_state=PASS.'
    Write-Host 'overall_acceptance_state is written only when database evidence passes, all six required PostgreSQL TRX tests are present and passed, failed count is zero, and no required test is skipped.'
    Write-Host 'Targeted PostgreSQL tests required in full execution:'
    $TargetedTestNames | ForEach-Object { Write-Host "required_test=$_" }
}
if ($GeneratePlanOnly) { Write-Plan; return }
try {
    Assert-TargetDatabaseName $Database
    $script:psqlExe = Resolve-File $psql 'psql.exe'
    $script:pgDumpExe = Resolve-File $pgDump 'pg_dump.exe'
    $script:dotnetExe = Resolve-File $dotnet '.NET executable'
    $script:InfrastructureProjectPath = Assert-SdkStyleProject $InfrastructureProject 'Infrastructure migration project'
    $script:StartupProjectPath = Assert-SdkStyleProject $StartupProject 'API startup project'
    $script:dotnetEfInvocation = Resolve-DotnetEfInvocation $script:dotnetExe $DotnetEfPath
    Test-DotnetEfTool
    Test-EfProjectMetadata
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
    New-Item -ItemType Directory -Force -Path $trxDir | Out-Null
    $stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $existingBackup = Find-ExistingValidPreC3Backup
    if ($null -ne $existingBackup) {
        $backupFile = $existingBackup.FullName
        $backupItem = $existingBackup
        $backupHash = (Get-FileHash -LiteralPath $backupFile -Algorithm SHA256).Hash
        $preReport = Join-Path $evidenceDir ("rev868c3_pre_migration_backup_reused_" + $stamp + ".md")
        @("# REV868C3 Reused Pre-Migration Isolated Backup", "", "Database: $Database", "Backup file: $backupFile", "Backup bytes: $($backupItem.Length)", "Backup SHA-256: $backupHash", "", "Existing non-zero pre-C3 backup reused. Migration not yet applied at this report checkpoint.") | Set-Content -LiteralPath $preReport -Encoding UTF8
    }
    else {
        $backupFile = Join-Path $backupDir ("sess_nexaerp_rev868_verify_pre_rev868c3_$stamp.dump")
        & $script:pgDumpExe -h $HostName -p $Port -U $UserName -d $Database -F c -f $backupFile
        if ($LASTEXITCODE -ne 0) { throw "pg_dump failed with exit code $LASTEXITCODE." }
        $backupItem = Get-Item -LiteralPath $backupFile
        if ($backupItem.Length -le 0) { throw 'Pre-C3 isolated backup is zero bytes. Migration blocked.' }
        $backupHash = (Get-FileHash -LiteralPath $backupFile -Algorithm SHA256).Hash
        $preReport = Join-Path $evidenceDir ("rev868c3_pre_migration_backup_" + $stamp + ".md")
        @("# REV868C3 Pre-Migration Isolated Backup", "", "Database: $Database", "Backup file: $backupFile", "Backup bytes: $($backupItem.Length)", "Backup SHA-256: $backupHash", "", "Migration not yet applied at this report checkpoint.") | Set-Content -LiteralPath $preReport -Encoding UTF8
    }
    Invoke-EfDatabaseUpdateSanitized
    $env:REV868C3_POSTGRES = $env:ConnectionStrings__NexaErp
    $trxName = "rev868c3_employee_reconciliation_$stamp.trx"
    $testOutput = @(& $script:dotnetExe test .\SESS.NexaERP.slnx --configuration Release --filter "Rev868C3PostgreSqlWorkflowVerificationTests" --logger "trx;LogFileName=$trxName" --results-directory $trxDir 2>&1)
    if ($LASTEXITCODE -ne 0) { throw "REV868C3 PostgreSQL tests failed. exit_code=$LASTEXITCODE; see sanitized TRX output path when available." }
    $trxPath = Join-Path $trxDir $trxName
    $testSummary = Get-TestResultSummary $trxPath
    Assert-RequiredTargetedTestsPassed $testSummary
    if ([int]$testSummary.Failed -ne 0) { throw "REV868C3 PostgreSQL test failed count must be zero. Failed: $($testSummary.Failed)" }
    $testAcceptanceState = 'PASS'
    $databaseEvidence = Invoke-Psql (Get-PostMigrationSql)
    if ($databaseEvidence -notmatch 'database_acceptance_state=PASS') { throw "REV868C3 database acceptance failed.`n$databaseEvidence" }
    $report = Join-Path $evidenceDir ("rev868c3_employee_reconciliation_" + $stamp + ".md")
    @("# REV868C3 Isolated Verification", "", "Backup file: $backupFile", "Backup SHA-256: $backupHash", "TRX path: $trxPath", "Test total: $($testSummary.Total); passed: $($testSummary.Passed); failed: $($testSummary.Failed); skipped: $($testSummary.Skipped)", "", '```text', $databaseEvidence, '```', "", '## Targeted PostgreSQL test evidence', '```text', (Format-RequiredTargetedTestEvidence $testSummary), '```', "", "database_acceptance_state=PASS", "test_acceptance_state=$testAcceptanceState", "overall_acceptance_state=PASS") | Set-Content -LiteralPath $report -Encoding UTF8
    Write-Host "REV868C3 report: $report"
}
finally {
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:\ConnectionStrings__NexaErp -ErrorAction SilentlyContinue
    Remove-Item Env:\NexaErp__ExpectedDatabase -ErrorAction SilentlyContinue
    if ($plainPassword) { $plainPassword = $null }
    if ($securePassword) { $securePassword.Dispose() }
}
