# REV869B source correction checkpoint 20

Date: 2026-08-14 (Asia/Calcutta)

Starting commit: `3234622e886a5fde3d90fe2cf98f7cedceb6fbef`

Authority: `outputs/rev869b_correction19_failure_reconciliation.md`, SHA-256 `EFFC832185DC4FE47E33A668E2693631BDEB3FBC5246746EAEE9B81957CBF6FD`

Entry decisions retained:

```text
correction_20_source_only_gate=GO
frozen_architecture_state=RETAIN
external_prerequisite_blocking_state=YES
```

This checkpoint records a bounded source-only implementation. External provisioning, the dedicated lifecycle controller, the surviving control-plane database, and target-local transactional ledgers remain separate trust boundaries. No PostgreSQL connection, helper/provisioning execution, migration application/removal, purge, recovery, quarantine, export, production, AWS, OIDC, frontend, REV861, REV869C or independent source-safety review was performed.

## 1. Bounded implementation inventory

The Correction 20 commit contains only these 15 files:

1. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs`
2. `src/SESS.NexaERP.Infrastructure/Persistence/Rev869BCommandContextAuthorizer.cs`
3. `tests/SESS.NexaERP.Tests/Rev869BControlPlaneProvisioningContract.cs`
4. `tests/SESS.NexaERP.Tests/Rev869BControlPlaneRegistry.cs`
5. `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs`
6. `tests/SESS.NexaERP.Tests/Rev869BCorrection16SourceContractTests.cs`
7. `tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs`
8. `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs`
9. `tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs`
10. `tests/SESS.NexaERP.Tests/Rev869BPurgeCoordinator.cs`
11. `tests/SESS.NexaERP.Tests/Rev869BTestDatabaseLease.cs`
12. `tools/rev869b-control-plane-install.sql`
13. `tools/rev869b-control-plane-preflight.sql`
14. `tools/rev869b-control-plane-verify.sql`
15. `outputs/rev869b_source_correction_checkpoint_20.md`

No migration identity, designer, snapshot, business entity, Purchase endpoint/service, helper, provisioning artifact or frontend file changed. REV869B remains the single migration immediately after REV869A.

## 2. Six-blocker reconciliation

| Blocker | Exact implementation evidence | Exact source/test evidence |
|---|---|---|
| R19-N01: incomplete control-plane canonical verification | Preflight now uses symmetric `expected_roles`/`actual_roles` differences over every `nexa_rev869b_%` role and rejects capability/membership drift. Install records `rev869b_control_plane_catalogue_fingerprint()`, covering relation/column/default/constraint/index/trigger/function-body/owner/ACL/schema/default-ACL facts. Verify compares the stored catalogue digest, symmetric object/function/effective-execute sets, arbitrary ordinary grantees, PUBLIC, database, schema, relation, sequence, membership and default-ACL-derived catalogue facts. | `CanonicalVerifierComparesCompleteObjectAndEffectiveAclSets`, `ExactInventoryModelRejectsAddedRemovedChangedAndDuplicateFacts`, and `PreflightRejectsUnexpectedPackageRolesAndWrongCapabilities` parse the required dimensions and prove pure exact-set rejection for an added, removed, changed or duplicate fact. |
| R19-N02: unreachable quarantine and under-bound recovery/replay | Purpose-only `rev869b_record_quarantine` realizes Reserved/Provisioning/Ready/InUse to Quarantined and terminalizes an active attempt. Recovery drop joins the consumed decision and permits only `DropAndFinalize`; direct finalization permits only `FinalizeAbsent`. Lifecycle versus recovery callers are restricted to DropAuthorized versus RecoveryAuthorized. Cleanup failure and finalization return the existing outcome only for identical evidence and reject mismatches. | `QuarantineRecoveryActionAndTerminalReplayAreDatabaseBound` checks every purpose path, exact action join, caller/prestate split and replay branch. The pure lifecycle model checks the frozen graph, exact recovery action and idempotent finalization. |
| R19-N03: under-bound command terminal ownership/replay | `rev869b_record_noncommit_outcome` now requires attempt, execution instance, service fingerprint, ownership fingerprint, state, category and deterministic outcome ID. It checks the exact active attempt, absence of a receipt, category-specific no-open/no-active-backend transaction evidence through `pg_stat_activity.backend_xid`, identical replay equality, mismatch rejection and exact-row deactivation. C# carries the original bindings and deterministic outcome ID to the independent audit connection. | `CommitIsInsideBusinessTransactionAndNoncommitUsesAuditPrincipal` parses every binding, authoritative no-commit predicate, mismatch constraints and absence of a terminal `ON CONFLICT ... DO NOTHING` shortcut. C01-C08 are separate executable future behavior bodies. |
| R19-N04: unenforced purge scope/retry and non-independent failure writer | Scope grammar is exactly `organization:<identifier>`; candidate selection joins request organization to the parsed authorization. `PriorAttemptId` has a real FK and registration requires a Failed/Interrupted prior attempt with identical scope, cutoff and maximum. Start/execute remain purge-worker operations; failure/reconciliation use the independently provisioned `nexa_rev869b_purge_audit` principal and separate connection. | `PurgeFreezesCandidatesAndHasNoRetryEligibleState` parses the scope predicate, candidate organization join, FK, prior terminal/policy binding and grant separation. G01-G06 are separate executable future behavior bodies. |
| R19-N05: export minimization/release and target ACL closure | Batch payload uses `jsonb_each` plus `field.key=ANY(authorization.Fields)`, preserving row/organization/as-of/maximum/expiry scope and hashing the minimized payload. A partial unique index permits one ReleaseStarted row; authorization/read require an unexpired batch and a new release only after Failed/Interrupted. `rev869b_target_catalogue_fingerprint` and `rev869b_verify_target_catalogue_acl` cover target definitions, function bodies/owners/ACLs, ordinary arbitrary grantees, PUBLIC, database/schema/relation/sequence/function/default ACLs and membership. | `ExportMaterializesImmutableRowsBeforeAuditedRelease` parses the field projection, expiry, one-active-release rule, retry states and target catalogue verifier. E01-E04 and A01-A02 are separate executable future behavior bodies. |
| R19-N06: label-only acceptance matrix | `Rev869BAcceptanceScenarioInventory` defines exactly 34 unique typed contracts. `Rev869BCorrection17PostgresScenarios` contains exactly 34 independently discoverable `[Fact]` bodies calling the HTTPS lifecycle controller. Evidence is bound to run/lease/attempt/required decision/fixture IDs, source/manifest/TLS/cluster identity, exact setup/action/prestate/poststate/result or SQLSTATE/object, affected rows, fixture/state/action/durable/cleanup digests, unrelated-mutation zero, and target/role cleanup. Only G02 permits zero action rows, with a positive prepared fixture and explicit ZeroRows terminal state. | `AcceptanceInventoryHasExactlyThirtyFourUniqueExecutablePostgresFacts` proves the exact ordered ID set, per-contract completeness, 34 executable calls and absence of source-label bodies/generic exception assertions. Discovery found all 34; none was executed. |

## 3. Exact 34-scenario acceptance inventory

Every row below maps to the same-named method in `Rev869BCorrection17PostgresScenarios` and the exact typed contract in `Rev869BAcceptanceScenarioInventory`.

| ID | Exact setup -> action | Required result |
|---|---|---|
| P01 | externally provisioned exact cluster/control plane -> canonical read-only verifier | ExternalProvisioned -> ExternalVerified; 1 affected fact |
| P02 | mismatched source/TLS manifest -> external preflight | exact 42501 on `rev869b_external_manifest`; PreflightDenied |
| P03 | changed definition/effective grant -> canonical verifier | exact 42501 on `rev869b_control_plane_catalogue_acl`; VerificationDenied |
| L01 | Reserved disposable lease -> controller provisioning | Ready; durable nonzero result |
| L02 | interruption at each create phase -> restart reconciliation | Provisioning -> Ready |
| L03 | active lifecycle attempt/barrier -> concurrent start | exact 40001 on `UX_rev869b_one_active_lifecycle_attempt` |
| L04 | DropAuthorized lease -> drop exact target/roles | Finalized |
| L05 | Ready target with identity/catalogue mismatch -> use/drop check and quarantine | exact 42501 on `rev869b_target_identity_mismatch`; Quarantined |
| R01 | Quarantined lease + fresh decision -> consume exact action/recover | decision-bound Finalized |
| R02 | consumed decision -> same/changed action replay | exact 42501 on `rev869b_recovery_decision_replay` |
| R03 | CleanupFailed + fresh linked decision -> recover | decision-bound Finalized |
| C01 | registered request + exact runtime transaction -> business/receipt/outcome commit | Committed; nonzero persistence |
| C02 | committed command/lost response -> same request replay | authoritative Committed receipt |
| C03 | same key/different request digest -> replay | exact 23505 on `rev869b_command_request_replay_mismatch` |
| C04 | receipt insertion failpoint -> attempted business commit | exact P0001 on `rev869b_command_receipt`; RolledBack |
| C05 | opened transaction -> rollback + exact terminal outcome | RolledBack; durable outcome |
| C06 | interruptions before/after open, during commit, after response -> restart reconciler | exact AttemptStarted -> Abandoned contract |
| C07 | concurrent differently bound attempts -> second start | exact 40001 on `UX_rev869b_one_active_command_attempt` |
| C08 | substituted backend/actor/org/role/operation -> open/terminalize | exact 42501 on `rev869b_attempt_binding` |
| G01 | missing/expired/wrong-org authorization -> start purge | exact 42501 on `rev869b_purge_authorization_scope` |
| G02 | scoped authorization + no eligible rows -> freeze candidates | explicit ZeroRows; only authorized zero-row case |
| G03 | scoped eligible temporary contexts + durable histories -> delete frozen candidates | Succeeded; exact nonzero deletion/audit |
| G04 | Started + candidate drift -> execute | exact 40001 on `rev869b_purge_candidate_drift`; Failed |
| G05 | deterministic delete/audit fault -> rollback then independent failure record | exact P0001 on `rev869b_purge_delete_failpoint`; Failed |
| G06 | concurrent start/execute + prior failure -> race/new linked authorization | exact 40001 on `rev869b_purge_retry_binding` |
| E01 | approved org/fields/rows/as-of/expiry -> prepare | immutable minimized Prepared batch |
| E02 | Prepared batch -> insert later ledger row/reread | unchanged Prepared digest/rows |
| E03 | expired/wrong-terminal/concurrent release -> read/authorize | exact 42501 on `rev869b_export_release_sequence` |
| E04 | ReleaseStarted + delivery loss -> Interrupted/new release ID | decision-bound Interrupted evidence and sequenced retry |
| A01 | canonical control/target packages -> enumerate effective privileges | Installed -> Verified |
| A02 | each package/arbitrary ordinary principal -> protected direct/ungranted call | exact 42501 on `rev869b_protected_object_acl` |
| T01 | exact isolated opt-in -> controller allocation | Reserved -> InUse with prepared fixture |
| T02 | deterministic scenario failure -> dispose/restart cleanup | CleanupFailed -> Finalized |
| T03 | two controller fixtures/barriers -> concurrent actors/isolation/cleanup | InUse -> Finalized; zero unrelated mutation |

For all 34 rows the client additionally requires exact source/manifest/TLS/cluster binding, positive fixture count, before/after counts, action and cleanup SHA-256 evidence, at least one durable evidence row, zero unrelated mutations, and finalized target/role cleanup. Denial rows cannot pass with a generic exception; successful mutation rows cannot pass with zero affected rows; G02 cannot pass without a real nonempty fixture.

## 4. Offline validation

| Validation | Result |
|---|---|
| Solution build | PASS; 5 projects; 0 warnings; 0 errors |
| Focused REV869B tests excluding `Postgres`/`PostgreSql` | PASS; 66 passed, 0 failed, 0 skipped |
| Complete suite excluding `Postgres`/`PostgreSql` | PASS; 440 passed, 0 failed, 0 skipped |
| Explicit no-connect model/snapshot parity | PASS; 1 passed, 0 failed, 0 skipped |
| PostgreSQL test compilation/discovery only | 88 PostgreSQL/PostgreSql-named tests overall; 60 REV869B-named; exactly 34 Correction 20 matrix tests; **0 executed** |
| PowerShell 5.1 AST | PASS; version 5.1.19041.6456; 24 files; 0 parse errors; no helper executed |
| EF migration discovery | PASS with `--no-connect` and inert `127.0.0.1:1`; 13 migrations; applied state intentionally unknown |
| REV869 ordering/uniqueness | PASS; REV869A ordinal 12, REV869B ordinal 13; each exactly once and adjacent |
| Offline REV869A -> REV869B Up SQL, `--no-transactions` | 255,932 bytes; 2,264 lines; SHA-256 `BF59E6B096315342498C483F669A7C92DA61F73B29C73C79902F2EC1B7347799` |
| Offline REV869B -> REV869A Down SQL, `--no-transactions` | 10,225 bytes; 214 lines; SHA-256 `DF43E23C0AFC77E5AB91AEB06462109394756EB2A7E2840208F438AD5338B297` |
| Generated SQL prohibited operations | 0 CREATE DATABASE; 0 DROP DATABASE; 0 `pg_terminate_backend` |
| Temporary SQL artifacts | removed; 0 remain |
| Changed executable source/test/tool secret scan | 0 password/client-secret/private-key/access-token/bearer assignments |
| Changed executable source/test/tool privacy scan | 0 DOB/payroll/bank/government-ID/private-employee literals |
| Changed executable source/test/tool prohibited scope scan | 0 database/role create/drop, AWS or OIDC operations |
| `git diff --check` | PASS |

Offline generation and source tests do not prove PostgreSQL syntax or behavior. PostgreSQL access and all PostgreSQL test execution remained unauthorized; discovery/listing executed no test body.

## 5. Purchase and frozen-architecture preservation

No Purchase source, migration identity, designer or snapshot changed. The full 440-test non-PostgreSQL suite preserves the previously approved Purchase workflow, endpoint/record permissions, approval thresholds, GST calculation/provenance, histories, segregation and REV869A data/model contracts. Model/snapshot parity remains exact.

The architecture remains:

1. external IaC owns cluster/database/role provisioning;
2. the dedicated lifecycle controller owns target creation/drop orchestration;
3. the surviving control-plane database owns durable lifecycle/recovery evidence;
4. the target owns transactional command, purge and export ledgers.

No boundary was merged and no competing design was introduced.

## 6. Remaining external prerequisites and explicit nonclaims

The following remain unmet and external:

1. External IaC provision of the exact NOINHERIT capability-minimized control-plane and target roles, including `nexa_rev869b_purge_audit`, closed memberships/default privileges and rotated credentials.
2. A pinned isolated PostgreSQL system identifier, endpoint, TLS/SPKI, environment and exact source/package manifest.
3. External lifecycle-administrator installation of the reviewed control-plane package in the surviving database.
4. Deployment of the dedicated lifecycle controller/reconciler and management approval writer outside application/test processes.
5. Controller implementation of the exact 34 contracts, deterministic isolated fixtures, failpoints, restart/barrier controls and bound evidence.
6. A fresh independent source-only safety rereview of this exact Correction 20 commit and parent.
7. Separate authorization before any read-only PostgreSQL verification or behavioral acceptance.

Explicit nonclaims:

- REV869B source safety: unclaimed; this checkpoint is not an independent review.
- Execution-helper/control-plane operational readiness: unclaimed and externally blocked.
- PostgreSQL behavioral acceptance: unclaimed; 0 PostgreSQL tests executed.
- Provisioning, migration apply/remove, lifecycle, recovery, quarantine, purge, export and production acceptance: unclaimed.
- Exactly-once network delivery: not claimed; durable release attempts and honest outcomes remain the frozen contract.

Exact next gate: after committing this bounded Correction 20 source set, perform a fresh independent source-only safety rereview of the exact Correction 20 commit and its parent. Do not access PostgreSQL or implement another correction in this task.
