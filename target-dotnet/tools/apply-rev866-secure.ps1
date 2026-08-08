[CmdletBinding()]
param(
    [string]$Database = "sess_nexaerp",
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$UserName = "postgres",
    [string]$MigrationName = "20260808123411_Rev866EmployeePermissionMatrix",
    [string]$RestoreVerifyDatabase = "sess_nexaerp_restore_verify_rev866"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$targetRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$dotnet = Resolve-Path (Join-Path $targetRoot "..\.dotnet10\dotnet.exe")
$pgBin = "C:\Program Files\PostgreSQL\17\bin"
$psql = Join-Path $pgBin "psql.exe"
$pgDump = Join-Path $pgBin "pg_dump.exe"
$pgRestore = Join-Path $pgBin "pg_restore.exe"
$backupDir = Join-Path $targetRoot "backups\postgresql\pre-rev866"
$reportDir = Join-Path $targetRoot "outputs"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = Join-Path $backupDir "$Database`_pre_rev866_$timestamp.dump"
$reportFile = Join-Path $reportDir "rev866_database_runtime_verification_$timestamp.md"

function Write-Section([string]$Text) {
    Write-Host ""
    Write-Host "== $Text =="
}

function Invoke-Psql([string]$Sql, [string]$Db = $Database) {
    & $psql -h $HostName -p $Port -U $UserName -d $Db -v ON_ERROR_STOP=1 -At -c $Sql
}

function Invoke-PsqlSafe([string]$Sql, [string]$Db = $Database) {
    $result = Invoke-Psql -Sql $Sql -Db $Db
    return ($result -join "`n")
}

function Add-Report([string]$Text) {
    Add-Content -LiteralPath $reportFile -Value $Text -Encoding utf8
}

$securePassword = $null
$PlainPassword = $null
try {
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

    Set-Location $repoRoot
    $gitStatus = (git status --short) -join "`n"
    $gitCommit = (git rev-parse HEAD).Trim()
    if ($gitStatus) { throw "Git status is not clean before database verification." }
    git merge-base --is-ancestor "330807171ce7ba85cc30a984f7467893eb32559a" HEAD | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Required REV866 source baseline 330807171ce7ba85cc30a984f7467893eb32559a is not in the current history." }

    if (!(Test-Path -LiteralPath $psql) -or !(Test-Path -LiteralPath $pgDump) -or !(Test-Path -LiteralPath $pgRestore)) {
        throw "PostgreSQL 17 tools were not found under $pgBin."
    }

    Write-Section "Pre-migration database checks"
    $dbName = Invoke-PsqlSafe "select current_database();"
    if ($dbName -ne $Database) { throw "Connected to unexpected database: $dbName" }
    $migrationsBefore = Invoke-PsqlSafe 'select "MigrationId" from "__EFMigrationsHistory" order by "MigrationId";'
    if ($migrationsBefore -notmatch "20260808110924_Phase1Foundation") { throw "Phase1Foundation migration missing before REV866." }
    if ($migrationsBefore -notmatch "20260808114550_Phase1AuthorizationSeed") { throw "Phase1AuthorizationSeed migration missing before REV866." }
    if ($migrationsBefore -match $MigrationName) { throw "REV866 migration is already applied. Stop to avoid duplicate migration action." }

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

    Write-Section "Post-migration evidence"
    $migrationsAfter = Invoke-PsqlSafe 'select "MigrationId" from "__EFMigrationsHistory" order by "MigrationId";'
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
    $secretPattern = ("SESS" + "@ERP2026") + "|" + ("Signing" + "Key") + "|" + ("Jwt" + "Secret") + "|" + ("JWT" + "_SECRET") + "|" + "Pass" + "word=" + "[^$]"
    $secretScan = rg -n $secretPattern .
    if ($LASTEXITCODE -eq 0) { throw "Secret scan found prohibited patterns." }

    Write-Section "Restore verification"
    $exists = Invoke-PsqlSafe "select 1 from pg_database where datname = '$RestoreVerifyDatabase';" "postgres"
    if ($exists -eq "1") { throw "Restore verification database already exists: $RestoreVerifyDatabase" }
    Invoke-Psql "create database $RestoreVerifyDatabase;" "postgres" | Out-Null
    & $pgRestore -h $HostName -p $Port -U $UserName -d $RestoreVerifyDatabase --no-owner --no-privileges $backupFile
    if ($LASTEXITCODE -ne 0) { throw "pg_restore failed with exit code $LASTEXITCODE." }
    $restoreMigrationCount = Invoke-PsqlSafe 'select count(*) from "__EFMigrationsHistory";' $RestoreVerifyDatabase
    $restorePageCount = Invoke-PsqlSafe 'select count(*) from nexa.page_definitions;' $RestoreVerifyDatabase

    Add-Report "# REV866 Database and Runtime Verification"
    Add-Report ""
    Add-Report "- Revision: REV866 database verification"
    Add-Report "- Source baseline commit: 330807171ce7ba85cc30a984f7467893eb32559a"
    Add-Report "- Verification helper commit: $gitCommit"
    Add-Report "- Database: $Database on ${HostName}:$Port"
    Add-Report "- Backup file: $backupFile"
    Add-Report "- Backup size bytes: $($backupItem.Length)"
    Add-Report "- Backup SHA-256: $backupHash"
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
    Add-Report "- Secret scan: clean"
    Add-Report "- Runtime auth evidence: API uses JWT/OIDC runtime only; real OIDC authority/audience remains an external configuration blocker for live token-based runtime tests. No temporary header auth was added."

    Write-Host "REV866 database verification report: $reportFile"
    Write-Host "Backup file: $backupFile"
    Write-Host "Backup SHA-256: $backupHash"
}
catch {
    Add-Content -LiteralPath $reportFile -Value "# REV866 Database Verification Failed" -Encoding utf8
    Add-Content -LiteralPath $reportFile -Value "" -Encoding utf8
    Add-Content -LiteralPath $reportFile -Value ("- Time: " + (Get-Date -Format o)) -Encoding utf8
    Add-Content -LiteralPath $reportFile -Value ("- Error: " + $_.Exception.Message) -Encoding utf8
    Write-Host "REV866 verification failed. Sanitized failure report: $reportFile"
    Write-Host $_.Exception.Message
    throw
}finally {
    Remove-Item Env:\ConnectionStrings__NexaErp -ErrorAction SilentlyContinue
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    if ($PlainPassword) { $PlainPassword = $null }
    if ($securePassword) { $securePassword.Dispose() }
}










