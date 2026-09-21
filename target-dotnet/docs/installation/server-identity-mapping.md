# D5: API trust configuration and employee identity ceremony

> **Protected server rule (21 September, 16:01):** NEVER stop, disable, modify or remove
> SESS_SQLEXPRESS, TEW_SQLEXPRESS, SQLBrowser, their ~77 SOLIDWORKS project databases,
> ewserver, ANY NI or Siemens service, ANY Rockwell FactoryTalk service, IIS Default
> Web Site or /Updater. Leave Wamp stopped/manual. Windows 10 stays; NEVER a clean
> Windows install; NEVER install a .NET SDK on this server. All application here is
> by the server agent, not from the laptop. See C:\SESS-ServerPrep for completed preparation.

These rules apply to every linked deployment, identity, certificate and backup procedure. Stop and report a conflict; do not reclaim ports or memory from protected services.

Perform in DEMO first and again for fresh Option C production after DEMO acceptance.
Do not restore DEMO into production. Do not alter employee subjects when changing
ERP databases. Keycloak's sess_keycloak database persists independently of DEMO.

1. Complete D2-D4 and the production frontend contract. In the installed API copy,
   merge Authentication and Reporting from authentication.server.keycloak.json into
   appsettings.Production.json; preserve Kestrel, runtime-only ConnectionStrings and
   exact ExpectedDatabase. Staff issuer ends /realms/staff with nexaerp-staff client;
   Approvers ends /realms/approvers with nexaerp-approvers. Both use audience nexaerp,
   azp client binding, typ=Bearer, sub, scope nexaerp/access, MappedHeader and
   X-NexaERP-Company. Only Approvers has MfaGuaranteedByProvider=true, justified by
   D3's inspected mandatory flow. Never set it true on Staff. Use runtime pool max15,
   min0. Keep config/credentials Administrators/SYSTEM + ERP service read only.
2. Confirm discovery issuer and jwks_uri exactly match each configured value using
   trusted HTTPS from the SERVER. Never enable development flags (even false),
   disable issuer/audience checks, inject subjects or accept ID tokens. Restart only
   SESSNexaERP after the complete configuration; check health and 401 without token.
3. Create SESS-12 in Staff, temporary password/change-on-login. Record Keycloak user
   ID and confirm the same sub in a successfully verified OIDC login. Never use the
   username/email as sub; never paste tokens into a public decoder. Record employee
   code, issuer, sub, administrator/date and company approval, not password/token.
4. With target ERP DB migrated/reconciled/verified, use the published Installer on
   the server. Set NexaErp__ExpectedDatabase to the exact DEMO (later production)
   name and securely set process ConnectionStrings__NexaErpBootstrap to a connection
   as nexa_erp_bootstrap, never runtime/postgres. Then:

   ```powershell
   & $installer authentication-bootstrap --issuer 'https://192.168.68.130:8444/realms/staff' --subject '<verified-SESS-12-sub>'
   if ($LASTEXITCODE -ne 0) { throw 'Authentication ceremony failed; do not repair by SQL' }
   Remove-Item Env:\ConnectionStrings__NexaErpBootstrap
   ```

   Check COMPLETED and the bootstrap/audit receipts. It handles SESS-12 for both
   companies and the governed IT-manager setup. Ordinary replay refuses; --rerun
   only verifies an identical completed result. No --retire-development is needed
   for an empty fresh customer DB; never run any *-development command.
5. SESS-12 signs in through the production SPA, selects each company and checks
   GET /api/v1/session/me, EmployeeCode=SESS-12, expected OrganizationId and authority.
   The frontend's governed identity-management screen (or a reviewed authenticated
   API client) now creates each employee mapping. Existing API, not database SQL:

   POST /api/v1/rev869a/configuration/employee-identities
   Authorization: Bearer <SESS-12-access-token>
   X-NexaERP-Company: SESS_PVT_LTD

   ```json
   {"OrganizationId":"SESS_PVT_LTD","Issuer":"https://192.168.68.130:8444/realms/approvers","Subject":"<verified-user-sub>","EmployeeCode":"SESS-01","IdentityType":"HUMAN","EffectiveFrom":"2026-09-21","EffectiveTo":null,"Remarks":"Approved initial production identity mapping; receipt reference"}
   ```

   Use the actual approved effective date (example is not an instruction to backdate).
   Check 201, mapping ID/history/audit and correct company. Repeat separately for
   SESS_PROPRIETORSHIP only where approved. Create SESS-02/SESS-14 with their own
   Approvers subjects; other employees use their verified realm/subject. Never reuse
   one person's subject for another, grant roles from Keycloak groups, or bulk-enable
   all users before the opening-stock gate. Employee login, effective company and
   department assignments and ERP role activation must also be valid.
6. Verify each enabled employee's session/me and permitted/refused operations in
   each approved company. An unmapped/revoked user must get 403; a privileged Staff
   identity must get MFA_REQUIRED. Current active mappings are checked on subsequent
   requests even for already-issued tokens. Revoke through the existing versioned
   API before replacing an identity; preserve history, never repoint it by SQL.
7. Production: keep access limited to named setup/Stores/Accounts/TD ceremony operators
   until BOTH opening ceremonies are posted. No GRN, issue, return or adjustment on
   sess_nexa_erp before that. Only then admit the other approved mappings/users.

The configuration adds no API field or route. It binds the existing validator and
ceremonies to the D1 issuers. Deployment acceptance still requires the frontend's
production source/build and field witnesses; no login is claimed installed here.
