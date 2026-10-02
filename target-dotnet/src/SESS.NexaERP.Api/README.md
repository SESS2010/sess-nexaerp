# SESS.NexaERP.Api

ASP.NET Core HTTP host.

Start at `Program.cs`. `Endpoints/` groups routes; partials may contain direct EF queries and service calls. `Security/` enforces identity and page permissions, `Middleware/` handles identity and errors, and `Hosting/` serves the packaged frontend.

See [R1 code map](../../docs/CODE-MAP.md) for screen, API, service, table, migration and test links, and the [fresh database runbook](../../docs/installation/go-live-fresh-database-runbook.md) for setup. No revision-named files are renamed before go-live.
