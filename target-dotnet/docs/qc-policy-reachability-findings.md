# QC policy and serialized-disposition correction

Status: QC defect corrections verified in an isolated candidate; ERP-wide reachability remains open.

## Written authority

The frozen schema guideline, decision B3-3, says QC_MANAGER prepares and
TECHNICAL_DIRECTOR approves. The role catalogue gives the same responsibility.
Missing policy continues to fail closed to QC_HOLD.

## Findings

The API created Pending Approval policies without a decision route. The shared
rev869a_guard_controlled_version trigger also prohibited approval-status changes.
The inspection resolver required uppercase APPROVED while the master constant
is Approved. The routine purchase witness inserted an already-approved policy
directly, bypassing the missing user path.

An all-discrepancy serialized inspection supplies no serial dispositions.
BuildQcLegs treated this as a non-serialized lot and called sources.Single().
A two-serial receipt has two source movements, so that branch raised an exception.
The inspected allocation also disappeared from the queue, and inspection detail
returned only dispositioned serials: an all-discrepancy result lost the API inputs
needed for correction.

## Backend behavior

- QC Manager creates the policy using the existing configuration POST.
- TD decides through POST /api/v1/rev869a/configuration/qc-inspection-policies/{policyId}/approve
  or /reject, with Remarks, Version and an Idempotency-Key header.
- GET /api/v1/rev869a/configuration/qc-inspection-policies?effectiveOnly=false
  exposes pending policy IDs, approval status, version, applicability, activity
  and effective dates to the decision actor.
- Preparation and decision history are retained. Decisions require an independent
  preparer, a current version and an ordinary registered runtime command.
- Migration QcPolicyDecisions replaces only the QC policy trigger with a
  decision-aware guard. Measurement criteria remain immutable. Other configuration
  guards and the original close-only semantics are unchanged. A deferred constraint
  requires matching append-only decision history in the same transaction.
- Canonical Approved and retained legacy APPROVED policies both resolve.
- All-discrepancy inspection creates no stock-disposition movement. Units remain
  in QC_HOLD. Correction resolves serials using their individual original provenance.
- The QC queue retains unresolved discrepancy allocations and supplies
  InspectionNumber, CurrentRevisionId and DiscrepancyPendingQuantity.
  Inspection detail supplies InventorySerialIds for the original receipt even
  when SerialDispositions is empty. QC does not need inventory.grn permission.

## Routine proof

The one canonical purchase witness prepares and approves its policy over HTTP,
with real page permissions and operational scopes and the non-owner runtime
database principal. It no longer injects an additional scope for the QC employee.
The witness adds a two-serial receipt to its existing disposable database:
finalize all discrepancy, replay, recover identifiers through GET, correct to
one accepted and one rejected serial, reject a concession, approve a replacement
concession, then reverse it before use. Original revisions and per-serial stock
balances are checked.

The route gate discovers every registered QC write route and requires a
successful HTTP witness by a seeded employee with a seeded role assignment.
It has no fixed list that can silently omit a newly added QC write route.
The pending-policy and discrepancy state exits are exercised, not inferred
from the presence of a handler. Policy and correction inputs come from reads
available to their actual actors.


Transitions exercised in the routine witness:

| Record and starting state | Successful exit | Actor |
| --- | --- | --- |
| QC policy: Pending Approval | Approved, or Rejected and inactive | Independent Technical Director |
| Inspection: finalized with all two serials discrepancy-pending | Correction with one accepted and one rejected serial | QC Manager |
| Concession: DRAFT | APPROVED or REJECTED | Technical Director |
| Concession: APPROVED, before use | Appended REVERSED decision and restored stock balances | Technical Director |

Before policy approval, an attempted stock disposition is refused and the
QC_HOLD balance stays unchanged. Historical inspection revisions are retained;
the correction does not rewrite the all-discrepancy result.

This proves the scoped QC path. It is not the requested ERP-wide
operation/state/master reachability inventory. That broader coverage remains open;
the known UOM/category seed issues are not waived by this QC gate.


The master-data gap is concrete: the canonical witness loads
database/postgresql/trial-master-data-apply.sql. No test currently references
database/postgresql/legacy-item-import-2026-08-29.sql, which contains 1,368 item
INSERT statements. That file alone must not be equated with the reported
1,388-record field catalogue. Broader acceptance must reconcile the baseline
plus imports with the actual catalogue, exercise its consuming operations, and
fail on unsupported records rather than granting fixture-only permissions or
changing their precision to make a witness pass.


The QC HTTP host uses the real permission and operational-scope services.
Other parts of the pre-existing purchase fixture still use test authorities;
they are not counted as QC route evidence and do not prove ERP-wide reachability.

The explicitly selected historical FIFO upgrade witness starts before the
QcPolicyDecisions migration. It retains its historical approved-policy fixture,
and asserts that the new migration is absent before inserting it. This fixture
is not an approval-path witness. All routine QC execution uses the API ceremony.

## Environment and limitations

Windows, PostgreSQL 17, fresh disposable database advance_parser, freshly
provisioned roles. The first isolated candidate failed before QC at migration 89
because its checked-out sources still had CRLF despite the checkout command's
core.autocrlf setting. Migration sources were then explicitly normalized to LF in
the disposable checkout for QC verification. Those existing migration changes are
LF/UTF-8 test preparation and are not part of this QC patch.

This is not a CRLF deployment proof or a witness of the private pre-75 field
backup and its historical REV869B roles. The older owner-ACL and migration
line-ending corrections remain separate pending work. No live database migration
or provisioning was performed.

## Verification results

Consolidated QC target: 3 passed, zero failed; the canonical workflow took
5 minutes. All nine registered QC write routes were reached.
First isolated full Release run: 843 passed, zero failed, 24.7361 measured minutes;
source hash audit found zero changes during that run.
Historical FIFO witness after its setup adjustment: 1 passed, zero failed,
4 minutes 12 seconds. Final routine rebuild passed with zero warnings/errors.
Final isolated full Release run: **843 passed, zero failed**, **24.4856 measured
minutes** (24 minutes 29 seconds wall time; test duration 24 minutes 24 seconds).
The final source-hash audit found zero changes during the run.

Evidence is retained in local-evidence/qc-reachability/candidate-verified/:
full-suite.trx, full-release-timing.json, final-source-hashes.json,
historical-fifo.trx and qc-operation-witnesses.json.

The 843-test count belongs to the isolated QC candidate at base 610894e. It
excludes the older uncommitted migration/owner-ACL tests. The serialized case is
part of the one canonical end-to-end test, not a second routine workflow fixture.
