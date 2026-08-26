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
