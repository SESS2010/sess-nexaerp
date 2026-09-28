using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>R1 decision 14: exclude dispatched jobs from selection and identify the job on the DC view.</summary>
[DbContext(typeof(NexaErpDbContext))]
[Migration("20260928100000_MachineDeliverySelection")]
public sealed class MachineDeliverySelection : Migration
{
    private const string ViewSignature = "advance.machine_delivery_json(uuid,uuid)";
    private const string Before = "to_jsonb(d)||jsonb_build_object('MachineState',";
    private const string After = "to_jsonb(d)||jsonb_build_object('JobOrderNumber',(SELECT j.\"JobOrderNumber\" FROM advance.job_orders j WHERE j.\"CompanyId\"=d.\"CompanyId\" AND j.\"Id\"=d.\"JobOrderId\"),'MachineState',";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            CREATE FUNCTION advance.machine_delivery_job_ids(p_company uuid) RETURNS SETOF uuid
            LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $dc$
            BEGIN
              IF session_user<>'nexa_erp_runtime' THEN
                RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Machine delivery reads require runtime.';
              END IF;
              RETURN QUERY SELECT d."JobOrderId" FROM advance.machine_delivery_challans d WHERE d."CompanyId"=p_company;
            END $dc$;
            REVOKE ALL ON FUNCTION advance.machine_delivery_job_ids(uuid) FROM PUBLIC;
            DO $acl$ DECLARE principal text; BEGIN
              IF to_regrole('nexa_erp_owner') IS NOT NULL THEN
                ALTER FUNCTION advance.machine_delivery_job_ids(uuid) OWNER TO nexa_erp_owner;
              END IF;
              FOREACH principal IN ARRAY ARRAY['nexa_erp_runtime','nexa_erp_bootstrap','nexa_erp_migration'] LOOP
                IF to_regrole(principal) IS NOT NULL THEN
                  EXECUTE format('REVOKE ALL ON FUNCTION advance.machine_delivery_job_ids(uuid) FROM %I',principal);
                END IF;
              END LOOP;
              IF to_regrole('nexa_erp_runtime') IS NOT NULL THEN
                GRANT EXECUTE ON FUNCTION advance.machine_delivery_job_ids(uuid) TO nexa_erp_runtime;
              END IF;
            END $acl$;
            """);
        migrationBuilder.Sql(InstalledFunctionSql.Rewrite(ViewSignature, (Before, After)));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        // Read projections only: no table, column, DC, signature or command receipt is removed.
        // Rewrite refuses an unexpected installed body instead of discarding another change.
        migrationBuilder.Sql(InstalledFunctionSql.Rewrite(ViewSignature, (After, Before)));
        migrationBuilder.Sql("DROP FUNCTION advance.machine_delivery_job_ids(uuid);");
    }
}
