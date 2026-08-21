# REV869B Option-A Phase-A A5 project-graph and implementation-boundary reconciliation

Date: 2026-08-21

Decision type: report-only project-graph and boundary reconciliation; no implementation authority

Starting HEAD: `fa00a1dc1c4ed4ff5a30ee58d2a5dd008fd182c4`

Expected parent: `0a787aa2a9f3a98ca877e86dde4587fd48f49505`

## 1. Gate result

`A5_ARCHITECTURE_FREEZE_REQUIRED`

The ERP target-side graph has a minimum acyclic solution: ERP Infrastructure may implement the existing
`ITargetAuthorizedPlanExecutionProvider` contract by adding a reference to Control Plane Contracts; ERP API may
compose that provider by directly referencing Contracts; and Control Plane tests may directly reference API and
Infrastructure for production-graph tests. The fixed 19 Purchase actions and the internal transaction-enlistment
seam require no additional project edge because Infrastructure already references Application and Domain.

The complete A5 graph is not reproducible from the committed project manifests without a new architecture or
package decision. The authorized Control Plane source includes a concrete
`NpgsqlA4DurableControlPlanePersistenceProvider`, but
`src/SESS.NexaERP.ControlPlane/SESS.NexaERP.ControlPlane.csproj` references only Control Plane Contracts and declares
no Npgsql package. Its existing `obj/project.assets.json` mentions `Npgsql/10.0.3`, but that generated, stale restore
artifact is not a committed dependency declaration and cannot make a clean offline checkout reproducible.

There are only three technical ways to close that gap:

1. add a direct Npgsql package dependency to Control Plane;
2. make Control Plane directly reference ERP Infrastructure; or
3. add a dedicated control-plane PostgreSQL adapter/project.

Option 1 is a new package dependency not frozen by the current manifests. Option 2 improperly couples the
controller host to the ERP implementation/deployment boundary. Option 3 adds a project/adapter and deployment
component. The instruction requires `A5_ARCHITECTURE_FREEZE_REQUIRED` for the latter two conditions and permits GO
only when no new package dependency is required. Therefore no implementation may start from this report.

## 2. Stage-0 evidence

| Check | Verified result | Status |
|---|---|---|
| HEAD | `fa00a1dc1c4ed4ff5a30ee58d2a5dd008fd182c4` | PASS |
| Parent | `0a787aa2a9f3a98ca877e86dde4587fd48f49505` | PASS |
| Branch | `master` | PASS |
| HEAD subject | `REV869B Phase-A A5 revised mutant harness reconciliation` | PASS |
| HEAD boundary | Exactly one added file: `target-dotnet/outputs/rev869b_external_controller_phase_a_a5_revised_mutant_harness_failure_reconciliation.md` | PASS |
| Required report SHA-256 | `83BEA9C48C26D8A209B472EFC5D63B1565CD01B8ED5A8B88BC1D1FD6ED3345C4` | PASS |
| Target tracked/index diff | Empty | PASS |
| Target-local untracked scope | Empty | PASS |
| Implementation changes | None | PASS |
| Legacy sibling | Git reports the pre-existing `../legacy-reference/` entry only; its contents were not accessed, enumerated or modified | PASS |

The architecture freeze, A4 failure reconciliation, immutable A5 action-contract/boundary decision, original and
revised blocker checkpoints, mutant-gate reconciliation, and revised mutant-harness reconciliation were read
completely. No build, restore, test, mutant, PostgreSQL, migration, service, deployment or production action was run.

## 3. Exact current solution membership

`SESS.NexaERP.slnx` currently contains:

- `src/SESS.NexaERP.Api/SESS.NexaERP.Api.csproj`
- `src/SESS.NexaERP.Application/SESS.NexaERP.Application.csproj`
- `src/SESS.NexaERP.Domain/SESS.NexaERP.Domain.csproj`
- `src/SESS.NexaERP.Infrastructure/SESS.NexaERP.Infrastructure.csproj`
- `tests/SESS.NexaERP.Tests/SESS.NexaERP.Tests.csproj`

It currently omits Contracts, Control Plane, Acceptance Verifier and Control Plane tests. A future authorized
implementation would add these four existing project entries to the solution:

- `src/SESS.NexaERP.ControlPlane.Contracts/SESS.NexaERP.ControlPlane.Contracts.csproj`
- `src/SESS.NexaERP.ControlPlane/SESS.NexaERP.ControlPlane.csproj`
- `src/SESS.NexaERP.AcceptanceVerifier/SESS.NexaERP.AcceptanceVerifier.csproj`
- `tests/SESS.NexaERP.ControlPlane.Tests/SESS.NexaERP.ControlPlane.Tests.csproj`

Solution membership is build orchestration, not a `ProjectReference` edge.

## 4. Exact current project dependency graph

An arrow means the project on the left has the exact `ProjectReference Include` on the right.

| Project | Exact current direct production/project references |
|---|---|
| Control Plane Contracts | none |
| Control Plane | `..\SESS.NexaERP.ControlPlane.Contracts\SESS.NexaERP.ControlPlane.Contracts.csproj` |
| Acceptance Verifier | `..\SESS.NexaERP.ControlPlane.Contracts\SESS.NexaERP.ControlPlane.Contracts.csproj` |
| ERP API | `..\SESS.NexaERP.Domain\SESS.NexaERP.Domain.csproj`; `..\SESS.NexaERP.Application\SESS.NexaERP.Application.csproj`; `..\SESS.NexaERP.Infrastructure\SESS.NexaERP.Infrastructure.csproj` |
| ERP Application | `..\SESS.NexaERP.Domain\SESS.NexaERP.Domain.csproj` |
| ERP Infrastructure | `..\SESS.NexaERP.Application\SESS.NexaERP.Application.csproj`; `..\SESS.NexaERP.Domain\SESS.NexaERP.Domain.csproj` |
| Domain | none |
| Control Plane tests | `..\..\src\SESS.NexaERP.ControlPlane.Contracts\SESS.NexaERP.ControlPlane.Contracts.csproj`; `..\..\src\SESS.NexaERP.ControlPlane\SESS.NexaERP.ControlPlane.csproj`; `..\..\src\SESS.NexaERP.AcceptanceVerifier\SESS.NexaERP.AcceptanceVerifier.csproj` |
| ERP tests | `..\..\src\SESS.NexaERP.Api\SESS.NexaERP.Api.csproj`; `..\..\src\SESS.NexaERP.Domain\SESS.NexaERP.Domain.csproj`; `..\..\src\SESS.NexaERP.Application\SESS.NexaERP.Application.csproj`; `..\..\src\SESS.NexaERP.Infrastructure\SESS.NexaERP.Infrastructure.csproj` |

The current production graph is acyclic:

`API -> Infrastructure -> Application -> Domain`

`API -> Application -> Domain`

`API -> Domain`

`ControlPlane -> Contracts`

`AcceptanceVerifier -> Contracts`

Contracts and Domain are leaves. Production projects have no test-project references.

## 5. Minimum exact safe ProjectReference changes

### 5.1 Edges to add

| Modified `.csproj` path | Exact `ProjectReference Include` to add | Required for | Permitted direction and reason |
|---|---|---|---|
| `src/SESS.NexaERP.Infrastructure/SESS.NexaERP.Infrastructure.csproj` | `..\SESS.NexaERP.ControlPlane.Contracts\SESS.NexaERP.ControlPlane.Contracts.csproj` | Typed Infrastructure target provider | Infrastructure implements the existing target execution/result contracts. Contracts remains independent of Infrastructure. |
| `src/SESS.NexaERP.Api/SESS.NexaERP.Api.csproj` | `..\SESS.NexaERP.ControlPlane.Contracts\SESS.NexaERP.ControlPlane.Contracts.csproj` | Signed target endpoint and explicit composition | API consumes shared protocol types directly. The direct edge avoids relying on a transitive compile reference and does not give Contracts knowledge of API. |
| `tests/SESS.NexaERP.ControlPlane.Tests/SESS.NexaERP.ControlPlane.Tests.csproj` | `..\..\src\SESS.NexaERP.Api\SESS.NexaERP.Api.csproj` | API production-graph resolution tests | Test-to-production only; API never references the test assembly. |
| `tests/SESS.NexaERP.ControlPlane.Tests/SESS.NexaERP.ControlPlane.Tests.csproj` | `..\..\src\SESS.NexaERP.Infrastructure\SESS.NexaERP.Infrastructure.csproj` | Concrete Infrastructure provider and production-graph tests | Test-to-production only; Infrastructure never references the test assembly. |

### 5.2 Edges to remove

None. Every current `ProjectReference` is directionally valid.

### 5.3 Requirements that need no new ProjectReference

- The fixed 19 server-owned Purchase actions need no additional edge. Infrastructure already references Application,
  where `IRev869BPurchaseService` and its immutable request/result contracts live, and Domain for Purchase entities
  and business rules.
- The approved Phase-A transaction-enlistment seam needs no additional edge. Both the target provider and
  `EfRev869BPurchaseService` are in ERP Infrastructure and share the same `NexaErpDbContext`/target transaction.
- The Control Plane durable provider should implement `IDurableControlPlanePersistenceProvider` from Contracts within
  the Control Plane deployment boundary. This needs a provider dependency decision, not an ERP project reference.

### 5.4 Unsafe or unapproved edges

- Do not add `ControlPlane -> Infrastructure`. It would make the controller deployment directly own/depend on ERP
  persistence, collapse the independent control/target boundary, and violate the explicit return condition.
- Do not add `ControlPlane -> Api`. API is the ERP target composition root, not a reusable control-plane adapter.
- Do not add `Infrastructure -> ControlPlane`. The target provider must not depend on controller implementation.
- Do not add any production-to-test edge.

## 6. Target classifier ownership

The existing `Rev869BL1BoundaryStateMachine.RequireTargetExecution` enforcement point is implemented in the Control
Plane assembly. ERP Infrastructure cannot call it without the unsafe `Infrastructure -> ControlPlane` edge.

The target provider belongs in ERP Infrastructure behind the existing
`ITargetAuthorizedPlanExecutionProvider`/`IAuthoritativeTargetResultProvider` contracts. Its structured fence,
lease, idempotency and result checks must not be copied into a second divergent implementation. A shared pure
classifier could be moved into Control Plane Contracts and used by both production assemblies, but moving
security-enforcement ownership out of the already frozen Control Plane state-machine location is an architecture
decision. Alternatively, a new adapter/project could own shared enforcement, which is also an architecture-freeze
condition.

Consequently the safe `Infrastructure -> Contracts` edge is necessary but, by itself, is not sufficient to make the
frozen implementation reproducible. Management must freeze the single-classifier owner and its allowed source path
before source work.

## 7. Composition root, ownership, security and deployment assessment

| Question | Determination |
|---|---|
| Dependency direction/circular risk | The four safe additions in section 5 are acyclic. Any `Infrastructure -> ControlPlane` edge is rejected. |
| May Control Plane own or directly depend on ERP Infrastructure? | No. Controller persistence and ERP target persistence are separately owned/deployed. |
| Where does the typed target provider belong? | ERP Infrastructure, implementing interfaces in Contracts and invoking immutable Application Purchase contracts. |
| Is API only the ERP composition root? | Yes. API composes ERP Infrastructure for the target runtime. The Control Plane host composes only its own controller graph and must not compose ERP Infrastructure. |
| Test dependency direction | Both test assemblies may reference production projects. No production assembly may reference either test assembly. |
| Frozen ownership/security/topology | The four safe edges do not change them. Direct Control Plane-to-Infrastructure coupling, moving the classifier, adding a package, or adding an adapter/project does require an explicit freeze. |
| Is a new adapter/project required? | It is one valid way to isolate the control-plane PostgreSQL provider, but is not authorized. A direct Control Plane Npgsql package is another possible decision. At least one new dependency/architecture decision is unavoidable. |
| Phase-A scope | No Phase B, Correction 2, deployment, provisioning or production capability is introduced by this reconciliation. |

## 8. Exact project files requiring modification after architecture resolution

The minimum project-file set for the safe target/test graph is exactly:

1. `src/SESS.NexaERP.Infrastructure/SESS.NexaERP.Infrastructure.csproj`
2. `src/SESS.NexaERP.Api/SESS.NexaERP.Api.csproj`
3. `tests/SESS.NexaERP.ControlPlane.Tests/SESS.NexaERP.ControlPlane.Tests.csproj`

The already-listed `src/SESS.NexaERP.ControlPlane/SESS.NexaERP.ControlPlane.csproj` also requires a separately frozen
resolution for its concrete Npgsql provider. It must not be changed under this report. No Application, Domain,
Contracts, Acceptance Verifier or ERP-tests `.csproj` change is required.

`SESS.NexaERP.slnx` requires the four solution-membership additions in section 3, but it is not a `.csproj`.

## 9. Conditional revised exhaustive implementation allowlist

No implementation is authorized because the architecture/package blockers above are unresolved. If management first
creates a separate architecture decision that selects the control-plane provider dependency and single target
classifier owner without expanding source responsibility, the following is the exact minimum candidate allowlist.
It replaces the prior checkpoint path, adds the omitted API project file, and has a maximum of **28 paths**:

1. `SESS.NexaERP.slnx`
2. `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs`
3. `src/SESS.NexaERP.ControlPlane/SESS.NexaERP.ControlPlane.csproj`
4. `src/SESS.NexaERP.ControlPlane/Configuration/ControlPlaneOptions.cs`
5. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs`
6. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BExecutionBinding.cs`
7. `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs`
8. `src/SESS.NexaERP.ControlPlane/Endpoints/ControllerContractEndpointsV1.cs`
9. `src/SESS.NexaERP.ControlPlane/Program.cs`
10. `src/SESS.NexaERP.ControlPlane/Persistence/Rev869BA4ControlPlaneSchemaV1.cs` — NEW
11. `src/SESS.NexaERP.ControlPlane/Persistence/NpgsqlA4DurableControlPlanePersistenceProvider.cs` — NEW
12. `src/SESS.NexaERP.ControlPlane/Reconciliation/PinnedA4TargetResultProvider.cs` — NEW
13. `src/SESS.NexaERP.ControlPlane/Reconciliation/A4TerminalResultReconciliationService.cs` — NEW
14. `src/SESS.NexaERP.Infrastructure/SESS.NexaERP.Infrastructure.csproj`
15. `src/SESS.NexaERP.Infrastructure/DependencyInjection.cs`
16. `src/SESS.NexaERP.Infrastructure/Persistence/NpgsqlA4TargetExecutionProvider.cs` — NEW
17. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BA4TargetExecutionBoundarySql.cs` — NEW
18. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260821093000_Rev869BA4TargetExecutionBoundary.cs` — NEW
19. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260821093000_Rev869BA4TargetExecutionBoundary.Designer.cs` — NEW
20. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs`
21. `src/SESS.NexaERP.Api/SESS.NexaERP.Api.csproj`
22. `src/SESS.NexaERP.Api/Program.cs`
23. `src/SESS.NexaERP.Api/Endpoints/Rev869BA4TargetExecutionEndpoints.cs` — NEW
24. `tests/SESS.NexaERP.ControlPlane.Tests/SESS.NexaERP.ControlPlane.Tests.csproj`
25. `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs`
26. `tests/SESS.NexaERP.ControlPlane.Tests/A4FailureCorrectionContractTests.cs` — NEW
27. `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs`
28. `outputs/rev869b_external_controller_phase_a_a5_revised_source_implementation_checkpoint.md` — NEW

The exact future checkpoint path is:

`outputs/rev869b_external_controller_phase_a_a5_revised_source_implementation_checkpoint.md`

The prior `outputs/rev869b_external_controller_phase_a_a5_revised_implementation_checkpoint.md` remains immutable and
is not in the candidate allowlist. The present reconciliation report is also immutable after this commit and is not
an implementation path.

If architecture resolution requires a new adapter/project, another source path, another project file, a package lock
file, a classifier file, or any 29th path, this 28-path boundary is insufficient and management must issue a new
exhaustive boundary. It may not be silently expanded during implementation.

## 10. Required offline build, test and mutant expectations

After a separate architecture freeze and fresh implementation authorization, all of the following remain mandatory:

1. exact authorized entry commit, parent, subject and clean target scope;
2. changed paths are a subset of the then-approved exhaustive allowlist, with no prior report/checkpoint edit;
3. no network restore; only an explicitly frozen and already-cached dependency graph may be used;
4. warning-as-error offline builds of Control Plane, API, both test graphs and `SESS.NexaERP.slnx` with zero
   warnings/errors;
5. exactly 30 A5 tests pass, each decisive individual invocation passes `1/1`, and all 23 retained A4 tests pass;
6. the complete Phase-A Control Plane suite and complete non-PostgreSQL ERP suite pass;
7. all 19 Purchase action rows remain fixed, server-owned, unique and mapped to the immutable Purchase service
   methods; manifest remains 2,668 bytes with SHA-256
   `EDAF648EFF4BD77158EF3A18A780D7B0DAD634FFB90CDBA8564A27D4DCFC95CB`;
8. no Purchase application/domain contract, business rule or public Purchase endpoint changes;
9. target business/history, terminal result, fence/idempotency, audit/outbox and evidence share one target-local
   transaction through the capability-bound enlistment seam;
10. all 40 semantic production mutants are rerun from fresh isolated worktrees at the exact candidate commit using
    authoritative Git-blob restoration;
11. mutant result is exactly `40 compiled / 40 killed / 0 survived / 0 invalid`, including unchanged M12;
12. every restored file blob/SHA equals baseline, `git diff --check` passes, no duplicate mutant production diff
    exists, and no tracked residue remains; and
13. final changed-path, project-graph, package-graph and target-cleanliness checks pass before one correction commit.

This report ran none of those acceptance actions.

## 11. Rollback and stop conditions

On any later authorized implementation, revert all implementation changes and create only the separately authorized
blocker report if:

- the entry commit or parent differs, or target scope is dirty;
- architecture/package/classifier ownership is not frozen first;
- Control Plane must directly reference ERP Infrastructure or API;
- Infrastructure must reference Control Plane;
- a new adapter/project, package dependency, lock file or unnamed path is needed;
- any production-to-test reference appears or a cycle is introduced;
- the 28-path conditional boundary is not exact and sufficient;
- API ceases to be the ERP target composition root;
- controller/target ownership, security boundary or deployment topology changes;
- a Purchase contract, domain rule, operation partial, public Purchase endpoint, accepted migration/snapshot,
  helper, existing report or checkpoint must change;
- the transaction-enlistment seam cannot remain internal, capability-bound and confined to the same target
  transaction;
- build/test/manifest/contract validation fails;
- a mutant is invalid, does not compile, survives, is killed by an unrelated failure, duplicates another mutant,
  fails exact restoration, or leaves tracked residue; or
- PostgreSQL, credentials, migrations, services, network restore, deployment, production, Phase B or Correction 2
  becomes necessary.

No fallback project edge, new file, package, alternate mutant or boundary expansion may be improvised after a stop.

## 12. Management decision required

Before another source implementation authorization, management and architecture must choose and freeze:

1. the reproducible control-plane PostgreSQL provider dependency boundary: direct pinned Npgsql package versus a new
   dedicated control-plane adapter/project; and
2. the single shared owner/path for target fence and terminal-result classification without
   `Infrastructure -> ControlPlane`.

Direct `ControlPlane -> Infrastructure` and `ControlPlane -> Api` are rejected options.

This report creates no implementation authority and makes no Phase-A management-acceptance or production-readiness
claim.

`A5_PROJECT_GRAPH_AND_BOUNDARY_GATE=NO_GO`

`A5_ARCHITECTURE_FREEZE_REQUIRED`

`A5_CORRECTION_IMPLEMENTATION_STATE=NOT_STARTED`

`POSTGRESQL_EXECUTION_STATE=NOT_AUTHORIZED_NOT_RUN`

`PHASE_B_STATE=NO_GO`

`CORRECTION_2_STATE=NO_GO`
