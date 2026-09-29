# Tracking-lite mocks (R1)

For the Pending page, the home tiles and the history panels. CONFIRMED: the shapes describe the G-13 through G-17 navigation contract; older deployed backends may return legacy links
(`GET /api/v1/tracking/pending`, `/summary`, `/{docType}/{documentId}/history`). The data is invented:
the numbers and names are placeholders, not real documents or employees.

| File | Endpoint |
|---|---|
| `pending.json` | `GET /api/v1/tracking/pending?page=1&pageSize=50` |
| `summary.json` | `GET /api/v1/tracking/summary`, same authorized data as pending.json |
| `summary-denied.json` | separate access-denied tile scenario; not paired with pending.json |
| `history-po.json` | `GET /api/v1/tracking/PO/{id}/history` (RFQ, quotation and comparison steps included) |
| `history-qc.json` | `GET /api/v1/tracking/QC/{grnId}/history` (gate entry, GRN and QC decisions) |

## Rules the screens rely on

- **Doc types:** `PR`, `RFQ`, `QUOTATION`, `COMPARISON`, `PO`, `GATE_ENTRY`, `GRN`, `QC`, `MIR`,
  `VENDOR_BILL`. The history URL takes the row's `DocType` and `DocumentId` as they are.
- **QC rows:** a `qc-pending` row's `DocumentId` is the GRN id.
- **Receipts without a bill:** a `grn-without-bill` row is a `GRN` row.
- **Order:** rows come overdue first, then oldest first.
- **Age:** `AgeDays` is calendar days in IST. `IsOverdue` means `AgeDays > OverdueAfterDays`.
- **Pending with:** `PendingWithEmployee*` is filled only when one person is named. That happens for:
  - a PR's department verifier;
  - the next approver of a PR, comparison or PO;
  - the buyer of a late PO.
  Otherwise show the role.
- **`mine=true`:** keeps the rows named to you, and the rows waiting on one of your roles when no one is
  named. A vendor bill never counts as "mine" for the person who entered it.
- **Tiles:** a tile with `"State": "ACCESS_DENIED"` has null counts. Hide it or grey it out.
- **Access denied:** the whole API returns 403 `TRACKING_ACCESS_DENIED` when the user has no Pending page.
- **History 404:** the history returns 404 when the document is outside your scope.
- **Timeline order:** history events are newest first. PR events carry `LoginId`, and `EmployeeCode`
  only for approval steps.

## G-13 through G-17 handoff

Canonical contract: `../R1-tracking-lite-frontend-contract.md`. ILAMPARUTHI owns Home tiles,
Pending, history and E-mail log; MAGESHWARI is off ERP.

`pending.json` covers all ten DocTypes with nonempty stable UUID DocumentId and Number. Summary
contains aggregates and queue links, not invented document IDs; drill down through /pending.
PO numbers contain slashes deliberately: links encode them as %2F. GRN links use goods-receipts;
QC links open /qc/inspections, retaining the GRN UUID for history; quotations open the list.
History responses include the same corrected Link. These are compatibility links: frontend
trackingLinks.ts can construct them from DocType, DocumentId and Number using the contract table.
Only READY tiles navigate. Do not import fixtures into production bundles.

CONFIRMED (TD approval, 29 September): document identity remains the existing UUID plus display number; summary remains aggregate queue links. No numeric IDs are introduced.
