# REV867C1 Migration-Target Mismatch Incident

## Finding
The completed read-only diagnostic for `sess_nexaerp_rev867c1_verify` proved that the verification database is empty and was not the database that Entity Framework reported as up to date.

Evidence from `rev867c1_readonly_diagnostic_20260808_223736.md`:

- Connected database: `sess_nexaerp_rev867c1_verify`
- PostgreSQL user: `postgres`
- Server: `localhost:5432`
- Schemas present: `information_schema`, `pg_catalog`, `public`
- `nexa` schema: absent
- Migration/history relations: none
- `public.__EFMigrationsHistory`: not found
- `MigrationId` rows: unavailable

Therefore, the earlier EF output `No migrations were applied. The database is already up to date.` did not prove that `sess_nexaerp_rev867c1_verify` contained REV867C1. It likely came from another target database.

## Likely Cause
Git history shows that the design-time DbContext factory previously hard-coded the main development database:

```csharp
.UseNpgsql("Host=localhost;Database=sess_nexaerp;Username=postgres")
```

Because `dotnet ef` uses the design-time factory, REV867C1 management runs before the secure factory fix could have targeted `sess_nexaerp` even when the helper supplied `ConnectionStrings__NexaErp` for `sess_nexaerp_rev867c1_verify`.

## Factory Timeline From Git

| Commit | Purpose | Design-time factory behavior |
| --- | --- | --- |
| `efd3a31` | REV867C1 master verification corrections | Hard-coded `Database=sess_nexaerp`; could silently target main development DB. |
| `844cb85` | REV867C1 verifier migration-history resume fix | Still hard-coded `Database=sess_nexaerp`; helper environment connection could be bypassed by EF design-time factory. |
| `74cedef` | Secure design-time database connection | Removed hard-coded database; requires `ConnectionStrings__NexaErp`; fails closed if absent. |
| `8256d02` | Read-only diagnostic SQL-generation fix | Current diagnostic helper is read-only and does not run EF. |

## Current Source Safety Finding
Current `NexaErpDesignTimeDbContextFactory`:

- has no hard-coded database fallback;
- reads only `ConnectionStrings__NexaErp` from the process environment;
- throws `InvalidOperationException` if the value is absent;
- cannot silently target `sess_nexaerp` unless the approved environment connection explicitly points there.

## Required Next Evidence
A separate read-only main-development-database diagnostic helper has been prepared for `localhost:5432 / sess_nexaerp`. It must be executed manually by management only after reviewing its generated SQL. It does not read business records and only checks session identity, schemas, EF history relation, migration IDs, and whether `20260808160435_Rev867C1Corrections` is present.

## Restrictions Maintained

- No database was queried or modified by Codex for this incident note.
- No migration, rollback, restore, create, drop, truncate, insert, update, or delete operation is included in the read-only helper.
- REV868 remains not started.
- Live REV861 remains untouched.
