# Frontend contract: vendor commercial verification

One small extension to the existing Vendor Detail screen, after production login.
No new backend field or endpoint is required by this contract. Source baseline is
MasterEndpoints.Rev869A.cs and the current vendor detail/action contracts.

## Request and authority

Show **Verify commercial details** to Accounts Manager with `masters.vendors:verify`.
Use the existing authenticated API client, the actual user's Keycloak ACCESS token and
selected-company `X-NexaERP-Company` header. Do not accept an employee/role/subject override.
Accounts must use the Approvers issuer and required MFA. Server permissions/role checks
remain authoritative; a visible button is not authority.

`POST /api/v1/masters/vendors/{URL-encoded-vendor-code}/verify-commercial`

```json
{ "Version": 3, "Remarks": "Commercial, GST and bank evidence checked; review reference ..." }
```

Version is the latest GET vendor detail Version (3 is illustrative). Remarks are required
and trimmed. Ask the user to confirm the evidence reviewed; disable duplicate submissions
while the request is in flight. This action is not idempotent by a supplied header: do not
blindly retry a lost response or invent a supported idempotency key.

## Response and behaviour

Success: HTTP 200, existing `VendorDetail` shape, including the new current Version,
ApprovalStatus=`Pending Approval`, VendorStatus=`Pending Approval`. Persist the returned
version/reload the vendor and its approval history. Show **Commercial details verified;
awaiting final MD approval**. Never label the vendor Active/Approved solely from this action.
Backend stores CommercialVerificationStatus=Approved, CommercialVerifiedBy/At and clears
RequiresReverification; it adds AccountsVerify approval/configuration/audit evidence.

**The current VendorDetail DTO does not expose those commercial-verification fields.**
Do not invent them in TypeScript or infer them from Pending Approval alone. Use the
successful action response plus refreshed `/approval-history` AccountsVerify evidence for
feedback; keep later controlled-detail changes visible through existing history. A durable
new verification-status badge would require a separately agreed additive DTO contract.
For this small launch change a pending-vendor action and successful-action confirmation
are sufficient. Re-verifying again can create another audited version; it is not a replay.

Use a pending vendor after maker submission for the normal workflow. IT/TD creates/submits;
Accounts verifies; MD performs existing final approve with refreshed Version. The API
requires Accounts verification before final approval and checks the configured final
approver role. Final master approval has the current-maker self-approval guard. Commercial
verification itself checks Accounts role/grant, version and remarks; it does not add a
separate generic creator-identity guard. Keep the agreed three-person workflow; do not
claim the verification endpoint enforces more than its actual contract.

Errors: 401 means authenticate again; 403 means role/grant/MFA/mapping refusal, do not
retry under another person's login; 400 means required remarks/invalid request; 404 vendor
not found; 409 stale version, refresh and require a new deliberate decision. On timeout or
5xx, refresh detail/history first; do not automatically resubmit. Surface a safe error and
correlation details without logging tokens or bank values. Backend-controlled GST/PAN/bank/
commercial changes require Accounts re-verification and final approval again.

## Acceptance cases for the frontend developer

1. Accounts/Approvers login, correct company, pending vendor and current version: success,
   refreshed Version and AccountsVerify history; vendor still pending MD approval.
2. Blank remarks, stale version, wrong role or unauthorised company and privileged Staff identity refused;
   no role/subject header can turn the caller into Accounts.
3. Double click suppressed; uncertain response resolved by read-back, not automatic retry.
4. MD cannot finish an unverified vendor; after verification, different MD can approve.
5. Controlled details changed afterward require a new verification/approval cycle.

Estimated frontend effort: 0.5-1 day after login. No frontend changes made in this task.
[Backend action](../../src/SESS.NexaERP.Api/Endpoints/MasterEndpoints.Rev869A.cs).
