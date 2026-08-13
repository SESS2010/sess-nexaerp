namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>
/// Exact, issuer-reserved REV869B operation grants. The issuer commits a fingerprint-only grant on
/// an independent connection before the business write. The grant is bound to one runtime backend,
/// transaction, principal and finite set of history slots; business rollback cannot restore or move it.
/// Temporary purge is fixed to management approval MGMT-REV869B-SECURITY-LEDGER-20260813-001:
/// unconsumed grants expire within fifteen minutes, temporary ledgers retain ninety days, exports
/// remain disabled, and bounded owner cleanup emits minimized durable evidence retained for ten years.
/// </summary>
internal static class Rev869BCommandContextSql
{
    public const string Install = """
        CREATE EXTENSION IF NOT EXISTS pgcrypto WITH SCHEMA public;
        DO $rev869b_owner$
        DECLARE owner_role oid; purge_role oid; authorizer_role oid; migration_role oid;
        BEGIN
          SELECT oid INTO owner_role FROM pg_roles WHERE rolname='nexa_rev869b_security_owner'
            AND NOT rolcanlogin AND NOT rolsuper AND NOT rolcreatedb AND NOT rolcreaterole
            AND NOT rolreplication AND NOT rolbypassrls;
          SELECT oid INTO purge_role FROM pg_roles WHERE rolname='nexa_rev869b_purge_executor'
            AND rolcanlogin AND NOT rolsuper AND NOT rolcreatedb AND NOT rolcreaterole
            AND NOT rolreplication AND NOT rolbypassrls;
          SELECT oid INTO authorizer_role FROM pg_roles WHERE rolname='nexa_rev869b_purge_authorizer'
            AND rolcanlogin AND NOT rolsuper AND NOT rolcreatedb AND NOT rolcreaterole
            AND NOT rolreplication AND NOT rolbypassrls;
          SELECT oid INTO migration_role FROM pg_roles WHERE rolname=current_user;
          IF owner_role IS NULL OR migration_role IS NULL OR
             NOT pg_has_role(current_user,'nexa_rev869b_security_owner','MEMBER') OR
             NOT EXISTS (SELECT 1 FROM pg_auth_members m WHERE m.roleid=owner_role AND m.member=migration_role
               AND NOT m.inherit_option AND m.set_option) OR
             EXISTS (SELECT 1 FROM pg_auth_members m WHERE m.roleid=owner_role AND m.member<>migration_role) OR
             EXISTS (SELECT 1 FROM pg_auth_members m WHERE m.member=owner_role) THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_dedicated_security_owner_required',
              MESSAGE='Exact NOLOGIN, no-capability security owner with one non-inheriting SET ROLE migration-owner membership is required.';
          END IF;
          IF purge_role IS NULL OR authorizer_role IS NULL OR purge_role=authorizer_role OR
             pg_has_role(current_user,'nexa_rev869b_purge_executor','MEMBER') OR
             pg_has_role(current_user,'nexa_rev869b_purge_authorizer','MEMBER') OR
             EXISTS (SELECT 1 FROM pg_auth_members m WHERE m.roleid IN (purge_role,authorizer_role) OR m.member IN (purge_role,authorizer_role)) THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_dedicated_purge_executor_required',
              MESSAGE='Distinct capability-free, membership-closed purge authorizer and executor LOGIN roles are required.';
          END IF;
        END $rev869b_owner$;

        CREATE TABLE nexa.rev869b_command_authorities(
          "KeyId" uuid PRIMARY KEY,
          "IssuerPrincipal" name NOT NULL,
          "RuntimePrincipal" name NOT NULL,
          "ActiveFrom" timestamptz NOT NULL,
          "ActiveTo" timestamptz NULL,
          "RevokedAt" timestamptz NULL,
          CONSTRAINT "CK_rev869b_command_authority_distinct" CHECK ("IssuerPrincipal"<>"RuntimePrincipal"),
          CONSTRAINT "CK_rev869b_command_authority_dates" CHECK ("ActiveTo" IS NULL OR "ActiveTo">="ActiveFrom")
        );
        CREATE UNIQUE INDEX "UX_rev869b_command_authority_active_pair"
          ON nexa.rev869b_command_authorities("IssuerPrincipal","RuntimePrincipal")
          WHERE "RevokedAt" IS NULL AND "ActiveTo" IS NULL;

        CREATE TABLE nexa.rev869b_command_grants(
          "GrantId" uuid PRIMARY KEY,
          "IssuerPrincipal" name NOT NULL,
          "RuntimePrincipal" name NOT NULL,
          "TargetBackendPid" integer NOT NULL,
          "TargetTransactionId" bigint NOT NULL,
          "OrganizationFingerprint" bytea NOT NULL CHECK (octet_length("OrganizationFingerprint")=32),
          "ActorFingerprint" bytea NOT NULL CHECK (octet_length("ActorFingerprint")=32),
          "IdentityFingerprint" bytea NOT NULL CHECK (octet_length("IdentityFingerprint")=32),
          "RoleFingerprint" bytea NOT NULL CHECK (octet_length("RoleFingerprint")=32),
          "SlotFingerprints" jsonb NOT NULL,
          "SlotCount" integer NOT NULL CHECK ("SlotCount" BETWEEN 1 AND 64),
          "ClaimSequence" name NOT NULL,
          "ClaimSequenceStart" bigint NOT NULL,
          "IssuedAt" timestamptz NOT NULL,
          "ExpiresAt" timestamptz NOT NULL,
          "ReservedAt" timestamptz NOT NULL,
          CONSTRAINT "CK_rev869b_grant_slots" CHECK (jsonb_typeof("SlotFingerprints")='array' AND jsonb_array_length("SlotFingerprints")="SlotCount"),
          CONSTRAINT "CK_rev869b_grant_expiry" CHECK ("ExpiresAt">"IssuedAt" AND "ExpiresAt"<="IssuedAt"+interval '30 seconds'),
          CONSTRAINT "UQ_rev869b_grant_runtime_transaction" UNIQUE("RuntimePrincipal","TargetBackendPid","TargetTransactionId","GrantId")
        );

        CREATE TABLE nexa.rev869b_command_contexts(
          "Token" uuid PRIMARY KEY,
          "GrantId" uuid NOT NULL UNIQUE REFERENCES nexa.rev869b_command_grants("GrantId") ON DELETE RESTRICT,
          "BackendPid" integer NOT NULL,
          "TransactionId" bigint NOT NULL,
          "DatabasePrincipal" name NOT NULL,
          "OpenedAt" timestamptz NOT NULL,
          "ExpiresAt" timestamptz NOT NULL,
          "Claims" jsonb NOT NULL DEFAULT '[]'::jsonb,
          CONSTRAINT "CK_rev869b_command_context_claims" CHECK (jsonb_typeof("Claims")='array')
        );

        CREATE TABLE nexa.rev869b_command_security_audits(
          "AuditId" uuid PRIMARY KEY,"GrantId" uuid NOT NULL,"EventId" uuid NOT NULL UNIQUE,
          "EventType" text NOT NULL CHECK ("EventType" IN ('Issued','Opened','Claimed','Expired','Rejected','Committed','Failed')),
          "CommandFingerprint" text NOT NULL CHECK ("CommandFingerprint"~'^[0-9a-f]{64}$'),
          "OrganizationFingerprint" bytea NOT NULL CHECK (octet_length("OrganizationFingerprint")=32),
          "ActorFingerprint" bytea NOT NULL CHECK (octet_length("ActorFingerprint")=32),
          "IssuerPrincipal" name NOT NULL,"Operation" text NOT NULL,"EntityType" text NOT NULL,"EntityId" uuid NOT NULL,
          "ExpectedVersion" bigint NOT NULL,"SourceStatus" text NULL,"TargetStatus" text NULL,
          "CorrelationFingerprint" bytea NOT NULL CHECK (octet_length("CorrelationFingerprint")=32),
          "OccurredAt" timestamptz NOT NULL,"Outcome" text NOT NULL,"FailureCategory" text NULL,
          "PolicyVersion" text NOT NULL CHECK ("PolicyVersion"='MGMT-REV869B-SECURITY-LEDGER-20260813-001')
        );
        CREATE INDEX "IX_rev869b_command_security_audit_grant_time" ON nexa.rev869b_command_security_audits("GrantId","OccurredAt");

        CREATE TABLE nexa.rev869b_purge_authorizations(
          "ExecutionId" uuid PRIMARY KEY,"ApprovalReference" text NOT NULL,"ApprovalFingerprint" bytea NOT NULL UNIQUE,
          "AuthorizedIssuer" name NOT NULL,"IssuerAuthority" text NOT NULL,"DatabaseName" name NOT NULL,
          "OrganizationScopeFingerprint" bytea NOT NULL CHECK (octet_length("OrganizationScopeFingerprint")=32),
          "PolicyVersion" text NOT NULL,"CutoffAt" timestamptz NOT NULL,"MaximumBatchSize" integer NOT NULL,
          "MaximumPermittedRows" integer NOT NULL,"EligibleStates" text[] NOT NULL,"IssuedAt" timestamptz NOT NULL,
          "ExpiresAt" timestamptz NOT NULL,"NonceFingerprint" bytea NOT NULL UNIQUE,"ExecutorPrincipal" name NOT NULL,
          "RequestedOperation" text NOT NULL,"ApprovalReason" text NOT NULL,"ExpectedAuditDestination" text NOT NULL,
          "State" text NOT NULL CHECK ("State" IN ('Approved','Started','Succeeded','Failed','Rejected')),
          "ConsumedAt" timestamptz NULL,"FinishedAt" timestamptz NULL,
          CONSTRAINT "CK_rev869b_purge_approval_policy" CHECK ("PolicyVersion"='MGMT-REV869B-SECURITY-LEDGER-20260813-001'),
          CONSTRAINT "CK_rev869b_purge_approval_expiry" CHECK ("ExpiresAt">"IssuedAt" AND "ExpiresAt"<="IssuedAt"+interval '15 minutes'),
          CONSTRAINT "CK_rev869b_purge_approval_batch" CHECK ("MaximumBatchSize" BETWEEN 1 AND 1000 AND "MaximumPermittedRows">= "MaximumBatchSize")
        );
        CREATE TABLE nexa.rev869b_purge_attempt_audits(
          "AttemptId" uuid PRIMARY KEY,"ExecutionId" uuid NOT NULL REFERENCES nexa.rev869b_purge_authorizations("ExecutionId") ON DELETE RESTRICT,
          "ApprovalReference" text NOT NULL,"ApprovalFingerprint" bytea NOT NULL CHECK (octet_length("ApprovalFingerprint")=32),
          "PolicyVersion" text NOT NULL,
          "Issuer" text NOT NULL,"Executor" name NOT NULL,"DatabaseName" name NOT NULL,"StartedAt" timestamptz NOT NULL,
          "FinishedAt" timestamptz NULL,"CutoffAt" timestamptz NOT NULL,"BatchLimit" integer NOT NULL,"PreCount" bigint NOT NULL,
          "CandidateCount" integer NOT NULL,"ClaimedCount" integer NOT NULL,"DeletedCount" integer NOT NULL,
          "RetainedAuditCount" bigint NOT NULL,"FailurePhase" text NULL,"SqlState" text NULL,"DatabaseObject" text NULL,
          "CandidateSetFingerprint" bytea NULL,"AcceptanceLabel" text NOT NULL,
          "Outcome" text NOT NULL CHECK ("Outcome" IN ('Started','ZeroRows','Succeeded','Failed','Rejected','PartialFailure')),
          "RetryEligible" boolean NOT NULL,"EvidenceFingerprint" text NOT NULL CHECK ("EvidenceFingerprint"~'^[0-9a-f]{64}$'),
          "CreatedAt" timestamptz NOT NULL
        );
        CREATE UNIQUE INDEX "UX_rev869b_purge_terminal_outcome" ON nexa.rev869b_purge_attempt_audits("ExecutionId")
          WHERE "Outcome" IN ('ZeroRows','Succeeded','Failed','Rejected','PartialFailure');
        CREATE TABLE nexa.rev869b_purge_rejection_audits(
          "RejectionId" uuid PRIMARY KEY,"ExecutionId" uuid NOT NULL,"ActorFingerprint" bytea NOT NULL,
          "ReasonCategory" text NOT NULL,"SqlState" text NOT NULL,"DatabaseObject" text NOT NULL,
          "AcceptanceLabel" text NOT NULL,"OccurredAt" timestamptz NOT NULL,
          "PolicyVersion" text NOT NULL CHECK ("PolicyVersion"='MGMT-REV869B-SECURITY-LEDGER-20260813-001')
        );

        CREATE TABLE nexa.rev869b_claim_sequence_pool(
          "SequenceName" name PRIMARY KEY,
          "GrantId" uuid NULL UNIQUE REFERENCES nexa.rev869b_command_grants("GrantId") ON DELETE RESTRICT
        );
        DO $rev869b_claim_sequence_pool$
        DECLARE sequence_name name; i integer;
        BEGIN
          FOR i IN 1..256 LOOP
            sequence_name:=('rev869b_claim_seq_'||lpad(i::text,3,'0'))::name;
            EXECUTE format('CREATE SEQUENCE nexa.%I AS bigint MINVALUE 1 START WITH 1 INCREMENT BY 1 NO CYCLE',sequence_name);
            EXECUTE format('ALTER SEQUENCE nexa.%I OWNER TO nexa_rev869b_security_owner',sequence_name);
            INSERT INTO nexa.rev869b_claim_sequence_pool("SequenceName") VALUES(sequence_name);
          END LOOP;
        END $rev869b_claim_sequence_pool$;

        REVOKE ALL ON nexa.rev869b_command_authorities FROM PUBLIC;
        REVOKE ALL ON nexa.rev869b_command_grants FROM PUBLIC;
        REVOKE ALL ON nexa.rev869b_command_contexts FROM PUBLIC;
        REVOKE ALL ON nexa.rev869b_claim_sequence_pool FROM PUBLIC;
        REVOKE ALL ON nexa.rev869b_command_security_audits FROM PUBLIC;
        REVOKE ALL ON nexa.rev869b_purge_authorizations FROM PUBLIC;
        REVOKE ALL ON nexa.rev869b_purge_attempt_audits FROM PUBLIC;
        REVOKE ALL ON nexa.rev869b_purge_rejection_audits FROM PUBLIC;

        CREATE OR REPLACE FUNCTION nexa.rev869b_reject_security_audit_mutation()
        RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,nexa AS $rev869b$
        BEGIN
          RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_ten_year_append_only_security_audit',
            MESSAGE='REV869B security audit evidence is append-only and retained for at least ten years.';
        END $rev869b$;
        REVOKE ALL ON FUNCTION nexa.rev869b_reject_security_audit_mutation() FROM PUBLIC;
        CREATE TRIGGER "TR_rev869b_command_security_audit_immutable" BEFORE UPDATE OR DELETE ON nexa.rev869b_command_security_audits
          FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_reject_security_audit_mutation();
        CREATE TRIGGER TR_rev869b_purge_attempt_audit_immutable BEFORE UPDATE OR DELETE ON nexa.rev869b_purge_attempt_audits
          FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_reject_security_audit_mutation();
        CREATE TRIGGER TR_rev869b_purge_rejection_audit_immutable BEFORE UPDATE OR DELETE ON nexa.rev869b_purge_rejection_audits
          FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_reject_security_audit_mutation();

        CREATE OR REPLACE FUNCTION nexa.rev869b_register_purge_authorization(
          execution_id uuid, approval_reference text, approval_fingerprint bytea, organization_scope_fingerprint bytea,
          cutoff_at timestamptz, maximum_batch_size integer, maximum_permitted_rows integer,
          eligible_states text[], issued_at timestamptz, expires_at timestamptz, nonce_fingerprint bytea,
          approval_reason text, expected_audit_destination text)
        RETURNS void LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,nexa AS $rev869b$
        BEGIN
          IF session_user<>'nexa_rev869b_purge_authorizer' OR execution_id IS NULL OR
             length(trim(approval_reference))<8 OR octet_length(approval_fingerprint)<>32 OR
             octet_length(organization_scope_fingerprint)<>32 OR cutoff_at IS DISTINCT FROM issued_at-interval '90 days' OR
             maximum_batch_size NOT BETWEEN 1 AND 1000 OR maximum_permitted_rows<maximum_batch_size OR
             eligible_states IS DISTINCT FROM ARRAY['Expired','Unclaimed']::text[] OR
             issued_at>clock_timestamp()+interval '30 seconds' OR expires_at<=clock_timestamp() OR
             expires_at>issued_at+interval '15 minutes' OR octet_length(nonce_fingerprint)<>32 OR
             expected_audit_destination<>'nexa.rev869b_purge_attempt_audits' THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_fresh_exact_purge_approval_required',
              MESSAGE='A fresh separately issued, execution-specific, organization-bound purge approval is required.';
          END IF;
          INSERT INTO nexa.rev869b_purge_authorizations
            ("ExecutionId","ApprovalReference","ApprovalFingerprint","AuthorizedIssuer","IssuerAuthority","DatabaseName",
             "OrganizationScopeFingerprint","PolicyVersion","CutoffAt","MaximumBatchSize","MaximumPermittedRows",
             "EligibleStates","IssuedAt","ExpiresAt","NonceFingerprint","ExecutorPrincipal","RequestedOperation",
             "ApprovalReason","ExpectedAuditDestination","State")
          VALUES(execution_id,trim(approval_reference),approval_fingerprint,session_user,
            'REV869B_MANAGEMENT_PURGE_AUTHORITY_V1',current_database(),organization_scope_fingerprint,
            'MGMT-REV869B-SECURITY-LEDGER-20260813-001',cutoff_at,maximum_batch_size,maximum_permitted_rows,
            eligible_states,issued_at,expires_at,nonce_fingerprint,'nexa_rev869b_purge_executor',
            'PurgeTemporarySecurityLedger',trim(approval_reason),expected_audit_destination,'Approved');
        END $rev869b$;
        REVOKE ALL ON FUNCTION nexa.rev869b_register_purge_authorization(uuid,text,bytea,bytea,timestamptz,integer,integer,text[],timestamptz,timestamptz,bytea,text,text) FROM PUBLIC;

        CREATE OR REPLACE FUNCTION nexa.rev869b_begin_purge_execution(execution_id uuid, nonce_fingerprint bytea)
        RETURNS integer LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,nexa AS $rev869b$
        DECLARE approval nexa.rev869b_purge_authorizations%ROWTYPE; candidate_ids uuid[]; candidate_count integer;
          eligible_count bigint; candidate_fingerprint bytea; evidence text; rejection text;
        BEGIN
          IF session_user<>'nexa_rev869b_purge_executor' THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_exact_purge_executor',
              MESSAGE='Only the dedicated purge executor may consume an approval.';
          END IF;
          SELECT a.* INTO approval FROM nexa.rev869b_purge_authorizations a WHERE a."ExecutionId"=execution_id FOR UPDATE;
          IF NOT FOUND THEN rejection:='MissingAuthorization';
          ELSIF approval."State"<>'Approved' OR approval."ConsumedAt" IS NOT NULL THEN rejection:='ReplayedAuthorization';
          ELSIF approval."DatabaseName"<>current_database() OR approval."ExecutorPrincipal"<>session_user THEN rejection:='WrongDatabaseOrExecutor';
          ELSIF approval."ExpiresAt"<=clock_timestamp() THEN rejection:='ExpiredAuthorization';
          ELSIF approval."NonceFingerprint" IS DISTINCT FROM nonce_fingerprint THEN rejection:='WrongNonce';
          ELSIF approval."PolicyVersion"<>'MGMT-REV869B-SECURITY-LEDGER-20260813-001' THEN rejection:='WrongPolicy';
          END IF;
          IF rejection IS NOT NULL THEN
            INSERT INTO nexa.rev869b_purge_rejection_audits VALUES(gen_random_uuid(),execution_id,
              public.digest(convert_to(session_user,'UTF8'),'sha256'),rejection,'42501','nexa.rev869b_purge_authorizations',
              'REV869B_PURGE_REJECTED',clock_timestamp(),'MGMT-REV869B-SECURITY-LEDGER-20260813-001');
            IF approval."ExecutionId" IS NOT NULL AND approval."State"='Approved' THEN
              UPDATE nexa.rev869b_purge_authorizations SET "State"='Rejected',"FinishedAt"=clock_timestamp()
                WHERE "ExecutionId"=execution_id;
            END IF;
            RETURN -1;
          END IF;
          SELECT count(*) INTO eligible_count FROM nexa.rev869b_command_grants g
            WHERE g."OrganizationFingerprint"=approval."OrganizationScopeFingerprint"
              AND g."ReservedAt"<approval."CutoffAt" AND g."ExpiresAt"<=approval."IssuedAt"
              AND NOT EXISTS (SELECT 1 FROM nexa.rev869b_command_security_audits x
                WHERE x."GrantId"=g."GrantId" AND x."EventType"='Claimed');
          SELECT array_agg(q."GrantId"),count(*) INTO candidate_ids,candidate_count FROM (
            SELECT g."GrantId" FROM nexa.rev869b_command_grants g
            WHERE g."OrganizationFingerprint"=approval."OrganizationScopeFingerprint"
              AND g."ReservedAt"<approval."CutoffAt" AND g."ExpiresAt"<=approval."IssuedAt"
              AND NOT EXISTS (SELECT 1 FROM nexa.rev869b_command_security_audits x
                WHERE x."GrantId"=g."GrantId" AND x."EventType"='Claimed')
            ORDER BY g."ReservedAt",g."GrantId" LIMIT approval."MaximumBatchSize") q;
          candidate_fingerprint:=public.digest(convert_to(coalesce(candidate_ids,ARRAY[]::uuid[])::text,'UTF8'),'sha256');
          evidence:=encode(public.digest(convert_to(jsonb_build_array(execution_id,approval."ApprovalReference",
            encode(approval."ApprovalFingerprint",'hex'),current_database(),eligible_count,candidate_count,
            encode(candidate_fingerprint,'hex'),clock_timestamp())::text,'UTF8'),'sha256'),'hex');
          UPDATE nexa.rev869b_purge_authorizations SET
            "State"=CASE WHEN candidate_count=0 THEN 'Succeeded' ELSE 'Started' END,
            "ConsumedAt"=clock_timestamp(),"FinishedAt"=CASE WHEN candidate_count=0 THEN clock_timestamp() ELSE NULL END
            WHERE "ExecutionId"=execution_id;
          INSERT INTO nexa.rev869b_purge_attempt_audits
            ("AttemptId","ExecutionId","ApprovalReference","ApprovalFingerprint","PolicyVersion","Issuer","Executor",
             "DatabaseName","StartedAt","FinishedAt","CutoffAt","BatchLimit","PreCount","CandidateCount","ClaimedCount",
             "DeletedCount","RetainedAuditCount","CandidateSetFingerprint","AcceptanceLabel","Outcome","RetryEligible",
             "EvidenceFingerprint","CreatedAt")
          VALUES(gen_random_uuid(),execution_id,approval."ApprovalReference",approval."ApprovalFingerprint",
            approval."PolicyVersion",approval."AuthorizedIssuer",session_user,current_database(),clock_timestamp(),
            CASE WHEN candidate_count=0 THEN clock_timestamp() ELSE NULL END,approval."CutoffAt",
            approval."MaximumBatchSize",eligible_count,candidate_count,0,0,
            (SELECT count(*) FROM nexa.rev869b_command_security_audits),candidate_fingerprint,
            CASE WHEN candidate_count=0 THEN 'REV869B_PURGE_ZERO_ROWS' ELSE 'REV869B_PURGE_STARTED' END,
            CASE WHEN candidate_count=0 THEN 'ZeroRows' ELSE 'Started' END,false,evidence,clock_timestamp());
          RETURN candidate_count;
        END $rev869b$;
        REVOKE ALL ON FUNCTION nexa.rev869b_begin_purge_execution(uuid,bytea) FROM PUBLIC;

        CREATE OR REPLACE FUNCTION nexa.rev869b_purge_temporary_security_ledger(execution_id uuid)
        RETURNS integer LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,nexa AS $rev869b$
        DECLARE approval nexa.rev869b_purge_authorizations%ROWTYPE; started nexa.rev869b_purge_attempt_audits%ROWTYPE;
          grant_ids uuid[]; candidate_count integer:=0; deleted_count integer:=0; evidence text;
          observed_fingerprint bytea; failure_state text; failure_object text; failure_outcome text;
        BEGIN
          IF session_user<>'nexa_rev869b_purge_executor' THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_exact_purge_executor',
              MESSAGE='Only the dedicated purge executor may execute purge.';
          END IF;
          SELECT a.* INTO STRICT approval FROM nexa.rev869b_purge_authorizations a
            WHERE a."ExecutionId"=execution_id FOR UPDATE;
          SELECT a.* INTO STRICT started FROM nexa.rev869b_purge_attempt_audits a
            WHERE a."ExecutionId"=execution_id AND a."Outcome"='Started';
          IF approval."State"<>'Started' OR approval."ConsumedAt" IS NULL OR approval."FinishedAt" IS NOT NULL OR
             approval."DatabaseName"<>current_database() OR approval."ExecutorPrincipal"<>session_user THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_purge_execution_state',
              MESSAGE='Purge execution is not in its exact one-consumable started state.';
          END IF;
          PERFORM pg_advisory_xact_lock(hashtextextended(execution_id::text,0));
          SELECT array_agg(q."GrantId"),count(*) INTO grant_ids,candidate_count FROM (
            SELECT g."GrantId" FROM nexa.rev869b_command_grants g
            WHERE g."OrganizationFingerprint"=approval."OrganizationScopeFingerprint"
              AND g."ReservedAt"<approval."CutoffAt" AND g."ExpiresAt"<=approval."IssuedAt"
              AND NOT EXISTS (SELECT 1 FROM nexa.rev869b_command_security_audits x
                WHERE x."GrantId"=g."GrantId" AND x."EventType"='Claimed')
            ORDER BY g."ReservedAt",g."GrantId" FOR UPDATE LIMIT approval."MaximumBatchSize") q;
          observed_fingerprint:=public.digest(convert_to(coalesce(grant_ids,ARRAY[]::uuid[])::text,'UTF8'),'sha256');
          IF candidate_count<>started."CandidateCount" OR observed_fingerprint IS DISTINCT FROM started."CandidateSetFingerprint" THEN
            RAISE EXCEPTION USING ERRCODE='P0001',CONSTRAINT='rev869b_purge_candidate_drift',
              MESSAGE='The exact durable candidate set changed; partial success is prohibited.';
          END IF;
          INSERT INTO nexa.rev869b_command_security_audits
            ("AuditId","GrantId","EventId","EventType","CommandFingerprint","OrganizationFingerprint","ActorFingerprint",
             "IssuerPrincipal","Operation","EntityType","EntityId","ExpectedVersion","SourceStatus","TargetStatus",
             "CorrelationFingerprint","OccurredAt","Outcome","FailureCategory","PolicyVersion")
          SELECT gen_random_uuid(),a."GrantId",gen_random_uuid(),'Expired',a."CommandFingerprint",
            a."OrganizationFingerprint",a."ActorFingerprint",a."IssuerPrincipal",a."Operation",a."EntityType",
            a."EntityId",a."ExpectedVersion",a."SourceStatus",a."TargetStatus",a."CorrelationFingerprint",
            clock_timestamp(),'Temporary grant expired without a committed claim','RetentionExpiry',a."PolicyVersion"
          FROM nexa.rev869b_command_security_audits a WHERE a."GrantId"=ANY(grant_ids) AND a."EventType"='Issued'
            AND NOT EXISTS (SELECT 1 FROM nexa.rev869b_command_security_audits x
              WHERE x."GrantId"=a."GrantId" AND x."CommandFingerprint"=a."CommandFingerprint"
                AND x."EventType" IN ('Claimed','Committed'));
          DELETE FROM nexa.rev869b_command_contexts c WHERE c."GrantId"=ANY(grant_ids);
          UPDATE nexa.rev869b_claim_sequence_pool p SET "GrantId"=NULL WHERE p."GrantId"=ANY(grant_ids);
          DELETE FROM nexa.rev869b_command_grants g WHERE g."GrantId"=ANY(grant_ids);
          GET DIAGNOSTICS deleted_count=ROW_COUNT;
          IF deleted_count<>started."CandidateCount" OR deleted_count>approval."MaximumPermittedRows" THEN
            RAISE EXCEPTION USING ERRCODE='P0001',CONSTRAINT='rev869b_temporary_purge_count_mismatch',
              MESSAGE='Exact purge count mismatch; all temporary deletion is rolled back.';
          END IF;
          evidence:=encode(public.digest(convert_to(jsonb_build_array(execution_id,candidate_count,deleted_count,
            encode(observed_fingerprint,'hex'),clock_timestamp())::text,'UTF8'),'sha256'),'hex');
          UPDATE nexa.rev869b_purge_authorizations SET "State"='Succeeded',"FinishedAt"=clock_timestamp()
            WHERE "ExecutionId"=execution_id;
          INSERT INTO nexa.rev869b_purge_attempt_audits
            ("AttemptId","ExecutionId","ApprovalReference","ApprovalFingerprint","PolicyVersion","Issuer","Executor",
             "DatabaseName","StartedAt","FinishedAt","CutoffAt","BatchLimit","PreCount","CandidateCount","ClaimedCount",
             "DeletedCount","RetainedAuditCount","CandidateSetFingerprint","AcceptanceLabel","Outcome","RetryEligible",
             "EvidenceFingerprint","CreatedAt")
          VALUES(gen_random_uuid(),execution_id,approval."ApprovalReference",approval."ApprovalFingerprint",
            approval."PolicyVersion",approval."AuthorizedIssuer",session_user,current_database(),approval."ConsumedAt",
            clock_timestamp(),approval."CutoffAt",approval."MaximumBatchSize",started."PreCount",candidate_count,
            candidate_count,deleted_count,(SELECT count(*) FROM nexa.rev869b_command_security_audits),
            observed_fingerprint,'REV869B_PURGE_SUCCEEDED','Succeeded',false,evidence,clock_timestamp());
          RETURN deleted_count;
        EXCEPTION WHEN OTHERS THEN
          GET STACKED DIAGNOSTICS failure_state=RETURNED_SQLSTATE,failure_object=CONSTRAINT_NAME;
          failure_outcome:=CASE WHEN deleted_count>0 THEN 'PartialFailure' ELSE 'Failed' END;
          UPDATE nexa.rev869b_purge_authorizations SET "State"='Failed',"FinishedAt"=clock_timestamp()
            WHERE "ExecutionId"=execution_id AND "State"='Started';
          INSERT INTO nexa.rev869b_purge_attempt_audits
            ("AttemptId","ExecutionId","ApprovalReference","ApprovalFingerprint","PolicyVersion","Issuer","Executor",
             "DatabaseName","StartedAt","FinishedAt","CutoffAt","BatchLimit","PreCount","CandidateCount","ClaimedCount",
             "DeletedCount","RetainedAuditCount","FailurePhase","SqlState","DatabaseObject","CandidateSetFingerprint",
             "AcceptanceLabel","Outcome","RetryEligible","EvidenceFingerprint","CreatedAt")
          SELECT gen_random_uuid(),a."ExecutionId",a."ApprovalReference",a."ApprovalFingerprint",a."PolicyVersion",
            a."AuthorizedIssuer",session_user,current_database(),a."ConsumedAt",clock_timestamp(),a."CutoffAt",
            a."MaximumBatchSize",coalesce(s."PreCount",0),coalesce(s."CandidateCount",0),candidate_count,0,
            (SELECT count(*) FROM nexa.rev869b_command_security_audits),'Execute',failure_state,
            coalesce(nullif(failure_object,''),'nexa.rev869b_purge_temporary_security_ledger'),
            observed_fingerprint,'REV869B_PURGE_FAILED',failure_outcome,true,
            encode(public.digest(convert_to(jsonb_build_array(execution_id,failure_state,failure_object,
              candidate_count,clock_timestamp())::text,'UTF8'),'sha256'),'hex'),clock_timestamp()
          FROM nexa.rev869b_purge_authorizations a LEFT JOIN nexa.rev869b_purge_attempt_audits s
            ON s."ExecutionId"=a."ExecutionId" AND s."Outcome"='Started'
          WHERE a."ExecutionId"=execution_id;
          RETURN -1;
        END $rev869b$;
        REVOKE ALL ON FUNCTION nexa.rev869b_purge_temporary_security_ledger(uuid) FROM PUBLIC;

        CREATE OR REPLACE FUNCTION nexa.rev869b_slot_fingerprint(
          claim_kind text, history_id uuid, entity_type text, entity_id uuid, operation text,
          parent_version bigint, from_status text, to_status text, actor_employee uuid,
          identity_issuer text, identity_subject text, actor_role text, organization text,
          correlation text, remarks text, include_history boolean)
        RETURNS text LANGUAGE sql IMMUTABLE SECURITY DEFINER
        SET search_path=pg_catalog,nexa AS $rev869b$
          SELECT encode(public.digest(convert_to(jsonb_build_array(
            claim_kind,CASE WHEN include_history THEN history_id::text ELSE NULL END,entity_type,entity_id::text,
            operation,parent_version,from_status,to_status,actor_employee::text,identity_issuer,
            identity_subject,actor_role,organization,correlation,remarks)::text,'UTF8'),'sha256'),'hex')
        $rev869b$;
        REVOKE ALL ON FUNCTION nexa.rev869b_slot_fingerprint(text,uuid,text,uuid,text,bigint,text,text,uuid,text,text,text,text,text,text,boolean) FROM PUBLIC;

        CREATE OR REPLACE FUNCTION nexa.rev869b_issue_command_grant(
          runtime_principal name, target_backend_pid integer, target_transaction_id bigint,
          actor_employee uuid, identity_issuer text, identity_subject text, actor_role text,
          organization text, authenticated_epoch_ms bigint, authorized_slots jsonb)
        RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER
        SET search_path=pg_catalog,nexa AS $rev869b$
        DECLARE grant_id uuid:=gen_random_uuid(); authenticated_at timestamptz:=to_timestamp(authenticated_epoch_ms/1000.0);
          slot jsonb; fingerprints jsonb:='[]'::jsonb; slot_fp text; semantic_fp text; slot_count integer;
          slot_ordinal integer:=0; claim_sequence name; claim_sequence_start bigint;
          identity_count bigint; role_count bigint;
        BEGIN
          IF runtime_principal IS NULL OR target_backend_pid<=0 OR target_transaction_id<=0 OR
             length(trim(coalesce(identity_issuer,'')))=0 OR length(trim(coalesce(identity_subject,'')))=0 OR
             length(trim(coalesce(actor_role,'')))=0 OR length(trim(coalesce(organization,'')))=0 OR
             jsonb_typeof(authorized_slots)<>'array' OR jsonb_array_length(authorized_slots) NOT BETWEEN 1 AND 64 OR
             abs(extract(epoch FROM (clock_timestamp()-authenticated_at)))>30 THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_exact_grant_required',MESSAGE='A fresh complete exact operation grant is required.';
          END IF;
          IF NOT EXISTS (SELECT 1 FROM nexa.rev869b_command_authorities a WHERE a."IssuerPrincipal"=session_user
             AND a."RuntimePrincipal"=runtime_principal AND a."RevokedAt" IS NULL AND a."ActiveFrom"<=clock_timestamp()
             AND (a."ActiveTo" IS NULL OR a."ActiveTo">=clock_timestamp())) THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_command_issuer_runtime_binding',MESSAGE='Issuer is not authorized for the exact runtime principal.';
          END IF;
          SELECT count(*) INTO identity_count FROM nexa.employee_identity_mappings m JOIN nexa.employees e ON e."Id"=m."EmployeeId"
           WHERE m."EmployeeId"=actor_employee AND m."Issuer"=identity_issuer AND m."Subject"=identity_subject
             AND m."OrganizationId"=organization AND m."IsActive" AND m."EffectiveFrom"<=transaction_timestamp()::date
             AND (m."EffectiveTo" IS NULL OR m."EffectiveTo">=transaction_timestamp()::date) AND e."Status"='Active' AND e."LoginEnabled";
          SELECT count(*) INTO role_count FROM nexa.employee_role_assignments a JOIN nexa.roles r ON r."Id"=a."RoleId"
           WHERE a."EmployeeId"=actor_employee AND r."Code"=actor_role AND r."IsActive"
             AND a."ApprovalStatus" IN ('Approved','SeedApproved') AND a."EffectiveFrom"<=transaction_timestamp()::date
             AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=transaction_timestamp()::date);
          IF identity_count<>1 OR role_count<>1 THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_grant_principal_binding',MESSAGE='Grant principal must resolve to one active issuer/subject employee identity and role.';
          END IF;
          FOR slot IN SELECT value FROM jsonb_array_elements(authorized_slots) LOOP
            slot_ordinal:=slot_ordinal+1;
            IF jsonb_typeof(slot)<>'object' OR jsonb_object_length(slot)<>10 OR
               length(trim(coalesce(slot->>'claimKind','')))=0 OR (slot->>'historyId') IS NULL OR
               length(trim(coalesce(slot->>'entityType','')))=0 OR (slot->>'entityId') IS NULL OR
               length(trim(coalesce(slot->>'operation','')))=0 OR (slot->>'parentVersion') IS NULL OR
               length(trim(coalesce(slot->>'correlation','')))=0 OR length(trim(coalesce(slot->>'remarks','')))=0 THEN
              RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_grant_slot_shape',MESSAGE='Every authorized slot must be exact and complete.';
            END IF;
            slot_fp:=nexa.rev869b_slot_fingerprint(slot->>'claimKind',(slot->>'historyId')::uuid,slot->>'entityType',(slot->>'entityId')::uuid,
              slot->>'operation',(slot->>'parentVersion')::bigint,slot->>'fromStatus',slot->>'toStatus',actor_employee,identity_issuer,
              identity_subject,actor_role,organization,slot->>'correlation',slot->>'remarks',true);
            semantic_fp:=nexa.rev869b_slot_fingerprint(slot->>'claimKind',(slot->>'historyId')::uuid,slot->>'entityType',(slot->>'entityId')::uuid,
              slot->>'operation',(slot->>'parentVersion')::bigint,slot->>'fromStatus',slot->>'toStatus',actor_employee,identity_issuer,
              identity_subject,actor_role,organization,slot->>'correlation',slot->>'remarks',false);
            IF fingerprints @> jsonb_build_array(jsonb_build_object('slot',slot_fp)) OR
               fingerprints @> jsonb_build_array(jsonb_build_object('semantic',semantic_fp)) THEN
              RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_grant_duplicate_semantic_slot',MESSAGE='Duplicate exact or semantic operation slots are prohibited.';
            END IF;
            fingerprints:=fingerprints||jsonb_build_array(jsonb_build_object('slot',slot_fp,'semantic',semantic_fp,'ordinal',slot_ordinal));
          END LOOP;
          slot_count:=jsonb_array_length(fingerprints);
          UPDATE nexa.rev869b_claim_sequence_pool p SET "GrantId"=NULL
            FROM nexa.rev869b_command_grants expired
            WHERE p."GrantId"=expired."GrantId" AND expired."ExpiresAt"<=clock_timestamp();
          SELECT p."SequenceName" INTO claim_sequence FROM nexa.rev869b_claim_sequence_pool p
            WHERE p."GrantId" IS NULL ORDER BY p."SequenceName" FOR UPDATE SKIP LOCKED LIMIT 1;
          IF claim_sequence IS NULL THEN
            RAISE EXCEPTION USING ERRCODE='53300',CONSTRAINT='rev869b_claim_sequence_pool_exhausted',MESSAGE='No isolated claim sequence is available.';
          END IF;
          EXECUTE format('SELECT nextval(%L::regclass)',format('nexa.%I',claim_sequence)) INTO claim_sequence_start;
          INSERT INTO nexa.rev869b_command_grants("GrantId","IssuerPrincipal","RuntimePrincipal","TargetBackendPid","TargetTransactionId",
            "OrganizationFingerprint","ActorFingerprint","IdentityFingerprint","RoleFingerprint","SlotFingerprints","SlotCount","ClaimSequence","ClaimSequenceStart","IssuedAt","ExpiresAt","ReservedAt")
          VALUES(grant_id,session_user,runtime_principal,target_backend_pid,target_transaction_id,
            public.digest(convert_to(organization,'UTF8'),'sha256'),public.digest(convert_to(actor_employee::text,'UTF8'),'sha256'),
            public.digest(convert_to(jsonb_build_array(identity_issuer,identity_subject)::text,'UTF8'),'sha256'),
            public.digest(convert_to(actor_role,'UTF8'),'sha256'),fingerprints,slot_count,claim_sequence,claim_sequence_start,authenticated_at,authenticated_at+interval '30 seconds',clock_timestamp());
          UPDATE nexa.rev869b_claim_sequence_pool SET "GrantId"=grant_id WHERE "SequenceName"=claim_sequence AND "GrantId" IS NULL;
          INSERT INTO nexa.rev869b_command_security_audits
            ("AuditId","GrantId","EventId","EventType","CommandFingerprint","OrganizationFingerprint","ActorFingerprint","IssuerPrincipal",
             "Operation","EntityType","EntityId","ExpectedVersion","SourceStatus","TargetStatus","CorrelationFingerprint","OccurredAt","Outcome","PolicyVersion")
          SELECT gen_random_uuid(),grant_id,gen_random_uuid(),'Issued',
            nexa.rev869b_slot_fingerprint(s.value->>'claimKind',(s.value->>'historyId')::uuid,s.value->>'entityType',(s.value->>'entityId')::uuid,
              s.value->>'operation',(s.value->>'parentVersion')::bigint,s.value->>'fromStatus',s.value->>'toStatus',actor_employee,
              identity_issuer,identity_subject,actor_role,organization,s.value->>'correlation',s.value->>'remarks',false),
            public.digest(convert_to(organization,'UTF8'),'sha256'),public.digest(convert_to(actor_employee::text,'UTF8'),'sha256'),session_user,
            s.value->>'operation',s.value->>'entityType',(s.value->>'entityId')::uuid,(s.value->>'parentVersion')::bigint,
            s.value->>'fromStatus',s.value->>'toStatus',public.digest(convert_to(s.value->>'correlation','UTF8'),'sha256'),
            clock_timestamp(),'Authorized; no committed claim yet','MGMT-REV869B-SECURITY-LEDGER-20260813-001'
          FROM jsonb_array_elements(authorized_slots) s;
          RETURN grant_id;
        END $rev869b$;
        REVOKE ALL ON FUNCTION nexa.rev869b_issue_command_grant(name,integer,bigint,uuid,text,text,text,text,bigint,jsonb) FROM PUBLIC;

        CREATE OR REPLACE FUNCTION nexa.rev869b_open_command_context(
          grant_id uuid, actor_employee uuid, identity_issuer text, identity_subject text,
          actor_role text, organization text, expected_backend_pid integer, expected_transaction_id bigint)
        RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER
        SET search_path=pg_catalog,nexa AS $rev869b$
        DECLARE command_token uuid:=gen_random_uuid(); grant_expires timestamptz;
        BEGIN
          IF expected_backend_pid IS DISTINCT FROM pg_backend_pid() OR expected_transaction_id IS DISTINCT FROM txid_current() THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_grant_connection_transaction_binding',MESSAGE='Grant is bound to another connection or transaction.';
          END IF;
          SELECT g."ExpiresAt" INTO STRICT grant_expires FROM nexa.rev869b_command_grants g
           WHERE g."GrantId"=grant_id AND g."RuntimePrincipal"=session_user AND g."TargetBackendPid"=pg_backend_pid()
             AND g."TargetTransactionId"=txid_current() AND g."ReservedAt" IS NOT NULL AND g."IssuedAt"<=clock_timestamp() AND g."ExpiresAt">clock_timestamp()
             AND g."OrganizationFingerprint"=public.digest(convert_to(organization,'UTF8'),'sha256')
             AND g."ActorFingerprint"=public.digest(convert_to(actor_employee::text,'UTF8'),'sha256')
             AND g."IdentityFingerprint"=public.digest(convert_to(jsonb_build_array(identity_issuer,identity_subject)::text,'UTF8'),'sha256')
             AND g."RoleFingerprint"=public.digest(convert_to(actor_role,'UTF8'),'sha256');
          INSERT INTO nexa.rev869b_command_contexts("Token","GrantId","BackendPid","TransactionId","DatabasePrincipal","OpenedAt","ExpiresAt")
          VALUES(command_token,grant_id,pg_backend_pid(),txid_current(),session_user,clock_timestamp(),grant_expires);
          INSERT INTO nexa.rev869b_command_security_audits
            ("AuditId","GrantId","EventId","EventType","CommandFingerprint","OrganizationFingerprint","ActorFingerprint","IssuerPrincipal",
             "Operation","EntityType","EntityId","ExpectedVersion","SourceStatus","TargetStatus","CorrelationFingerprint","OccurredAt","Outcome","PolicyVersion")
          SELECT gen_random_uuid(),a."GrantId",gen_random_uuid(),'Opened',a."CommandFingerprint",a."OrganizationFingerprint",a."ActorFingerprint",a."IssuerPrincipal",
            a."Operation",a."EntityType",a."EntityId",a."ExpectedVersion",a."SourceStatus",a."TargetStatus",a."CorrelationFingerprint",
            clock_timestamp(),'Opened on exact backend and transaction',a."PolicyVersion"
          FROM nexa.rev869b_command_security_audits a WHERE a."GrantId"=grant_id AND a."EventType"='Issued';
          PERFORM set_config('nexa.rev869b_command_token',command_token::text,true);
          PERFORM set_config('nexa.rev869b_actor_employee_id',actor_employee::text,true);
          PERFORM set_config('nexa.rev869b_actor_login',identity_subject,true);
          PERFORM set_config('nexa.rev869b_identity_issuer',identity_issuer,true);
          PERFORM set_config('nexa.rev869b_identity_subject',identity_subject,true);
          PERFORM set_config('nexa.rev869b_actor_role',actor_role,true);
          PERFORM set_config('nexa.rev869b_organization',organization,true);
          RETURN command_token;
        EXCEPTION WHEN no_data_found OR unique_violation THEN
          RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_grant_missing_stale_or_reused',MESSAGE='Exact grant is missing, stale, already opened, or bound elsewhere.';
        END $rev869b$;
        REVOKE ALL ON FUNCTION nexa.rev869b_open_command_context(uuid,uuid,text,text,text,text,integer,bigint) FROM PUBLIC;

        CREATE OR REPLACE FUNCTION nexa.rev869b_command_context_valid(organization text, actor_employee uuid, identity_issuer text, identity_subject text, actor_role text)
        RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,nexa AS $rev869b$
          SELECT count(*)=1 FROM nexa.rev869b_command_contexts c JOIN nexa.rev869b_command_grants g ON g."GrantId"=c."GrantId"
           WHERE c."Token"=nullif(current_setting('nexa.rev869b_command_token',true),'')::uuid AND c."BackendPid"=pg_backend_pid()
             AND c."TransactionId"=txid_current() AND c."DatabasePrincipal"=session_user AND c."OpenedAt"<=clock_timestamp() AND c."ExpiresAt">clock_timestamp()
             AND g."OrganizationFingerprint"=public.digest(convert_to(organization,'UTF8'),'sha256')
             AND g."ActorFingerprint"=public.digest(convert_to(actor_employee::text,'UTF8'),'sha256')
             AND g."IdentityFingerprint"=public.digest(convert_to(jsonb_build_array(identity_issuer,identity_subject)::text,'UTF8'),'sha256')
             AND g."RoleFingerprint"=public.digest(convert_to(actor_role,'UTF8'),'sha256')
        $rev869b$;
        REVOKE ALL ON FUNCTION nexa.rev869b_command_context_valid(text,uuid,text,text,text) FROM PUBLIC;

        CREATE OR REPLACE FUNCTION nexa.rev869b_claim_command_context(claim_kind text, history_id uuid, entity_type text, entity_id uuid,
          operation text, parent_version bigint, from_status text, to_status text, correlation text, remarks text)
        RETURNS void LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,nexa AS $rev869b$
        DECLARE command_token uuid; slot_fp text; semantic_fp text; claim jsonb; claim_sequence name; claim_sequence_start bigint; attempt_ordinal bigint;
        BEGIN
          command_token:=nullif(current_setting('nexa.rev869b_command_token',true),'')::uuid;
          IF command_token IS NULL OR length(trim(coalesce(claim_kind,'')))=0 OR history_id IS NULL OR length(trim(coalesce(entity_type,'')))=0 OR
             length(trim(coalesce(operation,'')))=0 OR length(trim(coalesce(correlation,'')))=0 OR length(trim(coalesce(remarks,'')))=0 THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_command_claim_required',MESSAGE='A complete exact command claim is required.';
          END IF;
          slot_fp:=nexa.rev869b_slot_fingerprint(claim_kind,history_id,entity_type,entity_id,operation,parent_version,from_status,to_status,
            nullif(current_setting('nexa.rev869b_actor_employee_id',true),'')::uuid,current_setting('nexa.rev869b_identity_issuer',true),
            current_setting('nexa.rev869b_identity_subject',true),current_setting('nexa.rev869b_actor_role',true),
            current_setting('nexa.rev869b_organization',true),correlation,remarks,true);
          semantic_fp:=nexa.rev869b_slot_fingerprint(claim_kind,history_id,entity_type,entity_id,operation,parent_version,from_status,to_status,
            nullif(current_setting('nexa.rev869b_actor_employee_id',true),'')::uuid,current_setting('nexa.rev869b_identity_issuer',true),
            current_setting('nexa.rev869b_identity_subject',true),current_setting('nexa.rev869b_actor_role',true),
            current_setting('nexa.rev869b_organization',true),correlation,remarks,false);
          SELECT g."ClaimSequence",g."ClaimSequenceStart" INTO STRICT claim_sequence,claim_sequence_start FROM nexa.rev869b_command_contexts c
            JOIN nexa.rev869b_command_grants g ON g."GrantId"=c."GrantId"
            WHERE c."Token"=command_token AND c."BackendPid"=pg_backend_pid() AND c."TransactionId"=txid_current()
              AND c."DatabasePrincipal"=session_user AND c."ExpiresAt">clock_timestamp();
          EXECUTE format('SELECT nextval(%L::regclass)',format('nexa.%I',claim_sequence)) INTO attempt_ordinal;
          attempt_ordinal:=attempt_ordinal-claim_sequence_start;
          claim:=jsonb_build_object('slot',slot_fp,'semantic',semantic_fp,'ordinal',attempt_ordinal);
          UPDATE nexa.rev869b_command_contexts c SET "Claims"=c."Claims"||jsonb_build_array(claim)
           FROM nexa.rev869b_command_grants g WHERE c."Token"=command_token AND g."GrantId"=c."GrantId"
             AND c."BackendPid"=pg_backend_pid() AND c."TransactionId"=txid_current() AND c."DatabasePrincipal"=session_user
             AND c."ExpiresAt">clock_timestamp() AND g."SlotFingerprints" @> jsonb_build_array(claim);
          IF NOT FOUND THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_command_claim_unissued_or_reused',MESSAGE='Claim was not issuer-authorized for this exact slot or was already consumed.';
          END IF;
          INSERT INTO nexa.rev869b_command_security_audits
            ("AuditId","GrantId","EventId","EventType","CommandFingerprint","OrganizationFingerprint","ActorFingerprint","IssuerPrincipal",
             "Operation","EntityType","EntityId","ExpectedVersion","SourceStatus","TargetStatus","CorrelationFingerprint","OccurredAt","Outcome","PolicyVersion")
          SELECT gen_random_uuid(),g."GrantId",gen_random_uuid(),'Claimed',a."CommandFingerprint",a."OrganizationFingerprint",a."ActorFingerprint",a."IssuerPrincipal",
            a."Operation",a."EntityType",a."EntityId",a."ExpectedVersion",a."SourceStatus",a."TargetStatus",a."CorrelationFingerprint",
            clock_timestamp(),'Claim accepted in protected transaction; terminal outcome pending',a."PolicyVersion"
          FROM nexa.rev869b_command_contexts c JOIN nexa.rev869b_command_grants g ON g."GrantId"=c."GrantId"
            JOIN nexa.rev869b_command_security_audits a ON a."GrantId"=g."GrantId" AND a."EventType"='Issued' AND a."CommandFingerprint"=semantic_fp
          WHERE c."Token"=command_token AND NOT EXISTS (SELECT 1 FROM nexa.rev869b_command_security_audits x
            WHERE x."GrantId"=g."GrantId" AND x."CommandFingerprint"=semantic_fp AND x."EventType"='Claimed');
          IF NOT FOUND THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_command_audit_claim_required',MESSAGE='Durable command audit claim could not be appended.';
          END IF;
        END $rev869b$;
        REVOKE ALL ON FUNCTION nexa.rev869b_claim_command_context(text,uuid,text,uuid,text,bigint,text,text,text,text) FROM PUBLIC;

        CREATE OR REPLACE FUNCTION nexa.rev869b_record_command_outcome(
          grant_id uuid, terminal_event text, failure_category text)
        RETURNS integer LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,nexa AS $rev869b$
        DECLARE inserted_count integer;
        BEGIN
          IF terminal_event NOT IN ('Committed','Failed','Rejected') OR
             (terminal_event='Committed' AND nullif(trim(coalesce(failure_category,'')),'') IS NOT NULL) OR
             (terminal_event IN ('Failed','Rejected') AND failure_category NOT IN
               ('ContextOpenRejected','IdempotentReplayOrExplicitRollback','BusinessTransactionRolledBack')) THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_exact_command_terminal_outcome',
              MESSAGE='An exact minimized terminal command outcome is required.';
          END IF;
          IF NOT EXISTS (SELECT 1 FROM nexa.rev869b_command_grants g WHERE g."GrantId"=grant_id AND
              ((terminal_event='Committed' AND g."RuntimePrincipal"=session_user AND EXISTS (
                  SELECT 1 FROM nexa.rev869b_command_contexts c WHERE c."GrantId"=g."GrantId"
                    AND c."BackendPid"=pg_backend_pid() AND c."TransactionId"=txid_current()))
               OR (terminal_event IN ('Failed','Rejected') AND g."IssuerPrincipal"=session_user))) THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_command_outcome_principal_binding',
              MESSAGE='Terminal outcome principal, backend, transaction, or grant binding failed.';
          END IF;
          INSERT INTO nexa.rev869b_command_security_audits
            ("AuditId","GrantId","EventId","EventType","CommandFingerprint","OrganizationFingerprint","ActorFingerprint",
             "IssuerPrincipal","Operation","EntityType","EntityId","ExpectedVersion","SourceStatus","TargetStatus",
             "CorrelationFingerprint","OccurredAt","Outcome","FailureCategory","PolicyVersion")
          SELECT gen_random_uuid(),a."GrantId",gen_random_uuid(),terminal_event,a."CommandFingerprint",
            a."OrganizationFingerprint",a."ActorFingerprint",a."IssuerPrincipal",a."Operation",a."EntityType",
            a."EntityId",a."ExpectedVersion",a."SourceStatus",a."TargetStatus",a."CorrelationFingerprint",
            clock_timestamp(),CASE terminal_event WHEN 'Committed' THEN 'Protected command committed'
              WHEN 'Rejected' THEN 'Protected command rejected before commit' ELSE 'Protected command failed and rolled back' END,
            nullif(trim(coalesce(failure_category,'')),''),a."PolicyVersion"
          FROM nexa.rev869b_command_security_audits a
          WHERE a."GrantId"=grant_id AND a."EventType"='Issued'
            AND (terminal_event<>'Committed' OR EXISTS (SELECT 1 FROM nexa.rev869b_command_security_audits c
              WHERE c."GrantId"=a."GrantId" AND c."CommandFingerprint"=a."CommandFingerprint" AND c."EventType"='Claimed'))
            AND NOT EXISTS (SELECT 1 FROM nexa.rev869b_command_security_audits t
              WHERE t."GrantId"=a."GrantId" AND t."CommandFingerprint"=a."CommandFingerprint"
                AND t."EventType" IN ('Committed','Failed','Rejected'));
          GET DIAGNOSTICS inserted_count=ROW_COUNT;
          IF inserted_count=0 THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_command_terminal_outcome_missing_or_replayed',
              MESSAGE='No exact nonterminal command slot was available for terminal outcome.';
          END IF;
          RETURN inserted_count;
        END $rev869b$;
        REVOKE ALL ON FUNCTION nexa.rev869b_record_command_outcome(uuid,text,text) FROM PUBLIC;

        CREATE OR REPLACE FUNCTION nexa.rev869b_provision_command_authority(issuer_principal name, runtime_principal name, active_to timestamptz DEFAULT NULL)
        RETURNS void LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,nexa AS $rev869b$
        BEGIN
          IF session_user IS DISTINCT FROM (SELECT pg_get_userbyid(d.datdba) FROM pg_database d WHERE d.datname=current_database()) OR
             issuer_principal IS NULL OR runtime_principal IS NULL OR issuer_principal=runtime_principal OR
             issuer_principal=session_user OR runtime_principal=session_user OR
             NOT EXISTS (SELECT 1 FROM pg_roles r WHERE r.rolname=issuer_principal AND r.rolcanlogin) OR
             NOT EXISTS (SELECT 1 FROM pg_roles r WHERE r.rolname=runtime_principal AND r.rolcanlogin) THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_command_authority_provisioning',MESSAGE='Database owner must provision distinct non-owner LOGIN issuer and runtime principals.';
          END IF;
          UPDATE nexa.rev869b_command_authorities SET "RevokedAt"=clock_timestamp()
           WHERE "IssuerPrincipal"=issuer_principal AND "RuntimePrincipal"=runtime_principal AND "RevokedAt" IS NULL;
          INSERT INTO nexa.rev869b_command_authorities("KeyId","IssuerPrincipal","RuntimePrincipal","ActiveFrom","ActiveTo")
          VALUES(gen_random_uuid(),issuer_principal,runtime_principal,clock_timestamp(),active_to);
          EXECUTE format('GRANT EXECUTE ON FUNCTION nexa.rev869b_issue_command_grant(name,integer,bigint,uuid,text,text,text,text,bigint,jsonb) TO %I',issuer_principal);
          EXECUTE format('GRANT EXECUTE ON FUNCTION nexa.rev869b_open_command_context(uuid,uuid,text,text,text,text,integer,bigint) TO %I',runtime_principal);
          EXECUTE format('GRANT EXECUTE ON FUNCTION nexa.rev869b_command_context_valid(text,uuid,text,text,text) TO %I',runtime_principal);
          EXECUTE format('GRANT EXECUTE ON FUNCTION nexa.rev869b_claim_command_context(text,uuid,text,uuid,text,bigint,text,text,text,text) TO %I',runtime_principal);
          EXECUTE format('GRANT EXECUTE ON FUNCTION nexa.rev869b_record_command_outcome(uuid,text,text) TO %I',runtime_principal);
          EXECUTE format('GRANT EXECUTE ON FUNCTION nexa.rev869b_record_command_outcome(uuid,text,text) TO %I',issuer_principal);
        END $rev869b$;
        REVOKE ALL ON FUNCTION nexa.rev869b_provision_command_authority(name,name,timestamptz) FROM PUBLIC;
        DO $rev869b_grant_owner$
        BEGIN
          EXECUTE format('GRANT EXECUTE ON FUNCTION nexa.rev869b_provision_command_authority(name,name,timestamptz) TO %I',current_user);
        END $rev869b_grant_owner$;

        ALTER TABLE nexa.rev869b_command_authorities OWNER TO nexa_rev869b_security_owner;
        ALTER TABLE nexa.rev869b_command_grants OWNER TO nexa_rev869b_security_owner;
        ALTER TABLE nexa.rev869b_command_contexts OWNER TO nexa_rev869b_security_owner;
        ALTER TABLE nexa.rev869b_claim_sequence_pool OWNER TO nexa_rev869b_security_owner;
        ALTER TABLE nexa.rev869b_command_security_audits OWNER TO nexa_rev869b_security_owner;
        ALTER TABLE nexa.rev869b_purge_authorizations OWNER TO nexa_rev869b_security_owner;
        ALTER TABLE nexa.rev869b_purge_attempt_audits OWNER TO nexa_rev869b_security_owner;
        ALTER TABLE nexa.rev869b_purge_rejection_audits OWNER TO nexa_rev869b_security_owner;
        ALTER FUNCTION nexa.rev869b_reject_security_audit_mutation() OWNER TO nexa_rev869b_security_owner;
        ALTER FUNCTION nexa.rev869b_slot_fingerprint(text,uuid,text,uuid,text,bigint,text,text,uuid,text,text,text,text,text,text,boolean) OWNER TO nexa_rev869b_security_owner;
        ALTER FUNCTION nexa.rev869b_issue_command_grant(name,integer,bigint,uuid,text,text,text,text,bigint,jsonb) OWNER TO nexa_rev869b_security_owner;
        ALTER FUNCTION nexa.rev869b_open_command_context(uuid,uuid,text,text,text,text,integer,bigint) OWNER TO nexa_rev869b_security_owner;
        ALTER FUNCTION nexa.rev869b_command_context_valid(text,uuid,text,text,text) OWNER TO nexa_rev869b_security_owner;
        ALTER FUNCTION nexa.rev869b_claim_command_context(text,uuid,text,uuid,text,bigint,text,text,text,text) OWNER TO nexa_rev869b_security_owner;
        ALTER FUNCTION nexa.rev869b_record_command_outcome(uuid,text,text) OWNER TO nexa_rev869b_security_owner;
        ALTER FUNCTION nexa.rev869b_provision_command_authority(name,name,timestamptz) OWNER TO nexa_rev869b_security_owner;
        ALTER FUNCTION nexa.rev869b_register_purge_authorization(uuid,text,bytea,bytea,timestamptz,integer,integer,text[],timestamptz,timestamptz,bytea,text,text) OWNER TO nexa_rev869b_security_owner;
        ALTER FUNCTION nexa.rev869b_begin_purge_execution(uuid,bytea) OWNER TO nexa_rev869b_security_owner;
        ALTER FUNCTION nexa.rev869b_purge_temporary_security_ledger(uuid) OWNER TO nexa_rev869b_security_owner;
        DO $rev869b_grant_purge_owner$
        BEGIN
          REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA nexa FROM nexa_rev869b_purge_executor,nexa_rev869b_purge_authorizer;
          REVOKE EXECUTE ON ALL FUNCTIONS IN SCHEMA nexa FROM nexa_rev869b_purge_executor,nexa_rev869b_purge_authorizer;
          REVOKE ALL ON SCHEMA nexa FROM nexa_rev869b_purge_executor,nexa_rev869b_purge_authorizer;
          GRANT USAGE ON SCHEMA nexa TO nexa_rev869b_purge_executor,nexa_rev869b_purge_authorizer;
          GRANT EXECUTE ON FUNCTION nexa.rev869b_register_purge_authorization(uuid,text,bytea,bytea,timestamptz,integer,integer,text[],timestamptz,timestamptz,bytea,text,text) TO nexa_rev869b_purge_authorizer;
          GRANT EXECUTE ON FUNCTION nexa.rev869b_begin_purge_execution(uuid,bytea) TO nexa_rev869b_purge_executor;
          GRANT EXECUTE ON FUNCTION nexa.rev869b_purge_temporary_security_ledger(uuid) TO nexa_rev869b_purge_executor;
          GRANT USAGE ON SCHEMA nexa TO nexa_rev869b_security_owner;
          GRANT SELECT ON nexa.employee_identity_mappings,nexa.employees,nexa.employee_role_assignments,nexa.roles
            TO nexa_rev869b_security_owner;
        END $rev869b_grant_purge_owner$;
        REVOKE ALL ON nexa.rev869b_command_authorities,nexa.rev869b_command_grants,nexa.rev869b_command_contexts,
          nexa.rev869b_claim_sequence_pool,nexa.rev869b_command_security_audits,nexa.rev869b_purge_authorizations,
          nexa.rev869b_purge_attempt_audits,nexa.rev869b_purge_rejection_audits
          FROM nexa_rev869b_purge_executor,nexa_rev869b_purge_authorizer;
        DO $rev869b_acl_closure$
        BEGIN
          IF has_schema_privilege('nexa_rev869b_purge_executor','nexa','CREATE') OR
             has_schema_privilege('nexa_rev869b_purge_authorizer','nexa','CREATE') OR
             has_table_privilege('nexa_rev869b_purge_executor','nexa.rev869b_command_grants','SELECT,INSERT,UPDATE,DELETE') OR
             has_table_privilege('nexa_rev869b_purge_authorizer','nexa.rev869b_purge_authorizations','SELECT,INSERT,UPDATE,DELETE') OR
             has_function_privilege('nexa_rev869b_purge_executor','nexa.rev869b_register_purge_authorization(uuid,text,bytea,bytea,timestamp with time zone,integer,integer,text[],timestamp with time zone,timestamp with time zone,bytea,text,text)','EXECUTE') OR
             has_function_privilege('nexa_rev869b_purge_authorizer','nexa.rev869b_begin_purge_execution(uuid,bytea)','EXECUTE') OR
             NOT has_function_privilege('nexa_rev869b_purge_executor','nexa.rev869b_begin_purge_execution(uuid,bytea)','EXECUTE') OR
             NOT has_function_privilege('nexa_rev869b_purge_executor','nexa.rev869b_purge_temporary_security_ledger(uuid)','EXECUTE') OR
             NOT has_function_privilege('nexa_rev869b_purge_authorizer','nexa.rev869b_register_purge_authorization(uuid,text,bytea,bytea,timestamp with time zone,integer,integer,text[],timestamp with time zone,timestamp with time zone,bytea,text,text)','EXECUTE') OR
             has_table_privilege('nexa_rev869b_security_owner','nexa.purchase_orders','SELECT,INSERT,UPDATE,DELETE') OR
             has_table_privilege('nexa_rev869b_security_owner','nexa.request_for_quotations','SELECT,INSERT,UPDATE,DELETE') OR
             NOT has_table_privilege('nexa_rev869b_security_owner','nexa.employee_identity_mappings','SELECT') OR
             NOT has_table_privilege('nexa_rev869b_security_owner','nexa.employee_role_assignments','SELECT') OR
             has_function_privilege('public','nexa.rev869b_purge_temporary_security_ledger(uuid)','EXECUTE') THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_security_role_acl_closure',
              MESSAGE='Exact purge/security role ACL closure failed.';
          END IF;
        END $rev869b_acl_closure$;
        """;

    public const string Remove = """
        DROP FUNCTION IF EXISTS nexa.rev869b_provision_command_authority(name,name,timestamptz);
        DROP FUNCTION IF EXISTS nexa.rev869b_purge_temporary_security_ledger(uuid);
        DROP FUNCTION IF EXISTS nexa.rev869b_begin_purge_execution(uuid,bytea);
        DROP FUNCTION IF EXISTS nexa.rev869b_register_purge_authorization(uuid,text,bytea,bytea,timestamptz,integer,integer,text[],timestamptz,timestamptz,bytea,text,text);
        DROP FUNCTION IF EXISTS nexa.rev869b_record_command_outcome(uuid,text,text);
        DROP FUNCTION IF EXISTS nexa.rev869b_claim_command_context(text,uuid,text,uuid,text,bigint,text,text,text,text);
        DROP FUNCTION IF EXISTS nexa.rev869b_command_context_valid(text,uuid,text,text,text);
        DROP FUNCTION IF EXISTS nexa.rev869b_open_command_context(uuid,uuid,text,text,text,text,integer,bigint);
        DROP FUNCTION IF EXISTS nexa.rev869b_issue_command_grant(name,integer,bigint,uuid,text,text,text,text,bigint,jsonb);
        DROP FUNCTION IF EXISTS nexa.rev869b_slot_fingerprint(text,uuid,text,uuid,text,bigint,text,text,uuid,text,text,text,text,text,text,boolean);
        DROP TRIGGER IF EXISTS "TR_rev869b_purge_attempt_audit_immutable" ON nexa.rev869b_purge_attempt_audits;
        DROP TRIGGER IF EXISTS "TR_rev869b_purge_rejection_audit_immutable" ON nexa.rev869b_purge_rejection_audits;
        DROP TRIGGER IF EXISTS "TR_rev869b_command_security_audit_immutable" ON nexa.rev869b_command_security_audits;
        DROP FUNCTION IF EXISTS nexa.rev869b_reject_security_audit_mutation();
        DROP TABLE IF EXISTS nexa.rev869b_purge_attempt_audits;
        DROP TABLE IF EXISTS nexa.rev869b_purge_rejection_audits;
        DROP TABLE IF EXISTS nexa.rev869b_purge_authorizations;
        DROP TABLE IF EXISTS nexa.rev869b_command_security_audits;
        DROP TABLE IF EXISTS nexa.rev869b_claim_sequence_pool;
        DO $rev869b_drop_claim_sequences$
        DECLARE sequence_name name; i integer;
        BEGIN
          FOR i IN 1..256 LOOP
            sequence_name:=('rev869b_claim_seq_'||lpad(i::text,3,'0'))::name;
            EXECUTE format('DROP SEQUENCE IF EXISTS nexa.%I',sequence_name);
          END LOOP;
        END $rev869b_drop_claim_sequences$;
        DROP TABLE IF EXISTS nexa.rev869b_command_contexts;
        DROP TABLE IF EXISTS nexa.rev869b_command_grants;
        DROP TABLE IF EXISTS nexa.rev869b_command_authorities;
        -- The shared pgcrypto extension and pre-provisioned NOLOGIN owner role are not migration-owned.
        """;
}
