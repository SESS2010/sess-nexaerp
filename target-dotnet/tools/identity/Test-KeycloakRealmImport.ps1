<#
.SYNOPSIS
Settles whether Keycloak 26.7.4's importer drops the OTP step from nexaerp-approver-browser, or
whether it was removed on the server by hand or by another script.

.DESCRIPTION
On a THROWAWAY Keycloak on this laptop, never the server:
  1. checks keycloak-26.7.4.zip against the SHA-256 in docs/installation/keycloak/server-prerequisites.json;
  2. takes the two realm files from a git ref (default origin/main) and records their hashes;
  3. extracts Keycloak into a new temporary folder and gives it its own disposable PostgreSQL cluster,
     because the server's Keycloak stores on PostgreSQL too (use -Database dev-file to skip that);
  4. runs the same build and the same import as the server procedure:
         kc.bat build --db=postgres --health-enabled=true
         kc.bat import --dir <folder> --override=false
  5. starts that Keycloak and reads the approvers realm's browser flow back from the LIVE admin API,
     then stops it, starts it again, and reads it back a second time;
  6. prints PASS or FAIL separately for "after import" and "after restart", records kc.bat --version,
     java -version, the zip hash, both realm file hashes and the import log, then deletes the
     throwaway Keycloak and cluster.

PASS means the live flow holds exactly the Username Password Form and the OTP Form, both REQUIRED,
in that order, and the realm is bound to it.

If "after import" FAILS, the importer is at fault and the go-live import on 30 September would
repeat it: the realm must then be repaired and read back after every import.
If both PASS, the importer is not at fault; the server's flow lost its step some other way.

It refuses to start while the nightly witness task or another disposable cluster is running.

Written 24 September 2026 for finding #33 and NOT run by its author: its first run is the witness.
The server imported the realm files as they were at f910f2f. The OTP flow is identical at that
commit and at origin/main; pass -RealmRef f910f2f to use exactly the server's files.

.EXAMPLE
.\tools\identity\Test-KeycloakRealmImport.ps1 -KeycloakZip D:\installers\keycloak-26.7.4.zip -JavaHome 'C:\Program Files\Eclipse Adoptium\jdk-17.0.12.7-hotspot'
#>
param(
    [Parameter(Mandatory = $true)][string]$KeycloakZip,
    # Use the same Java major version the server's Keycloak runs on.
    [Parameter(Mandatory = $true)][string]$JavaHome,
    [string]$RealmRef = 'origin/main',
    [ValidateSet('postgres', 'dev-file')][string]$Database = 'postgres',
    [string]$PostgreSqlBin = 'C:\Program Files\PostgreSQL\17\bin',
    [string]$EvidenceDirectory,
    [switch]$KeepWorkFolder
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
if (-not $EvidenceDirectory) { $EvidenceDirectory = Join-Path $repo "local-evidence\keycloak-import-test\$stamp" }
New-Item -ItemType Directory -Force $EvidenceDirectory | Out-Null
$work = Join-Path ([IO.Path]::GetTempPath()) "kc-import-test-$stamp"
$receipt = [ordered]@{ Computer = $env:COMPUTERNAME; Started = (Get-Date -Format o); RealmRef = $RealmRef; Database = $Database; WorkFolder = $work }
function Note([string]$Text) { "$(Get-Date -Format o) $Text" | Add-Content (Join-Path $EvidenceDirectory 'progress.log'); Write-Output $Text }
function Get-FreePort { $l = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0); $l.Start(); $p = $l.LocalEndpoint.Port; $l.Stop(); return $p }
function Invoke-Logged([string]$File, [string]$Arguments, [string]$LogName) {
    $out = Join-Path $EvidenceDirectory "$LogName.out.log"; $err = Join-Path $EvidenceDirectory "$LogName.err.log"
    $p = Start-Process -FilePath $File -ArgumentList $Arguments -Wait -PassThru -NoNewWindow -RedirectStandardOutput $out -RedirectStandardError $err
    return $p.ExitCode
}

# --- Guards: never beside another harness, never against the server. ---
$task = Get-ScheduledTask -TaskName 'NexaERP nightly witnesses' -ErrorAction SilentlyContinue
if ($task -and $task.State -eq 'Running') { throw 'The nightly witness task is running. Run this when it has finished.' }
$livePg = @(Get-CimInstance Win32_Process -Filter "Name='postgres.exe'" | Select-Object -ExpandProperty ProcessId)
foreach ($dir in (Get-ChildItem ([IO.Path]::GetTempPath()) -Directory -Filter 'advance-postgresql-parser-*' -ErrorAction SilentlyContinue)) {
    $pidFile = Join-Path $dir.FullName 'data\postmaster.pid'
    if ((Test-Path $pidFile) -and ($livePg -contains [int]((Get-Content $pidFile -TotalCount 1).Trim()))) {
        throw "A disposable test cluster is running ($($dir.Name)). One cluster at a time: run this when it has finished."
    }
}

# --- 1. Keycloak zip hash. ---
$expectedZip = ((Get-Content (Join-Path $repo 'docs\installation\keycloak\server-prerequisites.json') -Raw | ConvertFrom-Json).PSObject.Properties.Value |
    Where-Object { $_.Url -like '*keycloak-26.7.4.zip' } | Select-Object -First 1).Sha256
if (-not $expectedZip) { $expectedZip = 'a286e98b4296d4e75ee88d8527c7cd463b307caa022f088c9f22cffccc741fa1' }
$zipHash = (Get-FileHash -LiteralPath $KeycloakZip -Algorithm SHA256).Hash.ToLowerInvariant()
$receipt.KeycloakZipSha256 = $zipHash
if ($zipHash -ne $expectedZip.ToLowerInvariant()) { throw "keycloak-26.7.4.zip hash $zipHash does not match the recorded $expectedZip. Stopping." }
Note "zip hash matches $zipHash"

# --- 2. The realm files from git, exactly as committed. ---
$import = Join-Path $work 'realm-import'
New-Item -ItemType Directory -Force $import | Out-Null
$receipt.RealmCommit = (git -C $repo rev-parse $RealmRef).Trim()
$receipt.RealmFiles = [ordered]@{}
foreach ($name in 'staff-realm.json', 'approvers-realm.json') {
    $target = Join-Path $import $name
    $content = git -C $repo show "$($RealmRef):target-dotnet/docs/installation/keycloak/server-realms/$name"
    if ($LASTEXITCODE -ne 0) { throw "Cannot read $name at $RealmRef." }
    [IO.File]::WriteAllText($target, (($content -join "`n") + "`n"), [Text.UTF8Encoding]::new($false))
    $receipt.RealmFiles[$name] = [ordered]@{
        Sha256 = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash.ToLowerInvariant()
        GitBlob = (git -C $repo rev-parse "$($RealmRef):target-dotnet/docs/installation/keycloak/server-realms/$name").Trim()
    }
    Copy-Item $target (Join-Path $EvidenceDirectory $name)
}
$declared = (Get-Content (Join-Path $import 'approvers-realm.json') -Raw | ConvertFrom-Json).authenticationFlows |
    Where-Object alias -eq 'nexaerp-approver-browser'
$receipt.FileDeclares = @($declared.authenticationExecutions | ForEach-Object { "$($_.authenticator)=$($_.requirement)" })
Note "realm files from $RealmRef ($($receipt.RealmCommit)); the file declares: $($receipt.FileDeclares -join ', ')"

$pgStarted = $false; $kcProcess = $null
try {
    # --- 3. Throwaway Keycloak and its own PostgreSQL. ---
    Expand-Archive -LiteralPath $KeycloakZip -DestinationPath $work
    $kcHome = Join-Path $work 'keycloak-26.7.4'
    $kcBat = Join-Path $kcHome 'bin\kc.bat'
    $env:JAVA_HOME = $JavaHome
    $env:PATH = (Join-Path $JavaHome 'bin') + ';' + $env:PATH
    $receipt.JavaVersion = (& cmd.exe /c "`"$(Join-Path $JavaHome 'bin\java.exe')`" -version 2>&1") -join ' | '
    Invoke-Logged 'cmd.exe' "/c `"`"$kcBat`" --version`"" 'kc-version' | Out-Null
    $receipt.KcVersion = (Get-Content (Join-Path $EvidenceDirectory 'kc-version.out.log')) -join ' | '
    Note "kc.bat --version: $($receipt.KcVersion)"

    if ($Database -eq 'postgres') {
        $pgPort = Get-FreePort
        $pgData = Join-Path $work 'pgdata'
        if ((Invoke-Logged (Join-Path $PostgreSqlBin 'initdb.exe') "-D `"$pgData`" -U postgres -A trust -E UTF8" 'initdb') -ne 0) { throw 'initdb failed' }
        if ((Invoke-Logged (Join-Path $PostgreSqlBin 'pg_ctl.exe') "-D `"$pgData`" -l `"$(Join-Path $work 'pg.log')`" -o `"-p $pgPort -c listen_addresses=127.0.0.1`" -w start" 'pg-start') -ne 0) { throw 'PostgreSQL start failed' }
        $pgStarted = $true
        if ((Invoke-Logged (Join-Path $PostgreSqlBin 'createdb.exe') "-h 127.0.0.1 -p $pgPort -U postgres keycloak" 'createdb') -ne 0) { throw 'createdb failed' }
        $env:KC_DB = 'postgres'
        $env:KC_DB_URL = "jdbc:postgresql://127.0.0.1:$pgPort/keycloak"
        $env:KC_DB_USERNAME = 'postgres'
        $env:KC_DB_PASSWORD = 'throwaway-trust-auth'
        $buildArgs = 'build --db=postgres --health-enabled=true'
    } else {
        $buildArgs = 'build --db=dev-file --health-enabled=true'
    }
    $receipt.BuildExit = Invoke-Logged 'cmd.exe' "/c `"`"$kcBat`" $buildArgs`"" 'kc-build'
    if ($receipt.BuildExit -ne 0) { throw 'kc.bat build failed; see kc-build logs.' }

    # --- 4. The same import command as the server procedure. ---
    $receipt.ImportCommand = "kc.bat import --dir $import --override=false"
    $receipt.ImportExit = Invoke-Logged 'cmd.exe' "/c `"`"$kcBat`" import --dir `"$import`" --override=false`"" 'kc-import'
    $importLog = (Get-Content (Join-Path $EvidenceDirectory 'kc-import.out.log'), (Join-Path $EvidenceDirectory 'kc-import.err.log') -ErrorAction SilentlyContinue) -join "`n"
    $receipt.ImportLogMentions = @(($importLog -split "`n") | Where-Object { $_ -match 'approvers|staff|Realm|import' } | Select-Object -First 20)
    if ($receipt.ImportExit -ne 0) { throw 'kc.bat import failed; see kc-import logs.' }
    Note "import exit 0"

    # A throwaway administrator, only for reading back. The password never leaves this process.
    $env:KC_TEST_ADMIN_PASSWORD = [Convert]::ToBase64String((1..24 | ForEach-Object { [byte](Get-Random -Maximum 256) }))
    if ((Invoke-Logged 'cmd.exe' "/c `"`"$kcBat`" bootstrap-admin user --username throwaway-reader --password:env KC_TEST_ADMIN_PASSWORD --optimized`"" 'kc-bootstrap') -ne 0) { throw 'bootstrap-admin failed' }

    $httpPort = Get-FreePort
    $base = "http://127.0.0.1:$httpPort"
    function Start-ThrowawayKeycloak([string]$Label) {
        $proc = Start-Process cmd.exe -PassThru -WindowStyle Hidden -RedirectStandardOutput (Join-Path $EvidenceDirectory "kc-$Label.out.log") -RedirectStandardError (Join-Path $EvidenceDirectory "kc-$Label.err.log") `
            -ArgumentList "/c `"`"$kcBat`" start --optimized --http-enabled=true --http-host=127.0.0.1 --http-port=$httpPort --hostname-strict=false`""
        for ($i = 0; $i -lt 180; $i++) {
            Start-Sleep -Seconds 2
            try { Invoke-RestMethod "$base/realms/master/.well-known/openid-configuration" -TimeoutSec 5 | Out-Null; return $proc } catch { }
        }
        throw "Throwaway Keycloak did not become ready ($Label)."
    }
    function Stop-ThrowawayKeycloak {
        # kc.bat launches java.exe; stop every java process started from this work folder.
        Get-CimInstance Win32_Process -Filter "Name='java.exe'" | Where-Object { $_.CommandLine -like "*$work*" } |
            ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
        Start-Sleep -Seconds 5
    }
    function Read-ApproverFlow([string]$Label) {
        $token = (Invoke-RestMethod -Method Post -Uri "$base/realms/master/protocol/openid-connect/token" -ContentType 'application/x-www-form-urlencoded' `
            -Body @{ grant_type = 'password'; client_id = 'admin-cli'; username = 'throwaway-reader'; password = $env:KC_TEST_ADMIN_PASSWORD }).access_token
        $h = @{ Authorization = "Bearer $token" }
        $realm = Invoke-RestMethod -Headers $h -Uri "$base/admin/realms/approvers"
        $raw = Invoke-RestMethod -Headers $h -Uri "$base/admin/realms/approvers/authentication/flows/nexaerp-approver-browser/executions"
        $executions = @($raw | ForEach-Object { $_ }) | Sort-Object level, index
        $pw = @($executions | Where-Object providerId -eq 'auth-username-password-form')
        $otp = @($executions | Where-Object providerId -eq 'auth-otp-form')
        $ok = ($realm.browserFlow -eq 'nexaerp-approver-browser') -and ($executions.Count -eq 2) -and
            ($pw.Count -eq 1 -and $pw[0].requirement -eq 'REQUIRED') -and ($otp.Count -eq 1 -and $otp[0].requirement -eq 'REQUIRED') -and
            ($otp[0].index -gt $pw[0].index)
        $result = [ordered]@{
            Result = $(if ($ok) { 'PASS' } else { 'FAIL' })
            BoundBrowserFlow = $realm.browserFlow
            Executions = @($executions | ForEach-Object { "$($_.index) $($_.displayName) [$($_.providerId)] $($_.requirement)" })
        }
        Write-Output ''
        Write-Output "==== $Label : $($result.Result) ===="
        Write-Output "bound browser flow: $($realm.browserFlow)"
        $result.Executions | ForEach-Object { Write-Output "  $_" }
        return $result
    }

    # D9 (26 September): the realm files must also reproduce the state the TD exported from the server
    # (RealmFixState-20260926-1545.json, SHA-256 02E9A4D5A73D5BA28005CA2D32AA26E5FB810F595A04CF5A6E59F514B5819FC2):
    # account-console with its three mappers and account role scope, UPDATE_PASSWORD enabled and not
    # default, CONFIGURE_TOTP default only in approvers, delete_account disabled, the ERP client keeping
    # only nexaerp/access, and no realm-level default client scopes.
    function Read-RealmFix([string]$Label) {
        $token = (Invoke-RestMethod -Method Post -Uri "$base/realms/master/protocol/openid-connect/token" -ContentType 'application/x-www-form-urlencoded' `
            -Body @{ grant_type = 'password'; client_id = 'admin-cli'; username = 'throwaway-reader'; password = $env:KC_TEST_ADMIN_PASSWORD }).access_token
        $h = @{ Authorization = "Bearer $token" }
        $failures = [Collections.Generic.List[string]]::new()
        function Check([bool]$Condition, [string]$What) { if (-not $Condition) { $failures.Add($What) } }
        $expectedMappers = [ordered]@{
            'audience resolve' = @{ Type = 'oidc-audience-resolve-mapper'; Config = [ordered]@{} }
            'sess-account-roles' = @{ Type = 'oidc-usermodel-client-role-mapper'; Config = [ordered]@{ 'access.token.claim' = 'true'; 'claim.name' = 'resource_access.${client_id}.roles';
                'id.token.claim' = 'false'; 'introspection.token.claim' = 'true'; 'jsonType.label' = 'String'; 'lightweight.claim' = 'false'; 'multivalued' = 'true';
                'userinfo.token.claim' = 'false'; 'usermodel.clientRoleMapping.clientId' = 'account' } }
            'sess-sub' = @{ Type = 'oidc-sub-mapper'; Config = [ordered]@{ 'access.token.claim' = 'true'; 'introspection.token.claim' = 'true'; 'lightweight.claim' = 'false' } }
        }
        $realms = [ordered]@{}
        foreach ($realmName in 'staff', 'approvers') {
            $r = "$base/admin/realms/$realmName"
            $console = @(Invoke-RestMethod -Headers $h -Uri "$r/clients?clientId=account-console")
            Check ($console.Count -eq 1) "$realmName account-console missing"
            $c = $console[0]
            Check ($c.publicClient -and -not $c.fullScopeAllowed -and $c.standardFlowEnabled -and -not $c.directAccessGrantsEnabled -and -not $c.implicitFlowEnabled -and -not $c.serviceAccountsEnabled) "$realmName account-console flags"
            Check ((@($c.redirectUris) -join ',') -eq "/realms/$realmName/account/*") "$realmName account-console redirect URIs $(@($c.redirectUris) -join ',')"
            Check (@($c.webOrigins).Count -eq 0) "$realmName account-console web origins"
            Check ($c.baseUrl -eq "/realms/$realmName/account/" -and $c.rootUrl -eq '${authBaseUrl}') "$realmName account-console base/root URL"
            Check ($c.attributes.'pkce.code.challenge.method' -eq 'S256' -and $c.attributes.'post.logout.redirect.uris' -eq '+') "$realmName account-console PKCE / logout attributes"
            $mappers = @($c.protocolMappers)
            Check ($mappers.Count -eq 3) "$realmName account-console has $($mappers.Count) mappers, expected 3"
            foreach ($name in $expectedMappers.Keys) {
                $m = @($mappers | Where-Object name -eq $name)
                $want = $expectedMappers[$name]
                Check ($m.Count -eq 1 -and $m[0].protocolMapper -eq $want.Type) "$realmName mapper $name missing or wrong type"
                if ($m.Count -eq 1) {
                    $have = @($m[0].config.PSObject.Properties | ForEach-Object { "$($_.Name)=$($_.Value)" } | Sort-Object) -join ';'
                    $need = @($want.Config.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" } | Sort-Object) -join ';'
                    Check ($have -eq $need) "$realmName mapper $name config [$have] expected [$need]"
                }
            }
            Check (@(Invoke-RestMethod -Headers $h -Uri "$r/clients/$($c.id)/default-client-scopes").Count -eq 0) "$realmName account-console has default client scopes"
            Check (@(Invoke-RestMethod -Headers $h -Uri "$r/clients/$($c.id)/optional-client-scopes").Count -eq 0) "$realmName account-console has optional client scopes"
            $account = @(Invoke-RestMethod -Headers $h -Uri "$r/clients?clientId=account")[0]
            $scope = @(Invoke-RestMethod -Headers $h -Uri "$r/clients/$($c.id)/scope-mappings/clients/$($account.id)" | ForEach-Object { $_.name } | Sort-Object) -join ','
            Check ($scope -eq 'manage-account,view-groups') "$realmName account-console role scope [$scope]"
            $actions = @(Invoke-RestMethod -Headers $h -Uri "$r/authentication/required-actions")
            $totp = @($actions | Where-Object alias -eq 'CONFIGURE_TOTP')[0]; $update = @($actions | Where-Object alias -eq 'UPDATE_PASSWORD')[0]; $delete = @($actions | Where-Object alias -eq 'delete_account')[0]
            Check ($totp -and $totp.enabled -and ($totp.defaultAction -eq ($realmName -eq 'approvers'))) "$realmName CONFIGURE_TOTP enabled/default"
            Check ($update -and $update.enabled -and -not $update.defaultAction) "$realmName UPDATE_PASSWORD enabled, not default"
            Check ($delete -and -not $delete.enabled -and -not $delete.defaultAction) "$realmName delete_account disabled"
            $erp = @(Invoke-RestMethod -Headers $h -Uri "$r/clients?clientId=nexaerp-$realmName")[0]
            $erpScopes = @(Invoke-RestMethod -Headers $h -Uri "$r/clients/$($erp.id)/default-client-scopes" | ForEach-Object { $_.name } | Sort-Object) -join ','
            Check ($erpScopes -eq 'nexaerp/access' -and @($erp.protocolMappers).Count -eq 2 -and -not $erp.fullScopeAllowed) "$realmName ERP client changed (scopes [$erpScopes])"
            Check (@(Invoke-RestMethod -Headers $h -Uri "$r/default-default-client-scopes").Count -eq 0) "$realmName has realm-level default client scopes"
            Check (@(Invoke-RestMethod -Headers $h -Uri "$r/default-optional-client-scopes").Count -eq 0) "$realmName has realm-level optional client scopes"
            $realms[$realmName] = [ordered]@{ Mappers = @($mappers | ForEach-Object { $_.name }); RoleScope = $scope; ErpScopes = $erpScopes
                RequiredActions = @($actions | ForEach-Object { "$($_.alias) enabled=$($_.enabled) default=$($_.defaultAction)" }) }
        }
        $result = [ordered]@{ Result = $(if ($failures.Count -eq 0) { 'PASS' } else { 'FAIL' }); Failures = @($failures); Realms = $realms }
        Write-Output ''
        Write-Output "==== D9 $Label : $($result.Result) ===="
        $failures | ForEach-Object { Write-Output "  FAIL: $_" }
        return $result
    }

    # --- 5. Read back straight after import, then again after a restart. ---
    $kcProcess = Start-ThrowawayKeycloak 'first-start'
    $receipt.AfterImport = Read-ApproverFlow 'AFTER IMPORT'
    $receipt.D9AfterImport = Read-RealmFix 'AFTER IMPORT'
    Stop-ThrowawayKeycloak
    $kcProcess = Start-ThrowawayKeycloak 'restart'
    $receipt.AfterRestart = Read-ApproverFlow 'AFTER RESTART'
    $receipt.D9AfterRestart = Read-RealmFix 'AFTER RESTART'
    Stop-ThrowawayKeycloak
    $kcProcess = $null
}
finally {
    if ($kcProcess) { try { Stop-ThrowawayKeycloak } catch { } }
    if ($pgStarted) { Invoke-Logged (Join-Path $PostgreSqlBin 'pg_ctl.exe') "-D `"$(Join-Path $work 'pgdata')`" -m fast -w stop" 'pg-stop' | Out-Null }
    Remove-Item Env:KC_TEST_ADMIN_PASSWORD, Env:KC_DB, Env:KC_DB_URL, Env:KC_DB_USERNAME, Env:KC_DB_PASSWORD -ErrorAction SilentlyContinue
    if (-not $KeepWorkFolder -and (Test-Path $work)) { Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue }
    $receipt.WorkFolderRemoved = -not (Test-Path $work)
    $receipt.Finished = (Get-Date -Format o)
    $receipt | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $EvidenceDirectory 'receipt.json') -Encoding utf8
}

Write-Output ''
Write-Output "AFTER IMPORT : $($receipt.AfterImport.Result)"
Write-Output "AFTER RESTART: $($receipt.AfterRestart.Result)"
if ($receipt.AfterImport.Result -eq 'FAIL') {
    Write-Output 'The importer itself loses the OTP step: the 30 September import would repeat it. Repair and read back after every import.'
} elseif ($receipt.AfterRestart.Result -eq 'FAIL') {
    Write-Output 'The import is correct but a restart loses the step: report this before any further server work.'
} else {
    Write-Output 'The importer keeps the OTP step through import and restart: the server flow lost it some other way (by hand or by another script).'
}
Write-Output "D9 AFTER IMPORT : $($receipt.D9AfterImport.Result)"
Write-Output "D9 AFTER RESTART: $($receipt.D9AfterRestart.Result)"
Write-Output "Receipt: $(Join-Path $EvidenceDirectory 'receipt.json')"
if ($receipt.AfterImport.Result -ne 'PASS' -or $receipt.AfterRestart.Result -ne 'PASS' -or
    $receipt.D9AfterImport.Result -ne 'PASS' -or $receipt.D9AfterRestart.Result -ne 'PASS') { exit 1 }
