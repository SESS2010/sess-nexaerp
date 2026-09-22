# Actual BOM: governed input tax credit and snapshot valuation

Accounts sets ItcEligibility when creating the company/HSN/state tax rule through the existing governed workflow. TD/MD approval, versioning, independent decision authority and immutable approved rules remain in force. No per-item classification and no vendor-bill-line tax flag are introduced.

| ItcEligibility | RecoverableTaxPercent | Tax included in inventory cost |
|---|---|---|
| FULLY_RECOVERABLE (default) | null | zero |
| BLOCKED | null | all captured tax |
| PARTIALLY_RECOVERABLE | greater than 0 and less than 100, maximum 6 decimals | tax times (100-percent)/100 |

Omitting eligibility defaults to FULLY_RECOVERABLE. Partial eligibility without a valid percentage is rejected, as are percentages supplied for full or blocked eligibility. Registration and reverse-charge fields are independent facts and never infer credit eligibility.

The eligibility and percentage are copied into the quotation TaxRuleSnapshotJson and retained through comparison and PO capture. Valuation reads the PO's immutable tax and commercial snapshots, never the current tax master. Taxable commercial amounts already include agreed charges and discounts; captured round-off is retained. Capitalized bill charges, including customs duty and NON_CREDITABLE_TAX, are added afterward. RECOVERABLE_GST charge allocation remains excluded by its existing governed classification.

Actual BOM and the vendor-bill line's LandedUnitRate use the same calculation. Existing FIFO layers/adjustments and Actual BOM entries remain immutable; Actual BOM reads return valuation deltas and existing reversal handling negates them. Estimated BOM and its ex-tax baseline are unchanged. This does not rewrite historical FIFO report balances or financial payable amounts.

For legacy snapshots without the new fields, the explicitly adopted compatibility default is FULLY_RECOVERABLE. Existing snapshot JSON is not rewritten and current master eligibility is not retroactively joined. Pre-commercial-snapshot records retain the agreed BilledUnitRate only under that full-credit default; explicit blocked/partial snapshots without captured commercial tax amounts are rejected rather than guessed. Historical exceptions require governed review, not a silent mutation.

## Exact regression

Two estimated units at 1,250 give 2,500. One fitted fully recoverable unit gives 1,250. Old variance: 1,475 - 2,500 = -1,025; corrected variance: 1,250 - 2,500 = -1,250. Both exact numbers remain asserted, including a deliberately tax-inclusive historical FIFO adjustment to prevent regression to that source. Blocked, partial, reverse-charge, customs/capitalized costs, fractional fitted quantity, pre-bill fitments, immutable evidence and company isolation have explicit cases.

## Frontend contract

Tax-rule creation accepts additive ItcEligibility and RecoverableTaxPercent properties; the tax-rule list returns both. Show a default FULLY_RECOVERABLE selection and expose the percentage only for PARTIALLY_RECOVERABLE. The percentage is the claimable share, not the cost share. Existing quotation and PO snapshot JSON carries the same two camel-case properties. Bill-line and Actual BOM response shapes are unchanged; their valuation numbers may change.

## Migration effects and acceptance

GovernedTaxInputCreditEligibility adds two tax-rule columns and one check constraint, and extends authoritative quotation/comparison/PO reconciliation to validate the captured ITC fields against the approved rule. Legacy snapshots retain their original JSON shape through comparison and PO creation. Every existing tax rule reads FULLY_RECOVERABLE/null through the column defaults. No item, bill-line, PO snapshot, FIFO or Actual BOM row is inserted, updated or deleted by the migration. ActualBomLandedRateValuation adds two internal calculation functions and replaces the guarded fitment function, bill reader and Actual BOM projection. Downgrade restores previous functions and removes the added schema.

Both frozen working-candidate V8 solution builds succeeded with zero warnings and errors before testing. Full working-candidate Debug: 874/874 (finding12-itc-debug-v9.trx); full working-candidate Release: 871/871 (finding12-itc-release-v8.trx), with no failures or skipped tests. Debug V9 reran the unchanged V8 binaries after the prior run timed out starting disposable PostgreSQL; that failed run is retained and is not acceptance. The focused eligibility/valuation/snapshot checks also passed 20/20 in each configuration. TRX files are under local-evidence/finding12. These full suites cover the frozen working candidate, including pending line-ending, technical-scope, category and read-grant fixes; they are not clean per-commit checkout runs. The exact staged #12 solution separately builds in both Debug and Release with zero warnings/errors. No owner database has been touched.