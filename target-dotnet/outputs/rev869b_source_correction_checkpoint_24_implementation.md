# REV869B source Correction 24 implementation checkpoint

Date: 2026-08-15

## Authority and entry gate

- Authorized starting HEAD: `039eb850a14dfa5592dac0b3cbd7519d9d3a2f0d`.
- Authoritative reconciliation: `outputs/rev869b_correction24_allowlist_evidence_reconciliation.md`.
- Reconciliation SHA-256: `E4015519B459C80AB533B0E18CEBC1C02EDEB2A13E30CB83CE038EAE7AF65DFF` (exact match).
- Target-scoped status was clean before implementation.
- Reconciliation was read completely. Its mapping contains exactly 34 unique scenarios.
- Reconciliation states `frozen_architecture_state=RETAIN` and `acl_boundary_state=RETAIN`.
- PostgreSQL, provisioning, migration execution, lifecycle/drop/purge/recovery/quarantine/export execution, production, and `../legacy-reference/` were not accessed or run.

## Exact exhaustive scope

Exactly these eleven authorized files comprise this correction:

1. `tools/rev869b-control-plane-install.sql`
2. `tools/rev869b-control-plane-verify.sql`
3. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs`
4. `tests/SESS.NexaERP.Tests/Rev869BControlPlaneProvisioningContract.cs`
5. `tests/SESS.NexaERP.Tests/Rev869BPostgresBehaviorTests.cs`
6. `tests/SESS.NexaERP.Tests/Rev869BCorrection16SourceContractTests.cs`
7. `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs`
8. `tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs`
9. `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs`
10. `tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs`
11. `outputs/rev869b_source_correction_checkpoint_24_implementation.md`

No twelfth file was created, renamed, or modified.

## F23-01 implementation evidence

`rev869b_begin_drop` now resolves the normal-drop `registration_request_id` against the immediately authoritative immutable `DropAuthorized` event before it changes the lease. The resolved event must match the lease ID, target instance, cluster, TLS thumbprint, expected lifecycle version, authorization request identity, lifecycle principal, safe target database identity, target manifest/marker hashes, and control manifest hash. The lease must still be in the exact `DropAuthorized` pre-state and version. A missing or substituted binding raises SQLSTATE `42501` with constraint `rev869b_drop_authorization_event_binding`.

The normal attempt persists the authorization event's immutable evidence hash, while the newly appended transition event retains its independently computed transition evidence. Source contracts pin the query-before-update order and mutation-sensitive rejection of missing, substituted, stale, replayed, cross-instance, cross-lease, cross-cluster, cross-TLS, wrong-version, wrong-authorization, wrong-pre-state, and wrong-manifest registrations. Existing recovery flow and frozen ownership boundaries remain unchanged.

## F23-02 implementation evidence

The acceptance harness now stages every scenario through distinct prepare, before-read, action, after-read, durable-read, audit-read, cleanup, and cleanup-read operations. Fixture, run, lease, command, authorization, attempt, action-evidence, and all read identifiers are deterministically distinct and scenario-bound. Action receipts and audit observations use separate origins and signing material. The local evaluator derives the verdict from typed assertions over independently canonicalized observations; no database reader accepts a claimed PASS state.

New verifier-only readers provide structured, authoritative evidence:

- control plane: `rev869b_read_lifecycle_evidence(uuid,uuid,uuid,uuid)` and `rev869b_read_control_plane_acl_evidence()`;
- target plane: `read_command_evidence(uuid,uuid)`, `read_purge_evidence(uuid,uuid)`, `read_export_evidence(uuid,uuid,uuid)`, and `read_target_acl_evidence()`.

The readers use fixed search paths and verifier-only EXECUTE grants. The ACL readers cover owners, role attributes/membership, database/schema/table/sequence/function privileges, default privileges, runtime/admin/audit/purge/export/verifier principals, and `PUBLIC`. Export evidence exposes field keys, counts, and recomputable hashes without unrelated payload. Down SQL removes all four target readers. The verifier now emits stable catalogue mismatch identity instead of the P03 arithmetic sentinel.

Every typed assertion has a remove-assertion mutant. Every scenario additionally has mutants for action, before, after, durable, audit, cleanup, fabricated, duplicated, substituted, stale, and cross-instance evidence. Shared signatures, copied `1/1` acceptance values, P02/P03 sentinels, echoed labels, and self-declared PASS values are absent from the acceptance decision.

## Complete 34-scenario implementation matrix

All entries preserve the reconciliation's exact scenario name and objective formula. `Implemented` means the compiled scenario contract contains its own fixture/action/observation IDs, typed formula assertions, independent reads, cleanup proof, and mutation set. PostgreSQL acceptance remains pending external execution.

| ID | Objective formula implemented | Offline result |
|---|---|---|
| P01 | pin mismatches = 0; control fingerprint exact; target ACL delta empty; verifier result exact | Implemented |
| P02 | each pin mismatch = 1; allocated leases = 0; actions = 0; exact code/object | Implemented |
| P03 | seeded delta = reported delta; protected mutations = 0; cleanup fingerprint restored | Implemented |
| L01 | reserved events = 1; resume-same-attempt XOR authorized cleanup; duplicate attempts = 0 | Implemented |
| L02 | per boundary: started = 1; reconciled = 1; state Ready; one target and one role set | Implemented |
| L03 | cleanup requests = 2; one DropStarted, active drop, and physical drop; exact authorization event binding | Implemented |
| L04 | per boundary: one DropStarted and Finalized; physical drops at most 1; target/roles absent | Implemented |
| L05 | use/drop mutations = 0; exact-attempt quarantine outcome = 1; state Quarantined | Implemented |
| R01 | decision = 1; exact attempt/action consumed; recovery = 1; Finalized = 1; target/roles absent | Implemented |
| R02 | new attempts/events = 0; decision consumed once; state RecoveryAuthorized; all replay substitutions denied | Implemented |
| R03 | cleanup failure = 1; old decision rejected; fresh linked decision = 1/consumed once; Finalized = 1 | Implemented |
| C01 | business/history deltas exact; receipt = 1; committed outcome = 1; active attempts = 0 | Implemented |
| C02 | second business/history fingerprints unchanged; same receipt/response hash; receipts/outcomes = 1 | Implemented |
| C03 | changed digest differs; request/attempt/business-history deltas = 0; exact denial | Implemented |
| C04 | business/history/receipt deltas = 0; durable RolledBack outcome = 1; exact failpoint identity | Implemented |
| C05 | exact attempt opened; transaction rolled back; business/history/receipt delta = 0; RolledBack = 1 | Implemented |
| C06 | four distinct interruption attempts/evidence IDs; exactly one prescribed terminal outcome each | Implemented |
| C07 | starts = 2; started/active = 1; unrelated mutation = 0; exact concurrent denial | Implemented |
| C08 | each substituted binding rejected; context/receipt/business-history deltas = 0 | Implemented |
| G01 | each invalid authorization: attempts/candidates/events = 0; exact denial | Implemented |
| G02 | eligible/frozen/deleted = 0; exact ZeroRows event = 1 | Implemented |
| G03 | eligible N > 0; frozen/deleted = N; candidate hash exact; remaining = 0; success = 1; unrelated unchanged | Implemented |
| G04 | current hash differs from frozen; deleted = 0; context unchanged; failure event = 1 | Implemented |
| G05 | deleted = 0; context unchanged; independently durable failure event = 1 | Implemented |
| G06 | starts = 2; authorizations consumed = 1; executions at most 1; exact root/prior/scope/ordinal/outcome linkage; one active child | Implemented |
| E01 | exact allowed projection; count within maximum; canonical hash exact; excluded fields = 0; prepared event = 1 | Implemented |
| E02 | prepared rows/hash/count unchanged; independently proven later row absent from batch | Implemented |
| E03 | each invalid release: released rows/events = 0; prepared hash unchanged; exact denial | Implemented |
| E04 | first Interrupted; second ID distinct and prior-linked; active = 1; delivery success at most 1; batch unchanged | Implemented |
| A01 | observed = expected; both set differences empty across the complete ACL inventory | Implemented |
| A02 | every prohibited tuple denied; protected fingerprint unchanged; distinct durable evidence | Implemented |
| T01 | one lease; fixture prepared; one exact target; exact runtime/verifier roles; no admin credential; InUse; cleanup absence | Implemented |
| T02 | restarted instance distinct; surviving attempt reconciled; one DropStarted/Finalized; target/roles absent; cleanup evidence = 1 | Implemented |
| T03 | for every scenario, killed mutants equal required non-equivalent mutants across all semantic paths | Implemented; 23/23 contract tests pass |

Scenario discovery found exactly 34 facts and 34 unique IDs. The inventory produces 34 unique fixture IDs, 34 unique action operation IDs, 34 unique cleanup operation IDs, and 170 unique observation-read IDs. PostgreSQL scenario execution count is zero.

## Offline validation record

| Validation | Result |
|---|---|
| Build, `--no-restore` | PASS: 0 warnings, 0 errors |
| Focused Correction 24 source/contract/mutation tests | PASS: 23/23 |
| Focused REV869B non-PostgreSQL tests | PASS: 73/73 |
| Complete non-PostgreSQL suite | PASS: 447/447 |
| Explicit model/snapshot and retained-SQL parity contracts | PASS: 2/2 |
| Correction 24 PostgreSQL scenario discovery | PASS: exactly 34; 34 unique; executed 0 |
| PowerShell 5.1 AST | PASS: 24 files, 0 parse errors; helpers not executed |
| EF no-connect migration discovery | PASS: 13 migrations, inert `127.0.0.1:1` connection |
| REV869A/REV869B uniqueness/order | PASS: two migration artifacts each; class indices 11/12; adjacent |
| Offline Up SQL | PASS: 280057 bytes, 2399 lines, SHA-256 `52D0073BAF870D55D5AFAED01C19F00CAC93E14F496774ED232732EC622DEC62` |
| Offline Down SQL | PASS: 10600 bytes, 220 lines, SHA-256 `20BC1489BCA0555E9FCC7020367B31A06BAD30F9FD20689639A0D652E5479737` |
| Target evidence reader lifecycle | PASS: four creates and four Down drops; no transaction-control statements in no-transaction SQL |
| Secret/privacy/prohibited-operation scans | PASS: no literal secret/private key/password/cloud/privacy/legacy token and no client admin credential, mutating SQL, DDL, migration, provisioning-process, or lifecycle execution pattern |
| ACL/owner/default/`PUBLIC` contract scans | PASS: complete reader inventories; no `PUBLIC` reader EXECUTE grant |
| `git diff --check` | PASS |
| Exact eleven-file scope | PASS |

An initial compatibility compile identified two excluded-consumer API breaks; the allowlisted client restored the compatible allocation/release surface. Initial focused contracts then exposed two missing semantic checks; both were corrected inside the allowlist. The table records the final reruns.

## Content hashes before checkpoint commit

| File | SHA-256 |
|---|---|
| `tools/rev869b-control-plane-install.sql` | `143C26F324B5989EC51C9916478C0F85FE011857DC2CEA9BBA4C1064BE25603A` |
| `tools/rev869b-control-plane-verify.sql` | `53AF67A611EDCB5AE00FDCD2DE5F7012BDC22B6A85A019B7D3F54850315B0ED2` |
| `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs` | `575228AFEEB8533A7C11B49776BA66296A6AD8EA31A4BB1F741E34073C2BEA19` |
| `tests/SESS.NexaERP.Tests/Rev869BControlPlaneProvisioningContract.cs` | `B86B8AAB145906EE83B304835D9C68DAD406E5589E6B703CE67C0CFADC3C57A3` |
| `tests/SESS.NexaERP.Tests/Rev869BPostgresBehaviorTests.cs` | `C02BAD5A5E79B532D80C3AE5A9373FD3388111B554CC23E96093B170D9E5C56C` |
| `tests/SESS.NexaERP.Tests/Rev869BCorrection16SourceContractTests.cs` | `A1EBBD80E59E2073D81D6FC3189566BABFF723F0201162B62878FB10EDF74EAA` |
| `tests/SESS.NexaERP.Tests/Rev869BCorrection14PostgresDesignTests.cs` | `E1B818A10E67E2CF51DCC63E656C784FC6A24E75BF2ACCB67CA42A54711B03A8` |
| `tests/SESS.NexaERP.Tests/Rev869BCorrection17PostgresScenarios.cs` | `8F8F4923ED91B69C50324215F1CA6F7315C04596879661C9C978A1096A3AE4B0` |
| `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs` | `E149D75679AE02953459BEB99B21CFA6340C4A5EF86C512CCDF0C6E7D0D71C00` |
| `tests/SESS.NexaERP.Tests/Rev869BLifecycleControllerClient.cs` | `10BD0CB2CA1A1A6C9DB5EE82765A47B92C5B8C1692C1C9BF3493C58ADC4CC795` |

## Architecture, unresolved prerequisites, and next gate

The frozen architecture is retained: external provisioning, a dedicated lifecycle controller, a surviving control-plane database, and target-local transactional ledgers. Owners and verifier/runtime/admin/audit/purge/export boundaries remain closed; no ownership or privilege broadening was introduced.

Unavailable external prerequisites remain blocking for execution acceptance: an externally provisioned isolated PostgreSQL cluster; independently pinned source/manifest/TLS/cluster/signing identities; externally provisioned owner, lifecycle, runtime, admin, audit, purge, export, recovery, and verifier principals; controller endpoint/process and separate signing/audit trust roots; and explicit later authorization to provision, apply, and execute the 34 PostgreSQL scenarios. This checkpoint makes no database-acceptance, production-readiness, source-safety PASS, or helper-readiness PASS claim.

The single next gate is a separately authorized internal adversarial source-only precheck of this committed Correction 24. Only after that precheck may management authorize a fresh independent source-safety review.

correction_24_source_implementation_state=COMPLETE_PENDING_PRECHECK
correction_24_internal_precheck_state=NOT_RUN
frozen_architecture_state=RETAIN
acl_boundary_state=RETAIN
external_prerequisite_blocking_state=YES
rev869b_source_safety_state=FAIL
rev869b_execution_helper_readiness_state=FAIL
postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN
