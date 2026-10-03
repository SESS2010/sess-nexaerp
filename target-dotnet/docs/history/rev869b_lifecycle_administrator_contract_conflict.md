# REV869B lifecycle-administrator contract conflict

Status: **OPEN - provisioning and security-package application prohibited pending a separate design decision**

This record documents an unresolved contradiction. It does not select or implement a resolution.

## Target security-package predicate

Source: `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs`

```sql
IF EXISTS(SELECT 1 FROM pg_auth_members m JOIN pg_roles a ON a.oid=m.roleid JOIN pg_roles b ON b.oid=m.member WHERE (a.rolname LIKE 'nexa_rev869b_%' OR b.rolname LIKE 'nexa_rev869b_%') AND NOT (a.rolname='nexa_rev869b_security_owner' AND b.rolname='nexa_rev869b_lifecycle_administrator')) OR NOT EXISTS(SELECT 1 FROM pg_auth_members m JOIN pg_roles a ON a.oid=m.roleid JOIN pg_roles b ON b.oid=m.member WHERE a.rolname='nexa_rev869b_security_owner' AND b.rolname='nexa_rev869b_lifecycle_administrator') THEN RAISE EXCEPTION 'Target role membership mismatch'; END IF;
```

```sql
IF EXISTS(SELECT 1 FROM pg_roles r WHERE r.rolname IN ('nexa_rev869b_security_owner','nexa_rev869b_lifecycle_administrator','nexa_rev869b_app_runtime','nexa_rev869b_command_audit','nexa_rev869b_management_writer','nexa_rev869b_purge_worker','nexa_rev869b_purge_audit','nexa_rev869b_export_service','nexa_rev869b_target_verifier') AND (r.rolsuper OR r.rolcreatedb OR r.rolcreaterole OR r.rolreplication OR r.rolbypassrls OR r.rolinherit OR (r.rolname='nexa_rev869b_security_owner' AND r.rolcanlogin) OR (r.rolname<>'nexa_rev869b_security_owner' AND NOT r.rolcanlogin))) THEN RAISE EXCEPTION 'Target role capability mismatch'; END IF;
```

The target predicate therefore requires the lifecycle administrator to be a login with `NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS`, and permits only the security-owner membership.

## Control-plane preflight predicate

Source: `tools/rev869b-control-plane-preflight.sql`

```sql
('nexa_rev869b_lifecycle_administrator',true,true,true)),
```

```sql
role_capability_mismatch AS (
 SELECT 1 FROM pg_roles r WHERE r.rolname IN (SELECT name FROM expected_roles)
 AND (r.rolsuper OR r.rolreplication OR r.rolbypassrls OR r.rolinherit OR r.rolconnlimit<>-1 OR r.rolvaliduntil IS NOT NULL)),
unexpected_membership AS (
 SELECT 1 FROM pg_auth_members m JOIN pg_roles granted ON granted.oid=m.roleid
 JOIN pg_roles member ON member.oid=m.member
 WHERE (granted.rolname LIKE 'nexa_rev869b_%' OR member.rolname LIKE 'nexa_rev869b_%')
   AND NOT (granted.rolname='nexa_rev869b_control_plane_owner' AND member.rolname='nexa_rev869b_lifecycle_administrator'))
```

```sql
AND EXISTS(SELECT 1 FROM pg_auth_members m JOIN pg_roles granted ON granted.oid=m.roleid JOIN pg_roles member ON member.oid=m.member WHERE granted.rolname='nexa_rev869b_control_plane_owner' AND member.rolname='nexa_rev869b_lifecycle_administrator')
```

The preflight tuple requires `LOGIN CREATEDB CREATEROLE`; the capability predicate additionally requires `NOINHERIT NOSUPERUSER NOREPLICATION NOBYPASSRLS CONNECTION LIMIT -1 VALID UNTIL NULL`. It permits only the control-plane-owner membership.

## Conflict

One cluster role cannot simultaneously be both `CREATEDB CREATEROLE` and `NOCREATEDB NOCREATEROLE`. The exclusive membership predicates also cannot simultaneously permit only `nexa_rev869b_security_owner` and only `nexa_rev869b_control_plane_owner`.

Until a separately authorized architecture decision reconciles both predicates:

- do not provision the nine target roles;
- do not apply `20260824120000_Rev869BSecurityPackage`;
- keep ordinary ERP migration operations limited to `20260824032638_AdvanceInitialBaseline`.
