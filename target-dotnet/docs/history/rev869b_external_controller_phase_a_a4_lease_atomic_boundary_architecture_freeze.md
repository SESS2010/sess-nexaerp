# REV869B Option-A Phase-A A4 lease and atomic-boundary architecture-freeze decision

Date: 2026-08-19

Decision type: report-only architecture freeze; no implementation authority

Starting HEAD: `21ab39c1e6e4658be892bfc06fc8a18b768c4d32`

Expected parent: `cb4e90852947f2c9fdada3ea60a3110660d5cec8`

Frozen architecture baseline: `51476760adcea9ed7babbc04d642e53e371c6941`

## 1. Architecture verdict

`PHASE_A_CORRECTION_A4_REVISED_SOURCE_ONLY_GATE=GO`

Option L1, **separate grant-bound lease acquisition**, is selected and frozen. It is safe only when `AcquireExecutionLease` is a high-level, grant-bound control-plane transition committed by the one durable provider. It is not a caller-created lease, public lease setter, or mutation facet separate from the composite provider.

The canonical controller lifecycle is owned only by the surviving control-plane database. The target ERP database owns the fenced business attempt, business rows, target-local history, target audit/outbox, and immutable terminal result. No transaction spans both databases. A controller interruption after target commit is resolved by reading the target's authoritative terminal result and terminalizing the control lifecycle without repeating the business mutation.

This decision resolves the A4 blocker. It does not approve A4 implementation automatically beyond the exact future boundary in section 16, and it does not approve Phase B, Correction 2, PostgreSQL, migration, provisioning, deployment, production, or operational execution.

## 2. Mandatory Stage-0 evidence

| Gate | Verified result | Status |
|---|---|---|
| HEAD | `21ab39c1e6e4658be892bfc06fc8a18b768c4d32` | PASS |
| Parent | `cb4e90852947f2c9fdada3ea60a3110660d5cec8` | PASS |
| Subject | `REV869B Phase-A Correction A4 architecture blocker` | PASS |
| Branch | `master` | PASS |
| HEAD content | Exactly `target-dotnet/outputs/rev869b_external_controller_phase_a_checkpoint.md` | PASS |
| Blocker checkpoint SHA-256 | `3368AE5FCF79A5EA78F17DBC9B7B69E061ECACB08D8350091722FCF8980E446A` | PASS |
| Target-scoped status | Clean at entry | PASS |
| Legacy boundary | `../legacy-reference/` remained untracked; its contents were not opened, read, enumerated, changed, or used | PASS |

The Phase-A architecture-freeze specification, A3 independent source-safety review, A3 failure reconciliation, A4 blocker checkpoint, and current authorization, grant, lease, lifecycle, fencing, idempotency, durable-provider, audit, evidence, verifier, and source-contract surfaces were read before this decision. The existing source confirms the blocker: `ControllerOperationV2` has no lease-acquisition operation; authorization requires no lease; execution requires an existing lease; and `IDurableControlPlanePersistenceProvider` exposes one snapshot read and one composite atomic mutation only.

## 3. Frozen terminology and non-duplication rule

- **Canonical controller lifecycle** means the single control-plane state for one grant-bound plan execution. It exists only in the control-plane database.
- **Target business workflow state** means normal ERP aggregate status written with business rows. It is not an alias or replica of the controller lifecycle.
- **Target execution result** means the immutable, idempotency-bound receipt of the target transaction. It is not a second controller lifecycle.
- **Fencing watermark** in the target is an enforcement value that rejects stale executors. It is not lease ownership or lifecycle authority.
- **Audit/evidence copy** is diagnostic and immutable but is not the sole truth for a business outcome.

No component may store or claim a second authoritative copy of the canonical controller lifecycle.

## 4. Authoritative-owner and storage table

| Fact | Sole authoritative owner | Authoritative storage | Write rule |
|---|---|---|---|
| Authorization grant | Durable control-plane provider | Control-plane DB | Created only by `Authorize`; versioned and immutable except state |
| Approved plan ID/version/hash | Management authorization writer, persisted by durable provider | Control-plane DB grant row | Exact values signed and immutable after authorization |
| Management authorizer identity | Management authorization system/writer | Control-plane DB grant row plus signed decision reference | Derived from authenticated workload/policy; never caller role text |
| Executor workload identity | Workload IAM/policy, bound by authorizer | Control-plane DB grant and lease rows | Exact SPIFFE/workload identity; substitution denies |
| Execution lease | Lease issuer through durable provider | Control-plane DB | Created/renewed/superseded only by high-level lease transactions |
| Fencing sequence | Durable control-plane provider | Control-plane DB monotonic sequence scoped to target/resource | Database-generated; caller values are comparison-only |
| Execution idempotency | Target execution provider | Target ERP DB | Unique by organization, target, operation, grant, plan and execution request digest |
| Business outcome | Target ERP transaction owner | Target ERP DB terminal-result row and business ledger | Written atomically with business mutation |
| Canonical lifecycle state/version | Lifecycle controller through durable provider | Control-plane DB | Changed only by listed high-level operations |
| Control audit receipt | Durable control-plane provider | Control-plane DB audit/outbox transaction | Required in the same control transaction |
| Target audit receipt | Target execution provider | Target ERP DB audit/outbox transaction | Required in the same target transaction |
| Outbox | Owner of the local transaction | Control-plane DB or target ERP DB, respectively | Local outbox commits atomically with its local facts |
| Reconciliation state/attempt | Recovery/reconciliation authority through durable provider | Control-plane DB | Reads target facts; may not execute business mutation |
| Immutable audit/evidence archive | Independent audit writer | WORM store | Receives local outbox events; never replaces source truth |

## 5. Selected operations and exact atomic boundaries

### 5.1 `Authorize`

One serializable control-plane transaction:

1. verifies authenticated management authorizer, signed policy, exact tenant/organization/target, operation, plan ID, plan version/hash, executor workload, evidence manifest, expiry, nonce and request digest;
2. reserves authorization idempotency and rejects a digest conflict;
3. inserts one authoritative grant with no lease and state `Authorized`;
4. stores canonical lifecycle version, authorizer identity, executor binding and response;
5. appends the control audit event and outbox row; and
6. commits all or rolls back all.

An exact replay returns the stored authorization response. A conflicting replay returns a durable conflict. Audit/outbox failure rolls back authorization.

### 5.2 `AcquireExecutionLease`

One serializable control-plane transaction under a row lock/advisory resource lock and uniqueness constraint:

1. authenticates the exact grant-bound executor workload;
2. verifies grant ID/version/state, authorization request, tenant/organization/target, operation, plan ID/version/hash, executor, policy/evidence bindings and expiry;
3. checks that no terminal target result is already known and that no other unexpired active lease exists;
4. reserves the grant by changing grant state `ACTIVE` to `RESERVED`;
5. allocates a strictly increasing database-generated fencing token;
6. creates the immutable lease receipt and changes lifecycle `Authorized` to `LeaseActive`;
7. stores acquisition idempotency response, lifecycle version, audit and outbox; and
8. commits all or rolls back all.

This is the only lease-creation authority. `IDurableControlPlanePersistenceProvider` remains non-decomposable: its one composite atomic method accepts the high-level operation request. No `SetLease`, `UpdateFence`, `RegisterNonce`, or independently injectable partial mutation interface is permitted.

### 5.3 `BeginExecuteAuthorizedPlan`

One serializable control-plane transaction validates the same grant, active lease, current fence, plan and executor; changes `LeaseActive` to `Executing`; records the execution dispatch/request digest and control outbox; and commits. The grant remains `RESERVED`, not consumed. Failure of its audit/outbox rolls back the transition and dispatch.

The outbox delivers one immutable execution job. Delivery may repeat; authority does not.

### 5.4 `ExecuteAuthorizedPlan` target transaction

One target-local ERP transaction:

1. authenticates the permitted executor/target runtime path and validates the signed job and target binding;
2. locks the target fence/idempotency row;
3. rejects a token lower than the target fencing watermark and rejects an expired/revoked authorization attestation;
4. reserves or reads the exact execution idempotency identity and digest;
5. applies the business mutation and normal ERP workflow/history writes;
6. advances the target fencing watermark as required;
7. writes one immutable terminal result containing request digest, grant/lease/plan/fence binding and business-result digest;
8. writes target audit and target outbox; and
9. commits all or rolls back all.

Target audit/outbox failure aborts the entire target transaction. A rollback leaves no business row, terminal result, target audit receipt, or outbox success. A repeated exact job returns the stored terminal result. A different digest under the same idempotency identity conflicts.

### 5.5 `ReconcileTerminalResult`

The reconciliation authority performs a read from the target followed by one serializable control-plane transaction; these are not one distributed transaction. The control transaction:

1. verifies an authoritative target result and its grant, lease, plan, request, fence, organization and target binding;
2. rejects missing, ambiguous, conflicting or stale evidence;
3. changes `Executing` to `Succeeded` or `Failed` from the target result;
4. marks the grant `CONSUMED` exactly once;
5. closes the lease, stores the original result digest/response reference and reconciliation outcome;
6. appends control audit/outbox; and
7. commits all or rolls back all.

An exact reconciliation replay returns the stored control outcome. It never calls the target business mutation. Audit/outbox failure rolls back terminalization and leaves the target result available for retry.

## 6. Cross-database consistency and recovery model

The design is a fenced, idempotent saga:

`control authorization commit -> control lease commit -> control execution-dispatch commit -> target execution commit -> control reconciliation commit`.

There is no distributed ACID claim. Each arrow can be retried from durable local facts:

- failure before a local commit leaves no part of that local operation;
- a committed control outbox makes dispatch retryable;
- a target idempotency row makes repeated delivery return the original outcome;
- a target terminal result survives loss of controller acknowledgement;
- reconciliation reads the result and terminalizes without executing business code;
- uncertainty never authorizes a new fence until the target is authoritatively checked;
- missing or contradictory facts quarantine the resource instead of guessing.

Control-plane database recovery restores grants, leases, fences, lifecycle, dispatch and reconciliation attempts. Target recovery restores business rows, fencing watermarks, idempotency and terminal results. After either restore, writers remain stopped until epoch/restore attestation and cross-store reconciliation establish that no old executor can commit. A restored control plane must never lower the target's accepted fence.

## 7. Canonical lifecycle and legal transitions

Canonical status names are exactly:

`Draft`, `Rejected`, `Authorized`, `LeaseActive`, `Executing`, `Succeeded`, `Failed`, `Expired`, `Revoked`, `Cancelled`, `Quarantined`.

There are no `GrantDraft`, `ExecutionAuthorized`, `Running`, `Completed`, `LeaseExpired`, or `ReconciliationPending` aliases. Lease and reconciliation conditions are substates/attempt records, not duplicate lifecycle states.

| Current | Operation/event | Next | Owner and rule |
|---|---|---|---|
| `Draft` | `Authorize` approved | `Authorized` | Management authorizer + durable provider; no lease |
| `Draft` | authorization rejected | `Rejected` | Durable denial decision and audit; terminal |
| `Draft` | draft cancelled | `Cancelled` | Authorized management cancellation |
| `Authorized` | authorization expires | `Expired` | Server time/policy transaction |
| `Authorized` | revoke before lease | `Revoked` | Management/security revocation transaction |
| `Authorized` | `AcquireExecutionLease` succeeds | `LeaseActive` | Lease issuer + durable provider |
| `Authorized` | lease denied/conflicting acquisition | `Authorized` | No state change; denial audit only |
| `LeaseActive` | exact acquisition replay | `LeaseActive` | No version/state change; original receipt |
| `LeaseActive` | valid renewal | `LeaseActive` | Version/expiry update; same fence |
| `LeaseActive` | lease cancelled before execution | `Cancelled` | Bound executor or management policy; closes lease |
| `LeaseActive` | lease expires before execution | `Authorized` | Marks lease expired; grant returns `RESERVED` to `ACTIVE` only after no-result proof |
| `LeaseActive` | `BeginExecuteAuthorizedPlan` | `Executing` | Lifecycle controller + durable provider |
| `LeaseActive` | nonretryable validation failure before business dispatch | `Failed` | Only with authoritative failure evidence; otherwise no transition |
| `Executing` | target transaction rollback proven | `Failed` | Reconciler; no partial target result |
| `Executing` | target commit acknowledged or discovered | `Succeeded` or `Failed` | Reconciler uses immutable target result |
| `Executing` | target commit but acknowledgement lost | `Executing` | No guessed transition; reconciliation attempt records interruption |
| `Executing` | exact execution replay | `Executing` or existing terminal | No new mutation; target result is returned if present |
| Any nonterminal | administrative quarantine | `Quarantined` | Separate recovery authority; reason/evidence required |
| `Quarantined` | recovery proves existing target result | `Succeeded` or `Failed` | Reconcile only; no business re-execution |
| `Quarantined` | recovery proves no result and grant remains valid | `Authorized` | Fresh management recovery decision; prior lease closed |
| Any terminal | exact replay/reconciliation replay | same terminal | Original durable result; no version change |
| Any state | conflicting replay/stale fence | same state | Denial audit; zero business mutation |

Interrupted reconciliation does not create a lifecycle alias. The lifecycle remains `Executing` or `Quarantined`; a separately versioned reconciliation-attempt record is retryable and auditable.

## 8. Grant reservation and consumption

Grant states are exactly `ACTIVE`, `RESERVED`, `CONSUMED`, `REVOKED`, `EXPIRED`, and `REJECTED`.

- `Authorize` creates `ACTIVE` and no lease.
- Successful lease acquisition changes `ACTIVE` to `RESERVED` and binds the lease ID/fence.
- Exact acquisition replay leaves the same `RESERVED` grant and returns the same receipt.
- Lease expiry before execution may return `RESERVED` to `ACTIVE` only after authoritative proof that no target terminal result exists and no execution remains capable of committing.
- `BeginExecuteAuthorizedPlan` retains `RESERVED`; this permits safe redelivery of the same exact job.
- Reconciliation of a target terminal result changes `RESERVED` to `CONSUMED` exactly once.
- Revocation/expiry while `RESERVED` prevents new dispatch or renewal, but cannot erase a target result; reconciliation may still consume and terminalize an already committed result.
- A grant is never consumed merely because a message was sent or an acknowledgement was received.

## 9. Lease, renewal, expiry, cancellation and reacquisition

1. Uniqueness enforces one active lease per grant, authorized operation and target.
2. Concurrent acquisition has one serializable winner. Losers return the winner only when their acquisition identity and digest are exact; otherwise conflict.
3. The lease receipt includes lease ID, grant ID/version, acquisition request ID/digest, tenant/organization, target, operation, plan ID/version/hash, executor workload identity, issue/expiry timestamps, controller epoch, fencing token and lifecycle version.
4. All authoritative values come from server policy and database state. Caller lease/fence values are comparisons only.
5. Renewal retains the same fencing token because the holder and execution authority do not change. It atomically extends expiry and increments lease/lifecycle record version. A changed executor, plan, operation, target or grant is not renewal.
6. Reacquisition after expiry creates a new lease ID and strictly greater fence. It requires the original grant to remain valid, the prior lease to be durably expired/superseded, and authoritative proof that no target terminal result exists.
7. If a prior target result exists, reacquisition is denied and reconciliation returns that result.
8. Cancellation closes an unused lease. It cannot cancel or hide a committed target result.
9. Lease expiry alone does not prove an executor did not commit. The target result/fence guard must be checked before reacquisition.

## 10. Fencing and stale-executor protection

- The control DB allocates monotonically increasing fences within the exact target/resource scope.
- The target transaction compares the signed incoming fence under lock with its durable fencing watermark.
- A token below the watermark always fails before business mutation.
- A token equal to the watermark is accepted only as an exact idempotent replay of the same grant, lease, plan and request digest.
- A greater token may advance the watermark only with a valid unexpired lease and exact grant-bound executor.
- Target commit verifies lease expiry/revocation evidence appropriate to the signed job; an expired or revoked executor cannot begin or commit new work.
- Reacquisition advances the fence before a replacement executor can act, permanently fencing the earlier executor.
- Controller restore/failover uses a monotonically increasing epoch and may not reuse or reduce a fence.

## 11. Interruption and reconciliation behaviour

| Interruption | Durable fact | Required response |
|---|---|---|
| Before authorization commit | None | Retry authorization normally |
| After authorization commit, response lost | Stored grant/response | Exact replay returns original grant |
| During lease transaction | Commit or rollback is authoritative | Exact replay reads one result |
| After lease commit, response lost | Lease receipt/outbox | Exact replay returns same lease |
| After dispatch commit, before target call | `Executing` + outbox | Redeliver same job |
| Target rollback | No business result | Reconcile failure evidence; never report success |
| Target commit, acknowledgement lost | Target terminal result | Reconcile and terminalize; never re-execute |
| Reconciliation control commit fails | Target result remains | Retry reconciliation |
| Reconciliation acknowledgement lost | Control terminal result | Exact replay returns original result |
| Contradictory/missing authoritative facts | Uncertain nonterminal state | Quarantine; require recovery decision |

## 12. Audit and outbox rules

- Every control-plane state-changing operation commits its event and control outbox in the same control transaction. Failure means no control state change.
- Target execution commits target audit, target outbox, business history and terminal result with business mutation. Failure means no target business commit.
- Denials that occur before a state transaction require a durable denial audit from the independent audit path. If the denial audit is unavailable, the request still fails closed and readiness becomes not-ready; no protected operation proceeds.
- WORM delivery may be asynchronous, but a committed local outbox is mandatory. WORM is not the sole source of lifecycle or business truth.
- Reconciliation binds both local transaction IDs, digests and receipts into the control terminal audit.

## 13. Version conflicts and concurrent requests

- Every grant, lease, lifecycle and reconciliation update uses expected version plus serializable isolation or an equivalent compare-and-swap constraint.
- Zero or multiple matching grants, leases, idempotency rows or terminal results fail closed.
- Same idempotency identity + same canonical digest returns the stored response.
- Same identity + different digest returns `IDEMPOTENCY_CONFLICT` and performs zero state/business mutation.
- Concurrent lease acquisition produces exactly one committed lease/fence.
- A serialization conflict is retried only within a bounded server-owned policy; exhaustion returns a durable conflict and never relaxes validation.
- Caller-supplied versions cannot advance authority; they are expected-value comparisons.

## 14. Separation-of-duties matrix

| Responsibility | Permitted authority | Forbidden authority | Identity overlap rule |
|---|---|---|---|
| Management authorizer | Approve exact plan/executor and issue signed decision | Lease, execution, target mutation | Must not overlap executor, lease issuer or recovery executor |
| Lifecycle controller | Evaluate legal canonical transitions | Self-authorize, direct target business mutation | May orchestrate lease operation but must not use executor/authorizer identity |
| Lease issuer | Invoke high-level lease transaction | Low-level lease/fence mutation, plan approval | Distinct production workload/DB role from authorizer and executor |
| Executor workload | Execute one signed fenced target job | Authorize, issue lease/fence, change control lifecycle | Must be the exact grant-bound identity; no authorizer overlap |
| Durable provider | Atomically persist control facts | Policy decision, target business SQL | One non-decomposable provider; no public partial facets |
| Target ERP runtime/provider | Fenced target transaction and exact replay | Control grant/lease/lifecycle writes | Operation-scoped identity; no control DB mutation role |
| Audit writer | Append immutable evidence | Create business outcome or lifecycle authority | Independent from executor and purge authority |
| Evidence reader | Read bounded authoritative facts | Submit verdict or mutate source | Distinct identity/key from verifier and callers |
| Acceptance verifier | Calculate/sign verdict from reader facts | Read source directly, lifecycle or target mutation | Distinct identity/key from controller/readers/executor |
| Recovery/reconciliation authority | Read control/target results and terminalize/recover under decision | Approve its own recovery or execute business mutation | Distinct from authorizer, executor and ordinary lifecycle runtime |

Co-location of code does not permit identity or database-role overlap where the matrix forbids it. Caller claims, role strings and supplied metadata are never trusted authority.

## 15. Unsafe options explicitly rejected

| Unsafe option | Decision and reason |
|---|---|
| Silently acquire lease inside `Execute` | REJECTED: lifecycle/business evaluation would precede an independently durable lease boundary |
| Caller-generated lease or fence | REJECTED: caller values cannot establish authority or monotonicity |
| Public low-level lease setter | REJECTED: violates F02 and permits partial state mutation |
| Execute before durable lease | REJECTED: unfenced/stale execution becomes possible |
| Terminalize before target commit | REJECTED: can report success for rolled-back business work |
| Re-execute after uncertain acknowledgement | REJECTED: target result must be discovered and replayed |
| Expired/stale executor commit | REJECTED: expiry, identity and target fence guard fail before mutation/commit |
| Audit as sole business truth | REJECTED: target business/result rows are authoritative |
| Cross-database ACID claim | REJECTED: the model is explicit local transactions plus reconciliation |
| Split lifecycle ownership | REJECTED: control-plane DB alone owns canonical lifecycle |

## 16. Smallest exhaustive revised A4 file allowlist

Exactly these eight files may change in one future A4 correction commit. Every unnamed file is forbidden.

| # | Allowed file | Exact required mapping |
|---:|---|---|
| 1 | `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs` | Add canonical L1 operations/statuses, plan/executor/grant/lease/acquisition/target-result/reconciliation contracts; add the high-level composite transaction request/results and target execution/reconciliation interfaces without any low-level lease setter. |
| 2 | `src/SESS.NexaERP.ControlPlane/Configuration/ControlPlaneOptions.cs` | Pin distinct authorizer, lease issuer, executor class, target execution provider and reconciliation descriptors/policy versions; validate forbidden identity overlap. |
| 3 | `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs` | Implement the exact lifecycle, grant reservation/consumption, renewal, expiry, cancellation, terminalization and replay rules frozen here. |
| 4 | `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs` | Preserve raw-only ingress; resolve trusted identities server-side; enforce complete preflight; construct only high-level authorize/acquire/begin/reconcile transactions; bind target jobs/results; never acquire inside execute. |
| 5 | `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs` | Complete F04 reader metadata/cardinality/readiness preflight before `ReadAsync`; preserve independent reader/oracle separation and zero-call denial traces. |
| 6 | `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs` | Add the exact L1 production-path tests and mutant-killing assertions; fixtures remain passive. |
| 7 | `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs` | Add exact source/tool/process evidence, executable hashes, observed no-operation counters and F07 arithmetic checks. |
| 8 | `outputs/rev869b_external_controller_phase_a_checkpoint.md` | Record the future A4 implementation, exact validation evidence, hashes, arithmetic, prohibited operations and independent-review gate. |

No `Program`, endpoint, project, solution, migration, model, snapshot, SQL, script, helper, `Rev869BExecutionBinding`, `AcceptanceVerifierOptions`, deployment or additional report file is permitted. The existing raw `IControlPlaneAuthority.AcceptRawCommandAsync` is the protected ingress; L1 is represented by typed internal contracts behind it. If implementation proves any ninth path necessary, A4 stops before that edit and only a new report-only boundary decision is allowed.

## 17. Exact future A4 source corrections

1. Add high-level `ACQUIRE_EXECUTION_LEASE`, `BEGIN_EXECUTE_AUTHORIZED_PLAN`, and `RECONCILE_TERMINAL_RESULT` operation contracts; do not add a partial mutation operation.
2. Replace lease-bearing authorization grants with no-lease plan/executor-bound grants.
3. Add immutable acquisition request/receipt, target execution job/result and reconciliation bindings with exact digests and versions.
4. Extend the one durable composite transaction to discriminate authorize, acquire, begin and reconcile atomically.
5. Freeze exact lifecycle and grant-state tables from sections 7 and 8.
6. Enforce server-owned authorizer/executor/lease-issuer/target/reconciler descriptors and forbidden overlaps.
7. Validate plan, executor, lease and fence before lifecycle/target calls.
8. Preserve F01 raw ingress, F02 non-decomposable provider and F05 readiness/audit/privacy rules.
9. Move all reader metadata mismatch decisions before `ReadAsync` and oracle invocation.
10. Correct F07 arithmetic and capture executable path/version/hash plus observed connection/application counters.

## 18. Exact future test design

The future A4 subset contains exactly 23 literal `[Fact]` methods, one per row below; parameter loops may expand matrices but do not inflate the unique count.

1. `A4_AuthorizationCreatesExactPlanExecutorGrantAndNoLease`
2. `A4_OnlyGrantBoundExecutorCanAcquireExecutionLease`
3. `A4_PlanIdVersionHashSubstitutionFailsBeforeLeaseOrLifecycle`
4. `A4_AuthorizerExecutorOrLeaseIssuerSubstitutionFailsClosed`
5. `A4_ExactLeaseAcquisitionReplayReturnsOriginalReceipt`
6. `A4_ConflictingLeaseAcquisitionReplayIsDenied`
7. `A4_ConcurrentLeaseAcquisitionHasExactlyOneWinner`
8. `A4_ExecutionBeforeAuthoritativeLeaseIsDenied`
9. `A4_StaleOrExpiredLeaseIsDeniedBeforeTargetMutation`
10. `A4_StaleFenceCannotCommitAtTarget`
11. `A4_RenewalRetainsFenceAndExtendsOnlySameBinding`
12. `A4_ReacquisitionRequiresNoResultProofAndGreaterFence`
13. `A4_TargetRollbackLeavesNoPartialBusinessResultAuditOrOutbox`
14. `A4_CommittedTargetResultSurvivesLostAcknowledgement`
15. `A4_ReconciliationNeverReexecutesBusinessMutation`
16. `A4_ExactExecutionReplayReturnsOriginalTargetAndControlResult`
17. `A4_ConflictingExecutionReplayIsDenied`
18. `A4_AuditOrOutboxFailureRollsBackItsLocalOperation`
19. `A4_ReaderMismatchIsRejectedBeforeReadAsync`
20. `A4_PreflightMismatchHasZeroReaderOracleLifecycleAndAtomicCalls`
21. `A4_F01RawIngressAndCanonicalTrustRegressionIsPreserved`
22. `A4_F02CompositeProviderHasNoPartialMutationCapability`
23. `A4_F05ReadinessAuditPrivacyAndEvidenceRegressionIsPreserved`

Every negative preflight case asserts exact denial code and zero forbidden calls. Concurrency uses two independently scheduled calls against one production contract boundary. Lost-acknowledgement and rollback fixtures are passive fault injectors; production code alone owns decisions.

## 19. Required real production mutants

Each mutant changes production code in an isolated disposable copy, must compile, must be killed by its named assertion, and must be deleted. Test-only mutations, noncompiling mutations and source-string-only changes are invalid.

| ID | Real production mutation | Required killer |
|---|---|---|
| A4-M01-AUTH-CREATES-LEASE | Populate a lease/fence during authorization | Test 1 |
| A4-M02-EXECUTOR-SUBSTITUTION | Accept caller or authorizer identity as executor | Tests 2 and 4 |
| A4-M03-PLAN-VERSION-BYPASS | Omit plan version/hash from grant/acquisition comparison | Test 3 |
| A4-M04-PARTIAL-LEASE-SETTER | Export/inject a low-level lease/fence mutation capability | Test 22 |
| A4-M05-LEASE-ORDER-BYPASS | Allow begin/target execution before committed lease validation | Test 8 |
| A4-M06-STALE-FENCE-BYPASS | Remove/lower target fence comparison | Test 10 |
| A4-M07-RECONCILIATION-REEXECUTES | Call target mutation when result lookup succeeds/ack is uncertain | Tests 14 and 15 |
| A4-M08-IDEMPOTENCY-DIGEST-BYPASS | Treat conflicting acquisition/execution digest as exact replay | Tests 6 and 17 |
| A4-M09-AUDIT-OUTBOX-NONATOMIC | Permit state/business commit when local audit/outbox append fails | Tests 13 and 18 |
| A4-M10-READER-PREFLIGHT-ORDER | Call `ReadAsync` before full metadata/cardinality match | Tests 19 and 20 |

Required result: `10 compiled; 10 killed by intended assertions; 0 survived; 0 invalid`.

## 20. Validation, arithmetic, hashes, Git and stop formulas

Future A4 must run only offline/no-connect validation:

1. warning-as-error build of affected projects and full solution: `0 warnings; 0 errors`;
2. A4 literal subset: exactly `23 passed; 0 failed; 0 skipped`;
3. complete Phase-A non-PostgreSQL assembly: all discovered tests pass;
4. focused REV869B non-PostgreSQL subset: all discovered tests pass;
5. complete ERP non-PostgreSQL assembly: all discovered tests pass;
6. canonical SQL/source-evidence subset: all exact tests pass in two fresh processes;
7. PostgreSQL tests: discovery only; executed count exactly `0`;
8. EF migrations list uses `--no-connect`; connections `0`, migration applies attempted `0`, completed `0`;
9. model/snapshot/source parity passes; no source/model/migration change is permitted;
10. all ten valid production mutants compile and are killed;
11. PowerShell files are parsed by AST only and no script is executed;
12. boundary scan reports exactly eight changed files and zero unnamed files;
13. secret, conflict-marker and prohibited-operation scans return zero findings; and
14. all disposable mutant/evidence artifacts are removed.

Arithmetic is machine-derived, not copied:

- `A4_unique = 23` literal A4 methods.
- `PhaseA_unique = discovered unique non-PostgreSQL tests in the control assembly`.
- `ERP_unique = discovered unique non-PostgreSQL tests in the ERP assembly`.
- `Combined_unique = PhaseA_unique + ERP_unique` because the assemblies are disjoint.
- `Raw_pass_events = sum of every explicitly listed invocation`, including diagnostic reruns; no overlapping invocation is silently omitted.
- `PostgreSQL_discovered = unique listed PostgreSQL tests`; `PostgreSQL_executed = 0`.

The future checkpoint must record for every executable/tool used: resolved absolute path, semantic/file version, byte length and uppercase SHA-256. It must also record SDK/runtime/package versions, OS/culture, exact commands, input source hashes, output byte/LF/hash values, newline/encoding rules and observed connection/application counters. A literal zero is not observation.

Git formulas:

- entry HEAD must be this report-only decision commit and target status clean;
- `git diff --name-only <entry>..HEAD` equals the eight-file allowlist exactly and no other path;
- `git diff --check <entry>..HEAD -- <allowlist>` exits `0`;
- cumulative `git diff --check 51476760adcea9ed7babbc04d642e53e371c6941..HEAD -- <allowlist>` exits `0`;
- future A4 creates exactly one commit and does not amend, reset, rebase or rewrite history;
- final target-scoped status is clean.

Any count, hash, identity, path, test, mutant, boundary, no-connect counter, or Git mismatch makes A4 `FAIL`. It permits only one separate report-only failure reconciliation. Passing A4 still requires a fresh independent report-only source architecture/security review before management acceptance or later phases.

## 21. Prohibited operations and retained states

This report performed no A4 implementation, Phase B, Correction 2, PostgreSQL connection/test, migration operation, provisioning, deployment, production access, real credential/key use, lifecycle/lease/recovery/purge/export execution, or legacy-reference access. No existing source, test, project, migration, helper or checkpoint file was modified.

`phase_a_management_acceptance_state=FAIL`

`phase_b_state=NO_GO`

`correction_2_state=NO_GO`

`postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN`

`external_provisioning_state=NOT_STARTED`

`production_readiness_state=NOT_READY`

## 22. Exact single next management gate

Approve or reject one future report-bound `REV869B Option-A Phase-A Correction A4` source-only implementation using exactly the eight-file allowlist, exact L1 decisions, 23-test contract, ten production mutants and validation/stop formulas above. If approved, require exactly one correction commit followed by a fresh independent report-only source architecture/security review. No PostgreSQL, Phase B, Correction 2, provisioning, deployment or production activity is included.
