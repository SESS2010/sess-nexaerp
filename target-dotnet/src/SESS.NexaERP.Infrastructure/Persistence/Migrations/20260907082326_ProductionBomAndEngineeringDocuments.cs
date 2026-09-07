using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProductionBomAndEngineeringDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(ProductionEngineeringSql.ClusterGuard);
            migrationBuilder.AddColumn<Guid>(
                name: "PinnedProductionBomRevisionId",
                schema: "advance",
                table: "job_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "production_boms",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BomNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    JobOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentRevisionNumber = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_production_boms", x => x.Id);
                    table.UniqueConstraint("AK_production_boms_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_production_boms_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_boms_job_orders_CompanyId_JobOrderId",
                        columns: x => new { x.CompanyId, x.JobOrderId },
                        principalSchema: "advance",
                        principalTable: "job_orders",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "production_bom_revisions",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionBomId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    SourceEstimatedBomRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupersedesRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_production_bom_revisions", x => x.Id);
                    table.UniqueConstraint("AK_production_bom_revisions_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_production_bom_revisions_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_bom_revisions_employees_ApprovedByEmployeeId",
                        column: x => x.ApprovedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_bom_revisions_employees_PreparedByEmployeeId",
                        column: x => x.PreparedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_bom_revisions_estimated_bom_revisions_CompanyId_~",
                        columns: x => new { x.CompanyId, x.SourceEstimatedBomRevisionId },
                        principalSchema: "advance",
                        principalTable: "estimated_bom_revisions",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_bom_revisions_production_bom_revisions_CompanyId~",
                        columns: x => new { x.CompanyId, x.SupersedesRevisionId },
                        principalSchema: "advance",
                        principalTable: "production_bom_revisions",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_bom_revisions_production_boms_CompanyId_Producti~",
                        columns: x => new { x.CompanyId, x.ProductionBomId },
                        principalSchema: "advance",
                        principalTable: "production_boms",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "production_bom_lines",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionBomRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_production_bom_lines", x => x.Id);
                    table.UniqueConstraint("AK_production_bom_lines_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_production_bom_lines_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "advance",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_bom_lines_production_bom_revisions_CompanyId_Pro~",
                        columns: x => new { x.CompanyId, x.ProductionBomRevisionId },
                        principalSchema: "advance",
                        principalTable: "production_bom_revisions",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_bom_lines_uoms_UomId",
                        column: x => x.UomId,
                        principalSchema: "advance",
                        principalTable: "uoms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "engineering_document_revisions",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EngineeringDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    RevisionCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SupersedesRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    DrawnByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevisionNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DocumentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    FileName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_engineering_document_revisions", x => x.Id);
                    table.UniqueConstraint("AK_engineering_document_revisions_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_engineering_document_revisions_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_engineering_document_revisions_employees_ApprovedByEmployee~",
                        column: x => x.ApprovedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_engineering_document_revisions_employees_CheckedByEmployeeId",
                        column: x => x.CheckedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_engineering_document_revisions_employees_DrawnByEmployeeId",
                        column: x => x.DrawnByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_engineering_document_revisions_engineering_document_revisio~",
                        columns: x => new { x.CompanyId, x.SupersedesRevisionId },
                        principalSchema: "advance",
                        principalTable: "engineering_document_revisions",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "engineering_documents",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    JobOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CurrentRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_engineering_documents", x => x.Id);
                    table.UniqueConstraint("AK_engineering_documents_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_engineering_documents_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_engineering_documents_engineering_document_revisions_Compan~",
                        columns: x => new { x.CompanyId, x.CurrentRevisionId },
                        principalSchema: "advance",
                        principalTable: "engineering_document_revisions",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_engineering_documents_job_orders_CompanyId_JobOrderId",
                        columns: x => new { x.CompanyId, x.JobOrderId },
                        principalSchema: "advance",
                        principalTable: "job_orders",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "production_engineering_history",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionBomId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductionBomRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    EngineeringDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    EngineeringDocumentRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ActorEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorRoleCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResolvedRoleAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResolvedRoleAssignmentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_engineering_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_production_engineering_history_employee_role_assignments_Re~",
                        column: x => x.ResolvedRoleAssignmentId,
                        principalSchema: "advance",
                        principalTable: "employee_role_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_engineering_history_employees_ActorEmployeeId",
                        column: x => x.ActorEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_engineering_history_engineering_document_revisio~",
                        columns: x => new { x.CompanyId, x.EngineeringDocumentRevisionId },
                        principalSchema: "advance",
                        principalTable: "engineering_document_revisions",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_engineering_history_engineering_documents_Compan~",
                        columns: x => new { x.CompanyId, x.EngineeringDocumentId },
                        principalSchema: "advance",
                        principalTable: "engineering_documents",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_engineering_history_production_bom_revisions_Com~",
                        columns: x => new { x.CompanyId, x.ProductionBomRevisionId },
                        principalSchema: "advance",
                        principalTable: "production_bom_revisions",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_engineering_history_production_boms_CompanyId_Pr~",
                        columns: x => new { x.CompanyId, x.ProductionBomId },
                        principalSchema: "advance",
                        principalTable: "production_boms",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_job_orders_CompanyId_PinnedProductionBomRevisionId",
                schema: "advance",
                table: "job_orders",
                columns: new[] { "CompanyId", "PinnedProductionBomRevisionId" });

            migrationBuilder.CreateIndex(
                name: "IX_engineering_document_revisions_ApprovedByEmployeeId",
                schema: "advance",
                table: "engineering_document_revisions",
                column: "ApprovedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_engineering_document_revisions_CheckedByEmployeeId",
                schema: "advance",
                table: "engineering_document_revisions",
                column: "CheckedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_engineering_document_revisions_CompanyId",
                schema: "advance",
                table: "engineering_document_revisions",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_engineering_document_revisions_CompanyId_EngineeringDocumen~",
                schema: "advance",
                table: "engineering_document_revisions",
                columns: new[] { "CompanyId", "EngineeringDocumentId" });

            migrationBuilder.CreateIndex(
                name: "IX_engineering_document_revisions_CompanyId_IdempotencyKey",
                schema: "advance",
                table: "engineering_document_revisions",
                columns: new[] { "CompanyId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_engineering_document_revisions_CompanyId_SupersedesRevision~",
                schema: "advance",
                table: "engineering_document_revisions",
                columns: new[] { "CompanyId", "SupersedesRevisionId" });

            migrationBuilder.CreateIndex(
                name: "IX_engineering_document_revisions_DrawnByEmployeeId",
                schema: "advance",
                table: "engineering_document_revisions",
                column: "DrawnByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_engineering_document_revisions_EngineeringDocumentId_Revisi~",
                schema: "advance",
                table: "engineering_document_revisions",
                columns: new[] { "EngineeringDocumentId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_engineering_documents_CompanyId",
                schema: "advance",
                table: "engineering_documents",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_engineering_documents_CompanyId_CurrentRevisionId",
                schema: "advance",
                table: "engineering_documents",
                columns: new[] { "CompanyId", "CurrentRevisionId" });

            migrationBuilder.CreateIndex(
                name: "IX_engineering_documents_CompanyId_DocumentNumber",
                schema: "advance",
                table: "engineering_documents",
                columns: new[] { "CompanyId", "DocumentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_engineering_documents_CompanyId_JobOrderId_DocumentType",
                schema: "advance",
                table: "engineering_documents",
                columns: new[] { "CompanyId", "JobOrderId", "DocumentType" });

            migrationBuilder.CreateIndex(
                name: "IX_production_bom_lines_CompanyId_ProductionBomRevisionId",
                schema: "advance",
                table: "production_bom_lines",
                columns: new[] { "CompanyId", "ProductionBomRevisionId" });

            migrationBuilder.CreateIndex(
                name: "IX_production_bom_lines_ItemId",
                schema: "advance",
                table: "production_bom_lines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_production_bom_lines_ProductionBomRevisionId_LineNumber",
                schema: "advance",
                table: "production_bom_lines",
                columns: new[] { "ProductionBomRevisionId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_production_bom_lines_UomId",
                schema: "advance",
                table: "production_bom_lines",
                column: "UomId");

            migrationBuilder.CreateIndex(
                name: "IX_production_bom_revisions_ApprovedByEmployeeId",
                schema: "advance",
                table: "production_bom_revisions",
                column: "ApprovedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_production_bom_revisions_CompanyId",
                schema: "advance",
                table: "production_bom_revisions",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_production_bom_revisions_CompanyId_IdempotencyKey",
                schema: "advance",
                table: "production_bom_revisions",
                columns: new[] { "CompanyId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_production_bom_revisions_CompanyId_ProductionBomId",
                schema: "advance",
                table: "production_bom_revisions",
                columns: new[] { "CompanyId", "ProductionBomId" });

            migrationBuilder.CreateIndex(
                name: "IX_production_bom_revisions_CompanyId_SourceEstimatedBomRevisi~",
                schema: "advance",
                table: "production_bom_revisions",
                columns: new[] { "CompanyId", "SourceEstimatedBomRevisionId" });

            migrationBuilder.CreateIndex(
                name: "IX_production_bom_revisions_CompanyId_SupersedesRevisionId",
                schema: "advance",
                table: "production_bom_revisions",
                columns: new[] { "CompanyId", "SupersedesRevisionId" });

            migrationBuilder.CreateIndex(
                name: "IX_production_bom_revisions_PreparedByEmployeeId",
                schema: "advance",
                table: "production_bom_revisions",
                column: "PreparedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_production_bom_revisions_ProductionBomId_RevisionNumber",
                schema: "advance",
                table: "production_bom_revisions",
                columns: new[] { "ProductionBomId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_production_boms_CompanyId",
                schema: "advance",
                table: "production_boms",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_production_boms_CompanyId_BomNumber",
                schema: "advance",
                table: "production_boms",
                columns: new[] { "CompanyId", "BomNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_production_boms_CompanyId_JobOrderId",
                schema: "advance",
                table: "production_boms",
                columns: new[] { "CompanyId", "JobOrderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_production_engineering_history_ActorEmployeeId",
                schema: "advance",
                table: "production_engineering_history",
                column: "ActorEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_production_engineering_history_CompanyId_CorrelationId",
                schema: "advance",
                table: "production_engineering_history",
                columns: new[] { "CompanyId", "CorrelationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_production_engineering_history_CompanyId_EngineeringDocume~1",
                schema: "advance",
                table: "production_engineering_history",
                columns: new[] { "CompanyId", "EngineeringDocumentRevisionId" });

            migrationBuilder.CreateIndex(
                name: "IX_production_engineering_history_CompanyId_EngineeringDocumen~",
                schema: "advance",
                table: "production_engineering_history",
                columns: new[] { "CompanyId", "EngineeringDocumentId" });

            migrationBuilder.CreateIndex(
                name: "IX_production_engineering_history_CompanyId_OccurredAt",
                schema: "advance",
                table: "production_engineering_history",
                columns: new[] { "CompanyId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_production_engineering_history_CompanyId_ProductionBomId",
                schema: "advance",
                table: "production_engineering_history",
                columns: new[] { "CompanyId", "ProductionBomId" });

            migrationBuilder.CreateIndex(
                name: "IX_production_engineering_history_CompanyId_ProductionBomRevis~",
                schema: "advance",
                table: "production_engineering_history",
                columns: new[] { "CompanyId", "ProductionBomRevisionId" });

            migrationBuilder.CreateIndex(
                name: "IX_production_engineering_history_ResolvedRoleAssignmentId",
                schema: "advance",
                table: "production_engineering_history",
                column: "ResolvedRoleAssignmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_job_orders_production_bom_revisions_CompanyId_PinnedProduct~",
                schema: "advance",
                table: "job_orders",
                columns: new[] { "CompanyId", "PinnedProductionBomRevisionId" },
                principalSchema: "advance",
                principalTable: "production_bom_revisions",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_engineering_document_revisions_engineering_documents_Compan~",
                schema: "advance",
                table: "engineering_document_revisions",
                columns: new[] { "CompanyId", "EngineeringDocumentId" },
                principalSchema: "advance",
                principalTable: "engineering_documents",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(ProductionEngineeringSql.Constraints);
            migrationBuilder.Sql(ProductionEngineeringSql.Governance);
            migrationBuilder.Sql(ProductionEngineeringSql.Immutability);
            migrationBuilder.Sql(ProductionEngineeringSql.Grants);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                  IF EXISTS (SELECT 1 FROM advance.production_boms)
                     OR EXISTS (SELECT 1 FROM advance.engineering_documents)
                     OR EXISTS (SELECT 1 FROM advance.production_engineering_history)
                  THEN RAISE EXCEPTION 'Production engineering rollback refused persisted business evidence.';
                  END IF;
                END $$;
                """);
            migrationBuilder.Sql(ProductionEngineeringSql.Down);
            migrationBuilder.DropForeignKey(
                name: "FK_job_orders_production_bom_revisions_CompanyId_PinnedProduct~",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropForeignKey(
                name: "FK_engineering_document_revisions_engineering_documents_Compan~",
                schema: "advance",
                table: "engineering_document_revisions");

            migrationBuilder.DropTable(
                name: "production_bom_lines",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "production_engineering_history",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "production_bom_revisions",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "production_boms",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "engineering_documents",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "engineering_document_revisions",
                schema: "advance");

            migrationBuilder.DropIndex(
                name: "IX_job_orders_CompanyId_PinnedProductionBomRevisionId",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropColumn(
                name: "PinnedProductionBomRevisionId",
                schema: "advance",
                table: "job_orders");
        }
    }
}
