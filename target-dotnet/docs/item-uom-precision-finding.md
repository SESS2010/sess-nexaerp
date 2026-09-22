# Finding 5: item editing and UOM precision

## Decision

Item create/update accepts active UOMs with a measurement dimension and quantity
precision 0 through 6, matching the UOM master service and import adapter.
No existing UOM precision is changed. The conversion precision remains exactly 6.
The item eligibility guard itself performs no rounding. The operations listed
below use their own six-decimal rules rather than reading the master precision
to choose a rounding scale.

The rejected equality check confused the transaction scale with the master unit's
precision. MIR conversions round to six decimal places; estimated BOM unit values
use six decimal places; supplier invoices validate six-decimal quantities/rates;
GRN, ledger and custody quantity columns store a six-decimal scale. Those rules
remain intact. This patch does not add or change per-unit stock rounding.

Written authority: management approval MGMT-REV869A-UOM-20260810-001 explicitly
approved EA / Each / COUNT / precision 0 as canonical, recorded in
outputs/rev869a_isolated_execution_tooling_checkpoint.md and
outputs/rev869a_preapply_source_safety_review.md. Installation trial masters also
use precision 0 for count units and 3 for KG/MTR/LTR. The frozen schema guideline
contains no requirement that every UOM master have precision 6.

## Regression and limits

The one canonical routine purchase workflow uses real page permissions and the
seeded SESS-01 Technical Director assignment to create, GET and update an item
for every active UOM in its disposable database. It creates explicit boundary units through the real UOM API, checks all precisions
0 through 6 as well as every existing trial UOM, refuses -1 and 7, and proves
every master precision remains unchanged.
It uses the ordinary runtime PostgreSQL principal; no fixture permission grants
are added for this check. This covers the migrated seed plus trial UOM catalogue,
not the separate 1,388-item customer database.

Verification environment: Windows, PostgreSQL 17, fresh disposable advance_parser,
fresh role history, migration C# files explicitly normalized to LF in the isolated
checkout. This is not a pre-75 field-upgrade or CRLF migration witness. No live
migration, provisioning or item update was run.

Full Release verification: 843 passed, zero failed, 28.5366 measured minutes
(28 minutes 32 seconds wall time), with TRX. Source hash audit: zero changes
during the run. A separate compile ran concurrently; the measured run still
remained under 30 minutes. The isolated candidate excludes older pending
migration/ACL work and the subsequent category/scope/read-grant drafts.
Evidence: local-evidence/item-uom-verified/full-suite.trx and full-suite-timing.json.
