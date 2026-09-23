# Frozen historical FIFO fixture - provenance and reproduction

TEST DATA ONLY. Never include this file in a deployment package, load it into an
owner database, or use it for SESS opening stock. The no-dump go-live decision is
unchanged. Only the disposable test helper may load this fixture.

Origin: synthetic CompletePurchaseFlow HTTP fixture at source
433f96840b79737b24e5288c200a1a2d42e738a3, captured by
HistoricalAcceptedReturnsGainRestorationsWithoutRewritingHistory immediately
before applying 20260914080000_FifoReturnRestorations. It contains three accepted
returns totalling 1.63, original FIFO layers/consumptions, return and stock history,
and their synthetic master/document dependencies. It contains no field database.

Reproduction: archive that source tree into an isolated ignored working directory.
Normalize migration and shared SQL/C# source CRLF to LF, as the later repository
line-ending correction does (159822d). Do not change any business assertion or
function guard. Add a capture-only method to that copy's DisposablePostgreSql:
run its local pg_dump on its own loopback port/advance_parser database with
--data-only --column-inserts --disable-triggers --no-owner --no-privileges,
exclude *."__EFMigrationsHistory", write to an isolated output file. Invoke it
immediately before `var originalHistory=await ReadOriginalFifoHistory(options);`.
Build Release and run only HistoricalAcceptedReturnsGainRestorationsWithoutRewritingHistory.
Retain its passing TRX, generator source patch and output SHA-256. Review the output
before replacing the fixture; timestamps/IDs can differ on regeneration.

The new test creates the actual predecessor schema from checked-in migrations,
loads this captured state into its own guarded disposable database, verifies all
triggers are enabled and restores the original fixture rows. Disable-trigger
commands are restricted to loading this synthetic test image; they are not an
Installer feature or an alternative business entry path.

It applies the exact FIFO migration and compares full original JSON rows, checks
1.63 historical restoration quantity/effective dates, refuses populated rollback,
then reaches current head. Later additive schema columns are allowed while every
original row/column must remain identical. Replaying to head must change neither
history nor restorations. Current API correctness remains in the complete purchase
flow and dedicated return witnesses; this test does not pretend the current API
can run against an old schema.

Capture runtime: Windows, PostgreSQL 17.10, .NET 10.0.11. Original-source capture witness: 1 passed, 0 failed/skipped, 4m05s.
Current migration acceptance is recorded in the consolidated B closure report.

Uncompressed SQL SHA-256: `d2318bba9d54c8feb2ca191a193236a9767c7f0e5349922ca8ce04686b5015f0` (3434782 bytes).
Compressed file SHA-256: `f3ea02b440df254782659d3156cfc4aea2b43b52dfc673177893a11d58d0c41d` (281318 bytes).
The test checks the uncompressed hash before loading. Gzip is deterministic (mtime=0).

Capture TRX: `local-evidence/consolidated-B/historical-source-results/original-433f968-normalized.trx`; SHA-256 `467ac909c295511738b313396978fe01ac741b40582030be9aa708a5abda80ee`.
