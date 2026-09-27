# Requirements from the Technical Director

- `SESS-NexaERP-Master-Requirements-v2.md` (27 September 2026) is the **current master
  requirements** document: a shared foundation plus Track A Stores + Purchase, Track B Service +
  Production, Track C Expenses + cash flow, and Track D Sales CRM.
- It **supersedes** `SESS-NexaERP-Tracking-Requirements-v1.md` (26 September 2026). v1 is kept
  unchanged, as the dated record.
- `inputs/` holds the TD's earlier requirement documents, converted from `.docx` to Markdown for
  traceability.
  - Each file starts with a line naming the original file and its SHA-256.
  - The wording is unchanged; only the layout is simplified.
- Reference apps named by the master are under `docs/reference/<app>/`: dc-tracker, service-hub,
  expense-erp, sales-crm. They hold code and guides only; data, registers, e-mail lists and
  certificates are left out.
- The design derived from these documents (the SRS per track, the sprint plan and the open
  questions) is kept outside the repository until the TD signs it off.
- **Redaction:** in both requirements files, the two default passwords of the DC Tracker, quoted
  in "Known weaknesses", are replaced by `<REDACTED>`. Nothing else was changed.
  - Original SHA-256 of `SESS-NexaERP-Tracking-Requirements-v1.md`: `BA7530072B93B137F3127FED46C691827595B47EFE4CB7BDBE5471A27E87001D`.
  - Original SHA-256 of `SESS-NexaERP-Master-Requirements-v2.md`: `08C481673508202CB03F5887DFD45FE5F576AC5042D0DCBD627E2AF3438E1D9C`.
