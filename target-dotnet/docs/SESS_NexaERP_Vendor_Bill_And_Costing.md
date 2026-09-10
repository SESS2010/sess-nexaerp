# Vendor Bill and costing contract

Vendor Bill is financial evidence against one finalized normal GRN. Its line set must equal the GRN line set, every billed quantity must equal the received quantity, and bill number/date must equal the evidence captured at GRN. Acceptance is restricted to an effective non-SUPPORT `ACCOUNTS_MANAGER` assignment.

There is no price tolerance. A line is matched only when its unit rate and payable value exactly equal the corresponding issued Purchase Order line apportioned to the received quantity. A mismatch may be rejected. It cannot be accepted until the Purchase Order is revised and the evidence is re-entered under the resulting valid process.

## Provisional and accepted values

A FIFO layer is created atomically when a GRN is finalized. Its immutable provisional unit cost is the issued PO payable value divided by ordered quantity. Issue consumes these provisional layers immediately, so production never waits for a supplier bill. Because an accepted bill must match that PO exactly, the accepted material value and provisional material value are identical. The model therefore has no purchase-price-variance account and no configurable tolerance.

Authoritative inward charges arrive with the Vendor Bill. Recoverable GST is excluded from inventory cost. Duty, insurance, packing, handling, clearing-agent, non-creditable tax and other inward charges are allocated by item value. Freight is allocated by verified gross weight only when every bill line has a positive verified weight; otherwise the whole freight charge uses item value. A single charge never mixes allocation bases.

Bill acceptance appends immutable line-charge allocations and one immutable landed-cost adjustment for each provisional FIFO layer. The adjustment records the provisional rate, landed rate, quantity already issued, quantity remaining, and the charge value attributable to each side. It does not update the FIFO layer or any earlier consumption. The landed projection is the provisional layer plus its adjustment ledger.

Accepted Vendor Bill lines create immutable accepted-value allocations to their GRN cost layers. Each allocation separates accepted material value from allocated landed charges; a generated Actual BOM entry carries both and its total is their sum.

## Fitment before bill acceptance

A confirmed fitment is valid before its supplier bill is accepted. The generated Actual BOM records the physical component and its GRN provenance immediately, reports `PROVISIONAL_UNBILLED`, and leaves accepted material and charge values unavailable rather than inventing a final cost. When Accounts accepts the bill, the same transaction appends the landed-cost and Actual-BOM valuation adjustments. The read projection then reports `LANDED_ACCEPTED`, the accepted material value, allocated charges and their total. The original fitment and Actual BOM entry are not rewritten.

Operational and commercial baseline revisions remain frozen. Their quantities and planned values never shift when a later bill is accepted. The reported variance does shift because its Actual side has moved from unavailable/provisional to the authoritative landed value. The audit explanation is therefore: the baseline did not change; accepted actual evidence arrived later.

If a bill is never accepted after all of its GRN stock has been issued, stock custody and the immutable provisional FIFO issue evidence remain valid, but the related Actual BOM entries remain `PROVISIONAL_UNBILLED` and final actual-cost variance remains incomplete. The product currently records and exposes that unresolved state; no automatic write-off, forced acceptance or substitute valuation policy is chosen here.

## Why FIFO issue cost and Actual BOM cost may differ

FIFO stock costing answers: “which financial receipt layer leaves inventory first?” It consumes the oldest available cost layer for the item, even when a newer serial physically leaves the shelf. Serial provenance never chooses a FIFO cost layer.

The Actual BOM answers: “which physical component was fitted into this machine, and what accepted vendor bill valued that component’s receipt?” It follows the fitment component’s immutable GRN/serial provenance to an accepted bill allocation. A newer serialized component can therefore carry its own accepted-bill value in the Actual BOM while the inventory issue consumes an older FIFO layer. This difference is intentional; making either value copy the other would destroy either FIFO accounting or physical component ancestry.

## Database authority

The runtime principal has no `SELECT`, `INSERT`, `UPDATE`, or `DELETE` privilege on Vendor Bill, bill history, cost-allocation, FIFO-layer, or FIFO-consumption tables. It receives `EXECUTE` only on fixed-search-path typed `SECURITY DEFINER` mutation functions and narrow JSON projection functions. Accepted bill content is immutable. A mistaken accepted bill can only move to `REVERSED` through the controlled Accounts Manager operation, after which a corrected bill may be entered; the original lines, decision, history, and allocations are never rewritten. Rejected bills, histories, allocations, layers, and consumptions are immutable. A replay is fingerprint-bound and may return the original result only to the same currently effective assignment; it opens no mutation authority and creates no evidence.

The migration is additive over existing GRN, issue, Vendor Bill, FIFO and Actual BOM structures. It refuses any database that already contains Vendor Bills because those financial rows require an explicitly reviewed landed-cost reconciliation; the migration does not invent charges or rewrite historical evidence. Databases with no Vendor Bill rows receive four empty landed-cost evidence tables and nullable/defaulted projection columns.