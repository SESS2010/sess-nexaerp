# REV869B pre-apply source safety re-review after Correction 13

Date: 2026-08-13 (Asia/Calcutta)

Review type: fresh independent source-only safety re-review

Authoritative workspace: `C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet`

Reviewed commit: `fd2fb607756d2de0db8be773fa6f7e874c5440e9`

Parent: `bce0b13f8a8caf5d4f22a7140c76cfbfbba2f414`

Reviewed subject: `Perform thirteenth controlled REV869B source correction`

No PostgreSQL test, PostgreSQL connection, database helper, migration apply/remove, purge, scheduler, provisioning, quarantine recovery, database create/drop, production operation, REV861, frontend, REV869C, or `legacy-reference` content was accessed or executed.

## 1. Entry gate and exact scope

The gate passed before review: exact HEAD, parent and subject; exactly 11 controlled files; target-scoped status clean; one REV869B immediately after REV869A; non-authoritative sibling `192d84fa1116975a09e9676a7d8c864f975380f5` absent from ancestry; only the two pre-existing untracked legacy ZIP paths outside target. Legacy content was not read or touched.

Exact commit scope:

1. `outputs/rev869b_source_correction_checkpoint_13.md`
2. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs`
3. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BControlledMutationSql.cs`
4. `src/SESS.NexaERP.Infrastructure/Persistence/Rev869BCommandContextAuthorizer.cs`
5. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.ComparisonPo.cs`
6. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.MaterialFollowUp.cs`
7. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.RfqQuotation.cs`
8. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs`
9. `tests/SESS.NexaERP.Tests/Rev869BDatabaseSafetyContractTests.cs`
10. `tests/SESS.NexaERP.Tests/Rev869BPurchaseCorrectionTests.cs`
11. `tests/SESS.NexaERP.Tests/Rev869BTestDatabaseLease.cs`

The checkpoint, prior independent report, complete 1,048-line Correction 13 diff and all cited authorization, migration, purchase, qualification, quarantine, ledger and PostgreSQL-test contracts were read completely. Checkpoint conclusions were not treated as acceptance evidence.

## 2. Canonical verdict

```text
rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
```

Source safety fails because durable target ownership, recovery authorization/audit, per-command durable security audit, purge execution authorization/privilege/evidence and mandatory PostgreSQL test design remain materially incomplete. Helper readiness independently fails because quarantine/recovery remains unsafe for required interruption states, policy execution is not fully fail-closed, and the source-defined purge cannot establish its required audit evidence under the shown privileges.

Neither state authorizes PostgreSQL, helper, migration, purge, recovery, or production execution.

## 3. New findings

### C13-N01 — pre-marker ownership remains filesystem-first and is not database-backed — BLOCKING

`Rev869BTestDatabaseLease.CreateAsync` writes a signed `PreCreateIntent` file before roles and database creation. That narrows the old evidence-free window, but it is still filesystem evidence. The target database receives its marker only later in `EstablishMarkerAsync`, after `CREATE ROLE`, `CREATE DATABASE`, `GRANT CONNECT`, lease construction and a new target connection.

Recovery explicitly accepts `PreCreateIntent` when the live target has the expected database owner and no marker. The live checks prove name, owner, retained migration/source shape and marker absence; they do not prove that this database was created by this exact lease/run. Filesystem intent plus generic clone shape therefore substitutes for the required target database-backed ownership marker. A hard interruption during database creation, role creation, marker creation, or before marker commit still has no target-owned run/correlation marker. Roles-only interruption also has no complete recovery state machine.

Required correction: establish database-backed ownership as part of the earliest atomic target-creation/provisioning boundary, or use an independently durable database-authority registry that cannot be satisfied by filesystem evidence plus clone shape. Model and test every creation/marker/preparation/execution/verification/cleanup/drop-preparation interruption state. Never authorize reuse, repair or DROP from filesystem evidence alone.

### C13-N02 — recovery authorization omits issuer, exact pre-state and outcome audit — BLOCKING

`RecoveryAuthorization` contains authorization ID, nonce, purpose, issued-at, expiry and signature. The canonical HMAC binds target/run/token hash/family/scenario/source/migration/owner/provisioning/roles, but it does not include an authorization issuer/authority identity or the exact evidence pre-state (`PreCreateIntent` versus `Quarantined`). The consumed record likewise has no issuer, requested pre-state, result, failure reason or completed state.

`FileMode.CreateNew` makes an authorization ID one-use before target verification, which is a meaningful replay improvement. However, every failed attempt writes only a generic consumed file and then throws; no durable signed success/failure outcome or fail-closed state transition is recorded. Successful completion changes quarantine evidence to `Dropped`, but the authorization-consumption record is not finalized with outcome. No PostgreSQL test design exercises expiry, replay, wrong action/database/run/owner/marker/fingerprint/pre-state or interrupted consumption.

Required correction: bind an explicit authorized issuer/authority, exact marker identity, exact pre-state and recovery action into the signed canonical payload; use a durable state machine recording reserved/consumed/succeeded/failed with timestamps and minimized reason evidence; ensure failed attempts remain non-reusable; add exact adversarial designs.

### C13-N03 — purge is not separately authorized per execution and cannot prove required failure evidence — BLOCKING

`rev869b_purge_temporary_security_ledger` hard-codes the management policy reference and accepts retention, batch, reason and correlation. Installation grants EXECUTE permanently to the migration `current_user`. There is no per-execution approval ID, approver/authority, issue/expiry, exact scope/cutoff, nonce, signature, one-use consumption or separate future-execution authorization. Possession of the migration-owner session is treated as approval.

The function writes a success aggregate only after deletion. `EXCEPTION WHEN OTHERS THEN RAISE` adds no failure evidence; all transactional evidence would roll back on error. A zero-candidate invocation returns without recording approval, executor, cutoff, pre-count or outcome. `candidate_count` is the bounded selected count, not an independently captured total eligible pre-count. The retained-audit count is a count of all audit rows within ten years, not proof that exact per-command durable audit evidence remains.

Required correction: require a fresh, exact, one-use, separately authorized purge execution grant; capture total eligible pre-count and bounded candidate count; record zero/success/failure outcomes durably without weakening transaction rollback; bind cutoff and scope; prove partial failure and concurrency behavior.

### C13-N04 — purge SECURITY DEFINER owner lacks demonstrated audit INSERT authority — BLOCKING

The purge function is transferred to `nexa_rev869b_security_owner` and runs as SECURITY DEFINER. Source grants its execution to migration `current_user`, but no source grant gives the NOLOGIN security owner INSERT on `nexa.audit_logs`, and that table is not transferred to the security owner. The required precondition establishes that migration owner is a member of the security owner role, not that the security owner inherits database-owner table rights.

Consequently the function has no demonstrated ability to insert its mandatory `PurgeTemporarySecurityLedger` audit row. A permissions failure after its deletes would roll the transaction back, making the purge unusable and producing no failure evidence. Compilation and SQL generation do not validate this runtime privilege path.

Required correction: define least-privilege, explicit audit-write authority that cannot mutate/delete/read unrestricted audit content; prove function owner, caller and runtime denial behavior with exact PostgreSQL tests.

### C13-N05 — durable per-command security audit separation is absent — REQUIRED CORRECTION / BLOCKING

Temporary grants hold issuer/runtime, organization/actor/identity/role fingerprints, slots, issued/expiry/reserved times; contexts hold opened time and claims. The 90-day purge deletes contexts and grants. No separate durable security-audit relation or exact audit event is written for command issue, open, claim/consumption, expiry and final outcome with operation/entity/correlation evidence. Application business audit rows do not supply the complete command authorization lifecycle, and savepoint/full rollback can remove context claims while only sequence movement survives.

The helper grants runtime `SELECT,INSERT,UPDATE,DELETE ON ALL TABLES IN SCHEMA nexa`, then revokes all on four command-ledger tables and revokes only UPDATE/DELETE on `audit_logs`. Runtime therefore retains broad SELECT on durable audit unless another unseen privilege boundary overrides it. That is inconsistent with the approved no-unrestricted-ledger-read/export boundary.

Required correction: create structurally separate minimized durable command-security audit evidence retained at least ten years; write issue/consume/expire/outcome events outside rollback-restorable state; prohibit ordinary runtime unrestricted read/export; define separately approved exceptional export.

### C13-N06 — mandatory PostgreSQL test design was not expanded for Correction 13 — BLOCKING

The same 25 PostgreSQL tests are discovered. None names or implements hard interruption, pre-create intent, marker-bound recovery, recovery expiry/replay/substitution, purge authorization, purge count mismatch/partial failure, purge owner privilege, durable purge audit, or runtime purge denial. The two new non-PostgreSQL contracts count strings and call counts; they do not prove per-flow ordering or PostgreSQL behavior.

Existing tests materially cover exact slot substitution, savepoint replay, ordinary ledger denial, transaction rollback, independent concurrency, terminal guards and exact SQLSTATE/object checks. They do not cover full-transaction old-grant replay, pooled connection reuse, process interruption, every valid/invalid workflow, complete runtime DDL/ownership/self-grant denial, or the new recovery/purge contracts.

Required correction: add compiled/listable, exact-evidence PostgreSQL designs for all mandatory scenarios without executing them during correction or re-review.

## 4. Finding matrix for prior blockers

| Finding | Prior severity | Correction 13 implementation and exact location | Application/database enforcement | Positive/adversarial evidence | Verdict | Remaining risk |
|---|---|---|---|---|---|---|
| C11-01 / C12-N01 authorization before mutation | BLOCKING | purchase partials stage histories and call `OpenPendingAuthorizationAsync` before every set-based parent update; creation uses `SaveAuthorizedChangesAsync` | exact future slots now use persisted version + 1; history claims remain deferred/atomic | source trace is coherent; static test only counts 19 opens/18 saves; application PG tests cover a narrow subset | PARTIAL | no executable design proves every listed workflow or first database mutation ordering |
| C11-02 slot canonicalization/substitution | BLOCKING | typed JSON slot fingerprint retained; quotation Draft insert maps to `Create` | organization, employee, issuer/subject, role, operation, entity, version, statuses, correlation, remarks, backend/transaction and ordinal are bound | exact substitution test exists with structured SQLSTATE/constraint | PASS in static design | PostgreSQL behavior remains NOT RUN; no acceptance claim |
| C11-03 rollback/replay/pooling | BLOCKING | opens exact grant before set-based update; nontransactional ordinal retained | grant/context backend, transaction, session, expiry and slot checks retained | savepoint and concurrency designs exist | PARTIAL | full rollback old-grant replay, pool reuse, process interruption and comprehensive retry remain unproved |
| C11-04 qualification compatibility | BLOCKING | qualification endpoints already open pending authorization before save; Correction 13 preserves them | lifecycle/SoD guards retained | source test design includes qualification cases but no legacy normalization/retained Approved/Approved executable positive | PARTIAL | retained-data compatibility remains unproved |
| C11-05 current-version/late-child | BLOCKING | amendment/revision creation histories moved into authorized save; set-based transitions preauthorized | current/terminal child guards retained | late-child and rejected revision tests exist | PARTIAL | valid amendment and complete dependent-path PostgreSQL proof remain incomplete/NOT RUN |
| C11-06 least privilege | BLOCKING | purge becomes seventh owner function; runtime ledger revocations retained | NOLOGIN owner/source checks exist | ordinary ledger/audit mutation denial exists | PARTIAL | purge audit privilege is missing; runtime DDL/trigger/schema/ownership/self-grant matrix incomplete |
| C11-07 privacy/data minimization | REQUIRED CORRECTION | separate recovery key and export-disabled configuration added | command ledgers contain fingerprints, not raw credentials | secret scan finds no committed secret/private key/raw assertion | FAIL | durable command audit absent; runtime retains audit SELECT; exceptional export contract absent |
| C11-08 rollback completeness | BLOCKING | business ordering is improved | transactional business/history/audit rollback retained; ordinal is nontransactional | existing application/direct snapshots are useful | FAIL | new purge/recovery and all workflow rollback states are not covered |
| C11-09 / C12-N02 quarantine ownership | BLOCKING | signed `PreCreateIntent` file before CREATE | later database marker remains exact | normal marker verification is strong | FAIL | pre-marker proof is not database-backed; roles-only and lifecycle interruptions lack safe recovery |
| C11-10 / C12-N03 recovery freshness | BLOCKING | nonce/ID/purpose/time bounds, distinct key, atomic one-use file | target tuple HMAC and active-connection refusal | meaningful freshness/replay improvement | FAIL | no issuer/pre-state binding, complete outcome audit or adversarial PG design |
| C11-11 PostgreSQL design | BLOCKING | no PostgreSQL test files changed | existing 25 compile/list | good structured-error helpers remain | FAIL | mandatory Correction 13 quarantine/recovery/purge tests are absent |
| C11-12 retention decision | EXTERNAL / BLOCKING | exact management policy config gate and future purge source added | 30-second authorization is within 15-minute maximum; 90-day selection; ten-year audit trigger retained | static strings and offline SQL generation pass | FAIL | purge execution authorization, privilege, failure evidence and durable per-command audit are incomplete |
| C12-N04 checkpoint reproducibility | IMPROVEMENT | exact filters/totals recorded | not applicable | independently reproduced | PASS | documentation does not establish safety |

## 5. Workflow and authorization ordering conclusion

Correction 13 materially repairs the immediate mutation-before-context defect. RFQ issue, invitation submit, quotation supersede/finalize, comparison recommend/resubmit/approve, PO submit/issue/approve/cancel and material-follow-up transitions stage the exact histories, open the pending authorization, execute protected mutation, save histories, write audit and commit. Reserve helpers follow the same pattern. Creation paths stage number sequence, parent/children and history in the change tracker, and the first database save opens authorization internally.

The authorizer's persisted-parent fallback now requests version + 1, aligning set-based parent updates with history trigger expectations. Operation-slot binding remains strong. This conclusion is source-design evidence only. The committed PostgreSQL design does not prove every required workflow and ordering boundary, so the overall finding is PARTIAL and remains blocking through C13-N06.

## 6. Replay and rollback conclusion

Exact slot, backend, transaction, principal, expiry and nontransactional ordinal mechanisms remain credible. Savepoint reuse has a focused design. Full transaction replay, pooled connection reuse, process interruption and complete winner/loser/retry state equality are not comprehensively designed. Durable command consumption/outcome evidence is absent once temporary rows are purged. Verdict: PARTIAL / BLOCKING.

## 7. Quarantine and recovery conclusion

Normal marker-bound cleanup is conservative: exact high-entropy name, marker fields, source/migration proof, owner, no active connections, ordinary DROP and role cleanup. The new pre-create intent and fresh one-use file improve safety but do not meet the required database-backed earliest ownership or issuer/pre-state/outcome-bound recovery contract. Verdict: FAIL.

## 8. Ledger retention, access, export and purge conclusion

The source separates temporary grant/context rows from general `audit_logs` and permanent business histories. It constrains grant validity to 30 seconds, temporary purge age to 90 days and durable audit deletion to at least ten years. The purge targets only command contexts, pool assignments and grants; no purchase, approval, status or qualification table is targeted. It is bounded, row-locked, advisory-serialized and transactionally count-checked; migration installation does not invoke it and no job is activated.

Those positives do not close C13-N03 through C13-N05: no separate execution approval, no durable failure/zero-candidate evidence, no demonstrated audit INSERT privilege for the function owner, no durable per-command lifecycle audit and no complete runtime read/export boundary. Verdict: FAIL. No purge execution is authorized.

## 9. PostgreSQL test-design assessment

Exactly 25 REV869B PostgreSQL tests were independently listed and **NOT RUN**: 7 application tests and 18 direct tests. Existing structured error assertions reject zero-row false positives and inspect SQLSTATE/constraint/table evidence in important paths. The design is not capable of proving every mandatory Correction 13 scenario because no quarantine/recovery/purge test was added. Verdict: FAIL.

## 10. Independently reproduced offline validation

| Validation | Independent result |
|---|---|
| Build | PASS; 0 warnings, 0 errors |
| Focused three-class REV869B | PASS; 44/44 |
| Inclusive REV869B non-PostgreSQL | PASS; 61/61 |
| Complete non-PostgreSQL | PASS; 435/435 |
| PostgreSQL behavior discovery | 25 discovered; **NOT RUN** |
| PowerShell 5.1 AST | PASS; 23/23; `5.1.19041.6456` |
| EF migration discovery | PASS with `--no-connect`; 13 migrations; REV869A immediately followed by one REV869B; applied state unknown |
| Pending model/snapshot parity | PASS; 1/1 exact offline test |
| Offline Up SQL | 231,112 bytes; SHA-256 `25C23AF75B339E3FC106372396A29F9D83377424ACB16E439F22D522D16A2EDD` |
| Offline Down SQL | 9,205 bytes; SHA-256 `DB39C1A36405A3C40763F9D40589B5B86E770F766D882822E044DF409F90CF36` |
| SQL inventory | 19 tables; 77 triggers; 28 function definitions / 27 distinct names; 46 FK clauses; 69 indexes; 42 CHECK clauses; 48 Down DROP lines |
| Secret/prohibited-operation scan | no committed credential/private-key pattern, FORCE, broad termination, wildcard DROP or business-history purge introduced; textual `raw token` and delete hits are policy/assertion text, not prohibited operations |
| Correction 13 `git diff --check` | PASS |

Offline generation and inspection do not parse or execute PostgreSQL SQL and do not establish PostgreSQL behavior.

## 11. Remaining execution gates and next action

All database/helper/migration/purge/recovery/production gates remain closed. PostgreSQL behavior, migration applicability, purge privilege/atomicity, quarantine recovery and runtime ownership are not accepted.

Exact next authorized action: perform a fourteenth controlled source-only correction against this report. Correct all BLOCKING and REQUIRED CORRECTION findings, expand (but do not execute) the PostgreSQL design, reproduce offline gates, create a new checkpoint and commit only controlled source changes. Then perform another fresh independent source-only safety re-review. No PostgreSQL, helper, migration or purge command is authorized.
