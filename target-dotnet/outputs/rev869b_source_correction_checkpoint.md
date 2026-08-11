# REV869B Controlled Source Correction Checkpoint

Date: 2026-08-11 (Asia/Calcutta)

- Starting commit: `ab4f79a047f6c2bd1eb43952284fe9a527fee626`
- Ending commit: the correction commit containing this checkpoint (reported in the Git handoff)
- Unchanged migration ID: `20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation`
- Scope: source-only correction in `target-dotnet`
- Database/PostgreSQL access or migration application: not performed
- Disposition: awaiting a new independent source-safety re-review; database acceptance and production readiness remain unassessed

## Finding corrections

| Finding | Correction |
|---|---|
| N-01 | Removed `Recommended` from the comparison status check and synchronized the canonical lifecycle `Draft -> PendingApproval -> Approved/Rejected/RevisionRequested`, with revision resubmission and cancellation edges. Domain, EF mapping, migration, designer, snapshot, service, and tests agree. Recommendation remains an action/data property, not a status. |
| N-02 | Approval policies now cover decimal(24,6) continuously and without overlap: Manager 0 through 50000; Technical Director 50000.000001 through 500000; Managing Director 500000.000001 through 999999999999999999.999999. Resolution uses total payable value. Tests execute exact/below/above boundary cases. |
| N-03 | Server calculation now derives taxable value as quantity times rate plus taxable charges minus discount, then applies effective-dated GST and rounding. Stored quotation values are recalculated and reconciled before reuse; comparison and PO aggregates use guarded addition. |
| N-04 | Added immutable rejected-initial-PO recovery: rejection makes the rejected row non-current; `revise-rejected` creates a linked `RevisionDraft` copy with required reason and terms; submit produces `Resubmitted`; approval promotes the new row; issue remains approval-gated. The rejected row and histories are retained. Issued-amendment rejection continues to retain its issued predecessor. |
| N-05 | Identity, organization, role, page, record-scope, scoped-missing, commercial masking, service authorization, segregation, approver, self-approval, conflict, amendment, cancellation, and issue denials now reach an awaited audit write before the denial result. Audit exceptions are not swallowed. Existing page permissions cover attachment and export entry points. |
| N-06 | Added PostgreSQL transition triggers for RFQ, quotation, comparison, and PO. The PO trigger validates complete terms, route, immutable line/tax snapshots, positive quantities, and header/line reconciliation before Approved-to-Issued. Snapshot guards protect PO commercial/provenance data across all statuses. |
| N-07 / R-12 | Added executable behavior tests for calculation sequence/overflow, complete pre-issue snapshots, canonical matrices, decimal boundaries, fail-closed service identity/role/validation branches, API 400/401/403/404/409 behavior, denial auditing, and propagation of audit-write failure. Existing EF model-differ and concurrency contract tests remain active. PostgreSQL trigger, true concurrent transaction, and rollback execution remain the later isolated acceptance gate. |
| R-02 | Scoped replay checks now compare material payloads for RFQ, invitation, quotation lines/provenance, recommendation, PO submission/approval, amendment, and rejected-PO revision. Equivalent retries return original aggregate evidence; conflicting reuse throws a deterministic conflict and is audited by the API boundary. |
| R-07 | Validation, malformed commercial input, overflow, and invalid domain operations map to 400; unauthenticated identity maps to 401; authorization maps to 403; organization-scoped misses map to 404 without existence disclosure; concurrency and idempotency conflicts map to 409. |
| I-02 | numeric(24,6) bounds are enforced for inputs, multiplication, taxable charges, tax components, aggregate sums, and final payable values before persistence. Overflow is normalized to controlled validation behavior and HTTP 400. |

## Preserved contracts

- Existing architecture and all accepted REV868, REV868C3, and REV869A migration identifiers and behavior were left intact.
- Organization and parent-chain validation, vendor qualification, technical/commercial person segregation, attachment provenance, append-only histories, least-privilege permission rows, Material Follow-up uniqueness, and configuration-based approval routing remain enforced.
- No employee-specific workflow rule was introduced.
- No source outside `target-dotnet` was changed, and `../legacy-reference/` was not accessed.

## Executable validation evidence

- Solution build: succeeded with 0 warnings and 0 errors.
- Focused `FullyQualifiedName~Rev869B`: 37 passed, 0 failed, 0 skipped.
- Complete non-PostgreSQL suite: 411 passed, 0 failed, 0 skipped; PostgreSQL/Postgres-named tests were explicitly excluded.
- EF migration discovery: 13 migrations with `--no-connect`; REV869B exactly once and directly after REV869A.
- EF pending-model check: no changes since the last migration.
- Migration/model/snapshot parity: exercised by the focused design-time model-differ test using an unreachable port-1 provider configuration.
- Offline Up SQL: 77,361 bytes; 15 owned table creates, 20 REV869B triggers, 44 foreign keys, and 66 indexes; SHA-256 `7A93EADD591A0046BCC04137BBE043DBF08F0A51F7630E72A917D9B865C312FD`.
- Offline Down SQL: 4,508 bytes; SHA-256 `BAC1AF4A71ED86113BEE2614D8E66A0633C2F22AAFAAF2A63C42E4475FE68D28`.
- Migration contract: 15 owned tables, 20 triggers, 4 owned trigger functions, canonical checks, parent guards, restricted foreign keys, indexes, 4 page definitions, 29 fixed-role permission rows, 3 approval policies, and 3 fail-closed Department Manager permission rows.
- No database connection, migration application/removal, helper, backup, restore, production, REV861, REV869C, frontend, or prohibited subsystem operation was performed.

## Remaining blockers

1. A new independent source-safety re-review must evaluate this commit; this checkpoint does not self-approve.
2. PostgreSQL Up/Down application, trigger behavior, true concurrency, transaction rollback, and database constraint acceptance require a later isolated and separately authorized gate.
3. Production deployment/readiness, OIDC activation, frontend work, REV861, REV869C, and other excluded subsystems remain outside this revision.
