# Frontend contract: machine delivery challan

Frozen 23 September 2026 for the 29 September – 1 October screen. Build after production
login. No new backend field or endpoint is required by this contract. Source baseline is
`MachineDeliveryEndpoints.cs`, `MachineDeliveryContracts.cs`, `EfMachineDeliveryService.cs`
and migration `20260915103000_MachineDeliveryDossier`. Background and the delivery rules
this screen exposes are in [item-15-machine-delivery-dossier.md](item-15-machine-delivery-dossier.md).

## Scope

Three things only: dispatch a FAT-ready machine, capture the customer signature with its
retained evidence, and show the signed DC the dossier reads.

**There is exactly one DC per job order** (`UNIQUE(CompanyId, JobOrderId)`). Return,
re-dispatch, cancellation and correction do not exist in the backend. Do not render
buttons for them, and do not offer a "new DC" action for a job that already has one.
The signed DC is historical evidence; no finished-machine stock is created.

## Because it cannot be undone, the screen must make the mistake hard to make

Owner decision, 23 September: no backend correction ceremony before go-live, and a delivery
challan *should* be hard to alter — a signed DC is evidence the customer holds a copy of. A
correction ceremony goes on the after-go-live list. The screen therefore carries the weight,
and these four rules are part of this contract, not suggestions.

1. **A confirmation step before dispatch.** Show job order, machine, customer, nature and —
   where it applies — the expected return date, **in words, not codes**. "Returnable, for
   demonstration, to be returned by 15 October 2026", never "RETURNABLE / DEMO".
2. **The confirmation names what cannot be changed afterwards**: the DC number, the nature and
   the job order. Say so plainly on the confirmation, not in a help link.
3. **A second confirmation before the signature upload**, showing the chosen file's name, size
   and type. The signature is what makes the machine DELIVERED; say that on the confirmation.
4. **No default that could be wrong.** Nature and expected return date are chosen every time and
   never pre-selected. An empty required field is safer here than a plausible wrong one.

The runbook and the training say the same thing: a machine DC is permanent, check twice.

## Request and authority

Use the existing authenticated API client, the actual user's Keycloak ACCESS token and the
selected-company `X-NexaERP-Company` header. Do not accept an employee/role/subject
override. Server permission and assignment checks remain authoritative; a visible button
is not authority.

| Operation | Endpoint | Permission |
|---|---|---|
| FAT-ready job list | `GET /api/v1/stores/machine-deliveries/job-orders` | `stores.machine-deliveries` **Issue** |
| Dispatch | `POST /api/v1/stores/machine-deliveries` | `stores.machine-deliveries` **Issue** |
| Signature | `POST /api/v1/stores/machine-deliveries/{id}/signature` | `stores.machine-deliveries` **Issue** |
| DC view | `GET /api/v1/stores/machine-deliveries/{id}` | `stores.machine-deliveries` **View** |
| Signature evidence | `GET /api/v1/stores/machine-deliveries/{id}/signature-evidence` | `reports.machine-dossier` **View** |

The three list/write operations additionally require a **substantive Stores assignment**:
the resolved role assignment type must be `FULL` or `TEMPORARY`. A support assignment is
refused with 403 even when the page permission is present.

Evidence download sits behind a **different permission** from the rest of the screen
(`reports.machine-dossier`). A Stores dispatcher who may sign will usually **not** be able
to download what was signed. Render that link only when permitted, and do not treat its
403 as a screen failure.

JSON is PascalCase in both directions (`PascalCaseJsonNamingPolicy`, case-insensitive
binding). Query parameters are camelCase.

## Do not call the Production job-order endpoint

The Stores dispatcher does not hold the permission the Production job-order GET requires.
Use `GET /job-orders` on this group. Parameters: `page` (default 1), `pageSize`
(default **50**, clamped **1–200**, not 1000) and `search` (optional, **at most 200
characters**; longer is a validation error). Search is a case-insensitive contains over
machine serial, job order number and customer name. Rows are ordered by machine serial.

Response is the standard paged envelope:

```json
{ "TotalCount": 1, "PageNumber": 1, "PageSize": 50,
  "Items": [ { "JobOrderId": "3370499a-…", "JobOrderNumber": "JO-000031",
               "MachineSerial": "MC-0916-001", "MachineModel": "…",
               "CustomerName": "…", "FatReadinessStatus": "READY" } ] }
```

The list already excludes everything that cannot be dispatched: it returns only jobs in
the selected company with `FatReadinessStatus = READY`, a latest FAT reconciliation and a
linked customer PO. Do not re-filter on the client, and do not allow free-text entry of a
job.

## Dispatch

`POST /api/v1/stores/machine-deliveries`

```json
{ "JobOrderId": "3370499a-b51c-4687-a753-9bef6d4a52b6",
  "DcNumber": "DC-2026-0002",
  "Nature": "RETURNABLE",
  "Purpose": "DEMO",
  "DispatchDate": "2026-09-29",
  "ExpectedReturnDate": "2026-10-15",
  "Destination": "Customer site, …",
  "IdempotencyKey": "…" }
```

| Field | Rule |
|---|---|
| `JobOrderId` | From the list above. |
| `DcNumber` | Trimmed, 1–100 characters, **unique within the company**. A repeat is a 409, not a field validation error. |
| `Nature` | **`RETURNABLE` or `NON_RETURNABLE`. Nothing else.** |
| `Purpose` | For `RETURNABLE`: `DEMO`, `TRIAL`, `JOB_WORK` or `SITE_WORK`. For `NON_RETURNABLE`: `CUSTOMER_PO_BASED` and nothing else. |
| `DispatchDate` | Date only. **Not in the future**, and **not before the job's FAT reconciliation date** (Asia/Kolkata). |
| `ExpectedReturnDate` | **Required for `RETURNABLE`, on or after `DispatchDate`. Must be null for `NON_RETURNABLE`.** |
| `Destination` | Trimmed, 1–500 characters. |
| `IdempotencyKey` | Required, at most 100 characters. |

**DEMO is a purpose, never a nature.** Bind the purpose list to the selected nature and
clear the return date when the user switches to `NON_RETURNABLE`. The server rejects the
invalid combinations, but the user should not be able to compose one.

Customer, customer PO number, machine serial, machine model and customer name are **taken
from the job order by the server**. Do not send them and do not let the user edit them.

A `NON_RETURNABLE` dispatch also raises an in-app Managing Director notification. If the
company has no active MD recipient the whole dispatch is refused with a conflict; surface
that message as an administration problem, not a form error.

## Signature

`POST /api/v1/stores/machine-deliveries/{id}/signature`

> **Send `DeliveredAt` in UTC, ending in `Z`.** This endpoint carries the timestamp through a
> JSON payload and casts it in SQL, so an offset would probably survive here — but the driver
> refuses a `DateTimeOffset` with a non-zero offset wherever the API binds one directly to a
> `timestamp with time zone` column, and the purchase endpoints do exactly that. Before
> `7003c02` that refusal surfaced as a 400; from `7003c02` it was a 500; since the #31 converter
> (`b394e50`) an offset such as `+05:30` is accepted as the same instant in UTC. The converter is
> a safety net, not permission: convert the operator's local time to UTC before sending and
> convert back for display, here and for every `DateTimeOffset` the API takes. One rule, no
> exceptions to remember.

```json
{ "DeliveredAt": "2026-09-30T10:50:00Z",
  "CustomerSignatory": "…",
  "Evidence": { "FileName": "dc-2026-0002-signed.pdf",
                "ContentType": "application/pdf",
                "Content": "<base64>" },
  "IdempotencyKey": "…" }
```

| Field | Rule |
|---|---|
| `DeliveredAt` | Timestamp **in UTC, ending in `Z`**. Not in the future; its Asia/Kolkata date must not be before the DC's `DispatchDate`. |
| `CustomerSignatory` | Trimmed, 1–200 characters. |
| `Evidence.Content` | Base64 of the file bytes. 1 byte to **5 242 880 bytes (5 MB)**. |
| `Evidence.ContentType` | Exactly `application/pdf`, `image/png` or `image/jpeg`. |
| `Evidence.FileName` | Basename only, 1–255 characters, no control characters. The server strips any path. |
| `IdempotencyKey` | Required, at most 100 characters. |

The server **sniffs the real type from the leading bytes** (`%PDF-`, the PNG signature,
`FF D8 FF`) and refuses the request unless the sniffed type **equals** the `ContentType`
sent. Send `image/jpeg`, never `image/jpg`, and derive the type from the file itself
rather than from its extension. Check size and type before upload so the user is not made
to wait for a 5 MB rejection.

The server computes and stores SHA-256 from the retained bytes; **do not compute or send a
hash**. Signature rows are immutable and one per DC: a second signature is a conflict, not
an update. Signing also refuses if FAT readiness, the FAT reconciliation or the machine
serial changed since dispatch, or if the job has no recorded Actual BOM.

## DC view

`GET /api/v1/stores/machine-deliveries/{id}` returns the challan fields plus:

| Field | Values |
|---|---|
| `MachineState` | `DISPATCHED` until signed, then `DELIVERED`. |
| `DcState` | `DISPATCHED`; after signing `OUTSTANDING` for `RETURNABLE` and `CLOSED` for `NON_RETURNABLE`. |
| `Signature` | `null` until signed; then the signature record **without** `Content`, including `FileName`, `ContentType`, `ContentSha256`, `DeliveredAt`, `CustomerSignatory` and `RecordedAt`. |

**DELIVERED is derived from the retained signature.** FAT readiness alone is not delivery,
and a dispatched DC is not a delivered machine — never label it so. A signed `RETURNABLE`
DC stays `OUTSTANDING` by design; that is its correct end state, not an incomplete one.

Signature bytes are never in this response. Fetch them from
`GET /{id}/signature-evidence`, which returns the file itself with its stored filename and
content type. Show the retained `ContentSha256` beside the download.

An unknown id, or an id belonging to another company, is 404.

## Errors

Application errors use the shared `problem+json` envelope; **branch on `Code`, never on
`Detail` text**. One rule since backend commit `b7f64a9` (*judge a machine DC request on its
own fields first*, findings #34 and #35 of `dc-frontend-contract-review-20260925.md`): **400
means the request itself is wrong, so fix the field. 409 means the request is well-formed but
the database state refuses it, so show the reason.** Expect:

- `401` unauthenticated. `403` for a missing permission, a non-substantive Stores
  assignment, `EMPLOYEE_ACCESS_NOT_CONFIGURED` or `MFA_REQUIRED`.
- `400 VALIDATION_FAILED` for every fault that can be judged from the request alone, with
  **every failing field named in `Errors`** in one answer. `Detail` repeats every message, so
  show it too. Show each `Errors` entry next to its field. Keys:
  - Dispatch: `JobOrderId` (missing); `DcNumber` (missing, blank, or over 100 characters after
    trimming); `Destination` (missing, blank, or over 500 after trimming); `Nature` (missing, or
    not exactly `RETURNABLE` / `NON_RETURNABLE`); `Purpose` (missing, or not allowed for the
    nature); `DispatchDate` (missing, or after today in India Standard Time);
    `ExpectedReturnDate` (RETURNABLE: missing or before `DispatchDate`; NON_RETURNABLE:
    present); `IdempotencyKey` (missing, or over 100 characters).
  - Signature: `DeliveredAt` (missing, or in the future); `CustomerSignatory` (missing, blank,
    or over 200 characters after trimming); `Evidence` (no file, an empty file, or over 5 MB);
    `Evidence.ContentType` (not a PDF, PNG or JPEG, or not matching `ContentType`);
    `Evidence.FileName` (missing, over 255 characters, or containing control characters);
    `IdempotencyKey`.
  - A job search over 200 characters, and malformed JSON, are also 400.
- `409 BUSINESS_RULE_CONFLICT` when the database state refuses a well-formed request. `Detail`
  is a sentence, never PostgreSQL's constraint text: the job is not FAT READY or unknown; no
  linked customer PO; a dispatch before the FAT reconciliation date; the job already has a DC;
  the DC number is already used in this company; signing an unknown or other-company DC (409,
  not 404); delivery before the dispatch date, or FAT identity changed since dispatch, or no
  Actual BOM; the DC is already signed; a NON_RETURNABLE dispatch with no active Managing
  Director recipient (an administration problem, not a form error). Show `Detail` with a
  generic line such as *"The server refused this dispatch: check the nature, purpose and
  dates"* alongside it, not in place of it.
- `409 CONCURRENCY_CONFLICT` when the serializable transaction loses a race: show the
  existing "Someone else changed this record" banner, reload the DC, and let the user
  retry deliberately.
- `404` only for the DC view and the evidence download.

DC view timestamps are rendered in the database session's time zone and may carry `+00:00` or
`+05:30`: parse them as instants, never compare them as strings. The view also carries internal
columns (`ActorEmployeeId`, `RoleAssignmentId`, `RecordedBy`, `CompanyId`,
`FatReconciliationId`) that are not part of this contract; do not build on them.

Both writes are idempotent by `IdempotencyKey` and replay the committed receipt, so a
retry with the **same** key is safe and returns the original DC. A retry with a **new**
key is a second attempt and will conflict. Generate the key once when the form opens, keep
it across retries of that submission, and disable duplicate submits while a request is in
flight.
