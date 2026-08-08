# REV861 .NET SDK Install And Foundation Build Report

Date: 2026-08-08

## Result

.NET 10 SDK was installed locally inside the workspace and the target .NET migration foundation was built successfully.

## SDK Installed

Workspace-local SDK path:

`C:\Users\User\Documents\Codex\2026-07-03\see\.dotnet10`

Verified SDK:

- .NET SDK: `10.0.302`
- .NET host/runtime: `10.0.10`
- ASP.NET Core runtime: `10.0.10`

The system-wide SDK remains `.NET 8.0.129`; the ERP migration target uses the workspace-local `.dotnet10\dotnet.exe`.

## Files Updated / Created

- `target-dotnet\SESS.NexaERP.slnx`
- `target-dotnet\global.json`
- `target-dotnet\src\SESS.NexaERP.Api\SESS.NexaERP.Api.csproj`
- `target-dotnet\src\SESS.NexaERP.Api\Program.cs`

## Dependency Security Fix

Adding `Microsoft.AspNetCore.OpenApi 10.0.10` initially pulled `Microsoft.OpenApi 2.0.0`, which NuGet flagged as high severity vulnerability `GHSA-v5pm-xwqc-g5wc`.

Fix applied:

- Added direct package reference `Microsoft.OpenApi 2.7.5`

This follows the advisory patched version line for `Microsoft.OpenApi` 2.x.

## Build Verification

Command:

```powershell
..\.dotnet10\dotnet.exe build .\SESS.NexaERP.slnx --configuration Release --no-restore
```

Actual result:

- Build succeeded
- Warnings: `0`
- Errors: `0`

## Smoke Test Verification

The new API was started briefly on:

`http://127.0.0.1:5096`

Endpoints tested:

| Endpoint | Result |
|---|---:|
| `/health/live` | 200 |
| `/health/ready` | 200 |
| `/api/v1/system/modules` | 200 |
| `/api/v1/purchase-stores/workflow-stages` | 200 |

The test API process was stopped after verification.

## Current Status

Phase 1 foundation has moved from blueprint-only to a compiling .NET 10 starting point. This is not yet the production ERP; PostgreSQL schema, identity, authorization, audit, migrations, and module services still need implementation before Purchase/Inventory migration.
