# Running NexaERP on the office network (one server, one database)

Goal: one machine (the "ERP server") runs PostgreSQL and the NexaERP API.
Every other workstation opens the ERP in a browser at `http://<server-ip>:5000`.
Nothing is installed on the workstations.

## 1. Fix the server's IP address

Give the ERP server a static IP (or a DHCP reservation on the router) so the
address never changes. Find the current address with:

```powershell
ipconfig
```

Look for `IPv4 Address` on the LAN adapter (for example `192.168.68.108`).

## 2. Allow the port through Windows Firewall (run once, as Administrator)

```powershell
New-NetFirewallRule -DisplayName "NexaERP API 5000" -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow -Profile Private,Domain
```

Only needed while using the Vite dev server from other machines:

```powershell
New-NetFirewallRule -DisplayName "NexaERP Vite 5173" -Direction Inbound -Protocol TCP -LocalPort 5173 -Action Allow -Profile Private,Domain
```

Make sure the network is classified as **Private** (Settings > Network > the
adapter > Network profile), otherwise the rule above does not apply.

## 3. Database

PostgreSQL stays on the ERP server and the API connects to it over
`localhost`, so the database port (5432) does **not** need to be opened.
Workstations never talk to PostgreSQL directly; they only talk to the API.

Only if PostgreSQL is on a *different* machine than the API:

- connection string: `Host=<db-server-ip>;Database=sess_nexa_erp;...`
- `postgresql.conf`: `listen_addresses = '*'`
- `pg_hba.conf`: add `host sess_nexa_erp <user> 192.168.68.0/24 scram-sha-256`
- open TCP 5432 in the DB server firewall, restart PostgreSQL.

## 4. Build the frontend into the API

From `target-dotnet/src/SESS.NexaERP.Web`:

```powershell
npm install
npm run build:api
```

This writes the compiled React app into `src/SESS.NexaERP.Api/wwwroot`
(git-ignored). The API serves it at `/` and routes `/api/*` and `/health/*`
to the backend. Re-run `npm run build:api` after every frontend change.

## 5. Start the API listening on all interfaces

`Properties/launchSettings.json` and `start-dev.local.ps1` already bind to
`http://0.0.0.0:5000`. If you start the API any other way, set:

```powershell
$env:ASPNETCORE_URLS = "http://0.0.0.0:5000"
dotnet run --project .\src\SESS.NexaERP.Api
```

`0.0.0.0` means "every network card on this machine", so the API answers on
`http://localhost:5000` *and* `http://<server-ip>:5000`.

## 6. Verify from another workstation

Open in a browser:

- `http://<server-ip>:5000/health/live` -> `Healthy`
- `http://<server-ip>:5000/` -> the ERP login page

If the first works and the second shows a JSON 404, `wwwroot` is missing:
run step 4 again.

## Development alternative (Vite dev server on the LAN)

`vite.config.ts` uses `host: true`, so `npm run dev` also listens on the LAN.
Other machines can open `http://<dev-machine-ip>:5173`; the Vite proxy forwards
API calls to the API on that same machine. Use this only while developing the
frontend; for the shared server use steps 4-5.

## Later: production hardening (not done yet)

- Run the API as a Windows Service (`sc.exe create` or NSSM) so it survives
  reboots and logouts.
- Put HTTPS in front (IIS or a reverse proxy with a company certificate).
- Replace development authentication with the OIDC provider
  (`docs/rev866_oidc_decision_note.md`).
