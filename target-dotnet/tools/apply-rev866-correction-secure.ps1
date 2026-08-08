[CmdletBinding()]
param(
    [string]$Database = "sess_nexaerp",
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$UserName = "postgres",
    [string]$MigrationName = "20260808142353_Rev866CorrectiveStatusPermissionAudit",
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
$backupDir = Join-Path $targetRoot "backups\postgresql\post-rev866-pre-correction"
$reportDir = Join-Path $targetRoot "local-evidence\rev866"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = Join-Path $backupDir "$Database`_post_rev866_pre_correction_$timestamp.dump"
$reportFile = Join-Path $reportDir "rev866_corrective_checkpoint_$timestamp.md"
$requiredRev866Migration = "20260808123411_Rev866EmployeePermissionMatrix"
$requiredBaselineCommit = "330807171ce7ba85cc30a984f7467893eb32559a"
$requiredHelperBaselineCommit = "3dd53466b1067cc4f7dfc6098fa87e64f47446d4"
$historyDelimiter = [char]31

function Write-Section([string]$Text) {
    Write-Host ""
    Write-Host "== $Text =="
}

function Assert-SafePgIdentifier([string]$Name, [string]$Label) {
    if ($Name -notmatch '^[A-Za-z_][A-Za-z0-9_]{0,62}$') {
        throw "$Label is not a safe PostgreSQL identifier."
    }
}

function Quote-PgIdentifier([string]$Name) {
    Assert-SafePgIdentifier $Name "PostgreSQL identifier"
    return '"' + $Name.Replace('"', '""') + '"'
}

function Join-PgQualifiedIdentifier([string]$SchemaName, [string]$TableName) {
    return (Quote-PgIdentifier $SchemaName) + "." + (Quote-PgIdentifier $TableName)
}

function Resolve-ExecutablePath([string]$Path, [string]$Label) {
    $resolved = Resolve-Path -LiteralPath $Path -ErrorAction Stop
    $item = Get-Item -LiteralPath $resolved.Path -ErrorAction Stop
    if (-not $item.Exists) { throw "$Label was not found: $Path" }
    return $item.FullName
}

function Resolve-GitExecutable([string]$ExplicitGitPath) {
    $candidates = New-Object System.Collections.Generic.List[string]
    if (-not [string]::IsNullOrWhiteSpace($ExplicitGitPath)) {
        $candidates.Add($ExplicitGitPath)
    }

    $command = Get-Command git.exe -ErrorAction SilentlyContinue
    if ($command -and $command.Source) {
        $candidates.Add($command.Source)
    }

    $candidates.Add("C:\Program Files\Git\cmd\git.exe")
    $candidates.Add("C:\Program Files\Git\bin\git.exe")
    $candidates.Add("D:\Git\cmd\git.exe")
    $candidates.Add("D:\Git\bin\git.exe")
    $candidates.Add("D:\PortableGit\cmd\git.exe")
    $candidates.Add("D:\PortableGit\bin\git.exe")
    $candidates.Add("D:\Program Files\Git\cmd\git.exe")
    $candidates.Add("D:\Program Files\Git\bin\git.exe")
    $candidates.Add("D:\Program Files (x86)\Git\cmd\git.exe")
    $candidates.Add("D:\Program Files (x86)\Git\bin\git.exe")
    $candidates.Add("C:\Users\User\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\git\cmd\git.exe")

    foreach ($candidate in $candidates | Select-Object -Unique) {
        if ([string]::IsNullOrWhiteSpace($candidate)) { continue }
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            $item = Get-Item -LiteralPath $candidate
            if ($item.Name -ne "git.exe") { throw "Resolved Git path is not git.exe: $($item.FullName)" }
            $versionOutput = & $item.FullName --version
            if ($LASTEXITCODE -ne 0 -or ($versionOutput -join "`n") -notmatch '^git version ') {
                throw "Resolved Git executable did not return a valid version: $($item.FullName)"
            }
            return $item.FullName
        }
    }

    throw "git.exe was not found. Pass -GitPath with a valid git.exe path."
}

function Resolve-RipgrepExecutable {
    $command = Get-Command rg.exe -ErrorAction SilentlyContinue
    if ($command -and $command.Source -and (Test-Path -LiteralPath $command.Source -PathType Leaf)) {
        return $command.Source
    }

    $codexBinRoot = "C:\Users\User\AppData\Local\OpenAI\Codex\bin"
    if (Test-Path -LiteralPath $codexBinRoot -PathType Container) {
        $candidate = Get-ChildItem -LiteralPath $codexBinRoot -Filter rg.exe -Recurse -File -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1
        if ($candidate) { return $candidate.FullName }
    }

    return $null
}

function Invoke-SecretScan([string]$Pattern, [string]$Root) {
    $rgExe = Resolve-RipgrepExecutable
    if ($rgExe) {
        $secretScan = & $rgExe --pcre2 -n $Pattern $Root
        $secretScanExitCode = $LASTEXITCODE
        if ($secretScanExitCode -eq 0) { throw "Secret scan found prohibited patterns." }
        if ($secretScanExitCode -gt 1) { throw "Secret scanner failed with exit code $secretScanExitCode." }
        return "clean in target-dotnet via rg.exe ($rgExe)"
    }

    $excludedPathPattern = '\\(\.git|bin|obj|backups|local-evidence)\\'
    $matches = @(Get-ChildItem -LiteralPath $Root -Recurse -File -Force -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch $excludedPathPattern } |
        Select-String -Pattern $Pattern -ErrorAction SilentlyContinue)
    if ($matches.Count -gt 0) { throw "Secret scan found prohibited patterns." }
    return "clean in target-dotnet via PowerShell Select-String fallback"
}

function Invoke-Psql([string]$Sql, [string]$Db = $Database) {
    Assert-SafePgIdentifier $Db "Database name"
    $sqlFile = Join-Path ([System.IO.Path]::GetTempPath()) ("sess_nexa_rev866c1_" + [Guid]::NewGuid().ToString("N") + ".sql")
    try {
        $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
        [System.IO.File]::WriteAllText($sqlFile, $Sql, $utf8NoBom)
        $previousErrorActionPreference = $ErrorActionPreference
        $ErrorActionPreference = "Continue"
        try {
            $output = & $psql -h $HostName -p $Port -U $UserName -d $Db -v ON_ERROR_STOP=1 -At -f $sqlFile 2>&1
            $exitCode = $LASTEXITCODE
        }
        finally {
            $ErrorActionPreference = $previousErrorActionPreference
        }

        if ($exitCode -ne 0) {
            $message = ($output | ForEach-Object { $_.ToString() }) -join "`n"
            if ([string]::IsNullOrWhiteSpace($message)) { $message = "psql failed without diagnostic output." }
            throw "psql failed with exit code $exitCode. $message"
        }
        return ($output -join "`n")
    }
    finally {
        if ($sqlFile -and (Test-Path -LiteralPath $sqlFile -PathType Leaf)) {
            Remove-Item -LiteralPath $sqlFile -Force -ErrorAction SilentlyContinue
        }
    }
}

function Get-HistoryTableInfo([string]$Db = $Database) {
    $tablesRaw = Invoke-Psql "select schemaname || chr(31) || tablename from pg_tables where tablename = '__EFMigrationsHistory' order by case when schemaname = 'public' then 0 when schemaname = 'nexa' then 1 else 2 end, schemaname, tablename;" $Db
    $historyTables = @()
    foreach ($line in @($tablesRaw -split "`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
        $parts = $line -split [regex]::Escape($historyDelimiter), 2
        if ($parts.Count -ne 2) { throw "Could not parse EF migration history table discovery output safely." }
        Assert-SafePgIdentifier $parts[0] "Migration history schema"
        Assert-SafePgIdentifier $parts[1] "Migration history table"
        $historyTables += [pscustomobject]@{
            Schema = $parts[0]
            Table = $parts[1]
            QualifiedTable = Join-PgQualifiedIdentifier $parts[0] $parts[1]
        }
    }

    if ($historyTables.Count -eq 0) { throw "No __EFMigrationsHistory table found in database $Db." }

    foreach ($historyTable in $historyTables) {
        $migrationRows = Invoke-Psql "SELECT `"MigrationId`" FROM $($historyTable.QualifiedTable) ORDER BY `"MigrationId`";" $Db
        if ($migrationRows -match [regex]::Escape($requiredRev866Migration)) {
            return [pscustomobject]@{
                Schema = $historyTable.Schema
                Table = $historyTable.Table
                QualifiedTable = $historyTable.QualifiedTable
                Migrations = $migrationRows
            }
        }
    }

    throw "REV866 migration was not found in any discovered EF migration history table."
}

function Add-Report([string]$Text) {
    Add-Content -LiteralPath $reportFile -Value $Text -Encoding utf8
}

function Get-HttpStatus([string]$Uri) {
    try {
        $response = Invoke-WebRequest -UseBasicParsing -Uri $Uri -TimeoutSec 10
        return [int]$response.StatusCode
    }
    catch {
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
            return [int]$_.Exception.Response.StatusCode
        }
        throw
    }
}

function Get-LatestCorrectionBackup {
    $existingBackups = @(Get-ChildItem -LiteralPath $backupDir -Filter "$Database`_post_rev866_pre_correction_*.dump" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending)
    if ($existingBackups.Count -eq 0) { throw "Corrective migration is already applied, but no post-REV866/pre-correction backup was found." }
    $existingBackup = $existingBackups[0]
    if ($existingBackup.Length -le 0) { throw "Existing correction backup has zero size: $($existingBackup.FullName)" }
    return $existingBackup
}

function Write-FailureReport([string]$Message) {
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
    Add-Report "# REV866 Corrective Checkpoint Failed"
    Add-Report ""
    Add-Report "- Time: $(Get-Date -Format o)"
    Add-Report "- Error: $Message"
    Write-Host "REV866 corrective checkpoint failed. Sanitized failure report: $reportFile"
}

$securePassword = $null
$plainPassword = $null
$apiProcess = $null
try {
    Write-Section "REV866 corrective no-secret prechecks"
    Assert-SafePgIdentifier $Database "Development database name"
    Assert-SafePgIdentifier $UserName "PostgreSQL user name"
    if ($Database -ne "sess_nexaerp") { throw "This helper is restricted to development database sess_nexaerp." }
    if ($HostName -ne "localhost" -or $Port -ne 5432) { throw "This helper is restricted to localhost:5432." }
    if ($MigrationName -ne "20260808142353_Rev866CorrectiveStatusPermissionAudit") { throw "Only the REV866 corrective migration target is allowed." }

    $gitExe = Resolve-GitExecutable $GitPath
    $dotnet = Resolve-ExecutablePath $dotnetPath ".NET executable"
    $psql = Resolve-ExecutablePath $psql "psql.exe"
    $pgDump = Resolve-ExecutablePath $pgDump "pg_dump.exe"
    $migrationFile = Resolve-ExecutablePath (Join-Path $targetRoot "src\SESS.NexaERP.Infrastructure\Persistence\Migrations\20260808142353_Rev866CorrectiveStatusPermissionAudit.cs") "REV866 corrective migration"

    Set-Location $repoRoot
    $gitStatus = (& $gitExe status --short) -join "`n"
    $gitCommit = (& $gitExe rev-parse HEAD).Trim()
    if ($gitStatus) { throw "Git status is not clean before corrective database verification." }
    & $gitExe merge-base --is-ancestor $requiredBaselineCommit HEAD | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Required REV866 source baseline $requiredBaselineCommit is not in history." }
    & $gitExe merge-base --is-ancestor $requiredHelperBaselineCommit HEAD | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Required secure helper baseline $requiredHelperBaselineCommit is not in history." }

    Write-Section "REV866 corrective secure database verification"
    $securePassword = Read-Host -AsSecureString "Enter PostgreSQL password for local development database only"
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    try {
        $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    }
    finally {
        if ($bstr -ne [IntPtr]::Zero) {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
        }
    }

    $env:PGPASSWORD = $plainPassword
    $env:ConnectionStrings__NexaErp = "Host=$HostName;Port=$Port;Database=$Database;Username=$UserName;Password=$plainPassword"
    New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null

    Write-Section "Pre-correction database checks"
    $dbName = Invoke-Psql "select current_database();"
    if ($dbName -ne $Database) { throw "Connected to unexpected database: $dbName" }
    $historyInfo = Get-HistoryTableInfo $Database
    $correctionAlreadyApplied = $historyInfo.Migrations -match [regex]::Escape($MigrationName)
    $applicationMode = "applied during this corrective checkpoint"

    if ($correctionAlreadyApplied) {
        Write-Section "Backup"
        Write-Host "REV866 corrective migration is already applied. Resuming verification with latest correction backup."
        $backupItem = Get-LatestCorrectionBackup
        $backupFile = $backupItem.FullName
        $backupHash = (Get-FileHash -LiteralPath $backupFile -Algorithm SHA256).Hash
        $applicationMode = "already applied before this resume run"
    }
    else {
        Write-Section "Post-REV866/pre-correction backup"
        & $pgDump -h $HostName -p $Port -U $UserName -d $Database -F c -f $backupFile
        if ($LASTEXITCODE -ne 0) { throw "pg_dump failed with exit code $LASTEXITCODE." }
        $backupItem = Get-Item -LiteralPath $backupFile
        if ($backupItem.Length -le 0) { throw "Correction backup file was created with zero size." }
        $backupHash = (Get-FileHash -LiteralPath $backupFile -Algorithm SHA256).Hash

        Write-Section "Apply REV866 corrective migration"
        Set-Location $targetRoot
        & $dotnet ef database update $MigrationName --project .\src\SESS.NexaERP.Infrastructure\SESS.NexaERP.Infrastructure.csproj --context NexaErpDbContext
        if ($LASTEXITCODE -ne 0) { throw "EF database update failed with exit code $LASTEXITCODE." }
    }

    Write-Section "Database evidence"
    $historyInfoAfter = Get-HistoryTableInfo $Database
    $migrationsAfter = Invoke-Psql "SELECT `"MigrationId`" FROM $($historyInfoAfter.QualifiedTable) ORDER BY `"MigrationId`";"
    if ($migrationsAfter -notmatch [regex]::Escape($MigrationName)) { throw "Corrective migration is missing after update." }
    $employeeCount = Invoke-Psql 'select count(*) from nexa.employees;'
    $statusHistoryCount = Invoke-Psql 'select count(*) from nexa.employee_status_history;'
    $initialStatusHistoryCount = Invoke-Psql "select count(*) from nexa.employee_status_history where `"Reason`" like 'Initial approved employee seed/import%REV866C1%';"
    $initialStatusDuplicateCount = Invoke-Psql "select count(*) from (select `"EmployeeId`" from nexa.employee_status_history where `"Reason`" like 'Initial approved employee seed/import%REV866C1%' group by `"EmployeeId`" having count(*) > 1) d;"
    $activeRoleCount = Invoke-Psql 'select count(*) from nexa.roles where "IsActive" = true;'
    $pageCount = Invoke-Psql 'select count(*) from nexa.page_definitions where "IsActive" = true;'
    $permissionCount = Invoke-Psql 'select count(*) from nexa.role_page_permissions;'
    $missingMatrixRows = Invoke-Psql 'select count(*) from (select r."Code" from nexa.roles r left join nexa.role_page_permissions p on p."RoleId" = r."Id" where r."IsActive" = true group by r."Code" having count(p."Id") <> (select count(*) from nexa.page_definitions where "IsActive" = true)) missing;'
    $roleMatrixSummary = Invoke-Psql 'select r."Code" || chr(31) || r."Name" || chr(31) || case when r."IsActive" then ''Active'' else ''Inactive'' end || chr(31) || coalesce(string_agg(distinct e."EmployeeCode", '','' order by e."EmployeeCode"), ''None'') || chr(31) || count(distinct p."Id") from nexa.roles r left join nexa.employee_role_assignments era on era."RoleId" = r."Id" left join nexa.employees e on e."Id" = era."EmployeeId" left join nexa.role_page_permissions p on p."RoleId" = r."Id" where r."IsActive" = true group by r."Code", r."Name", r."IsActive" order by r."Code";'
    $auditEvidence = Invoke-Psql "select `"Action`" || chr(31) || `"EntityName`" || chr(31) || `"Result`" || chr(31) || `"CorrelationId`" from nexa.audit_logs where `"CorrelationId`" like 'REV866C1_%' order by `"Action`";"

    if ($employeeCount -ne "39") { throw "Expected 39 employees, found $employeeCount." }
    if ($initialStatusHistoryCount -ne "39") { throw "Expected 39 initial status-history rows, found $initialStatusHistoryCount." }
    if ($initialStatusDuplicateCount -ne "0") { throw "Initial status-history duplicate rows found: $initialStatusDuplicateCount." }
    if ($permissionCount -ne "684") { throw "Expected 684 role-page permission rows, found $permissionCount." }
    if ($missingMatrixRows -ne "0") { throw "Some active roles are missing page-permission rows." }

    Write-Section "Build, tests, and secret scan"
    Set-Location $targetRoot
    & $dotnet restore .\SESS.NexaERP.slnx
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }
    & $dotnet build .\SESS.NexaERP.slnx --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed." }
    $testOutput = & $dotnet test .\SESS.NexaERP.slnx --configuration Release --no-build 2>&1
    $testExit = $LASTEXITCODE
    if ($testExit -ne 0) { throw "dotnet test failed with exit code $testExit. $($testOutput -join "`n")" }
    Set-Location $repoRoot
    $secretPattern = '(?i)\b(password|pwd|secret|token)\b\s*[:=]\s*[''"`]?(?!\$|%|\{|<|REDACTED|redacted|your_|change_me|example|placeholder)[^''"`\s;]+'
    $secretScanEvidence = Invoke-SecretScan -Pattern $secretPattern -Root $targetRoot

    Write-Section "Live API no-token 401"
    $apiPort = 58966
    $apiOut = Join-Path $reportDir "rev866_corrective_api_$timestamp.out.log"
    $apiErr = Join-Path $reportDir "rev866_corrective_api_$timestamp.err.log"
    $env:ASPNETCORE_URLS = "http://127.0.0.1:$apiPort"
    Set-Location $targetRoot
    $apiProcess = Start-Process -FilePath $dotnet -ArgumentList @("run", "--project", ".\src\SESS.NexaERP.Api\SESS.NexaERP.Api.csproj", "--configuration", "Release", "--no-build") -WorkingDirectory $targetRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $apiOut -RedirectStandardError $apiErr
    $liveStatus = $null
    for ($attempt = 1; $attempt -le 30; $attempt++) {
        Start-Sleep -Seconds 1
        if ($apiProcess.HasExited) { throw "API process exited before no-token check. See $apiErr" }
        try {
            $liveStatus = Get-HttpStatus "http://127.0.0.1:$apiPort/api/v1/employees"
            break
        }
        catch {
            if ($attempt -eq 30) { throw }
        }
    }
    if ($liveStatus -ne 401) { throw "Expected live no-token 401, got $liveStatus." }

    Add-Report "# REV866 Corrective Checkpoint Evidence"
    Add-Report ""
    Add-Report "- Corrective revision: REV866C1"
    Add-Report "- Corrective migration: $MigrationName"
    Add-Report "- Source commit: $gitCommit"
    Add-Report "- Database: $Database on ${HostName}:$Port"
    Add-Report "- EF migration history table: $($historyInfoAfter.QualifiedTable)"
    Add-Report "- Backup path: $backupFile"
    Add-Report "- Backup size bytes: $($backupItem.Length)"
    Add-Report "- Backup SHA-256: $backupHash"
    Add-Report "- Migration application mode: $applicationMode"
    Add-Report "- Employee count: $employeeCount"
    Add-Report "- Employee status history count: $statusHistoryCount"
    Add-Report "- Initial REV866C1 status history count: $initialStatusHistoryCount"
    Add-Report "- Initial status-history duplicate employee count: $initialStatusDuplicateCount"
    Add-Report "- Active role count: $activeRoleCount"
    Add-Report "- Active page count: $pageCount"
    Add-Report "- Role-page permission row count: $permissionCount"
    Add-Report "- Missing active role matrix rows: $missingMatrixRows"
    Add-Report "- Live no-token protected employee API status: $liveStatus"
    Add-Report "- Secret scan: $secretScanEvidence"
    Add-Report "- Restore verification database: sess_nexaerp_restore_verify_rev866 was not modified or dropped."
    Add-Report "- OIDC blocker: real external OIDC token testing remains pending until authority/audience/provider are approved."
    Add-Report ""
    Add-Report "## Applied migrations"
    Add-Report '```text'
    Add-Report $migrationsAfter
    Add-Report '```'
    Add-Report ""
    Add-Report "## Role matrix summary"
    Add-Report '```text'
    Add-Report "RoleCode<US>RoleName<US>Status<US>EmployeeAssignments<US>PermissionRows"
    Add-Report $roleMatrixSummary
    Add-Report '```'
    Add-Report ""
    Add-Report "## Audit evidence"
    Add-Report '```text'
    Add-Report "Action<US>EntityName<US>Result<US>CorrelationId"
    Add-Report $auditEvidence
    Add-Report '```'
    Add-Report ""
    Add-Report "## Test output"
    Add-Report '```text'
    Add-Report (($testOutput | Select-Object -Last 20) -join "`n")
    Add-Report '```'
    Add-Report ""
    Add-Report "## Rollback"
    Add-Report "Restore from the recorded backup into a separate database first for validation. If management approves rollback of the development database, restore the backup to sess_nexaerp while the API is stopped, then rerun migration verification. Do not touch live REV861."

    Write-Host "REV866 corrective checkpoint report: $reportFile"
    Write-Host "Backup file: $backupFile"
    Write-Host "Backup SHA-256: $backupHash"
}
catch {
    Write-FailureReport $_.Exception.Message
    Write-Host $_.Exception.Message
    throw
}
finally {
    if ($apiProcess -and -not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
    }
    Remove-Item Env:\ConnectionStrings__NexaErp -ErrorAction SilentlyContinue
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:\ASPNETCORE_URLS -ErrorAction SilentlyContinue
    if ($plainPassword) { $plainPassword = $null }
    if ($securePassword) { $securePassword.Dispose() }
}
