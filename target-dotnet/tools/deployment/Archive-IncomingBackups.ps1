[CmdletBinding()]
param([string]$Receiver='DESKTOP-AP',[string]$Incoming='D:\SESS-Backup-Incoming',[string]$Archive='D:\SESS-Backup-Vault',[Parameter(Mandatory=$true)][string]$StatusDirectory)
$ErrorActionPreference='Stop'
Import-Module (Join-Path $PSScriptRoot 'VerifiedBackupTransfer.psm1') -Force
$lock=$null;$result=1
try {
 if ($env:COMPUTERNAME -ne $Receiver -or $Receiver -eq 'DESKTOP-SPF5420') { throw 'Wrong receiver.' }
 $Incoming=Assert-SafeBackupPath $Incoming;$Archive=Assert-SafeBackupPath $Archive;$StatusDirectory=Assert-SafeBackupPath $StatusDirectory
 $lock=[IO.File]::Open((Join-Path $StatusDirectory 'archive.lock'),'OpenOrCreate','ReadWrite','None')
 foreach ($db in @('ERP','Identity')) {
  $root=Join-Path $Incoming $db;$dest=Join-Path $Archive $db
  $null=Get-BackupRootId $root
  $bundles=@(Get-ChildItem -LiteralPath $root -Directory -Filter 'run-*')
  if (-not $bundles.Count) { throw 'Missing incoming backup.' }
  $newest=[DateTimeOffset]::MinValue
  foreach ($bundle in $bundles) {
   $m=Get-VerifiedBundle $root $bundle.FullName
   if ($m.Database -ne $(if ($db -eq 'ERP') {'sess_nexa_erp'} else {'sess_keycloak'})) { throw 'Wrong database.' }
   $null=Copy-VerifiedBundle $root $bundle.FullName $dest
   if ([DateTimeOffset]$m.VerifiedUtc -gt $newest) { $newest=[DateTimeOffset]$m.VerifiedUtc }
  }
  if ($newest -lt [DateTimeOffset]::UtcNow.AddHours(-26)) { throw 'Incoming backup stale.' }
 }
 $result=0
} catch { Write-Error 'Receiver archive failed or stale; inspect receiver storage and protected evidence.' -ErrorAction Continue }
finally {
 if ($lock) {
  try {
   $file=Assert-SafeBackupPath (Join-Path $StatusDirectory 'archive-last-run.json')
   [ordered]@{FinishedUtc=[DateTime]::UtcNow.ToString('o');ExitCode=$result} | ConvertTo-Json | Set-Content -LiteralPath $file -Encoding UTF8
  } catch { $result=1 }
  $lock.Dispose()
 }
}
exit $result
