\set ON_ERROR_STOP on
-- Read-only prerequisite verification. External IaC owns every cluster role and database.
WITH expected_roles(name,can_login,can_createdb,can_createrole) AS (VALUES
 ('nexa_rev869b_control_plane_owner',false,false,false),
 ('nexa_rev869b_lifecycle_api',true,false,false),
 ('nexa_rev869b_lifecycle_audit',true,false,false),
 ('nexa_rev869b_recovery_executor',true,false,false),
 ('nexa_rev869b_control_plane_verifier',true,false,false),
 ('nexa_rev869b_management_writer',true,false,false),
 ('nexa_rev869b_security_owner',false,false,false),
 ('nexa_rev869b_app_runtime',true,false,false),
 ('nexa_rev869b_command_audit',true,false,false),
 ('nexa_rev869b_purge_worker',true,false,false),
 ('nexa_rev869b_purge_audit',true,false,false),
 ('nexa_rev869b_export_service',true,false,false),
 ('nexa_rev869b_target_verifier',true,false,false),
 ('nexa_rev869b_lifecycle_administrator',true,true,true)),
actual_roles AS (
 SELECT r.rolname::text name,r.rolcanlogin can_login,r.rolcreatedb can_createdb,r.rolcreaterole can_createrole
 FROM pg_roles r WHERE r.rolname LIKE 'nexa_rev869b_%'),
role_mismatch AS (
 (SELECT * FROM expected_roles EXCEPT SELECT * FROM actual_roles)
 UNION ALL (SELECT * FROM actual_roles EXCEPT SELECT * FROM expected_roles)),
role_capability_mismatch AS (
 SELECT 1 FROM pg_roles r WHERE r.rolname IN (SELECT name FROM expected_roles)
 AND (r.rolsuper OR r.rolreplication OR r.rolbypassrls OR r.rolinherit OR r.rolconnlimit<>-1 OR r.rolvaliduntil IS NOT NULL)),
unexpected_membership AS (
 SELECT 1 FROM pg_auth_members m JOIN pg_roles granted ON granted.oid=m.roleid
 JOIN pg_roles member ON member.oid=m.member
 WHERE (granted.rolname LIKE 'nexa_rev869b_%' OR member.rolname LIKE 'nexa_rev869b_%')
   AND NOT (granted.rolname='nexa_rev869b_control_plane_owner' AND member.rolname='nexa_rev869b_lifecycle_administrator'))
SELECT CASE WHEN current_database()='postgres'
 AND pg_control_system().system_identifier::text=:'expected_system_identifier'
 AND coalesce(inet_server_addr()::text,'local')=:'expected_server_address'
 AND inet_server_port()=:'expected_server_port'::integer
 AND session_user=:'expected_administrative_user'
 AND :'expected_manifest_sha256'~'^[0-9A-F]{64}$'
 AND :'expected_tls_spki_sha256'~'^[0-9a-f]{64}$'
 AND :'expected_environment'~'^[a-z][a-z0-9-]{1,30}$'
 AND :'expected_source_commit'~'^[0-9a-f]{40}$'
 AND :'execution_instance_id'~'^[0-9a-fA-F-]{36}$'
 AND (SELECT count(*) FROM pg_database WHERE datname=:'target_database'
      AND pg_get_userbyid(datdba)='nexa_rev869b_control_plane_owner'
      AND datallowconn AND NOT datistemplate)=1
 AND NOT has_database_privilege('public',:'target_database','CONNECT,TEMPORARY')
 AND NOT EXISTS(SELECT 1 FROM role_mismatch)
 AND NOT EXISTS(SELECT 1 FROM role_capability_mismatch)
 AND NOT EXISTS(SELECT 1 FROM unexpected_membership)
 AND EXISTS(SELECT 1 FROM pg_auth_members m JOIN pg_roles granted ON granted.oid=m.roleid JOIN pg_roles member ON member.oid=m.member WHERE granted.rolname='nexa_rev869b_control_plane_owner' AND member.rolname='nexa_rev869b_lifecycle_administrator')
 THEN 'REV869B_EXTERNAL_PROVISIONING_EXACT' ELSE 1/0::text END;
