[CmdletBinding()]
param([Parameter(Mandatory)][string]$ConfigPath)
$ErrorActionPreference='Stop'
function CheckedPath([string]$path) {
    if(-not [IO.Path]::IsPathRooted($path)){throw 'Absolute monitoring paths are required.'}
    $full=[IO.Path]::GetFullPath($path)
    for($part=$full;$part;$part=[IO.Path]::GetDirectoryName($part)){
        if((Test-Path -LiteralPath $part) -and ((Get-Item -LiteralPath $part -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Monitoring paths cannot contain reparse points.'}
    }
    return $full
}
function ReadEndpoint([string]$address){
    $uri=[Uri]$address
    if(-not $uri.IsAbsoluteUri -or ($uri.Scheme -ne 'https' -and -not ($uri.Scheme -eq 'http' -and $uri.IsLoopback))){throw 'Monitor endpoints require HTTPS, except loopback test endpoints.'}
    return Invoke-RestMethod -Uri $uri -TimeoutSec 10 -MaximumRedirection 0 -UseBasicParsing
}
$config=Get-Content -LiteralPath (CheckedPath $ConfigPath) -Raw | ConvertFrom-Json
$directory=CheckedPath ([string]$config.StatusDirectory)
if(-not (Test-Path -LiteralPath $directory -PathType Container)){throw 'Create the protected monitoring status directory first.'}
if($config.MaximumBackupAgeHours -lt 1 -or $config.MaximumBackupAgeHours -gt 168){throw 'Set MaximumBackupAgeHours between 1 and 168.'}
$checks=[Collections.Generic.List[object]]::new()
try{
    $ready=ReadEndpoint $config.ReadyUrl
    $databaseChecks=@($ready.checks | Where-Object {$_.name -match 'database'})
    $healthy=$ready.status -eq 'UP' -and $databaseChecks.Count -gt 0 -and @($databaseChecks | Where-Object {$_.status -ne 'UP'}).Count -eq 0
    $checks.Add(@{Name='Identity server and database';Healthy=$healthy})
}catch{$checks.Add(@{Name='Identity server and database';Healthy=$false})}
if(@($config.Realms).Count -ne 2 -or @($config.Realms | Where-Object {$_.Name -eq 'Staff'}).Count -ne 1 -or @($config.Realms | Where-Object {$_.Name -eq 'Approvers'}).Count -ne 1 -or @($config.Realms.Issuer | Sort-Object -Unique).Count -ne 2){throw 'Configure both Staff and Approvers realms.'}
foreach($realm in $config.Realms){
    $healthy=$false
    try{
        $metadata=ReadEndpoint $realm.DiscoveryUrl
        if($metadata.issuer -ceq $realm.Issuer -and $metadata.jwks_uri -ceq $realm.JwksUrl){
            $keys=ReadEndpoint $realm.JwksUrl
            $healthy=@($keys.keys | Where-Object {$_.kid -and $_.kty -in @('RSA','EC')}).Count -gt 0
        }
    }catch{$healthy=$false}
    $checks.Add(@{Name=([string]$realm.Name+' sign-in configuration');Healthy=$healthy})
}
if(@($config.BackupResults | Where-Object {$_.Name -eq 'ERP'}).Count -ne 1 -or @($config.BackupResults | Where-Object {$_.Name -eq 'Identity'}).Count -ne 1){throw 'Configure ERP and identity backup results.'}
foreach($backup in $config.BackupResults){
    $healthy=$false
    try{
        $result=Get-Content -LiteralPath (CheckedPath $backup.Path) -Raw | ConvertFrom-Json
        $age=([DateTimeOffset]::UtcNow-[DateTimeOffset]::Parse($result.FinishedUtc)).TotalHours
        $healthy=$null -ne $result.ExitCode -and $result.ExitCode -eq 0 -and $age -ge 0 -and $age -le $config.MaximumBackupAgeHours
        if($backup.TaskName){
            $task=Get-ScheduledTaskInfo -TaskName ([string]$backup.TaskName) -ErrorAction Stop
            $healthy=$healthy -and $task.LastTaskResult -in @(0,267009)
        }
    }catch{$healthy=$false}
    $checks.Add(@{Name=([string]$backup.Name+' verified backup');Healthy=$healthy})
}
$healthy=@($checks | Where-Object {-not $_.Healthy}).Count -eq 0
$state=if($healthy){'HEALTHY'}else{'ACTION_REQUIRED'}
$observed=[DateTimeOffset]::UtcNow
$result=[ordered]@{State=$state;CheckedUtc=$observed.ToString('o');Checks=@($checks.ToArray())}
$jsonPath=CheckedPath (Join-Path $directory 'identity-status.json')
$htmlPath=CheckedPath (Join-Path $directory 'identity-status.html')
$temporary=CheckedPath (Join-Path $directory ('status-'+[Guid]::NewGuid().ToString('N')+'.pending'))
try{
    $result | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $temporary -Encoding UTF8
    if(Test-Path -LiteralPath $jsonPath){[IO.File]::Replace($temporary,$jsonPath,[System.Management.Automation.Language.NullString]::Value)}else{[IO.File]::Move($temporary,$jsonPath)}
    $rows=($checks | ForEach-Object {'<li>'+[Net.WebUtility]::HtmlEncode([string]$_.Name)+': '+$(if($_.Healthy){'OK'}else{'CHECK REQUIRED'})+'</li>'}) -join ''
    $color=if($healthy){'#126b35'}else{'#a21b18'}
    $stamp=$observed.ToUnixTimeMilliseconds()
    $html='<!doctype html><html lang="en"><meta charset="utf-8"><meta http-equiv="refresh" content="30"><title>SESS identity and backup status</title><body style="font:20px sans-serif;padding:2rem"><h1 id="state" style="color:'+$color+'">'+$state+'</h1><p>Checked: '+$observed.ToString('u')+'</p><ul>'+$rows+'</ul><p>If red, contact the designated SESS IT maintainer.</p><script>setInterval(function(){if(Date.now()-'+$stamp+'>120000){var e=document.getElementById("state");e.textContent="MONITOR STALE - CONTACT IT";e.style.color="#a21b18"}},1000)</script></body></html>'
    [IO.File]::WriteAllText($temporary,$html,[Text.UTF8Encoding]::new($false))
    if(Test-Path -LiteralPath $htmlPath){[IO.File]::Replace($temporary,$htmlPath,[System.Management.Automation.Language.NullString]::Value)}else{[IO.File]::Move($temporary,$htmlPath)}
}finally{if(Test-Path -LiteralPath $temporary){Remove-Item -LiteralPath $temporary}}
Write-Output $state
if(-not $healthy){exit 1}
