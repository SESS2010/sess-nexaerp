# REV869B source correction checkpoint 7

## Gate and scope

- Starting commit: `a5856312806bbc5929624a6602602df2910eaedc`; required parent: `d688bc37d4ed672e0a322d2b00f22a459c6101e0`.
- Ending commit: the commit containing this checkpoint; exact hash is reported in the handoff because a commit cannot contain its own hash.
- Entry target status: clean. Legacy-reference was not accessed or modified.
- Retained migration `20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation` remains exactly once, immediately after REV869A.

## Finding-to-correction results

1. PostgreSQL isolation/cleanup: application fixtures now prove exact deterministic database-name nonexistence, clone the accepted isolated database, create their deterministic organization/identity/warehouse/PR/line/handoff records, disable pooling, and drop only that quoted owned database in `finally`, verifying absence. This safely contains a committed concurrency winner and immutable histories.
2. Exact database evidence: eight generic assertion call sites now use `AssertPostgresGuardAsync`, which fails on zero affected rows and asserts SQLSTATE plus constraint/trigger/function evidence. IDs are deterministic.
3. Concurrency/rollback: independent DbContexts, connections and services share a coordinated start; rollback verification uses an independent context and counts all 15 REV869B relations plus audit, numbering, identity mapping, warehouse, PR, PR line and handoff state.
4. Database SOD: dead `rev869b_write_bound_history` was removed. Installed history guards enforce creator/approver, verifier/approver, and PO submitter/resubmitter/approver separation with named 42501 constraints.
5. Technical history: deferred `trg_rev869b_bound_technical_history` requires exact same-command history.
6. Correlation: five aggregates now persist `TransitionCorrelationId`; insert binds it to idempotency fingerprint, update requires a new fingerprint, history must equal parent correlation, and same-status reservations write exact histories.
7. Qualification provenance: authoritative verifier/approver employee IDs and restrictive FKs were added; invitation event time and full qualification fields/identities reconcile through comparison and PO.
8. Pipeline/rollback gaps: mapped ASP.NET design covers success, 400/401/403/404/409 and unauthorized attachment/export; supporting rollback state is included.

## Exact PostgreSQL evidence matrix

| Case | SQLSTATE | Evidence |
|---|---|---|
| Idempotency collision | 23505 | `IX_request_for_quotations_OrganizationId_IdempotencyKey` |
| Terminal insert | P0001 | `rev869b_enforce_transition` |
| PO issue tamper | 23514 | `rev869b_po_issue_allowlist` |
| Version skip/lower | 40001 | `rev869b_exact_version_increment` |
| PO org/totals/policy tamper | 23514 | `rev869b_po_approval_allowlist` |
| Immutable JSON/tax/provenance update | P0001 | `rev869b_reject_immutable_mutation` |
| Controlled delete | 23514 | `rev869b_controlled_delete_guard` |
| Late child | P0001 | `rev869b_validate_child_insert` |
| Audit null ID | 23502 | `audit_logs_Id` |

## Changed files

1. `src/SESS.NexaERP.Domain/Masters/VendorQualification.cs`
2. `src/SESS.NexaERP.Domain/Purchase/Rev869BPurchaseTransactions.cs`
3. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.Designer.cs`
4. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.cs`
5. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/NexaErpDbContextModelSnapshot.cs`
6. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BControlledMutationSql.cs`
7. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BDatabaseSafetySql.cs`
8. `src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.Rev869A.cs`
9. `src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.Rev869B.cs`
10. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.ComparisonPo.cs`
11. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.RfqQuotation.cs`
12. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs`
13. `tests/SESS.NexaERP.Tests/Rev869BDatabaseSafetyContractTests.cs`
14. `tests/SESS.NexaERP.Tests/Rev869BPostgresApplicationBehaviorTests.cs`
15. `tests/SESS.NexaERP.Tests/Rev869BPostgresBehaviorTests.cs`
16. `tests/SESS.NexaERP.Tests/Rev869BPurchaseCorrectionTests.cs`
17. `tests/SESS.NexaERP.Tests/Rev869BPurchaseFoundationTests.cs`
18. `outputs/rev869b_source_correction_checkpoint_7.md`

## Offline evidence

- Build: 0 warnings, 0 errors.
- Focused REV869B non-PostgreSQL: 49/49 passed.
- Complete non-PostgreSQL: 423/423 passed.
- REV869B PostgreSQL: 22 methods compiled/listed; **NOT RUN**.
- PowerShell AST: 23 files, 0 errors; scripts not executed.
- EF `--no-connect`: 13 migrations; REV869B once after REV869A.
- Model/snapshot parity: passed. `git diff --check`: passed.
- Up: 175,904 bytes, SHA-256 `16A90ED2FEF1933C3D9E62E1EB6676279625597044BD1D802D0E09CCF40765BB`.
- Down: 7,161 bytes, SHA-256 `5F869A3FE6024A468E0B1D58D836CA8A756497418B6115D4374FA6F6E779A883`.
- Inventory: 15 tables; 73 trigger occurrences/73 unique; 16 function occurrences/15 unique; 46 FKs; 68 indexes; 31 checks.
- No PostgreSQL access/test execution, helper, migration apply/remove, backup/restore, production, REV861, REV869C, frontend, AWS, or legacy-reference operation occurred. Secret scan found no private/AWS key material.

## Unresolved material finding

The older direct guard class still validates and consumes a named complete `REV869B-PG-DIRECT-TEST-OWNED` graph instead of creating the entire vendor/qualification/UOM/Rack-Bin/quotation/comparison/PO graph. The disposable database boundary fixes application-test ownership and committed-history cleanup, but does not make those direct cases self-creating. This remains a material test-design blocker for the mandatory independent re-review.

PostgreSQL tests remain NOT RUN. This correction does not self-declare source-safety PASS, helper readiness, PostgreSQL or migration acceptance, production readiness, or final REV869B acceptance. A new independent source-only safety re-review is mandatory.
