# Finding and open-item status — 24 September 2026

One place for the list to live. Written after finding #12 was discovered already shipped
on 20 September while still sitting open on the Technical Director's list, which would
have cost a full suite-and-gate cycle to re-confirm.

**How each line was decided.** Shipped means the change is in `src` or `database` on `main`,
with the commit or migration named below, and I checked it is an ancestor of `origin/main`
rather than sitting in a local tree. Superseded means a later decision removed the work.
Open means no code exists. Nothing here is a field witness: a shipped backend change is not
a claim that it has been applied to any server database.

## Findings

| # | Status | Evidence | Note |
|---|---|---|---|
| 3 | **SHIPPED** | `f7f0c24` — *Scope technical verification to the verifier department*, 19 Sep | Also resolves #17. |
| 4 | **NOT FOUND** | — | **No finding #4 exists anywhere in this repository.** I searched every document. It may be numbered only on your own list, or the number may never have been used. I need your entry before I can say anything about it. |
| 11 | **SHIPPED** | `5b903eb` — *Reconcile imported items to canonical Stores categories*, plus `database/postgresql/reconcile-legacy-item-categories.sql` | Runbook step 7: the reconcile script is **not needed** on a database the corrected import created. For a fresh go-live database the preflight count may legitimately be zero. |
| 12 | **SHIPPED** | `d36b001`, migration `20260920120000_QcGoodsReceiptRead` | **This is the one that stayed on the list.** `QC_MANAGER` *and* `STORES_MANAGER` get `inventory.grn` view only — no create, finalize, reverse or download. One operational action remains: remove the `STORES_ASSISTANT` support cover from whichever database carries it, and record it. |
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

Nothing in the findings list above blocks 8 October. What blocks it is field work, not code:
production login, DEMO acceptance and drop, the fresh Option C build on the server,
certificate trust on eleven PCs, setup on 1-3 October, both ceremonies on 5-6 October.

**One open investigation:** RFQ creation returns 400 with a detail naming `NpgsqlTransaction`
on the frontend developer's database, which was migrated 113→130 rather than built fresh. The
same path passes end to end on a fresh database. The evidence-destroying mechanism that hid
the cause is fixed at `7003c02`; **the trigger is still unfound** and this stays open pending
the exact `Detail` text and that database's history. Do not read the fix as closing it.
