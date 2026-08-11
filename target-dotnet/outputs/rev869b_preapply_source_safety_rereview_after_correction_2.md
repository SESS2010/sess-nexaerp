# REV869B Independent Source-Safety Re-review After Correction 2

Date: 2026-08-11 (Asia/Calcutta)

## Review identity and boundary

- Reviewed correction commit: `46be5774f7d7dbc7583c3ef26b92d6fe2c1cadf8`
- Correction base: `192dd4b7a0cd6783172bd8b658f7a415f2e172aa`
- Exact reviewed diff: `192dd4b7a0cd6783172bd8b658f7a415f2e172aa..46be5774f7d7dbc7583c3ef26b92d6fe2c1cadf8`
- Review method: independent source, authorization, migration, generated-SQL, test-body, test-inventory, build, offline EF, parsing, and diff inspection. The correction checkpoint was read for scope but was not accepted as proof.
- PostgreSQL/database access, migration application/removal, execution-helper creation, backup/restore, production, REV861, REV869C, frontend, AWS, and legacy-reference operations were not performed.

The source-safety gate remains closed. Material database enforcement, numeric-scale, authorization-reachability, and executable behavior evidence defects remain.

## Entry verification

| Check | Result | Independent evidence |
|---|---|---|
| Exact current commit | PASS | `git rev-parse HEAD` returned `46be5774f7d7dbc7583c3ef26b92d6fe2c1cadf8`. |
| Exact correction scope | PASS | The reviewed range contains exactly 17 files: 15 modified and two added. |
| Target-scoped status before report | PASS | `git status --short -- .` returned no entries. |
| Legacy isolation | PASS | The reviewed range has zero paths under `../legacy-reference/`; that directory was not accessed. |
| Diff whitespace | PASS | `git diff --check` returned no errors. |

## Exact reviewed files

1. `outputs/rev869b_source_correction_checkpoint_2.md`
2. `src/SESS.NexaERP.Api/Endpoints/Rev869BPurchaseEndpoints.cs`
3. `src/SESS.NexaERP.Application/Authorization/IPagePermissionService.cs`
4. `src/SESS.NexaERP.Domain/Authorization/RolePagePermission.cs`
5. `src/SESS.NexaERP.Domain/Purchase/Rev869BPurchaseTransactions.cs`
6. `src/SESS.NexaERP.Infrastructure/Authorization/EfPagePermissionService.cs`
7. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.Designer.cs`
8. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.cs`
9. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/NexaErpDbContextModelSnapshot.cs`
10. `src/SESS.NexaERP.Infrastructure/Persistence/Rev869BSeedData.cs`
11. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.ComparisonPo.cs`
12. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.RfqQuotation.cs`
13. `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs`
14. `tests/SESS.NexaERP.Tests/Rev869BPostgresBehaviorTests.cs`
15. `tests/SESS.NexaERP.Tests/Rev869BPurchaseBehaviorTests.cs`
16. `tests/SESS.NexaERP.Tests/Rev869BPurchaseCorrectionTests.cs`
17. `tests/SESS.NexaERP.Tests/Rev869BPurchaseFoundationTests.cs`

## Finding disposition

| Finding | Result | Independent evidence |
|---|---|---|
| N-01 canonical comparison statuses | PASS | Domain transitions and migration check constraints retain only Draft, PendingApproval, Approved, Rejected, RevisionRequested, and Cancelled. No persisted Recommended state was introduced. |
| N-02 approval thresholds | PASS | The three seeded ranges remain continuous at six-decimal precision: 0..50000, 50000.000001..500000, and 500000.000001..999999999999999999.999999. Runtime requires exactly one organization/date/amount match and uses total payable value. |
| N-03 GST snapshots and authoritative commercial reconciliation | FAIL | GST resolution now uses quotation `ReceivedAt`, typed tax snapshots are stored, later stages reuse them, and in-memory pre-issue reconciliation is substantially stronger. However, no distinct header-discount input or stored header-discount component exists; `DiscountValue` is only an aggregate of line discounts. The database issue trigger does not validate the tax snapshot's rule identity, approval/effective dates, HSN/supply/component split, quotation/RFQ IDs, vendor-qualification JSON, attachment object key, quotation receipt time, or comparison approval time. It therefore cannot independently prove complete immutable commercial/GST provenance. |
| N-04 rejected initial PO recovery and repeated revision lifecycle | PASS for source structure | Recovery compares rather than increments the rejected predecessor version, inserts a linked RevisionDraft, supports RevisionDraft -> Resubmitted -> Approved/Rejected and repeated rejected revisions, requires remarks, retains history, and returns stable RevisionDraft/0 creation results on later replay. Runtime transaction behavior remains unproved under N-07/R-12. |
| N-05 awaited denial auditing and failure propagation | PASS for source structure | Attachment and export endpoints are mapped with exact download/export filters. Identity, page-permission, record-scope, missing-record, masking, attachment, and export audit calls are awaited. No catch swallows audit exceptions. One mapped attachment RequestDelegate test reaches the real endpoint-filter chain and obtains 403 after an audit call. Successful data access and audit/database rollback remain unproved under N-07/R-12. |
| N-06 database INSERT/UPDATE transition and snapshot enforcement | FAIL | INSERT triggers correctly limit RFQ to Draft, quotation to Submitted, comparison to Draft, and PO to Draft/controlled RevisionDraft, and parent organization/ancestry is guarded. But UPDATE rejects only `NEW.Version < OLD.Version`; it permits unchanged versions and arbitrary forward jumps, including status changes without the required exact increment. The issue trigger's JSON checks are incomplete as described under N-03, and several JSON fields can be absent without a positive equality proof. Direct SQL therefore can bypass the intended version protocol and can issue against snapshots whose complete GST/provenance semantics were never database-reconciled. |
| N-07 / R-12 executable behavior | FAIL | The new calculator/fingerprint cases execute pure domain helpers and the mapped attachment case executes a real filter chain. There is still no successful real service workflow, EF transaction commit, injected transaction failure/rollback, concurrent CAS race, concurrent idempotency race, repeated revision service workflow, or successful mapped data endpoint. The two PostgreSQL tests named for concurrency perform only scalar counts and create no concurrent commands. The rollback probe updates zero rows (`WHERE false`). |
| R-02 canonical organization-scoped idempotency and replay | FAIL | SHA-256 fingerprints bind organization, operation, key, sorted object properties, trimmed strings, and request objects containing expected versions; most replay results are now stable constants or history-derived results. Arrays retain caller order, however, so logically identical unordered RFQ/quotation line sets generate different fingerprints; CreateRFQ additionally compares sorted lines but first rejects the different fingerprint. No real service idempotency replay or concurrent collision is executed. Closure is not independently proven. |
| R-07 HTTP semantics and non-disclosure | PASS | Exception mapping retains 400/401/403/404/409 behavior. Organization is included in entity lookups and scoped absence is returned without a cross-organization existence probe. The mapped denial test confirms a real 403 filter path. |
| I-01 approval-policy overlap prevention | PASS for source structure | A BEFORE INSERT OR UPDATE trigger rejects active policies whose effective-date and amount ranges overlap within an organization. Runtime policy resolution still requires exactly one match. Live trigger behavior remains for the isolated database gate. |
| I-02 numeric safety | FAIL | Capacity overflow and negative-value checks are present and the exact maximum `999999999999999999.999999` is accepted. Inputs are not required to have scale <= 6. For example, a sub-sixth-decimal discount can remain in `Breakdown.DiscountValue` while taxable/payable values are rounded; PostgreSQL `numeric(24,6)` would round that stored component and break the immutable snapshot equality. Exact decimal(24,6) behavior is therefore not fail-closed before persistence. |
| I-03 Down ownership hardening | PASS for source structure | Down drops owned Up functions/triggers before dependent tables, installs ownership triggers over permission/page deletion, rejects generated seed deletion when `CreatedBy` is not `migration-rev869b`, and removes the temporary guards afterward. Manual Department Manager rows also include an owner predicate; retained altered rows prevent dependent page deletion rather than being silently removed. |
| M-01 PO submit/resubmit/approve/issue reachability | FAIL | Purchase Manager receives create/update/submit/resubmit and logical issue but no approve/reject/revision. Department Manager receives mapped manager-route approval, and TD/MD receive higher-route approval. However, `HasFullControl || switch` means Managing Director's PO row implicitly grants create, update, submit, resubmit, issue, and every other action despite the claimed separation. Service role checks later reject most operational writes, but the page permission itself is unintended and the required exact permission separation is false. `issue` is also a non-persisted conjunction of update and submit, not an independently grantable flag. |
| M-02 terminal-state insertion prevention | PASS for source structure | BEFORE INSERT OR UPDATE transition triggers reject terminal RFQ, quotation, comparison, and PO inserts. PO RevisionDraft additionally requires a matching rejected predecessor in the same organization/root/number/revision chain. Live enforcement remains unexecuted. |

Because N-03, N-06, N-07/R-12, R-02, I-02, and M-01 remain open, material closure is not established.

## Commercial calculation and decimal boundary review

| Vector | Observed source result | Result |
|---|---:|---|
| 2 x 50, no charges/discount/tax | taxable/payable 100 | PASS |
| 1 x 100, charges 5 + 5, discount 10, CGST/SGST 9% | taxable 100, payable 118 | PASS |
| Intra-state on taxable 100 | CGST 9, SGST 9, IGST 0 | PASS |
| Inter-state on taxable 100 | CGST 0, SGST 0, IGST 18 | PASS |
| Zero tax | all tax components zero | PASS |
| Reference 3 x 100, discount 10, charges 2/3/4/5, 9%+9%, cess 1%, round-off .005 | taxable 304; CGST 27.36; SGST 27.36; cess 3.04; payable 361.77 | PASS |
| 0.3333334 at rounding scale 6 | 0.333333 | PASS |
| Exact numeric(24,6) maximum | accepted for quantity 1 and zero additions | PASS |
| Maximum multiplied by two | rejected before persistence | PASS |
| Negative quantity/charge/discount | rejected | PASS |
| Discount greater than assessable plus charges | rejected | PASS |
| Stored/client component mismatch | typed in-memory reconciliation rejects | PASS |
| Input component with scale greater than six | accepted rather than rejected/normalized consistently | FAIL |
| Distinct line discount and header discount | no header-discount contract/component exists | FAIL |

Server calculation covers item base (`quantity * unit rate`), line discount, packing, freight, insurance, other charges, taxable value, CGST, SGST, IGST, cess, round-off, aggregate taxable/tax/payable values, and approval-policy value. It does not model a separately controlled header discount. The item base is recomputed but is not an independently stored breakdown field.

## PO permission reachability matrix

Every listed endpoint first requires authenticated employee/organization scope. Each data operation then performs organization/record-scope authorization in the endpoint or service. Denials await audit writes; audit failures propagate.

| Operation | Page / required action | Effective page grants | Service authorization and stage | Self-approval / segregation | Assessment |
|---|---|---|---|---|---|
| View PO | `purchase.po` / view | PM, DM, TD, MD, Stores Manager, Stores Executive, Accounts Head | Organization/current-version query plus record-scope check | Commercial values separately masked | Correct |
| Create PO | `purchase.po` / create | PM; also MD through full control | `CreatePurchaseOrderAsync` requires PM; approved comparison; scoped parent | Creator later cannot self-approve | MD page overgrant, service denies |
| Update/amend/revise rejected | `purchase.po` / update | PM; also MD through full control | Service requires PM; issued-current or rejected-predecessor lifecycle and scope checks | Later approval remains separate | MD page overgrant, service denies |
| Submit initial PO | `purchase.po` / submit | PM; also MD through full control | Service requires PM; Draft -> PendingApproval, exact service CAS | Submitter/creator cannot approve own PO | MD page overgrant, service denies |
| Resubmit rejected revision | `purchase.po` / submit endpoint; seed also sets resubmit | PM; also MD through full control | Service requires PM; RevisionDraft -> Resubmitted, exact service CAS | Approver mapping and creator check apply later | Reachable for PM; MD page overgrant |
| Issue PO | `purchase.po` / logical issue (`CanSubmit && CanUpdate`) | PM; also MD through full control | Service requires PM; Approved/current plus in-memory pre-issue snapshot check | Approval must already have been performed by another authorized employee | Not independently grantable; MD page overgrant |
| Approve/reject manager stage | `purchase.po` / approve or reject | mapped Department Manager; MD also has full control | Service requires one effective department mapping and accepts mapped DM or PM role; PM lacks endpoint grant | Creator login cannot equal approver login | Effective DM-only endpoint path; MD reaches filter but service rejects manager route unless role matches |
| Approve/reject TD stage | `purchase.po` / approve or reject | TD and MD through their rows/full control | Service requires exact TD role | Creator cannot self-approve | Correct TD path; MD reaches filter but service rejects TD route |
| Approve/reject MD stage | `purchase.po` / approve or reject | MD | Service requires exact MD role | Creator cannot self-approve | Correct |
| Cancel | `purchase.po` / cancel | PM, TD, MD | Service requires PM/TD/MD and legal status transition | Scoped authorization | Correct |
| Export | `purchase.po` / export | PM, TD, MD, Accounts Head | Organization/current-version query and record scope | Awaited Export audit | Correct |

No fixed REV869B role-page row is all-false. No employee-specific permission row was introduced. The changed migration/designer/snapshot agree that `CanIssue` is not mapped, and no schema column was introduced. The shared `EfPagePermissionService` maps the logical action to `CanSubmit && CanUpdate`, but `HasFullControl` overrides it and all other action flags.

No accepted REV868, REV868C3, or REV869A migration file is changed by the reviewed range. No prior permission row is deleted. The unintended new effective MD issue/submit permission arises from adding a new logical action to the existing full-control override, so regression safety for shared authorization is not fully established.

## Database transition, schema, and rollback assessment

- Exactly 13 migrations were discovered offline. REV869B occurs once and follows REV869A.
- The migration, designer, and snapshot compile and the focused executable model-differ test passes.
- The installed EF CLI accepts `--no-connect` for migration discovery but rejects it for `migrations has-pending-model-changes` as an unrecognized option. No fallback capable of contacting a database was run. Pending-model evidence is therefore the executable model-differ test, not a successful CLI pending-model command.
- REV869B-only offline Up SQL: 83,539 bytes; SHA-256 `EA2BE57D5088F45CD34597AB2E86B94BFBED6A5776DBB74390E5CECDC7FE137D`.
- REV869B-only offline Down SQL: 5,813 bytes; SHA-256 `91AAEE42BF2DFD6E975463AE94D9FB4D8D9E840879E7A8D144600F466A6D9C01`.
- Up inventory: 15 tables, 21 REV869B triggers, five REV869B functions, 44 foreign keys, 66 indexes, and 29 checks.
- Down inventory: 15 table drops plus two temporary ownership triggers and one temporary ownership function. No REV869A object name occurs in the generated Down SQL.
- Temporary SQL artifacts were removed after hashing.

Direct-SQL assessment:

| Required prevention | Source result |
|---|---|
| Insert terminal RFQ | Prevented: insert must be Draft. |
| Insert terminal quotation | Prevented: insert must be Submitted. |
| Insert terminal comparison | Prevented: insert must be Draft. |
| Insert approved/issued/rejected/cancelled PO | Prevented: insert must be Draft or controlled RevisionDraft. |
| Cross-organization parent substitution | Parent triggers and update immutability checks reject the inspected RFQ/quotation/comparison/PO chains. |
| Bypass version transitions | Not prevented: unchanged and arbitrarily increased versions are accepted. |
| Issue without complete reconciled snapshots | Not fully prevented: header sums and selected JSON fields are checked, but complete GST/provenance contents are not database-validated. |

## Test-count and test-strength reconciliation

- Starting commit discovered cases: 439.
- Ending commit discovered cases: 450.
- Net additions: 11.
- Starting complete non-PostgreSQL executed cases: 411.
- Ending complete non-PostgreSQL executed cases: 414.
- Focused ending REV869B non-PostgreSQL cases: 40.
- Current listed test names: 450 total, 48 containing REV869B, 38 containing PostgreSQL/Postgres, and 412 not containing those database markers. Execution reports 414 non-PostgreSQL cases because parameterized case expansion differs from listed-name counting.
- Added non-PostgreSQL cases: three.
- Added PostgreSQL cases: eight.
- Removed tests: none.
- Renamed tests: none.
- No assertion deletion or clear weakening was found. Trigger-count assertions now count 21 Up triggers plus two temporary Down guards; they remain structural rather than runtime trigger proof.

Added non-PostgreSQL cases:

1. `AuthoritativeCommercialPipelineExecutesRequiredVectors`
2. `CanonicalIdempotencyFingerprintBindsOrganizationOperationKeyPayloadAndVersions`
3. `MappedAttachmentEndpointExecutesPermissionDenialAndAwaitsAudit`

Added PostgreSQL cases, inspected but not executed:

1. `InsertTerminalStateIsRejected`
2. `InvalidUpdateTransitionIsRejectedByTrigger`
3. `SnapshotMismatchBlocksIssue`
4. `ImmutableHistoryRejectsMutation`
5. `TransactionFailureRollsBackAllRows`
6. `ConcurrentAggregateUpdatesHaveSingleWinner`
7. `ConcurrentIdempotencyHasSingleAuthoritativeResult`
8. `TriggerInventoryIsInstalledExactlyOnce`

Behavior classification:

- Real domain behavior: calculator, overflow, approval boundaries, status matrices, typed snapshot reconciliation, and fingerprint helper.
- Real mapped API behavior: one attachment permission-denial RequestDelegate/filter-chain case. It does not execute the data handler.
- Real service methods: existing fail-fast CreateRfq branches only; no successful database-backed service command.
- Real transactions/rollback: none in the non-PostgreSQL suite.
- Real concurrency: none.
- Real service idempotency replay: none.
- EF model parity: executable model-differ test without database access.
- Remaining focused tests are source/reflection/seed/mapping structural checks or pure domain tests.

## PostgreSQL test source-safety inspection

The eight tests are discoverable and can be excluded consistently by `FullyQualifiedName!~Postgres`. They are not ready for an isolated execution gate:

| Requirement | Result | Evidence |
|---|---|---|
| Exact isolated-database protection | FAIL | Any connection string in `REV869B_POSTGRES` is accepted. There is no database-name allowlist, expected-name comparison, host/port restriction, production denylist, or fixture marker check. |
| Fail-closed connection handling | FAIL | If the environment variable is missing/blank, each test simply returns and is reported as passed rather than skipped or failed. |
| Rollback/cleanup | PARTIAL | SQL mutations use transactions and disposal/explicit rollback, but the rollback probe updates zero rows and therefore proves no material rollback. No per-test fixture creation/cleanup is present. |
| Deterministic independence | FAIL | Several probes depend on pre-existing Draft/Approved/history rows selected with `LIMIT 1`; zero matching rows can turn mutation probes into no-ops. Tests share arbitrary existing fixture state. |
| Real transaction failure | FAIL | `TransactionFailureRollsBackAllRows` executes `UPDATE ... WHERE false`, causes no failure, changes no row, and verifies only that its marker is absent. |
| Real aggregate concurrency | FAIL | `ConcurrentAggregateUpdatesHaveSingleWinner` only counts rows with negative Version. It creates no competing connections or updates. |
| Real idempotency concurrency | FAIL | `ConcurrentIdempotencyHasSingleAuthoritativeResult` only checks for existing duplicates. It creates no simultaneous commands. |
| Exact trigger inventory | FAIL | The test asserts only count >= 21, despite its name claiming exactly once. |
| Protected database access | FAIL | The source contains no protection against a production/protected connection string. |

No PostgreSQL test was executed during this review.

## Regression and offline validation record

- Build: 0 warnings, 0 errors.
- Focused REV869B non-PostgreSQL: 40 passed, 0 failed, 0 skipped.
- Complete non-PostgreSQL: 414 passed, 0 failed, 0 skipped.
- PowerShell 5.1 AST parsing: 23 files, 0 errors; scripts were not executed.
- Secret-like additions: 0.
- Employee literal/PII-like additions: 0.
- A broad prohibited-term scan produced 16 textual matches; inspection found checkpoint boundary statements, migration ownership text, and ordinary `CreatedBy` initializers, not executable database/backup/production/AWS/legacy helper commands.
- No accepted REV868, REV868C3, or REV869A migration file changed.
- Full non-PostgreSQL tests provide broad regression evidence, but shared authorization regression remains open because Managing Director full control now authorizes the newly introduced logical issue action.

## Remaining blockers and exact next authorized gate

1. Enforce exact database version transitions, not merely nondecreasing versions.
2. Complete database-side issue reconciliation for immutable GST rule contents and full quotation/RFQ/vendor/qualification/attachment/approval provenance.
3. Model and reconcile a distinct header discount, or explicitly remove that requirement through an authoritative specification change.
4. Reject or consistently normalize every numeric input to decimal(24,6) scale before persistence and snapshot comparison.
5. Separate PO issue/submit permissions from Managing Director full control and make the intended grant independently expressible without unintended action reachability.
6. Add successful service, transaction failure/rollback, concurrent CAS, and concurrent idempotency behavior tests.
7. Replace the eight PostgreSQL placeholders with fail-closed isolated-database tests that validate the exact database name/fixture marker, materially mutate rollback fixtures, run genuine concurrent commands, require deterministic per-test data, and assert exact trigger inventory.

The exact next authorized gate is another controlled source-only REV869B correction addressing these blockers, followed by a new independent source-safety re-review. Execution-helper creation and any PostgreSQL execution remain blocked.

rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
