# Stores dashboard — mock acceptance record

Checked on 23 September 2026 against `dashboard-frontend-contract.md` (22 Sep), endpoints
`/api/v1/dashboards/stores/workload` and `/api/v1/dashboards/stores/qc-stock`, using the
two synthetic bodies `stores-workload.json` and `stores-qc-stock.json` (read, never edited).
Same method as the Purchase record: browser on the dashboard mock server, no backend,
observations read from the rendered page. Every check also asserted the page URL.

## MUST DO AT THE 28 SEPTEMBER REBASE

The four items at the top of `PURCHASE-ACCEPTANCE-RECORD.md` apply here unchanged:
company header, BOTH companies, a real 401 redirect, and a company switch with a slow
response in flight. The Stores page uses the same `useDashboardQuery` guard.

## DECISION PENDING — stock dimensions

QC stock rows give warehouse, rack/bin, ownership, custody, provenance and serial as GUIDs
only; the contract carries no names or codes and the app has no warehouse lookup screen.
**Current default:** a short labelled ID (`Warehouse ID …000001`) with the full GUID on
hover, in one component (`DimensionId` in `DashboardParts.tsx`). A storekeeper cannot
recognise a location from this. Options: accept for now; or ask the backend to add
codes/names to the row; or show dimensions only on hover. Change `DimensionId` only.

## Contract points that needed a frontend choice

- QC tiles have a Key only, no Title. Labels are ours: QC_HOLD → "Held for QC",
  PENDING_RETURNABLE_DC → "Pending returnable DC".
- A `/qc/inspect/:allocationId` screen exists, but the contract says QC allocation rows
  link to the GRN only. Followed the contract: no link to the inspection action.
- `IsOverdue` is the server's verdict and is displayed as given; never recomputed from the
  browser clock.

## States, by variant (session = td unless stated)

| Variant | Stores workload | QC and held stock | Pass |
|---|---|---|---|
| `reference` | The reference body's gate card is ACCESS_DENIED: "1 queue is not available to you — withheld, not zero"; gate tile "Permission denied / Withheld". MIR approval 1; row shows "Any of: STORES_MANAGER, PRODUCTION_MANAGER" and the server's "No named approver is assigned by the current MIR workflow." — no invented person. | Held for QC "1 GRN line", "1 line past QC due", ₹4,000.00; row "QC overdue"; Basis text plus "provisional receipt value before taxes and charges … not Actual BOM cost … no paid or unpaid information". | ✅ |
| `slow` | Loading at 1 s, loaded at 5 s. | Loading at 1 s, loaded at 5 s. | ✅ |
| `empty` | Denied gate card stays "Withheld" (not 0); **Empty** "Nothing is waiting in Stores"; ready tiles 0. | **Empty** "No stock is held for QC or a returnable DC"; tiles "0 GRN lines"; no ₹0. | ✅ |
| `withheld` | (unchanged) | **Permission denied** "Values are withheld from your role"; tile value "Withheld"; row value "Withheld" (tooltip "Not zero…"); no ₹ anywhere in the section. | ✅ |
| `stores-all-queues` | All three cards READY (1/1/1). Gate entry links to `/stores/gate-entries/<id>` with vendor shown; MIRs link to `/stores/material-issue-requests/<id>`. | — | ✅ |
| `qc-split` | — | One GRN line split over two warehouses shows as TWO rows while the card still says "1 GRN line" (₹14,000.00 = 4,000 + 10,000). Pending returnable DC line shows no "QC overdue" and the card says "Not subject to QC overdue". Missing rack/bin shows "—". | ✅ |
| `multi-currency` | — | Held for QC shows ₹4,000.00 and $612.40 on separate lines, never added; only the server-flagged row is "QC overdue". | ✅ |
| `denied-403` | Exact Detail "Stores workload is not permitted…", TraceId. | Exact Detail "Stores QC stock is not permitted…", TraceId. | ✅ |
| `unauthenticated-401` | "Not signed in" + sign-in link. | Same. | ✅ |
| `server-500` | "Could not load this section", TraceId, no exception text. | Same. | ✅ |
| `company-mismatch` | Discarded: "Response discarded: it belongs to a different company"; 0 tiles, 0 rows. | Same. | ✅ |

## Permissions (session profiles)

| Profile | Result | Pass |
|---|---|---|
| `td` | Both sections; nav shows Purchase and Stores. | ✅ |
| `stores-exec` | Both sections; nav shows Stores only; MIR and GRN links present. | ✅ |
| `stores-no-grn` (QC key but no `inventory.grn:view`) | Workload only: QC stock needs its key AND GRN view. | ✅ |
| `production-manager` (PRODUCTION_MANAGER alone) | No sections, no Dashboards nav: "not available to your role". | ✅ |

## Detail filters narrow rows only

| Action | Result | Pass |
|---|---|---|
| QC › click "Pending returnable DC" card (`qc-split`) | Rows 3 → 1 (GRN-26-27-000110); both cards unchanged; note "Detail filtered … overview is not filtered". | ✅ |

## Fixed during checking

- At narrow widths GRN and item codes broke across four lines ("GRN- / 26-27- / 000011")
  and dimension IDs split mid-value. Codes, quantities and IDs no longer wrap; the table
  scrolls sideways instead.

## Observed, not an app fault

- Twice the browser pane jumped to `/dashboards/purchase` during a wait. The jumped page
  had navigation type `navigate` with no referrer (a fresh top-level load from outside the
  page), which matches the pane reloading its first address `/__mock-signin`. With
  navigation logging armed, neither a tile click nor 8 idle seconds reproduced it. All
  checks above asserted the URL afterwards.
- Console errors seen before a fresh load (`countOf is not defined`, 503s, a connection
  reset) came from hot reloads mid-edit and the mock server restarting after config edits.
  On a fresh load the only API requests are two `GET /api/v1/session/me` (React dev
  double-mount), both 200.
