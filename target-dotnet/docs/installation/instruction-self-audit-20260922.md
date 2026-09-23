# Instruction self-audit — 20–22 September 2026

Reviewed the retained user messages dated 20–22 September in the local session
history, the three pasted instruction files, Git history and local evidence.
The 22 September evening consolidated instruction is controlling. Superseded
requests below are not silently treated as unfinished work to execute later.
This report distinguishes code/doc delivery from server and human witnessing.

## Delivered earlier and reported

| Instruction | Delivery / evidence | Remaining limit |
|---|---|---|
| ActualBomEntryView provenance without payment status; category command | 812db31; full acceptance report f5356ec. ActualBomEntryView gained a field; commercial dossier retains its own full sentence. | Field frontend/server witness separate. Latest canonical-category prerequisite is added in this document batch. |
| Server facts, Windows 10 decision, service hosting on 8443 | dfd7d38, ed04b8a, 4b3177f, 401c6ea and deployment runbook. | Win11/ESU remains a planned TD decision; no OS change performed. Current cleanup status supersedes old NI/Siemens retention. |
| SDK-free migration mechanism and packaged publish | d2d9a1c, 25ed6f0 and successive package reports. Final current commissioning package is 662e9a3; receipt 56a79f9. | This laptop has SDK installed; the proof excludes SDK from PATH and runs the bundle on owned empty disposable databases. It does not reproduce a physically SDK-uninstalled field machine, server ACLs, engineering workload or real network/certificates. |
| Production login contract, Keycloak install/realms, HTTPS and identity ceremony | c9ac742, ec85e45, f910f2f, 3a0a4ad, c0cf852; D1–D5 subsequently updated. | Production frontend login is the frontend developer's work. Field Keycloak/realms/MFA/mapping not witnessed here. |
| Daily off-machine backup, C: staging and 25 GB headroom | d2cf5bf; receiver correction/configuration 828816c. | Ilamparuthi PC exact hostname still missing: RECEIVER_NOT_SET is intentional and visible. No claim that daily copies or receiver-owned archive are commissioned. |
| Configuration inventory/proposal only | 8a5f773, docs/configuration-inventory-and-proposal.md. | No Configuration feature before 1 October. Full governed implementation is backlog, not a launch deliverable. |
| No developer dump; clean Option C and setup dependencies | ac45aa8; corrected package docs bcfee49 and report 2bf2e3f. | Team entry, both opening ceremonies and 1 October transactions remain field operations. No owner database touched. |
| Every setup-screen gap and safe fallback | 50ced03, setup-screen-gaps-20260921.md. | Only the commercial-verification screen is authorized for launch after login; other screens deferred. |
| Wrappers, data pack, review register and employee authentication | 15f27ff, 243ae99; D5/setup completion 8a9e4c8. | Real-employee wrapper rehearsal requires deployed Keycloak, mapped named employees and separate decision sessions. Not rehearsed on field DEMO yet. |
| Vendor commercial-verification frontend contract | ff99772. | Frontend developer implements after login; field verification pending. |
| One server instruction, step 0 permissions, STOP after SESS-12 | 2ed84e4, 2561d59 / merge 662e9a3. | Server agent applies locally and reports. Laptop does not apply server changes. Old package instructions need this separately supplied documentation update. |
| One-page TD scope roster | 1c98287. | TD must fill/sign full roster before any scope is entered. Scope creation once from signed form; no revoke endpoint exists. |
| Four real Stores XLSX workbooks, dropdowns and Bin Code note | 017a56e, tracked in 8c13f52 and packaged. | Team fills one warehouse and one rack workbook per company; separate submission/TD approval still required. Opening-stock Bin Code must match approved bins. |
| Four day-one flows ungated in fresh-company rehearsal | 8f86685. | Included in current routine acceptance, not dependent on the nightly compile gate. |

Current commissioning package:
`C:\SESS-Deploy\662e9a311ec6f8078d0333753654cf77f62882d6`.
MANIFEST SHA-256:
`bb9581dd11be54b173afdfe308c45447a23a5bf1c8dba7897404685bd625cfd3`.
213 files were verified; fresh bundle proof reached 130 migrations, RECONCILED and
VERIFIED, replay unchanged, zero stock movements, owned cluster stopped. This
package predates the current concurrency fixes and deliberately remains unchanged.

## This batch: delivered handoffs and acceptance-dependent work

Documentation-only handoffs are separately committed without changing the 884-file
acceptance set: D4 **0f5eab7**, dashboards **1f058cc**, category/Keycloak runbook
additions **056d567**, readiness follow-up **a94ee75**, dashboard type-rendering
follow-up **deb96c6**. Fix/evidence and E/F/G/H commits remain gated on acceptance.

| Instruction | Work now present | Still unfinished / unreported |
|---|---|---|
| D4 certificates first | Four files issued in C:\SESS-CA\server-output; hashes and issuance checks passed. Fingerprint reported immediately; validity and renew-by date in server-https.md. Eleven-PC one-page trust sheet written. | Encrypted root PFX external-disk copy/hash check/local removal is still a USER custody action. Server import/private-key ACLs and eleven client HTTPS checks are not witnessed. |
| Dashboard developer handoff | dashboard-frontend-contract.md and six complete synthetic JSON mocks, validated against 32 DTO records / 294 fields. Shared-client requirement, company header, denied bodies, filters, money and drill-downs documented. | Frontend implementation and live login integration are not this report's work. Current spending includes embedded GST; an ex-recoverable-GST spending change is NOT implemented. |
| Canonical categories and Keycloak availability | Runbook prerequisite/read-back; variable alias count; 8444 discovery checks in monitor profile; service recovery and outage procedure. | Server agent must receive/configure the updated profile and witness restart/reload. No remote service/config change done here. |
| E cleanup revision | pre-go-live-server-cleanup.md replaces removal procedures; current never-touch lists updated. | NI/Siemens completion is reported by the server agent, not independently witnessed. Suspicious service removal/Defender full scan, unused-software inventory/TD decisions and daily disk-watch receipts remain field items. |
| F DC wrapper proposal | machine-delivery-wrapper.md, 2–3 engineering-day proposal. | **DO NOT BUILD**. TD decision depends on delivery challan screen timing. |
| G laptop investigation | td-laptop-backup-task-check-20260922.md: task descriptor/principal/token, future deletion command, watcher coverage, OPC installer package, Everyone ACLs. | No deletion/repair done. True elevated-task denial cause remains unproved; continuous Start monitoring stops early and later process absence is not a recorded click test. OPC Bonjour parent is now proved by the Siemens TIA Portal V20 setup log and installed-component path. |
| H one backlog document | post-october-backlog.md covers all requested areas/estimates, including measured suite timing. | Proposal only; no features, scope revoke, OS upgrade, tool/legacy-bill import or DC dispatch built. |
| B six gated failures and new concurrency defects | Frozen Round 4 candidate; focused 19, Debug 1,029 and Release 1,026 passed. Arithmetic reported before expectation change; typed 40001 response/rollback/replay coverage added. | The first gate run was interrupted by Windows power-off at 21:14 (boot 21:15); no gated TRX resulted. All three unchanged gates restarted at 21:59 after preserving evidence and confirming all 884 hashes. Still running/pending at drafting. Final evidence and commits must wait for all gates, unchanged-source check and nightly-task restoration. No premature closing SHA. |

## Decisions superseded or deliberately deferred

- The original laptop-as-production-server plan, production-hours restriction and
  October laptop nightly-disable request were superseded by the dedicated office
  server. Development nightly may remain on DESKTOP-AP after cutover; NEVER install
  that witness schedule on the ERP server. Laptop NI/LabVIEW restoration is a later
  cutover action, not authorized by this report-only laptop inspection.
- The nightly harness runs gates sequentially, but that is **not a complete memory
  guard**: no machine-wide memory threshold/mutex is in the harness; ordinary test
  disposal exists, crash/startup cleanup is incomplete. Current acceptance uses
  one owned disposable cluster at a time. Do not claim the original memory-guard
  question was fully satisfied by adding a scheduler command.
- Old dump questions, vendor_attachment restoration and finding #20 dump dependency
  were removed by the no-dump decision. No dump is awaited or to be imported.
- D:/E: backup plans and weekly copying were superseded by C: verified staging plus
  DAILY verified off-machine copying. Latest decision says no ERP use of D:/E: ever,
  even while the HDD remains connected under its trial exception.
- Windows 11 before deployment was superseded: Windows 10 stays now; later upgrade
  or commercial ESU remains planned, with pricing recorded in the backlog.
- Old E removal commands are withdrawn. No further product/account/credential/ACL
  removal procedure until server inventory and TD decisions.
- The earlier instruction to build F after A–C is cancelled. F remains proposal only.
- **No server-package rebuild now.** Commission DEMO from 662e9a3. The next package
  accompanies production login and must include accepted payment/QC/GRN/opening-stock
  concurrency fixes, updated documentation and a fresh manifest/verification proof.
- UOM conversions and party/company relationships remain post-launch unless a hard
  dependency is established; current setup uses agreed same-UOM paths. No new hard
  dependency was introduced by this documentation/certificate batch.

## Pre-existing working-tree items not absorbed into this batch

`stock-adjustment-posting-plan.md` already contains uncommitted live removal-value/
reapproval and post-launch serialized-identity decisions. `fresh-company-rehearsal-findings.md`
also has pre-existing edits, including the old laptop nightly-cutoff wording that
is superseded by the dedicated-server decision. These edits were preserved, not
silently included in another commit. Their author should reconcile/commit them;
the current deployment runbook and this audit state the controlling server/laptop rule.
The memory-guard hardening is still unfinished, as described above.

## External inputs and witnesses still owed

TD/router reservation of 192.168.68.130; exact backup receiver hostname/account/share
and a witnessed missed-day failure; root-key offline custody; server permission
report/approved remediation if required; D4→D2→D3→DEMO provision/migrate→API readiness
→D5 SESS-12 STOP receipt; signed full scope roster; named-employee wrapper rehearsal;
production login and vendor commercial screen; other-office-PC/reboot/MFA witnesses;
DEMO acceptance then governed drop; fresh go-live Option C and team setup/training
28–30 September; both opening stock ceremonies before any GRN/issue/adjustment;
verified restore/off-machine backup receipts and C: headroom checks. These remain
visible gates, not claims of completed field deployment.

Final acceptance and separate commit SHAs will be recorded after Round 4 completes.
The user pushes. Until field witnesses are received: RESULT_REPORTED_PENDING_WITNESS.

## Addendum, 23 September 2026

This audit reports the position as at 22 September and its body is left unchanged. Three
things have since moved, and where this document says "1 October transactions" it should
now be read as **8 October**:

1. **Go-live moved from 1 to 8 October** by governed decision, because production login
   is the critical path and lands about 28 September; setup is now 1-3 October, both
   opening-stock ceremonies 5-6 October, BroPOS frozen 7 October. Reason and schedule:
   [the runbook](go-live-fresh-database-runbook.md#governed-schedule-change-23-september-2026-go-live-moves-to-8-october).
2. **Round 4 did not complete on 22 September.** The 21:59 restart recorded in the table
   above was itself cut short with the machine; the Workflow gate was still running and no
   gate TRX resulted. Its `finally` block never ran, so `NexaERP nightly witnesses` was
   left Disabled until 23 September, when it was verified against its saved definition and
   re-enabled. All three gates were restarted on the unchanged candidate on 23 September
   after rechecking all 884 hashes.
3. **The memory-guard limitation produced real orphans.** Four disposable clusters were
   left in TEMP by the interrupted runs and were removed on 23 September. The fix is
   approved and scheduled after go-live:
   [harness resilience proposal](harness-resilience-proposal.md).
