# Finding and open-item status — 24 September 2026

One place for the list to live. Written after finding #29 (then called #12, see below) was
discovered already shipped on 20 September while still sitting open on the Technical
Director's list, which would have cost a full suite-and-gate cycle to re-confirm.

**How each line was decided.** Shipped means the change is in `src` or `database` on `main`,
with the commit or migration named below, and I checked it is an ancestor of `origin/main`
rather than sitting in a local tree. Superseded means a later decision removed the work.
Open means no code exists. Nothing here is a field witness: a shipped backend change is not
a claim that it has been applied to any server database.

**Rule, set by the Technical Director on 24 September after #33: a configuration is
verified by reading the live system, never by reading what we meant to send it.** An
export, a script, a click list or a commit proves the intention. Only a read-back from the
running system proves the state. #33 is the case that made it a rule. The realm file said
*password and OTP REQUIRED*, the static check on that file passed, and the live Approvers
flow had no OTP step at all. **Before Step 6, the Staff flow, both clients' redirect URIs
and grants, and the MFA trust mapping are read back from the running Keycloak and the
deployed API configuration, not from the exports.**
[tools/identity/Read-KeycloakLiveConfig.ps1](../../tools/identity/Read-KeycloakLiveConfig.ps1)
does that, read-only, as PASS/FAIL lines. The same holds beyond identity: a grant, a
scheduled task, a firewall rule or a certificate is checked where it runs.

**Updated the same afternoon.** The RFQ investigation is closed, finding #4 is found, #11
is corrected, and support-role grants are a category of their own. The morning version was
wrong on two lines, #4 and #11, and the cause is below, because it will recur.

**Two finding lists share one number range.** The grid walk of 19 September
([status of 19 September](../SESS_NexaERP_Status_19_September.md)) numbers 1 to 16. The
fresh-company rehearsal ([findings](fresh-company-rehearsal-findings.md)) numbers 12 and 17
to 28. They collided at 12: the grid walk's was the Actual BOM valued at taxable, the
rehearsal's was QC unable to read the receipt.

**Decided by the Technical Director, 24 September:** new findings take #29 onward, and no
number is ever reused. **Neither finding keeps the bare "#12".** The grid-walk one is
**grid #12**. The rehearsal one is renumbered **#29**, with **rehearsal #12** as its alias. Two weeks
of messages say "#12" meaning one or the other. **A message about QC reading the GRN, the
`STORES_ASSISTANT` support cover or `d36b001` means #29. A message about the Actual BOM, the
−1,250 or `c94897c` means grid #12.**

**Why the morning version said "#4 not found".** The search was for the string "#4", and
both places that record the finding write it as "Finding 4": the first line of the
reconcile script, and [item-category-import-correction.md](item-category-import-correction.md).
The same miss put finding 4's evidence on #11. **The next search will have the same problem.
Search for `#N`, `Finding N` and `finding N`, across `docs/`, `database/`, `tools/` and the
commit messages, before calling any finding missing.**

## Findings

| # | Status | Evidence | Note |
|---|---|---|---|
| 3 | **SHIPPED** | `f7f0c24` — *Scope technical verification to the verifier department*, 19 Sep | Also resolves #17. |
| 4 | **SHIPPED** via `database/postgresql/reconcile-legacy-item-categories.sql` | `5b903eb`, *Reconcile imported items to canonical Stores categories*, 19 Sep | The GRN three-character mismatch: imported items sat on ELECTRICALS/FABRICATION/REFRIGERATION while the racks used ELE/FAB/REF. The import now maps to the canonical codes, and the script repairs rows imported before that. **Runbook step 7: ELE, FAB and REF must exist and be active first**, and the script refuses with *Create or activate canonical ELE/FAB/REF categories before reconciliation* otherwise. On the developer's database it corrected 1,087 items (923 FAB + 164 REF), as reported by the developer and not witnessed here. **Not needed on a fresh go-live database** built by the corrected import, where the preflight count may legitimately be zero. |
| 11 | **SHIPPED** | `04036dd`, *Allow Accounts to read GRNs for vendor billing*, migration `20260918091000_AccountsGoodsReceiptRead` | Grid walk: Accounts can read the GRN, so the vendor bill screen can work. **Corrected this afternoon**: the morning version showed #4's evidence here. The runbook said "the #11 reconcile script", the same slip, and now says finding 4. |
| grid #12 | **SHIPPED** | `c94897c`, *Value Actual BOM from governed tax recovery snapshots*, 18 Sep | The Actual BOM valued at taxable. The −1,250 matched the prediction. |
| 17 | **SHIPPED, as a duplicate of #3** | `f7f0c24` | Not a separate defect. The 500 the frontend developer saw predates that commit. A probe is appended to `local-evidence/finding-17/tsm-probe.jsonl` on every rehearsal run, so a regression shows as a status other than 200. |
| 18 | **SHIPPED** | `20260920090000_StoresWarehouseRackGrants`, `20260920100000_StoreCategoryRoutePage` | The grant had belonged to `STORE_HEAD`, a legacy role held by nobody. |
| 19 | **SHIPPED** | `b13cebd` — *Make migrations tolerant of CRLF worktrees*, plus `LineEndingNormalizingMigrationsSqlGenerator` and `MigrationLineEndingToleranceTests` | Runbook precondition stands: the pulled worktree must carry `b13cebd` or later before any migration runs. |
| 20 | **SUPERSEDED** | No-dump decision, 21 Sep | Not fixed and not to be fixed. The data carry-over it existed for is not happening. SESS uploads its own GST certificates through the vendor screen into the clean database; no attachment-table restore, old GUID or developer workbook is permitted. What remains is an operational sequencing rule at setup, not a code item. |
| 21 | **SHIPPED** | `20260920110000_AccountsVendorCommercialVerificationGrant` | `ACCOUNTS_MANAGER` can now perform commercial verification, so vendors can reach final approval. |
| 22 | **SHIPPED** | Verification now resolves `CompanyId` the way the re-verification path already did | Returns 400 if the company cannot be resolved, instead of 500 from a failed save. |
| 23 | **SHIPPED** | QC resolves the pending-return location from the receipt line's route snapshot (`StoreCategoryRoute.PendingReturnConditionLocationId`) | The old rule demanded two conditions on one rack, which only the trial script could produce. Every customer installation would have failed at its first inspection. |
| 24 | **SHIPPED** | `20260920140000_ItemMergeDirectorAuthority` | Rewrites the installed `guard_estimated_bom_governance` body. Merge stays Technical Director only, per reading (a). |
| 25 | **OPEN** | — | `company_sites` and `customer_company_relationships` have no API and no seed, so on a fresh database no intercompany route can be proposed and the DC-only transfer path has no endpoint at all. This is **A1 in the backlog, 6-10 days**, and is not a launch deliverable. The rehearsal asserts the empty options and stops there. |
| 26 | **SHIPPED** | `20260920150000_OpeningStockIssueOrigin`, `…160000_OpeningStockProvenance`, `…170000_OpeningStockFitmentValuation` | Opening stock could never be issued at all before this. Provenance columns are declared, never verified, and never change value. |
| 27 | **SHIPPED** | `EfMaterialIssueService.CreateReturnAsync` now subtracts fitted quantity net of reversals | The engineer no longer has to declare a fitted unit "consumed" to return the rest. |
| 28 | **SHIPPED** | Service refuses an unknown MIR purpose with 400 and the allowed list | Previously reached `CK_mir_lifecycle` and returned an internal error. |
| **29** (alias **rehearsal #12**) | **SHIPPED** | `d36b001`, migration `20260920120000_QcGoodsReceiptRead` | QC could not read the receipt it inspects. **This is the one that stayed on the list.** `QC_MANAGER` *and* `STORES_MANAGER` get `inventory.grn` view only: no create, finalize, reverse or download. Its field workaround is a support grant, tracked below. |
| **30** | **COMMITTED `84b8698`, not yet pushed** | Setup wrapper, `tools/setup/SetupOperator.psm1`; 44 wrapper checks pass | The wrapper replaced every refusal with "HTTP 409" and hid the server's reason. A go-live blocker for setup on 1-3 October. See below. |
| **31** | **OPEN, converter approved** | Found 24 Sep, RFQ investigation | A local-offset timestamp on any of **13** request fields (listed below) gives a 500 the user cannot act on. **The frontend must send UTC on all 13 before 1 October, whatever the backend does.** The converter is a safety net, not a substitute. |
| **32** | **OPEN** | Found by the developer, 24 Sep | SESS-16's untracked `TECHNICAL_ENGINEER` support grant. See *Support-role grants*. |
| **33** | **OPEN: repair approved, the Technical Director runs it on the server**; scripts committed `54b5ba9`, not yet pushed | Server Step 4 Check B, 24 Sep about 17:40 | **Approvers log in with password only.** On the server, `nexaerp-approver-browser` holds one step, the Username Password Form. The OTP Form is missing, although the flow's own description says *Mandatory password and OTP*. See below. **Step 6 stays on hold until it is repaired and a fresh login is witnessed.** |

## Support-role grants

**A workaround nobody is tracking is a finding.** A `SUPPORT` assignment gives an employee
another role's authority without making it their job, so each one is here with the reason it
was granted and the condition that ends it. When a finding ships, its support grant is ended
through the role-assignment workflow. Ending one requires `EndReason`, `EndedAt` and `EndedBy`
(`CK_employee_role_assignment_end_metadata`), so the end is recorded rather than deleted.

| Holder | Support role | Granted | For | Still needed? | Status |
|---|---|---|---|---|---|
| Not recorded here | `STORES_ASSISTANT` | Not recorded here | #29 (alias rehearsal #12), so QC could read the GRN it inspects | **No.** `d36b001` gives `QC_MANAGER` its own view-only GRN grant | Being ended 24 Sep, per the Technical Director. **Not closed on this list until the ended row is read back** with its `EndReason`. |
| SESS-16, KAMALI SRINIVASAN (whose own FULL role is `STORES_ASSISTANT`) | `TECHNICAL_ENGINEER` | 16 Sep | **Unknown.** It was untracked until the developer found it on 24 Sep | **Cannot say until the reason is known.** Its `Remarks` column should say why | Active. **Open finding: needs an owner and a decision to keep or end it.** |

**This list is only what has been reported, not an audit of any database.** A database
could carry support grants that nobody has mentioned. Running this read-only query on each
database closes that gap. **The developer runs it on `sess_nexa_erp` on 24 September**, and
the output is recorded here. On the fresh go-live database it should return nothing until someone grants support
deliberately, and every such grant starts with a row on this list.

```sql
SELECT c."Code" AS company, e."EmployeeCode", e."EmployeeName", r."Code" AS support_role,
       a."EffectiveFrom", a."EffectiveTo", a."Remarks", a."CreatedAt", a."CreatedBy",
       a."EndReason", a."EndedAt", a."EndedBy"
  FROM advance.employee_role_assignments a
  JOIN advance.employees e ON e."Id" = a."EmployeeId"
  JOIN advance.roles r ON r."Id" = a."RoleId"
  JOIN advance.companies c ON c."Id" = a."CompanyId"
 WHERE a."AssignmentType" = 'SUPPORT'
 ORDER BY (a."EffectiveTo" IS NULL) DESC, a."EffectiveFrom";
```

## The seven setup screen gaps

**No screen is being built before go-live, and that is the decision, not a gap.** Each has a
governed launch path today; the screens themselves are backlog at 8-14 days total.

| Gap | Launch path | Screen |
|---|---|---|
| 8.1 Warehouse | Governed workbook import, Stores submits, different TD approves | Open, backlog 1-2 |
| 8.2 Rack/bin | Same lifecycle, after warehouses are approved | Open, backlog 1-2 |
| 8.3 Condition location | Wrapper create/read; API has **no approval stage**, so TD review is an operational check, not enforced maker-checker | Open, backlog 1-2 |
| 8.4 Category route | Wrapper create/read, same caveat | Open, backlog 1-2 |
| 9 GST rule | Wrapper; Accounts makes, a different TD or MD decides, creator self-decision refused | Open, backlog 2-3 |
| 11.2 Vendor commercial verification | **The frontend developer's launch deliverable**, after login | Required for launch, not backlog |
| 11.4 Vendor qualification | Wrapper; Purchase creates, TD verifies, MD approves, three distinct employees | Open, backlog 2-3 |

## Self-audit items

Everything in the 22 September self-audit that was code or documentation is now committed:
D4 certificates, the dashboard contract, canonical categories and Keycloak monitoring, the
E cleanup revision, the F proposal (**do not build**), the G laptop investigation, and the
H backlog. The audit's own remaining column is almost entirely **field witnesses**, which no
commit can close: TD/router reservation of 192.168.68.130, the backup receiver hostname
(still `RECEIVER_NOT_SET`) and a witnessed missed-day failure, root-key custody, the D4→D5
server sequence and its STOP receipt, the signed scope roster, named-employee wrapper
rehearsal, production login, other-PC and reboot checks, DEMO acceptance and its governed
drop, and both opening-stock ceremonies.

Two items from the audit have moved since:

- **The memory guard** is no longer only a limitation. It produced four orphan clusters on
  22 September, which were removed on 23 September, and the fix is approved and scheduled
  after go-live in [the harness resilience proposal](harness-resilience-proposal.md).
- **The next commissioning package** still has to carry the accepted payment, QC, GRN and
  opening-stock concurrency fixes, and now also the failure-reporting fix at `7003c02`.
  Package 662e9a3 predates both and is deliberately unchanged for DEMO.

## Still genuinely open before go-live

Nothing in the findings list above blocks 8 October. What blocks it is mostly field work,
not code: production login, DEMO acceptance and drop, the fresh Option C build on the server,
certificate trust on eleven PCs, setup on 1-3 October, both ceremonies on 5-6 October.

Two items below are not field work and **must be settled before 1 October**. The frontend
must send UTC on all 13 timestamp fields listed under #31, whatever the backend does; a
local offset is expected to fail the GRN, gate entry, QC and material issue paths the same
way it failed the RFQ. And the SESS-16 support grant (#32) needs an owner and a decision.

**#30: the setup wrapper now prints the server's reason on business refusals.** During setup on
1-3 October, an operator whose category route was refused used to see only "HTTP 409". For a
400, 404, 409 or 422 from the ERP API, `Invoke-SetupHttp` now adds a `Server said:` line
under its own, carrying the envelope's `Detail`, for example *Route requires effective
same-company QC_HOLD, PENDING_RETURNABLE_DC and AVAILABLE condition locations.* Three cases
keep the original wording only: 401 and 403, every 5xx, and every identity-provider call.
Only that one named field is printed, never the body, so a body carrying a token prints
nothing extra. Commit `84b8698`, not yet pushed.

### #33 Approvers MFA flow missing its OTP step

**What the server shows (Step 4 Check B).** The Approvers realm is bound to
`nexaerp-approver-browser`, and the built-in `browser` flow is not in use. That flow holds
one step, *Username Password Form: Required*, with no OTP Form and no sub-flow. The client
`nexaerp-approvers` has no flow override, and its capabilities are right: standard flow only,
PKCE S256, and direct grants, implicit and device grant off. **The missing OTP Form is the
only gap.** Check C (realm settings) passed in both realms.

**Why it matters more than a login screen.** The API trusts the Approvers realm for MFA as a
whole: `MfaGuaranteedByProvider=true` in `authentication.server.keycloak.json`. It reads no
per-login claim. So while the flow is password-only, **a password-only Approvers token passes
the API's MFA check for TD, MD, Accounts Manager and CFO.** Nothing in the API can detect
this; the realm flow is the whole guarantee, as [server-keycloak-realms.md](server-keycloak-realms.md)
step 6 says. Step 6 (identity mapping) is on hold, and no ERP mapping to an Approvers subject
has been made, so no ERP access has been exposed. That holds only while Step 6 stays on hold.

**Why the OTP Form is missing: not established.** The laptop's source is correct.
`docs/installation/keycloak/server-realms/approvers-realm.json` (`f910f2f`) declares both
executions REQUIRED, the password form at priority 10 and `auth-otp-form` at priority 20.
It is identical in structure to the witness fixture `sess-approvers-realm.json`, which passed
earlier Keycloak witnesses. **This repository holds no "Step 4 realm script"**: the realms
were meant to come from that JSON through `kc.bat import --override=false`, or through the
Admin Console's *Create realm* with the file. So the script that ran on the server was
written there, and only the server agent can show it. Three explanations fit and should be
checked in this order:

1. The realm was created some other way, by a server-side script or by hand, and the JSON
   was never imported. `--override=false` then skipped the file silently.
2. A server-side script built the flow through the admin API and added only the first
   execution.
3. Keycloak 26.7.4's importer dropped the second execution. This is the least likely, since
   the same structure imported correctly before, and it would need the import log to show it.

**Needed from the server:** the Step 4 script source, and the `kc.bat import` output if
there is one. **The lesson either way:** the static check on the export, *REQUIRED
OTP/password*, proved the file and not the realm. The first check that looked at the live
flow found the gap. **A flow is verified by reading the live executions, never by reading
the file that was supposed to create them.**

**Repair: [tools/identity/Repair-ApproverOtpFlow.ps1](../../tools/identity/Repair-ApproverOtpFlow.ps1)**,
run on the server by the identity maintainer. It adds the OTP Form as **REQUIRED, not
conditional or alternative**, to `nexaerp-approver-browser`, and prints the executions
before and after. It then checks, reading back from Keycloak:

- the realm binding;
- exactly two top-level executions, both REQUIRED;
- OTP after password;
- Configure OTP enabled and default.

It refuses to change anything if the flow holds anything else, if the binding differs, or
if the client overrides the flow. `-VerifyOnly` changes nothing. `-SignOutApproverSessions`
ends every Approvers session, so no token issued under the password-only flow stays usable;
run the repair with it. It prints no password or token, and it touches nothing outside that
one flow. Its first run is the witness; nothing here has run it against Keycloak.

**Decided by the Technical Director, 24 September:** the repair runs **with
`-SignOutApproverSessions`**. That nothing is exposed yet, because no ERP mapping exists, is
an accident of timing and not a control. The Technical Director runs it and records the
before and after steps here.

**Step 6 stays on hold until the Technical Director has personally witnessed** one fresh
Approvers login that asks for the password AND the authenticator code, and a wrong code
being refused (step 5 of the realm procedure). The script reporting PASS is not enough.
Before Step 6, `Read-KeycloakLiveConfig.ps1` must also show all PASS, against the live realms
and the deployed API configuration. An
Approvers account that signed in while the flow was password-only has no authenticator yet.
At its next login the OTP Form makes it enrol (the Configure OTP action), which is correct,
and it must be that person's own device.

**Staff is meant to be password-only, and a Staff login never counts as MFA. Confirmed by
the Technical Director, 24 September: keep it that way.** Confirmed
in three places. [server-keycloak-realms.md](server-keycloak-realms.md) step 3: *Staff
retains its ordinary browser flow; it does not grant the API's trusted-MFA guarantee*. The
Keycloak click list: *Staff OTP enrollment stays optional*. And the API configuration,
`MfaGuaranteedByProvider=false` for Staff, with no MFA claim configured. Even a Staff user who
chooses to enrol an authenticator gets no MFA credit, and a Staff token for a TD, MD,
Accounts Manager or CFO role is refused with `MFA_REQUIRED`.

**"Login with email": decided OFF in both realms, Technical Director, 24 September.** One
person, one sign-in name: the employee code. ON was never a decision. Neither realm export
sets `loginWithEmailAllowed`, so it was Keycloak's default, and a setting nobody chose is the
kind that surprises someone later. Turn it off in the Admin Console: *Realm settings > Login
> Login with email: Off*, in both realms. `Read-KeycloakLiveConfig.ps1` checks it live. The
exports get `"loginWithEmailAllowed": false` so a rebuilt realm cannot bring the default
back, but by the rule above the read-back is what counts.

### RFQ `NpgsqlTransaction`: closed

**Cause.** The client sent `QuoteDueAt` as `2026-09-30T18:00:00+05:30`. Npgsql writes
`timestamptz` only at offset 0, and EF wrapped the refusal in `DbUpdateException`. The
disposal rollback that followed threw `ObjectDisposedException`, whose message is the bare
string `NpgsqlTransaction`, and before `7003c02` that replaced the real exception. Because it
derives from `InvalidOperationException`, `Run()` answered 400 with it as the `Detail`.
Sending `...T12:30:00Z` created RFQ-26-27-000031 at once. **The 113→130 migration history is
not the cause.** Both halves were true: a client-side trigger, hidden by the masking defect.

**The fix, proven with the real request.** The complete purchase flow now sends that exact
request on a disposable database, on the same route with the same handoff. With the two
source files put back to their state before `7003c02`, it reproduces the developer's response
exactly: `400 VALIDATION_FAILED, "Detail":"NpgsqlTransaction"`. With `7003c02` in place, no
RFQ is written and the real cause survives, reaching the server log as `DbUpdateException →
ArgumentException: … only offset 0 (UTC) is supported`. Tests committed `99e5e5d`, not yet pushed.

**Not what was hoped.** The caller does not get the offset message. It gets **500
INTERNAL_ERROR** with a TraceId, because `Run()` treats `DbUpdateException` as an
infrastructure failure, and the detail of a 5xx is suppressed by design. That is honest but
not actionable, and a client mistake is being reported as a server fault.

### #31 Local-offset timestamps: 13 fields, frontend obligation plus a backend safety net

> **The frontend obligation does not depend on the backend.** The frontend must send every
> one of the 13 fields below in UTC, ending in `Z`, **before 1 October, whatever the backend
> does.** The backend converter is a **safety net, not a substitute**. It is not built yet.
> It reaches the server only with a later commissioning package. And it protects only a
> field someone remembered to route through it. A reader who stops here has the whole
> instruction: **send UTC**. In JavaScript, `date.toISOString()` produces it.

**The 13 fields.** They are every request field of type `DateTimeOffset` that the API
accepts. This is the complete list, taken from the source on 24 September, and not a sample:
every request record with such a field was checked for the route that binds it, and none is
taken from the query string. **The earlier figure of 10 was wrong**: it missed the three
material issue and return fields, because their records are not named `...Request`.
**Request records in this codebase are not reliably named `...Request`**: `CreateMaterialIssue`,
`CreateMaterialReturn` and `AcceptMaterialReturn` are request bodies too. To find request
fields, start from the endpoints. Take every type an endpoint binds as its body, then
search those types for the field type. Never select types by name.

| # | Endpoint | Field | Screen |
|---:|---|---|---|
| 1 | `POST /api/v1/purchase/rfqs` | `QuoteDueAt` | Create RFQ |
| 2 | `POST /api/v1/purchase/rfq-invitations/{id}/quotations` | `ReceivedAt` | Enter vendor quotation |
| 3 | `POST /api/v1/stores/gate-entries` | `ArrivedAt` | Create gate entry |
| 4 | `PUT /api/v1/stores/gate-entries/{id}` | `ArrivedAt` | Edit gate entry |
| 5 | `POST /api/v1/stores/goods-receipts` | `ReceivedAt` | Create GRN |
| 6 | `PUT /api/v1/stores/goods-receipts/{id}` | `ReceivedAt` | Edit GRN |
| 7 | `POST /api/v1/qc/inspections` | `InspectionStartedAt` | Finalize QC inspection |
| 8 | `POST /api/v1/qc/inspections/{number}/corrections` | `InspectionStartedAt` | Correct QC inspection |
| 9 | `POST /api/v1/stores/material-issues/from-request/{requestId}` | `IssuedAt` | Issue material against an MIR |
| 10 | `POST /api/v1/stores/material-returns/from-issue/{materialIssueId}` | `DeclaredAt` | Declare a material return |
| 11 | `POST /api/v1/stores/material-returns/{id}/accept` | `AcceptedAt` | Accept a material return |
| 12 | `POST /api/v1/production/component-fitments` | `FittedAt` | Confirm component fitment |
| 13 | `POST /api/v1/stores/machine-deliveries/{id}/signature` | `DeliveredAt` | Sign machine delivery |

**What a local offset does today.** Row 1 is proven: a 500 INTERNAL_ERROR, no RFQ written,
and the real cause in the server log only. Rows 2-13 write through the same Npgsql path, and
the same failure is expected there but not yet witnessed. The converter's tests will send a
`+05:30` value to every row and pin the result.

**Backend safety net: approved, built after #30 lands.** Decided by the Technical Director on
24 September: **convert, don't reject.** +05:30 names one exact instant, and the database
stores the instant anyway. Refusing a valid timestamp because of its notation would be
pedantry the user pays for. One `DateTimeOffset` JSON converter in `ApiJsonContract` turns
every incoming value into UTC, which covers all 13 at once. It also keeps idempotency
fingerprints stable when the same instant is resent with a different offset. It is **about
half a day** with tests, plus its own full-suite and gate cycle, as a separate item after
the current suites pass and #30 is committed. **None of this changes the frontend
obligation above.**
