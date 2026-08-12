# REV869B Fourth Controlled Source Correction Checkpoint

Date: 2026-08-12 (Asia/Calcutta)

## Identity, scope, and disposition

- Starting commit: `88730a5de6f73402ceaf4149ebe0cd9439f51b9f`.
- Ending commit: the single correction commit containing this checkpoint; its non-self-referential hash is reported in the final handoff.
- Preserved migration ID: `20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation`.
- Scope: source-only correction inside `target-dotnet`.
- PostgreSQL access/tests, migration/helper execution, migration creation/application/removal, backup/restore, production, REV861, REV869C, frontend, AWS, and legacy-reference operations: not performed.
- This checkpoint does not declare source-safety PASS, database acceptance, helper readiness, PostgreSQL behavioral acceptance, production readiness, frontend completion, or final REV869B acceptance. A new independent source-only safety re-review is mandatory.

## Exact controlled file list

1. `outputs/rev869b_source_correction_checkpoint_4.md`
2. `src/SESS.NexaERP.Domain/Purchase/Rev869BPurchaseTransactions.cs`
3. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.Designer.cs`
4. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.cs`
5. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/NexaErpDbContextModelSnapshot.cs`
6. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BDatabaseLifecycleSql.cs`
7. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BDatabaseSafetySql.cs`
8. `src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.Rev869B.cs`
9. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.ComparisonPo.cs`
10. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.RfqQuotation.cs`
11. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs`
12. `tests/SESS.NexaERP.Tests/Rev869BDatabaseSafetyContractTests.cs`
13. `tests/SESS.NexaERP.Tests/Rev869BPostgresApplicationBehaviorTests.cs`
14. `tests/SESS.NexaERP.Tests/Rev869BPostgresBehaviorTests.cs`
15. `tests/SESS.NexaERP.Tests/Rev869BPurchaseCorrectionTests.cs`
16. `tests/SESS.NexaERP.Tests/Rev869BPurchaseFoundationTests.cs`

No accepted REV868, REV868C3, or REV869A migration file changed.

## Blockers and implemented corrections

| Blocker | Controlled correction |
|---|---|
| Late child INSERT | Added fail-closed BEFORE INSERT guards for RFQ lines, invitations, quotation lines, technical verification, comparison lines, PO lines, and material-follow-up handoffs. Each resolves exactly one parent in the permitted editable/authorized state; missing, ambiguous, terminal, wrong-parent, and cross-organization ancestry fail. Existing UPDATE/DELETE immutable guards remain. |
| Quotation had no true editable boundary | Added canonical `Draft` quotation state and `Draft -> Submitted` transition. The service persists parent/children in Draft, performs an organization/version/status CAS to Submitted, detaches the stale tracked parent, then appends history and audit in the same transaction. |
| Commercial proof trusted stored totals | Added one canonical `rev869b_commercial_snapshot_reconciles(uuid,jsonb,jsonb)` function. It reads quantity/rate/discounts/charges/tax inputs from exact relational quotation/tax rows, calculates decimal gross, assessable, taxable, components, cess, round-off, and payable, enforces numeric(24,6) capacity/ranges, and compares exact relational totals and JSON input/result/tax evidence. |
| Nullable/malformed JSON | Mandatory commercial/tax/PO/approval JSON is object/type/presence checked. Typed UUID/numeric/date/timestamp/boolean parsing is exception-guarded. Exact sets use `jsonb_object_length`, key-set checks, and `IS NOT DISTINCT FROM`; the unsafe nullable `taxRule <>` pattern is absent. |
| Incomplete joins/cardinality | Comparison and PO guards join exact organization, RFQ/line, invitation, quotation/revision/line, technical verification, comparison/line, PO/line, Item, UOM, Vendor, qualification, Tax/GST, attachment, approval policy, route/history, approver timestamp provenance. Explicit expected/actual/missing/unexpected/duplicate/stale/organization/parent/commercial/tax/attachment-qualification/approval counters must reconcile to exact zero mismatches. |
| Weak future PostgreSQL source | Added a deterministic, collision-guarded `REV869B-PG-OWNED` fixture using exact seeded identities and an outer serializable rollback proof. Real `EfRev869BPurchaseService`, EF DbContext, tax/vendor services, scope authorizer, and audit writer are invoked. Removed arbitrary `ORDER BY ... LIMIT 1` business-row selection. Restored all-four aggregate late-INSERT, immutable-history, and exact trigger/function inventory sources. |

## Child INSERT immutability matrix

| Relation | Authorized INSERT parent boundary | Rejected afterward |
|---|---|---|
| `request_for_quotation_lines` | exact RFQ Draft/version 0 | Issued, Closed, Cancelled |
| `rfq_vendor_invitations` | exact RFQ Issued | Closed, Cancelled or missing parent |
| `vendor_quotation_lines` | exact current quotation Draft/version 0 | Submitted, technical result, Superseded, Withdrawn, Rejected |
| `quotation_technical_verifications` | exact quotation line under Submitted quotation | technical terminal/superseded/withdrawn states |
| `commercial_comparison_lines` | exact comparison Draft/version 0 | PendingApproval, Approved, Rejected, RevisionRequested, Cancelled |
| `purchase_order_lines` | exact PO Draft or RevisionDraft/version 0 | PendingApproval, Resubmitted, Approved, Rejected, Issued, Cancelled, Superseded |
| `material_followup_handoffs` | exact current Issued PO | all non-Issued/non-current states |
| status/approval/PO history | exact current parent transition, route, revision and organization | fabricated, stale, wrong-parent, self-approval or mismatched transition |

## Canonical database calculation formula

All arithmetic is PostgreSQL `numeric`, corresponding to application `decimal`:

1. `gross = round(quantity * unitRate, roundingScale)`.
2. `assessable = gross + packing + freight + insurance + otherCharges`.
3. Reject negative inputs/rates, invalid scale/rate, discount greater than assessable, and any intermediate absolute value above `999999999999999999.999999`.
4. `taxable = round(assessable - lineDiscount - allocatedHeaderDiscount, roundingScale)`.
5. Each tax component is `round(taxable * rate / 100, roundingScale)`.
6. `payable = round(taxable + CGST + SGST + IGST + cess + roundOff, roundingScale)`.
7. Stored quotation values and exact JSON input/result/tax objects must equal the authoritative relational calculation.

Currency is exact, exchange rate is exactly one for the accepted no-conversion workflow, rounding scale is 0..6, India GST intra/interstate component selection is exact, and tax organization/HSN/SAC/states/registration/effective range/approval/active/reverse-charge/exemption evidence is immutable.

## Strict JSON and authoritative provenance contract

- SQL NULL, JSON null, missing objects/members, wrong JSON type, malformed typed values, extra exact-set keys, stale IDs/versions, and altered values reject.
- Comparison recommendation/approval requires exact recommended quotation line coverage, one technical-compliance record per line, current quotation revision, exact RFQ/invitation/vendor/Item/UOM/tax/qualification/attachment evidence, and exact canonical commercial reconciliation.
- PO submit/approval/issue requires exact approved comparison, selected quotation and revision, recommended comparison line, RFQ line/item/UOM, quotation attachment and qualification snapshot, commercial/tax snapshots, approval route/policy, and exactly one approval history/timestamp for issue.
- Down removes correction-owned functions with CASCADE before retained table teardown; temporary permission/page ownership guards remain scoped to Down.

## Deterministic future PostgreSQL fixture and test inventory

The future application fixture:

- accepts only exact opt-in `ISOLATED_REV869B_BEHAVIOR_TESTS` and database `sess_nexaerp_rev869b_verify`;
- verifies `current_database()` and the retained migration exactly once;
- derives deterministic SHA-256 IDs, checks collisions before writes, marks owned rows `REV869B-PG-OWNED`, uses exact accepted seed identities, and never selects arbitrary business rows;
- owns an outer serializable EF transaction; normal service calls participate in an existing transaction and otherwise create/own their serializable transaction;
- rolls the fixture back and opens a clean verifier context to prove exact owned-row before/after equality.

Compiled future cases include real service success, injected audit failure rollback, real idempotent replay, protected scope denial, audit failure propagation, conflicting service writer, direct rollback/concurrency/idempotency guards, terminal state rejection, snapshot mismatch, exact +1 version rejection, all-four late child INSERT rejection, immutable-history UPDATE/DELETE rejection, and exact trigger/function inventory. These 19 PostgreSQL cases were listed/compiled and **NOT RUN**.

## Executed validation and regression evidence

| Validation | Result |
|---|---|
| PowerShell 5.1 AST parse | PASS: 23 files, 0 errors; scripts not executed |
| `dotnet build SESS.NexaERP.slnx --no-restore` | PASS: 0 warnings, 0 errors |
| Focused REV869B non-PostgreSQL | PASS: 47/47 |
| Complete non-PostgreSQL | PASS: 421/421 |
| PostgreSQL source compilation | PASS through solution build; 19 listed, **NOT RUN** |
| EF migration discovery | PASS with `--no-connect`: 13 migrations; retained REV869B exactly once after REV869A |
| Executable no-connect model/snapshot parity | PASS: 1/1 |
| Migration/designer/snapshot consistency | PASS through build, mapping constraints, and model differ |
| Accepted REV868/REV868C3/REV869A regression | PASS through complete 421-test non-PostgreSQL suite; no accepted migration changed |
| `git diff --check` | PASS |

No PostgreSQL connection, test, helper, or migration operation occurred. The installed EF pending-model CLI is not invoked because its no-connect form is unsupported; executable model differ is the no-connect parity evidence.

## Canonical offline SQL hashes and inventory

Generated REV869A -> retained REV869B and reverse using `--no-transactions`, an unreachable loopback port-1 design string, and no database connection:

- Up: **129,064 bytes**, SHA-256 `42050498E9ED4F876FAE02CC2AF95BB5680434B4FF31FDF6ABBC5E941D3CB725`.
- Down: **6,241 bytes**, SHA-256 `70E55F4635F44F2FBD7035910E1620A0F0507E760E402F83419C30E4093CC1D5`.
- Up inventory: 15 tables, 35 unique REV869B triggers, 10 unique REV869B functions, 44 foreign keys, 66 indexes, and 29 check constraints.
- Down inventory: seven explicit REV869B function drops before 15 table drops; remaining retained owned functions are removed after dependent teardown; two temporary ownership triggers and one temporary ownership function protect permission/page deletion and are removed in rollback order.
- Every Up trigger name occurs exactly once. The exact future runtime inventory test carries the complete 35-trigger and 10-function name sets.

## Remaining blockers

1. A new independent source-only safety re-review must assess this committed correction; this checkpoint is not approval.
2. All 19 PostgreSQL cases are **NOT RUN**. Live trigger/function inventory, tampering, transaction, rollback, concurrency, idempotency, protected endpoint, repeated revision, and immutable-history behavior remain unaccepted until separately authorized isolated execution.
3. No execution helper exists. Database acceptance, PostgreSQL behavioral acceptance, production readiness, frontend completion, source-safety PASS, and final REV869B acceptance are not claimed.
