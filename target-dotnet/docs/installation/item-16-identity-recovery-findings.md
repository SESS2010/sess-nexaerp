# Identity recovery findings: 15 September 2026

The first production-mode, PostgreSQL-backed Keycloak recovery witness reached successful source password/TOTP login and the ERP OIDC/mapping checks, then failed scheduled-backup verification. Its TRX is local-evidence/item16-local-production/keycloak-recovery-release.trx (one failed diagnostic test). This is not a recovery pass.

## PostgreSQL internal index attributes are not portable column evidence

pg_restore exited zero. Source and restored evidence contained 3 realms, 4 users and 5 credentials; bootstrap identity, server version, roles and every metadata section except columns matched.

Four differing entries were all indexes (pg_class.relkind=i):

| Index | Source internal attribute | Restored internal attribute |
|---|---|---|
| constraint_84 | application_id | client_id |
| pk_cl_tmpl_attr | template_id | scope_id |
| pk_template_scope | template_id | scope_id |
| uk_b71cjlbenv945rb6gcon438at | name | client_id |

For constraint_84, BOTH definitions are exactly:
CREATE UNIQUE INDEX constraint_84 ON public.client_node_registrations USING btree (client_id, name)

Keycloak's migration history renamed table columns; historical internal index labels remained until the index was recreated by restore. pg_attribute describes indexes as well as real tables. Treating all its names as portable application column names was the wrong assumption.

The correction keeps captured raw evidence, but removes only index/partitioned-index attribute entries from BOTH sides of the metadata comparison. Actual table/view/other non-index columns remain compared. Existing index definitions, constraints, relation kind, ownership, ACL, role, function, sequence, settings, count and version checks remain. Applying the comparison to both expected and actual evidence preserves old format-1 backup compatibility. No ERP migration or migration guard changes.

Read-only comparison of the actual failed evidence confirmed complete equality after removing only those internal index attributes. Files: index-internal-name-differences.json and backup-index-metadata-proof.json in the same evidence directory. The new routine PostgreSQL test must also prove a renamed-column backup restores and that actual column renaming or loss of index uniqueness still fails verification.

Primary reference: https://www.postgresql.org/docs/17/catalog-pg-attribute.html

## Scheduled failures must replace old success status

The old PowerShell wrapper could jump to its catch block on native stderr before writing last-run.json. A previous successful result could remain visible. The wrapper now captures the native process's actual exit code and stdout/stderr explicitly, then atomically records the result in finally, including failures. Secret connections remain process-environment values and are not command arguments.

The disposable scheduler witness also assumed LastRunTime must follow its explicit Start-ScheduledTask timestamp. StartWhenAvailable can legitimately start the freshly registered task earlier. The observed disposable task was Ready with LastTaskResult=1, but the test kept waiting. The corrected witness checks failure status on its uniquely named fresh task without that invalid timestamp precondition.

After recording the real failure, only the waiting diagnostic helper and its already-failed disposable task were stopped/removed. The parent test then failed and cleaned up its own database. Evidence: diagnostic-task-cleanup.json. No live task or database was changed.

The first focused Debug run passed 10/12 and caught a new PowerShell atomic-file replacement bug in both status writers: an untyped null backup-file argument became an invalid empty path. An isolated Windows PowerShell probe reproduced the failure and verified Language.NullString.Value as the correct null argument. Both writers were corrected; the failed focused TRX is retained as backup-monitor-debug.trx. This was found before any deployment or commit.
## Stronger final acceptance

The final provider restore witness must make the original disposable identity database unavailable before the recovered login. Otherwise a wrong connection could accidentally authenticate against the source. It must retain the same Staff and Approver subjects and signing keys, pass password/TOTP login, and rerun ERP OIDC/mapping checks.

The initial emulated startup built PostgreSQL support in 313.577 seconds before JVM/schema/realm startup. The local adapter caches only the resulting server image between runs; identity data remains in PostgreSQL. The emulated startup allowance is 40 minutes, consistent with the earlier container tooling. This is not a routine suite performance measurement.

The corrected results below supersede the diagnostic failures. A targeted recovery test does not replace full routine-suite acceptance before the item commit.


## Corrected Debug recovery result

The corrected Debug witness passed 1/1, zero failures/skips. VSTest duration: 25m10s; measured test-process wall time: 1,514.7212698 seconds (25.245 minutes), excluding the build. TRX: local-evidence/item16-local-production/final-debug/keycloak-recovery-debug.trx. Detailed evidence: local-evidence/item16/recovery-8a0f743a64ff474daa673ab0657d2a8a/result.json.

The scheduled task returned zero and produced a VERIFIED format-2 bundle. Expected and restored counts: 3 realms, 4 users, 5 credential rows. A fresh cluster restored the deployment configuration fixture, and the original identity database was shut down before the recovered provider started. Staff password login, Approver password/TOTP login, unchanged subjects/signing keys and the production ERP OIDC/mapping checks all passed after recovery.

This witness uses a Linux Keycloak container with a dedicated Windows PostgreSQL 17 source and recovery cluster. It is not certification of a complete Linux production appliance. The configuration fixture is a small keycloak.conf; TLS fixture files remain separately mounted. The production site's actual TLS/configuration recovery must be rehearsed before deployment. The first-month operations handoff now selects a dedicated Windows PostgreSQL instance to match this proof; choosing Linux PostgreSQL instead requires an additional platform-specific restore rehearsal. No production setup or live database is changed by this evidence.

The Release recovery result follows below; full routine-suite acceptance is recorded separately.


## Corrected Release recovery result

The Release witness also passed 1/1, zero failures/skips. VSTest duration: 22m20s; measured test-process wall time: 1,346.0320515 seconds (22.434 minutes), excluding the successful 35.01-second build. TRX: local-evidence/item16-local-production/final-release/keycloak-recovery-release.trx. Detailed evidence: local-evidence/item16/recovery-c9167d067bf540b0806a144bbe7d53fa/result.json.

It repeated the scheduled backup, fresh-cluster recovery, original-source shutdown, password/TOTP login, unchanged subjects/signing keys and production ERP mapping checks. All passed; the restored credential count was 5, matching the source. The same topology and configuration-fixture limitations stated above apply.

Full routine Debug passed 844/844, zero failed/skipped, with all eight external witness gates disabled. Measured process wall time: 1,476.3495798 seconds (24.606 minutes); VSTest duration: 24m31s. TRX: local-evidence/item16-local-production/routine-debug/full-suite.trx. Debug contains three existing DEBUG-only identity/bootstrap cases in addition to the 841 expected Release cases (836 prior Release cases plus five new routine regressions). The routine Release build passed with zero warnings/errors in 33.14 seconds.

The first full Release process ended without a completion record or TRX; its cause is not established and it is not counted as a result. Its partial console log and interruption record are retained. The complete Release rerun passed 841/841, zero failed/skipped: 1,603.7944354 seconds (26.730 minutes) process wall time, VSTest duration 26m27s. TRX: local-evidence/item16-local-production/routine-release-retry/full-suite.trx. Both complete routine runs are under 30 minutes.


## Commit acceptance and deployment boundary

Full TRX comparison confirms the three additional Debug cases are DevelopmentBootstrapRollsBackSessionAuthorizationAndTemporaryRoleAfterCeremonyFailure, DevelopmentBootstrapUsesProductionFunctionDropsRoleAndRefusesReplay, and DevelopmentWorkflowIdentitiesConvergeLegacySubjectsAndRefuseUnrelatedMappings. All passed. The five added routine cases cover configuration-file recovery/refusal, internal-index-label portability with real structural mismatch refusal, failed-backup status replacement and operational monitor failure detection. Existing mixed-format retention regression also passed.

Backend Item 16 acceptance is complete for this package. Expected ERP business-row changes: zero. No migration, owner-database build, provisioning, production task/service installation, or ERP HTTP route/response change was performed. Recovery used disposable identity clusters with 3 realms, 4 users and 5 credential rows before and after. The generic Docker controller is syntax-checked; this machine executed the owned QEMU adapter. The disposable VM is no longer running; its disk and private evidence are retained.

The owner accepted hostname, TLS, independent backup destination and production login screens as outstanding site installation. The selected first-month topology and platform-specific recovery limitations remain in the operations handoff. The next item is A1 intercompany; the exact GRN-versus-QC ownership-transfer event has been asked explicitly and will not be silently chosen.
