[CmdletBinding()]
param(
 [string]$OutputRoot='C:\SESS-Deploy',
 [string]$FrontendRef,
 [Parameter(Mandatory=$true)][string]$MigrationProof,
 [switch]$Candidate,
 # 26 September: a package whose frontend signs in through the production OIDC login.
 [switch]$ProductionLogin,
 # Build 2 (the DC screen): the MANIFEST of build 1, whose backend must be reproduced exactly.
 [string]$SameBackendAs
)
$ErrorActionPreference='Stop'
function CheckExit([string]$step) { if ($LASTEXITCODE -ne 0) { throw "$step failed: $LASTEXITCODE" } }
if ($Candidate -eq $ProductionLogin) { throw 'Choose exactly one of -Candidate or -ProductionLogin.' }
if ($Candidate) {
 if ($SameBackendAs) { throw '-SameBackendAs applies only to -ProductionLogin.' }
 if (-not $FrontendRef) { $FrontendRef='0c59254f58bd49fc13a8b919387ba0cf5a1d9988' }
} elseif ($FrontendRef -cnotmatch '\A[0-9a-f]{40}\z') {
 throw '-ProductionLogin needs -FrontendRef as the full 40-character lowercase SHA the frontend developer witnessed; never a branch, tag or short SHA.'
}
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Set-Location -LiteralPath $repo
$gitRoot=(& git rev-parse --show-toplevel).Trim()
$head=(& git rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $head -notmatch '\A[0-9a-f]{40}\z') { throw 'Cannot identify source HEAD.' }
$frontendSha=(& git rev-parse "$FrontendRef^{commit}").Trim()
if ($LASTEXITCODE -ne 0 -or $frontendSha -notmatch '\A[0-9a-f]{40}\z') { throw 'Cannot identify frontend source commit.' }
if ($ProductionLogin) {
 if ($frontendSha -cne $FrontendRef) { throw 'The frontend SHA must name a commit directly.' }
 # Only a pushed commit can be the one the developer witnessed; a local-only SHA is refused.
 & git -C $gitRoot fetch --quiet origin feature/frontend; CheckExit 'Fetching origin/feature/frontend'
 & git -C $gitRoot merge-base --is-ancestor $frontendSha refs/remotes/origin/feature/frontend
 if ($LASTEXITCODE -ne 0) { throw "Frontend commit $frontendSha is not on origin/feature/frontend. Push it and name the pushed SHA." }
}
$dirty=& git status --porcelain -- src tests tools/deployment tools/setup tools/identity
if ($dirty) { throw 'Commit source, tests and deployment tools before packaging.' }
$proof=Get-Content -LiteralPath $MigrationProof -Raw | ConvertFrom-Json
if ($proof.sdk_list -ne '' -or $proof.databases.Count -ne 2 -or @($proof.databases | Where-Object { -not $_.verified -or -not $_.reconciled -or -not $_.replay_history_and_audits_unchanged }).Count) { throw 'Migration proof is incomplete.' }
# The proof must cover exactly the migrations in this tree: a migration added after proving is refused.
$migrationDir='src/SESS.NexaERP.Infrastructure/Persistence/Migrations'
$treeMigrations=@(Get-ChildItem -LiteralPath (Join-Path $repo $migrationDir) -Filter '*.cs' -File | ForEach-Object { "$migrationDir/$($_.Name)" })
if (-not $proof.migration_sources -or (Compare-Object $treeMigrations @($proof.migration_sources.PSObject.Properties.Name))) { throw 'Migration sources in this tree differ from the proof: re-run prove_migrations.py for this HEAD.' }
$baseline=$null
if ($SameBackendAs) {
 $baselinePath=[IO.Path]::GetFullPath($SameBackendAs)
 $sidecar=[IO.Path]::GetDirectoryName($baselinePath)+'.MANIFEST.sha256'
 if (-not (Test-Path -LiteralPath $sidecar)) { throw "Build 1 digest not found: $sidecar" }
 if ((Get-FileHash -LiteralPath $baselinePath -Algorithm SHA256).Hash -ne ((Get-Content -LiteralPath $sidecar -Raw).Trim() -split '\s+')[0]) { throw 'Build 1 MANIFEST differs from its separately recorded digest.' }
 $baseline=Get-Content -LiteralPath $baselinePath -Raw | ConvertFrom-Json
 if ($baseline.Readiness -ne 'LOGIN_CANDIDATE_FIELD_WITNESS_PENDING') { throw 'Build 1 is not a production-login package.' }
 # During the backend freeze only documentation may land between build 1 and build 2.
 $changed=@(& git -C $repo diff --name-only $baseline.HeadSha $head -- . ':(exclude)docs'); CheckExit 'Comparing with build 1'
 if ($changed.Count) { throw "Backend changed since build 1 ($($baseline.HeadSha)): $($changed -join ', ')" }
}
$OutputRoot=[IO.Path]::GetFullPath($OutputRoot).TrimEnd('\')
New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
# A production-login package is named by backend and frontend, so a frontend-only rebuild has its own folder.
$packageName=if ($ProductionLogin) { "$head-web-$($frontendSha.Substring(0,12))" } else { $head }
$target=Join-Path $OutputRoot $packageName
if (Test-Path -LiteralPath $target) { throw 'Package destination already exists; do not overwrite a witnessed package.' }
$stage=Join-Path $OutputRoot ('.staging-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stage | Out-Null
foreach ($folder in @('api','web','installer','migrate','installer/tools','installer/docs')) { New-Item -ItemType Directory -Path (Join-Path $stage $folder) -Force | Out-Null }
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
Remove-Item -LiteralPath $frontendArchive
if ($ProductionLogin) {
 if (@(Get-ChildItem -LiteralPath $frontendWork -Recurse -File -Force -Filter '.env*' | Where-Object { $_.Name -like '*.local' }).Count) { throw 'The frontend export carries a .env*.local file.' }
 $package=Get-Content -LiteralPath (Join-Path $frontendWork 'package.json') -Raw | ConvertFrom-Json
 if (-not $package.dependencies.'oidc-client-ts') { throw 'The frontend does not depend on oidc-client-ts: not the production OIDC login.' }
}
Push-Location $frontendWork
try {
 & npm.cmd ci; CheckExit 'npm ci'
 & npm.cmd run build; CheckExit 'Frontend production build'
 if ($ProductionLogin) {
  # The development login must be absent from both the source and the build. Redirect URIs are built
  # at runtime from the ERP origin (8443) plus these paths; the authorities are compiled in.
  $scanned=@(Get-ChildItem -LiteralPath $frontendWork -Recurse -File -Force | Where-Object {
   $_.FullName -notmatch '\\node_modules\\' -and ($_.Name -like '.env*' -or $_.Extension -in '.ts','.tsx','.js','.jsx','.mjs','.cjs','.json','.html','.css','.map') })
  $residue=@($scanned | Select-String -SimpleMatch -Pattern '/api/v1/dev','nexaerp.dev.','DevelopmentToken' -List)
  if ($residue.Count) { throw "Development-login residue in the frontend: $(($residue | ForEach-Object { $_.Path.Substring($frontendWork.Length+1)+': '+$_.Pattern }) -join '; ')" }
  $built=(Get-ChildItem -LiteralPath 'dist' -Recurse -File -Include '*.js','*.html' | ForEach-Object { [IO.File]::ReadAllText($_.FullName) }) -join "`n"
  foreach ($needle in '/oidc/callback','/oidc/logout-callback','https://192.168.68.130:8444/realms/staff','https://192.168.68.130:8444/realms/approvers') {
   if (-not $built.Contains($needle)) { throw "The frontend build lacks $needle." }
  }
 }
 Copy-Item -Path 'dist/*' -Destination (Join-Path $stage 'web') -Recurse
} finally { Pop-Location }
$bundle=Join-Path (Split-Path -Parent ([IO.Path]::GetFullPath($MigrationProof))) 'efbundle.exe'
if ((Get-FileHash -LiteralPath $bundle -Algorithm SHA256).Hash -ne $proof.bundle_sha256) { throw 'Bundle differs from the SDK-free witnessed artifact.' }
foreach ($property in $proof.migration_sources.PSObject.Properties) {
 if ((Get-FileHash -LiteralPath (Join-Path $repo $property.Name) -Algorithm SHA256).Hash -ne $property.Value) { throw "Migration source changed after proof: $($property.Name)" }
}
Copy-Item -LiteralPath $bundle -Destination (Join-Path $stage 'migrate/efbundle.exe')
Copy-Item -LiteralPath $MigrationProof -Destination (Join-Path $stage 'migrate/proof.json')
foreach ($file in @('Invoke-VerifiedDatabaseBackup.ps1','Register-VerifiedDatabaseBackup.ps1','Test-ProductionState.ps1','Test-DailyBackupState.ps1')) { Copy-Item -LiteralPath (Join-Path $repo "tools/$file") -Destination (Join-Path $stage 'installer/tools') }
foreach ($file in @('Verify-Package.ps1','VerifiedBackupTransfer.psm1','Invoke-ServerDailyBackup.ps1','Archive-IncomingBackups.ps1')) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot $file) -Destination (Join-Path $stage 'installer/tools') }
New-Item -ItemType Directory -Path (Join-Path $stage 'installer/tools/setup') -Force | Out-Null
foreach ($file in @('Invoke-Setup.ps1','SetupOperator.psm1')) { Copy-Item -LiteralPath (Join-Path $repo "tools/setup/$file") -Destination (Join-Path $stage 'installer/tools/setup') }
if ($ProductionLogin) {
 # Decided 26 September: only the import test travels. Repair-ApproverOtpFlow.ps1 is never packaged, and
 # Step4-Verify-RealmFlows.ps1 (the server agent's) is the one live verifier.
 New-Item -ItemType Directory -Path (Join-Path $stage 'installer/tools/identity') -Force | Out-Null
 Copy-Item -LiteralPath (Join-Path $repo 'tools/identity/Test-KeycloakRealmImport.ps1') -Destination (Join-Path $stage 'installer/tools/identity')
}
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
$readiness='CANDIDATE_BLOCKED_PRODUCTION_FRONTEND_OIDC_AND_FIELD_WITNESS'
if ($ProductionLogin) {
 $readiness='LOGIN_CANDIDATE_FIELD_WITNESS_PENDING'
 # Replace the candidate wording exactly; a drifted template is refused rather than half-edited.
 $readme=$readme.Replace("`r`n","`n").Replace("`n","`r`n")
 $replacements=[ordered]@{
  'SESS NexaERP - DESKTOP-SPF5420 deployment candidate'='SESS NexaERP - DESKTOP-SPF5420 production-login package'
  "READINESS: CANDIDATE, NOT CLEARED FOR FRONTEND DEMO OR GO-LIVE.`r`nThe initial handoff permits checked server preparation, DEMO API commissioning`r`nand SESS-12 bootstrap only, then STOP and report. Named-employee wrapper rehearsal`r`ncomes later. The selected frontend still signs in through Debug-only /api/v1/dev`r`nendpoints; Release excludes these. Do not serve web/ until production OIDC login`r`nis integrated and witnessed. Never enable development authentication in Release."="READINESS: LOGIN_CANDIDATE_FIELD_WITNESS_PENDING.`r`nThe frontend is the production OIDC login (authorization code with PKCE against`r`nKeycloak on 8444), built from a commit on origin/feature/frontend. The builder refused`r`nany development-login residue. Serve web/ only after the frontend developer has`r`nwitnessed sign-in against THIS server in both realms. Never enable development`r`nauthentication in Release. installer/tools/identity/ holds only the Keycloak import`r`ntest; the server agent's Step4-Verify-RealmFlows.ps1 is the one live verifier."
  'and health. Do not serve the development-login frontend.'='and health. Serve web/ only after the login witness above.'
 }
 foreach ($pair in $replacements.GetEnumerator()) {
  if (-not $readme.Contains($pair.Key)) { throw "README template changed; production-login wording not applied: $($pair.Key.Split("`r")[0])" }
  $readme=$readme.Replace($pair.Key,$pair.Value)
 }
}
$readme=$readme.Replace('{{HEAD}}',$head).Replace('{{FRONTEND}}',$frontendSha)
$readme+="`r`n`r`nSERVER RUNBOOK (packaged copy):`r`n"+(Get-Content docs/installation/server-deployment.md -Raw)
[IO.File]::WriteAllText((Join-Path $stage 'README.txt'),$readme,[Text.UTF8Encoding]::new($false))
$files=@(Get-ChildItem -LiteralPath $stage -File -Recurse | Sort-Object FullName | ForEach-Object {
 [ordered]@{Path=$_.FullName.Substring($stage.Length+1).Replace('\','/');Length=$_.Length;Sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}
})
if ($baseline) {
 # Everything but the web, the documents and the README must be byte-identical to build 1.
 $backend={ param($p) $p -notlike 'web/*' -and $p -notlike 'installer/docs/*' -and $p -ne 'README.txt' }
 $mine=@($files | Where-Object { & $backend $_.Path } | ForEach-Object { "$($_.Path) $($_.Sha256)" })
 $theirs=@($baseline.Files | Where-Object { & $backend $_.Path } | ForEach-Object { "$($_.Path) $($_.Sha256)" })
 $differences=@(Compare-Object $theirs $mine)
 if ($differences.Count -or $baseline.MigrationHead -ne $proof.databases[0].head) { throw "Backend differs from build 1: $(($differences | Select-Object -First 5 | ForEach-Object { $_.InputObject }) -join '; ')" }
}
$manifest=[ordered]@{HeadSha=$head;FrontendSha=$frontendSha;BuiltAtUtc=[DateTime]::UtcNow.ToString('o');Readiness=$readiness;MigrationHead=$proof.databases[0].head;Files=$files}
if ($ProductionLogin) { $manifest.PackageName=$packageName; if ($baseline) { $manifest.SameBackendAs=$baseline.PackageName } }
[IO.File]::WriteAllText((Join-Path $stage 'MANIFEST'),($manifest | ConvertTo-Json -Depth 6),[Text.UTF8Encoding]::new($false))
$digest=(Get-FileHash -LiteralPath (Join-Path $stage 'MANIFEST') -Algorithm SHA256).Hash.ToLowerInvariant()
& (Join-Path $PSScriptRoot 'Verify-Package.ps1') -Root $stage -ExpectedHead $head -ExpectedManifestSha256 $digest
# Only move the new staging directory, after checking both absolute paths.
if (-not $stage.StartsWith($OutputRoot+'\',[StringComparison]::OrdinalIgnoreCase) -or -not $target.StartsWith($OutputRoot+'\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Destination escaped output root.' }
Move-Item -LiteralPath $stage -Destination $target
[IO.File]::WriteAllText((Join-Path $OutputRoot ($packageName+'.MANIFEST.sha256')),$digest+"  MANIFEST`r`n")
Write-Output "PACKAGE: $target"
Write-Output "MANIFEST SHA256: $digest"
