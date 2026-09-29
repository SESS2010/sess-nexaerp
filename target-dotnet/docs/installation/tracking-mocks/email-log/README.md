# E-mail log mocks (R1, design only)

CONFIRMED: frontend owner is ILAMPARUTHI on feature/frontend. The TD assigned email-lite
backend ownership to the integrator on 29 September, superseding the older AGENTS.md row.
Canonical API plan: `../../R1-email-lite-contract-for-TD.md`.
CLAIMED: admin endpoints and action responses remain planned until the integrator implements and validates them.
These fixtures do not start or implement email-lite, SMTP, a worker, or an admin API.
They use the existing Application/Outbox/EmailOutboxContracts.cs list shape and the
planned email-lite admin contract. The backend contract must confirm HTTP action responses before frontend actions are enabled.
All IDs, document numbers, messages and example.invalid addresses are invented.

| File | Planned endpoint/scenario |
|---|---|
| outbox.json | GET /api/v1/email/outbox?page=1&pageSize=50; all eight statuses |
| outbox-failed.json | GET /api/v1/email/outbox?status=FAILED&page=1&pageSize=50 |
| outbox-empty.json | Same list shape, no rows |

## Screen and wire contract

App route: /admin/email. Use PascalCase fields exactly as in the JSON. The page shape is
Total, Page, PageSize, Items. Row identity is Id (UUID), not array position or Subject.
CompanyId is a UUID; the eventual API must scope the list to the selected company.
PayloadJson is a JSON **string**, not an embedded object; it is internal event metadata and
need not be displayed. To and Cc are arrays. Subject, NextAttemptAt, LastError and SentAt may
be null. Bodies and credentials are absent and must not be requested or rendered.

Show event, subject, recipients, status, attempts, created time, next attempt, sent time and
sanitized error text. Display times in Asia/Kolkata. Support loading, empty, error, forbidden,
status-filter and paged-list states. Do not treat absent backend routes as an empty successful log.
The standalone status examples represent different synthetic events/revisions, not one live workflow.

## Permissions and actions (existing planned contract)

- Page admin.email View: TECHNICAL_DIRECTOR, MANAGING_DIRECTOR and IT_MANAGER. Resolve effective grants; do not
  infer permission from whether the fixture loads. Backend authorization remains mandatory.
- Update is for TD and MD. IT_MANAGER is read-only. Retry is visible/enabled only with Update and
  Status FAILED or DEAD. Planned request: POST /api/v1/email/outbox/{Id}/retry.
- Send test requires TD/MD Update and is allow-list restricted. Planned request: POST /api/v1/email/test
  with {"to":"recipient@example.invalid"}. No real address is supplied by these fixtures.
- Settings are read-only in the planned API. No settings response shape or credential input is
  invented here; wait for the backend owner's confirmed contract.
- Retry/test success response shapes are not yet frozen. Mock UI actions must be clearly local,
  never contact SMTP, and stay disabled against an unavailable backend. Display the standard
  400/403/404/409 server sentence when the real API is available.

PENDING_COMPOSE, QUEUED, SENDING, SENT, FAILED, DEAD, BLOCKED_ALLOWLIST and SKIPPED are the
existing outbox statuses. Automatic backoff and retries are backend concerns. SENT is terminal;
BLOCKED_ALLOWLIST and SKIPPED are not manual-retry states in the existing store contract.

Keep mocks behind development-only imports. Production bundles must contain no fixture data,
mock token or alternate development authentication. This handoff changes documentation only.
