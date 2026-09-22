# Runtime-only Windows migration bundle

Choose a **framework-dependent win-x64 EF migration bundle**, `migrate/efbundle.exe`.
The server runs this executable with its installed .NET 10 runtime; it does not run
`dotnet ef`, compile source, install an SDK, or need network package restore.
The design-time factory reads `ConnectionStrings__NexaErp` and
`NexaErp__ExpectedDatabase` from environment. The migration login connection includes
`Options=-c role=nexa_erp_owner`; no secret appears in process arguments.

Build on the laptop after a Release build (not on the field server):
```powershell
$env:ConnectionStrings__NexaErp='Host=127.0.0.1;Port=1;Database=offline_generation;Username=nexa_erp_migration'
$env:NexaErp__ExpectedDatabase='offline_generation'
dotnet ef migrations bundle --no-build --configuration Release --project src/SESS.NexaERP.Infrastructure --startup-project src/SESS.NexaERP.Api --context NexaErpDbContext --target-runtime win-x64 --output local-evidence/server-package/efbundle.exe --force
```
Generation uses a deliberately non-serving loopback connection; it does not migrate
any database. Keep the bundle immutable after witnessing, and copy that exact binary
into the package. The package builder verifies its digest and migration source hashes.

The alternative generated `--idempotent` SQL failed the stored-function baseline guard
at `20260913020000_FitmentIssueHeaderLockOrder`: the Windows-generated SQL reintroduces CRLF
inside the expected-body literal while that guard normalizes the actual body to LF.
The retained byte-level comparison confirms the mismatch and equality after normalizing
both; no newline rewriting of the deployment SQL was accepted as a substitute for proof. No historical migration/guard was loosened to admit
it. The bundle retains normal EF execution and history-based no-op replay.

The empty-first installation rehearsal exposed two Installer problems, now corrected:
absent bootstrap/page tables were assumed during pre-migration provisioning, and the
per-schema default revoke did not remove PostgreSQL's global PUBLIC function-execute
default. Provisioning now handles the genuinely absent objects and preserves partial
state refusal; it revokes the default for the managed owner in the target database.
The new routine test checks empty-schema provision/status/replay, runtime DDL refusal
and execute refusal on a newly created owner function. The full chain checks all
normal ownership and grants after reconciliation.

Witness command (laptop only, one cluster at a time):
```powershell
python tools/deployment/prove_migrations.py --bundle local-evidence/server-package/efbundle.exe --installer local-evidence/server-package/installer/SESS.NexaERP.Installer.exe --evidence local-evidence/server-package/bundle-proof
```
The script creates its own unique loopback PostgreSQL cluster. It sequentially creates
`sess_nexa_erp_DEMO` and `sess_nexa_erp`, provisions the
empty schema/principals, runs to current head, reconciles (`RECONCILED`) and verifies
(`VERIFIED`), replays, compares complete migration history and audit counts, checks zero
stock movements, then drops only these disposable databases and stops its own cluster.
No owner database is opened by this witness.

Child PATH contains only a copied runtime root, PostgreSQL binaries and System32.
The runtime root contains dotnet.exe/host/shared frameworks, **no sdk directory**.
`dotnet --list-sdks` must be empty before any Installer/bundle run. Logs and result.json
are retained under the evidence path; the package includes the result as proof.json.
The actual counts, migration head, runtime versions and artifact hashes are recorded
there and in the completed deployment build report; no source command alone is proof.

Witness OS: Microsoft Windows 10 Pro, version10.0.19045/build19045 (recorded in
`local-evidence/server-package/witness-os.json`).

Limitations: the .NET SDK remains installed elsewhere on the laptop, excluded from the
child environment; this is not a fresh SDK-uninstalled OS image. Laptop .NET 10.0.11
and PostgreSQL 17.10 differ from the field .NET 10.0.12/PostgreSQL 17.11. Trust-auth
loopback with synthetic credentials does not reproduce field password/TLS/network,
locale, storage, performance, 16GB shared-memory pressure, SOLIDWORKS coexistence,
SCM/certificate/reboot behavior, external OIDC or another-PC frontend access. These are
explicit field witnesses, not claims established by this isolated database proof.

References: [EF migration bundles](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying)
and [PostgreSQL default privileges](https://www.postgresql.org/docs/17/sql-alterdefaultprivileges.html).
