# REV869B pre-apply source safety re-review after Correction 11

Reviewed commit: `510717a356c00958fbdbf89193242afa383dc0a9`
Reviewed parent: `6b05c850a4f3cff6bb0c146391f1954357c9a7d2`

```text
rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
```

## 1. Scope and independence

This is a fresh independent source-only review. Correction 11's checkpoint was treated as a claim to verify, not as acceptance evidence. No PostgreSQL test, server, database, execution helper, migration apply/remove, provisioning, quarantine recovery, backup/restore, production system, AWS resource, REV861/frontend surface, REV869C surface, or excluded legacy reference was accessed.

The entry gate passed before review:

- `HEAD` was exactly `510717a356c00958fbdbf89193242afa383dc0a9`.
- Its parent was exactly `6b05c850a4f3cff6bb0c146391f1954357c9a7d2`.
- The target-scoped worktree was clean.
- `../legacy-reference/` was still untracked as `?? ../legacy-reference/`; it was not opened or changed.
- The migration list contained exactly 13 migrations, with the one REV869B migration immediately after REV869A.

The complete committed 13-file diff was reviewed:

1. `outputs/rev869b_source_correction_checkpoint_11.md`
2. `src/SESS.NexaERP.Infrastructure/Masters/EfRev869AFoundationServices.cs`
3. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs`
4. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BControlledMutationSql.cs`
5. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BDatabaseSafetySql.cs`
6. `src/SESS.NexaERP.Infrastructure/Persistence/Rev869BCommandContextAuthorizer.cs`
7. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.RfqQuotation.cs`
8. `tests/SESS.NexaERP.Tests/Rev869BCompleteGraphSeeder.cs`
9. `tests/SESS.NexaERP.Tests/Rev869BDatabaseSafetyContractTests.cs`
10. `tests/SESS.NexaERP.Tests/Rev869BOwnedPostgresDatabase.cs`
11. `tests/SESS.NexaERP.Tests/Rev869BPostgresApplicationBehaviorTests.cs`
12. `tests/SESS.NexaERP.Tests/Rev869BPostgresBehaviorTests.cs`
13. `tests/SESS.NexaERP.Tests/Rev869BTestDatabaseLease.cs`

The prior Correction 10 review, Correction 11 checkpoint, and directly affected workflow, authorization, command-context, mutation, qualification, security-ledger, rollback, quarantine, migration, and test contracts were read completely.

## 2. Executive verdict

Correction 11 contains useful hardening, but material source and test-design blockers remain. It also introduces two important regressions: a caller can evade the new command-context duplicate guard by selecting a different history identifier, and the late-child purchase-order guard rejects the valid amendment service path while it constructs a non-current draft version.

The principal authorization envelope still does not pre-authorize an exact operation slot. Savepoint rollback still restores transactional claim consumption. The helper still uses the database owner as the runtime identity. Qualification compatibility is incomplete. Quarantine ownership and recovery proof are not independently durable or strongly bound. Required substitution, savepoint, lifecycle, and complete rollback evidence tests are absent.

These defects are material. Both canonical states therefore remain `FAIL`, and this review authorizes no database command or helper execution.

## 3. Finding-by-finding verdict on the eleven inherited blockers

| # | Required area | Verdict | Independent result |
|---:|---|---|---|
| 1 | Exact operation-slot authorization | **BLOCKING** | The signed envelope binds principal/context attributes but not action, entity type/ID, expected version, source/target state, correlation, or permitted history slot. Those values remain caller-authored at claim time. |
| 2 | Savepoint/rollback-safe durable claim consumption | **BLOCKING** | Context, nonce, and claims are transactional. Savepoint or transaction rollback restores the consumed state. A holder of the shared signing key can issue a fresh envelope after rollback. |
| 3 | Qualification workflow compatibility | **BLOCKING** | The canonical new path is consistent, but retained `Approved/Approved` is accepted by database provenance while service transition branches require `Verified/Approved`. Rejected/correction states are not reconciled, and existing noncanonical values have no preflight or normalization. |
| 4 | Current-version and late-child guards | **BLOCKING** | Technical-verification currentness improved. The PO-line guard now requires a current version, but amendment intentionally inserts lines while the new draft PO has `IsCurrentVersion=false`; the valid service path is rejected. |
| 5 | Least-privilege runtime ownership | **BLOCKING** | The helper provisions `current_user`, which is the cloned database owner, as the command authority/runtime principal. Security-definer functions have no dedicated no-login owner and no realistic least-privilege boundary. |
| 6 | Security-ledger privacy/data minimization | **REQUIRED CORRECTION** | Raw per-command secret storage was removed and fingerprints/expiry were added. However, the reusable signing key is still passed to a database function for every context opening, and durable context/authority rows retain principal metadata without a purge contract. |
| 7 | Management-approved retention | **EXTERNAL MANAGEMENT DECISION** | No retention period was invented, correctly. Management still must approve the durable security-ledger retention/purge policy; source must then implement a controlled, auditable purge without deleting required audit evidence. |
| 8 | Complete rollback evidence | **BLOCKING** | Fingerprints were expanded, but the PostgreSQL design still lacks complete independent proof of winner commit, loser rollback, rejected replay, unrelated-state preservation, and cleanup/security-ledger state for every affected aggregate. |
| 9 | Durable quarantine ownership evidence | **BLOCKING** | Marker fields improved, but no durable sanitized evidence outside the disposable database establishes database/run/token/family/source/owner/scenario ownership after a hard interruption. Filesystem evidence is neither sufficient nor independently protected. |
| 10 | Safe proof-bound quarantine recovery | **BLOCKING** | Recovery fails closed in uncertain states, but its approval phrase is global rather than instance-bound; owner proof is self-referential; scenario hash/provisioning time are not verified; source fingerprinting is too weak; forced drop terminates all connections to the target. |
| 11 | PostgreSQL substitution/replay/rollback/concurrency design | **BLOCKING** | Exactly 22 tests exist, but no complete operation/entity/version/status/organization/actor substitution matrix, no savepoint rollback replay test, and no full qualification/current-version positive and negative lifecycle proof exist. |

## 4. Newly discovered findings

### 4.1 History-ID substitution defeats the new claim duplicate guard — BLOCKING

Correction 11 replaces the prior semantic duplicate check with a fingerprint that includes caller-selected `history_id`. The same principal context can therefore claim the same semantic operation, entity, version, and correlation again merely by supplying another history identifier. The different history ID produces a different fingerprint and avoids the uniqueness check. This weakens, rather than closes, operation-slot single consumption.

The fingerprint is also serialized with newline-delimited `concat_ws`. PostgreSQL omits null arguments in `concat_ws`, and free-text claim fields are not length-prefixed or type-tagged. Distinct tuples can therefore have the same canonical input string without any cryptographic hash collision. The authorization claim encoding is structurally ambiguous.

### 4.2 Purchase-order amendment path is made unreachable — BLOCKING

The new late-child guard requires a purchase-order parent to be current and at version zero before inserting a line. `AmendPurchaseOrderAsync` deliberately creates the next draft PO with version zero and `IsCurrentVersion=false`, copies its lines, and saves before the version becomes current. The database guard rejects those legitimate child inserts. A required business workflow is therefore incompatible with the new constraint.

### 4.3 Qualification compatibility claim is overstated — BLOCKING

The database check and provenance predicate preserve the REV869A `Approved/Approved` tuple, but the authoritative service branches require `Verified/Approved`. Thus the legacy tuple is structurally allowed yet functionally unreachable. Domain states such as rejected, clarification requested, and revision requested are also not represented in the new tuple constraint or a mapped correction endpoint. Existing REV869A rows outside the permitted tuples could make migration application fail; no source-only preflight or normalization contract addresses them.

### 4.4 Reusable signing key crosses the database boundary — REQUIRED CORRECTION

Although raw secret persistence was removed, the reusable application signing key remains an input to the database context-opening function. It can enter database statement observability/logging surfaces and authorizes all contexts held by that application identity. This is reusable authorization material, not an exact one-time operation authorization.

## 5. Workflow, ownership, permissions, and approval

The canonical new qualification flow is `Pending/Pending` to `Verified/Pending` to `Verified/Approved`. Verification and approval identities and segregation checks are present. The tightened history aggregate now obtains count, ID, and remarks from the same full predicate, which is an improvement over the prior weak lookup.

These gains do not resolve lifecycle compatibility: legacy `Approved/Approved` cannot traverse the service's procurement predicate, rejection/correction handling is absent, and the migration has no data-compatibility gate for other existing state pairs.

Record-level organization and employee checks exist in service and database paths, but the command authorization issuer does not pre-bind the record operation. A valid principal context can be repurposed for different caller-supplied operation claims. Transaction-ID binding helps prevent reuse of the identical signed envelope on a later transaction, but it neither binds the operation nor prevents a shared-key holder from minting a new envelope.

PUBLIC privilege revocation is useful but not a least-privilege ownership model. The database owner owns/bypasses the intended boundary, while the helper exercises that owner as the application role. The source therefore does not prove realistic runtime denial, security-definer containment, or independent ownership.

## 6. Command issuance, consumption, retry, and direct-SQL resistance

`Rev869BCommandContextAuthorizer` signs employee, issuer, subject, role, organization, issue time, nonce, and transaction ID. It does not sign the operation/action, entity, expected version, source/target status, correlation, history record, or permitted mutation slot. `rev869b_claim_command_context` accepts those fields later from the caller.

Consequences:

- Different-operation, entity, expected-version, source/target-state, and history substitution are not cryptographically precluded by issuance.
- A runtime principal with execute access can author claims using an otherwise valid context.
- Transaction-local setting forgery is narrowed by signature verification, but a holder of the shared key remains an authority for every operation.
- Direct SQL by the database owner remains outside the tested runtime boundary.
- Success-path claim consumption is not durable across rollback or savepoint rollback.
- Connection pooling does not itself defeat transaction-ID binding, but it does not repair operation binding or rollback replayability.

No source test exercises savepoint creation, rollback-to-savepoint, then reuse. No source test proves a nontransactionally durable, operation-bound single-use grant while retaining atomic business history.

## 7. Database constraints, triggers, functions, and Down

Correction 11 improves technical-verification current-parent enforcement and qualification history aggregation. Static SQL generation remains deterministic, and the retained migration is singular and ordered correctly.

The current-version PO-line guard is nevertheless incompatible with amendment creation. The qualification tuple constraint may reject existing states without a preflight. The claim fingerprint and uniqueness design do not represent a semantic one-time operation slot.

The Down path removes the REV869B functions and owned tables and removes the new qualification constraint in a coherent static order. Shared `pgcrypto` remains retained. Static generation cannot prove catalog behavior, lock behavior, trigger execution, transactional restoration, or reversibility on PostgreSQL, and no such acceptance is claimed.

## 8. Audit, privacy, retention, and immutable history

Durable business history/audit relations remain separate from command contexts and claims. Raw per-context secrets were replaced by fingerprints, and an explicit 30-second authorization expiry was added. These are improvements.

However:

- The reusable signing key is still supplied across the database function boundary.
- Authority, context, and claim rows are durable physical records even after logical expiry.
- Principal and organization metadata have no approved purge lifecycle.
- No dedicated no-login owner or realistic runtime role protects the ledger from the database owner.
- No source demonstrates protected export/administration behavior for the owner boundary.
- No foreign-key/RLS or equivalent complete ledger lifecycle contract is established.

The 30-second authorization expiry is not being treated as a management-approved retention period. Management approval remains an external decision. Required durable audit history must not be deleted with temporary authorization material when a purge design is later implemented.

## 9. Quarantine ownership and recovery

Improvements include surfaced cleanup exceptions, explicit expected-owner and quarantine-state marker fields, higher-entropy naming, source verification before recovery, and fail-closed handling of uncertain state.

Remaining blockers are material:

- The recovery approval value is a reusable global phrase, not a signature or grant bound to the exact database, run, token, migration family, source, owner, scenario hash, and expiry.
- `ExpectedOwner=current_user` is self-attestation by the same database-owner identity used for provisioning/runtime.
- The stored scenario hash and provisioning timestamp are not part of recovery authorization or ownership validation.
- Source verification checks the database name and migration-ID shape, not an independently recorded expected schema/source fingerprint.
- A hard interruption before durable marker establishment leaves no independently recoverable proof.
- No durable sanitized external evidence record supports separately governed recovery.
- Forced database drop can terminate all connections to the named database; exact naming narrows scope but does not make broad connection termination safe.

The helper correctly refuses recovery when proof is uncertain. That safe refusal does not make it execution-ready.

## 10. PostgreSQL test-design reconciliation — NOT RUN

Exactly 22 PostgreSQL tests were discovered by listing only: 7 application-level and 15 direct-database tests. All 22 are **NOT RUN**.

The application group covers success, failure rollback, replay, denial/no disclosure, audit failure, independent-context concurrency, and mapped endpoint traversal. The direct-database group covers success, rollback, concurrency, idempotency, terminal-state rejection, snapshot mismatch, tampering, permission/audit behavior, skipped versions, late children, immutable history, PO revision ancestry, and object inventory.

| Required design property | Verdict |
|---|---|
| Explicitly owned isolated target and marker checks | Partial: exact naming/marker checks exist, but ownership evidence is self-attested and hard-interruption proof is absent. |
| Independent connections/DbContexts for concurrency | Present in source. |
| One committed winner and complete loser rollback | Partial: one-winner cases exist; independent complete final-state/loser/unrelated/security-ledger proof is incomplete. |
| Exact SQLSTATE and database object | Central helper requires typed PostgreSQL exception, SQLSTATE, and constraint or native object metadata. |
| Reject zero-row false positives | Present in the central exception assertion helper. Some concurrency evidence still relies on conflict outcomes without complete independent state proof. |
| Operation substitution | Missing complete matrix. |
| Rollback/savepoint replay | Transaction rollback is represented; savepoint rollback and durable reuse denial are missing. |
| Current-version behavior | Late-child negatives exist; valid amended-PO child insertion is not proven and is contradicted by source. |
| Qualification lifecycle | Canonical end-state data exists, but full legacy/rejection/correction and procurement traversal behavior is not proven. |
| Complete independent pre/post reads | Incomplete across the required winner/loser/replay/unrelated/quarantine/ledger matrix. |
| Cannot drop/repair/reuse unexpected database | Fail-closed checks improve this, but proof-bound durable ownership/recovery is incomplete and forced drop remains too broad. |

Method discovery itself proves only source inventory. It is not PostgreSQL behavioral evidence.

## 11. Permitted offline validation

| Validation | Result |
|---|---|
| Entry commit/parent/status | PASS; exact hashes and clean target scope |
| Correction diff scope | PASS; exactly 13 committed files |
| Build, `--no-restore` | PASS; 0 warnings, 0 errors |
| Focused REV869B non-PostgreSQL suite | PASS; 58 passed, 0 failed, 0 skipped |
| Complete non-PostgreSQL suite | PASS; 462 passed, 0 failed, 0 skipped |
| PostgreSQL discovery only | 22 discovered; **NOT RUN** |
| PowerShell 5.1 AST | PASS; 23 files, 0 parse errors |
| EF migration discovery, `--no-connect` | PASS; 13 migrations; REV869A then exactly one REV869B |
| Exact design model/snapshot parity | PASS; 1/1, no connection |
| Offline REV869A-to-REV869B Up SQL | 211,401 bytes; SHA-256 `1FDD7332D9323BEBE1D88C116EE11112886A4AD0AE72B9848B3E46B93F0D964C` |
| Offline REV869B-to-REV869A Down SQL | 8,312 bytes; SHA-256 `02F924F818916B50CF2EB3332C66979EECCFB2B97F388A8E2F28699986AB7E5C` |
| Static SQL inventory | 17 tables; 76 triggers; 24 function definitions / 23 unique functions; 46 FKs; 69 indexes; 40 check-pattern matches; balanced 50 revision and 2 extension delimiters |
| Authorization-operation scan | FAIL; issuance is principal/context-bound, not exact-operation-bound |
| Replay/savepoint/rollback scan | FAIL; transactional consumption is restored by rollback/savepoint rollback |
| Qualification consistency scan | FAIL; legacy and rejection/correction compatibility gaps |
| Current-version scan | FAIL; amended-PO line insertion regression |
| Ledger/privacy/ownership/retention scan | FAIL; reusable key boundary, owner role, and lifecycle gaps |
| Quarantine recovery scan | FAIL; no independent durable ownership or instance-bound recovery grant |
| Secret/prohibited-operation scan | No literal password/private/AWS key found in the correction; reusable signing-key flow remains. No prohibited operation was executed. |
| `git diff --check` on reviewed range | PASS |
| Temporary offline SQL artifacts | Removed; none retained |

Compilation, unit tests, parsing, discovery, and static SQL inspection establish source consistency only. They do not neutralize the demonstrated design defects or constitute PostgreSQL acceptance.

## 12. Required corrections and management decision

### BLOCKING / REQUIRED CORRECTION

1. Replace the principal-only shared-key context with an issuer-authorized, exact one-time operation slot binding organization, actor/role/issuer, entity type/ID, action/transition, expected version, source/target status, correlation, every permitted history slot, and expiry.
2. Define unambiguous type- and length-bound claim canonicalization. Enforce semantic uniqueness independently of caller-chosen history IDs.
3. Make consumption survive transaction and savepoint rollback without weakening atomic mutation/history behavior; add success, rollback, savepoint, retry, pooling, and replay proofs.
4. Reconcile qualification service predicates, database predicates, retained legacy tuples, rejection/correction lifecycle, and existing-data preflight.
5. Repair the current-version guard so legitimate amendment construction remains possible while late/superseded parent writes fail closed.
6. Assign security-definer functions to a dedicated no-login owner and test with a genuinely least-privilege runtime role distinct from migration/database owner.
7. Complete independent rollback fingerprints for every business, history, audit, authorization, sequence, and unrelated relation affected by each case.
8. Establish durable sanitized quarantine ownership evidence and separately governed, instance-bound, expiring recovery authorization; avoid broad forced connection termination.
9. Add the complete operation/entity/version/status/organization/actor substitution matrix, savepoint rollback replay, qualification lifecycle, valid amendment, and full winner/loser/replay/unrelated-state tests.

### EXTERNAL MANAGEMENT DECISION

Management must approve the security-ledger retention and purge policy, including privacy scope, audit preservation, protected access/export, deletion semantics, schedule, and evidence. No arbitrary duration is authorized by this review.

## 13. Exact next gate

The next authorized gate is a **twelfth controlled source-only REV869B correction** limited to the blockers and required corrections above, followed by a new independent source-only safety re-review of its committed diff. Until that re-review sets both canonical states to `PASS`, PostgreSQL tests, database helpers, migration apply/remove, provisioning, quarantine recovery, backup/restore, and production execution remain unauthorized by this review.

```text
rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
```
