using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MasterDataImportFramework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.CreateTable(
                name: "master_import_batches",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MasterKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    TemplateVersion = table.Column<int>(type: "integer", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportMode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    FileSha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RequestFingerprint = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    UploadedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedByEmployeeCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OperationalRoleCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RetentionExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP + INTERVAL '90 days'"),
                    SensitiveValuesPurgedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TotalRows = table.Column<int>(type: "integer", nullable: false),
                    ValidRows = table.Column<int>(type: "integer", nullable: false),
                    InvalidRows = table.Column<int>(type: "integer", nullable: false),
                    CreatedRows = table.Column<int>(type: "integer", nullable: false),
                    UpdatedRows = table.Column<int>(type: "integer", nullable: false),
                    UnchangedRows = table.Column<int>(type: "integer", nullable: false),
                    RejectedRows = table.Column<int>(type: "integer", nullable: false),
                    NotImportedRows = table.Column<int>(type: "integer", nullable: false),
                    FailureSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_master_import_batches", x => x.Id);
                    table.CheckConstraint("CK_master_import_batch_completed", "\"CompletedAt\" IS NULL OR \"CompletedAt\" >= \"UploadedAt\"");
                    table.CheckConstraint("CK_master_import_batch_counts", "\"TotalRows\" >= 0 AND \"ValidRows\" >= 0 AND \"InvalidRows\" >= 0 AND \"CreatedRows\" >= 0 AND \"UpdatedRows\" >= 0 AND \"UnchangedRows\" >= 0 AND \"RejectedRows\" >= 0 AND \"NotImportedRows\" >= 0");
                    table.CheckConstraint("CK_master_import_batch_file_size", "\"FileSizeBytes\" >= 0");
                    table.CheckConstraint("CK_master_import_batch_mode", "\"ImportMode\" IN ('IMPORT_VALID_ROWS','REJECT_ENTIRE_FILE')");
                    table.CheckConstraint("CK_master_import_batch_retention", "\"RetentionExpiresAt\" >= \"UploadedAt\"");
                    table.CheckConstraint("CK_master_import_batch_status", "\"Status\" IN ('PROCESSING','COMPLETED','COMPLETED_WITH_ERRORS','REJECTED','FAILED')");
                    table.ForeignKey(
                        name: "FK_master_import_batches_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_master_import_batches_employees_UploadedByEmployeeId",
                        column: x => x.UploadedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "master_import_row_results",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceRowNumber = table.Column<int>(type: "integer", nullable: false),
                    BusinessCode = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    NormalizedBusinessCode = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    IntendedAction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SubmittedValuesJson = table.Column<string>(type: "jsonb", nullable: true),
                    ErrorsJson = table.Column<string>(type: "jsonb", nullable: false),
                    ResultRecordId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResultVersion = table.Column<long>(type: "bigint", nullable: true),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_master_import_row_results", x => x.Id);
                    table.CheckConstraint("CK_master_import_row_action", "\"IntendedAction\" IS NULL OR \"IntendedAction\" IN ('CREATE','UPDATE','NO_CHANGE')");
                    table.CheckConstraint("CK_master_import_row_errors_json", "jsonb_typeof(\"ErrorsJson\") = 'array'");
                    table.CheckConstraint("CK_master_import_row_outcome", "\"Outcome\" IN ('CREATED','UPDATED','UNCHANGED','REJECTED','NOT_IMPORTED')");
                    table.CheckConstraint("CK_master_import_row_rejected_error", "\"Outcome\" <> 'REJECTED' OR jsonb_array_length(\"ErrorsJson\") > 0");
                    table.CheckConstraint("CK_master_import_row_source", "\"SourceRowNumber\" >= 2");
                    table.CheckConstraint("CK_master_import_row_success_result", "\"Outcome\" NOT IN ('CREATED','UPDATED','UNCHANGED') OR \"ResultRecordId\" IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_master_import_row_results_master_import_batches_ImportBatch~",
                        column: x => x.ImportBatchId,
                        principalSchema: "advance",
                        principalTable: "master_import_batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_master_import_batches_CompanyId_MasterKey_IdempotencyKey",
                schema: "advance",
                table: "master_import_batches",
                columns: new[] { "CompanyId", "MasterKey", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_master_import_batches_CompanyId_MasterKey_UploadedAt",
                schema: "advance",
                table: "master_import_batches",
                columns: new[] { "CompanyId", "MasterKey", "UploadedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_master_import_batches_CorrelationId",
                schema: "advance",
                table: "master_import_batches",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_master_import_batches_FileSha256",
                schema: "advance",
                table: "master_import_batches",
                column: "FileSha256");

            migrationBuilder.CreateIndex(
                name: "IX_master_import_batches_RetentionExpiresAt_SensitiveValuesPur~",
                schema: "advance",
                table: "master_import_batches",
                columns: new[] { "RetentionExpiresAt", "SensitiveValuesPurgedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_master_import_batches_UploadedByEmployeeId_UploadedAt",
                schema: "advance",
                table: "master_import_batches",
                columns: new[] { "UploadedByEmployeeId", "UploadedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_master_import_row_results_ImportBatchId_NormalizedBusinessC~",
                schema: "advance",
                table: "master_import_row_results",
                columns: new[] { "ImportBatchId", "NormalizedBusinessCode" });

            migrationBuilder.CreateIndex(
                name: "IX_master_import_row_results_ImportBatchId_Outcome_SourceRowNu~",
                schema: "advance",
                table: "master_import_row_results",
                columns: new[] { "ImportBatchId", "Outcome", "SourceRowNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_master_import_row_results_ImportBatchId_SourceRowNumber",
                schema: "advance",
                table: "master_import_row_results",
                columns: new[] { "ImportBatchId", "SourceRowNumber" },
                unique: true);

            migrationBuilder.Sql(MasterDataImportFrameworkSql.Up);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(MasterDataImportFrameworkSql.Down);

            migrationBuilder.DropTable(
                name: "master_import_row_results",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "master_import_batches",
                schema: "advance");
        }
    }
}
