# REV869B source correction checkpoint 9

Date: 2026-08-13
Scope: ninth controlled source-only REV869B correction
Entry HEAD: `a235792909f3f0f9c6ac097c6fba882ccde4742e`
Entry parent: `624ca346028589022654136b5e4861cf099fb419`

## Safety boundary

- The exact entry gate passed before edits: expected HEAD and parent, target-scoped clean state, and the entry commit contained only `outputs/rev869b_preapply_source_safety_rereview_after_correction_8.md`.
- Work remained inside `target-dotnet`. The sibling `../legacy-reference/` was not read, changed, staged, or removed.
- No PostgreSQL server was contacted by this correction. No PostgreSQL-backed test was run.
- No helper/apply tool was created or executed. No migration was created, applied, removed, or rolled back. No database, backup, restore, production, REV861, frontend, REV869C, or AWS operation occurred.
- EF migration listing used `--no-connect` and an intentionally non-routable design-time connection string. Migration SQL was generated to the PowerShell pipeline and discarded in memory.
- The repository contains no `scripts/validate-rev869b-source.ps1`; an attempted lookup therefore produced no validation result and made no change.

## Correction matrix

| # | Authoritative rereview blocker | Ninth correction |
|---|---|---|
| 1 | Qualification status vocabulary mismatch | Qualification guards and histories now use exact canonical `Pending Approval` values for both verification and approval fields; offline contracts reject the unspaced value on those columns. |
| 2 | Qualification endpoint scope, transaction, and audit gaps | Create/verify/approve now require authenticated employee and organization, exact organization masking, record-scope authorization, denial audit, required remarks, one transaction, protected command context, concurrency handling, business history, audit, and commit. |
| 3 | Forgeable GUC/correlation/history evidence | A retained-migration-owned, DB-private command-context table and SECURITY DEFINER functions bind a random token to backend PID, transaction ID, authenticated identity/role/organization, server timestamp, exact entity/action/version/status/correlation/remarks claims, and transaction-local selectors. Controlled guards validate and consume those claims. |
| 4 | Legacy qualification overlap ambiguity | Creation blocks every active overlapping effective range; older actorless rows remain readable but cannot be mutated through the controlled lifecycle. |
| 5 | Zero-row late-child tests | Every late-child attempt first proves exactly one owned source row, uses a deterministic target ID, asserts the database guard, and independently verifies the self-owned terminal graph remains committed. |
| 6 | Rejected-PO history/deferred evidence gaps | Both revision cycles now write specialized PO history and generic status history, force deferred constraints, commit, and use an independent connection to verify exact ancestry and history counts. |
| 7 | Concatenated exception-message evidence | Guard assertions use structured SQLSTATE plus schema and exact constraint/table/column or server routine evidence; message text is not used as substitute evidence. |
| 8 | Disposable database ownership and cleanup risk | Both PostgreSQL suites share a high-entropy per-run database lease. Source identity/migration are checked before CREATE; target identity/migration plus an unguessable durable marker are checked before every use and DROP; collisions and proof failures quarantine rather than delete; pooling is disabled and disposal is retryable/idempotent. |
| 9 | Scalar rollback and concurrency assertions | Application tests capture the complete owned relation vector including all line relations, compare exact before/after rollback state, and assert exact winner/loser deltas across two independent contexts/connections/services. |
| 10 | Mapped endpoint coverage gaps | The mapped ASP.NET path seeds its own complete graph and covers qualification create/verify/approve, stale version, separation of duties, record-scope denial, organization masking, commercial-value masking, attachment/export/audit permissions, denial audit, and audit-failure rollback. |
| 11 | PO issuer/approver separation | Service and database guard require one exact approval history and reject issue by the approving employee. |
| 12 | Runtime proof unavailable and legacy-style fixture literal | The old direct fixture marker was replaced by `REV869B-PG-SELF-OWNED-GRAPH`; direct tests seed their complete graph inside their owned clone. PostgreSQL execution remains explicitly NOT RUN and therefore unverified. |

## Principal implementation details

- Added `Rev869BCommandContextSql.cs` and wired its install/remove SQL into the retained REV869B migration; no new EF migration was created.
- RFQ creation serializes same-key contention with a transaction advisory lock before replay lookup.
- Qualification INSERT, verify, and approve require exact same-transaction controlled history; command claims are single-use per entity/correlation.
- Direct fixture seeding now includes terminal RFQ/invitation/follow-up rows needed to exercise every late-child guard.
- Rejected PO revision tests persist two revision cycles and verify specialized plus generic histories from a fresh connection.
- Error evidence assertions preserve structured PostgreSQL fields.
- The DB-private command-context relation is deliberately not EF-mapped, so the model snapshot/designer remain unchanged and model parity remains exact.

## Validation evidence

- `dotnet build SESS.NexaERP.slnx --no-restore --nologo`: PASS  0 warnings, 0 errors.
- Focused REV869B non-PostgreSQL tests: PASS  51 passed, 0 failed, 0 skipped.
- Complete non-PostgreSQL suite: PASS  425 passed, 0 failed, 0 skipped.
- Current design-time model versus snapshot parity: PASS as part of the focused/non-PostgreSQL suite.
- EF migrations list with `--no-connect`: PASS  13 migrations listed once; terminal migration is `20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation`.
- Retained REV869B migration SQL generation to memory: PASS.
- PostgreSQL test discovery only: 22 tests listed (7 application behavior, 15 direct behavior); **NOT RUN**.
- `git diff --check`: PASS. Git emitted only line-ending conversion notices.
- Target stale literal scan: old `REV869B-PG-DIRECT-TEST-OWNED` absent.
- No helper or database execution was used.

A requested PowerShell parser/source-validator step is not applicable because the repository has no REV869B validation script. This is recorded as missing tooling, not as a pass.

## Source hashes before commit

- `6CB16F18BD1A3D6ABEE02A413AC806FC6EAA7057FFBCEDC030AC597803623C81`  retained migration
- `D2FDF1EF94C447155D2B91DB0768CA0EA70F6D6D138746A5BE3635821547BD31`  controlled mutation SQL
- `E10050C97B52A39FDABF534C683A1F095130BCAF44E354AD61F9CAEC2F9942E7`  command-context SQL

## Residual pre-apply status

This checkpoint does not claim PostgreSQL behavior, migration application, runtime trigger/function inventory, disposable database cleanup, or production readiness passed. Those remain unverified until a separately authorized isolated PostgreSQL execution and independent source/safety rereview. The required action after this checkpoint is the single controlled source commit, then stop.
