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
