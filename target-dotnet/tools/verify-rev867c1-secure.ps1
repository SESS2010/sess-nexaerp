[CmdletBinding()]
param(
    [string]$Database = "sess_nexaerp_rev867c1_verify",
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$UserName = "postgres",
    [string]$MigrationName = "20260808160435_Rev867C1Corrections",
    [string]$GitPath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$targetRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$dotnetPath = Join-Path $targetRoot "..\.dotnet10\dotnet.exe"
$pgBin = "C:\Program Files\PostgreSQL\17\bin"
$psql = Join-Path $pgBin "psql.exe"
$reportDir = Join-Path $targetRoot "local-evidence\rev867c1"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$reportFile = Join-Path $reportDir "rev867c1_verification_$timestamp.md"
$delimiter = [char]31

$securePassword = $null; $plainPassword = $null; $testOutput = @(); $buildOutput = @(); $secretScanOutput = ""; $migrationRows = ""; $databaseEvidence = [ordered]@{}

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
    $candidates.Add("C:\Program Files\Git\cmd\git.exe"); $candidates.Add("C:\Program Files\Git\bin\git.exe"); $candidates.Add("D:\Git\cmd\git.exe"); $candidates.Add("D:\Git\bin\git.exe"); $candidates.Add("D:\PortableGit\cmd\git.exe"); $candidates.Add("D:\PortableGit\bin\git.exe"); $candidates.Add("C:\Users\User\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\git\cmd\git.exe")
    foreach ($candidate in $candidates | Select-Object -Unique) { if (Test-Path -LiteralPath $candidate -PathType Leaf) { $item = Get-Item -LiteralPath $candidate; if ($item.Name -ne "git.exe") { throw "Resolved Git path is not git.exe: $($item.FullName)" }; $v = & $item.FullName --version; if ($LASTEXITCODE -eq 0 -and ($v -join "`n") -match '^git version ') { return $item.FullName } } }
    throw "git.exe was not found. Pass -GitPath with a valid git.exe path."
}
function Resolve-RipgrepExecutable { $cmd = Get-Command rg.exe -ErrorAction SilentlyContinue; if ($cmd -and $cmd.Source) { return $cmd.Source }; $root = "C:\Users\User\AppData\Local\OpenAI\Codex\bin"; if (Test-Path -LiteralPath $root) { $c = Get-ChildItem -LiteralPath $root -Filter rg.exe -Recurse -File -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1; if ($c) { return $c.FullName } }; return $null }
function Invoke-SecretScan([string]$Pattern, [string]$Root) { $rg = Resolve-RipgrepExecutable; if (-not $rg) { return "rg unavailable; manual secret scan required before approval" }; $scan = & $rg --pcre2 -n $Pattern $Root; $code = $LASTEXITCODE; if ($code -eq 0) { throw "Secret scan found prohibited patterns." }; if ($code -gt 1) { throw "Secret scanner failed with exit code $code." }; return "clean via rg.exe ($rg)" }
function Invoke-Psql([string]$Sql, [string]$Db = $Database) { Assert-SafePgIdentifier $Db "Database name"; $sqlFile = Join-Path ([System.IO.Path]::GetTempPath()) ("sess_nexa_rev867c1_" + [Guid]::NewGuid().ToString("N") + ".sql"); try { [System.IO.File]::WriteAllText($sqlFile, $Sql, [System.Text.UTF8Encoding]::new($false)); $old = $ErrorActionPreference; $ErrorActionPreference = "Continue"; try { $output = & $psql -h $HostName -p $Port -U $UserName -d $Db -v ON_ERROR_STOP=1 -At -f $sqlFile 2>&1; $exit = $LASTEXITCODE } finally { $ErrorActionPreference = $old }; if ($exit -ne 0) { throw "psql failed with exit code $exit. $(($output | ForEach-Object { $_.ToString() }) -join "`n")" }; return ($output -join "`n") } finally { Remove-Item -LiteralPath $sqlFile -Force -ErrorAction SilentlyContinue } }
function Get-HistoryTableInfo { $raw = Invoke-Psql "select schemaname || chr(31) || tablename from pg_tables where tablename = '__EFMigrationsHistory' order by schemaname, tablename;"; foreach ($line in @($raw -split "`n" | Where-Object { $_ })) { $parts = $line -split [regex]::Escape($delimiter), 2; if ($parts.Count -eq 2) { return Join-PgQualifiedIdentifier $parts[0] $parts[1] } }; throw "EF migrations history table was not found after migration." }
function Write-FailureReport([string]$Message) { New-Item -ItemType Directory -Force -Path $reportDir | Out-Null; Add-Report "# REV867C1 Verification Failed"; Add-Report ""; Add-Report "- Time: $(Get-Date -Format o)"; Add-Report "- Error: $Message"; Add-Report "- Database: $Database"; Add-Report "- Sensitive values are not written by this report."; Write-Host "REV867C1 verification failed. Sanitized report: $reportFile" }

try {
    Write-Section "REV867C1 no-secret prechecks"
    Assert-SafePgIdentifier $Database "Verification database name"; Assert-SafePgIdentifier $UserName "PostgreSQL user name"
    if ($Database -ne "sess_nexaerp_rev867c1_verify" -or $HostName -ne "localhost" -or $Port -ne 5432) { throw "This helper is restricted to sess_nexaerp_rev867c1_verify on localhost:5432." }
    if ($MigrationName -ne "20260808160435_Rev867C1Corrections") { throw "Only REV867C1 migration is allowed." }
    $gitExe = Resolve-GitExecutable $GitPath; $dotnet = Resolve-ExecutablePath $dotnetPath ".NET executable"; $psql = Resolve-ExecutablePath $psql "psql.exe"
    Set-Location $repoRoot; $gitStatus = (& $gitExe status --short) -join "`n"; $gitCommit = (& $gitExe rev-parse HEAD).Trim(); if ($gitStatus) { throw "Git status is not clean before REV867C1 verification." }

    Write-Section "REV867C1 secure database verification"
    $securePassword = Read-Host -AsSecureString "Enter PostgreSQL password for local verification database only"; $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword); try { $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) } finally { if ($bstr -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) } }
    $env:PGPASSWORD = $plainPassword; $env:ConnectionStrings__NexaErp = "Host=$HostName;Port=$Port;Database=$Database;Username=$UserName;Password=$plainPassword"; $env:REV867C1_POSTGRES = $env:ConnectionStrings__NexaErp
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null

    Write-Section "Verify existing verification database"
    $dbName = Invoke-Psql "select current_database();"
    if ($dbName -ne $Database) { throw "Connected to unexpected database: $dbName" }

    Write-Section "Apply migrations to verification database only"
    Set-Location $targetRoot
    & $dotnet ef database update $MigrationName --project .\src\SESS.NexaERP.Infrastructure\SESS.NexaERP.Infrastructure.csproj --startup-project .\src\SESS.NexaERP.Api\SESS.NexaERP.Api.csproj --context NexaErpDbContext
    if ($LASTEXITCODE -ne 0) { throw "EF database update failed with exit code $LASTEXITCODE." }
    $historyTable = Get-HistoryTableInfo
    $migrationRows = Invoke-Psql "SELECT `"MigrationId`" FROM $historyTable ORDER BY `"MigrationId`";"
    if ($migrationRows -notmatch [regex]::Escape($MigrationName)) { throw "REV867C1 migration was not found after update." }

    Write-Section "Build, tests and secret scan"
    $buildOutput = & $dotnet build .\SESS.NexaERP.slnx --configuration Release 2>&1
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed. $(($buildOutput | ForEach-Object { $_.ToString() }) -join "`n")" }
    $testOutput = & $dotnet test .\SESS.NexaERP.slnx --configuration Release --no-build --filter "Rev867C1PostgresVerificationTests|Rev867MasterFoundationTests" 2>&1
    if ($LASTEXITCODE -ne 0) { throw "dotnet test failed. $(($testOutput | ForEach-Object { $_.ToString() }) -join "`n")" }
    $scanWordPattern = 'pass' + 'word|pwd|secret|token'
    $scanPattern = '(?i)\b(' + $scanWordPattern + ')\b\s*[:=]\s*[''"`]?(?!\$|%|\{|<|REDACTED|redacted|your_|change_me|example|placeholder)[^''"`\s;]+'
    $secretScanOutput = Invoke-SecretScan $scanPattern $targetRoot

    Write-Section "Post-test PostgreSQL evidence"
    $databaseEvidence["History counts"] = Invoke-Psql "select 'master_status_history=' || count(*) from nexa.master_status_history union all select 'master_approval_history=' || count(*) from nexa.master_approval_history union all select 'audit_logs=' || count(*) from nexa.audit_logs;"
    $databaseEvidence["Self approval denial audit"] = Invoke-Psql "select count(*) from nexa.audit_logs where `"Module`"='Security' and `"Action`"='Denied' and `"EntityName`"='Item' and `"Result`"='Failure';"
    $databaseEvidence["Organization scoped records"] = Invoke-Psql "select 'customers=' || count(*) from nexa.customers where `"CustomerCode`" like 'C1-CUST-%' union all select 'vendors=' || count(*) from nexa.vendors where `"VendorCode`" like 'C1-VEND-%';"
    $databaseEvidence["Commercial mask source"] = "Customer credit and vendor bank values are masked in API DTO projection when CanViewCommercialValues is false; PostgreSQL scoped records include controlled credit/bank metadata for verification."

    Add-Report "# REV867C1 Verification Report"; Add-Report ""; Add-Report "- Source commit: $gitCommit"; Add-Report "- Verification database: $Database on ${HostName}:$Port"; Add-Report "- Migration applied/present: $MigrationName"; Add-Report "- Secret scan: $secretScanOutput"; Add-Report "- Runtime temporary authentication: not added; PostgreSQL verification uses test-only services in the test project."; Add-Report "- Real OIDC provider/token testing remains production-readiness blocker."; Add-Report "- Live REV861, sess_nexaerp and restore databases are not touched by this helper."; Add-Report ""; Add-Report "## Applied migrations"; Add-Report '```text'; Add-Report $migrationRows; Add-Report '```'; Add-Report ""; Add-Report "## PostgreSQL evidence"; foreach($entry in $databaseEvidence.GetEnumerator()){ Add-Report "### $($entry.Key)"; Add-Report '```text'; Add-Report ([string]$entry.Value); Add-Report '```' }; Add-Report ""; Add-Report "## Test output"; Add-Report '```text'; Add-Report (($testOutput | Select-Object -Last 40) -join "`n"); Add-Report '```'
    Write-Host "REV867C1 verification report: $reportFile"
} catch { Write-FailureReport $_.Exception.Message; Write-Host $_.Exception.Message; throw } finally { Remove-Item Env:\ConnectionStrings__NexaErp -ErrorAction SilentlyContinue; Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue; Remove-Item Env:\REV867C1_POSTGRES -ErrorAction SilentlyContinue; if ($plainPassword) { $plainPassword = $null }; if ($securePassword) { $securePassword.Dispose() } }
