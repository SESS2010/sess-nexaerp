# Vendor Bill and costing contract

Vendor Bill is financial evidence against one finalized normal GRN. Its line set must equal the GRN line set, every billed quantity must equal the received quantity, and bill number/date must equal the evidence captured at GRN. Acceptance is restricted to an effective non-SUPPORT `ACCOUNTS_MANAGER` assignment.

There is no price tolerance. A line is matched only when its unit rate and payable value exactly equal the corresponding issued Purchase Order line apportioned to the received quantity. A mismatch may be rejected. It cannot be accepted until the Purchase Order is revised and the evidence is re-entered under the resulting valid process.

## Provisional and accepted values

A FIFO layer is created atomically when a GRN is finalized. Its provisional unit cost is the issued PO payable value divided by ordered quantity. Because an accepted bill must match that PO exactly, the accepted value and provisional value are identical. The model therefore has no purchase-price-variance account and no configurable tolerance.

Accepted Vendor Bill lines create immutable accepted-value allocations to their GRN cost layers. Each allocation separates accepted material value from accepted allocated charges; a generated Actual BOM entry carries both and its total is their sum. Those allocations are the source used by the generated Actual BOM once confirmed fitment exists.

## Why FIFO issue cost and Actual BOM cost may differ

FIFO stock costing answers: “which financial receipt layer leaves inventory first?” It consumes the oldest available cost layer for the item, even when a newer serial physically leaves the shelf. Serial provenance never chooses a FIFO cost layer.

The Actual BOM answers: “which physical component was fitted into this machine, and what accepted vendor bill valued that component’s receipt?” It follows the fitment component’s immutable GRN/serial provenance to an accepted bill allocation. A newer serialized component can therefore carry its own accepted-bill value in the Actual BOM while the inventory issue consumes an older FIFO layer. This difference is intentional; making either value copy the other would destroy either FIFO accounting or physical component ancestry.

## Database authority

The runtime principal has no `SELECT`, `INSERT`, `UPDATE`, or `DELETE` privilege on Vendor Bill, bill history, cost-allocation, FIFO-layer, or FIFO-consumption tables. It receives `EXECUTE` only on fixed-search-path typed `SECURITY DEFINER` mutation functions and narrow JSON projection functions. Accepted bill content is immutable. A mistaken accepted bill can only move to `REVERSED` through the controlled Accounts Manager operation, after which a corrected bill may be entered; the original lines, decision, history, and allocations are never rewritten. Rejected bills, histories, allocations, layers, and consumptions are immutable. A replay is fingerprint-bound and may return the original result only to the same currently effective assignment; it opens no mutation authority and creates no evidence.

The migration refuses an existing database containing finalized normal GRNs or material issues. Such a database requires an explicitly reviewed historical-cost reconciliation; this migration does not invent financial history.