# Purchase dashboard — mock acceptance record

## MUST DO AT THE 28 SEPTEMBER REBASE — none of these is optional

These are the open risks; the code below was checked only against mocks.

1. **Company header.** Live calls do not yet send `X-NexaERP-Company`, because `api.get`
   cannot send headers. Add it with the shared-client rework, value from the login
   context, and replace the TEMPORARY `getCompanyCode()`.
2. **Test BOTH companies**, SESS_PVT_LTD and SESS_PROPRIETORSHIP, with real logins. Until
   the rebase the company is fixed, so Proprietorship data has never been shown.
3. **Real 401 redirect.** The live client clears the token and sends the user to /login;
   the mocks only show the section notice. Prove the real redirect with an expired session.
4. **Company switch with a slow response in flight.** Start a slow dashboard request,
   switch company before it answers, and prove the late response is discarded rather than
   shown under the new company. The guard (`useDashboardQuery`: request sequence plus
   CompanyCode check) is written, and the CompanyCode check was seen working with
   `company-mismatch`, but it has not been proven against a real delay and a real switch.

## Mock server safety

- `vite.dashboards-mock.config.ts` is used only by an explicit `--config`; `npm run dev`,
  `build`, `build:api` and `preview` use `vite.config.ts` and never load it.
- It refuses `build` and `preview` outright (checked: `vite build --config …` fails with
  "refuses to build or preview" and writes no output; `vite preview --config …` exits).
  Its session plugin is `apply: 'serve'` only.
- The live bundle contains none of `__mock-signin`, `dashboard-mock-session`,
  `MOCK Technical Director`, `MOCK_NO_BACKEND` (checked 23 Sep).
- It listens on `::1:5180` only; `http://192.168.68.130:5180` is refused from this PC.
  Stop it at the end of each working day.

Checked on 23 September 2026 against `dashboard-frontend-contract.md` (22 Sep) and its six
synthetic bodies. There is no frontend test framework, so this record is the evidence.
Every check was done in a browser on the dashboard mock server, with no backend:

```
npx vite --config vite.dashboards-mock.config.ts      # localhost:5180 only
open http://localhost:5180/__mock-signin               # stores a meaningless token
http://localhost:5180/dashboards/purchase?mock=<variant>&session=<profile>
```

Each observation was read from the rendered page (text and `data-dashboard-state`
attributes), not from the source code.

## Build

| Check | Result |
|---|---|
| `npm run build` (tsc -b + vite build, live mode) | Pass |
| Live bundle contains mock data? Searched for `TRIAL Alpine`, `mockDashboardGet`, `mock-trace-403`, `dashboard-mocks`, `SYNTHETIC MOCK` | None found: mocks are not shipped |
| `fetch(` in any dashboard file | None: all calls go through `api.get` in `src/api/dashboards.ts` |
| `client.ts` changed | No |
| Company code | Appears only in `getCompanyCode()` (TEMPORARY, fixed `SESS_PVT_LTD`) |

## States, by variant (session = td unless stated)

| Variant | What the page showed | Pass |
|---|---|---|
| `reference` | All four sections loaded from the six bodies unmodified; figures match the contract (e.g. PR ₹4,999.99, open PO ₹4,720.00, GRNI ₹1,09,000.01, FY spending ₹1,28,632.01). Basis text beside each money summary. | ✅ |
| `slow` | At 1 s all four sections showed **Loading**; at 5 s all loaded. | ✅ |
| `empty` | Each section showed its own **Empty** notice ("No purchase documents are waiting", "No open purchase orders", "No outstanding obligations", "No accepted purchase bills"). No `₹0.00` anywhere; empty amounts show a dashed "—" marker. | ✅ |
| `withheld` | Denied card "Comparisons awaiting…" showed **Permission denied** + "Withheld", plus a section notice "1 queue is not available to you — withheld, not zero". PR approval amount showed "Withheld"; row value showed "Withheld" (tooltip "Not zero: this value is withheld from your role."). No `₹0`. | ✅ |
| `incomplete` | Open orders showed **Incomplete source**: "overall totals are unknown", listing PO-26-27-000041 (RECEIPT_QUANTITY_INCONSISTENT). Open POs, oldest age, overdue and amounts all showed "Unknown". Reliable row still listed. | ✅ |
| `unconfirmed-delivery` | **Incomplete source**: "Overdue totals are unknown: some delivery dates are not confirmed". Overdue POs and overdue payable "Unknown"; line badge "Date not confirmed", "No confirmed commitment". | ✅ |
| `multi-currency` | INR, USD, EUR shown on separate lines everywhere, never added (workload $1,250.50; open orders INR and USD rows; GRNI ₹ and €; spending ₹ and $). USD line OVERDUE "5 days late". REVERSED events labelled "Reversed (subtracts)", negative amounts in red. | ✅ |
| `denied-403` | All four sections: **Permission denied** with each endpoint's exact contract Detail text and TraceId `mock-trace-403`. No figures. | ✅ |
| `integrity-409` | Obligations: red "Figures do not reconcile — Report source needs administrator action"; "These balances are inconsistent, so no figures are shown"; contract Detail; TraceId `mock-trace-409`. Zero tiles and no ₹ amounts in the section. Other three sections loaded normally. | ✅ |
| `unauthenticated-401` | All four: "Not signed in", **Sign in again** link to /login, TraceId. | ✅ |
| `invalid-400` | "The filter was not accepted", Validation failed, TraceId, Try again. | ✅ |
| `server-500` | "Could not load this section — The server could not produce this report", TraceId, Try again. No exception text. | ✅ |
| `company-mismatch` | Bodies claiming SESS_PROPRIETORSHIP were discarded in all four sections: "Response discarded: it belongs to a different company". No rows, no ₹. | ✅ |

## Permissions (session profiles)

| Profile | What the page showed | Pass |
|---|---|---|
| `td` | All four sections; Dashboards › Purchase in the navigation. | ✅ |
| `purchase-exec` (PURCHASE_EXECUTIVE alone) | No sections, no Dashboards navigation; page says "not available to your role". | ✅ |
| `partial` (workload + spending only) | Only those two sections. PR number not linked (no requisitions permission); vendor names plain text (no vendor master permission); PO numbers linked. | ✅ |

## Detail filters narrow rows only

| Action | Result | Pass |
|---|---|---|
| Open orders › "only this vendor" (TRIAL-VEN-002, multi-currency) | Rows 2 → 1. Overview figures and currency table identical before/after. Note: "Detail filtered … The overview above is not filtered." | ✅ |
| Workload › click "POs approved but not issued" tile | All 7 tiles still shown; detail note names the queue; rows narrowed. | ✅ |
| Spending › click Aug 2026 in the monthly trend | Detail range 1–31 Aug 2026, "No bill events match this selection"; FY card unchanged. | ✅ |

## Fixed during checking

- Spending rows used a key that repeats when one bill line is accepted, reversed and accepted
  again on the same date (TRIAL-BILL-TD in the reference body). Now keyed by position.
- 409 text repeated "Ask the administrator"; reworded.
- "1 docs", "1 POs · 1 bills" → correct singular.
- Large counts were shrunk by the global `.mono` rule; `.mono` now sits on an inner span.

## Smaller known limits

- The four rebase items are at the top of this record.
- `ApiError` drops Type, Title, Errors and AdministratorActionRequired (see dashboards.ts).
- In `multi-currency` the workload "POs approved" tile says 1 but the mock body has no
  matching row — a limitation of the synthetic variant, not the page.
