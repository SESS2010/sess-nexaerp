# REV869B pre-apply source-safety re-review after correction 7

## Canonical verdict

`rev869b_source_safety_state=FAIL`

`rev869b_execution_helper_readiness_state=FAIL`

This was an independent source-only review. PostgreSQL tests were compiled/listed and were **NOT RUN**. No PostgreSQL server, database helper, migration apply/remove operation, backup, restore, production system, REV861, frontend, REV869C, AWS resource, or legacy-reference path was accessed.

The seventh correction contains useful structural improvements, and all permitted offline compilation/tests pass. It nevertheless has multiple material source and future-execution blockers. Most critically, the installed deferred Material Follow-up history trigger refers to a column that the table does not have, the qualification actor fields required by the invitation path have no production writer, and the direct PostgreSQL suite is neither self-creating nor internally compatible with the newly installed guards. Passing source-contract tests do not override these reachable defects.

## Entry gate and reviewed range

- Reviewed commit: `e5715cafb66d896c0a7af542bb3de89af4638413`
- Required and actual parent: `a5856312806bbc5929624a6602602df2910eaedc`
- Exact range: `a5856312806bbc5929624a6602602df2910eaedc..e5715cafb66d896c0a7af542bb3de89af4638413`
- Entry target-scoped status: clean.
- The commit contains 18 controlled REV869B source/test/checkpoint paths and no legacy-reference path.
- No unrelated path was found in the reviewed commit.

## Independently enumerated reviewed files

1. `outputs/rev869b_source_correction_checkpoint_7.md`
2. `src/SESS.NexaERP.Domain/Masters/VendorQualification.cs`
3. `src/SESS.NexaERP.Domain/Purchase/Rev869BPurchaseTransactions.cs`
4. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.Designer.cs`
5. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.cs`
6. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/NexaErpDbContextModelSnapshot.cs`
7. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BControlledMutationSql.cs`
8. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BDatabaseSafetySql.cs`
9. `src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.Rev869A.cs`
10. `src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.Rev869B.cs`
11. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.ComparisonPo.cs`
12. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.RfqQuotation.cs`
13. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs`
14. `tests/SESS.NexaERP.Tests/Rev869BDatabaseSafetyContractTests.cs`
15. `tests/SESS.NexaERP.Tests/Rev869BPostgresApplicationBehaviorTests.cs`
16. `tests/SESS.NexaERP.Tests/Rev869BPostgresBehaviorTests.cs`
17. `tests/SESS.NexaERP.Tests/Rev869BPurchaseCorrectionTests.cs`
18. `tests/SESS.NexaERP.Tests/Rev869BPurchaseFoundationTests.cs`

## Finding-by-finding verdict

| Review area | Verdict | Independent result |
|---|---|---|
| Workflow and canonical transitions | FAIL | The state matrices and Material Follow-up `PendingFollowUp -> InProgress -> Completed` structure are present, but the installed deferred follow-up history check reads nonexistent `NEW."TransitionCorrelationId"`; the entity has only `CorrelationId`. A valid follow-up insert/transition will fail when the deferred trigger runs. |
| One-record/one-process and duplicate prevention | PARTIAL | Unique keys and service idempotency exist. Direct SQL can still reserve versions using a caller-selected new correlation plus matching fabricated history, and the future collision tests are intercepted by the initial-correlation guard before reaching the claimed unique-key evidence. |
| Organization/department/record scope | PARTIAL | Application queries and database parent checks generally bind organization. The mapped 404 case is a missing number, not a cross-organization nondisclosure case, and the direct fixture is shared/external. |
| Creator/verifier/approver/issuer segregation | FAIL | Installed history guards add reachable role/SOD checks, but creator comparison is login-text based rather than immutable creator employee identity; initial creator/login data is caller-supplied; PO logic checks a prior submitter/resubmitter history but does not establish an authoritative issuer identity. Direct SQL can fabricate the parent identity and matching history. |
| Permissions/API/export/attachment | PARTIAL | Real routing/auth middleware is exercised. The 403 attachment/export cases only toggle a global fake permission against `NO-SUCH`; they do not prove record scope, denial audit, cross-org masking, or commercial masking. Audit failure and scope denial are not exercised through the mapped pipeline. |
| Amount-based approval routing | PARTIAL | Existing routing source and offline tests remain green; runtime database behavior was not run and SOD/history origin remains bypassable. |
| Commercial/GST/payable reconciliation | PARTIAL | The nullable `taxRule` comparison is fail-closed and the SQL reconciles relational commercial components, totals, versions, organization, and provenance. Future direct tampering statements generally hit the new correlation/allowlist guard first and therefore do not prove the intended reconciliation guard. |
| Qualification provenance | FAIL | Snapshot content and event-time comparisons were expanded, but the required verifier/approver IDs have no production write path. Fabricated JSON can still pass if it copies the complete matching live qualification because snapshot origin is not independently server-bound. |
| Snapshot immutability | PARTIAL | Invitation/quotation/comparison/PO snapshots have strong update/delete guards; authoritative snapshot origin and runnable exact negative evidence remain incomplete. |
| Database mutation/transition controls | FAIL | Explicit mutation/delete/version guards cover the 15 relations structurally, but the follow-up trigger is runtime-invalid and command correlation remains caller-fabricable rather than authoritative. |
| History/audit binding | FAIL | Same-transaction parent/version/from/to/action/login/employee/role/correlation checks are materially improved, including technical history, but arbitrary direct SQL parent+history mutations can satisfy them and several future tests omit required histories. |
| Idempotency/rollback/concurrency | FAIL | Service design is improved, but one rollback assertion contradicts its own five-row prerequisite baseline, cleanup is not guaranteed in an outer `finally`, and concurrency does not prove one committed attempt or exhaustive loser absence. |
| PostgreSQL fixture/cleanup | FAIL | Application tests use disposable owned databases, but their cleanup path can be skipped by earlier disposal/verification failures. All 15 direct tests still consume `REV869B-PG-DIRECT-TEST-OWNED` instead of creating it. |
| Migration/model/Down safety | PARTIAL | Migration identity, count, ordering, model parity, FKs, and deterministic SQL reproduce. Runtime SQL validity and preservation are not accepted because PostgreSQL was not run and the installed follow-up function is defective. |
| Execution-helper readiness | FAIL | Source safety and future test execution safety are not established. |

## Workflow and lifecycle review

The PR-to-RFQ-to-invitation-to-quotation-to-technical-verification-to-comparison-to-approval-to-PO-to-follow-up flow is represented in the service and database guard sources. Exact version `+1`, version-zero inserts, delete rejection, editable-state boundaries, same-status reservation actions, rejected-PO revision ancestry, and the three-state follow-up matrix are present as source structures.

The follow-up path is not executable as written. `rev869b_require_bound_history()` selects `CorrelationId` for Material Follow-up in its parent union, but its final exact-history predicate unconditionally uses `NEW."TransitionCorrelationId"`. `material_followup_handoffs` defines `CorrelationId`, not `TransitionCorrelationId`. `trg_rev869b_bound_followup_history` installs this function for INSERT/UPDATE, so deferred checking will raise an undefined-column/runtime record-field error instead of accepting a correctly bound handoff. This blocks PO issue/follow-up completion behavior and makes the lifecycle verdict FAIL.

The new technical-verification deferred trigger is structurally reachable and uses the technical entity's `CorrelationId`. It requires a same-transaction `TechnicalVerification` status-history row with null old status, exact compliance status/action/actor/version/correlation. This specific correction item is improved, but it was not runtime-proved.

Same-status reservations now require named actions (`ReserveInvitation`, `ReserveComparison`, `ReserveQuotation`, `ReserveTechnicalVerification`, `ReservePurchaseOrder`, `ReserveAmendment`). The database nevertheless accepts any caller-chosen changed correlation if the caller also inserts a matching valid-looking history row. It proves internal agreement, not authoritative command origin.

## Permission, scope, and segregation review

The database history insert trigger is installed and therefore the previously dead SOD logic is now reachable. It checks active employee identity mapping, organization, login, actor employee, role, version, correlation, server-time proximity replacement through same transaction, and nonblank exception remarks. Technical verifier/approver and PO submitter/resubmitter/approver separation queries are present.

The controls remain bypassable by a direct SQL caller able to supply a valid employee/role:

- Creator self-approval compares `ActorLoginId` to the parent's caller-supplied `CreatedBy` login text, not an immutable creator employee ID.
- Aggregate INSERT binds `TransitionCorrelationId` to `IdempotencyKey`, but both values are caller-supplied. UPDATE only requires a nonblank value different from the previous one.
- A direct caller can mutate parent version/status/correlation and insert a matching history in one transaction; no protected server command ledger, session identity, or server-derived fingerprint establishes origin.
- The PO check identifies the actor of prior `PendingApproval`/`Resubmitted` status history. It does not independently bind an issuer identity, despite the required submitter/issuer/approver separation contract.

Application page/action checks remain in place. The mapped test covers HTTP 400/401/403/404/409 and success through real ASP.NET routing, authentication and authorization middleware, but uses `TogglePagePermissions`, `AllowingScope`, and an in-process service fixture. Its 404 is `NO-SUCH`, not a cross-organization record. Attachment/export denial is permission-only, with no denial-audit assertion. Mapped scope denial, audit-writer failure, and commercial-value masking are absent.

## Commercial, GST, and payable reconciliation

The corrected SQL uses fail-closed exact Boolean reconciliation rather than allowing SQL UNKNOWN to pass. It compares tax-rule JSON, taxable value, charges, discounts, CGST, SGST, IGST, total tax, total payable, currency/precision, quotation/comparison versions, organization, and parent provenance. Server-side service calculations remain authoritative in the normal application path.

This is a source improvement, not runtime acceptance. The direct tampering test combines mutations and generally omits a fresh `TransitionCorrelationId` and mandatory histories. Those statements will be rejected by `rev869b_transition_command_correlation`, an update allowlist, or exact-version guard before the intended reconciliation function. The claimed SQLSTATE/object matrix therefore does not prove JSON missing/null/malformed/wrong-type, tax, totals, version, organization, and provenance guards independently.

## Qualification provenance review

The invitation snapshot now records `VendorQualificationId`, vendor, organization, category, qualification code/type, version, effective range, active state, verification/approval statuses, `VerifiedByEmployeeId`, `ApprovedByEmployeeId`, and snapshot time. Database reconciliation uses invitation `InvitedAt`, improving event-time semantics, and comparison/PO checks carry the provenance forward.

Two material defects remain:

1. `InviteVendorAsync` requires both actor IDs to have values. A complete source search finds only reads/mappings/migration definitions for these fields. The sole production qualification creator initializes PendingApproval and never assigns either field; no production assignment exists. Consequently the application cannot create an eligible qualification satisfying the new invitation query.
2. Database validation proves the supplied JSON equals a matching live qualification. It does not prove that JSON was produced by the controlled server path. Direct SQL can copy the complete live row into fabricated eligible JSON and pass solely because that matching row exists.

The two nullable UUID columns, indexes, and Restrict employee FKs are consistent across domain, mapping, migration, designer, and snapshot. Nullability preserves existing REV869A rows but, without a controlled backfill/lifecycle writer, also makes every retained row fail the new invitation predicate.

## Mutation, history, and correlation review

### Improvements verified structurally

- INSERT/UPDATE/DELETE treatment is explicit across the 15 REV869B relations.
- Version-zero and exact `+1` checks exist.
- Controlled business/history deletion is rejected.
- Protected parent, organization, snapshot, and provenance fields have update allowlists.
- Technical and same-status reservation histories are required by deferred triggers.
- History inserts reconcile parent/version/from/to/action/employee/login/role/correlation and require remarks.
- The five-second proximity heuristic was removed in favor of current-transaction checks and server timestamp assignment.

### Blocking gaps

- Material Follow-up uses the nonexistent transition-correlation field described above.
- Changed/reused correlation protection is local equality/freshness only; the correlation remains arbitrary caller data.
- Parent audit identity and initial creator identity are not bound to a trustworthy database session/command identity.
- A fabricated parent transition and fabricated matching history in the same transaction can pass the consistency checks.
- Several direct PostgreSQL tests mutate version/status without the newly required correlation/history and thus cannot reach their advertised guard.

## REV869B PostgreSQL test-method inventory

PostgreSQL tests were listed/compiled only and **NOT RUN**. Exactly 22 methods were discovered.

### `Rev869BPostgresApplicationBehaviorTests` (7)

1. `RealServiceTransactionPersistsParentChildHistoryAndAudit`
2. `RealServiceFailureAfterWritesRollsBackEveryAffectedRelation`
3. `RealServiceIdempotentReplayReturnsAuthoritativeOriginalWithoutDuplicates`
4. `RealProtectedServiceDenialHasNoBusinessMutationAndNoCrossOrganizationDisclosure`
5. `RealProtectedServicePropagatesAuditWriterFailureWithoutFalseSuccess`
6. `TwoIndependentDbContextsConnectionsAndServicesProduceOneAuthoritativeWinner`
7. `AuthenticatedMappedAspNetEndpointTraversesPermissionScopeServiceAndEf`

### `Rev869BPostgresBehaviorTests` (15)

1. `SuccessfulTransactionPersistsAndCanBeVerified`
2. `FailedTransactionRollsBackWithBeforeAfterEquality`
3. `TwoIndependentConnectionsHaveExactlyOneWinnerAndRejectStaleWriter`
4. `IdempotentReplayReturnsOriginalRowWithoutDuplicate`
5. `ConcurrentIdempotencyCollisionHasOneWinnerAndReturnsOriginal`
6. `DirectTerminalStateInsertIsRejected`
7. `SnapshotMismatchIsRejectedOnIssue`
8. `CommercialJsonTaxTotalsVersionOrganizationAndProvenanceTamperingAllReject`
9. `PermissionDenialPersistsAuditEvidence`
10. `AuditFailureCausesProtectedOperationToFailAndRollback`
11. `SkippedAndLowerVersionsAreRejected`
12. `DirectDatabaseRejectsLateChildInsertForEveryTerminalAggregate`
13. `ImmutableHistoryRelationsRejectUnauthorizedUpdateAndDelete`
14. `RejectedPoRevisionResubmissionAndRepeatedRevisionKeepExactAncestry`
15. `ExactRev869BTriggerAndFunctionInventoryOccursOnce`

## Fixture ownership matrix

| Test group | Fixture/database ownership | Determinism and cleanup | Verdict |
|---|---|---|---|
| Application PostgreSQL tests (7) | Creates a deterministic SHA-256 named disposable database from the explicitly accepted isolated template; checks exact absence before creation; creates identity mapping, warehouse, PR, PR line and handoff prerequisites. | Pooling is disabled; database drop uses quoted identifier and `WITH (FORCE)` and verifies absence. Cleanup is not protected by one outer `finally`: rollback-baseline verification or `Db.DisposeAsync` can throw before lease disposal. | PARTIAL/FAIL |
| Direct PostgreSQL tests (15) | Every method calls `OpenVerifiedAsync`, which calls `RequireExactOwnedFixtureAsync`. It only counts externally existing rows under `REV869B-PG-DIRECT-TEST-OWNED`; it creates no graph. | Requires exactly one RFQ, RFQ line, invitation, quotation, quotation line, technical verification, comparison, comparison line, PO, PO line, follow-up, status history, approval history and PO history. Cleanup is missing or incompatible with immutable-delete guards in committed cases. | FAIL |

The declared external-fixture blocker alone requires source-safety FAIL. All 15 direct methods can fail before their intended assertion when the exact graph is absent/ambiguous. If a statement's source predicate matches zero rows, the common guard helper rejects zero affected rows, but that does not repair fixture ownership or prove the intended trigger. Other methods use scalar/exact-count assumptions against mutable shared state and can consume or damage the external graph.

Additional direct-suite defects:

- Idempotent/collision/terminal copy-inserts change `IdempotencyKey` but leave the copied `TransitionCorrelationId`; the new initial guard rejects them before unique-key or terminal-transition evidence.
- RFQ version updates in rollback/stale-writer/audit-failure tests omit the new correlation and exact history.
- PO snapshot/tampering updates omit correlation/history and are intercepted before the intended commercial guard.
- Rejected-PO revision inserts change the idempotency key without the transition correlation, resubmission updates omit correlation/history, and line IDs use nondeterministic `gen_random_uuid()`.
- The committed collision winner is cleaned with direct `DELETE` from controlled RFQ, which the migration explicitly rejects, and the cleanup is outside a `finally`.
- Shared-fixture stale-writer mutation is not restored.
- The expected trigger inventory omits the newly installed `trg_rev869b_bound_technical_history`, so the exact-inventory test will fail against the generated 73-trigger inventory.
- Audit null-ID evidence expects `audit_logs_Id`, but the assertion helper does not include PostgreSQL `ColumnName`; PostgreSQL normally reports table `audit_logs` and column `Id`, so the advertised exact evidence is not established.

No `REV869B-PG-OWNED-DATABASE-GUARDS` source fixture was found in the reviewed paths. The material named dependency is `REV869B-PG-DIRECT-TEST-OWNED`. No randomized `GetHashCode()` was found for IDs; the direct revision test nevertheless retains `gen_random_uuid()`. Current/server timestamps are used in behavior being tested, so exact timestamp equality is not controlled; server timestamp ownership is desirable, but assertions must use bounded/event-order semantics.

## SQLSTATE and database-object assertion matrix

| Intended case | Declared SQLSTATE/object | Independent reachability result |
|---|---|---|
| Idempotency collision | `23505` / `IX_request_for_quotations_OrganizationId_IdempotencyKey` | FAIL: copied transition correlation does not equal the new key, so `23514` / `rev869b_initial_command_correlation` is reached first. |
| Terminal aggregate insert | `P0001` / `rev869b_enforce_transition` | FAIL: copied transition correlation mismatch is reached first. |
| PO issue snapshot tamper | `23514` / `rev869b_po_issue_allowlist` | Not uniformly proved; missing correlation/history or earlier allowlists intercept cases. |
| Version skip/lower | `40001` / `rev869b_exact_version_increment` | Structurally reachable where exact-version is the first check; external fixture still blocks independent evidence. |
| PO org/totals/policy tamper | `23514` / `rev869b_po_approval_allowlist` | FAIL as a matrix: correlation and other earlier guards can intercept combined statements. |
| Immutable JSON/tax/provenance update | `P0001` / `rev869b_reject_immutable_mutation` | Not independently isolated; statement ordering can reach explicit mutation guards first. |
| Controlled delete | `23514` / `rev869b_controlled_delete_guard` | Structurally reachable, but the collision test incorrectly attempts this forbidden delete as cleanup. |
| Late child insert | `P0001` / `rev869b_validate_child_insert` | Copied child version/state can be intercepted by explicit insertion contracts; exact intended guard is not proved for every row type. |
| Audit null ID | `23502` / `audit_logs_Id` | SQLSTATE is plausible; exact evidence string is inconsistent with the helper omitting `ColumnName`. |

The helper itself checks that a mutation cannot return zero rows as successful trigger evidence, which is an improvement. Its call sites still frequently exercise the wrong guard or depend on an external source row, so the exact evidence contract remains FAIL.

## Rollback design

The service failure test uses an independent context after the failed service transaction, which is the correct observation boundary. `CountOwnedAsync` was expanded to include all 15 REV869B relations, audit, number sequence, employee identity mapping, warehouse, PR, PR line and handoff.

It is still not an exact state proof: it sums counts rather than comparing a per-relation snapshot and cannot detect field changes with unchanged counts. More importantly, fixture creation adds five supporting rows (identity mapping, warehouse, PR, PR line, handoff). `RealProtectedServicePropagatesAuditWriterFailureWithoutFalseSuccess` asserts the independent total is zero, so it contradicts its own baseline and must fail. The first rollback test correctly compares the captured nonzero baseline.

`OwnedRfqFixture.DisposeAsync` can throw on baseline mismatch before `databaseLease.DisposeAsync`, and the nonambient branch can throw during Db disposal before lease cleanup. Database deletion is therefore not guaranteed in `finally`, contrary to the checkpoint. Partial parent/child/history/audit/idempotency/number-series/supporting state is not exhaustively compared field-for-field.

## Concurrency design

The application concurrency test genuinely creates two DbContexts, connections and service instances; uses the same organization/key; coordinates a start with `TaskCompletionSource`; awaits both without `Task.Delay`; verifies a same-ID replay; and separately checks conflicting payload rejection. These are real improvements.

It does not place a barrier after each transaction has independently reached the contested read/write boundary, so scheduling can serialize the service calls. Both returned results are accepted without proving exactly one committed attempt and one replay/loser. It checks only one RFQ and one RFQ line, not zero partial loser data across history, audit, number sequence, invitations, technical verification, comparison, PO, follow-up and supporting state. Cleanup has the lease-finally defect above. The direct two-connection tests additionally mutate an external shared graph and omit the new mandatory correlation/history data.

## Migration, model, snapshot, and Down review

- Migration ID remains exactly `20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation`.
- EF no-connect discovery lists exactly 13 migrations; REV869B occurs once immediately after `20260810120000_Rev869AIdentityMasterScopeFoundation`.
- No new migration was added.
- Domain, EF mapping, migration, designer, and model snapshot agree under the exact parity test.
- Five aggregate `TransitionCorrelationId` columns are non-null `varchar(200)` and receive lifecycle treatment in source. No dedicated aggregate transition-correlation index was added; history relations have parent/correlation unique indexes. This is not the primary safety blocker but should be justified against query plans before execution.
- Qualification verifier/approver fields are nullable UUIDs with indexes and Restrict FKs to employees.
- The new qualification columns preserve existing rows during Up, but no controlled population path exists; retained accepted rows become ineligible for invitation.
- Up/Down offline SQL generation is deterministic. Down removes owned triggers/functions before dropping qualification FKs/indexes/columns and REV869B tables. No other revision's table is dropped.
- Dropping the two REV869B-added qualification actor columns on Down necessarily discards values populated after Up. That is owned schema rollback, but retained business-history preservation cannot be accepted until the intended policy is explicit and runtime isolation validates it.
- Runtime trigger compilation/behavior and transaction atomicity were **NOT RUN**. The follow-up invalid field is a source-level reason not to authorize execution.

## Reproduced offline validation

| Validation | Result |
|---|---|
| Solution build, `--no-restore` | PASS: 0 warnings, 0 errors |
| Focused REV869B non-PostgreSQL tests | PASS: 49/49 |
| Complete non-PostgreSQL suite | PASS: 423/423 |
| REV869B PostgreSQL discovery | 22 methods compiled/listed; **NOT RUN** |
| PowerShell AST | PASS: 23 files, 0 errors; scripts not executed |
| EF migration discovery | PASS with `--no-connect`: exactly 13; REV869B exactly once after REV869A |
| Exact design-time model/snapshot parity | PASS: 1/1 |
| `git diff --check` on reviewed commit | PASS |
| Reviewed-diff secret scan | 0 private-key/AWS-key/password-pattern matches |
| Reviewed commit legacy path scan | 0 legacy-reference paths |

Offline Up SQL:

- Size: 175,904 bytes
- SHA-256: `16A90ED2FEF1933C3D9E62E1EB6676279625597044BD1D802D0E09CCF40765BB`

Offline Down SQL:

- Size: 7,161 bytes
- SHA-256: `5F869A3FE6024A468E0B1D58D836CA8A756497418B6115D4374FA6F6E779A883`

Independent Up SQL textual inventory:

- Tables: 15
- Trigger occurrences/unique: 73/73
- Function occurrences/unique: 16/15
- Foreign keys: 46
- Indexes: 68
- Checks: 31

These are offline generation/textual results only. They do not establish PostgreSQL runtime validity.

## Blocking findings

1. Installed follow-up deferred history logic references nonexistent `NEW."TransitionCorrelationId"` instead of `NEW."CorrelationId"`.
2. `VerifiedByEmployeeId` and `ApprovedByEmployeeId` are mandatory for invitations but have no controlled production write/approval path.
3. Qualification JSON origin can be fabricated by direct SQL copying a matching live qualification.
4. Correlation/history/SOD controls reconcile caller-supplied data but do not establish authoritative command/actor origin; creator employee and issuer separation are incomplete.
5. All 15 direct PostgreSQL tests depend on the external `REV869B-PG-DIRECT-TEST-OWNED` graph.
6. Direct test statements are incompatible with the new correlation/history guards, use nondeterministic IDs in revision lines, perform forbidden cleanup, and omit the new technical trigger from expected inventory.
7. Application audit-failure rollback expects zero despite a five-row fixture baseline; state verification is only an aggregate count.
8. Disposable database cleanup is not guaranteed through one outer `finally`.
9. Concurrency does not prove exactly one committed attempt or exhaustive zero partial loser state.
10. Mapped security coverage does not prove cross-org 404, scope-denial/audit, audit failure, or commercial masking through the real pipeline.

Any one of findings 1, 2, or 5 independently requires source-safety FAIL. Together they also require execution-helper readiness FAIL.

## Required eighth controlled source-only correction

1. Correct the follow-up deferred history predicate to use the actual correlation property and add source contracts that distinguish aggregate transition correlation from event correlation.
2. Add a controlled, segregated qualification verification/approval lifecycle that writes verifier/approver employee IDs, preserves existing data, records immutable history, and supplies a safe transition/backfill policy without creating another REV869B migration.
3. Bind command correlation and actor identity to a database-verifiable server-controlled command context/ledger so direct SQL cannot fabricate parent+history agreement; bind creator employee and PO issuer explicitly.
4. Make each of the 15 direct PostgreSQL methods construct its complete deterministic owned graph inside a disposable test-owned database, prove exact nonexistence, and clean the whole database in an unconditional outer `finally`.
5. Rewrite every direct mutation to satisfy prerequisite correlation/history contracts and isolate one intended failure; assert exact SQLSTATE plus constraint/trigger/function/column evidence and explicit affected-row behavior.
6. Remove `gen_random_uuid()`, forbidden row cleanup, shared state mutation and external named fixture dependencies; include the technical bound-history trigger in exact inventory.
7. Fix application rollback expected baselines, compare a per-relation and relevant-field snapshot through an independent context, and guarantee database lease disposal even if verification/disposal fails.
8. Strengthen concurrency coordination at the contested transaction boundary and prove one committed winner, correct replay/loser result, conflicting payload, and zero partial loser state across all affected relations.
9. Add mapped-pipeline cross-org nondisclosure, scope denial with audit, audit failure propagation, attachment/export record-scope denial, and commercial masking assertions.
10. Preserve the retained migration ID and all accepted REV868/REV868C3/REV869A behavior, then perform a new independent source-only safety re-review.

## Improvements retained

- Explicit mutation/delete/version controls are broader and easier to audit.
- Technical-verification and reservation histories are structurally bound.
- The five-second history heuristic was removed.
- Fail-closed commercial/qualification reconciliation is substantially more complete.
- Application PostgreSQL tests moved toward deterministic disposable-database ownership.
- Mapped endpoint coverage now traverses real routing/auth middleware for several response classes.
- Offline build, tests, model parity, SQL determinism and inventory remain reproducible.

## Exact next gate

Stop here. Do not provide or execute a PostgreSQL command, create an execution helper, or begin database execution. The next authorized activity must be an **eighth controlled source-only REV869B correction** based on the commit containing this single review report. It must modify only the minimum controlled REV869B source/tests and its correction checkpoint, retain the existing REV869B migration ID, resolve every blocking and required-correction item above without PostgreSQL access, and be followed by a new independent source-only safety re-review. No source-safety PASS or execution-helper readiness claim is permitted before that review reproduces exact evidence and finds no material blocker.
