# Purchase order cancellation history authorization

Status: implemented and verified in Release and Debug. This is a targeted cancellation fix, not a full Item 21 completion.

The open-order amendment witness reproduced HTTP 500 when the Technical Director cancelled an approved, unissued PO amendment. PostgreSQL rejected the history insert with SQLSTATE 42501 / rev869b_history_action_role: the API permits Technical Director or Managing Director, while the original history guard's generic PO branch permitted only Purchase Manager for this action.

Migration 20260914045000 adds exact cancellation authorization for those two director roles. It requires the cancelled parent, nonempty reason and cancellation timestamp, matching organization, actor login and transition correlation, and a parent updated in the same transaction. Existing actor, context and role-assignment checks remain. Purchase Manager is not authorized to write cancellation history through the generic branch. No historical financial or history records are rewritten.

Migration inspection checks the entire retained history body, function owner, invoker mode, search path, ACL and enabled history insertion triggers. Downgrade removes only this patch after the same inspection. A changed body or authority fails closed.

The historical cash revision test deliberately downgrades older migrations to reproduce the already witnessed legacy overpayment. The later cancellation patch must be removed and restored around that test-only downgrade: the older full-body guard is supposed to reject an unknown later patch. This adjustment is confined to the disposable test setup.

Verification covers: guarded install/reprovision/tamper refusal/down/reapply, actual director cancellation, Purchase Manager refusal, exact history/audit/request/receipt increments, unchanged stock and cash rows, and idempotent replay. Both configurations exercise these checks.

Release runtime evidence has reached the complete assertion set: PM refusal adds one denial audit (184 to 185) and no business rows; TD cancellation adds one PO history (27 to 28), one status history (93 to 94), one successful audit (176 to 177), one request and receipt (140 to 141). Total audits become 186. GRNs stay 3, FIFO layers 3, movements 28, advances 4, payments 2. Cancellation returns Version 4; replay returns the same result and all counts remain unchanged. Artifact: po-cancellation-atomicity-verified-release.json. Release runtime fact passed in 4m48.217s in item29-open-orders-revision-correction-release.trx (3 passed, zero failed/skipped, 11m36s overall). Migration fact passed in 31.330s in item29-open-orders-cancellation-release.trx. Debug passed all eight related facts (zero failed/skipped) in 19m18s, including the cancellation migration and runtime facts. The clean Debug build took 4m48.28s with zero warnings/errors. Every recorded cancellation row count matches Release; po-cancellation-atomicity-verified-debug.json is retained.
