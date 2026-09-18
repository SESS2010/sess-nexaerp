namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

// The caller declares role_name and validates the complete retired-role topology.
// Empty retired roles must not force an ordinary upgrade to acquire cluster authority.
internal static class RetiredRoleOwnershipSql
{
    internal const string ReassignIfNeeded = """
        IF EXISTS (
          SELECT 1 FROM pg_catalog.pg_shdepend d
          JOIN pg_catalog.pg_roles r ON r.oid=d.refobjid
          WHERE d.refclassid='pg_catalog.pg_authid'::regclass
            AND r.rolname=role_name
            -- Match PostgreSQL 17 shdepReassignOwned: default ACLs and user
            -- mappings are ignored; initial extension ACL references are rewritten.
            AND ((d.deptype='o' AND d.classid NOT IN
                  ('pg_catalog.pg_default_acl'::regclass,'pg_catalog.pg_user_mapping'::regclass))
                 OR d.deptype='i')
            AND (d.dbid=0 OR d.dbid=(SELECT oid FROM pg_catalog.pg_database WHERE datname=current_database()))
        ) THEN
          IF NOT pg_catalog.pg_has_role(current_user,role_name,'SET')
             OR NOT pg_catalog.pg_has_role(current_user,'nexa_erp_owner','SET') THEN
            RAISE EXCEPTION USING ERRCODE='42501',
              MESSAGE=format('Retired role %s still owns objects; ownership preparation is required before this upgrade.',role_name),
              HINT='Keep the API stopped. Have the PostgreSQL administrator reconcile ownership using the legacy upgrade runbook; do not grant the migration login cluster privileges.';
          END IF;
          EXECUTE format('REASSIGN OWNED BY %I TO nexa_erp_owner',role_name);
        END IF;
        """;
}
