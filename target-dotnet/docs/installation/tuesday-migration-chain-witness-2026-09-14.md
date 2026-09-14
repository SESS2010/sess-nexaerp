# Tuesday migration chain - backup witness and deployment blocker

## Current answer: do not run the committed chain on sess_nexa_erp yet

The chain has a database-name blocker. At revision 433f968 (unchanged by gate commit 983a45f), the first refusal is 20260913060000_CommandReceiptReplay, the seventh pending migration after the supplied 78-applied state. Six preceding migration transactions may commit before that refusal. Do not skip the migration, edit migration history, or use the earlier unqualified morning instruction.

A proposal for explicitly authorizing the named target while preserving managed-role and function-authority checks is in outputs/proposed-reviewed-migration-target.md. Automatic approval review rejected applying that cross-cutting authorization change without explicit user approval. It remains a proposal; none of the 13 production guards has been changed. There is no verified replacement morning command yet.

## What the backup witness does prove

The supplied backup restored into a new isolated PostgreSQL cluster as advance_parser, on 127.0.0.1:56949. Its actual 78-row starting history upgraded through 22 pending migrations to 100, ending at 20260914080000_FifoReturnRestorations. Installer provision returned RECONCILED and status VERIFIED, both exit 0, with existing credentials unchanged. SQL execution took 0.8056528 seconds, excluding restore, generation, provisioning and status.

This proves compatibility with that data and ownership state under the disposable database name. It DOES NOT prove the sess_nexa_erp command, because the protected-name predicate differs. The earlier affirmative conclusion was overstated and has been withdrawn. Report-5 migrations remain stashed and are excluded.

## Source and isolation

- Backup: C:\Users\User\Desktop\before_tuesday.dump; custom format; 10,076,698 bytes; created 14 September at 19:53.
- SHA-256: 7601FC95F017B3C1104C4888430F82076ABD30594A83A7FEA9F44B760DB8C5A5.
- Globals: C:\Users\User\Desktop\globals_before_tuesday.sql. Source files remain unchanged.
- Initial history: 78 rows, ending 20260912064200_GovernedOpeningStockThreeActorCeremony.
- Restored purchase_orders and __EFMigrationsHistory owner: nexa_erp_owner.
- Script ran as nexa_erp_migration with SET ROLE nexa_erp_owner; ON_ERROR_STOP enabled; durable PostgreSQL settings retained.
- No connection to or restoration over the live database was made.

## Bootstrap test failure

The disposable fixture created managed roles but migrated as postgres. This produced mixed ownership: PurchaseWorkload's function belonged to nexa_erp_owner while purchase_orders belonged to postgres. Its production guard correctly refused. Running the fixture as the migration login acting as owner clears that disagreement.

The next guard exposed a second fixture omission: newly created functions retained PUBLIC EXECUTE. Receipt continuity correctly refused those permissions. The fixture now revokes default PUBLIC function execution for its owner. Targeted verification passed: Release 15/15 across the bootstrap, full migration up/down and purchase foundation checks; Debug 15/15. Both builds passed without warnings or errors. Neither production guard has been weakened. No production rows or frontend contracts change.

## Evidence

local-evidence/overnight-20260914/owner-backup-witness-state.json identifies the owned cluster. Its root retains starting-state.txt, pending-from-78.sql, pending-apply.log, pending-apply-result.json, principals-provision.log and principals-status.log. The private globals copy contains credential hashes and must not be committed or included in a public handoff.

After approval and implementation, repeat the isolated 78-state witness with the actual target-name predicate exercised, then provision/status. Report the final committed revision, count and tested operator command before morning handoff.
