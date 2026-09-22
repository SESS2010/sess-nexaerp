# Inventory-period foundation for stock adjustments

This candidate adds CFO-governed inventory-period open/close commands using the existing financial-period calendar. It does not implement stock-adjustment posting. No adjustment can be declared operational from this period workflow alone.

The documented CFO holder is SESS-02. The new role is distinct from MD authority. Its seed records identify migration baseline evidence, not a human clicking approval. Existing CFO configuration is refused for explicit reconciliation; existing assignments are not overwritten. Installation adds one CFO role, two company activations, two role assignments, two baseline events, one page and one page grant: nine authority/configuration rows, zero period or stock rows.

Endpoints at `/api/v1/accounts/inventory-periods` list/read periods and accept open and close commands. Opening accepts code, name, inclusive start/end dates, reason and idempotency key. Closing accepts current version, reason and idempotency key. Company and resolved CFO authority come from the authenticated request. Period dates cannot overlap even when an earlier period is closed. Dates and identity are immutable, and closed periods cannot reopen. Replay returns the retained result; a changed payload under the same key is refused.

The migration reuses `financial_periods` and adds a private event table. The date uniqueness index includes PeriodType, allowing the financial-year and inventory calendars to cover the same dates; inventory-only overlapping periods remain prohibited. Rollback refuses cross-type date duplicates that the earlier index cannot represent. Installer provisioning repairs private ACL drift; status detects missing/disabled guards. A runtime session cannot append period evidence by setting a custom session variable. Actual stock posting must lock the same period row as close; that posting integration is still outstanding.

Unused installation seed/package can be removed. Retained period decisions, changed CFO authority, additional grants or recorded CFO commands block rollback. Do not erase history to make a rollback pass.

Frontend contract change: a new inventory-period page and these request/read contracts. No existing screen is changed by this backend-only candidate. Shared frozen final candidate: clean Debug/Release builds; full Debug 999/999 and Release 996/996, zero failed/skipped; focused period, assessment, evidence, migration, installer, model and read-path checks 14/14 each. Retained TRX and all indexed source hashes verified. These results cover the final combined source; they are not fresh full-suite runs of each intermediate item commit.

Field boundary: disposable PostgreSQL 17 on Windows, newly generated migration chains, normalized LF embedded SQL and newly provisioned role history. Tests use `advance_parser` and an isolated database named `sess_nexa_erp`; neither is the real pre-75 field database. They do not reproduce the field dump, SCRAM globals, prior grants or operating-system provenance. The exact pre-75 witness remains pending.

The CFO role independently requires mandatory MFA, even when its holder has no MD role. An optional-MFA identity is refused before reaching commands. The middleware regression covers CFO alone with and without validated MFA; this is not a claim of a live field-provider login witness.
