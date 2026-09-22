# Item 15 - invoice-before-receipt source and report 5

Status: implementation and targeted Release/Debug witnesses pass. Full routine Release passed 836/836, zero failed/skipped, in 23m42s (1422.332 seconds process wall time).

Written basis: Pending_Work_Specification item 15; Answers_To_Open_Questions precedence; the frozen schema's company scope and immutable provenance; Vendor_Bill_And_Costing's one-GRN accepted bill and exact quantity/value rules. The latest user instruction authorizes implementation and requires actual receipt, reversal, permission, Excel and drill-through proof.

## Behavior

Accounts records documentary supplier invoices against issued POs. Retained PDF/JPEG/PNG bytes have a server-computed SHA-256, filename, MIME type, size, uploader and time. Immutable lines retain item, unit, quantity, price and original-currency payable value. Recording does not accept a vendor bill or authorize payment.

Controlled recording, cancellation and accepted-bill linkage use the ordinary command ledger and exact committed-response replay. Accounts Assistant SUPPORT may create documentary evidence under the existing role-authority rule; cancellation and accepted-bill linkage require Accounts Manager authority. Cancellation keeps the evidence. A linked active accepted bill prevents cancellation.

Receipt matching follows company/vendor/PO family/stable purchase requirement/item/unit/currency/invoice number/date. Finalized normal GRNs append matches. Reversals append negative entries referencing the original matches. Original invoice and match rows are never rewritten.

The company-scoped, permission-gated report groups by vendor/item/unit/currency. Outstanding quantity and proportional documentary value drill to invoice lines, receipt history and evidence. The fixed controlled SQL performs the report read and export audit in one statement. Existing Excel output is reused. Installer provisioning keeps all five supplier-invoice tables private from generic runtime grants.

## Existing receipt limitation

The existing effective-GRN guard refuses more than one active finalized GRN for the same company/vendor-bill number. The accepted-bill model also requires one GRN, identical bill number/date and billed quantities equal to receipt quantities. This report does not relax either rule.

Consequently, an invoice can be recorded before receipt and reported as partially outstanding, but receiving that same invoice across several simultaneously effective GRNs is unsupported. The acceptance witness must prove this refusal, then use reversal and a corrected replacement GRN. It must not silently bypass the guard to make a split-delivery test pass. This limitation is separate from reporting the outstanding documentary amount.

The first repaired workflow reached this restriction and exposed HTTP 500 for the deferred business refusal. A narrow service correction now translates that exact known refusal to 409 Conflict. Unexpected database errors keep their existing handling; no production migration guard was weakened.

## Frontend contract changes

New governed page key and UI route: accounts.supplier-invoices and /accounts/supplier-invoices. New invoice API group: /api/v1/accounts/supplier-invoices. It supports POST recording, GET by id, GET evidence, POST cancel and POST link-accepted-bill. GET /purchase-order-options accepts search, page and pageSize and returns the standard TotalCount/PageNumber/PageSize/Items envelope. Its issued-PO and line options remain available for fully paid POs and require Accounts view plus commercial-value permission. The intake page has independent role grants derived from the current Accounts vendor-bill grants: Accounts Assistant/Manager entry, upload and commercial read/download; only Accounts Manager cancellation and linkage. Existing vendor-bill page grants are unchanged. Employee-specific grants are not copied. Use its PO/line IDs for recording, the invoice GET's Version for cancellation/linkage, and existing Accounts vendor-bill GETs for accepted bill IDs.

Report key and route: billed-not-received, through the existing reports GET and Excel endpoints. Existing envelope names remain unchanged. The known duplicate effective GRN refusal changes from HTTP 500 to HTTP 409.

## Verification

Targeted Release 3/3 and Debug 3/3 passed, zero failed/skipped, each 6m56s. Both builds passed with zero warnings/errors. Evidence: report5-page-permissions-release.trx and report5-page-permissions-debug.trx under local-evidence/overnight-20260914.

The tests cover invoice lifecycle with the real page-permission service, private migration/empty rollback/reapply, and company report access/export. A partial receipt leaves 0.6 outstanding with documentary value 3540, verified in report detail and Excel. The second effective GRN is refused with HTTP 409 and no new movements or FIFO layers. Reversal and replacement produce exactly three immutable match entries (0.4, -0.4, 1.0), final received quantity 1 and one accepted LOW bill link. Replay does not duplicate evidence. Stores access is refused; Accounts Assistant SUPPORT can record documentary evidence but cannot cancel it.

Future FIFO consumption previously failed to exclude reversed receipts. Migration 20260914100000 excludes them and refuses receipt reversal while outstanding FIFO consumption or an active accepted bill remains. Report 3 already excludes reversed receipts. The witness excludes the reversed layer, refuses excess costing, permits only eligible layers and refuses reversal of a consumed receipt. This boundary uses restricted costing-function inputs in an isolated clone, not completed physical issues. See item-15-fifo-reversed-receipts.md.

The separate exact fitment -> reversal -> return test also passes in Release and Debug: machine value 1419.60 becomes zero on reversal and remains zero after the physical return, without a re-fit. Original costing and BOM history remains unchanged; the return appends FIFO restorations. See item-15-fifo-restoration.md.

Expected installation effect: two new page definitions, derived role grants and five initially empty private invoice tables. No business invoices or receipt rows are invented. Existing financial and stock history is unchanged. Full routine Release evidence: report5-routine-release.trx; all 836 tests passed, zero failed/skipped, in 23m42s. Default Release and Debug builds after retired-project removal passed with zero warnings/errors (27.72s and 54.25s). Debug behavioral evidence is the focused runs above; no full Debug pass is claimed.

The renamed disposable copy of the supplied 78-state backup had already reached 100. These two migrations then applied as nexa_erp_migration acting as nexa_erp_owner to reach 102; all five invoice tables remained empty and both page definitions existed. Installer provision returned RECONCILED and status VERIFIED, credentials unchanged. This proves the new migrations on that restored data, not the protected sess_nexa_erp name: its earlier CommandReceiptReplay refusal remains unresolved. Evidence: report5-copy-after.txt and report5-copy-principals-*.log. No live database was touched.
