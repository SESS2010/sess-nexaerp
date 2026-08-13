namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>
/// Private, signed REV869B command envelopes. A database principal is authorized only when an
/// independently provisioned 256-bit key for that exact principal verifies the OIDC-bound request.
/// The key is never embedded in source. Context selectors are transaction-local; retained replay
/// nonces and claims are purged after fifteen minutes and cannot authorize another transaction.
/// </summary>
internal static class Rev869BCommandContextSql
{
    public const string Install = """
        CREATE EXTENSION IF NOT EXISTS pgcrypto WITH SCHEMA public;
        DO $rev869b_extension$
        BEGIN
          IF to_regprocedure('public.hmac(bytea,bytea,text)') IS NULL THEN
            RAISE EXCEPTION USING ERRCODE='0A000',CONSTRAINT='rev869b_pgcrypto_hmac_required',
              MESSAGE='REV869B requires pgcrypto hmac(bytea,bytea,text) in the explicitly qualified public schema.';
          END IF;
        END $rev869b_extension$;

        CREATE TABLE nexa.rev869b_command_authorities(
          "KeyId" uuid PRIMARY KEY,
          "DatabasePrincipal" name NOT NULL,
          "Secret" bytea NOT NULL,
          "ActiveFrom" timestamptz NOT NULL,
          "ActiveTo" timestamptz NULL,
          "RevokedAt" timestamptz NULL,
          CONSTRAINT "CK_rev869b_command_authority_secret" CHECK (octet_length("Secret")=32),
          CONSTRAINT "CK_rev869b_command_authority_dates" CHECK ("ActiveTo" IS NULL OR "ActiveTo">="ActiveFrom")
        );
        CREATE UNIQUE INDEX "UX_rev869b_command_authority_active_principal"
          ON nexa.rev869b_command_authorities("DatabasePrincipal")
          WHERE "RevokedAt" IS NULL AND "ActiveTo" IS NULL;
        REVOKE ALL ON nexa.rev869b_command_authorities FROM PUBLIC;

        CREATE TABLE nexa.rev869b_command_contexts(
          "Token" uuid PRIMARY KEY,
          "BackendPid" integer NOT NULL,
          "TransactionId" bigint NOT NULL,
          "DatabasePrincipal" name NOT NULL,
          "OrganizationId" character varying(100) NOT NULL,
          "ActorEmployeeId" uuid NOT NULL,
          "IdentityIssuer" character varying(500) NOT NULL,
          "IdentitySubject" character varying(500) NOT NULL,
          "ActorRoleCode" character varying(100) NOT NULL,
          "Nonce" uuid NOT NULL UNIQUE,
          "AuthenticatedAt" timestamptz NOT NULL,
          "IssuedAt" timestamptz NOT NULL,
          "Claims" jsonb NOT NULL DEFAULT '[]'::jsonb,
          CONSTRAINT "CK_rev869b_command_context_claims" CHECK (jsonb_typeof("Claims")='array'),
          CONSTRAINT "CK_rev869b_command_context_freshness" CHECK ("IssuedAt">="AuthenticatedAt"-interval '30 seconds' AND "IssuedAt"<="AuthenticatedAt"+interval '30 seconds'),
          CONSTRAINT "UQ_rev869b_command_context_transaction" UNIQUE("BackendPid","TransactionId","Token")
        );
        REVOKE ALL ON nexa.rev869b_command_contexts FROM PUBLIC;

        CREATE OR REPLACE FUNCTION nexa.rev869b_open_command_context(
          actor_employee uuid, identity_issuer text, identity_subject text, actor_role text,
          organization text, authenticated_epoch_ms bigint, command_nonce uuid, signature_hex text)
        RETURNS uuid
        LANGUAGE plpgsql SECURITY DEFINER
        SET search_path=pg_catalog,nexa AS $rev869b$
        DECLARE
          command_token uuid:=gen_random_uuid();
          identity_count bigint;
          role_count bigint;
          authority_count bigint;
          signing_secret bytea;
          authenticated_at timestamptz:=to_timestamp(authenticated_epoch_ms/1000.0);
          canonical text;
          expected_signature bytea;
        BEGIN
          IF length(trim(coalesce(identity_issuer,'')))=0 OR length(trim(coalesce(identity_subject,'')))=0 OR
             length(trim(coalesce(actor_role,'')))=0 OR length(trim(coalesce(organization,'')))=0 OR
             signature_hex !~ '^[0-9A-Fa-f]{64}$' THEN
            RAISE EXCEPTION USING ERRCODE='42501',SCHEMA='nexa',TABLE='rev869b_command_contexts',
              CONSTRAINT='rev869b_command_principal_required',
              MESSAGE='A complete signed OIDC command principal is required.';
          END IF;
          IF abs(extract(epoch FROM (clock_timestamp()-authenticated_at)))>30 THEN
            RAISE EXCEPTION USING ERRCODE='42501',SCHEMA='nexa',TABLE='rev869b_command_contexts',
              CONSTRAINT='rev869b_command_signature_stale',
              MESSAGE='The signed command authentication time is stale or from the future.';
          END IF;
          SELECT count(*) INTO authority_count
          FROM nexa.rev869b_command_authorities a
          WHERE a."DatabasePrincipal"=session_user AND a."RevokedAt" IS NULL
            AND a."ActiveFrom"<=clock_timestamp() AND (a."ActiveTo" IS NULL OR a."ActiveTo">=clock_timestamp());
          IF authority_count<>1 THEN
            RAISE EXCEPTION USING ERRCODE='42501',SCHEMA='nexa',TABLE='rev869b_command_authorities',
              CONSTRAINT='rev869b_command_database_principal',
              MESSAGE='The current database principal has no exact active command authority.';
          END IF;
          SELECT a."Secret" INTO STRICT signing_secret
          FROM nexa.rev869b_command_authorities a
          WHERE a."DatabasePrincipal"=session_user AND a."RevokedAt" IS NULL
            AND a."ActiveFrom"<=clock_timestamp() AND (a."ActiveTo" IS NULL OR a."ActiveTo">=clock_timestamp());
          canonical:=format('%s|%s|%s|%s|%s|%s|%s',
            replace(actor_employee::text,'-',''),identity_issuer,identity_subject,actor_role,organization,
            authenticated_epoch_ms,replace(command_nonce::text,'-',''));
          expected_signature:=public.hmac(convert_to(canonical,'UTF8'),signing_secret,'sha256');
          IF expected_signature IS DISTINCT FROM decode(lower(signature_hex),'hex') THEN
            RAISE EXCEPTION USING ERRCODE='42501',SCHEMA='nexa',TABLE='rev869b_command_contexts',
              CONSTRAINT='rev869b_command_signature_invalid',
              MESSAGE='The command signature is invalid for the current database principal.';
          END IF;
          SELECT count(*) INTO identity_count
          FROM nexa.employee_identity_mappings m JOIN nexa.employees e ON e."Id"=m."EmployeeId"
          WHERE m."EmployeeId"=actor_employee AND m."Issuer"=identity_issuer AND
            m."Subject"=identity_subject AND m."OrganizationId"=organization AND m."IsActive" AND
            m."EffectiveFrom"<=transaction_timestamp()::date AND
            (m."EffectiveTo" IS NULL OR m."EffectiveTo">=transaction_timestamp()::date) AND
            e."Status"='Active' AND e."LoginEnabled";
          SELECT count(*) INTO role_count
          FROM nexa.employee_role_assignments a JOIN nexa.roles r ON r."Id"=a."RoleId"
          WHERE a."EmployeeId"=actor_employee AND r."Code"=actor_role AND r."IsActive" AND
            a."ApprovalStatus" IN ('Approved','SeedApproved') AND
            a."EffectiveFrom"<=transaction_timestamp()::date AND
            (a."EffectiveTo" IS NULL OR a."EffectiveTo">=transaction_timestamp()::date);
          IF identity_count<>1 OR role_count<>1 THEN
            RAISE EXCEPTION USING ERRCODE='42501',SCHEMA='nexa',TABLE='rev869b_command_contexts',
              CONSTRAINT='rev869b_command_principal_binding',
              MESSAGE='Command principal must resolve to one active issuer/subject employee identity and role.';
          END IF;
          DELETE FROM nexa.rev869b_command_contexts
          WHERE "IssuedAt"<clock_timestamp()-interval '15 minutes';
          INSERT INTO nexa.rev869b_command_contexts(
            "Token","BackendPid","TransactionId","DatabasePrincipal","OrganizationId","ActorEmployeeId",
            "IdentityIssuer","IdentitySubject","ActorRoleCode","Nonce","AuthenticatedAt","IssuedAt")
          VALUES(command_token,pg_backend_pid(),txid_current(),session_user,organization,actor_employee,
            identity_issuer,identity_subject,actor_role,command_nonce,authenticated_at,transaction_timestamp());
          PERFORM set_config('nexa.rev869b_command_token',command_token::text,true);
          PERFORM set_config('nexa.rev869b_actor_employee_id',actor_employee::text,true);
          PERFORM set_config('nexa.rev869b_actor_login',identity_subject,true);
          PERFORM set_config('nexa.rev869b_identity_issuer',identity_issuer,true);
          PERFORM set_config('nexa.rev869b_identity_subject',identity_subject,true);
          PERFORM set_config('nexa.rev869b_actor_role',actor_role,true);
          PERFORM set_config('nexa.rev869b_organization',organization,true);
          RETURN command_token;
        EXCEPTION WHEN unique_violation THEN
          RAISE EXCEPTION USING ERRCODE='42501',SCHEMA='nexa',TABLE='rev869b_command_contexts',
            CONSTRAINT='rev869b_command_nonce_reused',
            MESSAGE='The signed command nonce was already used.';
        END $rev869b$;
        REVOKE ALL ON FUNCTION nexa.rev869b_open_command_context(uuid,text,text,text,text,bigint,uuid,text) FROM PUBLIC;

        CREATE OR REPLACE FUNCTION nexa.rev869b_command_context_valid(
          organization text, actor_employee uuid, identity_issuer text, identity_subject text, actor_role text)
        RETURNS boolean
        LANGUAGE sql STABLE SECURITY DEFINER
        SET search_path=pg_catalog,nexa AS $rev869b$
          SELECT count(*)=1
          FROM nexa.rev869b_command_contexts c
          WHERE c."Token"=nullif(current_setting('nexa.rev869b_command_token',true),'')::uuid
            AND c."BackendPid"=pg_backend_pid() AND c."TransactionId"=txid_current()
            AND c."DatabasePrincipal"=session_user AND c."OrganizationId"=organization
            AND c."ActorEmployeeId"=actor_employee AND c."IdentityIssuer"=identity_issuer
            AND c."IdentitySubject"=identity_subject AND c."ActorRoleCode"=actor_role
            AND c."IssuedAt">=clock_timestamp()-interval '15 minutes'
        $rev869b$;
        REVOKE ALL ON FUNCTION nexa.rev869b_command_context_valid(text,uuid,text,text,text) FROM PUBLIC;

        CREATE OR REPLACE FUNCTION nexa.rev869b_claim_command_context(
          claim_kind text, history_id uuid, entity_type text, entity_id uuid, operation text,
          parent_version bigint, from_status text, to_status text, correlation text, remarks text)
        RETURNS void
        LANGUAGE plpgsql SECURITY DEFINER
        SET search_path=pg_catalog,nexa AS $rev869b$
        DECLARE command_token uuid; claim jsonb;
        BEGIN
          command_token:=nullif(current_setting('nexa.rev869b_command_token',true),'')::uuid;
          IF command_token IS NULL OR length(trim(coalesce(claim_kind,'')))=0 OR history_id IS NULL OR
             length(trim(coalesce(entity_type,'')))=0 OR length(trim(coalesce(operation,'')))=0 OR
             length(trim(coalesce(correlation,'')))=0 OR length(trim(coalesce(remarks,'')))=0 THEN
            RAISE EXCEPTION USING ERRCODE='42501',SCHEMA='nexa',TABLE='rev869b_command_contexts',
              CONSTRAINT='rev869b_command_claim_required',
              MESSAGE='A complete server-bound command claim is required.';
          END IF;
          claim:=jsonb_build_object(
            'claimKind',claim_kind,'historyId',history_id,'entityType',entity_type,'entityId',entity_id,
            'operation',operation,'parentVersion',parent_version,'fromStatus',from_status,'toStatus',to_status,
            'actorEmployeeId',current_setting('nexa.rev869b_actor_employee_id',true),
            'identityIssuer',current_setting('nexa.rev869b_identity_issuer',true),
            'identitySubject',current_setting('nexa.rev869b_identity_subject',true),
            'actorRole',current_setting('nexa.rev869b_actor_role',true),
            'organization',current_setting('nexa.rev869b_organization',true),
            'correlation',correlation,'remarks',remarks,'serverTransactionId',txid_current(),
            'serverTimestamp',transaction_timestamp());
          UPDATE nexa.rev869b_command_contexts c
          SET "Claims"=c."Claims"||jsonb_build_array(claim)
          WHERE c."Token"=command_token AND c."BackendPid"=pg_backend_pid() AND
            c."TransactionId"=txid_current() AND c."DatabasePrincipal"=session_user AND NOT EXISTS (
              SELECT 1 FROM jsonb_array_elements(c."Claims") prior
              WHERE (prior->>'claimKind'=claim_kind AND prior->>'historyId'=history_id::text) OR
                    (prior->>'claimKind'=claim_kind AND prior->>'entityType'=entity_type AND
                     prior->>'entityId'=entity_id::text AND prior->>'operation'=operation AND
                     (prior->>'parentVersion')::bigint=parent_version AND prior->>'correlation'=correlation));
          IF NOT FOUND THEN
            RAISE EXCEPTION USING ERRCODE='42501',SCHEMA='nexa',TABLE='rev869b_command_contexts',
              CONSTRAINT='rev869b_command_claim_stale_or_reused',
              MESSAGE='Command context is missing, stale, reused or bound to another exact history slot.';
          END IF;
        END $rev869b$;
        REVOKE ALL ON FUNCTION nexa.rev869b_claim_command_context(text,uuid,text,uuid,text,bigint,text,text,text,text) FROM PUBLIC;

        CREATE OR REPLACE FUNCTION nexa.rev869b_provision_command_authority(
          database_principal name, signing_secret bytea, active_to timestamptz DEFAULT NULL)
        RETURNS void
        LANGUAGE plpgsql SECURITY DEFINER
        SET search_path=pg_catalog,nexa AS $rev869b$
        BEGIN
          IF session_user IS DISTINCT FROM (
               SELECT pg_get_userbyid(d.datdba) FROM pg_database d WHERE d.datname=current_database()) OR
             octet_length(signing_secret)<>32 OR database_principal IS NULL OR
             NOT EXISTS (SELECT 1 FROM pg_roles r WHERE r.rolname=database_principal AND r.rolcanlogin) THEN
            RAISE EXCEPTION USING ERRCODE='42501',SCHEMA='nexa',TABLE='rev869b_command_authorities',
              CONSTRAINT='rev869b_command_authority_provisioning',
              MESSAGE='Only the database owner may provision a 256-bit key for an existing login principal.';
          END IF;
          UPDATE nexa.rev869b_command_authorities SET "RevokedAt"=clock_timestamp()
          WHERE "DatabasePrincipal"=database_principal AND "RevokedAt" IS NULL;
          INSERT INTO nexa.rev869b_command_authorities(
            "KeyId","DatabasePrincipal","Secret","ActiveFrom","ActiveTo")
          VALUES(gen_random_uuid(),database_principal,signing_secret,clock_timestamp(),active_to);
          EXECUTE format(
            'GRANT EXECUTE ON FUNCTION nexa.rev869b_open_command_context(uuid,text,text,text,text,bigint,uuid,text) TO %I',
            database_principal);
          EXECUTE format(
            'GRANT EXECUTE ON FUNCTION nexa.rev869b_command_context_valid(text,uuid,text,text,text) TO %I',
            database_principal);
          EXECUTE format(
            'GRANT EXECUTE ON FUNCTION nexa.rev869b_claim_command_context(text,uuid,text,uuid,text,bigint,text,text,text,text) TO %I',
            database_principal);
        END $rev869b$;
        REVOKE ALL ON FUNCTION nexa.rev869b_provision_command_authority(name,bytea,timestamptz) FROM PUBLIC;
        """;

    public const string Remove = """
        DROP FUNCTION IF EXISTS nexa.rev869b_provision_command_authority(name,bytea,timestamptz);
        DROP FUNCTION IF EXISTS nexa.rev869b_claim_command_context(text,uuid,text,uuid,text,bigint,text,text,text,text);
        DROP FUNCTION IF EXISTS nexa.rev869b_command_context_valid(text,uuid,text,text,text);
        DROP FUNCTION IF EXISTS nexa.rev869b_open_command_context(uuid,text,text,text,text,bigint,uuid,text);
        DROP TABLE IF EXISTS nexa.rev869b_command_contexts;
        DROP TABLE IF EXISTS nexa.rev869b_command_authorities;
        -- pgcrypto is a shared database extension and is deliberately not removed by REV869B Down.
        """;
}
