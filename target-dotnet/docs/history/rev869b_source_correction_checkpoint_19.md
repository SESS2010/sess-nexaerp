# REV869B source correction checkpoint 19

Date: 2026-08-14 (Asia/Calcutta)

Starting commit: `c8b692070c4257623877db42803510116ff1d830`

Starting subject: `Freeze REV869B architecture for correction 19`

Authoritative architecture report: `outputs/rev869b_architecture_freeze_root_cause_review.md`

Verified architecture-report SHA-256: `FBD74D7663BB3FD989158DB97C5544A2DA31307E5113DD5C12283E7959BC1B08`

Scope: bounded source-only Correction 19. No PostgreSQL connection, provisioning, migration application/removal, purge, recovery, export, or database-test execution occurred.

## 1. Architecture-freeze implementation map

| Frozen requirement | Correction 19 implementation | Source evidence |
|---|---|---|
| External provisioning owns cluster roles and databases | The PowerShell helper exposes only `GeneratePlanOnly`, `PreflightOnly`, and `PostProvisionVerification`. Cluster-mutating bootstrap/deprovision artifacts were deleted. Preflight is read-only and binds system identifier, endpoint, TLS SPKI, environment, source commit, package manifest, exact database, roles, capability flags, and the single explicit external-admin-to-owner NOINHERIT membership. | `tools/manage-rev869b-control-plane-secure.ps1`; `tools/rev869b-control-plane-preflight.sql`; deletion of `tools/rev869b-control-plane-bootstrap.sql` and `tools/rev869b-control-plane-deprovision.sql` |
| Dedicated lifecycle controller; no admin credential in application/tests | Disposable-test allocation and cleanup use a typed HTTPS controller client. Allocation proves exact derived name, runtime/verifier principals, pinned identity/manifest values, controller-prepared fixture evidence, and absence of lifecycle-admin credentials. Test helpers contain no direct role/database creation or deletion and no filesystem authority. | `Rev869BLifecycleControllerClient.cs`; `Rev869BTestDatabaseLease.cs`; `Rev869BControlPlaneRegistry.cs`; `Rev869BOwnedPostgresDatabase.cs` |
| Surviving control-plane database is lifecycle authority | The transactional package stores immutable manifest identity, versioned leases/events, recovery decisions, lifecycle attempts/outcomes, a single-active-attempt constraint, exact purpose APIs, stable/idempotent finalization, and schema-only rollback after all leases are Finalized. Provisioning attempts terminalize at Ready; recovery reuses only the bound active attempt or creates a new linked attempt. | `rev869b-control-plane-install.sql`; `rev869b-control-plane-verify.sql`; `rev869b-control-plane-rollback.sql` |
| Exact control-plane ACL closure | The verifier compares full expected/actual relation, function-owner, function-execute, direct-table-access, PUBLIC/default, database/schema, and membership sets. Owner and externally held lifecycle administrator are treated as the explicit administrative trust boundary, not ordinary ACL-constrained principals. | `rev869b-control-plane-verify.sql`; `Rev869BControlPlaneProvisioningContract.cs`; Correction 16 source contracts |
| Request-scoped command authorization | Every protected Purchase request passes its existing idempotency value; protected qualification operations require an explicit `Idempotency-Key`. No process-global command idempotency key remains. Registration binds organization, operation, request hash, employee, issuer, subject, and role. | `Rev869BCommandContextAuthorizer.cs`; Purchase service partials; `Rev869AConfigurationEndpoints.cs` |
| Ordered, immutable attempt lifecycle and replay/concurrency protection | Target-local requests have scoped unique keys; attempts have database-generated ordinals and one-active-attempt uniqueness. An active attempt is reusable only by its exact execution/service/ownership/runtime/backend/transaction binding; a concurrent foreign binding receives a serialization conflict. Replay fingerprint mismatch is closed. | `Rev869BCommandContextSql.cs`; authorizer; Correction 17/database-safety source contracts |
| Business commit and receipt share one transaction | Open/claim occurs in the service-owned target transaction. Multi-save slot fingerprints are accumulated identically in source and database. Exact claim coverage and cumulative business fingerprint are verified before the immutable receipt and Committed outcome are inserted, then the business transaction commits. | `EfRev869BPurchaseService.cs`; `Rev869BCommandContextAuthorizer.cs`; `Rev869BCommandContextSql.cs` |
| Rollback-independent noncommit evidence | Request/attempt Started evidence is written by the distinct audit principal before target mutation. Rejected, RolledBack, and Abandoned outcomes use a fresh non-pooled audit connection after rollback; reconcile returns authoritative receipt/outcome state. A concurrent loser cannot terminalize another backend's attempt. | Authorizer and target command APIs |
| Target-local purge saga | Management writer registers one bounded decision; purge worker freezes exact candidate IDs/count/digest. Parent attempt is inserted before candidates. Claims are deleted before contexts; candidate drift or an active attempt aborts the deletion transaction. Delete plus Succeeded evidence is atomic; Failed/Interrupted is appended only after rollback; retry requires a new decision linked by `PriorAttemptId`. | `Rev869BCommandContextSql.cs`; `Rev869BPurgeCoordinator.cs` |
| Governed immutable export | Management writer registers the exact organization/field/row/as-of/expiry authorization. Export service consumes it once, creates the parent batch before rows, materializes minimized immutable rows and digest, commits a release ID before read, and records Delivered/Failed/Interrupted. Removed API live-query export routes are not retained. | target export tables/functions; `Rev869BPurchaseEndpoints.cs` |
| Frozen role/ACL model | Capability-free externally provisioned target roles are required. PUBLIC and every non-owner role lose direct security-ledger and default privileges. Runtime receives only the explicit retained Purchase/master business allowlist plus exact command APIs; audit, management, purge, export, and verifier receive only purpose APIs. The verifier reads minimized security state through a definer function, not direct ledger DML. | target role gate, REVOKE/GRANT inventory, `rev869b_read_target_security_state` |
| Disposable isolated acceptance ownership | The 18 direct and 7 application PostgreSQL behavior tests compile against controller-owned allocation/cleanup and frozen command interfaces. Controller-prepared fixtures replace verifier/owner seeding. Source design contracts map P01–T03 to deterministic controller evidence fields: action reached, initial/final state, SQLSTATE/object for denial, durable evidence, zero unrelated mutation, and finalized cleanup. | lifecycle client/lease; retained behavior tests; Correction 14/17 PostgreSQL design sources |
| Preserve REV869A and business scope | No migration ID/designer/snapshot, earlier migration, domain schema, frontend, REV861, REV869C, AWS, OIDC configuration, or unrelated API was changed. REV869B remains the single unapplied migration immediately after REV869A. | committed-file inventory; EF discovery; model/snapshot parity |

## 2. Frozen interfaces and transaction boundaries

### Control plane

- Lifecycle API: reserve, begin provisioning, mark Ready/InUse, authorize normal drop, begin drop, and read.
- Management writer: register a recovery decision only.
- Recovery executor: consume the exact unused decision and bind/reuse the stable recovery attempt.
- Lifecycle audit: record CleanupFailed or atomically finalize proven target/role absence.
- Verifier: read-only exact catalogue/ACL/manifest verification.

Reservation, external create/drop, target marker installation, control-plane observation, and finalization remain distinct saga boundaries. No source claims cross-database atomicity.

### Target command

- Audit transaction C1: request registration and ordered Started attempt.
- Business transaction C2: exact context/claims, protected rows/histories, cumulative receipt, and Committed outcome.
- Audit transaction C3: Rejected/RolledBack/Abandoned only after C2 does not commit.

### Purge and export

- P1/P2: management decision and frozen purge candidate batch.
- P3: exact candidate deletion and Succeeded evidence in one transaction.
- P4: Failed/Interrupted evidence after P3 rollback.
- E1: consume management export approval and materialize immutable rows/digest.
- E2/E3: durable release attempt before bytes and honest terminal delivery outcome afterward.

## 3. Exact committed-file inventory

The intended Correction 19 commit contains exactly these 30 paths:

1. `src/SESS.NexaERP.Api/Endpoints/Rev869AConfigurationEndpoints.cs`
2. `src/SESS.NexaERP.Api/Endpoints/Rev869BPurchaseEndpoints.cs`
3. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs`
4. `src/SESS.NexaERP.Infrastructure/Persistence/Rev869BCommandContextAuthorizer.cs`
5. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.ComparisonPo.cs`
6. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.MaterialFollowUp.cs`
7. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.RfqQuotation.cs`
8. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs`
9. `tests/SESS.NexaERP.Tests/Rev869BControlPlaneProvisioningContract.cs`
10. `tests/SESS.NexaERP.Tests/Rev869BControlPlaneRegistry.cs`
11. `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs`
12. `tests/SESS.NexaERP.Tests/Rev869BCorrection16SourceContractTests.cs`
13. `tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs`
14. `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs`
15. `tests/SESS.NexaERP.Tests/Rev869BDatabaseSafetyContractTests.cs`
16. `tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs`
17. `tests/SESS.NexaERP.Tests/Rev869BOwnedPostgresDatabase.cs`
18. `tests/SESS.NexaERP.Tests/Rev869BPostgresApplicationBehaviorTests.cs`
19. `tests/SESS.NexaERP.Tests/Rev869BPostgresBehaviorTests.cs`
20. `tests/SESS.NexaERP.Tests/Rev869BPurchaseCorrectionTests.cs`
21. `tests/SESS.NexaERP.Tests/Rev869BPurgeCoordinator.cs`
22. `tests/SESS.NexaERP.Tests/Rev869BTestDatabaseLease.cs`
23. `tools/manage-rev869b-control-plane-secure.ps1`
24. `tools/rev869b-control-plane-bootstrap.sql` (deleted)
25. `tools/rev869b-control-plane-deprovision.sql` (deleted)
26. `tools/rev869b-control-plane-install.sql`
27. `tools/rev869b-control-plane-preflight.sql`
28. `tools/rev869b-control-plane-rollback.sql`
29. `tools/rev869b-control-plane-verify.sql`
30. `outputs/rev869b_source_correction_checkpoint_19.md`

## 4. Reconciled offline validation

| Validation | Result |
|---|---|
| PowerShell 5.1 AST parsing | 0 parse errors |
| Solution build | 5 projects; 0 warnings; 0 errors |
| Focused REV869B non-PostgreSQL tests | 63 passed; 0 failed; 0 skipped |
| Complete non-PostgreSQL suite | 437 passed; 0 failed; 0 skipped |
| Explicit model/snapshot parity | 1 passed; 0 failed; 0 skipped |
| PostgreSQL test compilation/discovery only | 75 PostgreSQL/PostgreSql-named tests discovered overall; 45 REV869B-named; **0 executed** |
| EF migration discovery | `--no-connect`, inert `127.0.0.1:1`; 13 migrations |
| REV869 order/uniqueness | REV869A index 11; REV869B index 12; REV869B occurrence count 1 |
| Offline REV869A→REV869B Up SQL | 241,416 bytes; 2,219 lines; SHA-256 `54CBC617C9B8738F8FC9C59995C3E5CA6B15375C4317A0A78ECAFBBF9F5D022986A08C` |
| Offline REV869B→REV869A Down SQL | 10,010 bytes; 211 lines; SHA-256 `80CB8F249EF486FE62A8AC4A1E314662469F0D666994FD48D32E0330A6C032F1` |
| SQL temp artifacts | Removed after hashing |
| Secret/privacy scan | No credential/privacy field in target/control-plane durable ledgers; the only secret-pattern hit is the helper's defensive rejection regex |
| Prohibited operation scan | No REV869B executable source contains role/database create/drop commands; unrelated pre-existing REV869A helper text is outside this change |
| `git diff --check` | Clean |

The source-only design contracts and offline SQL generation are not PostgreSQL syntax or behavior acceptance. They do not replace execution against a separately authorized isolated PostgreSQL environment.

## 5. External prerequisites

1. Management ratification of the lifecycle controller owner/on-call authority, recovery RTO/nonterminal age, recovery/purge/export approver separation, retention values, retry policy, emergency-admin process, and control-plane cost/availability ownership.
2. Externally pinned isolated PostgreSQL system identifier, verified TLS/SPKI, endpoint/environment classification, and clock/monitoring/backup controls.
3. External IaC creation of the exact NOINHERIT/capability-minimized roles, the surviving control-plane database, database CONNECT closure, credentials, and rotation.
4. Externally reviewed installation of the exact control-plane package with the verified aggregate manifest and source commit.
5. Deployment of the lifecycle controller/reconciler and management approval writer outside application/test processes.
6. Controller-prepared deterministic isolated fixtures and acceptance-only deterministic failpoints.
7. Independent source-only safety rereview of this commit.
8. Separate authorization before any PostgreSQL verification, migration, provisioning, purge, recovery, export, helper execution, or production use.

## 6. Explicit nonclaims and next gate

This checkpoint does **not** self-declare REV869B source safety or execution-helper readiness.

- PostgreSQL behavioral acceptance: unclaimed; zero PostgreSQL tests executed.
- Control-plane/helper operational readiness: unclaimed.
- Provisioning, lifecycle, recovery, purge, export, migration, and production acceptance: unclaimed.
- Exactly-once network export delivery: not claimed; durable release attempts and honest outcomes are the frozen contract.

Exact next gate: commit this bounded Correction 19 source set, then perform a fresh independent source-only safety rereview of the exact Correction 19 commit and its parent. Do not access PostgreSQL or begin any further correction in this task.
