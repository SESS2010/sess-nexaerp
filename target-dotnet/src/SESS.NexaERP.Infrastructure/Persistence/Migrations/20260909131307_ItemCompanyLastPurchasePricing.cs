using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ItemCompanyLastPurchasePricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.CreateTable(
                name: "item_company_last_purchases",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastPurchaseRate = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: true),
                    LastPurchaseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    LastPurchaseBillId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_company_last_purchases", x => x.Id);
                    table.UniqueConstraint("AK_item_company_last_purchases_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_item_company_last_purchase_complete", "num_nonnulls(\"LastPurchaseRate\",\"LastPurchaseDate\",\"LastPurchaseBillId\") IN (0,3) AND (\"LastPurchaseRate\" IS NULL OR \"LastPurchaseRate\">=0)");
                    table.ForeignKey(
                        name: "FK_item_company_last_purchases_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_company_last_purchases_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "advance",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_company_last_purchases_vendor_bills_CompanyId_LastPurc~",
                        columns: x => new { x.CompanyId, x.LastPurchaseBillId },
                        principalSchema: "advance",
                        principalTable: "vendor_bills",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_item_company_last_purchases_CompanyId",
                schema: "advance",
                table: "item_company_last_purchases",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_item_company_last_purchases_CompanyId_ItemId",
                schema: "advance",
                table: "item_company_last_purchases",
                columns: new[] { "CompanyId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_item_company_last_purchases_CompanyId_LastPurchaseBillId",
                schema: "advance",
                table: "item_company_last_purchases",
                columns: new[] { "CompanyId", "LastPurchaseBillId" });

            migrationBuilder.CreateIndex(
                name: "IX_item_company_last_purchases_ItemId",
                schema: "advance",
                table: "item_company_last_purchases",
                column: "ItemId");
            migrationBuilder.Sql(ItemCompanyLastPurchasePricingSql.Up);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(ItemCompanyLastPurchasePricingSql.Down);
            migrationBuilder.DropTable(
                name: "item_company_last_purchases",
                schema: "advance");
        }
    }
}
