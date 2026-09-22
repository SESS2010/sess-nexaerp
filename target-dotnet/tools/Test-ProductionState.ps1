# Test-ProductionState.ps1 - read-only production checks using an explicit machine profile.
# DESKTOP-SPF5420: use server-production-state.example.json; legacy default is laptop-only.
# Read-only. Prints one PASS/FAIL line per check, writes the same to local-evidence/production-days/,
# and exits with the number of failed checks (0 = production state confirmed).
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File tools\Test-ProductionState.ps1
#   ... -BeforeGoLive     # 1-30 September: the witness task may still be enabled and the verified backup not yet registered
param(
    [string] $ConfigPath = (Join-Path $PSScriptRoot 'production-state.json'),
    [switch] $BeforeGoLive
)
$ErrorActionPreference = 'Continue'
$config = Get-Content -LiteralPath $ConfigPath -Raw | ConvertFrom-Json
$root = Split-Path -Parent $PSScriptRoot
$evidenceDir = if ([IO.Path]::IsPathRooted($config.EvidenceDirectory)) { $config.EvidenceDirectory } else { Join-Path $root $config.EvidenceDirectory }
New-Item -ItemType Directory -Force -Path $evidenceDir | Out-Null
$evidence = Join-Path $evidenceDir ("{0:yyyy-MM-dd}-state-{0:HHmmss}.txt" -f (Get-Date))
$failed = 0
$lines = @("Production-state check $(Get-Date -Format o) host=$env:COMPUTERNAME user=$env:USERNAME")
function Check([bool]$ok, [string]$name, [string]$detail) {
    $line = ("{0}  {1,-34} {2}" -f $(if ($ok) { 'PASS' } else { 'FAIL' }), $name, $detail)
    $script:lines += $line
    if ($ok) { Write-Host $line -ForegroundColor Green } else { Write-Host $line -ForegroundColor Red; $script:failed++ }
}
function Is-Protected([string]$commandLine) {
    foreach ($p in $config.ProtectedCommandLinePatterns) { if ($commandLine -and $commandLine -match $p) { return $true } }
    return $false
}

if ($config.ExpectedHost) { Check ($env:COMPUTERNAME -eq $config.ExpectedHost) 'expected server' $config.ExpectedHost }

# 1. Development processes closed (a process whose command line is protected, e.g. the ERP's own node/dotnet, does not count).
$procs = Get-CimInstance Win32_Process
$dev = $procs | Where-Object { ($_.Name -replace '\.exe$','') -in $config.DevelopmentProcesses -and -not (Is-Protected $_.CommandLine) }
Check ($dev.Count -eq 0) 'development processes closed' $(if ($dev) { ($dev | Group-Object Name | ForEach-Object { "$($_.Name) x$($_.Count)" }) -join ', ' } else { 'none running' })

# 2. No test host, compiler server or build in progress.
$build = $procs | Where-Object { $_.Name -match '^(testhost|VBCSCompiler|MSBuild|dotnet)\.exe$' -and $_.CommandLine -match 'test|build|VBCSCompiler|MSBuild|ef ' -and -not (Is-Protected $_.CommandLine) }
Check ($build.Count -eq 0) 'no test/build/migration running' $(if ($build) { ($build | ForEach-Object { "$($_.Name)($($_.ProcessId))" }) -join ', ' } else { 'none' })

# 3. No disposable PostgreSQL clusters: every postgres.exe must belong to the service data directory; no leftover cluster folders in TEMP.
$pg = $procs | Where-Object { $_.Name -eq 'postgres.exe' }
$foreign = $pg | Where-Object { $_.CommandLine -and $_.CommandLine -notmatch [regex]::Escape($config.PostgreSqlServiceDataDirectory) -and $_.CommandLine -match '-D\s' }
$tempDirs = foreach ($pat in $config.DisposableClusterDirectoryPatterns) { Get-ChildItem -Path $env:TEMP -Directory -Filter $pat -ErrorAction SilentlyContinue }
Check (($foreign.Count -eq 0) -and ($tempDirs.Count -eq 0)) 'no disposable PostgreSQL clusters' ("foreign postgres processes: {0}; leftover cluster folders in TEMP: {1}" -f $foreign.Count, $tempDirs.Count)

# 4. Production services running.
foreach ($s in $config.Services) { $svc = Get-Service -Name $s -ErrorAction SilentlyContinue; Check ($svc -and $svc.Status -eq 'Running') "service $s" $(if ($svc) { "$($svc.Status)/$($svc.StartType)" } else { 'not installed' }) }

# 5. ERP endpoints responding.
foreach ($e in $config.Endpoints) {
    $status = $null; $detail = ''
    try {
        $req = [System.Net.HttpWebRequest]::Create($e.Url); $req.Method = 'GET'; $req.Timeout = 10000; $req.AllowAutoRedirect = $false
        $resp = $req.GetResponse(); $status = [int]$resp.StatusCode; $resp.Close()
    }
    catch [System.Net.WebException] { if ($_.Exception.Response) { $status = [int]$_.Exception.Response.StatusCode } else { $detail = $_.Exception.Message } }
    catch { $detail = $_.Exception.Message }
    $ok = ($null -ne $status) -and ($status -in $e.AcceptStatus)
    if (-not $e.Required -and -not $ok) { $lines += ("SKIP  {0,-34} {1} -> {2} (not required)" -f $e.Name, $e.Url, $(if ($status) { $status } else { $detail })); Write-Host $lines[-1] -ForegroundColor Yellow; continue }
    Check $ok "endpoint $($e.Name)" ("{0} -> {1}" -f $e.Url, $(if ($status) { "HTTP $status" } else { $detail }))
}

# 6. Memory, CPU and disk headroom.
$os = Get-CimInstance Win32_OperatingSystem
$availMB = [math]::Round($os.FreePhysicalMemory / 1KB)
Check ($availMB -ge $config.MinAvailableMemoryMB) 'available memory' ("{0} MB available (minimum {1})" -f $availMB, $config.MinAvailableMemoryMB)
$commit = [math]::Round(($os.TotalVirtualMemorySize - $os.FreeVirtualMemory) / $os.TotalVirtualMemorySize * 100)
Check ($commit -lt 70) 'commit charge' ("{0} % of {1} GB limit" -f $commit, [math]::Round($os.TotalVirtualMemorySize / 1MB))
$cpu = [math]::Round(((Get-Counter '\Processor(_Total)\% Processor Time' -SampleInterval 1 -MaxSamples 3).CounterSamples | Measure-Object CookedValue -Average).Average)
Check ($cpu -le $config.MaxCpuPercent) 'CPU load (3 s)' ("{0} % (maximum {1})" -f $cpu, $config.MaxCpuPercent)
foreach ($d in $config.MinFreeSpaceGB.PSObject.Properties) { $free = [math]::Round((Get-PSDrive $d.Name -ErrorAction SilentlyContinue).Free / 1GB, 1); Check ($free -ge $d.Value) "free space $($d.Name):" ("{0} GB (minimum {1})" -f $free, $d.Value) }

# 7. LabVIEW / NI services stopped (disabled 21 September; restore with pc-maintenance-20260921\RESTORE-NI-Siemens.ps1 for a chamber test).
if ($config.NiServiceDisplayNamePattern) {
$ni = Get-Service | Where-Object { $_.DisplayName -match $config.NiServiceDisplayNamePattern -and $_.Status -eq 'Running' }
Check ($ni.Count -eq 0) 'NI / LabVIEW services stopped' $(if ($ni) { ($ni | ForEach-Object { $_.Name }) -join ', ' } else { 'none running' })

}

# 8. Nightly witness task disabled on the production server (runbook section 0).
$task = Get-ScheduledTask -TaskName $config.WitnessTaskName -ErrorAction SilentlyContinue
$taskOk = (-not $task) -or ($task.State -eq 'Disabled')
if ($BeforeGoLive -and -not $taskOk) { $lines += "SKIP  nightly witness task                  still $($task.State) (allowed before 1 October)"; Write-Host $lines[-1] -ForegroundColor Yellow }
else { Check $taskOk 'nightly witness task disabled' $(if ($task) { $task.State } else { 'not registered' }) }

# 9. Verified backup: last run finished within BackupMaxAgeHours with exit code 0 (Item 26 wrapper writes logs\last-run.json).
if ($config.DailyOffMachineBackup) {
    . (Join-Path $PSScriptRoot 'Test-DailyBackupState.ps1')
    $daily = $config.DailyOffMachineBackup
    try {
        $backupTask = Get-ScheduledTask -TaskName $daily.TaskName -ErrorAction Stop
        $taskInfo = Get-ScheduledTaskInfo -TaskName $daily.TaskName -ErrorAction Stop
        $state = Test-DailyBackupState -StatusPath $daily.StatusFile -MaxAgeHours $daily.MaxAgeHours -TaskEnabled ($backupTask.State -ne 'Disabled') -TaskResult $taskInfo.LastTaskResult
        Check $state.Ok 'daily off-machine backup' $state.Detail
    } catch { Check $false 'daily off-machine backup' ('task/status unavailable: ' + $_.Exception.Message) }
} else {
$statusFile = $config.BackupStatusFile
if (Test-Path -LiteralPath $statusFile) {
    $st = Get-Content -LiteralPath $statusFile -Raw | ConvertFrom-Json
    $age = ((Get-Date).ToUniversalTime() - [datetime]::Parse($st.FinishedUtc).ToUniversalTime()).TotalHours
    Check (($st.ExitCode -eq 0) -and ($age -le $config.BackupMaxAgeHours)) 'verified backup fresh' ("exit {0}, finished {1:N1} h ago (maximum {2} h)" -f $st.ExitCode, $age, $config.BackupMaxAgeHours)
} elseif ($BeforeGoLive) { $lines += "SKIP  verified backup fresh               $statusFile absent (register the 18:45 task before 1 October)"; Write-Host $lines[-1] -ForegroundColor Yellow }
else { Check $false 'verified backup fresh' "$statusFile absent - the 18:45 verified backup task is not registered or has never run" }

}

$summary = "RESULT: $(if ($failed -eq 0) { 'PRODUCTION STATE CONFIRMED' } else { "$failed check(s) failed - NOT in production state" })"
$lines += $summary
$lines | Out-File -LiteralPath $evidence -Encoding utf8
Write-Host $summary -ForegroundColor $(if ($failed -eq 0) { 'Green' } else { 'Red' })
Write-Host "evidence: $evidence"
exit $failed
