# REV867C1 Main Development Database Read-Only Diagnostic SQL Source Verification

- Generated offline from `diagnose-rev867c1-main-db-readonly-secure.ps1 -GenerateSqlOnly`.
- No password requested.
- No PostgreSQL connection attempted.
- No migration/database modification command executed.

```text

== REV867C1 main-db read-only diagnostic prechecks ==
REV867C1 main-db generated diagnostic SQL report: C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet\local-evidence\rev867c1\rev867c1_main_db_readonly_diagnostic_20260808_225338.md
-- Session identity
select 'database=' || current_database()
union all select 'user=' || current_user
union all select 'server_addr=' || coalesce(inet_server_addr()::text, 'local_socket')
union all select 'server_port=' || inet_server_port()::text;

-- Schemas
select nspname
from pg_catalog.pg_namespace
where nspname not like 'pg_toast%'
order by nspname;

-- Exact EF history lookup
select n.nspname || chr(31) || c.relname
from pg_catalog.pg_class c
join pg_catalog.pg_namespace n on n.oid = c.relnamespace
where c.relname = '__EFMigrationsHistory'
order by n.nspname, c.relname;

-- Public EF history exists
select case when exists (
    select 1
    from pg_catalog.pg_class c
    join pg_catalog.pg_namespace n on n.oid = c.relnamespace
    where n.nspname = 'public' and c.relname = '__EFMigrationsHistory'
) then 'found' else 'not_found' end;

-- Migration IDs
select "MigrationId"
from "public"."__EFMigrationsHistory"
order by "MigrationId";

-- REV867C1 migration present
select case when exists (
    select 1
    from "public"."__EFMigrationsHistory"
    where "MigrationId" = '20260808160435_Rev867C1Corrections'
) then 'present' else 'absent' end;

```
