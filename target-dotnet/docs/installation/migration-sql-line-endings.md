# Migration SQL line endings

Status: correction under verification. This is not clearance for the pending real pre-75 field upgrade.

## 20 September 2026 — finding #19: a pulled worktree keeps CRLF

`.gitattributes` governs only what Git writes on checkout. A worktree that already
held CRLF migration sources keeps them after a pull, and its binary compiles CRLF
into every raw string literal. The migrations themselves must therefore tolerate
CRLF sources.

**Reproduction.** The whole `src` tree was copied, every `.cs`/`.sql`/`.csproj`
converted to CRLF, the Infrastructure assembly compiled from it, and every
migration's Up and Down operations materialized without a database. Exactly one
migration failed: `20260918090000_ActualBomLandedRateValuation` Up threw
"Actual BOM fitment valuation baseline changed" — `BillJson` normalized the
fragment it searched for but not the baseline it searched in. After CRLF→LF
normalization the SQL of every other migration was identical to the LF build.

**Correction.**

- `MigrationText.Lf` is the single normalizer. Migration 108 normalizes both sides
  in `BillJson`, `ReplaceOnce` and the Down extraction.
- `LineEndingNormalizingMigrationsSqlGenerator` (registered in
  `NexaErpDbContext.OnConfiguring`) rewrites every raw `SqlOperation` to LF at
  generation time. A CRLF worktree now installs byte-identical function bodies to
  an LF checkout, and every stored-body guard, which already normalizes `prosrc`,
  compares like with like. `E'\r\n'` escape sequences are backslash text and are
  untouched. EF's own DDL formatting (CreateTable and friends, joined with
  `Environment.NewLine`) is outside the correction.

**Tests.** `MigrationLineEndingToleranceTests` proves the generator normalizes a
CRLF operation and preserves escapes, that every migration's raw SQL operations
generate LF in both directions, and that migration 108 derives identical SQL from
CRLF and LF baselines.

**Witness.** Disposable PostgreSQL 17.10 on Windows: 0→113 installed from the LF
script; all 206 `advance` functions re-created with CRLF text (205 bodies stored
with CRLF, the field state a CRLF binary leaves behind); 114→119 applied from the
corrected build with exit 0, the Stores route page, nine mirrored grants and three
audited Stores grant receipts present. A harsher emulation that converts the guard
expected-body literals themselves to CRLF fails at
`20260913020000_FitmentIssueHeaderLockOrder`; the generator makes that state
unreachable from any worktree. Evidence is under `local-evidence/finding-19`.

Not reproduced: the field database name, starting state, role history, original
dump line endings and operating system; the witness scripts ran as the cluster
superuser, not `nexa_erp_migration`.

## Defect and sweep

C# raw multiline strings preserve their source file's line endings. Under the previous `* text=auto` checkout rule, Windows could compile CRLF into a patch while its PostgreSQL guard normalized the stored function body to LF. Exact comparison then rejected its own installed patch.

The full migration-source sweep found four affected generators:

| Migration | Affected fragment | Correction |
|---|---|---|
| `20260913095000_PurchaseOrderSupersedeHistory` | Supersede approval patch | Normalize Patch to LF before insertion and guard comparison |
| `20260914045000_PurchaseOrderCancellationHistory` | Director cancellation patch | Normalize Patch to LF before insertion and guard comparison |
| `20260914030000_ReceiptQuantityAcrossPoRevisions` | Receipt header/line raw insertions | Normalize the composed replacement result |
| `20260914065000_QcDiscrepancyPostingGuard` | Disposition-count replacement | Normalize the replacement fragment |

The supersede and cancellation change operations also normalize `pg_get_functiondef` before modifying it. This makes Down work when a previously stored function uses CRLF.

The other normalized `prosrc` comparisons obtain normalized expected bodies, normalized resource readers, or replacements that normalize their inputs. FIFO restoration's replacement helper normalizes the entire composed definition; the later reversed-receipt guard uses that normalized definition. The legacy command-context `prosrc` hash is an integrity fingerprint, not a patch comparison, and is unchanged.

The whole-assembly regression additionally exposed platform newline separators in four SQL composition helpers (`AdvanceDatabaseContractSql`, `Rev869BControlledMutationSql`, `OrdinaryCommandLedgerSql`, `RetireRev869BOrdinaryDeploymentSql`) and a CRLF machine-delivery SQL resource. Composition separators now use literal LF. Git enforces LF throughout the migration directory, including SQL resources. No SQL authorization, ownership, signature, body-content, trigger or version predicate is removed.

## Checkout and build

`.gitattributes` now declares `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/** text eol=lf`. A fresh Git checkout therefore uses LF even on Windows. Existing checkouts must refresh their migration files after saving any local edits, then rebuild; an old binary retains its embedded strings. Do not deploy with `--no-build` against a binary built before this correction.

After pulling the corrected commit, from `target-dotnet`, the following refuses local migration edits before refreshing tracked migration files from the index using the new attributes:

```powershell
$migrationDirectory = 'src/SESS.NexaERP.Infrastructure/Persistence/Migrations'
$localMigrationChanges = git status --porcelain -- $migrationDirectory
if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect migration worktree.' }
if ($localMigrationChanges) { throw 'Save and resolve local migration edits before refreshing line endings.' }
$trackedMigrationPaths = git ls-files -- $migrationDirectory
if ($LASTEXITCODE -ne 0) { throw 'Cannot enumerate tracked migration files.' }
$trackedMigrationPaths | git checkout-index --force --stdin
if ($LASTEXITCODE -ne 0) { throw 'Migration checkout refresh failed.' }
dotnet build SESS.NexaERP.slnx -c Release --nologo
```

This refresh is not a database operation. Do not use it over unsaved local changes. Run the full routine suite with a TRX logger against the rebuilt binaries before declaring the installation candidate verified.

`MigrationSqlLineEndingTests.EveryMigrationEmbeddedSqlUsesLfInBothDirections` enumerates every registered migration and checks every embedded `SqlOperation` from Up and Down. It reports each offending migration, direction and operation. It does not normalize the inspected SQL, so the test cannot hide a CRLF producer.

## Evidence

The regression failed against the original mixed-ending checkout. Initial LF verification also failed and exposed the platform separators/resource described above. The corrected LF whole-assembly test passed. A deliberate Release rebuild with all 237 migration C# files in CRLF then passed the supersede/cancellation/receipt lifecycle tests (3/3, 1m55s), the routine QC migration test (1/1, 1m06s), and a fresh current-chain install/trial-data round trip (1/1, 1m04s). The supersede and cancellation tests also replace the stored function with CRLF before rollback. The fresh-chain test used advance_parser and synthetic/fresh role history; it is not the field upgrade. The full Release suite on restored LF sources passed **846/846, zero failed, 25.29 minutes**, with TRX. That historical run does not certify the current candidate; current full Debug and Release evidence is recorded separately. Evidence is retained privately under `local-evidence/migration-89`.

Before each chain witness, record the matches and gaps in [migration witness environment](migration-witness-environment.md). A CRLF fixture run is separate from the required restored customer-upgrade proof.

## Current shared-candidate regression evidence

The frozen remaining-findings working candidate passed full Debug 878/878 and Release 875/875, with zero failures/skips, after both solution builds succeeded with zero warnings/errors. All 1,013 recorded source hashes matched after testing. TRX evidence is under local-evidence/finding3. These are shared working-candidate results, not clean per-commit checkout runs; ordered commits and their exact staged builds remain pending. No owner database was changed. The exact pre-75 field-backup witness remains unperformed.
