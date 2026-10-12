# Migration pre-merge checks

For every migration change, test the exact merge candidate in a disposable PostgreSQL database. Run the whole AdvanceMigrationSqlSyntaxTests class in Debug and Release, all migration lifecycle and round-trip tests, focused feature tests, the fast suite, and frontend TypeScript, all tests and production build. Preserve TRX paths and SHA-256 hashes. A focused filter alone does not replace the whole migration class. No competing builds or tests while the nightly runs.

Enable all three compile-time witness flags (`WorkflowWitness`, `ConcurrencyWitness`, `MigrationLifecycleWitness`) for the migration class checks in both configurations. MigrationLifecycleWitness alone omits workflow/payment and concurrency fixtures; those must be checked before pushing a migration candidate.
