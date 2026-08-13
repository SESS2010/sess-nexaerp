# REV869B pre-apply source safety re-review after correction 10

Review date: 2026-08-13
Reviewed commit: `9f9ddbf82991d656d289ff87400bf4b779bd0fcb`
Reviewed parent: `433bcfc4f36cc882d5a93d578797a5546ca8e386`
Commit subject: `Correct REV869B source safety controls checkpoint 10`

rev869b_source_safety_state=FAIL

rev869b_execution_helper_readiness_state=FAIL

## 1. Scope, independence, and entry gate

This is an independent, source-only pre-apply safety re-review of the tenth controlled REV869B correction. No PostgreSQL server, database helper, migration apply/remove, backup/restore, production system, AWS resource, REV861/frontend surface, or REV869C surface was used. The excluded `../legacy-reference/` directory was not accessed.

The entry gate passed for the target directory:

- `HEAD` exactly matched `9f9ddbf82991d656d289ff87400bf4b779bd0fcb`.
- Its parent and subject exactly matched the required correction-10 gate.
- The correction commit contains exactly 21 permitted files, no legacy-reference path, and no new migration.
- The scoped target status was clean before this report. A repository-wide status exposes the excluded sibling as untracked; it was neither entered nor inspected.

The correction-10 checkpoint and correction-9 independent re-review were used as authoritative inputs, but every material claim was rechecked against source or reproduced offline evidence.

## 2. Executive verdict

Correction 10 materially improves static SQL generation, PostgreSQL assertion metadata, late-child test determinism, mapped transaction ownership, qualification endpoints, and cleanup retry state. It does not close the safety gate.

Decisive blockers:

1. The signed envelope authenticates a principal but not a specific entity, operation, version, transition, correlation, or history. A shared-key holder can author the parent/history and claim that caller-authored operation.
2. Rollback removes nonce/context consumption, leaving the signed envelope reusable within its validity window.
3. Qualification-history consumption uses a weaker, non-strict second lookup and can select ambiguously.
4. The lifecycle ends at `Verified/Approved`; service eligibility and database transition guards require `Approved/Approved`.
5. Technical-verification and PO-line child guards omit required current-revision/current-version predicates.
6. Rollback and denial fingerprints omit controlled qualification history and command authorization/claim ledgers.
7. A reusable HMAC secret is stored in a raw table; context retention is opportunistic and raw-ledger relational/privacy controls are absent.
8. The helper uses the database owner as application role, swallows one cleanup error, and emits no durable sanitized quarantine recovery evidence.

## 3. B1-B10 disposition

| Control | Verdict | Independent finding |
|---|---|---|
| B1 generated SQL/migration | PASS, source-only | In-memory Up/Down generation, delimiters, inventory, date constraint, and object order are coherent. No PostgreSQL parser/server acceptance is claimed. |
| B2 authorization boundary | FAIL | Signature binds principal/session only. Operation data is supplied later to `rev869b_claim_command_context`; direct SQL with the shared key can fabricate the paired mutation. Rollback restores nonce reuse. |
| B3 controlled-history claim | FAIL | Generic claims are more specific, but inherit B2. Qualification exact-count is followed by a weaker non-strict lookup. |
| B4 PostgreSQL assertions | FAIL | Typed exception/SQLSTATE/metadata assertions improved, but all 22 remain unexecuted and several lack complete independent or least-privilege proof. |
| B5 late-child enforcement | FAIL | Seven tests are deterministic with peer absence; two database guards omit current flags. |
| B6 mapped transactions | PASS, source-only | No ambient fixture transaction; endpoints own transaction, audit, commit, rollback, and fresh verification. |
| B7 qualification lifecycle | FAIL | Isolated lifecycle is reachable, but its final canonical tuple is rejected downstream. B2/B3 also remain. |
| B8 rollback/denial evidence | FAIL | Fingerprints omit controlled configuration histories and command ledgers; several proofs are narrow. |
| B9 ledger privacy/retention | FAIL | Plain reusable authority secret, opportunistic retention, no FKs/RLS/dedicated owner/purge/export protection. |
| B10 helper/quarantine | FAIL | Marker/naming/drop safety improved; owner-backed application role, swallowed cleanup, and unrecoverable interruption evidence remain. |

## 4. Generated SQL and migration evidence

In-memory migration SQL generation completed without opening a connection:

| Artifact | Size | SHA-256 |
|---|---:|---|
| Up SQL | 208,804 bytes | `E721B491D7C09C95C0848FCA530F87CF1E571A584826971D1C85B0D45AC4A91F` |
| Down SQL | 8,021 bytes | `C97ACCAD52F635B30E15E6DDEA77F53A339A484330B8D2020708CBBB20D6D077` |

Static inventory:

- 17 revision tables, 76 triggers, 24 function definitions representing 23 unique names, 46 foreign keys, 69 indexes, and 35 checks.
- 50 revision dollar delimiters and two extension delimiters were balanced.
- The valid date fragment was present; the previously malformed fragment was absent.
- Up installs database safety, lifecycle, command context, then controlled mutation. Down removes them in reverse.
- Down emits function removal before dependent table removal.
- `pgcrypto` is created if absent and intentionally retained as a potentially shared extension.
- Current design-time model and snapshot comparison passed without connecting.

The 17 revision domain tables were present. Two raw security tables (`rev869b_command_authorities` and `rev869b_command_contexts`) are SQL-managed rather than EF-modeled, making their separate ownership, retention, and privacy review mandatory.

Source-only generation cannot establish PostgreSQL parsing, catalog ownership, trigger execution, transaction behavior, or actual privilege enforcement. B1 is only a static pass.

## 5. Command authority and claim review

### 5.1 Signed envelope coverage

| Field/class | Signature-bound | Open validated | Operation-bound |
|---|---:|---:|---:|
| Employee ID | Yes | Yes | Context only |
| Issuer/subject/role/organization | Yes | Yes | Context only |
| Authenticated time | Yes | Yes | Context only |
| Nonce | Yes | Yes if committed | No operation specificity |
| Entity type/ID | No | No | Caller supplies later |
| Operation/action | No | No | Caller supplies later |
| Version/from-to state | No | No | Caller supplies later |
| Correlation/history | No | No | Caller supplies later |
| Remarks/business tuple | No | No | Caller supplies later |

`Rev869BCommandContextAuthorizer` signs only the principal/session envelope. The database open function validates it, while `rev869b_claim_command_context` later accepts business fields after caller-authored writes exist. The runtime principal is granted that claim function. The claim proves consistency with a context created by that caller, not preauthorization by a distinct trusted authority.

Context/nonce insertion and mutation share a transaction. Rollback removes both, so the exact signed envelope can be presented again within its approximately 30-second validity. This violates the no-reusable-authorization-window requirement.

### 5.2 Claim specificity

| Claim family | Improvement | Remaining defect |
|---|---|---|
| Generic status/approval/PO history | Claim kind includes history table/ID; entity/action/version/correlation are compared; committed consumption is atomic. | Caller-fabricable context; rollback restores the opportunity. |
| Qualification history | Exact-history count precedes claim. | Subsequent non-strict lookup omits organization, actor, role, server time, and full state tuple; a second non-exact row can make selection ambiguous. |
| Parent pairing | Triggers validate transaction/context/history pairing. | Both sides can be authored by the shared-key holder; no operation-specific external grant exists. |

This is a trust-boundary failure, not merely missing test coverage. PostgreSQL execution cannot make a principal-only signature operation-specific.

## 6. PostgreSQL method matrix (enumerated, not executed)

All 22 PostgreSQL methods were discovered by test enumeration. None was run.

| ID | Method | Intended evidence | Independent source result |
|---|---|---|---|
| A1 | `RealServiceTransactionPersistsParentChildHistoryAndAudit` | committed application graph | Statically credible; runtime unproved. |
| A2 | `RealServiceFailureAfterWritesRollsBackEveryAffectedRelation` | full application rollback | Fingerprint omits controlled histories and command ledgers. |
| A3 | `RealServiceIdempotentReplayReturnsAuthoritativeOriginalWithoutDuplicates` | replay identity | Improved; runtime and independent post-state unproved. |
| A4 | `RealProtectedServiceDenialHasNoBusinessMutationAndNoCrossOrganizationDisclosure` | denial/no disclosure | Comparison is narrow and omits number sequence/all-family invariance. |
| A5 | `RealProtectedServicePropagatesAuditWriterFailureWithoutFalseSuccess` | audit rollback | Fresh verification exists; fingerprint exclusions remain. |
| A6 | `TwoIndependentDbContextsConnectionsAndServicesProduceOneAuthoritativeWinner` | concurrency winner | Two contexts/connections present; runtime unproved. |
| A7 | `AuthenticatedMappedAspNetEndpointTraversesPermissionScopeServiceAndEf` | mapped chain | Transaction nesting fixed; owner role defeats least-privilege realism. |
| D1 | `SuccessfulTransactionPersistsAndCanBeVerified` | committed direct graph | Independent verification present; runtime unproved. |
| D2 | `FailedTransactionRollsBackWithBeforeAfterEquality` | rollback equality | Typed RFQ state is too narrow for full graph/ledgers. |
| D3 | `TwoIndependentConnectionsHaveExactlyOneWinnerAndRejectStaleWriter` | concurrency/stale writer | Stale writer expects zero rows rather than a structured database guard. |
| D4 | `IdempotentReplayReturnsOriginalRowWithoutDuplicate` | identity/no duplicate | Path rolls back and lacks independent peer post-rollback proof. |
| D5 | `ConcurrentIdempotencyCollisionHasOneWinnerAndReturnsOriginal` | unique guard | Exact `23505` and constraint asserted; runtime unproved. |
| D6 | `DirectTerminalStateInsertIsRejected` | transition guard | `P0001` plus constraint asserted; runtime unproved. |
| D7 | `SnapshotMismatchIsRejectedOnIssue` | snapshot allowlist | `23514` plus constraint asserted; runtime unproved. |
| D8 | `CommercialJsonTaxTotalsVersionOrganizationAndProvenanceTamperingAllReject` | tamper matrix | Structured version/allowlist/immutability checks improved; unexecuted. |
| D9 | `PermissionDenialPersistsAuditEvidence` | privilege denial/audit | Bad signature under owner-backed role is not a least-privilege boundary. |
| D10 | `AuditFailureCausesProtectedOperationToFailAndRollback` | native audit failure | Exact `23502`, schema/table/column asserted; fingerprint incomplete. |
| D11 | `SkippedAndLowerVersionsAreRejected` | exact version guard | Exact `40001` asserted; runtime unproved. |
| D12 | `DirectDatabaseRejectsLateChildInsertForEveryTerminalAggregate` | seven child guards | Deterministic cardinality/peer absence improved; two predicates incomplete. |
| D13 | `ImmutableHistoryRelationsRejectUnauthorizedUpdateAndDelete` | immutable history | Structured `P0001`/`23514` improved; owner role non-representative. |
| D14 | `RejectedPoRevisionResubmissionAndRepeatedRevisionKeepExactAncestry` | PO ancestry | Source assertions improved; runtime unproved. |
| D15 | `ExactRev869BTriggerAndFunctionInventoryOccursOnce` | catalog inventory | Exact inventory encoded; actual catalog unproved. |

The central assertion helper now requires typed `PostgresException`, exact SQLSTATE, and exact constraint or native schema/table/column metadata. Zero-row mutation cannot masquerade as a database exception. B4 still fails because the helper runs as database owner, D3 uses zero-row conflict behavior, D4 lacks independent final-state evidence, D9 does not test a least-privilege boundary, and none ran.

## 7. Late-child matrix

| Child | Intended editable-parent rule | Result |
|---|---|---|
| RFQ line | Draft/version 0 | Deterministic source and peer absence; guard appears complete. |
| Invitation | RFQ non-terminal | Deterministic source and peer absence; guard appears complete. |
| Quotation line | quotation Draft | Deterministic source and peer absence; guard appears complete. |
| Technical verification | Submitted, current revision, exact line | Guard omits `IsCurrentRevision`. FAIL. |
| Comparison line | comparison Draft/current sources | Deterministic source and peer absence; guard appears complete. |
| PO line | Draft/RevisionDraft, current version | Guard omits `IsCurrentVersion`. FAIL. |
| Material follow-up | current Issued PO | Deterministic source and peer absence; guard appears complete. |

D12 now requires exactly one deterministic source for each relation and a peer connection proves all attempted child IDs absent. That fixes test cardinality ambiguity but not the two database predicates.

## 8. Qualification lifecycle and reachability

| Operation | Before | After | Control |
|---|---|---|---|
| Create | none | Pending Approval/Pending Approval v0 | signed creator; history/audit |
| Normalize legacy | Draft/Draft | Pending Approval/Pending Approval | explicit, actorless retained shape only |
| Verify | Pending/Pending | Verified/Pending | verifier distinct from creator |
| Approve | Verified/Pending | Verified/Approved | approver distinct from creator/verifier |

Mapped setup uses `useAmbientTransaction: false`; endpoints own command context, mutation, history, audit, commit, and rollback. This supports B6.

End-to-end reachability fails. `EfRev869BPurchaseService.RfqQuotation`, `EfRev869AFoundationServices.IsEligibleAsync`, and both relevant `rev869b_guard_authoritative_transition` branches require `Approved/Approved`. Only `rev869b_qualification_provenance_valid` accepts verification `Verified` or `Approved`. A new canonical `Verified/Approved` qualification cannot authorize the RFQ invitation/comparison/procurement chain, while legacy `Approved/Approved` can. B7 and workflow section J fail.

## 9. Rollback and denial evidence

| Surface | Covered | Material omissions |
|---|---|---|
| Application `OwnedState` | Main business/support counts and fingerprint | `controlled_configuration_histories`, command contexts, authorities, and claim state |
| A2 failure-after-writes | Business before/after | Authorization/history ledgers; nonce replay remains possible because context rolls back |
| A4 denial | RFQs, lines, status history, audits | Number sequence, qualification families, all protected aggregates |
| A5/D10 audit failure | Failure propagation and selected state | Full ledger/fingerprint coverage |
| D2 rollback | Parent fields and selected counts | Complete graph, qualification history, authorization/claim ledgers |
| D4 idempotency | In-transaction assertions | Independent peer verification after rollback |

Rollback must prove both no durable partial mutation and no reusable/misleading authorization window. Evidence does not fully prove the first; transaction-coupled nonce consumption demonstrably violates the second.

## 10. Raw security-ledger analysis

| Concern | Authorities | Contexts |
|---|---|---|
| Sensitive content | Reusable 32-byte HMAC `Secret` in `bytea` | Employee, issuer, subject, role, organization, nonce, claims, correlation, remarks |
| Retention | No expiry/purge for revoked or old secrets | 15-minute deletion only when a future context opens; idle rows persist |
| Relational binding | No principal/employee/organization FK | No principal/employee/organization FK |
| Isolation | No RLS | No RLS |
| Ownership | Migration/database owner | Migration/database owner |
| PUBLIC | Revoked | Revoked |
| Down | Revision table dropped | Revision table dropped |

Revoking PUBLIC is useful but insufficient. No dedicated no-login security-definer owner is assigned, and the helper application connection is the database owner. Backups capture the plaintext authority secret and context PII/claims. No source establishes protected export, secret rotation/purge, deterministic retention scheduling, or subject/organization deletion semantics.

## 11. Helper, cleanup, and quarantine

Positive controls:

- Names include family/run/token entropy and are bounded; pooling is disabled.
- Destructive cleanup requires exact name and exact marker verification.
- Cleanup drops the whole isolated database, never rows.
- Recovery rechecks the marker and fails closed.
- Disposal tracks rollback, transaction, context, baseline, and lease stages without falsely marking a failed lease complete.

Blockers:

1. The application role is `current_user`, also the cloned database owner. Owner privileges can bypass intended revoke/trigger boundaries and invalidate least-privilege evidence.
2. Create contains `try { await lease.DisposeAsync(); } catch { }`; cleanup failure can be silently lost.
3. Recovery requires family, run ID, and token. After hard interruption they exist only in memory and the orphan marker; no durable sanitized bounded record makes recovery operationally reachable.
4. Failed lease disposal is retryable only if the caller invokes disposal again; there is no automatic guaranteed retry.
5. Creation verifies source identity. Recovery validates configured source name and target marker but does not independently reconnect to revalidate a source before destructive recovery.

These fail execution-helper readiness despite the absence of broad or markerless cleanup.

## 12. Workflow, migration, preservation, and security regression

- No REV861/frontend or REV869C file changed.
- No migration was added; identity and designer/snapshot remain aligned.
- Generated Up/Down ordering is coherent source-side; Down is revision-scoped and retains the potentially shared extension.
- No hard-coded password, private key, or AWS credential was found. The intentional reusable authority-secret store remains a blocker.
- The canonical qualification workflow is internally reachable but incompatible with downstream predicates.
- Runtime privilege separation is not exercised because the helper application principal is database owner.
- No claim is made about production compatibility, PostgreSQL execution, catalog state, locks, trigger behavior, or real-server reversibility.

## 13. Reproduced permitted validation

| Validation | Result |
|---|---|
| `dotnet build SESS.NexaERP.slnx --no-restore` | PASS, 0 warnings/errors |
| Focused REV869B non-PostgreSQL tests | PASS, 57/57 |
| Complete non-PostgreSQL tests | PASS, 431/431 |
| PostgreSQL enumeration | PASS, exactly 22 discovered; NOT RUN |
| PowerShell AST | PASS, 23 files, 0 parse errors |
| EF migration list, non-routable settings | PASS, 13; REV869A then REV869B |
| Design model/snapshot exact comparison | PASS, 1/1, no connection |
| In-memory Up/Down SQL/inventory | PASS as static generation; hashes above |
| Correction range/path | PASS, 21 files, no excluded path/new migration |
| `git diff --check` on correction range | PASS |
| Secret-pattern review | No literal password/private/AWS key; raw reusable secret store found |

Compilation and non-PostgreSQL tests establish source consistency only; they do not neutralize demonstrated design defects.

## 14. Required next controlled correction

The next source-only correction must:

1. Replace the principal-only shared-key envelope with operation-specific one-time authorization binding organization, entity/type/ID, action/transition, expected version, correlation/history, and expiry before mutation. Rollback must not restore replayability. Do not store reusable plaintext signing secrets in a business table.
2. Establish real role separation: no-login function owner, least-privilege runtime, distinct migration/database owner. Future PostgreSQL guards must run as runtime.
3. Make qualification history selection exact, strict, complete-tuple, and single-consumer; reject any ambiguous additional history.
4. Reconcile every service/database qualification predicate with canonical `Verified/Approved` and add a mapped qualification-through-procurement source contract.
5. Add `IsCurrentRevision` and `IsCurrentVersion` to technical-verification and PO-line guards with structured assertions.
6. Expand rollback/denial fingerprints to controlled histories, contexts/claims/authorities, number sequences, qualification families, and every mutated aggregate, using independent readers.
7. Add deterministic purge/retention, relational subject/organization binding, least-privilege ownership, privacy/export/backup handling, and secret rotation/removal.
8. Remove swallowed cleanup failures and create a durable sanitized quarantine record with only exact recovery identifiers, bounded retention, and removal after successful cleanup.
9. Complete the 22-method matrix so every failure proves exact PostgreSQL metadata and every rollback/idempotent/denial case proves complete independent state.

Until another independent source-only re-review passes, no PostgreSQL command/helper execution is authorized by this review.

## 15. Final canonical determination

rev869b_source_safety_state=FAIL

rev869b_execution_helper_readiness_state=FAIL

The source is not safe to advance to PostgreSQL execution. This failure rests on concrete source behavior, not merely absence of runtime execution.
