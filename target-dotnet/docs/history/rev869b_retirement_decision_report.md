# REV869B retirement decision

Date: 6 September 2026

Scope: source and migration audit only. No database was changed and REV869B has not been removed.

## Decision

Retire the unprovisionable nine-role/control-plane topology, but retain the purchase workflow, business idempotency keys, immutable business histories, and the server-owned command-envelope idea. Move any retained command receipt/context evidence into the ordinary four-principal contract in a later additive migration. Do not leave the current 58 tests as a permanently failing opt-in suite.

For a 42-person manufacturer and comparable customer sites, an HTTPS lifecycle controller, two signing authorities, TLS pinning, nine target roles, separate purge/export operators, and a security engineer are disproportionate dependencies. More importantly, this implementation cannot currently be provisioned: no repository tool creates the target roles and the lifecycle administrator is required to be both CREATEDB/CREATEROLE and NOCREATEDB/NOCREATEROLE, with mutually exclusive memberships.

## Guarantees unique to REV869B

The ordinary model does not reproduce these guarantees:

1. Distinct real PostgreSQL session principals for application mutation and command-attempt evidence.
2. Nine-role separation of security ownership, runtime, command audit, management authorization, purge execution, purge audit, export, and verification.
3. Controller-issued target leases and immutable target identity bound to database name, instance/lease fingerprints, backend PID, transaction ID, and process execution ID.
4. Pre-mutation command registration, one active attempt, transaction-bound context, and exact semantic history claims in the five command request/attempt/context/claim/outcome tables.
5. A committed receipt in the same transaction as the business mutation, with separately recorded reject, rollback, abandonment, and commit outcomes.
6. Database rejection of controlled RFQ, invitation, quotation, technical-verification, comparison, PO, material-follow-up, vendor-qualification, approval-policy, and related history mutations unless exact command slots are present.
7. Immutable command receipts, outcomes, target identity, purge events, and export rows.
8. Bounded, expiring, independently evidenced purge batches with frozen candidates and retry lineage.
9. Field-minimized, bounded, expiring export batches with immutable row hashes and independently recorded release outcomes.
10. Catalogue/ACL fingerprints and an independent verifier detecting function, trigger, ownership, or privilege drift.
11. External lifecycle evidence signed by separate controller/audit keys and pinned to an HTTPS origin and TLS SPKI.
12. Quarantine/recovery/drop evidence for disposable targets, including interruption and concurrent-cleanup reconciliation.

These are defence-in-depth and operational-evidence guarantees. They are not the purchase business workflow itself.

## Ordinary-chain guarantees that survive

- Stores posting remains controlled. advance.post_stores_stock_batch is SECURITY DEFINER; ordinary provisioning removes direct runtime INSERT/UPDATE/DELETE on stock_posting_batches and stock_movements and grants runtime EXECUTE on the function.
- Stock posting batches and stock movements remain append-only; ordinary triggers require reversal postings.
- QC revisions/results/dispositions, provenance annotations, concessions, Stores status/approval history, role-assignment events, controlled-configuration history, customer-PO revisions/lines, memo-liability events, and master-import results retain ordinary-chain guards.
- Purchase quotation lines, technical verifications, purchase approval history, PO lines/history, and purchase status history retain immutable triggers installed by AdvanceDatabaseContractSql.
- Document idempotency keys, request fingerprints, unique constraints, optimistic versions, approval snapshots, no-self-approval, company scope, and resolved acting-role/assignment evidence remain business-schema features.

One correction is essential: **general audit_logs immutability does not survive today.** The unconditional UPDATE/DELETE trigger is installed only by the separate REV869B security package. The ordinary chain validates sensitive assignment evidence on INSERT but does not prohibit later mutation of every audit row. An additive ordinary migration must install an ordinary-owned immutable-audit trigger before retirement is complete.

Ordinary immutable purchase histories also do not reproduce exact transaction-bound command-slot authorization. They prevent later rewriting; they do not prove each mutation was authorized by a separately registered attempt.

## Source impact and counts

The repository-wide textual inventory contains 301 files mentioning REV869B, mostly archived reports and generated snapshots. The actionable inventory is:

- **1** separate security EF migration: 20260824120000_Rev869BSecurityPackage.
- **4** dedicated SQL implementations: command context, controlled mutation, database lifecycle, and database safety.
- **3** security-migration support files (project, DbContext, factory), plus that migration.
- **10** ControlPlane C#/project files.
- **5** control-plane SQL/operator scripts.
- **20** dedicated REV869B-named main test/support files, plus the ControlPlane architecture test and mixed integration tests.
- **2** REV869B-named purchase HTTP files. Their routes and behavior must be retained and renamed/reworked, not deleted. Two other endpoint files call the command authorizer and need rewriting.
- **4** seed files with references. Only Rev869BSeedData.cs is dedicated; three contain live purchase/configuration/page data and need selective renaming, not deletion.
- **4** product call-site files for command authorization: purchase service, tax/GST service, vendor-qualification endpoints, and the authorizer.

Generated EF designers/snapshots and applied business migrations must not be edited merely to erase a label. Deleting Rev869B-named domain/contracts/services would delete the live RFQ-to-PO product.

## Migration-history-safe removal

Removal does not require rewriting migration history. Leave applied main migrations and the separate security migration historical. Add a new ordinary migration which installs retained protections first, detects absent versus fully present security-package state and refuses partial state, removes REV triggers/functions/tables in dependency order when present, restores ordinary ownership/ACLs, and never drops RFQ-to-PO business tables or histories.

An owner database where the security migration was never applied takes a no-op cleanup branch. A guarded database takes the verified removal branch. No rebuild and no deletion from either migrations-history table is required.

## The 58 opt-in failures

They must not remain as unreachable expected failures:

- Rewrite command idempotency/receipt, immutable audit, purchase mutation, ACL, and ordinary-principal behavior cases against the supported four-principal model.
- Retain useful pure source/domain tests after renaming.
- Delete only controller lifecycle, leased-target, signed HTTPS evidence, REV-only purge/export, and nine-role topology scenarios after their source is removed.
- The default suite should finish with zero unexplained failures. Until the retirement commit reclassifies them, 58 remains the known baseline.

## Smaller design worth keeping

Keep a server-generated command envelope and business idempotency. An ordinary version can let nexa_erp_runtime call a narrow SECURITY DEFINER function owned by nexa_erp_owner. It can atomically register organization, operation, caller-key hash, request hash, employee, resolved role and assignment ID; record one terminal receipt with the mutation; and return it on replay. Runtime gets EXECUTE only and no direct receipt-ledger mutation.

This gives database-enforced atomic idempotency and attribution inside the ordinary trust boundary. It cannot claim independent-principal or externally signed lifecycle evidence.

## Recommendation

Enable the ordinary model now, then retire REV869B additively after ordinary immutable audit and compact command receipts are proven. Do not repair/deploy the current nine-role design at customer sites. An architecture that cannot be provisioned forces real work onto unguarded databases or permanent opt-outs and therefore weakens, rather than strengthens, the product.
