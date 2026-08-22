# REV869B A5 first-acceptance-test failure reconciliation

Date: 2026-08-22

Decision type: report-only failure-evidence and formal-gate reconciliation

## 1. Decision

`A5_FIRST_ACCEPTANCE_FAILURE_RECONCILIATION_GATE=NO_GO`

Primary root-cause classification:

`INSUFFICIENT_RETAINED_EVIDENCE`

The retained target-scoped evidence identifies the failed literal A5 test, its position in the frozen test list, the
aggregate `29/30` result, the semantic nature of the test-only correction and the later aggregate `30/30` result. It
does not retain enough raw evidence to supply every mandatory failure-reconstruction field. In particular, the first
run's fully qualified name, exact command/filter, execution timestamp, process exit code, assertion text,
expected/actual values, stack trace and source locations are absent. No candidate test blob, before/after file hash,
TRX file, console log or formal manifest survived rollback.

Accordingly, the likely test-assertion correction is not promoted to a fully evidenced formal PASS. No production
defect, nondeterminism or environment defect is inferred from missing evidence.

`A5_REVISED_SOURCE_IMPLEMENTATION_GATE=NOT_REAUTHORIZED_FROM_THIS_RECONCILIATION`

## 2. Entry and repository boundary

| Check | Observed result | Status |
|---|---|---|
| HEAD | `d5d38b4460f6caa2f5bfc4c8287d2f82d01bfc1f` | PASS |
| Parent | `65aff8032551c00b24e5898056a0c2336c569e36` | PASS |
| Branch | `master` | PASS |
| Subject | `REV869B A5 failed-gate-history blocker` | PASS |
| HEAD boundary | Exactly one modified blocker report | PASS |
| Blocker path | `outputs/rev869b_external_controller_phase_a_a5_revised_source_implementation_blocker_3.md` | PASS |
| Blocker SHA-256 | `0EDAAA183F874A49F13D726C5AC9B6F5E5D238F85903F7B08B20B9EA7A6CBA71` | PASS |
| Target-scoped status at entry | Clean | PASS |
| Implementation checkpoint | Absent | PASS |
| Rolled-back new A5 implementation artifacts sampled from the frozen boundary | Absent | PASS |

HEAD is a normal one-parent commit. It changes only the authoritative blocker report. No A5 implementation source,
test, project, package lock, migration or checkpoint is committed at HEAD. The external legacy sibling was not
accessed, enumerated, hashed or verified.

The complete blocker report was read. The target-scoped reports identified by its retained SHA-256 evidence were
read completely: the immutable action/boundary decision, controlled official EF package verification, package
evidence-integrity reconciliation, dual-context migration/package decision and both project-aware lock
reconciliations. A target-only search found no retained TRX, console transcript or source copy containing the failed
assertion details.

## 3. Evidence hierarchy

This reconciliation distinguishes three evidence levels:

1. **Committed evidence**: immutable reports and Git commit boundaries at the authorized HEAD.
2. **Current execution-session evidence**: the recorded command and aggregate output from the later corrected run,
   plus the recorded incremental edit description. These observations were not written to a retained target log
   before rollback.
3. **Absent evidence**: details neither committed nor present in the current execution-session record. These are
   reported exactly as `NOT_EVIDENCED`.

Session evidence may explain why the stop occurred, but it cannot replace the missing immutable first-run record
required by this reconciliation's GO criteria.

## 4. First failed-run reconstruction

| Required field | Reconciled value | Evidence status |
|---|---|---|
| Literal test method | `A5_EachActionInvokesExactlyItsExistingPurchaseMethodWithDerivedIdempotencyAndNoDirectBusinessDml` | Committed blocker and boundary decision |
| Frozen test number | A5 test `27` of `30` | Committed boundary decision |
| Fully qualified test name | `NOT_EVIDENCED` | Namespace/class-qualified runner output was not retained |
| Theory/data row | No row is identified; whether the method was a `Fact` or parameterized case in the failed candidate is `NOT_EVIDENCED` | Candidate source was rolled back |
| Source line number | `NOT_EVIDENCED` | Candidate source was rolled back |
| Exact first-run command | `NOT_EVIDENCED` | No first-run transcript or command record survives |
| Exact first-run filter | `NOT_EVIDENCED` | No first-run transcript or command record survives |
| Execution timestamp | `NOT_EVIDENCED` | Neither console output nor report records it |
| Exit code | `NOT_EVIDENCED` | A nonzero code is expected for a failed `dotnet test`, but is not asserted as evidence |
| Raw assertion/error message | `NOT_EVIDENCED` | No TRX or console failure block survives |
| Expected value | `NOT_EVIDENCED` | No raw assertion output survives |
| Actual value | `NOT_EVIDENCED` | No raw assertion output survives |
| Stack trace | `NOT_EVIDENCED` | No raw assertion output survives |
| Runner source locations | `NOT_EVIDENCED` | No raw assertion output or candidate source survives |

### First-run arithmetic

The committed blocker says that the first exact 30-test A5 invocation returned `29/30` and identifies one failed
test. The only arithmetic consistent with those statements is:

```text
passed=29
failed=1
skipped=0
total=30
```

This arithmetic is derived from the committed aggregate, not reproduced from a retained runner summary.

## 5. Correction reconstruction

The committed blocker states that the original assertion searched only for a Purchase method token immediately
followed by `(` and therefore did not tolerate the production call formatting. It also states that the assertion was
corrected before the successful rerun.

The current execution-session record identifies the changed candidate path as:

`tests/SESS.NexaERP.ControlPlane.Tests/A4FailureCorrectionContractTests.cs`

and describes the incremental assertion edit as changing the per-method source-count search from the equivalent of:

```text
Count("." + method + "(")
```

to the broader receiver-and-method identity search equivalent to:

```text
Count("purchase." + method)
```

The semantic purpose was to count each fixed `purchase.<method>` invocation regardless of whitespace or a line
break before the opening parenthesis. The required invariant remained: every one of the 19 server-owned action IDs
maps exactly once to its approved existing Purchase service method, with no direct business DML.

Evidence limitations:

- the exact before/after candidate bytes and Git blobs are `NOT_EVIDENCED`;
- the exact assertion line number is `NOT_EVIDENCED`;
- no retained diff proves that these were the only bytes changed between the two runs; and
- the production candidate was not committed, so production/test tree equality cannot be reconstructed now.

The available evidence describes a **test-only assertion-shape correction**. It does not describe a production
source change made to obtain the later 30/30 result. Production behavior therefore was not intentionally changed by
this correction. This conclusion is limited to the recorded semantic edit; a byte-exhaustive comparison is
unavailable.

The corrected test file was included in the final rollback. It is absent at this HEAD, as are the new A5 provider,
endpoint and persistence-project artifacts. The correction is understandable and can be reimplemented within the
already allowed test path, but exact byte-for-byte reproduction is impossible from retained evidence alone.

## 6. Corrected rerun evidence

The current execution-session record retains this later command:

```powershell
dotnet test tests\SESS.NexaERP.ControlPlane.Tests\SESS.NexaERP.ControlPlane.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~A4FailureCorrectionContractTests' --logger 'console;verbosity=minimal'
```

Its recorded result was:

```text
exit_code=0
passed=30
failed=0
skipped=0
total=30
```

The exact rerun execution timestamp is `NOT_EVIDENCED`. The session record establishes that the test assertion file
changed between the first and second invocations. It does not retain file hashes, a frozen changed-file manifest or
an immutable runner log. Therefore the later result is classified as:

`CANNOT_BE_INDEPENDENTLY_EVALUATED`

It is not merely an unchanged rerun, because a test edit is recorded. There is no evidence of order/timing
nondeterminism. The result is consistent with a valid assertion correction, but it does not prove one under the
required immutable-evidence standard.

## 7. Root-cause classification

Primary classification:

`INSUFFICIENT_RETAINED_EVIDENCE`

Contributing causes:

1. `TEST_ASSERTION_DEFECT` — supported at the semantic level: the initial source-token assertion improperly coupled
   the invariant to call formatting.
2. `PREMATURE_FORMAL_GATE_START` — the failed invocation was later treated as the first formal acceptance run even
   though the implementation/test candidate and changed-file manifest were not yet frozen and the test assertion was
   still under development.
3. Evidence-capture discipline was incomplete: no formal-run declaration, manifest, source/test hashes, timestamped
   command record, TRX/console failure evidence or process-exit record was retained before rollback.

Not supported by evidence:

- `PRODUCTION_SOURCE_DEFECT`;
- `TEST_FIXTURE_DEFECT`;
- `MUTATION_OR_HARNESS_DEFECT` as the primary failure mechanism;
- `ORDER_OR_TIMING_NONDETERMINISM`; or
- `ENVIRONMENT_OR_COMMAND_DEFECT`.

## 8. Frozen future development/prequalification stage

The future process must treat all implementation-time feedback as non-acceptance:

1. Every build or test command before formal declaration is labelled `DEVELOPMENT_FEEDBACK_ONLY` in the execution
   record before it starts.
2. Source, tests and authorized harness commands may be corrected during this stage, but every change remains inside
   the existing 39-path implementation allowlist.
3. Each run records exact UTC and local timestamps, command line, working directory, process exit code, test filter,
   runner summary and any failure block.
4. The failed test's fully qualified name and source location are captured after the candidate test compiles.
5. Before formal declaration, test 27 must pass repeatedly in fresh processes. Any inconsistent result is a
   development defect and must be resolved before formal acceptance starts.
6. Development runs are never included in formal acceptance arithmetic and cannot trigger a claimed formal PASS.
7. No development failure is hidden: it is labelled as development feedback and retained separately from the later
   formal evidence.

No repository helper or new harness file is required. The commands may be orchestrated outside the repository; the
authorized A5 test path contains the test itself, and the authorized success checkpoint records final evidence.

## 9. Frozen future formal acceptance stage

Formal acceptance begins only after all six preconditions are recorded:

1. every implementation and test file is complete;
2. the exact changed-file manifest and SHA-256 table are frozen;
3. ERP lock SHA-256 is
   `06DF9719F8A02E344C4F565DBB538DD5E39F5D49B600081291FC940EF1E1F953` and Control Plane lock SHA-256 is
   `4D99C6E0124356EB289FCB231E89A6534A6BB1FAE1F42A5A01BCFE0D60F89DEB`;
4. the warning-as-error build passes;
5. target-scoped diff, allowlist, privacy, secret and security scans pass; and
6. a formal-gate declaration records that no source, test, project, migration, lock or harness bytes may change.

Once declared:

- test 27 must pass in at least 10 consecutive fresh processes;
- the complete A5 subset must pass `30/30` in at least two fresh-process executions;
- each declared formal command must pass on its first execution;
- retained A4 must pass `23/23`;
- a failed formal command immediately ends that implementation attempt;
- no correction and rerun converts a failed formal attempt into PASS;
- mutants begin only after every formal build/test gate passes;
- every one of the 40 mutants still uses its separately required fresh isolated baseline; and
- any post-declaration candidate-file change invalidates all formal evidence and requires a new formal gate from the
  beginning. If the change follows a formal failure, the failed implementation attempt remains ended and requires a
  separately authorized new attempt.

For every formal run, evidence records the exact candidate manifest identity, command, timestamps, exit code,
passed/failed/skipped/total arithmetic, failure details and process identity. This prevents a corrected rerun from
masking a failed formal attempt.

These requirements fit the existing boundary. The failed test belongs in the already allowed
`tests/SESS.NexaERP.ControlPlane.Tests/A4FailureCorrectionContractTests.cs`; the two project-aware locks, production
paths, tests and mutually exclusive checkpoint/blocker paths are already named. Repeated process execution and
external evidence capture do not require a 40th implementation path, package, project edge or architecture change.

## 10. Retained architecture and execution evidence

The failure-evidence gap changes no frozen architecture or package conclusion:

| Evidence | Retained value |
|---|---|
| ERP project-aware lock | `06DF9719F8A02E344C4F565DBB538DD5E39F5D49B600081291FC940EF1E1F953` |
| ERP nodes | 41 NuGet + 3 project = 44 |
| Control Plane project-aware lock | `4D99C6E0124356EB289FCB231E89A6534A6BB1FAE1F42A5A01BCFE0D60F89DEB` |
| Control Plane nodes | 35 NuGet + 1 project = 36 |
| Official package trust | `41/41 PASS` |
| Authoritative offline package-only replays | `4/4 PASS` |
| PostgreSQL connection attempts/connections during the stopped attempt | `0/0` |
| Migration applications/removals | `0/0` |
| Production mutants executed | `0/40` |
| Frozen implementation allowlist | Unchanged: 39 named alternatives, maximum 38 changed paths |

No package hash, identity, signature, timestamp, certificate, revocation result, lock graph, assets graph, migration
inventory, ownership boundary or deployment topology is changed by this reconciliation.

## 11. Exact blockers and next gate

GO is withheld because these mandatory fields are not retained:

1. first-run fully qualified test name and any data row identity;
2. exact first-run command and filter;
3. exact first-run timestamp and exit code;
4. raw assertion message, expected value, actual value and stack trace/source locations;
5. pre-correction and post-correction test blobs/hashes or a retained exact diff; and
6. a frozen candidate manifest proving what did and did not change between runs.

The exact next gate is management review of this evidence gap. Management may supply immutable retained logs or
artifacts that close every missing field, or may issue a separate report-only decision explicitly accepting that the
historical details are unrecoverable and governing any new attempt solely through the frozen
development/prequalification-versus-formal protocol above. A5 source implementation may not be separately
re-authorized by this report itself.

This report performs no implementation, build, test, mutant, package restore, migration operation, PostgreSQL
access, service start, deployment, provisioning, production activity, Phase B or Correction 2.

Retained states:

`phase_a_management_acceptance_state=FAIL_PENDING_INDEPENDENT_REVIEW`

`phase_b_state=NO_GO`

`correction_2_state=NO_GO`

`postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN`

`production_readiness_state=NOT_READY`
