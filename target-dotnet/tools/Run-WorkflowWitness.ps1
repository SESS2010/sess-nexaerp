# Nightly run of the witness-gated test suites (decision of the Technical Director, 20 Sep 2026).
# The routine suite compiles without these gates because each gated test re-runs a complete
# purchase flow; nothing ran them until now. Schedule this script nightly on the build machine:
#
#   schtasks /Create /SC DAILY /ST 01:00 /TN "NexaERP nightly witnesses" /TR "powershell.exe -NoProfile -ExecutionPolicy Bypass -File C:\path\to\target-dotnet\tools\Run-WorkflowWitness.ps1"
#
# Each gate builds its own copy of the test project (the gate is a compile-time constant) and
# writes a TRX under local-evidence/nightly/<date>/<gate>.trx. Exit code is non-zero if any gate
# fails, so the scheduled task history shows red. Disposable PostgreSQL only; never the field database.
param(
    [string[]] $Gates = @('WorkflowWitness', 'ConcurrencyWitness', 'MigrationLifecycleWitness'),
    [string] $Configuration = 'Release'
)
$ErrorActionPreference = 'Continue'
$root = Split-Path -Parent $PSScriptRoot
$stamp = Get-Date -Format 'yyyy-MM-dd'
$evidence = Join-Path $root "local-evidence\nightly\$stamp"
New-Item -ItemType Directory -Force $evidence | Out-Null
$log = Join-Path $evidence 'nightly.log'
"start $(Get-Date -Format o) head $(git -C $root rev-parse HEAD)" | Out-File $log -Encoding utf8
$failed = 0
foreach ($gate in $Gates) {
    $project = Join-Path $root 'tests\SESS.NexaERP.Tests\SESS.NexaERP.Tests.csproj'
    $output = Join-Path $evidence "bin-$gate"
    "gate $gate build $(Get-Date -Format o)" | Add-Content $log
    dotnet build $project -c $Configuration -nologo -v q "-p:$gate=true" -o $output 2>&1 | Tee-Object -FilePath (Join-Path $evidence "$gate-build.log") | Out-Null
    if ($LASTEXITCODE -ne 0) { "gate $gate build failed" | Add-Content $log; $failed++; continue }
    "gate $gate tests $(Get-Date -Format o)" | Add-Content $log
    dotnet test (Join-Path $output 'SESS.NexaERP.Tests.dll') --nologo --logger "trx;LogFileName=$gate.trx" --results-directory $evidence 2>&1 | Tee-Object -FilePath (Join-Path $evidence "$gate-tests.log") | Out-Null
    "gate $gate exit $LASTEXITCODE $(Get-Date -Format o)" | Add-Content $log
    if ($LASTEXITCODE -ne 0) { $failed++ }
}
"done failed-gates=$failed $(Get-Date -Format o)" | Add-Content $log
exit $failed
