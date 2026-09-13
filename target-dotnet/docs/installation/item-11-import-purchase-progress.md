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

Further code inspection: list_vendor_positions currently sums outstanding amounts
across all currencies for a vendor, and list_vendor_payables omits CurrencyCode.
Import support must return separate currency positions and identify payable
currency; adding actual-INR evidence alone would leave misleading balances.
That response change must be documented for the frontend and verified before
calling Item 11 complete.
