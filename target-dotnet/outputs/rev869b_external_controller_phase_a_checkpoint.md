# REV869B Option-A Phase-A Correction A3 checkpoint

Date: 2026-08-17
Checkpoint type: bounded source-only implementation handoff pending independent review
Authorization: `PHASE_A_CORRECTION_A3_SOURCE_ONLY_GATE=APPROVED`
Starting HEAD: `8c78f6a480fcbf86afbf9f5460598ece5b8d6732`
Starting parent: `ef38eeb58a03cdf76a19320832f7194b468b70d5`
Ending commit: the single Correction-A3 commit containing this checkpoint. A commit cannot contain its own SHA-1 without changing that SHA-1; the exact authoritative ending identifier is the post-commit `git rev-parse HEAD` reported in the final handoff.
Authoritative reconciliation: `outputs/rev869b_external_controller_phase_a_correction_a2_failure_reconciliation.md`
Reconciliation SHA-256: `D0D578542A7183EAEF87E77C9ED98F06406493C8061D4FD02C5247027B7A9F64`
Entry-blocker SHA-256: `BA7EE5E76AB7A95BA96300FEEC4535E78483449B617E6C1FF8E1FF24C873041B`

## Verdict and boundary

`phase_a_correction_a3_source_implementation_state=COMPLETE_PENDING_INDEPENDENT_REVIEW`

Exactly the ten authorized files below changed. The duplicate-entry blocker and all historical reports/commits remain immutable. No file in `../legacy-reference/` was accessed or modified.

1. `src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs`
2. `src/SESS.NexaERP.ControlPlane/Configuration/ControlPlaneOptions.cs`
3. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BExecutionBinding.cs`
4. `src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs`
5. `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs`
6. `src/SESS.NexaERP.AcceptanceVerifier/Configuration/AcceptanceVerifierOptions.cs`
7. `src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs`
8. `tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs`
9. `tests/SESS.NexaERP.Tests/Rev869BCorrection17SourceContractTests.cs`
10. `outputs/rev869b_external_controller_phase_a_checkpoint.md`

## Finding closure and preservation

| Finding | Source-only result pending independent review |
|---|---|
| F02 ownership/provenance | Closed by one non-decomposable `IDurableControlPlanePersistenceProvider` surface, one snapshot read, one atomic mutation, server-pinned provider/controller identity, semantic version, artifact and readiness-policy version, and rejection before any owner call. |
| F03 authorization/atomicity | Closed by separated current execution authorization and immutable stored management grant. Exact issuer/key/version/signature, authorization/original authorizer, operation/executor, tenant/organization/database/resource/version, plan/evidence, lease/epoch/fence, policy row/artifact, time window and one-time state comparisons precede lifecycle and atomic persistence. Replay returns only the original grant-bound result. |
| F04 reader/oracle closure | Closed by server-owned exact reader descriptor multiset. Resolution uses only server selection; exact service/source identity, schema/stage, artifact, version/compatibility, downgrade and revocation fields are checked before read/oracle. Caller metadata is comparison-only after the selected reader runs; oracle selection remains independent. |
| F06 independent assurance | Closed by 16 literal A3 tests, matrix assertions inside those methods, raw production traces, four compiled/killed A3 production mutants and four killed A2 regression mutants. |
| F07 checkpoint integrity | Closed by this A3 checkpoint, machine-consumed SQL evidence, explicit unique/raw count arithmetic, exact commands, hashes, boundaries and retained states. |
| F01 preservation | Raw-only ingress, canonical envelope/signature/nonce/freshness/idempotency/scope/lease/fence checks, non-public typed services, and health/version-only host surfaces remain intact. |
| F05 preservation | Frozen readiness, audit receipt, privacy/minimization, freshness, fail-closed 503 behavior and immutable evidence requirements remain covered by the passing regression suite. |

## Effective-owner closure: 14/14

The literal catalog has 14 entries, 14 distinct owner interfaces, and the executable validator proves constructor graph, capability surface, trusted descriptor and denial traces:

1. Nexa ERP business runtime
2. Control Plane
3. Acceptance Verifier
4. Durable control-plane persistence
5. Trusted issuer key registry
6. KMS/HSM signing
7. Authoritative evidence reader
8. Immutable audit evidence
9. Lifecycle controller
10. Backup/recovery authority
11. Purge authorizer
12. Purge executor
13. Export authorizer
14. Export delivery executor

The five formerly open effective owners—Control Plane, Acceptance Verifier, durable persistence, authoritative reader and lifecycle controller—now have one effective constructor-graph owner each. Public partial nonce/idempotency/lease/state mutation facets do not exist and cannot be cast or injected.

## Exact A3 tests

All 16 required literal `[Fact]` methods were discovered and passed:

1. `A3_CompositeProviderHasOnePinnedOwnerAndOneAtomicMutationCapability`
2. `A3_ExportedOrInjectablePartialNonceIdempotencyLeaseAndStateMutationIsImpossible`
3. `A3_SelfAttestedProviderOrLifecycleIdentityVersionArtifactIsRejectedBeforeSnapshotUse`
4. `A3_All14ResponsibilitiesHaveOneCatalogOwnerAndOneEffectiveOwner`
5. `A3_AuthorizeThenConsumeExactGrantAndApprovedPlanThroughRawProductionPath`
6. `A3_ExactCompletedReplayReturnsOnlyOriginalGrantBoundOutcome`
7. `A3_EveryStoredGrantIssuerActorPolicyTenantPlanVersionEvidenceLeaseFenceAndExpirySubstitutionFailsBeforeLifecycle`
8. `A3_MissingDuplicateConsumedStaleOrAmbiguousGrantFailsClosedWithoutAtomicCall`
9. `A3_CallerSnapshotGrantAndApprovedPlanClaimsRemainComparisonOnly`
10. `A3_ServerPinnedReaderIdentityVersionArtifactSetSelectsEveryReaderExactlyOnce`
11. `A3_OracleReceivesOnlyFactsFromServerSelectedReaders`
12. `A3_CallerSelectedReaderVersionUpgradeDowngradeArtifactOrSchemaNeverSelectsReader`
13. `A3_MissingDuplicateUnexpectedRevokedOrStalePinnedReaderFailsBeforeReadAndOracle`
14. `A3_CanonicalOfflineSqlGenerationIsStableAcrossTwoFreshProcesses`
15. `A3_CheckpointSqlEvidenceMatchesMachineCapturedCanonicalResultExactly`
16. `A3_WrongMigrationEndpointOptionInputHashNewlineEncodingSizeOrSqlHashFailsEvidenceGate`

Result: `16 passed; 0 failed; 0 skipped`. Looped substitution matrices remain inside the literal methods and do not inflate unique counts.

## Production-mutant evidence

All mutations were made only in isolated disposable copies, compiled, killed by their intended assertion, and removed. One preliminary M04 attempt lacking Git metadata was corrected and rerun; it is not part of the valid campaign.

| ID | Mutated production SHA-256 | Intended enforcement and result |
|---|---|---|
| A3-M01-PARTIAL-MUTATION-API-BYPASS | `F8142A5599903C63A44F850D492388728D09246151FD6097DF9A089A85A0109A` | Exported/injectable partial mutation capability; killed by A3 test 2. |
| A3-M02-AUTHORIZATION-PROVIDER-SUBSTITUTION | `3F668217A5A82A75A231A3E37F88EE3F30749079AB06ED5EC118FF39785B25BF` | Provider substitution; killed by the stored-grant exact-comparison matrix. |
| A3-M03-CALLER-READER-VERSION-DOWNGRADE | `EBDF15EF56BA591CC6ED8EAC2215C76447546BD7A603B19A8DC749D0285020FB` | Caller reader downgrade selection; killed before authoritative read/oracle. |
| A3-M04-SQL-EVIDENCE-PRODUCTION-DRIFT | `0069AD7667B2AF9ED3EA357BA6D9C8B093FFA6B571424A9B9D3F7418BB844050` | Copied migration installation operation removed; killed by the machine checkpoint evidence gate. |

`A3_mutants_total=4`
`A3_mutants_compiled=4`
`A3_mutants_killed_by_intended_assertion=4`
`A3_mutants_survived=0`
`A3_mutants_invalid=0`

The original A2 request-as-authority, lifecycle-version-gate, reader-cardinality and readiness-freshness production mutants were rerun and killed:

`A2_regression_mutants_total=4`
`A2_regression_mutants_killed=4`
`A2_regression_mutants_survived=0`

## Canonical offline SQL evidence

The procedure runs in the authorized ERP source-contract test with `IMigrator.GenerateScript`, `MigrationsSqlGenerationOptions.Default`, inert `127.0.0.1:1`, pooling disabled, and an EF connection interceptor. It generates REV869A→REV869B and REV869B→REV869A in memory, normalizes CRLF and lone CR to LF only, performs no trim/format/rewrite, encodes UTF-8 without BOM, and records bytes, LF count and uppercase SHA-256. Two fresh worker processes returned byte-identical JSON. Temporary JSON and disposable mutant directories were deleted.

The evidence `Commit` is the exact authorized source baseline. This avoids an impossible self-referential final-commit hash; each actual SQL input is independently bound below by SHA-256, and the final commit is supplied by the post-commit handoff.

Commands and environment:

```powershell
$env:ConnectionStrings__NexaErp = "Host=127.0.0.1;Port=1;Database=rev869b_no_connect;Username=no_connect;Timeout=1;Pooling=false"
$env:NexaErp__ExpectedDatabase = "rev869b_no_connect"
dotnet ef migrations list --no-connect --project src/SESS.NexaERP.Infrastructure/SESS.NexaERP.Infrastructure.csproj --startup-project src/SESS.NexaERP.Api/SESS.NexaERP.Api.csproj --context NexaErpDbContext
dotnet test tests/SESS.NexaERP.Tests/SESS.NexaERP.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~Rev869BCorrection17SourceContractTests.A3_CanonicalOfflineSqlGenerationIsStableAcrossTwoFreshProcesses|FullyQualifiedName~Rev869BCorrection17SourceContractTests.A3_CheckpointSqlEvidenceMatchesMachineCapturedCanonicalResultExactly|FullyQualifiedName~Rev869BCorrection17SourceContractTests.A3_WrongMigrationEndpointOptionInputHashNewlineEncodingSizeOrSqlHashFailsEvidenceGate"
```

A3_CANONICAL_SQL_EVIDENCE_JSON_BEGIN
{"Commit":"8c78f6a480fcbf86afbf9f5460598ece5b8d6732","SdkVersion":"10.0.303","RuntimeVersion":".NET 10.0.11","EfCliVersion":"Entity Framework Core .NET Command-line Tools\r\n10.0.10","EfCoreVersion":"10.0.10.0","NpgsqlVersion":"10.0.3.0","OperatingSystem":"Microsoft Windows 10.0.19045","Culture":"en-US","ConnectionString":"Host=127.0.0.1;Port=1;Database=rev869b_no_connect;Username=no_connect;Timeout=1;Pooling=false","UpFrom":"20260810120000_Rev869AIdentityMasterScopeFoundation","UpTo":"20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation","DownFrom":"20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation","DownTo":"20260810120000_Rev869AIdentityMasterScopeFoundation","GenerationOptions":"Default","NewlineRule":"CRLF and lone CR to LF only; no trim, format, rewrite, or execute","EncodingRule":"UTF-8 without BOM","SourceHashes":{"src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260810120000_Rev869AIdentityMasterScopeFoundation.cs":"245F42879EA50AF436DA3CB56B0A490D50D87C21C7CE6C16E3EEC689A29F4A2A","src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260810120000_Rev869AIdentityMasterScopeFoundation.Designer.cs":"98FBA38754F3F231F907DF1D281A761F77EE9F8AA1FC5ACDB5064FDAED0A0478","src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.cs":"20864B2FF2736FA9AB4F011E41CCC2CCD1157951BF0F97FAEB5683603A1E69CD","src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.Designer.cs":"26535D40537D7910D61EFFCD310CDD90D59FBEBF8323103A5DA15621DC1DB5B8","src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.cs":"527487DC61DAA334227CD61A55CA094FF4754E182320432E6BBD21AB7B356BE1","src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.Rev869A.cs":"81CAB9D245BC3A7DD3C1829B381D8EB2CE98CB2E267BFA6D95F6419D420F66E3","src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.Rev869B.cs":"3E3377A8212333E801064E13C4D76024D8D1C7739B6C489C8409AFC8B4D469EF","src/SESS.NexaERP.Infrastructure/Persistence/Migrations/NexaErpDbContextModelSnapshot.cs":"6048F29765A91790A76657A73E6806463356B45722BE6E19F0697859DA210F4C","src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BCommandContextSql.cs":"C83A8371FEAD3E04171C3AAD4501945130086AF663F26E0A3ACA3686F33800BB"},"UpByteCount":323960,"UpLfCount":2634,"UpSha256":"55FBFAF35B6FB4233B71F018C81149703BEE898791168590ADFE008AC8BDE22C","DownByteCount":11527,"DownLfCount":230,"DownSha256":"39A250AA07136FCF5B50E80E59EA0623B5B4E91D1CEEF6252E53AEDE5A0E5778","ConnectionOpenCount":0,"MigrationApplyCount":0}
A3_CANONICAL_SQL_EVIDENCE_JSON_END

## Validation and test arithmetic

| Gate | Result |
|---|---|
| Control-plane warning-as-error build | 0 warnings; 0 errors |
| Complete solution warning-as-error build | 0 warnings; 0 errors |
| A3 architecture subset | 13 passed; 0 failed; 0 skipped |
| Complete Phase-A control assembly | 63 passed; 0 failed; 0 skipped |
| Focused REV869B ERP subset | 79 passed; 0 failed; 0 skipped |
| Complete ERP non-PostgreSQL assembly | 453 passed; 0 failed; 0 skipped |
| Canonical A3 ERP subset | 3 passed; 0 failed; 0 skipped |
| PowerShell 5.1 AST | 24 files; 0 parse errors; no scripts executed |
| EF no-connect discovery | 13 migrations; REV869A and REV869B unique, adjacent, positions 12 and 13; applied status intentionally unknown |
| Source/model/snapshot parity | Passing focused and full source-contract/model-differ coverage |
| SQL process equality | 2 fresh processes; exact JSON/byte equality |
| Database connection/application counters | 0 opens; 0 migration applications |
| PostgreSQL discovery/execution | 87 unique discovered; 0 executed |
| Incremental/cumulative diff checks | exit 0 / exit 0 |
| Boundary/security scans | 10 changed target files; 0 outside allowlist; 1,240 added lines; 0 secret, prohibited-operation or conflict-marker hits |
| Temporary artifacts | no mutant/evidence artifacts retained |

Counting basis:

- New A3 tests: `16`.
- Final Phase-A unique test total: `63`.
- Focused REV869B subset: `79` (contained in the ERP total).
- Complete ERP non-PostgreSQL total: `453`.
- Unique total across the two assemblies: `63 + 453 = 516`.
- Raw overlapping formal invocation pass events: `13 + 63 + 79 + 453 = 608`.
- The separate three-test canonical rerun is diagnostic confirmation and is not added to the stated four-invocation raw formal total or any unique total.
- PostgreSQL tests discovered: `87`; executed: `0`.

Principal validation commands:

```powershell
dotnet build tests/SESS.NexaERP.ControlPlane.Tests/SESS.NexaERP.ControlPlane.Tests.csproj --no-restore -warnaserror
dotnet build SESS.NexaERP.slnx --no-restore -warnaserror
dotnet test tests/SESS.NexaERP.ControlPlane.Tests/SESS.NexaERP.ControlPlane.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~ArchitectureFreezeContractTests.A3_&FullyQualifiedName!~Postgres" --logger "console;verbosity=minimal"
dotnet test tests/SESS.NexaERP.ControlPlane.Tests/SESS.NexaERP.ControlPlane.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName!~Postgres" --logger "console;verbosity=minimal"
dotnet test tests/SESS.NexaERP.Tests/SESS.NexaERP.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~Rev869B&FullyQualifiedName!~Postgres" --logger "console;verbosity=minimal"
dotnet test tests/SESS.NexaERP.Tests/SESS.NexaERP.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName!~Postgres" --logger "console;verbosity=minimal"
dotnet test tests/SESS.NexaERP.Tests/SESS.NexaERP.Tests.csproj --no-build --no-restore --list-tests --filter "FullyQualifiedName~Postgres" --logger "console;verbosity=minimal"
git diff --check 8c78f6a480fcbf86afbf9f5460598ece5b8d6732 -- <exact-ten-file-allowlist>
git diff --check 51476760adcea9ed7babbc04d642e53e371c6941 -- <exact-ten-file-allowlist>
```

## Final source/test artifact SHA-256 before checkpoint commit

| File | SHA-256 |
|---|---|
| `Rev869BControllerMessagesV1.cs` | `A156C11AEE62954504F348394E8C92A5FD9BD4B3FAD37635DF0EA7373B7E1BBE` |
| `ControlPlaneOptions.cs` | `7AA59A7308BE18C99099ACAEA1FE94AF8D5340D631A9F692721DBF02E0703254` |
| `Rev869BExecutionBinding.cs` | `627E1BD4B08DE0F429E5327E0169DCE8EC8DEF5EE745B78CE7526866D01A9745` |
| `Rev869BControllerStateMachine.cs` | `E80EA9F58431CB150A1296E170BE54586E99FDECDE3EE57FEA205818C952AA05` |
| `SignedEnvelopeService.cs` | `CA3AC0715D6C79E516AF23A4DB745C84F0A249378F053873874139BCA290F5C5` |
| `AcceptanceVerifierOptions.cs` | `B742F7285117676C719F27A7D9CE533B7442F10FB71BDDF731A02135D4219694` |
| `ClosedEvidenceVerifierV1.cs` | `95C7FB4C8B5E08E40FD0672037B7A70C69F0E6A0876B7B06A6161E2027BF49EB` |
| `ArchitectureFreezeContractTests.cs` | `6EEC68E7E98762631136BB3F11CDC2A272B0DF17F40FF2A06C576A41F44899A5` |
| `Rev869BCorrection17SourceContractTests.cs` | `CB3A379B7C553705AB10024E98A1386871C7D34C0937F827EF2D46F2B298297B` |

## Prohibited operations and remaining prerequisites

No PostgreSQL access/test execution, migration application, Phase B, Correction 2, provisioning, deployment, production access, real key/credential/trust-root use, lifecycle/recovery/purge/export execution, frontend work, or unrelated Purchase/Stores work occurred. Migration/model/snapshot/project/solution/helper files were not changed. The exact target-scoped worktree is required clean after the one commit.

External prerequisites remain: deployed composite durable persistence; workload identity/IAM and private networking; production issuer/policy stores and non-exportable KMS/HSM keys; authoritative least-privilege readers and pinned oracle artifacts; immutable audit/evidence storage; HA/failover; authorized PostgreSQL behavioral/concurrency/rollback/restart/PITR evidence; backup/restore/DR, scale/load/chaos, monitoring, runbooks, training and management approval.

## Retained states and exact next gate

`phase_a_correction_a3_source_implementation_state=COMPLETE_PENDING_INDEPENDENT_REVIEW`

`phase_a_management_acceptance_state=FAIL_PENDING_INDEPENDENT_REVIEW`

`phase_b_state=NO_GO`

`correction_2_state=NO_GO`

`postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN`

`external_provisioning_state=NOT_STARTED`

`production_readiness_state=NOT_READY`

Exact next gate: a fresh independent report-only source architecture/security review of the committed A3 diff. It must remain separate from implementation.
