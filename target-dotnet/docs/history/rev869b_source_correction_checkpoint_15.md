# REV869B Source Correction Checkpoint 15

Date: 2026-08-13 (Asia/Calcutta)

Starting commit: `5ca141178d936259883e704969f0217eba48be31`

Ending commit: the commit containing this checkpoint (reported after commit; a Git commit cannot embed its own SHA-1 without changing that SHA-1).

Required commit subject: `Correct REV869B control-plane safety checkpoint 15`

Scope: controlled source-only Correction 15. PostgreSQL, PostgreSQL tests, database helpers, registry/role provisioning, migration application/removal, database creation/drop/recovery/quarantine, purge, production, REV861, AWS, OIDC production, frontend, Docker, REV869C and legacy applications were not accessed or executed.

## Entry gate

The exact starting HEAD was `5ca141178d936259883e704969f0217eba48be31`, whose parent was Correction 14 source commit `f8a87fc8313405478765aeddb28f591371b27fce`. Subjects, target-scoped cleanliness, complete Correction 14 checkpoint/report, and report terminal next action matched. EF `--no-connect` listed 13 migrations with the retained REV869B exactly once immediately after REV869A. Global status contained only the pre-existing untracked `../legacy-reference/` boundary. Its contents and two ZIP files were not enumerated, read, copied, staged or changed.

## Six-finding correction matrix

| Finding | Root cause | Controlled correction | Offline/future evidence and acceptance formula |
|---|---|---|---|
| C14-N01 | Registry readiness checked only two APIs; markers omitted commit/policy/expiry/state; ordinary cleanup and quarantine bypassed authoritative transitions; roles-only and pre-marker interruption states were not recoverable. | `Rev869BControlPlaneRegistry` now verifies seven exact APIs, four registry relations, append-only triggers, owner, `SECURITY DEFINER`, fixed search path, caller EXECUTE, PUBLIC denial and zero direct table DML. Lease identity is derived from assembly source revision and any supplied revision must match. Target/supplemental evidence cross-binds source commit, policy, request/expiry, state and marker fingerprint. Every use reads the exact lease; normal drop durably begins and ends in the registry; quarantine no longer suppresses failure; roles-only/pre-database recovery is explicit and idempotent. | Offline source ordering and field/API counts pass. Future designs cover missing registry, stale lease, marker mismatch, interruption phases and independent backends. Acceptance: one exact lease + one legal state transition + one terminal outcome; otherwise no test connection or DROP. |
| C14-N02 | Recovery sent only a partial lease; supplemental consumption sat outside the terminal-outcome boundary; issuer/executor/readback were incomplete; outcome could be lost around DROP. | Recovery consumption now supplies all lease fields independently, including request issuer/authority, and first reads the exact registry lease/state/marker. Supplemental replay evidence is inside the post-consumption `try`; every failure attempts a durable non-reusable terminal outcome. The registry records the authoritative success before supplemental file evidence. Pre-create/roles-only states are reconciled through the administrative catalogue without assuming a target can open. | Future designs bind wrong issuer, exact pre-state, expiry, replay and terminal outcome to exact SQLSTATE/object labels. Acceptance: exact lease readback + fresh one-use approval + observed exact pre-state + terminal Succeeded/Failed; consumed approval is never reusable. |
| C14-N03 | Database-owner text was treated as purge approval; rejection/partial/failure evidence was missing or optional; eligible scope/state and candidate drift were not enforced. | Fresh approvals can only be registered by distinct `nexa_rev869b_purge_authorizer`; issuer is `session_user`, not caller text. Approval fingerprint, organization fingerprint, database, policy, 90-day cutoff, batch/maximum rows, exact states, nonce, executor and 15-minute expiry are bound. A separate append-only rejection ledger records missing/expired/replayed/substituted attempts. Eligible pre-count is organization-scoped and unclaimed; the exact candidate-set fingerprint/count must match execution. PL/pgSQL exception handling rolls back attempted deletion then appends Failed/PartialFailure evidence and marks retry eligibility. Attempt rows FK to authorization and have one terminal-outcome index. The unsafe caller-supplied failure recorder was removed. | Offline SQL inventory/static contracts pass. Future zero-row, replay/concurrency, drift, partial failure, retry and protected-history designs are distinct and NOT RUN. Acceptance: independently issued one-use approval + exact candidate equality + deleted count equality + one terminal evidence row; no durable audit deletion. |
| C14-N04 | Purge/security roles were checked only for LOGIN shape; inherited capabilities, memberships, object ownership and pre-existing ACLs were not closed. | Source now requires capability-free security owner, purge authorizer and purge executor roles: no SUPERUSER, CREATEDB, CREATEROLE, REPLICATION or BYPASSRLS; closed memberships; one non-inheriting SET ROLE migration-owner membership for the NOLOGIN security owner; no purge-role object ownership. Existing purge table/function/schema ACLs are revoked before only schema USAGE and exact function EXECUTE are granted. Exact ACL closure is checked. The security owner receives only the identity/role reads required by command authorization and is explicitly denied broad purchase-table privilege by the closure proof. Helper readiness re-verifies attributes, memberships, ownership, exact function ACLs and zero direct ledger DML. | Offline role/ACL source scans and focused tests pass. Future role-misconfiguration/export/direct-DML tests are compiled and NOT RUN. Acceptance: exact role topology + exact three purge API grants + no direct ledger/business DML; otherwise migration/helper fails closed. |
| C14-N05 | Durable audit declared terminal events but wrote only Issued/Opened/Claimed/Expired; rollback erased opened/claimed evidence and no minimized terminal failure category remained. | Added `rev869b_record_command_outcome`. Each exact grant is returned to the service and correlated through a service-owned serializable transaction. Committed events are staged inside the business transaction before commit, so audit insertion failure blocks commit. Context-open rejection and post-rollback Failed/Rejected events are independently appended by the issuer connection from durable Issued evidence. Fixed minimized failure categories prohibit arbitrary payloads. Terminal events are single-use per exact command fingerprint. | Focused static tests verify Committed/Failed/Rejected paths and absence of raw credentials. Future rollback/audit-failure/cross-command designs are compiled and NOT RUN. Acceptance: every issued slot has exactly one minimized terminal event; protected commit cannot occur without its Committed audit. |
| C14-N06 | The 25 new facts all delegated to one unrelated body. | Replaced all 25 aliases with scenario-specific control-plane, recovery, purge, privilege and audit arrangements. Each definition supplies a distinct acceptance label and, where applicable, exact SQLSTATE, database object and independently verified post-state. Shared execution plumbing still creates one proof-bound disposable database per fact and opens independent actor/verifier backends; scenario behavior is no longer one common assertion body. | Exact discovery: 25 corrected designs + 18 direct behavior tests + 7 application behavior tests = 50; executed count 0, **NOT RUN**. Acceptance later requires the explicit disposable database gate and exact scenario evidence. |

## Changed-file list

1. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs`
2. `src/SESS.NexaERP.Infrastructure/Persistence/Rev869BCommandContextAuthorizer.cs`
3. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs`
4. `tests/SESS.NexaERP.Tests/Rev869BControlPlaneRegistry.cs`
5. `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs`
6. `tests/SESS.NexaERP.Tests/Rev869BDatabaseSafetyContractTests.cs`
7. `tests/SESS.NexaERP.Tests/Rev869BPurchaseCorrectionTests.cs`
8. `tests/SESS.NexaERP.Tests/Rev869BTestDatabaseLease.cs`
9. `outputs/rev869b_source_correction_checkpoint_15.md`

No earlier migration, retained migration ID, EF model, designer or snapshot was changed.

## Control-plane ownership contract

The separately provisioned exact database remains `sess_nexaerp_rev869b_control_plane`, owned by `nexa_rev869b_control_plane_owner`. Pooling is disabled. Readiness requires seven exact owner-installed, fixed-search-path, SECURITY DEFINER APIs and four registry relations with append-only outcome enforcement. The caller must have exact API EXECUTE and no SELECT/INSERT/UPDATE/DELETE on registry tables.

Lifecycle:

`Reserved/PreCreateIntent -> target roles/database -> fully bound marker -> OwnedActive -> DropStarted -> Dropped`.

Any create/marker/use/cleanup mismatch transitions or remains fail-closed as `Quarantined/Failed`. Target/file evidence is supplemental. Test connection, quarantine, normal cleanup and recovery each reconcile the full database/run/token/family/scenario/source/source-commit/migration/owner/request/expiry/runtime/issuer/policy/marker tuple with the registry.

## Authorization issuance, consumption and replay contract

Command authorization remains issuer-reserved on an independent non-pooled connection, bound to organization, actor, OIDC issuer/subject, role, runtime principal/backend/transaction, exact operation/entity/record/version/from/to/correlation/remarks slot and nontransactional claim ordinal. The application carries exact grant IDs through one owned transaction. Committed audit is staged before commit; rollback/rejection is appended through the bound issuer after rollback. Savepoint, pooling, cross-organization, cross-command and slot substitution remain fail-closed.

Recovery authorization binds authorization ID, nonce fingerprint, purpose, approval issuer/authority, organization-equivalent exact target tuple, database instance, owner, source commit/fingerprint, migration, runtime/issuer roles, exact pre/post state, reason/reference, executor, issue/expiry and target fingerprint. Maximum lifetime remains 15 minutes. Registry consumption is atomic, single-winner and permanently non-reusable.

## Recovery state machine and evidence contract

1. Read complete registry lease in the exact recorded pre-state.
2. Verify signed supplemental evidence and exact marker fingerprint.
3. Validate fresh issuer/executor/nonce/pre/post-state approval.
4. Atomically consume it in the registry.
5. Put every supplemental I/O and target/catalogue operation inside the terminal-outcome boundary.
6. Re-prove target marker or registry-proven pre-create absence.
7. Drop only the exact owned target/roles with no broad connection termination.
8. Append authoritative Succeeded/Failed outcome, then write supplemental evidence.

Roles-only and database-absent pre-create interruptions no longer require opening a nonexistent target. A changed state, replay, active connection, unexpected name, source mismatch or outcome failure remains quarantined/reconcilable and never authorizes name-only repair.

## Purge authorization, retention, failure, retry and audit contract

Policy remains `MGMT-REV869B-SECURITY-LEDGER-20260813-001`.

- Temporary approval maximum: 15 minutes.
- Consumed/expired operational metadata retention boundary: 90 days.
- Durable minimized security audit retention: at least 10 years.
- No automatic migration-time or scheduled purge.
- No durable audit/history purge.
- Each execution requires a new separately issued approval fingerprint and nonce.
- Organization-scoped eligible count and exact bounded candidate-set fingerprint are durable before deletion.
- Zero rows is `ZeroRows`, never false `Succeeded`.
- Candidate drift/count mismatch rolls back deletion and records a terminal retry-eligible failure.
- Retry requires a fresh execution authorization; consumed approval is not restored.

## Runtime/security/purge role permission matrix

| Principal | LOGIN | Allowed | Explicitly denied |
|---|---:|---|---|
| Ordinary runtime | yes | exact business tables/functions and bound command-context APIs only | security/purge ledgers; durable audit export/update/delete; purge APIs |
| Command issuer | yes | issue exact grants; append fixed Failed/Rejected terminal outcomes for its grants | business mutation; direct ledger access/export |
| `nexa_rev869b_security_owner` | no | owns security tables/functions; exact employee identity/role SELECT needed by authorization; function-mediated audit insertion | broad purchase/RFQ table privileges; LOGIN; elevated role capabilities |
| `nexa_rev869b_purge_authorizer` | yes | schema USAGE and exact fresh-approval registration function | purge execution; table DML/SELECT; object ownership; elevated capabilities/memberships |
| `nexa_rev869b_purge_executor` | yes | schema USAGE and exact begin/purge functions | approval issuance; table DML/SELECT; durable audit deletion/export; object ownership; elevated capabilities/memberships |
| Migration owner | existing | non-inheriting SET ROLE membership solely to assign security ownership during migration | inherited security-owner access; purge-role membership |

## Durable versus temporary ledger separation

`rev869b_command_grants`, contexts, claim sequence assignments and purge authorizations are temporary operational metadata. `rev869b_command_security_audits`, purge attempt audits and purge rejection audits are minimized append-only durable evidence. Temporary purge can append Expired outcomes and remove eligible temporary grants/contexts only; it cannot update/delete/export the durable ledgers or business audit/history. No password, raw token, OIDC assertion, nonce, reusable credential or command remarks are written to durable command audit.

## Future PostgreSQL inventory — NOT RUN

- 18 direct database behavior tests.
- 7 application/service behavior tests.
- 25 Correction 15 scenario-specific control-plane/recovery/purge/audit designs.
- Total discovered: **50**.
- Total executed: **0**.
- Status: **NOT RUN**.

Future execution requires explicit management authorization, the exact disposable database opt-in, separately provisioned registry/roles, independent connections/DbContexts, exact winner/loser cleanup, rollback evidence and proof-bound quarantine/cleanup. No PostgreSQL acceptance is claimed.

## Offline validation

| Gate | Result |
|---|---|
| `dotnet build SESS.NexaERP.slnx --no-restore` | PASS; 0 warnings, 0 errors |
| Focused REV869B source/offline contracts | PASS; 33/33 |
| Inclusive REV869B non-PostgreSQL | PASS; 65/65 |
| Complete non-PostgreSQL suite | PASS; 439/439 |
| Exact REV869B PostgreSQL class discovery | 50 discovered; 0 executed; **NOT RUN** |
| PowerShell 5.1 AST | PASS; 23/23; version `5.1.19041.6456` |
| EF discovery | PASS with `--no-connect`; 13 migrations; REV869A followed immediately by one REV869B; applied state unknown |
| Exact model/snapshot parity | PASS; 1/1 no-connect test |
| Offline Up SQL | 260,733 bytes; SHA-256 `609D64ED91EEBF2E5CB6A09BFA364A0C11194812302063969070D8A2438DDEAC` |
| Offline Down SQL | 10,120 bytes; SHA-256 `C815CA31188B7CB57C873D85BB7E5FEC3BDD5505A7A3CE6A3152944DF7AF1D0C` |
| Up inventory | 23 tables; 80 triggers; 32 function definitions / 31 distinct names; 46 explicit FOREIGN KEY clauses / 49 REFERENCES tokens; 71 indexes; 57 CHECK occurrences |
| Down inventory | 6 DROP TRIGGER statements; 1 generated function definition; 59 DROP lines |
| Secret scan | 0 private-key/client-secret/bearer findings |
| Privacy review | generated role passwords are high-entropy, parameterized, process-memory-only and not logged/persisted; no raw token/assertion/nonce durable logging introduced |
| Forbidden-operation review | expected proof-bound future helper CREATE/DROP only; no FORCE, broad termination, production operation or business/durable-audit purge introduced |
| `git diff --check` | PASS before checkpoint creation; final check repeated before commit |

Offline SQL used an inert loopback port-1 design identity and was never applied. Temporary SQL files were deleted after hashing/inventory.

## Remaining external provisioning dependencies

Before any separately authorized PostgreSQL/helper execution:

1. Provision the exact control-plane database, owner, four relations, append-only triggers and seven APIs with the exact argument counts/state constraints/ACLs required by source.
2. Provision the authorized registry request issuer and recovery approval issuer/executor bindings.
3. Provision `nexa_rev869b_security_owner`, `nexa_rev869b_purge_authorizer` and `nexa_rev869b_purge_executor` with the exact capability, membership and ACL matrix above.
4. Provision distinct runtime/command-issuer connections for each disposable target.
5. Supply the approved retention/privacy environment values and separately authorized fresh recovery/purge approvals.

These are external gates, not source-safety acceptance. Provisioning was not performed.

## Explicit exclusions and next gate

No PostgreSQL connection/test, helper, migration apply/remove, database lifecycle, recovery, quarantine, purge, provisioning, production/REV861/AWS/OIDC-production/frontend/Docker/REV869C/legacy operation occurred. `../legacy-reference/` remained untracked, unread and untouched.

This checkpoint does **not** declare source safety PASS, execution-helper readiness PASS, database acceptance, migration acceptance, production readiness or final REV869B acceptance.

The only next gate is a **fresh independent source-only safety re-review of the committed Correction 15 diff**.
