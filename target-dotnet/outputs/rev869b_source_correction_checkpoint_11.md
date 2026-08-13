# REV869B source correction checkpoint 11

Date: 2026-08-13

Starting commit: `6b05c850a4f3cff6bb0c146391f1954357c9a7d2`

Ending commit: the correction-11 commit containing this checkpoint; its immutable hash is reported in the final handoff because a commit cannot contain its own hash.

## Scope and boundary

This was a controlled source-only correction. No PostgreSQL test, database helper, migration application/removal, database create/drop, backup/restore, production system, AWS resource, REV861/frontend surface, REV869C surface, or excluded legacy reference was accessed.

The entry gate passed exactly: expected HEAD and parent, one report-only HEAD file, clean target-scoped status, one retained REV869B migration following REV869A, and no replacement migration.

## Correction-10 findings matrix

| Finding | Root cause | Correction 11 | Evidence / remaining status |
|---|---|---|---|
| B1 static SQL | Source generation was already coherent. | Retained migration identity and object order; regenerated Up/Down and inventories. | Static generation passes; no PostgreSQL acceptance claimed. |
| B2 operation-bound authorization | Signature authenticated only principal/session. | Added irreversible authority fingerprint, exact database transaction ID to the signed canonical envelope and database validation, and minimized context identity fields. | Later-transaction replay is closed. Exact operation/action/entity/version/status/history-slot authorization is not yet implemented and remains blocking. |
| B2 rollback replay | Nonce/context consumption rolled back with the business transaction. | Signature now includes and database verifies `expected_transaction_id=txid_current()`, so a new retry transaction cannot reuse it. | Savepoint rollback in the same transaction can still restore a claim slot; durable single-use consumption remains blocking. |
| B3 qualification history ambiguity | Exact count was followed by a weaker non-strict lookup. | The exact full-tuple query now returns count, history ID, and remarks in one aggregate statement; the weaker lookup was removed. | Source ambiguity closed; PostgreSQL behavior not run. |
| B4 PostgreSQL evidence | Some paths lacked complete independent state and least-privilege realism. | Expanded independent fingerprint inputs and retained exact SQLSTATE/constraint/native-field assertions. Removed an invalid native schema assertion. | Helper still uses owner-backed application credentials; B4 remains blocked. |
| B5 current child versions | PO-line and technical-verification guards omitted current markers. | PO-line requires `IsCurrentVersion`; technical verification requires `IsCurrentRevision` and an organization-bearing parent. | Source guard gap closed; PostgreSQL behavior not run. |
| B6 mapped transactions | Correction 10 already removed nested ambient ownership. | Preserved endpoint-owned transactions and no-ambient mapped fixture. | No regression found offline. |
| B7 qualification compatibility | Canonical lifecycle produced Verified/Approved while consumers required Approved/Approved. | Service eligibility and both database authoritative-transition branches now require Verified/Approved. Seeded canonical graph uses Verified/Approved. Added an exact lifecycle tuple constraint while explicitly allowing retained REV869A Approved/Approved rows. | Cross-layer source values align; PostgreSQL behavior not run. |
| B8 rollback evidence | Fingerprints omitted qualification history, command ledgers, permissions, migration history, and ownership. | Independent database fingerprint now includes vendor qualifications, controlled histories, command contexts, authorities, role-page permissions, migration history, and `nexa` schema owner. | Direct D2/D4 and complete winner/loser/replay distinctions still require further PostgreSQL-test design and remain blocking. |
| B9 raw secret/privacy | Authority table stored reusable HMAC key and contexts stored readable identity/claim payloads. | Authority stores only SHA-256 `SecretFingerprint`; ephemeral key is supplied for verification but not persisted. Context stores irreversible organization/actor/identity/role fingerprints. Claims store one irreversible exact-tuple fingerprint. Authorization has explicit 30-second expiry. | Durable row retention/purge period lacks management approval and remains blocking; owner/runtime separation also remains open. |
| B10 quarantine | Cleanup failure was swallowed; recovery lacked fresh source proof and exact quarantine state. | Cleanup failures are surfaced; marker records expected owner and state; recovery requires fresh source/migration verification, exact owner, exact `Quarantined` state, high-entropy proof, and separate explicit approval. Failed disposal attempts a proof-bound quarantine transition and never authorizes DROP on failure. | No durable sanitized interruption record exists; a hard interruption before marking is deliberately unrecoverable automatically. Runtime role is still owner-backed. |

## Exact changed files

1. `src/SESS.NexaERP.Infrastructure/Masters/EfRev869AFoundationServices.cs`
2. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs`
3. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BControlledMutationSql.cs`
4. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BDatabaseSafetySql.cs`
5. `src/SESS.NexaERP.Infrastructure/Persistence/Rev869BCommandContextAuthorizer.cs`
6. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.RfqQuotation.cs`
7. `tests/SESS.NexaERP.Tests/Rev869BCompleteGraphSeeder.cs`
8. `tests/SESS.NexaERP.Tests/Rev869BDatabaseSafetyContractTests.cs`
9. `tests/SESS.NexaERP.Tests/Rev869BOwnedPostgresDatabase.cs`
10. `tests/SESS.NexaERP.Tests/Rev869BPostgresApplicationBehaviorTests.cs`
11. `tests/SESS.NexaERP.Tests/Rev869BPostgresBehaviorTests.cs`
12. `tests/SESS.NexaERP.Tests/Rev869BTestDatabaseLease.cs`
13. `outputs/rev869b_source_correction_checkpoint_11.md`

## Schema, model, and migration impact

- Retained migration ID unchanged: `20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation`.
- No migration, designer, snapshot, or runtime EF entity was added or replaced.
- Raw migration SQL changes:
  - command authority secret column becomes irreversible `SecretFingerprint`;
  - context identity and claim payloads become irreversible fingerprints;
  - explicit `ExpiresAt` is added;
  - command-open signature includes exact transaction ID and ephemeral key input;
  - qualification lifecycle tuple check is added and removed by retained Up/Down;
  - two child guards gain missing current-version predicates.
- EF model/snapshot parity remains exact because these are retained-migration raw SQL objects/constraints outside the mapped raw security tables.

## Permission, privacy, and workflow impact

- PUBLIC remains revoked from security tables and functions.
- No reusable signing key is stored in the database schema.
- Ordinary readable identity, organization, role, remarks, and claim material was removed from command ledgers.
- The helper still provisions the database owner as runtime principal; real least-privilege separation remains mandatory.
- Canonical new qualification flow is Pending/Pending -> Verified/Pending -> Verified/Approved.
- Retained REV869A Approved/Approved is explicitly recognized for compatibility, not silently normalized.
- Verification and approval employee identities and segregation remain enforced.

## Test and validation inventory

| Validation | Result |
|---|---|
| PowerShell 5.1 AST | 23 files, 0 parse errors |
| Build, `--no-restore` | PASS, 0 warnings, 0 errors |
| Focused REV869B non-PostgreSQL | 58/58 passed |
| Complete non-PostgreSQL | 462/462 passed |
| PostgreSQL discovery | 22 discovered, NOT RUN |
| EF migration discovery | 13 migrations; REV869A then exactly one retained REV869B; `--no-connect` |
| Design model/snapshot parity | 1/1 passed without connecting |
| Offline Up SQL | 211,401 bytes; SHA-256 `1FDD7332D9323BEBE1D88C116EE11112886A4AD0AE72B9848B3E46B93F0D964C` |
| Offline Down SQL | 8,312 bytes; SHA-256 `02F924F818916B50CF2EB3332C66979EECCFB2B97F388A8E2F28699986AB7E5C` |
| SQL inventory | 17 tables, 76 triggers, 24 definitions / 23 unique functions, 46 FKs, 69 indexes, 40 check-pattern matches, balanced 50 revision and 2 extension delimiters |
| Diff whitespace | `git diff --check` passed before checkpoint |

The PostgreSQL methods were listed only. No helper or server was invoked.

## Remaining blockers

1. Exact operation binding remains absent. The authorization must pre-bind action, entity type/ID, organization, actor/role, expected version, source/target status, correlation, and every permitted history slot, with substitution-negative tests.
2. Same-transaction savepoint rollback can restore transactional claim consumption. A non-transactionally durable, proof-bound single-use design is still required without weakening mutation/history atomicity.
3. The helper runtime/application database identity is still the database owner. A distinct least-privilege runtime role and dedicated no-login function owner are required.
4. Management has not approved the durable security-ledger retention/purge period. No arbitrary period was invented. A controlled, auditable purge contract is required after approval.
5. A durable sanitized quarantine evidence artifact and separately governed recovery workflow are still absent. Current recovery correctly refuses uncertain/hard-interruption states.
6. PostgreSQL test source does not yet contain the complete action/entity/version/status/organization/actor substitution and savepoint replay matrix.
7. D2/D4 and the application denial/concurrency cases still need complete independent committed-winner, rolled-back-loser, rejected-replay, unrelated-state, and cleanup-state proof.

## Explicitly unclaimed states

This checkpoint does not declare source safety, execution-helper readiness, PostgreSQL acceptance, migration acceptance, production readiness, or final REV869B acceptance.

PostgreSQL execution remains unauthorized. A fresh independent source-only safety re-review is mandatory after this correction.
