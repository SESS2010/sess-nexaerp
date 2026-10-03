# REV869B Phase-A A5 dual-context migration and EF package-boundary decision

Date: 2026-08-21

Decision type: report-only architecture/package boundary freeze

Starting HEAD: `08e2ad0a1ea24e2e7c8e6a6301bb37e530e5b4c8`

Expected parent: `cdd0ca6777e6c3981d3ec3ce3973fdaa2b78aef6`

## 1. Decision

`A5_DUAL_CONTEXT_MIGRATION_AND_PACKAGE_BOUNDARY_GATE=GO`

The complete design is frozen as two independent PostgreSQL/EF migration streams:

- ERP retains its 13 accepted migrations and adds one target-only A5 migration as ERP ordinal 14.
- Control Plane adds one independent initial migration in its dedicated persistence assembly.
- The combined inventory is 15, with no shared context, database, history, snapshot, migration or transaction.

The project/package design is exact and cycle-free. Implementation remains blocked until one separately authorized
controlled official package-verification gate verifies the 40 non-Npgsql archives in the union of the frozen Control
Plane and ERP EF closures and proves both local-only generated locks. `Npgsql 10.0.3` itself already has complete official trust
evidence. This `GO` approves architecture and bounding only; it does not authorize source implementation.

## 2. Stage 0

| Gate | Observed result | Status |
|---|---|---|
| HEAD | `08e2ad0a1ea24e2e7c8e6a6301bb37e530e5b4c8` | PASS |
| Parent | `cdd0ca6777e6c3981d3ec3ce3973fdaa2b78aef6` | PASS |
| Subject | `REV869B Phase-A A5 migration context reconciliation` | PASS |
| Branch | `master` | PASS |
| HEAD content | Exactly `outputs/rev869b_external_controller_phase_a_a5_migration_inventory_and_context_reconciliation.md` | PASS |
| Reconciliation SHA-256 | `AE92DA33A18944AC6942B752D8B91E55386BE14BD6599130D142C577A55CACF7` | PASS |
| Target tracked/index scope | Clean at entry | PASS |
| Legacy sibling | Remained the same untracked entry; no content was accessed or modified | PASS |

The authoritative Phase-A and A4 architecture/reconciliation reports; revised A5 architecture, immutable
action-manifest, mutant-harness and implementation blockers; persistence/classifier freeze; project/package graph;
official Npgsql identity report; and migration/context reconciliation were read completely. Project/package/source
inspection was read-only.

## 3. Frozen migration inventory

```text
erp_existing_migration_count=13
erp_a5_target_migration_count=1
erp_post_a5_migration_count=14
control_plane_initial_migration_count=1
control_plane_post_a5_migration_count=1
combined_solution_migration_count=15
rev869a_erp_ordinal=12
rev869b_erp_ordinal=13
a5_target_erp_ordinal=14
modified_existing_migration_count=0
cross_database_migration_count=0
migration_attempt_count=0
migration_application_count=0
```

The 13 existing ERP migration IDs remain, in order:

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

Every existing migration primary/designer file remains byte-for-byte immutable.

## 4. Context, assembly and database ownership

### 4.1 ERP target stream

- Context: `SESS.NexaERP.Infrastructure.Persistence.NexaErpDbContext`.
- Project/migration assembly: `SESS.NexaERP.Infrastructure`.
- Snapshot: `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/NexaErpDbContextModelSnapshot.cs`.
- Existing design factory: `SESS.NexaERP.Infrastructure.Persistence.NexaErpDesignTimeDbContextFactory`.
- Runtime configuration owner: ERP API via `AddInfrastructure`, using `ConnectionStrings:NexaErp`.
- Design configuration: `ConnectionStrings__NexaErp` plus `NexaErp__ExpectedDatabase` with exact database-name match.
- History: provider `__EFMigrationsHistory` physically owned only by the ERP database/connection.
- New migration: `20260821093000_Rev869BA4TargetExecutionBoundary`, ERP ordinal 14.
- Owned state: target fencing watermark; target execution idempotency/result; immutable terminal receipt; target audit
  and target outbox; target-local ACL/functions required to commit these facts with Purchase work.
- Excluded state: Control Plane grant, lease allocation, lifecycle, controller replay, control audit/outbox.

The ERP snapshot and `NexaErpDbContext.Rev869B.cs` are now explicit future paths because the instruction requires an
updated target model/snapshot, not merely an unmodeled raw-SQL migration. Entity/configuration types remain internal
to Infrastructure and may be defined in these already-bounded Infrastructure files; no Domain/Application contract
changes are allowed.

### 4.2 Control Plane stream

- Context: `SESS.NexaERP.ControlPlane.Persistence.Rev869BControlPlaneDbContext`.
- Project/migration assembly: `SESS.NexaERP.ControlPlane.Persistence`.
- Snapshot: `src/SESS.NexaERP.ControlPlane.Persistence/Migrations/Rev869BControlPlaneDbContextModelSnapshot.cs`.
- Design factory: `SESS.NexaERP.ControlPlane.Persistence.Rev869BControlPlaneDesignTimeDbContextFactory`.
- Initial migration: `20260821093000_Rev869BA4ControlPlaneInitial`.
- Runtime configuration owner: Control Plane composition root, using
  `ConnectionStrings:Rev869BControlPlane`; ERP API/Infrastructure never receive this value.
- Design configuration: `ConnectionStrings__Rev869BControlPlane` plus
  `Rev869BControlPlane__ExpectedDatabase`, with exact database-name match.
- History: explicitly `control.__EFMigrationsHistory`, physically owned only by the Control Plane database.
- Owned state: authorization/grant, lease/fence allocation, lifecycle/version, controller idempotency/result,
  dispatch/reconciliation, control audit and control outbox.
- Excluded state: Purchase/business/history rows and all target fence/result/audit/outbox state.

### 4.3 Mandatory separation

The streams have separate projects, assemblies, contexts, factories, snapshots, connections, expected-database
checks, histories, SQL artifacts and hashes. No connection, `DbContext`, migration, table, foreign key or transaction
crosses databases. Runtime flow remains `control commit -> target commit -> control reconciliation commit`; it is a
fenced idempotent saga, not distributed ACID.

## 5. Frozen direct package graph

The new `SESS.NexaERP.ControlPlane.Persistence.csproj` owns exactly these direct package references:

| Direct package | Version | Metadata |
|---|---:|---|
| `Microsoft.EntityFrameworkCore` | `10.0.10` | normal compile/runtime assets |
| `Microsoft.EntityFrameworkCore.Design` | `10.0.10` | `PrivateAssets=all`; `IncludeAssets=runtime; build; native; contentfiles; analyzers; buildtransitive` |
| `Npgsql` | `10.0.3` | direct ADO.NET provider retained for the durable adapter |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | `10.0.3` | EF migrations/context provider |

These versions are not invented. The committed ERP Infrastructure project directly pins EF Core and Design
`10.0.10` and Npgsql EF provider `10.0.3`; its resolved net10.0 graph proves the provider resolves with EF
Relational `10.0.10` and Npgsql `10.0.3`. The SDK remains `10.0.302` with `latestFeature` roll-forward.

No `Microsoft.EntityFrameworkCore.Tools` project package is added: the installed `dotnet ef` tool is external build
tooling. Contracts, Control Plane executable, API, Acceptance Verifier and both test projects gain no direct EF/Npgsql
package. Control Plane tests consume the persistence project via `ProjectReference`; production never references a
test project.

Central package management remains absent and prohibited. No repository `NuGet.Config` is added. Package versions
remain explicit in the owner project.

## 6. Exact expected 35-package net10.0 closure

The closure below is derived from the locally restored `project.assets.json` for the committed Infrastructure project
and exact four roots. It is
expected evidence only until regenerated in a clean controlled local-source restore:

| Package | Version | Role |
|---|---:|---|
| `Microsoft.EntityFrameworkCore` | `10.0.10` | direct |
| `Microsoft.EntityFrameworkCore.Design` | `10.0.10` | direct/design-private |
| `Npgsql` | `10.0.3` | direct |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | `10.0.3` | direct |
| `Humanizer.Core` | `2.14.1` | transitive |
| `Microsoft.Build.Framework` | `18.0.2` | transitive |
| `Microsoft.CodeAnalysis.Analyzers` | `3.11.0` | transitive |
| `Microsoft.CodeAnalysis.Common` | `5.0.0` | transitive |
| `Microsoft.CodeAnalysis.CSharp` | `5.0.0` | transitive |
| `Microsoft.CodeAnalysis.CSharp.Workspaces` | `5.0.0` | transitive |
| `Microsoft.CodeAnalysis.Workspaces.Common` | `5.0.0` | transitive |
| `Microsoft.CodeAnalysis.Workspaces.MSBuild` | `5.0.0` | transitive |
| `Microsoft.EntityFrameworkCore.Abstractions` | `10.0.10` | transitive |
| `Microsoft.EntityFrameworkCore.Analyzers` | `10.0.10` | transitive |
| `Microsoft.EntityFrameworkCore.Relational` | `10.0.10` | transitive |
| `Microsoft.Extensions.Caching.Abstractions` | `10.0.10` | transitive |
| `Microsoft.Extensions.Caching.Memory` | `10.0.10` | transitive |
| `Microsoft.Extensions.Configuration.Abstractions` | `10.0.10` | transitive |
| `Microsoft.Extensions.DependencyInjection` | `10.0.10` | transitive |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `10.0.10` | transitive |
| `Microsoft.Extensions.DependencyModel` | `10.0.10` | transitive |
| `Microsoft.Extensions.Logging` | `10.0.10` | transitive |
| `Microsoft.Extensions.Logging.Abstractions` | `10.0.10` | transitive |
| `Microsoft.Extensions.Options` | `10.0.10` | transitive |
| `Microsoft.Extensions.Primitives` | `10.0.10` | transitive |
| `Microsoft.VisualStudio.SolutionPersistence` | `1.0.52` | transitive |
| `Mono.TextTemplating` | `3.0.0` | transitive |
| `Newtonsoft.Json` | `13.0.3` | transitive |
| `System.CodeDom` | `6.0.0` | transitive |
| `System.Composition` | `9.0.0` | transitive |
| `System.Composition.AttributedModel` | `9.0.0` | transitive |
| `System.Composition.Convention` | `9.0.0` | transitive |
| `System.Composition.Hosting` | `9.0.0` | transitive |
| `System.Composition.Runtime` | `9.0.0` | transitive |
| `System.Composition.TypedParts` | `9.0.0` | transitive |

No package outside these 35 may enter the Control Plane lock. Any version or closure difference returns the later
implementation to blocker-only handling.

## 7. ERP package graph and lock ownership

ERP Infrastructure retains its four committed direct references:

- `Microsoft.EntityFrameworkCore 10.0.10`;
- `Microsoft.EntityFrameworkCore.Design 10.0.10`, with the same private/include-assets metadata frozen above;
- `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore 10.0.10`; and
- `Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3`.

Its exact resolved closure is the 35-package set in section 6 plus these six packages, for 41 total:

| ERP-only closure addition | Version | Recorded NuGet contentHash |
|---|---:|---|
| `Microsoft.Extensions.Diagnostics.Abstractions` | `10.0.10` | `9uWiKpeOVac355STyChWR/pliFX/5CeLqChW9kKsaxyDH4EUTZxMkT4Jwp/J/peLm0GBFmSX5c0WCse3yCnq1Q==` |
| `Microsoft.Extensions.Diagnostics.HealthChecks` | `10.0.10` | `R0O5oG+zAJeBSM8nNTa+Ycj2Zobyr/v6Ilo7Dha0sNB2Vq/XXoLdoecj9DAWGbN8YrPaW6u8+osTQ5Ypj7ZF0w==` |
| `Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions` | `10.0.10` | `6euxgVR7NS83y0a2wLRAxfYXusLQJ2e1ah0MpQgYTYMs5lYrmdNP79C6T8uvRZdP87n5mcCcp6+w0EyWAidKZw==` |
| `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` | `10.0.10` | `jgYLn+CG1/EgZ3lsAuRUvb0RFhr1q23/z4U85arQ8XgABVZAuJIVecLaShMxsB/AS0ufY+Z4OCv1facaiEyc5g==` |
| `Microsoft.Extensions.FileProviders.Abstractions` | `10.0.10` | `c5zqFCY9DiIpMovLd7/d/CTiEtrMOuQ639dhv3PABtKQIKNQikSHwQt8+N679uii9q+B55lgK28Uv64FOwEu8w==` |
| `Microsoft.Extensions.Hosting.Abstractions` | `10.0.10` | `5LugpYGHk+mkn0a8IZgcyfBca8PCTAU9RQFoMrTdtOOidq88M2SI5f3px6ugnzgxC+eTkvYYJi8pzlUnG5xdAQ==` |

Two project-local locks are required:

- `src/SESS.NexaERP.ControlPlane.Persistence/packages.lock.json`: exactly 35 packages;
- `src/SESS.NexaERP.Infrastructure/packages.lock.json`: exactly 41 packages.

Both owner projects set `RestorePackagesWithLockFile=true`. After controlled materialization, all acceptance restores
use `--locked-mode`, an explicit disposable local source containing exactly the verified union, an empty isolated
packages directory and HTTP cache, and zero fallback sources/DNS/HTTP. Lock bytes must remain stable after a second
fresh replay. All later builds/tests/discovery use `--no-restore` or `--no-build` as applicable.

## 8. Package identity and trust boundary

Already officially verified:

- `Npgsql 10.0.3` archive SHA-256
  `75D0970923A8C9FCBBD37E4EBE72FEE0B10362A1E36723E86777DF1B6728316D`;
- raw archive SHA-512 Base64
  `9wZYlR6r1uLGnAxBuUPOCmgbS6SDtVKyGkZ0tDGPaBHYbbdEDEajD0VErq+W/dn3gHfJ3WvOdZaNNNybnFCupA==`;
- NuGet lock/content hash
  `7nb5YzXuvWWJxB0J8DiyL3we+X4FOctZrt0fIBnucOIaIevFEEwGQVZKtiu9olXdlNAK1eNgqSral6r/jlhI4w==`;
- repository signer `C72FE7739A9EECB8EC1E4F596DB3BB74039B1DE2`; and
- timestamp signer `DD6230AC860A2D306BDA38B16879523007FB417E`.

The locally restored assets graph for the committed project records versions and NuGet content hashes for every other closure member, including
`Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3` contentHash
`IPGrrZnRkuW7OlHDhUESZz4G5DLkW7Nej/O3Cx+0iTsgyU5XJxBgpsvTHLloo3WWuAKKbDHXBvWPVkX1deRh1Q==` and EF Core
`10.0.10` contentHash `a0V7zj/VbYP6dTdWpUgE/r2PuLKtUGe2aJ0lVKkn/wP9ZhaxUz2kQydVfvOjCv2SKxlrqdBfHhPD4Cvlf+4ffA==`.
Generated assets are compatibility evidence, not independent trust evidence.

One later controlled official package-verification gate must verify all 40 non-Npgsql packages in the 41-package
union. For each exact ID/version it must record official archive length, raw SHA-256, raw SHA-512, NuGet contentHash,
repository/author signature and timestamp/chain/revocation result; byte-compare official and cached archives; create
an exact local source; generate both locks; and prove two fresh network-disabled locked restores. No implementation
may begin until that gate is `GO`. Any changed version, extra dependency, signature failure or content mismatch is a
package blocker, not authority to select a substitute.

## 9. Project-reference and composition graph

The previously frozen acyclic edges remain exact:

- `ControlPlane.Persistence -> ControlPlane.Contracts`;
- `ControlPlane -> ControlPlane.Persistence` and `ControlPlane.Contracts`;
- `Infrastructure -> ControlPlane.Contracts`, Application and Domain;
- `Api -> ControlPlane.Contracts`, Infrastructure, Application and Domain;
- Control Plane tests -> Contracts, Control Plane, Persistence, Acceptance Verifier, API and Infrastructure.

The solution adds Contracts, Persistence, Control Plane, Acceptance Verifier and Control Plane tests. No production
project references tests. Control Plane never references ERP API/Infrastructure; Infrastructure never references the
Control Plane executable/persistence project. API remains the target composition root and Control Plane remains the
control composition root.

## 10. Revised exhaustive implementation allowlist

The exact future boundary contains 39 named paths because checkpoint and blocker are both named alternatives. They
are mutually exclusive, so the maximum paths changed/committed by one implementation attempt is exactly **38**.

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
17. `src/SESS.NexaERP.Infrastructure/packages.lock.json`
18. `src/SESS.NexaERP.Infrastructure/DependencyInjection.cs`
19. `src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.Rev869B.cs`
20. `src/SESS.NexaERP.Infrastructure/Persistence/NpgsqlA4TargetExecutionProvider.cs`
21. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BA4TargetExecutionBoundarySql.cs`
22. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260821093000_Rev869BA4TargetExecutionBoundary.cs`
23. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260821093000_Rev869BA4TargetExecutionBoundary.Designer.cs`
24. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/NexaErpDbContextModelSnapshot.cs`
25. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs`
26. `src/SESS.NexaERP.Api/SESS.NexaERP.Api.csproj`
27. `src/SESS.NexaERP.Api/Program.cs`
28. `src/SESS.NexaERP.Api/Endpoints/Rev869BA4TargetExecutionEndpoints.cs`
29. `tests/SESS.NexaERP.ControlPlane.Tests/SESS.NexaERP.ControlPlane.Tests.csproj`
30. `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs`
31. `tests/SESS.NexaERP.ControlPlane.Tests/A4FailureCorrectionContractTests.cs`
32. `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs`
33. `src/SESS.NexaERP.ControlPlane.Persistence/Rev869BControlPlaneDbContext.cs`
34. `src/SESS.NexaERP.ControlPlane.Persistence/Rev869BControlPlaneDesignTimeDbContextFactory.cs`
35. `src/SESS.NexaERP.ControlPlane.Persistence/Migrations/20260821093000_Rev869BA4ControlPlaneInitial.cs`
36. `src/SESS.NexaERP.ControlPlane.Persistence/Migrations/20260821093000_Rev869BA4ControlPlaneInitial.Designer.cs`
37. `src/SESS.NexaERP.ControlPlane.Persistence/Migrations/Rev869BControlPlaneDbContextModelSnapshot.cs`
38. `outputs/rev869b_external_controller_phase_a_a5_revised_source_implementation_checkpoint.md`
39. `outputs/rev869b_external_controller_phase_a_a5_revised_source_implementation_blocker_3.md`

Paths 38 and 39 cannot coexist. A successful run uses path 38; a stopped/failed run reverts all implementation
changes and uses only path 39. The maximum is 38, not 39. Every existing report/checkpoint and every unnamed file
remains immutable.

## 11. Future migration discovery, parity and SQL evidence

After package verification, an authorized implementation and offline warning-as-error build, future validation uses
non-routable design-only values and no real credential.

ERP discovery and parity:

```powershell
$env:ConnectionStrings__NexaErp='Host=127.0.0.1;Port=1;Database=rev869b_erp_design_only;Username=design_only;Timeout=1;Command Timeout=1'
$env:NexaErp__ExpectedDatabase='rev869b_erp_design_only'
dotnet ef migrations list --context SESS.NexaERP.Infrastructure.Persistence.NexaErpDbContext --project src/SESS.NexaERP.Infrastructure/SESS.NexaERP.Infrastructure.csproj --startup-project src/SESS.NexaERP.Api/SESS.NexaERP.Api.csproj --no-build --no-connect
dotnet ef migrations has-pending-model-changes --context SESS.NexaERP.Infrastructure.Persistence.NexaErpDbContext --project src/SESS.NexaERP.Infrastructure/SESS.NexaERP.Infrastructure.csproj --startup-project src/SESS.NexaERP.Api/SESS.NexaERP.Api.csproj --no-build
```

Control Plane discovery and parity:

```powershell
$env:ConnectionStrings__Rev869BControlPlane='Host=127.0.0.1;Port=1;Database=rev869b_control_design_only;Username=design_only;Timeout=1;Command Timeout=1'
$env:Rev869BControlPlane__ExpectedDatabase='rev869b_control_design_only'
dotnet ef migrations list --context SESS.NexaERP.ControlPlane.Persistence.Rev869BControlPlaneDbContext --project src/SESS.NexaERP.ControlPlane.Persistence/SESS.NexaERP.ControlPlane.Persistence.csproj --startup-project src/SESS.NexaERP.ControlPlane/SESS.NexaERP.ControlPlane.csproj --no-build --no-connect
dotnet ef migrations has-pending-model-changes --context SESS.NexaERP.ControlPlane.Persistence.Rev869BControlPlaneDbContext --project src/SESS.NexaERP.ControlPlane.Persistence/SESS.NexaERP.ControlPlane.Persistence.csproj --startup-project src/SESS.NexaERP.ControlPlane/SESS.NexaERP.ControlPlane.csproj --no-build
```

EF 10 `migrations list --no-connect` must enumerate exactly 14 ERP and 1 Control Plane migration. The parity commands
must report no pending model changes; they compare runtime model/snapshot without a database query.

EF 10 `migrations script` has no connection option and generates from migration metadata without querying a database.
Each command runs twice in fresh disposable directories:

```powershell
dotnet ef migrations script 20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation 20260821093000_Rev869BA4TargetExecutionBoundary --context SESS.NexaERP.Infrastructure.Persistence.NexaErpDbContext --project src/SESS.NexaERP.Infrastructure/SESS.NexaERP.Infrastructure.csproj --startup-project src/SESS.NexaERP.Api/SESS.NexaERP.Api.csproj --no-build --output <erp-up.sql>
dotnet ef migrations script 20260821093000_Rev869BA4TargetExecutionBoundary 20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation --context SESS.NexaERP.Infrastructure.Persistence.NexaErpDbContext --project src/SESS.NexaERP.Infrastructure/SESS.NexaERP.Infrastructure.csproj --startup-project src/SESS.NexaERP.Api/SESS.NexaERP.Api.csproj --no-build --output <erp-down.sql>
dotnet ef migrations script 0 20260821093000_Rev869BA4ControlPlaneInitial --context SESS.NexaERP.ControlPlane.Persistence.Rev869BControlPlaneDbContext --project src/SESS.NexaERP.ControlPlane.Persistence/SESS.NexaERP.ControlPlane.Persistence.csproj --startup-project src/SESS.NexaERP.ControlPlane/SESS.NexaERP.ControlPlane.csproj --no-build --output <control-up.sql>
dotnet ef migrations script 20260821093000_Rev869BA4ControlPlaneInitial 0 --context SESS.NexaERP.ControlPlane.Persistence.Rev869BControlPlaneDbContext --project src/SESS.NexaERP.ControlPlane.Persistence/SESS.NexaERP.ControlPlane.Persistence.csproj --startup-project src/SESS.NexaERP.ControlPlane/SESS.NexaERP.ControlPlane.csproj --no-build --output <control-down.sql>
```

The four output placeholders must resolve inside the disposable evidence directory, never the repository. Up/Down
bytes, line counts and SHA-256 must match between processes; schema/object/ACL inventories must be disjoint. Any
socket open or database query fails validation.

## 12. Complete future validation and stop rules

A separately authorized source attempt must prove:

1. ERP discovery `14`, Control Plane discovery `1`, combined `15`, with ordinals `12/13/14` exact;
2. existing migration primary/designer hashes unchanged and modified-existing count zero;
3. two snapshot parity checks pass and two migration histories/assemblies remain disjoint;
4. offline SQL/Down SQL hashes reproduce independently with zero connections/applications;
5. both project locks have exact counts/content hashes and replay from only the verified local source with no drift;
6. exact project/package/solution graph, no cycle, no production-to-test edge and no cross-database dependency;
7. warning-as-error offline builds and complete authorized contract/security/privacy scans pass;
8. exactly `30/30` A5 tests and all `23/23` retained A4 tests pass, plus complete offline suites;
9. all 40 semantic production mutants compile and are killed from clean isolated Git baselines, with zero survivors,
   invalids, duplicate diffs or restoration residue; and
10. PostgreSQL connections/tests, migration attempts/applications, services, deployment, production, Phase B and
    Correction 2 all remain zero/not started.

Mandatory blocker-only stop applies if a package/archive/lock differs, an unnamed path is required, either migration
enters the wrong assembly/database, an existing migration changes, model parity fails, a cross-database context or
transaction appears, any build/test/mutant fails, or any prohibited operation becomes necessary.

## 13. Next gate and retained prohibitions

This report modified no source, test, project, package, migration, snapshot, checkpoint or existing report. It ran no
build, restore, test, mutant, EF discovery, migration generation/application, PostgreSQL operation, service,
deployment, production action, Phase B or Correction 2. Migration attempts/applications remain `0/0`.

The exact single next management gate is one controlled, report-only official verification and offline-lock
materialization decision for the 40 non-Npgsql packages in the frozen 41-package union. It may use external package
sources only if separately and explicitly authorized. It must end with one report-only commit and a new exact source
implementation baseline; it must not start A5 implementation automatically.

`A5_DUAL_CONTEXT_MIGRATION_AND_PACKAGE_BOUNDARY_GATE=GO`

`A5_SOURCE_IMPLEMENTATION_STATE=BLOCKED_PENDING_EF_PACKAGE_TRUST_GATE`

`MIGRATION_ATTEMPTS_APPLICATIONS=0/0`

`POSTGRESQL_CONNECTIONS=0`

`PHASE_B_STATE=NO_GO`

`CORRECTION_2_STATE=NO_GO`
