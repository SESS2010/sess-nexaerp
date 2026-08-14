\set ON_ERROR_STOP on
WITH expected_roles(name,login,inherit) AS (VALUES
 ('nexa_rev869b_control_plane_owner',false,false),('nexa_rev869b_control_plane_api',true,false),
 ('nexa_rev869b_control_plane_issuer',true,false),('nexa_rev869b_control_plane_audit_writer',true,false),
 ('nexa_rev869b_recovery_administrator',true,false),('nexa_rev869b_purge_authorizer',true,false),
 ('nexa_rev869b_purge_executor',true,false),('nexa_rev869b_verifier',true,false),
 ('nexa_rev869b_security_owner',false,false),('nexa_rev869b_runtime',true,false),
 ('nexa_rev869b_command_issuer',true,false),('nexa_rev869b_purge_audit_writer',true,false),
 ('nexa_rev869b_security_export_authorizer',true,false),('nexa_rev869b_security_export_reader',true,false)),
bad_roles AS (
 SELECT e.name FROM expected_roles e LEFT JOIN pg_roles r ON r.rolname=e.name
 WHERE r.oid IS NULL OR r.rolcanlogin<>e.login OR r.rolinherit<>e.inherit OR
  r.rolsuper OR r.rolcreatedb OR r.rolcreaterole OR r.rolreplication OR r.rolbypassrls),
expected_relations(name,column_count) AS (VALUES ('rev869b_database_leases',29),('rev869b_database_lease_events',19),
 ('rev869b_recovery_approvals',18),('rev869b_recovery_attempts',8),('rev869b_recovery_outcomes',7)),
bad_relations AS (
 SELECT e.name FROM expected_relations e LEFT JOIN pg_class c ON c.relname=e.name
  LEFT JOIN pg_namespace n ON n.oid=c.relnamespace AND n.nspname='nexa'
 WHERE c.oid IS NULL OR c.relkind<>'r' OR pg_get_userbyid(c.relowner)<>'nexa_rev869b_control_plane_owner'
  OR (SELECT count(*) FROM pg_attribute a WHERE a.attrelid=c.oid AND a.attnum>0 AND NOT a.attisdropped)<>e.column_count),
unexpected_relations AS (
 SELECT c.relname FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
 WHERE n.nspname='nexa' AND c.relkind IN ('r','p','v','m','S') AND
 c.relname NOT IN ('rev869b_database_leases','rev869b_database_lease_events',
  'rev869b_recovery_approvals','rev869b_recovery_attempts','rev869b_recovery_outcomes')),
public_or_direct AS (
 SELECT c.relname FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
 WHERE n.nspname='nexa' AND c.relkind='r' AND
  (has_table_privilege('public',c.oid,'SELECT,INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER') OR
   has_table_privilege('nexa_rev869b_control_plane_api',c.oid,'SELECT,INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER'))
)
SELECT CASE WHEN current_database()='sess_nexaerp_rev869b_control_plane'
 AND pg_get_userbyid((SELECT datdba FROM pg_database WHERE datname=current_database()))='nexa_rev869b_control_plane_owner'
 AND NOT has_database_privilege('public',current_database(),'CONNECT')
 AND NOT has_schema_privilege('public','nexa','USAGE,CREATE')
 AND NOT has_schema_privilege('nexa_rev869b_control_plane_api','nexa','CREATE')
 AND EXISTS(SELECT 1 FROM pg_roles r WHERE r.rolname='nexa_rev869b_provisioning_administrator'
   AND r.rolcanlogin AND NOT r.rolinherit AND NOT r.rolsuper AND r.rolcreatedb AND r.rolcreaterole
   AND NOT r.rolreplication AND NOT r.rolbypassrls)
 AND NOT EXISTS(SELECT 1 FROM bad_roles) AND NOT EXISTS(SELECT 1 FROM bad_relations)
 AND (SELECT count(*) FROM pg_class i JOIN pg_namespace n ON n.oid=i.relnamespace
   WHERE n.nspname='nexa' AND i.relkind='i')=20
 AND NOT EXISTS(SELECT 1 FROM unexpected_relations) AND NOT EXISTS(SELECT 1 FROM public_or_direct)
 AND (SELECT count(*) FROM pg_auth_members m JOIN pg_roles r ON r.oid=m.roleid
   JOIN pg_roles u ON u.oid=m.member WHERE r.rolname IN ('nexa_rev869b_security_owner','nexa_rev869b_control_plane_owner')
    AND u.rolname='nexa_rev869b_provisioning_administrator'
    AND NOT m.inherit_option AND m.set_option)=2
 AND NOT EXISTS(SELECT 1 FROM pg_auth_members m JOIN pg_roles r ON r.oid=m.roleid
   JOIN pg_roles u ON u.oid=m.member WHERE r.rolname LIKE 'nexa_rev869b_%'
    AND NOT (r.rolname IN ('nexa_rev869b_security_owner','nexa_rev869b_control_plane_owner')
      AND u.rolname='nexa_rev869b_provisioning_administrator'
      AND NOT m.inherit_option AND m.set_option))
 AND nexa.rev869b_verify_exact_control_plane(
   'sess_nexaerp_rev869b_control_plane'::name,
   'nexa_rev869b_control_plane_owner'::name,
   session_user::name)
 THEN 'REV869B_CONTROL_PLANE_EXACT' ELSE 1/0::text END;
