# Server agent: initial DEMO commissioning — STOP after SESS-12

## NEVER TOUCH

SQL Server (including SESS_SQLEXPRESS, TEW_SQLEXPRESS, SQLBrowser and all design
databases); SOLIDWORKS Electrical / ewserver; ANY NI, Siemens or Rockwell service;
IIS Default Web Site and /Updater, ports 80/81; Wampserver (leave stopped/manual).
**D: and E: are prohibited for ANY use**, including staging, logs, backups and verification.
No clean Windows install, SDK, owner database, production ERP database or stock-moving
command. Never stop protected services to free memory/ports. Only the newly installed
ERP/Keycloak services may be managed here. Do not reboot the shared engineering PC
without a separately approved window.

This is the single ordered handoff for the agent working from **MAGESHWARI PC**.
The recorded target is **DESKTOP-SPF5420, 192.168.68.130**. Check `hostname` and IP
against the server-preparation receipt. MAGESHWARI is not assumed to be its Windows
hostname: if the agent is on another PC, perform server steps locally on the confirmed
target; do not install a second ERP on the operator PC. Any target mismatch stops work
for TD confirmation. Keycloak is NOT installed yet. Only PostgreSQL 17.11, ASP.NET
Core .NET 10 runtime and server preparation are confirmed.

Each numbered step ends with a check and a recorded receipt. A failed check stops the
sequence. References below are included in the package's installer/docs folder and
supply the exact certificate/service commands; use only the named bounded sections,
not later production/demo-walk steps. Never record passwords, bearer tokens or private keys.

## 0. READ-ONLY permission check - report before installation

Run on the confirmed SERVER in elevated PowerShell. This step changes no ACLs.
Do not copy the laptop's permissions or assume the server has the same problem.
Identify the actual PostgreSQL data directory with an authenticated read-only query:

```powershell
whoami
& 'C:\Program Files\PostgreSQL\17\bin\psql.exe' -X -h 127.0.0.1 -U postgres -d postgres -At -c 'SHOW data_directory;'
if ($LASTEXITCODE -ne 0) { throw 'Cannot identify PostgreSQL data directory' }
$pgData=Read-Host 'Exact data_directory returned above (must be on C:)'
if (-not [IO.Path]::GetFullPath($pgData).StartsWith('C:\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected PostgreSQL disk; stop' }
foreach ($path in @('C:\','C:\Users',$pgData,'C:\SESS-Deploy')) {
 if (-not (Test-Path -LiteralPath $path)) { Write-Output "ABSENT: $path"; continue }
 $acl=Get-Acl -LiteralPath $path
 Write-Output "PATH: $path OWNER: $($acl.Owner) INHERITANCE_DISABLED: $($acl.AreAccessRulesProtected)"
 $acl.Access | Select-Object IdentityReference,AccessControlType,FileSystemRights,IsInherited,InheritanceFlags,PropagationFlags | Format-Table -AutoSize
}
Get-LocalGroupMember -Group 'Administrators' | Select-Object Name,ObjectClass,PrincipalSource
Get-LocalGroupMember -Group 'Users' | Select-Object Name,ObjectClass,PrincipalSource
```

Check/report **all Allow entries containing FullControl, Modify, Write, CreateFiles,
CreateDirectories, ChangePermissions or TakeOwnership**, alongside Deny entries and
inheritance/propagation. Explicitly call out Everyone, Authenticated Users, BUILTIN\Users,
interactive users, local groups and service identities. Root-container rights may
not apply to descendants; a DACL listing is not a complete effective-access calculation.
Resolve nested/domain group membership or use Windows Advanced Security / Effective
Access read-only for any ambiguous principal; mark unresolved membership UNKNOWN.
Do not read database contents or print secrets. An absent C:\SESS-Deploy is reported
as absent; inspect C:\ inheritance before creating it later.

Retain the redacted ACL/group report and submit the findings to TD. **No ACL change
or broad repair is authorised by this check.** If untrusted users can modify the
PostgreSQL data directory or package/installation contents, pause installation for
TD's explicit remediation decision. Do not use icacls /grant, /reset or /inheritance,
takeown, or a security-template reset to repair these findings. The later dedicated
ERP/Keycloak directory/private-key ACL steps also require TD approval of the exact
paths, principals and rights BEFORE execution. Present that proposed change with this
report; do not treat an installation command as approval. Never broaden the approved
change to C:\, C:\Users, existing PostgreSQL ACLs or protected services.

## 1. Copy and verify

Copy the immutable package to `C:\SESS-Deploy\<source-head-sha>` and its separately
received MANIFEST SHA-256 receipt. Do not use D:/E:. In elevated Windows PowerShell:

```powershell
$package=Read-Host 'Full C:\SESS-Deploy\<source-head-sha> path'
$expectedManifest=Read-Host 'MANIFEST SHA-256 from laptop build report'
if ((Get-FileHash -LiteralPath "$package\MANIFEST" -Algorithm SHA256).Hash -ne $expectedManifest) { throw 'MANIFEST digest mismatch' }
& "$package\installer\tools\Verify-Package.ps1" -Root $package -ExpectedHead (Split-Path -Leaf $package) -ExpectedManifestSha256 $expectedManifest
if ($LASTEXITCODE -ne 0) { throw 'Package verification failed' }
```

Check every file hash passes; record MANIFEST HeadSha, Readiness and MigrationHead.
The candidate frontend uses development login: do NOT serve `web` yet. Follow
server-deployment.md step 1 to copy to the separate installed `C:\SESS\<sha>` tree
and establish `$install`, `$installer`, `$pg`, `Assert-Exit`. Verify the installed
copy BEFORE changing configuration. Preserve the original immutable package.
Check free C: capacity (25 GB desired reserve), ports 8443/8444 free, PostgreSQL on C:,
Defender current, RDP off and protected services unchanged. Do not repeat completed
server preparation. Record runtime versions and that `dotnet --list-sdks` is empty.

## 2. D4 — trust and certificates first

Perform server-https.md's local office CA and certificate procedure. Reuse the approved
office CA if one exists; otherwise create it on a separate maintained administration
machine. Keep its private key offline, never in this package/on the ERP server.
Issue two distinct server-auth leaves/keys: ERP 8443 and Keycloak 8444, both with IP SAN
192.168.68.130 (ports are NOT part of a certificate SAN). ERP uses the configured
unique DESKTOP-SPF5420 subject, password-protected PFX imported into LocalMachine\My.
Keycloak uses its PEM certificate chain and PKCS#8 key under C:\SESS-Identity\tls.
Exchange only the public root certificate with server/operator/client PCs; check its
fingerprint over an independent channel before trusting LocalMachine\Root.

Check valid chain/dates, IP SAN, server-auth EKU, distinct private keys, and ACLs:
Administrators/SYSTEM plus only the appropriate service's read access. Grant ERP
private-key read after its virtual service account exists in step 6. Record public
fingerprints/expiry and renewal owner. Never bypass TLS validation. HTTPS response
checks follow service startup below.

## 3. D2 — Java 17, private Keycloak database, Windows service on 8444

Follow server-keycloak-install.md sections in order, with these boundaries:

- Check the actual Java 17 binary with `java -version`; if absent install the approved
  Java 17 distribution on C: first. Do not change global JAVA_HOME or engineering Java.
- Stage pinned Keycloak 26.7.4 and Windows amd64 Commons Daemon 1.6.1 separately from
  the ERP package; verify the hashes in keycloak/server-prerequisites.json before use.
  No Maven, Node, Docker or .NET SDK. Record archive hashes/version commands.
- DBA creates only the dedicated `sess_keycloak` PostgreSQL login/database, using the
  guide's least-privilege SQL and interactive password prompt. Never reuse an ERP role,
  grant superuser or connect Keycloak to sess_nexa_erp_DEMO. Check owner/privileges and
  absence of ERP role memberships. Database files stay on C:.
- Create non-admin Windows service account SESSKeycloak, deny interactive/RDP login,
  grant Log on as a service, and apply the guide's C:\SESS-Identity ACLs. Put secrets
  only in the locally restricted config, not the package or command history.
- Configure server-keycloak.conf: HTTPS 8444, correct IP issuer, loopback PostgreSQL,
  pool max 10, management health on loopback 9000; process/service-local Java 17 and
  heap 128/512 MB. Run the documented `kc.bat build` (Java augmentation, not an SDK).
- BEFORE first start, perform step 4's offline realm import. Then resume D2: temporary
  bootstrap administrator through secure process environment; foreground readiness
  check; gracefully stop that new foreground process; install SESSKeycloak with delayed
  automatic startup, actual PostgreSQL service dependency, dedicated account and
  recovery configuration. Check service command/logon/Java environment before starting.
- Open only TCP8444 from 192.168.68.0/24. No new 5432, 8080 or 9000 LAN allow rule.
  Create and verify a named MFA-protected administration account before removing the
  temporary bootstrap admin; never leave a shared commissioning credential active.

Check SESSKeycloak Running, localhost:9000 health/ready UP, trusted HTTPS discovery
on 8444, no public management listener, correct service account and delayed-auto
configuration. Inspect new service logs for errors without exposing secrets. A later
approved reboot must witness no-login startup; do NOT reboot during this handoff.

## 4. D3 — Staff and Approvers, with wrapper callback

At step 3's offline-import point, import the two packaged realm JSONs per
server-keycloak-realms.md with `--override=false`. Stop if either realm already exists;
never overwrite a live realm. After step 3 starts the service, inspect both realms:

- Realm IDs staff / approvers (display Staff / Approvers), exact HTTPS issuer on 8444.
- Public clients nexaerp-staff / nexaerp-approvers; standard flow, S256 PKCE required;
  direct password, implicit and service-account grants disabled.
- Add **http://127.0.0.1:8765/callback/** to BOTH clients' Valid Redirect URIs, in
  addition to the exact packaged SPA redirects. No wildcard and no extra web origin.
  This is a callback on the employee's own computer. The JSON exports do not already
  contain this addition. Record the post-import settings check.
- Preserve nexaerp/access scope, nexaerp audience and azp client binding. Approvers'
  browser flow must require password AND OTP, without a weaker client override.

Check each discovery issuer/JWKS through trusted HTTPS from the ERP server. Record
redacted realm/client/flow receipts. Defer other named employee enrolment, authentication
witnesses and wrapper rehearsal. Only SESS-12 verification in step 7 is in this handoff.

## 5. Create DEMO, provision, migrate, reconcile and verify

Execute server-deployment.md steps **2 and 3 only**, with `$db='sess_nexa_erp_DEMO'`.
Check it does not already exist; if it does, STOP without dropping/reusing it. Create
its empty advance schema. Use secure process-only credentials, no transcript:

1. Set exact ExpectedDatabase and DBA Installer connection. Provision principals;
   create passwords only if none of the four roles exists. All-four reuse preserves
   passwords; mixed state refuses and must not be repaired by hand.
2. Set migration connection as nexa_erp_migration with `Options=-c role=nexa_erp_owner`.
   Run `$install\migrate\efbundle.exe`; never `dotnet ef` or SQL migration hand-editing.
3. Run Installer `database-principals provision`, then `database-principals status`.
   Check **RECONCILED**, **VERIFIED**, 130 migrations/head matching MANIFEST and ZERO
   stock movements with the included read-only verification query.
4. Clear process credentials. Keep the migration/provision/status outputs and exact
   database/system identifier; check no SDK installation was needed.

Do not create sess_nexa_erp, restore any dump, run legacy items, seed business masters,
post ceremonies, GRNs, issues or adjustments in this handoff.

## 6. DEMO API Windows service on 8443; health only

Use server-deployment.md step 4's **service configuration/install portion**, deferring
its bootstrap command to step 7 below. Merge service-settings.example.json with
Authentication/Reporting from authentication.server.keycloak.json into installed
api/appsettings.Production.json. Both database fields must be sess_nexa_erp_DEMO;
connection uses nexa_erp_runtime only, pool max15/min0. Keep real Staff/Approvers HTTPS
issuers/audience/azp; only Approvers has MfaGuaranteedByProvider=true after the D3 flow
check. No development flags, even false. Restrict config ACLs as documented.

Install SESSNexaERP delayed-auto under NT SERVICE\SESSNexaERP, recovery configured.
Inspect `sc.exe qc SESSNexaERP`; verify quoted paths and PostgreSQL automatic/running.
Grant this service read access to the ERP private key; then start it. Leave wwwroot
empty: no candidate frontend copy. Apply only step 5's scoped ERP TCP8443 firewall
rule, not its frontend copy or demo instructions; inspect address/port filters.

Check Running and trusted HTTP200 from /health/live, /health/ready and /health/db at
https://192.168.68.130:8443. An unauthenticated protected API call must return 401.
Check health from another authorised office PC after trusting the root. Record the
service identity/target database, restricted firewall scope, health receipts and that
protected services are unchanged. Health is not a login or business acceptance witness.

## 7. D5 — signed roster, verified SESS-12, bootstrap, STOP

Before ANY scope entry, including bootstrap's built-in two-company scopes, obtain the
TD-signed COMPLETE [scope roster](setup-data/scope-roster.html). No signed roster:
stop here and report awaiting approval; never infer consent from a blank form.
Follow server-identity-mapping.md steps 3-4 only: create SESS-12 in Staff, require own
password change and verify immutable provider subject from a successfully verified
OIDC login. This one-person identity verification is required for bootstrap; other
employees and wrapper rehearsal come later. If SESS-12 cannot personally verify login,
report pending bootstrap rather than inventing/impersonating a subject.

Set exact DEMO ExpectedDatabase and securely prompt for the nexa_erp_bootstrap
connection, then use the published Installer:

```powershell
$env:NexaErp__ExpectedDatabase='sess_nexa_erp_DEMO'
Set-SecretEnvironment 'ConnectionStrings__NexaErpBootstrap' 'DEMO bootstrap connection (nexa_erp_bootstrap)'
$subject=Read-Host 'Verified immutable SESS-12 Staff subject'
& $installer authentication-bootstrap --issuer 'https://192.168.68.130:8444/realms/staff' --subject $subject
Assert-Exit
Remove-Item Env:\ConnectionStrings__NexaErpBootstrap
```

Check **COMPLETED**, both-company bootstrap/audit receipts and exact approved scope.
Ordinary replay refuses; documented --rerun is identical-result verification only.
Do not run a development bootstrap command. Do not create any further mappings/scopes.

**STOP HERE AND REPORT.** Keep DEMO intact. No demo walk/drop, production database,
opening ceremonies, business imports, stock movements or wrapper rehearsal. Report:
package SHA/MANIFEST digest; actual host/IP; public certificate fingerprints/expiry;
Java/Keycloak/runtime/PG versions; realm/callback/MFA checks; principal RECONCILED and
VERIFIED; migration count/head; API health/401 and DEMO target; signed roster reference;
SESS-12 COMPLETED (or precise pending prerequisite); service startup configuration;
protected-service comparison and no D:/E: use. Reboot/login/employee/wrapper/business
witnesses remain explicitly pending. End RESULT_REPORTED_PENDING_WITNESS.

