param(
    [string]$GitPath,
    [string]$DotnetPath,
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
$dotnet = if ($DotnetPath) { $DotnetPath } else { Join-Path $root '..\.dotnet10\dotnet.exe' }
$evidenceDir = Join-Path $root 'local-evidence\rev868c3'
$trxDir = Join-Path $evidenceDir 'test-results'
$outputDir = Join-Path $root 'outputs'
$tempSqlFile = $null
function Resolve-File([string]$Path, [string]$Label) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "$Label not found: $Path" }
    return (Resolve-Path -LiteralPath $Path).Path
}
function Assert-TargetDatabaseName([string]$Name) {
    if ($Name -ne $Database) { throw "REV868C3 resume helper is restricted to $Database. Actual: $Name" }
    if ($RejectedDatabases -contains $Name -or $Name -match '(?i)rev861') { throw "Protected database rejected: $Name" }
}
function Get-ResumeSql {
    $values = ($ExpectedMigrations | ForEach-Object { "('$_')" }) -join ",`n        "
    return @"
begin transaction read only;
select 'database_identity=' || current_database();
select 'server_user=' || current_user;
select 'server_endpoint=' || coalesce(inet_server_addr()::text, 'local') || ':' || coalesce(inet_server_port()::text, 'unknown');
with expected("MigrationId") as (
    values
        $values
), actual as (
    select "MigrationId"
    from "public"."__EFMigrationsHistory"
), counts as (
    select
        (select count(*) from expected) as expected_count,
        (select count(*) from actual where "MigrationId" in (select "MigrationId" from expected)) as matched_count,
        (select count(*) from (select "MigrationId" from actual group by "MigrationId" having count(*) > 1) d) as duplicate_count,
        (select count(*) from expected e left join actual a on a."MigrationId" = e."MigrationId" where a."MigrationId" is null) as missing_count,
        (select count(*) from actual a left join expected e on e."MigrationId" = a."MigrationId" where e."MigrationId" is null) as unexpected_count
)
select 'migration_expected_count=' || expected_count from counts
union all select 'migration_matched_count=' || matched_count from counts
union all select 'migration_duplicate_count=' || duplicate_count from counts
union all select 'migration_missing_count=' || missing_count from counts
union all select 'migration_unexpected_count=' || unexpected_count from counts
union all select 'migration_acceptance_state=' || case when expected_count = 10 and matched_count = 10 and duplicate_count = 0 and missing_count = 0 and unexpected_count = 0 then 'PASS' else 'FAIL' end from counts;
select 'migration_id=' || "MigrationId"
from "public"."__EFMigrationsHistory"
where "MigrationId" in (select * from (values
        $values
) as expected("MigrationId"))
order by "MigrationId";
commit;
"@
}
function Assert-ReadOnlyResumeSql([string]$Sql) {
    $withoutStrings = [regex]::Replace($Sql, "'([^']|'')*'", "''")
    $withoutComments = [regex]::Replace($withoutStrings, '(?m)--.*$', '')
    if ($withoutComments -match '(?i)\b(insert|update|delete|merge|create|alter|drop|truncate|grant|revoke|copy|call|do|vacuum|analyze|reindex)\b') {
        throw 'Resume SQL contains a prohibited write/destructive token.'
    }
    if ($Sql -match '""[A-Za-z_]+""') { throw 'Resume SQL contains doubled quoted identifier output.' }
    if ($Sql -notmatch 'select "MigrationId"\s+from "public"\."__EFMigrationsHistory"') { throw 'Resume SQL is missing exact mixed-case migration-history query.' }
    if (($Sql.ToCharArray() | Where-Object { $_ -eq '"' }).Count % 2 -ne 0) { throw 'Resume SQL has unbalanced double quotes.' }
}
function Invoke-PsqlFile([string]$Sql) {
    Assert-ReadOnlyResumeSql $Sql
    $script:tempSqlFile = Join-Path ([System.IO.Path]::GetTempPath()) ('sess_nexa_rev868c3_resume_' + [Guid]::NewGuid().ToString('N') + '.sql')
    Set-Content -LiteralPath $script:tempSqlFile -Value $Sql -Encoding UTF8
    $output = @(& $script:psqlExe -h $HostName -p $Port -U $UserName -d $Database -v ON_ERROR_STOP=1 -t -A -f $script:tempSqlFile 2>&1)
    $exit = $LASTEXITCODE
    if ($exit -ne 0) { throw "psql read-only verification failed with exit code $exit. $($output -join ' ')" }
    return ($output -join "`n")
}
function ConvertTo-SanitizedTestOutput([object[]]$Output) {
    $lines = New-Object System.Collections.Generic.List[string]
    foreach ($item in @($Output)) {
        $line = if ($null -eq $item) { '' } else { $item.ToString() }
        $passwordKey = 'Pass' + 'word'
        $line = $line -replace ($passwordKey + '=[^;\s]+'), ($passwordKey + '=<redacted>')
        $line = $line -replace ('Host=[^;\s]+;Port=[^;\s]+;Database=[^;\s]+;Username=[^;\s]+;' + $passwordKey + '=<redacted>'), 'ConnectionString=<redacted>'
        $line = $line -replace (('SESS' + '@') + '[^\s;]+'), '<redacted-password>'
        $line = $line -replace '\b[A-Z][A-Z. ]{2,}\b', '<redacted-uppercase-text>'
        if ($line.Length -gt 500) { $line = $line.Substring(0, 500) + '...' }
        $lines.Add($line)
    }
    return ($lines -join "`n")
}
function Get-TestResultSummary([string]$TrxPath) {
    [xml]$trx = Get-Content -LiteralPath $TrxPath
    $ns = New-Object System.Xml.XmlNamespaceManager($trx.NameTable)
    $ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
    $counters = $trx.TestRun.ResultSummary.Counters
    $results = @($trx.SelectNodes('//t:UnitTestResult', $ns) | ForEach-Object { "$($_.testName)|$($_.outcome)" })
    return [pscustomobject]@{ Total = $counters.total; Passed = $counters.passed; Failed = $counters.failed; Skipped = $counters.notExecuted; Results = $results }
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
function Write-TestReport([string]$ReportPath, [string]$TrxPath, [object[]]$Output, [int]$ExitCode) {
    $summaryText = 'TRX not created or unavailable.'
    if (Test-Path -LiteralPath $TrxPath -PathType Leaf) {
        try {
            $summary = Get-TestResultSummary $TrxPath
            $summaryText = "total=$($summary.Total); passed=$($summary.Passed); failed=$($summary.Failed); skipped=$($summary.Skipped)`n" + (Format-RequiredTargetedTestEvidence $summary)
        }
        catch { $summaryText = "TRX exists but could not be parsed: $($_.Exception.GetType().Name)" }
    }
    @(
        '# REV868C3 Resume PostgreSQL Test Evidence',
        '',
        "database=$Database",
        "exit_code=$ExitCode",
        "trx_path=$TrxPath",
        "trx_exists=$(Test-Path -LiteralPath $TrxPath -PathType Leaf)",
        '',
        '## TRX summary',
        '```text',
        $summaryText,
        '```',
        '',
        '## Sanitized dotnet test output',
        '```text',
        (ConvertTo-SanitizedTestOutput $Output),
        '```'
    ) | Set-Content -LiteralPath $ReportPath -Encoding UTF8
}
Set-Location -LiteralPath $root
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
$resumeSql = Get-ResumeSql
Assert-ReadOnlyResumeSql $resumeSql
if ($GenerateSqlOnly) {
    $report = Join-Path $outputDir 'rev868c3_resume_sql_source_verification.md'
    @('# REV868C3 Resume SQL Source Verification', '', '```sql', $resumeSql, '```') | Set-Content -LiteralPath $report -Encoding UTF8
    Write-Host "REV868C3 resume SQL source report: $report"
    Write-Host $resumeSql
    return
}
$script:psqlExe = Resolve-File $psql 'psql.exe'
$script:dotnetExe = Resolve-File $dotnet 'dotnet.exe'
if ($GitPath) { [void](Resolve-File $GitPath 'git.exe') }
New-Item -ItemType Directory -Force -Path $evidenceDir | Out-Null
New-Item -ItemType Directory -Force -Path $trxDir | Out-Null
$securePassword = Read-Host -AsSecureString 'Enter PostgreSQL password for isolated REV868C3 verification database only'
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
try { $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
finally { if ($bstr -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) } }
try {
    $env:PGPASSWORD = $plainPassword
    $env:ConnectionStrings__NexaErp = "Host=$HostName;Port=$Port;Database=$Database;Username=$UserName;Password=$plainPassword"
    $env:NexaErp__ExpectedDatabase = $Database
    Write-Host "Expected host: $HostName"
    Write-Host "Expected port: $Port"
    Write-Host "Expected database: $Database"
    Write-Host "PostgreSQL user parameter: $UserName"
    $resumeEvidence = Invoke-PsqlFile $resumeSql
    $identityLine = @($resumeEvidence -split "`r?`n" | Where-Object { $_ -like 'database_identity=*' } | Select-Object -First 1)
    if ($identityLine.Count -ne 1) { throw 'Database identity evidence missing from resume SQL output.' }
    Assert-TargetDatabaseName ($identityLine[0].Substring('database_identity='.Length).Trim())
    if ($resumeEvidence -notmatch 'migration_acceptance_state=PASS') { throw "REV868C3 resume requires all $($ExpectedMigrations.Count) migrations already present exactly once.`n$resumeEvidence" }
    $returnedMigrations = @($resumeEvidence -split "`r?`n" | Where-Object { $_ -like 'migration_id=*' } | ForEach-Object { $_.Substring('migration_id='.Length).Trim() })
    $unexpectedMigrations = @($returnedMigrations | Where-Object { $ExpectedMigrations -notcontains $_ })
    $missingMigrations = @($ExpectedMigrations | Where-Object { $returnedMigrations -notcontains $_ })
    $duplicateMigrations = @($returnedMigrations | Group-Object | Where-Object { $_.Count -ne 1 })
    if ($returnedMigrations.Count -ne $ExpectedMigrations.Count -or $unexpectedMigrations.Count -ne 0 -or $missingMigrations.Count -ne 0 -or $duplicateMigrations.Count -ne 0) {
        throw 'REV868C3 resume migration ID evidence did not contain exactly the ten expected migrations once.'
    }
    $stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $trxName = "rev868c3_resume_$stamp.trx"
    $trxPath = Join-Path $trxDir $trxName
    $report = Join-Path $evidenceDir ("rev868c3_resume_postgresql_tests_" + $stamp + ".md")
    $testOutput = @(& $script:dotnetExe test .\SESS.NexaERP.slnx --configuration Release --filter "Rev868C3PostgreSqlWorkflowVerificationTests" --logger "trx;LogFileName=$trxName" --results-directory $trxDir 2>&1)
    $exit = $LASTEXITCODE
    Write-TestReport -ReportPath $report -TrxPath $trxPath -Output $testOutput -ExitCode $exit
    if ($exit -ne 0) { throw "REV868C3 resume PostgreSQL tests failed. exit_code=$exit; trx_path=$trxPath; sanitized_report=$report" }
    Write-Host "REV868C3 resume test report: $report"
}
finally {
    if ($script:tempSqlFile -and (Test-Path -LiteralPath $script:tempSqlFile -PathType Leaf)) { Remove-Item -LiteralPath $script:tempSqlFile -Force -ErrorAction SilentlyContinue }
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:\ConnectionStrings__NexaErp -ErrorAction SilentlyContinue
    Remove-Item Env:\NexaErp__ExpectedDatabase -ErrorAction SilentlyContinue
}
