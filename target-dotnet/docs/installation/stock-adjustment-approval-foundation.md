# A2 stock-adjustment approval foundation

A2 remains incomplete: this change supplies tested domain policies and revision-bound review. It does not expose a stock-adjustment API, create a period, persist an approval, or post stock/FIFO movements.

The policy carries the approved bands: below 5,000 Stores Manager; 5,000 through 100,000 TD; above 100,000 MD. Serialized identity uses TD regardless of value. Damage/loss write-off uses TD plus independent Accounts concurrence. Captured accepted line values are summed without netting additions against removals. Recorder/counters are excluded; reversal retains at least the original required roles.

Every review belongs to one retained revision. Changing the revision invalidates use of earlier decisions. The review retains employee, role assignment, server time and reason, and requires different employees for separate decisions. Runtime services must resolve those identities and scope from governed records; these domain objects alone are not authorization.

Date policy refuses closed periods, future dates and dates outside the selected open period. Backdating retains reason/evidence; beyond seven days adds TD without dropping MD or Accounts requirements. The server date and period state must come from trusted persisted context.

The special SCRAP route still requires its own TD/MD workflow; this damage/loss policy must not be reused as scrap-disposal authority. Atomic physical/FIFO posting, persisted revision/decision evidence, idempotency, concurrent issue protection, period commands, serialized identity movement and end-to-end acceptance remain outstanding. No ordinary operational gap is declared closed by these policy tests.

## Validation

Fresh active-solution Debug and Release builds: zero warnings/errors. Focused checks: 41/41 in both configurations. Full TRX: Debug 922/922 and Release 919/919, zero failed/skipped. All indexed source hashes verified before publication. These suites accept this foundation, not the unimplemented adjustment workflow.

Expected business-row and schema effects: zero. No frontend read or request contract changes. No owner database accessed. Separate draft period SQL and A3/A4 experiments are excluded from this commit and acceptance claim.
