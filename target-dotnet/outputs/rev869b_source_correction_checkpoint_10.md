# REV869B source correction checkpoint 10

Date: 2026-08-13
Scope: tenth controlled source-only REV869B correction
Starting commit: `433bcfc4f36cc882d5a93d578797a5546ca8e386`
Starting parent: `7da4f34fad6595194b50bf95e6c762fb1a509390`
Ending commit: the single correction commit containing this checkpoint; its exact hash is reported in the post-commit handoff because a commit cannot embed its own content-derived hash.

## Boundary and entry gate

- Before edits, HEAD and parent matched exactly, target-scoped status was clean, and HEAD contained only `outputs/rev869b_preapply_source_safety_rereview_after_correction_9.md`.
- Both authoritative reports were read completely. A complete B1-B10 pre-edit matrix was published before editing.
- Work stayed within `target-dotnet`. The sibling `../legacy-reference/` was not read or modified during correction work and no legacy path is included in the correction diff.
- No PostgreSQL server, database helper, migration apply/remove, backup, restore, production-like database, REV861, frontend, REV869C, or AWS operation occurred.
- The retained migration ID remains exactly `20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation`; no migration was added.

## Finding-to-correction matrix

| Finding | Authoritative defect | Correction | Offline acceptance evidence |
|---|---|---|---|
| B1 | Malformed policy-date check SQL | Closed the quoted `EffectiveFrom` identifier in model, retained migration, designer, and snapshot. | Four-location contract; generated Up contains the valid fragment and not the malformed fragment. |
| B2 | Caller-forgeable command identity | Replaced caller-agreement trust with an owner-provisioned per-database-principal 256-bit authority, external HMAC key, exact OIDC issuer/subject/employee/role/org binding, 30-second signed freshness, nonce uniqueness, backend/transaction identity, transaction-local selectors, explicit PUBLIC revokes and principal grants. | Build/contracts; production scan: zero embedded signing keys and zero persistent command-token setters. |
| B3 | Paired history claim collision | Claims now identify claim kind, exact history ID, entity/type/action/version/correlation, actor and transaction context; generic and specialized histories consume distinct slots. | Claim-slot contract checks exact identifiers, duplicate/replay failure, and distinct qualification/history calls. |
| B4 | Defective PostgreSQL error assertions | Custom guards emit structured SQLSTATE/object metadata; the central helper checks SQLSTATE and exact constraint or schema/table/column, and rejects zero-row mutations. | Compiled-only direct-test scan: one typed `PostgresException` capture, zero generic exception captures, structured-field assertions, zero-row guard. |
| B5 | Late-child source cardinality | Seven deterministic source IDs and target IDs select exactly one source; each attempt checks cardinality before mutation and a peer connection proves all attempted IDs absent. | Seven `child."Id"=@sourceId` occurrences plus exact-one and independent zero-final-state contracts. |
| B6 | Nested mapped qualification transaction | Mapped fixture uses no ambient transaction; each endpoint owns its transaction; committed lifecycle and rollback are verified through fresh independent contexts. | Seven application fixture creations specify `useAmbientTransaction: false`; mapped qualification and rollback verifier contracts. |
| B7 | Incomplete qualification lifecycle/history | Added explicit `Verified`; canonical create is Pending/Pending; lifecycle is Pending/Pending -> Verified/Pending -> Verified/Approved. Exact old/new JSON tuples, role, issuer/subject, correlation, DB transaction time and history claim are enforced. Retained actorless Draft rows have a one-time signed, authorized, audited, versioned Draft/Draft -> Pending/Pending normalization that adopts the signed actor as creator; all other legacy mutation remains closed. | Lifecycle and generated-SQL contracts include Create/Normalize/Verify/Approve, immutable history guard and exact server timestamp. |
| B8 | Narrow same-connection rollback proof | Direct rollback captures typed RFQ identity/org/number/status/version/correlation/line/history state and compares it through a peer connection. Additional PO, history, version and terminal rejection paths now use peer post-state evidence. | Compiled-only source contracts and build. |
| B9 | Raw ledger outside explicit parity/retention | Raw authority/context tables are explicitly inventoried as migration-owned security tables outside the 15 domain tables; both are PUBLIC-revoked. Contexts are immutable to ordinary SQL, transaction/backend bound and deleted after 15 minutes; authority provisioning/revocation is owner-only. | Generated inventory is 17 tables including both security tables; ownership/retention/grant contracts. |
| B10 | Outer cleanup retry suppressed | Disposal state is set only after verified drop; rollback, transaction disposal, context disposal and baseline verification are separately staged for retry. Added an explicit opt-in quarantine recovery method requiring exact high-entropy run/token/family/name/source/migration marker proof before DROP. | Cleanup contract checks stage flags, post-drop disposed assignment, proof-bound recovery and refusal text. Recovery was not executed. |

## Corrected migration-SQL contract

- Up/Down were generated offline in memory from REV869A to the retained REV869B migration and in reverse.
- Dollar quoting is paired: 50 `$rev869b$` delimiters and 2 `$rev869b_extension$` delimiters.
- Every created security/trigger function has an explicit `pg_catalog,nexa` search path; `public.hmac` is schema-qualified.
- `pgcrypto` capability is installed/checked before functions. Down removes provision/claim/validate/open functions before context/authority tables and deliberately retains the shared extension.
- Offline contracts scan exact corrected functions/triggers, generated fragments, trigger count, function count, reverse-safe ordering, table/column names, casts, error metadata, and valid policy-date text.
- EF SQL generation is not represented as PostgreSQL parser/runtime acceptance.

## Command-identity trust boundary

| Boundary | Authority and binding | Fail-closed property |
|---|---|---|
| Provisioning | Database owner provisions one active 32-byte secret for an existing login principal. | Non-owner, invalid key, missing login, duplicate active authority fail. |
| Application | Authenticated current user supplies exact employee, issuer, subject=login, role and organization; external environment supplies the key. | Missing/malformed key or identity throws before mutation; no source secret. |
| Signature | HMAC covers employee, issuer, subject, role, org, millisecond authentication time and random nonce. | Tamper, stale/future time and nonce replay use exact 42501 constraints. |
| Database identity | `session_user` selects the exact active authority; identity mapping and active approved role must each resolve once. | Direct SQL cannot create a valid signature or select a different principal. |
| Transaction | Context stores backend PID, `txid_current()`, principal and server issue time; selectors are transaction-local. | Commit/rollback clears selectors; another backend/transaction/org/entity cannot reuse them. |
| Claim | Claim includes kind/history/entity/action/parent version/from/to/correlation/remarks plus server actor/org/time/transaction facts. | Missing, stale, duplicate, ambiguous and cross-record claims fail closed. |
| Privileges/retention | PUBLIC has no table/function access; only the provisioned runtime principal receives three execute grants; contexts older than 15 minutes are purged. | No public/session-global bypass; ledger privacy lifetime is explicit. |

## History-claim uniqueness and pairing

| History family | Exact slot | Pairing rule |
|---|---|---|
| RFQ/invitation/quotation/comparison/PO status | history table name + history ID + parent tuple | Exactly one same-transaction candidate; generic and specialized slots differ. |
| Comparison approval | approval-history table + exact ID | Cannot collide with comparison status history. |
| PO history | PO-history table + exact ID | Cannot collide with generic PO status history; both legitimate claims remain independently single-use. |
| Technical verification | technical-history identity and parent correlation | Exact actor/line/version history required. |
| Qualification | `qualification_history` + controlled-history ID | Exact Create/Normalize/Verify/Approve tuple, role, signed subject, correlation, DB time and remarks required. |
| Material follow-up | follow-up history slot and exact parent | Same-status reservation binds its own history; late/replay/cross-parent claims fail. |

Zero candidates and multiple candidates both fail because required counts must equal one. A prior equal claim kind/history/entity/action/version/correlation is rejected. Histories retain immutable update/delete guards.

## Twenty-two PostgreSQL method evidence matrix  discovered, compiled, NOT RUN

No row below is runtime evidence. N/A means the method expects a success/application result rather than a PostgreSQL error.

| # | Method | Expected SQLSTATE / object when an error is intended | Target and independent evidence |
|---:|---|---|---|
| A1 | RealServiceTransactionPersistsParentChildHistoryAndAudit | N/A | Independent context proves RFQ, line, status history and audit. |
| A2 | RealServiceFailureAfterWritesRollsBackEveryAffectedRelation | N/A; exact `InjectedAuditFailure` | Typed full owned-state equality through independent contexts. |
| A3 | RealServiceIdempotentReplayReturnsAuthoritativeOriginalWithoutDuplicates | N/A | Independent exact counts for RFQ, line, history and audit. |
| A4 | RealProtectedServiceDenialHasNoBusinessMutationAndNoCrossOrganizationDisclosure | N/A; exact `UnauthorizedAccessException` | Independent before/after business fields and one durable denial audit. |
| A5 | RealProtectedServicePropagatesAuditWriterFailureWithoutFalseSuccess | N/A; exact `InjectedAuditFailure` | Independent typed full-state equality. |
| A6 | TwoIndependentDbContextsConnectionsAndServicesProduceOneAuthoritativeWinner | N/A; conflict path exact `Rev869BConflictException` | Two connections/services; third verifier proves one winner and full deltas. |
| A7 | AuthenticatedMappedAspNetEndpointTraversesPermissionScopeServiceAndEf | N/A; exact HTTP 400/403/404/409 and success outcomes | No ambient transaction; fresh contexts prove lifecycle commits and audit rollback. |
| D1 | SuccessfulTransactionPersistsAndCanBeVerified | N/A | Peer connection proves exact audit ID/correlation. |
| D2 | FailedTransactionRollsBackWithBeforeAfterEquality | N/A | Typed RFQ state compared through peer connection. |
| D3 | TwoIndependentConnectionsHaveExactlyOneWinnerAndRejectStaleWriter | Stale writer is exact zero-row optimistic conflict | Third connection proves winner version. |
| D4 | IdempotentReplayReturnsOriginalRowWithoutDuplicate | N/A | Exact affected row and same ID/count inside controlled transaction; rollback isolation. |
| D5 | ConcurrentIdempotencyCollisionHasOneWinnerAndReturnsOriginal | 23505 / `IX_request_for_quotations_OrganizationId_IdempotencyKey` | Peer proves one row and winner ID. |
| D6 | DirectTerminalStateInsertIsRejected | P0001 / `rev869b_enforce_transition` or `rev869b_enforce_quotation_transition` | Exact-one deterministic source; peer proves attempted IDs absent. |
| D7 | SnapshotMismatchIsRejectedOnIssue | 23514 / `rev869b_po_issue_allowlist` | Exact PO/version target; peer canonical PO+line state equality. |
| D8 | CommercialJsonTaxTotalsVersionOrganizationAndProvenanceTamperingAllReject | 40001 `rev869b_exact_version_increment`; 23514 `rev869b_po_approval_allowlist` or `rev869b_controlled_delete_guard`; P0001 `rev869b_reject_immutable_mutation` | Exact PO/line targets; peer canonical PO+line equality. |
| D9 | PermissionDenialPersistsAuditEvidence | 42501 / `rev869b_command_signature_invalid` | Invalid signed open is genuinely attempted; peer proves durable denial audit. |
| D10 | AuditFailureCausesProtectedOperationToFailAndRollback | 23502 / schema `nexa`, table `audit_logs`, column `Id` | Typed RFQ state equality through peer. |
| D11 | SkippedAndLowerVersionsAreRejected | 40001 / `rev869b_exact_version_increment` | Exact existing RFQ/version; peer typed state equality. |
| D12 | DirectDatabaseRejectsLateChildInsertForEveryTerminalAggregate | P0001 / `rev869b_guard_child_insert` | Exact-one source for seven relations; peer proves all attempted IDs absent. |
| D13 | ImmutableHistoryRelationsRejectUnauthorizedUpdateAndDelete | P0001 / `rev869b_reject_immutable_mutation`; 23514 / `rev869b_controlled_delete_guard` | Exact owned history predicates; peer canonical three-family history equality. |
| D14 | RejectedPoRevisionResubmissionAndRepeatedRevisionKeepExactAncestry | N/A | Exact affected counts, forced deferred constraints, commit, peer ancestry and two history-family counts. |
| D15 | ExactRev869BTriggerAndFunctionInventoryOccursOnce | N/A | Exact catalog name arrays and count-one assertions; gated owned database. |

The shared helper throws if a mutation returns zero before any `PostgresException`, checks exact SQLSTATE, and uses `ConstraintName` or the appropriate `SchemaName/TableName/ColumnName` fields. PostgreSQL tests were not executed.

## Late-child cardinality and lifecycle matrix

| Child | Editable parent rule | Corrected evidence |
|---|---|---|
| RFQ line | RFQ Draft/version 0 | deterministic `terminal-rfq-line`, exact-one source, P0001 child guard, peer absence |
| Invitation | RFQ non-terminal | deterministic `terminal-invitation`, exact-one, peer absence |
| Quotation line | quotation Draft | deterministic `quotation-line`, exact-one, peer absence |
| Technical verification | quotation Submitted and exact line | deterministic `technical`, exact-one, peer absence |
| Comparison line | comparison Draft/current sources | deterministic `comparison-line`, exact-one, peer absence |
| PO line | PO Draft/RevisionDraft current version | deterministic `po-line-rejected`, exact-one, peer absence |
| Material follow-up | current Issued PO | deterministic `terminal-followup`, exact-one, peer absence |

Parent transitions and deferred contracts retain zero/one/multiple child-count checks, organization/current-version filters, immutable historical children and revision-by-new-version behavior.

## Qualification transaction ownership

- Each create/normalize/verify/approve endpoint owns one transaction; the mapped fixture has no ambient transaction.
- Command context, qualification mutation, exact controlled history and audit participate before commit.
- Create: Pending Approval/Pending Approval version 0.
- Normalize retained actorless legacy: Draft/Draft -> Pending Approval/Pending Approval, adopts signed actor as creator, version +1.
- Verify: Pending/Pending -> Verified/Pending by an authorized non-creator employee.
- Approve: Verified/Pending -> Verified/Approved by an authorized employee distinct from creator and verifier.
- Early validation/authorization/conflict paths cannot commit partial business state; mapped audit-failure rollback is checked via a new independent context.

## Disposable database safety and quarantine

- Exact prefix plus a 96-bit hex name suffix derives from a 128-bit run ID; ownership uses a separate 256-bit marker token.
- Source and target database identity and the retained migration occurrence are checked before create/use/drop.
- The marker binds token, run, database, source, migration and fixture family. No name-only ownership is accepted.
- Pooling is disabled; connections are disposed/cleared before forced drop. Cleanup never deletes business rows or histories.
- Proof failure quarantines and refuses drop. Ordinary disposal is gated and retryable by rollback/dispose/verification stage.
- Hard-interruption recovery is an explicit opt-in method, not discovery behavior. It requires the exact run ID, ownership token, family and derived name, then independently rechecks current database, migration and durable marker before drop.
- Test discovery/listing does not instantiate the lease. No embedded key/password exists; each opted-in lease generates a random key and restores the previous environment value only after verified deletion.

## Rollback and concurrency

- Application rollback cases compare a complete typed owned-relation vector and fingerprint through independent contexts.
- Direct rollback compares typed RFQ state; PO tamper and immutable-history rejection compare peer canonical state.
- Service idempotency/concurrency uses endpoint/service-owned transactions and advisory serialization before replay/number allocation. Independent contexts prove one authoritative winner, one set of child/history/audit rows and exact unaffected-family counts.

## Offline validation results

| Validation | Result |
|---|---|
| Solution build | 0 warnings, 0 errors |
| Focused REV869B non-PostgreSQL tests | 57 passed, 0 failed |
| Complete non-PostgreSQL suite | 431 passed, 0 failed |
| PostgreSQL test discovery | Exactly 22 listed; NOT RUN |
| PowerShell AST | 23 files, 0 parse errors; scripts not executed |
| EF migration discovery | `--no-connect`; exactly 13; REV869B once immediately after REV869A |
| Model/designer/snapshot/migration parity contracts | Passed in offline suite |
| Generated SQL contracts and inventories | Passed in offline suite; regenerated independently below |
| Source scans | No production embedded signing key/password, persistent command token, malformed policy literal, generic direct exception capture or production database literal |
| Diff hygiene | `git diff --check -- .` passed before checkpoint |

The first two no-connect discovery attempts failed locally before EF initialized because required safe environment variable names were missing; the corrected invocation supplied an intentionally non-routable database and matching expected database and succeeded without connecting.

## Offline SQL fingerprints and inventory

- Up: 208,804 UTF-8 bytes; SHA-256 `E721B491D7C09C95C0848FCA530F87CF1E571A584826971D1C85B0D45AC4A91F`
- Down: 8,021 UTF-8 bytes; SHA-256 `C97ACCAD52F635B30E15E6DDEA77F53A339A484330B8D2020708CBBB20D6D077`
- Inventory: 17 created tables, 76 unique triggers, 24 function-definition occurrences representing 23 unique functions, 46 foreign keys, 69 indexes and 35 check occurrences.
- Tables: `commercial_comparison_lines`, `commercial_comparisons`, `material_followup_handoffs`, `purchase_order_history`, `purchase_order_lines`, `purchase_orders`, `purchase_transaction_approval_history`, `purchase_transaction_approval_policies`, `purchase_transaction_status_history`, `quotation_technical_verifications`, `request_for_quotation_lines`, `request_for_quotations`, `rev869b_command_authorities`, `rev869b_command_contexts`, `rfq_vendor_invitations`, `vendor_quotation_lines`, `vendor_quotations`.
- Functions: `rev869b_claim_command_context`, `rev869b_command_context_valid`, `rev869b_commercial_snapshot_reconciles`, `rev869b_enforce_quotation_transition`, `rev869b_enforce_transition`, `rev869b_guard_authoritative_transition`, `rev869b_guard_child_insert`, `rev869b_guard_controlled_snapshot`, `rev869b_guard_explicit_mutation`, `rev869b_guard_extended_immutability`, `rev869b_guard_history_insert`, `rev869b_guard_qualification_history_insert`, `rev869b_guard_qualification_lifecycle`, `rev869b_open_command_context`, `rev869b_provision_command_authority`, `rev869b_qualification_provenance_valid`, `rev869b_reject_controlled_delete`, `rev869b_reject_immutable_mutation`, `rev869b_reject_overlapping_approval_policy`, `rev869b_require_bound_history`, `rev869b_require_qualification_history`, `rev869b_validate_parent_contract`, `rev869b_write_policy_history`.

## Exact controlled files

1. `outputs/rev869b_source_correction_checkpoint_10.md`
2. `src/SESS.NexaERP.Api/Endpoints/Rev869AConfigurationEndpoints.cs`
3. `src/SESS.NexaERP.Domain/Masters/MasterSupport.cs`
4. `src/SESS.NexaERP.Domain/Masters/VendorQualification.cs`
5. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.Designer.cs`
6. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.cs`
7. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/NexaErpDbContextModelSnapshot.cs`
8. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs`
9. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BControlledMutationSql.cs`
10. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BDatabaseLifecycleSql.cs`
11. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BDatabaseSafetySql.cs`
12. `src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.Rev869B.cs`
13. `src/SESS.NexaERP.Infrastructure/Persistence/Rev869BCommandContextAuthorizer.cs`
14. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.RfqQuotation.cs`
15. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs`
16. `tests/SESS.NexaERP.Tests/Rev869BDatabaseSafetyContractTests.cs`
17. `tests/SESS.NexaERP.Tests/Rev869BOwnedPostgresDatabase.cs`
18. `tests/SESS.NexaERP.Tests/Rev869BPostgresApplicationBehaviorTests.cs`
19. `tests/SESS.NexaERP.Tests/Rev869BPostgresBehaviorTests.cs`
20. `tests/SESS.NexaERP.Tests/Rev869BPurchaseCorrectionTests.cs`
21. `tests/SESS.NexaERP.Tests/Rev869BTestDatabaseLease.cs`

## Remaining blockers and status limitation

- PostgreSQL syntax and runtime behavior have not been validated by a PostgreSQL parser/server. All 22 PostgreSQL methods are NOT RUN.
- Migration apply/remove, helper execution, disposable database creation and quarantine recovery are NOT RUN.
- Therefore PostgreSQL acceptance, migration acceptance, helper readiness, production readiness and final REV869B acceptance remain outside this checkpoint.
- This correction does not self-declare source-safety PASS or helper-readiness PASS. A new independent source-only safety re-review from the clean correction commit is mandatory.
