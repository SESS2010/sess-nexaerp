# Printable documents (A4)

Two print layouts, wired to the print endpoints; the preview still runs on mock data.

| Component | Print model (`types.ts`) | Status |
|---|---|---|
| `PurchaseOrderPrintView` (`PurchaseOrderPrint.tsx`) | `PurchaseOrderPrint` | wired |
| `MachineDeliveryChallanPrintView` (`MachineDeliveryChallanPrint.tsx`) | `MachineDeliveryChallanPrint` | wired |

## Preview

    npm run dev
    open http://localhost:5173/src/print/preview.html?doc=po&company=SESS_PVT_LTD

`doc` is `po` or `dc`; `company` is `SESS_PVT_LTD` or `SESS_PROPRIETORSHIP`.
The preview is a separate HTML entry: no login, no API calls, and it is not in
`npm run build` output, so the mock documents cannot ship. Ctrl+P prints only
the document (A4 portrait, footer with document number and "Page X of Y").

## Wiring

- `PrintDocumentPage.tsx` is the in-app route page (`/purchase/purchase-orders/:poNumber/print`,
  `/stores/machine-deliveries/:id/print`). It fetches the server's print view
  (`src/api/print.ts`, types in `src/types/print.ts`), maps it in `mapPrint.ts`
  onto the models in `types.ts`, and renders the layout with a `.no-print`
  toolbar. Every successful fetch is one audited print on the server.
- A live PO carries `amounts` (the API's snapshot figures, incl. cess and
  charges) and the layout prints them as they are; `gst.ts` is only the
  fallback for the mock preview (`resolvePurchaseOrder`).
- Addresses from the masters are one free-text string: it becomes `lines[0]`,
  city/pin stay empty and the layout drops that line.
- `print.css` hides `.sidebar`, `.topbar` and `.no-print` when a `.print-root`
  is on the page and un-clips `.app-shell` / `.main` / `.content` (the class
  names in `src/App.tsx`).
- `mockData.ts` is placeholder only (company names, GSTIN, PAN are invented).
