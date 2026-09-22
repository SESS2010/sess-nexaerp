using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EstimatedBomReturnToDraftAuthority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(GuardedSql(enableReturnToDraft: true));
        }

        /// <inheritdoc />
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
            var rollbackEvidence = enableReturnToDraft ? string.Empty : """
              IF EXISTS (SELECT 1 FROM advance.estimated_bom_history WHERE "Action"='ReturnToDraft') THEN
                RAISE EXCEPTION 'Refusing rollback: Estimated BOM return-to-draft evidence exists.';
              END IF;
            """;
            return $"""
            DO $guard$
            DECLARE matching_grants integer;
            BEGIN
              IF to_regclass('advance.estimated_bom_history') IS NULL
                 OR to_regprocedure('advance.guard_estimated_bom_governance()') IS NULL THEN
                RAISE EXCEPTION 'Estimated BOM governance is absent or partial.';
              END IF;
              SELECT count(*) INTO matching_grants
              FROM advance.role_page_permissions p
              JOIN advance.page_definitions d ON d."Id"=p."PageDefinitionId"
              JOIN advance.roles r ON r."Id"=p."RoleId"
              WHERE d."PageKey"='design.estimated-bom' AND r."Code"='TECHNICAL_DIRECTOR'
                AND p."CanApprove" AND p."CanReject"={expectedReject};
              IF matching_grants<>1 THEN
                RAISE EXCEPTION 'Expected exactly one Technical Director Estimated BOM grant at the predecessor contract, found %.', matching_grants;
              END IF;
              {rollbackEvidence}
            END $guard$;

            UPDATE advance.role_page_permissions p SET "CanReject"={nextReject},
              "UpdatedAt"=now(),"UpdatedBy"='EstimatedBomReturnToDraftAuthority',"Version"=p."Version"+1
            FROM advance.page_definitions d, advance.roles r
            WHERE d."Id"=p."PageDefinitionId" AND r."Id"=p."RoleId"
              AND d."PageKey"='design.estimated-bom' AND r."Code"='TECHNICAL_DIRECTOR';

            CREATE OR REPLACE FUNCTION advance.guard_estimated_bom_governance()
            RETURNS trigger LANGUAGE plpgsql AS $function$
            DECLARE a record; organization text;
            BEGIN
              IF TG_OP IN ('UPDATE','DELETE') THEN RAISE EXCEPTION 'Estimated BOM history and item merge evidence are immutable.'; END IF;
              SELECT r."Code",e."AssignmentType" INTO a FROM advance.employee_role_assignments e
              JOIN advance.roles r ON r."Id"=e."RoleId" WHERE e."Id"=NEW."ResolvedRoleAssignmentId"
                AND e."EmployeeId"=NEW."ActorEmployeeId" AND e."CompanyId"=COALESCE(NEW."CompanyId",e."CompanyId")
                AND e."ApprovalStatus" IN ('Approved','SeedApproved') AND e."EffectiveFrom"<=CURRENT_DATE
                AND (e."EffectiveTo" IS NULL OR e."EffectiveTo">=CURRENT_DATE);
              IF a IS NULL OR a."Code"<>NEW."ActorRoleCode" OR a."AssignmentType"<>NEW."ResolvedRoleAssignmentType" THEN RAISE EXCEPTION 'Resolved role assignment evidence does not match a currently effective assignment.'; END IF;
              SELECT "Code" INTO STRICT organization FROM advance.companies WHERE "Id"=NEW."CompanyId";
              IF NOT advance.ordinary_command_context_valid(organization,NEW."ActorEmployeeId",current_setting('advance.ordinary_identity_issuer',true),current_setting('advance.ordinary_identity_subject',true),NEW."ActorRoleCode") THEN RAISE EXCEPTION 'Estimated BOM and item merge evidence requires the current ordinary command transaction.'; END IF;
              IF TG_TABLE_NAME='estimated_bom_history' AND NEW."Action" IN {deniedActions} AND a."AssignmentType"='SUPPORT' THEN RAISE EXCEPTION 'SUPPORT authority cannot approve or return an Estimated BOM to Draft.'; END IF;
              IF TG_TABLE_NAME='item_merge_aliases' AND (a."AssignmentType"='SUPPORT' OR a."Code" NOT IN ('STORES_MANAGER','PURCHASE_MANAGER')) THEN RAISE EXCEPTION 'Item merge requires FULL or TEMPORARY STORES_MANAGER or PURCHASE_MANAGER authority.'; END IF;
              RETURN NEW;
            END $function$;
            """;
        }
    }
}
