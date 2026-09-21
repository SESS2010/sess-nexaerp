# Assisted setup wrappers for the 1 October launch

TD decision: no new setup screens before launch except Accounts commercial verification,
which the frontend developer implements after login. SESS prepares/owns its inputs; Stores
uses governed warehouse/rack workbook imports, and configuration uses the existing APIs.
The broad Configuration Page remains deferred. These wrappers never connect to PostgreSQL.

## What is delivered

`tools/setup/Invoke-Setup.ps1` with `SetupOperator.psm1` is one reviewed runner with these
separate operation modes; one invocation is one kind/action, company and employee session:

| Kind | Actions | Operator/order |
|---|---|---|
| Warehouses | Template, Import, Read, Submit, Approve | Stores downloads/imports/submits; different TD reads/approves |
| RackBins | Template, Import, Read, Submit, Approve | Same, after warehouses are active/approved |
| ConditionLocations | Create, Read | Stores creates; independent TD reads and signs review |
| CategoryRoutes | Create, Read | Stores creates after locations; TD reads/reviews ELE/FAB/REF in each company |
| TaxRules | Create, Read, Approve | Accounts creates; different TD/MD reads/approves |
| VendorQualifications | Create, Read, Verify, Approve | Purchase creates; different TD verifies; different MD approves |
| Identities | Create, Read | SESS-12/IT after Installer bootstrap; approved recipients only |
| Scopes | Create, Read | SESS-12/IT; existing department/company assignment required |

No arbitrary endpoint, bearer-token input, user/role override, stock-moving operation,
UOM-conversion write, party/company relationship write or SQL escape hatch is provided.
Only Create for new configuration is supported; corrections/closures/revocations are not
silently implemented as updates. Use the existing governed workflows or stop for review.

Exact [business columns and input forms](setup-data/README.md), including both warehouse
and rack Excel schemas, are available now. JSON examples deliberately contain invalid
placeholders, null decision versions or unfilled GST rates. Fill and review them; do not
run example business values as if approved. Duplicate each example separately per company.
Every real row/action gets a new GUID OperationId; preserve it, file and evidence for replay.

## Keycloak authentication: real person, not an asserted identity

Run on the named operator's trusted Windows office PC with Windows PowerShell 5.1 and the
server CA trusted by Windows. No SDK/Python/Node is needed for execution. Tests/builds stay
on the development laptop. Close any prior wrapper; loopback port 8765 must be free.

The runner opens the system browser against the fixed Staff or Approvers issuer on
`https://192.168.68.130:8444`, requests authorization code with S256 PKCE, fresh state,
`prompt=login`, `max_age=0`, and exchanges the code itself over trusted TLS. The verifier
and access token remain in this process only. No password grant, client secret, service
account, copied token or employee-subject selection is accepted. It uses access tokens,
not ID tokens. Callback listens only on 127.0.0.1:8765, checks state and returned issuer
when present, and times out. HTTPS redirects are not followed by the HTTP client; TLS
certificate validation is not disabled. This follows the native-app pattern in
[RFC 8252](https://www.rfc-editor.org/rfc/rfc8252.html).

**One required Keycloak client setting, before rehearsal:** the server identity maintainer
adds the exact additional redirect `http://127.0.0.1:8765/callback/` to the existing public
clients `nexaerp-staff` and `nexaerp-approvers` in their respective realms, preserving the
SPA redirects. No wildcard, client secret, implicit/direct grant, service account, token
exchange or authentication-flow override. Do not add a wildcard Web Origin; the CLI token
exchange is not browser JavaScript. Keep S256 required, API audience `nexaerp`, azp binding
and scope `nexaerp/access`. Preserve the mandatory Approvers password+OTP browser flow.
This narrowly specified loopback exception is for the operator CLI, not an internet HTTP
callback; no LAN firewall port is opened for it. Existing realm exports are not overwritten
or re-imported over populated realms to make this change. Remove the additional callback
when the assisted-entry period ends if the CLI is retired.

After login the API validates issuer/signature/audience/client/scope/expiry, resolves the
mapped employee, company, assignments and roles, and enforces permissions on every call.
The wrapper checks `/api/v1/session/me` before writes: EmployeeCode must equal the expected
operator, OrganizationId the selected company and IdentityIssuer the selected realm.
`ExpectedEmployeeCode` is an assertion that fails on mismatch, not impersonation.
`Company` selects only a company the authenticated subject can resolve. Changing JSON
cannot grant authority. TD, MD, Accounts and other privileged roles use Approvers with MFA.
Each checker starts their own invocation/login; never share an open console or credentials.
Like all bearer-token clients, a compromised PC/account remains outside this guarantee;
identity administrators remain trusted to maintain correct approved mappings.

## Run order and concrete command

Server agent first confirms the running API service is targeting the intended DEMO database
from its installed connection/ExpectedDatabase configuration, with health/readiness and
D5 receipts. The CLI's `ConfirmedServerDatabase` is an explicit acknowledgement, **not a
remote database identity proof or routing switch**. The same origin is reused at cutover:
never infer DEMO from the hostname. Stop if the server target has not been confirmed.

On Stores' PC, with a reviewed copy of the warehouse Template plan:

```powershell
powershell -NoProfile -File .\tools\setup\Invoke-Setup.ps1 -PlanPath .\Warehouses.template.json -ExpectedEmployeeCode SESS-41 -Realm staff -ConfirmedServerDatabase sess_nexa_erp_DEMO -EvidenceDirectory .\setup-evidence\demo-stores
```

Without `-Apply`, this validates the plan locally only: no login, network or writes. Add
`-Apply` after review to authenticate and download the template. Fill the template, use
the Import plan, then Read. Prepare Submit plans with the current versions. TD uses a
separate evidence folder/login and Approve plans with refreshed versions. Repeat for racks.
Use the same command shape with the appropriate Kind/Action plan for each configuration
operation; Accounts/TD/MD choose `-Realm approvers` and their own employee code. Never use
SESS-41 as a universal login. The identity roster supplies actual Purchase/QC employee codes.

Input plan and workbook hashes bind each operation. The runner writes a PENDING receipt
before a POST, saves returned IDs, then rereads the API and verifies fields/status/version
before marking VERIFIED. It reads all pages. VERIFIED means that operation read back
correctly; a created Pending Approval rule is not yet Approved, and a future-dated row is
not yet effective today. Independently check the intended opening/1 October dates.

The runner uses only REJECT_ENTIRE_FILE for imports and retains the batch read-back;
any invalid/rejected/not-imported rows stop the workflow. Import is not submission or
approval. Record read-backs and lifecycle decisions remain separate employee actions.

Reusing an unchanged VERIFIED operation is read-only verification. Changed plan/workbook
or employee with the same OperationId refuses. A lost response, failed read-back or other
uncertain PENDING receipt refuses automatic replay, even for endpoints supporting keys.
Use a fresh Read plan, keep all evidence, and have the maintainer reconcile with server
audit/command/import receipts; do not delete the pending receipt or invent a fresh key to
force a possibly duplicated operation. Expired-token/403/409 failures stop, not auto-retry.
Keep evidence and input folders restricted to the operators/TD/IT; no tokens are written.
A Read action replaces the latest snapshot for that kind/company in its evidence folder.
Use a new evidence folder for each independent TD review, and archive its snapshot with
the signed register before another Read; retain the original operation folder for replay.

Condition-location and category-route APIs do not have an approval stage. Their independent
TD Read receipt plus signed review register is the explicit second-person operational
control. Link original input/hash, maker receipt, TD read receipt, effective dates and
review signature. Do not falsely call these server-enforced maker-checker approvals.
The tax, qualification and master lifecycle decisions retain their existing server guards.

## DEMO rehearsal handoff: pending server readiness

Run on `sess_nexa_erp_DEMO` only after Keycloak, exact CLI callbacks, trusted certificates,
D5 bootstrap/mappings/scopes and named employee sign-ins are confirmed. Never use a
production owner database for rehearsal or insert records by SQL to make this pass.

1. Verify no token -> 401; unmapped user refused; privileged Staff account -> MFA_REQUIRED.
   Sign in as each real employee, verify session/me employee/company/realm. Attempting
   another ExpectedEmployeeCode must fail before writes. Record each participant.
2. Download warehouse/rack templates, import SESS's DEMO data as Stores. Test invalid-row
   reject-entire-file and wrong-company references. Submit as Stores, self-approve must
   fail, approve as TD. Confirm Active/Approved; read back IDs and versions.
3. Stores creates AVAILABLE/QC_HOLD/PENDING_RETURNABLE_DC locations and canonical routes.
   Test overlap/wrong-condition/wrong-company refusals in DEMO. TD reads and signs review.
4. Accounts creates tax rules, self-approval refused; separate TD/MD approves. Purchase
   creates qualification; creator verify/approve and TD self-final-approve must refuse;
   three separate employees complete it. Use new current versions at each stage.
5. Check unchanged verified replay writes nothing. Exercise a controlled lost-response
   scenario in DEMO; pending journal must block retry and require read-back reconciliation.
6. Read-only ledger check: configuration/import/lifecycle work did not create stock
   movements. Verify dates/coverage and preserve receipts. Only the accepted DEMO walk
   may then test transactions; production retains BOTH opening ceremonies first.

Field witness must record the actual source commit, service/database confirmation,
operator IDs (not tokens), input hashes, operation/record IDs, maker/checker evidence and
results. No field rehearsal was performed by writing these tools. If the existing API
refuses required setup, stop and report it rather than widening permissions or patching DB.


## Deferred items: concrete dependency check

No unconditional conversion or party/company-link entry requirement was found for opening
stock and the normal first-GRN purchase flow. Keep launch data in the item's base UOM.
There IS a conditional hard dependency: mixed-UOM material requests, estimated BOM lines
and fitments require an effective approved conversion. If SESS's first-day line UOM differs
from its item base UOM, flag that exact item/document immediately; do not fake the quantity,
rate or unit to suppress validation. Code references: EfMaterialIssueService.RequestCommands,
EfEstimatedBomService.Helpers and EfFitmentActualBomService. Party-company tables exist,
but the inspected launch services do not establish a mandatory manual relationship-entry
step; verify shared-party visibility in DEMO. No conversion/relationship wrapper is included.
