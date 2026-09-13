[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$InstallerPath,
    [Parameter(Mandatory=$true)][string]$ConfigPath,
    [Parameter(Mandatory=$true)][string]$CredentialFile,
    [Parameter(Mandatory=$true)][string]$LogDirectory,
    [string]$TaskName='SESS-NexaERP-VerifiedBackup',
    [string]$DailyAt='02:00',
    [PSCredential]$Credential,
    [switch]$CurrentUserWitness
)
$ErrorActionPreference='Stop'
if($TaskName -notmatch '\ASESS-NexaERP-[A-Za-z0-9-]+\z') { throw 'Task name must start with SESS-NexaERP- and contain only letters, numbers and hyphens.' }
$at=[DateTime]::ParseExact($DailyAt,'HH:mm',[Globalization.CultureInfo]::InvariantCulture)
$wrapper=Join-Path $PSScriptRoot 'Invoke-VerifiedDatabaseBackup.ps1'
foreach($path in @($InstallerPath,$ConfigPath,$CredentialFile,$LogDirectory,$wrapper)) {
    if(-not [IO.Path]::IsPathRooted($path) -or $path.IndexOfAny([char[]]@([char]34,[char]13,[char]10)) -ge 0 -or -not (Test-Path -LiteralPath $path)) {
        throw 'Existing absolute paths without quote/newline characters are required.'
    }
    $current=[IO.Path]::GetFullPath($path)
    while($current) {
        if(Test-Path -LiteralPath $current) {
            if(((Get-Item -LiteralPath $current -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw 'Scheduled backup paths cannot contain reparse points.'
            }
        }
        $current=[IO.Path]::GetDirectoryName($current)
    }
}
if(Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue) { throw 'A task with that name already exists; review it explicitly before replacement.' }
$arguments='-WindowStyle Hidden -NoProfile -NonInteractive -File "'+$wrapper+'" -InstallerPath "'+$InstallerPath+'" -ConfigPath "'+$ConfigPath+'" -CredentialFile "'+$CredentialFile+'" -LogDirectory "'+$LogDirectory+'"'
$powerShell=Join-Path $env:SystemRoot 'System32/WindowsPowerShell/v1.0/powershell.exe'
$action=New-ScheduledTaskAction -Execute $powerShell -Argument $arguments
$trigger=New-ScheduledTaskTrigger -Daily -At $at
$settings=New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable -MultipleInstances IgnoreNew -RestartCount 2 -RestartInterval (New-TimeSpan -Minutes 5) -ExecutionTimeLimit (New-TimeSpan -Hours 4)
if($CurrentUserWitness) {
    if($Credential) { throw 'Witness mode and account credentials are mutually exclusive.' }
    $account=[Security.Principal.WindowsIdentity]::GetCurrent().Name
    $principal=New-ScheduledTaskPrincipal -UserId $account -LogonType Interactive -RunLevel Limited
    $task=New-ScheduledTask -Action $action -Trigger $trigger -Settings $settings -Principal $principal -Description 'Disposable logged-in-account backup witness; unregister after test.'
    Register-ScheduledTask -TaskName $TaskName -InputObject $task | Out-Null
}
else {
    if(-not $Credential) { throw 'Supply the Windows scheduled-account credential. Export the database SecureString using that same account.' }
    $task=New-ScheduledTask -Action $action -Trigger $trigger -Settings $settings -Description 'Daily PostgreSQL backup with matching globals, automatic restore verification and guarded retention.'
    Register-ScheduledTask -TaskName $TaskName -InputObject $task -User $Credential.UserName -Password $Credential.GetNetworkCredential().Password | Out-Null
}
Get-ScheduledTask -TaskName $TaskName | Select-Object TaskName,State
