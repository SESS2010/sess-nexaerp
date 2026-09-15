# Migration name guard review - 15 September 2026

Subsequent decision: the user identified the SESS-name prohibition as misplaced test-time protection and explicitly authorized its removal, without the proposed approval mechanism. See [implemented change and exact-name proof](exact-name-migration-witness-2026-09-15.md). The review below records the earlier authorization boundary.

No database was connected to or migrated for this review. No migration, guard or design-time factory was changed. This is source inspection against retained restore evidence.

## Finding and authorization boundary

The observed refusal is an unconditional database-name prohibition. It is not a failed ownership, ACL, ExpectedDatabase or ledger-definition assertion. The name check deliberately runs before ledger inspection. The differently named restore proves data-state compatibility on that copy, not authorization to deploy to the protected name and not universal migration safety.

I cannot establish that this prohibition was an erroneous assumption rather than a deliberate deployment restriction. The source calls the target a protected owner database; the original Item 26 record explicitly lists protected-database checks. Therefore the user's condition for changing a guard has not been established. No guard change is made. There is no evidence here requiring a business-migration change either. The deployment policy, rather than a failed data precondition, is what remains unresolved.

Two distinct protections were previously conflated: PostgreSQL executed one name refusal at CommandReceiptReplay. Separately, automatic tool approval review rejected editing 13 guard builders. PostgreSQL did not execute and reject thirteen migrations.

## CommandReceiptReplay: actual assertions

Source: src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260913060000_CommandReceiptReplay.cs.

Both directions require the Npgsql provider. The SQL guard first requires server_version_num >= 170000 and current_database() outside postgres, template0, template1, sess_nexaerp and sess_nexa_erp. This file uses a case-sensitive comparison; the other twelve protected-name guards use lower(current_database()).

Next it requires advance.command_requests, advance.command_receipts and the exact register_command_request(text,text,bytea,bytea,uuid,text,text,text,uuid) signature to exist. Up refuses an existing read_command_receipt(uuid). Down requires that installed function's exact body, jsonb result, SECURITY DEFINER flag, owner matching command_receipts, fixed search path and no EXECUTE grantees beyond owner/runtime. These are separate checks; they were not the observed reason for the exact-name failure.

Up installs a receipt-reader function, revokes PUBLIC execution and, when the managed owner exists, sets that owner and grants execution to runtime while revoking bootstrap/migration execution. The function verifies the runtime login, lack of owner membership, restricted role attributes, exact registered command context and current actor authority before returning the stored response. Up does not rewrite business ledger rows. None of this establishes the safety of every later migration for arbitrary data.

The design-time factory checks ConnectionStrings__NexaErp's database against NexaErp__ExpectedDatabase using exact ordinal equality. It does not pass that environment value into this SQL guard. Setting ExpectedDatabase cannot override the literal denylist. Correct migration/owner roles cannot override it either.

## Thirteen builders: current checks and proposed scope

Every row below currently refuses both SESS spellings. All require PostgreSQL 17 or later (ForeignPaymentBankAdvice inherits that requirement and system-database refusal from VendorPaymentBillLockOrderSql.Guard). The proposed target assertion described below would replace only the SESS-name prohibition in every row, in both directions; all listed structural checks would remain. No replacement is implemented or certified.

| Migration | Other existing assertions retained under the proposal |
| --- | --- |
| 20260913060000_CommandReceiptReplay | Ledger tables/registration signature; reader absent on Up; exact reader definition, result, owner, search path and permitted EXECUTE grantees on Down. |
| 20260913070000_ForeignPaymentBankAdvice | Inherited exact payment function body, SECURITY DEFINER and search path; immutable source insertion anchor. This inherited guard does not itself assert the full owner/ACL contract. |
| 20260913080000_ForeignCurrencyFinancialReadModels | Vendor evidence table and exact payable/position function bodies, result type, stability, owner, search path and ACLs. |
| 20260913090000_GovernedVendorBankAdvice | SHA-256 availability; no partial installation; predecessor functions; installed evidence ownership/generated columns/immutability trigger and helper ACLs; rollback refuses retained documents. |
| 20260913095000_PurchaseOrderSupersedeHistory | History function owner, invoker/stability/search-path flags, exact retained-creator baseline and before/after patch markers; exact insertion anchor. |
| 20260913100000_VendorPoCashCap | Prior financial functions and bank-advice baseline; absent new helpers/index; exact helper/payment/advance/options definitions and ACLs; rollback trigger/index protection. |
| 20260914010000_PurchaseWorkload | No partial function/page; active PO permission baseline; installed function body, owner, stability, search path and ACLs; rollback refuses changed page permissions. |
| 20260914020000_PurchaseSpending | Same categories for spending, with active PO permission baseline. |
| 20260914030000_ReceiptQuantityAcrossPoRevisions | Exact predecessor GRN header/line function definitions, ownership, authority and attached trigger configuration. |
| 20260914040000_PurchaseObligations | Same projection checks for obligations, with active PO permission baseline. |
| 20260914050000_PurchaseOpenOrders | Same projection checks for open orders, with active PO permission baseline. |
| 20260914060000_StoresWorkload | Same projection checks for Stores workload, with active GRN/MIR permission baselines and owner matching gate_entries. |
| 20260914070000_StoresQcStock | Same projection checks for Stores QC stock, with active GRN permission baseline and owner matching goods_receipts. |

## What the proposed assertion would mean

Current: never migrate a database bearing either protected SESS name, regardless of actor, approval or schema state.

Proposed: still refuse system databases; for a protected SESS name, require an explicitly approved exact target, session_user=nexa_erp_migration, current_user=nexa_erp_owner, database ownership by that owner, owner NOLOGIN, migration LOGIN, and neither role having superuser/CREATEDB/CREATEROLE/REPLICATION/BYPASSRLS attributes. Retain version and every existing structural/definition/ACL/trigger check. The factory would also require the approval, connection database and ExpectedDatabase to match.

These are real, independently testable catalog and session checks. However, they are NOT logically equivalent to the current prohibition: they admit an approved case that is currently always denied. A custom session setting is an operator opt-in, not a signed authorization or protection from the database owner; database roles and privileges remain the security boundary. Calling this merely a stronger guard or a proven correction would be misleading. It is a deployment-policy change and remains a proposal in outputs/proposed-reviewed-migration-target.md.

Before any such change could be certified, it would need negative tests for missing/wrong target, wrong session actor, wrong current role, elevated attributes and system databases; unchanged structural guards; and a fresh exact-name 78-to-102 isolated witness followed by principal reconciliation. The user's current instruction is not to migrate, so none is run now.

## Resume

Resolve whether the deliberate protected-name prohibition should be replaced by an approved deployment policy. Do not rename the live database, skip migrations or alter migration history to evade it. Report 10 remains next after this question is settled; Block A remains excluded.