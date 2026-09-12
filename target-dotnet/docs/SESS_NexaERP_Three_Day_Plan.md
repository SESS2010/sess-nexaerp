# Three Days Alone — Sunday 13 to Tuesday 15 September

Date: 12 September 2026, evening
For: A. Paramananthan, Technical Director

The IT team returns Tuesday morning. Three days to push the backend as far as
it will go.

---

## The agenda, and whether it is realistic

**Your plan:** finish the backend, then hand it to the IT team so they work
only on the frontend.

**Is it realistic?** For what 1 October needs — yes. For the whole 50-item
specification — no, and it does not have to be.

| | |
|---|---|
| Items acceptance-complete | 16 of 50 |
| **Blocking 1 October** | **2 remain: authentication and reports** |
| Not blocking 1 October | 32 items — DC custody, vendor rating, cycle counting, QC check sheets, dashboards, ISO |

**Three days is enough for the two blockers, plus concurrency, failure
behaviour, automated backup and import purchase.** That is six items.

It is not enough for the other 32, and pushing for them would spread the three
days thin across work that can wait until SESS is already using the system.

**Target for Tuesday morning: every item that blocks 1 October is done, and
the IT team gets a backend they can build against without asking you anything.**

---

# THE DAILY RHYTHM

The same three steps every morning. Fifteen minutes, except Tuesday.

## Step 1 — What happened overnight

```powershell
cd C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet
git --no-pager log -6 --oneline
git --no-pager status --short | Select-Object -First 8
git --no-pager log origin/main..HEAD --oneline
```

Read three things:
- **new commits** — what Codex finished
- **uncommitted files** — what it is working on now
- **the third list** — what is not yet on GitHub

## Step 2 — Push, without stopping Codex

```powershell
cd C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet
git push
git --no-pager log origin/main -1 --oneline
```

**`git push` does not build.** It cannot disturb Codex. Safe at any moment.

**This is the step that matters most.** A commit only on your laptop is a
commit one disk failure away from gone. Yesterday two sat unpushed for hours
while Codex worked; that was a risk taken for no reason.

## Step 3 — Space and health

```powershell
Get-PSDrive C | Select-Object @{n='Free_GB';e={[math]::Round($_.Free/1GB,1)}}
Get-Service postgresql* | Select-Object Name, Status
```

**If C: drops below 20 GB, clear temp:**

```powershell
Get-ChildItem "$env:TEMP" -Recurse -Force -ErrorAction SilentlyContinue | Where-Object { $_.LastWriteTime -lt (Get-Date).AddHours(-6) } | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
```

Three days of disposable PostgreSQL clusters will fill the disk. Check it every
morning.

---

# SUNDAY 13 SEPTEMBER

## Morning — start the run

1. The three steps above
2. **Send Codex the three-day prompt** — it is waiting and every hour without
   it is four to six engineering days lost
3. Leave it alone

## What Codex should reach today

| | |
|---|---|
| Item 16 | authentication report, then the build |
| Item 15 | the first four or five reports |

## During the day

**Nothing.** Do not build, do not test, do not run migrations. Codex has the
machine.

If you want to check progress, the three read-only commands above are safe.
Nothing else is.

## Evening

Push again if new commits appeared.

---

# MONDAY 14 SEPTEMBER

## Morning

The three steps. Push whatever is new.

## What Codex should reach

| | |
|---|---|
| Item 15 | the remaining reports, ending with component ancestry |
| Item 25 | concurrency — eleven users at once |

## The one thing to read carefully

**Item 25 reports seconds.** When it lands, read the numbers rather than
skipping to the next item.

Eleven people will use this on your laptop from 1 October. If a posting takes
four seconds under eleven concurrent users, that is a decision about the server
PC, and it needs making in September, not October.

## Evening

Push.

---

# TUESDAY 15 SEPTEMBER — the full witness

Today you stop Codex and do the complete verification. Allow ninety minutes.

## 1. Stop Codex

In VS Code, press **Stop**. Then check the worktree is clean:

```powershell
cd C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet
git --no-pager status --short | Select-Object -First 8
```

If files are modified, Codex was mid-item. **Stash them** so they are not lost:

```powershell
git stash push -u -m "wip at Tuesday witness" -- src tests
```

## 2. Clear stale processes

```powershell
Get-Process testhost,dotnet,VBCSCompiler,MSBuild -ErrorAction SilentlyContinue | Stop-Process -Force
dotnet build-server shutdown
```

A hung `testhost` locks a DLL and the build fails for no real reason. This cost
an hour on the 12th.

## 3. Build and test

```powershell
cd C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet
dotnet build SESS.NexaERP.slnx -c Release --nologo
dotnet test SESS.NexaERP.slnx -c Release --no-build --nologo 2>&1 | Select-String -Pattern "Failed:|Passed:"
```

**`Failed: 0` or stop.** Do not apply a migration on a red suite.

Twenty-five minutes.

## 4. Back up both

```powershell
& "C:\Program Files\PostgreSQL\17\bin\pg_dump.exe" -h localhost -p 5432 -U postgres -d sess_nexa_erp -F c -f "$env:USERPROFILE\Desktop\before_tuesday.dump"
& "C:\Program Files\PostgreSQL\17\bin\pg_dumpall.exe" -h 127.0.0.1 -U postgres --globals-only -f "$env:USERPROFILE\Desktop\globals_before_tuesday.sql"
Get-ChildItem "$env:USERPROFILE\Desktop\*before_tuesday*" | Select-Object Name, Length
```

Both files, both non-empty.

## 5. Apply migrations as the migration principal

```powershell
cd C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet
$env:ConnectionStrings__NexaErp = 'Host=127.0.0.1;Port=5432;Database=sess_nexa_erp;Username=nexa_erp_migration;Password=SessMigr@2026Chennai#Str0ng!;Options=-c role=nexa_erp_owner'
$env:NexaErp__ExpectedDatabase = 'sess_nexa_erp'
dotnet ef database update --project src\SESS.NexaERP.Infrastructure --startup-project src\SESS.NexaERP.Api
git push
```

**Not as postgres.** PostgreSQL gives a new table to whichever role created it,
so a migration run as postgres leaves drift.

## 6. Reconcile — mandatory, not optional

```powershell
cd C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet
$env:ConnectionStrings__NexaErpInstaller = 'Host=127.0.0.1;Port=5432;Database=sess_nexa_erp;Username=postgres;Password=<your postgres password>'
$env:NexaErp__ExpectedDatabase = 'sess_nexa_erp'
dotnet run --project .\src\SESS.NexaERP.Installer\SESS.NexaERP.Installer.csproj -c Release -- database-principals provision
dotnet run --project .\src\SESS.NexaERP.Installer\SESS.NexaERP.Installer.csproj -c Release -- database-principals status
```

**`RECONCILED` then `VERIFIED`.** Anything else and you stop.

This is the step that prevents the 34 postgres-owned tables the IT team found
on their machine.

## 7. Restore the stash and resume Codex

```powershell
cd C:\Users\User\Documents\Codex\2026-07-03\see\target-dotnet
git stash pop
```

Then press **Resume** in VS Code.

## 8. Hand over to the IT team

Send them:
- the `origin/main` SHA, **verified after push, not before**
- what changed that affects them — authentication contract, report endpoints
- what is still coming

---

# WHAT TO TELL THE IT TEAM ON TUESDAY

```
Three days of backend while you were away. origin/main is <SHA>.

  authentication - the real sign-in path exists alongside the dev token
  ten reports, ending with the component ancestry dossier
  concurrency proven with eleven simultaneous users
  failure behaviour, and automated backup

Every item that blocks 1 October is now done on the backend. From here the
frontend is the only thing between us and a date.

Your order:
  1. the grid - still the only number I cannot estimate
  2. opening stock ceremony screen
  3. vendor advance and payment
  4. master data import - template, upload, errors.xlsx round trip
  5. the ten report screens

Read docs/SESS_NexaERP_Plan_To_1_October.md. Your section says 17 to 23 days of
screen work in the days remaining, and it has no slack. If the grid says the
frontend is not close, we start Purchase only on 1 October and Stores follows
mid-October. That decision gets made on 25 September.
```

---

# THE THREE RULES FOR THESE THREE DAYS

**1. Push every morning. Never build while Codex runs.**

`git push` is safe at any moment. `dotnet build`, `dotnet test` and
`dotnet ef` are not — they fight Codex for the same files and the same
PostgreSQL.

**2. One full witness, Tuesday. Not three.**

Codex builds and tests every item before committing, and its numbers have
matched yours every time it has been checked. Trust the daily push to protect
the work; verify properly once.

**3. Read the concurrency numbers when they arrive.**

Everything else can be skimmed. That one tells you whether your laptop can
carry eleven people, and if it cannot, the server decision moves from October
to this week.

---

# WHAT SUCCESS LOOKS LIKE ON TUESDAY MORNING

| | |
|---|---|
| Items acceptance-complete | 20 to 22 of 50 |
| **Blocking 1 October** | **zero on the backend** |
| Suite | green, around 850 tests |
| origin/main | every commit pushed |
| Your database | every migration applied, reconciled, verified |
| The IT team | can build without asking you anything |

Then the frontend is the only question left — which is exactly where you wanted
to be.
