[CmdletBinding()]
param(
    [string]$Database = "sess_nexaerp",
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$UserName = "postgres",
    [string]$MigrationName = "20260808123411_Rev866EmployeePermissionMatrix",
    [string]$RestoreVerifyDatabase = "sess_nexaerp_restore_verify_rev866",
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
$pgRestore = Join-Path $pgBin "pg_restore.exe"
$backupDir = Join-Path $targetRoot "backups\postgresql\pre-rev866"
$reportDir = Join-Path $targetRoot "local-evidence\rev866"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = Join-Path $backupDir "$Database`_pre_rev866_$timestamp.dump"
$reportFile = Join-Path $reportDir "rev866_database_runtime_verification_$timestamp.md"
$requiredBaselineCommit = "330807171ce7ba85cc30a984f7467893eb32559a"
$requiredPreviousMigrations = @("20260808110924_Phase1Foundation", "20260808114550_Phase1AuthorizationSeed")
$expectedHistorySchemaForCurrentEfConfig = "public"
$script:DiagnosticMigrationsBefore = "not checked"
$script:DiagnosticHistoryTables = "not checked"
$script:PsqlFieldDelimiter = [char]31

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

    throw "git.exe was not found. Pass -GitPath with a valid git.exe path or install Git after management approval."
}

function Resolve-RipgrepExecutable {
    $candidates = New-Object System.Collections.Generic.List[string]
    $command = Get-Command rg.exe -ErrorAction SilentlyContinue
    if ($command -and $command.Source) {
        $candidates.Add($command.Source)
    }

    $codexBinRoot = "C:\Users\User\AppData\Local\OpenAI\Codex\bin"
    if (Test-Path -LiteralPath $codexBinRoot -PathType Container) {
        foreach ($candidate in @(Get-ChildItem -LiteralPath $codexBinRoot -Filter rg.exe -Recurse -File -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -ExpandProperty FullName)) {
            $candidates.Add($candidate)
        }
    }

    foreach ($candidate in $candidates | Select-Object -Unique) {
        if ([string]::IsNullOrWhiteSpace($candidate)) { continue }
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            $item = Get-Item -LiteralPath $candidate
            if ($item.Name -ne "rg.exe") { continue }
            return $item.FullName
        }
    }

    return $null
}

function Invoke-SecretScan([string]$Pattern, [string]$Root) {
    $rgExe = Resolve-RipgrepExecutable
    if ($rgExe) {
        $secretScan = & $rgExe -n $Pattern $Root
        $secretScanExitCode = $LASTEXITCODE
        if ($secretScanExitCode -eq 0) { throw "Secret scan found prohibited patterns." }
        if ($secretScanExitCode -gt 1) { throw "Secret scanner failed with exit code $secretScanExitCode." }
        return "clean via rg.exe ($rgExe)"
    }

    $excludedPathPattern = '\\(\.git|bin|obj|backups|local-evidence)\\'
    $matches = @(Get-ChildItem -LiteralPath $Root -Recurse -File -Force -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch $excludedPathPattern } |
        Select-String -Pattern $Pattern -ErrorAction SilentlyContinue)
    if ($matches.Count -gt 0) { throw "Secret scan found prohibited patterns." }
    return "clean via PowerShell Select-String fallback"
}

function Get-LatestPreRev866Backup {
    $existingBackups = @(Get-ChildItem -LiteralPath $backupDir -Filter "$Database`_pre_rev866_*.dump" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending)
    if ($existingBackups.Count -eq 0) { throw "REV866 migration is already applied, but no existing pre-REV866 backup was found for resume verification." }
    $existingBackup = $existingBackups[0]
    if ($existingBackup.Length -le 0) { throw "Existing pre-REV866 backup has zero size: $($existingBackup.FullName)" }
    return $existingBackup
}

function Invoke-Psql([string]$Sql, [string]$Db = $Database) {
    Assert-SafePgIdentifier $Db "Database name"
    $sqlFile = Join-Path ([System.IO.Path]::GetTempPath()) ("sess_nexa_rev866_" + [Guid]::NewGuid().ToString("N") + ".sql")
    try {
        $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
        [System.IO.File]::WriteAllText($sqlFile, $Sql, $utf8NoBom)
        & $psql -h $HostName -p $Port -U $UserName -d $Db -v ON_ERROR_STOP=1 -At -f $sqlFile
    }
    finally {
        if ($sqlFile -and (Test-Path -LiteralPath $sqlFile -PathType Leaf)) {
            Remove-Item -LiteralPath $sqlFile -Force -ErrorAction SilentlyContinue
        }
    }
}

function Invoke-PsqlSafe([string]$Sql, [string]$Db = $Database) {
    $result = Invoke-Psql -Sql $Sql -Db $Db
    return ($result -join "`n")
}

function Add-Report([string]$Text) {
    Add-Content -LiteralPath $reportFile -Value $Text -Encoding utf8
}

function Get-HistoryTableInfo([string]$Db = $Database) {
    $tablesRaw = Invoke-PsqlSafe "select schemaname || chr(31) || tablename from pg_tables where tablename = '__EFMigrationsHistory' order by case when schemaname = 'public' then 0 when schemaname = 'nexa' then 1 else 2 end, schemaname, tablename;" $Db
    $historyTables = @()
    foreach ($line in @($tablesRaw -split "`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
        $parts = $line -split [regex]::Escape($script:PsqlFieldDelimiter), 2
        if ($parts.Count -ne 2) {
            throw "Could not parse EF migration history table discovery output safely."
        }
        Assert-SafePgIdentifier $parts[0] "Migration history schema"
        Assert-SafePgIdentifier $parts[1] "Migration history table"
        $historyTables += [pscustomobject]@{
            Schema = $parts[0]
            Table = $parts[1]
            QualifiedTable = Join-PgQualifiedIdentifier $parts[0] $parts[1]
        }
    }

    if ($historyTables.Count -eq 0) {
        throw "No __EFMigrationsHistory table found in database $Db."
    }

    foreach ($historyTable in $historyTables) {
        $migrationRows = Invoke-PsqlSafe "SELECT `"MigrationId`" FROM $($historyTable.QualifiedTable) ORDER BY `"MigrationId`";" $Db
        $hasAllRequired = $true
        foreach ($migration in $requiredPreviousMigrations) {
            if ($migrationRows -notmatch [regex]::Escape($migration)) {
                $hasAllRequired = $false
            }
        }

        if ($hasAllRequired) {
            return [pscustomobject]@{
                Schema = $historyTable.Schema
                Table = $historyTable.Table
                QualifiedTable = $historyTable.QualifiedTable
                Migrations = $migrationRows
                AllTables = (($historyTables | ForEach-Object { "$($_.Schema).$($_.Table)" }) -join ',')
            }
        }
    }

    $diagnostic = foreach ($historyTable in $historyTables) {
        $migrationRows = Invoke-PsqlSafe "SELECT `"MigrationId`" FROM $($historyTable.QualifiedTable) ORDER BY `"MigrationId`";" $Db
        "Schema: $($historyTable.Schema); Table: $($historyTable.Table); Relation: $($historyTable.QualifiedTable); Migration rows:`n$migrationRows"
    }
    throw "Required previous migrations were not found in any discovered __EFMigrationsHistory table. Discovered history tables: $((($historyTables | ForEach-Object { "$($_.Schema).$($_.Table)" }) -join ',')). Details: $($diagnostic -join ' | ')"
}

function Write-FailureReport([string]$Message) {
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
    Add-Content -LiteralPath $reportFile -Value "# REV866 Database Verification Failed" -Encoding utf8
    Add-Content -LiteralPath $reportFile -Value "" -Encoding utf8
    Add-Content -LiteralPath $reportFile -Value ("- Time: " + (Get-Date -Format o)) -Encoding utf8
    Add-Content -LiteralPath $reportFile -Value ("- Error: " + $Message) -Encoding utf8
    Add-Content -LiteralPath $reportFile -Value ("- Migration history tables: " + $script:DiagnosticHistoryTables) -Encoding utf8
    Add-Content -LiteralPath $reportFile -Value "- Migrations before failure:" -Encoding utf8
    Add-Content -LiteralPath $reportFile -Value '```text' -Encoding utf8
    Add-Content -LiteralPath $reportFile -Value $script:DiagnosticMigrationsBefore -Encoding utf8
    Add-Content -LiteralPath $reportFile -Value '```' -Encoding utf8
    Write-Host "REV866 verification failed. Sanitized failure report: $reportFile"
}

$securePassword = $null
$PlainPassword = $null
try {
    Write-Section "REV866 no-secret prechecks"
    Assert-SafePgIdentifier $Database "Development database name"
    Assert-SafePgIdentifier $UserName "PostgreSQL user name"
    Assert-SafePgIdentifier $RestoreVerifyDatabase "Restore verification database name"
    if ($Database -ne "sess_nexaerp") { throw "This helper is restricted to development database sess_nexaerp." }
    if ($HostName -ne "localhost" -or $Port -ne 5432) { throw "This helper is restricted to localhost:5432." }
    if ($RestoreVerifyDatabase -in @($Database, "postgres", "template0", "template1")) { throw "Restore verification database name is unsafe." }
    if ($MigrationName -ne "20260808123411_Rev866EmployeePermissionMatrix") { throw "Only the REV866 migration target is allowed." }

    $gitExe = Resolve-GitExecutable $GitPath
    $dotnet = Resolve-ExecutablePath $dotnetPath ".NET executable"
    $psql = Resolve-ExecutablePath $psql "psql.exe"
    $pgDump = Resolve-ExecutablePath $pgDump "pg_dump.exe"
    $pgRestore = Resolve-ExecutablePath $pgRestore "pg_restore.exe"

    Set-Location $repoRoot
    $gitStatus = (& $gitExe status --short) -join "`n"
    $gitCommit = (& $gitExe rev-parse HEAD).Trim()
    if ($gitStatus) { throw "Git status is not clean before database verification." }
    & $gitExe merge-base --is-ancestor $requiredBaselineCommit HEAD | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Required REV866 source baseline $requiredBaselineCommit is not in the current history." }

    Write-Section "REV866 secure database verification"
    $securePassword = Read-Host -AsSecureString "Enter PostgreSQL password for local development database only"
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    try {
        $PlainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    }
    finally {
        if ($bstr -ne [IntPtr]::Zero) {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
        }
    }

    $env:PGPASSWORD = $PlainPassword
    $env:ConnectionStrings__NexaErp = "Host=$HostName;Port=$Port;Database=$Database;Username=$UserName;Password=$PlainPassword"

    New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null

    Write-Section "Pre-migration database checks"
    $dbName = Invoke-PsqlSafe "select current_database();"
    if ($dbName -ne $Database) { throw "Connected to unexpected database: $dbName" }
    $historyInfo = Get-HistoryTableInfo $Database
    $script:DiagnosticHistoryTables = "Schema: $($historyInfo.Schema); Table: $($historyInfo.Table); Relation: $($historyInfo.QualifiedTable)"
    $script:DiagnosticMigrationsBefore = $historyInfo.Migrations
    if ($historyInfo.Schema -ne $expectedHistorySchemaForCurrentEfConfig) {
        throw "Existing EF migration history is in schema '$($historyInfo.Schema)', but current source has no MigrationsHistoryTable configuration and generated scripts are unqualified. Stop before applying REV866."
    }
    foreach ($migration in $requiredPreviousMigrations) {
        if ($historyInfo.Migrations -notmatch [regex]::Escape($migration)) { throw "$migration migration missing before REV866." }
    }
    $rev866AlreadyApplied = $historyInfo.Migrations -match [regex]::Escape($MigrationName)
    $migrationApplicationMode = "applied during this verification run"

    if ($rev866AlreadyApplied) {
        Write-Section "Backup"
        Write-Host "REV866 migration is already applied. Resuming verification with the latest existing pre-REV866 backup."
        $backupItem = Get-LatestPreRev866Backup
        $backupFile = $backupItem.FullName
        $backupHash = (Get-FileHash -LiteralPath $backupFile -Algorithm SHA256).Hash
        $migrationApplicationMode = "already applied before this resume run"
    }
    else {
        Write-Section "Backup"
        & $pgDump -h $HostName -p $Port -U $UserName -d $Database -F c -f $backupFile
        if ($LASTEXITCODE -ne 0) { throw "pg_dump failed with exit code $LASTEXITCODE." }
        $backupItem = Get-Item -LiteralPath $backupFile
        if ($backupItem.Length -le 0) { throw "Backup file was created with zero size." }
        $backupHash = (Get-FileHash -LiteralPath $backupFile -Algorithm SHA256).Hash

        Write-Section "Apply REV866 migration"
        Set-Location $targetRoot
        & $dotnet ef database update $MigrationName --project .\src\SESS.NexaERP.Infrastructure\SESS.NexaERP.Infrastructure.csproj --context NexaErpDbContext
        if ($LASTEXITCODE -ne 0) { throw "EF database update failed with exit code $LASTEXITCODE." }
    }

    Write-Section "Post-migration evidence"
    $historyInfoAfter = Get-HistoryTableInfo $Database
    $migrationsAfter = Invoke-PsqlSafe "select `"MigrationId`" from $($historyInfoAfter.QualifiedTable) order by `"MigrationId`";"
    $employeeCount = Invoke-PsqlSafe 'select count(*) from nexa.employees;'
    $employeeRange = Invoke-PsqlSafe 'select min("EmployeeCode") || ''..'' || max("EmployeeCode") from nexa.employees;'
    $duplicateEmployeeCodes = Invoke-PsqlSafe 'select count(*) from (select "EmployeeCode" from nexa.employees group by "EmployeeCode" having count(*) > 1) d;'
    $roleCount = Invoke-PsqlSafe 'select count(*) from nexa.roles where "IsActive" = true;'
    $pageCount = Invoke-PsqlSafe 'select count(*) from nexa.page_definitions;'
    $employeePages = Invoke-PsqlSafe "select string_agg(`"PageKey`", ',' order by `"PageKey`") from nexa.page_definitions where `"PageKey`" in ('employees.master','employees.role-mapping','employees.audit-history');"
    $permissionCount = Invoke-PsqlSafe 'select count(*) from nexa.role_page_permissions;'
    $sess001Roles = Invoke-PsqlSafe "select string_agg(r.`"Code`", ',' order by r.`"Code`") from nexa.employee_role_assignments era join nexa.employees e on e.`"Id`" = era.`"EmployeeId`" join nexa.roles r on r.`"Id`" = era.`"RoleId`" where e.`"EmployeeCode`" = 'SESS-001';"
    $sess002Roles = Invoke-PsqlSafe "select string_agg(r.`"Code`", ',' order by r.`"Code`") from nexa.employee_role_assignments era join nexa.employees e on e.`"Id`" = era.`"EmployeeId`" join nexa.roles r on r.`"Id`" = era.`"RoleId`" where e.`"EmployeeCode`" = 'SESS-002';"
    $sess012Roles = Invoke-PsqlSafe "select string_agg(r.`"Code`", ',' order by r.`"Code`") from nexa.employee_role_assignments era join nexa.employees e on e.`"Id`" = era.`"EmployeeId`" join nexa.roles r on r.`"Id`" = era.`"RoleId`" where e.`"EmployeeCode`" = 'SESS-012';"
    $otherTdMd = Invoke-PsqlSafe "select count(*) from nexa.employee_role_assignments era join nexa.employees e on e.`"Id`" = era.`"EmployeeId`" join nexa.roles r on r.`"Id`" = era.`"RoleId`" where r.`"Code`" in ('technical_director','managing_director') and e.`"EmployeeCode`" not in ('SESS-001','SESS-002');"
    $statusHistoryCount = Invoke-PsqlSafe 'select count(*) from nexa.employee_status_history;'
    $roleHistoryCount = Invoke-PsqlSafe 'select count(*) from nexa.employee_role_assignments;'
    $importHistoryCount = Invoke-PsqlSafe 'select count(*) from nexa.employee_import_history;'

    Write-Section "Build, tests and secret scan"
    Set-Location $targetRoot
    & $dotnet restore .\SESS.NexaERP.slnx
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }
    & $dotnet build .\SESS.NexaERP.slnx --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed." }
    & $dotnet test .\SESS.NexaERP.slnx --configuration Release --no-build
    if ($LASTEXITCODE -ne 0) { throw "dotnet test failed." }
    Set-Location $repoRoot
    $secretPattern = ("SESS" + "@") + "|" + ("ERP" + "2026") + "|" + ("Signing" + "Key") + "|" + ("Jwt" + "Secret") + "|" + ("JWT" + "_SECRET") + "|" + "Pass" + "word=" + "[^$]"
    $secretScanEvidence = Invoke-SecretScan -Pattern $secretPattern -Root $repoRoot

    Write-Section "Restore verification"
    $exists = Invoke-PsqlSafe "select 1 from pg_database where datname = '$RestoreVerifyDatabase';" "postgres"
    if ($exists -eq "1") { throw "Restore verification database already exists: $RestoreVerifyDatabase" }
    Invoke-Psql "create database $RestoreVerifyDatabase;" "postgres" | Out-Null
    & $pgRestore -h $HostName -p $Port -U $UserName -d $RestoreVerifyDatabase --no-owner --no-privileges $backupFile
    if ($LASTEXITCODE -ne 0) { throw "pg_restore failed with exit code $LASTEXITCODE." }
    $restoreHistoryInfo = Get-HistoryTableInfo $RestoreVerifyDatabase
    $restoreMigrationCount = Invoke-PsqlSafe "select count(*) from $($restoreHistoryInfo.QualifiedTable);" $RestoreVerifyDatabase
    $restorePageCount = Invoke-PsqlSafe 'select count(*) from nexa.page_definitions;' $RestoreVerifyDatabase

    Add-Report "# REV866 Database and Runtime Verification"
    Add-Report ""
    Add-Report "- Revision: REV866 database verification"
    Add-Report "- Source baseline commit: $requiredBaselineCommit"
    Add-Report "- Verification helper commit: $gitCommit"
    Add-Report "- Database: $Database on ${HostName}:$Port"
    Add-Report "- EF migration history table: $($historyInfoAfter.QualifiedTable)"
    Add-Report "- Backup file: $backupFile"
    Add-Report "- Backup size bytes: $($backupItem.Length)"
    Add-Report "- Backup SHA-256: $backupHash"
    Add-Report "- Migration application mode: $migrationApplicationMode"
    Add-Report "- Applied migrations after:"
    Add-Report '```text'
    Add-Report $migrationsAfter
    Add-Report '```'
    Add-Report "- Employee count: $employeeCount"
    Add-Report "- Employee code range: $employeeRange"
    Add-Report "- Duplicate employee codes: $duplicateEmployeeCodes"
    Add-Report "- Active role count: $roleCount"
    Add-Report "- Page Master count: $pageCount"
    Add-Report "- Employee pages: $employeePages"
    Add-Report "- Role-page permission count: $permissionCount"
    Add-Report "- SESS-001 roles: $sess001Roles"
    Add-Report "- SESS-002 roles: $sess002Roles"
    Add-Report "- SESS-012 roles: $sess012Roles"
    Add-Report "- Other TD/MD mappings: $otherTdMd"
    Add-Report "- Employee status history rows: $statusHistoryCount"
    Add-Report "- Employee role assignment rows: $roleHistoryCount"
    Add-Report "- Employee import history rows: $importHistoryCount"
    Add-Report "- Restore verification database: $RestoreVerifyDatabase"
    Add-Report "- Restore verification migration count: $restoreMigrationCount"
    Add-Report "- Restore verification page count: $restorePageCount"
    Add-Report "- Restore removal: pending management confirmation; database was intentionally left in place."
    Add-Report "- Secret scan: $secretScanEvidence"
    Add-Report "- Runtime auth evidence: API uses JWT/OIDC runtime only; real OIDC authority/audience remains an external configuration blocker for live token-based runtime tests. No temporary header auth was added."

    Write-Host "REV866 database verification report: $reportFile"
    Write-Host "Backup file: $backupFile"
    Write-Host "Backup SHA-256: $backupHash"
}
catch {
    Write-FailureReport $_.Exception.Message
    Write-Host $_.Exception.Message
    throw
}
finally {
    Remove-Item Env:\ConnectionStrings__NexaErp -ErrorAction SilentlyContinue
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    if ($PlainPassword) { $PlainPassword = $null }
    if ($securePassword) { $securePassword.Dispose() }
}


