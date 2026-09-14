# Overnight open questions â€” 14 September

These are source findings, not a new full-suite certification. The current priority is default test duration, then failures, then Item 15 and Item 16. Report 5 was restored after the baseline run; its original stash remains retained until verification and commit.

## Opening stock in two companies

`EfOpeningStockService.CompanyAsync` resolves the selected active company. Its reads and controlled commands use that company. `OpeningStockSql` requires a completed error-free import belonging to that same company and refuses opening if that company already has any stock movements. Authorizing Company A does not create Company B's opening, does not mark Company B complete and does not borrow balances across companies. B shows only its own ledger. B must complete its own opening ceremony before its first movement; otherwise the current opening command refuses it. There is no global requirement in the opening service that both companies finish together.

## Notification failure and port collision

`InAppNotificationWorker.RunOnce` catches exceptions from scope creation, processor resolution and refresh, logs them, and retries on the one-minute timer. A normal notification refresh failure does not escape into the default `StopHost` policy. A second API process cannot bind an occupied port and must fail startup; the existing API stays live. `HostFailureBehaviorTests.NotificationFailureRetriesAndPortCollisionDoesNotStopExistingHost` exercises the actual interval and health endpoint. The default StopHost policy remains for an unhandled background-service failure; changing it globally is not necessary to retry this worker's database failures.

## Fitted, reversed, then returned

The exact sequence without a re-fit now passes in both Release and Debug. Fit 0.30 at 1419.60, reverse it to zero machine quantity/material/charges/commercial actual, then physically return 0.30 to Stores. Machine values remain zero and the same BOM entries remain; FIFO restoration appends 0.30 against the original consumption, restoring 1416 base cost plus charges. Engineer custody retains the unrelated 0.05. Replay appends nothing further and original layers/consumptions are unchanged. Evidence: fifo-reversed-component-return.json; report5-repaired-release.trx and report5-supporting-debug.trx. The earlier re-fit witness is separate.

## REV869B leftovers

Removed the four requested obsolete project directories and their two inactive dependents, AcceptanceVerifier and ControlPlane.Tests: 20 tracked files across six directories (Persistence was empty). Active solution projects and tools reference none of them. The old group did reference itself, so the removal includes those dependent projects rather than leaving broken references. All targets were checked inside the workspace; no untracked files or reparse points were removed. See retired-rev869b-project-removal.md for post-removal validation and recoverable source revision.
## Purchase foundation guard failure

This failure is present in committed 433f968 independently of report 5. `Rev869BPurchaseFoundationTests.ServiceReusesPendingRfqAndPreventsDuplicateAndOverOrder` forbids Serializable anywhere in the purchase service. Commit 2b41e15 introduced Serializable specifically for IssuePO as part of the witnessed combined advance/bill-payment cash cap. The written record `item-11-import-purchase-progress.md`, Combined PO cash cap, documents why: an issued revision cannot reduce its value below already-paid cash, including concurrent cash changes. Ordinary purchase operations remain ReadCommitted; serialization failures in issuance become a concurrency conflict. Preserve the cash protection and replace the obsolete blanket prohibition with a check of the narrow IssuePO exception and refusal handling. This is a stale guard, not evidence that report 5 introduced serialization.

## Verification evidence

The user confirmed the earlier 905-test console run produced no TRX. Its six failures cannot be reconstructed from a nonexistent file. The current gated suite gives a new, independently retained failure list; it does not name the six historical failures retroactively.

Gate commit 983a45f preserves one canonical purchase flow and makes repeated workflow, historical migration, concurrency and deliberate failure witnesses opt-in. Final complete routine Release: 835 total, 832 passed, 3 failed, zero skipped, 22m10s process wall time. TRX: local-evidence/overnight-20260914/routine-final-release.trx. The timing target is met.

The three failures are ServiceReusesPendingRfqAndPreventsDuplicateAndOverOrder, AuthenticationBootstrapCeremonyCompletesExactlyOnceForSess12InBothCompanies, and GeneratedBusinessBaselineScriptsAreAcceptedByDisposablePostgreSql. Report 5 was stashed during this run.

The two bootstrap setups migrated as postgres after creating managed roles and omitted the owner's restrictive default function ACL. The corrected fixture runs as the migration login acting as owner and revokes default PUBLIC function execution. The focused Release bootstrap ceremony passed 1/1 in 1m06s, recorded in priority-2-bootstrap-acl-release.trx. Release verification passed all 15 targeted tests (bootstrap 1/1 and remaining 14/14); Debug passed 15/15 in 1m42s. Both builds passed with zero warnings/errors. TRX files: priority-2-bootstrap-acl-release.trx, priority-2-remaining-release.trx and priority-2-fixed-debug.trx. This is targeted verification, not a new full-suite certification. No production rows, frontend routes, fields or envelopes change. Governing records consulted: the final overnight instruction, Three_Day_Plan, Answers_To_Open_Questions and item-11-import-purchase-progress (combined cash cap).

## Separate morning deployment blocker

The restored 78-migration backup upgraded to 100 in the isolated database named advance_parser. That result does not prove the actual sess_nexa_erp command: 13 production guards explicitly refuse that name. The first blocker is CommandReceiptReplay, seventh pending after 78. The earlier affirmative morning conclusion has been withdrawn. Do not run the current chain on sess_nexa_erp yet.

The explicit-target authorization proposal is outputs/proposed-reviewed-migration-target.md. Automatic approval review rejected applying it without explicit approval. No production guards have changed and no live database was connected to. See tuesday-migration-chain-witness-2026-09-14.md for the scope and retained evidence.


Final routine verification after restoring and fixing report 5: Release 836/836, zero failed/skipped, 23m42s; report5-routine-release.trx. Targeted report-5 lifecycle/privacy/report checks passed Release 3/3 and Debug 3/3. Both default builds passed after cleanup. This supersedes the earlier three-failure baseline as current verification, without reconstructing the historical six console failures.
