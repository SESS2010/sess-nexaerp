# Frontend handoff acceptance - 21 September 2026

Implementation commit: `812db3199c6152f6399d494ab567ea897eab88bb`.

**ActualBomEntryView gains a field: `Provenance`**, computed on the server for
Production/Stores using the four requested receipt/opening-stock sentences. It never
adds paid/unpaid/part-paid status or queries payment allocations. The commercial
dossier retains its full sentence. Opening dates use the report calendar and the
authorising employee code. No schema migration is needed.

The same commit contains identical category command blocks in the category correction
guide and go-live runbook: command, owner principal, mutations, refusal guards, replay
and post-run checks. The reported 1,087-item preflight is documented; no field repair
was executed by this work.

Server/OS documentation commit: `c7832b3ee9147dc9a439278a28a5f503e3557eea`.
It records the selected desktop, separate-disk backup layout and restored laptop role,
plus Windows 11 eligibility, backup-before-upgrade, post-upgrade prerequisite checks
and commercial ESU prices before API deployment. No remote OS/database change was made.

## Full routine suite evidence

Both solution builds passed with zero warnings/errors. Runs were sequential, with no
test-name filter and all eight deliberate witness build properties false. The retained
`run-regressions.ps1` contains the exact commands: solution build with `--no-restore
-p:UseSharedCompilation=false -m:1`, then solution test with `--no-build --no-restore`
and separate TRX loggers/results directories. Times below are process wall times.

| Configuration | Executed | Passed | Failed | Skipped/not executed | Build time | Test time |
|---|---:|---:|---:|---:|---:|---:|
| Debug | 1024 | 1024 | 0 | 0 | 267.856 s | 3442.474 s |
| Release | 1021 | 1021 | 0 | 0 | 260.227 s | 3496.850 s |

Both full routine suites pass correctness but **miss the under-30-minute timing target**.
These are not the earlier five-test targeted result or the opt-in heavy witnesses.
Tracked `src` and `tests` content matches `812db31` (`git diff --quiet 812db31 -- src
tests`, exit 0). Runs used the existing worktree with the server documentation update
and unrelated documentation edits; this is not a separate clean-checkout certification
of each commit.

In both configurations the category fixture edited all 1,368 imported items through
the API, verified repair audits, retired three aliases and removed them from active
choices, proved replay idempotence and preserved receipt snapshots. Separate
`category-Debug.json` and `category-Release.json` preserve these results.

## Retained local artifacts

`local-evidence/provenance-812db31/` contains both build/test logs, both TRX files,
`timings.json`, the runner and category evidence. TRX SHA-256 hashes:

- `local-evidence/provenance-812db31/Debug/full-Debug.trx`: `7bd7fe9a117d5f1581c584d8554bb008f0146fc670cec5fc1d78b4f81b42afd8`.
- `local-evidence/provenance-812db31/Release/full-Release.trx`: `b781d6ae51402e4349ccf0a9e9087dcc0db7e21e50eb1343c872b5b4592c8f5c`.

Push status at report creation: not pushed. Automatic approval review requires
explicit user approval before sending these commits to origin/main.
