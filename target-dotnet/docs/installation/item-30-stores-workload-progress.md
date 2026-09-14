# Item 30 Stores document workload

Status: implemented and verified across three targeted facts in Release and Debug. No complete Stores dashboard is claimed.

GET /api/v1/dashboards/stores/workload returns three count/age tiles and their source rows. Filters: queue (gate-no-grn, mir-approval, mir-unissued), documentId, page and pageSize (1 to 1000, default 100). Filters select the detail list; tile totals remain the full authorized scope.

Gate entries awaiting GRN counts finalized normal, unreversed gate entries with no normal GRN at all. Creating a draft GRN removes that gate from this queue. Age starts at arrival. Vendor and document identifiers support follow-up.

MIRs awaiting approval counts submitted requests. Current commands accept an independent Stores Manager or Production Manager but do not populate a named approver snapshot. EligibleApprovalRoles therefore identifies those two roles; AssignedApproverEmployeeId remains null with an explicit ResponsibilityIssue. This is not yet a queue grouped by an assigned employee.

Approved MIRs awaiting issue includes APPROVED and PARTIALLY_FULFILLED headers with remaining request lines. Remaining quantity follows the issue service: requested base quantity minus all issue-line quantities for that request line. Accepted returns do not reopen a fulfilled MIR. Age starts at approval, with creation as fallback if historical approval time is unavailable. Excess approvals or stock availability can still block issue; a place in this queue is not an assertion of readiness.

The response has no financial values. PendingLineCount is a number of lines, never a sum of incompatible item quantities. Each row provides a real gate/MIR detail API path; that path keeps its own permission checks.

The dedicated dashboards.stores-workload page derives read grants for Stores Assistant, Stores Executive, Stores Manager, TD and MD from existing GRN/MIR view permissions. Each tile also checks its source page. One private SQL statement verifies live company membership, employee status, role assignments/activation, page permission and operational scope. A request with an unspecified warehouse cannot satisfy a warehouse-restricted scope; explicit director cross-scope still requires a stored privileged grant. No command or financial permission is added.

Global denial returns 403 DASHBOARD_ACCESS_DENIED. An inaccessible source tile returns ACCESS_DENIED with null count and no rows. A permitted empty queue returns zero. Invalid queue/page returns 400 DASHBOARD_REQUEST_INVALID. The frontend must preserve these distinctions.

Migration 20260914060000 adds the private function and guarded page grants. Installer provision/status checks its owner, fixed search path and narrow runtime EXECUTE permission. Runtime does not receive direct financial-table reads. Rollback refuses altered runtime permissions.

Verification observes gate finalization and GRN draft creation across all three approval bands, then MIR submission, approval and issue in the full PR-to-Actual-BOM witness. It checks one SQL call, unchanged business row counts, source/global permission refusal, scoped filtering, role deactivation, company mismatch, paging/drill filters, restricted-runtime HTTP and migration install/reprovision/down/reapply. The partial-issue runtime fact passed in Release: a 0.02-unit request remains open after 0.01 is issued and closes after the other 0.01. Replay changes no rows.

QC hold, rejection/returnable-DC, put-away, per-engineer custody/value, stock valuation and counting are separate remaining components. Source findings are retained in local-evidence/item30/workload-source-review.md. No frontend is implemented.

The first flow selected a TD whose stored scope did not cover the gate; it correctly returned zero. The positive witness now uses the actual Stores Executive (SESS-35), with a separate SESS-16 read check, for gates and the Stores Manager (SESS-41) for MIRs. The disposable Stores Manager has explicit IT and Production department reporting scopes, without privileged cross-scope. The production predicate is unchanged. The manager's existing GRN page denial is asserted as ACCESS_DENIED/null, not broadened to make the test pass.

Release evidence so far: migration passed in 1m12.643s (item30-workload-first-release.trx). Partial issue passed in 3m13.095s (item30-workload-runtime-release.trx). That run also retained the manager-GRN permission expectation failure in the earlier main-flow test. The original outside-scope and manager-page-denied artifacts are preserved. Initial build was clean in 4m33.11s; subsequent test builds were clean.

Partial-issue read snapshots: before issue, 3 issues / 3 FIFO consumptions / 172 audits / 131 requests and receipts / 28 movements. After first issue: 4 / 4 / 173 / 132 / 30. After completion: 5 / 5 / 174 / 133 / 32. Each read's before/after snapshot is identical, and final issue replay retains the final counts. Artifact: stores-workload-partial-verified-release.json.

The corrected nine-stage Release flow passed in 4m08.663s (item30-workload-correct-readers-release.trx). All nine read snapshots are unchanged; gates are observed with SESS-35 and a separate SESS-16 check, MIRs with SESS-41. The manager's inaccessible gate tile is explicitly asserted. stores-workload-states-verified-release.json preserves these states. The final test rebuild was clean in 27.23s. Debug build is also clean in 4m15.19s; all three matching tests passed with zero failures or skips in 7m49s (item30-workload-debug.trx). Nine flow snapshots and three partial-issue snapshots match Release exactly and every read leaves counts unchanged. Verified Debug JSON artifacts are retained alongside Release. These are three distinct Release passes across the retained runs, not a full suite.
