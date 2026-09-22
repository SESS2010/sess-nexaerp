# Item 29 — purchase workload projection

Status: workload backend verified in Release and Debug. Each configuration passed two targeted tests with zero failures/skips. This component covers seven workload queues; the remaining purchase dashboard sections and frontend are outstanding.

Release build: zero warnings/errors, 3m52.71s. Release tests: 2 passed, approximately 5m (flow 3m44.492s, migration 1m16.049s).
Debug build: zero warnings/errors, 4m06.36s. Debug tests: 2 passed, 5m22s.
These are targeted checks, not a new full-suite result.

## Contract

GET /api/v1/dashboards/purchase/workload accepts queue, approvalRoute, page
(default 1) and pageSize (default 100, maximum 1000). Omit queue to see all
visible workload rows. approvalRoute is permitted only with pr-approval.
Each tile's key is its queue filter; each approval band's stored route is
its approvalRoute filter. DetailPath on each row points to the existing
document API, with the document number escaped.

The response contains company, generation time, configured company timezone,
all seven tiles, selected filters, total matching rows and a paged row list.
Tile counts and totals cover all visible documents, not just the current page.

| Queue | Counted document |
|---|---|
| pr-department-verification | Active Submitted PR |
| pr-approval | Active DepartmentVerified or PendingApproval PR |
| pr-stock-check | Active StockCheckPending PR |
| rfq-no-quotation | Issued active RFQ without a current usable quotation |
| quotation-technical-verification | Current Submitted quotation with an unverified line |
| comparison-decision | Draft, RevisionRequested or PendingApproval comparison |
| po-approved-unissued | Current Approved PO that has not been issued |

An RFQ is counted once even if two vendors were invited. Its row lists invited
vendors. Withdrawn, rejected, superseded and draft quotations do not satisfy
the quotation requirement. A quotation is counted once; PendingLineCount is
a separate measure rather than another quotation count.

PR approval tiles contain counts, oldest age and amounts per stored approval
route. Next approver details come from the saved workflow's next step.
Missing, duplicate or invalid next-step identity produces an administrator
responsibility issue; the projection does not invent an approver.

Age is measured in calendar days in the company's reporting timezone.
PR, comparison and PO age uses the latest actual transition into the current
state, with creation time as the legacy fallback. RFQ uses issue time and
quotation uses submission time.

## Values and visibility

Amounts remain grouped by native currency. There is no exchange-rate lookup,
conversion or cross-currency sum. PR estimates use the existing INR approval
basis. Commercial rows are redacted when the relevant commercial permission
is absent. View-only employee permission never supplies commercial permission.

An unrecommended comparison has no selected payable value. Its row Value is
null; UnvaluedDocumentCount exposes how many documents are excluded from the
amount. The frontend must not describe a partial sum as a complete valuation.

A source tile without view permission has State ACCESS_DENIED and Count null,
rather than zero. A permitted empty queue has State READY and Count zero.
OldestAgeDays is null for an empty queue. CommercialValuesVisible distinguishes
redaction from an empty amount list.

The intended dashboard roles are Purchase Manager, Technical Director and
Managing Director. The new dashboards.purchase page initially inherits view
and commercial visibility from each role's existing purchase.po permissions.
Each source page's view permission is checked again. Quotations, comparisons
and POs also require the source commercial grant. PR estimates follow the
existing PR read contract, with the dashboard commercial grant still required.

The SQL checks the current company membership, active employee, effective role
assignments and company-role activation. Operational scope checks intersect
department, warehouse, rack/bin and own-record restrictions. A director needs
an explicit privileged cross-scope grant for that bypass. PR visibility also
retains its requester, creator and named-approver read paths.

## Database boundary

One restricted runtime SQL command produces the access decision, summaries,
bands and paged details from one snapshot. Identity and assignments are
supplied by the resolved current user, not query-string parameters.

Migration 20260914010000 installs the new page and restricted SECURITY DEFINER
reader with a fixed search path. Its SQL is an embedded immutable migration
resource. Runtime receives EXECUTE on the reader; direct financial-table read
permissions are not added. The Installer reconciles and checks its authority.
Rollback refuses changed runtime page permissions and does not delete business
records.

## Witness and current limit

The actual three-band purchase flow now offers eight read-only checkpoints
per band: submitted PR, pending PR approval, pending stock check, invited RFQ
before quotations, two unverified quotations, draft comparison, recommended
comparison and approved unissued PO. Expected observations are 24.

The test asserts source document identity, exact counts, stored approvers,
amount reconciliation where visible, pagination and one Npgsql command per
projection. Allowed reads must leave audit, request, receipt and movement
counts unchanged. Negative permission and scope checks use rollback-only transactions over
disposable metadata. Scope replacements obey the database version guard and
the reader runs under the restricted runtime role; rollback restores the
original configuration. These probes do not manufacture financial rows or
change site permissions. HTTP checks exercise the registered endpoint.

Release and Debug verification passed. No production dataset, frontend tile, spending projection,
vendor rating or foreign procurement valuation is claimed by this component.
Debt sections remain subsequent work. The separate spending component and its verification are recorded in [purchase spending progress](item-29-purchase-spending-progress.md). Billed-before-received and
vendor quality need their already-recorded workflow prerequisites.

## Runtime verification notes

The first three Release flow attempts were retained and failed in the test
setup or expected values: first, a PM-only assignment list omitted PRIYA's
Purchase Executive and Stores Executive assignments (1m45s); second, direct
scope edits were refused by the existing immutable-version guard (1m45s);
third, the expected PR band name used MANAGER instead of the actual saved
DEPARTMENT_ONLY value (1m47s).

The projection returned ACCESS_DENIED for missing source-page permission and
then returned the submitted PR once the witness supplied its actual assignments.
The scope probes now close open versions, insert a temporary replacement and
roll back the transaction. They execute the projection with current_user set
to nexa_erp_runtime. Scope guards remain enabled. The third run passed wrong
department, own-record, live role activation, commercial redaction and source
page revocation checks before reaching the unrelated route-name assertion.

These first three runs were not complete runtime passes.

The fourth Release flow passed (1 passed, 0 failed, 0 skipped, 4m29s), with
24 observations and all access probes. Review of its saved rows then found a
contract gap: PR estimates were redacted because the legacy PR page's commercial
flag is unset, although its existing authorized detail response always includes
EstimatedTotal. The old evidence is retained as
purchase-workload-states-pr-redacted-release.json and
purchase-workload-access-fourth-release.json.

The query now follows that existing PR read contract while still requiring the
dashboard commercial grant. Other commercial sources retain their own source
commercial checks. Tests now require positive estimate values in all three PR
bands and require commercial visibility for the quotation, comparison and PO
cases. The commercial-redaction probe revokes the dashboard commercial grant.
The initial Debug build passed (zero warnings/errors, 4m34s) before this change;
it is not verification of the corrected query. The corrected Release build passed with zero warnings/errors (3m52.71s). Both corrected Release facts passed, zero failed/skipped, in approximately five minutes: migration/authority reconciliation and the actual three-band flow. The saved evidence contains all 24 observations, including INR PR estimates 4,999.99, 5,000 and 100,000.01. Access probes left 18 audits, 9 requests, 9 receipts and zero movements unchanged. Verified Release artifacts are purchase-workload-states-verified-release.json and purchase-workload-access-verified-release.json. The corrected Debug build and the same two facts also passed. Verified Debug artifacts are purchase-workload-states-verified-debug.json and purchase-workload-access-verified-debug.json. Both configurations recorded 24 observations and unchanged business counts for the access probes.
