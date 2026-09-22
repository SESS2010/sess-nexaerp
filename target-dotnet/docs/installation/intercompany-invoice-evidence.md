# Intercompany invoice evidence

Status: invoice-evidence implementation validated in the frozen combined candidate. A1 remains incomplete until dispatch, destination acceptance and both ledger effects are proven.

Accounts Manager can retain a GST invoice supplied by Accounts against a published normal buyer PO. The record preserves the published commercial order and invoice file with its SHA-256 digest. Seller and buyer Accounts can retrieve the same retained evidence within their selected company. The seller view contains the existing shared commercial order, without the buyer’s private PO, requisition or quotation IDs.

The order snapshot records what was agreed. Invoice document registration alone does not establish invoice-line reconciliation, a stock dispatch, destination acceptance, a payable or seller cost relief. Those remain required A1 integrations. Recording an invoice creates no stock movements.

## API

All routes use `/api/v1/accounts/intercompany-invoices`, require a resolved company/employee and the new `accounts.intercompany-invoices` page permissions.

- `POST /`: `CorrelationId`, `InvoiceNumber`, `InvoiceDate`, `Evidence` (`FileName`, `ContentType`, `Content` bytes), `IdempotencyKey`.
- `GET /for-purchase/{correlationId}`: retained invoices for that published order visible to the selected company.
- `GET /{id}`: invoice identity, participant companies, order snapshot and retained-file metadata.
- `GET /{id}/evidence`: the retained file, checked against its digest before download.

The existing intercompany purchase list supplies the correlation ID. Create requires create, upload and commercial-value permissions. Reads require view and commercial-value permissions; downloads require download and commercial-value permissions. Accounts Manager follows the existing intercompany commercial-reader authority.

Files are limited to 5 MB and checked as PDF, JPEG or PNG. Invoice numbers are unique per seller financial year, using the SESS April–March year. Repeating the same command returns the original result. Reusing its key with different evidence or header data is refused. Retained evidence cannot be updated or deleted through this workflow.

New invoice registration requires the current issued PO version, an approved effective route and unchanged approved route identities. Revoking a route refuses new registration while preserving existing reads and exact command replay.

## Persistence and validation

Migration `20260919100000_IntercompanyInvoiceEvidence` adds one page definition, one Accounts Manager role permission and zero business rows. Each successful recording adds one immutable invoice-evidence row plus the existing command, receipt and audit evidence. Private table access is revoked from runtime/bootstrap/migration principals; only the controlled record/read/download functions are exposed to runtime. Installer provision and status use the same versioned ACL contract. Rollback refuses retained invoice evidence.

Passing routine witnesses cover the real issued-PO HTTP flow, role/company boundaries, evidence download and integrity, repeated/changed commands, duplicate numbers, invalid files, immutable history, route revocation, direct-table refusal, ordinary-principal migration Up/Down/reapply and independent table/function ACL drift. Both builds passed with zero warnings/errors. Full Debug 881/881 and Release 878/878 passed with no failures/skips; focused 3/3 passed in each. Retained block-a-v2 TRX and 872 source hashes were verified. These counts belong to the shared frozen candidate, which also includes the separately published routine fitment/reversal/return witness; they are not a standalone clean-checkout invoice-only suite. The complete A1 sale is still unaccepted.
