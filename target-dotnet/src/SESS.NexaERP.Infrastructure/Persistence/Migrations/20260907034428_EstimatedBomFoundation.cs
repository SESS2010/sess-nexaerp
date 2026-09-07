using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EstimatedBomFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DO $$ BEGIN IF current_setting('server_version_num')::integer < 170000 OR current_database() IN ('postgres','template0','template1') THEN RAISE EXCEPTION 'Estimated BOM migration cluster guard refused this database.'; END IF; END $$;");
            migrationBuilder.CreateTable(
                name: "estimated_boms",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BomNumber = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    JobOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentRevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    ApprovedRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estimated_boms", x => x.Id);
                    table.UniqueConstraint("AK_estimated_boms_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_estimated_boms_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_estimated_boms_job_orders_CompanyId_JobOrderId",
                        columns: x => new { x.CompanyId, x.JobOrderId },
                        principalSchema: "advance",
                        principalTable: "job_orders",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "estimated_bom_revisions",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EstimatedBomId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RevisionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PreparedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovalReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ContentFingerprint = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estimated_bom_revisions", x => x.Id);
                    table.UniqueConstraint("AK_estimated_bom_revisions_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_estimated_bom_revisions_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_estimated_bom_revisions_employees_ApprovedByEmployeeId",
                        column: x => x.ApprovedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_estimated_bom_revisions_employees_PreparedByEmployeeId",
                        column: x => x.PreparedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_estimated_bom_revisions_estimated_boms_CompanyId_EstimatedB~",
                        columns: x => new { x.CompanyId, x.EstimatedBomId },
                        principalSchema: "advance",
                        principalTable: "estimated_boms",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "estimated_bom_lines",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    EstimatedBomRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    UomId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estimated_bom_lines", x => x.Id);
                    table.UniqueConstraint("AK_estimated_bom_lines_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_estimated_bom_lines_estimated_bom_revisions_CompanyId_Estim~",
                        columns: x => new { x.CompanyId, x.EstimatedBomRevisionId },
                        principalSchema: "advance",
                        principalTable: "estimated_bom_revisions",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_estimated_bom_lines_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "advance",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_estimated_bom_lines_uoms_UomId",
                        column: x => x.UomId,
                        principalSchema: "advance",
                        principalTable: "uoms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_estimated_bom_lines_CompanyId_EstimatedBomRevisionId",
                schema: "advance",
                table: "estimated_bom_lines",
                columns: new[] { "CompanyId", "EstimatedBomRevisionId" });

            migrationBuilder.CreateIndex(
                name: "IX_estimated_bom_lines_CompanyId_ItemId",
                schema: "advance",
                table: "estimated_bom_lines",
                columns: new[] { "CompanyId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_estimated_bom_lines_EstimatedBomRevisionId_LineNumber",
                schema: "advance",
                table: "estimated_bom_lines",
                columns: new[] { "EstimatedBomRevisionId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_estimated_bom_lines_ItemId",
                schema: "advance",
                table: "estimated_bom_lines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_estimated_bom_lines_UomId",
                schema: "advance",
                table: "estimated_bom_lines",
                column: "UomId");

            migrationBuilder.CreateIndex(
                name: "IX_estimated_bom_revisions_ApprovedByEmployeeId",
                schema: "advance",
                table: "estimated_bom_revisions",
                column: "ApprovedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_estimated_bom_revisions_CompanyId",
                schema: "advance",
                table: "estimated_bom_revisions",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_estimated_bom_revisions_CompanyId_EstimatedBomId",
                schema: "advance",
                table: "estimated_bom_revisions",
                columns: new[] { "CompanyId", "EstimatedBomId" });

            migrationBuilder.CreateIndex(
                name: "IX_estimated_bom_revisions_CompanyId_IdempotencyKey",
                schema: "advance",
                table: "estimated_bom_revisions",
                columns: new[] { "CompanyId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_estimated_bom_revisions_EstimatedBomId_RevisionNumber",
                schema: "advance",
                table: "estimated_bom_revisions",
                columns: new[] { "EstimatedBomId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_estimated_bom_revisions_PreparedByEmployeeId",
                schema: "advance",
                table: "estimated_bom_revisions",
                column: "PreparedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_estimated_boms_CompanyId",
                schema: "advance",
                table: "estimated_boms",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_estimated_boms_CompanyId_BomNumber",
                schema: "advance",
                table: "estimated_boms",
                columns: new[] { "CompanyId", "BomNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_estimated_boms_CompanyId_JobOrderId",
                schema: "advance",
                table: "estimated_boms",
                columns: new[] { "CompanyId", "JobOrderId" },
                unique: true);
            migrationBuilder.Sql("""
                CREATE FUNCTION advance.estimated_bom_revision_immutable() RETURNS trigger LANGUAGE plpgsql AS $f$
                BEGIN
                  IF TG_OP IN ('UPDATE','DELETE') AND to_jsonb(OLD)->>'Status'='APPROVED' THEN RAISE EXCEPTION 'Approved Estimated BOM revisions are immutable.'; END IF;
                  RETURN CASE WHEN TG_OP='DELETE' THEN OLD ELSE NEW END;
                END $f$;
                CREATE TRIGGER estimated_bom_revision_immutable BEFORE UPDATE OR DELETE ON advance.estimated_bom_revisions FOR EACH ROW EXECUTE FUNCTION advance.estimated_bom_revision_immutable();
                CREATE FUNCTION advance.estimated_bom_line_immutable() RETURNS trigger LANGUAGE plpgsql AS $f$
                DECLARE rid uuid; frozen boolean;
                BEGIN
                  rid:=coalesce((to_jsonb(OLD)->>'EstimatedBomRevisionId')::uuid,(to_jsonb(NEW)->>'EstimatedBomRevisionId')::uuid);
                  SELECT to_jsonb(r)->>'Status'='APPROVED' INTO frozen FROM advance.estimated_bom_revisions r WHERE (to_jsonb(r)->>'Id')::uuid=rid;
                  IF frozen THEN RAISE EXCEPTION 'Approved Estimated BOM lines are immutable.'; END IF;
                  RETURN CASE WHEN TG_OP='DELETE' THEN OLD ELSE NEW END;
                END $f$;
                CREATE TRIGGER estimated_bom_line_immutable BEFORE INSERT OR UPDATE OR DELETE ON advance.estimated_bom_lines FOR EACH ROW EXECUTE FUNCTION advance.estimated_bom_line_immutable();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS (SELECT 1 FROM advance.estimated_boms) THEN RAISE EXCEPTION 'Estimated BOM rollback refuses persisted BOM evidence.'; END IF; END $$; DROP FUNCTION advance.estimated_bom_line_immutable() CASCADE; DROP FUNCTION advance.estimated_bom_revision_immutable() CASCADE;");
            migrationBuilder.DropTable(
                name: "estimated_bom_lines",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "estimated_bom_revisions",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "estimated_boms",
                schema: "advance");
        }
    }
}
