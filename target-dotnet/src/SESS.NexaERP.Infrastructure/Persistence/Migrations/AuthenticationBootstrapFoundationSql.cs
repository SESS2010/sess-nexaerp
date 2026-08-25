namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class AuthenticationBootstrapFoundationSql
{
    internal static string PreUp => AdvanceSchemaSql.Expand(PreUpSql);
    internal static string PostUp => AdvanceSchemaSql.Expand(PostUpSql);
    internal static string PreDown => AdvanceSchemaSql.Expand(PreDownSql);
    internal static string PostDown => AdvanceSchemaSql.Expand(PostDownSql);

    private const string PreUpSql = """
        DO $cluster_guard$
        BEGIN
          IF current_setting('server_version_num')::integer < 170000 THEN RAISE EXCEPTION 'Authentication bootstrap foundation requires PostgreSQL 17 or later.'; END IF;
          IF current_database() IN ('postgres','template0','template1') THEN RAISE EXCEPTION 'Authentication bootstrap foundation refuses a PostgreSQL administrative database.'; END IF;
          IF to_regnamespace('__advance_schema__') IS NULL THEN RAISE EXCEPTION 'Authentication bootstrap foundation requires the advance schema.'; END IF;
        END $cluster_guard$;

        LOCK TABLE __advance_schema__.roles IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.role_page_permissions IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.employee_role_assignments IN SHARE ROW EXCLUSIVE MODE;

        DO $preflight$
        BEGIN
          IF (SELECT count(*) FROM __advance_schema__.roles)<>43 THEN RAISE EXCEPTION 'Role reconciliation expected exactly 43 roles.'; END IF;
          IF (SELECT count(*) FROM __advance_schema__.role_page_permissions)<>1086 THEN RAISE EXCEPTION 'Role reconciliation expected exactly 1,086 permissions.'; END IF;
          IF (SELECT count(*) FROM __advance_schema__.employee_role_assignments)<>40 THEN RAISE EXCEPTION 'Role reconciliation expected exactly 40 employee role assignments.'; END IF;
          IF (SELECT count(*) FROM __advance_schema__.roles WHERE "Code"=lower(btrim("Code")))<>38
             OR (SELECT count(*) FROM __advance_schema__.roles WHERE "Code"=upper(btrim("Code")))<>5 THEN
            RAISE EXCEPTION 'Role casing witness differs from the approved 38 lowercase / 5 uppercase state.';
          END IF;
          IF EXISTS(
            SELECT target_code
            FROM (
              SELECT CASE WHEN upper(btrim("Code"))='IT_ADMIN' THEN 'IT_MANAGER' ELSE upper(btrim("Code")) END target_code
              FROM __advance_schema__.roles
            ) x
            GROUP BY target_code HAVING count(*)>1
          ) THEN RAISE EXCEPTION 'Canonical role reconciliation would create a case or semantic collision.'; END IF;
          IF NOT EXISTS(
            SELECT 1 FROM __advance_schema__.roles
            WHERE "Id"='10000000-0000-0000-0000-000000000014'::uuid
              AND "Code"='it_admin' AND "Name"='IT Admin'
          ) THEN RAISE EXCEPTION 'Expected it_admin source role and RoleId were not found.'; END IF;
          IF (SELECT count(*) FROM __advance_schema__.role_page_permissions
              WHERE "RoleId"='10000000-0000-0000-0000-000000000014'::uuid)<>26 THEN
            RAISE EXCEPTION 'it_admin must have exactly 26 permissions before reconciliation.';
          END IF;
          IF EXISTS(SELECT 1 FROM __advance_schema__.employee_role_assignments
                    WHERE "RoleId"='10000000-0000-0000-0000-000000000014'::uuid) THEN
            RAISE EXCEPTION 'it_admin unexpectedly has employee assignments.';
          END IF;
          IF (SELECT count(*) FROM __advance_schema__.role_page_permissions p
              JOIN __advance_schema__.page_definitions d ON d."Id"=p."PageDefinitionId"
              WHERE p."RoleId"='10000000-0000-0000-0000-000000000014'::uuid
                AND d."PageKey" IN ('identity.roles','identity.users') AND NOT p."CanCreate")<>2 THEN
            RAISE EXCEPTION 'IT identity permission source state is not the approved read-only state.';
          END IF;
        END $preflight$;
        """;

    private const string PostUpSql = """
        DO $acceptance$
        BEGIN
          IF (SELECT count(*) FROM __advance_schema__.roles)<>43
             OR EXISTS(SELECT 1 FROM __advance_schema__.roles WHERE "Code"<>upper(btrim("Code"))) THEN
            RAISE EXCEPTION 'Acceptance failed: role codes are not exactly 43 canonical uppercase values.';
          END IF;
          IF (SELECT count(DISTINCT "Code") FROM __advance_schema__.roles)<>43 THEN
            RAISE EXCEPTION 'Acceptance failed: canonical role codes are not unique.';
          END IF;
          IF NOT EXISTS(
            SELECT 1 FROM __advance_schema__.roles
            WHERE "Id"='10000000-0000-0000-0000-000000000014'::uuid
              AND "Code"='IT_MANAGER' AND "Name"='IT Manager'
          ) THEN RAISE EXCEPTION 'Acceptance failed: IT_MANAGER did not retain the approved RoleId.'; END IF;
          IF (SELECT count(*) FROM __advance_schema__.role_page_permissions)<>1088 THEN
            RAISE EXCEPTION 'Acceptance failed: expected 1,088 permissions after the two additions.';
          END IF;
          IF (SELECT count(*) FROM __advance_schema__.employee_role_assignments)<>40 THEN
            RAISE EXCEPTION 'Acceptance failed: employee role assignments changed.';
          END IF;
          IF (SELECT count(*) FROM __advance_schema__.role_page_permissions
              WHERE "RoleId"='10000000-0000-0000-0000-000000000014'::uuid)<>28 THEN
            RAISE EXCEPTION 'Acceptance failed: IT_MANAGER must have exactly 28 permission rows.';
          END IF;
          IF (SELECT count(*) FROM __advance_schema__.role_page_permissions p
              JOIN __advance_schema__.page_definitions d ON d."Id"=p."PageDefinitionId"
              WHERE p."RoleId"='10000000-0000-0000-0000-000000000014'::uuid
                AND d."PageKey" IN ('identity.roles','identity.users','security.employee-identities','security.operational-scopes')
                AND p."CanView" AND p."CanCreate" AND NOT p."HasFullControl")<>4 THEN
            RAISE EXCEPTION 'Acceptance failed: IT_MANAGER ceremony grants are incomplete or over-privileged.';
          END IF;
          IF (SELECT count(*) FROM __advance_schema__.authentication_bootstrap_state
              WHERE "Id"='81000000-0000-0000-0000-000000000001'::uuid
                AND "Status"='PENDING' AND "EmployeeId" IS NULL AND "CompanyId" IS NULL
                AND "IssuerSha256" IS NULL AND "SubjectSha256" IS NULL)<>1 THEN
            RAISE EXCEPTION 'Acceptance failed: bootstrap singleton is not pending and empty.';
          END IF;
        END $acceptance$;
        """;

    private const string PreDownSql = """
        DO $cluster_guard$
        BEGIN
          IF current_setting('server_version_num')::integer < 170000 THEN RAISE EXCEPTION 'Authentication bootstrap rollback requires PostgreSQL 17 or later.'; END IF;
          IF current_database() IN ('postgres','template0','template1') THEN RAISE EXCEPTION 'Authentication bootstrap rollback refuses a PostgreSQL administrative database.'; END IF;
          IF to_regnamespace('__advance_schema__') IS NULL THEN RAISE EXCEPTION 'Authentication bootstrap rollback requires the advance schema.'; END IF;
        END $cluster_guard$;

        LOCK TABLE __advance_schema__.roles IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.role_page_permissions IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.employee_role_assignments IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.authentication_bootstrap_state IN SHARE ROW EXCLUSIVE MODE;

        DO $down_guard$
        BEGIN
          IF (SELECT count(*) FROM __advance_schema__.authentication_bootstrap_state
              WHERE "Id"='81000000-0000-0000-0000-000000000001'::uuid
                AND "Status"='PENDING' AND "EmployeeId" IS NULL AND "CompanyId" IS NULL
                AND "IssuerSha256" IS NULL AND "SubjectSha256" IS NULL)<>1 THEN
            RAISE EXCEPTION 'Rollback refused: the one-time bootstrap has been used or its state changed.';
          END IF;
          IF (SELECT count(*) FROM __advance_schema__.roles)<>43
             OR (SELECT count(*) FROM __advance_schema__.role_page_permissions)<>1088
             OR (SELECT count(*) FROM __advance_schema__.employee_role_assignments)<>40 THEN
            RAISE EXCEPTION 'Rollback refused: authorization cardinality changed after migration.';
          END IF;
          IF EXISTS(SELECT 1 FROM __advance_schema__.employee_role_assignments
                    WHERE "RoleId"='10000000-0000-0000-0000-000000000014'::uuid) THEN
            RAISE EXCEPTION 'Rollback refused: IT_MANAGER now has employee assignments.';
          END IF;
          IF NOT EXISTS(SELECT 1 FROM __advance_schema__.roles
                        WHERE "Id"='10000000-0000-0000-0000-000000000014'::uuid
                          AND "Code"='IT_MANAGER' AND "Name"='IT Manager') THEN
            RAISE EXCEPTION 'Rollback refused: IT_MANAGER source state changed.';
          END IF;
        END $down_guard$;
        """;

    private const string PostDownSql = """
        DO $acceptance$
        BEGIN
          IF (SELECT count(*) FROM __advance_schema__.roles)<>43
             OR (SELECT count(*) FROM __advance_schema__.roles WHERE "Code"=lower(btrim("Code")))<>38
             OR (SELECT count(*) FROM __advance_schema__.roles WHERE "Code"=upper(btrim("Code")))<>5 THEN
            RAISE EXCEPTION 'Rollback acceptance failed: role casing was not restored.';
          END IF;
          IF NOT EXISTS(SELECT 1 FROM __advance_schema__.roles
                        WHERE "Id"='10000000-0000-0000-0000-000000000014'::uuid
                          AND "Code"='it_admin' AND "Name"='IT Admin') THEN
            RAISE EXCEPTION 'Rollback acceptance failed: it_admin was not restored.';
          END IF;
          IF (SELECT count(*) FROM __advance_schema__.role_page_permissions)<>1086
             OR (SELECT count(*) FROM __advance_schema__.employee_role_assignments)<>40 THEN
            RAISE EXCEPTION 'Rollback acceptance failed: authorization cardinality was not restored.';
          END IF;
        END $acceptance$;
        """;
}
