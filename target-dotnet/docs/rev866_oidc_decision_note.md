# REV866 OIDC Decision Note

REV866 keeps production authentication on permanent JWT/OIDC-compatible authorization. No temporary header authentication is registered in Development, UAT, or Production runtime code. Test-only authentication remains confined to the automated test project.

## Supported Permanent Options

| Option | Employee login | Customer/vendor external login | MFA | Scalability | Administration | Hosting responsibility | Cost implication | Integration complexity |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Microsoft Entra ID / Entra External ID | Strong fit for company users and Microsoft ecosystem | Strong external identity support through Entra External ID | Mature MFA and conditional access | Managed cloud scale | Centralized Microsoft admin model | Microsoft-managed | Subscription/license dependent | Medium |
| Keycloak/self-hosted OIDC | Strong fit when SESS wants full identity control | Supports separate customer/vendor realms or clients | Supported, but SESS must operate it | Depends on SESS hosting design | SESS-owned admin and maintenance | SESS/AWS-owned | Infra and support cost | Higher |
| Standards-compliant managed OIDC provider | Good if provider supports enterprise login needs | Depends on provider B2B/B2C features | Usually available by plan | Managed cloud scale | Provider-specific | Provider-managed | Plan/provider dependent | Medium |

## Current Decision

No production identity provider is selected or configured in REV866. Management/provider decision is still required for:

- OIDC authority
- API audience
- Client IDs
- User and external portal tenant/realm model
- MFA policy
- Customer/vendor onboarding policy
- UAT and production secret-store locations

## Production-Readiness Blocker

Real external OIDC token testing remains pending until management approves the identity provider and supplies the real authority/audience configuration through a secure configuration channel. This blocker must stay open before production deployment.
