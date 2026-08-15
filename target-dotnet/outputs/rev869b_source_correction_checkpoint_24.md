# REV869B Correction 24 source-only blocker checkpoint

Date: 2026-08-15 (Asia/Calcutta)

## 1. Disposition

**BLOCKED — no Correction 24 source or test correction was committed.**

The authorized entry gate passed, but the exhaustive eight-file allowlist cannot implement F23-02's mandatory independent evidence model without violating its own restrictions and the retained ACL architecture. Partial exploratory edits were completely backed out before this report was created. This report is the only change.

## 2. Entry-gate evidence

| Check | Result |
|---|---|
| Authorized starting HEAD | PASS — `d36fb4d9e210895359979048ed1ed0f84229debd` |
| Branch | PASS — `master` |
| Reconciliation path | PASS — `outputs/rev869b_correction23_failure_reconciliation.md` |
| Reconciliation SHA-256 | PASS — `64406CC3C34951B940A136D5B3EFB54EE1E01DCCB859798CB7FCACF42E9DA791` |
| Reconciliation read | PASS — 205/205 lines read |
| Target-scoped status on entry | PASS — clean |
| Exact allowlist extracted | PASS — eight files |
| Frozen architecture | PASS — `RETAIN` |
| PostgreSQL/operational execution | `0` |
| `../legacy-reference/` | Not read, listed, searched, modified, staged or committed |

## 3. Exact allowlist contradiction

The reconciliation requires the adjudicator to open only verifier/audit-scoped connections, execute independent before/after/durable/cleanup reads, canonicalize rows and independently prove every one of the 34 formulas. It also prohibits any ninth file and excludes `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs`.

The committed ACL and function surface does not expose the required facts:

- Control-plane `nexa_rev869b_lifecycle_audit` has EXECUTE only on quarantine/failure/finalization and `rev869b_read_lease`/`rev869b_read_nonterminal_leases`. It cannot independently read immutable lease events, lifecycle attempts/outcomes, recovery decisions or quarantine rows.
- Target `nexa_rev869b_target_verifier` has EXECUTE only on `rev869b_reconcile_command_attempt`, `rev869b_reconcile_purge`, `rev869b_read_target_security_state`, `rev869b_target_catalogue_fingerprint` and `rev869b_verify_target_catalogue_acl`.
- The verifier cannot read command business/history/receipt rows needed by C01-C08 beyond the minimized reconcile projection.
- The verifier cannot read purge authorization/root/child/event/candidate evidence needed by G01-G06 beyond the minimized attempt projection.
- `nexa.rev869b_read_prepared_export_batch(uuid,uuid)` is executable only by `nexa_rev869b_export_service`; it explicitly checks `session_user='nexa_rev869b_export_service'`. The verifier/audit roles cannot independently prove E01-E04.
- No retained read function exposes controller physical create/drop counts, target/role absence across every lifecycle boundary, per-subcase denial records, or the full A02 principal/object Cartesian results.
- Direct table SELECT is intentionally revoked from verifier/audit roles. Granting it would violate the retained ACL closure and the reconciliation's prohibition on ACL redesign.

Therefore an allowlisted test client can either fail at runtime for lack of privilege or accept controller-supplied/signed echoes. The first is non-executable; the second recreates F23-02. Neither satisfies the authoritative formulas.

## 4. Why the allowed SQL files are insufficient

`tools/rev869b-control-plane-install.sql` is allowlisted only for the F23-01 normal-drop predicate and stable rejection identity. Using it to add a broad evidence API or new grants would exceed the permitted purpose.

`tools/rev869b-control-plane-verify.sql` is allowlisted only to pin the affected function definition and any stable P03 verifier denial identity. It is a control-plane verifier script and cannot add the target-local command, purge and export read contracts defined in the excluded migration SQL.

The necessary target SECURITY DEFINER evidence functions and exact EXECUTE grants would have to be added in `Rev869BCommandContextSql.cs` or another newly authorized production SQL location. That file is expressly excluded. Exposing export-service credentials to the test process would also violate the reconciliation's verifier/audit-only boundary.

## 5. F23 status

| Finding | State | Reason |
|---|---|---|
| F23-01 | UNCHANGED / FAIL | It is independently source-correctable in the current allowlist, but committing it alone would be a partial Correction 24 after the combined gate became blocked. |
| F23-02 | BLOCKED / FAIL | Exact independent evidence for all 34 scenarios cannot be implemented through the authorized read surface. |

No source/test/helper file remains modified. No ninth file was touched. No claim of source safety, helper readiness, database acceptance or production readiness is made.

## 6. Validation disposition

The full Correction 24 build/test/SQL validation suite was not run because no Correction 24 implementation exists and Stage 0 requires stopping when the exhaustive allowlist condition fails. The safe validations completed were entry lineage, report hashing/full read, allowlist extraction, read-only ACL/function contract inspection, rollback of all partial edits, `git diff --check`, and final target-scoped status verification.

PostgreSQL tests executed: `0`.

## 7. Smallest required management action

The next reconciliation must decide one of these architecture-preserving routes before implementation:

1. Add `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs` to the Correction 24 allowlist solely for narrowly scoped SECURITY DEFINER evidence-read functions and exact verifier EXECUTE grants covering command, purge, export and ACL formulas; and broaden the permitted purpose of `tools/rev869b-control-plane-install.sql` solely for equivalent control-plane evidence reads.
2. Supply a separately source-reviewed independent evidence service whose implementation, credentials, query contracts and trust separation are authoritative inputs and whose claims are not produced by the lifecycle action controller.

The first route is smaller and keeps ledgers target-local, provisioning external, the lifecycle controller dedicated, and the control-plane database surviving. It must be reconciled against ACL least privilege before authorization.

**Single next gate:** management authorization for a corrected source-only failure reconciliation that resolves the allowlist/evidence-interface contradiction. No Correction 24 implementation, Correction 25, PostgreSQL or operational execution is authorized by this checkpoint.

correction_24_source_implementation_state=BLOCKED
correction_24_source_only_gate=NO_GO
frozen_architecture_state=RETAIN
external_prerequisite_blocking_state=YES
rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN
