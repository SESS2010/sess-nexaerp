using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ControlledTaxGstWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql("""
                DO $preflight$
                BEGIN
                  IF EXISTS (SELECT 1 FROM advance.tax_gst_settings) THEN
                    RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='tax_gst_creator_preflight',
                      MESSAGE='Existing GST rows have no trustworthy creator employee. Migrate them explicitly before applying ControlledTaxGstWorkflow.';
                  END IF;
                END $preflight$;
                """);
            migrationBuilder.AddColumn<Guid>(
                name: "CreatorEmployeeId",
                schema: "advance",
                table: "tax_gst_settings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DecisionAt",
                schema: "advance",
                table: "tax_gst_settings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DecisionEmployeeId",
                schema: "advance",
                table: "tax_gst_settings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecisionRemarks",
                schema: "advance",
                table: "tax_gst_settings",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecisionRoleCode",
                schema: "advance",
                table: "tax_gst_settings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupersedesTaxGstSettingId",
                schema: "advance",
                table: "tax_gst_settings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tax_gst_settings_CreatorEmployeeId",
                schema: "advance",
                table: "tax_gst_settings",
                column: "CreatorEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_tax_gst_settings_DecisionEmployeeId",
                schema: "advance",
                table: "tax_gst_settings",
                column: "DecisionEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_tax_gst_settings_SupersedesTaxGstSettingId",
                schema: "advance",
                table: "tax_gst_settings",
                column: "SupersedesTaxGstSettingId",
                unique: true,
                filter: "\"SupersedesTaxGstSettingId\" IS NOT NULL AND \"ApprovalStatus\" = 'Approved'");

            migrationBuilder.AddForeignKey(
                name: "FK_tax_gst_settings_employees_CreatorEmployeeId",
                schema: "advance",
                table: "tax_gst_settings",
                column: "CreatorEmployeeId",
                principalSchema: "advance",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tax_gst_settings_employees_DecisionEmployeeId",
                schema: "advance",
                table: "tax_gst_settings",
                column: "DecisionEmployeeId",
                principalSchema: "advance",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tax_gst_settings_tax_gst_settings_SupersedesTaxGstSettingId",
                schema: "advance",
                table: "tax_gst_settings",
                column: "SupersedesTaxGstSettingId",
                principalSchema: "advance",
                principalTable: "tax_gst_settings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(ControlledTaxGstWorkflowSql.Up);
            migrationBuilder.Sql(AdvanceDatabaseContractSql.ReconcileRev869BTransitionGuard);
            migrationBuilder.Sql(AdvanceDatabaseContractSql.ReconcileRev869BParentGuard);
            migrationBuilder.Sql(AdvanceDatabaseContractSql.ReconcileRev869BSnapshotGuard);
            migrationBuilder.Sql(Rev869BDatabaseSafetySql.ReconcileInvitationImmutability);
            migrationBuilder.Sql(Rev869BDatabaseSafetySql.ReconcileAuthoritativeJsonObjectCounts);
            migrationBuilder.Sql(Rev869BControlledMutationSql.ReconcileExplicitMutationGuard);
            migrationBuilder.Sql(Rev869BCommandContextSql.ReconcileCommercialSnapshotHelperAcl);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(Rev869BCommandContextSql.RemoveCommercialSnapshotHelperAcl);
            migrationBuilder.Sql(Rev869BDatabaseSafetySql.RestoreAuthoritativeTransition);
            migrationBuilder.Sql(Rev869BDatabaseSafetySql.RestoreInvitationImmutability);
            migrationBuilder.Sql(AdvanceDatabaseContractSql.RestoreRev869BGuards);
            migrationBuilder.Sql(ControlledTaxGstWorkflowSql.Down);
            migrationBuilder.DropForeignKey(
                name: "FK_tax_gst_settings_employees_CreatorEmployeeId",
                schema: "advance",
                table: "tax_gst_settings");

            migrationBuilder.DropForeignKey(
                name: "FK_tax_gst_settings_employees_DecisionEmployeeId",
                schema: "advance",
                table: "tax_gst_settings");

            migrationBuilder.DropForeignKey(
                name: "FK_tax_gst_settings_tax_gst_settings_SupersedesTaxGstSettingId",
                schema: "advance",
                table: "tax_gst_settings");

            migrationBuilder.DropIndex(
                name: "IX_tax_gst_settings_CreatorEmployeeId",
                schema: "advance",
                table: "tax_gst_settings");

            migrationBuilder.DropIndex(
                name: "IX_tax_gst_settings_DecisionEmployeeId",
                schema: "advance",
                table: "tax_gst_settings");

            migrationBuilder.DropIndex(
                name: "IX_tax_gst_settings_SupersedesTaxGstSettingId",
                schema: "advance",
                table: "tax_gst_settings");

            migrationBuilder.DropColumn(
                name: "CreatorEmployeeId",
                schema: "advance",
                table: "tax_gst_settings");

            migrationBuilder.DropColumn(
                name: "DecisionAt",
                schema: "advance",
                table: "tax_gst_settings");

            migrationBuilder.DropColumn(
                name: "DecisionEmployeeId",
                schema: "advance",
                table: "tax_gst_settings");

            migrationBuilder.DropColumn(
                name: "DecisionRemarks",
                schema: "advance",
                table: "tax_gst_settings");

            migrationBuilder.DropColumn(
                name: "DecisionRoleCode",
                schema: "advance",
                table: "tax_gst_settings");

            migrationBuilder.DropColumn(
                name: "SupersedesTaxGstSettingId",
                schema: "advance",
                table: "tax_gst_settings");
        }
    }
}
