# SESS NexaERP Web (React frontend)

React + Vite + TypeScript frontend for the NexaERP .NET 10 API. Lives at `src/SESS.NexaERP.Web/` per the team layout agreement.

Implemented screens: **Login**, **Employee Master**, **Vendor Master**.

## Run (development)

1. Start the API (from `target-dotnet`):

   ```powershell
   $env:ConnectionStrings__NexaErp = "Host=localhost;Database=sess_nexa_erp;Username=postgres;Password=<password>"
   $env:ASPNETCORE_ENVIRONMENT = "Development"
   $env:DatabaseSecurity__AllowDevelopmentSuperuser = "true"
   $env:NexaErp__AllowDevelopmentAuthentication = "true"
   $env:ASPNETCORE_URLS = "http://localhost:5000"
   dotnet run --project .\src\SESS.NexaERP.Api
   ```

2. Start the frontend:

   ```powershell
   cd src\SESS.NexaERP.Web
   npm install
   npm run dev
   ```

3. Open http://localhost:5173

The Vite dev server proxies `/api` and `/health` to the API (default `http://localhost:5000`). If the API listens elsewhere, create `.env.local` with `VITE_API_TARGET=http://localhost:<port>`.

## Run on the office network (shared server)

Build the frontend straight into the API host and let the API serve it, so everyone opens one URL:

```powershell
cd src\SESS.NexaERP.Web
npm run build:api      # writes ../SESS.NexaERP.Api/wwwroot
```

Then start the API bound to all interfaces (`ASPNETCORE_URLS=http://0.0.0.0:5000`, already the default in `launchSettings.json`) and open `http://<server-ip>:5000` from any workstation. Full steps, including the Windows Firewall rule and database notes, are in `docs/installation/lan-network-access.md`.

## Authentication

The API accepts only JWT bearer tokens (permanent OIDC design; provider selection pending — see `docs/rev866_oidc_decision_note.md`). The **/login** page uses the Debug-only development sign-in pipeline (`/api/v1/dev/*`): pick an employee identity and a company; the API issues a short-lived JWT bound to that employee's real identity mapping. When the production OIDC provider is chosen, the login page swaps to the standard OIDC redirect — no other screen changes.

There is deliberately **no username/password authentication** anywhere (REV866 decision); `user_accounts.PasswordHash` is a placeholder.

## Structure

- `src/api` — fetch client (PascalCase wire contract, standard error envelope) and per-module API bindings
- `src/types` — TypeScript mirrors of the backend contracts
- `src/features/auth` — login page and route guard
- `src/features/employees`, `src/features/vendors` — master screens
- `src/components` — shared UI
