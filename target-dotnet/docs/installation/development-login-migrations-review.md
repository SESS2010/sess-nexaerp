# Development-login migrations: what they leave, and whether any of it can authenticate

Asked by the Technical Director, 24 September 2026, as a go-live security question for the
30 September build. Answered **from the migration source and the authentication code, not
from the route list.** Nothing here was run against any database. Every statement cites the
file it rests on.

## Answer

**Inert on a Release build, on a fresh database and on a migrated one. No new migration is
needed before 30 September.** A Release API accepts only RS256/384/512 tokens signed by the
configured Keycloak realm's keys and carrying that realm's exact issuer. Nothing these
migrations leave is a credential, and nothing a development command ever wrote can produce
such a token.

## What the four migrations leave in a freshly migrated database

| Migration | Source | Effect | Left behind |
|---|---|---|---|
| `20260829122646_DevelopmentLoginPasswords` | creates `advance.development_login_passwords` (`Id`, `EmployeeId`, `PasswordHash`, audit columns) | a table, no rows | **nothing**: dropped two migrations later |
| `20260831150840_RemoveDevelopmentLoginPasswords` | `DropTable("development_login_passwords")` | the table and any rows in it are gone | **no table, no hashes** |
| `20260907114500_EnableWorkflowDevelopmentLogins` | `UPDATE advance.employees SET "LoginEnabled"=true` for SESS-02, -14, -15, -16, -25, -33, -35, -41; refuses unless all 8 exist | a flag | `employees.LoginEnabled = true` on 8 rows, `UpdatedBy='migration-workflow-development-logins'` |
| `20260907220500_CompleteSeededWorkflowDevelopmentLogins` | same, for SESS-01, -04, -12 if ACTIVE; refuses unless all 3 | a flag | `LoginEnabled = true` on 3 rows |

**Net result:** there is no password table, no password hash, no token, no key and no
identity mapping. The only residue is `employees.LoginEnabled = true` on 11 employees.
`LoginEnabled` is not a credential. It is one of several eligibility conditions checked
**after** a token has already been validated.

No migration creates an identity mapping on a fresh database either.
`MultiCompanyEmployeeAuthorizationPart1Sql` only copies mappings that already exist (from
company …0001 into …0002), and a fresh database has none to copy. `GovernedAuthenticationRuntimeSql`
defines the SESS-12 bootstrap function but inserts no mapping when it is migrated.

## Can any of it authenticate a request on a Release build? The path, in order

1. **Development login is not in the binary.** `DevelopmentTokenService.cs` and
   `DevelopmentAuthEndpoints.cs` are wrapped in `#if DEBUG` whole. In Release there is no
   `/api/v1/dev/token`, no symmetric development key, and no type to register.
2. **Its switch cannot even be present.** `Program.cs` lines 36-42: in Release, if
   `NexaErp:AllowDevelopmentAuthentication` exists at all, even as `false`, the API throws at
   startup. The installer does the same for `NexaErp__AllowDevelopmentAuthenticationBootstrap`
   and `NexaErp__AllowDevelopmentWorkflowIdentities`, and its two development commands are
   `#if DEBUG` too (`Installer/Program.cs` lines 12-35).
3. **The only authentication registered is OIDC.** `OidcAccessTokenConfiguration.cs`:
   - one JwtBearer scheme per configured provider;
   - `ValidateIssuer = true` with `ValidIssuer` = that realm's authority (line 127-128);
   - `ValidAudience` = `nexaerp`;
   - `RequireSignedTokens = true` and `ValidateIssuerSigningKey = true`, with keys only from
     the realm's HTTPS JWKS;
   - `ValidAlgorithms` limited to RSA.

   A token with any other issuer goes to the reject scheme ("The bearer token issuer is not
   configured").
4. **Only then is the employee resolved.** `EfEmployeeIdentityResolver.ResolveAsync` needs
   exactly one active, effective `employee_identity_mappings` row for the **validated**
   issuer + subject + requested company. After that it checks `LoginEnabled`, ACTIVE status,
   one active company assignment and one primary department. `LoginEnabled = true` with no
   mapping yields *"No active employee identity mapping exists."*

**So what rejects a seeded "credential"?** There is none to reject. The password table was
dropped. The `LoginEnabled` flags are unreachable without a Keycloak-signed token for a subject
that has an approved mapping, and those are created only by the governed SESS-12 bootstrap
and the Step 6 mapping procedure.

## Fresh database versus one migrated from an earlier state

**Fresh (the Option C go-live database):** exactly the residue above, the `LoginEnabled`
flags and nothing else. The Debug-only installer commands never run against it, because the
go-live package is a Release build, and step 2 refuses the settings they need.

**Migrated from an earlier state** (for example the frontend developer's `sess_nexa_erp`):
two extra things can exist.

- **Password hashes from 29-31 August:** gone. `RemoveDevelopmentLoginPasswords` dropped the
  table with its rows. `DevelopmentLoginPasswordRemovalTests` pins that the model no longer
  has the entity.
- **Development identity mappings**, if the Debug-only `workflow-identities-development`
  command was ever run there: 22 rows (11 employees × 2 companies) with
  `Issuer = 'urn:nexaerp:development'` and `Subject` = the employee code (for example
  `SESS-15`). **Inert on Release.** Step 3 cannot validate a token whose issuer is
  `urn:nexaerp:development`, because no provider has that authority, JWKS or key. The mapping
  is never reached. **What removes it:** the governed authentication bootstrap run with
  *retire development* (`GovernedAuthenticationRuntimeSql`, lines 132-150). It deactivates every
  active `urn:nexaerp:development` HUMAN mapping and writes a `DevelopmentIdentityRetired`
  audit row for each. `GovernedAuthenticationRuntimeTests` expects 22 such rows inactive
  afterwards.

**A new migration before 30 September: not needed.** The go-live database is fresh, so none
of the migrated-database residue exists there. Where it does exist, Release cannot reach it,
and a governed, audited retirement is already built. A migration deleting the rows would only
remove audit history of an inert record.

## One observation that is not a hole

On a fresh database, 11 named employees start with `LoginEnabled = true` because of these
migrations. That grants nothing by itself, since each still needs a Keycloak account and an
approved mapping. But it means "login enabled" on day one reflects a September development
list, not a go-live decision. Whether those 11 should stay enabled is a roster question for
the Technical Director at Step 6. The switch is the governed employee endpoint
(`EmployeeEndpoints`, *Activate / Deactivate login*), not a migration.
