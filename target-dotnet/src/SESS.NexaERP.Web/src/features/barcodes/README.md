# Barcode scanning and labels

`BarcodeScanField` captures scanner text, queues read-only lookups and focuses a quantity control. Opt a quantity field into wedge capture with `data-barcode-quantity`; its selector is supplied by the caller. Manual quantity entry requires turning Scan mode off. No scan calls a command or submits a form.

- `barcodeScan.ts`: text matching, ambiguity refusal, quantity increments and target/owner lifecycle.
- `quantityScanCapture.ts`: preserves leading zeros and consumes scanner suffixes on quantities and focused action buttons.
- `barcodeSession.ts`: identity/company/authority cache key. Callers also include document/version and relevant purpose/customer context.
- `scanReadCache.ts`: bounded ten-second memory cache for GETs only. Never cache command responses here.
- `issueScanDraft.ts` / `returnScanDraft.ts`: aggregate base-unit caps, physical-serial uniqueness and return accounting.
- `grnLotCode.ts`: company-qualified durable lot code, mirrored by Application `GrnLotBarcode.cs`.
- `barcodeLabels.ts` / `BarcodeLabelDialog.tsx`: local Code128 encoding and 50x25/A4 layouts. Require current View + Print permission at the caller and again at click time. GRN stock-lot labels require a finalized normal receipt.

Bindings are in `src/api/barcodes.ts`; API endpoints are named `*BarcodeEndpoints.cs`. They use existing page/action grants and active company/warehouse/custody scopes. The current feature adds no schema/grant migration; barcode columns already store text. The item-workbook reader refuses numeric barcode cells.

Tests include actual GRN, MIR, issue, return and stock-check forms, scanner owner transitions, GET transports, text/quantity handling, print gates and label geometry. Disposable PostgreSQL purchase-cycle tests verify scope refusals. Browser rendering and physical printer/scanner acceptance remain separate commissioning witnesses.

Do not add auto-submit, silent first-match selection, numeric barcode coercion, broader authorization, physical-count posting or unrelated UI changes to this module without TD approval.
JsBarcode 3.12.3 is pinned and bundled locally. Its MIT notice is copied to public/licenses/jsbarcode.txt and shipped with web assets; printing uses no CDN.
