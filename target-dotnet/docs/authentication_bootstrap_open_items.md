# Authentication bootstrap open items

## Deferred role taxonomy cleanup

Management has explicitly deferred role consolidation beyond canonical uppercase
casing. The database currently has 43 roles; 23 have no employee assignment.
Known overlaps include:

- `ADMIN`, `MD`, and `MANAGING_DIRECTOR`
- `DCC` and `DOCUMENT_CONTROLLER`
- `SOFTWARE_DEVELOPER` and `SOFTWARE_ENGINEER`

Do not merge, delete, or remap these roles as part of authentication bootstrap.
A later change must inventory permission differences and external references,
choose canonical semantics, and preserve all relationships by `RoleId`.

## Development-only database guard exemption

`DatabaseSecurity:AllowDevelopmentSuperuser` belongs to the runtime startup
guard checkpoint. It must be accepted only in the Development environment,
emit a prominent warning on every startup, and cause non-Development startup
to fail if the setting is present.
