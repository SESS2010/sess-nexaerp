# REV869B Third Controlled Source Correction Checkpoint

Date: 2026-08-11 (Asia/Calcutta)

## Identity, scope, and disposition

- Starting commit: 360a59c4d2ac89f125ce9a1622ea67f16fbc84e5.
- Ending commit: the single correction commit containing this checkpoint; its non-self-referential Git hash is reported in the final handoff.
- Preserved migration ID: 20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.
- Scope: source-only correction inside target-dotnet.
- PostgreSQL access, migration application/removal/creation, execution-helper creation, backup/restore, production, REV861, REV869C, frontend, AWS, and legacy-reference operations: not performed.
- This checkpoint does not self-approve REV869B. A new independent source-safety re-review is mandatory.

## Exact controlled files

1. outputs/rev869b_source_correction_checkpoint_3.md
2. src/SESS.NexaERP.Application/Purchase/Rev869BPurchaseContracts.cs
3. src/SESS.NexaERP.Domain/Authorization/RolePagePermission.cs
4. src/SESS.NexaERP.Domain/Purchase/Rev869BPurchaseTransactions.cs
5. src/SESS.NexaERP.Infrastructure/Authorization/EfPagePermissionService.cs
6. src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.Designer.cs
7. src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.cs
8. src/SESS.NexaERP.Infrastructure/Persistence/Migrations/NexaErpDbContextModelSnapshot.cs
9. src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.Rev869B.cs
10. src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.cs
11. src/SESS.NexaERP.Infrastructure/Persistence/Rev869BSeedData.cs
12. src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.ComparisonPo.cs
13. src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.RfqQuotation.cs
14. src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs
15. tests/SESS.NexaERP.Tests/Rev869BPostgresBehaviorTests.cs
16. tests/SESS.NexaERP.Tests/Rev869BPurchaseBehaviorTests.cs
17. tests/SESS.NexaERP.Tests/Rev869BPurchaseCorrectionTests.cs
18. tests/SESS.NexaERP.Tests/Rev869BPurchaseFoundationTests.cs

No accepted REV868, REV868C3, or REV869A migration file changed.

## Independent reviewer blockers and corrections

| Finding | Controlled correction |
|---|---|
| N-03 commercial/GST provenance and header discount | Added distinct quotation-header, quotation-line allocation, PO-header, input, breakdown, aggregate, mapping, migration, designer, and snapshot fields. Quotation allocation is deterministic in RFQ-line order and cannot exceed a line's undiscounted capacity. One calculator/reconciler is reused by quotation, comparison, PO construction, pre-submit, pre-approval, and pre-issue checks. Comparison snapshots now include organization, comparison, RFQ, vendor, quotation/revision/line, item, quantity, UOM, currency/exchange, typed input/result, and typed tax rule. PO snapshots retain qualification, attachment, approval, commercial, tax, currency, and provenance evidence. Database comparison and issue guards positively reconcile required JSON fields and stored rows. |
| N-06 exact direct-SQL transitions | RFQ, invitation, quotation, comparison, and PO updates must set NEW.Version = OLD.Version + 1; unchanged, lower, skipped, and arbitrary higher versions are rejected. Parent/organization fields remain immutable. Terminal inserts remain rejected. The invitation now has its own insert/update transition trigger. |
| N-07 / R-12 executable behavior | Replaced no-op PostgreSQL probes with exact-database, explicit-opt-in, material transaction, rollback, two-connection CAS, concurrent idempotency, terminal-insert, snapshot, permission-audit, and audit-failure tests. These sources compile but were not run because database access is prohibited in this task. Added executable non-PostgreSQL decimal, reconciliation, malformed-policy, HTTP 400, and permission cases. |
| R-02 canonical idempotency | Canonical object properties remain ordinally sorted and strings trimmed; array members are now independently canonicalized and ordinally sorted, so logically identical unordered line sets produce the same fingerprint. Organization, operation, key, payload, and expected versions remain bound. |
| I-02 numeric safety | All quantity, money, charge, discount, round-off, exchange-rate, percentage, and tax-rate inputs are checked for scale no greater than six before calculation. Values beyond numeric(24,6), checked additions, multiplications, tax components, and payable totals fail closed. Seven-decimal positive and negative vectors map to HTTP 400. |
| M-01 MD/full-control reachability and issue persistence | Added persisted CanIssue with a false database default and explicit Purchase Manager PO seed grant. For the six REV869B transaction pages, HasFullControl no longer implies actions; each action must have its stored flag. MD retains explicit view/audit/commercial/export and value-route approval but not create/update/submit/resubmit/issue. Existing historical raw inserts remain valid through the false default. |

## Authoritative commercial formula and evidence contract

All arithmetic is decimal.

1. gross = Round(quantity * unitRate, roundingScale, AwayFromZero).
2. assessable = gross + packingForwarding + freight + insurance + otherCharges.
3. taxable = Round(assessable - lineDiscount - allocatedHeaderDiscount, roundingScale, AwayFromZero).
4. Each GST component = Round(taxable * componentRate / 100, roundingScale, AwayFromZero).
5. payable = Round(taxable + CGST + SGST + IGST + cess + roundOff, roundingScale, AwayFromZero).
6. Header values are exact checked sums of reconciled line results: taxable, line discount, header discount, all tax components, packing, freight, insurance, other charges, round-off, and payable.

Input scale/capacity validation occurs before multiplication or rounding. Rounding occurs only after validated multiplication/tax calculation and uses MidpointRounding.AwayFromZero. Currency is snapshotted as the RFQ/quotation/PO ISO code; exchange rate is explicitly 1.000000 because currency conversion is not configured. HSN/SAC, GST identity/status/effective range/supply split, quotation receipt date, vendor qualification, attachment key/hash, comparison approval route/time, and approval-policy organization/route/value/effective date are immutable evidence. Snapshot, stored line/header, audit/export source, and authoritative calculation must compare exactly.

## Exact version transition contract

- Each mutable aggregate command receives an expected uint version.
- The service predicate includes organization scope, record ID, and Version == expected.
- The atomic update writes exactly expected + 1.
- Anything other than one affected row throws DbUpdateConcurrencyException; the API returns HTTP 409 and awaits denial audit.
- The database trigger independently requires exactly OLD.Version + 1 for RFQ, invitation, quotation, comparison, and PO UPDATE statements.
- Version-only parent reservations use the same exact predicate/increment and run inside the command transaction.
- Status/history rows retain action, from/to status, actor, role, reason, correlation fingerprint, and immutable predecessor/version ancestry.

## Role × page × action matrix

Legend: V=view, C=create, U=update, S=submit, I=issue, Ver=verify, A=approve, R=reject, Clar=clarification, Rev=revision request, ReS=resubmit, Can=cancel, P=print, D=download, E=export, Up=upload, CV=commercial values, AH=audit history, FC=stored full-control marker. Actions not listed are denied. All REV869B transaction actions use explicit flags even when FC is stored.

| Role | Page | Explicit actions |
|---|---|---|
| Purchase Manager | RFQ | V,C,U,S,Ver,Clar,Can,P,D,E,Up,CV,AH |
| Purchase Manager | Vendor quotations | V,C,U,S,Ver,Can,P,D,E,Up,CV,AH |
| Purchase Manager | Technical verification | V,Clar,P,D,E,CV,AH |
| Purchase Manager | Commercial comparisons | V,C,U,S,Ver,A,R,Clar,Rev,ReS,P,D,E,Up,CV,AH |
| Purchase Manager | PO | V,C,U,S,I,ReS,Can,P,D,E,Up,CV,AH |
| Purchase Manager | Material follow-up | V,P,D,E,CV,AH |
| Purchase Executive | RFQ | V,C,U,S,Clar,P,D,Up,CV |
| Purchase Executive | Vendor quotations | V,C,U,S,P,D,Up,CV |
| Technical Engineer | RFQ | V,Clar,P,D |
| Technical Engineer | Vendor quotations | V,P,D |
| Technical Engineer | Technical verification | V,C,U,S,Ver,Clar,P,D,Up |
| Technical Director | every six-page row | V,P,D,E,CV,AH; plus Ver on technical/comparison, A/R/Rev on comparison/PO, Can on RFQ/quotation/PO, Clar on RFQ/technical/comparison |
| Managing Director | every six-page row | Same explicit flags as Technical Director plus FC marker; FC adds no REV869B transaction action |
| Stores Manager | PO; material follow-up | V,P,D |
| Stores Executive | PO; material follow-up | V,P,D |
| Accounts Head | Commercial comparisons; PO | V,P,D,E,CV,AH |
| Department Manager (resolved existing role) | RFQ; commercial comparisons | V,Clar,P,D,AH |
| Department Manager (resolved existing role) | PO | V,A,R,Rev,P,D,AH |

Fixed deterministic seed inventory is 29 rows; three Department Manager rows are inserted only after resolving the single existing active role. Only Purchase Manager/PO has CanIssue=true. Creator-login self-approval is rejected. Manager-route approval requires one effective department mapping; TD and MD routes require the exact configured role.

## Validation and test inventory

Executed offline:

- PowerShell 5.1 AST parse: 23 scripts, 0 errors; scripts were not executed.
- dotnet build SESS.NexaERP.slnx --no-restore: 0 warnings, 0 errors.
- Focused REV869B non-PostgreSQL: 43 passed, 0 failed, 0 skipped.
- Complete non-PostgreSQL: 417 passed, 0 failed, 0 skipped.
- EF migration discovery with supported --no-connect: 13 migrations; REV869B exactly once after REV869A.
- EF CLI pending-model comparison with an unreachable loopback port-1 design string: no model changes. The installed command does not accept --no-connect; the focused executable model-differ test independently passed without opening a connection.
- Migration/designer/snapshot parity: executable model-differ test passed.
- git diff --check: clean.
- Diff-only secret-like additions: 0.
- Diff-only prohibited-scope additions: 0.
- Protected database literals in changed executable source: 0.

PostgreSQL tests compiled but **NOT RUN**:

1. SuccessfulTransactionPersistsAndCanBeVerified
2. FailedTransactionRollsBackWithBeforeAfterEquality
3. TwoIndependentConnectionsHaveExactlyOneWinnerAndRejectStaleWriter
4. IdempotentReplayReturnsOriginalRowWithoutDuplicate
5. ConcurrentIdempotencyCollisionHasOneWinnerAndReturnsOriginal
6. DirectTerminalStateInsertIsRejected
7. SnapshotMismatchIsRejectedOnIssue
8. PermissionDenialPersistsAuditEvidence
9. AuditFailureCausesProtectedOperationToFailAndRollback
10. SkippedAndLowerVersionsAreRejected

Every PostgreSQL test requires REV869B_POSTGRES_OPT_IN=ISOLATED_REV869B_BEHAVIOR_TESTS, requires REV869B_POSTGRES, accepts only database sess_nexaerp_rev869b_verify, verifies current_database() after opening, and requires the REV869B migration history row exactly once. Missing or mismatched configuration throws; no fallback exists. Exact-name equality rejects base, REV868, REV869A, postgres/template, REV861-like, and production-like databases.

## Offline generated SQL

- Up: 92,431 bytes; SHA-256 BDF097EC935941348F796F4B7E9B6FB147D3055220ECBA99FFAE32EFF1D6E5AC; 15 tables, 22 REV869B triggers, five functions.
- Down: 5,848 bytes; SHA-256 1E71AC4E4B42DC7DD48DB718B0CD188BD1DBE8D1B003B22D0A49654E189E4647; 15 table drops, two temporary ownership triggers, one temporary ownership function.
- Generated SQL had zero malformed escaped-identifier hits. Temporary SQL files were removed after hashing.

## Preservation evidence

- Complete non-PostgreSQL regression execution covers accepted REV868 PR workflow, stock checking/reservations, PendingRFQ handoff, REV868C3 employee/department/manager mapping, and REV869A identity/UOM/tax/vendor/warehouse/Rack-Bin/QC/scope contracts.
- The one detected REV868C3 regression was corrected by modeling CanIssue with the same false database default used by the migration; historical raw inserts remain valid without rewriting an accepted migration.
- Migration ordering remains REV868 → REV868C3 → REV869A → the retained REV869B ID.
- No prior accepted permission row or history was removed. New issue authority is additive and explicit.
- No application outside the controlled REV869B/shared-authorization files was changed.

## Remaining blockers

1. A new independent source-safety re-review must assess the committed diff; this checkpoint is not approval.
2. All ten PostgreSQL tests are NOT RUN. Live Up/Down, trigger, rollback, concurrency, idempotency, audit-transaction, and persisted service behavior remain unaccepted until a separately authorized isolated database gate.
3. The REV869B execution helper has not been created.
4. Database acceptance, source-safety PASS, helper readiness, production readiness, and final REV869B acceptance are not claimed.
