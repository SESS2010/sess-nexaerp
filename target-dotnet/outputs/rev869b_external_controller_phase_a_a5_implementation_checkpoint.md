# REV869B Option-A Phase-A Correction A5 blocker checkpoint

Date: 2026-08-21 (Asia/Calcutta)

## Decision

`A5_BLOCKED_MUTANT_GATE`

The revised A5 correction is not committed. The immutable stop condition in section 9.14 of `rev869b_external_controller_phase_a_a5_boundary_and_immutable_plan_contract_decision.md` triggered during the required production-mutant campaign. All attempted source, test, project, migration and solution changes were removed. This checkpoint is the only retained change.

## Authoritative entry verification

- Entry HEAD: `fe704bd65879cb1b3fc64193d9050387834144e3`.
- Entry parent: `86cf8e09677fe81923296a808e7ed31b70f0f323`.
- The boundary/plan-contract report was read completely.
- Report SHA-256: `D41FFDA84969F4E64575FF207EC7413C4A7E7194AEEDC50179EF87937710F4A3`.
- Action manifest independently reproduced in two fresh processes: `20` rows, `2,668` UTF-8 bytes, SHA-256 `EDAF648EFF4BD77158EF3A18A780D7B0DAD634FFB90CDBA8564A27D4DCFC95CB` both times.
- Exact lineage and the clean target-scoped entry worktree passed before editing.

## Validation reached before the stop

- Warning-as-error solution build: passed with `0` warnings and `0` errors.
- Separate warning-as-error Control Plane host, API host, control-test graph and ERP-test graph builds: all passed with `0` warnings and `0` errors.
- Exact A5 aggregate: `30/30`; strengthened aggregate immediately before mutation testing: `30/30`.
- Exact A5 individual invocations: `30/30`, each `1/1`.
- Retained A4 aggregate: `23/23`.
- Complete control assembly: `116/116`.
- Complete non-PostgreSQL ERP assembly: `455/455`.
- EF discovery used `migrations list --no-connect --no-build`: `14` migrations, with `20260821093000_Rev869BA4TargetExecutionBoundary` appearing exactly once after REV869B; applied state intentionally unknown.
- PostgreSQL tests executed: `0`; PostgreSQL connections: `0`; migration applications: `0`; services started: `0`; provisioning/deployment/production operations: `0`; network/package downloads: `0`; Phase B and Correction 2 operations: `0`.

These passing observations are diagnostic only. They do not override the mutant stop.

## Exact blocking observation

Disposable-copy mutation arithmetic at the stop:

- retained A4 mutants A4-M01 through A4-M10: `10` compiled, `10` killed;
- retained A5 mutants A5-M11 through A5-M17: `7` compiled, `7` killed;
- A5-M18 candidate: compiled, survived its named A5-11 killer, and was then classified invalid because changing only the SQL exception token from `A4_STALE_FENCE` to `STALE_FENCE_DISABLED` did not remove the stale-fence predicate;
- observed total: `18` compiled, `17` killed, `1` survived candidate, `1` invalid candidate;
- A5-M19 through A5-M40: not started after the mandatory stop.

The attempted A5-M18 run also exposed a vacuous assertion in `A5_StaleFenceOrEqualFenceDifferentDigestFailsBeforeActionHandler`: it compares `IndexOf("A4_STALE_FENCE")` with the completion-function position but does not first require the stale-fence marker to exist. When the marker is absent, `IndexOf` returns `-1`, so the ordering assertion remains true. The test separately requires the equal-fence and replay-conflict markers but not the stale-fence marker.

This evidence violates the required formula `compiled=40 AND killed=40 AND survived=0 AND invalid=0`. Under stop condition 9.14, no test repair, replacement mutant, remaining campaign, source correction or correction commit is authorized in this run.

## Cleanup and retained state

- The disposable directory `rev869b-a5-mutants-20260821` was deleted after its source files were restored and checked against the working source.
- Every tracked A5 implementation edit was restored to entry HEAD.
- Every new A5 source/test/migration file was deleted by exact verified path.
- No prior report or checkpoint was modified.
- Before adding this checkpoint, `git diff --quiet fe704bd65879cb1b3fc64193d9050387834144e3 -- .` returned `0`.

## Next management gate

Management must commission a separate report-only A5 mutant-gate failure reconciliation. It must define a valid A5-M18 production mutation that actually removes stale/equal-fence enforcement, repair the vacuous A5-11 assertion within a newly authorized boundary, and state whether the entire 40-mutant campaign must restart. No correction starts automatically.

`phase_a_correction_a5_implementation_state=BLOCKED_NO_SOURCE_CORRECTION`

`phase_a_management_acceptance_state=FAIL_PENDING_MUTANT_GATE_RECONCILIATION`

`phase_b_state=NO_GO`

`correction_2_state=NO_GO`

`production_readiness_state=NOT_READY`
