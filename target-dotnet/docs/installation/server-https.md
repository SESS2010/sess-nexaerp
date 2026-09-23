# D4: one HTTPS trust plan for ERP and Keycloak

> **Protected server rule (updated 22 September):** NEVER stop, disable, modify or remove
> SESS_SQLEXPRESS, TEW_SQLEXPRESS, SQLBrowser, their ~77 SOLIDWORKS project databases,
> ewserver, ANY Rockwell FactoryTalk service, IIS Default
> Web Site or /Updater. Leave Wamp stopped/manual. Windows 10 stays; NEVER a clean
> Windows install; NEVER install a .NET SDK on this server. All application here is
> by the server agent, not from the laptop. See C:\SESS-ServerPrep for completed preparation.

These rules apply to every linked deployment, identity, certificate and backup procedure. Stop and report a conflict; do not reclaim ports or memory from protected services.

Use ONE private office certification authority trusted by the server and all eleven
PCs. Issue TWO server-authentication leaf certificates with separate keys: ERP 8443
and Keycloak 8444. Both must contain **IP Address SAN 192.168.68.130** (not a DNS SAN
containing digits). A port does not belong in a certificate. A public CA will not issue
this private-IP identity. Do not use browser exceptions or disable .NET validation.

## Maintainer preparation, before either service is admitted

1. Use an existing managed office CA if available. Otherwise the nominated maintainer
   creates an offline SESS Office Root CA on a separate maintained machine, with an
   encrypted private key and offline recovery copy. Root: RSA3072/SHA256 or stronger,
   basicConstraints CA:true critical, keyCertSign/cRLSign; leaf: CA:false, digitalSignature
   and RSA keyEncipherment, EKU serverAuth, exact IP SAN. Root validity ten years;
   leaf validity one year, scheduled renewal at least 30 days before expiry. Record
   serials, expiry and SHA256 fingerprints. No root private key goes on this server,
   any user PC, Git or the deployment package. Use only the public root .cer for trust.
   The completed 22 September issuance and remaining offline-custody/field checks
   are recorded in the issuance receipt section below.
2. Issue ERP leaf with CN=DESKTOP-SPF5420 and Keycloak leaf with CN=SESS-Keycloak;
   both have the same required IP SAN and separate private keys. Supply ERP as a
   password-protected PFX with its chain; Keycloak as leaf+intermediate PEM chain in
   keycloak.crt and PKCS8 PEM keycloak.key. Keep any intermediate chain consistent.
   Package no private keys. Private backup of both leaves/configuration is required;
   encrypt the receiving backup disk and restrict its share because identity bundles
   also contain password hashes/TOTP secrets.
3. On the server import root/intermediates into Local Computer trusted root/intermediate
   stores. Import ERP PFX into Local Computer -> Personal, not the logged-in user's
   store. In certlm.msc inspect valid dates, Server Authentication EKU, IP SAN and chain.
   Ensure exactly one currently valid ERP certificate matches the configured subject;
   review renewal overlap before restarting Kestrel. After registering the ERP service, BEFORE its first start: Manage Private Keys ->
   grant Read to NT SERVICE\SESSNexaERP, Administrators/SYSTEM retained, no Users grant.
   Do not change certificates/bindings of IIS/Rockwell or other retained products. NI/Siemens were reported removed; do not reinstall.
4. Put Keycloak PEM files in C:\SESS-Identity\tls. Administrators/SYSTEM Full and
   SESSKeycloak Read only; no ordinary Users. D2 references those explicit files.
   No global Java truststore modification for engineering apps. The public server
   chain is sent by Keycloak; .NET API discovery/JWKS validates it through Windows
   Local Computer trust. Restart ONLY the affected ERP/Keycloak service after
   reviewed certificate replacement, then check both origins and discovery.

## Each office PC: a non-developer's checklist

The maintainer supplies ONLY SESS-Office-Root.cer (and any public intermediate), plus
its SHA256 fingerprint by a separate trusted route. Never accept a PFX/private key.

1. Ask the maintainer to confirm the certificate file/fingerprint is the approved one.
   The maintainer can run `certutil -hashfile SESS-Office-Root.cer SHA256`; compare the
   full value with the separate record. Do not accept a substituted certificate.
2. Double-click the .cer -> Install Certificate -> **Local Machine** -> approve the
   administrator prompt (call the maintainer if you do not have that permission).
3. Choose **Place all certificates in the following store** -> Browse ->
   **Trusted Root Certification Authorities** -> Next -> Finish. If an intermediate
   was supplied, install it in **Intermediate Certification Authorities**, not Root.
4. Close and reopen Microsoft Edge or Chrome. Visit https://192.168.68.130:8443 and
   https://192.168.68.130:8444/realms/staff/account. Both must open with trusted HTTPS,
   no warning/interstitial. Never click through a warning. ERP may still show its
   deployment gate until the frontend is ready; that does not waive the HTTPS check.
5. Report success with PC name/date/browser to the maintainer. If warned, stop and
   report the exact address and warning; the maintainer checks CA trust, clock, SAN,
   expiry and chain. Use the managed Edge/Chrome baseline for all eleven PCs; other
   browsers need an explicitly witnessed trust setup, not assumed Windows-store use.

Acceptance requires eleven recorded client checks, plus server-side Invoke-WebRequest
for ERP readiness and BOTH Keycloak discovery URLs without -SkipCertificateCheck or
custom validation callbacks. No service should depend on a logged-in user's trust store.

Sources: [Keycloak PEM TLS configuration](https://www.keycloak.org/server/enabletls),
[Windows certificate store import](https://learn.microsoft.com/en-us/windows-hardware/drivers/install/certificate-stores).

## D4 issuance receipt — 22 September 2026

Created on the separate administration laptop DESKTOP-AP, outside the repository.
Server transfer folder: `C:\SESS-CA\server-output`, exactly four files:
SESS-Office-Root.cer (public DER root), erp-DESKTOP-SPF5420.pfx (encrypted ERP
private key and public chain), keycloak.crt (PEM leaf/public chain), keycloak.key
(separate PKCS#8 private key, service-readable only). All four file hashes were
verified against `C:\SESS-CA\issuance-receipt.json`. No private keys enter Git or
the deployment package. The issuance validation checked chain, serverAuth, SANs,
distinct leaf keys, PFX recovery and exclusion of the root private key from ERP PFX.

Root SHA-256 fingerprint:
`A95109800BD04600670FAD6E4316996FEB5B153D03EA07493ED8BE691F796913`.
Compare this complete value on the server before trusting the root.

Both leaves are valid **22 September 2026 15:59:14 UTC through 22 September 2027
16:04:14 UTC** (21:29:14 IST through 21:34:14 IST respectively).
**Renew by 23 August 2027**, allowing thirty days for deployment and eleven-PC
checks. ERP subject CN=DESKTOP-SPF5420; Keycloak CN=SESS-Keycloak. Both include
DNS DESKTOP-SPF5420 and IP 192.168.68.130 SANs; keys are distinct. Certificates
identify hosts, not ports: use the ERP leaf on 8443 and Keycloak leaf on 8444.
The root expires 22 September 2036 16:04:14 UTC.

Root private export: `C:\SESS-CA\offline-root\SESS-Office-Root-PRIVATE.pfx`, encrypted
with the password entered locally at issuance. The laptop account has no Windows
password: **offline custody is still PENDING**. Follow `C:\SESS-CA\ROOT-OFFLINE.md`
to copy onto a different physical external disk, verify its SHA-256 against the
receipt, confirm the copy, and remove that exact local PFX. No root key was imported
into a Windows certificate store. Do not transfer the offline-root directory to
the server. File deletion is not a claim of forensic SSD erasure. Retain the
external encrypted root under TD-controlled custody and its password separately.

Give each office PC the [one-page trust instruction](office-pc-https-trust.md).
Certificate creation is complete; external-root custody, server import/service
ACLs and eleven-client HTTPS witnessing remain field actions.
