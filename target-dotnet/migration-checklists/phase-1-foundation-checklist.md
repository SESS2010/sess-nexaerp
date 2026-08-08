# Phase 1 .NET Foundation Checklist

Date: 2026-08-08

## Required Before Build Claim

- Install current supported .NET LTS SDK for the target runtime.
- Confirm `dotnet --info` shows the required SDK.
- Restore, build and test the solution in `target-dotnet`.

## Foundation Work

- ASP.NET Core Web API host
- Modular monolith folder/project structure
- PostgreSQL connection configuration
- EF Core migration project
- Central validation pipeline
- Central exception handling
- Structured logging
- Health and readiness endpoints
- OpenAPI documentation
- API versioning
- Identity and access foundation
- Role/permission backend authorization
- Record-level authorization for customer/vendor/employee scope
- Audit framework
- Redis/distributed cache abstraction
- File-storage abstraction for S3/Azure Blob
- Queue/background worker abstraction
- Environment-based configuration
- Test projects and migration tests

## Do Not Mark Complete Until

- Build passes with the approved SDK.
- PostgreSQL migration can create the foundation schema.
- Health/readiness endpoints run.
- Identity authorization is backend enforced.
- Audit records are written for create/update/delete/approval actions.
- A sample current-system page/route is mapped into the new module structure.
