[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$CertificatePath,
    [Parameter(Mandatory)][string]$PrivateKeyPath,
    [ValidateSet('Debug','Release')][string]$Configuration = 'Release',
    [ValidateRange(1,120)][int]$StartupTimeoutMinutes = 40
)
$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
$fixtures = Join-Path $repository 'tests\SESS.NexaERP.Tests\Fixtures\Keycloak'
$certificate = (Resolve-Path -LiteralPath $CertificatePath).Path
$privateKey = (Resolve-Path -LiteralPath $PrivateKeyPath).Path
Get-Command docker -ErrorAction Stop | Out-Null
Get-Command dotnet -ErrorAction Stop | Out-Null
# Supply a disposable PEM certificate with IP SAN 127.0.0.1.
# The test pins this certificate; production validation is not weakened.
$pem = [IO.File]::ReadAllText($certificate)
$base64 = $pem.Replace('-----BEGIN CERTIFICATE-----','').Replace('-----END CERTIFICATE-----','').Trim()
$sha = [Security.Cryptography.SHA256]::Create()
try { $pin = -join ($sha.ComputeHash([Convert]::FromBase64String($base64)) | ForEach-Object { $_.ToString('X2') }) }
finally { $sha.Dispose() }
$name = 'sess-keycloak-witness-' + [Guid]::NewGuid().ToString('N').Substring(0,12)
$oldUrl = $env:SESS_KEYCLOAK_WITNESS_URL
$oldPin = $env:SESS_KEYCLOAK_CERT_SHA256
Push-Location $repository
try {
    & dotnet build SESS.NexaERP.slnx -c $Configuration -p:KeycloakWitness=true --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Build failed; witness did not run.' }
    & docker run -d --name $name -p 127.0.0.1:18443:8443 --mount "type=bind,source=$fixtures,target=/opt/keycloak/data/import,readonly" --mount "type=bind,source=$certificate,target=/witness/tls.crt,readonly" --mount "type=bind,source=$privateKey,target=/witness/tls.key,readonly" quay.io/keycloak/keycloak:26.7.3 start-dev --https-certificate-file=/witness/tls.crt --https-certificate-key-file=/witness/tls.key --hostname=https://127.0.0.1:18443 --http-enabled=false --import-realm
    if ($LASTEXITCODE -ne 0) { throw 'Local Keycloak container could not start.' }
    $ready = $false
    for ($attempt = 0; $attempt -lt ($StartupTimeoutMinutes * 12); $attempt++) {
        $logs = & docker logs $name 2>&1
        if (($logs -join [Environment]::NewLine) -match 'Listening on: .*https://') { $ready = $true; break }
        $running = & docker inspect --format '{{.State.Running}}' $name
        if ($running -ne 'true') { throw ('Keycloak exited: ' + ($logs -join [Environment]::NewLine)) }
        Start-Sleep -Seconds 5
    }
    if (-not $ready) { throw 'Keycloak did not become ready within the configured startup timeout.' }
    & docker inspect --format '{{.Image}}' $name
    $env:SESS_KEYCLOAK_WITNESS_URL = 'https://127.0.0.1:18443'
    $env:SESS_KEYCLOAK_CERT_SHA256 = $pin
    & dotnet test tests/SESS.NexaERP.Tests/SESS.NexaERP.Tests.csproj -c $Configuration --no-build --filter 'Witness=KeycloakContainer' --logger "trx;LogFileName=keycloak-$Configuration.trx" --results-directory local-evidence/item16
    if ($LASTEXITCODE -ne 0) { throw 'Real Keycloak authentication witness failed.' }
}
finally {
    & docker logs --tail 80 $name
    & docker rm -f $name
    $env:SESS_KEYCLOAK_WITNESS_URL = $oldUrl
    $env:SESS_KEYCLOAK_CERT_SHA256 = $oldPin
    Pop-Location
}
