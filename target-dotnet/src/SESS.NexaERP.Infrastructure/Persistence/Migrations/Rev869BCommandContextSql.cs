namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>
/// Database-private REV869B command envelopes. The table is intentionally not EF-mapped: application
/// code cannot perform ordinary CRUD against it. The only writer is the retained migration's
/// SECURITY DEFINER entry point, and every envelope is bound to one backend and transaction ID.
/// The transaction-local token selector clears automatically on commit/rollback; persisted envelopes
/// are inert outside their issuing transaction and provide sanitized provenance for later review.
/// </summary>
internal static class Rev869BCommandContextSql
{
    public const string Install = """
        CREATE TABLE nexa.rev869b_command_contexts(
          "Token" uuid PRIMARY KEY,
          "BackendPid" integer NOT NULL,
          "TransactionId" bigint NOT NULL,
          "OrganizationId" character varying(100) NOT NULL,
          "ActorEmployeeId" uuid NOT NULL,
          "ActorLoginId" character varying(256) NOT NULL,
          "ActorRoleCode" character varying(100) NOT NULL,
          "IssuedAt" timestamptz NOT NULL,
          "Claims" jsonb NOT NULL DEFAULT '[]'::jsonb,
          CONSTRAINT "CK_rev869b_command_context_claims" CHECK (jsonb_typeof("Claims")='array'),
          CONSTRAINT "UQ_rev869b_command_context_transaction" UNIQUE("BackendPid","TransactionId","Token")
        );
        REVOKE ALL ON nexa.rev869b_command_contexts FROM PUBLIC;

        CREATE OR REPLACE FUNCTION nexa.rev869b_open_command_context(
          actor_employee uuid, actor_login text, actor_role text, organization text)
        RETURNS uuid
        LANGUAGE plpgsql SECURITY DEFINER
        SET search_path=pg_catalog,nexa AS $rev869b$
        DECLARE command_token uuid:=gen_random_uuid(); identity_count bigint; role_count bigint;
        BEGIN
          IF length(trim(coalesce(actor_login,'')))=0 OR length(trim(coalesce(actor_role,'')))=0 OR
             length(trim(coalesce(organization,'')))=0 THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_command_principal_required',
              MESSAGE='A complete authenticated command principal is required.';
          END IF;
          SELECT count(*) INTO identity_count
          FROM nexa.employee_identity_mappings m JOIN nexa.employees e ON e."Id"=m."EmployeeId"
          WHERE m."EmployeeId"=actor_employee AND m."Subject"=actor_login AND
            m."OrganizationId"=organization AND m."IsActive" AND
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
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_command_principal_binding',
              MESSAGE='Command principal must resolve to one active employee identity and role.';
          END IF;
          INSERT INTO nexa.rev869b_command_contexts(
            "Token","BackendPid","TransactionId","OrganizationId","ActorEmployeeId",
            "ActorLoginId","ActorRoleCode","IssuedAt")
          VALUES(command_token,pg_backend_pid(),txid_current(),organization,actor_employee,
            actor_login,actor_role,transaction_timestamp());
          PERFORM set_config('nexa.rev869b_command_token',command_token::text,true);
          PERFORM set_config('nexa.rev869b_actor_employee_id',actor_employee::text,true);
          PERFORM set_config('nexa.rev869b_actor_login',actor_login,true);
          PERFORM set_config('nexa.rev869b_actor_role',actor_role,true);
          PERFORM set_config('nexa.rev869b_organization',organization,true);
          RETURN command_token;
        END $rev869b$;
        REVOKE ALL ON FUNCTION nexa.rev869b_open_command_context(uuid,text,text,text) FROM PUBLIC;

        CREATE OR REPLACE FUNCTION nexa.rev869b_command_context_valid(
          organization text, actor_employee uuid, actor_login text, actor_role text)
        RETURNS boolean
        LANGUAGE sql STABLE SECURITY DEFINER
        SET search_path=pg_catalog,nexa AS $rev869b$
          SELECT count(*)=1
          FROM nexa.rev869b_command_contexts c
          WHERE c."Token"=nullif(current_setting('nexa.rev869b_command_token',true),'')::uuid
            AND c."BackendPid"=pg_backend_pid() AND c."TransactionId"=txid_current()
            AND c."OrganizationId"=organization AND c."ActorEmployeeId"=actor_employee
            AND c."ActorLoginId"=actor_login AND c."ActorRoleCode"=actor_role
        $rev869b$;
        REVOKE ALL ON FUNCTION nexa.rev869b_command_context_valid(text,uuid,text,text) FROM PUBLIC;

        CREATE OR REPLACE FUNCTION nexa.rev869b_claim_command_context(
          entity_type text, entity_id uuid, operation text, parent_version bigint,
          from_status text, to_status text, correlation text, remarks text)
        RETURNS void
        LANGUAGE plpgsql SECURITY DEFINER
        SET search_path=pg_catalog,nexa AS $rev869b$
        DECLARE command_token uuid; claim jsonb;
        BEGIN
          command_token:=nullif(current_setting('nexa.rev869b_command_token',true),'')::uuid;
          IF command_token IS NULL OR length(trim(coalesce(entity_type,'')))=0 OR
             length(trim(coalesce(operation,'')))=0 OR length(trim(coalesce(correlation,'')))=0 OR
             length(trim(coalesce(remarks,'')))=0 THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_command_claim_required',
              MESSAGE='A complete server-bound command claim is required.';
          END IF;
          claim:=jsonb_build_object(
            'entityType',entity_type,'entityId',entity_id,'operation',operation,
            'parentVersion',parent_version,'fromStatus',from_status,'toStatus',to_status,
            'correlation',correlation,'remarks',remarks,'serverTransactionId',txid_current(),
            'serverTimestamp',transaction_timestamp());
          UPDATE nexa.rev869b_command_contexts c
          SET "Claims"=c."Claims"||jsonb_build_array(claim)
          WHERE c."Token"=command_token AND c."BackendPid"=pg_backend_pid() AND
            c."TransactionId"=txid_current() AND NOT EXISTS (
              SELECT 1 FROM jsonb_array_elements(c."Claims") prior
              WHERE prior->>'entityType'=entity_type AND prior->>'entityId'=entity_id::text AND
                prior->>'correlation'=correlation);
          IF NOT FOUND THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_command_claim_stale_or_reused',
              MESSAGE='Command context is missing, stale, reused or bound to another entity.';
          END IF;
        END $rev869b$;
        REVOKE ALL ON FUNCTION nexa.rev869b_claim_command_context(text,uuid,text,bigint,text,text,text,text) FROM PUBLIC;
        """;

    public const string Remove = """
        DROP FUNCTION IF EXISTS nexa.rev869b_claim_command_context(text,uuid,text,bigint,text,text,text,text);
        DROP FUNCTION IF EXISTS nexa.rev869b_command_context_valid(text,uuid,text,text);
        DROP FUNCTION IF EXISTS nexa.rev869b_open_command_context(uuid,text,text,text);
        DROP TABLE IF EXISTS nexa.rev869b_command_contexts;
        """;
}
