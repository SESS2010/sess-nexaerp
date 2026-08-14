\set ON_ERROR_STOP on
BEGIN;
DO $guard$ BEGIN
 IF current_database()<>'sess_nexaerp_rev869b_control_plane' THEN RAISE EXCEPTION 'Wrong rollback database'; END IF;
 IF EXISTS (SELECT 1 FROM nexa.rev869b_database_leases WHERE "State"<>'Finalized') THEN
  RAISE EXCEPTION 'Rollback refused while non-finalized leases exist';
 END IF;
END $guard$;
DROP FUNCTION nexa.rev869b_verify_exact_control_plane(name,name,name);
DROP FUNCTION nexa.rev869b_record_recovery_outcome(uuid,text,text,text,text,text,timestamptz,text);
DROP FUNCTION nexa.rev869b_consume_recovery_approval(uuid,name,text,text,text,text,name,text,text,text,text,name,timestamptz,timestamptz,name,name,text,text,text,text,text,text,text,text,text,text,timestamptz,timestamptz,text,text,timestamptz,text);
DROP FUNCTION nexa.rev869b_record_database_drop_outcome(uuid,name,text,text,text,text,text,text,text,timestamptz,text);
DROP FUNCTION nexa.rev869b_begin_database_drop(name,text,text,text,text,name,text,text,text,text,name,timestamptz,timestamptz,name,name,text,text,text,text,text,timestamptz,text);
DROP FUNCTION nexa.rev869b_read_exact_database_lease(name,text,text,text,text,name,text,text,text,text,name,timestamptz,timestamptz,name,name,text,text,text,text);
DROP FUNCTION nexa.rev869b_complete_database_lease(name,text,text,text,text,name,text,text,text,text,name,timestamptz,timestamptz,name,name,text,text,text,text,text,text,text,timestamptz,text);
DROP FUNCTION nexa.rev869b_reserve_database_lease(name,text,text,text,text,name,text,text,text,text,name,timestamptz,timestamptz,name,name,text,text,text);
DROP FUNCTION nexa.rev869b_read_drop_started_attempt(name,text);
DROP FUNCTION nexa.rev869b_read_database_lease_transition_states(name,text);
DROP FUNCTION nexa.rev869b_read_authoritative_database_lease(name,text);
DROP FUNCTION nexa.rev869b_transition_database_lease(name,text,text,bigint,text,bytea,uuid,bytea,name,text,bytea,bytea,uuid,text,text,text);
DROP TRIGGER "TR_rev869b_recovery_attempts_immutable" ON nexa.rev869b_recovery_attempts;
DROP TRIGGER "TR_rev869b_recovery_outcomes_immutable" ON nexa.rev869b_recovery_outcomes;
DROP TRIGGER "TR_rev869b_lease_events_immutable" ON nexa.rev869b_database_lease_events;
DROP FUNCTION nexa.rev869b_reject_registry_audit_mutation();
DROP TABLE nexa.rev869b_recovery_outcomes;
DROP TABLE nexa.rev869b_recovery_attempts;
DROP TABLE nexa.rev869b_recovery_approvals;
DROP TABLE nexa.rev869b_database_lease_events;
DROP TABLE nexa.rev869b_database_leases;
DROP SCHEMA nexa;
COMMIT;
