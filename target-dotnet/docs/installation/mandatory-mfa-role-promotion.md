# Promotion to a mandatory-MFA role

Applies to TECHNICAL_DIRECTOR, MANAGING_DIRECTOR and ACCOUNTS_MANAGER. Example: an approved promotion of SESS-15 to ACCOUNTS_MANAGER.

## What happens if the pool is wrong

The identity provider can accept the Staff password, but ERP refuses all authenticated ERP access in a company where the employee's effective ERP roles require MFA. It returns HTTP 403 with Code MFA_REQUIRED. It does not allow ordinary screens and block only the final approval. Existing Staff tokens are checked against current ERP roles on subsequent requests; their remaining token lifetime is not an exemption.

Company authorization remains company-scoped. During a person's promotion, move every active company identity to the Approvers provider so the person has one working login and the Staff account can be disabled. No provider group grants a role.

## Identity history

An existing mapping is never repointed to a different issuer or subject. Revoke it with its current Version and a promotion/change reference in Remarks. Then create the new mapping using the verified Approvers issuer and subject. Both identities still refer to the same permanent EmployeeId. Existing documents, approval history and employee relationships remain attached to that employee.

Revocation is a runtime administrator operation, not a migration. The old mapping remains inactive; the new mapping has its own Id. A stale Version refuses. No delete, silent subject rewrite or temporary MFA bypass is used.

## Who performs the handover

SURANTHER P, SESS-12, operates the Cognito account setup using his AWS administrative access and the identity handover using his ERP IT_MANAGER permission. Another authorized ERP identity administrator can perform it. Cognito administration alone cannot alter ERP mappings or roles. An ERP identity administrator without AWS rights must coordinate with the Cognito administrator. Neither task bypasses the separate governed ERP role-assignment approval process. An administrator cannot revoke their own mapping through this route.

## Procedure before the promotion becomes effective

1. Record the approved role change, effective company/date, employee code and responsible administrators. Check all company memberships requiring handover.
2. Cognito administrator creates the employee in Approvers using the same employee code as the username, verifies their personal email and records the new immutable subject.
3. Employee changes the temporary password and enrolls their own TOTP authenticator. Verify a fresh password-plus-TOTP sign-in. End enrollment sessions before activating ERP access. Never share a phone or copy a TOTP secret.
4. Schedule a short handover interruption and coordinate the role's effective activation through its existing governed workflow. If the role becomes effective first, Staff-based ERP access fails closed until the remaining steps finish.
5. ERP IT_MANAGER lists the employee's existing company mappings. For each, POST /api/v1/rev869a/configuration/employee-identities/{identityId}/revoke with Version and Remarks containing the promotion reference.
6. For each authorized company, POST /api/v1/rev869a/configuration/employee-identities with the new verified issuer/subject, existing employee code, company, HUMAN identity type, effective dates and the same promotion reference. This does not create or approve any role.
7. Employee signs in freshly through Approvers. Verify /api/v1/session/me, employee identity, company, expected effective roles and authorized operations in each company.
8. Verify the old Staff token no longer reaches ERP operations. SURANTHER disables the old Staff account and revokes its provider sessions. Keep the old account/mapping records for audit.
9. Record the mapping IDs, completion time, both administrators if different, and successful/refused verification outcomes.

If any step fails, leave access refused and resolve the account/mapping problem; do not temporarily make MFA optional, reactivate the Staff mapping to bypass the role policy, or reset the bootstrap ceremony. A subsequent demotion does not require an immediate move back to Staff: the employee can continue using Approvers and MFA.


Cognito can issue tokens during first-time enrollment even in a required-MFA pool. Before activating a new approver mapping, complete TOTP enrollment, end/revoke enrollment sessions, wait 16 minutes so any enrollment access token expires including the API's 30-second clock skew, then verify a fresh password-plus-TOTP sign-in. Do not activate the mapping earlier. This is an onboarding procedure, not a change to token lifetime. Source: https://docs.aws.amazon.com/cognito/latest/developerguide/user-pool-settings-mfa.html .


## Documents already in progress

The purchase and vendor-qualification creator checks use retained mapping history to identify the original employee after a login handover. Old mappings remain inactive for authentication. Replacing a login does not make its original employee a different creator or remove creator self-approval restrictions. Legacy records that store only a login subject must resolve to one distinct employee within the company; ambiguous or missing creator history refuses and requires administrator reconciliation. Do not rewrite old document creators or reactivate a revoked login to work around that refusal.
