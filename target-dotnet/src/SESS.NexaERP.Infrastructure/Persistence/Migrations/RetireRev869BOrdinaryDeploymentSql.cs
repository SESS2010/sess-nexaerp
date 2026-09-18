namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class RetireRev869BOrdinaryDeploymentSql
{
    private static readonly string[] TargetRoles =
    [
        "nexa_rev869b_security_owner", "nexa_rev869b_lifecycle_administrator",
        "nexa_rev869b_app_runtime", "nexa_rev869b_command_audit",
        "nexa_rev869b_management_writer", "nexa_rev869b_purge_worker", "nexa_rev869b_purge_audit",
        "nexa_rev869b_export_service", "nexa_rev869b_target_verifier"
    ];

    internal static string Up => BuildPreflight() + WrapWhenInstalled(
        Rev869BControlledMutationSql.Remove + "\n" +
        Rev869BCommandContextSql.RetirePreservingEvidence + "\n" +
        ReassignRetiredOwnership + "\n" +
        RevokeRetiredRoleAccess) + """

        DROP TRIGGER IF EXISTS trg_rev869a_vendor_qualification_version_guard ON advance.vendor_qualifications;
        REVOKE ALL ON TABLE advance.rev869b_retirement_state FROM PUBLIC;
        DO $owner$
        BEGIN
          IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_owner') THEN
            ALTER TABLE advance.rev869b_retirement_state OWNER TO nexa_erp_owner;
          END IF;
        END $owner$;
        """;

    internal static string Down => WrapWhenInstalled(RestorePreparation + "\n" +
        Rev869BCommandContextSql.RestorePreservedEvidence + "\n" +
        Rev869BControlledMutationSql.Install) + """

        DROP TABLE advance.rev869b_retirement_state;
        """;

    internal static string RepairAlreadyAppliedOwnership => $$"""
        DO $repair$
        DECLARE was_installed boolean; role_count integer; role_name text;
        BEGIN
          IF to_regclass('advance.rev869b_retirement_state') IS NULL THEN
            RAISE EXCEPTION USING ERRCODE='55000',
              MESSAGE='Refusing REV869B ownership repair: retirement state is missing.';
          END IF;
          SELECT "WasInstalled" INTO STRICT was_installed
          FROM advance.rev869b_retirement_state WHERE "Id";
          SELECT count(*) INTO role_count FROM pg_roles WHERE rolname=ANY({{SqlArray(TargetRoles)}});

          IF NOT was_installed AND role_count=0 THEN
            RETURN;
          END IF;
          IF NOT was_installed OR role_count<>{{TargetRoles.Length}} THEN
            RAISE EXCEPTION USING ERRCODE='55000',
              MESSAGE=format('Refusing REV869B ownership repair: partial retired state (was installed %s, roles %s/%s).',
                was_installed,role_count,{{TargetRoles.Length}});
          END IF;
          IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_owner') THEN
            RAISE EXCEPTION USING ERRCODE='55000',
              MESSAGE='Refusing REV869B ownership repair: nexa_erp_owner is missing.';
          END IF;

          FOREACH role_name IN ARRAY {{SqlArray(TargetRoles)}} LOOP
            EXECUTE format('REASSIGN OWNED BY %I TO nexa_erp_owner',role_name);
          END LOOP;
        END $repair$;
        """;

    private static string BuildPreflight()
    {
        var relations = SqlArray(Rev869BCommandContextSql.InstalledRelationNames);
        var functions = SqlArray(Rev869BCommandContextSql.InstalledFunctionNames);
        var triggers = SqlArray(Rev869BCommandContextSql.InstalledTriggerNames);
        var roles = SqlArray(TargetRoles);
        return $$"""
        CREATE TABLE advance.rev869b_retirement_state(
          "Id" boolean PRIMARY KEY DEFAULT true CHECK ("Id"),
          "WasInstalled" boolean NOT NULL,
          "RetiredAt" timestamp with time zone NOT NULL DEFAULT clock_timestamp()
        );
        DO $preflight$
        DECLARE relation_count integer; function_count integer; exclusive_function_count integer;
                trigger_count integer; role_count integer;
                expected_relations integer:={{Rev869BCommandContextSql.InstalledRelationNames.Count}};
                expected_functions integer:={{Rev869BCommandContextSql.InstalledFunctionNames.Count}};
                expected_triggers integer:={{Rev869BCommandContextSql.InstalledTriggerNames.Count}};
        BEGIN
          SELECT count(*) INTO relation_count FROM unnest({{relations}}) x(name)
            WHERE to_regclass('advance.'||x.name) IS NOT NULL;
          SELECT count(*) INTO function_count FROM unnest({{functions}}) x(name)
            WHERE EXISTS (SELECT 1 FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
                          WHERE n.nspname='advance' AND p.proname=x.name);
          SELECT count(*) INTO exclusive_function_count FROM unnest({{functions}}) x(name)
            WHERE x.name <> 'rev869b_register_command_request'
              AND EXISTS (SELECT 1 FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
                          WHERE n.nspname='advance' AND p.proname=x.name);
          SELECT count(*) INTO trigger_count FROM unnest({{triggers}}) x(name)
            WHERE EXISTS (SELECT 1 FROM pg_trigger t WHERE t.tgname=x.name AND NOT t.tgisinternal);
          SELECT count(*) INTO role_count FROM pg_roles WHERE rolname=ANY({{roles}});
          -- RevisedEmployeeRoleGovernancePhase2 installed the register overload
          -- before the optional security package existed. It is therefore not
          -- evidence that the package is partially installed; every other
          -- function in this catalogue is package-exclusive.
          IF relation_count=0 AND exclusive_function_count=0
                AND trigger_count=0 AND role_count=0 THEN
            INSERT INTO advance.rev869b_retirement_state("Id","WasInstalled") VALUES(true,false);
          ELSIF relation_count=expected_relations AND function_count=expected_functions
                AND trigger_count=expected_triggers AND role_count={{TargetRoles.Length}} THEN
            INSERT INTO advance.rev869b_retirement_state("Id","WasInstalled") VALUES(true,true);
          ELSE
            RAISE EXCEPTION USING ERRCODE='55000',
              MESSAGE=format('Refusing REV869B retirement: partial installation (relations %s/%s, functions %s/%s, triggers %s/%s, roles %s/%s).',
                relation_count,expected_relations,function_count,expected_functions,trigger_count,expected_triggers,role_count,{{TargetRoles.Length}});
          END IF;
        END $preflight$;
        """;
    }

    private static string WrapWhenInstalled(string sql) => $$"""
        DO $retire$
        BEGIN
          IF (SELECT "WasInstalled" FROM advance.rev869b_retirement_state WHERE "Id") THEN
            EXECUTE $retired_sql$
        {{sql}}
            $retired_sql$;
          END IF;
        END $retire$;
        """;

    private static string SqlArray(IEnumerable<string> values) =>
        "ARRAY[" + string.Join(',', values.Select(x => "'" + x.Replace("'", "''", StringComparison.Ordinal) + "'")) + "]::text[]";

    private static readonly string RevokeRetiredRoleAccess =
        "REVOKE USAGE ON SCHEMA advance FROM " + string.Join(',', TargetRoles) + ";" + "\n" +
        "REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA advance FROM " + string.Join(',', TargetRoles) + ";" + "\n" +
        "REVOKE ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA advance FROM " + string.Join(',', TargetRoles) + ";" + "\n" +
        "REVOKE ALL PRIVILEGES ON ALL FUNCTIONS IN SCHEMA advance FROM " + string.Join(',', TargetRoles) + ";";

    private static readonly string ReassignRetiredOwnership =
        "DO $ownership_target$ BEGIN " +
        "IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_owner') THEN " +
        "RAISE EXCEPTION USING ERRCODE='55000',MESSAGE='Refusing REV869B retirement: nexa_erp_owner is missing.'; " +
        "END IF; END $ownership_target$;" + "\n" +
        string.Join("\n", TargetRoles.Select(role =>
            $"REASSIGN OWNED BY {role} TO nexa_erp_owner;"));

    private const string RestorePreparation = """
        DROP TRIGGER IF EXISTS "TR_rev869b_command_outcomes_immutable" ON advance.rev869b_command_attempt_outcomes;
        DROP TRIGGER IF EXISTS "TR_rev869b_command_receipts_immutable" ON advance.rev869b_command_receipts;
        DROP TRIGGER IF EXISTS "TR_rev869b_target_instance_identity_immutable" ON advance.rev869b_target_instance_identity;
        DROP TRIGGER IF EXISTS "TR_rev869b_purge_authorizations_no_delete" ON advance.rev869b_purge_authorizations;
        DROP TRIGGER IF EXISTS "TR_rev869b_purge_attempts_no_delete" ON advance.rev869b_purge_attempts;
        DROP TRIGGER IF EXISTS "TR_rev869b_purge_events_immutable" ON advance.rev869b_purge_events;
        DROP TRIGGER IF EXISTS "TR_rev869b_export_rows_immutable" ON advance.rev869b_export_batch_rows;
        DROP FUNCTION IF EXISTS advance.rev869b_deny_ledger_mutation();
        """;
}
