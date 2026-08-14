# REV869B Source Correction Checkpoint 18

Date: 2026-08-14
Authoritative starting HEAD: c2d5fb208cf7657ab817a1f5ca651f9a2c5ba7a4
Reviewed Correction 17 source commit: ff177328b341c535059d0fdfb49e6733335b7a03
Authoritative review SHA-256: 90CF20CBEF35E4B7AC02E4628FD106D5DD3256A0F262CF6B5027A367566F8EE9
Required subject: Correct REV869B control-plane safety checkpoint 18
Ending commit: the commit containing this checkpoint.

## Entry gate and restrictions

The required HEAD, reviewed parent, subjects, review hash, clean target status and migration order matched before editing. The sibling ../legacy-reference/ remained untracked and was not enumerated, read or modified. No PostgreSQL connection was opened; no PostgreSQL test, provisioning mode, migration application, database/role operation, purge, recovery, export or business action was executed.

This checkpoint records source changes and offline evidence only. It does not self-adjudicate the independent safety verdict. The only next gate is a fresh independent source-only rereview.

## Correction matrix

| Finding | Source correction |
|---|---|
| C17-N01 provisioning | Added an immutable read-only preflight bound to externally approved system identifier, server address/port, administrator, source commit, execution ID and aggregate manifest SHA-256. ProvisionAuthorized invokes it immediately before bootstrap. Bootstrap repeats cluster identity and unconditional role/database creation makes any pre-existing package identity fail closed. Added exact cluster-guarded database, membership and role deprovisioning after schema rollback. |
| C17-N02 lifecycle/recovery | Removed caller grants on the generic transition function. Creation, drop, outcome, recovery and finalization wrappers enforce purpose-specific caller/state/policy/time/binding checks. Drop initiation validates every supplied lease field. Recovery binds request issuer/authority, management approval authority, executor, target and nonce; pre-create recovery is legal. Recovery attempts are immutable start records with a separate unique terminal outcome. Successful ordinary and recovery drops invoke an evidence-bound Dropped-to-Finalized function through the isolated audit writer. |
| C17-N03 purge | Split ConsumerPrincipal and ExecutorPrincipal, making valid audit-writer begin reachable while keeping destructive execution on the executor. Begin and execution use separate role-specific connections; destructive deletion and terminal evidence remain one transaction, so audit failure or caller rollback also rolls back deletion and leaves Started work retryable. |
| C17-N04 command attempts | A real external idempotency key is hashed and bound. One attempt per grant and one database/idempotency fingerprint are enforced without the identity column defeating uniqueness. Open and terminal functions require the exact attempt ID. Slot terminal evidence and immutable attempt outcome are appended by one function/transaction. Committed evidence is staged inside the business transaction before commit; rollback evidence is one issuer call. A read-only exact outcome reconciliation function is present. All direct consumers now retain and terminalize the attempt handle. |
| C17-N05 ACL/export | Authority rotation revokes every old issuer/runtime function grant before installing the new pair. Direct generic lifecycle transition access is absent. Export authorization is consumed and audited before the distinct reader connection can receive rows; the reader function is read-only and requires already-durable consumed/audit evidence. |
| C17-N06 PostgreSQL scenarios | The 25 future scenarios remain compiled and discoverable and directly affected fixtures now use exact attempt/idempotency signatures, the corrected purge principal split, audit-writer lifecycle outcomes and Finalized recovery path. They were not executed and are not claimed as behavioral evidence. |

## Provisioning artifacts

Final GeneratePlanOnly evidence reported PostgreSqlAccessed=false, ContainsCredential=false, host fingerprint 590F83C6021931B4C7C95BF5BB5B02FEAFB0B9E2ED6CEF999A061746E5998C0B and manifest SHA-256 667C6D0DE84521DF184D3F69BD23EB674B1EEA9278A3DCE34CEE6273C594B583.

| Artifact | Bytes | SHA-256 |
|---|---:|---|
| Preflight | 1,223 | ECC120DA942731076C882E2C1D91BFC9668CF252A685F78F30025C2204E7D800 |
| Bootstrap | 6,521 | 4B754AE160596322CD7AE850A7FEFB924613A46E8A0160A114954D527C52551E |
| Install | 41,286 | 7A6F1B7BA7971698124525F5BDA716D2F65CBC9E7FC18FD744D00881D9A9F334 |
| Verify | 4,186 | D382803C7866208D95D5C4784DB4DB21E99B8CF98723DEE74C7A30AC7701FA57 |
| Rollback | 2,596 | 9FEA89C631D28BEE235F8AC2A0F0BA6C1E50632660FB807220FB58EB720DBBBF |
| Deprovision | 1,586 | 665D37DE9CADE7DF281CC98023500AA6F6B1731A11FFB8F6DA3DCD8DCCB0031F |

The installed control-plane source contains five tables and fourteen functions. GeneratePlanOnly returns before database invocation. Database modes require the external bindings and exact computed manifest hash.

## Offline validation

| Gate | Result |
|---|---|
| dotnet build SESS.NexaERP.slnx --no-restore | completed; 0 warnings, 0 errors |
| Complete suite excluding Postgres names | 453 passed, 0 failed, 0 skipped |
| Focused offline migration and Correction 17 contracts | 9 passed |
| PowerShell parser | 0 parse errors |
| GeneratePlanOnly | completed without PostgreSQL access |
| REV869B PostgreSQL scenario discovery | 25 corrected scenarios listed; retained 18 direct plus 7 application scenarios remain compiled; 0 executed |
| git diff --check | completed without whitespace errors |
| Stale command SQL signature scan | no production/source occurrence of the removed split terminal function or old signatures |

## Prohibited operations and next gate

No PostgreSQL or external service was accessed. No database, role, schema, migration, purge, export, recovery or application mutation was performed. No generated SQL was applied. The legacy-reference sibling remained untouched.

Exact next gate: commit this controlled source/checkpoint change and request a fresh independent source-only safety rereview. PostgreSQL execution remains unauthorized.
