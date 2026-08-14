\set ON_ERROR_STOP on
BEGIN;
DO $guard$
BEGIN
  IF current_database()<>'sess_nexaerp_rev869b_control_plane' OR
     session_user NOT IN ('nexa_rev869b_control_plane_owner','nexa_rev869b_provisioning_administrator') THEN
    RAISE EXCEPTION 'Exact REV869B control-plane database and provisioning owner are required';
  END IF;
END $guard$;
SET LOCAL ROLE nexa_rev869b_control_plane_owner;
CREATE SCHEMA nexa AUTHORIZATION nexa_rev869b_control_plane_owner;
CREATE EXTENSION IF NOT EXISTS pgcrypto WITH SCHEMA public;
REVOKE ALL ON SCHEMA public FROM PUBLIC;
REVOKE ALL ON SCHEMA nexa FROM PUBLIC;

CREATE TABLE nexa.rev869b_database_leases(
  "LeaseId" uuid PRIMARY KEY,"DatabaseName" name NOT NULL UNIQUE,"RunId" text NOT NULL UNIQUE,
  "OwnershipTokenHash" text NOT NULL CHECK ("OwnershipTokenHash"~'^[0-9A-F]{64}$'),
  "FixtureFamily" text NOT NULL,"ScenarioHash" text NOT NULL CHECK ("ScenarioHash"~'^[0-9A-F]{64}$'),
  "SourceDatabase" name NOT NULL,"SourceFingerprint" text NOT NULL CHECK ("SourceFingerprint"~'^[0-9A-F]{64}$'),
  "SourceCommitFingerprint" text NOT NULL CHECK ("SourceCommitFingerprint"~'^[0-9a-f]{40}$'),
  "MigrationId" text NOT NULL,"MigrationFingerprint" text NOT NULL CHECK ("MigrationFingerprint"~'^[0-9A-F]{64}$'),
  "ExpectedOwner" name NOT NULL,"RuntimeRole" name NOT NULL,"IssuerRole" name NOT NULL,
  "OrganizationFingerprint" bytea NOT NULL CHECK (octet_length("OrganizationFingerprint")=32),
  "ExecutionInstanceId" uuid NOT NULL,"ActorFingerprint" bytea NOT NULL CHECK (octet_length("ActorFingerprint")=32),
  "RequestIssuer" name NOT NULL,"IssuerAuthority" text NOT NULL,
  "Operation" text NOT NULL CHECK ("Operation"='REV869B_DISPOSABLE_DATABASE_LIFECYCLE'),
  "TargetFingerprint" bytea NOT NULL CHECK (octet_length("TargetFingerprint")=32),
  "NonceFingerprint" bytea NOT NULL UNIQUE CHECK (octet_length("NonceFingerprint")=32),
  "RequestedAt" timestamptz NOT NULL,"LeaseExpiresAt" timestamptz NOT NULL,
  "State" text NOT NULL CHECK ("State" IN
    ('PreCreate','Created','Provisioned','Executing','Failed','Quarantined','CleanupAuthorized',
     'DropStarted','Dropped','CleanupFailed','Finalized')),
  "StateVersion" bigint NOT NULL DEFAULT 1 CHECK ("StateVersion">0),
  "MarkerFingerprint" text NULL CHECK ("MarkerFingerprint" IS NULL OR "MarkerFingerprint"~'^[0-9A-F]{64}$'),
  "LastTransitionId" uuid NOT NULL UNIQUE,"UpdatedAt" timestamptz NOT NULL,
  CONSTRAINT "CK_rev869b_lease_time" CHECK ("LeaseExpiresAt">"RequestedAt"),
  CONSTRAINT "CK_rev869b_marker_state" CHECK (
    ("State" IN ('PreCreate','Created','Failed','Quarantined','CleanupAuthorized','CleanupFailed','Dropped','Finalized') ) OR
    ("State" IN ('Provisioned','Executing','DropStarted') AND "MarkerFingerprint" IS NOT NULL))
);
CREATE UNIQUE INDEX "UX_rev869b_lease_exact_identity" ON nexa.rev869b_database_leases
  ("DatabaseName","RunId","ExecutionInstanceId","TargetFingerprint");

CREATE TABLE nexa.rev869b_database_lease_events(
  "EventId" uuid PRIMARY KEY,"LeaseId" uuid NOT NULL REFERENCES nexa.rev869b_database_leases("LeaseId") ON DELETE RESTRICT,
  "TransitionSequence" bigint NOT NULL,"PreState" text NULL,"PostState" text NOT NULL,
  "PreStateVersion" bigint NOT NULL,"PostStateVersion" bigint NOT NULL,
  "DatabaseName" name NOT NULL,"OrganizationFingerprint" bytea NOT NULL,
  "ExecutionInstanceId" uuid NOT NULL,"ActorFingerprint" bytea NOT NULL,"IssuerPrincipal" name NOT NULL,
  "Operation" text NOT NULL,"TargetFingerprint" bytea NOT NULL,"NonceFingerprint" bytea NOT NULL,
  "CorrelationId" uuid NOT NULL,"OccurredAt" timestamptz NOT NULL,"Outcome" text NOT NULL,
  "FailureCategory" text NULL, UNIQUE("LeaseId","TransitionSequence"),UNIQUE("CorrelationId")
);
CREATE TABLE nexa.rev869b_recovery_approvals(
  "AuthorizationId" uuid PRIMARY KEY,"LeaseId" uuid NOT NULL REFERENCES nexa.rev869b_database_leases("LeaseId") ON DELETE RESTRICT,
  "ApprovalReference" text NOT NULL,"ApprovalIssuer" name NOT NULL,"IssuerAuthority" text NOT NULL,
  "OrganizationFingerprint" bytea NOT NULL,"ExecutionInstanceId" uuid NOT NULL,
  "ActorFingerprint" bytea NOT NULL,"Operation" text NOT NULL CHECK ("Operation"='REV869B_QUARANTINE_DROP_V1'),
  "TargetFingerprint" bytea NOT NULL,"NonceFingerprint" bytea NOT NULL UNIQUE,
  "ExpectedPreState" text NOT NULL,"ExpectedPreStateVersion" bigint NOT NULL,"AuthorizedPostState" text NOT NULL,
  "IssuedAt" timestamptz NOT NULL,"ExpiresAt" timestamptz NOT NULL,"ConsumedAt" timestamptz NULL,
  "ConsumedAttemptId" uuid NULL UNIQUE,
  CHECK ("ExpiresAt">"IssuedAt" AND "ExpiresAt"<="IssuedAt"+interval '15 minutes')
);
CREATE TABLE nexa.rev869b_recovery_attempts(
  "AttemptId" uuid PRIMARY KEY,"AuthorizationId" uuid NOT NULL UNIQUE
    REFERENCES nexa.rev869b_recovery_approvals("AuthorizationId") ON DELETE RESTRICT,
  "LeaseId" uuid NOT NULL REFERENCES nexa.rev869b_database_leases("LeaseId") ON DELETE RESTRICT,
  "StartedAt" timestamptz NOT NULL,"FinishedAt" timestamptz NULL,
  "PreState" text NOT NULL,"PreStateVersion" bigint NOT NULL,"RequestedPostState" text NOT NULL,
  "ObservedPostState" text NULL,"OutcomeId" uuid NULL UNIQUE,"Outcome" text NOT NULL
    CHECK ("Outcome" IN ('Started','Succeeded','Failed','Interrupted')),
  "FailureCategory" text NULL,"CorrelationId" uuid NOT NULL UNIQUE
);
CREATE TABLE nexa.rev869b_recovery_outcomes(
  "OutcomeId" uuid PRIMARY KEY,"AttemptId" uuid NOT NULL UNIQUE
    REFERENCES nexa.rev869b_recovery_attempts("AttemptId") ON DELETE RESTRICT,
  "ObservedPostState" text NOT NULL,"MarkerFingerprint" text NULL,
  "Outcome" text NOT NULL CHECK ("Outcome" IN ('Succeeded','Failed','Interrupted')),
  "FailureCategory" text NULL,"FinishedAt" timestamptz NOT NULL,
  CHECK (("Outcome"='Succeeded' AND "FailureCategory" IS NULL) OR
         ("Outcome"<>'Succeeded' AND length(trim("FailureCategory"))>0))
);
CREATE INDEX "IX_rev869b_lease_state_expiry" ON nexa.rev869b_database_leases("State","LeaseExpiresAt");
CREATE INDEX "IX_rev869b_event_lease_time" ON nexa.rev869b_database_lease_events("LeaseId","OccurredAt");

CREATE FUNCTION nexa.rev869b_reject_registry_audit_mutation() RETURNS trigger
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,nexa AS $$
BEGIN RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_control_plane_append_only',
 MESSAGE='REV869B control-plane audit is append-only'; END $$;
CREATE TRIGGER "TR_rev869b_lease_events_immutable" BEFORE UPDATE OR DELETE ON nexa.rev869b_database_lease_events
 FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_reject_registry_audit_mutation();
CREATE TRIGGER "TR_rev869b_recovery_attempts_immutable" BEFORE UPDATE OR DELETE ON nexa.rev869b_recovery_attempts
 FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_reject_registry_audit_mutation();
CREATE TRIGGER "TR_rev869b_recovery_outcomes_immutable" BEFORE UPDATE OR DELETE ON nexa.rev869b_recovery_outcomes
 FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_reject_registry_audit_mutation();

CREATE FUNCTION nexa.rev869b_transition_database_lease(
  database_name name,run_id text,expected_state text,expected_version bigint,post_state text,
  organization_fingerprint bytea,execution_instance_id uuid,actor_fingerprint bytea,
  issuer name,operation text,target_fingerprint bytea,nonce_fingerprint bytea,
  correlation_id uuid,marker_fingerprint text,outcome text,failure_category text)
RETURNS bigint LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,nexa AS $$
DECLARE lease nexa.rev869b_database_leases%ROWTYPE; allowed boolean:=false;
BEGIN
 IF session_user NOT IN ('nexa_rev869b_control_plane_api','nexa_rev869b_control_plane_audit_writer',
                         'nexa_rev869b_recovery_administrator') THEN
  RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_control_plane_exact_caller';
 END IF;
 SELECT * INTO STRICT lease FROM nexa.rev869b_database_leases
  WHERE "DatabaseName"=database_name AND "RunId"=run_id FOR UPDATE;
 IF lease."State"<>expected_state OR lease."StateVersion"<>expected_version OR
    lease."OrganizationFingerprint" IS DISTINCT FROM organization_fingerprint OR
    lease."ExecutionInstanceId"<>execution_instance_id OR lease."ActorFingerprint" IS DISTINCT FROM actor_fingerprint OR
    lease."RequestIssuer"<>issuer OR lease."Operation"<>operation OR
    lease."TargetFingerprint" IS DISTINCT FROM target_fingerprint OR
    lease."NonceFingerprint" IS DISTINCT FROM nonce_fingerprint THEN
  RAISE EXCEPTION USING ERRCODE='40001',CONSTRAINT='rev869b_control_plane_changed_or_substituted_state';
 END IF;
 allowed:=(expected_state,post_state) IN
  (('PreCreate','Created'),('Created','Provisioned'),('Provisioned','Executing'),
   ('Executing','DropStarted'),('DropStarted','Dropped'),('Dropped','Finalized'),
   ('PreCreate','Failed'),('Created','Failed'),('Provisioned','Failed'),('Executing','Failed'),
   ('PreCreate','Quarantined'),('Created','Quarantined'),('Provisioned','Quarantined'),('Executing','Quarantined'),
   ('Failed','Quarantined'),('Quarantined','CleanupAuthorized'),('CleanupFailed','CleanupAuthorized'),
   ('DropStarted','CleanupAuthorized'),('CleanupAuthorized','DropStarted'),
   ('CleanupAuthorized','Dropped'),('CleanupAuthorized','CleanupFailed'),('DropStarted','CleanupFailed'));
 IF NOT allowed THEN RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_control_plane_illegal_transition'; END IF;
 IF post_state IN ('Provisioned','Executing','DropStarted') AND marker_fingerprint IS NULL THEN
  RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_control_plane_marker_binding';
 END IF;
 UPDATE nexa.rev869b_database_leases SET "State"=post_state,"StateVersion"="StateVersion"+1,
  "MarkerFingerprint"=coalesce(marker_fingerprint,"MarkerFingerprint"),"LastTransitionId"=correlation_id,
  "UpdatedAt"=clock_timestamp() WHERE "LeaseId"=lease."LeaseId";
 INSERT INTO nexa.rev869b_database_lease_events VALUES(
  correlation_id,lease."LeaseId",lease."StateVersion",expected_state,post_state,lease."StateVersion",
  lease."StateVersion"+1,database_name,organization_fingerprint,execution_instance_id,actor_fingerprint,
  issuer,operation,target_fingerprint,nonce_fingerprint,correlation_id,clock_timestamp(),outcome,failure_category);
 RETURN lease."StateVersion"+1;
END $$;

CREATE FUNCTION nexa.rev869b_read_authoritative_database_lease(database_name name,run_id text)
RETURNS SETOF nexa.rev869b_database_leases LANGUAGE sql STABLE SECURITY DEFINER
SET search_path=pg_catalog,nexa AS $$
 SELECT * FROM nexa.rev869b_database_leases
 WHERE "DatabaseName"=database_name AND "RunId"=run_id
$$;

CREATE FUNCTION nexa.rev869b_reserve_database_lease(
 database_name name,run_id text,ownership_token_hash text,fixture_family text,scenario_hash text,
 source_database name,source_fingerprint text,source_commit_fingerprint text,migration_id text,
 migration_fingerprint text,expected_owner name,requested_at timestamptz,lease_expires_at timestamptz,
 runtime_role name,issuer_role name,request_issuer text,issuer_authority text,policy text)
RETURNS bigint LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,nexa AS $$
DECLARE lease_id uuid:=public.gen_random_uuid(); transition_id uuid:=public.gen_random_uuid();
 org bytea; execution_id uuid; actor bytea; target bytea; nonce bytea;
BEGIN
 IF session_user<>'nexa_rev869b_control_plane_api' OR policy<>'MGMT-REV869B-CONTROL-PLANE-20260813-001'
  OR database_name !~ '^sess_nexaerp_rev869b_[0-9a-f]{24}$' OR lease_expires_at<=requested_at
  OR lease_expires_at>requested_at+interval '4 hours' THEN
  RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_exact_precreate_reservation';
 END IF;
 org:=public.digest(convert_to(fixture_family,'UTF8'),'sha256');
 execution_id:=md5(run_id)::uuid;
 actor:=public.digest(convert_to(request_issuer,'UTF8'),'sha256');
 target:=public.digest(convert_to(jsonb_build_array(database_name,run_id,source_database,source_fingerprint,
  source_commit_fingerprint,migration_id,migration_fingerprint,expected_owner)::text,'UTF8'),'sha256');
 nonce:=decode(ownership_token_hash,'hex');
 INSERT INTO nexa.rev869b_database_leases VALUES(
  lease_id,database_name,run_id,ownership_token_hash,fixture_family,scenario_hash,source_database,
  source_fingerprint,source_commit_fingerprint,migration_id,migration_fingerprint,expected_owner,
  runtime_role,issuer_role,org,execution_id,actor,request_issuer,issuer_authority,
  'REV869B_DISPOSABLE_DATABASE_LIFECYCLE',target,nonce,requested_at,lease_expires_at,
  'PreCreate',1,NULL,transition_id,clock_timestamp());
 INSERT INTO nexa.rev869b_database_lease_events VALUES(
  transition_id,lease_id,1,NULL,'PreCreate',0,1,database_name,org,execution_id,actor,request_issuer,
  'REV869B_DISPOSABLE_DATABASE_LIFECYCLE',target,nonce,transition_id,clock_timestamp(),'Reserved',NULL);
 RETURN 1;
END $$;

CREATE FUNCTION nexa.rev869b_complete_database_lease(
 database_name name,run_id text,ownership_token_hash text,fixture_family text,scenario_hash text,
 source_database name,source_fingerprint text,source_commit_fingerprint text,migration_id text,
 migration_fingerprint text,expected_owner name,requested_at timestamptz,lease_expires_at timestamptz,
 runtime_role name,issuer_role name,request_issuer text,issuer_authority text,exact_pre_state text,
 exact_post_state text,marker_fingerprint text,outcome text,failure_category text,occurred_at timestamptz,policy text)
RETURNS bigint LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,nexa AS $$
DECLARE l nexa.rev869b_database_leases%ROWTYPE; correlation uuid:=public.gen_random_uuid();
BEGIN
 SELECT * INTO STRICT l FROM nexa.rev869b_database_leases WHERE "DatabaseName"=database_name
  AND "RunId"=run_id AND "OwnershipTokenHash"=ownership_token_hash AND "FixtureFamily"=fixture_family
  AND "ScenarioHash"=scenario_hash AND "SourceDatabase"=source_database
  AND "SourceFingerprint"=source_fingerprint AND "SourceCommitFingerprint"=source_commit_fingerprint
  AND "MigrationId"=migration_id AND "MigrationFingerprint"=migration_fingerprint
  AND "ExpectedOwner"=expected_owner AND "RequestedAt"=requested_at AND "LeaseExpiresAt"=lease_expires_at
  AND "RuntimeRole"=runtime_role AND "IssuerRole"=issuer_role AND "RequestIssuer"=request_issuer
  AND "IssuerAuthority"=issuer_authority FOR UPDATE;
 RETURN nexa.rev869b_transition_database_lease(database_name,run_id,exact_pre_state,l."StateVersion",
  exact_post_state,l."OrganizationFingerprint",l."ExecutionInstanceId",l."ActorFingerprint",
  l."RequestIssuer",l."Operation",l."TargetFingerprint",l."NonceFingerprint",correlation,
  marker_fingerprint,outcome,failure_category);
END $$;

CREATE FUNCTION nexa.rev869b_read_exact_database_lease(
 database_name name,run_id text,ownership_token_hash text,fixture_family text,scenario_hash text,
 source_database name,source_fingerprint text,source_commit_fingerprint text,migration_id text,
 migration_fingerprint text,expected_owner name,requested_at timestamptz,lease_expires_at timestamptz,
 runtime_role name,issuer_role name,request_issuer text,issuer_authority text,required_state text,policy text)
RETURNS TABLE("DatabaseName" name,"RunId" text,"OwnershipTokenHash" text,"FixtureFamily" text,
 "ScenarioHash" text,"SourceDatabase" name,"SourceFingerprint" text,"SourceCommitFingerprint" text,
 "MigrationId" text,"MigrationFingerprint" text,"ExpectedOwner" name,"RequestedAt" timestamptz,
 "LeaseExpiresAt" timestamptz,"RuntimeRole" name,"IssuerRole" name,"State" text,"MarkerFingerprint" text)
LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,nexa AS $$
 SELECT l."DatabaseName",l."RunId",l."OwnershipTokenHash",l."FixtureFamily",l."ScenarioHash",
  l."SourceDatabase",l."SourceFingerprint",l."SourceCommitFingerprint",l."MigrationId",
  l."MigrationFingerprint",l."ExpectedOwner",l."RequestedAt",l."LeaseExpiresAt",l."RuntimeRole",
  l."IssuerRole",l."State",l."MarkerFingerprint" FROM nexa.rev869b_database_leases l
 WHERE l."DatabaseName"=database_name AND l."RunId"=run_id AND l."OwnershipTokenHash"=ownership_token_hash
  AND l."FixtureFamily"=fixture_family AND l."ScenarioHash"=scenario_hash
  AND l."SourceDatabase"=source_database AND l."SourceFingerprint"=source_fingerprint
  AND l."SourceCommitFingerprint"=source_commit_fingerprint AND l."MigrationId"=migration_id
  AND l."MigrationFingerprint"=migration_fingerprint AND l."ExpectedOwner"=expected_owner
  AND l."RequestedAt"=requested_at AND l."LeaseExpiresAt"=lease_expires_at
  AND l."RuntimeRole"=runtime_role AND l."IssuerRole"=issuer_role AND l."RequestIssuer"=request_issuer
  AND l."IssuerAuthority"=issuer_authority AND l."State"=required_state
  AND policy='MGMT-REV869B-CONTROL-PLANE-20260813-001'
$$;

CREATE FUNCTION nexa.rev869b_begin_database_drop(
 database_name name,run_id text,ownership_token_hash text,fixture_family text,scenario_hash text,
 source_database name,source_fingerprint text,source_commit_fingerprint text,migration_id text,
 migration_fingerprint text,expected_owner name,requested_at timestamptz,lease_expires_at timestamptz,
 runtime_role name,issuer_role name,request_issuer text,issuer_authority text,exact_pre_state text,
 marker_fingerprint text,requested_post_state text,occurred_at timestamptz,policy text)
RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,nexa AS $$
DECLARE l nexa.rev869b_database_leases%ROWTYPE; attempt uuid:=public.gen_random_uuid();
BEGIN
 SELECT * INTO STRICT l FROM nexa.rev869b_database_leases WHERE "DatabaseName"=database_name
  AND "RunId"=run_id AND "OwnershipTokenHash"=ownership_token_hash AND "State"=exact_pre_state
  AND "MarkerFingerprint"=marker_fingerprint FOR UPDATE;
 PERFORM nexa.rev869b_transition_database_lease(database_name,run_id,exact_pre_state,l."StateVersion",
  'DropStarted',l."OrganizationFingerprint",l."ExecutionInstanceId",l."ActorFingerprint",
  l."RequestIssuer",l."Operation",l."TargetFingerprint",l."NonceFingerprint",attempt,
  marker_fingerprint,'Started',NULL);
 RETURN attempt;
END $$;

CREATE FUNCTION nexa.rev869b_record_database_drop_outcome(
 attempt_id uuid,database_name name,run_id text,ownership_token_hash text,exact_pre_state text,
 observed_post_state text,marker_fingerprint text,outcome text,failure_category text,
 occurred_at timestamptz,policy text)
RETURNS bigint LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,nexa AS $$
DECLARE l nexa.rev869b_database_leases%ROWTYPE;
BEGIN
 SELECT * INTO STRICT l FROM nexa.rev869b_database_leases WHERE "DatabaseName"=database_name
  AND "RunId"=run_id AND "OwnershipTokenHash"=ownership_token_hash AND "State"='DropStarted'
  AND EXISTS(SELECT 1 FROM nexa.rev869b_database_lease_events e WHERE e."LeaseId"=l."LeaseId"
   AND e."CorrelationId"=attempt_id AND e."PostState"='DropStarted') FOR UPDATE;
 RETURN nexa.rev869b_transition_database_lease(database_name,run_id,'DropStarted',l."StateVersion",
  observed_post_state,l."OrganizationFingerprint",l."ExecutionInstanceId",l."ActorFingerprint",
  l."RequestIssuer",l."Operation",l."TargetFingerprint",l."NonceFingerprint",public.gen_random_uuid(),
  marker_fingerprint,outcome,failure_category);
END $$;

CREATE FUNCTION nexa.rev869b_consume_recovery_approval(
 authorization_id uuid,database_name name,run_id text,ownership_token_hash text,fixture_family text,
 scenario_hash text,source_database name,source_fingerprint text,source_commit_fingerprint text,
 migration_id text,migration_fingerprint text,expected_owner name,requested_at timestamptz,
 lease_expires_at timestamptz,runtime_role name,issuer_role name,request_issuer text,
 request_authority text,purpose text,approval_issuer text,approval_authority text,
 expected_pre_state text,authorized_post_state text,approval_reference text,reason text,
 executor text,issued_at timestamptz,expires_at timestamptz,nonce_hash text,
 target_fingerprint text,consumed_at timestamptz,policy text)
RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,nexa AS $$
DECLARE l nexa.rev869b_database_leases%ROWTYPE; attempt uuid:=public.gen_random_uuid();
 approval_nonce bytea; approval_target bytea; transition_id uuid:=public.gen_random_uuid();
BEGIN
 IF session_user<>'nexa_rev869b_recovery_administrator' OR purpose<>'REV869B_QUARANTINE_DROP_V1'
  OR authorized_post_state<>'Dropped' OR expires_at<=clock_timestamp()
  OR expires_at>issued_at+interval '15 minutes' OR consumed_at<issued_at OR consumed_at>expires_at
  OR policy<>'MGMT-REV869B-CONTROL-PLANE-20260813-001' THEN
  RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_fresh_exact_recovery_approval';
 END IF;
 SELECT * INTO STRICT l FROM nexa.rev869b_database_leases WHERE "DatabaseName"=database_name
  AND "RunId"=run_id AND "OwnershipTokenHash"=ownership_token_hash AND "FixtureFamily"=fixture_family
  AND "ScenarioHash"=scenario_hash AND "SourceDatabase"=source_database
  AND "SourceFingerprint"=source_fingerprint AND "SourceCommitFingerprint"=source_commit_fingerprint
  AND "MigrationId"=migration_id AND "MigrationFingerprint"=migration_fingerprint
  AND "ExpectedOwner"=expected_owner AND "RequestedAt"=requested_at AND "LeaseExpiresAt"=lease_expires_at
  AND "RuntimeRole"=runtime_role AND "IssuerRole"=issuer_role AND "State"=expected_pre_state FOR UPDATE;
 approval_nonce:=decode(nonce_hash,'hex'); approval_target:=decode(target_fingerprint,'hex');
 INSERT INTO nexa.rev869b_recovery_approvals VALUES(
  authorization_id,l."LeaseId",approval_reference,approval_issuer,approval_authority,
  l."OrganizationFingerprint",l."ExecutionInstanceId",l."ActorFingerprint",purpose,
  approval_target,approval_nonce,expected_pre_state,l."StateVersion",authorized_post_state,
  issued_at,expires_at,consumed_at,attempt);
 INSERT INTO nexa.rev869b_recovery_attempts(
  "AttemptId","AuthorizationId","LeaseId","StartedAt","PreState","PreStateVersion",
  "RequestedPostState","Outcome","CorrelationId")
 VALUES(attempt,authorization_id,l."LeaseId",consumed_at,expected_pre_state,l."StateVersion",
  authorized_post_state,'Started',transition_id);
 PERFORM nexa.rev869b_transition_database_lease(database_name,run_id,expected_pre_state,l."StateVersion",
  'CleanupAuthorized',l."OrganizationFingerprint",l."ExecutionInstanceId",l."ActorFingerprint",
  l."RequestIssuer",l."Operation",l."TargetFingerprint",l."NonceFingerprint",transition_id,
  l."MarkerFingerprint",'RecoveryAuthorized',NULL);
 RETURN attempt;
END $$;

CREATE FUNCTION nexa.rev869b_record_recovery_outcome(
 attempt_id uuid,exact_pre_state text,observed_post_state text,marker_fingerprint text,
 outcome text,failure_category text,finished_at timestamptz,policy text)
RETURNS bigint LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,nexa AS $$
DECLARE a nexa.rev869b_recovery_attempts%ROWTYPE; l nexa.rev869b_database_leases%ROWTYPE;
 outcome_id uuid:=public.gen_random_uuid(); post_state text;
BEGIN
 IF session_user NOT IN ('nexa_rev869b_recovery_administrator','nexa_rev869b_control_plane_audit_writer')
  OR policy<>'MGMT-REV869B-CONTROL-PLANE-20260813-001' OR outcome NOT IN ('Succeeded','Failed','Interrupted') THEN
  RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_exact_recovery_outcome';
 END IF;
 SELECT * INTO STRICT a FROM nexa.rev869b_recovery_attempts WHERE "AttemptId"=attempt_id
  AND "PreState"=exact_pre_state AND NOT EXISTS(
   SELECT 1 FROM nexa.rev869b_recovery_outcomes o WHERE o."AttemptId"=attempt_id);
 SELECT * INTO STRICT l FROM nexa.rev869b_database_leases WHERE "LeaseId"=a."LeaseId"
  AND "State"='CleanupAuthorized' FOR UPDATE;
 post_state:=CASE WHEN outcome='Succeeded' THEN observed_post_state ELSE 'CleanupFailed' END;
 INSERT INTO nexa.rev869b_recovery_outcomes VALUES(
  outcome_id,attempt_id,observed_post_state,marker_fingerprint,outcome,failure_category,finished_at);
 PERFORM nexa.rev869b_transition_database_lease(l."DatabaseName",l."RunId",'CleanupAuthorized',
  l."StateVersion",post_state,l."OrganizationFingerprint",l."ExecutionInstanceId",l."ActorFingerprint",
  l."RequestIssuer",l."Operation",l."TargetFingerprint",l."NonceFingerprint",outcome_id,
  coalesce(marker_fingerprint,l."MarkerFingerprint"),outcome,failure_category);
 RETURN 1;
END $$;

CREATE FUNCTION nexa.rev869b_verify_exact_control_plane(expected_database name,expected_owner name,expected_caller name)
RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,nexa AS $$
 WITH expected_roles(name,login) AS (VALUES
  ('nexa_rev869b_control_plane_owner',false),('nexa_rev869b_control_plane_api',true),
  ('nexa_rev869b_control_plane_issuer',true),('nexa_rev869b_control_plane_audit_writer',true),
  ('nexa_rev869b_recovery_administrator',true),('nexa_rev869b_purge_authorizer',true),
  ('nexa_rev869b_purge_executor',true),('nexa_rev869b_verifier',true),
  ('nexa_rev869b_security_owner',false),('nexa_rev869b_runtime',true),
  ('nexa_rev869b_command_issuer',true),('nexa_rev869b_purge_audit_writer',true),
  ('nexa_rev869b_security_export_authorizer',true),('nexa_rev869b_security_export_reader',true)),
 expected_relations(name,column_count) AS (VALUES ('rev869b_database_leases',29),('rev869b_database_lease_events',19),
  ('rev869b_recovery_approvals',18),('rev869b_recovery_attempts',13),('rev869b_recovery_outcomes',7)),
 expected_functions(name) AS (VALUES ('rev869b_reserve_database_lease'),('rev869b_complete_database_lease'),
  ('rev869b_read_exact_database_lease'),('rev869b_begin_database_drop'),
  ('rev869b_record_database_drop_outcome'),('rev869b_consume_recovery_approval'),
  ('rev869b_record_recovery_outcome'),('rev869b_transition_database_lease'),
  ('rev869b_read_authoritative_database_lease'),('rev869b_read_drop_started_attempt'),
  ('rev869b_read_database_lease_transition_states'),('rev869b_verify_exact_control_plane'),
  ('rev869b_reject_registry_audit_mutation'))
 SELECT current_database()=expected_database AND expected_database='sess_nexaerp_rev869b_control_plane'
  AND pg_get_userbyid((SELECT datdba FROM pg_database WHERE datname=current_database()))=expected_owner
  AND expected_owner='nexa_rev869b_control_plane_owner'
  AND NOT has_database_privilege('public',current_database(),'CONNECT')
  AND NOT has_schema_privilege('public','nexa','USAGE,CREATE')
  AND NOT has_schema_privilege(expected_caller,'nexa','CREATE')
  AND (SELECT count(*) FROM expected_roles e JOIN pg_roles r ON r.rolname=e.name
    WHERE r.rolcanlogin=e.login AND NOT r.rolinherit AND NOT r.rolsuper AND NOT r.rolcreatedb
      AND NOT r.rolcreaterole AND NOT r.rolreplication AND NOT r.rolbypassrls)=14
  AND EXISTS(SELECT 1 FROM pg_roles r WHERE r.rolname='nexa_rev869b_provisioning_administrator'
    AND r.rolcanlogin AND NOT r.rolinherit AND NOT r.rolsuper AND r.rolcreatedb AND r.rolcreaterole
    AND NOT r.rolreplication AND NOT r.rolbypassrls)
  AND (SELECT count(*) FROM pg_auth_members m JOIN pg_roles r ON r.oid=m.roleid
    JOIN pg_roles u ON u.oid=m.member WHERE r.rolname LIKE 'nexa_rev869b_%'
      AND r.rolname IN ('nexa_rev869b_security_owner','nexa_rev869b_control_plane_owner')
      AND u.rolname='nexa_rev869b_provisioning_administrator'
      AND NOT m.inherit_option AND m.set_option)=2
  AND NOT EXISTS(SELECT 1 FROM pg_auth_members m JOIN pg_roles r ON r.oid=m.roleid
    JOIN pg_roles u ON u.oid=m.member WHERE r.rolname LIKE 'nexa_rev869b_%'
      AND NOT (r.rolname IN ('nexa_rev869b_security_owner','nexa_rev869b_control_plane_owner')
        AND u.rolname='nexa_rev869b_provisioning_administrator'
        AND NOT m.inherit_option AND m.set_option))
  AND (SELECT count(*) FROM expected_relations e JOIN pg_class c ON c.relname=e.name
    JOIN pg_namespace n ON n.oid=c.relnamespace AND n.nspname='nexa'
    WHERE c.relkind='r' AND pg_get_userbyid(c.relowner)=expected_owner
      AND (SELECT count(*) FROM pg_attribute a WHERE a.attrelid=c.oid AND a.attnum>0 AND NOT a.attisdropped)=e.column_count)=5
  AND (SELECT count(*) FROM pg_class i JOIN pg_namespace n ON n.oid=i.relnamespace
    WHERE n.nspname='nexa' AND i.relkind='i')=20
  AND NOT EXISTS(SELECT 1 FROM expected_relations e JOIN pg_class c ON c.relname=e.name
    JOIN pg_namespace n ON n.oid=c.relnamespace AND n.nspname='nexa'
    WHERE has_table_privilege('public',c.oid,'SELECT,INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER')
       OR has_table_privilege(expected_caller,c.oid,'SELECT,INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER'))
  AND (SELECT count(DISTINCT e.name) FROM expected_functions e JOIN pg_proc p ON p.proname=e.name
    JOIN pg_namespace n ON n.oid=p.pronamespace AND n.nspname='nexa'
    WHERE p.prosecdef AND
      ((e.name IN ('rev869b_read_exact_database_lease','rev869b_read_authoritative_database_lease',
        'rev869b_read_drop_started_attempt','rev869b_read_database_lease_transition_states',
        'rev869b_verify_exact_control_plane') AND p.provolatile='s')
       OR (e.name NOT IN ('rev869b_read_exact_database_lease','rev869b_read_authoritative_database_lease',
        'rev869b_read_drop_started_attempt','rev869b_read_database_lease_transition_states',
        'rev869b_verify_exact_control_plane') AND p.provolatile='v'))
      AND p.proparallel='u' AND NOT p.proleakproof
      AND p.proconfig=ARRAY['search_path=pg_catalog, nexa']::text[]
      AND pg_get_userbyid(p.proowner)=expected_owner
      AND NOT has_function_privilege('public',p.oid,'EXECUTE'))=13
  AND NOT EXISTS(SELECT 1 FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
    WHERE n.nspname='nexa' AND p.proname LIKE 'rev869b_%'
      AND p.proname NOT IN (SELECT name FROM expected_functions))
  AND (SELECT count(*) FROM pg_trigger t JOIN pg_class c ON c.oid=t.tgrelid
    JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='nexa' AND NOT t.tgisinternal
      AND c.relname IN ('rev869b_database_lease_events','rev869b_recovery_attempts','rev869b_recovery_outcomes'))=3
 $$;
CREATE FUNCTION nexa.rev869b_read_drop_started_attempt(database_name name,run_id text)
RETURNS TABLE("AttemptId" uuid,"StateVersion" bigint,"MarkerFingerprint" text)
LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,nexa AS $$
 SELECT e."CorrelationId",l."StateVersion",l."MarkerFingerprint"
 FROM nexa.rev869b_database_leases l JOIN nexa.rev869b_database_lease_events e ON e."LeaseId"=l."LeaseId"
 WHERE l."DatabaseName"=database_name AND l."RunId"=run_id AND l."State"='DropStarted'
   AND e."PostState"='DropStarted' ORDER BY e."TransitionSequence" DESC LIMIT 1
$$;
CREATE FUNCTION nexa.rev869b_read_database_lease_transition_states(database_name name,run_id text)
RETURNS text[] LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,nexa AS $$
 SELECT array_agg(e."PostState" ORDER BY e."TransitionSequence")
 FROM nexa.rev869b_database_leases l JOIN nexa.rev869b_database_lease_events e ON e."LeaseId"=l."LeaseId"
 WHERE l."DatabaseName"=database_name AND l."RunId"=run_id
$$;

ALTER TABLE nexa.rev869b_database_leases OWNER TO nexa_rev869b_control_plane_owner;
ALTER TABLE nexa.rev869b_database_lease_events OWNER TO nexa_rev869b_control_plane_owner;
ALTER TABLE nexa.rev869b_recovery_approvals OWNER TO nexa_rev869b_control_plane_owner;
ALTER TABLE nexa.rev869b_recovery_attempts OWNER TO nexa_rev869b_control_plane_owner;
ALTER TABLE nexa.rev869b_recovery_outcomes OWNER TO nexa_rev869b_control_plane_owner;
ALTER FUNCTION nexa.rev869b_reject_registry_audit_mutation() OWNER TO nexa_rev869b_control_plane_owner;
ALTER FUNCTION nexa.rev869b_transition_database_lease(name,text,text,bigint,text,bytea,uuid,bytea,name,text,bytea,bytea,uuid,text,text,text) OWNER TO nexa_rev869b_control_plane_owner;
ALTER FUNCTION nexa.rev869b_read_authoritative_database_lease(name,text) OWNER TO nexa_rev869b_control_plane_owner;
ALTER FUNCTION nexa.rev869b_read_drop_started_attempt(name,text) OWNER TO nexa_rev869b_control_plane_owner;
ALTER FUNCTION nexa.rev869b_read_database_lease_transition_states(name,text) OWNER TO nexa_rev869b_control_plane_owner;
ALTER FUNCTION nexa.rev869b_reserve_database_lease(name,text,text,text,text,name,text,text,text,text,name,timestamptz,timestamptz,name,name,text,text,text) OWNER TO nexa_rev869b_control_plane_owner;
ALTER FUNCTION nexa.rev869b_complete_database_lease(name,text,text,text,text,name,text,text,text,text,name,timestamptz,timestamptz,name,name,text,text,text,text,text,text,text,timestamptz,text) OWNER TO nexa_rev869b_control_plane_owner;
ALTER FUNCTION nexa.rev869b_read_exact_database_lease(name,text,text,text,text,name,text,text,text,text,name,timestamptz,timestamptz,name,name,text,text,text,text) OWNER TO nexa_rev869b_control_plane_owner;
ALTER FUNCTION nexa.rev869b_begin_database_drop(name,text,text,text,text,name,text,text,text,text,name,timestamptz,timestamptz,name,name,text,text,text,text,text,timestamptz,text) OWNER TO nexa_rev869b_control_plane_owner;
ALTER FUNCTION nexa.rev869b_record_database_drop_outcome(uuid,name,text,text,text,text,text,text,text,timestamptz,text) OWNER TO nexa_rev869b_control_plane_owner;
ALTER FUNCTION nexa.rev869b_consume_recovery_approval(uuid,name,text,text,text,text,name,text,text,text,text,name,timestamptz,timestamptz,name,name,text,text,text,text,text,text,text,text,text,text,timestamptz,timestamptz,text,text,timestamptz,text) OWNER TO nexa_rev869b_control_plane_owner;
ALTER FUNCTION nexa.rev869b_record_recovery_outcome(uuid,text,text,text,text,text,timestamptz,text) OWNER TO nexa_rev869b_control_plane_owner;
ALTER FUNCTION nexa.rev869b_verify_exact_control_plane(name,name,name) OWNER TO nexa_rev869b_control_plane_owner;

REVOKE ALL ON ALL TABLES IN SCHEMA nexa FROM PUBLIC;
REVOKE ALL ON ALL SEQUENCES IN SCHEMA nexa FROM PUBLIC;
REVOKE EXECUTE ON ALL FUNCTIONS IN SCHEMA nexa FROM PUBLIC;
REVOKE ALL ON ALL TABLES IN SCHEMA nexa FROM nexa_rev869b_control_plane_api,
 nexa_rev869b_control_plane_issuer,nexa_rev869b_control_plane_audit_writer,
 nexa_rev869b_recovery_administrator,nexa_rev869b_verifier;
GRANT USAGE ON SCHEMA nexa TO nexa_rev869b_control_plane_api,nexa_rev869b_control_plane_audit_writer,
 nexa_rev869b_recovery_administrator,nexa_rev869b_verifier,nexa_rev869b_provisioning_administrator;
GRANT EXECUTE ON FUNCTION nexa.rev869b_transition_database_lease(name,text,text,bigint,text,bytea,uuid,bytea,name,text,bytea,bytea,uuid,text,text,text)
 TO nexa_rev869b_control_plane_api,nexa_rev869b_control_plane_audit_writer,nexa_rev869b_recovery_administrator;
GRANT EXECUTE ON FUNCTION nexa.rev869b_read_authoritative_database_lease(name,text),
 nexa.rev869b_read_drop_started_attempt(name,text),
 nexa.rev869b_read_database_lease_transition_states(name,text)
 TO nexa_rev869b_control_plane_api,nexa_rev869b_recovery_administrator,nexa_rev869b_verifier;
GRANT EXECUTE ON FUNCTION nexa.rev869b_reserve_database_lease(name,text,text,text,text,name,text,text,text,text,name,timestamptz,timestamptz,name,name,text,text,text),
 nexa.rev869b_complete_database_lease(name,text,text,text,text,name,text,text,text,text,name,timestamptz,timestamptz,name,name,text,text,text,text,text,text,text,timestamptz,text),
 nexa.rev869b_read_exact_database_lease(name,text,text,text,text,name,text,text,text,text,name,timestamptz,timestamptz,name,name,text,text,text,text),
 nexa.rev869b_begin_database_drop(name,text,text,text,text,name,text,text,text,text,name,timestamptz,timestamptz,name,name,text,text,text,text,text,timestamptz,text),
 nexa.rev869b_record_database_drop_outcome(uuid,name,text,text,text,text,text,text,text,timestamptz,text)
 TO nexa_rev869b_control_plane_api;
GRANT EXECUTE ON FUNCTION nexa.rev869b_consume_recovery_approval(uuid,name,text,text,text,text,name,text,text,text,text,name,timestamptz,timestamptz,name,name,text,text,text,text,text,text,text,text,text,text,timestamptz,timestamptz,text,text,timestamptz,text),
 nexa.rev869b_record_recovery_outcome(uuid,text,text,text,text,text,timestamptz,text)
 TO nexa_rev869b_recovery_administrator;
GRANT EXECUTE ON FUNCTION nexa.rev869b_verify_exact_control_plane(name,name,name)
 TO nexa_rev869b_control_plane_api,nexa_rev869b_recovery_administrator,nexa_rev869b_verifier,
 nexa_rev869b_provisioning_administrator;
ALTER DEFAULT PRIVILEGES FOR ROLE nexa_rev869b_control_plane_owner IN SCHEMA nexa REVOKE ALL ON TABLES FROM PUBLIC;
ALTER DEFAULT PRIVILEGES FOR ROLE nexa_rev869b_control_plane_owner IN SCHEMA nexa REVOKE EXECUTE ON FUNCTIONS FROM PUBLIC;
ALTER DEFAULT PRIVILEGES FOR ROLE nexa_rev869b_control_plane_owner IN SCHEMA nexa REVOKE ALL ON SEQUENCES FROM PUBLIC;
COMMIT;
