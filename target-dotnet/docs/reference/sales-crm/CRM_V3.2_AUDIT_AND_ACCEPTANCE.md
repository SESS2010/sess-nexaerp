# SESS Sales CRM V3.2 — Audit and Acceptance

## Verdict

**APPROVED WITH CORRECTIONS / LIVE ACCEPTANCE REQUIRED**

## Implemented controls

- One common FY sequence for Sales, Service and Spares.
- Normal format: `SESS/OFR/0941/2026-27`.
- Revision format: `SESS/OFR/0941/R01/2026-27`.
- New FY starts at `0001`; current FY continues from the highest recognizable existing running number.
- Number allocation remains inside the document lock and occurs only after mandatory validation.
- Legacy numeric and earlier `SESS-OFR-YY-NNNN` IDs are recognized when initializing the FY counter.
- Revision retains the base relationship and carries the revision financial year in the visible number.
- Current Ledger page defaults to Current FY and supports Last 3, 6 and 12 Months.
- Ledger View routes to the existing controlled Edit, Revise, Stage, Print and Communication workflow.
- Search/Edit remains a separate function for specific and older records.
- Business Type and Department continue to distinguish Sales, Service and Spares; no fragmented SPR/SCR sequence is introduced.

## Required live tests

1. Run `setupSystem()` and record the highest current-FY legacy number.
2. Save two valid offers simultaneously from different accounts; confirm sequential, non-duplicate numbers.
3. Start an invalid/incomplete entry before another valid save; confirm the invalid entry receives no number.
4. Revise the latest offer; confirm R01, then revise again and confirm R02.
5. Confirm the quotation, ledger, search, email subject/history and audit log all show the same offer ID.
6. Open Current Ledger and verify Current FY / 3 / 6 / 12-month filters against the All Leads rows.
7. Confirm Viewer cannot edit/revise and direct server-function calls are rejected.

Final production approval requires screenshots and saved-sheet evidence for these tests.
