# Owner instruction after Item 15 acceptance — 15 September 2026

Recorded verbatim from the supplied attachment. This updates priority and the Stage Signature decision; implementation status must be evidenced separately.

fc6de74 witnessed on my machine and pushed. 836 passed, zero failed, 22m 35s.
Migrations 78 to 103 applied to the live database, RECONCILED and VERIFIED.

ITEM 15 IS DONE. That was the last go-live blocker in the reports.

=============================================================
WHAT COMES NEXT, IN ORDER
=============================================================

1. THE SCALE MEASUREMENT, if it is not already complete

   300,000 items, 2,000,000 movements. Report SECONDS, not reassurance. Stock
   balance aggregates movements and degrades first. If it needs a materialised
   view, say what invalidates it.

2. ITEM 16 - AUTHENTICATION. DECISION CHANGED: LOCAL KEYCLOAK, NOT COGNITO.

   Reasons: sign-in keeps working when the internet is down, a customer with no
   AWS account can run NexaERP, data stays on premises, no cloud cost. Your
   Keycloak witness already proves the code works with only configuration
   changed - that proof is what made this decision cheap.

   Answer these before building:
     where does Keycloak run - same machine as the ERP, or separate? The ERP
       server is a laptop for the first month.
     Keycloak's own database - its own PostgreSQL, or a schema in
       sess_nexa_erp? I expect separate; say why.
     BACKUP - Keycloak holds the only copy of every password hash and TOTP
       secret. Losing it locks everyone out permanently. Fold it into the
       automated backup you built and prove a restore.
     the two-pool design becomes two REALMS - Staff and Approvers, MFA
       mandatory in one. Confirm Keycloak does per-realm MFA the way Cognito
       does per-pool, or say what differs.
     what SURANTHER installs and configures - a checklist of what to click,
       not what it means. He is not a developer.
     upgrade path - Keycloak releases every few months. Who upgrades a customer
       site, and how?
     what breaks if Keycloak is down, and how SESS knows before eleven people
       call.

   KEEP THE COGNITO PATH WORKING. Do not remove it. A customer may prefer it
   and the code is already provider-agnostic.

3. BLOCK A - docs/SESS_NexaERP_Post_Session_Plan.md

   A1 INTERCOMPANY MOVEMENT - 5 to 10 times a month at SESS. Routine, not
      occasional. I had it low in the plan and that was wrong. Each is a real
      sale between the two SESS companies: GST invoice, purchase order, entries
      in both ledgers.

   A2 STOCK ADJUSTMENT - opening stock will be wrong somewhere and direct SQL
      is the only fix today. Below 5,000 Stores Manager, 5,000 to 100,000 TD,
      above MD. Serialised identity always TD. Write-off TD with Accounts
      concurrence.

   A3 TOOLS - the register is clean and imports as it stands: 164 types, 730
      purchased, 564 issued, 288 custody lines, 23 holders. Purchased minus
      issued equals balance on every one of the 164 rows.
      Individual custody, never a set. Temporary issue in DAYS, not a date.
      Loss written off at depreciated value; damage REPLACED, not written off.
      Resignation clearance blocked while custody is open, naming the tools.

   A4 VENDOR RATING - I analysed the spreadsheet: 1,057 bills, 225 vendors,
      average 96.9. FIVE of eight dimensions score full marks on EVERY BILL -
      Technical 15, Warranty 10, Commercial 10, Response 5, Overall 5. Sixty-
      five points measuring nothing. Documents is a constant 7.5. ZERO
      rejections across 1,057 bills.
      Five become automatic: quality from QC accepted/received, delivery from
      PO committed date against GRN date, warranty from GRN, commercial from PO
      terms, documents from GRN attachments. Three stay manual per GRN.
      Import the 1,057 bills marked LEGACY so nobody mistakes a typed 15/15 for
      a measured one.

4. ITEM 35 CORRECTION - STAGE SIGNATURE

   I wrote that the QC team alone prepares and signs the fifteen-stage check
   sheet. The Technical Director has corrected it: PRODUCTION MANAGER, QC
   MANAGER and DESIGN ENGINEER all verify stages, and any one of them may sign.

   Rework requires TD approval, and responsibility is shared - the engineer who
   did the work, but also the Production Manager and QC Manager who should have
   guided and caught it, and the Design Engineer who should have specified it
   clearly. Do not attribute rework to the engineer alone.

5. THE FOUR REV869B LEFTOVERS - 4,179 lines, a quarter of the real source

   ControlPlane 2,227 lines, ControlPlane.Contracts 1,886,
   ControlPlane.Persistence empty, SecurityMigrations 66.

   You found a ControlPlane.Tests project still referencing two of them.
   Include it in the retirement review. A new developer should not have to read
   and dismiss a quarter of this repository.

6. THE FITMENT → REVERSAL → RETURN SEQUENCE

   Still not proven. A component fitted, reversed, then returned to Stores: the
   machine is already at zero from the reversal; the return then restores the
   FIFO layer. Neither step should double-count. Prove it.

=============================================================
RULES - unchanged, plus one
=============================================================
  RUN THE FULL SUITE WITH TRX BEFORE REPORTING AN ITEM COMPLETE. Targeted runs
    hid a failure twice this week - the six console failures on 14 September
    and DispatchMachineRequest.JobOrderId today. Twenty-two minutes is the
    price and the gates exist to make it affordable.
  every frozen decision stands; contradiction means stop and say so
  PostgreSQL cluster guard Up and Down for every migration
  nothing applied to the owner database
  BUILD MUST SUCCEED before any test count
  expected row counts and BOTH Debug and Release counts per commit
  commit each item separately on current HEAD
  say in the commit report if anything the frontend reads has changed

Report which items are complete, which are partial, and exactly where to
resume.

End with RESULT_REPORTED_PENDING_WITNESS.

## Later owner amendment during Item 16 validation — 15 September 2026

The owner is at an expo for three days from tomorrow and cannot run PowerShell.

After EVERY commit, Codex must run git push and report the pushed SHA. Do not build against, migrate or provision the owner database. This replaces the earlier plan for the owner to push from a phone.

The owner accepted the Item 16 recovery method: stop Keycloak, run the scheduled backup, restore a fresh cluster, shut down the original database, and prove Staff and Approver/TOTP sign-in. The renamed internal-index-label false red and stale-success backup-status false green are accepted findings. Hostname, TLS, independent backup destination and production login screens remain outstanding site installation, not backend completion.

Order while the owner is away:
1. Finish Item 16: Release recovery, full routine Debug and Release suites, commit and PUSH.
2. A1 intercompany: regular real sale, normal PO, GST invoice, entries in both companies.
3. A2 stock adjustment.
4. A3 tools: 164 types, 730 purchased, 564 issued, 288 custody lines.
5. A4 vendor rating: five of eight dimensions automatic.
6. Item 35 correction: Production Manager, QC Manager or Design Engineer may sign; rework requires TD approval and shared responsibility.
7. Four REV869B leftovers, including the dependent ControlPlane.Tests project.
8. Fitment → reversal → return-to-Stores sequence.

Run the FULL suite with TRX before reporting an item complete. Keep going after a verification checkpoint.
