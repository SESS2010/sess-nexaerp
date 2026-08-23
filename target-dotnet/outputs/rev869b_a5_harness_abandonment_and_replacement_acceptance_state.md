# REV869B A5 Harness Abandonment and Replacement Acceptance State

## Management decision

The owner has authorized abandonment of the A5 formal evidence harness introduced by commit `687a75ee7e9cdf7efb6e66e51547872ab3ed196a`. It will not be remediated.

The harness is abandoned because it could not emit legitimate mutant evidence, its prohibited-operation counters were fabricated, and its authorization trust anchor was caller-supplied. Management determined that remediation cost exceeds the risk the harness mitigates for this program.

The terminal independent review failure is recorded by commit `972dea44c3e1b693e3570478b0c6e9acd5b494bd` in `outputs/rev869b_a5_formal_evidence_harness_independent_review_failure_state.md`.

## Canonical states

```text
A5_FORMAL_EVIDENCE_HARNESS_STATE=ABANDONED_NOT_REMEDIATED
A5_ACCEPTANCE_MODEL=MANUAL_WITNESSED_PLUS_INDEPENDENT_REVIEW
A5_SOURCE_IMPLEMENTATION_GATE=GO
phase_a_management_acceptance_state=FAIL_PENDING_REWORK
phase_b_state=NO_GO
postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN
production_readiness_state=NOT_READY
```

The A5 source implementation gate is recorded as authorized by management, but no A5 source implementation begins in this recorder/file-mover change.

## Replacement A5 acceptance model

A5 acceptance requires all four steps below, in this exact order:

1. Codex reports test results but never declares acceptance. Codex output must end with `RESULT_REPORTED_PENDING_WITNESS`. Codex must not write `PASS`, `ACCEPTED`, or any success verdict for A5.
2. The owner or IT person executes the test command themselves and reads the counts on screen. Agent-reported counts are not evidence.
3. The same command is re-run on a second machine from a clean clone. Counts must match.
4. A fresh Codex session, with no implementation history, performs a report-only independent review and issues its own verdict.

Any Critical or High finding in step 4 fails the candidate.

No A5 candidate can be accepted by agent-reported results alone. Completion of an implementation or an agent test run is not an A5 acceptance decision.

## Abandoned artifact handling

The six harness artifacts are retained under `outputs/deprecated/a5_evidence_harness/` for lessons only. They must never be executed or cited as evidence. Their relocation does not reopen, amend, or replace the terminal failed review of the harness candidate.

This record authorizes no PostgreSQL access, migration execution, Phase B work, deployment, service start, production access, or production-readiness claim.
