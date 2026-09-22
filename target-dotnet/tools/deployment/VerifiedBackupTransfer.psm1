Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
function Assert-SafeBackupPath([string]$Path) {
 if (-not [IO.Path]::IsPathRooted($Path)) { throw 'Absolute backup path required.' }
 $full=[IO.Path]::GetFullPath($Path).TrimEnd('\')
 $walk=$full
 while ($walk) {
  if (Test-Path -LiteralPath $walk) {
   if ((Get-Item -LiteralPath $walk -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Reparse points refused.' }
  }
  $walk=[IO.Path]::GetDirectoryName($walk)
 }
 return $full
}
function Get-BackupRootId([string]$Root) {
 $rootPath=Assert-SafeBackupPath $Root
 $m=Get-Content -LiteralPath (Join-Path $rootPath '.sess-verified-backups.json') -Raw | ConvertFrom-Json
 if ($m.Format -ne 1 -or [Guid]$m.Id -eq [Guid]::Empty) { throw 'Invalid root marker.' }
 return [string]$m.Id
}
function Get-VerifiedBundle([string]$Root,[string]$Bundle) {
 $rootPath=Assert-SafeBackupPath $Root; $path=Assert-SafeBackupPath $Bundle
 if ([IO.Path]::GetDirectoryName($path) -ne $rootPath) { throw 'Bundle escaped root.' }
 $m=Get-Content -LiteralPath (Join-Path $path 'manifest.json') -Raw | ConvertFrom-Json
 if ($m.State -ne 'VERIFIED' -or $m.Format -notin 1,2 -or $m.RootId -ne (Get-BackupRootId $rootPath) -or
     [Guid]$m.RunId -eq [Guid]::Empty -or [IO.Path]::GetFileName($path) -ne ('run-'+([Guid]$m.RunId).ToString('N'))) { throw 'Invalid verified bundle ownership.' }
 $names=@{}; $allowed=@('database.dump','globals.sql','manifest.json','started.json','tools.log','last-restored-evidence.json','last-restore-postgresql.log','source-evidence.json')
 foreach ($f in $m.Files) {
  if ($f.Name -notmatch '\A(?:database\.dump|globals\.sql|configuration-[A-Za-z0-9_.-]+)\z' -or $names.ContainsKey($f.Name)) { throw 'Unsafe/duplicate manifest file.' }
  $names[$f.Name]=$true; $allowed+=$f.Name
  $file=Assert-SafeBackupPath (Join-Path $path $f.Name)
  if ((Get-Item -LiteralPath $file).Length -ne $f.Length -or (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $f.Sha256) { throw 'Bundle payload hash mismatch.' }
 }
 if (-not $names.ContainsKey('database.dump') -or -not $names.ContainsKey('globals.sql')) { throw 'Missing recovery payload.' }
 foreach ($f in Get-ChildItem -LiteralPath $path -Force) {
  $null=Assert-SafeBackupPath $f.FullName
  if ($f.PSIsContainer -or $f.Name -notin $allowed) { throw 'Unexpected bundle entry.' }
 }
 return $m
}
function Compare-BackupCopies([string]$Source,[string]$Destination) {
 $sourcePath=Assert-SafeBackupPath $Source; $destPath=Assert-SafeBackupPath $Destination
 $a=@(Get-ChildItem -LiteralPath $sourcePath -Force);$b=@(Get-ChildItem -LiteralPath $destPath -Force)
 if ($a.Count -ne $b.Count) { throw 'Copied file set differs.' }
 foreach ($f in $a) {
  $null=Assert-SafeBackupPath $f.FullName
  $other=Assert-SafeBackupPath (Join-Path $destPath $f.Name)
  if ($f.PSIsContainer -or -not (Test-Path -LiteralPath $other -PathType Leaf) -or
      (Get-Item -LiteralPath $other).Length -ne $f.Length -or
      (Get-FileHash -LiteralPath $other -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $f.FullName -Algorithm SHA256).Hash) { throw 'Off-machine copy mismatch.' }
 }
}
function Copy-VerifiedBundle([string]$Root,[string]$Bundle,[string]$DestinationRoot) {
 $m=Get-VerifiedBundle $Root $Bundle
 $dest=Assert-SafeBackupPath $DestinationRoot; $src=Assert-SafeBackupPath $Root
 if ($dest -eq $src -or $dest.StartsWith($src+'\',[StringComparison]::OrdinalIgnoreCase) -or $src.StartsWith($dest+'\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Copy roots overlap.' }
 if (-not (Test-Path -LiteralPath $dest)) { New-Item -ItemType Directory -Path $dest | Out-Null }
 $marker=Join-Path $dest '.sess-verified-backups.json'
 if (-not (Test-Path -LiteralPath $marker)) {
  if (@(Get-ChildItem -LiteralPath $dest -Force).Count) { throw 'Destination is nonempty and unowned.' }
  Copy-Item -LiteralPath (Join-Path $src '.sess-verified-backups.json') -Destination $marker
 }
 if ((Get-BackupRootId $dest) -ne $m.RootId) { throw 'Destination root identity mismatch.' }
 $target=Join-Path $dest ([IO.Path]::GetFileName($Bundle))
 if (Test-Path -LiteralPath $target) {
  $null=Get-VerifiedBundle $dest $target; Compare-BackupCopies $Bundle $target
  return $target
 }
 $stage=Join-Path $dest ('.copy-'+[Guid]::NewGuid().ToString('N'))
 New-Item -ItemType Directory -Path $stage | Out-Null
 foreach ($f in Get-ChildItem -LiteralPath $Bundle -File -Force) { Copy-Item -LiteralPath $f.FullName -Destination (Join-Path $stage $f.Name) }
 Compare-BackupCopies $Bundle $stage
 # Checked root-contained absolute paths, no overwrite, no recursive cleanup.
 $stage=Assert-SafeBackupPath $stage; $target=Assert-SafeBackupPath $target
 if ([IO.Path]::GetDirectoryName($stage) -ne $dest -or [IO.Path]::GetDirectoryName($target) -ne $dest) { throw 'Copy escaped root.' }
 [IO.Directory]::Move($stage,$target)
 $null=Get-VerifiedBundle $dest $target
 return $target
}
function Remove-OffMachineCopiedHistory([string]$Root,[string]$DestinationRoot,[int]$Keep=2) {
 if ($Keep -lt 2) { throw 'Keep at least two local verified bundles.' }
 $rootPath=Assert-SafeBackupPath $Root
 $items=@(Get-ChildItem -LiteralPath $rootPath -Directory -Filter 'run-*' | ForEach-Object {
  # Failed/incomplete runs are never deleted by this function.
  if (Test-Path -LiteralPath (Join-Path $_.FullName 'manifest.json')) {
   $m=Get-VerifiedBundle $rootPath $_.FullName
   [pscustomobject]@{Path=$_.FullName; Started=[DateTimeOffset]$m.StartedUtc}
  }
 } | Sort-Object Started -Descending)
 foreach ($item in $items | Select-Object -Skip $Keep) {
  $remote=Join-Path $DestinationRoot ([IO.Path]::GetFileName($item.Path))
  $null=Get-VerifiedBundle $DestinationRoot $remote
  Compare-BackupCopies $item.Path $remote
  $path=Assert-SafeBackupPath $item.Path
  if ([IO.Path]::GetDirectoryName($path) -ne $rootPath) { throw 'Retention escaped root.' }
  $null=Get-VerifiedBundle $rootPath $path
  # Delete only individually validated flat files, never recurse through arbitrary paths.
  foreach ($f in Get-ChildItem -LiteralPath $path -File -Force) { Remove-Item -LiteralPath $f.FullName }
  [IO.Directory]::Delete($path,$false)
 }
}
function Assert-BackupCapacity([double]$FreeGiB,[double]$RequiredRunGiB) {
 if ($RequiredRunGiB -le 0 -or $FreeGiB -lt (25+$RequiredRunGiB)) { throw 'Insufficient capacity: preserve 25 GiB plus the measured run budget.' }
}
Export-ModuleMember -Function Assert-SafeBackupPath,Get-BackupRootId,Get-VerifiedBundle,Compare-BackupCopies,Copy-VerifiedBundle,Remove-OffMachineCopiedHistory,Assert-BackupCapacity
