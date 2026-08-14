\set ON_ERROR_STOP on
-- Canonical full-inventory verifier: no sampled objects or count-only acceptance.
WITH expected_relations(name,owner) AS (VALUES
 ('rev869b_control_plane_manifest','nexa_rev869b_control_plane_owner'),('rev869b_database_leases','nexa_rev869b_control_plane_owner'),
 ('rev869b_database_lease_events','nexa_rev869b_control_plane_owner'),('rev869b_recovery_decisions','nexa_rev869b_control_plane_owner'),
 ('rev869b_lifecycle_attempts','nexa_rev869b_control_plane_owner'),('rev869b_lifecycle_outcomes','nexa_rev869b_control_plane_owner')),
actual_relations AS (SELECT c.relname::text name,pg_get_userbyid(c.relowner)::text owner FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='nexa' AND c.relkind='r'),
expected_functions(signature,owner) AS (VALUES
 ('nexa.rev869b_authorize_normal_drop(uuid,bigint,uuid,text)','nexa_rev869b_control_plane_owner'),('nexa.rev869b_begin_drop(uuid,bigint,uuid,uuid,text)','nexa_rev869b_control_plane_owner'),
 ('nexa.rev869b_begin_provisioning(uuid,bigint,uuid,uuid,text)','nexa_rev869b_control_plane_owner'),('nexa.rev869b_consume_recovery_decision(uuid,bigint,uuid,uuid,text,uuid,text)','nexa_rev869b_control_plane_owner'),
 ('nexa.rev869b_deny_evidence_mutation()','nexa_rev869b_control_plane_owner'),('nexa.rev869b_finalize_absent_target(uuid,text,text,text)','nexa_rev869b_control_plane_owner'),
 ('nexa.rev869b_mark_in_use(uuid,bigint,uuid,text)','nexa_rev869b_control_plane_owner'),('nexa.rev869b_mark_ready(uuid,bigint,uuid,text,text,text)','nexa_rev869b_control_plane_owner'),
 ('nexa.rev869b_read_lease(uuid)','nexa_rev869b_control_plane_owner'),('nexa.rev869b_read_nonterminal_leases(text)','nexa_rev869b_control_plane_owner'),
 ('nexa.rev869b_record_cleanup_failure(uuid,text,text,text)','nexa_rev869b_control_plane_owner'),('nexa.rev869b_register_recovery_decision(uuid,uuid,text,text,text,timestamp with time zone)','nexa_rev869b_control_plane_owner'),
 ('nexa.rev869b_reserve_lease(uuid,uuid,name,text,text,text,text,text,text,name,name,name,text)','nexa_rev869b_control_plane_owner')),
actual_functions AS (SELECT p.oid::regprocedure::text signature,pg_get_userbyid(p.proowner)::text owner FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='nexa'),
expected_exec(role_name,signature) AS (VALUES
 ('nexa_rev869b_lifecycle_api','nexa.rev869b_reserve_lease(uuid,uuid,name,text,text,text,text,text,text,name,name,name,text)'),('nexa_rev869b_lifecycle_api','nexa.rev869b_begin_provisioning(uuid,bigint,uuid,uuid,text)'),
 ('nexa_rev869b_lifecycle_api','nexa.rev869b_mark_ready(uuid,bigint,uuid,text,text,text)'),('nexa_rev869b_lifecycle_api','nexa.rev869b_mark_in_use(uuid,bigint,uuid,text)'),
 ('nexa_rev869b_lifecycle_api','nexa.rev869b_authorize_normal_drop(uuid,bigint,uuid,text)'),('nexa_rev869b_lifecycle_api','nexa.rev869b_begin_drop(uuid,bigint,uuid,uuid,text)'),
 ('nexa_rev869b_lifecycle_api','nexa.rev869b_read_lease(uuid)'),('nexa_rev869b_lifecycle_api','nexa.rev869b_read_nonterminal_leases(text)'),
 ('nexa_rev869b_lifecycle_audit','nexa.rev869b_record_cleanup_failure(uuid,text,text,text)'),('nexa_rev869b_lifecycle_audit','nexa.rev869b_finalize_absent_target(uuid,text,text,text)'),
 ('nexa_rev869b_lifecycle_audit','nexa.rev869b_read_lease(uuid)'),('nexa_rev869b_lifecycle_audit','nexa.rev869b_read_nonterminal_leases(text)'),
 ('nexa_rev869b_recovery_executor','nexa.rev869b_consume_recovery_decision(uuid,bigint,uuid,uuid,text,uuid,text)'),('nexa_rev869b_recovery_executor','nexa.rev869b_begin_drop(uuid,bigint,uuid,uuid,text)'),
 ('nexa_rev869b_recovery_executor','nexa.rev869b_read_lease(uuid)'),('nexa_rev869b_recovery_executor','nexa.rev869b_read_nonterminal_leases(text)'),
 ('nexa_rev869b_management_writer','nexa.rev869b_register_recovery_decision(uuid,uuid,text,text,text,timestamp with time zone)'),
 ('nexa_rev869b_control_plane_verifier','nexa.rev869b_read_lease(uuid)'),('nexa_rev869b_control_plane_verifier','nexa.rev869b_read_nonterminal_leases(text)')),
actual_exec AS (SELECT r.rolname::text role_name,p.oid::regprocedure::text signature FROM pg_roles r CROSS JOIN pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='nexa' AND r.rolname LIKE 'nexa_rev869b_%' AND r.rolname NOT IN ('nexa_rev869b_control_plane_owner','nexa_rev869b_lifecycle_administrator') AND has_function_privilege(r.oid,p.oid,'EXECUTE')),
relation_delta AS ((SELECT * FROM expected_relations EXCEPT SELECT * FROM actual_relations) UNION ALL (SELECT * FROM actual_relations EXCEPT SELECT * FROM expected_relations)),
function_delta AS ((SELECT * FROM expected_functions EXCEPT SELECT * FROM actual_functions) UNION ALL (SELECT * FROM actual_functions EXCEPT SELECT * FROM expected_functions)),
exec_delta AS ((SELECT * FROM expected_exec EXCEPT SELECT * FROM actual_exec) UNION ALL (SELECT * FROM actual_exec EXCEPT SELECT * FROM expected_exec)),
direct_table_access AS (SELECT r.rolname,c.relname FROM pg_roles r CROSS JOIN pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='nexa' AND c.relkind='r' AND r.rolname LIKE 'nexa_rev869b_%' AND r.rolname<>'nexa_rev869b_control_plane_owner' AND has_table_privilege(r.oid,c.oid,'SELECT,INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER'))
SELECT CASE WHEN current_database()='sess_nexaerp_rev869b_control_plane'
 AND pg_get_userbyid((SELECT datdba FROM pg_database WHERE datname=current_database()))='nexa_rev869b_control_plane_owner'
 AND (SELECT count(*) FROM nexa.rev869b_control_plane_manifest WHERE Environment=:'expected_environment' AND ClusterSystemIdentifier=:'expected_system_identifier' AND TlsSpkiSha256=lower(:'expected_tls_spki_sha256') AND Endpoint=:'expected_server_address'||':'||:'expected_server_port' AND SourceCommit=:'expected_source_commit' AND ManifestSha256=lower(:'expected_manifest_sha256'))=1
 AND NOT has_database_privilege('public',current_database(),'CONNECT,TEMPORARY')
 AND NOT has_schema_privilege('public','nexa','USAGE,CREATE')
 AND NOT EXISTS(SELECT 1 FROM relation_delta) AND NOT EXISTS(SELECT 1 FROM function_delta)
 AND NOT EXISTS(SELECT 1 FROM exec_delta) AND NOT EXISTS(SELECT 1 FROM direct_table_access)
 AND NOT EXISTS(SELECT 1 FROM pg_auth_members m JOIN pg_roles a ON a.oid=m.roleid JOIN pg_roles b ON b.oid=m.member WHERE (a.rolname LIKE 'nexa_rev869b_%' OR b.rolname LIKE 'nexa_rev869b_%') AND NOT (a.rolname='nexa_rev869b_control_plane_owner' AND b.rolname='nexa_rev869b_lifecycle_administrator'))
 AND EXISTS(SELECT 1 FROM pg_auth_members m JOIN pg_roles a ON a.oid=m.roleid JOIN pg_roles b ON b.oid=m.member WHERE a.rolname='nexa_rev869b_control_plane_owner' AND b.rolname='nexa_rev869b_lifecycle_administrator')
 AND NOT EXISTS(SELECT 1 FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='nexa' AND has_function_privilege('public',p.oid,'EXECUTE'))
 THEN 'REV869B_CONTROL_PLANE_CANONICAL_EXACT' ELSE 1/0::text END;
