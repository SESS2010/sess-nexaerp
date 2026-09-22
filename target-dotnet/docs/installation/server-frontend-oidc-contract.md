# D1: production frontend contract, DESKTOP-SPF5420

Frozen 21 September 2026. This is the frontend handoff; no development authentication
is permitted. No API code changes are required for this contract.

| Setting | Exact value |
|---|---|
| ERP origin/API base | https://192.168.68.130:8443 (API paths remain /api/v1/...) |
| Staff authority/issuer | https://192.168.68.130:8444/realms/staff |
| Staff public client ID | nexaerp-staff |
| Approvers authority/issuer | https://192.168.68.130:8444/realms/approvers |
| Approvers public client ID | nexaerp-approvers |
| Requested scopes | openid nexaerp/access |
| Redirect URI, both clients | https://192.168.68.130:8443/oidc/callback |
| Post-logout redirect URI, both clients | https://192.168.68.130:8443/oidc/logout-callback |
| Allowed web origin, both clients | https://192.168.68.130:8443 |
| API access-token audience | nexaerp |
| Company request header | X-NexaERP-Company |

Discover endpoints from <authority>/.well-known/openid-configuration. Exact issuer
matching includes scheme, IP, port and realm; no trailing slash on these issuers.
Port 8444 is reserved for this design and must pass the server free-port check.
No client secret belongs in this public SPA. Do not request profile, email or
offline_access; ERP supplies employee display details after mapping.

## Login and token lifecycle

1. Show Staff and Approvers sign-in choices. SESS-01, SESS-02 and SESS-14 MUST use
   Approvers with password + TOTP; any current TD/MD/Accounts Manager/CFO role uses
   Approvers. Provider choice does not grant roles. SESS-12 bootstraps through Staff.
2. Use authorization code + S256 PKCE through a maintained OIDC library. Generate and
   validate state and nonce; bind callback to the initiating provider/client and PKCE
   verifier. Full-page redirect, no embedded password form or password-grant request.
3. Exchange the one-use code at that realm's discovered token endpoint with the exact
   redirect URI, client ID and verifier. Validate the OIDC response with the library.
   Remove code/state from the address bar immediately. Reject unexpected callback
   state, issuer or return URL. Saved return paths must be relative ERP paths.
4. Keep access/refresh/ID tokens in memory; never localStorage, URLs, logs or reports.
   Only short-lived transaction state/nonce/verifier may use sessionStorage across the
   redirect, deleted on completion/failure. Page reload performs a new authorization
   redirect; Keycloak's own session can satisfy it. No silent iframe dependency.
5. Send ACCESS token only as Authorization: Bearer <token> to this ERP origin. Never
   send ID/refresh tokens to ERP; do not attach tokens to arbitrary URLs. Refresh at
   the provider before expiry, single-flight. Access lifetime 900 seconds; session
   idle/max 28800 seconds. Refresh failure clears session and offers login.
6. Logout through the discovered end_session_endpoint, id_token_hint and the exact
   post_logout_redirect_uri plus state. Clear local tokens/company/caches even if
   Keycloak is down; explain that provider logout could not finish. Validate returned
   logout state. Switching realm/account clears all old identity/company data first.

## Company selection and existing API contract

Offer these two requested company codes, never employee IDs or database names:
SESS_PVT_LTD (Sri Easwari Scientific Solution Private Limited),
SESS_PROPRIETORSHIP (Sri Easwari Scientific Solution). Pick explicitly after sign-in
(or retain within that same live authenticated session). Send ONE header value, e.g.
X-NexaERP-Company: SESS_PVT_LTD, on GET /api/v1/session/me and subsequent API calls.
Do not invent a companies-discovery endpoint. These are candidate choices, not granted
memberships: the server checks the exact issuer/sub mapping, company assignment,
active login, department and effective ERP roles every request.

Only show the workspace after session/me returns 200. Its PascalCase fields include
EmployeeId, EmployeeCode, EmployeeName, CompanyId, OrganizationId, DepartmentId,
DepartmentCode, RoleCodes, Permissions, IdentityIssuer, IdentitySubject,
FullAuthorityRoleCodes. Use this response for display/menus; client JWT decoding is
not permission authority. On company switch, cancel requests, clear company-scoped
caches, send the new header and fetch session/me before loading that workspace.

## Errors: exact current wire casing

Protected endpoints with missing, expired, malformed, wrong-issuer/client/audience or
wrong-token-use tokens return HTTP 401, application/problem+json, e.g.:

```json
{"Type":"https://api.sess.example/problems/authentication-required","Title":"Authentication required","Status":401,"Code":"AUTHENTICATION_REQUIRED","Detail":"A valid OIDC bearer token and active employee identity are required.","TraceId":"<request trace>","Errors":{}}
```

401 is JSON, not an HTTP redirect to login. Refresh once if possible; otherwise show
login. Retry a safe read at most once; never silently replay a stock/approval/write
command (preserve its original idempotency key and ask the user to check its outcome).
A valid token with absent/inactive/ambiguous identity or invalid company selection is
403 Code=EMPLOYEE_ACCESS_NOT_CONFIGURED. A governed role without trusted MFA is
403 Code=MFA_REQUIRED. Ordinary missing operation permission is 403
Code=PERMISSION_DENIED. Show Detail and TraceId, do not loop refresh on 403. The auth
403 envelope does NOT promise AdministratorActionRequired; branch on Code.

## Frontend acceptance handoff

Deliver production build/source SHA. Prove both realm PKCE callbacks and logout,
refresh expiry/failure, required OTP for all three named approvers, both companies,
company cache isolation, unmapped/revoked users, MFA_REQUIRED and permission refusal.
No /api/v1/dev/* calls, local token storage, wildcard callbacks or embedded credentials.
The server demo remains blocked until that build and trusted HTTPS/Keycloak are ready.
Backend source references: Security/OidcAccessTokenConfiguration.cs,
Middleware/EmployeeIdentityResolutionMiddleware.cs, Middleware/StandardErrorEnvelopeMiddleware.cs,
Serialization/ApiJsonContract.cs, Application/Identity/IdentityContracts.cs.

Operational deployment follows [mandatory server boundaries](server-protection.md).
