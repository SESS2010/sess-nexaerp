# HTTP 409 error envelopes: what the frontend receives

For the frontend developer, 23 September 2026. **No frontend change is required by this
note** — `ErrorAlert` already branches on the envelope `Code` as of commit 2d3cf61
(22 September), which is the correct thing to branch on. This records what the backend
now guarantees, so the branch can be relied on rather than assumed.

Pending backend acceptance: the change described here is in the Round 4 candidate and is
not committed until all three witness gates pass. This note describes the accepted
behaviour of that candidate, not of the currently deployed commissioning package
(662e9a3), which predates it.

## Read this first if you are touching the API client

**GRN and opening-stock conflicts that previously returned HTTP 500 now return HTTP 409.**

Deterministic audit-insertion failures on those paths used to surface as a server error.
They are conflicts, and they now say so. If any part of the client treats 500 as
"transient, retry later" — a retry wrapper, an interceptor, a toast that says *try again
in a moment* — it will now correctly receive a 409 conflict code instead and must route to
conflict handling, not to retry-later handling.

Nothing else about the status codes changed. No automatic retry was added anywhere in the
application: retry remains a user action.

## The envelope

Every handled failure returns `application/problem+json; charset=utf-8` with the same
shape: `Type`, `Title`, `Status`, `Code`, `Detail`, `TraceId`, `Errors`.

**Branch on `Code`. Never branch on `Detail` text**, which is operator-facing prose and
may be reworded without notice.

## The three conflict codes

| Code | Type slug | Meaning for the user |
|---|---|---|
| `CONCURRENCY_CONFLICT` | `concurrency-conflict` | Someone else changed this record after you loaded it. Reload and retry. Safe to retry after reload. |
| `IDEMPOTENCY_CONFLICT` | `idempotency-conflict` | This idempotency key was already used for a *different* request. **Do not silently retry** — read the earlier result first. |
| `BUSINESS_RULE_CONFLICT` | `business-rule-conflict` | The request conflicts with the current business state. Retrying the identical request will fail again. Show `Detail`. |

All three are HTTP 409. `Type` is always
`https://api.sess.example/problems/<slug>` using the slug above.

## Why the classification is now reliable

Concurrency runs found that some endpoints lost the *type* of a failure before it reached
central error handling, so the envelope had to guess the category from words in the
message. Payment replaced a PostgreSQL SQLSTATE 40001 with a business exception and lost
the cause; QC flattened a typed concurrency exception; GRN and opening stock returned 500
as described above.

A genuine concurrency failure is now classified from a server-side marker set when the
typed exception is caught, not from message wording. Two consequences you can rely on:

- A typed concurrency failure is **always** `CONCURRENCY_CONFLICT`, even when its message
  happens to mention idempotency. Wire case `/typed-concurrency-idempotency`.
- An ordinary business exception raised on the same endpoint is **still**
  `BUSINESS_RULE_CONFLICT` and is not promoted by the change. Wire case `/business-qc`.

Ten wire-contract cases pin this, including three added for the above. That is how we know
it stays true: a regression in classification fails the contract test, not production.

## One honest caveat

For 409s that do **not** carry the typed marker, the backend still classifies by scanning
`Detail` for the fragments `idempot`, then `stale` / `concurr` / `version`, falling back to
`BUSINESS_RULE_CONFLICT`. So a business-rule message that happens to contain the word
"version" can still arrive as `CONCURRENCY_CONFLICT`.

This is pre-existing behaviour, unchanged by this candidate, and deliberately **not**
altered before go-live. It means "reload and retry" is the right default for
`CONCURRENCY_CONFLICT` but will occasionally be shown for a conflict that a reload will not
resolve.

**When the friendly line says reload and retry but the reload does not fix it, the `Detail`
text is the real reason — which is exactly why `Detail` must stay visible to the user and
never be hidden behind the friendly message.**

Replacing the word-scanning classifier with explicit typed markers everywhere is on the
after-go-live backlog, estimated at 1.5-3 days.

## Related

- [Dashboard frontend contract](dashboard-frontend-contract.md) — dashboards add
  `DASHBOARD_ACCESS_DENIED` (403) and `DASHBOARD_SOURCE_INCONSISTENT` (409, with
  `AdministratorActionRequired=true`). Those are separate codes and keep their own handling.
- [OIDC login contract](server-frontend-oidc-contract.md) — `AUTHENTICATION_REQUIRED`,
  `MFA_REQUIRED` and `EMPLOYEE_ACCESS_NOT_CONFIGURED`.
- Source: `src/SESS.NexaERP.Api/Middleware/StandardErrorEnvelopeMiddleware.cs`;
  contract tests: `tests/SESS.NexaERP.Tests/ApiWireContractTests.cs`.
