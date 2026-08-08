[CmdletBinding()]
param(
    [string]$RestoreDatabase,
    [string]$ExpectedRestoreDatabase,
    [switch]$GeneratePlanOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$targetRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$reportDir = Join-Path $targetRoot "outputs"
$reportFile = Join-Path $reportDir "rev868_isolated_restore_verification_plan.md"

function Assert-SafeRestoreName([string]$Name, [string]$Label) {
    if ([string]::IsNullOrWhiteSpace($Name)) { throw "$Label is required." }
    if ($Name -notmatch '^sess_nexaerp_rev868_restore_verify_[A-Za-z0-9_]+$') { throw "$Label must be an explicit isolated REV868 restore verification database name." }
    if ($Name -in @('sess_nexaerp','postgres','template0','template1')) { throw "$Label targets a protected database." }
}

Assert-SafeRestoreName $RestoreDatabase "RestoreDatabase"
Assert-SafeRestoreName $ExpectedRestoreDatabase "ExpectedRestoreDatabase"
if ($RestoreDatabase -ne $ExpectedRestoreDatabase) { throw "Restore database mismatch. Refusing to continue." }

$plan = @"
# REV868 Isolated Restore Verification Plan

Target isolated database: `$RestoreDatabase`

This helper/plan is intentionally non-executing unless future management explicitly authorizes database creation/restoration. It must never target `sess_nexaerp`, `postgres`, `template0`, `template1` or live REV861.

Required future steps after management approval:

1. Management creates or authorizes creation of the isolated database named `$RestoreDatabase`.
2. Restore only the clearly named post-REV868 safety baseline backup into `$RestoreDatabase`.
3. Verify current_database() equals `$RestoreDatabase`.
4. Compare migration rows against `sess_nexaerp` expected REV868 chain.
5. Verify REV868 table, index, FK and check-constraint names.
6. Compare non-sensitive table counts for framework/audit/history tables only.
7. Leave the isolated database in place until management approves removal.

No create/drop/restore operation is executed by this source plan.
"@

if ($GeneratePlanOnly) { $plan; return }
New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
Set-Content -LiteralPath $reportFile -Value $plan -Encoding utf8
Write-Host "REV868 isolated restore verification plan: $reportFile"
