# UTC timestamp checklist — 24 September 2026

Every `DateTimeOffset` the API binds from a request body must reach it in UTC (`…Z`). Npgsql
refuses any other offset against `timestamp with time zone`; since `7003c02` that surfaces as
`500 INTERNAL_ERROR` with a TraceId, and the cause is only in the API log.

**How the list was built.** From source on `origin/main` (`2149d27`), not from a count: every
record with a `DateTimeOffset` parameter that a `MapPost`/`MapPut`/`MapPatch` handler takes as its
body. Result: **13 fields**. Checked as well, and empty: `DateTimeOffset` fields nested inside
those requests (11 nested types, none carry one), and `DateTimeOffset` query parameters (none).
The manager's finding #31 table was not on `main` when this was written; this list stands on
its own.

**The frontend pattern.** Every screen holds a `datetime-local` value and sends
`new Date(value).toISOString()`, which is always UTC.

| # | Endpoint | Field | Screen that sends it | Sends UTC |
|---|---|---|---|---|
| 1 | `POST /api/v1/purchase/rfqs` | `QuoteDueAt` | `features/purchase/RfqCreateModal.tsx:77` | ✅ |
| 2 | `POST /api/v1/purchase/rfq-invitations/{id}/quotations` | `ReceivedAt` | `features/purchase/QuotationPage.tsx:197` | ✅ |
| 3 | `POST /api/v1/stores/gate-entries` | `ArrivedAt` | `features/stores/GateEntryFormModal.tsx:147` | ✅ |
| 4 | `PUT /api/v1/stores/gate-entries/{id}` | `ArrivedAt` | same modal, same line | ✅ |
| 5 | `POST /api/v1/stores/goods-receipts` | `ReceivedAt` | `features/stores/GoodsReceiptFormModal.tsx:391` | ✅ |
| 6 | `PUT /api/v1/stores/goods-receipts/{id}` | `ReceivedAt` | same modal, same line | ✅ |
| 7 | `POST /api/v1/qc/inspections` (finalize) | `InspectionStartedAt` | `features/qc/QcDispositionForm.tsx:184` → `QcInspectPage.tsx:80` | ✅ |
| 8 | `POST /api/v1/qc/inspections/{number}/corrections` | `InspectionStartedAt` | `QcDispositionForm.tsx:184` → `QcInspectionPage.tsx:87` | ✅ |
| 9 | `POST /api/v1/stores/material-issues/from-request/{requestId}` | `IssuedAt` | `features/stores/MaterialIssueFormModal.tsx:111` | ✅ |
| 10 | `POST /api/v1/stores/material-returns/from-issue/{materialIssueId}` | `DeclaredAt` | `features/stores/MaterialReturnFormModal.tsx:121` | ✅ |
| 11 | `POST /api/v1/stores/material-returns/{id}/accept` | `AcceptedAt` | `features/stores/MaterialIssueDetailPage.tsx:81` (`new Date().toISOString()`) | ✅ |
| 12 | `POST /api/v1/production/component-fitments` | `FittedAt` | `features/production/ComponentFitmentFormModal.tsx:112` | ✅ |
| 13 | `POST /api/v1/stores/machine-deliveries/{id}/signature` | `DeliveredAt` | **DC screen, not built yet** (starts 29 Sep) | ⏳ contract requires UTC (`bc0a26d`) |

Handler files: `Rev869BPurchaseEndpoints.cs`,
`StoresGateEntryEndpoints.cs`, `StoresGoodsReceiptEndpoints.cs`, `QcEndpoints.cs`,
`MaterialIssueEndpoints.cs`, `FitmentActualBomEndpoints.cs` and `MachineDeliveryEndpoints.cs`.

## What this means

- **The failing RFQ on 23 Sep did not come from a screen.** It came from a hand-written
  API request with `+05:30`. The RFQ screen already sent UTC.
- Twelve of thirteen are UTC today and need no change. Row 13 is built on 29 Sep against the
  DC contract, which already says UTC.
- The backend converter the manager mentions is still worth having: it protects the setup
  wrapper, scripts and any future client, which this checklist does not cover.
