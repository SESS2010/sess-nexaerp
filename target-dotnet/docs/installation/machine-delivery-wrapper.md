# Machine delivery wrapper proposal (consolidated F)

**PROPOSAL ONLY — DO NOT BUILD.** The TD will decide whether this is needed based on when the delivery challan screen lands. No implementation is authorized by acceptance passing. Estimate: 2-3 engineering days for the two actions,
strict input validation, retained journals, read-back and negative/replay checks;
field rehearsal needs deployed Keycloak and named Stores employees. Full regression
wall time and customer/TD acceptance are additional.

Reuse SetupOperator.psm1's pinned HTTPS endpoints and interactive Keycloak
Authorization Code + PKCE login, not a password grant, supplied token or service
identity. Each invocation confirms /session/me against the expected employee,
company and issuer; the expected employee argument is a check, never impersonation.
The API enforces issue permission and a FULL/TEMPORARY Stores Assistant, Executive
or Manager assignment. Read-back also needs machine-deliveries view permission.
No new role, bypass grant or maker/checker fiction: dispatch/signature are existing
Stores actions, not approval decisions.

One reviewed JSON operation per invocation, preview without network by default.
Dispatch data: company, acknowledged database, operation GUID/idempotency key,
job-order GUID and expected number, machine serial/model, customer name, DC number,
nature, purpose, dispatch date, expected return date where returnable, destination.
Read eligible jobs before dispatch; require the exact reviewed candidate and FAT READY.
NON_RETURNABLE requires CUSTOMER_PO_BASED and null expected return date;
RETURNABLE allows DEMO/TRIAL/JOB_WORK/SITE_WORK with a due date on/after dispatch.
The server rechecks linked customer PO, FAT, identity, uniqueness and dates atomically.
NON_RETURNABLE also requires an active MD notification recipient.

Signature data: same company/database/operator controls, existing delivery GUID,
expected DC/job/machine identifiers, delivered-at timestamp with offset, actual
customer signatory, local signed PDF/PNG/JPEG filename and independently recorded
SHA-256. Maximum 5 MiB, matching magic/content type. Send bytes as base64 for the
API byte[] contract. Employee uploads a genuine customer-signed document as themself;
this does not impersonate the customer or manufacture a signature.

POST /api/v1/stores/machine-deliveries/ uses DispatchMachineRequest.
POST /api/v1/stores/machine-deliveries/{id}/signature uses SignMachineDeliveryRequest.
GET /api/v1/stores/machine-deliveries/{id} verifies all business fields, actor,
company and retained signature metadata/hash. Signature download is a separate
reports.machine-dossier permission; do not grant it merely to operate this wrapper.
Machine DCs have no ItemId and do not create inventory postings; this is not a
loose-material DC workflow. Signature requires an existing Actual BOM.

Retain a pending intent BEFORE POST, stable key, exact plan/file hashes, resolved
employee and company; never tokens or passwords. A changed plan, file or actor
cannot reuse an operation. Verified replay is read-only. An uncertain POST must
retain its journal: permit only explicit same-key/same-body/same-actor recovery
using the server's existing receipt idempotency, followed by read-back. No automatic
retry, new key, journal deletion or direct SQL. Refuse extra fields/actor overrides.
The database argument only acknowledges a server-agent-confirmed target; it does
not select or remotely prove the database. First rehearse on DEMO; release normal
production operations only after both opening-ceremony receipts are accepted.

Acceptance: wrong employee/issuer/company/role; altered plan/evidence; invalid
nature/date/file; FAT/PO refusal; pending-response recovery; identical replay;
read-back mismatch; token secrecy; retained customer evidence. Offline tests do not
replace real named-employee DEMO login and server-authority witnesses.
