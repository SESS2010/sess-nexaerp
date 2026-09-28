# Printable documents (A4)

Two print layouts, built against mock data; not yet wired to the API.

| Component | Print model (`types.ts`) | Status |
|---|---|---|
| `PurchaseOrderPrintView` (`PurchaseOrderPrint.tsx`) | `PurchaseOrderPrint` | layout done, mock data |
| `MachineDeliveryChallanPrintView` (`MachineDeliveryChallanPrint.tsx`) | `MachineDeliveryChallanPrint` | layout done, mock data |

## Preview

    npm run dev
    open http://localhost:5173/src/print/preview.html?doc=po&company=SESS_PVT_LTD

`doc` is `po` or `dc`; `company` is `SESS_PVT_LTD` or `SESS_PROPRIETORSHIP`.
The preview is a separate HTML entry: no login, no API calls, and it is not in
`npm run build` output, so the mock documents cannot ship. Ctrl+P prints only
the document (A4 portrait, footer with document number and "Page X of Y").

## Wiring (to do)

- Map the API and masters onto the print models; the layouts take no API types.
  `PurchaseOrderDetail` does not yet carry vendor address/GSTIN, HSN, UOM or
  per-line GST rate — those come from the vendor and item masters or need
  adding to the contract.
- `gst.ts` computes line amounts (CGST+SGST when the vendor state equals the
  delivery state, else IGST; per-line rounding to paise; grand total rounded to
  the rupee). Once the API returns computed amounts, print those instead.
- `MachineDeliveryView` maps directly for DC number, dates, nature, purpose,
  job order, machine serial/model and `Signature` (`DeliveredAt` → printed in IST).
  Transport / e-way bill fields and the items list are not in the contract yet.
- Inside the app, render the view on a route or detail page and call
  `window.print()`. `print.css` hides `.sidebar`, `.topbar` and `.no-print`
  when a `.print-root` is on the page and un-clips the app shell.
- `mockData.ts` is placeholder only (company names, GSTIN, PAN are invented).
