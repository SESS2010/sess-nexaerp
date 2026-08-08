# REV864 Permanent Auth And PostgreSQL Apply Report

Date: 2026-08-08

## Result

User correction accepted: no temporary workflow should remain in the ERP migration foundation.

Actions completed:

- Applied `Phase1Foundation` migration to local PostgreSQL.
- Removed the temporary header-based current-user adapter.
- Added permanent JWT/OIDC bearer authentication foundation.
- Added claims-based current user adapter.
- Protected identity/master/inventory CRUD endpoint groups with authorization.
- Removed all temporary smoke-test records from the development database.

The live REV861 HTML/Node ERP was not modified.

## PostgreSQL Migration Applied

Database:

- `sess_nexaerp`

Schema:

- `nexa`

Applied migration:

- `20260808110924_Phase1Foundation`

Created foundation tables:

- `nexa.audit_logs`
- `nexa.customers`
- `nexa.items`
- `nexa.roles`
- `nexa.vendors`
- `nexa.warehouses`
- `nexa.user_accounts`
- `nexa.rack_bins`
- `nexa.stock_movements`

Indexes and constraints were also created, including unique checks for item code/barcode, vendor/customer GST, role code, login ID, warehouse code, and rack/bin per warehouse.

## Temporary Smoke Test Data Removed

The following temporary records were deleted from PostgreSQL:

- Test role `ADMIN`
- Test user `TEST.USER@SESS`
- Test item `TEST-ITEM-001`
- Test customer `CUST-TEST-001`
- Test vendor `VEN-TEST-001`
- Test warehouse `MAIN-STORE`
- Test rack/bin `RACK-A-01`
- Related audit records

Verification count:

| Record type | Remaining test rows |
|---|---:|
| Roles | 0 |
| Users | 0 |
| Items | 0 |
| Customers | 0 |
| Vendors | 0 |
| Warehouses | 0 |
| Rack/bins | 0 |

## Permanent Auth Direction

Removed:

- `HeaderCurrentUser`
- Header-based temporary identity behavior

Added:

- `ClaimsCurrentUser`
- `Microsoft.AspNetCore.Authentication.JwtBearer 10.0.10`
- `UseAuthentication()`
- `UseAuthorization()`
- JWT bearer configuration placeholders through `Authentication:Authority` and `Authentication:Audience`

Protected endpoint groups:

- `/api/v1/identity/*`
- `/api/v1/masters/*`
- `/api/v1/inventory/*`

Public endpoints retained:

- `/health/live`
- `/health/ready`
- `/health/db`
- `/api/v1/system/architecture`
- `/api/v1/system/modules`
- `/api/v1/system/database-model`
- `/api/v1/purchase-stores/workflow-stages`

## Verification

Build:

- Passed
- Warnings: `0`
- Errors: `0`

Tests:

- Passed: `3`
- Failed: `0`

Runtime smoke test:

| Check | Result |
|---|---:|
| `/health/live` | 200 |
| `/health/db` | 200 |
| `/api/v1/system/database-model` | 200 |
| `/api/v1/inventory/items` without JWT | 401 |

## Permanent Next Step

Configure the real identity provider/OIDC authority and audience, then seed permanent baseline roles/users through a controlled seed migration or admin setup flow. No temporary login or header identity should be used going forward.
