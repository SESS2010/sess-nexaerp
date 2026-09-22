# Item 13 — opening stock order for both SESS companies

Confirmed by the Technical Director on 14 September 2026.

Pvt Ltd and Proprietorship each require their own opening-stock ceremony. Complete both companies' ceremonies before starting GRNs at go-live.

For each company, select that company and complete its opening import, Stores Manager count, Accounts Manager valuation confirmation and Technical Director authorization. Confirm the resulting OPENING_BALANCE movement and OPENING_LANDED layer before transaction entry.

Opening is company-scoped. Authorizing Pvt Ltd does not introduce stock into Proprietorship, mark its ceremony complete, or allow it to use Pvt Ltd's balance. Until its own ceremony posts, a company's reports contain only its own existing ledger balance.

The ordering is enforced: both recording and authorizing opening refuse if that company already has any stock movements. Therefore opening must precede that company's first movement, including its first GRN. The two companies are independent in the code; the go-live checklist requires both ceremonies before either starts receipt entry so neither is overlooked.

Do not bypass this refusal with direct SQL. A company that has already posted movements needs a separately governed correction path; this document does not authorize rewriting its ledger.

Source: EfOpeningStockService (selected-company resolution and scoped commands), OpeningStockSql (company import validation and no-prior-movement guards), and the Technical Director's accepted 14 September instruction. This clarification changes no API route, field, migration or business row.