# Reachability: classification of the 24 V3 authority diagnostics

The 24 failures are useful evidence, not 24 independent defects. They repeat across two companies and include reader failures cascading from the absence of a command actor. Twelve runtime checkpoints passed before this graph failed. This report classifies the original retained V3 output; it does not claim reachability acceptance.

| Requested group | Original diagnostics | Interpretation |
|---|---:|---|
| Real unreachable operations/records | 16 | 14 concern current item approval/merge; 2 concern the unpublished UOM decision draft, not a deployed independence rule. |
| Test models authority incorrectly | 4 | Vendor qualification paths that nominate MD as verifier. |
| Seed/permission configuration dead ends | 4 | MD-created vendor qualification has no independent seeded approver; two overlapping diagnostics per company. |
| Total | 24 | Counts are diagnostic messages, not unique defects. |

## Current item gaps: 14 diagnostics

Two actual operations, item approval and duplicate merge, have no callable seeded actor in either company. The endpoints/service explicitly require STORES_MANAGER or PURCHASE_MANAGER; current page approval grants belong to TD/MD (and unassigned legacy/admin roles). The graph correctly intersects these conditions. This is not repaired by adding another employee with the same existing role grants.

The 14 messages comprise four operation failures (two operations times two companies), eight downstream input-reader failures, and two approval-gate failures. The item readers are not thereby proved independently broken: with no eligible command actor, no reader can be callable by that actor. The generic gate names SESS-25 because its assertion stops at the first failing creator, not because that employee is uniquely at fault.

Source: [item approval](../../src/SESS.NexaERP.Api/Endpoints/InventoryEndpoints.cs), [merge role check](../../src/SESS.NexaERP.Infrastructure/Stores/EfEstimatedBomService.ImportMerge.cs). No authority was widened in this audit. The intended approval authority must be made consistent across page permissions and service rules.

## Four graph modelling errors: impossible MD-verifier paths

V3 checks the page grant and its known service-role table. That table had no VendorQualification branch, so MD full-control was taken as verification authority. The retained `rev869b_guard_qualification_lifecycle` trigger permits verification only with ACCOUNTS_HEAD or TECHNICAL_DIRECTOR; MD alone cannot execute that transition. Creator TD/verifier MD and creator Purchase Manager/verifier MD are therefore impossible paths, each reported once per company.

Source: [database lifecycle guard](../../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BControlledMutationSql.cs), verification branch; [endpoint independence checks](../../src/SESS.NexaERP.Api/Endpoints/Rev869AConfigurationEndpoints.cs). This is a test model omission of a database-enforced business rule, not permission to remove independence.

Before editing the test, the error and its reason were reported to the owner. The isolated V4 draft adds the actual verification and approval role restrictions; it does not change production grants, employee assignments, guards or independence assertions. The graph still asserts that no failures exist: known gaps are not converted into an expected-success test.

Correcting this model should also reveal a missing independent verifier for TD-created qualifications. The old graph incorrectly supplied MD for that role. Therefore removing four impossible paths must not be reported as four business defects fixed. V4 recheck results are recorded below when available.

## Four seed/configuration diagnostics: MD-created qualification

The graph correctly sees SESS-02 as the sole seeded page-authorized qualification approver. The endpoint and database both prohibit the creator from approving, and require the approver to differ from the verifier. A qualification created by SESS-02 and verified by SESS-01 cannot complete. The generic no-independent-approver message and the specific creator/verifier message describe the same dead end in each company.

This is not proof that the whole qualification operation is unreachable: Purchase Manager SESS-15 -> TD SESS-01 -> MD SESS-02 is a valid authority chain. It is an unsafe set of allowed entry paths in the current seeded authority configuration. An approved preparer/verification route or a genuinely authorized independent approver is needed. Do not invent an employee, give TD approval permission without authority, or make the MD self-approve merely to turn the graph green. The unassigned legacy ACCOUNTS_HEAD verification grant is not an available employee and must not be silently treated as ACCOUNTS_MANAGER.

## Two real draft-only gaps: UOM conversion

The unpublished candidate added a decision endpoint restricted to MD and explicitly rejects the creator deciding their own conversion. The graph uses that same rule, so these two diagnostics are not a disagreement between the graph and its candidate service: an MD-created conversion has no different seeded MD to decide it.

However, independent UOM approval was introduced in this draft and has not been established as approved owner policy. The current published service has no conversion decision endpoint at all. Report that original missing exit separately; do not present the draft's MD self-preparation dead end as a deployed production defect or quietly remove the independence rule. Resolve the intended UOM lifecycle before accepting the candidate.

## Original diagnostics

- **F01 ? REAL_CURRENT_CODE**: SESS_PVT_LTD /api/v1/inventory/items/{code}/approve input Version has no explicitly bound reader callable by a command actor (masters.items)
- **F02 ? REAL_CURRENT_CODE**: SESS_PVT_LTD /api/v1/inventory/items/{code}/approve input $route:code has no explicitly bound reader callable by a command actor (masters.items)
- **F03 ? REAL_CURRENT_CODE**: SESS_PVT_LTD /api/v1/inventory/items/{code}/approve masters.items:approve
- **F04 ? REAL_CURRENT_CODE**: SESS_PROPRIETORSHIP /api/v1/inventory/items/{code}/approve input Version has no explicitly bound reader callable by a command actor (masters.items)
- **F05 ? REAL_CURRENT_CODE**: SESS_PROPRIETORSHIP /api/v1/inventory/items/{code}/approve input $route:code has no explicitly bound reader callable by a command actor (masters.items)
- **F06 ? REAL_CURRENT_CODE**: SESS_PROPRIETORSHIP /api/v1/inventory/items/{code}/approve masters.items:approve
- **F07 ? REAL_CURRENT_CODE**: SESS_PVT_LTD /api/v1/inventory/items/{sourceItemId:guid}/merge input SurvivorItemId has no explicitly bound reader callable by a command actor (masters.items)
- **F08 ? REAL_CURRENT_CODE**: SESS_PVT_LTD /api/v1/inventory/items/{sourceItemId:guid}/merge input $route:sourceItemId has no explicitly bound reader callable by a command actor (masters.items)
- **F09 ? REAL_CURRENT_CODE**: SESS_PVT_LTD /api/v1/inventory/items/{sourceItemId:guid}/merge masters.items:approve
- **F10 ? REAL_CURRENT_CODE**: SESS_PROPRIETORSHIP /api/v1/inventory/items/{sourceItemId:guid}/merge input SurvivorItemId has no explicitly bound reader callable by a command actor (masters.items)
- **F11 ? REAL_CURRENT_CODE**: SESS_PROPRIETORSHIP /api/v1/inventory/items/{sourceItemId:guid}/merge input $route:sourceItemId has no explicitly bound reader callable by a command actor (masters.items)
- **F12 ? REAL_CURRENT_CODE**: SESS_PROPRIETORSHIP /api/v1/inventory/items/{sourceItemId:guid}/merge masters.items:approve
- **F13 ? REAL_CURRENT_CODE**: Clause 3: SESS_PVT_LTD Item created by SESS-25 has no reachable approver.
- **F14 ? SEED_CONFIGURATION**: Clause 3: SESS_PVT_LTD VendorQualification created by SESS-02 has no reachable independent approver.
- **F15 ? MODEL_MISMATCH**: Clause 3: SESS_PVT_LTD qualification creator SESS-01, verifier SESS-02 has no third independent approver.
- **F16 ? SEED_CONFIGURATION**: Clause 3: SESS_PVT_LTD qualification creator SESS-02, verifier SESS-01 has no third independent approver.
- **F17 ? MODEL_MISMATCH**: Clause 3: SESS_PVT_LTD qualification creator SESS-15, verifier SESS-02 has no third independent approver.
- **F18 ? REAL_DRAFT_ONLY**: Clause 3: SESS_PVT_LTD UomConversion created by SESS-02 has no reachable independent approver.
- **F19 ? REAL_CURRENT_CODE**: Clause 3: SESS_PROPRIETORSHIP Item created by SESS-25 has no reachable approver.
- **F20 ? SEED_CONFIGURATION**: Clause 3: SESS_PROPRIETORSHIP VendorQualification created by SESS-02 has no reachable independent approver.
- **F21 ? MODEL_MISMATCH**: Clause 3: SESS_PROPRIETORSHIP qualification creator SESS-01, verifier SESS-02 has no third independent approver.
- **F22 ? SEED_CONFIGURATION**: Clause 3: SESS_PROPRIETORSHIP qualification creator SESS-02, verifier SESS-01 has no third independent approver.
- **F23 ? MODEL_MISMATCH**: Clause 3: SESS_PROPRIETORSHIP qualification creator SESS-15, verifier SESS-02 has no third independent approver.
- **F24 ? REAL_DRAFT_ONLY**: Clause 3: SESS_PROPRIETORSHIP UomConversion created by SESS-02 has no reachable independent approver.

## Validation and scope

Original V3: clean Debug build, focused runtime witness failed at the final authority graph after twelve checkpoints; full reachability acceptance is not claimed. V3 source hashes and output remain retained. Classification does not modify any business row or frontend contract. The V4 diagnostic uses a fresh disposable Windows/PostgreSQL17 database, current chain and repository seeds; it does not reproduce the field pre-75 database name, starting state, role history or supplied dump line endings. The exact field dump/globals witness remains pending. No owner database was used.

V4 status at publication: Debug build passed with zero warnings/errors. The first graph process ended without a TRX or exit marker; a single-diagnostic retry is running. No revised failure count or V4 pass is claimed. The original 24-message classification above is a source-and-retained-evidence audit.
