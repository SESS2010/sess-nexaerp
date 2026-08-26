# Authentication bootstrap steps 7-12

## Implemented contract

- The one-time installer command accepts an exact OIDC issuer and subject. It does not contact the provider.
- The ceremony enables login only for `SESS-12` / `SURANTHER P`, creates the `IT_MANAGER` assignment, and refuses any partial or replayed state. It does not bootstrap `SESS-01` or `SESS-02`.
- SESS-12 is a shared employee identity with two company-specific identity mappings and two company-specific `IT_MANAGER` assignments. Existing company-correct operational scopes are verified, not recreated.
- Company switching is explicit: the access token's `organization_id` (or `org_id`) selects one company mapping. A token without that claim, or naming a company without an exact active mapping, is denied.
- Provider role and group claims are never read for ERP authorization. Effective roles, department, company assignment, operational scope, and page permissions come from PostgreSQL.
- `/api/v1/system/database-model` is mapped only in Development.
- `/api/v1/audit/history` requires authentication, a resolved employee operational scope, `audit.history` / `view-audit-history`, and returns only `COMPANY` audit rows for the selected company.
- `GET /api/v1/session/me` returns the resolved employee, selected company, primary department, effective ERP roles, issuer, and subject.

## Ceremony command

Run schema/security installation first. Do not run this against `postgres`, `template0`, `template1`, or an owner connection.

Set these process environment values without putting the connection string on the command line:

```text
ConnectionStrings__NexaErpBootstrap=<connection as nexa_erp_bootstrap>
NexaErp__ExpectedDatabase=<exact customer database name>
```

Then run exactly once:

```text
SESS.NexaERP.Installer authentication-bootstrap --issuer <exact-issuer> --subject <exact-SESS-12-sub>
```

The command requires PostgreSQL 17+, the expected non-administrative database, the `advance` schema, the dedicated `nexa_erp_bootstrap` session principal, and the reviewed bootstrap function. Success returns a JSON witness with employee, role, company, mapping, and scope counts. Replay is refused.

## Cognito values required before the ceremony

Obtain these after creating the user pool and SESS-12 user:

1. **Exact issuer**: copy the token's `iss` claim or the `issuer` value from the pool's OIDC discovery document. For an original issuer it is `https://cognito-idp.<region>.amazonaws.com/<user-pool-id>`; an updated issuer uses `https://issuer-cognito-idp.<region>.amazonaws.com/<user-pool-id>`. Preserve case and otherwise pass the value exactly; a terminal slash is normalized away.
2. **Exact subject**: copy SESS-12's `sub` attribute from **Cognito > User pools > <pool> > Users > <SESS-12 user>**, from a verified signed token, or from `ListUsers`. Do not use username, email, or `cognito:username`, and do not assume the subject has a particular UUID format.
3. **API audience/resource identifier**: choose a stable HTTPS identifier for this API, for example `https://api.example.invalid/nexaerp`. Configure it as the Cognito resource-server identifier and as API setting `Authentication__Audience`.
4. **Issuer runtime setting**: configure the API setting `Authentication__Authority` with the exact issuer from item 1.
5. **Company selector claim**: issue one exact `organization_id` (or `org_id`) in each access token. For SESS-12 it must identify whichever of the two companies was selected for that session. This is routing context only; the database mapping remains authoritative.

The Cognito app client for a browser/native client must be a **public client with no client secret**, with only the **authorization-code grant**, **PKCE using S256**, exact pre-registered HTTPS callback and sign-out URLs (`http://localhost` only for local testing), and the intended identity provider enabled. Allow `openid` plus the NexaERP resource-server scope required by the client. Do not enable implicit or client-credentials grants for the human client. The authorization request must include the API resource identifier in `resource`; Cognito then places that identifier in the access token `aud` claim. Send the access token, not provider role/group claims, to the API.

Entra ID uses the same runtime contract: its authority/issuer and API audience are configuration, and its immutable `iss` + `sub` identity maps to ERP records. Any Entra roles/groups are likewise ignored.

## Expected database effects

For the settled two-company SESS-12 seed, a successful ceremony produces:

- 1 bootstrap singleton changed from `PENDING` to `COMPLETED`;
- 1 employee login enabled (`SESS-12`);
- 2 employee identity mappings, one per company, sharing the same issuer and subject;
- 2 `IT_MANAGER` role assignments, one per company;
- 2 company audit rows;
- 2 existing SESS-12 company operational scopes verified, with 0 new scope rows;
- 0 identities or roles created for `SESS-01` and `SESS-02`.

The migration contains the PostgreSQL cluster guard in both `Up` and `Down`. Validation uses disposable PostgreSQL only; no migration or ceremony was applied to an owner database.
