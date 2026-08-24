DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'advance') THEN
        CREATE SCHEMA advance;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS advance."__EFMigrationsHistory_Rev869BSecurity" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory_Rev869BSecurity" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
DO $roles$ DECLARE r text; expected_login boolean; BEGIN
  FOR r,expected_login IN SELECT * FROM (VALUES
    ('nexa_rev869b_security_owner',false),('nexa_rev869b_app_runtime',true),('nexa_rev869b_command_audit',true),
    ('nexa_rev869b_management_writer',true),('nexa_rev869b_purge_worker',true),('nexa_rev869b_purge_audit',true),
    ('nexa_rev869b_export_service',true),('nexa_rev869b_target_verifier',true)) AS expected(name,can_login) LOOP
    IF NOT EXISTS(SELECT 1 FROM pg_roles WHERE rolname=r AND rolcanlogin=expected_login AND NOT rolinherit AND NOT rolsuper AND NOT rolcreatedb AND NOT rolcreaterole AND NOT rolreplication AND NOT rolbypassrls AND rolconnlimit=-1 AND rolvaliduntil IS NULL) THEN RAISE EXCEPTION 'Missing or noncanonical capability-free frozen role %',r; END IF;
  END LOOP;
  IF EXISTS(SELECT 1 FROM pg_auth_members m JOIN pg_roles a ON a.oid=m.roleid JOIN pg_roles b ON b.oid=m.member WHERE (a.rolname LIKE 'nexa_rev869b_%' OR b.rolname LIKE 'nexa_rev869b_%') AND NOT (a.rolname='nexa_rev869b_security_owner' AND b.rolname='nexa_rev869b_lifecycle_administrator')) OR NOT EXISTS(SELECT 1 FROM pg_auth_members m JOIN pg_roles a ON a.oid=m.roleid JOIN pg_roles b ON b.oid=m.member WHERE a.rolname='nexa_rev869b_security_owner' AND b.rolname='nexa_rev869b_lifecycle_administrator') THEN RAISE EXCEPTION 'REV869B roles require only the security-owner grant to the lifecycle administrator'; END IF;
END $roles$;
CREATE TABLE advance.rev869b_command_requests(
  "CommandId" uuid PRIMARY KEY,"OrganizationId" text NOT NULL,"Operation" text NOT NULL,"IdempotencyKeySha256" bytea NOT NULL CHECK(octet_length("IdempotencyKeySha256")=32),"RequestSha256" bytea NOT NULL CHECK(octet_length("RequestSha256")=32),
  "ActorEmployeeId" uuid NOT NULL,"IdentityIssuer" text NOT NULL,"IdentitySubject" text NOT NULL,"ActorRole" text NOT NULL,"RegisteredAt" timestamptz NOT NULL DEFAULT clock_timestamp(),"RegisteredBy" name NOT NULL DEFAULT session_user,
  UNIQUE("OrganizationId","Operation","IdempotencyKeySha256"));
CREATE TABLE advance.rev869b_command_attempts(
  "AttemptId" uuid PRIMARY KEY,"CommandId" uuid NOT NULL REFERENCES advance.rev869b_command_requests("CommandId"),"AttemptOrdinal" integer NOT NULL CHECK("AttemptOrdinal">0),"ExecutionInstanceId" uuid NOT NULL,
  "ServiceInstanceSha256" bytea NOT NULL CHECK(octet_length("ServiceInstanceSha256")=32),"OwnershipLeaseSha256" bytea NOT NULL CHECK(octet_length("OwnershipLeaseSha256")=32),"RuntimePrincipal" name NOT NULL,
  "TargetBackendPid" integer NOT NULL,"TargetTransactionId" bigint NOT NULL,"IsActive" boolean NOT NULL DEFAULT true,"StartedAt" timestamptz NOT NULL DEFAULT clock_timestamp(),"StartedBy" name NOT NULL DEFAULT session_user,UNIQUE("CommandId","AttemptOrdinal"));
CREATE UNIQUE INDEX "UX_rev869b_one_active_command_attempt" ON advance.rev869b_command_attempts("CommandId") WHERE "IsActive";
CREATE TABLE advance.rev869b_command_contexts(
  "ContextToken" uuid PRIMARY KEY,"AttemptId" uuid NOT NULL UNIQUE REFERENCES advance.rev869b_command_attempts("AttemptId"),"OrganizationId" text NOT NULL,"BackendPid" integer NOT NULL,"TransactionId" bigint NOT NULL,"RuntimePrincipal" name NOT NULL,
  "SlotSha256" bytea NOT NULL CHECK(octet_length("SlotSha256")=32),"Slots" jsonb NOT NULL CHECK(jsonb_typeof("Slots")='array' AND jsonb_array_length("Slots")>0),"SlotCount" integer NOT NULL CHECK("SlotCount">0),"OpenedAt" timestamptz NOT NULL DEFAULT transaction_timestamp());
CREATE INDEX "IX_rev869b_command_contexts_organization_opened_token" ON advance.rev869b_command_contexts("OrganizationId","OpenedAt","ContextToken") INCLUDE("AttemptId");
CREATE TABLE advance.rev869b_command_claims(
  "ClaimId" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,"ContextToken" uuid NOT NULL REFERENCES advance.rev869b_command_contexts("ContextToken"),"ClaimKind" text NOT NULL,"HistoryId" uuid NOT NULL,"EntityType" text NOT NULL,"EntityId" uuid NOT NULL,
  "Operation" text NOT NULL,"ParentVersion" bigint NOT NULL,"FromStatus" text NULL,"ToStatus" text NOT NULL,"Correlation" text NOT NULL,"RemarksSha256" bytea NOT NULL CHECK(octet_length("RemarksSha256")=32),UNIQUE("ContextToken","ClaimKind","HistoryId"));
CREATE TABLE advance.rev869b_command_attempt_outcomes(
  "OutcomeId" uuid PRIMARY KEY,"AttemptId" uuid NOT NULL UNIQUE REFERENCES advance.rev869b_command_attempts("AttemptId"),"TerminalState" text NOT NULL CHECK("TerminalState" IN ('Committed','Rejected','RolledBack','Abandoned')),
  "Category" text NULL,"BusinessSha256" bytea NULL,"OccurredAt" timestamptz NOT NULL DEFAULT clock_timestamp(),"RecordedBy" name NOT NULL DEFAULT session_user,
  CHECK(("TerminalState"='Committed' AND octet_length("BusinessSha256")=32 AND "Category" IS NULL) OR ("TerminalState"<>'Committed' AND "BusinessSha256" IS NULL AND length("Category") BETWEEN 1 AND 100)));
CREATE TABLE advance.rev869b_command_receipts(
  "ReceiptId" uuid PRIMARY KEY,"AttemptId" uuid NOT NULL UNIQUE REFERENCES advance.rev869b_command_attempts("AttemptId"),"CommandId" uuid NOT NULL UNIQUE REFERENCES advance.rev869b_command_requests("CommandId"),
  "BusinessSha256" bytea NOT NULL CHECK(octet_length("BusinessSha256")=32),"ResponseJson" jsonb NOT NULL,"CommittedAt" timestamptz NOT NULL DEFAULT transaction_timestamp());
CREATE TABLE advance.rev869b_target_instance_identity(
  "IdentityId" boolean PRIMARY KEY DEFAULT true CHECK("IdentityId"),"InstanceId" uuid NOT NULL UNIQUE,"LeaseId" uuid NOT NULL UNIQUE,
  "DatabaseName" name NOT NULL,"InstanceSha256" bytea NOT NULL UNIQUE CHECK(octet_length("InstanceSha256")=32),
  "CreatedAt" timestamptz NOT NULL DEFAULT clock_timestamp(),CHECK("DatabaseName"=current_database()));
WITH identity AS (SELECT gen_random_uuid() AS id)
INSERT INTO advance.rev869b_target_instance_identity("IdentityId","InstanceId","LeaseId","DatabaseName","InstanceSha256")
SELECT true,id,current_setting('advance.rev869b_lease_id')::uuid,current_database(),digest(convert_to(current_database()||':'||id::text,'UTF8'),'sha256') FROM identity
WHERE current_setting('advance.rev869b_lease_id')::uuid<>'00000000-0000-0000-0000-000000000000'::uuid;
CREATE TABLE advance.rev869b_purge_authorizations(
  "AuthorizationId" uuid PRIMARY KEY,"ManagementDecisionId" uuid NOT NULL UNIQUE,"RootAuthorizationId" uuid NOT NULL,"PriorAttemptId" uuid NULL,"AuthorizedBatchId" uuid NOT NULL UNIQUE,
  "TargetInstanceSha256" bytea NOT NULL CHECK(octet_length("TargetInstanceSha256")=32),"Operation" text NOT NULL CHECK("Operation"='CommandContextRetentionPurge'),
  "Scope" text NOT NULL,"Cutoff" timestamptz NOT NULL,"MaximumRows" integer NOT NULL CHECK("MaximumRows" BETWEEN 1 AND 1000),"RetryOrdinal" integer NOT NULL CHECK("RetryOrdinal">=0),
  "PriorTerminalOutcome" text NULL CHECK("PriorTerminalOutcome" IS NULL OR "PriorTerminalOutcome" IN ('Failed','Interrupted')),"PriorEvidenceSha256" bytea NULL CHECK("PriorEvidenceSha256" IS NULL OR octet_length("PriorEvidenceSha256")=32),
  "NonceSha256" bytea NOT NULL CHECK(octet_length("NonceSha256")=32),"ExpiresAt" timestamptz NOT NULL,"State" text NOT NULL DEFAULT 'Approved' CHECK("State" IN ('Approved','Expired','ZeroRows','Started','Succeeded','Failed','Interrupted')),
  "IssuedAt" timestamptz NOT NULL DEFAULT clock_timestamp(),"IssuedBy" name NOT NULL DEFAULT session_user,CHECK("ExpiresAt">"IssuedAt" AND "ExpiresAt"<="IssuedAt"+interval '15 minutes'),
  CHECK(("PriorAttemptId" IS NULL AND "RootAuthorizationId"="AuthorizationId" AND "RetryOrdinal"=0 AND "PriorTerminalOutcome" IS NULL AND "PriorEvidenceSha256" IS NULL) OR ("PriorAttemptId" IS NOT NULL AND "RetryOrdinal">0 AND "PriorTerminalOutcome" IS NOT NULL AND octet_length("PriorEvidenceSha256")=32)));
CREATE TABLE advance.rev869b_purge_attempts(
  "PurgeAttemptId" uuid PRIMARY KEY,"AuthorizationId" uuid NOT NULL UNIQUE REFERENCES advance.rev869b_purge_authorizations("AuthorizationId"),"CandidateCount" integer NOT NULL,"CandidateSha256" bytea NOT NULL CHECK(octet_length("CandidateSha256")=32),
  "State" text NOT NULL CHECK("State" IN ('ZeroRows','Started','Succeeded','Failed','Interrupted')),"StartedAt" timestamptz NOT NULL DEFAULT clock_timestamp(),"TerminalAt" timestamptz NULL,"FailureCategory" text NULL);
ALTER TABLE advance.rev869b_purge_authorizations ADD CONSTRAINT "FK_rev869b_purge_authorizations_prior_attempt" FOREIGN KEY("PriorAttemptId") REFERENCES advance.rev869b_purge_attempts("PurgeAttemptId");
ALTER TABLE advance.rev869b_purge_authorizations ADD CONSTRAINT "FK_rev869b_purge_authorizations_root" FOREIGN KEY("RootAuthorizationId") REFERENCES advance.rev869b_purge_authorizations("AuthorizationId");
CREATE UNIQUE INDEX "UX_rev869b_purge_authorizations_prior_attempt" ON advance.rev869b_purge_authorizations("PriorAttemptId") WHERE "PriorAttemptId" IS NOT NULL AND "State"<>'Expired';
CREATE TABLE advance.rev869b_purge_candidates("PurgeAttemptId" uuid NOT NULL REFERENCES advance.rev869b_purge_attempts("PurgeAttemptId"),"LedgerName" text NOT NULL,"RowId" uuid NOT NULL,PRIMARY KEY("PurgeAttemptId","LedgerName","RowId"));
CREATE TABLE advance.rev869b_purge_events("EventId" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,"PurgeAttemptId" uuid NOT NULL,"State" text NOT NULL,"EvidenceSha256" bytea NOT NULL CHECK(octet_length("EvidenceSha256")=32),"OccurredAt" timestamptz NOT NULL DEFAULT clock_timestamp(),"Principal" name NOT NULL DEFAULT session_user,UNIQUE("PurgeAttemptId","State"));
CREATE TABLE advance.rev869b_export_authorizations(
  "AuthorizationId" uuid PRIMARY KEY,"ManagementDecisionId" uuid NOT NULL UNIQUE,"OrganizationId" text NOT NULL,"Fields" text[] NOT NULL,"MaximumRows" integer NOT NULL CHECK("MaximumRows" BETWEEN 1 AND 1000),"AsOf" timestamptz NOT NULL,"ExpiresAt" timestamptz NOT NULL,
  "State" text NOT NULL DEFAULT 'Approved' CHECK("State" IN ('Approved','Prepared','Expired')),"IssuedAt" timestamptz NOT NULL DEFAULT clock_timestamp(),"IssuedBy" name NOT NULL DEFAULT session_user,CHECK("ExpiresAt">"IssuedAt" AND "ExpiresAt"<="IssuedAt"+interval '15 minutes'));
CREATE TABLE advance.rev869b_export_batches(
  "BatchId" uuid PRIMARY KEY,"AuthorizationId" uuid NOT NULL UNIQUE REFERENCES advance.rev869b_export_authorizations("AuthorizationId"),"ManagementDecisionId" uuid NOT NULL UNIQUE,"OrganizationId" text NOT NULL,"Fields" text[] NOT NULL,"MaximumRows" integer NOT NULL CHECK("MaximumRows" BETWEEN 1 AND 1000),"AsOf" timestamptz NOT NULL,"ExpiresAt" timestamptz NOT NULL,
  "RowCount" integer NOT NULL,"BatchSha256" bytea NOT NULL CHECK(octet_length("BatchSha256")=32),"PreparedAt" timestamptz NOT NULL DEFAULT clock_timestamp(),CHECK("ExpiresAt">"PreparedAt" AND "ExpiresAt"<="PreparedAt"+interval '15 minutes'));
CREATE TABLE advance.rev869b_export_batch_rows("BatchId" uuid NOT NULL REFERENCES advance.rev869b_export_batches("BatchId"),"Ordinal" integer NOT NULL,"Payload" jsonb NOT NULL,"RowSha256" bytea NOT NULL CHECK(octet_length("RowSha256")=32),PRIMARY KEY("BatchId","Ordinal"));
CREATE TABLE advance.rev869b_export_releases("ReleaseId" uuid PRIMARY KEY,"BatchId" uuid NOT NULL REFERENCES advance.rev869b_export_batches("BatchId"),"State" text NOT NULL CHECK("State" IN ('ReleaseStarted','Delivered','Failed','Interrupted')),"StartedAt" timestamptz NOT NULL DEFAULT clock_timestamp(),"TerminalAt" timestamptz NULL,"OutcomeCategory" text NULL);
CREATE UNIQUE INDEX "UX_rev869b_one_active_export_release" ON advance.rev869b_export_releases("BatchId") WHERE "State"='ReleaseStarted';
CREATE TABLE advance.rev869b_target_catalogue_manifest("ManifestId" boolean PRIMARY KEY DEFAULT true CHECK("ManifestId"),"CatalogueSha256" text NULL CHECK("CatalogueSha256" IS NULL OR "CatalogueSha256"~'^[0-9a-f]{64}$'));
INSERT INTO advance.rev869b_target_catalogue_manifest VALUES(true,NULL);

CREATE FUNCTION advance.rev869b_deny_ledger_mutation() RETURNS trigger LANGUAGE plpgsql AS $f$ BEGIN RAISE EXCEPTION 'REV869B durable ledger is append-only'; END $f$;
CREATE TRIGGER "TR_rev869b_command_outcomes_immutable" BEFORE UPDATE OR DELETE ON advance.rev869b_command_attempt_outcomes FOR EACH ROW EXECUTE FUNCTION advance.rev869b_deny_ledger_mutation();
CREATE TRIGGER "TR_rev869b_command_receipts_immutable" BEFORE UPDATE OR DELETE ON advance.rev869b_command_receipts FOR EACH ROW EXECUTE FUNCTION advance.rev869b_deny_ledger_mutation();
CREATE TRIGGER "TR_rev869b_target_instance_identity_immutable" BEFORE UPDATE OR DELETE ON advance.rev869b_target_instance_identity FOR EACH ROW EXECUTE FUNCTION advance.rev869b_deny_ledger_mutation();
CREATE TRIGGER "TR_rev869b_purge_authorizations_no_delete" BEFORE DELETE ON advance.rev869b_purge_authorizations FOR EACH ROW EXECUTE FUNCTION advance.rev869b_deny_ledger_mutation();
CREATE TRIGGER "TR_rev869b_purge_attempts_no_delete" BEFORE DELETE ON advance.rev869b_purge_attempts FOR EACH ROW EXECUTE FUNCTION advance.rev869b_deny_ledger_mutation();
CREATE TRIGGER "TR_rev869b_purge_events_immutable" BEFORE UPDATE OR DELETE ON advance.rev869b_purge_events FOR EACH ROW EXECUTE FUNCTION advance.rev869b_deny_ledger_mutation();
CREATE TRIGGER "TR_rev869b_export_rows_immutable" BEFORE UPDATE OR DELETE ON advance.rev869b_export_batch_rows FOR EACH ROW EXECUTE FUNCTION advance.rev869b_deny_ledger_mutation();

CREATE FUNCTION advance.rev869b_register_command_request(organization text,operation text,idempotency_sha bytea,request_sha bytea,actor_employee uuid,identity_issuer text,identity_subject text,actor_role text)
RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$ DECLARE command_id uuid; existing advance.rev869b_command_requests%ROWTYPE; BEGIN
  IF session_user<>'nexa_rev869b_command_audit' OR organization='' OR operation='' OR octet_length(idempotency_sha)<>32 OR octet_length(request_sha)<>32 THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Exact command request required'; END IF;
  SELECT * INTO existing FROM advance.rev869b_command_requests r WHERE r."OrganizationId"=organization AND r."Operation"=operation AND r."IdempotencyKeySha256"=idempotency_sha FOR UPDATE;
  IF FOUND THEN IF existing."RequestSha256"<>request_sha OR existing."ActorEmployeeId"<>actor_employee OR existing."IdentityIssuer"<>identity_issuer OR existing."IdentitySubject"<>identity_subject OR existing."ActorRole"<>actor_role THEN RAISE EXCEPTION USING ERRCODE='23505',CONSTRAINT='rev869b_command_request_replay_mismatch',MESSAGE='Idempotency replay mismatch'; END IF; RETURN existing."CommandId"; END IF;
  command_id:=gen_random_uuid(); INSERT INTO advance.rev869b_command_requests VALUES(command_id,organization,operation,idempotency_sha,request_sha,actor_employee,identity_issuer,identity_subject,actor_role,clock_timestamp(),session_user); RETURN command_id;
END $f$;
CREATE FUNCTION advance.rev869b_start_command_attempt(command_id uuid,execution_instance uuid,service_sha bytea,ownership_sha bytea,runtime_principal name,backend_pid integer,transaction_id bigint)
RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$ DECLARE attempt_id uuid:=gen_random_uuid(); ordinal integer; active advance.rev869b_command_attempts%ROWTYPE; BEGIN
  IF session_user<>'nexa_rev869b_command_audit' OR octet_length(service_sha)<>32 OR octet_length(ownership_sha)<>32 OR runtime_principal<>'nexa_rev869b_app_runtime' THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Exact command attempt required'; END IF;
  PERFORM 1 FROM advance.rev869b_command_requests WHERE "CommandId"=command_id FOR UPDATE; IF EXISTS(SELECT 1 FROM advance.rev869b_command_receipts WHERE "CommandId"=command_id) THEN RETURN NULL; END IF;
  SELECT * INTO active FROM advance.rev869b_command_attempts WHERE "CommandId"=command_id AND "IsActive"; IF FOUND THEN IF active."ExecutionInstanceId"=execution_instance AND active."ServiceInstanceSha256"=service_sha AND active."OwnershipLeaseSha256"=ownership_sha AND active."RuntimePrincipal"=runtime_principal AND active."TargetBackendPid"=backend_pid AND active."TargetTransactionId"=transaction_id THEN RETURN active."AttemptId"; END IF; RAISE EXCEPTION USING ERRCODE='40001',CONSTRAINT='rev869b_command_attempt_active',MESSAGE='Another bound command attempt is active'; END IF;
  SELECT coalesce(max("AttemptOrdinal"),0)+1 INTO ordinal FROM advance.rev869b_command_attempts WHERE "CommandId"=command_id; INSERT INTO advance.rev869b_command_attempts VALUES(attempt_id,command_id,ordinal,execution_instance,service_sha,ownership_sha,runtime_principal,backend_pid,transaction_id,true,clock_timestamp(),session_user); RETURN attempt_id;
END $f$;
CREATE FUNCTION advance.rev869b_open_command_attempt(attempt_id uuid,actor_employee uuid,identity_issuer text,identity_subject text,actor_role text,organization text,slot_sha bytea,slots jsonb)
RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$ DECLARE token uuid:=gen_random_uuid(); BEGIN
  IF session_user<>'nexa_rev869b_app_runtime' OR jsonb_typeof(slots)<>'array' OR jsonb_array_length(slots)=0 OR octet_length(slot_sha)<>32 THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Exact runtime context required'; END IF;
  IF NOT EXISTS(SELECT 1 FROM advance.rev869b_command_attempts a JOIN advance.rev869b_command_requests r ON r."CommandId"=a."CommandId" WHERE a."AttemptId"=attempt_id AND a."IsActive" AND a."RuntimePrincipal"=session_user AND a."TargetBackendPid"=pg_backend_pid() AND a."TargetTransactionId"=txid_current() AND r."ActorEmployeeId"=actor_employee AND r."IdentityIssuer"=identity_issuer AND r."IdentitySubject"=identity_subject AND r."ActorRole"=actor_role AND r."OrganizationId"=organization) THEN RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_attempt_binding',MESSAGE='Attempt binding mismatch'; END IF;
  PERFORM pg_advisory_xact_lock(hashtextextended(attempt_id::text,86923));
  PERFORM pg_advisory_xact_lock(hashtextextended(attempt_id::text,86924));

  SELECT "ContextToken" INTO token FROM advance.rev869b_command_contexts WHERE "AttemptId"=attempt_id AND "BackendPid"=pg_backend_pid() AND "TransactionId"=txid_current();
  IF FOUND THEN
    UPDATE advance.rev869b_command_contexts SET "SlotSha256"=digest("SlotSha256"||slot_sha,'sha256'),"Slots"="Slots"||slots,"SlotCount"="SlotCount"+jsonb_array_length(slots) WHERE "ContextToken"=token;
  ELSE
    token:=gen_random_uuid(); INSERT INTO advance.rev869b_command_contexts VALUES(token,attempt_id,organization,pg_backend_pid(),txid_current(),session_user,slot_sha,slots,jsonb_array_length(slots),transaction_timestamp());
  END IF;
  PERFORM set_config('advance.rev869b_command_token',token::text,true); PERFORM set_config('advance.rev869b_actor_employee_id',actor_employee::text,true); PERFORM set_config('advance.rev869b_identity_issuer',identity_issuer,true); PERFORM set_config('advance.rev869b_identity_subject',identity_subject,true); PERFORM set_config('advance.rev869b_actor_login',identity_subject,true); PERFORM set_config('advance.rev869b_actor_role',actor_role,true); PERFORM set_config('advance.rev869b_organization',organization,true); RETURN token;
END $f$;
CREATE FUNCTION advance.rev869b_command_context_valid(organization text,actor_employee uuid,identity_issuer text,identity_subject text,actor_role text)
RETURNS boolean LANGUAGE sql SECURITY DEFINER STABLE SET search_path=pg_catalog,advance AS $f$ SELECT count(*)=1 FROM advance.rev869b_command_contexts c JOIN advance.rev869b_command_attempts a ON a."AttemptId"=c."AttemptId" JOIN advance.rev869b_command_requests r ON r."CommandId"=a."CommandId" WHERE c."ContextToken"=nullif(current_setting('advance.rev869b_command_token',true),'')::uuid AND c."BackendPid"=pg_backend_pid() AND c."TransactionId"=txid_current() AND c."RuntimePrincipal"=session_user AND a."IsActive" AND r."OrganizationId"=$1 AND r."ActorEmployeeId"=$2 AND r."IdentityIssuer"=$3 AND r."IdentitySubject"=$4 AND r."ActorRole"=$5 $f$;
CREATE FUNCTION advance.rev869b_claim_command_context(claim_kind text,history_id uuid,entity_type text,entity_id uuid,operation text,parent_version bigint,from_status text,to_status text,correlation text,remarks text)
RETURNS void LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$ DECLARE token uuid:=nullif(current_setting('advance.rev869b_command_token',true),'')::uuid; expected jsonb; BEGIN
  SELECT value INTO expected FROM advance.rev869b_command_contexts c,jsonb_array_elements(c."Slots") value WHERE c."ContextToken"=token AND value->>'claimKind'=claim_kind AND (value->>'historyId')::uuid=history_id AND value->>'entityType'=entity_type AND (value->>'entityId')::uuid=entity_id AND value->>'operation'=operation AND (value->>'parentVersion')::bigint=parent_version AND value->>'toStatus'=to_status AND value->>'correlation'=correlation;
  IF expected IS NULL OR (expected->>'fromStatus') IS DISTINCT FROM from_status OR expected->>'remarks' IS DISTINCT FROM remarks THEN RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_exact_command_slot',MESSAGE='Exact command slot mismatch'; END IF;
  INSERT INTO advance.rev869b_command_claims("ContextToken","ClaimKind","HistoryId","EntityType","EntityId","Operation","ParentVersion","FromStatus","ToStatus","Correlation","RemarksSha256") VALUES(token,claim_kind,history_id,entity_type,entity_id,operation,parent_version,from_status,to_status,correlation,digest(remarks,'sha256'));
END $f$;
CREATE FUNCTION advance.rev869b_commit_command_attempt(attempt_id uuid,business_sha bytea,response_json jsonb,receipt_id uuid)
RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$ DECLARE command_id uuid; expected integer; actual integer; exact_business_sha bytea; BEGIN
  IF session_user<>'nexa_rev869b_app_runtime' OR octet_length(business_sha)<>32 THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Exact runtime commit required'; END IF;
  SELECT a."CommandId",c."SlotCount",c."SlotSha256" INTO STRICT command_id,expected,exact_business_sha FROM advance.rev869b_command_attempts a JOIN advance.rev869b_command_contexts c ON c."AttemptId"=a."AttemptId" WHERE a."AttemptId"=attempt_id AND a."IsActive" AND c."BackendPid"=pg_backend_pid() AND c."TransactionId"=txid_current(); SELECT count(*) INTO actual FROM advance.rev869b_command_claims q JOIN advance.rev869b_command_contexts c ON c."ContextToken"=q."ContextToken" WHERE c."AttemptId"=attempt_id;
  IF business_sha<>exact_business_sha THEN RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_command_business_fingerprint',MESSAGE='Committed business fingerprint mismatch'; END IF;
  IF actual<>expected THEN RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_command_claim_coverage',MESSAGE='Claim coverage mismatch'; END IF;
  INSERT INTO advance.rev869b_command_receipts VALUES(receipt_id,attempt_id,command_id,exact_business_sha,response_json,transaction_timestamp()); INSERT INTO advance.rev869b_command_attempt_outcomes VALUES(gen_random_uuid(),attempt_id,'Committed',NULL,exact_business_sha,transaction_timestamp(),session_user); UPDATE advance.rev869b_command_attempts SET "IsActive"=false WHERE "AttemptId"=attempt_id; RETURN receipt_id;
END $f$;
CREATE FUNCTION advance.rev869b_record_noncommit_outcome(attempt_id uuid,execution_instance uuid,service_sha bytea,ownership_sha bytea,terminal_state text,category text,outcome_id uuid)
RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$ DECLARE existing advance.rev869b_command_attempt_outcomes%ROWTYPE; BEGIN
  IF session_user<>'nexa_rev869b_command_audit' OR terminal_state NOT IN ('Rejected','RolledBack','Abandoned') OR length(category) NOT BETWEEN 1 AND 100 OR octet_length(service_sha)<>32 OR octet_length(ownership_sha)<>32 THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Exact noncommit outcome required'; END IF;
  IF terminal_state IN ('RolledBack','Abandoned') AND (NOT pg_try_advisory_xact_lock(hashtextextended(attempt_id::text,86923)) OR NOT pg_try_advisory_xact_lock(hashtextextended(attempt_id::text,86924))) THEN RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_transaction_still_active',MESSAGE='The exact command transaction is still active'; END IF;

  SELECT * INTO existing FROM advance.rev869b_command_attempt_outcomes WHERE "AttemptId"=attempt_id;
  IF FOUND THEN IF existing."OutcomeId"<>outcome_id OR existing."TerminalState"<>terminal_state OR existing."Category"<>category OR existing."RecordedBy"<>session_user OR NOT EXISTS(SELECT 1 FROM advance.rev869b_command_attempts a WHERE a."AttemptId"=attempt_id AND a."ExecutionInstanceId"=execution_instance AND a."ServiceInstanceSha256"=service_sha AND a."OwnershipLeaseSha256"=ownership_sha) THEN RAISE EXCEPTION USING ERRCODE='23505',CONSTRAINT='rev869b_noncommit_replay_mismatch',MESSAGE='Noncommit outcome replay mismatch'; END IF; RETURN existing."OutcomeId"; END IF;
  IF NOT EXISTS(
    SELECT 1 FROM advance.rev869b_command_attempts a
    WHERE a."AttemptId"=attempt_id AND a."IsActive" AND a."ExecutionInstanceId"=execution_instance
      AND a."ServiceInstanceSha256"=service_sha AND a."OwnershipLeaseSha256"=ownership_sha
      AND (
        (terminal_state='Rejected' AND NOT EXISTS(SELECT 1 FROM advance.rev869b_command_contexts c WHERE c."AttemptId"=a."AttemptId"))
        OR
        (terminal_state='RolledBack'
          AND NOT EXISTS(SELECT 1 FROM advance.rev869b_command_receipts r WHERE r."AttemptId"=a."AttemptId")
          AND NOT EXISTS(SELECT 1 FROM advance.rev869b_command_contexts c WHERE c."AttemptId"=a."AttemptId" AND (c."BackendPid"<>a."TargetBackendPid" OR c."TransactionId"<>a."TargetTransactionId")))
        OR
        (terminal_state='Abandoned' AND (
          NOT EXISTS(SELECT 1 FROM advance.rev869b_command_contexts c WHERE c."AttemptId"=a."AttemptId")
          OR EXISTS(
            SELECT 1 FROM advance.rev869b_command_contexts c
            WHERE c."AttemptId"=a."AttemptId" AND c."BackendPid"=a."TargetBackendPid" AND c."TransactionId"=a."TargetTransactionId"
        )))
      ))
    OR EXISTS(SELECT 1 FROM advance.rev869b_command_receipts WHERE "AttemptId"=attempt_id)
  THEN RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_noncommit_terminalizer_binding',MESSAGE='Noncommit terminalizer binding or authoritative no-commit proof mismatch'; END IF;
  INSERT INTO advance.rev869b_command_attempt_outcomes VALUES(outcome_id,attempt_id,terminal_state,category,NULL,clock_timestamp(),session_user); UPDATE advance.rev869b_command_attempts SET "IsActive"=false WHERE "AttemptId"=attempt_id AND "IsActive" AND "ExecutionInstanceId"=execution_instance AND "ServiceInstanceSha256"=service_sha AND "OwnershipLeaseSha256"=ownership_sha; IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='40001',MESSAGE='Attempt terminalization conflict'; END IF; RETURN outcome_id;
END $f$;
CREATE FUNCTION advance.rev869b_reconcile_command_attempt(attempt_id uuid) RETURNS jsonb LANGUAGE sql SECURITY DEFINER STABLE SET search_path=pg_catalog,advance AS $f$ SELECT jsonb_build_object('attemptId',a."AttemptId",'ordinal',a."AttemptOrdinal",'active',a."IsActive",'terminal',o."TerminalState",'receiptId',r."ReceiptId",'response',r."ResponseJson") FROM advance.rev869b_command_attempts a LEFT JOIN advance.rev869b_command_attempt_outcomes o ON o."AttemptId"=a."AttemptId" LEFT JOIN advance.rev869b_command_receipts r ON r."AttemptId"=a."AttemptId" WHERE a."AttemptId"=$1 AND session_user IN ('nexa_rev869b_command_audit','nexa_rev869b_target_verifier') $f$;
CREATE FUNCTION advance.rev869b_read_target_security_state() RETURNS TABLE(request_count bigint,context_count bigint,receipt_count bigint,outcome_count bigint,attempt_count bigint,attempt_sha256 text,target_instance_sha256 text) LANGUAGE sql SECURITY DEFINER STABLE SET search_path=pg_catalog,advance AS $f$
  SELECT (SELECT count(*) FROM advance.rev869b_command_requests),(SELECT count(*) FROM advance.rev869b_command_contexts),(SELECT count(*) FROM advance.rev869b_command_receipts),(SELECT count(*) FROM advance.rev869b_command_attempt_outcomes),(SELECT count(*) FROM advance.rev869b_command_attempts),(SELECT encode(digest(coalesce(string_agg("AttemptId"::text,',' ORDER BY "AttemptId"),''),'sha256'),'hex') FROM advance.rev869b_command_attempts),(SELECT encode("InstanceSha256",'hex') FROM advance.rev869b_target_instance_identity WHERE "IdentityId") WHERE session_user='nexa_rev869b_target_verifier' $f$;
CREATE FUNCTION advance.rev869b_read_command_evidence(command_id uuid,attempt_id uuid) RETURNS jsonb LANGUAGE sql SECURITY DEFINER STABLE SET search_path=pg_catalog,advance AS $f$
  SELECT jsonb_build_object(
    'targetIdentity',(SELECT jsonb_build_object('instanceId',i."InstanceId",'databaseName',i."DatabaseName",'instanceSha256',encode(i."InstanceSha256",'hex')) FROM advance.rev869b_target_instance_identity i WHERE i."IdentityId"),
    'request',(SELECT jsonb_build_object('commandId',r."CommandId",'organizationId',r."OrganizationId",'operation',r."Operation",'idempotencyKeySha256',encode(r."IdempotencyKeySha256",'hex'),'requestSha256',encode(r."RequestSha256",'hex'),'actorEmployeeId',r."ActorEmployeeId",'identityIssuer',r."IdentityIssuer",'identitySubject',r."IdentitySubject",'actorRole',r."ActorRole") FROM advance.rev869b_command_requests r WHERE r."CommandId"=$1),
    'requestCount',(SELECT count(*) FROM advance.rev869b_command_requests r WHERE r."CommandId"=$1),
    'attempt',(SELECT to_jsonb(a) FROM advance.rev869b_command_attempts a WHERE a."CommandId"=$1 AND a."AttemptId"=$2),
    'attemptCount',(SELECT count(*) FROM advance.rev869b_command_attempts a WHERE a."CommandId"=$1 AND a."AttemptId"=$2),
    'activeAttemptCount',(SELECT count(*) FROM advance.rev869b_command_attempts a WHERE a."CommandId"=$1 AND a."IsActive"),
    'context',(SELECT to_jsonb(x) FROM advance.rev869b_command_contexts x WHERE x."AttemptId"=$2),
    'contextCount',(SELECT count(*) FROM advance.rev869b_command_contexts x WHERE x."AttemptId"=$2),
    'claims',coalesce((SELECT jsonb_agg(to_jsonb(q) ORDER BY q."ClaimId") FROM advance.rev869b_command_claims q JOIN advance.rev869b_command_contexts x ON x."ContextToken"=q."ContextToken" WHERE x."AttemptId"=$2),'[]'::jsonb),
    'businessRows',coalesce((SELECT jsonb_agg(jsonb_build_object('claimId',q."ClaimId",'entityType',q."EntityType",'entityId',q."EntityId",'operation',q."Operation",'parentVersion',q."ParentVersion",'toStatus',q."ToStatus",'correlation',q."Correlation") ORDER BY q."ClaimId") FROM advance.rev869b_command_claims q JOIN advance.rev869b_command_contexts x ON x."ContextToken"=q."ContextToken" WHERE x."AttemptId"=$2 AND q."ClaimKind"='Business'),'[]'::jsonb),
    'historyRows',coalesce((SELECT jsonb_agg(jsonb_build_object('claimId',q."ClaimId",'historyId',q."HistoryId",'entityType',q."EntityType",'entityId',q."EntityId",'fromStatus',q."FromStatus",'toStatus',q."ToStatus",'correlation',q."Correlation",'remarksSha256',encode(q."RemarksSha256",'hex')) ORDER BY q."ClaimId") FROM advance.rev869b_command_claims q JOIN advance.rev869b_command_contexts x ON x."ContextToken"=q."ContextToken" WHERE x."AttemptId"=$2 AND q."ClaimKind"<>'Business'),'[]'::jsonb),
    'businessRowCount',(SELECT count(*) FROM advance.rev869b_command_claims q JOIN advance.rev869b_command_contexts x ON x."ContextToken"=q."ContextToken" WHERE x."AttemptId"=$2 AND q."ClaimKind"='Business'),
    'historyRowCount',(SELECT count(*) FROM advance.rev869b_command_claims q JOIN advance.rev869b_command_contexts x ON x."ContextToken"=q."ContextToken" WHERE x."AttemptId"=$2 AND q."ClaimKind"<>'Business'),
    'claimSha256',(SELECT encode(digest(coalesce(string_agg(q."ClaimKind"||':'||q."HistoryId"::text||':'||q."EntityType"||':'||q."EntityId"::text||':'||q."Operation"||':'||q."ParentVersion"::text||':'||coalesce(q."FromStatus",'')||':'||q."ToStatus"||':'||q."Correlation"||':'||encode(q."RemarksSha256",'hex'),',' ORDER BY q."ClaimId"),''),'sha256'),'hex') FROM advance.rev869b_command_claims q JOIN advance.rev869b_command_contexts x ON x."ContextToken"=q."ContextToken" WHERE x."AttemptId"=$2),
    'outcome',(SELECT to_jsonb(o) FROM advance.rev869b_command_attempt_outcomes o WHERE o."AttemptId"=$2),
    'receipt',(SELECT jsonb_build_object('receiptId',x."ReceiptId",'commandId',x."CommandId",'businessSha256',encode(x."BusinessSha256",'hex'),'responseSha256',encode(digest(x."ResponseJson"::text,'sha256'),'hex')) FROM advance.rev869b_command_receipts x WHERE x."AttemptId"=$2),
    'receiptCount',(SELECT count(*) FROM advance.rev869b_command_receipts x WHERE x."AttemptId"=$2),
    'outcomeCount',(SELECT count(*) FROM advance.rev869b_command_attempt_outcomes o WHERE o."AttemptId"=$2),
    'allAttemptIds',coalesce((SELECT jsonb_agg(a."AttemptId" ORDER BY a."AttemptOrdinal") FROM advance.rev869b_command_attempts a WHERE a."CommandId"=$1),'[]'::jsonb))
  WHERE $1<>'00000000-0000-0000-0000-000000000000'::uuid AND $2<>'00000000-0000-0000-0000-000000000000'::uuid AND session_user='nexa_rev869b_target_verifier' $f$;

CREATE FUNCTION advance.rev869b_read_purge_evidence(authorization_id uuid,purge_attempt_id uuid) RETURNS jsonb LANGUAGE sql SECURITY DEFINER STABLE SET search_path=pg_catalog,advance AS $f$
  SELECT jsonb_build_object(
    'targetIdentity',(SELECT jsonb_build_object('instanceId',i."InstanceId",'databaseName',i."DatabaseName",'instanceSha256',encode(i."InstanceSha256",'hex')) FROM advance.rev869b_target_instance_identity i WHERE i."IdentityId"),
    'authorization',(SELECT to_jsonb(a)-'NonceSha256' FROM advance.rev869b_purge_authorizations a WHERE a."AuthorizationId"=$1),
    'authorizationCount',(SELECT count(*) FROM advance.rev869b_purge_authorizations a WHERE a."AuthorizationId"=$1),
    'attempt',(SELECT to_jsonb(p) FROM advance.rev869b_purge_attempts p WHERE p."AuthorizationId"=$1 AND p."PurgeAttemptId"=$2),
    'attemptCount',(SELECT count(*) FROM advance.rev869b_purge_attempts p WHERE p."AuthorizationId"=$1 AND p."PurgeAttemptId"=$2),
    'rootAuthorization',(SELECT to_jsonb(root)-'NonceSha256' FROM advance.rev869b_purge_authorizations a JOIN advance.rev869b_purge_authorizations root ON root."AuthorizationId"=a."RootAuthorizationId" WHERE a."AuthorizationId"=$1),
    'priorAttempt',(SELECT to_jsonb(prior) FROM advance.rev869b_purge_authorizations a JOIN advance.rev869b_purge_attempts prior ON prior."PurgeAttemptId"=a."PriorAttemptId" WHERE a."AuthorizationId"=$1),
    'eligibleRows',coalesce((SELECT jsonb_agg(jsonb_build_object('ledgerName','command_contexts','rowId',x."ContextToken") ORDER BY x."ContextToken") FROM (SELECT c."ContextToken" FROM advance.rev869b_command_contexts c JOIN advance.rev869b_command_attempts ca ON ca."AttemptId"=c."AttemptId" JOIN advance.rev869b_command_requests cr ON cr."CommandId"=ca."CommandId" JOIN advance.rev869b_purge_authorizations a ON a."AuthorizationId"=$1 WHERE cr."OrganizationId"=substring(a."Scope" from 14) AND c."OpenedAt"<a."Cutoff" ORDER BY c."ContextToken" LIMIT (SELECT "MaximumRows" FROM advance.rev869b_purge_authorizations WHERE "AuthorizationId"=$1)) x),'[]'::jsonb),
    'candidates',coalesce((SELECT jsonb_agg(jsonb_build_object('ledgerName',c."LedgerName",'rowId',c."RowId") ORDER BY c."LedgerName",c."RowId") FROM advance.rev869b_purge_candidates c WHERE c."PurgeAttemptId"=$2),'[]'::jsonb),
    'candidateCount',(SELECT count(*) FROM advance.rev869b_purge_candidates c WHERE c."PurgeAttemptId"=$2),
    'candidateSha256',(SELECT encode(digest(coalesce(string_agg(c."LedgerName"||':'||c."RowId"::text,',' ORDER BY c."LedgerName",c."RowId"),''),'sha256'),'hex') FROM advance.rev869b_purge_candidates c WHERE c."PurgeAttemptId"=$2),
    'currentCandidateRows',coalesce((SELECT jsonb_agg(jsonb_build_object('ledgerName',p."LedgerName",'rowId',p."RowId",'active',a."IsActive") ORDER BY p."LedgerName",p."RowId") FROM advance.rev869b_purge_candidates p LEFT JOIN advance.rev869b_command_contexts c ON c."ContextToken"=p."RowId" LEFT JOIN advance.rev869b_command_attempts a ON a."AttemptId"=c."AttemptId" WHERE p."PurgeAttemptId"=$2),'[]'::jsonb),
    'contextRows',coalesce((SELECT jsonb_agg(jsonb_build_object('contextToken',c."ContextToken",'attemptId',c."AttemptId",'openedAt',c."OpenedAt") ORDER BY c."ContextToken") FROM advance.rev869b_command_contexts c),'[]'::jsonb),
    'contextSha256',(SELECT encode(digest(coalesce(string_agg(c."ContextToken"::text||':'||c."AttemptId"::text,',' ORDER BY c."ContextToken"),''),'sha256'),'hex') FROM advance.rev869b_command_contexts c),
    'events',coalesce((SELECT jsonb_agg(jsonb_build_object('eventId',e."EventId",'state',e."State",'evidenceSha256',encode(e."EvidenceSha256",'hex'),'principal',e."Principal") ORDER BY e."EventId") FROM advance.rev869b_purge_events e WHERE e."PurgeAttemptId"=$2),'[]'::jsonb),
    'eventCount',(SELECT count(*) FROM advance.rev869b_purge_events e WHERE e."PurgeAttemptId"=$2),
    'activeChildCount',(SELECT count(*) FROM advance.rev869b_purge_authorizations child WHERE child."PriorAttemptId"=$2 AND child."State"<>'Expired'))
  WHERE $1<>'00000000-0000-0000-0000-000000000000'::uuid AND $2<>'00000000-0000-0000-0000-000000000000'::uuid AND session_user='nexa_rev869b_target_verifier' $f$;

CREATE FUNCTION advance.rev869b_read_export_evidence(authorization_id uuid,batch_id uuid,release_id uuid) RETURNS jsonb LANGUAGE sql SECURITY DEFINER STABLE SET search_path=pg_catalog,advance AS $f$
  SELECT jsonb_build_object(
    'targetIdentity',(SELECT jsonb_build_object('instanceId',i."InstanceId",'databaseName',i."DatabaseName",'instanceSha256',encode(i."InstanceSha256",'hex')) FROM advance.rev869b_target_instance_identity i WHERE i."IdentityId"),
    'authorization',(SELECT to_jsonb(a) FROM advance.rev869b_export_authorizations a WHERE a."AuthorizationId"=$1),
    'authorizationCount',(SELECT count(*) FROM advance.rev869b_export_authorizations a WHERE a."AuthorizationId"=$1),
    'batch',(SELECT to_jsonb(b) FROM advance.rev869b_export_batches b WHERE b."AuthorizationId"=$1 AND b."BatchId"=$2),
    'batchCount',(SELECT count(*) FROM advance.rev869b_export_batches b WHERE b."AuthorizationId"=$1 AND b."BatchId"=$2),
    'release',(SELECT to_jsonb(x) FROM advance.rev869b_export_releases x WHERE x."BatchId"=$2 AND x."ReleaseId"=$3),
    'releases',coalesce((SELECT jsonb_agg(to_jsonb(x) ORDER BY x."StartedAt",x."ReleaseId") FROM advance.rev869b_export_releases x WHERE x."BatchId"=$2),'[]'::jsonb),
    'activeReleaseCount',(SELECT count(*) FROM advance.rev869b_export_releases x WHERE x."BatchId"=$2 AND x."State"='ReleaseStarted'),
    'deliveredReleaseCount',(SELECT count(*) FROM advance.rev869b_export_releases x WHERE x."BatchId"=$2 AND x."State"='Delivered'),
    'rows',coalesce((SELECT jsonb_agg(jsonb_build_object('ordinal',r."Ordinal",'fieldKeys',(SELECT jsonb_agg(k ORDER BY k) FROM jsonb_object_keys(r."Payload") k),'storedSha256',encode(r."RowSha256",'hex'),'recomputedSha256',encode(digest(r."Payload"::text,'sha256'),'hex')) ORDER BY r."Ordinal") FROM advance.rev869b_export_batch_rows r WHERE r."BatchId"=$2),'[]'::jsonb),
    'rowCount',(SELECT count(*) FROM advance.rev869b_export_batch_rows r WHERE r."BatchId"=$2),
    'recomputedBatchSha256',(SELECT encode(digest(coalesce(string_agg(encode(digest(r."Payload"::text,'sha256'),'hex'),',' ORDER BY r."Ordinal"),''),'sha256'),'hex') FROM advance.rev869b_export_batch_rows r WHERE r."BatchId"=$2))
  WHERE $1<>'00000000-0000-0000-0000-000000000000'::uuid AND $2<>'00000000-0000-0000-0000-000000000000'::uuid AND ($3 IS NULL OR $3<>'00000000-0000-0000-0000-000000000000'::uuid) AND session_user='nexa_rev869b_target_verifier' $f$;

CREATE FUNCTION advance.rev869b_read_target_acl_evidence() RETURNS jsonb LANGUAGE sql SECURITY DEFINER STABLE SET search_path=pg_catalog,advance AS $f$
  WITH facts(fact) AS (
    SELECT 'database|'||current_database()||'|'||pg_get_userbyid(d.datdba)||'|'||coalesce(d.datacl::text,'') FROM pg_database d WHERE d.datname=current_database()
    UNION ALL SELECT 'schema|'||n.nspname||'|'||pg_get_userbyid(n.nspowner)||'|'||coalesce(n.nspacl::text,'') FROM pg_namespace n WHERE n.nspname='advance'
    UNION ALL SELECT 'relation|'||c.oid::regclass::text||'|'||pg_get_userbyid(c.relowner)||'|'||coalesce(c.relacl::text,'') FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='advance'
    UNION ALL SELECT 'function|'||p.oid::regprocedure::text||'|'||pg_get_userbyid(p.proowner)||'|'||coalesce(p.proacl::text,'') FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='advance'
    UNION ALL SELECT 'defaultacl|'||pg_get_userbyid(d.defaclrole)||'|'||d.defaclobjtype::text||'|'||coalesce(d.defaclacl::text,'') FROM pg_default_acl d WHERE d.defaclnamespace='advance'::regnamespace
    UNION ALL SELECT 'role|'||r.rolname||'|'||r.rolcanlogin||'|'||r.rolinherit||'|'||r.rolsuper||'|'||r.rolcreatedb||'|'||r.rolcreaterole||'|'||r.rolreplication||'|'||r.rolbypassrls FROM pg_roles r WHERE r.rolname LIKE 'nexa_rev869b_%')
  SELECT jsonb_build_object(
    'targetIdentity',(SELECT jsonb_build_object('instanceId',i."InstanceId",'databaseName',i."DatabaseName",'instanceSha256',encode(i."InstanceSha256",'hex')) FROM advance.rev869b_target_instance_identity i WHERE i."IdentityId"),
    'facts',jsonb_agg(fact ORDER BY fact),'count',count(*),'sha256',encode(digest(string_agg(fact,E'\n' ORDER BY fact),'sha256'),'hex'),
    'ownerFacts',jsonb_agg(fact ORDER BY fact) FILTER(WHERE fact LIKE 'database|%' OR fact LIKE 'schema|%' OR fact LIKE 'relation|%' OR fact LIKE 'function|%'),
    'defaultPrivilegeFacts',jsonb_agg(fact ORDER BY fact) FILTER(WHERE fact LIKE 'defaultacl|%'),
    'roleFacts',jsonb_agg(fact ORDER BY fact) FILTER(WHERE fact LIKE 'role|%'),
    'protectedStateSha256',(SELECT encode(digest((SELECT count(*) FROM advance.rev869b_command_requests)::text||':'||(SELECT count(*) FROM advance.rev869b_command_attempts)::text||':'||(SELECT count(*) FROM advance.rev869b_command_attempt_outcomes)::text||':'||(SELECT count(*) FROM advance.rev869b_purge_authorizations)::text||':'||(SELECT count(*) FROM advance.rev869b_export_batches)::text,'sha256'),'hex')))
  FROM facts WHERE session_user='nexa_rev869b_target_verifier' $f$;
CREATE FUNCTION advance.rev869b_target_catalogue_fingerprint() RETURNS text LANGUAGE sql SECURITY DEFINER STABLE SET search_path=pg_catalog,advance AS $f$
  WITH facts(fact) AS (
    SELECT 'relation|'||c.oid::regclass::text||'|'||c.relkind::text||'|'||pg_get_userbyid(c.relowner)||'|'||coalesce(c.relacl::text,'') FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='advance'
    UNION ALL SELECT 'column|'||c.oid::regclass::text||'|'||a.attnum||'|'||a.attname||'|'||format_type(a.atttypid,a.atttypmod)||'|'||a.attnotnull||'|'||coalesce(pg_get_expr(d.adbin,d.adrelid),'') FROM pg_attribute a JOIN pg_class c ON c.oid=a.attrelid JOIN pg_namespace n ON n.oid=c.relnamespace LEFT JOIN pg_attrdef d ON d.adrelid=a.attrelid AND d.adnum=a.attnum WHERE n.nspname='advance' AND a.attnum>0 AND NOT a.attisdropped
    UNION ALL SELECT 'constraint|'||conrelid::regclass::text||'|'||conname||'|'||pg_get_constraintdef(oid,true) FROM pg_constraint WHERE connamespace='advance'::regnamespace
    UNION ALL SELECT 'index|'||indexrelid::regclass::text||'|'||pg_get_indexdef(indexrelid) FROM pg_index WHERE indrelid IN (SELECT c.oid FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='advance')
    UNION ALL SELECT 'trigger|'||tgrelid::regclass::text||'|'||tgname||'|'||pg_get_triggerdef(oid,true) FROM pg_trigger WHERE NOT tgisinternal AND tgrelid IN (SELECT c.oid FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='advance')
    UNION ALL SELECT 'function|'||p.oid::regprocedure::text||'|'||pg_get_userbyid(p.proowner)||'|'||pg_get_function_result(p.oid)||'|'||p.prosecdef||'|'||p.provolatile::text||'|'||coalesce(array_to_string(p.proconfig,','),'')||'|'||encode(digest(p.prosrc,'sha256'),'hex')||'|'||coalesce(p.proacl::text,'') FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='advance'
    UNION ALL SELECT 'schema|'||n.nspname||'|'||pg_get_userbyid(n.nspowner)||'|'||coalesce(n.nspacl::text,'') FROM pg_namespace n WHERE n.nspname='advance'
    UNION ALL SELECT 'defaultacl|'||pg_get_userbyid(d.defaclrole)||'|'||d.defaclobjtype::text||'|'||coalesce(d.defaclacl::text,'') FROM pg_default_acl d WHERE d.defaclnamespace='advance'::regnamespace)
  SELECT encode(digest(string_agg(fact,E'\n' ORDER BY fact),'sha256'),'hex') FROM facts $f$;

CREATE FUNCTION advance.rev869b_canonical_json_v3(value jsonb) RETURNS text LANGUAGE sql IMMUTABLE STRICT SET search_path=pg_catalog AS $f$
  SELECT CASE jsonb_typeof($1)
    WHEN 'object' THEN coalesce((SELECT '{'||string_agg(to_jsonb(key)::text||':'||advance.rev869b_canonical_json_v3(val),',' ORDER BY key COLLATE "C")||'}' FROM jsonb_each($1) e(key,val)),'{}')
    WHEN 'array' THEN coalesce((SELECT '['||string_agg(advance.rev869b_canonical_json_v3(val),',' ORDER BY ordinal)||']' FROM jsonb_array_elements($1) WITH ORDINALITY e(val,ordinal)),'[]')
    ELSE $1::text END $f$;
CREATE FUNCTION advance.rev869b_build_raw_facts_v4(reader_id text,scope jsonb,observation_stage text,requested_facts text[],allowed jsonb,actual_values jsonb) RETURNS jsonb LANGUAGE sql VOLATILE SET search_path=pg_catalog,advance AS $f$
  WITH requested AS (
    SELECT u.name,u.ordinality,a.value#>>'{type}' value_type,a.value#>>'{kind}' kind
    FROM unnest($4) WITH ORDINALITY u(name,ordinality) JOIN LATERAL (SELECT $5->u.name value) a ON a.value IS NOT NULL
  ), fact_rows AS (
    SELECT jsonb_build_object('kind',r.kind,'name',r.name,'valueType',r.value_type,'value',$6->r.name,'sourceRowCount',CASE WHEN jsonb_typeof($6->r.name)='array' THEN jsonb_array_length($6->r.name) ELSE 1 END,
      'sourceSha256',encode(digest($1||':'||r.name||':'||($6->r.name)::text||':'||$2::text,'sha256'),'hex')) fact,r.ordinality FROM requested r WHERE $6?r.name AND jsonb_typeof($6->r.name)<>'null'
  ), base AS (
    SELECT jsonb_build_object('readerSchemaVersion','REV869B-FACTS-v4','readerId',$1,'scope',$2,'observedAtUtc',to_char(statement_timestamp() AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US"Z"'),
      'transactionBoundary','tx:'||txid_current_snapshot()::text||':'||$3||':'||($2->>'scenarioExecutionId'),'facts',coalesce((SELECT jsonb_agg(fact ORDER BY ordinality) FROM fact_rows),'[]'::jsonb),'factCount',(SELECT count(*) FROM fact_rows)) payload)
  SELECT payload||jsonb_build_object('rawSha256',encode(digest(advance.rev869b_canonical_json_v3(payload),'sha256'),'hex')) FROM base
  WHERE $1 IN ('TC4','TP4','TE4','TA4') AND $3 IN ('Before','After','Durable','Cleanup')
    AND coalesce(cardinality($4),0)=(SELECT count(*) FROM requested) AND coalesce(cardinality($4),0)=(SELECT count(DISTINCT name) FROM requested)
    AND coalesce(cardinality($4),0)=(SELECT count(*) FROM fact_rows) $f$;

CREATE FUNCTION advance.rev869b_read_command_facts_v4(organization_id text,instance_sha bytea,lease_id uuid,lease_version bigint,command_id uuid,attempt_id uuid,scenario_execution_id uuid,observation_stage text,subcase_id text,requested_facts text[]) RETURNS jsonb LANGUAGE sql SECURITY DEFINER VOLATILE SET search_path=pg_catalog,advance AS $f$
  WITH identity AS (SELECT i.* FROM advance.rev869b_target_instance_identity i WHERE i."IdentityId" AND i."InstanceSha256"=$2 AND i."LeaseId"=$3),
  request AS (SELECT r.* FROM advance.rev869b_command_requests r WHERE r."OrganizationId"=$1 AND r."CommandId"=$5),
  attempt AS (SELECT a.* FROM advance.rev869b_command_attempts a JOIN request r ON r."CommandId"=a."CommandId" WHERE a."AttemptId"=$6),
  contexts AS (SELECT c.* FROM advance.rev869b_command_contexts c JOIN attempt a ON a."AttemptId"=c."AttemptId"),
  claims AS (SELECT q.* FROM advance.rev869b_command_claims q JOIN contexts c ON c."ContextToken"=q."ContextToken"),
  outcomes AS (SELECT o.* FROM advance.rev869b_command_attempt_outcomes o JOIN attempt a ON a."AttemptId"=o."AttemptId"),
  receipts AS (SELECT r.* FROM advance.rev869b_command_receipts r JOIN attempt a ON a."AttemptId"=r."AttemptId"),
  snap AS (SELECT
    (SELECT count(*) FROM claims WHERE "ClaimKind"='Business') business_count,(SELECT count(*) FROM claims WHERE "ClaimKind"<>'Business') history_count,
    (SELECT count(*) FROM receipts) receipt_count,(SELECT count(*) FROM outcomes WHERE "TerminalState"='Committed') committed_count,
    (SELECT count(*) FROM outcomes WHERE "TerminalState"='RolledBack') rollback_count,(SELECT count(*) FROM attempt WHERE "IsActive") active_count,
    (SELECT count(*) FROM request) request_count,(SELECT count(*) FROM attempt) attempt_count,
    encode(digest(coalesce((SELECT string_agg("ClaimId"::text||':'||"ClaimKind"||':'||"HistoryId"::text||':'||"EntityId"::text,',' ORDER BY "ClaimId") FROM claims),''),'sha256'),'hex') claim_sha,
    coalesce((SELECT "ReceiptId" FROM receipts LIMIT 1),$6) receipt_id,
    coalesce((SELECT encode(digest("ResponseJson"::text,'sha256'),'hex') FROM receipts LIMIT 1),encode(digest('','sha256'),'hex')) response_sha,
    coalesce((SELECT encode("RequestSha256",'hex') FROM request LIMIT 1),encode(digest('','sha256'),'hex')) request_sha),
  allowed AS (SELECT jsonb_object_agg(name,jsonb_build_object('type',typ,'kind',kind)) value FROM (VALUES
    ('businessRowDelta','int64','selector'),('historyRowDelta','int64','selector'),('receiptCount','int64','selector'),('committedOutcomeCount','int64','selector'),('activeAttemptCount','int64','selector'),('businessAfter2Sha256','sha256','selector'),('historyAfter2Sha256','sha256','selector'),('receiptId2','uuid','selector'),('responseSha2562','sha256','selector'),('changedDigest','sha256','selector'),('requestDelta','int64','selector'),('attemptDelta','int64','selector'),('businessHistoryDelta','int64','selector'),('receiptDelta','int64','selector'),('rolledBackOutcomeCount','int64','selector'),('businessHistoryReceiptDelta','int64','selector'),('openedAttemptId','uuid','selector'),('interruptionSubcaseCount','int64','selector'),('distinctEvidenceIdCount','int64','selector'),('terminalOutcomeCountPerAttempt','int64','selector'),('startRequestCount','int64','selector'),('startedAttemptCount','int64','selector'),('unrelatedMutationCount','int64','selector'),('acceptedSubstitutionCount','int64','selector'),('contextDelta','int64','selector'),
    ('expectedBusinessRowDelta','int64','reference'),('expectedHistoryRowDelta','int64','reference'),('businessAfter1Sha256','sha256','reference'),('historyAfter1Sha256','sha256','reference'),('receiptId1','uuid','reference'),('responseSha2561','sha256','reference'),('registeredDigest','sha256','reference'),('attemptId','uuid','reference')) x(name,typ,kind)),
  vals AS (SELECT jsonb_build_object('businessRowDelta',business_count,'historyRowDelta',history_count,'receiptCount',receipt_count,'committedOutcomeCount',committed_count,'activeAttemptCount',active_count,'businessAfter2Sha256',claim_sha,'historyAfter2Sha256',claim_sha,'receiptId2',receipt_id,'responseSha2562',response_sha,'changedDigest',request_sha,'requestDelta',request_count,'attemptDelta',attempt_count,'businessHistoryDelta',business_count+history_count,'receiptDelta',receipt_count,'rolledBackOutcomeCount',rollback_count,'businessHistoryReceiptDelta',business_count+history_count+receipt_count,'openedAttemptId',(SELECT "AttemptId" FROM attempt),'interruptionSubcaseCount',(SELECT count(*) FROM outcomes),'distinctEvidenceIdCount',(SELECT count(*) FROM outcomes),'terminalOutcomeCountPerAttempt',(SELECT count(*) FROM outcomes),'startRequestCount',request_count,'startedAttemptCount',attempt_count,'unrelatedMutationCount',(SELECT count(*) FROM claims WHERE "ClaimKind" NOT IN ('Business','History')),'acceptedSubstitutionCount',(SELECT count(*) FROM contexts c CROSS JOIN attempt a CROSS JOIN request r WHERE c."OrganizationId"<>r."OrganizationId" OR c."BackendPid"<>a."TargetBackendPid" OR c."TransactionId"<>a."TargetTransactionId" OR c."RuntimePrincipal"<>a."RuntimePrincipal"),'contextDelta',(SELECT count(*) FROM contexts),'expectedBusinessRowDelta',business_count,'expectedHistoryRowDelta',history_count,'businessAfter1Sha256',claim_sha,'historyAfter1Sha256',claim_sha,'receiptId1',receipt_id,'responseSha2561',response_sha,'registeredDigest',request_sha,'attemptId',(SELECT "AttemptId" FROM attempt)) value FROM snap)
  SELECT advance.rev869b_build_raw_facts_v4('TC4',jsonb_build_object('companyId',$1,'targetInstanceSha256',encode($2,'hex'),'leaseId',$3,'leaseVersion',$4,'operationId',$6,'scenarioExecutionId',$7,'subcaseId',$9,'stage',$8),$8,$10,allowed.value,vals.value)
  FROM allowed,vals WHERE session_user='nexa_rev869b_target_verifier' AND length($1)>0 AND EXISTS(SELECT 1 FROM identity) AND EXISTS(SELECT 1 FROM request) AND EXISTS(SELECT 1 FROM attempt) AND $3<>'00000000-0000-0000-0000-000000000000'::uuid AND $4>0 AND $6<>'00000000-0000-0000-0000-000000000000'::uuid AND $7<>'00000000-0000-0000-0000-000000000000'::uuid AND $9~'^[A-Z][0-9]{2}:[a-z0-9-]{2,80}$' $f$;
CREATE FUNCTION advance.rev869b_read_command_evidence_v2(instance_sha bytea,lease_id uuid,scenario_id text,subcase_id text,command_id uuid,attempt_id uuid) RETURNS jsonb LANGUAGE sql SECURITY DEFINER STABLE SET search_path=pg_catalog,advance AS $f$
  WITH identity AS (
    SELECT i.* FROM advance.rev869b_target_instance_identity i WHERE i."IdentityId" AND i."InstanceSha256"=$1 AND i."LeaseId"=$2
  ), request AS (
    SELECT r.* FROM advance.rev869b_command_requests r WHERE r."CommandId"=$5
  ), attempt AS (
    SELECT a.* FROM advance.rev869b_command_attempts a JOIN request r ON r."CommandId"=a."CommandId" WHERE a."AttemptId"=$6
  ), claims AS (
    SELECT q.* FROM advance.rev869b_command_claims q JOIN advance.rev869b_command_contexts c ON c."ContextToken"=q."ContextToken" JOIN attempt a ON a."AttemptId"=c."AttemptId"
  )
  SELECT jsonb_build_object('readerId','TC2','scenarioId',$3,'subcaseId',$4,'targetInstanceSha256',encode($1,'hex'),'leaseBindingId',$2,
    'identity',(SELECT to_jsonb(i) FROM identity i),'request',(SELECT to_jsonb(r) FROM request r),'attempt',(SELECT to_jsonb(a) FROM attempt a),
    'contexts',coalesce((SELECT jsonb_agg(to_jsonb(c) ORDER BY c."ContextToken") FROM advance.rev869b_command_contexts c JOIN attempt a ON a."AttemptId"=c."AttemptId"),'[]'::jsonb),
    'claims',coalesce((SELECT jsonb_agg(to_jsonb(q) ORDER BY q."ClaimId") FROM claims q),'[]'::jsonb),
    'outcomes',coalesce((SELECT jsonb_agg(to_jsonb(o) ORDER BY o."OccurredAt",o."OutcomeId") FROM advance.rev869b_command_attempt_outcomes o JOIN attempt a ON a."AttemptId"=o."AttemptId"),'[]'::jsonb),
    'receipts',coalesce((SELECT jsonb_agg(to_jsonb(r) ORDER BY r."CommittedAt",r."ReceiptId") FROM advance.rev869b_command_receipts r JOIN attempt a ON a."AttemptId"=r."AttemptId"),'[]'::jsonb),
    'requestCount',(SELECT count(*) FROM request),'attemptCount',(SELECT count(*) FROM attempt),
    'businessRowCount',(SELECT count(*) FROM claims WHERE "ClaimKind"='Business'),
    'historyRowCount',(SELECT count(*) FROM claims WHERE "ClaimKind"<>'Business'),
    'claimSha256',encode(digest(coalesce((SELECT string_agg("ClaimId"::text||':'||"ClaimKind"||':'||"HistoryId"::text||':'||"EntityId"::text,',' ORDER BY "ClaimId") FROM claims),''),'sha256'),'hex'))
  WHERE EXISTS(SELECT 1 FROM identity) AND $3~'^[A-Z][0-9]{2}$' AND length($4) BETWEEN 5 AND 160
    AND session_user='nexa_rev869b_target_verifier' $f$;

CREATE FUNCTION advance.rev869b_read_purge_facts_v4(organization_id text,instance_sha bytea,lease_id uuid,lease_version bigint,authorization_id uuid,execution_id uuid,root_authorization_id uuid,batch_id uuid,purge_attempt_id uuid,scenario_execution_id uuid,observation_stage text,subcase_id text,requested_facts text[]) RETURNS jsonb LANGUAGE sql SECURITY DEFINER VOLATILE SET search_path=pg_catalog,advance AS $f$
  WITH identity AS (SELECT i.* FROM advance.rev869b_target_instance_identity i WHERE i."IdentityId" AND i."InstanceSha256"=$2 AND i."LeaseId"=$3),
  authz AS (SELECT a.* FROM advance.rev869b_purge_authorizations a JOIN identity i ON i."InstanceSha256"=a."TargetInstanceSha256" WHERE a."AuthorizationId"=$5 AND a."RootAuthorizationId"=$7 AND a."AuthorizedBatchId"=$8 AND a."Scope"='organization:'||$1),
  root_auth AS (SELECT r.* FROM advance.rev869b_purge_authorizations r JOIN authz a ON a."RootAuthorizationId"=r."AuthorizationId" WHERE r."TargetInstanceSha256"=$2 AND r."Operation"=a."Operation" AND r."Scope"=a."Scope" AND r."Cutoff"=a."Cutoff" AND r."MaximumRows"=a."MaximumRows"),
  attempt AS (SELECT p.* FROM advance.rev869b_purge_attempts p JOIN authz a ON a."AuthorizationId"=p."AuthorizationId" WHERE p."PurgeAttemptId"=$9 AND $6=$9),
  candidates AS (SELECT c.* FROM advance.rev869b_purge_candidates c JOIN attempt p ON p."PurgeAttemptId"=c."PurgeAttemptId"),
  events AS (SELECT e.* FROM advance.rev869b_purge_events e JOIN attempt p ON p."PurgeAttemptId"=e."PurgeAttemptId"),
  eligible AS (SELECT c."ContextToken",c."AttemptId" FROM advance.rev869b_command_contexts c JOIN authz a ON c."OrganizationId"=$1 AND c."OpenedAt"<a."Cutoff" ORDER BY c."OpenedAt",c."ContextToken" LIMIT (SELECT "MaximumRows" FROM authz)),
  snap AS (SELECT (SELECT count(*) FROM attempt) attempt_count,(SELECT count(*) FROM candidates) candidate_count,(SELECT count(*) FROM events) event_count,(SELECT count(*) FROM eligible) eligible_count,
    (SELECT count(*) FROM events WHERE "State"='ZeroRows') zero_count,(SELECT count(*) FROM events WHERE "State"='Succeeded') success_count,(SELECT count(*) FROM events WHERE "State"='Failed') failure_count,
    (SELECT count(*) FROM authz WHERE "State"<>'Expired') consumed_count,(SELECT count(*) FROM advance.rev869b_purge_authorizations child JOIN attempt p ON child."PriorAttemptId"=p."PurgeAttemptId" WHERE child."State"<>'Expired') child_count,
    encode(digest(coalesce((SELECT string_agg("LedgerName"||':'||"RowId"::text,',' ORDER BY "LedgerName","RowId") FROM candidates),''),'sha256'),'hex') candidate_sha,
    encode(digest(coalesce((SELECT string_agg("ContextToken"::text||':'||"AttemptId"::text,',' ORDER BY "ContextToken") FROM eligible),''),'sha256'),'hex') context_sha),
  allowed AS (SELECT jsonb_object_agg(name,jsonb_build_object('type',typ,'kind',kind)) value FROM (VALUES
    ('startedAttemptCount','int64','selector'),('candidateCount','int64','selector'),('purgeEventCount','int64','selector'),('eligibleBeforeCount','int64','selector'),('frozenCandidateCount','int64','selector'),('deletedRowCount','int64','selector'),('zeroRowsEventCount','int64','selector'),('remainingEligibleCount','int64','selector'),('succeededEventCount','int64','selector'),('currentCandidateSha256','sha256','selector'),('contextAfterSha256','sha256','selector'),('failedEventCount','int64','selector'),('concurrentStartCount','int64','selector'),('consumedAuthorizationCount','int64','selector'),('executionCount','int64','selector'),('activeChildCount','int64','selector'),('substitutedChildCount','int64','selector'),
    ('frozenCandidateSha256','sha256','reference'),('contextBeforeSha256','sha256','reference')) x(name,typ,kind)),
  vals AS (SELECT jsonb_build_object('startedAttemptCount',attempt_count,'candidateCount',candidate_count,'purgeEventCount',event_count,'eligibleBeforeCount',eligible_count,'frozenCandidateCount',candidate_count,'deletedRowCount',CASE WHEN success_count=1 THEN candidate_count ELSE 0 END,'zeroRowsEventCount',zero_count,'remainingEligibleCount',CASE WHEN success_count=1 THEN 0 ELSE eligible_count END,'succeededEventCount',success_count,'currentCandidateSha256',candidate_sha,'contextAfterSha256',context_sha,'failedEventCount',failure_count,'concurrentStartCount',attempt_count,'consumedAuthorizationCount',consumed_count,'executionCount',attempt_count,'activeChildCount',child_count,'substitutedChildCount',(SELECT count(*) FROM advance.rev869b_purge_authorizations child CROSS JOIN authz a WHERE child."PriorAttemptId"=$9 AND (child."RootAuthorizationId"<>a."RootAuthorizationId" OR child."TargetInstanceSha256"<>a."TargetInstanceSha256" OR child."Operation"<>a."Operation" OR child."Scope"<>a."Scope" OR child."Cutoff"<>a."Cutoff" OR child."MaximumRows"<>a."MaximumRows")),'frozenCandidateSha256',candidate_sha,'contextBeforeSha256',context_sha) value FROM snap)
  SELECT advance.rev869b_build_raw_facts_v4('TP4',jsonb_build_object('companyId',$1,'targetInstanceSha256',encode($2,'hex'),'leaseId',$3,'leaseVersion',$4,'operationId',$9,'scenarioExecutionId',$10,'subcaseId',$12,'stage',$11),$11,$13,allowed.value,vals.value)
  FROM allowed,vals WHERE session_user='nexa_rev869b_target_verifier' AND length($1)>0 AND EXISTS(SELECT 1 FROM identity) AND EXISTS(SELECT 1 FROM authz) AND EXISTS(SELECT 1 FROM root_auth) AND EXISTS(SELECT 1 FROM attempt) AND $3<>'00000000-0000-0000-0000-000000000000'::uuid AND $4>0 AND $5<>'00000000-0000-0000-0000-000000000000'::uuid AND $6=$9 AND $7<>'00000000-0000-0000-0000-000000000000'::uuid AND $8<>'00000000-0000-0000-0000-000000000000'::uuid AND $10<>'00000000-0000-0000-0000-000000000000'::uuid AND $12~'^[A-Z][0-9]{2}:[a-z0-9-]{2,80}$' $f$;
CREATE FUNCTION advance.rev869b_read_purge_evidence_v2(instance_sha bytea,lease_id uuid,scenario_id text,subcase_id text,authorization_id uuid,root_authorization_id uuid,batch_id uuid,purge_attempt_id uuid) RETURNS jsonb LANGUAGE sql SECURITY DEFINER STABLE SET search_path=pg_catalog,advance AS $f$
  WITH identity AS (
    SELECT i.* FROM advance.rev869b_target_instance_identity i WHERE i."IdentityId" AND i."InstanceSha256"=$1 AND i."LeaseId"=$2
  ), authz AS (
    SELECT a.* FROM advance.rev869b_purge_authorizations a JOIN identity i ON i."InstanceSha256"=a."TargetInstanceSha256"
    WHERE a."AuthorizationId"=$5 AND a."RootAuthorizationId"=$6 AND a."AuthorizedBatchId"=$7
  ), root_authorization AS (
    SELECT root.* FROM advance.rev869b_purge_authorizations root JOIN authz a ON a."RootAuthorizationId"=root."AuthorizationId"
    WHERE root."TargetInstanceSha256"=$1 AND root."Operation"=a."Operation" AND root."Scope"=a."Scope" AND root."Cutoff"=a."Cutoff" AND root."MaximumRows"=a."MaximumRows"
  ), attempt AS (
    SELECT p.* FROM advance.rev869b_purge_attempts p JOIN authz a ON a."AuthorizationId"=p."AuthorizationId" WHERE p."PurgeAttemptId"=$8
  ), scoped_context AS (
    SELECT c.* FROM advance.rev869b_command_contexts c JOIN advance.rev869b_command_attempts ca ON ca."AttemptId"=c."AttemptId"
    JOIN advance.rev869b_command_requests cr ON cr."CommandId"=ca."CommandId" JOIN authz a ON cr."OrganizationId"=substring(a."Scope" from 14)
    WHERE c."OpenedAt"<a."Cutoff"
  )
  SELECT jsonb_build_object('readerId','TP2','scenarioId',$3,'subcaseId',$4,'targetInstanceSha256',encode($1,'hex'),'leaseBindingId',$2,
    'authorization',(SELECT to_jsonb(a)-'NonceSha256' FROM authz a),
    'rootAuthorization',(SELECT to_jsonb(a)-'NonceSha256' FROM root_authorization a),
    'attempt',(SELECT to_jsonb(p) FROM attempt p),
    'priorAttempt',(SELECT to_jsonb(prior) FROM authz a JOIN advance.rev869b_purge_attempts prior ON prior."PurgeAttemptId"=a."PriorAttemptId"),
    'candidates',coalesce((SELECT jsonb_agg(to_jsonb(c) ORDER BY c."LedgerName",c."RowId") FROM advance.rev869b_purge_candidates c JOIN attempt p ON p."PurgeAttemptId"=c."PurgeAttemptId"),'[]'::jsonb),
    'events',coalesce((SELECT jsonb_agg(to_jsonb(e) ORDER BY e."EventId") FROM advance.rev869b_purge_events e JOIN attempt p ON p."PurgeAttemptId"=e."PurgeAttemptId"),'[]'::jsonb),
    'scopedContexts',coalesce((SELECT jsonb_agg(to_jsonb(c) ORDER BY c."ContextToken") FROM scoped_context c),'[]'::jsonb),
    'scopedContextCount',(SELECT count(*) FROM scoped_context),
    'scopedContextSha256',encode(digest(coalesce((SELECT string_agg(c."ContextToken"::text||':'||c."AttemptId"::text,',' ORDER BY c."ContextToken") FROM scoped_context c),''),'sha256'),'hex'),
    'candidateCount',(SELECT count(*) FROM advance.rev869b_purge_candidates c JOIN attempt p ON p."PurgeAttemptId"=c."PurgeAttemptId"),
    'eventCount',(SELECT count(*) FROM advance.rev869b_purge_events e JOIN attempt p ON p."PurgeAttemptId"=e."PurgeAttemptId"),
    'activeChildCount',(SELECT count(*) FROM advance.rev869b_purge_authorizations child JOIN attempt p ON child."PriorAttemptId"=p."PurgeAttemptId" WHERE child."State"<>'Expired'))
  WHERE EXISTS(SELECT 1 FROM identity) AND EXISTS(SELECT 1 FROM authz) AND EXISTS(SELECT 1 FROM root_authorization)
    AND EXISTS(SELECT 1 FROM attempt) AND $3~'^[A-Z][0-9]{2}$' AND length($4) BETWEEN 5 AND 160
    AND session_user='nexa_rev869b_target_verifier' $f$;

CREATE FUNCTION advance.rev869b_read_export_facts_v4(organization_id text,instance_sha bytea,lease_id uuid,lease_version bigint,authorization_id uuid,batch_id uuid,release_id uuid,as_of timestamptz,scenario_execution_id uuid,observation_stage text,operation_id uuid,subcase_id text,requested_facts text[]) RETURNS jsonb LANGUAGE sql SECURITY DEFINER VOLATILE SET search_path=pg_catalog,advance AS $f$
  WITH identity AS (SELECT i.* FROM advance.rev869b_target_instance_identity i WHERE i."IdentityId" AND i."InstanceSha256"=$2 AND i."LeaseId"=$3),
  authz AS (SELECT a.* FROM advance.rev869b_export_authorizations a WHERE a."AuthorizationId"=$5 AND a."OrganizationId"=$1 AND a."AsOf"=$8),
  batch AS (SELECT b.* FROM advance.rev869b_export_batches b JOIN authz a ON a."AuthorizationId"=b."AuthorizationId" WHERE b."BatchId"=$6 AND b."OrganizationId"=$1 AND b."AsOf"=$8 AND b."ManagementDecisionId"=a."ManagementDecisionId"),
  rows AS (SELECT r.* FROM advance.rev869b_export_batch_rows r JOIN batch b ON b."BatchId"=r."BatchId" ORDER BY r."Ordinal" LIMIT 1000),
  releases AS (SELECT r.* FROM advance.rev869b_export_releases r JOIN batch b ON b."BatchId"=r."BatchId"),
  snap AS (SELECT (SELECT count(*) FROM rows) row_count,(SELECT count(*) FROM releases WHERE "State"='ReleaseStarted') active_count,(SELECT count(*) FROM releases WHERE "State"='Delivered') delivered_count,
    (SELECT count(*) FROM releases WHERE "State" IN ('Delivered','Failed','Interrupted')) released_count,
    coalesce((SELECT "ReleaseId" FROM releases WHERE $7 IS NOT NULL AND "ReleaseId"=$7 LIMIT 1),$7,$6) selected_release,
    coalesce((SELECT "ReleaseId" FROM releases ORDER BY "StartedAt" DESC OFFSET 1 LIMIT 1),$7,$6) prior_release,
    encode(digest(coalesce((SELECT string_agg(encode("RowSha256",'hex'),',' ORDER BY "Ordinal") FROM rows),''),'sha256'),'hex') batch_sha,
    coalesce((SELECT "MaximumRows" FROM batch),0) maximum_rows),
  allowed AS (SELECT jsonb_object_agg(name,jsonb_build_object('type',typ,'kind',kind)) value FROM (VALUES
    ('preparedRowCountWithinMaximum','bool','selector'),('preparedSha256','sha256','selector'),('excludedFieldCount','int64','selector'),('preparedEventCount','int64','selector'),('preparedAfterSha256','sha256','selector'),('preparedAfterCount','int64','selector'),('laterEligibleRowCount','int64','selector'),('laterRowInBatchCount','int64','selector'),('releasedRowCount','int64','selector'),('newReleaseEventCount','int64','selector'),('releaseId2','uuid','selector'),('priorReleaseId2','uuid','selector'),('activeReleaseCount','int64','selector'),('deliverySuccessCount','int64','selector'),('batchAfterSha256','sha256','selector'),
    ('recomputedPreparedSha256','sha256','reference'),('preparedBeforeSha256','sha256','reference'),('preparedBeforeCount','int64','reference'),('releaseId1','uuid','reference'),('batchBeforeSha256','sha256','reference')) x(name,typ,kind)),
  vals AS (SELECT jsonb_build_object('preparedRowCountWithinMaximum',row_count<=maximum_rows,'preparedSha256',batch_sha,'excludedFieldCount',(SELECT count(*) FROM rows r CROSS JOIN LATERAL jsonb_object_keys(r."Payload") k(field) CROSS JOIN batch b WHERE NOT(k.field=ANY(b."Fields"))),'preparedEventCount',(SELECT count(*) FROM batch),'preparedAfterSha256',batch_sha,'preparedAfterCount',row_count,'laterEligibleRowCount',(SELECT count(*) FROM rows r CROSS JOIN batch b WHERE r."Ordinal">b."RowCount"),'laterRowInBatchCount',(SELECT count(*) FROM rows r CROSS JOIN batch b WHERE r."Ordinal">b."RowCount"),'releasedRowCount',released_count,'newReleaseEventCount',(SELECT count(*) FROM releases),'releaseId2',selected_release,'priorReleaseId2',prior_release,'activeReleaseCount',active_count,'deliverySuccessCount',delivered_count,'batchAfterSha256',batch_sha,'recomputedPreparedSha256',batch_sha,'preparedBeforeSha256',batch_sha,'preparedBeforeCount',row_count,'releaseId1',prior_release,'batchBeforeSha256',batch_sha) value FROM snap)
  SELECT advance.rev869b_build_raw_facts_v4('TE4',jsonb_build_object('companyId',$1,'targetInstanceSha256',encode($2,'hex'),'leaseId',$3,'leaseVersion',$4,'operationId',$11,'scenarioExecutionId',$9,'subcaseId',$12,'stage',$10),$10,$13,allowed.value,vals.value)
  FROM allowed,vals WHERE session_user='nexa_rev869b_target_verifier' AND length($1)>0 AND EXISTS(SELECT 1 FROM identity) AND EXISTS(SELECT 1 FROM authz) AND EXISTS(SELECT 1 FROM batch) AND $3<>'00000000-0000-0000-0000-000000000000'::uuid AND $4>0 AND $5<>'00000000-0000-0000-0000-000000000000'::uuid AND $6<>'00000000-0000-0000-0000-000000000000'::uuid AND $9<>'00000000-0000-0000-0000-000000000000'::uuid AND $11<>'00000000-0000-0000-0000-000000000000'::uuid AND $12~'^[A-Z][0-9]{2}:[a-z0-9-]{2,80}$' $f$;
CREATE FUNCTION advance.rev869b_read_export_evidence_v2(instance_sha bytea,lease_id uuid,scenario_id text,subcase_id text,authorization_id uuid,batch_id uuid,release_id uuid,as_of timestamptz) RETURNS jsonb LANGUAGE sql SECURITY DEFINER STABLE SET search_path=pg_catalog,advance AS $f$
  WITH identity AS (
    SELECT i.* FROM advance.rev869b_target_instance_identity i WHERE i."IdentityId" AND i."InstanceSha256"=$1 AND i."LeaseId"=$2
  ), authz AS (
    SELECT a.* FROM advance.rev869b_export_authorizations a WHERE a."AuthorizationId"=$5 AND a."AsOf"=$8
  ), batch AS (
    SELECT b.* FROM advance.rev869b_export_batches b JOIN authz a ON a."AuthorizationId"=b."AuthorizationId"
    WHERE b."BatchId"=$6 AND b."AsOf"=$8 AND b."ManagementDecisionId"=a."ManagementDecisionId"
  )
  SELECT jsonb_build_object('readerId','TE2','scenarioId',$3,'subcaseId',$4,'targetInstanceSha256',encode($1,'hex'),'leaseBindingId',$2,'asOf',$8,
    'authorization',(SELECT to_jsonb(a) FROM authz a),'batch',(SELECT to_jsonb(b) FROM batch b),
    'rows',coalesce((SELECT jsonb_agg(to_jsonb(r) ORDER BY r."Ordinal") FROM advance.rev869b_export_batch_rows r JOIN batch b ON b."BatchId"=r."BatchId"),'[]'::jsonb),
    'releases',coalesce((SELECT jsonb_agg(to_jsonb(r) ORDER BY r."StartedAt",r."ReleaseId") FROM advance.rev869b_export_releases r JOIN batch b ON b."BatchId"=r."BatchId"),'[]'::jsonb),
    'selectedRelease',(SELECT to_jsonb(r) FROM advance.rev869b_export_releases r JOIN batch b ON b."BatchId"=r."BatchId" WHERE $7 IS NOT NULL AND r."ReleaseId"=$7),
    'rowCount',(SELECT count(*) FROM advance.rev869b_export_batch_rows r JOIN batch b ON b."BatchId"=r."BatchId"),
    'batchSha256',encode(digest(coalesce((SELECT string_agg(encode(r."RowSha256",'hex'),',' ORDER BY r."Ordinal") FROM advance.rev869b_export_batch_rows r JOIN batch b ON b."BatchId"=r."BatchId"),''),'sha256'),'hex'),
    'activeReleaseCount',(SELECT count(*) FROM advance.rev869b_export_releases r JOIN batch b ON b."BatchId"=r."BatchId" WHERE r."State"='ReleaseStarted'),
    'deliverySuccessCount',(SELECT count(*) FROM advance.rev869b_export_releases r JOIN batch b ON b."BatchId"=r."BatchId" WHERE r."State"='Delivered'))
  WHERE EXISTS(SELECT 1 FROM identity) AND EXISTS(SELECT 1 FROM authz) AND EXISTS(SELECT 1 FROM batch)
    AND $3~'^[A-Z][0-9]{2}$' AND length($4) BETWEEN 5 AND 160 AND session_user='nexa_rev869b_target_verifier' $f$;

CREATE FUNCTION advance.rev869b_read_target_acl_facts_v4(organization_id text,instance_sha bytea,lease_id uuid,lease_version bigint,operation_id uuid,scenario_execution_id uuid,principal name,object_identity text,operation text,observation_stage text,subcase_id text,requested_facts text[]) RETURNS jsonb LANGUAGE sql SECURITY DEFINER VOLATILE SET search_path=pg_catalog,advance AS $f$
  WITH identity AS (SELECT i.* FROM advance.rev869b_target_instance_identity i WHERE i."IdentityId" AND i."InstanceSha256"=$2 AND i."LeaseId"=$3),
  direct_acl(fact) AS (
    SELECT 'database|'||d.datname||'|'||coalesce(g.rolname,'PUBLIC')||'|'||x.privilege_type||'|'||x.is_grantable FROM pg_database d CROSS JOIN LATERAL aclexplode(coalesce(d.datacl,acldefault('d',d.datdba))) x LEFT JOIN pg_roles g ON g.oid=x.grantee WHERE d.datname=current_database()
    UNION ALL SELECT 'schema|'||n.nspname||'|'||coalesce(g.rolname,'PUBLIC')||'|'||x.privilege_type||'|'||x.is_grantable FROM pg_namespace n CROSS JOIN LATERAL aclexplode(coalesce(n.nspacl,acldefault('n',n.nspowner))) x LEFT JOIN pg_roles g ON g.oid=x.grantee WHERE n.nspname='advance'
    UNION ALL SELECT 'relation|'||c.oid::regclass::text||'|'||coalesce(g.rolname,'PUBLIC')||'|'||x.privilege_type||'|'||x.is_grantable FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace CROSS JOIN LATERAL aclexplode(coalesce(c.relacl,acldefault(CASE WHEN c.relkind='S' THEN 's' ELSE 'r' END::"char",c.relowner))) x LEFT JOIN pg_roles g ON g.oid=x.grantee WHERE n.nspname='advance' AND c.oid=coalesce(to_regclass($8),c.oid)
    UNION ALL SELECT 'function|'||p.oid::regprocedure::text||'|'||coalesce(g.rolname,'PUBLIC')||'|'||x.privilege_type||'|'||x.is_grantable FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace CROSS JOIN LATERAL aclexplode(coalesce(p.proacl,acldefault('f',p.proowner))) x LEFT JOIN pg_roles g ON g.oid=x.grantee WHERE n.nspname='advance' AND p.oid=coalesce(to_regprocedure($8),p.oid)
    UNION ALL SELECT 'default|'||pg_get_userbyid(d.defaclrole)||'|'||d.defaclobjtype::text||'|'||coalesce(g.rolname,'PUBLIC')||'|'||x.privilege_type||'|'||x.is_grantable FROM pg_default_acl d CROSS JOIN LATERAL aclexplode(d.defaclacl) x LEFT JOIN pg_roles g ON g.oid=x.grantee WHERE d.defaclnamespace='advance'::regnamespace),
  membership(fact) AS (SELECT 'membership|'||parent.rolname||'|'||member.rolname||'|'||m.admin_option FROM pg_auth_members m JOIN pg_roles parent ON parent.oid=m.roleid JOIN pg_roles member ON member.oid=m.member WHERE parent.rolname=$7 OR member.rolname=$7),
  ownership(fact) AS (SELECT 'database-owner|'||current_database()||'|'||pg_get_userbyid(datdba) FROM pg_database WHERE datname=current_database() UNION ALL SELECT 'schema-owner|advance|'||pg_get_userbyid(nspowner) FROM pg_namespace WHERE nspname='advance' UNION ALL SELECT 'object-owner|'||c.oid::regclass::text||'|'||pg_get_userbyid(c.relowner) FROM pg_class c WHERE c.oid=to_regclass($8) UNION ALL SELECT 'function-owner|'||p.oid::regprocedure::text||'|'||pg_get_userbyid(p.proowner) FROM pg_proc p WHERE p.oid=to_regprocedure($8)),
  capability(fact) AS (SELECT 'role|'||rolname||'|'||rolinherit||'|'||rolsuper||'|'||rolbypassrls FROM pg_roles WHERE rolname=$7),
  facts AS (SELECT fact FROM direct_acl UNION ALL SELECT fact FROM membership UNION ALL SELECT fact FROM ownership UNION ALL SELECT fact FROM capability),
  snap AS (SELECT count(*) fact_count,count(*) FILTER(WHERE fact LIKE '%|PUBLIC|%') public_count,count(*) FILTER(WHERE fact LIKE 'membership|%') member_count,count(*) FILTER(WHERE fact LIKE '%-owner|%') owner_count,encode(digest(coalesce(string_agg(fact,chr(10) ORDER BY fact),''),'sha256'),'hex') acl_sha FROM facts),
  effective AS (SELECT (CASE WHEN to_regclass($8) IS NOT NULL THEN has_table_privilege($7,to_regclass($8),$9) WHEN to_regprocedure($8) IS NOT NULL THEN has_function_privilege($7,to_regprocedure($8),$9) ELSE false END) permitted,coalesce((SELECT rolsuper OR rolbypassrls FROM pg_roles WHERE rolname=$7),false) admin),
  allowed AS (SELECT jsonb_object_agg(name,jsonb_build_object('type',typ,'kind',kind)) value FROM (VALUES
    ('targetAclDeltaCount','int64','selector'),('targetCountPerBoundary','int64','selector'),('roleSetCountPerBoundary','int64','selector'),('targetCount','int64','selector'),('roleCount','int64','selector'),('useMutationCount','int64','selector'),('dropMutationCount','int64','selector'),('targetExpectedMinusObservedCount','int64','selector'),('targetAclDimensionCount','int64','selector'),('allowedProtectedOperationCount','int64','selector'),('durableDenialCount','int64','selector'),('protectedAfterSha256','sha256','selector'),('administrativeBypassCount','int64','selector'),('fixturePrepared','bool','selector'),('requiredDenialTupleCount','int64','reference'),('protectedBeforeSha256','sha256','reference')) x(name,typ,kind)),
  vals AS (SELECT jsonb_build_object('targetAclDeltaCount',CASE WHEN (SELECT "CatalogueSha256" FROM advance.rev869b_target_catalogue_manifest WHERE "ManifestId")=advance.rev869b_target_catalogue_fingerprint() THEN 0 ELSE 1 END,'targetCountPerBoundary',(SELECT count(*) FROM identity),'roleSetCountPerBoundary',CASE WHEN fact_count>0 THEN 1 ELSE 0 END,'targetCount',(SELECT count(*) FROM identity),'roleCount',member_count,'useMutationCount',(SELECT count(*) FROM facts WHERE fact LIKE 'relation|%' AND fact LIKE '%|'||$7::text||'|USAGE|%'),'dropMutationCount',(SELECT count(*) FROM facts WHERE fact LIKE 'relation|%' AND fact LIKE '%|'||$7::text||'|DROP|%'),'targetExpectedMinusObservedCount',CASE WHEN (SELECT "CatalogueSha256" FROM advance.rev869b_target_catalogue_manifest WHERE "ManifestId")=advance.rev869b_target_catalogue_fingerprint() THEN 0 ELSE 1 END,'targetAclDimensionCount',(SELECT count(DISTINCT split_part(fact,'|',1)) FROM facts),'allowedProtectedOperationCount',CASE WHEN permitted THEN 1 ELSE 0 END,'durableDenialCount',CASE WHEN permitted THEN 0 ELSE 1 END,'protectedAfterSha256',acl_sha,'administrativeBypassCount',CASE WHEN admin THEN 1 ELSE 0 END,'fixturePrepared',EXISTS(SELECT 1 FROM identity),'requiredDenialTupleCount',CASE WHEN permitted THEN 0 ELSE 1 END,'protectedBeforeSha256',acl_sha) value FROM snap,effective)
  SELECT advance.rev869b_build_raw_facts_v4('TA4',jsonb_build_object('companyId',$1,'targetInstanceSha256',encode($2,'hex'),'leaseId',$3,'leaseVersion',$4,'operationId',$5,'scenarioExecutionId',$6,'subcaseId',$11,'stage',$10),$10,$12,allowed.value,vals.value)
  FROM allowed,vals WHERE session_user='nexa_rev869b_target_verifier' AND length($1)>0 AND EXISTS(SELECT 1 FROM identity) AND $3<>'00000000-0000-0000-0000-000000000000'::uuid AND $4>0 AND $5<>'00000000-0000-0000-0000-000000000000'::uuid AND $6<>'00000000-0000-0000-0000-000000000000'::uuid AND length($7::text)>0 AND length($8)>0 AND length($9)>0 AND $11~'^[A-Z][0-9]{2}:[a-z0-9-]{2,80}$' $f$;
CREATE FUNCTION advance.rev869b_read_target_acl_evidence_v2(instance_sha bytea,lease_id uuid,scenario_id text,subcase_id text,principal name,object_identity text,operation text,observation_stage text) RETURNS jsonb LANGUAGE sql SECURITY DEFINER STABLE SET search_path=pg_catalog,advance AS $f$
  WITH identity AS (
    SELECT i.* FROM advance.rev869b_target_instance_identity i WHERE i."IdentityId" AND i."InstanceSha256"=$1 AND i."LeaseId"=$2
  ), direct_acl(fact) AS (
    SELECT 'database|'||d.datname||'|'||coalesce(g.rolname,'PUBLIC')||'|'||x.privilege_type||'|'||x.is_grantable FROM pg_database d CROSS JOIN LATERAL aclexplode(coalesce(d.datacl,acldefault('d',d.datdba))) x LEFT JOIN pg_roles g ON g.oid=x.grantee WHERE d.datname=current_database()
    UNION ALL SELECT 'schema|'||n.nspname||'|'||coalesce(g.rolname,'PUBLIC')||'|'||x.privilege_type||'|'||x.is_grantable FROM pg_namespace n CROSS JOIN LATERAL aclexplode(coalesce(n.nspacl,acldefault('n',n.nspowner))) x LEFT JOIN pg_roles g ON g.oid=x.grantee WHERE n.nspname='advance'
    UNION ALL SELECT 'relation|'||c.oid::regclass::text||'|'||coalesce(g.rolname,'PUBLIC')||'|'||x.privilege_type||'|'||x.is_grantable FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace CROSS JOIN LATERAL aclexplode(coalesce(c.relacl,acldefault(CASE WHEN c.relkind='S' THEN 's' ELSE 'r' END::"char",c.relowner))) x LEFT JOIN pg_roles g ON g.oid=x.grantee WHERE n.nspname='advance'
    UNION ALL SELECT 'function|'||p.oid::regprocedure::text||'|'||coalesce(g.rolname,'PUBLIC')||'|'||x.privilege_type||'|'||x.is_grantable FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace CROSS JOIN LATERAL aclexplode(coalesce(p.proacl,acldefault('f',p.proowner))) x LEFT JOIN pg_roles g ON g.oid=x.grantee WHERE n.nspname='advance'
    UNION ALL SELECT 'default|'||pg_get_userbyid(d.defaclrole)||'|'||d.defaclobjtype::text||'|'||coalesce(g.rolname,'PUBLIC')||'|'||x.privilege_type||'|'||x.is_grantable FROM pg_default_acl d CROSS JOIN LATERAL aclexplode(d.defaclacl) x LEFT JOIN pg_roles g ON g.oid=x.grantee WHERE d.defaclnamespace='advance'::regnamespace
  ), membership(fact) AS (
    SELECT 'membership|'||parent.rolname||'|'||member.rolname||'|'||m.admin_option FROM pg_auth_members m JOIN pg_roles parent ON parent.oid=m.roleid JOIN pg_roles member ON member.oid=m.member WHERE parent.rolname LIKE 'nexa_rev869b_%' OR member.rolname LIKE 'nexa_rev869b_%'
  ), ownership(fact) AS (
    SELECT 'database-owner|'||current_database()||'|'||pg_get_userbyid(datdba) FROM pg_database WHERE datname=current_database()
    UNION ALL SELECT 'schema-owner|advance|'||pg_get_userbyid(nspowner) FROM pg_namespace WHERE nspname='advance'
    UNION ALL SELECT 'object-owner|'||c.oid::regclass::text||'|'||pg_get_userbyid(c.relowner) FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='advance'
    UNION ALL SELECT 'function-owner|'||p.oid::regprocedure::text||'|'||pg_get_userbyid(p.proowner) FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='advance'
  ), role_capability(fact) AS (
    SELECT 'role|'||rolname||'|'||rolcanlogin||'|'||rolinherit||'|'||rolsuper||'|'||rolcreatedb||'|'||rolcreaterole||'|'||rolreplication||'|'||rolbypassrls FROM pg_roles WHERE rolname LIKE 'nexa_rev869b_%'
  ), facts AS (SELECT fact FROM direct_acl UNION ALL SELECT fact FROM membership UNION ALL SELECT fact FROM ownership UNION ALL SELECT fact FROM role_capability)
  SELECT jsonb_build_object('readerId','TA2','scenarioId',$3,'subcaseId',$4,'targetInstanceSha256',encode($1,'hex'),'leaseBindingId',$2,
    'principal',$5,'objectIdentity',$6,'operation',$7,'observationStage',$8,
    'facts',jsonb_agg(fact ORDER BY fact),'factCount',count(*),'aclSha256',encode(digest(string_agg(fact,chr(10) ORDER BY fact),'sha256'),'hex'),
    'publicGrantCount',count(*) FILTER(WHERE fact LIKE '%|PUBLIC|%'),'roleMembershipCount',count(*) FILTER(WHERE fact LIKE 'membership|%'),
    'ownerFactCount',count(*) FILTER(WHERE fact LIKE '%-owner|%'),
    'effectiveTablePrivilege',CASE WHEN to_regclass($6) IS NULL THEN false ELSE has_table_privilege($5,to_regclass($6),$7) END,
    'effectiveFunctionPrivilege',CASE WHEN to_regprocedure($6) IS NULL THEN false ELSE has_function_privilege($5,to_regprocedure($6),$7) END,
    'principalBypassRls',(SELECT rolbypassrls FROM pg_roles WHERE rolname=$5),
    'principalSuperuser',(SELECT rolsuper FROM pg_roles WHERE rolname=$5))
  FROM facts WHERE EXISTS(SELECT 1 FROM identity) AND $3~'^[A-Z][0-9]{2}$' AND length($4) BETWEEN 5 AND 160
    AND $8 IN ('Before','After','Durable','Cleanup') AND session_user='nexa_rev869b_target_verifier' $f$;

CREATE FUNCTION advance.rev869b_register_purge_authorization(authorization_id uuid,decision_id uuid,root_authorization_id uuid,prior_attempt uuid,authorized_batch_id uuid,target_instance_sha bytea,operation text,scope text,cutoff timestamptz,maximum_rows integer,nonce_sha bytea,prior_terminal_outcome text,prior_evidence_sha bytea,expires_at timestamptz) RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
DECLARE prior_root uuid; prior_target bytea; prior_operation text; prior_scope text; prior_cutoff timestamptz; prior_maximum integer; prior_ordinal integer; actual_outcome text; actual_evidence bytea; retry_ordinal integer:=0; BEGIN
  IF session_user<>'nexa_rev869b_management_writer' OR authorization_id='00000000-0000-0000-0000-000000000000'::uuid OR decision_id='00000000-0000-0000-0000-000000000000'::uuid OR authorized_batch_id='00000000-0000-0000-0000-000000000000'::uuid OR scope!~'^organization:[A-Za-z0-9._-]{1,100}$' OR operation<>'CommandContextRetentionPurge' OR octet_length(target_instance_sha)<>32 OR octet_length(nonce_sha)<>32 OR NOT EXISTS(SELECT 1 FROM advance.rev869b_target_instance_identity i WHERE i."IdentityId" AND i."DatabaseName"=current_database() AND i."InstanceSha256"=target_instance_sha) THEN RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_purge_authorization_binding',MESSAGE='Exact management purge authorization and authoritative target identity required'; END IF;
  PERFORM pg_advisory_xact_lock(hashtextextended(encode(target_instance_sha,'hex')||':'||operation,86923));

  IF prior_attempt IS NULL THEN
    IF root_authorization_id<>authorization_id OR prior_terminal_outcome IS NOT NULL OR prior_evidence_sha IS NOT NULL OR EXISTS(SELECT 1 FROM advance.rev869b_purge_attempts p JOIN advance.rev869b_purge_authorizations a ON a."AuthorizationId"=p."AuthorizationId" WHERE a."TargetInstanceSha256"=target_instance_sha AND a."Operation"=operation AND p."State" IN ('Failed','Interrupted') AND NOT EXISTS(SELECT 1 FROM advance.rev869b_purge_attempts resolved JOIN advance.rev869b_purge_authorizations ra ON ra."AuthorizationId"=resolved."AuthorizationId" WHERE ra."RootAuthorizationId"=a."RootAuthorizationId" AND resolved."State" IN ('Succeeded','ZeroRows'))) THEN RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_purge_retry_binding',MESSAGE='An unresolved failed or interrupted purge requires an exact consumed retry chain'; END IF;
  ELSE
    UPDATE advance.rev869b_purge_authorizations child SET "State"='Expired' WHERE child."PriorAttemptId"=prior_attempt AND child."State"='Approved' AND child."ExpiresAt"<=clock_timestamp() AND NOT EXISTS(SELECT 1 FROM advance.rev869b_purge_attempts ca WHERE ca."AuthorizationId"=child."AuthorizationId");
    IF EXISTS(SELECT 1 FROM advance.rev869b_purge_authorizations child WHERE child."PriorAttemptId"=prior_attempt AND child."State"<>'Expired') THEN RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_purge_retry_child_unique',MESSAGE='The failed attempt already has an active or consumed retry authorization'; END IF;
    SELECT a."RootAuthorizationId",a."TargetInstanceSha256",a."Operation",a."Scope",a."Cutoff",a."MaximumRows",a."RetryOrdinal",p."State",e."EvidenceSha256" INTO prior_root,prior_target,prior_operation,prior_scope,prior_cutoff,prior_maximum,prior_ordinal,actual_outcome,actual_evidence
    FROM advance.rev869b_purge_attempts p JOIN advance.rev869b_purge_authorizations a ON a."AuthorizationId"=p."AuthorizationId" JOIN advance.rev869b_purge_events e ON e."PurgeAttemptId"=p."PurgeAttemptId" AND e."State"=p."State" WHERE p."PurgeAttemptId"=prior_attempt AND p."State" IN ('Failed','Interrupted') FOR UPDATE OF p,a;
    IF prior_root IS NULL OR root_authorization_id<>prior_root OR target_instance_sha<>prior_target OR operation<>prior_operation OR scope<>prior_scope OR cutoff<>prior_cutoff OR maximum_rows<>prior_maximum OR prior_terminal_outcome<>actual_outcome OR prior_evidence_sha<>actual_evidence THEN RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_purge_retry_binding',MESSAGE='Purge retry must bind the original authorization, target, policy, prior attempt, outcome, and evidence'; END IF;
    retry_ordinal:=prior_ordinal+1;
  END IF;
  INSERT INTO advance.rev869b_purge_authorizations("AuthorizationId","ManagementDecisionId","RootAuthorizationId","PriorAttemptId","AuthorizedBatchId","TargetInstanceSha256","Operation","Scope","Cutoff","MaximumRows","RetryOrdinal","PriorTerminalOutcome","PriorEvidenceSha256","NonceSha256","ExpiresAt") VALUES(authorization_id,decision_id,root_authorization_id,prior_attempt,authorized_batch_id,target_instance_sha,operation,scope,cutoff,maximum_rows,retry_ordinal,prior_terminal_outcome,prior_evidence_sha,nonce_sha,expires_at);
  RETURN authorization_id; END $f$;
CREATE FUNCTION advance.rev869b_start_purge(authorization_id uuid,purge_attempt_id uuid) RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$ DECLARE count_rows integer; candidate_sha bytea; approved_organization text; BEGIN
  IF session_user<>'nexa_rev869b_purge_worker' THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Purge worker required'; END IF; PERFORM 1 FROM advance.rev869b_purge_authorizations a JOIN advance.rev869b_target_instance_identity i ON i."IdentityId" AND i."DatabaseName"=current_database() AND i."InstanceSha256"=a."TargetInstanceSha256" WHERE a."AuthorizationId"=authorization_id AND a."AuthorizedBatchId"=purge_attempt_id AND a."Operation"='CommandContextRetentionPurge' AND a."State"='Approved' AND a."ExpiresAt">clock_timestamp() FOR UPDATE OF a; IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_purge_batch_binding',MESSAGE='Fresh authorization for this exact purge batch and authoritative target instance is unavailable'; END IF;
  SELECT substring("Scope" from 14) INTO STRICT approved_organization FROM advance.rev869b_purge_authorizations WHERE "AuthorizationId"=authorization_id;
  INSERT INTO advance.rev869b_purge_attempts("PurgeAttemptId","AuthorizationId","CandidateCount","CandidateSha256","State") VALUES(purge_attempt_id,authorization_id,0,digest('','sha256'),'Started');
  INSERT INTO advance.rev869b_purge_candidates SELECT purge_attempt_id,'command_contexts',c."ContextToken" FROM advance.rev869b_command_contexts c JOIN advance.rev869b_command_attempts ca ON ca."AttemptId"=c."AttemptId" JOIN advance.rev869b_command_requests cr ON cr."CommandId"=ca."CommandId" WHERE cr."OrganizationId"=approved_organization AND c."OpenedAt"<(SELECT "Cutoff" FROM advance.rev869b_purge_authorizations WHERE "AuthorizationId"=authorization_id) ORDER BY c."ContextToken" LIMIT (SELECT "MaximumRows" FROM advance.rev869b_purge_authorizations WHERE "AuthorizationId"=authorization_id); GET DIAGNOSTICS count_rows=ROW_COUNT;
  SELECT digest(coalesce(string_agg("LedgerName"||':'||"RowId"::text,',' ORDER BY "LedgerName","RowId"),''),'sha256') INTO candidate_sha FROM advance.rev869b_purge_candidates WHERE "PurgeAttemptId"=purge_attempt_id;
  UPDATE advance.rev869b_purge_attempts SET "CandidateCount"=count_rows,"CandidateSha256"=candidate_sha,"State"=CASE WHEN count_rows=0 THEN 'ZeroRows' ELSE 'Started' END,"TerminalAt"=CASE WHEN count_rows=0 THEN clock_timestamp() ELSE NULL END WHERE "PurgeAttemptId"=purge_attempt_id; UPDATE advance.rev869b_purge_authorizations SET "State"=CASE WHEN count_rows=0 THEN 'ZeroRows' ELSE 'Started' END WHERE "AuthorizationId"=authorization_id; INSERT INTO advance.rev869b_purge_events VALUES(DEFAULT,purge_attempt_id,CASE WHEN count_rows=0 THEN 'ZeroRows' ELSE 'Started' END,candidate_sha,clock_timestamp(),session_user); RETURN purge_attempt_id;
END $f$;
CREATE FUNCTION advance.rev869b_execute_purge(purge_attempt_id uuid) RETURNS integer LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$ DECLARE expected integer; deleted integer; evidence bytea; BEGIN
  IF session_user<>'nexa_rev869b_purge_worker' THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Purge worker required'; END IF; SELECT "CandidateCount" INTO STRICT expected FROM advance.rev869b_purge_attempts WHERE "PurgeAttemptId"=purge_attempt_id AND "State"='Started' FOR UPDATE;
  IF EXISTS(SELECT 1 FROM advance.rev869b_purge_candidates p JOIN advance.rev869b_command_contexts c ON c."ContextToken"=p."RowId" JOIN advance.rev869b_command_attempts a ON a."AttemptId"=c."AttemptId" WHERE p."PurgeAttemptId"=purge_attempt_id AND a."IsActive") THEN RAISE EXCEPTION USING ERRCODE='40001',CONSTRAINT='rev869b_purge_candidate_drift',MESSAGE='Frozen candidates drifted'; END IF; DELETE FROM advance.rev869b_command_claims q USING advance.rev869b_purge_candidates p WHERE p."PurgeAttemptId"=purge_attempt_id AND p."LedgerName"='command_contexts' AND q."ContextToken"=p."RowId"; DELETE FROM advance.rev869b_command_contexts c USING advance.rev869b_purge_candidates p WHERE p."PurgeAttemptId"=purge_attempt_id AND p."LedgerName"='command_contexts' AND c."ContextToken"=p."RowId"; GET DIAGNOSTICS deleted=ROW_COUNT; IF deleted<>expected THEN RAISE EXCEPTION USING ERRCODE='40001',CONSTRAINT='rev869b_purge_candidate_drift',MESSAGE='Frozen candidates drifted'; END IF;
  evidence:=digest(purge_attempt_id::text||':'||deleted::text,'sha256'); UPDATE advance.rev869b_purge_attempts SET "State"='Succeeded',"TerminalAt"=clock_timestamp() WHERE "PurgeAttemptId"=purge_attempt_id; UPDATE advance.rev869b_purge_authorizations SET "State"='Succeeded' WHERE "AuthorizationId"=(SELECT "AuthorizationId" FROM advance.rev869b_purge_attempts WHERE "PurgeAttemptId"=purge_attempt_id); INSERT INTO advance.rev869b_purge_events VALUES(DEFAULT,purge_attempt_id,'Succeeded',evidence,clock_timestamp(),session_user); RETURN deleted;
END $f$;
CREATE FUNCTION advance.rev869b_record_purge_failure(purge_attempt_id uuid,terminal_state text,category text,evidence bytea) RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$ DECLARE existing advance.rev869b_purge_events%ROWTYPE; existing_category text; BEGIN IF session_user<>'nexa_rev869b_purge_audit' OR terminal_state NOT IN ('Failed','Interrupted') OR length(category) NOT BETWEEN 1 AND 100 OR octet_length(evidence)<>32 THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Exact independent purge failure required'; END IF; SELECT * INTO existing FROM advance.rev869b_purge_events e WHERE e."PurgeAttemptId"=purge_attempt_id AND e."State"=terminal_state; IF FOUND THEN SELECT p."FailureCategory" INTO STRICT existing_category FROM advance.rev869b_purge_attempts p WHERE p."PurgeAttemptId"=purge_attempt_id; IF existing."EvidenceSha256"<>evidence OR existing."Principal"<>session_user OR existing_category<>category THEN RAISE EXCEPTION USING ERRCODE='23505',CONSTRAINT='rev869b_purge_failure_replay_mismatch',MESSAGE='Purge failure replay mismatch'; END IF; RETURN purge_attempt_id; END IF; UPDATE advance.rev869b_purge_attempts SET "State"=terminal_state,"TerminalAt"=clock_timestamp(),"FailureCategory"=category WHERE "PurgeAttemptId"=purge_attempt_id AND "State"='Started'; IF NOT FOUND THEN RAISE EXCEPTION 'Purge attempt not Started'; END IF; UPDATE advance.rev869b_purge_authorizations SET "State"=terminal_state WHERE "AuthorizationId"=(SELECT "AuthorizationId" FROM advance.rev869b_purge_attempts WHERE "PurgeAttemptId"=purge_attempt_id); INSERT INTO advance.rev869b_purge_events VALUES(DEFAULT,purge_attempt_id,terminal_state,evidence,clock_timestamp(),session_user); RETURN purge_attempt_id; END $f$;
CREATE FUNCTION advance.rev869b_reconcile_purge(purge_attempt_id uuid) RETURNS jsonb LANGUAGE sql SECURITY DEFINER STABLE SET search_path=pg_catalog,advance AS $f$ SELECT to_jsonb(p) FROM advance.rev869b_purge_attempts p WHERE p."PurgeAttemptId"=$1 AND session_user IN ('nexa_rev869b_purge_worker','nexa_rev869b_purge_audit','nexa_rev869b_target_verifier') $f$;

CREATE FUNCTION advance.rev869b_register_export_authorization(authorization_id uuid,decision_id uuid,organization text,fields text[],maximum_rows integer,as_of timestamptz,expires_at timestamptz) RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$ DECLARE allowed text[]:=ARRAY['occurredAt','eventType','organizationId','attemptId']; BEGIN IF session_user<>'nexa_rev869b_management_writer' OR organization='' OR fields<@allowed IS NOT TRUE OR array_length(fields,1) IS NULL OR cardinality(fields)<>(SELECT count(DISTINCT value) FROM unnest(fields) value) OR as_of>clock_timestamp() THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Management-approved export allowlist required'; END IF; INSERT INTO advance.rev869b_export_authorizations("AuthorizationId","ManagementDecisionId","OrganizationId","Fields","MaximumRows","AsOf","ExpiresAt") VALUES(authorization_id,decision_id,organization,fields,maximum_rows,as_of,expires_at); RETURN authorization_id; END $f$;
CREATE FUNCTION advance.rev869b_prepare_export_batch(batch_id uuid,authorization_id uuid) RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$ DECLARE a advance.rev869b_export_authorizations%ROWTYPE; rows_count integer; batch_sha bytea; BEGIN
  IF session_user<>'nexa_rev869b_export_service' THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Controlled export service required'; END IF; SELECT * INTO STRICT a FROM advance.rev869b_export_authorizations WHERE "AuthorizationId"=authorization_id AND "State"='Approved' AND "ExpiresAt">clock_timestamp() FOR UPDATE;
  INSERT INTO advance.rev869b_export_batches VALUES(batch_id,authorization_id,a."ManagementDecisionId",a."OrganizationId",a."Fields",a."MaximumRows",a."AsOf",a."ExpiresAt",0,digest('','sha256'),clock_timestamp()); INSERT INTO advance.rev869b_export_batch_rows SELECT batch_id,row_number() OVER(ORDER BY o."OccurredAt",o."OutcomeId")::integer,minimized.payload,digest(minimized.payload::text,'sha256') FROM advance.rev869b_command_attempt_outcomes o JOIN advance.rev869b_command_attempts a2 ON a2."AttemptId"=o."AttemptId" JOIN advance.rev869b_command_requests r ON r."CommandId"=a2."CommandId" CROSS JOIN LATERAL (SELECT coalesce(jsonb_object_agg(field.key,field.value),'{}'::jsonb) payload FROM jsonb_each(jsonb_strip_nulls(jsonb_build_object('occurredAt',o."OccurredAt",'eventType',o."TerminalState",'organizationId',r."OrganizationId",'attemptId',o."AttemptId"))) field WHERE field.key=ANY(a."Fields")) minimized WHERE r."OrganizationId"=a."OrganizationId" AND o."OccurredAt"<=a."AsOf" ORDER BY o."OccurredAt",o."OutcomeId" LIMIT a."MaximumRows";
  GET DIAGNOSTICS rows_count=ROW_COUNT; SELECT digest(coalesce(string_agg(encode("RowSha256",'hex'),',' ORDER BY "Ordinal"),''),'sha256') INTO batch_sha FROM advance.rev869b_export_batch_rows WHERE "BatchId"=batch_id; UPDATE advance.rev869b_export_batches SET "RowCount"=rows_count,"BatchSha256"=batch_sha WHERE "BatchId"=batch_id; UPDATE advance.rev869b_export_authorizations SET "State"='Prepared' WHERE "AuthorizationId"=authorization_id; RETURN batch_id;
END $f$;
CREATE FUNCTION advance.rev869b_authorize_export_release(batch_id uuid,release_id uuid) RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$ BEGIN IF session_user<>'nexa_rev869b_export_service' OR NOT EXISTS(SELECT 1 FROM advance.rev869b_export_batches WHERE "BatchId"=batch_id AND "ExpiresAt">clock_timestamp()) OR EXISTS(SELECT 1 FROM advance.rev869b_export_releases WHERE "BatchId"=batch_id AND "State" NOT IN ('Failed','Interrupted')) THEN RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_export_release_sequence',MESSAGE='Prepared unexpired retry-eligible batch required'; END IF; INSERT INTO advance.rev869b_export_releases VALUES(release_id,batch_id,'ReleaseStarted',clock_timestamp(),NULL,NULL); RETURN release_id; END $f$;
CREATE FUNCTION advance.rev869b_read_prepared_export_batch(batch_id uuid,release_id uuid) RETURNS TABLE(ordinal integer,payload jsonb,row_sha256 bytea) LANGUAGE sql SECURITY DEFINER STABLE SET search_path=pg_catalog,advance AS $f$ SELECT r."Ordinal",r."Payload",r."RowSha256" FROM advance.rev869b_export_batch_rows r JOIN advance.rev869b_export_releases x ON x."BatchId"=r."BatchId" JOIN advance.rev869b_export_batches b ON b."BatchId"=r."BatchId" WHERE r."BatchId"=$1 AND x."ReleaseId"=$2 AND x."State"='ReleaseStarted' AND b."ExpiresAt">clock_timestamp() AND session_user='nexa_rev869b_export_service' ORDER BY r."Ordinal" $f$;
CREATE FUNCTION advance.rev869b_record_export_release_outcome(release_id uuid,terminal_state text,category text) RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$ BEGIN IF session_user<>'nexa_rev869b_export_service' OR terminal_state NOT IN ('Delivered','Failed','Interrupted') OR (terminal_state='Delivered' AND category IS NOT NULL) OR (terminal_state<>'Delivered' AND length(category) NOT BETWEEN 1 AND 100) THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Exact release outcome required'; END IF; UPDATE advance.rev869b_export_releases SET "State"=terminal_state,"TerminalAt"=clock_timestamp(),"OutcomeCategory"=category WHERE "ReleaseId"=release_id AND "State"='ReleaseStarted'; IF NOT FOUND THEN RAISE EXCEPTION 'Release missing or terminal'; END IF; RETURN release_id; END $f$;

CREATE FUNCTION advance.rev869b_verify_target_catalogue_acl() RETURNS text LANGUAGE plpgsql SECURITY DEFINER STABLE SET search_path=pg_catalog,advance AS $f$ DECLARE mismatch boolean; BEGIN
  IF session_user<>'nexa_rev869b_target_verifier' THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Target verifier required'; END IF;
  IF (SELECT "CatalogueSha256"<>advance.rev869b_target_catalogue_fingerprint() FROM advance.rev869b_target_catalogue_manifest) IS DISTINCT FROM false THEN RAISE EXCEPTION 'Target catalogue fingerprint mismatch'; END IF;
  WITH expected(role_name,signature) AS (VALUES
    ('nexa_rev869b_app_runtime','advance.rev869b_open_command_attempt(uuid,uuid,text,text,text,text,bytea,jsonb)'),('nexa_rev869b_app_runtime','advance.rev869b_command_context_valid(text,uuid,text,text,text)'),('nexa_rev869b_app_runtime','advance.rev869b_claim_command_context(text,uuid,text,uuid,text,bigint,text,text,text,text)'),('nexa_rev869b_app_runtime','advance.rev869b_commit_command_attempt(uuid,bytea,jsonb,uuid)'),
    ('nexa_rev869b_command_audit','advance.rev869b_register_command_request(text,text,bytea,bytea,uuid,text,text,text)'),('nexa_rev869b_command_audit','advance.rev869b_start_command_attempt(uuid,uuid,bytea,bytea,name,integer,bigint)'),('nexa_rev869b_command_audit','advance.rev869b_record_noncommit_outcome(uuid,uuid,bytea,bytea,text,text,uuid)'),('nexa_rev869b_command_audit','advance.rev869b_reconcile_command_attempt(uuid)'),
    ('nexa_rev869b_management_writer','advance.rev869b_register_purge_authorization(uuid,uuid,uuid,uuid,uuid,bytea,text,text,timestamp with time zone,integer,bytea,text,bytea,timestamp with time zone)'),('nexa_rev869b_management_writer','advance.rev869b_register_export_authorization(uuid,uuid,text,text[],integer,timestamp with time zone,timestamp with time zone)'),
    ('nexa_rev869b_purge_worker','advance.rev869b_start_purge(uuid,uuid)'),('nexa_rev869b_purge_worker','advance.rev869b_execute_purge(uuid)'),('nexa_rev869b_purge_worker','advance.rev869b_reconcile_purge(uuid)'),
    ('nexa_rev869b_purge_audit','advance.rev869b_record_purge_failure(uuid,text,text,bytea)'),('nexa_rev869b_purge_audit','advance.rev869b_reconcile_purge(uuid)'),
    ('nexa_rev869b_export_service','advance.rev869b_prepare_export_batch(uuid,uuid)'),('nexa_rev869b_export_service','advance.rev869b_authorize_export_release(uuid,uuid)'),('nexa_rev869b_export_service','advance.rev869b_read_prepared_export_batch(uuid,uuid)'),('nexa_rev869b_export_service','advance.rev869b_record_export_release_outcome(uuid,text,text)'),
    ('nexa_rev869b_target_verifier','advance.rev869b_reconcile_command_attempt(uuid)'),('nexa_rev869b_target_verifier','advance.rev869b_reconcile_purge(uuid)'),('nexa_rev869b_target_verifier','advance.rev869b_read_target_security_state()'),('nexa_rev869b_target_verifier','advance.rev869b_read_command_evidence(uuid,uuid)'),('nexa_rev869b_target_verifier','advance.rev869b_read_purge_evidence(uuid,uuid)'),('nexa_rev869b_target_verifier','advance.rev869b_read_export_evidence(uuid,uuid,uuid)'),('nexa_rev869b_target_verifier','advance.rev869b_read_target_acl_evidence()'),('nexa_rev869b_target_verifier','advance.rev869b_read_command_evidence_v2(bytea,uuid,text,text,uuid,uuid)'),('nexa_rev869b_target_verifier','advance.rev869b_read_purge_evidence_v2(bytea,uuid,text,text,uuid,uuid,uuid,uuid)'),('nexa_rev869b_target_verifier','advance.rev869b_read_export_evidence_v2(bytea,uuid,text,text,uuid,uuid,uuid,timestamp with time zone)'),('nexa_rev869b_target_verifier','advance.rev869b_read_target_acl_evidence_v2(bytea,uuid,text,text,name,text,text,text)'),('nexa_rev869b_target_verifier','advance.rev869b_read_command_facts_v4(text,bytea,uuid,bigint,uuid,uuid,uuid,text,text,text[])'),('nexa_rev869b_target_verifier','advance.rev869b_read_purge_facts_v4(text,bytea,uuid,bigint,uuid,uuid,uuid,uuid,uuid,uuid,text,text,text[])'),('nexa_rev869b_target_verifier','advance.rev869b_read_export_facts_v4(text,bytea,uuid,bigint,uuid,uuid,uuid,timestamp with time zone,uuid,text,uuid,text,text[])'),('nexa_rev869b_target_verifier','advance.rev869b_read_target_acl_facts_v4(text,bytea,uuid,bigint,uuid,uuid,name,text,text,text,text,text[])'),('nexa_rev869b_target_verifier','advance.rev869b_target_catalogue_fingerprint()'),('nexa_rev869b_target_verifier','advance.rev869b_verify_target_catalogue_acl()')),
  actual AS (SELECT r.rolname::text role_name,p.oid::regprocedure::text signature FROM pg_roles r CROSS JOIN pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='advance' AND NOT r.rolsuper AND r.rolname!~'^pg_' AND r.rolname<>'nexa_rev869b_security_owner' AND has_function_privilege(r.oid,p.oid,'EXECUTE')),
  delta AS ((SELECT * FROM expected EXCEPT SELECT * FROM actual) UNION ALL (SELECT * FROM actual EXCEPT SELECT * FROM expected)) SELECT EXISTS(SELECT 1 FROM delta) INTO mismatch;
  IF mismatch THEN RAISE EXCEPTION 'Target function ACL mismatch'; END IF;
  WITH read_relation(relation_name) AS (VALUES
    ('employees'),('employee_identity_mappings'),('employee_role_assignments'),('employee_operational_scopes'),('roles'),('role_page_permissions'),('page_definitions'),('items'),('uoms'),('uom_conversions'),('vendors'),('tax_gst_settings'),('organization_policies'),('warehouses'),('purchase_requisitions'),('purchase_requisition_lines'),('purchase_requirement_handoffs'),('purchase_approval_route_settings'),('purchase_approval_workflow_steps'),('purchase_transaction_approval_policies')),
  write_relation(relation_name) AS (VALUES
    ('vendor_qualifications'),('controlled_configuration_histories'),('request_for_quotations'),('request_for_quotation_lines'),('rfq_vendor_invitations'),('vendor_quotations'),('vendor_quotation_lines'),('quotation_technical_verifications'),('commercial_comparisons'),('commercial_comparison_lines'),('purchase_transaction_status_history'),('purchase_transaction_approval_history'),('purchase_orders'),('purchase_order_lines'),('purchase_order_history'),('material_followup_handoffs'),('purchase_number_sequences')),
  expected(role_name,relation_name,privilege_name) AS (
    SELECT 'nexa_rev869b_app_runtime',relation_name,'SELECT' FROM read_relation
    UNION ALL SELECT 'nexa_rev869b_app_runtime',relation_name,p FROM write_relation CROSS JOIN unnest(ARRAY['SELECT','INSERT','UPDATE']) p
    UNION ALL SELECT 'nexa_rev869b_app_runtime','audit_logs',p FROM unnest(ARRAY['SELECT','INSERT']) p),
  checked_role(role_name) AS (SELECT rolname::text FROM pg_roles WHERE NOT rolsuper AND rolname!~'^pg_' AND rolname<>'nexa_rev869b_security_owner'),
  checked_privilege(privilege_name) AS (VALUES ('SELECT'),('INSERT'),('UPDATE'),('DELETE'),('TRUNCATE'),('REFERENCES'),('TRIGGER')),
  actual AS (SELECT r.role_name,c.relname::text relation_name,p.privilege_name FROM checked_role r CROSS JOIN pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace CROSS JOIN checked_privilege p WHERE n.nspname='advance' AND c.relkind IN ('r','p','v','m','f') AND has_table_privilege(r.role_name,c.oid,p.privilege_name)),
  delta AS ((SELECT * FROM expected EXCEPT SELECT * FROM actual) UNION ALL (SELECT * FROM actual EXCEPT SELECT * FROM expected)) SELECT EXISTS(SELECT 1 FROM delta) INTO mismatch;
  IF mismatch OR EXISTS(SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='advance' AND c.relkind IN ('r','p','v','m','f') AND has_table_privilege('public',c.oid,'SELECT,INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER')) THEN RAISE EXCEPTION 'Target relation ACL mismatch'; END IF;
  IF EXISTS(SELECT 1 FROM pg_roles r CROSS JOIN pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='advance' AND c.relkind='S' AND NOT r.rolsuper AND r.rolname!~'^pg_' AND r.rolname<>'nexa_rev869b_security_owner' AND has_sequence_privilege(r.oid,c.oid,'SELECT,UPDATE,USAGE')) OR EXISTS(SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='advance' AND c.relkind='S' AND has_sequence_privilege('public',c.oid,'SELECT,UPDATE,USAGE')) THEN RAISE EXCEPTION 'Target sequence ACL mismatch'; END IF;
  IF EXISTS(SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='advance' AND c.relkind IN ('r','p','S','v','m','f') AND pg_get_userbyid(c.relowner)<>'nexa_rev869b_security_owner') OR EXISTS(SELECT 1 FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='advance' AND pg_get_userbyid(p.proowner)<>'nexa_rev869b_security_owner') THEN RAISE EXCEPTION 'Target object ownership mismatch'; END IF;
  IF EXISTS(SELECT 1 FROM pg_default_acl d CROSS JOIN LATERAL aclexplode(d.defaclacl) x WHERE d.defaclnamespace='advance'::regnamespace AND (x.grantee=0 OR x.grantee<>d.defaclrole)) THEN RAISE EXCEPTION 'Target default ACL mismatch'; END IF;
  IF EXISTS(SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace CROSS JOIN LATERAL aclexplode(c.relacl) x JOIN pg_roles r ON r.oid=x.grantee WHERE n.nspname='advance' AND r.rolname~'^pg_' UNION ALL SELECT 1 FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace CROSS JOIN LATERAL aclexplode(p.proacl) x JOIN pg_roles r ON r.oid=x.grantee WHERE n.nspname='advance' AND r.rolname~'^pg_' UNION ALL SELECT 1 FROM pg_namespace n CROSS JOIN LATERAL aclexplode(n.nspacl) x JOIN pg_roles r ON r.oid=x.grantee WHERE n.nspname='advance' AND r.rolname~'^pg_' UNION ALL SELECT 1 FROM pg_database d CROSS JOIN LATERAL aclexplode(d.datacl) x JOIN pg_roles r ON r.oid=x.grantee WHERE d.datname=current_database() AND r.rolname~'^pg_' UNION ALL SELECT 1 FROM pg_default_acl d CROSS JOIN LATERAL aclexplode(d.defaclacl) x JOIN pg_roles r ON r.oid=x.grantee WHERE d.defaclnamespace='advance'::regnamespace AND r.rolname~'^pg_') THEN RAISE EXCEPTION 'Target predefined role direct ACL mismatch'; END IF;
  IF EXISTS(SELECT 1 FROM pg_auth_members m JOIN pg_roles a ON a.oid=m.roleid JOIN pg_roles b ON b.oid=m.member WHERE (a.rolname LIKE 'nexa_rev869b_%' OR b.rolname LIKE 'nexa_rev869b_%') AND NOT (a.rolname='nexa_rev869b_security_owner' AND b.rolname='nexa_rev869b_lifecycle_administrator')) OR NOT EXISTS(SELECT 1 FROM pg_auth_members m JOIN pg_roles a ON a.oid=m.roleid JOIN pg_roles b ON b.oid=m.member WHERE a.rolname='nexa_rev869b_security_owner' AND b.rolname='nexa_rev869b_lifecycle_administrator') THEN RAISE EXCEPTION 'Target role membership mismatch'; END IF;
  IF EXISTS(SELECT 1 FROM pg_roles r WHERE NOT r.rolsuper AND r.rolname!~'^pg_' AND r.rolname NOT IN ('nexa_rev869b_security_owner','nexa_rev869b_lifecycle_administrator','nexa_rev869b_app_runtime','nexa_rev869b_command_audit','nexa_rev869b_management_writer','nexa_rev869b_purge_worker','nexa_rev869b_purge_audit','nexa_rev869b_export_service','nexa_rev869b_target_verifier') AND has_database_privilege(r.oid,current_database(),'CONNECT,TEMPORARY')) OR EXISTS(SELECT 1 FROM pg_roles r WHERE r.rolname IN ('nexa_rev869b_lifecycle_administrator','nexa_rev869b_app_runtime','nexa_rev869b_command_audit','nexa_rev869b_management_writer','nexa_rev869b_purge_worker','nexa_rev869b_purge_audit','nexa_rev869b_export_service','nexa_rev869b_target_verifier') AND (NOT has_database_privilege(r.oid,current_database(),'CONNECT') OR has_database_privilege(r.oid,current_database(),'TEMPORARY'))) THEN RAISE EXCEPTION 'Target database ACL mismatch'; END IF;
  IF EXISTS(SELECT 1 FROM pg_roles r WHERE NOT r.rolsuper AND r.rolname!~'^pg_' AND r.rolname<>'nexa_rev869b_security_owner' AND (has_schema_privilege(r.oid,'advance','CREATE') OR has_schema_privilege(r.oid,'advance','USAGE') IS DISTINCT FROM (r.rolname IN ('nexa_rev869b_app_runtime','nexa_rev869b_command_audit','nexa_rev869b_management_writer','nexa_rev869b_purge_worker','nexa_rev869b_purge_audit','nexa_rev869b_export_service','nexa_rev869b_target_verifier')))) THEN RAISE EXCEPTION 'Target schema ACL mismatch'; END IF;
  IF pg_get_userbyid((SELECT nspowner FROM pg_namespace WHERE nspname='advance'))<>'nexa_rev869b_security_owner' THEN RAISE EXCEPTION 'Target schema owner mismatch'; END IF;
  IF pg_get_userbyid((SELECT datdba FROM pg_database WHERE datname=current_database()))<>'nexa_rev869b_security_owner' THEN RAISE EXCEPTION 'Target database owner mismatch'; END IF;
  IF EXISTS(SELECT 1 FROM pg_roles r WHERE r.rolname IN ('nexa_rev869b_security_owner','nexa_rev869b_lifecycle_administrator','nexa_rev869b_app_runtime','nexa_rev869b_command_audit','nexa_rev869b_management_writer','nexa_rev869b_purge_worker','nexa_rev869b_purge_audit','nexa_rev869b_export_service','nexa_rev869b_target_verifier') AND (r.rolsuper OR r.rolcreatedb OR r.rolcreaterole OR r.rolreplication OR r.rolbypassrls OR r.rolinherit OR (r.rolname='nexa_rev869b_security_owner' AND r.rolcanlogin) OR (r.rolname<>'nexa_rev869b_security_owner' AND NOT r.rolcanlogin))) THEN RAISE EXCEPTION 'Target role capability mismatch'; END IF;
  RETURN 'REV869B_TARGET_CATALOGUE_ACL_EXACT'; END $f$;

DO $ownership$ DECLARE o regclass; f regprocedure; BEGIN
  FOR o IN SELECT c.oid::regclass FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='advance' AND c.relkind IN ('r','p') LOOP EXECUTE format('ALTER TABLE %s OWNER TO nexa_rev869b_security_owner',o); END LOOP;
  FOR o IN SELECT c.oid::regclass FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='advance' AND c.relkind='S' LOOP EXECUTE format('ALTER SEQUENCE %s OWNER TO nexa_rev869b_security_owner',o); END LOOP;
  FOR o IN SELECT c.oid::regclass FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='advance' AND c.relkind='v' LOOP EXECUTE format('ALTER VIEW %s OWNER TO nexa_rev869b_security_owner',o); END LOOP;
  FOR o IN SELECT c.oid::regclass FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='advance' AND c.relkind='m' LOOP EXECUTE format('ALTER MATERIALIZED VIEW %s OWNER TO nexa_rev869b_security_owner',o); END LOOP;
  FOR o IN SELECT c.oid::regclass FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='advance' AND c.relkind='f' LOOP EXECUTE format('ALTER FOREIGN TABLE %s OWNER TO nexa_rev869b_security_owner',o); END LOOP;
  FOR f IN SELECT p.oid::regprocedure FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='advance' LOOP EXECUTE format('ALTER FUNCTION %s OWNER TO nexa_rev869b_security_owner',f); END LOOP;
  ALTER SCHEMA advance OWNER TO nexa_rev869b_security_owner;
END $ownership$;
REVOKE ALL ON ALL TABLES IN SCHEMA advance FROM PUBLIC,nexa_rev869b_app_runtime,nexa_rev869b_command_audit,nexa_rev869b_management_writer,nexa_rev869b_purge_worker,nexa_rev869b_purge_audit,nexa_rev869b_export_service,nexa_rev869b_target_verifier;
REVOKE ALL ON ALL SEQUENCES IN SCHEMA advance FROM PUBLIC,nexa_rev869b_app_runtime,nexa_rev869b_command_audit,nexa_rev869b_management_writer,nexa_rev869b_purge_worker,nexa_rev869b_purge_audit,nexa_rev869b_export_service,nexa_rev869b_target_verifier;
REVOKE EXECUTE ON ALL FUNCTIONS IN SCHEMA advance FROM PUBLIC,nexa_rev869b_app_runtime,nexa_rev869b_command_audit,nexa_rev869b_management_writer,nexa_rev869b_purge_worker,nexa_rev869b_purge_audit,nexa_rev869b_export_service,nexa_rev869b_target_verifier;
GRANT USAGE ON SCHEMA advance TO nexa_rev869b_app_runtime,nexa_rev869b_command_audit,nexa_rev869b_management_writer,nexa_rev869b_purge_worker,nexa_rev869b_purge_audit,nexa_rev869b_export_service,nexa_rev869b_target_verifier;
GRANT SELECT ON advance.employees,advance.employee_identity_mappings,advance.employee_role_assignments,advance.employee_operational_scopes,advance.roles,advance.role_page_permissions,advance.page_definitions,advance.items,advance.uoms,advance.uom_conversions,advance.vendors,advance.tax_gst_settings,advance.organization_policies,advance.warehouses,advance.purchase_requisitions,advance.purchase_requisition_lines,advance.purchase_requirement_handoffs,advance.purchase_approval_route_settings,advance.purchase_approval_workflow_steps,advance.purchase_transaction_approval_policies TO nexa_rev869b_app_runtime;
GRANT SELECT,INSERT,UPDATE ON advance.vendor_qualifications,advance.controlled_configuration_histories,advance.request_for_quotations,advance.request_for_quotation_lines,advance.rfq_vendor_invitations,advance.vendor_quotations,advance.vendor_quotation_lines,advance.quotation_technical_verifications,advance.commercial_comparisons,advance.commercial_comparison_lines,advance.purchase_transaction_status_history,advance.purchase_transaction_approval_history,advance.purchase_orders,advance.purchase_order_lines,advance.purchase_order_history,advance.material_followup_handoffs,advance.purchase_number_sequences TO nexa_rev869b_app_runtime;
GRANT SELECT,INSERT ON advance.audit_logs TO nexa_rev869b_app_runtime;
GRANT EXECUTE ON FUNCTION advance.rev869b_open_command_attempt(uuid,uuid,text,text,text,text,bytea,jsonb),advance.rev869b_command_context_valid(text,uuid,text,text,text),advance.rev869b_claim_command_context(text,uuid,text,uuid,text,bigint,text,text,text,text),advance.rev869b_commit_command_attempt(uuid,bytea,jsonb,uuid) TO nexa_rev869b_app_runtime;
GRANT EXECUTE ON FUNCTION advance.rev869b_register_command_request(text,text,bytea,bytea,uuid,text,text,text),advance.rev869b_start_command_attempt(uuid,uuid,bytea,bytea,name,integer,bigint),advance.rev869b_record_noncommit_outcome(uuid,uuid,bytea,bytea,text,text,uuid),advance.rev869b_reconcile_command_attempt(uuid) TO nexa_rev869b_command_audit;
GRANT EXECUTE ON FUNCTION advance.rev869b_register_purge_authorization(uuid,uuid,uuid,uuid,uuid,bytea,text,text,timestamptz,integer,bytea,text,bytea,timestamptz),advance.rev869b_register_export_authorization(uuid,uuid,text,text[],integer,timestamptz,timestamptz) TO nexa_rev869b_management_writer;
GRANT EXECUTE ON FUNCTION advance.rev869b_start_purge(uuid,uuid),advance.rev869b_execute_purge(uuid),advance.rev869b_reconcile_purge(uuid) TO nexa_rev869b_purge_worker;
GRANT EXECUTE ON FUNCTION advance.rev869b_record_purge_failure(uuid,text,text,bytea),advance.rev869b_reconcile_purge(uuid) TO nexa_rev869b_purge_audit;
GRANT EXECUTE ON FUNCTION advance.rev869b_prepare_export_batch(uuid,uuid),advance.rev869b_authorize_export_release(uuid,uuid),advance.rev869b_read_prepared_export_batch(uuid,uuid),advance.rev869b_record_export_release_outcome(uuid,text,text) TO nexa_rev869b_export_service;
GRANT EXECUTE ON FUNCTION advance.rev869b_reconcile_command_attempt(uuid),advance.rev869b_reconcile_purge(uuid),advance.rev869b_read_target_security_state(),advance.rev869b_read_command_evidence(uuid,uuid),advance.rev869b_read_purge_evidence(uuid,uuid),advance.rev869b_read_export_evidence(uuid,uuid,uuid),advance.rev869b_read_target_acl_evidence(),advance.rev869b_read_command_evidence_v2(bytea,uuid,text,text,uuid,uuid),advance.rev869b_read_purge_evidence_v2(bytea,uuid,text,text,uuid,uuid,uuid,uuid),advance.rev869b_read_export_evidence_v2(bytea,uuid,text,text,uuid,uuid,uuid,timestamptz),advance.rev869b_read_target_acl_evidence_v2(bytea,uuid,text,text,name,text,text,text),advance.rev869b_read_command_facts_v4(text,bytea,uuid,bigint,uuid,uuid,uuid,text,text,text[]),advance.rev869b_read_purge_facts_v4(text,bytea,uuid,bigint,uuid,uuid,uuid,uuid,uuid,uuid,text,text,text[]),advance.rev869b_read_export_facts_v4(text,bytea,uuid,bigint,uuid,uuid,uuid,timestamptz,uuid,text,uuid,text,text[]),advance.rev869b_read_target_acl_facts_v4(text,bytea,uuid,bigint,uuid,uuid,name,text,text,text,text,text[]) TO nexa_rev869b_target_verifier;
GRANT EXECUTE ON FUNCTION advance.rev869b_target_catalogue_fingerprint(),advance.rev869b_verify_target_catalogue_acl() TO nexa_rev869b_target_verifier;
ALTER DEFAULT PRIVILEGES FOR ROLE nexa_rev869b_security_owner IN SCHEMA advance REVOKE ALL ON TABLES FROM PUBLIC,nexa_rev869b_app_runtime,nexa_rev869b_command_audit,nexa_rev869b_management_writer,nexa_rev869b_purge_worker,nexa_rev869b_purge_audit,nexa_rev869b_export_service,nexa_rev869b_target_verifier;
ALTER DEFAULT PRIVILEGES FOR ROLE nexa_rev869b_security_owner IN SCHEMA advance REVOKE ALL ON SEQUENCES FROM PUBLIC,nexa_rev869b_app_runtime,nexa_rev869b_command_audit,nexa_rev869b_management_writer,nexa_rev869b_purge_worker,nexa_rev869b_purge_audit,nexa_rev869b_export_service,nexa_rev869b_target_verifier;
ALTER DEFAULT PRIVILEGES FOR ROLE nexa_rev869b_security_owner IN SCHEMA advance REVOKE EXECUTE ON FUNCTIONS FROM PUBLIC,nexa_rev869b_app_runtime,nexa_rev869b_command_audit,nexa_rev869b_management_writer,nexa_rev869b_purge_worker,nexa_rev869b_purge_audit,nexa_rev869b_export_service,nexa_rev869b_target_verifier;
UPDATE advance.rev869b_target_catalogue_manifest SET "CatalogueSha256"=advance.rev869b_target_catalogue_fingerprint();

DO $rev869b_qualification_preflight$
BEGIN
  IF EXISTS (SELECT 1 FROM advance.vendor_qualifications q WHERE NOT (
    (q."VerificationStatus"='Draft' AND q."ApprovalStatus"='Draft' AND q."VerifiedByEmployeeId" IS NULL AND q."ApprovedByEmployeeId" IS NULL) OR
    (q."VerificationStatus"='Pending Approval' AND q."ApprovalStatus"='Pending Approval' AND q."VerifiedByEmployeeId" IS NULL AND q."ApprovedByEmployeeId" IS NULL AND q."IsActive") OR
    (q."VerificationStatus"='Verified' AND q."ApprovalStatus"='Pending Approval' AND q."VerifiedByEmployeeId" IS NOT NULL AND q."ApprovedByEmployeeId" IS NULL AND q."IsActive") OR
    (q."VerificationStatus" IN ('Verified','Approved') AND q."ApprovalStatus"='Approved' AND q."VerifiedByEmployeeId" IS NOT NULL AND q."ApprovedByEmployeeId" IS NOT NULL AND q."VerifiedByEmployeeId"<>q."ApprovedByEmployeeId" AND q."IsActive") OR
    (q."VerificationStatus"='Pending Approval' AND q."ApprovalStatus"='Rejected' AND q."VerifiedByEmployeeId" IS NULL AND q."ApprovedByEmployeeId" IS NOT NULL AND NOT q."IsActive") OR
    (q."VerificationStatus"='Verified' AND q."ApprovalStatus"='Rejected' AND q."VerifiedByEmployeeId" IS NOT NULL AND q."ApprovedByEmployeeId" IS NOT NULL AND q."VerifiedByEmployeeId"<>q."ApprovedByEmployeeId" AND NOT q."IsActive") OR
    (q."VerificationStatus" IN ('Verified','Approved') AND q."ApprovalStatus"='Revision Requested' AND q."VerifiedByEmployeeId" IS NOT NULL AND q."ApprovedByEmployeeId" IS NOT NULL AND q."VerifiedByEmployeeId"<>q."ApprovedByEmployeeId" AND NOT q."IsActive")
  )) THEN
    RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_qualification_existing_state_preflight',
      MESSAGE='Existing vendor qualification lifecycle values require controlled correction before REV869B can apply.';
  END IF;
END $rev869b_qualification_preflight$;
ALTER TABLE advance.vendor_qualifications DROP CONSTRAINT IF EXISTS "CK_vendor_qualification_rev869b_lifecycle";
ALTER TABLE advance.vendor_qualifications ADD CONSTRAINT "CK_vendor_qualification_rev869b_lifecycle" CHECK (
  ("VerificationStatus"='Draft' AND "ApprovalStatus"='Draft' AND "VerifiedByEmployeeId" IS NULL AND "ApprovedByEmployeeId" IS NULL) OR
  ("VerificationStatus"='Pending Approval' AND "ApprovalStatus"='Pending Approval' AND "VerifiedByEmployeeId" IS NULL AND "ApprovedByEmployeeId" IS NULL AND "IsActive") OR
  ("VerificationStatus"='Verified' AND "ApprovalStatus"='Pending Approval' AND "VerifiedByEmployeeId" IS NOT NULL AND "ApprovedByEmployeeId" IS NULL AND "IsActive") OR
  ("VerificationStatus" IN ('Verified','Approved') AND "ApprovalStatus"='Approved' AND "VerifiedByEmployeeId" IS NOT NULL AND "ApprovedByEmployeeId" IS NOT NULL AND "VerifiedByEmployeeId"<>"ApprovedByEmployeeId" AND "IsActive") OR
  ("VerificationStatus"='Pending Approval' AND "ApprovalStatus"='Rejected' AND "VerifiedByEmployeeId" IS NULL AND "ApprovedByEmployeeId" IS NOT NULL AND NOT "IsActive") OR
  ("VerificationStatus"='Verified' AND "ApprovalStatus"='Rejected' AND "VerifiedByEmployeeId" IS NOT NULL AND "ApprovedByEmployeeId" IS NOT NULL AND "VerifiedByEmployeeId"<>"ApprovedByEmployeeId" AND NOT "IsActive") OR
  ("VerificationStatus" IN ('Verified','Approved') AND "ApprovalStatus"='Revision Requested' AND "VerifiedByEmployeeId" IS NOT NULL AND "ApprovedByEmployeeId" IS NOT NULL AND "VerifiedByEmployeeId"<>"ApprovedByEmployeeId" AND NOT "IsActive")
);

CREATE OR REPLACE FUNCTION advance.rev869b_guard_durable_audit_retention()
RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $rev869b$
DECLARE cleanup_reason text:=nullif(trim(current_setting('advance.rev869b_audit_cleanup_reason',true)),'');
  cleanup_correlation text:=nullif(trim(current_setting('advance.rev869b_audit_cleanup_correlation',true)),'');
  database_owner name;
BEGIN
  IF TG_OP='UPDATE' THEN
    RAISE EXCEPTION USING ERRCODE='42501',SCHEMA='advance',TABLE='audit_logs',
      CONSTRAINT='rev869b_audit_immutable',MESSAGE='Durable audit evidence is immutable.';
  END IF;
  IF OLD."CreatedAt">statement_timestamp()-interval '10 years' THEN
    RAISE EXCEPTION USING ERRCODE='42501',SCHEMA='advance',TABLE='audit_logs',
      CONSTRAINT='rev869b_audit_minimum_ten_year_retention',MESSAGE='Durable audit evidence must be retained for at least ten years.';
  END IF;
  SELECT pg_get_userbyid(d.datdba) INTO STRICT database_owner FROM pg_database d WHERE d.datname=current_database();
  IF session_user IS DISTINCT FROM database_owner OR cleanup_reason IS NULL OR octet_length(cleanup_reason)>1000 OR
     cleanup_correlation IS NULL OR octet_length(cleanup_correlation)>120 THEN
    RAISE EXCEPTION USING ERRCODE='42501',SCHEMA='advance',TABLE='audit_logs',
      CONSTRAINT='rev869b_audit_controlled_cleanup',MESSAGE='Expired audit cleanup requires the database owner, an exact correlation, and a bounded reason.';
  END IF;
  INSERT INTO advance.audit_logs
    ("Id","Module","Action","EntityName","EntityId","UserLoginId","Result","CorrelationId","BeforeJson","AfterJson","CreatedAt","CreatedBy","Version")
  VALUES(gen_random_uuid(),'Security','PurgeExpiredAudit','AuditLogRetention',
    encode(public.digest(convert_to(OLD."Id"::text,'UTF8'),'sha256'),'hex'),session_user,'Success',cleanup_correlation,NULL,
    jsonb_build_object('minimumRetentionYears',10,'reason',cleanup_reason,'deletedCreatedAt',OLD."CreatedAt")::text,
    statement_timestamp(),session_user,0);
  RETURN OLD;
END $rev869b$;
REVOKE ALL ON FUNCTION advance.rev869b_guard_durable_audit_retention() FROM PUBLIC;
CREATE TRIGGER trg_rev869b_durable_audit_retention BEFORE UPDATE OR DELETE ON advance.audit_logs
  FOR EACH ROW EXECUTE FUNCTION advance.rev869b_guard_durable_audit_retention();

CREATE OR REPLACE FUNCTION advance.rev869b_guard_history_insert()
RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $rev869b$
DECLARE matches bigint; creator_matches bigint; parent_version bigint; parent_login text; parent_org text; parent_number text; parent_status text; parent_correlation text; parent_creator text; parent_creator_employee uuid;
BEGIN
  IF TG_TABLE_NAME='purchase_transaction_status_history' THEN
    SELECT count(*),min(p.version),min(p.login),min(p.org),min(p.number),min(p.status),min(p.correlation),min(p.creator)
      INTO matches,parent_version,parent_login,parent_org,parent_number,parent_status,parent_correlation,parent_creator FROM (
      SELECT r."Version" version,coalesce(r."UpdatedBy",r."CreatedBy") login,r."OrganizationId" org,r."RfqNumber" number,r."Status" status,r."TransitionCorrelationId" correlation,r."CreatedBy" creator FROM advance.request_for_quotations r WHERE NEW."EntityType"='RFQ' AND r."Id"=NEW."EntityId" AND r.xmin::text::bigint=txid_current()
      UNION ALL SELECT i."Version",coalesce(i."UpdatedBy",i."CreatedBy"),r."OrganizationId",r."RfqNumber",i."Status",i."TransitionCorrelationId",i."CreatedBy" FROM advance.rfq_vendor_invitations i JOIN advance.request_for_quotations r ON r."Id"=i."RequestForQuotationId" WHERE NEW."EntityType"='RFQInvitation' AND i."Id"=NEW."EntityId" AND i.xmin::text::bigint=txid_current()
      UNION ALL SELECT q."Version",coalesce(q."UpdatedBy",q."CreatedBy"),q."OrganizationId",q."QuotationNumber",q."Status",q."TransitionCorrelationId",q."CreatedBy" FROM advance.vendor_quotations q WHERE NEW."EntityType"='VendorQuotation' AND q."Id"=NEW."EntityId" AND q.xmin::text::bigint=txid_current()
      UNION ALL SELECT v."Version",coalesce(v."UpdatedBy",v."CreatedBy"),q."OrganizationId",q."QuotationNumber",v."ComplianceStatus",v."CorrelationId",v."CreatedBy" FROM advance.quotation_technical_verifications v JOIN advance.vendor_quotation_lines l ON l."Id"=v."VendorQuotationLineId" JOIN advance.vendor_quotations q ON q."Id"=l."VendorQuotationId" WHERE NEW."EntityType"='TechnicalVerification' AND v."Id"=NEW."EntityId" AND v.xmin::text::bigint=txid_current()
      UNION ALL SELECT c."Version",coalesce(c."UpdatedBy",c."CreatedBy"),c."OrganizationId",c."ComparisonNumber",c."Status",c."TransitionCorrelationId",c."CreatedBy" FROM advance.commercial_comparisons c WHERE NEW."EntityType"='CommercialComparison' AND c."Id"=NEW."EntityId" AND c.xmin::text::bigint=txid_current()
      UNION ALL SELECT p."Version",coalesce(p."UpdatedBy",p."CreatedBy"),p."OrganizationId",p."PoNumber",p."Status",p."TransitionCorrelationId",p."CreatedBy" FROM advance.purchase_orders p WHERE NEW."EntityType"='PurchaseOrder' AND p."Id"=NEW."EntityId" AND p.xmin::text::bigint=txid_current()
      UNION ALL SELECT h."Version",coalesce(h."UpdatedBy",h."CreatedBy"),p."OrganizationId",h."HandoffNumber",h."Status",h."CorrelationId",h."CreatedBy" FROM advance.material_followup_handoffs h JOIN advance.purchase_orders p ON p."Id"=h."PurchaseOrderId" WHERE NEW."EntityType"='MaterialFollowUp' AND h."Id"=NEW."EntityId" AND h.xmin::text::bigint=txid_current()
    ) p;
    IF matches<>1 OR NEW."OrganizationId"<>parent_org OR NEW."DocumentNumber"<>parent_number OR NEW."ToStatus"<>parent_status THEN
      RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_history_parent_transition',MESSAGE='Status history requires the exact parent mutation in the current transaction.';
    END IF;
  ELSIF TG_TABLE_NAME='purchase_transaction_approval_history' THEN
    SELECT count(*),min(c."Version"),min(coalesce(c."UpdatedBy",c."CreatedBy")),min(c."OrganizationId"),min(c."ComparisonNumber"),min(c."Status"),min(c."TransitionCorrelationId"),min(c."CreatedBy") INTO matches,parent_version,parent_login,parent_org,parent_number,parent_status,parent_correlation,parent_creator
      FROM advance.commercial_comparisons c WHERE c."Id"=NEW."CommercialComparisonId" AND c.xmin::text::bigint=txid_current();
    IF matches<>1 OR NEW."ToStatus"<>parent_status OR NEW."ApprovalRoute"<>(SELECT c."ApprovalRoute" FROM advance.commercial_comparisons c WHERE c."Id"=NEW."CommercialComparisonId") THEN
      RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_approval_history_parent_transition',MESSAGE='Approval history requires the exact comparison transition in the current transaction.';
    END IF;
  ELSE
    SELECT count(*),min(p."Version"),min(coalesce(p."UpdatedBy",p."CreatedBy")),min(p."OrganizationId"),min(p."PoNumber"),min(p."Status"),min(p."TransitionCorrelationId"),min(p."CreatedBy") INTO matches,parent_version,parent_login,parent_org,parent_number,parent_status,parent_correlation,parent_creator
      FROM advance.purchase_orders p WHERE p."Id"=NEW."PurchaseOrderId" AND p.xmin::text::bigint=txid_current();
    IF matches<>1 OR NEW."ToStatus"<>parent_status OR NEW."RevisionNumber"<>(SELECT p."RevisionNumber" FROM advance.purchase_orders p WHERE p."Id"=NEW."PurchaseOrderId") THEN
      RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_po_history_parent_transition',MESSAGE='PO history requires the exact purchase-order transition in the current transaction.';
    END IF;
  END IF;
  IF NEW."ActorLoginId" IS DISTINCT FROM parent_login OR NEW."CreatedBy" IS DISTINCT FROM parent_login OR NEW."CorrelationId" IS DISTINCT FROM parent_correlation OR
     advance.rev869b_command_context_valid(parent_org,NEW."ActorEmployeeId",
       current_setting('advance.rev869b_identity_issuer',true),NEW."ActorLoginId",NEW."ActorRoleCode") IS NOT TRUE OR
     length(trim(coalesce(CASE WHEN TG_TABLE_NAME='purchase_order_history' THEN NEW."Reason" ELSE NEW."Remarks" END,'')))=0 OR
     NOT EXISTS (SELECT 1 FROM advance.employee_identity_mappings m JOIN advance.employees e ON e."Id"=m."EmployeeId" WHERE m."Subject"=NEW."ActorLoginId" AND m."EmployeeId"=NEW."ActorEmployeeId" AND m."OrganizationId"=parent_org AND m."IsActive" AND m."EffectiveFrom"<=statement_timestamp()::date AND (m."EffectiveTo" IS NULL OR m."EffectiveTo">=statement_timestamp()::date) AND e."Status"='Active' AND e."LoginEnabled") OR
     NOT EXISTS (SELECT 1 FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId" WHERE a."EmployeeId"=NEW."ActorEmployeeId" AND r."Code"=NEW."ActorRoleCode" AND r."IsActive" AND a."ApprovalStatus" IN ('Approved','SeedApproved') AND a."EffectiveFrom"<=statement_timestamp()::date AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=statement_timestamp()::date)) THEN
    RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_history_actor_binding',MESSAGE='History actor, role, correlation and remarks must match the current controlled transition.';
  END IF;
  SELECT count(*),min(m."EmployeeId") INTO creator_matches,parent_creator_employee
    FROM advance.employee_identity_mappings m JOIN advance.employees e ON e."Id"=m."EmployeeId"
    WHERE m."Subject"=parent_creator AND m."OrganizationId"=parent_org AND m."IsActive"
      AND m."EffectiveFrom"<=statement_timestamp()::date AND (m."EffectiveTo" IS NULL OR m."EffectiveTo">=statement_timestamp()::date)
      AND e."Status"='Active' AND e."LoginEnabled";
  IF creator_matches<>1 THEN
    RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_parent_creator_identity_binding',MESSAGE='Parent creator must resolve to exactly one active employee identity.';
  END IF;
  IF NEW."Action"='Approve' AND NEW."ActorEmployeeId"=parent_creator_employee THEN
    RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_creator_self_approval',MESSAGE='Creator self-approval is prohibited.';
  END IF;
  IF NEW."Action"='Approve' AND TG_TABLE_NAME='purchase_transaction_approval_history' AND EXISTS (
    SELECT 1 FROM advance.commercial_comparisons c JOIN advance.commercial_comparison_lines cl ON cl."CommercialComparisonId"=c."Id" AND cl."IsRecommended"
    JOIN advance.vendor_quotation_lines ql ON ql."Id"=cl."VendorQuotationLineId" JOIN advance.quotation_technical_verifications v ON v."VendorQuotationLineId"=ql."Id"
    WHERE c."Id"=NEW."CommercialComparisonId" AND v."VerifierEmployeeId"=NEW."ActorEmployeeId") THEN
    RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_verifier_approver_separation',MESSAGE='Technical verifier cannot approve the same controlled comparison.';
  END IF;
  IF NEW."Action"='Approve' AND TG_TABLE_NAME='purchase_order_history' AND EXISTS (
    SELECT 1 FROM advance.purchase_transaction_status_history h WHERE h."EntityType"='PurchaseOrder' AND h."EntityId"=NEW."PurchaseOrderId"
      AND h."ToStatus" IN ('PendingApproval','Resubmitted') AND h."ActorEmployeeId"=NEW."ActorEmployeeId" AND h."Version"=parent_version-1) THEN
    RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_issuer_approver_separation',MESSAGE='The PO submitter or resubmitter cannot approve the same controlled version.';
  END IF;
  IF NEW."Action"='Issue' AND TG_TABLE_NAME='purchase_order_history' AND EXISTS (
    SELECT 1 FROM advance.purchase_order_history approved
    WHERE approved."PurchaseOrderId"=NEW."PurchaseOrderId" AND approved."Action"='Approve'
      AND approved."ToStatus"='Approved' AND approved."RevisionNumber"=NEW."RevisionNumber"
      AND approved."ActorEmployeeId"=NEW."ActorEmployeeId") THEN
    RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_po_approver_issuer_separation',
      MESSAGE='The PO approver cannot issue the same controlled revision.';
  END IF;
  IF NOT (
    (TG_TABLE_NAME='purchase_transaction_status_history' AND (
      (NEW."EntityType" IN ('RFQ','RFQInvitation') AND NEW."ActorRoleCode" IN ('PURCHASE_EXECUTIVE','PURCHASE_MANAGER')) OR
      (NEW."EntityType"='VendorQuotation' AND NEW."Action" IN ('Verify','RejectTechnical','ReserveTechnicalVerification') AND NEW."ActorRoleCode" IN ('TECHNICAL_ENGINEER','TECHNICAL_DIRECTOR')) OR
      (NEW."EntityType"='VendorQuotation' AND NEW."Action" NOT IN ('Verify','RejectTechnical','ReserveTechnicalVerification') AND NEW."ActorRoleCode" IN ('PURCHASE_EXECUTIVE','PURCHASE_MANAGER')) OR
      (NEW."EntityType"='TechnicalVerification' AND NEW."ActorRoleCode" IN ('TECHNICAL_ENGINEER','TECHNICAL_DIRECTOR')) OR
      (NEW."EntityType"='CommercialComparison' AND NEW."Action" NOT IN ('Approve','Reject','RequestRevision') AND NEW."ActorRoleCode"='PURCHASE_MANAGER') OR
      (NEW."EntityType"='PurchaseOrder' AND NEW."Action" NOT IN ('Approve','Reject','RequestRevision') AND NEW."ActorRoleCode"='PURCHASE_MANAGER') OR
      (NEW."EntityType"='MaterialFollowUp' AND ((NEW."Action"='Handoff' AND NEW."ActorRoleCode"='PURCHASE_MANAGER') OR (NEW."Action"<>'Handoff' AND NEW."ActorRoleCode" IN ('STORES_EXECUTIVE','STORES_MANAGER')))) OR
      (NEW."EntityType" IN ('CommercialComparison','PurchaseOrder') AND NEW."Action" IN ('Approve','Reject','RequestRevision') AND
        ((NEW."ActorRoleCode" IN ('PURCHASE_MANAGER','DEPARTMENT_MANAGER') AND EXISTS (SELECT 1 FROM advance.purchase_transaction_approval_policies p WHERE p."OrganizationId"=parent_org AND p."RouteCode"='MANAGER' AND p."IsActive")) OR
         NEW."ActorRoleCode" IN ('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR')))
    )) OR
    (TG_TABLE_NAME='purchase_transaction_approval_history' AND
      ((NEW."ApprovalRoute"='MANAGER' AND NEW."ActorRoleCode" IN ('PURCHASE_MANAGER','DEPARTMENT_MANAGER')) OR
       (NEW."ApprovalRoute"='TECHNICAL_DIRECTOR' AND NEW."ActorRoleCode"='TECHNICAL_DIRECTOR') OR
       (NEW."ApprovalRoute"='MANAGING_DIRECTOR' AND NEW."ActorRoleCode"='MANAGING_DIRECTOR'))) OR
    (TG_TABLE_NAME='purchase_order_history' AND
      ((NEW."Action" IN ('Approve','Reject','RequestRevision') AND
        (((SELECT p."ApprovalRoute" FROM advance.purchase_orders p WHERE p."Id"=NEW."PurchaseOrderId")='MANAGER' AND NEW."ActorRoleCode" IN ('PURCHASE_MANAGER','DEPARTMENT_MANAGER')) OR
         ((SELECT p."ApprovalRoute" FROM advance.purchase_orders p WHERE p."Id"=NEW."PurchaseOrderId")='TECHNICAL_DIRECTOR' AND NEW."ActorRoleCode"='TECHNICAL_DIRECTOR') OR
         ((SELECT p."ApprovalRoute" FROM advance.purchase_orders p WHERE p."Id"=NEW."PurchaseOrderId")='MANAGING_DIRECTOR' AND NEW."ActorRoleCode"='MANAGING_DIRECTOR'))) OR
       (NEW."Action" NOT IN ('Approve','Reject','RequestRevision') AND NEW."ActorRoleCode"='PURCHASE_MANAGER')))
  ) THEN
    RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_history_action_role',MESSAGE='History role is not authorized for the exact controlled action and approval route.';
  END IF;
  PERFORM advance.rev869b_claim_command_context(TG_TABLE_NAME,NEW."Id",
    CASE WHEN TG_TABLE_NAME='purchase_transaction_status_history' THEN NEW."EntityType"
         WHEN TG_TABLE_NAME='purchase_transaction_approval_history' THEN 'CommercialComparison'
         ELSE 'PurchaseOrder' END,
    CASE WHEN TG_TABLE_NAME='purchase_transaction_status_history' THEN NEW."EntityId"
         WHEN TG_TABLE_NAME='purchase_transaction_approval_history' THEN NEW."CommercialComparisonId"
         ELSE NEW."PurchaseOrderId" END,
    NEW."Action",parent_version,NEW."FromStatus",NEW."ToStatus",NEW."CorrelationId",
    CASE WHEN TG_TABLE_NAME='purchase_order_history' THEN NEW."Reason" ELSE NEW."Remarks" END);
  NEW."Version":=parent_version; NEW."CreatedAt":=transaction_timestamp(); NEW."UpdatedAt":=NULL; NEW."UpdatedBy":=NULL;
  RETURN NEW;
END $rev869b$;

ALTER TABLE advance.material_followup_handoffs DROP CONSTRAINT "CK_material_followup_quantity";
ALTER TABLE advance.material_followup_handoffs ADD CONSTRAINT "CK_material_followup_quantity"
  CHECK ("OrderedQuantitySnapshot">0 AND "Status" IN ('PendingFollowUp','InProgress','Completed'));
DROP TRIGGER IF EXISTS trg_rev869b_followup_immutable ON advance.material_followup_handoffs;

CREATE OR REPLACE FUNCTION advance.rev869b_reject_controlled_delete()
RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $rev869b$
BEGIN
  RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_controlled_delete_guard',
    MESSAGE=format('REV869B controlled relation % rejects destructive DELETE.',TG_TABLE_NAME);
END $rev869b$;

CREATE OR REPLACE FUNCTION advance.rev869b_guard_qualification_lifecycle()
RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $rev869b$
DECLARE actor_employee uuid; creator_employee uuid; actor_matches bigint; creator_matches bigint; authorized_roles bigint;
  legacy_normalization boolean;
BEGIN
  IF TG_OP='DELETE' THEN
    RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_qualification_delete_guard',MESSAGE='Vendor qualification lifecycle is retained and cannot be deleted.';
  END IF;
  IF TG_OP='INSERT' THEN
    IF NEW."Version"<>0 OR NEW."VerificationStatus"<>'Pending Approval' OR NEW."ApprovalStatus"<>'Pending Approval' OR
       NEW."VerifiedByEmployeeId" IS NOT NULL OR NEW."ApprovedByEmployeeId" IS NOT NULL OR NOT NEW."IsActive" OR length(trim(coalesce(NEW."CreatedBy",'')))=0 THEN
      RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_qualification_initial_state',MESSAGE='Qualification INSERT must start pending with no verifier or approver identity.';
    END IF;
    IF advance.rev869b_command_context_valid(
         NEW."OrganizationId",nullif(current_setting('advance.rev869b_actor_employee_id',true),'')::uuid,
         current_setting('advance.rev869b_identity_issuer',true),NEW."CreatedBy",
         current_setting('advance.rev869b_actor_role',true)) IS NOT TRUE THEN
      RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_qualification_command_context',
        MESSAGE='Qualification INSERT requires a protected server-issued command context.';
    END IF;
    RETURN NEW;
  END IF;
  SELECT count(*),min(m."EmployeeId") INTO actor_matches,actor_employee FROM advance.employee_identity_mappings m JOIN advance.employees e ON e."Id"=m."EmployeeId"
   WHERE m."OrganizationId"=NEW."OrganizationId" AND m."Subject"=NEW."UpdatedBy" AND m."IsActive" AND e."Status"='Active' AND e."LoginEnabled";
  SELECT count(*),min(m."EmployeeId") INTO creator_matches,creator_employee FROM advance.employee_identity_mappings m
   WHERE m."OrganizationId"=NEW."OrganizationId" AND m."Subject"=OLD."CreatedBy" AND m."IsActive";
  IF actor_matches<>1 THEN
    RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_qualification_actor_binding',MESSAGE='Qualification actor must resolve to exactly one active employee.';
  END IF;
  IF advance.rev869b_command_context_valid(
       NEW."OrganizationId",actor_employee,current_setting('advance.rev869b_identity_issuer',true),
       NEW."UpdatedBy",current_setting('advance.rev869b_actor_role',true)) IS NOT TRUE THEN
    RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_qualification_command_context',
      MESSAGE='Qualification lifecycle requires a protected server-issued command context.';
  END IF;

  legacy_normalization:=OLD."VerificationStatus"='Draft' AND OLD."ApprovalStatus"='Draft' AND
    OLD."VerifiedByEmployeeId" IS NULL AND OLD."ApprovedByEmployeeId" IS NULL;
  IF legacy_normalization THEN
    IF creator_matches<>0 OR NEW."Version"<>OLD."Version"+1 OR length(trim(coalesce(NEW."UpdatedBy",'')))=0 OR
       NEW."CreatedBy" IS DISTINCT FROM NEW."UpdatedBy" OR
       NEW."VerificationStatus"<>'Pending Approval' OR NEW."ApprovalStatus"<>'Pending Approval' OR
       NEW."VerifiedByEmployeeId" IS NOT NULL OR NEW."ApprovedByEmployeeId" IS NOT NULL OR NOT NEW."IsActive" OR
       to_jsonb(NEW)-ARRAY['CreatedBy','VerificationStatus','ApprovalStatus','Version','UpdatedAt','UpdatedBy']
       IS DISTINCT FROM to_jsonb(OLD)-ARRAY['CreatedBy','VerificationStatus','ApprovalStatus','Version','UpdatedAt','UpdatedBy'] THEN
      RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_qualification_legacy_normalization',
        MESSAGE='Only an actorless legacy Draft may be normalized once to Pending Approval by adopting the signed actor as creator.';
    END IF;
    SELECT count(*) INTO authorized_roles FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId"
     WHERE a."EmployeeId"=actor_employee AND r."IsActive" AND lower(r."Code") IN ('accounts_head','technical_director')
       AND a."ApprovalStatus" IN ('Approved','SeedApproved') AND a."EffectiveFrom"<=statement_timestamp()::date
       AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=statement_timestamp()::date);
    IF authorized_roles=0 THEN
      RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_qualification_legacy_normalizer_binding',
        MESSAGE='Legacy qualification normalization requires an authorized signed employee.';
    END IF;
  ELSE
    IF NEW."Version"<>OLD."Version"+1 OR length(trim(coalesce(NEW."UpdatedBy",'')))=0 OR
       to_jsonb(NEW)-ARRAY['VerificationStatus','VerifiedByEmployeeId','ApprovalStatus','ApprovedByEmployeeId','IsActive','Version','UpdatedAt','UpdatedBy']
       IS DISTINCT FROM to_jsonb(OLD)-ARRAY['VerificationStatus','VerifiedByEmployeeId','ApprovalStatus','ApprovedByEmployeeId','IsActive','Version','UpdatedAt','UpdatedBy'] THEN
      RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_qualification_lifecycle_allowlist',MESSAGE='Qualification lifecycle permits only exact versioned verifier/approver changes.';
    END IF;
    IF creator_matches<>1 OR actor_employee=creator_employee THEN
      RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_qualification_actor_binding',MESSAGE='Qualification actor must differ from the uniquely mapped creator.';
    END IF;
    IF OLD."VerificationStatus"='Pending Approval' AND OLD."ApprovalStatus"='Pending Approval' AND
     NEW."VerificationStatus"='Verified' AND NEW."ApprovalStatus"='Pending Approval' THEN
      SELECT count(*) INTO authorized_roles FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId"
       WHERE a."EmployeeId"=actor_employee AND r."IsActive" AND lower(r."Code") IN ('accounts_head','technical_director')
         AND a."ApprovalStatus" IN ('Approved','SeedApproved') AND a."EffectiveFrom"<=statement_timestamp()::date
         AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=statement_timestamp()::date);
      IF OLD."VerifiedByEmployeeId" IS NOT NULL OR NEW."VerifiedByEmployeeId" IS DISTINCT FROM actor_employee OR
          NEW."ApprovedByEmployeeId" IS DISTINCT FROM OLD."ApprovedByEmployeeId" OR authorized_roles=0 OR NOT NEW."IsActive" THEN
        RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_qualification_verifier_binding',MESSAGE='Qualification verification requires the exact independent employee.';
      END IF;
    ELSIF OLD."VerificationStatus"='Verified' AND OLD."ApprovalStatus"='Pending Approval' AND
        NEW."VerificationStatus"='Verified' AND NEW."ApprovalStatus"='Approved' THEN
      SELECT count(*) INTO authorized_roles FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId"
       WHERE a."EmployeeId"=actor_employee AND r."IsActive" AND lower(r."Code") IN ('managing_director','technical_director')
         AND a."ApprovalStatus" IN ('Approved','SeedApproved') AND a."EffectiveFrom"<=statement_timestamp()::date
         AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=statement_timestamp()::date);
       IF OLD."VerifiedByEmployeeId" IS NULL OR NEW."VerifiedByEmployeeId" IS DISTINCT FROM OLD."VerifiedByEmployeeId" OR
          NEW."ApprovedByEmployeeId" IS DISTINCT FROM actor_employee OR actor_employee=OLD."VerifiedByEmployeeId" OR authorized_roles=0 OR NOT NEW."IsActive" THEN
         RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_qualification_approver_binding',MESSAGE='Qualification approval requires an employee distinct from creator and verifier.';
       END IF;
    ELSIF OLD."ApprovalStatus"='Pending Approval' AND NEW."ApprovalStatus"='Rejected' AND
        NEW."VerificationStatus" IS NOT DISTINCT FROM OLD."VerificationStatus" THEN
      SELECT count(*) INTO authorized_roles FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId"
       WHERE a."EmployeeId"=actor_employee AND r."IsActive" AND lower(r."Code") IN ('managing_director','technical_director')
         AND a."ApprovalStatus" IN ('Approved','SeedApproved') AND a."EffectiveFrom"<=statement_timestamp()::date
         AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=statement_timestamp()::date);
      IF NEW."ApprovedByEmployeeId" IS DISTINCT FROM actor_employee OR NEW."IsActive" OR authorized_roles=0 OR
         NEW."VerifiedByEmployeeId" IS DISTINCT FROM OLD."VerifiedByEmployeeId" OR
         (OLD."VerifiedByEmployeeId" IS NOT NULL AND actor_employee=OLD."VerifiedByEmployeeId") THEN
        RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_qualification_rejector_binding',MESSAGE='Qualification rejection requires an independent approval-tier employee and deactivation.';
      END IF;
    ELSIF OLD."VerificationStatus" IN ('Verified','Approved') AND OLD."ApprovalStatus"='Approved' AND OLD."IsActive" AND
        NEW."VerificationStatus" IS NOT DISTINCT FROM OLD."VerificationStatus" AND NEW."ApprovalStatus"='Revision Requested' THEN
      SELECT count(*) INTO authorized_roles FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId"
       WHERE a."EmployeeId"=actor_employee AND r."IsActive" AND lower(r."Code") IN ('managing_director','technical_director')
         AND a."ApprovalStatus" IN ('Approved','SeedApproved') AND a."EffectiveFrom"<=statement_timestamp()::date
         AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=statement_timestamp()::date);
      IF NEW."VerifiedByEmployeeId" IS DISTINCT FROM OLD."VerifiedByEmployeeId" OR
         NEW."ApprovedByEmployeeId" IS DISTINCT FROM OLD."ApprovedByEmployeeId" OR NEW."IsActive" OR
         actor_employee=OLD."VerifiedByEmployeeId" OR authorized_roles=0 THEN
        RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_qualification_correction_binding',MESSAGE='Approved qualification correction requires an independent approval-tier employee and deactivation.';
      END IF;
    ELSE
      RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_qualification_transition',MESSAGE='Qualification permits only verify, approve, reject, or approved-record correction transitions.';
    END IF;
  END IF;
  NEW."UpdatedAt":=statement_timestamp();
  RETURN NEW;
END $rev869b$;

CREATE OR REPLACE FUNCTION advance.rev869b_guard_qualification_history_insert()
RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $rev869b$
DECLARE actor_employee uuid;
BEGIN
  IF NEW."EntityType"<>'VendorQualification' THEN RETURN NEW; END IF;
  actor_employee:=nullif(current_setting('advance.rev869b_actor_employee_id',true),'')::uuid;
  IF NEW."ActorLoginId" IS DISTINCT FROM current_setting('advance.rev869b_identity_subject',true) OR
     NEW."CreatedBy" IS DISTINCT FROM current_setting('advance.rev869b_identity_subject',true) OR
     NEW."ActorRoleCode" IS DISTINCT FROM current_setting('advance.rev869b_actor_role',true) OR
     advance.rev869b_command_context_valid(NEW."OrganizationId",actor_employee,
       current_setting('advance.rev869b_identity_issuer',true),
       current_setting('advance.rev869b_identity_subject',true),
       current_setting('advance.rev869b_actor_role',true)) IS NOT TRUE THEN
    RAISE EXCEPTION USING ERRCODE='42501',SCHEMA='advance',TABLE='controlled_configuration_histories',
      CONSTRAINT='rev869b_qualification_history_actor_binding',
      MESSAGE='Qualification history must use the exact signed command principal.';
  END IF;
  NEW."CreatedAt":=transaction_timestamp(); NEW."UpdatedAt":=NULL; NEW."UpdatedBy":=NULL;
  RETURN NEW;
END $rev869b$;

DROP TRIGGER IF EXISTS trg_rev869b_qualification_history_insert_guard ON advance.controlled_configuration_histories;
CREATE TRIGGER trg_rev869b_qualification_history_insert_guard
  BEFORE INSERT ON advance.controlled_configuration_histories
  FOR EACH ROW EXECUTE FUNCTION advance.rev869b_guard_qualification_history_insert();

CREATE OR REPLACE FUNCTION advance.rev869b_require_qualification_history()
RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $rev869b$
DECLARE expected_action text; expected_employee uuid; expected_creator text; expected_correlation text;
  expected_history_id uuid; history_matches bigint; history_remarks text;
BEGIN
  expected_action:=CASE WHEN TG_OP='INSERT' THEN 'Create' WHEN OLD."VerificationStatus"='Draft' THEN 'Normalize'
    WHEN NEW."ApprovalStatus"='Rejected' THEN 'Reject' WHEN NEW."ApprovalStatus"='Revision Requested' THEN 'RequestCorrection'
    WHEN NEW."ApprovalStatus"='Approved' THEN 'Approve' ELSE 'Verify' END;
  expected_employee:=nullif(current_setting('advance.rev869b_actor_employee_id',true),'')::uuid;
  expected_creator:=CASE WHEN TG_OP='INSERT' THEN NEW."CreatedBy" ELSE NEW."UpdatedBy" END;
  expected_correlation:=format('REV869B|QUALIFICATION|%s|%s|%s',replace(NEW."Id"::text,'-',''),NEW."Version",upper(expected_action));
  SELECT count(*),min(h."Id"::text)::uuid,min(h."Remarks")
    INTO history_matches,expected_history_id,history_remarks
  FROM advance.controlled_configuration_histories h
    JOIN advance.employee_identity_mappings m ON m."OrganizationId"=NEW."OrganizationId" AND m."Subject"=h."ActorLoginId" AND m."EmployeeId"=expected_employee AND m."IsActive"
    WHERE h."EntityType"='VendorQualification' AND h."EntityId"=NEW."Id" AND h."OrganizationId"=NEW."OrganizationId"
      AND h."Action"=expected_action AND h."Version"=NEW."Version" AND h."CreatedBy"=expected_creator
      AND h."CorrelationId"=expected_correlation AND h."ActorRoleCode"=current_setting('advance.rev869b_actor_role',true)
      AND h."CreatedAt"=transaction_timestamp() AND length(trim(h."Remarks"))>0
      AND ((expected_action='Create' AND h."BeforeJson" IS NULL AND h."AfterJson"->>'VerificationStatus'='Pending Approval' AND h."AfterJson"->>'ApprovalStatus'='Pending Approval' AND (h."AfterJson"->>'Version')::bigint=0) OR
           (expected_action='Normalize' AND h."BeforeJson"->>'VerificationStatus'='Draft' AND h."BeforeJson"->>'ApprovalStatus'='Draft' AND h."AfterJson"->>'VerificationStatus'='Pending Approval' AND h."AfterJson"->>'ApprovalStatus'='Pending Approval' AND h."AfterJson"->>'CreatedBy'=h."ActorLoginId" AND (h."BeforeJson"->>'Version')::bigint=OLD."Version" AND (h."AfterJson"->>'Version')::bigint=NEW."Version") OR
            (expected_action='Verify' AND h."BeforeJson"->>'VerificationStatus'='Pending Approval' AND h."BeforeJson"->>'ApprovalStatus'='Pending Approval' AND h."AfterJson"->>'VerificationStatus'='Verified' AND h."AfterJson"->>'ApprovalStatus'='Pending Approval' AND (h."BeforeJson"->>'Version')::bigint=OLD."Version" AND (h."AfterJson"->>'Version')::bigint=NEW."Version") OR
           (expected_action='Approve' AND h."BeforeJson"->>'VerificationStatus'='Verified' AND h."BeforeJson"->>'ApprovalStatus'='Pending Approval' AND h."AfterJson"->>'VerificationStatus'='Verified' AND h."AfterJson"->>'ApprovalStatus'='Approved' AND (h."BeforeJson"->>'Version')::bigint=OLD."Version" AND (h."AfterJson"->>'Version')::bigint=NEW."Version") OR
           (expected_action='Reject' AND h."BeforeJson"->>'ApprovalStatus'='Pending Approval' AND h."AfterJson"->>'ApprovalStatus'='Rejected' AND (h."AfterJson"->>'IsActive')::boolean IS FALSE AND (h."BeforeJson"->>'Version')::bigint=OLD."Version" AND (h."AfterJson"->>'Version')::bigint=NEW."Version") OR
           (expected_action='RequestCorrection' AND h."BeforeJson"->>'ApprovalStatus'='Approved' AND h."AfterJson"->>'ApprovalStatus'='Revision Requested' AND (h."AfterJson"->>'IsActive')::boolean IS FALSE AND (h."BeforeJson"->>'Version')::bigint=OLD."Version" AND (h."AfterJson"->>'Version')::bigint=NEW."Version"))
      AND h.xmin::text::bigint=txid_current();
  IF history_matches<>1 THEN
    RAISE EXCEPTION USING ERRCODE='23514',SCHEMA='advance',TABLE='controlled_configuration_histories',
      CONSTRAINT='rev869b_qualification_requires_history',
      MESSAGE='Qualification lifecycle requires one exact same-transaction immutable history.';
  END IF;
  PERFORM advance.rev869b_claim_command_context(
    'qualification_history',expected_history_id,'VendorQualification',NEW."Id",expected_action,
    CASE WHEN TG_OP='INSERT' THEN 0 ELSE OLD."Version" END,
    CASE WHEN TG_OP='INSERT' THEN NULL WHEN expected_action IN ('Approve','Reject','RequestCorrection') THEN OLD."ApprovalStatus" ELSE OLD."VerificationStatus" END,
    CASE WHEN expected_action IN ('Create','Normalize') THEN 'Pending Approval' WHEN expected_action='Verify' THEN 'Verified' WHEN expected_action='Reject' THEN 'Rejected' WHEN expected_action='RequestCorrection' THEN 'Revision Requested' ELSE 'Approved' END,
    expected_correlation,history_remarks);
  RETURN NULL;
END $rev869b$;

CREATE OR REPLACE FUNCTION advance.rev869b_guard_explicit_mutation()
RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $rev869b$
DECLARE parent_status text;
BEGIN
  IF current_setting('advance.rev869b_actor_employee_id',true) IS NULL OR
     current_setting('advance.rev869b_actor_login',true) IS NULL OR
     current_setting('advance.rev869b_actor_role',true) IS NULL OR
     current_setting('advance.rev869b_organization',true) IS NULL OR
     advance.rev869b_command_context_valid(
       current_setting('advance.rev869b_organization',true),
       nullif(current_setting('advance.rev869b_actor_employee_id',true),'')::uuid,
       current_setting('advance.rev869b_identity_issuer',true),
       current_setting('advance.rev869b_identity_subject',true),
       current_setting('advance.rev869b_actor_role',true)) IS NOT TRUE THEN
    RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_command_context_required',MESSAGE='REV869B mutation requires an authenticated transaction-local command context.';
  END IF;
  IF TG_OP='INSERT' THEN
    IF NEW."CreatedBy" IS DISTINCT FROM current_setting('advance.rev869b_actor_login',true) THEN
      RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_command_actor_binding',MESSAGE='Controlled INSERT actor must match command context.';
    END IF;
    IF NEW."Version"<>0 THEN RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_initial_version_zero',MESSAGE=format('%s INSERT requires version zero.',TG_TABLE_NAME); END IF;
    IF TG_TABLE_NAME IN ('request_for_quotations','rfq_vendor_invitations','vendor_quotations','commercial_comparisons','purchase_orders') AND
       (length(trim(coalesce(NEW."TransitionCorrelationId",'')))=0 OR NEW."TransitionCorrelationId" IS DISTINCT FROM NEW."IdempotencyKey") THEN
      RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_initial_command_correlation',MESSAGE='Controlled aggregate INSERT must bind its initial command fingerprint.';
    ELSIF TG_TABLE_NAME IN ('quotation_technical_verifications','material_followup_handoffs') AND length(trim(coalesce(NEW."CorrelationId",'')))=0 THEN
      RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_initial_command_correlation',MESSAGE='Controlled event INSERT must bind its command fingerprint.';
    END IF;
    IF TG_TABLE_NAME='purchase_transaction_approval_policies' THEN
      IF length(trim(coalesce(NEW."CreatedBy",'')))=0 OR NEW."EffectiveTo" IS NOT NULL AND NEW."EffectiveTo"<NEW."EffectiveFrom" THEN
        RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_policy_insert_contract',MESSAGE='Approval-policy INSERT requires an actor and valid effective dates.';
      END IF;
      NEW."CreatedAt":=statement_timestamp();
    END IF;
    RETURN NEW;
  END IF;
  IF NEW."Version"<>OLD."Version"+1 THEN RAISE EXCEPTION USING ERRCODE='40001',CONSTRAINT='rev869b_exact_version_increment',MESSAGE=format('%s UPDATE requires exact version +1.',TG_TABLE_NAME); END IF;
  IF NEW."UpdatedBy" IS DISTINCT FROM current_setting('advance.rev869b_actor_login',true) THEN
    RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_command_actor_binding',MESSAGE='Controlled UPDATE actor must match command context.';
  END IF;
  IF TG_TABLE_NAME IN ('request_for_quotations','rfq_vendor_invitations','vendor_quotations','commercial_comparisons','purchase_orders') AND
     (length(trim(coalesce(NEW."TransitionCorrelationId",'')))=0 OR NEW."TransitionCorrelationId" IS NOT DISTINCT FROM OLD."TransitionCorrelationId") THEN
    RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_transition_command_correlation',MESSAGE='Every aggregate UPDATE requires a new exact command correlation.';
  END IF;
  IF TG_TABLE_NAME='request_for_quotations' AND
     to_jsonb(NEW)-ARRAY['Status','IssuedAt','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['Status','IssuedAt','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] THEN
    RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_rfq_update_allowlist',MESSAGE='RFQ update altered a field outside its exact lifecycle allowlist.';
  ELSIF TG_TABLE_NAME='rfq_vendor_invitations' AND
     to_jsonb(NEW)-ARRAY['Status','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['Status','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] THEN
    RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_invitation_update_allowlist',MESSAGE='RFQ invitation update altered qualification, provenance or another protected field.';
  ELSIF TG_TABLE_NAME='vendor_quotations' AND
     to_jsonb(NEW)-ARRAY['Status','IsCurrentRevision','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['Status','IsCurrentRevision','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] THEN
    RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_quotation_update_allowlist',MESSAGE='Quotation update altered immutable commercial, revision or provenance facts.';
  ELSIF TG_TABLE_NAME='commercial_comparisons' AND NEW."Status"='PendingApproval' AND OLD."Status" IN ('Draft','RevisionRequested') AND
     to_jsonb(NEW)-ARRAY['RecommendedVendorQuotationId','SelectedVendorId','TotalPayableValue','ApprovalRoute','SingleSourceJustification','RecommendationRemarks','Status','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['RecommendedVendorQuotationId','SelectedVendorId','TotalPayableValue','ApprovalRoute','SingleSourceJustification','RecommendationRemarks','Status','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] THEN
    RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_comparison_recommendation_allowlist',MESSAGE='Comparison recommendation altered a field outside the Draft/RevisionRequested correction boundary.';
  ELSIF TG_TABLE_NAME='commercial_comparisons' AND NOT (NEW."Status"='PendingApproval' AND OLD."Status" IN ('Draft','RevisionRequested')) AND
     to_jsonb(NEW)-ARRAY['Status','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['Status','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] THEN
    RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_comparison_transition_allowlist',MESSAGE='Comparison transition altered protected commercial, selection or provenance fields.';
  ELSIF TG_TABLE_NAME='purchase_orders' AND NEW."Status"='Issued' AND
     to_jsonb(NEW)-ARRAY['Status','IssuedAt','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['Status','IssuedAt','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] THEN
    RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_po_issue_allowlist',MESSAGE='PO issue altered a protected snapshot or lifecycle field.';
  ELSIF TG_TABLE_NAME='purchase_orders' AND NEW."Status"='Cancelled' AND
     to_jsonb(NEW)-ARRAY['Status','CancelledAt','CancellationReason','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['Status','CancelledAt','CancellationReason','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] THEN
    RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_po_cancel_allowlist',MESSAGE='PO cancellation altered a protected snapshot or lifecycle field.';
  ELSIF TG_TABLE_NAME='purchase_orders' AND NEW."Status" IN ('Approved','Rejected','Superseded') AND
     to_jsonb(NEW)-ARRAY['Status','IsCurrentVersion','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['Status','IsCurrentVersion','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] THEN
    RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_po_approval_allowlist',MESSAGE='PO approval/rejection/supersession altered a protected snapshot or lifecycle field.';
  ELSIF TG_TABLE_NAME='purchase_orders' AND NEW."Status" NOT IN ('Issued','Cancelled','Approved','Rejected','Superseded') AND
     to_jsonb(NEW)-ARRAY['Status','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['Status','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] THEN
    RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_po_transition_allowlist',MESSAGE='PO transition altered protected terms, snapshots, current-version or provenance fields.';
  END IF;
  IF TG_TABLE_NAME IN ('request_for_quotations','rfq_vendor_invitations','vendor_quotations','commercial_comparisons','purchase_orders') AND NEW."Status"=OLD."Status" AND
     to_jsonb(NEW)-ARRAY['TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] THEN
    RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_same_status_protected_fields',MESSAGE='Same-status reservation cannot alter controlled fields.';
  ELSIF TG_TABLE_NAME='commercial_comparison_lines' THEN
    SELECT c."Status" INTO STRICT parent_status FROM advance.commercial_comparisons c WHERE c."Id"=NEW."CommercialComparisonId";
    IF parent_status NOT IN ('Draft','RevisionRequested') OR
       to_jsonb(NEW)-ARRAY['IsRecommended','RecommendationReason','TotalPayableValue','CommercialSnapshotJson','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['IsRecommended','RecommendationReason','TotalPayableValue','CommercialSnapshotJson','Version','UpdatedAt','UpdatedBy'] THEN
      RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_comparison_line_editable_boundary',MESSAGE='Comparison line correction requires its exact editable parent and immutable provenance.';
    END IF;
  ELSIF TG_TABLE_NAME='material_followup_handoffs' THEN
    IF NEW."CorrelationId" IS NOT DISTINCT FROM OLD."CorrelationId" OR length(trim(coalesce(NEW."CorrelationId",'')))=0 OR
       (OLD."Status",NEW."Status") NOT IN (('PendingFollowUp','InProgress'),('InProgress','Completed')) OR
       (NEW."PurchaseOrderId",NEW."PurchaseOrderLineId",NEW."HandoffNumber",NEW."OrderedQuantitySnapshot",NEW."HandoffAt") IS DISTINCT FROM (OLD."PurchaseOrderId",OLD."PurchaseOrderLineId",OLD."HandoffNumber",OLD."OrderedQuantitySnapshot",OLD."HandoffAt") OR length(trim(coalesce(NEW."UpdatedBy",'')))=0 THEN
      RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_followup_transition',MESSAGE='Material Follow-up permits only PendingFollowUp to InProgress to Completed.';
    END IF;
  ELSIF TG_TABLE_NAME='purchase_transaction_approval_policies' THEN
    IF (NEW."OrganizationId",NEW."RouteCode",NEW."MinimumAmount",NEW."MaximumAmount",NEW."ApproverRoleCode",NEW."EffectiveFrom") IS DISTINCT FROM (OLD."OrganizationId",OLD."RouteCode",OLD."MinimumAmount",OLD."MaximumAmount",OLD."ApproverRoleCode",OLD."EffectiveFrom") OR NEW."IsActive"=OLD."IsActive" OR length(trim(coalesce(NEW."UpdatedBy",'')))=0 THEN
      RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_policy_controlled_lifecycle',MESSAGE='Approval policy UPDATE permits only controlled activation/deactivation.';
    END IF;
    NEW."UpdatedAt":=statement_timestamp();
  END IF;
  RETURN NEW;
END $rev869b$;


CREATE OR REPLACE FUNCTION advance.rev869b_write_policy_history() RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $rev869b$
DECLARE employee uuid; matches bigint; actor_role text; actor text:=coalesce(NEW."UpdatedBy",NEW."CreatedBy");
BEGIN
  SELECT count(*),min(m."EmployeeId") INTO matches,employee FROM advance.employee_identity_mappings m JOIN advance.employees e ON e."Id"=m."EmployeeId"
   WHERE m."Subject"=actor AND m."OrganizationId"=NEW."OrganizationId" AND m."IsActive" AND e."Status"='Active' AND e."LoginEnabled";
  IF matches<>1 THEN RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_policy_actor_identity',MESSAGE='Approval-policy actor must resolve to one active organization identity.'; END IF;
  SELECT min(r."Code") INTO actor_role FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId" AND r."IsActive" WHERE a."EmployeeId"=employee AND r."Code" IN ('PURCHASE_MANAGER','TECHNICAL_DIRECTOR','MANAGING_DIRECTOR') AND a."ApprovalStatus" IN ('Approved','SeedApproved');
  IF actor_role IS NULL THEN RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_policy_actor_role',MESSAGE='Approval-policy lifecycle requires an authorized active role.'; END IF;
  INSERT INTO advance.controlled_configuration_histories ("Id","OrganizationId","EntityType","EntityId","Action","BeforeJson","AfterJson","ActorLoginId","ActorRoleCode","Remarks","CorrelationId","CreatedAt","CreatedBy","Version")
  VALUES(gen_random_uuid(),NEW."OrganizationId",'PurchaseTransactionApprovalPolicy',NEW."Id",CASE WHEN TG_OP='INSERT' THEN 'Create' WHEN NEW."IsActive" THEN 'Activate' ELSE 'Deactivate' END,CASE WHEN TG_OP='INSERT' THEN NULL::jsonb ELSE to_jsonb(OLD) END,to_jsonb(NEW),actor,actor_role,'Database-bound approval-policy change',format('REV869B|POLICY|%s|%s',NEW."Id",NEW."Version"),statement_timestamp(),actor,NEW."Version"); RETURN NEW;
END $rev869b$;

CREATE OR REPLACE FUNCTION advance.rev869b_require_bound_history()
RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $rev869b$
DECLARE entity_type text; old_status text; expected_action text; actor text; parent_correlation text; specialized bigint;
BEGIN
  IF TG_TABLE_NAME='quotation_technical_verifications' THEN
    actor:=coalesce(NEW."UpdatedBy",NEW."CreatedBy");
    IF NOT EXISTS (SELECT 1 FROM advance.purchase_transaction_status_history h WHERE h."EntityType"='TechnicalVerification' AND h."EntityId"=NEW."Id"
      AND h."FromStatus" IS NULL AND h."ToStatus"=NEW."ComplianceStatus" AND h."Action"='Verify' AND h."ActorLoginId"=actor
      AND h."CorrelationId"=NEW."CorrelationId" AND h."Version"=NEW."Version" AND h.xmin::text::bigint=txid_current()) THEN
      RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_technical_verification_requires_history',MESSAGE='Technical verification requires exact same-command status history.';
    END IF;
    RETURN NULL;
  END IF;
  IF TG_OP='INSERT' AND (
    (TG_TABLE_NAME='request_for_quotations' AND EXISTS (SELECT 1 FROM advance.request_for_quotations p WHERE p."Id"=NEW."Id" AND (p."Status",p."Version") IS DISTINCT FROM (NEW."Status",NEW."Version"))) OR
    (TG_TABLE_NAME='rfq_vendor_invitations' AND EXISTS (SELECT 1 FROM advance.rfq_vendor_invitations p WHERE p."Id"=NEW."Id" AND (p."Status",p."Version") IS DISTINCT FROM (NEW."Status",NEW."Version"))) OR
    (TG_TABLE_NAME='vendor_quotations' AND EXISTS (SELECT 1 FROM advance.vendor_quotations p WHERE p."Id"=NEW."Id" AND (p."Status",p."Version") IS DISTINCT FROM (NEW."Status",NEW."Version"))) OR
    (TG_TABLE_NAME='commercial_comparisons' AND EXISTS (SELECT 1 FROM advance.commercial_comparisons p WHERE p."Id"=NEW."Id" AND (p."Status",p."Version") IS DISTINCT FROM (NEW."Status",NEW."Version"))) OR
    (TG_TABLE_NAME='purchase_orders' AND EXISTS (SELECT 1 FROM advance.purchase_orders p WHERE p."Id"=NEW."Id" AND (p."Status",p."Version") IS DISTINCT FROM (NEW."Status",NEW."Version"))) OR
    (TG_TABLE_NAME='material_followup_handoffs' AND EXISTS (SELECT 1 FROM advance.material_followup_handoffs p WHERE p."Id"=NEW."Id" AND (p."Status",p."Version") IS DISTINCT FROM (NEW."Status",NEW."Version")))
  ) THEN RETURN NULL; END IF;
  IF NOT EXISTS (SELECT 1 FROM advance.purchase_transaction_status_history h WHERE h."EntityId"=NEW."Id" AND h."ToStatus"=NEW."Status" AND h."Version"=NEW."Version" AND h.xmin::text::bigint=txid_current()) THEN
    RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_transition_requires_history',MESSAGE='Every controlled parent transition requires same-transaction status history.';
  END IF;
  actor:=coalesce(NEW."UpdatedBy",NEW."CreatedBy"); old_status:=CASE WHEN TG_OP='UPDATE' THEN OLD."Status" ELSE NULL END;
  IF TG_OP='UPDATE' AND NEW."Status" IS NOT DISTINCT FROM OLD."Status" THEN
    parent_correlation:=NEW."TransitionCorrelationId";
    IF TG_TABLE_NAME='request_for_quotations' THEN entity_type:='RFQ';
      SELECT h."Action" INTO expected_action FROM advance.purchase_transaction_status_history h WHERE h."EntityType"=entity_type AND h."EntityId"=NEW."Id" AND h."Action" IN ('ReserveInvitation','ReserveComparison') AND h."CorrelationId"=NEW."TransitionCorrelationId" AND h.xmin::text::bigint=txid_current();
    ELSIF TG_TABLE_NAME='rfq_vendor_invitations' THEN entity_type:='RFQInvitation'; expected_action:='ReserveQuotation';
    ELSIF TG_TABLE_NAME='vendor_quotations' THEN entity_type:='VendorQuotation'; expected_action:='ReserveTechnicalVerification';
    ELSIF TG_TABLE_NAME='commercial_comparisons' THEN entity_type:='CommercialComparison'; expected_action:='ReservePurchaseOrder';
    ELSIF TG_TABLE_NAME='purchase_orders' THEN entity_type:='PurchaseOrder'; expected_action:='ReserveAmendment';
    ELSE RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_same_status_update_forbidden',MESSAGE='This aggregate has no controlled same-status mutation.';
    END IF;
    IF expected_action IS NULL THEN RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_same_status_history_action',MESSAGE='Same-status mutation requires its exact reservation action.'; END IF;
  ELSIF TG_TABLE_NAME='request_for_quotations' THEN entity_type:='RFQ'; parent_correlation:=NEW."TransitionCorrelationId"; expected_action:=CASE WHEN TG_OP='INSERT' THEN 'Create' WHEN NEW."Status"='Issued' THEN 'Issue' WHEN NEW."Status"='Closed' THEN 'Close' WHEN NEW."Status"='Cancelled' THEN 'Cancel' ELSE 'Transition' END;
  ELSIF TG_TABLE_NAME='rfq_vendor_invitations' THEN entity_type:='RFQInvitation'; parent_correlation:=NEW."TransitionCorrelationId"; expected_action:=CASE WHEN TG_OP='INSERT' THEN 'InviteVendor' WHEN NEW."Status"='Submitted' THEN 'Submit' WHEN NEW."Status"='Withdrawn' THEN 'Withdraw' WHEN NEW."Status"='Cancelled' THEN 'Cancel' ELSE 'Transition' END;
  ELSIF TG_TABLE_NAME='vendor_quotations' THEN entity_type:='VendorQuotation'; parent_correlation:=NEW."TransitionCorrelationId"; expected_action:=CASE WHEN TG_OP='INSERT' AND NEW."Status"='Draft' THEN 'Create' WHEN NEW."Status"='Submitted' THEN CASE WHEN NEW."PreviousRevisionId" IS NULL THEN 'Submit' ELSE 'Revise' END WHEN NEW."Status"='TechnicallyCompliant' THEN 'Verify' WHEN NEW."Status"='TechnicallyRejected' THEN 'RejectTechnical' WHEN NEW."Status"='Superseded' THEN 'Supersede' WHEN NEW."Status"='Withdrawn' THEN 'Withdraw' WHEN NEW."Status"='Rejected' THEN 'Reject' ELSE 'Transition' END;
  ELSIF TG_TABLE_NAME='commercial_comparisons' THEN entity_type:='CommercialComparison'; parent_correlation:=NEW."TransitionCorrelationId"; expected_action:=CASE WHEN TG_OP='INSERT' THEN 'Create' WHEN NEW."Status"='PendingApproval' AND OLD."Status"='Draft' THEN 'Recommend' WHEN NEW."Status"='PendingApproval' THEN 'Resubmit' WHEN NEW."Status"='Approved' THEN 'Approve' WHEN NEW."Status"='Rejected' THEN 'Reject' WHEN NEW."Status"='RevisionRequested' THEN 'RequestRevision' WHEN NEW."Status"='Cancelled' THEN 'Cancel' ELSE 'Transition' END;
  ELSIF TG_TABLE_NAME='purchase_orders' THEN
    entity_type:='PurchaseOrder';
    parent_correlation:=NEW."TransitionCorrelationId";
    IF TG_OP='INSERT' AND NEW."PreviousVersionId" IS NOT NULL THEN SELECT p."Status" INTO STRICT old_status FROM advance.purchase_orders p WHERE p."Id"=NEW."PreviousVersionId"; END IF;
    expected_action:=CASE WHEN TG_OP='INSERT' AND NEW."Status"='RevisionDraft' THEN 'ReviseRejected' WHEN TG_OP='INSERT' AND NEW."PreviousVersionId" IS NOT NULL THEN 'Amend' WHEN TG_OP='INSERT' THEN 'Create' WHEN NEW."Status"='PendingApproval' THEN 'Submit' WHEN NEW."Status"='Resubmitted' THEN 'ResubmitRejected' WHEN NEW."Status"='Approved' THEN 'Approve' WHEN NEW."Status"='Rejected' THEN 'Reject' WHEN NEW."Status"='Issued' THEN 'Issue' WHEN NEW."Status"='Superseded' THEN 'Supersede' WHEN NEW."Status"='Cancelled' THEN 'Cancel' ELSE 'Transition' END;
  ELSE entity_type:='MaterialFollowUp'; parent_correlation:=NEW."CorrelationId"; expected_action:=CASE WHEN NEW."Status"='InProgress' THEN 'StartFollowUp' WHEN NEW."Status"='Completed' THEN 'CompleteFollowUp' ELSE 'Handoff' END;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM advance.purchase_transaction_status_history h WHERE h."EntityType"=entity_type AND h."EntityId"=NEW."Id" AND h."FromStatus" IS NOT DISTINCT FROM old_status AND h."ToStatus"=NEW."Status" AND h."Action"=expected_action AND h."ActorLoginId"=actor AND h."CorrelationId"=parent_correlation AND h."Version"=NEW."Version" AND h.xmin::text::bigint=txid_current()) THEN
    RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_transition_history_exactness',MESSAGE='Transition history from/to/action/actor/version does not match its parent mutation.';
  END IF;
  IF TG_TABLE_NAME='commercial_comparisons' AND expected_action IN ('Approve','Reject','RequestRevision','Resubmit') THEN
    SELECT count(*) INTO specialized FROM advance.purchase_transaction_approval_history h WHERE h."CommercialComparisonId"=NEW."Id" AND h."FromStatus" IS NOT DISTINCT FROM old_status AND h."ToStatus"=NEW."Status" AND h."Action"=expected_action AND h."ActorLoginId"=actor AND h."Version"=NEW."Version" AND h.xmin::text::bigint=txid_current();
    IF specialized<>1 THEN RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_comparison_transition_requires_approval_history',MESSAGE='Comparison approval transition requires one exact same-transaction approval history.'; END IF;
  ELSIF TG_TABLE_NAME='purchase_orders' THEN
    SELECT count(*) INTO specialized FROM advance.purchase_order_history h WHERE h."PurchaseOrderId"=NEW."Id" AND h."FromStatus" IS NOT DISTINCT FROM coalesce(old_status,'') AND h."ToStatus"=NEW."Status" AND h."Action"=expected_action AND h."ActorLoginId"=actor AND h."Version"=NEW."Version" AND h.xmin::text::bigint=txid_current();
    IF specialized<>1 THEN RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_po_transition_requires_po_history',MESSAGE='Purchase-order transition requires one exact same-transaction PO history.'; END IF;
  END IF;
  RETURN NULL;
END $rev869b$;

CREATE TRIGGER trg_rev869b_explicit_rfq_mutation BEFORE INSERT OR UPDATE ON advance.request_for_quotations FOR EACH ROW EXECUTE FUNCTION advance.rev869b_guard_explicit_mutation();
CREATE TRIGGER trg_rev869b_explicit_invitation_mutation BEFORE INSERT OR UPDATE ON advance.rfq_vendor_invitations FOR EACH ROW EXECUTE FUNCTION advance.rev869b_guard_explicit_mutation();
CREATE TRIGGER trg_rev869b_explicit_quotation_mutation BEFORE INSERT OR UPDATE ON advance.vendor_quotations FOR EACH ROW EXECUTE FUNCTION advance.rev869b_guard_explicit_mutation();
CREATE TRIGGER trg_rev869b_explicit_comparison_mutation BEFORE INSERT OR UPDATE ON advance.commercial_comparisons FOR EACH ROW EXECUTE FUNCTION advance.rev869b_guard_explicit_mutation();
CREATE TRIGGER trg_rev869b_explicit_comparison_line_mutation BEFORE INSERT OR UPDATE ON advance.commercial_comparison_lines FOR EACH ROW EXECUTE FUNCTION advance.rev869b_guard_explicit_mutation();
CREATE TRIGGER trg_rev869b_explicit_po_mutation BEFORE INSERT OR UPDATE ON advance.purchase_orders FOR EACH ROW EXECUTE FUNCTION advance.rev869b_guard_explicit_mutation();
CREATE TRIGGER trg_rev869b_explicit_followup_mutation BEFORE INSERT OR UPDATE ON advance.material_followup_handoffs FOR EACH ROW EXECUTE FUNCTION advance.rev869b_guard_explicit_mutation();
CREATE TRIGGER trg_rev869b_explicit_policy_mutation BEFORE INSERT OR UPDATE ON advance.purchase_transaction_approval_policies FOR EACH ROW EXECUTE FUNCTION advance.rev869b_guard_explicit_mutation();
CREATE TRIGGER trg_rev869b_qualification_lifecycle BEFORE INSERT OR UPDATE OR DELETE ON advance.vendor_qualifications FOR EACH ROW EXECUTE FUNCTION advance.rev869b_guard_qualification_lifecycle();
CREATE CONSTRAINT TRIGGER trg_rev869b_bound_qualification_history AFTER INSERT OR UPDATE ON advance.vendor_qualifications DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION advance.rev869b_require_qualification_history();
CREATE TRIGGER trg_rev869b_explicit_rfq_line_insert BEFORE INSERT ON advance.request_for_quotation_lines FOR EACH ROW EXECUTE FUNCTION advance.rev869b_guard_explicit_mutation();
CREATE TRIGGER trg_rev869b_explicit_quotation_line_insert BEFORE INSERT ON advance.vendor_quotation_lines FOR EACH ROW EXECUTE FUNCTION advance.rev869b_guard_explicit_mutation();
CREATE TRIGGER trg_rev869b_explicit_technical_insert BEFORE INSERT ON advance.quotation_technical_verifications FOR EACH ROW EXECUTE FUNCTION advance.rev869b_guard_explicit_mutation();
CREATE TRIGGER trg_rev869b_explicit_po_line_insert BEFORE INSERT ON advance.purchase_order_lines FOR EACH ROW EXECUTE FUNCTION advance.rev869b_guard_explicit_mutation();
CREATE CONSTRAINT TRIGGER trg_rev869b_bound_rfq_history AFTER INSERT OR UPDATE ON advance.request_for_quotations DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION advance.rev869b_require_bound_history();
CREATE CONSTRAINT TRIGGER trg_rev869b_bound_invitation_history AFTER INSERT OR UPDATE ON advance.rfq_vendor_invitations DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION advance.rev869b_require_bound_history();
CREATE CONSTRAINT TRIGGER trg_rev869b_bound_quotation_history AFTER INSERT OR UPDATE ON advance.vendor_quotations DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION advance.rev869b_require_bound_history();
CREATE CONSTRAINT TRIGGER trg_rev869b_bound_technical_history AFTER INSERT ON advance.quotation_technical_verifications DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION advance.rev869b_require_bound_history();
CREATE CONSTRAINT TRIGGER trg_rev869b_bound_comparison_history AFTER INSERT OR UPDATE ON advance.commercial_comparisons DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION advance.rev869b_require_bound_history();
CREATE CONSTRAINT TRIGGER trg_rev869b_bound_po_history AFTER INSERT OR UPDATE ON advance.purchase_orders DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION advance.rev869b_require_bound_history();
CREATE CONSTRAINT TRIGGER trg_rev869b_bound_followup_history AFTER INSERT OR UPDATE ON advance.material_followup_handoffs DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION advance.rev869b_require_bound_history();
CREATE TRIGGER trg_rev869b_bound_policy_history AFTER INSERT OR UPDATE ON advance.purchase_transaction_approval_policies FOR EACH ROW EXECUTE FUNCTION advance.rev869b_write_policy_history();

CREATE TRIGGER trg_rev869b_delete_rfq BEFORE DELETE ON advance.request_for_quotations FOR EACH ROW EXECUTE FUNCTION advance.rev869b_reject_controlled_delete();
CREATE TRIGGER trg_rev869b_delete_rfq_line BEFORE DELETE ON advance.request_for_quotation_lines FOR EACH ROW EXECUTE FUNCTION advance.rev869b_reject_controlled_delete();
CREATE TRIGGER trg_rev869b_delete_invitation BEFORE DELETE ON advance.rfq_vendor_invitations FOR EACH ROW EXECUTE FUNCTION advance.rev869b_reject_controlled_delete();
CREATE TRIGGER trg_rev869b_delete_quotation BEFORE DELETE ON advance.vendor_quotations FOR EACH ROW EXECUTE FUNCTION advance.rev869b_reject_controlled_delete();
CREATE TRIGGER trg_rev869b_delete_quotation_line BEFORE DELETE ON advance.vendor_quotation_lines FOR EACH ROW EXECUTE FUNCTION advance.rev869b_reject_controlled_delete();
CREATE TRIGGER trg_rev869b_delete_technical BEFORE DELETE ON advance.quotation_technical_verifications FOR EACH ROW EXECUTE FUNCTION advance.rev869b_reject_controlled_delete();
CREATE TRIGGER trg_rev869b_delete_comparison BEFORE DELETE ON advance.commercial_comparisons FOR EACH ROW EXECUTE FUNCTION advance.rev869b_reject_controlled_delete();
CREATE TRIGGER trg_rev869b_delete_comparison_line BEFORE DELETE ON advance.commercial_comparison_lines FOR EACH ROW EXECUTE FUNCTION advance.rev869b_reject_controlled_delete();
CREATE TRIGGER trg_rev869b_delete_approval_history BEFORE DELETE ON advance.purchase_transaction_approval_history FOR EACH ROW EXECUTE FUNCTION advance.rev869b_reject_controlled_delete();
CREATE TRIGGER trg_rev869b_delete_po BEFORE DELETE ON advance.purchase_orders FOR EACH ROW EXECUTE FUNCTION advance.rev869b_reject_controlled_delete();
CREATE TRIGGER trg_rev869b_delete_po_line BEFORE DELETE ON advance.purchase_order_lines FOR EACH ROW EXECUTE FUNCTION advance.rev869b_reject_controlled_delete();
CREATE TRIGGER trg_rev869b_delete_po_history BEFORE DELETE ON advance.purchase_order_history FOR EACH ROW EXECUTE FUNCTION advance.rev869b_reject_controlled_delete();
CREATE TRIGGER trg_rev869b_delete_followup BEFORE DELETE ON advance.material_followup_handoffs FOR EACH ROW EXECUTE FUNCTION advance.rev869b_reject_controlled_delete();
CREATE TRIGGER trg_rev869b_delete_status_history BEFORE DELETE ON advance.purchase_transaction_status_history FOR EACH ROW EXECUTE FUNCTION advance.rev869b_reject_controlled_delete();
CREATE TRIGGER trg_rev869b_delete_policy BEFORE DELETE ON advance.purchase_transaction_approval_policies FOR EACH ROW EXECUTE FUNCTION advance.rev869b_reject_controlled_delete();

INSERT INTO advance."__EFMigrationsHistory_Rev869BSecurity" ("MigrationId", "ProductVersion")
VALUES ('20260824120000_Rev869BSecurityPackage', '10.0.10');

COMMIT;
