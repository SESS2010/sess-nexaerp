# REV869B Option-A Phase-A Correction A1 Checkpoint

Date: 2026-08-16
Checkpoint type: source-only Correction A1 implementation handoff
Starting commit: `7051b8fa93b1605e35b98394f21479e820c8f18c`
Required starting parent: `ba37f6ac746bfd6eccaae571a0676f4b1f28b9ee`
Ending commit: the single commit containing this checkpoint; its exact immutable hash is obtained after commit creation and is part of the independent-review handoff.
Failure reconciliation: `outputs/rev869b_external_controller_phase_a_failure_reconciliation.md`
Failure reconciliation SHA-256: `C310E9F23985AD70AB64B6231DB5FF46199D0AE1321E3A05913DB0D5E6AC4234`
Authorized decision: `PHASE_A_CORRECTION_A1_GO`

The ending hash cannot be embedded in the commit that it hashes. This checkpoint therefore identifies the ending commit unambiguously as its own containing commit. No amend, rebase, reset, or other history rewrite is permitted to manufacture a self-reference.

## Scope and exact boundary

The exhaustive authorized allowlist is exactly:

1. `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs`
2. `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BCompatibilityManifestV1.cs`
3. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BExecutionBinding.cs`
4. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs`
5. `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs`
6. `src/SESS.NexaERP.ControlPlane/Configuration/ControlPlaneOptions.cs`
7. `src/SESS.NexaERP.ControlPlane/Program.cs`
8. `src/SESS.NexaERP.ControlPlane/Endpoints/ControllerContractEndpointsV1.cs`
9. `src/SESS.NexaERP.AcceptanceVerifier/Configuration/AcceptanceVerifierOptions.cs`
10. `src/SESS.NexaERP.AcceptanceVerifier/Program.cs`
11. `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs`
12. `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs`
13. `outputs/rev869b_external_controller_phase_a_checkpoint.md`

The changed subset is eight files: 1, 3, 4, 5, 9, 11, 12, and 13. Files 2, 6, 7, 8, and 10 required no artificial edit because their frozen versions/composition already satisfy A1. Boundary verification reported `allowlist_count=13`, `changed_subset_count=8`, and `outside_allowlist_count=0`.

The Git index-only boundary check continued to identify `../legacy-reference/` as untracked; its contents were not enumerated, read, copied, staged, or modified.

## Seven-finding correction map

| Finding | Corrected paths | Correction outcome |
|---|---|---|
| F-01 public typed ingress bypasses remain | 1, 5, 11, 12 | The only public protected command/verifier authorities accept raw canonical bytes plus authenticated transport identity. V1/V2 typed compatibility services and alternate verifier paths are internal. |
| F-02 fourteen responsibilities do not have exact owners | 1, 3, 5, 11, 12 | The exact 14-entry ownership catalog is authoritative; legacy parallel store, signer, registry, reader, and audit interfaces are non-public. |
| F-03 lifecycle transition and authorization contracts are incomplete | 1, 3, 4, 5, 12 | Exact operation/state/role/evidence/lease/resource/version rules, one-time authorization state, cancel/expire, quarantine, and export substates are enforced by the lifecycle-controller path. |
| F-04 verifier accepts caller-shaped or insufficiently bound evidence | 1, 5, 9, 11, 12 | Raw strict evidence, signed reader bundles, pinned reader/oracle metadata, scope/time/attempt/stage/watermark, hashes, limits, sensitive-field rejection, and immutable audit receipt are mandatory. |
| F-05 audit and readiness contracts are incomplete | 1, 9, 11, 12 | Readiness includes freshness and identity/version/policy facts, fails closed on missing/duplicate/stale/error/timeout/mismatch, and the shared V3 authority keeps both existing health routes at HTTP 503 unless ready. Audit binds before/after state/version, attempt, grant, lease/fence, transaction, key/version, and chain hash. |
| F-06 tests are not independently decisive | 12 | Twenty-seven independent A1 tests exercise production paths, exact failures/state outcomes, concurrency, four decisive mutants with zero survivors, ownership, raw-only ingress, evidence/oracle closure, readiness, audit, paging, whitespace, and Phase-B leakage. |
| F-07 checkpoint validation discrepancy | 13 | Historical trailing whitespace was removed; the original failure is disclosed below; incremental and cumulative tree checks pass without Git configuration suppression. |

No finding required Phase B, Correction 2, PostgreSQL behavior, external provisioning, deployment, live credentials, or production execution.

## Exact 14-owner matrix

| Production responsibility | Sole authoritative owner |
|---|---|
| NexaERP business runtime | `INexaErpBusinessRuntime` |
| Control Plane | `IControlPlaneAuthority` |
| Acceptance Verifier | `IAcceptanceVerifierAuthority` |
| Durable Control Plane persistence | `IDurableControlPlanePersistenceProvider` |
| Trusted issuer/key registry | `ITrustedIssuerKeyRegistryProvider` |
| KMS/HSM signing | `IKmsHsmSigningProvider` |
| Authoritative evidence reader | `IAuthoritativeEvidenceReaderProvider` |
| Immutable audit/evidence | `IImmutableAuditEvidenceProvider` |
| Lifecycle controller | `ILifecycleControllerAuthority` |
| Backup/recovery authority | `IBackupRecoveryAuthority` |
| Purge authorizer | `IPurgeAuthorizer` |
| Purge executor | `IPurgeExecutor` |
| Export authorizer | `IExportAuthorizer` |
| Export delivery executor | `IExportDeliveryExecutor` |

The owner test proves 14 distinct interface types and confirms that the former lease/idempotency/lifecycle stores, legacy issuer/signer, legacy reader, and V2 verification-audit interfaces are absent from exported production types.

## Frozen lifecycle effects

The exact 26 conceptual transition groups are:

1. prepare-authorize
2. prepare-start
3. prepare-complete
4. prepare-fail
5. execute-authorize
6. execute-start
7. execute-complete
8. execute-fail
9. verify-accept
10. verify-reject
11. quarantine
12. recover-authorize
13. recover-start
14. recover-complete
15. recover-fail
16. drop-authorize
17. drop
18. purge-authorize
19. purge-start
20. purge-complete
21. purge-fail
22. export-authorize
23. export-start
24. export-complete
25. authorization-cancel
26. authorization-expire

Every listed transition binds its exact trusted role, current/next/failure state, evidence set, authorization state, export substate, resource version, tenant/database/resource identity, and lease/fence when required. Unlisted combinations fail `STATE_TRANSITION_ILLEGAL`. Grants become `CONSUMED`, `CANCELLED`, or `EXPIRED` and cannot be reused. Cancel and expire change only authorization state. Quarantine is controller-owned, has no automatic exit, and cannot act on Purged. Export cannot skip or reuse `NONE/EXPIRED/FAILED -> AUTHORIZED -> DELIVERING -> DELIVERED`.

## A1 test and mutation inventory

The 27-test `ArchitectureFreezeContractTests` inventory covers:

- two public raw-only authorities and no typed protected bypass;
- non-canonical command and evidence mutations rejected before privileged delegates;
- exact 14 owners and no parallel public authority;
- trusted grant/policy/lease/reader facts not synthesized from requests;
- exact 26 conceptual lifecycle groups and all unlisted combinations illegal;
- every lifecycle binding dimension with unchanged state on rejection;
- export substate ordering/reuse, cancel/expire isolation, quarantine and Purged terminality;
- resource/holder/epoch/fence/expiry lease binding and one-time operation-bound grants;
- reader bundle signature/hash/scope recomputation;
- caller fact/action receipt/expected/PASS isolation;
- reader/global bounds using the stricter server-owned limit;
- pinned mutation-sensitive oracle with no reader-supplied verdict;
- missing, duplicate, exception, timeout, stale, version, identity, policy, and degraded readiness failures;
- exact immutable audit bindings and audit-failure suppression of success/verdict;
- concurrent duplicate ownership and changed-payload idempotency collision;
- opaque page-token scope/snapshot/prior-digest/expiry/limit binding;
- forbidden evidence/audit/readiness fields and sanitized diagnostics;
- deterministic decisive mutation manifest;
- reviewed-range whitespace/conflict-marker rejection;
- Phase-B implementation leakage rejection.

The decisive mutation manifest killed exactly four independent mutants: command scope substitution, evidence hash substitution, export-substate skip, and missing-readiness dependency. Survivors: exactly zero.

## Validation evidence

All commands were offline, used the restored dependency graph, and executed no helper, PostgreSQL scenario, migration operation, provisioning, network request, or production action.

| Validation | Exit/result |
|---|---|
| Phase-A test project warning-as-error build | exit 0; 4 projects; 0 warnings; 0 errors |
| Full solution warning-as-error build | exit 0; 5 projects; 0 warnings; 0 errors |
| Correction-A1 focused tests | exit 0; 27 passed, 0 failed, 0 skipped |
| All Phase-A tests | exit 0; 27 passed, 0 failed, 0 skipped |
| Focused REV869B non-PostgreSQL tests | exit 0; 76 passed, 0 failed, 0 skipped |
| Complete non-PostgreSQL suite | exit 0; 450 passed, 0 failed, 0 skipped |
| PostgreSQL scenario discovery only | exit 0; 34 discovered, 34 unique, 0 executed |
| Windows PowerShell 5.1 AST | exit 0; version 5.1.19041.6456; 24 scripts; 0 parse errors; 0 executed |
| EF migration discovery | exit 0; `--no-connect`; inert `127.0.0.1:1`; 13 listed; applied state unknown |
| REV869A/REV869B uniqueness and adjacency | exit 0; one each; ordinals 12 and 13; adjacent |
| Model/snapshot and offline SQL/hash contracts | exit 0; 3 passed, 0 failed, 0 skipped |
| Added-line safety scans | exit 0; 1,955 added lines; 0 hard-coded credential, private-key, database-action, process/network-client, protected-mutation-endpoint, or sensitive-logging hits |
| Exact 13-file boundary | exit 0; 13 allowed, 8 changed, 0 outside |
| Incremental working-tree diff check from starting commit | exit 0; no output |
| Cumulative working-tree diff check from `51476760...` | exit 0; no output |

The exact focused commands were:

```powershell
dotnet build .	estsSESS.NexaERP.ControlPlane.TestsSESS.NexaERP.ControlPlane.Tests.csproj --no-restore -warnaserror
dotnet test .	estsSESS.NexaERP.ControlPlane.TestsSESS.NexaERP.ControlPlane.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~ArchitectureFreezeContractTests" --logger "console;verbosity=minimal"
dotnet test .	estsSESS.NexaERP.TestsSESS.NexaERP.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~Rev869B&FullyQualifiedName!~Postgres" --logger "console;verbosity=minimal"
dotnet test .	estsSESS.NexaERP.TestsSESS.NexaERP.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName!~Postgres" --logger "console;verbosity=minimal"
```

### Historical and corrected diff-check evidence

Historical command:

```powershell
git diff --check 51476760adcea9ed7babbc04d642e53e371c6941..18a6458cbddf50e8cd45c9f789be2bdd2e859b08 -- .
```

Historical exit code: `2`.

Exact historical output, with each pair of trailing spaces rendered visibly as `␠␠`:

```text
target-dotnet/outputs/rev869b_external_controller_phase_a_checkpoint.md:3: trailing whitespace.
+Date: 2026-08-16␠␠
target-dotnet/outputs/rev869b_external_controller_phase_a_checkpoint.md:4: trailing whitespace.
+Checkpoint type: report-only handoff for the authorized source-only Phase A implementation␠␠
target-dotnet/outputs/rev869b_external_controller_phase_a_checkpoint.md:5: trailing whitespace.
+Architecture specification: `outputs/rev869b_external_controller_phase1_architecture_freeze_specification.md`␠␠
target-dotnet/outputs/rev869b_external_controller_phase_a_checkpoint.md:6: trailing whitespace.
+Architecture specification SHA-256: `3F0BC461865D69E3D9827D763D7C403E3BD4E82ECF488AE4FDF3E48D9722DDB8`␠␠
target-dotnet/outputs/rev869b_external_controller_phase_a_checkpoint.md:7: trailing whitespace.
+Entry HEAD: `51476760adcea9ed7babbc04d642e53e371c6941`␠␠
```

Root cause: the historical checkpoint was written after, or not included in, the earlier successful pre-check; its final Markdown hard-break spaces were never validated by the claimed exact-range command. The evidence only proves the final command was not enforced, not which process error caused it.

Corrected path: `outputs/rev869b_external_controller_phase_a_checkpoint.md`, historical lines 3-7.

Precommit equivalent commands over the exact final tree:

```powershell
git diff --check 7051b8fa93b1605e35b98394f21479e820c8f18c -- .
git diff --check 51476760adcea9ed7babbc04d642e53e371c6941 -- .
```

Both exited `0` with no diff-check output. After the single A1 commit, the mandatory immutable-range forms are `git diff --check HEAD^..HEAD -- .` and `git diff --check 51476760adcea9ed7babbc04d642e53e371c6941..HEAD -- .`; both must exit 0 before handoff.

## Source/test artifact SHA-256

| File | SHA-256 |
|---|---|
| `Rev869BControllerMessagesV1.cs` | `724954026987C92BD0AD3ABBF8D88542A2487F9EDB67C94BD2C641EA910A0EAC` |
| `Rev869BCompatibilityManifestV1.cs` | `8EC77B0642EE84B5F8EE0EB869A3C1B866DF3394FD910CC1B23693A83CA0FE42` |
| `Rev869BExecutionBinding.cs` | `0CC3019DEC10E82F0A952FE0CA931B5CA7C270138FC7090BA81C03D5112875A4` |
| `Rev869BControllerStateMachine.cs` | `1F745C3B5147FE2E7C98F583D64EAA70ECFB3B30A93AB7959C3BD287CC8DC131` |
| `SignedEnvelopeService.cs` | `45049A40D5946A794292DF0EC74F9F1CF6D5E4D44B0556E80E5A278B28795737` |
| `ControlPlaneOptions.cs` | `AF068630FB585E925223A8927DB9D06F5204BC97184F2FB5CF78EEC31EBDCA37` |
| `ControlPlane/Program.cs` | `782F4EA9394D4373DCBBCA9799DE57D454C6485A3EC7BF3DAABDDBB0A82C1777` |
| `ControllerContractEndpointsV1.cs` | `24AE2963F76E3CD2202585E53ABEAA3CDC0E3655A57CFF0B22EF320EEF88EB53` |
| `AcceptanceVerifierOptions.cs` | `90688F42AD2E0CF6ECBF061B096B0661626B6BE197D5B287F9A1FB8B6C9FE33D` |
| `AcceptanceVerifier/Program.cs` | `39C98D2B5938EB2CB7268A01AD7FCD58ED9A03386AC3985B96163B6762ACFECB` |
| `ClosedEvidenceVerifierV1.cs` | `822A2B43801668DE5DD38E3827CFB7FC02304EEEBF02E1EA498ADE8AA00261BB` |
| `ArchitectureFreezeContractTests.cs` | `ABAD99F4F5D2DBBFC4BD7DF55C102C53BA76AECC7F0AC76ED50D3C58AF62546F` |

## Architecture preservation and exclusions

The frozen Option-A split remains intact: NexaERP business runtime does not own lifecycle authority; the Control Plane owns raw command acceptance and delegates lifecycle changes only to `ILifecycleControllerAuthority`; the Acceptance Verifier owns raw evidence verification but cannot mutate lifecycle state. Trusted issuer/key, KMS, durable transaction, reader, oracle, immutable audit, recovery, purge, and export boundaries remain interfaces with deterministic offline fakes only.

No durable atomic implementation, PostgreSQL behavior, migration operation, database connection/action, external reader, real KMS/HSM, provisioning, deployment, production credential, network call, lifecycle/quarantine/recovery/drop/purge/export execution, Phase B, Correction 2, or Correction 29 was introduced or performed.

External prerequisites remain blocking: separately deployed Control Plane and Acceptance Verifier; HA durable control storage and transactional outbox; real workload identity/IAM/private network; independent issuer/policy stores; non-exportable KMS/HSM keys and rotation/revocation; authoritative least-privilege readers; pinned oracle artifacts; WORM audit/evidence storage; isolated PostgreSQL behavioral/concurrency/rollback/restart/PITR evidence; backup/restore/DR; scale/load/chaos qualification; monitoring, runbooks, training, and production approval.

## Canonical stop states

`phase_a_correction_a1_source_implementation_state=COMPLETE_PENDING_INDEPENDENT_REVIEW_OR_BLOCKED`

`phase_a_management_acceptance_state=NOT_APPROVED_PENDING_REVIEW`

`phase_b_source_only_gate=NO_GO`

`phase1_correction2_source_only_gate=NO_GO`

`rev869b_source_safety_state=FAIL`

`rev869b_execution_helper_readiness_state=FAIL`

`external_provisioning_state=NOT_STARTED`

`postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN`

`production_readiness_state=NOT_READY`

The sole next gate is a fresh independent source-only architecture and security review of the exact A1 commit. Do not automatically reconcile or correct any new finding.
