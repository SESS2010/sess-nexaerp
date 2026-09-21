# Enter-ProductionDay.ps1 - close development on the ERP laptop before 09:30 and confirm the ERP is still up
# (runbook section 15, operating rule from 1 October 2026). Run as the logged-in user; no admin rights needed.
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File tools\Enter-ProductionDay.ps1            # do it
#   powershell -NoProfile -ExecutionPolicy Bypass -File tools\Enter-ProductionDay.ps1 -DryRun    # only show what would be closed
#
# It never touches: the PostgreSQL service and its cluster, the ERP's own node/dotnet processes, browsers,
# or anything whose command line matches ProtectedCommandLinePatterns in production-state.json.
# It refuses to run while a migration (`dotnet ef`) is executing: a migration finishes, it is not killed.
param(
    [string] $ConfigPath = (Join-Path $PSScriptRoot 'production-state.json'),
    [switch] $DryRun,
    [int] $GracefulSeconds = 20
)
$ErrorActionPreference = 'Continue'
$config = Get-Content -LiteralPath $ConfigPath -Raw | ConvertFrom-Json
$root = Split-Path -Parent $PSScriptRoot
$evidenceDir = Join-Path $root $config.EvidenceDirectory
New-Item -ItemType Directory -Force -Path $evidenceDir | Out-Null
$log = Join-Path $evidenceDir ("{0:yyyy-MM-dd}-enter-{0:HHmmss}.log" -f (Get-Date))
function Log([string]$s) { $line = "{0:HH:mm:ss}  {1}" -f (Get-Date), $s; $line | Tee-Object -FilePath $log -Append }
function Is-Protected([string]$commandLine) {
    foreach ($p in $config.ProtectedCommandLinePatterns) { if ($commandLine -and $commandLine -match $p) { return $true } }
    return $false
}
Log "Enter production day $(if ($DryRun) { '(DRY RUN - nothing closed)' }) host=$env:COMPUTERNAME"

# 0. A running migration is never interrupted.
$migration = Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'dotnet.exe' -and $_.CommandLine -match '\sef\s+(database|migrations)' }
if ($migration) { Log "REFUSED: a migration is running (PID $(($migration | ForEach-Object { $_.ProcessId }) -join ',')). Let it finish, reconcile principals, then run again."; exit 2 }

# 1. Stop the .NET build servers politely, then the development processes (VS Code and its helpers, Claude Code, Codex, test hosts, compilers).
if (-not $DryRun) { & dotnet build-server shutdown 2>&1 | ForEach-Object { Log "build-server: $_" } }
$procs = Get-CimInstance Win32_Process
$targets = $procs | Where-Object { ($_.Name -replace '\.exe$','') -in $config.DevelopmentProcesses -and -not (Is-Protected $_.CommandLine) }
foreach ($t in $targets | Sort-Object Name) { Log ("close  {0,-45} pid {1,-6} {2}" -f $t.Name, $t.ProcessId, $(if ($t.CommandLine) { $t.CommandLine.Substring(0, [math]::Min(90, $t.CommandLine.Length)) } else { '' })) }
if (-not $DryRun -and $targets) {
    # VS Code first and gracefully, so open editors are not lost; everything else after a short wait.
    $code = $targets | Where-Object { $_.Name -eq 'Code.exe' }
    foreach ($c in $code) { $p = Get-Process -Id $c.ProcessId -ErrorAction SilentlyContinue; if ($p -and $p.MainWindowHandle -ne 0) { [void]$p.CloseMainWindow() } }
    if ($code) { Log "asked VS Code to close; waiting up to $GracefulSeconds s"; $deadline = (Get-Date).AddSeconds($GracefulSeconds); while ((Get-Date) -lt $deadline -and (Get-Process -Name Code -ErrorAction SilentlyContinue)) { Start-Sleep -Seconds 1 } }
    foreach ($t in $targets) { $p = Get-Process -Id $t.ProcessId -ErrorAction SilentlyContinue; if ($p) { Stop-Process -Id $t.ProcessId -Force -ErrorAction SilentlyContinue; Log "stopped $($t.Name) pid $($t.ProcessId)" } }
}

# 2. Disposable PostgreSQL clusters: stop any postgres.exe that is not the service cluster, then remove leftover cluster folders in TEMP.
$pgctl = Join-Path $config.PostgreSqlBin 'pg_ctl.exe'
$foreign = Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'postgres.exe' -and $_.CommandLine -match '-D\s+"?([^"]+)"?' -and $_.CommandLine -notmatch [regex]::Escape($config.PostgreSqlServiceDataDirectory) }
$dirs = @()
foreach ($f in $foreign) { if ($f.CommandLine -match '-D\s+"?([^"]+?)"?(\s|$)') { $dirs += $Matches[1] } }
foreach ($d in ($dirs | Sort-Object -Unique)) {
    Log "disposable cluster running at $d"
    if (-not $DryRun -and (Test-Path -LiteralPath $pgctl)) { & $pgctl stop -D $d -m fast -w 2>&1 | ForEach-Object { Log "pg_ctl: $_" } }
}
$leftovers = foreach ($pat in $config.DisposableClusterDirectoryPatterns) { Get-ChildItem -Path $env:TEMP -Directory -Filter $pat -ErrorAction SilentlyContinue }
foreach ($l in $leftovers) {
    $inUse = Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -and $_.CommandLine -match [regex]::Escape($l.FullName) }
    if ($inUse) { Log "leftover $($l.Name) still in use by pid $(($inUse | ForEach-Object { $_.ProcessId }) -join ','); not removed"; continue }
    Log "remove leftover cluster folder $($l.FullName)"
    if (-not $DryRun) { Remove-Item -LiteralPath $l.FullName -Recurse -Force -ErrorAction SilentlyContinue }
}

# 3. LabVIEW / NI services are Disabled since 21 September; report if any is running (stopping a service needs an elevated window).
$ni = Get-Service | Where-Object { $_.DisplayName -match $config.NiServiceDisplayNamePattern -and $_.Status -eq 'Running' }
if ($ni) { Log "WARNING: NI services running: $(($ni | ForEach-Object { $_.Name }) -join ', ') - stop them from an elevated window (Stop-Service) before 09:30" } else { Log 'NI / LabVIEW services: none running' }

# 4. Confirm the machine is in production state (ERP still up, memory free, nothing disposable left).
Log 'running Test-ProductionState.ps1'
& (Join-Path $PSScriptRoot 'Test-ProductionState.ps1') -ConfigPath $ConfigPath
$result = $LASTEXITCODE
Log "Test-ProductionState exit $result $(if ($result -eq 0) { '- PRODUCTION DAY OPEN' } else { '- NOT in production state; fix the FAIL lines before users start' })"
Write-Host "log: $log"
exit $result
