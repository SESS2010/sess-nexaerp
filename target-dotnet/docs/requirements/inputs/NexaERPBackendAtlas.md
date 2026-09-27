> Converted from NexaERPBackendAtlas.docx (SHA-256 A16BF2999F7D7C60467CC408F8F8F88CDE26E510DACD9E9F91DDC3AB31213F36) to Markdown on 27 September 2026 for traceability. Wording unchanged; layout simplified (headings, lists, tables).

SESS NEXAERP · TARGET-DOTNET · REFERENCE

**NexaERP Backend Atlas**

The complete map of the .NET backend: where the server starts, where the database connects, how a request travels through the middleware to an endpoint, and exactly which files to touch when you change a single field. Every path is relative to target-dotnet/.

Generated 2026-08-29 against commit 04bcb5e on main. Paths are stable; line numbers drift as code evolves — search by symbol name.

## 1. The four layers and their folders

target-dotnet/ is the complete, current backend (the old Node code in current-system-snapshot/ is a frozen reference only). The solution is SESS.NexaERP.slnx, and the four projects under src/ form strict layers:

| **Project** | **Role** | **What lives there** |
|---|---|---|
| SESS.NexaERP.Api | HTTP layer | Entry point, routes, middleware, security filters. The only project that knows about HTTP. Start reading here. |
| SESS.NexaERP.Application | Contracts layer | DTOs (request/response shapes) and service interfaces — one *Contracts.cs file per module. |
| SESS.NexaERP.Domain | Business layer | Entities and business constants. A class here (e.g. Vendor) is a database table. No HTTP, no SQL. |
| SESS.NexaERP.Infrastructure | Database layer | PostgreSQL connection, EF Core DbContext, migrations, and every Ef* service implementation. The only project that talks to the database. |

Rule of thumb: Api depends on Application, Application on Domain; Infrastructure implements Application’s interfaces against Domain’s entities. Nothing points the other way.

## 2. Startup: what runs when the server boots

The single entry point — the equivalent of the old server.js — is src/SESS.NexaERP.Api/Program.cs. It executes top to bottom, in three phases:

### Phase A — register services (nothing runs yet)

- ConfigureHttpJsonOptions(...) → global PascalCase JSON contract, defined in src/SESS.NexaERP.Api/Serialization/ApiJsonContract.cs
- AddInfrastructure(configuration) → THE DATABASE CONNECTION IS CONFIGURED HERE: src/SESS.NexaERP.Infrastructure/DependencyInjection.cs reads ConnectionStrings:NexaErp (supplied by the ConnectionStrings__NexaErp environment variable — never a file in the repo) and calls UseNpgsql(...) to register NexaErpDbContext. It also registers every Ef* service.
- AddAuthentication().AddJwtBearer(...) → token validation. Two branches: the Debug-only development scheme signed by src/SESS.NexaERP.Api/Security/DevelopmentTokenService.cs, or the permanent OIDC authority from Authentication__Authority / Authentication__Audience.

### Phase B — the startup guard

Before serving a single request, src/SESS.NexaERP.Infrastructure/Persistence/DatabaseRuntimePrincipalGuard.cs opens a real PostgreSQL connection and refuses to start unless the session principal is nexa_erp_runtime (or the Debug-only superuser exemption is on). This is the first place a wrong connection string fails.

### Phase C — build the pipeline and mount the routes

The app.UseMiddleware<...>() calls fix the middleware order (section 3), then the app.Map*Endpoints() calls at the bottom of Program.cs mount every route group (section 4).

## 3. The request pipeline — every call passes through these files, in order

| **#** | **Stage** | **File and what it does** |
|---|---|---|
| 1 | **StandardErrorEnvelopeMiddleware** | src/SESS.NexaERP.Api/Middleware/StandardErrorEnvelopeMiddleware.cs — wraps every 4xx/5xx response into the standard envelope {Type, Title, Status, Code, Detail, TraceId, Errors}. |
| 2 | **ExceptionHandlingMiddleware** | src/SESS.NexaERP.Api/Middleware/ExceptionHandlingMiddleware.cs — catches unhandled exceptions so a crash becomes a clean 500 instead of a stack trace. |
| 3 | **UseAuthentication (JWT)** | Framework middleware configured in Program.cs — reads "Authorization: Bearer <JWT>", verifies the signature, builds context.User claims. Proves WHO you are, nothing more. |
| 4 | **EmployeeIdentityResolutionMiddleware** | src/SESS.NexaERP.Api/Middleware/EmployeeIdentityResolutionMiddleware.cs — takes the token’s iss / sub / organization_id claims and calls src/SESS.NexaERP.Infrastructure/Identity/EfEmployeeIdentityResolver.cs, which queries employee_identity_mappings in the database to resolve WHICH employee, company and roles this token is. The token proves identity; the database decides authority. |
| 5 | **UseAuthorization** | Framework middleware — enforces .RequireAuthorization() on every endpoint group: no valid token → 401 before any handler runs. |
| 6 | **PagePermissionEndpointFilter (per route)** | src/SESS.NexaERP.Api/Security/PagePermissionEndpointFilter.cs, attached by .RequirePagePermission("masters.vendors", View) — asks src/SESS.NexaERP.Infrastructure/Authorization/EfPagePermissionService.cs whether the resolved roles hold that action on that page (the role–page matrix lives in the DB). No → 403. |
| 7 | **The endpoint handler** | The lambda inside an src/SESS.NexaERP.Api/Endpoints/*.cs file — parameters auto-bound from URL and JSON body, business logic runs, EF Core translates the query to SQL, result serialized back as PascalCase JSON. |

## 4. The route table — which file owns which URL

Every endpoints file registers one MapGroup(prefix). To find any URL’s code: match the prefix below, open that file, search for the MapGet( / MapPost( line. All files live in src/SESS.NexaERP.Api/Endpoints/.

| **URL prefix** | **File** | **What it serves** |
|---|---|---|
| /api/v1/session/me | SessionEndpoints.cs | Who am I (resolved employee, roles, company) |
| /api/v1/identity/* | IdentityEndpoints.cs | Roles, user accounts |
| /api/v1/authorization/* | AuthorizationEndpoints.cs | Page definitions, role–page permission matrix |
| /api/v1/masters/customers\|vendors | MasterEndpoints.cs | Customer & Vendor masters + workflow actions |
| /api/v1/masters/item-categories, item-subcategories, uoms, manufacturers | ReferenceMasterEndpoints.cs | Item reference lookups (new) |
| /api/v1/items, /warehouses, /rack-bins | InventoryEndpoints.cs | Item, Warehouse, Rack/Bin masters |
| /api/v1/employees/* | EmployeeEndpoints.cs | Employee master, approval workflow, roles, history, lookups |
| /api/v1/purchase/* (requisitions) | PurchaseRequisitionEndpoints.cs | PR creation, stock check, approvals |
| /api/v1/purchase/* (RFQ→PO) | Rev869BPurchaseEndpoints.cs | RFQ, quotations, comparison, PO, follow-up |
| /api/v1/configuration/* | Rev869AConfigurationEndpoints.cs | UOM conversions, tax/GST, vendor qualifications, QC policies |
| /api/v1/audit/* | AuditEndpoints.cs | Audit history |
| /api/v1/dev/* | DevelopmentAuthEndpoints.cs | Debug-only dev sign-in (absent from Release builds) |

## 5. The database layer — where SQL actually happens

- **Connection:** configured once in src/SESS.NexaERP.Infrastructure/DependencyInjection.cs from the ConnectionStrings__NexaErp environment variable. There is deliberately no appsettings.json with credentials in the repo.
- **The DbContext (the ORM’s map of every table):** src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.cs — one DbSet<Entity> property per table, split into partial files per module (NexaErpDbContext.StoresPart1.cs, .Rev869A.cs, …). All tables live in the PostgreSQL schema "advance".
- **Migrations (versioned schema changes):** src/SESS.NexaERP.Infrastructure/Persistence/Migrations/ — applied with dotnet ef database update.
- **Service implementations (complex module logic):** src/SESS.NexaERP.Infrastructure/ subfolders — e.g. Purchase/EfRev869BPurchaseService.cs, Authorization/EfPagePermissionService.cs. Simple CRUD endpoints use the DbContext directly; workflow-heavy modules go through these services.
- **Seed data:** src/SESS.NexaERP.Infrastructure/Persistence/*SeedData.cs; reversible trial rows come from database/postgresql/trial-master-data-apply.sql via tools/trial-master-data.ps1.

## 6. One call end-to-end: GET /api/v1/masters/vendors

- **Route match.** MapGroup("/api/v1/masters") + MapGet("/vendors", …) in src/SESS.NexaERP.Api/Endpoints/MasterEndpoints.cs (line ~113).
- **Pipeline steps 1–5.** Error envelope armed → JWT verified → identity resolved from DB → authorization enforced.
- **Page permission.** .RequirePagePermission("masters.vendors", View) → role matrix checked in the DB. Fail → 403.
- **Parameter binding.** page, pageSize, search, status, type, sortBy bound automatically from the query string into the handler’s parameters.
- **Query building.** db.Vendors.AsNoTracking() + organization scope + search/status filters — LINQ only, no SQL executed yet.
- **SQL executes.** CountAsync() (total for paging), then sort + Skip/Take + projection to VendorSummary — two SELECTs against advance.vendors.
- **Field-level security.** BankMetadata is nulled unless the role holds commercial-view permission.
- **Response.** Wrapped in PagedResponse<T> → {TotalCount, PageNumber, PageSize, Items}, serialized PascalCase.

**POST** (same file, line ~148) adds: body → UpsertVendorRequest DTO → ValidateVendorAsync (required fields 400, GSTIN/PAN format 400, duplicates 409) → new Vendor entity in Draft + MasterStatusHistory row → one transaction → audit write → 201 Created.

**PUT** (line ~162) adds three protections: approved vendor code is immutable (400); the body must echo the record’s Version or you get 409 stale-version (optimistic concurrency); changing controlled fields (GST, bank) records re-verification evidence.

## 7. Change one field, end to end

Say you want to add Website to the Vendor master. Six touches, in this order:

- **Domain entity — the table shape.** src/SESS.NexaERP.Domain/Masters/Vendor.cs → add: public string? Website { get; set; }
- **Migration — the schema change.** From target-dotnet/: dotnet ef migrations add AddVendorWebsite --project src\SESS.NexaERP.Infrastructure --startup-project src\SESS.NexaERP.Api → generates a file in Persistence/Migrations/. Then: dotnet ef database update
- **Contracts — the wire shape.** src/SESS.NexaERP.Application/Masters/MasterContracts.cs → add Website to VendorDetail and UpsertVendorRequest.
- **Endpoint — read & write the field.** src/SESS.NexaERP.Api/Endpoints/MasterEndpoints.cs → map it in ApplyVendor(...) (request → entity) and ToDetail(...) (entity → response). Add validation in ValidateVendorAsync if needed.
- **Frontend type.** frontend/src/types/vendor.ts → add Website: string | null to both interfaces.
- **Frontend UI.** frontend/src/features/vendors/VendorFormModal.tsx (input) and frontend/src/features/vendors/VendorDetailPage.tsx (display).

| **Pattern** The same six touches apply to any module — swap Vendor.cs for the module’s entity, MasterContracts.cs for its contracts file, and the endpoints file per the route table in section 4. |
|---|

## 8. Configuration — every environment variable the API reads

| **Variable** | **Purpose** | **Read in** |
|---|---|---|
| ConnectionStrings__NexaErp | PostgreSQL connection (the only secret) | Infrastructure/DependencyInjection.cs |
| ASPNETCORE_ENVIRONMENT | "Development" unlocks dev-only behavior | framework |
| Authentication__Authority / __Audience | Production OIDC issuer + API audience | Api/Program.cs |
| DatabaseSecurity__AllowDevelopmentSuperuser | Debug-only: allow postgres superuser locally | DatabaseRuntimePrincipalGuard.cs |
| NexaErp__AllowDevelopmentAuthentication | Debug-only: enable /api/v1/dev/* sign-in | Api/Program.cs |
| NexaErp__ExpectedDatabase | Safety check for installer/trial tooling | Installer, tools/trial-master-data.ps1 |

| **Release safety** Every Development/Debug exemption above hard-fails a Release build if the setting is even present — the dev conveniences physically cannot ship to production. |
|---|

## 9. Where do I look for…?

| **Question** | **Answer** |
|---|---|
| What URLs exist? | Search MapGet( / MapPost( in src/SESS.NexaERP.Api/Endpoints/ |
| What JSON goes over the wire? | src/SESS.NexaERP.Application/<module>/*Contracts.cs |
| What columns does a table have? | src/SESS.NexaERP.Domain/<module>/*.cs (+ its migration) |
| Where does SQL run? | DbContext usage in endpoints, or src/SESS.NexaERP.Infrastructure/<module>/Ef*.cs |
| Why did I get 401? | Token invalid (pipeline 3) or identity unresolved (pipeline 4) |
| Why did I get 403? | Role lacks the page action — Api/Security/PagePermissionEndpointFilter.cs |
| Why did I get 409? | Duplicate identity, or stale Version (optimistic concurrency) |
| Who changed this record? | advance.audit_logs via Infrastructure/Audit/EfAuditWriter.cs |
