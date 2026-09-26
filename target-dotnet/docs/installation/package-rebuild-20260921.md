# Rebuilt deployment candidate: clean setup and unset receiver

Package source commit: `bcfee49b148a154388a47428c76824322d21c357` (contains both `828816c` and `ac45aa8`).
Built UTC: `2026-09-21T17:21:57.8459561Z`.
Frontend: `0c59254f58bd49fc13a8b919387ba0cf5a1d9988`; remote branch checked on 21 September.

Package directory: `C:\SESS-Deploy\bcfee49b148a154388a47428c76824322d21c357\`
MANIFEST: `C:\SESS-Deploy\bcfee49b148a154388a47428c76824322d21c357\MANIFEST`
Separate digest: `C:\SESS-Deploy\bcfee49b148a154388a47428c76824322d21c357.MANIFEST.sha256`
MANIFEST SHA-256:
`447a09e22fb397849f258487ed793a481183a86dd9d57383033a99e7fe9ac141`

[Exact committed copy of MANIFEST](deployment-manifests/bcfee49b148a154388a47428c76824322d21c357.json).
This report commit is intentionally later than the immutable package source HEAD;
it does not change the package's identity or claim to be included inside it.

## Verification performed

- Release framework-dependent API and Installer publish succeeded; exact frontend source
  exported and `npm ci` / `npm run build` succeeded. Vite reported its bundle-size warning.
- New win-x64 migration bundle built, then proved on one owned disposable loopback cluster,
  databases created/dropped sequentially: DEMO and go-live-named disposable databases each
  migrated from empty to all 130 migrations, head `20260920210000_StockAdjustmentPosting`.
  Both RECONCILED and VERIFIED; replay preserved migration history/audit counts; both had
  zero stock movements. The owned cluster stopped; no postmaster.pid remained.
- Child PATH/runtime root excluded SDKs (`dotnet --list-sdks` empty). Witness used
  PostgreSQL **17.10** and .NET/ASP.NET Core **10.0.11**, not field 17.11/10.0.12.
  SDK is still installed elsewhere on the laptop. Trust-auth loopback does not reproduce
  field passwords/TLS/network/locale, disk pressure, engineering workload coexistence,
  SCM/reboot, another-PC browser access or production OIDC. These remain field witnesses.
- Builder verification and independent post-build Python verification passed all **171**
  manifest entries: byte lengths, SHA-256, exact file set, source head and separate digest.
  Published Installer assembly and bundle match their SDK-free proof hashes. The copied
  `Test-ProductionState.ps1` and `Test-DailyBackupState.ps1` match the committed sources.
- Verified package ancestry includes both required commits; packaged runbook contains the
  no-dump decision, setup checklist, the setup/training window and both ceremony gate, with
  no `[dump]` placeholder or attachment restore. No developer data is packaged.
- `DestinationHost` and both UNC roots use **RECEIVER_NOT_SET**. It fails the existing
  hostname guard. Receiver remains Ilamparuthi's PC; IT TEAM 2 is not treated as a valid
  hostname. Only an administrator configures the protected plan with the verified name.
  Current runner's refusal is generic, not a new RECEIVER_NOT_SET diagnostic code.
  Missing/failed/stale daily copy remains a Test-ProductionState failure; no silent fallback.

No executable code changed since `828816c`: only documentation, README template and the
example plan's unset value. The receiver correction's existing full routine TRX results
are Debug **1026/1026** and Release **1023/1023**, zero failed/skipped; they were not rerun
or presented as new results for this documentation-only revision. Fresh evidence here is
publish, bundle proof and package/content verification. See
[receiver regression record](receiver-correction-validation.md).

Local evidence: `local-evidence/server-update-final-bcfee49/` (`result.json`, witness/build
logs, `package-verification.json`). Source scripts remain the committed deployment tools.
Nothing was installed or applied on DESKTOP-SPF5420 or an owner database.
Unrelated working-tree edits were excluded; packaged installation docs came from Git HEAD.

## Readiness

`CANDIDATE_BLOCKED_PRODUCTION_FRONTEND_OIDC_AND_FIELD_WITNESS`.
The packaged frontend still uses Debug login; Release does not expose that login API.
This rebuilt artifact fixes the receiver/runbook contents, not the production login gate.
Do not approve DEMO/go-live merely because hashes pass. Confirm the receiver hostname,
healthy capacity/share/account and daily copy witness before accepting backups.

Verify after transfer, using this separately recorded digest:

```powershell
powershell -NoProfile -File .\installer\tools\Verify-Package.ps1 -Root . -ExpectedHead bcfee49b148a154388a47428c76824322d21c357 -ExpectedManifestSha256 447a09e22fb397849f258487ed793a481183a86dd9d57383033a99e7fe9ac141
```

Follow the packaged README and server deployment runbook for application order.
The setup-screen report is a separate source review/proposal, not an implemented fallback.

RESULT_REPORTED_PENDING_WITNESS
