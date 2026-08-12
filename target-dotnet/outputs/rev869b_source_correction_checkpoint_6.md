# REV869B Sixth Controlled Source Correction Checkpoint

Date: 2026-08-12 (Asia/Calcutta)

## Identity and boundary

- Starting commit: `c494ba2e63b23696f6ee92433015bd4e398da434` (parent `8e929f48c6abebda510205defb7bf2c3214cae18`).
- Ending commit: the single correction commit containing this checkpoint; reported in the final handoff.
- Retained migration ID: `20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation`.
- Source-only work inside `target-dotnet`; PostgreSQL tests compiled/listed but **NOT RUN**.
- No PostgreSQL access, helper execution, migration creation/application/removal, database creation, backup, restore, production, REV861, frontend, REV869C, AWS, or legacy-reference operation occurred.
- This checkpoint does not self-declare source-safety PASS, helper readiness, PostgreSQL acceptance, migration acceptance, production readiness, or final REV869B acceptance.

## Exact controlled file list

1. `outputs/rev869b_source_correction_checkpoint_6.md`
2. `src/SESS.NexaERP.Api/Endpoints/Rev869BPurchaseEndpoints.cs`
3. `src/SESS.NexaERP.Application/Purchase/Rev869BPurchaseContracts.cs`
4. `src/SESS.NexaERP.Domain/Purchase/Rev869BPurchaseTransactions.cs`
5. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.Designer.cs`
6. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.cs`
7. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/NexaErpDbContextModelSnapshot.cs`
8. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BControlledMutationSql.cs`
9. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BDatabaseSafetySql.cs`
10. `src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.Rev869B.cs`
11. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.ComparisonPo.cs`
12. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.MaterialFollowUp.cs`
13. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.RfqQuotation.cs`
14. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs`
15. `tests/SESS.NexaERP.Tests/Rev869BDatabaseSafetyContractTests.cs`
16. `tests/SESS.NexaERP.Tests/Rev869BPostgresApplicationBehaviorTests.cs`
17. `tests/SESS.NexaERP.Tests/Rev869BPostgresBehaviorTests.cs`
18. `tests/SESS.NexaERP.Tests/Rev869BPurchaseCorrectionTests.cs`

No accepted REV868, REV868C3, or REV869A migration file changed.

## Finding-by-finding correction mapping

| Review finding | Sixth correction |
|---|---|
| Incomplete mutation coverage | Added explicit version-zero INSERT, exact +1 UPDATE, per-aggregate field allowlists, controlled child/snapshot behavior, and direct DELETE rejection across all 15 relations. |
| Material Follow-up had no lifecycle | Added `PendingFollowUp -> InProgress -> Completed` domain, EF, service, endpoint, exact CAS/version, actor/reason/idempotency, audit, and status-history behavior. |
| Fabricable five-second histories | Removed the time-proximity heuristic. History INSERT now requires the parent row's `xmin` to be the current transaction, exact parent/status/version/organization/document, active employee/login/role, nonblank correlation and remarks, and server timestamp. Deferred constraint triggers require exact from/to/action/actor/version status history and specialized comparison/PO history before commit. |
| Same-status/protected-field mutation | Same-status changes are restricted to version/audit metadata; transition-specific allowlists protect ownership, organization, parents, provenance, commercial/tax snapshots, version flags, and issue/cancellation fields. |
| Comparison-line workflow | Draft/RevisionRequested corrections use exact +1 per-line versions and a limited recommendation/commercial recalculation allowlist; destructive DELETE remains prohibited. |
| Approval-policy lifecycle | Version zero and exact +1, effective-date validation, overlap guard, authorized organization identity/role, server timestamps, no DELETE, and controlled-configuration history are enforced. |
| Nullable Boolean/tax path | Replaced the retained nullable `taxRule <>` predicate with explicit missing/null rejection plus `IS DISTINCT FROM`; canonical commercial reconciliation still requires exact TRUE. |
| Qualification provenance | Invitation snapshots contain exact qualification IDs, vendor/organization/category/type/version/effective dates, approval/active state, approving identity and event timestamp; comparison/PO guards match retained elements to authoritative rows. |
| PO history gap | Added the missing status-history row for superseded predecessors and retained exact PO-history evidence for create, revision, submit/resubmit, approval/rejection, issue, cancellation and supersession paths. |
| Rollback/concurrency source design | Added fresh independent-context rollback counting across business/history/audit/number-series relations; replaced timing-only delay with coordinated two-context/two-connection/two-service start plus replay and conflicting-payload assertions. |
| Endpoint coverage | Added the Material Follow-up mapped endpoint and retained authenticated mapped ASP.NET pipeline success coverage and status mapping contracts. |

## Fifteen-table mutation-control matrix

| Relation | INSERT | UPDATE | DELETE |
|---|---|---|---|
| `purchase_transaction_approval_policies` | version 0, valid dates/actor, non-overlap | exact +1 controlled activate/deactivate, server time/history | rejected |
| `purchase_transaction_status_history` | same-transaction parent/actor/action evidence only | immutable | rejected |
| `request_for_quotations` | Draft, version 0 | exact transition/+1; lifecycle-only allowlist | rejected |
| `request_for_quotation_lines` | exact Draft/version-0 parent, version 0 | immutable | rejected |
| `rfq_vendor_invitations` | Issued, version 0, exact parent/qualification | exact transition/+1; qualification/provenance immutable | rejected |
| `vendor_quotations` | controlled initial state, version 0 | exact transition/+1; commercial/provenance immutable | rejected |
| `vendor_quotation_lines` | exact editable parent, version 0 | immutable | rejected |
| `quotation_technical_verifications` | exact Submitted parent, version 0 | immutable | rejected |
| `commercial_comparisons` | Draft, version 0 | exact +1; recommendation allowlist only at editable boundary | rejected |
| `commercial_comparison_lines` | exact Draft/version-0 parent | exact +1 recommendation/recalculation only while Draft/RevisionRequested | rejected |
| `purchase_transaction_approval_history` | same-transaction comparison transition only | immutable | rejected |
| `purchase_orders` | Draft/RevisionDraft, version 0, exact ancestry | exact +1 state-specific allowlists | rejected |
| `purchase_order_lines` | exact Draft/RevisionDraft parent, version 0 | immutable | rejected |
| `purchase_order_history` | same-transaction PO transition only | immutable | rejected |
| `material_followup_handoffs` | PendingFollowUp/version 0 under current Issued PO | exact +1 PendingFollowUp/InProgress/Completed lifecycle | rejected |

## Allowed state-transition matrix

- RFQ: Draft to Issued/Cancelled; Issued to Closed/Cancelled.
- Invitation: Issued to Submitted/Withdrawn/Cancelled.
- Quotation: Draft to Submitted; Submitted to TechnicallyCompliant/TechnicallyRejected/Superseded/Withdrawn; compliant to Superseded/Withdrawn; rejected technical result to Superseded/Withdrawn/Rejected.
- Comparison: Draft to PendingApproval/Cancelled; PendingApproval to Approved/Rejected/RevisionRequested; RevisionRequested to PendingApproval/Cancelled.
- PO: Draft to PendingApproval/Cancelled; PendingApproval to Approved/Rejected/Cancelled; Rejected to RevisionDraft; RevisionDraft to Resubmitted/Cancelled; Resubmitted to Approved/Rejected/Cancelled; Approved to Issued/Cancelled; Issued to Superseded/Cancelled.
- Material Follow-up: PendingFollowUp to InProgress; InProgress to Completed.

## Binding and reconciliation contracts

- History: parent `xmin` must belong to the current transaction; parent/status/version/organization/document, employee/login/role, route/action, correlation and remarks are checked. Server time/version replace caller values. Deferred triggers reject a parent transition without its exact histories.
- Qualification: exact authoritative qualification ID/version/vendor/organization/category/type/effective range/approval/active/approved-by data is captured at the invitation event and retained immutably.
- Calculation: missing/JSON-null `taxRule`, malformed JSON, non-TRUE canonical reconciliation, and mismatched quantity/rates/discounts/charges/CGST/SGST/IGST/cess/rounding/payable/currency/precision/quotation version/comparison version/organization/parent provenance fail closed.

## Test inventory and offline validation

- 22 future REV869B PostgreSQL methods compiled/listed: 7 application/pipeline and 15 direct database cases. They were **NOT RUN**.
- Build: 0 warnings, 0 errors.
- Focused REV869B non-PostgreSQL: 48/48 passed.
- Complete non-PostgreSQL suite: 422/422 passed.
- PowerShell 5.1 AST: 23 files, 0 parse errors; scripts were not executed.
- EF no-connect discovery: 13 migrations; retained REV869B exactly once after REV869A.
- Executable no-connect model/snapshot parity: passed within the focused suite.
- `git diff --check`: passed.

## Canonical offline SQL

Generated from REV869A to the retained REV869B migration and in reverse, with no transactions, no build, an unreachable loopback port-1 design string, and no PostgreSQL connection.

- Up: 174,802 bytes; SHA-256 `9ED9E9386CA55A4D0823C10DB0F21343B33AF07BD32A0504F98ADF32225DC3CA`.
- Down: 6,672 bytes; SHA-256 `EA2D5BA6F173E71DA2C25067FB21F1ECC75F66A3FDEF73CD7EE6377FA17689C4`.
- Up inventory: 15 tables, 72 trigger creation occurrences/72 unique created names (71 final installed after the controlled obsolete follow-up trigger drop), 17 function creation occurrences/16 unique created names (15 final installed after the superseded automatic-history function drop), 44 foreign keys, 66 indexes, and 29 checks.
- Temporary SQL evidence files were removed after hashing.

## Remaining blockers and exclusions

- PostgreSQL behavior, trigger execution order, deferred constraint behavior, SQLSTATE/constraint identities, and runtime cleanup were not executed and are not accepted.
- The direct PostgreSQL suite still expects a separately present `REV869B-PG-DIRECT-TEST-OWNED` graph rather than constructing every vendor/UOM/quotation/comparison/PO prerequisite itself. Several direct negative cases still use generic `PostgresException` assertions. These are material source-test-design blockers for the mandatory independent re-review.
- The committed-winner concurrency design cannot delete immutable business/history rows for cleanup; it therefore still needs an independently reviewed disposable-database or rollback-safe acceptance strategy before execution.
- No source-safety PASS or execution-helper readiness is self-declared.

## Mandatory next gate

A new independent source-only REV869B safety re-review is mandatory. It must treat this checkpoint as an unverified claim, independently inspect the SQL and service paths, regenerate hashes/inventory, keep PostgreSQL tests NOT RUN, and decide whether the remaining future-test blockers require another controlled correction.
