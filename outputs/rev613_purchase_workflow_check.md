# REV613 Purchase Workflow Check

Checked installed ERP:

- `C:\Users\User\AppData\Local\SESS NexaERP\app\InventoryERP_Software.html`
- Running URL: `http://127.0.0.1:8783/InventoryERP_Software.html`

## Result

Purchase process is mostly working at workflow level, but it is not fully strict against the final reviewed requirement because six control gaps remain.

## Working / Passed

- Purchase Request screen and save flow exist.
- RFQ screen and save flow exist.
- RFQ minimum vendor field defaults to `3`.
- Vendor Offer Entry exists.
- Vendor portal quote submission/upsert exists.
- Vendor Offer Finalisation / Comparison exists.
- Vendor selection history exists.
- PO creation from final vendor selection exists.
- PO vendor approval/active check exists.
- PO confirmation screen exists.
- PO confirmation can update PO acknowledgement.
- Purchase follow-up combines PO, confirmation and GRN.
- Material pending list combines PR, PO and GRN.
- GRN updates PR `grnStatus` and `grnNumber`.
- QC Vendor Rating after GRN exists.
- Vendor performance combines vendor ratings, purchase orders and GRN.
- Purchase cost comparison history exists.
- PostgreSQL fast mirror hooks exist for purchase ledgers.
- Live ERP page serves purchase screens and `Software REV613`.

## Needs Fix Before Final Purchase Sign-Off

1. PO duplicate number is not strictly blocked.
2. RFQ does not strictly enforce 3 unique vendor names before save.
3. Vendor comparison/finalisation does not strictly block fewer than 3 unique vendor offers.
4. Manual vendor quote can duplicate the same vendor/RFQ/PR line instead of updating the same offer with negotiation history.
5. Main PO confirmation handler does not strictly require the PO to be Approved/Released/Sent before confirmation.
6. PO save does not strictly require a final vendor selection/comparison row.

## Recommendation

Apply a focused purchase-control revision after approval. Suggested scope:

- Enforce minimum 3 unique vendors on RFQ.
- Enforce minimum 3 unique vendor offers before comparison/finalisation.
- Upsert same vendor/RFQ/PR quote and store negotiation history.
- Block duplicate PO numbers.
- Require final vendor selection before PO save.
- Require PO Approved/Released/Sent/Acknowledged before PO confirmation.
