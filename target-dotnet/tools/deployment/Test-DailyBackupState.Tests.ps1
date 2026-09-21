$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot '../Test-DailyBackupState.ps1')
$fixture=Join-Path $env:TEMP ('sess-daily-state-'+[guid]::NewGuid()+'.json')
$now=[DateTimeOffset]::UtcNow
$count=0
function Case($name,$receipt,$enabled,$result,$expected) {
 $receipt | ConvertTo-Json | Set-Content -LiteralPath $fixture
 $actual=Test-DailyBackupState -StatusPath $fixture -TaskEnabled $enabled -TaskResult $result -Now $now
 if ($actual.Ok -ne $expected) { throw "FAILED $name : $($actual.Detail)" }
 $script:count++
}
$good=@{FinishedUtc=$now.AddHours(-1).ToString('o');ExitCode=0;OffMachineVerified=$true}
Case good $good $true 0 $true
Case disabled $good $false 0 $false
Case failedTask $good $true 1 $false
Case runningTask $good $true 267009 $false
Case stale @{FinishedUtc=$now.AddHours(-27).ToString('o');ExitCode=0;OffMachineVerified=$true} $true 0 $false
Case future @{FinishedUtc=$now.AddHours(1).ToString('o');ExitCode=0;OffMachineVerified=$true} $true 0 $false
Case copyFailed @{FinishedUtc=$now.ToString('o');ExitCode=1;OffMachineVerified=$false} $true 0 $false
Case localOnly @{FinishedUtc=$now.ToString('o');ExitCode=0} $true 0 $false
Case stringBoolean @{FinishedUtc=$now.ToString('o');ExitCode=0;OffMachineVerified='true'} $true 0 $false
Case missingExit @{FinishedUtc=$now.ToString('o');OffMachineVerified=$true} $true 0 $false
Case invalidDate @{FinishedUtc='bad';ExitCode=0;OffMachineVerified=$true} $true 0 $false
'{' | Set-Content -LiteralPath $fixture
if ((Test-DailyBackupState -StatusPath $fixture -TaskEnabled $true -TaskResult 0).Ok) { throw 'Malformed JSON accepted' }; $count++
Remove-Item -LiteralPath $fixture
if ((Test-DailyBackupState -StatusPath $fixture -TaskEnabled $true -TaskResult 0).Ok) { throw 'Missing receipt accepted' }; $count++
Write-Output "$count daily backup state cases passed"
