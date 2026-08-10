[CmdletBinding()]
param(
    [string]$SourceDatabase = "sess_nexaerp_rev868_verify",
    [string]$TargetDatabase = "sess_nexaerp_rev869a_verify",
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$UserName = "postgres",
    [string]$PsqlPath = "C:\\Program Files\\PostgreSQL\\17\\bin\\psql.exe",
    [string]$PgDumpPath = "C:\\Program Files\\PostgreSQL\\17\\bin\\pg_dump.exe",
    [string]$PgRestorePath = "C:\\Program Files\\PostgreSQL\\17\\bin\\pg_restore.exe",
    [string]$CreateDbPath = "C:\\Program Files\\PostgreSQL\\17\\bin\\createdb.exe",
    [switch]$GeneratePlanOnly,
    [switch]$SourcePreflightOnly,
    [switch]$Provision,
    [switch]$PostProvisionVerification
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$acceptedSource = "sess_nexaerp_rev868_verify"
$acceptedTarget = "sess_nexaerp_rev869a_verify"
$acceptedHost = "localhost"
$acceptedPort = 5432
$expectedMigrations = @(
    "20260808110924_Phase1Foundation",
    "20260808114550_Phase1AuthorizationSeed",
    "20260808123411_Rev866EmployeePermissionMatrix",
    "20260808142353_Rev866CorrectiveStatusPermissionAudit",
    "20260808151207_Rev867MasterFoundation",
    "20260808160435_Rev867C1Corrections",
    "20260808182945_Rev868PurchaseRequisitionFoundation",
    "20260808190920_Rev868PurchaseLocationAllocationCorrection",
    "20260809123000_Rev868C2DepartmentManagerApprovalMapping",
    "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation",
    "20260810110000_Rev868C3LegacyMixedDepartmentDeactivationCorrection"
)
$protectedDatabases = @(
    "sess_nexaerp", "postgres", "template0", "template1",
    "REV861-like names", "production-like names", "every unexpected database"
)
$repoRoot = Split-Path -Parent $PSScriptRoot
$approvedBackupRoot = Join-Path $repoRoot "backups\postgresql\pre-rev869a-isolated"
$evidenceRoot = Join-Path $repoRoot "local-evidence\rev869a-isolated-provisioning"
$password = $null
$plainPassword = $null
$failedQueryLabel = "NOT_APPLICABLE"
$failureSqlState = "NOT_AVAILABLE"
$failureSchema = "NOT_AVAILABLE"
$failureTable = "NOT_AVAILABLE"
$failureColumn = "NOT_AVAILABLE"
$lastSafeEvidenceLines = @()
$lastEvidenceMalformedCount = 0

function Assert-Mode {
    $count = @($GeneratePlanOnly, $SourcePreflightOnly, $Provision, $PostProvisionVerification | Where-Object { $_ }).Count
    if ($count -ne 1) { throw "Select exactly one mode." }
}

function Assert-EndpointSafety {
    if ($SourceDatabase -cne $acceptedSource) { throw "Rejected source database." }
    if ($TargetDatabase -cne $acceptedTarget) { throw "Rejected target database." }
    if ($SourceDatabase -ceq $TargetDatabase) { throw "Source and target must differ." }
    if ($HostName -cne $acceptedHost -or $Port -ne $acceptedPort) { throw "Only localhost:5432 is accepted." }
    foreach ($name in @($SourceDatabase, $TargetDatabase)) {
        if ($name -match "(?i)(rev861|production|prod|live|main)") { throw "Protected database pattern rejected." }
    }
}

function Protect-Text([string]$Text) {
    if ($null -eq $Text) { return "" }
    $safe = $Text -replace "(?i)(password|pwd|secret|token)\s*[=:]\s*[^;\s]+", '$1=[REDACTED]'
    $safe = $safe -replace "(?i)(employee(code|name)?|email)\s*[=:]\s*[^;\r\n]+", '$1=[REDACTED]'
    $safe = $safe -replace "(?is)\b(DETAIL|CONTEXT|STATEMENT):.*", '$1=[REDACTED]'
    return $safe
}

function Assert-ReadOnlySql([string]$Name, [string]$Sql) {
    $normalized = $Sql -replace "(?s)/\*.*?\*/", " " -replace "(?m)--.*$", " "
    $normalized = $normalized -replace "'(?:''|[^'])*'", "''" -replace '"(?:""|[^"])*"', '""'
    if ($Sql -notmatch "(?is)^\s*begin\s+transaction\s+read\s+only\s*;" -or $Sql -notmatch "(?is)(commit|rollback)\s*;\s*$") {
        throw "$Name SQL is not enclosed in a read-only transaction."
    }
    if ($normalized -match "(?i)\b(insert|update|delete|merge|create|alter|drop|truncate|grant|revoke|copy|call|do|vacuum|analyze|reindex)\b") {
        throw "$Name SQL contains a modifying statement."
    }
}

function Assert-SafeRestoreArguments([string[]]$Arguments) {
    if ($Arguments -contains "--clean" -or $Arguments -contains "--create") { throw "Unsafe restore option rejected." }
    if ($Arguments -notcontains "--no-owner" -or $Arguments -notcontains "--no-privileges") { throw "Required restore isolation options are missing." }
}

function Resolve-Tool([string]$Path, [string]$Leaf) {
    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "$Leaf was not found." }
    return (Resolve-Path -LiteralPath $Path).Path
}

function Initialize-DatabaseAccess {
    $script:PsqlPath = Resolve-Tool $PsqlPath "psql.exe"
    $script:PgDumpPath = Resolve-Tool $PgDumpPath "pg_dump.exe"
    $script:PgRestorePath = Resolve-Tool $PgRestorePath "pg_restore.exe"
    $script:CreateDbPath = Resolve-Tool $CreateDbPath "createdb.exe"
    $script:password = Read-Host "PostgreSQL password (not logged)" -AsSecureString
    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($script:password)
    try { $script:plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }
    $env:PGPASSWORD = $script:plainPassword
}

function Find-DiagnosticIdentifier([string]$Text, [string[]]$Patterns) {
    foreach ($pattern in $Patterns) {
        $match = [regex]::Match($Text, $pattern)
        if ($match.Success) { return $match.Groups[1].Value }
    }
    return "NOT_AVAILABLE"
}

function Set-SanitizedFailureMetadata([object[]]$Output) {
    $diagnostic = $Output -join "`n"
    $script:failureSqlState = Find-DiagnosticIdentifier $diagnostic @(
        '(?im)(?:ERROR|FATAL|PANIC):\s+([0-9A-Z]{5}):',
        '(?im)\bSQLSTATE\s*[=:]\s*([0-9A-Z]{5})'
    )
    $script:failureSchema = Find-DiagnosticIdentifier $diagnostic @(
        '(?im)SCHEMA NAME:\s*([A-Za-z_][A-Za-z0-9_]*)',
        '(?im)relation\s+"([A-Za-z_][A-Za-z0-9_]*)\.[A-Za-z_][A-Za-z0-9_]*"'
    )
    $script:failureTable = Find-DiagnosticIdentifier $diagnostic @(
        '(?im)TABLE NAME:\s*([A-Za-z_][A-Za-z0-9_]*)',
        '(?im)relation\s+"(?:[A-Za-z_][A-Za-z0-9_]*\.)?([A-Za-z_][A-Za-z0-9_]*)"'
    )
    $script:failureColumn = Find-DiagnosticIdentifier $diagnostic @(
        '(?im)COLUMN NAME:\s*([A-Za-z_][A-Za-z0-9_]*)',
        '(?im)column\s+"([A-Za-z_][A-Za-z0-9_]*)"\s+does not exist'
    )
}

function Invoke-Native([string]$Executable, [string[]]$Arguments, [string]$Purpose) {
    $output = & $Executable @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        Set-SanitizedFailureMetadata @($output)
        throw (Protect-Text "$Purpose failed; sanitized diagnostic metadata captured.")
    }
    return @($output)
}

function Invoke-ReadOnlySql([string]$Database, [string]$Name, [string]$Sql) {
    if ($Database -cne $acceptedSource -and $Database -cne $acceptedTarget) { throw "Unexpected database rejected." }
    Assert-ReadOnlySql $Name $Sql
    $tempFile = Join-Path ([IO.Path]::GetTempPath()) ("rev869a-" + [guid]::NewGuid().ToString("N") + ".sql")
    try {
        [IO.File]::WriteAllText($tempFile, $Sql, [Text.UTF8Encoding]::new($false))
        $script:failedQueryLabel = $Name
        $args = @("-X", "-h", $HostName, "-p", "$Port", "-U", $UserName, "-d", $Database, "-v", "ON_ERROR_STOP=1", "-v", "VERBOSITY=verbose", "-At", "-f", $tempFile)
        $result = Invoke-Native $PsqlPath $args $Name
        return $result
    }
    finally { if (Test-Path -LiteralPath $tempFile) { Remove-Item -LiteralPath $tempFile -Force } }
}

function Get-ExpectedMigrationValues {
    return (($expectedMigrations | ForEach-Object { "('" + $_ + "')" }) -join ",`n")
}

$preservationRelations = [ordered]@{
    employees = "nexa.employees"
    departments = "nexa.departments"
    department_approval_mappings = "nexa.department_approval_mappings"
    purchase_requisitions = "nexa.purchase_requisitions"
    purchase_requisition_approval_history = "nexa.purchase_requisition_approval_history"
    purchase_requisition_status_history = "nexa.purchase_requisition_status_history"
    stock_availability_checks = "nexa.stock_availability_checks"
    stock_availability_check_lines = "nexa.stock_availability_check_lines"
    stock_reservations = "nexa.stock_reservations"
    stock_reservation_history = "nexa.stock_reservation_history"
    purchase_requirement_handoffs = "nexa.purchase_requirement_handoffs"
    purchase_approval_route_settings = "nexa.purchase_approval_route_settings"
    purchase_approval_workflow_steps = "nexa.purchase_approval_workflow_steps"
    page_definitions = "nexa.page_definitions"
    role_page_permissions = "nexa.role_page_permissions"
    audit_logs = "nexa.audit_logs"
    employee_status_history = "nexa.employee_status_history"
    employee_department_history = "nexa.employee_department_history"
    employee_approval_history = "nexa.employee_approval_history"
    employee_import_history = "nexa.employee_import_history"
}
$preservationTables = @($preservationRelations.Keys)
$sqlColumnContracts = [ordered]@{
    'public.__EFMigrationsHistory' = @('MigrationId')
    'nexa.employees' = @('EmployeeCode', 'Status')
    'nexa.departments' = @('Code', 'IsActive')
    'nexa.department_approval_mappings' = @('ApprovalRouteCode', 'IsActive')
    'nexa.purchase_approval_workflow_steps' = @('RouteCode')
}

function New-SchemaContractSql {
    $relationRows = @($preservationRelations.Values | ForEach-Object {
        $parts = $_.Split('.')
        "('" + $parts[0] + "','" + $parts[1] + "')"
    })
    $relationRows += "('public','__EFMigrationsHistory')"
    $relationValues = $relationRows -join ",`n"
    $columnRows = New-Object System.Collections.Generic.List[string]
    foreach ($entry in $sqlColumnContracts.GetEnumerator()) {
        $parts = $entry.Key.Split('.')
        foreach ($column in $entry.Value) { $columnRows.Add("('" + $parts[0] + "','" + $parts[1] + "','" + $column + "')") }
    }
    $columnValues = $columnRows -join ",`n"
    return @"
begin transaction read only;
with expected_relations(schema_name, table_name) as (values
$relationValues
), expected_columns(schema_name, table_name, column_name) as (values
$columnValues
), contract_evidence as (
  select
    (select count(*) from expected_relations e left join information_schema.tables t
      on t.table_schema=e.schema_name and t.table_name=e.table_name where t.table_name is null) missing_relation_count,
    (select count(*) from expected_columns e left join information_schema.columns c
      on c.table_schema=e.schema_name and c.table_name=e.table_name and c.column_name=e.column_name where c.column_name is null) missing_column_count
)
select 'missing_relation_count=' || missing_relation_count from contract_evidence
union all select 'missing_column_count=' || missing_column_count from contract_evidence
union all select 'schema_contract_state=' || case when missing_relation_count=0 and missing_column_count=0 then 'PASS' else 'FAIL' end from contract_evidence
order by 1;
commit;
"@
}

function New-EvidenceSql([bool]$RequireTargetAbsent, [string]$ExpectedDatabase, [string]$SchemaContractState) {
    if ($ExpectedDatabase -cne $acceptedSource -and $ExpectedDatabase -cne $acceptedTarget) { throw "Unexpected evidence database contract." }
    if ($SchemaContractState -cne "PASS" -and $SchemaContractState -cne "FAIL") { throw "Malformed schema contract state." }
    $expected = Get-ExpectedMigrationValues
    $requiredPreservationCount = $preservationTables.Count
    $absenceClause = if ($RequireTargetAbsent) { "AND target_database_count = 0" } else { "" }
    $tableCountSql = (($preservationRelations.GetEnumerator() | ForEach-Object {
        "  select '" + $_.Key + "' as name, count(*)::bigint as row_count from " + $_.Value
    }) -join "`n  union all`n")
    return @"
begin transaction read only;
with expected_migrations(id) as (values
$expected
), actual_migrations as (
  select "MigrationId" as id, count(*)::int as copies from public."__EFMigrationsHistory" group by "MigrationId"
), migration_evidence as (
  select
    (select count(*) from expected_migrations) expected_migration_count,
    (select count(*) from actual_migrations a join expected_migrations e using(id) where a.copies = 1) actual_matched_migration_count,
    (select count(*) from expected_migrations e left join actual_migrations a using(id) where a.id is null) missing_migration_count,
    (select count(*) from actual_migrations a left join expected_migrations e using(id) where e.id is null) unexpected_migration_count,
    (select count(*) from actual_migrations where copies <> 1) duplicate_migration_count,
    (select string_agg(id, ',' order by id) from actual_migrations where copies = 1) migration_fingerprint
), clean_departments(code) as (values
 ('MANAGEMENT'),('PURCHASE'),('STORES'),('ACCOUNTS_FINANCE'),('HR_ADMIN'),('PRODUCTION_FABRICATION'),
 ('DESIGN'),('ELECTRICAL_PLC_INSTRUMENTATION'),('REFRIGERATION_MECHANICAL'),
 ('SERVICE_TECHNICAL_SUPPORT'),('SOFTWARE_IT'),('QUALITY_QC')
), counts as (
  select
    (select count(*) from pg_catalog.pg_database where datname = '$acceptedTarget') target_database_count,
    (select count(*) from nexa.employees where "EmployeeCode" like 'SESS-%' and lower("Status") = 'active') active_employee_count,
    (select count(*) from nexa.employees where "EmployeeCode" like 'SESS-%' and lower("Status") = 'relieved') relieved_employee_count,
    (select count(*) from nexa.departments d join clean_departments c on c.code=d."Code" where d."IsActive") active_clean_department_count,
    (select count(*) from nexa.department_approval_mappings where "ApprovalRouteCode"='MANAGER' and "IsActive") active_manager_mapping_count
), table_counts as (
$tableCountSql

), preservation_evidence as (
  select count(*)::int preservation_relation_count,
    case when count(*)=$requiredPreservationCount then 'PASS' else 'FAIL' end preservation_evidence_state
  from table_counts
), gates as (
  select *,
    (current_database()='$ExpectedDatabase'
      and expected_migration_count=11 and actual_matched_migration_count=11
      and missing_migration_count=0 and unexpected_migration_count=0 and duplicate_migration_count=0
      and active_employee_count=42 and relieved_employee_count=9 and active_clean_department_count=12 and active_manager_mapping_count=14
      and '$SchemaContractState'='PASS' and preservation_evidence_state='PASS'
      $absenceClause) all_source_conditions_pass
  from migration_evidence cross join counts cross join preservation_evidence
)
select 'database_identity=' || current_database()
union all select 'expected_migration_count=' || expected_migration_count from gates
union all select 'actual_matched_migration_count=' || actual_matched_migration_count from gates
union all select 'missing_migration_count=' || missing_migration_count from gates
union all select 'unexpected_migration_count=' || unexpected_migration_count from gates
union all select 'duplicate_migration_count=' || duplicate_migration_count from gates
union all select 'migration_fingerprint=' || migration_fingerprint from gates
union all select 'target_database_count=' || target_database_count from gates
union all select 'active_employee_count=' || active_employee_count from gates
union all select 'relieved_employee_count=' || relieved_employee_count from gates
union all select 'active_clean_department_count=' || active_clean_department_count from gates
union all select 'active_manager_mapping_count=' || active_manager_mapping_count from gates
union all select 'schema_contract_state=$SchemaContractState' from gates
union all select 'preservation_relation_count=' || preservation_relation_count from gates
union all select 'preservation_evidence_state=' || preservation_evidence_state from gates
union all select 'safe_source_state=' || case when all_source_conditions_pass then 'PASS' else 'FAIL' end from gates
union all select 'provisioning_readiness_state=' || case when all_source_conditions_pass then 'PASS' else 'FAIL' end from gates
union all select 'preservation.' || name || '=' || row_count from table_counts
order by 1;
commit;
"@
}

function Convert-Evidence([object[]]$Lines) {
    $map = @{}
    $counts = @{}
    $safeLines = New-Object System.Collections.Generic.List[string]
    $malformedCount = 0
    foreach ($raw in $Lines) {
        foreach ($segment in ([string]$raw -split "`r?`n")) {
            $text = $segment.Trim()
            if ([string]::IsNullOrWhiteSpace($text) -or $text -ceq "BEGIN" -or $text -ceq "COMMIT") { continue }
            $match = [regex]::Match($text, '^(?<key>[a-z][a-z0-9_.]*)=(?<value>[A-Za-z0-9_.-]+(?:,[A-Za-z0-9_.-]+)*)$')
            if (-not $match.Success) { $malformedCount++; continue }
            $key = $match.Groups['key'].Value
            $value = $match.Groups['value'].Value
            if (-not $counts.ContainsKey($key)) { $counts[$key] = 0 }
            $counts[$key] = [int]$counts[$key] + 1
            $safeLines.Add("$key=$value")
            if ($counts[$key] -eq 1) { $map[$key] = $value } else { $map.Remove($key) }
        }
    }
    $map['__label_counts'] = $counts
    $map['__malformed_count'] = $malformedCount
    $map['__safe_lines'] = @($safeLines)
    $script:lastSafeEvidenceLines = @($safeLines)
    $script:lastEvidenceMalformedCount = $malformedCount
    return $map
}

function Assert-EvidenceValue([hashtable]$Evidence, [string]$Key, [string]$ExpectedValue, [string]$Section) {
    if (-not $Evidence.ContainsKey('__label_counts') -or -not ($Evidence['__label_counts'] -is [hashtable])) { throw "$Section evidence cardinality metadata is missing." }
    $counts = [hashtable]$Evidence['__label_counts']
    $count = if ($counts.ContainsKey($Key)) { [int]$counts[$Key] } else { 0 }
    if ($count -ne 1) { throw "$Section evidence label $Key must occur exactly once; actual_count=$count." }
    if (-not $Evidence.ContainsKey($Key) -or $Evidence[$Key] -cne $ExpectedValue) { throw "$Section evidence label $Key must equal $ExpectedValue." }
}

function Get-EvidenceValueExactlyOnce([hashtable]$Evidence, [string]$Key, [string]$Section) {
    if (-not $Evidence.ContainsKey('__label_counts') -or -not ($Evidence['__label_counts'] -is [hashtable])) { throw "$Section evidence cardinality metadata is missing." }
    $counts = [hashtable]$Evidence['__label_counts']
    $count = if ($counts.ContainsKey($Key)) { [int]$counts[$Key] } else { 0 }
    if ($count -ne 1 -or -not $Evidence.ContainsKey($Key)) { throw "$Section evidence label $Key must occur exactly once; actual_count=$count." }
    return [string]$Evidence[$Key]
}

function Assert-EvidenceIsWellFormed([hashtable]$Evidence, [string]$Section) {
    if (-not $Evidence.ContainsKey('__malformed_count') -or [int]$Evidence['__malformed_count'] -ne 0) { throw "$Section evidence contains malformed output." }
}

function Assert-SchemaContractEvidence([hashtable]$Evidence) {
    Assert-EvidenceIsWellFormed $Evidence "Schema contract"
    foreach ($pair in @{ missing_relation_count='0'; missing_column_count='0'; schema_contract_state='PASS' }.GetEnumerator()) {
        Assert-EvidenceValue $Evidence $pair.Key $pair.Value "Schema contract"
    }
}

function Get-DatabaseSchemaContract([string]$Database, [string]$QueryLabel) {
    $sql = New-SchemaContractSql
    return Convert-Evidence (Invoke-ReadOnlySql $Database $QueryLabel $sql)
}

function Assert-SourceEvidence([hashtable]$Evidence) {
    Assert-EvidenceIsWellFormed $Evidence "Source preflight"
    foreach ($pair in @{
        database_identity=$acceptedSource; expected_migration_count='11'; actual_matched_migration_count='11';
        missing_migration_count='0'; unexpected_migration_count='0'; duplicate_migration_count='0'; target_database_count='0';
        active_employee_count='42'; relieved_employee_count='9'; active_clean_department_count='12';
        active_manager_mapping_count='14'; schema_contract_state='PASS'; preservation_relation_count='20';
        preservation_evidence_state='PASS'; safe_source_state='PASS'; provisioning_readiness_state='PASS'
    }.GetEnumerator()) {
        Assert-EvidenceValue $Evidence $pair.Key $pair.Value "Source preflight"
    }
}

function Assert-AcceptedCoreEvidence([hashtable]$Evidence, [string]$ExpectedDatabase) {
    Assert-EvidenceIsWellFormed $Evidence "Accepted core"
    foreach ($pair in @{
        database_identity=$ExpectedDatabase; expected_migration_count='11'; actual_matched_migration_count='11';
        missing_migration_count='0'; unexpected_migration_count='0'; duplicate_migration_count='0'; active_employee_count='42';
        relieved_employee_count='9'; active_clean_department_count='12'; active_manager_mapping_count='14';
        schema_contract_state='PASS'; preservation_relation_count='20'; preservation_evidence_state='PASS';
        safe_source_state='PASS'; provisioning_readiness_state='PASS'
    }.GetEnumerator()) {
        Assert-EvidenceValue $Evidence $pair.Key $pair.Value "Accepted core"
    }
}
function Get-SourceEvidence {
    $schemaEvidence = Get-DatabaseSchemaContract $acceptedSource "SOURCE_PREFLIGHT_SCHEMA_CONTRACT"
    Assert-SchemaContractEvidence $schemaEvidence
    $sql = New-EvidenceSql $true $acceptedSource $schemaEvidence['schema_contract_state']
    return Convert-Evidence (Invoke-ReadOnlySql $acceptedSource "SOURCE_PREFLIGHT_ACCEPTANCE_AND_PRESERVATION" $sql)
}

function Assert-PreservationEqual([hashtable]$Source, [hashtable]$Target) {
    Assert-EvidenceIsWellFormed $Source "Source preservation"
    Assert-EvidenceIsWellFormed $Target "Target preservation"
    foreach ($table in $preservationTables) {
        $key = "preservation.$table"
        $sourceValue = Get-EvidenceValueExactlyOnce $Source $key "Source preservation"
        $targetValue = Get-EvidenceValueExactlyOnce $Target $key "Target preservation"
        if ($sourceValue -cne $targetValue) { throw "Preservation mismatch for $table." }
    }
    $sourceFingerprint = Get-EvidenceValueExactlyOnce $Source 'migration_fingerprint' "Source preservation"
    $targetFingerprint = Get-EvidenceValueExactlyOnce $Target 'migration_fingerprint' "Target preservation"
    if ($sourceFingerprint -cne $targetFingerprint) { throw "Migration sets do not match." }
}

function New-SanitizedEvidencePath {
    return Join-Path $evidenceRoot ("rev869a-isolated-provisioning-" + (Get-Date).ToUniversalTime().ToString("yyyyMMdd-HHmmss") + "-" + [guid]::NewGuid().ToString("N") + ".txt")
}

function Write-SanitizedEvidence([string[]]$Lines, [string]$Path = "") {
    if ([string]::IsNullOrWhiteSpace($Path)) { $Path = New-SanitizedEvidencePath }
    if (-not (Test-Path -LiteralPath $evidenceRoot)) { New-Item -ItemType Directory -Path $evidenceRoot | Out-Null }
    [IO.File]::WriteAllLines($Path, @($Lines | ForEach-Object { Protect-Text $_ }), [Text.UTF8Encoding]::new($false))
    return $Path
}

function Write-Plan {
    Write-Output "mode=GeneratePlanOnly"
    Write-Output "host=$HostName"
    Write-Output "port=$Port"
    Write-Output "source_database=$SourceDatabase"
    Write-Output "target_database=$TargetDatabase"
    Write-Output "rejected_databases=$($protectedDatabases -join ', ')"
    Write-Output "accepted_migration_count=11"
    $expectedMigrations | ForEach-Object { Write-Output "accepted_migration=$_" }
    Write-Output "backup_root=$approvedBackupRoot"
    Write-Output "backup_policy=fresh current custom-format source backup; older pre-C3 backup forbidden"
    Write-Output "source_preflight=read-only schema/column contract, identity, target absence, exact migrations, REV868C3 counts and preservation evidence"
    Write-Output "provision=pg_dump custom; create exact absent target; pg_restore --no-owner --no-privileges"
    Write-Output "post_verification=read-only identity, exact migration-set equality, preservation equality, PASS gates"
    Write-Output "failure_policy=QUARANTINED_DO_NOT_USE_OR_AUTO_REPAIR; no automatic drop or repair"
    Write-Output "This plan requests no password and performs no PostgreSQL, backup, create, restore, drop, migration, main-database, REV861, or production operation."
}

Assert-Mode
Assert-EndpointSafety
if ($GeneratePlanOnly) { Write-Plan; return }

$targetCreated = $false
$backupPath = $null
$backupSha256 = $null
$failedPhase = "INITIALIZE_DATABASE_ACCESS"
try {
    Initialize-DatabaseAccess
    if ($SourcePreflightOnly) {
        $failedPhase = "SOURCE_PREFLIGHT"
        $sourceEvidence = Get-SourceEvidence
        Assert-SourceEvidence $sourceEvidence
        Write-Output "safe_source_state=PASS"
        Write-Output "provisioning_readiness_state=PASS"
        return
    }

    if ($PostProvisionVerification) {
        $failedPhase = "POST_PROVISION_VERIFICATION"
        $sourceSchemaEvidence = Get-DatabaseSchemaContract $acceptedSource "POST_PROVISION_SOURCE_SCHEMA_CONTRACT"
        Assert-SchemaContractEvidence $sourceSchemaEvidence
        $targetSchemaEvidence = Get-DatabaseSchemaContract $acceptedTarget "POST_PROVISION_TARGET_SCHEMA_CONTRACT"
        Assert-SchemaContractEvidence $targetSchemaEvidence
        $sourceSql = New-EvidenceSql $false $acceptedSource $sourceSchemaEvidence['schema_contract_state']
        $targetSql = New-EvidenceSql $false $acceptedTarget $targetSchemaEvidence['schema_contract_state']
        $sourceEvidence = Convert-Evidence (Invoke-ReadOnlySql $acceptedSource "POST_PROVISION_SOURCE_ACCEPTANCE_AND_PRESERVATION" $sourceSql)
        $targetEvidence = Convert-Evidence (Invoke-ReadOnlySql $acceptedTarget "POST_PROVISION_TARGET_ACCEPTANCE_AND_PRESERVATION" $targetSql)
        Assert-AcceptedCoreEvidence $sourceEvidence $acceptedSource
        Assert-AcceptedCoreEvidence $targetEvidence $acceptedTarget
        Assert-PreservationEqual $sourceEvidence $targetEvidence
        Write-Output "provision_acceptance_state=PASS"
        return
    }

    $failedPhase = "SOURCE_PREFLIGHT"
    $sourceEvidence = Get-SourceEvidence
    Assert-SourceEvidence $sourceEvidence
    $failedPhase = "SOURCE_BACKUP"
    $failedQueryLabel = "NOT_APPLICABLE"
    if (-not (Test-Path -LiteralPath $approvedBackupRoot)) { New-Item -ItemType Directory -Path $approvedBackupRoot | Out-Null }
    $resolvedBackupRoot = (Resolve-Path -LiteralPath $approvedBackupRoot).Path
    $expectedRoot = [IO.Path]::GetFullPath($approvedBackupRoot)
    if ($resolvedBackupRoot -cne $expectedRoot) { throw "Backup root escaped the approved path." }
    $backupPath = Join-Path $resolvedBackupRoot ("rev868c3-source-" + (Get-Date).ToUniversalTime().ToString("yyyyMMdd-HHmmss") + "-" + [guid]::NewGuid().ToString("N") + ".dump")
    $dumpArgs = @("-h", $HostName, "-p", "$Port", "-U", $UserName, "-d", $acceptedSource, "--format=custom", "--file", $backupPath, "--no-owner", "--no-privileges")
    Invoke-Native $PgDumpPath $dumpArgs "Fresh source backup" | Out-Null
    $backupItem = Get-Item -LiteralPath $backupPath
    if ($backupItem.Length -le 0) { throw "Backup is empty." }
    $backupSha256 = (Get-FileHash -LiteralPath $backupPath -Algorithm SHA256).Hash
    if ($backupSha256 -notmatch '^[A-Fa-f0-9]{64}$') { throw "Backup SHA-256 is missing." }
    $backupCreatedUtc = $backupItem.CreationTimeUtc.ToString("o")

    $failedPhase = "SOURCE_PREFLIGHT_RECHECK"
    $sourceEvidence = Get-SourceEvidence
    Assert-SourceEvidence $sourceEvidence
    $failedPhase = "TARGET_CREATE"
    $failedQueryLabel = "NOT_APPLICABLE"
    $createArgs = @("-h", $HostName, "-p", "$Port", "-U", $UserName, "--maintenance-db=postgres", "--encoding=UTF8", "--template=template0", $acceptedTarget)
    Invoke-Native $CreateDbPath $createArgs "Create isolated target" | Out-Null
    $targetCreated = $true
    $failedPhase = "TARGET_RESTORE"
    $failedQueryLabel = "NOT_APPLICABLE"
    $restoreArgs = @("-h", $HostName, "-p", "$Port", "-U", $UserName, "-d", $acceptedTarget, "--no-owner", "--no-privileges", $backupPath)
    Assert-SafeRestoreArguments $restoreArgs
    Invoke-Native $PgRestorePath $restoreArgs "Restore isolated target" | Out-Null

    $failedPhase = "POST_PROVISION_VERIFICATION"
    $targetSchemaEvidence = Get-DatabaseSchemaContract $acceptedTarget "POST_PROVISION_TARGET_SCHEMA_CONTRACT"
    Assert-SchemaContractEvidence $targetSchemaEvidence
    $verificationSql = New-EvidenceSql $false $acceptedTarget $targetSchemaEvidence['schema_contract_state']
    $targetEvidence = Convert-Evidence (Invoke-ReadOnlySql $acceptedTarget "POST_PROVISION_TARGET_ACCEPTANCE_AND_PRESERVATION" $verificationSql)
    Assert-AcceptedCoreEvidence $targetEvidence $acceptedTarget
    Assert-PreservationEqual $sourceEvidence $targetEvidence
    $evidencePath = Write-SanitizedEvidence @(
        "source_database=$acceptedSource", "target_database=$acceptedTarget", "backup_path=$backupPath",
        "backup_byte_size=$($backupItem.Length)", "backup_creation_utc=$backupCreatedUtc", "backup_sha256=$backupSha256",
        "provision_acceptance_state=PASS"
    )
    Write-Output "provision_acceptance_state=PASS"
    Write-Output "sanitized_evidence_path=$evidencePath"
}
catch {
    $state = if ($targetCreated) { "QUARANTINED_DO_NOT_USE_OR_AUTO_REPAIR" } else { "NOT_CREATED_SAFE_RETRY_REQUIRES_NEW_PREFLIGHT" }
    $details = @("provision_acceptance_state=FAIL", "failed_phase=$failedPhase", "failed_query_label=$failedQueryLabel", "sqlstate=$failureSqlState", "failure_schema=$failureSchema", "failure_table=$failureTable", "failure_column=$failureColumn", "target_state=$state", "error=$(Protect-Text $_.Exception.Message)")
    $details += "returned_evidence_malformed_count=$lastEvidenceMalformedCount"
    foreach ($line in $lastSafeEvidenceLines) {
        $separator = $line.IndexOf('=')
        if ($separator -le 0) { continue }
        $key = $line.Substring(0, $separator)
        if ($key -ceq 'database_identity' -or $key -match '(_count|_state)$' -or $key -match '^preservation\.') {
            $details += "returned_evidence.$line"
        }
    }
    if ($backupPath) { $details += "backup_path=$backupPath" }
    if ($backupSha256) { $details += "backup_sha256=$backupSha256" }
    $failureEvidencePath = New-SanitizedEvidencePath
    Write-Output "provision_acceptance_state=FAIL"
    Write-Output "failed_phase=$failedPhase"
    Write-Output "failed_query_label=$failedQueryLabel"
    Write-Output "sqlstate=$failureSqlState"
    Write-Output "failure_schema=$failureSchema"
    Write-Output "failure_table=$failureTable"
    Write-Output "failure_column=$failureColumn"
    Write-Output "target_state=$state"
    Write-Output "sanitized_evidence_path=$failureEvidencePath"
    Write-SanitizedEvidence $details $failureEvidencePath | Out-Null
    throw (Protect-Text "Provisioning failed closed. failed_phase=$failedPhase target_state=$state")
}
finally {
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    $plainPassword = $null
    $failedQueryLabel = "NOT_APPLICABLE"
    $failureSqlState = "NOT_AVAILABLE"
    $failureSchema = "NOT_AVAILABLE"
    $failureTable = "NOT_AVAILABLE"
    $failureColumn = "NOT_AVAILABLE"
    $lastSafeEvidenceLines = @()
    $lastEvidenceMalformedCount = 0
    if ($password -is [IDisposable]) { $password.Dispose() }
    $password = $null
}
