# Tracking-lite: frontend contract (R1)

Updated 28 September 2026 for UAT G-13 through G-17. Canonical committed path:
`docs/installation/R1-tracking-lite-frontend-contract.md`.

Frontend owner: ILAMPARUTHI on `feature/frontend` (Home tiles, Pending, history and E-mail log).
Backend: integrator. This revision is prepared on `fix/r1-tracking-links` for the 29 September
merge; tonight's `integration/r1` remains 5011bd2. It is not yet the live integration contract.
Mocks: `docs/installation/tracking-mocks/`; all data is invented. Do not remove these from the
backend branch. Frontend copies are optional; import fixtures only through development guards.

## Identity and navigation: G-13 through G-17

- Every pending document and history response retains `DocType`, `DocumentId`, and `Number`.
  `DocumentId` is the existing PostgreSQL UUID, serialized as a string, not a sequence number.
  Use `(DocType, DocumentId)` as identity; `Number` is display text and can contain `/`.
  Confirmation requested from the TD that "numeric ids" means these existing stable IDs;
  no new integer-key scheme or migration has been introduced.
- QC identity is the **GRN UUID**, never an inspection number or allocation UUID. A QC and GRN
  response may therefore share the UUID but differ in DocType. GRN-without-bill is a GRN.
- Summary is an aggregate, not a document list: `DocType` + `Queue` identify each tile. Its `Link`
  opens `/tracking/pending?queue=<encoded Queue>`. Fetch `/pending?queue=...` for each document's
  ID and number. `ACCESS_DENIED` tiles keep null counts and a null Link; do not navigate them.
- `Link` is retained for compatibility and corrected in the API and digest projections. It is
  a relative **app** path, not a REST endpoint. The SQL functions' old internal link strings are
  not the public contract. No merged migration is edited.
- `26d692e` derives app paths in `src/features/tracking/trackingLinks.ts` and ignores known-type
  API links. IL must keep that mapping consistent with this table. Number-keyed routes must
  apply `encodeURIComponent(Number)` **once**, including PO numbers. Never use the raw number
  as several path segments, double-decode it, or replace the UUID with the document number.

| DocType | App destination | Value used |
|---|---|---|
| PR | `/purchase/requisitions/{number}` | encoded Number |
| RFQ | `/purchase/rfqs/{number}` | encoded Number |
| QUOTATION | `/purchase/quotations` | list only; no detail route |
| COMPARISON | `/purchase/comparisons/{number}` | encoded Number |
| PO | `/purchase/purchase-orders/{number}` | encoded Number |
| GATE_ENTRY | `/stores/gate-entries/{id}` | DocumentId |
| GRN | `/stores/goods-receipts/{id}` | DocumentId |
| QC | `/qc/inspections` | queue; retain GRN DocumentId for history |
| MIR | `/stores/material-issue-requests/{id}` | DocumentId |
| VENDOR_BILL | `/accounts/vendor-bills/{id}` | DocumentId |

G-16 example: `PO/SPVT/26-27/000012` becomes
`/purchase/purchase-orders/PO%2FSPVT%2F26-27%2F000012`.
For G-14, 26d692e currently opens a QC row's GRN detail; change its QC case to the QC queue above.
There is no supported GRN query filter on that queue, so do not invent `?grn=` semantics.
History always uses `/api/v1/tracking/{DocType}/{DocumentId}/history`, independent of app routes.

Tracking-lite **reuses the existing workload queues** (the same rules, scopes and ages as the
Purchase and Stores dashboards) and adds the missing ones. You see exactly what the dashboards
already let you see.

## Document types and queues

| DocType | Queue | "Pending with" (role) | Source |
|---|---|---|---|
| PR | `pr-department-verification` | the department verifier | existing |
| PR | `pr-approval` | the approver by route | existing |
| PR | `pr-stock-check` | STORES_EXECUTIVE / STORES_MANAGER | existing |
| RFQ | `rfq-no-quotation` | PURCHASE_EXECUTIVE | existing |
| QUOTATION | `quotation-technical-verification` | the technical verifier (no role is named: any holder of Verify) | existing |
| COMPARISON | `comparison-decision` | the next approver when pending approval, else the owner (PURCHASE_EXECUTIVE) | existing |
| PO | `po-pending-approval` | the approver by the PO route | **new** |
| PO | `po-approved-unissued` | PURCHASE_MANAGER | existing |
| PO | `po-delivery-overdue` | PURCHASE_EXECUTIVE (buyer) | **new** (open PO lines past the promised date) |
| GATE_ENTRY | `gate-no-grn` | STORES_EXECUTIVE | existing |
| GRN | `grn-not-finalised` | STORES_EXECUTIVE / STORES_MANAGER | **new** |
| QC | `qc-pending` | QC_MANAGER (the QC queue is QC-Manager only); DocumentId = the GRN id | **new** (the QC queue's own rule) |
| MIR | `mir-approval` | the MIR approver | existing |
| MIR | `mir-unissued` | STORES_EXECUTIVE | existing |
| VENDOR_BILL | `bill-awaiting-decision` | ACCOUNTS_MANAGER (not the person who entered it, R3) | **new** |
| GRN | `grn-without-bill` | ACCOUNTS_ASSISTANT | **new** (GRNI rule; a GRN with a draft bill shows under the bill instead) |

## 1. GET `/api/v1/tracking/pending`

- Query: `docType`, `queue`, `overdueOnly` (bool), `mine` (bool: pending with one of my roles),
  `page`, `pageSize` (≤ 200).
- Order: overdue first, then by `WaitingSince` ascending.

```json
{
  "Total": 37, "Page": 1, "PageSize": 50, "GeneratedAt": "2026-10-15T09:00:02+05:30", "TimeZone": "Asia/Kolkata",
  "Items": [
    { "DocType": "PO", "Queue": "po-pending-approval", "DocumentId": "6f1c2a9e-2b1d-4c3e-9a51-0d6c1b7e4a10", "Number": "PO/SPVT/26-27/000012",
      "Status": "PendingApproval", "PendingWithRole": "TECHNICAL_DIRECTOR", "PendingWithRoleName": "Technical Director",
      "PendingWithEmployeeCode": null, "PendingWithEmployeeName": null,
      "WaitingSince": "2026-10-13T11:20:00+05:30", "AgeDays": 2, "OverdueAfterDays": 1, "IsOverdue": true,
      "Link": "/purchase/purchase-orders/PO%2FSPVT%2F26-27%2F000012" }
  ]
}
```

- `PendingWithEmployee*` is filled only where one person is named, for example a PR's resolved
  department verifier. Otherwise it is null and the role is shown.
- `AgeDays` counts calendar days in IST since `WaitingSince`. `IsOverdue` = `AgeDays >
  OverdueAfterDays`; the thresholds are decision 10.
- A queue the user may not see is **absent** from `/pending` and shows `ACCESS_DENIED` in `/summary`.
- **Visibility (decision 11, as built).** A queue needs View on its own page. For each row:
  - Purchase, gate entry, GRN and MIR rows follow the dashboard operational scope: department, warehouse,
    own records, and the TD/MD cross scope. A PR also shows to its requester, creator and approvers.
  - The QC and Accounts queues are company-wide for their page holders, the same as the QC queue and the
    vendor-bill screens.
  - The Pending page itself is `tracking.pending`. Every role that views one of the nine source pages
    holds it.
- **Doc types (changed 27 Sep):** the list is `PR, RFQ, QUOTATION, COMPARISON, PO, GATE_ENTRY, GRN, QC, MIR, VENDOR_BILL`. The
  quotation and comparison queues now carry their own doc type, so the history link opens the right document.

## 2. GET `/api/v1/tracking/summary`

For home tiles and menu badges:

```json
[ { "DocType": "PO", "Queue": "po-pending-approval", "Title": "POs waiting for approval",
    "State": "READY", "Count": 4, "OverdueCount": 1, "OldestAgeDays": 3,
    "Link": "/tracking/pending?queue=po-pending-approval" } ]
```

`State` is `READY` or `ACCESS_DENIED`, as on the dashboards.

## 3. GET `/api/v1/tracking/{docType}/{documentId}/history`

The timeline for one document, merged from its module's history tables:
- PR status history;
- the purchase transaction history for RFQ, quotation, comparison and PO;
- the stores document history for gate entry, GRN, QC and MIR;
- the vendor bill history.

```json
{
  "DocType": "PO", "DocumentId": "6f1c2a9e-2b1d-4c3e-9a51-0d6c1b7e4a10", "Number": "PO/SPVT/26-27/000012", "CurrentStatus": "Issued",
  "PendingWithRole": null, "WaitingSince": null, "AgeDays": null, "IsOverdue": null,
  "Link": "/purchase/purchase-orders/PO%2FSPVT%2F26-27%2F000012",
  "Events": [
    { "At": "2026-10-12T10:02:11+05:30", "Action": "Create", "FromStatus": null, "ToStatus": "Draft",
      "EmployeeCode": "SESS-15", "EmployeeName": "…", "RoleCode": "PURCHASE_MANAGER", "Remarks": "Created from approved comparison" },
    { "At": "…", "Action": "Approve", "FromStatus": "PendingApproval", "ToStatus": "Approved", "EmployeeCode": "SESS-01", "EmployeeName": "…", "RoleCode": "TECHNICAL_DIRECTOR", "Remarks": "…" }
  ]
}
```

- For a PO, `Events` include the RFQ, quotation and comparison steps that led to it, marked by
  `"Stage": "RFQ" | "QUOTATION" | "COMPARISON" | "PO"`, so one panel shows the chain.
- **What each history shows:**
  - GRN and QC: the gate entry and GRN steps. QC adds `QC_STARTED` and `QC_FINALIZED`, with the decision as `ToStatus`.
  - Each event also carries `LoginId`. PR status steps have only the login; PR approval steps name the employee.
  - An accountant may open a GRN's history from `grn-without-bill`.
- 404 if the document is outside your scope (same rule as its detail page).

## Screens (R1)

1. **Pending page** `/tracking/pending` (IL):
   - filters for doc type, queue, "mine" and "overdue only";
   - overdue rows in red;
   - age in days;
   - a "pending with" column;
   - the number links to the detail page;
   - CSV export (client side);
   - mobile-friendly.
2. **Home tiles and menu badges** from `/summary` (IL): overdue count in red.
3. **History panel** on available detail pages of the ten doc types: a vertical timeline, newest
   first, with the pending-with box on top. IL owns these panels; MAGESHWARI is off ERP.
4. **E-mail log** `/admin/email` (IL), for the TD and IT_MANAGER only:
   - the list from `GET /api/v1/email/outbox`, with a status filter;
   - a Retry button for the TD;
   - a "send test" dialog.
   - The mock-only handoff is in `email-log-mocks/README.md`. Backend ownership remains undecided;
     no email-lite backend is implemented by this change.

## Errors

The standard envelope applies: 400 field errors, 403, 404, and 409 with the server sentence shown
as it is.
