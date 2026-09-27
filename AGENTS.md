# AGENTS.md: SESS NexaERP

For any coding agent working in this repository. The .NET solution is in `target-dotnet/`: ASP.NET
Core 10, PostgreSQL 17 and Keycloak. Release 1 (Track A, Stores and Purchase) goes live on
**15 Oct 2026**.

**Read these first**, in the audit folder outside the repository
(`C:\Users\User\Documents\Codex\audit\`):
- `HANDOFF-TO-CODEX-20260927.md`: the full state, rules, queue, how-tos and known traps;
- `R1-plan-to-15-Oct.md`: the day-by-day plan and owners;
- `R1-branch-merge-migration-rules.md`: branches, the merge protocol, migrations and file ownership.

## Standing rules

**Branches and pushing**
- **Never push `main`.** Only the Technical Director (TD) moves main, and only to a
  green-nightly SHA.
- The integrator may push `integration/r1` and its own feature branches. **Never force-push
  anything.**

**Migrations**
- **Only the integrator writes migrations.** Each one needs:
  - `PostgreSqlClusterGuard.Require`;
  - a Down that refuses to lose data;
  - a runtime grant, plus an installer provisioning entry (`DatabasePrincipalProvisioningSql.cs`)
    for every new SECURITY DEFINER function;
  - a round-trip test.
- **Never edit a merged migration**; fix it with a new one. SQL-only by default.

**Daily rhythm**
- **18:00 merge cutoff.** A branch must be pushed and pass its focused tests plus the fast suite.
- **21:00 nightly**: scheduled task `NexaERP R1 nightly acceptance`, full Debug and Release suites
  with TRX plus the three witness gates, run on `integration/r1`.
- **07:00 report**: SHA, counts, GREEN or RED.
- Nobody pushes to `integration/r1` while the nightly runs.

**Tests and commits**
- **Focused tests per change.** Nothing reaches `main` without a green nightly.
- Fast suite:
  ```
  dotnet test tests/SESS.NexaERP.Tests --filter "FullyQualifiedName!~AdvanceMigrationSqlSyntaxTests"
  ```
- **One item per commit.** Report every SHA.
- **Park rule:** a change that is red again after its one re-run (two red nightlies) leaves
  `integration/r1` until its owner shows it green.
- Tests are never weakened to pass.
- **Disposable PostgreSQL only.** Never a real or live database.
- The fresh-company rehearsal (`FreshCompanyReachesAvailableStockThroughSeededStoresAuthority`) is
  the go-live proof. Fix defects; never work around them.

**Machines**
- **Never touch the server (DESKTOP-SPF5420) or any other PC.** Nothing runs against the server
  from this laptop, not even read-only.
- No development on the server PC. Server scripts come from the server agent only.

**Data and secrets**
- **Redaction grep before any `docs/reference/` or `docs/requirements/` commit.** Check the staged
  tree for passwords, personal e-mails, Sheet/Drive IDs, API keys, names, phones and bank numbers.
  Report the empty result.
- **Never commit** xlsx, backups, registers, e-mail lists, GST certificates, customer offers,
  workbook dumps, passwords or secrets.
  - The SMTP password never goes in chat or the repo.
  - `tools/identity/Repair-ApproverOtpFlow.ps1` is never run or packaged.

**Decisions**
- **Frozen decisions win.** Precedence:
  `docs/SESS_ERP_Stores_Full_Schema_Guideline.docx` >
  `docs/SESS_NexaERP_Answers_To_Open_Questions.md` > the item's spec > anything older.
  On a conflict, stop and report.
- **The TD answers business rules.** Never invent one; ask, with a proposed default.
- **Verify live, not intended:** configuration is proven by reading the running system back.
- **After the 7 Oct 18:00 freeze:** blocking fixes only, each approved by the TD by name.

## File ownership

| Owner | Files |
|---|---|
| TD | `src/**/Email/**`, `src/**/Endpoints/EmailEndpoints.cs`, `tests/**/Email*Tests.cs` (except `EmailOutboxTests.cs`). Branch `feature/email-lite`. |
| Frontend (ILAMPARUTHI, MAGESHWARI) | `feature/frontend`, `feature/print-layouts`. Not merged into `integration/r1`; the package builder takes a named frontend SHA. |
| Integrator | Migrations, `Outbox/`, `Tracking/`, the installer provisioning, and the shared `Program.cs`, `DependencyInjection.cs`, `*.csproj`, `appsettings*.json`. |

## Windows notes

- The shell is Windows PowerShell 5.1. Native stderr under `$ErrorActionPreference='Stop'` is
  fatal, so run long scripts as `powershell.exe -File … *> log`.
- Write files as UTF-8 without BOM and keep their existing line endings.
