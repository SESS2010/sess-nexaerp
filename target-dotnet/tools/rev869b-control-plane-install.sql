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

CREATE FUNCTION nexa.rev869b_begin_drop(lease_id uuid,expected_version bigint,request_id uuid,attempt_id uuid,execution_instance_id uuid,actor_id text,actor_issuer text,operation text,evidence text) RETURNS bigint LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,nexa AS $$ DECLARE v bigint; BEGIN
 IF session_user NOT IN ('nexa_rev869b_lifecycle_api','nexa_rev869b_recovery_executor') OR execution_instance_id='00000000-0000-0000-0000-000000000000'::uuid OR length(actor_id) NOT BETWEEN 1 AND 200 OR length(actor_issuer) NOT BETWEEN 1 AND 200 OR length(operation) NOT BETWEEN 1 AND 100 THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Exact lifecycle or recovery authority required'; END IF;
 UPDATE nexa.rev869b_database_leases SET State='DropStarted',Version=Version+1,ActiveAttemptId=attempt_id,UpdatedAt=clock_timestamp() WHERE LeaseId=lease_id AND Version=expected_version AND ((session_user='nexa_rev869b_lifecycle_api' AND State='DropAuthorized') OR (session_user='nexa_rev869b_recovery_executor' AND State='RecoveryAuthorized')) RETURNING Version INTO v;
 IF v IS NULL THEN RAISE EXCEPTION USING ERRCODE='40001',MESSAGE='Lease state/version conflict'; END IF;
 IF session_user='nexa_rev869b_lifecycle_api' THEN INSERT INTO nexa.rev869b_lifecycle_attempts(AttemptId,LeaseId,Kind,ExecutionInstanceId,ActorId,ActorIssuer,Operation,RegistrationRequestId,AuthorityEvidenceSha256) VALUES(attempt_id,lease_id,'NormalDrop',execution_instance_id,actor_id,actor_issuer,operation,request_id,evidence);
 ELSIF NOT EXISTS(SELECT 1 FROM nexa.rev869b_lifecycle_attempts a JOIN nexa.rev869b_recovery_decisions d ON d.DecisionId=a.DecisionId WHERE a.AttemptId=attempt_id AND a.LeaseId=lease_id AND a.Kind='Recovery' AND a.TerminalState IS NULL AND a.ExecutionInstanceId=execution_instance_id AND a.ActorId=actor_id AND a.ActorIssuer=actor_issuer AND a.Operation=operation AND a.RegistrationRequestId=request_id AND d.ConsumedAttemptId=attempt_id AND d.AuthorizedAction='DropAndFinalize') THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Recovery attempt/action binding mismatch'; END IF;
 INSERT INTO nexa.rev869b_database_lease_events VALUES(DEFAULT,lease_id,request_id,attempt_id,CASE WHEN session_user='nexa_rev869b_recovery_executor' THEN 'RecoveryAuthorized' ELSE 'DropAuthorized' END,'DropStarted',v,evidence,clock_timestamp(),session_user); RETURN v; END $$;

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
GRANT EXECUTE ON FUNCTION nexa.rev869b_reserve_lease(uuid,uuid,name,text,text,text,text,text,text,name,name,name,text),nexa.rev869b_begin_provisioning(uuid,bigint,uuid,uuid,uuid,text,text,text,text),nexa.rev869b_begin_quarantine_attempt(uuid,bigint,uuid,uuid,uuid,text,text,text,text),nexa.rev869b_mark_ready(uuid,bigint,uuid,text,text,text),nexa.rev869b_mark_in_use(uuid,bigint,uuid,text),nexa.rev869b_authorize_normal_drop(uuid,bigint,uuid,text),nexa.rev869b_begin_drop(uuid,bigint,uuid,uuid,uuid,text,text,text,text),nexa.rev869b_read_lease(uuid),nexa.rev869b_read_nonterminal_leases(text) TO nexa_rev869b_lifecycle_api;
GRANT EXECUTE ON FUNCTION nexa.rev869b_record_quarantine(uuid,bigint,uuid,uuid,text,text,text,text),nexa.rev869b_record_cleanup_failure(uuid,text,text,text),nexa.rev869b_finalize_absent_target(uuid,text,text,text),nexa.rev869b_read_lease(uuid),nexa.rev869b_read_nonterminal_leases(text) TO nexa_rev869b_lifecycle_audit;
GRANT EXECUTE ON FUNCTION nexa.rev869b_consume_recovery_decision(uuid,bigint,uuid,uuid,text,uuid,uuid,text,text,text,text),nexa.rev869b_begin_drop(uuid,bigint,uuid,uuid,uuid,text,text,text,text),nexa.rev869b_read_lease(uuid),nexa.rev869b_read_nonterminal_leases(text) TO nexa_rev869b_recovery_executor;
GRANT EXECUTE ON FUNCTION nexa.rev869b_register_recovery_decision(uuid,uuid,text,text,text,timestamptz) TO nexa_rev869b_management_writer;
GRANT EXECUTE ON FUNCTION nexa.rev869b_read_lease(uuid),nexa.rev869b_read_nonterminal_leases(text),nexa.rev869b_control_plane_catalogue_fingerprint() TO nexa_rev869b_control_plane_verifier;
ALTER DEFAULT PRIVILEGES FOR ROLE nexa_rev869b_control_plane_owner IN SCHEMA nexa REVOKE ALL ON TABLES FROM PUBLIC,nexa_rev869b_lifecycle_api,nexa_rev869b_lifecycle_audit,nexa_rev869b_recovery_executor,nexa_rev869b_control_plane_verifier,nexa_rev869b_management_writer;
ALTER DEFAULT PRIVILEGES FOR ROLE nexa_rev869b_control_plane_owner IN SCHEMA nexa REVOKE ALL ON SEQUENCES FROM PUBLIC,nexa_rev869b_lifecycle_api,nexa_rev869b_lifecycle_audit,nexa_rev869b_recovery_executor,nexa_rev869b_control_plane_verifier,nexa_rev869b_management_writer;
ALTER DEFAULT PRIVILEGES FOR ROLE nexa_rev869b_control_plane_owner IN SCHEMA nexa REVOKE EXECUTE ON FUNCTIONS FROM PUBLIC,nexa_rev869b_lifecycle_api,nexa_rev869b_lifecycle_audit,nexa_rev869b_recovery_executor,nexa_rev869b_control_plane_verifier,nexa_rev869b_management_writer;
UPDATE nexa.rev869b_control_plane_manifest SET CatalogueSha256=nexa.rev869b_control_plane_catalogue_fingerprint();
COMMIT;
