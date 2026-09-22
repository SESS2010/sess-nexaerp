# Exact-name migration witness - 15 September 2026

The user authorized removing only sess_nexaerp and sess_nexa_erp from thirteen migration denylists. The PostgreSQL system names postgres, template0 and template1 remain refused. No approved-target setting or other authorization mechanism was added. Every existing version and structural check is unchanged.

## Result

The supplied before_tuesday.dump was restored into a NEW native PostgreSQL 17.10 cluster, initialized as postgres, with globals restored while omitting only the duplicate bootstrap CREATE ROLE. Database name: sess_nexa_erp. Loopback port: 56326. fsync and synchronous_commit stayed on. The prior failed copy on port 56949 was retained untouched.

Source: C:\Users\User\Desktop\before_tuesday.dump, 10,076,698 bytes; SHA-256 7601FC95F017B3C1104C4888430F82076ABD30594A83A7FEA9F44B760DB8C5A5. The source and live database were not changed.

Preflight verified the owned data directory, exact database/port, session_user=nexa_erp_migration, current_user=nexa_erp_owner, durable settings, 78 history entries and last migration 20260912064200_GovernedOpeningStockThreeActorCeremony. The generated production migration SQL then applied all 24 pending migrations, with ON_ERROR_STOP, to 102 ending 20260914100000_FifoReversedReceiptEligibility. Exit code 0. SQL execution took 0.6659309 seconds, excluding restore, build and provisioning.

Installer provision: RECONCILED; status: VERIFIED; both exit 0, credentials unchanged. This exact-name result supersedes the earlier protected-name blocker. No migration on the live server was performed or authorized by this witness.

Release build passed with zero warnings/errors (4m06s). Targeted bootstrap and purchase-foundation regression passed 14/14, zero failed/skipped, 1m15s, with name-guard-targeted-release.trx. This is a new targeted run, not a rerun of the accepted 836-test suite or a new Debug certification.

## Exactly which guards changed, and their origin

Each file matches a mechanical predicate-only transformation against its prior committed contents. Twelve remove the two SESS entries from the five-name list. ForeignPaymentBankAdvice's separate two-name prohibition now lists only the three system names; its inherited system/version/function-baseline guard is unchanged. All thirteen affect both migration directions through existing shared builders.

| Migration | Introducing commit and timestamp (IST) | Change |
| --- | --- | --- |
| 20260913060000_CommandReceiptReplay | c3e4902 2026-09-13T17:35:51+05:30 | Changed |
| 20260913070000_ForeignPaymentBankAdvice | 4c5325e 2026-09-13T20:57:18+05:30 | Changed |
| 20260913080000_ForeignCurrencyFinancialReadModels | c43f90d 2026-09-13T21:36:47+05:30 | Changed |
| 20260913090000_GovernedVendorBankAdvice | dd4ce50 2026-09-13T23:21:01+05:30 | Changed |
| 20260913095000_PurchaseOrderSupersedeHistory | 2b41e15 2026-09-14T02:11:00+05:30 | Changed |
| 20260913100000_VendorPoCashCap | 2b41e15 2026-09-14T02:11:00+05:30 | Changed |
| 20260914010000_PurchaseWorkload | be40354 2026-09-14T03:20:38+05:30 | Changed |
| 20260914020000_PurchaseSpending | 8e329cf 2026-09-14T03:48:34+05:30 | Changed |
| 20260914030000_ReceiptQuantityAcrossPoRevisions | 5e34e61 2026-09-14T05:07:31+05:30 | Changed |
| 20260914040000_PurchaseObligations | 41740cd 2026-09-14T05:38:04+05:30 | Changed |
| 20260914050000_PurchaseOpenOrders | 358e921 2026-09-14T07:25:11+05:30 | Changed |
| 20260914060000_StoresWorkload | cb49ee6 2026-09-14T08:19:09+05:30 | Changed |
| 20260914070000_StoresQcStock | 1c847a8 2026-09-14T09:24:31+05:30 | Changed |

The pattern began in c3e4902 on 13 September and spread through 14 September: it was introduced during the three-day session, after the supplied first 78 migrations. The source inspection and exact diff audit preserve the distinction between the mistaken deployment denylist and legitimate test-harness isolation protections; no test-harness protections were removed.

## Whole-chain sweep

Reviewed all C# migration files, SQL helpers/resources and the generated full 0-to-102 script. The full script contains 102 migration-history insertions and zero occurrences of either SESS database spelling after this change. Other name-based refusals DO exist: they refuse only postgres/template0/template1. Those remain. Current-database ownership/ACL checks and command-context identity binding also remain; they are not arbitrary database-name denylists.

Evidence: local-evidence/overnight-20260914/database-name-guard-sweep-after.txt, generated-database-name-sweep.txt, approved-name-full-chain.sql and name-guard-change-audit.txt. No other production database-name denylist was found in that sweep. Legacy verification tools still protect live/source database names; those are test-time protections outside the deployable migration chain and were not changed.

## Retained execution and resume

local-evidence/overnight-20260914/name-guard-witness-state.json identifies the owned instance. Its directory retains restore.log, apply-78-to-102.sql, apply.log, apply-result.json and principal provision/status logs. globals-private.sql contains private role material and must not be committed or forwarded.

The existing migration-principal procedure in developer-runtime-principals.md remains applicable; no new environment setting is required. This witness executed the EF-generated SQL through psql as the migration login with SET ROLE owner, not a live dotnet ef command. Keep that distinction in the handoff. The user decides when to deploy.

Next: report 10's governed delivery prerequisite and audit dossier. The machine witness remains FAT-ready, not delivered; its pending delivery-accounting choice is recorded in item-15-delivered-machine-source-findings.md. Block A remains excluded.