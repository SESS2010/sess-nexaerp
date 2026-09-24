# SESS backlog after go-live, 8 October 2026

> Retitled 23 September 2026 with the governed move of go-live from 1 to 8 October,
> recorded in [the runbook](go-live-fresh-database-runbook.md#governed-schedule-change-23-september-2026-go-live-moves-to-8-october).
> The filename is unchanged so existing links keep working; "post-October" still describes it.
> Nothing in the content below was rescheduled by the move — these items were never
> launch deliverables, and the extra week does not make any of them one.

Proposal only: no feature, migration or OS change is authorised by this document.
One developer familiar with this code; estimates are engineering person-days including
focused permission/audit/replay tests, not calendar promises. Allow roughly +/-50%
until design. Full regression wall time, SESS decisions and field acceptance are extra.
Rows share infrastructure and must not be added blindly. Production login and the
small vendor commercial-verification screen remain the frontend developer's launch
work; this backlog must not delay either. Server Windows 10 stays for now.

## Seven setup operations

These are the seven gaps in setup-screen-gaps-20260921.md, not seven new backend APIs.
Use the agreed workbook/API fallbacks for launch, with real employee sessions and
separate checker decisions. Screen effort assumes the existing API contract; adding
uniform configuration maker-checker is separately governed by the Configuration work.

| Screen | Days | Scope / dependency |
|---|---:|---|
| Warehouse | 1-2 | Governed import/create/edit, submission, independent TD approval, concurrency and read-back. |
| Rack/bin | 1-2 | Same lifecycle, approved warehouse dependency, condition/UOM lookup and immutable Bin Code. |
| Condition location | 1-2 | Effective company/warehouse/bin/condition references and closing. Existing API has no second-person approval state; retain independent TD review until a governed decision workflow is built. |
| Category route | 1-2 | ELE/FAB/REF, same-warehouse condition locations and effective dates. Preserve independent TD review; do not invent API approval. |
| GST rules | 2-3 | Accounts maker, different TD/MD checker; effective coverage and ITC classification, historic document snapshots retained. |
| Vendor commercial verification | 0.5-1 | Already assigned to frontend developer AFTER login and required for launch. Carry forward only if unfinished; do not postpone a launch blocker to this backlog by listing it here. |
| Vendor qualifications | 2-3 | Purchase create, distinct TD verify, MD approve; date/category/vendor read-back. |

## Remaining work and estimates

| Item | Days | Concrete remaining result / boundary |
|---|---:|---|
| Governed business Configuration page | 6-10 shared foundation; 20-30 additional for a first business release; approximately 80-120 total for the full staged inventory | TD/MD maker-checker with no self-approval, audited proposals/decisions, optimistic concurrency and effective rule versions. First release should prioritise company details, approval bands, item-exception age, fiscal/period configuration and number series. Use the per-setting inventory, not a single free-text settings table. Approval snapshots on old documents remain unchanged. |
| Operational scope revoke | 3-5 backend; 0.5-1 operator wrapper/docs | Versioned authorised revocation, retained history/audit, immediate checks on already-issued tokens, company isolation and concurrency/refusal tests. A narrower scope must never be treated as revoking a broad one. UI later can share configuration governance. |
| UOM conversion approval | 2-4 | Missing decision lifecycle plus versioned approval/rejection and effective conversion reads; factor precision, dimensions, evidence and historical snapshots. Screen-only effort is not completion. No arbitrary conversion approval by SQL. |
| A1 intercompany completion | 6-10 | company_sites and customer_company_relationships governed entry/read paths plus DC-only dispatch/receipt endpoint, authority in both companies, partial receipts/returns, reconciliation and dossier links. Reuse the route, PO-publication and invoice-evidence foundations; dispatch, destination acceptance and both ledgers are still unfinished, as is the separate DC-only path. Decide ownership/custody and tax treatment before implementing that path. |
| A3 tool register and custody | 5-8 | Governed retained import and operator screens/API for 164 types, 730 purchased, 564 issued and 288 custody lines from the planned source workbook (actual workbook still required); custody acknowledgements, returns, transfer and reconciliation. Recheck actual workbook/hash and totals before import. Existing domain/draft tests are not a delivered register. |
| A4 vendor rating completion | 6-10 | Approved source rules for warranty/payment terms/documents, multi-line/rolling aggregation and governed purchasing decisions; keep manual assessment separate from measured evidence and missing data unscored. Existing source-evidence/calculation foundations remain reusable. |
| A4 LEGACY bill import | 3-5 additional | Import the actual 1,057 bills/225 vendors with source file hash/sheet/row, original dimensions/total and discrepancies. Mark LEGACY; a typed historic score is not measured ERP performance. Requires original workbook, mapping and reviewed discrepancy rules; no developer-database dump. |
| Material DC dispatch | 5-8 | Material/quantity/UOM/lot/serial dispatch, returnable custody/due/return reconciliation and evidence. Separate from chamber/machine delivery and from A1's intercompany decision. Never repurpose machine delivery endpoints for loose material. |
| A2 serialized identity change | 4-7 | TD-governed immutable old/new identity link, unique serial checks, ownership/custody/FIFO continuity, audit and reversal policy, concurrent issue protection. Domain approval policy alone is not posting support. |
| Business rejections stop using `InvalidOperationException` | 2-4 | 53 throw sites across the Purchase and Stores services raise `InvalidOperationException` for deliberate business rejections ("RFQ split exceeds approved handoff quantity.", "PR has not reached an approved PendingRFQ state."). The shared `Run()` wrapper must therefore treat that type as a 400, which means a plain `InvalidOperationException` thrown by EF for an infrastructure reason — "The instance of entity type cannot be tracked…" is the common one — is still reported to the operator as a validation error. The 23 September fix closes the observed hole by rethrowing the infrastructure subtypes (`ObjectDisposedException`, `NpgsqlOperationInProgressException`), but the category is only carried properly once these 53 sites move to the existing `Rev869BValidationException`. Do them with their tests; each site is a decision about whether 400 was in fact right. **Same class of defect as the row below: one type carrying two meanings, resolved by guessing.** One guesses from the exception type, the other from words in the message; both are correct most of the time and silently wrong the rest. |
| Replace the word-scanning conflict classifier | 1.5-3 | `StandardErrorEnvelopeMiddleware.Conflict()` still categorises a 409 by scanning `Detail` for `idempot`, then `stale` / `concurr` / `version`, before falling back to `BUSINESS_RULE_CONFLICT`. So a business-rule message that merely contains the word "version" is reported to the frontend as a concurrency conflict, and the user is told to reload when a reload cannot help. The typed marker added in the Round 4 candidate fixes this only for failures that carry it. Audit every 409 producer, give each an explicit conflict kind at the throw site, and reduce or remove the wording heuristic. Extend the ten wire-contract cases to pin each producer, including a business-rule message that deliberately contains "version" and must stay `BUSINESS_RULE_CONFLICT`. Deliberately NOT done before go-live: it touches every conflict path and the wording heuristic is wrong only occasionally and visibly, never silently. Frontend impact: `Code` values become more accurate; no new codes, so no client change is forced. See [the frontend note](conflict-envelope-frontend-note.md). |
| Suite time: about 60 toward 30 minutes | 3-5 investigation/implementation | Measure fixture setup/migrations and slow tests, then reduce duplicated work without removing business assertions, moving day-one paths out of rehearsal or running multiple disposable clusters. 30 minutes is a target, not a promised result. Keep full TRX acceptance and deliberate gates. |
| Planned Windows 11 in-place upgrade | 1-2 technical days plus approved downtime | Check TPM 2.0/Secure Boot/PC Health Check and vendor compatibility of the installed engineering software before scheduling; verified ERP and protected engineering backups first. Never clean-install. Validate SQL/SOLIDWORKS project access, retained Rockwell services, IIS/Updater, PostgreSQL, .NET, Keycloak, certificates, firewall and backups after upgrade. Preserve all protected software. |
| Commercial Windows 10 ESU if upgrade deferred | 0.5-1 technical day plus procurement | Confirm eligibility and commercial licensing with supplier; activate and verify update installation. Published per-device years are USD 61, 122, 244; purchases are cumulative and taxes/reseller terms additional. Year one starts November 2025; no partial-period purchase and no new twelve-month term from a late purchase date. Consumer offers are not assumed to license this business server. |

The Configuration total is a planning range across the full inventory, not an additive
price for every row above: shared setup/identity/period work must be estimated once.
Server settings (backup receiver, database, Keycloak, ports and certificates) stay in
restricted scripts/configuration, NEVER editable from the ERP UI. TD/MD may receive
read-only verified-backup/off-machine-copy/disk status. A stolen ERP login must not be
able to redirect backups.

## Measured starting point for suite timing

The 22 September consolidated B round-4 Debug run passed 1,029/1,029 in 61m43s
(TRX start-to-finish; build separate). Its largest individual test durations were:

| Test | Seconds |
|---|---:|
| Complete purchase flow, all three approval bands | 981.4 |
| Intercompany publication and seller disclosure | 228.5 |
| Reversed component return / no double machine charge | 255.0 |
| Ungated fresh-company day-one rehearsal | 158.7 |
| Vendor rating current QC / reversed concessions | 162.8 |

These five individual elapsed times total about 29.8 minutes. They identify where
to instrument first, not which internal operation caused the delay: TRX does not
separate migration, fixture setup, HTTP business work and assertions. Profile those
phases before choosing an optimisation. Preserve all three approval bands, the
ungated day-one flows, independent state and negative/audit/replay assertions.
Evidence: local-evidence/consolidated-B-round4/Debug/full-Debug.trx. Release and gated
acceptance are recorded separately in the consolidated B closure report.

## Inputs and acceptance references

- [Configuration inventory and individual setting estimates](../configuration-inventory-and-proposal.md).
- [Seven setup gaps and safe fallbacks](setup-screen-gaps-20260921.md).
- [D5 scope restrictions](server-identity-mapping.md).
- [Intercompany implementation boundaries](item-19-intercompany-implementation.md).
- [Vendor rating retained-source evidence](vendor-rating-source-evidence.md) and [calculation foundation](vendor-rating-calculation-foundation.md).
- [Tool register source limitations](tool-custody-foundation.md).
- [Microsoft commercial ESU terms](https://learn.microsoft.com/en-us/windows/whats-new/extended-security-updates), checked 22 September 2026: year-one USD 61, annual doubling and cumulative purchase rules.

No server change, legacy import or new feature was performed for this backlog.
