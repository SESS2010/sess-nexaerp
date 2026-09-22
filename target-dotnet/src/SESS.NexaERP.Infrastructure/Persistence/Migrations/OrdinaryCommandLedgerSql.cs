namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class OrdinaryCommandLedgerSql
{
    internal static string Up => UpCore + "\n" +
        Rev869BControlledMutationSql.OrdinaryPurchaseAuthority + "\n" +
        OrdinaryTaxAuthority + "\n" + Rev869BControlledMutationSql.OrdinaryQualificationAuthority +
        "\n" + StoresGrnSlice2Sql.BuiltInHashGoodsReceiptGuard + "\n" +
        Foundation3InventoryProvenanceGenealogySql.BuiltInHashGrnLegPreparation + "\n" +
        ControlledAuthorityOwnership;

    private const string UpCore = """
        DO $ordinary_command_preflight$
        DECLARE managed_role_count integer;
        BEGIN
          SELECT count(*) INTO managed_role_count FROM pg_roles
           WHERE rolname IN ('nexa_erp_owner','nexa_erp_migration','nexa_erp_bootstrap','nexa_erp_runtime');
          IF managed_role_count NOT IN (0,4) THEN
            RAISE EXCEPTION USING ERRCODE='55000',
              MESSAGE='Refusing ordinary command-ledger installation: the ordinary database-principal set is partial.';
          END IF;
          IF to_regprocedure('advance.guard_audit_logs_immutable()') IS NULL OR NOT EXISTS (
            SELECT 1 FROM pg_trigger t
            JOIN pg_class c ON c.oid=t.tgrelid
            JOIN pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname='advance' AND c.relname='audit_logs'
              AND t.tgname='TR_audit_logs_immutable' AND NOT t.tgisinternal
              AND t.tgenabled<>'D'
              AND t.tgfoid=to_regprocedure('advance.guard_audit_logs_immutable()')) THEN
            RAISE EXCEPTION USING ERRCODE='55000',
              MESSAGE='Ordinary immutable-audit guard must be installed before the command ledger.';
          END IF;
        END $ordinary_command_preflight$;

        CREATE TABLE advance.command_requests (
          "CommandId" uuid NOT NULL,
          "OrganizationId" character varying(100) NOT NULL,
          "Operation" character varying(100) NOT NULL,
          "IdempotencyKeySha256" bytea NOT NULL,
          "RequestSha256" bytea NOT NULL,
          "ActorEmployeeId" uuid NOT NULL,
          "IdentityIssuer" character varying(500) NOT NULL,
          "IdentitySubject" character varying(500) NOT NULL,
          "ActorRoleCode" character varying(100) NOT NULL,
          "ResolvedRoleAssignmentId" uuid NOT NULL,
          "RegisteredAt" timestamp with time zone NOT NULL,
          "RegisteredBy" character varying(100) NOT NULL,
          CONSTRAINT "PK_command_requests" PRIMARY KEY ("CommandId"),
          CONSTRAINT "CK_command_request_hashes" CHECK (octet_length("IdempotencyKeySha256")=32 AND octet_length("RequestSha256")=32),
          CONSTRAINT "FK_command_request_actor" FOREIGN KEY ("ActorEmployeeId") REFERENCES advance.employees("Id") ON DELETE RESTRICT,
          CONSTRAINT "FK_command_request_assignment" FOREIGN KEY ("ResolvedRoleAssignmentId") REFERENCES advance.employee_role_assignments("Id") ON DELETE RESTRICT,
          CONSTRAINT "UQ_command_request_idempotency" UNIQUE ("OrganizationId","Operation","IdempotencyKeySha256")
        );

        CREATE TABLE advance.command_receipts (
          "ReceiptId" uuid NOT NULL,
          "CommandId" uuid NOT NULL,
          "BusinessFingerprint" bytea NOT NULL,
          "ResponseJson" jsonb NOT NULL,
          "CommittedAt" timestamp with time zone NOT NULL,
          "CommittedBy" character varying(100) NOT NULL,
          CONSTRAINT "PK_command_receipts" PRIMARY KEY ("ReceiptId"),
          CONSTRAINT "UQ_command_receipt_command" UNIQUE ("CommandId"),
          CONSTRAINT "CK_command_receipt_fingerprint" CHECK (octet_length("BusinessFingerprint")=32),
          CONSTRAINT "FK_command_receipt_request" FOREIGN KEY ("CommandId") REFERENCES advance.command_requests("CommandId") ON DELETE RESTRICT
        );
        CREATE FUNCTION advance.guard_command_request_immutable()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $ordinary_command$
        BEGIN
          IF TG_OP<>'INSERT' THEN
            RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Command request evidence is immutable.';
          END IF;
          IF current_setting('sess.ordinary_command_ledger_write',true) IS DISTINCT FROM txid_current()::text THEN
            RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Command requests may be registered only through the controlled function.';
          END IF;
          RETURN NEW;
        END $ordinary_command$;
        CREATE TRIGGER "TR_command_request_immutable"
          BEFORE INSERT OR UPDATE OR DELETE ON advance.command_requests
          FOR EACH ROW EXECUTE FUNCTION advance.guard_command_request_immutable();

        CREATE FUNCTION advance.guard_command_receipt_immutable()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $ordinary_command$
        BEGIN
          IF TG_OP<>'INSERT' THEN
            RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Command receipts are immutable.';
          END IF;
          IF current_setting('sess.ordinary_command_ledger_write',true) IS DISTINCT FROM txid_current()::text THEN
            RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Command receipts may be committed only through the controlled function.';
          END IF;
          RETURN NEW;
        END $ordinary_command$;
        CREATE TRIGGER "TR_command_receipt_immutable"
          BEFORE INSERT OR UPDATE OR DELETE ON advance.command_receipts
          FOR EACH ROW EXECUTE FUNCTION advance.guard_command_receipt_immutable();

        CREATE FUNCTION advance.ordinary_command_context_valid(
          organization text,actor_employee uuid,identity_issuer text,identity_subject text,actor_role text)
        RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER
        SET search_path=pg_catalog,advance AS $ordinary_command$
          SELECT session_user='nexa_erp_runtime'
             AND current_setting('advance.ordinary_command_id',true) IS NOT NULL
             AND EXISTS (
               SELECT 1 FROM advance.command_requests r
               WHERE r."CommandId"=nullif(current_setting('advance.ordinary_command_id',true),'')::uuid
                 AND r."OrganizationId"=organization AND r."ActorEmployeeId"=actor_employee
                 AND r."IdentityIssuer"=identity_issuer AND r."IdentitySubject"=identity_subject
                 AND r."ActorRoleCode"=actor_role
                 AND r.xmin::text::bigint=txid_current());
        $ordinary_command$;

        CREATE FUNCTION advance.ordinary_claim_command_context(
          claim_kind text,history_id uuid,entity_type text,entity_id uuid,operation text,
          parent_version bigint,from_status text,to_status text,correlation text,remarks text)
        RETURNS void LANGUAGE plpgsql SECURITY DEFINER
        SET search_path=pg_catalog,advance AS $ordinary_command$
        BEGIN
          IF session_user<>'nexa_erp_runtime' OR nullif(current_setting('advance.ordinary_command_id',true),'') IS NULL
             OR history_id IS NULL OR entity_id IS NULL OR btrim(coalesce(claim_kind,''))=''
             OR btrim(coalesce(entity_type,''))='' OR btrim(coalesce(operation,''))=''
             OR btrim(coalesce(correlation,''))='' OR btrim(coalesce(remarks,''))='' THEN
            RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Controlled history requires the current ordinary command transaction.';
          END IF;
        END $ordinary_command$;

        CREATE FUNCTION advance.register_command_request(
          organization text,operation text,idempotency_sha bytea,request_sha bytea,
          actor_employee uuid,identity_issuer text,identity_subject text,
          actor_role text,actor_assignment uuid)
        RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER
        SET search_path=pg_catalog,advance AS $ordinary_command$
        DECLARE
          command_id uuid;
          existing advance.command_requests%ROWTYPE;
          company_id uuid;
          runtime_role record;
        BEGIN
          SELECT * INTO runtime_role FROM pg_roles WHERE rolname='nexa_erp_runtime';
          IF NOT FOUND OR session_user<>'nexa_erp_runtime' OR NOT runtime_role.rolcanlogin
             OR runtime_role.rolsuper OR runtime_role.rolcreatedb OR runtime_role.rolcreaterole
             OR runtime_role.rolreplication OR runtime_role.rolbypassrls
             OR pg_has_role(session_user,'nexa_erp_owner','MEMBER') THEN
            RAISE EXCEPTION USING ERRCODE='42501',
              MESSAGE='Ordinary command registration requires the exact non-privileged nexa_erp_runtime session principal.';
          END IF;
          IF btrim(coalesce(organization,''))='' OR btrim(coalesce(operation,''))=''
             OR btrim(coalesce(identity_issuer,''))='' OR btrim(coalesce(identity_subject,''))=''
             OR btrim(coalesce(actor_role,''))='' OR octet_length(idempotency_sha)<>32
             OR octet_length(request_sha)<>32 THEN
            RAISE EXCEPTION USING ERRCODE='22023',MESSAGE='Complete assignment-bound command evidence is required.';
          END IF;

          SELECT "Id" INTO STRICT company_id FROM advance.companies
           WHERE "Code"=organization AND "IsActive" AND "Status"='ACTIVE';
          IF NOT EXISTS (
            SELECT 1 FROM advance.resolve_employee_role_authority(
              actor_employee,company_id,CURRENT_DATE,operation,ARRAY[actor_role]) resolved
            WHERE resolved."AssignmentId"=actor_assignment AND resolved."RoleCode"=actor_role) THEN
            RAISE EXCEPTION USING ERRCODE='42501',
              MESSAGE='Resolved command role assignment is not effective for this employee, company and date.';
          END IF;

          SELECT * INTO existing FROM advance.command_requests r
           WHERE r."OrganizationId"=organization AND r."Operation"=operation
             AND r."IdempotencyKeySha256"=idempotency_sha FOR UPDATE;
          IF FOUND THEN
            IF existing."RequestSha256"<>request_sha OR existing."ActorEmployeeId"<>actor_employee
               OR existing."IdentityIssuer"<>identity_issuer OR existing."IdentitySubject"<>identity_subject
               OR existing."ActorRoleCode"<>actor_role
               OR existing."ResolvedRoleAssignmentId"<>actor_assignment THEN
              RAISE EXCEPTION USING ERRCODE='23505',CONSTRAINT='command_request_replay_mismatch',
                MESSAGE='Idempotency key reuse mismatch.';
            END IF;
            PERFORM set_config('advance.ordinary_command_id',existing."CommandId"::text,true);
            PERFORM set_config('advance.ordinary_actor_employee_id',actor_employee::text,true);
            PERFORM set_config('advance.ordinary_actor_login',identity_subject,true);
            PERFORM set_config('advance.ordinary_identity_issuer',identity_issuer,true);
            PERFORM set_config('advance.ordinary_identity_subject',identity_subject,true);
            PERFORM set_config('advance.ordinary_actor_role',actor_role,true);
            PERFORM set_config('advance.ordinary_organization',organization,true);
            RETURN existing."CommandId";
          END IF;

          command_id:=gen_random_uuid();
          PERFORM set_config('sess.ordinary_command_ledger_write',txid_current()::text,true);
          INSERT INTO advance.command_requests
            ("CommandId","OrganizationId","Operation","IdempotencyKeySha256","RequestSha256",
             "ActorEmployeeId","IdentityIssuer","IdentitySubject","ActorRoleCode",
             "ResolvedRoleAssignmentId","RegisteredAt","RegisteredBy")
          VALUES(command_id,organization,operation,idempotency_sha,request_sha,actor_employee,
            identity_issuer,identity_subject,actor_role,actor_assignment,statement_timestamp(),session_user);
          PERFORM set_config('advance.ordinary_command_id',command_id::text,true);
          PERFORM set_config('advance.ordinary_actor_employee_id',actor_employee::text,true);
          PERFORM set_config('advance.ordinary_actor_login',identity_subject,true);
          PERFORM set_config('advance.ordinary_identity_issuer',identity_issuer,true);
          PERFORM set_config('advance.ordinary_identity_subject',identity_subject,true);
          PERFORM set_config('advance.ordinary_actor_role',actor_role,true);
          PERFORM set_config('advance.ordinary_organization',organization,true);
          RETURN command_id;
        END $ordinary_command$;
        CREATE FUNCTION advance.commit_command_receipt(
          command_id uuid,business_sha bytea,response_json jsonb,receipt_id uuid)
        RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER
        SET search_path=pg_catalog,advance AS $ordinary_command$
        DECLARE existing advance.command_receipts%ROWTYPE;
        BEGIN
          IF session_user<>'nexa_erp_runtime' OR pg_has_role(session_user,'nexa_erp_owner','MEMBER') THEN
            RAISE EXCEPTION USING ERRCODE='42501',
              MESSAGE='Ordinary command receipt requires the exact non-owner nexa_erp_runtime session principal.';
          END IF;
          IF command_id IS NULL OR receipt_id IS NULL OR octet_length(business_sha)<>32
             OR response_json IS NULL OR jsonb_typeof(response_json)<>'object' THEN
            RAISE EXCEPTION USING ERRCODE='22023',MESSAGE='Complete command receipt evidence is required.';
          END IF;
          IF NOT EXISTS (SELECT 1 FROM advance.command_requests r WHERE r."CommandId"=command_id) THEN
            RAISE EXCEPTION USING ERRCODE='23503',MESSAGE='Command receipt requires its registered request.';
          END IF;
          SELECT * INTO existing FROM advance.command_receipts r
           WHERE r."CommandId"=command_id FOR UPDATE;
          IF FOUND THEN
            IF existing."BusinessFingerprint"<>business_sha OR existing."ResponseJson"<>response_json THEN
              RAISE EXCEPTION USING ERRCODE='23505',CONSTRAINT='command_receipt_replay_mismatch',
                MESSAGE='Committed command replay evidence does not match the original receipt.';
            END IF;
            RETURN existing."ReceiptId";
          END IF;
          PERFORM set_config('sess.ordinary_command_ledger_write',txid_current()::text,true);
          INSERT INTO advance.command_receipts
            ("ReceiptId","CommandId","BusinessFingerprint","ResponseJson","CommittedAt","CommittedBy")
          VALUES(receipt_id,command_id,business_sha,response_json,statement_timestamp(),session_user);
          RETURN receipt_id;
        END $ordinary_command$;
        REVOKE ALL ON FUNCTION advance.guard_command_request_immutable() FROM PUBLIC;
        REVOKE ALL ON FUNCTION advance.guard_command_receipt_immutable() FROM PUBLIC;
        REVOKE ALL ON FUNCTION advance.ordinary_command_context_valid(text,uuid,text,text,text) FROM PUBLIC;
        REVOKE ALL ON FUNCTION advance.ordinary_claim_command_context(text,uuid,text,uuid,text,bigint,text,text,text,text) FROM PUBLIC;
        REVOKE ALL ON FUNCTION advance.register_command_request(text,text,bytea,bytea,uuid,text,text,text,uuid) FROM PUBLIC;
        REVOKE ALL ON FUNCTION advance.commit_command_receipt(uuid,bytea,jsonb,uuid) FROM PUBLIC;
        REVOKE ALL ON TABLE advance.command_requests,advance.command_receipts FROM PUBLIC;

        DO $ordinary_command_acl$
        BEGIN
          IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_owner') THEN
            IF NOT pg_has_role(current_user,'nexa_erp_owner','MEMBER') THEN
              RAISE EXCEPTION USING ERRCODE='42501',
                MESSAGE='Installing the ordinary command ledger requires nexa_erp_owner membership.';
            END IF;
            ALTER TABLE advance.command_requests OWNER TO nexa_erp_owner;
            ALTER TABLE advance.command_receipts OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.guard_command_request_immutable() OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.guard_command_receipt_immutable() OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.ordinary_command_context_valid(text,uuid,text,text,text) OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.ordinary_claim_command_context(text,uuid,text,uuid,text,bigint,text,text,text,text) OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.register_command_request(text,text,bytea,bytea,uuid,text,text,text,uuid) OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.commit_command_receipt(uuid,bytea,jsonb,uuid) OWNER TO nexa_erp_owner;
            REVOKE ALL ON TABLE advance.command_requests,advance.command_receipts
              FROM nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
            REVOKE ALL ON FUNCTION advance.register_command_request(text,text,bytea,bytea,uuid,text,text,text,uuid)
              FROM nexa_erp_bootstrap,nexa_erp_migration;
            REVOKE ALL ON FUNCTION advance.commit_command_receipt(uuid,bytea,jsonb,uuid)
              FROM nexa_erp_bootstrap,nexa_erp_migration;
            GRANT EXECUTE ON FUNCTION advance.register_command_request(text,text,bytea,bytea,uuid,text,text,text,uuid)
              TO nexa_erp_runtime;
            GRANT EXECUTE ON FUNCTION advance.commit_command_receipt(uuid,bytea,jsonb,uuid)
              TO nexa_erp_runtime;
            GRANT EXECUTE ON FUNCTION advance.ordinary_command_context_valid(text,uuid,text,text,text),
              advance.ordinary_claim_command_context(text,uuid,text,uuid,text,bigint,text,text,text,text)
              TO nexa_erp_runtime;
            IF to_regprocedure('advance.rev869b_commercial_snapshot_reconciles(uuid,jsonb,jsonb)') IS NOT NULL THEN
              GRANT EXECUTE ON FUNCTION advance.rev869b_commercial_snapshot_reconciles(uuid,jsonb,jsonb) TO nexa_erp_runtime;
            END IF;
            IF to_regprocedure('advance.rev869b_qualification_provenance_valid(uuid)') IS NOT NULL THEN
              GRANT EXECUTE ON FUNCTION advance.rev869b_qualification_provenance_valid(uuid) TO nexa_erp_runtime;
            END IF;
          END IF;
        END $ordinary_command_acl$;
        """;

    private static string OrdinaryTaxAuthority => ControlledTaxGstWorkflowSql.Down + "\n" +
        ControlledTaxGstWorkflowSql.Up.Replace("rev869b_", "ordinary_", StringComparison.Ordinal);

    private const string ControlledAuthorityOwnership = """
        REVOKE ALL ON FUNCTION advance.ordinary_guard_qualification_lifecycle() FROM PUBLIC;
        REVOKE ALL ON FUNCTION advance.ordinary_guard_qualification_history_insert() FROM PUBLIC;
        REVOKE ALL ON FUNCTION advance.ordinary_require_qualification_history() FROM PUBLIC;
        DO $ordinary_controlled_owners$
        BEGIN
          IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_owner') THEN
            ALTER FUNCTION advance.tax_gst_guard_controlled_mutation() OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.tax_gst_guard_history_insert() OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.tax_gst_require_history() OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.ordinary_guard_qualification_lifecycle() OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.ordinary_guard_qualification_history_insert() OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.ordinary_require_qualification_history() OWNER TO nexa_erp_owner;
          END IF;
        END $ordinary_controlled_owners$;
        """;

    internal static string Down => """
        DROP TRIGGER IF EXISTS trg_ordinary_bound_qualification_history ON advance.vendor_qualifications;
        DROP TRIGGER IF EXISTS trg_ordinary_qualification_lifecycle ON advance.vendor_qualifications;
        DROP TRIGGER IF EXISTS trg_ordinary_qualification_history_insert_guard ON advance.controlled_configuration_histories;
        DROP FUNCTION IF EXISTS advance.ordinary_require_qualification_history();
        DROP FUNCTION IF EXISTS advance.ordinary_guard_qualification_history_insert();
        DROP FUNCTION IF EXISTS advance.ordinary_guard_qualification_lifecycle();
        DROP TRIGGER IF EXISTS trg_rev869a_vendor_qualification_version_guard ON advance.vendor_qualifications;
        CREATE TRIGGER trg_rev869a_vendor_qualification_version_guard BEFORE UPDATE OR DELETE ON advance.vendor_qualifications FOR EACH ROW EXECUTE FUNCTION advance.rev869a_guard_controlled_version();
        """ + "\n" + ControlledTaxGstWorkflowSql.Down + "\n" + ControlledTaxGstWorkflowSql.Up +
        "\n" + Rev869BControlledMutationSql.RestorePurchaseAuthority + "\n" + """
        DO $ordinary_command_down$
        BEGIN
          IF EXISTS (SELECT 1 FROM advance.command_requests LIMIT 1)
             OR EXISTS (SELECT 1 FROM advance.command_receipts LIMIT 1) THEN
            RAISE EXCEPTION USING ERRCODE='55000',
              MESSAGE='Refusing command-ledger rollback because immutable command evidence exists.';
          END IF;
        END $ordinary_command_down$;
        DROP TRIGGER "TR_command_receipt_immutable" ON advance.command_receipts;
        DROP TRIGGER "TR_command_request_immutable" ON advance.command_requests;
        DROP FUNCTION advance.commit_command_receipt(uuid,bytea,jsonb,uuid);
        DROP FUNCTION advance.register_command_request(text,text,bytea,bytea,uuid,text,text,text,uuid);
        DROP FUNCTION advance.ordinary_claim_command_context(text,uuid,text,uuid,text,bigint,text,text,text,text);
        DROP FUNCTION advance.ordinary_command_context_valid(text,uuid,text,text,text);
        DROP FUNCTION advance.guard_command_receipt_immutable();
        DROP FUNCTION advance.guard_command_request_immutable();
        DROP TABLE advance.command_receipts;
        DROP TABLE advance.command_requests;
        """;
}
