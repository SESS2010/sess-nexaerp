# MIR concurrency, read-model and post-audit reachability report

Audit base: `d794006`. Scope: all mutable workflow transitions plus all request/read contracts added after the earlier 42-contract QC/Stores/Purchase audit. No owner database was accessed.

## 1. Workflow Version audit

A mutable aggregate must compare the caller's Version and advance that same token in the successful transaction. Append-only evidence workflows are marked separately because they create a new row and never mutate the prior evidence row.

| Workflow | Transitions checked | Result |
| --- | --- | --- |
| Purchase Requisition | submit, department verify, approve steps, reject, request revision, resubmit, cancel, hold | Pass: every status change increments `PurchaseRequisition.Version`. |
| RFQ and invitation | issue RFQ, reserve/invite vendor, invitation submitted | Pass: CAS updates set Version + 1. |
| Vendor quotation | submit/revise/supersede, technical verify/reject | Pass: current and superseded rows use CAS Version + 1. |
| Commercial comparison | recommend, submit/resubmit, approve steps, reject/revise | Pass: every live status mutation uses CAS Version + 1. |
| Purchase Order | submit/resubmit, approval steps, reject, issue, amend/reserve, supersede, cancel | Pass: every live status mutation uses CAS Version + 1; new revisions are new rows. |
| Material follow-up | pending -> in progress -> complete | Pass: CAS Version + 1. |
| Gate Entry | draft edit, finalize | Pass: edit and finalize both set Version + 1. |
| GRN | draft edit, finalize | Pass: edit increments and controlled finalize sets Version + 1. Reversal is compensating append-only evidence and does not rewrite the finalized original. |
| QC inspection/disposition | finalize and correct | Pass by design: finalized inspection revisions and lot dispositions are append-only; correction creates a new revision linked by `RevisesRevisionId`. |
| QC concession | approve, reject | Pass: Version + 1. Reversal creates a new compensating concession and leaves the accepted original immutable. |
| MIR | draft edit; submit, approve, reject, cancel; issue-driven partial/full fulfilment | **Fixed:** all now advance `MaterialIssueRequest.Version`. Previously edit and all transition paths left it unchanged. |
| Material Return | accept and resulting Issue status | Pass: both Return and Issue versions advance. |
| Vendor Bill | accept, reject, reverse | Pass: SECURITY DEFINER functions lock/check the row and set Version + 1. |
| Job Order | Accounts confirmation, Production BOM pin, FAT reconciliation | Pass after audit correction: confirmation and FAT already advanced; pin now advances Job Order Version. |
| Estimated BOM | draft edit, submit, approve, new revision | **Fixed:** revision and affected header versions now advance. |
| Production BOM | draft edit, submit, approve, new revision, pin | **Fixed:** revision/header tokens and the pinned Job Order token now advance. |
| Engineering document | submit, approve, supersede prior revision, new revision | **Fixed:** revision/document tokens advance; a superseded prior revision also advances. |
| Item/master lifecycle | draft edit, submit/approve/reject/revision/hold/reactivate/deactivate; item merge | **Fixed:** shared lifecycle, custom Item approval, Item edit and merge now advance Version. Other master update services already did. |
| Tax/GST | approve, reject | Pass: Version + 1. |
| Master-data import batch | complete, reject, fail | Pass: Version + 1. |
| Component fitment | confirm, reverse, re-verify | Pass by design: confirmation and reversal are immutable append-only evidence; no existing fitment row changes state. |

The unused private `Transition(CommercialComparison,...)` helper was removed so it cannot become a future non-versioning path; all live comparison transitions use CAS updates.

## 2. MIR readable projection

`MaterialIssueRequestView` now returns `DepartmentCode`, `EmployeeCode`, and `EmployeeName` next to `RequestingDepartmentId` and `RequestedByEmployeeId`. Both list and detail use the same mapper.

## 3. Bare foreign-key read-contract audit

Self IDs and IDs accompanied by a code/number/name in the same projection pass. The following unresolved bare identifiers remain. Assignment IDs, posting-batch IDs, provenance IDs and revision-link IDs are also listed where the API exposes no resolver; they are audit identifiers rather than dropdown labels, but the UI must not render the GUID as a human label.

| Surface / read contract | Bare identifiers |
| --- | --- |
| AuditLogSummary | ResolvedRoleAssignmentId |
| EmployeeRoleAssignmentEventSummary | AssignmentId, ActorEmployeeId |
| CustomerCompanyRelationshipDetail | CompanyId, CustomerId, PaymentTermId, ApprovedByEmployeeId |
| VendorCompanyRelationshipDetail | CompanyId, VendorId, PaymentTermId, ApprovedByEmployeeId |
| TaxGstWorkflowResult / TaxGstSettingSummary | CreatorEmployeeId, DecisionEmployeeId; SupersedesTaxGstSettingId is an unresolved revision link |
| VendorQualificationSummary | VerifiedByEmployeeId, ApprovedByEmployeeId |
| GateEntryHistoryResult / GoodsReceiptHistoryResult | ActorEmployeeId |
| GoodsReceiptLineResult | QcHoldConditionLocationId |
| GoodsReceiptResult | ReversesGoodsReceiptId and StockPostingBatchId |
| QcInspectionResult | InspectorEmployeeId and StockPostingBatchId |
| InventoryConcessionResult | CreatedByEmployeeId, DecidedByEmployeeId, StockPostingBatchId, AvailableProvenanceLayerId |
| MaterialFollowUpListItem | PurchaseOrderId and PurchaseOrderLineId (no PO number or item/line label) |
| EstimatedBomRevisionView / EstimatedBomHistoryView | PreparedByEmployeeId, ApprovedByEmployeeId, ActorEmployeeId, ResolvedRoleAssignmentId |
| ProductionBomRevisionView | SourceEstimatedBomRevisionId, SupersedesRevisionId, PreparedByEmployeeId, ApprovedByEmployeeId |
| EngineeringDocumentRevisionView | SupersedesRevisionId, DrawnByEmployeeId, CheckedByEmployeeId, ApprovedByEmployeeId |
| JobOrderView / JobOrderHistoryView | InitiatedByEmployeeId, AccountsConfirmedByEmployeeId, FatReconciledByEmployeeId, ActorEmployeeId, role-assignment IDs, LatestFatReconciliationId |
| VendorBillView / VendorBillLineView | GoodsReceiptId, PurchaseOrderId, VendorId, DecidedByEmployeeId, ReversedByEmployeeId, GoodsReceiptLineId, PurchaseOrderLineId, ItemId, ResolvedRoleAssignmentId |
| ComponentFitmentSummary | MaterialIssueLineId, ConfirmedByEmployeeId, ResolvedRoleAssignmentId, ReverifiesFitmentId |
| FatCustodyExplanationView / FatReconciliationView | ExplainedByEmployeeId, ReconciledByEmployeeId, ResolvedRoleAssignmentId |
| MaterialIssueRequestView | JobOrderId, CustomerId, VendorId, DestinationDepartmentId; requester and requesting department are now fixed |
| MaterialIssueRequestLineView | CustomerPurchaseOrderLineId |
| MaterialIssueView / MaterialIssueLineView | JobOrderId, IssuedToEmployeeId, ItemId and custody/provenance/location IDs |
| MaterialReturnView / MaterialReturnLineView | ReturnedByEmployeeId, AcceptedByEmployeeId, ItemId, posting/assignment IDs |

Already-humanized examples that pass include Item/category/UOM, Rack Bin/warehouse, Gate Entry/PO/vendor/item, GRN/gate/PO/vendor/item, QC/GRN/item/serial, Estimated/Production BOM job and item/UOM, Actual BOM job/item/UOM/GRN/bill/serial, and RFQ/quotation/comparison/PO vendor projections.

## 4. Post-audit request-input reachability

Thirty-five request/input records were re-audited across MIR, Issue, Return, Vendor Bill, Job Order, Estimated BOM, Production BOM, engineering documents, fitment and FAT.

### Passing paths

- MIR edit/transition/decision versions and line IDs come from MIR reads.
- MIR `RequestingDepartmentId` comes from `/session/me` for the requester.
- MIR/Estimated/Production BOM ItemId and a valid default UomId now come from ItemSummary/ItemDetail through `BaseUomId`; no `masters.uoms:view` grant was added.
- Return acceptance Version comes from the Return read.
- Vendor Bill decision Version comes from the Vendor Bill read.
- Job Order confirmation Version comes from the Job Order read.
- Estimated BOM edit/action/header versions, item merge survivor, Production BOM edit/action/revision IDs, and engineering-document versions come from their own readable projections.
- Fitment reversal ID comes from the fitment list/detail. FAT explanation can use the material-issue line IDs returned by a blocked reconciliation attempt, after a reconciler creates that attempt.

### Permission-compatible source failures found

| Caller/command | Mandatory input with no usable source | Why it fails |
| --- | --- | --- |
| PRODUCTION_COORDINATOR / PRODUCTION_MANAGER create Job Order | CustomerPurchaseOrderLineId | Customer PO detail's `CustomerPoLineDto` omits the line ID. These roles also do not hold `sales.customer-po:view`. |
| Estimated BOM preparers create Estimated BOM | JobOrderId | DESIGN_ENGINEER, TECHNICAL_DIRECTOR and the SESS-04/SESS-05 employee grants can operate Estimated BOMs, but the Job Order list is limited to Production/Accounts roles and there is no Estimated-BOM Job Order candidate endpoint. |
| SERVICE_ENGINEER and other non-Job-Order roles create a job-backed MIR | JobOrderId and CustomerPurchaseOrderLineId | MIR Create is granted broadly, but no command-scoped eligible Job Order/Customer PO line candidate read exists. Consumable/office MIRs are reachable because these fields are not required there. |
| STORES_ASSISTANT / STORES_EXECUTIVE / STORES_MANAGER issue material | IssuedToEmployeeId | The only general employee-ID source is `employees.master:view`; Stores issue roles do not have a command-scoped eligible engineer/employee candidate endpoint. |
| Engineer creates Material Return | MaterialIssueId and MaterialIssueLineId | Returners do not have the Stores-only material-issue read and there is no own-custody return candidate projection. |
| ACCOUNTS_ASSISTANT / ACCOUNTS_MANAGER create Vendor Bill | GoodsReceiptId and GoodsReceiptLineId | Vendor Bill Create has no command-scoped finalized-GRN candidate read; Accounts roles should not be granted the whole GRN page merely to obtain IDs. |
| TECHNICAL_DIRECTOR pins Production BOM | ExpectedJobOrderVersion | ProductionBomView exposes JobOrderId but not JobOrderVersion; TD does not hold the Job Order page read. Production Manager can obtain it from Job Order detail, so this is role-specific. |
| Engineering-document preparers create/revise a drawing | DrawnByEmployeeId and CheckedByEmployeeId | No command-scoped eligible employee lookup exists, and these roles/employee grants need not hold the full employee master. |
| Fitment confirmation roles | MaterialIssueLineId (and for SERVICE_ENGINEER, JobOrderId) | Material issues are readable only to Stores roles; fitment has no issued-custody candidate endpoint. Existing fitment reads only show already-confirmed rows. |
| QC_MANAGER performs FAT reconciliation | JobOrderId route value | QC_MANAGER has FAT Verify but no Job Order/Estimated BOM/fitment candidate list that returns eligible JobOrderId values. |

No permission was widened to hide any of these failures.

## 5. Automatic prevention design

The focused regression added here locks the three reported fields and every audited Version path. The durable solution is a route-level reachability manifest, not another periodic source review:

1. Every command endpoint declares metadata for each server-derived selector: request/path property, conditional-required predicate, source GET route, source response property, and source action permission.
2. The endpoint already declares its command page/action; the canonical operation-role policy supplies the service roles.
3. A build test enumerates ASP.NET endpoint metadata, expands every role allowed to invoke the command, and proves that role can call at least one declared source GET and that the source contract exposes the same CLR type.
4. Non-null Guid/version properties and explicitly conditional selectors must have manifest entries. Adding a request field or endpoint without one fails with `Command / role / property has no readable source`.
5. Candidate reads use the command permission where broad master/transaction View would violate least privilege.

This should be implemented while closing the ten failures above; otherwise a passing manifest would merely encode known false claims.
