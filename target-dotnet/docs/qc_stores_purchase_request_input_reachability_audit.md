# QC, Stores, and Purchase request-input reachability audit

## Rule and scope

Audit base: 9792ae4.

The audit covers every request record used by the QC, Stores, Purchase Requisition, and REV869B Purchase HTTP surfaces. A request is source-complete when every mandatory server-derived selector, document identity, child identity, and concurrency version is returned by a read endpoint available to the role allowed to invoke the command.

Operator-authored facts such as remarks, reasons, measurements, dates, quantities, commercial terms, received document values, and ISO evidence are not server-derived selectors. Idempotency keys are client-generated. Fixed enums are contract values. These fields remain mandatory and validated, but do not require a database read model.

## Result

- 42 request records audited: 8 QC, 11 Stores, 6 Purchase Requisition, and 17 REV869B Purchase.
- The two reported QC gaps are fixed. All eight QC request records are now source-complete without granting QC_MANAGER access to inventory.grn.
- Purchase Requisition is source-complete for all six request records.
- The one Stores and five REV869B Purchase read-model gaps are fixed with command-permission-compatible projections.
- Material follow-up Update now matches its existing service role guard: STORES_MANAGER and STORES_EXECUTIVE only.
- The seed migration expects exactly two updated permission rows in Up and two in Down, with zero inserts and zero deletes.

## QC

| Request record | Mandatory server-derived inputs | Readable source | Result |
| --- | --- | --- | --- |
| QcParameterResultRequest | QcInspectionPolicyId | GET /api/v1/rev869a/configuration/qc-inspection-policies, readable through qc.inspection-policies View | Pass |
| QcSerialDispositionRequest | InventorySerialId | GET /api/v1/qc/queue now returns InventorySerialIds through qc.inspection-policies View | Fixed |
| FinalizeQcInspectionRequest | GoodsReceiptLineLotAllocationId, policy IDs, serial IDs, accepted condition location when accepted quantity is positive | QC queue; QC policy list; warehouse-condition-location list. QC_MANAGER can read all three | Pass after fix |
| CorrectQcInspectionRequest | inspection number, RevisesRevisionId, policy IDs, serial IDs, accepted condition location when needed | GET QC inspection; QC policy list; warehouse-condition-location list | Pass |
| CreateInventoryConcessionRequest | QcInspectionLotDispositionId, FailedParameterResultId, rejected quantity and serial IDs | GET QC inspection now returns QcInspectionLotDispositionId and already returns parameter-result IDs and serial dispositions | Fixed |
| ApproveInventoryConcessionRequest | Version and AvailableConditionLocationId | GET concession and warehouse-condition-location list; TECHNICAL_DIRECTOR has both View grants | Pass |
| RejectInventoryConcessionRequest | Version | GET concession | Pass |
| ReverseInventoryConcessionRequest | Version | GET concession | Pass |

## Stores

| Request record | Mandatory server-derived inputs | Readable source | Result |
| --- | --- | --- | --- |
| GateEntryLineRequest | PurchaseOrderLineId and deliverable quantity context | GET /api/v1/stores/gate-entries/purchase-order-candidates under inventory.grn Create | Fixed |
| CreateGateEntryRequest | PurchaseOrderNumber and line IDs | Scoped issued-PO candidates under the command permission | Fixed |
| UpdateGateEntryRequest | Gate Entry ID, line IDs, Version | Gate Entry list/detail on inventory.grn | Pass |
| FinalizeGateEntryRequest | Gate Entry ID and Version | Gate Entry list/detail | Pass |
| GoodsReceiptLotRequest | no server-derived identity; lot ordinal and captured lot facts are operator-authored | Gate Entry quantity context plus receiving evidence | Pass |
| GoodsReceiptSerialRequest | LotOrdinal; serial values are captured facts | Current GRN draft for update; request lot rows for create | Pass |
| GoodsReceiptLineRequest | GateEntryLineId | Gate Entry detail on the same inventory.grn page | Pass |
| CreateGoodsReceiptRequest | GateEntryNumber and GateEntryLineId | Gate Entry list/detail on inventory.grn | Pass |
| UpdateGoodsReceiptRequest | GRN ID, existing line context, Version | Goods Receipt list/detail on inventory.grn | Pass |
| FinalizeGoodsReceiptRequest | GRN ID and Version | Goods Receipt list/detail | Pass |
| ReverseGoodsReceiptRequest | GRN ID and Version | Goods Receipt list/detail | Pass |

The Stores correction does not grant STORES_ASSISTANT purchase.po visibility. It exposes only current issued POs accepted by Gate Entry creation, preserves employee receipt-operator and record-scope checks, and returns the PO/line identifiers needed by the request.

## Purchase Requisition

| Request record | Mandatory server-derived inputs | Readable source | Result |
| --- | --- | --- | --- |
| PurchaseRequisitionLineRequest | ItemCode and optional warehouse code | Create-protected item and warehouse lookups | Pass |
| CreatePurchaseRequisitionRequest | OrganizationId, RequesterEmployeeCode, department code, warehouse code, item codes | GET /api/v1/session/me plus the three create-protected requisition lookups | Pass |
| UpdatePurchaseRequisitionRequest | PR number, current values, line numbers, Version | PR list/detail | Pass |
| PurchaseRequisitionActionRequest | PR number and Version | PR detail; earlier approval-chain visibility correction supplies reader access to action holders | Pass |
| StockCheckLocationRequest | PR line number, warehouse code, optional rack-bin code | PR detail, warehouse context, and rack-bin lookup; existing stock-check corrections provide access | Pass |
| StockCheckRequest | PR number and Version | PR detail, readable by the stock-check actor after the existing reachability correction | Pass |

## REV869B Purchase

| Request record | Mandatory server-derived inputs | Readable source | Result |
| --- | --- | --- | --- |
| Rev869BRfqSourceLineRequest | PurchaseRequirementHandoffId and available quantity | GET requisition handoffs, readable by PURCHASE_EXECUTIVE | Pass |
| Rev869BCreateRfqRequest | handoff IDs | Requisition handoff list | Pass |
| Rev869BInviteVendorRequest | VendorId and RfqVersion | GET RFQ vendor candidates under purchase.rfq Submit; RFQ detail/list returns Version | Fixed |
| Rev869BQuotationLineRequest | RequestForQuotationLineId and RFQ quantity context | RFQ detail returns line IDs and quantities | Pass |
| Rev869BSubmitQuotationRequest | invitation path ID, InvitationVersion, RFQ line IDs, previous quotation version when revising | GET RFQ invitations under vendor-quotations Create returns invitation ID/version, current quotation version, and RFQ line IDs | Fixed |
| Rev869BTechnicalVerificationRequest | VendorQuotationLineId and QuotationVersion | Quotation detail | Pass |
| Rev869BCreateComparisonRequest | RfqNumber and RfqVersion | GET comparison RFQ candidates under commercial-comparisons Create returns scoped RFQs with current technically compliant same-currency quotations | Fixed |
| Rev869BRecommendComparisonRequest | VendorQuotationId and comparison Version | Comparison detail now returns VendorQuotationId on every line in commercial and masked output | Fixed |
| Rev869BApprovalActionRequest | comparison Version | Comparison detail, readable by approval actors | Pass |
| Rev869BPoApprovalActionRequest | PO Version | PO detail, readable by approval actors | Pass |
| Rev869BCreatePurchaseOrderRequest | ComparisonNumber and ComparisonVersion | Comparison list/detail | Pass |
| Rev869BSubmitPurchaseOrderRequest | PO Version | PO detail | Pass |
| Rev869BIssuePurchaseOrderRequest | PO Version | PO detail | Pass |
| Rev869BAmendPurchaseOrderRequest | PO Version and existing commercial terms for editing | PO detail with commercial visibility for PURCHASE_MANAGER | Pass |
| Rev869BReviseRejectedPurchaseOrderRequest | rejected PO Version and existing terms | Current rejected PO detail | Pass |
| Rev869BCancelPurchaseOrderRequest | PO Version | PO detail | Pass |
| Rev869BMaterialFollowUpTransitionRequest | handoff path ID, current status, Version | Material follow-up list returns ID, status, and Version; STORES_MANAGER and STORES_EXECUTIVE hold Update | Fixed |

All Purchase corrections are narrow command-support read models. No master, RFQ, quotation, PO, GRN, or QC read permission was widened to compensate for a missing field.

## Correction verification

- PostgreSQL: the full migration chain applied and reverted on a fresh disposable PostgreSQL cluster, exercising the cluster guard in both directions. The owner database was not used.
- Expected migration rows: Up updates 2 existing role-page-permission rows; Down updates the same 2 rows. Both directions insert 0 and delete 0 rows.
- Debug: 779 tests total = 721 safe tests passed + 58 explicit opt-in tests not run.
- Release: 777 tests total = 719 safe tests passed + 58 explicit opt-in tests not run.
