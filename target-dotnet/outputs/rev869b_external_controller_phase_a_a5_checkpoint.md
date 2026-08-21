# REV869B Option-A Phase-A Correction A5 Stage 0 blocker

Date: 2026-08-21

Decision: `A5_IMPLEMENTATION_BLOCKED_BEFORE_SOURCE_EDIT`

## Authoritative entry

- Entry HEAD: `cce7cc01dd0606b3e4c628ad6cb737eeec56e76a`
- Entry parent: `0474169e379b2a1cd7aef800fabd225114d01fc8`
- Entry subject: `REV869B Phase-A A4 failure reconciliation`
- Entry commit content: exactly `outputs/rev869b_external_controller_phase_a_a4_failure_reconciliation.md`
- Reconciliation report SHA-256: `1AD5CEF72BDD3292FFEA244FAA77652A32B6501EFF42059DC729BE6E99075A78`
- Target-scoped worktree at entry: clean
- Exact maximum allowlist checked: 25 paths; all 14 pre-existing paths were present and all 11 `NEW` paths were absent
- Offline provider prerequisite: cached `Npgsql/10.0.3` was present in the existing Infrastructure assets and local package cache; no package download or network access occurred

The authoritative reconciliation, its exact 25-path maximum allowlist, 22 named A5 tests, 20 new A5 mutants and 10 retained A4 mutants were read and accepted as the proposed correction boundary.

## Blocking finding

`A5-STAGE0-B01` — **the committed immutable plan cannot identify a server-owned business action handler**.

The frozen reconciliation requires the target provider to consume a signed immutable job and invoke a server-owned action-handler registry inside the target transaction. It also requires the target transaction to include the business mutation and history rows, not merely A4 bookkeeping.

The current immutable production contract cannot express that responsibility:

- `A4ExecutionPlanBindingV1` contains `PlanId`, `PlanVersion`, `PlanSha256`, organization, target, a generic `Operation`, executor identity and evidence hash. It contains no server-owned action identifier, action version, fixed action parameters or handler-artifact binding.
- `A4TargetExecutionJobV1` adds only execution identity/digest, the grant, the lease and dispatch time. It does not add an action identity or server-owned input.
- `BuildA4Operation` sets the plan operation from the incoming lifecycle payload operation. For this path that value is `BEGIN_EXECUTE_AUTHORIZED_PLAN`; it identifies the controller lifecycle command, not the business action to execute.
- The allowed target-side source contains no existing server-owned action registry or handler that can derive a business action from this immutable plan.

Consequently, a concrete target provider inside the frozen boundary has only unsafe or incomplete choices: select code/type/input from the caller, invent a generic executor, record bookkeeping without performing the required business mutation, or add/change a real business handler and its plan contract. The first two are expressly forbidden; the third does not close A4-IR-F02 or satisfy A5-09/A5-10; the fourth requires a new business/plan responsibility outside the frozen boundary and may enter Phase B.

This directly triggers reconciliation stop condition 5: target execution cannot identify a server-owned action handler from the immutable plan without caller-selected code/type/input. Closing it also requires a management decision on stop condition 4 because Purchase application/domain and other business source are explicitly immutable under A5.

## Actions not performed

No production source, test, solution, project, host, schema or migration file was edited. No build, test or mutant was run after the blocker became authoritative because the gate requires stopping before implementation. No PostgreSQL process or test was run; no migration was generated, applied or rolled back; no network, infrastructure, credential, provisioning, deployment, service start, production operation, Phase B, Correction 2, recovery, purge or export action occurred. `../legacy-reference/` was not enumerated, opened, read, modified or used.

## Required management gate

`NEXT_MANAGEMENT_GATE=REPORT_ONLY_BOUNDARY_AND_PLAN_CONTRACT_DECISION`

Management must define and authorize a server-owned target action contract before another correction can start. That decision must identify the fixed action ID/version and canonical parameters carried by the immutable management-approved plan, the concrete target business handler and source paths allowed to implement it, and whether that responsibility remains Phase A or belongs to Phase B. The revised gate must also re-freeze its path boundary, enforcement points, tests and mutants.

Do not start A5 again, Phase B or Correction 2 automatically. A fresh authorization is required.

`phase_a_correction_a5_state=BLOCKED_AT_STAGE_0`

`phase_a_management_acceptance_state=FAIL`

`rev869b_source_safety_state=FAIL_NOT_CORRECTED`

`phase_b_state=NO_GO`

`correction_2_state=NO_GO`

`postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN`

`production_readiness_state=NOT_READY`
