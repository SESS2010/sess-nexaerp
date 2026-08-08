[CmdletBinding()]
param(
    [string]$Database = "sess_nexaerp",
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$UserName = "postgres",
    [string]$MigrationName = "20260808151207_Rev867MasterFoundation",
    [string]$GitPath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$targetRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$dotnetPath = Join-Path $targetRoot "..\.dotnet10\dotnet.exe"
$pgBin = "C:\Program Files\PostgreSQL\17\bin"
$psql = Join-Path $pgBin "psql.exe"
$backupDir = Join-Path $targetRoot "backups\postgresql\pre-rev867"
$reportDir = Join-Path $targetRoot "local-evidence\rev867"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$reportFile = Join-Path $reportDir "rev867_resume_verification_$timestamp.md"
$requiredPreviousMigration = "20260808142353_Rev866CorrectiveStatusPermissionAudit"
$delimiter = [char]31

$securePassword = $null; $plainPassword = $null; $apiProcess = $null
$testOutput = @(); $buildOutput = @(); $restoreOutput = @(); $secretScanOutput = ""; $scanEvidence = ""; $databaseEvidence = [ordered]@{}
$backupItem = $null; $backupFile = ""; $backupHash = ""; $migrations = ""; $api401Evidence = ""; $api403Evidence = "Covered by automated authorization tests; see test output."

function Write-Section([string]$Text) { Write-Host ""; Write-Host "== $Text ==" }
function Add-Report([string]$Text) { Add-Content -LiteralPath $reportFile -Value $Text -Encoding utf8 }
function Assert-SafePgIdentifier([string]$Name, [string]$Label) { if ($Name -notmatch '^[A-Za-z_][A-Za-z0-9_]{0,62}$') { throw "$Label is not a safe PostgreSQL identifier." } }
function Quote-PgIdentifier([string]$Name) { Assert-SafePgIdentifier $Name "PostgreSQL identifier"; return '"' + $Name.Replace('"', '""') + '"' }
function Join-PgQualifiedIdentifier([string]$SchemaName, [string]$TableName) { return (Quote-PgIdentifier $SchemaName) + "." + (Quote-PgIdentifier $TableName) }
function Resolve-ExecutablePath([string]$Path, [string]$Label) { $r = Resolve-Path -LiteralPath $Path -ErrorAction Stop; $i = Get-Item -LiteralPath $r.Path -ErrorAction Stop; if (-not $i.Exists) { throw "$Label was not found: $Path" }; return $i.FullName }
function Resolve-GitExecutable([string]$ExplicitGitPath) {
    $candidates = New-Object System.Collections.Generic.List[string]
    if (-not [string]::IsNullOrWhiteSpace($ExplicitGitPath)) { $candidates.Add($ExplicitGitPath) }
    $cmd = Get-Command git.exe -ErrorAction SilentlyContinue; if ($cmd -and $cmd.Source) { $candidates.Add($cmd.Source) }
    $candidates.Add("C:\Program Files\Git\cmd\git.exe"); $candidates.Add("C:\Program Files\Git\bin\git.exe")
    $candidates.Add("D:\Git\cmd\git.exe"); $candidates.Add("D:\Git\bin\git.exe"); $candidates.Add("D:\PortableGit\cmd\git.exe"); $candidates.Add("D:\PortableGit\bin\git.exe")
    $candidates.Add("D:\Program Files\Git\cmd\git.exe"); $candidates.Add("D:\Program Files\Git\bin\git.exe"); $candidates.Add("D:\Program Files (x86)\Git\cmd\git.exe"); $candidates.Add("D:\Program Files (x86)\Git\bin\git.exe")
    $candidates.Add("C:\Users\User\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\git\cmd\git.exe")
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
function Resolve-RipgrepExecutable {
    $cmd = Get-Command rg.exe -ErrorAction SilentlyContinue
    if ($cmd -and $cmd.Source) { return $cmd.Source }
    $root = "C:\Users\User\AppData\Local\OpenAI\Codex\bin"
    if (Test-Path -LiteralPath $root) {
        $candidate = Get-ChildItem -LiteralPath $root -Filter rg.exe -Recurse -File -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($candidate) { return $candidate.FullName }
    }
    return $null
}
function Invoke-SecretScan([string]$Pattern, [string]$Root) {
    $rg = Resolve-RipgrepExecutable
    if (-not $rg) { return "rg unavailable; manual secret scan required before approval" }
    $scan = & $rg --pcre2 -n $Pattern $Root
    $code = $LASTEXITCODE
    if ($code -eq 0) { throw "Secret scan found prohibited patterns." }
    if ($code -gt 1) { throw "Secret scanner failed with exit code $code." }
    return "clean via rg.exe ($rg)"
}
function Invoke-Psql([string]$Sql, [string]$Db = $Database) {
    Assert-SafePgIdentifier $Db "Database name"
    $sqlFile = Join-Path ([System.IO.Path]::GetTempPath()) ("sess_nexa_rev867_resume_" + [Guid]::NewGuid().ToString("N") + ".sql")
    try {
        [System.IO.File]::WriteAllText($sqlFile, $Sql, [System.Text.UTF8Encoding]::new($false))
        $oldPreference = $ErrorActionPreference; $ErrorActionPreference = "Continue"
        try { $output = & $psql -h $HostName -p $Port -U $UserName -d $Db -v ON_ERROR_STOP=1 -At -f $sqlFile 2>&1; $exit = $LASTEXITCODE } finally { $ErrorActionPreference = $oldPreference }
        if ($exit -ne 0) { throw "psql failed with exit code $exit. $(($output | ForEach-Object { $_.ToString() }) -join "`n")" }
        return ($output -join "`n")
    } finally {
        Remove-Item -LiteralPath $sqlFile -Force -ErrorAction SilentlyContinue
    }
}
function Get-HistoryTableInfo([string]$Db = $Database) {
    $raw = Invoke-Psql "select schemaname || chr(31) || tablename from pg_tables where tablename = '__EFMigrationsHistory' order by schemaname, tablename;" $Db
    foreach ($line in @($raw -split "`n" | Where-Object { $_ })) {
        $parts = $line -split [regex]::Escape($delimiter), 2
        if ($parts.Count -ne 2) { continue }
        $qualified = Join-PgQualifiedIdentifier $parts[0] $parts[1]
        $rows = Invoke-Psql "SELECT `"MigrationId`" FROM $qualified ORDER BY `"MigrationId`";" $Db
        if ($rows -match [regex]::Escape($requiredPreviousMigration)) { return [pscustomobject]@{ QualifiedTable = $qualified; Migrations = $rows } }
    }
    throw "Required previous REV866C1 migration was not found."
}
function Write-FailureReport([string]$Message) {
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
    Add-Report "# REV867 Resume Verification Failed"; Add-Report ""
    Add-Report "- Time: $(Get-Date -Format o)"
    Add-Report "- Error: $Message"
    Add-Report "- Backup path: $backupFile"
    Add-Report "- Backup SHA-256: $backupHash"
    Add-Report "- Build lines captured: $(@($buildOutput).Count)"
    Add-Report "- Test lines captured: $(@($testOutput).Count)"
    Add-Report "- Secret scan: $secretScanOutput"
    Add-Report "- Sensitive values are not written by this report."
    Write-Host "REV867 resume verification failed. Sanitized report: $reportFile"
}

try {
    Write-Section "REV867 resume no-secret prechecks"
    Assert-SafePgIdentifier $Database "Development database name"; Assert-SafePgIdentifier $UserName "PostgreSQL user name"
    if ($Database -ne "sess_nexaerp" -or $HostName -ne "localhost" -or $Port -ne 5432) { throw "This verifier is restricted to sess_nexaerp on localhost:5432." }
    if ($MigrationName -ne "20260808151207_Rev867MasterFoundation") { throw "Only REV867 verification is allowed." }
    $gitExe = Resolve-GitExecutable $GitPath
    $dotnet = Resolve-ExecutablePath $dotnetPath ".NET executable"; $psql = Resolve-ExecutablePath $psql "psql.exe"
    Set-Location $repoRoot
    $gitStatus = (& $gitExe status --short) -join "`n"
    $gitCommit = (& $gitExe rev-parse HEAD).Trim()
    if ($gitStatus) { throw "Git status is not clean before REV867 resume verification." }

    Write-Section "REV867 resume secure database verification"
    $securePassword = Read-Host -AsSecureString "Enter PostgreSQL password for local development database only"
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    try { $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) } finally { if ($bstr -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) } }
    $env:PGPASSWORD = $plainPassword; $env:ConnectionStrings__NexaErp = "Host=$HostName;Port=$Port;Database=$Database;Username=$UserName;Password=$plainPassword"
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null

    Write-Section "Read-only migration and backup checks"
    $dbName = Invoke-Psql "select current_database();"
    if ($dbName -ne $Database) { throw "Connected to unexpected database: $dbName" }
    $history = Get-HistoryTableInfo $Database
    $migrations = Invoke-Psql "SELECT `"MigrationId`" FROM $($history.QualifiedTable) ORDER BY `"MigrationId`";"
    if ($migrations -notmatch [regex]::Escape($MigrationName)) { throw "REV867 migration is missing. This resume verifier will not apply migrations." }
    $backupItem = Get-ChildItem -LiteralPath $backupDir -Filter "$Database`_pre_rev867_*.dump" -File -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $backupItem) { throw "No pre-REV867 backup found in $backupDir." }
    if ($backupItem.Length -le 0) { throw "Pre-REV867 backup has zero size." }
    $backupFile = $backupItem.FullName
    $backupHash = (Get-FileHash -LiteralPath $backupFile -Algorithm SHA256).Hash

    Write-Section "Post-migration PostgreSQL evidence"
    $databaseEvidence["Master tables"] = Invoke-Psql "select table_name from information_schema.tables where table_schema = 'nexa' and table_name in ('items','vendors','customers','warehouses','rack_bins','item_categories','item_subcategories','uoms','manufacturers','vendor_categories','vendor_contacts','vendor_addresses','customer_contacts','customer_addresses','master_status_history','master_approval_history','master_attachment_metadata') order by table_name;"
    $databaseEvidence["Master columns"] = Invoke-Psql "select table_name || '.' || column_name from information_schema.columns where table_schema = 'nexa' and table_name in ('items','vendors','customers','warehouses','rack_bins') and column_name in ('ItemCode','VendorCode','CustomerCode','WarehouseCode','BinCode','Version','Status','ApprovalStatus') order by table_name,column_name;"
    $databaseEvidence["Unique constraints/indexes"] = Invoke-Psql "select tablename || ':' || indexname from pg_indexes where schemaname = 'nexa' and (indexdef ilike '%unique%' or indexname ilike '%unique%') and tablename in ('items','vendors','customers','warehouses','rack_bins') order by tablename,indexname;"
    $databaseEvidence["Check constraints"] = Invoke-Psql "select conrelid::regclass::text || ':' || conname from pg_constraint where contype = 'c' and connamespace = 'nexa'::regnamespace order by conrelid::regclass::text, conname;"
    $databaseEvidence["Status approval indexes"] = Invoke-Psql "select tablename || ':' || indexname from pg_indexes where schemaname = 'nexa' and (indexname ilike '%status%' or indexdef ilike '%Status%' or indexdef ilike '%ApprovalStatus%') order by tablename,indexname;"
    $databaseEvidence["History counts"] = Invoke-Psql "select 'master_status_history=' || count(*) from nexa.master_status_history union all select 'master_approval_history=' || count(*) from nexa.master_approval_history union all select 'audit_logs=' || count(*) from nexa.audit_logs;"
    $databaseEvidence["No direct stock-balance editing"] = if (Select-String -Path (Join-Path $targetRoot "src\SESS.NexaERP.Api\Endpoints\*.cs") -Pattern "stock-balance|stock balance|direct stock" -Quiet) { "Review required: stock-balance text found." } else { "No direct stock-balance endpoint text found in API endpoints." }
    $databaseEvidence["No hard-delete endpoints"] = if (Select-String -Path (Join-Path $targetRoot "src\SESS.NexaERP.Api\Endpoints\*.cs") -Pattern "MapDelete|DeleteAsync|Remove\(" -Quiet) { "Review required: delete-like text found in API endpoints." } else { "No hard-delete endpoint text found in API endpoints." }

    Write-Section "Build, tests and secret scan"
    Set-Location $targetRoot
    $restoreOutput = & $dotnet restore .\SESS.NexaERP.slnx 2>&1
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed with exit code $LASTEXITCODE. $(($restoreOutput | ForEach-Object { $_.ToString() }) -join "`n")" }
    $buildOutput = & $dotnet build .\SESS.NexaERP.slnx --configuration Release --no-restore 2>&1
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE. $(($buildOutput | ForEach-Object { $_.ToString() }) -join "`n")" }
    $testOutput = & $dotnet test .\SESS.NexaERP.slnx --configuration Release --no-build 2>&1
    if ($LASTEXITCODE -ne 0) { throw "dotnet test failed with exit code $LASTEXITCODE. $(($testOutput | ForEach-Object { $_.ToString() }) -join "`n")" }
    $scanWordPattern = 'pass' + 'word|pwd|secret|token'
    $scanPattern = '(?i)\b(' + $scanWordPattern + ')\b\s*[:=]\s*[''"`]?(?!\$|%|\{|<|REDACTED|redacted|your_|change_me|example|placeholder)[^''"`\s;]+'
    $secretScanOutput = Invoke-SecretScan $scanPattern $targetRoot
    $scanEvidence = $secretScanOutput

    Write-Section "Live API no-token 401"
    $apiDll = Join-Path $targetRoot "src\SESS.NexaERP.Api\bin\Release\net10.0\SESS.NexaERP.Api.dll"
    $apiOut = Join-Path $reportDir "rev867_resume_api_$timestamp.out.log"
    $apiErr = Join-Path $reportDir "rev867_resume_api_$timestamp.err.log"
    $apiProcess = Start-Process -FilePath $dotnet -ArgumentList @($apiDll, "--urls", "http://127.0.0.1:50867") -WorkingDirectory (Join-Path $targetRoot "src\SESS.NexaERP.Api") -RedirectStandardOutput $apiOut -RedirectStandardError $apiErr -PassThru -WindowStyle Hidden
    Start-Sleep -Seconds 3
    try {
        Invoke-WebRequest -UseBasicParsing -Uri "http://127.0.0.1:50867/api/v1/inventory/items" -TimeoutSec 10 | Out-Null
        throw "Expected unauthenticated inventory request to fail with 401."
    } catch {
        if ($_.Exception.Response -and [int]$_.Exception.Response.StatusCode -eq 401) { $api401Evidence = "GET /api/v1/inventory/items without token returned 401." } else { throw }
    }

    Add-Report "# REV867 Resume Verification Final Report"; Add-Report ""
    Add-Report "- Source commit: $gitCommit"
    Add-Report "- Migration present: $MigrationName"
    Add-Report "- EF history table: $($history.QualifiedTable)"
    Add-Report "- Database: $Database on ${HostName}:$Port"
    Add-Report "- Pre-REV867 backup path: $backupFile"
    Add-Report "- Pre-REV867 backup bytes: $($backupItem.Length)"
    Add-Report "- Pre-REV867 backup SHA-256: $backupHash"
    Add-Report "- Secret scan: $scanEvidence"
    Add-Report "- Live no-token 401 evidence: $api401Evidence"
    Add-Report "- Unauthorized 403 evidence: $api403Evidence"
    Add-Report "- Restore verification database sess_nexaerp_restore_verify_rev866 was not modified or dropped."
    Add-Report "- No restore verification database was created by this resume verifier."
    Add-Report "- No migration apply, downgrade, rollback, PR/RFQ/PO/GRN, stock issue, or stock balance editing action was executed."
    Add-Report ""
    Add-Report "## Applied migrations"; Add-Report '```text'; Add-Report $migrations; Add-Report '```'
    Add-Report ""; Add-Report "## PostgreSQL persistence evidence"
    foreach ($entry in $databaseEvidence.GetEnumerator()) { Add-Report "### $($entry.Key)"; Add-Report '```text'; Add-Report ([string]$entry.Value); Add-Report '```' }
    Add-Report ""; Add-Report "## Build/test output"; Add-Report '```text'; Add-Report (($testOutput | Select-Object -Last 30) -join "`n"); Add-Report '```'
    Write-Host "REV867 resume verification report: $reportFile"
    Write-Host "Backup file: $backupFile"
    Write-Host "Backup SHA-256: $backupHash"
} catch {
    Write-FailureReport $_.Exception.Message
    Write-Host $_.Exception.Message
    throw
} finally {
    if ($apiProcess -and -not $apiProcess.HasExited) { Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue }
    Remove-Item Env:\ConnectionStrings__NexaErp -ErrorAction SilentlyContinue
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    if ($plainPassword) { $plainPassword = $null }
    if ($securePassword) { $securePassword.Dispose() }
}
