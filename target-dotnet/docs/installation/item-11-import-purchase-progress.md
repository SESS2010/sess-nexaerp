# Item 11 — import purchase progress

Status: partial. Accounts' actual banker INR amount for each payment is authoritative;
the ERP must not calculate currency conversions or maintain a rate table. Wire,
commission and advice charges belong in item cost; customs duty and clearing are
separate GRN-date adjustments.

## Bank-advice reference refusal

The existing advance command already requires a nonblank evidence reference.
The bill-payment command did not. The new migration and API validation require
a bank-advice evidence reference when payment currency, after trimming and
uppercasing, is not INR. The existing payment function signature, DTO fields,
routes and response envelopes stay the same. The frontend must show the new
HTTP 400 validation response for a foreign payment with an empty reference.
Database callers receive P0001 after the existing Accounts authority check.

This verifies the presence of an evidence reference, not the existence or
contents of a bank document in external storage. It does not claim that the
remaining import payment or costing requirements are implemented.

The migration preserves the sorted bill lock order, function owner, fixed
search path and restricted runtime permissions. Up and Down check the provider,
PostgreSQL version, protected database names and exact prior function body.
The payment integration witness now rolls back and reapplies the intervening
migrations in order instead of bypassing later versions of the same function.

Baseline Release build: zero warnings/errors, 4m18.94s; corrected test fixture
build: zero warnings/errors, 28.24s. Baseline test: 0 passed, 1 failed, 0 skipped,
3m13s. Against the domestic witness's INR bills, a USD request with missing
advice reached the currency-mismatch refusal (HTTP 409 and SQL P0001), instead
of the required advice refusal. This is evidence of the missing validation
branch; it is not a successful foreign-payment posting witness.

Before/after baseline counts were identical: 0 payments, 0 allocations,
74 requests, 74 receipts, 111 audits. Retained evidence:
`local-evidence/item11/bank-advice-baseline-release.json` and
`item11-advice-baseline-release.trx`.

Post-change Release build passed with zero warnings/errors in 3m59.09s.
The targeted batch passed **3 tests, 0 failed, 0 skipped, 7m48s**: migration
30.2958s, concurrent settlement 4m20.7257s, advice/full-flow witness 2m57.0819s.
Missing advice now returns HTTP 400 / VALIDATION_FAILED (1.4059s); a supplied
reference with the wrong PO currency still returns HTTP 409 (1.0209s).
The direct restricted runtime call returns P0001 with the bank-advice refusal.
The same five refusal-phase counts remain unchanged. The parent witness then
completes the domestic settlement with one payment and two allocations.

The retained concurrent settlement still produces one payment of 123650.01,
two allocations (118000.01 and 5650), and zero outstanding on both bills.
First request 201, competitor 409, competitor retry 409, replay 201; no 40P01.
Migration apply, two reprovisions, rollback and reapply preserve permissions;
an intentionally changed function body is refused. Both posting tests finish
the full three-band flow. These are targeted counts, not a new full-suite count.
Evidence: bank-advice-verified-release.json, payment-settlement-verified-release.json,
payment-function-permissions-release.json and item11-advice-verified-release.trx
under local-evidence/item11. Debug build passed with zero warnings/errors in 4m19.26s. The same targeted
batch passed **3 tests, 0 failed, 0 skipped, 7m45s**: migration 29.4229s,
concurrent settlement 4m17.5850s, advice/full-flow 2m58.3737s. Missing advice
returned HTTP 400 in 1.3929s and wrong currency HTTP 409 in 0.3403s; the direct
runtime call returned P0001. The same five counts remained unchanged, and
both posting cases completed the full three-band witness. Matching Debug
JSON/permissions artifacts and item11-advice-verified-debug.trx are retained
alongside Release. No owner database was used. The initial failing baseline
is retained separately; the final selected checks in both builds have zero failures.

## Provisional INR decision remains open

The frozen guideline says GRN provisional cost uses approved PO value.
Current code copies the PO line payable amount directly into FIFO cost.
A foreign PO amount therefore cannot serve as an INR stock cost without an
explicit policy, especially before its instalments have all been paid.
Approval limits also need an INR basis.

The pending question proposes Accounts entering a provisional INR PO value,
then immutable cost adjustments from actual banker INR amounts. No conversion
would be performed by the ERP. Until that policy is answered, do not silently
use foreign amounts as INR, introduce a rate table, or claim complete foreign
procurement support.

Remaining independent work: immutable actual-INR and bank-charge evidence per
payment; foreign-currency cash totals across advances, bill payments and PO
revisions; complete advice attachment linkage. Dependent work: provisional
approval/cost basis and immutable landed-cost adjustments, including consumed
stock and actual job cost, followed by the complete three-band witness.

Earlier code inspection found that vendor positions combined currencies and
payable rows omitted CurrencyCode. The correction, frontend contract and
verification are recorded below; actual-INR payment evidence and costing
remain separate unfinished parts of Item 11.

## Currency-aware payable and vendor-position reads

Implemented after bank-advice commit 4c5325e. The response now includes
required CurrencyCode on VendorPayableView and VendorPositionView; missing
currency is not defaulted to INR. Vendor positions are grouped by vendor and
currency. The frontend must use (VendorId, CurrencyCode) as the position row
identity, display the currency beside its amounts, and avoid summing unlike
currencies. Routes and array envelopes are unchanged. The existing INR values remain unchanged in the domestic witness.

The new function-only migration checks the provider, PostgreSQL version,
protected database names, exact function bodies, owners, fixed search path,
SECURITY DEFINER state and allowed function grants on both Up and Down.
There is no currency conversion or rate lookup.

Baseline Release build: zero warnings/errors, 4m25.42s. Baseline test:
0 passed, 1 failed, 0 skipped, 3m16s. The actual domestic API's two payable
rows omitted CurrencyCode. Separately, copied read tables and the deployed
function bodies in an isolated, rolled-back schema combined USD and INR into
one position: advances 18, bills 123650.01, net 123632.01. That combined net has
no meaningful single currency.

The isolated read fixture expects two positions: USD advances 7, bills 5650,
net 5643; INR advances 11, bills 118000.01, net 117989.01. Its copied tables do
not carry the source's transaction guards or constraints. It tests read-side
currency separation, not governed foreign procurement, foreign FIFO costing
or dashboard proof. The actual source financial records are only read during
the fixture; successful verification must then finish their real domestic
settlement and the full three-band flow through the restricted runtime API.
Baseline evidence: local-evidence/item11/currency-read-baseline-release.json
and item11-currency-baseline-release.trx. Post-change Release build passed with zero warnings/errors in 3m59.61s.
The targeted batch passed **3 tests, 0 failed, 0 skipped, 7m47s**:
currency read/full-flow 4m18.6266s, advice regression/full-flow 2m59.9241s,
and migration authority/rollback/reprovision 28.8869s. Actual domestic API
rows identify INR. The isolated mixed-currency read returns exactly the two
positions specified above. Both posting cases complete the full three-band
flow; the currency case also verifies concurrent settlement through the real
restricted runtime. Evidence is currency-read-verified-release.json,
currency-advice-regression-release.json, currency-payment-settlement-release.json,
currency-payment-permissions-release.json and item11-currency-verified-release.trx
under local-evidence/item11. Debug build passed with zero warnings/errors in 4m27.24s. The same targeted
batch passed **3 tests, 0 failed, 0 skipped, 7m56s**: currency read/full-flow
4m23.7653s, advice regression/full-flow 3m03.1247s, migration checks 29.6719s.
The same two currency positions and actual domestic INR responses are verified;
both posting cases finish the full three-band flow. Matching currency-*Debug
artifacts and item11-currency-verified-debug.trx are retained. These are targeted
counts, not a new full-suite result. No owner database was used.


## Retained bank-advice files (verified)

The new runtime upload stores immutable PDF, JPEG or PNG bytes in PostgreSQL,
scoped to company and vendor, with a 5 MiB limit. PostgreSQL derives the byte
count and SHA-256 digest from the stored content. Download checks the digest.
This proves retained-byte integrity, not that a bank issued the document;
Accounts must review the advice.

Only current FULL or TEMPORARY ACCOUNTS_MANAGER authority can upload. Upload,
audit and command receipt share one serializable transaction. Identical
idempotent retries return the existing document; changed content under the
same key is refused. There is no edit/delete endpoint. Migration rollback
refuses to discard retained documents.

Frontend contract: under /api/v1/accounts/vendor-financial-evidence,
POST /bank-advices takes multipart vendorId and one file, plus a required
Idempotency-Key header (1–100 characters). The 201 metadata contains Id,
CompanyId, VendorId, vendor names, FileName, ContentType, SizeBytes,
ContentSha256, EvidenceObjectKey, CreatedAt and Replayed. Use the returned
EvidenceObjectKey for an advance/payment. GET /bank-advices/{id} returns
metadata; GET /bank-advices/by-key?evidenceObjectKey=... resolves a stored
reference; GET /bank-advices/{id}/content downloads the file. Existing
company/employee and page permissions apply. No frontend screen is included.

Foreign advances/payments now require a retained advice belonging to the same
company and vendor. Domestic references remain supported; a domestic reference
using the bank-advice: prefix must also resolve to a matching retained file.
Historical payments are not rewritten. This does not yet record actual banker
INR, fees, or complete the foreign PO cash cap and costing work.

Implementation follows c43f90d. Verification history and final passing results
are recorded below.


Storage test baseline: the first Release batch had 0 passed, 4 failed in
2m52s, all at installer provisioning. The advice table had not been excluded
from the generic direct-table privilege assertion. Correcting both ordinary
table lists preserved the separate EXECUTE-only checks. Rebuild passed with
zero warnings/errors in 35.88s.

The next batch reached upload: the first upload succeeded, but identical
replay returned 403 because the new function requires active command context
and the retry's command was already committed. The fix uses the existing
actor/identity/assignment-checked receipt reader and records the document ID
in the original committed receipt. The batch finished 3 passed, 1 failed,
0 skipped in 12m08s. After applying the replay fix, Release rebuilt with zero
warnings/errors in 4m21.68s. The subsequent four-test batch verified 201 replay, atomic rollback on injected audit failure,
matching download bytes, company/vendor linkage refusals, and refusal to
roll back populated advice storage.


Final Release verification: **4 passed, 0 failed, 0 skipped, 13m10s**.
Migration checks took 1m18.9109s; upload/payment/full-flow took 3m47.7226s;
backup/recovery/full-flow took 5m00.4958s; existing foreign advice refusal/
full-flow took 3m03.0377s. These are targeted checks, not a new full-suite count.

The upload test retained exactly three documents, three upload audits, three
requests and three receipts. Injected audit failure returned 500 and left
all four counts at one; retry succeeded. Identical concurrent uploads returned
201/201, with only one document for that key; no serialization-refusal branch
is claimed for that pair. The linked payment race returned 201/409, retry 409,
replay 201: one payment of 123650.01, two allocations, no remaining payable
for those bills and no deadlock. Missing retained reference returned 409.
Company/vendor mismatch was refused, downloaded bytes matched, and populated
rollback was refused.

Release backup evidence is under local-evidence/item26/
automated-backup-a6221aed59d74dcdbb3c06b846186bdf. Its 223 tables contain one
430-byte advice file, 129 matching requests/receipts, three POs, 28 movements
and three FIFO consumptions. Exact restored bytes and SHA-256 were verified
through the restricted runtime reader. Direct backup took 36.2018s; recovery
28.7170s. The scheduled backup completed and its temporary task was removed.
This remains an owned disposable database and one-shot scheduler witness,
not a permanent customer backup deployment.

Release artifacts under local-evidence/item11:
bank-advice-storage-verified-release.json, storage-advice-regression-release.json,
storage-payment-settlement-release.json, storage-payment-permissions-release.json
and item11-storage-final-release.trx. Earlier failed baselines remain separate.
Debug verification follows.


Final Debug verification: build zero warnings/errors in 4m43.42s;
**4 passed, 0 failed, 0 skipped, 13m08s**. Migration checks took 1m21.3047s,
upload/payment/full-flow 3m51.8778s, backup/recovery/full-flow 4m56.1601s,
and foreign advice refusal/full-flow 2m58.8299s. The same row counts, 201/201
upload replay pair, atomic failure rollback, linked payment 201/409/retry409/
replay201, company/vendor checks and retained-document rollback refusal passed.

Debug backup evidence is under local-evidence/item26/
automated-backup-7613c73c0ab941f6a557c357e9c9312b: 223 tables and the same
430-byte PDF restored exactly, direct backup 34.6128s, recovery 26.7480s.
The scheduled backup completed and the temporary task was removed.
Matching storage-*Debug and bank-advice-storage-verified-debug.json artifacts
and item11-storage-final-debug.trx are retained beside Release.

The retained-advice component is verified. Item 11 remains partial: actual
banker-INR/fee capture, total-cash limits across PO revisions, provisional INR
approval/cost basis and landed-cost allocation are not completed by this change.
No owner database was used, and no permanent customer schedule was installed.
