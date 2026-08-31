# Purchase and Stores audit-readiness gap analysis

Date: 31 August 2026  
Scope: implemented `advance` ERP behavior plus the frozen Purchase/Stores design baseline  
Question tested: can SESS answer an ISO or statutory auditor from the ERP today, without reconstructing evidence in a spreadsheet?

## Executive verdict

**No. Purchase can demonstrate one controlled PR-to-issued-PO transaction, but Purchase and Stores together are not audit-ready.** The ERP can currently show a strong subset of purchasing authorization evidence: PR creation, two-level route resolution, RFQ/invitations, quotation revisions, technical verification, commercial comparison, PO approval/issue, immutable workflow history and audit rows. That end-to-end Purchase path has been exercised against disposable PostgreSQL.

The evidence chain stops at Gate Entry. Stores currently exposes only Gate Entry and warehouse/rack master operations. GRN, lot allocation, QC, concession, stock posting, MIR, issue, fitment, return, transfer, count, adjustment, valuation and accepted-vendor-bill allocation are schema/design assets, not operational evidence produced by live services. A table definition or frozen guideline is not an audit record.

Likely result if an auditor sampled Purchase and Stores today:

- selected Purchase authorization controls: capable of passing a transaction sample;
- ISO 9001 clause 8.4 vendor governance: likely major gap because evaluation, re-evaluation, suspension criteria and supplier corrective-action evidence are incomplete;
- ISO incoming inspection, traceability, preservation and nonconforming output controls: likely major because no operational Stores chain exists;
- statutory stock valuation, count, cut-off and GST input-credit trail: likely major because the necessary posted transactions and reports do not exist;
- segregation of duties across the complete procure-to-pay and inventory cycles: cannot be proved end to end.

Severity in this report is a readiness assessment, not a prediction of an auditor's formal classification. A finding depends on the sampled evidence, scope and compensating controls outside the ERP.

## Evidence boundary: what the ERP can produce today

| Area | Evidence available today | Important boundary |
|---|---|---|
| Purchase demand and authorization | PR header/lines, status history, approval history, route snapshot, resolved approver identity/role, audit rows | Strong transaction evidence; the shared approval engine enforces creator separation, sequence and configured employee/role resolution |
| Sourcing | RFQ, two vendor invitations, quotation revisions/attachments, technical verification, comparison, recommendation and approvals | Proven in the Purchase end-to-end PostgreSQL test; no periodic supplier-governance report |
| Purchase order | PO creation, submission, route approvals, issue, amendments/rejected revisions, status/approval/audit history | Proven for three approval bands; this is authorization evidence, not receipt/bill/payment evidence |
| Material follow-up | Status transitions and operational history | Useful for expediting; not proof of receipt, custody or ownership |
| Customer-order intake | Company-scoped Customer PO intake revisions and optional same-company PR link | Explicitly not canonical Sales; final offer-versus-actual is impossible until offer/BOM/bill allocation exists |
| Vendor and item masters | Vendor records, company relationships, item-vendor links, category qualification data and attachments | No approved-vendor-list snapshot, evaluation period, scorecard or governed suspension decision |
| Stores inward | PO-linked Gate Entry create/edit/list/detail/finalize with history and audit | Gate Entry proves arrival at the gate only; it does not prove quantity accepted, title, QC or stock |
| Warehouse configuration | Warehouses, rack bins and condition-location configuration | Configuration only; not proof of balance, storage compliance or stock movement |
| Inventory ledger and downstream Stores | Tables and controlled-posting design exist | No GRN/QC/posting/issue endpoint produces usable operational records today |
| Audit access | Protected, company-scoped audit history API | No evidence-pack export, control-total report, retention catalogue or auditor-ready cross-document dossier |

## Classic traceability test: one delivered chamber

Auditor request: “Show every component in chamber X, its GRN, vendor, accepted bill, QC inspection and every deviation approval.”

| Link | Can ERP answer today? | Exact break |
|---|---|---|
| Delivered chamber -> canonical customer order | No | Customer PO is an intake register; canonical Sales order, contract review and delivered-machine linkage are not implemented |
| Customer order -> frozen offer/BOM | No | Offer revision, frozen Estimated BOM and Production BOM/machine pinning are design only |
| Customer PO intake -> PR | Partly | Nullable same-company PR link now exists, but it is not yet the canonical customer-order/BOM chain |
| PR -> RFQ -> quotation -> comparison -> supplier PO | Yes | Transaction path, approval identity, status history and audit can be shown |
| Supplier PO -> gate arrival | Yes | Finalized Gate Entry can be shown |
| Gate arrival -> GRN line and ERP lot | No | GRN service/endpoints and GRN-line lot allocation are not implemented |
| GRN lot -> QC samples/results | No | QC table design exists, but no operational inspection workflow writes the evidence |
| Rejection -> return/concession | No | Exact lot/serial TD concession and provenance-carrying movement are frozen design only |
| QC acceptance -> put-away/stock movement | No | No stock-posting endpoint invokes the controlled posting path for receipt/QC |
| Available stock -> approved MIR -> issue | No | Job Order, MIR approval and issue services are not implemented |
| Issue -> confirmed fitment -> machine Actual BOM | No | Fitment, reversal/re-verification, component ancestry and Actual BOM generation are not implemented |
| Physical component -> accepted vendor bill allocation | No | Vendor bill, PO/bill match and allocation do not exist |
| Accepted bill -> payment/GST credit | No | Accounts invoice, payment and GST input-credit chain are outside current implementation |
| Concession component -> chamber three years later | No | The planned `InventoryProvenanceLayerId` chain is not in the operational ledger/API |

**Conclusion:** the chain is defensible only through supplier PO issue. The first physical-evidence break is GRN; the final commercial-evidence break is accepted vendor bill allocation. The classic traceability test fails today.

## ISO 9001 clause 8.4 - purchasing and external providers

| Gap / auditor question | What ERP can produce today | Missing evidence/control | Work type | Likely severity |
|---|---|---|---|---|
| Show the approved vendor list and selection criteria | Active/approved vendor master rows and category qualification records | Versioned approval-list snapshot; documented selection criteria; approver/reason/evidence; effective suspension/reactivation | Schema + workflow + report | Major |
| Show initial vendor evaluation | Vendor identity, MSME/tax/contact data and attachments | Evaluation questionnaire, criteria scores, evidence, decision and effective period | Schema + workflow | Major |
| Show periodic re-evaluation | Nothing operational | Evaluation periods, immutable scorecards, sample threshold, weighted metrics, review approval and overdue review report | Schema + workflow + report | Major |
| Prove purchasing information specified requirements adequately | PR lines, RFQ, quotation, technical verification, comparison and PO revisions | Controlled specification/drawing revision link and a completeness report tying every PO line to approved technical/commercial requirements | Application + report; some document schema | Minor, potentially major for safety/defence items |
| Prove purchased product was verified before use | Gate Entry only | GRN, lot/serial capture, policy snapshot, QC measurements, inspector, disposition and controlled release to AVAILABLE | Schema already designed; service + DB posting + report | Major |
| Show vendor nonconformity and action taken | No governed record | Supplier NCR, containment, return/replacement, concession, root cause, corrective action, effectiveness check and closure | Schema + workflow + report | Major |
| Show criteria and authority for vendor suspension/removal | Vendor status fields can be displayed | Versioned criteria, initiating evidence, TD/MD decision, effective dates, open-PO impact and reactivation workflow | Schema + workflow | Major |
| Show delivery, quality and response trends | Material follow-up rows and individual technical verification may be queried | Period scorecards, denominator/sample disclosure, trend and approved management snapshot | Schema + report | Minor until used for supplier control; then major |
| Prove only qualified vendors were invited for the category | End-to-end Purchase test exercises category qualification | Auditor-facing exception/denial report and historical qualification snapshot attached to each invitation | Application + report | Minor |
| Show changes to vendor commercial/KYC data | Generic audit rows and attachments exist | Dedicated sensitive-master change dossier, four-eyes control for bank/KYC changes and access/export evidence | Workflow + report | Major for payment fraud control; ISO minor |

## ISO 9001 Stores controls - clauses 7.1.5, 8.5.2, 8.5.4 and 8.7

| Gap / auditor question | What ERP can produce today | Missing evidence/control | Work type | Likely severity |
|---|---|---|---|---|
| Identify and trace every stocked item | Item master, warehouse/rack configuration | Posted GRN lot/serial, provenance layer, put-away, transfer, issue, return, fitment and balance projection | Service + controlled DB posting + report | Major |
| Show preservation/storage conditions | Condition-location configuration | Required-condition policy by Item, actual environmental checks/readings, excursions, shelf-life status and disposition | Schema + workflow + report | Major for controlled items |
| Show incoming inspection | No operational QC evidence | Inspection policy snapshot, lot allocation, sample plan, parameter/result, measured value, inspector, accept/reject and immutable revision | Service + report | Major |
| Show segregation of nonconforming material | PENDING_RETURNABLE_DC/QC_HOLD locations are configured | Posted custody movement into segregation, physical location evidence, status aging and blocked use | Service + DB enforcement + report | Major |
| Show disposition and authority for rejected material | Frozen design defines return/repair/scrap/concession | Operational decisions, approvals, exact quantity/lot/serial and posting evidence | Schema/design exists; service + DB enforcement | Major |
| Show use-as-is concessions | Frozen TD-only rule is documented | Concession transaction, failed parameter, measured value, reason, intended use, TD identity and life-long provenance | Schema + workflow + report | Major |
| Show calibration status of measuring equipment | Item type/configuration only | Tool asset register, provider/certificate, due date, warning evidence, issue-while-expired record and impact assessment | Schema + workflow + report | Major for inspection equipment |
| What was done when equipment was found out of calibration? | Nothing | Affected-use search, product/inspection impact assessment, notification, reinspection/recall decision and closure | Schema + workflow + report | Major |
| Show shelf-life/expiry control | Lot policy is frozen in design | Posted manufacture/expiry values, FEFO warning where relevant, expired-stock block/disposition and report | Service + DB control + report | Major for shelf-life items |
| Show customer property preservation | Ownership/custody rules are frozen | Receipt/accessory list/photos, condition, location/custodian, handovers, due extensions, return acknowledgement and loss case | Schema + workflow + report | Major when customer property is in scope |
| Show tools/returnables overdue | Tables/design only | Individual asset/custody transactions, due dates, acknowledgements, escalation and closure reconciliation | Service + report | Minor, potentially major for calibrated/customer assets |

## Statutory and financial audit

| Gap / auditor question | What ERP can produce today | Missing evidence/control | Work type | Likely severity |
|---|---|---|---|---|
| State and prove consistent stock valuation | Frozen design says strict FIFO | Operational FIFO cost layers, posted valuation legs, period roll-forward and reconciliation to financial ledger | Schema + posting engine + report | Major |
| Reconcile stock subledger to control account | No posted Stores subledger | Opening balance, receipts/issues/adjustments/closing balance by period and GL interface/control totals | Workflow + integration + report | Major |
| Show physical-verification records | Nothing operational | Inventory periods, blind count sheets, scoped freezes, independent recount, approved variance and posting | Schema + workflow + report | Major |
| Prove period cut-off: goods received not billed | Gate Entry is not receipt | GRN posting time, title/custody state, accepted bill status and GRNI report as-of timestamp | Schema + workflow + report | Major |
| Prove goods billed not received | No vendor bill module | Accepted bill/PO/GRN match, exception queue and period report | Schema + workflow + report | Major |
| Show goods in transit and ownership | Material follow-up only | Dispatch/in-transit/acceptance events, incoterm/title basis and as-of ownership valuation | Schema + workflow + report | Major |
| Identify slow/non-moving/obsolete stock and provision | Frozen proposed aging thresholds | Posted movement history, approved classification, aging snapshot, provision basis and Finance approval | Schema + report + policy | Major |
| Show scrap generation and sale proceeds | Frozen split between write-off and disposal | Scrap lot/weight, declarer, approvals, buyer selection, invoice, cleared receipt, dispatch and accounting link | Schema + workflow + report | Major |
| Trace GST input credit from bill to GRN to payment | Purchase tax calculation exists; no bill/receipt/payment chain | Vendor invoice, GSTIN/invoice identity, PO/GRN match, accepted tax, ITC eligibility/reconciliation and payment | Accounts schema + integration + report | Major |
| Show e-way bill evidence | Attachment framework exists | Typed e-way document, applicability decision, number/validity, movement link and expiry/exception control | Schema + workflow + report | Major for applicable movements |
| Show insurance cover and damaged-stock claims | PO insurance charge can be recorded | Policy/coverage register, insured locations/values, incident, survey, claim, recovery and accounting | Schema + workflow + report | Minor; major if material uninsured exposure exists |
| Prove accepted-bill Actual BOM cost | Design is frozen; Customer PO can link to PR | Accepted bill allocation through PO/GRN/provenance/fitment to machine Actual BOM | Schema + workflow + report | Major for offer-versus-actual objective |
| Explain purchase price variance | Frozen rule says accepted bill must match PO, so no PPV | Exception report proving every accepted bill matched the effective PO revision; separate landed-cost allocation evidence | Application + report after bill module | Minor once implemented; unavailable today |

## Segregation of duties

### Procure-to-pay and issue cycle

| Activity | Intended role/control | Enforced today? |
|---|---|---|
| Raise | Requesting employee/department; creator identity immutable | Yes for PR |
| Approve | Department-mapped level 1, then configured TD/MD by value; creator excluded; level 2 differs from level 1 | Yes for PR/comparison/PO approval |
| Receive | Stores employee against PO/Gate Entry | No operational GRN service |
| Inspect | Effective QC_MANAGER employee with measurement evidence | No operational QC service |
| Issue | Stores against approved MIR and available balance | No Job Order/MIR/issue service |
| Pay | Accounts after accepted bill and match | No Accounts bill/payment workflow |

The ERP prevents one person from raising and approving the Purchase document, but it cannot prove separation across receive, inspect, issue and pay because those actions are absent. Absence does not establish a control; it pushes the activity outside the system. **Likely severity: major.**

### Scrap cycle

Intended separation is declarer/technical disposition, Stores buyer selection, MD disposal approval, Accounts invoice/payment receipt and Stores dispatch. None of this cycle is operational. The ERP cannot prove that the declarer did not select the buyer, approve disposal or receive payment. **Likely severity: major.**

### Count and adjustment cycle

The frozen design separates recorder, independent recounter and variance approver, with special value/risk bands and controlled posting. There is no count or adjustment workflow today. **Likely severity: major.**

### Additional segregation concerns

- Vendor bank/KYC changes lack a dedicated maker-checker workflow and evidence pack.
- Customer/vendor attachment reads are permission-controlled in places, but there is no consolidated sensitive-data access report.
- Cross-company operational scoping exists, but intercompany sale/purchase reconciliation is not implemented.
- Effective role resolution and OIDC/ERP authorization separation are strong foundations; production database-principal separation is deliberately deferred and remains a deployment prerequisite.

## Records and document control

| Gap / auditor question | Current answer | Missing | Work type | Likely severity |
|---|---|---|---|---|
| What is the retention period for each record type? | Audit logs have a ten-year control; import row PII has 90-day purge; Service design says at least ten years | Approved retention schedule covering every Purchase/Stores record, attachments, tax evidence, customer/defence obligations and legal hold | Policy + configuration + purge/report | Major |
| Show controlled procedures/specifications and revisions | Some attachments and Customer PO revisions are immutable/superseding | General controlled-document register, owner, revision, approval/effective dates, distribution and obsolete-copy control | Schema + workflow | Major for QMS documents |
| Who can amend or delete accepted records? | Purchase histories and several DB functions/triggers are immutable; corrections use revisions | Complete record-class matrix and automated proof that every accepted Stores/Accounts record uses reversal/supersession | Application + DB enforcement + report | Minor now; major if an accepted record is mutable |
| Export an audit evidence pack | Individual APIs and audit history can be queried | Read-only dossier export by document/machine/vendor/period with hashes, histories, attachments and control totals | Reporting | Observation to minor; operationally important |
| Prove attachment integrity | File metadata/hashes exist in the design and some modules | Uniform hash verification, supersession chain, sensitivity label and access/export log across all attachment types | Application + report | Minor |
| Prove completeness of numbered documents | Number sequences exist | Sequence-gap/cancelled/voided-document report with reason and authorization | Reporting | Minor |
| Prove delayed/backdated entries | Some audit timestamps exist | Physical-event time versus entry time, reason/evidence, open-period enforcement and backdate approval report across all Stores documents | Schema + workflow + report | Major for cut-off |

## Management-review inputs

| Required input | Available today | Gap | Likely severity |
|---|---|---|---|
| Vendor performance trends | Individual sourcing transactions | No period scorecard/trend/suspension dashboard | Minor ISO finding if management review lacks alternate evidence |
| Nonconformity and corrective-action trends | No supplier/Stores NCR workflow | No NCR/CAPA trend, recurrence, overdue action or effectiveness report | Major if no other QMS evidence |
| Stock accuracy trends | No counts | No count accuracy, variance by item/location/cause or repeat discrepancy trend | Major |
| Overdue returnables and tools | No operational custody issue/return chain | No aging/escalation dashboard | Minor |
| Purchase lead-time and delivery performance | Material follow-up can supply part of the chronology | No governed KPI snapshot and exception denominator | Observation to minor |
| Offer-versus-actual margin | Customer PO intake and PR link only | No frozen offer BOM, accepted bill allocation or Actual BOM | Major against the ERP's primary business objective |

## Additional questions an auditor is likely to ask

| Auditor question | Readiness today | Required response |
|---|---|---|
| Who reviewed user access and conflicting roles this quarter? | No periodic access-certification evidence pack | Effective assignment export, conflicts, reviewer, removals and exceptions |
| Can administrators alter audit evidence? | Durable audit protections exist in database design; production principals are not yet provisioned | Provision principals, verify ACLs, run privileged-access review and retain results |
| Has backup restore been tested? | Outside Purchase/Stores evidence | Scheduled restore test, RPO/RTO result and signed exception closure |
| How were opening balances migrated and approved? | No opening-stock ceremony executed | Three-actor opening batch, source reconciliation, hash/control totals and sign-off |
| Are interface failures complete and reconciled? | Transactional outbox is design only for future Finance/HR integrations | Outbox attempts, terminal queue, reconciliation owner and completeness report |
| Can master-data changes rewrite historical meaning? | Purchase workflow snapshots protect approvals; some masters have audit | Effective-dated/snapshotted QC, valuation, tolerance, qualification and routing policies everywhere they affect a posted transaction |
| How is cyber/time integrity controlled? | Authentication/audit foundations exist | Production OIDC ceremony, database principals, time-sync monitoring, device/offline event controls and access-review evidence |
| Show legal-entity isolation | Company scoping is broadly implemented | Intercompany commercial correlation, privileged read-only reconciliation and negative access tests for all new Stores paths |

## Remediation order

### P0 - required before claiming Stores is operational or audit-ready

1. Implement PO-linked GRN, lot/serial allocation, QC and controlled receipt/QC posting.
2. Implement rejected-material return/repair/scrap and exact TD concession with life-long provenance.
3. Implement Job Order, approved MIR, issue/return, fitment and reversal/re-verification.
4. Implement accepted vendor bill, PO/GRN match, GST evidence, payment handoff and accepted-bill allocation to Actual BOM.
5. Implement FIFO cost layers, inventory periods, opening stock, physical counts, scoped freezes, adjustments and subledger roll-forward.
6. Implement supplier evaluation/re-evaluation, NCR/CAPA and governed suspension/reactivation.
7. Implement the enforceable segregation chains for receipt/inspection/issue/pay, scrap and counts.

### P1 - required before the audit evidence can be produced without spreadsheets

1. Delivered-machine component ancestry dossier.
2. Approved vendor list and period evaluation pack.
3. Incoming-inspection, nonconformance, concession and corrective-action pack.
4. Stock valuation, movement roll-forward, count/variance and cut-off reports.
5. GRNI, billed-not-received, in-transit, aging/obsolescence, scrap and GST reports.
6. Access/segregation review, sensitive-master changes, sequence gaps and attachment-integrity reports.
7. Approved retention schedule and automated legal-hold/purge evidence.

### P2 - management effectiveness and optimization

1. Vendor, NCR/CAPA, stock accuracy, overdue custody and Purchase lead-time trends.
2. Insurance/claim register and policy-coverage reconciliation.
3. Auditor dossier export with stable hashes and read-only evidence manifest.
4. Deferred mobile/offline and customer portal, using the schema-reserved event/device/conflict/access dimensions.

## Bottom line

The design now anticipates most of the evidence an auditor will request, including strict FIFO, exact provenance, concession ancestry, customer property, tools, intercompany isolation, counts and immutable documents. That is valuable because it avoids rebuilding the schema later. It is not audit evidence yet.

Today SESS can defend **how a purchase was requested, sourced, approved and issued as a PO**. It cannot defend **what was physically received, inspected, accepted, stored, issued, fitted, billed, paid or counted**, nor can it produce supplier-performance and stock-control trends without external records. Until the P0 workflows and P1 evidence projections exist, an auditor who follows one chamber or one stock balance end to end will force the team back to spreadsheets and paper.