using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20261017160000_NamedAccountsBackupIdentity")]
public sealed class NamedAccountsBackupIdentity : Migration
{
    private const string RegisterSignature = "advance.register_command_request(text,text,bytea,bytea,uuid,text,text,text,uuid)";
    private const string RegisterBody = "SELECT * INTO existing FROM advance.command_requests r";
    private const string GuardedRegisterBody = "IF advance.accounts_identity_role_allowed(organization,actor_employee,identity_issuer,identity_subject,actor_role,operation,actor_assignment) IS NOT TRUE THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Identity authority profile does not permit this command role or operation.'; END IF; " + RegisterBody;

    protected override void Up(MigrationBuilder builder)
    {
        PostgreSqlClusterGuard.Require(builder);
        builder.Sql(MigrationText.Lf("""
            LOCK TABLE advance.employee_identity_mappings,advance.employee_role_assignments,advance.command_requests IN SHARE ROW EXCLUSIVE MODE;
            ALTER TABLE advance.employee_identity_mappings DROP CONSTRAINT "CK_employee_identity_mapping_type";
            ALTER TABLE advance.employee_identity_mappings ADD CONSTRAINT "CK_employee_identity_mapping_type"
              CHECK("IdentityType" IN('HUMAN','SERVICE','HUMAN_ACCT_BACKUP'));
            -- The original standard HUMAN and issuer/subject unique indexes are not changed.
            CREATE UNIQUE INDEX "IX_employee_identity_accounts_backup" ON advance.employee_identity_mappings
              ("CompanyId","OrganizationId","EmployeeId","IdentityType","IsActive")
              WHERE "IsActive" AND "IdentityType"='HUMAN_ACCT_BACKUP';
            CREATE FUNCTION advance.guard_accounts_backup_identity() RETURNS trigger
            LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $guard$
            DECLARE approved_date date;
            BEGIN
              IF TG_OP='UPDATE' AND (OLD."IdentityType"='HUMAN_ACCT_BACKUP' OR NEW."IdentityType"='HUMAN_ACCT_BACKUP') AND
                 (NEW."EmployeeId",NEW."CompanyId",NEW."OrganizationId",NEW."Issuer",NEW."Subject",NEW."IdentityType") IS DISTINCT FROM
                 (OLD."EmployeeId",OLD."CompanyId",OLD."OrganizationId",OLD."Issuer",OLD."Subject",OLD."IdentityType") THEN
                RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Accounts backup identity ownership and authority type are immutable.';
              END IF;
              IF NEW."IdentityType"<>'HUMAN_ACCT_BACKUP' THEN RETURN NEW; END IF;
              IF TG_OP='UPDATE' AND NOT NEW."IsActive" THEN RETURN NEW; END IF;
              approved_date:=NEW."EffectiveFrom";
              IF NOT EXISTS(SELECT 1 FROM advance.employees e WHERE e."Id"=NEW."EmployeeId" AND e."EmployeeCode"='SESS-41'
                   AND e."LoginEnabled" AND upper(e."Status")='ACTIVE') OR
                 NOT EXISTS(SELECT 1 FROM advance.companies c WHERE c."Id"=NEW."CompanyId" AND c."Code"=NEW."OrganizationId"
                   AND c."Code" IN('SESS_PVT_LTD','SESS_PROPRIETORSHIP') AND c."IsActive" AND c."Status"='ACTIVE') OR
                 NOT EXISTS(SELECT 1 FROM advance.employee_company_assignments a WHERE a."CompanyId"=NEW."CompanyId"
                   AND a."EmployeeId"=NEW."EmployeeId" AND a."IsActive" AND a."Status"='ACTIVE' AND a."EffectiveFrom"<=approved_date
                   AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=approved_date)) OR
                 NOT EXISTS(SELECT 1 FROM advance.employee_identity_mappings m JOIN advance.employee_role_assignments a
                   ON a."EmployeeId"=m."EmployeeId" AND a."CompanyId"=m."CompanyId" JOIN advance.roles r ON r."Id"=a."RoleId"
                   WHERE m."CompanyId"=NEW."CompanyId" AND m."OrganizationId"=NEW."OrganizationId" AND m."Issuer"=NEW."Issuer"
                     AND m."IdentityType"='HUMAN' AND m."IsActive" AND m."EffectiveFrom"<=approved_date
                     AND (m."EffectiveTo" IS NULL OR m."EffectiveTo">=approved_date) AND r."Code" IN('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR')
                     AND r."IsActive" AND a."ApprovalStatus" IN('Approved','SeedApproved') AND a."AssignmentType" IN('FULL','TEMPORARY')
                     AND a."EffectiveFrom"<=approved_date AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=approved_date)) OR
                 EXISTS(SELECT 1 FROM advance.employee_identity_mappings m WHERE m."Issuer"=NEW."Issuer"
                   AND m."Subject"=NEW."Subject" AND m."EmployeeId"<>NEW."EmployeeId") OR
                 EXISTS(SELECT 1 FROM advance.employee_identity_mappings m WHERE m."CompanyId"=NEW."CompanyId"
                   AND m."EmployeeId"=NEW."EmployeeId" AND m."IdentityType"='HUMAN' AND m."Subject"=NEW."Subject") THEN
                RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Named accounts backup requires canonical SESS-41, approved company membership, a distinct individual subject and the established approvers issuer.';
              END IF;
              RETURN NEW;
            END $guard$;
            REVOKE ALL ON FUNCTION advance.guard_accounts_backup_identity() FROM PUBLIC;
            DO $acl$ DECLARE principal_count integer; BEGIN
              SELECT count(*) INTO principal_count FROM pg_roles WHERE rolname IN('nexa_erp_owner','nexa_erp_bootstrap','nexa_erp_migration','nexa_erp_runtime');
              IF principal_count NOT IN(0,4) THEN RAISE EXCEPTION 'Accounts identity migration refuses partial deployment principal state.'; END IF;
              IF principal_count=4 THEN
                ALTER FUNCTION advance.guard_accounts_backup_identity() OWNER TO nexa_erp_owner;
                REVOKE ALL ON FUNCTION advance.guard_accounts_backup_identity() FROM nexa_erp_bootstrap,nexa_erp_migration;
                GRANT EXECUTE ON FUNCTION advance.guard_accounts_backup_identity() TO nexa_erp_runtime;
              END IF;
            END $acl$;
            CREATE FUNCTION advance.accounts_identity_role_allowed(p_org text,p_employee uuid,p_issuer text,p_subject text,p_role text,p_operation text,p_assignment uuid)
            RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $profile$
              SELECT CASE WHEN NOT EXISTS(SELECT 1 FROM advance.employees named WHERE named."Id"=p_employee AND named."EmployeeCode"='SESS-41') THEN true
                WHEN p_role<>'ACCOUNTS_MANAGER' AND NOT EXISTS(SELECT 1 FROM advance.employee_identity_mappings backup
                  WHERE backup."EmployeeId"=p_employee AND backup."OrganizationId"=p_org AND backup."Issuer"=p_issuer
                    AND backup."Subject"=p_subject AND backup."IdentityType"='HUMAN_ACCT_BACKUP') THEN true
                ELSE (SELECT count(*)=1 AND bool_and(CASE WHEN m."IdentityType"='HUMAN_ACCT_BACKUP' THEN
                e."EmployeeCode"='SESS-41' AND p_role='ACCOUNTS_MANAGER' AND
                p_operation IN('DepartmentVerify','Approve','Reject','RequestRevision','RequestClarification',
                  'ApproveComparison','RejectComparison','RequestClarificationComparison','RequestRevisionComparison',
                  'ApprovePO','RejectPO','RequestClarificationPO','RequestRevisionPO','CreateTaxGstSetting',
                  'OpeningStock.ConfirmValue','StockAdjustment.Decide','StockAdjustment.Reject','AccountsVerify','VerifyVendorCommercial') AND
                EXISTS(SELECT 1 FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId"
                  WHERE a."Id"=p_assignment AND a."CompanyId"=m."CompanyId" AND a."EmployeeId"=e."Id" AND r."Code"='ACCOUNTS_MANAGER' AND r."IsActive"
                    AND a."AssignmentType" IN('FULL','TEMPORARY') AND a."ApprovalStatus" IN('Approved','SeedApproved')
                    AND a."EffectiveFrom"<=advance.current_business_date() AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=advance.current_business_date()))
                ELSE e."EmployeeCode"<>'SESS-41' OR p_role<>'ACCOUNTS_MANAGER' END)
              FROM advance.employee_identity_mappings m JOIN advance.employees e ON e."Id"=m."EmployeeId"
                JOIN advance.companies c ON c."Id"=m."CompanyId"
              WHERE m."OrganizationId"=p_org AND c."Code"=p_org AND c."IsActive" AND c."Status"='ACTIVE'
                AND m."EmployeeId"=p_employee AND m."Issuer"=p_issuer AND m."Subject"=p_subject AND m."IsActive"
                AND e."LoginEnabled" AND upper(e."Status")='ACTIVE' AND m."EffectiveFrom"<=advance.current_business_date()
                AND (m."EffectiveTo" IS NULL OR m."EffectiveTo">=advance.current_business_date())) END;
            $profile$;
            REVOKE ALL ON FUNCTION advance.accounts_identity_role_allowed(text,uuid,text,text,text,text,uuid) FROM PUBLIC;
            DO $acl$ BEGIN
              IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_owner') THEN
                ALTER FUNCTION advance.accounts_identity_role_allowed(text,uuid,text,text,text,text,uuid) OWNER TO nexa_erp_owner;
                REVOKE ALL ON FUNCTION advance.accounts_identity_role_allowed(text,uuid,text,text,text,text,uuid) FROM nexa_erp_bootstrap,nexa_erp_migration;
                GRANT EXECUTE ON FUNCTION advance.accounts_identity_role_allowed(text,uuid,text,text,text,text,uuid) TO nexa_erp_runtime;
              END IF;
            END $acl$;
            CREATE TRIGGER "TR_accounts_backup_identity" BEFORE INSERT OR UPDATE ON advance.employee_identity_mappings
              FOR EACH ROW EXECUTE FUNCTION advance.guard_accounts_backup_identity();
            -- No identities, role assignments, scopes or production grants are created by this migration.
            """));
        builder.Sql(InstalledFunctionSql.Rewrite(RegisterSignature,true,(RegisterBody,GuardedRegisterBody,1)));
    }
    protected override void Down(MigrationBuilder builder)
    {
        PostgreSqlClusterGuard.Require(builder);
        builder.Sql(MigrationText.Lf("""
            LOCK TABLE advance.employee_identity_mappings,advance.employee_role_assignments,advance.command_requests IN SHARE ROW EXCLUSIVE MODE;
            DO $guard$ BEGIN
              IF EXISTS(SELECT 1 FROM advance.employee_identity_mappings WHERE "IdentityType"='HUMAN_ACCT_BACKUP') OR
                 EXISTS(SELECT 1 FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId"
                   JOIN advance.employees e ON e."Id"=a."EmployeeId" WHERE e."EmployeeCode"='SESS-41' AND r."Code"='ACCOUNTS_MANAGER') OR
                 EXISTS(SELECT 1 FROM advance.command_requests q JOIN advance.employees e ON e."Id"=q."ActorEmployeeId"
                   WHERE e."EmployeeCode"='SESS-41' AND q."ActorRoleCode"='ACCOUNTS_MANAGER') THEN
                RAISE EXCEPTION 'Accounts identity rollback refuses retained identities, accounts assignments or commands; retain forward code and evidence.';
              END IF;
            END $guard$;
            """));
        builder.Sql(InstalledFunctionSql.Rewrite(RegisterSignature,true,(GuardedRegisterBody,RegisterBody,1)));
        builder.Sql(MigrationText.Lf("""
            DROP FUNCTION advance.accounts_identity_role_allowed(text,uuid,text,text,text,text,uuid);
            DROP TRIGGER "TR_accounts_backup_identity" ON advance.employee_identity_mappings;
            DROP FUNCTION advance.guard_accounts_backup_identity();
            DROP INDEX advance."IX_employee_identity_accounts_backup";
            ALTER TABLE advance.employee_identity_mappings DROP CONSTRAINT "CK_employee_identity_mapping_type";
            ALTER TABLE advance.employee_identity_mappings ADD CONSTRAINT "CK_employee_identity_mapping_type"
              CHECK("IdentityType" IN('HUMAN','SERVICE'));
            """));
    }
}
