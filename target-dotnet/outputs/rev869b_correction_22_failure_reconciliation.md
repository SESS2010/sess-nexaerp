# REV869B Correction 22 failure reconciliation

Verdict: **NOT APPROVED**

Reconciliation date: 2026-08-14

## 1. Mandatory preflight and authoritative state

| Item | Required | Observed | Result |
|---|---|---|:---:|
| Repository root | Existing NexaERP repository | `C:/Users/User/Documents/Codex/2026-07-03/see` | PASS |
| Target directory | `C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet` | Exact match | PASS |
| HEAD | `db999aecaa54a92d82ca5be15873243128ad9abd` | Exact match | PASS |
| HEAD parent | `5a114cb0dcb4a304916343c1e23f4bf75299132c` | Exact match | PASS |
| Branch | Attached work | `master`; not detached | PASS |
| Target-scoped status | Clean | 0 staged, 0 unstaged, 0 untracked target files | PASS |
| Git operation state | None | No merge, `rebase-merge`, or `rebase-apply` state | PASS |
| Reviewed Correction 22 commit | `5a114cb0dcb4a304916343c1e23f4bf75299132c` | Present as report parent | PASS |
| Correction 22 source range | d571a08e6ba691da8e1dc1a803df7c6bf73f8b42..5a114cb0dcb4a304916343c1e23f4bf75299132c | Exact 11-file reviewed correction range | PASS |
| Reviewed report commit | `db999aecaa54a92d82ca5be15873243128ad9abd` | One report-only file over its parent | PASS |
| Reviewed report range | `5a114cb0dcb4a304916343c1e23f4bf75299132c..db999aecaa54a92d82ca5be15873243128ad9abd` | Exactly one file | PASS |

The authoritative input is `outputs/rev869b_preapply_source_safety_rereview_after_correction_22.md`. Its verified SHA-256 is `6B81A1F8572C41F761594B55FF5EFD802DF75900761A5A69910B64B9F1C32746`.

The authoritative results are preserved without promotion: B21-01 PASS, B21-02 PASS, B21-03 FAIL, B21-04 FAIL, B21-05 FAIL; all 34 scenarios P01 through T03 FAIL. The unavailable runtime prerequisites remain unavailable. No compiled/discovered test is treated as behavioral evidence.

## 2. Process understood and checks completed

This is a read-only source reconciliation plus one report-only commit. It identifies why the five reported areas failed, traces each of the 34 scenarios to its current contract and executable assertion path, separates source/test defects from external dependencies, and proposes—but does not authorize or implement—bounded candidate work packages.

Completed checks were: exact Git gate; full authoritative-report read and hash; current control-plane SQL, target raw SQL, controller client, scenario inventory, scenario bodies and source-contract inspection; exact source references; warning-as-error build; focused and complete non-PostgreSQL suites; PostgreSQL list-only discovery; PowerShell 5.1 AST parsing; EF `--no-connect` migration listing; migration uniqueness/order; model/snapshot parity; reviewed-addition secret/privacy/prohibited-scope scans; and `git diff --check`.

## 3. Five-area root-cause reconciliation

### A. Recovery request/event identity

- Failed IDs: RC21-01 overall; scenarios R01 and R03 directly, with L04 and recovery portions of R02/T02 dependent. B21-02's narrow quarantine statement remains PASS and must be preserved.
- Source: `tools/rev869b-control-plane-install.sql:36,164-190`; signatures/grants in `tools/rev869b-control-plane-verify.sql:10-26`; contracts/models in `Rev869BControlPlaneProvisioningContract.cs`, `Rev869BControlPlaneRegistry.cs`, and `Rev869BLifecycleControllerClient.cs`.
- Intended invariant: a management recovery decision is consumed exactly once into an immutable, actor/operation/attempt-bound recovery authority; the subsequent exact action progresses through auditable events without reusing an idempotency identity for a distinct transition.
- Current behavior: `rev869b_consume_recovery_decision` inserts `RecoveryAuthorized` using `(LeaseId,request_id)`. `rev869b_begin_drop` requires the recovery attempt's `RegistrationRequestId=request_id` and inserts `DropStarted` using the same pair. The event table enforces `UNIQUE(LeaseId,RequestId)`.
- Contradiction: the required binding makes the next valid transition collide with the earlier event. Supplying another request avoids the unique collision only by failing the required binding.
- Classification/boundary: implementation defect plus missing behavioral evidence; recovery authorization, lifecycle audit and idempotency boundary.
- Smallest safe future correction: distinguish immutable recovery-registration identity from each transition request identity, or define an explicitly idempotent transition-event key that permits ordered distinct events without weakening exact attempt/decision/action binding. Preserve the existing quarantine attempt fields and replay comparisons.
- Required proof: pure-model transition tests; SQL contract tests that reject reuse for a different transition yet permit the authorized sequence; replay tests for each request; later PostgreSQL R01/R02/R03/L04 tests with exact event order, versions, attempt/decision/request IDs and one terminal outcome.
- Risks: accidental replay weakening, duplicate DROP, decision reuse, audit ambiguity, verifier signature drift, or incompatible deployed-controller API.
- External dependency: reviewed/deployed lifecycle controller and isolated control plane remain unavailable.

### B. Purge retry-root enforcement

- Failed IDs: B21-03; G01 and G06 directly; G02-G05 depend on the same authorization/attempt ledger.
- Source: `Rev869BCommandContextSql.cs:46-58,167-193`; callers in `Rev869BPurgeCoordinator.cs`; source assertions in `Rev869BCorrection17SourceContractTests.cs:68+`; scenario contracts/bodies at design lines 89-94 and scenario lines 28-33.
- Intended invariant: every retry after Failed/Interrupted is the unique monotonic child of that exact attempt, carries the same root/target/batch/operation and exact prior terminal evidence, and no fresh root can bypass an unresolved chain.
- Current behavior: new-root rejection asks only whether a child authorization exists. The child need not have started, remained valid, or terminalized. Once any child exists, another root can be registered. An expired unused child is irreplaceable because `PriorAttemptId` is unique. The check is not serialized against concurrent registrations.
- Contradiction: authorization-row existence is treated as resolution of a failed attempt; it is neither consumption nor terminal progress.
- Classification/boundary: implementation defect plus insufficient concurrency/retry tests; purge authorization and destructive-operation audit boundary.
- Smallest safe future correction: serialize on the exact failed attempt/root; define its unresolved state from the authoritative attempt chain; admit only one valid next child; prevent any new root until that child is started and terminally reconciled; explicitly define safe replacement of an expired unused child without losing history.
- Required proof: offline state-machine and SQL predicate tests including unused, expired, concurrent and policy-substitution cases; later PostgreSQL barriers showing one child, ordinal +1, exact prior evidence/root/target/batch equality, fresh-root denial and deterministic expired-child handling.
- Risks: deadlock or over-serialization, permanent stranded chains, accepting relabeled scope, duplicate deletion, or loss of failure evidence.
- External dependency: management writer, purge worker/auditor credentials, isolated deterministic candidate fixtures and runtime barriers.

### C. ACL-role closure

- Failed IDs: B21-04; P01, P03, A01 and A02 directly; any runtime scenario depends on correct least privilege.
- Source: target verifier `Rev869BCommandContextSql.cs:206-239`; control-plane verifier `tools/rev869b-control-plane-verify.sql:18-55`; install ownership/defaults at `tools/rev869b-control-plane-install.sql:211+`; source tests `Rev869BCorrection16SourceContractTests.cs` and `Rev869BCorrection17SourceContractTests.cs:86+`.
- Intended invariant: exact symmetric effective privileges and ownership across database, schema, relations, sequences, every function, default privileges, memberships/inheritance and PUBLIC for runtime, audit, purge, export, recovery, verifier and administrator roles.
- Current behavior: actual scans cross-join every non-superuser role while containing no classification for PostgreSQL predefined aggregate roles such as `pg_read_all_data`/`pg_write_all_data`. Those roles can legitimately report effective privileges and become false unexpected rows. At the same time `nexa_rev869b_lifecycle_administrator` is excluded wholesale, so its exact allowed/denied matrix is never checked.
- Contradiction: the universe is simultaneously overbroad for predefined roles and incomplete for the privileged administrator whose closure was requested.
- Classification/boundary: verifier implementation and test-evidence defect; least-privilege, ownership, role-inheritance and PUBLIC boundary.
- Smallest safe future correction: define an explicit canonical role taxonomy (application principals, administrator, owners, predefined/system roles and PUBLIC); compare exact expected effective and direct rights per category; verify administrator capabilities instead of excluding it; keep all-object/function/default-owner coverage.
- Required proof: offline inventory fixtures including predefined roles, membership chains, PUBLIC and administrator; symmetric-delta tests with one mutation in every category; later PostgreSQL P01/P03/A01/A02 against an externally provisioned exact role set.
- Risks: masking an actual grant by over-exclusion, rejecting supported PostgreSQL defaults, owner-derived false positives, privilege escalation, or verifier access that itself broadens authority.
- External dependency: exact provisioned cluster roles, database ACLs, memberships, defaults and lifecycle administrator installation.

### D. Rollback transaction visibility

- Failed IDs: RC21-04 and C05 directly; C04/C06 depend on the same noncommit terminalization proof. B21-01 remains PASS and its physical column ownership/types must not change.
- Source: `Rev869BCommandContextSql.cs:125-146`, especially `pg_stat_activity` predicates at lines 138 and 146; owner/grant clauses later in that SQL; `Rev869BCommandContextAuthorizer.cs`; source test `Rev869BCorrection17SourceContractTests.cs:51+`.
- Intended invariant: only after the original exact target transaction is authoritatively no longer active and no receipt committed may the audit principal durably record RolledBack; a rolled-back transaction-local context may corroborate but cannot be required.
- Current behavior: the SECURITY DEFINER recorder is owned by capability-free `nexa_rev869b_security_owner` and checks absence of a matching visible `pg_stat_activity.backend_xid`. That owner is neither superuser nor a member of `pg_read_all_stats`; restricted columns for another role can be null.
- Contradiction: absence of a visible XID is not authoritative absence of the transaction, so RolledBack can be accepted while the original transaction remains active.
- Classification/boundary: implementation/evidence-generation defect; command transaction, audit terminalization and replay boundary.
- Smallest safe future correction: replace visibility-dependent absence with a capability-minimized authoritative transaction-liveness witness. The design must be proven before coding—for example, an exact transaction identity/status primitive or collision-safe transaction-scoped fence whose acquisition/release semantics are independently testable—without granting broad statistics or superuser power to the audit path.
- Required proof: offline predicate/state-machine tests for active, committed-with-receipt, aborted, wrong backend/transaction and replay; explicit role-capability tests; later two-session PostgreSQL tests proving active denial, post-rollback acceptance, commit precedence and durable immutable outcome.
- Risks: false rollback, leaked locks/fences, XID reuse/wraparound, excessive role capability, deadlock, or terminalizing before commit visibility settles.
- External dependency: isolated multi-session PostgreSQL and exact runtime/audit principals.

### E. Generic or label-derived evidence

- Failed IDs: B21-05 and P01-P03, L01-L05, R01-R03, C01-C08, G01-G06, E01-E04, A01-A02 and T01-T03.
- Source: common contract factory `Rev869BCorrection14PostgresDesignTests.cs:9-63`, including `REV869B-C22|ID|purpose` at line 20; all inventory rows at lines 66-106; all bodies and common assertion runner `Rev869BCorrection17PostgresScenarios.cs:8-105`; expected label hash in `Rev869BLifecycleControllerClient.cs:242`; source-shape assertions in `Rev869BCorrection17SourceContractTests.cs`.
- Intended invariant: each scenario executes its exact frozen action against an exact deterministic fixture/target and accepts only independently queried, scenario-specific IDs, SQLSTATE/object, before/after facts, durable outcome and safe cleanup.
- Current behavior: all 34 call one `RunAsync` assertion envelope. Expected IDs/hashes derive from scenario labels. The actual target singleton is generated during installation, so a label-derived target hash is not an authoritative target fact. Compound cases are strings. C04/G05 failpoints have no source declarations. T03 mutates only its own serialized action. Several actions drift from the frozen cases.
- Contradiction: signature/shape equality proves that one endpoint returned the expected labels; it does not prove that the underlying action, fixture, transaction, denial or cleanup occurred.
- Classification/boundary: test-design and evidence-generation defects, plus unavailable controller/fixture runtime support and implementation dependencies A-D.
- Smallest safe future correction: replace the generic acceptance record with scenario-local typed action/results and per-subcase records; derive target/fixture/fact evidence from authoritative readbacks; add reviewed deterministic failpoint/barrier/restart contracts; restore exact frozen actions; make T03 mutation-test every scenario action.
- Required proof: offline negative/mutation tests that fail when any action/query/fixture/assertion is removed; unique scenario/subcase schema checks; later PostgreSQL evidence with exact database facts and signed controller provenance for every row.
- Risks: test-only backdoors entering production, shared-fixture contamination, false independence, leaking administrator credentials, non-determinism, unsafe cleanup or accepting a controller-signed fiction.
- External dependency: lifecycle controller implementation, signing key/pins, isolated targets, deterministic fixture/failpoint/barrier/restart support and separately authorized credentials.

## 4. Exactly 34-scenario reconciliation matrix

All current results are FAIL. “Common envelope” means the executable body reaches `RunAsync` at `Rev869BCorrection17PostgresScenarios.cs:73-105`; those assertions are executable only in a later authorized environment and were **NOT RUN** here. There is currently no independent authoritative scenario-specific behavioral assertion unless explicitly noted.

| ID | Scenario name | Required invariant | Exact failure | Authoritative source location | Current test location | Current evidence type | Why non-authoritative | Required implementation correction | Required test correction | Deterministic fixture | PG/runtime required | Future acceptance evidence | Dependency |
|---|---|---|---|---|---|---|---|---|---|---|:---:|---|---|
| P01 | External provisioning manifest verified | Exact external manifest/catalogue/ACL has zero delta | Generic `ExternalVerified`; defective ACL universe; no database-derived sets | control verifier `verify.sql:18-55`; target verifier `Sql.cs:206-239` | design `:66`; body `:8` | Common signed envelope | Label/hash does not expose expected/actual facts | Package C plus external verifier deployment | Assert canonical row sets and zero symmetric deltas | Exact provisioned cluster/control-plane manifest | YES | Signed pins plus independently queried catalogue/ACL rows and zero deltas | C, external provisioning |
| P02 | Mismatched external manifest denied | Each wrong system/TLS/endpoint/source/manifest pin denied before mutation | Uses generic division sentinel and one compressed preflight | controller client `:26-37` | design `:67`; body `:9` | Common envelope, SQLSTATE label | No lifecycle denial point or per-pin state proof | External controller must expose minimized pin-specific denial | One subcase per wrong pin; unchanged authoritative hashes | Correct pins plus one wrong field per subcase | YES | Exact pin, rejection ID/point, 42501-equivalent contract result, zero mutation | E, external controller |
| P03 | Catalogue/ACL drift denied | One unexpected role/database/object/grant yields exact delta without repair | Generic verifier denial; no concrete mutation/delta; ACL defect | both verifiers above | design `:68`; body `:10` | Common envelope | `22012` label is unrelated to concrete drift | Package C | Mutate one fact per subcase and assert exact symmetric delta/no repair | Exact canonical state plus one controlled drift | YES | Changed fact, expected/actual delta and unchanged non-target facts | C, E |
| L01 | Reserved interruption recovery | Interrupt after reservation/before role; exact resume or approved cleanup | Current action is ordinary Reserved-to-Ready provisioning | control-plane lease/provision functions `install.sql:82-115` | design `:70`; body `:11` | Common envelope | No interruption/restart/branch evidence | External controller phase checkpoint/reconcile support | Restore frozen interruption and assert same attempt or approved cleanup | Reserved lease stopped before role creation | YES | Lease/attempt/event IDs, phase marker, restart observation, one terminal cleanup | E, external controller |
| L02 | Interrupted create phases recovered | Every create-phase interruption reaches deterministic Ready or Quarantined | Six phases are string keys, not executed subcases | control lifecycle functions `install.sql:82-159` | design `:71`; body `:12` | Common envelope plus subcase labels | No phase-local objects/fingerprints/restart facts | External controller deterministic phase injection | Typed ordered subcase per phase | One isolated lease per create phase | YES | Phase, objects before/after, same attempt, event/outcome/cleanup IDs | E, L01 support |
| L03 | Concurrent normal cleanup | Two cleanup requests from Ready/InUse produce one DropStarted and one DROP | Current case starts concurrent lifecycle attempt from Provisioning | drop functions `install.sql:124,164-170` | design `:72`; body `:13` | Common envelope | Wrong action/state; no two request barrier or DROP count | Package A as needed; external cleanup barrier | Restore Ready/InUse cleanup race and winner/loser assertions | Ready and InUse leases with two-request barrier | YES | Two requests/attempts, exact loser, one DropStarted/DROP/finalization | A, E, controller |
| L04 | Drop interruption reconciliation | Before/during/after DROP and role cleanup converge once to Finalized | Straight drop/finalize; phase labels only | `install.sql:164-210` | design `:73`; body `:14` | Common envelope/subcase labels | No interruption boundaries or object-presence readbacks | Package A; external phase/restart support | Typed boundary subcases with one stable attempt | DropAuthorized target at each cleanup boundary | YES | Presence/role fingerprints, event versions, one final outcome | A, E |
| L05 | Identity mismatch quarantine | Mismatch denies use/drop and durably quarantines exact attempt | Binding improved; use/drop denials and mismatch are response fields only | quarantine `install.sql:131-159`; registry/client | design `:74`; body `:15` | Common envelope | No independent target mismatch or denial readback | Preserve B21-02; external verifier/action support | Separate use, drop and quarantine records | Ready target with one exact marker/catalogue mismatch | YES | Mismatch fact, bound attempt/actor/version, two denials, quarantine event/cleanup | E; B21-02 preserved |
| R01 | Exact recovery decision consumed | One valid decision authorizes exact action and terminal result | Bound sequence collides on `(LeaseId,RequestId)` | `install.sql:36,164-190` | design `:76`; body `:17` | Common envelope | Impossible source path plus no readback | Package A | Assert decision/event sequence and replay semantics | Quarantined lease with valid unconsumed decision | YES | Decision before/after, distinct transition requests, ordered events, final outcome | A, E |
| R02 | Recovery decision replay denied | Wrong/expired/replayed/foreign/pre-state/action/nonce denied; valid decision preserved | Only replay labels; variants compressed; transition identity design unresolved | recovery functions `install.sql:172-190` | design `:77`; body `:18` | Common envelope/subcase labels | No per-variant decision/state fact | Package A | Typed variant matrix and preservation assertions | One valid decision plus one controlled invalid field each | YES | Exact denial source, unchanged decision/lease hashes, valid path retained | A, E |
| R03 | Cleanup failure fresh recovery | Failure survives restart; old decision unusable; fresh linked decision required | Inherits request collision; no restart or old/new decision chain | recovery/cleanup `install.sql:177-202` | design `:78`; body `:19` | Common envelope | Finalized label does not prove durable chain | Package A; external restart support | Explicit old denial/new linkage/restart readback | CleanupFailed lease with first durable failed attempt | YES | First outcome, restart, old denial, new decision/attempt link, final cleanup | A, E, T02 |
| C01 | Atomic command commit | Business/history/receipt/outcome commit atomically | Generic counts/hashes, no concrete command facts | command SQL `Sql.cs:83-124` | design `:80`; body `:20` | Common envelope | No independently queried business/history/receipt rows | None beyond D if shared terminal design changes | Scenario-local command and relation assertions | Exact RFQ/purchase command graph | YES | Exact IDs and before/after row/hash deltas in one transaction | E |
| C02 | Lost-response replay | Same request returns original receipt with zero new mutation/attempt | Default affected data cannot prove zero duplicate work | command register/commit/reconcile `Sql.cs:83-124,152` | design `:81`; body `:21` | Common envelope | Same expected labels may be freshly fabricated | None specific | Force lost response; compare same receipt and zero deltas | One committed command with response interruption | YES | Same receipt/hash; unchanged business/history/attempt counts | E, C01 |
| C03 | Changed request replay denied | Changed digest gets exact 23505/object and no mutation | SQLSTATE/object are expected response fields only | register request `Sql.cs:83+` | design `:82`; body `:22` | Common envelope | No original/changed digest or independent state query | None specific | Exercise exact changed payload and assert constraint/readback | Registered key with one altered request field | YES | Exact constraint, unchanged request/business hashes and counts | E |
| C04 | Receipt failpoint rollback | Receipt failure rolls back business/history/receipt, then durable noncommit outcome | Named trigger has no declaration; D proof also unsafe | commit/noncommit `Sql.cs:117-146` | design `:83`; body `:23` | Common envelope/failpoint label | Trigger and rollback are not implemented fixtures | Package D; reviewed test-only failpoint support | Source-declared failpoint plus all-zero transaction deltas | Exact command with scoped receipt failpoint | YES | Exact P0001/trigger, zero transactional deltas, durable outcome | D, E |
| C05 | Explicit rollback terminalization | Exact rolled-back transaction yields durable RolledBack only after inactivity | `pg_stat_activity` visibility is non-authoritative | noncommit `Sql.cs:125-146` | design `:84`; body `:24` | Common envelope | RolledBack field can be accepted on hidden active XID | Package D | Two-session active-denial/post-rollback test | Open exact transaction held across audit attempt | YES | Active denial, no receipt, post-rollback acceptance, immutable replay | D, E |
| C06 | Interrupted attempt reconciliation | Four interruption points reconcile receipt-first without double commit | Four cases compressed; union terminal label | reconcile `Sql.cs:152` and authorizer | design `:85`; body `:25` | Common envelope/subcase labels | No exact phase or terminal per subcase | Package D where noncommit path used | Typed four-phase cases with one exact terminal each | One command per interruption boundary | YES | Phase/backend/attempt IDs, receipt precedence, no duplicate business hash | D, E |
| C07 | Concurrent command attempt | One active winner and authoritative loser | No barrier participants/winner/loser/ordinal evidence | start attempt `Sql.cs:90+` | design `:86`; body `:26` | Common envelope | 40001 label does not prove race or one active row | None specific | Two-actor barrier and exact loser assertion | One request, two separately bound actors/sessions | YES | Two execution IDs, one active attempt, ordinals and exact loser | E |
| C08 | Substituted command binding denied | Every pool/backend/transaction/actor/org/version/operation substitution denied | Variants compressed; pool/transaction/version not independently exercised | open/noncommit SQL `Sql.cs:97-146`; authorizer | design `:87`; body `:27` | Common envelope/subcase labels | No per-field call or zero-mutation readback | Package D for transaction proof | Typed substitution for every frozen field | Exact attempt plus one changed binding per subcase | YES | Exact 42501/function/constraint, zero claims/business mutation | D, E |
| G01 | Invalid purge authorization denied | Missing/expired/wrong target/batch/org denied before candidates/deletion | Five variants are labels; retry chain remains defective | purge SQL `Sql.cs:167-193` | design `:89`; body `:28` | Common envelope/subcase labels | No exact auth/start/readback for each variant | Package B | Per-variant authorization and zero-work assertions | One candidate scope plus controlled invalid authority | YES | Exact IDs/denial, zero attempt/candidate/deletion deltas | B, E |
| G02 | Genuine empty purge | Exact eligible count zero ends ZeroRows, never Succeeded | Zero is contract input, not authoritative query | start/execute purge `Sql.cs:180-193` | design `:90`; body `:29` | Common envelope with zeroRows flag | No deterministic noneligible fixture/query | Package B only if chain semantics affect start | Query exact eligibility before/after | Scope containing only noneligible rows | YES | Eligible count/hash 0, ZeroRows event, no deletion/success | B, E |
| G03 | Frozen purge candidates deleted | Exact frozen candidates deleted atomically; durable histories preserved | Generic values do not identify deletion/preservation sets | purge execute `Sql.cs:180-193` | design `:91`; body `:30` | Common envelope | No candidate IDs or relation-specific history hashes | Package B only for authorization chain | Assert candidates, deletions and each preserved history | Known temporary candidates and durable histories | YES | Nonzero candidate/deletion set hash; preserved-history hashes; success | B, E |
| G04 | Candidate drift rollback | Drift returns exact conflict and preserves candidates | No deterministic drift action or rollback query | purge execute `Sql.cs:188+` | design `:92`; body `:31` | Common envelope | Label-derived unchanged hashes do not prove drift | None beyond B | Barrier-driven drift with post-failure queries | Frozen batch plus one controlled concurrent mutation | YES | Exact 40001/object, zero deletion, candidate hashes unchanged | B, E |
| G05 | Delete failpoint failure audit | Delete fault rolls back; separate audit principal records Failed | Named trigger absent; separate commit not proven | purge execute/failure `Sql.cs:188-193` | design `:93`; body `:32` | Common envelope/failpoint label | No failpoint declaration or principal-separated sequence | Reviewed test-only failpoint; B if chain used | Assert two-principal ordering and rollback | Exact purge batch with scoped delete failpoint | YES | P0001/trigger, zero deletion, audit-principal durable Failed | B, E |
| G06 | Purge concurrency and retry | One winner; substitutions denied; one exact monotonic linked retry | Cases compressed; unused-child fresh-root bypass | purge chain `Sql.cs:167-193` | design `:94`; body `:33` | Common envelope/subcase labels | No race actors or exact chain rows | Package B | Barrier plus per-substitution and retry-chain assertions | Failed attempt, two racers and altered-policy requests | YES | Winner/loser, root/prior/evidence equality, ordinal +1, new-root denial | B, E |
| E01 | Minimized export batch | Immutable batch exactly matches approved fields/rows/as-of/expiry | Generic hashes do not prove projection or snapshot | export SQL `Sql.cs:196-204` | design `:96`; body `:34` | Common envelope | No row IDs, allowed-key inspection or authoritative batch read | None specific | Scenario-local payload/query assertions | Exact ledger rows around approved as-of boundary | YES | Auth/batch/row IDs, projected keys only, count and hashes | E |
| E02 | Prepared export immutable | Later ledger row cannot change prepared batch | No concrete later insert or same-batch reread | export SQL `Sql.cs:197-203` | design `:97`; body `:35` | Common envelope | Prepared label/hash does not prove exclusion | None specific | Insert exact later row then compare batch rows | Prepared batch plus one later eligible ledger row | YES | Later row exists; batch rows/count/hash identical; row excluded | E, E01 |
| E03 | Invalid release denied | Expired/wrong/terminal/concurrent releases denied exactly | Variants compressed into one response | release SQL `Sql.cs:202-204` | design `:98`; body `:36` | Common envelope/subcase labels | No distinct release IDs or unchanged-state proof | None specific | Per-variant release tests | Prepared batches/releases in each invalid state | YES | Exact 42501/object and unchanged batch/release hashes | E |
| E04 | Interrupted release retry | Interrupted is durable; retry uses distinct fresh release ID | Composite `InterruptedThenReleaseStarted` label | release SQL `Sql.cs:202-204` | design `:99`; body `:37` | Common envelope | One terminal string cannot prove two records | None specific | Assert old and new release rows separately | ReleaseStarted with deterministic delivery loss | YES | Old Interrupted ID/outcome, distinct new ID, same batch hash | E |
| A01 | Exact effective privilege inventory | Every ordinary/admin effective privilege equals exact matrix | Predefined-role false positives; admin excluded | both ACL verifiers | design `:101`; body `:38` | Common envelope | Verified label hides expected/actual facts | Package C | Enumerate and compare every classified principal | Exact provisioned role/membership universe | YES | Complete expected/actual matrices and zero deltas | C, external provisioning |
| A02 | Protected direct access denied | Every protected category denied for each principal | Categories are labels; admin exactness absent | both ACL verifiers | design `:102`; body `:39` | Common envelope/subcase labels | No individual permission attempt or unchanged ACL state | Package C | Principal/category matrix with exact denial | Runtime/purge/export/recovery/admin/ordinary/PUBLIC principals | YES | Exact 42501/object each; zero grants/mutations; same ACL hash | C, E |
| T01 | Controller-owned fixture | Test gets no admin credential; signed allocation/cleanup proves provenance/absence | Signed calls improved, but expected target hash is label-derived and absence unqueried | client allocation/release; control plane | design `:104`; body `:41-50` | Common envelope plus signed allocation/release | Signature authenticates controller response, not database fact | External controller/query support | Cross-check controller evidence with independent verifier | One isolated opt-in allocation | YES | Pins/lease/database/actual target hash, role checks, signed cleanup and absence | E, external controller |
| T02 | Failed cleanup survives restart | Exact failed fixture survives process boundary and reaches one terminal cleanup | “Any scenario” is not exact; no restart/readback | client/control-plane cleanup APIs | design `:105`; body `:52` | Common envelope | No selected failure, process boundary or surviving record | External restart/reconciler support | Pick one deterministic failure and assert before/after process | Named scenario failure with durable lease/attempt | YES | Pre-failure IDs, process restart, surviving row, exact cleanup outcome | A/E, external controller |
| T03 | Mutation-sensitive scenario actions | Removing intended action from any of 34 bodies makes offline mutation test fail | Mutates only T03 Action; concurrent fixtures replace frozen meta-test | inventory/client hash | design `:106`; body `:54-71` | Serialization inequality plus common/signed calls | Proves one string affects one hash, not that 34 actions are required | No production implementation; package E test architecture | Mutation-test every action/query/assertion and restore frozen meta-test | Source mutation set for all 34 contracts | NO for meta-test; YES for fixture behavior | 34 negative mutations fail; pristine offline suite passes | E |

Scenario reconciliation count: exactly **34** rows; **0 PASS, 34 FAIL**. PostgreSQL behavioral execution: **0**.

## 5. Blocking findings and required future corrections

1. Recovery event identity must permit the valid bound sequence without duplicate-event or replay ambiguity.
2. Purge chain resolution must be based on authoritative consumption/terminal state under serialization, not child-row existence.
3. ACL closure must classify predefined roles and verify administrator exactness while preserving all-owner/all-function/default/PUBLIC coverage.
4. Rollback must use an authoritative least-privilege transaction-liveness proof, not restricted statistics visibility.
5. Each scenario must own typed action, fixture, database-derived facts, durable evidence and cleanup; frozen actions and compound subcases must be restored.
6. External runtime/controller/provisioning prerequisites cannot be promoted to source PASS or fabricated.

## 6. Proposed bounded work packages (specification only)

Every package below would be implementation of a future **Correction 23** if separately authorized. This report neither creates, starts nor authorizes Correction 23.

### Package A — recovery request/event identity

- Candidate permitted files: `tools/rev869b-control-plane-install.sql`, `tools/rev869b-control-plane-verify.sql`, `tests/SESS.NexaERP.Tests/Rev869BControlPlaneProvisioningContract.cs`, `Rev869BControlPlaneRegistry.cs`, `Rev869BLifecycleControllerClient.cs`, `Rev869BCorrection16SourceContractTests.cs`, `Rev869BCorrection17SourceContractTests.cs`, and recovery/lifecycle rows in the design/scenario files.
- Frozen: application/domain/purchase code, migration identities/designers/snapshot, target command/purge/export SQL except signature-consistency references, architecture boundaries.
- Database impact: control-plane package/function/event-key semantics only; no provisioning or migration execution in source work.
- Security/audit impact: preserves decision/attempt/action authority while separating transition idempotency.
- Offline tests: pure transition/replay matrix, SQL signature/grant scan, request uniqueness and ordered-event contract tests.
- Later PostgreSQL: R01-R03 and dependent L03/L04/T02 with exact event rows and concurrency/replay.
- Rollback risk: incompatible controller API or duplicate destructive action; Down/compatibility behavior must be specified before implementation.
- Acceptance: valid sequence succeeds once; every replay/substitution fails exactly; immutable evidence remains complete.
- Prerequisites/order: design first; implement before package E integration. Recommended order 1.

### Package B — purge retry-root enforcement

- Candidate permitted files: `Rev869BCommandContextSql.cs`, `Rev869BCommandContextAuthorizer.cs` only if its contract changes, `Rev869BPurgeCoordinator.cs`, `Rev869BCorrection17SourceContractTests.cs`, and G01-G06 design/scenario rows.
- Frozen: migration identity/designer/snapshot and all business entities/services/endpoints.
- Database impact: existing REV869B raw SQL function/index/state semantics; no new migration identity without separate scope review.
- Security/audit impact: destructive authorization lineage, serialization and immutable retry evidence.
- Offline tests: state-machine matrix for absent/unused/expired/started/terminal child; concurrent-registration predicate; exact root/target/batch/outcome/evidence equality.
- Later PostgreSQL: G01-G06 barriers, one winner, new-root denial, monotonic exact retry and no duplicate deletion.
- Rollback risk: stranded chains or deadlock; define expired-unused-child recovery and lock ordering first.
- Acceptance: no new root while any failed/interrupted chain is unresolved; exactly one admissible next child; history never rewritten.
- Prerequisites/order: independent of A; recommended order 2.

### Package C — ACL-role closure

- Candidate permitted files: `Rev869BCommandContextSql.cs`, `tools/rev869b-control-plane-install.sql`, `tools/rev869b-control-plane-verify.sql`, provisioning/registry contracts, C16/C17 source-contract tests and P01/P03/A01/A02 rows.
- Frozen: application permissions/approval/business rules, migration identities/designers/snapshot, external bootstrap implementation.
- Database impact: verifier/install ACL statements and canonical inventories; external provisioning remains external.
- Security/audit impact: exact least privilege, ownership, inheritance/default/PUBLIC closure and administrator accountability.
- Offline tests: role-taxonomy fixtures including predefined aggregate roles, owners, admin, membership chains and PUBLIC; one-mutation symmetric-delta tests.
- Later PostgreSQL: P01/P03/A01/A02 on exact provisioned roles, with no repair by verifier.
- Rollback risk: an exclusion can mask privilege; an overbroad expected role can grant access. All exclusions must be named and justified.
- Acceptance: zero symmetric deltas across every requested category; every injected drift yields one exact delta.
- Prerequisites/order: provisioned-role specification before code; recommended order 3.

### Package D — rollback transaction visibility

- Candidate permitted files: `Rev869BCommandContextSql.cs`, `Rev869BCommandContextAuthorizer.cs`, C17 source contracts and C04-C06 design/scenario rows. Add no broad role membership without a separate security review.
- Frozen: B21-01 physical column ownership/types unless a separately reviewed additive witness is unavoidable; migration identity/designer/snapshot; business transaction implementation outside the narrow authorizer/SQL boundary.
- Database impact: target-local raw SQL transaction witness and grants only.
- Security/audit impact: prevents false RolledBack while retaining capability minimization and receipt-first reconciliation.
- Offline tests: formal state/predicate model and SQL source tests for active/aborted/committed/wrong identity/replay; role-capability negative tests.
- Later PostgreSQL: real two-session active/rollback/commit races using runtime and audit principals.
- Rollback risk: false status, XID reuse, lock leak/deadlock or privilege expansion; prototype must be rejected unless all are addressed.
- Acceptance: active transaction always denied; committed receipt always wins; exact aborted transaction terminalizes once; no broad stats/superuser grant.
- Prerequisites/order: approve the witness design before coding; recommended order 4.

### Package E — scenario evidence and deterministic fixtures

- Candidate permitted files: `Rev869BCorrection14PostgresDesignTests.cs`, `Rev869BCorrection17PostgresScenarios.cs`, `Rev869BCorrection17SourceContractTests.cs`, `Rev869BLifecycleControllerClient.cs`, `Rev869BTestDatabaseLease.cs`, and only narrowly required existing REV869B test fixture/coordinator files. External controller changes are prerequisites, not repository authorization.
- Frozen: production application/domain/purchase code, migration identity/designer/snapshot, architecture boundaries, and any generic production failpoint.
- Database impact: none for offline contract redesign; later reviewed test-only fixture objects require separate PostgreSQL authorization and safe teardown design.
- Security/audit impact: evidence independence, credential isolation, deterministic cleanup and no test-only production bypass.
- Offline tests: 34 typed contracts; per-subcase uniqueness; exact schema; source-declared fixture manifest; negative tests for placeholders/shared records; T03 mutation removal of every scenario action/query/assertion.
- Later PostgreSQL: exactly 34 individually authorized executions with database-derived facts and signed controller provenance.
- Rollback risk: test backdoors, leaked admin credentials, non-isolated fixtures or cleanup deletion outside owned scope.
- Acceptance: each of 34 fails if its intended action or authoritative query is removed; no label-only, zero-row-only, shared-record or missing-fixture acceptance.
- Prerequisites/order: A-D contracts and external controller/fixture specification first; recommended order 5.

## 7. Frozen files and architecture boundaries

Unless a future explicit authorization names them, all files are frozen. In particular: all application/domain/API purchase workflow files; permission and approval logic; calculation code; audit-history entities/services; migration identities, designers and `NexaErpDbContextModelSnapshot.cs`; configuration, generated files and unrelated tests/tools.

The architecture remains frozen and retained:

- provisioning is external;
- a dedicated lifecycle controller alone holds lifecycle administration;
- the control-plane database survives target disposal;
- command, purge and export ledgers remain target-local and transactional;
- tests/application code receive no lifecycle-administrator credentials;
- no alternate service, database, trust boundary or execution path is authorized.

## 8. Offline validation results

| Check/command | Exact result |
|---|---|
| `dotnet build SESS.NexaERP.slnx --no-restore --nologo` | PASS; 5 projects; 0 warnings; 0 errors. |
| Focused filter `FullyQualifiedName~Rev869B&FullyQualifiedName!~Postgres` | PASS; 71 passed, 0 failed, 0 skipped. |
| Complete filter `FullyQualifiedName!~Postgres` | PASS; 445 passed, 0 failed, 0 skipped. |
| Exact class `Rev869BCorrection17PostgresScenarios`, `--list-tests` | 34 compiled/discovered; **0 executed; NOT RUN; not behavioral evidence**. |
| PowerShell AST | Windows PowerShell 5.1.19041.6456; 24 tracked scripts; 0 parse errors; 0 helpers executed. |
| EF migrations list | PASS with `--no-build --no-connect`, inert `127.0.0.1:1`, matching dummy expected database; 13 listed; applied state unknown. No connection. |
| Migration order | PASS; one REV869A at ordinal 12, one REV869B at ordinal 13; adjacent. |
| Model/snapshot parity exact test | PASS; 1 passed, 0 failed. |
| Reviewed Correction 22 added-line secret scan | PASS; 0 matches. |
| Reviewed Correction 22 added-line privacy scan | PASS; 0 matches. |
| Reviewed Correction 22 added-line prohibited-scope scan | PASS; 0 database/role create/drop, backend termination, AWS/OIDC, EF database apply/drop or `psql` patterns. |
| `git diff --check d571a08e... 5a114cb0...` | PASS; exit 0. |
| Existing authoritative review hash | PASS; `6B81A1F8572C41F761594B55FF5EFD802DF75900761A5A69910B64B9F1C32746`. |

The first EF attempt stopped before context creation because the required variable name was not supplied; the second stopped before context creation because expected database identity was absent. The final command supplied an inert dummy string and matching expected name with `--no-connect` and listed migrations. None attempted PostgreSQL access.

No PostgreSQL test or connection, SQL generation or execution, migration apply/update/remove, database/role/schema operation, helper execution, lifecycle-controller call, recovery, purge, quarantine, export, business action, provisioning, credentials use, production/AWS/frontend/REV869C work, history rewrite, or `../legacy-reference/` access occurred.

## 9. External prerequisites still unavailable

1. Exact externally provisioned PostgreSQL cluster, surviving control plane, roles, memberships, database/schema/object/default/PUBLIC ACLs and rotated credentials.
2. Pinned cluster system identifier, endpoint, TLS/SPKI, source/package/controller manifests and target-instance provenance.
3. Independently reviewed deployed lifecycle controller/reconciler and signing keys.
4. Management writers and separately approved single-use recovery/purge/export decisions.
5. Deterministic isolated fixtures, failpoints, barriers, process restarts and independent database evidence for all 34 scenarios.
6. Separate authorization first for read-only PostgreSQL preflight and later for behavioral execution.

## 10. Report integrity, exact changes and next gate

Exactly one file is created by this reconciliation:

`outputs/rev869b_correction_22_failure_reconciliation.md`

No source, test, migration, script, SQL, configuration or generated file is changed. The final byte-for-byte report SHA-256 cannot be embedded in the file without changing that hash; it is therefore computed after finalization, verified against the committed blob, and returned with the report commit handoff.

Embedded canonical report SHA-256 (computed after replacing the 64 hexadecimal characters on this line with 64 ASCII zeroes): `F9186F38E07A6AFE648963B66B49DDB1AAAFABAA30F56E045881F724962F57EC`

Exact next authorization gate:

**Management/owner review of the REV869B Correction 22 failure reconciliation and a separate explicit decision on whether a bounded Correction 23 may be authorized.**

`rev869b_source_safety_state=FAIL`

`rev869b_execution_helper_readiness_state=FAIL`
