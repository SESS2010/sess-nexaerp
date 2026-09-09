# Developer database runtime principals

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

After an upgrade, run `database-principals status`. If an upgrade was
accidentally run as `postgres`, stop the API and run `database-principals
provision` once more as the installer administrator; its all-present replay
reassigns every current `advance` object and reconciles ACLs.

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

The following post-principal migrations create 34 relations in total:

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

Every one is correctly owned when the migration session acts as
`nexa_erp_owner`; every one lands under `postgres` when the documented
boundary is bypassed. `job_order_history` was not exceptional—it was simply
the object on which the drift became visible. The all-present provisioning
replay is idempotent and repairs the complete set, not only that table.

Migrations in the same period that create no relation can still create
functions. The installer replay enumerates and re-owns every function in
`advance`, then rebuilds the controlled-function ACL contract. A successful
`status` is required before the API is started.
