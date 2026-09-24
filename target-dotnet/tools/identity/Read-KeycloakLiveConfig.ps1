<#
.SYNOPSIS
Reads the LIVE Keycloak configuration of the staff and approvers realms back from the server and
compares it with what Step 6 depends on. READ-ONLY: it changes nothing.

.DESCRIPTION
Rule of 24 September 2026 (finding #33): a configuration is verified by reading the live system,
never by reading what we meant to send it. The realm exports said "REQUIRED OTP"; the live
Approvers flow had no OTP step. Run this on the server before Step 6 and paste its output.

Per realm it reads: realm login and session settings, the bound browser flow and its executions,
the ERP client (redirect URIs, web origins, grants, PKCE, flow overrides, scopes, protocol
mappers), identity providers and the Configure OTP required action. With -ApiAuthConfigPath it
also reads the API's deployed authentication profiles, which decide which realm counts as MFA.

Every line is PASS, FAIL or INFO. Exit code 1 if any line FAILs. No password or token is printed.
#>
param(
    [string]$KeycloakBase = 'https://192.168.68.130:8444',
    [string]$AdminRealm = 'master',
    [Parameter(Mandatory = $true)][string]$AdminUser,
    # The deployed API file holding Authentication:Providers, e.g. the server's appsettings.
    [string]$ApiAuthConfigPath
)
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$erpOrigin = 'https://192.168.68.130:8443'
$expected = @{
    staff = @{
        Client = 'nexaerp-staff'; BrowserFlow = 'browser'; OtpDefaultAction = $false
        Redirects = @("$erpOrigin/oidc/callback", 'http://127.0.0.1:8765/callback/')
    }
    approvers = @{
        Client = 'nexaerp-approvers'; BrowserFlow = 'nexaerp-approver-browser'; OtpDefaultAction = $true
        Redirects = @("$erpOrigin/oidc/callback", 'http://127.0.0.1:8765/callback/')
    }
}

$secure = Read-Host "Password for $AdminUser in realm $AdminRealm" -AsSecureString
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
try {
    $plain = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    $token = (Invoke-RestMethod -Method Post -Uri "$KeycloakBase/realms/$AdminRealm/protocol/openid-connect/token" `
        -ContentType 'application/x-www-form-urlencoded' `
        -Body @{ grant_type = 'password'; client_id = 'admin-cli'; username = $AdminUser; password = $plain }).access_token
} finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    $plain = $null
}
if (-not $token) { throw 'No admin token returned.' }
$headers = @{ Authorization = "Bearer $token" }

function Get-Rest([string]$Uri) {
    $result = Invoke-RestMethod -Headers $headers -Uri $Uri
    # Windows PowerShell 5.1 returns a JSON array as one object; enumerate it explicitly.
    return @($result | ForEach-Object { $_ })
}
$script:failed = 0
function Check([string]$Realm, [string]$Name, [bool]$Ok, $Actual) {
    $state = if ($Ok) { 'PASS' } else { $script:failed++; 'FAIL' }
    Write-Output ("{0}  [{1}] {2}  (live: {3})" -f $state, $Realm, $Name, $Actual)
}
function Info([string]$Realm, [string]$Text) { Write-Output ("INFO  [{0}] {1}" -f $Realm, $Text) }
function Same-Set($A, $B) {
    $x = @($A | Sort-Object -Unique); $y = @($B | Sort-Object -Unique)
    return ($x.Count -eq $y.Count) -and (@(Compare-Object $x $y -CaseSensitive).Count -eq 0)
}

foreach ($realm in 'staff', 'approvers') {
    $e = $expected[$realm]
    $admin = "$KeycloakBase/admin/realms/$realm"
    Write-Output ''
    Write-Output "==== $realm ===="
    $r = Invoke-RestMethod -Headers $headers -Uri $admin

    # Realm login and session settings.
    Check $realm 'Enabled' ($r.enabled -eq $true) $r.enabled
    Check $realm 'Require SSL = all' ($r.sslRequired -eq 'all') $r.sslRequired
    Check $realm 'User registration OFF' ($r.registrationAllowed -eq $false) $r.registrationAllowed
    Check $realm 'Forgot password OFF' ($r.resetPasswordAllowed -eq $false) $r.resetPasswordAllowed
    Check $realm 'Remember me OFF' ($r.rememberMe -eq $false) $r.rememberMe
    Check $realm 'Login with email OFF (decided 24 Sep)' ($r.loginWithEmailAllowed -eq $false) $r.loginWithEmailAllowed
    Check $realm 'Duplicate emails OFF' ($r.duplicateEmailsAllowed -eq $false) $r.duplicateEmailsAllowed
    Check $realm 'Password policy has length(14)' ([string]$r.passwordPolicy -match 'length\(14\)') $r.passwordPolicy
    Check $realm 'Brute force protection ON' ($r.bruteForceProtected -eq $true) $r.bruteForceProtected
    Check $realm 'Access token 900 s' ($r.accessTokenLifespan -eq 900) $r.accessTokenLifespan
    Check $realm 'SSO idle and max 28800 s' ($r.ssoSessionIdleTimeout -eq 28800 -and $r.ssoSessionMaxLifespan -eq 28800) "$($r.ssoSessionIdleTimeout)/$($r.ssoSessionMaxLifespan)"
    Check $realm 'Client session idle and max 28800 s' ($r.clientSessionIdleTimeout -eq 28800 -and $r.clientSessionMaxLifespan -eq 28800) "$($r.clientSessionIdleTimeout)/$($r.clientSessionMaxLifespan)"

    # The bound browser flow, read from its live executions.
    Check $realm "Browser flow bound = $($e.BrowserFlow)" ($r.browserFlow -eq $e.BrowserFlow) $r.browserFlow
    $executions = Get-Rest "$admin/authentication/flows/$([uri]::EscapeDataString($r.browserFlow))/executions"
    Info $realm "Live executions of '$($r.browserFlow)':"
    Write-Output ($executions | Sort-Object index | Format-Table level, index, displayName, providerId, requirement -AutoSize | Out-String -Width 200).TrimEnd()
    $otp = @($executions | Where-Object providerId -eq 'auth-otp-form')
    if ($realm -eq 'approvers') {
        $pw = @($executions | Where-Object providerId -eq 'auth-username-password-form')
        Check $realm 'Exactly two top-level executions' ($executions.Count -eq 2 -and @($executions | Where-Object level -ne 0).Count -eq 0) $executions.Count
        Check $realm 'Username Password Form REQUIRED' ($pw.Count -eq 1 -and $pw[0].requirement -eq 'REQUIRED') (($pw | ForEach-Object requirement) -join ',')
        Check $realm 'OTP Form REQUIRED (not conditional/alternative)' ($otp.Count -eq 1 -and $otp[0].requirement -eq 'REQUIRED') (($otp | ForEach-Object requirement) -join ',')
    } else {
        # Staff keeps the ordinary flow. OTP there must never be REQUIRED for everyone; the API gives Staff no MFA credit either way.
        Check $realm 'No OTP Form REQUIRED in the Staff flow' (@($otp | Where-Object requirement -eq 'REQUIRED').Count -eq 0) (($otp | ForEach-Object requirement) -join ',')
    }

    # The ERP client.
    $clients = @(Get-Rest "$admin/clients?clientId=$($e.Client)")
    Check $realm "Exactly one client $($e.Client)" ($clients.Count -eq 1) $clients.Count
    if ($clients.Count -eq 1) {
        $c = $clients[0]
        Check $realm 'Client enabled, public' ($c.enabled -eq $true -and $c.publicClient -eq $true) "enabled=$($c.enabled) public=$($c.publicClient)"
        Check $realm 'Standard flow ON' ($c.standardFlowEnabled -eq $true) $c.standardFlowEnabled
        Check $realm 'Direct access grants OFF' ($c.directAccessGrantsEnabled -eq $false) $c.directAccessGrantsEnabled
        Check $realm 'Implicit flow OFF' ($c.implicitFlowEnabled -eq $false) $c.implicitFlowEnabled
        Check $realm 'Service accounts OFF' ($c.serviceAccountsEnabled -eq $false) $c.serviceAccountsEnabled
        Check $realm 'Device grant OFF' ($c.attributes.'oauth2.device.authorization.grant.enabled' -ne 'true') $c.attributes.'oauth2.device.authorization.grant.enabled'
        Check $realm 'PKCE S256' ($c.attributes.'pkce.code.challenge.method' -eq 'S256') $c.attributes.'pkce.code.challenge.method'
        Check $realm 'Redirect URIs exactly as expected, no wildcard' ((Same-Set $c.redirectUris $e.Redirects) -and -not ($c.redirectUris -match '\*')) ($c.redirectUris -join ' , ')
        Check $realm 'Web origins exactly the ERP origin' (Same-Set $c.webOrigins @($erpOrigin)) ($c.webOrigins -join ' , ')
        Check $realm 'Post-logout redirect' ($c.attributes.'post.logout.redirect.uris' -eq "$erpOrigin/oidc/logout-callback") $c.attributes.'post.logout.redirect.uris'
        $overrides = @($c.authenticationFlowBindingOverrides.PSObject.Properties | Where-Object { $_.Value })
        Check $realm 'No client flow override' ($overrides.Count -eq 0) (($overrides | ForEach-Object { "$($_.Name)=$($_.Value)" }) -join ',')
        Check $realm 'Full scope allowed OFF' ($c.fullScopeAllowed -eq $false) $c.fullScopeAllowed
        $scopes = Get-Rest "$admin/clients/$($c.id)/default-client-scopes" | ForEach-Object name
        Check $realm 'Default scopes include nexaerp/access' ($scopes -contains 'nexaerp/access') ($scopes -join ',')
        $mappers = Get-Rest "$admin/clients/$($c.id)/protocol-mappers/models"
        $audience = @($mappers | Where-Object { $_.protocolMapper -eq 'oidc-audience-mapper' -and $_.config.'included.custom.audience' -eq 'nexaerp' -and $_.config.'access.token.claim' -eq 'true' })
        Check $realm 'Audience mapper nexaerp on access token' ($audience.Count -eq 1) $audience.Count
        $others = @($mappers | Where-Object { $_.name -notin @('erp-audience', 'stable-subject') })
        Check $realm 'No client mappers beyond erp-audience and stable-subject' ($others.Count -eq 0) (($others | ForEach-Object { "$($_.name) [$($_.protocolMapper)]" }) -join ',')
    }

    $idps = Get-Rest "$admin/identity-provider/instances"
    Check $realm 'No external identity providers' ($idps.Count -eq 0) (($idps | ForEach-Object alias) -join ',')
    $totp = Invoke-RestMethod -Headers $headers -Uri "$admin/authentication/required-actions/CONFIGURE_TOTP"
    Check $realm "Configure OTP enabled, default=$($e.OtpDefaultAction)" ($totp.enabled -eq $true -and $totp.defaultAction -eq $e.OtpDefaultAction) "enabled=$($totp.enabled) default=$($totp.defaultAction)"
}

# The API side: which realm the API gives MFA credit to. Read from the deployed file, not the package copy.
Write-Output ''
Write-Output '==== API authentication profiles ===='
if ($ApiAuthConfigPath) {
    $providers = (Get-Content -LiteralPath $ApiAuthConfigPath -Raw | ConvertFrom-Json).Authentication.Providers
    foreach ($name in 'staff', 'approvers') {
        $p = $providers.$name
        if (-not $p) { Check 'api' "Profile $name present" $false 'missing'; continue }
        Check 'api' "$name Authority is the $name realm" ($p.Authority -eq "$KeycloakBase/realms/$name") $p.Authority
        Check 'api' "$name ClientId" ($p.ClientId -eq $expected[$name].Client) $p.ClientId
        Check 'api' "$name MfaGuaranteedByProvider = $($name -eq 'approvers')" ($p.MfaGuaranteedByProvider -eq ($name -eq 'approvers')) $p.MfaGuaranteedByProvider
        Check 'api' "$name has no MfaClaim" (-not $p.MfaClaim) $p.MfaClaim
    }
} else {
    Info 'api' 'Not read: pass -ApiAuthConfigPath <deployed config file> to check which realm the API treats as MFA.'
}

Write-Output ''
if ($script:failed -gt 0) { Write-Output "RESULT: $($script:failed) FAIL line(s). Step 6 stays on hold."; exit 1 }
Write-Output 'RESULT: all PASS. This is the live configuration, not the export. Step 6 still also needs the witnessed Approvers login.'
