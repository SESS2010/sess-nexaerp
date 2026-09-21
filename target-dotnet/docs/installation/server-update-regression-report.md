# Consolidated server update: regression acceptance, 21 September 2026

Full routine suites completed BEFORE committing the backup code. Backend compiled
source and deployment script hashes match local-evidence/server-update-20260921/source-hashes.json;
no code changed during these runs. Documentation/configuration refinements were
reviewed independently. No API C# or migration source changed in this update.

| Configuration | Passed / executed | Failed / skipped | Build | Test process |
|---|---|---|---|---|
| Debug | 1026 / 1026 | 0 / 0 | 258.682 seconds | 3512.143 seconds |
| Release | 1023 / 1023 | 0 / 0 | 262.770 seconds | 3742.292 seconds |

Both builds exited 0, no warnings/errors. No test filter. All eight optional heavyweight
witness build flags were false, matching the documented full ROUTINE suite; separate
opt-in failure/volume/Keycloak/lifecycle witnesses are not claimed rerun.
Evidence: local-evidence/server-update-20260921/{Debug,Release}/full-<configuration>.trx,
timings.json, source-hashes.json and acceptance.json. One disposable cluster at a
time; no owner database, server service, account, firewall or certificate was changed.

New Windows PowerShell transfer module: 16 synthetic checks passed for intact copy,
exact replay, corrupt remote refusal before local retention, keep-two behaviour,
minimum retention, unexpected entries, overlapping/foreign/unowned roots, bundle
escape, insufficient/invalid capacity, valid capacity, traversal/duplicate manifest
entries and junction refusal. All deployment scripts passed PowerShell parsing.
This is filesystem evidence, not a real SMB/share/Task Scheduler witness. JSON parses,
realm/client/issuer/callback alignment and local server-document links passed.

D1 frontend contract was immediately committed/pushed at c9ac742; D2-D5 separately
followed. Production frontend source remains pending at feature/frontend 0c59254,
so release remains a candidate. Final artifact migration proof is identified by the
MANIFEST/build report, not a claim of field acceptance. Native Keycloak 26.7.4,
Java 17/Procrun 1.6.1 service, TLS on eleven PCs, MFA enrollment, receiver DESKTOP-AP
health/availability/share credentials, capacity and signed-out schedules remain field
checks. Provider binaries were not installed here. Publisher digests are recorded.

The earlier ActualBomEntryView.Provenance field addition and category block remain
in 812db3199c6152f6399d494ab567ea897eab88bb, pushed through the earlier package chain.
This update adds no API response field. Capacity admission checks are not a filesystem
quota against concurrent database/WAL growth; inadequate capacity blocks backup
admission/go-live. D:/E: on the server are prohibited until later clearance.

Automatic review rejected a combined commit/push as too broadly grouped for the
item-by-item instruction. Follow-ups are separated into local commits for user push;
no force push or alternate publishing route is used.
