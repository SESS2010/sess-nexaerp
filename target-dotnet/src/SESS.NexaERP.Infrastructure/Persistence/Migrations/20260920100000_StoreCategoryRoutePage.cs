using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>
/// Gives Stores category routes a governed page. The route table and its GRN consumer
/// already exist; only the development-only trial script ever wrote rows, so a real
/// warehouse in a fresh company could not receive goods. Grants mirror the sibling
/// warehouse condition-location page exactly, role for role, so the same people who
/// place racks also route categories to them. No route rows are created.
/// </summary>
[DbContext(typeof(NexaErpDbContext))]
[Migration("20260920100000_StoreCategoryRoutePage")]
public sealed class StoreCategoryRoutePage : Migration
{
    internal const string PageKey = "masters.store-category-routes";
    internal const string SiblingPageKey = "masters.warehouse-condition-locations";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(MigrationText.Lf("""
            DO $guard$ BEGIN
             IF current_setting('server_version_num')::integer<170000 OR current_database() IN('postgres','template0','template1')
             THEN RAISE EXCEPTION 'Stores category route page requires PostgreSQL 17 and an application database.'; END IF;
             IF EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='masters.store-category-routes')
             THEN RAISE EXCEPTION 'Stores category route page is already installed.'; END IF;
             IF NOT EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='masters.warehouse-condition-locations' AND "IsActive")
             THEN RAISE EXCEPTION 'Stores category route page requires the warehouse condition-location page it mirrors.'; END IF;
             IF to_regclass('advance.store_category_routes') IS NULL
             THEN RAISE EXCEPTION 'Stores category route page requires the route table.'; END IF;
            END $guard$;
            INSERT INTO advance.page_definitions("Id","PageKey","Module","Title","Route","IsActive","CreatedAt","CreatedBy","Version")
             VALUES(md5('masters.store-category-routes')::uuid,'masters.store-category-routes','Masters','Stores Category Routes',
              '/masters/store-category-routes',true,now(),'StoreCategoryRoutePage',0);
            INSERT INTO advance.role_page_permissions
            SELECT (jsonb_populate_record(NULL::advance.role_page_permissions,to_jsonb(source)||jsonb_build_object(
             'Id',md5('masters.store-category-routes:'||source."RoleId"::text)::uuid,
             'PageDefinitionId',md5('masters.store-category-routes')::uuid,
             'CreatedAt',now(),'CreatedBy','StoreCategoryRoutePage','UpdatedAt',NULL,'UpdatedBy',NULL,'Version',0))).*
            FROM advance.role_page_permissions source JOIN advance.page_definitions d ON d."Id"=source."PageDefinitionId"
            WHERE d."PageKey"='masters.warehouse-condition-locations';
            """));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(MigrationText.Lf("""
            DO $guard$ BEGIN
             IF NOT EXISTS(SELECT 1 FROM advance.page_definitions WHERE "Id"=md5('masters.store-category-routes')::uuid)
             THEN RAISE EXCEPTION 'Stores category route page is absent.'; END IF;
             IF EXISTS(SELECT 1 FROM advance.role_page_permissions p WHERE p."PageDefinitionId"=md5('masters.store-category-routes')::uuid
              AND (p."UpdatedAt" IS NOT NULL OR p."CreatedBy"<>'StoreCategoryRoutePage'))
             THEN RAISE EXCEPTION 'Stores category route rollback refuses changed or site-added page grants.'; END IF;
            END $guard$;
            DELETE FROM advance.role_page_permissions WHERE "PageDefinitionId"=md5('masters.store-category-routes')::uuid;
            DELETE FROM advance.page_definitions WHERE "Id"=md5('masters.store-category-routes')::uuid;
            """));
    }
}
