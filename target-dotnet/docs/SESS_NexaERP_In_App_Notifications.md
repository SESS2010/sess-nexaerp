# In-app notifications

## Operational path

The API runs `InAppNotificationWorker` immediately after startup and then once per minute. Each pass takes a PostgreSQL transaction advisory lock, derives due events from business evidence, inserts one immutable event plus immutable `IN_APP` delivery evidence per recipient, and is idempotent while the source remains open.

Every authenticated employee can read only their own company-scoped recipients:

- `GET /api/v1/notifications?unreadOnly=true&page=1&pageSize=50`
- `GET /api/v1/notifications/unread-count`
- `POST /api/v1/notifications/{recipientId}/read`

Read acknowledgement is one-way. It cannot change the event payload, target, source, or delivery evidence.

## Active frozen timers

- `UNUSED_MATERIAL_OVERDUE`: one day from material issue. Remaining custody subtracts accepted returns and confirmed, unreversed fitments. It is delivered to the named custodian. A later fitment reversal may create a new occurrence after the earlier event completed.
- `QC_AGEING_OVERDUE`: the GRN receipt time plus its frozen `QcCompletionDaysSnapshot` (two days under the settled configuration). It is delivered to every currently effective `QC_MANAGER` in that company and completes when an inspection exists.

The persisted `GoodsReceipt.QcDueAt` is not used as authority because the current GRN finalization function rewrites it from finalization time, while the established QC queue correctly computes ageing from receipt time. Correcting that stored derivative is tracked separately; notification and queue behavior remain aligned to the frozen receipt-time rule.

## Not yet operational

The delivery-challan handoff and automatic deviation workflows currently have only foundation entities; no governed command path produces authoritative handoff or deviation evidence. Their one-day handoff and deviation-threshold notification producers must be activated with those workflows. The notification engine must not infer them from draft/foundation rows.

## Email planning

Email is deliberately not enabled. Adding it requires an administrator-managed provider/credential secret, validated employee email destinations, templating, retry/backoff and dead-letter policy, delivery/bounce evidence, and an explicit privacy/retention decision. It can append `EMAIL` delivery attempts to the existing event/recipient evidence without changing the in-app contract. SMS is not planned.
