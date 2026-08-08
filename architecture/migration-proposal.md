# SESS NexaERP Migration Proposal

Date: 2026-08-08

## Target Stack

- Backend: ASP.NET Core Web API on .NET 10 LTS.
- Architecture: modular monolith first, with module boundaries that can later split into services.
- Database: PostgreSQL as authoritative system of record.
- ORM/data access: EF Core migrations for transactional writes; Dapper/optimized SQL only for measured report hot paths.
- Frontend: React + TypeScript with feature modules, route splitting and server-side pagination.
- Cache: Redis for permission lookup, safe master-data cache, dashboard summaries, rate limits and idempotency keys.
- Storage: AWS S3 or Azure Blob with file metadata in PostgreSQL.
- Deployment: Linux containers behind load balancer with CI/CD, health/readiness checks and observability.

Official .NET check: Microsoft .NET support policy lists .NET 10 as LTS, active, supported until November 14, 2028. The local machine currently has .NET 8 SDK only, so .NET 10 SDK must be installed before target build/test can be claimed.

## Migration Method

Use a strangler-style migration. Do not delete the existing REV861 ERP.

1. Freeze current source and data structures.
2. Place snapshot and migration documents in Git.
3. Export current PostgreSQL schema and local JSON structures.
4. Catalogue every page, route, field, calculation, permission and workflow.
5. Build the ASP.NET Core foundation separately.
6. Migrate foundation masters.
7. Migrate Purchase and Stores module by module.
8. Compare old and new outputs for the same business cases.
9. Run role/security/concurrency/load tests.
10. Switch production only after UAT and rollback readiness.

## Module Migration Order

1. Identity and Access
2. Audit and Notification
3. Company, Employee/User, Role, Permission
4. Customer Master
5. Vendor Master
6. Item/Material Master
7. Warehouse, Rack/Bin, Numbering and Approval Settings
8. Purchase Request and stock check
9. RFQ, Quote, Comparison and Purchase Approval
10. PO and Follow-up
11. Material Inward, GRN, QC
12. Inventory, Reservation, Issue, Return, Transfer and Stock Ledger
13. Accounts handover and three-way match
14. Customer/Vendor portals
15. Reports, dashboards and exports

## Rollback Strategy

- Keep REV861 snapshot and installed app unchanged during migration.
- All data migration scripts must be repeatable and logged.
- New system initially runs in UAT against copied/migrated test data.
- Production cutover requires:
  - database backup
  - application image version
  - config snapshot
  - migration run log
  - rollback script
  - DNS/load-balancer rollback procedure
- Old system remains read-only during transition.

## Test Strategy

- Unit tests for domain rules.
- Integration tests against PostgreSQL test container/database.
- API authorization tests for each action.
- End-to-end happy path: PR -> RFQ -> Quote -> Compare -> PO -> Inward -> GRN -> QC -> Stock -> Issue -> Accounts.
- Concurrency tests for stock, approval, PO, GRN and payment duplicate prevention.
- Load tests with k6/JMeter at staged targets: 1k, 5k, 10k, 25k, 50k, 100k and only then 300k concurrent sessions if commercially required.
- Backup/restore and failover drills.

## Evidence Required Before Production Switch

- Passing .NET 10 build and automated tests.
- PostgreSQL migrations reviewed and applied.
- Data migration reconciliation.
- Role-permission matrix.
- API security test report.
- Stock-ledger reconciliation report.
- Purchase/Stores report reconciliation.
- Load-test report with environment, scripts, data volume, P95/P99, error rate and bottlenecks.
- Backup/restore proof.
- UAT approval.

