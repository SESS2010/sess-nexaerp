namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class ProductionEngineeringSql
{
    internal const string ClusterGuard = """
        DO $$ BEGIN
          IF current_setting('server_version_num')::integer < 170000
             OR current_database() IN ('postgres','template0','template1')
          THEN RAISE EXCEPTION 'Production engineering migration cluster guard refused this database.';
          END IF;
        END $$;
        """;

    internal const string Governance = """
        CREATE FUNCTION advance.guard_production_engineering_history()
        RETURNS trigger LANGUAGE plpgsql AS $$
        DECLARE a record; DECLARE organization text;
        BEGIN
          IF TG_OP IN ('UPDATE','DELETE') THEN
            RAISE EXCEPTION 'Production engineering history is immutable.';
          END IF;
          SELECT r."Code",e."AssignmentType" INTO a
          FROM advance.employee_role_assignments e
          JOIN advance.roles r ON r."Id"=e."RoleId"
          WHERE e."Id"=NEW."ResolvedRoleAssignmentId"
            AND e."EmployeeId"=NEW."ActorEmployeeId"
            AND e."CompanyId"=NEW."CompanyId"
            AND e."ApprovalStatus"='Approved'
            AND e."EffectiveFrom"<=CURRENT_DATE
            AND (e."EffectiveTo" IS NULL OR e."EffectiveTo">=CURRENT_DATE);
          IF a IS NULL OR a."Code"<>NEW."ActorRoleCode"
             OR a."AssignmentType"<>NEW."ResolvedRoleAssignmentType" THEN
            RAISE EXCEPTION 'Resolved role assignment evidence is not currently effective in this company.';
          END IF;
          IF NEW."Action"='Approve' AND a."AssignmentType"='SUPPORT' THEN
            RAISE EXCEPTION 'SUPPORT authority cannot approve Production engineering records.';
          END IF;
          SELECT "Code" INTO STRICT organization FROM advance.companies WHERE "Id"=NEW."CompanyId";
          IF NOT advance.ordinary_command_context_valid(
              organization,NEW."ActorEmployeeId",
              current_setting('advance.ordinary_identity_issuer',true),
              current_setting('advance.ordinary_identity_subject',true),
              NEW."ActorRoleCode") THEN
            RAISE EXCEPTION 'Production engineering evidence requires the current ordinary command transaction.';
          END IF;
          RETURN NEW;
        END $$;
        CREATE TRIGGER trg_production_engineering_history_guard
          BEFORE INSERT OR UPDATE OR DELETE ON advance.production_engineering_history
          FOR EACH ROW EXECUTE FUNCTION advance.guard_production_engineering_history();

        CREATE FUNCTION advance.guard_production_bom_pin()
        RETURNS trigger LANGUAGE plpgsql AS $$
        BEGIN
          IF NEW."PinnedProductionBomRevisionId" IS NOT NULL
             AND NEW."PinnedProductionBomRevisionId" IS DISTINCT FROM OLD."PinnedProductionBomRevisionId"
             AND (NOT EXISTS (
               SELECT 1 FROM advance.production_bom_revisions r
               JOIN advance.production_boms b ON b."Id"=r."ProductionBomId"
               WHERE r."Id"=NEW."PinnedProductionBomRevisionId"
                 AND r."CompanyId"=NEW."CompanyId" AND r."Status"='APPROVED'
                 AND b."JobOrderId"=NEW."Id" AND b."CompanyId"=NEW."CompanyId")
               OR NOT EXISTS (
                 SELECT 1 FROM advance.production_engineering_history h
                 WHERE h."CompanyId"=NEW."CompanyId"
                   AND h."ProductionBomRevisionId"=NEW."PinnedProductionBomRevisionId"
                   AND h."Action"='PinToMachine'
                   AND h.xmin::text::bigint=txid_current()))
          THEN RAISE EXCEPTION 'A machine may pin only its own approved Production BOM revision.';
          END IF;
          RETURN NEW;
        END $$;
        CREATE CONSTRAINT TRIGGER trg_job_order_production_bom_pin
          AFTER UPDATE OF "PinnedProductionBomRevisionId" ON advance.job_orders
          DEFERRABLE INITIALLY DEFERRED
          FOR EACH ROW EXECUTE FUNCTION advance.guard_production_bom_pin();
        """;

    internal const string Constraints = """
        ALTER TABLE advance.production_bom_revisions
          ADD CONSTRAINT ck_production_bom_revision_status
          CHECK ("Status" IN ('DRAFT','SUBMITTED','APPROVED'));
        ALTER TABLE advance.production_bom_lines
          ADD CONSTRAINT ck_production_bom_line_quantity CHECK ("Quantity">0);
        ALTER TABLE advance.engineering_documents
          ADD CONSTRAINT ck_engineering_document_type CHECK ("DocumentType" IN ('GA','PART'));
        ALTER TABLE advance.engineering_document_revisions
          ADD CONSTRAINT ck_engineering_document_revision_status
          CHECK ("Status" IN ('DRAFT','SUBMITTED','APPROVED','SUPERSEDED')),
          ADD CONSTRAINT ck_engineering_document_revision_size
          CHECK ("SizeBytes">0 AND "SizeBytes"<=26214400),
          ADD CONSTRAINT ck_engineering_document_revision_sha
          CHECK ("Sha256"~'^[0-9A-F]{64}$'),
          ADD CONSTRAINT ck_engineering_document_people
          CHECK ("DrawnByEmployeeId"<>"CheckedByEmployeeId");
        ALTER TABLE advance.production_engineering_history
          ADD CONSTRAINT ck_production_engineering_history_target
          CHECK (
            ("ProductionBomId" IS NOT NULL AND "ProductionBomRevisionId" IS NOT NULL
             AND "EngineeringDocumentId" IS NULL AND "EngineeringDocumentRevisionId" IS NULL)
            OR
            ("ProductionBomId" IS NULL AND "ProductionBomRevisionId" IS NULL
             AND "EngineeringDocumentId" IS NOT NULL AND "EngineeringDocumentRevisionId" IS NOT NULL)
          );
        """;

    internal const string Immutability = """
        CREATE FUNCTION advance.guard_production_bom_revision()
        RETURNS trigger LANGUAGE plpgsql AS $$
        BEGIN
          IF TG_OP='DELETE' OR OLD."Status"='APPROVED' THEN
            RAISE EXCEPTION 'Approved Production BOM revisions are immutable.';
          END IF;
          IF OLD."Status"='SUBMITTED' AND (
             NEW."Status"<>'APPROVED'
             OR NEW."ProductionBomId"<>OLD."ProductionBomId"
             OR NEW."RevisionNumber"<>OLD."RevisionNumber"
             OR NEW."SourceEstimatedBomRevisionId"<>OLD."SourceEstimatedBomRevisionId"
             OR NEW."SupersedesRevisionId" IS DISTINCT FROM OLD."SupersedesRevisionId"
             OR NEW."RevisionReason"<>OLD."RevisionReason"
             OR NEW."PreparedByEmployeeId"<>OLD."PreparedByEmployeeId"
             OR NEW."ContentFingerprint"<>OLD."ContentFingerprint")
          THEN RAISE EXCEPTION 'Submitted Production BOM content is immutable.';
          END IF;
          RETURN NEW;
        END $$;
        CREATE TRIGGER trg_production_bom_revision_guard
          BEFORE UPDATE OR DELETE ON advance.production_bom_revisions
          FOR EACH ROW EXECUTE FUNCTION advance.guard_production_bom_revision();

        CREATE FUNCTION advance.guard_production_bom_line()
        RETURNS trigger LANGUAGE plpgsql AS $$
        DECLARE revision_id uuid;
        BEGIN
          revision_id := CASE WHEN TG_OP='DELETE'
            THEN OLD."ProductionBomRevisionId" ELSE NEW."ProductionBomRevisionId" END;
          IF NOT EXISTS (SELECT 1 FROM advance.production_bom_revisions
             WHERE "Id"=revision_id AND "Status"='DRAFT') THEN
            RAISE EXCEPTION 'Production BOM lines change only while their revision is Draft.';
          END IF;
          IF TG_OP='DELETE' THEN RETURN OLD; END IF;
          RETURN NEW;
        END $$;
        CREATE TRIGGER trg_production_bom_line_guard
          BEFORE INSERT OR UPDATE OR DELETE ON advance.production_bom_lines
          FOR EACH ROW EXECUTE FUNCTION advance.guard_production_bom_line();

        CREATE FUNCTION advance.guard_engineering_document_revision()
        RETURNS trigger LANGUAGE plpgsql AS $$
        BEGIN
          IF TG_OP='DELETE' OR OLD."Status" IN ('APPROVED','SUPERSEDED') THEN
            RAISE EXCEPTION 'Approved and superseded drawing revisions are immutable.';
          END IF;
          IF OLD."Status"='SUBMITTED' AND (
             NEW."Status"<>'APPROVED'
             OR NEW."EngineeringDocumentId"<>OLD."EngineeringDocumentId"
             OR NEW."RevisionNumber"<>OLD."RevisionNumber"
             OR NEW."RevisionCode"<>OLD."RevisionCode"
             OR NEW."SupersedesRevisionId" IS DISTINCT FROM OLD."SupersedesRevisionId"
             OR NEW."DrawnByEmployeeId"<>OLD."DrawnByEmployeeId"
             OR NEW."CheckedByEmployeeId"<>OLD."CheckedByEmployeeId"
             OR NEW."RevisionNote"<>OLD."RevisionNote"
             OR NEW."StorageKey"<>OLD."StorageKey"
             OR NEW."Sha256"<>OLD."Sha256")
          THEN RAISE EXCEPTION 'Submitted drawing content is immutable.';
          END IF;
          RETURN NEW;
        END $$;
        CREATE TRIGGER trg_engineering_document_revision_guard
          BEFORE UPDATE OR DELETE ON advance.engineering_document_revisions
          FOR EACH ROW EXECUTE FUNCTION advance.guard_engineering_document_revision();
        """;

    internal const string Grants = """
        DO $$
        DECLARE production_page uuid := '52000000-0000-0000-0000-000000000002';
        DECLARE document_page uuid := '52000000-0000-0000-0000-000000000003';
        DECLARE affected integer;
        BEGIN
          IF EXISTS (SELECT 1 FROM advance.page_definitions
             WHERE "PageKey" IN ('production.production-bom','design.engineering-documents')) THEN
            RAISE EXCEPTION 'Production engineering page grants are partially or already installed.';
          END IF;
          INSERT INTO advance.page_definitions
            ("Id","PageKey","Module","Title","Route","IsActive","CreatedAt","CreatedBy","Version")
          VALUES
            (production_page,'production.production-bom','Production','Production BOM',
             '/production/boms',TRUE,now(),'ProductionBomAndEngineeringDocuments',0),
            (document_page,'design.engineering-documents','Design','Engineering Documents',
             '/design/documents',TRUE,now(),'ProductionBomAndEngineeringDocuments',0);

          INSERT INTO advance.role_page_permissions
            ("Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit",
             "CanIssue","CanVerify","CanApprove","CanReject","CanRequestClarification",
             "CanRequestRevision","CanResubmit","CanCancel","CanDeactivate","CanPrint",
             "CanDownload","CanExport","CanUploadAttachment","CanReplaceAttachment",
             "CanViewCommercialValues","CanViewAuditHistory","HasFullControl",
             "CreatedAt","CreatedBy","Version")
          SELECT md5('production-bom-'||r."Id"::text)::uuid,r."Id",production_page,
             TRUE,TRUE,TRUE,TRUE,FALSE,FALSE,r."Code"='TECHNICAL_DIRECTOR',
             FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,TRUE,FALSE,FALSE,FALSE,
             FALSE,TRUE,FALSE,now(),'ProductionBomAndEngineeringDocuments',0
          FROM advance.roles r
          WHERE r."Code" IN ('PRODUCTION_MANAGER','DESIGN_ENGINEER','TECHNICAL_DIRECTOR')
            AND r."IsActive";
          GET DIAGNOSTICS affected = ROW_COUNT;
          IF affected<>3 THEN RAISE EXCEPTION 'Expected three Production BOM role grants, found %.',affected; END IF;

          INSERT INTO advance.role_page_permissions
            ("Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit",
             "CanIssue","CanVerify","CanApprove","CanReject","CanRequestClarification",
             "CanRequestRevision","CanResubmit","CanCancel","CanDeactivate","CanPrint",
             "CanDownload","CanExport","CanUploadAttachment","CanReplaceAttachment",
             "CanViewCommercialValues","CanViewAuditHistory","HasFullControl",
             "CreatedAt","CreatedBy","Version")
          SELECT md5('engineering-document-'||r."Id"::text)::uuid,r."Id",document_page,
             TRUE,TRUE,FALSE,TRUE,FALSE,FALSE,r."Code"='TECHNICAL_DIRECTOR',
             FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,TRUE,FALSE,TRUE,FALSE,
             FALSE,TRUE,FALSE,now(),'ProductionBomAndEngineeringDocuments',0
          FROM advance.roles r
          WHERE r."Code" IN ('DESIGN_ENGINEER','TECHNICAL_DIRECTOR') AND r."IsActive";
          GET DIAGNOSTICS affected = ROW_COUNT;
          IF affected<>2 THEN RAISE EXCEPTION 'Expected two engineering-document role grants, found %.',affected; END IF;

          INSERT INTO advance.employee_page_permissions
            ("Id","CompanyId","EmployeeId","PageDefinitionId","CanView","CanCreate",
             "CanUpdate","CanSubmit","CanDownload","CanViewAuditHistory",
             "CreatedAt","CreatedBy","Version")
          SELECT md5('engineering-document-employee-'||c."Id"::text||e."Id"::text)::uuid,
             c."Id",e."Id",document_page,TRUE,TRUE,FALSE,TRUE,TRUE,TRUE,
             now(),'ProductionBomAndEngineeringDocuments',0
          FROM advance.companies c CROSS JOIN advance.employees e
          WHERE c."IsActive" AND c."Status"='ACTIVE' AND e."Status"='Active'
            AND e."EmployeeCode" IN ('SESS-04','SESS-05');
          GET DIAGNOSTICS affected = ROW_COUNT;
          IF affected<>4 THEN
            RAISE EXCEPTION 'Expected four named employee/company engineering-document grants, found %.',affected;
          END IF;
        END $$;
        """;

    internal const string Down = """
        DROP TRIGGER IF EXISTS trg_engineering_document_revision_guard
          ON advance.engineering_document_revisions;
        DROP FUNCTION IF EXISTS advance.guard_engineering_document_revision();
        DROP TRIGGER IF EXISTS trg_production_bom_line_guard ON advance.production_bom_lines;
        DROP FUNCTION IF EXISTS advance.guard_production_bom_line();
        DROP TRIGGER IF EXISTS trg_production_bom_revision_guard
          ON advance.production_bom_revisions;
        DROP FUNCTION IF EXISTS advance.guard_production_bom_revision();
        DROP TRIGGER IF EXISTS trg_job_order_production_bom_pin ON advance.job_orders;
        DROP FUNCTION IF EXISTS advance.guard_production_bom_pin();
        DROP TRIGGER IF EXISTS trg_production_engineering_history_guard
          ON advance.production_engineering_history;
        DROP FUNCTION IF EXISTS advance.guard_production_engineering_history();
        DELETE FROM advance.employee_page_permissions
          WHERE "CreatedBy"='ProductionBomAndEngineeringDocuments';
        DELETE FROM advance.role_page_permissions
          WHERE "CreatedBy"='ProductionBomAndEngineeringDocuments';
        DELETE FROM advance.page_definitions
          WHERE "CreatedBy"='ProductionBomAndEngineeringDocuments'
            AND "PageKey" IN ('production.production-bom','design.engineering-documents');
        """;
}
