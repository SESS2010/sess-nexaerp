# Migration SQL line endings

Status: correction under verification. This is not clearance for the pending real pre-75 field upgrade.

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
