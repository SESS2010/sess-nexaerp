# REV865 Page Permission And Role Seed Report

Date: 2026-08-08

## Result

Continued permanent development from REV864. This revision adds Page Master, role-page permissions, permanent SESS role seeds, and authorization policy structure.

The live REV861 HTML/Node ERP was not modified.

## Security Verification

Completed secret scan for:

- Shared PostgreSQL password
- Database password connection-string fragments
- JWT signing key names
- JWT secret names
- Development database connection environment-variable name

Result:

- No matches found in workspace files after cleanup.

Corrections made:

- Redacted old backup-output password strings.
- Removed hard-coded PostgreSQL fallback password from Infrastructure.
- Added no-secret EF design-time DbContext factory for migration/script generation.
- Kept DB credential usage outside source files.

## Database Migration Created / Applied

Migration created:

`target-dotnet\src\SESS.NexaERP.Infrastructure\Persistence\Migrations\20260808114550_Phase1AuthorizationSeed.cs`

Migration applied to separate development database:

- Database: `sess_nexaerp`
- Schema: `nexa`

New tables:

- `nexa.page_definitions`
- `nexa.role_page_permissions`

Seeded permanent SESS roles:

- `admin`
- `md`
- `accounts_head`
- `purchase_head`
- `store_head`
- `production_head`
- `qc_head`
- `design_head`
- `service_head`
- `sales_head`
- `service_coordinator`
- `service_engineer`
- `sales_engineer`
- `it_admin`
- `customer`
- `vendor`
- `document_controller`
- `dcc`
- `branch_manager`
- `ops_admin_no_hr`

Seeded initial Page Master entries:

- Role Master
- User Master
- Page Master
- Role Page Permissions
- Customer Master
- Vendor Master
- Item Master
- Warehouse Master
- Rack/Bin Master
- Purchase Request
- RFQ
- Purchase Order
- GRN
- Stock Ledger
- Audit History

Idempotent SQL script:

`target-dotnet\database\postgresql\rev865_phase1_authorization_seed_idempotent.sql`

## API Added

Authorization:

- `GET /api/v1/authorization/pages`
- `POST /api/v1/authorization/pages`
- `GET /api/v1/authorization/role-page-permissions`
- `PUT /api/v1/authorization/role-page-permissions`

Audit:

- `GET /api/v1/audit/history`

## Permission/Security Evidence

- Temporary header identity was already removed in REV864.
- Current user is claims-based only.
- JWT/OIDC bearer package is configured.
- Identity APIs require `AdminOnly`.
- Authorization page/permission APIs require `AdminOnly`.
- Master and inventory APIs require authenticated JWT.
- No JWT secret is hard-coded.
- No database password is hard-coded.

## Build/Test Result

Build:

- Passed
- Warnings: `0`
- Errors: `0`

Tests:

- Passed: `3`
- Failed: `0`

## Database Persistence Evidence

EF migration apply output confirmed:

- `page_definitions` table created.
- `role_page_permissions` table created.
- 15 page definitions inserted.
- 20 permanent SESS roles inserted.
- Migration history updated with `20260808114550_Phase1AuthorizationSeed`.

## Rollback Instructions

Development rollback only:

```powershell
cd C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet
..\.dotnet10\dotnet.exe tool run dotnet-ef database update 20260808110924_Phase1Foundation --project .\src\SESS.NexaERP.Infrastructure\SESS.NexaERP.Infrastructure.csproj --startup-project .\src\SESS.NexaERP.Api\SESS.NexaERP.Api.csproj --context NexaErpDbContext
```

Supply the development database connection string through environment variable or secret store before running rollback. Do not put credentials in the command or source file.

## Unresolved Issues

- Real OIDC/identity provider authority and audience are not configured yet.
- No production users are migrated.
- No production business data is migrated.
- Role-page permissions table exists, but detailed page-level permissions are not fully seeded yet.
- Purchase and Inventory transaction services are not yet implemented.
- No cloud deployment, DNS, firewall, or public exposure was done.

## Exact Next Step

Seed detailed role-page permission matrix for the 20 SESS roles, then add permanent Item/Vendor/Customer/Employee master service tests and API update/deactivate endpoints.
