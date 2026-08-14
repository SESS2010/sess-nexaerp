set ON_ERROR_STOP on
SELECT CASE WHEN current_database()='postgres'
 AND pg_control_system().system_identifier::text=:'expected_system_identifier'
 AND coalesce(inet_server_addr()::text,'local')=:'expected_server_address'
 AND inet_server_port()=:'expected_server_port'::integer
 AND session_user=:'expected_administrative_user'
 AND :'expected_manifest_sha256'~'^[0-9A-F]{64}$'
 AND :'expected_source_commit'~'^[0-9a-f]{40}$'
 AND :'execution_instance_id'~'^[0-9a-fA-F-]{36}$'
 AND NOT EXISTS(SELECT 1 FROM pg_database WHERE datname=:'target_database')
 AND NOT EXISTS(SELECT 1 FROM pg_roles WHERE rolname=ANY(ARRAY[
  'nexa_rev869b_control_plane_owner','nexa_rev869b_control_plane_api',
  'nexa_rev869b_control_plane_issuer','nexa_rev869b_control_plane_audit_writer',
  'nexa_rev869b_recovery_administrator','nexa_rev869b_purge_authorizer',
  'nexa_rev869b_purge_executor','nexa_rev869b_verifier','nexa_rev869b_security_owner',
  'nexa_rev869b_runtime','nexa_rev869b_command_issuer','nexa_rev869b_purge_audit_writer',
  'nexa_rev869b_security_export_authorizer','nexa_rev869b_security_export_reader',
  'nexa_rev869b_provisioning_administrator']::name[]))
 THEN 'REV869B_PREFLIGHT_EXACT_EMPTY_TARGET' ELSE 1/0::text END;
