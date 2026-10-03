# SESS.NexaERP.Installer

Command-line provisioning, authentication bootstrap, retention and verified backup operations.

Start at `Program.cs`. `DatabasePrincipalProvisioningSql.cs` provisions database roles and function access; `*BootstrapCommand*.cs` handles authentication; `VerifiedBackup*` verifies backups. Commands can mutate a database: follow the runbook rather than execute them for discovery.

See [R1 code map](../../docs/CODE-MAP.md) for screen, API, service, table, migration and test links, and the [fresh database runbook](../../docs/installation/go-live-fresh-database-runbook.md) for setup. No revision-named files are renamed before go-live.
