# Go-live runbook: fresh database, 1 October 2026

> **Protected server rule (21 September, 16:01):** NEVER stop, disable, modify or remove
> SESS_SQLEXPRESS, TEW_SQLEXPRESS, SQLBrowser, their ~77 SOLIDWORKS project databases,
> ewserver, ANY NI or Siemens service, ANY Rockwell FactoryTalk service, IIS Default
> Web Site or /Updater. Leave Wamp stopped/manual. Windows 10 stays; NEVER a clean
> Windows install; NEVER install a .NET SDK on this server. All application here is
> by the server agent, not from the laptop. See C:\SESS-ServerPrep for completed preparation.


For this runtime-only server, use [server-deployment.md](server-deployment.md) for
package verification, DEMO-first order, service setup and migration bundle commands.
Its deployment sequence supersedes the source/SDK examples below. Principal provisioning
requires `CREATE SCHEMA advance` on an empty database before the Installer; then the
bundle runs as migration login with `Options=-c role=nexa_erp_owner`, followed by
RECONCILED/VERIFIED. The checked-in frontend's production authentication is still a gate.


Status: decision updated 21 September 2026: **NO DUMP.** Build Option C clean on
DESKTOP-SPF5420. Nothing is carried from the frontend developer's database: no data,
attachments, exported masters, identity mappings or earlier opening-stock balances.
Migrations supply the system baseline; the checked-in legacy item script supplies items.
SESS's own team enters all other business setup on **28-30 September** through existing
screens and the approved assisted-entry workflows
as training, and posts **both opening-stock ceremonies using template v2** in that window.
Daily transactions start **1 October 2026**, only after the release checks below.
The disposable fresh-company rehearsal is evidence for backend behaviour, not proof that
all training screens, production login or the selected server installation are accepted.

Actors: **DBA** = the PostgreSQL administrator (superuser session, `postgres`);
**Owner** = `nexa_erp_migration` acting as `nexa_erp_owner` (psql `SET ROLE`);
**API** = a signed-in seeded employee through the screens or the API.

Precondition for every step that runs a migration: the pulled worktree carries commit
`b13cebd` or later (finding #19). Build Release once on the development laptop: `dotnet build SESS.NexaERP.slnx -c Release`.
Publish the deployment artifacts there; do not build or test on DESKTOP-SPF5420.
In steps 2 onward, execute on the server; `<host>` means 127.0.0.1. No remote DB connection from the laptop.

## 0. Preserve what exists (DBA, before anything else)

### Production server and development laptop (decision, 21 September)

DESKTOP-SPF5420 is the production server. Build the fresh Option C database there
using steps 2-14; section 15 records its specification, disk layout and cutover.
The laptop supplies no go-live business data and returns to development and LabVIEW
after cutover. The previous 30 September laptop witness cutoff is
superseded: `NexaERP nightly witnesses` may remain on the laptop after server cutover.
Never install or run that task on DESKTOP-SPF5420. Keep login and cutover work ahead of optional laptop witnesses.

The existing laptop witness tooling and PostgreSQL binaries can remain in place.
The failure-cleanup limitations below still apply; moving production does not cure
them. Retain gate logs/TRX, review failures, and prevent overlapping witness runs.
The frontend developer's PC is no longer required as the replacement witness host.

### Laptop memory-guard inspection (21 September 2026; still relevant to witnesses)

**Partial protection only; complete failure-safe cleanup is not guaranteed.** The
installed task is Ready, daily at 01:00, `MultipleInstances=IgnoreNew`, with no trigger
end date and a 72-hour execution limit. No task settings were changed in this inspection.
The script waits for each gate before the next. The private `DisposablePostgreSql`
helper belongs to the partial `AdvanceMigrationSqlSyntaxTests` xUnit class, whose tests
run sequentially within one test process. There is no machine-wide lock against another
manual or scheduled test process.

Test `using` scopes dispose clusters on ordinary assertion/exception failures, and
startup catches call `Dispose`. But `_started` is set only after `pg_ctl start` succeeds:
partially successful startup can escape stop cleanup. `Dispose` does not check the stop
exit code, and process termination can bypass disposal. The nightly wrapper has no final
cleanup or orphan-cluster check before the next gate. Thus one cluster at a time with
cleanup on every failure is not guaranteed. This was source/settings inspection only;
no heavy witnesses were launched.

### Preserve existing environments

Leave developer databases and their existing evidence untouched. No source backup,
export or restore is an input to this clean installation. This decision does **not**
cancel normal verified backups of the newly configured server: section 15.1 and the
server daily-backup procedure still apply.

## 1. Prepare SESS training inputs, not a database transfer

TD coordinates the 28-30 September setup roster: Stores, Purchase, Accounts, QC, IT,
TD and MD, with separate named maker/checker logins. SESS supplies its own warehouse
and rack plan, receiving routes, approved GST/QC requirements, supplier certificates
and customer details. Use the checked-in item script and two freshly prepared template-v2
opening-stock workbooks based on physical counts and Accounts' carrying values.
Do not request a developer dump, export counts or attachment GUIDs.

**Launch workflow acceptance gate (TD decision after screen-gap review):** no new setup
screens except the frontend developer's vendor commercial-verification action after login.
Warehouses/racks use governed workbook imports, followed by separate Stores submission
and TD approval. Condition locations, category routes, GST and vendor qualifications use
[the reviewed employee-authenticated wrapper](setup-operator-wrappers.md), with independent
decisions/review and read-back. Proper administration screens follow after go-live.
SESS prepares [these exact columns/forms](setup-data/README.md) now; no developer-source
business data is used. Demonstrate the complete assisted workflows in DEMO, including
Keycloak/MFA and D5 operator scopes, before applying the clean-production setup.
The Configuration Page remains deferred; login remains the critical path.
The one frontend addition is [specified here](vendor-commercial-verification-contract.md).

## 1.5 Server OS decision (Technical Director, 21 September)

Windows 10 Pro stays for this deployment, without ESU currently activated. Windows 11
is a planned later upgrade, not a prerequisite for API deployment. Apply the compensating
controls in the server deployment procedure and follow `server-deployment.md`. No clean Windows install.

## 2. Create the database on the server (DBA)

Only AFTER accepted DEMO and its deletion, follow server-deployment.md steps 2-3 with
$db='sess_nexa_erp' on the SERVER. Use the published Installer and bundle from the
hash-verified package. Do not execute database commands remotely from the laptop;
pg_hba stays local-only and there is no 5432 firewall opening. No SDK on the server.

## 3. Provision principals (server-local published Installer)

Set exact ExpectedDatabase and the protected installer DBA connection as documented
in server-deployment.md. Use `$installer database-principals provision` and
`$installer database-principals status`. No dotnet run/build/tool installation.

## 4. Migrate 0 to current (migration login assuming owner)

Run the package's migrate/efbundle.exe on the server with the protected migration
connection and Options=-c role=nexa_erp_owner. No dotnet ef; no owner DB application
from this laptop. Follow the package's post-migration checks and replay rules.

Then, API still stopped, as DBA: `database-principals provision` then `database-principals status`
(mandatory reconciliation after every migration run).

Check: `SELECT count(*) FROM advance."__EFMigrationsHistory"` equals the number of migrations
in the built assembly (117 at `c69366d`; 130 with the thirteen 20 September migrations, which
apply in timestamp order 090000 → 210000 with no other ordering requirement). Expected
configuration rows from the 20 September commits: 3 + 1 page + 9 + 1 + 2 + 2 role page
permissions, 8 audit receipts (the route page copies its 9 grants without receipts; its
rollback compares rows instead), one rewritten trigger function
(`guard_estimated_bom_governance`), two new nullable columns (`OriginOpeningStockLineId`
on `stock_movements` and `material_issue_lines`) with their backfill of existing opening
receipts and three rewritten posting functions (#26), eight provenance columns on the two
opening-stock line tables plus the 22-argument staging overload, and the Actual BOM
entry origin column with its rewritten fitment, reversal and dossier functions, 23 report
export grants (one receipt each) and the Technical Support Manager Estimated BOM preparer
grant (one receipt), the Estimated BOM line `ValueSource` column (existing priced lines
classified) and the rewritten draft-line replacement function, and the A2 stock-adjustment
contract (20260920210000): three new tables (`stock_adjustments`, `stock_adjustment_lines`,
`stock_adjustment_decisions`) with their immutability triggers, five new nullable ledger
columns (`StockAdjustmentId` on batches; `StockAdjustmentLineId` and
`OriginStockAdjustmentLineId` on movements; `StockAdjustmentLineId` on FIFO layers and
consumptions, whose `MaterialIssueLineId` becomes nullable under a one-of check), the
batch-kind, batch-source, movement-contract, outbound-origin and FIFO-layer checks widened,
five rewritten functions (batch/movement/reconcile guards, `consume_fifo_for_issue`,
`company_report_fifo_valuation`), two new functions (`post_stock_adjustment`,
`fifo_carrying_value_preview`), the page `stores.stock-adjustments` with 5 role grants (no
receipts; the rollback refuses once an adjustment exists); zero business rows. `status`
prints VERIFIED.

## 5. Authentication bootstrap and identities (DBA, then API)

Run the one-time ceremony as `nexa_erp_bootstrap` with SESS-12's exact issuer and `sub`
(see `authentication-bootstrap.md` steps 7–12), then sign in as SESS-12 and create every
other employee's identity mapping through the governed identity endpoint
(`security.employee-identities`) for the approved SESS setup roster and new Keycloak
identities. Add only explicitly approved, dated temporary covers if needed; do not copy
mappings or covers from another database. Enable ordinary users after both ceremonies.

Check: `/api/v1/session/me` for SESS-12; each mapped user can sign in and sees their company.

## 6. Prepare fresh supporting documents (SESS team)

Collect vendor GST certificates from SESS's own records. IT Manager uploads each original
through the vendor screen into this database and links the newly returned certificate
reference when creating the vendor (step 11). The create endpoint already requires an
uploaded GST certificate, so upload precedes vendor creation, not final approval.
Upload any customer supporting documents afresh through its screen as needed.
No attachment table restore, old GUID reuse or developer workbook metadata is permitted.
Check each uploaded document opens from the new screen and belongs to the intended party.

## 7. Load the item master (Owner)

```powershell
psql --host <host> --port 5432 --username nexa_erp_migration --dbname sess_nexa_erp -v ON_ERROR_STOP=1 -c "SET ROLE nexa_erp_owner" -f .\database\postgresql\legacy-item-import-2026-08-29.sql
```

Check: `SELECT count(*) FROM advance.items WHERE "CreatedBy"='EXCEL_IMPORT'` = 1,368, all
`Approved`; categories ELE/FAB/REF present; UOMs present. The #11 reconcile script is not
needed on a database the corrected import created.


### Category reconciliation command (Owner)

```powershell
# Run from the repository root after backup; select the intended database explicitly.
# For today's repair use the developer's server at 130; for go-live use the go-live host/database.
$categoryHost = Read-Host 'PostgreSQL host'
$categoryPort = Read-Host 'PostgreSQL port (normally 5432)'
$categoryDatabase = Read-Host 'Target database name'
# Principal: nexa_erp_migration, SET ROLE nexa_erp_owner (not the API runtime login).
psql -X --host "$categoryHost" --port "$categoryPort" --username nexa_erp_migration --dbname "$categoryDatabase" -v ON_ERROR_STOP=1 -c "SET ROLE nexa_erp_owner" -f .\database\postgresql\reconcile-legacy-item-categories.sql
if ($LASTEXITCODE -ne 0) { throw 'Category reconciliation failed; retain output and resolve the reported guard before retrying.' }
# Changes: EXCEL_IMPORT item categories ELECTRICALS->ELE, FABRICATION->FAB,
# REFRIGERATION->REF; increments versions/update metadata and writes GLOBAL item audits.
# Retires active import-owned legacy aliases, with separate category audits.
# Preserves IDs, receipt snapshots and historical QC evidence.
# Refuses: system databases; missing/inactive required canonical categories; active
# legacy QC policies, vendor qualifications or Stores routes; affected imported items
# with subcategories; active non-import items or active subcategories preventing alias retirement.
# The script owns one SERIALIZABLE transaction: any error rolls the repair back.
# Replay: paste this same block with the same target. With no new affected data it
# changes zero items/aliases and appends zero audits. Do not rerun the original import as a repair.
# Check afterwards: exit 0 and COMMIT; corrected_items sum = 1,087 for today's
# guarded database at 130 (go-live: use its own preflight count, possibly zero).
# Check zero EXCEL_IMPORT items remain on the three legacy codes; ELE/FAB/REF are active;
# retired_import_aliases matches the active import-owned aliases (normally 3).
# Audit deltas: ReconcileImportedCategory = corrected items; RetireImportedCategoryAlias
# = retired aliases; Scope GLOBAL, UserLoginId nexa_erp_migration, ActorRoleCode INSTALLER.
# Replay check: zero corrected items/retired aliases and zero additional audit rows.
# Before receipts, configure effective canonical Stores routes, QC policies and vendor qualifications.
```

**Item baseline.** The checked-in script is the approved source; no comparison with a
developer database is required. SESS reviews the loaded item list. Any necessary screen
correction follows the existing item maker-checker workflow; do not replay the original
import to repair a used database. Keep canonical ELE / FAB / REF active.

## 8. SETUP-BEFORE-FIRST-GRN: warehouse and receiving topology

Complete steps 8-11 through the accepted screens/imports/API wrappers during **28-30 September**, in the
order below. Repeat company-scoped setup for **SESS_PROPRIETORSHIP and SESS_PVT_LTD**;
shared parties need not be duplicated. The named roles below are the training assignment,
not an exhaustive list of all permission holders. Record IDs/codes, company, versions,
effective dates, maker/checker identities and approval receipts in the training record.
A saved draft or HTTP success alone is not evidence that a setting is effective.

**Configuration entries do NOT create stock movements and therefore do NOT block the
opening-stock ceremony. ANY GRN, issue or adjustment before BOTH opening-stock ceremonies
are POSTED is forbidden and can make a ceremony refuse.** Do not create even test drafts
of those transactions on go-live. The database's existing-movement refusal is per company;
the operational gate is deliberately stronger: both companies must finish before either
starts transactions. The two authorised opening postings are the only planned exceptions.

| Order / entry | WHO enters; maker-checker rule | Check before marking complete |
|---|---|---|
| 8.1 Warehouse, per company | Stores Manager creates and submits; a different Technical Director approves through the warehouse lifecycle. | Correct selected company/code, Active, Approved, IsActive; inspect approval history and responsible employee assignment. |
| 8.2 Rack/bin under each warehouse | Stores Manager creates and submits; a different TD approves. Set material condition deliberately before a condition location references it. | Active/Approved bin belongs to the intended company and warehouse; its condition agrees with its planned use. Provide AVAILABLE, QC_HOLD and PENDING_RETURNABLE_DC bins for the receiving route. |
| 8.3 Condition location for each required bin | Stores Manager creates an effective version. Current API has create/list/close, **no separate approval stage**. TD performs and records an independent operational review; this is not an enforced maker-checker approval. | Effective-location read shows the correct company, warehouse, bin and condition, active on the opening date and 1 October. AVAILABLE location exists for every opening-stock bin; required QC_HOLD and PENDING_RETURNABLE_DC locations exist for receiving. |
| 8.4 Category route, explicitly **ELE**, **FAB**, **REF** in each company | Stores Manager creates after 8.3. Current API has create/list/close, **no separate approval stage**; TD records independent review. | Exactly one effective route per canonical category on the intended receipt date; all three location references are effective, in the same company and one warehouse, with their required conditions. No legacy category aliases or overlapping routes. |

The diagnostic read paths behind the screens are `/api/v1/inventory/warehouses`,
`/api/v1/inventory/rack-bins`, and `/api/v1/rev869a/configuration/` followed by
`warehouse-condition-locations?effectiveOnly=true` or `store-category-routes?effectiveOnly=true`.
Use the wrapper Read action and independent TD review for verification; do not prove routing by posting a GRN.

## 9. SETUP-BEFORE-FIRST-GRN: GST rules

**Accounts Manager** enters applicable GST rules after reviewing company/state, item
HSN/SAC, supplier registration and place-of-supply combinations. A **different TD or MD**
approves; the service refuses the creator's own decision. Do not treat an item's display
GST percentage as a substitute for an effective tax rule.

Check the tax/GST read view (`/api/v1/rev869a/configuration/tax-gst`) for Approved, active,
correct effective dates covering the intended transaction date, matching jurisdiction,
HSN/SAC, supply states and registration type; confirm CGST/SGST or IGST, exemption/RCM,
ITC and rounding with Accounts. Retain rule ID/version and independent decision evidence.
Resolve missing or conflicting coverage without creating a production receipt.

## 10. SETUP-BEFORE-FIRST-GRN: QC policies

**QC Manager** creates item- or canonical-category inspection policies; a **different TD**
approves. Creator self-approval is refused. Confirm the required coverage for ELE / FAB /
REF and any item-specific policy, parameter/UOM, limits, method, sample size and dates.

Check the QC Inspection Policies screen/read (`qc-inspection-policies` under the same
configuration API) shows Approved and active in the correct company, effective on the
intended receipt date. Pending policies are not ready. Missing QC policy must not be
worked around by releasing stock; retain approved policy IDs/versions. Verify required
UOMs already exist from the item baseline; resolve additional governed UOM/conversion
requirements before use, without changing stock.

## 11. SETUP-BEFORE-FIRST-GRN: vendors and customers

| Order / entry | WHO enters; maker-checker rule | Check before marking complete |
|---|---|---|
| 11.1 Vendor GST certificate, then vendor create/submit | IT Manager uploads the certificate and creates/submits the vendor through the screen. The creation request must reference this database's uploaded GST certificate. This is document validation, not a separate certificate-approval workflow. | Certificate opens from the screen and its identity is reviewed against the vendor; required GST/PAN, addresses and commercial details are correct. No copied attachment IDs. Retain vendor code and upload evidence. |
| 11.2 Vendor commercial verification | Accounts Manager checks commercial/bank/GST information and performs verify-commercial with remarks. Keep this actor separate from the maker. | Retain the successful AccountsVerify action and refreshed approval history with actor/date; the vendor remains Pending Approval. The current VendorDetail DTO does not expose commercial-verification fields: use the [frontend contract](vendor-commercial-verification-contract.md), not an invented status field. |
| 11.3 Vendor final approval | Managing Director, the effective VENDOR_FINAL_APPROVER policy role, approves independently of the maker. Commercial verification is mandatory; the master lifecycle refuses self-approval of the current maker's submission. | Vendor Active and Approved, commercial verification current, code locked; retain approval history. A controlled commercial change requires re-verification and approval. |
| 11.4 Vendor qualification per supplied canonical category and company | Purchase Manager creates; TD verifies; MD approves. Three distinct employees: creator cannot verify/approve and verifier cannot approve. | Qualification is Verified and Approved, active and effective for the company, vendor and ELE/FAB/REF category actually supplied. Retain the qualification and decision IDs; mere vendor approval is insufficient. Do not qualify categories a vendor cannot supply. |
| 11.5 Customer | IT Manager creates/submits; a different TD approves through the customer lifecycle. Unlike the vendor, the customer has no vendor-style verify-commercial step. | Customer Active and Approved; correct identity/GST/contact/billing/shipping details and applicable company relationship; inspect independent approval history. Accounts reviews commercial/credit values with its permitted access. |

Check the vendor and customer screens and approval histories; qualification diagnostics
are at `/api/v1/rev869a/configuration/vendor-qualifications`. Purchase enters necessary
item/vendor links after both masters exist. Confirm selected-company visibility and any
required party/company relationship through the supported screens. Do not duplicate a
shared party merely to make it appear in another company.

**Setup exit check (TD with Stores, Accounts and QC):** all checklist receipts complete;
all dates cover their intended use; all six company/category route combinations checked;
required suppliers qualified and certificates readable; customers approved. Use read-only
stock-ledger checks to confirm **zero stock movements in each company before its own
opening ceremony**. Configuration audit/history records are expected and must be retained.

### Checklist source checks

The role/lifecycle checks above were reviewed against
[warehouse/rack endpoints](../../src/SESS.NexaERP.Api/Endpoints/InventoryEndpoints.cs),
[master maker-checker enforcement](../../src/SESS.NexaERP.Api/Endpoints/MasterEndpointHelpers.cs),
[party endpoints](../../src/SESS.NexaERP.Api/Endpoints/MasterEndpoints.cs),
[vendor verification/final approval](../../src/SESS.NexaERP.Api/Endpoints/MasterEndpoints.Rev869A.cs),
[certificate validation](../../src/SESS.NexaERP.Api/Endpoints/MasterEndpoints.VendorAttachments.cs),
[configuration endpoints](../../src/SESS.NexaERP.Api/Endpoints/Rev869AConfigurationEndpoints.cs),
[category routes](../../src/SESS.NexaERP.Api/Endpoints/Rev869AConfigurationEndpoints.StoreCategoryRoutes.cs),
[QC decisions](../../src/SESS.NexaERP.Api/Endpoints/Rev869AConfigurationEndpoints.QcPolicies.cs), and
[GST workflow](../../src/SESS.NexaERP.Infrastructure/Masters/EfTaxGstWorkflowService.cs).
This is source review, not a server or screen witness; retain actual training evidence.

### Recovery and replay

A configuration error is corrected through its supported governed workflow, preserving audit history.
The initial-entry wrapper does not implement closures/edits or silently replace used records.
A timeout requires reading the result first. The wrapper blocks uncertain PENDING receipts
for reconciliation; do not delete them or assign a new key to force a retry. Changed input is a new action after
resolving the prior outcome. Never casually drop the go-live database or erase training
and approval evidence because no stock has yet moved.

Use normal verified server backups for recovery checkpoints, including after setup and
after both ceremonies. These are backups of this new installation, never source inputs
from the frontend developer. A restore/restart needs a separately reviewed recovery plan.
After authorisation may have succeeded, inspect the posted receipt before any retry;
do not restore an earlier checkpoint over subsequent business activity.

## 12. Opening stock, SESS_PROPRIETORSHIP (API, three actors)

Stores Manager: `POST /api/v1/master-data/opening-stock/import` with a freshly prepared template-v2
workbook during 28-30 September, then `POST /api/v1/stores/opening-stock/from-import`. Accounts Manager:
`…/confirm-value`. Technical Director: `…/authorize`.

Check: status POSTED; `stock_movements` count for the company equals the workbook line
count; FIFO layer value equals the Accounts-confirmed physical-count workbook total.
Do not copy the developer ceremony or its balances. Template and preparation rules in
step 13 apply equally to this company.

## 13. Opening stock, SESS_PVT_LTD (API, three actors) — the one step with no rehearsal

PVT LTD's opening stock has never been posted anywhere. The PROPRIETORSHIP ceremony was
done once on the developer's machine, so its shape is known; PVT LTD will be the first
time with real quantities. Complete both ceremonies on 28-30 September; daily
transactions start 1 October. Prepare and independently review both workbooks in advance.

**Who prepares it.** The Stores Manager (SESS-41) prepares the physical count; Accounts
(SESS-14) supplies the rate per line from the carrying-value policy; the Technical
Director authorizes and does not prepare. The count is a physical count of what is on the
racks on the cutover date, not a copy of any earlier system's balance.

**What it must contain.** One row per item, warehouse, rack and (where the item is tracked)
lot or serial, in version 2 of the `opening-stock` template — `docs/installation/opening-stock-template-v2.xlsx`
now, `GET /api/v1/master-data/opening-stock/template` once the database exists. Values are
**ex-tax**. The optional provenance columns (vendor name, vendor bill number, bill date,
purchase date, make, model, part number, remarks) are filled where SESS knows them and left
blank where it does not; they never change the value:

| Column | Rule |
|---|---|
| `LineReference` | unique within the workbook (e.g. `PVT-OPEN-0001`); it is the operator's stable row reference in every later report |
| `ItemCode` | an approved, active item — exactly as loaded by step 7 |
| `WarehouseCode` | an active PVT LTD warehouse created in step 8 |
| `RackBinCode` | an active rack of that warehouse whose condition is AVAILABLE and that has an effective AVAILABLE condition location (step 8); the rack is mandatory and must come from the physical-count plan |
| `LotNumber` | required for batch-tracked items, blank otherwise |
| `SerialNumber` | required for serial-tracked items; one row per unit with `Quantity` = 1 |
| `Quantity` | greater than zero; zero-quantity lines are left out, negative is refused |
| `Unit Value Ex-Tax` | the taxable unit value in rupees without GST, zero or more; the stated legacy value is deliberately not imported and the line value is recomputed as Quantity × Unit Value |
| provenance columns | optional; declared, not verified; shown in the machine dossier as "declared at opening stock, not verified in this system" |

Items with lot or serial tracking are the rows most likely to be wrong on the day: get the
serial lists from the racks, not from the old system. A row that fails validation stops
nothing — the import reports it and the corrected workbook is re-imported under a new
idempotency key before the count is recorded.

**What the ceremony then does.** Stores Manager: import, then `from-import` with the
fiscal period (1 April – cutover date). Accounts Manager: `confirm-value`. Technical
Director: `authorize`. The authorization is the moment PVT LTD acquires stock movements;
after it, the rule "company already has movements" applies to PVT LTD for good.

Check: status POSTED; movement count equals the workbook line count; FIFO layer value
equals the confirmed total; `GET /api/v1/reports/…` stock and FIFO valuation agree.
Only after BOTH ceremonies are POSTED and ordinary use opens on 1 October, the first MIR: until `20260920150000_OpeningStockIssueOrigin` (finding #26) no
opening-stock unit could be issued at all; the rehearsal now issues, returns and fits
opening stock, values the fitment at the confirmed ex-tax value, and shows it in the
dossier as opening stock. Opening stock consumes before any later receipt of the same
item (FIFO dates it from the period end above).

## 14. Close

- Reconciliation: DBA `database-principals provision` then `status` → VERIFIED.
- Backup: run the server verified-backup job from section 15.1; retain its VERIFIED
  manifest and successful restore evidence, labelled go-live. Verify identity and
  external ERP files through their separate backup procedures too.
- **No walk-throughs in either company from this point.** The opening-stock rule refuses a
  company that already has movements; there is no legitimate escape from it.

## Release for daily transactions: 1 October

TD records completion of training on 28-30 September, setup checklist evidence and both
POSTED template-v2 ceremony receipts; Accounts confirms values and Stores confirms counts.
Also require accepted production login, other-PC/reboot checks and verified backup evidence
from the server deployment procedure. Only then release ordinary users on 1 October.
No developer-source row counts or dump delivery is a release dependency.

## 15. DESKTOP-SPF5420: current operating specification

> **Protected server rule (21 September, 16:01):** NEVER stop, disable, modify or remove
> SESS_SQLEXPRESS, TEW_SQLEXPRESS, SQLBrowser, their ~77 SOLIDWORKS project databases,
> ewserver, ANY NI or Siemens service, ANY Rockwell FactoryTalk service, IIS Default
> Web Site or /Updater. Leave Wamp stopped/manual. Windows 10 stays; NEVER a clean
> Windows install; NEVER install a .NET SDK on this server. All application here is
> by the server agent, not from the laptop. See C:\SESS-ServerPrep for completed preparation.

These rules apply to every linked deployment, identity, certificate and backup procedure. Stop and report a conflict; do not reclaim ports or memory from protected services.

Latest facts are [server-facts-20260921.md](server-facts-20260921.md), 16:01:
6.4 GB RAM free, C:41.6 GB free, PG 17.11 local-only pg_hba, .NET10.0.12 runtime,
no SDK. Already prepared controls are recorded at C:\SESS-ServerPrep; do not redo.
Use [server-deployment.md](server-deployment.md) for the authoritative SDK-free,
server-local DEMO-first installation. Earlier laptop/remote dotnet ef examples in
this document are historical development guidance and MUST NOT be used on the server.

Windows 10 stays, no clean install. A later compatible in-place Windows11 upgrade
remains planned because standard commercial ESU costs US$61/$122/$244 per device
in successive programme years (US$427 total), cumulative purchases, taxes/reseller
pricing extra; ESU has not been activated by this decision. Check CPU/TPM2.0/UEFI
Secure Boot/driver compatibility, backup first, and verify protected services,
PostgreSQL, .NET and firewall after any later approved upgrade.
[Microsoft ESU terms](https://learn.microsoft.com/en-us/windows/whats-new/extended-security-updates).

ERP is a Windows service on https://192.168.68.130:8443. Keycloak is local native
Java 17 on 8444, its own sess_keycloak database in the same PG cluster. Boundaries,
ports, memory pools and no-login startup checks are in the linked server procedures.
Keep all protected engineering workloads running; add no development or general
work to the server. Eleven transactional users do not require more than the stable
100Mbps link; allow extra transfer time for daily off-machine copies.

### 15.1 Backup and capacity supersede all earlier D:/E: directions

D:/E: share Disk1 with documented hardware/filesystem errors. Neither may hold new
backups or restore work. Follow [daily backup procedure](server-daily-backups.md):
C:\SESS-Backups component roots, C:\SESS-Backup-Verification, and DAILY verified
copy to another PC plus receiver-owned protected archive. Keep 25 GiB free, only two
verified local bundles per DB, prune only with matching off-machine copies. The
old assertion that 40 GB fits six months is withdrawn for this combined layout:
ERP/identity growth, WAL, dumps and restore peaks must pass the measured space gate.
At a 5-8 GB database this can require more SSD space; fix capacity before that point.
Move to a healthy/replaced HDD only after later acceptance; daily off-machine remains.

### 15.2 Cutover and laptop release

DEMO first, accepted frontend/login/MFA/other-PC/reboot/backup witnesses, drop DEMO,
then fresh Option C sess_nexa_erp directly ON THE SERVER. No stock-moving command
before BOTH opening ceremonies are posted. Admit only named setup/ceremony operators
until both receipts exist; not even a test GRN beforehand.

Only AFTER successful cutover does the laptop return to development/LabVIEW: its
production-hours rule ends, NI services return with RESTORE-NI-Siemens.ps1 and the
nightly development witness can remain. This applies ONLY to the laptop; NEVER run
that maintenance script on the server. Server NI/Siemens/Rockwell stay running.
Backup verification uses one private disposable cluster at a time; no laptop tests
or builds, stock tests or owner-data experiments run on the server.
