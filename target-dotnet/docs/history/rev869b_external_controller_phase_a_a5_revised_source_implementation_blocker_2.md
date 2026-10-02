# REV869B Option-A Phase-A revised A5 source implementation blocker 2

Date: 2026-08-21

Decision type: authorized report-only blocker after mandatory pre-edit acceptance reconciliation

Entry HEAD: `431e91583d0e55438ed85b06f1f360790e6c8a0c`

Expected parent: `f7c7c4ebd59973d549dc00effeba6b16d983ba5c`

## 1. Decision

`A5_REVISED_SOURCE_IMPLEMENTATION=BLOCKED_BEFORE_SOURCE_EDIT`

The source implementation did not start. The authorized acceptance set contains an irreconcilable migration-count
boundary: the committed ERP already contains exactly 13 accepted migrations, while the frozen A5 allowlist requires
one additional discoverable migration, `20260821093000_Rev869BA4TargetExecutionBoundary`, and prohibits changing or
removing any existing accepted migration or snapshot. A conforming implementation would therefore expose 14
migrations, but the current authorization requires verification of exactly 13.

Making the mandatory A5 migration non-discoverable would not implement the frozen target schema boundary. Keeping the
count at 13 would require modifying or removing an accepted migration, which is outside the exact 30-path allowlist
and is an explicit stop condition. No compliant source candidate can satisfy both requirements.

## 2. Stage 0 evidence

| Gate | Observed result | Status |
|---|---|---|
| HEAD | `431e91583d0e55438ed85b06f1f360790e6c8a0c` | PASS |
| Parent | `f7c7c4ebd59973d549dc00effeba6b16d983ba5c` | PASS |
| Subject | `REV869B Phase-A A5 official package identity amendment` | PASS |
| Branch | `master` | PASS |
| HEAD boundary | Exactly `outputs/rev869b_external_controller_phase_a_a5_controlled_official_package_acquisition_and_identity.md` | PASS |
| Official package report SHA-256 | `C9D9403E0AE3EEB891544628D80372B10A912B599698EA842D128590966B0B95` | PASS |
| Required report decision | `A5_CONTROLLED_OFFICIAL_PACKAGE_IDENTITY_GATE=GO` | PASS |
| Target tracked/index scope | Clean before this report | PASS |
| Legacy sibling | Remained the same untracked entry and was not accessed or modified | PASS |

All governing Phase-A, A4 and revised A5 architecture, action-manifest, boundary, mutant-harness, project/package
graph, package-artifact and official package-identity reports were read as the authoritative boundary. The exact
frozen allowlist contains 30 paths, with this blocker report replacing the mutually exclusive implementation
checkpoint. No unnamed repository path was created or modified.

## 3. Exact migration contradiction

The committed migration tree contains these 13 non-designer migrations:

1. `20260808110924_Phase1Foundation.cs`
2. `20260808114550_Phase1AuthorizationSeed.cs`
3. `20260808123411_Rev866EmployeePermissionMatrix.cs`
4. `20260808142353_Rev866CorrectiveStatusPermissionAudit.cs`
5. `20260808151207_Rev867MasterFoundation.cs`
6. `20260808160435_Rev867C1Corrections.cs`
7. `20260808182945_Rev868PurchaseRequisitionFoundation.cs`
8. `20260808190920_Rev868PurchaseLocationAllocationCorrection.cs`
9. `20260809123000_Rev868C2DepartmentManagerApprovalMapping.cs`
10. `20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation.cs`
11. `20260810110000_Rev868C3LegacyMixedDepartmentDeactivationCorrection.cs`
12. `20260810120000_Rev869AIdentityMasterScopeFoundation.cs`
13. `20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation.cs`

The frozen architecture simultaneously requires both new paths:

- `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260821093000_Rev869BA4TargetExecutionBoundary.cs`
- `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260821093000_Rev869BA4TargetExecutionBoundary.Designer.cs`

It also states that existing accepted migrations and the model snapshot are immutable, that the new migration must
appear once after REV869B in offline EF discovery, and that needing an unnamed migration/snapshot path triggers stop.
Consequently, the only valid post-implementation arithmetic is `13 existing + 1 mandatory new = 14`, not 13.

An offline baseline discovery invocation using `dotnet ef migrations list`, with `--no-connect` supplied, stopped in
the design-time factory before listing because `ConnectionStrings__NexaErp` was absent. No connection was opened. The
factory source is not in the 30-path allowlist. This observation does not replace the decisive count contradiction:
even if discovery were supplied a nonconnecting design-time value, the mandatory new discoverable migration would
still make the required count 14.

## 4. Cached package identity evidence

The cached archive was inspected read-only and not used for restore:

- Package: `Npgsql 10.0.3`
- Archive SHA-256 recomputed: `75D0970923A8C9FCBBD37E4EBE72FEE0B10362A1E36723E86777DF1B6728316D`
- Raw archive SHA-512 Base64 recomputed: `9wZYlR6r1uLGnAxBuUPOCmgbS6SDtVKyGkZ0tDGPaBHYbbdEDEajD0VErq+W/dn3gHfJ3WvOdZaNNNybnFCupA==`
- Both match the official package-identity amendment.
- No package source was contacted and no restore was run in this attempt.

The package gate is not the blocker in this attempt.

## 5. Work not performed

- No production source, test, project, solution, package-lock, migration, helper, existing report or implementation
  checkpoint was created or modified.
- No build, test or mutant was run because the mandatory pre-edit boundary failed.
- Mutant result is `0 compiled / 0 killed / 0 survived / 0 invalid / 40 not started`; no result is claimed or reused.
- PostgreSQL tests discovered: `0`; PostgreSQL tests executed: `0`; PostgreSQL connections: `0`.
- Migration applications/removals: `0`; services started: `0`; deployments and production operations: `0`.
- Phase B and Correction 2 were not started.
- No external network or package source was accessed.
- No amend, reset, rebase, stash or history rewrite occurred.
- `../legacy-reference/` was not accessed or modified.

## 6. Required next gate

The single next gate is a report-only management reconciliation that chooses one exact migration acceptance rule:

1. revise the required post-A5 migration count to 14 while retaining the mandatory new A5 migration and all accepted
   migrations; or
2. explicitly revise the frozen architecture and exhaustive allowlist if a different migration topology is intended.

That reconciliation must also define a reproducible offline EF discovery command that creates the design-time context
without opening a PostgreSQL connection and without requiring an unnamed source edit. It must issue a new exact
implementation baseline before another source attempt.

This blocker authorizes no source implementation, build, tests, mutants, PostgreSQL, migration execution, services,
deployment, production access, Phase B or Correction 2.

`A5_REVISED_SOURCE_IMPLEMENTATION_STATE=BLOCKED_MIGRATION_COUNT_CONTRADICTION`

`A5_IMPLEMENTATION_CHECKPOINT_STATE=NOT_CREATED`

`A5_MUTANT_GATE_STATE=NOT_STARTED`

`POSTGRESQL_DISCOVERED_EXECUTED=0/0`

`PHASE_A_MANAGEMENT_ACCEPTANCE_STATE=FAIL_PENDING_MIGRATION_GATE_RECONCILIATION`

`PHASE_B_STATE=NO_GO`

`CORRECTION_2_STATE=NO_GO`
