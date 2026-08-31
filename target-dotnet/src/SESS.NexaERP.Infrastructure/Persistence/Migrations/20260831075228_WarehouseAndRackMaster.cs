using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WarehouseAndRackMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.DropIndex(
                name: "IX_warehouses_WarehouseCode",
                schema: "advance",
                table: "warehouses");

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_CompanyId_WarehouseCode",
                schema: "advance",
                table: "warehouses",
                columns: new[] { "CompanyId", "WarehouseCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rack_bins_CompanyId_BinCode",
                schema: "advance",
                table: "rack_bins",
                columns: new[] { "CompanyId", "BinCode" },
                unique: true);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION advance.guard_warehouse_rack_deactivation()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $guard$
                DECLARE
                    current_balance numeric(18,3);
                BEGIN
                    IF OLD."IsActive" AND NOT NEW."IsActive" THEN
                        IF TG_TABLE_NAME = 'warehouses' THEN
                            SELECT COALESCE(SUM(sm."QuantityIn" - sm."QuantityOut"), 0)
                              INTO current_balance
                              FROM advance.stock_movements sm
                             WHERE sm."CompanyId" = OLD."CompanyId"
                               AND sm."WarehouseId" = OLD."Id";
                            IF current_balance <> 0 THEN
                                RAISE EXCEPTION 'Warehouse cannot be deactivated while its current stock balance is %. Transfer or issue the stock first.', current_balance USING ERRCODE = '23514';
                            END IF;
                            IF EXISTS (
                                SELECT 1 FROM advance.rack_bins rb
                                 WHERE rb."CompanyId" = OLD."CompanyId"
                                   AND rb."WarehouseId" = OLD."Id"
                                   AND rb."IsActive"
                            ) THEN
                                RAISE EXCEPTION 'Warehouse cannot be deactivated while it has active rack/bins.' USING ERRCODE = '23514';
                            END IF;
                        ELSE
                            SELECT COALESCE(SUM(sm."QuantityIn" - sm."QuantityOut"), 0)
                              INTO current_balance
                              FROM advance.stock_movements sm
                             WHERE sm."CompanyId" = OLD."CompanyId"
                               AND sm."RackBinId" = OLD."Id";
                            IF current_balance <> 0 THEN
                                RAISE EXCEPTION 'Rack/bin cannot be deactivated while its current stock balance is %. Transfer or issue the stock first.', current_balance USING ERRCODE = '23514';
                            END IF;
                            IF EXISTS (
                                SELECT 1 FROM advance.warehouse_condition_locations wcl
                                 WHERE wcl."CompanyId" = OLD."CompanyId"
                                   AND wcl."RackBinId" = OLD."Id"
                                   AND wcl."IsActive"
                                   AND wcl."EffectiveTo" IS NULL
                            ) THEN
                                RAISE EXCEPTION 'Rack/bin cannot be deactivated while an open condition-location version uses it.' USING ERRCODE = '23514';
                            END IF;
                        END IF;
                    END IF;
                    RETURN NEW;
                END;
                $guard$;

                CREATE TRIGGER trg_warehouses_guard_deactivation
                BEFORE UPDATE OF "IsActive" ON advance.warehouses
                FOR EACH ROW EXECUTE FUNCTION advance.guard_warehouse_rack_deactivation();

                CREATE TRIGGER trg_rack_bins_guard_deactivation
                BEFORE UPDATE OF "IsActive" ON advance.rack_bins
                FOR EACH ROW EXECUTE FUNCTION advance.guard_warehouse_rack_deactivation();

                CREATE OR REPLACE FUNCTION advance.guard_condition_location_close_stock()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $guard$
                DECLARE
                    current_balance numeric(18,3);
                BEGIN
                    IF OLD."EffectiveTo" IS NULL AND NEW."EffectiveTo" IS NOT NULL THEN
                        SELECT COALESCE(SUM(sm."QuantityIn" - sm."QuantityOut"), 0)
                          INTO current_balance
                          FROM advance.stock_movements sm
                         WHERE sm."CompanyId" = OLD."CompanyId"
                           AND sm."WarehouseConditionLocationId" = OLD."Id";
                        IF current_balance <> 0 THEN
                            RAISE EXCEPTION 'Condition location cannot be closed while its current stock balance is %. Transfer or issue the stock first.', current_balance USING ERRCODE = '23514';
                        END IF;
                    END IF;
                    RETURN NEW;
                END;
                $guard$;

                CREATE TRIGGER trg_warehouse_condition_locations_guard_close_stock
                BEFORE UPDATE OF "EffectiveTo" ON advance.warehouse_condition_locations
                FOR EACH ROW EXECUTE FUNCTION advance.guard_condition_location_close_stock();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);

            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS trg_warehouse_condition_locations_guard_close_stock ON advance.warehouse_condition_locations;
                DROP FUNCTION IF EXISTS advance.guard_condition_location_close_stock();
                DROP TRIGGER IF EXISTS trg_rack_bins_guard_deactivation ON advance.rack_bins;
                DROP TRIGGER IF EXISTS trg_warehouses_guard_deactivation ON advance.warehouses;
                DROP FUNCTION IF EXISTS advance.guard_warehouse_rack_deactivation();
                """);
            migrationBuilder.DropIndex(
                name: "IX_warehouses_CompanyId_WarehouseCode",
                schema: "advance",
                table: "warehouses");

            migrationBuilder.DropIndex(
                name: "IX_rack_bins_CompanyId_BinCode",
                schema: "advance",
                table: "rack_bins");

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_WarehouseCode",
                schema: "advance",
                table: "warehouses",
                column: "WarehouseCode",
                unique: true);
        }
    }
}
