# REV862 Phase 1 .NET Backend Foundation Report

Date: 2026-08-08

## Result

Phase 1 backend foundation has started in the controlled `.NET 10 / PostgreSQL` migration target. The live REV861 HTML/Node ERP was not modified.

## Projects Added

Solution:

`C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet\SESS.NexaERP.slnx`

Projects now included:

- `SESS.NexaERP.Api`
- `SESS.NexaERP.Domain`
- `SESS.NexaERP.Application`
- `SESS.NexaERP.Infrastructure`
- `SESS.NexaERP.Tests`

## Packages Added

Application:

- `Microsoft.Extensions.DependencyInjection.Abstractions 10.0.10`

Infrastructure:

- `Microsoft.EntityFrameworkCore 10.0.10`
- `Microsoft.EntityFrameworkCore.Design 10.0.10`
- `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore 10.0.10`
- `Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3`

API:

- `Microsoft.EntityFrameworkCore.Design 10.0.10`
- Existing OpenAPI package retained with patched `Microsoft.OpenApi 2.7.5`

Tooling:

- Local `dotnet-ef 10.0.10` tool manifest added in `target-dotnet`

## Foundation Domain Model Added

Identity:

- `Role`
- `UserAccount`

Masters:

- `Customer`
- `Vendor`

Inventory / Stores:

- `Item`
- `Warehouse`
- `RackBin`
- `StockMovement`

Audit:

- `AuditLog`

Common:

- `AuditableEntity`

## PostgreSQL EF Core Foundation

DbContext:

`target-dotnet\src\SESS.NexaERP.Infrastructure\Persistence\NexaErpDbContext.cs`

Schema:

- `nexa`

Initial migration generated:

`target-dotnet\src\SESS.NexaERP.Infrastructure\Persistence\Migrations\20260808110924_Phase1Foundation.cs`

Tables created by migration:

- `nexa.audit_logs`
- `nexa.customers`
- `nexa.items`
- `nexa.roles`
- `nexa.vendors`
- `nexa.warehouses`
- `nexa.user_accounts`
- `nexa.rack_bins`
- `nexa.stock_movements`

Important constraints started:

- Unique role code
- Unique user login ID
- Unique customer code
- Unique customer GST number where present
- Unique vendor code
- Unique vendor GST number where present
- Unique item code
- Unique item barcode where present
- Unique warehouse code
- Unique rack/bin code within warehouse
- Foreign keys for user role, rack/bin warehouse, and stock movement item/location
- Concurrency token field on foundation entities

## API Foundation Added

Endpoints available:

- `/health/live`
- `/health/ready`
- `/health/db`
- `/api/v1/system/architecture`
- `/api/v1/system/modules`
- `/api/v1/system/database-model`
- `/api/v1/purchase-stores/workflow-stages`

Notes:

- `/health/ready` remains usable without a local PostgreSQL database.
- `/health/db` is the honest PostgreSQL readiness check and will require a real configured PostgreSQL connection.

## Verification

Build command:

```powershell
..\.dotnet10\dotnet.exe build .\SESS.NexaERP.slnx --configuration Release
```

Build result:

- Passed
- Warnings: `0`
- Errors: `0`

Test command:

```powershell
..\.dotnet10\dotnet.exe test .\SESS.NexaERP.slnx --configuration Release --no-build
```

Test result:

- Passed: `2`
- Failed: `0`
- Skipped: `0`

Smoke test result:

| Endpoint | Result |
|---|---:|
| `/health/live` | 200 |
| `/health/ready` | 200 |
| `/api/v1/system/modules` | 200 |
| `/api/v1/purchase-stores/workflow-stages` | 200 |
| `/api/v1/system/database-model` | 200 |

## Still Pending

- Configure real PostgreSQL connection/environment secrets.
- Apply migration to a development PostgreSQL database.
- Add identity password hashing, login/token flow, MFA policy, and lockout rules.
- Add backend permission enforcement and record-level customer/vendor isolation.
- Add audit writer implementation and API request audit capture.
- Add Item/Vendor/Customer/Employee master APIs.
- Add migration scripts from REV861 catalogue/local data into PostgreSQL.
- Add Purchase and Inventory module services only after master/permission foundation is ready.
