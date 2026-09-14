# Overnight open questions — 14 September

These are source findings, not a new full-suite certification. The current priority is default test duration, then failures, then Item 15 and Item 16. The report-5 work is preserved in the named stash `Preserve restored report 5 work before isolated witness gating` while the baseline is measured.

## Opening stock in two companies

`EfOpeningStockService.CompanyAsync` resolves the selected active company. Its reads and controlled commands use that company. `OpeningStockSql` requires a completed error-free import belonging to that same company and refuses opening if that company already has any stock movements. Authorizing Company A does not create Company B's opening, does not mark Company B complete and does not borrow balances across companies. B shows only its own ledger. B must complete its own opening ceremony before its first movement; otherwise the current opening command refuses it. There is no global requirement in the opening service that both companies finish together.

## Notification failure and port collision

`InAppNotificationWorker.RunOnce` catches exceptions from scope creation, processor resolution and refresh, logs them, and retries on the one-minute timer. A normal notification refresh failure does not escape into the default `StopHost` policy. A second API process cannot bind an occupied port and must fail startup; the existing API stays live. `HostFailureBehaviorTests.NotificationFailureRetriesAndPortCollisionDoesNotStopExistingHost` exercises the actual interval and health endpoint. The default StopHost policy remains for an unhandled background-service failure; changing it globally is not necessary to retry this worker's database failures.

## Fitted, reversed, then returned

The committed partial-return witness verifies 0.30 fitment at 1419.60, reversal to zero, re-fit 0.20 at 946.40, then physical return 0.10 restoring 472 base FIFO cost plus applicable charges while the retained machine stays 946.40. Original consumption/layer rows remain unchanged. That does not yet prove the specifically requested sequence with NO re-fit between reversal and full return. Add that exact sequence and assert zero machine quantity/material/charges/variance actual after both operations, a single physical/FIFO restoration, and unchanged original ledger rows. Do not count the partial-return witness as this acceptance.

## REV869B leftovers

The current solution lists only Api, Application, Domain, Infrastructure, Installer and the main Tests project. No active-project or tools reference to the four named obsolete projects was found in the source/reference scan. A separate `tests/SESS.NexaERP.ControlPlane.Tests` project DOES reference ControlPlane and ControlPlane.Contracts and contains old source-contract tests. Therefore removal must account for that dependent orphan test project; the statement that nothing references the old code is not literally true. Removal has not happened yet.

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
