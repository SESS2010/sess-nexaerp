# REV869B Correction 19 failure-reconciliation review

Date: 2026-08-14 (Asia/Calcutta)

Review type: source-only failure reconciliation; no Correction 20 implementation

Starting commit: `7c7455ad965b503c7d84492907c389edbf36910c`

Reviewed Correction 19 commit: `9917812388c54a874df6061a32451878a6c88728`

Reviewed Correction 19 parent: `c8b692070c4257623877db42803510116ff1d830`

Exact Correction 19 diff: `c8b692070c4257623877db42803510116ff1d830..9917812388c54a874df6061a32451878a6c88728`

## 1. Decision

```text
correction_20_source_only_gate=GO
frozen_architecture_state=RETAIN
external_prerequisite_blocking_state=YES
```

The selected architecture remains valid: externally provisioned cluster/roles, a dedicated lifecycle controller, a surviving control-plane database, and target-local transactional command/purge/export ledgers. None of the six blocking findings requires Option A to change or revives rejected Options B/C. The failures are incomplete or non-enforcing implementations of requirements already stated by the freeze, plus non-probative test evidence.

`external_prerequisite_blocking_state=YES` means external provisioning and a real isolated controller/database remain mandatory before execution or behavioral acceptance. It does not block the bounded source-only Correction 20 authorized below. Correction 20 may define and compile the exact contracts and tests, but it cannot claim execution/helper readiness without the external prerequisites and a later separate execution authorization.

PostgreSQL access and execution remain unauthorized.

## 2. Entry gate and authoritative scope

- HEAD matched the required starting commit `7c7455ad965b503c7d84492907c389edbf36910c`.
- Target-scoped Git status was clean before this report was created.
- `outputs/rev869b_architecture_freeze_root_cause_review.md`: SHA-256 `FBD74D7663BB3FD989158DB97C5544A2DA31307E5113DD5C12283E7959BC1B08`.
- `outputs/rev869b_source_correction_checkpoint_19.md`: SHA-256 `CD41495678E87AFC6415E5F9DE115AEF30A919175CAD73CCD79EEAA6DB3682C2`.
- `outputs/rev869b_preapply_source_safety_rereview_after_correction_19.md`: SHA-256 `488AB01FC5AEED38C94F8E027A030A2ED3DD924A681A001B6926543DF15346A8`.
- The exact Correction 19 diff reconciles to 30 files, 1,280 insertions and 4,579 deletions and passes `git diff --check`.
- No path under `../legacy-reference/` was read, enumerated, modified, staged or committed.

Only R19-N01 through R19-N06 are blocking findings. R19-N07 was explicitly classified by the rereview as checkpoint reconciliation rather than an architecture blocker; it is retained as a checkpoint-accuracy correction, not added to the Correction 20 blocker set.

## 3. Exact blocking finding list

1. **R19-N01 — control-plane verification is not canonical or complete.** The verifier checks selected object names/owners and ACL sets, not canonical definitions and complete effective privileges across every grantee/object category.
2. **R19-N02 — quarantine, recovery action and lifecycle replay are incomplete.** `Quarantined` is unreachable, recovery-authorized action is not enforced at drop/finalize, and cleanup-failure acknowledgement loss is not idempotently replayable.
3. **R19-N03 — command terminal ownership and replay binding are not exact.** The shared audit role can terminalize a known attempt without exact terminalizer binding, and mismatched terminal replay is silently accepted.
4. **R19-N04 — purge authorization scope, failure authority and retry linkage are unenforced.** Candidate selection ignores approved scope, `PriorAttemptId` is not validated, and the destructive worker also records failure.
5. **R19-N05 — export minimization/replay and target ACL closure fail.** Prepared payload ignores the approved field subset, release sequencing/expiry is not enforced, and the target has no canonical complete effective-ACL verifier.
6. **R19-N06 — required PostgreSQL acceptance matrix remains label-only.** P01-T03 are grouped substring checks rather than isolated action-sensitive behavior bodies.

## 4. Finding classification and frozen-architecture reconciliation

Each blocker has exactly one primary category.

| Finding | Primary category | Frozen requirement already present | Architecture change required? | Source-only correction possible? |
|---|---|---|:---:|:---:|
| R19-N01 | **1. Source implementation defect** | Canonical definitions and complete database/schema/relation/sequence/function/default/PUBLIC/membership allowlists; no counts or samples. | No | Yes |
| R19-N02 | **1. Source implementation defect** | Explicit quarantine state graph, exact recovery decision/action, stable attempt and idempotent finalization/reconciliation. | No | Yes |
| R19-N03 | **1. Source implementation defect** | Exact request/attempt/backend/actor binding, one active attempt, terminal outcomes and idempotent restart reconciliation. | No | Yes |
| R19-N04 | **1. Source implementation defect** | Management-bound scope, frozen candidates, independent failure writer/reconciler and new linked authorization for retry. | No | Yes, with later external principal provisioning |
| R19-N05 | **1. Source implementation defect** | Exact field/row/as-of/expiry approval, immutable batch, audited release state machine and exact target ACL verifier. | No | Yes |
| R19-N06 | **4. Missing or non-probative test evidence** | One named invariant per isolated fixture with exact pre-state, result/error, post-state, durable evidence and cleanup. | No | Yes to author/compile; execution remains external |

No blocking finding has category 2 or 3 as its primary classification. External provisioning is a prerequisite for later execution, but it did not cause the source defects. The architecture describes the required responsibilities and state machines sufficiently to correct them without choosing a new design.

Adding a purpose-specific quarantine operation and an independently credentialed purge audit/reconciler operation does not amend Option A. Both are already named responsibilities in the frozen state machine, trust boundary and responsibility matrix, and the freeze expressly permits a role/function that maps to a frozen responsibility and replaces the defective path. They must not become generic transition or administrative APIs.

## 5. Why Correction 19 failed despite adopting the frozen design

Correction 19 implemented the architecture's component boundaries but not all of its executable invariants:

1. **Shape was substituted for canonical equivalence.** Tables, function signatures and role names were inventoried, while function bodies, constraints, triggers, defaults and every effective grantee were not.
2. **State labels were substituted for reachable transitions.** `Quarantined`, `PriorAttemptId`, approved export fields and release states were stored or mentioned without enforcement at the operation that consumes them.
3. **Client convention was substituted for database authority.** Fresh autocommit connections are used by honest clients, but granted functions still permit under-bound or caller-transaction-controlled terminal writes.
4. **Checkpoint claims exceeded source behavior.** The checkpoint claimed exact ACL closure, cross-backend terminal protection, scoped purge retry and minimized export, while the cited functions did not enforce those claims.
5. **Source-string tests were substituted for future behavioral proof.** P01-T03 labels and keywords were present, but named actions, deterministic fixtures/failpoints and authoritative state assertions were absent.

This is an implementation-completeness and evidence failure, not a failure of the selected component architecture.

## 6. Root cause and required correction for every finding

### R19-N01

Root cause: `rev869b-control-plane-verify.sql` models exactness as relation/function name-owner pairs, selected package-role execution privileges and selected direct table privileges. Preflight similarly verifies expected role rows without rejecting all unexpected package roles or proving complete definitions.

Required correction: express canonical expected inventories for every control-plane object definition and every effective privilege category, then compare expected and actual sets symmetrically. Include arbitrary/non-prefixed grantees, PUBLIC, database, schema, relations, sequences, functions, memberships and default ACLs. The verifier must fail on one extra or one missing fact.

### R19-N02

Root cause: the SQL state enum copied the frozen graph, but no purpose API realizes the quarantine edges. Recovery decision consumption stores `AuthorizedAction`, but drop/finalize consume only the attempt ID. Failure recording lacks the exact-replay branch used by finalization.

Required correction: add the minimum purpose-specific quarantine path for the frozen Reserved/Provisioning/Ready/InUse mismatch edges; bind the consumed decision's exact action to the permitted follow-on operation; make cleanup-failure and finalization same-evidence replay idempotent and different-evidence replay reject. No generic transition API is allowed.

### R19-N03

Root cause: start/open store exact execution/service/ownership/backend/transaction bindings, but the noncommit function neither consumes those bindings nor compares replay evidence. `ON CONFLICT DO NOTHING` collapses exact replay and mismatched replay.

Required correction: authorize terminalization through an exact attempt-bound terminalizer/reconciler contract; require the authoritative no-commit condition appropriate to Rejected, RolledBack or Abandoned; return the existing outcome only for identical outcome ID/state/category/binding; reject any mismatch; never deactivate an attempt that has a committed receipt. Do not require an impossible cross-database atomic transaction.

### R19-N04

Root cause: `Scope` and `PriorAttemptId` are passive columns. Candidate SQL filters only by cutoff/limit. Failure recording is granted to the same worker that performs deletion, contrary to the frozen independent audit/reconciler boundary.

Required correction: define the exact supported purge scope grammar and apply it to every candidate query; enforce that a retry authorization references an existing terminal Failed/Interrupted attempt and matches the required policy dimensions; grant failure/interruption recording only to an independently provisioned purge audit/reconciler principal; preserve atomic delete+Succeeded and separate post-rollback failure evidence.

### R19-N05

Root cause: authorization stores `Fields`, but preparation always emits all four payload keys. Release rows have no enforced active/terminal sequencing or expiry check at read. Target ACL evidence is ledger counts, not catalogue/privilege closure.

Required correction: project only approved fields and hash exactly that payload; deny preparation/read/release outside approval/batch expiry; allow a new release ID only after the prior release is Failed/Interrupted, while keeping every release visible; reject terminal/replayed/wrong-batch release IDs; add a canonical target definition/effective-ACL verifier equivalent in rigor to R19-N01.

### R19-N06

Root cause: the 34 frozen matrix IDs were compressed into ten source tests and then accepted by keyword presence. The typed controller client accepts generic booleans/counts and is invoked for only R03, so source cannot show that each named scenario reaches its intended operation.

Required correction: implement exactly 34 independently discoverable scenario facts — `P01-P03`, `L01-L05`, `R01-R03`, `C01-C08`, `G01-G06`, `E01-E04`, `A01-A02`, `T01-T03` — with one invariant per fact. Each body must acquire or identify its isolated fixture, invoke the named API/action or deterministic failpoint, and assert the exact evidence contract in section 9. Source-only Correction 20 may compile/list these tests but must not run them.

## 7. Rejected, narrowed and non-expanding interpretations

- **R19-N07 is not a seventh blocker.** Correct the malformed Up hash and discovery total in the future checkpoint, but do not treat metadata correction as an architecture requirement.
- **The matrix contains 34 unique IDs, not 41.** The rereview's “41 P01-T03 cases” wording is a counting error. The authoritative table contains 34 IDs with no duplicates. The freeze's “replace all 25 bodies” describes the prior bodies to replace; it does not reduce the 34-row one-invariant-per-fact matrix.
- **Multiple export release rows are not inherently defective.** The freeze explicitly permits retries with new release IDs and requires every release to remain visible. The defect is absent state/expiry sequencing: a second release can start without the first reaching Failed/Interrupted, and an active release can remain readable outside the authorized window.
- **No exactly-once network export requirement is added.** Durable release attempts and honest Delivered/Failed/Interrupted outcomes remain the contract.
- **No generic database proof is demanded for every noncommit category.** The proof must match the category: application-observed rollback/rejection for Rejected/RolledBack, and reconciler-observed backend/lease/receipt state for Abandoned/Interrupted.
- **Purchase workflow is not reopened.** The prior review passed preservation of Purchase routes, approval thresholds, GST, histories, segregation and permissions; no blocker supplies contrary source evidence.
- **No new migration identity or business schema is authorized.** REV869B remains the single unapplied raw-SQL correction point immediately after REV869A.

## 8. External prerequisites

These cannot be fulfilled by Correction 20 source edits and continue to block execution/helper readiness:

1. External IaC provision of the exact capability-minimized control-plane and target roles, including the independent purge audit/reconciler principal selected by the frozen responsibility mapping, with NOINHERIT, closed memberships/default privileges and rotated credentials.
2. The pinned isolated PostgreSQL system identifier, endpoint, TLS/SPKI, environment and exact reviewed package/source manifest.
3. Installation of the reviewed control-plane package by the external lifecycle administrator in the surviving database.
4. Deployment of the dedicated lifecycle controller/reconciler and management approval writer outside application/test processes.
5. Controller support for the exact 34 scenario contracts, isolated deterministic fixtures, deterministic failpoints, restart/barrier control and signed/bound evidence.
6. Separate authorization to run read-only verification and then the PostgreSQL acceptance suite.

Correction 20 may define the expected role/API/catalogue and compile the clients/tests without provisioning any of the above. It may not claim that these prerequisites exist.

## 9. Objective acceptance evidence for every blocker

| Finding | Source-only acceptance evidence required at the next review | Later PostgreSQL evidence required after separate authorization |
|---|---|---|
| R19-N01 | Parsed expected inventories cover all objects/definitions and all privilege dimensions; symmetric-delta predicates have no sampled/count shortcut; mutation-oriented source tests each remove/add/change one fact and prove the verifier source would reject it. | Exact verifier passes pristine package and fails independently for changed column/default/constraint/index/trigger/function body/owner, extra arbitrary grantee, sequence privilege, default ACL, membership, schema and database privilege. |
| R19-N02 | Purpose APIs and constraints encode every frozen quarantine/recovery/finalization edge; recovery action is joined from the consumed decision at execution; same-evidence failure/finalization replay returns the same ID and mismatch rejects. | L01-L05/R01-R03 prove every interruption, quarantine, one-time decision, exact action, restart and terminal evidence path from the surviving control plane. |
| R19-N03 | Function signatures/tables enforce exact terminalizer binding and equality-checked replay; source contains no `ON CONFLICT ... DO NOTHING` terminal shortcut; C# passes the exact binding and reconciles authoritative receipt/outcome. | C01-C08 prove commit/rollback/restart/replay/concurrency/substitution, including foreign terminalizer denial and identical-versus-mismatched terminal replay. |
| R19-N04 | Candidate query demonstrably consumes parsed authorization scope; retry has a real FK/validated prior terminal attempt and policy linkage; failure API is absent from purge-worker grants and present only for the independent audit/reconciler. | G01-G06 prove wrong-scope denial, genuine ZeroRows, nonzero exact deletion, drift rollback, durable separate failure, concurrency and new linked authorization retry. |
| R19-N05 | Export payload construction is conditional on approved fields; release constraints/functions enforce expiry and one active attempt with new ID only after Failed/Interrupted; target verifier enumerates exact definitions and all effective privileges. | E01-E04/A01-A02 prove field/row/as-of immutability, post-prepare stability, expiry/replay/foreign-ID denial, release retry sequencing and every extra/missing ACL failure. |
| R19-N06 | Exactly 34 uniquely named facts are discoverable; each body calls a real typed controller/database action and contains scenario-specific initial state, action/failpoint, exact result or SQLSTATE/object, final state, durable evidence digest/count, unrelated-mutation assertion and cleanup finalization. No fact may pass by source substring, report label, generic exception, absent fixture or zero affected mutation. | All 34 run against separately authorized isolated controller-owned fixtures; 34 pass, 0 fail, 0 skip, with per-case evidence retained and every lease/role cleanup Finalized. |

The next source review must evaluate these objective predicates, not infer behavior from names, totals or comments. PostgreSQL results are a later gate and cannot be fabricated or substituted by source assertions.

## 10. Minimal authorized Correction 20 file/change scope

Correction 20 is authorized source-only and only for the six blockers above. The maximum file allowlist is:

### Control-plane package

1. `tools/rev869b-control-plane-preflight.sql`
2. `tools/rev869b-control-plane-install.sql`
3. `tools/rev869b-control-plane-verify.sql`

Allowed changes: canonical definition/ACL inventories; purpose-specific quarantine, action-bound recovery, idempotent failure/finalization; exact grants/default-ACL checks. No provisioning command or generic transition API.

### Target command/purge/export implementation

4. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs`
5. `src/SESS.NexaERP.Infrastructure/Persistence/Rev869BCommandContextAuthorizer.cs`

Allowed changes: exact noncommit binding/replay, scoped purge and linked retry, independent purge failure writer, approved-field export and release sequencing, canonical target verifier. Only the existing REV869B raw SQL fragment may change; no migration ID/designer/snapshot or business model change.

### Test contracts and future behavior bodies

6. `tests/SESS.NexaERP.Tests/Rev869BControlPlaneProvisioningContract.cs`
7. `tests/SESS.NexaERP.Tests/Rev869BControlPlaneRegistry.cs`
8. `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs`
9. `tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs`
10. `tests/SESS.NexaERP.Tests/Rev869BCorrection16SourceContractTests.cs`
11. `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs`
12. `tests/SESS.NexaERP.Tests/Rev869BDatabaseSafetyContractTests.cs`
13. `tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs`
14. `tests/SESS.NexaERP.Tests/Rev869BOwnedPostgresDatabase.cs`
15. `tests/SESS.NexaERP.Tests/Rev869BPurgeCoordinator.cs`
16. `tests/SESS.NexaERP.Tests/Rev869BTestDatabaseLease.cs`

Allowed changes: replace label/count/string acceptance with parsed source invariants and 34 genuine compiled future PostgreSQL bodies; bind typed evidence to exact lease/attempt/decision/fixture/cluster/manifest/state/error/digest/cleanup facts. No database test execution.

### Documentation

17. One future Correction 20 checkpoint report recording the exact commit scope, source-only validation and explicit nonclaims.

No other production, API, Purchase workflow, migration identity/designer/snapshot, helper mode, provisioning artifact, UI, AWS, OIDC, REV861, REV869A or REV869C file is authorized. If implementation proves another file indispensable, stop and obtain a scope amendment before editing it.

## 11. Correction 20 gate and stop condition

**GO for one bounded source-only Correction 20 using only section 10 and satisfying section 9.** The architecture is retained. This GO authorizes source/test/report edits only; it does not authorize provisioning, helper execution, PostgreSQL access, migration apply/remove, purge, recovery, quarantine, export, database tests or production use.

Exact next gate after a future Correction 20 commit: a fresh independent source-only safety rereview of that exact commit and parent. Only after a PASS and separate explicit authorization may external prerequisites or PostgreSQL behavior be considered.

This reconciliation task did not implement or begin Correction 20.
