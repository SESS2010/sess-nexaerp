# Item 15 reporting — work in progress

Parent commit: 7eb9a67 (Item 16). This commit is a partial Item 15 checkpoint; the ten-report requirement is not complete. No changes have been applied to the owner database.

## Scope and unresolved prerequisites

Seven report paths have passing business checks: stock balance, movement roll-forward, GRNI, vendor purchases, purchase register, pending approvals and custody by engineer. FIFO valuation is an eighth, restricted prototype: initial layers, bill adjustments, opening valuation and age boundaries pass, but accepted physical returns without cost credits cause 409 FIFO_RETURN_CREDITS_REQUIRED. This refusal is not a completed return-cost policy. Billed-not-received and delivered-machine ancestry are not implemented.

Three decisions remain pending, documented in item-15-source-prerequisites.md:
- attribution of a partial return across its original issue's consumed FIFO layers;
- whether invoice-before-receipt and actual machine-delivery recording should be brought forward;
- scheduling the confirmed ownership-pool source correction and its real transaction refusal witness.

Frozen Stores Full Schema Guideline paragraphs 116 and 325 require FIFO within company, item and ownership/value pool. The installed consume_fifo_for_issue selects layers by company/item without an ownership predicate. This is a confirmed source conflict, not yet a witnessed cross-ownership transaction failure. Grouping report layers by ownership does not resolve it. FIFO completion is paused; no historical postings have been rewritten.

## Report and frontend contract

Routes:
- GET /api/v1/reports/
- GET /api/v1/reports/{key}
- GET /api/v1/reports/{key}/excel

Query parameters: fromDate, toDate, mode=summary|details, selection (JSON group returned by a row or total), metric, page and pageSize (1–1000). The API's existing PascalCase DTO convention applies, including TimeZone, Rows, Totals and Coverage. JSON objects inside source rows retain their explicit SQL keys. Rows and totals return selections for drill-through.

Reporting.DefaultTimeZone supplies default dates, timestamp date cutoffs and age bands. Reporting.CompanyTimeZones overrides it by company code. Both SESS identity-provider configuration examples select Asia/Kolkata; absent configuration defaults to UTC. Startup validates the configuration. SQL receives a bound timezone parameter and validates it too. Stored PostingDate values and existing authorization calendar rules are unchanged. Excel identifies the report calendar separately from its UTC generation timestamp.

Report failures retain their specific Code through the standard error envelope. AdministratorActionRequired is present and true for source failures requiring intervention, otherwise omitted. Covered codes include REPORT_ACCESS_DENIED, REPORT_REQUEST_INVALID, FIFO_RETURN_CREDITS_REQUIRED and FIFO_SOURCE_INCONSISTENT. Only trusted endpoint metadata may override the standard wrapper's code; arbitrary response content cannot.

One PostgreSQL statement checks the authenticated employee, selected company, live membership, operational scope, approved role assignments, role activation and page permissions, then returns totals and rows. Export/denial/source-unavailable auditing occurs within that statement. Employee page grants allow view only, not export or commercial access. Forbidden reports are absent from the catalogue and direct access returns 403.

Stock/custody permissions inherit stock-check permissions; finance/FIFO inherit vendor-bill commercial permissions; register inherits PO permissions. Pending queues derive read/export permissions from existing approval pages without granting transaction rights. Directors see the operational overview; other approvers see their named or role queue. Shared masters are identified. Pending approvals is current-only and counts actions, not distinct documents.

Five financial/register/approval/FIFO functions use SECURITY DEFINER with fixed queries/search_path, an explicit nexa_erp_runtime session guard, embedded authorization and exact-signature grants. Direct vendor_bill_lines SELECT remains refused. No financial table SELECT permission was added. Up/Down/reapply runs only against disposable PostgreSQL. Controlled SQL is an immutable embedded migration snapshot: SHA-256 CBDFD27E74CC85D4F3244958108FFB2B7E471ED22E4F04323E921F21CF37F689. Later changes require a new migration.

## Data and Excel behavior

Stock retains item, unit, warehouse, bin, lot, serial, ownership, condition and custody. Opening + receipts - issues + adjustments = closing. Opening introduced during the period is part of adjustments and identified separately, not counted twice.

GRNI uses finalized normal receipts without accepted bills as of the date, excluding finalized reversals; its value is explicitly at PO rate. Vendor purchases include bill acceptance/reversal events and allocated inventory charges. Register stage events prevent line-join multiplication; draft/rejected bill references do not become accepted quantity. Current PR/PO status is live, not a historical snapshot.

Unlike units and currencies stay separate. Custody totals retain engineer identity and unit; no grand custody quantity exists.

Excel provides About, Totals, Summary and source Details, with internal links from numeric totals/summary cells. XML streams into ZIP; compressed download bytes remain in memory. Details split at 1,048,575 data rows; summary sheets split before 60,000 numeric hyperlinks. Text uses inline strings, never formulas; invalid/oversized cell text fails explicitly. Export sends stock labels once per contiguous group; the writer retains only that group's labels and rejects missing/out-of-order metadata. API JSON rows retain full labels.

[Excel worksheet limits](https://support.microsoft.com/en-us/excel/excel-specifications-and-limits) informed splitting.

## Current verification

Release builds succeeded with zero warnings/errors. The calendar/report regression passed 13 tests (item15-calendar-release.trx, 4m56s). The full core Release suite passed **834 tests, zero failed/skipped, 31m03s** (item15-core-release.trx), excluding the separate Keycloak witness and preceding the test-only company-switch addition.

The updated complete three-band purchase witness, including company switching, passed in 3m19.3457s. The same two-test run had a Keycloak fixture failure, so item15-company-keycloak-release.trx records one pass and one failure, not a passing overall run. Its mapping used database local current_date after India midnight while application identity resolution used the previous UTC day. Explicit UTC fixture dates corrected that mismatch; production authentication was unchanged. The corrected Release build succeeded with zero warnings/errors, and **the real Keycloak rerun passed one test, zero failed/skipped, 1m28s** (item15-keycloak-utc-release.trx).

Both Debug builds succeeded with zero warnings/errors: 4m06.80s with Keycloak and volume witness compilation enabled, then 17.27s with volume execution disabled. The full Debug suite, including real Keycloak, passed **838 tests, zero failed/skipped, 30m25s** (item15-full-debug.trx). All current source was built for this run.

See [the frontend contract](item-15-frontend-contract.md) for exact casing, drill-through selections, company switching and errors. These are backend endpoints; production frontend integration is not claimed.

Observed business assertions:
- Every report Get/Export uses one observed Npgsql command under the restricted runtime principal; direct financial SELECT returns 42501. Live permission revocation and export/denial audits pass.
- GRNI quantity moves 3 → 1 → 0 as bills are accepted.
- Vendor net accepted quantity 3 and charges 12 reconcile across five acceptance/reversal rows.
- Register has three PR groups and nineteen source events; requested, ordered, net received and net billed each total 3.
- Each band's pending PR names the actual SESS-14 approver.
- SESS-05 custody is 0.95 after issue and 0.40 after first return. A later governed 0.05 issue to SESS-06 retains SESS-05's captured balance unchanged. Each engineer's drill-through reconciles; Excel has separate totals. Stores export is refused; Technical Director export succeeds.
- Initial FIFO provisional value is 128,620.01, matching existing PO TotalPayableValue / OrderedQuantity layer creation. Two accepted bill layers become BILL_LANDED while one remains PO_PROVISIONAL_IDENTICAL. Returns then trigger the explicit source refusal and two failed/unavailable audit rows.
- Governed opening posts ten units at 25, OPENING_LANDED 250, on 2027-03-31. Earlier reports exclude it. Age boundaries 30/31, 90/91, 180/181 and 365/366 pass.
- India calendar boundary, company timezone override, invalid configuration, response metadata and the vendor report before its first local acceptance day pass.
- Standard HTTP 403/400/source-refusal codes and administrator-action flag behavior pass.

## Measured scale result

item15-shared-labels-volume.trx: **one passed, zero failed/skipped, 20m51s**. It first executes the three-band business and two-engineer assertions, then benchmarks exactly 300,000 items and 2,000,000 movements.

| Measurement | Stock balance | Movement roll-forward |
|---|---:|---:|
| Runtime summary SQL | 16.0833 s | 14.3387 s |
| Groups | 300,007 | 300,012 |
| Contributing rows | 1,999,989 | 2,000,000 |
| EXPLAIN execution | 16.2359 s | 14.9008 s |
| Estimated root cost | 1,587,724.25 | 1,589,721.07 |

Full roll-forward SQL plus Excel writing took **206.9907 seconds**, with the header available after 132.61 seconds. Export estimated root cost was 2,426,904.56. The **479,177,860-byte XLSX** contains 300,012 summary rows, 2,000,000 detail rows, 1,800,108 hyperlinks and 35 worksheets. Streaming XML verification checked row counts, sheet limits and hyperlink destinations. It has not been opened in desktop Excel. The database's 4 GB temporary-file cap remained in force.

Evidence is local-evidence/item15/volume-shared-labels/: plans, measurements, progress and movement-roll-forward.xlsx. The measured candidate precedes the calendar changes; those add timezone metadata/auditing and financial date handling. The stock aggregation/export algorithm is unchanged.

The benchmark creates separate report_volume copies of items/movements on the disposable advance_parser database, loopback non-5432 port. It retains columns/indexes but omits business checks, foreign keys and triggers; original governed rows are untouched. Production stock SQL changes only those two relation names. Setup took 698.287 seconds. The cluster uses fsync/synchronous_commit off; reads follow setup/ANALYZE and are not a guaranteed cold-cache benchmark. These are SQL timings, not HTTP end-to-end timings or two million governed posting operations.

Decision: retain direct queries for now, using one request-local materialized raw-source CTE, aggregation before labels and compact export sorting. No persisted aggregate/materialized view or invalidation scheduler was introduced. Maximum-size summaries still take 14–16 seconds and the complete file is large.

## Earlier failures and corrections

The baseline measured stock/roll-forward at 33.73/32.76 seconds, with stock EXPLAIN 35.25 seconds and root cost 2,167,064.84. Export exceeded the same 4 GB temporary-file cap. A second compact-array version still failed export. Sharing labels per group and reusing the filtered source produced the passing result above.

Earlier attempts exposed constrained synthetic posting hashes/chain fields, an incorrect Stores export expectation, the provisional FIFO expected value, UTC/local-date cutoff mismatches and report codes lost by the standard wrapper. Fixes retained ledger constraints and authority rules. Failed runs remain in local evidence and are not counted as passes. The combined pre-calendar API/report/OIDC run item15-explicit-date-volume.trx had 54 passed and one failed (the volume export); it is not a full passing regression.

The shared-label workbook check item15-shared-labels-writer.trx passed seven tests in 620 ms. A preceding new test's wrong ItemCode column expectation was corrected from column 2 (PostingDate) to column 3.

## Operational notes

Two verified inactive disposable PostgreSQL directories were removed, recovering 139,734,497 bytes. Recorded PIDs were absent or no longer PostgreSQL, test ports were closed, and paths contained no links. No owner database or personal files were touched.

Automatic review initially rejected broad privileged report functions; inspection supported a narrower existing runtime-only pattern with fixed queries, authorization and exact grants, which was accepted. There is no outstanding approval denial.

Company-switch verification uses existing SESS_PVT_LTD/SESS_PROPRIETORSHIP memberships and role assignments. Six report families switch to the empty second company and restore their totals on switching back. First-company assignment IDs are refused in the second company. It adds no authority rows or synthetic movements. The empty second-company FIFO report is unaffected by the first company's return-credit gap.

Additional cleanup removed only redundant session-created qemu.exe installer and 7zip.msi downloads: 208,622,968 bytes. The extracted QEMU executable, Alpine media, persistent VM disk and evidence remain.

The mixed application-UTC/database-session-date authority boundary is recorded for Item 25's role-change review. No authorization bypass is claimed. Permission-seed review confirmed that Up refuses installation unless all three required baseline pages are active.

Resume: continue independent Item 25 concurrency work while the three policy/source decisions remain pending. Item 15 remains partial; its incomplete reports are not silently accepted as complete.
