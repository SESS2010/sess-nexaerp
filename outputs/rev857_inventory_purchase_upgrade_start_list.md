# REV857 Inventory + Purchase Upgrade Start List

Date: 2026-08-08

## Base Confirmed

Correct continuation base:
- Installed ERP: `Software REV857`
- Server revision: `REV857`
- PostgreSQL/local server: running on port `8783`
- Health endpoint: PASS after startup warm-up fix

Important correction:
- Do not continue from old REV622.
- Continue from the latest REV857 PostgreSQL/.NET-backed ERP line.

## Already Available In ERP

Inventory / Store modules already present:
- Item Master
- Product / Finished Goods Master
- BIN / Rack Master
- Material Request
- Inspection Note Before GRN
- GRN / Receive GRN
- Daily Material Movement
- Material Transfer Note
- Material Issue to Project
- Material Return from Project
- Stock Register / Store Ledger
- Stock Adjustment
- Inventory Aging Report
- Minimum Stock Alert
- Customer Material Inward / Outward / Repair
- Tools Master, Tool Issue, Tool Return, Store Tool Register, Engineer Tool Register

Purchase modules already present:
- Purchase Request
- RFQ / Vendor Quotation
- Vendor Offer Entry
- Negotiation Update
- Vendor Comparison
- Purchase Order
- PO Confirmation
- Purchase Follow-up
- Material Pending List
- Vendor Performance
- Purchase Cost Comparison History
- Vendor Portal quotation flow

## Manufacturing Inventory Gaps To Add / Strengthen

Priority 1:
- Monthly stock statement with opening, inward, outward, closing, adjustment, and value.
- Minimum stock statement with reorder quantity, lead time, shortage risk, and purchase action.
- Item-wise stock card / bin card view.
- Project-wise material issue vs BOM consumption.
- Reserved stock / allocated stock for project, production, and service.
- GRN pending inspection and rejected/quarantine stock view.

Priority 2:
- ABC analysis by value and FSN analysis by movement.
- Slow-moving / non-moving stock ageing.
- Batch/serial number traceability where applicable.
- Stock valuation method display: average rate / last purchase rate / manual valuation.
- Store inward/outward monthly trend chart.
- Stock adjustment audit and approval summary.

Priority 3:
- Physical stock verification sheet.
- Cycle count plan and variance report.
- Rack/bin capacity and location utilization.
- Tool calibration/damage/lost cost summary.
- Scrap/rework/return-to-vendor material register.

## Purchase Department Gaps To Add / Strengthen

Priority 1:
- Purchase dashboard covering PR to RFQ to PO to GRN.
- PR ageing: pending, approved, RFQ created, PO created, closed.
- RFQ 3-vendor compliance view.
- Vendor comparison approval status.
- PO pending confirmation and delivery commitment tracker.
- PO vs GRN pending quantity and value.
- Top pending purchase items by required date.

Priority 2:
- Vendor lead-time performance.
- Top vendors by purchase value.
- Vendor rejection/quality trend.
- Price variance: last purchase price vs current PO price.
- Emergency purchase / non-standard purchase register.
- Advance/payment terms tracker for purchase orders.

Priority 3:
- 3-way match: PO vs GRN vs vendor invoice.
- Purchase budget vs actual.
- Vendor category-wise spend.
- Open order liability report.
- Material shortage reason analysis.

## Dashboard Required

Inventory dashboard should show:
- Total inventory value
- Stock items count
- Below minimum stock count
- Critical shortage items
- Slow-moving / non-moving stock
- GRN pending inspection
- Rejected / quarantine stock
- Project reserved stock
- Monthly inward vs outward trend
- Top 10 high-value stock items
- Top 10 fast-moving items
- Top 10 shortage items

Purchase dashboard should show:
- Open PR count
- Pending approval PR count
- RFQ pending count
- Vendor quotes pending count
- PO pending confirmation count
- PO overdue delivery count
- PO value this month
- GRN pending against PO
- Top 10 vendors by value
- Vendor on-time delivery percentage
- Purchase price variance
- Material pending by project

Recommended graphs:
- Monthly stock inward/outward trend
- Monthly purchase value trend
- PR ageing bucket chart
- PO delivery status chart
- Minimum stock criticality chart
- Vendor performance ranking
- Item category stock value chart
- Project-wise material consumption chart

## Implementation Start Plan

Batch 1:
- Create Inventory Control Dashboard.
- Create Monthly Stock Statement.
- Strengthen Minimum Stock Statement.
- Add project-wise issue vs BOM summary.

Batch 2:
- Create Purchase Control Dashboard.
- Add PR ageing and RFQ/PO/GRN status pipeline.
- Add vendor top list and vendor lead-time summary.

Batch 3:
- Add stock valuation and movement analysis.
- Add slow/non-moving stock analysis.
- Add 3-way PO-GRN-Invoice match view.

## Work Started

Completed first technical recovery step:
- Restored stable REV857 base after the old REV622/REV858 menu confusion.
- Fixed server boot responsiveness by making PostgreSQL cache warm-up opt-in.
- Verified `/api/health` returns `200` on REV857.

Next implementation target:
- Batch 1 Inventory Control Dashboard + Monthly Stock Statement + Minimum Stock Statement.
