# Job Order creation workflow

A Job Order is the stable identity of exactly one machine. Production initiates it from one unit of a current Customer PO line; Accounts independently confirms that the customer order and machine scope are correct. The Job Order remains `PENDING_ACCOUNTS` and cannot be used for Estimated BOM, Production BOM, or a job-backed MIR until confirmation changes it to `OPEN`.

The unique `(CompanyId, CustomerPurchaseOrderLineId, MachineOrdinal)` identity means a Customer PO line for quantity two creates machine ordinals one and two as two Job Orders. A chamber line and cooling-tower line therefore create two Job Orders, not one combined order. Machine model and customer name are server-derived snapshots; the read model also retains the Customer PO header, current revision, line, item, and machine ordinal identifiers.

Production initiation accepts `PRODUCTION_COORDINATOR` or `PRODUCTION_MANAGER`. Accounts confirmation accepts `ACCOUNTS_ASSISTANT` or `ACCOUNTS_MANAGER`, must be performed by a different employee, and records both resolved assignment IDs and assignment types. Both steps are idempotent ordinary commands, commit their receipts in the business transaction, write the ordinary audit row, and append immutable Job Order history.

Legacy Job Orders remain readable by existing internal relationships but have no fabricated Customer PO provenance. The governed endpoint returns only governed rows. The migration is additive; Down refuses once any governed Job Order or its history exists.