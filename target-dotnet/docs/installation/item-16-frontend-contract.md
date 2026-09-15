# Item 16 frontend and deployment contract

Configure every provider's issuer, discovery and JWKS URLs, client binding, token-use claim/value, subject claim and API scope. Discovery must advertise the exact configured issuer and JWKS URL. Example configurations contain deliberately unusable deployment placeholders.

For deployment, copy the edited Authentication section from the chosen example into the published API's appsettings.Production.json, or set the equivalent Authentication__Providers__... environment variables. Run the published Release API with Production as its environment. Keep the existing database connection and expected-database guard configuration. The example filenames are templates; the API does not automatically load files with those names. Remove NexaErp__AllowDevelopmentAuthentication entirely from Release configuration. Restart the API after changing provider trust configuration, then verify sign-in and refusals in each company.

## Company context

OrganizationSelectionMode=Claim reads the configured signed OrganizationClaim. OrganizationSelectionMode=MappedHeader reads a requested company from OrganizationHeader (the examples use X-NexaERP-Company). The header is not authority. Before any ERP endpoint executes, the API checks the verified issuer/subject against the selected company's active mapping, employee login status, company and department assignments, and effective ERP roles. Missing, ambiguous or unauthorized selection is refused. Changing a header cannot grant company membership.

The frontend keeps the chosen company in its authenticated session and sends it on each request. A cross-origin deployment must allow the configured header in its reviewed CORS policy. Cognito requires no company-claim customization with MappedHeader.

## Sign-in and errors

The current development frontend login is not production OIDC login. Implement authorization code with S256 PKCE, exact callback/sign-out URLs, provider selection and secure session/token handling. Send ACCESS tokens to the API, never ID tokens. Clear identity and company state when changing accounts. Refresh requires the provider.

Invalid, expired, wrong-client and wrong-token-use tokens receive 401. A valid identity without effective ERP access receives 403 EMPLOYEE_ACCESS_NOT_CONFIGURED with an administrator-action message. Current TECHNICAL_DIRECTOR, MANAGING_DIRECTOR and ACCOUNTS_MANAGER roles without a trusted MFA assurance receive 403 MFA_REQUIRED for ALL ERP access in the company. Mapping and role changes affect subsequent requests, including already-issued tokens.

## Governed identity administration

POST /api/v1/rev869a/configuration/employee-identities/{id}/revoke requires security.employee-identities:Deactivate. Body: {"version":0,"remarks":"Reason"}. Fetch the actual version first. Stale or already inactive mappings return 409. The current administrator cannot revoke their own mapping. Mapping changes, controlled history and audit commit atomically. Revoke then create a replacement; never re-point historical issuer/subject ownership to another employee.

Installer authentication-bootstrap takes --issuer and --subject. Ordinary replay refuses. --rerun verifies the identical completed result without changes or repair. --retire-development explicitly closes development history during initial transition and cannot be combined with --rerun. The runtime principal cannot execute the ceremony.

See [promotion procedure](mandatory-mfa-role-promotion.md), [local Keycloak operations](item-16-local-keycloak-operations.md) and the [forwardable Suranther list](suranther-local-keycloak-click-list.txt). The 15 September owner decision selects local Keycloak for SESS; the Cognito configuration path remains available for other deployments.
