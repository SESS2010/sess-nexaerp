[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$PlanPath)
$ErrorActionPreference='Stop'
Import-Module (Join-Path $PSScriptRoot 'VerifiedBackupTransfer.psm1') -Force
$gate=$null; $result=1; $status=$null
try {
 $PlanPath=Assert-SafeBackupPath $PlanPath
 $plan=Get-Content -LiteralPath $PlanPath -Raw | ConvertFrom-Json
 if ($env:COMPUTERNAME -ne $plan.Server -or $plan.Server -ne 'DESKTOP-SPF5420') { throw 'Wrong source machine.' }
 if ($plan.DestinationHost -in @($env:COMPUTERNAME,'localhost','127.0.0.1','192.168.68.130') -or $plan.DestinationHost -notmatch '\A[A-Za-z0-9-]+\z') { throw 'Off-machine host required.' }
 $status=Assert-SafeBackupPath $plan.StatusDirectory
 if (-not (Test-Path -LiteralPath $status -PathType Container)) { throw 'Protected status directory required.' }
 $gate=[IO.File]::Open((Join-Path $status 'daily.lock'),'OpenOrCreate','ReadWrite','None')
 $configs=@($plan.Databases)
 if ($configs.Count -ne 2 -or @($configs | ForEach-Object { (Get-Content -LiteralPath $_.ConfigPath -Raw | ConvertFrom-Json).ExpectedDatabase } | Select-Object -Unique).Count -ne 2) { throw 'Distinct ERP and identity jobs both required.' }
 foreach ($job in $configs) {
  $configPath=Assert-SafeBackupPath $job.ConfigPath
  $config=Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
  if ($config.ExpectedDatabase -notin @('sess_nexa_erp','sess_keycloak') -or $config.ExpectedHost -ne '127.0.0.1' -or $config.ExpectedPort -ne 5432) { throw 'Unexpected source database.' }
  $root=Assert-SafeBackupPath $config.BackupRoot
  $work=Assert-SafeBackupPath $config.WorkingRoot
  if (-not $root.StartsWith('C:\SESS-Backups\',[StringComparison]::OrdinalIgnoreCase) -or
      -not $work.StartsWith('C:\SESS-Backup-Verification\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Only approved SSD roots permitted.' }
  $remote=[string]$job.DestinationRoot
  if (-not $remote.StartsWith(('\\'+$plan.DestinationHost+'\SESS-Backup-Incoming$\'),[StringComparison]::OrdinalIgnoreCase)) { throw 'Unapproved remote share.' }
  $null=Assert-SafeBackupPath $remote
  # Fail before creating another dump if the receiver is offline.
  if (-not (Test-Path -LiteralPath ('\\'+$plan.DestinationHost+'\SESS-Backup-Incoming$'))) { throw 'Daily off-machine destination unavailable.' }
  $preLock=$null
  try {
   if (Test-Path -LiteralPath (Join-Path $root '.sess-verified-backups.json')) {
    $preLock=[IO.File]::Open((Join-Path $root 'backup.lock'),'OpenOrCreate','ReadWrite','None')
    foreach ($b in Get-ChildItem -LiteralPath $root -Directory -Filter 'run-*') {
     if (Test-Path -LiteralPath (Join-Path $b.FullName 'manifest.json')) { $null=Copy-VerifiedBundle $root $b.FullName $remote }
    }
    Remove-OffMachineCopiedHistory $root $remote
   }
  } finally { if ($preLock) { $preLock.Dispose() } }
  $free=([IO.DriveInfo]::new('C')).AvailableFreeSpace / 1GB
  Assert-BackupCapacity $free ([double]$job.RequiredRunGiB)
  $runStarted=[DateTimeOffset]::UtcNow
  $engineLock=$null
  & powershell.exe -NoProfile -NonInteractive -File (Join-Path $PSScriptRoot 'Invoke-VerifiedDatabaseBackup.ps1') -InstallerPath $plan.InstallerPath -ConfigPath $configPath -CredentialFile $job.CredentialFile -LogDirectory $job.LogDirectory
  if ($LASTEXITCODE -ne 0) { throw 'Local restore-verified backup failed.' }
  try {
   $engineLock=[IO.File]::Open((Join-Path $root 'backup.lock'),'OpenOrCreate','ReadWrite','None')
   $latest=Get-ChildItem -LiteralPath $root -Directory -Filter 'run-*' | Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'manifest.json') } |
    Sort-Object { [DateTimeOffset](Get-Content -LiteralPath (Join-Path $_.FullName 'manifest.json') -Raw | ConvertFrom-Json).StartedUtc } -Descending | Select-Object -First 1
   if (-not $latest) { throw 'No verified result.' }
   $m=Get-VerifiedBundle $root $latest.FullName
   if ([DateTimeOffset]$m.StartedUtc -lt $runStarted.AddSeconds(-2)) { throw 'Stale backup result.' }
   if ($m.Database -ne $config.ExpectedDatabase -or $m.SourceSystemIdentifier -ne $config.ExpectedSystemIdentifier) { throw 'Source identity mismatch.' }
   $null=Copy-VerifiedBundle $root $latest.FullName $remote
   Remove-OffMachineCopiedHistory $root $remote
  } finally { if ($engineLock) { $engineLock.Dispose() } }
  Assert-BackupCapacity (([IO.DriveInfo]::new('C')).AvailableFreeSpace/1GB) 0.001
 }
 if (@($configs | ForEach-Object { (Get-Content -LiteralPath $_.ConfigPath -Raw | ConvertFrom-Json).ExpectedDatabase } | Select-Object -Unique).Count -ne 2) { throw 'Distinct ERP/identity jobs required.' }
 $result=0
} catch { Write-Error 'Daily site backup FAILED: inspect protected local logs, capacity and receiver availability. No fresh off-machine receipt is implied.' -ErrorAction Continue }
finally {
 if ($gate) {
  try {
   $out=Assert-SafeBackupPath (Join-Path $status 'daily-last-run.json')
   [ordered]@{FinishedUtc=[DateTime]::UtcNow.ToString('o');ExitCode=$result;OffMachineVerified=($result -eq 0)} | ConvertTo-Json | Set-Content -LiteralPath $out -Encoding UTF8
  } catch { $result=1 }
  $gate.Dispose()
 }
}
exit $result
