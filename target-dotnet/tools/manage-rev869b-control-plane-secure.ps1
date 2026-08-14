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
    [string]$AuthorizationReference,
    [string]$ExpectedSystemIdentifier,
    [string]$ExpectedServerAddress,
    [string]$ExpectedManifestSha256,
    [string]$ExpectedSourceCommit,
    [Guid]$ExecutionInstanceId
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'
$exactTarget = 'sess_nexaerp_rev869b_control_plane'
$policy = 'MGMT-REV869B-CONTROL-PLANE-20260813-001'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$artifacts = [ordered]@{
    Preflight = Join-Path $scriptRoot 'rev869b-control-plane-preflight.sql'
    Bootstrap = Join-Path $scriptRoot 'rev869b-control-plane-bootstrap.sql'
    Install = Join-Path $scriptRoot 'rev869b-control-plane-install.sql'
    Verify = Join-Path $scriptRoot 'rev869b-control-plane-verify.sql'
    Rollback = Join-Path $scriptRoot 'rev869b-control-plane-rollback.sql'
    Deprovision = Join-Path $scriptRoot 'rev869b-control-plane-deprovision.sql'
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
        '--username',$AdministrativeUser,'--dbname',$Database,
        '--set',"expected_system_identifier=$ExpectedSystemIdentifier",
        '--set',"expected_server_address=$ExpectedServerAddress",'--set',"expected_server_port=$Port",
        '--set',"expected_administrative_user=$AdministrativeUser",'--set',"target_database=$TargetDatabase",
        '--set',"expected_manifest_sha256=$ExpectedManifestSha256",'--set',"expected_source_commit=$ExpectedSourceCommit",
        '--set',"execution_instance_id=$ExecutionInstanceId",'--file',$Path)
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
$manifestCanonical = ($manifest | ForEach-Object { "$($_.Name):$($_.File):$($_.Bytes):$($_.Sha256)" }) -join '|'
$sha = [Security.Cryptography.SHA256]::Create()
try { $computedManifestSha256 = ([BitConverter]::ToString(
    $sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($manifestCanonical)))).Replace('-','') }
finally { $sha.Dispose() }
$databaseMode = $Mode -ne 'GeneratePlanOnly'
if ($databaseMode) {
    if ($HostName -cne $ExpectedServerAddress -or $AdministrativeDatabase -cne 'postgres' -or
        $AdministrativeUser -notmatch '^[a-z_][a-z0-9_]{0,62}$' -or
        $ExpectedSystemIdentifier -notmatch '^[0-9]{10,20}$' -or
        $ExpectedManifestSha256 -cne $computedManifestSha256 -or
        $ExpectedSourceCommit -notmatch '^[0-9a-f]{40}$' -or $ExecutionInstanceId -eq [Guid]::Empty) {
        throw 'Exact external cluster, administrator, manifest, source-commit, and execution identity is required.'
    }
}
$evidence = [ordered]@{
    Mode = $Mode
    Policy = $policy
    TargetDatabase = $TargetDatabase
    HostFingerprint = (Get-FileHash -LiteralPath $MyInvocation.MyCommand.Path -Algorithm SHA256).Hash
    Artifacts = $manifest
    ManifestSha256 = $computedManifestSha256
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
        Invoke-PsqlFile 'postgres' $artifacts.Preflight
        $evidence.PostgreSqlAccessed = $true
        $evidence | ConvertTo-Json -Depth 5
    }
    'ProvisionAuthorized' {
        if ($AuthorizationReference -cne 'MGMT-REV869B-CONTROL-PLANE-PROVISION') {
            throw 'A separate exact provisioning authorization reference is required.'
        }
        Invoke-PsqlFile 'postgres' $artifacts.Preflight
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
        Invoke-PsqlFile 'postgres' $artifacts.Deprovision
        $evidence.PostgreSqlAccessed = $true
        $evidence | ConvertTo-Json -Depth 5
    }
}
