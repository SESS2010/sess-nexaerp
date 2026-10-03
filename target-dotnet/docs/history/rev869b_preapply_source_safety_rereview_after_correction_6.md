# REV869B pre-apply source-safety re-review after correction 6

## Canonical result

`rev869b_source_safety_state=FAIL`

`rev869b_execution_helper_readiness_state=FAIL`

This is an independent, source-only and read-only review of the sixth controlled correction. No PostgreSQL test was run, no PostgreSQL server was accessed, and no database helper, migration application/removal, backup, restore, production, REV861, frontend, REV869C, AWS, or legacy-reference operation was performed. Passing compilation and offline tests do not override the material blockers below.

## Gate and reviewed range

- Reviewed commit: `d688bc37d4ed672e0a322d2b00f22a459c6101e0`
- Required parent and actual parent: `c494ba2e63b23696f6ee92433015bd4e398da434`
- Review range: `c494ba2e63b23696f6ee92433015bd4e398da434..d688bc37d4ed672e0a322d2b00f22a459c6101e0`
- Entry target-scoped status: clean
- Entry legacy-reference worktree/range diff: zero
- Range size: 18 files, 685 insertions, 135 deletions
- Scope verdict: the range contains only the REV869B checkpoint, controlled REV869B API/application/domain/infrastructure/migration source, and REV869B tests. No unrelated source path was found.

## Exact reviewed-file list

1. `outputs/rev869b_source_correction_checkpoint_6.md`
2. `src/SESS.NexaERP.Api/Endpoints/Rev869BPurchaseEndpoints.cs`
3. `src/SESS.NexaERP.Application/Purchase/Rev869BPurchaseContracts.cs`
4. `src/SESS.NexaERP.Domain/Purchase/Rev869BPurchaseTransactions.cs`
5. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.Designer.cs`
6. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.cs`
7. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/NexaErpDbContextModelSnapshot.cs`
8. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BControlledMutationSql.cs`
9. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BDatabaseSafetySql.cs`
10. `src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.Rev869B.cs`
11. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.ComparisonPo.cs`
12. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.MaterialFollowUp.cs`
13. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.RfqQuotation.cs`
14. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs`
15. `tests/SESS.NexaERP.Tests/Rev869BDatabaseSafetyContractTests.cs`
16. `tests/SESS.NexaERP.Tests/Rev869BPostgresApplicationBehaviorTests.cs`
17. `tests/SESS.NexaERP.Tests/Rev869BPostgresBehaviorTests.cs`
18. `tests/SESS.NexaERP.Tests/Rev869BPurchaseCorrectionTests.cs`

## Finding-by-finding verdict

| Area | Verdict | Independent finding |
|---|---|---|
| Workflow/status transitions | PARTIAL | Explicit transition guards and the Material Follow-up state machine are present, but technical-verification insertion is not transaction-bound to a required history event and same-status version/audit-only updates remain possible by direct SQL. |
| Organization/record scope | PARTIAL | Service queries and endpoint reads bind organization and record scope; mutation SQL protects parent/organization identities. Future PostgreSQL proof remains incomplete and depends partly on a pre-existing direct fixture graph. |
| Permissions/direct URL security | PARTIAL | Endpoint mappings require page actions; attachment/export reads apply organization and record scope and audit denial. The new mapped pipeline design proves only authenticated success, not mapped 400/401/403/404/409 or unauthorized attachment/export outcomes. |
| Approvals/segregation of duties | FAIL | Service checks exist, but database checks placed in `rev869b_write_bound_history()` are dropped before trigger installation and no installed trigger calls that function. A direct transaction can therefore update a parent and add matching history without database-enforced creator/approver or submitter/approver separation. |
| Commercial/GST/payable calculations | PARTIAL | Canonical calculations and exact comparison predicates are extensive; nullable `taxRule <>` was replaced with fail-closed `IS NULL`/`IS DISTINCT FROM` logic and authoritative predicates require exact TRUE. Runtime negative evidence still uses generic exceptions and does not prove the intended predicate for each malformed/tampered case. |
| Immutable snapshots/provenance | FAIL | Invitation snapshots are immutable and many qualification attributes reconcile. Approver/verifier semantics are not authoritative, event-time validity is checked against quotation receipt rather than the invitation snapshot event, and the PO predicate omits several qualification fields it claims to prove. |
| Mutation/transition controls | PARTIAL | All 15 tables now have explicit trigger treatment, exact `+1` versions for controlled updates, version-zero insert checks, and destructive-delete guards. Gaps described in the matrix and history analysis remain material. |
| History/audit integrity | FAIL | The five-second heuristic is gone and same-transaction parent binding is a strong improvement. Correlation is merely nonblank rather than bound to an authoritative command, technical verification lacks a required bound-history trigger, and database segregation checks are not installed. |
| Idempotency/concurrency/rollback | FAIL | Two contexts/connections/services and coordinated concurrent start are genuine. The committed winner makes nonambient fixture disposal fail before cleanup; immutable REV869B/history rows cannot be safely deleted. Rollback counting also omits supporting prerequisite state mutations. |
| PostgreSQL fixture/test design | FAIL | Application tests create deterministic local fixtures. Direct SQL tests still require a pre-existing exact global graph and use eight generic `PostgresException` assertions. PostgreSQL tests were correctly not run. |
| Migration Up/Down safety | PARTIAL | One retained migration, model parity, deterministic offline SQL, owned seed removal, and expected object inventory were reproduced. Runtime trigger syntax/behavior and preservation cannot be accepted without safe isolated execution. |
| Evidence/helper readiness | FAIL | Offline evidence is reproducible, but the future execution suite is not safely self-contained or cleanup-safe and cannot yield precise guard evidence. |

## Fifteen-table mutation-control matrix

| REV869B table | INSERT | UPDATE | DELETE | Independent verdict |
|---|---|---|---|---|
| `purchase_approval_policies` | Requires version 0, authorized actor, effective-date/amount validity, and non-overlap | Exact +1; activation state is the controlled mutable fact; server timestamp/history logic present | Rejected | PARTIAL: retained history and overlap logic are present, but runtime exact-guard proof is absent. |
| `purchase_transaction_status_history` | Parent/org/status/action/version/identity/role/correlation/time checks present | Rejected | Rejected | FAIL: correlation is only nonblank; technical verification can exist without a bound row; database SOD is incomplete. |
| `request_for_quotations` | Version 0 and initial Draft contract | Exact +1 and allowlisted lifecycle/edit changes | Rejected | PARTIAL: same-status audit/version-only direct updates are allowed without bound history/actor validation. |
| `request_for_quotation_lines` | Version 0 under the exact Draft/version-0 parent | Rejected after creation | Rejected | PASS at source-contract level; runtime trigger evidence remains NOT RUN. |
| `rfq_vendor_invitations` | Version 0, Issued, exact RFQ/provenance contract | Exact +1 status progression; protected snapshots/identity retained | Rejected | PARTIAL: qualification provenance/event-time concerns remain. |
| `vendor_quotations` | Version 0 and controlled initial submission contract | Exact +1; status/current-version allowlist | Rejected | PARTIAL: initial service transaction relies on deferred final state; runtime evidence is required. |
| `vendor_quotation_lines` | Version 0 under exact Draft/version-0 quotation | Rejected after creation | Rejected | PASS at source-contract level; runtime trigger evidence remains NOT RUN. |
| `quotation_technical_verifications` | Version 0 and submitted-parent constraints | Rejected | Rejected | FAIL: immutable row creation is not required to have a matching same-transaction status-history event. |
| `commercial_comparisons` | Version 0 Draft and exact ancestry | Exact +1 with Draft/RevisionRequested and lifecycle allowlists | Rejected | PARTIAL: same-status audit/version-only direct updates and runtime reconciliation proof remain gaps. |
| `commercial_comparison_lines` | Version 0 under Draft/version-0 parent | Exact +1 recommendation correction only in Draft/RevisionRequested | Rejected | PASS at source-contract level; destructive correction was removed. |
| `purchase_transaction_approval_history` | Exact comparison/organization/level/action/actor/version binding | Rejected | Rejected | PARTIAL: same-transaction binding exists, but complete database SOD/correlation binding does not. |
| `purchase_orders` | Version 0 Draft/RevisionDraft with approved comparison ancestry | Exact +1 with lifecycle and protected-field allowlists | Rejected | PARTIAL: direct database SOD and complete qualification provenance remain incomplete. |
| `purchase_order_lines` | Version 0 under exact Draft/RevisionDraft version-0 parent | Rejected after creation | Rejected | PASS at source-contract level; runtime trigger evidence remains NOT RUN. |
| `purchase_order_history` | Exact PO/organization/action/actor/version binding | Rejected | Rejected | PARTIAL: same-transaction binding exists, but correlation and full SOD requirements are incomplete. |
| `material_follow_up_handoffs` | Version 0 PendingFollowUp for current Issued PO/line | Exact +1 PendingFollowUp -> InProgress -> Completed | Rejected | PARTIAL: actor/reason/history are represented through the transition plus required status history, but safe execution/cleanup proof is absent. |

Across the matrix, parent, organization, owner, current-version, snapshot, issue/cancellation, and identity substitutions are generally rejected by column allowlists or immutable-row guards. No controlled/history table has an accepted destructive DELETE path. The material FAIL findings concern authority/binding and executable evidence, not an observed broad DELETE opening.

## Lifecycle matrix

| Aggregate | Allowed source transitions | Denied/terminal behavior | Verdict |
|---|---|---|---|
| RFQ | Draft -> Issued; controlled later states retained from the approved workflow | Invalid edges and destructive delete rejected | PARTIAL because direct same-status version/audit changes lack bound actor evidence. |
| Vendor invitation | Issued -> QuotationReceived through controlled processing | Snapshot substitution/delete rejected | PARTIAL because provenance timing/approver semantics are incomplete. |
| Vendor quotation | Draft/Submitted processing with current revision discipline | Protected commercial values and delete rejected | PARTIAL pending exact runtime evidence. |
| Technical verification | Submitted immutable verification fact | Update/delete rejected | FAIL because insertion need not be paired with required history. |
| Commercial comparison | Draft -> Recommended -> approval outcome; RevisionRequested -> resubmission path | Invalid/terminal changes rejected | PARTIAL; database SOD is incomplete. |
| Purchase order | Draft/RevisionDraft -> Submitted -> approval/rejection -> Issued; rejected revision/resubmission source paths exist | Terminal mutation/delete rejected; cancellation controlled | PARTIAL; repeated lifecycle is designed but the direct fixture cannot safely execute/clean up and exact per-guard evidence is absent. |
| Material Follow-up | PendingFollowUp -> InProgress -> Completed | Skips, repeats, reverse transitions, mutation after Completed, and delete rejected | PASS at source-transition level; execution evidence remains blocked. |
| Approval policy | Version-0 new policy; exact-version activation/deactivation with effective/amount non-overlap rules | Arbitrary field mutation and delete rejected | PARTIAL pending precise runtime proof. |

## History fabrication and segregation analysis

The correction removes the prior five-second timestamp-proximity heuristic. `rev869b_guard_history_insert()` now checks a parent whose `xmin` matches `txid_current()`, exact organization/document/to-status, parent version, employee/login/role membership, mandatory correlation, server-time tolerance, and mandatory exception remarks. Deferred parent triggers require matching status/approval/PO-history rows for controlled transitions. This materially narrows standalone fabricated-history attacks.

It does not complete the required contract:

1. `rev869b_write_bound_history()` contains creator-self-approval and issuer/approver separation checks, but the function is dropped before trigger installation and no installed trigger calls it. Those database SOD checks are dead source, not an applied control.
2. A caller-supplied nonblank `CorrelationId` is accepted. It is not matched to an authoritative parent command/idempotency field or a server-derived command fingerprint.
3. No deferred parent/history trigger requires a history row for `quotation_technical_verifications`.
4. Same-status aggregate updates return without a required history event while allowlists permit version/audit columns to change. A direct SQL caller can consume versions and supply audit identity text without the complete actor proof required for a controlled transition.
5. Same-transaction `xmin` binding proves transaction proximity, not necessarily a unique application command when several changes occur in one transaction.

Therefore direct SQL fabrication resistance and exact employee/login/role/correlation binding are not complete.

## Commercial, tax and provenance analysis

The retained nullable comparison `CommercialSnapshotJson->'taxRule' <> TaxRuleSnapshotJson` is absent. The new source rejects a missing `taxRule` and uses `IS DISTINCT FROM`; authoritative Boolean reconciliation is checked with `IS NOT TRUE`, so FALSE and NULL fail. Source predicates reconcile tax-rule JSON, taxable value, charges, discounts, CGST, SGST, IGST, total tax, total payable, currency, precision, quotation version, comparison version, organization, and parent lineage. Server-side services recalculate authoritative commercial values.

No obvious SQL NULL/UNKNOWN fail-open was found in the corrected canonical comparison. Malformed typed JSON can raise and is converted by the authoritative reconciliation trigger to a closed failure for its handled data/cast exceptions. However, the future PostgreSQL tests combine mutations and assert only generic `PostgresException`; they do not prove missing, JSON null, malformed, wrong-type, wrong-version, wrong-organization, and fabricated-total cases reach the intended exact guard.

Qualification snapshots include qualification ID, vendor, organization, category/type, version, effective dates, verification/approval/active states, an `approvedBy` value, and snapshot time, and invitation snapshots are protected from later mutation. Remaining material gaps are:

- `approvedBy = UpdatedBy ?? CreatedBy` is audit provenance, not an independently authoritative approver/verifier identity.
- The SQL validity check is tied to quotation `ReceivedAt`, while the controlled provenance event is invitation/snapshot time; a qualification valid when invited can be rejected after later expiry, and the exact event-time contract is not proved.
- The PO-side qualification predicate checks only a subset (ID/vendor/organization/category/version/approval/active) and omits type, effective dates, verification state, and approver identity in that local exact-provenance assertion.

Cross-organization and parent identities are otherwise explicitly compared throughout RFQ, comparison, and PO construction and mutation guards.

## Service, endpoint, rollback and concurrency analysis

The new mapped success test builds an ASP.NET application, installs authentication/authorization, maps the real REV869B endpoints, sends an authenticated HTTP POST, and verifies both the RFQ and audit row. This is genuine pipeline success design. Source mappings require page permissions for every write/read/export/attachment action, and attachment/export handlers additionally enforce organization and record scope and write audit evidence.

No new mapped-pipeline test proves 400, 401, 403, 404, or 409, and no mapped-pipeline test proves unauthorized attachment/export denial. Unit-level `Run`/handler tests are useful but do not replace those pipeline cases.

The concurrency test genuinely creates two different DbContexts, connections, and service instances, gates them with a `TaskCompletionSource`, awaits both without `Task.Delay`, verifies a common idempotent result and one RFQ/line, then checks a conflicting replay. It is not execution-safe: it uses `useAmbientTransaction: false`, so the committed winner remains. Fixture disposal first demands the REV869B count equal the pre-test baseline and throws before deleting prerequisites. Even if reordered, immutable RFQ/history rows cannot be deleted by the fixture. The test also proves aggregate winner counts, not an exhaustive zero-partial-loser inventory.

Audit-writer rollback verification uses a fresh independent DbContext and is a real improvement. Its `CountOwnedAsync` covers the 15 REV869B relations, audit rows, and number-sequence rows. It does not compare supporting prerequisite state (the Purchase Requirement handoff, PR/line, warehouse, and identity mapping fields), so it does not prove every possible partial mutation of supporting transaction data. The fixture cleanup deletes only deterministic prerequisite IDs, which is appropriately ownership-scoped when no REV869B winner was committed.

## PostgreSQL fixture and negative-test design

PostgreSQL tests were compiled/listed only and were **NOT RUN**.

Material blockers explicitly rechecked:

1. **Pre-existing direct fixture graph remains.** `Rev869BPostgresBehaviorTests` uses organization `REV869B-PG-DIRECT-TEST-OWNED`; `OpenVerifiedAsync()` calls `RequireExactOwnedFixtureAsync()`, which SELECT-counts existing rows and requires exactly one. It does not create the graph it consumes. Renaming the marker does not make the fixture self-created or deterministic.
2. **Generic negative evidence remains.** Eight negative cases use `Assert.ThrowsAsync<PostgresException>` without asserting exact SQLSTATE and exact constraint/trigger identity. Combined tampering can be rejected by an earlier immutable guard, so the intended reconciliation/history guard is not proved.
3. **Concurrency-winner cleanup remains unsafe.** The nonambient concurrent winner is committed, its immutable REV869B/history graph cannot be destructively removed, and fixture disposal fails before prerequisite cleanup.

Controlled timestamps and deterministic IDs are present in the application fixture. No randomized `GetHashCode()` dependency was found. The safe cases using an ambient transaction roll back and independently check the owned baseline. Those improvements do not resolve the three blockers above.

## Migration Up/Down and offline inventory

- Retained migration ID: `20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation`
- Discovered migrations with `--no-connect`: 13
- REV869B occurrence/order: exactly once, immediately after REV869A
- Exact model/snapshot parity test: PASS (1/1)
- Offline Up SQL: 174,802 bytes; SHA-256 `9ED9E9386CA55A4D0823C10DB0F21343B33AF07BD32A0504F98ADF32225DC3CA`
- Offline Down SQL: 6,672 bytes; SHA-256 `EA2D5BA6F173E71DA2C25067FB21F1ECC75F66A3FDEF73CD7EE6377FA17689C4`
- Up inventory: 15 tables; 72 trigger-create occurrences / 72 unique trigger names; 17 function-create occurrences / 16 unique function names; 44 foreign keys; 66 indexes; 29 checks
- Offline generation used a deliberately unreachable loopback connection and did not connect.
- Down source remains scoped to REV869B-owned migration objects/seed handling; no source evidence of non-REV869B data deletion was found. Runtime preservation remains unproved because database execution was prohibited.

## Independently reproduced validation evidence

| Validation | Result |
|---|---|
| Entry commit/parent/status gate | PASS |
| Reviewed path/scope inspection | PASS: 18 controlled paths |
| `git diff --check` on review range | PASS |
| Solution build | PASS: 0 warnings, 0 errors |
| Focused REV869B non-PostgreSQL tests | PASS: 48/48 |
| Complete non-PostgreSQL tests (`FullyQualifiedName!~Postgres`) | PASS: 422/422 |
| REV869B PostgreSQL discovery | 22 methods compiled/listed; **NOT RUN** |
| PowerShell AST validation | PASS: 23 files, 0 parse errors |
| EF discovery with `--no-connect` | PASS: 13 migrations, REV869B exactly once after REV869A |
| Exact model/snapshot parity | PASS: 1/1 |
| Offline Up/Down generation and independent hash | PASS; hashes and sizes above |
| SQL inventory | PASS as an offline textual inventory; runtime semantics NOT RUN |
| Secret/privacy/prohibited-operation scan | No new secret/private-key/password material in the reviewed correction; prohibited words found only in historical test assertions/checkpoint declarations, not as operations performed by this review |
| Legacy-reference | Unchanged; zero range and worktree diff established at entry |

## Blocking findings

1. The direct PostgreSQL suite is not self-contained and still requires a pre-existing exact fixture graph.
2. Eight direct negative tests accept any `PostgresException` and do not prove exact SQLSTATE plus exact intended guard/trigger.
3. The real concurrent committed winner has no cleanup path compatible with immutable history, and fixture disposal necessarily fails/leaks.
4. Database-level creator/approver and submitter/approver segregation logic is dead/dropped rather than installed.
5. Technical-verification INSERT is not transaction-bound to mandatory status history.
6. Correlation/idempotency history binding is only a nonblank check, not exact authoritative binding.
7. Qualification approver/verifier provenance, event-time validity, and PO-side exact-field reconciliation are incomplete.
8. Rollback proof does not compare all supporting prerequisite state, and endpoint error/security cases are not proved through the mapped pipeline.

## Required correction before another review

1. Make every direct PostgreSQL test create its entire deterministic owned graph from accepted seeds, prove exact nonexistence before creation, and remove the pre-existing `REV869B-PG-DIRECT-TEST-OWNED` dependency.
2. Isolate each mutation and assert exact SQLSTATE and exact constraint/trigger/function evidence, including zero-row UPDATE/DELETE checks and all JSON/tax/version/org/provenance cases.
3. Redesign committed concurrency proof so cleanup is guaranteed in `finally`, test-owned only, and compatible with immutable histories; alternatively use a disposable test-owned database boundary when authorized by a later execution plan. Prove every loser-side table remains empty.
4. Install database-enforced creator/submitter/issuer versus approver/verifier separation in triggers that actually execute; remove dead security code.
5. Require an exact same-command bound history event for technical-verification creation and close same-status audit/version mutation paths or bind them to authorized history.
6. Bind correlation/idempotency to an authoritative parent command value or server-derived fingerprint, not arbitrary nonblank text.
7. Store and reconcile a genuine authoritative qualification approver/verifier identity, use the approved business event time consistently, and make PO reconciliation exact across every retained qualification field.
8. Expand rollback verification to compare supporting transaction state and add real mapped-pipeline 400/401/403/404/409 plus unauthorized attachment/export tests.

## Improvements that are not blockers by themselves

- Consolidate dense migration SQL into auditable named fragments while preserving deterministic output.
- Add source-contract assertions that every declared SOD function is referenced by an installed trigger and that no security function is dropped before use.
- Report trigger/function inventory as both occurrences and unique definitions, as done here, to make intentional replacement explicit.

## Exact next gate

Stop here. Do not provide or execute a PostgreSQL, helper, or migration command. The next authorized activity must be a **seventh controlled source-only REV869B correction** based on the commit containing this single report. It must modify only the minimum controlled REV869B source/tests and its correction checkpoint, resolve every blocking and required-correction item above without accessing PostgreSQL, retain the existing REV869B migration ID, and be followed by a new independent source-only safety re-review. Source-safety PASS and execution-helper readiness may not be claimed before that re-review reproduces exact evidence and finds no material blocker.
