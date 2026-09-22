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

1. Complete D2-D4. The production frontend contract gates browser rollout, not the
   authorised backend-only DEMO handoff in server-agent-demo-start.md. In the installed API copy,
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
   The reviewed employee-authenticated setup wrapper now creates each approved missing
   employee mapping; no identity-management screen is assumed. Existing API, not database SQL:

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


## D5 completion for assisted setup (21 September decision)

The Installer fully covers **SESS-12 bootstrap only**, including its two company mappings
and governed IT authority/scopes. It does not create every Keycloak account, map all
employees or prove every operator has usable department/warehouse scope. The earlier D5
text mentioned assignments but omitted an explicit operational-scope completion step.
Use this order in DEMO; repeat mappings/scopes for clean production without restoring DEMO:

1. Server identity maintainer completes D2-D4, trusts CA on operator PCs and enables only
   the exact additional CLI loopback callback described in setup-operator-wrappers.md.
   Creates named Staff/Approvers accounts; verifies sub, password change and required OTP.
   D5's audience/client/scope validation remains unchanged. No direct password grants.
2. Server DBA/operator runs the published Installer as nexa_erp_bootstrap with the exact
   confirmed DEMO/production target, for verified SESS-12 issuer/sub. Retain COMPLETED and
   its verification receipts; use only documented identical-result verification on replay.
3. SESS-12 signs in as themselves. IT reconciles the approved named setup roster against
   active employee, company, department and role assignments in each company. TD/MD,
   Accounts, Stores, Purchase and QC must have their actual effective ERP roles. Neither
   Keycloak groups nor the mapping wrapper can grant roles or repair inactive assignments.
4. IT uses Identities Read, then Identities Create plans only for missing approved mappings,
   through the wrapper as SESS-12. Subjects come from verified provider users, never guessed
   usernames. Read back recipient/issuer/company/dates and subject hash. Do not enable
   ordinary transaction users before BOTH ceremonies.
5. IT uses Scopes Read for each operator/company and compares existing effective scope
   with their active department assignment. Create only missing, approved scope through
   Scopes Create. DepartmentCode is required; cross-scope privilege stays false. A setup
   department scope with no warehouse restriction is broader than a warehouse scope:
   approve it explicitly, and if temporary set EffectiveTo=2026-09-30. This enables initial
   warehouse creation before warehouse IDs exist. After warehouses/racks exist, add only
   the approved operational restrictions/periods for use from 1 October.
6. Each real operator signs in separately and proves session/me plus allowed and refused
   company/department operations. A mapping row alone is not proof of scope. Retain this
   operator-access matrix before either opening ceremony. Repeat it after scope dates or
   roles change, without stock-moving probes on production.

**Remaining boundary:** operational-scope API currently offers create/list, not a general
edit/revoke endpoint. Adding a narrower row does NOT remove an existing broader effective
scope. If an existing broad/incorrect scope needs removal, or an employee assignment/role
is wrong, this wrapper/Installer does not fix it. Stop and resolve through the applicable
governed administration workflow (or a separately reviewed backend change if none exists),
never SQL or a fictitious narrower-row fix. Do not call D5 fully accepted until every named
operator's actual scope passes the matrix. Fresh correctly assigned operators can be
covered by the explicit mapping/create-scope sequence above.

[Executable wrapper and rehearsal](setup-operator-wrappers.md);
[identity/scope input columns](setup-data/README.md).

## Mandatory signed scope roster before entry (22 September)

Use [the one-page roster](setup-data/scope-roster.html). TD completes and signs the
FULL roster before ANY scope is entered, including the Installer-created SESS-12
bootstrap authority in BOTH companies. Record the Installer scope preview/description
on the form; never treat bootstrap as an exception to approval. No signature: stop
before authentication-bootstrap. The complete roster may use numbered signed pages.

One row per employee/company/department/warehouse restriction and effective period.
Write ALL explicitly for unrestricted warehouses, never leave it ambiguous. Record
OwnRecordsOnly, cross-scope privilege (false for these wrappers), effective dates and
any rack restriction on the form continuation. TD must explicitly approve temporary
setup scopes that cover all warehouses before warehouse IDs exist. A later narrower
scope does not cancel a broad scope; initial and later dated scopes must both appear
on the approved roster. Read existing scopes, compare exactly, enter missing approved
rows ONCE, then read back and retain IDs/version/receipt and second-person check.
After an uncertain response, read back before any retry. Do not blindly resubmit.

The Installer covers SESS-12 only. Named employee mappings, active role/company/
department assignments and per-person scope/access witnesses remain later D5 work.
No scope edit/revoke endpoint exists. Wrong scope: STOP; do not use SQL or add a
narrower row as a purported fix. Revoke endpoint estimate after go-live: 3-5 engineering
days for authorised versioned revocation, audit/history, live request enforcement and
security/regression tests; another 0.5-1 day for an operator wrapper/documentation.
This is an estimate, not implemented functionality.
