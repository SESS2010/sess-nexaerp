# Item 15 frontend contract

These are backend contracts. Report screens are not implemented in this repository. Item 15 remains partial: billed-not-received and delivered-machine ancestry are absent, and FIFO source reconciliation is pending.

## Discovery and requests

Use the configured company-selection header and the production authentication flow documented in item-16-frontend-contract.md.

- GET /api/v1/reports/ returns the permitted catalogue only.
- GET /api/v1/reports/{key} returns a page and its totals.
- GET /api/v1/reports/{key}/excel returns the XLSX attachment for the selected company, dates and optional selection.

Catalogue fields are Key, Title, UsesPeriod, ContainsCommercialValues, Coverage and CurrentOnly. Keys are stock-balance, movement-roll-forward, grni, vendor-purchases, purchase-register, pending-approvals, engineer-custody and the restricted fifo-valuation prototype. Do not invent entries for the two absent reports.

Query parameters are fromDate, toDate (ISO dates), mode (summary or details), selection (URL-encoded JSON), metric, page and pageSize. Page numbering starts at 1; pageSize is 1–1000. Excel accepts fromDate, toDate and selection and exports the complete selection. It is not limited to the current page. CurrentOnly reports accept only the configured company's current calendar day.

On company switch, discard the previous selection and page, reload the permitted catalogue and request the selected company's report. A role-assignment ID from one company is not valid in another.

## Rendering and drill-through

The outer response uses the API's existing PascalCase convention:
Key, Title, CompanyCode, GeneratedAt, FromDate, ToDate, Mode, Page, PageSize, TotalRows, TotalSourceRows, Columns, Rows, Totals, Coverage and TimeZone.

Columns contain Key, Label, Type and optional Metric. Source objects inside Rows and Totals use explicit SQL JSON keys, such as itemCode, uom and quantity. Render row[column.Key]; do not change the key's case. Type is text, number or date. Unlike units and currencies must remain separate.

A summary row or total supplies a lowercase group object. Send that object unchanged as selection when opening its source rows, with mode=details. For a specific measure use the column's Metric. Treat the selection as opaque; constructing it from displayed names can lose IDs or null dimensions. TotalRows describes the selected mode; TotalSourceRows describes its contributing source rows.

Engineer-custody totals contain engineerId, engineerCode, engineerName and uom. Display those identities with every custody total. Do not add a grand custody quantity.

Coverage explains source limitations and live-versus-historical semantics. Show it where users interpret the report. TimeZone identifies date cutoffs and age calculations; GeneratedAt is an instant, and Excel labels its generation timestamp in UTC. Reporting.DefaultTimeZone and Reporting.CompanyTimeZones are deployment settings, not user query parameters.

## Refusals and files

Standard errors contain Type, Title, Status, Code, Detail, TraceId and Errors. Report source failures additionally contain AdministratorActionRequired=true. That field is omitted for ordinary report validation/permission errors.

- 403 REPORT_ACCESS_DENIED: report is not permitted; remove stale catalogue access and show the refusal.
- 400 REPORT_REQUEST_INVALID: invalid report options or selection.
- 409 FIFO_RETURN_CREDITS_REQUIRED or FIFO_SOURCE_INCONSISTENT: do not display a zero valuation; show Detail and the administrator action.
- 404: unknown report key.
- Authentication middleware can instead return MFA_REQUIRED or EMPLOYEE_ACCESS_NOT_CONFIGURED; handle those according to the Item 16 contract.

Excel MIME type is application/vnd.openxmlformats-officedocument.spreadsheetml.sheet. Worksheets include About, Totals, Summary and Details, with numbered continuations. Numeric totals/summary cells link to underlying rows. The measured maximum-size roll-forward file is 479 MB; desktop Excel opening has not been witnessed. The backend retains compressed download bytes in memory.

Verification and measured limits: item-15-reporting-progress.md. Unresolved source decisions: item-15-source-prerequisites.md.
