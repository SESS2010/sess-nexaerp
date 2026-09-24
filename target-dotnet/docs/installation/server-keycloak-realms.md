# D3: Staff and Approvers realm import and mandatory MFA

> **Protected server rule (updated 22 September):** NEVER stop, disable, modify or remove
> SESS_SQLEXPRESS, TEW_SQLEXPRESS, SQLBrowser, their ~77 SOLIDWORKS project databases,
> ewserver, ANY Rockwell FactoryTalk service, IIS Default
> Web Site or /Updater. Leave Wamp stopped/manual. Windows 10 stays; NEVER a clean
> Windows install; NEVER install a .NET SDK on this server. All application here is
> by the server agent, not from the laptop. See C:\SESS-ServerPrep for completed preparation.

These rules apply to every linked deployment, identity, certificate and backup procedure. Stop and report a conflict; do not reclaim ports or memory from protected services.

The package contains credential-free server realm exports in
installer/docs/keycloak/server-realms/{staff,approvers}-realm.json. They use D1's
exact ERP callbacks/origin on 8443, scopes, public clients and logout callback. They
contain no employees, passwords, TOTP seeds or signing keys. The Keycloak database
will generate and retain those. Never replace a populated realm by reimporting.

1. With only SESSKeycloak stopped and the configured sess_keycloak database ready,
   copy these two files to C:\SESS-Identity\realm-import. Check JSON and package hashes.
   Run the native Keycloak importer (NOT the ERP database migration Installer):

   ```powershell
   & "$kc\bin\kc.bat" import --dir C:\SESS-Identity\realm-import --override=false
   if ($LASTEXITCODE -ne 0) { throw 'Realm import failed' }
   ```

   Check both realms imported, rather than skipped unexpectedly. On replay,
   --override=false preserves existing realms; compare their live settings manually
   and stop on drift. Never change that to true against populated realms.
2. Complete the D2 startup/bootstrap-master-admin steps. In realm settings verify
   registration/reset-by-email/Remember Me are off, brute-force protection on,
   password length >=14, access token 900 seconds, bounded session 28800 seconds.
   Clients: Standard flow only, public, S256 PKCE required; direct grants, implicit
   flow, service accounts off. No wildcard redirect/origin, external broker or client
   authentication-flow override. Check discovery and token audience nexaerp, azp
   matching the client, typ=Bearer and scope containing nexaerp/access.
3. Approvers Authentication -> Bindings must select nexaerp-approver-browser. Its
   password and OTP executions are both REQUIRED (not conditional/alternative).
   Configure OTP required action is enabled/default. Staff retains its ordinary
   browser flow; it does not grant the API's trusted-MFA guarantee.
4. Create SESS-01, SESS-02 and SESS-14 ONLY in Approvers; mandatory Configure OTP and
   Update Password at first login. Set strong temporary passwords separately and
   deliver them privately. Do not seed passwords in JSON. SESS-12 belongs to Staff.
   Other current TD/MD/Accounts Manager/CFO users also belong to Approvers even if
   their employee code is not in the named three. All other staff start in Staff.
5. Each named approver signs in personally, changes temporary password and enrolls
   their authenticator. Check every one cannot finish sign-in without OTP and is
   refused with a wrong OTP. No recovery path may silently downgrade to Staff.
   Lost-device recovery requires verified identity, approved reset, reenrollment,
   session revocation and audit; never weaken the realm flow temporarily.
6. Capture each user's immutable realm subject (Keycloak user ID), match it to a
   successful OIDC login, then follow D5. Realm choice/name alone grants no ERP role.
   API trusted MFA for Approvers relies on administrators preserving this flow.
   Verify Staff cannot access a company as a governed privileged role, even with an
   otherwise valid token. Keep per-employee acceptance receipts; no token values.

Static checks on these exports passed for exact URLs, PKCE, public clients, no
embedded users/secrets and REQUIRED OTP/password. They derive from the previously
witnessed templates; import and login on this Windows 26.7.4 server remain field
acceptance, not a newly executed production witness.

[Keycloak offline import semantics](https://www.keycloak.org/server/importExport).

## Running the two identity scripts (finding #33)

[tools/identity/Repair-ApproverOtpFlow.ps1](../../tools/identity/Repair-ApproverOtpFlow.ps1) and
[tools/identity/Read-KeycloakLiveConfig.ps1](../../tools/identity/Read-KeycloakLiveConfig.ps1) talk
to Keycloak only over HTTPS REST at `https://192.168.68.130:8444`. That is Keycloak's main
listener: `server-keycloak.conf` sets no separate admin hostname, so `/admin` is served on the
same port. Neither script uses the management port 9000, which stays on loopback.

**Can they run from this laptop over the network? Technically yes**, given three things you
have confirmed or can check in one line:

- this laptop trusts the SESS root CA;
- the `SESS-Keycloak-HTTPS-8444` rule admits `192.168.68.0/24`;
- Windows PowerShell 5.1, which both scripts force onto TLS 1.2.

Check before anything else. It must print `200`:

```powershell
(Invoke-WebRequest https://192.168.68.130:8444/realms/approvers/.well-known/openid-configuration -UseBasicParsing).StatusCode
```

**Sign-in.** Both scripts sign in as the **named master administrator** created in
[server-keycloak-install.md](server-keycloak-install.md) step 9. Never use `sess-setup`, which
that step deletes. The scripts prompt for the password and then for that account's **current
authenticator code**, because step 9 gives the account REQUIRED OTP. Nothing is printed or
stored. They use Keycloak's own `admin-cli` client in the master realm, whose direct sign-in
is on by default. If the token call fails with `unauthorized_client`, that has been turned off
on the server; stop and report, and do not turn it on to make the script work.

**Where to run each one:**

| Script | What it does | Where |
|---|---|---|
| `Repair-ApproverOtpFlow.ps1` | **Changes** the live Approvers flow | **On the server**, recommended. It works from the laptop, but the standing rule is that the server is changed on the server, not from the laptop. Copy the one file to `C:\SESS-Identity\scripts\` and check its SHA-256 against the committed file. |
| `Read-KeycloakLiveConfig.ps1`, Keycloak part | Reads only | **Either.** From the laptop is fine. |
| `Read-KeycloakLiveConfig.ps1` with `-ApiAuthConfigPath` | Also reads the **deployed** API authentication profiles | **On the server only.** The file is the installed `appsettings.Production.json`, which is ACL-restricted and not shared, so the laptop cannot read it. |

**Exact commands on the server** (elevated Windows PowerShell 5.1; elevation is only for
reading the ACL-protected API file):

```powershell
cd C:\SESS-Identity\scripts
# 1. Read first; changes nothing.
powershell -NoProfile -ExecutionPolicy Bypass -File .\Repair-ApproverOtpFlow.ps1 -AdminUser <named-admin> -VerifyOnly
# 2. The repair, ending every session opened under the password-only flow.
powershell -NoProfile -ExecutionPolicy Bypass -File .\Repair-ApproverOtpFlow.ps1 -AdminUser <named-admin> -SignOutApproverSessions
# 3. Full live read-back, including the API's MFA trust.
powershell -NoProfile -ExecutionPolicy Bypass -File .\Read-KeycloakLiveConfig.ps1 -AdminUser <named-admin> -ApiAuthConfigPath 'C:\SESS\<installed-sha>\api\appsettings.Production.json'
```

**`-ApiAuthConfigPath`** points at the API configuration the running service actually reads:
`C:\SESS\<installed-sha>\api\appsettings.Production.json`. `<installed-sha>` is the folder of
the package currently installed for the `SESSNexaERP` service; `sc.exe qc SESSNexaERP` shows
its path. **Not** the package copy under `C:\SESS-Deploy\...`, and not
`authentication.server.keycloak.json` from the package. Those are what we meant to deploy, and
by the rule of 24 September only the installed file proves what the API does.

**From the laptop** (read-only Keycloak part only), in the repository:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\identity\Read-KeycloakLiveConfig.ps1 -AdminUser <named-admin>
```

## Initial wrapper callback (22 September handoff)

After importing both realm exports, the identity maintainer adds the exact redirect
URI `http://127.0.0.1:8765/callback/` to BOTH public clients `nexaerp-staff` in Staff
and `nexaerp-approvers` in Approvers. Keep existing exact SPA redirects. No wildcard,
localhost alias, alternate port or unrestricted web origin. This narrow HTTP exception
is the native loopback callback on the EMPLOYEE'S PC, not a server HTTP listener.
Exports in this package retain their original SPA redirect lists: this is an explicit
post-import configuration step, not an assertion that exports already contain it.
Check standard flow enabled, S256 required, direct/implicit/service-account grants off,
audience/scope/azp contract intact, and Approvers required password+OTP flow unchanged.
Save a redacted client-settings receipt. No employee impersonation or password grant.
For server-agent-demo-start.md, defer general employee onboarding and wrapper rehearsal;
only verified SESS-12 identity/bootstrap is in the initial handoff.
