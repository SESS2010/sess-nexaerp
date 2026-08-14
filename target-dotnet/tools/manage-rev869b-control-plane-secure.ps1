[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('GeneratePlanOnly','PreflightOnly','ProvisionAuthorized','PostProvisionVerification','RollbackAuthorized')]
    [string]$Mode,
    [string]$HostName = '127.0.0.1',
    [ValidateRange(1,65535)][int]$Port = 5432,
    [string]$AdministrativeDatabase = 'postgres',
    [string]$AdministrativeUser,
    [string]$TargetDatabase = 'sess_nexaerp_rev869b_control_plane',
    [string]$AuthorizationReference
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'
$exactTarget = 'sess_nexaerp_rev869b_control_plane'
$policy = 'MGMT-REV869B-CONTROL-PLANE-20260813-001'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$artifacts = [ordered]@{
    Bootstrap = Join-Path $scriptRoot 'rev869b-control-plane-bootstrap.sql'
    Install = Join-Path $scriptRoot 'rev869b-control-plane-install.sql'
    Verify = Join-Path $scriptRoot 'rev869b-control-plane-verify.sql'
    Rollback = Join-Path $scriptRoot 'rev869b-control-plane-rollback.sql'
}

function Assert-ExactTarget {
    param([string]$Value)
    if ($Value -cne $exactTarget) { throw 'Only the exact isolated REV869B control-plane database is permitted.' }
}

function Assert-Artifact {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "Required provisioning artifact is missing: $Path" }
    $content = Get-Content -LiteralPath $Path -Raw
    if ([string]::IsNullOrWhiteSpace($content) -or $content.Contains('...') -or
        $content -match '(?i)(password\s*=|bearer\s+|client_secret|private[_ -]?key)') {
        throw "Provisioning artifact is empty, truncated, or contains prohibited credential material: $Path"
    }
}

function Invoke-PsqlFile {
    param([string]$Database,[string]$Path)
    if ([string]::IsNullOrWhiteSpace($AdministrativeUser)) { throw 'AdministrativeUser is required for database modes.' }
    $arguments = @('--no-password','--set','ON_ERROR_STOP=1','--host',$HostName,'--port',$Port,
        '--username',$AdministrativeUser,'--dbname',$Database,'--file',$Path)
    & psql @arguments
    if ($LASTEXITCODE -ne 0) { throw "psql failed for the sanitized artifact $([IO.Path]::GetFileName($Path))." }
}

Assert-ExactTarget $TargetDatabase
foreach ($entry in $artifacts.GetEnumerator()) { Assert-Artifact $entry.Value }
$manifest = foreach ($entry in $artifacts.GetEnumerator()) {
    $item = Get-Item -LiteralPath $entry.Value
    [ordered]@{
        Name = $entry.Key
        File = $item.Name
        Bytes = $item.Length
        Sha256 = (Get-FileHash -LiteralPath $entry.Value -Algorithm SHA256).Hash
    }
}
$evidence = [ordered]@{
    Mode = $Mode
    Policy = $policy
    TargetDatabase = $TargetDatabase
    HostFingerprint = (Get-FileHash -LiteralPath $MyInvocation.MyCommand.Path -Algorithm SHA256).Hash
    Artifacts = $manifest
    ContainsCredential = $false
    PostgreSqlAccessed = $false
    TimestampUtc = [DateTimeOffset]::UtcNow.ToString('O')
}

switch ($Mode) {
    'GeneratePlanOnly' {
        $evidence | ConvertTo-Json -Depth 5
        return
    }
    'PreflightOnly' {
        if ($AdministrativeDatabase -cne 'postgres') { throw 'Preflight must use the exact postgres administrative database.' }
        $preflight = @"
SELECT CASE WHEN current_database()='postgres'
 AND NOT EXISTS(SELECT 1 FROM pg_database WHERE datname IN ('template0','template1') AND datallowconn)
 THEN 'REV869B_PREFLIGHT_READ_ONLY' ELSE 1/0::text END;
"@
        $temporary = Join-Path ([IO.Path]::GetTempPath()) ('rev869b-preflight-' + [Guid]::NewGuid().ToString('N') + '.sql')
        try {
            [IO.File]::WriteAllText($temporary,$preflight,[Text.UTF8Encoding]::new($false))
            Invoke-PsqlFile $AdministrativeDatabase $temporary
            $evidence.PostgreSqlAccessed = $true
            $evidence | ConvertTo-Json -Depth 5
        } finally { if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Force } }
    }
    'ProvisionAuthorized' {
        if ($AuthorizationReference -cne 'MGMT-REV869B-CONTROL-PLANE-PROVISION') {
            throw 'A separate exact provisioning authorization reference is required.'
        }
        if ($AdministrativeDatabase -cne 'postgres') { throw 'Provisioning bootstrap must use postgres.' }
        Invoke-PsqlFile 'postgres' $artifacts.Bootstrap
        Invoke-PsqlFile $TargetDatabase $artifacts.Install
        Invoke-PsqlFile $TargetDatabase $artifacts.Verify
        $evidence.PostgreSqlAccessed = $true
        $evidence | ConvertTo-Json -Depth 5
    }
    'PostProvisionVerification' {
        Invoke-PsqlFile $TargetDatabase $artifacts.Verify
        $evidence.PostgreSqlAccessed = $true
        $evidence | ConvertTo-Json -Depth 5
    }
    'RollbackAuthorized' {
        if ($AuthorizationReference -cne 'MGMT-REV869B-CONTROL-PLANE-ROLLBACK') {
            throw 'A separate exact rollback authorization reference is required.'
        }
        Invoke-PsqlFile $TargetDatabase $artifacts.Rollback
        $evidence.PostgreSqlAccessed = $true
        $evidence | ConvertTo-Json -Depth 5
    }
}
