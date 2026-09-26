# A2 stock adjustment: physical and FIFO posting — build plan

Written 20 September 2026 (late), before the build. The domain policies exist
(`StockAdjustmentApprovalPolicy`, `StockAdjustmentPostingDatePolicy`,
`StockAdjustmentApprovalSnapshot`, `StockAdjustmentReview`); inventory periods exist
(`/api/v1/accounts/inventory-periods`, CFO). What is missing is the record, the API and
the posting. The ledger contract (`stock_movements`, `stock_posting_batches`, FIFO layers
and consumptions, provenance layers) has to admit a new document kind, which is why this
is a day and a half of work and not an evening.

## Record

- `stock_adjustments` (company, number `ADJ-<company>-<fy>-<n>`, warehouse, reason kind
  `COUNT_VARIANCE | DAMAGE_LOSS | CORRECTION`, effective date, inventory period id,
  status `DRAFT → SUBMITTED → APPROVED → POSTED`, `REJECTED`, current revision, recorder,
  counters, backdate reason and evidence, approval snapshot JSON (required roles,
  excluded employees, absolute value), posting batch, idempotency keys, version).
- `stock_adjustment_revisions` + `stock_adjustment_lines` (item, condition location,
  lot, serial, quantity change signed, unit value for additions, accepted line value,
  changes-serialized-identity flag). Every edit of a submitted adjustment is a new
  revision; decisions bind to a revision (the domain `StockAdjustmentReview`).
- `stock_adjustment_decisions` (revision, employee, role, assignment, decided at,
  reason, decision `APPROVE | REJECT`), append-only.

## Authority

- Stores Executive/Manager records (recorder excluded from approval); counters named on
  the record are excluded too.
- Required roles from the snapshot: value band (Stores Manager < 5,000; TD to 1,00,000;
  MD above), serialized identity → TD, damage/loss write-off → TD + Accounts Manager
  concurrence, backdated beyond seven days → TD in addition. Distinct employees per
  required decision.
- Posting is the last approver's act (the decision that completes the review posts in
  the same transaction), so approval and posting cannot diverge.

## Posting

- New `PostingKind = 'STOCK_ADJUSTMENT'` and `stock_posting_batches.StockAdjustmentId`;
  new `stock_movements.StockAdjustmentLineId` as the document reference (the v2 contract's
  `num_nonnulls(...)=1` list gains it); movement guard and reconcile guard branches.
- Addition: `RECEIPT_IN` into the line's AVAILABLE condition location, Stores custody,
  the company's own ownership account, a new provenance layer of type ADJUSTMENT, a FIFO
  layer at the stated unit value (cost basis `ADJUSTMENT`), origin = the adjustment line
  (`OriginStockAdjustmentLineId`, the third origin next to GRN and opening).
- Removal: `CONSUMPTION_OUT` from the located stock, FIFO consumption oldest-first from
  the same item/ownership pool, recorded against the adjustment line (the consumption
  table gains `StockAdjustmentLineId`; `MaterialIssueLineId` becomes nullable with a
  one-of check).
- Serialized identity change: `TRANSFER_OUT`/`TRANSFER_IN` on the serial with the old
  and new identity retained on the line; no FIFO effect.
- Period: the posting locks the inventory period row (`FOR UPDATE`) and refuses a closed
  period, exactly as period close does; the effective date policy is evaluated from the
  server date and the locked period.
- Concurrency: advisory lock per company/item, serializable transaction, idempotent by
  key with fingerprint, replay returns the retained batch.
- Reversal: a new adjustment of the opposite sign referencing the original, requiring at
  least the original's roles (`originalForReversal`).

## API

`/api/v1/stores/stock-adjustments`: list, read, create (draft), update draft (new
revision), submit, approve, reject, and `/{id}/history`. Page `stores.stock-adjustments`
with grants: STORES_EXECUTIVE/STORES_MANAGER create/update/submit; STORES_MANAGER, TD, MD,
ACCOUNTS_MANAGER approve/reject (the service enforces the snapshot); view for the same.
Reachability manifest declares the new selectors.

## Proof

The rehearsal posts, on the fresh database, a count-variance removal of one opening unit
(Stores Manager band) and a damage write-off (TD + Accounts), then reads the stock
balance, FIFO valuation and movement roll-forward and checks the FIFO consumption is
against the opening layer. A focused test covers the bands, the excluded recorder, the
closed-period refusal, backdating, replay and the reversal.

## Built (20 September 2026, late evening) — where it differs from the plan

- No `stock_adjustment_revisions` table: lines carry `RevisionNumber`; a revision appends
  the next revision's lines and returns the record to DRAFT, so the snapshot is retaken at
  the next submission and decisions bind to the revision they approved.
- The removal value is a FIFO fact: `advance.fifo_carrying_value_preview` walks the item's
  layers oldest-first (net of restorations) and the service records that as the accepted
  line value; a removal that states a unit value is refused. At the decision that completes
  the review the value is recomputed and the band must not have moved, or the decision is
  refused with 409 and Stores resubmits.
- Cost basis of an addition's layer is `ADJUSTMENT_STATED`; the provenance layer type is
  `ADJUSTMENT`; the layer is dated from the effective date.
- Serialized identity change (`TRANSFER_OUT`/`TRANSFER_IN` on a serial) is **not built**. A
  serialized line changes exactly one unit in or out; renaming a serial is a removal and an
  addition until that path exists. Stated here so it is not mistaken for finished work.
- The approver acts in one of the outstanding required roles; the request may name it,
  otherwise the least privileged outstanding role the employee holds is used. The recorder
  and the counters are excluded by the snapshot; a role outside the band is refused (403).
- Rejection returns the record to REJECTED; it is revised (new revision) before it can be
  submitted again. A posted adjustment is immutable (trigger and status check).
- Stores reads open inventory periods through `GET /api/v1/stores/stock-adjustments/inventory-periods`
  (the accounts list is CFO-only), and the rehearsal opens the period as the CFO first.

## Owner decisions (21 September 2026)

- Removal value stays live. Recompute FIFO value at the completing decision; if the band
  moved, refuse with 409 and require resubmission. Do not freeze the submission value:
  the approver must have authority for the value actually removed.
- Serialized identity change remains in the plan after go-live on 8 October, outside go-live scope.
  Correct a wrong opening-stock serial with two auditable adjustments: wrong serial out,
  correct serial in, each through normal approval and posting. This does not mark the
  identity-transfer path built.

## Original order of work

1. Migration: tables, ledger contract (batch kind, document reference, origin, FIFO
   consumption reference), posting function, page and grants — one migration.
2. Domain entities, DbContext, snapshot.
3. Service (record, revise, submit, decide-and-post, reject) and endpoints.
4. Rehearsal step and focused test; full suites; runbook count.
