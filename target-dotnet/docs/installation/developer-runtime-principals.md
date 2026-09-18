# Developer database runtime principals

For a legacy installation before migration 75, first read [the REV869B upgrade privilege procedure](legacy-rev869b-upgrade-privileges.md). The ordinary sequence below does not authorize an unprivileged ownership transfer from retired roles. The owner-approved one-migration administrator exception is documented there.

This is the exact sequence for an existing, populated developer database after
REV869B retirement. The managed topology contains only:

- `nexa_erp_owner`: `NOLOGIN`; owns the database, `advance` schema, and application objects.
- `nexa_erp_migration`: `LOGIN`; may `SET ROLE nexa_erp_owner` for reviewed migrations.
- `nexa_erp_bootstrap`: `LOGIN`; restricted to the one-time authentication ceremony.
- `nexa_erp_runtime`: `LOGIN`; the only principal the API uses.

Do not create REV869B roles. Do not run the API or ordinary migrations as
`postgres`.

## 1. Back up before changing principals

Take both backups while connected as the current PostgreSQL administrator:

```powershell
pg_dump --format=custom --file .\sess_nexa_erp-before-principals.dump --dbname "host=127.0.0.1 port=5432 dbname=sess_nexa_erp user=postgres"
pg_dumpall --globals-only --file .\postgres-globals-before-principals.sql --host 127.0.0.1 --port 5432 --username postgres
```

The database dump protects all schema and business data. The globals dump
protects cluster roles and memberships. Store both files away from the database
disk and verify that they are non-empty. No command below changes business rows.

## 2. Inspect the current state

Set secrets only in the current process. Do not put them in command history,
source control, or command-line arguments.

```powershell
$env:ConnectionStrings__NexaErpInstaller = 'Host=127.0.0.1;Port=5432;Database=sess_nexa_erp;Username=postgres;Password=<administrator-secret>'
$env:NexaErp__ExpectedDatabase = 'sess_nexa_erp'
dotnet run --project .\src\SESS.NexaERP.Installer\SESS.NexaERP.Installer.csproj -c Release -- database-principals status
```

`NOT_PROVISIONED` with exit code 3 is expected when none of the four roles
exists. A partial set is a hard refusal; do not guess or create the missing
roles manually.

## 3. Provision and reconcile

Choose three independent passwords of at least 24 characters:

```powershell
$env:NEXAERP_MIGRATION_PASSWORD = '<new-migration-secret>'
$env:NEXAERP_BOOTSTRAP_PASSWORD = '<new-bootstrap-secret>'
$env:NEXAERP_RUNTIME_PASSWORD = '<new-runtime-secret>'
dotnet run --project .\src\SESS.NexaERP.Installer\SESS.NexaERP.Installer.csproj -c Release -- database-principals provision
dotnet run --project .\src\SESS.NexaERP.Installer\SESS.NexaERP.Installer.csproj -c Release -- database-principals status
```

Provisioning is one PostgreSQL transaction. It creates all four roles when all
are absent, transfers the database, schema, relation, sequence, and function
ownership to `nexa_erp_owner`, reconciles ACLs on every existing object, and
verifies the resulting contract. On replay it does not rotate passwords. It
does not insert, update, or delete any business row.

If the process or connection fails, PostgreSQL rolls the transaction back. Run
`status` again. Re-run `provision` only for an all-absent or all-present
state. A reported partial state requires investigation against the globals
backup; do not continue to the API.

## 4. Run future migrations as the migration principal acting as owner

The connection must authenticate as `nexa_erp_migration` and set the reviewed
owner role for that session:

```powershell
$env:ConnectionStrings__NexaErp = 'Host=127.0.0.1;Port=5432;Database=sess_nexa_erp;Username=nexa_erp_migration;Password=<migration-secret>;Options=-c role=nexa_erp_owner'
$env:NexaErp__ExpectedDatabase = 'sess_nexa_erp'
dotnet ef database update --project .\src\SESS.NexaERP.Infrastructure\SESS.NexaERP.Infrastructure.csproj --startup-project .\src\SESS.NexaERP.Api\SESS.NexaERP.Api.csproj --context NexaErpDbContext
```

Migrations do not remain on `postgres` after provisioning. PostgreSQL owns a
new table as the role executing `CREATE TABLE`; therefore a migration run as
`postgres` creates drift even when every grant is otherwise correct.

## Mandatory post-migration ownership and ACL reconciliation

After **every** migration run, keep the API stopped and run both commands below
through the PostgreSQL superuser installer connection. This is mandatory even
when the migration authenticated as `nexa_erp_migration` and acted as
`nexa_erp_owner`; `status` alone is not a substitute for reconciliation.

```powershell
$env:ConnectionStrings__NexaErpInstaller = 'Host=127.0.0.1;Port=5432;Database=sess_nexa_erp;Username=postgres;Password=<administrator-secret>'
$env:NexaErp__ExpectedDatabase = 'sess_nexa_erp'
dotnet run --project .\src\SESS.NexaERP.Installer\SESS.NexaERP.Installer.csproj -c Release -- database-principals provision
dotnet run --project .\src\SESS.NexaERP.Installer\SESS.NexaERP.Installer.csproj -c Release -- database-principals status
```

For an all-present principal set, `provision` prints `RECONCILED`: it does not
change credentials or business rows. In one transaction it reassigns every
current `advance` schema object to `nexa_erp_owner`, rebuilds the controlled
ACL contract, and verifies it. `status` must then print `VERIFIED`. If either
command exits non-zero, do not start the API and do not continue the upgrade;
investigate and rerun the complete reconciliation sequence after correction.

Opening-stock reconciliation retains runtime execution of the staging, count,
value-confirmation and authorization functions. The four opening-stock evidence
tables remain read-only for runtime; direct writes and execution of the private
validation/trigger helpers are refused. An incomplete opening-stock package or
a mismatched permission boundary makes provisioning/status fail. Reconciliation
must run after the opening-stock migration as well as after later migrations.
Item 25 regression: provisioning after the migration previously revoked opening
command execution (42501) and restored direct INSERT/UPDATE on its evidence
tables. The regression provisions twice, checks command owners, fixed search
paths and privileges after each call, then verifies one three-actor posting,
one landed FIFO layer, quantity 10 and the opening reports. Targeted results:
Release 1 passed, 0 failed (1m14.376s); Debug 1 passed, 0 failed (1m15.714s).
These ran within the two-test Item 25 batches, not new full-suite runs. Builds
passed with zero warnings/errors (Release 27.84s; Debug 4m24.24s). No schema,
migration body, API contract or owner database was changed.

## 5. Run the API as runtime

```powershell
$env:ConnectionStrings__NexaErp = 'Host=127.0.0.1;Port=5432;Database=sess_nexa_erp;Username=nexa_erp_runtime;Password=<runtime-secret>'
$env:NexaErp__ExpectedDatabase = 'sess_nexa_erp'
dotnet run --project .\src\SESS.NexaERP.Api\SESS.NexaERP.Api.csproj -c Debug
```

Keep the installer, migration, bootstrap, and runtime connection strings
separate. The API startup guard must continue to reject `postgres`, a
superuser, an owner, or an owner-role member.

## Ownership drift found in the current chain

The following post-principal migrations create 38 relations in total:

| Migration area | New relations |
| --- | ---: |
| Estimated BOM foundation and lifecycle | 6 |
| Production BOM and engineering documents | 6 |
| Material Issue | 4 |
| Material Return | 3 |
| Vendor Bill and FIFO costing | 6 |
| Job Order governance | 1 |
| Fitment and generated Actual BOM | 4 |
| FAT readiness | 3 |
| Item-company last-purchase cache | 1 |
| Immutable landed-cost adjustment ledgers | 4 |

Every one is correctly owned when the migration session acts as
`nexa_erp_owner`; every one lands under `postgres` when the documented
boundary is bypassed. `job_order_history` was not exceptional—it was simply
the object on which the drift became visible. The all-present provisioning
replay is idempotent and repairs the complete set, not only that table.

Migrations in the same period that create no relation can still create
functions. The installer replay enumerates and re-owns every function in
`advance`, then rebuilds the controlled-function ACL contract. A successful
`status` is required before the API is started.
