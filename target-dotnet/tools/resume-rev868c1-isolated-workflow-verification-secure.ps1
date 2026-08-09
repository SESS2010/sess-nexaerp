[CmdletBinding()]
param(
    [string]$Database = "sess_nexaerp_rev868_verify",
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$UserName = "postgres",
    [string]$GitPath = "",
    [switch]$GeneratePlanOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$targetRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$dotnetPath = Join-Path $targetRoot "..\.dotnet10\dotnet.exe"
$pgBin = "C:\Program Files\PostgreSQL\17\bin"
$psqlPath = Join-Path $pgBin "psql.exe"
$reportDir = Join-Path $targetRoot "local-evidence\rev868c1"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$reportFile = Join-Path $reportDir "rev868c1_isolated_resume_final_evidence_$timestamp.md"
$trxDir = Join-Path $reportDir "test-results"
$trxName = "rev868c1_resume_$timestamp.trx"
$expectedMigrationIds = @(
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
    $candidates = New-Object System.Collections.Generic.List[string]
    if (-not [string]::IsNullOrWhiteSpace($ExplicitGitPath)) { $candidates.Add($ExplicitGitPath) }
    $cmd = Get-Command git.exe -ErrorAction SilentlyContinue; if ($cmd -and $cmd.Source) { $candidates.Add($cmd.Source) }
    $candidates.Add("C:\Users\User\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\git\cmd\git.exe")
    $candidates.Add("C:\Program Files\Git\cmd\git.exe"); $candidates.Add("C:\Program Files\Git\bin\git.exe")
    foreach ($candidate in $candidates | Select-Object -Unique) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            $item = Get-Item -LiteralPath $candidate
            if ($item.Name -ne "git.exe") { throw "Resolved Git path is not git.exe: $($item.FullName)" }
            $version = & $item.FullName --version
            if ($LASTEXITCODE -eq 0 -and ($version -join "`n") -match '^git version ') { return $item.FullName }
        }
    }
    throw "git.exe was not found. Pass -GitPath with a valid git.exe path."
}
function Get-ResumeSql {
    [ordered]@{
        "Session identity" = @"
select 'database=' || current_database()
union all select 'user=' || current_user
union all select 'server_addr=' || coalesce(inet_server_addr()::text, 'local_socket')
union all select 'server_port=' || inet_server_port()::text;
"@.Trim()
        "Unsafe target rejection evidence" = @"
select case when current_database() in ('sess_nexaerp','postgres','template0','template1','rev861','sess_rev861','sess_nexaerp_rev861')
then 'blocked_target' else 'not_blocked_name' end;
"@.Trim()
        "Eight migration IDs" = @"
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
 '20260808190920_Rev868PurchaseLocationAllocationCorrection')
group by "MigrationId"
order by "MigrationId";
"@.Trim()
        "PR lifecycle status names and counts" = @"
select "NewStatus" || '=' || count(*)
from nexa.purchase_requisition_status_history
where "CorrelationId" like 'REV868C1-%'
group by "NewStatus"
order by "NewStatus";
"@.Trim()
        "PR lifecycle branch verification" = @"
select branch || '=' || case when evidence_count > 0 then 'PASS' else 'MISSING' end
from (
  select 'Draft' branch, count(*) evidence_count from nexa.purchase_requisition_status_history where "CorrelationId" = 'REV868C1-CREATE' and "NewStatus" = 'Draft'
  union all select 'Submitted', count(*) from nexa.purchase_requisition_status_history where "CorrelationId" = 'REV868C1-SUBMIT' and "NewStatus" = 'Submitted'
  union all select 'Stores Verified', count(*) from nexa.purchase_requisition_status_history where "CorrelationId" = 'REV868C1-VERIFY' and "NewStatus" = 'PendingApproval'
  union all select 'Approved', count(*) from nexa.purchase_requisition_approval_history where "CorrelationId" = 'REV868C1-APPROVE' and "Action" = 'Approve' and "ToStatus" = 'Approved'
  union all select 'Rejected with remarks', count(*) from nexa.purchase_requisition_approval_history where "CorrelationId" = 'REV868C1-REJECT' and "Action" = 'Reject' and "ToStatus" = 'Rejected' and coalesce("Remarks",'') <> ''
  union all select 'Revision Requested', count(*) from nexa.purchase_requisition_approval_history where "CorrelationId" = 'REV868C1-REVISION' and "Action" = 'RequestRevision' and "ToStatus" = 'RevisionRequested'
  union all select 'Resubmitted', count(*) from nexa.purchase_requisition_status_history where "CorrelationId" = 'REV868C1-RESUBMIT' and "NewStatus" = 'Submitted'
  union all select 'Hold', count(*) from nexa.purchase_requisition_status_history where "CorrelationId" = 'REV868C1-HOLD' and "NewStatus" = 'Held' and coalesce("Reason",'') <> ''
  union all select 'Cancelled', count(*) from nexa.purchase_requisition_status_history where "CorrelationId" = 'REV868C1-CANCEL' and "NewStatus" = 'Cancelled' and coalesce("Reason",'') <> ''
) b
order by branch;
"@.Trim()
        "Amount routing boundary evidence" = @"
select b.amount::text
    || '|expected_route=' || b.expected_route
    || '|configured_route=' || coalesce(r."RouteCode", 'NO_ROUTE')
    || '|calculated_route=' || coalesce(r."RouteCode", 'NO_ROUTE')
    || '|canonical_role=' || coalesce(r."ApproverRoleCode", 'NO_ROLE')
    || '|display=' || case coalesce(r."RouteCode", 'NO_ROUTE')
        when 'MANAGER' then 'Department Manager'
        when 'TECHNICAL_DIRECTOR' then 'Technical Director'
        when 'MANAGING_DIRECTOR' then 'Managing Director'
        else 'Unknown'
       end
    || '|' || case when b.expected_route = r."RouteCode" and b.expected_role = r."ApproverRoleCode" then 'PASS' else 'FAIL' end
from (
  values
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
        "Approval route configuration evidence" = @"
select "RouteCode"
    || '|min=' || "MinimumAmount"::text
    || '|max=' || coalesce("MaximumAmount"::text,'NULL')
    || '|role=' || "ApproverRoleCode"
    || '|display=' || case "RouteCode"
        when 'MANAGER' then 'Department Manager'
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
        "Security 401 403 and self approval evidence" = @"
select case_name || '=' || case when evidence_count > 0 then 'PASS' else 'MISSING' end
from (
  select 'Unauthenticated request 401' case_name, count(*) evidence_count from nexa.audit_logs where "CorrelationId" = 'REV868C1-UNAUTHENTICATED-401' and "Result" = 'Failure'
  union all select 'Unauthorized role 403', count(*) from nexa.audit_logs where "CorrelationId" = 'REV868C1-DIRECT-API-403' and "Result" = 'Failure'
  union all select 'Creator submitter self approval 403', count(*) from nexa.audit_logs where "CorrelationId" = 'REV868C1-SELF-APPROVAL-403' and "Result" = 'Failure'
) s
order by case_name;
"@.Trim()
        "Workflow record counts" = @"
select 'purchase_requisitions=' || count(*) from nexa.purchase_requisitions where "PrNumber" like 'REV868C1-PR-%'
union all select 'purchase_requisition_lines=' || count(*) from nexa.purchase_requisition_lines l join nexa.purchase_requisitions p on p."Id" = l."PurchaseRequisitionId" where p."PrNumber" like 'REV868C1-PR-%'
union all select 'stock_availability_checks=' || count(*) from nexa.stock_availability_checks where "CorrelationId" like 'REV868C1-CHECK-%'
union all select 'stock_availability_check_lines=' || count(*) from nexa.stock_availability_check_lines l join nexa.stock_availability_checks c on c."Id" = l."StockAvailabilityCheckId" where c."CorrelationId" like 'REV868C1-CHECK-%'
union all select 'stock_reservations=' || count(*) from nexa.stock_reservations where "CorrelationId" like 'REV868C1-%'
union all select 'active_reservations=' || count(*) from nexa.stock_reservations where "CorrelationId" like 'REV868C1-%' and "Status" = 'Active'
union all select 'purchase_requirement_handoffs=' || count(*) from nexa.purchase_requirement_handoffs where "CorrelationId" like 'REV868C1-%'
union all select 'pending_rfq_handoffs=' || count(*) from nexa.purchase_requirement_handoffs where "CorrelationId" like 'REV868C1-%' and "Status" = 'PendingRFQ'
union all select 'purchase_requisition_status_history=' || count(*) from nexa.purchase_requisition_status_history where "CorrelationId" like 'REV868C1-%'
union all select 'purchase_requisition_approval_history=' || count(*) from nexa.purchase_requisition_approval_history where "CorrelationId" like 'REV868C1-%'
union all select 'stock_reservation_history=' || count(*) from nexa.stock_reservation_history where "CorrelationId" like 'REV868C1-%'
union all select 'audit_logs=' || count(*) from nexa.audit_logs where "CorrelationId" like 'REV868C1-%';
"@.Trim()
        "Stock reconciliation scenario evidence" = @"
select p."PrNumber" || '|requested=' || l."RequestedQuantity"::text || '|reserved=' || l."ReservedQuantity"::text || '|shortage=' || l."ShortageQuantity"::text || '|handoff=' || l."ProcurementHandoffQuantity"::text || '|status=' || p."Status"
from nexa.purchase_requisition_lines l
join nexa.purchase_requisitions p on p."Id" = l."PurchaseRequisitionId"
where p."PrNumber" in ('REV868C1-PR-FULL','REV868C1-PR-PARTIAL','REV868C1-PR-ZERO')
order by p."PrNumber";
"@.Trim()
        "Quantity reconciliation violation count" = @"
select count(*)
from nexa.purchase_requisition_lines l
join nexa.purchase_requisitions p on p."Id" = l."PurchaseRequisitionId"
where p."PrNumber" like 'REV868C1-PR-%'
  and (l."RequestedQuantity" <= 0
   or l."ReservedQuantity" < 0
   or l."ShortageQuantity" < 0
   or l."ProcurementHandoffQuantity" < 0
   or l."ReservedQuantity" > l."RequestedQuantity"
   or l."ShortageQuantity" <> greatest(l."RequestedQuantity" - l."ReservedQuantity", 0)
   or l."ProcurementHandoffQuantity" <> l."ShortageQuantity");
"@.Trim()
        "Duplicate active reservation violation count" = @"
select count(*)
from (
    select "PurchaseRequisitionLineId", "LocationKey", count(*)
    from nexa.stock_reservations
    where "CorrelationId" like 'REV868C1-%' and "Status" = 'Active'
    group by "PurchaseRequisitionLineId", "LocationKey"
    having count(*) > 1
) d;
"@.Trim()
        "Duplicate PendingRFQ handoff violation count" = @"
select count(*)
from (
    select "PurchaseRequisitionLineId", count(*)
    from nexa.purchase_requirement_handoffs
    where "CorrelationId" like 'REV868C1-%' and "Status" = 'PendingRFQ'
    group by "PurchaseRequisitionLineId"
    having count(*) > 1
) d;
"@.Trim()
        "Missing location evidence counts" = @"
select 'stock_reservations_missing_location=' || count(*)
from nexa.stock_reservations
where "CorrelationId" like 'REV868C1-%' and ("WarehouseId" is null or "LocationKey" is null or length("LocationKey") = 0)
union all select 'stock_check_lines_missing_location=' || count(*)
from nexa.stock_availability_check_lines l
join nexa.stock_availability_checks c on c."Id" = l."StockAvailabilityCheckId"
where c."CorrelationId" like 'REV868C1-CHECK-%' and (l."WarehouseId" is null or l."LocationKey" is null or length(l."LocationKey") = 0)
union all select 'handoffs_missing_location=' || count(*)
from nexa.purchase_requirement_handoffs
where "CorrelationId" like 'REV868C1-%' and ("WarehouseId" is null or "LocationKey" is null or length("LocationKey") = 0);
"@.Trim()
    }
}
function Remove-SqlNonExecutableText([string]$Sql) {
    $builder = [System.Text.StringBuilder]::new($Sql.Length)
    $i = 0
    while ($i -lt $Sql.Length) {
        $ch = $Sql[$i]
        $next = if ($i + 1 -lt $Sql.Length) { $Sql[$i + 1] } else { [char]0 }
        if ($ch -eq "'"[0]) {
            [void]$builder.Append(' ')
            $i++
            while ($i -lt $Sql.Length) {
                if ($Sql[$i] -eq "'"[0]) {
                    [void]$builder.Append(' ')
                    if ($i + 1 -lt $Sql.Length -and $Sql[$i + 1] -eq "'"[0]) { $i += 2; continue }
                    $i++
                    break
                }
                [void]$builder.Append(' ')
                $i++
            }
            continue
        }
        if ($ch -eq '"'[0]) {
            [void]$builder.Append(' ')
            $i++
            while ($i -lt $Sql.Length) {
                [void]$builder.Append(' ')
                if ($Sql[$i] -eq '"'[0]) {
                    if ($i + 1 -lt $Sql.Length -and $Sql[$i + 1] -eq '"'[0]) { $i += 2; continue }
                    $i++
                    break
                }
                $i++
            }
            continue
        }
        if ($ch -eq '-'[0] -and $next -eq '-'[0]) {
            [void]$builder.Append('  ')
            $i += 2
            while ($i -lt $Sql.Length -and $Sql[$i] -notin "`r", "`n") { [void]$builder.Append(' '); $i++ }
            continue
        }
        if ($ch -eq '/'[0] -and $next -eq '*'[0]) {
            [void]$builder.Append('  ')
            $i += 2
            while ($i -lt $Sql.Length) {
                if ($Sql[$i] -eq '*'[0] -and $i + 1 -lt $Sql.Length -and $Sql[$i + 1] -eq '/'[0]) { [void]$builder.Append('  '); $i += 2; break }
                [void]$builder.Append(' ')
                $i++
            }
            continue
        }
        [void]$builder.Append($ch)
        $i++
    }
    return $builder.ToString()
}
function Assert-SqlSafe([string]$Title, [string]$Sql) {
    if ([string]::IsNullOrWhiteSpace($Sql)) { throw "SQL '$Title' is empty." }
    $stripped = (Remove-SqlNonExecutableText $Sql).Trim()
    if (-not $stripped.EndsWith(';')) { throw "SQL '$Title' is missing a statement terminator." }
    $statementParts = @($stripped.Split(';') | ForEach-Object { $_.Trim() } | Where-Object { $_.Length -gt 0 })
    if ($statementParts.Count -ne 1) { throw "SQL '$Title' must contain exactly one executable statement." }
    $statement = $statementParts[0]
    if ($statement -notmatch '(?is)^\s*(select|with)\b') { throw "SQL '$Title' must start with SELECT or a read-only CTE." }
    if ($statement -match '(?is)\b(insert|update|delete|merge|create|alter|drop|truncate|grant|revoke|copy|call|do|execute|vacuum|analyze|refresh|listen|notify)\b') { throw "SQL '$Title' is not read-only." }
}
function Invoke-PsqlRead([string]$Sql) {
    $sqlFile = Join-Path ([System.IO.Path]::GetTempPath()) ("sess_nexa_rev868c1_resume_" + [Guid]::NewGuid().ToString("N") + ".sql")
    try {
        $readOnlySql = "begin transaction read only;`n$Sql`ncommit;"
        [System.IO.File]::WriteAllText($sqlFile, $readOnlySql, [System.Text.UTF8Encoding]::new($false))
        $old = $ErrorActionPreference
        $ErrorActionPreference = "Continue"
        try { $output = @(& $psql -h $HostName -p $Port -U $UserName -d $Database -v ON_ERROR_STOP=1 -At -f $sqlFile 2>&1); $exit = $LASTEXITCODE }
        finally { $ErrorActionPreference = $old }
        if ($exit -ne 0) { throw "psql failed with exit code $exit. $((($output | ForEach-Object { $_.ToString() }) -join "`n"))" }
        $filtered = @($output | Where-Object { $_.ToString() -notin @('BEGIN', 'COMMIT') })
        return ($filtered -join "`n")
    }
    finally { Remove-Item -LiteralPath $sqlFile -Force -ErrorAction SilentlyContinue }
}
function Add-Evidence([string]$Title, [string]$Sql) { $evidence[$Title] = Invoke-PsqlRead $Sql }
function Get-TestResultSummary([string]$TrxPath) {
    [xml]$trx = Get-Content -LiteralPath $TrxPath
    $ns = New-Object System.Xml.XmlNamespaceManager($trx.NameTable)
    $ns.AddNamespace("t", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")
    $counters = $trx.TestRun.ResultSummary.Counters
    $results = @($trx.SelectNodes("//t:UnitTestResult", $ns) | ForEach-Object { "$($_.testName)|$($_.outcome)" })
    return [pscustomobject]@{
        Total = $counters.total
        Passed = $counters.passed
        Failed = $counters.failed
        Skipped = $counters.notExecuted
        Results = ($results -join "`n")
    }
}
function Write-PlanReport([System.Collections.Specialized.OrderedDictionary]$Sql) {
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
    Add-Report "# REV868C1 Isolated Resume Final Evidence Plan"
    Add-Report ""
    Add-Report "- Mode: GeneratePlanOnly"
    Add-Report "- Expected host: $HostName"
    Add-Report "- Expected port: $Port"
    Add-Report "- Expected database: $Database"
    Add-Report "- PostgreSQL user parameter: $UserName"
    Add-Report "- No password requested and no PostgreSQL connection attempted in this mode."
    Add-Report "- Resume verifier contains no EF migration application; it requires all 8 migrations already present."
    foreach ($entry in $Sql.GetEnumerator()) { Add-Report "## $($entry.Key)"; Add-Report '```sql'; Add-Report ([string]$entry.Value); Add-Report '```' }
    Write-Host "REV868C1 isolated resume plan report: $reportFile"
}

try {
    Write-Section "REV868C1 resume no-secret prechecks"
    Assert-SafePgIdentifier $Database "Verification database name"
    Assert-SafePgIdentifier $UserName "PostgreSQL user name"
    if ($HostName -ne "localhost" -or $Port -ne 5432 -or $Database -ne "sess_nexaerp_rev868_verify") { throw "This resume verifier is permanently restricted to localhost:5432 / sess_nexaerp_rev868_verify." }
    if ($blockedDatabaseNames -contains $Database -or $Database -match 'rev861') { throw "Blocked database target: $Database" }
    $sql = Get-ResumeSql
    foreach ($entry in $sql.GetEnumerator()) { Assert-SqlSafe $entry.Key ([string]$entry.Value) }
    if ($GeneratePlanOnly) {
        Write-PlanReport $sql
        foreach ($entry in $sql.GetEnumerator()) { Write-Output "-- $($entry.Key)"; Write-Output ([string]$entry.Value); Write-Output "" }
        return
    }

    $gitExe = Resolve-GitExecutable $GitPath
    $dotnet = Resolve-ExecutablePath $dotnetPath ".NET executable"
    $psql = Resolve-ExecutablePath $psqlPath "psql.exe"
    Set-Location $repoRoot
    $gitStatus = (& $gitExe status --short) -join "`n"
    $gitCommit = (& $gitExe rev-parse HEAD).Trim()
    if ($gitStatus) { throw "Git status is not clean before REV868C1 resume verification." }

    Write-Host "Expected host: $HostName"
    Write-Host "Expected port: $Port"
    Write-Host "Expected database: $Database"
    Write-Host "PostgreSQL user parameter: $UserName"
    Write-Section "REV868C1 secure isolated resume verification"
    $securePassword = Read-Host -AsSecureString "Enter PostgreSQL password for isolated REV868C1 verification database only"
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    try { $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
    finally { if ($bstr -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) } }
    $env:PGPASSWORD = $plainPassword
    $env:REV868C1_POSTGRES = "Host=$HostName;Port=$Port;Database=$Database;Username=$UserName;Password=$plainPassword"
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
    New-Item -ItemType Directory -Force -Path $trxDir | Out-Null

    Add-Evidence "Session identity" $sql["Session identity"]
    Add-Evidence "Unsafe target rejection evidence" $sql["Unsafe target rejection evidence"]
    if ($evidence["Session identity"] -notmatch "database=$Database") { throw "Connected database did not match isolated verification database." }
    if ($evidence["Session identity"] -match "database=sess_nexaerp(\r?\n|$)" -or $evidence["Unsafe target rejection evidence"].Trim() -ne "not_blocked_name") { throw "Refusing unsafe database target." }
    Add-Evidence "Eight migration IDs" $sql["Eight migration IDs"]
    foreach ($migrationId in $expectedMigrationIds) {
        if ($evidence["Eight migration IDs"] -notmatch ([regex]::Escape($migrationId) + "\|1")) { throw "Expected migration missing or duplicated: $migrationId" }
    }

    Write-Section "Run PostgreSQL-backed REV868C1 resume tests"
    Set-Location $targetRoot
    $testOutput = @(& $dotnet test .\SESS.NexaERP.slnx --configuration Release --filter "Rev868C1PostgresWorkflowVerificationTests|AuthorizationIntegrationTests" --logger "trx;LogFileName=$trxName" --results-directory $trxDir 2>&1)
    if ($LASTEXITCODE -ne 0) { throw "dotnet test failed. $((($testOutput | ForEach-Object { $_.ToString() }) -join "`n"))" }
    $trxPath = Join-Path $trxDir $trxName
    $testSummary = Get-TestResultSummary $trxPath

    foreach ($entry in $sql.GetEnumerator()) {
        if ($entry.Key -in @("Session identity", "Unsafe target rejection evidence", "Eight migration IDs")) { continue }
        Add-Evidence $entry.Key ([string]$entry.Value)
    }

    Add-Report "# REV868C1 Isolated Resume Final Evidence Report"
    Add-Report ""
    Add-Report "- Source commit: $gitCommit"
    Add-Report "- Expected database: $Database"
    Add-Report "- Mode: resume only; no migration apply/remove/update command executed."
    Add-Report "- TRX path: $trxPath"
    Add-Report "- Test total: $($testSummary.Total); passed: $($testSummary.Passed); failed: $($testSummary.Failed); skipped: $($testSummary.Skipped)"
    Add-Report "- Real OIDC provider/token testing remains pending."
    foreach ($entry in $evidence.GetEnumerator()) { Add-Report "## $($entry.Key)"; Add-Report '```text'; Add-Report ([string]$entry.Value); Add-Report '```' }
    Add-Report "## Named test results"
    Add-Report '```text'
    Add-Report $testSummary.Results
    Add-Report '```'
    Add-Report "## Test output tail"
    Add-Report '```text'
    Add-Report (($testOutput | Select-Object -Last 80) -join "`n")
    Add-Report '```'
    Write-Host "REV868C1 isolated resume final evidence report: $reportFile"
}
catch {
    $lineNumber = $_.InvocationInfo.ScriptLineNumber
    $errorType = $_.Exception.GetType().FullName
    $message = $_.Exception.Message
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
    Add-Report "# REV868C1 Isolated Resume Failure Report"
    Add-Report ""
    Add-Report "- Sanitized failure: true"
    Add-Report "- Script line: $lineNumber"
    Add-Report "- Error type: $errorType"
    Add-Report "- Error message: $message"
    Write-Host "REV868C1 isolated resume verification failed. Sanitized report: $reportFile"
    Write-Host "Failure line: $lineNumber"
    throw
}
finally {
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:\REV868C1_POSTGRES -ErrorAction SilentlyContinue
    if ($plainPassword) { $plainPassword = $null }
    if ($securePassword) { $securePassword.Dispose() }
}
