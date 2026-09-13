using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260913050000_ConcessionSerialDecisionHistory")]
public sealed class ConcessionSerialDecisionHistory : Migration
{
    private const string IndexName = "IX_inventory_concession_allocation_serials_CompanyId_Inventory~";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(ConcessionSerialDecisionHistorySql.Guard(true));
        migrationBuilder.DropIndex(IndexName, "inventory_concession_allocation_serials", "advance");
        migrationBuilder.CreateIndex(IndexName, "inventory_concession_allocation_serials",
            new[] { "CompanyId", "InventorySerialId" }, "advance");
        migrationBuilder.Sql(ConcessionSerialDecisionHistorySql.Install);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(ConcessionSerialDecisionHistorySql.Guard(false));
        migrationBuilder.Sql("""
            DROP TRIGGER "TR_concession_serial_active_allocation" ON advance.inventory_concession_allocation_serials;
            DROP FUNCTION advance.guard_active_concession_serial();
            """);
        migrationBuilder.DropIndex(IndexName, "inventory_concession_allocation_serials", "advance");
        migrationBuilder.CreateIndex(IndexName, "inventory_concession_allocation_serials",
            new[] { "CompanyId", "InventorySerialId" }, "advance", unique: true);
    }
}

internal static class ConcessionSerialDecisionHistorySql
{
    private const string Body = """
        DECLARE allocation_id uuid; parent_status text;
        BEGIN
          SELECT a."GoodsReceiptLineLotAllocationId",c."Status" INTO allocation_id,parent_status
          FROM advance.inventory_concession_allocations a
          JOIN advance.inventory_concessions c ON c."CompanyId"=a."CompanyId" AND c."Id"=a."InventoryConcessionId"
          WHERE a."CompanyId"=NEW."CompanyId" AND a."Id"=NEW."InventoryConcessionAllocationId";
          IF allocation_id IS NULL OR parent_status<>'DRAFT' THEN
            RAISE EXCEPTION 'Concession serials require a draft concession allocation.';
          END IF;
          PERFORM pg_advisory_xact_lock(hashtextextended(
            'CONCESSION:ACTIVE-SERIAL:'||NEW."CompanyId"||':'||allocation_id,0));
          IF EXISTS(
            SELECT 1 FROM advance.inventory_concession_allocation_serials s
            JOIN advance.inventory_concession_allocations a
              ON a."CompanyId"=s."CompanyId" AND a."Id"=s."InventoryConcessionAllocationId"
            JOIN advance.inventory_concessions c
              ON c."CompanyId"=a."CompanyId" AND c."Id"=a."InventoryConcessionId"
            WHERE s."CompanyId"=NEW."CompanyId" AND s."InventorySerialId"=NEW."InventorySerialId"
              AND s."Id"<>NEW."Id" AND a."GoodsReceiptLineLotAllocationId"=allocation_id
              AND c."Status" IN ('DRAFT','APPROVED')
              AND NOT EXISTS(SELECT 1 FROM advance.inventory_concessions r
                WHERE r."CompanyId"=c."CompanyId" AND r."ReversesConcessionId"=c."Id" AND r."Status"='REVERSED')) THEN
            RAISE EXCEPTION USING ERRCODE='23505',
              CONSTRAINT='UX_concession_active_serial_allocation',
              MESSAGE='The serial already belongs to an active concession for this receipt allocation.';
          END IF;
          RETURN NEW;
        END;
        """;

    internal static string Install => """
        CREATE FUNCTION advance.guard_active_concession_serial()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $function$
        """ + Body + """
        $function$;
        REVOKE ALL ON FUNCTION advance.guard_active_concession_serial() FROM PUBLIC;
        CREATE TRIGGER "TR_concession_serial_active_allocation"
          BEFORE INSERT OR UPDATE OF "CompanyId","InventoryConcessionAllocationId","InventorySerialId"
          ON advance.inventory_concession_allocation_serials
          FOR EACH ROW EXECUTE FUNCTION advance.guard_active_concession_serial();
        DO $owner$
        BEGIN
          IF to_regrole('nexa_erp_owner') IS NOT NULL THEN
            ALTER FUNCTION advance.guard_active_concession_serial() OWNER TO nexa_erp_owner;
          END IF;
        END $owner$;
        """;

    internal static string Guard(bool expectUnique)
    {
        var specific = expectUnique ? """
            IF to_regprocedure('advance.guard_active_concession_serial()') IS NOT NULL
               OR EXISTS(SELECT 1 FROM pg_trigger WHERE tgrelid=target
                 AND tgname='TR_concession_serial_active_allocation') THEN
              RAISE EXCEPTION 'Concession history migration refuses an existing active-serial guard.';
            END IF;
            """ : $$"""
            IF EXISTS(SELECT 1 FROM advance.inventory_concession_allocation_serials
              GROUP BY "CompanyId","InventorySerialId" HAVING count(*)>1) THEN
              RAISE EXCEPTION 'Concession serial history prevents rollback; historical records must not be deleted.';
            END IF;
            IF NOT EXISTS(
              SELECT 1 FROM pg_proc p
              WHERE p.oid=to_regprocedure('advance.guard_active_concession_serial()') AND NOT p.prosecdef
                AND p.proowner=(SELECT relowner FROM pg_class WHERE oid=target)
                AND replace(p.prosrc,E'\r\n',E'\n')='{{Body.Replace("\r\n", "\n").Replace("'", "''")}}'
                AND array_length(p.proconfig,1)=1
                AND EXISTS(SELECT 1 FROM unnest(p.proconfig) AS settings(setting)
                  WHERE regexp_replace(setting,'[[:space:]]','','g')='search_path=pg_catalog,advance')
                AND NOT EXISTS(SELECT 1 FROM aclexplode(coalesce(p.proacl,acldefault('f',p.proowner))) a
                  WHERE a.grantee<>p.proowner AND a.privilege_type='EXECUTE')) THEN
              RAISE EXCEPTION 'Concession history rollback refuses a changed active-serial function.';
            END IF;
            IF NOT EXISTS(SELECT 1 FROM pg_trigger WHERE tgrelid=target
              AND tgname='TR_concession_serial_active_allocation'
              AND tgfoid=to_regprocedure('advance.guard_active_concession_serial()')
              AND tgtype=23 AND tgenabled='O' AND tgqual IS NULL AND tgnargs=0) THEN
              RAISE EXCEPTION 'Concession history rollback refuses a changed active-serial trigger.';
            END IF;
            """;
        return $$"""
            DO $guard$
            DECLARE target regclass:=to_regclass('advance.inventory_concession_allocation_serials');
                    lookup_index regclass:=to_regclass('advance."IX_inventory_concession_allocation_serials_CompanyId_Inventory~"');
            BEGIN
              IF current_setting('server_version_num')::integer<170000
                 OR current_database() IN ('postgres','template0','template1') THEN
                RAISE EXCEPTION 'Concession history migration refuses this cluster or administrative database.';
              END IF;
              IF target IS NULL OR lookup_index IS NULL THEN
                RAISE EXCEPTION 'Concession history migration requires its existing serial table and index.';
              END IF;
              EXECUTE 'LOCK TABLE advance.inventory_concession_allocation_serials IN ACCESS EXCLUSIVE MODE';
              IF NOT EXISTS(SELECT 1 FROM pg_index i WHERE i.indexrelid=lookup_index
                AND i.indrelid=target AND i.indisvalid AND i.indisready
                AND i.indisunique={{(expectUnique ? "true" : "false")}}
                AND i.indnkeyatts=2 AND i.indnatts=2 AND i.indpred IS NULL AND i.indexprs IS NULL
                AND (SELECT array_agg(a.attname ORDER BY k.ordinality)
                  FROM unnest(i.indkey::smallint[]) WITH ORDINALITY k(attnum,ordinality)
                  JOIN pg_attribute a ON a.attrelid=target AND a.attnum=k.attnum)
                  =ARRAY['CompanyId','InventorySerialId']::name[]) THEN
                RAISE EXCEPTION 'Concession history migration refuses a changed serial lookup index.';
              END IF;
              IF NOT EXISTS(SELECT 1 FROM pg_index i WHERE i.indrelid=target AND i.indisunique
                AND i.indisvalid AND i.indnkeyatts=3 AND i.indnatts=3
                AND i.indpred IS NULL AND i.indexprs IS NULL
                AND (SELECT array_agg(a.attname ORDER BY k.ordinality)
                  FROM unnest(i.indkey::smallint[]) WITH ORDINALITY k(attnum,ordinality)
                  JOIN pg_attribute a ON a.attrelid=target AND a.attnum=k.attnum)
                  =ARRAY['CompanyId','InventoryConcessionAllocationId','InventorySerialId']::name[]) THEN
                RAISE EXCEPTION 'Concession history migration requires uniqueness within each allocation.';
              END IF;
              {{specific}}
            END $guard$;
            """;
    }
}
