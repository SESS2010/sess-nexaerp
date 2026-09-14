# Item 16 verification

Parent commit: 2f0a007 (the existing three-day plan commit after 255af75). Backend implementation and local provider verification completed on 12 September 2026. Owner database unchanged. Production Cognito onboarding remains for Tuesday.

## Latest rerun - 15 September 2026

The real retained Keycloak container passed again against the current backend code: Release 1/1, zero failed/skipped, 130.532 seconds process wall time; Debug 1/1, zero failed/skipped, 95.017 seconds. Preceding complete project builds passed with zero warnings/errors (Release 34.06 seconds, Debug 124.42 seconds). TRX: local-evidence/overnight-20260914/keycloak-final-release.trx and keycloak-final-debug.trx.

The witness verifies browser authorization-code/S256 PKCE login, actual password-plus-TOTP challenge for Approvers, configured OIDC issuer/JWKS/client/claim bindings, restricted runtime database access, Staff and Approver employee mappings, wrong-pool MFA_REQUIRED, unmapped/revoked identity refusal, company selection, and rejection of ID tokens and tampered signatures. Application authentication code is unchanged by this rerun. The generic validator works with the local provider; real Cognito onboarding is still separate.

Current routine regression for the same application changes: Release 836/836, zero failed/skipped, 23m42s, report5-routine-release.trx. Earlier focused bootstrap/migration corrections passed 15/15 in each configuration. A new full Debug suite is not claimed. After the provider witnesses, both default test binaries were rebuilt without dependencies (Release 10.78 seconds, Debug 9.35 seconds, zero warnings/errors); all eight witness gates are off, discovery excludes the Keycloak test and retains the canonical purchase flow. The already-verified application assemblies were unchanged.

The expired disposable certificate was renewed and independently verified with its explicit CA and IP identity: TLS1.3, verification OK. Validity: 14 September 19:03:24 UTC through 21 September 19:03:24 UTC. SHA-256: C72513E288C2850120A1D420E0E94E5B7DD385933BDCA04DDA14FFE9A00D2578. Current retained container: d93baee93c6ef0e23cb9f089f07270027d811d303c41aa88e168f0851711b7ad, using the same fixture image listed below. Old test TLS material was archived; the active key is mode0600 for the container's UID1000. No production trust setting, provider policy, VM disk or native PostgreSQL data was replaced.

This is backend provider verification, not go-live approval. The live-name migration chain remains blocked at CommandReceiptReplay after the supplied 78-state history; see tuesday-migration-chain-witness-2026-09-14.md. Report10 still needs its delivered-machine prerequisite. Production frontend PKCE and identity-administration screens remain integration work, and Cognito needs the real pool/client/domain/subject configuration and onboarding witness. An eight-hour refresh token does not provide eight hours offline.

Expected production business-row changes from this verification commit: zero. No new frontend route, field or envelope changes. Existing frontend requirements and the single forwardable Suranther block remain in item-16-frontend-contract.md and suranther-tuesday-click-list.txt. Governing records reread: item-16-authentication-plan, authentication-bootstrap, mandatory-mfa-role-promotion, frontend contract and the final overnight instruction, with the frozen schema and Answers_To_Open_Questions taking precedence.

## Earlier observed results

- Release and Debug builds with the real-container witness enabled: zero warnings and errors.
- Full Release regression: 824 passed, 0 failed, 0 skipped, 30m11s. Includes the real container witness, retained creator-history amendment and complete purchase workflow.
- Follow-up Release runtime-principal test: 1 passed, 0 failed, 0 skipped, 1m13s. This strengthens an existing test after the full run; application code is unchanged. It verifies session_user=nexa_erp_runtime and exercises creation, revocation, duplicate/stale refusal, resolution and atomic audit-failure rollback with Installer-provisioned permissions.
- Selected Debug regression: 58 passed, 0 failed, 0 skipped, 7m11s. Includes OIDC validation and MFA, bootstrap foundation and ceremony, development identity/password-removal checks, receipt authorization, the strengthened runtime-principal test, real Keycloak and the full three-approval-band purchase flow. This is a targeted Debug run, not a full Debug suite claim.
- Earlier focused Release checks: 32 passed, 0 failed, 0 skipped, 5m31s. Includes 30 authentication/configuration cases, governed identity PostgreSQL operations with migration Down/reapply, and the full three-band flow with changed creator login subjects.
- Standalone real Keycloak container API witness: 1 passed, 0 failed, 0 skipped, 1m37s. Also passed within the final Release and selected Debug runs.

The Keycloak witness performs browser authorization code with S256 PKCE for Staff, and password plus TOTP for Approvers. It uses the production OIDC validator, restricted runtime database principal, actual employee mappings and session endpoint. Assertions passed for normal Staff access; mandatory-role Staff-pool refusal with 403 MFA_REQUIRED; Approver access after revoke/new mapping; missing or unauthorized company, unmapped and revoked identity refusal; ID-token and tampered-signature refusal.

## Database evidence

All tests use disposable PostgreSQL 17 clusters. Creation and revocation execute through the endpoint methods under the restricted runtime database principal; the real-container session test additionally exercises the HTTP middleware.

Observed rows: 22 inactive development mappings with 22 retirement audits; 2 active first-administrator mappings with 2 bootstrap audits; explicit rerun mutations 0; one runtime employee mapping retained inactive after create/revoke, with one creation audit, one revocation audit and two controlled-history rows. An injected audit failure left no new mapping. Nonexistent/inactive employees, duplicate active mappings, stale revocation versions, ordinary bootstrap replay and changed bootstrap subjects were refused.

The migration's Down and reapply passed before bootstrap. Existing purchase and qualification creators continue to resolve through retained mapping history to the same employee after pool migration. The complete purchase flow exercised identity changes in qualification, RFQ and PO creation and passed all three approval bands, retaining creator self-approval restrictions.

## Container provenance and reproducibility

Base image: quay.io/keycloak/keycloak:26.7.3
Resolved base digest: sha256:ff4257d0d64efbe99ed1ddfaf07765cc3c36dc7518bf8324d41961327f441c54
Witness container: 81502f8dcd62631c6951297300277950cfa6c75499c37fbb6b30877369cdfd24
Fixture-configured image: sha256:722e94bbce9af36011fd78da1ba2aeac2cdf5bfb545dcc05fef197beaeead25b

The configured image was committed from the stock image after its own augmentation and an offline import of the checked-in disposable realms. No custom Keycloak provider or application authentication branch was added. This Windows host has no Docker engine; a checksum-verified portable QEMU guest runs Alpine Linux and Docker, with HTTPS forwarded only to host loopback. A test-only backchannel pins the disposable TLS certificate. Production validation still requires trusted HTTPS. The development-mode provider container is an integration fixture, not a production Keycloak deployment recipe.

Initial witness attempts found fixture-only defects: raw versus Base32 OTP secret encoding, an omitted access-token subject mapper, and overly broad manually seeded test-principal membership. The corrected witness uses the explicit stable-subject mapper and Installer-created restricted principals. Final API witnesses passed in Release and Debug. Interrupted overlapping Debug builds are not counted; the subsequent isolated Debug build passed.

Reproduce with tools/Invoke-KeycloakWitness.ps1 on a local Docker Linux engine, or use the documented already-running container mode. This PC used the latter mode; the wrapper itself was syntax-checked. The explicit witness build fails when its URL/certificate configuration is missing; ordinary builds exclude this external-runtime test. Raw TRX evidence is under local-evidence/item16 (not committed).

## Deployment and frontend boundary

The on-premises provider path has been demonstrated against real Keycloak using the same application code with configured issuer, discovery/JWKS endpoints, client and claim bindings. Cognito resources and production subjects are unavailable until Tuesday, so Cognito deployment remains a configuration and onboarding witness. Both example profiles now use the same validated company-selection header.

Frontend contract changes: production authorization-code/PKCE flow and provider selection; configured company header; 401 for invalid tokens; 403 MFA_REQUIRED for mandatory roles through Staff; 403 EMPLOYEE_ACCESS_NOT_CONFIGURED for valid unmapped/inactive access; governed POST identity revocation with Version and Remarks. The current development frontend still requires these production changes. Details: item-16-frontend-contract.md.

An eight-hour refresh lifetime does not provide eight offline hours: refresh requires the provider. Existing tokens work only for their remaining 15-minute lifetime plus 30-second validation skew, with usable cached signing keys and current ERP access.

Operational handoff: suranther-tuesday-click-list.txt and mandatory-mfa-role-promotion.md. Promotion refuses all ERP access in the affected company through Staff; revoke/create replaces the login mapping while retaining history. SURANTHER or another authorized ERP identity administrator performs the mapping handover, coordinating with the Cognito administrator and the separate role-approval process.

Continue with Item 15 reports.

RESULT_REPORTED_PENDING_WITNESS
