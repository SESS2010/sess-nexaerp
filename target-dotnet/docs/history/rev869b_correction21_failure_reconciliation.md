# REV869B Correction 21 failure reconciliation

Date: 2026-08-14 (Asia/Calcutta)

Mode: controlled source-only failure reconciliation

Correction 21 source commit: `b24ba9a7d813f3e2c32ac8fe69275423cbc12cc1`

Independent review commit: `6e2e39660867843df1389b9b165d6b4f93118a12`

Preserved concurrent report commit: `eaf1f6028274be1a1d48178aa13bca12ed241cbe`

Independent review SHA-256: `E037FC580049216A71245F6C36FD09FA1D8BB09903F7C6E704C733C7D3A0EB4E`

## 1. Decision

The Correction 21 result is correctly reconciled as one narrow blocker PASS, four blocker FAILs and 34 derived scenario FAILs. The failures consolidate into six source-correctable groups rather than 34 independent repairs. No finding invalidates the selected four-component architecture. A bounded source-only Correction 22 is justified, but external provisioning and all PostgreSQL execution remain separately blocked.

```text
correction_22_source_only_gate=GO
frozen_architecture_state=RETAIN
external_prerequisite_blocking_state=YES
correction_21_failure_reconciliation_state=PASS
```

`GO` authorizes only the exact future source/test/report boundary in section 9. It does not implement Correction 22 and does not authorize PostgreSQL, provisioning or any lifecycle/data operation.

## 2. Entry-gate evidence

| Gate | Result | Evidence |
|---|:---:|---|
| Current branch | **PASS** | `master`. |
| HEAD | **PASS** | `6e2e39660867843df1389b9b165d6b4f93118a12`. |
| Parent chain | **PASS** | `6e2e39660867843df1389b9b165d6b4f93118a12` -> `eaf1f6028274be1a1d48178aa13bca12ed241cbe` -> `b24ba9a7d813f3e2c32ac8fe69275423cbc12cc1` -> `7a1e4739b733acb4a90594fa4112cad52aa0f71c`. |
| Correction 21 ancestry | **PASS** | `git merge-base --is-ancestor b24ba9a... HEAD` returned success. |
| Target-scoped status | **PASS** | Clean before report creation: 0 target-scoped entries. |
| Independent review hash | **PASS** | Exact SHA-256 `E037FC580049216A71245F6C36FD09FA1D8BB09903F7C6E704C733C7D3A0EB4E`. |
| `6e2e396...` scope | **PASS** | Exactly one modified path: `outputs/rev869b_preapply_source_safety_rereview_after_correction_21.md`; no source file. |
| `eaf1f602...` scope | **PASS** | Exactly one added path: the same review report; no source file. |
| Migration order | **PASS** | EF `--no-connect` found 13 migrations: REV869A exactly once at ordinal 12 and REV869B exactly once at ordinal 13, immediately adjacent. Tracked inventory contains one migration/designer pair for each. |
| History preservation | **PASS** | No reset, rewrite, cherry-pick, merge, rebase or deletion occurred. |
| Legacy boundary | **PASS** | `../legacy-reference/` was not read, enumerated, modified, staged or committed. |

The entry gate therefore authorized reconciliation to continue.

## 3. Commit-lineage and concurrent-report reconciliation

Correction 21 remains the sole reviewed source state. Commit `b24ba9a...` contains the exact 11-file, 549-insertion/92-deletion correction diff against `7a1e473...`. Commit `eaf1f602...` was created directly on `b24ba9a...` and added only the requested review-report path. Commit `6e2e396...` was created directly on `eaf1f602...` and modified only that same path. Neither report commit changes executable source, tests, SQL, migration identity, model or snapshot.

The current authoritative report content is the blob at `6e2e396...`, whose SHA-256 matches the supplied value. The earlier `eaf1f602...` report is preserved as linear history. It independently corroborates the 1/4/0-of-34 result and supplies narrower examples—quarantine replay omits the supplied version, purge new-root detection is policy-substitution-sensitive, and target function enumeration omits REV869A functions. Those examples are reconciled below as additional evidence within the same four failed blocker statements; they do not create a second source state or require history repair.

## 4. Five-blocker matrix

`Blocks C22` means the item must be closed in the future Correction 22 source commit before its source-only completion gate can pass. `Later PG` means runtime acceptance still requires separately authorized PostgreSQL execution after a future independent source PASS.

| ID and exact expected result | Result / classification | Actual failure and root cause | Source location | Shared correction | Required correction | Objective acceptance evidence | Later PG | Blocks C22 |
|---|:---:|---|---|---|---|---|:---:|:---:|
| B21-01 — Every command-terminalization SQL column exists in the authoritative physical schema with correct type and ownership | **PASS — preserve** | Correction 21 removed invalid attempt aliases. Attempt owns `TargetBackendPid integer`/`TargetTransactionId bigint`; context owns `BackendPid integer`/`TransactionId bigint`/`OpenedAt timestamptz`. | `Rev869BCommandContextSql.Install`; `TerminalizationReferencesOnlyAuthoritativeColumnsWithExactTypes` | RC21-04 must preserve this while repairing C05. | Do not reintroduce `a.OpenedAt`, `a.BackendPid` or `a.TransactionId`; keep table-derived alias/type scan. | Exact alias scan plus negative invalid-column/dynamic/broad-handler assertions; offline SQL generation succeeds. | **YES** for terminal behavior, not column ownership | **YES, preservation invariant** |
| B21-02 — Quarantine evidence is complete, durable, instance/operation/attempt-bound and cannot complete with missing evidence | **FAIL — SOURCE_DEFECT** | Outcome fields are durable/nonempty, but execution instance, actor/issuer and operation are same-call claims rather than values joined from an immutable attempt registration. Replay selects by lease/request and returns before validating caller `expected_version`; a false version can replay the old result. | `tools/rev869b-control-plane-install.sql`: `rev869b_lifecycle_attempts`, `rev869b_quarantine_outcomes`, `rev869b_record_quarantine`; verifier/signature contracts | **RC21-01** also resolves L05 and contributes to R01-R03/T02. | Persist authoritative execution/actor/issuer/operation at lifecycle-attempt registration under the lifecycle API; make quarantine compare those stored facts; persist source lease version and require exact version on replay; keep exact-evidence idempotence and mismatch rejection. | Parsed table/function contract proves every field originates from authoritative joined rows; mutation tests change execution, actor, issuer, operation, request, attempt and version one at a time and prove rejection; replay returns same outcome only for complete equality. | **YES** | **YES** |
| B21-03 — Purge retries bind original authorization, instance, batch, attempt sequence and prior terminal outcome | **FAIL — SOURCE_DEFECT** | Target digest is caller-supplied and never compared with a target-local authoritative identity. A failed attempt can be relabeled as a new root by changing any target/policy field used by the initial-root `EXISTS`; uniqueness applies only after the caller admits `PriorAttemptId`. | `Rev869BCommandContextSql.Install`: purge tables, `rev869b_register_purge_authorization`, `rev869b_start_purge`; `Rev869BPurgeCoordinator.RegisterAsync` | **RC21-02** also resolves G01/G06. | Add an immutable target-local singleton instance identity and compare every authorization/start with it. Reject every new root while any unresolved Failed/Interrupted attempt on that target/operation lacks a unique child, independent of changed scope/cutoff/maximum/digest; linked retry must preserve root/policy and advance exactly one ordinal. | Offline parsed SQL proves target identity is read, not trusted from a parameter; mutation tests vary target/scope/cutoff/maximum/root/batch/prior outcome/evidence and prove denial; exact FK/unique/unresolved-chain predicates and monotonic ordinal are present. | **YES** | **YES** |
| B21-04 — Target ACL closure covers owners, schemas, tables, sequences, functions, default privileges, inheritance and `PUBLIC` | **FAIL — SOURCE_DEFECT** | Table privileges are wider, but owner checks cover only `rev869b_%`; default ACL checks cover only the REV869B security owner; effective function comparison filters `proname LIKE 'rev869b_%'`. Arbitrary/PUBLIC EXECUTE or owner drift on non-REV869B functions, including the three REV869A functions, is outside the verifier. | `Rev869BCommandContextSql.Install`: `rev869b_verify_target_catalogue_acl`, ownership/revoke/default blocks; REV869A migration functions | **RC21-03** also resolves P01/P03/A01/A02. | Define canonical owner and effective-EXECUTE sets across every `nexa` function, including REV869A and any future unexpected function; compare expected/actual symmetrically for all ordinary roles and PUBLIC; enumerate default ACLs for every relevant owner, not one sampled owner. | Offline catalogue parser derives the complete authoritative function set; mutation tests add/remove/change one owner, direct grant, PUBLIC grant, default grant and membership and prove symmetric-delta failure; no prefix filter/count shortcut. | **YES** | **YES** |
| B21-05 — All 34 scenarios contain pinned, scenario-specific and objectively verifiable evidence | **FAIL — TEST_DESIGN_DEFECT** | Thirty-two facts call one generic remote runner; most contracts default to counts `1 -> 1`, affected `1`, and generic terminal/cleanup. Target database is absent from signed evidence; most hashes are shape-only; IDs are merely nonempty; compound cases share one tuple; fixture failpoints are not source-defined. T01/T03 allocation/release evidence is unsigned. Several bodies contradict the frozen matrix. | `Rev869BCorrection14PostgresDesignTests.cs`; `Rev869BCorrection17PostgresScenarios.cs`; `Rev869BLifecycleControllerClient.cs`; `Rev869BTestDatabaseLease.cs` | **RC21-05/06** resolves most scenario symptoms; RC21-01/02/03/04 supply underlying authoritative facts. | Restore the exact frozen 34 definitions; give every fact a scenario-local typed action and evidence schema; split compound subcases into pinned per-subcase records; include exact target database/lease/command/authorization/attempt IDs, deterministic expected counts/hashes, database-derived provenance, exact SQLSTATE/object and signed allocation/release/cleanup. Common transport is allowed; generic echo acceptance is not. | Offline mutation-sensitive tests prove removing/changing each fact's intended action or expected evidence makes that fact fail; exact 34 discovery; no default counts/hashes; failpoint/fixture declarations exist; signed envelope covers allocation, action and cleanup; later 34/34 isolated PostgreSQL PASS. | **YES** | **YES** |

## 5. Consolidated root-cause groups

| Group | Primary category | Symptoms covered | Root cause | One bounded correction |
|---|---|---|---|---|
| RC21-01 — authoritative lifecycle/quarantine binding | SOURCE_DEFECT | B21-02; L05; R01-R03; part of T02 | Evidence completeness was modeled as nonempty caller fields rather than equality to an immutable earlier authority; replay omits supplied source version. | Bind quarantine to stored attempt identity/action/actor/version and exact replay equality inside the surviving control plane. |
| RC21-02 — target-local purge identity and unresolved retry chain | SOURCE_DEFECT | B21-03; G01; G06 | Stored target/policy fields are not tied to current target identity, and “new root” detection depends on caller-preserved policy equality. | Add target-local identity authority and require the next authorization after any unresolved failure to link that exact attempt regardless of relabeling. |
| RC21-03 — incomplete ACL universe | SOURCE_DEFECT | B21-04; P01; P03; A01; A02 | Exactness still uses a REV869B prefix/one-owner subset for functions/defaults/ownership. | Canonical symmetric inventories across every `nexa` function, all relevant owners/defaults, every ordinary role and PUBLIC. |
| RC21-04 — rollback proof conflated with rolled-back context | SOURCE_DEFECT | C05; part of C04/C06 while preserving B21-01 | The `RolledBack` branch requires a context row created inside the transaction that has just rolled back. | Prove rollback/no-commit from durable attempt backend/transaction binding plus absence of receipt/backend transaction; context may only corroborate if it survived, never be mandatory after rollback. |
| RC21-05 — generic assertion envelope substituted for behavior-derived evidence | TEST_DESIGN_DEFECT | B21-05 and nearly all P01-T02 cases | Authentication of one signer was mistaken for independent proof of the described action, fixture and database-derived facts. | Scenario-local typed contracts, deterministic expectations, exact target identity, per-subcase records, independently derived/queryable evidence and signed full lifecycle envelopes. |
| RC21-06 — frozen matrix drift and compound-case compression | ARCHITECTURE_CONTRADICTION | P02, L01, L03, L04, T03 and compound L02/R02/C06/C08/G01/G06/E03/E04 | Test descriptions/actions were rewritten away from the authoritative frozen cases or combined several invariants into one tuple. The contradiction is in tests, not in Option A. | Restore the exact frozen action for each ID and make the meta-test T03 mutation-sensitive; use subcase evidence without inventing new scenario IDs. |
| RC21-07 — unavailable runtime environment | EXTERNAL_PROVISIONING_PREREQUISITE | Later execution for P/L/R/C/G/E/A/T | Cluster, roles, installed package, deployed reviewed controller, management writer, credentials and isolated fixtures do not exist in source-only evidence. | Keep external; do not fake it in source. Satisfy only after source PASS and separate authorization. |

Thus 34 FAIL rows do not authorize 34 unrelated source changes. Four production/control-plane corrections plus one coherent acceptance-contract redesign close the source-correctable surface; external runtime proof remains a later gate.

## 6. Complete 34-scenario classification matrix

Every scenario classification below is exactly one primary category. `Shared` names the consolidated correction that resolves the symptom. All rows block Correction 22 source completion because their current source definition/evidence is defective or derived from a source blocker; external deployment itself is not required to author and offline-validate the corrected contracts. PostgreSQL remains required later for behavioral acceptance except the T03 mutation-sensitivity meta-gate.

| ID and exact expected result | Classification | Actual failure / root cause | Source/test involved | Shared | Required correction | Objective acceptance evidence | Later PG | Blocks C22 |
|---|---|---|---|---|---|---|:---:|:---:|
| P01 — exact external manifest/catalogue/ACL verifier PASS | MISSING_OFFLINE_EVIDENCE | One signed `ExternalVerified` label/object does not expose canonical definition/ACL rows. | Inventory P01; generic `RunAsync`; verifier | RC21-03/05 | Require exact manifest plus complete expected/actual catalogue and ACL digest sets. | Deterministic inventory hashes and zero symmetric deltas, tied to exact target/control-plane instance. | YES | YES |
| P02 — each wrong system/TLS/endpoint/source/manifest denied before mutation | ARCHITECTURE_CONTRADICTION | Contract substitutes generic preflight/division sentinel for frozen lifecycle-operation denial and compresses all wrong pins. | Inventory P02; controller client | RC21-06 | Restore frozen action; one signed subcase per wrong pin with unchanged authoritative state and minimized rejection. | Per-subcase ID, wrong field, exact denial point, before/after hashes, rejection ID. | YES | YES |
| P03 — one unexpected role/database/object/grant yields exact verifier delta, no repair | MISSING_OFFLINE_EVIDENCE | Generic changed-definition/grant response lacks the concrete changed fact and rejected delta. | Inventory P03; verifier; generic runner | RC21-03/05 | Pin one mutation per subcase and return exact symmetric delta. | Mutation-oriented offline verifier tests plus later database-derived delta and unchanged state. | YES | YES |
| L01 — interrupt after reservation/before role; exact resume or approved cleanup | ARCHITECTURE_CONTRADICTION | Current case merely provisions Reserved -> Ready. | Inventory/body L01 | RC21-06 | Restore interruption and restart action with same attempt or approved cleanup branch. | Reservation/attempt/event IDs, phase marker, restart observation, exact terminal cleanup. | YES | YES |
| L02 — interrupt after every create phase; deterministic Ready or Quarantined | TEST_DESIGN_DEFECT | Every phase is aggregated into one tuple with no phase-local evidence. | Inventory/body L02; controller client | RC21-05/06 | Add ordered per-phase subcases under L02, each with unique attempt/restart evidence. | Phase, pre/post state, object-presence fingerprint, attempt/event/outcome/cleanup IDs. | YES | YES |
| L03 — two concurrent normal cleanup requests from Ready/InUse; one DropStarted/one DROP | ARCHITECTURE_CONTRADICTION | Current case starts a different lifecycle attempt from Provisioning. | Inventory/body L03 | RC21-06 | Restore concurrent cleanup/barrier case and authoritative loser observation. | Two request IDs, winner/loser attempt IDs, exact loser result, one DROP/finalization count. | YES | YES |
| L04 — DropStarted interruptions before/during/after DROP and role cleanup; one Finalized | ARCHITECTURE_CONTRADICTION | Current straight drop/finalize omits all frozen interruption boundaries. | Inventory/body L04 | RC21-06 | Restore per-boundary restart/reconcile subcases using one stable attempt. | Presence/absence and role fingerprints per phase, same attempt, one outcome/event. | YES | YES |
| L05 — marker/catalogue mismatch denies use/drop and durably Quarantines | DUPLICATE_OR_DERIVED_FAILURE | Generic response plus unbound quarantine evidence; claimed denial object is a label. | Inventory L05; `rev869b_record_quarantine` | RC21-01/05 | Close B21-02 and require separate use denial, drop denial and quarantine records. | Exact mismatch fact, authoritative attempt/actor/action/version, unchanged target, quarantine outcome and cleanup. | YES | YES |
| R01 — valid decision consumed once for exact action; Finalized or CleanupFailed | DUPLICATE_OR_DERIVED_FAILURE | Returned IDs/state do not prove stored decision, exact action or authoritative attempt binding. | Inventory R01; recovery/quarantine functions | RC21-01/05 | Bind decision/action/attempt and scenario-local readback. | Decision row before/after, consumption attempt, exact action, outcome and cleanup IDs/hashes. | YES | YES |
| R02 — wrong/expired/replayed/foreign/pre-state/action/nonce denied; valid decision preserved | DUPLICATE_OR_DERIVED_FAILURE | Only consumed replay is aggregated; variants and unused-decision preservation absent. | Inventory R02; recovery function; runner | RC21-01/05/06 | Per-variant subcase records and exact database constraints/messages; no label-only object. | Each request/decision/attempt, exact 42501 source, unchanged decision/state fingerprints. | YES | YES |
| R03 — failed recovery survives restart; old decision unusable; fresh linked decision required | DUPLICATE_OR_DERIVED_FAILURE | No source-local failure/restart chain or distinct linked decision evidence. | Inventory R03; `Rev869BTestDatabaseLease.RecoverQuarantinedAsync` | RC21-01/05 | Typed failure/restart/retry action with old/new decision and attempt evidence. | Durable first outcome, restart observation, old denial, new decision link, final outcome/cleanup. | YES | YES |
| C01 — business/history/receipt/outcome commit atomically | TEST_DESIGN_DEFECT | No fact-local transaction or exact row/fingerprint assertions. | Inventory/body C01; generic runner | RC21-05 | Scenario-specific command fixture and result schema. | Exact IDs; before/after business/history/receipt/outcome counts and hashes; nonzero mutation. | YES | YES |
| C02 — lost-response replay returns original receipt, no new rows/attempt | TEST_DESIGN_DEFECT | Generic `1 -> 1`, affected `1` cannot express zero duplicate mutation. | Inventory/body C02 | RC21-05 | Pin original receipt/request and zero duplicate deltas. | Same receipt ID/hash; business/history counts unchanged; no new active attempt. | YES | YES |
| C03 — changed request fingerprint returns exact 23505, no mutation | TEST_DESIGN_DEFECT | SQLSTATE/object and unchanged hashes are same-signer assertions. | Inventory/body C03; client | RC21-05 | Fact-local original/changed request digests and independent readback. | Exact constraint, request count/hash unchanged, zero business effects. | YES | YES |
| C04 — receipt failpoint rolls back business/history/receipt then noncommit terminalizes | MISSING_OFFLINE_EVIDENCE | Claimed trigger exists only as contract text; no fixture declaration/action/rollback design. | Inventory C04; scenario body | RC21-04/05 | Define reviewed deterministic test-only failpoint contract and terminal proof path. | Offline fixture/failpoint inventory; later exact P0001/trigger, all transactional deltas zero, durable noncommit outcome. | YES | YES |
| C05 — explicit rollback leaves durable request/attempt and exact RolledBack outcome | SOURCE_DEFECT | `RolledBack` requires a context row that rolled back with the transaction. | `rev869b_record_noncommit_outcome`; authorizer | RC21-04 | Use durable attempt backend/transaction binding plus no receipt/inactive backend proof; preserve B21-01 alias ownership. | Offline predicate tests for rollback/no receipt/exact binding; later real rollback and durable outcome readback. | YES | YES |
| C06 — four interruption points reconcile receipt-first without double commit | TEST_DESIGN_DEFECT | Four cases collapse into one composite terminal label. | Inventory/body C06; reconciler client | RC21-04/05/06 | Per-interruption subcase evidence; exact terminal per subcase, never a union label. | Unique attempt/backend/phase IDs; receipt/outcome precedence; no duplicate business hashes. | YES | YES |
| C07 — concurrent attempts yield one active winner and authoritative loser | TEST_DESIGN_DEFECT | No barrier participants/winner/loser/ordinal evidence in body. | Inventory/body C07 | RC21-05 | Typed two-actor barrier result and exact loser contract. | Two execution IDs, one active attempt, unique ordinals, exact 40001/constraint, unchanged data. | YES | YES |
| C08 — every pool/backend/transaction/actor/org/version/operation substitution denied | TEST_DESIGN_DEFECT | Current setup omits pool/transaction/version and aggregates remaining variants. | Inventory/body C08 | RC21-05/06 | Restore all frozen substitutions as individually pinned subcases. | Each substituted field, intended function/constraint, exact 42501, zero claims/business mutation. | YES | YES |
| G01 — missing/expired/wrong target/batch/org authorization denied, no candidates/deletion | SOURCE_DEFECT | Target digest is passive; wrong organization can become ZeroRows; variants are combined. | Purge register/start; inventory G01 | RC21-02/05 | Enforce target singleton and exact scope/batch; separate denial subcases. | Exact auth/target/batch/scope IDs, 42501 constraint, zero attempts/candidates/deletions. | YES | YES |
| G02 — genuine empty eligible set terminates ZeroRows with exact pre-count 0 | TEST_DESIGN_DEFECT | Contract says zero but no authoritative eligible-set query/fingerprint proves it. | Inventory/body G02 | RC21-05 | Add deterministic noneligible fixture and exact eligibility query evidence. | Eligible count/hash 0 before/after, ZeroRows event only, no Succeeded. | YES | YES |
| G03 — exact frozen candidates deleted; durable histories preserved; Succeeded atomic | TEST_DESIGN_DEFECT | Generic `1 -> 1` cannot express deletion/preservation sets. | Inventory/body G03 | RC21-05 | Pin candidate IDs/digest, deletion delta and separate preservation relations. | Nonzero exact deletion, candidate hash, histories unchanged hashes, Succeeded in same transaction. | YES | YES |
| G04 — candidate drift rolls back deletion and preserves candidates | TEST_DESIGN_DEFECT | Same-signer unchanged hashes do not prove a deterministic drift mutation/rollback. | Inventory/body G04 | RC21-05 | Source-defined drift fixture and independent post-failure queries. | Exact 40001/constraint, deletion zero, candidates and target hashes unchanged, durable failure. | YES | YES |
| G05 — delete failpoint rolls back; separate audit principal records Failed | MISSING_OFFLINE_EVIDENCE | Trigger is label-only; no reviewed fixture or separate-commit proof. | Inventory G05; scenario body; purge audit client | RC21-05 | Define deterministic test-only trigger contract and two-principal sequence. | Offline fixture declaration; later exact P0001/trigger, zero deletion, audit principal/outcome survives. | YES | YES |
| G06 — one concurrency winner; substitutions rejected; one monotonic linked retry | DUPLICATE_OR_DERIVED_FAILURE | Current final state cannot express accepted retry; purge relabel bypass remains. | Inventory G06; purge SQL/coordinator | RC21-02/05/06 | Close B21-03 and return per-race/substitution/retry records with new auth/batch/ordinal. | One winner, exact loser, rejected relabels, root/prior/outcome/evidence equality, ordinal +1. | YES | YES |
| E01 — minimized immutable batch matches approved fields/rows/as-of/expiry | TEST_DESIGN_DEFECT | Generic counts/object identity do not prove payload projection or exact snapshot. | Inventory/body E01; export functions | RC21-05 | Pin authorization fields/rows/as-of and deterministic expected payload hashes. | Exact auth/batch/row IDs, projected keys only, row/batch hashes and nonzero count. | YES | YES |
| E02 — later ledger insert does not change prepared batch | TEST_DESIGN_DEFECT | No fact-local later insert or before/after batch comparison. | Inventory/body E02 | RC21-05 | Add exact later-row action and reread same release/batch. | Later row ID exists; prepared row count/hash/rows identical; later row excluded. | YES | YES |
| E03 — expired/wrong/terminal/concurrent release IDs denied exactly | TEST_DESIGN_DEFECT | Several variants and read/authorize operations collapse into one response. | Inventory/body E03 | RC21-05/06 | Per-variant release records and exact function/constraint. | Distinct release IDs, exact 42501/constraint, batch/release hashes unchanged. | YES | YES |
| E04 — Interrupted release durable; retry uses distinct fresh release ID | TEST_DESIGN_DEFECT | Composite terminal label does not prove old outcome plus new active release. | Inventory/body E04 | RC21-05/06 | Return two exact release records and immutable batch evidence. | Old Interrupted ID/outcome; distinct new ReleaseStarted ID; same batch hash; final cleanup. | YES | YES |
| A01 — every ordinary effective privilege equals exact matrix | DUPLICATE_OR_DERIVED_FAILURE | Generic Verified response cannot cure incomplete function/owner/default universe. | ACL verifier; inventory/body A01 | RC21-03/05 | Close B21-04 and expose exact expected/actual matrices. | Zero deltas across database/schema/table/sequence/function/default/owner/member/PUBLIC facts. | YES | YES |
| A02 — every protected direct/ungranted capability denied for each principal/category | DUPLICATE_OR_DERIVED_FAILURE | One denial aggregates principals/categories; non-REV869B functions are outside verifier. | ACL verifier; inventory/body A02 | RC21-03/05 | Close B21-04 and use per-principal/per-category subcase evidence. | Exact 42501/object per attempt; no grants/mutations; canonical ACL fingerprint unchanged. | YES | YES |
| T01 — test receives no admin credentials; signed controller allocation and cleanup | TEST_DESIGN_DEFECT | Allocation/release JSON is unsigned and omits common authoritative IDs/fingerprints/outcomes. | T01 body; `AllocateAsync`/`ReleaseAsync` | RC21-05 | Put allocation and cleanup in signed, contract-bound envelopes; include exact database and provenance. | Request/lease/database/manifest pins, credential-role checks, signed cleanup ID/hash, target/roles absent. | YES | YES |
| T02 — exact failed fixture survives dispose/restart and reaches Quarantined/CleanupFailed/Finalized | TEST_DESIGN_DEFECT | “Any scenario” is not exact; generic response does not prove restart or surviving control-plane record. | Inventory/body T02; test lease | RC21-01/05 | Choose deterministic frozen failure fixture and explicit restart/read/finalize sequence. | Pre-failure lease/attempt, process boundary, post-restart durable row, exact cleanup outcome/absence. | YES | YES |
| T03 — removing intended action from any scenario body makes its offline mutation test fail | ARCHITECTURE_CONTRADICTION | Correction 21 replaced the frozen meta-test with two concurrent unsigned allocations. | Inventory/body T03; source-contract tests | RC21-06 | Restore mutation-sensitivity meta-test; concurrent-fixture coverage may remain elsewhere but cannot replace T03. | Offline mutations remove/change each intended action and corresponding test fails; pristine exact 34 passes. | **NO** for T03 itself | YES |

## 7. Frozen-architecture decision

| Frozen component | Decision | Reconciliation |
|---|:---:|---|
| External provisioning | **RETAIN** | No source path creates/drops cluster roles or databases. Exact roles/databases/credentials remain external. |
| Dedicated lifecycle controller | **RETAIN** | Tests must not receive lifecycle-admin credentials. Missing deployed/reviewed controller evidence is an external prerequisite, not authority to move lifecycle admin into tests or application code. |
| Surviving control-plane database | **RETAIN** | Lease/recovery/quarantine durability belongs here. RC21-01 is corrected inside this boundary with authoritative attempt/replay binding. |
| Target-local transactional ledgers | **RETAIN** | Command commit receipts, purge and export remain target-local. RC21-02/03/04 are local enforcement/verifier corrections, not a trust-boundary redesign. |

`frozen_architecture_state=RETAIN`. The `ARCHITECTURE_CONTRADICTION` scenario classifications identify test cases that contradict the frozen matrix; they do not mean Option A is contradictory. No architecture review is required unless a future implementation proposes a second provisioning path, exposes lifecycle-admin credentials, moves surviving evidence into disposable targets, or moves target transaction receipts to the control plane.

## 8. External prerequisites

These remain unavailable and block operational/execution readiness, but they do not block authoring and offline review of Correction 22:

1. Externally provisioned capability-minimized NOINHERIT control-plane and target roles/databases with exact membership, CONNECT, schema, object, default and PUBLIC closure and rotated credentials.
2. Pinned isolated PostgreSQL system identifier, endpoint, TLS/SPKI, environment, exact source commit and exact package/controller manifests.
3. External lifecycle-administrator installation of the reviewed control-plane package in the surviving database.
4. A deployed, independently reviewed lifecycle controller/reconciler whose implementation manifest is pinned and which never exposes administrator credentials to tests/application code.
5. A management approval writer and short-lived single-use recovery/purge/export decisions delivered through approved identity/secret channels.
6. Controller support for exact deterministic fixtures, failpoints, barriers, process restarts, database-derived evidence and signed allocation/action/cleanup envelopes for all frozen cases.
7. Separate authorization first for read-only PostgreSQL preflight/verifier work and later for behavioral execution.

`external_prerequisite_blocking_state=YES`. Source-only Correction 22 must state these as nonclaims and must not simulate their existence.

## 9. Exact bounded Correction 22 authorization

Correction 22 is authorized only as a source/test/report correction for RC21-01 through RC21-06. The maximum file allowlist is:

### Target-local enforcement

1. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs`
2. `src/SESS.NexaERP.Infrastructure/Persistence/Rev869BCommandContextAuthorizer.cs`
3. `tests/SESS.NexaERP.Tests/Rev869BPurgeCoordinator.cs`

Allowed: preserve B21-01 column ownership; repair durable rollback proof; add immutable target-instance authority; enforce unresolved failure linkage and exact purge retry; complete all-function/all-owner/all-default/PUBLIC ACL verification. Only the existing REV869B raw-SQL fragment may change. No new migration identity/designer/snapshot/business model.

### Surviving control plane

4. `tools/rev869b-control-plane-install.sql`
5. `tools/rev869b-control-plane-verify.sql`
6. `tests/SESS.NexaERP.Tests/Rev869BControlPlaneProvisioningContract.cs`
7. `tests/SESS.NexaERP.Tests/Rev869BControlPlaneRegistry.cs`

Allowed: authoritative lifecycle-attempt execution/actor/issuer/operation registration, source-version-bound quarantine replay, signature/catalogue/ACL updates and exact pure-model contracts. No provisioning commands, generic transition API or alternate lifecycle implementation.

### Exact 34-case future acceptance design

8. `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs`
9. `tests/SESS.NexaERP.Tests/Rev869BCorrection16SourceContractTests.cs`
10. `tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs`
11. `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs`
12. `tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs`
13. `tests/SESS.NexaERP.Tests/Rev869BTestDatabaseLease.cs`

Allowed: restore exact frozen P01-T03 semantics; scenario-local typed actions; pinned subcase evidence; exact target/fixture/ID/count/hash/SQLSTATE/object/outcome contracts; signed allocation/release/cleanup; offline mutation sensitivity; source declarations for future deterministic fixtures/failpoints without executing them. Common transport/serialization helpers are allowed, but a generic label/shape-only acceptance path is not.

### Documentation

14. `outputs/rev869b_source_correction_checkpoint_22.md`

Allowed: one future implementation checkpoint recording exact scope, offline validation and explicit external/PostgreSQL nonclaims.

No other file is authorized. In particular, no migration ID/designer/snapshot, REV869A, Purchase entity/service/endpoint/model, production, frontend, PowerShell provisioning/helper, AWS, OIDC, REV869C or legacy file may change. If a file outside this allowlist proves indispensable, stop and obtain a scope amendment before editing it.

## 10. Exact Correction 22 validation plan

Correction 22 must reproduce all of the following without PostgreSQL access:

1. Entry gate: exact authorized starting HEAD/parent/subject, clean target scope and exact allowlist diff.
2. Build with `--no-restore`: 5 projects, 0 warnings, 0 errors.
3. Focused REV869B tests excluding `Postgres`/`PostgreSql`: all pass, 0 skipped.
4. Complete suite excluding `Postgres`/`PostgreSql`: all pass, 0 skipped.
5. PostgreSQL compilation/discovery only: exactly 34 Correction 22 matrix facts in frozen order; 0 executed.
6. B21-01 preservation: table-derived alias/type/ownership scan; no invalid attempt columns, dynamic SQL or broad exception catch.
7. Quarantine offline contract: authoritative attempt fields, exact source-version replay, immutable outcome, per-field mismatch mutation tests.
8. Purge offline contract: target singleton equality, unresolved-failure chain independent of substituted policy, unique prior child and ordinal +1 mutation tests.
9. ACL offline contract: complete authoritative `nexa` function/owner/default/PUBLIC inventory; symmetric add/remove/owner/grant mutation tests including all three REV869A functions.
10. C05 rollback predicate contract: durable attempt fields and absence-of-receipt/backend-transaction proof; context row not required after rollback; B21-01 remains green.
11. Exact scenario contract scan: no generic defaults; exact target database; per-scenario/subcase IDs/counts/hashes/objects/outcomes; signed allocation/action/cleanup; source-declared fixtures/failpoints; T03 mutation sensitivity.
12. Windows PowerShell 5.1 AST for every tracked `.ps1`; 0 parse errors; no helper executed.
13. EF migration discovery with `--no-connect`, matching expected-database guard and inert `127.0.0.1:1`; exactly 13 migrations; REV869A ordinal 12, REV869B ordinal 13, unique and adjacent.
14. Explicit no-connect model/snapshot parity: 1 pass, 0 differences.
15. Offline REV869A -> REV869B Up and REV869B -> REV869A Down generation with byte/line counts and new SHA-256 values recorded.
16. Generated SQL scan: 0 CREATE DATABASE, 0 DROP DATABASE, 0 role/user creation/deletion, 0 backend termination.
17. Exact changed executable-file secret, privacy and prohibited-scope scans.
18. `git diff --check` on the exact Correction 22 range.
19. One future checkpoint only; commit contains only allowlisted files; target-scoped status clean.

After a future independent source-only PASS and separate authorization, later PostgreSQL acceptance must run the exact 34 cases: 34 passed, 0 failed, 0 skipped, with every fixture/lease/role cleanup Finalized and retained per-case evidence. Offline results cannot substitute for that later gate.

## 11. Explicit prohibited operations

This reconciliation and the authorized source-only Correction 22 boundary prohibit:

- PostgreSQL access, connection attempts or PostgreSQL test execution;
- provisioning or deprovisioning roles/databases;
- helper execution;
- migration apply, remove or rollback;
- lifecycle, purge, recovery, quarantine or export execution;
- production, AWS, OIDC, frontend or REV869C activity;
- access to `../legacy-reference/`;
- source changes during this reconciliation;
- Correction 22 implementation during this reconciliation;
- history reset, rewrite, cherry-pick, merge, rebase, deletion or cleanup of `eaf1f602...`;
- any file outside section 9 in a future Correction 22 without prior scope amendment.

## 12. Exact next gate

The exact next gate is one bounded source-only Correction 22 starting from this committed reconciliation report, changing only section 9 files and satisfying section 10. Correction 22 must stop after its implementation checkpoint and receive a fresh independent source-only review of its exact commit and parent. PostgreSQL and external execution remain NO-GO until that future review passes and separate explicit authorization is given.

```text
correction_22_source_only_gate=GO
frozen_architecture_state=RETAIN
external_prerequisite_blocking_state=YES
correction_21_failure_reconciliation_state=PASS
```
