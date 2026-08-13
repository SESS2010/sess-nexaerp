# REV869B pre-apply source safety re-review after Correction 15

Date: 2026-08-13 (Asia/Calcutta)

Review type: fresh independent source-only safety re-review

Reviewed source commit: `6279a71f5565f35c426c8470a5982010b313bc0a`

Parent/report commit: `5ca141178d936259883e704969f0217eba48be31`

Reviewed subject: `Correct REV869B control-plane safety checkpoint 15`

## 1. Verdict

Correction 15 adds useful fields, APIs, state labels, role checks and terminal-event plumbing, but it does not close the six Correction 14 findings. The control-plane verifier is not an exact or reproducible provisioning contract; pre-marker and post-drop failure states are not recoverable; quarantine recovery compares two different timestamps as if they were one; purge rejection/failure evidence is rollbackable and several denial paths return a value instead of the asserted SQLSTATE; durable command audit still omits database/execution identity and loses attempt evidence on rollback; and the 25 future PostgreSQL facts are still largely names around non-executable scenarios.

```text
rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
```

No PostgreSQL, helper, migration, provisioning, recovery, quarantine, purge, or production execution is authorized by this report.

## 2. Entry gate and exact scope

The entry gate passed before any edit:

- HEAD was exactly `6279a71f5565f35c426c8470a5982010b313bc0a`; its parent was exactly `5ca141178d936259883e704969f0217eba48be31`; the subject matched.
- `git diff-tree --no-commit-id --name-only -r HEAD` returned exactly nine target paths: the Correction 15 checkpoint; command-context SQL; command authorizer; purchase service; control-plane registry; Correction 14 PostgreSQL designs; database safety contracts; purchase correction tests; and database lease helper.
- Target-scoped tracked status and `git diff --check` were clean.
- EF `--no-connect` discovery returned 13 migrations, with one `20260811025827_Rev869B...` immediately after `20260810120000_Rev869A...`.
- The three authoritative reports were non-empty and read completely (20,092; 8,985; and 17,863 bytes). The complete nine-file committed diff was reviewed independently rather than accepting checkpoint assertions.
- `git status --short -- ../legacy-reference` returned only `?? ../legacy-reference/`. Its contents were not enumerated, opened, copied, staged, or changed.

Exact Correction 15 diff: 9 files, 1,077 insertions and 265 deletions. No earlier migration ID, EF model, designer, or snapshot was changed.

## 3. Correction 14 finding disposition

| Correction 14 finding | Correction 15 disposition | Independent conclusion |
|---|---|---|
| C14-N01 control-plane readiness/lifecycle | FAIL | More APIs and fields are called, but readiness proves names/argument counts rather than exact signatures/schema/state enforcement; several interruption states remain unrecoverable. |
| C14-N02 recovery consumption/outcome | FAIL | More lease fields are supplied, but pre-create state and marker time reconciliation are internally inconsistent, and irreversible drop can precede a recordable terminal outcome. |
| C14-N03 purge authorization/evidence | FAIL | A distinct authorizer role and candidate fingerprint are improvements; rejection/failure evidence remains caller-transactional, denial semantics conflict with tests, and eligible-state retention is incomplete. |
| C14-N04 least privilege | FAIL | Role capabilities and broad revokes improve the design; retained administrative SET ROLE power and incomplete catalog verification do not prove the claimed closure or protected export/access path. |
| C14-N05 durable command audit | FAIL | Terminal event plumbing exists, but database/execution/transaction identity and durable consumption-attempt evidence are absent. |
| C14-N06 PostgreSQL designs | FAIL | The aliases were expanded syntactically, but most scenarios cannot reach their named operation or establish their required state. |

## 4. Findings

### C15-N01 - control-plane provisioning/readiness is not exact or reproducible - BLOCKING

Evidence: `tests/SESS.NexaERP.Tests/Rev869BControlPlaneRegistry.cs:205-257`.

`OpenVerifiedAsync` joins `pg_proc` by schema, function name and argument count only (`:217-238`). It does not verify identity argument types/order, return type/shape, exact overload uniqueness, function definitions, volatility/parallel/leakproof attributes, or exact owner-safe configuration. Four relation names and a minimum of two enabled triggers are counted (`:239-244`) without proving relkind, owners, columns, keys, state/expiry constraints, trigger events/functions, append-only coverage, or exact ACLs for every relevant principal. No committed provisioning source defines the seven external functions or four relations.

Failure scenario: a same-name/same-count overload, wrong result contract, incomplete state constraint, or unrelated enabled trigger passes readiness. Reservation or finalization then fails after a protected boundary, leaving an external state the helper cannot reconcile. The source cannot reproduce or independently verify the prerequisite it calls authoritative.

Required correction: commit a separately reviewable provisioning and rollback contract, or an exact declarative catalog contract, for every control-plane relation, constraint, index, trigger and API. Verify `pg_get_function_identity_arguments`, result type/columns, exact owner, `SECURITY DEFINER`, exact fixed search path, PUBLIC denial, caller-specific EXECUTE, relation owner/relkind/columns/constraints, exact append-only triggers and complete ACL closure. Reject duplicate overloads and any extra caller privilege.

### C15-N02 - lifecycle and recovery contain unrecoverable or falsely described states - BLOCKING

Evidence: `tests/SESS.NexaERP.Tests/Rev869BTestDatabaseLease.cs:117-123,152-156,177-198,297-371,519-715,717-809`.

The pre-marker failure path first changes the registry from `PreCreateIntent` to `Quarantined` (`:152-156`) while supplemental evidence remains `PreCreateIntent` (`:119-123`). Recovery derives its required registry state from that file (`:554-562,598-600`), so the exact recovery read cannot match the state just written by the failure handler. A hard interruption after reservation but before the file write has no recovery input at all.

The marker stores `LeaseRequestedAt` and an independently defaulted `ProvisionedAt` (`:318-323,326-370`). Recovery accepts one `provisionedAt` value, uses it as the reservation request time (`:549-553,587-591`), and also requires target `ProvisionedAt=@provisioned` (`:663-665`). Normal marker provisioning makes those timestamps different, so active-marker quarantine recovery is rejected.

Normal disposal drops the database before recording success (`:734-740`). If outcome recording then fails, the catch attempts a failure outcome and opens the already absent target to mark quarantine (`:745-763,772-806`). Recovery likewise drops first and records success later (`:687-699`); a failure path can report the old state after the target is already absent (`:701-711`). These are unresolved or inaccurate authoritative states, not deterministic reconciliation.

Required correction: model request time and actual marker-provisioned time as separate bound fields; add registry-first reconciliation for reservation-without-file, roles-only, database-without-marker, marker-bound, drop-started and database-absent states. Make every irreversible action idempotently resumable from the authoritative registry. A post-drop retry must prove absence and finalize the same attempt without opening or rewriting the target, and must never record the pre-state as the observed post-state.

### C15-N03 - purge authorization, retention and failure evidence are not rollback-safe - BLOCKING

Evidence: `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs:182-366`.

The new authorizer/executor split and exact candidate fingerprint are positive. However, an invalid, missing, expired, replayed or substituted begin inserts a rejection and then `RETURN -1` (`:222-239`), not the exact `42501` asserted by the future tests. A wrong nonce also changes a still-valid approval to `Rejected` (`:234-236`), allowing the executor to destroy a valid approval by presenting bad data.

`rev869b_purge_temporary_security_ledger` catches errors, inserts a failure/partial record and returns `-1` (`:343-365`). All of that remains in the caller's transaction; an outer rollback or connection loss removes the supposedly durable outcome. PostgreSQL has no autonomous transaction here. The same rollback weakness applies to begin rejection/zero/start evidence. Thus failure, rejection, retry and outer-rollback durability are not enforced by this function source.

The approval stores `EligibleStates=['Expired','Unclaimed']` (`:193`), but selection never evaluates that field and excludes every grant with a `Claimed` event (`:240-251,296-302`). Consumed temporary grants therefore have no 90-day purge path, contrary to the stated consumed/expired temporary-metadata boundary.

Required correction: put authorization consumption and terminal evidence in a separately committed, fail-closed control-plane/helper transaction with exact attempt IDs and idempotent reconciliation; do not claim a SQL exception and durable insert from one rollbackable transaction. Record bad-nonce rejection without consuming a valid approval. Enforce the approved eligible-state model explicitly, including bounded consumed temporary metadata, while proving durable audit/history exclusion. Return/raise semantics, retry state and SQLSTATE/object contracts must agree with tests.

### C15-N04 - durable command audit does not bind the complete command instance or preserve attempt evidence - BLOCKING

Evidence: `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs:107-122,550-613`; `src/SESS.NexaERP.Infrastructure/Persistence/Rev869BCommandContextAuthorizer.cs:18-102`; `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs:25-97`.

The durable table binds grant, organization, actor, issuer, operation, entity, version/status and correlation. It does not store or fingerprint the exact database instance, execution instance, runtime principal, backend PID, transaction ID, authorization authority, or ownership lease. `Opened`/`Claimed` are written in the business transaction and disappear on rollback. The independent Failed/Rejected path reconstructs terminals from `Issued` rows (`Rev869BCommandContextSql.cs:589-605`), so durable history cannot distinguish issuance-only from an actual open/claim/consumption attempt after rollback.

The service now rejects every ambient transaction (`EfRev869BPurchaseService.cs:32-40`). That may be a safe fail-closed choice, but no scenario-specific application design proves compatibility with qualification, audit-writer and caller-owned workflow orchestration. Passing source-string tests is not workflow evidence.

Required correction: add minimized immutable database-instance, execution-instance, runtime/backend/transaction and ownership/authorization correlation; durably record attempt/consumption before or independently of rollback without making authorization reusable; retain exactly one terminal per slot. Define and test a transaction-ownership contract that preserves approved qualification and application workflows rather than silently making ambient orchestration impossible.

### C15-N05 - role closure and protected ledger access/export are not fully proven - BLOCKING

Evidence: `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs:13-47,642-697`; `tests/SESS.NexaERP.Tests/Rev869BTestDatabaseLease.cs:222-274`.

Capability checks and broad purge-role revokes are improvements. The migration nevertheless retains a SET ROLE membership from the migration owner to the security owner, which owns every security function/table and receives identity/role SELECT. The claimed "solely to assign ownership during migration" boundary is not time-bounded or revoked. No approval-bound exceptional export/access API exists.

The helper verification counts functions by `proname` and owner, not exact signatures/definitions (`Rev869BTestDatabaseLease.cs:236-242`); samples only five ledger tables for purge-role DML (`:261-265`); and does not prove exact schema privileges, PUBLIC function ACL, all business-table privileges, default privileges, or the intended migration-owner-to-security-owner membership. The membership query (`:243-246`) does not even select that permitted membership topology. Same-name overloads can satisfy counts while required functions are wrong.

Required correction: define the administrative/recovery owner explicitly and constrain its use; revoke temporary ownership membership after installation or provide a separately governed rollback mechanism. Verify every security object and overload, all role memberships/capabilities, schema/default privileges, PUBLIC and per-principal ACLs, all business relations, and an approval-bound minimized export route. Source/helper verification must enumerate the complete closure, not representative objects.

### C15-N06 - the 25 future PostgreSQL tests are not valid scenario designs - BLOCKING

Evidence: `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs:10-179`.

All purge/audit scenarios open `lease.OpenVerifiedConnectionAsync()` for actor and verifier (`:157-175`), which returns the ordinary runtime role. That role is deliberately denied purge APIs and durable-ledger SELECT, so the tests cannot act as purge authorizer/executor and their verifier queries cannot read the evidence they assert. Fixed execution IDs `...001` through `...007` have no setup authorization, candidates or Started attempt (`:62-112`). `SET LOCAL` is issued outside an explicit transaction and no production function reads the proposed fault-injection setting (`:72-81,99-103`). The concurrency test makes one call on one actor connection. Missing-authorization begin expects an exception although source returns `-1` (`:52-56` versus SQL `:230-239`).

The control-plane "hard interruption" test only substitutes a commit fingerprint on a completed lease (`:12-14`); it interrupts no phase. The filesystem-only test removes the registry variable before creation (`:15-25`), so no filesystem evidence is created. Recovery denial tests synthesize unregistered approvals and never exercise recovery mutation/outcome/replay (`:138-155`). Several named tests can pass or fail at an unrelated permission/missing-row boundary without touching the intended row, trigger or state.

Required correction: give each scenario explicit, least-privilege authorizer/executor/runtime/verifier connections; create exact approvals, candidates and pre-states; use independent transactions/connections for real concurrency and rollback; add supported fault injection or deterministic database conditions; assert exact SQLSTATE/object plus exact before/after and durable evidence. Implement real interruption and recovery entry points. A test must be unable to pass without reaching its named operation.

## 5. Workflow, authorization and enforcement assessment

The retained purchase workflow source continues to bind organization, record, version, transition, operation slot, actor/OIDC identity, correlation, command backend and transaction, and retained database triggers continue to guard current-version and late-child mutations. No Correction 15 diff removes PR -> RFQ -> vendor quote -> comparison -> PO -> follow-up, GST reconciliation, approval routing, segregation, immutable snapshots/histories, or REV869A foundations. Offline tests cover those source contracts.

That is regression evidence, not PostgreSQL acceptance. Exact grant issuance still precedes protected service mutation and nontransactional claim ordinals resist savepoint restoration, but complete durable attempt/outcome identity is missing as described in C15-N04. Qualification and service transaction compatibility remain unproved because the new service-owned-only transaction rule is not exercised by a valid PostgreSQL application scenario.

## 6. Role and privilege matrix

| Principal | Intended source allowance | Independent result |
|---|---|---|
| Ordinary runtime | Bound business DML/functions; no ledger export/purge | Static revokes exist; future purge tests incorrectly use this role. PostgreSQL denial NOT RUN. |
| Command issuer | Issue exact grants and fixed rollback terminals; no business mutation | Function path exists; full ACL/object closure and database-instance audit binding are incomplete. |
| Control-plane caller | Seven exact APIs, no direct registry tables | FAIL: names/counts are checked, but exact API/schema/trigger contract is absent. |
| Security-ledger owner | NOLOGIN owner; narrow identity reads | Capability checks exist; retained migration-owner SET ROLE and export governance remain unresolved. |
| Purge authorizer | Exact registration API only | Broad revokes/grant exist; management issuance and exact helper ACL verification are incomplete. |
| Purge executor | Begin/execute only; no direct DML | Broad revokes/grants exist; failure durability and test-role use are invalid. |
| Audit writer | Append minimized evidence only | Command function paths exist; database/execution identity and rollback-persistent attempt evidence are incomplete. |
| Administrative/recovery owner | Separately governed install/rollback/reconciliation | Not defined with a reproducible least-privilege provisioning and access contract. |

## 7. State-machine assessment

Command authorization intends `Issued -> Opened -> Claimed -> Committed|Failed|Rejected`, with one terminal per fingerprint. `Opened` and `Claimed` roll back, so the durable graph collapses to `Issued -> Failed|Rejected` without evidence that consumption was attempted.

Database lifecycle intends `PreCreateIntent -> OwnedActive -> DropStarted -> Dropped`, with `Quarantined` failures. Actual source can create `registry=Quarantined/file=PreCreateIntent`, `target absent/registry=DropStarted`, and `target absent/failed outcome reporting old pre-state`; the recovery algorithm cannot reconcile these states.

Recovery intends `Approved -> Consumed -> Succeeded|Failed`. Exact pre-state read and consumption are improvements, but filesystem-first input, mismatched timestamps and post-drop outcome ordering break deterministic recovery.

Purge intends `Approved -> Started -> ZeroRows|Succeeded|Failed|PartialFailure|Rejected`. Rejection and failure are rollbackable with the caller, invalid begin returns `-1`, and consumed candidates are excluded indefinitely. The state machine is not durably enforced.

## 8. Durable versus temporary ledgers and privacy

The source keeps command security audits and purge attempt/rejection audits separate from grants, contexts and purge authorizations. Immutable UPDATE/DELETE triggers and absence of durable-audit DELETE in generated Up SQL are positive. The reviewed diff contains no private key, bearer credential, assigned client secret or raw OIDC assertion pattern. Generated role passwords are random, parameterized and not persisted in source.

Privacy/access remains incomplete because the durable audit omits required instance identity while its owner has broad ownership and identity SELECT through a retained administrative membership. There is no approval-bound exceptional access/export implementation. Temporary purge excludes claimed/consumed grants, so the declared 90-day consumed/expired boundary is not fully implemented. Ten-year durable retention is expressed by policy/check/immutability source but not PostgreSQL-tested.

## 9. PostgreSQL design matrix - discovered only, NOT RUN

| Area | Discovered designs | Independent validity |
|---|---:|---|
| Prior direct behavior | 18 | Useful retained designs; execution remains required. |
| Prior application behavior | 7 | Useful retained designs; new transaction ownership needs valid execution evidence. |
| Correction 15 control-plane | 5 | Mostly completed-lease read/substitution; no real phase interruption or file-only state. |
| Correction 15 recovery | 6 | Synthetic unregistered denial inputs; no actual recovery/outcome/replay lifecycle. |
| Correction 15 purge/audit/role | 14 | Wrong runtime principal, absent setup, fake fault injection and sequential "concurrency" make most invalid. |
| Total | 50 | 50 discovered; 0 executed; **NOT RUN**. |

No test was executed against PostgreSQL. Discovery and compilation do not prove SQL behavior.

## 10. Independently reproduced offline validation

| Validation | Independent result |
|---|---|
| Build: `dotnet build SESS.NexaERP.slnx --no-restore --nologo` | PASS; 0 warnings, 0 errors |
| Focused database-safety + purchase-correction tests | PASS; 33/33 |
| Inclusive `Rev869B` excluding `Postgres` | PASS; 65/65 |
| Complete suite excluding `Postgres` | PASS; 439/439 |
| Exact three PostgreSQL classes, `--list-tests` only | 50 discovered; 0 executed; **NOT RUN** |
| PowerShell AST | PASS; 23/23 under 5.1.19041.6456 |
| EF migration list | PASS with inert loopback port 1 and `--no-connect`; 13; applied state unknown |
| Migration uniqueness/order | PASS; one REV869B immediately after REV869A |
| Model/designer/snapshot parity | PASS; 1/1 no-connect test |
| Offline REV869A-to-REV869B Up SQL | 260,733 bytes; SHA-256 `609D64ED91EEBF2E5CB6A09BFA364A0C11194812302063969070D8A2438DDEAC` |
| Offline REV869B-to-REV869A Down SQL | 10,120 bytes; SHA-256 `C815CA31188B7CB57C873D85BB7E5FEC3BDD5505A7A3CE6A3152944DF7AF1D0C` |
| Up inventory | 23 tables; 80 triggers including 8 constraint triggers; 32 function definitions / 31 distinct names; 46 FK clauses / 49 REFERENCES; 71 indexes; 57 checks |
| Down inventory | 6 DROP TRIGGER statements; 1 generated function definition; 59 DROP lines |
| Secret/privacy scan | 0 private-key, bearer-literal, client-secret-assignment or raw-OIDC-assertion matches in the exact diff |
| Prohibited generated-SQL scan | 0 CREATE/DROP DATABASE; 0 `pg_terminate_backend`; 0 durable security-audit DELETE |
| Exact correction diff | 9 files; 1,077 insertions; 265 deletions |
| `git diff --check` | PASS |
| Target-scoped tracked status before report | clean |

The offline SQL was generated with an inert loopback port-1 design identity and was not applied or parsed by PostgreSQL. Temporary SQL artifacts were used only for byte/hash/inventory calculation and removed. Passing offline tests does not alter any finding.

## 11. External prerequisites and future evidence

External prerequisites remain: separately provisioned control-plane database/owner/caller; exact registry relations/APIs and authorized request/recovery issuers; capability-free security owner, purge authorizer and purge executor; distinct runtime/issuer connections; protected recovery keys/evidence storage; and management-approved retention values. None was provisioned or accessed.

Those prerequisites are not automatically source defects. Helper readiness fails because the committed source does not yet define and verify their complete reproducible contract. Future PostgreSQL evidence is still required after source correction and a separately authorized provisioning/execution-plan review. Production, REV861, AWS, OIDC production, frontend, Docker, legacy applications, final migration acceptance and final production readiness remain closed.

## 12. Prohibited operations not performed and next gate

No PostgreSQL connection or test was made. No migration was applied/removed. No helper, database create/clone/restore/drop/quarantine/recovery, role/control-plane provisioning, purge, scheduler, production/REV861/AWS/OIDC/frontend/Docker/legacy operation, or `legacy-reference` content access occurred. No execution command for those operations is supplied here. Only this report is changed.

Exact next gate: perform a sixteenth controlled source-only correction against this report. Correct every BLOCKING finding, reproduce the offline gates, commit the controlled source/checkpoint only, and request a fresh independent source-only re-review. PostgreSQL execution is not the next gate.
