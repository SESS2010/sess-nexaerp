# SESS NexaERP — Backend Architecture Guide

Date: 12 September 2026
Purpose: where everything lives, and where to look when something is wrong

---

## The eleven projects

| Project | Files | What it is |
|---|---|---|
| **Domain** | 67 | The business itself — entities and rules, no database, no HTTP |
| **Application** | 48 | Contracts and service interfaces — what an operation needs and returns |
| **Infrastructure** | 326 | Everything that touches PostgreSQL |
| **Api** | 58 | HTTP endpoints, authentication, middleware |
| **Installer** | 16 | Provisioning, ceremonies, import retention |
| Web | 0 | React frontend (TypeScript, not counted here) |
| AcceptanceVerifier | 3 | Acceptance checking |
| ControlPlane | 6 | **REV869B leftover — see below** |
| ControlPlane.Contracts | 2 | **REV869B leftover** |
| ControlPlane.Persistence | 0 | **Empty — REV869B leftover** |
| SecurityMigrations | 3 | **REV869B leftover** |

**529 C# files.**

### The four REV869B leftovers

`ControlPlane`, `ControlPlane.Contracts`, `ControlPlane.Persistence` and
`SecurityMigrations` belong to the retired nine-role topology. One is empty.

They are almost certainly dead weight. Worth asking Codex to confirm nothing
references them, then removing them — every file in a repository is something a
future reader has to understand and dismiss.

---

## The dependency rule

```
Api  →  Application  →  Domain
 ↓           ↓
Infrastructure ──────────┘
```

**Domain depends on nothing.** It holds the rules that would still be true if
SESS threw away PostgreSQL tomorrow.

**Infrastructure depends on everything** and nothing depends on it. It is where
the database lives.

That direction is why a business rule can be read without reading SQL, and why
the rules survive a database change.

---

## Where a business area lives

Each area appears in three or four places. To understand "material issue", read
all of them in this order:

| Area | Domain | Application | Infrastructure | Endpoints |
|---|---|---|---|---|
| **Stores** | 14 | 13 | 37 | the largest |
| **Employees** | 12 | 1 | — | |
| **Masters** | 10 | 5 | 4 | |
| **Inventory** | 8 | 1 | — | |
| **Identity** | 5 | 2 | 2 | |
| **Authorization** | 4 | 3 | 2 | |
| **Purchase** | 2 | 8 | 7 | |
| Sales | 1 | — | — | customer PO only |
| Audit | 1 | 2 | 2 | |

**Stores is the heart of this ERP.** 64 files across three layers. It carries
GRN, QC, MIR, issue, return, custody, BOM, fitment, Actual BOM, FAT, vendor
bill, landed cost, advance, payment and opening stock.

Purchase looks small in Domain — two files — because most purchase logic is
workflow and permission rather than entity shape.

---

## Infrastructure/Persistence — 253 files

This is half the backend. Almost all of it is migrations.

```
Persistence/
  Migrations/            ~80 migrations, each with a .cs and a .Designer.cs
  NexaErpDbContext.*.cs  the model, split by area
  *Sql.cs                raw SQL for controlled operations
```

**Why Designer.cs files are 3.6 MB each.** EF writes the entire model snapshot
into every migration. With 215 tables that is large and unavoidable. Ignore
them when reading; they are generated.

**The `*Sql.cs` files are the important ones.** They hold the SECURITY DEFINER
functions and the triggers that enforce rules the application cannot be trusted
to enforce:

| File | What it protects |
|---|---|
| `VendorAdvancePaymentSql.cs` | advances and payments — runtime has EXECUTE only |
| `RetireRev869BOrdinaryDeploymentSql.cs` | the retirement, and the ACL fix |
| `DevelopmentWorkflowIdentitiesCommandSql.cs` | development identity seeding |

When a rule must hold even if the API is wrong, it lives in one of these.

---

## The migration chain — read it as a history

Migration names are the project's diary. Recent ones, newest first:

| Date | Migration | What changed |
|---|---|---|
| 12 Sep | `GovernedOpeningStockThreeActorCeremony` | **opening stock — item 13** |
| 12 Sep | `OpeningStockImportAdapterPermissions` | its permissions |
| 11 Sep | `ConvergeOrdinaryOwnerExplicitAcl` | the ACL defect |
| 11 Sep | `AlignGrnQcDueAtWithReceiptTime` | two answers became one |
| 11 Sep | `JobOrderAccountsReturnAndResubmission` | third no-exit state |
| 11 Sep | `EngineeringDocumentReturnToDraftAuthority` | fourth no-exit state |
| 11 Sep | `ProductionBomReturnToDraftAuthority` | second no-exit state |
| 10 Sep | `EstimatedBomReturnToDraftAuthority` | first no-exit state |
| 10 Sep | `ImmutableLandedCostAdjustments` | freight into item cost |
| 10 Sep | `InAppNotificationDelivery` | notifications reached somebody |
| 10 Sep | `LookupReachabilityCorrections` | dropdowns a role could not load |
| 9 Sep | `ItemCompanyLastPurchasePricing` | last accepted purchase price |
| 9 Sep | `FrozenEstimatedBomUnitValue` | commercial variance became possible |
| 8 Sep | `JobOrderFatReadiness` | FAT |
| 8 Sep | `ComponentFitmentAndGeneratedActualBom` | consumption, and the Actual BOM |
| 8 Sep | `GovernedJobOrderCreationWorkflow` | one machine, one job order |
| 8 Sep | `VendorBillAndAcceptedBillCosting` | accepted-bill cost |
| 7 Sep | `MaterialReturnToStores` | material could come back |

**Never edit an applied migration.** A correction is a new migration. That rule
has held through eighty of them and it is why the chain still applies cleanly
from an empty database.

---

## Api/Endpoints — 40 files

One file per business area, each mapping a route group.

| Pattern | Meaning |
|---|---|
| `*Endpoints.cs` | route group for one area |
| `Rev869A*`, `Rev869B*` | named from an earlier revision; the names stayed, the security model did not |

**The `Rev869A`/`Rev869B` prefixes are historical.** They do not mean the
retired security package is still in use. Worth renaming eventually — the names
now mislead.

### Middleware — 3 files

Where a business conflict becomes a truthful 409 rather than escaping to a
generic 500. Codex fixed that on 8 September; before it, a conflict looked like
a crash.

### Security — 4 files

Token validation, the development token service, and the identity resolver that
turns a token subject into an employee with company scope and resolved roles.

---

## Where to look when something is wrong

| Symptom | Look here |
|---|---|
| A rule is not enforced | Domain first, then the `*Sql.cs` trigger |
| A role cannot do something it should | `Api/Endpoints/*` permission, then role-page grants in a migration |
| A role CAN do something it should not | the same two, plus `Authorization` in Application |
| A list is empty for the right person | company scope in the Infrastructure service |
| A write returns 500 | the ordinary command ledger, then the audit guard |
| A number disagrees with another number | a stored derived value versus a live projection — the `QcDueAt` class of defect |
| Nobody can sign in | principals and ACLs, `database-principals status` |

---

## The three things that are not in the code

**Frozen decisions** live in
`docs/SESS_ERP_Stores_Full_Schema_Guideline.docx` and the eight specification
documents in `docs/`. The code implements them; it does not explain them.

**Why a rule exists** lives in those documents and nowhere else. "Tools return
100 per cent" is a line of validation in the code and a paragraph of reasoning
in the specification.

**What is not built yet** lives in
`docs/SESS_NexaERP_Pending_Work_Specification.md` and its companions — 50
items, of which 14 are acceptance-complete.

---

## Reading order for someone new

1. `docs/SESS_NexaERP_Handoff.md` — what this is and why
2. `Domain/Stores` — the business objects
3. `Application/Stores` — what an operation needs
4. One `*Sql.cs` — how a rule is enforced where it cannot be bypassed
5. The migration list — the history of every decision
6. `Api/Endpoints` — how it is reached

**Do not start with Infrastructure.** 326 files, most of them generated, and
they will teach you the database before the business.

---

## One observation worth acting on

**Infrastructure is 62 per cent of the backend by file count**, and
`Persistence` alone is 253 files.

Most of that is unavoidable — eighty migrations, each with a generated snapshot.
But it means a new developer opening the repository sees mostly generated code,
and the 129 files that hold the actual business logic are outnumbered three to
one.

The four REV869B leftover projects make that worse for no benefit. Removing
them is an hour of work and makes the repository honest about its own size.
