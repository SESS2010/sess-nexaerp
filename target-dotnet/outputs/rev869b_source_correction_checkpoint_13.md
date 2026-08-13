# REV869B source correction checkpoint 13

Date: 2026-08-13 (Asia/Calcutta)

Authoritative workspace: `C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet`

Scope: controlled source-only correction. No PostgreSQL test, PostgreSQL connection, database helper, migration apply/remove, provisioning, purge, scheduler activation, quarantine recovery, backup/restore, production operation, REV861 access, or `legacy-reference` access was performed.

Starting commit: `bce0b13f8a8caf5d4f22a7140c76cfbfbba2f414`

Reviewed Correction 12 commit: `b0eaac705b9630717917ad6957f5a28fd0ceebbe`

Non-authoritative sibling excluded: `192d84fa1116975a09e9676a7d8c864f975380f5`

Ending commit: the commit containing this checkpoint; its exact hash is reported in the final handoff because embedding a commit's own hash would alter that hash.

Commit message: `Perform thirteenth controlled REV869B source correction`

## 1. Entry and continuation gates

The original gate passed before Correction 13 edits: exact HEAD, exact reviewed Correction 12 commit, exactly one independent report at HEAD, clean target, one REV869B immediately after REV869A, and only the untouched untracked `../legacy-reference/`. After management approval was supplied, target status showed only the seven paths already changed by this same active Correction 13 task; they were preserved. No unrelated target change was found.

Management approval: `MGMT-REV869B-SECURITY-LEDGER-20260813-001`, explicitly confirmed in the supplied management approval attachment. It authorizes source definition and offline validation only, not database or purge execution.

The complete independent report, Correction 12 checkpoint, authoritative Correction 12 range, and cited source/migration/authorization/quarantine/security-ledger/test files were read. The pre-edit matrix covered every blocking and required-correction item.

## 2. Finding matrix

| Finding | Exact source location | Root cause | Application correction | Database/source correction | Positive/adversarial evidence | Rollback/recovery and acceptance evidence |
|---|---|---|---|---|---|---|
| C11-01, C12-N01 exact authorization before mutation | `EfRev869BPurchaseService.cs`; RFQ/quotation, comparison/PO and material-follow-up partials | protected `ExecuteUpdateAsync` ran before exact future history slots were issued | exact histories are staged, `OpenPendingAuthorizationAsync` reserves/opens slots, protected CAS mutation follows, then `SavePreauthorizedChangesAsync` writes correlated history in the same transaction | persisted-parent slots now bind the next version; quotation Draft insert maps to action `Create` | structural inventory: 19 explicit opens and 18 preauthorized saves; all `ExecuteUpdateAsync` paths are preceded by an open; focused and static contracts pass | audit failure or CAS failure rolls back business/history work while independent grant/ordinal consumption remains durable; PostgreSQL behavior is not claimed |
| C11-02 operation/entity/org/actor/version/transition/history substitution | `Rev869BCommandContextAuthorizer.cs`, `Rev869BCommandContextSql.cs`, `Rev869BControlledMutationSql.cs` | future slot version previously reflected the old persisted version when the parent was updated with set-based SQL | `NextPersistedVersionAsync` binds every persisted status/approval/PO-history slot to old version + 1 | existing exact typed slot fingerprint and claim trigger remain authoritative | existing substitution design plus build/static contracts; PostgreSQL test `ExactGrantRejectsEveryOperationSlotAndPrincipalSubstitution` discovered, NOT RUN | acceptance requires later authorized PostgreSQL execution and independent review |
| C11-03 replay/savepoint/pooling | command-context SQL, authorizer and PostgreSQL design | call ordering made intended one-use claims unusable in application flows | one issuer grant is opened before mutation and is never reissued by the later save | transaction/backend/runtime/slot/ordinal bindings and nontransactional claim sequence retained | savepoint, replay, concurrency and independent-connection tests discovered, NOT RUN | no PostgreSQL behavior claimed |
| C11-04 qualification compatibility | qualification SQL/endpoints/tests retained | Correction 12 qualification contract needed preservation while ordering changed | qualification flows remain exact-context protected and independent-actor bound | retained Approved/Approved compatibility, verification/approval/rejection/correction guards unchanged | focused/non-PG suites pass; PostgreSQL qualification behavior remains NOT RUN | independent review must assess retained-data evidence |
| C11-05 current-version and late-child | safety SQL and purchase services | valid amendment/late-child behavior could not be reached while authorization ordering failed | amendment/rejected revision parents and histories are staged before one authorized save; predecessor reservation is preauthorized | current/terminal child guards and same-transaction amendment exception retained | current-version/late-child designs discovered, NOT RUN | no applied-schema claim |
| C11-06 least privilege | command-context SQL and `Rev869BTestDatabaseLease.cs` | security function inventory and runtime denial coverage needed to include retention control | helper now requires seven exact security functions owned by dedicated NOLOGIN owner | purge function is SECURITY DEFINER, owner-only, PUBLIC-revoked; runtime ledger/audit mutation revocations retained | runtime privacy/privilege test discovered, NOT RUN; static owner/access contracts pass | no runtime privilege acceptance claimed |
| C11-07 privacy/data minimization | command ledgers, lease evidence and recovery authorization | deterministic recovery reused evidence key; temporary policy was ungated | separate evidence and authorization keys; authorization payload stores no password/raw token/OIDC assertion/reusable secret; runtime exports must be `DISABLED` | ledgers retain fingerprints/principal names only; durable audit remains ten-year protected | key-distinctness, export-disabled, pseudonymous-ledger static contracts pass | exceptional export remains outside this task and requires separate MD approval |
| C11-08 rollback completeness | service transaction boundaries and existing PostgreSQL designs | mutation-first ordering prevented complete rollback scenarios | history, mutation and audit remain within one business transaction after independent claim opening | count mismatch in future purge raises and rolls back the batch | complete non-PG suite passes; application/direct rollback tests discovered, NOT RUN | winner/loser/database equality needs later PostgreSQL execution |
| C11-09, C12-N02 hard-interruption quarantine | `Rev869BTestDatabaseLease.CreateAsync`, `EstablishMarkerAsync`, recovery proof | role/database creation preceded any durable signed intent | signed exact `PreCreateIntent` is written before role/database creation; normal marker/evidence transitions remain | pre-marker recovery requires signed intent plus exact live database name, owner, source/migration proof and marker absence; filesystem evidence alone cannot satisfy live proof | static lifecycle contract covers pre-create, marker and quarantine states; PostgreSQL/hard-process interruption tests are NOT RUN | uncertain name/owner/source/marker state refuses recovery/drop; no automatic repair |
| C11-10, C12-N03 fresh recovery | `RecoverQuarantinedAsync`, `ConsumeRecoveryAuthorizationAsync` | instance-bound HMAC had no issue/expiry/nonce/purpose or consumption | JSON authorization binds ID, nonce, purpose, issued/expiry, exact instance/run/owner/source/migration/pre-state; max 15 minutes | distinct authorization key; atomic `FileMode.CreateNew` durable consumption prevents replay before target/drop attempt | static stale/expired/wrong-purpose/replay/key-separation contracts pass; recovery execution NOT RUN | any attempted recovery consumes authorization before destructive boundary; mismatch refuses DROP |
| C11-11 complete PostgreSQL design | two PostgreSQL behavior classes and lease | runtime execution was incompatible with application ordering and lacked new lifecycle controls | service sources now follow intended order; independent DbContexts/connections retained | exact SQLSTATE/object evidence and zero-row rejection retained | 25 exact REV869B PostgreSQL tests discovered, NOT RUN | behavioral completeness remains an independent-review and authorized-execution question |
| C11-12 management retention | `Rev869BCommandContextSql.cs`, `Rev869BTestDatabaseLease.cs` | temporary retention was unresolved and helper execution was ungated | helper fails closed unless exact approval, 15-minute max, 90-day temporary, ten-year durable, exports-disabled, bounded batch and schedule configuration are present | owner-only future purge: exact 90-day eligibility, 1..1000 batch, advisory serialization, row locks/SKIP LOCKED, exact count match, durable minimized evidence; migration does not invoke it | static retention/privacy/purge contract passes; no purge or scheduler executed | future execution requires separate explicit authorization |
| C12-N04 checkpoint reproducibility | this checkpoint | prior focused count omitted filter | exact three-class focused filter and inclusive filter are recorded below | not applicable | reproducible counts recorded | satisfied as documentation evidence, not source acceptance |

## 3. Authorization and atomicity evidence

For set-based transitions, the order is: validate actor/organization/role/data/current entity/state; stage exact status/approval/PO history; calculate the next-version slot; open the exact single-use context; execute CAS mutation; save the preauthorized history; write audit; commit. Creation paths stage parent, children, numbering update and exact creation history in the change tracker, then open authorization inside `SaveAuthorizedChangesAsync` before the first database save. Number allocation is only tracked before authorization and is persisted in the authorized transaction.

Exact slot binding retains claim kind, history ID, entity type/ID, operation, next parent version, from/to status, actor employee, OIDC issuer/subject, role, organization, correlation, remarks, runtime principal, backend, transaction, expiry and nontransactional ordinal.

## 4. Quarantine and recovery lifecycle

Lifecycle states represented in source: `PreCreateIntent` -> `OwnedActive` -> `Quarantined` -> `Dropped`. Pre-create intent is signed before any role/database creation. The normal database marker binds ownership token, run, exact database/source, source and migration fingerprints, family/scenario, expected owner, creation time and quarantine state. Recovery accepts only exact `Quarantined` marker proof or an exact `PreCreateIntent` plus independent live database owner/source/migration/marker-absence proof.

Recovery authorization purpose is `REV869B_QUARANTINE_DROP_V1`; it binds a unique authorization ID, high-entropy nonce, issued/expiry timestamps (maximum fifteen minutes), exact database/run/owner/source/migration/pre-state tuple and separately governed key. `FileMode.CreateNew` records one-use consumption before recovery verification/drop, so success, mismatch, interruption and replay cannot reuse the authorization. Active connections still cause refusal; wildcard names, FORCE and broad termination remain absent.

## 5. Approved ledger policy

- Approval: `MGMT-REV869B-SECURITY-LEDGER-20260813-001`.
- Temporary command authorization: exact slots, single-use, existing 30-second grant expiry (stricter than approved 15-minute maximum), no secrets/raw tokens/raw OIDC assertions.
- Temporary operational metadata: exact expired state and age older than 90 days; future purge batches 1..1000; serialized and row-locked; count mismatch rejects transaction.
- Durable non-sensitive audit: minimum ten years; future purge evidence includes approval reference, executor, execution/cutoff, candidate/deleted/retained-audit counts, outcome and reason.
- Permanent purchase/approval/status/qualification/business histories are not purge targets.
- Runtime has no ledger ownership or unrestricted ledger/audit mutation/export access. Exports are fail-closed `DISABLED`; exceptional export is not implemented or authorized here.
- Migration installation defines but never invokes the purge. No schedule was created or activated.

## 6. Offline validation

| Gate | Result |
|---|---|
| PowerShell 5.1 AST | PASS; 23/23 tool scripts; PowerShell `5.1.19041.6456` |
| Build | PASS; 0 warnings, 0 errors |
| Focused three-class filter (`Rev869BPurchaseFoundationTests`, `Rev869BPurchaseCorrectionTests`, `Rev869BPurchaseBehaviorTests`) | PASS; 44/44 |
| Inclusive `FullyQualifiedName~Rev869B&FullyQualifiedName!~Postgres` | PASS; 61/61; +2 static Correction 13 contracts from baseline 59 |
| Complete `FullyQualifiedName!~Postgres` | PASS; 435/435; +2 from baseline 433 |
| Exact PostgreSQL behavior discovery | 25 tests discovered; **NOT RUN** |
| EF migrations list | PASS with repository tool and `--no-connect`; 13 migrations; REV869A followed immediately by one REV869B; applied state unknown |
| Pending model/model snapshot | PASS in focused test `RuntimeModelMatchesSnapshotWithoutPendingChanges`; no connection |
| Offline Up SQL | PASS; 231,112 bytes; SHA-256 `25C23AF75B339E3FC106372396A29F9D83377424ACB16E439F22D522D16A2EDD` |
| Offline Down SQL | PASS; 9,205 bytes; SHA-256 `DB39C1A36405A3C40763F9D40589B5B86E770F766D882822E044DF409F90CF36` |
| SQL inventory | 19 CREATE TABLE; 77 triggers; 28 function definitions / 27 distinct names; 46 FK clauses; 69 indexes; 42 CHECK clauses; 48 Down DROP statements |
| Authorization ordering scan | PASS; all protected set-based mutations have prior explicit context open; creation saves open inside authorized save |
| Slot/replay/savepoint scan | PASS as static design; PostgreSQL behavior NOT RUN |
| Quarantine/recovery freshness scan | PASS as static design; pre-create intent, max-15-minute expiry, purpose/nonce/ID, distinct key and atomic one-use consumption present |
| Ledger privacy/retention/purge scan | PASS as static source; exact approval, 90 days, bounded locked batch, owner-only function, count match and ten-year evidence present |
| `git diff --check` | PASS; only line-ending normalization warnings |

Offline SQL was generated by EF's model-to-SQL command with inert loopback design-time identity. This EF version does not expose `--no-connect` for `migrations script`; generation completed without database access. Temporary SQL files were used only to compute inventory, sizes and hashes.

## 7. Exact controlled paths

1. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs`
2. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BControlledMutationSql.cs`
3. `src/SESS.NexaERP.Infrastructure/Persistence/Rev869BCommandContextAuthorizer.cs`
4. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs`
5. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.RfqQuotation.cs`
6. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.ComparisonPo.cs`
7. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.MaterialFollowUp.cs`
8. `tests/SESS.NexaERP.Tests/Rev869BDatabaseSafetyContractTests.cs`
9. `tests/SESS.NexaERP.Tests/Rev869BPurchaseCorrectionTests.cs`
10. `tests/SESS.NexaERP.Tests/Rev869BTestDatabaseLease.cs`
11. `outputs/rev869b_source_correction_checkpoint_13.md`

Schema/model impact: no EF entity, table, column, FK, index, CHECK, designer or snapshot model change. Existing REV869B migration ID and 13-migration topology are unchanged. Migration SQL source adds one owner-only function definition and matching Down removal.

## 8. Remaining blockers and explicit non-claims

No PostgreSQL behavior, database ownership state, migration applicability, purge behavior or recovery behavior was executed. The 25 PostgreSQL designs remain **NOT RUN**. A fresh independent source-only safety re-review is mandatory and may identify further source corrections.

This checkpoint does not claim `rev869b_source_safety_state=PASS`, `rev869b_execution_helper_readiness_state=PASS`, PostgreSQL acceptance, migration acceptance, production readiness, or final REV869B acceptance.
