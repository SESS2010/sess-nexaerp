# Item 15 acceptance work — 14 September

Priority: all ten reports before further dashboards; delivered-machine dossier last; then Keycloak authentication. The user has brought the report source prerequisites into this work. Previous partial report code is reused, not treated as completion.

Verified checkpoint:
- Actual opening ceremony: OPENING_BALANCE movement and OPENING_LANDED layer, quantity 10 and value INR 250 at unit cost 25; preceding date excluded; stored fiscal posting date 2027-03-31 in this disposable witness.
- Stock balance and movement roll-forward one-command checks, total-to-detail selection and Excel totals/hyperlinks.
- FIFO opening quantity/value/age boundaries and Excel proof from the existing governed witness.
- No business rows added by report reads/exports; export audit remains expected.
- Isolated Actual BOM check after fitment reversal and before re-fit: zero material value, allocated charges, total and net fitted quantity; engineer custody restored to 0.35; original issue consumption rows unchanged. Full three-band flow then continues.

Verified: Release and Debug builds succeeded with zero warnings/errors. The final targeted runs each passed two tests, zero failures/skips: item15-opening-reversal-verified-release.trx (5m01s) and item15-opening-reversal-verified-debug.trx (5m28s). Reversal full-flow durations were 4m26.424s / 4m50.310s; opening-stock durations 35.519s / 38.415s. These are two distinct tests in two configurations, not a full-suite run. Reversal values and custody agree; opening quantities, values and business-row counts agree. Generated fixture ownership UUIDs differ between the disposable databases.

FIFO restoration instruction is settled: mechanically unwind original consumption rows in reverse creation order, append restoration entries referencing them, rewrite neither layers nor consumptions. No policy switch. Refuse excess returns and fully restored consumptions. Prove 1@100 + 1@200 issued, restore 0.5 against 200, leaving net issued cost 200.

Fitment reversal is a distinct deliberate operation: it negates machine cost and restores engineer custody while the issue remains outstanding. Do not restore issue FIFO merely because fitment was reversed. Actual return to Stores is the restoration boundary. The isolated zero-cost proof now passes before re-fit, including both operational and commercial comparisons; original baseline identities and values stay unchanged.

FIFO implementation checkpoint: migration 20260914080000 and targeted Release witnesses now cover immutable reverse-order restoration, actual partial-fitment return, historical accepted-return reconciliation without rewrites, and ownership/concurrency refusal. Debug verification passed: item15-fifo-complete-debug.trx, six tests, zero failed/skipped, 11m52s. See item-15-fifo-restoration.md; this is not a new full-suite pass.

Current acceptance checkpoint - 15 September:

- Reports 1-9 have passing backend business checks: stock balance, movement roll-forward, FIFO valuation/ageing, GRNI, billed not received, purchase register, vendor summary, pending approvals and custody by engineer. Company scope, runtime permissions, one-command reads, drill-through and Excel checks are recorded in item-15-reporting-progress.md; opening/FIFO corrections above and report 5's own witness supersede its historical gaps. This is backend verification, not a claim that the production frontend or customer acceptance ceremony is complete.
- Report 5 is committed at 9d2c0d4. Targeted Release 3/3 and Debug 3/3 cover actual receipt/reversal, permissions and Excel. Its one-effective-GRN-per-invoice limitation remains explicit in item-15-billed-not-received.md.
- The final routine Release suite passed 836/836, zero failed/skipped, in 1422.3317698 seconds (23m42s; 23.71 minutes), excluding the separate build. Evidence: local-evidence/overnight-20260914/report5-routine-release.trx. The bootstrap setup and stale Serializable assertion are fixed at d32dc9b; production guards were preserved.
- The earlier gated baseline named three failed tests: AuthenticationBootstrapCeremonyCompletesExactlyOnceForSess12InBothCompanies, GeneratedBusinessBaselineScriptsAreAcceptedByDisposablePostgreSql and ServiceReusesPendingRfqAndPreventsDuplicateAndOverOrder. All are fixed. The historical 905-test console run's six names cannot be reconstructed; no claim that all six have been identified is made.
- Report 5's two migrations separately passed from 100 to 102 on the restored advance_parser copy; principal provision/status returned RECONCILED/VERIFIED. This does not clear the production database name: the isolated sess_nexa_erp copy stopped at 84 on 20260913060000_CommandReceiptReplay. There are now 24 pending migrations from 78, and no verified live upgrade command. See tuesday-migration-chain-witness-2026-09-14.md.
- Scale measurement is complete: stock summary 18.936 seconds, roll-forward 17.164 seconds, full SQL/Excel export 215.382 seconds at 300,000 items and 2,000,000 movements. See item-15-scale-witness-2026-09-15.md for limits and aggregate invalidation requirements.
- Real Keycloak reruns passed Release 1/1 and Debug 1/1. See item-16-verification.md. Cognito deployment and production frontend integration are not claimed.
- Report 10 remains unimplemented. The retained machine witness reaches FAT readiness, not signed delivery. Resume at the governed delivery prerequisite and then the ancestry dossier; see item-15-delivered-machine-source-findings.md. The delivery accounting decision remains pending. Do not start Block A.
- The separate explicit-target migration policy remains a proposal awaiting authorization after automatic approval review rejected changes to the 13 guards. No guard bypass or live migration has been performed.

One unrelated business decision remains pending: provisional INR basis for foreign procurement. No currency conversion or fabricated INR cost is authorized.
