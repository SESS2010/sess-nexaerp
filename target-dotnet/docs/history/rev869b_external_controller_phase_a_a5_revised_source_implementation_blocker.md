# REV869B Option-A Phase-A revised A5 source implementation blocker

Date: 2026-08-21

Decision type: authorized report-only blocker after mandatory pre-edit package stop

Entry HEAD: `08a2afe1f78110bf83c032920b281fe5c8420f92`

Expected parent: `7261382b42b3762b9b5bae3ab16b121affb2532d`

## 1. Decision

`A5_REVISED_SOURCE_IMPLEMENTATION=BLOCKED_BEFORE_SOURCE_EDIT`

The implementation did not start. The local `Npgsql 10.0.3` cache has contradictory content-identity evidence and
cannot produce the exact trusted project-local lock evidence required by the architecture freeze.

The frozen package content SHA-512 is:

`7nb5YzXuvWWJxB0J8DiyL3we+X4FOctZrt0fIBnucOIaIevFEEwGQVZKtiu9olXdlNAK1eNgqSral6r/jlhI4w==`

The actual cached `npgsql.10.0.3.nupkg` bytes hash to:

`9wZYlR6r1uLGnAxBuUPOCmgbS6SDtVKyGkZ0tDGPaBHYbbdEDEajD0VErq+W/dn3gHfJ3WvOdZaNNNybnFCupA==`

The package sidecar agrees with the actual bytes, while `.nupkg.metadata` and the restored assets graph retain the
different frozen value. Accepting either value silently would bypass the exact pinned-content architecture rule.
The authorization therefore requires rollback/stop and permits only this blocker report.

## 2. Stage-0 result

| Gate | Verified result | Status |
|---|---|---|
| HEAD | `08a2afe1f78110bf83c032920b281fe5c8420f92` | PASS |
| Parent | `7261382b42b3762b9b5bae3ab16b121affb2532d` | PASS |
| Subject | `REV869B Phase-A A5 persistence classifier architecture freeze` | PASS |
| Branch | `master` | PASS |
| HEAD content | Exactly `target-dotnet/outputs/rev869b_external_controller_phase_a_a5_persistence_and_classifier_architecture_freeze.md` | PASS |
| Architecture report SHA-256 | `0A7158D452F06CECEBF4863FF2103007D5FBB5FDFDA6DF817E359778C9742833` | PASS |
| Architecture decision | `A5_REVISED_ARCHITECTURE_AND_PACKAGE_GATE=GO` | PASS |
| Target tracked/index scope | Clean before package proof | PASS |
| Legacy sibling | Remained the same untracked entry; contents were not accessed, enumerated, opened or modified | PASS |

All authoritative Phase-A, A4 and revised A5 architecture, action-manifest, boundary, mutant-harness, project-graph
and package-ownership reports were read completely. The exact frozen implementation allowlist contains 30 paths,
including the mutually exclusive implementation checkpoint. No unnamed repository path was created or modified.

## 3. Exact local package evidence

Package path inspected:

`C:\Users\User\.nuget\packages\npgsql\10.0.3\npgsql.10.0.3.nupkg`

| Evidence source | Observed identity |
|---|---|
| Frozen architecture report | SHA-512 base64 `7nb5YzXuvWWJxB0J8DiyL3we+X4FOctZrt0fIBnucOIaIevFEEwGQVZKtiu9olXdlNAK1eNgqSral6r/jlhI4w==` |
| `.nupkg.metadata` `contentHash` | `7nb5YzXuvWWJxB0J8DiyL3we+X4FOctZrt0fIBnucOIaIevFEEwGQVZKtiu9olXdlNAK1eNgqSral6r/jlhI4w==` |
| Restored Infrastructure `project.assets.json` `sha512` | `7nb5YzXuvWWJxB0J8DiyL3we+X4FOctZrt0fIBnucOIaIevFEEwGQVZKtiu9olXdlNAK1eNgqSral6r/jlhI4w==` |
| SHA-512 computed directly over cached `.nupkg` bytes | `9wZYlR6r1uLGnAxBuUPOCmgbS6SDtVKyGkZ0tDGPaBHYbbdEDEajD0VErq+W/dn3gHfJ3WvOdZaNNNybnFCupA==` |
| Cached `npgsql.10.0.3.nupkg.sha512` sidecar | `9wZYlR6r1uLGnAxBuUPOCmgbS6SDtVKyGkZ0tDGPaBHYbbdEDEajD0VErq+W/dn3gHfJ3WvOdZaNNNybnFCupA==` |
| SHA-256 computed directly over cached `.nupkg` bytes | `75D0970923A8C9FCBBD37E4EBE72FEE0B10362A1E36723E86777DF1B6728316D` |

This is a local cache/content-evidence inconsistency. This report does not classify the upstream package or signature
as defective because no authorized independent upstream artifact was consulted and no network access is permitted.

## 4. Local-only restore proof and why it is insufficient

A restore feasibility check used only an empty local directory as the named NuGet source:

`dotnet restore src/SESS.NexaERP.Infrastructure/SESS.NexaERP.Infrastructure.csproj --force --no-http-cache --source <empty-local-directory> --verbosity minimal`

The restore succeeded from global cached package content and restored Domain, Application and Infrastructure. It did
not name or contact a public/external package source. The command changed only ignored generated `obj` assets.

Success does not close the gate: NuGet reused `.nupkg.metadata` and emitted the frozen metadata hash into
`project.assets.json` even though the actual cached archive bytes and sidecar have a different SHA-512. A future lock
file derived from this state would not prove that the locked content identity matches the archive used. Generated
assets are explicitly non-authoritative under the architecture freeze.

## 5. Triggered mandatory stop rules

The authorization requires stop if:

- any lock file drifts;
- a cached package is hash-mismatched;
- exact local package/lock reproducibility cannot be proven; or
- an architecture/package rule differs before editing.

All four formulations apply to the contradictory evidence above. No approval to replace the frozen hash, rewrite the
cache, fetch a package, use another source, waive content verification or generate a lock from inconsistent evidence
was requested or inferred.

## 6. Actions not performed

- No production source, test, project, solution, package-lock, migration, helper or checkpoint file was created or
  modified.
- The implementation checkpoint
  `outputs/rev869b_external_controller_phase_a_a5_revised_source_implementation_checkpoint.md` was not created.
- No build, test, PostgreSQL test, mutant, EF discovery, PowerShell script, service or endpoint was run.
- No PostgreSQL connection, migration application/removal, provisioning, deployment, production access, credential,
  key, Phase B or Correction 2 operation occurred.
- No public/external package source was contacted.
- No history rewrite, amend, reset, rebase or stash occurred.
- `../legacy-reference/` was not accessed or modified.

Only ignored restore assets were refreshed before the mismatch was fully classified. The tracked target scope remained
clean before this blocker report was added.

## 7. Required next decision

The next gate must be a separate report-only package-artifact reconciliation. It must name one authorized offline
`Npgsql 10.0.3` archive, independently verify its archive SHA-512/content signature, decide the authoritative content
hash, define safe cache replacement without network access, and issue a new exact implementation baseline only after
the package archive, sidecar, metadata, lock and restore assets agree byte-for-byte.

This blocker does not authorize cache mutation, package download, source implementation, tests, mutants, PostgreSQL,
migrations, services, deployment, production, Phase B or Correction 2.

`A5_REVISED_SOURCE_IMPLEMENTATION_STATE=BLOCKED_PACKAGE_CONTENT_IDENTITY`

`A5_IMPLEMENTATION_CHECKPOINT_STATE=NOT_CREATED`

`A5_MUTANT_GATE_STATE=NOT_STARTED`

`POSTGRESQL_EXECUTION_STATE=NOT_AUTHORIZED_NOT_RUN`

`PHASE_A_MANAGEMENT_ACCEPTANCE_STATE=FAIL_PENDING_PACKAGE_RECONCILIATION`

`PHASE_B_STATE=NO_GO`

`CORRECTION_2_STATE=NO_GO`
