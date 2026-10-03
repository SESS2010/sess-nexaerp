# SESS.NexaERP.Infrastructure

EF Core/PostgreSQL persistence and concrete services.

Start at `DependencyInjection.cs`; feature folders contain `Ef...` services. `Persistence/NexaErpDbContext*.cs` maps entities; `Persistence/Migrations/` holds the immutable chain and SQL resources. `Reporting/` holds read SQL; `Outbox/` and `Tracking/` supply retained R1 data. Only the integrator writes migrations; never edit merged migrations.

See [R1 code map](../../docs/CODE-MAP.md) for screen, API, service, table, migration and test links, and the [fresh database runbook](../../docs/installation/go-live-fresh-database-runbook.md) for setup. No revision-named files are renamed before go-live.
