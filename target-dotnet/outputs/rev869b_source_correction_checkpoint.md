# REV869B Source Correction Checkpoint

Date: 2026-08-11 (Asia/Calcutta)

- Starting commit: `f18a557641683c6374493c5c0a097ab6bca1b405`
- Migration: `20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation`
- Scope: source-only correction of the unapplied REV869B migration, model, services, API, tests, and reports
- Database/PostgreSQL execution: **NOT RUN**
- Safety conclusion: corrections are ready for a new independent source review; this report does **not** claim source-safety or database acceptance PASS.

## Corrected contracts

Canonical statuses are now defined once in `Rev869BStatusContracts` and enforced by service transition matrices, EF checks, migration checks, and exhaustive offline matrix tests. Quotation states are `Submitted`, `TechnicallyCompliant`, `TechnicallyRejected`, `Superseded`, `Withdrawn`, and `Rejected`. Comparison states are `Draft`, `PendingApproval`, `Approved`, `Rejected`, `RevisionRequested`, and `Cancelled`. PO states are `Draft`, `PendingApproval`, `Approved`, `Issued`, `Rejected`, `Superseded`, and `Cancelled`. No request accepts an arbitrary body-supplied status.

Material commands carry the applicable expected aggregate version. RFQ, invitation, quotation, comparison, and PO reservations use an atomic `ExecuteUpdateAsync` predicate containing ID, organization scope, and expected Version, increment Version in the same operation, and throw a concurrency exception when zero rows are affected. PO amendment now also reserves its issued predecessor through this CAS path. The API maps concurrency and domain conflict to 409, validation to 400, missing records to 404, and authorization to 403.

Commercial values are recalculated from persisted quotation-line inputs and effective REV869A tax settings. Quotation lines retain HSN/SAC, supplier state, place-of-supply state, vendor registration type, component values, tax snapshot, and result. Approval policy resolution is effective-dated and requires exactly one match at the approved boundaries: Manager through 50,000 inclusive; TD above 50,000 through 500,000 inclusive; MD above 500,000. Missing/overlapping policies, manager mappings, identity, scope, vendor qualification, tax configuration, or self-approval fail closed.

The PO lifecycle is explicit: approved comparison to Draft; Draft to PendingApproval; approval to Approved; only approved current version to Issued. Amendment retains the issued version and creates a non-current Draft linked by `PreviousVersionId`; approval atomically supersedes the issued predecessor and promotes the amendment. Rejection leaves the issued predecessor unchanged. Issue creates one handoff per PO line using the PO UUID in its globally unique number.

## Review finding disposition

| Finding | Original defect | Corrected source/rule | Regression evidence / remaining limitation |
|---|---|---|---|
| B-01 | Technical quotation terminal statuses violated the DB check. | Canonical quotation set and migration/model check now include both technical terminal results; final verification, status history, and audit are one transaction. | Exhaustive status-matrix and constraint-set tests. PostgreSQL constraint execution remains pending. |
| B-02 | Passive Version tokens allowed lost updates. | Organization-scoped CAS reservations atomically match/increment Version for every existing aggregate command; stale updates throw and map to 409. | Request-version reflection, CAS contract, and model tests. True concurrent PostgreSQL execution remains pending. |
| B-03 | GET returned commercial values to view-only roles. | Comparison/PO reads require `ViewCommercialValues`; otherwise explicit non-commercial projections are returned. | API masking/permission contract tests. |
| B-04 | No role could approve a PO amendment. | PO submit/approve/reject endpoints and 29-row least-privilege matrix grant PO approval to Purchase Manager, TD, and MD; approval route is recalculated. | Permission-row and endpoint contract tests. |
| B-05 | Cross-organization/parent UUID substitution was possible. | Queries bind organization and parent IDs before mutation; migration adds fail-closed quotation/comparison/PO/line/follow-up parent-chain triggers. | Parent-trigger and scoped-query negative contract tests. PostgreSQL trigger execution remains pending. |
| R-01 | State/history/audit writes were not consistently atomic. | All multi-record commands use explicit serializable transactions; the audit writer shares the scoped DbContext and saves before commit. | Transaction/audit source contract tests; rollback behavior awaits PostgreSQL. |
| R-02 | Replay behavior and payload checks were incomplete. | Commands check scoped correlation/idempotency evidence, return exact prior results for equivalent retries, and reject conflicting reuse. | Idempotency and scoped unique-arbiter tests. |
| R-03 | PO skipped category qualification. | PO creation revalidates every selected quotation item category at execution time. | Service contract test for per-category eligibility. |
| R-04 | Quote provenance/attachment evidence was absent. | Quote header requires source, received time, object key, SHA-256, vendor attestation; DB validates source/hash/time and submitted headers are immutable. | Provenance model/check tests. Object-storage existence is outside this source-only boundary. |
| R-05 | Technical/commercial person-level segregation was missing. | Comparison approval rejects an employee who technically verified any selected quotation line. | Duty-segregation query/authorization test. |
| R-06 | Matrix was overbroad and contained all-false rows. | Fixed matrix is reduced from 48 to 29 non-empty rows; Purchase Executive, Stores, Technical Engineer, and Accounts access is page-limited; PO approvers are explicit. | Exact count, no-empty-row, view/commercial/export coherence tests. Three scoped Department Manager rows remain fail-closed migration SQL. |
| R-07 | API collapsed validation/not-found/conflict. | Dedicated validation, conflict, and not-found exceptions map to 400/409/404; authorization remains 403. | Endpoint semantics tests. |
| R-08 | Material Follow-up read was unbounded. | Page/pageSize are validated and capped at 100; query uses deterministic ordering, Skip, and Take. | Pagination contract test. |
| R-09 | Direct record read denials lacked audit. | RFQ/comparison/PO/list scope denials write sanitized Security/Denied audit evidence before 403. | Audited-denial contract test. |
| R-10 | Completed commercial snapshots lacked DB immutability. | Controlled snapshot triggers protect submitted quotation terms, recommended/approved comparison snapshots, and issued/cancelled/superseded PO terms while allowing approved lifecycle fields and RevisionRequested editing. | Trigger ownership/count and transition tests; PostgreSQL execution pending. |
| R-11 | Global handoff and invitation idempotency keys could collide across organizations. | Handoff number contains PO UUID; invitation idempotency unique arbiter is `(RequestForQuotationId, IdempotencyKey)`; technical replay relies on its unique quotation-line parent rather than a global correlation key. | Index/model and deterministic-number tests. |
| R-12 | Tests were largely source-string checks. | Added executable exhaustive domain transition, calculation boundary/maximum, deterministic permission, request-version, and EF design-time model-differ tests, plus focused migration/API contracts. | Offline suite passes; database trigger/concurrency behavior requires later isolated PostgreSQL tests. |

## Corrected migration contract

The unchanged migration ID owns 15 tables, 309 columns, 15 PKs, 66 indexes, 44 restricted FKs, 29 checks, 16 triggers, and 3 trigger functions. It seeds 4 pages, 29 fixed-role permission rows, 3 approval policies, and 3 fail-closed Department Manager permissions. It seeds no vendor, employee, quotation, PO, or other business transaction. Up creates dependencies before indexes/seeds/triggers. Down removes the 16 table-owned triggers with the tables, drops only the 3 REV869B functions, removes the exact 29 generated permission rows plus the 3 ownership-guarded Department Manager rows, and drops only the 15 REV869B tables in dependency-safe order.

REV868, REV868C3, and REV869A entities, migrations, histories, PRs, approvals, reservations, employees, and accepted data remain unaltered. Existing approved PR and PendingRFQ handoff records are referenced and never duplicated.

## Validation evidence

- Build: PASS; 0 warnings and 0 errors.
- Focused REV869B tests: PASS; 26 passed, 0 failed, 0 skipped.
- Complete non-PostgreSQL tests: PASS; 411 passed, 0 failed, 0 skipped; PostgreSQL-backed classes were excluded.
- EF migration discovery: PASS; 13 migrations discovered with `--no-connect`, REV869B exactly once after REV869A.
- EF model/snapshot parity: PASS through an in-process `IMigrationsModelDiffer` test using a port-1 dummy provider configuration and no connection.
- Offline Up SQL: PASS; `START TRANSACTION`/`COMMIT`, 15 table creates and 16 trigger creates; SHA-256 `e48b74e3c057e5f648ed6d87405ee130e006230154ef30755a2646c535cf3481`.
- Offline Down SQL: PASS; `START TRANSACTION`/`COMMIT`, 15 owned table drops and 3 owned function drops; SHA-256 `5af0c755302580dff3792b22784c8c7540a766e87fe115089ba0ec438e687618`.
- PostgreSQL-backed tests and migration application: NOT RUN.
- Secret/privacy/safety scans: PASS; zero secret, employee-PII, or executable database-operation matches in added source.
- `git diff --check`: PASS.

## Remaining blockers

1. A new independent pre-application source review must confirm these corrections; this checkpoint does not self-approve.
2. No isolated PostgreSQL preflight/application/rollback/constraint/concurrency acceptance has occurred for REV869B.
3. Production deployment, production OIDC activation, frontend implementation, REV861, and REV869C remain outside this checkpoint.
