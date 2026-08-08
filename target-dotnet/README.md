# SESS NexaERP Target .NET Architecture Workspace

This folder is the controlled migration target. It must not replace the current installed REV861 ERP until migration, testing and UAT are complete.

Target runtime: .NET 10 LTS.

Local status on 2026-08-08:

- System SDK: .NET 8.0.129
- Workspace-local SDK: .NET 10.0.302 at `..\.dotnet10`
- Foundation build status: PASS on 2026-08-08 using the workspace-local SDK.

## Intended Solution Layout

- `src/SESS.NexaERP.Api` - ASP.NET Core Web API host
- `src/SESS.NexaERP.Application` - application services, DTOs and validation contracts
- `src/SESS.NexaERP.Domain` - domain entities and business rules
- `src/SESS.NexaERP.Infrastructure` - PostgreSQL, Redis, file storage and external services
- `src/SESS.NexaERP.Modules.*` - module boundaries such as Identity, Purchase and Stores
- `tests` - unit, integration, security and migration tests

## Build Commands

Use the workspace-local .NET 10 SDK:

```powershell
..\.dotnet10\dotnet.exe --info
..\.dotnet10\dotnet.exe restore .\SESS.NexaERP.slnx
..\.dotnet10\dotnet.exe build .\SESS.NexaERP.slnx --configuration Release
..\.dotnet10\dotnet.exe test
```
