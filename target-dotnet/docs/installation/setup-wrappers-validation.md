# Assisted setup wrappers: local validation

Validated on the development laptop, 22 September 2026. No owner database or server business setup was changed.

## Results

| Configuration | Executed | Passed | Failed | Not executed | Build seconds | Test-process seconds |
|---|---:|---:|---:|---:|---:|---:|
| Debug | 1026 | 1026 | 0 | 0 | 282.492 | 3807.690 |
| Release | 1023 | 1023 | 0 | 0 | 271.163 | 3881.245 |

Both are the full unfiltered routine suite. All eight deliberate witness compilation gates were false; those opt-in witnesses are not claimed by these counts. Debug and Release ran sequentially, with no second manually started disposable cluster.

TRX evidence remains under local-evidence/setup-wrappers/<configuration>/full-<configuration>.trx. SHA-256:

- Debug: `0bef6ca16533ea0ccbdbb0e4442a4fecfff730fd475c8b94fa986edbbde689b8`
- Release: `a37dafa2de8723a7521e60dc7930929d34104decee5c6116d12d807ecf5f8bc6`

Focused wrapper checks: 32 PowerShell checks passed, including PKCE, callback state/issuer refusal, resolved employee/company checks, input controls, read-back/version checks, pagination and HTTP failure handling. Five runner safety tests passed: successful operation and read-only replay, changed-input refusal, uncertain response, failed read-back, and wrong employee before any write. These tests use in-memory/fake transports, not a field Keycloak session.

The five executable/test source hashes were recorded before regression and checked unchanged afterward. Both workbook header lists were extracted from the existing C# definitions: 11 warehouse columns and 16 rack/bin columns. All 25 JSON examples parsed; placeholders deliberately prevent accidental application. Source/Markdown reference and whitespace checks passed.

The DESKTOP-AP scheduled task NexaERP nightly witnesses was temporarily disabled to prevent the 01:00:30 launch overlapping these runs, then restored after routine-test cluster cleanup. Its schedule/actions were preserved. That missed nightly witness is not claimed as executed or passed; pause/restore receipts are retained beside the TRX evidence.

## Scope and field boundary

Added employee-authenticated PowerShell setup tools, input/review templates, the small vendor verification frontend contract and D5/runbook handoffs. The package builder includes the runner/module in future packages. No C# API field, route, database schema or frontend screen changed. The previously built package was not rebuilt in this task and does not contain these new tools.

DEMO rehearsal remains pending the server agent confirming Keycloak, the exact loopback callback registration, trusted CA, installed API database target, and named employees completing their own logins. Local tests do not reproduce the server installation, MFA browser interaction, actual assignments/scopes, or end-to-end human maker/checker decisions. Follow setup-operator-wrappers.md and retain its witness register before production assisted entry.

Condition-location and category-route APIs do not implement approval states; their independent TD read-back and signed review are operational controls. Other decisions retain existing API maker/checker enforcement. Installer bootstraps SESS-12 only; D5 must complete and verify the remaining identity/scope roster. No stock-moving probe is allowed on production before BOTH opening ceremonies.

RESULT_REPORTED_PENDING_WITNESS
