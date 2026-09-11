using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    public partial class EngineeringDocumentReturnToDraftAuthority : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(GuardedSql(enableReturnToDraft: true));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(GuardedSql(enableReturnToDraft: false));
        }

        private static string GuardedSql(bool enableReturnToDraft)
        {
            var expectedReject = enableReturnToDraft ? "FALSE" : "TRUE";
            var nextReject = enableReturnToDraft ? "TRUE" : "FALSE";
            var submittedDestinations = enableReturnToDraft ? "('APPROVED','DRAFT')" : "('APPROVED')";
            var rollbackEvidence = enableReturnToDraft ? string.Empty : """
              IF EXISTS (SELECT 1 FROM advance.production_engineering_history
                         WHERE "Action"='ReturnToDraft' AND "EngineeringDocumentId" IS NOT NULL) THEN
                RAISE EXCEPTION 'Refusing rollback: Engineering Document return-to-draft evidence exists.';
              END IF;
            """;
            return $"""
            DO $guard$
            DECLARE matching_grants integer; history_definition text;
            BEGIN
              IF to_regclass('advance.production_engineering_history') IS NULL
                 OR to_regprocedure('advance.guard_production_engineering_history()') IS NULL
                 OR to_regprocedure('advance.guard_engineering_document_revision()') IS NULL THEN
                RAISE EXCEPTION 'Production engineering governance is absent or partial.';
              END IF;
              SELECT pg_get_functiondef('advance.guard_production_engineering_history()'::regprocedure)
                INTO history_definition;
              IF position('ReturnToDraft' in history_definition)=0 THEN
                RAISE EXCEPTION 'Production engineering SUPPORT boundary is not at the expected predecessor contract.';
              END IF;
              SELECT count(*) INTO matching_grants
              FROM advance.role_page_permissions p
              JOIN advance.page_definitions d ON d."Id"=p."PageDefinitionId"
              JOIN advance.roles r ON r."Id"=p."RoleId"
              WHERE d."PageKey"='design.engineering-documents' AND r."Code"='TECHNICAL_DIRECTOR'
                AND p."CanApprove" AND p."CanReject"={expectedReject};
              IF matching_grants<>1 THEN
                RAISE EXCEPTION 'Expected exactly one Technical Director Engineering Document grant at the predecessor contract, found %.', matching_grants;
              END IF;
              {rollbackEvidence}
            END $guard$;

            UPDATE advance.role_page_permissions p SET "CanReject"={nextReject},
              "UpdatedAt"=now(),"UpdatedBy"='EngineeringDocumentReturnToDraftAuthority',"Version"=p."Version"+1
            FROM advance.page_definitions d, advance.roles r
            WHERE d."Id"=p."PageDefinitionId" AND r."Id"=p."RoleId"
              AND d."PageKey"='design.engineering-documents' AND r."Code"='TECHNICAL_DIRECTOR';

            CREATE OR REPLACE FUNCTION advance.guard_engineering_document_revision()
            RETURNS trigger LANGUAGE plpgsql AS $function$
            BEGIN
              IF TG_OP='DELETE' OR OLD."Status" IN ('APPROVED','SUPERSEDED') THEN
                RAISE EXCEPTION 'Approved and superseded drawing revisions are immutable.';
              END IF;
              IF OLD."Status"='SUBMITTED' AND (
                 NEW."Status" NOT IN {submittedDestinations}
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
            END $function$;
            """;
        }
    }
}