# REV869B pre-apply source safety re-review after Correction 17

Date: 2026-08-14 (Asia/Calcutta)

Review type: fresh independent source-only safety re-review

Reviewed source commit: `ff177328b341c535059d0fdfb49e6733335b7a03`

Parent / Correction 16 review commit: `d5ecbc42d1b52f788af709a74645866ec4a270f7`

Reviewed subject: `Correct REV869B control-plane safety checkpoint 17`

## 1. Verdict

Correction 17 adds executable control-plane artifacts, registry-first application flow, detached durable command-attempt rows, an identity sequence, a terminal-outcome table, exact target-role allowlists, separated purge principals and a minimized export API. Those are material improvements. They do not close the six Correction 16 blockers.

The provisioning helper can target an arbitrary PostgreSQL instance, does not run a meaningful preflight before mutation, mutates pre-existing roles/memberships before validating them, and verifies counts/names rather than exact definitions and effective ACLs. The control-plane API exposes a generic transition function that bypasses recovery approval and several exact-looking APIs ignore supplied bindings. Pre-create recovery and finalization are unreachable through the consumer, while post-drop/failure paths remain inconsistent. A valid purge authorization is always rejected because the audit writer is compared to an executor field fixed to the purge executor; purge/export durability still relies on caller autocommit. Business commit precedes attempt-terminal linkage without a restart-safe reconciler. The 25 corrected PostgreSQL scenarios remain substantially non-probative and several cannot pass against the committed SQL.

```text
rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
```

No PostgreSQL, provisioning, migration, lifecycle, recovery, purge, export or protected purchase action is authorized by this report.

## 2. Entry gate and exact scope

The entry gate passed before this report was created:

- HEAD was exactly `ff177328b341c535059d0fdfb49e6733335b7a03`; its parent was exactly `d5ecbc42d1b52f788af709a74645866ec4a270f7`; the subject matched exactly.
- Git returned exactly 20 committed paths, and their set reconciled exactly with checkpoint 17.
- Target-top-scoped Git status was clean.
- Git migration order contains one REV869B migration immediately after REV869A; each has its normal designer companion. EF no-connect discovery returned 13 migrations in the same order.
- The three authoritative Markdown inputs were non-empty and read completely: Correction 16 rereview 28,320 bytes / 198 lines / SHA-256 `CE753A8052F4FD8AB119C26614CD98E90FD66BB3DCCD4AE60F9A6B092613DFAD`; checkpoint 16 16,157 bytes / 120 lines / `3FDF233EB50C0D8AA2260D39A24320F63A8E0358CB650BEEBED0E9A446966314`; checkpoint 17 18,609 bytes / 137 lines / `766E8CD5A9D67D065F51BF652B0DB31AE366E131A9B39A7F260445E529AC5466`.
- The exact committed diff and all changed source consumers were inspected independently. The diff is 20 files, 1,937 insertions and 337 deletions; `git diff --check` passed.
- Repository-top metadata-only status returned exactly `?? ../legacy-reference/`. Its contents were not enumerated, read, copied, executed, staged or modified.

Exact Correction 17 path scope:

1. `outputs/rev869b_source_correction_checkpoint_17.md`
2. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs`
3. `src/SESS.NexaERP.Infrastructure/Persistence/Rev869BCommandContextAuthorizer.cs`
4. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs`
5. `tests/SESS.NexaERP.Tests/Rev869BControlPlaneProvisioningContract.cs`
6. `tests/SESS.NexaERP.Tests/Rev869BControlPlaneRegistry.cs`
7. `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs`
8. `tests/SESS.NexaERP.Tests/Rev869BCorrection16SourceContractTests.cs`
9. `tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs`
10. `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs`
11. `tests/SESS.NexaERP.Tests/Rev869BDatabaseSafetyContractTests.cs`
12. `tests/SESS.NexaERP.Tests/Rev869BOwnedPostgresDatabase.cs`
13. `tests/SESS.NexaERP.Tests/Rev869BPurchaseCorrectionTests.cs`
14. `tests/SESS.NexaERP.Tests/Rev869BPurgeCoordinator.cs`
15. `tests/SESS.NexaERP.Tests/Rev869BTestDatabaseLease.cs`
16. `tools/manage-rev869b-control-plane-secure.ps1`
17. `tools/rev869b-control-plane-bootstrap.sql`
18. `tools/rev869b-control-plane-install.sql`
19. `tools/rev869b-control-plane-rollback.sql`
20. `tools/rev869b-control-plane-verify.sql`

## 3. Six-finding disposition

| Correction 16 finding | Result | Independent disposition |
|---|---|---|
| 1. Descriptive/incomplete control-plane provisioning | FAIL / BLOCKING | Artifacts are executable, but target-instance authorization, preflight-before-mutation, exact catalogue definitions, exact effective ACLs and complete rollback are not enforceable. |
| 2. Filesystem-first lifecycle and unresolved post-DROP states | FAIL / BLOCKING | Registry is now consulted first, but approval-sensitive transitions are directly callable, pre-create recovery is illegal in the state graph, post-start failure cannot record Quarantined, and consumers never reach Finalized. |
| 3. Caller-convention purge safety and conflicting purge/FK design | FAIL / BLOCKING | Durable rows are detached from purgeable grants, but valid purge begin is rejected by a principal mismatch and transaction-independent evidence remains a caller autocommit convention. |
| 4. Bypassable durable attempts, sequence 1, missing terminal linkage | FAIL / BLOCKING | Open now requires an attempt and sequence is database-generated, but multi-attempt/retry semantics are unusable and post-commit terminal linkage can remain permanently absent. |
| 5. Direct runtime durable-ledger access | FAIL / BLOCKING | Runtime table access is revoked, but readiness does not reconstruct all effective ACLs, stale dynamic function grants remain, recovery can bypass approval through the generic transition API, and export consumption is rollback-replayable. |
| 6. Non-probative PostgreSQL scenarios | FAIL / BLOCKING | All 25 bodies exist and compile, but many assert unrelated rejection, share identical bodies, omit required fixtures/outcomes, or contradict the committed SQL. |

## 4. New findings

### C17-N01 - provisioning preflight, target identity, verification and rollback are not safe or exact - BLOCKING

Evidence:

- `tools/manage-rev869b-control-plane-secure.ps1:6,26-29,44-45` accepts an arbitrary host/port/admin user and guards only the target database name. An exact database name on a production or otherwise unexpected cluster is permitted.
- `manage-rev869b-control-plane-secure.ps1:77-87` calls a preflight that requires both `template0` and `template1` to disallow connections. A normal PostgreSQL cluster permits `template1` connections, so this is not a useful environment identity predicate.
- `manage-rev869b-control-plane-secure.ps1:92-100` does not invoke preflight in `ProvisionAuthorized`; it runs bootstrap, install and only then verification.
- `tools/rev869b-control-plane-bootstrap.sql:3-38` creates roles/database only when missing, then unconditionally grants owner memberships. Existing substituted or overprivileged roles are mutated before any exact validation.
- `tools/rev869b-control-plane-install.sql:346-405` and `tools/rev869b-control-plane-verify.sql:14-43` verify relation column counts, a total index count, function names/properties and only PUBLIC/API direct table access. They do not verify exact column names/types/defaults, constraint/index definitions, function identity signatures/result types/bodies, trigger definitions, database CONNECT allowlist, schema privileges for every role, all function grantees, inherited privileges, or arbitrary extra-role DML/EXECUTE.
- `tools/rev869b-control-plane-rollback.sql:1-31` drops the schema but not the created database, roles, memberships or CONNECT grants; it cannot undo bootstrap and refuses every lease that remains Dropped rather than Finalized.

Failure scenario: an administrator points the helper at an unexpected cluster containing the exact database name and substituted pre-existing roles. Bootstrap grants memberships and install mutates the database before verification eventually fails. Alternatively, same-count substituted columns/indexes and an arbitrary extra role with direct table/function access pass readiness.

Required correction: bind a separately authorized immutable cluster/instance fingerprint and exact host/TLS identity; make a complete non-mutating preflight mandatory inside provisioning; reject every pre-existing mismatch before any grant/DDL; verify exact definitions, signatures, bodies, owners and effective ACL closure; make provisioning failure resumable without widening privileges; and provide a complete exact rollback/removal plan for every package-owned role, membership, ACL, database and object.

### C17-N02 - lifecycle and recovery authorization/state enforcement is bypassable and incomplete - BLOCKING

Evidence:

- `tools/rev869b-control-plane-install.sql:99-141,449-450` grants the generic `rev869b_transition_database_lease` API to ordinary control-plane API/audit/recovery roles. Its allowed graph includes `Quarantined -> CleanupAuthorized`, `CleanupFailed -> CleanupAuthorized` and `DropStarted -> CleanupAuthorized`, bypassing `rev869b_consume_recovery_approval` entirely.
- `rev869b-control-plane-install.sql:230-247` accepts 22 drop parameters but selects only database/run/token/state/marker and ignores fixture, source, migration, owner, timestamps, roles, issuer, requested post-state, occurrence time and policy.
- `rev869b-control-plane-install.sql:249-264` ignores `exact_pre_state`, `occurred_at` and `policy` and lets the API report Dropped without database-side absence evidence.
- `rev869b-control-plane-install.sql:279-307` does not compare request issuer/authority, approval issuer/authority, reason, executor or supplied target fingerprint to an authoritative issuer registry/lease binding; it accepts a future `consumed_at` and merely inserts supplied approval identity strings.
- The state graph at `rev869b-control-plane-install.sql:121-128` has no `PreCreate -> CleanupAuthorized`, so advertised pre-create recovery cannot consume approval. It also has no `DropStarted -> Quarantined`, although `Rev869BTestDatabaseLease.cs:824-826` attempts exactly that after a failed DROP.
- `Rev869BTestDatabaseLease.cs:793-815` records Dropped but never performs `Dropped -> Finalized`. No consumer invokes Finalized anywhere, so rollback remains permanently ineligible after any successful lease.
- `rev869b-control-plane-install.sql:310-333` inserts a recovery outcome but never updates the recovery attempt's `OutcomeId`, `FinishedAt`, `ObservedPostState` or `Outcome`; the attempt remains `Started` forever.

Failure scenario: an API principal directly transitions a quarantined lease to CleanupAuthorized and begins cleanup without fresh approval. Separately, a normal lease drops successfully but stays Dropped forever, or a failed DROP tries an illegal Quarantined transition and leaves only DropStarted. Recovery outcome rows then disagree with permanently Started attempt rows.

Required correction: remove the generic transition API from callers and expose only purpose-specific functions; validate every supplied binding or remove it; make recovery approval issuer/authority/target/executor authoritative; add legal, exact and resumable pre-marker/post-DROP paths; atomically terminalize attempts and outcomes; and add a checked consumer path to Finalized.

### C17-N03 - purge cannot start valid work and durability still depends on caller convention - BLOCKING

Evidence:

- `Rev869BCommandContextSql.cs:337-341` stores `ExecutorPrincipal='nexa_rev869b_purge_executor'`.
- `Rev869BCommandContextSql.cs:345-357` requires the begin caller to be `nexa_rev869b_purge_audit_writer` and then compares that session user with `ExecutorPrincipal`. Every otherwise valid authorization is therefore rejected as `WrongDatabaseOrExecutor`.
- `Rev869BPurgeCoordinator.cs:43-56` uses a fresh autocommit connection, but the database function does not enforce that boundary. A credential holder can call begin inside an explicit transaction and roll back consumption/Started/ZeroRows evidence.
- `Rev869BCommandContextSql.cs:416-510` performs destructive work and terminal evidence in one caller transaction. Its PL/pgSQL exception handler preserves failure evidence only if the caller commits; a caller rollback or a failure while inserting terminal audit erases it.
- The source has no independent reconciliation API for uncertain begin/execute commit, audit-writer failure or interrupted Started executions.

Failure scenario: valid purge registration can never produce Started. If the principal mismatch is corrected alone, an executor can wrap the definer function in `BEGIN`, obtain a result, and roll back consumption/evidence; an interruption after Started has no controlled reconciler.

Required correction: bind audit-writer and executor fields separately; move consumption/attempt/terminal writes behind a trusted independently committed writer boundary that clients cannot wrap or roll back; add idempotent uncertain-commit and interruption reconciliation; and make audit-write failure itself durably distinguishable.

### C17-N04 - command attempts are not safely retryable or guaranteed terminal - BLOCKING

Evidence:

- `Rev869BCommandContextSql.cs:130-133` uses an identity sequence, but the claimed idempotency uniqueness includes always-unique `AttemptSequence`, so it prevents no duplicate idempotency key.
- `Rev869BCommandContextSql.cs:626-645` stores `BusinessCommandFingerprint` again as `IdempotencyKeyFingerprint`; no exact application idempotency key is supplied or bound, and service/ownership fingerprints remain caller-supplied environment values rather than registry-verified identities.
- `Rev869BCommandContextSql.cs:672-677` uses `INTO STRICT` for all pending attempts on a grant. A legitimate second retry attempt makes open fail; `rev869b_record_command_attempt_outcome` at `:823-832` can also select multiple pending attempts and has no exact attempt-id parameter.
- `EfRev869BPurchaseService.cs:49-57` commits the business transaction before writing the durable attempt outcome. A crash or exception at that point produces committed business state and a nonterminal attempt.
- `Rev869BCommandContextAuthorizer.cs:148-155` writes the slot terminal and then the attempt terminal as separate autocommit commands. If the second fails, retrying first fails as a replay, so terminal linkage cannot be repaired through the consumer.

Failure scenario: the business commit succeeds, terminal-link insertion fails, and the client receives an exception. The attempt remains pending, a retry cannot safely target that attempt, and a duplicate attempt either breaks STRICT selection or is ambiguously terminalized.

Required correction: pass and enforce the real idempotency key; allocate explicit per-command attempt identities; bind open and terminal outcome to one exact attempt ID; make slot terminal and attempt terminal atomically/idempotently reconcilable after commit/rollback; and expose a controlled query/reconcile path for interrupted attempts.

### C17-N05 - effective ACL and export closure is incomplete - BLOCKING

Evidence:

- The target runtime uses an explicit business-table allowlist and direct durable-ledger revokes at `Rev869BTestDatabaseLease.cs:355-365`, which closes the Correction 16 direct-table exposure.
- `Rev869BCommandContextSql.cs:841-862` dynamically grants functions when authority is provisioned but never revokes EXECUTE from previously provisioned issuer/runtime roles. Revoking the authority row does not remove effective capabilities.
- Target ACL readiness at `Rev869BCommandContextSql.cs:899-941` samples purge roles and a few owner privileges; it does not reject all missing/extra/inherited/PUBLIC/default/owner-bypass privileges across every table, sequence and function, nor does it validate export-role closure.
- Control-plane readiness likewise checks only PUBLIC/API table DML and selected memberships, while the API itself owns the approval-bypassing transition capability described in C17-N02.
- `Rev869BCommandContextSql.cs:279-311` consumes export authorization, writes export audit and returns rows in the caller transaction. An export reader can use an explicit transaction, read the data, then roll back and reuse the authorization while erasing the audit.

Failure scenario: an old runtime role retains command-context EXECUTE after authority replacement, or an export reader retrieves rows and rolls back, leaving the one-use authorization Approved with no durable audit.

Required correction: reconstruct and verify complete effective ACLs including CONNECT, schemas, tables/views/sequences/functions, membership inheritance, owners, PUBLIC and default privileges; revoke stale dynamic grants on authority rotation; remove approval-sensitive generic APIs; and place export consumption/audit in an independently committed non-rollbackable boundary before release of data.

### C17-N06 - the 25 corrected PostgreSQL scenarios remain non-probative - BLOCKING

Evidence: `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs:1-32` and `Rev869BCorrection17PostgresScenarios.cs:11-403`.

The scenario bodies compile and are discoverable, but the detailed disposition in section 10 shows identical wrappers, unrelated denials, missing valid recovery states, a purge principal contradiction, owner-fabricated fixture graphs, absent exact terminal/attempt assertions, vacuous zero-row checks and PID-only concurrency proof.

Failure scenario: source contracts pass because they search for strings while the behavioral scenarios would fail before the named boundary or pass on an unrelated rejection/empty fixture.

Required correction: build each deterministic graph through the actual least-privilege API, prove the intended boundary and exact SQLSTATE/object/state/audit, add real concurrent winners/losers and restart reconciliation, and remove all shared/vacuous/unrelated rejection paths.

## 5. Provisioning-package assessment

Static package inventory:

- Bootstrap: 4,299 bytes; 15 role-creation statements, two owner memberships, exact database create, PUBLIC CONNECT revoke and six explicit control-plane CONNECT grants.
- Install: 36,279 bytes; one schema, five tables, 13 functions, three append-only triggers, three explicit indexes plus 17 constraint-backed indexes (20 catalogue indexes total), 20 CHECK occurrences and seven REFERENCES occurrences.
- Verify: 4,187 bytes; external database/role/relation/count/membership/PUBLIC/API checks plus installed verifier invocation.
- Rollback: 2,506 bytes; 13 function drops, three trigger drops, five table drops and schema drop.

All definer functions declare fixed `search_path=pg_catalog,nexa`, object names are static or identifier-quoted, psql uses `--no-password`, and no credential is embedded. `GeneratePlanOnly` was statically proven to return before `Invoke-PsqlFile`; it was run offline and reported `PostgreSqlAccessed=false` and `ContainsCredential=false`.

The generated plan provides artifact names/hashes, not a safe authorized execution plan: it does not bind an instance identity, demonstrate a successful preflight, enumerate external credential/role activation steps, or protect against partial bootstrap. Exact definitions and ACLs are not fully verified. Provisioning source and helper readiness therefore fail.

## 6. Lifecycle and recovery state assessment

| State | Source successor(s) | Independent result |
|---|---|---|
| PreCreate | Created, Failed, Quarantined | Reservation is registry-first, but authorized pre-create cleanup is unreachable. |
| Created | Provisioned, Failed, Quarantined | Exact consumer path exists; several complete-function inputs are not enforced by the inner transition. |
| Provisioned | Executing, Failed, Quarantined | Marker required; positive path exists. |
| Executing | DropStarted, Failed, Quarantined | Normal drop begins, but requested-post/time/policy bindings are ignored. |
| Failed | Quarantined | No direct approved cleanup despite checkpoint table claiming it. |
| Quarantined | CleanupAuthorized | Direct generic transition bypasses fresh recovery approval. |
| CleanupAuthorized | DropStarted, Dropped, CleanupFailed | Outcome API can declare Dropped without database-side absence proof. |
| DropStarted | Dropped, CleanupAuthorized, CleanupFailed | Application attempts unsupported Quarantined; direct CleanupAuthorized can bypass fresh approval. |
| CleanupFailed | CleanupAuthorized | Direct generic transition bypasses approval. |
| Dropped | Finalized | Source edge exists, but no application consumer invokes it. |
| Finalized | none | Unreachable in ordinary/recovery consumers; rollback is consequently blocked after use. |

Filesystem evidence is now supplemental and registry state is read first, which is a genuine correction. It does not compensate for bypassable transitions, ignored identity/correlation fields, impossible pre-marker recovery, incomplete recovery attempt terminalization, and missing finalization.

## 7. Purge/FK/durability assessment

Positive source properties: policy is exact; expiry is capped at 15 minutes; cutoff is exactly 90 days; durable audit tables are not purge candidates and no longer reference purgeable grants; candidate count/fingerprint and bounded batch are captured; durable append-only triggers remain; no migration-time purge exists; no raw nonce/secret is stored.

Blocking results: valid begin is impossible due to audit-writer/executor mismatch; autocommit is a C# convention rather than database/trusted-writer enforcement; terminal audit failure and uncertain commits have no reconciler; and `PartialFailure` can be labelled after the PL/pgSQL subtransaction has rolled all deletion back. Success, interruption, audit failure and retry are not all durably and truthfully distinguishable.

## 8. Durable-attempt assessment

Positive source properties: ordinary open requires a durable attempt; attempts are append-only; the attempt no longer has an FK to temporary grants; identity allocates sequences beyond 1; operation slots, organization, actor, issuer, database, execution and fingerprints are captured; outcomes are in a separate immutable table.

Blocking results: actual idempotency is not bound; external instance/ownership values are not registry-verified; multiple attempts per grant break strict open/terminal selection; terminal linking is after business commit and is not restart-safe; rollback terminal commands are not atomically/idempotently linked; and no controlled reconciliation proves exactly one terminal outcome for every completed attempt.

## 9. Effective ACL/export matrix

| Principal | Intended effective access | Independent result |
|---|---|---|
| Ordinary runtime / purchase service | Exact business-table allowlist and command-context APIs; no durable tables | Direct-table closure improved and passes source inspection; complete effective/function/stale-grant readiness still fails. |
| Command issuer | Issue grant, record attempt, record rollback/terminal link; no table access | Narrow current grants, but caller fingerprints are not registry-bound and old grants are never revoked. |
| Control-plane API | Registry functions only; no direct tables | FAIL: generic transition EXECUTE bypasses approval and exact functions ignore bindings. |
| Control-plane audit writer | Transition/outcome function boundary | FAIL: generic transition is broader than append-only outcome writing. |
| Recovery administrator | Consume fresh approval and terminalize recovery | FAIL: generic transition bypass and incomplete attempt terminalization. |
| Purge authorizer | Register authorization only | Narrow grant present. |
| Purge audit writer | Consume/begin and durably record Started/Rejected/ZeroRows | FAIL: exact valid approval is rejected by executor comparison; caller rollback remains possible. |
| Purge executor | Execute one Started purge only | Direct table access revoked; terminal durability still caller-controlled. |
| Security export authorizer | Register one scoped 15-minute authorization | Narrow grant present; issuer authority itself is only a fixed role name. |
| Security export reader | Minimized organization-scoped export only | FAIL: data can be read then transaction rolled back, restoring authorization and erasing audit. |
| Security owner | NOLOGIN owner of security objects | Capability-free intent is checked, but migration-owner membership and complete object ACL closure are not independently verified. |
| Provisioning administrator | Administrative boundary only | CREATEDB/CREATEROLE plus SET on owners; not a runtime identity, but arbitrary cluster targeting and preflight gaps make the helper unsafe. |
| PUBLIC / unexpected roles | No CONNECT/schema/table/function capability | PUBLIC revokes are present, but unexpected role CONNECT/DML/EXECUTE is not comprehensively rejected. |

Temporary authorization tables are excluded from the export result and the result is organization-scoped, field-limited and row-limited. The purpose is only hashed in audit and is not bound to a controlled vocabulary or external case; more importantly, one-use/audit durability is rollbackable.

## 10. Test-by-test disposition of the 25 corrected PostgreSQL scenarios

All 25 compile and are discovered. None was executed.

| # | Scenario | Source-only disposition |
|---:|---|---|
| 1 | `ControlPlanePreMarkerEvidencePrecedesTargetMarkerDependency` | FAIL: creates a fully Executing lease; it does not interrupt or inspect the pre-marker boundary. |
| 2 | `HardInterruptionAfterEachLifecyclePhaseRemainsIdentifiable` | FAIL: delegates to the identical happy-path body as #1; no interruption matrix or next-action proof. |
| 3 | `FilesystemEvidenceAloneIsRejected` | Partial: missing control-plane configuration is rejected, but no orphan file is constructed and no reconciliation/target-absence assertion exists. Overall FAIL. |
| 4 | `ControlPlaneAndTargetMarkerMismatchIsRejected` | FAIL: mutates the lease migration fingerprint, not the actual target marker/control-plane marker relation. |
| 5 | `WrongStaleOrDuplicateRunLeaseIsRejected` | FAIL: tests one random run ID only; no stale lease, duplicate reservation, expiry or concurrency. |
| 6 | `RecoveryApprovalIssuerIsRequiredAndValidated` | FAIL: uses an Executing lease and fails on an illegal state transition; approval issuer is not independently validated by SQL. |
| 7 | `WrongRecoveryPreStateIsRejected` | FAIL: uses an impossible recovery baseline and asserts any exception, not exact SQLSTATE/object/durable rejection. |
| 8 | `ExpiredRecoveryApprovalIsRejected` | FAIL: same unrelated Executing-state setup and generic exception. |
| 9 | `ReplayedRecoveryApprovalIsRejected` | FAIL: expects the first supposedly valid consume to fail, then repeats the failure; no consumed valid approval exists. |
| 10 | `FailedRecoveryPermanentlyConsumesApproval` | FAIL: identical body to #9; no recovery attempt/failure/outcome or non-reuse proof. |
| 11 | `DurableRecoveryOutcomeIsRecorded` | FAIL: deliberately supplies a wrong post-state and asserts denial; no outcome is recorded or linked. |
| 12 | `PurgeRequiresFreshPerExecutionApproval` | Partial missing-authorization rejection only; no fresh-success contrast. Overall FAIL. |
| 13 | `WrongPurgeCutoffBatchDatabasePolicyOrExecutorIsRejected` | FAIL: mutates many fields simultaneously, omits database/policy/executor mutations and cannot attribute the rejection to each binding. |
| 14 | `ReplayedOrConcurrentPurgeApprovalIsRejected` | FAIL: uses concurrent independent connections, but valid begin is impossible due to the committed principal mismatch, so the asserted winner cannot exist. |
| 15 | `ZeroRowPurgeRecordsExactEvidence` | FAIL: valid begin is impossible; even after that fix it asserts only label/value, not complete approval/pre/post/audit state. |
| 16 | `PartialOrFailingPurgeRecordsFailureEvidence` | FAIL: owner creates an ad hoc trigger; valid begin cannot start; no exact SQLSTATE/object/rollback/audit-failure/retry evidence. |
| 17 | `PurgeCountMismatchRollsBackAndFailsClosed` | FAIL: changing ReservedAt by one second does not necessarily change candidate identity/order; valid begin cannot start and exact mismatch metadata is not asserted. |
| 18 | `PurgeOwnerInsertsAuditOnlyThroughApprovedRoute` | Partial direct-DML denial only; no approved audit-writer contrast or durable route evidence. Overall FAIL. |
| 19 | `PurgeOwnerCannotDirectlyMutateProtectedTables` | Partial single-table DELETE denial; no full protected-table/function/sequence matrix. Overall FAIL. |
| 20 | `TemporaryPurgePreservesDurablePerCommandAudit` | FAIL: uses owner-fabricated grant/security audit and asserts only security audit survival, not durable attempt/outcome linkage; valid begin cannot start. |
| 21 | `RuntimeCannotUpdateDeleteOrExportDurableCommandAudit` | Partial runtime SELECT denial on three tables, but no INSERT/UPDATE/DELETE or runtime export-function denial; authorized export uses an empty unrelated organization and does not test rollback/replay. Overall FAIL. |
| 22 | `AuditInsertionFailureBlocksProtectedCommandAcceptance` | FAIL: injects failure during grant/audit setup and performs no protected business mutation, pre/post comparison, durable attempt or terminal outcome assertion. |
| 23 | `ExactSqlStateAndDatabaseObjectAreAsserted` | Partial immutable-trigger SQLSTATE/constraint assertion as database owner; not the required least-privilege route and no pre/post/durable evidence. Overall FAIL. |
| 24 | `ZeroRowFalsePositiveIsProhibited` | FAIL: invokes the same empty-fixture zero-row body as #15 and merely asserts no Succeeded row; it does not construct a nonempty eligible fixture. |
| 25 | `ActorAndVerifierUseIndependentConnectionsAndContexts` | Partial distinct PID/identity strings only; no concurrent operation, actor result, verifier result, winner/loser or cleanup proof. Overall FAIL. |

All 50 PostgreSQL tests were compiled/listed: 18 direct, 7 application and 25 corrected. Executed count was exactly zero. PostgreSQL tests are **NOT RUN**; discovery is not behavioral evidence.

## 11. Regression review

The diff does not change domain aggregates, API endpoints, EF model/designer/snapshot or the retained REV869A/REV869B migration identity. Offline suites continue to cover PR -> RFQ -> vendor quote -> comparison -> PO -> material follow-up; organization/parent scoping; vendor qualification; technical/commercial segregation; manager/TD/MD approvals; GST/commercial calculations; optimistic concurrency/idempotency; revision/resubmission/issue/amendment/cancellation; immutable histories/snapshots; URL/permission/attachment/masking/export/denial auditing; REV869A UOM/Department Manager foundations; migration order; and model parity.

However, `EfRev869BPurchaseService.cs:49-57` can commit business state and then throw if attempt terminalization fails. That creates a false-failure/retry ambiguity and is a REQUIRED_CORRECTION regression to reliable idempotent command completion. Offline tests do not exercise this PostgreSQL durability window. No PostgreSQL preservation claim is made.

## 12. Reconciled offline validation

| Validation | Independent result |
|---|---|
| Build, `--no-restore --nologo` | PASS; 0 warnings, 0 errors |
| Focused Correction 16/17 + purchase contracts | PASS; 24/24 |
| Inclusive REV869B excluding PostgreSQL names | PASS; 79/79 |
| Complete suite excluding `Postgres` and `PostgreSql` names | PASS; 453/453 |
| Exact three REV869B PostgreSQL classes, list-tests only | 50 discovered: 18 direct, 7 application, 25 corrected; 0 executed; NOT RUN |
| PowerShell 5.1 AST | PASS; 24 files, 0 errors; version 5.1.19041.6456 |
| EF migrations list | PASS with `--no-connect`, inert `127.0.0.1:1`; 13; applied state unknown |
| Migration uniqueness/order | PASS; one REV869B immediately after REV869A |
| Model/designer/snapshot parity | PASS; 1/1 no-connect test |
| GeneratePlanOnly | PASS as offline operation; PostgreSQLAccessed=false; ContainsCredential=false |
| Provisioning manifest verification | FAIL source adequacy; counts/hashes reproduce but exact definitions/effective ACLs do not |
| Effective ACL/export contract | FAIL; C17-N02/N05 |
| Lifecycle completeness | FAIL; C17-N02 |
| Purge/FK/retention contract | FK separation/retention positive; executable durability FAIL; C17-N03 |
| Attempt sequence/terminal link | Identity sequence positive; retry/terminal guarantee FAIL; C17-N04 |
| Secret/privacy scan | No private key, bearer literal, assigned client secret or embedded password; sole hit was a test assertion forbidding `BEGIN PRIVATE KEY` |
| Prohibited generated SQL | 0 CREATE DATABASE; 0 DROP DATABASE; 0 `pg_terminate_backend` |
| Migration-source prohibited operations | 0 CREATE/DROP DATABASE, 0 terminate, 0 purge invocation |
| Exact committed diff | 20 files; 1,937 insertions; 337 deletions |
| `git diff --check` | PASS |
| Target status before report | clean |

Offline SQL generation used only REV869A -> REV869B and REV869B -> REV869A with an inert loopback port-1 design identity. No SQL was applied or parsed/executed by PostgreSQL. Both exact temporary SQL files were removed after measurement.

## 13. SQL sizes, hashes and inventories

| Artifact | Bytes | SHA-256 |
|---|---:|---|
| Offline REV869A-to-REV869B Up SQL | 279,710 | `C10D173C6F98BD648354692D377A56067D1C4183D56EC5CEFDC30D841BF880FD` |
| Offline REV869B-to-REV869A Down SQL | 11,133 | `589D7A0DD2448C63B836C45618832855EF5F289E3EE3F0DC993B351A28066841` |
| Control-plane bootstrap | 4,299 | `23B7934BF70C217EBD695B2D9F7D97BD2A1B2806A6BA81EA24E65E67FFD9BA91` |
| Control-plane install | 36,279 | `6A44F1EB7E3742A89367ACF1F58D2AFA6138780E864A5D30E739A88CF98182BC` |
| Control-plane verify | 4,187 | `7D4F9B939E6994CF538567B11BC7DFE8C9B0C94D7D4373BBE59D68DCA6BAEB7C` |
| Control-plane rollback | 2,506 | `77CFDE8032FA77BC53C8CA6C6C9236B16473C6F81571E1D7FFC8E21C2CEEC45D` |

Up SQL inventory: 27 tables; 75 trigger occurrences; 36 function definitions / 35 distinct names; 47 FOREIGN KEY clauses / 50 REFERENCES tokens; 72 indexes; 87 CHECK occurrences.

Down SQL inventory: 9 DROP TRIGGER statements; 1 generated function definition; 70 DROP lines.

Control-plane install source inventory: 5 tables; 13 functions; 3 triggers; 3 explicit plus 17 constraint-backed indexes; 20 CHECK occurrences; 7 REFERENCES occurrences. The counts reproduce checkpoint 17 but do not establish exact catalogue equivalence.

## 14. External execution prerequisites

External prerequisites remain closed and unexecuted:

1. Correction 18 source that closes every blocking/required item in this report.
2. A separately governed immutable PostgreSQL cluster identity and TLS/host allowlist.
3. Exact capability-free roles and credentials provisioned only after complete non-mutating preflight.
4. Protected issuer, recovery, purge, export, nonce, execution/service and ownership identities bound to the authoritative registry.
5. Independently reviewed, transaction-independent audit writer/reconciliation services for lifecycle, attempts, purge and export.
6. A future separately authorized source-only review of the isolated provisioning/execution plan; PostgreSQL execution is not the next gate.

None was provisioned, accessed or assumed as evidence.

## 15. Prohibited operations and exact next gate

No PostgreSQL connection or PostgreSQL test was made. No migration was applied or removed. No database was created, dropped, restored, quarantined, repaired or altered. No role, schema, control-plane object, issuer, key or credential was provisioned. No purge, recovery, export or purchase action ran. No production, REV861, AWS, production OIDC, frontend, Docker, legacy application or REV869C resource was accessed. No execution/provisioning command is supplied. `../legacy-reference/` remained untracked, unread and untouched.

Exactly one next gate: **Correction 18 source-only**. Correct every BLOCKING and REQUIRED_CORRECTION item above, reproduce the offline gates, commit only controlled source/checkpoint changes, and request a fresh independent source-only safety re-review. PostgreSQL execution is not authorized.
