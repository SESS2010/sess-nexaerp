using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20261003113000_MirRollbackJournalProtection")]
public sealed class MirRollbackJournalProtection : Migration
{
    private const string ProtectJournal = """
        DO $protect$
        DECLARE journal regclass; grant_entry record;
        BEGIN
          journal:=to_regclass('advance.r1_mir_pending_scope_backup');
          IF journal IS NULL THEN RAISE EXCEPTION 'MIR rollback journal protection requires migration 147.'; END IF;
          LOCK TABLE advance.r1_mir_pending_scope_backup IN ACCESS EXCLUSIVE MODE;
          IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_owner') THEN
            ALTER TABLE advance.r1_mir_pending_scope_backup OWNER TO nexa_erp_owner;
          END IF;
          FOR grant_entry IN SELECT DISTINCT acl.grantee FROM pg_class c
            CROSS JOIN LATERAL aclexplode(coalesce(c.relacl,acldefault('r',c.relowner))) acl
            WHERE c.oid=journal AND acl.grantee<>c.relowner LOOP
            IF grant_entry.grantee=0 THEN
              REVOKE ALL ON advance.r1_mir_pending_scope_backup FROM PUBLIC;
            ELSE
              EXECUTE format('REVOKE ALL ON advance.r1_mir_pending_scope_backup FROM %I',pg_get_userbyid(grant_entry.grantee));
            END IF;
          END LOOP;
        END $protect$;
        """;

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(ProtectJournal);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        // Keep the journal, every backup row and owner-only protection; never reopen unsafe ACLs.
        migrationBuilder.Sql(ProtectJournal);
    }
}
