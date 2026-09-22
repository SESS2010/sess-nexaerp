[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ConfigPath,
    [Parameter(Mandatory)][PSCredential]$Credential,
    [string]$TaskName='SESS-NexaERP-Identity-Monitor'
)
$ErrorActionPreference='Stop'
if($TaskName -notmatch '\ASESS-NexaERP-[A-Za-z0-9-]+\z'){throw 'Use a SESS-NexaERP task name.'}
$wrapper=Join-Path $PSScriptRoot 'Test-IdentityOperations.ps1'
foreach($path in @($ConfigPath,$wrapper)){
    if(-not [IO.Path]::IsPathRooted($path) -or $path.IndexOfAny([char[]]@([char]34,[char]13,[char]10)) -ge 0 -or -not (Test-Path -LiteralPath $path -PathType Leaf)){
        throw 'Existing absolute config/script paths without quotes or newlines are required.'
    }
    for($part=[IO.Path]::GetFullPath($path);$part;$part=[IO.Path]::GetDirectoryName($part)){
        if((Test-Path -LiteralPath $part) -and ((Get-Item -LiteralPath $part -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Monitor paths cannot contain reparse points.'}
    }
}
if(Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue){throw 'Existing task must be reviewed before replacement.'}
$arguments='-WindowStyle Hidden -NoProfile -NonInteractive -File "'+$wrapper+'" -ConfigPath "'+$ConfigPath+'"'
$action=New-ScheduledTaskAction -Execute (Join-Path $env:SystemRoot 'System32/WindowsPowerShell/v1.0/powershell.exe') -Argument $arguments
$trigger=New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes(1) -RepetitionInterval (New-TimeSpan -Minutes 1)
$settings=New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable -MultipleInstances IgnoreNew -ExecutionTimeLimit (New-TimeSpan -Minutes 2)
$task=New-ScheduledTask -Action $action -Trigger $trigger -Settings $settings -Description 'Checks identity readiness, both realms, and ERP/identity verified backup freshness. No outbound messages.'
Register-ScheduledTask -TaskName $TaskName -InputObject $task -User $Credential.UserName -Password $Credential.GetNetworkCredential().Password | Out-Null
Get-ScheduledTask -TaskName $TaskName | Select-Object TaskName,State
