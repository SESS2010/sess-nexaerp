# REV869B Phase-A A5 migration inventory and DbContext-boundary reconciliation

Date: 2026-08-21

Decision type: report-only migration/context reconciliation

Starting HEAD: `cdd0ca6777e6c3981d3ec3ce3973fdaa2b78aef6`

Expected parent: `431e91583d0e55438ed85b06f1f360790e6c8a0c`

## 1. Verdict

`A5_MIGRATION_CONTEXT_RECONCILIATION_GATE=NO_GO`

The two database streams can be named and bounded precisely, but the requested post-A5 formula is not supported by
the complete frozen A5 architecture. The committed ERP has 13 migrations. The frozen target execution design needs
a new ERP-local migration for target fence, execution-idempotency, terminal-result, target-audit and target-outbox
state. The new management intent independently needs one initial migration in a dedicated Control Plane DbContext.
Neither migration can own the other database's schema.

Therefore the architecture-complete inventory is:

`13 existing ERP + 1 required A5 target ERP + 1 required Control Plane initial = 15 combined`

The requested `13 ERP + 1 Control Plane = 14 combined` inventory omits the required A5 target ERP schema. Removing
that target migration makes the source boundary insufficient; moving its objects into the Control Plane migration
would be a prohibited cross-database migration. No migration was generated or applied during this reconciliation.

## 2. Stage 0

| Gate | Observed result | Status |
|---|---|---|
| HEAD | `cdd0ca6777e6c3981d3ec3ce3973fdaa2b78aef6` | PASS |
| Parent | `431e91583d0e55438ed85b06f1f360790e6c8a0c` | PASS |
| Subject | `REV869B Phase-A A5 migration count blocker` | PASS |
| Branch | `master` | PASS |
| HEAD content | Exactly `outputs/rev869b_external_controller_phase_a_a5_revised_source_implementation_blocker_2.md` | PASS |
| Blocker SHA-256 | `C43992A458DCF6C93213B5448484F1B952B4CCA4E094164B3A408F46773A2984` | PASS |
| Target tracked/index scope | Clean at entry | PASS |
| Legacy sibling | Remained the same untracked entry; no content was accessed or modified | PASS |

The persistence/classifier architecture freeze, project/package graph reconciliation, official package-identity
report, current revised implementation authorization and latest migration-count blocker were read completely. Source
and project inspection was read-only.

## 3. Committed ERP migration stream

- DbContext type: `SESS.NexaERP.Infrastructure.Persistence.NexaErpDbContext`.
- Owning project and migration assembly: `src/SESS.NexaERP.Infrastructure/SESS.NexaERP.Infrastructure.csproj` /
  `SESS.NexaERP.Infrastructure`.
- Snapshot: `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/NexaErpDbContextModelSnapshot.cs`.
- Runtime connection owner: ERP API composition root through `Infrastructure.DependencyInjection.AddInfrastructure`,
  using `ConnectionStrings:NexaErp`.
- Design-time owner: `src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDesignTimeDbContextFactory.cs`, using only
  `ConnectionStrings__NexaErp` plus `NexaErp__ExpectedDatabase` and checking that both database identities match.
- Migration history: provider-default `__EFMigrationsHistory` in the ERP database; no explicit history-table override
  is committed. Its physical ownership is the ERP database/connection only.

The exact committed migration order is:

1. `20260808110924_Phase1Foundation`
2. `20260808114550_Phase1AuthorizationSeed`
3. `20260808123411_Rev866EmployeePermissionMatrix`
4. `20260808142353_Rev866CorrectiveStatusPermissionAudit`
5. `20260808151207_Rev867MasterFoundation`
6. `20260808160435_Rev867C1Corrections`
7. `20260808182945_Rev868PurchaseRequisitionFoundation`
8. `20260808190920_Rev868PurchaseLocationAllocationCorrection`
9. `20260809123000_Rev868C2DepartmentManagerApprovalMapping`
10. `20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation`
11. `20260810110000_Rev868C3LegacyMixedDepartmentDeactivationCorrection`
12. `20260810120000_Rev869AIdentityMasterScopeFoundation`
13. `20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation`

REV869A is unique at ERP ordinal 12. REV869B is unique at ERP ordinal 13. They are adjacent and immutable.

## 4. Required target-local A5 migration

The governing A5 architecture assigns the target transaction to `NexaErpDbContext` and requires target-local
watermark, execution-idempotency, immutable terminal-result, A4 audit and A4 outbox state to commit atomically with
Purchase business/history/receipt state. Those objects are not supplied by the proposed Control Plane database.

The frozen target artifacts are:

- `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BA4TargetExecutionBoundarySql.cs`;
- `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260821093000_Rev869BA4TargetExecutionBoundary.cs`; and
- `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260821093000_Rev869BA4TargetExecutionBoundary.Designer.cs`.

The migration must be discoverable in `SESS.NexaERP.Infrastructure` after current ERP ordinal 13. It would be ERP
ordinal 14. It cannot be classified as a Control Plane migration merely to preserve a combined count.

## 5. Exact proposed Control Plane migration stream

The management intent would require this separate design:

- DbContext type: `SESS.NexaERP.ControlPlane.Persistence.Rev869BControlPlaneDbContext`.
- Owning project and migration assembly: `src/SESS.NexaERP.ControlPlane.Persistence/SESS.NexaERP.ControlPlane.Persistence.csproj`
  / `SESS.NexaERP.ControlPlane.Persistence`.
- Initial migration ID/name: `20260821093000_Rev869BA4ControlPlaneInitial`.
- Snapshot: `src/SESS.NexaERP.ControlPlane.Persistence/Migrations/Rev869BControlPlaneDbContextModelSnapshot.cs`.
- Design-time factory: `SESS.NexaERP.ControlPlane.Persistence.Rev869BControlPlaneDesignTimeDbContextFactory`.
- Runtime connection owner: Control Plane composition root using a distinct `ConnectionStrings:Rev869BControlPlane`
  value and expected control-database identity; ERP API and Infrastructure never receive it.
- Design-time variables: `ConnectionStrings__Rev869BControlPlane` and
  `Rev869BControlPlane__ExpectedDatabase`, with exact database-name comparison before context construction.
- Migration history: `control.__EFMigrationsHistory` in the Control Plane database, explicitly configured by both
  runtime and design-time options.

This context and all five context/migration files are absent. The frozen adapter instead uses direct Npgsql ADO.NET,
owns raw control schema source, and explicitly states that it owns no DbContext. A real DbContext also requires
`Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Design` and
`Npgsql.EntityFrameworkCore.PostgreSQL`; the frozen adapter package graph permits only direct `Npgsql 10.0.3` and its
three-package lock closure. The EF package closure and content identities have not been reconciled for this project.

## 6. Database, history and transaction separation

The only safe ownership is:

```text
ERP API -> NexaErpDbContext -> ERP database
                         -> ERP __EFMigrationsHistory
                         -> 13 committed + required target A5 migration

Control Plane -> Rev869BControlPlaneDbContext -> Control Plane database
                                           -> control.__EFMigrationsHistory
                                           -> one Control Plane initial migration
```

There is no shared connection string, `DbContext`, migration assembly, snapshot, history table, transaction, foreign
key or migration. The control provider cannot migrate target objects. The target provider cannot migrate control
objects. Runtime ordering remains a saga of independent local transactions, never a distributed transaction.

## 7. Exact future offline discovery and parity commands

These commands are definitions only and were not run. They require a prior authorized offline build. The loopback
design values contain no real credential; `--no-connect` must prevent opening a connection.

ERP migration discovery:

```powershell
$env:ConnectionStrings__NexaErp='Host=127.0.0.1;Port=1;Database=rev869b_erp_design_only;Username=design_only;Timeout=1;Command Timeout=1'
$env:NexaErp__ExpectedDatabase='rev869b_erp_design_only'
dotnet ef migrations list --context SESS.NexaERP.Infrastructure.Persistence.NexaErpDbContext --project src/SESS.NexaERP.Infrastructure/SESS.NexaERP.Infrastructure.csproj --startup-project src/SESS.NexaERP.Api/SESS.NexaERP.Api.csproj --no-build --no-connect
```

ERP model/snapshot parity:

```powershell
dotnet ef migrations has-pending-model-changes --context SESS.NexaERP.Infrastructure.Persistence.NexaErpDbContext --project src/SESS.NexaERP.Infrastructure/SESS.NexaERP.Infrastructure.csproj --startup-project src/SESS.NexaERP.Api/SESS.NexaERP.Api.csproj --no-build
```

Control Plane migration discovery:

```powershell
$env:ConnectionStrings__Rev869BControlPlane='Host=127.0.0.1;Port=1;Database=rev869b_control_design_only;Username=design_only;Timeout=1;Command Timeout=1'
$env:Rev869BControlPlane__ExpectedDatabase='rev869b_control_design_only'
dotnet ef migrations list --context SESS.NexaERP.ControlPlane.Persistence.Rev869BControlPlaneDbContext --project src/SESS.NexaERP.ControlPlane.Persistence/SESS.NexaERP.ControlPlane.Persistence.csproj --startup-project src/SESS.NexaERP.ControlPlane/SESS.NexaERP.ControlPlane.csproj --no-build --no-connect
```

Control Plane model/snapshot parity:

```powershell
dotnet ef migrations has-pending-model-changes --context SESS.NexaERP.ControlPlane.Persistence.Rev869BControlPlaneDbContext --project src/SESS.NexaERP.ControlPlane.Persistence/SESS.NexaERP.ControlPlane.Persistence.csproj --startup-project src/SESS.NexaERP.ControlPlane/SESS.NexaERP.ControlPlane.csproj --no-build
```

Each list must be captured separately and must show only its owning context. The installed EF 10 CLI exposes
`--no-connect` for `migrations list`; `has-pending-model-changes` has no connection option and compares the runtime
model with its snapshot without a database query. Environment values must be removed after discovery. Any attempted
socket, package restore, migration generation or migration application is a failure.

## 8. Formula reconciliation

Current committed inventory:

```text
erp_migration_count=13
control_plane_migration_count=0
combined_solution_migration_count=13
rev869a_erp_ordinal=12
rev869b_erp_ordinal=13
migration_attempt_count=0
migration_application_count=0
cross_database_migration_count=0
modified_existing_erp_migration_count=0
```

Requested management inventory, not frozen because it omits the target schema:

```text
erp_migration_count=13
control_plane_migration_count=1
combined_solution_migration_count=14
```

Architecture-complete A5 inventory:

```text
erp_migration_count=14
control_plane_migration_count=1
combined_solution_migration_count=15
rev869a_erp_ordinal=12
rev869b_erp_ordinal=13
migration_attempt_count=0
migration_application_count=0
cross_database_migration_count=0
modified_existing_erp_migration_count=0
```

This report does not describe 14 ERP migrations as the requested design. It records 14 only as the unavoidable
architecture-complete consequence of retaining the separately required target A5 migration.

## 9. Required path-boundary revision

The frozen 30-path boundary lacks all five Control Plane DbContext/migration artifacts. The only
architecture-complete bounded candidate is the existing 30 paths plus these exact five paths:

31. `src/SESS.NexaERP.ControlPlane.Persistence/Rev869BControlPlaneDbContext.cs`
32. `src/SESS.NexaERP.ControlPlane.Persistence/Rev869BControlPlaneDesignTimeDbContextFactory.cs`
33. `src/SESS.NexaERP.ControlPlane.Persistence/Migrations/20260821093000_Rev869BA4ControlPlaneInitial.cs`
34. `src/SESS.NexaERP.ControlPlane.Persistence/Migrations/20260821093000_Rev869BA4ControlPlaneInitial.Designer.cs`
35. `src/SESS.NexaERP.ControlPlane.Persistence/Migrations/Rev869BControlPlaneDbContextModelSnapshot.cs`

The exhaustive architecture-complete candidate allowlist is therefore:

1. `SESS.NexaERP.slnx`
2. `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs`
3. `src/SESS.NexaERP.ControlPlane.Persistence/SESS.NexaERP.ControlPlane.Persistence.csproj`
4. `src/SESS.NexaERP.ControlPlane.Persistence/packages.lock.json`
5. `src/SESS.NexaERP.ControlPlane.Persistence/Rev869BA4ControlPlaneSchemaV1.cs`
6. `src/SESS.NexaERP.ControlPlane.Persistence/NpgsqlA4DurableControlPlanePersistenceProvider.cs`
7. `src/SESS.NexaERP.ControlPlane/SESS.NexaERP.ControlPlane.csproj`
8. `src/SESS.NexaERP.ControlPlane/Configuration/ControlPlaneOptions.cs`
9. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs`
10. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BExecutionBinding.cs`
11. `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs`
12. `src/SESS.NexaERP.ControlPlane/Endpoints/ControllerContractEndpointsV1.cs`
13. `src/SESS.NexaERP.ControlPlane/Program.cs`
14. `src/SESS.NexaERP.ControlPlane/Reconciliation/PinnedA4TargetResultProvider.cs`
15. `src/SESS.NexaERP.ControlPlane/Reconciliation/A4TerminalResultReconciliationService.cs`
16. `src/SESS.NexaERP.Infrastructure/SESS.NexaERP.Infrastructure.csproj`
17. `src/SESS.NexaERP.Infrastructure/DependencyInjection.cs`
18. `src/SESS.NexaERP.Infrastructure/Persistence/NpgsqlA4TargetExecutionProvider.cs`
19. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BA4TargetExecutionBoundarySql.cs`
20. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260821093000_Rev869BA4TargetExecutionBoundary.cs`
21. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260821093000_Rev869BA4TargetExecutionBoundary.Designer.cs`
22. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs`
23. `src/SESS.NexaERP.Api/SESS.NexaERP.Api.csproj`
24. `src/SESS.NexaERP.Api/Program.cs`
25. `src/SESS.NexaERP.Api/Endpoints/Rev869BA4TargetExecutionEndpoints.cs`
26. `tests/SESS.NexaERP.ControlPlane.Tests/SESS.NexaERP.ControlPlane.Tests.csproj`
27. `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs`
28. `tests/SESS.NexaERP.ControlPlane.Tests/A4FailureCorrectionContractTests.cs`
29. `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs`
30. `outputs/rev869b_external_controller_phase_a_a5_revised_source_implementation_checkpoint.md`
31. `src/SESS.NexaERP.ControlPlane.Persistence/Rev869BControlPlaneDbContext.cs`
32. `src/SESS.NexaERP.ControlPlane.Persistence/Rev869BControlPlaneDesignTimeDbContextFactory.cs`
33. `src/SESS.NexaERP.ControlPlane.Persistence/Migrations/20260821093000_Rev869BA4ControlPlaneInitial.cs`
34. `src/SESS.NexaERP.ControlPlane.Persistence/Migrations/20260821093000_Rev869BA4ControlPlaneInitial.Designer.cs`
35. `src/SESS.NexaERP.ControlPlane.Persistence/Migrations/Rev869BControlPlaneDbContextModelSnapshot.cs`

Maximum candidate path count: `35`.

A formula-only 32-path list could remove target paths 19-21 and add the five Control Plane paths, but it is rejected
as non-exhaustive because it has no migration for required target-local A5 state. The 35-path candidate is itself not
authorized: it conflicts with the requested count and needs a revised EF package/lock architecture decision.

## 10. Exact blockers and next gate

The gate is `NO_GO` for three exact reasons:

1. the requested count omits the mandatory target-local A5 migration;
2. the dedicated Control Plane DbContext requires five unnamed paths outside the frozen 30-path maximum; and
3. it changes the persistence adapter from the frozen direct-Npgsql-only graph to an EF Core provider graph whose
   exact packages, versions, lock closure and offline content identities have not been frozen.

No source, test, project, migration, snapshot, checkpoint or existing report was modified. Builds, restores, tests,
mutants and EF discovery were not run. Migration generation attempts/applications remain `0/0`; PostgreSQL
connections remain `0`; services, deployment, production, Phase B and Correction 2 remain `0`/not started.

The exact single next management gate is a report-only A5 dual-context and package-boundary architecture decision.
It must choose either:

- preserve the complete target and control schemas, accept `14 ERP + 1 Control Plane = 15`, freeze the 35-path
  boundary and verify the expanded EF lock closure offline; or
- preserve `13 ERP + 1 Control Plane = 14` only by explicitly redesigning how all target fence/result/audit/outbox
  state is supplied without a new ERP migration, with no cross-database ownership or weakened atomicity.

It must issue a new exact implementation baseline. A5 implementation must not restart automatically.

`A5_MIGRATION_CONTEXT_RECONCILIATION_GATE=NO_GO`

`MIGRATION_ATTEMPTS_APPLICATIONS=0/0`

`POSTGRESQL_CONNECTIONS=0`

`PHASE_B_STATE=NO_GO`

`CORRECTION_2_STATE=NO_GO`
