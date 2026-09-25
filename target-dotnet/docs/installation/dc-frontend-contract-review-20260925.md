# Machine DC frontend contract: backend review against `7a7a030`

Reviewed on the night of 24-25 September 2026 at the Technical Director's request. The
contract is `docs/installation/machine-delivery-frontend-contract.md` as of `7a7a030`
(*require the DC screen to guard an action that cannot be undone*). It lives on
`origin/feature/frontend`, not on `main`; its merge base with `main` is `deb96c6`. **The
contract is not reissued here.** Each difference below is a finding for both sides, numbered
from #34 onward per the numbering rule of 24 September.

It was checked line by line against the backend it names:
`MachineDeliveryEndpoints.cs`, `MachineDeliveryContracts.cs`, `EfMachineDeliveryService.cs`
and `20260915103000_MachineDeliveryDossier.sql` (the tables, the checks,
`record_machine_delivery`, `machine_delivery_json`, `machine_delivery_signature_content`).
No later migration alters these tables or functions.

## Update, 25 September: #34 and #35 fixed in the backend. The contract changes

> **For the Technical Director to pass to the frontend developer (ILAMPARUTHI).** The review
> below is kept as written. This section replaces its *Findings* for #34 and #35. The backend
> fix is the commit that adds this section (*fix: judge a machine DC request on its own fields
> first*). It passed full suites and three gates on 25-26 September, and reaches the server only
> with the next package.

**One rule for the screen. 400 means the request itself is wrong, so fix the field. 409 means
the request is well-formed but the database state refuses it, so show the reason.**

**400 `VALIDATION_FAILED`, with every failing field named in `Errors`.** Each key below
appears only when that field fails. A request with several faults gets all of them in one
answer. `Detail` repeats every message, so a client that ignores `Errors` still shows the
reason.

**Dispatch** (`POST /api/v1/stores/machine-deliveries/`):

| `Errors` key | When |
|---|---|
| `JobOrderId` | missing |
| `DcNumber` | missing, blank, or more than 100 characters after trimming |
| `Destination` | missing, blank, or more than 500 characters after trimming |
| `Nature` | missing, or not exactly `RETURNABLE` / `NON_RETURNABLE` (uppercase) |
| `Purpose` | missing, or not allowed for the nature: `DEMO`, `TRIAL`, `JOB_WORK`, `SITE_WORK` for RETURNABLE; `CUSTOMER_PO_BASED` for NON_RETURNABLE |
| `DispatchDate` | missing, or after today in India Standard Time |
| `ExpectedReturnDate` | RETURNABLE: missing, or before `DispatchDate`. NON_RETURNABLE: present |
| `IdempotencyKey` | missing, or more than 100 characters |

**Signature** (`POST /api/v1/stores/machine-deliveries/{id}/signature`):

| `Errors` key | When |
|---|---|
| `DeliveredAt` | missing, or in the future |
| `CustomerSignatory` | missing, blank, or more than 200 characters after trimming |
| `Evidence` | no file, an empty file, or more than 5 MB |
| `Evidence.ContentType` | the file is not a PDF, PNG or JPEG, or it does not match `ContentType` |
| `Evidence.FileName` | missing, more than 255 characters, or containing control characters |
| `IdempotencyKey` | missing, or more than 100 characters |

**Still 409 `BUSINESS_RULE_CONFLICT`: the database state refuses the request.** These now
carry a sentence, never PostgreSQL's constraint text.

| Case | `Detail` |
|---|---|
| Job not FAT READY, or unknown | *Machine dispatch requires this company job to be FAT READY.* |
| No linked customer PO | *Machine dispatch requires its linked customer PO.* |
| Dispatch before the FAT reconciliation date | *Nature, purpose, dispatch date and required return date must be valid after FAT readiness.* |
| The job already has a DC | *This job already has a machine DC.* |
| The DC number is already used in this company | *This DC number is already used in this company.* |
| Signing an unknown or other-company DC | *Machine DC not found in this company.* |
| Delivery before the dispatch date, or FAT identity changed since dispatch | *Signed delivery requires unchanged FAT-ready machine identity, a valid delivery date and retained signature.* |
| The DC is already signed | *This machine DC is already signed.* |
| NON_RETURNABLE with no Managing Director to notify | *NON_RETURNABLE dispatch requires an active Managing Director notification recipient.* |

**Unchanged:**

- 403 for a missing permission or a non-substantive assignment;
- `409 CONCURRENCY_CONFLICT` on a race;
- a same-key retry replays the receipt;
- 404 only for the DC view and the evidence download;
- every success response is byte-for-byte as before.

**Timestamps:** `DeliveredAt` still goes in UTC, ending in `Z`, as for all 13 fields. A value
with no zone is now read as India Standard Time and logged as a warning naming the field. That
is a safety net, not permission.

## What matches

Everything not listed as a finding below matches the backend:

- **Routes and permissions:** five routes; `stores.machine-deliveries` Issue and View;
  evidence behind `reports.machine-dossier` View.
- **Substantive assignment:** `FULL` or `TEMPORARY` for the job list and both writes, else 403.
- **Job list:** `pageSize` default 50, clamped 1-200; `search` at most 200 characters,
  contains over serial, number and customer; ordered by machine serial; only READY jobs with
  a FAT reconciliation and a customer PO; paged envelope `TotalCount`, `PageNumber`,
  `PageSize`, `Items`.
- **Dispatch rules:** the Nature and Purpose sets; `ExpectedReturnDate` required and on or
  after dispatch for RETURNABLE, null for NON_RETURNABLE; dispatch not in the future and not
  before the Asia/Kolkata FAT reconciliation date; one DC per job; `DcNumber` unique per company.
- **Managing Director:** a NON_RETURNABLE dispatch notifies the MD, and is refused whole if
  there is no MD recipient.
- **Signature:** 1 byte to 5 MB; the type sniffed and required to equal `ContentType`;
  basename 1-255 with no control characters; SHA-256 computed by the server (a generated
  column); one immutable signature per DC; refused if FAT readiness, reconciliation or serial
  changed, or if there is no Actual BOM.
- **DC view:** `MachineState` and `DcState` derived from the signature; `Signature` without
  `Content` but with `ContentSha256` and `RecordedAt`; 404 for an unknown or other-company id.
- **Races and retries:** a serialization failure gives `409 CONCURRENCY_CONFLICT`; a retry
  with the same key replays the receipt.

## Findings

### #34: a missing text field on dispatch or signature gives 500 (backend defect)

`record_machine_delivery` inserts `btrim(p_payload->>'dcNumber')`, `'destination'` and
`'customerSignatory'` without checking them for null. A request that **omits** one of these
fields, or sends `null`, reaches the column's `NOT NULL` constraint. That is SQLSTATE 23502,
which `EfMachineDeliveryService.Execute` does not catch (it maps only 42501, P0001, 23514 and
23505). The exception reaches the central handler as **500 INTERNAL_ERROR**: a client mistake
reported as a server fault, the same class as #31. An empty or whitespace-only value is fine,
because the CHECK constraint refuses it as 409.

- **Frontend:** the required fields are required in the form, so the screen should never
  send this. Nothing to change beyond not omitting fields.
- **Backend fix, not built tonight:** validate the required text fields in the service
  before the command, as 400 naming the field. About **half a day** with tests, plus a full
  cycle. It is not started, because the night's cycle is taken by #31 and nothing long may
  start after 07:00. **Needs a slot from the Technical Director.**

### #35: the field rules answer 409, not 400 (contract differs from backend)

The contract's *Errors* section says `400` for the validation rules. The backend answers
**`409 BUSINESS_RULE_CONFLICT`** for every rule enforced in SQL, which is nearly all of them.
The rule either raises `P0001` in `record_machine_delivery` or trips a table CHECK (`23514`),
and the service maps both to `StoresConflictException`. That covers:

- Nature, Purpose and the Nature/Purpose/return-date combination;
- both dispatch date rules;
- `DcNumber` and `Destination` length or blankness;
- `CustomerSignatory` length or blankness;
- both `DeliveredAt` rules, and an unknown or missing job.

**400 is returned only** for the signature file checks (size, type, name), a missing or
over-long `IdempotencyKey`, a job search over 200 characters, and malformed JSON.

Two consequences for the screen:

- A CHECK refusal carries PostgreSQL's own text as `Detail`, for example *new row for
  relation "machine_delivery_challans" violates check constraint …*. That is not a message
  for a Stores operator. The contract's four rules (confirmation in words, nature-bound
  purposes, no defaults) already prevent composing these, so this is the last line, not the
  first.
- Branch on `409 BUSINESS_RULE_CONFLICT` for these refusals, not on `VALIDATION_FAILED`. Show
  a generic *"The server refused this dispatch: check the nature, purpose and dates"*
  alongside `Detail`, not in place of it.

**Backend option, same slot as #34:** validate these fields in C# as 400 with field-level
`Errors`, which gives the operator words and the frontend a field to highlight. About one day
together with #34, plus a cycle. Until then the contract's *Errors* section should read 409
for these. Changing the contract is the frontend developer's call; this review does not
reissue it.

### #36: the signature example sends `DeliveredAt` with `+05:30` (contract against #31)

> **Already corrected by the frontend developer**, after `7a7a030`, on the same branch:
> `bc0a26d` and then `d6efb79` (23 September). The example now sends
> `"2026-09-30T10:50:00Z"`, and the rule is stated as one rule for every `DateTimeOffset`. It
> also says that this endpoint would probably have survived an offset, which matches the
> reading below. The #34/#35 text (*"400 for the validation rules above"*) is unchanged at
> the branch head. **One inaccuracy remains in the corrected text:** it says an offset
> elsewhere surfaces "as a 400 with an unactionable message". That was true before `7003c02`
> (400 `NpgsqlTransaction`). Since `7003c02` it is a 500, and with the #31 converter the
> request is accepted as the same instant. The UTC rule stands either way.

The contract's signature example is `"DeliveredAt": "2026-09-30T16:20:00+05:30"`. That goes
against the frontend obligation of #31: every timestamp is sent in UTC, ending in `Z`. On this
endpoint it happens to work, which makes it easy to copy elsewhere. `DeliveredAt` reaches
PostgreSQL inside a JSON payload, which PostgreSQL parses itself, so it never met the Npgsql
refusal. The same notation on the other 12 fields was a 500 until the UTC converter.
**Follow the one rule on all 13 fields, this one included:** send
`"2026-09-30T10:50:00Z"`.

### Smaller differences, for information

- **Signing an unknown or other-company DC** answers `409` (*Machine DC not found in this
  company*), not 404. 404 is only the DC view and the evidence download.
- **Timestamps in the DC view** come from PostgreSQL `to_jsonb` and are rendered in the
  database session's time zone, so the offset may be `+00:00` or `+05:30` depending on server
  configuration. Parse them as instants and never compare them as strings.
- **The DC view carries internal columns** as well: `ActorEmployeeId`, `RoleAssignmentId`,
  `RecordedBy`, `CompanyId` and `FatReconciliationId`. They are harmless, but not part of the
  contract. Do not build on them.

## Questions for the Technical Director

1. #34 and #35: schedule the backend fix (about one day plus a cycle) before 29 September,
   when the screen is built? Or ship the screen against 409 and fix after go-live?
