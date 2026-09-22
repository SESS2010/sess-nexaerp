using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>
/// Finding #21. Final vendor approval requires Accounts commercial verification, and that
/// operation accepted only the legacy role ACCOUNTS_HEAD, which role governance has retired
/// (not employee-assignable) and replaced with ACCOUNTS_MANAGER. No vendor could therefore be
/// approved in a fresh company. The verification now belongs to the vendor page, and the
/// governed successor receives view, verify, commercial-value and audit-history rights there.
/// Vendor approval itself stays with the Technical Director and Managing Director.
/// </summary>
[DbContext(typeof(NexaErpDbContext))]
[Migration("20260920110000_AccountsVendorCommercialVerificationGrant")]
public sealed class AccountsVendorCommercialVerificationGrant : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(MigrationText.Lf("""
            SET LOCAL TIME ZONE 'UTC';
            LOCK TABLE advance.role_page_permissions IN SHARE ROW EXCLUSIVE MODE;
            DO $grant$
            DECLARE role_id uuid; page_id uuid; grant_id uuid; prior jsonb; after_row jsonb;
            BEGIN
              IF NOT EXISTS(SELECT 1 FROM advance.roles legacy JOIN advance.roles successor ON successor."Id"=legacy."ReplacementRoleId"
                            WHERE legacy."Code"='ACCOUNTS_HEAD' AND successor."Code"='ACCOUNTS_MANAGER') THEN
                RAISE EXCEPTION 'Role governance no longer names ACCOUNTS_MANAGER as the ACCOUNTS_HEAD replacement; decide the Accounts verifier before granting.';
              END IF;
              SELECT "Id" INTO STRICT role_id FROM advance.roles WHERE "Code"='ACCOUNTS_MANAGER' AND "IsActive";
              SELECT "Id" INTO STRICT page_id FROM advance.page_definitions WHERE "PageKey"='masters.vendors' AND "IsActive";
              SELECT to_jsonb(p),p."Id" INTO prior,grant_id FROM advance.role_page_permissions p
                WHERE p."RoleId"=role_id AND p."PageDefinitionId"=page_id;
              IF prior IS NULL THEN
                grant_id := md5('accounts-vendor-commercial-verification:ACCOUNTS_MANAGER:masters.vendors')::uuid;
                INSERT INTO advance.role_page_permissions
                  ("Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit","CanIssue",
                   "CanVerify","CanApprove","CanReject","CanRequestClarification","CanRequestRevision","CanResubmit",
                   "CanCancel","CanDeactivate","CanPrint","CanDownload","CanExport","CanUploadAttachment",
                   "CanReplaceAttachment","CanViewCommercialValues","CanViewAuditHistory","HasFullControl",
                   "CreatedAt","CreatedBy","Version")
                VALUES(grant_id,role_id,page_id,
                  true,false,false,false,false,
                  true,false,false,true,false,false,
                  false,false,true,true,false,false,
                  false,true,true,false,clock_timestamp(),'AccountsVendorCommercialVerificationGrant',0);
              ELSE
                UPDATE advance.role_page_permissions SET
                  "CanView"=true,"CanVerify"=true,"CanRequestClarification"=true,"CanPrint"=true,"CanDownload"=true,
                  "CanViewCommercialValues"=true,"CanViewAuditHistory"=true,
                  "Version"="Version"+1,"UpdatedAt"=clock_timestamp(),"UpdatedBy"='AccountsVendorCommercialVerificationGrant'
                WHERE "Id"=grant_id;
              END IF;
              SELECT to_jsonb(p) INTO STRICT after_row FROM advance.role_page_permissions p WHERE p."Id"=grant_id;
              INSERT INTO advance.audit_logs
                ("Id","Scope","Module","Action","EntityName","EntityId","UserLoginId","ActorRoleCode","Result",
                 "CorrelationId","BeforeJson","AfterJson","CreatedAt","CreatedBy","Version")
              VALUES(gen_random_uuid(),'GLOBAL','Security','GrantAccountsVendorVerification','RolePagePermission',grant_id::text,
                session_user,'','Success','AccountsVendorCommercialVerificationGrant:ACCOUNTS_MANAGER:masters.vendors',
                prior::text,after_row::text,clock_timestamp(),'AccountsVendorCommercialVerificationGrant',0);
            END $grant$;
            """));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(MigrationText.Lf("""
            SET LOCAL TIME ZONE 'UTC';
            LOCK TABLE advance.role_page_permissions IN SHARE ROW EXCLUSIVE MODE;
            DO $revert$
            DECLARE receipt record; current_row jsonb; prior jsonb; after_row jsonb; grant_id uuid;
            BEGIN
              IF (SELECT count(*) FROM advance.audit_logs a
                WHERE a."CreatedBy"='AccountsVendorCommercialVerificationGrant' AND a."Action"='GrantAccountsVendorVerification'
                  AND NOT EXISTS(SELECT 1 FROM advance.audit_logs r WHERE r."CreatedBy"='AccountsVendorCommercialVerificationGrant'
                    AND r."Action"='RevertAccountsVendorVerification' AND r."CorrelationId"=a."Id"::text)) <> 1 THEN
                RAISE EXCEPTION 'Accounts vendor verification grant rollback requires one intact unreverted receipt';
              END IF;
              SELECT a.* INTO STRICT receipt FROM advance.audit_logs a
                WHERE a."CreatedBy"='AccountsVendorCommercialVerificationGrant' AND a."Action"='GrantAccountsVendorVerification'
                  AND NOT EXISTS(SELECT 1 FROM advance.audit_logs r WHERE r."CreatedBy"='AccountsVendorCommercialVerificationGrant'
                    AND r."Action"='RevertAccountsVendorVerification' AND r."CorrelationId"=a."Id"::text);
              grant_id := receipt."EntityId"::uuid;
              SELECT to_jsonb(p) INTO current_row FROM advance.role_page_permissions p WHERE p."Id"=grant_id;
              IF current_row IS DISTINCT FROM receipt."AfterJson"::jsonb THEN
                RAISE EXCEPTION 'Accounts vendor verification grant rollback refuses a changed or missing permission: %',grant_id;
              END IF;
              prior := receipt."BeforeJson"::jsonb;
              IF prior IS NULL THEN
                DELETE FROM advance.role_page_permissions WHERE "Id"=grant_id;
                after_row := NULL;
              ELSE
                UPDATE advance.role_page_permissions SET
                  "CanView"=(prior->>'CanView')::boolean,"CanVerify"=(prior->>'CanVerify')::boolean,
                  "CanRequestClarification"=(prior->>'CanRequestClarification')::boolean,"CanPrint"=(prior->>'CanPrint')::boolean,
                  "CanDownload"=(prior->>'CanDownload')::boolean,"CanViewCommercialValues"=(prior->>'CanViewCommercialValues')::boolean,
                  "CanViewAuditHistory"=(prior->>'CanViewAuditHistory')::boolean,
                  "Version"=(prior->>'Version')::bigint,"UpdatedAt"=(prior->>'UpdatedAt')::timestamptz,
                  "UpdatedBy"=prior->>'UpdatedBy' WHERE "Id"=grant_id;
                SELECT to_jsonb(p) INTO after_row FROM advance.role_page_permissions p WHERE p."Id"=grant_id;
                IF after_row IS DISTINCT FROM prior THEN
                  RAISE EXCEPTION 'Accounts vendor verification grant rollback failed to restore the exact prior row: %',grant_id;
                END IF;
              END IF;
              INSERT INTO advance.audit_logs
                ("Id","Scope","Module","Action","EntityName","EntityId","UserLoginId","ActorRoleCode","Result",
                 "CorrelationId","BeforeJson","AfterJson","CreatedAt","CreatedBy","Version")
              VALUES(gen_random_uuid(),'GLOBAL','Security','RevertAccountsVendorVerification','RolePagePermission',grant_id::text,
                session_user,'','Success',receipt."Id"::text,current_row::text,after_row::text,
                clock_timestamp(),'AccountsVendorCommercialVerificationGrant',0);
            END $revert$;
            """));
    }
}
