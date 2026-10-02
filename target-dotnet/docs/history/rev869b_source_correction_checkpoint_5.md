# REV869B Fifth Controlled Source Correction Checkpoint

Date: 2026-08-12 (Asia/Calcutta)

## Identity and boundary

- Starting commit: b510a4963ec95258f4a3ffc1bd3610f2371ef95d.
- Ending commit: the single correction commit containing this checkpoint; reported in the final handoff.
- Retained migration ID: 20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.
- Scope: source-only correction inside target-dotnet.
- PostgreSQL tests compiled/listed but NOT RUN.
- No PostgreSQL access, helper execution, migration creation/application/removal, database creation, backup, restore, production, REV861, frontend, REV869C, AWS, or legacy-reference operation occurred.
- This checkpoint is not independent acceptance and does not claim source-safety PASS, helper readiness, database acceptance, production readiness, frontend completion, or final REV869B acceptance.

## Exact controlled file list

1. outputs/rev869b_source_correction_checkpoint_5.md
2. src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.cs
3. src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BDatabaseLifecycleSql.cs
4. src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BDatabaseSafetySql.cs
5. tests/SESS.NexaERP.Tests/Rev869BDatabaseSafetyContractTests.cs
6. tests/SESS.NexaERP.Tests/Rev869BPostgresApplicationBehaviorTests.cs
7. tests/SESS.NexaERP.Tests/Rev869BPostgresBehaviorTests.cs
8. tests/SESS.NexaERP.Tests/Rev869BPurchaseCorrectionTests.cs
9. tests/SESS.NexaERP.Tests/Rev869BPurchaseFoundationTests.cs

No accepted REV868, REV868C3, or REV869A migration file changed.

## Controlled corrections

| Independent-review blocker | Fifth correction |
|---|---|
| False-is-non-null quotation proof | Removed the duplicate quotation authoritative trigger/branch. The one retained quotation lifecycle guard requires the canonical reconciliation result IS TRUE; FALSE and NULL fail. |
| RFQ-line mutation | Added an UPDATE/DELETE immutable trigger. |
| Invitation qualification/provenance mutation | Added a guard that permits only controlled status/version/audit-field updates; qualification/provenance changes and DELETE reject. |
| Comparison-line DELETE | Added an explicit DELETE guard; existing post-boundary UPDATE protection remains. |
| Material-follow-up mutation | Added UPDATE/DELETE immutable protection. |
| Unsafe function resolution | All 11 retained REV869B functions set search_path to pg_catalog, nexa and retain static schema qualification. |
| Aliased mismatch counters | Comparison and PO now independently count missing, unexpected, duplicate, stale-version, organization, parent/provenance, commercial, tax, attachment/qualification, and approval mismatches. Commercial mismatch requires canonical TRUE. |
| Fabricated history | History guards bind status/version/route to the exact transition actor and a five-second parent-transition timestamp window. Unique correlations and immutable history UPDATE/DELETE remain. |
| Ambient rollback pseudo-proof | Injected-failure cases create prerequisites without an ambient transaction, forcing the real service to own and roll back its serializable transaction. Disposal fails closed on any leaked REV869B row and removes only deterministic prerequisites in reverse FK order. |
| Sequential pseudo-concurrency | Replaced by two independent DbContexts, physical connections, serializable transactions, and real service instances contending on one organization-scoped idempotency key. Both test transactions roll back. |
| Missing mapped success | Added an authenticated in-process ASP.NET test using the real mapped endpoints, authorization, permission and employee/scope filters, real EF service, history, and audit under a rolled-back owned fixture. |
| Denial audit ambiguity | The real service denial case separately proves zero RFQ/line mutation and exactly one awaited Denied/Failure audit row. |
| Zero-row false positives | Direct test entry requires exact, non-ambiguous test-owned relation counts. Mutations use exact IDs/row counts, and each expected failure gets a fresh transaction. |
| Incomplete terminal/child coverage | Runtime source attempts all four aggregate terminal INSERTs and all seven late-child INSERTs. |
| Incomplete tampering | Added JSON result, tax, totals, version, organization, approval policy, quotation provenance, and DELETE tampering with authoritative post-attempt checks. |
| Missing repeat revision | Added rolled-back rejected predecessor to RevisionDraft to Resubmitted to Rejected to second RevisionDraft to Resubmitted coverage with exact ancestry and line counts. |
| Runtime contracts | Immutable-history UPDATE/DELETE and exact trigger/function inventory tests remain and now fail closed on absent fixture rows. |

## Fifteen-table mutation coverage

The five aggregates retain controlled INSERT and exact +1 UPDATE transition guards. The seven child/snapshot relations retain editable-boundary INSERT guards and appropriate UPDATE/DELETE protection. The three history/policy relations retain authorized INSERT/overlap guards and immutable history UPDATE/DELETE protection.

The four gaps named by the review now have explicit guards:

- request_for_quotation_lines: immutable UPDATE/DELETE.
- rfq_vendor_invitations: immutable qualification/provenance and DELETE; controlled lifecycle fields only.
- commercial_comparison_lines: DELETE rejected and post-boundary UPDATE rejected.
- material_followup_handoffs: immutable UPDATE/DELETE.

Down removes correction-owned functions before retained table teardown and names no accepted REV868, REV868C3, or REV869A object.

## PostgreSQL test inventory — NOT RUN

Exactly 22 methods in the two REV869B PostgreSQL behavior classes compiled and were listed:

- 7 application/pipeline cases: real service commit, service-owned rollback, replay, denial/audit, audit failure, two-context contention, and authenticated mapped endpoint success.
- 15 direct cases: persistence/rollback, independent-connection CAS, replay/collision, four terminal inserts, issue mismatch, tampering matrix, permission/audit contracts, exact +1, seven late-child inserts, immutable histories, repeated rejected-PO revision, and exact inventory.

Every entry requires opt-in ISOLATED_REV869B_BEHAVIOR_TESTS, exact database sess_nexaerp_rev869b_verify, no fallback, current_database verification, and the retained migration exactly once. Direct fixtures require exact non-ambiguous test-owned counts. All 22 remained NOT RUN.

## Permitted offline validation

| Validation | Result |
|---|---|
| PowerShell 5.1 AST | PASS: 23 files, 0 errors; scripts not executed |
| Release build without restore | PASS: 0 warnings, 0 errors |
| Focused REV869B non-PostgreSQL | PASS: 48/48 |
| Complete non-PostgreSQL | PASS: 422/422 |
| PostgreSQL compile/list | PASS: 22 REV869B database-backed methods; NOT RUN |
| Total discovered | 473 |
| EF discovery | PASS with no-connect: 13 migrations; retained REV869B once after REV869A |
| Executable no-connect model/snapshot parity | PASS: 1/1 |
| Accepted regressions | PASS through 422 tests; no accepted migration changed |
| Git diff check | PASS |
| Secret/privacy/prohibited-operation scans | PASS; no secret-like addition and no prohibited operation executed |

## Canonical offline SQL

Generated REV869A to retained REV869B and reverse with no-transactions, no-build, and unreachable loopback port 1. No PostgreSQL connection opened. Unique temporary files were deleted after hashing.

- Up: 137,006 bytes.
- Up SHA-256: 2B9CEDA0618F88122E54D893D53DD1592041490A111BB8A4DD8E9CDE3A232A33.
- Down: 6,317 bytes.
- Down SHA-256: 19712B3C4843797AF55927AD1DA720E11310A00A64A85639C193CBA0020A6591.
- Up inventory: 15 tables, 38 trigger occurrences/38 unique triggers, 11 function occurrences/11 unique functions, 44 foreign keys, 66 indexes, and 29 checks.
- Down inventory: 15 table drops plus correction/retained owned teardown and scoped Down ownership guards.

## Mandatory next gate

A new independent source-only REV869B safety re-review must treat this checkpoint as an unverified claim, inspect all changed bodies, independently regenerate tests/hashes/inventory, and keep PostgreSQL tests NOT RUN. This checkpoint does not authorize isolated database provisioning or execution-helper design.
