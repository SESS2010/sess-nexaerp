using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>
/// DC print addendum (approved 26 September 2026): a machine DC may carry the vehicle number, the
/// transporter and the e-way bill number and date, all optional, so the printed DC shows them.
/// <c>record_machine_delivery</c> is rewritten from its installed body with only these four values
/// added to the dispatch insert; every rule in it is unchanged (the IST date finding F6 is not part
/// of this). Down refuses once any DC carries one of the new values, then reverses the rewrite.
/// </summary>
[DbContext(typeof(NexaErpDbContext))]
[Migration("20260926120000_MachineDeliveryDispatchDetails")]
public sealed class MachineDeliveryDispatchDetails : Migration
{
    private const string Signature = "advance.record_machine_delivery(uuid,uuid,uuid,text,jsonb,bytea,uuid,text,uuid,text,text)";
    private const string ColumnsBefore = "\"FatReconciliationId\",\"ActorEmployeeId\",\"RoleAssignmentId\",\"RecordedBy\")";
    private const string ColumnsAfter = "\"FatReconciliationId\",\"ActorEmployeeId\",\"RoleAssignmentId\",\"RecordedBy\",\"VehicleNo\",\"Transporter\",\"EwayBillNo\",\"EwayBillDate\")";
    private const string ValuesBefore = "job.\"LatestFatReconciliationId\",p_actor,p_assignment,p_login);";
    private const string ValuesAfter = "job.\"LatestFatReconciliationId\",p_actor,p_assignment,p_login,nullif(btrim(p_payload->>'vehicleNo'),''),nullif(btrim(p_payload->>'transporter'),''),nullif(btrim(p_payload->>'ewayBillNo'),''),(p_payload->>'ewayBillDate')::date);";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            ALTER TABLE advance.machine_delivery_challans
              ADD COLUMN "VehicleNo" text CHECK("VehicleNo" IS NULL OR length(btrim("VehicleNo")) BETWEEN 1 AND 30),
              ADD COLUMN "Transporter" text CHECK("Transporter" IS NULL OR length(btrim("Transporter")) BETWEEN 1 AND 200),
              ADD COLUMN "EwayBillNo" text CHECK("EwayBillNo" IS NULL OR "EwayBillNo" ~ '^[0-9]{12}$'),
              ADD COLUMN "EwayBillDate" date,
              ADD CONSTRAINT "CK_machine_delivery_challans_eway_bill" CHECK(("EwayBillNo" IS NULL) = ("EwayBillDate" IS NULL) AND ("EwayBillDate" IS NULL OR "EwayBillDate"<="DispatchDate"));
            """);
        migrationBuilder.Sql(InstalledFunctionSql.Rewrite(Signature, (ColumnsBefore, ColumnsAfter), (ValuesBefore, ValuesAfter)));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            DO $guard$ BEGIN
              IF EXISTS(SELECT 1 FROM advance.machine_delivery_challans WHERE "VehicleNo" IS NOT NULL OR "Transporter" IS NOT NULL OR "EwayBillNo" IS NOT NULL)
              THEN RAISE EXCEPTION 'Machine DC dispatch-details rollback refuses retained vehicle, transporter or e-way bill evidence.'; END IF;
            END $guard$;
            """);
        migrationBuilder.Sql(InstalledFunctionSql.Rewrite(Signature, (ColumnsAfter, ColumnsBefore), (ValuesAfter, ValuesBefore)));
        migrationBuilder.Sql("""
            ALTER TABLE advance.machine_delivery_challans
              DROP CONSTRAINT "CK_machine_delivery_challans_eway_bill",
              DROP COLUMN "EwayBillDate", DROP COLUMN "EwayBillNo", DROP COLUMN "Transporter", DROP COLUMN "VehicleNo";
            """);
    }
}
