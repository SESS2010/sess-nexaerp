# Go-live — 1 October 2026: run order, first-time traps, two people on one screen

Written 19 September against feature/frontend `18c4f12`, backend `f94f332`,
DB 113. Three sections, as asked: the morning's sequence for the operators;
the places where a screen assumes something a first-time user does not know;
and what actually happens when two people work the same record at once —
tested today, not assumed.

Logins are the eleven enabled ones. Each step names the role that the server
will accept; nobody else's button will work, whatever is on their menu.

---

## 1. Run order for the morning of 1 October

Everything below assumes the database is at the go-live migration, the API is
running as `nexa_erp_runtime`, and the three pre-go-live items in section 1.0
were done the evening before. Items marked **once** are done once per company
and never again.

### 1.0 The evening before (once, per company)

| # | Screen | Who | What | Done when |
|---|---|---|---|---|
| 0.1 | Item Master → Import | TD (SESS-01) | Import the item workbook; fix errors.xlsx; import again until Rejected 0. | Import banner shows Rejected 0 |
| 0.2 | Vendors → Import, Customers → Import | TD | Same. The vendor file will reject the PAN duplicate (SESS-V-0004 / 0028) until one is ended — decide which before the night. | Rejected 0 |
| 0.3 | Employee Master | HR (SESS-12) | Confirm the eleven logins are enabled and the SUPPORT roles used in testing (SESS-33 STORES_ASSISTANT, SESS-16 TECHNICAL_ENGINEER) are ended. | Roles tab clean |
| 0.4 | QC Policies | QC Manager (SESS-33) prepare → TD approve | One policy per category at least (VISUAL is enough to start). **Without an approved policy nothing can leave QC.** | Each shows Approved |
| 0.5 | Tax rule (API only — no screen) | Accounts (SESS-14) create → MD (SESS-02) approve | HSN / state / rate rows for the items you buy, with ITC eligibility. **Without a rule a quotation cannot be recorded.** | `GET tax-gst-settings` shows Approved |
| 0.6 | Condition locations (API only until finding 18 lands) | Stores Manager (SESS-41) | Every bin that will hold stock needs an AVAILABLE location. Today only the seeded GEN-01 bins have one. | — |

### 1.1 Opening stock — first thing, before any movement (once, per company)

The ceremony refuses to run once the company has a single stock movement, so
this is first, and it is the only thing anyone does until it is POSTED.

| # | Screen | Who | Action |
|---|---|---|---|
| 1 | Stores → Opening Stock | Stores Manager (SESS-41) | ⬇ Template. Fill one row per item / bin; serial-tracked items one row per serial, qty 1. ⬆ Import. If Rejected > 0: ⬇ errors.xlsx, fix, import again. Batch id fills itself after a clean upload. Period, reason → **Record count**. |
| 2 | Opening Stock → the ceremony | Accounts Manager (SESS-14) | Open it, reason → **Confirm value**. |
| 3 | Opening Stock → the ceremony | TD (SESS-01) | Reason → **Authorize and post**. Status POSTED, posting batch shown, every line shows its FIFO layer. |
| 4 | Reports → Stock balance | TD or Accounts | Totals equal the count. Difference 0.00 in the reconciliation block. |

Three different people, in that order. The screen warns and the server refuses
if the same person tries two steps.

### 1.2 The purchase chain — for every item bought

| # | Screen | Who | Action | Leaves the screen as |
|---|---|---|---|---|
| 5 | Purchase → Purchase Requisition | Requester (SESS-15 for Purchase; PRODUCTION_MANAGER cannot raise) | + New, lines, **Submit** | Submitted |
| 6 | Purchase Requisition | Accounts Manager (SESS-14) | Verify, then Approve. Route is chosen by value; DEPARTMENT_THEN_TD adds TD's approve. | StockCheckPending |
| 7 | Stores → Stock Check | SESS-15 | Pick warehouse + bin per line → Check. Available stock is reserved; the shortage becomes a handoff. | Handoff PendingRFQ |
| 8 | Purchase → RFQ | SESS-15 | + New from the handoff, due date, invite vendor(s). Inviting the first vendor issues it. | Issued |
| 9 | Purchase → Vendor Quotations | SESS-15 | Record what the vendor sent: reference, terms, rates, HSN, states, registration type, attachment key + SHA-256. | Submitted |
| 10 | Quotation → Technical verification | **TD (SESS-01)** until finding 17 is fixed; then TSM (SESS-04) | Compliant / not, evidence, remarks. | TechnicallyCompliant |
| 11 | Purchase → Comparison | SESS-15 build + recommend, SESS-14 approve | + New from the RFQ → Recommend a quote → Approve. | Approved |
| 12 | Purchase → Purchase Order | SESS-15 create + submit, SESS-14 approve, SESS-15 issue | Four clicks, four states. Issued is what the vendor gets. | Issued |
| 13 | Stores → Gate Entry | Stores Executive (SESS-35) | + New from the PO number, DC no., vehicle, ISO checks, quantities → Finalize. | FINALIZED |
| 14 | Stores → GRN | SESS-35 | + New from the gate entry number, bill no. + date, lots, serials (scan), ISO checks → Finalize. Stock goes to QC hold. | FINALIZED |
| 15 | Stores → QC / Inspection | QC Manager (SESS-33) | Queue → Inspect: every effective policy's samples, per-serial accept/reject, accepted location → Finalize. | ACCEPTED / PARTIAL / REJECTED |
| 16 | Accounts → Vendor Bills | SESS-14 | + Raise from GRN (type the GRN number), check payable, charges → Raise → **Accept**. This is the moment the stock gets its landed value. | ACCEPTED |
| 17 | Accounts → Vendor Payments | SESS-14 | Record advance (against an issued PO, bank advice mandatory) or Record payment (allocate across the vendor's bills). | VPAY / VADV numbers |

### 1.3 The production chain — for every machine sold

| # | Screen | Who | Action |
|---|---|---|---|
| 18 | Sales → Customer PO | TD (SESS-01) | Record the customer's PO and its machine lines. No offer stage exists. |
| 19 | Production → Estimated BOM | TSM (SESS-04) create/submit → TD approve | The **offer** — the baseline every variance is measured against. Ex-tax. |
| 20 | Production → Job Orders | Production Manager (SESS-25) create → Accounts (SESS-14) verify | One per machine line. |
| 21 | Production → Production BOM | SESS-25 create/submit → TD approve | The plan. |
| 22 | Stores → MIR | Requester raise → SESS-25 approve | Job order is mandatory for chamber manufacture, service-with-PO, site work; not for consumables. |
| 23 | Stores → Material Issues | Stores (SESS-35) | Issue by scan against the approved MIR. Return is due one day later. |
| 24 | Production → Fitments / Actual BOM | SESS-25 | Fit by issue line + serial. The Actual BOM pane shows PROVISIONAL until the vendor bill (step 16) is accepted, then LANDED_ACCEPTED and **Offer vs actual**. |
| 25 | Stores → Material Returns | Custodian declare → Stores accept | Anything not fitted comes back; the FAT readiness check will not pass with unexplained custody. |
| 26 | Job Order → FAT readiness | QC Manager (SESS-33) | Reconcile: issued = fitted + returned. READY. |
| 27 | Machine delivery + signature | Stores Manager (SESS-41) — **API only, no screen was asked for** | DC number, non-returnable needs purpose CUSTOMER_PO_BASED, signature PNG. |
| 28 | Reports → Machine dossier | Accounts / TD | Serial in → ancestry, landed value, difference 0.00. |

### 1.4 Every day, before anyone goes home

Reports → Pending approvals as TD (what is waiting on whom), Engineer custody
(what is out and overdue), GRNI (received, not yet billed).


### 1.5 The room sheet — the 28 steps by person (manager's mapping, 19 Sep)

| Code | Person / role | Their steps on 1 October |
|---|---|---|
| SESS-01 | **Manager — Technical Director** | 0.1 item import · 0.2 vendor / customer import · 0.4 approve QC policies · **3 authorize opening stock** · 4 stock balance check · 6 (TD approve when the route says so) · **10 technical verification (until finding 17 is fixed)** · 18 customer PO · 19 approve Estimated BOM · 21 approve Production BOM · 28 dossier · 1.4 pending approvals |
| SESS-02 | MD | 0.5 approve tax rules · approvals above TD's band (comparison / PO routes DEPARTMENT_THEN_MD) |
| SESS-04 | TSM | 19 create / submit Estimated BOM · 10 technical verification **after** finding 17 |
| SESS-12 | IT Manager | 0.3 logins and roles · end the two SUPPORT roles · identity mappings when Keycloak lands |
| SESS-14 | Accounts Manager | 0.5 create tax rules · **2 confirm opening stock value** · 6 verify + approve PR · 11 approve comparison · 12 approve PO · **16 vendor bill raise + accept** · **17 advances and payments** · 20 verify job order · 1.4 GRNI |
| **SESS-15** | **PRIYA — Purchase Manager** | 5 raise PR · 7 stock check · 8 RFQ + invite · 9 record quotation · 11 build + recommend comparison · 12 create, submit, issue PO |
| SESS-25 | Production Manager | 20 job orders · 21 create / submit Production BOM · 22 approve MIR · 24 fitments / Actual BOM · 25 declare returns (as custodian) |
| SESS-33 | QC Manager | 0.4 prepare QC policies · **15 inspect and finalize lots** · concessions · 26 FAT readiness |
| **SESS-35** | **SUDALAI — Stores Executive** | **13 gate entry · 14 GRN (scan serials) · 23 material issue by scan · 25 accept returns** |
| SESS-41 | Stores Manager | 0.6 condition locations · **1 opening stock count (workbook, Record count)** · 27 machine delivery + signature (API) |
| SESS-16 | Stores Assistant | none on day one once the TECHNICAL_ENGINEER SUPPORT role is ended; second pair of hands on 13 / 14 / 23 |

Bold = the steps that person owns outright. The morning order is 0.x the
evening before → 1, 2, 3, 4 (SESS-41 → SESS-14 → SESS-01, nobody else
touches anything) → then PRIYA opens the first PR.

**Read aloud in training, twice:** *Opening stock is done once. There is no
undo. Practise in the trial company, never in the live one.* The screen now
says the same thing in amber at the top and asks once more before Authorize.

---

## 2. Where a screen assumes something a first-time user does not know

Not defects. Places where the screen is right and the person will still stop.
Ordered by how early in the morning it will bite.

1. **Login is a login id and a company, nothing else.** Dev-token login today;
   Keycloak later. Pick the company every time — the last one is remembered
   per browser, and a PROPRIETORSHIP action in PVT_LTD fails with a scope
   refusal, not with "wrong company".
2. **The menu shows what your role may see; the server decides what you may
   do.** A button that renders can still be refused (403 with the sentence).
   Read the sentence — every refusal names the rule.
3. **Every write needs a reason / remarks.** Empty remarks are refused before
   the request is sent; the reason is stored on the record and shows in the
   ceremony table, decision block or history. Write what a reader will want
   in six months, not "ok".
4. **Numbers are addressed by number, not by picking from a list.** Gate
   entry asks for the PO *number*; GRN asks for the gate entry *number*;
   vendor bill asks for the GRN *number*. Copy them from the previous screen.
   The purchase screens keep a browser-local "recent documents" list — it is
   per browser and per PC, it is not the system's list.
5. **Scan boxes commit on Enter and clear.** A barcode scanner sends Enter;
   typing works the same way. Keyboard mode is a checkbox on the return form
   for people without a scanner. A serial scan must be the exact stored
   serial; the item code is refused for a serialized line.
6. **The Estimated BOM is ex-tax and so is the landed rate.** A vendor bill's
   payable is tax-inclusive (1,475), the landed rate is 1,250, the offer was
   1,250. Anyone comparing payable to offer will think the variance is +225.
   The Actual BOM pane shows the right pair; the vendor bill detail shows
   both columns side by side for this reason.
7. **A GRN can only take an item whose category code is three characters**
   (ELE, FAB, FAS, MEC, PLC, REF). Until Codex's reconcile script has run on
   the live database, items still on FABRICATION / REFRIGERATION cannot be
   received — the storekeeper will see a refusal on the GRN line, not on the
   item.
8. **QC needs an approved policy before the queue will let a lot through**,
   and category policies apply as well as item policies — the inspector
   must record every effective policy's samples, or the finalize is refused
   with "policy X requires samples 1 through N".
9. **Opening stock is once.** After the first movement the screen refuses
   with "already has stock movements". Practise in PROPRIETORSHIP, not in
   the company you are going live in.
10. **Three-person ceremonies are three different people.** Opening stock
    (count / value / authorize), QC policy (prepare / approve), comparison
    (recommend / approve), vendor qualification (create / verify / approve).
    The screen warns when you have already taken an earlier step; the server
    refuses if you try anyway.
11. **Idempotency is per click, not per intent.** Clicking Submit twice is
    safe (the second is replayed). Filling the same PR twice on two
    different days creates two PRs — the system does not know they are the
    same need.
12. **Advance adjustment happens at bill acceptance, not after.** An advance
    recorded after the bill was accepted stays outstanding on the advance
    and unadjusted on the bill; the vendor position nets them. Record the
    advance before the bill, as happens in life.
13. **Reports' "to" date is locked to today for current-state reports**
    (stock balance, custody, pending approvals); only period reports take a
    from-date. Drill-through is by clicking the row, not a button.
14. **The Delivery Challan has no screen.** Dispatch and signature are API
    today (step 27). Decide before 1 October who does that and how.
15. **Exports are the import template.** To correct a master in bulk: Export,
    edit in Excel, Import — unchanged rows come back as Unchanged, not as
    errors. Do not delete the Record ID / Version columns.

---

## 3. Two people on one screen — what breaks, from what was seen

Codex proved eleven concurrent users at the API. Until today every screen had
one person on it. Today the same record was worked from two Technical
Director sessions at once; the results below are what the screens do, not
what they are meant to do.

### What holds

- **No write can overwrite another person's write.** Every transition
  carries the `Version` the screen last read (32 screens) and the server
  refuses a stale one with 409. Tested: TD session B approved QC policy
  `CONTACT_RESISTANCE`; TD session A, whose list still showed it Pending,
  clicked Decide → Approve and got **"Someone else changed this record — Your
  copy is out of date, so the save was refused rather than overwriting their
  work. Reload to get the current version, then reapply your changes. QC
  policy version is stale. ↻ Reload."** One approval exists, by B.
- **No double posting from a double click or a retried request.** 27 call
  sites send an idempotency key; a repeat returns the original document
  (`Replayed: true`).
- **Every refusal is a sentence, and the 409s carry a Reload button.** The
  opening-stock same-person and stale-version refusals, the QC stale version,
  the stale PR / PO / comparison versions all render the same way.

### What does not hold — and what to tell the eleven

1. **Screens never refresh by themselves.** No polling, no focus refresh
   (0 occurrences in the code). A list loaded at 09:00 shows 09:00 until
   Refresh or a navigation. In the test above, session A's list still said
   "Pending approval (1)" after B had approved. Consequence on 1 October:
   two storekeepers working the QC queue or the outstanding-custody list
   will both see the same rows; the second one to act gets the stale-version
   banner. **Rule for the room: Refresh before you act, Reload when the
   banner appears.** Not a defect — it is the design — but it will be the
   most common message of the first week.
2. **Two people can create two of the same thing.** The version rule protects
   a record; it does not stop two people creating two records for one need:
   two PRs for the same shortage, two RFQs from the same handoff is refused
   (the handoff moves to PendingRFQ), but two gate entries against one PO for
   the same delivery are two gate entries. **Rule: one person owns each PO's
   receiving.**
3. **Modals hold stale data for as long as they are open.** The vendor bill
   "Raise from GRN" modal, the fitment form, the return declaration all snap
   the record when opened. If the GRN is reversed while the bill modal is
   open, the raise fails at submit with the server's sentence — no earlier.
   Fine, but the person will have typed for two minutes first.
4. **Browser-local state is per PC, not per person.** Recent purchase
   documents, the last company, the collapsed nav sections and the dev token
   live in that browser. Two people sharing one PC will see each other's
   recent list; the last person's company is preselected for the next.
   **Rule: one login per browser profile; log out when leaving a shared PC.**
5. **Restarting the API logs everyone out at once** (dev tokens are
   invalidated). With Keycloak this changes; until then, schedule restarts.
6. **Not tested with two people, and I would want to before 1 October:**
   two storekeepers issuing from the same MIR by scan at the same time (the
   ledger check is at commit — the second should get "Insufficient AVAILABLE
   custody", and the screen will show it, but I have not watched it happen);
   two inspectors opening the same QC lot (finalize is versioned, the second
   should get the banner); two Accounts users allocating one payment to the
   same bill (the server refuses a bill allocated beyond outstanding).
   Each is half an hour with two browsers. I can run all three on 22
   September if the manager wants them witnessed rather than reasoned.

### The one line for the training slide

> Refresh before you act. If you see "Someone else changed this record",
> nothing was lost — Reload and look again; the other person's work is
> already in.
