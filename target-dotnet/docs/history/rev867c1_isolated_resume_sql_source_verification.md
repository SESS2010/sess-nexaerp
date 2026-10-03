# REV867C1 Isolated Resume SQL Source Verification

- Mode: source review / GenerateSqlOnly equivalent
- Expected host: localhost
- Expected port: 5432
- Expected database: sess_nexaerp_rev867c1_verify
- No password requested.
- No PostgreSQL connection attempted.
- No helper executed by Codex.
- No migration, update, cleanup, restore, create, drop, or data-modification operation executed by Codex.

## Session identity
```sql
select 'database=' || current_database()
union all select 'user=' || current_user
union all select 'server_addr=' || coalesce(inet_server_addr()::text, 'local_socket')
union all select 'server_port=' || inet_server_port()::text;
```

## Applied migration IDs
```sql
select "MigrationId"
from "public"."__EFMigrationsHistory"
order by "MigrationId";
```

## Nexa schema present
```sql
select case when exists (select 1 from pg_catalog.pg_namespace where nspname = 'nexa') then 'present' else 'absent' end;
```

## REV867C1 table evidence
```sql
select table_name
from information_schema.tables
where table_schema = 'nexa'
  and table_name in ('items','vendors','customers','warehouses','rack_bins','master_status_history','master_approval_history','audit_logs')
order by table_name;
```

## Persistent evidence counts before tests
```sql
select 'master_status_history=' || count(*) from nexa.master_status_history
union all select 'master_approval_history=' || count(*) from nexa.master_approval_history
union all select 'audit_logs=' || count(*) from nexa.audit_logs;
```