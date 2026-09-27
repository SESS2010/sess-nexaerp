# Tracking-lite mocks (R1)

For the Pending page, the home tiles and the history panels. The shapes are what the API returns today
(`GET /api/v1/tracking/pending`, `/summary`, `/{docType}/{documentId}/history`). The data is invented:
the numbers and names are placeholders, not real documents or employees.

| File | Endpoint |
|---|---|
| `pending.json` | `GET /api/v1/tracking/pending?page=1&pageSize=50` |
| `summary.json` | `GET /api/v1/tracking/summary` |
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
