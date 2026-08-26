# Authentication bootstrap installation order

Use this order for every customer installation. Database-principal provisioning and the one-time employee bootstrap are separate security boundaries; neither is performed by the API at startup.

## 1. Provision database principals

Run the Installer as an explicit PostgreSQL 17+ superuser against the exact customer database:

```text
SESS.NexaERP.Installer database-principals provision
```

Supply the connection through `ConnectionStrings__NexaErpInstaller`, the exact database name through `NexaErp__ExpectedDatabase`, and the initial migration, bootstrap, and runtime passwords through `NEXAERP_MIGRATION_PASSWORD`, `NEXAERP_BOOTSTRAP_PASSWORD`, and `NEXAERP_RUNTIME_PASSWORD`.

This creates `nexa_erp_owner`, `nexa_erp_migration`, `nexa_erp_bootstrap`, and `nexa_erp_runtime`; transfers ownership; installs least-privilege grants; and reconciles the exact bootstrap ceremony function grant if that function already exists.

Inspect the actual state at any time with:

```text
SESS.NexaERP.Installer database-principals status
```

Status prints one `ROLE_STATUS` line per managed role, including existence, login and prohibited cluster attributes, ceremony-function presence, and whether that role has an explicit ceremony execution grant. Partial role sets are refused and the missing roles are named.

## 2. Run migrations through the migration-owner path

Connect as `nexa_erp_migration` and explicitly `SET ROLE nexa_erp_owner` for the reviewed migration operation. Do not run customer migrations as `postgres`, the API runtime principal, or the bootstrap principal.

The authentication ceremony migration supports a development database where all four managed roles are absent: it always revokes execution from `PUBLIC` and leaves the function inaccessible. It refuses a partial managed-role set. Customer installations must still provision the complete principal topology first.

## 3. Configure the OIDC provider

Configure a standards-compliant OIDC issuer, API audience, public authorization-code client with PKCE, exact callbacks, and the selected `organization_id` or `org_id` claim. Provider group and role claims do not grant ERP authority.

Record the exact issuer and the immutable `sub` for `SESS-12` / `SURANTHER P`. Provider setup details and Cognito reference values are in [the steps 7-12 ceremony report](../../outputs/authentication_bootstrap_steps_7_12.md).

## 4. Run the one-time authentication bootstrap

Connect through `ConnectionStrings__NexaErpBootstrap` as `nexa_erp_bootstrap`, set `NexaErp__ExpectedDatabase`, and run:

```text
SESS.NexaERP.Installer authentication-bootstrap --issuer <exact-issuer> --subject <exact-SESS-12-sub>
```

The command is single-use. It bootstraps only SESS-12, verifies both company assignments and operational scopes, and refuses replay or partial identity/role state. SESS-01 and SESS-02 are added later through the normal authenticated API.

## Development-only ceremony without persistent principals

A Debug build provides `authentication-bootstrap-development` for an isolated local development database whose four managed principals are all absent. It requires `DOTNET_ENVIRONMENT=Development`, `NexaErp__AllowDevelopmentAuthenticationBootstrap=true`, `ConnectionStrings__NexaErpDevelopmentBootstrap`, and the exact `NexaErp__ExpectedDatabase`.

The command requires a PostgreSQL 17+ superuser that owns the selected database and `advance` schema. In one transaction it creates a temporary `NOLOGIN nexa_erp_bootstrap` role, grants only schema usage and execution of the existing production ceremony function, assumes that identity, calls the unchanged function, restores the original session authorization, revokes the grants, and drops the role. Failure rolls back the ceremony and temporary role together. The command refuses any managed principal, including a partial principal set.

This command is compiled only in Debug. It is absent from Release binaries. A Release build refuses to start the Installer if `NexaErp__AllowDevelopmentAuthenticationBootstrap` is present at all, including when its value is `false`.

> **Never promote, restore, clone, or otherwise use a development database whose one-time authentication ceremony has been consumed as the basis of a customer database. Build the customer database through the production installation sequence below.**

## Mandatory customer deployment checklist

Complete and witness every item before a real customer deployment:

1. Build and deploy Release artifacts only.
2. Remove every development exemption and development-bootstrap setting, including `DatabaseSecurity:AllowDevelopmentSuperuser` and `NexaErp__AllowDevelopmentAuthenticationBootstrap`.
3. Provision all four managed principals: `nexa_erp_owner`, `nexa_erp_migration`, `nexa_erp_bootstrap`, and `nexa_erp_runtime`.
4. Store and rotate the migration, bootstrap, and runtime login credentials through the deployment secret manager.
5. Transfer database, `advance` schema, table, sequence, and function ownership from the provisioning administrator to `nexa_erp_owner`.
6. Run Installer verification mode and witness the principal attributes, ownership, memberships, least-privilege grants, and ceremony-function ACL.
7. Run every migration through `nexa_erp_migration` using the reviewed migration-owner path; do not migrate as `postgres`, the API runtime principal, or the bootstrap principal.
8. Configure the API to connect exclusively as `nexa_erp_runtime` and verify that production startup rejects `postgres`, any superuser, and any database or schema owner.
9. Configure and validate the standards-compliant production OIDC provider, exact issuer, audience, authorization-code client with PKCE, callbacks, and organization claim.
10. Obtain the immutable production OIDC `sub` for SESS-12 and run the one-time ceremony while connected as `nexa_erp_bootstrap`.
11. Verify SESS-12 identity mappings for both companies, IT_MANAGER assignments, operational scopes, bootstrap state, audit records, and `/api/v1/session/me`.
12. Add SESS-01 and SESS-02 afterwards through the normal authenticated API; do not add them through bootstrap.
13. **Never promote, restore, or clone a development database whose authentication ceremony has been consumed into any customer environment. Create and migrate the customer database through this production sequence.**
