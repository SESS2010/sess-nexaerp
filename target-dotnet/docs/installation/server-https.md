# D4: one HTTPS trust plan for ERP and Keycloak

> **Protected server rule (21 September, 16:01):** NEVER stop, disable, modify or remove
> SESS_SQLEXPRESS, TEW_SQLEXPRESS, SQLBrowser, their ~77 SOLIDWORKS project databases,
> ewserver, ANY NI or Siemens service, ANY Rockwell FactoryTalk service, IIS Default
> Web Site or /Updater. Leave Wamp stopped/manual. Windows 10 stays; NEVER a clean
> Windows install; NEVER install a .NET SDK on this server. All application here is
> by the server agent, not from the laptop. See C:\SESS-ServerPrep for completed preparation.

These rules apply to every linked deployment, identity, certificate and backup procedure. Stop and report a conflict; do not reclaim ports or memory from protected services.

Use ONE private office certification authority trusted by the server and all eleven
PCs. Issue TWO server-authentication leaf certificates with separate keys: ERP8443
and Keycloak8444. Both must contain **IP Address SAN 192.168.68.130** (not a DNS SAN
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
   These are issuance requirements for the maintainer's CA tooling, not a claim a CA
   or certificate has already been issued.
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
   review renewal overlap before restarting Kestrel. Manage Private Keys -> grant
   Read to NT SERVICE\SESSNexaERP, Administrators/SYSTEM retained, no Users grant.
   Do not change certificates/bindings of IIS/NI/Siemens/Rockwell.
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
[Windows certificate store import](https://learn.microsoft.com/en-us/windows-server/identity/ad-cs/manage/import-export-certificates).
