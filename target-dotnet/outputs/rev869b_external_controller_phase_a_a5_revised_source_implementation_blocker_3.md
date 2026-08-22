# REV869B Option-A Phase-A A5 revised source implementation blocker 3

Date: 2026-08-21

Decision type: mandatory blocker-only stop during the authorized bounded A5 source attempt

Starting and final source HEAD: `2aa8106e697f360274a30386379aaa6a1c42583c`

Parent: `a84a4aad1dbe6d841545d424d3896da8cb79c3ad`

Branch: `master`

## Decision

`A5_REVISED_SOURCE_IMPLEMENTATION_GATE=NO_GO`

No A5 implementation commit may be created from this attempt. All source, test, project, solution, migration,
package-lock and implementation-checkpoint changes made during the attempt were reverted. This report is the only
target-scoped change retained.

The exact frozen Control Plane EF discovery command is not reproducible with the frozen package-ownership graph:

```text
dotnet ef migrations list
  --context SESS.NexaERP.ControlPlane.Persistence.Rev869BControlPlaneDbContext
  --project src/SESS.NexaERP.ControlPlane.Persistence/SESS.NexaERP.ControlPlane.Persistence.csproj
  --startup-project src/SESS.NexaERP.ControlPlane/SESS.NexaERP.ControlPlane.csproj
  --no-build --no-connect
```

It exits `1` with:

```text
Your startup project 'SESS.NexaERP.ControlPlane' doesn't reference
Microsoft.EntityFrameworkCore.Design. This package is required for the Entity Framework Core Tools to work.
```

The verified package graph assigns direct `Microsoft.EntityFrameworkCore.Design 10.0.10` ownership to
`SESS.NexaERP.ControlPlane.Persistence`, with `PrivateAssets=all`. The frozen Control Plane executable package graph
does not authorize that direct package. Adding it to the executable would change verified package ownership and would
also require lock/replay treatment not frozen for that project. Using the persistence project as the startup project
would change the exact authoritative command. Neither substitution is authorized.

This triggers the mandatory stop rules for package/lock identity drift and for an acceptance invariant that cannot be
completed within the frozen boundary. No unnamed path was added and no package substitute or network source was used.

## Entry gate

The attempt began from the exact authorized commit, parent and branch. The target-scoped worktree was clean; the
pre-existing untracked `../legacy-reference/` entry was not accessed or modified. The governing report paths and
SHA-256 identities were verified, including:

- evidence-integrity reconciliation: `42215A833682C8E8BBB2751B558B221EB90DEFA7CB457F6203B8A2E61D76EB68`;
- official EF package graph verification: `6E33DB8F4866FA8692B318C4A112074C4B2B60EF1BA55F29B027FFBB721973F2`;
- dual-context migration/package decision: `3CF02E3E6D7F464942CC175E3EC699D40365FEBB4CEC38B7AE24CC40592D1EAB`.

The exact 39-path allowlist was extracted before editing. Its implementation checkpoint and blocker paths were
confirmed mutually exclusive, with a maximum outcome of 38 changed paths.

## Diagnostic evidence before rollback

All results in this section describe the reverted disposable implementation candidate. They are diagnostic evidence,
not acceptance of source that remains in the worktree.

- affected Control Plane build: `PASS`, zero warnings and zero errors;
- exact A5 suite: `30/30 PASS`;
- retained A4 suite: `23/23 PASS`;
- complete Control Plane assembly: `116/116 PASS` before the final reconciliation-test strengthening;
- focused and complete ERP non-PostgreSQL filter: `455/455 PASS` after preserving the immutable A3 checkpoint hash boundary;
- ERP warning-as-error build: `PASS`, zero warnings and zero errors;
- ERP EF discovery with `--no-connect`: `14` migrations, including ordinals 12/13/14 as
  `Rev869AIdentityMasterScopeFoundation`, `Rev869BRfqQuotationComparisonPurchaseOrderFoundation`, and
  `Rev869BA4TargetExecutionBoundary`;
- Control Plane EF discovery: `FAIL` before enumeration for the exact package-owner/startup-project conflict above;
- migration creation/removal/application: `0/0/0`;
- observed PostgreSQL connections: `0`;
- package download and network restore: `0`.

One unfiltered ERP test invocation selected PostgreSQL-named scenario methods. Their mandatory opt-in guard rejected
each before fixture creation or connection (`Explicit isolated REV869B PostgreSQL opt-in is required`). No opt-in was
set and no database connection occurred. Because the authorization required discovery without execution, that
invocation is independently non-acceptable and reinforces this blocker-only outcome. The later explicitly filtered
non-PostgreSQL invocation passed `455/455`.

The model-parity command was not run: the execution safety gate rejected `has-pending-model-changes` because that EF
subcommand exposes no explicit `--no-connect` option. No workaround was attempted. Migration SQL generation and the
40-mutant campaign did not start after the mandatory EF/package boundary failure.

## Mutant arithmetic

- required: `40 compiled / 40 killed / 0 survived / 0 invalid`;
- executed in this attempt: `0`;
- reused from an earlier attempt: `0`;
- acceptance: not reached.

The authorization requires stopping immediately once a mandatory pre-mutant acceptance gate fails. Therefore no
mutant result is claimed and no partial mutant evidence is accepted.

## Rollback proof

The rollback restored every tracked implementation path from the authorized starting commit and removed every
untracked implementation file by exact, workspace-validated path. The implementation checkpoint does not exist.
Before this report was created:

- `git diff --check`: `PASS`;
- target-scoped tracked changes: `0`;
- target-scoped untracked implementation files: `0`;
- preserved external status: only untouched `?? ../legacy-reference/`.

## Required next decision

A separate report-only architecture/package reconciliation must choose one exact reproducible Control Plane EF tooling
boundary. It must either authorize the Design package in the executable with its exact lock/package graph, or revise
the frozen offline EF command to use a package-owning startup project, while preserving composition-root ownership and
no-connect guarantees. It must also define an explicitly no-socket model-parity method and reconcile the prohibited
PostgreSQL-test invocation evidence. No source implementation resumes automatically.

Retained gates:

`phase_a_management_acceptance_state=FAIL_PENDING_INDEPENDENT_REVIEW`

`phase_b_state=NO_GO`

`correction_2_state=NO_GO`

`postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN`

`external_provisioning_state=NOT_STARTED`

`production_readiness_state=NOT_READY`

---

## ERP Infrastructure project-aware lock drift from `fadcdd4`

Date: 2026-08-22

This section records the management-re-authorized attempt from exact HEAD
`fadcdd48731dee78fc1b50354af85982fba337b4`, parent
`7e015878dc5e36f8a7f908e6b544be88279c7550`, branch `master`. It supersedes every earlier next-gate statement in
this retained blocker report.

`A5_REVISED_SOURCE_IMPLEMENTATION_GATE=STOPPED_ROLLED_BACK`

---

## Superseding stopped implementation attempt from `65aff803`

Date: 2026-08-22

This section records the management-re-authorized implementation attempt from exact HEAD
`65aff8032551c00b24e5898056a0c2336c569e36`, parent
`6b72ba8766281bab8e7bb2dffde8a1b9671de81e`, branch `master`. It supersedes the earlier controlling-decision and
next-gate statements in this retained blocker report.

`A5_REVISED_SOURCE_IMPLEMENTATION_GATE=STOPPED_ROLLED_BACK`

### Exact mandatory stop

The authorization required immediate rollback if **any** build or test failed. During candidate development, the
first exact 30-test A5 invocation returned `29/30`: the failed test was
`A5_EachActionInvokesExactlyItsExistingPurchaseMethodWithDerivedIdempotencyAndNoDirectBusinessDml`. Its original
source assertion looked only for a method token immediately followed by `(` and did not accommodate the production
call formatting. The assertion was corrected and the exact A5 rerun subsequently passed `30/30`, but the later pass
does not erase the explicit "any build/test fails" stop condition. An earlier retained-A4 diagnostic invocation had
also failed before the candidate's provenance and required-option handling was completed; its corrected rerun passed
`23/23`.

This is a failed-gate-history blocker. It is not evidence of an unresolved Purchase production-source defect, a
package-identity defect, lock drift, a migration defect or PostgreSQL behavior. Because the stop rule is absolute,
the complete test assemblies, EF/model/SQL acceptance and all 40 production mutants were not run after recognizing
the controlling failure history. No partial evidence is promoted to A5 acceptance.

### Entry and partial diagnostic evidence

- starting lineage and branch: exact;
- ERP project-aware lock report SHA-256:
  `3B51F719B747EF490CA002C4F1171AD5072A2EE2BDBE233A7714C337F7C13406`;
- Control Plane project-aware lock report SHA-256:
  `336F45F661BA1194762EE2CEDD6EA980E66E0896CCE328ACAE5F4A3ECF262A95`;
- regenerated ERP Infrastructure lock SHA-256:
  `06DF9719F8A02E344C4F565DBB538DD5E39F5D49B600081291FC940EF1E1F953`, with `41` NuGet package nodes,
  `3` project-reference nodes and `44` total nodes;
- regenerated Control Plane Persistence lock SHA-256:
  `4D99C6E0124356EB289FCB231E89A6534A6BB1FAE1F42A5A01BCFE0D60F89DEB`, with `35` NuGet package nodes,
  `1` Contracts project-reference node and `36` total nodes;
- verified offline warning-free candidate builds: Control Plane Persistence and ERP API graphs;
- corrected diagnostic test results: A5 `30/30`, retained A4 `23/23`, unique total `53`;
- disposable EF migration generation: Control Plane initial `1`, ERP A5 target `1`;
- package downloads and HTTP package sources: `0`;
- PostgreSQL connection attempts/connections: `0/0`;
- migration applications/removals: `0/0`;
- production mutants executed/reused: `0/40` and `0`.

The generated migrations and every source, test, project, solution, snapshot and lock change were diagnostic
candidate artifacts only and have been rolled back. The diagnostic results above are not an acceptance checkpoint.

### Rollback proof

Every tracked implementation path was restored from the authorized starting commit. Every untracked implementation
path was removed by exact, workspace-validated path. The isolated migration worktree under the system temporary
directory was removed after its absolute path was validated. Before this report edit, target-scoped status was empty.

```text
retained_implementation_paths=0
implementation_checkpoint_created=0
blocker_report_paths_changed=1
network_downloads=0
postgresql_connection_attempts=0
postgresql_connections=0
migration_applications=0
migration_removals=0
service_starts=0
deployments=0
production_access=0
phase_b_work=0
correction_2_work=0
```

### Required next gate

The exact next gate is a fresh management decision on this blocker-only commit. A future implementation attempt
requires new bounded reauthorization and must start from that new exact report-only HEAD; no source implementation,
test execution or mutant campaign resumes automatically.

Retained gates:

`phase_a_management_acceptance_state=FAIL_PENDING_INDEPENDENT_REVIEW`

`phase_b_state=NO_GO`

`correction_2_state=NO_GO`

`postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN`

`external_provisioning_state=NOT_STARTED`

`production_readiness_state=NOT_READY`

### Exact blocker

The latest reconciliation corrected the Control Plane Persistence lock for its real Contracts project-reference
graph, but it did not reconcile the second required project-local lock for the real ERP Infrastructure graph.
The frozen official package-verification evidence gives the ERP package-only lock as:

```text
packages.lock.json_sha256=CF17917E57148E4E35D6C483CEF990615C11405EFD97DE3AB562FD98759E004E
nuget_package_nodes=41
project_reference_nodes=0
```

The authorized production graph requires ERP Infrastructure to reference Application, Domain and
Control Plane Contracts. Before any implementation edit, an isolated disposable copy of those exact committed
projects was created. The sole future graph changes in that copy were the already-frozen
`Infrastructure -> ControlPlane.Contracts` edge and `RestorePackagesWithLockFile=true`. A restore used only the
previously verified local 41-archive source, an isolated packages directory, `--no-cache`,
`--force-evaluate` and `NuGetAudit=false`. It succeeded with zero warnings and zero errors and generated:

```text
packages.lock.json_bytes=16501
packages.lock.json_sha256=06DF9719F8A02E344C4F565DBB538DD5E39F5D49B600081291FC940EF1E1F953
nuget_package_nodes=41
project_reference_nodes=3
total_lock_nodes=44
project_nodes=sess.nexaerp.application,sess.nexaerp.controlplane.contracts,sess.nexaerp.domain
```

The 41 NuGet package count remains exact; the additional entries are structural `Project` nodes and are not NuGet
packages. Nevertheless, the required real-project lock bytes and SHA-256 differ from the frozen ERP lock. The latest
project-aware reconciliation froze only the Control Plane Persistence lock
`4D99C6E0124356EB289FCB231E89A6534A6BB1FAE1F42A5A01BCFE0D60F89DEB`; it supplied no authoritative replacement
for the ERP Infrastructure lock. Committing `06DF...` would therefore violate the binding no-lock-drift rule, while
committing `CF179...` would not represent the required real Infrastructure project graph.

This is a reproducible lock-evidence boundary defect, not a package-content, Purchase source, migration, test or
runtime defect. It triggers the explicit mandatory stop conditions: “package or lock identity drifts” and
“project-aware lock differs.”

### Stop evidence

The stop occurred before source, test, project, solution, migration, lock or checkpoint edits in the target
worktree. Consequently no implementation rollback operation was necessary. The only retained target change is this
authorized blocker report.

```text
implementation_files_edited=0
implementation_checkpoint_created=0
production_mutants_executed=0/40
network_package_sources=0
package_downloads=0
postgresql_connection_attempts=0
postgresql_connections=0
migration_creations=0
migration_applications=0
migration_removals=0
service_starts=0
deployments=0
production_access=0
phase_b_work=0
correction_2_work=0
```

The target-scoped status was clean immediately before this report edit. The external legacy sibling was not
accessed, enumerated, hashed or modified.

### Required next decision

A separate report-only package-lock reconciliation must reproduce and freeze the ERP Infrastructure lock from its
complete real project graph, including all three project-reference nodes, and must state its exact bytes, SHA-256,
node arithmetic and offline locked-replay evidence. It must explicitly supersede the package-only
`CF17917E...` lock for the real Infrastructure project. No source implementation resumes automatically.

Retained gates:

`phase_a_management_acceptance_state=FAIL_PENDING_INDEPENDENT_REVIEW`

`phase_b_state=NO_GO`

`correction_2_state=NO_GO`

`postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN`

`external_provisioning_state=NOT_STARTED`

`production_readiness_state=NOT_READY`

---

## Earlier controlling blocker: package-lock reproducibility from `141b316`

Date: 2026-08-22

This latest controlling section records the management-re-authorized, strict target-only attempt from exact HEAD
`141b316245475ddbd861c7dfc4aa0838c4067d11`, parent
`d0261c7ae178d1596786090fe9d2bc8dc5005048`, branch `master`. It supersedes every next-gate statement in the retained
earlier stopped-attempt records, including the chronologically earlier `d0261c7` record preserved below.

`A5_REVISED_SOURCE_IMPLEMENTATION_GATE=STOPPED_ROLLED_BACK`

### Exact blocker

The frozen Control Plane lock and the frozen required production project graph cannot both pass NuGet locked-mode
restore.

Using only the previously verified local 41-archive source, a package-only Control Plane Persistence restore produced
the frozen lock exactly:

```text
package_identity_count=35
packages.lock.json_bytes=13361
packages.lock.json_sha256=64DC53ED03457021DFCBC985D9C8C5C0468B82BB102BC8382C3D920827137AA6
```

After restoring the mandatory production edge
`SESS.NexaERP.ControlPlane.Persistence -> SESS.NexaERP.ControlPlane.Contracts`, the exact offline locked-mode restore
failed with `NU1004`:

```text
A new project reference to SESS.NexaERP.ControlPlane.Contracts was found for net10.0 target framework.
The packages lock file is inconsistent with the project dependencies so restore can't be run in locked mode.
```

Regenerating the lock with that required edge retained the same 35 package identities but added NuGet's required
`sess.nexaerp.controlplane.contracts` entry of type `Project`. The resulting real-project lock was 13,446 bytes with
SHA-256 `4D99C6E0124356EB289FCB231E89A6534A6BB1FAE1F42A5A01BCFE0D60F89DEB`. Its dependency table had 36 entries: 35
packages and one project reference. That byte identity differs from the frozen `64DC...` lock and therefore cannot be
committed under the no-lock-drift acceptance rule.

The local restore also transiently materialized
`src/SESS.NexaERP.ControlPlane.Contracts/packages.lock.json` because `--use-lock-file` applied to the referenced
project. That path is not in the 39-path allowlist. It was removed during rollback and is not retained.

This is reproducible package-lock/graph evidence, not a package artifact, source, migration, Purchase-rule or test
defect. No package version or package content hash changed. The frozen lock evidence was generated from a package-only
probe, while the authorized implementation requires a project reference that NuGet represents in the real project
lock.

### Stop and rollback evidence

The mandatory stop was taken before migration generation, test creation, mutant execution or checkpoint creation.
All tracked implementation changes were restored from exact HEAD `141b316245475ddbd861c7dfc4aa0838c4067d11`.
All untracked implementation artifacts, including the transient Contracts lock, were removed by exact
workspace-validated paths. Before this report-only edit, target-scoped status, tracked diff and untracked-file output
were empty.

```text
warning_as_error_infrastructure_builds=1/1 PASS
control_plane_locked_restore=FAIL NU1004
production_mutants_executed=0/40
network_package_sources=0
package_downloads=0
postgresql_connection_attempts=0
postgresql_connections=0
migration_creations=0
migration_applications=0
migration_removals=0
service_starts=0
deployments=0
production_access=0
phase_b_work=0
correction_2_work=0
implementation_checkpoint_created=0
```

### Required next decision

A separate report-only architecture/package reconciliation must freeze a lock protocol for the real
Control Plane Persistence project graph. It must either authorize the real-project lock including the required
Contracts project entry and its exact hash, or define and justify a different reproducible locked-restore mechanism
that does not contradict the required project edge. No source implementation resumes automatically.

Retained gates:

`phase_a_management_acceptance_state=FAIL_PENDING_INDEPENDENT_REVIEW`

`phase_b_state=NO_GO`

`correction_2_state=NO_GO`

`postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN`

`external_provisioning_state=NOT_STARTED`

`production_readiness_state=NOT_READY`

---

## Chronology note

The ERP Infrastructure project-aware lock drift recorded for the `fadcdd4` attempt is the latest controlling
blocker and supersedes the chronologically earlier stopped-attempt sections retained after it.

`A5_REVISED_SOURCE_IMPLEMENTATION_GATE=STOPPED_ROLLED_BACK`

---

## Superseding stopped implementation attempt from `d0261c7`

Date: 2026-08-22

This section records the separately authorized implementation attempt that started from exact HEAD
`d0261c7ae178d1596786090fe9d2bc8dc5005048`. It supersedes only this report's earlier “Required next decision”
section. The Option-T1 tooling contradiction documented above had already been resolved by the committed tooling
reconciliation at the authorized starting HEAD.

`A5_REVISED_SOURCE_IMPLEMENTATION_GATE=STOPPED_ROLLED_BACK`

### Exact blocker

During target-scope accounting, one diagnostic command used
`git status --short --untracked-files=all`. Git consequently enumerated two child path names beneath the prohibited
external sibling `../legacy-reference/`. No sibling file was opened, read, hashed, copied, written, deleted or
otherwise modified, and no file contents or metadata beyond those emitted path names were inspected. Nevertheless,
the authorization prohibited access to that sibling, and enumeration itself exceeded the permitted evidence
boundary. The discrepancy was disclosed immediately and treated as a mandatory stop.

### Work reached before the stop

- exact entry lineage, report hashes, branch, one-file HEAD boundary and clean target scope passed;
- local-only package locks were generated from the previously verified 41-archive source;
- warning-as-error offline builds passed;
- A5 aggregate: `30/30 PASS`;
- retained A4: `23/23 PASS`;
- complete Control Plane suite: `116/116 PASS`;
- complete non-PostgreSQL ERP suite: `455/455 PASS`;
- PostgreSQL tests were discovered by name only and were not executed;
- EF no-connect discovery enumerated exactly 14 ERP migrations and one Control Plane migration;
- two disposable model-parity probes each generated zero operations;
- repeated disposable SQL generation was byte-identical for ERP Up/Down and Control Plane Up/Down;
- production mutants executed: `0/40`; no mutant result is claimed.

These partial observations cannot establish A5 acceptance because the evidence-boundary violation stopped the run
before the 40-mutant gate.

### Rollback and prohibited-operation counters

All tracked implementation changes were restored from the authorized starting commit. Every untracked implementation
path was removed using an exact workspace-validated path. The three exact disposable A5 temp directories were removed
after verifying that each resolved beneath the system temp root. No implementation checkpoint exists.

```text
target_tracked_implementation_changes=0
target_untracked_implementation_paths=0
network_downloads=0
postgresql_connection_attempts=0
postgresql_connections=0
migration_applications=0
migration_removals=0
service_starts=0
deployments=0
production_access=0
phase_b_work=0
correction_2_work=0
```

The exact next gate is fresh management direction that either authorizes another bounded A5 implementation attempt
from the new report-only commit or closes A5. No source implementation resumes automatically.

Retained gates:

`phase_a_management_acceptance_state=FAIL_PENDING_INDEPENDENT_REVIEW`

`phase_b_state=NO_GO`

`correction_2_state=NO_GO`

`postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN`

`external_provisioning_state=NOT_STARTED`

`production_readiness_state=NOT_READY`

---

## Current controlling decision

The ERP Infrastructure project-aware lock drift recorded for the `fadcdd4` attempt is the latest controlling
blocker. It supersedes every next-gate statement in all chronologically earlier stopped-attempt sections retained in
this report.

`A5_REVISED_SOURCE_IMPLEMENTATION_GATE=STOPPED_ROLLED_BACK`

---

## Current controlling decision from `65aff803`

The failed-gate-history blocker recorded above for the attempt from
`65aff8032551c00b24e5898056a0c2336c569e36` is the latest controlling decision. It supersedes every earlier
controlling-decision and next-gate statement retained in this report. The implementation remains fully rolled back;
the exact next gate is a fresh management decision on this blocker-only commit.

`A5_REVISED_SOURCE_IMPLEMENTATION_GATE=STOPPED_ROLLED_BACK`
