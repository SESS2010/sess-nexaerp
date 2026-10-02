# REV869B Correction 24 internal adversarial precheck

Date: 2026-08-15

Verdict: **FAIL**. F23-01 is source-complete and fail-closed, but F23-02 is not established. The committed mutation test changes contract metadata without proving that altered evidence is rejected, and the scenario formulas consume controller-audit metrics for facts that the authoritative reconciliation requires the client to derive from independent database observations. Therefore all 34 scenarios fail this internal source-only gate. This is an internal precheck and is not independent.

## 1. Entry gate

| Check | Result | Evidence |
|---|---|---|
| HEAD | PASS | `25b14ce70b511b7d608feb6a09f260fdd00a5913` |
| Parent | PASS | `039eb850a14dfa5592dac0b3cbd7519d9d3a2f0d` |
| Branch / subject | PASS | `master`; `Implement bounded REV869B Correction 24` |
| Checkpoint hash | PASS | `650568E65EA4D4DF01CFAB6888C8839BED818D923E148D861368BC7111AF3FCD` |
| Target-scoped status | PASS | Clean at entry |
| Correction 24 scope | PASS | Exactly eleven committed files: the ten authorized implementation paths plus the implementation checkpoint |
| Later source/test/helper changes | PASS | None; authorized Correction 24 remains HEAD |
| Frozen architecture | PASS | Checkpoint states `RETAIN`; committed diff retains external provisioning, dedicated lifecycle controller, surviving control-plane database and target-local transactional ledgers |
| ACL boundary | PASS | Checkpoint states `RETAIN`; six new readers are verifier-only, fixed-search-path security-definer functions; no reader EXECUTE grant to `PUBLIC` |

No source, test, SQL, migration or helper was modified. PostgreSQL, provisioning, migration application/removal, lifecycle, purge, recovery, quarantine, export, production and `../legacy-reference/` were not accessed or executed.

## 2. F23-01 adversarial trace: PASS

The normal-drop chain is:

1. `rev869b_authorize_normal_drop` locks the Ready/InUse lease, changes it to `DropAuthorized`, increments its version, and appends the immutable event using the authorization request ID (`tools/rev869b-control-plane-install.sql:124-129`).
2. `rev869b_begin_drop` requires distinct nonzero transition and registration request IDs, then selects the immutable authorization event before the lease update (`:164-178`).
3. The lookup binds `LeaseId`, `RequestId`, null `AttemptId`, Ready/InUse `FromState`, `DropAuthorized` `ToState`, exact expected version and lifecycle principal. Its joins bind the current lease, target database pattern, cluster, TLS pin, endpoint, source commit, target manifest, target marker and control manifest (`:168-176`).
4. Because lease events are unique on `(LeaseId,RequestId)` and `(LeaseId,Version)` and immutable, the lease/request/version tuple identifies exactly one preceding event. Missing provenance raises SQLSTATE `42501`, object `rev869b_drop_authorization_event_binding`.
5. The state/version conditional update changes only the exact current `DropAuthorized` lease to `DropStarted`; a concurrent state change fails closed with `40001`. The normal attempt records the registration request and the authorization event's immutable evidence hash, and the distinct transition event records the new attempt and transition evidence (`:178-182`).
6. Existing cleanup-failure/finalization paths remain attempt-bound and terminalize the same surviving attempt; the correction did not broaden their ACLs.

| Source-level substitution | Fail-closed predicate/result | Result |
|---|---|---|
| Missing registration | No matching immutable event; `42501/rev869b_drop_authorization_event_binding` | PASS |
| Stale registration | Exact event and current lease versions must both equal `expected_version` | PASS |
| Replayed registration | Lease is no longer the exact `DropAuthorized` version; lookup/update cannot succeed | PASS |
| Cross-instance / cross-target | Event must join its exact lease and pinned target/control manifests | PASS |
| Cross-lease | `e.LeaseId=lease_id` plus both event uniqueness constraints | PASS |
| Wrong version | `e.Version=expected_version` and `l.Version=expected_version` | PASS |
| Wrong event/state | Null attempt, Ready/InUse source, DropAuthorized destination and lifecycle principal are exact | PASS |
| Wrong authorization identity | `e.RequestId=registration_request_id`; attempt persists that request and event evidence | PASS |
| Wrong expected pre-state | Current lease must be `DropAuthorized` before the conditional update | PASS |
| ACL bypass | Function checks `session_user`; normal path is lifecycle-only and existing purpose grants remain closed | PASS |

The source mutation contract at `tests/SESS.NexaERP.Tests/Rev869BCorrection16SourceContractTests.cs:128-165` removes each decisive predicate and confirms the exact source contract no longer holds. F23-01 is the previously corrected safety result that must be preserved.

## 3. F23-02 adversarial findings

### F24-PRE-01 — semantic mutants are not killed

`T03_EveryScenarioActionQueryAssertionAndCleanupIsMutationSensitive` at `tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs:41-68` proves only that `ApplyMutation` changes the descriptor hash. When structural validation does not reject a mutation:

- a removed decisive assertion is treated as success by asserting that the assertion is absent (`:61-62`);
- fabricated, substituted and cross-instance mutations are treated as success merely because a marker value was inserted (`:63-66`);
- no mutated evidence bundle is passed to `Evaluate`, no scenario is run with the mutant, and no rejection or failed assertion is required.

`ApplyMutation` confirms this weakness (`tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs:213-245`). `RemoveAssertion` deletes the check, while fabricated/substituted/cross-instance variants change or append expected metadata rather than altering an observed authoritative row. The test therefore permits surviving decisive-assertion mutants for every scenario. It also has no direct mutation that changes an observed count, state, audit row, lease or version and then proves evaluation failure.

### F24-PRE-02 — database-verifiable formulas are sourced from one audit document

`FormulaAssertions` hard-codes every formula metric to `EvidenceStage.Audit` (`tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs:111-152`). The client fetches that entire observation from one separately signed audit endpoint (`Rev869BLifecycleControllerClient.cs:285-293`) and compares many expected/actual values within that same document. Examples include C01 business/history deltas and their expected values, G03 eligible/frozen/deleted counts, E01 stored/recomputed hashes, and A01 ACL set differences.

The reconciliation requires database-verifiable counts and hashes to be cardinalities/recomputations over independently returned before/after/durable rows. It limits controller audit to physical process facts PostgreSQL cannot prove. Although the client does run database readers at `:295-328`, most decisive scenario formulas do not derive their counts, transitions or hashes from those observations. A correctly signed but internally fabricated audit metric can therefore satisfy the formula without corresponding authoritative database evidence.

### F24-PRE-03 — formula text is not bound to executable completeness

`ValidateContract` requires only a nonempty `ExactFormula` and nonempty/unique assertions (`Rev869BLifecycleControllerClient.cs:121-167`). It does not prove that every formula term has an assertion, that the assertion reads the authoritative surface, or that deleting one assertion invalidates the formula. `RunAcceptanceScenarioAsync` evaluates the remaining assertion list only (`:95-108`). Consequently, the immutable formula string can remain unchanged while a decisive executable term is removed.

The 23/23 Correction 24 contract/mutation test result is therefore a false green for the required mutation-sensitivity property. It verifies inventory shape, not the authoritative acceptance formula.

## 4. Complete 34-scenario matrix

Evidence notation: `D` is `Rev869BCorrection14PostgresDesignTests.cs`; `S` is `Rev869BCorrection17PostgresScenarios.cs`; `LC` is `Rev869BLifecycleControllerClient.cs`. Every row has a unique fixture/action/read-ID shape, but fails F24-PRE-01 and F24-PRE-02; T03 additionally fails its own claimed mutant-kill formula.

| ID | Exact objective formula | Evidence location | Result |
|---|---|---|---|
| P01 | PinMismatch=0; control fingerprint expected; target ACL delta empty; verifier exact | D:119,202; S:6; LC:95-108,213-245 | FAIL |
| P02 | PinMismatch=1; leases=0; actions=0; exact problem code/object | D:120,203; S:7; LC:95-108,213-245 | FAIL |
| P03 | Seeded delta=1/reported; protected mutations=0; cleanup baseline | D:121,204; S:8; LC:95-108,213-245 | FAIL |
| L01 | Reserved=1; resume XOR cleanup; duplicate attempts=0 | D:122,205; S:9; LC:95-108,213-245 | FAIL |
| L02 | Per boundary started/reconciled/target/roles=1 and Ready | D:123,206; S:10; LC:95-108,213-245 | FAIL |
| L03 | Requests=2; one DropStarted/active/physical drop; exact authorization chain | D:124,207; S:11; LC:95-108,213-245 | FAIL |
| L04 | Per boundary one DropStarted/Finalized; physical<=1; target/roles=0 | D:125,208; S:12; LC:95-108,213-245 | FAIL |
| L05 | Use/drop mutations=0; exact mismatch; quarantine=1/Quarantined | D:126,209; S:13; LC:95-108,213-245 | FAIL |
| R01 | Decision=1; exact attempt/action; recovery=1; Finalized=1 | D:127,210; S:14; LC:95-108,213-245 | FAIL |
| R02 | New attempts/events=0; replay denied; consumed once; RecoveryAuthorized | D:128,211; S:15; LC:95-108,213-245 | FAIL |
| R03 | Failure=1; old=0; fresh linked/consumed=1; Finalized=1 | D:129,212; S:16; LC:95-108,213-245 | FAIL |
| C01 | Business/history deltas expected; receipt/outcome=1; active=0 | D:130,213; S:17; LC:95-108,213-245 | FAIL |
| C02 | Business/history unchanged; same receipt/response; counts=1 | D:131,214; S:18; LC:95-108,213-245 | FAIL |
| C03 | Digest differs; exact replay error; request/attempt/business deltas=0 | D:132,215; S:19; LC:95-108,213-245 | FAIL |
| C04 | Exact failpoint; business/history/receipt deltas=0; RolledBack=1 | D:133,216; S:20; LC:95-108,213-245 | FAIL |
| C05 | Exact opened attempt/rollback; business/history/receipt delta=0; RolledBack=1 | D:134,217; S:21; LC:95-108,213-245 | FAIL |
| C06 | Four distinct interruption evidences; one authoritative terminal each | D:135,218; S:22; LC:95-108,213-245 | FAIL |
| C07 | Starts=2; started/active=1; exact loser; unrelated mutation=0 | D:136,219; S:23; LC:95-108,213-245 | FAIL |
| C08 | Every substitution rejected exactly; context/receipt/business deltas=0 | D:137,220; S:24; LC:95-108,213-245 | FAIL |
| G01 | Every invalid authorization has attempts/candidates/events=0 and exact error | D:138,221; S:25; LC:95-108,213-245 | FAIL |
| G02 | Eligible/frozen/deleted=0; ZeroRows event=1 | D:139,222; S:26; LC:95-108,213-245 | FAIL |
| G03 | N>0; frozen/deleted=N; exact candidate hash; remaining=0; success=1 | D:140,223; S:27; LC:95-108,213-245 | FAIL |
| G04 | Current hash differs; deleted=0; context unchanged; failure=1 | D:141,224; S:28; LC:95-108,213-245 | FAIL |
| G05 | Exact failpoint; deleted=0; context unchanged; durable failure=1 | D:142,225; S:29; LC:95-108,213-245 | FAIL |
| G06 | Starts=2; consumed=1; executions<=1; exact monotonic retry chain | D:143,226; S:30; LC:95-108,213-245 | FAIL |
| E01 | Exact projection; within maximum; canonical hash; excluded fields=0 | D:144,227; S:31; LC:95-108,213-245 | FAIL |
| E02 | Prepared rows/hash/count unchanged; later row independently absent | D:145,228; S:32; LC:95-108,213-245 | FAIL |
| E03 | Every invalid release has rows/events=0, exact error, unchanged batch | D:146,229; S:33; LC:95-108,213-245 | FAIL |
| E04 | R1 Interrupted; R2 distinct/prior-linked; active=1; success<=1 | D:147,230; S:34; LC:95-108,213-245 | FAIL |
| A01 | Observed=expected and both ACL set differences empty | D:148,231; S:35; LC:95-108,213-245 | FAIL |
| A02 | Every protected tuple denied exactly; fingerprint unchanged | D:149,232; S:36; LC:95-108,213-245 | FAIL |
| T01 | Lease/target=1; fixture prepared; exact identity; admin credentials=0; InUse | D:150,233; S:37; LC:95-108,213-245 | FAIL |
| T02 | Restart instance differs; attempt same; one DropStarted/Finalized; cleanup exact | D:151,234; S:38; LC:95-108,213-245 | FAIL |
| T03 | Killed mutants=required; every semantic mutant individually identified | D:152,235; S:41-68; LC:213-245 | FAIL |

Totals: **0 PASS / 34 FAIL**. Unique scenario IDs, fixture operation IDs, action operation IDs, cleanup operation IDs and 170 read IDs are present. Those uniqueness properties do not cure the shared audit-verdict and surviving-mutant defects.

## 5. Offline validation reproduction

| Validation | Result |
|---|---|
| Build (`--no-restore`) | PASS: 0 warnings, 0 errors |
| Correction 24 source/contract/mutation filter | PASS mechanically: 23/23; does not kill the mutants described above |
| Focused REV869B non-PostgreSQL | PASS: 73/73 |
| Complete non-PostgreSQL suite | PASS: 447/447 |
| Scenario discovery only | PASS: exactly 34 names, 34 unique names and 34 unique IDs; PostgreSQL execution=0 |
| PowerShell 5.1 AST | PASS: 5.1.19041.6456; 24 scripts; 0 errors; 0 helpers executed |
| EF migration discovery | PASS: `--no-connect`, inert `127.0.0.1:1`, exactly 13 migrations |
| Migration uniqueness/order | PASS: one REV869A primary migration and one REV869B primary migration; ordinals 12/13 and adjacent |
| Model/snapshot and retained-SQL contracts | PASS: 2/2 |
| Offline Up SQL, raw in-memory output | PASS reproduction: 280,057 bytes, 2,399 logical lines, SHA-256 `52D0073BAF870D55D5AFAED01C19F00CAC93E14F496774ED232732EC622DEC62` |
| Offline Down SQL, raw in-memory output | PASS reproduction: 10,600 bytes, 220 logical lines, SHA-256 `20BC1489BCA0555E9FCC7020367B31A06BAD30F9FD20689639A0D652E5479737` |
| ACL scan | PASS mechanically: 2 control and 4 target reader definitions, all six fixed-search-path; 0 `PUBLIC` reader grants |
| Secret/privacy scan | PASS: 0 added literal private-key/password/secret/API-key/token assignments |
| Prohibited-operation scan | PASS: client has no mutating SQL/DDL path and no administrator connection; the two SQL-keyword matches are C# `Create` method names only |
| Sentinel scan | PASS: no sentinel evidence; `22012` and `int4div` occur only in defensive rejection code |
| `git diff --check HEAD^ HEAD` | PASS |
| Target-scoped status before report | PASS: clean |

All SQL was generated in memory only and was not applied. The first normalized-text hash experiment changed newline representation; direct raw process capture reproduced both checkpoint hashes exactly. No generated SQL file was created.

## 6. Architecture, ACL and prerequisites

The frozen architecture remains valid and retained. The defect is confined to the acceptance evidence/test design and does not justify redesigning external provisioning, the dedicated lifecycle controller, the surviving control-plane database or target-local ledgers. ACL ownership/default/role/`PUBLIC` closure remains retained.

External prerequisites remain unavailable and blocking for later behavioral acceptance: an isolated externally provisioned PostgreSQL cluster; independent source/manifest/TLS/cluster/signing pins; externally provisioned owner/lifecycle/runtime/admin/audit/purge/export/recovery/verifier principals; independently deployed controller action/audit interfaces and trust roots; and explicit later PostgreSQL execution authorization. They do not prevent a report-only reconciliation of these source/test-design failures.

## 7. Required next gate

The single next gate is **management authorization for a report-only Correction 24 internal-precheck failure reconciliation**. That reconciliation must define the smallest bounded correction that (1) derives database-verifiable formula terms locally from typed before/after/durable reader output, (2) limits controller audit to irreducible physical-controller facts, (3) binds every immutable formula term to an executable assertion, and (4) proves each non-equivalent mutant is rejected by structural validation or by evaluating deliberately altered evidence. Correction 25 is not authorized or implemented.

correction_24_internal_precheck_state=FAIL
correction_24_internal_precheck_independence_state=NOT_INDEPENDENT
correction_24_scenario_pass_count=0
correction_24_scenario_fail_count=34
frozen_architecture_state=RETAIN
acl_boundary_state=RETAIN
external_prerequisite_blocking_state=YES
rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN
