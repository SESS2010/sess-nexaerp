# First Stores Module - Schema Design Report

Status: design only. No code, migration, or database operation is authorised by this report.

Authoritative process: `outputs/sess_business_process_purchase_stores.md`

Architecture pattern reference: `outputs/backend_architecture_reference.md`

## 1. Scope and design rules

This design covers Gate Entry, one effective GRN per Gate Entry, mandatory one-bill/one-GRN evidence, GRN-line Item snapshots, serial capture, Item ERP barcode generation, category-routed QC with optional structured parameters and immutable revisions, QC ageing, accepted and rejected disposition, append-only stock ledger, approved material issues, bidirectional Delivery Challans, a minimal manually created Job Order, and one shared role-resolved notification engine for this and future modules.

The Delivery Challan boundary includes returnable rejected-material, subcontract and demo dispatch/return, plus non-returnable warranty, bill-based and customer-PO-based dispatch. It does not include a subcontract PO or customer commercial-document model.

Explicitly deferred: Customer PO, offer generation, contract review, Estimated BOM, Actual BOM rollup, labour hours, subcontract PO, installation reports, installed-machine register, vendor-performance report/KPI, e-way bill, batch/lot tracking, source-less stock adjustments, and barcode-printer hardware integration. The minimal Job Order has no FK to a nonexistent Customer PO; its stable UUID lets a nullable `CustomerPoId` be added later without changing any table that references `JobOrderId`.

All transactional rows carry `CompanyId uuid NOT NULL`. It is copied from the selected Purchase Order by the service and verified against the PO by database constraints/triggers; request-supplied company values are never trusted. Shared `items` remain without `CompanyId`.

Unless a table definition says otherwise, a new mutable header has the standard columns `Id uuid PK`, `CompanyId uuid NOT NULL`, `CreatedAt timestamptz NOT NULL`, `CreatedBy varchar(160) NOT NULL`, `UpdatedAt timestamptz NULL`, `UpdatedBy varchar(160) NULL`, and `Version bigint NOT NULL DEFAULT 0`. Finalised document rows and all history/ledger rows reject `UPDATE` and `DELETE` by trigger. Draft updates use compare-and-swap `Version`.

Quantities use `numeric(24,6)`; money and percentages use `numeric(24,6)`. All FK delete actions are `RESTRICT`. Cross-company FKs use `(CompanyId, Id)` alternate keys wherever the principal is company-scoped.

“One Gate Entry produces one GRN” and “one inspection per GRN line” mean one **effective** record. A finalised reversal leaves the original record in history and makes it ineffective; only then may a corrected replacement become effective.

## 2. Existing Advance tables reused

These tables already exist in schema `advance`; they are not recreated.

| Table | Existing role and relevant shape | Change in this module |
|---|---|---|
| `companies` | Company tenant principal, `Id` PK. | None. |
| `items` | Shared Item master; `Id` PK; Item code, name, category, model, part number, HSN/SAC, GST, UOM, manufacturer, QC/serial flags, legacy `Barcode`; unique ItemCode and filtered unique Barcode. No `CompanyId` by design. | No company column. Add no per-company data here. ERP barcode and serial override live in `item_company_inventory_settings`. |
| `item_categories` | Shared category master; `Id` PK, unique code through master rules. | Category codes `ELE`, `REF`, `FAS`, `PLC`, `FAB`, and `MEC` are validated by service/configuration; no table-shape change. |
| `vendors` | Shared vendor master used by PO. | None; GRN snapshots vendor name. |
| `customers` | Shared customer master. | Reused as an optional typed DC/MIR destination; Job Order intentionally stores the required customer-name snapshot only. |
| `departments` | Shared department master. | Reused for request ownership, department destination and department-owner approval resolution. |
| `employees` | Shared employee identity used for received-by, inspector, finaliser, and Stores poster. | None. |
| `purchase_number_sequences` | Existing company/fiscal-year/prefix sequence foundation. | Extended/reused for Gate Entry, GRN, QC, Job Order, Material Issue Request, DC and company Item-barcode numbering; no new sequence table. |
| `purchase_orders` | Company-scoped PO header with VendorId, DeliveryWarehouseId, status and immutable commercial snapshot. | Add/retain alternate key `UNIQUE (CompanyId, Id)` for tenant-safe FKs; no received-to-date column. |
| `purchase_order_lines` | Company-scoped PO line with PurchaseOrderId, ItemId, OrderedQuantity, UnitRate, UOM and commercial snapshots. | Add/retain `UNIQUE (CompanyId, Id)` and `UNIQUE (PurchaseOrderId, Id)` for composite FKs; no received-to-date column. |
| `warehouses` | Company-scoped warehouse; defaults include accepted and QC-hold locations. | Retained. Default accepted destination is offered through the new category route, not forced. |
| `rack_bins` | Company-scoped warehouse rack/bin; `UNIQUE (WarehouseId, BinCode)`; alternate key `(WarehouseId, Id)`. | Retained. Add/retain `UNIQUE (CompanyId, Id)` and enforce Warehouse and Rack company equality. |
| `warehouse_condition_locations` | Company-scoped effective mapping of warehouse/rack to condition; currently supports `AVAILABLE`, `QC_HOLD`, `REJECTED`, `QUARANTINE`, `RETURN_TO_VENDOR`, and `SCRAP`. | Extend condition vocabulary with `PENDING_RETURNABLE_DC`; add/retain `UNIQUE (CompanyId, Id)`. |
| `qc_inspection_policies` | Company-scoped effective policy rows owned by exactly one Item or ItemCategory, including parameter code, UOM, limits, method, sample size and approval state. | Retained as an optional source. Zero policies produces zero result rows and never blocks QC. |
| `stock_movements` | Existing company-scoped minimal ledger; detailed redesign is in Section 4. | Modified in place. |
| `stock_reservations` | Existing company-scoped reservation foundation. | No write path in this module. QC-hold and pending-return quantities are never reservable. |
| `controlled_configuration_histories` and `audit_logs` | Existing controlled before/after configuration history and general audit evidence. | Reused for shared Item and company-setting changes. They do not replace `stores_document_status_history` for Gate/GRN/QC lifecycle evidence. |

## 3. New and modified table definitions

The final Stores schema contains exactly 24 new tables. The shared notification engine replaces the earlier single DC-specific delivery table with three generic tables. The following four earlier proposals remain explicitly deferred without weakening the design:

| Deferred table | Replacement in the first module | Reason |
|---|---|---|
| `stores_number_sequences` | Reuse and extend existing `purchase_number_sequences` for Gate Entry, GRN, QC and Item-barcode prefixes. | The established locked, company/fiscal-year sequence pattern already provides the required allocation semantics. |
| `item_master_change_history` | Use existing `controlled_configuration_histories` plus `audit_logs`, with a required Item/setting entity type, before/after JSON, actor, role, reason and correlation ID. | Existing controlled history carries the complete old/new evidence; a second typed table is unnecessary for the first module. |
| `qc_inspection_parameter_observations` | Store one `qc_inspection_parameter_results` row per required sample. | The normalised result row can hold the policy snapshot, sample ordinal, observed value and PASS/FAIL without a separate observation child. |
| `stores_command_receipts` | Use header `IdempotencyKey/RequestFingerprint` and `stock_posting_batches` idempotency. | These protect every first-module business effect and posting; a separate exact-response replay table can be added later if operational need justifies it. |

The existing `purchase_number_sequences` table must accept Stores prefixes/scopes and a non-fiscal Item-barcode scope while retaining its current locking and uniqueness rules. This is an extension of an existing table, not a new table.

### 3.1 `item_company_inventory_settings` - NEW

Company-specific control for a shared Item. This resolves the otherwise impossible combination of shared `items` and per-company ERP barcode sequences.

| Column | Definition |
|---|---|
| Standard identity/audit | `Id`, `CompanyId`, audit columns and `Version` |
| `ItemId` | `uuid NOT NULL FK items(Id)` |
| `ErpBarcode` | `varchar(128) NOT NULL`, generated as `SESS-<CAT>-<serial>` |
| `BarcodeCategoryCode` | `varchar(3) NOT NULL`; one of `ELE/REF/FAS/PLC/FAB/MEC` |
| `BarcodeSequenceNumber` | `bigint NOT NULL` |
| `BarcodeSymbology` | `varchar(30) NOT NULL DEFAULT 'CODE128'`; storage only, printer choice remains deferred |
| `SerialCaptureMode` | `varchar(20) NOT NULL DEFAULT 'INHERIT'`; `INHERIT`, `REQUIRED`, or `OPTIONAL` |
| `IsActive` | `boolean NOT NULL DEFAULT true` |

Keys/indexes: PK `Id`; unique `(CompanyId, ItemId)`; unique `(CompanyId, ErpBarcode)`; unique `(CompanyId, BarcodeSequenceNumber)`; index `(ItemId, IsActive)`; alternate key `(CompanyId, Id)`.

Checks: allowed category and serial modes; positive sequence; category code must agree with the Item category at creation and is then immutable. Barcode, sequence, company, and item are immutable after insertion. Changes to serial mode append complete old/new evidence to existing `controlled_configuration_histories` and `audit_logs`.

### 3.2 `business_rule_configuration_versions` - NEW, APPEND-ONLY

This is the single company-scoped, effective-dated registry required by Section 17.1. This module uses `SERIAL_CAPTURE_THRESHOLD` (5,000 initially) and `QC_COMPLETION_DAYS` (2 initially).

| Column | Definition |
|---|---|
| `Id` | `uuid PK` |
| `CompanyId` | `uuid NOT NULL FK companies(Id)` |
| `RuleKey` | `varchar(100) NOT NULL`; includes `SERIAL_CAPTURE_THRESHOLD` and the other centrally governed rules |
| `ValueType` | `varchar(20) NOT NULL`; `INTEGER`, `DECIMAL`, `BOOLEAN`, or `TEXT` |
| `OldValueJson` | `jsonb NULL`; null only for the first version |
| `NewValueJson` | `jsonb NOT NULL` |
| `UnitCode` | `varchar(30) NULL`, for example `INR`, `COUNT`, `KM` |
| `VersionNumber` | `integer NOT NULL` |
| `PreviousVersionId` | `uuid NULL FK business_rule_configuration_versions(Id)` |
| `EffectiveFrom` | `timestamptz NOT NULL`; the effective end is derived from the next version and is never written back |
| `ChangedByEmployeeId` | `uuid NOT NULL FK employees(Id)` |
| `ChangedByRoleCode` | `varchar(100) NOT NULL` |
| `ChangeReason` | `varchar(1000) NOT NULL` |
| `ChangedAt` | `timestamptz NOT NULL` |
| `CorrelationId` | `varchar(100) NOT NULL` |

Keys/indexes: PK; unique `(CompanyId, RuleKey, VersionNumber)`; unique `(CompanyId, RuleKey, EffectiveFrom)`; unique `CorrelationId`; index `(CompanyId, RuleKey, EffectiveFrom DESC)`.

Checks/guards: positive version; previous row has the same company/rule and exactly the prior version; old value equals the previous new value; role must be `TECHNICAL_DIRECTOR`, `MANAGING_DIRECTOR`, or `IT_MANAGER`. Trigger rejects update/delete. The effective value at time T is the highest version whose `EffectiveFrom <= T`; inserting a later version never updates the earlier row.

### 3.3 `store_category_routes` - NEW

| Column | Definition |
|---|---|
| Standard identity/audit | `Id`, `CompanyId`, audit columns and `Version` |
| `ItemCategoryId` | `uuid NOT NULL FK item_categories(Id)` |
| `QcHoldConditionLocationId` | `uuid NOT NULL FK warehouse_condition_locations(Id)` |
| `PendingReturnConditionLocationId` | `uuid NOT NULL FK warehouse_condition_locations(Id)` |
| `DefaultAcceptedConditionLocationId` | `uuid NOT NULL FK warehouse_condition_locations(Id)` |
| `EffectiveFrom` | `date NOT NULL` |
| `EffectiveTo` | `date NULL` |
| `IsActive` | `boolean NOT NULL DEFAULT true` |

Keys/indexes: PK; unique `(CompanyId, ItemCategoryId, EffectiveFrom)`; PostgreSQL exclusion constraint prevents overlapping active `daterange(EffectiveFrom, EffectiveTo, '[]')` periods for the same Company/category; index on each condition-location FK; alternate key `(CompanyId, Id)`.

Checks/guards: valid dates; QC and pending-return mappings must point to the same company, warehouse, and rack, with conditions `QC_HOLD` and `PENDING_RETURNABLE_DC`; default accepted mapping must be same company and `AVAILABLE`. Non-overlap plus a coverage guard on route changes and GRN creation enforces exactly one effective QC rack per company/category while allowing Stores to select another valid `AVAILABLE` destination during posting.

### 3.4 `gate_entries` - NEW

| Column | Definition |
|---|---|
| Standard identity/audit | `Id`, `CompanyId`, audit columns and `Version` |
| `GateEntryNumber` | `varchar(50) NOT NULL` |
| `DocumentKind` | `varchar(20) NOT NULL DEFAULT 'NORMAL'`; `NORMAL` or `REVERSAL` |
| `ReversesGateEntryId` | `uuid NULL FK gate_entries(Id)` |
| `ReversalReason` | `varchar(1000) NULL` |
| `PurchaseOrderId` | `uuid NOT NULL FK purchase_orders(Id)` |
| `VendorId` | `uuid NOT NULL FK vendors(Id)`, copied from PO and database-verified |
| `VendorNameSnapshot` | `varchar(240) NOT NULL` |
| `VendorDcNumber` | `varchar(100) NOT NULL` |
| `VehicleNumber` | `varchar(50) NULL` |
| `ModeOfTransport` | `varchar(50) NOT NULL` |
| `ArrivedAt` | `timestamptz NOT NULL` |
| `ReceivedByEmployeeId` | `uuid NOT NULL FK employees(Id)` |
| `IsoReceiptVerificationJson` | `jsonb NOT NULL`, immutable object containing the receipt/document-verification evidence |
| `Status` | `varchar(20) NOT NULL DEFAULT 'DRAFT'`; `DRAFT` or `FINALIZED` |
| `FinalizedAt` | `timestamptz NULL` |
| `FinalizedByEmployeeId` | `uuid NULL FK employees(Id)` |
| `IdempotencyKey` | `varchar(100) NOT NULL` |
| `RequestFingerprint` | `char(64) NOT NULL` |

Keys/indexes: PK; unique `(CompanyId, GateEntryNumber)`; unique `(CompanyId, IdempotencyKey)`; unique filtered `ReversesGateEntryId WHERE DocumentKind='REVERSAL' AND Status='FINALIZED'`; index `(CompanyId, PurchaseOrderId, ArrivedAt DESC)`; index `(CompanyId, VendorId, VendorDcNumber)`; alternate keys `(CompanyId, Id)` and `(PurchaseOrderId, Id)`.

Checks/guards: document-kind/reversal fields agree; reversal target is a finalised normal Gate Entry in the same company/PO; ISO JSON is an object; finaliser fields are both null in draft and both present when finalised. Company and Vendor must match PO. Finalised row is immutable.

### 3.5 `gate_entry_lines` - NEW

| Column | Definition |
|---|---|
| `Id` | `uuid PK` |
| `CompanyId` | `uuid NOT NULL` |
| `GateEntryId` | `uuid NOT NULL FK gate_entries(Id)` |
| `PurchaseOrderId` | `uuid NOT NULL` |
| `PurchaseOrderLineId` | `uuid NOT NULL FK purchase_order_lines(Id)` |
| `LineNumber` | `integer NOT NULL` |
| `ItemId` | `uuid NOT NULL FK items(Id)` |
| `ItemCodeSnapshot` | `varchar(80) NOT NULL` |
| `UomSnapshot` | `varchar(32) NOT NULL` |
| `DeliveredQuantity` | `numeric(24,6) NOT NULL` |
| `CreatedAt`, `CreatedBy` | Immutable audit columns |

Keys/indexes: PK; unique `(GateEntryId, LineNumber)`; unique `(GateEntryId, PurchaseOrderLineId)`; index `(CompanyId, PurchaseOrderLineId)`; alternate key `(CompanyId, Id)`.

Checks/guards: positive line/quantity; composite FKs prove line belongs to Gate PO and Company; Item must equal PO-line Item. Lines become immutable with their finalised parent. A reversal line must exactly mirror its target line; the reversal sign is supplied by `DocumentKind`, never a negative quantity.

### 3.6 `goods_receipts` - NEW

This is the GRN header.

| Column | Definition |
|---|---|
| Standard identity/audit | `Id`, `CompanyId`, audit columns and `Version` |
| `GrnNumber` | `varchar(50) NOT NULL` |
| `DocumentKind` | `varchar(20) NOT NULL DEFAULT 'NORMAL'`; `NORMAL` or `REVERSAL` |
| `ReversesGoodsReceiptId` | `uuid NULL FK goods_receipts(Id)` |
| `ReversalReason` | `varchar(1000) NULL` |
| `GateEntryId` | `uuid NOT NULL FK gate_entries(Id)` |
| `PurchaseOrderId` | `uuid NOT NULL FK purchase_orders(Id)` |
| `VendorId` | `uuid NOT NULL FK vendors(Id)` |
| `VendorNameSnapshot` | `varchar(240) NOT NULL` |
| `VendorBillNumber` | `varchar(100) NOT NULL` |
| `VendorBillDate` | `date NOT NULL` |
| `VendorDcNumberSnapshot` | `varchar(100) NOT NULL` |
| `ModeOfTransportSnapshot` | `varchar(50) NOT NULL` |
| `ReceivedAt` | `timestamptz NOT NULL` |
| `ReceivedByEmployeeId` | `uuid NOT NULL FK employees(Id)` |
| `IsoReceiptVerificationJson` | `jsonb NOT NULL`, copied/extended from Gate evidence |
| `ConfigurationSnapshotJson` | `jsonb NOT NULL`, immutable effective rule IDs and values |
| `ConfigurationSnapshotHash` | `char(64) NOT NULL` |
| `QcCompletionDaysConfigVersionId` | `uuid NOT NULL FK business_rule_configuration_versions(Id)` |
| `QcCompletionDaysSnapshot` | `integer NOT NULL` |
| `QcDueAt` | `timestamptz NOT NULL`, calculated from GRN finalisation plus the snapshotted day limit |
| `Status` | `varchar(20) NOT NULL DEFAULT 'DRAFT'`; `DRAFT` or `FINALIZED` |
| `FinalizedAt` | `timestamptz NULL` |
| `FinalizedByEmployeeId` | `uuid NULL FK employees(Id)` |
| `IdempotencyKey` | `varchar(100) NOT NULL` |
| `RequestFingerprint` | `char(64) NOT NULL` |

Keys/indexes: PK; unique `(CompanyId, GrnNumber)`; unique `(CompanyId, IdempotencyKey)`; unique filtered `ReversesGoodsReceiptId WHERE DocumentKind='REVERSAL' AND Status='FINALIZED'`; index `(CompanyId, PurchaseOrderId, ReceivedAt DESC)`; index `(CompanyId, VendorBillNumber)`; index `(CompanyId, QcDueAt)`; index `GateEntryId`; alternate keys `(CompanyId, Id)`, `(GateEntryId, Id)`.

Effective-cardinality guard: a deferred constraint trigger permits at most one effective finalised normal GRN per Gate Entry and at most one effective normal GRN per `(CompanyId, VendorBillNumber)`. A corrected replacement using the same bill number is allowed only after a finalised reversal of the prior GRN. Draft duplicates may exist but cannot both finalise.

Other guards: Gate Entry must be finalised/effective and have the same Company, PO and Vendor; bill fields are mandatory; document-kind/reversal fields agree; snapshot hash matches canonical JSON; reversal copies the target Gate/PO/vendor/bill/configuration facts. Finalisation and its QC-hold posting batch are one transaction. Finalised rows reject update/delete.

### 3.7 `goods_receipt_lines` - NEW

| Column | Definition |
|---|---|
| `Id` | `uuid PK` |
| `CompanyId` | `uuid NOT NULL` |
| `GoodsReceiptId` | `uuid NOT NULL FK goods_receipts(Id)` |
| `GateEntryLineId` | `uuid NOT NULL FK gate_entry_lines(Id)` |
| `PurchaseOrderLineId` | `uuid NOT NULL FK purchase_order_lines(Id)` |
| `LineNumber` | `integer NOT NULL` |
| `ItemId` | `uuid NOT NULL FK items(Id)` |
| `ItemCodeSnapshot` | `varchar(80) NOT NULL` |
| `ItemNameSnapshot` | `varchar(240) NOT NULL` |
| `ItemCategoryIdSnapshot` | `uuid NOT NULL FK item_categories(Id)` |
| `ItemCategoryCodeSnapshot` | `varchar(20) NOT NULL` |
| `HsnSacCodeSnapshot` | `varchar(30) NOT NULL` |
| `GstPercentageSnapshot` | `numeric(8,4) NOT NULL` |
| `ModelSnapshot` | `varchar(160) NULL` |
| `ManufacturerPartNumberSnapshot` | `varchar(160) NULL` |
| `ManufacturerMakeSnapshot` | `varchar(160) NULL` |
| `UomSnapshot` | `varchar(32) NOT NULL` |
| `PoOrderedQuantitySnapshot` | `numeric(24,6) NOT NULL` |
| `PriorEffectiveReceivedQuantitySnapshot` | `numeric(24,6) NOT NULL` |
| `RemainingPoQuantitySnapshot` | `numeric(24,6) NOT NULL` |
| `DeliveredQuantitySnapshot` | `numeric(24,6) NOT NULL` |
| `ReceivedQuantity` | `numeric(24,6) NOT NULL`; PO-authorised quantity admitted to QC inspection |
| `ExcessRejectedQuantity` | `numeric(24,6) NOT NULL DEFAULT 0`; delivered excess admitted only to segregated QC-rack custody |
| `ExcessDisposition` | `varchar(40) NULL`; `PENDING_RETURNABLE_DC` when excess is positive |
| `LineValueSnapshot` | `numeric(24,6) NOT NULL`, commercial value attributable to this GRN line's `ReceivedQuantity` |
| `UnitRateSnapshot` | `numeric(24,6) NOT NULL`, exactly `LineValueSnapshot / ReceivedQuantity`; basis for the serial threshold |
| `SerialThresholdConfigVersionId` | `uuid NOT NULL FK business_rule_configuration_versions(Id)` |
| `SerialThresholdValueSnapshot` | `numeric(24,6) NOT NULL` |
| `SerialCaptureModeSnapshot` | `varchar(20) NOT NULL`; resolved `REQUIRED` or `OPTIONAL` |
| `SerialOverrideSettingId` | `uuid NULL FK item_company_inventory_settings(Id)` |
| `QcRouteIdSnapshot` | `uuid NOT NULL FK store_category_routes(Id)` |
| `QcHoldConditionLocationIdSnapshot` | `uuid NOT NULL FK warehouse_condition_locations(Id)` |
| `BillWarrantyLimitDate` | `date NOT NULL`; bill date plus 13 months |
| `InitialWarrantyExpiryDate` | `date NOT NULL`; initially equal to the bill date plus 13 months |
| `CreatedAt`, `CreatedBy` | Immutable audit columns |

Keys/indexes: PK; unique `(GoodsReceiptId, LineNumber)`; unique `(GoodsReceiptId, GateEntryLineId)`; index `(CompanyId, PurchaseOrderLineId)`; index `(CompanyId, ItemId)`; alternate key `(CompanyId, Id)`.

Checks: all quantities nonnegative; received positive; `ReceivedQuantity + ExcessRejectedQuantity = DeliveredQuantitySnapshot`; `ReceivedQuantity <= RemainingPoQuantitySnapshot`; `Remaining = Ordered - PriorReceived`; excess disposition is `PENDING_RETURNABLE_DC` iff excess is positive; GST range 0-100; serial mode valid; `UnitRateSnapshot = LineValueSnapshot / ReceivedQuantity`; initial warranty equals bill date plus 13 months.

Finalisation trigger, under a lock covering the PO line, recalculates effective received-to-date using only `ReceivedQuantity`, not excess. It rejects any result above `OrderedQuantity`; there is no override path or override column. It also requires every Gate line to have exactly one GRN line and vice versa. GRN posting separately records `ReceivedQuantity` in `QC_HOLD` and `ExcessRejectedQuantity` in `PENDING_RETURNABLE_DC` at the same category QC rack.

### 3.8 `inventory_serials` - NEW, DURABLE IDENTITY

This table owns company-wide stored-serial uniqueness independently of any one GRN. A corrected GRN may reference the same durable identity after the original receipt is reversed; it does not create a false duplicate.

| Column | Definition |
|---|---|
| `Id` | `uuid PK` |
| `CompanyId` | `uuid NOT NULL FK companies(Id)` |
| `ItemId` | `uuid NOT NULL FK items(Id)` |
| `StoredSerialNumber` | `varchar(300) NOT NULL` |
| `NormalizedStoredSerialNumber` | `varchar(300) NOT NULL` |
| `FirstCapturedAt` | `timestamptz NOT NULL` |
| `FirstCapturedByEmployeeId` | `uuid NOT NULL FK employees(Id)` |
| `CreatedAt`, `CreatedBy` | Immutable audit columns |

Keys/indexes: PK; unique `(CompanyId, NormalizedStoredSerialNumber)`; index `(CompanyId, ItemId, StoredSerialNumber)`; alternate key `(CompanyId, Id)`.

Checks/guards: serial values are nonblank and canonical value equals the normalisation function. Identity rows reject update/delete. Reversal does not delete or rename the identity.

### 3.9 `goods_receipt_line_serials` - NEW

Serial rows exist only for serial-captured GRN lines; warranty remains once on the GRN line.

| Column | Definition |
|---|---|
| `Id` | `uuid PK` |
| `CompanyId` | `uuid NOT NULL` |
| `GoodsReceiptLineId` | `uuid NOT NULL FK goods_receipt_lines(Id)` |
| `ItemId` | `uuid NOT NULL FK items(Id)` |
| `InventorySerialId` | `uuid NOT NULL FK inventory_serials(Id)` |
| `SerialOrdinal` | `integer NOT NULL` |
| `EnteredSerialNumber` | `varchar(200) NOT NULL`, original operator input |
| `StoredSerialNumberSnapshot` | `varchar(300) NOT NULL`, possibly disambiguated and equal to the durable identity |
| `ReceiptDisposition` | `varchar(30) NOT NULL`; `QC_INSPECTION` or `EXCESS_PENDING_RETURN` |
| `DisambiguationApplied` | `boolean NOT NULL DEFAULT false` |
| `DuplicateWarningAcknowledged` | `boolean NOT NULL DEFAULT false` |
| `DisambiguationReason` | `varchar(500) NULL` |
| `CapturedByEmployeeId` | `uuid NOT NULL FK employees(Id)` |
| `CapturedAt` | `timestamptz NOT NULL` |

Keys/indexes: PK; unique `(GoodsReceiptLineId, SerialOrdinal)`; unique `(GoodsReceiptLineId, InventorySerialId)`; index `(CompanyId, ItemId, StoredSerialNumberSnapshot)`; index `InventorySerialId`; alternate key `(CompanyId, Id)`.

Checks/guards: positive ordinal; nonblank values; disambiguation fields agree; Company and Item equal both parent and durable serial identity; stored snapshot equals the identity. Before finalisation, the service warns if entered value already exists. A genuine new duplicate must be disambiguated before its durable identity can be inserted. A correction of a reversed receipt reuses the existing identity. When serial capture is required, finalisation requires an integral delivered quantity and exactly that many serial rows: the `QC_INSPECTION` count equals `ReceivedQuantity` and the `EXCESS_PENDING_RETURN` count equals `ExcessRejectedQuantity`. Capture rows become immutable with the GRN.

### 3.10 `stores_document_status_history` - NEW, APPEND-ONLY

Typed status history shared by Gate Entry, GRN, and QC revision without text-only document references.

| Column | Definition |
|---|---|
| `Id` | `uuid PK` |
| `CompanyId` | `uuid NOT NULL` |
| `GateEntryId` | `uuid NULL FK gate_entries(Id)` |
| `GoodsReceiptId` | `uuid NULL FK goods_receipts(Id)` |
| `QcInspectionRevisionId` | `uuid NULL FK qc_inspection_revisions(Id)` |
| `JobOrderId` | `uuid NULL FK job_orders(Id)` |
| `MaterialIssueRequestId` | `uuid NULL FK material_issue_requests(Id)` |
| `DeliveryChallanId` | `uuid NULL FK delivery_challans(Id)` |
| `FromStatus` | `varchar(30) NULL` |
| `ToStatus` | `varchar(30) NOT NULL` |
| `Action` | `varchar(30) NOT NULL`; `CREATED`, `SUBMITTED`, `APPROVED`, `REJECTED`, `FINALIZED`, `INSTALLED`, `DISPATCHED`, `RECEIVED`, `CLOSED`, or `REVERSED` |
| `ActorEmployeeId` | `uuid NOT NULL FK employees(Id)` |
| `ActorRoleCode` | `varchar(100) NOT NULL` |
| `Reason` | `varchar(1000) NULL` |
| `OccurredAt` | `timestamptz NOT NULL` |
| `CorrelationId` | `varchar(100) NOT NULL` |

Keys/indexes: PK; unique `CorrelationId`; indexes on every source FK plus `OccurredAt`.

Checks: exactly one source FK is non-null; legal status/action pair; Company matches source. Trigger rejects update/delete and illegal transition insertion.

### 3.11 `qc_inspections` - NEW, LOGICAL IDENTITY

| Column | Definition |
|---|---|
| `Id` | `uuid PK` |
| `CompanyId` | `uuid NOT NULL` |
| `InspectionNumber` | `varchar(50) NOT NULL` |
| `GoodsReceiptLineId` | `uuid NULL FK goods_receipt_lines(Id)` |
| `DeliveryChallanLineId` | `uuid NULL FK delivery_challan_lines(Id)`; inbound-return source |
| `CreatedAt`, `CreatedBy` | Immutable identity audit |

Keys/indexes: PK; unique `(CompanyId, InspectionNumber)`; unique filtered `GoodsReceiptLineId` where non-null; unique filtered `DeliveryChallanLineId` where non-null; alternate key `(CompanyId, Id)`.

The row itself is immutable. Exactly one source line FK is non-null. It guarantees one logical inspection per GRN line or QC-required inbound DC-return line. Corrections are revisions, not additional logical inspections.

### 3.12 `qc_inspection_revisions` - NEW

| Column | Definition |
|---|---|
| Standard identity/audit | `Id`, `CompanyId`, audit columns and `Version` |
| `QcInspectionId` | `uuid NOT NULL FK qc_inspections(Id)` |
| `RevisionNumber` | `integer NOT NULL` |
| `RevisionKind` | `varchar(20) NOT NULL`; `INITIAL` or `CORRECTION` |
| `RevisesRevisionId` | `uuid NULL FK qc_inspection_revisions(Id)` |
| `CorrectionReason` | `varchar(1000) NULL` |
| `InspectorEmployeeId` | `uuid NOT NULL FK employees(Id)` |
| `InspectorBasis` | `varchar(30) NOT NULL`; `QC_MANAGER` or `PR_RAISER_FALLBACK` |
| `FallbackReason` | `varchar(1000) NULL` |
| `InspectionStartedAt` | `timestamptz NOT NULL` |
| `InspectionCompletedAt` | `timestamptz NULL` |
| `InspectedQuantity` | `numeric(24,6) NOT NULL` |
| `AcceptedQuantity` | `numeric(24,6) NOT NULL` |
| `RejectedQuantity` | `numeric(24,6) NOT NULL` |
| `InspectionShortfallRejectedQuantity` | `numeric(24,6) NOT NULL` |
| `Decision` | `varchar(30) NOT NULL`; `ACCEPTED`, `PARTIALLY_ACCEPTED`, or `REJECTED` |
| `AcceptedConditionLocationId` | `uuid NULL FK warehouse_condition_locations(Id)`, selected by Stores |
| `QcHoldConditionLocationIdSnapshot` | `uuid NOT NULL FK warehouse_condition_locations(Id)` |
| `PendingReturnConditionLocationIdSnapshot` | `uuid NOT NULL FK warehouse_condition_locations(Id)` |
| `PolicyResolvedAt` | `timestamptz NOT NULL` |
| `Status` | `varchar(20) NOT NULL DEFAULT 'DRAFT'`; `DRAFT` or `FINALIZED` |
| `FinalizedAt` | `timestamptz NULL` |
| `FinalizedByEmployeeId` | `uuid NULL FK employees(Id)` |
| `IdempotencyKey` | `varchar(100) NOT NULL` |
| `RequestFingerprint` | `char(64) NOT NULL` |

Keys/indexes: PK; unique `(QcInspectionId, RevisionNumber)`; unique filtered `RevisesRevisionId WHERE RevisionKind='CORRECTION'`; unique `(CompanyId, IdempotencyKey)`; index `(CompanyId, Status, InspectionStartedAt)`; alternate key `(CompanyId, Id)`.

Checks/guards: revision chain is same logical inspection and sequential; correction fields agree; fallback requires reason and inspector equal the originating PR/MIR raiser; quantities reconcile to the GRN received quantity or inbound DC-line quantity; any inspection shortfall is rejected; accepted plus rejected equals the source quantity; decision agrees with quantities; accepted destination is required iff accepted is positive and must be an active same-company `AVAILABLE` mapping; pending-return mapping is required for rejected quantity and remains the same physical QC rack. QC finalisation requires policy-result rows only when effective policies exist. Zero policies and zero parameter rows are valid and never block the inspector's accept/reject decision. Serial dispositions remain mandatory for serialized sources. Finalisation and all stock posting legs are atomic. A finalised revision is immutable.

### 3.13 `qc_inspection_parameter_results` - NEW

One normalised result row is stored for each required sample of each effective policy. Item- and category-owned policies are not collapsed; each source policy and sample remains independently traceable.

| Column | Definition |
|---|---|
| `Id` | `uuid PK` |
| `CompanyId` | `uuid NOT NULL` |
| `QcInspectionRevisionId` | `uuid NOT NULL FK qc_inspection_revisions(Id)` |
| `QcInspectionPolicyId` | `uuid NOT NULL FK qc_inspection_policies(Id)` |
| `ParameterCodeSnapshot` | `varchar(100) NOT NULL` |
| `MeasurementUomIdSnapshot` | `uuid NOT NULL FK uoms(Id)` |
| `MeasurementUomCodeSnapshot` | `varchar(32) NOT NULL` |
| `LowerLimitSnapshot` | `numeric(24,6) NULL` |
| `UpperLimitSnapshot` | `numeric(24,6) NULL` |
| `InspectionMethodSnapshot` | `varchar(200) NOT NULL` |
| `RequiredSampleSizeSnapshot` | `integer NOT NULL` |
| `SampleOrdinal` | `integer NOT NULL` |
| `ObservedNumericValue` | `numeric(24,6) NULL` |
| `ObservedTextValue` | `varchar(500) NULL` |
| `Result` | `varchar(20) NOT NULL`; `PASS` or `FAIL` |
| `Remarks` | `varchar(1000) NULL` |
| `ObservedAt` | `timestamptz NOT NULL` |
| `ObservedByEmployeeId` | `uuid NOT NULL FK employees(Id)` |
| `CreatedAt`, `CreatedBy` | Immutable audit columns after revision finalisation |

Keys/indexes: PK; unique `(QcInspectionRevisionId, QcInspectionPolicyId, SampleOrdinal)`; index `(CompanyId, ParameterCodeSnapshot, Result)`; alternate key `(CompanyId, Id)`.

Checks: when an effective policy exists, limits/sample/result are valid, `SampleOrdinal` is positive and not above the snapshotted sample size, exactly one observed-value column is non-null, and snapshots equal that policy at creation. Finalisation requires exactly `RequiredSampleSizeSnapshot` rows for every resolved policy. When no effective policy exists, the revision has zero parameter rows and finalises from its quantity decision alone. Rows are mutable only while the parent revision is draft and immutable afterward.

### 3.14 `qc_inspection_serial_dispositions` - NEW

This table makes serial-level stock location and condition deterministic while the QC decision remains one record per GRN line.

| Column | Definition |
|---|---|
| `Id` | `uuid PK` |
| `CompanyId` | `uuid NOT NULL` |
| `QcInspectionRevisionId` | `uuid NOT NULL FK qc_inspection_revisions(Id)` |
| `InventorySerialId` | `uuid NOT NULL FK inventory_serials(Id)` |
| `Disposition` | `varchar(20) NOT NULL`; `ACCEPTED` or `REJECTED` |
| `Reason` | `varchar(1000) NULL` |
| `CreatedAt`, `CreatedBy` | Immutable audit columns |

Keys/indexes: PK; unique `(QcInspectionRevisionId, InventorySerialId)`; index `(CompanyId, InventorySerialId)`.

Checks/guards: serial belongs to the inspected GRN's `QC_INSPECTION` population or the inbound DC source line; every inspected serial has exactly one disposition; accepted/rejected serial counts equal the revision quantities. Excess serials bypass QC only into `PENDING_RETURNABLE_DC` and must be selected on their outbound DC. Rows are immutable after finalisation.

### 3.15 `stock_posting_batches` - NEW, APPEND-ONLY

One business command creates a batch containing all balanced ledger legs.

| Column | Definition |
|---|---|
| `Id` | `uuid PK` |
| `CompanyId` | `uuid NOT NULL` |
| `PostingKind` | `varchar(30) NOT NULL`; `GRN_CUSTODY`, `QC_DISPOSITION`, `MATERIAL_ISSUE`, `DC_DISPATCH`, `DC_RETURN_CUSTODY`, or `REVERSAL` |
| `GoodsReceiptId` | `uuid NULL FK goods_receipts(Id)` |
| `QcInspectionRevisionId` | `uuid NULL FK qc_inspection_revisions(Id)` |
| `MaterialIssueRequestId` | `uuid NULL FK material_issue_requests(Id)` |
| `DeliveryChallanId` | `uuid NULL FK delivery_challans(Id)` |
| `ReversesPostingBatchId` | `uuid NULL FK stock_posting_batches(Id)` |
| `ReferenceType` | `varchar(40) NOT NULL`, descriptive snapshot |
| `ReferenceNumber` | `varchar(120) NOT NULL`, descriptive snapshot |
| `PostingDate` | `date NOT NULL` |
| `PostedAt` | `timestamptz NOT NULL` |
| `PostedByEmployeeId` | `uuid NOT NULL FK employees(Id)` |
| `IdempotencyKey` | `varchar(100) NOT NULL` |
| `RequestFingerprint` | `char(64) NOT NULL` |
| `CorrelationId` | `varchar(100) NOT NULL` |

Keys/indexes: PK; unique `(CompanyId, IdempotencyKey)`; unique `CorrelationId`; unique filtered `ReversesPostingBatchId WHERE PostingKind='REVERSAL'`; indexes on every source FK and `(CompanyId, PostingDate)`; alternate key `(CompanyId, Id)`.

Checks/guards: exactly one direct source FK for non-reversal batches; reversal target required only for `REVERSAL`; source and reversal company match; reference values must be derived from the source document. Trigger rejects update/delete. A reused idempotency key with identical fingerprint replays; a different fingerprint conflicts.

### 3.16 `job_orders` - NEW

Minimal manually created Job Order identity. It deliberately has no Customer PO, offer, contract-review or BOM dependency.

| Column | Definition |
|---|---|
| Standard identity/audit | `Id`, `CompanyId`, audit columns and `Version` |
| `JobOrderNumber` | `varchar(50) NOT NULL` |
| `MachineModel` | `varchar(160) NOT NULL` |
| `MachineSerial` | `varchar(100) NOT NULL`; existing sequence mechanism may generate the settled SESS format |
| `CustomerName` | `varchar(240) NOT NULL` |
| `Status` | `varchar(20) NOT NULL`; `DRAFT`, `OPEN`, `INSTALLED`, or `CLOSED` |
| `JobOrderDate` | `date NOT NULL` |
| `PlannedCompletionDate` | `date NULL` |
| `InstallationDate` | `date NULL` |
| `ClosedAt` | `timestamptz NULL` |
| `IdempotencyKey` | `varchar(100) NOT NULL` |
| `RequestFingerprint` | `char(64) NOT NULL` |

Keys/indexes: PK; unique `(CompanyId, JobOrderNumber)`; unique `(CompanyId, MachineSerial)`; unique `(CompanyId, IdempotencyKey)`; index `(CompanyId, Status, JobOrderDate)`; alternate key `(CompanyId, Id)`.

Checks/guards: nonblank identity fields; installation date is present for `INSTALLED/CLOSED`; close date only for `CLOSED`; valid date order. A future nullable `CustomerPoId` can be added to this stable header without changing any MIR, DC, approval, ledger or reporting FK that already targets `JobOrderId`.

### 3.17 `material_issue_requests` - NEW

The approved authority to issue material. There is no issue-without-request path.

| Column | Definition |
|---|---|
| Standard identity/audit | `Id`, `CompanyId`, audit columns and `Version` |
| `RequestNumber` | `varchar(50) NOT NULL` |
| `Purpose` | `varchar(30) NOT NULL`; `FACTORY_ASSEMBLY`, `PROJECT`, `SERVICE`, `WARRANTY`, `DEMO`, `SALE`, or `FREE_OF_COST` |
| `DestinationType` | `varchar(20) NOT NULL`; `JOB_ORDER`, `CUSTOMER`, `VENDOR`, `DEPARTMENT`, or `OTHER` |
| `JobOrderId` | `uuid NULL FK job_orders(Id)` |
| `CustomerId` | `uuid NULL FK customers(Id)` |
| `VendorId` | `uuid NULL FK vendors(Id)` |
| `DestinationDepartmentId` | `uuid NULL FK departments(Id)` |
| `DestinationNameSnapshot` | `varchar(240) NOT NULL` |
| `RequestingDepartmentId` | `uuid NOT NULL FK departments(Id)` |
| `RequestedByEmployeeId` | `uuid NOT NULL FK employees(Id)` |
| `RequiredDate` | `date NOT NULL` |
| `Status` | `varchar(30) NOT NULL`; `DRAFT`, `SUBMITTED`, `APPROVED`, `REJECTED`, `PARTIALLY_FULFILLED`, `FULFILLED`, or `REVERSED` |
| `ApprovalRouteSnapshotJson` | `jsonb NOT NULL`; Production Manager or resolved department owner |
| `ApprovedAt` | `timestamptz NULL` |
| `ApprovedByEmployeeId` | `uuid NULL FK employees(Id)` |
| `IdempotencyKey` | `varchar(100) NOT NULL` |
| `RequestFingerprint` | `char(64) NOT NULL` |

Keys/indexes: PK; unique `(CompanyId, RequestNumber)`; unique `(CompanyId, IdempotencyKey)`; index `(CompanyId, Status, RequiredDate)`; index `JobOrderId`; alternate key `(CompanyId, Id)`.

Checks/guards: exactly one destination FK is non-null for typed destination types; `OTHER` has none and requires a name; Company of Job Order matches request; approval fields agree with status; route snapshot is immutable after submission. Issue posting requires an append-only approval row matching the snapshot. Fulfilled state is derived from signed posting batches, not a trusted client quantity.

### 3.18 `material_issue_request_lines` - NEW

| Column | Definition |
|---|---|
| `Id` | `uuid PK` |
| `CompanyId` | `uuid NOT NULL` |
| `MaterialIssueRequestId` | `uuid NOT NULL FK material_issue_requests(Id)` |
| `LineNumber` | `integer NOT NULL` |
| `ItemId` | `uuid NOT NULL FK items(Id)` |
| `ItemCodeSnapshot` | `varchar(80) NOT NULL` |
| `ItemNameSnapshot` | `varchar(240) NOT NULL` |
| `UomSnapshot` | `varchar(32) NOT NULL` |
| `RequestedQuantity` | `numeric(24,6) NOT NULL` |
| `Remarks` | `varchar(1000) NULL` |
| `CreatedAt`, `CreatedBy` | Audit columns; immutable after parent submission |

Keys/indexes: PK; unique `(MaterialIssueRequestId, LineNumber)`; index `(CompanyId, ItemId)`; alternate key `(CompanyId, Id)`.

Checks/guards: positive line and quantity; Company matches header. Issued-to-date is the signed sum of stock movements sourced to this line and may not exceed requested quantity. A posting batch is the fulfilment document, avoiding a redundant material-issue header/line pair.

### 3.19 `stores_approval_history` - NEW, APPEND-ONLY

| Column | Definition |
|---|---|
| `Id` | `uuid PK` |
| `CompanyId` | `uuid NOT NULL` |
| `MaterialIssueRequestId` | `uuid NULL FK material_issue_requests(Id)` |
| `DeliveryChallanId` | `uuid NULL FK delivery_challans(Id)` |
| `ApprovalCycle` | `integer NOT NULL` |
| `StepNumber` | `integer NOT NULL` |
| `Action` | `varchar(30) NOT NULL`; `APPROVE`, `REJECT`, or `REQUEST_REVISION` |
| `ResolvedEmployeeId` | `uuid NOT NULL FK employees(Id)` |
| `ResolvedRoleCode` | `varchar(100) NOT NULL` |
| `SnapshotIdentity` | `varchar(100) NOT NULL` |
| `Remarks` | `varchar(1000) NOT NULL` |
| `OccurredAt` | `timestamptz NOT NULL` |
| `CorrelationId` | `varchar(100) NOT NULL` |

Keys/indexes: PK; unique `CorrelationId`; unique `(MaterialIssueRequestId, ApprovalCycle, StepNumber)` where MIR is non-null; equivalent unique index for DC; indexes on resolved employee and occurrence time.

Checks/guards: exactly one parent FK; positive cycle/step; actor must equal the immutable route snapshot. Trigger rejects update/delete. It covers MIR approval, rejected-material DC approval where required by the specification, and department-owner approval for non-returnable DC.

### 3.20 `delivery_challans` - NEW

One table handles outbound DCs and inbound return receipts. An inbound row points to its original outbound returnable DC; multiple partial returns are allowed.

| Column | Definition |
|---|---|
| Standard identity/audit | `Id`, `CompanyId`, audit columns and `Version` |
| `DcNumber` | `varchar(50) NOT NULL` |
| `Direction` | `varchar(20) NOT NULL`; `OUTBOUND` or `INBOUND_RETURN` |
| `ParentDeliveryChallanId` | `uuid NULL FK delivery_challans(Id)`; required for inbound return |
| `DcType` | `varchar(20) NOT NULL`; `RETURNABLE` or `NON_RETURNABLE` |
| `Purpose` | `varchar(30) NOT NULL`; `REJECTED_MATERIAL`, `SUBCONTRACT`, `DEMO`, `WARRANTY`, `BILL_BASED`, or `CUSTOMER_PO_BASED` |
| `MaterialIssueRequestId` | `uuid NULL FK material_issue_requests(Id)` |
| `JobOrderId` | `uuid NULL FK job_orders(Id)` |
| `VendorId` | `uuid NULL FK vendors(Id)` |
| `CustomerId` | `uuid NULL FK customers(Id)` |
| `DestinationNameSnapshot` | `varchar(240) NOT NULL` |
| `ExternalReferenceNumber` | `varchar(120) NULL`; required for bill-based/customer-PO-based DC until native source tables exist |
| `DispatchEvidenceJson` | `jsonb NOT NULL`, immutable supporting reference, transport and acknowledgement evidence |
| `ExpectedReturnDate` | `date NULL`; mandatory for outbound returnable DC |
| `DocumentDate` | `date NOT NULL` |
| `Status` | `varchar(30) NOT NULL`; `DRAFT`, `SUBMITTED`, `APPROVED`, `DISPATCHED`, `OUTSTANDING`, `PARTIALLY_RETURNED`, `RECEIVED`, `CLOSED`, or `REVERSED` |
| `ApprovalRouteSnapshotJson` | `jsonb NULL`; mandatory where DC approval applies |
| `DispatchedAt` | `timestamptz NULL` |
| `ReceivedAt` | `timestamptz NULL` |
| `HandledByEmployeeId` | `uuid NOT NULL FK employees(Id)` |
| `IdempotencyKey` | `varchar(100) NOT NULL` |
| `RequestFingerprint` | `char(64) NOT NULL` |

Keys/indexes: PK; unique `(CompanyId, DcNumber)`; unique `(CompanyId, IdempotencyKey)`; index `(CompanyId, Status, ExpectedReturnDate)`; indexes `ParentDeliveryChallanId`, `MaterialIssueRequestId`, `JobOrderId`; alternate key `(CompanyId, Id)`.

Checks/guards: inbound rows require a same-company outbound returnable parent and inherit its purpose/type/destination; `REJECTED_MATERIAL/SUBCONTRACT/DEMO` are returnable and `WARRANTY/BILL_BASED/CUSTOMER_PO_BASED` are non-returnable; outbound returnable rows require expected date and remain effectively `OUTSTANDING` until signed inbound quantities fully reconcile; creating a non-returnable outbound row atomically enqueues the immediate TD/MD event, and dispatch additionally requires department-owner approval; Job Order FK is mandatory for job-related purposes. No Customer PO FK exists yet; `CUSTOMER_PO_BASED` requires an external reference and evidence snapshot until that future module exists.

### 3.21 `delivery_challan_lines` - NEW

For serialized material, one line is recorded per serial with quantity 1; non-serialized material may use aggregate quantity. This avoids another serial junction table.

| Column | Definition |
|---|---|
| `Id` | `uuid PK` |
| `CompanyId` | `uuid NOT NULL` |
| `DeliveryChallanId` | `uuid NOT NULL FK delivery_challans(Id)` |
| `LineNumber` | `integer NOT NULL` |
| `ParentDeliveryChallanLineId` | `uuid NULL FK delivery_challan_lines(Id)`; required for inbound return |
| `MaterialIssueRequestLineId` | `uuid NULL FK material_issue_request_lines(Id)` |
| `QcInspectionRevisionId` | `uuid NULL FK qc_inspection_revisions(Id)`; rejected-QC source |
| `GoodsReceiptLineId` | `uuid NULL FK goods_receipt_lines(Id)`; excess source or replacement linkage |
| `ItemId` | `uuid NOT NULL FK items(Id)` |
| `InventorySerialId` | `uuid NULL FK inventory_serials(Id)` |
| `ItemCodeSnapshot` | `varchar(80) NOT NULL` |
| `UomSnapshot` | `varchar(32) NOT NULL` |
| `Quantity` | `numeric(24,6) NOT NULL` |
| `WeightUomId` | `uuid NULL FK uoms(Id)` |
| `DispatchedWeight` | `numeric(24,6) NULL` |
| `ReturnedWeight` | `numeric(24,6) NULL` |
| `CalculatedScrapWeight` | `numeric(24,6) NULL` |
| `VendorWeightExplanation` | `varchar(2000) NULL` |
| `RequiresQcSnapshot` | `boolean NOT NULL` |
| `ReplacementGoodsReceiptLineId` | `uuid NULL FK goods_receipt_lines(Id)` |
| `CreatedAt`, `CreatedBy` | Audit columns; immutable after dispatch/receipt |

Keys/indexes: PK; unique `(DeliveryChallanId, LineNumber)`; unique filtered `(DeliveryChallanId, InventorySerialId)` where serial is non-null; indexes on every typed source/parent FK; alternate key `(CompanyId, Id)`.

Checks/guards: positive quantity; serialized quantity equals 1; outbound line has exactly one source among approved MIR line, QC rejection revision, or GRN excess line; inbound line requires a parent line and may identify a replacement GRN line; returned-to-date cannot exceed dispatched quantity. Subcontract and demo inbound returns require QC; rejected-material replacement links to a new Gate/GRN and therefore receives full GRN QC rather than a duplicate DC-return posting. Subcontract outbound requires dispatched weight; inbound requires returned weight; `CalculatedScrapWeight = DispatchedWeight - cumulative ReturnedWeight`, is nonnegative, and positive scrap requires vendor explanation. Rejected/excess return quantities cannot exceed their current `PENDING_RETURNABLE_DC` balance.

### 3.22 `notification_events` - NEW, SHARED ENGINE

One generic event/outbox row. A module raises or cancels an event through the shared notification service; the delivery engine does not contain module-specific source FKs or event-type code.

| Column | Definition |
|---|---|
| `Id` | `uuid PK` |
| `CompanyId` | `uuid NOT NULL` |
| `EventType` | `varchar(120) NOT NULL`; extensible business key such as `STORES.QC_OVERDUE` |
| `SourceEntityType` | `varchar(120) NOT NULL`; stable logical source type |
| `SourceEntityId` | `uuid NOT NULL`; source identity within the named type |
| `SourceReferenceSnapshot` | `varchar(160) NOT NULL`; user-readable GRN/DC/etc. number |
| `RecipientRoleCodes` | `text[] NOT NULL`; role targets, never employee IDs |
| `TitleSnapshot` | `varchar(300) NOT NULL` |
| `BodySnapshot` | `text NOT NULL` |
| `DeepLinkSnapshot` | `varchar(500) NOT NULL`; company-scoped application route |
| `PayloadJson` | `jsonb NOT NULL DEFAULT '{}'`; versioned, non-secret display/context data |
| `NotBeforeAt` | `timestamptz NOT NULL`; immediate or scheduled activation time |
| `CancellationKey` | `varchar(200) NULL`; stable key used when the condition is resolved before activation |
| `Status` | `varchar(24) NOT NULL`; `SCHEDULED`, `READY`, `ACTIVE`, `COMPLETED`, `CANCELLED`, or `RECIPIENT_BLOCKED` |
| `IdempotencyKey` | `varchar(160) NOT NULL` |
| `CreatedAt`, `CreatedBy` | Creation audit columns |
| `ActivatedAt` | `timestamptz NULL` |
| `CompletedAt` | `timestamptz NULL` |
| `CancelledAt`, `CancelledBy`, `CancellationReason` | Nullable controlled cancellation audit |

Keys/indexes: PK; unique `(CompanyId, IdempotencyKey)`; unique filtered `(CompanyId, CancellationKey)` while status is schedulable/active; index `(Status, NotBeforeAt, Id)` for worker claiming; index `(CompanyId, SourceEntityType, SourceEntityId, CreatedAt)`; GIN index on `RecipientRoleCodes` only if role-target reporting needs it.

Checks/guards: nonblank type/source/title/body/link; at least one distinct role code; timestamp/status consistency; Company is copied from and checked against the source command context. `EventType` is data, not a database enum, so later modules can publish vendor-evaluation, calibration, task-overdue, or reorder events without an engine migration. Source identity is deliberately generic because notification evidence is not a financial integrity link; `PayloadJson` carries a schema-version property. Activation resolves every target role against active role membership in the event Company. A mandatory event becomes `RECIPIENT_BLOCKED` and raises an operational fault if any required role has no active recipient; it never substitutes a hard-coded employee ID.

### 3.23 `notification_recipients` - NEW, SHARED ENGINE

Resolved employee inbox rows and read state. Role resolution occurs at activation, so a scheduled event reaches the people holding the roles when it becomes due.

| Column | Definition |
|---|---|
| `Id` | `uuid PK` |
| `CompanyId` | `uuid NOT NULL` |
| `NotificationEventId` | `uuid NOT NULL FK notification_events(Id)` |
| `RecipientEmployeeId` | `uuid NOT NULL FK employees(Id)` |
| `ResolvedRoleCodes` | `text[] NOT NULL`; all event roles that matched this employee |
| `ResolvedAt` | `timestamptz NOT NULL` |
| `InAppAvailableAt` | `timestamptz NOT NULL` |
| `ReadAt` | `timestamptz NULL` |
| `ReadByEmployeeId` | `uuid NULL FK employees(Id)`; must equal the recipient |
| `ReadCorrelationId` | `varchar(100) NULL` |

Keys/indexes: PK; unique `(NotificationEventId, RecipientEmployeeId)`; alternate key `(CompanyId, Id)`; index `(CompanyId, RecipientEmployeeId, ReadAt, InAppAvailableAt DESC)` for unread badge/inbox; index `NotificationEventId`.

Checks/guards: role list is nonempty and a subset of the event targets; Company equals the event Company and the employee has an active company-role assignment for at least one recorded role at `ResolvedAt`; read fields are all null or all populated; only the recipient may mark the row read. The header unread badge is `COUNT(*) WHERE RecipientEmployeeId = current employee AND CompanyId = current company AND ReadAt IS NULL`. Opening the inbox does not mark anything read. The user opens a notification/deep link and invokes the explicit mark-read command; bulk mark-read is permitted only for that user's currently visible company inbox.

### 3.24 `notification_delivery_attempts` - NEW, APPEND-ONLY SHARED ENGINE

One immutable attempt per recipient and channel. In-app availability and every email attempt are witnessed without overwriting earlier failures.

| Column | Definition |
|---|---|
| `Id` | `uuid PK` |
| `CompanyId` | `uuid NOT NULL` |
| `NotificationRecipientId` | `uuid NOT NULL FK notification_recipients(Id)` |
| `Channel` | `varchar(20) NOT NULL`; `IN_APP` or `EMAIL` |
| `AttemptNumber` | `integer NOT NULL` |
| `Status` | `varchar(20) NOT NULL`; `SENT` or `FAILED` |
| `AttemptedAt` | `timestamptz NOT NULL` |
| `DeliveredAt` | `timestamptz NULL` |
| `ProviderMessageId` | `varchar(300) NULL` |
| `ErrorCode` | `varchar(100) NULL` |
| `ErrorDetail` | `varchar(2000) NULL` |
| `CorrelationId` | `varchar(100) NOT NULL` |

Keys/indexes: PK; unique `(NotificationRecipientId, Channel, AttemptNumber)`; unique `CorrelationId`; index `(CompanyId, Channel, Status, AttemptedAt)`; index `NotificationRecipientId`.

Checks/guards: positive sequential attempt number; status/timestamp/error fields agree; Company equals the recipient Company. Trigger rejects update/delete. Activation creates one successful `IN_APP` attempt and makes the inbox row visible in the same transaction; email is delivered asynchronously with append-only retries and bounded backoff. Event completion is derived when all recipients have in-app delivery and at least one successful email attempt. Read state is independent of email delivery.

### 3.25 Notification scheduling contract

The same enqueue/cancel/activate/deliver mechanism covers this module:

| Event type | Created/scheduled by | `NotBeforeAt` | Recipient roles | Cancellation condition |
|---|---|---|---|---|
| `STORES.QC_OVERDUE` | GRN finalisation | Snapshotted `QcDueAt` | `QC_MANAGER` | All effective GRN-line inspections finalised before due time |
| `STORES.RETURNABLE_DC_OVERDUE` | Returnable DC dispatch | End of mandatory expected return date in company timezone | `PURCHASE_MANAGER`, `TECHNICAL_DIRECTOR`, `MANAGING_DIRECTOR` | DC fully returned/closed before due time |
| `STORES.NON_RETURNABLE_DC_CREATED` | Non-returnable DC creation | Immediate | `TECHNICAL_DIRECTOR`, `MANAGING_DIRECTOR` | Never; creation and durable enqueue are atomic |
| `STORES.REJECTED_VENDOR_COLLECTION_OVERDUE` | Finalised QC rejection | Rejection finalisation plus seven calendar days | `PURCHASE_MANAGER`, `TECHNICAL_DIRECTOR`, `MANAGING_DIRECTOR` | Rejected quantity dispatched/collected before due time |

`PURCHASE_MANAGER` is the company role that currently resolves to PRIYA E; the design intentionally records the role and resolved holder rather than PRIYA's employee ID. If more than one active employee holds a target role for the company, every holder receives one deduplicated inbox row and email. A generic scheduler claims due events with `FOR UPDATE SKIP LOCKED`; idempotency and cancellation keys prevent duplicates and late sends. Future modules call the same contract with their own event type, source, schedule, role codes, title/body/link and payload. The engine requires no schema or branching change for vendor evaluation due, calibration due, task overdue, or stock below reorder.

## 4. Exact `advance.stock_movements` redesign

### 4.1 Corrected baseline

The repository model is authoritative. `stock_movements` already has `CompanyId` through `CompanyScopedAuditableEntity`. No CompanyId migration is proposed.

Existing columns retained:

- `Id uuid PK`, `CompanyId uuid NOT NULL`, audit fields, and legacy `Version`.
- `ItemId uuid NOT NULL`, `WarehouseId uuid NULL`, `RackBinId uuid NULL`.
- `MovementType varchar(40) NOT NULL`.
- `ReferenceType varchar(40) NOT NULL`, `ReferenceNumber varchar(120) NOT NULL`.
- `QuantityIn numeric(18,3)`, `QuantityOut numeric(18,3)`, `PostingDate date NOT NULL`.

Exact changes:

| Column/change | Definition |
|---|---|
| Quantity precision | Widen `QuantityIn` and `QuantityOut` to `numeric(24,6) NOT NULL DEFAULT 0`. |
| `LedgerSchemaVersion` | Add `smallint NOT NULL DEFAULT 1`; existing rows remain version 1 and every movement created by this module is version 2. |
| Location nullability | Existing version-1 rows retain their current nullability; version-2 trigger requires `WarehouseId` and `RackBinId`. |
| `WarehouseConditionLocationId` | Add nullable `uuid FK warehouse_condition_locations(Id)`; mandatory for version 2, nullable only for unprovable legacy version-1 rows. |
| `ConditionCode` | Add nullable `varchar(30)`; mandatory immutable mapping snapshot for version 2. |
| `StockPostingBatchId` | Add nullable `uuid FK stock_posting_batches(Id)`; mandatory for version 2. |
| `BatchLineOrdinal` | Add nullable `integer`; mandatory and positive for version 2. |
| `MovementLeg` | Add nullable `varchar(30)`; mandatory for version 2: `RECEIPT_IN`, `TRANSFER_OUT`, `TRANSFER_IN`, `ISSUE_OUT`, `DISPATCH_OUT`, `RETURN_IN`, or `REVERSAL`. |
| `GoodsReceiptLineId` | Add nullable typed FK to `goods_receipt_lines(Id)`. |
| `QcInspectionRevisionId` | Add nullable typed FK to `qc_inspection_revisions(Id)`. |
| `MaterialIssueRequestLineId` | Add nullable typed FK to `material_issue_request_lines(Id)`. |
| `DeliveryChallanLineId` | Add nullable typed FK to `delivery_challan_lines(Id)`. |
| `OriginGoodsReceiptLineId` | Add nullable provenance FK to `goods_receipt_lines(Id)`; identifies the receipt layer consumed and is not a source-document FK. |
| `InventorySerialId` | Add nullable FK to `inventory_serials(Id)`; null for non-serial aggregate movement. No batch/lot column is added. |
| `ReversesStockMovementId` | Add nullable self-FK; required for reversal rows. |
| `PostingIdentity` | Add nullable `varchar(200)`, deterministic identity of source, serial/aggregate and leg; mandatory for version 2. |
| Audit mutability | `UpdatedAt`, `UpdatedBy`, and `Version` remain physically present for compatibility but are never changed after insert. |

New keys/indexes:

- Unique filtered `(StockPostingBatchId, BatchLineOrdinal)` where batch is non-null.
- Unique filtered `(CompanyId, PostingIdentity)` where identity is non-null.
- Unique filtered `ReversesStockMovementId WHERE ReversesStockMovementId IS NOT NULL`.
- Index `(CompanyId, ItemId, WarehouseConditionLocationId, PostingDate, Id)` for ledger/balance queries.
- Index `(CompanyId, InventorySerialId, PostingDate, Id)` where serial is non-null.
- Indexes on every typed source FK, `OriginGoodsReceiptLineId`, and `StockPostingBatchId`.
- Composite tenant/location FKs enforce the same Company, Warehouse, Rack and mapping.

New checks/triggers:

- Exactly one of `QuantityIn` and `QuantityOut` is greater than zero; the other is zero.
- Every version-2 row has exactly one source FK: GRN custody uses `GoodsReceiptLineId`; QC disposition uses `QcInspectionRevisionId`; internal fulfilment uses `MaterialIssueRequestLineId`; and DC dispatch/return uses `DeliveryChallanLineId`. Serial and origin-receipt FKs supplement, never replace, that source. Legacy version-1 rows are preserved without invented source provenance.
- Reversal rows copy the original source FK, Item, serial, location and condition, swap quantity in/out exactly, and identify the original movement.
- Serialized movement quantity is exactly 1. Non-serialized movement has no serial FK.
- Every issue/DC outbound movement carries `OriginGoodsReceiptLineId`; serialized movement origin must agree with the serial's receipt provenance.
- `ReferenceType` and `ReferenceNumber` remain readable audit/display snapshots. They are derived and verified from the typed FK; they are not the integrity link.
- Trigger rejects every `UPDATE` and `DELETE` after the one controlled migration/backfill transaction. Corrections insert a reversal batch and new movements.
- Batch insert is rejected unless its legs reconcile: a GRN batch contains the ordered QC-hold and excess pending-return inbound legs; a QC batch has equal out/in quantities for each Item/serial; MIR/DC quantities remain within approved/source balances; a reversal exactly negates its target batch.

No over-receipt override field is added to GRN lines or movements. Source-less adjustment support is deliberately not enabled in this module. The future adjustment module must add its typed override evidence and relax the exactly-one-source rule only for `ADJUSTMENT`; no free-text or source-less movement can be posted now.

The migration may promote a legacy row from version 1 to version 2 only when location, condition, batch and typed source can be proven deterministically. Otherwise it remains an immutable version-1 historical row. This avoids fabricating provenance merely to satisfy new constraints while making all new module postings fully constrained.

### 4.2 Posting examples

- GRN finalisation: `ReceivedQuantity` posts into category `QC_HOLD`; `ExcessRejectedQuantity` posts separately into `PENDING_RETURNABLE_DC` at that QC rack. Both use the GRN-line source, but only received quantity contributes to PO received-to-date.
- QC accepted quantity: `TRANSFER_OUT` from `QC_HOLD` plus `TRANSFER_IN` to the Stores-selected `AVAILABLE` location. Both use the same QC revision source and posting batch.
- QC rejected quantity: `TRANSFER_OUT` from `QC_HOLD` plus `TRANSFER_IN` to `PENDING_RETURNABLE_DC` at the same warehouse/rack. It is physically retained but cannot be reserved or issued.
- Internal approved issue: `ISSUE_OUT` from `AVAILABLE`, sourced to the approved MIR line and carrying `JobOrderId` through the MIR header.
- DC dispatch: `DISPATCH_OUT` from `AVAILABLE` or `PENDING_RETURNABLE_DC`, sourced to the outbound DC line.
- DC return: `RETURN_IN` sourced to the inbound DC line. QC-required returns enter `QC_HOLD`; a rejected-material replacement may instead identify the new Gate/GRN line so receipt is not posted twice.
- Correction: a `REVERSAL` batch negates every row of the erroneous finalised posting, followed by the replacement document/revision and its new batch.

Stock balance is a query/view, not a mutable balance table: sum `QuantityIn - QuantityOut` by Company, Item, condition location and optional serial. Received-to-date is likewise a query over signed effective GRN `ReceivedQuantity` by PO line. `OriginGoodsReceiptLineId` preserves receipt-layer and bill-date provenance for warranty recomputation after issue.

## 5. State machine

Stored document status is deliberately small: draft rows may be edited with optimistic concurrency; finalised rows are immutable. “Reversed” and “superseded” are effective states derived from append-only reversal/revision records, so the original finalised row is never updated.

### 5.1 Gate Entry

| State | Meaning | Allowed transition |
|---|---|---|
| `DRAFT` | Header and delivered PO-line quantities may be edited. It has no downstream effect. | `FINALIZE` -> `FINALIZED_AWAITING_GRN`. |
| `FINALIZED_AWAITING_GRN` | Immutable, effective Gate Entry; eligible for exactly one effective GRN. | GRN draft creation -> `GRN_IN_PROGRESS`; Gate reversal is allowed only when no effective downstream GRN remains. |
| `GRN_IN_PROGRESS` | Derived: an associated GRN draft exists. Gate remains immutable. | GRN finalisation -> `GRN_FINALIZED`; abandoning the GRN draft returns the derived state to `FINALIZED_AWAITING_GRN`. |
| `GRN_FINALIZED` | Derived: its effective GRN is finalised. | Downstream reversals must occur in reverse order before Gate reversal. |
| `REVERSED` | Derived: a finalised Gate reversal document points to this Gate Entry. | Terminal. A corrected normal Gate Entry is a new document. |

The reversal Gate Entry itself follows `DRAFT -> FINALIZED`; finalisation validates that its lines mirror the target. There is no update, cancel, or direct “mark reversed” operation on the original.

### 5.2 GRN

Prerequisite: only an effective finalised Gate Entry can create a GRN draft. Vendor bill data is mandatory before finalisation.

| State | Meaning | Allowed transition |
|---|---|---|
| `DRAFT` | GRN header, Item snapshots, quantities and serials may be edited. | `FINALIZE` -> `FINALIZED_QC_HOLD`. |
| `FINALIZED_QC_HOLD` | Immutable GRN. The finalisation transaction inserted the QC-hold posting batch; nothing is available for issue. | QC draft creation -> `QC_IN_PROGRESS`; GRN reversal only if no effective finalised QC revision exists. |
| `QC_IN_PROGRESS` | Derived: the one logical inspection has a draft revision. | QC finalisation -> `QC_FINALIZED_STOCK_POSTED`. |
| `QC_FINALIZED_STOCK_POSTED` | Derived: QC is final and its available/pending-return posting batch committed. | Reverse QC posting/revision before any GRN reversal. |
| `REVERSED` | Derived: a finalised GRN reversal and its exact ledger reversal exist. | Terminal. A corrected GRN is a new effective document after reversal. |

The GRN finalisation transaction locks affected PO lines, recomputes received-to-date, rejects over-receipt, validates all Gate lines and required serials, finalises the document, appends status history, creates its idempotent posting batch/movements and audit event, then commits. The GRN header retains the command idempotency key and request fingerprint. Failure rolls back everything.

### 5.3 QC inspection

One immutable `qc_inspections` identity exists per GRN line or QC-required inbound DC line. Each attempt/correction is a revision.

| State | Meaning | Allowed transition |
|---|---|---|
| `REVISION_DRAFT` | Quantities, per-sample policy results, serial dispositions and accepted destination may be edited. | `FINALIZE_AND_POST` -> `REVISION_FINALIZED_STOCK_POSTED`. |
| `REVISION_FINALIZED_STOCK_POSTED` | Immutable revision; accepted stock is `AVAILABLE`, rejected stock is `PENDING_RETURNABLE_DC`. | Correction command creates a new `CORRECTION` draft after atomically reversing the prior revision's posting. |
| `REVISION_SUPERSEDED` | Derived for an older revision when a later correction revision finalises. | Terminal; the row and its evidence remain unchanged. |

There is no stock-availability bypass. QC finalisation and ledger posting are one transaction, so no persisted “final but unposted” state exists. Structured parameters are optional: when no effective policy exists, the inspector finalises the quantity decision with zero parameter rows. Missing policy never blocks QC.

The due time is `goods_receipts.QcDueAt`, calculated from the snapshotted `QC_COMPLETION_DAYS` value. GRN finalisation schedules the shared `STORES.QC_OVERDUE` event for that instant. Completing all line inspections first cancels it; otherwise activation notifies the current-company `QC_MANAGER`. The queue also shows `OVERDUE` and warns Stores; neither the dashboard condition nor notification mutates the immutable GRN.

### 5.4 Physical/ledger condition path

`VENDOR/OUTSIDE -> QC_HOLD -> AVAILABLE` for accepted quantity.

`VENDOR/OUTSIDE -> QC_HOLD -> PENDING_RETURNABLE_DC -> DC_DISPATCHED/OUTSIDE` for QC-rejected quantity.

`VENDOR/OUTSIDE -> PENDING_RETURNABLE_DC -> DC_DISPATCHED/OUTSIDE` for delivered excess, which is held separately in the category QC rack and never enters an available/store rack.

`AVAILABLE -> MATERIAL_ISSUED` for an approved internal issue, or `AVAILABLE -> DC_DISPATCHED/OUTSIDE` for an approved external movement.

`DC_OUTSIDE -> DC_RETURN_CUSTODY -> QC_HOLD -> AVAILABLE/PENDING_RETURNABLE_DC` for QC-required returns.

Strict-sequence guards reject:

- GRN creation/finalisation without an effective finalised Gate Entry.
- QC creation/finalisation without an effective finalised GRN and QC-hold posting.
- available or pending-return posting without a finalising QC revision.
- any issue posting without an approved MIR and remaining approved line quantity.
- any outbound DC posting without its MIR/QC-rejection/GRN-excess source and required approvals.
- closing a returnable DC before cumulative inbound-return quantities reconcile to dispatched quantities.
- direct stock-movement writes without a typed source and authorised posting batch.
- reversal of an upstream document while an effective downstream document/posting remains.

### 5.5 Material Issue Request

| State | Meaning | Allowed transition |
|---|---|---|
| `DRAFT` | Header, one destination and many Item lines are editable. | `SUBMIT` -> `SUBMITTED`. |
| `SUBMITTED` | Frozen approval route; no issue permitted. | `APPROVE` -> `APPROVED`; `REJECT` -> `REJECTED`. |
| `APPROVED` | Production Manager or department owner approval exists. | First signed issue/DC posting -> `PARTIALLY_FULFILLED` or `FULFILLED`. |
| `PARTIALLY_FULFILLED` | Some approved quantity has moved. | Further valid postings -> `FULFILLED`. |
| `FULFILLED` | Signed issued quantities equal all requested quantities. | Reversal postings derive `PARTIALLY_FULFILLED` or `REVERSED`. |
| `REJECTED` / `REVERSED` | Terminal effective states. | New requirement uses a new request. |

### 5.6 Delivery Challan

Outbound DC sequence is `DRAFT -> SUBMITTED -> APPROVED -> DISPATCHED`. Approval resolution has no shortcut: it either consumes the approved MIR authority or records the separate DC approval required for rejected/non-returnable material.

- Returnable dispatch immediately derives `OUTSTANDING`, remains outstanding past its mandatory expected return date, becomes `PARTIALLY_RETURNED` after a partial inbound DC, and becomes `CLOSED` only when cumulative returns/replacement links reconcile every line.
- Returnable dispatch schedules `STORES.RETURNABLE_DC_OVERDUE`; full reconciliation before the expected date cancels it. Rejected-material QC also schedules the separate seven-day vendor-collection event, cancelled only by dispatch/collection.
- Creating a non-returnable DC atomically enqueues `STORES.NON_RETURNABLE_DC_CREATED` for TD/MD. Department-owner approval is still required before dispatch; after dispatch it becomes `CLOSED`.
- Each inbound return document follows `DRAFT -> RECEIVED`; its posting and the parent outstanding-balance update are atomic.
- Reversal is a new posting/document event. Dispatched quantities are never edited in place.

### 5.7 Minimal Job Order

`DRAFT -> OPEN -> INSTALLED -> CLOSED`. The record is created manually. Setting `InstallationDate` moves an open Job Order to `INSTALLED` and causes warranty queries to recompute every issued GRN layer as the earlier of the immutable bill-date-plus-13-month limit and installation-date-plus-12-month limit. The GRN is not updated.

## 6. Configuration and immutable snapshots

Editable values live in `business_rule_configuration_versions`, one effective append-only version per Company and RuleKey. Only `TECHNICAL_DIRECTOR`, `MANAGING_DIRECTOR`, and `IT_MANAGER` may append a version. Every row records actor, role, time, old value, new value, reason, previous version and effective-from time.

For this module the relevant values are `SERIAL_CAPTURE_THRESHOLD`, initially 5,000 per unit rate, and `QC_COMPLETION_DAYS`, initially 2. At **GRN draft creation**, the service:

1. resolves the effective company configuration row;
2. reads the company-specific Item `SerialCaptureMode`;
3. stores the complete canonical configuration object plus version IDs in `goods_receipts.ConfigurationSnapshotJson`;
4. stores a SHA-256 identity in `ConfigurationSnapshotHash`; and
5. writes the resolved serial threshold, version FK, Item override and final `REQUIRED/OPTIONAL` decision on every GRN line, comparing the threshold to `receipt line value / receipt quantity`; and
6. writes the QC-day version, value and resulting `QcDueAt` on the GRN header.

Every later validation uses those snapshots. Configuration changes therefore affect only GRNs created after the new version becomes effective. A draft created earlier keeps its original values; submission/finalisation does not re-resolve them.

QC policy is optional effective-dated operational master data rather than a Section 17.1 configuration value. Each QC draft resolves zero or more effective policy rows. When rows exist, it snapshots each policy into per-sample `qc_inspection_parameter_results`; when none exist, the inspection retains zero parameter rows and proceeds with its accept/reject quantity decision. Later policy edits do not change an existing revision. A correction revision resolves policies afresh at its own creation time.

Warranty has two layers: the immutable GRN line stores the initial bill-date-plus-13-month limit, while the effective warranty query joins issued stock provenance to the minimal Job Order. Before installation it returns the bill limit; after `JobOrder.InstallationDate` is recorded it returns the earlier of installation plus 12 months and the bill limit. No finalised GRN row is rewritten.

## 7. API surface

All routes are under `/api/v1/stores`, require the selected company context, apply database role/page permission plus record scope, accept `Idempotency-Key` on commands, and use expected `Version` for draft edits. No request may set `CompanyId`.

### Gate Entry

| Method and route | Purpose |
|---|---|
| `GET /gate-entries` | Company-scoped list/filter by PO, vendor, date and effective state. |
| `GET /gate-entries/{id}` | Header, delivered lines, status history and downstream effective GRN link. |
| `POST /gate-entries` | Create draft against one issued/eligible PO with delivered lines. |
| `PUT /gate-entries/{id}` | Replace editable draft fields/lines using expected Version. |
| `POST /gate-entries/{id}/finalize` | Validate and freeze Gate Entry. |
| `POST /gate-entries/{id}/reversals` | Create/finalise a typed reversal after downstream reversal prerequisites. |

There is no update or delete endpoint for a finalised Gate Entry.

### GRN and serials

| Method and route | Purpose |
|---|---|
| `GET /goods-receipts` | Company-scoped GRN list/filter by Gate, PO, bill, vendor, date and state. |
| `GET /goods-receipts/{id}` | Full immutable header/line snapshots, serials, warranty-at-receipt, status and posting links. |
| `POST /gate-entries/{gateEntryId}/goods-receipt` | Create the sole effective GRN draft and snapshot configuration/Item/PO/Gate values. |
| `PUT /goods-receipts/{id}` | Edit a GRN draft and its lines using expected Version. |
| `POST /goods-receipts/{id}/serials/validate` | Return duplicate warnings and candidate disambiguation; performs no final write. |
| `POST /goods-receipts/{id}/finalize` | Lock PO lines, cap received-to-date, validate bill/serials, freeze GRN, post ordered quantity to QC hold and excess separately to pending-return custody. |
| `POST /goods-receipts/{id}/reversals` | Reverse an eligible GRN and its posting batch; no edit to original. |
| `GET /purchase-orders/{poId}/receipt-position` | Computed ordered, effective received, Gate-delivered, excess and remaining quantity by PO line. |

### Item ERP barcode

| Method and route | Purpose |
|---|---|
| `GET /items/{itemId}/inventory-setting` | Current-company ERP barcode and serial mode for the shared Item. |
| `POST /items/{itemId}/erp-barcode` | Allocate through the existing Purchase sequence table and create the immutable company ERP barcode. |
| `PUT /items/{itemId}/serial-capture-mode` | Change company Item override with reason and existing controlled before/after history. |
| `GET /items/{itemId}/barcode-label` | Download label data/artifact from the stored ERP barcode; direct printer-hardware control is deferred. |
| `GET /items/{itemId}/change-history` | Shared Item and company-setting change evidence visible in current scope. |

### QC and category routes

| Method and route | Purpose |
|---|---|
| `GET /qc/queue` | Stores dashboard of effective GRN/DC-return lines awaiting QC, including category rack, age, due time and overdue warning. |
| `GET /qc/inspections/{id}` | Logical inspection with all immutable revisions, per-sample parameter results, serial dispositions and posting links. |
| `POST /goods-receipt-lines/{lineId}/qc-inspection` | Create the one logical inspection and initial draft revision. |
| `POST /delivery-challan-lines/{lineId}/qc-inspection` | Create inspection for a QC-required inbound return line. |
| `PUT /qc/revisions/{revisionId}` | Edit a draft revision, per-sample parameter results and serial dispositions. |
| `POST /qc/revisions/{revisionId}/finalize-and-post` | Validate reconciliation/evidence, freeze revision and atomically post accepted/pending-return movements. |
| `POST /qc/inspections/{id}/corrections` | Reverse the effective QC posting and create the next correction draft with mandatory reason. |
| `GET /category-routes` | Read effective current-company QC/default-accepted mappings. |
| `POST /category-routes` | Create a future/effective route under authorised master administration. |
| `POST /category-routes/{id}/close` | Effective-date close a route; never rewrites document snapshots. |

### Minimal Job Orders

| Method and route | Purpose |
|---|---|
| `GET /job-orders` / `GET /job-orders/{id}` | Company-scoped list/detail with issue/DC links and effective component warranties. |
| `POST /job-orders` | Manually create the minimal Job Order. |
| `PUT /job-orders/{id}` | Edit permitted fields using expected Version. |
| `POST /job-orders/{id}/open` | Move draft to open. |
| `POST /job-orders/{id}/installation-date` | Record installation date and expose recomputed effective warranty dates without changing GRNs. |
| `POST /job-orders/{id}/close` | Close the minimal Job Order. |

### Material Issue Requests

| Method and route | Purpose |
|---|---|
| `GET /material-issue-requests` / `GET /material-issue-requests/{id}` | List/detail with destination, approval, line balances and postings. |
| `POST /material-issue-requests` | Create one-destination, many-line draft. |
| `PUT /material-issue-requests/{id}` | Edit draft header/lines using expected Version. |
| `POST /material-issue-requests/{id}/submit` | Freeze destination, lines and approval route. |
| `POST /material-issue-requests/{id}/decisions` | Production Manager or resolved department owner approves/rejects. |
| `POST /material-issue-requests/{id}/issue` | Post an approved internal issue from available locations; no exception path. |
| `POST /material-issue-requests/{id}/reversals` | Append an exact issue reversal. |

### Delivery Challans and returns

| Method and route | Purpose |
|---|---|
| `GET /delivery-challans` / `GET /delivery-challans/{id}` | List/detail with lines, approvals, notifications, postings, returns and outstanding balance. |
| `GET /delivery-challans/outstanding` | Returnable-DC dashboard filtered by expected date, overdue age, vendor/customer and purpose. |
| `POST /delivery-challans` | Create outbound returnable or non-returnable draft from approved MIR, QC rejection or GRN excess. |
| `PUT /delivery-challans/{id}` | Edit an outbound/inbound draft using expected Version. |
| `POST /delivery-challans/{id}/submit` | Freeze source quantities, destination, expected date and approval route. |
| `POST /delivery-challans/{id}/decisions` | Record required rejected-material or department-owner decision. |
| `POST /delivery-challans/{id}/dispatch` | Validate authority/notifications and post outbound movement. |
| `POST /delivery-challans/{id}/returns` | Create an inbound return draft against an outstanding returnable DC. |
| `POST /delivery-challans/{returnId}/receive` | Receive partial/full return, record weights/scrap explanation and post custody or link replacement GRN. |
| `POST /delivery-challans/{id}/reversals` | Reverse an eligible dispatch/return posting without editing history. |
| `GET /delivery-challans/{id}/notifications` | TD/MD in-app/email delivery and retry evidence for non-returnable DC. |

Returnable closure is derived and automatic after full reconciliation; there is no endpoint that can force-close an outstanding quantity.

### Shared notifications

| Method and route | Purpose |
|---|---|
| `GET /notifications?state=unread` | Current employee/current company inbox; returns unread count, title, age, source reference and deep link. |
| `GET /notifications/{recipientId}` | Read one resolved notification and its delivery evidence within the current company. |
| `POST /notifications/{recipientId}/read` | Explicitly mark the current employee's inbox row read, idempotently. |
| `POST /notifications/read-visible` | Mark only the current employee's supplied, currently visible company inbox rows read. |
| `POST /internal/notification-events` | Authenticated internal publisher contract for any module: enqueue immediate/scheduled role-targeted event. Not exposed to ordinary users. |
| `POST /internal/notification-events/{id}/cancel` | Authenticated idempotent cancellation when a scheduled business condition is resolved. |
| `GET /admin/notification-events` | Authorised company-scoped operations view of scheduled, blocked, failed and completed events. |
| `POST /admin/notification-deliveries/{recipientId}/retry-email` | Authorised retry request; creates a new immutable attempt rather than editing evidence. |

The application shell displays an unread-count badge for the selected company. Selecting it opens the inbox; selecting an item follows its stored company-scoped deep link and explicitly marks that recipient row read. Company switching changes the inbox and badge. There is no cross-company notification view in this scope.

### Stock ledger and configuration

| Method and route | Purpose |
|---|---|
| `GET /stock/ledger` | Filtered append-only ledger by date, Item, condition, warehouse/rack, source or serial. |
| `GET /stock/balances` | Computed balances; only `AVAILABLE` is reservable/issuable. |
| `GET /stock/posting-batches/{id}` | Batch, typed source, all legs, idempotency and reversal chain. |
| `GET /stock/serials/{serial}` | Company-scoped serial provenance and current condition/location. |
| `GET /job-orders/{id}/component-warranties` | Effective warranty by consumed GRN layer/serial using bill and installation dates. |
| `GET /configuration/{ruleKey}` | Effective value and version for current company. |
| `GET /configuration/{ruleKey}/history` | Immutable version/change history. |
| `POST /configuration/{ruleKey}/versions` | Append a version; TD, MD or IT Manager only, with reason. |

There is intentionally no public `POST/PUT/DELETE /stock-movements` endpoint and no adjustment endpoint.

## 8. Resolved risks and implementation constraints

1. **Excess custody resolved.** Excess is a GRN-line quantity, posts to `PENDING_RETURNABLE_DC` in the category QC rack, is excluded from PO received-to-date and available stock, and leaves through a DC sourced to that GRN line.
2. **Category routing resolved.** The Item's current master category is validated and snapshotted on the GRN line; that category selects the one effective company/category route.
3. **Vendor bill mapping resolved.** A company/bill number can have only one effective normal GRN. Reversal preserves history and permits a corrected replacement to reuse the bill number.
4. **Empty QC policy resolved.** Zero effective policies means zero parameter-result rows; accept/reject quantity evidence still finalises. Missing policy never blocks QC.
5. **Serial threshold basis resolved.** The snapshotted unit rate is receipt line value divided by receipt quantity and is compared with the snapshotted 5,000 threshold, subject to Item override.
6. **Warranty recomputation resolved.** GRN preserves bill-plus-13 months; effective warranty is a query over receipt provenance and Job Order installation date, never a GRN update.
7. **QC deadline resolved.** The configurable two-day value is snapshotted on the GRN, `QcDueAt` is stored, and the pending-QC dashboard derives age/overdue warnings.
8. **DC accumulation risk resolved.** Returnable/non-returnable DC, dispatch, partial inbound returns, outstanding/overdue state, replacement linkage, subcontract weight/scrap explanation, approvals and notifications are inside this module.

Implementation constraints, not open business questions:

- `IsoReceiptVerificationJson` needs a versioned module-owned JSON schema because the exact ISO field catalogue/signature attachment set remains deferred.
- Existing number-sequence allocation is reused; display formats must be configured without rewriting issued numbers.
- Cross-row invariants require serializable service transactions and narrow PostgreSQL deferred triggers/functions; application-only checks are insufficient.
- Ledger/source quantities use `numeric(24,6)` consistently.
- The future Vendor Invoice module must reference the GRN bill identity rather than create a conflicting second bill.

No unresolved contradiction blocks the final schema design.

## 9. ISO 9001 gaps explicitly not satisfied by this module

These requirements are recorded here so completing the Stores flow is not mistaken for completing the ISO 9001 control set:

1. **Vendor performance and re-evaluation:** not satisfied. It requires months of on-time-delivery, rejection and price-variance history from GRN/QC before meaningful evaluation and re-evaluation can be implemented.
2. **Calibration register:** not satisfied. Equipment identity, calibration intervals, certificates, due/overdue state and calibration history belong with the future QC/calibration module. The shared notification engine can later deliver `CALIBRATION_DUE` events without being redesigned.
3. **Shelf-life and batch tracking:** not satisfied. Batch/lot capture was explicitly deferred by decision Q7; therefore expiry control, FEFO and batch traceability are absent.
4. **Document control for QC sheets and vendor certificates:** not satisfied. This module does not yet provide governed file attachment, version, approval, retention or retrieval evidence for those documents. `IsoReceiptVerificationJson` is metadata, not a substitute for controlled files.

## 10. Final new-table list

All 24 tables below are `ESSENTIAL` for the now-final module scope. The four previously deferred support tables are not included in this count.

| # | New table | What it holds and why it is needed | Marking |
|---:|---|---|---|
| 1 | `item_company_inventory_settings` | Company ERP barcode and serial override for shared Items. | `ESSENTIAL` |
| 2 | `business_rule_configuration_versions` | Immutable serial-threshold and QC-deadline versions used by document snapshots. | `ESSENTIAL` |
| 3 | `store_category_routes` | One company/category QC rack, pending-return mapping and default accepted location. | `ESSENTIAL` |
| 4 | `gate_entries` | PO-linked physical arrival header and ISO receipt evidence. | `ESSENTIAL` |
| 5 | `gate_entry_lines` | Delivered quantities by PO line, including physical excess. | `ESSENTIAL` |
| 6 | `goods_receipts` | One-Gate/one-bill GRN header, rule snapshot, QC due date and reversal identity. | `ESSENTIAL` |
| 7 | `goods_receipt_lines` | Ordered receipt, segregated excess, Item/tax/UOM snapshots, unit rate and initial warranty. | `ESSENTIAL` |
| 8 | `inventory_serials` | Durable company-unique serial identities across reversal/correction. | `ESSENTIAL` |
| 9 | `goods_receipt_line_serials` | GRN capture occurrences and duplicate-disambiguation evidence. | `ESSENTIAL` |
| 10 | `stores_document_status_history` | Append-only Gate, GRN, QC, Job Order, MIR and DC lifecycle evidence. | `ESSENTIAL` |
| 11 | `qc_inspections` | Stable one-per-source-line logical inspection identity. | `ESSENTIAL` |
| 12 | `qc_inspection_revisions` | Immutable inspection decisions, corrections, quantities, inspector and destinations. | `ESSENTIAL` |
| 13 | `qc_inspection_parameter_results` | Optional per-policy, per-sample readings when policies exist. | `ESSENTIAL` |
| 14 | `qc_inspection_serial_dispositions` | Accepted/rejected result for every serialized unit. | `ESSENTIAL` |
| 15 | `stock_posting_batches` | Atomic, idempotent grouping and reversal of all ledger legs. | `ESSENTIAL` |
| 16 | `job_orders` | Stable minimal Job Order, machine/customer fields and installation date for issue/DC linkage and warranty. | `ESSENTIAL` |
| 17 | `material_issue_requests` | One-destination issue authority, purpose, approval route and fulfilment state. | `ESSENTIAL` |
| 18 | `material_issue_request_lines` | Many requested Items/quantities and the typed source for approved issue postings. | `ESSENTIAL` |
| 19 | `stores_approval_history` | Immutable MIR and DC approval decisions against snapshotted routes. | `ESSENTIAL` |
| 20 | `delivery_challans` | Outbound and inbound-return DC headers, expected dates, destination and outstanding lifecycle. | `ESSENTIAL` |
| 21 | `delivery_challan_lines` | Typed material sources, serials, quantities, return reconciliation and subcontract weight/scrap evidence. | `ESSENTIAL` |
| 22 | `notification_events` | Generic immediate/scheduled/cancellable role-targeted event and outbox source for every module. | `ESSENTIAL` |
| 23 | `notification_recipients` | Company-role-resolved employee inbox rows and explicit unread/read evidence. | `ESSENTIAL` |
| 24 | `notification_delivery_attempts` | Append-only in-app/email delivery and retry evidence. | `ESSENTIAL` |

Existing tables modified/reused but not counted as new include `stock_movements`, `purchase_number_sequences`, `warehouse_condition_locations`, `qc_inspection_policies`, `controlled_configuration_histories` and the existing masters/Purchase tables.

## 11. End-to-end user capability

When this module ships, a user will be able to:

- manually create a minimal Job Order with stable ID, machine/customer facts and later installation date;
- create a PO-linked Gate Entry with delivered quantities, then one mandatory-bill GRN with immutable Item snapshots, serials, warranty basis and separately held excess;
- see pending-QC age and overdue warnings, inspect each GRN or QC-required return line with or without configured parameters, move accepted stock to a selected store rack, and isolate rejected stock;
- create a one-destination multi-Item Material Issue Request, obtain Production Manager/department-owner approval, and issue only approved available stock to the referenced Job Order/destination;
- create and dispatch returnable DCs for rejection, subcontract and demo, receive partial/full returns, track mandatory expected dates, record subcontract dispatched/returned weight and vendor-explained scrap, route required returns through QC, and close only after reconciliation;
- create non-returnable warranty, bill-based and customer-PO-based DCs after department-owner approval, with mandatory TD/MD in-app and email notification evidence;
- trace every receipt, QC decision, issue, DC dispatch/return, serial and reversal through the append-only ledger;
- see a current-company unread notification badge/inbox, open source-linked notifications, mark them read, and receive the same events by email; and
- view effective component warranty after installation without altering the original GRN.

The user still cannot:

- create or link native Customer POs, offers or contract reviews;
- create Estimated BOMs, roll up Actual BOMs, or post labour hours;
- create a subcontract PO or allocate subcontract PO bill value;
- create installation reports or maintain the full installed-machine register;
- generate vendor-performance/KPI reports or e-way-bill payloads;
- control barcode-printer hardware;
- use batch/lot or shelf-life tracking, source-less stock adjustments, the calibration register, controlled QC/vendor-certificate attachments, or vendor re-evaluation.

RESULT_REPORTED_PENDING_WITNESS
## Foundation 2 implementation contract: ownership and custody

Foundation 2 represents ownership independently from physical custody. An inventory account holder may be a SESS company, external customer or vendor, or employee; ownership accounts identify SESS inventory, customer property, supplier-loan stock and demo custody, while custody accounts identify warehouse, rack, employee, vehicle, site or external-party possession. A rack can therefore hold multiple identifiable ownership classes without merging their balances.

Customer property uses explicit custody-case types for other-brand modification, SESS machine warranty return, SESS spare warranty return and removed customer parts. Every case records the customer's inbound returnable DC. A machine may be received before a customer PO, but it remains RECEIVED_AWAITING_COMMERCIAL_AUTHORIZATION; work cannot start until its case or line is backed by the required offer and customer-PO scope. Other-brand work is always chargeable. Warranty scope may be NOT_REQUIRED for free replacement under warranty terms. A line outside PO scope remains unauthorized until a separate offer and PO are linked, so a future Stores issue command can fail closed.

Removed parts retain the customer ownership account. SESS ownership is possible only through an explicit CUSTOMER_BUYBACK ownership transfer with an agreement reference; ownership is never changed by editing an account foreign key. Stores records the due date after management consultation. The due date has no database or application default and carries the employee and timestamp that set it; a later notification slice uses the due-date index to notify Technical Director and Managing Director when overdue.

Supplier-loan stock uses zero SESS inventory value plus append-only memo-liability events. Closing the loan requires both a real supplier purchase order and goods receipt. No ad-hoc payable or free-of-cost conversion path is represented.

### Scanner-first capture contract

Barcode scanning is the primary item and serial capture path. Frontend forms must keep focus on the next expected scan field and treat the scanner's Enter or Tab terminator as “accept this value and advance”. Manual typing remains an accessible fallback, not the default interaction. The API still normalizes and revalidates every scanned value, and finalization—not the browser—enforces item identity and serial uniqueness. USB keyboard-wedge scanners therefore require no scanner-specific server protocol, while later camera or mobile scanners can call the same APIs.
