# SESS NexaERP specification implementation audit

Date: 9 September 2026. Scope: frozen Purchase, Stores, QC, production hand-offs, role, custody and costing decisions.

`Partial` means schema or only part of the workflow exists; a real user cannot complete the stated result end to end. Repeated statements of one decision are consolidated. The DOCX renderer lacked `pdf2image` and LibreOffice/Poppler were unavailable, so both DOCX sources were read from OOXML; this is a content-to-code audit, not layout certification.

| Decision | Source | State | What remains |
|---|---|---|---|
| Release needs workflow, permission, posting, reversal, audit, reconciliation and acceptance, not a screen. | Guideline §1; Requirement §§1,21–22 | Partial | Core PR-to-FAT is witnessed; DC, counts, customer property, vendor rating, reports and product gates remain. |
| Company comes from session/document ancestry; legal entities retain separate books/numbers/tax/stock. | Guideline §§1.2,4; Requirement §§2,4 | Yes | Intercompany commercial movement remains. |
| Location, ownership, custody, reservation and valuation stay separate. | Guideline §§1.2,4 | Partial | Ledger/MIR do; reservation and wider DC/site/vehicle custody do not. |
| Posted evidence is immutable; correction is reversal/repost. | Guideline §§1.2,4 | Partial | Current controlled workflows comply; future DC/count/transfer/tool workflows do not exist. |
| QC_HOLD is pending, not final disposition. | Guideline decision 1 | Yes | — |
| Delivered excess is recorded and segregated; GRN cannot accept beyond PO before revision. | Guideline decision 2 | Yes | Vendor-return closure remains. |
| AVAILABLE stock never goes negative; no emergency override. | Guideline decision 3/ruling 4 | Yes | Broader concurrency proof remains Part 15. |
| Issue requires approved MIR. | Guideline decision 4 | Yes | — |
| Internal same-item BOM excess is allowed with variance evidence and notification. | Guideline decision 5 | Partial | Evidence exists; notification creation/delivery does not. |
| Customer-facing excess requires exact TD decision, never a client bypass. | Guideline §5.2.1 | Yes | Later Sales offer-resolution links are deferred. |
| Normalized serial is unique at finalization. | Guideline decision 6 | Yes | — |
| Returnable loss closes only through typed loss/damage/write-off. | Guideline decision 7 | No | Document, approval and reconciliation path remain. |
| Actual BOM uses accepted bill allocation; FIFO issue value is separate. | Guideline decision 8/§6 | Partial | Core separation works; landed charges are absent. |
| Effective QC_MANAGER acts and actual inspector is recorded. | Guideline decision 9 | Yes | Old PR-raiser-fallback wording conflicts with later role governance and should be corrected. |
| Reservation is availability, not movement/location. | Guideline decision 10 | Partial | Foundation exists; complete workflow/API not found. |
| QC_HOLD differs from QUARANTINE. | Guideline decision 11 | Yes | — |
| Every tool is an Item plus identified asset, with no value threshold. | Guideline decision 12/TOOL-1 | Partial | Schema exists; operational custody/loss/clearance does not. |
| Approval bands are <₹5k manager, ₹5k–₹100k manager+TD, >₹100k manager+MD; missing configuration fails closed. | Guideline ruling 1 | Yes | Older requirement bands are superseded. |
| Strict FIFO by company/item/ownership; physical serial does not pick cost layer. | Guideline ruling 2/V1 | Yes | Landed-rate and deep concurrency witnesses remain. |
| Use-as-is concession follows rejection and needs direct TD. | Guideline ruling 3/§5.1.1 | Yes | — |
| Concession binds quantity/lot/serial/failure and posts atomically; correction reverses. | Guideline §5.1.1 | Yes | — |
| QC is per GRN-line lot; absent policy fails to QC_HOLD; shortage is DISCREPANCY_PENDING. | Guideline B3-2/B3-4/B3-6 | Yes | Broader discrepancy-resolution UI evidence remains. |
| Provenance survives receipt, QC, issue, return and fitment. | Guideline §5.1.1 | Partial | Current chain does; transfer/DC does not. |
| Production authentication is OIDC; provider groups never authorize ERP actions. | Guideline ruling 6; Requirement §3 | Partial | Mapping/bootstrap exist; production provider deployment/witness remains. |
| Multiple effective company roles act simultaneously; server records resolved role+assignment. | Role Catalogue; revised Phase 2 | Yes | Catalogue counts/holders/primary-role language are stale against 51 roles/159 assignments. |
| SUPPORT reads/creates/updates/submits but cannot control/approve/administer. | Revised Phase 2 | Yes | UI must keep `/session/me.Permissions` as sole authority. |
| Nobody self-assigns roles; assignment history is dated, typed, audited. | Role Catalogue §11; Phase 2 | Yes | — |
| Every service-required role has an effective holder. | Role Catalogue §11 | Partial | QC_MANAGER fixed; continuous dead-role operational detection remains advisable. |
| PR→RFQ→quote→comparison→PO retains linkage, revisions, approvals and idempotency. | Requirement §6 | Partial | Core works; RFQ PDF/email/reminders and richer negotiation evidence remain. |
| Stores has least-disclosure stock-check PR reads. | Requirement §§6.2–6.3 | Yes | — |
| PO amendment/revision/cancellation retains history and reapproval. | Requirement §§6.8–6.9 | Partial | Full received/invoiced short-close and vendor communication remain. |
| Material follow-up retains promises, history and version. | Requirement §6.10 | Partial | Alerts/escalation delivery does not. |
| Gate Entry→GRN→QC partial/concession→put-away yields traceable AVAILABLE stock. | Requirement §§7–10 | Yes | Vendor-return and reporting remain. |
| MIR supports job/no-job cases; issue records engineer+job custody and is not consumption. | Guideline §§5.2–5.3 | Yes | — |
| Return is tied to issue; returned quantity/serial cannot escape issued provenance. | Requirement §§10–11 | Yes | Same-day notifications are absent. |
| Confirmed fitment starts CONSUMPTION_OUT; reversal compensates; Actual BOM is generated. | Guideline §5.3 | Yes | Pattern reporting and Part 15 concurrency remain. |
| One machine has one stable Job Order pinned to approved Production BOM. | Guideline §5.3; Job Order spec | Yes | Full production task execution remains. |
| Estimated BOM is revisioned/frozen, manual/import capable, and may use Draft items. | Guideline BOM decisions | Yes | Copy-from-completed-Actual activation remains. |
| Production BOM and engineering documents are revisioned/approved/superseded. | Guideline §5.3; Requirement §5 | Yes | Universal retention/access audit remains partial. |
| Last purchase price is newest accepted company bill line with bill provenance. | TD pricing decision; Bill spec | Yes | Null until first accepted bill; owner DB was not queried. |
| Estimated line value uses last rate or explicit override, never zero, frozen at approval. | TD pricing decision | Yes | Frontend must visibly require missing values. |
| Estimated-vs-Actual quantity/monetary variance uses frozen Estimated and Production baselines. | Guideline §6 | Yes | Sales offer-price margin is deferred and must not be inferred. |
| Bill matches PO/GRN price+quantity exactly; Accounts accepts; accepted bill immutable. | Bill spec; Requirement §§8.5,14 | Yes | Debit/credit notes and purchase return remain. |
| Bill acceptance atomically updates LastPurchaseRate/Date/BillId. | TD pricing decision | Yes | — |
| FIFO layers are DB-controlled at GRN and oldest-first at issue, regardless of physical serial. | Bill spec; Guideline §6 | Yes | Landed charges and racing-last-layer test remain. |
| Recoverable GST excluded; duty/noncreditable tax/insurance/inward charges allocate into landed cost. | Consolidated Part 4b; Guideline V3 | No - decision blocked | Charges are authoritative only at later bill acceptance, while immutable FIFO layers are created and may be consumed at GRN. TD must choose pre-GRN charge authority, issue hold until bill, or an immutable later cost-adjustment ledger. |
| Vendor advance is PO-scoped/capped/adjusted/visible. | Remaining Plan; Part 4c | No | Entire workflow plus cancellation rule remains. |
| Import PO/bill keeps foreign currency and rupee landed cost. | Remaining Plan; Part 4d | No | Exchange-date decision and implementation remain. |
| FAT cannot become READY with unexplained custody. | Later FAT decision | Yes | Notification delivery is absent. |
| DC type is immutable; only MACHINE needs customer signature. | DC decisions 1–3 | No | Schema only; operational DC module absent. |
| Tools return 100%; a missing tool is loss. | DC decision 4 | No | Tool/DC reconciliation absent. |
| Handoff is line-by-line; custody moves only on acceptance. | DC decisions 5–6 | No | Handoff schema only. |
| One-day accept/explain timers escalate and create manager defect. | DC decision 7 | No | Timers/events/delivery absent. |
| Stores judges explanations; Service Manager ≤₹5k and TD above accepts shortfall. | DC decisions 8–9 | No | Workflow absent. |
| Return without DC is accepted unidentified and auto-raises deviation. | DC decisions 10–11 | No | Register/event generation absent. |
| Deviation thresholds 3/5 configurable; reasoned waivers count against manager. | DC decisions 12–13 | No | Configuration/counters/waivers absent. |
| Engineer portal shows sites/tasks/custody/handoffs/shortfalls/deviations. | DC §10 | Partial | Own issue/custody read works; remaining projections do not. |
| Rating occurs after GRN; quality per line, other dimensions per GRN. | Vendor Rating decisions 1–2 | No | Entire rating workflow remains. |
| Six configurable weights; delivery/quality/documentation computed. | Vendor Rating decisions 3–4 | No | Entire rating workflow remains. |
| Rolling 12 months; provisional 100 until 3 receipts; fixed bands. | Vendor Rating decisions 5–7 | No | Aggregate remains. |
| Poor vendor PO needs both TD and MD. | Vendor Rating decision 8 | No | No rating exists to enforce. |
| Revaluation needs letter, QC recommendation, TD approval, five-receipt probation. | Vendor Rating decisions 9–10 | No | Entire workflow remains. |
| Comparison keeps price/terms/performance separate; contrary human choice records reason. | Vendor Rating decision 11 | Partial | Price/technical exists; performance ranking does not. |
| Import rating facts and recompute; discard hand-entered scores. | Vendor Rating decision 12 | No | Adapter remains. |
| Structured vendor qualification stores evidence, capability, approval and due date. | Vendor Rating §9 | Partial | Workflow exists; full evidence/rating/requalification linkage remains. |
| Opening stock is a three-distinct-actor, one-time controlled posting. | Guideline E6/B4-3 | No | Entire import/ceremony/posting remains. |
| Adjustments are typed with frozen bands and backdate/open-period controls. | Guideline E3–E5 | No | Entire workflow remains. |
| Cycle count uses A/B/C/annual frequency, scoped freeze and independent recount. | Guideline E7–E9 | No | Entire workflow remains. |
| Scrap write-off and disposal are separate; invoice/payment precede MD-approved dispatch. | Guideline E10/B4-7 | No | Entire workflow remains. |
| Same-company transfer is in-transit/accepted; intercompany is legal sale/purchase. | Guideline §5.8 | No | Entire workflow remains. |
| Customer property is outside SESS value with condition/photo/due-date/extension/buyback evidence. | Guideline §§5.5–5.6 | No | Foundation schema only. |
| Calibration is annual; provider mastered; expiry warns/records rather than blocks. | Guideline CAL-1 | No | Foundation schema only. |
| Vendor return/jobwork/RMA retains dispatch/return/typed resolution evidence. | Guideline §5.7 | No | Operational modules absent. |
| Attachments are versioned/hashed/permissioned and dangerous content blocked. | Guideline D1–D4; Requirement §§4,19 | Partial | Universal scanning, sensitivity access audit and retention remain. |
| Offline events retain event+entry time and later synchronize idempotently. | Guideline D6 | Partial | Schema accommodation only; client/sync deferred. |
| Finance/HR integration uses transactional outbox/retry/reconciliation. | Guideline D7–D8 | No | No outbox/worker found. |
| In-app notification deep-links to authorized record. | Requirement §16 | No | Tables exist; no insertion path, read endpoint or worker exists. |
| Required stock/custody/GRNI/purchase/variance/ancestry reports are scoped/gated/exportable. | Requirement §17; Part 12 | No | Point reads exist; report/export suite does not. |
| Backup/restore, load and one-day install are acceptance gates. | Requirement §§19–22; Remaining Plan | No | Parts 17–19 and 21 remain. |

| L1 reservation commitment-only | Guideline L1 | Partial | Reservation model exists; operational workflow incomplete. |
| L2 WIP has physical location plus custody | Guideline L2 | Partial | MIR/issue custody works; complete project/service WIP reconciliation does not. |
| L3 one bin may contain separately identified ownership classes | Guideline L3 | Yes | — |
| L4 supplier-loan stock is zero-valued and conversion uses normal purchase flow | Guideline L4 | No | Supplier-loan operational workflow/liability closure absent. |
| L5 customer property has zero inventory value plus declared/replacement value | Guideline L5 | No | Operational customer-property workflow absent. |
| L6 future custody supports employee plus optional vehicle/site | Guideline L6 | Partial | Schema accommodation exists; vehicle/site operation deferred. |
| L7 company site has many warehouses | Guideline L7 | Yes | Branch/site administration surface remains limited. |
| P1 project/customer order owns project; Production owns machine instances | Guideline P1 | Partial | Job Order machine identity works; complete Project module does not. |
| P2 three BOM identities and approval/freeze rules | Guideline P2/B2-1 | Yes | — |
| P3 machines in one project may pin different approved BOM revisions | Guideline P3 | Yes | — |
| P4 consumption begins at fitment, not issue | Guideline P4 | Yes | — |
| P5 fitment correction is reversal plus re-verification | Guideline P5 | Yes | — |
| P6 shared issue material is explicitly allocated to machines | Guideline P6 | Yes | Broader shared-material UI witness remains. |
| P7 subassembly transformation retains input/output/loss/scrap and cost | Guideline P7/B2-2 | No | Transformation batch and scrap recovery absent. |
| P8 different-item substitution needs TD technical approval | Guideline P8/B2-3 | No | Substitution workflow absent. |
| P10 project cannot close with unexplained WIP | Guideline P10 | Partial | FAT custody gate exists; complete project closure does not. |
| B2-4 physical closure may precede commercial cost closure | Guideline B2-4 | Partial | States exist across Job/FAT/Bill, but explicit combined closure projection remains. |
| B2-5 both Production and frozen Estimated variance baselines survive revision | Guideline B2-5 | Yes | — |
| B2-6 internal overrun notifies Production Manager, Stores, TD and MD only | Guideline B2-6 | No | No notification events are raised. |
| S1 ERP owns Service Ticket with complaints/visits/job cards | Guideline S1 | No | Service module absent. |
| S2 one installed-machine register covers SESS and other-brand machines | Guideline S2 | No | Service asset register absent. |
| S3 warranty/AMC/CAMC entitlement is effective-dated and snapshotted | Guideline S3 | No | Entitlement workflow absent. |
| S4 service material authority depends on entitlement/customer PO | Guideline S4 | Partial | Customer-facing excess TD decision exists; service entitlement does not. |
| S5 removed parts remain customer property absent explicit instruction | Guideline S5 | No | Service/customer-property workflow absent. |
| S6 replacement serial ancestry is permanent | Guideline S6 | No | Service replacement ancestry absent. |
| C1 customer property/returnables share typed custody core | Guideline C1 | Partial | Foundation core exists; operations absent. |
| C2 accessories are quantity/condition/photo lines, not individually serialized | Guideline C2 | No | Customer inward workflow absent. |
| C3 named recipient, timestamp and signature/OTP/POD/hash prove return | Guideline C3 | No | Delivery/return evidence workflow absent. |
| C4 every customer-property due extension needs TD and preserves original date | Guideline C4 | No | Extension workflow absent. |
| C5 returnables cannot force-close | Guideline C5 | No | Typed reconciliation absent. |
| C6 customer property transfer uses dual-acknowledged immutable handover | Guideline C6 | No | Handoff operation absent. |
| C7 inbound demo is custody property, never capitalized by possession | Guideline C7 | No | Demo inward workflow absent. |
| T2 every hand tool is an asset; only non-tool consumables are quantity stock | Guideline T2 | Partial | Asset schema exists; operation absent. |
| T3 kit membership is effective-dated | Guideline T3 | Partial | Foundation schema exists; no operational kit workflow found. |
| T4 custody approval differs for temporary, renewal, permanent/high-risk | Guideline T4 | No | Tool custody workflow absent. |
| T5 temporary tool duration is entered in days and snapshot on issue | Guideline T5/TOOL-2 | No | Tool issue workflow absent. |
| T6 annual calibration provider/interval governance | Guideline T6/CAL-1 | No | Provider/register/approval operation absent. |
| T7 expired calibration warns, does not block, and issue records warning | Guideline T7 | No | Issue warning evidence absent. |
| T8 tool loss assessment/write-off/recovery has Stores, technical, TD, HR/Finance roles | Guideline T8 | No | Incident and settlement workflow absent. |
| J1 jobwork reconciles inputs/outputs/loss/scrap/open balance | Guideline J1 | No | Jobwork module absent. |
| J2 tolerance is effective-dated per process with approval outside tolerance | Guideline J2 | No | Configuration/workflow absent; high-value concurrence remains a proposed detail. |
| J3 subcontract accepted bill cost allocates to outputs/projects | Guideline J3 | No | Subcontract costing absent. |
| J4 jobwork supports partial accept/return by lot/serial | Guideline J4 | No | Jobwork module absent. |
| J5 RMA has typed terminal outcomes | Guideline J5 | No | RMA module absent. |
| J6 invoice hold is line-level by default with evidence-based release | Guideline J6 | No | Payment-hold workflow absent. |
| J7 vendor-return dispatch has department and risk escalation approval | Guideline J7 | No | Vendor-return module absent; escalation role is still proposed. |
| X1-X7 transfer/intercompany lifecycle, ownership, GST evidence and scoped reconciliation | Guideline X1-X7 | No | Entire transfer/intercompany chain absent. |
| G4 lot/manufacture/expiry follows effective item/company policy | Guideline G4 | Yes | — |
| G6/B3-1 specified return classes require QC before AVAILABLE | Guideline G6/B3-1 | Partial | Policy structures exist; all future return types are not operational. |
| G7 rejected quantity stays pending return until governed outcome/concession | Guideline G7 | Partial | Concession works; return/replacement/repair/scrap outcomes incomplete. |
| V2 valuation-method change is prospective/effective-dated | Guideline V2 (proposed) | No | Not frozen and not implemented; current frozen method is FIFO. |
| V4 PO is provisional value before exact-match bill acceptance | Guideline V4 | Yes | Landed-charge timing conflict remains unresolved. |
| V6 free/warranty receipt is zero procurement cost plus memo value | Guideline V6 (proposed) | No | Await confirmation and workflow. |
| V7 Stores/Purchase maintain planning levels with manager approval | Guideline V7 (proposed) | Partial | Item settings exist; full maker/reviewer/approver lifecycle incomplete. |
| V8 slow/nonmoving/obsolete thresholds are governed | Guideline V8 (proposed) | No | Await confirmation and implementation. |
| D4 workflows use only necessary approval states | Guideline D4 | Yes | — |
| D5 delegation is explicit/effective/scope-bound; named-role rules have no fallback | Guideline D5 | Partial | Role assignments are dated; a general approver-delegation operation is not complete. |
| D8 retry uses idempotency, attempts, terminal queue and governed reconciliation | Guideline D8 (proposed) | Partial | Command idempotency/receipts exist; integration retry/terminal queue does not. |
| VP1-VP5 older vendor-performance proposals | Guideline VP1-VP5 (proposed) | Superseded | The later Vendor Rating Specification freezes different six-dimension weights and three-receipt rule; do not implement the older proposal. |
| SPARE-1 item default margin and reasoned offer-line override | Guideline SPARE-1 | No | Sales/spare offer module deferred. |
| Sales offer is distinct from Estimated BOM; supports finished-good, spare and service shapes; selling price never inferred from BOM. | Sales Offer Schema Notes | No | Sales is deferred; Job Order/CPO linkage is ready for a future immutable offer baseline. |

## Notification delivery today

Nothing is raised or delivered. `NotificationEvent`, `NotificationRecipient` and `NotificationDeliveryAttempt` exist, but source inspection found no insertion path, background delivery service or client read endpoint. Computed overdue flags are not durable notifications. Overdue returns, BOM excess, deviation escalation, calibration due, QC ageing, approvals, same-day non-return and unexplained FAT custody reach nobody today.

## Three MIR defects

- Version on both Draft edit and lifecycle transitions: **fixed and tested**; the PostgreSQL witness proves increments and stale rejection.
- EmployeeCode, EmployeeName and DepartmentCode in MaterialIssueRequestView: **fixed and tested**.
- BaseUomId in ItemSummary and ItemDetail: **fixed and tested**; the reachability manifest covers the MIR selector.

## Bottom line

The executable core covers sourcing through PO, inward/QC/put-away, MIR issue/return, bill/FIFO, Job Order, fitment/Actual BOM, FAT, BOM revisions and role governance. The largest paper-only areas are DC accountability, vendor rating, opening stock, adjustments/counts, tools/calibration, customer property, transfers/intercompany, landed/import/advance costs, notifications, reports and product deployment/recovery.
