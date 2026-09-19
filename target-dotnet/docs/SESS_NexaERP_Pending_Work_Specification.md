# SESS NexaERP — Pending Work Specification

Date: 10 September 2026
For: Codex
Basis: frontend grid of 10 September, specification audit of 9 September,
frozen decisions in `docs/SESS_ERP_Stores_Full_Schema_Guideline.docx`

---

## How to use this

Twenty-eight items, in priority order. Each states what is wrong or missing,
what must be true when it is done, what must be refused, and what test proves
it.

Work top to bottom. Commit each separately. Where an item is too large for one
session, finish what you can, commit it, and report where you stopped.

Nothing in this document overrides a frozen decision. Where you find a
contradiction, stop and say so rather than choosing.

**Already committed, do not restart:** defect 3 (`4367948`), defect 5
(`d100526`), landed cost (`ff4a432`), ACL runbook (`457c832`), notifications
(`8838f50`).

---

# BLOCK A — DEFECTS FROM THE FRONTEND GRID

## 1. Estimated BOM can become permanently stuck — IN PROGRESS

**Wrong:** EBOM-000011 was submitted without a unit value. It cannot be
approved, edited or returned. `ValidateSubmissionAsync` does not check values,
and no reject or return-to-draft transition exists.

**Done when:**
- a submission with any line lacking both a unit value and an override is
  refused at SUBMIT with 409 naming the first offending line
- a reject / return-to-draft transition exists, recorded with actor, timestamp
  and reason
- the returned revision is editable again
- EBOM-000011 can be recovered by that transition

**Must refuse:** approval of a revision containing a valueless line, even if
submission somehow succeeded.

**Test:** a workbook import carrying a line with no value and no override,
submitted through the API, is refused. The form gate is not the control —
import bypasses it.

**Then:** audit every workflow for a state with no exit. Report each state and
its exits. Report every rule that exists only in the UI.

---

## 2. A Purchase-department PR is invisible to TD, MD and Production Manager

**Wrong:** 404 for all three. Only Accounts and the raiser can read it. The TD
approves PRs between ₹5,000 and ₹1,00,000 and cannot see the ones his own
purchase department raises.

**Done when:**
- TECHNICAL_DIRECTOR and MANAGING_DIRECTOR can read any PR in a company where
  they hold an effective assignment
- an employee mapped to approve any step of a PR can read it
- the requester can always read their own

**Must refuse:** an employee with no assignment in that company, and an
employee with only a SUPPORT assignment attempting an approval verb.

**Test:** as SESS-01, read a PR raised by SESS-15 in the Purchase department.
Today it is 404.

---

## 3. RFQ, comparison and PO detail cut the connection

**Wrong:** `Rev869BPurchaseEndpoints.cs:57` GetRfq, `:113` GetComparison, `:127`
GetPo return EF entities with Lines included. Lines carry the back reference,
the serializer cycles, 200 is already sent, the connection is severed —
"transfer closed with outstanding read data remaining".

**Done when:** all three project to DTOs with no navigation back-references.

**Test:** fetch each detail with a multi-line document and read the response to
completion.

**Then:** report every endpoint that returns an EF entity directly rather than a
projection.

---

## 4. Lookup reachability — the build-failing test misses dropdowns

**Wrong:** the test covers mandatory request fields but not lookups a screen
needs to render.

Known gaps:
- TECHNICAL_SUPPORT_MANAGER cannot read OPEN job orders or UOMs
- PRODUCTION_MANAGER cannot read UOMs
- QC_MANAGER cannot read the job order it must reconcile, and has no list to
  find one
- STORES_EXECUTIVE SESS-16 cannot read the PO a gate entry needs

**Done when:** every role that may open a screen can load every lookup that
screen requires, without widening an unrelated master permission.

**Must refuse:** widening `masters.uoms:view` to production roles. Expose the
identifier on the item projection instead, as was done for `BaseUomId`.

**Test:** extend the build-failing test so a screen a role may open must be
able to load every lookup it needs. QC_MANAGER unable to read the job order it
must reconcile is the same class as the custodian who could not read his own
custody — the test must catch both.

---

## 5. Stores cannot read the PR detail a stock check needs

**Wrong:** Stores hold `stores.stock-check:verify`, but only
`/stores/stock-check/requisitions` is Stores-scoped. The stock check request
needs line numbers from the PR detail, which returns 403.

**Done when:** a role holding `stores.stock-check:verify` can read the lines of
a PR in `StockCheckPending`, and nothing else about it.

**Must refuse:** the same role reading a PR in any other state, or a PR in
another company.

---

## 6. `/session/me` hides a page the API allows

**Wrong:** it omits `security.employee-identities` for IT_MANAGER though the
endpoint filter grants it. `EfSessionService`'s projection disagrees with
`RequirePagePermission`.

**Done when:** the projection and the filter derive from one source.

**Test:** a contract test asserting that for every role, the pages in
`/session/me.Permissions` exactly equal the pages `RequirePagePermission`
would admit.

**Then:** report every page where the two disagree today.

---

## 7. A malformed GUID returns 500

**Wrong:** `estimated-boms` and `material-issues/from-request` return 500 for a
malformed GUID in the body, not 400.

**Done when:** fixed at the model binder, globally, not per endpoint.

**Test:** post a malformed GUID to three unrelated endpoints and assert 400
with a field name.

---

# BLOCK B — DATA AND CONSISTENCY

## 8. Seed all eleven identity mappings

**Wrong:** nine of eleven development logins had NO identity mapping.
Development sign-in issues subject = employee code, and
`rev869b_guard_history_insert` requires a mapping whose Subject equals the
actor login. Every RFQ, quotation, comparison and PO write returned 500 for
every login.

The frontend created nine by hand on his database only. SESS-04 and SESS-12
still carry `dev-sess-04` and `dev-sess-12` and will fail the same guard.

**Done when:** all eleven are seeded with issuer `urn:nexaerp:development` and
subject equal to the employee code, including corrected SESS-04 and SESS-12.

**Must refuse:** creating a mapping for an employee code that does not exist or
is inactive.

**Test:** every one of the eleven logins can complete one controlled write.

---

## 9. `QcDueAt` has two answers

**Wrong:** GRN finalization rewrites the stored `QcDueAt` from finalization
time; the QC queue correctly measures from receipt time. Two answers to one
question.

**Done when:** the stored derivative is computed from receipt time, matching the
queue and the notification engine.

**Then — the more important half:** report every other stored derived value
that could disagree with the projection that computes it live. Two answers to
one question is worse than one wrong answer, because nobody knows which they
are reading.

---

# BLOCK C — COMMERCIAL COMPLETENESS

## 10. Vendor advance

**Missing entirely.** SESS pays some vendors in advance; every offer carries
30 per cent advance against order acceptance.

**Done when:**
- an advance is recorded against a purchase order, immutable once recorded
- total active advance may never exceed the PO value
- ACCOUNTS_MANAGER records and reverses; SUPPORT cannot
- adjustment at bill acceptance is automatic and oldest-first
- reversing an accepted bill restores the adjustment evidence
- outstanding advance is visible per vendor
- a cancelled PO with an outstanding advance is REPORTED, never silently
  refunded, reallocated or written off

**Must refuse:** an advance exceeding the PO value; an advance against a
cancelled or draft PO; a second adjustment of the same advance.

**Test:** two advances against one PO, a bill accepted consuming the older
first, then the bill reversed and the advance restored.

---

## 11. Import purchase

**Missing entirely.** SESS buys from China and other countries, by sea and air.

**Done when:**
- PO and bill carry a foreign currency and an exchange rate
- customs duty, clearing agent charges and freight are landed cost, allocated
  by the confirmed rules
- item cost is stored in rupees; the bill may be in dollars

**Report before building:** which exchange-rate date Indian accounting practice
requires — PO date, bill date, or payment date. Accounts will confirm; do not
choose.

**Must refuse:** a bill in a currency other than its PO's, unless an explicit
revision changed it.

---

# BLOCK D — DEPLOYMENT CORRECTNESS

## 12. The ACL defect

**Wrong:** FK checks run as the referencing table's owner. On a database where
the reassign failed before the revoke, `nexa_erp_owner` has no USAGE on schema
`advance` and no SELECT on its own tables, so `dev/token` returns 42501 and
nobody can sign in.

**Done when:**
- ownership is reassigned BEFORE revoking, never after
- after any reassign, the owner is explicitly granted USAGE on the schema and
  ALL on its tables, sequences and functions
- no code relies on implicit owner rights surviving a revoke against the owner

**Test:** the regression must assert the owner can USE the schema, SELECT from
its tables, and that a FK CHECK SUCCEEDS. The current test asserts only that
nothing remains REV-owned — which passes while the ACL is stripped.

**Report:** what a database already in the broken state must run to recover,
and what else in the migration chain behaves differently depending on database
history.

---

# BLOCK E — GO-LIVE PREREQUISITES

## 13. Opening stock

**Missing entirely.** Without it the ERP goes live at zero and every issue
fails. This is a hard prerequisite, not a refinement.

**Done when:**
- imported through the framework: item, warehouse, rack bin, lot, serial where
  applicable, quantity, rate
- posted as OPENING_BALANCE through the controlled posting function
- THREE SEPARATE ACTORS: Stores counts, Accounts values, Technical Director
  authorises. No one person may do two stages.
- immutable once posted; correction is an adjustment, never an edit
- opening layers carry the valued rate on the same landed-cost basis as a GRN
  layer

**Must refuse:** running twice for the same company and period; running when
any stock movement already exists for that company; one person completing two
stages.

**Report:** what happens when a customer holds stock in two companies and loads
only one.

---

## 14. The missing import adapters

**Wrong:** the framework has UOM, customer and vendor only. 1,388 items cannot
be loaded any way except direct SQL.

**Done when** adapters exist for:
- items — HSN, GST rate, UOM, category, subcategory, manufacturer, preferred
  vendor, serial policy, reorder level
- employees — code, name, department, designation, joining date, company
  assignments
- warehouses and rack bins — the real 16 racks, the QC rack, two
  customer-property racks
- item-vendor links

Same three-sheet workbook, same validation, same per-row errors, same
RejectEntireFile and ImportValidRows modes.

**Must refuse:** employee import creating identities or logins — those are
governed runtime operations. Item import bypassing Draft governance — imported
items land as Draft unless the importer holds STORES_MANAGER or
PURCHASE_MANAGER.

---

## 15. The reports

**Missing entirely.** Ten reports, all company-scoped, permission-gated,
exportable to Excel.

1. stock balance by item, warehouse, rack, lot, serial, ownership, condition
2. movement roll-forward — opening, receipts, issues, adjustments, closing
3. FIFO valuation with ageing buckets
4. goods received not billed
5. billed not received
6. purchase register — PR through PO through GRN through bill
7. vendor-wise purchase summary
8. pending approvals by approver, with age
9. outstanding engineer custody, by engineer
10. **component ancestry for one delivered machine**

**The tenth is the audit dossier.** An auditor names a chamber and receives
every component, its GRN, its vendor, its accepted bill including allocated
charges, its QC inspection and every deviation approval.

**Report:** which cannot be built yet and what each waits for.

---

## 16. Cognito and real login

**Report only. Do not build.**

Nobody can sign in for real; the bootstrap ceremony has never run.

- what SURANTHER must create in AWS Cognito — user pool, app client, settings
- what the ceremony does, step by step, and what it needs from him
- how the 42 employees get their identity mapping
- what happens to the Debug token path afterwards — removed, or retained under
  `#if DEBUG`
- what breaks if Cognito is unreachable at a customer site
- **whether an on-premises alternative is possible for customers who will not
  use a cloud identity provider**

The last question matters. This is being sold. Not every customer will accept
AWS.

---

# BLOCK F — THE REST OF THE SPECIFICATION

These are decided and written. None blocks the first day of use.

## 17. Delivery challan and engineer accountability

Full specification: `docs/SESS_NexaERP_DC_Custody_Specification.md`. Follow it
exactly; if any part cannot be built as written, stop and say so.

Key invariants:
- three DC types chosen at creation, immutable: CONSUMABLE, TOOL, MACHINE
- machine DCs require a customer signature at delivery
- tools return 100 per cent — a missing tool is a LOSS, never consumption
- custody follows the material, not the paperwork
- handoff is line by line, on the same soft copy both engineers see
- custody moves only on acceptance; unaccepted lines stay with the sender
- one day to accept, one day to explain, then the Service Manager, then his own
  defect
- Stores decides whether a shortfall explanation is accepted
- shortfall acceptance: Service Manager to ₹5,000, TD above
- material returning without a DC is accepted as an unidentified return, with a
  deviation raised
- deviations are recorded automatically from real events, never typed
- the Service Manager may waive with a reason, and waivers are counted against
  him

**Engineer portal read model:** sites assigned, material in custody, handoffs
waiting, shortfalls to explain, own deviation count.

---

## 18. Stock adjustment and cycle counting

- adjustment bands: below ₹5,000 Stores Manager, ₹5,000–₹1,00,000 TD, above MD,
  serialized identity always TD, write-off TD with Accounts concurrence
- cycle counts are quarterly from go-live; ABC classification is automatic by annual consumption value, with a reasoned Stores Manager override
- count freeze; break requires TD approval, is recorded, and the affected scope
  is recounted
- inventory periods; backdating maximum 7 days, beyond that TD

---

## 19. Transfers, intercompany and scrap

- stock transfer between warehouses inside one company
- **intercompany movement is a REAL SALE** with a GST invoice, raised through
  the normal purchase flow with the other SESS company as vendor. Nobody moves
  material between companies without that PO.
- scrap declaration and scrap disposal are TWO DIFFERENT ACTS with two
  different approvals. Do not collapse them.
- disposal: invoice before dispatch, payment before dispatch, Stores selects the
  buyer, MD approves every disposal

---

## 20. Job work and subcontract

- dispatch and return, weight balance both ways
- tolerance PER PROCESS — powder coating ~5 per cent, CNC ~10 — configurable by
  TD, MD or IT_MANAGER
- within tolerance: accepted automatically with scrap recorded
- outside tolerance: vendor explanation and approval before closure

---

## 21. Customer property

- SEPARATE from the vendor GRN. Customer property never enters SESS inventory
  value.
- other-brand machines for modification — always chargeable, need an offer and
  a PO
- SESS machines and spares returned under warranty
- a machine may arrive WITHOUT a customer PO — accept the inward entry, but
  work cannot start until an offer is made and a PO received
- accessories as a list with photo evidence, not individually serialised
- due date entered by Stores after consulting management; overdue notifies TD
  and MD; extension requires TD
- **removed parts are ALWAYS customer property** and go back with the machine.
  SESS keeps them only under an explicit recorded buyback agreement.

---

## 22. Tools and calibration

- every tool is an asset with its own identity — no value threshold
- permanent custody and temporary issue with a duration in DAYS entered at issue
- a tool not returned is a LOSS, written off with TD approval
- employee resignation clearance blocks while tool custody is open
- calibration register for internal master instruments, annually
- provider master, initially Sansel Calibration Laboratories LLP, Chennai
- expired calibration WARNS, does not block, and the fact that an instrument was
  issued while expired is recorded on the issue

---

## 23. Vendor rating

Full specification: `docs/SESS_NexaERP_Vendor_Rating_Specification.md`.

- rated after GRN by the QC Manager
- quality per GRN LINE; delivery, documentation, technical and response per GRN
- eight dimensions: Quality /25, Delivery /20, Warranty /10, Commercial /10,
  Documents /10, Technical /15, Response /5 and Overall /5
- Quality, Delivery, Warranty, Commercial and Documents are computed from
  retained QC/GRN/PO evidence. Missing evidence must not become full marks.
- QC Manager records Technical /15, Response /5 and Overall /5 per GRN.
- retain the 1,057 historical bill ratings as LEGACY, preserving their typed
  marks rather than presenting them as measured results
- twelve-month rolling window
- new vendors open at 100 per cent, provisional until three receipts
- bands: 90+ Excellent, 80–89 Good, 70–79 Acceptable, below 70 Poor
- **a vendor below 70 cannot receive a PO without BOTH TD and MD concurrence**
- revaluation: improvement letter as evidence, QC Manager recommends, TD
  approves, reinstated on probation for five receipts
- vendor qualification record — factory inspection, GST, bank, MSME, category
  capability, approved makes, re-qualification due date

---

## 24. Remaining purchase

- vendor debit and credit notes
- purchase return against a specific GRN and bill
- purchase amendment and cancellation with recorded reasons
- blanket and rate contracts drawn down over time

---

# BLOCK G — PRODUCT READINESS

## 25. Concurrency under real use

Everything has been tested one user at a time. Two storekeepers will work the
same afternoon.

Build genuinely concurrent tests against real PostgreSQL:
- two people issue the same item from the same lot at the same moment — one
  succeeds, one is refused, the balance is never negative
- two people finalise the same GRN
- two people approve the same PR, MIR or vendor bill
- a QC disposition and a concession on the same lot at once
- an issue and a stock adjustment on the same balance
- a role assignment change while that employee is mid-command
- fitment and return on the same issued serial
- two FIFO consumptions racing for the last unit of the oldest layer

**Prove the lock ordering holds** across GRN, QC, MIR, Issue and DC when all
run at once. Anything that can deadlock or oversubscribe is a defect.

The MIR Version defect the frontend found is this class. Report every other
transition where two people could overwrite each other.

---

## 26. Failure behaviour

Report what happens today, then fix what is wrong:
- the API loses its database connection mid-transaction
- PostgreSQL restarts while a posting is in flight
- a user closes the browser between submit and approve
- a duplicate request arrives because the network retried
- the disk fills
- a migration is interrupted
- two API instances run at once against one database

For each: does it corrupt data, leave an orphan, or fail cleanly? Anything that
corrupts or orphans must be fixed. Prove the retry case rather than assuming
idempotency covers it.

---

## 27. Performance at real volume

Everything has been tested with 20 trial items. The question that moved this
project to .NET — will it hold 300,000 items — has never been answered.

Build a load fixture and report **numbers**:
- 300,000 items, 5,000 vendors, 50,000 POs, 200,000 GRN lines, 2,000,000 stock
  movements, 500,000 issue lines, 100,000 FIFO layers
- list and search response times
- the stock balance query — it aggregates movements, so it degrades first
- FIFO layer consumption at depth
- component ancestry for one machine when the ledger is two million rows
- offer versus actual across 500 completed machines
- which indexes are missing
- where the controlled posting function serialises, and what that costs under
  concurrency

Report seconds, not reassurance. If something takes eleven seconds, say eleven
seconds.

---

## 28. Installation, backup and upgrade

**Installation.** A customer site has an IT person, not a developer. One person
must install it in a day: empty PostgreSQL to running ERP, the four principals,
migrations, seed data, company setup, first administrator, configuration
defaults, and a verification command proving the installation is complete.

Report every step that currently requires a developer, and close each one.

**Backup.** Every backup on this project has been typed by hand. Scheduled
daily database and weekly globals, a retention policy, a restore procedure that
has been TESTED, automatic verification that a backup is restorable, and what
the customer does when a disk fails.

**Upgrade.** This decides whether this is a product or a one-off build:
- how migrations reach a customer database
- who runs them and with what authority
- what happens if one fails halfway at a customer site
- how we know which version a customer is on
- whether a customer can skip versions
- rollback, and whether it is possible at all once business data exists

Answer plainly.

---

# THE FINAL VERIFICATION

When Blocks A to E are complete, one clean run on a database built from
nothing:

```
empty PostgreSQL
  → four principals provisioned
  → entire migration chain
  → seed reference data, company, first administrator
  → master data through the import framework
  → opening stock through the three-actor ceremony
  → PR raised, approved, RFQ to PO, PO issued
  → gate entry, GRN with serials, QC partial acceptance, one concession
  → approved MIR, issue to an engineer, partial return
  → fitment confirmed, Actual BOM generated
  → vendor bill with freight accepted, landed rate proven
  → FAT reconciled to READY
  → component ancestry report
  → both variance baselines
```

Report every step that needed a developer, a manual SQL statement, or a
workaround. **Those are what stand between this and a product.**

Then answer the auditor's question with the actual output: name every component
in that chamber, its GRN, its vendor, its accepted bill including allocated
charges, its QC inspection and every deviation approval.

That single report is the point of everything since 27 August.

---

# STANDING RULES

- every frozen decision in `docs/SESS_ERP_Stores_Full_Schema_Guideline.docx`
  stands; contradiction means stop and say so rather than choosing
- PostgreSQL cluster guard on Up and Down for every migration
- nothing applied to the owner database
- BUILD MUST SUCCEED before any test count; stash model-changing work and
  rebuild first
- expected row counts and BOTH Debug and Release counts per commit
- commit each item separately on current HEAD
- never synthesise a different parent commit
- target zero failures

Report which items are complete, which are partial, and exactly where to
resume.
