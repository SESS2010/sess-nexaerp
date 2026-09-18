# Required operational lookups and first-purchase prerequisites

Prepared correction (verification pending):

- PURCHASE_EXECUTIVE and PURCHASE_MANAGER: masters.vendors view.
- TECHNICAL_DIRECTOR: production.job-orders view.
- TECHNICAL_SUPPORT_MANAGER: purchase.vendor-quotations view and download.
  The seeded TSM verification authority had no quotation input reader; the real
  SESS-04 API witness reproduced the 403 before this correction.

Only the required read bits are added where needed. Existing write and commercial-value
permissions are preserved. Vendor bank metadata remains controlled by the
existing separate commercial-value check. Job-order reads remain company-scoped.

The migration records the exact before/after permission row, including an
already-satisfied site grant. Rollback refuses changed permissions and restores
the original row rather than removing a pre-existing site permission. Audit
receipts remain append-only. No production migration has been run.

Before the first quotation, configure and approve an effective GST tax rule that
matches the intended company, jurisdiction/HSN, supply type, supplier/place-of-
supply states, vendor registration type, currency and transaction date. A fresh
installation without a matching effective approved rule cannot price its first
quotation. This is an installation prerequisite, not permission to bypass tax
resolution or invent a rate.

Also configure each company's canonical-category Stores route and approved QC
policy before receiving/inspecting its corresponding items. Item master UOMs may
have precision 0 through 6; do not change them all to 6 to work around an item form.

### Input tax credit eligibility

Accounts sets ITC eligibility on the company/HSN/state tax rule when creating its governed version. Leave FULLY_RECOVERABLE selected for normal eligible production inputs. Use BLOCKED for a purchase category with no credit, or PARTIALLY_RECOVERABLE with the claimable percentage strictly between0 and100. TD/MD approval remains required. Do not classify every item or add a recoverability override to vendor bill lines.

Quotation and PO snapshots retain the approved eligibility and percentage captured when the rate was agreed. A later rule version does not change an existing PO. Customs duty remains capitalized independently. Do not re-enter GST already captured in the line snapshot as an additional bill charge.