# Stored derived-value consistency audit

Date: 11 September 2026
Scope: current Purchase, Stores, QC, costing, BOM, custody, and notification read/write paths.

## Finding

`GoodsReceipt.QcDueAt` was the only persisted derivative for which a current
read path independently computed the same business answer from a different
base. GRN finalization and its trigger used `FinalizedAt + snapshot days`;
the QC queue and notification processor used `ReceivedAt + snapshot days`.
Migration `AlignGrnQcDueAtWithReceiptTime` aligns the stored value, finalizer,
trigger, queue, and notification engine to receipt time.

## Other persisted derivatives reviewed

| Derivative family | Persisted authority | Read behavior | Drift assessment |
|---|---|---|---|
| MIR issue `ReturnDueAt` | `IssuedAt + 1 day`, enforced by the database issue contract | custody and notification reads use the stored deadline | One answer; no live competing formula |
| GRN warranty limits | vendor bill date + 13 months, enforced by the GRN-line guard | GRN reads return the stored snapshot | One answer; immutable receipt evidence |
| PR estimated totals and stock-check quantities | line/header and stock-check snapshots | PR reads return the same stored evidence | Deliberate point-in-time snapshots, not live balances |
| quotation, comparison and PO monetary totals | canonical commercial calculator plus database reconciliation guards | document reads return stored frozen commercial evidence | One answer; transition-time recalculation is validation, not a second projection |
| vendor-bill payable, charge and landed totals | owner-executed acceptance functions and immutable allocation rows | bill reads return the stored accepted evidence | One answer; no independent live total is presented |
| FIFO availability and stock balances | immutable layer, consumption and stock-movement ledgers | projections aggregate those ledgers live | No stored balance competes with the projection |
| Actual BOM accepted values | immutable fitment entries plus append-only landed-cost adjustments | projections aggregate base entries and adjustments | No mutable cached total; movement after bill acceptance is explained by new evidence |
| item last-purchase rate/date/bill | transactionally maintained materialized pointer to the newest accepted bill | item reads use the pointer and retain bill provenance | No second client-visible calculation; database tests reconcile the pointer after acceptance/reversal |
| FAT reconciliation quantities | immutable reconciliation snapshot | readiness rechecks `fat_live_balances` before READY | Two temporal views by design: historical reconciliation evidence versus current readiness, explicitly named and not interchangeable |
| approval step counters | workflow snapshot and completed-step count | queues and document reads use the stored workflow state | One answer; database transition guards constrain increments |

## Guardrail

The PostgreSQL witness now finalizes a GRN received three days earlier and
asserts that `QcDueAt = ReceivedAt + QcCompletionDaysSnapshot` and is not based
on finalization time. A migration test also proves Up, Down, and reapply on a
disposable cluster, while an application-alignment test pins create/update,
QC queue, and notification calculations to receipt time.
