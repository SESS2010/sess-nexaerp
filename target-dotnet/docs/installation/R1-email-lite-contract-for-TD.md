# R1 email-lite contract for the TD

Updated 29 September 2026. Canonical path: `docs/installation/R1-email-lite-contract-for-TD.md`.

## Evidence and ownership

CONFIRMED (repository): migration 137, the outbox store, PO issue hook, print query,
tracking digest query and registration/endpoint stubs exist. Email sender, worker,
composers, digest scheduler and admin endpoints are not implemented by those stubs.
CONFIRMED: AGENTS.md reserves the Email folders, endpoints and tests for the TD;
the earlier TD instruction left implementation ownership undecided. A fresh ownership
answer is pending before backend coding begins. ILAMPARUTHI alone owns the frontend,
including the e-mail log. The integrator owns migrations and shared registration/package
files. Point 1's focused green result unblocks planning; it does not transfer file ownership.
CLAIMED (target): first merge 2 October; freeze 3 October 18:00 IST; go-live
15 October 09:00 IST. All merges need focused and fast tests and a nightly before main.

## Provider and settings contract

CONFIRMED (TD configuration decisions): provider cPanel; sender `erp@sess.co.in`;
assumed cap 50 sends/hour; first week TEST mode restricted to the TD's mailbox.
CLAIMED (TD-reported, not independently read back): the sender mailbox is created.
Host, port, transport and the test recipient are still to be provided. The integrator
never connects to the server; commissioning read-back belongs to the operator.

CLAIMED (proposed keys, to implement and validate in the TD's branch):

| Setting | Required value or behavior |
|---|---|
| `Email:Enabled` | Enable/disable worker and scheduled sends |
| `Email:Mode` | `TEST` initially; explicit operator switch to `LIVE` |
| `Email:AllowList` | TD test mailbox only in week 1; configured outside the repo |
| `Email:Smtp:Host` | cPanel SMTP hostname from TD |
| `Email:Smtp:Port` | Port from TD; do not assume 587 or 465 |
| `Email:Smtp:Security` | Explicit `StartTls` or `SslOnConnect` from TD; no insecure fallback |
| `Email:Smtp:User` | SMTP login confirmed by TD, not inferred from sender |
| `NEXAERP_SMTP_PASSWORD` | Secret environment variable set by operator on server; never committed or returned |
| `Email:From` | `erp@sess.co.in` |
| `Email:FromNamePerCompany` | Company-specific display names |
| `Email:HourlyLimit` | 50 assumed; shared cap across all send types and companies for the mailbox |
| `Email:PurchaseMailbox` | Purchase CC address, supplied outside the repo |
| `Email:VendorPoEmailEnabled` | Configuration switch, initially false |
| `Email:DigestTimeIst` | `09:00` |
| `Email:DigestDays` | `Mon-Sat` |

The proposed explicit Security value replaces the earlier draft's ambiguous
StartTls boolean; it must not be represented as an already implemented option.
Never log secrets or return them from the settings endpoint; report presence only.
TEST mode must validate every To/CC destination against the allow-list, including
manual tests and retries. Do not silently send to original recipients. Whether
out-of-list PO mail is redirected to the test mailbox or recorded BLOCKED_ALLOWLIST
needs the TD's business decision; existing default is BLOCKED_ALLOWLIST.

## Existing backend interfaces

CONFIRMED (source): namespace `SESS.NexaERP.Application.Outbox` exposes
`IEmailOutboxStore`, `EmailOutboxRequest`, `EmailComposition`, `EmailOutboxItem` and
`EmailOutboxPage`. Use these concrete interfaces, not the obsolete draft signatures.

- `EnqueueAsync`: idempotent per company and idempotency key.
- `ClaimAsync(status, max, ct)`: due work; QUEUED/FAILED become SENDING;
  PENDING_COMPOSE uses a five-minute lease.
- `ComposeAsync`, `SkipAsync`, `MarkSentAsync`, `MarkFailedAsync`, `MarkBlockedAsync`.
- `RetryAsync`: FAILED/DEAD to QUEUED, preserving attempts.
- `ListAsync`: signed-in company only; returns `EmailOutboxPage(Total, Page, PageSize, Items)`.

`EmailOutboxItem` contains Id, CompanyId, EventType, SourceEntityType, SourceEntityId,
IdempotencyKey, PayloadJson, To, Cc, Subject, Status, Attempts, NextAttemptAt,
LastError, CreatedAt and SentAt. Bodies are absent from the public log DTO.
`IPurchaseOrderPrintQuery` supplies the PO print data.
`ITrackingDigestQuery.PendingForRolesAsync` and `RecipientsAsync` are implemented;
the old statement that the digest query throws until 1 October is obsolete.

CONFIRMED (source): PO issue enqueues `PO_ISSUED:<poId:N>:<revision>` inside the
issue transaction. A rolled-back issue sends nothing; an SMTP failure does not undo
an issued PO. Outbox status values are PENDING_COMPOSE, QUEUED, SENDING, SENT,
FAILED, DEAD, BLOCKED_ALLOWLIST and SKIPPED. Retry delays are 1/5/15/60 minutes,
then DEAD at five attempts. Log rows are retained; runtime has no DELETE grant.

## TD implementation and frontend API plan

CLAIMED (planned, not live endpoints):

| Endpoint | Permission and contract |
|---|---|
| `GET /api/v1/email/outbox?status=&page=&pageSize=` | `admin.email` View; existing EmailOutboxPage shape; no bodies |
| `POST /api/v1/email/test` | TD Update; allow-listed recipient only; response contract to be finalized |
| `POST /api/v1/email/outbox/{id}/retry` | TD Update; FAILED/DEAD only; company authorization; response to be finalized |
| `GET /api/v1/email/settings` | Authorized read; no password value; final DTO to be agreed |

CONFIRMED (migration): TECHNICAL_DIRECTOR has View and Update; IT_MANAGER has View.
The frontend must not expose retry/test actions to a view-only role.

CLAIMED (plan, implementation owner pending): add MailKit sender, outbox worker, HTML/text PO composer,
09:00 IST Mon-Sat digest job and endpoints in TD-owned files; integrator adds the
reviewed MailKit dependency/shared hooks when needed. PO e-mail is HTML/text only,
no PDF. Digest recipients come from active company roles and OfficialEmail;
missing addresses are skipped with a reason. Digest queries retain role scopes.
A mailbox-wide throttle must defer excess work without consuming a send attempt;
restart, competing workers and ambiguous SMTP acceptance need explicit tests.

## Evidence required before merge

CLAIMED (required validation): fake-SMTP tests for allow-list coverage, mode and
transport validation, PO/digest composition, transactional enqueue and idempotency,
claim races, retry/backoff/DEAD, hourly cap across companies, restart behavior,
authorization/company isolation and absence of secrets/bodies from log responses.
No automated test sends real e-mail. Focused and fast tests must pass before the
2 October merge; nightly must be green before the TD moves main. The operator
performs the authorized cPanel test-mailbox read-back on MAGESHWARI SERVER PC;
this laptop never writes to or probes that server.

Frontend fixtures will be under `docs/installation/tracking-mocks/email-log/`.
All mock recipients use `example.invalid`; no actual TD/vendor address list is committed.
