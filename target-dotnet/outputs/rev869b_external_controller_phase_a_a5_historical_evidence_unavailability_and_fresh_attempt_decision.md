# REV869B Phase-A A5 historical-evidence unavailability and fresh-attempt protocol decision

Date: 2026-08-22

Decision type: report-only management decision and future protocol freeze

## 1. Decision

```text
A5_HISTORICAL_EVIDENCE_UNAVAILABILITY_DECISION=ACCEPTED_WITHOUT_RETROSPECTIVE_PASS
A5_PRIOR_ATTEMPT_STATE=FAILED_NOT_ACCEPTED
A5_FRESH_ATTEMPT_PROTOCOL_STATE=PASS
A5_FRESH_SOURCE_IMPLEMENTATION_GATE=GO_PENDING_SEPARATE_AUTHORIZATION
```

The unavailable historical fields are accepted as permanently unavailable, not as satisfied. The previous attempt
remains failed and supplies no reusable formal acceptance evidence. This decision freezes a clean, executable
development/candidate/formal protocol for a completely fresh attempt. It neither implements A5 nor authorizes the
fresh source attempt automatically.

The required evidence fits the existing mutually exclusive implementation checkpoint and blocker paths. No helper,
log, evidence directory, package, project, source path or allowlist expansion is required.

## 2. Stage-0 evidence

| Check | Observed result | Status |
|---|---|---|
| Starting HEAD | `3dc9137c257020880a9b0431a1e41b5b474246b0` | PASS |
| Parent | `d5d38b4460f6caa2f5bfc4c8287d2f82d01bfc1f` | PASS |
| Branch | `master` | PASS |
| Subject | `REV869B A5 first acceptance failure reconciliation` | PASS |
| HEAD boundary | Exactly one added reconciliation report | PASS |
| Reconciliation path | `outputs/rev869b_external_controller_phase_a_a5_first_acceptance_failure_reconciliation.md` | PASS |
| Reconciliation SHA-256 | `7C3C75F742C9852B8C4C6905359682FE6B73719978809EED39C9E04B7177383F` | PASS |
| Original blocker SHA-256 | `0EDAAA183F874A49F13D726C5AC9B6F5E5D238F85903F7B08B20B9EA7A6CBA71` | PASS |
| Target-scoped status at entry | Clean | PASS |
| New decision report at entry | Absent | PASS |
| A5 implementation checkpoint | Absent | PASS |

HEAD is a normal one-parent report-only commit. No source, test, project, migration, snapshot, lock, helper or
implementation checkpoint change exists at the authorized baseline. The external legacy sibling was not accessed,
enumerated, read, hashed, verified or modified.

The original blocker, first-failure reconciliation, immutable action/boundary decision, project-graph
reconciliation, persistence/classifier architecture freeze, dual-context migration/package decision, Control Plane
EF tooling reconciliation, controlled official Npgsql identity decision, controlled official 41-package graph
verification, evidence-integrity reconciliation and both project-aware lock reconciliations were read completely.

No build, test, mutant, restore, package acquisition, migration operation, PostgreSQL action, service, deployment,
provisioning, production access, Phase B or Correction 2 operation was performed.

## 3. Mandatory historical decision

The historical record is frozen without reinterpretation:

1. The previous A5 implementation attempt remains `FAILED_NOT_ACCEPTED`.
2. The initial `29/30` A5 run is a formal failed gate.
3. The later corrected `30/30` rerun is development information only. It cannot replace, cure or supersede the
   failed formal run.
4. The missing historical command, filter, timestamp, exit code, raw assertion output, expected/actual values, stack
   trace, fully qualified test name, source locations and before/after blobs cannot be reconstructed or invented.
5. No build, test, migration, SQL, package, mutant or other validation observation from the previous attempt may be
   reused as formal acceptance for a future attempt.
6. The previous rollback remains valid and complete.
7. This decision grants no retrospective PASS and waives no architecture, security, package, migration, test,
   mutant, evidence or prohibited-operation requirement.

Historical unavailability is closed only as a management recordkeeping decision. It is not evidence that the
historical assertion correction was correct, deterministic or sufficient.

## 4. Frozen package and architecture identities

| Evidence | Frozen identity |
|---|---|
| ERP Infrastructure project-aware lock SHA-256 | `06DF9719F8A02E344C4F565DBB538DD5E39F5D49B600081291FC940EF1E1F953` |
| ERP graph arithmetic | 41 NuGet packages + 3 project-reference nodes = 44 nodes |
| Control Plane Persistence project-aware lock SHA-256 | `4D99C6E0124356EB289FCB231E89A6534A6BB1FAE1F42A5A01BCFE0D60F89DEB` |
| Control Plane graph arithmetic | 35 NuGet packages + 1 project-reference node = 36 nodes |
| Official package verification | `41/41 PASS` |

A project-reference node is not a NuGet package. The project-aware identities supersede the earlier package-only
locks only for the future real owner-project graphs. Package IDs, versions, archives, content hashes, signatures,
timestamps, certificate/revocation results and the official trust decision remain unchanged.

The frozen architecture remains cycle-free: Control Plane Persistence owns control EF/Npgsql persistence and
depends only on neutral Contracts; ERP Infrastructure owns target persistence and consumes Contracts; Control Plane
does not depend on ERP API/Infrastructure; Infrastructure does not depend on the Control Plane executable or
Persistence; API and Control Plane remain their respective composition roots; production never references tests;
and no context, connection, migration, history or transaction crosses the two databases.

The existing exhaustive implementation boundary remains 39 named alternatives with a maximum 38-path outcome
because the success checkpoint and failure blocker are mutually exclusive. No path is added or removed by this
decision.

## 5. Fresh-attempt topology

The future attempt must keep development, formal evidence and target history distinct:

```text
authorized starting master HEAD
  -> disposable development worktree
       -> frozen detached candidate commit
            -> formal commands on clean candidate worktree
            -> one fresh isolated worktree per mutant

authorized starting master HEAD
  -> success: materialize exact frozen candidate blobs + evidence checkpoint
              -> exactly one correction commit on master
  -> failure: retain no implementation blobs; update only authorized blocker
              -> exactly one blocker-only commit on master
```

The detached candidate commit is temporary evidence and is never merged, rebased or added as an intermediate commit
to `master`. Its parent must be the exact separately authorized starting HEAD. The eventual target correction commit
has that same authorized starting HEAD as its direct parent and contains source/test/project/migration/snapshot/lock
blobs byte-identical to the successful frozen candidate plus the authorized evidence checkpoint.

This topology makes both the target worktree and the frozen candidate worktree clean when formal acceptance begins,
while preserving the rule that `master` receives exactly one final correction or blocker commit.

## 6. Development phase — `DEVELOPMENT_FEEDBACK_ONLY`

Before candidate freeze:

1. Every build and test invocation is declared and recorded as `DEVELOPMENT_FEEDBACK_ONLY` before process start.
2. Development runs are excluded from every formal acceptance count and checkpoint PASS claim.
3. Every development failure is corrected before freeze; an unresolved or nondeterministic result prohibits formal
   declaration.
4. The previously failed A5 test
   `A5_EachActionInvokesExactlyItsExistingPurchaseMethodWithDerivedIdempotencyAndNoDirectBusinessDml` must pass in at
   least 10 consecutive fresh processes.
5. Each fresh-process run records exact command, filter, working directory, executable/tool identity, start/end
   timestamps, exit code and complete stdout/stderr.
6. The test's fully qualified name, discovered count, pass/fail/skip arithmetic and source location are captured.
7. Any source, test or harness correction remains within the already frozen allowlist and restarts the consecutive
   development sequence.
8. Development results demonstrate readiness to freeze only; they never become formal evidence.

Temporary development logs remain outside the repository. They are not an additional source/evidence path and are
not committed as independent artifacts.

## 7. Candidate-freeze procedure

Formal acceptance may begin only after this exact sequence succeeds:

1. Create a fresh isolated disposable worktree at the separately authorized starting HEAD.
2. Materialize only explicitly authorized implementation paths into it.
3. Verify both project-aware lock hashes and exact node arithmetic.
4. Run the authorized warning-as-error offline build and required prequalification scans.
5. Freeze the exact changed-path list and require it to be a subset of the existing 39-path alternatives with a
   maximum 38-path success outcome.
6. Record SHA-256, Git blob identity and size for every changed source, test, project, migration, designer, snapshot
   and lock file.
7. Verify no unauthorized, unnamed or 40th path exists.
8. Create one detached disposable candidate commit whose parent is the exact authorized starting HEAD.
9. Record candidate commit, parent, tree and complete file manifest.
10. Require the candidate worktree to be clean and require the target-scoped `master` worktree to remain clean.
11. Verify incremental and cumulative target/candidate `git diff --check` and security/privacy/secret scans.
12. Record the exact timestamp and declaration:

```text
FORMAL_ACCEPTANCE_GATE_STARTED
```

After declaration, no source, test, project, migration, designer, snapshot, lock or acceptance-rule logic may
change. The implementation checkpoint has no executable logic; its post-pass population may only transcribe values
into the fixed evidence schema frozen by this report. Any executable-candidate or acceptance-rule change cancels the
candidate. If the change follows a formal failure, that attempt remains ended and a separately authorized fresh
attempt is required.

## 8. Formal acceptance execution

Each command runs from the same immutable detached candidate, in the prescribed order, and must pass on its first
formal execution:

1. A5 suite: exactly `30/30`.
2. The same A5 suite in a second fresh process: exactly `30/30`.
3. Retained A4 suite: exactly `23/23`.
4. Complete Phase-A suite.
5. Focused REV869B non-PostgreSQL suite.
6. Complete ERP non-PostgreSQL suite.
7. Windows PowerShell 5.1 AST validation.
8. EF discovery for both contexts using the approved offline/no-connect procedures, including Option T1 for Control
   Plane Persistence as both EF target and startup project.
9. Migration ordering, uniqueness, model/snapshot parity and reproducible Up/Down SQL/hash contracts.
10. Both project-aware locked offline restores using only verified local package artifacts.
11. All security, privacy, secret, changed-scope, dependency-direction and prohibited-operation scans.
12. Incremental and cumulative `git diff --check`.
13. Exactly 40 semantic production mutants, with final arithmetic:

```text
compiled=40
killed=40
survived=0
invalid=0
```

14. PostgreSQL connection attempts/connections, PostgreSQL tests executed, migration applications/removals, service
    starts, deployments, provisioning and external/production executions remain zero.

Mutants start only after every preceding formal gate passes. Any formal command failure immediately ends the attempt.
It must not be corrected and rerun as formal acceptance. Complete decisive evidence is captured before rollback.

## 9. Mandatory command evidence

For every development and formal command, capture:

- phase label (`DEVELOPMENT_FEEDBACK_ONLY` or formal);
- exact command and arguments;
- exact working directory;
- executable path, product/file version, size and SHA-256;
- candidate commit/tree and frozen-manifest SHA-256;
- local and UTC start/end timestamps;
- process exit code;
- complete stdout and stderr as distinct byte streams;
- stdout/stderr size and SHA-256;
- TRX or equivalent result path, size and SHA-256 when applicable;
- discovered, selected, passed, failed, skipped and total counts;
- fully qualified failed test and data-row identity;
- raw assertion/error message;
- expected and actual values when emitted;
- complete stack trace and source locations when emitted; and
- exact zero-operation counters relevant to the command.

If a runner does not emit a requested field, record `NOT_EMITTED_BY_TOOL` and retain the complete raw stream proving
that absence. Never reconstruct, normalize or invent missing output.

The success checkpoint or failure blocker embeds the complete formal command inventory and the complete decisive
stdout/stderr/failure evidence. Non-decisive passing streams may be embedded verbatim in bounded sections with their
exact byte hashes and complete line-preserving content; no external log is treated as the sole authoritative record.

## 10. Mandatory mutant evidence

Each of the 40 mutants runs from a fresh isolated disposable worktree based on the exact frozen candidate commit.
For each mutant capture:

- mutant ID and retained A4/A5/new-contract classification;
- exact production file and enforcement location;
- original Git blob ID, file size and SHA-256;
- one semantic mutation description;
- mutated Git blob/file size and SHA-256;
- proof that exactly one production mutant diff exists and is nonduplicate;
- exact compile command, timestamps, exit code and complete output;
- exact decisive non-vacuous killing test command;
- killing test's fully qualified name and complete raw failure evidence;
- proof that no unrelated build/test failure caused the kill;
- authoritative original Git-blob restoration method and restored SHA-256;
- post-restoration blob/size/SHA equality result;
- target/candidate `git diff --check`; and
- zero tracked changes before the next mutant.

Reverse patches are prohibited. Restoration uses only the authoritative pre-mutant Git blob. A noncompiling,
surviving, invalid, duplicate, vacuous, unrelated-kill or restoration-failing mutant immediately ends the attempt.

## 11. Evidence retention within the frozen boundary

The existing authorized alternatives are sufficient:

- success checkpoint:
  `outputs/rev869b_external_controller_phase_a_a5_revised_source_implementation_checkpoint.md`;
- failure blocker:
  `outputs/rev869b_external_controller_phase_a_a5_revised_source_implementation_blocker_3.md`.

They never coexist in one implementation outcome. Markdown can retain the complete command inventory, hashes,
arithmetic, mutant table, decisive raw failure blocks and zero-operation counters. Evidence logs and TRX files may
remain in validated disposable directories during execution, but they are not new repository paths and are never a
substitute for the embedded authoritative checkpoint/blocker record.

On success, temporary evidence must remain available until the checkpoint commit, report SHA-256, one-commit file
boundary, source/blob equality and target-scoped clean status are verified. On failure, temporary evidence must
remain available until the blocker commit, blocker SHA-256, one-file commit boundary and target-scoped clean status
are verified. Only then may validated disposable worktrees/log roots be removed.

No extra evidence path, helper, archive, README, package configuration or lock path is required. Therefore the
existing implementation boundary remains exact and sufficient.

## 12. Formal failure stop and rollback

At the first formal failure:

1. stop all later formal commands and do not run mutants unless the failure occurs within the mutant campaign;
2. do not repair or rerun the failed command as formal acceptance;
3. record complete command, timestamp, exit, stdout/stderr, failure, manifest and tool evidence;
4. preserve the candidate commit/worktree and evidence root read-only while the blocker is prepared;
5. restore any target implementation path from the authorized starting Git blobs; never use reverse patches;
6. remove only exact, validated untracked implementation paths;
7. require zero target implementation changes and no checkpoint;
8. update only the authorized blocker report with the decisive failure record, formal arithmetic, candidate manifest,
   rollback proof, zero-operation counters and exact next gate;
9. create exactly one blocker-only commit on `master`;
10. verify its parent, sole path, SHA-256, `git diff --check` and target-scoped clean status; and
11. only after commit verification, remove validated disposable candidate/mutant/evidence roots.

The failed candidate and every partial PASS remain non-accepted. A subsequent attempt requires fresh management
authorization and starts the entire development and formal protocol again.

## 13. Successful completion

Only after every formal gate and all 40 mutants pass:

1. reverify that the frozen candidate commit/tree and every executable-candidate blob remain unchanged;
2. populate the authorized checkpoint solely with the frozen protocol's evidence values and complete embedded
   command/mutant record;
3. materialize the exact frozen candidate blobs plus checkpoint into the clean target `master` worktree;
4. verify every materialized source/test/project/migration/snapshot/lock SHA-256 and Git blob equals the frozen
   candidate;
5. explicitly stage only authorized paths;
6. verify the maximum 38-path boundary, both lock hashes, cumulative `git diff --check` and security scans;
7. create exactly one A5 correction commit whose parent is the separately authorized starting HEAD;
8. verify commit path count, checkpoint SHA-256 and final target-scoped clean status; and
9. stop for a fresh independent report-only architecture/security review.

The disposable candidate commit never enters `master`. No prior-attempt result is counted. This protocol does not
self-declare Phase-A management acceptance or production readiness.

## 14. Exact future management gate

The only next gate is separate management authorization for one completely fresh bounded REV869B Option-A Phase-A
A5 source-only implementation from the commit containing this decision. That authorization must restate the exact
starting HEAD/parent, frozen 39-path alternative allowlist and maximum 38-path outcome, both project-aware lock
hashes, 30 A5 tests, retained 23 A4 tests, complete offline suites, approved EF procedures, all 40 fresh mutants,
zero-operation requirements and the protocol frozen here.

No implementation begins automatically from this report.

## 15. Canonical retained states

```text
phase_a_management_acceptance_state=FAIL_PENDING_INDEPENDENT_REVIEW
phase_b_state=NO_GO
correction_2_state=NO_GO
postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN
external_provisioning_state=NOT_STARTED
production_readiness_state=NOT_READY
```
