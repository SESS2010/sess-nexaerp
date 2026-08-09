param(
    [string]$GitPath,
    [string]$DotnetPath
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
function Resolve-File([string]$Path, [string]$Label) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "$Label not found: $Path" }
    return (Resolve-Path -LiteralPath $Path).Path
}
function Assert-TargetDatabaseName([string]$Name) {
    if ($Name -ne $Database) { throw "REV868C3 resume helper is restricted to $Database. Actual: $Name" }
    if ($RejectedDatabases -contains $Name -or $Name -match '(?i)rev861') { throw "Protected database rejected: $Name" }
}
function Invoke-Psql([string]$Sql) {
    $output = & $script:psqlExe -h $HostName -p $Port -U $UserName -d $Database -v ON_ERROR_STOP=1 -t -A -c $Sql 2>&1
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
    $identity = Invoke-Psql 'select current_database();'
    Assert-TargetDatabaseName $identity.Trim()
    $migrationList = ($ExpectedMigrations | ForEach-Object { "'$_'" }) -join ','
    $migrationCheck = Invoke-Psql "select count(*) from \"public\".\"__EFMigrationsHistory\" where \"MigrationId\" in ($migrationList);"
    if ([int]$migrationCheck.Trim() -ne $ExpectedMigrations.Count) { throw "REV868C3 resume requires all $($ExpectedMigrations.Count) migrations already present exactly once. Count: $migrationCheck" }
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
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:\ConnectionStrings__NexaErp -ErrorAction SilentlyContinue
    Remove-Item Env:\NexaErp__ExpectedDatabase -ErrorAction SilentlyContinue
}
