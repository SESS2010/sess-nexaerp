# R1 Purchase and Production Manager page grants

TD Option B, 29 September 2026. Migration 144: `20260929181000_R1PurchaseProductionGrants`.

CONFIRMED: migration-144 disposable database readback matches all 46 page/action rows below (Purchase 28, Production 18). Focused tests passed 2/2; fast suite passed 1053/1053. This is the migrated repository role baseline. Employee access may combine additional roles and employee overrides. Production Manager retains PR and MIR approval; only PO and Comparison Approve are removed. Existing Reject, RequestRevision and unrelated actions remain unchanged. Purchase Manager receives PR, RFQ and Quotation View/Create/Submit only, with no new approval or Accounts permissions.

The migration journals exact before/after rows. Rollback restores the prior grants and refuses subsequent permission edits or deletion. Unexpected full-control grants on the two approval-removal targets, or approval/full-control on the Purchase author targets, stop migration for review rather than silently broadening the approved exception. No workflow routes, approval snapshots or employee role assignments change.

## PURCHASE_MANAGER (28 pages)

| Page key | Enabled actions |
|---|---|
| dashboards.purchase | View, ViewCommercialValues |
| dashboards.purchase-obligations | View, ViewCommercialValues |
| dashboards.purchase-open-orders | View, ViewCommercialValues |
| dashboards.purchase-spending | View, ViewCommercialValues |
| masters.item-categories | View, Print, Cancel, Create, Submit, Update, Download, Resubmit, UploadAttachment, ViewCommercialValues |
| masters.item-subcategories | View, Print, Cancel, Create, Submit, Update, Download, Resubmit, UploadAttachment, ViewCommercialValues |
| masters.items | View, Print, Create, Export, Reject, Submit, Update, Approve, Download, Resubmit, RequestRevision, UploadAttachment, ViewAuditHistory, RequestClarification |
| masters.manufacturers | View, Print, Cancel, Create, Submit, Update, Download, Resubmit, UploadAttachment, ViewCommercialValues |
| masters.store-category-routes | View, Print, Download, ViewCommercialValues |
| masters.uom-conversions | View, Print, Cancel, Create, Submit, Update, Download, Resubmit, UploadAttachment, ViewCommercialValues |
| masters.uoms | View, Print, Cancel, Create, Submit, Update, Download, Resubmit, UploadAttachment, ViewCommercialValues |
| masters.vendor-qualifications | View, Print, Cancel, Create, Submit, Update, Download, Resubmit, UploadAttachment, ViewCommercialValues |
| masters.vendors | View |
| masters.warehouse-condition-locations | View, Print, Download, ViewCommercialValues |
| purchase.commercial-comparisons | View, Create, Submit, Resubmit, ViewCommercialValues |
| purchase.intercompany-orders | View, Issue, ViewAuditHistory, ViewCommercialValues |
| purchase.po | View, Issue, Print, Create, Submit, Update, ViewCommercialValues |
| purchase.requisitions | View, Create, Submit |
| purchase.rfq | View, Create, Submit |
| purchase.vendor-quotations | View, Create, Submit |
| qc.inspection-policies | View, Print, Download, ViewCommercialValues |
| reports.purchase-register | View, Export |
| security.employee-identities | View, Print, Download, ViewCommercialValues |
| security.operational-scopes | View, Print, Download, ViewCommercialValues |
| settings.tax-gst | View, Print, Download, ViewCommercialValues |
| stores.material-issue-requests | View, Create, Submit, Update, ViewAuditHistory |
| stores.material-returns | View, Create, ViewAuditHistory |
| tracking.pending | View, ViewAuditHistory |

## PRODUCTION_MANAGER (18 pages)

| Page key | Enabled actions |
|---|---|
| design.estimated-bom | View |
| masters.uoms | View |
| production.component-fitments | View, Cancel, Create, ViewAuditHistory, ReplaceAttachment, ViewCommercialValues |
| production.fat-readiness | View, Create, ViewAuditHistory |
| production.job-orders | View, Create, Submit, Update, ViewAuditHistory |
| production.production-bom | View, Create, Submit, Update, Download, ViewAuditHistory |
| purchase.commercial-comparisons | View, Reject, RequestRevision, ViewAuditHistory, ViewCommercialValues |
| purchase.po | View, Reject, RequestRevision, ViewAuditHistory, ViewCommercialValues |
| purchase.requisition-approvals | View, Reject, Approve, RequestRevision, ViewAuditHistory, ViewCommercialValues |
| purchase.requisitions | View, Reject, Verify, Approve, RequestRevision, ViewAuditHistory, ViewCommercialValues |
| quality.vendor-manual-assessments | View, Create, ViewAuditHistory |
| reports.pending-approvals | View, Export |
| reports.purchase-register | View, Export |
| stores.machine-deliveries | View |
| stores.material-issue-requests | View, Create, Reject, Submit, Update, Approve, ViewAuditHistory |
| stores.material-issues | View |
| stores.material-returns | View, Create, ViewAuditHistory |
| tracking.pending | View, ViewAuditHistory |

## Validation contract

Focused disposable-database tests verify exactly eight affected permission rows, six additions, unchanged unrelated permissions, retained PR/MIR approval, unchanged TD/Accounts approval grants, refusal of custom full-control conflicts, refusal of changed rollback, exact rollback/reapply and a Production Manager HTTP 403 on the PO approval endpoint using the real page-permission service. The admitted PO-list read is the positive control.

This page-grant change does not grant other workflow roles to Purchase Manager. Existing endpoint/service business rules and maker-checker checks remain in force. No Accounts page or additional approval permission is introduced.
