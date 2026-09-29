# Call-site audit for the production login

Read-only survey of `target-dotnet/src/SESS.NexaERP.Web/src`, 23 September 2026, on
`feature/frontend`. Nothing was changed. This exists so the login work does not have to
discover the same list twice; the login itself belongs to the frontend developer.

Build against [server-frontend-oidc-contract.md](server-frontend-oidc-contract.md).

## First, a correction to the brief

The brief said "all 11 call sites including the two written inside `PurchaseDocumentRegister`
and `QuotationPage`". That figure counted **files containing the text `fetch(`**, and one of the
two named files is a false positive.

`features/purchase/PurchaseDocumentRegister.tsx` does **not** call `window.fetch`. It declares a
prop (line 20) `fetch: (query: PurchaseDocumentListQuery) => Promise<PagedResponse<T>>` and
invokes that prop at line 78. It is a typed loader passed in by its parent. Leave it alone.

`features/purchase/QuotationPage.tsx:249` **is** a real raw call.

The accurate figure is **15 raw `fetch` calls in 9 files**, plus the shared client itself.

## The shared client: `src/api/client.ts`

One `fetch`, in `request()`. Five things in it have to change.

| Today | Needed |
|---|---|
| `api.get` and `api.getPaged` take **no `headers` argument** (`api.post` and `api.put` do) | every verb must carry `X-NexaERP-Company`; the simplest correct move is to set it centrally in `request()` from the session, not per call |
| Bearer token read from `localStorage` key `nexaerp.dev.bearerToken` via `getStoredToken()` | access token from the OIDC session, held in memory only |
| On 401: clears token and identity, then `window.location.assign('/login')` | **refresh once**, retry the original request, and only then send the user to sign-in |
| On 403: substitutes a generic message when the server gave no `Detail` | branch on the envelope `Code` — `EMPLOYEE_ACCESS_NOT_CONFIGURED`, `MFA_REQUIRED`, `PERMISSION_DENIED` — each needs different words |
| `ApiError` keeps `status`, `message`, `code`, `traceId` | it drops `Type`, `Title`, the `Errors` object (folded into the message string) and any extra field such as the obligations `AdministratorActionRequired`. The dashboards already needed one of those. Preserve the whole envelope. |

There is no `api.delete`. Nothing calls one, so nothing is missing.

## The 15 raw calls, and why they exist

Every one of them builds its own `Headers`, reads `getStoredToken()` itself, and parses the
error body itself. **None sends a company header, because nothing does yet.**

| File | Line | Export | What it does |
|---|---|---|---|
| `api/boms.ts` | 71 | internal `request` helper | its own JSON wrapper, duplicating `client.ts` |
| `api/customerPos.ts` | 73 | `uploadCustomerPoFile` | multipart upload |
| `api/customerPos.ts` | 90 | `downloadCustomerPoFile` | blob download |
| `api/customers.ts` | 78 | `uploadCustomerAttachment` | multipart upload |
| `api/customers.ts` | 94 | `downloadCustomerAttachment` | blob download |
| `api/items.ts` | 71 | `uploadItemImage` | multipart upload |
| `api/items.ts` | 87 | `fetchItemImageUrl` | blob download |
| `api/masterdata.ts` | 14 | `downloadFile` | template/errors workbook |
| `api/masterdata.ts` | 78 | `getImportBatch` | **JSON**, no reason to be raw |
| `api/masterdata.ts` | 97 | master import | multipart upload |
| `api/reports.ts` | 88 | `downloadReportExcel` | XLSX export |
| `api/vendorPayments.ts` | 63 | bank advice | multipart upload, carries `Idempotency-Key` |
| `api/vendors.ts` | 81 | vendor attachment upload | multipart upload |
| `api/vendors.ts` | 97 | vendor attachment download | blob download |
| `features/purchase/QuotationPage.tsx` | 249 | quotation attachment | blob download, and the only one outside `src/api` |

**The pattern matters more than the count.** Thirteen of the fifteen are binary: a `FormData`
upload or a `blob()` download. They bypass the shared client because `request()` always
`JSON.stringify`s the body and always `response.json()`s the reply, so it cannot carry either.

So "route them through `api.post`" will not work. The shared client needs a binary-capable path
as part of this work — an `api.upload(path, formData, headers)` and an `api.download(path)`, or
a single exported `authorizedFetch(path, init)` that the file helpers call. Either way the token,
the company header, the 401 refresh and the error envelope are then in **one** place, which is
the point of the exercise.

Two of the fifteen need no binary support at all and can move straight onto the client once it
exists: `api/masterdata.ts:78` (`getImportBatch`, plain JSON) and `api/boms.ts:71` (a private
duplicate of `request()` — delete it and use the shared one).

## Development sign-in, to be removed

- `api/client.ts` — `getStoredToken` / `setStoredToken` / `getStoredIdentity` /
  `setStoredIdentity` / `getLastCompany` / `setLastCompany`, over the localStorage keys
  `nexaerp.dev.bearerToken`, `nexaerp.dev.identity`, `nexaerp.dev.lastCompany`
- `features/auth/RequireAuth.tsx:9` — the route guard is `if (!getStoredToken())`. It becomes
  the OIDC session check plus the `session/me` gate.
- the header sign-in box, and every call to `/api/v1/dev/*`

## localStorage that must survive

"Remove localStorage" is too broad a sweep. Three uses are per-viewer UI convenience, have
nothing to do with identity, and are correct to keep:

- `api/purchase.ts:244,255,263` — recently used purchase documents
- `components/NavSection.tsx:7,16` — which navigation sections are open
- `features/purchase/RfqDetailPage.tsx:38,48` — remembered RFQ invitation selection

Only the three `nexaerp.dev.*` keys go.

## One rule found the hard way today

**Every `DateTimeOffset` sent to the API must be UTC, ending in `Z`.** Npgsql refuses a non-zero
offset against a `timestamp with time zone` column, and the purchase endpoints bind one
directly: `2026-09-30T18:00:00+05:30` is rejected with *"only offset 0 (UTC) is supported"*, and
the endpoint reports that server fault as a `400 VALIDATION_FAILED` whose `Detail` names no
field. A correct local timestamp, refused with a message that blames the user. Convert to UTC
before sending and back for display.
