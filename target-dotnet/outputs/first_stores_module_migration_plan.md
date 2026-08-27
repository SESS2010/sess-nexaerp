# SESS First Stores Module - Four-Stage Migration Plan

Status: Parts 1 and 2 witnessed; Part 3A migration prepared but not applied; Part 3B pending
Schema authority: `outputs/first_stores_module_schema_design.md`  
Architecture pattern authority: `outputs/backend_architecture_reference.md`

## 1. Decision and safety boundary

Use four sequential, independently transactional migration stages. The final schema contains 24 new tables. The originally planned Part 3 is split into Part 3A and Part 3B at the document/ledger activation boundary.

The approved four-stage split is:

| Part | Boundary | New tables | Existing-table work |
|---:|---|---:|---|
| 1 | Foundation, Gate Entry and notification engine | 9 | Reuse sequence/audit/role/Purchase/location masters; extend condition vocabulary only |
| 2 | GRN and serial identity/capture | 4 | Add later-document FKs to status history only after their parents exist |
| 3A | QC and outbound document graph, database-blocked from finalisation/posting | 10 | Complete status-history FKs; preserve the Part 2 GRN block |
| 3B | Append-only ledger contract and activation | 1 | Create posting batches, harden `stock_movements`, then replace temporary blockers |
| **Total** |  | **24** |  |

The approved 3A/3B split is safer to witness than one large Part 3 migration. Part 3A creates the complete cyclic QC/DC document graph in one transaction, but database guards prohibit every posting, dispatch and finalisation path. The Part 2 GRN finalisation block remains in force because `stock_posting_batches` does not yet exist. Documents may be drafted and their FK graph witnessed, but no Part 3A document can create an inventory consequence.

Part 3B is the sole ledger activation boundary. It creates `stock_posting_batches`, hardens `stock_movements`, installs the complete typed-source and atomic-posting contract, and only then replaces the temporary Part 2/3A blockers. There is no witness point where a finalised source can exist without its append-only ledger contract.

“Independently applicable and rollback-able” means each stage has its own migration identity, transaction, preflight, post-apply witness, and down path, while respecting dependency order. Part 2 applies only after Part 1; Part 3A only after Parts 1 and 2; Part 3B only after Part 3A. Rollback order is 3B, 3A, 2, then 1. A down path fails closed rather than deleting business data.

## 2. Row-count notation and fresh-chain assumption

- `C` = count of active companies when Part 1 applies.
- `S` = count of pre-existing `advance.stock_movements` rows when Part 3 applies.
- “Fresh chain” means the existing authoritative baseline and prior Purchase/multi-company migrations are present, but no Stores-module business commands have run.
- Only the two required default configuration versions are seeded: `SERIAL_CAPTURE_THRESHOLD = 5000` and `QC_COMPLETION_DAYS = 2`, once per active company. New-company bootstrap must create the same initial versions later.
- Category routes, company Item settings, document numbers and notification events are not invented by migration seed data. Their expected count is zero until authorised setup or business use.

## 3. Part 1 - foundation and inbound

### 3.1 Objects created

| # | Table | Initial purpose |
|---:|---|---|
| 1 | `business_rule_configuration_versions` | Effective-dated, audited Stores rule values and document-snapshot sources. |
| 2 | `item_company_inventory_settings` | Company barcode sequence/value and serial-capture override for shared Items. |
| 3 | `store_category_routes` | Exactly one active QC route per company and Item category. |
| 4 | `gate_entries` | Immutable/reversible PO-linked arrival header. |
| 5 | `gate_entry_lines` | Delivered quantity by PO line. |
| 6 | `stores_document_status_history` | Append-only lifecycle evidence; Part 1 initially enables only `GateEntryId`. |
| 7 | `notification_events` | Generic immediate/scheduled/cancellable role-targeted event/outbox. |
| 8 | `notification_recipients` | Resolved company-role recipients, inbox visibility and read state. |
| 9 | `notification_delivery_attempts` | Append-only in-app and email delivery attempts. |

Part 1 also adds the `PENDING_RETURNABLE_DC` condition-code vocabulary required by later posting, without manufacturing a rack mapping. It reuses `purchase_number_sequences`; no Stores-specific sequence table is created.

### 3.2 Dependencies

- Existing company, employee, company-role membership, Item, Item category, UOM, warehouse, rack/bin, warehouse-condition-location and controlled-history foundations.
- Existing Purchase Order and Purchase Order Line company-safe alternate keys/FKs.
- Existing Purchase sequence allocation and audit/correlation patterns.
- No dependency on any Part 2 or Part 3 table.

Role resolution uses active role assignments for the event's `CompanyId`. The notification schema stores role targets and their resolved users; it never seeds or references PRIYA E, TD or MD by hard-coded employee ID.

### 3.3 Applicability and usability after apply

The database and all pre-existing ERP functions remain usable. Part 1 can safely persist configuration, routes, Gate Entry drafts/finalisation, generic notification events, inbox rows, read actions and delivery attempts. It cannot create a GRN, serial, QC decision, stock posting, MIR, Job Order or DC yet. Gate Entry availability should therefore be feature-gated unless the business accepts a deliberate pause at finalised Gate Entry.

Notification producers for later parts may not publish source events until their source transactions exist. The engine itself can be witnessed with a controlled test event and rolled back with the transaction used by the witness fixture.

### 3.4 Expected fresh-chain row counts

| Table | Expected rows immediately after apply |
|---|---:|
| `business_rule_configuration_versions` | `2 x C` |
| The other eight new Part 1 tables | `0` each |

No existing transactional row count changes. If either configuration key already exists for a company during a reapply/recovery scenario, idempotent seed logic must insert nothing for that company/key and the witness must still report exactly one initial effective version.

### 3.5 Witness and rollback gate

Witness apply in one transaction, then verify table/column/PK/FK/index/check/trigger inventory, the `2 x C` configuration rows, unique active category-route enforcement, Gate company/PO-line containment, notification event idempotency, role/company resolution, explicit read ownership, and append-only delivery attempts. Verify a due event can be claimed once, a cancelled event cannot activate, and a missing mandatory role yields `RECIPIENT_BLOCKED` rather than a silent send.

Rollback is allowed only when Parts 2 and 3 are absent and all Part 1 business tables are empty except the exact untouched seed configuration rows. The down path verifies that condition, removes only those seed rows, drops Part 1 objects in FK-safe order, and restores the prior condition vocabulary. Any Gate, route, Item setting, notification or changed configuration row makes rollback abort without deleting data.

## 4. Part 2 - GRN and serials

### 4.1 Objects created

| # | Table | Initial purpose |
|---:|---|---|
| 1 | `goods_receipts` | One Gate/one effective GRN and one company-unique effective vendor bill, including QC deadline snapshot. |
| 2 | `goods_receipt_lines` | PO-authorised receipt, segregated excess, Item/commercial snapshots and initial warranty. |
| 3 | `inventory_serials` | Durable company-unique serial identity across corrections. |
| 4 | `goods_receipt_line_serials` | GRN serial occurrence, duplicate warning/disambiguation and ordered/excess disposition. |

Part 2 adds `GoodsReceiptId` to `stores_document_status_history` only after `goods_receipts` exists. It does not yet add QC, Job Order, MIR or DC history FKs.

### 4.2 Dependencies on Part 1

- Finalised effective Gate Entry and Gate Entry Lines.
- Snapshottable `SERIAL_CAPTURE_THRESHOLD` and `QC_COMPLETION_DAYS` configuration versions.
- Company Item settings, category routes, Purchase Orders/lines, vendors, Items and receipt locations.
- Gate-only status history and existing number sequence allocation.

### 4.3 Applicability and usability after apply

The database and all earlier functions remain usable. Gate and GRN drafts, bill uniqueness, line snapshots, excess segregation, serial validation/disambiguation and warranty facts can be persisted and witnessed. Production GRN finalisation must remain feature-disabled until Part 3 exists, because finalisation is required to atomically create a `stock_posting_batch` and ledger custody movements. The plan does not permit a “finalised but unposted” GRN intermediate state.

### 4.4 Expected fresh-chain row counts

All four new Part 2 tables contain `0` rows immediately after apply. Part 1 row counts remain unchanged. No placeholder GRN, serial, bill or status-history row is seeded.

### 4.5 Witness and rollback gate

Verify one-Gate/one-effective-GRN, company/bill effective uniqueness, tenant-safe parent FKs, immutable Item snapshots, `UnitRateSnapshot = LineValueSnapshot / ReceivedQuantity`, received/excess reconciliation, initial bill-plus-13-month warranty, serial-threshold snapshot, durable serial uniqueness and required duplicate disambiguation. Use transaction-scoped fixtures and roll them back.

Rollback is allowed only when Parts 3A and 3B are absent and all four Part 2 tables plus GRN status-history rows are empty. The down path first removes the GRN FK/column from status history, then drops serial occurrence, serial identity, GRN line and GRN header tables. Any persisted GRN/serial data causes a fail-closed abort. Part 1 remains applied and usable.

## 5. Part 3A - QC and outbound document graph

### 5.1 Objects created

| # | Table | Initial purpose |
|---:|---|---|
| 1 | `qc_inspections` | Stable logical inspection per GRN or QC-required DC-return line. |
| 2 | `qc_inspection_revisions` | Immutable initial/correction decisions and quantity disposition. |
| 3 | `qc_inspection_parameter_results` | Optional normalised per-policy, per-sample evidence. |
| 4 | `qc_inspection_serial_dispositions` | Accepted/rejected decision per serialised unit. |
| 5 | `job_orders` | Minimal stable machine/customer identity and installation date. |
| 6 | `material_issue_requests` | One-destination approved issue authority. |
| 7 | `material_issue_request_lines` | Multi-Item request lines and typed issue sources. |
| 8 | `stores_approval_history` | Append-only MIR/DC decisions. |
| 9 | `delivery_challans` | Returnable/non-returnable outbound and inbound-return lifecycle. |
| 10 | `delivery_challan_lines` | Typed material sources, quantities, serials, weights and return reconciliation. |

Part 3A completes the QC/Job Order/MIR/DC typed columns in `stores_document_status_history`. It does not create `stock_posting_batches` and does not modify `stock_movements`.

### 5.2 Dependencies on Parts 1 and 2

- All Part 1 company settings, routes, status evidence, notification engine, role resolution and Gate foundations.
- All Part 2 GRN, line, warranty and serial foundations.
- Existing optional `qc_inspection_policies`, Items, employees, departments, customers, vendors, locations, stock reservations and role/permission data.
- Existing `stock_movements` and its authoritative baseline shape.

The internal DDL order resolves the deliberate QC/DC relationship safely: create Job Order and MIR; create approval/DC headers and lines; create QC logical/revision/result/disposition tables; then add the cross-direction QC-to-DC and DC-to-QC FKs. All steps are inside one migration transaction.

### 5.3 Applicability and usability after Part 3A

All pre-existing ERP functions remain usable. Part 3A documents may be created as drafts so the complete FK graph can be witnessed. Database triggers reject QC `FINALIZED`, MIR issue/fulfilment states, DC dispatch/return-receipt/final states, and any attempt to derive an inventory consequence. The Part 2 GRN finalisation block remains unchanged and effective. Feature endpoints for those transitions remain disabled.

### 5.4 Expected fresh-chain row counts after Part 3A

All ten new Part 3A tables contain `0` rows immediately after apply. Parts 1 and 2 retain their witnessed counts. No QC, Job Order, MIR, approval, DC, status-history, notification or ledger row is seeded.

### 5.5 Part 3A witness and rollback gate

Witness the exact ten-table catalog, complete tenant-safe FK graph including the QC/DC cycle, checks, indexes, append-only evidence guards, draft-write paths, and negative database tests for every prohibited finalisation/posting/dispatch path. Verify `stock_posting_batches` is absent, `stock_movements` is unchanged, and a Part 2 GRN still cannot finalise.

Rollback is permitted only while Part 3B is absent and every Part 3A table and Part 3A status-history reference is empty. Down removes the Part 3A status-history FKs/columns and ten tables in dependency-safe order. Any persisted Part 3A business or history row makes rollback fail closed.

## 6. Part 3B - append-only ledger contract and activation

### 6.1 Object created

| # | Table | Initial purpose |
|---:|---|---|
| 1 | `stock_posting_batches` | Atomic/idempotent posting and reversal grouping. |

### 6.2 Dependencies

Part 3B requires witnessed Parts 1, 2 and 3A, all temporary database blockers, the complete QC/DC document graph, and the authoritative existing `stock_movements` shape.

### 6.3 `stock_movements` transition

1. Widen `QuantityIn`/`QuantityOut` to `numeric(24,6)`; this is non-lossy upward.
2. Add `LedgerSchemaVersion smallint NOT NULL DEFAULT 1`; pre-existing rows remain version 1 and all Stores-module inserts must explicitly use version 2.
3. Add nullable location-condition, posting-batch, ordinal, leg, typed-source, receipt-provenance, serial, reversal and posting-identity columns plus their FKs/indexes.
4. Do not invent source or location provenance for legacy rows. Promote a version-1 row only when every required version-2 fact is deterministically provable; otherwise leave it immutable as version 1.
5. Add conditional constraints/triggers: every version-2 row requires one typed source, batch, location/condition, movement leg and posting identity; all versions reject update/delete after the controlled migration transaction.
6. Enable the balanced-batch, exact-reversal, serial-quantity, source-balance and company-containment guards before enabling module write endpoints.

This staged version rule is safer than making new columns immediately `NOT NULL`, because no database inspection has authorised an assumption that every historical ledger row can be mapped to a real new source line.

### 6.4 Applicability and usability after apply

After the Part 3 transaction and witness pass, the full scoped module is usable end to end: Gate Entry, GRN, QC with or without policies, accepted/rejected/excess custody, approved issues, minimal Job Orders, bidirectional DCs, scheduled/immediate notifications, serial provenance, and append-only ledger reversals. Existing functions remain usable. Feature enablement occurs only after every Part 3 constraint and witness succeeds; there is no interval where finalisation can bypass posting.

The explicitly documented ISO gaps and deferred modules remain unavailable.

### 6.5 Expected fresh-chain row counts

| Object | Expected rows immediately after apply |
|---|---:|
| `stock_posting_batches` | `0` |
| `stock_movements` | `S` (unchanged) |
| Version-2 `stock_movements` | `0` |

No QC policy, Job Order, MIR, DC, approval, posting batch, notification or ledger business row is seeded. Part 1 remains at `2 x C` configuration rows and zero in its other new tables; Part 2 remains zero.

### 6.6 Witness and rollback gate

Witness the empty-policy QC path, optional parameter samples, per-line and per-serial reconciliation, correction revision immutability, accepted transfer to selected available rack, rejected/excess pending-return custody, MIR approved-only issue, returnable/non-returnable DC rules, partial return, subcontract weight/scrap evidence, four notification scenarios, role/company recipient resolution, posting idempotency, balanced legs, typed source FKs, exact reversal, serial provenance, and update/delete rejection. Verify legacy version-1 ledger rows remain byte-for-byte equivalent in their original columns and are not assigned fabricated sources.

Rollback requires module writes to be quiesced and fails unless all posting batches and version-2 ledger rows are empty and no Part 3A document has crossed an activated transition. It restores the Part 2/3A database blockers before removing ledger guards/indexes/FKs and added columns in dependency order. Before narrowing quantities back to `numeric(18,3)`, it proves every retained value fits that precision and scale; otherwise rollback aborts. The pre-existing `S` ledger rows must survive with equal original-column values and counts. Parts 1, 2 and 3A remain applied.

Once real Part 3 documents or ledger postings exist, schema rollback is intentionally not destructive or automatic. Operational rollback is then feature disablement plus a forward correction migration; business records and ledger evidence are never dropped to make a down script pass.

## 7. Cross-part witness sequence

For each stage on an isolated fresh-chain database:

1. Record pre-apply migration history, schema fingerprint and relevant row counts.
2. Apply only that part in one transaction.
3. Verify catalog shape, named constraints/indexes/triggers, expected row counts and negative/positive transaction fixtures.
4. Restart the application against the intermediate schema and run existing-module smoke tests; new commands not yet supported remain feature-disabled.
5. Roll back the part using its down path and verify the pre-apply fingerprint and row counts.
6. Reapply the part, repeat the witness, and retain both apply/rollback evidence before authorising the next part.

The final production plan must name the concrete migration IDs, generated SQL hashes, backup/restore point, maintenance/feature-toggle steps, timeouts, lock expectations and witness signatories. Those belong to the migration implementation review and are not invented in this design-only plan.

## 8. Stop point

This document records the approved migration boundaries. Part 3A source is prepared for witness but has not been applied to any owner database. Part 3B remains the explicit stop point and is not written or authorised by this stage.

RESULT_REPORTED_PENDING_WITNESS
