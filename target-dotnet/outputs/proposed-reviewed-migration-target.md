# Proposed explicit approval for the SESS migration target

Status: proposal only. No production guard has been changed. The current chain still refuses sess_nexa_erp. Automatic approval review rejected applying the change without the user's explicit approval.

## Decision requested

Permit reviewed upgrades of sess_nexa_erp only when the operator explicitly names that database, authenticates as nexa_erp_migration, and acts as its managed nexa_erp_owner role. Keep the default refusal. Keep PostgreSQL version checks, all function-body/ownership/ACL/trigger checks, and unconditional refusal of postgres, template0 and template1.

This adds one explicit environment setting to the existing documented command:

```powershell
$env:NexaErp__ApprovedMigrationDatabase = 'sess_nexa_erp'
```

The design-time factory will require this to match NexaErp__ExpectedDatabase and the connection's database before a protected-name update starts. It will reject a non-migration username, append only a validated database identifier to the connection's custom session setting, and never change the supplied credentials. It will not connect to, migrate, rename or restore the live database as part of development verification. No manual history insertion or skipped migration is proposed.

## Exact shared SQL refusal predicate

Replace only the old unconditional protected-name predicate in the 13 listed migration guard builders with this predicate. The other guard conditions and all business SQL remain intact:

```sql
(lower(current_database()) IN ('postgres','template0','template1')
 OR (lower(current_database()) IN ('sess_nexaerp','sess_nexa_erp') AND NOT (
   session_user='nexa_erp_migration' AND current_user='nexa_erp_owner'
   AND coalesce(current_setting('nexa_erp.approved_migration_database',true),'')=current_database()
   AND EXISTS (
     SELECT 1 FROM pg_catalog.pg_roles m
     JOIN pg_catalog.pg_roles o ON o.rolname='nexa_erp_owner'
     JOIN pg_catalog.pg_database d ON d.datname=current_database() AND d.datdba=o.oid
     WHERE m.rolname=session_user AND m.rolcanlogin AND NOT o.rolcanlogin
       AND NOT (m.rolsuper OR m.rolcreatedb OR m.rolcreaterole OR m.rolreplication OR m.rolbypassrls
         OR o.rolsuper OR o.rolcreatedb OR o.rolcreaterole OR o.rolreplication OR o.rolbypassrls)
   )
 )))
```

The coalesce is deliberate: a missing session setting must refuse, not evaluate to SQL NULL and fall through. A matching setting alone does not authorize postgres, runtime, bootstrap, a migration login without the owner role, or incorrectly privileged managed roles.

## Affected migration guard builders

- 20260913060000_CommandReceiptReplay
- 20260913070000_ForeignPaymentBankAdvice
- 20260913080000_ForeignCurrencyFinancialReadModels
- 20260913090000_GovernedVendorBankAdvice
- 20260913095000_PurchaseOrderSupersedeHistory
- 20260913100000_VendorPoCashCap
- 20260914010000_PurchaseWorkload
- 20260914020000_PurchaseSpending
- 20260914030000_ReceiptQuantityAcrossPoRevisions
- 20260914040000_PurchaseObligations
- 20260914050000_PurchaseOpenOrders
- 20260914060000_StoresWorkload
- 20260914070000_StoresQcStock

Both Up and Down use their existing guard builders, so the approval requirement applies to both directions. This is a real change to deployment authorization; it is not merely a test correction. It does not change application authentication or grant the runtime role migration authority.

## Required verification after approval

Build both configurations. Prove missing/wrong approval, wrong login/role, prohibited role attributes, and system databases remain refused. Prove the explicitly approved migration login acting as owner is allowed. Re-run the fresh bootstrap, complete migration up/down and current guard-drift tests. Re-run the actual 78-migration backup in an isolated PostgreSQL instance with the relevant target-name check exercised, then Installer provision/status. Keep the live server untouched. Report the final commit, migration count and exact command only after those checks pass.

The earlier successful advance_parser restore proves the backup's data-state compatibility through 100 migrations; it does not prove the live-name command. The unmodified chain would first stop at CommandReceiptReplay after six earlier pending migrations.