# SESS NexaERP: go-live gap audit, 26 September 2026

> **Updated 26 September evening. The body below is kept as written that afternoon.** Later
> decisions of the Technical Director change parts of it:
>
> - **Go-live date:** the 10 October go-live is cancelled; the date now comes from the plan.
>   Build 1 (28 Sep) is a UAT/demo build for the server only, and the backend freeze is lifted
>   (full acceptance per item still applies).
> - **R5 is built:** stock adjustment and inventory period screens were built by ILAMPARUTHI and
>   proven on `feature/frontend` 937ee28. They were not seen by this audit, which read 667c248.
> - **Dashboards:** merged into `feature/frontend` with live APIs, and **in** for go-live. Spend is
>   labelled "incl. GST" until the backend excludes recoverable GST.
> - **R9 (migration 131) is committed:** `19c3a94`.
> - **Backend batch approved by name:** R1-R4, R10, the PO and DC print endpoints, the Print
>   grant, the GST tax-context endpoint, opening-stock Option A, and G1/G2. Frontend: a machine DC
>   screen is built; its audit is R11.

Read-only audit, requested by the Technical Director on 26 September 2026. **No code was changed
and no run was started.** Written outside the repository while migration 131's acceptance cycle
runs, to be moved into `docs/` only on the Technical Director's OK.

## What was read

| Part | Ref | Note |
|---|---|---|
| Backend | `main` working tree = `b81df55` plus the uncommitted migration 131 (`20260926090000_DevelopmentLoginsOffByDefault`) | Includes `b7f64a9` (DC #34/#35) and `040a24c` (plan) |
| Frontend | `origin/feature/frontend` = **`667c248`** (26 Sep 10:58) | **`f67c00a` was not on this machine.** Fetching was off-limits during the cycle. Anything it adds is not counted here, and it is checked after the cycle |
| Dashboards | `origin/feature/dashboards` = `4804586` | |
| Wrappers and package | `tools/setup`, `tools/deployment` (builder `b81df55`) | |

- **Method:** six read-only research passes (A-I), then every claim that decides a RED was
  checked again by hand against the code. Those re-checks are marked **(verified)**.
- **Status values:** PRESENT, PARTIAL, MISSING, BROKEN. "Cannot prove" means the code does not
  show it either way.
- **Rating:**
  - **RED** = go-live blocker: stock accuracy, money, GST/legal, or ISO mandatory on day 1, with
    no safe manual workaround.
  - **AMBER** = can be run safely by hand (a register, Excel or Tally) from go-live.
  - **GREEN** = backlog after go-live.
- File paths are relative to `target-dotnet/`. "FE" means `origin/feature/frontend:src/SESS.NexaERP.Web/src/`.

## Headline

1. **The core flow works end to end:** PR → RFQ → quotation → comparison → PO → gate entry →
   GRN → QC → MIR → issue/return → fitment → Actual BOM → FAT → dossier, plus vendor bill,
   advance and payment, and the opening-stock ceremony. Self-approval is refused in code for PR,
   PO, GST rules, stock adjustment, opening stock, MIR and job orders.
2. **Nothing can be printed.**
   - There is no PDF library in any project, and no `window.print` or print CSS on either
     frontend branch (verified).
   - Company records hold no GSTIN, address or state (verified: `Company` has Code, LegalName,
     EntityType only).
   - **A PO cannot be sent to a vendor from the ERP.**
3. **Four defects must be fixed before go-live, all small:**
   - vendor reactivate sets the wrong status (verified);
   - the quotation GST state codes default to '33' and are never validated (verified);
   - one Accounts Manager can create and accept the same vendor bill (verified);
   - QC writes no audit row (verified).
4. **Two core Stores screens do not exist, although their backend is complete:**
   - the machine DC screen (already planned for build 2);
   - stock adjustment and inventory period (verified: no frontend reference).

   Without the second one, Stores cannot correct a count or write off a loss.
5. **Many real SESS activities have no ERP path and must run on registers from day 1:**
   - return to vendor and debit note;
   - returnable gate pass for tools;
   - job work and ITC-04;
   - service and AMC tickets;
   - refrigerant cylinders;
   - transfers between bins;
   - customer-supplied material;
   - calibration, CAPA and document control;
   - GST register, TDS and MSME tracking.

   These are AMBER only if the registers exist on 8 October.
6. **Cut-over from BroPOS has no path for open documents:** open POs, pending GRN/QC, advances,
   unpaid bills. A PO can only be created through the full PR→RFQ→quotation→comparison chain, so
   every BroPOS PO still open on 7 October must be re-raised that way before its goods can be
   received. **This needs a decision, and possibly code.**

## 1. Item table

**A. Purchase**

| # | Item | Status | Evidence | Rating |
|---|---|---|---|---|
| 1 | Requisition sources | PARTIAL | Manual PR end to end: `PurchaseRequisitionEndpoints.cs:120`, FE `features/purchase/PurchaseRequisition*`. No PR from BOM shortfall; nothing reads `Item.ReorderLevel` | GREEN |
| 2 | PR approval, no self-approval | PRESENT | Refused in code (`EfPurchaseApprovalWorkflowService.cs:112`, `EfPurchaseRequisitionWorkflowService.cs:135`) and in SQL (`purchase_approval_self_approval`); tests `PurchaseApprovalEnginePart3Tests` | GREEN |
| 3 | Vendor master | PARTIAL + **BROKEN** | **Present:** GSTIN/PAN regex server-side (`MasterEndpointHelpers.cs:256-260`), state code, MSME status + number, bank data (redacted by permission), commercial verification, hold/blacklist; FE `features/vendors/*`. **BROKEN (verified):** `reactivate` sets `VendorStatus="Approved"` (`MasterEndpoints.cs:211`), but approve sets `"Active"` and RFQ/PO eligibility needs `"Active"` (`VendorQualification.cs:48`, SQL `Rev869BDatabaseSafetySql.cs:225`). A held-then-reactivated vendor can never get an RFQ or PO again. **Also missing:** no from-state guard on lifecycle actions (a blacklisted vendor can be approved); GSTIN↔PAN and GSTIN↔state checks are FE only; no Udyam class | **RED** (reactivate) |
| 4 | Approved vendors per category, qualification, re-evaluation, rating | PARTIAL | Qualification per category with three distinct actors, enforced at RFQ invite (code + SQL). Rating evidence and manual assessment are backend only. **No FE screen** for any of them; no re-evaluation cycle; rating is backlog A4 | AMBER (qualifications by setup wrapper; rating on the existing register) |
| 5 | Item master | PARTIAL | HSN/SAC, GST %, UOM, category, make, model, spec text, serial/batch/shelf-life flags, types RAW_MATERIAL…NON_STOCK. **Missing:** CAPITAL type, alternates, drawing file (text reference only). UOM conversion has no screen and no approval | AMBER |
| 6 | RFQ, quotations, technical verification, comparison, price history | PRESENT (price history PARTIAL) | Full chain with screens (`features/purchase/Rfq*`, `Quotation*`, `Comparison*`). `Item.LastPurchaseRate` shown on the item page; not shown on the comparison screen; no history list. **No direct PO:** every PO needs the full chain | GREEN (direct PO: see item 38) |
| 7 | PO controls | PARTIAL | Value bands MANAGER/TD/MD, submitter≠approver; terms as text; P&F, freight, insurance, other charges, discount; amend with revision number (**terms only**: qty, rate, lines and dates are copied); cancel TD/MD. **No short-close. No delivery schedule** (one promised date per line). Cancel after a partial GRN: cannot prove | AMBER (cancel and re-raise; register of short-closed POs) |
| 8 | PO print/PDF per company | **MISSING** | Verified: no PDF library, no print code, no letterhead data on `Company` | **RED** |
| 9 | Service and capital POs | PARTIAL / cannot prove | `SERVICE_ITEM`/`NON_STOCK` are constants only. A vendor bill can only be made from a GRN (`VendorBillEndpoints.cs:15`), so calibration, transport and AMC need a fake receipt. No capital type | AMBER (Tally-only services until built; **TD decision**) |
| 10 | Import purchase | MISSING | Currency must be INR, `ExchangeRate == 1` (`Rev869BPurchaseTransactions.cs:638`); no Bill of Entry | AMBER if SESS imports (manual + Tally), else out of scope; **TD decision** |
| 11 | Vendor advance and adjustment | PRESENT | Advance against PO, auto-applied oldest first on bill acceptance, reversal restores (`VendorAdvancePaymentSql.cs:190-225`); FE `VendorPaymentsPage.tsx` | GREEN |
| 12 | Pending-PO follow-up | PARTIAL | `dashboards/purchase/open-orders` endpoint (FE only on the unmerged dashboards branch); material follow-up API has no screen; reports `purchase-register`, `grni`, `billed-not-received` via the generic report viewer | GREEN (reports usable) |
| 13 | Job work / subcontract, ITC-04 | MISSING | No material-out challan, receive-back or ITC-04. Machine DC `JOB_WORK` is for whole machines only | AMBER (job-work register + ITC-04 from it) |

**B. Stores**

| # | Item | Status | Evidence | Rating |
|---|---|---|---|---|
| 14 | Gate entry | PARTIAL | End to end (`StoresGateEntryEndpoints.cs`, FE `GateEntry*`): vehicle, mode, vendor DC no. **No e-way bill field anywhere.** Vendor invoice no. is taken at GRN, not gate. PO-only: no non-PO inward | AMBER (e-way bill no. on the gate register) |
| 15 | GRN partial / excess / short / invoice mismatch | PARTIAL | Partial supported; **excess refused outright**; short via QC discrepancy; bill mismatch is a hard block (see 33) | AMBER |
| 16 | QC | PARTIAL | **Present:** policy per item/category, sampling, accept/partial/reject/discrepancy, TD concession, rejected stock to PENDING_RETURNABLE_DC; FE `features/qc/*`. **Missing policy fails closed** (`EfQcWorkflowService.cs:150`): FAB with no approved policy can never be accepted. **Return to vendor:** schema only, no service writes `delivery_challans` (verified). **No debit note. Replacement:** schema only | AMBER (RTV and debit note on a register + Tally; returned qty written off by a stock adjustment, see 25). **Setup blocker:** QC policies for ELE/FAB/REF |
| 17 | Put-away, stock by location, transfer | PARTIAL | Put-away at QC accept; stock-by-location through the `stock-balance` report. **No transfer between bins or warehouses** (legs exist, no endpoint). No warehouse/rack/condition/route screens | AMBER (transfer register; **TD: how often?**) |
| 18 | MIR → issue by purpose, returns | PARTIAL | Purposes and situations in `CK_mir_lifecycle`/`CK_mir_situation`; FE MIR/issue/return. **No service/AMC/warranty ticket:** service needs an open job order, or CONSUMABLE_OFFICE. **`CK_material_issues_status` forces `ReturnDueAt = IssuedAt + 1 day` (verified),** so every multi-day site trip counts as late at FAT readiness | AMBER |
| 19 | Returnable gate pass for tools and spares | MISSING | Only the machine DC exists; material DC is backlog 5-8 d | AMBER (manual returnable DC book, GST rule 55) |
| 20 | Refrigerant by kg, cylinders, oil | PARTIAL | kg quantities work. **No cylinder or deposit concept.** Stock ledger `QuantityIn/Out` is **numeric(18,3)** while documents are (24,6): a quantity finer than 0.001 is not proven safe | AMBER (cylinder register; UOM precision ≤ 3, **TD rule**) |
| 21 | Serial traceability → dossier | PRESENT | GRN serial → issue by serial → fitment → Actual BOM → `machine-dossier` report; warranty expiry on the GRN line. Dossier shows IDs; component serial text not proven. No reverse lookup | GREEN |
| 22 | Batch / shelf-life / expiry | PARTIAL | Lot, manufacture and expiry captured; expiry optional even for shelf-life items; **expired stock can be issued; no expiry report** | AMBER (expiry register for oils, adhesives, paints) |
| 23 | Reservation for a job | PARTIAL | Reservation keyed to the PR, not the job; **issue never reads reservations (verified)** | AMBER (reservations advisory only; do not rely on them) |
| 24 | Customer-supplied material | MISSING | Ownership schema only; no endpoint or screen | AMBER (free-issue register, outside ERP stock) |
| 25 | Stock take; adjustment with approval and reason | PARTIAL | Backend complete and tested (reasons, value bands, TD + Accounts on write-off, posting `20260920210000`). **No FE screen for adjustment or for the inventory period it needs (verified).** No count-sheet / freeze | **RED** (screens) |
| 26 | FIFO, ledger, ageing, non-moving, ABC | PARTIAL | FIFO valuation with age buckets, stock balance, roll-forward via the generic report viewer + Excel. No non-moving report, no ABC | GREEN |
| 27 | Scrap and scrap sale | PARTIAL | Write-off = DAMAGE_LOSS adjustment (no screen, see 25); no scrap sale document | AMBER (scrap sale in Tally) |
| 28 | Min/max/reorder alerts | MISSING | `ReorderLevel` stored, never read | GREEN |
| 29 | Barcode / labels | MISSING (scan-in only) | Keyboard-wedge scanning on GRN/issue/return; nothing prints labels | AMBER (labels from Excel/label software) |
| 30 | Opening stock ceremony | PRESENT | Template v2 → import → Accounts value → TD authorise; once per company (unique index + refusal after first movement); screens; tests. The ceremony itself is a field witness for 5-6 Oct | GREEN (field step pending) |

**C. Production and dispatch**

| # | Item | Status | Evidence | Rating |
|---|---|---|---|---|
| 31 | Customer PO → job → EBOM → Actual BOM → FAT → machine DC → dossier | PARTIAL | Every link has an endpoint and a screen **except the machine DC** (verified: no FE reference). FAT is a readiness/reconciliation gate, not a test record. The dossier needs a signed DC. **"DC gaps a/b/c": no document uses those labels.** The nearest are: #34/#35 (fixed `b7f64a9`, reaches the server with the next package); #36 (closed); and the DC scope gaps (no correction/return/re-dispatch, no material DC, no intercompany DC path) | **RED** (DC screen, in the plan for build 2) |
| 32 | Intercompany movement + GST invoice | PARTIAL | Routes, purchases and invoice endpoints exist; **#25: unreachable on a fresh database** (no `company_sites` / customer relationship API); no screen; the invoice is recorded, not generated | AMBER (separate PO/GRN in each company; **TD: needed on day 1?**) |

**D. Accounts boundary**

| # | Item | Status | Evidence | Rating |
|---|---|---|---|---|
| 33 | 3-way match, variance | PARTIAL | Bill from one GRN; qty must equal received qty; **any price difference, even ₹0.01, is refused**: "Reject the bill or revise the PO". The PO amendment cannot change the rate. No tolerance; no multi-GRN bill. QC-rejected qty on the bill: cannot prove | AMBER (Accounts keys PO values in the ERP, books the actual invoice in Tally; **TD: tolerance?**) |
| 34 | Bank advice, MSME 45 days, TDS, RCM | PARTIAL / MISSING | Bank advice = upload of the bank's PDF; payment against accepted bills. **MSME status is never used; no 45-day logic** (due date = acceptance + "NET n" parsed from terms). **No TDS** anywhere. **RCM (verified):** the calculator adds CGST/SGST/IGST to the payable whatever `IsReverseCharge` says (`Domain/Purchase/Rev869BPurchaseTransactions.cs:171-185`) | AMBER **on two conditions:** RCM GST rules set up at zero rate with the RCM liability in Tally; the MSME 45-day watch on an Excel aging. **TD decision** |
| 35 | ERP vs Tally boundary | MISSING (no interface) | No Tally/XML/CSV voucher export; generic report Excel only; no GST columns in any report | AMBER (documented boundary; **TD decision**) |
| 36 | GST purchase register, outstanding, aging | PARTIAL | `purchase-register` is a PR→PO→GRN→bill quantity register, not GST-wise; payables and vendor positions on screen; **no aging buckets; no Excel for payables** | AMBER (GST register and aging from Tally) |
| 37 | FY numbering rollover | PRESENT | `PREFIX-26-27-000001`, FY Apr-Mar derived automatically. Edges: UTC used, so 00:00-05:29 IST on 1 April takes the old FY; QC/concession numbers are not FY-based; the PO sequence has no advisory lock | GREEN (before 31 Mar 2027) |

**E. Cut-over**

| # | Item | Status | Evidence | Rating |
|---|---|---|---|---|
| 38 | BroPOS cut-over | PARTIAL | **Paths exist for:** items (owner SQL script, 1,368), vendors (screens, per runbook §11), customers, warehouses/racks (wrapper), opening stock (ceremony). **No path for:** open POs (no PO import, no direct PO), pending GRN/QC (GRN needs an ERP PO; nothing before the ceremonies), vendor advances (need an ERP PO), unpaid bills (need an ERP GRN). The runbook describes none of these | **RED: decision required** (see question 1). Code only if the open-PO count is large |

**F. ISO 9001:2015**

| # | Clause | Status | Evidence | Rating |
|---|---|---|---|---|
| 39 | 7.1.5 calibration of own instruments | MISSING | No register, due dates or certificates; the dashboard "Calibration due" has no source | AMBER (existing calibration register) |
| 40 | 7.5 documented information | PARTIAL | Immutable `audit_logs` + about 160 guard triggers; no delete endpoints; vendor attachments; revised drawings/BOMs. **No control of the manual, SOPs or forms;** hard deletes for item-vendor links and item images | AMBER (existing document control) |
| 41 | 8.4 supplier control; purchase information | PARTIAL | Selection is enforced (active + approved + commercially verified + qualified per category). No re-evaluation cycle; rating not wired; **PO line has no quality/spec/test-certificate requirement** (spec only on RFQ/PR lines) | AMBER (quality requirements written in the PO terms text; re-evaluation register) |
| 42 | 8.5.2 traceability; 8.5.4 preservation | PRESENT / PARTIAL | Serial, lot and provenance genealogy; condition states. No expiry block; no storage conditions | AMBER |
| 43 | 8.6 release; 8.7 nonconforming + concession | PRESENT / PARTIAL | QC records, TD concession with creator≠decider, FAT gate. **QC writes no `audit_logs` row (verified: no audit calls in `EfQcWorkflowService*`).** No NCR beyond QC | AMBER (records carry actor and time; fix the audit gap, see RED batch) |
| 44 | 9.1 supplier performance; 10.2 CAPA/SCAR | PARTIAL / MISSING | Rating foundation only; **no CAPA/SCAR anywhere** | AMBER (existing CAPA register) |
| 45 | Audit trail on every save | PARTIAL | `EfAuditWriter` with before/after; the table is immutable; most modules call it. **Gaps:** QC (none); masters audit written in a second save (not atomic); many writes pass `before = null`; no audit viewer screen | AMBER (fix QC in the RED batch; see 43) |

**G. Controls and build quality**

| # | Item | Status | Evidence | Rating |
|---|---|---|---|---|
| 46 | Segregation of duties | PARTIAL | Refused in code for PR, PO/quotation, vendor qualification, GST rule, stock adjustment, opening stock, item master, MIR/return/BOM/job order. **Not refused:** vendor bill accept by its creator (verified: `decide_vendor_bill`, `VendorBillCostingSql.cs:111-135`, checks the role only); vendor commercial verification; item merge (TD alone, by design); vendor payments (cannot prove) | **RED** (vendor bill) |
| 47 | Permissions, scopes, company isolation | PARTIAL | Company header verified against the signed identity's mapping; page permissions; composite `(CompanyId, Id)` keys; MFA for TD/MD/Accounts Manager/CFO. No PostgreSQL row-level security; no scope revoke; #32 open; the 11 development logins stay enabled on a fresh database until migration 131 ships (cycle running) | AMBER (131 in flight) |
| 48 | Rounding, GST split, UOM precision | PARTIAL | Calculator rounds each component and refuses an inconsistent split. **Supplier and place-of-supply state codes are typed per quotation line, default '33' in the FE (verified: `QuotationPage.tsx:85-86`), and are never checked against the vendor's GSTIN or the company's state.** The company has no stored state. An inter-state vendor is taxed CGST+SGST unless the operator notices. Customer PO takes typed GST percentages. PR qty (18,3) vs PO (24,6) | **RED** (GST state) |
| 49 | Contract mismatches | PARTIAL | Sampled status literals match. #34/#35 fixed, not yet deployed. #31: backend safety net shipped; **FE confirmation of UTC on all 13 fields still outstanding** | AMBER |
| 50 | Open findings | OPEN | #25, #32; dashboard spending includes recoverable GST (confirmed); grid line 25 (DC) never proven; screens built after 19 Sep have no recorded walk | AMBER (walk before go-live, see timeline) |

**H. Dashboards** (`origin/feature/dashboards`, 2 commits, frontend only)

| Widget | Status | Evidence / issue |
|---|---|---|
| Purchase workload, open POs, obligations | PRESENT / PARTIAL | "Billed not received" queue missing |
| Purchase spending | **Wrong against the decision** | Uses `BilledPayableValue`, which includes GST (verified, `20260914020000_PurchaseSpending.sql:93-95`); backend change needed |
| Stores workload, QC stock | PRESENT | QC stock shows warehouse/rack as IDs; the MIR approver is never named |
| Custody by engineer, overdue returnables, stock value, reorder, put-away, vendor quality | MISSING | Not in the contract |
| Branch | **Not merged** (verified); company hard-coded `SESS_PVT_LTD` (verified); a Proprietorship response would be discarded; mock-tested only; 42 commits behind | |

**Rating: GREEN.** Dashboards are not needed to run on 8 October, unless the TD says otherwise.

**I. Print formats** (verified: none exist)

| Document | Status | Rating |
|---|---|---|
| Purchase order | MISSING | **RED** |
| Machine DC (dispatch) | MISSING (and no screen) | **RED** with the DC screen, unless the manual DC book continues (question 3) |
| GRN, gate pass, issue slip | MISSING | AMBER (screen view / registers) |
| Returnable gate pass, debit note | MISSING (no document flow at all) | AMBER (manual books, Tally debit note) |

**Setup blockers (not code, no go-live without them):**
- QC policies approved for ELE, FAB and REF (a category with none can never be accepted).
- Vendor qualification per category for every vendor to be invited.
- GST rules, with RCM set up as decided under question 5.
- An open inventory period (needed for any adjustment).
- The signed scope roster.
- Migration 131 on the server.
- Both opening-stock ceremonies.

## 2. RED items: owner, size, migration, dependencies

Owners are proposed; the Technical Director confirms (question 13).

| RED | Owner | Backend d | Frontend d | Migration | Depends on |
|---|---|---|---|---|---|
| **R1** Vendor reactivate → `Active`, plus from-state guards on vendor lifecycle actions | backend | 0.5-1 | 0 | No (data fix only if a vendor was already reactivated) | — |
| **R2** GST state codes: derive supplier state from the vendor GSTIN and place of supply from the company state; validate at quotation; FE pre-fills and stops defaulting '33' | backend + ILAMPARUTHI | 1-2 | 0.5-1 | **Yes** if company state is stored (question 2) | Q2, Q4 |
| **R3** Vendor bill: refuse creator = decider in `decide_vendor_bill` | backend | 0.5-1 | 0 | **Yes** (function replacement) | — |
| **R4** QC audit rows (inspection, correction, concession create/decide/reverse) | backend | 1-2 | 0 | No | — (AMBER on its own, cheapest in R1-R3's cycle) |
| **R5** Stock adjustment + inventory period screens (list, create, submit, approve/reject by band, period open/close) | MAGESHWARI (proposed) | 0-0.5 (reversal route optional) | 3-5 | No | Backend complete |
| **R6** Machine DC screen + DC print view | ILAMPARUTHI | 0 | 3-4 | No | `b7f64a9` in the package; build 2 (in the plan) |
| **R7** PO print view per company (letterhead, GSTIN, vendor GSTIN, HSN, CGST/SGST/IGST, terms, revision, signatory) as a browser print page | MAGESHWARI (proposed) + backend | 1-2 (company profile read endpoint) | 2-4 | **Yes** if company profile is stored in the database (question 2) | Q2 |
| **R8** Cut-over decision for open BroPOS documents | TD | 0, or 4-6 for a governed legacy open-PO import | 0-1 | 0, or Yes | Q1 (count of open POs) |
| **R9** Migration 131 (development logins off) | backend | done | 0 | Yes (head 131) | Cycle running; commit tonight if green |

## 3. AMBER items: the register or format needed from day 1

Each register needs an owner and a place (paper book, shared Excel, or Tally) **before 8
October**.

| For | Register / format from day 1 |
|---|---|
| 4, 41, 44 | Approved vendor list and re-evaluation register; supplier performance and SCAR/CAPA log |
| 5 | Capital-goods list; alternates list; drawing file index |
| 7 | Short-closed / cancelled-and-re-raised PO register |
| 9 | Service purchases (calibration, transport, AMC) in Tally only, with a service-PO register |
| 10 | Import purchases in Tally with BOE file (or declared out of scope) |
| 13 | Job-work register (material out, vendor, returnable challan no., qty back, date); ITC-04 from it |
| 14 | E-way bill number on the gate register |
| 15 | Excess-quantity register (returned or new PO) |
| 16 | RTV / debit note register; debit notes in Tally; the ERP quantity written off by a stock adjustment quoting the DC number |
| 17 | Bin transfer register (and periodic correction) |
| 18, 19 | Returnable gate pass book (tools and spares to sites) with expected return date; service/AMC material issued against a job order or CONSUMABLE_OFFICE with the ticket number in remarks |
| 20 | Refrigerant cylinder register (cylinder no., deposit, out/in, kg) |
| 22 | Expiry register for oils, adhesives and paints; FEFO by hand |
| 23 | Reservations are advisory; Stores checks the job before issuing |
| 24 | Customer free-issue material register |
| 27 | Scrap sale in Tally |
| 29 | Labels from Excel / label software |
| 32 | Inter-company moves as a PO/GRN in each company |
| 33 | Accounts keys PO values; the actual invoice goes into Tally; differences on a variance log |
| 34 | RCM rules at zero rate + RCM in Tally; MSME 45-day aging in Excel/Tally; TDS in Tally |
| 35, 36 | Boundary note: Tally is the ledger of record for GST, TDS, payables aging and returns |
| 39, 40 | Existing ISO calibration register and document control continue unchanged |
| 49 | Frontend developer confirms UTC on all 13 fields, field by field |

## 4. Batching plan

- **Backend batch 1: one acceptance cycle.**
  - Contents: R1 + R2 (backend part) + R3 + R4. Migration 131 is already cycling on its own.
  - About 3-5 dev days, plus about 8 h for the cycle.
  - Migrations 132 (vendor bill function) and 133 (company state/profile, if question 2 says
    the database) can go together in one cycle.
  - **It must land before the build 1 package (28 Sep), or be approved by name during the
    freeze.** If it lands after build 1, build 2 is no longer frontend-only. It then needs its
    own cycle and a new migration proof, and must start by 1 October.
- **Frontend: no cycle, but a witness.**
  - R6 (ILAMPARUTHI, in the plan) and R2's FE part.
  - R5 and R7 (proposed MAGESHWARI) all go into **build 2** (about 2-3 Oct, frontend only if
    backend batch 1 is in build 1).
- **R8** is a decision, not a build, unless the open-PO count forces an import. That import
  would be a third backend batch after go-live, or a separate cycle in the first week.

## 5. Timelines

**Go-live 8 October:**

| Date | Step |
|---|---|
| 26 Sep evening | Migration 131 committed if green |
| 27 Sep | RED list approved; backend batch 1 coded |
| 27-28 Sep | Backend batch 1 cycle (overnight) |
| 28-29 Sep | Build 1 (backend incl. 131 + batch 1; login frontend) |
| 29 Sep-2 Oct | FE: DC screen + print (ILAMPARUTHI); stock adjustment + period screens and PO print (MAGESHWARI); GST pre-fill |
| 2 Oct 12:00 | Frontend SHA deadline |
| 2 Oct | Build 2 |
| Evening 3 Oct | Install |
| 5-6 Oct | Ceremonies |
| 7 Oct | Final checks + walk of the new screens |

**Slack:** none. It holds only if both frontend developers work full time from 27 September, R7
is a plain print page, and no cycle fails. A failed cycle costs about 8 h and pushes build 1
into 29-30 September.

**Go-live 10 October:** the same up to build 1, then:

| Date | Step |
|---|---|
| Up to 4 Oct | Build 2 |
| 4 Oct | Install (not during ceremonies) |
| 5-6 Oct | Ceremonies |
| 7-9 Oct | Walk of every new screen on DEMO or the server, register readiness check, training |
| 10 Oct | Go-live |

Build 2 could also be installed on the evening of 7 October, after the ceremonies, if the
frontend needs the extra days.

**Recommendation: 10 October is the realistic date.** It absorbs one failed cycle and two
frontend days. It also leaves time to walk the screens nobody has walked since 19 September.
8 October is possible only if every assumption above holds.

## 6. Questions only the Technical Director can answer

1. **Open BroPOS POs on 7 October:** how many?
   - Re-raise each through the full chain in the ERP?
   - Complete them outside the ERP (receipts booked as opening stock or Tally only)?
   - Build a governed legacy open-PO import (4-6 backend days)?

   The same question applies to pending GRN/QC, vendor advances and unpaid bills.
2. **Company letterhead data** (GSTIN, address, state code, logo, signatory) for both companies:
   stored in the database (migration + read endpoint), or fixed configuration in the frontend?
   It is needed by R2 and R7.
3. **Machine DC:** must it be printed from the ERP on day 1, or does the manual DC book continue
   until the print view exists?
4. **GST state rule:**
   - Is the supplier state always the first two digits of the vendor GSTIN? For an unregistered
     vendor, is it the vendor's state code?
   - Is the place of supply always the buying company's state?
5. **RCM:** set up RCM GST rules at zero rate, with the liability booked in Tally? Or fix the
   calculator to exclude RCM tax from the payable (small backend change)?
6. **3-way match:** accept a rounding tolerance (for example ±₹1 per bill)? Or keep exact match,
   with Accounts keying PO values?
7. **Service POs** (calibration, transport, AMC): in the ERP (needs a service-receipt path,
   4-7 days) or Tally-only for now?
8. **Tally boundary:** is Tally the ledger of record, with ERP payments as evidence only? Who
   keys what?
9. **Dashboards:** needed on day 1? If yes, spending must first exclude recoverable GST and the
   branch must be merged with the company fixed.
10. **Bin/warehouse transfers:** how often do they happen?
11. **Job work** (laser cutting, powder coating, machining): how many movements a month? Is a
    register enough until the material DC exists?
12. **MSME 45-day rule:** who watches it, and from which report?
13. **Owners:** is MAGESHWARI available full time from 27 September for R5 and R7? Is
    ILAMPARUTHI kept on the DC screen and login?
14. **Backend freeze:** will you approve backend batch 1 by name if it cannot land before the
    28 September build?
15. **ISO:** do calibration, CAPA/SCAR and document control stay in the existing registers?
16. **Inter-company:** is any material moving between the two companies in October?
17. **Vendor commercial verification:** must the verifier differ from the vendor's creator?
    (It is not refused today.)
18. **UOM precision:** limit every UOM to 3 decimals, to match the stock ledger (18,3)?

## Limits of this audit

- **`f67c00a` is not included**, and the frontend has not been re-fetched since 667c248.
- **Server state is not audited:** installed package, migration head, grants, QC policies,
  scopes.
- **Nothing was executed.** "Cannot prove" items remain open:
  - QC-rejected quantity on a bill;
  - cancelling a PO after a partial GRN;
  - posting of quantities finer than 0.001;
  - vendor payment maker-checker;
  - component serial text in the dossier.
- **Sizes are one-developer estimates,** before review and before an acceptance cycle.
