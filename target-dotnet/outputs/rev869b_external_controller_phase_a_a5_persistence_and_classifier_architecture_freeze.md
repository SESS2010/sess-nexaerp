# REV869B Option-A Phase-A A5 persistence and classifier architecture freeze

Date: 2026-08-21

Decision type: report-only architecture and package-ownership freeze; no implementation authority

Starting HEAD: `7261382b42b3762b9b5bae3ab16b121affb2532d`

Expected parent: `fa00a1dc1c4ed4ff5a30ee58d2a5dd008fd182c4`

## 1. Architecture gate

`A5_REVISED_ARCHITECTURE_AND_PACKAGE_GATE=GO`

The exact selected architecture is:

1. a new dedicated assembly/project, `SESS.NexaERP.ControlPlane.Persistence`, owns the concrete PostgreSQL control
   persistence adapter, control schema source and the only direct Control Plane `Npgsql` package reference;
2. the Control Plane executable references that adapter and is the sole composition root for it;
3. the adapter references only `SESS.NexaERP.ControlPlane.Contracts` and has no reference to ERP API,
   ERP Infrastructure, Application, Domain, Acceptance Verifier or tests;
4. the pure target fence/replay classifier moves to the neutral Contracts assembly as
   `SESS.NexaERP.ControlPlane.Contracts.A4TargetExecutionClassifierV1`;
5. ERP Infrastructure references Contracts and calls that classifier from its target-local transaction;
6. ERP API remains the sole target-side composition root;
7. no production assembly references a test assembly; and
8. control and target databases retain separate schemas, migrations, connections, transactions, audit/outbox
   commits and deployment ownership.

This graph is complete, reproducible under the locked offline rules below, and acyclic. The new adapter is an assembly
boundary inside the already-frozen Control Plane deployment, not a new service and not authorization to begin Phase B
or execute a database operation.

## 2. Stage-0 evidence

| Check | Verified result | Status |
|---|---|---|
| HEAD | `7261382b42b3762b9b5bae3ab16b121affb2532d` | PASS |
| Parent | `fa00a1dc1c4ed4ff5a30ee58d2a5dd008fd182c4` | PASS |
| Branch | `master` | PASS |
| HEAD subject | `REV869B Phase-A A5 project graph boundary reconciliation` | PASS |
| HEAD content | Exactly `target-dotnet/outputs/rev869b_external_controller_phase_a_a5_project_graph_and_boundary_reconciliation.md` | PASS |
| Reconciliation SHA-256 | `A77F4E31A80767493117FA27C033B500A971ABC06497DDCA681D61756981913B` | PASS |
| Target worktree/index | Clean at entry | PASS |
| Implementation changes | None | PASS |
| Legacy boundary | `../legacy-reference/` was not queried, enumerated, opened, read, modified or used | PASS |

The authoritative external-controller architecture-freeze specification; Phase-A checkpoints, reviews and failure
reconciliations; A4 lease/atomic-boundary freeze, review and reconciliation; revised A5 immutable action-manifest
decision and blocker evidence; A5 mutant reconciliation; deterministic mutant-harness reconciliation; and latest
project-graph reconciliation were read completely. Their frozen ownership, fixed 19-action manifest, 30 A5 tests,
23 retained A4 tests, 40 mutants and prohibited-operation rules remain authoritative except where this report
explicitly relocates assembly ownership and expands the exact future file boundary.

No build, restore, test, mutant, PostgreSQL, migration, service, deployment, production, Phase B or Correction 2
operation was run.

## 3. Committed graph and package facts

The committed solution contains only API, Application, Domain, Infrastructure and ERP tests. Contracts, Control
Plane, Acceptance Verifier and Control Plane tests exist but are not solution members.

The committed direct production graph is:

`ControlPlane -> ControlPlane.Contracts`

`AcceptanceVerifier -> ControlPlane.Contracts`

`Api -> Infrastructure -> Application -> Domain`

`Api -> Application -> Domain`

`Api -> Domain`

Control Plane Contracts and Domain have no project references. The Control Plane test project references Contracts,
Control Plane and Acceptance Verifier. ERP tests reference API, Application, Infrastructure and Domain. There is no
production-to-test edge.

The repository has no committed `Directory.Packages.props`, `NuGet.Config`, `packages.lock.json` or
`Directory.Build.props/targets` package policy. Package versions are declared directly in each project. The SDK is
pinned by `global.json` to `10.0.302` with `latestFeature` roll-forward.

ERP Infrastructure directly declares `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` and resolves
`Npgsql/10.0.3`. The committed Control Plane project declares no Npgsql package. Its current generated
`obj/project.assets.json` entry for `Npgsql/10.0.3` is not a committed dependency declaration and is not
reproducibility evidence.

## 4. Frozen project names and responsibilities

| Project/assembly | Frozen responsibility | Explicit exclusions |
|---|---|---|
| `SESS.NexaERP.ControlPlane.Contracts` | Neutral immutable controller/target protocol records and interfaces; pure deterministic target fence/replay classifier | No Npgsql, EF, database connection, DI, ERP business rule or executable composition |
| `SESS.NexaERP.ControlPlane.Persistence` — NEW | Concrete Npgsql control-plane adapter, control schema source, serializable control transactions, control locks, grants, leases/fences, lifecycle, audit/outbox and replay persistence | No ERP API/Infrastructure/Application/Domain, target schema, Purchase behavior, HTTP endpoint or test dependency |
| `SESS.NexaERP.ControlPlane` | Controller executable, raw ingress, controller state machine, reconciliation orchestration and composition root for control persistence | No ERP Infrastructure/API reference, no direct Npgsql package, no target transaction ownership |
| `SESS.NexaERP.AcceptanceVerifier` | Independent verifier host consuming Contracts | No persistence adapter, ERP implementation or lifecycle mutation |
| `SESS.NexaERP.Infrastructure` | ERP target adapter, target schema/migration, fixed 19-action dispatcher, Purchase transaction enlistment and terminal-result storage/read | No Control Plane executable or control-database migration |
| `SESS.NexaERP.Api` | ERP target-side executable and sole composition root for ERP Infrastructure target services | No control persistence ownership |
| `SESS.NexaERP.Application` | Immutable Purchase service and DTO contracts | No controller or provider dependency |
| `SESS.NexaERP.Domain` | Immutable Purchase business/domain rules | No controller or persistence-adapter dependency |
| Control Plane tests | Test-only validation of controller, adapter, verifier and target production graphs | Never referenced by production |
| ERP tests | Test-only ERP/Application/Infrastructure/API validation | Never referenced by production |

## 5. Frozen revised dependency graph

```text
ControlPlane executable
  -> ControlPlane.Contracts
  -> ControlPlane.Persistence
       -> ControlPlane.Contracts
       -> NuGet: Npgsql 10.0.3

AcceptanceVerifier
  -> ControlPlane.Contracts

ERP Api
  -> ControlPlane.Contracts
  -> ERP Infrastructure
       -> ControlPlane.Contracts
       -> ERP Application
            -> Domain
       -> Domain
  -> ERP Application
  -> Domain

ControlPlane.Tests
  -> ControlPlane.Contracts
  -> ControlPlane
  -> ControlPlane.Persistence
  -> AcceptanceVerifier
  -> ERP Api
  -> ERP Infrastructure

ERP.Tests
  -> ERP Api
  -> ERP Infrastructure
  -> ERP Application
  -> Domain
```

Every arrow points from a composition/implementation/test assembly toward a neutral contract, lower business layer,
adapter it composes, or production assembly it tests. Contracts and Domain remain leaves. No arrow returns to an
upstream executable, so no cycle exists.

## 6. Exact solution and ProjectReference changes

### 6.1 Solution membership

`SESS.NexaERP.slnx` must add these existing projects:

- `src/SESS.NexaERP.ControlPlane.Contracts/SESS.NexaERP.ControlPlane.Contracts.csproj`
- `src/SESS.NexaERP.ControlPlane/SESS.NexaERP.ControlPlane.csproj`
- `src/SESS.NexaERP.AcceptanceVerifier/SESS.NexaERP.AcceptanceVerifier.csproj`
- `tests/SESS.NexaERP.ControlPlane.Tests/SESS.NexaERP.ControlPlane.Tests.csproj`

It must also add the new project:

- `src/SESS.NexaERP.ControlPlane.Persistence/SESS.NexaERP.ControlPlane.Persistence.csproj`

No current solution project is removed.

### 6.2 Exact edges to add

| Modified/new `.csproj` | Exact `ProjectReference Include` |
|---|---|
| `src/SESS.NexaERP.ControlPlane.Persistence/SESS.NexaERP.ControlPlane.Persistence.csproj` | `..\SESS.NexaERP.ControlPlane.Contracts\SESS.NexaERP.ControlPlane.Contracts.csproj` |
| `src/SESS.NexaERP.ControlPlane/SESS.NexaERP.ControlPlane.csproj` | `..\SESS.NexaERP.ControlPlane.Persistence\SESS.NexaERP.ControlPlane.Persistence.csproj` |
| `src/SESS.NexaERP.Infrastructure/SESS.NexaERP.Infrastructure.csproj` | `..\SESS.NexaERP.ControlPlane.Contracts\SESS.NexaERP.ControlPlane.Contracts.csproj` |
| `src/SESS.NexaERP.Api/SESS.NexaERP.Api.csproj` | `..\SESS.NexaERP.ControlPlane.Contracts\SESS.NexaERP.ControlPlane.Contracts.csproj` |
| `tests/SESS.NexaERP.ControlPlane.Tests/SESS.NexaERP.ControlPlane.Tests.csproj` | `..\..\src\SESS.NexaERP.ControlPlane.Persistence\SESS.NexaERP.ControlPlane.Persistence.csproj` |
| `tests/SESS.NexaERP.ControlPlane.Tests/SESS.NexaERP.ControlPlane.Tests.csproj` | `..\..\src\SESS.NexaERP.Api\SESS.NexaERP.Api.csproj` |
| `tests/SESS.NexaERP.ControlPlane.Tests/SESS.NexaERP.ControlPlane.Tests.csproj` | `..\..\src\SESS.NexaERP.Infrastructure\SESS.NexaERP.Infrastructure.csproj` |

No `ProjectReference` is removed. No Application, Domain, Acceptance Verifier or ERP-tests project file changes.

### 6.3 Explicitly prohibited edges

- `ControlPlane -> Infrastructure`
- `ControlPlane -> Api`
- `ControlPlane.Persistence -> Infrastructure`
- `ControlPlane.Persistence -> Api`
- `Infrastructure -> ControlPlane`
- `Infrastructure -> ControlPlane.Persistence`
- any production project -> either test project

## 7. Package ownership and offline reproducibility freeze

### 7.1 Exact owner and version

Only `src/SESS.NexaERP.ControlPlane.Persistence/SESS.NexaERP.ControlPlane.Persistence.csproj` may add:

`<PackageReference Include='Npgsql' Version='10.0.3' />`

`10.0.3` is frozen because the committed ERP Infrastructure project already pins
`Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` and its resolved assets identify `Npgsql/10.0.3` with package
SHA-512 `7nb5YzXuvWWJxB0J8DiyL3we+X4FOctZrt0fIBnucOIaIevFEEwGQVZKtiu9olXdlNAK1eNgqSral6r/jlhI4w==`.
That existing cache observation establishes availability, not the new project's dependency declaration; the direct
reference and lock file are authoritative.

Control Plane, Contracts and Acceptance Verifier must not directly reference Npgsql or EF Core. The persistence
adapter must not reference `Npgsql.EntityFrameworkCore.PostgreSQL` because it uses the Npgsql ADO.NET transaction
boundary and owns no ERP `DbContext`.

### 7.2 Lock and central package decisions

- Central package management is **not required and is prohibited for A5**. The repository has none, and adding
  `Directory.Packages.props` would alter package policy for unrelated projects.
- A project-local lock file is **required** at
  `src/SESS.NexaERP.ControlPlane.Persistence/packages.lock.json`.
- The new project must set `RestorePackagesWithLockFile=true`. Acceptance after initial offline materialization uses
  `RestoreLockedMode=true` or command-line `--locked-mode`.
- The lock must contain the exact `net10.0` direct/transitive closure, resolved versions and NuGet content hashes.
  At minimum it must lock `Npgsql/10.0.3` and its actual resolved logging-abstractions closure. No hand-edited or
  incomplete lock is accepted.
- Existing projects retain their current explicit package declarations. This freeze does not authorize lock files or
  package-version changes for them.

### 7.3 Zero-network restore protocol

The future implementation may materialize the new lock/assets only from a disposable local NuGet source containing
the exact cached `.nupkg` files required by the adapter closure. Before restore it must record each package ID,
version, file SHA-256 and NuGet content hash. The restore command must name only that local source, disable the HTTP
cache, create/verify the project lock, and make zero DNS/HTTP requests.

After the lock is committed, every acceptance restore is local-source-only and locked. All builds/tests then use
`--no-restore`. A network source, fallback download, floating/ranged version, changed lock, missing package, different
content hash or unrecorded transitive dependency triggers rollback and stop.

Generated `obj`/`bin`/global-cache state is never source evidence and is never committed.

## 8. Classifier ownership freeze

### 8.1 Exact identity

- Owning assembly: `SESS.NexaERP.ControlPlane.Contracts`
- Namespace: `SESS.NexaERP.ControlPlane.Contracts`
- Type: `public static class A4TargetExecutionClassifierV1`
- Owning source path: `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs`
- Pure entry point: `Classify` over the immutable `A4TargetExecutionJobV1`, authoritative locked fencing token,
  explicit server time and optional committed `A4TargetTerminalResultV1`
- Return: the already-frozen structured target-attempt outcome with `FIRST_OWNER`, `COMPLETED_REPLAY` or
  `REJECTED` and exact failure/binding facts

The classifier has no service provider, database, connection, transaction, clock singleton, mutable state, callback,
delegate, reflection, handler or caller-selected policy input. It never writes. Its authoritative watermark and
committed result are read under the target transaction lock by ERP Infrastructure; the incoming job is comparison
input only.

### 8.2 Consumers and single-rule requirement

- `Rev869BL1BoundaryStateMachine.RequireTargetExecution` becomes a compatibility forwarding wrapper with no fence or
  replay rule of its own.
- `NpgsqlA4TargetExecutionProvider` invokes `A4TargetExecutionClassifierV1.Classify` directly after locked target
  facts and before actor resolution, Purchase dispatch or any target write.
- Control Plane tests and ERP production-graph tests invoke/observe the same classifier through real production paths.

No consumer may copy, fork or weaken the predicates. Lower fence rejects; greater fence with no result is the only
first owner; equal fence can only return an exact committed replay; equal collision rejects without disclosure; and
equal incomplete state fails closed. Caller-supplied classification, watermark, existing result or handler selection
is never authoritative.

The semantic locations of A4-M06 and A5-M18 move unchanged in meaning to
`A4TargetExecutionClassifierV1.Classify`. Their decisive lower/equal/first-owner assertions remain mandatory. The
other 38 mutant definitions, including unchanged M12, retain their frozen meanings and enforcement points.

## 9. Interface, DI and composition-root ownership

`IDurableControlPlanePersistenceProvider` remains owned by Control Plane Contracts. It retains one authoritative
snapshot read and one composite atomic mutation; no partial grant, lease, fence, lifecycle, nonce, audit or outbox
setter may be exported.

`ITargetAuthorizedPlanExecutionProvider` and `IAuthoritativeTargetResultProvider` remain owned by Control Plane
Contracts. ERP Infrastructure implements them. Contracts define protocol authority, not database ownership.

The new adapter exposes one strongly typed factory that returns `IDurableControlPlanePersistenceProvider` and keeps
all Npgsql concrete types internal to the adapter assembly. The factory and option surface contain only bounded
control connection/configuration values; they do not accept a service locator, arbitrary provider type or target
database object.

The Control Plane `Program.cs` reads validated controller configuration and registers exactly one adapter instance
and descriptor under `IDurableControlPlanePersistenceProvider`. It must not reference an Npgsql type. ERP API
`Program.cs` registers exactly one ERP Infrastructure target provider/result reader and remains the target
composition root. No alternate/null/in-memory production registration is allowed.

Tests may reference production projects and use declared internal friend seams only for deterministic transaction
facts/counters. Production projects never reference tests, and test seams are not DI-selectable providers.

## 10. Deployment, migration and database boundaries

`SESS.NexaERP.ControlPlane.Persistence.dll` and its locked Npgsql runtime dependency ship in the Control Plane
deployment artifact and process. They do not create a separate service, identity, endpoint or deployment authority.
The Control Plane workload identity alone receives the control-database credential/role.

The adapter owns only the control-plane source schema in
`src/SESS.NexaERP.ControlPlane.Persistence/Rev869BA4ControlPlaneSchemaV1.cs`. ERP Infrastructure owns only the target
A4 migration/source under its existing migration tree. Neither project may contain or apply the other's tables,
functions, roles or migrations.

There is no shared `DbContext`, `DbConnection`, `DbTransaction`, transaction scope, connection string or migration
history between control and target databases. No cross-database migration, foreign key, transaction, two-phase commit
or distributed ACID claim is allowed. This Phase-A source gate never applies either schema.

## 11. Transaction, audit and failure boundaries

### 11.1 Control transaction

The persistence adapter owns one serializable control-database transaction per high-level operation. Grant,
authorization, idempotency, lease/fence allocation, lifecycle/version, dispatch/reconciliation, immutable response,
control audit and control outbox commit together or all roll back. The database is the concurrency owner; process
locks are not authoritative. Audit/outbox failure prevents the control state commit.

### 11.2 Target transaction

ERP Infrastructure owns a separate serializable target transaction. Locked target fence/idempotency facts feed the
neutral classifier. Only `FIRST_OWNER` may invoke the fixed server-owned Purchase mapping. Purchase business rows,
history, command receipts, normal Purchase audit, target fence/idempotency, terminal result, A4 target audit and
target outbox commit together or all roll back.

The capability-bound Purchase enlistment seam may join only the same current `NexaErpDbContext` target transaction.
It cannot commit, roll back or dispose the outer transaction. Normal public Purchase calls retain service-owned
transactions. Purchase application/domain contracts, partial implementations and business rules remain unchanged.

### 11.3 Cross-store recovery

The sequence remains a fenced idempotent saga:

`control commit -> target commit -> control reconciliation commit`.

Loss after target commit is recovered by reading the immutable target result; reconciliation never invokes Purchase
again. Missing, conflicting or ambiguous facts quarantine/fail closed. No local failure can be compensated by
silently committing the other database.

## 12. Exhaustive future A5 implementation allowlist

This report authorizes no implementation. A future management authorization may permit only the following **30
paths**. This list supersedes the conditional 28-path candidate boundary solely for a later A5 implementation:

1. `SESS.NexaERP.slnx`
2. `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs`
3. `src/SESS.NexaERP.ControlPlane.Persistence/SESS.NexaERP.ControlPlane.Persistence.csproj` — NEW
4. `src/SESS.NexaERP.ControlPlane.Persistence/packages.lock.json` — NEW
5. `src/SESS.NexaERP.ControlPlane.Persistence/Rev869BA4ControlPlaneSchemaV1.cs` — NEW
6. `src/SESS.NexaERP.ControlPlane.Persistence/NpgsqlA4DurableControlPlanePersistenceProvider.cs` — NEW
7. `src/SESS.NexaERP.ControlPlane/SESS.NexaERP.ControlPlane.csproj`
8. `src/SESS.NexaERP.ControlPlane/Configuration/ControlPlaneOptions.cs`
9. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs`
10. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BExecutionBinding.cs`
11. `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs`
12. `src/SESS.NexaERP.ControlPlane/Endpoints/ControllerContractEndpointsV1.cs`
13. `src/SESS.NexaERP.ControlPlane/Program.cs`
14. `src/SESS.NexaERP.ControlPlane/Reconciliation/PinnedA4TargetResultProvider.cs` — NEW
15. `src/SESS.NexaERP.ControlPlane/Reconciliation/A4TerminalResultReconciliationService.cs` — NEW
16. `src/SESS.NexaERP.Infrastructure/SESS.NexaERP.Infrastructure.csproj`
17. `src/SESS.NexaERP.Infrastructure/DependencyInjection.cs`
18. `src/SESS.NexaERP.Infrastructure/Persistence/NpgsqlA4TargetExecutionProvider.cs` — NEW
19. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BA4TargetExecutionBoundarySql.cs` — NEW
20. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260821093000_Rev869BA4TargetExecutionBoundary.cs` — NEW
21. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260821093000_Rev869BA4TargetExecutionBoundary.Designer.cs` — NEW
22. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs`
23. `src/SESS.NexaERP.Api/SESS.NexaERP.Api.csproj`
24. `src/SESS.NexaERP.Api/Program.cs`
25. `src/SESS.NexaERP.Api/Endpoints/Rev869BA4TargetExecutionEndpoints.cs` — NEW
26. `tests/SESS.NexaERP.ControlPlane.Tests/SESS.NexaERP.ControlPlane.Tests.csproj`
27. `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs`
28. `tests/SESS.NexaERP.ControlPlane.Tests/A4FailureCorrectionContractTests.cs` — NEW
29. `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs`
30. `outputs/rev869b_external_controller_phase_a_a5_revised_source_implementation_checkpoint.md` — NEW

The exact implementation checkpoint path is:

`outputs/rev869b_external_controller_phase_a_a5_revised_source_implementation_checkpoint.md`

The prior in-Control-Plane persistence paths are replaced, not retained:

- `src/SESS.NexaERP.ControlPlane/Persistence/Rev869BA4ControlPlaneSchemaV1.cs`
- `src/SESS.NexaERP.ControlPlane/Persistence/NpgsqlA4DurableControlPlanePersistenceProvider.cs`

They must remain absent. Existing reports/checkpoints, Purchase application/domain files, Purchase operation partials,
public Purchase endpoints, Acceptance Verifier sources, existing accepted migrations/snapshot, scripts/helpers and
all unnamed paths remain immutable.

The maximum is exactly `30`. A 31st path, central package file, second lock file, new classifier file, adapter DI
file, extra migration, project, helper, README or package source configuration is not implicit and triggers stop.

## 13. Complete future build, test and mutant acceptance

A separately authorized implementation passes only when its checkpoint proves all of the following:

1. exact authorized entry HEAD/parent/subject, clean target scope and unchanged legacy sibling boundary;
2. exactly the approved subset of the 30-path maximum, with every required artifact present and no old persistence
   path or prior report/checkpoint edit;
3. the solution contains all ten production/test projects named in the graph and the graph has exactly the frozen
   edges, no cycle, no prohibited edge and no production-to-test reference;
4. the persistence adapter alone directly references `Npgsql` `10.0.3`; its lock file is exact; no unpinned, floating,
   changed or duplicate provider package exists;
5. local-source-only offline restore creates/verifies the lock with zero network requests, then all remaining commands
   use `--no-restore`;
6. warning-as-error offline builds of Contracts, Persistence, Control Plane, Acceptance Verifier, Infrastructure,
   API, both test graphs and `SESS.NexaERP.slnx` pass with zero warnings/errors;
7. exactly 30 A5 tests pass as an aggregate and each decisive test passes individually `1/1` with zero skip;
8. all 23 retained A4 tests, the complete Phase-A Control Plane suite and the complete non-PostgreSQL ERP suite pass;
9. the fixed 19 Purchase action mappings remain server-owned, unique and bound to the immutable existing Purchase
   methods; the 20-row manifest remains 2,668 UTF-8 bytes with SHA-256
   `EDAF648EFF4BD77158EF3A18A780D7B0DAD634FFB90CDBA8564A27D4DCFC95CB` in two fresh processes;
10. production graph tests resolve one non-null pinned control adapter from the Control Plane composition root and one
    pinned target provider/result reader from API, with no alternate/null/in-memory production provider;
11. classifier tests cover lower fence, greater first owner, equal exact replay, equal collision and equal incomplete
    state through the real target orchestration, with zero handler/write/commit counters for every rejection/replay;
12. deterministic transaction seams prove one control commit or rollback and one independent target commit or
    rollback; no shared connection/context/transaction and no cross-database migration;
13. all Purchase business/history/receipt/audit and A4 target fence/result/audit/outbox facts commit or roll back in
    the single target transaction, while normal public Purchase behavior is unchanged;
14. every control grant/lease/fence/lifecycle/audit/outbox fact commits or rolls back in the single control transaction;
15. all 40 semantic production mutants run again from fresh isolated worktrees at the exact candidate commit under
    the deterministic Git-blob harness;
16. mutant arithmetic is exactly `40 compiled / 40 killed / 0 survived / 0 invalid`, with A4-M06 and A5-M18 applied
    to the shared classifier and M12 rerun unchanged;
17. every killer fails only for its intended non-vacuous invariant; duplicate production-diff hashes are zero;
18. every mutated file is restored from the original Git blob, post-restore SHA/blob/size equals baseline,
    `git diff --check` passes, and no tracked residue remains before the next mutant;
19. schema/SQL bytes, line counts and hashes reproduce offline; EF discovery is `--no-connect` only; no migration is
    applied and model/snapshot parity remains exact;
20. privacy, secret, forbidden dependency/package, direct business-DML, public capability and source-boundary scans
    pass;
21. observed counters remain zero for PostgreSQL connections/tests, migration application, services, deployment,
    production access, credentials, Phase B and Correction 2; and
22. exactly one source-only correction commit is created, its boundary and parent are verified, target scope is clean,
    and work stops for fresh independent report-only architecture/security review.

## 14. Explicitly rejected designs

The following designs are frozen as invalid:

- any circular project reference;
- `ControlPlane -> ERP Infrastructure` or `ControlPlane -> ERP API`;
- `ERP Infrastructure -> ControlPlane executable` or `ERP Infrastructure -> ControlPlane.Persistence`;
- any production-to-test reference;
- putting the concrete control Npgsql provider in the Control Plane executable, ERP Infrastructure or ERP API;
- putting ERP target persistence or Purchase behavior in the control persistence adapter;
- a shared cross-database `DbContext`, connection, transaction, migration or distributed transaction;
- caller-controlled fence, watermark, replay outcome, existing-result authority, handler or classifier selection;
- duplicate fence/replay rule implementations;
- unpinned/floating Npgsql, absent/changed lock, remote/fallback restore source or network-dependent acceptance;
- central package management added only for this correction;
- dynamic/reflection/plugin/caller-selected Purchase dispatch;
- copying or reimplementing Purchase business rules or direct Purchase DML in the target provider; and
- treating package cache, `obj` assets, fake providers, source strings or exception messages as production evidence.

## 15. Rollback and mandatory stop conditions

Any future implementation must revert all implementation changes, create only a separately authorized report-only
blocker commit and stop if:

- entry lineage, report hash or target cleanliness differs;
- any 31st/unnamed path or unlisted project/package/reference is required;
- the exact adapter name, location, responsibility or deployment boundary cannot be preserved;
- Npgsql `10.0.3` or its locked closure is unavailable locally, hash-mismatched or requires network;
- a direct Npgsql dependency leaks into Control Plane/Contracts/Verifier;
- the graph develops a cycle or any prohibited edge;
- the shared classifier cannot remain pure, deterministic and sole-owner in Contracts;
- Infrastructure needs the Control Plane executable/persistence adapter or Control Plane needs ERP API/Infrastructure;
- a production assembly needs a test assembly;
- a shared cross-database context/connection/transaction/migration becomes necessary;
- a caller controls classification or any rejected/equal fence reaches Purchase or a write;
- the control or target audit/outbox can commit separately from its local authoritative state;
- the capability-bound Purchase enlistment seam can escape, own the outer transaction or alter public behavior;
- a Purchase application/domain contract, operation partial, business rule, accepted public endpoint, existing
  migration/snapshot, helper or prior report/checkpoint must change;
- the fixed 19-action mapping or manifest changes;
- a build, test, package/graph, schema/hash, privacy/security or regression check fails;
- any mutant fails to compile, survives, is killed for an unrelated reason, is invalid/duplicate, fails exact blob
  restoration or leaves tracked residue; or
- PostgreSQL, migration execution, service start, credentials, deployment, production, Phase B or Correction 2
  becomes necessary.

No alternate architecture, package version, retry mutant, replacement path or silent boundary expansion is allowed
after a stop.

## 16. Authorization state and next gate

This report resolves both project/package ownership blockers and freezes a complete cycle-free architecture. It does
not authorize implementation, restore, build, test, mutant execution, database use or operational activity.

`A5_REVISED_ARCHITECTURE_AND_PACKAGE_GATE=GO`

`A5_CORRECTION_IMPLEMENTATION_STATE=NOT_STARTED`

`PHASE_A_MANAGEMENT_ACCEPTANCE_STATE=FAIL_PENDING_IMPLEMENTATION_AND_INDEPENDENT_REVIEW`

`POSTGRESQL_EXECUTION_STATE=NOT_AUTHORIZED_NOT_RUN`

`PHASE_B_STATE=NO_GO`

`CORRECTION_2_STATE=NO_GO`

The single next management gate is a fresh explicit authorization for one bounded revised A5 source-only
implementation from the commit containing this report, constrained to the exact 30-path maximum, locked offline
package protocol, 30 A5 tests, 23 retained A4 tests, all non-PostgreSQL regressions and all 40 deterministic semantic
mutants. Success creates exactly one correction commit and stops for independent review; failure reverts and creates
only the authorized blocker report.
