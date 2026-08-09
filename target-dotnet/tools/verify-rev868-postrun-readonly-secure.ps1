[CmdletBinding()]
param(
    [string]$Database = "sess_nexaerp",
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$UserName = "postgres",
    [switch]$GenerateSqlOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$targetRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$psqlPath = "C:\Program Files\PostgreSQL\17\bin\psql.exe"
$reportDir = Join-Path $targetRoot "local-evidence\rev868"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$reportFile = Join-Path $reportDir "rev868_postrun_readonly_verification_$timestamp.md"
$plainPassword = $null
$securePassword = $null

function Add-Report([string]$Text) { Add-Content -LiteralPath $reportFile -Value $Text -Encoding utf8 }
function Assert-SafePgIdentifier([string]$Name, [string]$Label) { if ($Name -notmatch '^[A-Za-z_][A-Za-z0-9_]{0,62}$') { throw "$Label is not a safe PostgreSQL identifier." } }
function Get-VerificationSql {
    [ordered]@{
        "Database identity" = "select 'database=' || current_database() union all select 'user=' || current_user union all select 'server_addr=' || coalesce(inet_server_addr()::text, 'local_socket') union all select 'server_port=' || inet_server_port()::text;"
        "Eight migration IDs through REV868" = @"
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
        "REV868 tables" = "select table_schema || '.' || table_name from information_schema.tables where table_schema = 'nexa' and table_name in ('purchase_requisitions','purchase_requisition_lines','purchase_number_sequences','stock_reservations','stock_availability_checks','stock_availability_check_lines','purchase_requirement_handoffs','purchase_requisition_status_history','purchase_requisition_approval_history','stock_reservation_history') order by 1;"
        "REV868 columns" = "select table_name || '.' || column_name || ':' || is_nullable || ':' || data_type from information_schema.columns where table_schema = 'nexa' and ((table_name='purchase_requisitions' and column_name in ('FinancialYear','PrSequence','PrNumber')) or (table_name in ('stock_reservations','stock_availability_check_lines','purchase_requirement_handoffs') and column_name in ('WarehouseId','RackBinId','LocationKey')) or (table_name='purchase_number_sequences' and column_name in ('OrganizationId','FinancialYear','Prefix','LastNumber'))) order by table_name,column_name;"
        "Indexes" = "select schemaname || '.' || tablename || ':' || indexname || ':' || indexdef from pg_catalog.pg_indexes where schemaname='nexa' and (tablename in ('purchase_requisitions','purchase_number_sequences','stock_reservations','stock_availability_check_lines','purchase_requirement_handoffs') or indexname ilike '%LocationKey%' or indexname ilike '%FinancialYear%') order by tablename,indexname;"
        "Check constraints" = "select n.nspname || '.' || c.relname || ':' || con.conname || ':' || pg_get_constraintdef(con.oid) from pg_catalog.pg_constraint con join pg_catalog.pg_class c on c.oid=con.conrelid join pg_catalog.pg_namespace n on n.oid=c.relnamespace where n.nspname='nexa' and con.contype='c' and con.conname in ('CK_purchase_number_sequences_last_number_nonnegative','CK_pr_lines_reconcile_requested','CK_stock_check_lines_quantities_valid','CK_purchase_route_limits_valid') order by c.relname,con.conname;"
        "Foreign keys" = "select n.nspname || '.' || c.relname || ':' || con.conname || ':' || pg_get_constraintdef(con.oid) from pg_catalog.pg_constraint con join pg_catalog.pg_class c on c.oid=con.conrelid join pg_catalog.pg_namespace n on n.oid=c.relnamespace where n.nspname='nexa' and con.contype='f' and c.relname in ('stock_reservations','stock_availability_check_lines','purchase_requirement_handoffs','purchase_requisition_lines') order by c.relname,con.conname;"
        "Purchase pages and permissions" = @"
select p."PageKey" || ':' || p."Title" || ':permissions=' || count(rp."Id")
from nexa.page_definitions p
left join nexa.role_page_permissions rp on rp."PageDefinitionId" = p."Id"
where p."PageKey" in ('purchase.requisitions','purchase.requisition-approvals','stores.stock-check','stores.reservations','purchase.requirement-handoff')
group by p."PageKey", p."Title"
order by p."PageKey";
"@.Trim()
        "Safe PR workflow counts" = @"
select 'purchase_requisitions=' || count(*) from nexa.purchase_requisitions
union all select 'purchase_requisition_lines=' || count(*) from nexa.purchase_requisition_lines
union all select 'stock_availability_checks=' || count(*) from nexa.stock_availability_checks
union all select 'stock_availability_check_lines=' || count(*) from nexa.stock_availability_check_lines
union all select 'stock_reservations=' || count(*) from nexa.stock_reservations
union all select 'stock_reservations_active=' || count(*) from nexa.stock_reservations where "Status" = 'Active'
union all select 'purchase_requirement_handoffs=' || count(*) from nexa.purchase_requirement_handoffs
union all select 'purchase_requirement_handoffs_pending_rfq=' || count(*) from nexa.purchase_requirement_handoffs where "Status" = 'PendingRFQ'
union all select 'purchase_requisition_status_history=' || count(*) from nexa.purchase_requisition_status_history
union all select 'purchase_requisition_approval_history=' || count(*) from nexa.purchase_requisition_approval_history
union all select 'stock_reservation_history=' || count(*) from nexa.stock_reservation_history
union all select 'audit_logs=' || count(*) from nexa.audit_logs;
"@.Trim()
    }
}
function Invoke-PsqlRead([string]$Sql) {
    $sqlFile = Join-Path ([System.IO.Path]::GetTempPath()) ("sess_nexa_rev868_verify_" + [Guid]::NewGuid().ToString("N") + ".sql")
    try {
        [System.IO.File]::WriteAllText($sqlFile, $Sql, [System.Text.UTF8Encoding]::new($false))
        $output = & $psql -h $HostName -p $Port -U $UserName -d $Database -v ON_ERROR_STOP=1 -At -f $sqlFile 2>&1
        if ($LASTEXITCODE -ne 0) { throw "psql failed with exit code $LASTEXITCODE." }
        return ($output -join "`n")
    }
    finally { Remove-Item -LiteralPath $sqlFile -Force -ErrorAction SilentlyContinue }
}

try {
    Assert-SafePgIdentifier $Database "Database name"
    Assert-SafePgIdentifier $UserName "PostgreSQL user name"
    if ($HostName -ne "localhost" -or $Port -ne 5432 -or $Database -ne "sess_nexaerp") { throw "Read-only verifier is restricted to localhost:5432 / sess_nexaerp." }
    $sql = Get-VerificationSql
    if ($GenerateSqlOnly) { foreach ($entry in $sql.GetEnumerator()) { Write-Output "-- $($entry.Key)"; Write-Output ([string]$entry.Value); Write-Output "" }; return }
    $psql = (Get-Item -LiteralPath $psqlPath -ErrorAction Stop).FullName
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
    Write-Host "Expected host: $HostName"
    Write-Host "Expected port: $Port"
    Write-Host "Expected database: $Database"
    $securePassword = Read-Host -AsSecureString "Enter PostgreSQL password for read-only REV868 verification only"
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    try { $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
    finally { if ($bstr -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) } }
    $env:PGPASSWORD = $plainPassword
    $env:NexaErp__ExpectedDatabase = $Database
    Add-Report "# REV868 Post-Run Read-Only Verification"
    foreach ($entry in $sql.GetEnumerator()) {
        $result = Invoke-PsqlRead ([string]$entry.Value)
        if ($entry.Key -eq "Database identity" -and $result -notmatch "database=$Database") { throw "Connected database mismatch." }
        Add-Report "## $($entry.Key)"
        Add-Report '```text'
        Add-Report $result
        Add-Report '```'
    }
    Write-Host "REV868 read-only verification report: $reportFile"
}
finally {
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:\NexaErp__ExpectedDatabase -ErrorAction SilentlyContinue
    if ($plainPassword) { $plainPassword = $null }
    if ($securePassword) { $securePassword.Dispose() }
}
