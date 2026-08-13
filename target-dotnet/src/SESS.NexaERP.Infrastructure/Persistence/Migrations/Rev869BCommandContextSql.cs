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
        DECLARE owner_can_login boolean;
        BEGIN
          SELECT r.rolcanlogin INTO owner_can_login FROM pg_roles r WHERE r.rolname='nexa_rev869b_security_owner';
          IF owner_can_login IS NULL OR owner_can_login OR
             NOT pg_has_role(current_user,'nexa_rev869b_security_owner','MEMBER') THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_dedicated_security_owner_required',
              MESSAGE='Pre-provisioned NOLOGIN role nexa_rev869b_security_owner and migration-owner membership are required.';
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

        CREATE OR REPLACE FUNCTION nexa.rev869b_purge_temporary_security_ledger(
          retention_days integer, max_rows integer, cleanup_reason text, cleanup_correlation text)
        RETURNS integer LANGUAGE plpgsql SECURITY DEFINER
        SET search_path=pg_catalog,nexa AS $rev869b$
        DECLARE grant_ids uuid[]; candidate_count integer:=0; deleted_count integer:=0; retained_audit_count bigint:=0;
        BEGIN
          IF retention_days<>90 OR max_rows NOT BETWEEN 1 AND 1000 OR
             length(trim(cleanup_reason)) NOT BETWEEN 1 AND 256 OR
             length(trim(cleanup_correlation)) NOT BETWEEN 1 AND 128 THEN
            RAISE EXCEPTION USING ERRCODE='22023',CONSTRAINT='rev869b_approved_temporary_retention',
              MESSAGE='MGMT-REV869B-SECURITY-LEDGER-20260813-001 requires ninety-day, bounded, auditable cleanup.';
          END IF;
          IF NOT pg_has_role(session_user,'nexa_rev869b_security_owner','MEMBER') THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_temporary_ledger_cleanup_owner',
              MESSAGE='Temporary security-ledger cleanup requires approved security-owner administration.';
          END IF;
          PERFORM pg_advisory_xact_lock(hashtextextended('MGMT-REV869B-SECURITY-LEDGER-20260813-001',0));
          SELECT array_agg(candidate."GrantId"),count(*) INTO grant_ids,candidate_count FROM (
            SELECT g."GrantId" FROM nexa.rev869b_command_grants g
            WHERE g."ReservedAt"<clock_timestamp()-make_interval(days=>retention_days)
              AND g."ExpiresAt"<=clock_timestamp()
            ORDER BY g."ReservedAt",g."GrantId" FOR UPDATE SKIP LOCKED LIMIT max_rows
          ) candidate;
          IF candidate_count=0 THEN RETURN 0; END IF;
          DELETE FROM nexa.rev869b_command_contexts c WHERE c."GrantId"=ANY(grant_ids);
          UPDATE nexa.rev869b_claim_sequence_pool p SET "GrantId"=NULL WHERE p."GrantId"=ANY(grant_ids);
          DELETE FROM nexa.rev869b_command_grants g WHERE g."GrantId"=ANY(grant_ids);
          GET DIAGNOSTICS deleted_count=ROW_COUNT;
          IF deleted_count<>candidate_count THEN
            RAISE EXCEPTION USING ERRCODE='P0001',CONSTRAINT='rev869b_temporary_purge_count_mismatch',
              MESSAGE='Temporary security-ledger purge count mismatch; transaction is rejected.';
          END IF;
          SELECT count(*) INTO retained_audit_count FROM nexa.audit_logs a
            WHERE a."CreatedAt">statement_timestamp()-interval '10 years';
          INSERT INTO nexa.audit_logs
            ("Id","Module","Action","EntityName","EntityId","UserLoginId","Result","CorrelationId","BeforeJson","AfterJson","CreatedAt","CreatedBy","Version")
          VALUES(public.gen_random_uuid(),'Security','PurgeTemporarySecurityLedger','Rev869BSecurityLedger',NULL,
            session_user,'Success',trim(cleanup_correlation),NULL,
            jsonb_build_object('approvalReference','MGMT-REV869B-SECURITY-LEDGER-20260813-001',
              'executor',session_user,'executedAt',statement_timestamp(),'eligibilityCutoff',statement_timestamp()-interval '90 days',
              'candidateCount',candidate_count,'deletedCount',deleted_count,'retainedAuditCount',retained_audit_count,
              'outcome','Success','reason',trim(cleanup_reason))::text,
            statement_timestamp(),session_user,0);
          RETURN deleted_count;
        EXCEPTION WHEN OTHERS THEN
          RAISE;
        END $rev869b$;
        REVOKE ALL ON FUNCTION nexa.rev869b_purge_temporary_security_ledger(integer,integer,text,text) FROM PUBLIC;

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
        END $rev869b$;
        REVOKE ALL ON FUNCTION nexa.rev869b_claim_command_context(text,uuid,text,uuid,text,bigint,text,text,text,text) FROM PUBLIC;

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
        ALTER FUNCTION nexa.rev869b_slot_fingerprint(text,uuid,text,uuid,text,bigint,text,text,uuid,text,text,text,text,text,text,boolean) OWNER TO nexa_rev869b_security_owner;
        ALTER FUNCTION nexa.rev869b_issue_command_grant(name,integer,bigint,uuid,text,text,text,text,bigint,jsonb) OWNER TO nexa_rev869b_security_owner;
        ALTER FUNCTION nexa.rev869b_open_command_context(uuid,uuid,text,text,text,text,integer,bigint) OWNER TO nexa_rev869b_security_owner;
        ALTER FUNCTION nexa.rev869b_command_context_valid(text,uuid,text,text,text) OWNER TO nexa_rev869b_security_owner;
        ALTER FUNCTION nexa.rev869b_claim_command_context(text,uuid,text,uuid,text,bigint,text,text,text,text) OWNER TO nexa_rev869b_security_owner;
        ALTER FUNCTION nexa.rev869b_provision_command_authority(name,name,timestamptz) OWNER TO nexa_rev869b_security_owner;
        ALTER FUNCTION nexa.rev869b_purge_temporary_security_ledger(integer,integer,text,text) OWNER TO nexa_rev869b_security_owner;
        DO $rev869b_grant_purge_owner$
        BEGIN
          EXECUTE format('GRANT EXECUTE ON FUNCTION nexa.rev869b_purge_temporary_security_ledger(integer,integer,text,text) TO %I',current_user);
        END $rev869b_grant_purge_owner$;
        """;

    public const string Remove = """
        DROP FUNCTION IF EXISTS nexa.rev869b_provision_command_authority(name,name,timestamptz);
        DROP FUNCTION IF EXISTS nexa.rev869b_purge_temporary_security_ledger(integer,integer,text,text);
        DROP FUNCTION IF EXISTS nexa.rev869b_claim_command_context(text,uuid,text,uuid,text,bigint,text,text,text,text);
        DROP FUNCTION IF EXISTS nexa.rev869b_command_context_valid(text,uuid,text,text,text);
        DROP FUNCTION IF EXISTS nexa.rev869b_open_command_context(uuid,uuid,text,text,text,text,integer,bigint);
        DROP FUNCTION IF EXISTS nexa.rev869b_issue_command_grant(name,integer,bigint,uuid,text,text,text,text,bigint,jsonb);
        DROP FUNCTION IF EXISTS nexa.rev869b_slot_fingerprint(text,uuid,text,uuid,text,bigint,text,text,uuid,text,text,text,text,text,text,boolean);
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
