using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustomerPurchaseOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_purchase_orders",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PoRecordNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CustomerPoNumber = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CustomerPoDate = table.Column<DateOnly>(type: "date", nullable: true),
                    QuoteNumber = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    QuoteDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServiceMode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    SalesType = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    TotalAmountWithGst = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    WorkStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    FinalInvoiceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PaymentStatus = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    PaymentTerms = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ModeOfDelivery = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FiscalYear = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_purchase_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_customer_purchase_orders_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_purchase_orders_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "advance",
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "page_definitions",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "IsActive", "Module", "PageKey", "Route", "Title", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[] { new Guid("44000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-sales-customer-po", true, "Sales", "sales.customer-po", "/sales/customer-po", "Customer PO", null, null, 0L });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "role_page_permissions",
                columns: new[] { "Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("44000000-0000-0000-0001-000000000001"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-sales-customer-po", true, new Guid("44000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("44000000-0000-0000-0001-000000000002"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-sales-customer-po", true, new Guid("44000000-0000-0000-0000-000000000001"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("44000000-0000-0000-0001-000000000003"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-sales-customer-po", true, new Guid("44000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("44000000-0000-0000-0001-000000000004"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-sales-customer-po", true, new Guid("44000000-0000-0000-0000-000000000001"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("44000000-0000-0000-0001-000000000005"), false, true, true, false, true, true, true, false, false, false, false, true, true, true, true, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-sales-customer-po", false, new Guid("44000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("44000000-0000-0000-0001-000000000006"), false, true, true, false, true, false, true, false, false, false, false, true, true, true, true, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-sales-customer-po", false, new Guid("44000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("44000000-0000-0000-0001-000000000007"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-sales-customer-po", false, new Guid("44000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("44000000-0000-0000-0001-000000000008"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-sales-customer-po", false, new Guid("44000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_customer_purchase_orders_CompanyId",
                schema: "advance",
                table: "customer_purchase_orders",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_customer_purchase_orders_CustomerId",
                schema: "advance",
                table: "customer_purchase_orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_customer_purchase_orders_CustomerPoNumber",
                schema: "advance",
                table: "customer_purchase_orders",
                column: "CustomerPoNumber");

            migrationBuilder.CreateIndex(
                name: "IX_customer_purchase_orders_FiscalYear",
                schema: "advance",
                table: "customer_purchase_orders",
                column: "FiscalYear");

            migrationBuilder.CreateIndex(
                name: "IX_customer_purchase_orders_PoRecordNumber",
                schema: "advance",
                table: "customer_purchase_orders",
                column: "PoRecordNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_purchase_orders_WorkStatus",
                schema: "advance",
                table: "customer_purchase_orders",
                column: "WorkStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_purchase_orders",
                schema: "advance");

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("44000000-0000-0000-0001-000000000001"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("44000000-0000-0000-0001-000000000002"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("44000000-0000-0000-0001-000000000003"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("44000000-0000-0000-0001-000000000004"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("44000000-0000-0000-0001-000000000005"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("44000000-0000-0000-0001-000000000006"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("44000000-0000-0000-0001-000000000007"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("44000000-0000-0000-0001-000000000008"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "page_definitions",
                keyColumn: "Id",
                keyValue: new Guid("44000000-0000-0000-0000-000000000001"));
        }
    }
}
