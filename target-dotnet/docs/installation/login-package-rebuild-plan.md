# Package rebuild for production login: plan (not built)

Written on the night of 24-25 September 2026 at the Technical Director's request. **Do not
build it yet:** the login acceptance is not finished. This says what goes in, what must be
proven first, how long it takes, and what is needed from the frontend developer.

## Update, 25 September: what changed after the Technical Director's answers

The plan below is kept as written on the night of 24-25 September. These points override it.

- **Prerequisite 1 (#33) is met differently.** #33 was fixed on the server on 24 September in
  the Admin Console. The server agent's `Step4-Verify-RealmFlows.ps1` returned PASS, and Step 6
  and 6F are done. `Repair-ApproverOtpFlow.ps1` is not run. `Read-KeycloakLiveConfig.ps1` is
  removed, because Step 4's script is the canonical verifier and there is to be only one.
- **Identity tools: inside the package (decided).** The row below now means the tools that are
  left in `tools/identity`:
  - `Test-KeycloakRealmImport.ps1`;
  - `Repair-ApproverOtpFlow.ps1`, packaged with its README line saying **do not run**, kept
    only in case the import test shows the importer loses the OTP step.

  No read-back verifier is packaged.
- **Prerequisite 4 moves.** The package HEAD is no longer the #31 converter commit. Two code
  changes are decided, each with its own full cycle:
  - zone-less timestamps read as IST with a warning naming the field;
  - the #34/#35 DC validation.

  A third is proposed: the login-enabled default migration, if approved. The acceptance that
  counts is the one at the final HEAD. **If the migration is approved, the head moves to 131**
  and the bundle's content changes; the builder proves it on a disposable cluster as before.
- **Ask 4 of the frontend developer changes.** After #34/#35 the DC field rules answer 400, not
  409. The contract change goes to the Technical Director, who tells the developer.

## Update, 26 September: builder mode and a second build for the DC screen (approved)

Approved by the Technical Director on 26 September. The frontend developer reports that the DC
screen is built from 29 September to 1 October, after the 28 September package. **Two
production-login builds** are therefore planned.

### The builder change (tools only, no suite; about 2-2.5 h with a dry build)

`tools/deployment/Build-DeploymentPackage.ps1` gains:

1. **`-ProductionLogin`**, mutually exclusive with `-Candidate`. It needs `-FrontendRef` as a
   full 40-character SHA: no default and no branch name.
2. **A frontend gate after `npm run build`.** The build is refused unless:
   - the exported source and `dist/` contain no `/api/v1/dev`, no `nexaerp.dev.` key and no
     `DevelopmentToken`;
   - `package.json` depends on `oidc-client-ts`;
   - `dist/` carries `https://192.168.68.130:8443/oidc/callback`, `/oidc/logout-callback` and
     the authority `https://192.168.68.130:8444`;
   - no `.env*.local` file was in the export.
3. **Readiness `LOGIN_CANDIDATE_FIELD_WITNESS_PENDING`** in the MANIFEST, and a README header
   that says to serve the web only after the frontend developer's server-login witness.
4. **`installer/tools/identity/`** holds `Test-KeycloakRealmImport.ps1`, and
   `Repair-ApproverOtpFlow.ps1` with a `DO-NOT-RUN.txt` beside it. No read-back verifier is
   packaged.
5. **The package folder is `<head>-web-<first 12 of the frontend SHA>`.** Today it is `<head>`
   alone, so a second package at the same backend HEAD would be refused. The no-overwrite
   rule stays.
6. **`-SameBackendAs <build-1 MANIFEST>`** refuses the build unless every `api/`, `installer/`
   and `migrate/` file hash, and the migration head, is identical to build 1. "Only the
   frontend changed" is then a checked fact.
7. **Every existing check stays:**
   - clean tree;
   - SDK-free migration proof;
   - bundle and Installer hashes;
   - no overwrite;
   - `Verify-Package.ps1`.

   A dry build goes to `C:\SESS-Deploy-dry\` only, and is deleted after its report. The dry
   build also proves the refusals: the development-login frontend `0c59254`, a branch-name
   ref, and a missing ref.

Open for the Technical Director: should the builder also refuse a frontend SHA that is not on
`origin/feature/frontend`? Recommended: yes.

### Build 1: 28 September

- **Backend:** `main` at the build, including migration 131 if it is accepted by then.
- **Frontend:** the developer's witnessed login SHA.
- **Prerequisites:**
  - migration 131 accepted and on `main`;
  - `prove_migrations.py` re-run for the head on a disposable cluster;
  - the developer's named, witnessed SHA;
  - a green full cycle at the package HEAD.

### Backend freeze

**Backend code on `main` is FROZEN from the 28 September build until build 2**, except fixes the
Technical Director approves by name. Documentation may still land. A backend change in the
window would make build 2 more than a frontend swap. It would then need a full cycle (about
8 h) and a new migration proof, and would not fit 2-3 October.

### Build 2: about 2 October, the DC screen

**What it is:** the same backend as build 1, and only the frontend replaced by the developer's
witnessed DC SHA. It is built with `-ProductionLogin -SameBackendAs <build-1 MANIFEST>`.

**Prerequisites:**

1. Build 1 is installed, and the frontend developer's login witness on the server is done.
2. The same backend HEAD as build 1, which the freeze above keeps. The DC contract, `b7f64a9`,
   is already in build 1.
3. **The DC SHA is pushed to `origin/feature/frontend`, with the developer's witness:**
   - dispatch and signature for both natures;
   - a 400 with the named field highlighted;
   - a 409 sentence shown;
   - a same-key retry;
   - `DeliveredAt` in UTC.

   The witness runs **against DEMO or the developer's own database, never production**. There
   are no probes on production before both ceremonies.
4. The login still works in that SHA. The builder's frontend gate runs again, and one Staff and
   one Approvers sign-in are re-witnessed after the install.
5. **Rollback:** build 1's package stays untouched. Going back means re-pointing the web to it.

**Dates, set by the Technical Director:**

| When | What |
|---|---|
| **1 October 18:00** | Target for the witnessed DC SHA |
| **2 October 12:00** | **Hard deadline.** After it, build 2 moves past go-live and the DC screen goes live later |
| 2 October | Build 2 on the laptop: about 10 minutes, the manifest comparison, the report |
| **Evening of 3 October** | Install window, by the server agent. Not during a setup session; installing restarts `SESSNexaERP` |
| 5-6 October | Ceremonies. Nothing is installed during them |

## Where things stand

- **Last package:** `bcfee49` (21 September), readiness
  `CANDIDATE_BLOCKED_PRODUCTION_FRONTEND_OIDC_AND_FIELD_WITNESS`. Its frontend, `0c59254`,
  still used the Debug development login, which a Release API does not serve.
- **The production login now exists** on `origin/feature/frontend` at **`a95de9f`** (24 Sep,
  *production OIDC sign-in against the frozen D1 contract*):
  - authorization code with S256 PKCE through oidc-client-ts, both realms chosen at runtime;
  - tokens held in memory only;
  - the company chosen explicitly and sent as `X-NexaERP-Company`;
  - end-session logout;
  - every call through one `authorizedFetch`;
  - all `/api/v1/dev` calls removed.
- **Backend code since `bcfee49`:** only `9998369` (typed concurrency failures kept),
  `7003c02` (an infrastructure failure never reported as user error) and, pending tonight's
  acceptance, the #31 UTC converter. **No new migration.** The head stays at 130,
  `20260920210000_StockAdjustmentPosting`, so the migration bundle's content is unchanged.
- **The builder refuses a non-candidate package.** `tools/deployment/Build-DeploymentPackage.ps1`
  throws unless `-Candidate` is set, and hard-codes the readiness string above.

## What goes in

| Part | Source | Note |
|---|---|---|
| API and Installer | `main` HEAD after the #31 converter commit | Release, framework-dependent, as before |
| Migration bundle | same HEAD, head 130 | Proved again on a disposable cluster, from empty to 130, twice (DEMO-named and go-live-named) |
| Frontend | **the exact commit the developer witnessed**, `a95de9f` or later on `feature/frontend` | Only `src/SESS.NexaERP.Web` is taken from it (`git archive`), then `npm ci`, `npm run build` |
| Realm exports | `main` | With `loginWithEmailAllowed: false` (`54b5ba9`) |
| Identity tools | **add** `tools/identity/*.ps1` to `installer/tools/identity/` | Repair, live read-back and import test reach the server inside a hash-verified package instead of an ad-hoc copy |
| Run-in-progress signal | not packaged | Laptop tool only |

**Builder change needed first (tools only, no suite).** A `-ProductionLogin` mode sets the
readiness to `LOGIN_CANDIDATE_FIELD_WITNESS_PENDING`. It refuses unless the exported frontend
contains no `/api/v1/dev` string, no `nexaerp.dev.` key and no `DevelopmentToken` reference,
and does contain the OIDC client dependency. It also adds `tools/identity`. Estimate **1-2
hours** with a dry build.

## What must be proven before it is built

1. **Login acceptance on the server, witnessed by the Technical Director (#33):**
   - `Repair-ApproverOtpFlow.ps1` run with `-SignOutApproverSessions`, with the before and
     after steps recorded;
   - one fresh Approvers login asking for the password AND the code, and a wrong code refused;
   - `Read-KeycloakLiveConfig.ps1 -ApiAuthConfigPath <installed appsettings>` all PASS;
   - Login with email switched off in both realms.
2. **Keycloak import test** (`tools/identity/Test-KeycloakRealmImport.ps1`) run. If "after
   import" FAILs, the runbook's 30 September import gains a mandatory repair and read-back
   step **before** the package's documentation is frozen.
3. **The frontend commit is the witnessed one.** The developer names the exact SHA they
   signed in with against the server Keycloak, in both realms (see below). The package takes
   that SHA and no later one.
4. **Backend acceptance at the package HEAD:** full Debug and Release suites and the three
   gates, green, at the same source hashes as the package. Tonight's converter run is that
   proof, provided nothing but documentation lands between it and the build. Any code change
   after it means another full cycle, about 7.5 hours.
5. **Migration bundle proof on a disposable cluster** (the builder does this), then the
   independent verification of every manifest entry, as for `bcfee49`.

## How long

| Step | Time |
|---|---|
| Builder change and dry build | 1-2 h, tools only |
| Package build: publish, frontend build, bundle, proof, verification | about **10 minutes** (`bcfee49`: 22:45 to 22:52 plus verification) |
| Independent manifest verification and report | 30 min |
| **If any code changes after tonight's acceptance** | **+7.5 h** full cycle |

**Laptop total once the prerequisites hold: half a day**, most of it the builder change and
the report. Transfer and verification on the server are the server agent's, and are not
counted here.

## What is needed from the frontend developer before 28 September

1. **The exact frontend SHA to package**, pushed, and a statement that it is the one they
   witnessed signing in.
2. **Their witness against the server Keycloak** after the #33 repair, with TraceIds and no
   tokens:
   - a Staff sign-in and a company chosen;
   - an Approvers sign-in with OTP;
   - a TD, MD, Accounts Manager or CFO role over Staff refused with `MFA_REQUIRED`;
   - an unmapped subject refused with `EMPLOYEE_ACCESS_NOT_CONFIGURED`;
   - a token refresh;
   - logout through Keycloak.
3. **UTC on all 13 timestamp fields** (the table under #31), checked field by field, not
   "done globally". The converter is a safety net, not a substitute.
4. **What else is in that SHA:** whether the 11.2 vendor commercial verification screen (their
   launch deliverable) is in it, and whether the DC screen is. If the DC screen is, note
   #34/#35: its field rules answer 409, not 400.
5. **A clean build from a fresh clone:** `npm ci` and `npm run build` with no
   `.env.development.local`. Production must default to `https://192.168.68.130:8444`, as
   `a95de9f` says.
6. **The redirect and logout URIs the build uses**, exactly
   `https://192.168.68.130:8443/oidc/callback` and `/oidc/logout-callback`. These are the
   registered ones, and the read-back checks them live.
7. **Merge `main` into `feature/frontend`.** It is 19 commits behind, mostly documentation
   the developer should read, including the DC contract review and #31's field list. The build
   does not need the merge, since it takes only the Web folder, but the developer reading
   stale contracts does.

## Open question for the Technical Director

- Should the identity scripts travel inside the package (recommended above), or keep being
  copied by hand with a hash check?
