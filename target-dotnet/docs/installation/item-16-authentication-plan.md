# Item 16: authentication report before implementation

> 19 September implementation update: the new CHIEF_FINANCIAL_OFFICER role for inventory-period governance also requires mandatory MFA, independently of any MD role held by the same employee. The earlier three-role descriptions below predate this addition. Use the current [promotion procedure](mandatory-mfa-role-promotion.md).

> 15 September owner update: Item 15 is witnessed complete and ERP migrations 78 to 103 are applied, RECONCILED and VERIFIED. SESS now selects local Keycloak; retain Cognito for other customers. Earlier Cognito-waiting and migration-blocked statements below are historical. Current deployment, backup and recovery work is tracked in [local Keycloak operations](item-16-local-keycloak-operations.md). Pool promotion instructions apply equivalently to Staff/Approvers realms, preserving revoke/create mapping history.

Prepared against HEAD 2f0a007, 12 September 2026. This is the original design report. Implementation and the latest passing Release/Debug Keycloak reruns are recorded in item-16-verification.md (15 September). No owner database changes. The protected-name migration blocker must be resolved before Tuesday deployment; the verification document records that boundary.

## On-premises alternative

Yes: a customer-hosted OIDC provider such as Keycloak needs no AWS account. Run the provider on local HTTPS with durable signing keys, provider database backups, DNS and synchronized clocks. This conforms to frozen authentication decision 6: OIDC only; provider-controlled passwords, reset, lockout and MFA; issuer plus subject is the durable key; provider groups never grant ERP authority.

References: https://www.keycloak.org/server/configuration-production and https://www.keycloak.org/securing-apps/overview .

## SURANTHER's setup and handoff

Create separate test/production Cognito user pools, administrator-created users, a managed-login domain, and a public SPA app client without a secret. Enable authorization code with S256 PKCE. Register exact HTTPS callback/logout URLs, no wildcards. Proposed frontend paths are /auth/callback and /signed-out; SESS must provide actual hosts and the frontend must implement the paths.

Proposed policy (not AWS defaults or frozen ERP logic): access/ID tokens 15 minutes; refresh lifetime eight hours; minimum 14-character passwords with upper/lowercase, number and symbol; temporary password expiry one day and first-login change; optional TOTP MFA in Staff and required TOTP MFA in Approvers for TECHNICAL_DIRECTOR, MANAGING_DIRECTOR and ACCOUNTS_MANAGER; administrator-managed recovery; no public signup. Provider lockout remains active. Configure an API resource-server scope restricted to this client. Use access tokens, not ID tokens, for API calls.

Return region, pool ID, exact issuer/discovery URL, client ID, login domain, API scope, actual callbacks/logout URLs, adopted policies and verified employee-code-to-subject roster, starting with SESS-12 / SURANTHER P. Do not send passwords, AWS secret keys, refresh tokens or private signing keys. Cognito validation must check signature, issuer, expiry, token_use=access, client_id and scope. Generic OIDC must validate API audience. Company selection remains checked against effective ERP identity mappings.

References: https://docs.aws.amazon.com/cognito/latest/developerguide/user-pool-settings-client-apps.html ; https://docs.aws.amazon.com/cognito/latest/developerguide/amazon-cognito-user-pools-using-tokens-verifying-a-jwt.html ; https://docs.aws.amazon.com/cognito/latest/developerguide/managing-users-passwords.html .

## Bootstrap ceremony

1. SURANTHER configures the provider, verifies SESS-12's immutable subject, completes enrollment appropriate to the chosen pool and confirms discovery and application URLs.
2. Provision four managed database principals through Installer using environment/secret-managed credentials.
3. Run guarded migrations as migration principal assuming owner; reconcile and verify principals. This session uses disposable clusters only.
4. Configure Release API with runtime principal and OIDC; remove development exemptions.
5. Set ConnectionStrings__NexaErpBootstrap and NexaErp__ExpectedDatabase. Run authentication-bootstrap --issuer <issuer> --subject <SESS-12-sub>.
6. In one transaction verify named employee, both company assignments, primary departments and scopes; create two production mappings, initial IT_MANAGER authority, audits and consumed singleton. Development mappings must be closed as retained history through an explicit governed transition, never relabelled.
7. Ordinary replay refuses. An explicit rerun flag should verify the identical consumed result only: no reset, subject change, restored revoked access or duplicated grants.
8. Sign in as SESS-12, check session and permissions in each company, then onboard other employees through runtime administration.

## Forty-two employees and development mappings

The development provisioner defines eleven people in two companies: 22 mappings. Issuer is urn:nexaerp:development, subjects are employee codes. These are not Cognito subjects. Close development rows as retained inactive history when transitioning; add verified production mappings. Never promote a database with a consumed development bootstrap to a customer.

Onboard 42 employees through governed runtime operations using their actual active company assignments. Count mappings by authorized employee/company memberships; do not assume 42 or 84. Refuse nonexistent/inactive/login-disabled employees, missing assignments, duplicate active identity keys and conflicting people. Issuer plus subject identifies a person; company-specific rows may represent that same person in both companies, as existing bootstrap requires. Revocation must retain history and require permission, Version and audit.

## Existing implementation and build gaps

Existing: dedicated Installer ceremony and singleton, company-specific runtime mapping creation, generic JWT bearer path.
Gaps found: all replays and pre-existing development mappings refused; no revoke route found; unmapped identity becomes generic unauthorized; Cognito client_id/token_use validation absent; Debug resolution can be selected by a claim without checking active development mode; creation audit occurs after SaveChanges without an enclosing transaction.

Retain Debug tokens behind #if DEBUG, Development environment and explicit opt-in, mutually exclusive with OIDC. Fix these gaps and report every frontend contract change. Do not store passwords or grant roles from provider claims.

## Provider outage

New login, refresh, recovery and MFA enrollment need the provider. Existing unexpired tokens may validate with cached usable keys and current ERP permissions. Cold startup/unknown signing keys fail closed. No Debug/local-password fallback. Provider-side revocation is not immediately established by offline JWT validation; ERP mapping revocation must affect subsequent requests.

Required messages: sign-in service unavailable for provider failure; sign-in required for invalid/expired tokens; distinct 403 administrator-action response for valid unmapped identities. Previously committed postings remain committed.

## Required evidence

Build before tests. Record Debug and Release counts; real isolated PostgreSQL bootstrap/create/revoke/refusal rows; signed-token issuer/client/audience/use/expiry and mode isolation tests; ordinary replay refusal and explicit identical rerun verification. Actual customer provider login/MFA requires SURANTHER's configuration. Report that separately; do not claim live login witnessed from local tokens.

## Accepted amendment: two pools and MFA by ERP role

The user approved two pools during implementation. The agreed role policy is reflected above. Staff has OPTIONAL TOTP MFA. Approvers has REQUIRED TOTP MFA and contains users holding TECHNICAL_DIRECTOR, MANAGING_DIRECTOR or ACCOUNTS_MANAGER. A resolved employee holding any of these roles is refused when authenticated by an optional-MFA trust profile. Provider groups do not grant ERP authority.

Cognito has no built-in per-group required-MFA policy. Its MFA enforcement is pool-wide; optional pools can enable MFA per enrolled user, which alone is not an enforced ERP role policy. Source: https://docs.aws.amazon.com/cognito/latest/developerguide/user-pool-settings-mfa.html .

Provider trust profiles configure issuer, discovery address, client, audience strategy, token-use claim/value, subject, organization and scope claims. JWKS URL is supplied explicitly as JwksAddress and must exactly match the configured discovery document's jwks_uri. No AWS hostname, pool ID, client ID or proprietary claim name is built into the validator. The generic MFA assurance profile must only be enabled for a provider whose sign-in policy actually guarantees MFA.

The required-MFA pool must have no federated identity providers and no remembered-device MFA bypass. Complete personal TOTP enrollment before mapping approvers, invalidate enrollment sessions and use a fresh MFA sign-in. Provider configuration and onboarding are security prerequisites; a configured boolean alone is not evidence of MFA. On promotion from Staff to an MFA-required role, optional-pool access fails closed immediately on subsequent requests; the administrator enrolls the person in Approvers, revokes their old company mappings and adds the verified new subjects. The ERP does not grant either pool's user a business role automatically.

## Offline correction

The accepted lifetimes remain 15-minute access/ID tokens and an eight-hour refresh token. A refresh token is usable only by contacting the identity provider. These settings DO NOT provide eight hours of offline operation. During a Cognito outage, a previously signed-in employee has at most the remaining access-token lifetime (plus configured validation clock skew), and only when the API has usable signing keys cached. An eight-hour offline ERP session would require a separate explicitly designed security/session mechanism or different access-token policy. Neither is silently introduced.

## Local Keycloak witness prerequisite

No docker or podman command, standard installation directory or service was detected on this Windows 10 Pro PC. WSL --list --quiet returned installation/help output rather than a usable distribution listing. Get-WindowsOptionalFeature requires OS administrator elevation unavailable to this shell. A checksum-verified portable QEMU VM runs Alpine Linux and Docker without changing Windows optional features or rebooting the PC. The real Keycloak 26.7.3 container API witness passed, including PKCE, password/TOTP, mapped access and refusal cases. See item-16-verification.md for provenance, counts and remaining full-regression checks.

Current frontend sign-in still calls /api/v1/dev/token; the frontend must implement the real authorization-code/PKCE callback and handle MFA_REQUIRED and EMPLOYEE_ACCESS_NOT_CONFIGURED responses. See item-16-frontend-contract.md for configurable company selection, including Cognito without custom access-token claims. See suranther-tuesday-click-list.txt for the single forwardable setup block. The new configuration examples are templates, not live deployment configuration.
