using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260913060000_CommandReceiptReplay")]
public sealed class CommandReceiptReplay : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(CommandReceiptReplaySql.Guard(false));
        migrationBuilder.Sql(CommandReceiptReplaySql.Install);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(CommandReceiptReplaySql.Guard(true));
        migrationBuilder.Sql("DROP FUNCTION advance.read_command_receipt(uuid);");
    }
}

internal static class CommandReceiptReplaySql
{
    private const string Body = """
        DECLARE request advance.command_requests%ROWTYPE; response jsonb;
        BEGIN
          IF session_user<>'nexa_erp_runtime'
             OR pg_has_role(session_user,'nexa_erp_owner','MEMBER')
             OR NOT EXISTS(SELECT 1 FROM pg_roles WHERE rolname=session_user
               AND rolcanlogin AND NOT rolsuper AND NOT rolcreatedb AND NOT rolcreaterole
               AND NOT rolreplication AND NOT rolbypassrls)
             OR command_id IS NULL
             OR command_id IS DISTINCT FROM nullif(current_setting('advance.ordinary_command_id',true),'')::uuid THEN
            RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Receipt replay requires the exact registered runtime command.';
          END IF;
          SELECT * INTO request FROM advance.command_requests r WHERE r."CommandId"=command_id;
          IF NOT FOUND
             OR request."OrganizationId" IS DISTINCT FROM current_setting('advance.ordinary_organization',true)
             OR request."ActorEmployeeId"::text IS DISTINCT FROM current_setting('advance.ordinary_actor_employee_id',true)
             OR request."IdentityIssuer" IS DISTINCT FROM current_setting('advance.ordinary_identity_issuer',true)
             OR request."IdentitySubject" IS DISTINCT FROM current_setting('advance.ordinary_identity_subject',true)
             OR request."ActorRoleCode" IS DISTINCT FROM current_setting('advance.ordinary_actor_role',true)
             OR NOT EXISTS(SELECT 1 FROM advance.companies c
               CROSS JOIN LATERAL advance.resolve_employee_role_authority(request."ActorEmployeeId",
                 c."Id",CURRENT_DATE,request."Operation",ARRAY[request."ActorRoleCode"]) a
               WHERE c."Code"=request."OrganizationId" AND c."IsActive" AND c."Status"='ACTIVE'
                 AND a."AssignmentId"=request."ResolvedRoleAssignmentId"
                 AND a."RoleCode"=request."ActorRoleCode") THEN
            RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Receipt replay actor authority does not match the registered request.';
          END IF;
          SELECT r."ResponseJson" INTO response FROM advance.command_receipts r WHERE r."CommandId"=command_id;
          RETURN response;
        END;
        """;

    internal static string Install => """
        CREATE FUNCTION advance.read_command_receipt(command_id uuid)
        RETURNS jsonb LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
        """ + Body + """
        $function$;
        REVOKE ALL ON FUNCTION advance.read_command_receipt(uuid) FROM PUBLIC;
        DO $owner$
        BEGIN
          IF to_regrole('nexa_erp_owner') IS NOT NULL THEN
            ALTER FUNCTION advance.read_command_receipt(uuid) OWNER TO nexa_erp_owner;
            REVOKE ALL ON FUNCTION advance.read_command_receipt(uuid) FROM nexa_erp_bootstrap,nexa_erp_migration;
            GRANT EXECUTE ON FUNCTION advance.read_command_receipt(uuid) TO nexa_erp_runtime;
          END IF;
        END $owner$;
        """;

    internal static string Guard(bool installed)
    {
        var specific = installed ? $$"""
            IF NOT EXISTS(SELECT 1 FROM pg_proc p
              WHERE p.oid=to_regprocedure('advance.read_command_receipt(uuid)')
                AND p.prosecdef AND p.prorettype='jsonb'::regtype
                AND p.proowner=(SELECT relowner FROM pg_class WHERE oid='advance.command_receipts'::regclass)
                AND replace(p.prosrc,E'\r\n',E'\n')='{{Body.Replace("\r\n", "\n").Replace("'", "''")}}'
                AND array_length(p.proconfig,1)=1
                AND EXISTS(SELECT 1 FROM unnest(p.proconfig) setting
                  WHERE regexp_replace(setting,'[[:space:]]','','g')='search_path=pg_catalog,advance')
                AND NOT EXISTS(SELECT 1 FROM aclexplode(coalesce(p.proacl,acldefault('f',p.proowner))) a
                  WHERE a.grantee<>p.proowner AND a.grantee<>coalesce(to_regrole('nexa_erp_runtime')::oid,0)
                    AND a.privilege_type='EXECUTE')) THEN
              RAISE EXCEPTION 'Receipt replay rollback refuses a changed function.';
            END IF;
            """ : """
            IF to_regprocedure('advance.read_command_receipt(uuid)') IS NOT NULL THEN
              RAISE EXCEPTION 'Receipt replay installation refuses an existing function.';
            END IF;
            """;
        return """
            DO $guard$
            BEGIN
              IF current_setting('server_version_num')::integer<170000
                 OR current_database() IN ('postgres','template0','template1') THEN
                RAISE EXCEPTION 'Receipt replay migration refuses this cluster or protected database.';
              END IF;
              IF to_regclass('advance.command_requests') IS NULL
                 OR to_regclass('advance.command_receipts') IS NULL
                 OR to_regprocedure('advance.register_command_request(text,text,bytea,bytea,uuid,text,text,text,uuid)') IS NULL THEN
                RAISE EXCEPTION 'Receipt replay requires the complete ordinary command ledger.';
              END IF;
            """ + specific + """
            END $guard$;
            """;
    }
}
