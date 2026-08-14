\set ON_ERROR_STOP on
BEGIN;
DO $guard$ BEGIN
 IF current_database()<>'sess_nexaerp_rev869b_control_plane' OR session_user<>'nexa_rev869b_lifecycle_administrator' THEN RAISE EXCEPTION 'Exact external rollback principal/database required'; END IF;
 IF EXISTS(SELECT 1 FROM nexa.rev869b_database_leases WHERE State<>'Finalized') THEN RAISE EXCEPTION 'Rollback refused while non-finalized leases exist'; END IF;
END $guard$;
SET LOCAL ROLE nexa_rev869b_control_plane_owner;
DROP SCHEMA nexa CASCADE;
COMMIT;
