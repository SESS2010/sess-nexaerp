# DESKTOP-SPF5420 deployment changes: regression acceptance

21 September 2026. Both full routine suites completed before the hosting and Installer
code commits. Builds passed with zero warnings/errors; no test failures or skips.

| Configuration | Executed / passed | Failed / skipped | Test-process wall time | Acceptance build time |
| --- | --- | --- | --- | --- |
| Debug | 1026 / 1026 | 0 / 0 | 3633.956s (60m34s) | 3.337s incremental |
| Release | 1023 / 1023 | 0 / 0 | 3436.957s (57m17s) | 57.577s |

A separate initial Debug build passed in 5m18.73s with zero warnings/errors.
Full TRX files: `local-evidence/server-package/regressions/Debug/full-Debug.trx`
and `local-evidence/server-package/regressions/Release/full-Release.trx`.
Build/test logs and process timings are retained alongside them. All eight opt-in
heavy witness properties were false, as specified by routine-tests-and-witness-gates.md;
those deliberately separate witnesses are absent from discovery, not counted as skips.
The suites were sequential, with no overlapping disposable-cluster witness or rebuild
of shared output. An isolated frontend build ran separately during Debug.

Compiled source inputs were frozen for both runs and checked unchanged afterward.
Source index SHA256: `003c5dda796370da7d5f890e7abee3b6b87effa6b0ba9d967fe242ad87405cc9`.
See `server-package-source-hashes.json` and `compiled-source-confirmation.json` in the
same evidence directory. Packaging-only edits pinned/exported the frontend branch and
removed trailing whitespace; they did not change compiled .NET inputs.

The new routine coverage checks SPA deep links/static assets while preserving API and
health 404 responses, and pre-migration empty-schema principal provisioning/replay with
runtime DDL/function-execute refusal. The SDK-free witness independently covered empty
DEMO and go-live-named databases through all 130 migrations and no-op replay, with
RECONCILED/VERIFIED and zero stock movements; only its disposable cluster was used.
The final package must use the exact separately witnessed bundle and Installer assembly.

The frontend developer branch `0c59254f58bd49fc13a8b919387ba0cf5a1d9988` was fetched
and built successfully (151 modules, npm audit 0 vulnerabilities). Vite reported its
existing >500kB chunk warning. The branch still uses Debug-only login endpoints;
production OIDC integration/configuration and the field demo remain blocking gates.
The package is a candidate, not a completed field installation.

The package verifier accepted intact files and rejected changed payloads, changed
MANIFEST, missing files and extra files. PowerShell syntax checks passed. The final
package additionally requires a complete file-by-file verification against its MANIFEST.

Earlier frontend handoff: **ActualBomEntryView gains a string Provenance field**, with
payment-free viewer-scoped text, in `812db3199c6152f6399d494ab567ea897eab88bb`.
The category command block is in that same commit, included in the user's pushed
`f5356ec683a3bdea8540d1b413dfa5d46ceba75a`. These full suites include those changes.

No owner database, SQL Server/SOLIDWORKS service, Windows installation, production
backup task, or server firewall was changed by this laptop work. Field runtime patch
versions, TLS/SCM reboot, shared workload, another-PC sign-in, opening ceremonies and
off-machine backup remain server witnesses described in server-deployment.md.
