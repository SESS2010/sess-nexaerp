# REV869B A5 deterministic formal-acceptance evidence-harness architecture and boundary decision

Generated: `2026-08-22T19:06:03.6544595+05:30`

Decision type: report-only architecture and bounded future-implementation decision

## 1. Decision

One repository-owned, PowerShell 5.1-compatible, deterministic and fail-closed harness architecture is complete and implementable in a separate bounded prerequisite change. The harness must be accepted before any new A5 source attempt. This report does not implement the harness, restart A5, or run formal acceptance.

```text
A5_FORMAL_EVIDENCE_HARNESS_ARCHITECTURE_GATE=GO
A5_FORMAL_TIMING_SEQUENCE_FAILURE_STATE=RETAINED_FAILED_NOT_ACCEPTED
A5_EVIDENCE_HARNESS_IMPLEMENTATION_GATE=GO_PENDING_SEPARATE_AUTHORIZATION
A5_SOURCE_IMPLEMENTATION_GATE=NO_GO_PENDING_HARNESS_ACCEPTANCE
A5_FAILED_CANDIDATE_SOURCE_REUSE=DEVELOPMENT_SEED_ONLY
```

Manual execution of an individual formal command is prohibited after harness adoption. Any command not launched synchronously by the one runner is development/diagnostic activity and can never become formal evidence. The independent verifier rejects an extra, missing, reordered, duplicated, manually inserted, relabeled, or retried command.

## 2. Mandatory Stage 0

| Gate | Observed result | Status |
|---|---|---|
| Starting HEAD | `6387f35c5de1fb96a2d28bcc85e7fd73958961a7` | PASS |
| Parent | `4be39f13a3560d67dcff32e5a085c5c5850a84ff` | PASS |
| Branch | `master` | PASS |
| Subject | `REV869B reconcile A5 formal timing sequence failure` | PASS |
| HEAD boundary | Exactly one added report: `target-dotnet/outputs/rev869b_external_controller_phase_a_a5_formal_timing_and_sequence_failure_reconciliation.md` | PASS |
| Reconciliation SHA-256 | `2A842290DA18A014D9D1A709CB5BCE0248152D890E7A9ED2FE73A80C045B1AE1` | PASS |
| Target-scoped status | Clean | PASS |
| Required architecture report at entry | Absent | PASS |

Exact Git commands were target-scoped and ended in `-- .`. No global untracked-file enumeration was run. The external `../legacy-reference/` sibling was not inspected, enumerated, read, hashed, or modified. The historical fresh-attempt decision, all three retained revised-source blocker reports, and the timing/sequence reconciliation were read completely. The failed candidate was not accessed and is not evidence for this decision.

No build, test, restore, mutant, source edit, harness implementation, candidate operation, PostgreSQL operation, migration operation, service, provisioning, deployment, production access, Phase B, or Correction 2 operation occurred.

## 3. Repository ownership and exact files

The harness is a repository tool, not production runtime code and not part of either application. Ownership is the existing `tools/` automation boundary. It introduces no production project reference and no NuGet dependency.

Exactly five executable/contract files form the harness:

1. `tools/rev869b-a5-formal-acceptance/Invoke-Rev869BA5FormalAcceptance.ps1`
   - the only orchestration owner;
   - exposes exactly the modes `DEVELOPMENT_FEEDBACK_ONLY` and `FORMAL_ACCEPTANCE`;
   - runs commands synchronously and never uses jobs, parallel pipelines, background processes, or asynchronous command dispatch;
   - owns candidate preflight, journal creation, command launch, fail-once lock, instrumentation, mutant orchestration, and checkpoint materialization.
2. `tools/rev869b-a5-formal-acceptance/Verify-Rev869BA5FormalEvidence.ps1`
   - separate read-only verifier;
   - imports no runner function and never executes an A5 command;
   - recalculates evidence and produces PASS only from calculation.
3. `tools/rev869b-a5-formal-acceptance/Test-Rev869BA5FormalEvidenceHarness.ps1`
   - standalone harness-validation driver;
   - creates only disposable fake repositories, commands, journals, streams, TRX fixtures, and manifests;
   - executes all 15 mandatory validation cases and exits nonzero on any unexpected result.
4. `tools/rev869b-a5-formal-acceptance/Rev869BA5FormalPlan.v1.json`
   - immutable ordered command plan;
   - contains the 18 formal stages, expected test arithmetic, exact project/filter identities, build/EF/restore/scan/diff invocations, 40 mutant definitions, allowed executables, zero-operation policy, candidate changed-path allowlist, and output rules.
5. `tools/rev869b-a5-formal-acceptance/Rev869BA5FormalEvidence.v1.schema.json`
   - JSON Schema contract for every journal event, manifest, detached verification result, failure lock, counter record, and validation fixture.

There is one test file: `Test-Rev869BA5FormalEvidenceHarness.ps1`. It is deliberately outside A5 test assemblies so harness validation cannot be counted as A5 evidence. PowerShell AST parsing of all three scripts is an independent harness-acceptance check.

The five-file aggregate harness source identity is:

```text
SHA256(UTF8-LF(path + NUL + git_blob_id + NUL + file_sha256 + LF) for all five paths sorted ordinal)
```

The aggregate is `harnessSourceSha256`. The individual blob, byte size, and SHA-256 of all five files are also recorded. An A5 candidate may not change any harness file.

## 4. Single orchestration owner and modes

The entry script parameters include `-Mode`, `-Purpose`, `-RunId`, `-AuthorizedStartingCommit`, `-CandidateCommit`, `-ExpectedTargetBranch`, `-AuthorizationRecordPath`, and `-AuthorizationRecordSha256`.

`-Mode` accepts only:

```text
DEVELOPMENT_FEEDBACK_ONLY
FORMAL_ACCEPTANCE
```

`-Purpose` is either `A5_ACCEPTANCE` or `HARNESS_VALIDATION`; purpose is not a third evidence mode. A `HARNESS_VALIDATION` journal is permanently ineligible for A5 acceptance.

Development mode writes to a development-only root and never opens a formal journal. Its completion receipt records only that all frozen development gates finished against the candidate precursor; it does not import output into formal evidence. Formal mode requires that receipt before candidate freeze, but recomputes every formal result.

The runner launches one child at a time with `System.Diagnostics.Process`, redirects stdout and stderr to distinct binary files, waits synchronously for exit, closes and hashes all result files, appends and flushes the result event, and only then advances. Each plan stage and every sub-invocation receives the next global sequence number. An unplanned descendant process, parallel child, or overlapping command interval fails the run.

## 5. Immutable candidate binding

Before the formal-start event, the runner must fail closed unless all of the following are recalculated successfully:

- authorization record SHA-256 and exact separately authorized starting commit;
- target branch equals the authorized branch and target HEAD remains the starting commit;
- candidate is one immutable commit whose parent is the authorized starting commit;
- candidate tree is resolved from the commit object;
- isolated candidate worktree is detached at that commit and clean in tracked and target-scoped untracked scope;
- changed paths are obtained from `git diff --name-only -z <authorized> <candidate>` and exactly satisfy the frozen A5 allowlist and maximum count;
- every manifest file is read with `git cat-file blob <candidate>:<path>`, never from worktree bytes;
- every changed file has path, mode, Git blob, byte length, and SHA-256;
- ERP and Control Plane package-lock blobs match their separately authorized project-aware SHA-256 values;
- migration inventory is derived from the committed tree with context, ordered migration ID, path, blob, length, and SHA-256;
- all five harness blobs equal the authorized starting commit and aggregate `harnessSourceSha256`;
- every executable/tool is resolved to an absolute path and has product version, file version, size, and SHA-256;
- candidate worktree bytes equal committed blobs immediately before the marker.

The canonical candidate manifest uses UTF-8 without BOM, LF, ordinal path ordering, invariant decimal numbers, uppercase SHA-256, lowercase Git object IDs, explicit nulls, and no insignificant whitespace. `candidateManifestSha256` is SHA-256 of those exact bytes. Any missing object, identity drift, dirty worktree, unauthorized path, lock mismatch, migration mismatch, tool mismatch, or worktree/blob mismatch fails before formal start.

## 6. Evidence root, fail-once registry, and start event

Formal evidence is outside the repository at the fixed root:

```text
%LOCALAPPDATA%\SESS.NexaERP\REV869B-A5\FormalRuns\<run-id>\
```

The runner atomically creates the run directory and a registry record using create-new semantics. The registry rejects an existing run ID and rejects a prior formal run for the same candidate commit. A new attempt requires both a new candidate identity and new run ID under separate authorization.

Before any formal command, the runner creates the append-only journal, starts required instrumentation, and writes exactly one event whose `eventType` is:

```text
FORMAL_ACCEPTANCE_GATE_STARTED
```

That event includes the run ID, UTC ISO-8601 timestamp, local ISO-8601 timestamp, Windows timezone ID and UTC offset, monotonic sequence/ticks/frequency, authorized start, candidate commit/parent/tree, manifest SHA-256, aggregate harness source SHA-256, previous-event SHA-256, and current-event SHA-256. The journal FileStream is flushed to disk before the first command process is created. No command start may have an earlier sequence, UTC time, or monotonic tick.

Preflight events may precede the marker but can never be command-result events. A marker count other than one fails verification.

## 7. Exact JSONL schema and hash chain

Schema version is `rev869b.a5.formal-evidence/1`. Every JSONL line contains every top-level property below. Non-applicable values are explicit JSON `null`; properties are never omitted.

| Property | Exact content |
|---|---|
| `schemaVersion` | fixed schema string |
| `runId` | lowercase canonical GUID |
| `sequence` | Int64, beginning at 1 and increasing by exactly 1 |
| `eventType` | schema enum |
| `mode`, `purpose` | exact mode and purpose |
| `timestampUtc` | UTC ISO-8601 round-trip string |
| `timestampLocal`, `timezoneId`, `utcOffsetMinutes` | local clock identity |
| `monotonicTicks`, `monotonicFrequency` | one `Stopwatch` epoch for the run |
| `candidate` | authorized start, commit, parent, tree, target branch, detached flag, manifest SHA-256, harness source SHA-256 |
| `command` | plan ID, stage, subordinal, exact executable, argument array, display string, working directory |
| `tool` | absolute path, product/file version, byte size, SHA-256 |
| `timing` | start/end UTC, local time, monotonic ticks, and duration |
| `result` | exit code and discovered/selected/passed/failed/skipped/total/expected counts |
| `stdout`, `stderr`, `trx` | relative evidence path, byte size, SHA-256 |
| `mutant` | ID, enforcement path/location, original/mutated blob and SHA-256, compile/killer result, restored blob/SHA-256/equality |
| `counters` | all observed prohibited-operation counters and their evidence hashes |
| `previousEventSha256` | 64 uppercase hexadecimal characters; zeros only for sequence 1 |
| `currentEventSha256` | 64 uppercase hexadecimal characters |

Event types are exactly `RUN_CREATED`, `PREFLIGHT_CHECKED`, `FORMAL_ACCEPTANCE_GATE_STARTED`, `COMMAND_STARTED`, `COMMAND_COMPLETED`, `MUTANT_STARTED`, `MUTANT_COMPLETED`, `COUNTERS_OBSERVED`, `FORMAL_ACCEPTANCE_FAILED`, and `FORMAL_EXECUTION_COMPLETED`.

Canonical JSON uses the schema property order, UTF-8 without BOM, LF, invariant numbers, JSON escaping, array order preservation, and no whitespace. To calculate an event hash, `currentEventSha256` is set to 64 zeroes, the complete event is canonicalized, and SHA-256 is computed. The resulting uppercase hash replaces the zeroes in the serialized line. `previousEventSha256` equals the immediately preceding calculated hash.

The journal is opened with append-only intent and flushed after every event. The verifier rejects malformed JSON, unknown/omitted/extra properties, wrong schema, a bad canonical form, missing/duplicate/reordered sequence, chain break, changed evidence hash, wrong run/candidate identity, an invalid interval, or any event after failure.

## 8. Fail-once lock

The first nonzero exit, count mismatch, identity drift, invalid command, invalid mutant, restoration mismatch, evidence-chain problem, or nonzero prohibited counter causes this exact order:

1. close/hash command streams;
2. append and disk-flush one `FORMAL_ACCEPTANCE_FAILED` event containing the decisive sequence and evidence;
3. atomically create `FORMAL_ACCEPTANCE_FAILED.lock` with run/candidate/journal-head identity;
4. terminate without launching another formal command;
5. preserve the entire evidence root read-only until blocker verification is complete.

The failure event is the final journal line. The runner refuses the same run ID, the same candidate identity, a pre-existing failure lock, development-result relabeling, and any retry. No later PASS or development result can supersede it.

## 9. Frozen formal sequence

`Rev869BA5FormalPlan.v1.json` contains these stages in exactly this order:

1. A5 suite, fresh process 1: exactly `30/30`.
2. A5 suite, fresh process 2: exactly `30/30`.
3. Retained A4 suite: exactly `23/23`.
4. Complete Phase-A suite, with frozen unique count.
5. Focused REV869B non-PostgreSQL suite, with frozen unique count.
6. Complete ERP non-PostgreSQL suite, with frozen unique count.
7. Warning-as-error builds.
8. PowerShell 5.1 AST checks.
9. ERP and Control Plane EF no-connect discovery.
10. Migration inventory and ordering.
11. Model/snapshot parity.
12. Offline Up/Down SQL generation and hashes.
13. Offline locked restores.
14. Security, secret, privacy, dependency and prohibited-operation scans.
15. Incremental and cumulative `git diff --check`.
16. Exactly 40 semantic production mutants.
17. Final evidence-journal verification.
18. PASS checkpoint generation.

Every multi-command stage has fixed subordinals in the plan. The verifier reconciles raw TRX test identities across invocations and rejects duplicate counting. It rejects any command not in the plan and any missing command. A failure at any stage prevents all later stages.

At stage 17 the runner closes the formal execution journal and launches the independent verifier in a new `powershell.exe -NoProfile` process. The verifier reads but cannot append to the journal. It writes a detached verification result and, only on calculated PASS, canonical checkpoint content. The runner cannot create a verifier PASS; at stage 18 it may only byte-copy verifier-produced checkpoint content to the authorized checkpoint path. Its hash and verifier identity remain independently recalculable.

## 10. Mutant evidence

The plan contains exactly 40 unique mutant records with fixed ID, production enforcement path/location, semantic change, expected compile command, and one non-vacuous killing-test identity. Each mutant uses a fresh disposable worktree at the exact candidate.

For every mutant the journal binds original Git blob/SHA-256, mutated blob/SHA-256, exact one-file production diff, compile result, killing-test identity and raw failure, original Git-blob restoration command, restored blob/SHA-256, and equality. Restoration is from `git cat-file blob <candidate>:<path>`; reverse patches are prohibited. A duplicate mutation, noncompiling mutant, survivor, unrelated/vacuous kill, dirty pre/post state, or restoration inequality fails once.

Required calculated result:

```text
compiled=40
killed=40
survived=0
invalid=0
```

## 11. Observed zero-operation instrumentation

Counters are calculated from retained observations, never assigned as success literals. Before the marker the runner must start:

- Windows process start/stop observation for the complete runner descendant tree;
- Windows TCP/IP ETW connection observation correlated to descendant process IDs and command intervals;
- Windows service-state observation;
- raw command argument/event capture;
- complete TRX identity capture;
- EF output and migration-command classification.

If any observer cannot start, cannot flush, drops events, or cannot be parsed, formal mode fails before the marker. Formal execution is offline and permits zero network connections; this stricter condition makes any PostgreSQL or external connection an immediate failure. The verifier derives counters independently from process, ETW, service, TRX, command, stdout, and stderr artifacts.

```text
postgresql_connections=0
postgresql_tests_executed=0
migration_applications=0
migration_removals=0
service_starts=0
production_access=0
external_deployments=0
```

Any observed prohibited event or ambiguous/dropped instrumentation record fails the run. Command-plan absence alone is not accepted as proof of zero.

## 12. Independent verifier

`Verify-Rev869BA5FormalEvidence.ps1` independently:

- validates canonical schema and recalculates every event hash;
- verifies continuous sequence and exactly one flushed start marker;
- recalculates candidate, parent, tree, branch, manifest, harness, lock, migration, tool, and evidence-file identities;
- validates command intervals, monotonic order, and no pre-marker command;
- reconciles unique test identities, counts, TRX, and raw invocation events;
- recalculates 40 compiled/killed/survived/invalid results and restoration equality;
- recalculates zero-operation counters from raw observer artifacts;
- rejects missing, extra, duplicated, reordered, overlapping, retried, or relabeled commands;
- rejects an event after `FORMAL_ACCEPTANCE_FAILED`;
- rejects runner-authored or schema-shaped fake verifier PASS files;
- emits PASS only after all calculations equal the immutable plan.

The verifier shares no executable code with the runner. The common plan and schema are immutable committed data whose hashes are bound in the authorization record and formal-start event.

## 13. Harness validation before A5

`Test-Rev869BA5FormalEvidenceHarness.ps1` uses disposable fake commands and `purpose=HARNESS_VALIDATION`; its outputs are never A5 evidence. It must demonstrate:

1. correct sequence passes;
2. command before marker rejected;
3. duplicate sequence rejected;
4. missing sequence rejected;
5. reordered event rejected;
6. timestamp reversal rejected;
7. candidate substitution rejected;
8. manifest substitution rejected;
9. modified stdout/stderr/TRX rejected;
10. retry after failure rejected;
11. development relabeling rejected;
12. file change after freeze rejected;
13. mutant restoration mismatch rejected;
14. nonzero PostgreSQL counter rejected;
15. runner-generated fake verifier PASS rejected.

Each case records fixture hash, expected exit, actual exit, exact rejection code, stdout/stderr hashes, and PASS/FAIL. Correct-sequence is the only expected zero exit. Validation passes only at `15/15`; it is explicitly `HARNESS_VALIDATION_NOT_A5_FORMAL_EVIDENCE`.

Stable rejection exits are: `70` schema/sequence, `71` hash chain, `72` candidate/manifest identity, `73` evidence-file hash, `74` fail-once/retry, `75` mode/relabel, `76` freeze drift, `77` mutant validity/restoration, `78` prohibited counter/instrumentation, and `79` verifier provenance/spoof.

## 14. Smallest exhaustive future implementation boundary

Harness implementation is a separate prerequisite change. Its exhaustive allowlist has seven named alternatives:

1. `tools/rev869b-a5-formal-acceptance/Invoke-Rev869BA5FormalAcceptance.ps1`
2. `tools/rev869b-a5-formal-acceptance/Verify-Rev869BA5FormalEvidence.ps1`
3. `tools/rev869b-a5-formal-acceptance/Test-Rev869BA5FormalEvidenceHarness.ps1`
4. `tools/rev869b-a5-formal-acceptance/Rev869BA5FormalPlan.v1.json`
5. `tools/rev869b-a5-formal-acceptance/Rev869BA5FormalEvidence.v1.schema.json`
6. `outputs/rev869b_external_controller_phase_a_a5_formal_evidence_harness_implementation_checkpoint.md`
7. `outputs/rev869b_external_controller_phase_a_a5_formal_evidence_harness_implementation_blocker.md`

Paths 6 and 7 are mutually exclusive. Therefore the maximum changed-path count for a harness implementation outcome is exactly `6`: five fixed harness files plus one checkpoint or blocker. No project, solution, package, lock, source, migration, snapshot, existing test, existing report, README, configuration, or helper path is required.

The existing A5 source implementation allowlist remains exactly 39 named alternatives with maximum 38 paths. It must not be expanded. The accepted harness will already exist in the separately authorized starting commit for a later A5 attempt, and all five harness paths must remain unchanged throughout that candidate.

Future A5 outcome paths remain:

- PASS: `outputs/rev869b_external_controller_phase_a_a5_revised_source_implementation_checkpoint.md`;
- FAIL: `outputs/rev869b_external_controller_phase_a_a5_revised_source_implementation_blocker_3.md`.

They remain mutually exclusive.

## 15. Dependencies and offline requirements

No new package dependency is required. Implementation may use only Windows PowerShell 5.1/.NET Framework APIs, committed JSON, the already pinned .NET SDK/tool manifest, Git, built-in Windows process/service/ETW facilities, and existing locally verified offline NuGet artifacts.

Harness implementation and validation require no restore. A5 formal restores are locked, use only the separately authorized local package source and isolated packages directory, disable HTTP sources/audit/cache fallback as frozen, and fail on any network observation. Executable paths, versions, sizes, and hashes are authorization inputs and are rechecked before the marker.

## 16. Evidence retention and cleanup

Runtime evidence remains outside the repository under the fixed run root. It contains `journal.jsonl`, candidate manifest, authorization record copy/hash, stdout/stderr binary files, TRX/results, SQL, restore, scan, diff, instrumentation artifacts, mutant subdirectories, failure lock when applicable, detached verification result, and calculated checkpoint/blocker content.

The runner never automatically deletes evidence. On PASS it remains until checkpoint commit, report SHA-256, candidate/blob equality, commit boundary, final target-scoped cleanliness, and a separate read-only verifier PASS are confirmed. On FAIL it remains until blocker commit, blocker SHA-256, one-file boundary, target cleanliness, and decisive-evidence verification are confirmed.

Cleanup requires separate authorization, exact resolved run-root validation beneath the fixed parent, run ID/candidate/journal-head match, and confirmation that no required report verification remains. Only that one validated run directory may be removed. A failure lock and formal history are never converted to PASS before cleanup.

## 17. Candidate reuse

```text
A5_FAILED_CANDIDATE_SOURCE_REUSE=DEVELOPMENT_SEED_ONLY
```

The failed commit/tree may inform future development only. It supplies no formal test evidence, mutant evidence, candidate identity, manifest authority, or management acceptance. It is not accessed or reused in this turn.

## 18. Exact single next management gate

The single next gate is separate management authorization to implement and validate exactly the five harness files plus one mutually exclusive harness checkpoint/blocker path, from the exact then-authorized report-only HEAD, with a maximum six-path outcome. A5 source implementation remains `NO_GO_PENDING_HARNESS_ACCEPTANCE` until that implementation passes all 15 fake-command validations, AST/source-contract checks, independent review, one-commit boundary verification, and clean target status.

No A5 implementation or formal acceptance begins automatically.

## 19. Canonical retained states

```text
phase_a_management_acceptance_state=FAIL_PENDING_INDEPENDENT_REVIEW
phase_b_state=NO_GO
correction_2_state=NO_GO
postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN
external_provisioning_state=NOT_STARTED
production_readiness_state=NOT_READY
```
