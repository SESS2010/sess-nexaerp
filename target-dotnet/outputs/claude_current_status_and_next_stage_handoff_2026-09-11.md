# SESS NexaERP — Current Status and Next-Stage Handoff for Claude

Date: 11 September 2026  
Repository reviewed: `target-dotnet`  
Current HEAD: `8b9c880 Align GRN QC deadline with receipt time`

## 1. Executive status

The backend’s core evidence chain is operational and proven against real,
disposable PostgreSQL:

```
PR → RFQ → quotation → comparison → PO
   → Gate Entry → GRN → incoming QC → concession/put-away
   → MIR → issue into engineer+job custody → return
   → vendor bill → FIFO/landed-cost evidence
   → Job Order → fitment → generated Actual BOM → FAT readiness
```

This does **not** mean the ERP is complete for go-live. Opening stock, bulk
master imports, vendor advance/import purchase, operational reports, production
authentication, dashboards, DC/customer-property/tool workflows, wider QC,
performance, backup/restore and installation acceptance remain.

Two completion measures must remain separate:

- **50-item specification acceptance:** 10/50 complete = **20%**.
- **Planning-weighted progress:** 10 complete + 22 partial/foundation + 18
  unbuilt gives approximately **42%** if partial is counted as half. This is a
  planning estimate, not acceptance.
- **Core transaction-chain completion:** PR through FAT/Actual BOM works. That
  is the strongest completed area, but it is only one part of product readiness.

## 2. Current evidence baseline

- Git HEAD: `8b9c880`
- EF migrations in source: **74**
- Latest migration: `20260911104631_AlignGrnQcDueAtWithReceiptTime`
- Model/table baseline last witnessed by the owner: **209 tables**; the latest
  migration adds no table.
- Latest fresh verification:
  - Debug build: success, 0 warnings, 0 errors
  - Debug suite: **785 passed, 0 failed, 0 skipped**
  - Release build: success, 0 warnings, 0 errors
  - Release suite: **782 passed, 0 failed, 0 skipped**
- Item 9 PostgreSQL guard: Up → Down → reapply passed.
- Owner database was not touched while producing this status.
- The owner database’s exact migration-history position after `8b9c880` has
  not been read and must not be inferred.

Unrelated untracked paths already present and not part of ERP work:

- `../legacy-reference/`
- `outputs/item_vendor_customer_api_fields_report.tex`

## 3. What is operational now

### Platform and security

- Company-scoped authorization.
- Multiple simultaneous effective employee roles.
- FULL, SUPPORT and TEMPORARY assignments with effective dates.
- Automatic operation-specific role resolution.
- SUPPORT denial for approval/control/configuration operations.
- Acting role and assignment provenance in command/audit evidence.
- Ordinary immutable audit guard and ordinary command ledger.
- REV869B ordinary-deployment dependency retired.
- Role-assignment governance, history, self-assignment refusal.
- Session permission projection and endpoint authorization share one policy.
- Malformed request GUIDs return field-addressed 400 responses.
- Request-input and actor/read reachability regression controls exist.
- Ownership convergence and mandatory ACL reconciliation exist.

### Purchase and Stores core

- PR lifecycle, amount bands and approvals.
- Stores-scoped stock-check list and detail.
- RFQ creation/invitation.
- Quotation submission and technical verification.
- Comparison recommendation/approval/revision.
- PO create/submit/issue/amend/revise/approve/reject/cancel.
- Material follow-up transition and permission alignment.
- Multi-company document-number collision correction.
- Gate Entry and GRN lifecycle, sorting and receipt controls.
- Serial/lot capture and immutable provenance.
- Incoming QC, partial acceptance and TD concession.
- Put-away to AVAILABLE.
- MIR lifecycle with optimistic concurrency.
- Issue requires approved MIR; custody identifies engineer and Job Order.
- No consumption at issue, including consumables.
- Material return against a specific issue.
- Vendor bill exact three-way match and Accounts acceptance.
- FIFO consumption oldest-first, independent of physical serial.
- Immutable landed-cost adjustment ledger.
- Last purchase rate/date/bill provenance.

### Production/cost evidence

- Estimated BOM manual lifecycle and workbook import.
- Frozen EstimatedUnitValue.
- Draft item governance, approval and immutable duplicate aliasing.
- Production BOM revision/approval/pinning.
- Engineering-document revision workflow.
- Governed one-machine/one-Job-Order creation.
- Fitment confirmation, reversal and re-verification.
- Generated Actual BOM valued from accepted bill evidence.
- Estimated/Production versus Actual variance.
- FAT custody reconciliation and readiness block.
- Recovery exits now exist for Submitted Estimated BOM, Submitted Production
  BOM, Submitted engineering document, and Job Order Pending Accounts.
- Rejected bills and rejected purchase documents remain intentional terminal
  evidence because replacement/revision paths exist.

### Notifications

- Backend in-app notification event generation and employee-scoped read API
  exist.
- Notification frontend page/header experience is not present in current HEAD.

## 4. Numbered specification status, items 1–50

Legend:

- **COMPLETE** — acceptance behavior and refusal path are implemented/tested.
- **PARTIAL** — useful schema/service/projection exists, but the item’s witness
  cannot be completed.
- **NOT BUILT** — no operational workflow satisfying the item.

| Item | Status | Current position / remaining work |
|---:|---|---|
| 1 | COMPLETE | Estimated BOM valueless submission refusal and recovery exit. |
| 2 | COMPLETE | TD/MD/mapped approver PR visibility corrected. |
| 3 | COMPLETE | RFQ/comparison/PO details project DTOs without serializer cycles. |
| 4 | COMPLETE | Lookup reachability regression and narrow read sources. |
| 5 | COMPLETE | Stores-scoped StockCheckPending PR detail. |
| 6 | COMPLETE | Session permissions and endpoint filter use canonical policy. |
| 7 | COMPLETE | Global malformed-GUID field errors. |
| 8 | COMPLETE | Eleven development identity mappings corrected. |
| 9 | COMPLETE | QcDueAt aligned to receipt time; migration and derivative audit complete. |
| 10 | NOT BUILT | Vendor advance lifecycle, reversals, oldest-first bill adjustment and vendor position. **Next item.** |
| 11 | PARTIAL / DECISION BLOCK | Currency fields/commercial foundations exist, but import purchase does not. Exchange-rate date still needs the explicitly required Accounts decision; do not choose in code. |
| 12 | COMPLETE | REV869B ownership convergence, schema/table/function ACL recovery, FK regression, mandatory post-migration reassign/reconcile runbook. |
| 13 | NOT BUILT | Three-actor opening stock ceremony and OPENING_BALANCE posting. Hard go-live blocker. |
| 14 | PARTIAL | Import framework exists; item, employee, warehouse/rack and item-vendor adapters remain. |
| 15 | PARTIAL | Ancestry/variance point reads exist; ten scoped, exportable operational reports do not. |
| 16 | PARTIAL / REPORT-ONLY | Bootstrap/dev identity documentation exists. Production Cognito ceremony and on-prem alternative are not operationally witnessed. |
| 17 | PARTIAL FOUNDATION | DeliveryChallan schema exists; complete dispatch/custody/handoff/shortfall workflow does not. |
| 18 | NOT BUILT | Stock adjustments, cycle counting, freeze/recount and inventory-period controls. |
| 19 | NOT BUILT | Same-company transfers, intercompany commercial sale/purchase and scrap disposal chain. |
| 20 | NOT BUILT | Operational job-work/subcontract lifecycle. |
| 21 | PARTIAL FOUNDATION | Customer-property foundation exists; inward/work-block/removed-parts/return case does not. |
| 22 | PARTIAL FOUNDATION | Asset/tool foundations exist; custody, loss, clearance and calibration operations do not. |
| 23 | PARTIAL | Vendor qualification lifecycle exists; computed vendor rating, bands, concurrence and revaluation do not. |
| 24 | PARTIAL | PO amendment/cancel exists; debit/credit notes, purchase return, blanket and rate contracts remain. |
| 25 | PARTIAL | Some concurrency/version/ledger tests exist; the complete simultaneous-operation and deadlock matrix remains. |
| 26 | PARTIAL | Transactions and idempotency are strong; restart, disk-full, uncertain-commit and two-instance fault evidence remains. |
| 27 | NOT BUILT | 300k-item/2m-movement load fixture and measured performance report. |
| 28 | PARTIAL | Installer/runbook/backup components exist; one-day empty-to-running install, automatic restore verification and customer upgrade witness remain. |
| 29 | NOT BUILT | Purchase dashboard projection. |
| 30 | NOT BUILT | Stores dashboard projection. |
| 31 | PARTIAL | Job/FAT/cost source endpoints exist; one-round-trip dashboard projection and drill-throughs do not. |
| 32 | NOT BUILT | TD/MD decision-only composite view. |
| 33 | PARTIAL | Notification engine/read API exists; UI page, header unread count and deep-link experience remain. |
| 34 | PARTIAL | Own custody read exists; full engineer portal—sites, tasks, handoffs, shortfalls, returns, deviations—does not. |
| 35 | NOT BUILT | Fifteen-stage QC process check sheet. |
| 36 | NOT BUILT | Powder-coating/CNC post-GRN job-work inspection. |
| 37 | PARTIAL FOUNDATION | Calibration-related foundation concepts exist; register, provider, certificate, custody and due workflow do not. |
| 38 | NOT BUILT | Corrected DC two-axis model: material type plus three commercial natures and purpose. |
| 39 | PARTIAL | Incoming QC queue and FAT sources exist; unified QC working screen does not. |
| 40 | PARTIAL FOUNDATION | DC entity exists; corrected operational two-axis DC and bought-demo reference document do not. |
| 41 | PARTIAL FOUNDATION | Customer-property structures exist; governed case workflow and close guards do not. |
| 42 | PARTIAL FOUNDATION | Tool/asset structures exist; individually governed tool custody/loss/clearance workflow does not. |
| 43 | NOT BUILT | QMS-wide controlled documents and annual review. MANAGEMENT_REPRESENTATIVE must approve quality manual. |
| 44 | NOT BUILT | Vendor payment allocation across one/multiple accepted bills. |
| 45 | PARTIAL | Item reorder fields exist; actionable list and Purchase+Stores notifications do not. |
| 46 | PARTIAL | Lot expiry exists; chemical-consumable shelf-life policy, immediate concerned-person notification and issue warning evidence do not. |
| 47 | NOT BUILT | Corrected job-work: powder coating by dual-signed COUNT; CNC by WEIGHT/scrap/tolerance/weighbridge slip. |
| 48 | NOT BUILT | Purchase GRN weight verification and variance. |
| 49 | NOT BUILT | Calibration failure back-assessment list and QC decisions. |
| 50 | NOT BUILT | Customer-property loss/damage/customer-notification/close block. |

Totals:

- COMPLETE: **10**
- PARTIAL/foundation: **22**
- NOT BUILT: **18**
- Acceptance-complete: **20%**
- Remaining acceptance items: **40**

## 5. Frontend status in current HEAD

The repository frontend currently contains screens/components for:

- login
- employee/customer/vendor/item masters
- Customer PO
- Purchase Requisition
- RFQ
- quotation
- comparison
- Purchase Order
- Gate Entry

The current frontend tree does not contain committed screens for the complete
backend workflow after Gate Entry: GRN, incoming QC, MIR/issue/return, vendor
bill, Job Order/BOM/fitment/FAT, notification page, dashboards or engineer
portal.

A frontend developer may have additional work on another machine or branch.
Do not count that as merged until its commit is an ancestor of current HEAD.

## 6. Immediate blockers to SESS beginning operational use

Priority order follows the governing specification:

1. **Item 10 — Vendor advance.**
2. **Item 11 — Import purchase**, after Accounts confirms exchange-rate date.
3. **Item 13 — Opening stock.** The ERP otherwise starts with zero usable
   inventory.
4. **Item 14 — Master import adapters.** Direct SQL is not an acceptable
   customer load path.
5. **Item 15 — Operational reports and audit dossier.**
6. **Item 16 — Production identity decision/witness.**
7. Frontend parity for the already-working backend chain.
8. Final clean end-to-end installation/use witness with no developer SQL or
   workaround.

Item 12 is already complete and should not be rebuilt.

## 7. Recommended next-stage plan

### Stage A — Complete commercial and go-live block

1. Build item 10 Vendor Advance as its own commit.
2. Stop at item 11 if exchange-rate-date authority is still unanswered.
3. Build item 11 once Accounts confirms the date basis.
4. Build item 13 Opening Stock.
5. Build item 14 import adapters.
6. Build item 15 reports.
7. Finish the item 16 identity report and choose/witness the production
   identity path.

Witness after each migration:

- fresh build before tests
- disposable PostgreSQL Up → Down → reapply
- Debug and Release full-suite counts
- expected row counts
- owner database untouched until human witness

### Stage B — Operational visibility

After Block E:

- items 29–34 projections/pages
- prove dashboard numbers from the end-to-end witness database
- outstanding custody always grouped by engineer
- never present provisional cost as final
- keep offer-versus-actual unavailable until Sales supplies a real offer value;
  never infer selling price from Estimated BOM

### Stage C — Remaining Stores controls

- item 17 DC custody
- item 18 adjustment/cycle count
- item 19 transfer/intercompany/scrap
- item 20 job work
- item 21 customer property
- item 22 tools/calibration
- item 23 vendor rating
- item 24 remaining purchase

### Stage D — Product hardening

- item 25 concurrency
- item 26 failure behavior
- item 27 volume/performance
- item 28 install/backup/upgrade

### Stage E — Quality and ISO completion

- items 35–39 QC module
- items 40–42 Stores completion
- items 43–50 September decisions

## 8. Decisions and corrections Claude must not lose

- Never widen an unrelated permission to compensate for a missing read field.
- All employee roles act simultaneously; there is no user role switcher and no
  authorization “primary role.”
- Resolve the least-privileged sufficient effective assignment automatically.
- SUPPORT may read/create/update/submit but cannot approve, reject, cancel,
  reverse, deactivate, configure permissions or administer roles.
- Every governed command records resolved role and assignment ID.
- Posted evidence is immutable; corrections use reversal/compensating evidence.
- Issue is custody, not consumption—even for consumables.
- Fitment starts CONSUMPTION_OUT.
- Actual BOM is generated, never authored, and uses accepted-bill landed value;
  FIFO valuation remains separate.
- Estimated BOM value is not an offer/selling price.
- Customer-facing excess requires an authoritative TD decision.
- Opening stock requires three different actors.
- Intercompany stock movement is a real sale/purchase, never an internal move.
- Only commercial natures: RETURNABLE, NON_RETURNABLE, WARRANTY_FREE.
  DEMO/JOB_WORK/SITE_WORK/TRIAL are purposes; RETURNABLE always needs a due
  date.
- Powder coating reconciliation is COUNT, dual-signed by QC and Stores, with no
  tolerance.
- CNC reconciliation is WEIGHT with returned scrap, configurable default 10%;
  above 500 kg requires an external weighbridge slip.
- Quality manual approval is by MANAGEMENT_REPRESENTATIVE role, not a hardcoded
  employee.
- Calibration failure lists affected measurements; QC decides rechecks.
- Customer-property cases cannot close with open damage/loss evidence.
- Commit on actual current HEAD; report ancestry conflict instead of
  synthesizing a parent.

## 9. Exact resume point

Resume at **specification item 10: Vendor Advance**.

Before coding item 11, obtain the missing Accounts decision for the exchange
rate date. If the current consolidated instruction claims every item is
decided, this unresolved item-11 instruction is a contradiction and must be
reported rather than silently resolved.
