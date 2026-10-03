# REV869B pre-apply source safety re-review after Correction 14

Date: 2026-08-13 (Asia/Calcutta)

Review type: fresh independent source-only safety re-review

Reviewed commit: `f8a87fc8313405478765aeddb28f591371b27fce`

Parent: `b0976299874cd978edf1106e13c2eb79e7047752`

Reviewed subject: `Correct REV869B control-plane and security ledgers`

No PostgreSQL connection or test, helper, control-plane/role provisioning, migration apply/remove, database create/drop/recovery/quarantine, purge, scheduler, production/REV861/frontend/REV869C operation, or `legacy-reference` content was accessed or executed.

## 1. Entry gate and reviewed scope

The entry gate passed before review: exact HEAD, parent and subject; exactly seven reported files; clean target-scoped status; one REV869B immediately after REV869A; non-authoritative sibling `192d84fa1116975a09e9676a7d8c864f975380f5` excluded. Global status remained the pre-existing untracked `legacy-reference/` boundary; its contents were not enumerated or opened, preserving the previously established two-ZIP baseline.

Exact reviewed scope:

1. `outputs/rev869b_source_correction_checkpoint_14.md`
2. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs`
3. `tests/SESS.NexaERP.Tests/Rev869BControlPlaneRegistry.cs`
4. `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs`
5. `tests/SESS.NexaERP.Tests/Rev869BDatabaseSafetyContractTests.cs`
6. `tests/SESS.NexaERP.Tests/Rev869BPurchaseCorrectionTests.cs`
7. `tests/SESS.NexaERP.Tests/Rev869BTestDatabaseLease.cs`

The checkpoint, complete Correction 13 report, committed Correction 14 diff, changed sources, retained authorization/safety/lifecycle contracts, PostgreSQL test sources, migration install/remove ordering, and relevant runtime/designer/snapshot evidence were reviewed independently. Checkpoint assertions were not treated as acceptance evidence.

## 2. Canonical verdict

```text
rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
```

Source safety fails because database ownership transitions are not consistently control-plane-authoritative, recovery has a consumed-without-outcome window, purge authorization/evidence and role restrictions are incomplete, durable command audit omits failed/rejected outcomes, and the 25 new PostgreSQL designs do not implement their named scenarios. Helper readiness separately fails because ordinary cleanup can drop without control-plane reconciliation and required external role/API properties are not fully checked.

Neither state authorizes PostgreSQL, helper, migration, purge, recovery, provisioning, or production execution.

## 3. New findings

### C14-N01 — control-plane readiness and lifecycle coverage are incomplete — BLOCKING

`ReserveBeforeCreateAsync` precedes role/database creation, which is a material improvement. Missing `REV869B_CONTROL_PLANE` fails closed, the exact registry database/owner is checked, pooling is disabled, and filesystem evidence no longer reaches the earliest creation boundary by itself.

The contract is nevertheless incomplete:

* `OpenVerifiedAsync` checks only `rev869b_reserve_database_lease` and `rev869b_consume_recovery_approval` existence (`Rev869BControlPlaneRegistry.cs:138-140`). It does not check the two APIs actually required for lease completion and recovery outcome, function ownership, `SECURITY DEFINER`, secure `search_path`, PUBLIC ACLs, append-only tables, duplicate/state/expiry constraints, or exact caller privilege.
* The source-commit value is an environment string checked only for 40 hexadecimal characters (`Rev869BTestDatabaseLease.cs:104-107`), not derived from or compared with the reviewed commit.
* The target marker omits source commit, control-plane policy/lease identity, lease expiry and registry state (`Rev869BTestDatabaseLease.cs:253-270`). Its marker fingerprint likewise omits source commit and expiry (`:145-149`).
* Normal `DisposeAsync` verifies only the target marker and then drops the database (`:574-596`); it performs no control-plane pre-drop transition, reconciliation or durable drop outcome. `MarkQuarantinedBestEffortAsync` updates only target/file evidence and suppresses failure (`:608-635`). Thus not every quarantine/cleanup/drop transition and outcome is registry-backed.
* Roles-only interruption, target creation interruption and marker-commit interruption do not have implemented recovery paths. `RecoverQuarantinedAsync` assumes the target database can be opened.

Filesystem evidence is still required by recovery before control-plane consumption, but it is not sufficient alone. The accepted REV868C3 database is not used as the registry. Result: **FAIL / BLOCKING**, not an external provisioning gate alone.

Required correction: define and verify the complete registry schema/API/owner/ACL/state-machine contract; derive the source commit authoritatively; cross-bind every marker field; require registry reconciliation before test connection and every cleanup/drop; record normal/quarantine/drop outcomes; and implement fail-closed roles-only and every hard-interruption state.

### C14-N02 — recovery can consume approval without a durable outcome and does not prove the complete lease — BLOCKING

The signed envelope now includes issuer/authority, exact filesystem pre-state, operation/purpose, expected post-state, reference, reason, executor, target tuple, nonce and freshness. Control-plane consumption occurs before target mutation, and success/failure outcome calls exist.

However:

* `ConsumeRecoveryBeforeMutationAsync` receives the `LeaseReservation` object but sends only database, run and token hash from it (`Rev869BControlPlaneRegistry.cs:67-96`). Owner, source commit/fingerprint, migration fingerprint, requested time and lease expiry are not supplied as independently typed values; only an opaque caller-produced target fingerprint is sent.
* The local `ConsumeRecoveryAuthorizationAsync` call occurs after durable control-plane consumption but before the `try` that records success/failure (`Rev869BTestDatabaseLease.cs:486-490`). Any filesystem-path/key/I/O/deserialization failure leaves a consumed approval with no durable outcome.
* After successful DROP, filesystem evidence is written before the control-plane success outcome (`:551-556`). An outcome API failure therefore leaves the database gone with unresolved registry state.
* Issuer/authority/executor are signed strings; the readiness contract does not prove an external authorized-issuer registry or executor-to-session binding.
* Pre-marker recovery still derives pre-state from filesystem evidence and tests a generic cloned target/owner/migration shape; exact control-plane lease readback and marker fingerprint reconciliation are absent.

Result: **FAIL / BLOCKING**. Failed attempts are not guaranteed to end in a durable non-reusable terminal outcome.

### C14-N03 — purge authorization and outcome evidence remain incomplete — BLOCKING

The source adds an execution ID, exact 90-day cutoff, bounds, expiry, nonce, executor, policy and audit destination. Begin atomically consumes an approval and writes `Started` or `ZeroRows`; migration application does not invoke purge and no schedule is installed.

Material defects remain:

* `rev869b_register_purge_authorization` lets the database owner supply arbitrary `authorized_issuer` and `issuer_authority` text (`Rev869BCommandContextSql.cs:153-179`). No signature, trusted issuer relation, approval fingerprint or independently issued management grant is validated. This is database-owner registration, not demonstrated separate authorization.
* Rejected/missing/expired/replayed/substituted begin attempts raise before inserting `Rejected` evidence (`:185-195`). The schema permits `Rejected`, but no rejection path records it.
* `PreCount` is all command grants, not total eligible rows (`:196`), and organization scope is always NULL (`:172-178`). `EligibleStates` is stored but execution never evaluates claimed/unclaimed state; selection uses only timestamps.
* Begin records a candidate count, but purge reselects and accepts any nonzero smaller set. It never compares the second candidate count with the durable Started count (`:225-247`). Candidate drift can therefore produce a partial `Succeeded` result.
* Database errors roll back the purge transaction and do not automatically append terminal failure evidence. `rev869b_record_purge_failure` is a later caller-supplied statement with arbitrary phase/SQLSTATE/object and zero candidate/claimed/deleted counts (`:262-281`). A crash or omitted call leaves only `Started`; a caller can also label a failure without proof of the actual error.
* There is no FK/unique terminal-outcome constraint connecting attempts to authorization, and no durable rejection/partial outcome implementation.

Count mismatch does raise and rolls back, and business histories/durable command audit are not deleted. No secret payload is stored. Result: **FAIL / BLOCKING**.

### C14-N04 — exact purge/security role least privilege is not proven — BLOCKING

PUBLIC execute is revoked; functions use fixed `pg_catalog,nexa` search paths and qualified relations; the dedicated purge caller receives only three explicit function grants in this migration. Runtime and issuer are revoked from the new ledger tables.

The precondition checks only that the security owner is NOLOGIN and the purge executor is LOGIN, plus membership separation (`Rev869BCommandContextSql.cs:16-30`). It does not reject SUPERUSER, CREATEDB, CREATEROLE, REPLICATION, BYPASSRLS, inherited role membership, object ownership, existing table/schema privileges or pre-existing function grants. It neither revokes nor proves the purge executor's direct ACLs. The source verification repeats only login/function-owner counts (`Rev869BTestDatabaseLease.cs:202-219`). There is no schema-USAGE or exact privilege-closure proof.

The security owner owns all temporary and durable ledger tables and functions, while the migration/database owner is required to be its member. That administrative topology is not independently constrained against direct mutation or function/trigger alteration. A separately provisioned role can be an external gate only after exact capabilities and ACLs are fail-closed in source; current checks do not do that.

Result: **FAIL / BLOCKING**.

### C14-N05 — durable command audit is structurally separated but lacks complete command outcomes — REQUIRED CORRECTION / BLOCKING

`rev869b_command_security_audits` is separate from temporary grants/contexts, retains minimized fingerprints and exact operation/entity/version/status/policy fields, rejects UPDATE/DELETE, survives temporary purge, and is revoked from ordinary runtime/issuer. Issued insertion failure blocks grant issuance; Claimed insertion is transactional with protected work.

The lifecycle is incomplete:

* `Issued`, `Opened`, `Claimed` and purge-time `Expired` are inserted. `Rejected`, `Failed` and `Committed` are declared but never written by command paths (`Rev869BCommandContextSql.cs:80-90, 370-480`).
* `Opened` is written inside the business transaction, so rollback removes it. A protected command that opens/claims and then fails leaves only `Issued`; its attempted consumption, failure time/category and outcome are not durable.
* Unclaimed and rolled-back commands are labeled Expired only if a future authorized purge runs, potentially after 90 days. No immediate rejection/failure outcome exists.
* `FailureCategory` remains NULL in all shown command-audit inserts.

Ordinary runtime cannot directly fabricate or export this table under the helper's shown revokes, and direct audit insertion failure is fail-closed. The missing durable failure/rejection/terminal lifecycle means the mandatory per-command outcome contract is not complete. Result: **PARTIAL / BLOCKING**.

### C14-N06 — the 25 new PostgreSQL test names do not implement 25 designs — BLOCKING

Exactly 25 new facts are listable, but every fact is `=> ExecuteAsync();` (`Rev869BCorrection14PostgresDesignTests.cs:11-35`). The single shared body only:

1. creates a lease;
2. opens two runtime connections;
3. checks object counts and one marker row;
4. checks runtime SELECT denial on durable audit; and
5. checks runtime denial calling purge begin.

It never interrupts lifecycle phases, removes filesystem/control-plane evidence, substitutes leases/markers/recovery approvals, invokes recovery, registers/begins/executes/fails purge, creates purge candidates, injects count drift/audit failure, checks unrelated rows, or verifies recovery/purge durable outcomes. The source scan independently found 25 facts and 25 delegates to the same body. Most names therefore provide no executable design for their asserted scenario. Static tests only count names/strings.

Result: **FAIL / BLOCKING**. The existing 25 PostgreSQL tests retain useful authorization, rollback, concurrency, terminal-guard and structured-error coverage, but they do not supply the missing Correction 14 scenarios.

## 4. Prior blocker matrix

| Finding | Previous severity | Correction 14 result | Database/runtime evidence | Remaining risk |
|---|---|---|---|---|
| C11-01 / C12-N01 authorization before mutation | BLOCKING | PARTIAL | prior exact open-before-mutation source ordering retained | comprehensive per-workflow PostgreSQL design remains absent |
| C11-02 slot substitution | BLOCKING | PASS in static design | exact fingerprint, principal, backend, transaction, version/status and ordinal checks retained | PostgreSQL behavior NOT RUN |
| C11-03 replay/savepoint/pooling | BLOCKING | PARTIAL | nontransactional ordinal and transaction/backend binding retained | full rollback, pool reuse and interruption coverage remains incomplete |
| C11-04 qualification compatibility | BLOCKING | PARTIAL | retained lifecycle/SoD SQL and source paths unchanged | retained-data positive PostgreSQL proof remains NOT RUN |
| C11-05 current-version/late-child | BLOCKING | PARTIAL | retained guards and tests remain | complete valid/invalid dependent workflow proof absent |
| C11-06 least privilege | BLOCKING | FAIL | new tables/revokes improve ordinary runtime boundary | pre-provisioned purge/security roles are not capability/ACL constrained |
| C11-07 privacy/data minimization | REQUIRED CORRECTION | PARTIAL | minimized command fingerprints and runtime audit SELECT revoke added | complete failure outcomes and exceptional export governance absent |
| C11-08 rollback completeness | BLOCKING | FAIL | business transaction rollback design retained | new purge/recovery interruption and unrelated-record tests are aliases only |
| C11-09 / C12-N02 ownership | BLOCKING | FAIL | reservation-before-CREATE added | normal drop/quarantine and hard-interruption states bypass/incompletely use registry |
| C11-10 / C12-N03 recovery | BLOCKING | FAIL | issuer/pre-state fields and outcome APIs added | consumed-without-outcome window and incomplete exact lease verification remain |
| C11-11 PostgreSQL design | BLOCKING | FAIL | 50 names list | 25 new names share one unrelated implementation |
| C11-12 retention/purge decision | EXTERNAL/BLOCKING | FAIL | fixed policy/cutoff and no schedule retained | separate issuer authorization, rejection/failure evidence and least privilege incomplete |
| C12-N04 reproducibility | IMPROVEMENT | PASS | commands/totals/hashes independently reproduced | documentation does not establish safety |

## 5. Area conclusions

### Control plane

Reservation-before-CREATE and fail-closed absence are positive. Source safety still fails because registry readiness is only nominal, cross-binding is incomplete, normal cleanup/drop bypasses registry transition/outcome, quarantine state is target/file-only, and hard-interruption recovery is not implemented. This is not reducible to provisioning alone.

### Recovery authorization

Envelope binding is materially stronger, but exact lease fields are not independently supplied to the registry API and a post-consumption/pre-`try` failure can omit durable outcome. Verdict: FAIL.

### Purge authorization and evidence

The one-use Started/ZeroRows state is a useful base. Database-owner-supplied issuer claims are not separate approval; rejection is unaudited; eligible pre-count/state/scope are incomplete; reselection drift can claim partial success; failure finalization is optional and caller-asserted. Verdict: FAIL. No purge is authorized.

### Purge privilege

Function ACLs are narrow in the migration, but role capabilities and inherited/direct ACLs are not proven or revoked. Verdict: FAIL.

### Durable command audit

Structural separation, minimization, immutability, retention and runtime denial are improved. Failed/rejected/rolled-back command outcomes are not durable. Verdict: PARTIAL / BLOCKING.

### PostgreSQL test design

Exactly 50 REV869B PostgreSQL tests were independently listed and **NOT RUN**: 18 prior direct tests, 7 prior application tests, and 25 new same-body aliases. Only the prior 25 and the narrow shared body are genuine designs. Correction 14 does not provide the required scenario-specific executable evidence. Verdict: FAIL.

## 6. Independently reproduced offline validation

| Validation | Independent result |
|---|---|
| Build | PASS; 0 warnings, 0 errors |
| Focused REV869B non-PostgreSQL | PASS; 44/44 |
| Inclusive REV869B non-PostgreSQL | PASS; 64/64 |
| Complete non-PostgreSQL | PASS; 438/438 |
| REV869B PostgreSQL discovery | 50 discovered; **NOT RUN** |
| PowerShell 5.1 AST | PASS; 23/23; `5.1.19041.6456` |
| EF migration discovery | PASS with `--no-connect`; 13 migrations; one REV869B immediately after REV869A; applied state unknown |
| Model/snapshot parity | PASS; exact no-connect test 1/1 |
| Offline Up SQL | 249,438 bytes; SHA-256 `4FB671D4AA1131E0D9E6D588E9393311361A0702605B1707F6082E9602580E7A` |
| Offline Down SQL | 9,955 bytes; SHA-256 `8D31CB465CE76C05B177C351D92106EC13A9E4EF72C5DD062BEBA52EA3F06AAC` |
| Up inventory | 22 tables; 79 triggers; 32 function definitions / 31 distinct names; 46 FK occurrences; 70 indexes; 54 CHECK occurrences |
| Down inventory | 2 trigger statements; 1 generated function definition; 57 DROP lines |
| Control-plane scan | one reservation call; zero control-plane calls in normal dispose; readiness checks 2 of 4 called APIs |
| Purge privilege scan | zero SUPERUSER/CREATEDB/CREATEROLE/REPLICATION/BYPASSRLS checks; zero exact table/function ACL checks |
| PostgreSQL design scan | 25 new facts; all 25 delegate to the same body |
| Secret scan | no committed private key, client secret, bearer credential, raw token or raw OIDC assertion pattern found in reviewed sources |
| Prohibited-operation scan | expected future helper CREATE/DROP statements only; no FORCE, broad termination or business-history/audit purge introduced |
| Correction 14 `git diff --check` | PASS |

Offline generation and inspection do not parse or execute PostgreSQL SQL and establish no PostgreSQL behavior.

## 7. External gates and exact next action

External provisioning gates remain closed for the control-plane database/APIs and exact security/purge roles. External execution gates remain closed for PostgreSQL tests, helpers, migrations, database lifecycle, purge and recovery. Provisioning alone cannot cure the identified source defects.

Exact next authorized action: perform a fifteenth controlled source-only correction against this report. Correct every BLOCKING and REQUIRED CORRECTION finding; implement complete registry-backed create/quarantine/test/cleanup/drop and recovery outcome transitions; prove exact external API/role/ACL contracts; redesign purge issuer/rejection/partial/failure evidence; complete failed/rejected command audit; and replace the 25 aliases with scenario-specific PostgreSQL designs without executing them. Then reproduce offline gates, checkpoint, commit controlled files, and request another fresh independent source-only re-review.

No PostgreSQL, helper, migration, purge, recovery, provisioning or production action is authorized.
