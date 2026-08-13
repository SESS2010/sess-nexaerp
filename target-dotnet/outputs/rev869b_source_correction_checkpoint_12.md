# REV869B source correction checkpoint 12

Date: 2026-08-13 (Asia/Calcutta)

Authoritative workspace: `C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet`

Scope: controlled source-only correction; no PostgreSQL command, database helper, migration apply/remove, provisioning, quarantine recovery, backup/restore, or production operation was executed.

Starting commit: `614e41c1dfa773b6bd8f9974e823f06647cea7de`

Reviewed Correction 11 source commit: `510717a356c00958fbdbf89193242afa383dc0a9`

Ending commit: the commit containing this checkpoint. Its exact hash is recorded after commit in the final handoff; embedding a Git commit's own hash in its tracked contents would change that hash.

Commit message: `Perform twelfth controlled REV869B source correction`

## 1. Entry gate

The gate was checked before any file was changed.

| Gate | Exact evidence | Result |
|---|---|---|
| HEAD | `614e41c1dfa773b6bd8f9974e823f06647cea7de` | PASS |
| Reviewed Correction 11 commit | `510717a356c00958fbdbf89193242afa383dc0a9` | PASS |
| Independent report count at HEAD | exactly one report, `outputs/rev869b_preapply_source_safety_rereview_after_correction_11.md` | PASS |
| Target-scoped status | clean | PASS |
| Legacy reference | `?? ../legacy-reference/`; not read, changed, staged, or otherwise touched | PASS |
| Migration topology | REV869B exists exactly once immediately after REV869A | PASS |

The independent Correction 11 report, Correction 11 checkpoint, complete committed diff, and all files cited by that report were read before source mutation. The finding matrix below was prepared before editing.

## 2. Finding matrix and completion evidence

Severity mapping from the authoritative independent report:

| ID | Severity |
|---|---|
| C11-01 exact operation slot | BLOCKING |
| C11-02 canonicalization/history substitution | BLOCKING / newly discovered 4.1 |
| C11-03 rollback/savepoint/replay/pooling | BLOCKING |
| C11-04 qualification compatibility | BLOCKING |
| C11-05 current/late child | BLOCKING |
| C11-06 least privilege | BLOCKING |
| C11-07 privacy/minimization/retention boundary | REQUIRED CORRECTION |
| C11-08 complete rollback evidence | BLOCKING |
| C11-09 quarantine durability/recovery | BLOCKING |
| C11-10 test design completeness | BLOCKING |
| C11-11 temporary security-ledger retention | EXTERNAL MANAGEMENT DECISION |

| ID | Exact affected files / methods | Root cause | Required correction | Database enforcement | Application enforcement | Positive test | Adversarial test | Rollback evidence | Completion evidence |
|---|---|---|---|---|---|---|---|---|---|
| C11-01 exact operation slot | `Rev869BCommandContextSql.Install`; `Rev869BCommandContextAuthorizer.OpenForPendingChangesAsync`; protected history triggers | Principal-only reusable context did not pre-authorize a mutation/history slot | Issuer-reserved one-time slot binding claim kind, history, entity, operation, version, transition, principal, organization, correlation, remarks and expiry | `rev869b_issue_command_grant`, `rev869b_open_command_context`, `rev869b_claim_command_context`; exact and semantic slot fingerprints; 30-second expiry | Change-tracker collection of every pending protected history; distinct issuer connection; opening occurs immediately before protected save | Existing service success tests plus exact history helper paths | `ExactGrantRejectsEveryOperationSlotAndPrincipalSubstitution` | Context is business-transaction local; durable grant/sequence state is separately fingerprinted | build and focused source contracts pass |
| C11-02 canonicalization/history substitution | `Rev869BCommandContextSql.rev869b_slot_fingerprint` and grant issuance | Newline `concat_ws` encoding was null-ambiguous and semantic uniqueness depended on caller history ID | Structured typed positional encoding; semantic uniqueness independent of history ID | SHA-256 of `jsonb_build_array(...)::text`; exact fingerprint includes history; semantic fingerprint excludes history; duplicate semantic slot rejected | Duplicate semantic slots rejected before issuance | Exact slot issue/claim cases | history-ID and all tuple substitutions are enumerated | Failed claim consumes ordinal fail-closed | static fingerprint/semantic contracts pass |
| C11-03 rollback/savepoint/replay/pooling | command grant/context SQL; direct/app PostgreSQL test sources | Transactional nonce consumption was restored by rollback/savepoint | Nontransactional, grant-isolated claim ordinal; backend, transaction and principal binding | Fixed 256-sequence migration-owned pool; issuance records baseline; claim uses `nextval`; expected ordinal is part of issued slot; backend PID, `txid_current`, `session_user` and grant uniqueness are checked | Issuer pooling disabled; issuer database must equal runtime database; issuer username must differ; runtime transaction must already exist | Existing success/idempotency/concurrency sources | `SavepointRollbackCannotRestoreConsumedExactClaim`; exact grant replay and connection binding contracts | business rollback equality plus owner-only durable grant/pool/sequence evidence | compiled/discovered only; PostgreSQL not run |
| C11-04 qualification compatibility | `Rev869BControlledMutationSql`; `Rev869BDatabaseSafetySql`; `Rev869AConfigurationEndpoints`; `EfRev869AFoundationServices`; RFQ qualification predicate | Service, database and retained tuple predicates disagreed; rejection/correction states and preflight were incomplete | Align Verified/Approved compatibility; add preflight, reject and correction lifecycle | lifecycle CHECK; existing-state preflight; exact same-transaction history; actor/role/SoD/deactivation; provenance accepts retained `Approved/Approved` and current `Verified/Approved` | create, normalize, verify, approve, reject and request-correction routes; version and independent actor checks | endpoint PostgreSQL source covers create/verify/approve/reject/request-correction | creator, verifier, stale version, scope and audit failure cases | qualification/history/audit relations included in independent fingerprints | build and static lifecycle contracts pass |
| C11-05 current/late child | `Rev869BDatabaseSafetySql.rev869b_guard_child_insert`; `EfRev869BPurchaseService.AmendPurchaseOrderAsync` | Requiring `IsCurrentVersion=true` rejected legitimate same-transaction noncurrent amendment construction | Admit only the exact newly inserted amendment parent; reject late or superseded parents | PO line accepts current editable parent or noncurrent parent with `PreviousVersionId` and current-transaction `xmin`; terminal/current guards remain | Amendment creates parent and lines together before histories and commit | service amendment source plus static positive predicate contract | terminal aggregate late-child matrix | parent/children/histories included in rollback fingerprint | focused contracts pass |
| C11-06 least privilege | migration command SQL; `Rev869BTestDatabaseLease` | Database owner was runtime and owned security-definer objects | Dedicated NOLOGIN security owner; distinct non-owner LOGIN runtime and issuer | migration preflight requires pre-provisioned NOLOGIN owner and migration-owner membership; tables, fixed sequences and six functions transfer ownership; PUBLIC revoked | disposable roles use NOSUPERUSER/NOCREATEDB/NOCREATEROLE/NOREPLICATION; runtime gets business DML only; security ledgers, sequence pool and ownership marker are revoked | source verifies exact owners and role separation | direct attempts have no owner connection; issuer/runtime mismatch fails closed | owner-only security-state reader separates durable ledger proof from business rollback | build/static ownership contracts pass |
| C11-07 privacy/minimization/retention boundary | command ledger DDL; `rev869b_guard_durable_audit_retention`; helper permissions; this checkpoint | Reusable signing key crossed SQL; ledger access was excessive; durable audit and temporary claim material were not separated | Remove reusable key; fingerprint minimum business identifiers; enforce established ten-year durable-audit minimum; protect ledgers; leave temporary-claim purge duration to management | exact grants retain fingerprints only; audit UPDATE is forbidden; DELETE before ten years is forbidden; expired cleanup requires database owner, bounded reason/correlation, and inserts minimized `PurgeExpiredAudit` evidence | distinct issuer only; runtime cannot read/write ledgers and cannot UPDATE/DELETE audit logs | ten-year source contract; catalog inventory; runtime privilege test design | principal/organization/actor/role/slot substitutions and runtime table access reject | durable audit remains in business fingerprint; grant/sequence consumption is separately durable | durable audit boundary implemented; temporary claim retention remains an external decision |
| C11-08 complete rollback evidence | `Rev869BPostgresApplicationBehaviorTests.OwnedRfqFixture` | Prior equality omitted grants and claim sequences, making durable consumption invisible | Separate business/authorization evidence and enumerate unrelated relations | owner-only snapshot counts authorities, grants, contexts, pool size/occupancy and hashes all claim sequence states | business state fingerprint retains parents, children, histories, audits, number sequence, qualification, authorization context and unrelated relations | winner and service success sources | audit failure, replay, collision, stale writer and denial sources | rollback tests assert exact business equality, unchanged authorities/contexts/pool, one durable grant/reservation, changed sequence fingerprint | compiled/discovered only; PostgreSQL not run |
| C11-09 quarantine durability/recovery | `Rev869BTestDatabaseLease` evidence, disposal and recovery methods | Ownership proof existed only inside disposable DB; recovery phrase was global; source proof was weak; FORCE could terminate unrelated sessions | Database-backed marker plus signed sanitized external evidence; exact source/migration catalog fingerprints; instance-bound HMAC approval; exact marker/time/role binding; regular DROP only after zero connections | owner-only marker binds database/run/source name and SHA-256/catalog fingerprint/migration ID and fingerprint/family/scenario/owner/provisioning time/state | source fingerprint is recomputed from migration history plus REV869B function/trigger definitions; fully-qualified protected evidence directory; external 256-bit key; ownership token stored externally only as SHA-256 | normal owned cleanup source | fingerprint/signature/marker mismatch, active connections and broad termination fail closed | cleanup surfaces failure and records `Quarantined`; evidence is retained through `Dropped` | no `WITH (FORCE)` in helper; static quarantine contract passes |
| C11-10 test design completeness | direct and application PostgreSQL source tests; static contract tests | Substitution, savepoint, qualification and durable rollback cases were absent | Add and require cases without running the forbidden gate | exact constraint/SQLSTATE assertions encoded | endpoint/service paths use real authorizer | qualification positive lifecycle and existing winner/revision cases | complete slot/principal substitution and savepoint replay cases | independent business and security snapshots | PostgreSQL tests compile and list; they were not executed |
| C11-11 temporary security-ledger retention | external management | The authoritative review forbids inventing a retention period for expired command grants, contexts, claim-sequence reservations and quarantine envelopes | Keep temporary authorization cleanup disabled until management approves privacy scope, protected export, deletion semantics, schedule and evidence; never delete required ten-year audit evidence | no temporary-ledger purge function or schedule is added; owner-only access and fingerprints minimize exposure | no runtime/issuer cleanup path | n/a pending decision | unauthorized runtime cleanup rejects | claim consumption remains durable and visible to owner-only verification | OPEN EXTERNAL DECISION; the established ten-year durable-audit rule is separate and enforced |

## 3. Correction design

### Exact authorization and durable consumption

The reusable `REV869B_COMMAND_SIGNING_KEY` path is removed. A distinct issuer reserves a grant for an exact runtime principal, backend PID, transaction ID, actor, OIDC issuer/subject, role, organization and ordered set of exact operation/history slots. The runtime may open that grant only once on the bound connection and transaction.

Each grant leases one pre-created sequence from a fixed pool owned by `nexa_rev869b_security_owner`. Sequence advancement is nontransactional. Therefore a claim attempt consumes its ordinal even if a statement, savepoint or transaction rolls back. The issued slot includes its expected ordinal, so retry, substitution and reordering fail closed. No runtime schema creation privilege is granted.

### Qualification and child compatibility

Qualification preflight now rejects unknown retained tuples before the constraint change. Canonical and retained approved tuples are aligned across service, database and eligibility checks. Rejection and management correction create exact immutable histories and deactivate the record. The PO-line guard admits a noncurrent amendment only while that parent was created in the same transaction and has a previous version; later inserts remain rejected.

### Security ownership, privacy and quarantine

The migration requires a pre-provisioned NOLOGIN security owner. Runtime and issuer are distinct least-privilege LOGIN roles in disposable source tests, while security ledgers, claim allocation and ownership marker are explicitly revoked. Raw application signing secrets no longer cross SQL or persist. Business identity, role and organization values in per-command grants are hashed; unavoidable database-role authority mappings remain owner-only administration data.

Durable `audit_logs` evidence is immutable and retained for at least ten years. Normal runtime has INSERT/SELECT but explicit UPDATE/DELETE revocation. A row older than ten years can be removed only by the database owner after setting a bounded exact cleanup reason and correlation; each removal inserts a new minimized `PurgeExpiredAudit` audit row containing the deleted row ID only as a SHA-256 fingerprint. This is independent of temporary authorization-ledger retention, whose duration remains unapproved.

Quarantine evidence is a sanitized HMAC-signed external envelope. Recovery re-verifies the exact isolated source, target name, marker, migration, run, scenario, owner, provisioning time, runtime/issuer roles and an instance-bound HMAC approval. Active connections cause refusal; `DROP DATABASE ... WITH (FORCE)` is absent.

## 4. Source validation

| Validation | Result |
|---|---|
| `dotnet build SESS.NexaERP.slnx --no-restore` | PASS; 0 warnings, 0 errors |
| PowerShell 5.1 AST parse | PASS; 23/23 repository tool scripts, PowerShell `5.1.19041.6456` |
| Focused REV869B contracts/behavior | PASS; 44/44 |
| Complete non-PostgreSQL suite (`FullyQualifiedName!~Postgres`) | PASS; 433/433 |
| PostgreSQL test discovery | PASS; 25 REV869B tests compile and list, including exact substitution, savepoint replay, qualification, rollback, privacy and privilege cases |
| PostgreSQL execution | NOT RUN; forbidden by the authoritative review |
| EF discovery | PASS with `--no-connect`; 13 migrations, REV869A immediately followed by exactly one REV869B; `applied` remains unknown/null |
| Pending-model/model-snapshot parity | PASS; exact offline parity test 1/1 |
| Offline Up SQL | PASS; 227,526 bytes; SHA-256 `741278C63AFDE04459A2A0240F6C5AF835AC195574FC7143E6E3CBEC751C48D5` |
| Offline Down SQL | PASS; 9,108 bytes; SHA-256 `48B5E8B99C23A53E6724DA901E86590FB913AD9F22E3BE1694EC542552735FFA` |
| Up inventory | 19 tables; 77 triggers; 27 function definitions / 26 unique functions; 46 FKs; 69 indexes; 42 CHECK patterns |
| Down inventory | 2 triggers; 1 function definition; command context, sequence pool and retention objects have coherent removal order |
| Operation-binding scan | PASS; organization, actor, role, issuer, operation, entity, version, from/to state, correlation, history and transaction bindings present |
| Replay/savepoint/rollback scan | PASS; nontransactional ordinal, baseline, backend/transaction/principal binding and exact replay error present |
| Qualification-state consistency scan | PASS; retained approved, current verified/approved, reject, correction, SoD and preflight paths aligned |
| Current-version coverage scan | PASS; filtered current uniqueness, superseded exclusion, cross-organization provenance and same-transaction amendment exception present |
| Runtime ownership/privilege scan | PASS; NOLOGIN security owner; distinct non-superuser runtime/issuer; ledger, marker, sequence-pool and audit mutation revocations |
| Ledger privacy/retention scan | PASS; fingerprinted business identifiers, ten-year audit minimum and controlled cleanup evidence |
| Quarantine safety scan | PASS; database marker plus signed external evidence, exact target/provisioning proof, instance-bound HMAC approval, no broad termination |
| `git diff --check` | PASS |
| Signing-key / FORCE / broad sequence-grant scan | PASS; no active reusable command signing key, forced drop, or runtime all-sequence grant |

Key SHA-256 source hashes before commit:

- `Rev869BCommandContextSql.cs`: `921BD3BC10DA35600E716E5F555CDCA7359F2566B96FA52F2A28457C06EFE76F`
- `Rev869BControlledMutationSql.cs`: `155F3A7D10E7AD5988ABE8C29F846D0104ED07DB4A7F85E93177FAB159B0B0BE`
- `Rev869BDatabaseSafetySql.cs`: `52D50478FD178D73EF7981FE9909A6B5704ACB951D97D7579076B819F1FCE713`
- `Rev869BCommandContextAuthorizer.cs`: `46DC285CF6639D9525CEFCB849C2B128A6FF5FC55F4C581F3CECDC9213D5EDC1`
- `Rev869BTestDatabaseLease.cs`: `9B354A225DCC3E1FF210674A960D9142C98930EA9489141DF26B040836BF593E`

## 5. Exact committed file scope

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

`../legacy-reference/` is outside this scope and remained untracked and untouched.

## 6. Workflow, permission, model and migration impact

- Workflow: protected purchase saves now reserve exact pending history slots immediately before `SaveChanges`; qualification reaches create, normalize, verify, approve, reject and request-correction states with exact history and SoD.
- Permission: command security objects use a dedicated NOLOGIN owner. Disposable execution design uses distinct issuer/runtime LOGIN roles with no superuser, database creation, role creation, replication, security-ledger, claim-pool, marker, audit UPDATE or audit DELETE privilege.
- Privacy/retention: no reusable application signing secret crosses SQL. Per-command business identity fields are irreversible fingerprints. Durable audit is immutable for ten years minimum and cleanup is owner-only, reason/correlation-bound and self-auditing.
- Model: no EF entity shape changed in this correction. The retained migration designer, snapshot and runtime model remain synchronized; exact pending-model parity passed.
- Migration: the retained REV869B migration ID and topology are unchanged. Source-managed SQL adds exact grant ledgers/sequence pool, qualification/currentness corrections and durable-audit retention. Up/Down SQL was generated only offline and never applied.
- Rollback: business/history/audit/unrelated state and security grant/context/pool/sequence state are captured separately from independent connections so durable claim consumption is not mistaken for business rollback leakage.

## 7. Remaining blockers and explicitly unclaimed states

Remaining blockers outside this controlled source correction:

1. A new independent source-only safety re-review must inspect the committed Correction 12 diff and set any canonical state; this checkpoint does not do so.
2. Management must approve retention/purge policy for temporary command grants, contexts, sequence reservations and quarantine evidence, including privacy scope, export, deletion semantics, schedule and proof. No period was invented.
3. PostgreSQL behavior, helper readiness, migration acceptance and production readiness remain untested and unauthorized.

Explicitly unclaimed:

- `rev869b_source_safety_state=PASS`
- `rev869b_execution_helper_readiness_state=PASS`
- PostgreSQL acceptance
- migration acceptance
- production readiness
- final REV869B acceptance

## 8. Controlled boundary and next gate

This checkpoint does not declare either canonical state `PASS`. It supplies the twelfth source-only correction candidate for independent review. The established ten-year durable-audit minimum is enforced; management approval for temporary claim-ledger retention remains external and no arbitrary period was invented.

The exact next gate is a fresh independent source-only safety re-review of the committed Correction 12 diff. Until that review explicitly authorizes the next state, do not run PostgreSQL tests or helpers, apply/remove migrations, provision roles or authorities, perform quarantine recovery, backup/restore, or execute against production.
