using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class ApprovalChainReachabilityCorrectionsSql
{
    internal static void ApplyUp(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql(Up.Replace("__advance_schema__", DatabaseSchemas.Advance, StringComparison.Ordinal));

    internal static void ApplyDownGuard(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql(DownGuard.Replace("__advance_schema__", DatabaseSchemas.Advance, StringComparison.Ordinal));

    internal const string Up = """
        LOCK TABLE __advance_schema__.role_page_permissions,
          __advance_schema__.department_approval_mappings,
          __advance_schema__.purchase_requisitions IN SHARE ROW EXCLUSIVE MODE;

        DO $orphan_guard$
        DECLARE
          orphan_id uuid;
          orphan_count integer;
        BEGIN
          SELECT count(*)
          INTO orphan_count
          FROM __advance_schema__.purchase_requisitions
          WHERE "PrNumber" = 'PR-2026-27-000011';

          IF orphan_count > 1 THEN
            RAISE EXCEPTION 'Expected at most one PR-2026-27-000011; found %.', orphan_count;
          END IF;

          IF orphan_count = 1 THEN
            SELECT "Id" INTO STRICT orphan_id
            FROM __advance_schema__.purchase_requisitions
            WHERE "PrNumber" = 'PR-2026-27-000011';

            IF NOT EXISTS (
              SELECT 1
              FROM __advance_schema__.purchase_requisitions pr
              JOIN __advance_schema__.employees employee ON employee."Id" = pr."CreatorEmployeeId"
              WHERE pr."Id" = orphan_id
                AND pr."Status" = 'Draft'
                AND pr."ApprovalCycle" = 0
                AND pr."CompletedApprovalStepCount" = 0
                AND employee."EmployeeCode" = 'SESS-01'
            ) THEN
              RAISE EXCEPTION 'PR-2026-27-000011 is no longer the reported SESS-01 draft orphan; refusing deletion.';
            END IF;

            IF EXISTS (SELECT 1 FROM __advance_schema__.purchase_requisition_approval_history WHERE "PurchaseRequisitionId" = orphan_id)
              OR EXISTS (SELECT 1 FROM __advance_schema__.purchase_requisition_attachments WHERE "PurchaseRequisitionId" = orphan_id)
              OR EXISTS (SELECT 1 FROM __advance_schema__.stock_availability_checks WHERE "PurchaseRequisitionId" = orphan_id)
              OR EXISTS (SELECT 1 FROM __advance_schema__.stock_reservations WHERE "PurchaseRequisitionId" = orphan_id)
              OR EXISTS (SELECT 1 FROM __advance_schema__.purchase_requirement_handoffs WHERE "PurchaseRequisitionId" = orphan_id)
              OR EXISTS (SELECT 1 FROM __advance_schema__.request_for_quotations WHERE "PurchaseRequisitionId" = orphan_id)
              OR (SELECT count(*) FROM __advance_schema__.purchase_requisition_status_history WHERE "PurchaseRequisitionId" = orphan_id) <> 1
              OR EXISTS (
                SELECT 1 FROM __advance_schema__.purchase_requisition_status_history
                WHERE "PurchaseRequisitionId" = orphan_id AND "NewStatus" <> 'Draft'
              )
            THEN
              RAISE EXCEPTION 'PR-2026-27-000011 has progressed or accumulated evidence; refusing deletion.';
            END IF;

            DELETE FROM __advance_schema__.purchase_requisitions WHERE "Id" = orphan_id;
          END IF;
        END $orphan_guard$;

        DO $grant_acceptance$
        DECLARE
          drift text;
        BEGIN
          WITH mapped_roles AS (
            SELECT DISTINCT upper(trim("ApproverRoleCode")) AS role_code
            FROM __advance_schema__.department_approval_mappings
            WHERE "IsActive"
          ),
          requirements(page_key, require_verify) AS (
            VALUES
              ('purchase.requisitions'::text, true),
              ('purchase.commercial-comparisons'::text, false),
              ('purchase.po'::text, false)
          )
          SELECT string_agg(mapped_roles.role_code || ':' || requirements.page_key, ', ' ORDER BY mapped_roles.role_code, requirements.page_key)
          INTO drift
          FROM mapped_roles
          CROSS JOIN requirements
          LEFT JOIN __advance_schema__.roles role ON upper(role."Code") = mapped_roles.role_code AND role."IsActive"
          LEFT JOIN __advance_schema__.page_definitions page ON page."PageKey" = requirements.page_key AND page."IsActive"
          LEFT JOIN __advance_schema__.role_page_permissions permission
            ON permission."RoleId" = role."Id" AND permission."PageDefinitionId" = page."Id"
          WHERE role."Id" IS NULL
             OR page."Id" IS NULL
             OR permission."Id" IS NULL
             OR NOT (permission."CanView" OR permission."HasFullControl")
             OR NOT (permission."CanApprove" OR permission."HasFullControl")
             OR (requirements.require_verify AND NOT (permission."CanVerify" OR permission."HasFullControl"));

          IF drift IS NOT NULL THEN
            RAISE EXCEPTION 'Mapped approver permission drift remains: %.', drift;
          END IF;
        END $grant_acceptance$;
        """;

    internal const string DownGuard = """
        DO $down_guard$
        BEGIN
          IF EXISTS (
            SELECT 1
            FROM __advance_schema__.role_page_permissions
            WHERE "Id" IN (
              '84000000-0000-0000-0000-000000000007'::uuid,
              '84000000-0000-0000-0000-000000000008'::uuid)
              AND (
                "CreatedBy" <> 'migration-approval-configuration-part2'
                OR "UpdatedBy" IS NOT NULL
                OR "Version" <> 0
                OR NOT "CanView"
                OR NOT "CanVerify"
                OR NOT "CanApprove")
          ) THEN
            RAISE EXCEPTION 'Approval-chain grants changed after migration; refusing destructive Down.';
          END IF;
        END $down_guard$;
        """;
}
