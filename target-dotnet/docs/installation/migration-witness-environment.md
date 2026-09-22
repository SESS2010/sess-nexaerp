# Migration witness environment

Before any migration-chain witness, record the following in its evidence **before running it**. Do not describe a mismatch as reproduced.

| Field condition | Required record |
|---|---|
| Database name | Exact restored name and cluster/port; system and live databases remain untouched |
| Starting migration | Actual history count and last entry, not a migration-file count |
| Role history | Matching globals, retained REV869B installation state, all nine role identities, ownership and memberships |
| Operating system | OS and PostgreSQL/server/client versions |
| Source line endings | Actual source bytes, Git checkout attributes and build configuration |
| Executing identity | Authenticated migration login, effective owner role and verified non-superuser status |
| Evidence boundary | Each field condition not reproduced; synthetic fixtures are not restored field backups |

Use the oldest real deployed starting state. A fresh install, a renamed database, a later backup, or invented legacy roles cannot replace that proof. Keep globals and connection secrets private.

## 16 September: CRLF correction regression

This is a synthetic migration regression, **not field-upgrade clearance**.

- Operating system: Windows 10 Pro 10.0.19045 x64, PostgreSQL 17.10 disposable instances. The field machine's exact OS/PostgreSQL versions are not yet known.
- Source: first LF-enforced migration source, then a deliberate CRLF conversion of every migration C# source before a separate Release rebuild. Restore LF before final routine suites.
- Database: existing lifecycle tests create disposable `advance_parser` databases. That does **not** reproduce the field name `sess_nexa_erp`.
- Starting state: tests generate fresh predecessors rather than restoring the field's 65-entry database.
- Roles: tests use their declared synthetic provisioning fixtures; they do **not** reproduce the customer's historical role grants.
- Purpose: prove the four corrected patch generators survive CRLF compilation and retain their body/authority/trigger drift checks. The whole-assembly regression must reject embedded CRLF in normal deliverables.

## Pending actual field witness

Restore the privately supplied `sess_nexa_erp-pre75-2026-09-16.dump` and matching globals into an owned isolated PostgreSQL cluster initialized as `postgres`. Inspect the restored migration history to establish the actual starting entry. Verify the reported 2.98 MB backup, 229 tables, `WasInstalled=true`, nine REV869B roles and 14 matching globals roles before proceeding; do not assume the earlier 65-entry fixture is this backup. Use the exact database name `sess_nexa_erp`, ordinary migration login with `SET ROLE nexa_erp_owner`, and the corrected Installer preparation and verification.

The files' local paths have not yet been supplied. This witness has **not run**. Record OS/PostgreSQL version differences against the frontend developer's environment when the handover identifies them; do not infer a match.

The current ordinary-principal migration-75 regressions use a synthetic `sess_nexa_erp` database. They reproduce the name and ordinary migration identity, but not the supplied backup contents or historical role state. Other workflow fixtures use `advance_parser`. Neither constitutes the requested field witness.
