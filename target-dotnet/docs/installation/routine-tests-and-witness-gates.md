# Routine tests and deliberate witnesses

## Completion rule (15 September 2026)

Run the full routine Release suite after implementation changes and before reporting an item complete. Targeted runs are diagnostic evidence; they do not replace full-suite acceptance. Whole-assembly checks such as RequestInputAndActorReadReachability can detect omissions outside a targeted report filter. Retain a TRX file for every full run, including failed runs, in a separate results directory for that run.

Do not overlap full suites or rebuild the shared output while a suite is running. Before using --no-build, finish a routine Release build with all witness gates off. Record the executed/passed/failed counts and measured test-process wall time separately from build time. An overlapping run is not a clean routine timing measurement.

```powershell
dotnet test SESS.NexaERP.slnx -c Release --no-build --nologo --logger "trx;LogFileName=full-suite.trx" --results-directory local-evidence/<unique-run-directory>
```

The results below describe earlier checkpoints, not acceptance of subsequent changes.

The 14 September overnight instruction requires a routine test run below 30 minutes and preserves deliberate heavy witnesses. This change does not change application routes, fields, envelopes, migrations, permissions or business rows.

Build without witness properties for routine verification. All eight properties are opt-in (unset means off): KeycloakWitness, ReportVolumeWitness, DiskFullWitness, MigrationInterruptWitness, ConcurrencyWitness, HostFailureWitness, WorkflowWitness and MigrationLifecycleWitness. Conditional test methods are absent from discovery, rather than reported as passing or skipped. Shared helpers still compile, including helpers required by ordinary business tests.

ConcurrencyWitness enables the fourteen Concurrent* entry points and the combined vendor PO cash race. MigrationInterruptWitness enables interrupted migration rollback/retry. HostFailureWitness enables both interrupted issue cases (lost connection and PostgreSQL restart) and the notification/port collision witness. Existing Keycloak, disk-full and report-volume gates retain their prior behavior. HostFailureWitness also includes the client-closure, injected posting/audit failure and backup/restore workflows; ConcurrencyWitness includes the stale receipt overlap. WorkflowWitness enables the additional full three-band financial, reporting, QC and FIFO scenario runs. ONE canonical complete purchase workflow remains in the routine suite. Ordinary business correctness and migration checks remain enabled.

Routine command (replace Release with Debug for the second configuration):

```powershell
dotnet test tests/SESS.NexaERP.Tests/SESS.NexaERP.Tests.csproj -c Release -p:UseSharedCompilation=false -m:1 --logger "trx;LogFileName=routine-release.trx"
```

Build a deliberate witness with its property, then use its test name filter. Do not reuse --no-build from a witness-enabled build when intending a routine run: the properties control compilation, and an already-built assembly retains its discovery set. Keycloak and disk-full additionally require their documented disposable external runtime configuration.

```powershell
dotnet build tests/SESS.NexaERP.Tests/SESS.NexaERP.Tests.csproj -c Release -p:ConcurrencyWitness=true -p:UseSharedCompilation=false -m:1
dotnet test tests/SESS.NexaERP.Tests/SESS.NexaERP.Tests.csproj -c Release --no-build --filter FullyQualifiedName~DirectFifoCallsCannotConsumeTheLastRemainingUnitTwice
```

MigrationLifecycleWitness enables 22 historical upgrade/rollback and ownership-state checks. The canonical purchase workflow, fresh bootstrap paths and full-chain provisioning/rollback remain routine. Ordinary disposable initdb uses --no-sync; durable fault/recovery fixtures retain synchronized initialization and fsync/synchronous_commit on. A local initialization comparison measured 19.996 seconds normally and 13.096 seconds without sync. These are disposable test settings, not deployment settings.

The first complete gated Release baseline ran 857 tests: 854 passed, 3 failed, zero skipped, 42m40s. It did NOT meet the timing target. Its retained TRX names the stale IssuePO guard and two bootstrap setups that run migrations as postgres despite managed roles. Those failures are independent of report 5, which is stashed. The first refinement ran 850 tests: 847 passed, the same 3 failed, zero skipped, 31m04.5s. Fifteen additional historical lifecycle round trips are now opt-in; the final measured result follows.

Both builds passed with zero warnings/errors: Debug with all eight witness properties enabled (4m30.96s), and routine Release with them unset (28.28s). Debug discovery includes 898 entries. Routine Release discovery includes 825 entries, contains exactly one canonical purchase flow, and contains none of the 69 gated test methods; ReportVolumeWitness extends the canonical flow rather than declaring another test method. Discovery counts are not execution counts because some theories expand at runtime.

The targeted Debug baseline ran 15 tests: 12 passed, the same 3 failed, zero skipped, 1m30s. This is a targeted baseline, not a full Debug certification. The final full routine Release run executed 835 tests: 832 passed, the same 3 failed, zero skipped. Measured process wall time was 1,330.066 seconds (22m10s), excluding the separate 28.28-second build. VSTest reported 22.1345 minutes. The timing target is met; the three pre-existing correctness failures remain the next priority and are not gated out. Retained evidence: local-evidence/overnight-20260914/routine-final-release.trx and gates-targeted-debug.trx. This is not a zero-failure certification.

Expected application/business-row changes from this commit: zero. Test databases remain disposable and isolated; durable fault/recovery fixtures retain synchronized initialization. No frontend route, field, or envelope changes.

Governing documents consulted: the user's final overnight instruction; docs/SESS_NexaERP_Three_Day_Plan.md (priority and verification workflow). The frozen schema and Answers precedence remain unchanged; no business decision is changed by gating.