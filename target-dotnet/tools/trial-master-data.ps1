[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Apply','Remove')]
    [string]$Action
)

$ErrorActionPreference = 'Stop'

if ($env:DOTNET_ENVIRONMENT -cne 'Development') {
    throw 'Trial master data is restricted to DOTNET_ENVIRONMENT=Development.'
}
if ($env:NexaErp__AllowTrialData -cne 'true') {
    throw 'Set NexaErp__AllowTrialData=true explicitly for this process.'
}
if ([string]::IsNullOrWhiteSpace($env:NexaErp__ExpectedDatabase)) {
    throw 'Set NexaErp__ExpectedDatabase to the exact development database name.'
}
if ([string]::IsNullOrWhiteSpace($env:PGDATABASE) -or
    $env:PGDATABASE -cne $env:NexaErp__ExpectedDatabase) {
    throw 'PGDATABASE must exactly match NexaErp__ExpectedDatabase.'
}
foreach ($name in @('PGHOST','PGPORT','PGUSER')) {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($name))) {
        throw "Set $name for the target development cluster."
    }
}

function Find-Psql {
    if (-not [string]::IsNullOrWhiteSpace($env:ADVANCE_POSTGRES_BIN)) {
        $candidate = Join-Path $env:ADVANCE_POSTGRES_BIN 'psql.exe'
        if (Test-Path -LiteralPath $candidate -PathType Leaf) { return $candidate }
    }
    $root = Join-Path $env:ProgramFiles 'PostgreSQL'
    if (Test-Path -LiteralPath $root -PathType Container) {
        $candidate = Get-ChildItem -LiteralPath $root -Directory |
            Sort-Object { [version]$_.Name } -Descending |
            ForEach-Object { Join-Path $_.FullName 'bin\psql.exe' } |
            Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
            Select-Object -First 1
        if ($candidate) { return $candidate }
    }
    throw 'PostgreSQL psql was not found. Set ADVANCE_POSTGRES_BIN.'
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$scriptName = if ($Action -ceq 'Apply') { 'trial-master-data-apply.sql' } else { 'trial-master-data-remove.sql' }
$sqlPath = Join-Path $repositoryRoot "database\postgresql\$scriptName"
$psql = Find-Psql

& $psql -X --set ON_ERROR_STOP=1 --set "expected_database=$($env:NexaErp__ExpectedDatabase)" --file $sqlPath
if ($LASTEXITCODE -ne 0) { throw "Trial master data $Action failed with psql exit code $LASTEXITCODE." }
Write-Host "Trial master data $Action completed for $($env:PGDATABASE)."
