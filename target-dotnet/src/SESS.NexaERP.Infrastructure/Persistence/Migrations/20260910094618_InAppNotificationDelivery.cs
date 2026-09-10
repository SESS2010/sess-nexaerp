using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    public partial class InAppNotificationDelivery : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql("""
                DO $guard$
                BEGIN
                  IF to_regclass('advance.notification_events') IS NULL
                     OR to_regclass('advance.notification_recipients') IS NULL
                     OR to_regclass('advance.notification_delivery_attempts') IS NULL
                     OR to_regprocedure('advance.stores_p1_actor_has_role(uuid,uuid,text,date)') IS NULL THEN
                    RAISE EXCEPTION 'In-app notification foundation is partially installed.';
                  END IF;
                END $guard$;
                REVOKE ALL ON FUNCTION advance.stores_p1_actor_has_role(uuid,uuid,text,date) FROM PUBLIC;
                DO $acl$
                BEGIN
                  IF EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='nexa_erp_runtime') THEN
                    GRANT EXECUTE ON FUNCTION advance.stores_p1_actor_has_role(uuid,uuid,text,date) TO nexa_erp_runtime;
                  END IF;
                END $acl$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql("""
                DO $acl$
                BEGIN
                  IF EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='nexa_erp_runtime')
                     AND to_regprocedure('advance.stores_p1_actor_has_role(uuid,uuid,text,date)') IS NOT NULL THEN
                    REVOKE EXECUTE ON FUNCTION advance.stores_p1_actor_has_role(uuid,uuid,text,date) FROM nexa_erp_runtime;
                  END IF;
                END $acl$;
                """);
        }
    }
}
