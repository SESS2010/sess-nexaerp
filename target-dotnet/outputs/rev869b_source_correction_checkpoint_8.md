# REV869B source correction checkpoint 8

## Gate, scope, and disposition

- Starting commit: `7fd9539421a59793a311f22ff877383ea0b0db5e`.
- Required parent and observed parent: `e5715cafb66d896c0a7af542bb3de89af4638413`.
- Ending commit: the commit containing this checkpoint; its non-self-referential hash is reported in the final handoff.
- Entry target-scoped status was clean.
- Work was limited to the authorized target workspace. `../legacy-reference/` was not accessed or modified.
- Retained migration ID remains `20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation`; no migration was created, applied, removed, or renamed.
- PostgreSQL tests compiled/listed only and were **NOT RUN**. No PostgreSQL server or helper was accessed.

## Authoritative finding-to-correction matrix

| Independent finding | Eighth correction |
|---|---|
| Deferred Material Follow-up history used nonexistent `TransitionCorrelationId` | Bound the deferred check to its declared `CorrelationId`; same-status aggregate branches now always initialize the exact parent correlation. |
| Qualification verifier/approver fields had no production writer | Added authenticated Verify and Approve endpoints, expected-version checks, independent employee identities, role checks, server timestamps, exact version increments, audit, and immutable controlled-configuration histories. |
| Qualification snapshot could be fabricated from a matching row | Added database provenance validation requiring the live approved qualification and exact same-lifecycle Verify/Approve histories, actor IDs, versions, organization, effective time, and snapshot reconciliation. |
| Retained nullable qualification rows had no transition policy | Existing actorless approved rows remain immutable and invitation-ineligible; they do not block a new independently verified replacement on a distinct effective range. Pending or fully controlled overlapping rows still block. |
| Correlation/actor/creator data was caller-fabricable | Service transactions set transaction-local employee/login/role/organization command context; explicit mutation/history guards require exact context agreement. Creator separation resolves the parent's creator login to exactly one active employee identity and compares immutable employee IDs. |
| Direct PostgreSQL suite consumed an external named graph | Every method now creates a deterministic disposable database, proves exact name absence, creates its complete owned graph, verifies exact prerequisites, disables pooling, and drops only that database in `finally`. |
| Direct statements reached the wrong guard or zero rows | Copy inserts set exact initial correlations and deterministic unique sequences; mutations set fresh correlations and histories; the assertion helper rejects zero affected rows and verifies exact SQLSTATE plus object evidence including column identity. |
| Rollback baseline contradicted fixture prerequisites and only summed counts | Failure tests capture the independent pre-state and compare the independent post-state. The snapshot includes per-relation counts and a SHA-256 fingerprint of canonical full rows across all 15 REV869B tables plus audit, numbering, identity, warehouse, PR, PR-line, and handoff tables. |
| Cleanup could skip database disposal | Creation and disposal paths use outer `finally` lease disposal; immutable business/history rows are never individually deleted. The entire proven-owned database is force-dropped and exact absence is verified. |
| Concurrency evidence was incomplete | Application tests use two independent DbContexts/connections/services with a coordinated gate, same organization/key, same-payload replay and conflicting payload paths, then verify the authoritative winner and absence of loser artifacts through a third independent context. Direct collision uses two independent connections and exact unique-index evidence. |
| Mapped endpoint coverage omitted scope/audit/cross-org paths | The real ASP.NET routing/auth/filter/service/EF design now covers 400/401/403/404/409/success, record-scope denial with persisted denial audit, known-record cross-organization 404 masking, attachment/export permission denial, and fail-closed audit-writer failure. |
| Rejected-PO repeated revision path omitted command history | Each revision, resubmission, and rejection now has deterministic IDs, fresh correlation, exact action/from/to/revision/version history, and a nonzero-row assertion while preserving root/previous ancestry. |
| Trigger/function runtime contracts were stale | Exact runtime inventories include bound technical and qualification triggers plus qualification lifecycle/provenance functions. |

## Exact changed files

1. `src/SESS.NexaERP.Api/Endpoints/Rev869AConfigurationEndpoints.cs`
2. `src/SESS.NexaERP.Application/Rev869A/Rev869AContracts.cs`
3. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BControlledMutationSql.cs`
4. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BDatabaseSafetySql.cs`
5. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs`
6. `tests/SESS.NexaERP.Tests/Rev869BCompleteGraphSeeder.cs`
7. `tests/SESS.NexaERP.Tests/Rev869BCompleteGraphSeeder.Transactions.cs`
8. `tests/SESS.NexaERP.Tests/Rev869BOwnedPostgresDatabase.cs`
9. `tests/SESS.NexaERP.Tests/Rev869BPostgresApplicationBehaviorTests.cs`
10. `tests/SESS.NexaERP.Tests/Rev869BPostgresBehaviorTests.cs`
11. `tests/SESS.NexaERP.Tests/Rev869BPurchaseCorrectionTests.cs`
12. `outputs/rev869b_source_correction_checkpoint_8.md`

## Complete self-owned fixture design

The direct suite derives deterministic database names and all entity IDs/codes from SHA-256 of the test method/scenario. It checks that the database does not exist, clones only the accepted isolated schema/template database, disables connection pooling, and then creates its own category, UOM, item, vendor, qualification, warehouse, Rack/Bin, tax rule, three identity mappings, PR, PR line, procurement handoff, all 15 REV869B-owned relation types, qualification histories, and numbering state. It references accepted seed employees by stable code but does not add employees or shared human logins.

The deterministic seed temporarily disables USER triggers only inside the proven disposable owned database so a complete historical starting graph can be loaded; constraints and foreign keys remain active. Triggers are re-enabled in `finally`, and exact existence checks run before the test connection is returned. No immutable trigger is weakened in migration/application source.

| Fixture consumer | Owned prerequisites | Ownership/cleanup proof |
|---|---|---|
| Seven application PostgreSQL methods | Scenario organization, identity mapping, warehouse, PR, PR line, handoff; service creates tested business rows | Exact deterministic DB absence before create; pooling false; outer-finally drop with quoted identifier and post-drop absence check |
| Fifteen direct PostgreSQL methods | Complete deterministic support and all 15 REV869B relations | Per-method deterministic DB and graph; exact required-row checks; peer connections verify same owned DB; whole DB dropped in finally |
| Concurrency peers | Same owned DB, organization, command identity and key | Independent contexts/connections; committed winner retained only until owned DB disposal |
| Histories | Seed histories and runtime histories are test-owned | No row-level history deletion; immutable-history-safe whole-database disposal |

## SQLSTATE/database-object and zero-row matrix

| Intended evidence | Exact SQLSTATE | Exact object/evidence | Zero-row prevention |
|---|---:|---|---|
| Idempotency collision | `23505` | `IX_request_for_quotations_OrganizationId_IdempotencyKey` | Distinct deterministic RFQ sequence/number; insert must affect one |
| Terminal aggregate insert | `P0001` | `rev869b_enforce_transition` | Complete source row required; helper rejects zero |
| PO issue tamper | `23514` | `rev869b_po_issue_allowlist` | Exact owned approved PO selected |
| Version skip/lower | `40001` | `rev869b_exact_version_increment` | Exact ID/version predicate and nonzero guard |
| PO org/totals/policy tamper | `23514` | `rev869b_po_approval_allowlist` | Fresh command context/correlation supplied so intended allowlist is reached |
| Immutable JSON/tax/provenance mutation | `P0001` | `rev869b_reject_immutable_mutation` | Exact owned PO lines selected |
| Controlled delete | `23514` | `rev869b_controlled_delete_guard` | Exact owned relation selected; never used for cleanup |
| Late child | `P0001` | `rev869b_validate_child_insert` | Deterministic source child and terminal parent required |
| Audit null ID | `23502` | exact `audit_logs|Id` table/column evidence | Protected RFQ reservation first affects exactly one |
| Immutable histories | named immutable mutation/delete guards | status, approval, and PO-history relations | Each attempt targets an exact existing owned row |

`AssertPostgresGuardAsync` fails if a command completes normally or reports zero affected rows; for PostgreSQL errors it checks SQLSTATE and the concatenated constraint/table/column/routine evidence.

## Rollback verification matrix

| State family | Independent evidence |
|---|---|
| RFQ parent/lines | Exact counts plus canonical full-row fingerprint |
| Invitations, quotations, quotation lines, technical verifications | Exact counts/full rows |
| Comparisons/lines and approval histories | Exact counts/full rows |
| POs/lines, PO histories, follow-ups | Exact counts/full rows |
| Status histories and approval policies | Exact counts/full rows |
| Audit and idempotency/correlation fields | Exact counts/full rows |
| Number-series consumption | Exact count and full sequence row |
| Identity, warehouse, PR, PR line, procurement handoff | Exact counts/full supporting rows |
| Observation boundary | Fresh independent DbContext/connection before and after failure |
| Disposal | Lease disposal is in `finally`; database absence verified |

## Concurrency design

- Two independent DbContexts, physical connections, and service instances.
- Coordinated simultaneous release without `Task.Delay`.
- Same organization and idempotency key.
- Same-payload case returns the same authoritative ID.
- Conflicting-payload case returns conflict.
- Exactly one RFQ and one line survive; invitations, quotations, comparisons, and POs remain absent.
- A third independent context verifies the committed state.
- Direct two-connection collision proves one unique-index winner and authoritative original.
- Immutable histories remain in the database until whole owned-database disposal.

## PostgreSQL test inventory  NOT RUN

Exactly 22 methods compiled/listed: seven `Rev869BPostgresApplicationBehaviorTests` and fifteen `Rev869BPostgresBehaviorTests`. They remain **NOT RUN** under this source-only correction. No PostgreSQL acceptance is inferred.

## Offline validation

| Validation | Result |
|---|---|
| Build | PASS: 0 warnings, 0 errors |
| Focused REV869B non-PostgreSQL | PASS: 49/49 |
| Complete non-PostgreSQL suite | PASS: 423/423 |
| PostgreSQL compile/list | PASS: 22 methods; **NOT RUN** |
| PowerShell AST | PASS: 23 files, 0 parse errors; scripts not executed |
| EF discovery | PASS with `--no-connect`: 13 migrations; REV869B exactly once immediately after REV869A |
| Model/snapshot parity | PASS: 1/1 executable no-connect model-differ test |
| `git diff --check` | PASS |
| Secret marker scan | PASS: no private-key or AWS access-key marker |
| Prohibited fixture/randomness scan | PASS: no external complete-graph marker, randomized `GetHashCode()`, or `gen_random_uuid()` in the REV869B PostgreSQL tests |

## Offline SQL evidence and schema inventory

- Up SQL: 187,749 UTF-8 bytes; SHA-256 `82BC6CB3EDBF6F24413788D9121AE339C87EC7EEC0ABF5EA592B4FB045A30A6B`.
- Down SQL: 7,417 UTF-8 bytes; SHA-256 `5B10F990341FC2B60EE820F0AE12245347BEB04076D78E0E7C5B69CFB2A91788`.
- Up inventory: 15 tables; 75 trigger statements; 19 function-definition occurrences representing 18 unique runtime functions; 46 foreign keys; 68 indexes; 31 checks.
- SQL was generated in memory from REV869A to the retained REV869B migration and in reverse. It was not applied or written to a database.

## Remaining acceptance boundaries

No material source correction identified by the authoritative seventh re-review is intentionally deferred. Runtime trigger compilation/behavior, PostgreSQL test results, migration execution, and database acceptance remain unverified because PostgreSQL access was prohibited. Those are acceptance boundaries, not claims of success.

No database, helper, migration apply/remove, backup/restore, production, REV861, frontend, REV869C, AWS, or legacy-reference operation occurred. Application source outside the controlled files was not changed.

This correction does **not** self-declare source-safety PASS, execution-helper readiness, PostgreSQL acceptance, migration acceptance, production readiness, or final REV869B acceptance. A new independent source-only safety re-review is mandatory.

