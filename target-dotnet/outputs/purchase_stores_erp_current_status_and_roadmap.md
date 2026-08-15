# Purchase and Stores ERP — Current Status and Roadmap

## 1. Management summary

The repository has a substantial, compiling Purchase backend and shared ERP foundation, but it is **not ready for production or PostgreSQL acceptance execution**. Offline quality is strong: the final lineage builds with 0 warnings/errors and all 445 non-PostgreSQL tests pass. REV869B Correction 23 improved the control-plane source, but its required internal precheck found two remaining source/test design issues. A fresh independent source-only review is still mandatory.

Stores has useful masters and inventory foundations, but the operational Stores lifecycle is materially less complete than Purchase. The percentages below are planning estimates based on implemented source breadth, automated offline coverage and missing acceptance/deployment work; they are not production-certification percentages.

## 2. Current architecture

- .NET layered solution: Domain, Application, Infrastructure and API, with EF Core/Npgsql persistence.
- PostgreSQL schema under `nexa`, with retained ordered migrations through REV869B.
- Employee identity resolution, organization scope, role/page permissions and audit writing are cross-cutting controls.
- Purchase and command operations use target-local transactional business/history/receipt ledgers.
- REV869B lifecycle architecture is frozen: external provisioning; dedicated lifecycle controller; surviving control-plane database; disposable isolated target databases; no lifecycle-administrator credential in application/test code.
- The present workspace is source-only. No deployed controller, exact provisioned cluster or authorized PostgreSQL acceptance environment was available.

## 3. Completed foundation

The source includes:

- Employee, department, designation, reporting, identity, role, operational-scope and page-permission foundations.
- Customer, vendor, item, UOM/conversion, tax/GST, warehouse, rack/bin, vendor-qualification and related master foundations.
- Organization-scoped authorization filters and record-scope checks.
- Approval/status/audit history patterns and immutable/controlled transition contracts.
- EF migration and snapshot parity, no-connect discovery and reproducible offline Up/Down SQL generation.
- 445 passing non-PostgreSQL tests on the final lineage.

## 4. Purchase status

### Implemented source modules

- Purchase requisition create/update/list/detail.
- PR submit, verify, approve, reject, request-revision, resubmit, cancel and hold flows.
- Stock-check, reservation and purchase handoff read paths.
- RFQ creation and vendor invitation.
- Vendor quotation revisions and attachment retrieval.
- Technical verification.
- Commercial comparison create, recommend, approve, reject, request-revision and resubmit.
- Purchase order create, submit, approve, reject, issue, amend, revise-rejected and cancel.
- Material follow-up transitions and reads.
- Organization scope, page/action permission separation, approval-policy snapshots, commercial masking, GST/payable calculations, optimistic versioning and audit/history source controls.

### Pending Purchase work

1. Resolve or formally reconcile the two Correction 23 internal findings: normal-drop registration/event linkage, and non-generic scenario-specific evidence.
2. Obtain a fresh independent source-only safety decision.
3. Provision and independently review the external lifecycle controller/control plane, pins, roles, ACLs and isolated test targets.
4. Run the separately authorized 34-scenario PostgreSQL acceptance matrix and retained Purchase PostgreSQL regression suite.
5. Complete user-facing workflow/UI integration where the repository currently exposes APIs only.
6. Complete operational reporting, notifications, attachment lifecycle, vendor-facing integration and exception dashboards as product requirements dictate.
7. Perform end-to-end UAT with real role matrices, representative organizations, approval thresholds, taxes, currencies and amendment/rejection cases.
8. Prepare deployment, monitoring, backup/recovery, support runbooks, training and cutover controls.

## 5. Stores status

### Available foundation

- Item, warehouse and rack/bin master APIs with permission and audit-history patterns.
- Warehouse-condition locations and QC inspection policy foundations.
- Stock movement domain foundation.
- PR stock-check/reservation/handoff concepts and Purchase material-follow-up handoff.

### Pending Stores work

- Purchase-order receipt/GRN, over/under receipt tolerances and receipt reversal.
- QC hold, inspection, acceptance/rejection and disposition integration.
- Put-away, bin-level balances and controlled location movement.
- Material issue, return, transfer and reservation-consumption workflows.
- Immutable stock ledger and reconciliation to balances.
- Batch/lot, serial, expiry and traceability where applicable.
- Cycle count, physical count, adjustment approval and variance audit.
- Valuation/costing policy and finance integration boundaries.
- Slow/non-moving, ageing, reorder, shortage and inventory valuation reports.
- Stores role matrix, maker/checker approvals, exception handling and audit histories.
- UI, barcode/device integration if required, UAT, performance/concurrency tests and operational runbooks.

## 6. Honest completion estimates

| Area | Source-function estimate | Production/pilot readiness estimate | Basis |
|---|---:|---:|---|
| Shared ERP foundation | 85% | 55% | Broad source and offline coverage; exact deployment/PG/security acceptance unavailable. |
| Purchase backend/business workflows | 78% | 50% | Major API and domain flows exist; source safety, PG acceptance, UI/integration and UAT remain. |
| Purchase end-to-end product | 62% | 40% | Backend is ahead of operator experience, deployment and acceptance. |
| Stores backend/business workflows | 30% | 15% | Masters and concepts exist; core receipt/put-away/issue/ledger/count flows are pending. |
| Stores end-to-end product | 22% | 10% | Most operational workflows, UI, reports and UAT remain. |
| Combined Purchase + Stores program | 48% | 28% | Weighted planning estimate; no production certification is implied. |

These estimates should be re-baselined after management freezes Stores scope and the independent REV869B review establishes the actual safety correction boundary.

## 7. Week-wise roadmap

Assumption: dedicated engineering/test ownership, management decisions within one business day, an available independent reviewer, and an externally provisioned non-production PostgreSQL environment only after source approval.

| Week | Outcome |
|---|---|
| 1 | Fresh independent Correction 23 review; reconcile F23-01/F23-02; management decides whether any new bounded source correction may be authorized. No PostgreSQL work. |
| 2 | If source-approved, independently review controller/provisioning package, role/ACL manifest, pins, credential custody and isolated-target operating procedure. |
| 3 | After a separate execution GO, run the exact 34 REV869B scenarios plus retained Purchase PostgreSQL regression; triage evidence without broadening scope. |
| 4 | Close Purchase acceptance defects, finish missing operator/API integration details, reports and exception paths; freeze pilot scope. |
| 5 | Purchase end-to-end UAT across organizations, permissions, approvals, GST/payables, rejection/revision, PO issue/amend/cancel and audit evidence. |
| 6 | Purchase pilot with a small trained user group, controlled vendors/items and daily reconciliation; no broad rollout. |
| 7 | Purchase stabilization and go/no-go for regular use; start Stores detailed process/design freeze and acceptance criteria. |
| 8 | Implement Stores receipt/GRN, QC hold/disposition and PO-to-receipt controls. |
| 9 | Implement put-away, bin balance, transfer, issue/return and reservation consumption with immutable stock movements. |
| 10 | Implement counts/adjustments, traceability, reporting, valuation boundary and maker/checker controls. |
| 11 | Stores integration, concurrency, reconciliation and role/UAT testing; prepare pilot data and training. |
| 12 | Stores limited pilot with daily physical/system reconciliation and exception review. |
| 13 | Stabilization; combined Purchase/Stores regular-use decision if two consecutive pilot cycles meet exit criteria. |

A credible earliest Purchase pilot is Week 6; regular Purchase use is Week 7 or later. A credible earliest Stores pilot is Week 12; combined regular use is Week 13 or later. Any failed safety gate, controller review, PostgreSQL acceptance or UAT cycle moves these dates.

## 8. Daily management involvement

Management should reserve 30–45 minutes daily through Purchase pilot and 45–60 minutes during Stores process/UAT weeks for:

- Same-day decisions on scope, organization rules, approval thresholds and segregation of duties.
- Named business owners for Purchase, Stores, Finance/Tax, IT/Security and Audit.
- Approval of representative but non-production test data and expected results.
- Daily review of blockers, evidence, unresolved risks and pilot exit criteria.
- Weekly sign-off on scope changes; no informal additions during a bounded correction or acceptance run.
- During pilots, review of transaction counts, exceptions, reconciliation differences, unauthorized attempts, failed approvals and support response time.

## 9. Pilot exit criteria

Do not begin regular use until all of the following are evidenced:

- Independent source-safety PASS and separately authorized PostgreSQL acceptance PASS.
- Exact roles/ACLs and controller provenance verified with no unexplained delta.
- Zero unresolved severity-1/2 defects; accepted plan for lower-severity issues.
- Complete audit/history evidence for sampled transactions.
- Business reconciliation of quantities, taxes, payable values, approvals and status transitions.
- Backup/recovery and support runbooks tested in the authorized non-production environment.
- Named business, security and operations owners sign the go-live decision.

## 10. Exact next management decision

**Authorize a fresh independent source-only review of Correction 23 commit `07a66905cf53a851927cfbc313aa348baa1f2133` and internal precheck commit `5b4cd48`, specifically adjudicating F23-01 and F23-02. Do not authorize PostgreSQL execution or a new correction from this roadmap.**
