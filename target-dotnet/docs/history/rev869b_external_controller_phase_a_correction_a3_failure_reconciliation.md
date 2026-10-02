# REV869B Option-A Phase-A Correction A3 failure reconciliation

Reconciled review commit: `ba99fc90a06d387d98396cbe80a23323f8f0baf0`

Reviewed A3 source commit: `634a5b3edb29bd241a6cb703a9e8c9ecfda092f5`

Frozen architecture baseline: `51476760adcea9ed7babbc04d642e53e371c6941`

Reconciliation verdict: `A3_FAILURES_RECONCILED_FOR_SOURCE_ONLY_CORRECTION`

`PHASE_A_CORRECTION_A4_SOURCE_ONLY_GATE=GO`

This report reconciles only independently confirmed F03, F04, F06 and F07. F01, F02 and F05 remain retained passes. The required changes refine contracts and enforcement already fixed by the Phase-A architecture: management authorization is separate from execution; authorization rows require version but no lease; execution rows require a lease/fence/version; authoritative readers and the oracle are separate; evidence generation remains offline. No architecture contradiction or Phase-B dependency is required to make the source correction.

No A4 implementation is included in this report.

## 1. Stage-0 gate

| Gate | Reproduced result | Status |
|---|---|---|
| HEAD | `ba99fc90a06d387d98396cbe80a23323f8f0baf0` | PASS |
| Parent | `634a5b3edb29bd241a6cb703a9e8c9ecfda092f5` | PASS |
| Subject / branch | `REV869B Phase-A Correction A3 independent source safety review` / `master` | PASS |
| HEAD content | Exactly one file: `outputs/rev869b_external_controller_phase_a_correction_a3_independent_source_safety_review.md` | PASS |
| Independent-review SHA-256 | `F7297EDB7B32B29FFF24DDC02AE1CABB62F8FE9F721CAE543483EBACBE712B77` | PASS |
| Target status | Clean before reconciliation | PASS |
| Legacy boundary | `../legacy-reference/` remained untracked; no legacy content was opened, read, changed or used | PASS |
| Mandatory reading | Architecture freeze, A2 review/reconciliation, A3 checkpoint, A3 independent review, and every production/test location cited for F03/F04/F06/F07 were read completely | PASS |

## 2. Reconciled F01-F07 disposition

| Finding | Disposition | Reconciliation boundary |
|---|---|---|
| F01 | `PASS_RETAINED` | Preserve raw-only protected ingress, canonical signed bytes, issuer/key/signature, nonce, freshness, scope, idempotency and health/version-only endpoints. |
| F02 | `PASS_RETAINED` | Preserve the non-decomposable durable provider: one descriptor, one authoritative snapshot read, one atomic mutation, zero exported partial mutation facets. |
| F03 | `FAIL_RECONCILED_A4_REQUIRED` | Add explicit plan identity/version, separate server-owned authorizer/executor identities, and freeze authorization-before-lease execution ordering. |
| F04 | `FAIL_RECONCILED_A4_REQUIRED` | Move complete caller-metadata comparison and reader readiness/cardinality preflight before every `ReadAsync`. |
| F05 | `PASS_RETAINED` | Preserve readiness freshness/cardinality, audit receipt binding and server-controlled privacy/evidence limits. |
| F06 | `FAIL_RECONCILED_A4_REQUIRED` | Add independent production-path tests and mutants at each corrected trust boundary; helpers remain data-only. |
| F07 | `FAIL_RECONCILED_A4_REQUIRED` | Freeze 611 raw events and bind canonical evidence to exact executable/tool identity and instrumented zero-operation counts. |

## 3. F03 root cause and exact correction contract

### Classification

| Required field | Reconciliation |
|---|---|
| Exact source locations | `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs:1072-1090`; `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs:824-874,958-959`; `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs:224-330`; `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs:1402-1428,2028` |
| Violated invariant | One immutable server-authorized grant must bind an exact plan revision, distinct authorizer and executor, then be consumed only after authoritative lease acquisition with the exact fence. |
| Root cause | The grant has no explicit plan ID/version. `ExecutorClass` is populated with the current authorization caller's workload and later compared with the execution caller, collapsing two identities. Authorization tests reuse one workload and attach a lease to a no-lease authorization row. |
| Security/operational impact | Separation of duties cannot be proven; a legitimate separate executor is rejected or authorizer/executor roles are collapsed; plan revision and lease ordering are ambiguous; stale grant/lease reuse cannot be conclusively denied. |
| Source correction required | Replace the ambiguous grant fields with explicit canonical bindings and enforce the frozen lifecycle/order below before lifecycle or atomic mutation. |
| Tests required | The exact A4 F03 tests in section 8, including distinct identities, plan substitutions, pre-lease denial, stale fence denial and ordered positive/replay traces. |
| Production mutants required | M01 plan-version removal, M02 authorizer/executor substitution, M03 lease-order bypass. |
| Acceptance evidence | Raw canonical signed-material trace; authoritative provider trace; zero lifecycle/atomic calls on every mismatch; exactly one complete atomic commit; exact completed replay. |
| External provisioning dependency | None for source-only acceptance. Real IAM, KMS and durable provider deployment remain later prerequisites and may not be simulated as production readiness. |
| Phase allocation | Phase A. These are frozen contract/enforcement rules, not Phase-B deployment or database implementation. |

### Frozen production data contract

Canonical signed intent, server-resolved authorization, `StoredAuthorizationGrantV3`, atomic transaction request, durable outcome and audit receipt must bind, without aliasing:

1. `request_id` and canonical request digest;
2. tenant ID, organization ID, database cluster/instance, resource type/ID/version;
3. lifecycle operation;
4. `approved_plan_id`;
5. `approved_plan_version`;
6. immutable approved-plan digest covering both plan fields and canonical parameters;
7. `management_authorizer_identity` from the trusted server authorization result;
8. `executor_workload_identity` from server policy for the authorized operation;
9. policy row/identity, policy version and policy artifact hash;
10. evidence manifest identity/version/digest;
11. whether execution requires a lease;
12. grant issuer, key, signature, contract version, nonce and idempotency identity;
13. not-before, expiry and one-time grant state;
14. after acquisition only: exact lease ID, holder executor, controller epoch, fencing token and expiry.

Neither plan field may be inferred from `ResourceVersion` or from parameter hash alone. Both plan fields must occur in canonical signed bytes and the persisted grant. Caller payloads may repeat these fields only for exact comparison.

Management authorizer and executor identities are distinct semantic fields sourced by server-owned policy. Caller role/workload claims cannot choose either. By default they must differ. Any exceptional same-identity policy is prohibited in A4 unless a future architecture freeze explicitly names the operation, policy identity, approval rule and audit treatment; no such exception exists now.

### Frozen lifecycle order

```text
management authorization approved (no lease)
→ exact ACTIVE grant persisted
→ approved executor requests lease using the grant identity
→ durable provider atomically acquires authoritative lease/fence for that executor
→ execution request proves exact grant + plan ID/version + executor + live lease/fence
→ lifecycle decision occurs
→ one composite atomic persistence operation records grant consumption, lifecycle/attempt,
  idempotency, nonce, result and audit outbox
→ durable terminal result
→ an exact completed replay returns only the original durable result
```

Execution before lease acquisition, lease acquisition by another workload, another plan/version, authorizer acting as executor, partial mutation before validation, stale/expired grant, stale lease/epoch/fence, cross-resource reuse and second grant consumption must fail closed.

## 4. F04 root cause and server-owned reader preflight

### Classification

| Required field | Reconciliation |
|---|---|
| Exact source locations | `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs:520-568,586-637`; `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs:1558-1609` |
| Violated invariant | Every reader mismatch or invalid reader set must deny before evidence collection, oracle evaluation, lifecycle calls or persistence mutation. |
| Root cause | Server resolution is pinned, but caller-declared bundle equality/version/artifact/schema/stage is checked after `ReadAsync`; the A3 test expects one read for mismatches. |
| Security/operational impact | Unauthorized envelopes can trigger authoritative data access, resource consumption, logs/timing exposure or reader-side effects before denial. |
| Source correction required | Introduce one complete server-owned preflight over the exact reader multiset and caller comparison metadata before constructing any read request. |
| Tests required | Exact zero-read tests for version, artifact, schema, stage, missing, duplicate, revoked, stale and unexpected readers; one positive all-readers-once trace. |
| Production mutants required | M04 moves one mismatch validation after `ReadAsync`. |
| Acceptance evidence | Ordered call trace showing expectation/descriptor preflight, denial audit on failure, then reads, then oracle only after all authoritative facts. |
| External provisioning dependency | None for source-only contract tests. Real least-privilege reader deployment remains an external prerequisite. |
| Phase allocation | Phase A. Reader ownership and call ordering are frozen source contracts. |

### Required preflight algorithm

Before the first `ReadAsync`, the verifier must:

1. load the trusted server expectation;
2. validate exact required-reader count and uniqueness;
3. resolve each descriptor using only server-pinned reader ID and version;
4. compare exact ID, version, artifact hash, schema, lifecycle stage, compatibility floor, downgrade policy, revocation state, readiness and allowed scope/resource;
5. compare caller-declared metadata against the server descriptor as untrusted comparison data;
6. construct immutable read requests only from trusted expectation/descriptor data;
7. emit a correctly bound audit denial and stop on any mismatch;
8. invoke every valid reader exactly once;
9. validate returned authoritative facts independently of caller facts;
10. invoke the oracle exactly once only after the complete authoritative fact set is collected and validated.

For any caller mismatch or missing, duplicate, unexpected, revoked, stale or unready reader, required counters are: reader calls `0`, oracle calls `0`, lifecycle calls `0`, persistence mutation calls `0`; denial audit receipt `1` with exact envelope/request, tenant, resource/version, attempted operation, trusted identity, policy, reason and result.

## 5. F06 root cause and independent assurance contract

### Classification

| Required field | Reconciliation |
|---|---|
| Exact source locations | `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs:1402-1609,1905-2040`; `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs:454-790` |
| Violated invariant | Tests must traverse production enforcement, model separate trust identities and assert the mandatory denial boundary; helpers may supply data but cannot decide authorization/lifecycle/reader rules. |
| Root cause | The positive grant fixture shares one workload, pre-binds a lease, lacks explicit plan version and expects one read for caller reader mismatch. Existing mutants therefore do not cover the missing ordering/identity/evidence boundaries. |
| Security/operational impact | A green suite can certify collapsed duties, pre-authorization reads and declared rather than measured evidence. |
| Source correction required | Keep rules only in production types; make fixtures passive inputs/counters; add the exact tests and mutants below. |
| Tests required | Fifteen new control-plane facts and two new ERP evidence facts, with no removal or renaming of existing tests. |
| Production mutants required | Six exact compiled mutants M01-M06 below. |
| Acceptance evidence | Literal test discovery, production stack traces/counters, mutant file hashes, compile logs, intended assertion failures, source restoration hashes and zero residue. |
| External provisioning dependency | None. All are source-only/offline. |
| Phase allocation | Phase A assurance. |

Helpers may construct canonical bytes, fixed server responses and counters. They must not reproduce operation→role mapping, grant validation, lifecycle order, reader selection, canonical evidence comparison or readiness decisions. Production methods must be the only decision point.

## 6. F07 root cause and canonical evidence contract

### Classification

| Required field | Reconciliation |
|---|---|
| Exact source locations | `outputs/rev869b_external_controller_phase_a_checkpoint.md:109-169`; `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs:454-590,612-790` |
| Violated invariant | Raw events count every invocation; canonical SQL evidence identifies exact source/process/tool/input/output and measures prohibited-operation counts. |
| Root cause | The checkpoint omits the separately executed three tests from raw events, stores version strings without executable/tool artifact hashes, defaults commit to the A3 parent and sets migration-application count to literal zero. |
| Security/operational impact | Evidence cannot identify the exact generator or prove no application attempt; arithmetic cannot be reproduced under one definition. |
| Source correction required | Extend canonical evidence and its worker instrumentation; update only the checkpoint after machine validation. |
| Tests required | A4 exact tool/source/process binding and independently instrumented zero-operation counters. |
| Production mutants required | M05 executable/tool substitution and M06 application-counter substitution. |
| Acceptance evidence | Two fresh processes emit byte-identical canonical JSON and SQL; exact hashes/counters/commands/timestamps; all formulas reconcile. |
| External provisioning dependency | None; endpoint remains inert and `--no-connect`. No real PostgreSQL is authorized. |
| Phase allocation | Phase A source evidence. |

Each canonical evidence record must bind:

- reviewed source-manifest hash and supplied source commit identity without self-reference;
- executable absolute path and SHA-256;
- .NET SDK, runtime and host identity;
- resolved EF tool artifact path, package/version and SHA-256;
- evidence worker assembly path/hash and helper/test source hash;
- exact command and ordered arguments;
- absolute working directory;
- allowlisted environment inputs and values that can affect output;
- migration IDs plus every source/input hash;
- generation options, UTF-8 without BOM and exact newline normalization;
- Up/Down byte counts, line-feed counts and SQL SHA-256;
- process start/end UTC timestamps and exit code;
- independently instrumented PostgreSQL connection attempts;
- independently instrumented migration-application attempts and successes.

The instrumentation boundary must observe the application API/command path and cannot populate zeros as constants. Required source-only measurements remain: PostgreSQL connections `0`; migration applications attempted `0`; migration applications completed `0`.

## 7. Corrected arithmetic frozen for A3

| Set/invocation | Count | Counting rule |
|---|---:|---|
| A3 subset | 16 | 13 control-plane + 3 canonical ERP tests |
| Complete Phase-A assembly | 63 | Unique tests |
| Focused REV869B ERP subset | 79 | Overlaps complete ERP |
| Complete ERP non-PostgreSQL assembly | 453 | Unique tests |
| Unique cross-assembly total | 516 | `63 + 453` |
| Raw invocation pass events | 611 | `16 + 63 + 79 + 453` |
| PostgreSQL discovery/execution | 87 / 0 | Discovery only |

If the three canonical tests are invoked as a separate command after a 13-test control command, those two commands jointly constitute the 16-test A3 invocation: `13 + 3`. The three ERP tests are already members of the 79 and 453 unique sets, so they do not alter unique totals. They are nevertheless real raw events in the separate A3 command and may not be omitted. `608` is prohibited.

Measured retained counts: PostgreSQL connections `0`; migration applications attempted `0`; migration applications completed `0`. A4 must reproduce these through instrumentation.

## 8. A4 exhaustive minimal file allowlist

Exactly these eight files may change in one future A4 correction commit. Every unnamed path is forbidden.

| # | Allowed file | Required mapping |
|---:|---|---|
| 1 | `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs` | F03 explicit plan/authorizer/executor/grant/lease bindings and signed/transaction/audit records. |
| 2 | `src/SESS.NexaERP.ControlPlane/Configuration/ControlPlaneOptions.cs` | F03 server-owned authorizer/executor separation policy and exact option validation; preserve F01/F02. |
| 3 | `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs` | F03 no-lease authorization, post-grant lease-bound execution, exact grant consumption and replay ordering. |
| 4 | `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs` | F03 canonical signed material, server resolution, pre-lifecycle exact comparison and atomic request construction. |
| 5 | `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs` | F04 complete metadata/cardinality/readiness preflight before `ReadAsync`, ordered audit/read/oracle flow. |
| 6 | `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs` | F03/F04/F06 exact production-path tests and passive fixtures. |
| 7 | `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs` | F06/F07 exact process/tool/source evidence and observed no-operation counters. |
| 8 | `outputs/rev869b_external_controller_phase_a_checkpoint.md` | F07 corrected arithmetic, commands, hashes, counters, boundaries and retained states after validation. |

No project, migration, model snapshot, new helper, endpoint, host `Program`, `Rev869BExecutionBinding`, `AcceptanceVerifierOptions`, SQL, script or other report is permitted. If implementation proves another path necessary, A4 is blocked and management must authorize a new architecture/allowlist reconciliation before any additional change.

## 9. Exact A4 acceptance tests

Add exactly these 17 new literal `[Fact]` methods; do not remove or rename an existing test:

Control-plane assembly, 15 methods:

1. `A4_ApprovedPlanIdSubstitutionFailsBeforeLifecycleAndAtomicMutation`
2. `A4_ApprovedPlanVersionSubstitutionFailsBeforeLifecycleAndAtomicMutation`
3. `A4_ManagementAuthorizerSubstitutionFailsBeforeLeaseLifecycleAndAtomicMutation`
4. `A4_ExecutorWorkloadSubstitutionFailsBeforeLeaseLifecycleAndAtomicMutation`
5. `A4_AuthorizerExecutorConflationFailsWhenSeparationIsRequired`
6. `A4_ExecutionBeforeAuthoritativeLeaseAcquisitionFailsClosed`
7. `A4_StaleLeaseEpochFenceAndGrantReuseFailBeforeLifecycleAndAtomicMutation`
8. `A4_ExactAuthorizationLeaseExecutionAtomicOutcomeAndReplayOrderIsEnforced`
9. `A4_ReaderVersionMismatchProducesAuditDenialAndZeroDownstreamCalls`
10. `A4_ReaderArtifactMismatchProducesAuditDenialAndZeroDownstreamCalls`
11. `A4_ReaderSchemaMismatchProducesAuditDenialAndZeroDownstreamCalls`
12. `A4_ReaderLifecycleStageMismatchProducesAuditDenialAndZeroDownstreamCalls`
13. `A4_MissingDuplicateUnexpectedRevokedStaleOrUnreadyReaderProducesZeroDownstreamCalls`
14. `A4_ExactPinnedReaderSetReadsEachOnceThenInvokesOracleOnce`
15. `A4_TestHelpersArePassiveAndContainNoProductionTrustRules`

ERP assembly, 2 methods:

16. `A4_CanonicalEvidenceBindsExactExecutableToolSourceCommandEnvironmentAndProcessIdentity`
17. `A4_CanonicalEvidenceMeasuresZeroConnectionsApplicationAttemptsAndSuccesses`

Every negative reader test must assert reader `0`, oracle `0`, lifecycle `0`, atomic mutation `0`, denial audit `1` and exact denial binding. Every negative grant/lease test must assert lifecycle `0`, atomic mutation `0` and exact denial audit. The ordered positive test must use distinct authorizer/executor identities, authorization with no lease, authoritative lease acquisition by the approved executor, one atomic commit, and exact completed replay.

With exactly 15 new control and 2 new ERP tests and no removals:

- A4 subset: `17`.
- Complete Phase-A assembly: `63 + 15 = 78`.
- Focused REV869B ERP subset: `79 + 2 = 81`.
- Complete ERP non-PostgreSQL assembly: `453 + 2 = 455`.
- Unique cross-assembly total: `78 + 455 = 533`.
- Required formal raw events, including retained A3 regression: `17 + 16 + 78 + 81 + 455 = 647`.
- PostgreSQL discovery remains `87`; execution remains `0` because no PostgreSQL test is added.

## 10. Exact A4 production mutants

Each mutant is applied alone in a disposable copy, changes a real production/evidence enforcement point, compiles with zero warnings/errors and is killed by its named intended test—not syntax or unrelated failure. Record the mutated-file SHA-256, build result, exact failure and restoration hash.

| ID | Required production mutation | Intended killer |
|---|---|---|
| A4-M01-PLAN-VERSION-BINDING | Remove/neutralize the persisted and canonical exact `approved_plan_version` comparison | `A4_ApprovedPlanVersionSubstitutionFailsBeforeLifecycleAndAtomicMutation` |
| A4-M02-AUTHORIZER-EXECUTOR-SUBSTITUTION | Accept current caller workload as executor or management authorizer | `A4_ExecutorWorkloadSubstitutionFailsBeforeLeaseLifecycleAndAtomicMutation` and conflation test |
| A4-M03-LEASE-ORDER-BYPASS | Permit execution before authoritative lease acquisition or omit exact fence check | `A4_ExecutionBeforeAuthoritativeLeaseAcquisitionFailsClosed` |
| A4-M04-POST-READ-METADATA-VALIDATION | Move one version/artifact/schema/stage mismatch comparison after `ReadAsync` | matching zero-downstream reader test |
| A4-M05-TOOL-IDENTITY-SUBSTITUTION | Replace exact executable/EF artifact identity or hash with version-only/caller value | `A4_CanonicalEvidenceBindsExactExecutableToolSourceCommandEnvironmentAndProcessIdentity` |
| A4-M06-APPLICATION-COUNTER-SUBSTITUTION | Replace observed application attempt/success counters with typed constants | `A4_CanonicalEvidenceMeasuresZeroConnectionsApplicationAttemptsAndSuccesses` |

Formula: `mutants_total=6`, `compiled=6`, `killed_for_intended_reason=6`, `survived=0`, `invalid=0`, `residue=0`. Re-run the eight A3/A2 retained mutants as regression diagnostics; all must remain valid and killed, but do not merge them into the six-mutant A4 count.

## 11. A4 build, validation, hash and Git formulas

Required before the future A4 checkpoint commit:

1. HEAD must equal this reconciliation commit and target status must be clean.
2. One A4 commit only; exactly the eight allowlisted paths change; no unnamed file changes.
3. Read and hash every changed file before and after validation.
4. Warning-as-error builds: contracts, Control Plane, Acceptance Verifier, control tests, ERP tests and `SESS.NexaERP.slnx`; all exit `0`, warnings `0`, errors `0`.
5. Test discovery and execution must reproduce A4 `17`, retained A3 `16`, Phase-A `78`, focused ERP `81`, full ERP `455`, unique `533`, raw `647`.
6. Discover PostgreSQL tests only: `87`; execute `0`.
7. Parse all 24 PowerShell files with Windows PowerShell 5.1 AST: zero errors, no execution.
8. EF `migrations list --no-connect` against the inert endpoint: 13 migrations; REV869A/REV869B unique, adjacent at positions 12/13; connections `0`; applies attempted/completed `0/0`.
9. Model/snapshot parity and retained offline SQL tests pass.
10. Two genuinely fresh evidence workers return byte-identical canonical JSON and Up/Down SQL and bind all section-6 fields.
11. Security, secret, privacy, endpoint, Phase-B and prohibited-operation scans have zero unexplained hits.
12. `git diff --check` exits `0` for both the incremental A4 range and cumulative frozen range over the exact eight-file allowlist.
13. `changed_file_count=8`; `outside_allowlist=0`; `missing_allowlist=0`; `commit_count=1`.
14. The checkpoint committed blob and worktree SHA-256 match; all seven source/test files match their recorded SHA-256.
15. Disposable processes/files/mutants are removed and target status is clean after the one commit.

Any mismatch makes A4 FAIL and permits only a separate report-only A4 failure reconciliation. Passing A4 still requires a fresh independent report-only source architecture/security review; it does not authorize Phase B, PostgreSQL, provisioning, deployment or management acceptance.

## 12. Management boundary and retained states

No source, test, project, migration, helper or checkpoint was changed. No A4 implementation, Phase B, Correction 2, PostgreSQL access/test, migration execution, provisioning, deployment, production access, real credential/key use, lifecycle, recovery, purge or export operation occurred.

`phase_a_management_acceptance_state=FAIL`

`phase_b_state=NO_GO`

`correction_2_state=NO_GO`

`postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN`

`external_provisioning_state=NOT_STARTED`

`production_readiness_state=NOT_READY`

Exact single next management gate: authorize one bounded source-only `REV869B Option-A Phase-A Correction A4` implementation against the eight-file allowlist and this exact contract. Do not begin implementation without that authorization.
