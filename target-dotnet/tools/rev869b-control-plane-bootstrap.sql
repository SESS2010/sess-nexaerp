\set ON_ERROR_STOP on
SELECT CASE WHEN current_database()<>'postgres' THEN 1/0 ELSE 1 END;
SELECT 'CREATE ROLE nexa_rev869b_control_plane_owner NOLOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS'
 WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_rev869b_control_plane_owner') \gexec
SELECT 'CREATE ROLE nexa_rev869b_control_plane_api LOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS'
 WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_rev869b_control_plane_api') \gexec
SELECT 'CREATE ROLE nexa_rev869b_control_plane_issuer LOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS'
 WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_rev869b_control_plane_issuer') \gexec
SELECT 'CREATE ROLE nexa_rev869b_control_plane_audit_writer LOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS'
 WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_rev869b_control_plane_audit_writer') \gexec
SELECT 'CREATE ROLE nexa_rev869b_recovery_administrator LOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS'
 WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_rev869b_recovery_administrator') \gexec
SELECT 'CREATE ROLE nexa_rev869b_purge_authorizer LOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS'
 WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_rev869b_purge_authorizer') \gexec
SELECT 'CREATE ROLE nexa_rev869b_purge_executor LOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS'
 WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_rev869b_purge_executor') \gexec
SELECT 'CREATE ROLE nexa_rev869b_verifier LOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS'
 WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_rev869b_verifier') \gexec
SELECT 'CREATE ROLE nexa_rev869b_security_owner NOLOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS'
 WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_rev869b_security_owner') \gexec
SELECT 'CREATE ROLE nexa_rev869b_runtime LOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS'
 WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_rev869b_runtime') \gexec
SELECT 'CREATE ROLE nexa_rev869b_command_issuer LOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS'
 WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_rev869b_command_issuer') \gexec
SELECT 'CREATE ROLE nexa_rev869b_purge_audit_writer LOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS'
 WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_rev869b_purge_audit_writer') \gexec
SELECT 'CREATE ROLE nexa_rev869b_security_export_authorizer LOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS'
 WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_rev869b_security_export_authorizer') \gexec
SELECT 'CREATE ROLE nexa_rev869b_security_export_reader LOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS'
 WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_rev869b_security_export_reader') \gexec
SELECT 'CREATE ROLE nexa_rev869b_provisioning_administrator LOGIN NOINHERIT NOSUPERUSER CREATEDB CREATEROLE NOREPLICATION NOBYPASSRLS'
 WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_rev869b_provisioning_administrator') \gexec
GRANT nexa_rev869b_security_owner TO nexa_rev869b_provisioning_administrator
 WITH INHERIT FALSE, SET TRUE;
GRANT nexa_rev869b_control_plane_owner TO nexa_rev869b_provisioning_administrator
 WITH INHERIT FALSE, SET TRUE;
SELECT 'CREATE DATABASE sess_nexaerp_rev869b_control_plane OWNER nexa_rev869b_control_plane_owner'
 WHERE NOT EXISTS (SELECT 1 FROM pg_database WHERE datname='sess_nexaerp_rev869b_control_plane') \gexec
REVOKE CONNECT ON DATABASE sess_nexaerp_rev869b_control_plane FROM PUBLIC;
GRANT CONNECT ON DATABASE sess_nexaerp_rev869b_control_plane TO
  nexa_rev869b_control_plane_api,nexa_rev869b_control_plane_issuer,
  nexa_rev869b_control_plane_audit_writer,nexa_rev869b_recovery_administrator,nexa_rev869b_verifier,
  nexa_rev869b_provisioning_administrator;
