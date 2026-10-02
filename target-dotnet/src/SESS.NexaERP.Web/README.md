# SESS.NexaERP.Web

React, Vite and TypeScript frontend.

Start at `src/App.tsx` for routes. `src/features/` groups screens, `src/api/` contains HTTP bindings, `src/types/` mirrors contracts, `src/auth/` and `features/auth/` handle OIDC/session gates, and `src/print/` contains PO/Machine DC layouts. Home shortcuts are in `src/features/home/pageCatalog.ts`.

Local commands are in `package.json`: `npm run dev`, `npm run build`, `npm test`. Vite proxies API calls; inspect `vite.config.ts` for the target. Use an authorized disposable development environment. Production packaging requires a named frontend SHA and production-login gate. A frontend page does not prove its backend exists.

See [R1 code map](../../docs/CODE-MAP.md) for screen, API, service, table, migration and test links, and the [fresh database runbook](../../docs/installation/go-live-fresh-database-runbook.md) for setup. No revision-named files are renamed before go-live.
