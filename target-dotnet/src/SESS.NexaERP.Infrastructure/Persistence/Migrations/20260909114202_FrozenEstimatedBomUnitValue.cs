using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FrozenEstimatedBomUnitValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.AddColumn<decimal>(
                name: "PlannedUnitValue",
                schema: "advance",
                table: "production_bom_lines",
                type: "numeric(20,6)",
                precision: 20,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedUnitValue",
                schema: "advance",
                table: "estimated_bom_lines",
                type: "numeric(20,6)",
                precision: 20,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EstimatedUnitValueOverridden",
                schema: "advance",
                table: "estimated_bom_lines",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddCheckConstraint(
                name: "CK_production_bom_line_value",
                schema: "advance",
                table: "production_bom_lines",
                sql: "\"PlannedUnitValue\" IS NULL OR \"PlannedUnitValue\">=0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_estimated_bom_line_value",
                schema: "advance",
                table: "estimated_bom_lines",
                sql: "\"EstimatedUnitValue\" IS NULL OR \"EstimatedUnitValue\">=0");
            migrationBuilder.Sql(FrozenEstimatedBomUnitValueSql.Up);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.DropCheckConstraint(
                name: "CK_production_bom_line_value",
                schema: "advance",
                table: "production_bom_lines");

            migrationBuilder.DropCheckConstraint(
                name: "CK_estimated_bom_line_value",
                schema: "advance",
                table: "estimated_bom_lines");

            migrationBuilder.DropColumn(
                name: "PlannedUnitValue",
                schema: "advance",
                table: "production_bom_lines");

            migrationBuilder.DropColumn(
                name: "EstimatedUnitValue",
                schema: "advance",
                table: "estimated_bom_lines");

            migrationBuilder.DropColumn(
                name: "EstimatedUnitValueOverridden",
                schema: "advance",
                table: "estimated_bom_lines");
            migrationBuilder.Sql(FrozenEstimatedBomUnitValueSql.Down);
        }
    }
}
