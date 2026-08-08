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
$pgDump = Join-Path $pgBin "pg_dump.exe"
$backupDir = Join-Path $targetRoot "backups\postgresql\pre-rev867"
$reportDir = Join-Path $targetRoot "local-evidence\rev867"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = Join-Path $backupDir "$Database`_pre_rev867_$timestamp.dump"
$reportFile = Join-Path $reportDir "rev867_master_foundation_verification_$timestamp.md"
$requiredSourceCommit = "TO_BE_FILLED_AFTER_COMMIT"
$requiredPreviousMigration = "20260808142353_Rev866CorrectiveStatusPermissionAudit"
$delimiter = [char]31

function Write-Section([string]$Text) { Write-Host ""; Write-Host "== $Text ==" }
function Assert-SafePgIdentifier([string]$Name, [string]$Label) { if ($Name -notmatch '^[A-Za-z_][A-Za-z0-9_]{0,62}$') { throw "$Label is not a safe PostgreSQL identifier." } }
function Quote-PgIdentifier([string]$Name) { Assert-SafePgIdentifier $Name "PostgreSQL identifier"; return '"' + $Name.Replace('"', '""') + '"' }
function Join-PgQualifiedIdentifier([string]$SchemaName, [string]$TableName) { return (Quote-PgIdentifier $SchemaName) + "." + (Quote-PgIdentifier $TableName) }
function Resolve-ExecutablePath([string]$Path, [string]$Label) { $r = Resolve-Path -LiteralPath $Path -ErrorAction Stop; $i = Get-Item -LiteralPath $r.Path -ErrorAction Stop; if (-not $i.Exists) { throw "$Label was not found: $Path" }; return $i.FullName }
function Resolve-GitExecutable([string]$ExplicitGitPath) {
    $candidates = New-Object System.Collections.Generic.List[string]
    if (-not [string]::IsNullOrWhiteSpace($ExplicitGitPath)) { $candidates.Add($ExplicitGitPath) }
    $cmd = Get-Command git.exe -ErrorAction SilentlyContinue; if ($cmd -and $cmd.Source) { $candidates.Add($cmd.Source) }
    $candidates.Add("C:\Program Files\Git\cmd\git.exe"); $candidates.Add("C:\Program Files\Git\bin\git.exe"); $candidates.Add("D:\Git\cmd\git.exe"); $candidates.Add("D:\Git\bin\git.exe"); $candidates.Add("D:\PortableGit\cmd\git.exe"); $candidates.Add("D:\PortableGit\bin\git.exe"); $candidates.Add("D:\Program Files\Git\cmd\git.exe"); $candidates.Add("D:\Program Files\Git\bin\git.exe"); $candidates.Add("D:\Program Files (x86)\Git\cmd\git.exe"); $candidates.Add("D:\Program Files (x86)\Git\bin\git.exe"); $candidates.Add("C:\Users\User\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\git\cmd\git.exe")
    foreach ($candidate in $candidates | Select-Object -Unique) { if (Test-Path -LiteralPath $candidate -PathType Leaf) { $item = Get-Item -LiteralPath $candidate; if ($item.Name -ne "git.exe") { throw "Resolved Git path is not git.exe: $($item.FullName)" }; $v = & $item.FullName --version; if ($LASTEXITCODE -eq 0 -and ($v -join "`n") -match '^git version ') { return $item.FullName } } }
    throw "git.exe was not found. Pass -GitPath with a valid git.exe path."
}
function Resolve-RipgrepExecutable { $cmd = Get-Command rg.exe -ErrorAction SilentlyContinue; if ($cmd -and $cmd.Source) { return $cmd.Source }; $root = "C:\Users\User\AppData\Local\OpenAI\Codex\bin"; if (Test-Path -LiteralPath $root) { $c = Get-ChildItem -LiteralPath $root -Filter rg.exe -Recurse -File -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1; if ($c) { return $c.FullName } }; return $null }
function Invoke-SecretScan([string]$Pattern, [string]$Root) { $rg = Resolve-RipgrepExecutable; if ($rg) { $scan = & $rg --pcre2 -n $Pattern $Root; $code = $LASTEXITCODE; if ($code -eq 0) { throw "Secret scan found prohibited patterns." }; if ($code -gt 1) { throw "Secret scanner failed with exit code $code." }; return "clean via rg.exe ($rg)" }; return "rg unavailable; run manual secret scan before approval" }
function Invoke-Psql([string]$Sql, [string]$Db = $Database) { Assert-SafePgIdentifier $Db "Database name"; $sqlFile = Join-Path ([System.IO.Path]::GetTempPath()) ("sess_nexa_rev867_" + [Guid]::NewGuid().ToString("N") + ".sql"); try { [System.IO.File]::WriteAllText($sqlFile, $Sql, [System.Text.UTF8Encoding]::new($false)); $old = $ErrorActionPreference; $ErrorActionPreference = "Continue"; try { $output = & $psql -h $HostName -p $Port -U $UserName -d $Db -v ON_ERROR_STOP=1 -At -f $sqlFile 2>&1; $exit = $LASTEXITCODE } finally { $ErrorActionPreference = $old }; if ($exit -ne 0) { throw "psql failed with exit code $exit. $(($output | ForEach-Object { $_.ToString() }) -join "`n")" }; return ($output -join "`n") } finally { Remove-Item -LiteralPath $sqlFile -Force -ErrorAction SilentlyContinue } }
function Get-HistoryTableInfo([string]$Db = $Database) { $raw = Invoke-Psql "select schemaname || chr(31) || tablename from pg_tables where tablename = '__EFMigrationsHistory' order by schemaname, tablename;" $Db; foreach ($line in @($raw -split "`n" | Where-Object { $_ })) { $parts = $line -split [regex]::Escape($delimiter), 2; $q = Join-PgQualifiedIdentifier $parts[0] $parts[1]; $rows = Invoke-Psql "SELECT `"MigrationId`" FROM $q ORDER BY `"MigrationId`";" $Db; if ($rows -match [regex]::Escape($requiredPreviousMigration)) { return [pscustomobject]@{ QualifiedTable = $q; Migrations = $rows } } }; throw "Required previous REV866C1 migration was not found." }
function Add-Report([string]$Text) { Add-Content -LiteralPath $reportFile -Value $Text -Encoding utf8 }
function Write-FailureReport([string]$Message) { New-Item -ItemType Directory -Force -Path $reportDir | Out-Null; Add-Report "# REV867 Verification Failed"; Add-Report ""; Add-Report "- Time: $(Get-Date -Format o)"; Add-Report "- Error: $Message"; Write-Host "REV867 verification failed. Sanitized report: $reportFile" }

$securePassword = $null; $plainPassword = $null; $apiProcess = $null; $testOutput = @(); $buildOutput = @(); $backupItem = $null; $backupHash = ""; $rev867AlreadyApplied = $false
try {
    Write-Section "REV867 no-secret prechecks"
    Assert-SafePgIdentifier $Database "Development database name"; Assert-SafePgIdentifier $UserName "PostgreSQL user name"
    if ($Database -ne "sess_nexaerp" -or $HostName -ne "localhost" -or $Port -ne 5432) { throw "This helper is restricted to sess_nexaerp on localhost:5432." }
    if ($MigrationName -ne "20260808151207_Rev867MasterFoundation") { throw "Only REV867 migration is allowed." }
    $gitExe = Resolve-GitExecutable $GitPath; $dotnet = Resolve-ExecutablePath $dotnetPath ".NET executable"; $psql = Resolve-ExecutablePath $psql "psql.exe"; $pgDump = Resolve-ExecutablePath $pgDump "pg_dump.exe"
    Set-Location $repoRoot; $gitStatus = (& $gitExe status --short) -join "`n"; $gitCommit = (& $gitExe rev-parse HEAD).Trim(); if ($gitStatus) { throw "Git status is not clean before REV867 database verification." }; if ($requiredSourceCommit -ne "TO_BE_FILLED_AFTER_COMMIT" -and $gitCommit -ne $requiredSourceCommit) { throw "Unexpected source commit: $gitCommit" }
    Write-Section "REV867 secure database verification"
    $securePassword = Read-Host -AsSecureString "Enter PostgreSQL password for local development database only"; $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword); try { $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) } finally { if ($bstr -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) } }
    $env:PGPASSWORD = $plainPassword; $env:ConnectionStrings__NexaErp = "Host=$HostName;Port=$Port;Database=$Database;Username=$UserName;Password=$plainPassword"
    New-Item -ItemType Directory -Force -Path $backupDir | Out-Null; New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
    Write-Section "Pre-REV867 database checks"
    $dbName = Invoke-Psql "select current_database();"
    if ($dbName -ne $Database) { throw "Connected to unexpected database: $dbName" }
    $history = Get-HistoryTableInfo $Database
    $rev867AlreadyApplied = $history.Migrations -match [regex]::Escape($MigrationName)

    if ($rev867AlreadyApplied) {
        Write-Section "Pre-REV867 backup"
        Write-Host "REV867 migration is already applied. Resuming verification with the latest existing pre-REV867 backup."
        $backupItem = Get-ChildItem -LiteralPath $backupDir -Filter "$Database`_pre_rev867_*.dump" -File -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if (-not $backupItem) { throw "REV867 is already applied, but no pre-REV867 backup was found in $backupDir." }
        if ($backupItem.Length -le 0) { throw "Existing pre-REV867 backup file has zero size." }
        $backupFile = $backupItem.FullName
        $backupHash = (Get-FileHash -LiteralPath $backupFile -Algorithm SHA256).Hash
    } else {
        Write-Section "Pre-REV867 backup"
        & $pgDump -h $HostName -p $Port -U $UserName -d $Database -F c -f $backupFile
        if ($LASTEXITCODE -ne 0) { throw "pg_dump failed with exit code $LASTEXITCODE." }
        $backupItem = Get-Item -LiteralPath $backupFile
        if ($backupItem.Length -le 0) { throw "Backup file has zero size." }
        $backupHash = (Get-FileHash -LiteralPath $backupFile -Algorithm SHA256).Hash

        Write-Section "Apply REV867 migration"
        Set-Location $targetRoot
        & $dotnet ef database update $MigrationName --project .\src\SESS.NexaERP.Infrastructure\SESS.NexaERP.Infrastructure.csproj --context NexaErpDbContext
        if ($LASTEXITCODE -ne 0) { throw "EF database update failed with exit code $LASTEXITCODE." }
    }

    Write-Section "Post-migration evidence"
    $after = Get-HistoryTableInfo $Database
    $migrations = Invoke-Psql "SELECT `"MigrationId`" FROM $($after.QualifiedTable) ORDER BY `"MigrationId`";"
    if ($migrations -notmatch [regex]::Escape($MigrationName)) { throw "REV867 migration is not present after verification." }
    $pageCount = Invoke-Psql "select count(*) from nexa.page_definitions where `"PageKey`" in ('masters.items','masters.vendors','masters.customers','masters.warehouses','masters.rack-bins');"
    $permissionCount = Invoke-Psql 'select count(*) from nexa.role_page_permissions;'
    $supportTables = Invoke-Psql "select count(*) from information_schema.tables where table_schema = 'nexa' and table_name in ('item_categories','item_subcategories','uoms','manufacturers','vendor_contacts','vendor_addresses','customer_contacts','customer_addresses','master_status_history','master_approval_history','master_attachment_metadata');"

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
    $scanEvidence = Invoke-SecretScan $scanPattern $targetRoot
    Add-Report "# REV867 Master Foundation Verification"; Add-Report ""; Add-Report "- Source commit: $gitCommit"; Add-Report "- Migration: $MigrationName"; Add-Report "- Database: $Database on ${HostName}:$Port"; Add-Report "- Backup path: $backupFile"; Add-Report "- Backup bytes: $($backupItem.Length)"; Add-Report "- Backup SHA-256: $backupHash"; Add-Report "- Required master page count: $pageCount"; Add-Report "- Role-page permission count: $permissionCount"; Add-Report "- Normalized support table count: $supportTables"; Add-Report "- Secret scan: $scanEvidence"; Add-Report "- Restore verification database sess_nexaerp_restore_verify_rev866 was not modified or dropped."; Add-Report "- Transactions PR/RFQ/PO/GRN/stock movements were not implemented in REV867."; Add-Report ""; Add-Report "## Applied migrations"; Add-Report '```text'; Add-Report $migrations; Add-Report '```'; Add-Report ""; Add-Report "## Test output"; Add-Report '```text'; Add-Report (($testOutput | Select-Object -Last 20) -join "`n"); Add-Report '```'
    Write-Host "REV867 verification report: $reportFile"; Write-Host "Backup file: $backupFile"; Write-Host "Backup SHA-256: $backupHash"
} catch { Write-FailureReport $_.Exception.Message; Write-Host $_.Exception.Message; throw } finally { if ($apiProcess -and -not $apiProcess.HasExited) { Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue }; Remove-Item Env:\ConnectionStrings__NexaErp -ErrorAction SilentlyContinue; Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue; if ($plainPassword) { $plainPassword = $null }; if ($securePassword) { $securePassword.Dispose() } }
