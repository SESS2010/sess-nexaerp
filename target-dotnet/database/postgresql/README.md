# PostgreSQL Migration Area

This folder will contain reviewed EF Core migrations and controlled SQL migration scripts.

Rules:

- PostgreSQL is the authoritative source of record.
- No production local JSON fallback.
- Every stock, approval, PO, GRN, issue, return, transfer and accounts handoff mutation must use transactions.
- Every command needs idempotency and duplicate prevention where retry is possible.
- Large registers must use server-side paging and indexes.

