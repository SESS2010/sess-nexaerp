[CmdletBinding()]
param(
 [string]$OutputRoot='C:\SESS-Deploy',
 [string]$FrontendRef='0c59254f58bd49fc13a8b919387ba0cf5a1d9988',
 [Parameter(Mandatory=$true)][string]$MigrationProof,
 [switch]$Candidate
)
$ErrorActionPreference='Stop'
if (-not $Candidate) { throw 'The checked-in frontend uses Debug authentication. Only a clearly marked candidate package can currently be built.' }
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Set-Location -LiteralPath $repo
$gitRoot=(& git rev-parse --show-toplevel).Trim()
$head=(& git rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $head -notmatch '\A[0-9a-f]{40}\z') { throw 'Cannot identify source HEAD.' }
$frontendSha=(& git rev-parse "$FrontendRef^{commit}").Trim()
if ($LASTEXITCODE -ne 0 -or $frontendSha -notmatch '\A[0-9a-f]{40}\z') { throw 'Cannot identify frontend source commit.' }
$dirty=& git status --porcelain -- src tests tools/deployment
if ($dirty) { throw 'Commit source, tests and deployment tools before packaging.' }
$proof=Get-Content -LiteralPath $MigrationProof -Raw | ConvertFrom-Json
if ($proof.sdk_list -ne '' -or $proof.databases.Count -ne 2 -or @($proof.databases | Where-Object { -not $_.verified -or -not $_.reconciled -or -not $_.replay_history_and_audits_unchanged }).Count) { throw 'Migration proof is incomplete.' }
$OutputRoot=[IO.Path]::GetFullPath($OutputRoot).TrimEnd('\')
New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
$target=Join-Path $OutputRoot $head
if (Test-Path -LiteralPath $target) { throw 'Package destination already exists; do not overwrite a witnessed package.' }
$stage=Join-Path $OutputRoot ('.staging-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stage | Out-Null
foreach ($folder in @('api','web','installer','migrate','installer/tools','installer/docs')) { New-Item -ItemType Directory -Path (Join-Path $stage $folder) -Force | Out-Null }
function CheckExit([string]$step) { if ($LASTEXITCODE -ne 0) { throw "$step failed: $LASTEXITCODE" } }
& dotnet publish src/SESS.NexaERP.Api -c Release --no-restore --self-contained false -p:UseSharedCompilation=false -o (Join-Path $stage 'api')
CheckExit 'API publish'
& dotnet publish src/SESS.NexaERP.Installer -c Release --no-restore --self-contained false -p:UseSharedCompilation=false -o (Join-Path $stage 'installer')
CheckExit 'Installer publish'
if ((Get-FileHash -LiteralPath (Join-Path $stage 'installer/SESS.NexaERP.Installer.dll') -Algorithm SHA256).Hash -ne $proof.installer_assembly_sha256) { throw 'Published Installer differs from the SDK-free witnessed assembly.' }
# Build the frontend developer's exact commit in an isolated directory.
$frontendWork=Join-Path $repo ('local-evidence/server-package/frontend-build-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $frontendWork -Force | Out-Null
$frontendArchive=Join-Path $frontendWork 'source.zip'
& git -C $gitRoot archive --format=zip "--output=$frontendArchive" "${frontendSha}:target-dotnet/src/SESS.NexaERP.Web"
CheckExit 'Frontend source export'
Expand-Archive -LiteralPath $frontendArchive -DestinationPath $frontendWork
Push-Location $frontendWork
try {
 & npm.cmd ci; CheckExit 'npm ci'
 & npm.cmd run build; CheckExit 'Frontend production build'
 Copy-Item -Path 'dist/*' -Destination (Join-Path $stage 'web') -Recurse
} finally { Pop-Location }
$bundle=Join-Path (Split-Path -Parent ([IO.Path]::GetFullPath($MigrationProof))) 'efbundle.exe'
if ((Get-FileHash -LiteralPath $bundle -Algorithm SHA256).Hash -ne $proof.bundle_sha256) { throw 'Bundle differs from the SDK-free witnessed artifact.' }
foreach ($property in $proof.migration_sources.PSObject.Properties) {
 if ((Get-FileHash -LiteralPath (Join-Path $repo $property.Name) -Algorithm SHA256).Hash -ne $property.Value) { throw "Migration source changed after proof: $($property.Name)" }
}
if (-not $proof.migration_sources) { throw 'Proof lacks migration source hashes.' }
Copy-Item -LiteralPath $bundle -Destination (Join-Path $stage 'migrate/efbundle.exe')
Copy-Item -LiteralPath $MigrationProof -Destination (Join-Path $stage 'migrate/proof.json')
foreach ($file in @('Invoke-VerifiedDatabaseBackup.ps1','Register-VerifiedDatabaseBackup.ps1')) { Copy-Item -LiteralPath (Join-Path $repo "tools/$file") -Destination (Join-Path $stage 'installer/tools') }
foreach ($file in @('Verify-Package.ps1','VerifiedBackupTransfer.psm1','Invoke-ServerDailyBackup.ps1','Archive-IncomingBackups.ps1')) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot $file) -Destination (Join-Path $stage 'installer/tools') }
# Export committed documents/business scripts, never unrelated local working edits.
foreach ($export in @(@{Tree='docs/installation';Destination='installer/docs'},@{Tree='database/postgresql';Destination='installer/database/postgresql'})) {
 $archive=Join-Path $stage ('archive-'+[Guid]::NewGuid().ToString('N')+'.zip')
 & git -C $gitRoot archive --format=zip "--output=$archive" "${head}:target-dotnet/$($export.Tree)"
 CheckExit 'Committed documentation export'
 Expand-Archive -LiteralPath $archive -DestinationPath (Join-Path $stage $export.Destination) -Force
 Remove-Item -LiteralPath $archive
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'service-settings.example.json') -Destination (Join-Path $stage 'installer')
$readme=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'README.template.txt') -Raw
$readme=$readme.Replace('{{HEAD}}',$head).Replace('{{FRONTEND}}',$frontendSha)
$readme+="`r`n`r`nSERVER RUNBOOK (packaged copy):`r`n"+(Get-Content docs/installation/server-deployment.md -Raw)
[IO.File]::WriteAllText((Join-Path $stage 'README.txt'),$readme,[Text.UTF8Encoding]::new($false))
$files=@(Get-ChildItem -LiteralPath $stage -File -Recurse | Sort-Object FullName | ForEach-Object {
 [ordered]@{Path=$_.FullName.Substring($stage.Length+1).Replace('\','/');Length=$_.Length;Sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}
})
$manifest=[ordered]@{HeadSha=$head;FrontendSha=$frontendSha;BuiltAtUtc=[DateTime]::UtcNow.ToString('o');Readiness='CANDIDATE_BLOCKED_PRODUCTION_FRONTEND_OIDC_AND_FIELD_WITNESS';MigrationHead=$proof.databases[0].head;Files=$files}
[IO.File]::WriteAllText((Join-Path $stage 'MANIFEST'),($manifest | ConvertTo-Json -Depth 6),[Text.UTF8Encoding]::new($false))
$digest=(Get-FileHash -LiteralPath (Join-Path $stage 'MANIFEST') -Algorithm SHA256).Hash.ToLowerInvariant()
& (Join-Path $PSScriptRoot 'Verify-Package.ps1') -Root $stage -ExpectedHead $head -ExpectedManifestSha256 $digest
# Only move the new staging directory, after checking both absolute paths.
if (-not $stage.StartsWith($OutputRoot+'\',[StringComparison]::OrdinalIgnoreCase) -or -not $target.StartsWith($OutputRoot+'\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Destination escaped output root.' }
Move-Item -LiteralPath $stage -Destination $target
[IO.File]::WriteAllText((Join-Path $OutputRoot ($head+'.MANIFEST.sha256')),$digest+"  MANIFEST`r`n")
Write-Output "PACKAGE: $target"
Write-Output "MANIFEST SHA256: $digest"
