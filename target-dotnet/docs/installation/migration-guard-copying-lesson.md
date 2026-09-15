# Migration guard copying: lesson from the SESS-name defect

The copied prohibition first appeared at c3e4902 (13 September 2026, 17:35 IST) and last spread at 1c847a8 (14 September, 09:24 IST): about 15 hours 49 minutes. It was present in thirteen deployment guards. dd992ea removed only the two production target names after explicit owner authorization and verified the actual backup under the exact target name.

When copying a guard, review every inherited assumption against the destination operation. A test's protection against connecting to live/source data belongs in the test harness. A deployable migration must retain PostgreSQL system-database refusals, version checks and its actual structural/authority preconditions; it must not silently blacklist the installation target.

For future migration review, inspect the full generated deployment script for target-name refusals as well as the changed source. Prove upgrades from the customer's migration-history state in an isolated instance using the exact database name, migration login and owner role. File counts and renamed-copy success do not establish that proof. Keep function-body, ownership, ACL, trigger and rollback checks intact when correcting an unrelated assumption.
