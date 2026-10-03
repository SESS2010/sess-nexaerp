# REV869B Source Correction Checkpoint 16

Date: 2026-08-14 (Asia/Calcutta)

Starting commit: `48c84086dedcc25a6a4c1dd2bdd1c999e426b7dd`

Ending commit: the commit containing this checkpoint (a commit cannot contain its own SHA-1 without changing it).

Required subject: `Correct REV869B control-plane safety checkpoint 16`

Scope: controlled source-only correction. PostgreSQL, PostgreSQL tests, migrations, provisioning, database lifecycle, recovery, purge, export, production, REV861, AWS, production OIDC, frontend, Docker, legacy applications and `../legacy-reference/` were not accessed or executed.

## Entry gate

HEAD, parent and subjects matched the required Correction 15 report/source chain. The Correction 15 report hash was `BF1936710E7BBAB850AC9551AE11449BF0E970A31D786757289AF132F9ABFA05`. Target-scoped status was clean. EF `--no-connect` returned 13 migrations with one REV869B immediately after REV869A. The complete Correction 15 checkpoint and independent report were read. The sibling legacy boundary remained the single untracked `../legacy-reference/` path; its contents were not enumerated or read.

## Exact six-finding matrix

| Finding | Root cause/failure | Implemented source correction | Acceptance formula |
|---|---|---|---|
| C15-N01 - BLOCKING | Control-plane readiness used function names/argument counts and object counts, allowing wrong overload/result/schema/trigger contracts. | Added `Rev869BControlPlaneProvisioningContract`: seven exact identity signatures/results, four relations, safe modes, exact target guard, ownership/ACL/default-privilege manifest and catalog predicate. Registry readiness now consumes that exact predicate. | Exactly 7 unique APIs + exact types/results/owner/SECURITY DEFINER/search path/ACL; exactly 4 owned tables + exact immutable triggers; otherwise no registry use. |
| C15-N02 - BLOCKING | Pre-marker registry/file states diverged; request/provision times were conflated; post-DROP outcome failure attempted to reopen an absent target. | Supplemental intent precedes authoritative reservation; pre-marker failure writes matching Quarantined evidence; recovery separately binds lease request and marker provision time; post-drop reconciliation proves absence and finalizes the same attempt without target access. | Exact predecessor + exact two timestamps + registry attempt + catalog/marker proof; absence finalizes same DropStarted attempt, presence remains quarantined. |
| C15-N03 - BLOCKING | Purge evidence could roll back with caller work; bad nonce destroyed a valid approval; claimed/consumed metadata never became eligible. | Added `Rev869BPurgeCoordinator` with fresh non-pooled role-specific autocommit phases. Rejected probes no longer mutate approval. Eligible selection includes terminal consumed grants older than the approved cutoff while durable audit is excluded. | Fresh authorization -> committed begin result -> bounded execute on a new autocommit connection -> committed terminal evidence; caller rollback cannot restore approval or erase phase evidence. |
| C15-N04 - BLOCKING | Durable command audit lacked database/execution/service/authorization/attempt identity and rollback erased open/claim evidence. | Added immutable `rev869b_command_consumption_attempt_audits` and issuer-only `rev869b_record_command_consumption_attempt`. Authorizer records a unique attempt before runtime context open, binding database instance, control-plane policy, execution/service/ownership fingerprints, runtime backend/transaction, authorization fingerprint/expiry and business fingerprint. | One unique attempt per grant/sequence and execution/attempt, durably committed before open; business rollback cannot erase Attempted evidence; terminal audit remains separate. |
| C15-N05 - BLOCKING | Role/ACL verification sampled objects and lacked a reproducible administrative/export closure. | Manifest defines API, recovery administrator, owner, connect/schema/table/function/membership/default/PUBLIC rules and no unrestricted export. Exact readiness rejects duplicate/wrong functions, schema CREATE and direct table access. Purge connections prove exact role/database. | Complete catalog predicate and role manifest match; any missing, duplicate, inherited, PUBLIC, direct-DML, wrong-owner or unexpected-target condition fails closed. |
| C15-N06 - BLOCKING | Purge designs used runtime for authorizer/executor and runtime verifier for protected audit; missing authorization returned `-1` while test expected SQLSTATE. | Scenario plumbing now opens exact authorizer/executor connections and an independent owner verifier; missing-authorization design expects a committed rejection result. Six new offline contracts protect the corrected scenario infrastructure. | Every future scenario uses owned database + exact role + independent verifier and must construct its own state, reach the named API/object and assert exact result/evidence. PostgreSQL behavior remains NOT RUN. |

## Changed paths

1. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs`
2. `src/SESS.NexaERP.Infrastructure/Persistence/Rev869BCommandContextAuthorizer.cs`
3. `tests/SESS.NexaERP.Tests/Rev869BControlPlaneProvisioningContract.cs`
4. `tests/SESS.NexaERP.Tests/Rev869BControlPlaneRegistry.cs`
5. `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs`
6. `tests/SESS.NexaERP.Tests/Rev869BCorrection16SourceContractTests.cs`
7. `tests/SESS.NexaERP.Tests/Rev869BPurgeCoordinator.cs`
8. `tests/SESS.NexaERP.Tests/Rev869BPurchaseCorrectionTests.cs`
9. `tests/SESS.NexaERP.Tests/Rev869BTestDatabaseLease.cs`
10. `outputs/rev869b_source_correction_checkpoint_16.md`

No migration ID, EF model, designer or snapshot was changed.

## Control-plane provisioning manifest

| Object/principal | Exact contract |
|---|---|
| Database | `sess_nexaerp_rev869b_control_plane` only; main/source/owned/prod/REV861/postgres/templates rejected |
| Owner | `nexa_rev869b_control_plane_owner`; NOLOGIN and no elevated capabilities |
| API caller | `nexa_rev869b_control_plane_api`; exact database CONNECT, schema USAGE and seven EXECUTEs; no table DML/schema CREATE |
| Recovery administrator | `nexa_rev869b_recovery_administrator`; separate LOGIN/NOINHERIT; API-mediated operations only |
| Relations | leases, lease events, recovery approvals, recovery attempts; ordinary tables, exact owner, exact keys; event/attempt append-only |
| APIs | seven exact identity-argument/result contracts; unique overload, owner, SECURITY DEFINER, volatile/unsafe-parallel, non-leakproof, exact `pg_catalog,nexa` search path |
| PUBLIC/defaults | no CONNECT/schema CREATE/table DML/function EXECUTE; default privileges revoked |
| Modes | `GeneratePlanOnly`, `PreflightOnly`, `PostProvisionVerification`; no mutating mode exists in Correction 16 |
| Secrets/evidence | external only; no password in plan or sanitized evidence; no silent repair/drop/privilege widening |

## Role/ACL matrix

| Role | CONNECT/USAGE | Allowed | Denied |
|---|---|---|---|
| Ordinary runtime/purchase service | exact owned target / `nexa` | scoped business DML and exact command-context APIs | registry/security/purge ledgers, export, purge/recovery, schema CREATE |
| Command issuer/audit writer | exact owned target / `nexa` | issue grant, record durable attempt and fixed rollback terminal | business mutation, direct ledger DML/export |
| Control-plane API | control-plane only / `nexa` | seven exact APIs | direct registry tables, target business database, schema CREATE |
| Security owner | target only, NOLOGIN | own security objects and exact identity reads | LOGIN/elevated capabilities, unrestricted business DML |
| Purge authorizer | exact owned target / `nexa` | registration API only | begin/execute, table DML/export, ownership/membership |
| Purge executor | exact owned target / `nexa` | begin and execute APIs only | registration, table DML/export, ownership/membership |
| Recovery administrator | control-plane administrative API only | exact approved recovery/reconciliation | ordinary runtime use, name-only repair/drop |
| Verification helper | exact source/control-plane/owned targets by mode | plan/preflight/verification | production/main/REV861/templates/postgres, silent repair |
| Database/migration owner | separately governed migration boundary | install/rollback ownership work | ordinary runtime credentials and unrestricted support export |

## Lifecycle and recovery state table

| Current | Exact evidence/action | Next |
|---|---|---|
| none | supplemental intent only | no authority; creation forbidden |
| none | exact registry reservation commits | PreCreateIntent |
| PreCreateIntent | roles/database/marker exact | OwnedActive |
| PreCreateIntent | creation failure + registry/file reconciliation | Quarantined |
| OwnedActive | exact use proof | OwnedActive |
| OwnedActive | exact drop attempt commits | DropStarted |
| DropStarted | target still present/changed | Quarantined or resumable same attempt |
| DropStarted | exact target absence + role cleanup | Dropped |
| Quarantined | fresh exact recovery approval consumed | RecoveryStarted |
| RecoveryStarted | target/marker/catalog proof and exact post-state | Dropped or Failed |
| Dropped | repeated same finalization | idempotent evidence only; never another DROP |

Filesystem evidence is supplemental and cannot authorize creation/recovery/drop. Request and marker provision timestamps are distinct. Zero-row/unknown-state recovery cannot be success.

## Authorization and durable audit contract

Issuer commits the exact grant, then commits one consumption-attempt row before runtime context open. The row binds database identity/fingerprint, registry policy, execution/service instance, runtime principal/backend/transaction, actor/issuer, authorization fingerprint/expiry, business command fingerprint, ownership lease fingerprint, sequence and time. Database unique/check/FK constraints prevent attempt merging/reuse. Runtime transaction records Opened/Claimed/Committed; issuer records fixed Failed/Rejected after rollback. Temporary grant deletion cannot delete durable attempt/security audit because both FKs use RESTRICT and purge never targets durable tables.

## Rollback-safe purge

Policy remains `MGMT-REV869B-SECURITY-LEDGER-20260813-001`: maximum authorization 15 minutes, eligible consumed/expired temporary metadata retained 90 days, durable minimized audit retained at least 10 years, no secrets and no migration-time purge. Registration, begin and execution use distinct exact roles. Begin occurs on a fresh autocommit connection and commits Rejected/ZeroRows/Started before destructive work. Execute occurs on another autocommit connection; its PL/pgSQL subtransaction rolls destructive work back on error, then the outer autocommit commits Failed/PartialFailure evidence. Bad nonce records rejection without consuming a valid approval. Retry requires a new execution authorization.

## Temporary/durable separation

Temporary: command grants, contexts, claim assignments and purge authorizations. Durable: command security audits, command consumption attempts, purge attempt and rejection audits. Only temporary relations enter purge candidates. Durable tables are immutable and remain denied to runtime/purge roles. No raw password, token, OIDC assertion, nonce or reusable credential is stored.

## PostgreSQL scenario replacement inventory

| Scenarios | Correction 16 disposition |
|---|---|
| 1-5 control-plane | Exact manifest/readiness and owned lease plumbing retained; interruption/mismatch states bind exact registry fields. |
| 6-11 recovery | Exact approval/issuer/pre-state/expiry/replay/outcome designs retained with separated timestamps and post-drop reconciliation source. |
| 12 missing purge approval | Now expects committed Rejected result/evidence rather than an impossible durable insert plus raised exception. |
| 13 wrong purge scope | Uses exact purge-authorizer connection. |
| 14 replay/concurrent approval | Uses exact executor role and independent connection infrastructure; exact single winner remains future evidence. |
| 15 zero rows | Exact executor and owner verifier; committed ZeroRows evidence. |
| 16-17 failure/drift | Exact executor and owner verifier; autocommit terminal evidence; deterministic fixture/fault behavior remains a future execution proof. |
| 18-19 direct DML | Purge principal reaches the intended ACL boundary; owner verifier observes no mutation. |
| 20 durable preservation | Exact executor with owner verification of durable rows. |
| 21 runtime/export denial | Runtime denial retained; approval-bound export remains externally unprovisioned. |
| 22 audit failure | Exact command-attempt ledger and fail-closed terminal path added. |
| 23 exact denial metadata | Exact SQLSTATE/object assertion retained. |
| 24 zero-row false positive | Exact executor/owner evidence path. |
| 25 independent backends | Distinct role-specific actor and owner verifier PIDs asserted. |

Exactly 50 REV869B PostgreSQL tests were discovered: 18 direct, 7 application and 25 corrected designs. Executed count: 0. **PostgreSQL tests NOT RUN**.

## Offline validation

| Gate/command | Actual result |
|---|---|
| `dotnet build SESS.NexaERP.slnx --no-restore --nologo` | PASS; 0 warnings, 0 errors |
| Focused Correction 16 + safety/correction tests | PASS; 39/39 |
| Inclusive `Rev869B` excluding `Postgres` | PASS; 71/71 |
| Complete suite excluding `Postgres` | PASS; 445/445 |
| Exact three PostgreSQL classes, `--list-tests` | 50 discovered; 0 executed; NOT RUN |
| PowerShell 5.1 AST | PASS; 23/23; version 5.1.19041.6456 |
| EF migrations `--no-connect` with inert port 1 | 13; one REV869B immediately after REV869A; applied state unknown |
| Model/snapshot parity | PASS; 1/1 |
| Offline Up | 266,257 bytes; SHA-256 `4E1C6659E2C15BB65AB773669345A4A0A8E7037AF9F4CECE52664ED9B5FF8336` |
| Offline Down | 10,417 bytes; SHA-256 `E75891F1E504F34BCA937A4BC89B772353F34F7C03E0C0C9AA777D2274D9A42E` |
| Up inventory | 24 tables; 81 triggers; 33 function definitions / 32 distinct; 46 FK clauses / 50 REFERENCES; 72 indexes; 66 checks |
| Down inventory | 7 DROP TRIGGER statements; 1 generated function definition; 62 DROP lines |
| Role/ACL/state scans | exact signatures/results, target guards, autocommit phases, attempt bindings and denied direct access present |
| Secret/privacy scan | no private key, bearer literal or client-secret assignment |
| Prohibited migration scan | no CREATE/DROP DATABASE or `pg_terminate_backend` in migration SQL source |
| Truncation scan | no truncation marker |
| `git diff --check` | PASS |

Offline SQL was generated only from REV869A to REV869B and back using an inert loopback port-1 design identity; it was not parsed or executed by PostgreSQL.

## External dependencies and remaining blockers

External provisioning remains closed: exact control-plane database/owner/API/recovery roles and objects; security owner; purge authorizer/executor credentials; distinct runtime/issuer connections; execution/service/ownership fingerprints; protected recovery keys/evidence store; and approved retention values. None was provisioned.

Remaining blockers are independent Correction 16 source-safety review and all future PostgreSQL behavioral evidence. The 25 scenario bodies compile and are discoverable but PostgreSQL behavior, complete deterministic fixture/fault injection, role ACL behavior, external provisioning correctness, migration acceptance and production readiness are unclaimed.

## Prohibited operations and next gate

No PostgreSQL access/test, migration apply/remove, database create/alter/clone/restore/quarantine/repair/drop, provisioning, recovery, purge, export, protected business command, production/REV861/AWS/OIDC/frontend/Docker/legacy action, or `legacy-reference` content access occurred.

This checkpoint does not self-declare source safety, helper readiness, PostgreSQL acceptance, migration acceptance, production readiness or final REV869B acceptance.

A fresh independent source-only safety re-review of the committed Correction 16 diff is mandatory and is the only next gate.
