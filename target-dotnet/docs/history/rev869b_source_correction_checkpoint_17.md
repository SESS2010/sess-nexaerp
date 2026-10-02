# REV869B Source Correction Checkpoint 17

Date: 2026-08-14
Authoritative starting HEAD: `d5ecbc42d1b52f788af709a74645866ec4a270f7`
Reviewed Correction 16 source commit: `85c2a05d1d392b3699997226ec591a9f218d3212`
Authoritative review SHA-256: `CE753A8052F4FD8AB119C26614CD98E90FD66BB3DCCD4AE60F9A6B092613DFAD`
Required subject: `Correct REV869B control-plane safety checkpoint 17`
Ending commit: the commit containing this checkpoint.

## Entry gate and scope

The required HEAD, reviewed source parent and review hash matched. The target-scoped worktree was clean, with exactly the independent Correction 16 review at HEAD. EF no-connect discovery returned 13 migrations and exactly one REV869B immediately after REV869A. The sibling `../legacy-reference/` remained the only untracked sibling path; its contents were not enumerated, read or modified. The complete Correction 16 checkpoint, complete independent review and committed Correction 16 diff were read before changes. No PostgreSQL or external system was accessed.

Correction 16 was insufficient because its control plane was descriptive, filesystem evidence could select recovery, post-DROP ambiguity was not reconcilable, purge evidence could be rolled back and conflicted with grant deletion, durable attempts were optional/incomplete, runtime could reach the new ledger, and the 25 corrected designs did not reach their named paths.

## Finding matrix

| Finding | Exact affected files/methods | Root cause | Required correction | Database enforcement | Application enforcement | Positive test | Adversarial test | Rollback evidence | Completion evidence |
|---|---|---|---|---|---|---|---|---|---|
| C16-N01 | `Rev869BControlPlaneProvisioningContract.GeneratePlan/ExactReadinessSql`; `Rev869BControlPlaneRegistry.VerifyProvisioningAsync` | Names/signatures substituted for reproducible topology and effective ACL verification. | Commit executable bootstrap/install/verify/rollback and exact readiness. | Exact database guard; roles, memberships, five relations, columns, constraints, 18 indexes, functions, triggers, owners, grants, default ACLs and unexpected-object/grant rejection. | Registry uses the installed verifier; helper exposes only explicit modes and authorization references. | GeneratePlanOnly inventory and source-contract topology assertions. | Wrong target, unexpected object/grant, missing column/index/membership and unsafe rollback fail closed. | Rollback refuses non-finalized leases and removes only the exact package. | Five executable artifacts plus exact installed/external verifier source. |
| C16-N02 | `Rev869BTestDatabaseLease.CreateAsync/RecoverAsync/DisposeAsync`; registry read/transition APIs | Filesystem-first selection and no durable same-attempt post-DROP reconciliation. | Make registry state/attempt authoritative and lifecycle resumable. | Durable states/events, nullable pre-marker time, exact transition CAS, DropStarted attempt read and target-absence finalization. | Reserve before file evidence; file only corroborates; recovery reads registry first and reconciles absent targets. | Lifecycle trace and durable recovery-outcome scenarios. | Filesystem-only, mutated marker, stale/duplicate run, wrong issuer/state/expiry/replay and interruption scenarios. | Same DropStarted attempt is resumed; surviving control plane records Dropped/CleanupFailed. | Registry-first source assertions and 11-state transition topology. |
| C16-N03 | `Rev869BCommandContextSql` purge functions/tables; `Rev869BPurgeCoordinator` | Caller transaction could roll back authorization/evidence; eligibility diverged; durable attempt FK blocked purge. | Separate committed authorization/audit boundary, align states and detach durable evidence. | Distinct authorizer/audit-writer/executor functions; exact approval bindings; immutable before/after/terminal evidence; eligible states `Expired,Committed,Failed,Rejected`; no grant FK from durable attempts. | Fresh non-pooled role-specific autocommit connections and mandatory post-result verification. | Authorized zero-row and durable-preservation scenarios. | Replay/concurrency, binding substitutions, trigger failure, candidate drift/count mismatch and direct DML denial. | Destructive subtransaction rolls back while outer durable failure evidence commits; retry requires new authorization. | SQL/source contracts and future scenario bodies cover all paths. |
| C16-N04 | `rev869b_record_command_consumption_attempt`, context open/claim, `Rev869BCommandContextAuthorizer`, purchase service, direct test helper | Attempt was optional, sequence constant, bindings incomplete and no immutable terminal linkage. | Require a database-allocated attempt bound to the exact command and append terminal outcome. | Identity sequence; exact grant/organization/operation/target/actor/version/transition/correlation/idempotency bindings; strict matching attempt FK; immutable one-to-one outcome. | Authorizer/direct helper records attempt before open; service records terminal after business commit; rollback/failure records terminal through issuer connection. | Protected command and terminal-link scenarios. | Bypass, substitution, replay, pooled-connection and audit-failure scenarios. | Terminal outcome is written on a separate issuer boundary after commit/rollback and does not depend on purgeable grants. | Mandatory `INTO STRICT`, attempt FK, identity and outcome source assertions. |
| C16-N05 | Runtime grants in `Rev869BTestDatabaseLease`; migration ACL/ownership/export SQL | Broad ALL TABLES grant exposed durable ledgers and no governed export existed. | Exact allowlist, capability-separated ownership/writers and minimized approved export. | Security owner; runtime/issuer/purge-audit/export-authorizer/export-reader separation; all direct ledger privileges revoked; approval-bound 15-minute minimized export with immutable audit. | Exact per-role connection selection; no general ledger reader path. | Authorized minimized export plus audit scenario. | Runtime read/write/export denial and purge-role direct DML denial. | Export authorization is one-use; durable audits are immutable and outside temporary-grant purge FKs. | ACL/export source assertions and explicit runtime grant allowlist. |
| C16-N06 | 25 facts in `Rev869BCorrection14PostgresDesignTests` and new `Rev869BCorrection17PostgresScenarios` | Synthetic/vacuous setup did not create approvals, candidates, concurrency or supported faults. | Build exact future PostgreSQL scenario bodies using authorized APIs and independent principals. | Scenarios target exact functions, SQLSTATE/object, durable rows and backend identities. | Owned database fixture exposes distinct role connections and deterministic fixtures/barriers/faults. | Each named success path has exact state and durable evidence assertions. | Each denial/fault/replay/concurrency path asserts exact rejection and before/after state. | Recovery/purge scenarios assert same-attempt finalization and durable failure evidence. | 25 facts compile and discover; execution is explicitly NOT RUN. |

## Executable provisioning inventory

- `tools/manage-rev869b-control-plane-secure.ps1`: GeneratePlanOnly, PreflightOnly, ProvisionAuthorized, PostProvisionVerification and RollbackAuthorized; exact target; `--no-password`; sanitized hashes; separate authorization references.
- `tools/rev869b-control-plane-bootstrap.sql`: exact database/role creation, PUBLIC CONNECT revocation and capability-free memberships.
- `tools/rev869b-control-plane-install.sql`: five control-plane relations, 13 APIs, append-only triggers, exact owners/ACL/default privileges, 20 indexes and installed exact verifier.
- `tools/rev869b-control-plane-verify.sql`: independent exact topology, column-count, index, role, membership and effective-privilege verification.
- `tools/rev869b-control-plane-rollback.sql`: exact target guard and refusal while any lease is non-finalized.

Final GeneratePlanOnly evidence reported `PostgreSqlAccessed=false`, `ContainsCredential=false`, host fingerprint `88B79F306981AD52D23E203FC3EA55EA89217903F638DB55063F17AEA55B83A3`, and:

| Artifact | Bytes | SHA-256 |
|---|---:|---|
| Bootstrap | 4,299 | `23B7934BF70C217EBD695B2D9F7D97BD2A1B2806A6BA81EA24E65E67FFD9BA91` |
| Install | 36,279 | `6A44F1EB7E3742A89367ACF1F58D2AFA6138780E864A5D30E739A88CF98182BC` |
| Verify | 4,187 | `7D4F9B939E6994CF538567B11BC7DFE8C9B0C94D7D4373BBE59D68DCA6BAEB7C` |
| Rollback | 2,506 | `77CFDE8032FA77BC53C8CA6C6C9236B16473C6F81571E1D7FFC8E21C2CEEC45D` |

## Lifecycle and recovery contract

| State | Authorized successor(s) | Durable boundary |
|---|---|---|
| PreCreate | Created, Failed, Quarantined | Reservation exists before supplemental file or target. |
| Created | Provisioned, Failed, Quarantined | Database exists; marker may not. |
| Provisioned | Executing, Failed, Quarantined | Exact marker binding recorded. |
| Executing | Failed, Quarantined, CleanupAuthorized | Test execution is registry-visible. |
| Failed | Quarantined, CleanupAuthorized | Failure remains recoverable. |
| Quarantined | CleanupAuthorized | Recovery approval is required. |
| CleanupAuthorized | DropStarted | Exact approval is consumed. |
| DropStarted | Dropped, CleanupFailed | Attempt ID is stable and resumable after target loss. |
| CleanupFailed | CleanupAuthorized, DropStarted | Explicitly approved retry only. |
| Dropped | Finalized | Absence is reconciled in the surviving control plane. |
| Finalized | none | Rollback eligibility boundary. |

Registry evidence selects the path. Filesystem evidence can corroborate but cannot authorize or block recovery. Pre-marker timestamps are nullable. Reservation acknowledgement, roles-only/database-without-marker, marker mismatch, target-absent DropStarted and post-DROP outcome ambiguity have deterministic registry entry points.

## Purge, attempts, roles and retention

Purge policy is exactly `MGMT-REV869B-SECURITY-LEDGER-20260813-001`: authorization is at most 15 minutes; eligible temporary metadata states are exactly Expired, Committed, Failed and Rejected; the management-approved cutoff is 90 days; minimized durable audit retention remains at least ten years. No migration-time purge occurs.

Purge authorization, Started/Rejected/ZeroRows evidence and destructive execution use distinct authorizer, audit-writer and executor principals. Exact cutoff, batch, database, policy, executor, nonce, maximum rows, candidate count and candidate fingerprint are bound. Durable command-attempt/outcome evidence has no FK to purgeable grants.

Attempt identity is generated by the database. Open requires one exact unconsumed attempt bound to database/execution/service/ownership, organization, issuer/actor/authorization, operation slot/target/pre-state/version/transition, correlation and idempotency fingerprints. Immutable terminal outcome links the attempt to the final business fingerprint after commit or rollback.

| Principal | Direct durable-ledger table access | Executable boundary |
|---|---|---|
| Runtime | none | Exact business/context functions only. |
| Command issuer | none | Issue grant, record attempt and terminal outcome. |
| Purge authorizer | none | Register one purge authorization. |
| Purge audit writer | none | Begin/consume authorization and record durable audit. |
| Purge executor | none | Execute exact started purge. |
| Security export authorizer | none | Register exact one-use minimized export authorization. |
| Security export reader | none | Execute approved minimized export only. |
| Security owner | owns security objects; NOLOGIN/capability-free | Owns definer functions and immutable ledgers. |

The export allowlist excludes raw identity and secret material, binds organization/purpose/fields/row limit/nonce/expiry, consumes once, and appends an immutable export audit.

## Test-by-test future PostgreSQL disposition

These are corrected source designs only; none was executed.

| # | Scenario | Correction 17 source disposition |
|---:|---|---|
| 1 | ControlPlanePreMarkerEvidencePrecedesTargetMarkerDependency | Registry lifecycle trace includes PreCreate before marker. |
| 2 | HardInterruptionAfterEachLifecyclePhaseRemainsIdentifiable | Each durable phase is read through control-plane transitions. |
| 3 | FilesystemEvidenceAloneIsRejected | Orphan file is insufficient without registry lease. |
| 4 | ControlPlaneAndTargetMarkerMismatchIsRejected | Actual marker/control-plane mutation is rejected. |
| 5 | WrongStaleOrDuplicateRunLeaseIsRejected | Wrong and replayed lease paths use exact bindings. |
| 6 | RecoveryApprovalIssuerIsRequiredAndValidated | Quarantined lifecycle uses exact issuer. |
| 7 | WrongRecoveryPreStateIsRejected | Changed-state denial records durable attempt evidence. |
| 8 | ExpiredRecoveryApprovalIsRejected | Expired registered approval is denied. |
| 9 | ReplayedRecoveryApprovalIsRejected | A consumed valid approval is replayed and denied. |
| 10 | FailedRecoveryPermanentlyConsumesApproval | Failure outcome prevents reuse. |
| 11 | DurableRecoveryOutcomeIsRecorded | Outcome relation is asserted after recovery. |
| 12 | PurgeRequiresFreshPerExecutionApproval | Missing/fresh authorization contrast is asserted. |
| 13 | WrongPurgeCutoffBatchDatabasePolicyOrExecutorIsRejected | Exact bindings are mutated individually. |
| 14 | ReplayedOrConcurrentPurgeApprovalIsRejected | TaskCompletionSource/Task.WhenAll barrier drives concurrent use. |
| 15 | ZeroRowPurgeRecordsExactEvidence | Authorized zero-row terminal evidence is asserted. |
| 16 | PartialOrFailingPurgeRecordsFailureEvidence | Supported trigger fault produces durable failure. |
| 17 | PurgeCountMismatchRollsBackAndFailsClosed | Candidate drift/count mismatch proves rollback. |
| 18 | PurgeOwnerInsertsAuditOnlyThroughApprovedRoute | Direct DML denial contrasts with approved writer API. |
| 19 | PurgeOwnerCannotDirectlyMutateProtectedTables | Protected-table privilege matrix is denied. |
| 20 | TemporaryPurgePreservesDurablePerCommandAudit | Deterministic expired grant is removed while durable attempt remains. |
| 21 | RuntimeCannotUpdateDeleteOrExportDurableCommandAudit | Runtime is the actor; both ledgers/export are denied. |
| 22 | AuditInsertionFailureBlocksProtectedCommandAcceptance | Supported audit trigger fault blocks command acceptance. |
| 23 | ExactSqlStateAndDatabaseObjectAreAsserted | Immutable trigger is reached and exact 42501/object asserted. |
| 24 | ZeroRowFalsePositiveIsProhibited | Nonempty fixture prevents vacuous zero-row success. |
| 25 | ActorAndVerifierUseIndependentConnectionsAndContexts | Independent backend PIDs and actor/verifier results are asserted. |

## Exact changed paths

- `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs`
- `src/SESS.NexaERP.Infrastructure/Persistence/Rev869BCommandContextAuthorizer.cs`
- `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs`
- `tests/SESS.NexaERP.Tests/Rev869BControlPlaneProvisioningContract.cs`
- `tests/SESS.NexaERP.Tests/Rev869BControlPlaneRegistry.cs`
- `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs`
- `tests/SESS.NexaERP.Tests/Rev869BCorrection16SourceContractTests.cs`
- `tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs`
- `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs`
- `tests/SESS.NexaERP.Tests/Rev869BDatabaseSafetyContractTests.cs`
- `tests/SESS.NexaERP.Tests/Rev869BOwnedPostgresDatabase.cs`
- `tests/SESS.NexaERP.Tests/Rev869BPurchaseCorrectionTests.cs`
- `tests/SESS.NexaERP.Tests/Rev869BPurgeCoordinator.cs`
- `tests/SESS.NexaERP.Tests/Rev869BTestDatabaseLease.cs`
- `tools/manage-rev869b-control-plane-secure.ps1`
- `tools/rev869b-control-plane-bootstrap.sql`
- `tools/rev869b-control-plane-install.sql`
- `tools/rev869b-control-plane-rollback.sql`
- `tools/rev869b-control-plane-verify.sql`
- `outputs/rev869b_source_correction_checkpoint_17.md`

## Offline validation

| Gate | Result |
|---|---|
| `dotnet build SESS.NexaERP.slnx --no-restore --nologo` | 0 warnings; 0 errors |
| Correction 17 source contracts after final tightening | 8/8 |
| Focused Correction 16/17 and purchase contracts | 24/24 |
| Inclusive REV869B excluding PostgreSQL | 79/79 |
| Complete suite excluding PostgreSQL | 453/453 |
| Exact three REV869B PostgreSQL classes, list-tests only | 50 discovered: 25 corrected, 7 application, 18 direct; 0 executed; **NOT RUN** |
| PowerShell 5.1 AST | 24/24 files; 0 parse errors |
| EF migrations `--no-connect`, inert loopback port 1 | 13; one REV869B immediately after REV869A; applied state unknown |
| Model/snapshot parity, no-connect | 1/1 |
| Offline Up SQL | 279,710 bytes; SHA-256 `C10D173C6F98BD648354692D377A56067D1C4183D56EC5CEFDC30D841BF880FD` |
| Offline Down SQL | 11,133 bytes; SHA-256 `589D7A0DD2448C63B836C45618832855EF5F289E3EE3F0DC993B351A28066841` |
| Up inventory | 27 tables; 75 trigger occurrences; 36 function definitions / 35 distinct names; 47 FK clauses / 50 REFERENCES; 72 indexes; 87 checks |
| Down inventory | 9 DROP TRIGGER statements; 1 generated function definition; 70 DROP lines |
| Prohibited generated SQL | 0 CREATE DATABASE; 0 DROP DATABASE; 0 `pg_terminate_backend` |
| GeneratePlanOnly | PostgreSQLAccessed=false; ContainsCredential=false |
| `git diff --check` before checkpoint | no errors; line-ending warnings only |

Offline SQL was generated only from REV869A to REV869B and back using an inert loopback port-1 design identity. Temporary SQL files were removed. It was not parsed or executed by PostgreSQL.

## External prerequisites, exclusions and next gate

Provisioning and execution remain external and closed: approved control-plane administrator identity; distinct role credentials; protected approval/nonce/fingerprint inputs; reviewed management retention authority; isolated test database authority; and an available PostgreSQL instance. None was provisioned.

No PostgreSQL test, migration apply/remove, database create/alter/clone/restore/quarantine/repair/drop, provisioning, recovery, purge, export, protected business command, production/REV861/AWS/OIDC/frontend/Docker/legacy action, or `legacy-reference` content access occurred.

This checkpoint does not self-declare source safety, provisioning correctness, PostgreSQL acceptance, migration acceptance, production readiness or final REV869B acceptance. The 50 PostgreSQL tests are **NOT RUN** and remain required future behavioral evidence after the source gate.

A fresh independent source-only safety re-review of the committed Correction 17 diff is mandatory and is the only next gate.
