# REV869B Fourth Independent Source-Safety Re-review After Correction 4

Date: 2026-08-12 (Asia/Calcutta)

## Review identity and boundary

- Correction commit: `6759c1b059d809ff8f31bb3ef86235d9905e8429`.
- Parent commit: `88730a5de6f73402ceaf4149ebe0cd9439f51b9f`.
- Exact reviewed range: `88730a5de6f73402ceaf4149ebe0cd9439f51b9f..6759c1b059d809ff8f31bb3ef86235d9905e8429`.
- Method: new independent diff, migration/guard, application transaction, test-body, test-inventory, no-connect EF, generated SQL, build, and non-PostgreSQL regression review. The correction checkpoint was treated as an unverified claim.
- PostgreSQL tests were compiled and **NOT RUN**. No PostgreSQL connection, database helper, migration application/removal, database creation, backup, restore, execution helper, production, REV861, frontend, REV869C, AWS, or legacy-reference operation was performed.

The source-safety gate remains closed. The correction adds substantial relational commercial proof and late-child INSERT guards, but critical immutability, quotation reconciliation, ambient-transaction rollback, deterministic fixture, and genuine application/endpoint behavior gaps remain.

## Entry verification and exact file scope

| Check | Result | Independent evidence |
|---|---|---|
| HEAD | PASS | `git rev-parse HEAD` returned the exact correction commit. |
| Parent | PASS | `git rev-parse HEAD^` returned the exact parent commit. |
| Declared correction scope | PASS | Exactly 16 paths, matching the checkpoint list; 1,146 insertions and 42 deletions. |
| Target status before report | PASS | `git status --short -- .` returned no entries. |
| Whitespace | PASS | `git diff --check` returned no findings. |
| Legacy isolation | PASS | The reviewed range has zero `../legacy-reference/` paths; that directory was not accessed or changed. |
| Accepted migrations | PASS | No REV868, REV868C3, or REV869A migration path changed. |
| Unrelated scope | PASS | The diff is limited to the checkpoint, retained REV869B domain/migration/model/service files, and REV869B tests. |

The 16 paths are the checkpoint; one purchase domain file; the retained migration, designer, snapshot, two new migration SQL fragments, and context mapping; three service files; and five REV869B test files exactly as declared by the checkpoint.

## Material blocker disposition

| Review area | State | Independent conclusion |
|---|---|---|
| A. Child INSERT and immutable relations | **FAIL** | Late INSERT guards exist for seven child relations, but RFQ lines have no UPDATE/DELETE guard, invitations can alter non-parent fields while incrementing version and have no DELETE guard, comparison lines have no DELETE guard, and material-follow-up has no DELETE guard. History INSERT guards prove only that the parent is already in the stated status; they do not bind insertion to the transition command, actor, or transition timestamp. |
| B. Canonical database calculation | **FAIL** | The canonical function correctly recomputes the formula for comparison/PO consumers. However, the safety trigger's quotation-submit branch calls it with deliberately incomplete JSON and tests `IS NOT NULL`; both `TRUE` and `FALSE` are non-null, so every line is counted as reconciled. The later lifecycle trigger is correct, but both BEFORE UPDATE triggers execute and the defective safety contract remains installed. Source safety cannot treat a false predicate as proof. |
| C. Strict JSON validation | **FAIL** | Exact equality and exception guards are materially improved. Remaining base migration expressions use nullable `<>`, and authoritative history evidence can be fabricated after a transition. The quotation safety branch does not require a true JSON reconciliation result. |
| D. Authoritative joins/cardinality | **FAIL** | Comparison and PO guards add exact relational joins and count checks, but named mismatch counters are all assigned from one aggregate `unexpected_count`, rather than independently measuring their named classes. Quotation submission remains bypassed by the false-is-non-null defect. History/approver provenance is not transition-bound. |
| E. Tax/GST proof | **FAIL** | The canonical function checks the linked tax row, effective date, organization, HSN/SAC, states, registration, supply split, rates, exemption/reverse charge, currency, rounding, approval, active state, and exact snapshot equality. This proof is not required to return true by the safety quotation guard, so it is not universal at the earliest immutable boundary. |
| F. Trigger/function architecture | **FAIL** | Counts and names reproduce exactly, and no unsafe dynamic SQL or session-setting bypass was found. Coverage is nevertheless incomplete, two separate quotation BEFORE UPDATE guards duplicate submission calculation, functions do not fix `search_path`, and trigger count does not cure the semantic defects above. |
| G. PostgreSQL test safety | **FAIL** | Opt-in, exact database, post-open `current_database()`, no fallback, serialization, deterministic IDs, and collision checks are present. The six application tests use an ambient outer transaction whose service scope deliberately no-ops rollback; failure assertions run before fixture disposal. Direct tests require `REV869B-PG-OWNED-DATABASE-GUARDS` rows that the new fixture does not create. Cleanup/repeatability is therefore not proven. |
| H. PostgreSQL test genuineness | **FAIL** | Real service creation/replay source exists, but the claimed two-service test is sequential on the same fixture/DbContext. There is no successful mapped ASP.NET endpoint, no rejected-PO revision/resubmission lifecycle, no full terminal aggregate insertion matrix, and no complete snapshot/tax tampering matrix. Several direct tests still manually compose SQL or audit rows. |
| I. Previously passed controls | **PASS within offline boundary** | Exact +1 version source, organization/version CAS, HTTP 409 mapping, decimal scale/capacity, permission segregation, `CanIssue`, self-approval and issuer separation, awaited audit writes, and accepted REV868/REV868C3/REV869A source remain present. Build and all 421 permitted tests pass. Live database behavior is unaccepted. |
| J. Test-count reconciliation | **PASS for accounting; FAIL for sufficiency** | All count changes reconcile exactly, but increased source-test count does not close the behavioral blockers. |

## Child immutability matrix

| Relation | Allowed INSERT state | Rejected INSERT states | INSERT trigger | UPDATE trigger | DELETE trigger | Owning function / parent checks | Down ownership |
|---|---|---|---|---|---|---|---|
| `request_for_quotation_lines` | Exact RFQ Draft, version 0 | Issued, Closed, Cancelled, missing/ambiguous parent | `trg_rev869b_rfq_line_insert_guard` | **None: FAIL** | **None: FAIL** | `rev869b_guard_child_insert`; parent ID/status/version and nonblank parent organization, but no child organization column | Owned function removal cascades owned trigger |
| `rfq_vendor_invitations` | Exact RFQ Issued | Draft, Closed, Cancelled, missing/ambiguous parent | `trg_rev869b_invitation_insert_guard` | Transition trigger checks version and immutable parent/vendor, but permits other-field mutation | **None: FAIL** | Child guard plus `rev869b_enforce_transition`; qualification JSON is not immutable | Owned child function removal cascades INSERT trigger; retained transition function falls with table teardown |
| `vendor_quotation_lines` | Exact current quotation Draft, version 0 | Submitted and every technical/terminal/superseded state | `trg_rev869b_quotation_line_insert_guard` | Immutable rejection | Immutable rejection | Child guard plus exact quotation/RFQ/invitation/line parent contract | Owned triggers/functions removed by named function drops/table teardown |
| `quotation_technical_verifications` | Exact line under Submitted quotation | Draft, technical/terminal/superseded/withdrawn states | `trg_rev869b_technical_insert_guard` | Immutable rejection | Immutable rejection | Child guard plus quotation-line ancestry | Owned triggers/functions removed |
| `commercial_comparison_lines` | Exact comparison Draft, version 0 | PendingApproval, Approved, Rejected, RevisionRequested, Cancelled | `trg_rev869b_comparison_line_insert_guard` | Snapshot rejection only after parent leaves Draft/RevisionRequested | **None: FAIL** | Child guard plus comparison/quotation/invitation/vendor/organization parent contract | Owned INSERT function removed; retained snapshot trigger falls with table |
| `purchase_order_lines` | Exact PO Draft or RevisionDraft, version 0 | PendingApproval, Resubmitted, Approved, Rejected, Issued, Cancelled, Superseded | `trg_rev869b_po_line_insert_guard` | Immutable rejection | Immutable rejection | Child guard plus PO/comparison/quotation/RFQ/item/handoff ancestry | Owned triggers/functions removed |
| `material_followup_handoffs` | Exact current Issued PO | Every non-Issued or non-current PO | `trg_rev869b_followup_insert_guard` | Parent-contract guard only; operational mutation remains possible | **None: FAIL** | Child guard checks PO; parent guard checks PO-line linkage | Owned INSERT function removed; retained parent trigger falls with table |
| `purchase_transaction_status_history` | Parent already has matching organization/entity/status | Unsupported entity or parent mismatch | `trg_rev869b_status_history_insert_guard` | Immutable rejection | Immutable rejection | `rev869b_guard_history_insert`; **not bound to the transition actor/time: FAIL** | Owned functions/triggers removed |
| `purchase_transaction_approval_history` | Comparison already has matching status/route/action family | Parent/status/route/action mismatch | `trg_rev869b_comparison_history_insert_guard` | Immutable rejection | Immutable rejection | History guard; **not transition-bound and does not prove approver identity: FAIL** | Owned functions/triggers removed |
| `purchase_order_history` | PO already has matching status/revision; approval actor differs from creator | Parent/status/revision mismatch or creator approval | `trg_rev869b_po_history_insert_guard` | Immutable rejection | Immutable rejection | History guard; **not transition-bound: FAIL** | Owned functions/triggers removed |

Amendment/revision ancestry is source-controlled for PO `RevisionDraft` through an exact rejected predecessor and `revision + 1`; old PO lines are immutable. That does not compensate for the uncovered relations above. No database-user, FULL_CONTROL, MD, nullable-status, session-setting, or explicit trigger-disable bypass was found in the reviewed diff. PostgreSQL superuser/owner trigger disabling cannot be excluded by ordinary trigger code and is not claimed as proven.

## Canonical database commercial calculation

The application and canonical database formula agree on the intended sequence:

1. `gross = round(quantity * unitRate, scale)`.
2. `assessable = gross + packing + freight + insurance + otherCharges`.
3. Reject negative quantity/rate/discount/charges, invalid rate/scale, discount above assessable, and values outside `numeric(24,6)` capacity.
4. `taxable = round(assessable - lineDiscount - allocatedHeaderDiscount, scale)`.
5. Calculate and round CGST, SGST, IGST, and cess separately.
6. `payable = round(taxable + tax components + roundOff, scale)`.
7. Compare stored quotation values and exact input/result/tax JSON with relational recomputation.

Arithmetic is PostgreSQL `numeric` and application `decimal`; the application uses `MidpointRounding.AwayFromZero`, consistent with PostgreSQL `round(numeric, scale)` for midpoint values. Representative vectors are covered by executed decimal/GST/maximum/overflow tests, including six-decimal inputs and seven-decimal rejection. Currency and exchange rate are exact for the accepted exchange-rate-one workflow.

The decisive defect is at `Rev869BDatabaseSafetySql.cs` quotation submit: the function is supplied an incomplete input/result object, necessarily returns `FALSE`, and `FALSE IS NOT NULL` counts as a match. The lifecycle trigger independently makes a valid call, but duplicate triggers do not make the defective guard correct or singular. Canonical calculation is therefore **FAIL** as a universal database contract.

## Strict JSON, authoritative joins, and Tax/GST

Positive evidence:

- Commercial input/result and tax objects are compared with fail-closed JSON equality in the canonical function.
- Typed UUID/numeric/date/timestamp conversions are exception-guarded in authoritative transitions.
- Comparison and PO joins cover organization, vendor, RFQ/line, invitation, quotation/revision/line, technical verification, comparison/version/line, PO/line, item, UOM, qualification, attachment, approval policy/history, and timestamps.
- PO traces to the approved comparison and exact recommended quotation line and calls the canonical commercial function.
- Tax proof uses the exact linked setting and checks effective range, organization, HSN/SAC, jurisdiction basis, intra/interstate split, rates, exemption/reverse charge, currency, rounding, approval and active state.

Failing evidence:

- The quotation safety predicate accepts `FALSE` as evidence.
- The retained base comparison/PO guard still contains many nullable `<>` expressions. The new guard is stronger, but both execute; no claim is made that every retained expression is independently fail-closed.
- The named mismatch variables are aliases of one count and are not independent expected/actual/missing/unexpected/duplicate/stale/organization/provenance/commercial/tax/qualification/approval measurements.
- Approval/status histories can be inserted after a parent reaches the relevant status; exact transition-time authorization is not proven.
- Effective tax overlap is not independently counted at the commercial transition. Exact linked-row proof is present, but universal missing/future/expired/ambiguous failure cannot be accepted while quotation reconciliation is bypassable.

## Trigger and function inventory/architecture

Canonical offline SQL contains **15 tables, 35 trigger occurrences/35 unique triggers, 10 function occurrences/10 unique functions, 44 foreign keys, 66 indexes, and 29 checks**.

| Function | Intended ownership |
|---|---|
| `rev869b_commercial_snapshot_reconciles` | Relational commercial/tax recomputation |
| `rev869b_enforce_quotation_transition` | Draft quotation lifecycle and submit proof |
| `rev869b_enforce_transition` | RFQ/invitation/comparison/PO lifecycle |
| `rev869b_guard_authoritative_transition` | Quotation/comparison/PO authoritative proof |
| `rev869b_guard_child_insert` | Late-child INSERT guard |
| `rev869b_guard_controlled_snapshot` | Parent/comparison snapshot mutation guard |
| `rev869b_guard_history_insert` | History-parent consistency |
| `rev869b_reject_immutable_mutation` | Immutable UPDATE/DELETE rejection |
| `rev869b_reject_overlapping_approval_policy` | Approval-policy overlap rejection |
| `rev869b_validate_parent_contract` | Child/parent provenance linkage |

The 35 unique trigger names exactly match the checkpoint and compiled inventory test. All are row-level BEFORE triggers. SQL is statically schema-qualified and contains no unsafe dynamic SQL, recursion, `SECURITY DEFINER`, session-variable bypass, or explicit trigger disabling. Functions use default volatility and invoker security; none fixes `search_path`. The quotation table has two independent BEFORE UPDATE guards that both perform submit reconciliation, so the intended contract is not performed exactly once. Down first removes correction-owned functions with `CASCADE`, then tears down retained owned objects/tables; no accepted migration object is named for removal.

## PostgreSQL test safety and genuineness

All 19 test bodies in the two REV869B PostgreSQL behavior classes compiled and were **NOT RUN**.

Safety positives: exact opt-in `ISOLATED_REV869B_BEHAVIOR_TESTS`; exact database `sess_nexaerp_rev869b_verify`; no fallback; post-open `current_database()` and exact migration-count checks; serial collection; deterministic fixture IDs; collision guards; exact accepted seed IDs; no `ORDER BY ... LIMIT 1` business selection.

Safety blockers:

1. `BeginTransactionScopeAsync` returns a scope with no owned transaction when `CurrentTransaction` exists. Its `RollbackAsync` is then a no-op. The injected-audit-failure tests inspect counts before outer fixture disposal/rollback, so partial writes in the ambient transaction remain visible and the claimed rollback proof is invalid.
2. The direct suite queries organization `REV869B-PG-OWNED-DATABASE-GUARDS`, but `OwnedRfqFixture` creates scenario-specific organizations only. No reviewed fixture creates the required complete RFQ/PO/comparison rows. The direct cases are not independently runnable.
3. Outer rollback is verified only during disposal. A test assertion failure before disposal still invokes disposal, but the test cannot use that post-disposal verifier as its in-body transactional proof.

| Required genuine behavior | Source assessment |
|---|---|
| Successful EF service transaction | Present for RFQ creation; runtime **NOT RUN** |
| Injected failure full rollback | **FAIL source design** due ambient rollback no-op before assertion |
| Real service replay twice | Present sequentially on one DbContext; runtime **NOT RUN** |
| Two independent DbContexts/connections, one winner | **FAIL**; named test uses the same fixture/DbContext sequentially |
| Stale loser with no partial rows | Direct two-connection SQL exists; not a two-service/DbContext proof |
| Protected denial through actual service/endpoint | Service scope denial exists; no mapped endpoint proof |
| Denial audit persistence | **FAIL**; service denial test does not assert audit, direct test manually inserts audit |
| Audit-writer failure rollback | Hook is test-assembly-only, but rollback proof is invalid under ambient transaction |
| Rejected PO revision/resubmission | **Missing** |
| Successful mapped ASP.NET data endpoint | **Missing** |
| RFQ/quotation/comparison/PO terminal INSERT rejection | **FAIL**; direct terminal test exercises RFQ only |
| Late child INSERT rejection | Four relations attempted; invitations, technical verification, and follow-up are omitted |
| Immutable history UPDATE/DELETE | Three history relations attempted, dependent on absent owned fixture |
| Snapshot/tax tampering | One PO total mutation only; no complete quotation/comparison/tax/provenance matrix |
| Exact installed inventory | Exact 35/10 arrays present; runtime **NOT RUN** |

The failure hook is private test code supplied directly through constructor injection; no production request/header/environment activation path was found. That positive isolation does not cure the transaction semantics.

## Test-count reconciliation

- Previous discovered method cases: **455**.
- Current discovered method cases: **469**.
- Added: **14**; removed: **0**; renamed: **0**.
- Added non-PostgreSQL structural/source-contract methods: **5**.
- Added actual PostgreSQL test bodies: **9** (10 -> 19).
- Current REV869B discovered names: **67**.
- Current actual REV869B PostgreSQL class test bodies: **19**.
- Name-marker count is **20** because the non-PostgreSQL structural method `FuturePostgresSourceRetainsExactDatabaseSafetyAndNoFallback` also contains `Postgres` in its fully qualified name.
- Focused filter executed: **47**. It excludes that structural method as well as the 19 PostgreSQL bodies.
- Complete name-filtered non-PostgreSQL execution: **421**. Parameterized theory expansion explains why executed cases do not equal simple listed-method subtraction.
- All Postgres/PostgreSql-marked discovered names: **50**, all excluded and **NOT RUN**.

Classification: the five new contract methods are structural/source-text tests; six new application-class cases invoke a real EF service but none invokes a mapped endpoint; thirteen direct-class cases use Npgsql/manual SQL. Actual successful mapped endpoint tests: **0**. Sequential operations are not classified as concurrency. No method was removed or renamed, but the ambient rollback, two-instance naming, and external-fixture dependency materially weaken the claims attached to several new names.

## Regression and permitted offline validation

| Validation | Result |
|---|---|
| PowerShell 5.1 AST parse | PASS: 23 files, 0 errors; scripts not executed |
| `dotnet build` Release `--no-restore` | PASS: 0 warnings, 0 errors |
| Focused REV869B non-PostgreSQL filter | PASS: 47/47 |
| Complete non-PostgreSQL filter | PASS: 421/421 |
| PostgreSQL source compilation | PASS through build; 19 bodies **NOT RUN** |
| EF migration discovery | PASS with `--no-connect`: 13 migrations; REV869B exactly once after REV869A |
| Executable model/snapshot parity | PASS in the focused suite (`CurrentDesignTimeModelAndSnapshotHaveNoDifferencesWithoutConnecting`) |
| Migration/designer/snapshot consistency | PASS through build, mapping tests, and executable model differ |
| Accepted REV868/REV868C3/REV869A regression | PASS through 421 tests; no accepted migration changed |
| Diff secret/prohibited scan | PASS: no secret-like addition and no prohibited operation performed |
| `git diff --check` | PASS |

Previously passed exact +1 enforcement, organization/version optimistic concurrency, HTTP 409 mapping, decimal scale/capacity, MD/operational permission segregation, explicit `CanIssue`, creator/issuer separation, awaited denial auditing, audit failure propagation, REV868 PR/stock/reservation/PendingRFQ behavior, REV868C3 employee/department/manager mapping, and REV869A identity/UOM/GST/vendor/warehouse/Rack-Bin/QC/scope contracts remain source-present and pass the permitted regression suite.

## Independently regenerated SQL hashes and schema inventory

Generated offline from REV869A to retained REV869B and in reverse with `--no-transactions`, `--no-build`, and an unreachable loopback port-1 design-time string. No database connection was opened. Unique temporary files were removed after hashing.

- Up: **129,064 bytes**, SHA-256 `42050498E9ED4F876FAE02CC2AF95BB5680434B4FF31FDF6ABBC5E941D3CB725`.
- Down: **6,241 bytes**, SHA-256 `70E55F4635F44F2FBD7035910E1620A0F0507E760E402F83419C30E4093CC1D5`.
- Up inventory: **15 tables, 35 unique triggers, 10 unique functions, 44 foreign keys, 66 indexes, 29 checks**.
- Down inventory: **15 table drops** plus correction/retained owned function teardown.

The bytes, hashes, and counts exactly reproduce the checkpoint. Inventory equality is not semantic acceptance.

## Remaining blockers and exact next authorized gate

1. Make quotation reconciliation require a true canonical result; remove the false-is-non-null path and duplicate/conflicting submit proof.
2. Add status-aware UPDATE/DELETE immutability for RFQ lines, invitations/qualification evidence, comparison lines, and any immutable follow-up evidence; bind every history INSERT to the authorized transition, actor, route/version, and timestamp.
3. Replace or remove retained nullable `<>` validation paths and measure each cardinality/mismatch category independently.
4. Correct ambient transaction failure semantics so a service failure restores an in-test savepoint/baseline even when an outer fixture transaction exists.
5. Create every direct-test fixture deterministically in source, or make the suite self-contained; remove the undeclared `REV869B-PG-OWNED-DATABASE-GUARDS` dependency.
6. Add genuine two-DbContext service concurrency, successful authenticated mapped endpoint, denial-audit assertion, rejected-PO revision/resubmission, all-parent terminal insertion, all-child late insertion, and full commercial/tax/provenance tampering tests.

The exact next authorized gate is a fifth controlled source-only REV869B correction followed by a new independent source-only safety re-review. Isolated database provisioning and execution-helper design are not authorized while either canonical state is FAIL. No migration, database, or helper command is provided.

rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
