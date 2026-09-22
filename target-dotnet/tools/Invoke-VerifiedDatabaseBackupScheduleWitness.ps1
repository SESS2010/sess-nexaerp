[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$RepositoryRoot,
    [Parameter(Mandatory=$true)][string]$InstallerPath,
    [Parameter(Mandatory=$true)][string]$ConfigPath,
    [Parameter(Mandatory=$true)][string]$EvidenceDirectory
)
$ErrorActionPreference='Stop'
$config=Get-Content -LiteralPath $ConfigPath -Raw | ConvertFrom-Json
if($config.ExpectedHost -ne '127.0.0.1' -or $config.ExpectedDatabase -notin @('advance_parser','keycloak_witness') -or
    $config.ExpectedPort -le 1024 -or $config.ExpectedPort -eq 5432 -or $config.ExpectedPort -eq 18444) {
    throw 'Schedule witness requires the disposable Windows source database.'
}
$connection=[Environment]::GetEnvironmentVariable([string]$config.ConnectionEnvironment,'Process')
if([string]::IsNullOrWhiteSpace($connection)) { throw 'The disposable connection environment variable is missing.' }
$taskName='SESS-NexaERP-Backup-Witness-'+[Guid]::NewGuid().ToString('N')
$credentialFile=Join-Path $EvidenceDirectory 'scheduled-connection.clixml'
$logDirectory=Join-Path $EvidenceDirectory 'scheduled-logs'
if(Test-Path -LiteralPath $credentialFile) { throw 'Witness credential path must be new.' }
New-Item -ItemType Directory -Path $logDirectory -ErrorAction Stop | Out-Null
ConvertTo-SecureString -String $connection -AsPlainText -Force | Export-Clixml -LiteralPath $credentialFile
$connection=$null
$registered=$false
try {
    & (Join-Path $RepositoryRoot 'tools/Register-VerifiedDatabaseBackup.ps1') -InstallerPath $InstallerPath -ConfigPath $ConfigPath -CredentialFile $credentialFile -LogDirectory $logDirectory -TaskName $taskName -CurrentUserWitness -DailyAt '02:00'
    $registered=$true
    $task=Get-ScheduledTask -TaskName $taskName
    if($task.Settings.MultipleInstances.ToString() -ne 'IgnoreNew' -or -not $task.Settings.StartWhenAvailable -or $task.Settings.RestartCount -ne 2) {
        throw 'Registered task settings differ from the required schedule.'
    }
    Export-ScheduledTask -TaskName $taskName | Set-Content -LiteralPath (Join-Path $EvidenceDirectory 'scheduled-task.xml') -Encoding UTF8
    $requested=Get-Date
    Start-ScheduledTask -TaskName $taskName
    $deadline=(Get-Date).AddMinutes(20)
    $resultPath=Join-Path $logDirectory 'last-run.json'
    do {
        Start-Sleep -Seconds 2
        $task=Get-ScheduledTask -TaskName $taskName
        if((Get-Date) -gt $deadline) { throw 'Scheduled backup witness timed out.' }
        $info=Get-ScheduledTaskInfo -TaskName $taskName
        if($task.State -ne 'Running' -and $info.LastTaskResult -notin @(0,267009,267011)) {
            throw ('Scheduled backup exited before producing success: '+$info.LastTaskResult)
        }

    } while(-not (Test-Path -LiteralPath $resultPath) -or $task.State -eq 'Running')
    $info=Get-ScheduledTaskInfo -TaskName $taskName
    $result=Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json
    [ordered]@{ LastTaskResult=$info.LastTaskResult; LastRunTime=$info.LastRunTime.ToUniversalTime().ToString('o'); Result=$result } |
        ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $EvidenceDirectory 'scheduled-result.json') -Encoding UTF8
    if($info.LastTaskResult -ne 0 -or $result.ExitCode -ne 0) { throw 'The scheduled backup did not finish successfully.' }
}
finally {
    if(Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue) {
        $task=Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
        if($task -and $task.State -eq 'Running') {
            # Stop only this disposable task; retain private restore evidence for explicit cleanup on timeout.
            Stop-ScheduledTask -TaskName $taskName
        }
        Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
    }
    if(Test-Path -LiteralPath $credentialFile) { Remove-Item -LiteralPath $credentialFile }
    if(Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue) { throw 'Disposable backup task still exists after cleanup.' }
    [ordered]@{ Task=$taskName; Removed=$true } | ConvertTo-Json |
        Set-Content -LiteralPath (Join-Path $EvidenceDirectory 'scheduled-cleanup.json') -Encoding UTF8

}
