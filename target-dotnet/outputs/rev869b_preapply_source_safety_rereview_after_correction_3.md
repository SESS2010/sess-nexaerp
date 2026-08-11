# REV869B Third Independent Source-Safety Re-review After Correction 3

Date: 2026-08-11 (Asia/Calcutta)

## Review identity and boundary

- Correction commit: `d40dbf621608dc3d2ba30aea30dfe8504753206f`.
- Parent commit: `360a59c4d2ac89f125ce9a1622ea67f16fbc84e5`.
- Exact reviewed range: `360a59c4d2ac89f125ce9a1622ea67f16fbc84e5..d40dbf621608dc3d2ba30aea30dfe8504753206f`.
- Method: independent diff, source, migration, generated SQL, test-body, test-inventory, build, no-connect EF, parsing, permission, privacy, and regression inspection. The correction checkpoint was treated as a claim and reproduced where possible.
- PostgreSQL tests were compiled but **NOT RUN**. No PostgreSQL connection, database helper, migration apply/remove, database creation, backup, restore, execution-helper, production, REV861, frontend, REV869C, AWS, or legacy-reference operation was performed.

The source-safety gate remains closed. Exact database version increments, application decimal validation, and explicit operational permissions are corrected, but immutable snapshot insertion and full database-side commercial/GST reconciliation remain bypassable. Several named PostgreSQL tests also do not execute the protected application behavior they claim.

## Entry and exact-diff verification

| Check | Result | Independent evidence |
|---|---|---|
| Current commit | PASS | `git rev-parse HEAD` returned `d40dbf621608dc3d2ba30aea30dfe8504753206f`. |
| Exact parent | PASS | `git rev-parse HEAD^` returned `360a59c4d2ac89f125ce9a1622ea67f16fbc84e5`. |
| Exact correction scope | PASS | Exactly 18 paths: one added checkpoint and 17 modified source/test files; 775 insertions and 145 deletions. |
| Declared files | PASS | The diff path set exactly matches the checkpoint's 18-file list. |
| Unrelated files | PASS | No path outside that declared set occurs in the range. |
| Target status before report | PASS | `git status --short -- .` returned no entries. |
| Legacy isolation | PASS | The range has zero `../legacy-reference/` paths; that directory was not accessed. |
| Whitespace | PASS | `git diff --check` returned no findings. |

The 18 reviewed paths are the checkpoint, application contract, authorization entity/service, purchase domain, retained REV869B migration/designer/snapshot/mappings/seed, three purchase service files, and four REV869B test files exactly as declared in `outputs/rev869b_source_correction_checkpoint_3.md`. No accepted REV868, REV868C3, or REV869A migration file changed.

## Material blocker disposition

| Area | State | Evidence and conclusion |
|---|---|---|
| A. Immutable database snapshot reconciliation | **FAIL** | Application reconciliation is materially stronger and covers typed quotation, comparison, PO, tax, provenance, line count, and aggregate evidence. Database enforcement remains incomplete: quotation, comparison, and PO child snapshot relations reject UPDATE/DELETE but permit INSERT after controlled/terminal parent states; the parent-contract trigger checks ancestry only. The comparison trigger does not recompute taxable/tax/payable values from authoritative input, and its `taxRule` JSON comparison uses `<>`, so a missing JSON member can yield SQL NULL rather than a positive mismatch. The PO issue trigger does not join snapshots back to the exact quotation/comparison rows and checks only selected tax/provenance fields and stored sums. Fabricated but internally self-consistent snapshot JSON can therefore pass direct SQL enforcement. |
| B. Exact database and application version enforcement | **PASS for source structure** | RFQ, invitation, quotation, comparison, and PO UPDATE triggers require `NEW."Version" = OLD."Version" + 1`; unchanged, lower, skipped, and higher versions fail. Application reserve operations scope by organization/record/version, atomically increment once, require exactly one affected row, and map `DbUpdateConcurrencyException` to HTTP 409 with awaited denial audit. Initial states and transition matrices are trigger-controlled. Live trigger behavior remains for the isolated database gate. |
| C. Decimal scale and overflow | **PASS for API/application; database raw-scale limitation recorded** | Every quantity, rate, discount, charge, round-off, exchange rate, and tax rate is checked for scale <= 6 before calculation. Arithmetic is decimal-only, checked for numeric(24,6) capacity, multiplication/sum/tax/payable overflow, prohibited negatives, and AwayFromZero rounding order. Executed tests cover six decimals, positive/negative seven-decimal rejection, exact maximum, above maximum, multiplication overflow, GST, and payable reconciliation. PostgreSQL `numeric(24,6)` itself rounds excess-scale direct SQL input during type conversion; this implementation does not and cannot claim raw pre-conversion scale detection in the database. |
| D. MD and operational permission segregation | **PASS for source structure** | `CanIssue` is persisted with false default, mapped in context/designer/snapshot, seeded true only for Purchase Manager/PO, and dropped by the REV869B Down migration. The six REV869B pages require explicit flags even when `HasFullControl` is true. MD retains explicit view/audit/commercial/export and configured approval rights but lacks create/update/submit/resubmit/issue. PO creator self-approval, approval-route role/mapping, and issuer/approver separation are enforced. Endpoint filters and service role/scope checks prevent direct URL bypass; denials await audit writes and audit failure propagates. No all-false fixed row exists. Live persistence behavior remains unrun. |
| E. PostgreSQL test safety | **PASS** | Every test requires exact opt-in `ISOLATED_REV869B_BEHAVIOR_TESTS`, requires `REV869B_POSTGRES` with no fallback, requires connection-string database exactly `sess_nexaerp_rev869b_verify`, verifies `current_database()` after opening, and requires the REV869B migration row exactly once. Exact equality rejects the base, REV868, REV869A, postgres/template, REV861-like, and production-like names. |
| E. PostgreSQL test genuineness | **FAIL** | Material rollback and two-connection CAS/concurrent unique-key collision are genuine direct PostgreSQL operations. However, successful persistence is only a manual audit insert/delete; idempotent replay is a direct SELECT after one insert, not service replay; permission denial manually checks a permission row then manually inserts audit evidence; audit-failure propagation manually performs an RFQ UPDATE and deliberately invalid audit INSERT rather than invoking a protected service/endpoint. No successful service transaction, protected denial path, audit writer failure path, repeated revision workflow, or mapped successful data endpoint is exercised. The removed runtime immutable-history and exact trigger-inventory probes were not replaced. |
| F. Regression review | **PASS within the permitted offline boundary** | Build and all 417 non-PostgreSQL tests pass. The diff changes no accepted REV868/REV868C3/REV869A migration, removes no prior permission/history row, and the complete suite covers PR creation/approval, stock/reservation, PendingRFQ, employee/department/manager mapping, identity, UOM/conversion, tax, vendor, warehouse/Rack-Bin, QC, and record-scope contracts. `CanIssue` has a false database default for historical inserts. Live PostgreSQL preservation remains outside this review. |
| G. Test reconciliation | **PASS for accounting; FAIL for sufficient behavioral strength** | Counts reconcile exactly, but the added/rewritten evidence does not close the application transaction, protected denial/audit, or database snapshot blockers. |

## Commercial and immutable snapshot assessment

The application calculator uses decimal arithmetic and the authoritative sequence: validated gross multiplication; assessable value plus packing/freight/insurance/other charges; line plus allocated header discount; taxable value; CGST/SGST/IGST/cess; round-off; payable total; exact line aggregation. Quotation submission allocates the header discount deterministically, stores typed tax snapshots, and later comparison/PO construction reuses server-calculated values. Comparison approval, PO submit, PO approval, and PO issue call application reconciliation.

Application snapshot checks cover organization, vendor, RFQ, quotation/revision/line, comparison, PO linkage, line/item/quantity/UOM, unit rate through typed input, gross/assessable/discounts/charges/tax/round-off/payable, currency/exchange rate, HSN/SAC and GST rule, qualification/attachment, and approval route/value/effective evidence. Exact line-count checks reject missing and unexpected recommended comparison rows, and `SingleOrDefault` rejects duplicate application matches.

Database proof is not equivalent:

1. `vendor_quotation_lines`, `commercial_comparison_lines`, and `purchase_order_lines` can be inserted after their parent has become submitted, approved, or issued. Their immutable triggers cover UPDATE/DELETE only.
2. The comparison transition guard checks selected stored fields but not the snapshot `input` object or a full formula recomputation. A direct SQL quotation line can contain mutually consistent but commercially false stored totals.
3. `cl."CommercialSnapshotJson"->'taxRule' <> ql."TaxRuleSnapshotJson"` is not fail-closed for a missing member because SQL NULL is not true. `IS DISTINCT FROM` or an explicit presence/type test is required.
4. The issue guard checks PO snapshot identifiers for presence/equality to the PO and sums stored JSON results, but does not join the quoted line, quotation version, RFQ, comparison line/version, tax rule, vendor qualification, attachment, or approval timestamp to authoritative rows and recompute the formula.
5. Tax-rule database checks are limited to organization, Approved/active, and nonblank HSN/SAC; they do not prove effective date, jurisdiction/supply split, rates, reverse charge/exemption, currency/rounding, or exact equality with the quotation tax snapshot.

Consequently recommendation, approval, and issue are fail-closed through the intended application service, but are not impossible through direct SQL against missing, altered, late-inserted, or self-consistent fabricated snapshots. Snapshot immutability and exact reconciliation are not proven for every version.

## Version enforcement assessment

- Every mutable request contract inspected carries an expected aggregate version.
- Every reserve query includes aggregate ID, organization scope, and exact expected version; affected rows must equal one.
- The service returns only `expected + 1` and checked arithmetic prevents overflow.
- Database UPDATE enforcement now independently requires exactly `OLD.Version + 1` for RFQ, invitation, quotation, comparison, and PO.
- Parent organization/provenance fields are compared with `IS DISTINCT FROM` tuples and are immutable.
- INSERT guards reject invalid RFQ, invitation, quotation, comparison, and PO initial/terminal states; RevisionDraft requires an exact rejected predecessor chain.
- Status, approval, PO, and transaction histories remain append-only through UPDATE/DELETE rejection.

This closes the previous non-exact version blocker at source level. The PostgreSQL skipped/lower-version test is compiled but was not run.

## Decimal and overflow assessment

| Required vector | Result |
|---|---|
| Six decimal inputs | PASS |
| Positive seven-decimal input | PASS: rejected before EF persistence and mapped to HTTP 400 |
| Negative seven-decimal round-off | PASS: rejected |
| Tax-rate scale violation | PASS: rejected |
| Exact `999999999999999999.999999` | PASS |
| Value above maximum | PASS: rejected |
| Intermediate multiplication overflow/capacity | PASS: rejected |
| Sum overflow | PASS: rejected |
| GST/tax/payable reconciliation | PASS |
| Float/double arithmetic | PASS: none in commercial calculation |
| Raw direct-SQL scale detection | Not claimed; PostgreSQL numeric conversion can silently round before constraints/triggers inspect the value |

## Exact role x page x requested-action matrix

Legend: `CE` Create/Edit, `S` Submit, `RS` Resubmit, `A` Approve, `R` Reject, `RV` Revise/request revision, `I` Issue, `C` Cancel, `CV` Commercial View, `E/D` Export/Download, `AH` Audit/History. A dash means denied. View/print/verify/clarification/upload are outside the requested action columns.

| Role | Page | CE | S/RS | A | R | RV | I | C | CV | E/D | AH |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Purchase Manager | RFQ | CE | S | - | - | - | - | C | CV | E/D | AH |
| Purchase Manager | Vendor quotations | CE | S | - | - | - | - | C | CV | E/D | AH |
| Purchase Manager | Technical verification | - | - | - | - | - | - | - | CV | E/D | AH |
| Purchase Manager | Commercial comparisons | CE | S/RS | A | R | RV | - | - | CV | E/D | AH |
| Purchase Manager | PO | CE | S/RS | - | - | - | I | C | CV | E/D | AH |
| Purchase Manager | Material follow-up | - | - | - | - | - | - | - | CV | E/D | AH |
| Purchase Executive | RFQ | CE | S | - | - | - | - | - | CV | D | - |
| Purchase Executive | Vendor quotations | CE | S | - | - | - | - | - | CV | D | - |
| Technical Engineer | RFQ / Vendor quotations / Technical verification | CE only on Technical | S only on Technical | - | - | - | - | - | - | D | - |
| Technical Director | All six REV869B pages | - | - | A on comparison/PO | R on comparison/PO | RV on comparison/PO | - | C on RFQ/quotation/PO | CV | E/D | AH |
| Managing Director | All six REV869B pages | - | - | A on comparison/PO | R on comparison/PO | RV on comparison/PO | - | C on RFQ/quotation/PO | CV | E/D | AH |
| Stores Manager / Stores Executive | PO and material follow-up | - | - | - | - | - | - | - | - | D | - |
| Accounts Head | Comparison and PO | - | - | - | - | - | - | - | CV | E/D | AH |
| Department Manager (resolved active role) | RFQ / comparison | - | - | - | - | - | - | - | - | D | AH |
| Department Manager (resolved active role) | PO | - | - | A | R | RV | - | - | - | D | AH |

Only Purchase Manager/PO has `CanIssue=true`. The fixed seed contains 29 nonempty rows; three Department Manager rows are inserted only after resolving one active role. Migration, designer, snapshot, context mapping, seed data, endpoint filters, and permission service agree on `CanIssue`. Down deletes only source-owned REV869B permission/page rows under owner predicates and removes the REV869B-owned column after temporary owner guards.

## PostgreSQL test safety and genuineness

All ten current REV869B PostgreSQL tests are compiled and **NOT RUN**.

| Claimed behavior | Source assessment |
|---|---|
| Persisted successful transaction | Partial: real insert/read/delete, but only a manually written audit row, not a purchase service transaction |
| Rollback before/after equality | Genuine direct PostgreSQL RFQ version mutation and rollback |
| Two independent connections/contexts | Two independent Npgsql connections, not DbContexts |
| Exactly one concurrency winner/stale rejection | Genuine two-connection CAS; first wins and stale UPDATE affects zero rows |
| Idempotent replay without duplicates | FAIL as application evidence: one direct insert followed by SELECT of the same row; no replay command/service call |
| Concurrent idempotency collision | Genuine database unique-key collision, but no application loser replay result |
| Direct terminal insertion | Genuine RFQ terminal INSERT rejection only; comparison and PO terminal INSERTs are not exercised |
| Snapshot mismatch | Genuine attempted PO issue UPDATE with altered total, dependent on an existing fixture |
| Permission denial with audit persistence | FAIL as protected-operation evidence: permission is queried and audit is manually inserted |
| Audit failure propagation | FAIL as protected-operation evidence: RFQ and invalid audit SQL are manually composed; no application audit writer/path is invoked |
| Lower/skipped version rejection | Genuine direct PostgreSQL attempts |

Fixture selection uses `ORDER BY ... LIMIT 1` existing Draft RFQ/Approved PO rows and does not create a complete deterministic per-test purchase fixture. The suite also no longer verifies immutable history mutation or the exact installed trigger inventory. These deficiencies keep executable behavior and helper readiness blocked even though opt-in/database-name safety is fail-closed.

## Test-count and strength reconciliation

- Before correction listed/discovered cases: **450**.
- After correction listed/discovered cases: **455**.
- Net listed-name increase: **5**.
- Focused REV869B non-PostgreSQL: **40 -> 43**.
- Complete non-PostgreSQL executed: **414 -> 417**.
- REV869B PostgreSQL: **8 -> 10**, all current ten **NOT RUN**.
- Current all PostgreSQL/Postgres-named tests: **40**; no PostgreSQL-named test was run.
- Current REV869B inventory: **53** listed names = 43 non-PostgreSQL plus 10 PostgreSQL.

Exact added non-PostgreSQL cases:

1. `DecimalBoundariesRejectInvalidScaleAndReconcileGstAndPayableExactly`
2. `SevenDecimalInputMapsToHttp400`
3. `PreApprovalSnapshotReconciliationDoesNotRequireApprovedStatusButStillFailsClosed`

The eight prior REV869B PostgreSQL method names were removed/replaced, not preserved as simple renames: `InsertTerminalStateIsRejected`, `InvalidUpdateTransitionIsRejectedByTrigger`, `SnapshotMismatchBlocksIssue`, `ImmutableHistoryRejectsMutation`, `TransactionFailureRollsBackAllRows`, `ConcurrentAggregateUpdatesHaveSingleWinner`, `ConcurrentIdempotencyHasSingleAuthoritativeResult`, and `TriggerInventoryIsInstalledExactlyOnce`. Ten new PostgreSQL methods replace them. The replacement materially strengthens rollback/concurrency and fail-closed setup, but weakens coverage by dropping live immutable-history and trigger-inventory checks and still does not exercise protected application permission/audit flows.

Focused evidence classification:

- Actual EF service tests: **1**, limited to identity/role/malformed-request branches before database access; successful service workflow count: **0**.
- Actual mapped API endpoint tests: **1**, a mocked permission-denial attachment route; successful mapped data endpoint count: **0**.
- Direct endpoint `Run` helper cases: **9** (HTTP exception mapping/audit behavior), not mapped data operations.
- Structural/source-only discovered cases: **13** (migration/API/service text and metadata assertions).
- Mocked focused cases: **11** (current user, audit, permission, authentication, scope, or no-connect service dependencies).
- PostgreSQL cases remaining NOT RUN: **10**.

No non-PostgreSQL test was removed or weakened. The PostgreSQL replacement findings above are the exact removals and coverage changes.

## Permitted offline validation results

| Validation | Result |
|---|---|
| PowerShell 5.1 AST parse | PASS: 23 files, 0 errors; no script executed |
| `dotnet build SESS.NexaERP.slnx --no-restore` | PASS: 0 warnings, 0 errors |
| Focused REV869B non-PostgreSQL | PASS: 43/43 |
| Complete non-PostgreSQL | PASS: 417/417 |
| PostgreSQL test compilation | PASS through solution build; NOT RUN |
| EF migration discovery | PASS with `--no-connect`: 13 migrations, REV869B exactly once after REV869A |
| EF pending-model CLI | NOT AVAILABLE: installed EF rejects `--no-connect`; no connecting fallback was run |
| Migration/designer/snapshot parity | PASS through the executable no-connect model-differ test in the focused suite |
| Diff whitespace | PASS |
| Diff-only secret scan | PASS: 0 secret-like additions |
| Diff-only privacy scan | PASS: ten numeric-pattern matches were all the all-zero UUID sentinel, not PII |
| Accepted migration preservation | PASS: 0 changed REV868/REV868C3/REV869A migration files |
| Legacy range diff | PASS: 0 paths |

### Independently generated SQL

Generated offline from REV869A to REV869B and the reverse, with `--no-transactions` and an unreachable loopback design-time string. No database connection was opened.

- Up: **92,431 bytes**, SHA-256 `BDF097EC935941348F796F4B7E9B6FB147D3055220ECBA99FFAE32EFF1D6E5AC`.
- Down: **5,848 bytes**, SHA-256 `1E71AC4E4B42DC7DD48DB718B0CD188BD1DBE8D1B003B22D0A49654E189E4647`.
- Up inventory: 15 tables and 22 REV869B triggers.
- Down inventory: 15 table drops plus temporary owner guards.
- The two temporary SQL files were removed after hashing.

Both hashes independently match the checkpoint claims. A first default generation included `START TRANSACTION`/`COMMIT` wrappers and was exactly 31 bytes larger; regenerating with the checkpoint's canonical `--no-transactions` form produced the claimed bytes and hashes.

## Remaining blockers and exact next authorized gate

1. Block INSERT, not only UPDATE/DELETE, into immutable quotation/comparison/PO child snapshot relations once the parent leaves its editable state.
2. Recompute and compare the complete commercial formula in database enforcement, including input quantity/rate, gross, line/header discount, charges, assessable/taxable value, GST components, round-off, and payable totals.
3. Replace nullable JSON `<>` checks with explicit presence/type validation and fail-closed `IS DISTINCT FROM` semantics where appropriate.
4. At comparison approval and PO issue, join every snapshot to exact organization/vendor/RFQ/quotation+version/comparison+version/line/item/UOM/currency/tax/qualification/attachment/approval source rows and reject missing, unexpected, duplicate, stale, or altered rows.
5. Add deterministic PostgreSQL fixtures and genuine application service/mapped-endpoint tests for successful commit, rollback on injected failure, idempotent replay, concurrent loser behavior, protected permission denial with audit persistence, and audit-writer failure propagation; restore immutable-history and exact trigger-inventory runtime checks.

The exact next authorized gate is another controlled source-only REV869B correction followed by a new independent source-safety re-review. Isolated database provisioning and execution-helper design are not authorized while this source-safety state is FAIL. No migration/apply command is provided.

rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
