# Production login proof: 26 September 2026

Frontend `feature/frontend` after merge `732b4ba` (origin/main fully merged). API from the
backend worktree at `origin/main` `b7f64a9`, OIDC mode (`NexaErp__AllowDevelopmentAuthentication=false`),
running as `nexa_erp_runtime` against the local database `sess_nexa_erp` (130/130).
Keycloak 26.7.4, local, **HTTPS on 8444** with the localhost dev certificate
(thumbprint `19344C4D9CFB974CD784F7A460A657F9B34D19F2`, trusted for the current Windows user only),
as approved by the TD on 26 Sep (report item 3.2). No `RequireHttps` exception, no backend change.

## Results

| # | Item | Result | Evidence |
|---|---|---|---|
| 1 | Sign-in redirect | **YES** | Staff button → `https://localhost:8444/realms/staff/.../auth`, `client_id=nexaerp-staff`, `scope=openid nexaerp/access`, `code_challenge_method=S256`, redirect `http://localhost:5175/oidc/callback`. |
| 2 | Return (callback) | **YES** | Callback → company chooser listing SESS_PVT_LTD and SESS_PROPRIETORSHIP, "Signed in through Staff". |
| 3 | API accepts the token | **YES** | `GET /api/v1/session/me` 200; home page "Welcome, SARATH BABU K", SESS-25, PRODUCTION_MANAGER, SESS PVT LTD, 9 of 22 screens. Access-token claims: `iss https://localhost:8444/realms/staff`, `aud nexaerp`, `azp nexaerp-staff`, `typ Bearer`, `scope openid nexaerp/access`. |
| 4 | `EMPLOYEE_ACCESS_NOT_CONFIGURED` | **YES** | Before the mapping existed: 403, `Code EMPLOYEE_ACCESS_NOT_CONFIGURED`, screen "Your sign-in is not set up for this company" with trace. After it: the same for SESS_PROPRIETORSHIP, where SESS-25 is not mapped. |
| 5 | `PERMISSION_DENIED` | **YES** | SESS-25 opening `/accounts/vendor-bills`: 403, `Code PERMISSION_DENIED`, screen "You are not allowed to do this" with the server's reason. Different wording from #4. |
| 6 | Both companies | **YES (switch + isolation)** | Company switch from the user menu re-reads `session/me` under the new `X-NexaERP-Company` header: PROPRIETORSHIP refused (#4), back to PVT_LTD 200. A successful sign-in in PROPRIETORSHIP needs a second mapping; not made. |
| 7 | Refresh | **YES** | `refreshAccessToken()` through the refresh-token grant: new access token (`iat` 05:24:37Z → 05:26:01Z, lifetime 900 s), then `session/me` 200 with it. |
| 8 | Tokens never stored | **YES** | `localStorage` and `sessionStorage` both empty while signed in. |
| 9 | Reload | **As designed (TD accepted, 3.9)** | Reload → realm choice again; Keycloak SSO skipped the password; company chosen again. |
| 10 | Logout | **YES** | Sign out → `/login`; the next Staff sign-in asks for the password again, so the Keycloak session ended too. |
| 11 | Approvers + OTP (sess-14) | **PENDING** | Needs the user's phone to enrol the authenticator on first sign-in. Mapping is in place. |

## Setup done for this proof (local database only)

Each employee may hold one active HUMAN mapping per company, and SESS-25 and SESS-14 held one for
the development issuer `urn:nexaerp:development`, which the frontend no longer uses since `a95de9f`.
With the user's approval, as SESS-02 (MANAGING_DIRECTOR, `security.employee-identities` create +
deactivate) through the governed API, in SESS_PVT_LTD:

| Employee | Revoked (dev issuer) | Created (local Keycloak) |
|---|---|---|
| SESS-25 | `32914d78-3cbf-43b1-ab8e-a08a74ef20b8` | `b3ba8360-0cdc-4b74-89bb-c7187120e218`, issuer `https://localhost:8444/realms/staff` |
| SESS-14 | `784870c8-c81c-4dc1-8554-2a2795269b68` | `8fec0c93-6ff6-428a-aa51-475c9c22f37c`, issuer `https://localhost:8444/realms/approvers` |

SESS-01 and SESS-02 keep their development mappings for API-level administration. SESS-35 and
SESS-33 have no local Keycloak user yet; they are created before grid lines 11 and 12 on screens.

Also on 26 Sep: SESS-16 `TECHNICAL_ENGINEER` SUPPORT `21136f14` ended by SESS-01 (EffectiveTo
2026-09-26, TD item 3.5, finding #32) and read back.

## Defect found and fixed during the proof

With the API down, `session/me` returned 500 from the dev proxy and the screen said
**"You are not signed in. Your sign-in expired or was not accepted"**. That sends the user round
the login for a server fault. `AccessProblem` now words any 5xx as **"The ERP server did not
answer"** and says the sign-in is still valid. Witnessed with the API stopped, then "Try again"
recovered to the home page once it was back.

## Note for the server

The local realm login form reads "Username or email": the local copies keep Keycloak's default.
The TD's decision (24 Sep) is *Login with email: Off* in both realms on the server; check it there
by the live read-back, not from here.
