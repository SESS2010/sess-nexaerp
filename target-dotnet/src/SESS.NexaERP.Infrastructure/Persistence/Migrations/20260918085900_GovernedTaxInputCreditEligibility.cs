using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260918085900_GovernedTaxInputCreditEligibility")]
public sealed class GovernedTaxInputCreditEligibility : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.AddColumn<string>(name: "ItcEligibility", schema: "advance", table: "tax_gst_settings",
            type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "FULLY_RECOVERABLE");
        migrationBuilder.AddColumn<decimal>(name: "RecoverableTaxPercent", schema: "advance", table: "tax_gst_settings",
            type: "numeric(9,6)", precision: 9, scale: 6, nullable: true);
        migrationBuilder.AddCheckConstraint(name: "CK_tax_gst_itc_eligibility", schema: "advance", table: "tax_gst_settings",
            sql: """("ItcEligibility" IN ('FULLY_RECOVERABLE','BLOCKED') AND "RecoverableTaxPercent" IS NULL) OR ("ItcEligibility"='PARTIALLY_RECOVERABLE' AND "RecoverableTaxPercent" IS NOT NULL AND "RecoverableTaxPercent">0 AND "RecoverableTaxPercent"<100)""");
        migrationBuilder.Sql(GovernedTaxInputCreditSql.Install);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(GovernedTaxInputCreditSql.Restore);
        migrationBuilder.DropCheckConstraint(name: "CK_tax_gst_itc_eligibility", schema: "advance", table: "tax_gst_settings");
        migrationBuilder.DropColumn(name: "RecoverableTaxPercent", schema: "advance", table: "tax_gst_settings");
        migrationBuilder.DropColumn(name: "ItcEligibility", schema: "advance", table: "tax_gst_settings");
    }
}
