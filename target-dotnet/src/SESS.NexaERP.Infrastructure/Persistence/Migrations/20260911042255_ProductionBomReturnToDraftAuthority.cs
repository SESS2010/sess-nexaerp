using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    public partial class ProductionBomReturnToDraftAuthority : Migration
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
            var deniedActions = enableReturnToDraft ? "('Approve','ReturnToDraft')" : "('Approve')";
            var submittedDestinations = enableReturnToDraft ? "('APPROVED','DRAFT')" : "('APPROVED')";
            var rollbackEvidence = enableReturnToDraft ? string.Empty : """
              IF EXISTS (SELECT 1 FROM advance.production_engineering_history
                         WHERE "Action"='ReturnToDraft' AND "ProductionBomId" IS NOT NULL) THEN
                RAISE EXCEPTION 'Refusing rollback: Production BOM return-to-draft evidence exists.';
              END IF;
            """;
            return $"""
            DO $guard$
            DECLARE matching_grants integer;
            BEGIN
              IF to_regclass('advance.production_engineering_history') IS NULL
                 OR to_regprocedure('advance.guard_production_engineering_history()') IS NULL THEN
                RAISE EXCEPTION 'Production engineering governance is absent or partial.';
              END IF;
              SELECT count(*) INTO matching_grants
              FROM advance.role_page_permissions p
              JOIN advance.page_definitions d ON d."Id"=p."PageDefinitionId"
              JOIN advance.roles r ON r."Id"=p."RoleId"
              WHERE d."PageKey"='production.production-bom' AND r."Code"='TECHNICAL_DIRECTOR'
                AND p."CanApprove" AND p."CanReject"={expectedReject};
              IF matching_grants<>1 THEN
                RAISE EXCEPTION 'Expected exactly one Technical Director Production BOM grant at the predecessor contract, found %.', matching_grants;
              END IF;
              {rollbackEvidence}
            END $guard$;

            UPDATE advance.role_page_permissions p SET "CanReject"={nextReject},
              "UpdatedAt"=now(),"UpdatedBy"='ProductionBomReturnToDraftAuthority',"Version"=p."Version"+1
            FROM advance.page_definitions d, advance.roles r
            WHERE d."Id"=p."PageDefinitionId" AND r."Id"=p."RoleId"
              AND d."PageKey"='production.production-bom' AND r."Code"='TECHNICAL_DIRECTOR';

            CREATE OR REPLACE FUNCTION advance.guard_production_engineering_history()
            RETURNS trigger LANGUAGE plpgsql AS $function$
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
                AND e."ApprovalStatus" IN ('Approved','SeedApproved')
                AND e."EffectiveFrom"<=CURRENT_DATE
                AND (e."EffectiveTo" IS NULL OR e."EffectiveTo">=CURRENT_DATE);
              IF a IS NULL OR a."Code"<>NEW."ActorRoleCode"
                 OR a."AssignmentType"<>NEW."ResolvedRoleAssignmentType" THEN
                RAISE EXCEPTION 'Resolved role assignment evidence is not currently effective in this company.';
              END IF;
              IF NEW."Action" IN {deniedActions} AND a."AssignmentType"='SUPPORT' THEN
                RAISE EXCEPTION 'SUPPORT authority cannot approve or return Production engineering records to Draft.';
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
            END $function$;

            CREATE OR REPLACE FUNCTION advance.guard_production_bom_revision()
            RETURNS trigger LANGUAGE plpgsql AS $function$
            BEGIN
              IF TG_OP='DELETE' OR OLD."Status"='APPROVED' THEN
                RAISE EXCEPTION 'Approved Production BOM revisions are immutable.';
              END IF;
              IF OLD."Status"='SUBMITTED' AND (
                 NEW."Status" NOT IN {submittedDestinations}
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
            END $function$;
            """;
        }
    }
}