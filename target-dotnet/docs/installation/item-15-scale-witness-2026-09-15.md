# Item 15 scale witness - 15 September 2026

Measured code: 9d2c0d4. Release opt-in witness passed 1/1, zero failed/skipped, 25m18s process wall time; preceding build passed with zero warnings/errors. This deliberate volume run is separate from the routine Release run, which passed 836/836 in 23m42s. Debug builds and report business checks pass as recorded in item-15-billed-not-received.md; no Debug volume timing is claimed.

Exactly 300,000 items and 2,000,000 movements were verified and analyzed before timing. Report date: 2026-09-15. PostgreSQL temporary-file limit: 4 GB per backend.

| Measurement | Stock balance | Movement roll-forward |
|---|---:|---:|
| Summary query seconds |18.9361623|17.1636112|
| EXPLAIN execution seconds |17.521559|18.247755|
| Estimated root cost (planner units, not seconds) |1,588,717.92|1,590,706.17|
| Summary groups |300,007|300,012|
| Contributing source rows |1,999,989|2,000,000|

Complete movement roll-forward SQL plus Excel writing took 215.3819814 seconds. The header arrived after 139.52 seconds. The 479,191,872-byte XLSX contains 300,012 summary rows, 2,000,000 detail rows, 1,800,108 drill-through links and 35 sheets. Streaming XML verification checked sheet limits and link targets; desktop Excel opening is not claimed.

## Scope and limits

The witness first completes the governed three-band purchase and report assertions, then creates separate report_volume copies with production column/index layout but no posting-chain constraints, foreign keys or triggers. Production stock SQL changes only those two relation names. These are query timings, not two million governed transactions or HTTP end-to-end timings. The original governed tables retain 30 movements and are unchanged by synthetic loading. Setup took 926.441722 seconds and is excluded from query times. Local Keycloak restarted during setup and was ready before timed queries; no builds or authentication tests ran during query/export measurement. The disposable PostgreSQL uses fsync/synchronous_commit off, and this is not a guaranteed cold-cache benchmark.

Evidence: local-evidence/item15/scale-20260915/measurements.json, both EXPLAIN JSON files, export-estimated-plan.json, progress.txt and movement-roll-forward.xlsx. TRX: local-evidence/overnight-20260914/report-volume-release.trx.

## Aggregate decision and invalidation

No persisted aggregate or materialized view is installed. At this scale, allow roughly 19 seconds for an unfiltered stock summary and more than 3.5 minutes for full SQL/Excel generation. A faster interactive requirement would need a maintained daily stock aggregate; the current result should not be described as instantaneous.

An authoritative daily aggregate must update for EVERY appended stock movement in the same transaction, including opening balances, GRNs, QC transfers, issues, fitment consumption/reversal, physical returns, DC movements and adjustments. The affected key is company, recorded posting date, item, warehouse/rack, lot/serial, ownership, custody and condition. Backdated postings invalidate the affected date bucket and any later cached cumulative balances. Reversals are new movements and have the same obligation. A scheduled refresh alone cannot silently serve stale balances as current ledger truth.

Keep live permissions and master labels outside the cached quantities: permission revocation must apply at query time, and names should be joined by stable IDs. Underlying movements remain the drill-through source. Any future projection requires upgrade/reconciliation and transactional freshness proof; this commit introduces none and changes no business rows or frontend contract.

Report 10 remains outstanding; these timings certify the existing stock/roll-forward paths, not its unbuilt delivery dossier. Sources consulted: Pending_Work_Specification item15, Answers_To_Open_Questions, frozen Stores Full Schema Guideline, final overnight instruction and existing reporting implementation/volume witness.