# Dashboard frontend contract — 22 September 2026

Build two pages: **Purchase** and **Stores**, each with its own permission-aware sections. TD/MD may open both. A single role-switched page would hide useful cross-functional access. Work on `feature/dashboards` with the six attached synthetic mock bodies until production login lands (target about 28 September).

Every call MUST use the shared API client being consolidated for login. No `fetch` in pages, and no independently written client in `src/api`. Do not copy PurchaseDocumentRegister or QuotationPage. Mock at that shared-client boundary, never by silently falling back from a failed live request.

## Shared request and response rules

All six endpoints are authenticated GETs. Send exactly one `X-NexaERP-Company` header: `SESS_PVT_LTD` or `SESS_PROPRIETORSHIP`, taken from the selected company in the shared login context. The current frontend does not send it; login work adds it. The header selects context, not authority: the authenticated issuer/subject must map to an active employee with access to that company. Never accept an employee identity from a page or query parameter.

JSON property names are PascalCase, including nested objects and errors. Query parameters are camelCase. Every endpoint accepts `page` (integer, default 1, minimum 1) and `pageSize` (integer, default 100, range 1–1000). Unknown/null monetary values are not zero. Dates/times in the bodies are server values; `TimeZone` defines calendar boundaries and aging (server default UTC; configure the SESS reporting timezone deliberately). No endpoint accepts `fromDate` or `toDate`.

**Detail filters narrow Rows and TotalRows only; they do not narrow overview tiles or aggregate groups.** Label filtered detail separately. Discard stale responses after a company change. Decimal JSON numbers are money/quantity, not formatted strings. Keep currencies separate; there is no exchange-rate conversion.

Purchase endpoints require an active PURCHASE_MANAGER, TECHNICAL_DIRECTOR or MANAGING_DIRECTOR role in the selected company, plus applicable page and operational scope permissions. PURCHASE_EXECUTIVE alone and Accounts/CFO alone do not qualify. Stores endpoints require STORES_ASSISTANT, STORES_EXECUTIVE, STORES_MANAGER, TECHNICAL_DIRECTOR or MANAGING_DIRECTOR plus permissions/scope. PRODUCTION_MANAGER alone and QC alone do not qualify. An employee view override cannot create commercial permission or bypass the role family. Department, warehouse, bin and own-record restrictions still apply; TD cross-scope access requires its explicit privileged scope.

Examples below are complete retained synthetic HTTP witness bodies (September 2026), validated against current application DTO fields and nullability. Their identifiers are mock data, not production lookup IDs. An optional C# `?` in the field tables means JSON null is allowed. DateOnly is YYYY-MM-DD; DateTimeOffset is an ISO timestamp; Guid is a UUID string; IReadOnlyList is a JSON array. Field tables include filter records as well as response records so every nested field is defined.

## Empty and error rendering

An authorized empty result is HTTP 200 with Rows=[], TotalRows=0, and empty/zero permitted aggregates as returned. Keep all required fields. Workload ACCESS_DENIED cards have withheld/null counts, not a zero-work claim. Do not show withheld money as zero. Open-order incomplete/unknown delivery totals remain null even when reliable detail rows exist. Currency arrays can be empty. Loading, authorized empty, partial/incomplete source, permission denial, and request failure are distinct states.

Errors use application/problem+json and the seven fields Type, Title, Status, Code, Detail, TraceId and Errors; Errors is an object. The complete endpoint-specific 403 examples below assume a valid mapped employee/company who lacks the required dashboard role. TraceId varies per request. Earlier identity checks may instead return EMPLOYEE_ACCESS_NOT_CONFIGURED or MFA_REQUIRED; unauthenticated access returns 401 AUTHENTICATION_REQUIRED. Invalid semantic filters return 400 DASHBOARD_REQUEST_INVALID (Title: Validation failed; Type: https://api.sess.example/problems/dashboard-request-invalid). Malformed typed query binding can return VALIDATION_FAILED before the endpoint. Unexpected failures return 500 INTERNAL_ERROR; do not expose exception text. Preserve TraceId for support. No automatic permission or authentication bypass.

Obligation integrity failures return HTTP 409, Type https://api.sess.example/problems/dashboard-source-inconsistent, Title `Report source needs administrator action`, Code DASHBOARD_SOURCE_INCONSISTENT, Detail `Receipt or advance balances are inconsistent. Ask the administrator to reconcile the source documents.`, Errors {}, and the additional field AdministratorActionRequired=true. Stop presenting the figures as reconciled; request administrator investigation.

## GET /api/v1/dashboards/purchase/workload

Permission: dashboards.purchase:view; each tile also requires its source page view. Commercial grants separately govern amounts.

Parameters beyond paging: queue omitted/null or one of pr-department-verification, pr-approval, pr-stock-check, rfq-no-quotation, quotation-technical-verification, comparison-decision, po-approved-unissued. approvalRoute is optional nonempty text up to 80 characters, permitted only with queue=pr-approval; use returned ApprovalBands route values. Current backlog, no date range. Sort: waiting timestamp, queue, document ID.

Seven tiles count documents; RFQ counts documents, not outstanding vendors, and has no amount. PendingLineCount is a separate quotation-line measure. Unrecommended comparisons may have null value. Next approver derives from the saved workflow step, not a fresh threshold calculation; display ResponsibilityIssue for missing/ambiguous responsibility.

Money: PR uses saved EstimatedTotal in INR; this is not a uniform ex-tax cost measure. Quotation/comparison/PO values are saved TotalPayableValue, including embedded GST and saved commercial terms. Never label this whole dashboard ex-tax or actual component cost.

### Complete HTTP 200 mock

[Standalone mock](dashboard-mocks/purchase-workload.json)

```json
{
  "CompanyCode": "SESS_PVT_LTD",
  "GeneratedAt": "2026-09-14T03:03:38.004948+05:30",
  "TimeZone": "Asia/Kolkata",
  "Tiles": [
    {
      "Key": "comparison-decision",
      "Title": "Comparisons awaiting recommendation or approval",
      "State": "READY",
      "Count": 0,
      "OldestAgeDays": null,
      "CommercialValuesVisible": true,
      "Amounts": [],
      "UnvaluedDocumentCount": 0,
      "ApprovalBands": [],
      "Coverage": "Unrecommended comparisons have no selected commercial value."
    },
    {
      "Key": "po-approved-unissued",
      "Title": "POs approved but not issued",
      "State": "READY",
      "Count": 0,
      "OldestAgeDays": null,
      "CommercialValuesVisible": true,
      "Amounts": [],
      "UnvaluedDocumentCount": 0,
      "ApprovalBands": [],
      "Coverage": "Current approved, unissued revisions; native-currency amounts."
    },
    {
      "Key": "pr-approval",
      "Title": "Requisitions awaiting approval",
      "State": "READY",
      "Count": 1,
      "OldestAgeDays": 0,
      "CommercialValuesVisible": true,
      "Amounts": [
        {
          "Currency": "INR",
          "Amount": 4999.99
        }
      ],
      "UnvaluedDocumentCount": 0,
      "ApprovalBands": [
        {
          "ApprovalRoute": "DEPARTMENT_ONLY",
          "Count": 1,
          "OldestAgeDays": 0,
          "Amounts": [
            {
              "Currency": "INR",
              "Amount": 4999.99
            }
          ]
        }
      ],
      "Coverage": "Stored approval route and next snapshot step; estimated amounts are INR."
    },
    {
      "Key": "pr-department-verification",
      "Title": "Requisitions awaiting department verification",
      "State": "READY",
      "Count": 0,
      "OldestAgeDays": null,
      "CommercialValuesVisible": true,
      "Amounts": [],
      "UnvaluedDocumentCount": 0,
      "ApprovalBands": [],
      "Coverage": "Requisition estimated amounts are INR."
    },
    {
      "Key": "pr-stock-check",
      "Title": "Requisitions awaiting stock check",
      "State": "READY",
      "Count": 0,
      "OldestAgeDays": null,
      "CommercialValuesVisible": true,
      "Amounts": [],
      "UnvaluedDocumentCount": 0,
      "ApprovalBands": [],
      "Coverage": "Final approval has completed; stock check is pending."
    },
    {
      "Key": "quotation-technical-verification",
      "Title": "Quotations awaiting technical verification",
      "State": "READY",
      "Count": 0,
      "OldestAgeDays": null,
      "CommercialValuesVisible": true,
      "Amounts": [],
      "UnvaluedDocumentCount": 0,
      "ApprovalBands": [],
      "Coverage": "One quotation per count; pending lines are separate."
    },
    {
      "Key": "rfq-no-quotation",
      "Title": "RFQs issued with no quotation",
      "State": "READY",
      "Count": 0,
      "OldestAgeDays": null,
      "CommercialValuesVisible": true,
      "Amounts": [],
      "UnvaluedDocumentCount": null,
      "ApprovalBands": [],
      "Coverage": "One RFQ per count, with no current non-draft, non-withdrawn or non-rejected quotation."
    }
  ],
  "Queue": "pr-approval",
  "ApprovalRoute": null,
  "Page": 1,
  "PageSize": 1000,
  "TotalRows": 1,
  "Rows": [
    {
      "Queue": "pr-approval",
      "DocumentId": "3cb6ba6f-2660-4ce7-8f4d-978877c45a7f",
      "DocumentType": "PR",
      "DocumentNumber": "PR-2026-27-000001",
      "Status": "PendingApproval",
      "WaitingSince": "2026-09-14T03:03:37.352682+05:30",
      "AgeDays": 0,
      "ApprovalRoute": "DEPARTMENT_ONLY",
      "NextApproverEmployeeId": "5fdedc5a-1740-164c-04e9-3c6f2db5417c",
      "NextApproverEmployeeCode": "SESS-14",
      "NextApproverRole": "ACCOUNTS_MANAGER",
      "ResponsibilityIssue": null,
      "Currency": "INR",
      "Value": 4999.99,
      "Vendors": [],
      "PendingLineCount": null,
      "DetailPath": "/api/v1/purchase/requisitions/PR-2026-27-000001"
    }
  ]
}
```

### Every field

#### PurchaseWorkloadRequest

| Field | JSON/C# type | Meaning |
|---|---|---|
| Queue | string? | Selected queue, or null for all permitted queues. In a row, identifies its source queue. |
| ApprovalRoute | string? | Stored approval route name, not a newly computed threshold. Null if not applicable. |
| Page | int | One-based requested detail page. |
| PageSize | int | Requested maximum detail rows per page, 1 to 1000. |

#### DashboardCurrencyAmount

| Field | JSON/C# type | Meaning |
|---|---|---|
| Currency | string | Native three-letter currency; purchase workload can redact it to null. |
| Amount | decimal | Amount in the named currency. Spending: MaterialValue + AllocatedCharges; workload: queue aggregate. |

#### PurchaseWorkloadBand

| Field | JSON/C# type | Meaning |
|---|---|---|
| ApprovalRoute | string | Stored approval route name, not a newly computed threshold. Null if not applicable. |
| Count | long | Distinct documents in this queue. Null on a denied workload card. |
| OldestAgeDays | int? | Largest whole local-calendar age among included records; null when none or unavailable. |
| Amounts | IReadOnlyList<DashboardCurrencyAmount> | Separate native-currency aggregates. An empty array is not a fabricated INR zero. |

#### PurchaseWorkloadTile

| Field | JSON/C# type | Meaning |
|---|---|---|
| Key | string | Stable card/period key used for selection; do not match translated titles. |
| Title | string | Server-provided human-readable card heading. |
| State | string | READY or ACCESS_DENIED for this card. Denied is not an empty queue. |
| Count | long? | Distinct documents in this queue. Null on a denied workload card. |
| OldestAgeDays | int? | Largest whole local-calendar age among included records; null when none or unavailable. |
| CommercialValuesVisible | bool | Whether this workload card may display its monetary values. |
| Amounts | IReadOnlyList<DashboardCurrencyAmount> | Separate native-currency aggregates. An empty array is not a fabricated INR zero. |
| UnvaluedDocumentCount | long? | Visible documents with no selected value yet; null for hidden commercial values, denied card, or RFQ no-quotation. |
| ApprovalBands | IReadOnlyList<PurchaseWorkloadBand> | PR-approval counts grouped by stored route; empty for other queues or no matching PRs. |
| Coverage | string? | Server explanation of what this card includes and excludes. |

#### PurchaseWorkloadRow

| Field | JSON/C# type | Meaning |
|---|---|---|
| Queue | string | Selected queue, or null for all permitted queues. In a row, identifies its source queue. |
| DocumentId | Guid | Identifier of this source document; its kind is given by the queue/document type. |
| DocumentType | string | Source kind: purchase PR/RFQ/QUOTATION/COMPARISON/PO, or Stores GATE_ENTRY/MIR. |
| DocumentNumber | string | Human-readable number of the source document. |
| Status | string | Recorded workflow status; preserve the returned spelling/case. |
| WaitingSince | DateTimeOffset | Timestamp from which the current queue age is measured. |
| AgeDays | int | Whole company-local calendar days since the row age origin, never negative. |
| ApprovalRoute | string? | Stored approval route name, not a newly computed threshold. Null if not applicable. |
| NextApproverEmployeeId | Guid? | Named employee from the saved next approval step, or null. |
| NextApproverEmployeeCode | string? | Human-readable employee code from that saved step, or null. |
| NextApproverRole | string? | Role from the saved next approval step; not a frontend guess. |
| ResponsibilityIssue | string? | Reason a responsible employee cannot be identified; show it rather than inventing an assignee. |
| Currency | string? | Native three-letter currency; purchase workload can redact it to null. |
| Value | decimal? | Monetary value on the endpoint/queue basis documented above, in Currency; not necessarily ex-tax cost. |
| Vendors | IReadOnlyList<string> | Workload: vendor names. Obligations page: grouped vendor balances described below. |
| PendingLineCount | long? | Number of still-pending document lines, not quantity; purchase workload may return null. |
| DetailPath | string | API resource path, NOT automatically a browser route. Use the mapping below. |

#### PurchaseWorkloadPage

| Field | JSON/C# type | Meaning |
|---|---|---|
| CompanyCode | string | Selected ERP company code; verify it still matches the active company before rendering. |
| GeneratedAt | DateTimeOffset | Server timestamp for this projection. Show last refreshed time; not a document transaction date. |
| TimeZone | string | Server-configured company reporting timezone used for calendar dates and ages. |
| Tiles | IReadOnlyList<PurchaseWorkloadTile> | All overview cards for this endpoint; scope-filtered but not narrowed by detail filters. |
| Queue | string? | Selected queue, or null for all permitted queues. In a row, identifies its source queue. |
| ApprovalRoute | string? | Stored approval route name, not a newly computed threshold. Null if not applicable. |
| Page | int | One-based requested detail page. |
| PageSize | int | Requested maximum detail rows per page, 1 to 1000. |
| TotalRows | long | Number of matching detail rows before pagination; not the number of cards or distinct documents. |
| Rows | IReadOnlyList<PurchaseWorkloadRow> | This page of matching detail rows. Empty array is a valid result. |

### Exact denied response shape

Example caller: otherwise mapped/scoped PURCHASE_EXECUTIVE alone. HTTP 403.

```json
{
  "Type": "https://api.sess.example/problems/dashboard-access-denied",
  "Title": "Permission denied",
  "Status": 403,
  "Code": "DASHBOARD_ACCESS_DENIED",
  "Detail": "The purchase dashboard is not permitted for your employee and selected company.",
  "TraceId": "mock-trace-403",
  "Errors": {}
}
```

## GET /api/v1/dashboards/purchase/open-orders

Permission: dashboards.purchase-open-orders:view AND commercial permission, within the Purchase role family.

Parameters beyond paging: vendorId (optional UUID), currency (optional three ASCII letters, trimmed/uppercased), rootPurchaseOrderId (optional UUID), overdueOnly (boolean, default false). Current issued commitments, no date range. Sort: first issue timestamp, PO, line.

Latest issued revision supplies lines; CurrentRevisionNumber/Status may describe a later unissued amendment. Finalized unreversed receipts across the root/comparison-line lineage reduce remaining quantities. Value is remaining quantity multiplied by line TotalPayableValue / ordered quantity: **gross payable commitment including embedded taxes/charges**, not recoverable-GST-excluded cost. Age starts at the first issue across the root.

A quoted delivery date is confirmed only if the comparison delivery snapshot and PO terms support it. Otherwise CommittedDeliveryDate and DaysLate are null and DeliveryState is CONFIRMATION_REQUIRED; other states are OVERDUE and WITHIN_COMMITMENT. DeliveryComplete=false means overdue totals can be null, not zero. overdueOnly excludes unknown commitments. SourceIssues may contain CURRENT_REVISION_UNAVAILABLE, CANCELLED_UNISSUED_AMENDMENT, LINE_PROVENANCE_INCONSISTENT or RECEIPT_QUANTITY_INCONSISTENT. With source issues, Complete=false and overall OpenPoCount/Amounts/OldestAgeDays are null; reliable detail may remain while bad roots are excluded.

### Complete HTTP 200 mock

[Standalone mock](dashboard-mocks/purchase-open-orders.json)

```json
{
  "CompanyCode": "SESS_PVT_LTD",
  "GeneratedAt": "2026-09-14T06:35:50.756068+05:30",
  "TimeZone": "Asia/Kolkata",
  "Basis": "Remaining quantity at the latest issued PO line payable per unit, by native currency. Age starts at first issue. Unconfirmed delivery dates do not produce overdue totals.",
  "Complete": true,
  "OpenPoCount": 1,
  "OldestAgeDays": 0,
  "DeliveryComplete": true,
  "OverduePoCount": 0,
  "DeliveryDateUnconfirmedPoCount": 0,
  "SourceIssues": [],
  "Amounts": [
    {
      "Currency": "INR",
      "PoCount": 1,
      "Value": 4720.0,
      "OverduePoCount": 0,
      "OverdueValue": 0
    }
  ],
  "Filters": {
    "VendorId": null,
    "Currency": null,
    "RootPurchaseOrderId": null,
    "OverdueOnly": false,
    "Page": 1,
    "PageSize": 100
  },
  "TotalRows": 1,
  "Rows": [
    {
      "PurchaseOrderId": "d1ad9d2d-ad77-49e4-ac45-308eb164674a",
      "RootPurchaseOrderId": "29d76093-a450-445e-a415-2ea3362e498f",
      "PoNumber": "PO-26-27-000011",
      "RevisionNumber": 1,
      "CurrentRevisionNumber": 2,
      "CurrentStatus": "Approved",
      "FirstIssuedAt": "2026-09-14T06:35:46.704601+05:30",
      "IssuedAt": "2026-09-14T06:35:46.704601+05:30",
      "AgeDays": 0,
      "VendorId": "71000000-0000-0000-0005-000000000001",
      "VendorCode": "TRIAL-VEN-001",
      "VendorName": "TRIAL Alpine Cooling Supplies",
      "LineId": "d2367f86-1587-4bac-b3ae-c3f5539cc240",
      "ItemId": "71000000-0000-0000-0006-000000000001",
      "ItemCode": "TRIAL-ITEM-001",
      "ItemName": "TRIAL Copper control cable",
      "Uom": "TRIAL-MTR",
      "OrderedQuantity": 1.0,
      "ReceivedQuantity": 0,
      "RemainingQuantity": 1.0,
      "Currency": "INR",
      "Value": 4720.0,
      "QuotedDeliveryDate": "2026-10-14",
      "CommittedDeliveryDate": "2026-10-14",
      "DeliveryTerms": "Delivered to trial warehouse",
      "DaysLate": 0,
      "DeliveryState": "WITHIN_COMMITMENT"
    }
  ]
}
```

### Every field

#### PurchaseOpenOrdersRequest

| Field | JSON/C# type | Meaning |
|---|---|---|
| VendorId | Guid? | Vendor identifier; null request means all visible vendors; a Stores MIR may have no vendor. |
| Currency | string? | Native three-letter currency; purchase workload can redact it to null. |
| RootPurchaseOrderId | Guid? | Stable original PO identity shared across amendments, or null request filter. |
| OverdueOnly | bool | When true, detail rows include only confirmed delivery dates already late. |
| Page | int | One-based requested detail page. |
| PageSize | int | Requested maximum detail rows per page, 1 to 1000. |

#### PurchaseOpenOrderAmount

| Field | JSON/C# type | Meaning |
|---|---|---|
| Currency | string | Native three-letter currency; purchase workload can redact it to null. |
| PoCount | long | Distinct root PO count represented by this native-currency aggregate. |
| Value | decimal | Monetary value on the endpoint/queue basis documented above, in Currency; not necessarily ex-tax cost. |
| OverduePoCount | long? | Distinct root PO count late against confirmed dates; null when overdue coverage is incomplete. |
| OverdueValue | decimal? | Remaining payable value late against confirmed dates; null when delivery coverage is incomplete. |

#### PurchaseOpenOrderIssue

| Field | JSON/C# type | Meaning |
|---|---|---|
| RootPurchaseOrderId | Guid | Stable original PO identity shared across amendments, or null request filter. |
| PoNumber | string | Human-readable PO number used to open the existing PO screen. |
| Code | string | Source issue code, or group business code (vendor/category), according to the enclosing object. |

#### PurchaseOpenOrderRow

| Field | JSON/C# type | Meaning |
|---|---|---|
| PurchaseOrderId | Guid | Specific PO revision identifier supplying this row, not necessarily the current revision. |
| RootPurchaseOrderId | Guid | Stable original PO identity shared across amendments, or null request filter. |
| PoNumber | string | Human-readable PO number used to open the existing PO screen. |
| RevisionNumber | int | Latest ISSUED revision represented by this outstanding line. |
| CurrentRevisionNumber | int | Current PO revision number, which can be a later unissued amendment. |
| CurrentStatus | string | Status of the current revision, while outstanding quantity can come from an earlier issued revision. |
| FirstIssuedAt | DateTimeOffset | First issue time across the root PO; age does not restart on amendment. |
| IssuedAt | DateTimeOffset | Issue time of the specific issued revision represented by the line. |
| AgeDays | int | Whole company-local calendar days since the row age origin, never negative. |
| VendorId | Guid | Vendor identifier; null request means all visible vendors; a Stores MIR may have no vendor. |
| VendorCode | string | Vendor business code. |
| VendorName | string | Vendor name from the applicable source/snapshot. |
| LineId | Guid | Source PO or GRN line identifier. Obligations advance rows have null because they are not item lines. |
| ItemId | Guid | Item identifier; null on advance rows. |
| ItemCode | string | Source item business code; null on advance rows. |
| ItemName | string | Source item description; null on advance rows. |
| Uom | string | Source unit of measure; null on advances. Never total unlike UOMs. |
| OrderedQuantity | decimal | Quantity ordered on the latest issued PO line. |
| ReceivedQuantity | decimal | Effective finalized, unreversed receipts across the same root PO/comparison-line lineage. |
| RemainingQuantity | decimal | OrderedQuantity minus ReceivedQuantity; only positive consistent outstanding rows are shown. |
| Currency | string | Native three-letter currency; purchase workload can redact it to null. |
| Value | decimal | Monetary value on the endpoint/queue basis documented above, in Currency; not necessarily ex-tax cost. |
| QuotedDeliveryDate | DateOnly | Source quotation promise; not necessarily a confirmed current commitment. |
| CommittedDeliveryDate | DateOnly? | Commitment date only when quotation/comparison/PO terms reconcile; otherwise null. |
| DeliveryTerms | string | Delivery terms snapshot on the represented PO. |
| DaysLate | int? | Whole days after confirmed commitment, zero when not late; null if date is unconfirmed. |
| DeliveryState | string | CONFIRMATION_REQUIRED, OVERDUE or WITHIN_COMMITMENT. |

#### PurchaseOpenOrdersPage

| Field | JSON/C# type | Meaning |
|---|---|---|
| CompanyCode | string | Selected ERP company code; verify it still matches the active company before rendering. |
| GeneratedAt | DateTimeOffset | Server timestamp for this projection. Show last refreshed time; not a document transaction date. |
| TimeZone | string | Server-configured company reporting timezone used for calendar dates and ages. |
| Basis | string | Exact server explanation of amount basis; display near monetary summaries. |
| Complete | bool | Whether source integrity permits full open-order aggregates. False is not zero outstanding. |
| OpenPoCount | long? | Distinct visible outstanding root POs, or null if source integrity is incomplete. |
| OldestAgeDays | int? | Largest whole local-calendar age among included records; null when none or unavailable. |
| DeliveryComplete | bool | True only if source integrity is complete and all outstanding lines have confirmed delivery dates. |
| OverduePoCount | long? | Distinct root PO count late against confirmed dates; null when overdue coverage is incomplete. |
| DeliveryDateUnconfirmedPoCount | long | Distinct visible outstanding root POs needing delivery-date confirmation. |
| SourceIssues | IReadOnlyList<PurchaseOpenOrderIssue> | Excluded/inconsistent root PO sources requiring reconciliation; show an actionable warning. |
| Amounts | IReadOnlyList<PurchaseOpenOrderAmount>? | Separate native-currency aggregates. An empty array is not a fabricated INR zero. |
| Filters | PurchaseOpenOrdersRequest | Normalised query echo. These select detail rows, not overview cards/groups. |
| TotalRows | long | Number of matching detail rows before pagination; not the number of cards or distinct documents. |
| Rows | IReadOnlyList<PurchaseOpenOrderRow> | This page of matching detail rows. Empty array is a valid result. |

### Exact denied response shape

Example caller: otherwise mapped/scoped PURCHASE_EXECUTIVE alone. HTTP 403.

```json
{
  "Type": "https://api.sess.example/problems/dashboard-access-denied",
  "Title": "Permission denied",
  "Status": 403,
  "Code": "DASHBOARD_ACCESS_DENIED",
  "Detail": "Open purchase orders are not permitted for your employee and selected company.",
  "TraceId": "mock-trace-403",
  "Errors": {}
}
```

## GET /api/v1/dashboards/purchase/obligations

Permission: dashboards.purchase-obligations:view AND commercial permission, within the Purchase role family.

Parameters beyond paging: queue omitted/null, grni or vendor-advances; vendorId (UUID), currency (three letters), documentId (UUID), all optional. Current outstanding balances, no date range. Sort: source date, queue, document, line. Overview Tiles and Vendors remain unfiltered by these detail selections.

GRNI includes unreversed FINALIZED NORMAL receipts, less quantities on ACCEPTED bills; reversed bills do not reduce it. Value = unaccepted quantity x UnitRateSnapshot. This is the quoted input unit rate before GST and added charges; it does NOT net quoted discounts or later bill adjustments. It is provisional receipt value, not final landed/Actual BOM cost. Advances = original cash amount minus adjustments plus restorations; reversed advances are excluded. Cash advances are not ex-tax purchase costs. SourceDate is the company-local receipt date or the advance PaidDate, which starts age. Impossible/negative balances return the integrity error described above.

### Complete HTTP 200 mock

[Standalone mock](dashboard-mocks/purchase-obligations.json)

```json
{
  "CompanyCode": "SESS_PVT_LTD",
  "GeneratedAt": "2026-09-14T05:25:08.760195+05:30",
  "TimeZone": "Asia/Kolkata",
  "Tiles": [
    {
      "Key": "grni",
      "Title": "Goods received not billed",
      "Basis": "Unbilled receipt quantity at receipt unit rate, by native PO currency; age from receipt date.",
      "Count": 3,
      "OldestAgeDays": 3,
      "Amounts": [
        {
          "Currency": "INR",
          "DocumentCount": 3,
          "LineCount": 3,
          "Value": 109000.01,
          "OldestAgeDays": 3
        }
      ]
    },
    {
      "Key": "vendor-advances",
      "Title": "Vendor advances outstanding",
      "Basis": "Unreversed advances less adjustments plus restorations, by native currency; age from payment date.",
      "Count": 2,
      "OldestAgeDays": 3,
      "Amounts": [
        {
          "Currency": "INR",
          "DocumentCount": 2,
          "LineCount": 2,
          "Value": 250.0,
          "OldestAgeDays": 3
        }
      ]
    }
  ],
  "Vendors": [
    {
      "Queue": "grni",
      "VendorId": "71000000-0000-0000-0005-000000000001",
      "VendorCode": "TRIAL-VEN-001",
      "VendorName": "TRIAL Alpine Cooling Supplies",
      "Currency": "INR",
      "DocumentCount": 3,
      "Value": 109000.01,
      "OldestAgeDays": 3
    },
    {
      "Queue": "vendor-advances",
      "VendorId": "71000000-0000-0000-0005-000000000001",
      "VendorCode": "TRIAL-VEN-001",
      "VendorName": "TRIAL Alpine Cooling Supplies",
      "Currency": "INR",
      "DocumentCount": 2,
      "Value": 250.0,
      "OldestAgeDays": 3
    }
  ],
  "Filters": {
    "Queue": null,
    "VendorId": null,
    "Currency": null,
    "DocumentId": null,
    "Page": 1,
    "PageSize": 100
  },
  "TotalRows": 5,
  "Rows": [
    {
      "Queue": "grni",
      "DocumentId": "e3d79320-950b-473f-8344-8ee41fef6ba6",
      "DocumentNumber": "GRN-26-27-000011",
      "LineId": "0db3491a-8d70-4800-97e6-54bd2bbdadf9",
      "PurchaseOrderId": "2a99024b-460d-41a2-b556-6501ce07176c",
      "RootPurchaseOrderId": "1d7c1479-8142-4ebd-a159-04ce3a1a677c",
      "PoNumber": "PO-26-27-000011",
      "VendorId": "71000000-0000-0000-0005-000000000001",
      "VendorCode": "TRIAL-VEN-001",
      "VendorName": "TRIAL Alpine Cooling Supplies",
      "ItemId": "71000000-0000-0000-0006-000000000001",
      "ItemCode": "TRIAL-ITEM-001",
      "ItemName": "TRIAL Copper control cable",
      "Uom": "TRIAL-MTR",
      "SourceDate": "2026-09-11",
      "AgeDays": 3,
      "Quantity": 1.0,
      "Currency": "INR",
      "Value": 4000.0,
      "UnitRate": 4000.0,
      "OriginalAmount": null,
      "AdjustedAmount": null
    },
    {
      "Queue": "vendor-advances",
      "DocumentId": "c300bba3-0216-4e7a-9986-7108e246324b",
      "DocumentNumber": "VADV-2026-000001",
      "LineId": null,
      "PurchaseOrderId": "04dea80f-bf95-4d54-b912-e14926aedc4f",
      "RootPurchaseOrderId": "9657a657-263e-4711-bf75-dae2426be130",
      "PoNumber": "PO-26-27-000021",
      "VendorId": "71000000-0000-0000-0005-000000000001",
      "VendorCode": "TRIAL-VEN-001",
      "VendorName": "TRIAL Alpine Cooling Supplies",
      "ItemId": null,
      "ItemCode": null,
      "ItemName": null,
      "Uom": null,
      "SourceDate": "2026-09-11",
      "AgeDays": 3,
      "Quantity": null,
      "Currency": "INR",
      "Value": 100.0,
      "UnitRate": null,
      "OriginalAmount": 100.0,
      "AdjustedAmount": 0
    },
    {
      "Queue": "vendor-advances",
      "DocumentId": "54cc39e4-e53b-44a5-8b91-f4ce1500aaa1",
      "DocumentNumber": "VADV-2026-000002",
      "LineId": null,
      "PurchaseOrderId": "04dea80f-bf95-4d54-b912-e14926aedc4f",
      "RootPurchaseOrderId": "9657a657-263e-4711-bf75-dae2426be130",
      "PoNumber": "PO-26-27-000021",
      "VendorId": "71000000-0000-0000-0005-000000000001",
      "VendorCode": "TRIAL-VEN-001",
      "VendorName": "TRIAL Alpine Cooling Supplies",
      "ItemId": null,
      "ItemCode": null,
      "ItemName": null,
      "Uom": null,
      "SourceDate": "2026-09-12",
      "AgeDays": 2,
      "Quantity": null,
      "Currency": "INR",
      "Value": 150.0,
      "UnitRate": null,
      "OriginalAmount": 150.0,
      "AdjustedAmount": 0
    },
    {
      "Queue": "grni",
      "DocumentId": "890607c1-8614-4cbc-a4ca-5cb2ca135af6",
      "DocumentNumber": "GRN-26-27-000031",
      "LineId": "786377ea-8aca-4d65-965d-48553d9ec9f5",
      "PurchaseOrderId": "bd632fcd-06fe-4f21-9b1f-dbe0f5535106",
      "RootPurchaseOrderId": "a067bfbb-cc82-44c5-97de-23001abacf79",
      "PoNumber": "PO-26-27-000031",
      "VendorId": "71000000-0000-0000-0005-000000000001",
      "VendorCode": "TRIAL-VEN-001",
      "VendorName": "TRIAL Alpine Cooling Supplies",
      "ItemId": "71000000-0000-0000-0006-000000000001",
      "ItemCode": "TRIAL-ITEM-001",
      "ItemName": "TRIAL Copper control cable",
      "Uom": "TRIAL-MTR",
      "SourceDate": "2026-09-14",
      "AgeDays": 0,
      "Quantity": 1.0,
      "Currency": "INR",
      "Value": 100000.01,
      "UnitRate": 100000.01,
      "OriginalAmount": null,
      "AdjustedAmount": null
    },
    {
      "Queue": "grni",
      "DocumentId": "8e85314e-ae7f-4d97-9496-aaca33dc22b1",
      "DocumentNumber": "GRN-26-27-000021",
      "LineId": "2374fa31-45bb-4ed8-b88c-4135f6c25378",
      "PurchaseOrderId": "04dea80f-bf95-4d54-b912-e14926aedc4f",
      "RootPurchaseOrderId": "9657a657-263e-4711-bf75-dae2426be130",
      "PoNumber": "PO-26-27-000021",
      "VendorId": "71000000-0000-0000-0005-000000000001",
      "VendorCode": "TRIAL-VEN-001",
      "VendorName": "TRIAL Alpine Cooling Supplies",
      "ItemId": "71000000-0000-0000-0006-000000000001",
      "ItemCode": "TRIAL-ITEM-001",
      "ItemName": "TRIAL Copper control cable",
      "Uom": "TRIAL-MTR",
      "SourceDate": "2026-09-14",
      "AgeDays": 0,
      "Quantity": 1.0,
      "Currency": "INR",
      "Value": 5000.0,
      "UnitRate": 5000.0,
      "OriginalAmount": null,
      "AdjustedAmount": null
    }
  ]
}
```

### Every field

#### PurchaseObligationsRequest

| Field | JSON/C# type | Meaning |
|---|---|---|
| Queue | string? | Selected queue, or null for all permitted queues. In a row, identifies its source queue. |
| VendorId | Guid? | Vendor identifier; null request means all visible vendors; a Stores MIR may have no vendor. |
| Currency | string? | Native three-letter currency; purchase workload can redact it to null. |
| DocumentId | Guid? | Identifier of this source document; its kind is given by the queue/document type. |
| Page | int | One-based requested detail page. |
| PageSize | int | Requested maximum detail rows per page, 1 to 1000. |

#### PurchaseObligationAmount

| Field | JSON/C# type | Meaning |
|---|---|---|
| Currency | string | Native three-letter currency; purchase workload can redact it to null. |
| DocumentCount | long | Distinct source documents represented by this queue/currency/vendor aggregate. |
| LineCount | long | Obligations: detail source rows. QC: distinct GRN lines, not allocation-row count or units. |
| Value | decimal | Monetary value on the endpoint/queue basis documented above, in Currency; not necessarily ex-tax cost. |
| OldestAgeDays | int? | Largest whole local-calendar age among included records; null when none or unavailable. |

#### PurchaseObligationTile

| Field | JSON/C# type | Meaning |
|---|---|---|
| Key | string | Stable card/period key used for selection; do not match translated titles. |
| Title | string | Server-provided human-readable card heading. |
| Basis | string | Exact server explanation of amount basis; display near monetary summaries. |
| Count | long | Distinct documents in this queue. Null on a denied workload card. |
| OldestAgeDays | int? | Largest whole local-calendar age among included records; null when none or unavailable. |
| Amounts | IReadOnlyList<PurchaseObligationAmount> | Separate native-currency aggregates. An empty array is not a fabricated INR zero. |

#### PurchaseObligationVendor

| Field | JSON/C# type | Meaning |
|---|---|---|
| Queue | string | Selected queue, or null for all permitted queues. In a row, identifies its source queue. |
| VendorId | Guid | Vendor identifier; null request means all visible vendors; a Stores MIR may have no vendor. |
| VendorCode | string | Vendor business code. |
| VendorName | string | Vendor name from the applicable source/snapshot. |
| Currency | string | Native three-letter currency; purchase workload can redact it to null. |
| DocumentCount | long | Distinct source documents represented by this queue/currency/vendor aggregate. |
| Value | decimal | Monetary value on the endpoint/queue basis documented above, in Currency; not necessarily ex-tax cost. |
| OldestAgeDays | int? | Largest whole local-calendar age among included records; null when none or unavailable. |

#### PurchaseObligationRow

| Field | JSON/C# type | Meaning |
|---|---|---|
| Queue | string | Selected queue, or null for all permitted queues. In a row, identifies its source queue. |
| DocumentId | Guid | Identifier of this source document; its kind is given by the queue/document type. |
| DocumentNumber | string | Human-readable number of the source document. |
| LineId | Guid? | Source PO or GRN line identifier. Obligations advance rows have null because they are not item lines. |
| PurchaseOrderId | Guid | Specific PO revision identifier supplying this row, not necessarily the current revision. |
| RootPurchaseOrderId | Guid | Stable original PO identity shared across amendments, or null request filter. |
| PoNumber | string | Human-readable PO number used to open the existing PO screen. |
| VendorId | Guid | Vendor identifier; null request means all visible vendors; a Stores MIR may have no vendor. |
| VendorCode | string | Vendor business code. |
| VendorName | string | Vendor name from the applicable source/snapshot. |
| ItemId | Guid? | Item identifier; null on advance rows. |
| ItemCode | string? | Source item business code; null on advance rows. |
| ItemName | string? | Source item description; null on advance rows. |
| Uom | string? | Source unit of measure; null on advances. Never total unlike UOMs. |
| SourceDate | DateOnly | GRNI receipt date or advance paid date, in the company reporting calendar. |
| AgeDays | int | Whole company-local calendar days since the row age origin, never negative. |
| Quantity | decimal? | GRNI unbilled quantity, signed spending-event quantity, or current QC held quantity; null for advances. |
| Currency | string | Native three-letter currency; purchase workload can redact it to null. |
| Value | decimal | Monetary value on the endpoint/queue basis documented above, in Currency; not necessarily ex-tax cost. |
| UnitRate | decimal? | GRNI receipt unit rate before GST/added charges; null for advances. |
| OriginalAmount | decimal? | Original cash advance amount; null for GRNI. |
| AdjustedAmount | decimal? | Net advance adjustments less restorations; null for GRNI. Outstanding = OriginalAmount - AdjustedAmount. |

#### PurchaseObligationsPage

| Field | JSON/C# type | Meaning |
|---|---|---|
| CompanyCode | string | Selected ERP company code; verify it still matches the active company before rendering. |
| GeneratedAt | DateTimeOffset | Server timestamp for this projection. Show last refreshed time; not a document transaction date. |
| TimeZone | string | Server-configured company reporting timezone used for calendar dates and ages. |
| Tiles | IReadOnlyList<PurchaseObligationTile> | All overview cards for this endpoint; scope-filtered but not narrowed by detail filters. |
| Vendors | IReadOnlyList<PurchaseObligationVendor> | Workload: vendor names. Obligations page: grouped vendor balances described below. |
| Filters | PurchaseObligationsRequest | Normalised query echo. These select detail rows, not overview cards/groups. |
| TotalRows | long | Number of matching detail rows before pagination; not the number of cards or distinct documents. |
| Rows | IReadOnlyList<PurchaseObligationRow> | This page of matching detail rows. Empty array is a valid result. |

### Exact denied response shape

Example caller: otherwise mapped/scoped PURCHASE_EXECUTIVE alone. HTTP 403.

```json
{
  "Type": "https://api.sess.example/problems/dashboard-access-denied",
  "Title": "Permission denied",
  "Status": 403,
  "Code": "DASHBOARD_ACCESS_DENIED",
  "Detail": "Purchase obligations are not permitted for your employee and selected company.",
  "TraceId": "mock-trace-403",
  "Errors": {}
}
```

## GET /api/v1/dashboards/purchase/spending

Permission: dashboards.purchase-spending:view AND commercial permission, within the Purchase role family.

Parameters beyond paging: period (default financial-year; exact values month, quarter, financial-year, twelve-months); month (optional DateOnly first day of a month, only with period=month, limited to current or preceding eleven months); vendorId, categoryId, billId (optional UUIDs); currency (optional three letters). Current month starts on its first day; quarter is calendar quarter; financial year starts 1 April; twelve-months starts eleven months before the current month. Ranges end inclusively at company-local today. No arbitrary date range.

Periods always contains month, quarter and financial-year summaries. TopVendors is FY top ten separately per currency; Categories is FY; MonthlyTrend has twelve buckets oldest first, including empty currency arrays. These summaries do not change with detail filters. Rows order by decision timestamp descending, then bill, line and event. ACCEPTED and REVERSED decisions are separate signed quantity/value events dated by the decision, not bill date or payment date. Distinct bill/root-PO counts indicate activity even if net value is zero. Category comes from GRN snapshot identity/code, not current item classification; category Group.Name currently contains its code.

**Current spending is NOT ex-recoverable-GST Actual BOM cost.** MaterialValue uses signed BilledPayableValue, which includes embedded GST; AllocatedCharges adds allocated bill charges, excluding separately designated recoverable-GST charges in allocation. Amount is their sum. Label as accepted payable purchases plus allocated charges, less reversals. For base 4,000 + GST 720 + freight 12, this measure can be 4,732 while component cost is 4,012 (30% = 1,203.60). The response lacks the tax breakdown needed to fix this in the browser. A future ex-tax spending measure needs a reviewed backend change. No payment-status inference.

### Complete HTTP 200 mock

[Standalone mock](dashboard-mocks/purchase-spending.json)

```json
{
  "CompanyCode": "SESS_PVT_LTD",
  "GeneratedAt": "2026-09-14T03:36:46.632766+05:30",
  "TimeZone": "Asia/Kolkata",
  "Basis": "Accepted bill material value plus allocated charges, less reversals, by local decision date and native currency. PO and bill counts measure documents with activity.",
  "Periods": [
    {
      "Key": "financial-year",
      "FromDate": "2026-04-01",
      "ToDate": "2026-09-14",
      "Amounts": [
        {
          "Currency": "INR",
          "MaterialValue": 128620.01,
          "AllocatedCharges": 12.0,
          "Amount": 128632.01,
          "PoCount": 3,
          "BillCount": 4
        }
      ]
    },
    {
      "Key": "month",
      "FromDate": "2026-09-01",
      "ToDate": "2026-09-14",
      "Amounts": [
        {
          "Currency": "INR",
          "MaterialValue": 128620.01,
          "AllocatedCharges": 12.0,
          "Amount": 128632.01,
          "PoCount": 3,
          "BillCount": 4
        }
      ]
    },
    {
      "Key": "quarter",
      "FromDate": "2026-07-01",
      "ToDate": "2026-09-14",
      "Amounts": [
        {
          "Currency": "INR",
          "MaterialValue": 128620.01,
          "AllocatedCharges": 12.0,
          "Amount": 128632.01,
          "PoCount": 3,
          "BillCount": 4
        }
      ]
    }
  ],
  "TopVendors": [
    {
      "Id": "71000000-0000-0000-0005-000000000001",
      "Code": "TRIAL-VEN-001",
      "Name": "TRIAL Alpine Cooling Supplies",
      "Currency": "INR",
      "MaterialValue": 128620.01,
      "AllocatedCharges": 12.0,
      "Amount": 128632.01,
      "PoCount": 3,
      "BillCount": 4
    }
  ],
  "Categories": [
    {
      "Id": "71000000-0000-0000-0001-000000000001",
      "Code": "ELE",
      "Name": "ELE",
      "Currency": "INR",
      "MaterialValue": 128620.01,
      "AllocatedCharges": 12.0,
      "Amount": 128632.01,
      "PoCount": 3,
      "BillCount": 4
    }
  ],
  "MonthlyTrend": [
    {
      "Key": "2025-10",
      "FromDate": "2025-10-01",
      "ToDate": "2025-10-31",
      "Amounts": []
    },
    {
      "Key": "2025-11",
      "FromDate": "2025-11-01",
      "ToDate": "2025-11-30",
      "Amounts": []
    },
    {
      "Key": "2025-12",
      "FromDate": "2025-12-01",
      "ToDate": "2025-12-31",
      "Amounts": []
    },
    {
      "Key": "2026-01",
      "FromDate": "2026-01-01",
      "ToDate": "2026-01-31",
      "Amounts": []
    },
    {
      "Key": "2026-02",
      "FromDate": "2026-02-01",
      "ToDate": "2026-02-28",
      "Amounts": []
    },
    {
      "Key": "2026-03",
      "FromDate": "2026-03-01",
      "ToDate": "2026-03-31",
      "Amounts": []
    },
    {
      "Key": "2026-04",
      "FromDate": "2026-04-01",
      "ToDate": "2026-04-30",
      "Amounts": []
    },
    {
      "Key": "2026-05",
      "FromDate": "2026-05-01",
      "ToDate": "2026-05-31",
      "Amounts": []
    },
    {
      "Key": "2026-06",
      "FromDate": "2026-06-01",
      "ToDate": "2026-06-30",
      "Amounts": []
    },
    {
      "Key": "2026-07",
      "FromDate": "2026-07-01",
      "ToDate": "2026-07-31",
      "Amounts": []
    },
    {
      "Key": "2026-08",
      "FromDate": "2026-08-01",
      "ToDate": "2026-08-31",
      "Amounts": []
    },
    {
      "Key": "2026-09",
      "FromDate": "2026-09-01",
      "ToDate": "2026-09-14",
      "Amounts": [
        {
          "Currency": "INR",
          "MaterialValue": 128620.01,
          "AllocatedCharges": 12.0,
          "Amount": 128632.01,
          "PoCount": 3,
          "BillCount": 4
        }
      ]
    }
  ],
  "FromDate": "2026-04-01",
  "ToDate": "2026-09-14",
  "Filters": {
    "Period": "financial-year",
    "Month": null,
    "VendorId": null,
    "CategoryId": null,
    "Currency": null,
    "BillId": null,
    "Page": 1,
    "PageSize": 100
  },
  "TotalRows": 5,
  "Rows": [
    {
      "BillId": "7fa6d5ab-0ef5-4695-a78b-57bae6080f7c",
      "BillLineId": "dee298cd-8894-4d51-9bcd-effb2fa98d8e",
      "BillNumber": "TRIAL-BILL-LOW",
      "Event": "ACCEPTED",
      "EventDate": "2026-09-14",
      "PurchaseOrderId": "43230da5-19cc-471b-8b7b-871b5bea1cd6",
      "RootPurchaseOrderId": "5286c355-25c1-497f-a212-c1a4206db61a",
      "PoNumber": "PO-26-27-000011",
      "VendorId": "71000000-0000-0000-0005-000000000001",
      "VendorCode": "TRIAL-VEN-001",
      "VendorName": "TRIAL Alpine Cooling Supplies",
      "CategoryId": "71000000-0000-0000-0001-000000000001",
      "CategoryCode": "ELE",
      "ItemId": "71000000-0000-0000-0006-000000000001",
      "ItemCode": "TRIAL-ITEM-001",
      "ItemName": "TRIAL Copper control cable",
      "Uom": "TRIAL-MTR",
      "Quantity": 1.0,
      "Currency": "INR",
      "MaterialValue": 4720.0,
      "AllocatedCharges": 12.0,
      "Amount": 4732.0
    },
    {
      "BillId": "3355a8d9-9e4f-4e76-977a-b5141297caa1",
      "BillLineId": "9d38cea6-deb9-4f14-a6ef-e66948262716",
      "BillNumber": "TRIAL-BILL-MD",
      "Event": "ACCEPTED",
      "EventDate": "2026-09-14",
      "PurchaseOrderId": "2b4e657d-35cd-448c-8bd2-2d658957489b",
      "RootPurchaseOrderId": "ed40ea57-0faf-46a1-9b5b-62e64552538e",
      "PoNumber": "PO-26-27-000031",
      "VendorId": "71000000-0000-0000-0005-000000000001",
      "VendorCode": "TRIAL-VEN-001",
      "VendorName": "TRIAL Alpine Cooling Supplies",
      "CategoryId": "71000000-0000-0000-0001-000000000001",
      "CategoryCode": "ELE",
      "ItemId": "71000000-0000-0000-0006-000000000001",
      "ItemCode": "TRIAL-ITEM-001",
      "ItemName": "TRIAL Copper control cable",
      "Uom": "TRIAL-MTR",
      "Quantity": 1.0,
      "Currency": "INR",
      "MaterialValue": 118000.01,
      "AllocatedCharges": 0,
      "Amount": 118000.01
    },
    {
      "BillId": "ee5c4d74-029b-4fb8-914b-ddac6753281f",
      "BillLineId": "c793a4a2-d0b6-4534-a5bf-7687d1b3e222",
      "BillNumber": "TRIAL-BILL-TD",
      "Event": "ACCEPTED",
      "EventDate": "2026-09-14",
      "PurchaseOrderId": "4bc28e17-7eb8-4f9b-825e-fd1a54d2f9d5",
      "RootPurchaseOrderId": "ba57cc3d-1ac5-480d-af89-7eaddb37e806",
      "PoNumber": "PO-26-27-000021",
      "VendorId": "71000000-0000-0000-0005-000000000001",
      "VendorCode": "TRIAL-VEN-001",
      "VendorName": "TRIAL Alpine Cooling Supplies",
      "CategoryId": "71000000-0000-0000-0001-000000000001",
      "CategoryCode": "ELE",
      "ItemId": "71000000-0000-0000-0006-000000000001",
      "ItemCode": "TRIAL-ITEM-001",
      "ItemName": "TRIAL Copper control cable",
      "Uom": "TRIAL-MTR",
      "Quantity": 1.0,
      "Currency": "INR",
      "MaterialValue": 5900.0,
      "AllocatedCharges": 0,
      "Amount": 5900.0
    },
    {
      "BillId": "7f9de969-3264-4f51-9862-cc050bfbe4ba",
      "BillLineId": "1a085094-32db-4f0d-a679-5160a4c1c5af",
      "BillNumber": "TRIAL-BILL-TD",
      "Event": "REVERSED",
      "EventDate": "2026-09-14",
      "PurchaseOrderId": "4bc28e17-7eb8-4f9b-825e-fd1a54d2f9d5",
      "RootPurchaseOrderId": "ba57cc3d-1ac5-480d-af89-7eaddb37e806",
      "PoNumber": "PO-26-27-000021",
      "VendorId": "71000000-0000-0000-0005-000000000001",
      "VendorCode": "TRIAL-VEN-001",
      "VendorName": "TRIAL Alpine Cooling Supplies",
      "CategoryId": "71000000-0000-0000-0001-000000000001",
      "CategoryCode": "ELE",
      "ItemId": "71000000-0000-0000-0006-000000000001",
      "ItemCode": "TRIAL-ITEM-001",
      "ItemName": "TRIAL Copper control cable",
      "Uom": "TRIAL-MTR",
      "Quantity": -1.0,
      "Currency": "INR",
      "MaterialValue": -5900.0,
      "AllocatedCharges": 0,
      "Amount": -5900.0
    },
    {
      "BillId": "7f9de969-3264-4f51-9862-cc050bfbe4ba",
      "BillLineId": "1a085094-32db-4f0d-a679-5160a4c1c5af",
      "BillNumber": "TRIAL-BILL-TD",
      "Event": "ACCEPTED",
      "EventDate": "2026-09-14",
      "PurchaseOrderId": "4bc28e17-7eb8-4f9b-825e-fd1a54d2f9d5",
      "RootPurchaseOrderId": "ba57cc3d-1ac5-480d-af89-7eaddb37e806",
      "PoNumber": "PO-26-27-000021",
      "VendorId": "71000000-0000-0000-0005-000000000001",
      "VendorCode": "TRIAL-VEN-001",
      "VendorName": "TRIAL Alpine Cooling Supplies",
      "CategoryId": "71000000-0000-0000-0001-000000000001",
      "CategoryCode": "ELE",
      "ItemId": "71000000-0000-0000-0006-000000000001",
      "ItemCode": "TRIAL-ITEM-001",
      "ItemName": "TRIAL Copper control cable",
      "Uom": "TRIAL-MTR",
      "Quantity": 1.0,
      "Currency": "INR",
      "MaterialValue": 5900.0,
      "AllocatedCharges": 0,
      "Amount": 5900.0
    }
  ]
}
```

### Every field

#### PurchaseSpendingRequest

| Field | JSON/C# type | Meaning |
|---|---|---|
| Period | string | Detail window: month, quarter, financial-year or twelve-months. |
| Month | DateOnly? | Optional first-of-month date for period=month, within the current and preceding eleven months. |
| VendorId | Guid? | Vendor identifier; null request means all visible vendors; a Stores MIR may have no vendor. |
| CategoryId | Guid? | Recorded receipt category identifier, or null request filter; not necessarily current item category. |
| Currency | string? | Native three-letter currency; purchase workload can redact it to null. |
| BillId | Guid? | Accepted/reversed bill identifier, or null request filter. |
| Page | int | One-based requested detail page. |
| PageSize | int | Requested maximum detail rows per page, 1 to 1000. |

#### PurchaseSpendingAmount

| Field | JSON/C# type | Meaning |
|---|---|---|
| Currency | string | Native three-letter currency; purchase workload can redact it to null. |
| MaterialValue | decimal | Signed bill-line BilledPayableValue, including embedded tax. This API name does not mean ex-tax material cost. |
| AllocatedCharges | decimal | Signed retained bill-line charge allocations; excludes separately marked recoverable-GST charges. |
| Amount | decimal | Amount in the named currency. Spending: MaterialValue + AllocatedCharges; workload: queue aggregate. |
| PoCount | long | Distinct root PO count represented by this native-currency aggregate. |
| BillCount | long | Distinct bills with acceptance/reversal activity; a fully reversed bill can still count. |

#### PurchaseSpendingPeriod

| Field | JSON/C# type | Meaning |
|---|---|---|
| Key | string | Stable card/period key used for selection; do not match translated titles. |
| FromDate | DateOnly | Inclusive beginning of this company-local period. |
| ToDate | DateOnly | Inclusive end, capped at company-local today. |
| Amounts | IReadOnlyList<PurchaseSpendingAmount> | Separate native-currency aggregates. An empty array is not a fabricated INR zero. |

#### PurchaseSpendingGroup

| Field | JSON/C# type | Meaning |
|---|---|---|
| Id | Guid | Vendor or category identifier for this breakdown group. |
| Code | string | Source issue code, or group business code (vendor/category), according to the enclosing object. |
| Name | string | Vendor name; category grouping currently repeats the recorded category code. |
| Currency | string | Native three-letter currency; purchase workload can redact it to null. |
| MaterialValue | decimal | Signed bill-line BilledPayableValue, including embedded tax. This API name does not mean ex-tax material cost. |
| AllocatedCharges | decimal | Signed retained bill-line charge allocations; excludes separately marked recoverable-GST charges. |
| Amount | decimal | Amount in the named currency. Spending: MaterialValue + AllocatedCharges; workload: queue aggregate. |
| PoCount | long | Distinct root PO count represented by this native-currency aggregate. |
| BillCount | long | Distinct bills with acceptance/reversal activity; a fully reversed bill can still count. |

#### PurchaseSpendingRow

| Field | JSON/C# type | Meaning |
|---|---|---|
| BillId | Guid | Accepted/reversed bill identifier, or null request filter. |
| BillLineId | Guid | Bill line supplying this spending event. |
| BillNumber | string | Human-readable accepted/reversed bill number. |
| Event | string | ACCEPTED adds value; REVERSED subtracts value. Both can appear for one bill. |
| EventDate | DateOnly | Company-local acceptance/reversal decision date; not invoice date or payment date. |
| PurchaseOrderId | Guid | Specific PO revision identifier supplying this row, not necessarily the current revision. |
| RootPurchaseOrderId | Guid | Stable original PO identity shared across amendments, or null request filter. |
| PoNumber | string | Human-readable PO number used to open the existing PO screen. |
| VendorId | Guid | Vendor identifier; null request means all visible vendors; a Stores MIR may have no vendor. |
| VendorCode | string | Vendor business code. |
| VendorName | string | Vendor name from the applicable source/snapshot. |
| CategoryId | Guid | Recorded receipt category identifier, or null request filter; not necessarily current item category. |
| CategoryCode | string | Category code frozen on the GRN line. |
| ItemId | Guid | Item identifier; null on advance rows. |
| ItemCode | string | Source item business code; null on advance rows. |
| ItemName | string | Source item description; null on advance rows. |
| Uom | string | Source unit of measure; null on advances. Never total unlike UOMs. |
| Quantity | decimal | GRNI unbilled quantity, signed spending-event quantity, or current QC held quantity; null for advances. |
| Currency | string | Native three-letter currency; purchase workload can redact it to null. |
| MaterialValue | decimal | Signed bill-line BilledPayableValue, including embedded tax. This API name does not mean ex-tax material cost. |
| AllocatedCharges | decimal | Signed retained bill-line charge allocations; excludes separately marked recoverable-GST charges. |
| Amount | decimal | Amount in the named currency. Spending: MaterialValue + AllocatedCharges; workload: queue aggregate. |

#### PurchaseSpendingPage

| Field | JSON/C# type | Meaning |
|---|---|---|
| CompanyCode | string | Selected ERP company code; verify it still matches the active company before rendering. |
| GeneratedAt | DateTimeOffset | Server timestamp for this projection. Show last refreshed time; not a document transaction date. |
| TimeZone | string | Server-configured company reporting timezone used for calendar dates and ages. |
| Basis | string | Exact server explanation of amount basis; display near monetary summaries. |
| Periods | IReadOnlyList<PurchaseSpendingPeriod> | Current month, calendar quarter and April-March financial-year overview buckets. |
| TopVendors | IReadOnlyList<PurchaseSpendingGroup> | Top ten vendors PER CURRENCY for the financial year; unaffected by detail filters. |
| Categories | IReadOnlyList<PurchaseSpendingGroup> | Financial-year category totals per currency; unaffected by detail filters. |
| MonthlyTrend | IReadOnlyList<PurchaseSpendingPeriod> | Twelve monthly buckets, oldest to newest, including empty months. |
| FromDate | DateOnly | Inclusive beginning of this company-local period. |
| ToDate | DateOnly | Inclusive end, capped at company-local today. |
| Filters | PurchaseSpendingRequest | Normalised query echo. These select detail rows, not overview cards/groups. |
| TotalRows | long | Number of matching detail rows before pagination; not the number of cards or distinct documents. |
| Rows | IReadOnlyList<PurchaseSpendingRow> | This page of matching detail rows. Empty array is a valid result. |

### Exact denied response shape

Example caller: otherwise mapped/scoped PURCHASE_EXECUTIVE alone. HTTP 403.

```json
{
  "Type": "https://api.sess.example/problems/dashboard-access-denied",
  "Title": "Permission denied",
  "Status": 403,
  "Code": "DASHBOARD_ACCESS_DENIED",
  "Detail": "Purchase spending is not permitted for your employee and selected company.",
  "TraceId": "mock-trace-403",
  "Errors": {}
}
```

## GET /api/v1/dashboards/stores/workload

Permission: dashboards.stores-workload:view; gate card additionally needs inventory.grn:view; MIR cards need stores.material-issue-requests:view.

Parameters beyond paging: queue omitted/null, gate-no-grn, mir-approval or mir-unissued; documentId (optional UUID). Current backlog, no date range. Sort: waiting timestamp, queue, document ID. No money fields.

Gate card includes finalized normal unreversed entries with no normal GRN at all; even a draft GRN removes an entry. MIR approval means submitted; issue queue means approved/partially issued remaining quantities. Returns do not reopen fulfilled MIR quantities. Age starts at arrival, submission (creation fallback), or approval as appropriate. EligibleApprovalRoles can be Stores Manager/Production Manager, but this does not itself give Production Manager dashboard access. AssignedApproverEmployeeId is currently always null. ResponsibilityIssue says `No named approver is assigned by the current MIR workflow.` Do not invent a named approver.

### Complete HTTP 200 mock

[Standalone mock](dashboard-mocks/stores-workload.json)

```json
{
  "CompanyCode": "SESS_PVT_LTD",
  "GeneratedAt": "2026-09-14T08:01:03.955147+05:30",
  "TimeZone": "Asia/Kolkata",
  "Tiles": [
    {
      "Key": "gate-no-grn",
      "Title": "Gate entries awaiting GRN",
      "State": "ACCESS_DENIED",
      "Count": null,
      "OldestAgeDays": null,
      "Coverage": "Finalized normal unreversed gate entries with no normal GRN, including no draft GRN. Age starts at arrival."
    },
    {
      "Key": "mir-approval",
      "Title": "MIRs awaiting approval",
      "State": "READY",
      "Count": 1,
      "OldestAgeDays": 0,
      "Coverage": "Submitted requests. Eligible approval roles are Stores Manager or Production Manager; no named approval assignment is currently recorded. Age starts at submission, or creation if submission history is unavailable."
    },
    {
      "Key": "mir-unissued",
      "Title": "Approved MIRs awaiting issue",
      "State": "READY",
      "Count": 0,
      "OldestAgeDays": null,
      "Coverage": "Approved or partially fulfilled requests with remaining lines. Returns do not reopen a fulfilled request. Age starts at approval."
    }
  ],
  "Filters": {
    "Queue": null,
    "DocumentId": null,
    "Page": 1,
    "PageSize": 100
  },
  "TotalRows": 1,
  "Rows": [
    {
      "Queue": "mir-approval",
      "DocumentId": "f254a728-fd57-4b9a-bb7c-3a5493ebc532",
      "DocumentType": "MIR",
      "DocumentNumber": "MIR-SESS_PVT_LTD-26-27-000011",
      "Status": "SUBMITTED",
      "WaitingSince": "2026-09-14T08:01:03.221262+05:30",
      "AgeDays": 0,
      "PendingLineCount": 1,
      "VendorId": null,
      "VendorName": null,
      "EligibleApprovalRoles": [
        "STORES_MANAGER",
        "PRODUCTION_MANAGER"
      ],
      "AssignedApproverEmployeeId": null,
      "ResponsibilityIssue": "No named approver is assigned by the current MIR workflow.",
      "DetailPath": "/api/v1/stores/material-issue-requests/f254a728-fd57-4b9a-bb7c-3a5493ebc532"
    }
  ]
}
```

### Every field

#### StoresWorkloadRequest

| Field | JSON/C# type | Meaning |
|---|---|---|
| Queue | string? | Selected queue, or null for all permitted queues. In a row, identifies its source queue. |
| DocumentId | Guid? | Identifier of this source document; its kind is given by the queue/document type. |
| Page | int | One-based requested detail page. |
| PageSize | int | Requested maximum detail rows per page, 1 to 1000. |

#### StoresWorkloadTile

| Field | JSON/C# type | Meaning |
|---|---|---|
| Key | string | Stable card/period key used for selection; do not match translated titles. |
| Title | string | Server-provided human-readable card heading. |
| State | string | READY or ACCESS_DENIED for this card. Denied is not an empty queue. |
| Count | long? | Distinct documents in this queue. Null on a denied workload card. |
| OldestAgeDays | int? | Largest whole local-calendar age among included records; null when none or unavailable. |
| Coverage | string | Server explanation of what this card includes and excludes. |

#### StoresWorkloadRow

| Field | JSON/C# type | Meaning |
|---|---|---|
| Queue | string | Selected queue, or null for all permitted queues. In a row, identifies its source queue. |
| DocumentId | Guid | Identifier of this source document; its kind is given by the queue/document type. |
| DocumentType | string | Source kind: purchase PR/RFQ/QUOTATION/COMPARISON/PO, or Stores GATE_ENTRY/MIR. |
| DocumentNumber | string | Human-readable number of the source document. |
| Status | string | Recorded workflow status; preserve the returned spelling/case. |
| WaitingSince | DateTimeOffset | Timestamp from which the current queue age is measured. |
| AgeDays | int | Whole company-local calendar days since the row age origin, never negative. |
| PendingLineCount | long | Number of still-pending document lines, not quantity; purchase workload may return null. |
| VendorId | Guid? | Vendor identifier; null request means all visible vendors; a Stores MIR may have no vendor. |
| VendorName | string? | Vendor name from the applicable source/snapshot. |
| EligibleApprovalRoles | IReadOnlyList<string> | Roles eligible to approve this MIR; empty outside MIR approval. Not a named assignment. |
| AssignedApproverEmployeeId | Guid? | Currently null: MIR workflow has no named approver assignment. |
| ResponsibilityIssue | string? | Reason a responsible employee cannot be identified; show it rather than inventing an assignee. |
| DetailPath | string | API resource path, NOT automatically a browser route. Use the mapping below. |

#### StoresWorkloadPage

| Field | JSON/C# type | Meaning |
|---|---|---|
| CompanyCode | string | Selected ERP company code; verify it still matches the active company before rendering. |
| GeneratedAt | DateTimeOffset | Server timestamp for this projection. Show last refreshed time; not a document transaction date. |
| TimeZone | string | Server-configured company reporting timezone used for calendar dates and ages. |
| Tiles | IReadOnlyList<StoresWorkloadTile> | All overview cards for this endpoint; scope-filtered but not narrowed by detail filters. |
| Filters | StoresWorkloadRequest | Normalised query echo. These select detail rows, not overview cards/groups. |
| TotalRows | long | Number of matching detail rows before pagination; not the number of cards or distinct documents. |
| Rows | IReadOnlyList<StoresWorkloadRow> | This page of matching detail rows. Empty array is a valid result. |

### Exact denied response shape

Example caller: otherwise mapped/scoped PRODUCTION_MANAGER alone. HTTP 403.

```json
{
  "Type": "https://api.sess.example/problems/dashboard-access-denied",
  "Title": "Permission denied",
  "Status": 403,
  "Code": "DASHBOARD_ACCESS_DENIED",
  "Detail": "Stores workload is not permitted for your employee and selected company.",
  "TraceId": "mock-trace-403",
  "Errors": {}
}
```

## GET /api/v1/dashboards/stores/qc-stock

Permission: dashboards.stores-qc-stock:view AND inventory.grn:view. Money additionally requires BOTH pages' commercial grants.

Parameters beyond paging: queue omitted/null, QC_HOLD or PENDING_RETURNABLE_DC (case-sensitive); documentId is an optional GRN UUID. Current positive GRN-allocation stock, not all opening stock; no date range. Sort: received timestamp, queue, allocation and dimensions.

Tiles count distinct GRN lines, not rows: one line/allocation may split by warehouse, rack, ownership, custody, provenance and serial. Only QC_HOLD past QcDueAt is overdue; pending returnable DC is not QC overdue. Provisional value = quantity x receipt UnitRateSnapshot, before taxes and charges, excluding later bill/landed-cost changes. Not Actual BOM cost. CanViewCommercialValues=false means Tiles.Values and Rows.ReceiptProvisionalValue are null; currency may remain. No paid/unpaid information is carried.

### Complete HTTP 200 mock

[Standalone mock](dashboard-mocks/stores-qc-stock.json)

```json
{
  "CompanyCode": "SESS_PVT_LTD",
  "GeneratedAt": "2026-09-14T09:06:53.507562+05:30",
  "TimeZone": "Asia/Kolkata",
  "CanViewCommercialValues": true,
  "ValueBasis": "Current GRN-origin held quantity times receipt unit rate, in native PO currency. Provisional receipt value; excludes later bill and landed-cost adjustments.",
  "Tiles": [
    {
      "Key": "PENDING_RETURNABLE_DC",
      "LineCount": 0,
      "OverdueLineCount": 0,
      "OldestReceiptAgeDays": null,
      "Values": []
    },
    {
      "Key": "QC_HOLD",
      "LineCount": 1,
      "OverdueLineCount": 1,
      "OldestReceiptAgeDays": 3,
      "Values": [
        {
          "Currency": "INR",
          "ReceiptProvisionalValue": 4000.0
        }
      ]
    }
  ],
  "Filters": {
    "Queue": null,
    "DocumentId": null,
    "Page": 1,
    "PageSize": 100
  },
  "TotalRows": 1,
  "Rows": [
    {
      "Queue": "QC_HOLD",
      "DocumentId": "6e70435e-de53-499f-b17b-d7f408d26f57",
      "DocumentNumber": "GRN-26-27-000011",
      "LineId": "837f8565-8c64-480f-8545-37031601f23f",
      "AllocationId": "d22cdb99-9a02-4ccb-99ea-7d93426c12bb",
      "ItemId": "71000000-0000-0000-0006-000000000001",
      "ItemCode": "TRIAL-ITEM-001",
      "ItemName": "TRIAL Copper control cable",
      "Uom": "TRIAL-MTR",
      "Quantity": 1.0,
      "WarehouseId": "71000000-0000-0000-0007-000000000001",
      "RackBinId": "71000000-0000-0000-0008-000000000006",
      "OwnershipAccountId": "be1df41c-3cfb-4952-a91f-b980bf24301a",
      "CustodyAssignmentId": "271d8985-bfb1-1ab0-5dac-ec6f30b84345",
      "ProvenanceLayerId": "fc587586-bd01-4c9a-bc8a-e94fcf1a6929",
      "SerialId": null,
      "ReceivedAt": "2026-09-11T09:06:49.688306+05:30",
      "QcDueAt": "2026-09-13T09:06:49.688306+05:30",
      "IsOverdue": true,
      "ReceiptAgeDays": 3,
      "Currency": "INR",
      "ReceiptProvisionalValue": 4000.0,
      "DetailPath": "/api/v1/stores/goods-receipts/6e70435e-de53-499f-b17b-d7f408d26f57"
    }
  ]
}
```

### Every field

#### StoresQcStockRequest

| Field | JSON/C# type | Meaning |
|---|---|---|
| Queue | string? | Selected queue, or null for all permitted queues. In a row, identifies its source queue. |
| DocumentId | Guid? | Identifier of this source document; its kind is given by the queue/document type. |
| Page | int | One-based requested detail page. |
| PageSize | int | Requested maximum detail rows per page, 1 to 1000. |

#### StoresQcStockValue

| Field | JSON/C# type | Meaning |
|---|---|---|
| Currency | string | Native three-letter currency; purchase workload can redact it to null. |
| ReceiptProvisionalValue | decimal | Current GRN-origin held quantity times receipt unit rate; null/hidden without commercial permission. |

#### StoresQcStockTile

| Field | JSON/C# type | Meaning |
|---|---|---|
| Key | string | Stable card/period key used for selection; do not match translated titles. |
| LineCount | long | Obligations: detail source rows. QC: distinct GRN lines, not allocation-row count or units. |
| OverdueLineCount | long | Distinct QC_HOLD GRN lines past QcDueAt; pending-returnable-DC rows are not QC overdue. |
| OldestReceiptAgeDays | int? | Largest local-calendar receipt age among held GRN lines; null when empty. |
| Values | IReadOnlyList<StoresQcStockValue>? | Per-currency provisional receipt values; null if commercial access is withheld, [] if permitted but empty. |

#### StoresQcStockRow

| Field | JSON/C# type | Meaning |
|---|---|---|
| Queue | string | Selected queue, or null for all permitted queues. In a row, identifies its source queue. |
| DocumentId | Guid | Identifier of this source document; its kind is given by the queue/document type. |
| DocumentNumber | string | Human-readable number of the source document. |
| LineId | Guid | Source PO or GRN line identifier. Obligations advance rows have null because they are not item lines. |
| AllocationId | Guid | GRN lot-allocation identity; one GRN line can occupy multiple detail rows. |
| ItemId | Guid | Item identifier; null on advance rows. |
| ItemCode | string | Source item business code; null on advance rows. |
| ItemName | string | Source item description; null on advance rows. |
| Uom | string | Source unit of measure; null on advances. Never total unlike UOMs. |
| Quantity | decimal | GRNI unbilled quantity, signed spending-event quantity, or current QC held quantity; null for advances. |
| WarehouseId | Guid? | Current warehouse dimension of held balance; may be null in the contract. |
| RackBinId | Guid? | Current rack/bin dimension; may be null. |
| OwnershipAccountId | Guid | Ownership dimension; do not merge stock merely because the item matches. |
| CustodyAssignmentId | Guid | Custody dimension of held stock. |
| ProvenanceLayerId | Guid | Inventory provenance layer linking the balance to retained source evidence. |
| SerialId | Guid? | Serialized inventory identity, or null for nonserialized stock. |
| ReceivedAt | DateTimeOffset | Original GRN receipt timestamp. |
| QcDueAt | DateTimeOffset | Recorded QC deadline for the receipt. |
| IsOverdue | bool | True only for QC_HOLD past its deadline; do not substitute a browser-clock calculation. |
| ReceiptAgeDays | int | Whole company-local calendar days since receipt. |
| Currency | string | Native three-letter currency; purchase workload can redact it to null. |
| ReceiptProvisionalValue | decimal? | Current GRN-origin held quantity times receipt unit rate; null/hidden without commercial permission. |
| DetailPath | string | API resource path, NOT automatically a browser route. Use the mapping below. |

#### StoresQcStockPage

| Field | JSON/C# type | Meaning |
|---|---|---|
| CompanyCode | string | Selected ERP company code; verify it still matches the active company before rendering. |
| GeneratedAt | DateTimeOffset | Server timestamp for this projection. Show last refreshed time; not a document transaction date. |
| TimeZone | string | Server-configured company reporting timezone used for calendar dates and ages. |
| CanViewCommercialValues | bool | Whether BOTH dashboard and GRN commercial permissions permit values. |
| ValueBasis | string | Server text identifying provisional receipt valuation and exclusions. |
| Tiles | IReadOnlyList<StoresQcStockTile> | All overview cards for this endpoint; scope-filtered but not narrowed by detail filters. |
| Filters | StoresQcStockRequest | Normalised query echo. These select detail rows, not overview cards/groups. |
| TotalRows | long | Number of matching detail rows before pagination; not the number of cards or distinct documents. |
| Rows | IReadOnlyList<StoresQcStockRow> | This page of matching detail rows. Empty array is a valid result. |

### Exact denied response shape

Example caller: otherwise mapped/scoped PRODUCTION_MANAGER alone. HTTP 403.

```json
{
  "Type": "https://api.sess.example/problems/dashboard-access-denied",
  "Title": "Permission denied",
  "Status": 403,
  "Code": "DASHBOARD_ACCESS_DENIED",
  "Detail": "Stores QC stock is not permitted for your employee and selected company.",
  "TraceId": "mock-trace-403",
  "Errors": {}
}
```

## Links to existing frontend screens

Route inventory checked against frontend commit 0c59254f58bd49fc13a8b919387ba0cf5a1d9988. Always check the target page permission separately. URL-encode document numbers; GUIDs and document numbers are not interchangeable. DetailPath is an API resource path, NOT a browser link: do not blindly remove /api/v1.

| Figure/document | Existing destination |
|---|---|
| PR waiting / PR approval | /purchase/requisitions/:prNumber |
| PR stock check | /stores/stock-check/:prNumber, with its separate permission |
| RFQ without quotation | /purchase/rfqs/:rfqNumber |
| Quotation awaiting technical verification | /purchase/quotations list; there is no quotation-number detail route. Agree list preselection with the frontend lead. |
| Comparison awaiting decision | /purchase/comparisons/:comparisonNumber |
| Approved/unissued PO and open-order line | /purchase/purchase-orders/:poNumber |
| GRNI / QC stock | /stores/goods-receipts/:id |
| Gate without GRN | /stores/gate-entries/:id |
| MIR queues | /stores/material-issue-requests/:id |
| Vendor group | /vendors/:vendorCode; resolve code, do not substitute GUID |
| Item | /items/:itemCode |
| Bill spending | Prefer an in-dashboard billId detail filter. /accounts/vendor-bills/:id ONLY with separate Accounts page permission; Purchase dashboard access grants none. |
| Advance | In-dashboard documentId filter plus permitted PO/vendor links. /accounts/vendor-payments exists but is not a promised advance-detail route. |

Tiles select the corresponding queue; PR approval bands additionally set approvalRoute. Open-order aggregates can select overdueOnly/vendor/currency/root PO. Obligation groups select queue/vendor/currency. Spending groups select vendor/category/currency and month/period within allowed ranges. Preserve the overview's broader scope in the labels. QC allocation rows link to GRN, not an invented allocation/action page.

## Developer acceptance

Use all six complete bodies at the shared-client mock boundary. Exercise empty rows, null/withheld money, denied cards, full 403, 401/re-login, invalid filter, obligation integrity 409, incomplete open-order source, unconfirmed delivery, reversal events, multiple currencies and company switching. Never sum currencies or transform withheld/null into zero. Permission to read a dashboard never authorizes an approval or an Accounts/payment drill-down.

Source of truth: Application/Reporting/*Contracts.cs, Api/Endpoints dashboard routes, Infrastructure reporting services, and the PostgreSQL dashboard functions/migrations. Examples retained from item29/item30 synthetic HTTP witnesses; field/type/nullability checked against current DTOs. This is a frontend handoff only: no endpoint, tax calculation, permission or frontend implementation changed.
