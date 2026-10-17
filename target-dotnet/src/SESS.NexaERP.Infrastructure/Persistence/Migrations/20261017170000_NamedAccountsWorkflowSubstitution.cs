using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20261017170000_NamedAccountsWorkflowSubstitution")]
public sealed class NamedAccountsWorkflowSubstitution : Migration
{
    private const string Helper="advance.accounts_backup_step_actor(uuid,uuid,text,integer,jsonb,uuid,uuid,text,uuid)";
    private const string Original="NEW.\"ResolvedEmployeeId\"<>(snapshot#>>ARRAY['steps',(NEW.\"StepNumber\"-1)::text,'employeeId'])::uuid";
    private const string Replacement="""
        (NEW."ResolvedEmployeeId" IS DISTINCT FROM (snapshot#>>ARRAY['steps',(NEW."StepNumber"-1)::text,'employeeId'])::uuid
          AND advance.accounts_backup_step_actor(NEW."CompanyId",NEW."ResolvedEmployeeId",NEW."ActorLoginId",NEW."StepNumber",snapshot,
            coalesce((parentj->>'DeliveryWarehouseId')::uuid,(SELECT r."DeliveryWarehouseId" FROM advance.request_for_quotations r
              WHERE r."Id"=(parentj->>'RequestForQuotationId')::uuid AND r."CompanyId"=NEW."CompanyId")),
            coalesce((parentj->>'RequesterEmployeeId')::uuid,(parentj->>'OwnerEmployeeId')::uuid,(parentj->>'CreatorEmployeeId')::uuid),
            TG_ARGV[0],parent_id) IS NOT TRUE)
        """;
    protected override void Up(MigrationBuilder builder)
    {
        PostgreSqlClusterGuard.Require(builder);
        builder.Sql(MigrationText.Lf("""
            CREATE FUNCTION advance.accounts_backup_step_actor(p_company uuid,p_actor uuid,p_subject text,p_step integer,p_snapshot jsonb,
              p_warehouse uuid,p_owner uuid,p_kind text,p_document uuid)
            RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $backup$
              SELECT coalesce(p_step=1 AND p_kind IN('purchase_requisitions','commercial_comparisons','purchase_orders')
                AND p_owner IS DISTINCT FROM p_actor
                AND p_snapshot#>>'{steps,0,resolutionType}'='DEPARTMENT_MAPPING'
                AND p_snapshot#>>'{steps,0,employeeCode}'='SESS-14'
                AND p_snapshot#>>'{steps,0,roleCode}'='ACCOUNTS_MANAGER'
                AND EXISTS(SELECT 1 FROM advance.employees primary_employee
                  WHERE primary_employee."Id"=(p_snapshot#>>'{steps,0,employeeId}')::uuid AND primary_employee."EmployeeCode"='SESS-14')
                AND EXISTS(SELECT 1 FROM advance.employee_identity_mappings m
                  JOIN advance.employees e ON e."Id"=m."EmployeeId"
                  JOIN advance.companies c ON c."Id"=m."CompanyId"
                  JOIN advance.employee_company_assignments member ON member."EmployeeId"=e."Id" AND member."CompanyId"=c."Id"
                  JOIN advance.employee_role_assignments a ON a."EmployeeId"=e."Id" AND a."CompanyId"=c."Id"
                  JOIN advance.roles r ON r."Id"=a."RoleId"
                  WHERE m."CompanyId"=p_company AND m."EmployeeId"=p_actor AND m."Subject"=p_subject
                    AND m."IdentityType"='HUMAN_ACCT_BACKUP' AND m."IsActive"
                    AND m."EffectiveFrom"<=advance.current_business_date() AND (m."EffectiveTo" IS NULL OR m."EffectiveTo">=advance.current_business_date())
                    AND e."EmployeeCode"='SESS-41' AND e."LoginEnabled" AND upper(e."Status")='ACTIVE'
                    AND c."Code"=m."OrganizationId" AND c."Code"=p_snapshot->>'organizationId' AND c."IsActive" AND c."Status"='ACTIVE'
                    AND member."IsActive" AND member."Status"='ACTIVE' AND member."EffectiveFrom"<=advance.current_business_date()
                    AND (member."EffectiveTo" IS NULL OR member."EffectiveTo">=advance.current_business_date())
                    AND r."Code"='ACCOUNTS_MANAGER' AND r."IsActive" AND a."AssignmentType" IN('FULL','TEMPORARY')
                    AND a."ApprovalStatus" IN('Approved','SeedApproved') AND a."EffectiveFrom"<=advance.current_business_date()
                    AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=advance.current_business_date())
                    AND EXISTS(SELECT 1 FROM advance.company_role_activations activation WHERE activation."CompanyId"=c."Id"
                      AND activation."RoleId"=r."Id" AND activation."IsEnabled" AND activation."EffectiveFrom"<=advance.current_business_date()
                      AND (activation."EffectiveTo" IS NULL OR activation."EffectiveTo">=advance.current_business_date()))
                    AND EXISTS(SELECT 1 FROM advance.employee_operational_scopes s
                      JOIN advance.employee_department_assignments d ON d."EmployeeCompanyAssignmentId"=member."Id" AND d."CompanyId"=c."Id"
                      JOIN advance.departments dept ON dept."Id"=d."DepartmentId" AND dept."IsActive"
                      WHERE s."EmployeeId"=e."Id" AND s."CompanyId"=c."Id" AND s."OrganizationId"=c."Code"
                        AND s."IsActive" AND s."EffectiveFrom"<=advance.current_business_date()
                        AND (s."EffectiveTo" IS NULL OR s."EffectiveTo">=advance.current_business_date())
                        AND s."RackBinId" IS NULL AND (s."WarehouseId" IS NULL OR s."WarehouseId"=p_warehouse) AND NOT s."OwnRecordsOnly"
                        AND d."IsActive" AND d."Status"='ACTIVE' AND d."EffectiveFrom"<=advance.current_business_date()
                        AND (d."EffectiveTo" IS NULL OR d."EffectiveTo">=advance.current_business_date())
                        AND (s."DepartmentId" IS NULL OR s."DepartmentId"=d."DepartmentId")))
                AND NOT EXISTS(SELECT 1 FROM advance.stock_availability_checks h WHERE p_kind='purchase_requisitions'
                  AND h."CompanyId"=p_company AND h."PurchaseRequisitionId"=p_document
                  AND (h."CheckedBy" IN('SESS-41','sess-41') OR EXISTS(SELECT 1 FROM advance.employee_identity_mappings attribution
                    WHERE attribution."EmployeeId"=p_actor AND attribution."CompanyId"=p_company AND attribution."Subject"=h."CheckedBy")))
                AND NOT EXISTS(SELECT 1 FROM advance.purchase_requisition_approval_history h WHERE p_kind='purchase_requisitions'
                  AND h."CompanyId"=p_company AND h."PurchaseRequisitionId"=p_document AND h."ResolvedEmployeeId"=p_actor
                  AND h."ActorRoleCode" IN('STORES_MANAGER','STORES_EXECUTIVE','STORES_ASSISTANT'))
                AND NOT EXISTS(SELECT 1 FROM advance.purchase_transaction_approval_history h WHERE p_kind='commercial_comparisons'
                  AND h."CompanyId"=p_company AND h."CommercialComparisonId"=p_document AND h."ActorEmployeeId"=p_actor
                  AND h."ActorRoleCode" IN('STORES_MANAGER','STORES_EXECUTIVE','STORES_ASSISTANT'))
                AND NOT EXISTS(SELECT 1 FROM advance.purchase_order_history h WHERE p_kind='purchase_orders'
                  AND h."CompanyId"=p_company AND h."PurchaseOrderId"=p_document AND h."ActorEmployeeId"=p_actor
                  AND (h."Action"='Issue' OR h."ActorRoleCode" IN('STORES_MANAGER','STORES_EXECUTIVE','STORES_ASSISTANT'))),false);
            $backup$;
            REVOKE ALL ON FUNCTION advance.accounts_backup_step_actor(uuid,uuid,text,integer,jsonb,uuid,uuid,text,uuid) FROM PUBLIC;
            DO $acl$ DECLARE principal_count integer; BEGIN
              SELECT count(*) INTO principal_count FROM pg_roles WHERE rolname IN('nexa_erp_owner','nexa_erp_bootstrap','nexa_erp_migration','nexa_erp_runtime');
              IF principal_count NOT IN(0,4) THEN RAISE EXCEPTION 'Accounts workflow migration refuses partial principal provisioning.'; END IF;
              IF principal_count=4 THEN
                ALTER FUNCTION advance.accounts_backup_step_actor(uuid,uuid,text,integer,jsonb,uuid,uuid,text,uuid) OWNER TO nexa_erp_owner;
                REVOKE ALL ON FUNCTION advance.accounts_backup_step_actor(uuid,uuid,text,integer,jsonb,uuid,uuid,text,uuid) FROM nexa_erp_bootstrap,nexa_erp_migration;
                GRANT EXECUTE ON FUNCTION advance.accounts_backup_step_actor(uuid,uuid,text,integer,jsonb,uuid,uuid,text,uuid) TO nexa_erp_runtime;
              END IF;
            END $acl$;
            """));
        builder.Sql(InstalledFunctionSql.Rewrite("advance.purchase_approval_decision_guard()",true,(Original,Replacement,1)));
    }
    protected override void Down(MigrationBuilder builder)
    {
        PostgreSqlClusterGuard.Require(builder);
        builder.Sql(MigrationText.Lf("""
            LOCK TABLE advance.employee_identity_mappings,advance.purchase_requisition_approval_history,
              advance.purchase_transaction_approval_history,advance.purchase_order_history IN SHARE ROW EXCLUSIVE MODE;
            DO $guard$ BEGIN
              IF EXISTS(SELECT 1 FROM advance.employee_identity_mappings WHERE "IdentityType"='HUMAN_ACCT_BACKUP') OR
                EXISTS(SELECT 1 FROM advance.purchase_requisition_approval_history h JOIN advance.employees e ON e."Id"=h."ResolvedEmployeeId"
                  WHERE e."EmployeeCode"='SESS-41' AND h."ResolvedRoleCode"='ACCOUNTS_MANAGER') OR
                EXISTS(SELECT 1 FROM advance.purchase_transaction_approval_history h JOIN advance.employees e ON e."Id"=h."ResolvedEmployeeId"
                  WHERE e."EmployeeCode"='SESS-41' AND h."ResolvedRoleCode"='ACCOUNTS_MANAGER') OR
                EXISTS(SELECT 1 FROM advance.purchase_order_history h JOIN advance.employees e ON e."Id"=h."ResolvedEmployeeId"
                  WHERE e."EmployeeCode"='SESS-41' AND h."ResolvedRoleCode"='ACCOUNTS_MANAGER') THEN
                RAISE EXCEPTION 'Accounts workflow rollback refuses retained backup identities or accounts decisions.';
              END IF;
            END $guard$;
            """));
        builder.Sql(InstalledFunctionSql.Rewrite("advance.purchase_approval_decision_guard()",true,(Replacement,Original,1)));
        builder.Sql("DROP FUNCTION "+Helper+";");
    }
}