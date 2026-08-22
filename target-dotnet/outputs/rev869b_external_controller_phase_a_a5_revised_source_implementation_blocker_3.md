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
