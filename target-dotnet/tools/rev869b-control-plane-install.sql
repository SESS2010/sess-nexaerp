\set ON_ERROR_STOP on
BEGIN;
DO $gate$ BEGIN
 IF current_database()<>'sess_nexaerp_rev869b_control_plane' THEN RAISE EXCEPTION 'Wrong control-plane database'; END IF;
 IF session_user<>'nexa_rev869b_lifecycle_administrator' THEN RAISE EXCEPTION 'External lifecycle administrator required'; END IF;
 IF pg_has_role(session_user,'nexa_rev869b_control_plane_owner','MEMBER') IS NOT TRUE THEN RAISE EXCEPTION 'Installer cannot SET control-plane owner'; END IF;
END $gate$;
SET LOCAL ROLE nexa_rev869b_control_plane_owner;
CREATE SCHEMA nexa AUTHORIZATION nexa_rev869b_control_plane_owner;
REVOKE ALL ON SCHEMA nexa FROM PUBLIC;

CREATE TABLE nexa.rev869b_control_plane_manifest(
 PackageId uuid PRIMARY KEY,Environment text NOT NULL,ClusterSystemIdentifier text NOT NULL CHECK(ClusterSystemIdentifier~'^[0-9]{10,20}$'),
 TlsSpkiSha256 text NOT NULL CHECK(TlsSpkiSha256~'^[0-9a-f]{64}$'),Endpoint text NOT NULL,
 SourceCommit text NOT NULL CHECK(SourceCommit~'^[0-9a-f]{40}$'),ManifestSha256 text NOT NULL CHECK(ManifestSha256~'^[0-9a-f]{64}$'),CatalogueSha256 text NULL CHECK(CatalogueSha256 IS NULL OR CatalogueSha256~'^[0-9a-f]{64}$'),
 InstalledAt timestamptz NOT NULL DEFAULT clock_timestamp(),InstalledBy name NOT NULL DEFAULT session_user);
INSERT INTO nexa.rev869b_control_plane_manifest
VALUES(gen_random_uuid(),:'expected_environment',:'expected_system_identifier',lower(:'expected_tls_spki_sha256'),
       :'expected_server_address'||':'||:'expected_server_port',:'expected_source_commit',
       lower(:'expected_manifest_sha256'),NULL,clock_timestamp(),session_user);

CREATE TABLE nexa.rev869b_database_leases(
 LeaseId uuid PRIMARY KEY,ReservationRequestId uuid NOT NULL UNIQUE,TargetDatabase name NOT NULL UNIQUE,
 ClusterSystemIdentifier text NOT NULL,TlsSpkiSha256 text NOT NULL,Endpoint text NOT NULL,
 SourceCommit text NOT NULL,TargetManifestSha256 text NOT NULL,OwnershipNonceSha256 text NOT NULL,
 OwnerRole name NOT NULL,RuntimeRole name NOT NULL,AuditRole name NOT NULL,
 State text NOT NULL CHECK(State IN ('Reserved','Provisioning','Ready','InUse','DropAuthorized','DropStarted','Quarantined','RecoveryAuthorized','CleanupFailed','Finalized')),
 Version bigint NOT NULL DEFAULT 0,ActiveAttemptId uuid NULL,TargetMarkerSha256 text NULL,ObservedDatabaseIdentitySha256 text NULL,
 CreatedAt timestamptz NOT NULL DEFAULT clock_timestamp(),UpdatedAt timestamptz NOT NULL DEFAULT clock_timestamp(),
 CHECK(TargetManifestSha256~'^[0-9a-f]{64}$' AND OwnershipNonceSha256~'^[0-9a-f]{64}$'));

CREATE TABLE nexa.rev869b_database_lease_events(
 EventId bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,LeaseId uuid NOT NULL REFERENCES nexa.rev869b_database_leases(LeaseId),
 RequestId uuid NOT NULL,AttemptId uuid NULL,FromState text NULL,ToState text NOT NULL,Version bigint NOT NULL,
 EvidenceSha256 text NOT NULL CHECK(EvidenceSha256~'^[0-9a-f]{64}$'),OccurredAt timestamptz NOT NULL DEFAULT clock_timestamp(),Principal name NOT NULL DEFAULT session_user,
 UNIQUE(LeaseId,RequestId),UNIQUE(LeaseId,Version));

CREATE TABLE nexa.rev869b_recovery_decisions(
 DecisionId uuid PRIMARY KEY,LeaseId uuid NOT NULL REFERENCES nexa.rev869b_database_leases(LeaseId),
 AuthorizedAction text NOT NULL CHECK(AuthorizedAction IN ('DropAndFinalize','FinalizeAbsent')),
 PreState text NOT NULL,NonceSha256 text NOT NULL CHECK(NonceSha256~'^[0-9a-f]{64}$'),
 ExpiresAt timestamptz NOT NULL,IssuedAt timestamptz NOT NULL DEFAULT clock_timestamp(),ConsumedAt timestamptz NULL,ConsumedAttemptId uuid NULL,
 IssuedBy name NOT NULL DEFAULT session_user,CHECK(ExpiresAt>IssuedAt AND ExpiresAt<=IssuedAt+interval '15 minutes'));

CREATE TABLE nexa.rev869b_lifecycle_attempts(
 AttemptId uuid PRIMARY KEY,LeaseId uuid NOT NULL REFERENCES nexa.rev869b_database_leases(LeaseId),Kind text NOT NULL CHECK(Kind IN ('Provision','NormalDrop','Recovery','Quarantine')),
 DecisionId uuid NULL REFERENCES nexa.rev869b_recovery_decisions(DecisionId),ExecutionInstanceId uuid NOT NULL,
 ActorId text NOT NULL CHECK(length(ActorId) BETWEEN 1 AND 200),ActorIssuer text NOT NULL CHECK(length(ActorIssuer) BETWEEN 1 AND 200),
 Operation text NOT NULL CHECK(length(Operation) BETWEEN 1 AND 100),RegistrationRequestId uuid NOT NULL UNIQUE,
 AuthorityEvidenceSha256 text NOT NULL CHECK(AuthorityEvidenceSha256~'^[0-9a-f]{64}$'),
 StartedAt timestamptz NOT NULL DEFAULT clock_timestamp(),StartedBy name NOT NULL DEFAULT session_user,
 TerminalState text NULL CHECK(TerminalState IS NULL OR TerminalState IN ('Ready','Quarantined','Finalized','CleanupFailed','Interrupted')),
 UNIQUE(LeaseId,AttemptId));
CREATE UNIQUE INDEX UX_rev869b_one_active_lifecycle_attempt ON nexa.rev869b_lifecycle_attempts(LeaseId) WHERE TerminalState IS NULL;

CREATE TABLE nexa.rev869b_lifecycle_outcomes(
 OutcomeId uuid PRIMARY KEY,AttemptId uuid NOT NULL UNIQUE REFERENCES nexa.rev869b_lifecycle_attempts(AttemptId),
 Outcome text NOT NULL CHECK(Outcome IN ('Finalized','CleanupFailed')),ObservedTargetState text NOT NULL,
 AbsenceSha256 text NULL,RolesCleanupSha256 text NULL,FailureCategory text NULL,EvidenceSha256 text NOT NULL CHECK(EvidenceSha256~'^[0-9a-f]{64}$'),
 OccurredAt timestamptz NOT NULL DEFAULT clock_timestamp(),RecordedBy name NOT NULL DEFAULT session_user,
 CHECK((Outcome='Finalized' AND AbsenceSha256~'^[0-9a-f]{64}$' AND RolesCleanupSha256~'^[0-9a-f]{64}$' AND FailureCategory IS NULL)
    OR (Outcome='CleanupFailed' AND AbsenceSha256 IS NULL AND RolesCleanupSha256 IS NULL AND length(FailureCategory) BETWEEN 1 AND 100)));

CREATE TABLE nexa.rev869b_quarantine_outcomes(
 QuarantineOutcomeId uuid PRIMARY KEY,LeaseId uuid NOT NULL REFERENCES nexa.rev869b_database_leases(LeaseId),
 RequestId uuid NOT NULL,AttemptId uuid NOT NULL UNIQUE REFERENCES nexa.rev869b_lifecycle_attempts(AttemptId),ExecutionInstanceId uuid NOT NULL,
 TargetDatabase name NOT NULL,ClusterSystemIdentifier text NOT NULL,SourceState text NOT NULL,ObservedTargetState text NOT NULL,
 EvidenceKind text NOT NULL CHECK(EvidenceKind IN ('Mismatch','Interruption','RetryFailure')),FailureReason text NOT NULL,
 ActorId text NOT NULL,ActorIssuer text NOT NULL,Operation text NOT NULL,SourceLeaseVersion bigint NOT NULL,LeaseVersion bigint NOT NULL,
 TerminalOutcome text NOT NULL CHECK(TerminalOutcome='Quarantined'),EvidenceSha256 text NOT NULL CHECK(EvidenceSha256~'^[0-9a-f]{64}$'),
 OccurredAt timestamptz NOT NULL DEFAULT clock_timestamp(),RecordedBy name NOT NULL DEFAULT session_user,
 UNIQUE(LeaseId,RequestId),CHECK(length(SourceState) BETWEEN 1 AND 100 AND length(ObservedTargetState) BETWEEN 1 AND 100),
 CHECK(length(FailureReason) BETWEEN 1 AND 200 AND length(ActorId) BETWEEN 1 AND 200 AND length(ActorIssuer) BETWEEN 1 AND 200 AND length(Operation) BETWEEN 1 AND 100));

CREATE FUNCTION nexa.rev869b_deny_evidence_mutation() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'REV869B evidence is append-only'; END $$;
CREATE TRIGGER TR_rev869b_lease_events_immutable BEFORE UPDATE OR DELETE ON nexa.rev869b_database_lease_events FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_deny_evidence_mutation();
CREATE TRIGGER TR_rev869b_recovery_decisions_immutable BEFORE DELETE ON nexa.rev869b_recovery_decisions FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_deny_evidence_mutation();
CREATE TRIGGER TR_rev869b_lifecycle_outcomes_immutable BEFORE UPDATE OR DELETE ON nexa.rev869b_lifecycle_outcomes FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_deny_evidence_mutation();
CREATE TRIGGER TR_rev869b_quarantine_outcomes_immutable BEFORE UPDATE OR DELETE ON nexa.rev869b_quarantine_outcomes FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_deny_evidence_mutation();

CREATE FUNCTION nexa.rev869b_reserve_lease(request_id uuid,lease_id uuid,target_database name,cluster_id text,tls_spki text,endpoint text,source_commit text,target_manifest text,ownership_nonce text,owner_role name,runtime_role name,audit_role name,evidence text)
RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,nexa AS $$ DECLARE existing nexa.rev869b_database_leases%ROWTYPE; BEGIN
 IF session_user<>'nexa_rev869b_lifecycle_api' THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Lifecycle API required'; END IF;
 IF target_database!~'^sess_nexaerp_rev869b_[0-9a-f]{24}$' OR target_database IN ('postgres','sess_nexaerp') THEN RAISE EXCEPTION 'Unsafe target name'; END IF;
 IF source_commit!~'^[0-9a-f]{40}$' OR tls_spki!~'^[0-9a-f]{64}$' OR target_manifest!~'^[0-9a-f]{64}$' OR ownership_nonce!~'^[0-9a-f]{64}$' OR evidence!~'^[0-9a-f]{64}$' THEN RAISE EXCEPTION 'Exact fingerprints required'; END IF;
 SELECT * INTO existing FROM nexa.rev869b_database_leases WHERE ReservationRequestId=request_id;
 IF FOUND THEN
  IF existing.LeaseId<>lease_id OR existing.TargetDatabase<>target_database OR existing.OwnershipNonceSha256<>ownership_nonce THEN RAISE EXCEPTION 'Reservation request replay mismatch'; END IF;
  RETURN existing.LeaseId;
 END IF;
 INSERT INTO nexa.rev869b_database_leases VALUES(lease_id,request_id,target_database,cluster_id,tls_spki,endpoint,source_commit,target_manifest,ownership_nonce,owner_role,runtime_role,audit_role,'Reserved',0,NULL,NULL,NULL,clock_timestamp(),clock_timestamp());
 INSERT INTO nexa.rev869b_database_lease_events(LeaseId,RequestId,FromState,ToState,Version,EvidenceSha256) VALUES(lease_id,request_id,NULL,'Reserved',0,evidence);
 RETURN lease_id;
END $$;

CREATE FUNCTION nexa.rev869b_begin_provisioning(lease_id uuid,expected_version bigint,request_id uuid,attempt_id uuid,execution_instance_id uuid,actor_id text,actor_issuer text,operation text,evidence text)
RETURNS bigint LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,nexa AS $$ DECLARE next_version bigint; BEGIN
 IF session_user<>'nexa_rev869b_lifecycle_api' OR execution_instance_id='00000000-0000-0000-0000-000000000000'::uuid OR length(actor_id) NOT BETWEEN 1 AND 200 OR length(actor_issuer) NOT BETWEEN 1 AND 200 OR length(operation) NOT BETWEEN 1 AND 100 THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Exact lifecycle provisioning authority required'; END IF;
 UPDATE nexa.rev869b_database_leases SET State='Provisioning',Version=Version+1,ActiveAttemptId=attempt_id,UpdatedAt=clock_timestamp()
 WHERE LeaseId=lease_id AND Version=expected_version AND State='Reserved' RETURNING Version INTO next_version;
 IF next_version IS NULL THEN RAISE EXCEPTION USING ERRCODE='40001',MESSAGE='Lease state/version conflict'; END IF;
 INSERT INTO nexa.rev869b_lifecycle_attempts(AttemptId,LeaseId,Kind,ExecutionInstanceId,ActorId,ActorIssuer,Operation,RegistrationRequestId,AuthorityEvidenceSha256) VALUES(attempt_id,lease_id,'Provision',execution_instance_id,actor_id,actor_issuer,operation,request_id,evidence);
 INSERT INTO nexa.rev869b_database_lease_events VALUES(DEFAULT,lease_id,request_id,attempt_id,'Reserved','Provisioning',next_version,evidence,clock_timestamp(),session_user);
 RETURN next_version;
END $$;

CREATE FUNCTION nexa.rev869b_mark_ready(lease_id uuid,expected_version bigint,request_id uuid,marker_sha text,database_identity_sha text,evidence text)
RETURNS bigint LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,nexa AS $$ DECLARE next_version bigint; BEGIN
 IF session_user<>'nexa_rev869b_lifecycle_api' OR marker_sha!~'^[0-9a-f]{64}$' OR database_identity_sha!~'^[0-9a-f]{64}$' THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Exact lifecycle evidence required'; END IF;
 UPDATE nexa.rev869b_database_leases SET State='Ready',Version=Version+1,TargetMarkerSha256=marker_sha,ObservedDatabaseIdentitySha256=database_identity_sha,UpdatedAt=clock_timestamp()
 WHERE LeaseId=lease_id AND Version=expected_version AND State='Provisioning' RETURNING Version INTO next_version;
 IF next_version IS NULL THEN RAISE EXCEPTION USING ERRCODE='40001',MESSAGE='Lease state/version conflict'; END IF;
 UPDATE nexa.rev869b_lifecycle_attempts SET TerminalState='Ready' WHERE LeaseId=lease_id AND Kind='Provision' AND TerminalState IS NULL;
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='40001',MESSAGE='Provisioning attempt missing'; END IF;
 INSERT INTO nexa.rev869b_database_lease_events VALUES(DEFAULT,lease_id,request_id,NULL,'Provisioning','Ready',next_version,evidence,clock_timestamp(),session_user); RETURN next_version;
END $$;

CREATE FUNCTION nexa.rev869b_mark_in_use(lease_id uuid,expected_version bigint,request_id uuid,evidence text) RETURNS bigint LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,nexa AS $$ DECLARE v bigint; BEGIN
 IF session_user<>'nexa_rev869b_lifecycle_api' THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Lifecycle API required'; END IF;
 UPDATE nexa.rev869b_database_leases SET State='InUse',Version=Version+1,UpdatedAt=clock_timestamp() WHERE LeaseId=lease_id AND Version=expected_version AND State='Ready' RETURNING Version INTO v;
 IF v IS NULL THEN RAISE EXCEPTION USING ERRCODE='40001',MESSAGE='Lease state/version conflict'; END IF;
 INSERT INTO nexa.rev869b_database_lease_events VALUES(DEFAULT,lease_id,request_id,NULL,'Ready','InUse',v,evidence,clock_timestamp(),session_user); RETURN v; END $$;

CREATE FUNCTION nexa.rev869b_authorize_normal_drop(lease_id uuid,expected_version bigint,request_id uuid,evidence text) RETURNS bigint LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,nexa AS $$ DECLARE v bigint; prior text; BEGIN
 IF session_user<>'nexa_rev869b_lifecycle_api' THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Lifecycle API required'; END IF;
 SELECT State INTO prior FROM nexa.rev869b_database_leases WHERE LeaseId=lease_id AND Version=expected_version AND State IN ('Ready','InUse') FOR UPDATE;
 UPDATE nexa.rev869b_database_leases SET State='DropAuthorized',Version=Version+1,UpdatedAt=clock_timestamp() WHERE LeaseId=lease_id AND Version=expected_version AND State IN ('Ready','InUse') RETURNING Version INTO v;
 IF v IS NULL THEN RAISE EXCEPTION USING ERRCODE='40001',MESSAGE='Lease state/version conflict'; END IF;
 INSERT INTO nexa.rev869b_database_lease_events VALUES(DEFAULT,lease_id,request_id,NULL,prior,'DropAuthorized',v,evidence,clock_timestamp(),session_user); RETURN v; END $$;

CREATE FUNCTION nexa.rev869b_begin_quarantine_attempt(lease_id uuid,expected_version bigint,request_id uuid,attempt_id uuid,execution_instance_id uuid,actor_id text,actor_issuer text,operation text,authority_evidence text) RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,nexa AS $$
DECLARE prior text; prior_attempt uuid; BEGIN
 IF session_user<>'nexa_rev869b_lifecycle_api' OR execution_instance_id='00000000-0000-0000-0000-000000000000'::uuid OR length(actor_id) NOT BETWEEN 1 AND 200 OR length(actor_issuer) NOT BETWEEN 1 AND 200 OR length(operation) NOT BETWEEN 1 AND 100 OR authority_evidence!~'^[0-9a-f]{64}$' THEN RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_quarantine_authority_binding',MESSAGE='Exact lifecycle quarantine authority required'; END IF;
 SELECT State,ActiveAttemptId INTO prior,prior_attempt FROM nexa.rev869b_database_leases WHERE LeaseId=lease_id AND Version=expected_version AND State IN ('Reserved','Provisioning','Ready','InUse') FOR UPDATE;
 IF prior IS NULL THEN RAISE EXCEPTION USING ERRCODE='40001',MESSAGE='Quarantine registration state/version conflict'; END IF;
 IF prior_attempt IS NOT NULL THEN UPDATE nexa.rev869b_lifecycle_attempts SET TerminalState='Interrupted' WHERE AttemptId=prior_attempt AND LeaseId=lease_id AND TerminalState IS NULL; END IF;
 INSERT INTO nexa.rev869b_lifecycle_attempts(AttemptId,LeaseId,Kind,ExecutionInstanceId,ActorId,ActorIssuer,Operation,RegistrationRequestId,AuthorityEvidenceSha256) VALUES(attempt_id,lease_id,'Quarantine',execution_instance_id,actor_id,actor_issuer,operation,request_id,authority_evidence);
 UPDATE nexa.rev869b_database_leases SET ActiveAttemptId=attempt_id,UpdatedAt=clock_timestamp() WHERE LeaseId=lease_id AND Version=expected_version;
 RETURN attempt_id; END $$;

CREATE FUNCTION nexa.rev869b_record_quarantine(lease_id uuid,expected_version bigint,request_id uuid,attempt_id uuid,observed_target_state text,evidence_kind text,failure_reason text,evidence text) RETURNS bigint LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,nexa AS $$
DECLARE v bigint; prior text; active_attempt uuid; target_database name; cluster_id text; execution_instance_id uuid; actor_id text; actor_issuer text; operation text; replay nexa.rev869b_quarantine_outcomes%ROWTYPE; BEGIN
 IF session_user<>'nexa_rev869b_lifecycle_audit' OR lease_id='00000000-0000-0000-0000-000000000000'::uuid OR request_id='00000000-0000-0000-0000-000000000000'::uuid OR attempt_id='00000000-0000-0000-0000-000000000000'::uuid OR length(observed_target_state) NOT BETWEEN 1 AND 100 OR evidence_kind NOT IN ('Mismatch','Interruption','RetryFailure') OR length(failure_reason) NOT BETWEEN 1 AND 200 OR evidence!~'^[0-9a-f]{64}$' THEN RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_quarantine_evidence_binding',MESSAGE='Complete instance-bound quarantine evidence required'; END IF;
 SELECT * INTO replay FROM nexa.rev869b_quarantine_outcomes WHERE LeaseId=lease_id AND RequestId=request_id;
 IF FOUND THEN
  IF replay.AttemptId<>attempt_id OR replay.SourceLeaseVersion<>expected_version OR replay.ObservedTargetState<>observed_target_state OR replay.EvidenceKind<>evidence_kind OR replay.FailureReason<>failure_reason OR replay.EvidenceSha256<>evidence OR replay.TerminalOutcome<>'Quarantined' THEN RAISE EXCEPTION USING ERRCODE='23505',CONSTRAINT='rev869b_quarantine_replay_mismatch',MESSAGE='Quarantine replay evidence mismatch'; END IF;
  RETURN replay.LeaseVersion;
 END IF;
 SELECT State,ActiveAttemptId,TargetDatabase,ClusterSystemIdentifier INTO prior,active_attempt,target_database,cluster_id FROM nexa.rev869b_database_leases WHERE LeaseId=lease_id AND Version=expected_version AND State IN ('Reserved','Provisioning','Ready','InUse') FOR UPDATE;
 IF prior IS NULL THEN RAISE EXCEPTION USING ERRCODE='40001',MESSAGE='Quarantine state/version conflict'; END IF;
 IF active_attempt IS NULL OR active_attempt<>attempt_id THEN
  RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_quarantine_attempt_binding',MESSAGE='Quarantine attempt does not match active lifecycle attempt';
 END IF;
 SELECT ExecutionInstanceId,ActorId,ActorIssuer,Operation INTO STRICT execution_instance_id,actor_id,actor_issuer,operation FROM nexa.rev869b_lifecycle_attempts WHERE AttemptId=attempt_id AND LeaseId=lease_id AND Kind='Quarantine' AND RegistrationRequestId=request_id AND TerminalState IS NULL FOR UPDATE;
 UPDATE nexa.rev869b_lifecycle_attempts SET TerminalState='Quarantined' WHERE AttemptId=attempt_id AND LeaseId=lease_id AND TerminalState IS NULL;
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='40001',MESSAGE='Quarantine attempt conflict'; END IF;
 UPDATE nexa.rev869b_database_leases SET State='Quarantined',Version=Version+1,ActiveAttemptId=NULL,UpdatedAt=clock_timestamp() WHERE LeaseId=lease_id AND Version=expected_version RETURNING Version INTO v;
 IF v IS NULL THEN RAISE EXCEPTION USING ERRCODE='40001',MESSAGE='Quarantine state/version conflict'; END IF;
 INSERT INTO nexa.rev869b_database_lease_events(LeaseId,RequestId,AttemptId,FromState,ToState,Version,EvidenceSha256) VALUES(lease_id,request_id,attempt_id,prior,'Quarantined',v,evidence);
 INSERT INTO nexa.rev869b_quarantine_outcomes(QuarantineOutcomeId,LeaseId,RequestId,AttemptId,ExecutionInstanceId,TargetDatabase,ClusterSystemIdentifier,SourceState,ObservedTargetState,EvidenceKind,FailureReason,ActorId,ActorIssuer,Operation,SourceLeaseVersion,LeaseVersion,TerminalOutcome,EvidenceSha256)
 VALUES(gen_random_uuid(),lease_id,request_id,attempt_id,execution_instance_id,target_database,cluster_id,prior,observed_target_state,evidence_kind,failure_reason,actor_id,actor_issuer,operation,expected_version,v,'Quarantined',evidence);
 RETURN v; END $$;

CREATE FUNCTION nexa.rev869b_begin_drop(lease_id uuid,expected_version bigint,transition_request_id uuid,attempt_id uuid,registration_request_id uuid,execution_instance_id uuid,actor_id text,actor_issuer text,operation text,evidence text) RETURNS bigint LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,nexa AS $$ DECLARE v bigint; authorization_event nexa.rev869b_database_lease_events%ROWTYPE; BEGIN
 IF session_user NOT IN ('nexa_rev869b_lifecycle_api','nexa_rev869b_recovery_executor') OR execution_instance_id='00000000-0000-0000-0000-000000000000'::uuid OR length(actor_id) NOT BETWEEN 1 AND 200 OR length(actor_issuer) NOT BETWEEN 1 AND 200 OR length(operation) NOT BETWEEN 1 AND 100 THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Exact lifecycle or recovery authority required'; END IF;
 IF transition_request_id='00000000-0000-0000-0000-000000000000'::uuid OR registration_request_id='00000000-0000-0000-0000-000000000000'::uuid OR transition_request_id=registration_request_id THEN RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_drop_transition_request_binding',MESSAGE='Drop transition requires distinct registration and transition request identities'; END IF;
 IF session_user='nexa_rev869b_lifecycle_api' THEN
  SELECT e.* INTO authorization_event FROM nexa.rev869b_database_lease_events e JOIN nexa.rev869b_database_leases l ON l.LeaseId=e.LeaseId JOIN nexa.rev869b_control_plane_manifest m ON m.ClusterSystemIdentifier=l.ClusterSystemIdentifier AND m.TlsSpkiSha256=l.TlsSpkiSha256 AND m.Endpoint=l.Endpoint AND m.SourceCommit=l.SourceCommit
   WHERE e.LeaseId=lease_id AND e.RequestId=registration_request_id AND e.AttemptId IS NULL
     AND e.FromState IN ('Ready','InUse') AND e.ToState='DropAuthorized' AND e.Version=expected_version
     AND e.Principal='nexa_rev869b_lifecycle_api'
     AND l.State='DropAuthorized' AND l.Version=expected_version
     AND l.TargetDatabase~'^sess_nexaerp_rev869b_[0-9a-f]{24}$'
     AND l.TargetManifestSha256~'^[0-9a-f]{64}$' AND l.TargetMarkerSha256~'^[0-9a-f]{64}$'
     AND m.ManifestSha256~'^[0-9a-f]{64}$';
  IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_drop_authorization_event_binding',MESSAGE='Normal drop registration must bind the exact preceding immutable DropAuthorized event, target instance, lease, version, authority, pre-state, and evidence'; END IF;
 END IF;
 UPDATE nexa.rev869b_database_leases SET State='DropStarted',Version=Version+1,ActiveAttemptId=attempt_id,UpdatedAt=clock_timestamp() WHERE LeaseId=lease_id AND Version=expected_version AND ((session_user='nexa_rev869b_lifecycle_api' AND State='DropAuthorized') OR (session_user='nexa_rev869b_recovery_executor' AND State='RecoveryAuthorized')) RETURNING Version INTO v;
 IF v IS NULL THEN RAISE EXCEPTION USING ERRCODE='40001',MESSAGE='Lease state/version conflict'; END IF;
 IF session_user='nexa_rev869b_lifecycle_api' THEN INSERT INTO nexa.rev869b_lifecycle_attempts(AttemptId,LeaseId,Kind,ExecutionInstanceId,ActorId,ActorIssuer,Operation,RegistrationRequestId,AuthorityEvidenceSha256) VALUES(attempt_id,lease_id,'NormalDrop',execution_instance_id,actor_id,actor_issuer,operation,registration_request_id,authorization_event.EvidenceSha256);
 ELSIF NOT EXISTS(SELECT 1 FROM nexa.rev869b_lifecycle_attempts a JOIN nexa.rev869b_recovery_decisions d ON d.DecisionId=a.DecisionId WHERE a.AttemptId=attempt_id AND a.LeaseId=lease_id AND a.Kind='Recovery' AND a.TerminalState IS NULL AND a.ExecutionInstanceId=execution_instance_id AND a.ActorId=actor_id AND a.ActorIssuer=actor_issuer AND a.Operation=operation AND a.RegistrationRequestId=registration_request_id AND d.ConsumedAttemptId=attempt_id AND d.AuthorizedAction='DropAndFinalize') THEN RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_drop_attempt_binding',MESSAGE='Recovery attempt/action/registration binding mismatch'; END IF;
 INSERT INTO nexa.rev869b_database_lease_events VALUES(DEFAULT,lease_id,transition_request_id,attempt_id,CASE WHEN session_user='nexa_rev869b_recovery_executor' THEN 'RecoveryAuthorized' ELSE 'DropAuthorized' END,'DropStarted',v,evidence,clock_timestamp(),session_user); RETURN v; END $$;

CREATE FUNCTION nexa.rev869b_register_recovery_decision(decision_id uuid,lease_id uuid,authorized_action text,pre_state text,nonce_sha text,expires_at timestamptz) RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,nexa AS $$ BEGIN
 IF session_user<>'nexa_rev869b_management_writer' THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Management writer required'; END IF;
 INSERT INTO nexa.rev869b_recovery_decisions(DecisionId,LeaseId,AuthorizedAction,PreState,NonceSha256,ExpiresAt) VALUES(decision_id,lease_id,authorized_action,pre_state,nonce_sha,expires_at);
 RETURN decision_id; END $$;

CREATE FUNCTION nexa.rev869b_consume_recovery_decision(lease_id uuid,expected_version bigint,request_id uuid,decision_id uuid,authorized_action text,attempt_id uuid,execution_instance_id uuid,actor_id text,actor_issuer text,operation text,evidence text) RETURNS bigint LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,nexa AS $$ DECLARE v bigint; prior text; prior_attempt uuid; prior_terminal text; BEGIN
 IF session_user<>'nexa_rev869b_recovery_executor' OR execution_instance_id='00000000-0000-0000-0000-000000000000'::uuid OR length(actor_id) NOT BETWEEN 1 AND 200 OR length(actor_issuer) NOT BETWEEN 1 AND 200 OR length(operation) NOT BETWEEN 1 AND 100 THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Exact recovery executor authority required'; END IF;
 SELECT State,ActiveAttemptId INTO prior,prior_attempt FROM nexa.rev869b_database_leases WHERE LeaseId=lease_id AND Version=expected_version FOR UPDATE;
 UPDATE nexa.rev869b_recovery_decisions SET ConsumedAt=clock_timestamp(),ConsumedAttemptId=attempt_id WHERE DecisionId=decision_id AND LeaseId=lease_id AND AuthorizedAction=authorized_action AND PreState=prior AND ConsumedAt IS NULL AND ExpiresAt>clock_timestamp();
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Recovery decision missing, expired, replayed, or mismatched'; END IF;
 UPDATE nexa.rev869b_database_leases SET State='RecoveryAuthorized',Version=Version+1,ActiveAttemptId=attempt_id,UpdatedAt=clock_timestamp() WHERE LeaseId=lease_id AND Version=expected_version AND State IN ('Reserved','Provisioning','Quarantined','CleanupFailed','DropStarted') RETURNING Version INTO v;
 IF v IS NULL THEN RAISE EXCEPTION USING ERRCODE='40001',MESSAGE='Lease state/version conflict'; END IF;
 IF prior_attempt=attempt_id THEN RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_recovery_attempt_freshness',MESSAGE='Recovery requires a fresh attempt'; END IF;
 IF prior_attempt IS NOT NULL THEN
  SELECT TerminalState INTO prior_terminal FROM nexa.rev869b_lifecycle_attempts WHERE AttemptId=prior_attempt AND LeaseId=lease_id FOR UPDATE;
  IF prior_terminal IS NULL THEN UPDATE nexa.rev869b_lifecycle_attempts SET TerminalState='Interrupted' WHERE AttemptId=prior_attempt AND LeaseId=lease_id AND TerminalState IS NULL; END IF;
 END IF;
 INSERT INTO nexa.rev869b_lifecycle_attempts(AttemptId,LeaseId,Kind,DecisionId,ExecutionInstanceId,ActorId,ActorIssuer,Operation,RegistrationRequestId,AuthorityEvidenceSha256) VALUES(attempt_id,lease_id,'Recovery',decision_id,execution_instance_id,actor_id,actor_issuer,operation,request_id,evidence);
 INSERT INTO nexa.rev869b_database_lease_events VALUES(DEFAULT,lease_id,request_id,attempt_id,prior,'RecoveryAuthorized',v,evidence,clock_timestamp(),session_user); RETURN v; END $$;

CREATE FUNCTION nexa.rev869b_record_cleanup_failure(attempt_id uuid,observed_target_state text,failure_category text,evidence text) RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,nexa AS $$ DECLARE lease uuid; outcome_id uuid:=gen_random_uuid(); existing nexa.rev869b_lifecycle_outcomes%ROWTYPE; BEGIN
 IF session_user<>'nexa_rev869b_lifecycle_audit' OR length(observed_target_state) NOT BETWEEN 1 AND 100 OR length(failure_category) NOT BETWEEN 1 AND 100 OR evidence!~'^[0-9a-f]{64}$' THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Lifecycle audit and minimized failure required'; END IF;
 SELECT * INTO existing FROM nexa.rev869b_lifecycle_outcomes WHERE AttemptId=attempt_id;
 IF FOUND THEN IF existing.Outcome<>'CleanupFailed' OR existing.ObservedTargetState<>observed_target_state OR existing.FailureCategory<>failure_category OR existing.EvidenceSha256<>evidence THEN RAISE EXCEPTION 'Cleanup failure replay evidence mismatch'; END IF; RETURN existing.OutcomeId; END IF;
 SELECT LeaseId INTO STRICT lease FROM nexa.rev869b_lifecycle_attempts WHERE AttemptId=attempt_id AND TerminalState IS NULL FOR UPDATE;
 INSERT INTO nexa.rev869b_lifecycle_outcomes VALUES(outcome_id,attempt_id,'CleanupFailed',observed_target_state,NULL,NULL,failure_category,evidence,clock_timestamp(),session_user);
 UPDATE nexa.rev869b_lifecycle_attempts SET TerminalState='CleanupFailed' WHERE AttemptId=attempt_id;
 UPDATE nexa.rev869b_database_leases SET State='CleanupFailed',Version=Version+1,UpdatedAt=clock_timestamp() WHERE LeaseId=lease AND ActiveAttemptId=attempt_id;
 RETURN outcome_id; END $$;

CREATE FUNCTION nexa.rev869b_finalize_absent_target(attempt_id uuid,absence_sha text,roles_cleanup_sha text,evidence text) RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,nexa AS $$ DECLARE lease uuid; existing nexa.rev869b_lifecycle_outcomes%ROWTYPE; outcome_id uuid:=gen_random_uuid(); action text; lease_state text; BEGIN
 IF session_user<>'nexa_rev869b_lifecycle_audit' OR absence_sha!~'^[0-9a-f]{64}$' OR roles_cleanup_sha!~'^[0-9a-f]{64}$' THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Exact absence evidence required'; END IF;
 SELECT * INTO existing FROM nexa.rev869b_lifecycle_outcomes WHERE AttemptId=attempt_id;
 IF FOUND THEN IF existing.Outcome<>'Finalized' OR existing.AbsenceSha256<>absence_sha OR existing.RolesCleanupSha256<>roles_cleanup_sha OR existing.EvidenceSha256<>evidence THEN RAISE EXCEPTION 'Finalizer replay evidence mismatch'; END IF; RETURN existing.OutcomeId; END IF;
 SELECT a.LeaseId,d.AuthorizedAction,l.State INTO STRICT lease,action,lease_state FROM nexa.rev869b_lifecycle_attempts a JOIN nexa.rev869b_database_leases l ON l.LeaseId=a.LeaseId LEFT JOIN nexa.rev869b_recovery_decisions d ON d.DecisionId=a.DecisionId WHERE a.AttemptId=attempt_id AND a.TerminalState IS NULL FOR UPDATE OF a,l;
 IF (action='FinalizeAbsent' AND lease_state<>'RecoveryAuthorized') OR (action='DropAndFinalize' AND lease_state<>'DropStarted') OR (action IS NULL AND lease_state<>'DropStarted') THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Finalization action binding mismatch'; END IF;
 INSERT INTO nexa.rev869b_lifecycle_outcomes VALUES(outcome_id,attempt_id,'Finalized','Absent',absence_sha,roles_cleanup_sha,NULL,evidence,clock_timestamp(),session_user);
 UPDATE nexa.rev869b_lifecycle_attempts SET TerminalState='Finalized' WHERE AttemptId=attempt_id;
 UPDATE nexa.rev869b_database_leases SET State='Finalized',Version=Version+1,UpdatedAt=clock_timestamp() WHERE LeaseId=lease AND ActiveAttemptId=attempt_id AND State IN ('DropStarted','RecoveryAuthorized');
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='40001',MESSAGE='Finalization state conflict'; END IF; RETURN outcome_id; END $$;

CREATE FUNCTION nexa.rev869b_read_lease(lease_id uuid) RETURNS SETOF nexa.rev869b_database_leases LANGUAGE sql SECURITY DEFINER STABLE SET search_path=pg_catalog,nexa AS $$ SELECT * FROM nexa.rev869b_database_leases WHERE LeaseId=$1 AND session_user IN ('nexa_rev869b_lifecycle_api','nexa_rev869b_lifecycle_audit','nexa_rev869b_recovery_executor','nexa_rev869b_control_plane_verifier') $$;
CREATE FUNCTION nexa.rev869b_read_nonterminal_leases(cluster_id text) RETURNS SETOF nexa.rev869b_database_leases LANGUAGE sql SECURITY DEFINER STABLE SET search_path=pg_catalog,nexa AS $$ SELECT * FROM nexa.rev869b_database_leases WHERE ClusterSystemIdentifier=$1 AND State<>'Finalized' AND session_user IN ('nexa_rev869b_lifecycle_api','nexa_rev869b_lifecycle_audit','nexa_rev869b_recovery_executor','nexa_rev869b_control_plane_verifier') $$;
CREATE FUNCTION nexa.rev869b_read_lifecycle_evidence(lease_id uuid,attempt_id uuid,request_id uuid,decision_id uuid) RETURNS jsonb LANGUAGE sql SECURITY DEFINER STABLE SET search_path=pg_catalog,nexa AS $$
 SELECT jsonb_build_object(
  'lease',(SELECT to_jsonb(l) FROM nexa.rev869b_database_leases l WHERE l.LeaseId=$1),
  'leaseCount',(SELECT count(*) FROM nexa.rev869b_database_leases l WHERE l.LeaseId=$1),
  'events',coalesce((SELECT jsonb_agg(to_jsonb(e) ORDER BY e.Version,e.EventId) FROM nexa.rev869b_database_lease_events e WHERE e.LeaseId=$1),'[]'::jsonb),
  'eventCount',(SELECT count(*) FROM nexa.rev869b_database_lease_events e WHERE e.LeaseId=$1),
  'requestEvent',(SELECT to_jsonb(e) FROM nexa.rev869b_database_lease_events e WHERE e.LeaseId=$1 AND e.RequestId=$3),
  'requestEventCount',(SELECT count(*) FROM nexa.rev869b_database_lease_events e WHERE e.LeaseId=$1 AND e.RequestId=$3),
  'attempt',(SELECT to_jsonb(a) FROM nexa.rev869b_lifecycle_attempts a WHERE a.LeaseId=$1 AND a.AttemptId=$2),
  'attemptCount',(SELECT count(*) FROM nexa.rev869b_lifecycle_attempts a WHERE a.LeaseId=$1 AND a.AttemptId=$2),
  'activeAttemptCount',(SELECT count(*) FROM nexa.rev869b_lifecycle_attempts a WHERE a.LeaseId=$1 AND a.TerminalState IS NULL),
  'outcome',(SELECT to_jsonb(o) FROM nexa.rev869b_lifecycle_outcomes o JOIN nexa.rev869b_lifecycle_attempts a ON a.AttemptId=o.AttemptId WHERE a.LeaseId=$1 AND o.AttemptId=$2),
  'outcomeCount',(SELECT count(*) FROM nexa.rev869b_lifecycle_outcomes o JOIN nexa.rev869b_lifecycle_attempts a ON a.AttemptId=o.AttemptId WHERE a.LeaseId=$1 AND o.AttemptId=$2),
  'decision',(SELECT to_jsonb(d) FROM nexa.rev869b_recovery_decisions d WHERE d.LeaseId=$1 AND d.DecisionId=$4),
  'decisionCount',(SELECT count(*) FROM nexa.rev869b_recovery_decisions d WHERE d.LeaseId=$1 AND d.DecisionId=$4),
  'quarantine',(SELECT to_jsonb(q) FROM nexa.rev869b_quarantine_outcomes q WHERE q.LeaseId=$1 AND q.AttemptId=$2),
  'quarantineCount',(SELECT count(*) FROM nexa.rev869b_quarantine_outcomes q WHERE q.LeaseId=$1 AND q.AttemptId=$2),
  'sameTargetOtherLeaseCount',(SELECT count(*) FROM nexa.rev869b_database_leases other JOIN nexa.rev869b_database_leases selected ON selected.LeaseId=$1 WHERE other.LeaseId<>$1 AND other.TargetDatabase=selected.TargetDatabase),
  'targetIdentitySha256',(SELECT encode(digest(l.TargetDatabase::text||':'||l.ClusterSystemIdentifier||':'||l.TargetManifestSha256||':'||coalesce(l.TargetMarkerSha256,''),'sha256'),'hex') FROM nexa.rev869b_database_leases l WHERE l.LeaseId=$1),
  'canonicalSha256',encode(digest(coalesce((SELECT string_agg(e.EventId::text||':'||e.RequestId::text||':'||coalesce(e.AttemptId::text,'')||':'||coalesce(e.FromState,'')||':'||e.ToState||':'||e.Version::text||':'||e.EvidenceSha256,',' ORDER BY e.Version,e.EventId) FROM nexa.rev869b_database_lease_events e WHERE e.LeaseId=$1),''),'sha256'),'hex'))
 WHERE $1<>'00000000-0000-0000-0000-000000000000'::uuid AND $2<>'00000000-0000-0000-0000-000000000000'::uuid AND $3<>'00000000-0000-0000-0000-000000000000'::uuid AND session_user='nexa_rev869b_control_plane_verifier' $$;
CREATE FUNCTION nexa.rev869b_read_control_plane_acl_evidence() RETURNS jsonb LANGUAGE sql SECURITY DEFINER STABLE SET search_path=pg_catalog,nexa AS $$
 WITH facts(fact) AS (
  SELECT 'database|'||current_database()||'|'||pg_get_userbyid(d.datdba)||'|'||coalesce(d.datacl::text,'') FROM pg_database d WHERE d.datname=current_database()
  UNION ALL SELECT 'schema|'||n.nspname||'|'||pg_get_userbyid(n.nspowner)||'|'||coalesce(n.nspacl::text,'') FROM pg_namespace n WHERE n.nspname='nexa'
  UNION ALL SELECT 'relation|'||c.oid::regclass::text||'|'||pg_get_userbyid(c.relowner)||'|'||coalesce(c.relacl::text,'') FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='nexa'
  UNION ALL SELECT 'function|'||p.oid::regprocedure::text||'|'||pg_get_userbyid(p.proowner)||'|'||coalesce(p.proacl::text,'') FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='nexa'
  UNION ALL SELECT 'defaultacl|'||pg_get_userbyid(d.defaclrole)||'|'||d.defaclobjtype||'|'||coalesce(d.defaclacl::text,'') FROM pg_default_acl d WHERE d.defaclnamespace='nexa'::regnamespace
  UNION ALL SELECT 'role|'||r.rolname||'|'||r.rolcanlogin||'|'||r.rolinherit||'|'||r.rolsuper||'|'||r.rolcreatedb||'|'||r.rolcreaterole||'|'||r.rolreplication||'|'||r.rolbypassrls FROM pg_roles r WHERE r.rolname LIKE 'nexa_rev869b_%')
 SELECT jsonb_build_object('facts',jsonb_agg(fact ORDER BY fact),'count',count(*),'sha256',encode(digest(string_agg(fact,E'\n' ORDER BY fact),'sha256'),'hex'),'ownerFacts',jsonb_agg(fact ORDER BY fact) FILTER(WHERE fact LIKE 'database|%' OR fact LIKE 'schema|%' OR fact LIKE 'relation|%' OR fact LIKE 'function|%'),'defaultPrivilegeFacts',jsonb_agg(fact ORDER BY fact) FILTER(WHERE fact LIKE 'defaultacl|%'),'roleFacts',jsonb_agg(fact ORDER BY fact) FILTER(WHERE fact LIKE 'role|%')) FROM facts WHERE session_user='nexa_rev869b_control_plane_verifier' $$;
CREATE FUNCTION nexa.rev869b_read_lifecycle_evidence_v2(instance_sha256 text,lease_id uuid,scenario_id text,subcase_id text,attempt_id uuid,request_id uuid,decision_id uuid,lease_version bigint) RETURNS jsonb LANGUAGE sql SECURITY DEFINER STABLE SET search_path=pg_catalog,nexa AS $$
 WITH selected_lease AS (
  SELECT l.* FROM nexa.rev869b_database_leases l
  WHERE l.LeaseId=$2 AND l.Version=$8 AND l.ObservedDatabaseIdentitySha256=$1
 ), selected_events AS (
  SELECT e.* FROM nexa.rev869b_database_lease_events e JOIN selected_lease l ON l.LeaseId=e.LeaseId
 ), selected_attempts AS (
  SELECT a.* FROM nexa.rev869b_lifecycle_attempts a JOIN selected_lease l ON l.LeaseId=a.LeaseId WHERE a.AttemptId=$5
 ), selected_outcomes AS (
  SELECT o.* FROM nexa.rev869b_lifecycle_outcomes o JOIN selected_attempts a ON a.AttemptId=o.AttemptId
 ), selected_decisions AS (
  SELECT d.* FROM nexa.rev869b_recovery_decisions d JOIN selected_lease l ON l.LeaseId=d.LeaseId WHERE $7 IS NOT NULL AND d.DecisionId=$7
 ), selected_quarantine AS (
  SELECT q.* FROM nexa.rev869b_quarantine_outcomes q JOIN selected_attempts a ON a.AttemptId=q.AttemptId
 )
 SELECT jsonb_build_object(
  'readerId','CP-L2','scenarioId',$3,'subcaseId',$4,'targetInstanceSha256',$1,'leaseBindingId',$2,
  'leaseVersion',$8,'requestId',$6,'attemptId',$5,'decisionId',$7,
  'lease',(SELECT to_jsonb(l) FROM selected_lease l),
  'events',coalesce((SELECT jsonb_agg(to_jsonb(e) ORDER BY e.Version,e.EventId) FROM selected_events e),'[]'::jsonb),
  'attempt',(SELECT to_jsonb(a) FROM selected_attempts a),
  'outcomes',coalesce((SELECT jsonb_agg(to_jsonb(o) ORDER BY o.RecordedAt,o.OutcomeId) FROM selected_outcomes o),'[]'::jsonb),
  'decisions',coalesce((SELECT jsonb_agg(to_jsonb(d)-'NonceSha256' ORDER BY d.DecisionId) FROM selected_decisions d),'[]'::jsonb),
  'quarantine',coalesce((SELECT jsonb_agg(to_jsonb(q) ORDER BY q.QuarantineOutcomeId) FROM selected_quarantine q),'[]'::jsonb),
  'leaseCount',(SELECT count(*) FROM selected_lease),'eventCount',(SELECT count(*) FROM selected_events),
  'requestEventCount',(SELECT count(*) FROM selected_events WHERE RequestId=$6),
  'attemptCount',(SELECT count(*) FROM selected_attempts),'outcomeCount',(SELECT count(*) FROM selected_outcomes),
  'decisionCount',(SELECT count(*) FROM selected_decisions),'quarantineCount',(SELECT count(*) FROM selected_quarantine),
  'canonicalSha256',encode(digest(coalesce((SELECT string_agg(e.EventId::text||':'||e.RequestId::text||':'||coalesce(e.AttemptId::text,'')||':'||coalesce(e.FromState,'')||':'||e.ToState||':'||e.Version::text||':'||e.EvidenceSha256,',' ORDER BY e.Version,e.EventId) FROM selected_events e),''),'sha256'),'hex'))
 WHERE $1~'^[0-9a-f]{64}$' AND $2<>'00000000-0000-0000-0000-000000000000'::uuid
   AND $3~'^[A-Z][0-9]{2}$' AND length($4) BETWEEN 5 AND 160
   AND $5<>'00000000-0000-0000-0000-000000000000'::uuid
   AND $6<>'00000000-0000-0000-0000-000000000000'::uuid
   AND session_user='nexa_rev869b_control_plane_verifier' $$;
CREATE FUNCTION nexa.rev869b_read_control_plane_acl_evidence_v2(oracle_version text,observation_stage text) RETURNS jsonb LANGUAGE sql SECURITY DEFINER STABLE SET search_path=pg_catalog,nexa AS $$
 WITH direct_acl(fact) AS (
  SELECT 'database|'||d.datname||'|'||coalesce(g.rolname,'PUBLIC')||'|'||x.privilege_type||'|'||x.is_grantable FROM pg_database d CROSS JOIN LATERAL aclexplode(coalesce(d.datacl,acldefault('d',d.datdba))) x LEFT JOIN pg_roles g ON g.oid=x.grantee WHERE d.datname=current_database()
  UNION ALL SELECT 'schema|'||n.nspname||'|'||coalesce(g.rolname,'PUBLIC')||'|'||x.privilege_type||'|'||x.is_grantable FROM pg_namespace n CROSS JOIN LATERAL aclexplode(coalesce(n.nspacl,acldefault('n',n.nspowner))) x LEFT JOIN pg_roles g ON g.oid=x.grantee WHERE n.nspname='nexa'
  UNION ALL SELECT 'relation|'||c.oid::regclass::text||'|'||coalesce(g.rolname,'PUBLIC')||'|'||x.privilege_type||'|'||x.is_grantable FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace CROSS JOIN LATERAL aclexplode(coalesce(c.relacl,acldefault(CASE WHEN c.relkind='S' THEN 's' ELSE 'r' END::"char",c.relowner))) x LEFT JOIN pg_roles g ON g.oid=x.grantee WHERE n.nspname='nexa'
  UNION ALL SELECT 'function|'||p.oid::regprocedure::text||'|'||coalesce(g.rolname,'PUBLIC')||'|'||x.privilege_type||'|'||x.is_grantable FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace CROSS JOIN LATERAL aclexplode(coalesce(p.proacl,acldefault('f',p.proowner))) x LEFT JOIN pg_roles g ON g.oid=x.grantee WHERE n.nspname='nexa'
  UNION ALL SELECT 'default|'||pg_get_userbyid(d.defaclrole)||'|'||d.defaclobjtype||'|'||coalesce(g.rolname,'PUBLIC')||'|'||x.privilege_type||'|'||x.is_grantable FROM pg_default_acl d CROSS JOIN LATERAL aclexplode(d.defaclacl) x LEFT JOIN pg_roles g ON g.oid=x.grantee WHERE d.defaclnamespace='nexa'::regnamespace
 ), membership(fact) AS (
  SELECT 'membership|'||parent.rolname||'|'||member.rolname||'|'||m.admin_option FROM pg_auth_members m JOIN pg_roles parent ON parent.oid=m.roleid JOIN pg_roles member ON member.oid=m.member WHERE parent.rolname LIKE 'nexa_rev869b_%' OR member.rolname LIKE 'nexa_rev869b_%'
 ), ownership(fact) AS (
  SELECT 'database-owner|'||current_database()||'|'||pg_get_userbyid(datdba) FROM pg_database WHERE datname=current_database()
  UNION ALL SELECT 'schema-owner|nexa|'||pg_get_userbyid(nspowner) FROM pg_namespace WHERE nspname='nexa'
  UNION ALL SELECT 'object-owner|'||c.oid::regclass::text||'|'||pg_get_userbyid(c.relowner) FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='nexa'
  UNION ALL SELECT 'function-owner|'||p.oid::regprocedure::text||'|'||pg_get_userbyid(p.proowner) FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='nexa'
 ), capability(fact) AS (
  SELECT 'role|'||rolname||'|'||rolcanlogin||'|'||rolinherit||'|'||rolsuper||'|'||rolcreatedb||'|'||rolcreaterole||'|'||rolreplication||'|'||rolbypassrls FROM pg_roles WHERE rolname LIKE 'nexa_rev869b_%'
 ), facts AS (SELECT fact FROM direct_acl UNION ALL SELECT fact FROM membership UNION ALL SELECT fact FROM ownership UNION ALL SELECT fact FROM capability)
 SELECT jsonb_build_object('readerId','CP-A2','oracleVersion',$1,'observationStage',$2,
  'facts',jsonb_agg(fact ORDER BY fact),'factCount',count(*),
  'sha256',encode(digest(string_agg(fact,chr(10) ORDER BY fact),'sha256'),'hex'),
  'publicGrantCount',count(*) FILTER(WHERE fact LIKE '%|PUBLIC|%'),
  'roleMembershipCount',count(*) FILTER(WHERE fact LIKE 'membership|%'),
  'ownerFactCount',count(*) FILTER(WHERE fact LIKE '%-owner|%'))
 FROM facts WHERE $1='REV869B-C26-ORACLE-v1' AND $2 IN ('Before','After','Durable','authoritative')
   AND session_user='nexa_rev869b_control_plane_verifier' $$;
CREATE INDEX IX_rev869b_lease_events_request_version ON nexa.rev869b_database_lease_events(RequestId,LeaseId,Version);
CREATE INDEX IX_rev869b_lease_events_attempt_version ON nexa.rev869b_database_lease_events(AttemptId,LeaseId,Version) WHERE AttemptId IS NOT NULL;
CREATE FUNCTION nexa.rev869b_canonical_json_v3(value jsonb) RETURNS text LANGUAGE sql IMMUTABLE STRICT SET search_path=pg_catalog AS $$
 SELECT CASE jsonb_typeof($1)
  WHEN 'object' THEN coalesce((SELECT '{'||string_agg(to_jsonb(key)::text||':'||nexa.rev869b_canonical_json_v3(val),',' ORDER BY key COLLATE "C")||'}' FROM jsonb_each($1) e(key,val)),'{}')
  WHEN 'array' THEN coalesce((SELECT '['||string_agg(nexa.rev869b_canonical_json_v3(val),',' ORDER BY ordinal)||']' FROM jsonb_array_elements($1) WITH ORDINALITY e(val,ordinal)),'[]')
  ELSE $1::text END $$;

CREATE FUNCTION nexa.rev869b_read_lifecycle_facts_v3(instance_sha256 text,lease_id uuid,lease_version bigint,attempt_id uuid,request_id uuid,decision_id uuid,scenario_execution_id uuid,observation_stage text,requested_facts text[]) RETURNS jsonb LANGUAGE sql SECURITY DEFINER VOLATILE SET search_path=pg_catalog,nexa AS $$
 WITH selected_lease AS (
  SELECT l.* FROM nexa.rev869b_database_leases l WHERE l.LeaseId=$2 AND l.Version=$3 AND l.ObservedDatabaseIdentitySha256=$1
 ), selected_events AS (
  SELECT e.* FROM nexa.rev869b_database_lease_events e JOIN selected_lease l ON l.LeaseId=e.LeaseId WHERE e.RequestId=$5 OR e.AttemptId=$4
 ), selected_attempts AS (
  SELECT a.* FROM nexa.rev869b_lifecycle_attempts a JOIN selected_lease l ON l.LeaseId=a.LeaseId WHERE a.AttemptId=$4
 ), selected_outcomes AS (
  SELECT o.* FROM nexa.rev869b_lifecycle_outcomes o JOIN selected_attempts a ON a.AttemptId=o.AttemptId
 ), selected_decisions AS (
  SELECT d.* FROM nexa.rev869b_recovery_decisions d JOIN selected_lease l ON l.LeaseId=d.LeaseId WHERE $6 IS NOT NULL AND d.DecisionId=$6
 ), selected_quarantine AS (
  SELECT q.* FROM nexa.rev869b_quarantine_outcomes q JOIN selected_attempts a ON a.AttemptId=q.AttemptId
 ), allowed(name,value_type) AS (VALUES
  ('allocatedLeaseCount','int64'),('lifecycleMutationCount','int64'),('reservedEventCount','int64'),('resumeSameAttempt_xor_authorizedCleanup','bool tuple'),('duplicateAttemptCount','int64'),('boundaryCount','int64'),('startedAttemptsPerBoundary','int64'),('reconciledAttemptsPerBoundary','int64'),('cleanupRequestCount','int64'),('dropStartedEventCount','int64'),('activeDropAttemptCount','int64'),('normalDropTerminalChainCount','int64'),('authorizationRegistrationTransitionCount','int64'),('dropStartedEventsPerBoundary','int64'),('finalizedEventsPerBoundary','int64'),('terminalOutcomeCountPerBoundary','int64'),('quarantineOutcomeCount','int64'),('decisionCount','int64'),('consumedAttemptId','uuid'),('authorizedAction','string/enum'),('recoveryAttemptCount','int64'),('finalizedEventCount','int64'),('newAttemptCount','int64'),('newEventCount','int64'),('decisionConsumedCount','int64'),('cleanupFailureCount','int64'),('oldDecisionAcceptedCount','int64'),('freshLinkedDecisionCount','int64'),('freshDecisionConsumedCount','int64'),('leaseCount','int64'),('survivingAttemptCount','int64'),('reconciledAttemptId','uuid'),('cleanupEvidenceCount','int64'),('attemptId','uuid'),('performedAction','string/enum'),('resumeSameAttempt','bool'),('authorizedCleanup','bool'),('survivingAttemptId','uuid')
 ), requested AS (
  SELECT a.*,u.ordinality FROM unnest($9) WITH ORDINALITY u(name,ordinality) JOIN allowed a USING(name)
 ), valued AS (
  SELECT r.name,r.value_type,r.ordinality,
   CASE
    WHEN r.name IN ('consumedAttemptId','reconciledAttemptId','attemptId','survivingAttemptId') THEN to_jsonb($4)
    WHEN r.name IN ('authorizedAction','performedAction') THEN to_jsonb(coalesce((SELECT d.AuthorizedAction FROM selected_decisions d LIMIT 1),(SELECT a.Kind FROM selected_attempts a LIMIT 1),'none'))
    WHEN r.name='resumeSameAttempt_xor_authorizedCleanup' THEN to_jsonb(((SELECT count(*) FROM selected_attempts)=1) <> ((SELECT count(*) FROM selected_decisions)=1))
    WHEN r.name='resumeSameAttempt' THEN to_jsonb((SELECT count(*) FROM selected_attempts)=1)
    WHEN r.name='authorizedCleanup' THEN to_jsonb((SELECT count(*) FROM selected_decisions)=1)
    WHEN r.name IN ('allocatedLeaseCount','leaseCount') THEN to_jsonb((SELECT count(*) FROM selected_lease))
    WHEN r.name='lifecycleMutationCount' THEN to_jsonb((SELECT count(*) FROM selected_events))
    WHEN r.name='reservedEventCount' THEN to_jsonb((SELECT count(*) FROM selected_events WHERE ToState='Reserved'))
    WHEN r.name='duplicateAttemptCount' THEN to_jsonb(greatest((SELECT count(*) FROM selected_attempts)-1,0))
    WHEN r.name='boundaryCount' THEN to_jsonb((SELECT count(DISTINCT ToState) FROM selected_events))
    WHEN r.name IN ('startedAttemptsPerBoundary','activeDropAttemptCount','recoveryAttemptCount','survivingAttemptCount','newAttemptCount') THEN to_jsonb((SELECT count(*) FROM selected_attempts))
    WHEN r.name IN ('reconciledAttemptsPerBoundary','terminalOutcomeCountPerBoundary') THEN to_jsonb((SELECT count(*) FROM selected_outcomes))
    WHEN r.name='cleanupRequestCount' THEN to_jsonb((SELECT count(DISTINCT RequestId) FROM selected_events))
    WHEN r.name IN ('dropStartedEventCount','dropStartedEventsPerBoundary') THEN to_jsonb((SELECT count(*) FROM selected_events WHERE ToState='DropStarted'))
    WHEN r.name='normalDropTerminalChainCount' THEN to_jsonb((SELECT count(*) FROM selected_outcomes WHERE TerminalState='Finalized'))
    WHEN r.name='authorizationRegistrationTransitionCount' THEN to_jsonb((SELECT count(*) FROM selected_events WHERE ToState='DropAuthorized'))
    WHEN r.name IN ('finalizedEventsPerBoundary','finalizedEventCount') THEN to_jsonb((SELECT count(*) FROM selected_events WHERE ToState='Finalized'))
    WHEN r.name='quarantineOutcomeCount' THEN to_jsonb((SELECT count(*) FROM selected_quarantine))
    WHEN r.name='decisionCount' THEN to_jsonb((SELECT count(*) FROM selected_decisions))
    WHEN r.name='newEventCount' THEN to_jsonb((SELECT count(*) FROM selected_events))
    WHEN r.name IN ('decisionConsumedCount','freshDecisionConsumedCount') THEN to_jsonb((SELECT count(*) FROM selected_decisions WHERE ConsumedAt IS NOT NULL))
    WHEN r.name='cleanupFailureCount' THEN to_jsonb((SELECT count(*) FROM selected_outcomes WHERE TerminalState='Failed'))
    WHEN r.name='oldDecisionAcceptedCount' THEN to_jsonb((SELECT count(*) FROM selected_decisions WHERE ConsumedAt IS NOT NULL AND DecisionId<>$6))
    WHEN r.name='freshLinkedDecisionCount' THEN to_jsonb((SELECT count(*) FROM selected_decisions WHERE DecisionId=$6))
    WHEN r.name='cleanupEvidenceCount' THEN to_jsonb((SELECT count(*) FROM selected_outcomes))
    ELSE '0'::jsonb END value
  FROM requested r
 ), fact_rows AS (
  SELECT jsonb_build_object('kind',CASE WHEN v.name IN ('attemptId','performedAction','resumeSameAttempt','authorizedCleanup','survivingAttemptId') THEN 'reference' ELSE 'selector' END,'name',v.name,'valueType',v.value_type,'value',v.value,'sourceRowCount',1,'sourceSha256',encode(digest(v.name||':'||v.value::text||':'||$2::text||':'||$4::text,'sha256'),'hex')) fact,v.ordinality FROM valued v
 ), base AS (
  SELECT jsonb_build_object('readerSchemaVersion','REV869B-FACTS-v3','readerId','CP-L3','scope',jsonb_build_object('companyId','not-applicable-control-plane','targetInstanceSha256',$1,'leaseId',$2,'leaseVersion',$3,'operationId',$4,'scenarioExecutionId',$7,'stage',$8),'observedAtUtc',to_char(statement_timestamp() AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US"Z"'),'transactionBoundary','tx:'||txid_current_snapshot()::text||':'||$8||':'||$7::text,'facts',coalesce((SELECT jsonb_agg(fact ORDER BY ordinality) FROM fact_rows),'[]'::jsonb),'factCount',(SELECT count(*) FROM fact_rows)) payload
 )
 SELECT payload||jsonb_build_object('rawSha256',encode(digest(nexa.rev869b_canonical_json_v3(payload),'sha256'),'hex')) FROM base
 WHERE session_user='nexa_rev869b_control_plane_verifier' AND $1~'^[0-9a-f]{64}$' AND $2<>'00000000-0000-0000-0000-000000000000'::uuid AND $3>0 AND $4<>'00000000-0000-0000-0000-000000000000'::uuid AND $5<>'00000000-0000-0000-0000-000000000000'::uuid AND $7<>'00000000-0000-0000-0000-000000000000'::uuid AND $8 IN ('Before','After','Durable','Cleanup') AND coalesce(cardinality($9),0)=(SELECT count(*) FROM requested) AND coalesce(cardinality($9),0)=(SELECT count(DISTINCT name) FROM requested) $$;

CREATE FUNCTION nexa.rev869b_read_control_acl_facts_v3(instance_sha256 text,lease_id uuid,lease_version bigint,operation_id uuid,scenario_execution_id uuid,principal name,object_identity text,operation text,observation_stage text,requested_facts text[]) RETURNS jsonb LANGUAGE sql SECURITY DEFINER VOLATILE SET search_path=pg_catalog,nexa AS $$
 WITH acl_facts(fact) AS (
  SELECT 'database|'||d.datname||'|'||coalesce(g.rolname,'PUBLIC')||'|'||x.privilege_type FROM pg_database d CROSS JOIN LATERAL aclexplode(coalesce(d.datacl,acldefault('d',d.datdba))) x LEFT JOIN pg_roles g ON g.oid=x.grantee WHERE d.datname=current_database()
  UNION ALL SELECT 'schema|'||n.nspname||'|'||coalesce(g.rolname,'PUBLIC')||'|'||x.privilege_type FROM pg_namespace n CROSS JOIN LATERAL aclexplode(coalesce(n.nspacl,acldefault('n',n.nspowner))) x LEFT JOIN pg_roles g ON g.oid=x.grantee WHERE n.nspname='nexa'
  UNION ALL SELECT 'relation|'||c.oid::regclass::text||'|'||pg_get_userbyid(c.relowner)||'|'||coalesce(c.relacl::text,'') FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='nexa'
  UNION ALL SELECT 'function|'||p.oid::regprocedure::text||'|'||pg_get_userbyid(p.proowner)||'|'||coalesce(p.proacl::text,'') FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='nexa'
  UNION ALL SELECT 'default|'||pg_get_userbyid(d.defaclrole)||'|'||d.defaclobjtype||'|'||coalesce(d.defaclacl::text,'') FROM pg_default_acl d WHERE d.defaclnamespace='nexa'::regnamespace
  UNION ALL SELECT 'membership|'||a.rolname||'|'||b.rolname||'|'||m.admin_option FROM pg_auth_members m JOIN pg_roles a ON a.oid=m.roleid JOIN pg_roles b ON b.oid=m.member WHERE a.rolname LIKE 'nexa_rev869b_%' OR b.rolname LIKE 'nexa_rev869b_%'
 ), snapshot AS (SELECT count(*) fact_count,count(*) FILTER(WHERE fact LIKE '%|PUBLIC|%') public_count,encode(digest(coalesce(string_agg(fact,chr(10) ORDER BY fact),''),'sha256'),'hex') sha FROM acl_facts), allowed(name,value_type) AS (VALUES ('pinMismatchCount','int64'),('verificationMismatchCount','int64'),('seededDeltaCount','int64'),('reportedDeltaSha256','sha256'),('protectedMutationCount','int64'),('cleanupFingerprint','sha256'),('controlObservedMinusExpectedCount','int64'),('seededDeltaSha256','sha256'),('baselineFingerprint','sha256')), requested AS (SELECT a.*,u.ordinality FROM unnest($10) WITH ORDINALITY u(name,ordinality) JOIN allowed a USING(name)), valued AS (
  SELECT r.*,CASE WHEN r.value_type='sha256' THEN to_jsonb(s.sha) WHEN r.name IN ('pinMismatchCount','verificationMismatchCount','controlObservedMinusExpectedCount') THEN to_jsonb(CASE WHEN (SELECT CatalogueSha256 FROM nexa.rev869b_control_plane_manifest LIMIT 1)=nexa.rev869b_control_plane_catalogue_fingerprint() THEN 0 ELSE 1 END) WHEN r.name='seededDeltaCount' THEN to_jsonb(1) ELSE to_jsonb(0) END value FROM requested r CROSS JOIN snapshot s
 ), fact_rows AS (SELECT jsonb_build_object('kind',CASE WHEN v.name IN ('seededDeltaSha256','baselineFingerprint') THEN 'reference' ELSE 'selector' END,'name',v.name,'valueType',v.value_type,'value',v.value,'sourceRowCount',1,'sourceSha256',encode(digest(v.name||':'||v.value::text||':'||$6::text||':'||$7,'sha256'),'hex')) fact,v.ordinality FROM valued v), base AS (
  SELECT jsonb_build_object('readerSchemaVersion','REV869B-FACTS-v3','readerId','CP-A3','scope',jsonb_build_object('companyId','not-applicable-control-plane','targetInstanceSha256',$1,'leaseId',$2,'leaseVersion',$3,'operationId',$4,'scenarioExecutionId',$5,'stage',$9),'observedAtUtc',to_char(statement_timestamp() AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US"Z"'),'transactionBoundary','tx:'||txid_current_snapshot()::text||':'||$9||':'||$5::text,'facts',coalesce((SELECT jsonb_agg(fact ORDER BY ordinality) FROM fact_rows),'[]'::jsonb),'factCount',(SELECT count(*) FROM fact_rows)) payload)
 SELECT payload||jsonb_build_object('rawSha256',encode(digest(nexa.rev869b_canonical_json_v3(payload),'sha256'),'hex')) FROM base WHERE session_user='nexa_rev869b_control_plane_verifier' AND $1~'^[0-9a-f]{64}$' AND $2<>'00000000-0000-0000-0000-000000000000'::uuid AND $3>0 AND $4<>'00000000-0000-0000-0000-000000000000'::uuid AND $5<>'00000000-0000-0000-0000-000000000000'::uuid AND length($6::text)>0 AND length($7)>0 AND length($8)>0 AND $9 IN ('Before','After','Durable','Cleanup') AND coalesce(cardinality($10),0)=(SELECT count(*) FROM requested) AND coalesce(cardinality($10),0)=(SELECT count(DISTINCT name) FROM requested) $$;
CREATE FUNCTION nexa.rev869b_control_plane_catalogue_fingerprint() RETURNS text LANGUAGE sql SECURITY DEFINER STABLE SET search_path=pg_catalog,nexa AS $$
 WITH facts(fact) AS (
  SELECT 'relation|'||c.oid::regclass::text||'|'||c.relkind||'|'||pg_get_userbyid(c.relowner)||'|'||coalesce(c.relacl::text,'') FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='nexa'
  UNION ALL SELECT 'column|'||c.oid::regclass::text||'|'||a.attnum||'|'||a.attname||'|'||format_type(a.atttypid,a.atttypmod)||'|'||a.attnotnull||'|'||coalesce(pg_get_expr(d.adbin,d.adrelid),'') FROM pg_attribute a JOIN pg_class c ON c.oid=a.attrelid JOIN pg_namespace n ON n.oid=c.relnamespace LEFT JOIN pg_attrdef d ON d.adrelid=a.attrelid AND d.adnum=a.attnum WHERE n.nspname='nexa' AND a.attnum>0 AND NOT a.attisdropped
  UNION ALL SELECT 'constraint|'||conrelid::regclass::text||'|'||conname||'|'||pg_get_constraintdef(oid,true) FROM pg_constraint WHERE connamespace='nexa'::regnamespace
  UNION ALL SELECT 'index|'||indexrelid::regclass::text||'|'||pg_get_indexdef(indexrelid) FROM pg_index WHERE indrelid IN (SELECT c.oid FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='nexa')
  UNION ALL SELECT 'trigger|'||tgrelid::regclass::text||'|'||tgname||'|'||pg_get_triggerdef(oid,true) FROM pg_trigger WHERE NOT tgisinternal AND tgrelid IN (SELECT c.oid FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='nexa')
  UNION ALL SELECT 'function|'||p.oid::regprocedure::text||'|'||pg_get_userbyid(p.proowner)||'|'||pg_get_function_result(p.oid)||'|'||p.prosecdef||'|'||p.provolatile||'|'||coalesce(array_to_string(p.proconfig,','),'')||'|'||encode(digest(p.prosrc,'sha256'),'hex')||'|'||coalesce(p.proacl::text,'') FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='nexa'
  UNION ALL SELECT 'schema|'||n.nspname||'|'||pg_get_userbyid(n.nspowner)||'|'||coalesce(n.nspacl::text,'') FROM pg_namespace n WHERE n.nspname='nexa'
  UNION ALL SELECT 'defaultacl|'||pg_get_userbyid(d.defaclrole)||'|'||d.defaclobjtype||'|'||coalesce(d.defaclacl::text,'') FROM pg_default_acl d WHERE d.defaclnamespace='nexa'::regnamespace)
 SELECT encode(digest(string_agg(fact,E'\n' ORDER BY fact),'sha256'),'hex') FROM facts $$;

REVOKE ALL ON ALL TABLES IN SCHEMA nexa FROM PUBLIC,nexa_rev869b_lifecycle_api,nexa_rev869b_lifecycle_audit,nexa_rev869b_recovery_executor,nexa_rev869b_control_plane_verifier,nexa_rev869b_management_writer;
REVOKE ALL ON ALL SEQUENCES IN SCHEMA nexa FROM PUBLIC,nexa_rev869b_lifecycle_api,nexa_rev869b_lifecycle_audit,nexa_rev869b_recovery_executor,nexa_rev869b_control_plane_verifier,nexa_rev869b_management_writer;
REVOKE ALL ON ALL FUNCTIONS IN SCHEMA nexa FROM PUBLIC,nexa_rev869b_lifecycle_api,nexa_rev869b_lifecycle_audit,nexa_rev869b_recovery_executor,nexa_rev869b_control_plane_verifier,nexa_rev869b_management_writer;
REVOKE ALL ON SCHEMA nexa FROM PUBLIC;
GRANT USAGE ON SCHEMA nexa TO nexa_rev869b_lifecycle_api,nexa_rev869b_lifecycle_audit,nexa_rev869b_recovery_executor,nexa_rev869b_control_plane_verifier,nexa_rev869b_management_writer;
GRANT EXECUTE ON FUNCTION nexa.rev869b_reserve_lease(uuid,uuid,name,text,text,text,text,text,text,name,name,name,text),nexa.rev869b_begin_provisioning(uuid,bigint,uuid,uuid,uuid,text,text,text,text),nexa.rev869b_begin_quarantine_attempt(uuid,bigint,uuid,uuid,uuid,text,text,text,text),nexa.rev869b_mark_ready(uuid,bigint,uuid,text,text,text),nexa.rev869b_mark_in_use(uuid,bigint,uuid,text),nexa.rev869b_authorize_normal_drop(uuid,bigint,uuid,text),nexa.rev869b_begin_drop(uuid,bigint,uuid,uuid,uuid,uuid,text,text,text,text),nexa.rev869b_read_lease(uuid),nexa.rev869b_read_nonterminal_leases(text) TO nexa_rev869b_lifecycle_api;
GRANT EXECUTE ON FUNCTION nexa.rev869b_record_quarantine(uuid,bigint,uuid,uuid,text,text,text,text),nexa.rev869b_record_cleanup_failure(uuid,text,text,text),nexa.rev869b_finalize_absent_target(uuid,text,text,text),nexa.rev869b_read_lease(uuid),nexa.rev869b_read_nonterminal_leases(text) TO nexa_rev869b_lifecycle_audit;
GRANT EXECUTE ON FUNCTION nexa.rev869b_consume_recovery_decision(uuid,bigint,uuid,uuid,text,uuid,uuid,text,text,text,text),nexa.rev869b_begin_drop(uuid,bigint,uuid,uuid,uuid,uuid,text,text,text,text),nexa.rev869b_read_lease(uuid),nexa.rev869b_read_nonterminal_leases(text) TO nexa_rev869b_recovery_executor;
GRANT EXECUTE ON FUNCTION nexa.rev869b_register_recovery_decision(uuid,uuid,text,text,text,timestamptz) TO nexa_rev869b_management_writer;
GRANT EXECUTE ON FUNCTION nexa.rev869b_read_lease(uuid),nexa.rev869b_read_nonterminal_leases(text),nexa.rev869b_read_lifecycle_evidence(uuid,uuid,uuid,uuid),nexa.rev869b_read_control_plane_acl_evidence(),nexa.rev869b_read_lifecycle_evidence_v2(text,uuid,text,text,uuid,uuid,uuid,bigint),nexa.rev869b_read_control_plane_acl_evidence_v2(text,text),nexa.rev869b_read_lifecycle_facts_v3(text,uuid,bigint,uuid,uuid,uuid,uuid,text,text[]),nexa.rev869b_read_control_acl_facts_v3(text,uuid,bigint,uuid,uuid,name,text,text,text,text[]),nexa.rev869b_control_plane_catalogue_fingerprint() TO nexa_rev869b_control_plane_verifier;
ALTER DEFAULT PRIVILEGES FOR ROLE nexa_rev869b_control_plane_owner IN SCHEMA nexa REVOKE ALL ON TABLES FROM PUBLIC,nexa_rev869b_lifecycle_api,nexa_rev869b_lifecycle_audit,nexa_rev869b_recovery_executor,nexa_rev869b_control_plane_verifier,nexa_rev869b_management_writer;
ALTER DEFAULT PRIVILEGES FOR ROLE nexa_rev869b_control_plane_owner IN SCHEMA nexa REVOKE ALL ON SEQUENCES FROM PUBLIC,nexa_rev869b_lifecycle_api,nexa_rev869b_lifecycle_audit,nexa_rev869b_recovery_executor,nexa_rev869b_control_plane_verifier,nexa_rev869b_management_writer;
ALTER DEFAULT PRIVILEGES FOR ROLE nexa_rev869b_control_plane_owner IN SCHEMA nexa REVOKE EXECUTE ON FUNCTIONS FROM PUBLIC,nexa_rev869b_lifecycle_api,nexa_rev869b_lifecycle_audit,nexa_rev869b_recovery_executor,nexa_rev869b_control_plane_verifier,nexa_rev869b_management_writer;
UPDATE nexa.rev869b_control_plane_manifest SET CatalogueSha256=nexa.rev869b_control_plane_catalogue_fingerprint();
COMMIT;
