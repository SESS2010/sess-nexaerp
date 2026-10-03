# R1 signed roster seed reconciliation (#57)

Migration 148: `20261003090000_R1RosterSeedReconciliation`.

Authority: TD-approved `NexaERP-R1-User-Roster-v1.xlsx` (30 Sep 2026), its
`ERP role (R1)` column, the 1 Oct addendum, and the **3 Oct correction** retaining
DESIGN_ENGINEER + SERVICE_ENGINEER for SESS-17 and SESS-19, plus the TD-approved
PURCHASE_MANAGER + PURCHASE_EXECUTIVE pair for SESS-15. The read-only source
workbook SHA-256 is `85d89105e7f0183399fcab01adff9c63b13637085c41588c9663e77a6f093966`.
The workbook and personal contact data are not repository artifacts.

The same assignments apply in SESS_PVT_LTD and SESS_PROPRIETORSHIP.

| Employee code | Approved R1 role(s) from 10 Oct |
|---|---|
| SESS-01 | TECHNICAL_DIRECTOR |
| SESS-02 | MANAGING_DIRECTOR + CHIEF_FINANCIAL_OFFICER |
| SESS-12 | IT_MANAGER |
| SESS-14 | ACCOUNTS_MANAGER |
| SESS-15 | PURCHASE_MANAGER + PURCHASE_EXECUTIVE |
| SESS-16 | STORES_ASSISTANT |
| SESS-17 | DESIGN_ENGINEER + SERVICE_ENGINEER |
| SESS-19 | DESIGN_ENGINEER + SERVICE_ENGINEER |
| SESS-21 | HR_EXECUTIVE + HR_MANAGER |
| SESS-25 | PRODUCTION_MANAGER |
| SESS-28 | ACCOUNTS_ASSISTANT |
| SESS-33 | QC_MANAGER |
| SESS-35 | STORES_EXECUTIVE |
| SESS-41 | STORES_MANAGER |

This is 14 users, 19 assignments per company, **38 assignments** in both companies.
No-ERP-login rows outside the approved exceptions receive no role reconciliation.
SESS-32 and SESS-40 receive no R1 role change; their access remains governed by the
separate DEMO-only UAT cutoff (9 Oct) and production identity provisioning.

## Cutoff and history

Correct existing FULL assignments retain their start dates and history. The fresh
3 Oct seed needs two missing HR_EXECUTIVE assignments for SESS-21, effective
**2026-10-10**. These do not become current authority before that date.

The following extra assignments are end-dated **2026-10-09**, inclusive, in each
company (six assignments total):

| Employee code | Extra role to end |
|---|---|
| SESS-15 | STORES_EXECUTIVE (SUPPORT) |
| SESS-28 | SERVICE_COORDINATOR (SUPPORT) |
| SESS-41 | ACCOUNTS_ASSISTANT (SUPPORT) |

The approval status remains Approved/SeedApproved through the last effective day;
setting it to Ended now would revoke authority early. End metadata and immutable
role events record the TD's cutoff decision. DESIGN_ENGINEER for SESS-17/19 is
explicitly retained, with SERVICE_ENGINEER, including after 10 Oct.

The merged CFO installation baseline derives its dates from the installation day.
For a fresh database created later than 10 Oct, only that untouched baseline's
assignment and company activation start are aligned to the approved 10 Oct cutoff.
Earlier valid starts remain intact; unexpected edited or future assignments are refused.
The original dates are journaled and the assignment correction has an immutable event.

The migration requires the approved employees, roles, exactly one effective company
membership and enabled role activation. It refuses unexpected extra authority or
changed desired assignments for TD review. It does not change employee login flags,
identity mappings, company memberships, operational scopes or page grants.

## Estimated BOM read-only confirmation

DESIGN_ENGINEER has `design.estimated-bom` View/Create/Update/Submit/Download.
Workbook import requires Create and creates a DRAFT; the template requires Download.
The job must be Accounts-confirmed OPEN in the selected company. Submission validates
the revision's lines and values. **TECHNICAL_DIRECTOR** approves a SUBMITTED revision,
with effective approval authority and its page grant; the preparer cannot approve their
own revision. SERVICE_ENGINEER remains available for the separate service MIR flow.

The readback test resolves the fresh seed's actual effective assignments and page
permissions for SESS-17/19 in both companies. It executes no Estimated BOM command.
Production identity and applied database configuration still require server-agent
readback during the production commissioning procedure.

## Rollback and verification

Up uses the existing independent role-administration guard. No guard is disabled and
no SECURITY DEFINER function is introduced. Down refuses changed/additional assignments,
additional role-administration events or recorded use of newly introduced authority.
For an untouched candidate, it restores previous validity, retires the unused new rows,
and appends rollback events. **No assignment or event is deleted.** Permanent owner-only
journals retain each Up/Down generation; repeat Up creates a new generation safely.
An unused assignment retained by Down is reactivated with a new history event rather
than inserting a duplicate assignment for the same effective date.

Focused checks cover the exact 10/11 Oct manifest, the 9 Oct boundary, all five exception
pairs, DEMO exclusions, unchanged identity/configuration, immutable history retention,
Up/Down/Up, atomic refusal of unexpected extra authority, refusal after immutable audit
use, and Estimated BOM permission readback. The fast suite is a separate merge gate;
only a green nightly SHA may move to main, and only the TD moves it.

## Fresh-company rehearsal gate

The TD approved PURCHASE_MANAGER + PURCHASE_EXECUTIVE for SESS-15 in both companies.
The existing PURCHASE_EXECUTIVE assignments are retained, resolving RFQ creation,
vendor invitation and quotation entry without changing operational role gates or grants.
Comparison and PO approval keep their independent configured workflow and maker/checker guards.
The department approver acts below INR 5,000; INR 5,000 through 100,000 adds TD,
and amounts above INR 100,000 add MD. Creator self-approval is prohibited at every step.
RFQ creation/invitation has no separate RFQ approval endpoint in this R1 lifecycle.
The fresh-company proof uses the real seeded SESS-15 pair for the purchase chain and
asserts HTTP 403 for creator attempts to approve its comparison and PO. Independent
approval then succeeds through the unchanged configured route.

Separate disposable support actors preserve the existing mixed FULL/SUPPORT security
regressions after the cutoff. They are not used by the fresh-company go-live proof
and do not change any real employee's production role assignments.
