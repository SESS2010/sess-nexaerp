# REV869A Management Decision and Implementation Scope Checkpoint

## 1. Checkpoint basis and status

This source-only checkpoint continues from clean commit `9ef229bf07c6ee7a25a0fd91be3e476a9f560223`. It records management decisions and freezes the proposed REV869A boundary. It is not an implementation.

Evidence reviewed includes the committed REV869 integrated Purchase + Stores plan and the current identity, employee, Purchase, master, inventory, authorization, API security, persistence, seed, and REV868/REV868C3 test sources.

REV869A is an additive contract-and-control foundation. It must preserve all REV868/REV868C3 PR, stock-check, reservation, `PendingRFQ` handoff, status, approval, audit, migration, and acceptance evidence. Existing accepted records must not be rewritten to manufacture new meanings.

## 2. Approved management decisions

The following decisions are approved and authoritative for future implementation:

1. A unique OIDC issuer/subject maps to exactly one active SESS employee.
2. Shared department, Purchase, Stores, QC, warehouse, or other human logins are prohibited.
3. Fixed employee-code approver fallbacks are removed; a missing, inactive, expired, duplicate, ambiguous, or self-approving manager mapping fails closed.
4. The canonical store hierarchy is `Warehouse -> Rack/Bin`.
5. `LocationKey` is derived from Warehouse and optional Rack/Bin identity and must not become a competing location master.
6. Every item has one Base UOM; conversions use a controlled UOM Conversion Master.
7. Tax/GST configuration is effective-dated and resolved from HSN/SAC, supply state, and vendor GST registration status.
8. `TaxableValue` and `TotalPayableValue` are separate immutable commercial snapshots on future commercial transactions.
9. Approval routing uses `TotalPayableValue` by default: `TaxableValue + GST + freight/charges - discount`.
10. Inventory valuation begins with weighted average.
11. QC tolerance is configurable by item or category, with item-specific configuration taking precedence.
12. REV869A does not duplicate Customer or Project masters.
13. A Vendor must be active and approved before RFQ or PO use.
14. Employee names and employee codes are not hard-coded into workflow logic.
15. All REV868/REV868C3 data, histories, constraints, and accepted behavior are preserved.

## 3. Rule-by-rule source decision record

### Rule 1 — Unique OIDC subject maps to one active SESS employee

- **Existing source state:** `ClaimsCurrentUser` chooses email, name identifier, or preferred username as a login string. `UserAccount` has a unique `LoginId` but no `EmployeeId`, issuer, or immutable OIDC subject. Employee has unique `EmployeeCode` and `LoginEnabled`, but the authentication boundary does not prove the claim maps to that employee.
- **Exact correction required:** Add an employee identity mapping keyed by normalized OIDC issuer plus immutable subject, unique both by issuer/subject and by employee for active human identity. Resolve each authenticated request to exactly one active, login-enabled, approved employee. Reject missing, duplicated, inactive, disabled, or ambiguous mappings.
- **Database/model impact:** Add `EmployeeIdentityMapping` with Employee FK, issuer, subject, identity type, effective dates, active flag, audit/concurrency fields, and filtered/ordinary unique constraints. Do not infer mapping from display name. Retain `UserAccount` for compatibility until an explicit deprecation decision; prevent it from bypassing employee resolution for human workflows.
- **API impact:** Add privileged identity-mapping list/create/update/deactivate endpoints and return a resolved employee context from the current-user service. Existing protected endpoints fail closed when employee resolution fails.
- **Permission/security impact:** Only narrowly privileged identity administrators manage mappings; no one may map two subjects to one employee or one subject to two employees. Authentication is followed by employee, role, department, and scope authorization.
- **Audit/history impact:** Record mapping create/change/deactivate, old/new issuer/subject metadata without tokens, actor employee/role, effective dates, reason, correlation, and all denied mapping/resolution attempts.
- **Migration risk:** Existing users may lack an unambiguous subject. Migration must add schema only and must not guess mappings. Production activation remains disabled until reviewed mappings exist.
- **Rollback requirement:** Before activation, the additive table can be removed if empty. After any mapping is used for audit attribution, preserve rows and roll forward; disabling the feature must not erase attribution.
- **Offline acceptance tests:** Unique employee and issuer/subject constraints in model; missing/duplicate/inactive/login-disabled/approval-invalid mappings fail; claims do not fall back to names; direct API is 401/403 as appropriate; concurrency and audit tests.
- **PostgreSQL acceptance tests required later:** Apply migration once in an isolated database; verify indexes/FKs/checks; reject duplicate subject and employee mappings; verify active/inactive resolution and rollback on a pristine migration-only target.

### Rule 2 — No shared department login

- **Existing source state:** `CreateUserAccountRequest` accepts arbitrary login, display name, and `UserType`. No database constraint prohibits department or shared human accounts. No seeded shared department account was found, but absence is not enforcement.
- **Exact correction required:** Define allowed identity types and prohibit shared human types/codes such as department, Purchase, Stores, QC, or warehouse accounts. Human authorization always resolves through Rule 1. Separately approved non-human service principals, if ever needed, cannot perform human approval/verification actions.
- **Database/model impact:** Add identity type/check constraints to the new mapping model; no new shared-account entity. Do not delete legacy accounts in this migration.
- **API impact:** Reject creation/activation of shared human identities and prevent identity endpoints from creating a human workflow account without an employee mapping.
- **Permission/security impact:** Prevent non-human identities from create/verify/approve/issue actions. Department is scope metadata, never an identity.
- **Audit/history impact:** Audit rejected shared-account creation/use with normalized reason; do not log secrets or token payloads.
- **Migration risk:** Legacy `UserAccount.UserType` values may be ambiguous. Inventory them later through approved isolated validation; do not mutate them automatically.
- **Rollback requirement:** Rollback must not re-enable shared human workflow access. If schema rolls back before activation, application denial remains until a safe replacement exists.
- **Offline acceptance tests:** Reject shared names/types case-insensitively; reject human actions by service identity; confirm unique employee identities still work across departments.
- **PostgreSQL acceptance tests required later:** Check-constraint and endpoint persistence tests for prohibited/allowed identity types; verify no shared mapping can become active.

### Rule 3 — Remove fixed employee-code approver fallbacks and fail closed

- **Existing source state:** Effective-dated `DepartmentApprovalMapping` and `PurchaseApprovalWorkflowStep` exist. Runtime helper fallback and REV868C3 fixtures contain `SESS-001` and `SESS-002`; `ApproverEmployeeCode` permits person-specific steps. Existing tests assert those fallback codes.
- **Exact correction required:** Remove runtime fixed-person fallback and resolve Department Manager from effective mapping and higher routes from active role/configuration. No hard-coded person identifiers. Missing, inactive, expired, overlapping, ambiguous, duplicate-person, or self-approval resolution returns a conflict/forbidden result without state mutation.
- **Database/model impact:** Retain historical workflow rows. For new effective workflow configuration, disallow person-specific resolver type; optionally retain nullable `ApproverEmployeeCode` only to read history, with application denial for active future steps. Do not rewrite REV868C3 histories.
- **API impact:** Approval/verification returns a stable fail-closed problem code and records a denied attempt. Management endpoints validate effective-date overlap and active employees.
- **Permission/security impact:** Page `CanApprove` is necessary but insufficient; actor must equal the uniquely resolved employee and must not be requester/creator/self-approver.
- **Audit/history impact:** Record resolver inputs, route, amount basis, mapping/version, candidate outcome, denial reason, actor employee/role, correlation, and timestamp. Denial never changes PR status.
- **Migration risk:** Removing seeded identities from historical fixtures would damage provenance. Only active runtime fallback is removed; historical employee data remains legitimate master data.
- **Rollback requirement:** Never restore person-specific runtime fallback. Roll forward if resolution defects appear; accepted historical rows remain readable.
- **Offline acceptance tests:** No employee code/name literals in workflow source; missing/overlap/inactive/self mapping fails closed; exact amount boundaries; denial audit; existing REV868 transitions remain valid when configured mapping exists.
- **PostgreSQL acceptance tests required later:** Effective-date overlap/unique-index behavior; inactive employee mapping rejection; no status/history mutation on failed resolution; exactly one denied audit row; configured approval succeeds once.

### Rule 4 — Canonical hierarchy is Warehouse to Rack/Bin

- **Existing source state:** `Warehouse` and `RackBin` exist; Rack/Bin has a required Warehouse FK and a unique warehouse/bin code. Purchase stock checks, reservations, and handoffs reference Warehouse and optional Rack/Bin. Warehouse holds default condition-location GUIDs without modeled RackBin navigation/FK enforcement.
- **Exact correction required:** Declare Warehouse and Rack/Bin as the only physical store hierarchy. Validate every Rack/Bin belongs to the selected Warehouse and every warehouse default receiving/accepted/hold/rejected/repairable/scrap identifier points to an active Rack/Bin in the same Warehouse with the matching material condition.
- **Database/model impact:** Add explicit self-consistent FK/navigation configuration or a normalized `WarehouseConditionLocation` mapping. Recommended REV869A model is the normalized mapping to avoid multiple nullable unverified GUID properties; preserve existing columns until data is validated and a later cleanup is approved.
- **API impact:** Master endpoints expose/validate warehouse condition locations and reject cross-warehouse, inactive, duplicate-condition, or incompatible Rack/Bin assignments.
- **Permission/security impact:** Warehouse/RackBin maintenance requires master permissions and location scope; scoped users cannot configure or view unauthorized warehouses.
- **Audit/history impact:** Record condition-location mapping changes, prior/new Rack/Bin, actor, effective state, reason, and denied cross-scope/cross-warehouse attempts.
- **Migration risk:** Existing default GUIDs may be null, orphaned, cross-warehouse, or condition-mismatched. Add schema without destructive conversion; later isolated preflight must fail on ambiguous backfill.
- **Rollback requirement:** New unused mappings may be dropped before transaction references. Do not delete or overwrite original default fields during REV869A.
- **Offline acceptance tests:** Same-warehouse and condition validation; unique mapping per warehouse/condition; inactive Rack/Bin rejection; scope filtering; existing LocationKey tests unchanged.
- **PostgreSQL acceptance tests required later:** FK/unique/check enforcement; cross-warehouse inserts fail; rollback preserves pre-existing Warehouse/RackBin and REV868 rows.

### Rule 5 — LocationKey remains derived

- **Existing source state:** `LocationKey` is persisted on stock-check, reservation, and handoff evidence and derived as `W:{warehouse-id}:B:{rack-bin-id-or-NONE}`. There is no separate Location entity.
- **Exact correction required:** Centralize deterministic LocationKey derivation/validation from WarehouseId and RackBinId. Never accept an arbitrary client LocationKey and never create a Location master/table.
- **Database/model impact:** No Location entity or table. Existing LocationKey columns and unique guards stay intact. Optional check/validation may verify stored keys for new writes without rewriting accepted rows.
- **API impact:** Inputs accept warehouse/rack-bin identity; responses may include derived LocationKey. Reject client attempts to supply a competing key.
- **Permission/security impact:** Authorization uses canonical Warehouse/RackBin FKs, not parsing a user-controlled string.
- **Audit/history impact:** Audit canonical IDs plus derived key for traceability and record derivation failures.
- **Migration risk:** A bulk key rewrite could break unique constraints and history, so none is permitted in REV869A.
- **Rollback requirement:** Preserve current derivation format and rows; centralization can be rolled back only if format and behavior remain identical.
- **Offline acceptance tests:** Stable formatting with/without Rack/Bin; mismatch rejected; no `Location` entity/DbSet/table; REV868 duplicate guards remain.
- **PostgreSQL acceptance tests required later:** Existing keys/counts unchanged; new writes match FKs; active-reservation and PendingRFQ filtered uniqueness still pass.

### Rule 6 — Base UOM and controlled conversions

- **Existing source state:** `Uom` exists with unique Code. Item has mutable UOM text plus nullable `UomId`; item API currently persists the text and does not resolve the FK. No conversion model/API exists. PR lines preserve a UOM snapshot.
- **Exact correction required:** Every active item must resolve one Base UOM. Add controlled directed conversion pairs with numerator/denominator or exact factor, decimal precision, effective dates, approval/active status, and no zero/negative factor. Transactions store entered UOM/quantity, base UOM/quantity, and conversion snapshot when conversion is used.
- **Database/model impact:** Add `UomConversion`; make canonical Item UOM relation mandatory only after reviewed backfill. Preserve legacy `Item.Uom` and PR `UomSnapshot` as evidence; do not rewrite PR lines.
- **API impact:** Add UOM and conversion maintenance/lookups. Item create/update resolves BaseUomId and rejects unknown/inactive UOM. No transaction conversion API is included yet.
- **Permission/security impact:** Separate view/manage/approve permissions; only approved conversions are usable; commercial users cannot silently change quantity semantics.
- **Audit/history impact:** Version conversion factors, precision, effective dates, approval, actor, remarks, and denial; never edit an effective factor in place.
- **Migration risk:** Legacy UOM strings may have aliases or ambiguous meanings. Schema is additive; mandatory constraint waits until isolated mapping validation proves all active items.
- **Rollback requirement:** Before use, drop additive conversion schema. After a conversion is referenced, retain it and roll forward. Never remove referenced UOM/conversion history.
- **Offline acceptance tests:** Unique codes/pairs/effective ranges; factor/precision validation; inactive/unapproved conversion rejection; item Base UOM resolution; immutable snapshots; PR snapshots unchanged.
- **PostgreSQL acceptance tests required later:** Index/FK/check precision; duplicate/overlapping conversion rejection; reviewed item mapping; migration rollback on an unused isolated target; accepted PR data unchanged.

### Rule 7 — Effective-dated HSN/SAC and GST resolution

- **Existing source state:** Item contains HSN/SAC and a mutable GST percentage; Vendor contains GST number/state. No tax master, effective dates, supply-state rules, registration classification, or CGST/SGST/IGST resolution exists.
- **Exact correction required:** Add effective-dated GST settings keyed by normalized HSN/SAC, intra/inter-state supply, and vendor registration status, with component rates, cess if approved, rounding, active/approved state, and non-overlapping effective periods.
- **Database/model impact:** Add `TaxGstSetting` and component/snapshot contract. Do not remove legacy Item GST fields in REV869A; treat them as legacy display/default data until later migration policy.
- **API impact:** Add tax setting maintenance, approval, history, and read-only resolution/preview endpoint. No quotation/PO calculation endpoint is included.
- **Permission/security impact:** Separate manage/approve/view permissions; requester cannot alter effective tax during approval; state and vendor registration are resolved server-side from approved masters.
- **Audit/history impact:** Version every rate/effective-date/registration/supply-rule change and resolution preview inputs/results; no silent overwrite of an effective rule.
- **Migration risk:** HSN aliases, missing vendor state/registration, overlapping rates, and rounding differences can produce wrong commercial totals. Do not backfill transactional tax snapshots in REV869A.
- **Rollback requirement:** Drop only if no future transaction references it. Effective rules used later become immutable reference evidence and require roll-forward correction.
- **Offline acceptance tests:** Intra/inter-state component selection; registered/unregistered vendor; exact effective boundaries; overlap and missing-rule fail closed; rounding; authorization/audit.
- **PostgreSQL acceptance tests required later:** Unique/exclusion-equivalent enforcement for effective ranges; decimal precision/checks; FK/lookup behavior; missing/overlap resolution failure in isolated DB.

### Rule 8 — Separate TaxableValue and TotalPayableValue

- **Existing source state:** PR has `EstimatedTotal`; no separate taxable, GST, freight/charge, discount, or total payable fields. RFQ/quotation/PO models do not yet exist.
- **Exact correction required:** Define money/value objects and immutable snapshot fields for future commercial aggregates. `TaxableValue` excludes GST and separately stated charges; `TotalPayableValue` includes configured GST and freight/charges less discount. Never overload one field for both meanings.
- **Database/model impact:** REV869A adds calculation contracts only, not RFQ/PO transaction columns. Transaction columns arrive with their owning REV869B/C migrations. Existing PR `EstimatedTotal` is preserved and not relabeled.
- **API impact:** Tax preview returns both values and components. Existing PR response remains backward compatible.
- **Permission/security impact:** Commercial-value visibility remains separately permissioned; calculated values cannot be client-authoritative.
- **Audit/history impact:** Future transactions store inputs, tax setting IDs/versions, rounding, components, and both totals. REV869A audits preview/configuration, not a commercial transaction.
- **Migration risk:** Adding/deriving values on accepted PRs would invent history; prohibited.
- **Rollback requirement:** Contract rollback must not reinterpret persisted `EstimatedTotal`.
- **Offline acceptance tests:** Formula, discount/charge signs, precision/rounding, zero values, and distinct serialization names; existing PR contract regression.
- **PostgreSQL acceptance tests required later:** None for transaction columns in REV869A beyond tax-setting precision. Later owning migration verifies non-negative/check/formula snapshot constraints.

### Rule 9 — Approval uses TotalPayableValue by default

- **Existing source state:** PR routing uses `EstimatedTotal`. The accepted REV868 bands and fixtures reflect legacy behavior. No future PO total exists.
- **Exact correction required:** Define the default approval basis as `TotalPayableValue = TaxableValue + GST + freight/charges - discount`. Future quotation award/PO approvals use that stored snapshot. REV868 PR approvals remain on their accepted estimated-total basis unless a separately approved PR contract migration is designed.
- **Database/model impact:** No REV868 PR value rewrite. Add approval-basis code/config contract so audit can identify `LEGACY_PR_ESTIMATED_TOTAL` versus `TOTAL_PAYABLE_VALUE`.
- **API impact:** Approval resolution takes a trusted server-calculated value and basis code; clients cannot select a lower basis.
- **Permission/security impact:** No self-approval; amount/value visibility does not itself grant approval; missing tax/value snapshot fails closed for future commercial approval.
- **Audit/history impact:** Store basis code, taxable value, tax, charges, discount, total payable, band, mapping, and decision/denial.
- **Migration risk:** Changing existing PR routing would break acceptance and history; explicitly out of REV869A.
- **Rollback requirement:** Preserve legacy PR routing and recorded basis. Never recalculate historical approvals using later rates.
- **Offline acceptance tests:** Formula and band boundaries; server-authoritative basis; future missing snapshot fails; REV868 approval regression stays green.
- **PostgreSQL acceptance tests required later:** REV869A verifies basis configuration persistence only. REV869C later proves PO stored total and approval route atomically.

### Rule 10 — Weighted-average inventory valuation

- **Existing source state:** `StockMovement` records quantity in/out but no unit cost, value, valuation layer, or posting service. No inventory transaction workflow exists.
- **Exact correction required:** Record weighted average as the initial valuation policy. Define formula, precision, negative-stock prohibition, landed-cost inputs, returns/reversals, and as-of ordering before inventory implementation.
- **Database/model impact:** None in REV869A. Valuation models and ledger columns belong exclusively to REV869E.
- **API impact:** None in REV869A beyond a read-only configuration decision if configuration storage is approved later.
- **Permission/security impact:** No valuation write permission in REV869A. Future cost/value visibility remains restricted.
- **Audit/history impact:** This checkpoint is the authoritative decision. Future posting audit records prior/new average, quantity/value inputs, source and reversal.
- **Migration risk:** Retrofitting costs to legacy StockMovement is high-risk and must not be guessed.
- **Rollback requirement:** REV869E must use compensating entries, never rewrite posted valuation history. No REV869A rollback action.
- **Offline acceptance tests:** REV869A boundary test asserts no valuation/ledger transaction implementation. Formula/property tests are planned for REV869E.
- **PostgreSQL acceptance tests required later:** None in REV869A; REV869E isolated tests cover average-cost precision, zero balance, returns, reversal, ordering, and legacy preservation.

### Rule 11 — Configurable QC tolerance by item/category

- **Existing source state:** Item has `QcRequired`; no tolerance, QC plan, effective dating, or category fallback exists. There are no GRN/QC transaction models.
- **Exact correction required:** Record precedence as item-specific approved tolerance over category-approved tolerance, otherwise fail closed where QC requires a tolerance. Define quantity/quality tolerance types and effective dating before REV869D.
- **Database/model impact:** No QC tolerance table in REV869A because the allowed boundary excludes QC transaction/configuration implementation beyond permissions. `QcToleranceSetting` is reserved for REV869D design unless management explicitly expands REV869A.
- **API impact:** No QC tolerance API in REV869A.
- **Permission/security impact:** REV869A may define QC page/action permissions only; it grants no QC transaction capability.
- **Audit/history impact:** Future tolerance changes require immutable versions, actor, approval, effective dates, remarks, and applied snapshot.
- **Migration risk:** Implementing a tolerance schema before measurement type/units/sampling rules are approved could encode the wrong contract.
- **Rollback requirement:** None in REV869A; REV869D settings become forward-corrected once referenced.
- **Offline acceptance tests:** Boundary test asserts no GRN/QC/tolerance transaction implementation; permission keys do not expose executable QC operations.
- **PostgreSQL acceptance tests required later:** None in REV869A; REV869D verifies precedence, effective dates, precision, missing-rule failure, and immutable applied snapshot.

### Rule 12 — No duplicate Customer or Project master

- **Existing source state:** Customer master already exists. PR stores free-text `CustomerReference`, `ProjectReference`, `CostCentre`, service, and work-order references. No Project aggregate exists in this repository.
- **Exact correction required:** Reuse Customer. Do not create `Customer2`, PurchaseCustomer, or RFQ-local customer. Do not invent a Project master in REV869A; retain references and await the approved project system of record/integration decision.
- **Database/model impact:** No Customer or Project table/DbSet/migration in REV869A.
- **API impact:** No duplicate master endpoints. Existing Customer APIs remain unchanged.
- **Permission/security impact:** Existing Customer permissions remain; future project scope cannot be inferred solely from free text.
- **Audit/history impact:** Preserve PR snapshots/references; future linkage must record source-system identity without rewriting legacy text.
- **Migration risk:** Creating a placeholder Project table would fragment identity; prohibited.
- **Rollback requirement:** Not applicable because no schema change is allowed.
- **Offline acceptance tests:** Architecture/source test asserts no new Customer/Project aggregate, DbSet, table, endpoint, or seed page in REV869A.
- **PostgreSQL acceptance tests required later:** Verify Customer table/count/keys unchanged and no REV869A Project/duplicate Customer table exists.

### Rule 13 — Vendor must be Active and Approved for RFQ/PO

- **Existing source state:** Vendor has `IsActive`, `VendorStatus`, and `ApprovalStatus`, plus lifecycle APIs and histories. No sourcing transaction currently enforces eligibility.
- **Exact correction required:** Add a reusable vendor qualification policy: `IsActive == true`, `VendorStatus == Active`, `ApprovalStatus == Approved`, plus effective category/item qualification if configured. Blacklisted/on-hold/inactive/unapproved vendors fail closed.
- **Database/model impact:** Add optional effective-dated `VendorQualification` foundation only for category/item eligibility; reuse Vendor identity/status. No RFQ/PO FK or transaction table.
- **API impact:** Add qualification maintenance and a read-only eligibility evaluation endpoint. RFQ/PO enforcement is integrated later by their owning phases.
- **Permission/security impact:** Vendor master and qualification approval are segregated; buyer cannot bypass eligibility through direct API or client-supplied status.
- **Audit/history impact:** Record qualification versions, status inputs, result/reason, actor, effective dates, and denied evaluation attempts.
- **Migration risk:** Existing category strings are denormalized and may not safely backfill qualifications. Start with explicit additive records; do not auto-approve.
- **Rollback requirement:** Drop only unused qualification schema; once referenced, retain and roll forward. Vendor master/history remains untouched.
- **Offline acceptance tests:** All active/approved combinations; category/item/effective date; blacklisted/hold rejection; permission/audit; no duplicate Vendor entity.
- **PostgreSQL acceptance tests required later:** FK/unique/effective-date checks; inactive/unapproved evaluation; Vendor rows/history unchanged.

### Rule 14 — No hard-coded employee names or codes in workflow logic

- **Existing source state:** Runtime and test/source fixtures contain employee codes, including fallback workflow decisions. Workbook/seed migration evidence legitimately contains employee data and is not itself runtime workflow logic.
- **Exact correction required:** Workflow source resolves employees only through identity, role, department mapping, and effective configuration. Add a targeted invariant test over runtime workflow files. Do not erase historical seed/migration evidence.
- **Database/model impact:** No names/codes in new workflow configuration defaults. Historical employee/migration rows remain.
- **API impact:** Responses use resolved employee identifiers/display data; requests never choose an approver by hard-coded default.
- **Permission/security impact:** Prevent impersonation and person-specific bypass; configured mappings require privileged audited management.
- **Audit/history impact:** Record resolved employee identity from configuration, not a source constant.
- **Migration risk:** Over-broad text replacement could corrupt legitimate historical evidence. Scope correction to active runtime logic and current tests.
- **Rollback requirement:** Never restore runtime hard-coded identities; roll forward.
- **Offline acceptance tests:** Scan runtime workflow source for employee-code/name literals; prove configured resolution and fail-closed behavior; allowlisted historical migration/workbook evidence remains readable.
- **PostgreSQL acceptance tests required later:** Verify active workflow configuration resolves via FKs/roles and no migration seeds person-specific fallback steps for REV869A.

### Rule 15 — Preserve REV868/REV868C3 evidence and behavior

- **Existing source state:** REV868C3 is accepted with 11 migrations, PR/stock/reservation/handoff guards, status/approval/audit histories, and regression tests. Existing migration files and snapshots encode the baseline.
- **Exact correction required:** Make REV869A additive. Do not alter prior migration files, accepted SQL/evidence reports, PR business records, stock checks, reservations, handoffs, histories, or identifiers. Update the current EF model snapshot only through the single new migration when implementation is authorized.
- **Database/model impact:** New tables/columns/indexes/FKs only within the listed A boundary. No drop/rename/retype of accepted columns; no PR/RFQ/PO/GRN/inventory transaction schema.
- **API impact:** Existing REV868 endpoints/contracts/status transitions remain backward compatible. New fail-closed identity/scope checks require configured test identity context but must not change authorized business outcomes.
- **Permission/security impact:** Security may become stricter; no accepted anonymous/shared bypass is preserved. Authorized unique employees retain role/scope-correct behavior.
- **Audit/history impact:** Baseline histories remain byte/logically unchanged; new controls append separate audit evidence.
- **Migration risk:** Backfill or constraint activation can block legacy data. Separate schema addition from reviewed activation; fail on ambiguity and never coerce.
- **Rollback requirement:** Down removes only unused REV869A objects in reverse dependency order and leaves all 11 accepted migrations/data intact. After REV869A evidence is referenced, roll forward.
- **Offline acceptance tests:** All REV868/REV868C3 offline tests; migration inventory; prior migration hash/source invariant; no forbidden aggregate/files; existing LocationKey and duplicate guards; API contract regression.
- **PostgreSQL acceptance tests required later:** Exactly 12 migrations after A, each once; baseline counts/hashes and canonical acceptance labels preserved; new schema checks; authorized regression flow; Down to REV868C3 on an isolated disposable target preserves baseline exactly.

## 4. Exact REV869A implementation boundary

The future implementation is limited to identity, approval resolution, master/configuration foundations, record scope, permissions, audit, and regression protection. Any file not listed below requires a new management checkpoint before modification.

### 4.1 Existing files proposed for modification

- `src/SESS.NexaERP.Domain/Identity/UserAccount.cs` — prevent legacy human-account bypass; compatibility only.
- `src/SESS.NexaERP.Domain/Inventory/Item.cs` — canonical Base UOM relation while retaining legacy UOM evidence.
- `src/SESS.NexaERP.Domain/Inventory/Warehouse.cs` — expose canonical condition-location relationships without deleting legacy fields.
- `src/SESS.NexaERP.Domain/Inventory/RackBin.cs` — canonical condition/location validation metadata only.
- `src/SESS.NexaERP.Domain/Purchase/PurchaseRequisition.cs` — remove active person-specific workflow resolver capability only if required; no PR transaction/value/history rewrite.
- `src/SESS.NexaERP.Application/Common/ICurrentUser.cs` — resolved employee/department/identity context contract.
- `src/SESS.NexaERP.Application/Identity/IdentityContracts.cs` — identity mapping administration contracts.
- `src/SESS.NexaERP.Application/Inventory/InventoryContracts.cs` — Base UOM and warehouse/RackBin configuration contracts.
- `src/SESS.NexaERP.Application/Masters/MasterContracts.cs` — UOM, conversion, tax/GST, and vendor qualification contracts.
- `src/SESS.NexaERP.Application/Authorization/AuthorizationContracts.cs` — record-scope/permission contracts.
- `src/SESS.NexaERP.Api/Security/ClaimsCurrentUser.cs` — immutable issuer/subject claim extraction; no email/name fallback for employee identity.
- `src/SESS.NexaERP.Api/Security/PagePermissionEndpointFilter.cs` — compose action and record-scope enforcement.
- `src/SESS.NexaERP.Api/Endpoints/IdentityEndpoints.cs` — privileged mapping administration and shared-login rejection.
- `src/SESS.NexaERP.Api/Endpoints/MasterEndpoints.cs` — UOM/conversion/tax/vendor qualification endpoints.
- `src/SESS.NexaERP.Api/Endpoints/InventoryEndpoints.cs` — Base UOM and warehouse/RackBin canonical validation.
- `src/SESS.NexaERP.Api/Endpoints/PurchaseRequisitionEndpointHelpers.cs` — remove fixed employee-code fallback and use configured resolution.
- `src/SESS.NexaERP.Api/Endpoints/PurchaseRequisitionEndpoints.cs` — identity/scope/approval fail-closed integration only.
- `src/SESS.NexaERP.Infrastructure/Authorization/EfPagePermissionService.cs` — permission plus resolved scope support.
- `src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.cs` — new DbSets, mappings, keys, precision, FKs, concurrency, and constraints.
- `src/SESS.NexaERP.Infrastructure/Persistence/FoundationSeedData.cs` — page definitions only for REV869A configuration/security pages; no transaction pages beyond existing placeholders.
- `src/SESS.NexaERP.Infrastructure/DependencyInjection.cs` — register only the new A services.
- `tests/SESS.NexaERP.Tests/Rev868PurchaseRequisitionTests.cs` — regression assertions only where behavior remains identical.
- `tests/SESS.NexaERP.Tests/Rev868C3ImplementationTests.cs` — replace active runtime-fallback expectations while preserving historical migration/workbook evidence expectations.

### 4.2 New source files proposed

- `src/SESS.NexaERP.Domain/Identity/EmployeeIdentityMapping.cs`
- `src/SESS.NexaERP.Domain/Authorization/EmployeeOperationalScope.cs`
- `src/SESS.NexaERP.Domain/Masters/UomConversion.cs`
- `src/SESS.NexaERP.Domain/Masters/TaxGstSetting.cs`
- `src/SESS.NexaERP.Domain/Masters/VendorQualification.cs`
- `src/SESS.NexaERP.Domain/Inventory/WarehouseConditionLocation.cs`
- `src/SESS.NexaERP.Application/Identity/IEmployeeIdentityResolver.cs`
- `src/SESS.NexaERP.Application/Authorization/IRecordScopeAuthorizer.cs`
- `src/SESS.NexaERP.Application/Masters/ITaxGstResolver.cs`
- `src/SESS.NexaERP.Application/Masters/IUomConversionService.cs`
- `src/SESS.NexaERP.Application/Masters/IVendorQualificationService.cs`
- `src/SESS.NexaERP.Infrastructure/Identity/EfEmployeeIdentityResolver.cs`
- `src/SESS.NexaERP.Infrastructure/Authorization/EfRecordScopeAuthorizer.cs`
- `src/SESS.NexaERP.Infrastructure/Masters/EfTaxGstResolver.cs`
- `src/SESS.NexaERP.Infrastructure/Masters/EfUomConversionService.cs`
- `src/SESS.NexaERP.Infrastructure/Masters/EfVendorQualificationService.cs`
- `tests/SESS.NexaERP.Tests/Rev869AIdentityApprovalTests.cs`
- `tests/SESS.NexaERP.Tests/Rev869AMasterConfigurationTests.cs`
- `tests/SESS.NexaERP.Tests/Rev869ARecordScopePermissionTests.cs`
- `tests/SESS.NexaERP.Tests/Rev869ARegressionBoundaryTests.cs`
- `tests/SESS.NexaERP.Tests/Rev869APostgreSqlAcceptanceTests.cs` — compiled/skipped unless later isolated PostgreSQL authorization and connection variables are supplied.

The exact implementation may use fewer files by consolidating services, but it may not add functional scope. Any additional file must be justified as a mechanical part of one listed concern and recorded before commit.

### 4.3 Exact migration boundary

Exactly one future EF Core migration is proposed, with logical migration name:

`Rev869AIdentityMasterScopeFoundation`

The tool-generated timestamp prefix is assigned only when implementation is authorized. The migration commit may contain only:

- employee identity mapping and allowed identity-type constraints;
- employee operational department/warehouse/RackBin scopes;
- UOM conversion foundation and additive Item Base UOM enforcement preparation;
- effective-dated Tax/GST settings;
- Vendor qualification foundation;
- warehouse condition-to-RackBin mappings;
- REV869A configuration/security page definitions and role permissions;
- required audit/history/index/FK/check/concurrency support.

The generated migration set is limited to:

- `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/<timestamp>_Rev869AIdentityMasterScopeFoundation.cs`
- `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/<timestamp>_Rev869AIdentityMasterScopeFoundation.Designer.cs`
- update to `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/NexaErpDbContextModelSnapshot.cs`

The migration must not create or modify RFQ, quotation, comparison, PO, follow-up, GRN, QC transaction, inventory posting/ledger, issue, return, aging, project consumption, Customer, or Project tables. It must not alter prior migration files. It must not rewrite accepted REV868/REV868C3 rows. Any ambiguous UOM, identity, location, or qualification backfill is a preflight failure, not an automatic guess.

### 4.4 Allowed API and page keys

Only configuration/security endpoints and pages are allowed:

- `security.employee-identities`
- `security.operational-scopes`
- `masters.uoms`
- `masters.uom-conversions`
- `settings.tax-gst`
- `masters.vendor-qualifications`
- `masters.warehouse-condition-locations`
- existing Purchase/Stores pages updated only for stricter identity, action, department, warehouse, and Rack/Bin scope.

QC may receive permission definitions for future view/verify/approve separation, but no executable QC transaction route/page is implemented in REV869A.

## 5. Explicit REV869A inclusions

REV869A includes only:

- unique employee/OIDC identity contract and shared-human-login prohibition;
- fixed employee-code/name approval fallback removal and fail-closed effective mapping;
- Base UOM and controlled conversion foundation;
- Warehouse/RackBin canonicalization with derived LocationKey;
- effective-dated Tax/GST settings and commercial-value calculation contracts;
- active-and-approved Vendor qualification controls;
- department/warehouse/RackBin record scope;
- Purchase/Stores/QC role/action permissions;
- immutable configuration, decision, denial, and scope audit/history;
- REV868C3 regression and migration-boundary protection.

Weighted-average valuation and QC tolerance are approved architectural decisions but their transactional/configuration persistence is deferred to their owning later phases, as stated under Rules 10 and 11.

## 6. Explicit exclusions

REV869A excludes:

- RFQ and handoff-consumption workflow;
- vendor quotation workflow;
- technical/commercial comparison workflow;
- Purchase Order workflow or transaction schema;
- material follow-up;
- GRN, physical verification, or QC transactions/tolerance persistence;
- inventory posting, valuation, stock-ledger, reservation-fulfillment, issue, return, aging, or project-consumption transactions;
- Customer or Project master creation/duplication;
- frontend implementation;
- production OIDC activation or real-token testing;
- PostgreSQL execution, data migration/backfill execution, helpers, backup/restore, main database, `sess_nexaerp`, REV861, or production access during this checkpoint.

## 7. Unresolved blockers before implementation

1. Exact OIDC issuer normalization, immutable subject claim, multi-issuer policy, service-principal policy, and legacy `UserAccount` deprecation/compatibility window.
2. Reviewed employee-to-OIDC mapping source and handling of employees without identities; no inference from email or name.
3. Operational-scope precedence when an employee has multiple department/warehouse/RackBin assignments and how temporary delegation is effective-dated.
4. Approved UOM alias cleanup, decimal precision by UOM/item, conversion graph policy, and whether chained conversions are prohibited (direct Base-UOM conversion is recommended).
5. GST component/cess, rounding, place-of-supply/state source, registration classifications, freight/charge taxability, reverse charge, exemption, and discount allocation rules.
6. Vendor category/item qualification vocabulary, expiry evidence, who approves qualification, and whether missing qualification is denied or master-status-only during transition.
7. Warehouse condition vocabulary and reviewed mapping of existing default location GUIDs.
8. Permission owners and exact role grants for Purchase, Stores, QC, identity administration, tax, UOM, vendor qualification, attachment, export, and commercial-value access.
9. Project system of record remains unresolved but is not allowed to block the limited A foundation because no Project schema is created.
10. Real OIDC provider/token testing remains a production-readiness blocker and is explicitly outside REV869A activation.

## 8. Future acceptance gate

REV869A implementation may be accepted only when:

- offline tests pass, including all REV868/REV868C3 regression and source-boundary tests;
- generated migration review proves exactly one additive A migration and no forbidden aggregate/table;
- later authorized isolated PostgreSQL tests prove migration up/down behavior, constraints, fail-closed resolution, audit evidence, baseline row/history preservation, and exactly 12 migrations once each;
- the work remains disabled for production OIDC until real provider/token acceptance is separately authorized and passed.

## 9. Checkpoint declaration

This checkpoint creates only this management-decision and scope report. It performs no source implementation, migration creation, API/frontend/seed implementation, PostgreSQL access, helper execution, backup/restore, main-database or `sess_nexaerp` access, REV861 access, production access, or OIDC activation.
