# REV869B pre-apply source safety re-review after Correction 12

Date: 2026-08-13 (Asia/Calcutta)

Reviewed commit: `b0eaac705b9630717917ad6957f5a28fd0ceebbe`

Reviewed parent: `614e41c1dfa773b6bd8f9974e823f06647cea7de`

```text
rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
```

## 1. Scope, independence, and entry gate

This is an independent source-only review. Correction 12's checkpoint was treated as a claim, not acceptance evidence. No PostgreSQL test, PostgreSQL server, database helper, migration apply/remove, provisioning, quarantine recovery, backup/restore, production system, REV861 surface, protected database, or `legacy-reference` content was accessed.

The entry and identity gate passed before this report was created:

- HEAD was exactly `b0eaac705b9630717917ad6957f5a28fd0ceebbe`.
- Its parent was exactly `614e41c1dfa773b6bd8f9974e823f06647cea7de`.
- Its subject was exactly `Perform twelfth controlled REV869B source correction`.
- It contained exactly the reported 17 controlled files.
- Target-scoped Git status was clean.
- EF `--no-connect` discovery listed 13 migrations, with the one REV869B migration immediately after REV869A.
- Global status contained only `?? ../legacy-reference/`; that directory was not opened, changed, or staged.

The conflicting commit `192d84fa1116975a09e9676a7d8c864f975380f5` exists. It and authoritative HEAD are parallel sibling commits: both have parent `614e41c1dfa773b6bd8f9974e823f06647cea7de`; neither is an ancestor or descendant of the other. Their stable patch IDs differ (`1d2f235962f3200e212a777ae8ef1bb334bc0aa0` versus `3edf249f1a7fab36ea9b8720ba4e35414a6e3361`), so they are not patch duplicates. No merge, cherry-pick, reset, rebase, or amendment was performed.

The exact reviewed Correction 12 scope was:

1. `outputs/rev869b_source_correction_checkpoint_12.md`
2. `src/SESS.NexaERP.Api/Endpoints/Rev869AConfigurationEndpoints.cs`
3. `src/SESS.NexaERP.Infrastructure/Masters/EfRev869AFoundationServices.cs`
4. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs`
5. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BControlledMutationSql.cs`
6. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BDatabaseSafetySql.cs`
7. `src/SESS.NexaERP.Infrastructure/Persistence/Rev869BCommandContextAuthorizer.cs`
8. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.ComparisonPo.cs`
9. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.MaterialFollowUp.cs`
10. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.RfqQuotation.cs`
11. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs`
12. `tests/SESS.NexaERP.Tests/Rev869BDatabaseSafetyContractTests.cs`
13. `tests/SESS.NexaERP.Tests/Rev869BOwnedPostgresDatabase.cs`
14. `tests/SESS.NexaERP.Tests/Rev869BPostgresApplicationBehaviorTests.cs`
15. `tests/SESS.NexaERP.Tests/Rev869BPostgresBehaviorTests.cs`
16. `tests/SESS.NexaERP.Tests/Rev869BPurchaseCorrectionTests.cs`
17. `tests/SESS.NexaERP.Tests/Rev869BTestDatabaseLease.cs`

The Correction 12 checkpoint, complete Correction 11 independent report, complete 17-file diff, and directly affected authorization, qualification, current-version, ownership, ledger, rollback, quarantine, and PostgreSQL test contracts were reviewed.

## 2. Executive verdict

Correction 12 materially improves exact slot binding, canonicalization, nontransactional claim consumption, qualification state coverage, current-version guards, security object ownership, durable audit retention, and test design. Those improvements do not make the candidate source-safe.

The principal new blocker is an application/database ordering regression. `BeginAsync` no longer opens a context. `SaveAuthorizedChangesAsync` derives grants only from histories already pending in the EF change tracker. Core purchase create paths call it before adding their history, so it returns with no context and then attempts protected parent/child inserts. Core transition paths perform protected `ExecuteUpdateAsync` calls before adding history or opening a context. `rev869b_guard_explicit_mutation` rejects all of those operations with `rev869b_command_context_required`. The seven PostgreSQL application tests were not run and therefore did not expose this source-demonstrable failure.

Quarantine readiness also remains blocked. A hard interruption after database creation but before marker and external evidence establishment leaves no durable proof usable by recovery. Recovery approval is bound to an instance but has no approval issuance time, expiry, nonce, or one-time consumption, so it is not fresh authorization. Temporary security-ledger retention remains an external management decision, and database execution is not gated on an approved retention configuration.

Complete rollback and PostgreSQL design coverage remain partial. Both canonical states are therefore `FAIL`, and this review authorizes no PostgreSQL or helper execution.

## 3. Finding-by-finding review

| Finding ID | Previous severity | Correction 12 implementation and enforcement | Positive evidence | Negative/adversarial evidence | Result | Remaining risk |
|---|---|---|---|---|---|---|
| C11-01 exact operation slot | BLOCKING | Issuer reserves fingerprint-only slots bound to organization, actor, OIDC issuer/subject, role, claim kind, history, entity, action, version, transition, correlation, remarks, backend, transaction, runtime principal, expiry, and ordinal. History triggers claim the exact slot. | `rev869b_issue_command_grant`, `rev869b_slot_fingerprint`, `rev869b_claim_command_context`; complete substitution test design. | Purchase callers do not open the context before protected mutations; exact authorization is consequently not usable for core workflows. | FAIL | Application integration must reserve/open the exact future slot before the protected mutation without making history forgeable. |
| C11-02 canonicalization/history substitution | BLOCKING / new 4.1 | Typed positional `jsonb_build_array(... )::text` hashing replaces null-ambiguous newline encoding; exact fingerprint includes history and semantic fingerprint excludes it. | Issuance rejects duplicate exact or semantic slots; substitution design includes history ID and every tuple field. | PostgreSQL behavior is unexecuted. | PASS | Catalog/runtime behavior still requires a later authorized PostgreSQL gate. |
| C11-03 rollback/savepoint/replay/pooling | BLOCKING | A fixed migration-owned sequence pool supplies nontransactional ordinals; grants bind runtime, backend, transaction, session user, expiry, and slot ordinal. Issuer pooling is disabled. | `SavepointRollbackCannotRestoreConsumedExactClaim` credibly claims, rolls back to a savepoint, and requires typed SQLSTATE/constraint denial on reuse. | No dedicated full-transaction old-grant replay, pooled-connection reuse, process-interruption, or concurrent same-grant replay test exists. | PARTIAL | Static design is credible, but required scenario coverage is incomplete and PostgreSQL was not run. |
| C11-04 qualification compatibility | BLOCKING | Preflight, lifecycle CHECK, normalize/create/verify/approve/reject/request-correction APIs, database transitions, exact histories, SoD, deactivation, and retained `Approved/Approved` acceptance are aligned in source. | Application PostgreSQL design covers new create/verify/approve/reject/correction plus stale, creator/verifier, scope, audit rollback, and cross-organization negatives. | No executable PostgreSQL positive case covers legacy normalization or retained `Approved/Approved` provenance/eligibility. | PARTIAL | Retained-data compatibility remains source-reasonable but behaviorally unproved. |
| C11-05 current-version and late-child | BLOCKING | PO lines admit a current editable parent or a same-transaction noncurrent amendment parent with `PreviousVersionId`; quotation and other dependent guards retain current/terminal restrictions. | Static contracts and terminal late-child matrix exist. | The valid amendment application path is blocked earlier by the missing command context; no isolated PostgreSQL positive test proves amended-parent child insertion. | FAIL | Valid amendment and every obsolete/late dependent path need executable positive/negative proof after ordering is repaired. |
| C11-06 least-privilege ownership | BLOCKING | Dedicated `nexa_rev869b_security_owner` must be NOLOGIN; six security functions, ledgers, and sequences transfer ownership. Runtime and issuer are distinct NOSUPERUSER/NOCREATEDB/NOCREATEROLE/NOREPLICATION logins. Runtime ledger and audit mutation access is revoked. | Runtime denial design checks protected ledgers and audit UPDATE/DELETE with SQLSTATE `42501`. | No PostgreSQL design directly attempts function replacement, trigger disable/alter, schema/migration mutation, ownership transfer, or self-grant. Migration authority remains a member of the NOLOGIN owner by design. | PARTIAL | Ordinary-runtime isolation is strong in static source but the complete mandatory denial matrix is absent. |
| C11-07 security-ledger privacy/data minimization | REQUIRED CORRECTION | Reusable signing key was removed. Per-command organization, actor, identity, and role are fingerprinted; ledger tables are owner-only. Durable audit is separated and protected for ten years minimum. | No raw OIDC assertion/token/password is stored in command ledgers; runtime read attempts are designed to fail. | UUID/organization/role hashes are pseudonymous and dictionary-testable, not anonymous; owner administration remains sensitive. Temporary rows have no approved purge. | PASS for implemented source boundary | Owner access and exports require governance; temporary retention remains external. |
| C11-08 complete rollback evidence | BLOCKING | Business and security snapshots are separated; failure tests expect business equality, one durable grant/reservation, and sequence movement. | Two application audit-failure paths capture owner-only grant/context/pool/sequence evidence. | Winner, loser, retry, concurrency, direct-SQL failures, every aggregate, unrelated state, and cleanup are not all covered by the same independent complete evidence model. | FAIL | Required complete rollback-state proof remains incomplete. |
| C11-09 durable quarantine ownership | BLOCKING | Owner-only database marker and signed external envelope bind exact name, run, token hash, family, scenario, owner, provisioning time, roles, source fingerprint, and migration fingerprint. | Normal cleanup rechecks marker and refuses DROP on mismatch or active connection. | Evidence is first written only after database creation and marker establishment. A hard interruption between CREATE and that point leaves database/roles without durable recoverable ownership proof. No interrupted-cleanup test exists. | FAIL | Pre-destructive durable intent plus a safe, testable interrupted-state protocol is required. |
| C11-10 safe proof-bound recovery | BLOCKING | Recovery recomputes source/migration proof, verifies signed envelope and database marker, requires an HMAC over exact instance fields, refuses active connections, and uses ordinary DROP without FORCE. | Wildcards and broad termination are absent; exact names and roles are validated. | The approval is deterministic and has no issued-at, expiry, nonce, purpose version, or consumed marker. The same long-lived key signs evidence and recovery approval. It is instance-bound but not fresh or one-time. | FAIL | Fresh separately governed, expiring, one-use recovery authorization is required. |
| C11-11 PostgreSQL substitution/rollback/concurrency design | BLOCKING | Discovery grows from 22 to 25 tests and adds exact substitution, savepoint durability, runtime privacy/privilege, qualification rejection/correction, and security snapshots. | 7 application and 18 direct tests compile/list; typed SQLSTATE/object helper rejects zero-row false positives. | Core application tests are source-incompatible with authorization ordering. Full rollback retry, connection reuse, process interruption, valid amendment, all runtime DDL denial, interrupted cleanup, and fresh-recovery cases are absent. | FAIL | Test inventory is not yet capable of proving all mandatory scenarios. |
| C11-12 management-approved temporary retention | EXTERNAL MANAGEMENT DECISION | Temporary grant/context/sequence/quarantine cleanup remains disabled; ten-year durable audit is separate. | No arbitrary temporary period or destructive default was invented. | Helper/database execution can proceed without an approved retention value or explicit management gate. | EXTERNAL DECISION; currently blocks both canonical states | Management must approve privacy scope, access/export, deletion semantics, schedule, and evidence; execution must fail closed until configured. |

## 4. New findings

### C12-N01 — protected purchase mutations occur before exact context issuance — BLOCKING

`Rev869BCommandContextAuthorizer.OpenForPendingChangesAsync` returns immediately when no added protected history exists. `EfRev869BPurchaseService.BeginAsync` no longer opens a context. The first saves in comparison, invitation, quotation, technical verification, PO, amendment, rejected-PO revision, and related creation paths call `SaveAuthorizedChangesAsync` before adding their history. Transition paths execute `ExecuteUpdateAsync` before adding history and before `SaveAuthorizedChangesAsync`.

At the database boundary, `rev869b_guard_explicit_mutation` is a BEFORE INSERT/UPDATE trigger on protected parent and child relations and requires `rev869b_command_context_valid(...)` before it permits the mutation. Therefore these callers cannot reach the later history insertion that would supply a slot. The failure is deterministic from source and does not depend on PostgreSQL acceptance.

Required correction: preconstruct and authorize the exact immutable history slot before every protected mutation, while preserving same-transaction history enforcement and preventing caller-authored slot forgery. Add an executable positive test for every service path and a negative test proving mutation without its pre-authorized exact slot fails.

### C12-N02 — quarantine has an unrecoverable pre-marker interruption window — BLOCKING

Database and roles are created before `EstablishMarkerAsync` and before signed external evidence is written. If the process terminates in that interval, no database-backed marker or independently signed evidence survives to authorize later recovery. The catch/dispose path helps ordinary exceptions but cannot handle hard process interruption.

Required correction: establish separately protected durable intent before CREATE, bind it to the exact forthcoming database/run/roles/source/migration/scenario/expiry, reconcile it atomically with the database marker, and test interruption at each boundary without name-only deletion or automatic uncertain repair.

### C12-N03 — quarantine recovery approval is not fresh — BLOCKING

The HMAC binds the exact instance but contains no approval-issued timestamp, expiry, nonce, authorization ID, or one-time consumed state. A retained `REV869B_QUARANTINE_RECOVERY_APPROVAL` can therefore remain valid indefinitely for that instance. The evidence-signing key and recovery-approval key are also the same environment value.

Required correction: use a separately governed recovery authorization key or authority, bind issued-at/expiry/nonce/purpose, record one-time consumption durably, and add stale/replayed/expired/wrong-purpose adversarial tests.

### C12-N04 — focused checkpoint claim is under-specified — IMPROVEMENT

The checkpoint reports 44 focused tests without recording its filter. The reproducible three-class purchase-focused filter produces 44/44; the inclusive `Rev869B` non-PostgreSQL filter produces 59/59 because it additionally includes 15 database-safety contracts. Future reports should record the exact filter and class inventory.

## 5. Authorization, replay, workflow, and version conclusions

The database grant tuple is exact and substantially stronger than Correction 11. It binds organization, actor, active identity, role, issuer principal, runtime principal, backend PID, transaction ID, claim kind, history ID, entity type/ID, operation, expected parent version, source/target states, correlation, remarks, expiry, semantic identity, and a single-use ordinal. Structured JSON canonicalization removes the prior null/newline ambiguity. Failed or rolled-back claims consume a nontransactional sequence ordinal, so the same grant slot cannot be restored by a savepoint.

This database design is not correctly integrated into the purchase service. Exact slots are derived too late. Consequently successful commit, command failure, retry, concurrent replay, and connection reuse cannot be accepted for the affected service paths. Qualification endpoints add history before opening the grant and are structurally compatible, but the broader purchase workflow is not.

Current-version and cross-organization database predicates are improved. The amended-PO same-transaction exception is narrow (`PreviousVersionId`, noncurrent, transaction `xmin`), while late/superseded parents remain rejected. Provenance predicates bind organization and exact qualification evidence. Those protections do not cure the earlier authorization-order failure.

## 6. Runtime ownership, ledger, audit, and retention conclusions

Ordinary runtime is a distinct non-owner login. It does not own protected functions/tables/sequences, receives no security-ledger access, and has audit UPDATE/DELETE revoked. Static PostgreSQL ownership semantics prevent it from replacing owner functions or altering owner triggers absent ownership or elevated role membership. However, the executable denial design does not directly exercise all required DDL, trigger, schema, migration, ownership, and self-grant attacks.

No reusable command signing key, password, token, raw OIDC assertion, private key, or AWS access-key literal was found in the Correction 12 path. Command ledgers contain fingerprints and database principal names. Durable audit UPDATE is always rejected; DELETE before ten years is rejected; expired deletion requires database owner plus bounded reason/correlation and creates minimized `PurgeExpiredAudit` evidence. Runtime cannot perform that cleanup.

Temporary command grants, contexts, sequence reservations, and quarantine envelopes have no approved retention/purge configuration. This remains an external management decision. It blocks source safety and helper readiness now because execution does not require an approved value and therefore does not satisfy the review's fail-closed exception for an unresolved decision. It also blocks production readiness. No period is invented here.

## 7. Quarantine conclusions

Normal-path proof is materially stronger: exact high-entropy target, database marker, ownership token, run, family, scenario, owner, provisioning time, roles, source/migration fingerprints, signed external evidence, zero active connections, and ordinary DROP are checked. Filesystem evidence alone is insufficient and is not treated as sufficient.

The pre-marker hard-interruption window and non-fresh approval remain blocking. Recovery is fail-closed when its available proof mismatches, but there is no authorized recovery route for an interrupted pre-marker database. A deterministic unexpired approval is not fresh authorization. Helper readiness remains `FAIL`.

## 8. PostgreSQL test-design review — NOT RUN

Exactly 25 PostgreSQL tests were discovered: 7 application-level and 18 direct-database tests. All 25 are **NOT RUN**.

| Required design property | Result |
|---|---|
| Operation/entity/version/status/organization/actor substitution | Present as a comprehensive typed denial matrix. |
| Savepoint rollback replay | Present with durable ordinal reuse denial. |
| Full rollback/retry/old-grant replay | Partial; business failure and durable evidence exist, but explicit old-grant replay and complete matrix do not. |
| Two independent DbContexts/connections and one winner | Present in source, but the underlying application write is blocked by context ordering. |
| Exact SQLSTATE/database object and zero-row rejection | Present in central helper and new direct cases. |
| Qualification lifecycle | New create/verify/approve/reject/correction and adversarial cases present; legacy normalization positive case absent. |
| Current-version/late-child | Terminal negatives present; valid amended-parent positive case absent and blocked by ordering. |
| Runtime privilege denial | Ledger/audit DML denial present; complete DDL/trigger/ownership/self-grant matrix absent. |
| Privacy and durable retention | Static contract plus runtime audit DML denial present; expired owner-cleanup behavior not executable here. |
| Durable quarantine ownership | Normal path is designed; pre-marker interruption proof is absent. |
| Safe interrupted cleanup and fresh recovery | Missing. |

Discovery and compilation do not establish PostgreSQL behavior.

## 9. Reconciled offline validation

| Validation | Authoritative HEAD result |
|---|---|
| Build `--no-restore` | PASS; 0 warnings, 0 errors |
| Focused three-class purchase suite | PASS; 44 passed, 0 failed, 0 skipped. This reconciles 43 versus 44 in favor of 44 at HEAD. |
| Inclusive REV869B non-PostgreSQL suite | PASS; 59/59; 17 behavior + 10 correction + 17 foundation + 15 database-safety contracts |
| Complete non-PostgreSQL suite | PASS; 433 passed, 0 failed, 0 skipped; this reconciles 432 versus 433 in favor of 433 |
| PostgreSQL discovery only | 25 discovered (7 application + 18 direct); **NOT RUN**; this reconciles 22 versus 25 in favor of 25 |
| PowerShell 5.1 AST | PASS; 23 files, 0 parse errors; version `5.1.19041.6456` |
| EF migration discovery | PASS with `--no-connect`; 13 migrations; REV869A immediately followed by exactly one REV869B; applied state unknown |
| Pending-model/model-snapshot parity | PASS; exact offline test 1/1 |
| Offline REV869A-to-REV869B Up SQL | 227,526 bytes; SHA-256 `741278C63AFDE04459A2A0240F6C5AF835AC195574FC7143E6E3CBEC751C48D5` |
| Offline REV869B-to-REV869A Down SQL | 9,108 bytes; SHA-256 `48B5E8B99C23A53E6724DA901E86590FB913AD9F22E3BE1694EC542552735FFA` |
| Up inventory | 19 tables; 77 triggers; 27 function definitions / 26 unique functions; 46 FKs; 69 indexes; 42 CHECK patterns |
| Down inventory | 0 tables; 2 triggers; 1 function definition / 1 unique function; 0 FKs; 0 indexes; 0 CHECK patterns |
| Operation-binding scan | Database tuple PASS; application ordering FAIL |
| Replay/savepoint/rollback scan | Durable ordinal/savepoint design PASS; full scenario coverage PARTIAL |
| Qualification consistency scan | Source alignment PASS; retained-data executable proof PARTIAL |
| Current-version coverage scan | Database guard PASS; valid application amendment proof FAIL due ordering |
| Runtime ownership/privilege scan | Static ownership PASS; executable denial matrix PARTIAL |
| Ledger privacy/retention scan | Durable audit PASS; temporary retention EXTERNAL and ungated |
| Quarantine safety scan | FAIL; pre-marker interruption and fresh-approval blockers |
| Secret/prohibited-operation scan | No active reusable signing key, FORCE drop, backend termination, migration execution, or secret literal in the reviewed path; matches were negative assertions only |
| `git diff --check` | PASS |
| Temporary SQL artifacts | Removed |

Offline validation is not PostgreSQL acceptance.

## 10. Required corrections and external management decision

### BLOCKING / REQUIRED CORRECTION

1. Repair application authorization ordering so every protected insert/update has its exact issuer-reserved history slot and transaction-local context before the mutation, while the matching immutable history remains mandatory in the same transaction.
2. Add positive PostgreSQL designs for every repaired service path, especially create, transition, amendment child construction, rejected revision, qualification normalization, retry, pooled connection reuse, and full rollback.
3. Complete rollback/security fingerprints across winners, losers, retries, direct failures, every affected aggregate, unrelated relations, grant/context/pool/sequence state, and cleanup.
4. Add runtime denial tests for function replacement, trigger alteration/disablement, schema/migration DDL, ownership transfer, privilege grant, and role escalation.
5. Establish durable pre-CREATE quarantine intent and test hard interruption at every marker/evidence boundary.
6. Replace deterministic indefinite recovery approval with separately governed, expiring, nonce-bound, purpose-bound, one-use authorization and test stale/replayed/expired/wrong-purpose cases.
7. Gate all database/helper execution on an explicitly approved temporary security-ledger retention configuration.

### EXTERNAL MANAGEMENT DECISION

Management must approve temporary command-grant, context, claim-sequence reservation, and quarantine-envelope retention, including privacy scope, protected access/export, deletion semantics, schedule, and auditable evidence. The established ten-year durable business-audit minimum is separate and must not be weakened. No temporary duration is authorized or invented by this review.

The unresolved decision currently blocks:

- source safety: **yes**, because execution is not gated on an approved value;
- execution-helper readiness: **yes**;
- production readiness: **yes**.

## 11. Exact next gate

The next authorized gate is a **thirteenth controlled source-only REV869B correction** limited to the blockers and required corrections in this report, followed by a fresh independent source-only safety re-review of that committed diff.

Until a later independent review explicitly sets both canonical states to `PASS`, PostgreSQL tests, database helpers, migration apply/remove, role/authority provisioning, quarantine recovery, backup/restore, protected database access, and production execution remain unauthorized by this review.

```text
rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
```
