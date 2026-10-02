# REV869B Correction 24 allowlist/evidence-interface reconciliation

Date: 2026-08-15 (Asia/Calcutta)
Authorization: management-authorized source-only, report-only reconciliation
Disposition: **PASS — a corrected bounded Correction 24 source-only implementation is GO; REV869B remains FAIL and PostgreSQL remains unauthorized.**

## 1. Entry gate and execution boundary

| Gate | Result |
|---|---|
| Authorized starting HEAD | PASS — `90d08f4e0bda9d85e40dbf016a2a265f4d194694` |
| HEAD parent | `d36fb4d9e210895359979048ed1ed0f84229debd` |
| Branch | `master` |
| Target-scoped status on entry | PASS — clean |
| Blocker checkpoint | `outputs/rev869b_source_correction_checkpoint_24.md` |
| Blocker checkpoint SHA-256 | PASS — `6A17FE012D42C4740844B05DB04944B46A02C5EE3FC63EE77B64BF8476C28930` |
| Correction 23 reconciliation | Read completely — 205/205 lines |
| Correction 24 blocker checkpoint | Read completely — 82/82 lines |
| Relevant committed source/test contracts | Read completely for the files and symbols identified below |
| Source/test/helper/SQL/migration changes | `0` |
| PostgreSQL connections/tests | `0` |
| Provisioning/migration/lifecycle/purge/recovery/quarantine/export/production execution | `0` |
| `../legacy-reference/` | Not read, listed, searched, modified, staged or committed |

The entry gate matched. This report is the only authorized filesystem change. No prior commit or report was rewritten.

## 2. Reconciled contradiction

The Correction 23 reconciliation simultaneously required all 34 acceptance decisions to be based on independently executed verifier/audit reads and excluded the only production SQL source that defines the target-local command, purge, export and target ACL read surface. The Correction 24 blocker correctly stopped rather than either granting direct table access or treating signed controller echoes as observations.

The contradiction is real and bounded:

1. `nexa_rev869b_target_verifier` cannot obtain the keyed command business/history, purge chain/candidate/event, export authorization/batch/release, or structured ACL evidence required by C01-C08, G01-G06, E01-E04 and A01-A02.
2. `nexa_rev869b_control_plane_verifier` cannot obtain keyed immutable lease events, lifecycle attempts/outcomes, recovery decisions or quarantine evidence required by P01-P03, L01-L05, R01-R03 and T01-T02.
3. Direct ledger `SELECT`, an export-service credential, or a lifecycle-administrator credential would violate the retained least-privilege architecture.
4. The existing single signed acceptance response is action-controller evidence. It may prove authenticity and correlation, but it cannot independently prove the facts it asserts.
5. Adding evidence functions changes two exact committed function inventories that the previous eight-file allowlist also excluded.

F23-01 remains a separate source defect: in `nexa.rev869b_begin_drop`, the normal-drop branch accepts caller-supplied `registration_request_id` without resolving it to the exact immediately preceding immutable `DropAuthorized` event for the same lease, target instance, authorization identity and predecessor/current lease version. The corrected allowlist still contains every file needed for its bounded repair and positive/negative contracts.

## 3. Requirements impossible under the previous eight-file boundary

| Requirement | Exact required file and symbol | Why modification is unavoidable | Why no existing authorized interface suffices | Security/ACL impact |
|---|---|---|---|---|
| Keyed target command evidence for C01-C08 | `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs`: `Install`, `Remove`, `rev869b_reconcile_command_attempt`, new `rev869b_read_command_evidence` | Only this helper creates/drops target-local ledger functions and emits their ownership, revokes and grants into REV869B Up/Down SQL. | Existing reconcile output contains only attempt ordinal/active/terminal/receipt/response and does not expose request/context/claims, independently recomputed business/history deltas or immutable evidence bindings. Direct table reads are revoked. | Add a narrowly keyed, read-only `SECURITY DEFINER STABLE` function owned by `nexa_rev869b_security_owner`; revoke from `PUBLIC` and every non-verifier role; grant EXECUTE only to `nexa_rev869b_target_verifier`. Return IDs, counts and canonical hashes, not unrestricted business payload. |
| Keyed purge root/child/candidate/event evidence for G01-G06 | Same file: `Install`, `Remove`, `rev869b_reconcile_purge`, new `rev869b_read_purge_evidence` | Authorizations, attempts, candidates and events are target-local objects defined only here. | `rev869b_reconcile_purge(uuid)` returns only the attempt row; it cannot prove the authorization root, prior attempt/outcome/evidence, candidate set/hash or terminal event. | Same definer/search-path/caller restrictions; exact authorization and attempt keys; no mutation or table grant. |
| Keyed minimized export evidence for E01-E04 | Same file: `Install`, `Remove`, new `rev869b_read_export_evidence` | Export authorization, batch rows and releases are created only here. A verifier-safe projection requires production SQL and a precise grant. | `rev869b_read_prepared_export_batch(uuid,uuid)` accepts only `nexa_rev869b_export_service`. Sharing that credential violates the verifier-only boundary, while its existing projection does not expose the authorization/release chain. | Do not broaden the export-service reader. Add a verifier-only projection of IDs, allowed field-key inventory, counts, stored and independently recomputed hashes, timestamps and release linkage; do not return unrestricted payload values. |
| Structured target ACL and isolation evidence for A01-A02 | Same file: `rev869b_verify_target_catalogue_acl`, `rev869b_target_catalogue_fingerprint`, new `rev869b_read_target_acl_evidence`, exact expected EXECUTE matrix | A01/A02 require the exact observed/expected delta and protected-object identity, not a generic exception or label. Adding functions also changes the exact function/EXECUTE universe. | The current verifier returns `text` or throws; it does not return a canonical per-principal/object/privilege delta. | Preserve all owner, database, schema, relation, sequence, function, default privilege, membership, capability and `PUBLIC` closure. New functions themselves must be included in the exact ACL universe and granted only to the target verifier. |
| Keyed control-plane lifecycle/recovery/quarantine evidence for P/L/R/T | `tools/rev869b-control-plane-install.sql`: new `rev869b_read_lifecycle_evidence` and `rev869b_read_control_plane_acl_evidence`; ownership/revokes/grants/catalogue hash | The prior purpose restriction allowed this file only for F23-01. Its purpose must be broadened narrowly because the authoritative surviving ledgers live here. | `rev869b_read_lease` returns a lease row only; `rev869b_read_nonterminal_leases` is not keyed evidence for events, attempts, outcomes, decisions or quarantine. | Owner remains `nexa_rev869b_control_plane_owner`; fixed `pg_catalog,nexa` search path; exact UUID keys; EXECUTE only for `nexa_rev869b_control_plane_verifier`; no direct relation privilege and no lifecycle mutation capability. |
| Exact control-plane object/function/EXECUTE closure and stable P03 evidence | `tools/rev869b-control-plane-verify.sql`: `expected_functions`, `expected_exec`, `function_delta`, `exec_delta`, final structured denial | Any added evidence function must appear in the exact verifier inventory. P03 also must report the seeded delta rather than `22012`/`pg_catalog.int4div(integer,integer)`. | Leaving the verifier unchanged makes a correct installation fail its exact-set check and preserves the prohibited sentinel. | No grant weakening. The verifier must emit a stable domain/code and the exact drifted object/grant while remaining read-only. |
| Exact offline control-plane API inventory | `tests/SESS.NexaERP.Tests/Rev869BControlPlaneProvisioningContract.cs`: `PurposeSpecificApis` | This committed array pins the authoritative control-plane API set. New evidence readers make it stale. | No allowed SQL or scenario file updates this independent C# inventory. | The added entries are read-only verifier APIs; no role, provisioning mode or mutation API is added. |
| Exact target PostgreSQL function inventory | `tests/SESS.NexaERP.Tests/Rev869BPostgresBehaviorTests.cs`: the `expectedFunctions` array in the exact trigger/function inventory test | The test asserts equality with every `rev869b_%` target function. New evidence readers otherwise fail discovery/behavior acceptance. | The array is local to this excluded test file and cannot be extended elsewhere. | This is a test-only inventory update; it must assert the new functions exist exactly once and must not authorize execution. PostgreSQL execution remains a later gate. |
| Immutable implementation checkpoint | `outputs/rev869b_source_correction_checkpoint_24_implementation.md` (new) | The committed `outputs/rev869b_source_correction_checkpoint_24.md` is the authoritative blocker record and must remain unchanged. A future implementation requires its own auditable checkpoint. | Overwriting the blocker path would obscure the current decision in the working tree and weaken report provenance. | No runtime or ACL impact. This is the required implementation audit artifact, not a convenience file. |

The migration class `20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.cs` already calls `Rev869BCommandContextSql.Install` and `.Remove`. Therefore it, its designer, the model snapshot and migration identities need no modification. Application/domain/API source also needs no modification.

## 4. Corrected exhaustive Correction 24 allowlist

The smallest corrected boundary is exactly these eleven files:

1. `tools/rev869b-control-plane-install.sql`
2. `tools/rev869b-control-plane-verify.sql`
3. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs`
4. `tests/SESS.NexaERP.Tests/Rev869BControlPlaneProvisioningContract.cs`
5. `tests/SESS.NexaERP.Tests/Rev869BPostgresBehaviorTests.cs`
6. `tests/SESS.NexaERP.Tests/Rev869BCorrection16SourceContractTests.cs`
7. `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs`
8. `tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs`
9. `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs`
10. `tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs`
11. `outputs/rev869b_source_correction_checkpoint_24_implementation.md` (new)

Files 1-3 are the mandatory source changes. Files 4-10 are the mandatory test/contract changes. File 11 is the mandatory immutable checkpoint. No twelfth file, migration identity/designer/snapshot file, application/domain/API file, provisioning helper, controller deployment source, or prior report is authorized.

## 5. Authoritative verifier/audit interface

### 5.1 Trust separation and protocol

The future client must use a staged protocol, not one acceptance-response call:

1. **Prepare:** allocate an isolated target and receive only signed pins, correlation IDs and narrowly scoped connection descriptors.
2. **Observe-before:** use control-plane/target verifier connections to execute the plan's exact fixture and before reads. Canonicalize ordered typed rows locally and compute counts/hashes locally.
3. **Act:** invoke the exact typed scenario adapter. The signed action response supplies action correlation, reached-boundary and error evidence only; it is not the verdict.
4. **Observe-after/durable:** reopen independent verifier/audit connections and execute the exact after, durable, ACL and isolation reads. Recompute all hashes and formulas locally.
5. **Cleanup and observe-cleanup:** request cleanup, then independently prove exact absence or the authorized retained quarantine state and verify durable cleanup evidence.
6. **Adjudicate:** apply the immutable per-scenario formula. Any missing read, privilege error, signature disagreement, wrong ID, noncanonical row, unexpected mutation or missing cleanup is FAIL.

Controller-only physical facts that PostgreSQL cannot itself prove—process instance, operation invocation count and deterministic barrier/restart evidence—must come from a separately signed, append-only controller audit interface with a distinct read credential and trust path from the action endpoint. Its deployment is an external prerequisite. The test contract for that interface belongs in the allowlisted lifecycle client; no controller implementation is present or authorized in this repository.

### 5.2 Database readers

The following names/signatures are the authoritative minimum contract for Correction 24; an implementation may not replace them with one generic scenario-label reader:

- Control plane: `nexa.rev869b_read_lifecycle_evidence(lease_id uuid, attempt_id uuid, request_id uuid, decision_id uuid)` and `nexa.rev869b_read_control_plane_acl_evidence()`.
- Target: `nexa.rev869b_read_command_evidence(command_id uuid, attempt_id uuid)`, `nexa.rev869b_read_purge_evidence(authorization_id uuid, purge_attempt_id uuid)`, `nexa.rev869b_read_export_evidence(authorization_id uuid, batch_id uuid, release_id uuid)` and `nexa.rev869b_read_target_acl_evidence()`.

Every reader must be `SECURITY DEFINER STABLE`, have a fixed `search_path=pg_catalog,nexa`, reject the wrong `session_user`, require nonzero exact keys where keyed, bind the current database/target/lease identity, use no dynamic SQL, perform no mutation and return a minimal canonical projection. All EXECUTE privileges are revoked from `PUBLIC` and all non-verifier roles, then granted only to the corresponding verifier. No raw ledger table privilege, lifecycle administrator credential or export-service credential enters the test process. The exact owner/function/default/role/`PUBLIC` inventories and catalogue hashes must include the new functions.

Command readers return request/attempt/context/claim/receipt/outcome identity plus counts and canonical hashes of claimed business/history rows. Purge readers return root/child authorization, policy, candidate IDs/hash, attempt and terminal events. Export readers return authorization/batch/release linkage, allowed field-key inventory, counts, stored hashes and hashes independently recomputed inside the read path, without exposing unrelated values. ACL readers return exact expected-minus-observed and observed-minus-expected tuples, owner and role-capability deltas, not a constant `PASS` or generic exception.

## 6. Objective acceptance formula and prohibited evidence

For every scenario `s`:

`Accept_s = Pins_s ∧ Identity_s ∧ Fixture_s ∧ Before_s ∧ Action_s ∧ After_s ∧ Durable_s ∧ Error_s ∧ Terminal_s ∧ Isolation_s ∧ Cleanup_s ∧ Mutation_s`.

Counts are cardinalities of independently returned canonical row sets. Hashes are independently recomputed over ordered typed values. A denial requires the exact error domain/code or SQLSTATE and exact constraint/object identity, plus zero prohibited mutation. Cleanup must have an independently durable identifier and exact absence/quarantine proof. Every non-equivalent action, before/after/durable query, decisive assertion, denial identity and cleanup mutant must be killed.

The implementation must statically and dynamically reject shared acceptance signatures as verdicts, copied `1/1` counts, P02/P03 `22012` or `pg_catalog.int4div(integer,integer)` sentinels, echoed evidence-query labels, controller-returned expectations treated as observations, constant/self-asserted PASS values, generic exception-only acceptance, zero-row labels without an eligibility query, label-only terminal states, shared evidence records, missing fixtures, unexecuted query strings and scenario compression.

## 7. Complete 34-scenario evidence mapping

File keys: `CP-I` = control-plane install SQL; `CP-V` = control-plane verifier SQL; `TSQL` = target command-context SQL; `PCI` = provisioning-contract inventory; `PGB` = exact PostgreSQL behavior inventory; `D` = Correction 14 design inventory; `S` = Correction 17 scenario bodies; `SC16`/`SC17` = source contracts; `LC` = lifecycle client. “Independent actual” always means a fresh read after/before action as stated, never a value copied from the action response.

| ID | Independent fixture source | Action and exact expected result | Independently observed actual and row/state/audit formula | Required mutation | Required files |
|---|---|---|---|---|---|
| P01 | External pins plus CP/target ACL readers and catalogue fingerprints | Run canonical verifier; exact installation, no error. | `PinMismatchCount=0 ∧ ControlFingerprint=expected ∧ TargetAclDelta=∅ ∧ VerifyResult=Exact`; local hashes over full owner/default/`PUBLIC` inventories. | Corrupt either fingerprint, omit one ACL tuple or accept nonempty delta; each must fail. | CP-I, CP-V, TSQL, PCI, PGB, D, S, SC16, SC17, LC |
| P02 | Separately supplied source/manifest/TLS/cluster/signing pins; CP verifier proves lease absence. | Run external preflight for each mutated pin; code `REV869B_PREFLIGHT_PIN_MISMATCH`, object = mutated pin. | Per subcase: `PinMismatchCount=1 ∧ AllocatedLeaseCount=0 ∧ ActionCount=0`; stable controller problem code/object from action plus independent absence read. | Remove each pin comparison, absence read or exact code/object assertion; each mutant fails. | CP-I, PCI, D, S, SC16, SC17, LC |
| P03 | One seeded role/database/object/grant delta plus baseline CP inventory. | Run canonical verifier; denied with stable verifier code and exact seeded object/grant, never `int4div`. | `SeededDeltaCount=1 ∧ ReportedDelta=SeededDelta ∧ ProtectedMutationCount=0 ∧ CleanupFingerprint=baseline`. | Hide/change each delta or accept generic denial/sentinel; fail. | CP-I, CP-V, PCI, D, S, SC16, SC17, LC |
| L01 | CP reader: reserved lease/event/attempt; external target/role inventory. | Resume same attempt or separately authorized cleanup. | `ReservedEvents=1 ∧ (ResumeSameAttempt XOR AuthorizedCleanup) ∧ DuplicateAttempts=0`; Ready has one target/role set, cleanup has none. | Swap attempt, skip XOR branch, duplicate event, or omit absence proof; fail. | CP-I, PCI, D, S, SC16, SC17, LC |
| L02 | Per-boundary CP attempt/event snapshot plus target/role inventory. | Restart reconciliation at every create boundary; terminal Ready. | For each `b`: `StartedAttempts_b=1 ∧ ReconciledAttempts_b=1 ∧ LeaseState_b=Ready ∧ TargetCount_b=1 ∧ RoleSetCount_b=1`. | Remove any boundary/action/restart or force a count/state; fail for that boundary. | CP-I, PCI, D, S, SC16, SC17, LC |
| L03 | Ready and InUse leases, immutable `DropAuthorized` event, two cleanup requests and controller barrier audit. | Race cleanup; loser `(40001,UX_rev869b_one_active_lifecycle_attempt)`; one DropStarted/drop. | `CleanupRequests=2 ∧ DropStartedEvents=1 ∧ ActiveDropAttempts=1 ∧ PhysicalDropExecutions=1`; registration request = exact preceding authorization event and transition request distinct. | Substitute/miss/stale/replay/cross-lease/cross-version registration; delete race or one-drop assertion; fail. | CP-I, CP-V, PCI, D, S, SC16, SC17, LC |
| L04 | DropStarted lease at before/during/after DROP and role-cleanup barriers. | Restart/reconcile each boundary; exactly one Finalized. | Per boundary: `DropStartedEvents=1 ∧ FinalizedEvents=1 ∧ PhysicalDropExecutions≤1 ∧ TargetCount=0 ∧ RoleCount=0`. | Omit any boundary, permit second physical action, or omit role/target absence; fail. | CP-I, PCI, D, S, SC16, SC17, LC |
| L05 | Ready target with independently inventoried marker/catalogue mismatch and exact quarantine attempt. | Use/drop denied `(42501,rev869b_target_identity_mismatch)` then quarantine. | `UseMutations=0 ∧ DropMutations=0 ∧ QuarantineOutcomeCount_exactAttempt=1 ∧ LeaseState=Quarantined`; recompute all identity/evidence hashes. | Accept wrong instance/attempt/evidence/version or label-only quarantine; fail. | CP-I, CP-V, PCI, D, S, SC16, SC17, LC |
| R01 | Quarantined lease, unconsumed decision and target inventory from CP reader. | Consume exact action and recover; Finalized. | `DecisionCount=1 ∧ ConsumedAttemptId=AttemptId ∧ AuthorizedAction=PerformedAction ∧ RecoveryAttempts=1 ∧ FinalizedEvents=1 ∧ TargetAndRolesAbsent`. | Change decision/action/attempt or omit absence; fail. | CP-I, PCI, D, S, SC16, SC17, LC |
| R02 | Consumed recovery decision and baseline lease/event counts. | Replay same/changed action; `(42501,rev869b_recovery_decision_replay)`. | `NewAttempts=0 ∧ NewEvents=0 ∧ DecisionConsumedOnce ∧ LeaseState=RecoveryAuthorized`, for wrong/expired/replayed/foreign/pre-state/action/nonce plus valid-preserved subcases. | Drop any subcase or exact zero-delta/error check; fail. | CP-I, PCI, D, S, SC16, SC17, LC |
| R03 | CleanupFailed event/outcome and fresh linked decision. | Reject old decision, consume fresh decision, recover; Finalized. | `CleanupFailureCount=1 ∧ OldDecisionAccepted=0 ∧ FreshLinkedDecisionCount=1 ∧ FreshDecisionConsumedOnce ∧ FinalizedEvents=1`, exact lease/instance/operation/action binding. | Reuse old/foreign decision or sever any link; fail. | CP-I, PCI, D, S, SC16, SC17, LC |
| C01 | Target command reader over exact registered request/attempt/context/claims and claimed business/history hashes. | Commit protected rows, histories, receipt and outcome. | `ΔBusiness=expected ∧ ΔHistory=expected ∧ Receipts_attempt=1 ∧ CommittedOutcomes_attempt=1 ∧ ActiveAttempts=0`; all IDs/hashes bind. | Corrupt business/history/receipt/outcome query or count; fail. | TSQL, PGB, D, S, SC17, LC |
| C02 | Committed command receipt plus first-run business/history fingerprints. | Replay same request after lost response. | `Business2=Business1 ∧ History2=History1 ∧ ReceiptId2=ReceiptId1 ∧ ResponseHash2=ResponseHash1 ∧ Receipts=1 ∧ Outcomes=1`. | Accept a second mutation/receipt or changed response; fail. | TSQL, PGB, D, S, SC17, LC |
| C03 | Existing key/request digest and before request/attempt/business hashes. | Replay changed digest; `(23505,rev869b_command_request_replay_mismatch)`. | `ChangedDigest≠RegisteredDigest ∧ ΔRequests=0 ∧ ΔAttempts=0 ∧ ΔBusinessHistory=0`. | Equalize digests, accept generic unique error or skip zero-delta proof; fail. | TSQL, PGB, D, S, SC17, LC |
| C04 | Exact test-only receipt failpoint plus before business/history/receipt fingerprints. | Attempt commit; `(P0001,TR_rev869b_command_receipt_failpoint)`. | `ΔBusiness=0 ∧ ΔHistory=0 ∧ ΔReceipts=0 ∧ RolledBackOutcome_attempt=1`; outcome observed after rollback from audit path. | Disable failpoint, accept transaction-local evidence or omit any rollback delta; fail. | TSQL, PGB, D, S, SC17, LC |
| C05 | Open exact command transaction with durable attempt identity. | Roll back and record terminal outcome. | `OpenedExactAttempt ∧ TransactionRollback ∧ ΔBusinessHistoryReceipts=0 ∧ RolledBackOutcome_attempt=1`; exact attempt/instance/service/ownership tuple. | Change a binding, use transaction-local context or remove rollback check; fail. | TSQL, PGB, D, S, SC17, LC |
| C06 | Four distinct attempts interrupted before-open/after-open/during-commit/after-response. | Restart authoritative reconciler. | Distinct evidence IDs; exact outcomes `Abandoned, Abandoned, RolledBack, Committed-or-authoritative-receipt-replay`; exactly one terminal row each. | Compress subcases, share evidence ID or swap any outcome; fail. | TSQL, PGB, D, S, SC17, LC |
| C07 | One request, exact concurrent-start barrier and unrelated sentinel fingerprint. | Start two differently bound attempts; loser `(40001,rev869b_command_attempt_active)`. | `StartRequests=2 ∧ StartedAttempts=1 ∧ ActiveAttempts=1 ∧ UnrelatedMutationCount=0`. | Serialize race, accept two attempts or omit isolation; fail. | TSQL, PGB, D, S, SC17, LC |
| C08 | Exact attempt plus independently generated backend/actor/org/version/role/operation substitutions. | Open/terminalize substituted binding; `(42501,rev869b_attempt_binding)`. | Per substitution: `Accepted=0 ∧ ΔContexts=0 ∧ ΔReceipts=0 ∧ ΔBusinessHistory=0`, distinct evidence. | Remove any substitution or one zero-delta/error-object assertion; fail. | TSQL, PGB, D, S, SC17, LC |
| G01 | Missing/expired/wrong-target/wrong-batch/wrong-org authorization fixtures plus before ledger counts. | Start purge; `(42501,rev869b_purge_batch_binding)`. | Per subcase: `StartedAttempts=0 ∧ Candidates=0 ∧ PurgeEvents=0`. | Compress subcases, accept generic denial or omit zero-ledger proof; fail. | TSQL, PGB, D, S, SC17, LC |
| G02 | Independent scoped eligibility query proves zero rows; exact fresh authorization. | Freeze candidate batch; terminal ZeroRows. | `EligibleBefore=0 ∧ FrozenCandidates=0 ∧ DeletedRows=0 ∧ ZeroRowsEvents_attempt=1`. | Replace eligibility query with label/count default or drop event proof; fail. | TSQL, PGB, D, S, SC17, LC |
| G03 | Scoped eligible context IDs and unrelated row fingerprint from target reader. | Freeze/delete exact candidates; Succeeded. | `N=EligibleBefore>0 ∧ Frozen=N ∧ CandidateHash=Hash(EligibleIds) ∧ Deleted=N ∧ Remaining=0 ∧ SucceededEvents=1 ∧ UnrelatedAfter=Before`. | Alter candidate set/hash/deleted count or isolation comparison; fail. | TSQL, PGB, D, S, SC17, LC |
| G04 | Started purge, frozen candidates and deterministic current-candidate drift. | Execute deletion; `(40001,rev869b_purge_candidate_drift)`. | `CurrentHash≠FrozenHash ∧ Deleted=0 ∧ ContextAfter=Before ∧ FailedEvents_attempt=1`. | Remove drift, accept deletion or use generic error; fail. | TSQL, PGB, D, S, SC17, LC |
| G05 | Exact test-only delete failpoint and before context fingerprint. | Delete, roll back, record failure; `(P0001,TR_rev869b_purge_delete_failpoint)`. | `Deleted=0 ∧ ContextAfter=Before ∧ FailedEvents_attempt=1`; event independently committed after rollback. | Disable failpoint, accept transaction-local failure row or omit rollback fingerprint; fail. | TSQL, PGB, D, S, SC17, LC |
| G06 | Concurrent start/execute barriers, actual failed parent and exact prospective child. | Race, reject substituted retry, accept one monotonic retry; wrong retry `(42501,rev869b_purge_retry_binding)`. | `ConcurrentStarts=2 ∧ ConsumedAuthorizations=1 ∧ Executions≤1`; child matches root/prior/target/op/scope/cutoff/max/ordinal/prior outcome/hash and `ActiveChildCount=1`; substitutions create zero child. | Alter each link independently, serialize race, or accept more than one child; fail. | TSQL, PGB, D, S, SC17, LC |
| E01 | Exact organization/field/as-of/expiry authorization and source-row hashes. | Prepare immutable minimized batch. | `PreparedRows=ExactAllowedProjection ∧ Count≤MaximumRows ∧ PreparedHash=Hash(CanonicalRows) ∧ ExcludedFieldCount=0 ∧ PreparedEvents_batch=1`; verifier recomputes hashes/field keys without unrelated payload. | Add excluded field, change source/batch row/hash or accept stored hash without recomputation; fail. | TSQL, PGB, D, S, SC17, LC |
| E02 | Prepared batch fingerprint plus independently inserted later eligible ledger row. | Reread batch. | `PreparedAfter=PreparedBefore ∧ HashAfter=HashBefore ∧ CountAfter=CountBefore`; later row independently exists and is absent. | Include later row, omit its existence proof or compare labels only; fail. | TSQL, PGB, D, S, SC17, LC |
| E03 | Expired/wrong-batch/terminal/concurrent-active release fixtures and baseline batch hash. | Read/authorize release; `(42501,rev869b_export_release_sequence)`. | Per subcase: `ReleasedRows=0 ∧ NewReleaseEvents=0 ∧ PreparedHashAfter=Before`. | Drop a subcase, allow release/event or accept generic denial; fail. | TSQL, PGB, D, S, SC17, LC |
| E04 | ReleaseStarted batch with delivery-loss barrier and baseline batch hash. | Record Interrupted; authorize distinct new release. | `R1=Interrupted ∧ R2.Id≠R1.Id ∧ R2.Prior=R1.Id ∧ ActiveReleases=1 ∧ DeliverySuccess≤1 ∧ BatchHashUnchanged`; distinct evidence each step. | Reuse ID, sever prior link, permit two active/successful releases or alter batch; fail. | TSQL, PGB, D, S, SC17, LC |
| A01 | CP and target structured ACL readers over complete expected inventories. | Enumerate effective privileges; exact match. | `Observed=Expected ∧ Observed−Expected=∅ ∧ Expected−Observed=∅`, including database/schema/table/sequence/function/default, owners, attributes/memberships, admin/runtime/audit/purge/export/verifier/`PUBLIC`. | Remove any ACL dimension or insert/delete/change one tuple; fail. | CP-I, CP-V, TSQL, PCI, PGB, D, S, SC16, SC17, LC |
| A02 | Per-principal/per-protected-object/per-ungranted-operation fixture matrix plus before fingerprint. | Attempt every prohibited privilege/function; `(42501,rev869b_protected_object_acl)`. | Every tuple: `Allowed=false ∧ ProtectedAfter=Before`, one durable result per runtime/purge/export/recovery/admin/ordinary/`PUBLIC` subcase. | Remove one tuple, accept aggregate label/generic error or allow fingerprint drift; fail. | CP-I, CP-V, TSQL, PCI, PGB, D, S, SC16, SC17, LC |
| T01 | Exact opt-in, external pins, CP lease evidence, target identity and role inventory. | Allocate controller-owned fixture; InUse. | `LeaseCount_run=1 ∧ FixturePrepared ∧ TargetCount=1 ∧ TargetHash=expected ∧ RuntimeRole=...app_runtime ∧ VerifierRole=...target_verifier ∧ AdminCredentialsInTest=0 ∧ LeaseState=InUse`; cleanup exact absence. | Inject admin credential, wrong target/role/hash or omit cleanup proof; fail. | CP-I, PCI, D, S, SC16, SC17, LC |
| T02 | L04 during-DROP fixture, surviving attempt and signed external controller process audit. | Fail process, dispose/restart/reconcile exact attempt. | `RestartedInstance≠Original ∧ ReconciledAttempt=SurvivingAttempt ∧ DropStartedEvents=1 ∧ FinalizedEvents=1 ∧ TargetAndRolesAbsent ∧ CleanupEvidenceCount=1`. | Reuse process/attempt, duplicate event or omit absence/cleanup; fail. | CP-I, PCI, D, S, SC16, SC17, LC |
| T03 | The executable plan, query delegates and assertion delegates for each of the other 33 facts and T03 itself. | Mutate each action/read/assertion/error/cleanup path. | For every `s`, `KilledMutants_s=RequiredNonEquivalentMutants_s`; failure identifies the intended invariant. Metadata blanks and contract-hash mutations do not count. | Seed surviving semantic mutants in every action, query, decisive comparison, denial identity and cleanup proof; any survivor fails T03. | D, S, SC17, LC |

Mapping result: **34/34 scenarios have an explicit independent fixture, action, expected result, independent observation source, row/state/audit formula, semantic mutation requirement and required-file route.** This is a design mapping only; database acceptance remains unexecuted and unclaimed.

## 8. Mandatory changes versus external prerequisites

### Mandatory source changes

- F23-01 exact `DropAuthorized` event lookup/binding and stable rejection in control-plane install SQL.
- The six narrowly scoped verifier evidence functions and their Up/Down lifecycle, exact ownership, revokes, grants, expected inventories and catalogue hashes in the two SQL packages and target migration SQL helper.
- A structured P03 delta/code/object result with no division sentinel.

### Mandatory test and contract changes

- Update both exact function inventories.
- Replace the descriptive copied-count/sentinel contracts with 34 immutable typed executable plans.
- Replace the single signed-response adjudicator with prepare/observe/action/observe/cleanup/adjudicate stages.
- Execute every declared query through only the authorized verifier/audit role for that surface; recompute counts/hashes locally.
- Implement F23-01 positive and missing/substituted/stale/replayed/cross-instance/cross-lease/wrong-version negatives.
- Implement complete semantic mutation coverage and source guards against generic/shared/echoed/constant evidence.
- Preserve all previously passing column, quarantine, purge, ACL and durable rollback contracts.

### External prerequisites required only for later execution

1. Approved isolated PostgreSQL cluster, surviving control plane and disposable targets.
2. Exact roles, owners, memberships, database/schema/object/default/`PUBLIC` ACLs and rotated scoped credentials.
3. Pinned cluster/TLS/source/package/controller/target provenance.
4. Independently reviewed deployed controller/reconciler and distinct append-only controller audit interface implementing the typed action/barrier/restart protocol.
5. Deterministic concurrency barriers, process restart controls and C04/G05 test-only failpoints with reviewed teardown.
6. Authorized management/recovery/purge/export/audit/verifier identities and decisions.
7. A separate management authorization for PostgreSQL execution after committed Correction 24 passes internal and fresh independent source-only review.

These prerequisites block execution-helper readiness. They do not block the corrected bounded source-only implementation.

## 9. Frozen architecture and ACL decision

**Frozen architecture: RETAIN. ACL boundary: RETAIN.**

- Provisioning stays external.
- Only the dedicated lifecycle controller holds lifecycle-administrator authority.
- The surviving control-plane database remains authoritative for lifecycle/recovery/quarantine evidence.
- Command, purge and export ledgers remain target-local and transactional.
- No new production table, migration identity, designer or snapshot change is permitted.
- Verifier evidence is exposed only through purpose-specific minimal read functions. No direct ledger grant, privilege inheritance, export-service reuse, administrator credential or `PUBLIC` execute grant is permitted.
- Purchase workflow, permissions, approvals, calculations, histories and existing audit semantics are outside the correction and must remain byte-for-byte unchanged.

## 10. Corrected Correction 24 objective source gate

A future Correction 24 implementation may reach its checkpoint only if:

1. Entry HEAD/report hash/status match the new management authorization and only the eleven allowlisted files change.
2. F23-01 immutable event binding and every required negative contract pass.
3. All six evidence readers satisfy caller, key, target identity, minimal projection, no-mutation, owner/default/`PUBLIC` and exact EXECUTE closure.
4. Exactly 34 unique facts and 34 unique typed executable plans are discovered; all 34 execute distinct fixture/action/observation/formula paths.
5. No shared verdict signature, copied count default, sentinel, echoed label, constant PASS, generic exception-only, zero-row-label, shared evidence record, missing fixture/query or compressed subcase remains.
6. T03 kills 100% of required non-equivalent semantic mutants for all 34 plans.
7. Build has zero warnings/errors; focused REV869B and complete non-PostgreSQL suites pass; PostgreSQL discovery is exactly 34 and execution count is zero.
8. PowerShell 5.1 AST, EF no-connect migration discovery, REV869A/B uniqueness/adjacency, model/snapshot parity, offline Up/Down SQL generation/hashes, secret/privacy/prohibited-operation scans, ACL/owner/default/`PUBLIC` scans and `git diff --check` pass.
9. The new immutable implementation checkpoint records exact files, totals and SQL hashes and makes no source-safety/helper-readiness/database/production PASS claim.
10. The implementation stops for a separate internal adversarial precheck and then a fresh independent source-only safety review. PostgreSQL remains unauthorized throughout.

## 11. Prohibited operations and single next gate

No PostgreSQL access/test, provisioning, migration apply/remove, lifecycle, purge, recovery, quarantine, export or production operation is authorized. No source/test/helper/SQL/migration modification is authorized by this report itself. No Correction 25, history rewrite, prior-report modification or access to `../legacy-reference/` is permitted.

**Single next management gate:** authorize one bounded source-only Correction 24 implementation starting from the commit containing this report, restricted to the exact eleven-file allowlist in section 4, followed by an immutable implementation checkpoint and immediate stop for internal adversarial precheck. PostgreSQL execution remains not authorized.

correction_24_corrected_source_only_gate=GO
correction_24_allowlist_reconciliation_state=PASS
frozen_architecture_state=RETAIN
acl_boundary_state=RETAIN
external_prerequisite_blocking_state=YES
rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN
