<#
.SYNOPSIS
Finding #33: the Approvers browser flow on the server holds only the Username Password Form.
This adds the OTP Form as REQUIRED (not conditional, not alternative) to nexaerp-approver-browser
and prints the flow executions as verification.

.DESCRIPTION
Run by the identity maintainer, in Windows PowerShell 5.1, against the running Keycloak -
preferably ON THE SERVER (see docs/installation/server-keycloak-realms.md, 'Running the two
identity scripts'). It works over HTTPS from any PC that trusts the SESS root CA. It changes ONLY the nexaerp-approver-browser flow of the approvers realm. It does not
touch the staff realm, the flow binding, clients, users, the ERP database or any protected service.

It refuses to change anything, and reports why, if the flow holds anything other than the
Username Password Form and the OTP Form, if the realm is not bound to this flow, or if the
nexaerp-approvers client overrides the browser flow.

Re-running is safe: an OTP Form that is already present and REQUIRED is left alone.
-VerifyOnly prints the checks and changes nothing.
No password or token is ever printed.
#>
param(
    [string]$KeycloakBase = 'https://192.168.68.130:8444',
    [string]$AdminRealm = 'master',
    [Parameter(Mandatory = $true)][string]$AdminUser,
    [switch]$VerifyOnly,
    # Ends every Approvers session so no token issued under the password-only flow stays usable.
    [switch]$SignOutApproverSessions
)
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$realm = 'approvers'
$flowAlias = 'nexaerp-approver-browser'
$passwordForm = 'auth-username-password-form'
$otpForm = 'auth-otp-form'

$secure = Read-Host "Password for $AdminUser in realm $AdminRealm" -AsSecureString
# The named master administrator has REQUIRED OTP (server-keycloak-install.md step 9), so the
# admin-cli sign-in needs the current authenticator code. Leave it blank only if the account has none.
$adminOtp = Read-Host "Current authenticator code for $AdminUser (blank if none)"
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
try {
    $plain = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    $token = (Invoke-RestMethod -Method Post -Uri "$KeycloakBase/realms/$AdminRealm/protocol/openid-connect/token" `
        -ContentType 'application/x-www-form-urlencoded' `
        -Body $($form = @{ grant_type = 'password'; client_id = 'admin-cli'; username = $AdminUser; password = $plain }; if ($adminOtp) { $form.otp = $adminOtp.Trim() }; $form)).access_token
} finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    $plain = $null
}
if (-not $token) { throw 'No admin token returned.' }
$headers = @{ Authorization = "Bearer $token" }
$admin = "$KeycloakBase/admin/realms/$realm"
$executionsUri = "$admin/authentication/flows/$([uri]::EscapeDataString($flowAlias))/executions"

function Get-Rest([string]$Uri) {
    $result = Invoke-RestMethod -Headers $headers -Uri $Uri
    # Windows PowerShell 5.1 returns a JSON array as one object; enumerate it explicitly.
    return @($result | ForEach-Object { $_ })
}
function Get-Executions { return Get-Rest $executionsUri }
function Show-Executions($Executions) {
    $Executions | Sort-Object level, index |
        Format-Table index, level, displayName, providerId, requirement -AutoSize | Out-String -Width 200
}

# 1. Pre-checks. Any mismatch stops before a change is made.
$realmRep = Invoke-RestMethod -Headers $headers -Uri $admin
if ($realmRep.browserFlow -ne $flowAlias) {
    throw "Realm $realm is bound to browser flow '$($realmRep.browserFlow)', not $flowAlias. Nothing changed; report this."
}
$client = @(Get-Rest "$admin/clients?clientId=nexaerp-approvers")
if ($client.Count -ne 1) { throw "Expected exactly one nexaerp-approvers client, found $($client.Count). Nothing changed." }
$overrides = $client[0].authenticationFlowBindingOverrides
if ($overrides -and @($overrides.PSObject.Properties | Where-Object { $_.Value }).Count -gt 0) {
    throw 'nexaerp-approvers has an authentication flow override. Nothing changed; report this.'
}

$before = Get-Executions
Write-Output 'BEFORE:'
Write-Output (Show-Executions $before)
$unexpected = @($before | Where-Object { $_.level -ne 0 -or $_.providerId -notin @($passwordForm, $otpForm) })
if ($unexpected.Count -gt 0) {
    throw "The flow holds executions other than the password and OTP forms: $(($unexpected | ForEach-Object { "$($_.displayName) [$($_.providerId)]" }) -join ', '). Nothing changed; report this."
}
$password = @($before | Where-Object providerId -eq $passwordForm)
if ($password.Count -ne 1 -or $password[0].requirement -ne 'REQUIRED') {
    throw 'The Username Password Form is missing or not REQUIRED. Nothing changed; report this.'
}

# 2. The change: add the OTP Form if absent, then make it REQUIRED.
if (-not $VerifyOnly) {
    $otp = @($before | Where-Object providerId -eq $otpForm)
    if ($otp.Count -gt 1) { throw 'More than one OTP Form in the flow. Nothing changed; report this.' }
    if ($otp.Count -eq 0) {
        Invoke-RestMethod -Method Post -Headers $headers -Uri "$executionsUri/execution" `
            -ContentType 'application/json' -Body (@{ provider = $otpForm } | ConvertTo-Json) | Out-Null
        Write-Output 'Added OTP Form.'
        $otp = @(Get-Executions | Where-Object providerId -eq $otpForm)
    }
    if ($otp[0].requirement -ne 'REQUIRED') {
        $otp[0].requirement = 'REQUIRED'
        Invoke-RestMethod -Method Put -Headers $headers -Uri $executionsUri `
            -ContentType 'application/json' -Body ($otp[0] | ConvertTo-Json -Depth 5) | Out-Null
        Write-Output 'Set OTP Form to REQUIRED.'
    }
}

# 3. Verification, read back from Keycloak rather than assumed.
$after = Get-Executions
Write-Output 'AFTER:'
Write-Output (Show-Executions $after)
$totp = Invoke-RestMethod -Headers $headers -Uri "$admin/authentication/required-actions/CONFIGURE_TOTP"
$pw = @($after | Where-Object providerId -eq $passwordForm)
$ot = @($after | Where-Object providerId -eq $otpForm)
$checks = [ordered]@{
    'Realm browser flow is nexaerp-approver-browser'   = ((Invoke-RestMethod -Headers $headers -Uri $admin).browserFlow -eq $flowAlias)
    'Exactly two executions, both top level'           = ($after.Count -eq 2 -and @($after | Where-Object level -ne 0).Count -eq 0)
    'Username Password Form is REQUIRED'               = ($pw.Count -eq 1 -and $pw[0].requirement -eq 'REQUIRED')
    'OTP Form is REQUIRED'                             = ($ot.Count -eq 1 -and $ot[0].requirement -eq 'REQUIRED')
    'OTP Form comes after the password form'           = ($pw.Count -eq 1 -and $ot.Count -eq 1 -and $ot[0].index -gt $pw[0].index)
    'Configure OTP required action enabled and default' = ($totp.enabled -eq $true -and $totp.defaultAction -eq $true)
}
$failed = 0
foreach ($check in $checks.GetEnumerator()) {
    $state = if ($check.Value) { 'PASS' } else { $failed++; 'FAIL' }
    Write-Output ("{0}  {1}" -f $state, $check.Key)
}

# 4. Sessions opened under the password-only flow.
$stats = Get-Rest "$admin/client-session-stats"
$active = ($stats | Measure-Object -Property active -Sum).Sum
Write-Output "Active Approvers client sessions now: $([int]$active)"
if ($SignOutApproverSessions -and -not $VerifyOnly -and $failed -eq 0) {
    Invoke-RestMethod -Method Post -Headers $headers -Uri "$admin/logout-all" | Out-Null
    Write-Output 'Signed out every Approvers session. Each approver signs in again with password AND OTP.'
} elseif ([int]$active -gt 0) {
    Write-Output 'Sessions exist that were opened under the password-only flow. Re-run with -SignOutApproverSessions.'
}

if ($failed -gt 0) { Write-Output "RESULT: $failed check(s) FAILED. Do not type CHECKED."; exit 1 }
Write-Output 'RESULT: all checks PASS. Next: one fresh Approvers login must ask for password AND the authenticator code, and a wrong code must be refused.'
