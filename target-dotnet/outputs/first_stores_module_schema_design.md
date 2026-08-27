# First Stores Module - Schema Design Report

Status: design only. No code, migration, or database operation is authorised by this report.

Authoritative process: `outputs/sess_business_process_purchase_stores.md`

Architecture pattern reference: `outputs/backend_architecture_reference.md`

## 1. Scope and design rules

This design covers Gate Entry, one effective GRN per Gate Entry, mandatory vendor-bill evidence, GRN-line Item snapshots, serial capture, Item ERP barcode generation, one logical QC inspection per GRN line with immutable revisions, company/category QC routing, accepted and rejected disposition, and the append-only stock ledger.

Issue, DC creation or dispatch, subcontract transactions, Job Order, BOM, vendor-performance reporting, e-way bill, batch/lot tracking, and source-less adjustment commands are excluded.

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
| `employees` | Shared employee identity used for received-by, inspector, finaliser, and Stores poster. | None. |
| `purchase_orders` | Company-scoped PO header with VendorId, DeliveryWarehouseId, status and immutable commercial snapshot. | Add/retain alternate key `UNIQUE (CompanyId, Id)` for tenant-safe FKs; no received-to-date column. |
| `purchase_order_lines` | Company-scoped PO line with PurchaseOrderId, ItemId, OrderedQuantity, UnitRate, UOM and commercial snapshots. | Add/retain `UNIQUE (CompanyId, Id)` and `UNIQUE (PurchaseOrderId, Id)` for composite FKs; no received-to-date column. |
| `warehouses` | Company-scoped warehouse; defaults include accepted and QC-hold locations. | Retained. Default accepted destination is offered through the new category route, not forced. |
| `rack_bins` | Company-scoped warehouse rack/bin; `UNIQUE (WarehouseId, BinCode)`; alternate key `(WarehouseId, Id)`. | Retained. Add/retain `UNIQUE (CompanyId, Id)` and enforce Warehouse and Rack company equality. |
| `warehouse_condition_locations` | Company-scoped effective mapping of warehouse/rack to condition; currently supports `AVAILABLE`, `QC_HOLD`, `REJECTED`, `QUARANTINE`, `RETURN_TO_VENDOR`, and `SCRAP`. | Extend condition vocabulary with `PENDING_RETURNABLE_DC`; add/retain `UNIQUE (CompanyId, Id)`. |
| `qc_inspection_policies` | Company-scoped effective policy rows owned by exactly one Item or ItemCategory, including parameter code, UOM, limits, method, sample size and approval state. | Retained as the source. Finalisation snapshots each resolved policy into normalised result rows. |
| `stock_movements` | Existing company-scoped minimal ledger; detailed redesign is in Section 4. | Modified in place. |
| `stock_reservations` | Existing company-scoped reservation foundation. | No write path in this module. QC-hold and pending-return quantities are never reservable. |
| `controlled_configuration_histories` and `audit_logs` | Existing generic configuration/audit evidence. | Retained, but neither replaces the typed append-only histories below. |

## 3. New and modified table definitions

### 3.1 `stores_number_sequences` - NEW

Gap-tolerant sequence storage for Gate Entry, GRN, QC document numbers, and per-company Item ERP barcodes. Number presentation remains format-configurable; schema uniqueness does not depend on a hard-coded display format.

| Column | Definition |
|---|---|
| `Id` | `uuid PK` |
| `CompanyId` | `uuid NOT NULL FK companies(Id)` |
| `SequenceType` | `varchar(30) NOT NULL`; `GATE_ENTRY`, `GRN`, `QC_INSPECTION`, or `ITEM_BARCODE` |
| `FinancialYear` | `varchar(12) NULL`; required for document sequences, null for the continuous Item barcode sequence |
| `LastNumber` | `bigint NOT NULL DEFAULT 0` |
| `Version` | `bigint NOT NULL DEFAULT 0`, concurrency token |
| `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` | Standard audit columns |

Keys/indexes: PK `Id`; unique `(CompanyId, SequenceType, FinancialYear) NULLS NOT DISTINCT`; index `(CompanyId, SequenceType)`.

Checks: `LastNumber >= 0`; allowed `SequenceType`; financial year nullability matches type. Allocation occurs in the document transaction under a row/advisory lock.

### 3.2 `item_company_inventory_settings` - NEW

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

Checks: allowed category and serial modes; positive sequence; category code must agree with the Item category at creation and is then immutable. Barcode, sequence, company, and item are immutable after insertion. Changes to serial mode create typed history.

### 3.3 `item_master_change_history` - NEW, APPEND-ONLY

| Column | Definition |
|---|---|
| `Id` | `uuid PK` |
| `ItemId` | `uuid NOT NULL FK items(Id)` |
| `CompanyInventorySettingId` | `uuid NULL FK item_company_inventory_settings(Id)` |
| `ChangeScope` | `varchar(30) NOT NULL`; `SHARED_ITEM` or `COMPANY_SETTING` |
| `ChangedFieldsJson` | `jsonb NOT NULL`, object containing field-level old/new values |
| `Reason` | `varchar(1000) NOT NULL` |
| `ActorEmployeeId` | `uuid NOT NULL FK employees(Id)` |
| `ActorRoleCode` | `varchar(100) NOT NULL` |
| `ChangedAt` | `timestamptz NOT NULL` |
| `CorrelationId` | `varchar(100) NOT NULL` |

Keys/indexes: PK; index `(ItemId, ChangedAt DESC)`; index `(CompanyInventorySettingId, ChangedAt DESC)`; unique `CorrelationId`.

Checks: setting FK is required only for `COMPANY_SETTING`; JSON must be a non-empty object. Trigger rejects update/delete.

### 3.4 `business_rule_configuration_versions` - NEW, APPEND-ONLY

This is the single company-scoped, effective-dated registry required by Section 17.1.

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

### 3.5 `store_category_routes` - NEW

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

### 3.6 `gate_entries` - NEW

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

### 3.7 `gate_entry_lines` - NEW

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

### 3.8 `goods_receipts` - NEW

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
| `Status` | `varchar(20) NOT NULL DEFAULT 'DRAFT'`; `DRAFT` or `FINALIZED` |
| `FinalizedAt` | `timestamptz NULL` |
| `FinalizedByEmployeeId` | `uuid NULL FK employees(Id)` |
| `IdempotencyKey` | `varchar(100) NOT NULL` |
| `RequestFingerprint` | `char(64) NOT NULL` |

Keys/indexes: PK; unique `(CompanyId, GrnNumber)`; unique `(CompanyId, IdempotencyKey)`; unique filtered `ReversesGoodsReceiptId WHERE DocumentKind='REVERSAL' AND Status='FINALIZED'`; index `(CompanyId, PurchaseOrderId, ReceivedAt DESC)`; index `(CompanyId, VendorId, VendorBillNumber, VendorBillDate)`; index `GateEntryId`; alternate keys `(CompanyId, Id)`, `(GateEntryId, Id)`.

Effective-cardinality guard: a deferred constraint trigger permits at most one effective finalised normal GRN per Gate Entry. A corrected replacement is allowed only after a finalised reversal of the prior GRN. Draft duplicates may exist but cannot both finalise.

Other guards: Gate Entry must be finalised/effective and have the same Company, PO and Vendor; bill fields are mandatory; document-kind/reversal fields agree; snapshot hash matches canonical JSON; reversal copies the target Gate/PO/vendor/bill/configuration facts. Finalisation and its QC-hold posting batch are one transaction. Finalised rows reject update/delete.

### 3.9 `goods_receipt_lines` - NEW

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
| `ReceivedQuantity` | `numeric(24,6) NOT NULL`; quantity admitted to QC-hold |
| `ExcessRejectedQuantity` | `numeric(24,6) NOT NULL DEFAULT 0`; delivered excess excluded from stock |
| `ExcessDisposition` | `varchar(40) NULL`; `PENDING_VENDOR_RETURN` when excess is positive |
| `UnitRateSnapshot` | `numeric(24,6) NOT NULL`, PO unit rate used for serial threshold |
| `SerialThresholdConfigVersionId` | `uuid NOT NULL FK business_rule_configuration_versions(Id)` |
| `SerialThresholdValueSnapshot` | `numeric(24,6) NOT NULL` |
| `SerialCaptureModeSnapshot` | `varchar(20) NOT NULL`; resolved `REQUIRED` or `OPTIONAL` |
| `SerialOverrideSettingId` | `uuid NULL FK item_company_inventory_settings(Id)` |
| `QcRouteIdSnapshot` | `uuid NOT NULL FK store_category_routes(Id)` |
| `QcHoldConditionLocationIdSnapshot` | `uuid NOT NULL FK warehouse_condition_locations(Id)` |
| `BillWarrantyLimitDate` | `date NOT NULL`; bill date plus 13 months |
| `InstallationDateAtReceipt` | `date NULL` |
| `InstallationWarrantyLimitDate` | `date NULL`; installation date plus 12 months |
| `WarrantyExpiryAtReceipt` | `date NOT NULL`; earlier limit, or bill limit when installation is absent |
| `CreatedAt`, `CreatedBy` | Immutable audit columns |

Keys/indexes: PK; unique `(GoodsReceiptId, LineNumber)`; unique `(GoodsReceiptId, GateEntryLineId)`; index `(CompanyId, PurchaseOrderLineId)`; index `(CompanyId, ItemId)`; alternate key `(CompanyId, Id)`.

Checks: all quantities nonnegative; received positive; `ReceivedQuantity + ExcessRejectedQuantity = DeliveredQuantitySnapshot`; `ReceivedQuantity <= RemainingPoQuantitySnapshot`; `Remaining = Ordered - PriorReceived`; excess disposition agrees with excess; GST range 0-100; serial mode valid; warranty-date arithmetic consistent.

Finalisation trigger, under a lock covering the PO line, recalculates effective received-to-date as the signed sum of finalised normal/reversal GRN lines. It rejects any result above `OrderedQuantity`; there is no override path or override column. It also requires every Gate line to have exactly one GRN line and vice versa.

### 3.10 `inventory_serials` - NEW, DURABLE IDENTITY

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

### 3.11 `goods_receipt_line_serials` - NEW

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
| `DisambiguationApplied` | `boolean NOT NULL DEFAULT false` |
| `DuplicateWarningAcknowledged` | `boolean NOT NULL DEFAULT false` |
| `DisambiguationReason` | `varchar(500) NULL` |
| `CapturedByEmployeeId` | `uuid NOT NULL FK employees(Id)` |
| `CapturedAt` | `timestamptz NOT NULL` |

Keys/indexes: PK; unique `(GoodsReceiptLineId, SerialOrdinal)`; unique `(GoodsReceiptLineId, InventorySerialId)`; index `(CompanyId, ItemId, StoredSerialNumberSnapshot)`; index `InventorySerialId`; alternate key `(CompanyId, Id)`.

Checks/guards: positive ordinal; nonblank values; disambiguation fields agree; Company and Item equal both parent and durable serial identity; stored snapshot equals the identity. Before finalisation, the service warns if entered value already exists. A genuine new duplicate must be disambiguated before its durable identity can be inserted. A correction of a reversed receipt reuses the existing identity. When serial capture is required, finalisation requires an integral received quantity and exactly that many serial rows. Capture rows become immutable with the GRN.

### 3.12 `stores_document_status_history` - NEW, APPEND-ONLY

Typed status history shared by Gate Entry, GRN, and QC revision without text-only document references.

| Column | Definition |
|---|---|
| `Id` | `uuid PK` |
| `CompanyId` | `uuid NOT NULL` |
| `GateEntryId` | `uuid NULL FK gate_entries(Id)` |
| `GoodsReceiptId` | `uuid NULL FK goods_receipts(Id)` |
| `QcInspectionRevisionId` | `uuid NULL FK qc_inspection_revisions(Id)` |
| `FromStatus` | `varchar(30) NULL` |
| `ToStatus` | `varchar(30) NOT NULL` |
| `Action` | `varchar(30) NOT NULL`; `CREATED` or `FINALIZED` |
| `ActorEmployeeId` | `uuid NOT NULL FK employees(Id)` |
| `ActorRoleCode` | `varchar(100) NOT NULL` |
| `Reason` | `varchar(1000) NULL` |
| `OccurredAt` | `timestamptz NOT NULL` |
| `CorrelationId` | `varchar(100) NOT NULL` |

Keys/indexes: PK; unique `CorrelationId`; indexes on each source FK plus `OccurredAt`.

Checks: exactly one source FK is non-null; legal status/action pair; Company matches source. Trigger rejects update/delete and illegal transition insertion.

### 3.13 `qc_inspections` - NEW, LOGICAL IDENTITY

| Column | Definition |
|---|---|
| `Id` | `uuid PK` |
| `CompanyId` | `uuid NOT NULL` |
| `InspectionNumber` | `varchar(50) NOT NULL` |
| `GoodsReceiptLineId` | `uuid NOT NULL FK goods_receipt_lines(Id)` |
| `CreatedAt`, `CreatedBy` | Immutable identity audit |

Keys/indexes: PK; unique `(CompanyId, InspectionNumber)`; unique `GoodsReceiptLineId`; alternate key `(CompanyId, Id)`.

The row itself is immutable. It guarantees one logical inspection per GRN line. Corrections are revisions, not additional logical inspections.

### 3.14 `qc_inspection_revisions` - NEW

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

Checks/guards: revision chain is same logical inspection and sequential; correction fields agree; fallback requires reason and inspector equal the originating PR raiser; `0 <= Inspected <= GRN Received`; `ShortfallRejected = GRN Received - Inspected`; `Accepted + Rejected = GRN Received`; `Rejected >= ShortfallRejected`; decision agrees with quantities; accepted destination is required iff accepted is positive and must be an active same-company `AVAILABLE` mapping; pending-return mapping is required for rejected quantity and remains the same physical QC rack. Finalisation requires the effective GRN, all policy results, and serial dispositions. QC finalisation and all stock posting legs are atomic. A finalised revision is immutable.

### 3.15 `qc_inspection_parameter_results` - NEW

One immutable result summary is derived from each effective policy row. Item- and category-owned policies are not collapsed; each source policy remains independently traceable.

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
| `Result` | `varchar(20) NOT NULL`; `PASS` or `FAIL` |
| `Remarks` | `varchar(1000) NULL` |
| `CreatedAt`, `CreatedBy` | Immutable audit columns |

Keys/indexes: PK; unique `(QcInspectionRevisionId, QcInspectionPolicyId)`; index `(CompanyId, ParameterCodeSnapshot, Result)`; alternate key `(CompanyId, Id)`.

Checks: valid limits/sample/result; policy is approved/effective for the GRN Item or category at `PolicyResolvedAt`; snapshots equal that policy at creation. Rows are mutable only while the parent revision is draft.

### 3.16 `qc_inspection_parameter_observations` - NEW

| Column | Definition |
|---|---|
| `Id` | `uuid PK` |
| `CompanyId` | `uuid NOT NULL` |
| `QcInspectionParameterResultId` | `uuid NOT NULL FK qc_inspection_parameter_results(Id)` |
| `SampleOrdinal` | `integer NOT NULL` |
| `ObservedNumericValue` | `numeric(24,6) NULL` |
| `ObservedTextValue` | `varchar(500) NULL` |
| `Result` | `varchar(20) NOT NULL`; `PASS` or `FAIL` |
| `Remarks` | `varchar(1000) NULL` |
| `ObservedAt` | `timestamptz NOT NULL` |
| `ObservedByEmployeeId` | `uuid NOT NULL FK employees(Id)` |

Keys/indexes: PK; unique `(QcInspectionParameterResultId, SampleOrdinal)`; index `(CompanyId, Result)`.

Checks: positive ordinal; exactly one observed-value column is non-null. Finalisation requires observation count equal the result's required sample size and the summary result to equal the aggregate observation result. Rows are immutable after parent finalisation.

### 3.17 `qc_inspection_serial_dispositions` - NEW

This table makes serial-level stock location and condition deterministic while the QC decision remains one record per GRN line.

| Column | Definition |
|---|---|
| `Id` | `uuid PK` |
| `CompanyId` | `uuid NOT NULL` |
| `QcInspectionRevisionId` | `uuid NOT NULL FK qc_inspection_revisions(Id)` |
| `GoodsReceiptLineSerialId` | `uuid NOT NULL FK goods_receipt_line_serials(Id)` |
| `Disposition` | `varchar(20) NOT NULL`; `ACCEPTED` or `REJECTED` |
| `Reason` | `varchar(1000) NULL` |
| `CreatedAt`, `CreatedBy` | Immutable audit columns |

Keys/indexes: PK; unique `(QcInspectionRevisionId, GoodsReceiptLineSerialId)`; index `(CompanyId, GoodsReceiptLineSerialId)`.

Checks/guards: serial belongs to the inspected GRN line; every serial on a serialised line has exactly one disposition; accepted/rejected serial counts equal the revision quantities. Rows are immutable after finalisation.

### 3.18 `stock_posting_batches` - NEW, APPEND-ONLY

One business command creates a batch containing all balanced ledger legs.

| Column | Definition |
|---|---|
| `Id` | `uuid PK` |
| `CompanyId` | `uuid NOT NULL` |
| `PostingKind` | `varchar(30) NOT NULL`; `GRN_TO_QC_HOLD`, `QC_DISPOSITION`, or `REVERSAL` |
| `GoodsReceiptId` | `uuid NULL FK goods_receipts(Id)` |
| `QcInspectionRevisionId` | `uuid NULL FK qc_inspection_revisions(Id)` |
| `ReversesPostingBatchId` | `uuid NULL FK stock_posting_batches(Id)` |
| `ReferenceType` | `varchar(40) NOT NULL`, descriptive snapshot |
| `ReferenceNumber` | `varchar(120) NOT NULL`, descriptive snapshot |
| `PostingDate` | `date NOT NULL` |
| `PostedAt` | `timestamptz NOT NULL` |
| `PostedByEmployeeId` | `uuid NOT NULL FK employees(Id)` |
| `IdempotencyKey` | `varchar(100) NOT NULL` |
| `RequestFingerprint` | `char(64) NOT NULL` |
| `CorrelationId` | `varchar(100) NOT NULL` |

Keys/indexes: PK; unique `(CompanyId, IdempotencyKey)`; unique `CorrelationId`; unique filtered `ReversesPostingBatchId WHERE PostingKind='REVERSAL'`; indexes on each source FK and `(CompanyId, PostingDate)`; alternate key `(CompanyId, Id)`.

Checks/guards: exactly one direct source FK for non-reversal batches; reversal target required only for `REVERSAL`; source and reversal company match; reference values must be derived from the source document. Trigger rejects update/delete. A reused idempotency key with identical fingerprint replays; a different fingerprint conflicts.

### 3.19 `stores_command_receipts` - NEW, APPEND-ONLY

This applies the architecture-reference idempotency pattern to retryable document commands, independent of stock-posting idempotency.

| Column | Definition |
|---|---|
| `Id` | `uuid PK` |
| `CompanyId` | `uuid NOT NULL` |
| `CommandScope` | `varchar(80) NOT NULL` |
| `IdempotencyKey` | `varchar(100) NOT NULL` |
| `RequestFingerprint` | `char(64) NOT NULL` |
| `ResponseStatusCode` | `integer NOT NULL` |
| `ResponseJson` | `jsonb NOT NULL` |
| `CreatedAt` | `timestamptz NOT NULL` |
| `ExpiresAt` | `timestamptz NULL` |

Keys/indexes: PK; unique `(CompanyId, CommandScope, IdempotencyKey)`; index `ExpiresAt`.

Checks: valid HTTP status and JSON object. Inserted in the same transaction as the business effect; trigger rejects update/delete. Retention is operational policy, not a delete permission for the runtime principal.

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
| Location nullability | Make `WarehouseId` and `RackBinId` non-null for all first-module movements. |
| `WarehouseConditionLocationId` | Add `uuid NOT NULL FK warehouse_condition_locations(Id)`; proves warehouse, rack and condition. |
| `ConditionCode` | Add `varchar(30) NOT NULL`; immutable snapshot of the condition mapping. |
| `StockPostingBatchId` | Add `uuid NOT NULL FK stock_posting_batches(Id)`. |
| `BatchLineOrdinal` | Add `integer NOT NULL`. |
| `MovementLeg` | Add `varchar(30) NOT NULL`: `RECEIPT_IN`, `TRANSFER_OUT`, `TRANSFER_IN`, or `REVERSAL`. |
| `GoodsReceiptLineId` | Add nullable typed FK to `goods_receipt_lines(Id)`. |
| `QcInspectionRevisionId` | Add nullable typed FK to `qc_inspection_revisions(Id)`. |
| `InventorySerialId` | Add nullable FK to `inventory_serials(Id)`; null for non-serial aggregate movement. No batch/lot column is added. |
| `ReversesStockMovementId` | Add nullable self-FK; required for reversal rows. |
| `PostingIdentity` | Add `varchar(200) NOT NULL`, deterministic identity of source, serial/aggregate and leg. |
| Audit mutability | `UpdatedAt`, `UpdatedBy`, and `Version` remain physically present for compatibility but are never changed after insert. |

New keys/indexes:

- Unique `(StockPostingBatchId, BatchLineOrdinal)`.
- Unique `(CompanyId, PostingIdentity)`.
- Unique filtered `ReversesStockMovementId WHERE ReversesStockMovementId IS NOT NULL`.
- Index `(CompanyId, ItemId, WarehouseConditionLocationId, PostingDate, Id)` for ledger/balance queries.
- Index `(CompanyId, InventorySerialId, PostingDate, Id)` where serial is non-null.
- Indexes on `GoodsReceiptLineId`, `QcInspectionRevisionId`, and `StockPostingBatchId`.
- Composite tenant/location FKs enforce the same Company, Warehouse, Rack and mapping.

New checks/triggers:

- Exactly one of `QuantityIn` and `QuantityOut` is greater than zero; the other is zero.
- Current module rows have exactly one source FK: GRN receipt rows use `GoodsReceiptLineId`; QC disposition rows use `QcInspectionRevisionId`. The serial FK supplements, never replaces, that source.
- Reversal rows copy the original source FK, Item, serial, location and condition, swap quantity in/out exactly, and identify the original movement.
- Serialized movement quantity is exactly 1. Non-serialized movement has no serial FK.
- `ReferenceType` and `ReferenceNumber` remain readable audit/display snapshots. They are derived and verified from the typed FK; they are not the integrity link.
- Trigger rejects every `UPDATE` and `DELETE`. Corrections insert a reversal batch and new movements.
- Batch insert is rejected unless its legs reconcile: a GRN batch contains QC-hold inbound legs; a QC batch has equal out/in quantities for each Item/serial; a reversal exactly negates its target batch.

No over-receipt override field is added to GRN lines or movements. Source-less adjustment support is deliberately not enabled in this module. The future adjustment module must add its typed override evidence and relax the exactly-one-source rule only for `ADJUSTMENT`; no free-text or source-less movement can be posted now.

### 4.2 Posting examples

- GRN finalisation: one `RECEIPT_IN` into the category `QC_HOLD` location per non-serial line, or one quantity-1 row per serial. Source FK is the GRN line.
- QC accepted quantity: `TRANSFER_OUT` from `QC_HOLD` plus `TRANSFER_IN` to the Stores-selected `AVAILABLE` location. Both use the same QC revision source and posting batch.
- QC rejected quantity: `TRANSFER_OUT` from `QC_HOLD` plus `TRANSFER_IN` to `PENDING_RETURNABLE_DC` at the same warehouse/rack. It is physically retained but cannot be reserved or issued.
- Correction: a `REVERSAL` batch negates every row of the erroneous finalised posting, followed by the replacement document/revision and its new batch.

Stock balance is a query/view, not a mutable balance table: sum `QuantityIn - QuantityOut` by Company, Item, condition location and optional serial. Received-to-date is likewise a query: signed effective GRN-line quantity by PO line.

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

The GRN finalisation transaction locks affected PO lines, recomputes received-to-date, rejects over-receipt, validates all Gate lines and required serials, finalises the document, appends status history, creates its posting batch/movements, audit event, and command receipt, then commits. Failure rolls back everything.

### 5.3 QC inspection

One immutable `qc_inspections` identity exists per GRN line. Each attempt/correction is a revision.

| State | Meaning | Allowed transition |
|---|---|---|
| `REVISION_DRAFT` | Quantities, policy results, observations, serial dispositions and accepted destination may be edited. | `FINALIZE_AND_POST` -> `REVISION_FINALIZED_STOCK_POSTED`. |
| `REVISION_FINALIZED_STOCK_POSTED` | Immutable revision; accepted stock is `AVAILABLE`, rejected stock is `PENDING_RETURNABLE_DC`. | Correction command creates a new `CORRECTION` draft after atomically reversing the prior revision's posting. |
| `REVISION_SUPERSEDED` | Derived for an older revision when a later correction revision finalises. | Terminal; the row and its evidence remain unchanged. |

There is no QC bypass. QC finalisation and ledger posting are one transaction, so no persisted “final but unposted” state exists.

### 5.4 Physical/ledger condition path

`VENDOR/OUTSIDE -> QC_HOLD -> AVAILABLE` for accepted quantity.

`VENDOR/OUTSIDE -> QC_HOLD -> PENDING_RETURNABLE_DC` for rejected quantity.

`PENDING_RETURNABLE_DC` is terminal within this module. The later DC module owns the next transition. Delivered excess never enters the stock ledger; it is recorded as Gate/GRN excess evidence pending vendor return.

Strict-sequence guards reject:

- GRN creation/finalisation without an effective finalised Gate Entry.
- QC creation/finalisation without an effective finalised GRN and QC-hold posting.
- available or pending-return posting without a finalising QC revision.
- direct stock-movement writes without a typed source and authorised posting batch.
- reversal of an upstream document while an effective downstream document/posting remains.

## 6. Configuration and immutable snapshots

Editable values live in `business_rule_configuration_versions`, one effective append-only version per Company and RuleKey. Only `TECHNICAL_DIRECTOR`, `MANAGING_DIRECTOR`, and `IT_MANAGER` may append a version. Every row records actor, role, time, old value, new value, reason, previous version and effective period.

For this module the relevant value is `SERIAL_CAPTURE_THRESHOLD`, initially 5,000. At **GRN draft creation**, the service:

1. resolves the effective company configuration row;
2. reads the company-specific Item `SerialCaptureMode`;
3. stores the complete canonical configuration object plus version IDs in `goods_receipts.ConfigurationSnapshotJson`;
4. stores a SHA-256 identity in `ConfigurationSnapshotHash`; and
5. writes the resolved threshold, configuration-version FK, override-setting FK and final `REQUIRED/OPTIONAL` decision on every GRN line.

Every later validation uses those snapshots. Configuration changes therefore affect only GRNs created after the new version becomes effective. A draft created earlier keeps its original values; submission/finalisation does not re-resolve them.

QC policy is effective-dated operational master data rather than a Section 17.1 monetary configuration value. Each QC draft resolves the effective policy rows once and snapshots every policy field into `qc_inspection_parameter_results`. Later policy edits do not change that revision. A correction revision resolves policies afresh at its own creation time and preserves both sets of evidence.

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
| `POST /goods-receipts/{id}/finalize` | Lock PO lines, block over-receipt, validate bill/serials, freeze GRN and post QC-hold ledger atomically. |
| `POST /goods-receipts/{id}/reversals` | Reverse an eligible GRN and its posting batch; no edit to original. |
| `GET /purchase-orders/{poId}/receipt-position` | Computed ordered, effective received, Gate-delivered, excess and remaining quantity by PO line. |

### Item ERP barcode

| Method and route | Purpose |
|---|---|
| `GET /items/{itemId}/inventory-setting` | Current-company ERP barcode and serial mode for the shared Item. |
| `POST /items/{itemId}/erp-barcode` | Allocate the company sequence and create the immutable ERP barcode. |
| `PUT /items/{itemId}/serial-capture-mode` | Change company Item override with reason and typed history. |
| `POST /items/{itemId}/barcode-label` | Produce the printable label artifact from the stored ERP barcode. |
| `GET /items/{itemId}/change-history` | Shared Item and company-setting change evidence visible in current scope. |

### QC and category routes

| Method and route | Purpose |
|---|---|
| `GET /qc/queue` | Effective finalised GRN lines awaiting QC, including assigned QC rack and inspector basis. |
| `GET /qc/inspections/{id}` | Logical inspection with all immutable revisions, parameters, observations, serial dispositions and posting links. |
| `POST /goods-receipt-lines/{lineId}/qc-inspection` | Create the one logical inspection and initial draft revision. |
| `PUT /qc/revisions/{revisionId}` | Edit a draft revision, parameter observations and serial dispositions. |
| `POST /qc/revisions/{revisionId}/finalize-and-post` | Validate reconciliation/evidence, freeze revision and atomically post accepted/pending-return movements. |
| `POST /qc/inspections/{id}/corrections` | Reverse the effective QC posting and create the next correction draft with mandatory reason. |
| `GET /category-routes` | Read effective current-company QC/default-accepted mappings. |
| `POST /category-routes` | Create a future/effective route under authorised master administration. |
| `POST /category-routes/{id}/close` | Effective-date close a route; never rewrites document snapshots. |

### Stock ledger and configuration

| Method and route | Purpose |
|---|---|
| `GET /stock/ledger` | Filtered append-only ledger by date, Item, condition, warehouse/rack, source or serial. |
| `GET /stock/balances` | Computed balances; only `AVAILABLE` is reservable/issuable. |
| `GET /stock/posting-batches/{id}` | Batch, typed source, all legs, idempotency and reversal chain. |
| `GET /stock/serials/{serial}` | Company-scoped serial provenance and current condition/location. |
| `GET /configuration/{ruleKey}` | Effective value and version for current company. |
| `GET /configuration/{ruleKey}/history` | Immutable version/change history. |
| `POST /configuration/{ruleKey}/versions` | Append a version; TD, MD or IT Manager only, with reason. |

There is intentionally no public `POST/PUT/DELETE /stock-movements` endpoint and no adjustment endpoint.

## 8. Practicality, contradictions, and risks

1. **QC custody versus “stock.”** A GRN must place material in the category QC rack, so the ledger records company custody at `QC_HOLD` before inspection. “Nothing reaches stock without QC” must mean nothing becomes **available stock** without QC. Interpreted literally as no ledger row before QC, the QC-rack quantity would be untracked and the stated flow would be contradictory.

2. **Shared Item versus per-company barcode.** The existing `items.Barcode` cannot correctly hold two company-scoped barcodes for a shared Item. The company child table is necessary. The legacy Item Barcode must not be treated as the new ERP barcode or used for company-specific uniqueness.

3. **Immutable GRN versus later installation date.** At receipt, installation normally has not occurred. The GRN can immutably store the bill-based limit and warranty expiry known at receipt. If a later installation makes the 12-month limit earlier, the future Installation/installed-machine module must hold the new effective warranty fact or an append-only warranty event; it cannot update the finalised GRN line. Treating the GRN column as permanently authoritative after installation would contradict both the warranty formula and GRN immutability.

4. **Mandatory vendor bill duplicates future invoice intake.** Capturing bill identity/date on the GRN is required and workable, but accounting invoice lines, tax matching, credits and payment status remain in the future Vendor Invoice module. That module must reference this GRN bill snapshot instead of silently creating a second inconsistent bill identity.

5. **Excess custody gap.** Delivered excess is recorded but deliberately never enters stock. Until the DC/return workflow exists, the ERP has evidence but no stock location or dispatch document for the physical excess. Operations must segregate it outside controlled stock and return it manually; otherwise physical custody and ERP stock will diverge.

6. **ISO verification vocabulary is not enumerated.** `IsoReceiptVerificationJson` preserves immutable evidence without inventing a fixed field catalogue, but validation can initially guarantee only a JSON object and a module-owned JSON schema version. Exact ISO fields, attachments and signatures remain a later hardening decision.

7. **Number formats remain deferred.** The schema guarantees scoped uniqueness and safe allocation, but it does not invent Gate/GRN/QC display formats. Those formats must be configured before implementation; changing presentation must not alter stored issued numbers.

8. **PostgreSQL constraints are essential.** Effective-cardinality, PO received-to-date, parent-state, snapshot reconciliation, balanced transfer, and reversal equivalence cannot be expressed by ordinary row CHECK constraints. They require serializable service transactions plus narrow deferred constraint triggers/functions, following the architecture reference. Application-only enforcement would be unsafe.

9. **Existing quantity precision is too narrow.** `numeric(18,3)` is inconsistent with existing UOM precision foundations and weight/length materials. Widening the ledger to six decimal places is necessary; all source quantities must use the same precision to prevent reconciliation drift.

10. **QC policy evidence is implementable but extended evidence remains deferred.** The normalised policy/result/observation tables support the first module. Attachments, signatures, FAT sheets, concessions and reinspection are intentionally absent and must not be simulated with ungoverned columns.

No other contradiction blocks the schema design.

RESULT_REPORTED_PENDING_WITNESS
