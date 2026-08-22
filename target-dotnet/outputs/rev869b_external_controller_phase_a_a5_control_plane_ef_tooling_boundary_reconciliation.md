# REV869B A5 Control Plane EF tooling-boundary architecture and package reconciliation

Date: 2026-08-22

Decision type: report-only architecture/package reconciliation

Authorized starting HEAD: `0cb692350a3afee12662856e5a1ed966029676a7`

Expected and observed parent: `2aa8106e697f360274a30386379aaa6a1c42583c`

Branch: `master`

## Decision

`A5_CONTROL_PLANE_EF_TOOLING_BOUNDARY_GATE=GO`

Option T1 is selected and reproducibly proven. The future
`SESS.NexaERP.ControlPlane.Persistence` class library is both the migrations target project and the EF design-time
startup project. It alone owns EF Core, EF Design, Npgsql, the Npgsql EF provider, its `DbContext`, design-time
factory, migrations, snapshot and project-local package lock. The production Control Plane executable remains the
runtime composition root and does not directly reference EF Design or Npgsql.

Option T2 is rejected. T1 succeeded without changing any verified package identity, package count, lock graph,
production package owner or project-reference direction.

This report resolves the tooling contradiction in blocker 3. It does not authorize source implementation. A separate
management authorization may now set:

`A5_REVISED_SOURCE_IMPLEMENTATION_GATE=GO`

for one bounded implementation using the already frozen 39-path allowlist and maximum 38-path outcome, subject to all
existing build, test, migration, package, security and 40-mutant gates.

## Stage 0

| Check | Observed result | Status |
|---|---|---|
| HEAD | `0cb692350a3afee12662856e5a1ed966029676a7` | PASS |
| Parent | `2aa8106e697f360274a30386379aaa6a1c42583c` | PASS |
| Subject | `REV869B Phase-A A5 source implementation blocker 3` | PASS |
| Branch | `master` | PASS |
| HEAD boundary | Exactly `outputs/rev869b_external_controller_phase_a_a5_revised_source_implementation_blocker_3.md` | PASS |
| Blocker SHA-256 | `78500D288AE265DF773407EB28DE5DDE58E0D896C2578C9529009104881B5B03` | PASS |
| EF package report SHA-256 | `6E33DB8F4866FA8692B318C4A112074C4B2B60EF1BA55F29B027FFBB721973F2` | PASS |
| Package evidence reconciliation SHA-256 | `42215A833682C8E8BBB2751B558B221EB90DEFA7CB457F6203B8A2E61D76EB68` | PASS |
| Target-scoped worktree/index | Clean | PASS |

Blocker 3, the persistence/classifier architecture freeze, dual-context migration/package decision, official EF
package-graph verification and package evidence-integrity reconciliation were read completely. The external
`legacy-reference` sibling was not queried, enumerated, opened or modified.

All previous implementation changes remain reverted. At entry and after graph inspection:

- `src/SESS.NexaERP.ControlPlane.Persistence/SESS.NexaERP.ControlPlane.Persistence.csproj` was absent;
- the Control Plane and ERP future `packages.lock.json` files were absent;
- no A5 migration, source implementation or implementation checkpoint remained; and
- target-scoped Git status was clean.

## Current committed graph

The committed production project-reference graph remains:

```text
ControlPlane -> ControlPlane.Contracts
AcceptanceVerifier -> ControlPlane.Contracts
Api -> Infrastructure -> Application -> Domain
Api -> Application -> Domain
Api -> Domain
```

Contracts and Domain remain leaves. Control Plane has no EF/Npgsql package. ERP Infrastructure owns its existing ERP
EF/Npgsql provider graph. API owns its existing ERP design-time package for the current ERP startup boundary. No
production project references a test project. No central package management, repository NuGet configuration or
committed package lock currently exists.

## Frozen T1 ownership

The future implemented graph is unchanged from the dual-context freeze:

```text
ControlPlane executable
  -> ControlPlane.Contracts
  -> ControlPlane.Persistence
       -> ControlPlane.Contracts
       -> Microsoft.EntityFrameworkCore 10.0.10
       -> Microsoft.EntityFrameworkCore.Design 10.0.10 (PrivateAssets=all)
       -> Npgsql 10.0.3
       -> Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3

ERP Api
  -> ERP Infrastructure
       -> ERP Application
       -> Domain
       -> ControlPlane.Contracts
```

The Persistence project must retain this exact EF Design metadata:

```xml
<PackageReference Include=Microsoft.EntityFrameworkCore.Design Version=10.0.10>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
```

The owning type and source boundary are frozen as follows:

- project/assembly: `SESS.NexaERP.ControlPlane.Persistence`;
- context: `SESS.NexaERP.ControlPlane.Persistence.Rev869BControlPlaneDbContext`;
- factory type: `SESS.NexaERP.ControlPlane.Persistence.Rev869BControlPlaneDbContextFactory`;
- factory source path:
  `src/SESS.NexaERP.ControlPlane.Persistence/Rev869BControlPlaneDesignTimeDbContextFactory.cs`;
- interface: `IDesignTimeDbContextFactory<Rev869BControlPlaneDbContext>`;
- migration assembly and startup assembly for EF tooling: `SESS.NexaERP.ControlPlane.Persistence`;
- runtime composition owner: `SESS.NexaERP.ControlPlane` executable.

The factory constructs only `DbContextOptions` and the context. It must not build or start a host, resolve the Control
Plane executable, call `Database.OpenConnection`, query migration history, create/apply/remove a migration, or execute
SQL. Its design-only connection identity must be non-production and exact-name validated. Acceptance instruments
connection opening and requires zero attempts for every command below.

## Isolated T1 probe

The probe was created under a unique disposable system-temp directory outside the repository. It was a `net10.0`
class library (`OutputType=Library`) with exactly the four frozen direct package references and a
`Rev869BControlPlaneDbContextFactory` implementing
`IDesignTimeDbContextFactory<Rev869BControlPlaneDbContext>`. The same `Probe.csproj` was supplied as both `--project`
and `--startup-project`.

The design factory configured the control history as `control.__EFMigrationsHistory` and installed a
`DbConnectionInterceptor` counter on synchronous and asynchronous connection opening. The counter remained zero.
There was no `Program`, host builder, web SDK, service startup or executable entry point.

### Tool identities

| Tool/artifact | Version | Bytes | SHA-256 |
|---|---:|---:|---|
| `C:\Program Files\dotnet\dotnet.exe` | SDK `10.0.303` | 167,208 | `AB1B71FD3DD71062E074C9FAB8312081A81B7F2B3E0327C48C4D249C8D1A3135` |
| `C:\Users\User\.nuget\packages\dotnet-ef\10.0.10\tools\net8.0\any\dotnet-ef.dll` | `10.0.10` | 91,448 | `520513FA1B7AC3E6F4195CF3CEFDF9D2F50924750EB480E44583A30A69BD8D25` |
| `C:\Users\User\.nuget\packages\dotnet-ef\10.0.10\tools\net8.0\any\tools\net8.0\any\ef.dll` | `10.0.10` | 114,016 | `252596F74AE15A65ECBB9228063F6FBA3B1344F507AC306FC898BD7395428FFD` |
| repository `dotnet-tools.json` | pins `dotnet-ef 10.0.10` | 195 | `C50D950C1B480D932399A71066B159A54BB4EDBF59B25067743C01C9B47FBFA7` |

The probe invoked the pinned `dotnet-ef.dll` directly through `dotnet`; it did not restore or update a tool.

### Restore boundary

- local source: the previously verified 41-archive source;
- source count/bytes: `41` / `53,929,626`;
- authoritative source manifest SHA-256:
  `7BE5281B6DF17BAACC3EEC865312A18CB7C5137FAE65B67CB0F93E03650872CD`;
- probe NuGet configuration: `<clear />` plus only that local directory;
- probe configuration SHA-256: `9D0E3C88CA80F52CB31706B4415A7F7AD6095D084D739D8BC272438D4B02317C`;
- packages, HTTP cache and CLI home: isolated subdirectories under the disposable probe;
- HTTP/HTTPS proxy: unreachable loopback;
- NuGet audit: disabled only for offline replay;
- normal package cache: not used for project restore, modified or cleared;
- central package management: absent.

Initial local-only restore exited `0`. The generated Control lock contained exactly `35` package identities, was
13,361 bytes, and had SHA-256
`64DC53ED03457021DFCBC985D9C8C5C0468B82BB102BC8382C3D920827137AA6`, exactly matching the officially verified
Control lock. A second restore used `--locked-mode`, a fresh isolated packages/HTTP/CLI cache and the same sole local
source. It exited `0`; pre/post lock hashes were identical. Replay assets SHA-256 was
`4C9F3D71D031C2B5FD9ABF5701ABD51E2444B44496BD5982649712D218CFDF76`.

The first assets SHA-256 was `AA21AE2F8A68B4D8D3C3FD86EE5BC2185AAD76D28BF084A5E25FDB531D498E56`.
The difference is expected because assets encode cache paths; the lock bytes and package identities were unchanged.
Restore and EF output contained zero HTTP request/download indicators, and the only configured NuGet source was the
verified local directory.

### Probe command results

| Operation | Exact material command shape | Exit/result |
|---|---|---|
| Initial restore | `dotnet restore Probe.csproj --configfile NuGet.Config --packages <isolated> --no-http-cache --force -p:NuGetAudit=false -warnaserror` | `0` |
| Build | `dotnet build Probe.csproj --no-restore -warnaserror` | `0`, zero warnings/errors |
| Context discovery | `dotnet <dotnet-ef.dll> dbcontext info --project Probe.csproj --startup-project Probe.csproj --context Rev869B.ToolingProbe.Rev869BControlPlaneDbContext --configuration Debug --no-build` | `0` |
| Initial source generation | `dotnet <dotnet-ef.dll> migrations add Rev869BControlPlaneInitial ... --output-dir Migrations` | `0` |
| Post-generation build | `dotnet build Probe.csproj --no-restore -warnaserror` | `0`, zero warnings/errors |
| Migration listing | `dotnet <dotnet-ef.dll> migrations list ... --no-build --no-connect` | `0`, one migration |
| Up SQL | `dotnet <dotnet-ef.dll> migrations script 0 <initial-id> ... --no-build --output <temp-up>` | `0` |
| Down SQL | `dotnet <dotnet-ef.dll> migrations script <initial-id> 0 ... --no-build --output <temp-down>` | `0` |
| Snapshot parity | second disposable `migrations add SnapshotParityProbe ... --no-build`; require empty generated `Up` and `Down` | `0`; `0/0` body bytes |
| Locked replay | `dotnet restore Probe.csproj ... --locked-mode --force-evaluate` into a new isolated cache | `0`; lock unchanged |

One preliminary `dbcontext list --context` command exited `1` because `dbcontext list` does not support `--context`.
It was rejected before context construction and recorded zero database opens. The corrected `dbcontext info` command
above is the frozen context-discovery form. A combined batch containing `has-pending-model-changes` was rejected by
the execution safety layer before launch because that command has no explicit no-connect flag. It was not executed
and is not acceptance evidence.

`dotnet-ef 10.0.10` help independently proved that `migrations list --no-connect` is supported. Therefore this report
does not invent or use an unsupported option. The option is frozen only for `migrations list`, where it exists.

### Generated evidence

| Artifact | Bytes | Lines | SHA-256 |
|---|---:|---:|---|
| Probe initial migration | 1,198 | 39 | `6D365FA34330A6C640E9AB5065533021B4A5AAA370477B36279F229B06E60FF4` |
| Probe initial designer | 1,599 | 47 | `9EC8B09E95194D463A65B06B60DBC1B5BFE39F1C34B70CAA756551E33167FD9C` |
| Probe snapshot | 1,485 | 44 | `AA0CD7B3760C566499EB28053F1C53A8502A4D706E7A1CF2EED07C6D5E3414CB` |
| Probe Up SQL | 889 | 31 | `3B40768FCFC1312766743EFBB3E51775795E713B02071AF3F445B3A29D083482` |
| Probe Down SQL | 182 | 8 | `0159242E9C62978084E8E25896C3FCA7BAD39EA674B057ED24BFEE19D55D7BCB` |

Observed counters:

```text
project_restore_http_sources=0
project_restore_http_request_indicators=0
control_plane_service_starts=0
postgresql_connection_open_attempts=0
postgresql_connections=0
migration_applications=0
migration_removals=0
repository_migration_creation_attempts=0
```

Two migration sources were generated only inside the authorized disposable probe: the initial proof and the empty
snapshot-parity proof. They do not alter repository migration-attempt arithmetic. The probe contained 3,296 files
and 1,394 directories at cleanup and was permanently removed. Post-delete existence was `False`.

## Exact future Control Plane commands

Every command runs after a warning-as-error offline build and locked local-only restore. `<dotnet-ef.dll>` means the
exact pinned `10.0.10` payload above. The project and startup project are deliberately identical.

### Context discovery

```powershell
dotnet <dotnet-ef.dll> dbcontext info --project src/SESS.NexaERP.ControlPlane.Persistence/SESS.NexaERP.ControlPlane.Persistence.csproj --startup-project src/SESS.NexaERP.ControlPlane.Persistence/SESS.NexaERP.ControlPlane.Persistence.csproj --context SESS.NexaERP.ControlPlane.Persistence.Rev869BControlPlaneDbContext --configuration Debug --no-build
```

### Migration listing

```powershell
dotnet <dotnet-ef.dll> migrations list --project src/SESS.NexaERP.ControlPlane.Persistence/SESS.NexaERP.ControlPlane.Persistence.csproj --startup-project src/SESS.NexaERP.ControlPlane.Persistence/SESS.NexaERP.ControlPlane.Persistence.csproj --context SESS.NexaERP.ControlPlane.Persistence.Rev869BControlPlaneDbContext --configuration Debug --no-build --no-connect
```

`--no-connect` is included only here and is supported by the pinned tool, as proven by its help and successful probe.

### Migration creation/source generation

```powershell
dotnet <dotnet-ef.dll> migrations add Rev869BA4ControlPlaneInitial --project src/SESS.NexaERP.ControlPlane.Persistence/SESS.NexaERP.ControlPlane.Persistence.csproj --startup-project src/SESS.NexaERP.ControlPlane.Persistence/SESS.NexaERP.ControlPlane.Persistence.csproj --context SESS.NexaERP.ControlPlane.Persistence.Rev869BControlPlaneDbContext --configuration Debug --no-build --output-dir Migrations
```

The committed source must retain the separately frozen exact migration identity
`20260821093000_Rev869BA4ControlPlaneInitial`; source generation does not authorize a different committed identity.

### Model/snapshot verification

Run in a fresh disposable worktree of the exact candidate, never in the target worktree:

```powershell
dotnet <dotnet-ef.dll> migrations add __Rev869BControlPlaneModelSnapshotParityProbe --project src/SESS.NexaERP.ControlPlane.Persistence/SESS.NexaERP.ControlPlane.Persistence.csproj --startup-project src/SESS.NexaERP.ControlPlane.Persistence/SESS.NexaERP.ControlPlane.Persistence.csproj --context SESS.NexaERP.ControlPlane.Persistence.Rev869BControlPlaneDbContext --configuration Debug --no-build --output-dir Migrations
```

Acceptance requires the generated parity migration's `Up` and `Down` bodies to be byte-empty, zero connection-open
attempts, and exact pre/post target-worktree status. The whole disposable worktree is then deleted. This replaces the
unexecuted `has-pending-model-changes` command with the no-socket method proven by the probe; it does not add a
repository migration or change the frozen inventory.

### Offline migration SQL

```powershell
dotnet <dotnet-ef.dll> migrations script 0 20260821093000_Rev869BA4ControlPlaneInitial --project src/SESS.NexaERP.ControlPlane.Persistence/SESS.NexaERP.ControlPlane.Persistence.csproj --startup-project src/SESS.NexaERP.ControlPlane.Persistence/SESS.NexaERP.ControlPlane.Persistence.csproj --context SESS.NexaERP.ControlPlane.Persistence.Rev869BControlPlaneDbContext --configuration Debug --no-build --output <disposable-control-up.sql>

dotnet <dotnet-ef.dll> migrations script 20260821093000_Rev869BA4ControlPlaneInitial 0 --project src/SESS.NexaERP.ControlPlane.Persistence/SESS.NexaERP.ControlPlane.Persistence.csproj --startup-project src/SESS.NexaERP.ControlPlane.Persistence/SESS.NexaERP.ControlPlane.Persistence.csproj --context SESS.NexaERP.ControlPlane.Persistence.Rev869BControlPlaneDbContext --configuration Debug --no-build --output <disposable-control-down.sql>
```

Both output paths must resolve outside the repository. Commands run twice in fresh disposable roots; normalized SQL
bytes, line counts and SHA-256 must match. Connection-opening counters and migration applications remain zero.

## Package and deployment consequences

- Control Plane package count remains `35` and lock SHA-256 remains
  `64DC53ED03457021DFCBC985D9C8C5C0468B82BB102BC8382C3D920827137AA6`.
- ERP package count remains `41`; its verified lock remains
  `CF17917E57148E4E35D6C483CEF990615C11405EFD97DE3AB562FD98759E004E`.
- The verified union remains `41` identities: one previously verified Npgsql identity plus forty other verified
  identities.
- No package version, archive, content hash, signature, timestamp, certificate, revocation result or dependency edge
  changes.
- No central package management or additional package lock is required.
- Control Plane Persistence ships inside the Control Plane deployment; it is not a service or new deployment unit.
- Production Control Plane remains runtime composition only and does not own EF Design.

## Context, database and transaction separation

- Control Plane Persistence owns only its control context, database, `control.__EFMigrationsHistory`, migration,
  snapshot, connection and local transactions.
- ERP Infrastructure owns only the ERP context, ERP migrations, target database and target-local transactions.
- Neither context references, migrates or transacts over the other's database.
- No shared `DbContext`, connection, transaction, history table, snapshot or migration exists.
- Runtime flow remains the fenced idempotent saga
  `control commit -> target commit -> control reconciliation commit`, not cross-database ACID.
- API remains the ERP composition root; Control Plane remains the control runtime composition root.
- Production assemblies never reference test assemblies.

## Migration inventory

The current committed source remains rolled back at 13 ERP migrations and no A5/Control Plane migration. The frozen
post-A5 implementation inventory remains unchanged:

```text
erp_existing_migrations=13
erp_a5_target_migrations=1
erp_post_a5_migrations=14
control_plane_initial_migrations=1
combined_post_a5_migrations=15
rev869a_erp_ordinal=12
rev869b_erp_ordinal=13
a5_target_erp_ordinal=14
repository_migration_attempts=0
migration_applications=0
postgresql_connections=0
```

No PostgreSQL connection, migration application/removal, service, provisioning, deployment, production access,
Phase B, Correction 2, real credential or mutant execution occurred.

## Supersession and stop rules

This report supersedes only the Control Plane EF startup-project command and model-parity method in the earlier
dual-context report. It does not alter its package identities, migrations, allowlist, maximum path count, context
ownership, database separation, transaction rules, tests, mutants or other acceptance requirements.

Future implementation must stop and revert if the Persistence project cannot serve as both project and startup,
if its factory starts a host or attempts a connection, if `--no-connect` is unavailable for the pinned listing tool,
if parity generates any operation, if the exact 35-package lock changes, if the executable needs EF Design/Npgsql,
or if any unnamed path, central package file, cross-database edge or prohibited operation is required.

Retained states:

`phase_a_management_acceptance_state=FAIL`

`phase_b_state=NO_GO`

`correction_2_state=NO_GO`

`postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN`

`external_provisioning_state=NOT_STARTED`

`production_readiness_state=NOT_READY`

This report is the sole authorized repository change. Stop after its one-file report-only commit.
