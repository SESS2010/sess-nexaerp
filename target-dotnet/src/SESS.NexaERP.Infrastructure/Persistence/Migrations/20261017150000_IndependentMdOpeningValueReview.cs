using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20261017150000_IndependentMdOpeningValueReview")]
public sealed class IndependentMdOpeningValueReview : Migration
{
    private const string Signature="advance.confirm_opening_stock_value(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text)";
    private const string Original="IF NOT advance.opening_stock_command_valid(p_company,p_actor,p_role,p_assignment,p_type,'OpeningStock.ConfirmValue','ACCOUNTS_MANAGER') THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Opening Stock valuation requires current FULL or TEMPORARY ACCOUNTS_MANAGER authority.'; END IF;";
    private const string Independent="""
        IF p_role='MANAGING_DIRECTOR' THEN
          IF NOT advance.opening_stock_command_valid(p_company,p_actor,p_role,p_assignment,p_type,'OpeningStock.ConfirmValue','MANAGING_DIRECTOR')
            OR NOT EXISTS(SELECT 1 FROM advance.employees e WHERE e."Id"=p_actor AND e."EmployeeCode"='SESS-02')
            OR NOT EXISTS(SELECT 1 FROM advance.employees e WHERE e."Id"=row."CountedByEmployeeId" AND e."EmployeeCode"='SESS-41')
            OR left(coalesce(p_reason,''),length('[Independent MD review: ALFATHIMA unavailable; KARTHICK involved] '))<>'[Independent MD review: ALFATHIMA unavailable; KARTHICK involved] '
            OR btrim(substr(p_reason,length('[Independent MD review: ALFATHIMA unavailable; KARTHICK involved] ')+1))=''
            OR EXISTS(SELECT 1 FROM advance.opening_stock_events h WHERE h."CompanyId"=p_company AND h."OpeningStockId"=p_opening AND h."ActorEmployeeId"=p_actor) THEN
              RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Independent opening value review requires the named MD, a KARTHICK count, an unconflicted reviewer and the audited absence reason.';
          END IF;
        ELSE
          IF NOT advance.opening_stock_command_valid(p_company,p_actor,p_role,p_assignment,p_type,'OpeningStock.ConfirmValue','ACCOUNTS_MANAGER') THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Opening Stock valuation requires current FULL or TEMPORARY ACCOUNTS_MANAGER authority.'; END IF;
        END IF;
        """;
    protected override void Up(MigrationBuilder builder)
    {
        PostgreSqlClusterGuard.Require(builder);
        builder.Sql(MigrationText.Lf("""
            LOCK TABLE advance.role_page_permissions IN SHARE ROW EXCLUSIVE MODE;
            DO $guard$ BEGIN
              IF EXISTS(SELECT 1 FROM advance.role_page_permissions p JOIN advance.roles r ON r."Id"=p."RoleId"
                JOIN advance.page_definitions d ON d."Id"=p."PageDefinitionId"
                WHERE r."Code"='MANAGING_DIRECTOR' AND d."PageKey"='stores.opening-stock') THEN
                RAISE EXCEPTION 'Independent MD review refuses an unexpected existing opening-stock permission; reconcile first.';
              END IF;
            END $guard$;
            INSERT INTO advance.role_page_permissions(
              "Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit","CanIssue","CanVerify","CanApprove","CanReject","CanRequestClarification","CanRequestRevision","CanResubmit","CanCancel","CanDeactivate","CanPrint","CanDownload","CanExport","CanUploadAttachment","CanReplaceAttachment","CanViewCommercialValues","CanViewAuditHistory","HasFullControl","CreatedAt","CreatedBy","Version")
            SELECT md5('rc26.independent-md-opening-review')::uuid,r."Id",d."Id",true,false,false,false,false,true,false,false,false,false,false,false,false,false,true,true,false,false,true,true,false,clock_timestamp(),'IndependentMdOpeningValueReview',0
              FROM advance.roles r CROSS JOIN advance.page_definitions d
              WHERE r."Code"='MANAGING_DIRECTOR' AND r."IsActive" AND d."PageKey"='stores.opening-stock' AND d."IsActive";
            DO $guard$ BEGIN
              IF (SELECT count(*) FROM advance.role_page_permissions WHERE "Id"=md5('rc26.independent-md-opening-review')::uuid)<>1 THEN
                RAISE EXCEPTION 'Independent MD review requires the existing active role and opening-stock page.';
              END IF;
            END $guard$;
            """));
        // Reuse the installed function, owner, signature, security mode, search_path and ACL unchanged.
        builder.Sql(InstalledFunctionSql.Rewrite(Signature,true,(Original,Independent,1)));
    }
    protected override void Down(MigrationBuilder builder)
    {
        PostgreSqlClusterGuard.Require(builder);
        builder.Sql(MigrationText.Lf("""
            LOCK TABLE advance.opening_stocks,advance.opening_stock_events,advance.role_page_permissions IN SHARE ROW EXCLUSIVE MODE;
            DO $guard$ BEGIN
              IF EXISTS(SELECT 1 FROM advance.opening_stocks WHERE "ValueActorRoleCode"='MANAGING_DIRECTOR')
                OR EXISTS(SELECT 1 FROM advance.opening_stock_events WHERE "ActorRoleCode"='MANAGING_DIRECTOR' AND "Action"='VALUE') THEN
                RAISE EXCEPTION 'Independent MD value-review rollback refuses retained valuation evidence.';
              END IF;
              IF NOT EXISTS(SELECT 1 FROM advance.role_page_permissions WHERE "Id"=md5('rc26.independent-md-opening-review')::uuid
                AND "RoleId"=(SELECT "Id" FROM advance.roles WHERE "Code"='MANAGING_DIRECTOR')
                AND "PageDefinitionId"=(SELECT "Id" FROM advance.page_definitions WHERE "PageKey"='stores.opening-stock')
                AND "CreatedBy"='IndependentMdOpeningValueReview' AND "Version"=0 AND "CanView" AND "CanVerify"
                AND NOT "CanCreate" AND NOT "CanUpdate" AND NOT "CanSubmit" AND NOT "CanIssue" AND NOT "CanApprove"
                AND NOT "CanReject" AND NOT "CanRequestClarification" AND NOT "CanRequestRevision" AND NOT "CanResubmit"
                AND NOT "CanCancel" AND NOT "CanDeactivate" AND NOT "CanPrint" AND "CanDownload" AND "CanExport"
                AND NOT "CanUploadAttachment" AND NOT "CanReplaceAttachment" AND "CanViewCommercialValues" AND "CanViewAuditHistory" AND NOT "HasFullControl") THEN
                RAISE EXCEPTION 'Independent MD review rollback refuses permission drift.';
              END IF;
            END $guard$;
            """));
        builder.Sql(InstalledFunctionSql.Rewrite(Signature,true,(Independent,Original,1)));
        builder.Sql("DELETE FROM advance.role_page_permissions WHERE \"Id\"=md5('rc26.independent-md-opening-review')::uuid;");
    }
}