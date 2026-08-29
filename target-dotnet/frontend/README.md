# SESS NexaERP Frontend

React + Vite + TypeScript frontend for the NexaERP .NET 10 API. First implemented screen: **Employee Master**.

## Run (development)

1. Start the API (from `target-dotnet`):

   ```powershell
   ..\.dotnet10\dotnet.exe run --project .\src\SESS.NexaERP.Api
   ```

2. Start the frontend:

   ```powershell
   cd frontend
   npm install
   npm run dev
   ```

3. Open http://localhost:5173

The Vite dev server proxies `/api` and `/health` to the API (default `http://localhost:5000`). If the API listens on a different port, create `frontend/.env.local` with:

```
VITE_API_TARGET=http://localhost:<port>
```

## Authentication

The API accepts only JWT bearer tokens (permanent OIDC design; the production identity provider is not yet selected — see `docs/rev866_oidc_decision_note.md`). Use the **Dev token** box in the top-right corner to paste a development JWT; it is stored in browser localStorage and attached to every request. Without a valid token the API returns 401.

## Structure

- `src/api` — fetch client (token handling, error mapping) and employee API bindings
- `src/types` — TypeScript mirrors of the backend contracts in `SESS.NexaERP.Application.Employees`
- `src/features/employees` — Employee Master list, detail (profile/roles/history tabs), create/edit form, approval and login actions
- `src/components` — shared UI (dev token box)

## Employee Master capabilities

- Paginated list with search (code/name) and status filter
- Detail view with Profile, Roles, and Approval History tabs
- Create / edit with mandatory remarks (matches backend validation)
- Approval workflow actions: Submit / Approve / Reject / Request revision (shown according to current approval status)
- Login activate/deactivate with mandatory reason
