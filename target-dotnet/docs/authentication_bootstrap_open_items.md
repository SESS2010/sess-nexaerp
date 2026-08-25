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
guard checkpoint. It is compiled out of Release eligibility: a Release startup
fails if the setting is present, even when its value is `false`. A Debug startup
may enable it only in the Development environment and emits critical warnings
on every exempted startup.

## Acting-role selection for workflow authority

Page permissions use the union of all effective database roles. Workflow and
approval authority remains deliberately scalar until an explicit acting-role
request contract, validation rule, audit field, and user experience are agreed.
Multi-role identities therefore have no implicit scalar acting role and fail
closed in legacy workflow comparisons.

## Reproducible deployment toolchain

Tightening `global.json` and pinning the customer build/runtime image are
deferred to deployment work. Release CI should eventually use an exact SDK and
runtime/container digest, with security patch upgrades performed deliberately
after verification rather than through `latestFeature` roll-forward.
