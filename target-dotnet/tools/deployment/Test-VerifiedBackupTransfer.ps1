$ErrorActionPreference='Stop'
Import-Module (Join-Path $PSScriptRoot 'VerifiedBackupTransfer.psm1') -Force
$root=Join-Path ([IO.Path]::GetTempPath()) ('sess-transfer-test-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $root | Out-Null
$src=Join-Path $root 'source';$dst=Join-Path $root 'receiver'
New-Item -ItemType Directory -Path $src | Out-Null
$id=[Guid]::NewGuid()
@{Format=1;Id=$id.ToString()} | ConvertTo-Json | Set-Content (Join-Path $src '.sess-verified-backups.json')
$script:checks=0
function Must-Fail([scriptblock]$Action) { $failed=$false;try { & $Action | Out-Null } catch {$failed=$true};if (-not $failed) {throw 'Expected refusal did not happen'};$script:checks++ }
function New-Fixture([int]$Age) {
 $run=[Guid]::NewGuid();$dir=Join-Path $src ('run-'+$run.ToString('N'));New-Item -ItemType Directory -Path $dir | Out-Null
 $files=@();foreach ($name in @('database.dump','globals.sql')) {
  $path=Join-Path $dir $name;[IO.File]::WriteAllText($path,'synthetic backup test '+$run)
  $files+=@{Name=$name;Length=(Get-Item $path).Length;Sha256=(Get-FileHash $path -Algorithm SHA256).Hash}
 }
 @{Format=1;State='VERIFIED';RootId=$id.ToString();RunId=$run.ToString();StartedUtc=[DateTime]::UtcNow.AddDays(-$Age).ToString('o');VerifiedUtc=[DateTime]::UtcNow.ToString('o');Database='sess_nexa_erp';Files=$files} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $dir 'manifest.json')
 return $dir
}
$b1=New-Fixture 3;$b2=New-Fixture 2;$b3=New-Fixture 1
foreach ($b in @($b1,$b2,$b3)) { $null=Copy-VerifiedBundle $src $b $dst };$checks++
$null=Copy-VerifiedBundle $src $b3 $dst;$checks++ # replay is a byte comparison, not overwrite
$remote=Join-Path $dst ([IO.Path]::GetFileName($b1))
[IO.File]::AppendAllText((Join-Path $remote 'database.dump'),'corrupt')
Must-Fail { Remove-OffMachineCopiedHistory $src $dst }
if (-not (Test-Path $b1)) {throw 'Retention deleted without good off-machine copy'}
Copy-Item -LiteralPath (Join-Path $b1 'database.dump') -Destination (Join-Path $remote 'database.dump')
Remove-OffMachineCopiedHistory $src $dst
if ((Test-Path $b1) -or -not (Test-Path $b2) -or -not (Test-Path $b3)) {throw 'Wrong local retention'};$checks++
Must-Fail { Remove-OffMachineCopiedHistory $src $dst 1 }
[IO.File]::WriteAllText((Join-Path $b3 'unexpected.txt'),'do not remove')
Must-Fail { Copy-VerifiedBundle $src $b3 $dst }
Remove-Item -LiteralPath (Join-Path $b3 'unexpected.txt')
Must-Fail { Copy-VerifiedBundle $src $b3 $src }
$foreign=Join-Path $root 'foreign';New-Item -ItemType Directory -Path $foreign | Out-Null
@{Format=1;Id=[Guid]::NewGuid().ToString()} | ConvertTo-Json | Set-Content (Join-Path $foreign '.sess-verified-backups.json')
Must-Fail { Copy-VerifiedBundle $src $b3 $foreign }
$bad=Join-Path $root 'nonempty';New-Item -ItemType Directory -Path $bad | Out-Null;Set-Content (Join-Path $bad 'keep.txt') 'untouched'
Must-Fail { Copy-VerifiedBundle $src $b3 $bad }
Must-Fail { Get-VerifiedBundle $src $remote }
Must-Fail { Assert-BackupCapacity 26 2 }
Assert-BackupCapacity 28 2;$checks++
Must-Fail { Assert-BackupCapacity 40 0 }
$mp=Join-Path $b3 'manifest.json';$original=[IO.File]::ReadAllText($mp);$m=$original | ConvertFrom-Json
$m.Files[0].Name='../outside';$m | ConvertTo-Json -Depth 8 | Set-Content $mp
Must-Fail { Get-VerifiedBundle $src $b3 }
[IO.File]::WriteAllText($mp,$original)
$m=$original | ConvertFrom-Json;$m.Files=@($m.Files)+@($m.Files[0]);$m | ConvertTo-Json -Depth 8 | Set-Content $mp
Must-Fail { Get-VerifiedBundle $src $b3 }
[IO.File]::WriteAllText($mp,$original)
# Windows directory junction: refusal must happen before following it.
$link=Join-Path $root 'junction';New-Item -ItemType Junction -Path $link -Target $src | Out-Null
Must-Fail { Get-BackupRootId $link }
# Preserve evidence; no broad recursive deletion from a computed temporary path.
Write-Output "PASSED: $checks transfer/capacity/retention checks; synthetic evidence $root"
