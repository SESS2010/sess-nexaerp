# REV863 Phase 1 Master API And PostgreSQL Apply Report

Date: 2026-08-08

## Result

Continued the `.NET 10 / PostgreSQL` migration foundation after REV862. The live REV861 HTML/Node ERP was not modified.

## PostgreSQL Status

Local PostgreSQL service found:

- `postgresql-x64-17`
- Status: `Running`

Migration apply attempted through EF Core:

```powershell
..\.dotnet10\dotnet.exe tool run dotnet-ef database update --project .\src\SESS.NexaERP.Infrastructure\SESS.NexaERP.Infrastructure.csproj --startup-project .\src\SESS.NexaERP.Api\SESS.NexaERP.Api.csproj --context NexaErpDbContext
```

Result:

- Build succeeded
- Database connection failed
- Error: `28P01: password authentication failed for user "postgres"`

Conclusion:

The local PostgreSQL admin password is different from the placeholder connection string. Database migration is ready, but applying it requires the correct dev DB username/password or a created `sess_nexaerp` database user.

## SQL Script Generated

Idempotent migration SQL script created:

`C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet\database\postgresql\rev862_phase1_foundation_idempotent.sql`

This can be reviewed/applied by IT or by using the correct PostgreSQL credentials.

## Master API Contracts Added

Identity:

- `RoleSummary`
- `CreateRoleRequest`
- `UserAccountSummary`
- `CreateUserAccountRequest`

Masters:

- `CustomerSummary`
- `CreateCustomerRequest`
- `VendorSummary`
- `CreateVendorRequest`

Inventory:

- `ItemSummary`
- `CreateItemRequest`
- `WarehouseSummary`
- `CreateWarehouseRequest`
- `RackBinSummary`
- `CreateRackBinRequest`

## API Endpoints Added

Identity:

- `GET /api/v1/identity/roles`
- `POST /api/v1/identity/roles`
- `GET /api/v1/identity/users`
- `POST /api/v1/identity/users`

Masters:

- `GET /api/v1/masters/customers`
- `POST /api/v1/masters/customers`
- `GET /api/v1/masters/vendors`
- `POST /api/v1/masters/vendors`

Inventory:

- `GET /api/v1/inventory/items`
- `POST /api/v1/inventory/items`
- `GET /api/v1/inventory/warehouses`
- `POST /api/v1/inventory/warehouses`
- `POST /api/v1/inventory/rack-bins`

## Controls Started

- Duplicate role code blocking
- Duplicate login ID blocking
- Active role required before creating user
- Privileged role forces MFA requirement
- Duplicate customer code/GST blocking
- Duplicate vendor code/GST blocking
- New vendor defaults to `PendingTdApproval`
- Duplicate item code/barcode blocking
- Negative minimum stock blocked
- Duplicate warehouse code blocking
- Duplicate rack/bin blocked within warehouse
- Server-side pagination limits added for large master lists
- Audit writer scaffold added for create actions
- Header-based temporary current-user adapter added for migration testing

## Verification

Build:

- Passed
- Warnings: `0`
- Errors: `0`

Tests:

- Passed: `3`
- Failed: `0`

Smoke tested endpoints:

| Endpoint | Result |
|---|---:|
| `/health/live` | 200 |
| `/health/ready` | 200 |
| `/api/v1/system/database-model` | 200 |

## Pending Before Runtime CRUD Demo

- Provide or configure valid development PostgreSQL credentials.
- Apply `Phase1Foundation` migration to PostgreSQL.
- Seed baseline roles from the REV861 role catalogue.
- Seed or migrate initial item/vendor/customer records.
- Replace temporary header current-user adapter with production OIDC/OAuth identity integration.
- Add update/deactivate endpoints and full audit history views.
