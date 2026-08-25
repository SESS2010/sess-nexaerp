# SESS NexaERP Backend Architecture Reference

This document describes the backend as it exists at commit `9046300`, immediately before this reference was added. It is a working contract for future modules, not an idealised target architecture. Where the repository is inconsistent or incomplete, that is called out explicitly.

## 1. Solution structure

The primary backend is a modular monolith. `SESS.NexaERP.slnx` contains the runtime, database tooling, and the main test project.

| Project | Purpose | Direct project dependencies |
|---|---|---|
| `src/SESS.NexaERP.Domain` | Business entities, value/status constants, transition rules, and domain calculations. | None |
| `src/SESS.NexaERP.Application` | Use-case contracts: DTOs, service interfaces, current-user abstractions, and application service contracts. | Domain |
| `src/SESS.NexaERP.Infrastructure` | EF Core persistence, PostgreSQL migrations and SQL, identity/permission resolution, auditing, and application-service implementations. | Application, Domain |
| `src/SESS.NexaERP.Api` | ASP.NET Core Minimal API host, composition root, authentication/authorization, middleware, filters, and endpoint adapters. | Application, Domain, Infrastructure |
| `src/SESS.NexaERP.Installer` | Standalone installer command for database-principal provisioning and installation checks. It intentionally does not start the API. | No project dependency; uses Npgsql |
| `src/SESS.NexaERP.SecurityMigrations` | Separately controlled EF security-migration assembly, including its own migration-history boundary. | Infrastructure |
| `tests/SESS.NexaERP.Tests` | Main unit, contract, API, EF-model, migration, and optional PostgreSQL tests. | All main projects |

There is also a deliberately separate control-plane strand which is not in `SESS.NexaERP.slnx`:

| Project | Purpose | Direct project dependencies |
|---|---|---|
| `src/SESS.NexaERP.ControlPlane.Contracts` | Frozen control-plane request/result contracts. | None |
| `src/SESS.NexaERP.ControlPlane` | Designed-but-not-deployed tenant/deployment orchestration logic. | ControlPlane.Contracts |
| `src/SESS.NexaERP.AcceptanceVerifier` | Acceptance/evidence verification against the frozen contracts. | ControlPlane.Contracts |
| `tests/SESS.NexaERP.ControlPlane.Tests` | Tests for the three control-plane projects. | The three projects above |
| `tests/SESS.NexaERP.A5Slice1.Probe` | Console probe for culture-independent canonicalisation evidence. | Application |

`src/SESS.NexaERP.ControlPlane.Persistence` is currently only an empty directory; it is not a project or an implemented persistence layer.

The dependency direction is intentional: business meaning does not depend on hosting or storage; application contracts do not know EF or HTTP; Infrastructure implements those contracts; Api composes and adapts them. Installer and security migration concerns are separate because the runtime principal must not own or evolve its own security boundary. The control plane is isolated because its deployment model is still `DESIGNED_NOT_DEPLOYED`, not part of the customer ERP process.

## 2. Layer rules

### Domain

Put business state and rules here when they can be expressed without ASP.NET Core, EF Core, Npgsql, configuration, or an external service.

Real examples:

- `src/SESS.NexaERP.Domain/Purchase/Rev869BPurchaseTransactions.cs` defines `RequestForQuotation`, `RequestForQuotationLine`, their snapshots, versions, and relationships.
- `Rev869BStatuses` and `Rev869BStatusContracts` in `src/SESS.NexaERP.Domain/Purchase/Rev869BPurchaseTransactions.cs` define lifecycle vocabulary and allowed transitions.
- Domain commercial calculation types hold deterministic business calculations rather than HTTP or persistence mechanics.

Forbidden: `DbContext`, SQL, migrations, `HttpContext`, claims, configuration lookup, logging-based policy decisions, DTOs shaped solely for transport, or calls to AWS/OIDC. A domain entity may expose business state; it must not save itself.

### Application

Put use-case-facing interfaces and transport-neutral contracts here. This layer defines what the application can do and what Infrastructure or Api must supply.

Real examples:

- `src/SESS.NexaERP.Application/Common/ICurrentUser.cs` is the current-user boundary, including effective role codes rather than raw OIDC groups.
- Purchase request/response contracts and service interfaces under `src/SESS.NexaERP.Application/Purchase` define the callable use cases without referencing EF.
- `IDateTimeProvider` is an injectable time boundary, although existing implementation code does not use it consistently.

Forbidden: EF entities/configuration, raw SQL, `NpgsqlConnection`, Minimal API route registration, middleware, database migrations, or authorization derived from provider role/group claims. Do not introduce an interface merely to hide a local helper; interfaces here represent an actual boundary or use case.

### Infrastructure

Put implementations that interact with PostgreSQL, EF Core, identity mappings, audit storage, clocks, or other external mechanisms here.

Real examples:

- `src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.cs` and `NexaErpDbContext.Rev869B.cs` map entities and constraints.
- `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs` implements Purchase use cases with EF transactions, idempotency, numbering, history, and auditing.
- `src/SESS.NexaERP.Infrastructure/Identity/EfEmployeeIdentityResolver.cs` resolves an OIDC issuer/subject/organisation to an employee and effective database roles.
- `src/SESS.NexaERP.Infrastructure/Audit/EfAuditWriter.cs` persists audit events.

Forbidden: HTTP response construction, route/filter registration, trusting OIDC groups as ERP authority, UI concerns, or business rules that should be enforceable and testable without a database. SQL is appropriate for database-wide invariants and security boundaries, not as a default substitute for understandable orchestration.

### Api

Put host composition and HTTP adaptation here.

Real examples:

- `src/SESS.NexaERP.Api/Program.cs` registers services, authentication, middleware, endpoint groups, and the runtime-principal startup guard.
- `src/SESS.NexaERP.Api/Endpoints/Rev869BPurchaseEndpoints.cs` maps Purchase routes and converts service outcomes/exceptions to HTTP results.
- Identity middleware and page-permission endpoint filters resolve the caller and enforce database permissions before handlers execute.

Forbidden: direct `DbContext` business queries in route handlers, duplicating domain transition rules, embedding customer/provider-specific authorization, migration execution on startup, or returning EF entities as the public contract. Keep handlers thin: bind, authorize, invoke an application service, and translate the result.

## 3. How one Purchase request flows end to end

The concrete example is `POST /api/v1/purchase/rfqs`, which creates a request for quotation.

1. **Composition and route registration.** `src/SESS.NexaERP.Api/Program.cs` registers Infrastructure and Application services and calls `MapRev869BPurchaseEndpoints`. `src/SESS.NexaERP.Api/Endpoints/Rev869BPurchaseEndpoints.cs` creates the Purchase route group, requires authentication and employee operational scope, and maps the RFQ POST route. The route calls `RequirePagePermission` with page key `purchase.rfq` and action `Create`.
2. **Startup protection.** Before serving requests, `Program.cs` invokes the runtime-principal guard. Outside the explicit Development-only exemption, startup rejects an owner/superuser or otherwise unsafe connection principal. This protects the process boundary; it is not request authorization.
3. **Global middleware.** The request passes through exception handling, JWT bearer authentication, employee identity resolution, and ASP.NET authorization in the order registered by `Program.cs`. Unexpected exceptions become a problem response rather than leaking a stack trace.
4. **External authentication.** JWT bearer authentication validates the standards-compliant OIDC token using configured authority/audience rules. Provider group and role claims are not ERP authorization inputs.
5. **Identity resolution.** `src/SESS.NexaERP.Api/Middleware/EmployeeIdentityResolutionMiddleware.cs` takes the authenticated `iss` and `sub` plus optional `organization_id`/`org_id` claim and calls `EfEmployeeIdentityResolver` in `src/SESS.NexaERP.Infrastructure/Identity/EfEmployeeIdentityResolver.cs`. The resolver looks up exactly one active, effective `employee_identity_mappings` row, confirms the employee is active and login-enabled, and loads all active approved role assignments for the mapped company and date. `src/SESS.NexaERP.Api/Security/ClaimsCurrentUser.cs` exposes that resolved employee, organisation, role set, and request identity to downstream code. Operational scope is checked by the endpoint filter, not by the resolver.
6. **Permission and scope filters.** `src/SESS.NexaERP.Api/Security/EmployeeScopeEndpointFilter.cs` requires the resolved organisation and calls the record-scope authorizer. `src/SESS.NexaERP.Api/Security/PagePermissionEndpointFilter.cs` checks page `purchase.rfq` plus action `Create` through `src/SESS.NexaERP.Infrastructure/Authorization/EfPagePermissionService.cs`. Permission is the union across effective database roles; casing is canonical uppercase for role codes, while page keys are lower-case module/resource identifiers. At this revision the Purchase group and the page-permission helper both attach scope checking, so this route performs the broad existence check twice. That duplication is harmless but should not be copied.
7. **Handler.** The RFQ handler in `Rev869BPurchaseEndpoints` binds the PascalCase request DTO and invokes `EfRev869BPurchaseService.CreateRfqAsync` through the local `Run` response adapter. The handler contains no EF query.
8. **Service and EF work.** `CreateRfqAsync` in `src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.RfqQuotation.cs` validates input and workflow authority, loads the handoff/purchase-request records and lines, and checks the caller against record scope. Helpers and shared transaction machinery are in `EfRev869BPurchaseService.cs`. The method begins a serializable transaction and takes a PostgreSQL transaction advisory lock for the idempotency command scope. An existing matching fingerprint is replayed; a reused key with a different fingerprint is rejected.
9. **Numbering and snapshots.** The service calls `NextNumberAsync` for the organisation/fiscal-year RFQ sequence, then creates the RFQ and line records. It copies relevant master data into snapshot fields while retaining live foreign keys. This preserves what the buyer saw even if an item or party master later changes.
10. **Lifecycle and authorized save.** The service appends the initial status row through `AddStatus`. `SaveAuthorizedChangesAsync` opens the REV869B database command context and calls EF `SaveChangesAsync`. PostgreSQL functions/triggers enforce the controlled mutation, transition, append-only, and tenant invariants independently of C#.
11. **Audit and idempotency receipt.** `EfAuditWriter` appends the audit record, and the service stores the command receipt/fingerprint before committing the same transaction. The audit identifies the actor and action; the current implementation does not yet have a first-class acting-role model for multi-role users.
12. **Response.** The service returns its response DTO and `Run` returns HTTP 200. The adapter maps validation failures to 400, missing records to 404, concurrency/idempotency conflicts to 409, and authentication/authorization failures to 401/403. This differs from some generic master creates, which return 201 and use inline anonymous error payloads.

The command-context functions used by REV869B are part of the separately gated security package, not merely the ordinary owner migration chain. A database with only the baseline schema migrations is therefore not equivalent to a fully provisioned runtime database.

## 4. Patterns already established

### Entity definition and configuration

Canonical example: `RequestForQuotation` and `RequestForQuotationLine` in `src/SESS.NexaERP.Domain/Purchase/Rev869BPurchaseTransactions.cs`, configured in `src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.Rev869B.cs`.

Define persistence-capable business state in Domain, then configure table, columns, keys, relationships, indexes, precision, maximum lengths, constraints, and concurrency tokens with Fluent API in Infrastructure. Do not place data annotations throughout the domain model. New transaction aggregates should follow the partial-`DbContext` organisation rather than expanding the central file indefinitely.

The physical convention is lower snake-case table names such as `request_for_quotations`, with quoted PascalCase columns such as `OrganizationId` and `Version`. Constraints and indexes use explicit `CK_...`, `FK_...`, `IX_...`, `PK_...`, or `UX_...` names. This mixed table/column casing is established and must be respected in handwritten SQL.

### Status and approval lifecycle

Canonical examples: `Rev869BStatuses` in `src/SESS.NexaERP.Domain/Purchase/Rev869BPurchaseTransactions.cs`, the transition checks in `EfRev869BPurchaseService`, and `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/Rev869BDatabaseLifecycleSql.cs`.

Use an explicit finite status vocabulary, validate transitions in C# for useful errors, append a lifecycle record, and enforce the legal transition again in PostgreSQL when all writers must obey it. Approval is a separate event/history concept, not a mutable set of columns masquerading as history. Workflow authority still uses a scalar role in parts of Purchase; do not silently turn that into any-role authority until an acting-role design is approved.

### History tables and append-only writes

Canonical examples: `AddStatus`, `AddApproval`, and `AddPoHistory` in `EfRev869BPurchaseService`, backed by the Purchase status/approval/history entities and append-only database triggers.

Use append-only rows for state transitions, approvals, and material document changes. The current aggregate row may hold the current status for efficient reads, but the event/history row is added rather than edited. Database triggers reject update/delete where immutability is a cross-writer guarantee. A general audit event is not a substitute for domain history: both may be required.

### Optimistic concurrency with `Version`

Canonical example: the REV869B compare-and-swap updates in `EfRev869BPurchaseService`, which include the record id, organisation, and expected version in the predicate, set `Version = Version + 1`, and call the local `RequireCas` helper. Database triggers also require an exact increment.

Use this for mutable business records where two actors can race. The request carries the expected `Version`; zero affected rows becomes a 409 rather than a last-write-wins overwrite. Merely calling `.IsConcurrencyToken()` does not increment an integer version. Some generic master services compare a version but do not visibly increment it; treat that as a weak point, not a pattern to copy.

### Number sequences

Canonical example: `PurchaseNumberSequence` plus `NextNumberAsync` in `EfRev869BPurchaseService`, producing numbers such as `RFQ-26-27-000001` within organisation and fiscal-year scope.

Use a database-backed sequence row when a business document needs a formatted, scoped, gap-tolerant number. Allocate inside the business transaction and protect concurrent allocation at the same scope. The current implementation serialises identical idempotency command scopes, but the initial/read-increment path for different idempotency keys deserves a concurrency review before it is copied to Stores.

### Snapshot versus master reference

Canonical example: RFQ and purchase-order lines in `Rev869BPurchaseTransactions.cs` retain item/party foreign keys and copy descriptive/commercial values into snapshot columns during creation.

Keep a live master foreign key when navigation to the current master is useful. Snapshot any value whose historical meaning must not change after the master changes: item code/name, supplier identity, UOM, price description, address, or tax basis as appropriate. Do not snapshot volatile master fields indiscriminately, and do not render an old commercial document solely from the current master row.

### Audit writing

Canonical example: `src/SESS.NexaERP.Infrastructure/Audit/EfAuditWriter.cs`, called by command services such as `EfRev869BPurchaseService`.

Write audit events for security-significant reads and state-changing commands with actor, organisation, action, target, outcome, and correlation/request information. Keep them in the same transaction when they are evidence of that transaction. Domain lifecycle history remains separate.

Current caveats: `EfAuditWriter` saves through the shared `DbContext`, so a call can flush other tracked changes; its correlation identifier is not consistently the request correlation; and only the literal `Denied` action is treated as failure. New work should make transaction boundaries explicit and should not assume this implementation is a complete security-view audit facility.

### Validation and error responses

Canonical Purchase example: guard methods and typed exceptions in `EfRev869BPurchaseService`, translated by `Run` in `Rev869BPurchaseEndpoints`. Global unexpected errors are handled by the API exception middleware as problem JSON.

Validate syntax/shape at the boundary and business/record rules in the service/domain. Return 400 for invalid input, 401/403 for identity/authority, 404 for absence within allowed scope, and 409 for state, concurrency, or idempotency conflicts. Never reveal that an out-of-scope record exists.

The response shape is inconsistent today: REV869B uses the `Run` adapter, while several master endpoints construct anonymous `{ message }` responses inline. Until a repository-wide error contract is adopted, new modules should choose one documented module-local contract and use it consistently; the Purchase `Run` mapping is the stronger existing example.

### Idempotency

Canonical example: Purchase command handling in `EfRev869BPurchaseService`, the command fingerprint/receipt entities, unique indexes, and PostgreSQL transaction advisory locks.

Use idempotency for externally retryable creates and workflow commands. Scope a client key to the command/tenant, canonicalise and hash the material request, replay the stored response for the same fingerprint, and reject the same key with different content. Persist the receipt in the same transaction as the business effect. A unique constraint remains necessary even when application locking is used.

### Database triggers and functions versus C#

Canonical examples: `Rev869BDatabaseSafetySql.cs`, `Rev869BDatabaseLifecycleSql.cs`, `Rev869BControlledMutationSql.cs`, and `Rev869BCommandContextSql.cs` under `src/SESS.NexaERP.Infrastructure/Persistence/Migrations`.

Use C# for orchestration, input-specific errors, calculations, calls across aggregates, and rules owned by one use case. Use a trigger/check/function for invariants every database writer must obey: append-only history, tenant consistency, legal lifecycle transitions, controlled mutation context, and privilege boundaries. Critical rules often exist in both places: C# explains the failure, PostgreSQL remains the final authority.

Security-definer functions must have a fixed safe `search_path`, narrow execute grants, explicit input checks, and tests using the actual runtime principal. Do not put ordinary business workflow into opaque trigger chains merely to avoid writing a service.

## 5. Naming and conventions

- Projects, namespaces, types, methods, and public DTO properties use PascalCase. Interfaces use the `I` prefix; async methods use the `Async` suffix.
- EF service implementations normally use the `Ef...Service` prefix, for example `EfRev869BPurchaseService`. Endpoint modules use `...Endpoints` and expose a `Map...Endpoints` extension.
- Request/response types generally end in `Request`, `Response`, `Dto`, or `Result`; the repository is not fully uniform about which suffix is used.
- API routes are versioned under `/api/v1`, use lower-case plural resource paths, and are grouped by module.
- Database table/function/trigger names use lower snake case. EF-generated columns are quoted PascalCase. Handwritten SQL must preserve that exact distinction.
- Role codes are canonical uppercase, for example `TECHNICAL_DIRECTOR`; page keys are lower-case resource identifiers such as `purchase.rfq`, paired with a separate action such as `Create`.
- Status values are string tokens. Newer REV869B values are constant-backed and machine-oriented; older areas contain space/casing variations. Reuse the owning module constants rather than typing literals.
- Organisation scoping is called `OrganizationId` in newer workflow code, while older/schema areas also use `CompanyId`. Both string and `Guid` identifiers occur. Do not translate between them by guesswork.
- Migration class/id names describe the business/security change. Tests should assert required migration IDs by name and order, never assert the total migration count.
- Large features commonly use partial `NexaErpDbContext` files and module-specific endpoint/service files. Some existing source is very dense and has multiple declarations or statements per line; readability is not a convention that new code should copy.

## 6. Testing approach

The main tests live in `tests/SESS.NexaERP.Tests`; the isolated control-plane tests live in `tests/SESS.NexaERP.ControlPlane.Tests`.

| Test kind | What it proves |
|---|---|
| Pure domain/application unit tests | Calculations, status/transition rules, canonicalisation, and contract behaviour without infrastructure. |
| Minimal API and endpoint-filter tests | Route metadata, authentication/permission filters, binding, and HTTP result mapping. |
| EF model tests | Entity discovery, keys, relationships, constraints, indexes, and concurrency configuration. |
| Migration contract/order tests | Required named migrations are present in the intended order and contain expected structural safeguards. |
| Disposable PostgreSQL migration tests | The real generated migration chain applies to a temporary cluster and `Down` reverses it. These catch syntax and engine behaviour that SQLite/in-memory tests cannot. |
| Opt-in PostgreSQL security/behaviour tests | Runtime-role grants, triggers, functions, tenant isolation, append-only enforcement, concurrency, and direct-SQL rejection using independent connections. These require explicit cluster provisioning. |
| Source/evidence contract tests | Frozen checkpoint inputs, hashes, and deliberately approved source constraints where runtime observation is not practical. |
| Control-plane freeze tests | Preserve the designed-but-not-deployed control-plane contract independently of ERP runtime work. |

There are two distinct protections commonly called a PostgreSQL cluster guard:

1. `src/SESS.NexaERP.Infrastructure/Persistence/Migrations/PostgreSqlClusterGuard.cs` is called by migration `Up` and `Down`. It rejects a non-Npgsql active provider so PostgreSQL-specific SQL is never silently run elsewhere. Its current exception text still mentions the employee-master rebuild, which is stale wording.
2. `RequireSafeClusterAsync` in `src/SESS.NexaERP.Installer/Program.cs` validates PostgreSQL 17 or newer, the explicitly expected non-system database, the `advance` schema, and a superuser installer session. It prevents privileged provisioning against an accidental cluster/database.

`src/SESS.NexaERP.Infrastructure/Persistence/DatabaseRuntimePrincipalGuard.cs` is a third, API-startup concern. It requires both `session_user` and `current_user` to be `nexa_erp_runtime`; rejects superuser, create-database, create-role, replication, and bypass-RLS attributes; and rejects database ownership, `advance` schema ownership, or membership in `nexa_erp_owner`. `DatabaseSecurity:AllowDevelopmentSuperuser=true` permits only a superuser and only in Development, with critical warnings on every startup. In a non-`DEBUG` build, mere presence of that setting, even `false`, fails startup through the compile-time `#if !DEBUG` guard.

Good patterns to copy are real PostgreSQL apply/down tests, concurrent tests with independent connections, tests that prove forbidden direct writes are rejected, named migration-ID checks, and exact SQL/hash evidence checks separated from non-semantic SDK/runtime provenance.

Do not copy tests that assert incidental implementation details: a hard-coded total migration count, a fixed parent-directory depth from `AppContext.BaseDirectory`, raw method/table occurrence counts, exact JSON equality that fails on a runtime patch version, or source-text regex assertions when behaviour can be exercised. Counts are valid only when the count itself is an approved business invariant, such as a frozen 42-row roster or an acceptance-scenario set. The current optional REV869B PostgreSQL set reports failures in an ordinary unprovisioned run; those are explicit environment opt-ins, not a model for silently classifying new regressions.

## 7. What is deliberately not there

- **No local password store.** Authentication is external standards-compliant OIDC. `PasswordHash = PENDING_IDENTITY_PROVIDER` is not a password mechanism. Cognito is the first reference deployment and Entra ID must also work; provider groups never grant ERP permissions.
- **No shared-SaaS tenant control plane.** Each customer owns a deployment in its AWS account. The control-plane code is contract/evidence work marked `DESIGNED_NOT_DEPLOYED`, not a hidden production dependency.
- **No automatic schema migration on API startup.** Privileged installation/migration and unprivileged runtime are separate ceremonies and principals. The runtime application must not own the schema.
- **No generic repository or unit-of-work wrapper.** EF Core `DbContext` is already the persistence unit of work. Services query aggregates directly through it.
- **No MediatR/CQRS framework, AutoMapper, or FluentValidation.** Requests call explicit services; mappings and validation are handwritten. Introduce a library only for a demonstrated cross-module need, not because another architecture normally has it.
- **No MVC controllers.** The host uses Minimal APIs with route groups and endpoint filters.
- **No generic CRUD engine.** Master modules share concepts but currently expose explicit services/endpoints. Transaction modules require lifecycle, scope, and idempotency behaviour that generic CRUD would conceal.
- **No global EF tenant query filter.** Organisation/record scope is applied explicitly. This avoids invisible query rewriting but makes missed filters a serious review risk.
- **No OIDC-role-to-ERP-role mapping.** Identity proves who the caller is; database role assignments and page permissions determine what that person may do.
- **No acting-role model yet.** Page access unions all effective roles, but workflow approval authority still expects a scalar role. This was consciously left unchanged pending a design for selecting and recording the role under which an action occurs.
- **No Item company filter.** The Item master is shared by management decision. Stock is company-scoped. Minimum/maximum/reorder thresholds belong later in Stores at company-and-warehouse scope, not on Item API filtering.
- **No implemented control-plane persistence project.** The directory exists, but treating it as a deployed subsystem would be inaccurate.

## 8. Rules a new module must follow

For a Stores module, complete this checklist before calling the slice finished.

### Boundaries and model

- [ ] Put business entities, status vocabulary, transitions, and deterministic calculations in Domain with no EF/HTTP dependencies.
- [ ] Put request/response DTOs and service/current-user contracts in Application.
- [ ] Put EF mappings, PostgreSQL work, service implementations, and audit persistence in Infrastructure.
- [ ] Keep Api endpoints thin and use versioned module route groups.
- [ ] Choose the aggregate and transaction boundary explicitly; do not expose EF entities as API DTOs.
- [ ] Decide for every master reference whether a live FK, a snapshot, or both is required.

### Security and scope

- [ ] Require OIDC authentication, resolved employee identity, active login/mapping, and the correct operational/record scope.
- [ ] Define page keys and enforce them through the database-authoritative permission service. UI visibility is not authorization.
- [ ] Use the union of effective roles for page access; do not read provider group/role claims.
- [ ] Keep workflow/approval role checks scalar until the acting-role design is implemented; list every such check in the change review.
- [ ] Apply organisation/company/warehouse scope in every query and command, including history and audit reads. Return 404 rather than disclose out-of-scope existence.
- [ ] Run as the runtime database principal and add privilege tests. Do not develop a feature that only succeeds as `postgres`/owner.

### Transactions and integrity

- [ ] Use explicit statuses and legal transitions; append status/approval/history rows rather than editing history.
- [ ] Use expected `Version` compare-and-swap with an actual increment for mutable concurrent records.
- [ ] Make retryable creates and commands idempotent with scoped key, canonical fingerprint, receipt, locking, and unique constraint.
- [ ] Allocate business numbers transactionally and test concurrent first-use and increment paths with independent connections.
- [ ] Put cross-writer invariants in named PostgreSQL constraints/triggers/functions and retain clear C# validation.
- [ ] For security-definer functions, fix `search_path`, minimise grants, validate context, and test direct misuse.
- [ ] Write business effect, domain history, idempotency receipt, and required audit evidence in an intentional transaction boundary.

### API and operations

- [ ] Use PascalCase contract properties where the established API/database contract requires it; use consistent request/result suffixes within the module.
- [ ] Document success codes and one consistent error shape. Map validation, missing, authority, concurrency, and conflict cases explicitly.
- [ ] Add list paging/sorting/filtering limits rather than returning unbounded tables.
- [ ] Do not run migrations or privileged provisioning from the API process.
- [ ] Add installer/upgrade-safe migrations with both `Up` and `Down`, and invoke `PostgreSqlClusterGuard` in each direction.
- [ ] Keep ordinary schema migration and separately privileged security migration boundaries explicit.

### Tests and evidence

- [ ] Add pure business-rule tests, service/endpoint authorization tests, and EF model tests.
- [ ] Apply the full migration chain to disposable PostgreSQL and reverse it.
- [ ] Add real PostgreSQL tests for triggers, runtime grants, tenant isolation, append-only behaviour, idempotency, and concurrency.
- [ ] Test required migration IDs by name/order, not total count or file-system accident.
- [ ] Prove denial paths as well as successful owner-style paths; test with the runtime principal.
- [ ] Document any opt-in environment prerequisite so a skipped environment cannot be confused with a passing security test.

## 9. Known weak points

These are traps for the next engineer, not endorsed patterns.

1. **Workflow authority is still single-role.** Effective page permissions are a union, but Purchase approval/action code still records or compares one `RoleCode`. Using `RoleCodes.Any(...)` there would silently expand approval authority and lose which role was exercised.
2. **Scope enforcement is manual and sometimes duplicated.** There is no global query filter. Purchase route composition can run the broad employee-scope check twice, while a new query can accidentally omit record scope. Centralise helpers only if their semantics remain visible.
3. **The operational-scope existence check is not itself row filtering.** Services must apply the permitted organisation/company/department/warehouse dimensions to their queries; passing the filter does not prove a query is scoped.
4. **Some generic-master concurrency handling is incomplete.** A numeric property marked as an EF concurrency token does not increment itself. Copy the REV869B compare-and-swap pattern, not a version equality check alone.
5. **Purchase sequence allocation needs stronger cross-command concurrency evidence.** Advisory locking is command/idempotency-scoped; different keys contending for the same new sequence scope may not be fully serialised by that lock.
6. **Audit implementation has transaction and semantics ambiguity.** `EfAuditWriter` calls save on a shared context, correlation is not consistently request-derived, success/failure classification is narrow, and acting role is absent. Security-sensitive PII views will need explicit read auditing rather than assuming ordinary change audit is sufficient.
7. **Error contracts and create status codes vary.** REV869B endpoint `Run`, global problem responses, and generic master anonymous messages are not one schema. Decide per new API and avoid adding a fourth style.
8. **Identifier and status vocabularies are inconsistent.** `OrganizationId` versus `CompanyId`, string versus `Guid`, and legacy human-formatted status values versus REV869B tokens require module-specific understanding.
9. **Database security provisioning is separate and easy to overlook.** Baseline/owner migrations do not by themselves install every controlled-mutation command-context function or runtime grant. Deployment evidence must cover the security package too.
10. **Development can mask privilege defects.** The explicit `DatabaseSecurity:AllowDevelopmentSuperuser` exemption is Development-only and noisy, but code exercised only under it can still fail for a customer runtime principal. PostgreSQL privilege tests are mandatory.
11. **The system metadata endpoint needs continuing scrutiny.** `/api/v1/system/database-model` has historically exposed schema metadata publicly. It must remain absent outside Development and must not be used as a production introspection API.
12. **Time abstraction is inconsistent.** `IDateTimeProvider` exists, but much implementation code directly calls `DateTimeOffset.UtcNow`/`DateTime.UtcNow`. New testable workflows should use the abstraction, while avoiding a broad unrelated rewrite.
13. **Role catalogue cleanup remains open.** Unassigned/duplicate legacy roles such as `admin` versus managing director, `dcc` versus document controller, and software developer versus engineer still exist. Do not authorise by guessed aliases.
14. **Test infrastructure can obscure signal.** The optional REV869B cluster tests have historically appeared as ordinary failures when prerequisites are absent. New regressions must be compared against a proven baseline and named, never dismissed by category.
15. **Some tests still inspect source shape.** Source/evidence checks are useful for frozen artefacts, but fragile path, count, regex, and runtime-patch assertions should be replaced with behaviour or semantic evidence when touched.
16. **`PostgreSqlClusterGuard` has stale feature-specific wording.** Its provider check is reusable; its employee-rebuild error text is not.
17. **Code density is uneven.** Several large files compress declarations and logic, making review harder. Extend by coherent module files/partials and do not preserve density merely for textual consistency.
18. **Control-plane names can imply more maturity than exists.** It is designed/frozen evidence, not deployed customer infrastructure, and `ControlPlane.Persistence` is empty.

## Quick decision rule

When adding a feature, ask in this order: Is it a pure business rule? Put it in Domain. Is it a use-case contract or boundary? Put it in Application. Does it talk to PostgreSQL or an external mechanism? Implement it in Infrastructure. Is it HTTP composition or translation? Put it in Api. Then prove the important invariant at the lowest layer that every possible writer must pass, and prove the friendly behaviour at the highest layer the caller uses.
