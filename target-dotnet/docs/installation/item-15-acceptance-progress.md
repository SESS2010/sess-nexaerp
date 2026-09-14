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

Remaining implementation/proof:
1. Finalize reports 1-9 against actual source movements and decisions, including the mechanical cost-restoration and ownership-pool correction.
2. Completed: governed invoice-before-receipt and report 5, targeted Release 3/3 and Debug 3/3 with actual receipt/reversal/permissions/Excel evidence. The existing one-effective-GRN-per-invoice limitation remains explicit. Full routine Release now passes 836/836, zero failed/skipped, 23m42s. See item-15-billed-not-received.md.
3. Build report 10's delivered-machine audit dossier last, including actual signed delivery evidence, components, GRNs, vendors, accepted bills/charges, QC and deviations.
4. Re-measure final heaviest report at 300,000 items/2,000,000 movements; explain materialized-view invalidation if one is introduced.
5. Re-authenticate the provider-configured code against the real Keycloak witness. No Cognito details are expected until Tuesday.

One unrelated business decision remains pending: provisional INR basis for foreign procurement. No currency conversion or fabricated INR cost is authorized.
