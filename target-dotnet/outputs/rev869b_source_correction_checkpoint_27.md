# REV869B source Correction checkpoint 27

## Decision and boundary

Correction 27 implements the reconciliation-selected Option A evidence pipeline within the exact nine-file allowlist. Fact-only PostgreSQL readers expose bounded typed observations; the trusted source-controlled .NET adapter validates raw scope, exact property sets, types, cardinalities, provenance and canonical raw digests; the separately frozen oracle supplies expectations; only the verifier calculates acceptance. Database/controller inputs cannot supply PASS, oracle expectations or assertion results.

This is a source-only implementation checkpoint, not an independent review and not database acceptance. PostgreSQL access, provisioning, migration execution, lifecycle/drop/purge/recovery/quarantine/export operations, production access, legacy-reference access and Correction 28 were not authorized or performed.

## Entry gate

| Gate | Evidence | Result |
|---|---|---|
| Authorized starting HEAD | `7e4d01f97c3eb8ac6cf402666c095fc54e49b3f1` | PASS |
| Required parent | `fb4ac96b9315e749cb1f586995221965d3f5a666` | PASS |
| Reconciliation | `outputs/rev869b_correction26_failure_evidence_interface_reconciliation.md` | PASS |
| Reconciliation SHA-256 | `5265F2F4C874888821385AB05598462D4E961551DF61EA0DDCD7987CE279FE13` | PASS |
| Reconciliation decision | Option A; Correction 27 GO | PASS |
| Inventory | 133 formula terms, 34 frozen scenarios, 108 explicit subcases | PASS |
| Worktree at entry | target-scoped clean | PASS |
| Architecture / ACL | `RETAIN` / `RETAIN` | PASS |
| Starting-to-ending scope | exactly the nine files listed below; ending commit is the immutable commit containing this checkpoint and is reported in the handoff | PASS |

## Exact exhaustive file scope

1. `tools/rev869b-control-plane-install.sql`
2. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs`
3. `tests/SESS.NexaERP.Tests/Rev869BControlPlaneProvisioningContract.cs`
4. `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs`
5. `tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs`
6. `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs`
7. `tests/SESS.NexaERP.Tests/Rev869BCorrection26FrozenOracle.cs`
8. `tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs`
9. `outputs/rev869b_source_correction_checkpoint_27.md`

No tenth file was created, renamed or modified.

## Consolidated root-cause implementation

| Root cause | Correction 27 implementation | Offline objective evidence | State |
|---|---|---|---|
| RC26-01 raw JSON had no typed adapter | Strict reader-specific v3 raw models, parser, canonical raw digest, adapter, envelope and verifier pipeline replace passthrough | Real `raw -> ParseTypedObservation -> AdaptAndVerifyDatabaseShapedEvidence -> VerifyEvidence` path passes pristine fixtures and rejects mutations | IMPLEMENTED_PENDING_PRECHECK |
| RC26-02 incomplete bounded reader contract | CP-L3, CP-A3, TC3, TP3, TE3 and TA3 fact-only readers use exact allowlists, scopes, types and cardinality | 133 selectors bind bijectively to v3 reader outputs | IMPLEMENTED_PENDING_PRECHECK |
| RC26-03 synthetic `BuildOracleEvidence` | Removed; independently authored database-shaped raw fixture facts are created without oracle operators/literals and then adapted | Source contract forbids the old builder and exercises all 108 raw fixtures | IMPLEMENTED_PENDING_PRECHECK |
| RC26-04 synthetic-only mutation coverage | 20 real pipeline mutations per subcase plus evidence tampering and removal of every decisive assertion | 2,160 pipeline mutations rejected; every plan assertion is tampered and individually removed through the real evaluator/contract gate | IMPLEMENTED_PENDING_PRECHECK |
| RC26-05 ACL scope/closure incomplete | CP-A3 and TA3 split global/control and company/target observations, including direct/default/inherited/aggregate/owner/PUBLIC/effective/admin/isolation facts | Exact principal/object/operation allowlists; verifier-only execute grants; PUBLIC revoked | IMPLEMENTED_PENDING_PRECHECK |
| RC26-06 purge typed/scope contract incomplete | TP3 binds organization, instance, lease/version, authorization, execution, root authorization, batch, purge attempt and stage | Exact equality predicates, attempt/execution equality, retry-root linkage and bounded candidate LIMIT | IMPLEMENTED_PENDING_PRECHECK |
| RC26-07 derived 133/34/108 symptoms | One pipeline correction plus individual selector/scenario/subcase contracts | 133/133 terms, 34/34 scenarios, 108/108 subcases represented | IMPLEMENTED_PENDING_PRECHECK |
| RC26-08 external prerequisites | Kept outside source; no contract weakening or fabricated execution claim | Listed under external prerequisites | EXTERNAL_PENDING |

## Evidence pipeline contract

Frozen versions are `REV869B-C27-ORACLE-v1`, `REV869B-C27-FORMULA-v1`, `REV869B-EVIDENCE-v3`, `REV869B-FACTS-v3`, `REV869B-ADAPTER-v3`, and `REV869B-MAPPING-v3`. The independent oracle computed SHA-256 is `6a1196cdad0bcbb086c771efb4f46f9b15db86aaabf6a1ff89e67afca5383bda`.

Selector distribution is exact: CP-A3 8, CP-L3 36, OR3 2 action-receipt selectors, TA3 15, TC3 32, TE3 16 and TP3 24; total 133. OR3 is authenticated action-receipt observation and cannot decide PASS by itself. Controller-audit material remains supplementary only.

The raw parser rejects non-exact root/scope/fact property sets, unrequested facts, duplicate facts, missing facts, wrong JSON types, wrong reader/stage/cardinality, cross-organization/instance/lease/version evidence and a noncanonical digest. The adapter binds scenario, subcase, preparation, attempt/execution, action, reader, stage and raw provenance before the verifier compares against the frozen oracle.

The 20 executable pipeline mutations are selector change, missing/extra/duplicated field, wrong type/count/state, fabricated history, cross-company, cross-instance, cross-lease, wrong lease version, stale/replayed, wrong oracle hash, wrong observation identity, wrong envelope identity, missing durable history, changed raw digest, broadened ACL/purge scope and removed decisive assertion. Each runs for all 108 subcases. Additionally, every decisive assertion in every scenario plan is tampered and individually removed; the actual evaluator or `ValidateContract` rejects the change.

## F23-01 preservation

The UTF-8 slice from `CREATE FUNCTION nexa.rev869b_authorize_normal_drop` to immediately before `CREATE FUNCTION nexa.rev869b_register_recovery_decision` remains 11,001 bytes with SHA-256 `34CAA290EBBDBC5CAAB5748E7019AB2A56118D664864412306A65739A41B8523`. Exact event, registration request, instance, lease, version, authorization and expected-pre-state binding therefore remains byte-for-byte preserved.

## Exact 34-scenario implementation matrix

Every row has unique frozen preparation, attempt, evidence, expected-result and action identities. `selectors` are executable formula terms sourced from the stated reader; `subcases` are individually bound and mutation-tested. PASS below means implemented source/offline contract PASS only, never PostgreSQL behavior acceptance.

| ID | Subcases | Selectors | Exact expected terminal/result | Authoritative evidence class | Offline result |
|---|---:|---:|---|---|---|
| P01 | 1 | 3 | ExternalVerified | CP-L3 provision/manifest/instance facts | PASS |
| P02 | 5 | 3 | PreflightDenied | CP-L3 mismatch-specific manifest/instance facts | PASS |
| P03 | 4 | 4 | VerificationDenied | CP-A3 catalogue/ACL drift facts | PASS |
| L01 | 3 | 3 | Ready | CP-L3 lease/event/cleanup facts | PASS |
| L02 | 6 | 5 | Ready | CP-L3 create/recovery transition facts | PASS |
| L03 | 5 | 5 | DropStarted | CP-L3 exact normal-drop event/request facts | PASS |
| L04 | 5 | 5 | Finalized | CP-L3 boundary/terminalization facts | PASS |
| L05 | 5 | 3 | Quarantined | CP-L3 mismatch/quarantine durable facts | PASS |
| R01 | 1 | 5 | Finalized | CP-L3 decision consumption/final facts | PASS |
| R02 | 8 | 3 | RecoveryAuthorized | CP-L3 replay/version/decision facts | PASS |
| R03 | 5 | 5 | Finalized | CP-L3 cleanup failure/fresh recovery facts | PASS |
| C01 | 1 | 5 | Committed | TC3 before/after/receipt/outcome facts | PASS |
| C02 | 1 | 5 | Committed | TC3 replay/original receipt facts | PASS |
| C03 | 1 | 4 | RequestRegistered | TC3 request binding/rejection facts | PASS |
| C04 | 5 | 4 | RolledBack | TC3 business/history rollback facts | PASS |
| C05 | 1 | 3 | RolledBack | TC3 durable noncommit terminal facts | PASS |
| C06 | 4 | 3 | FourExactInterruptionOutcomesReconciled | TC3 per-subcase attempt/outcome facts | PASS |
| C07 | 1 | 4 | AttemptStarted | TC3 concurrency/attempt facts | PASS |
| C08 | 8 | 4 | AttemptStarted | TC3 substituted-binding rejection facts | PASS |
| G01 | 5 | 3 | Denied | TP3 exact authorization/root/batch scope | PASS |
| G02 | 1 | 4 | ZeroRows | TP3 scoped eligibility/count/audit facts | PASS |
| G03 | 1 | 5 | Succeeded | TP3 before/deleted/durable audit facts | PASS |
| G04 | 1 | 4 | Failed | TP3 candidate drift/count facts | PASS |
| G05 | 3 | 3 | Failed | TP3 rollback and independent durable facts | PASS |
| G06 | 4 | 5 | Failed | TP3 concurrency/retry-root/attempt facts | PASS |
| E01 | 1 | 4 | Prepared | TE3 scoped minimized batch facts | PASS |
| E02 | 1 | 4 | Prepared | TE3 immutable batch/history facts | PASS |
| E03 | 4 | 3 | Denied | TE3 release authorization/binding facts | PASS |
| E04 | 3 | 5 | ReleaseRetrySequenceVerified | TE3 release/retry/attempt facts | PASS |
| A01 | 1 | 3 | Verified | CP-A3/TA3 effective privilege exact set | PASS |
| A02 | 7 | 3 | Denied | CP-A3/TA3 direct/inherited/PUBLIC/admin denial | PASS |
| T01 | 1 | 4 | InUse | CP-L3 fixture allocation/ownership facts | PASS |
| T02 | 1 | 5 | Finalized | CP-L3 durable cleanup/restart facts | PASS |
| T03 | 4 | 2 | MutationSensitive | Independent raw pipeline/evaluator mutation corpus | PASS |
| **Total** | **108** | **133** | **34 frozen IDs** | **no shared decisive evidence** | **34/34 PASS offline** |

## ACL, purge, scale and architecture

- Fact readers are `SECURITY DEFINER` with fixed `pg_catalog,nexa` search paths, exact fact-name allowlists and scoped arguments. Execute is revoked from PUBLIC and granted only to the appropriate verifier role. Runtime/admin/export roles are not broadened.
- CP-A3 and TA3 expose bounded effective-privilege facts without unrestricted ledger/export reads. Direct grants, inherited memberships, aggregate roles, ownership, default privileges, PUBLIC, administrative bypass and organization isolation are explicit selector facts.
- TP3 begins with organization/instance/lease/version/authorization/execution/root/batch/attempt equality scope. `execution_id == purge_attempt_id` is enforced and candidate work is limited by the authoritative maximum-row authorization.
- Target command contexts now retain organization identity and an organization/opened-token index; lifecycle events retain request/version and attempt/version indexes. Reads remain operation-scoped and bounded rather than enterprise-wide scans.
- External provisioning, dedicated lifecycle controller, surviving control-plane database and target-local transactional ledgers are unchanged. Purchase workflow, permissions, approvals, calculations and audit-history production paths were not redesigned.

## Offline validation

| Validation | Result |
|---|---|
| Build | PASS: 0 warnings, 0 errors |
| Correction 27 source-contract/mutation tier | PASS: 15/15 |
| Focused REV869B non-PostgreSQL tier | PASS: 75/75 |
| Complete non-PostgreSQL tier | PASS: 449/449 |
| Scenario discovery | PASS: exactly 34 unique tests; discovery-only |
| Formula/subcase inventory | PASS: 133/133 terms, 108/108 unique subcases |
| Pipeline mutation corpus | PASS: 20 x 108 = 2,160 rejected mutations, plus all decisive-assertion tamper/removal checks |
| Windows PowerShell AST | PASS: PowerShell 5.1.19041.6456, 24 scripts, 0 errors |
| EF discovery | PASS: `--no-connect`, inert `127.0.0.1:1`, 13 migrations |
| REV869A/REV869B | PASS: one primary and one designer each; REV869B immediately follows REV869A |
| Model/snapshot and retained SQL | PASS: 2/2 no-connect contracts |
| Offline Up SQL | PASS: 322,999 UTF-8 bytes; 2,635 lines; SHA-256 `B4D22AB600F2F7B27A8ACBD417B067ACC5D8A1488E513F562BEAAAD146781F1C` |
| Offline Down SQL | PASS: 11,700 UTF-8 bytes; 231 lines; SHA-256 `268D0FC8FCE08B7F3ADBE378879AD0A325965F784A87FC987D2BAF2FAFA42131` |
| F23-01 slice | PASS: 11,001 bytes; SHA-256 `34CAA290EBBDBC5CAAB5748E7019AB2A56118D664864412306A65739A41B8523` |
| Frozen oracle | PASS: SHA-256 `6a1196cdad0bcbb086c771efb4f46f9b15db86aaabf6a1ff89e67afca5383bda` |
| Secret/privacy scan | PASS: 1,354 added lines; 0 credential/private-key/PII matches |
| Prohibited-operation scan | PASS with adjudication: one `OpenAsync` source occurrence is the authorized future live verifier path and was not executed; no migration/provisioning/lifecycle/purge/export command invocation was added or run |
| SQL self-attestation scan | PASS: v3 fact-reader slices contain no oracle version/hash, expected outcome, assertion result or PASS state; the sole `oracleVersion` SQL-source match is retained CP-A2 compatibility outside v3 |
| ACL scan | PASS: verifier-only grants, PUBLIC revocation, fixed search paths and exact object/principal scopes retained |
| `git diff --check` | PASS |
| Exact scope | PASS before checkpoint: eight implementation files; after checkpoint: exact nine-file allowlist |

One initially over-broad focused-test filter admitted 25 PostgreSQL-labelled methods. The explicit opt-in guard rejected all 25 at `Create()` before opening any connection or performing database work. Those guard interceptions are excluded from product totals above. The corrected `FullyQualifiedName!~Postgres` invocations passed. PostgreSQL connections, commands and database-backed test executions remained zero.

## Implementation file hashes before commit

| File | SHA-256 |
|---|---|
| `tools/rev869b-control-plane-install.sql` | `A33185965355D529DF88CB2CA0F762288860C965DE7DB8D256EBA18EA3C1582A` |
| `Rev869BCommandContextSql.cs` | `691BE616374C4988B50B0388042B33D982044893B55EF4505501F995E1C0DC0F` |
| `Rev869BControlPlaneProvisioningContract.cs` | `8EDBCAAE7D805691ED712B47108A94E5B7B56A78BA6ED08899A33F0F34524E8F` |
| `Rev869BCorrection14PostgresDesignTests.cs` | `7C9CEE82A7A9FEAC726E2FE66CCB5BED69D6F8507179262BB83053E2B78B27D5` |
| `Rev869BCorrection17PostgresScenarios.cs` | `6244DD9EF9D92EE796FFE976750A2A97F2002A508F81FC0AAB06F6E4C3FC8B43` |
| `Rev869BCorrection17SourceContractTests.cs` | `1FD0E5A182B8EFBF1825EE3E2EAD781B15D582AE2A20639B01D56495F275DAD3` |
| `Rev869BCorrection26FrozenOracle.cs` | `A98CE20C980B070752B9DDB6AACFFA497ED0D6273F2BF9A72BA6B0EAE9151B58` |
| `Rev869BLifecycleControllerClient.cs` | `C4B467C529A88EFF3415F0C56F886028C2684A752613269CF9CA3941065DE227` |

## External prerequisites and mandatory next gate

External prerequisites remain blocking: management-owned production adapter ownership assignment, isolated PostgreSQL control/target databases, externally provisioned roles and ACLs, pinned controller/verifier identities and TLS/signing material, unique fixtures for all 108 subcases, authorized migration/provisioning application, and later database-backed behavioral acceptance. None is source-correctable or authorized here.

The single mandatory next gate is a separately authorized internal adversarial source-only precheck of the committed Correction 27. It must independently attack the typed reader -> parser -> adapter -> envelope -> verifier path and all 34 scenarios before any separate independent review or database execution authorization.

correction_27_source_implementation_state=COMPLETE_PENDING_PRECHECK
f23_01_state=PASS_RETAINED
f23_02_source_correction_state=IMPLEMENTED_PENDING_PRECHECK
evidence_pipeline_implementation_state=PASS
trusted_adapter_production_ownership_state=EXTERNAL_PENDING
formula_term_coverage_state=PASS
scenario_subcase_design_state=PASS
enterprise_scale_compatibility_state=PASS
frozen_architecture_state=RETAIN
acl_boundary_state=RETAIN
external_prerequisite_blocking_state=YES
rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN