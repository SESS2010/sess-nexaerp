[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$helperPath = Join-Path $PSScriptRoot "apply-rev866-secure.ps1"
$source = Get-Content -LiteralPath $helperPath -Raw

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Assert-SafePgIdentifier([string]$Name, [string]$Label) {
    if ($Name -notmatch '^[A-Za-z_][A-Za-z0-9_]{0,62}$') {
        throw "$Label is not a safe PostgreSQL identifier."
    }
}

function Quote-PgIdentifier([string]$Name) {
    Assert-SafePgIdentifier $Name "PostgreSQL identifier"
    return '"' + $Name.Replace('"', '""') + '"'
}

function Join-PgQualifiedIdentifier([string]$SchemaName, [string]$TableName) {
    return (Quote-PgIdentifier $SchemaName) + "." + (Quote-PgIdentifier $TableName)
}

function Parse-HistoryDiscovery([string]$Raw) {
    $delimiter = [char]31
    $tables = @()
    foreach ($line in @($Raw -split "`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
        $parts = $line -split [regex]::Escape($delimiter), 2
        if ($parts.Count -ne 2) { throw "Could not parse EF migration history table discovery output safely." }
        Assert-SafePgIdentifier $parts[0] "Migration history schema"
        Assert-SafePgIdentifier $parts[1] "Migration history table"
        $tables += [pscustomobject]@{
            Schema = $parts[0]
            Table = $parts[1]
            QualifiedTable = Join-PgQualifiedIdentifier $parts[0] $parts[1]
        }
    }
    if ($tables.Count -eq 0) { throw "No __EFMigrationsHistory table found in database." }
    return $tables
}

$expectedRelation = '"public"."__EFMigrationsHistory"'
Assert-True ((Join-PgQualifiedIdentifier "public" "__EFMigrationsHistory") -eq $expectedRelation) "Fully qualified relation generation failed."
Assert-True (("SELECT `"MigrationId`" FROM $expectedRelation ORDER BY `"MigrationId`";") -eq 'SELECT "MigrationId" FROM "public"."__EFMigrationsHistory" ORDER BY "MigrationId";') "MigrationId query generation failed."

$single = @(Parse-HistoryDiscovery ("public" + [char]31 + "__EFMigrationsHistory"))
Assert-True ($single.Count -eq 1) "Single public history table was not parsed."
Assert-True ($single[0].Schema -eq "public") "Public schema was not preserved."
Assert-True ($single[0].Table -eq "__EFMigrationsHistory") "Mixed-case history table was not preserved."
Assert-True ($single[0].QualifiedTable -eq $expectedRelation) "Public mixed-case relation was not quoted correctly."

$multiple = @(Parse-HistoryDiscovery (("public" + [char]31 + "__EFMigrationsHistory") + "`n" + ("nexa" + [char]31 + "__EFMigrationsHistory")))
Assert-True ($multiple.Count -eq 2) "Multiple history tables were not parsed."
Assert-True ($multiple[1].QualifiedTable -eq '"nexa"."__EFMigrationsHistory"') "Second history table relation was not quoted correctly."

$noTableFailed = $false
try { Parse-HistoryDiscovery "" | Out-Null } catch { $noTableFailed = $_.Exception.Message -like "No __EFMigrationsHistory table found*" }
Assert-True $noTableFailed "No discovered history table did not fail safely."

$invalidFailed = $false
try { Parse-HistoryDiscovery ("public.bad" + [char]31 + "__EFMigrationsHistory") | Out-Null } catch { $invalidFailed = $_.Exception.Message -like "*safe PostgreSQL identifier*" }
Assert-True $invalidFailed "Invalid identifier was not rejected."

Assert-True ($source -match 'chr\(31\)') "Helper does not use an unambiguous delimiter for psql output."
Assert-True ($source -match 'SELECT `"MigrationId`" FROM \$\(\$historyTable\.QualifiedTable\) ORDER BY `"MigrationId`";') "Helper does not query mixed-case MigrationId through the qualified relation."
Assert-True ($source -match 'Join-PgQualifiedIdentifier') "Helper does not build schema and table as separately quoted identifiers."
Assert-True ($source -match '-f \$sqlFile') "Helper does not preserve SQL through a script file."
Assert-True ($source -match '\$exitCode = \$LASTEXITCODE') "Helper does not capture psql exit code."
Assert-True ($source -match 'psql failed with exit code') "Helper does not fail clearly on psql errors."
Assert-True ($source -match '\$previousErrorActionPreference = \$ErrorActionPreference') "Helper does not preserve PowerShell ErrorActionPreference around psql."
Assert-True ($source -match '\$ErrorActionPreference = "Continue"') "Helper does not prevent native stderr from bypassing psql exit-code handling."

Assert-True ($source -match 'function Resolve-RipgrepExecutable') "Helper does not resolve rg.exe safely."
Assert-True ($source -match 'function Invoke-SecretScan') "Helper does not wrap secret scanning in a safe function."
Assert-True ($source -match '\$secretScanExitCode -eq 0') "Helper does not treat rg exit code 0 as finding/fail."
Assert-True ($source -match '\$secretScanExitCode -gt 1') "Helper does not treat rg exit code greater than 1 as scanner error/fail."
Assert-True ($source -match 'PowerShell Select-String fallback') "Helper does not provide a fallback when rg.exe is unavailable."
Assert-True ($source -match 'function Get-LatestPreRev866Backup') "Helper does not support safe resume with an existing pre-REV866 backup."
Assert-True ($source -match 'REV866 migration is already applied\. Resuming verification') "Helper does not resume cleanly when REV866 was already applied."
Assert-True ($source -notmatch '\brg -n \$secretPattern \.') "Helper still contains a direct rg call instead of resolved scanner execution."
Write-Host "REV866 helper static verification passed."