# Tools

Operational helpers, verification scripts, setup wrappers and historical revision tools. Start with the [R1 code map](../docs/CODE-MAP.md), [fresh database runbook](../docs/installation/go-live-fresh-database-runbook.md) and [routine tests / witness gates](../docs/installation/routine-tests-and-witness-gates.md).

| Location | Contents / starting point |
|---|---|
| `deployment/` | `Build-DeploymentPackage.ps1`, `Verify-Package.ps1`, migration proof `prove_migrations.py`, verified backup transfer and backup scripts. Packages use named backend/frontend SHAs. |
| `setup/` | Operator wrapper `Invoke-Setup.ps1`, `SetupOperator.psm1` and safety tests. |
| `identity/` | Disposable realm verification `Test-KeycloakRealmImport.ps1`. `Repair-ApproverOtpFlow.ps1` must never run or be packaged. |
| `tests/` | Import-generator tests; other tests live beside tools or under the .NET test project. |
| Root `Run-WorkflowWitness.ps1` | Three acceptance witness gates; follow the disposable test procedure. |
| Root `Show-RunInProgress.ps1` | Local run progress inspection. |
| Root `generate-item-import.py` | Item import SQL generation; workbooks are never committed. |
| Root backup / identity / production-state helpers | Backup, recovery and state checks; read parameters and runbooks before selecting a command. |
| Root `*rev86*` scripts and SQL | Historical apply/correction/rollback, isolated verification and control-plane workflows; presence does not authorize execution. |

Nothing runs against the server or another PC from this laptop, even read-only. Server scripts come from the server agent. Use disposable PostgreSQL for tests. Never put passwords in commands, docs or Git. Windows PowerShell 5.1 is the shell; long approved scripts run via `powershell.exe -File ... *> log` as documented. This documentation task runs no setup, migration or deployment tool.
