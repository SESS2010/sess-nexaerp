[CmdletBinding()]
param([Parameter(Mandatory)][ValidateSet('start','stop')][string]$Action,
      [Parameter(Mandatory)][ValidateRange(1025,65535)][int]$DatabasePort)
$ErrorActionPreference='Stop'
if($DatabasePort -in @(5432,18444)){throw 'Only a dedicated disposable database port is allowed.'}
$name='sess-keycloak-recovery-witness'
$label='sess.nexaerp.disposable-keycloak-recovery'
$existing=& docker ps -aq --filter "name=^/$name`$"
if($LASTEXITCODE -ne 0){throw 'Docker is unavailable.'}
if($existing){
    $description=& docker inspect $name | ConvertFrom-Json
    if($LASTEXITCODE -ne 0 -or @($description).Count -ne 1 -or $description[0].Config.Labels.'sess.nexaerp.disposable-keycloak-recovery' -ne 'true'){throw 'The existing container is not this disposable witness.'}
    & docker rm -f $name | Out-Null
    if($LASTEXITCODE -ne 0){throw 'Cannot stop the owned witness container.'}
}
if($Action -eq 'stop'){exit 0}
$cert=(Resolve-Path -LiteralPath $env:SESS_KEYCLOAK_WITNESS_CERTIFICATE).Path
$key=(Resolve-Path -LiteralPath $env:SESS_KEYCLOAK_WITNESS_PRIVATE_KEY).Path
$fixtures=Join-Path (Split-Path $PSScriptRoot -Parent) 'tests/SESS.NexaERP.Tests/Fixtures/Keycloak'
& docker run -d --name $name --label "$label=true" -p 127.0.0.1:18443:8443 `
    --mount "type=bind,source=$fixtures,target=/opt/keycloak/data/import,readonly" `
    --mount "type=bind,source=$cert,target=/witness/tls.crt,readonly" `
    --mount "type=bind,source=$key,target=/witness/tls.key,readonly" `
    quay.io/keycloak/keycloak:26.7.3 start --db=postgres --db-url-host=host.docker.internal `
    "--db-url-port=$DatabasePort" --db-url-database=keycloak_witness --db-username=keycloak_fixture `
    --db-password=Keycloak-Database-Witness-Only!26 --https-certificate-file=/witness/tls.crt `
    --https-certificate-key-file=/witness/tls.key --hostname=https://127.0.0.1:18443 `
    --http-enabled=false --health-enabled=true --metrics-enabled=true --import-realm
if($LASTEXITCODE -ne 0){throw 'Cannot start the disposable recovery container.'}