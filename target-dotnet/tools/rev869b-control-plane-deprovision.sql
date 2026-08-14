set ON_ERROR_STOP on
SELECT CASE WHEN current_database()='postgres'
 AND pg_control_system().system_identifier::text=:'expected_system_identifier'
 AND coalesce(inet_server_addr()::text,'local')=:'expected_server_address'
 AND inet_server_port()=:'expected_server_port'::integer
 AND EXISTS(SELECT 1 FROM pg_database d WHERE d.datname=:'target_database'
  AND pg_get_userbyid(d.datdba)='nexa_rev869b_control_plane_owner')
 AND (SELECT count(*) FROM pg_stat_activity WHERE datname=:'target_database')=0
 AND NOT EXISTS(SELECT 1 FROM pg_auth_members m
  JOIN pg_roles r ON r.oid=m.roleid JOIN pg_roles u ON u.oid=m.member
  WHERE (r.rolname LIKE 'nexa_rev869b_%' OR u.rolname LIKE 'nexa_rev869b_%')
   AND NOT (r.rolname IN ('nexa_rev869b_security_owner','nexa_rev869b_control_plane_owner')
    AND u.rolname='nexa_rev869b_provisioning_administrator'
    AND NOT m.inherit_option AND m.set_option))
 THEN 1 ELSE 1/0 END;
DROP DATABASE sess_nexaerp_rev869b_control_plane;
REVOKE nexa_rev869b_security_owner,nexa_rev869b_control_plane_owner
 FROM nexa_rev869b_provisioning_administrator;
DROP ROLE nexa_rev869b_control_plane_api,nexa_rev869b_control_plane_issuer,
 nexa_rev869b_control_plane_audit_writer,nexa_rev869b_recovery_administrator,
 nexa_rev869b_purge_authorizer,nexa_rev869b_purge_executor,nexa_rev869b_verifier,
 nexa_rev869b_runtime,nexa_rev869b_command_issuer,nexa_rev869b_purge_audit_writer,
 nexa_rev869b_security_export_authorizer,nexa_rev869b_security_export_reader,
 nexa_rev869b_provisioning_administrator,nexa_rev869b_security_owner,
 nexa_rev869b_control_plane_owner;
