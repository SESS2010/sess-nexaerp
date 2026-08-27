# SESS NexaERP API contract

Status: frontend integration contract  
HTTP API version: `v1`  
Authoritative for frontend field names and planned Stores routes: this document  
Stores implementation status: database schema installed; Stores HTTP endpoints in section 13 are **NOT YET IMPLEMENTED**

## 1. Contract rules

### 1.1 Base URL and versioning

The deployment supplies the origin, for example `https://erp.sess.example`. All business routes are relative to that origin and start with `/api/v1`. A breaking request or response change requires `/api/v2`; additive nullable fields may be added to `v1`. Clients must not construct a different schema prefix or call the database directly.

Every JSON request uses `Content-Type: application/json; charset=utf-8`. Every authenticated request uses:

```http
Authorization: Bearer <OIDC access token>
Accept: application/json
```

### 1.2 Company context

Company context comes only from the authenticated identity. The API resolves the token's `iss`, `sub`, and `organization_id` claim (with `org_id` accepted as the legacy alias) to one employee and one `CompanyId`. There is no `X-Company-Id` header and a client must never send `CompanyId` to choose or override a company. Where an existing administrative request contains `OrganizationId`, it must equal the authenticated organization.

Company selection persists for the login session. To change company, the client obtains a token for the other organization, discards all company-scoped cached data and drafts, calls `GET /api/v1/session/me` again, and starts a new working context. Cross-company views are not supported.

### 1.3 Field naming: PascalCase is mandatory

JSON property names are **PascalCase and match the C# and database column names exactly**. There is no frontend/backend field-mapping layer. Use `CompanyId`, `RequiredByDate`, and `Version`; do not use `companyId`, `required_by_date`, or aliases. Query-string parameter names shown in paths remain camelCase because they are parameter names, not JSON fields.

Implementation alignment required before frontend integration: the current general ASP.NET JSON defaults serialize ordinary DTOs as camelCase, and some legacy failures return `{ "message": "..." }` or an empty body. Those are implementation variances, not this contract. The backend must configure PascalCase and the standard error envelope below before a strict client is connected. Mock data and new Stores APIs must follow this document now.

### 1.4 Dates, numbers, nulls and identifiers

- `DateOnly`: ISO `YYYY-MM-DD`, for example `"2026-08-27"`.
- Timestamp: ISO-8601 UTC with `Z`, for example `"2026-08-27T10:15:30.125Z"`. A client may send an explicit offset; the server normalizes it to UTC.
- GUID: canonical hyphenated string, lower-case in examples.
- Decimal quantities and money: JSON numbers, never localized strings; a period is the decimal separator. Quantity/rate fields allow up to six fractional digits unless a narrower business rule applies.
- Currency: ISO-4217 upper-case code such as `"INR"`.
- `Version`: unsigned JSON integer. It is the PostgreSQL `xmin` concurrency token exposed by the API.
- Enum/status values: exact upper/lower case shown by the response; clients must not silently transform them.
- Nullable fields are present with `null`. Empty collections are `[]`, not `null`.
- Fields ending in `Json` are JSON-encoded strings unless their declared response example shows a nested object.

## 2. Authentication and session bootstrap

The API accepts an OIDC bearer access token. Token claims used by the server are:

| Claim | Required | Meaning |
|---|---:|---|
| `iss` | yes | Trusted identity issuer; part of the employee identity key. |
| `sub` | yes | Subject at that issuer; part of the employee identity key. |
| `organization_id` | yes | Selected company/organization. `org_id` is the temporary legacy alias. |
| `exp`, `nbf`, `aud` | yes | Standard token validity and configured API audience checks. |

Application roles and page permissions are resolved from ERP data. The frontend must not grant access merely because a role-like token claim exists.

The first authenticated call is always `GET /api/v1/session/me`. On access-token expiry, the OIDC client performs silent renewal. After a `401`, it may renew once and retry a GET/HEAD or an idempotent command with the same idempotency key. It must not blindly replay a non-idempotent command. If renewal fails, clear protected state and begin interactive login. A `403` is an authorization decision and must not trigger token renewal.

### 2.1 `GET /api/v1/session/me`

Permission: authenticated employee; no page permission. Request body: none.

`200 OK`:

```json
{
  "EmployeeId": "145e2c65-3f72-4ef3-b7d0-9f323404298c",
  "EmployeeCode": "EMP-0042",
  "EmployeeName": "Priya E",
  "CompanyId": "11111111-1111-1111-1111-111111111111",
  "OrganizationId": "SESS-PVT",
  "DepartmentId": "22222222-2222-2222-2222-222222222222",
  "DepartmentCode": "STORES",
  "RoleCodes": ["STORES_MANAGER", "QC_MANAGER"],
  "IdentityIssuer": "https://login.example.com/realms/sess",
  "IdentitySubject": "00u1abc234xyz"
}
```

Errors: `401 AUTHENTICATION_REQUIRED` when the token is absent/invalid or no active employee identity matches; `403 PERMISSION_DENIED` when the identity is valid but its company/scope is inactive.

## 3. Standard responses

### 3.1 Error envelope

Every non-2xx JSON response uses exactly these properties. `Errors` is `{}` unless field validation details exist.

Validation, `400`:

```json
{
  "Type": "https://api.sess.example/problems/validation-error",
  "Title": "Validation failed",
  "Status": 400,
  "Code": "VALIDATION_FAILED",
  "Detail": "One or more fields are invalid.",
  "TraceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "Errors": {
    "RequiredByDate": ["RequiredByDate is required."],
    "Lines[0].RequestedQuantity": ["RequestedQuantity must be greater than zero."]
  }
}
```

Authentication, `401`:

```json
{
  "Type": "https://api.sess.example/problems/authentication-required",
  "Title": "Authentication required",
  "Status": 401,
  "Code": "AUTHENTICATION_REQUIRED",
  "Detail": "A valid OIDC bearer token and active employee identity are required.",
  "TraceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "Errors": {}
}
```

Permission, `403`:

```json
{
  "Type": "https://api.sess.example/problems/permission-denied",
  "Title": "Permission denied",
  "Status": 403,
  "Code": "PERMISSION_DENIED",
  "Detail": "The current role does not have masters.items:Approve in this company and scope.",
  "TraceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "Errors": {}
}
```

Not found, `404`:

```json
{
  "Type": "https://api.sess.example/problems/not-found",
  "Title": "Resource not found",
  "Status": 404,
  "Code": "NOT_FOUND",
  "Detail": "Item ITEM-404 was not found in the current scope.",
  "TraceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "Errors": {}
}
```

Concurrency, `409`:

```json
{
  "Type": "https://api.sess.example/problems/concurrency-conflict",
  "Title": "Concurrency conflict",
  "Status": 409,
  "Code": "CONCURRENCY_CONFLICT",
  "Detail": "The record changed after it was loaded. Refresh and retry.",
  "TraceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "Errors": {
    "Version": ["Expected 142, current version is 145."]
  }
}
```

An idempotency key reused with a different request returns the same shape with `Code: "IDEMPOTENCY_CONFLICT"`. An illegal lifecycle transition returns `409 BUSINESS_RULE_CONFLICT`. An unexpected failure returns `500 INTERNAL_ERROR`; internal exception text and stack traces are never exposed.

### 3.2 Pagination, sorting and filtering

The canonical list response is:

```json
{
  "TotalCount": 247,
  "PageNumber": 2,
  "PageSize": 25,
  "Items": [{ "Id": "6d3b08fc-5b02-45c9-8760-0795f988c5cb" }]
}
```

Canonical query parameters are `page` (default `1`, minimum `1`), `pageSize` (default `25`, maximum `100`), `search`, module-specific exact filters, `sortBy`, and `sortDirection=asc|desc`. Invalid filter/sort values return `400`; unknown filters are not ignored. Unless an endpoint below says otherwise, sorting defaults to its stable number/code ascending and the server adds `Id` as the final tie-breaker. Filters combine with AND. Search is trimmed, case-insensitive, and matches the named code/number and display-name fields.

Legacy list exceptions that the frontend must handle until normalized: master/PR list helpers clamp rather than reject oversized `pageSize`; employee and audit lists return a bare array; REV869B material follow-up returns `Page` instead of `PageNumber` and has no `TotalCount`.

### 3.3 Optimistic concurrency

GET/detail and mutation responses include `Version` where the resource supports concurrency. PUT and lifecycle bodies echo that exact integer:

```json
{ "Remarks": "Approved against quotation comparison.", "Version": 145 }
```

On `409 CONCURRENCY_CONFLICT`, discard the attempted local version, reload, show the differences, and require a conscious retry. Do not auto-merge. Employee endpoints are the current exception: they do not expose or accept `Version` and therefore do not yet offer optimistic concurrency.

### 3.4 Idempotency

REV869B purchase commands carry required `IdempotencyKey` in the JSON body. Vendor-qualification commands require the `Idempotency-Key` header. PR stock check accepts optional `IdempotencyKey`; when supplied, an exact retry returns the existing check result. New Stores command endpoints require the `Idempotency-Key` header. A successful exact retry returns the original status and representation without a second side effect; the response may include `Idempotency-Replayed: true`. Reusing a key with a different canonical body/path/company/actor produces `409 IDEMPOTENCY_CONFLICT`.

Create/update endpoints not named above are not idempotent. A client-generated ID is not a substitute for an idempotency key.

## 4. Shared implemented shapes

Tables below refer to these concrete shapes. `ActionRequest` is:

```json
{ "Remarks": "Checked and approved.", "Version": 42 }
```

`LifecycleResult` is:

```json
{ "Code": "ITEM-001", "Status": "Active", "ApprovalStatus": "Approved", "Version": 43 }
```

`StatusHistory[]`, `ApprovalHistory[]`, and `AuditHistory[]` are respectively:

```json
[
  { "Id": "e1d67c32-4e23-4a10-82af-cce61318d782", "PreviousStatus": "Draft", "NewStatus": "PendingApproval", "Reason": "Ready", "CreatedAt": "2026-08-27T08:30:00Z", "CorrelationId": "REV867_Item_Submit_a1" }
]
```

```json
[
  { "Id": "4742ff75-c3c2-4445-9187-cebb3790de26", "Action": "Approve", "FromStatus": "PendingApproval", "ToStatus": "Approved", "Remarks": "Verified", "ActorLoginId": "emp-0042", "ActorRoleCode": "TECHNICAL_DIRECTOR", "CreatedAt": "2026-08-27T08:35:00Z", "CorrelationId": "REV867_Item_Approve_a2" }
]
```

```json
[
  { "Id": "6e9ae0f5-c752-4553-a30e-641f53d61efb", "Module": "Masters", "Action": "Update", "UserLoginId": "emp-0042", "Result": "Success", "CorrelationId": "a3", "BeforeJson": "{}", "AfterJson": "{}", "CreatedAt": "2026-08-27T08:36:00Z" }
]
```

Unless a row states otherwise, all authenticated endpoints can also return `401`, `403`, and `500`; detail/mutation routes can return `404`; writes can return `400` and `409`.

## 5. Implemented platform, identity and authorization endpoints

| Method and path | Request | Success response | Required page permission | Endpoint-specific errors |
|---|---|---|---|---|
| `GET /health/live` | none | `200 text/plain` body `Healthy` | public | `503 text/plain` if unhealthy |
| `GET /health/ready` | none | `200 text/plain` body `Healthy` | public | `503 text/plain` if not ready |
| `GET /health/db` | none | `200 text/plain` body `Healthy` | public | `503 text/plain` if DB check fails |
| `GET /api/v1/system/architecture` | none | `{ "App":"SESS NexaERP", "Architecture":"ASP.NET Core modular monolith target", "Status":"Phase 1 permanent auth foundation", "SourceSystem":"REV861 Node.js/single HTML current ERP snapshot", "Database":"PostgreSQL authoritative target", "Note":"Master APIs require authenticated JWT/OIDC claims. No temporary header identity is used." }` | public | `500` |
| `GET /api/v1/system/modules` | none | `{ "Modules":["IdentityAndAccess","CustomerPortal","VendorPortal","EmployeeAdmin","Sales","Project","Design","Purchase","StoresInventory","Qc","Production","Dispatch","Finance","Service","DocumentManagement","Notification","AuditReporting"] }` | public | `500` |
| `GET /api/v1/purchase-stores/workflow-stages` | none | `{ "Stages":["MaterialRequirement","CurrentStockCheck","ProjectReservation","Rfq","VendorQuotation","VendorComparison","PurchaseApproval","PurchaseOrder","MaterialFollowUp","GateEntry","Grn","QcVerification","InventoryUpdate","MaterialIssue","MaterialReturn","StockLedger","ProjectConsumption","AccountsHandover","VendorPerformance"] }` | public | `500` |
| `GET /api/v1/system/database-model` | none | `{ "Schema":"advance","Provider":"Npgsql.EntityFrameworkCore.PostgreSQL","Entities":[{"Name":"Item","Table":"items"}] }` | public, Development environment only | `404` outside Development |
| `GET /api/v1/session/me` | none | `SessionMe` in 2.1 | authenticated | `401`, `403` |
| `GET /api/v1/identity/roles` | none | `RoleSummary[]` | `identity.roles:View` | common |
| `POST /api/v1/identity/roles` | `CreateRole` | `201 RoleSummary` | `identity.roles:Create` | duplicate code `409` |
| `GET /api/v1/identity/users` | none | `UserAccountSummary[]` | `identity.users:View` | common |
| `POST /api/v1/identity/users` | `CreateUserAccount` | `201 UserAccountSummary` | `identity.users:Create` | inactive/missing role `400`; duplicate login `409` |
| `GET /api/v1/authorization/pages` | none | `PageDefinitionSummary[]` | `authorization.pages:View` | common |
| `POST /api/v1/authorization/pages` | `CreatePageDefinition` | `201 PageDefinitionSummary` | `authorization.pages:Create` | duplicate key `409` |
| `GET /api/v1/authorization/role-page-permissions?roleCode=PURCHASE_MANAGER` | none | `RolePagePermission[]` | `authorization.role-pages:View` | unknown role `400` |
| `PUT /api/v1/authorization/role-page-permissions` | `RolePagePermission` | `200 RolePagePermission` | `authorization.role-pages:Update` | inactive role/page `400` |
| `GET /api/v1/audit/history?module=Purchase&page=1&pageSize=25` | none | `AuditLogSummary[]` | `audit.history:ViewAuditHistory` | common |

```json
{
  "CreateRole": { "Code": "QC_MANAGER", "Name": "QC Manager", "IsPrivileged": false },
  "RoleSummary": { "Id": "1bf836b4-84bd-4466-9e69-c97ca8cc3f70", "Code": "QC_MANAGER", "Name": "QC Manager", "IsPrivileged": false, "IsActive": true },
  "CreateUserAccount": { "LoginId": "priya.e", "DisplayName": "Priya E", "Email": "priya@example.com", "UserType": "Employee", "RoleCode": "QC_MANAGER", "MfaRequired": true },
  "UserAccountSummary": { "Id": "15f310a0-c22d-41d2-b40b-27f56960bf8c", "LoginId": "priya.e", "DisplayName": "Priya E", "Email": "priya@example.com", "UserType": "Employee", "RoleCode": "QC_MANAGER", "MfaRequired": true, "IsActive": true },
  "CreatePageDefinition": { "PageKey": "stores.gate-entry", "Module": "Stores", "Title": "Gate Entry", "Route": "/stores/gate-entries" },
  "PageDefinitionSummary": { "Id": "01f5731f-4f59-40aa-ad33-e8387525e9dd", "PageKey": "stores.gate-entry", "Module": "Stores", "Title": "Gate Entry", "Route": "/stores/gate-entries", "IsActive": true }
}
```

`RolePagePermission` has `Id`, `RoleCode`, `PageKey`, then these Boolean fields: `CanView`, `CanCreate`, `CanUpdate`, `CanSubmit`, `CanVerify`, `CanApprove`, `CanReject`, `CanRequestClarification`, `CanRequestRevision`, `CanResubmit`, `CanCancel`, `CanDeactivate`, `CanPrint`, `CanDownload`, `CanExport`, `CanUploadAttachment`, `CanReplaceAttachment`, `CanViewCommercialValues`, `CanViewAuditHistory`, `HasFullControl`.

## 6. Implemented employee endpoints

Employee request/response examples:

```json
{
  "CreateEmployee": {
    "EmployeeCode": "EMP-0042", "EmployeeName": "Priya E", "EmployeeType": "Permanent", "Grade": "M2",
    "DepartmentCode": "STORES", "SkillCode": "INVENTORY", "DesignationCode": "STORES_MANAGER",
    "DateOfJoining": "2024-06-01", "OfficialEmail": "priya@example.com", "MobileNumber": "9876543210", "Remarks": "Initial onboarding"
  },
  "UpdateEmployee": {
    "EmployeeName": "Priya E", "EmployeeType": "Permanent", "Grade": "M3", "DepartmentCode": "STORES",
    "SkillCode": "INVENTORY", "DesignationCode": "STORES_MANAGER", "DateOfJoining": "2024-06-01",
    "OfficialEmail": "priya@example.com", "MobileNumber": "9876543210", "Reason": "Grade revision"
  },
  "EmployeeDetail": {
    "Id": "145e2c65-3f72-4ef3-b7d0-9f323404298c", "EmployeeCode": "EMP-0042", "EmployeeName": "Priya E",
    "OriginalImportedName": null, "EmployeeType": "Permanent", "Grade": "M3", "Department": "Stores",
    "SkillCategories": ["Inventory"], "JobDesignation": "Stores Manager", "Status": "Active", "DateOfJoining": "2024-06-01",
    "OfficialEmail": "priya@example.com", "MobileNumber": "9876543210", "LoginEnabled": true,
    "ApprovalStatus": "Approved", "Roles": ["STORES_MANAGER"]
  }
}
```

| Method and path | Request | Success response | Required page permission | Endpoint-specific errors |
|---|---|---|---|---|
| `GET /api/v1/employees?page=1&pageSize=25&search=&status=` | none | `EmployeeSummary[]` (bare array) | `employees.master:View` | common |
| `GET /api/v1/employees/{employeeCode}` | none | `EmployeeDetail` | `employees.master:View` | `404` |
| `POST /api/v1/employees` | `CreateEmployee` | `201 EmployeeDetail` | `employees.master:Create` | invalid master codes `400`; duplicate `409` |
| `PUT /api/v1/employees/{employeeCode}` | `UpdateEmployee` | `200 EmployeeDetail` | `employees.master:Update` | `404` |
| `POST /api/v1/employees/{employeeCode}/submit` | `{ "Remarks":"Ready for approval" }` | `{ "EmployeeCode":"EMP-0042", "ApprovalStatus":"PendingApproval" }` | `employees.master:Submit` | illegal state `409` |
| `POST /api/v1/employees/{employeeCode}/approve` | `{ "Remarks":"Verified" }` | approval result above | `employees.master:Approve` | self-approval/role `403`; illegal state `409` |
| `POST /api/v1/employees/{employeeCode}/reject` | `{ "Remarks":"Identity proof missing" }` | approval result | `employees.master:Reject` | illegal state `409` |
| `POST /api/v1/employees/{employeeCode}/revise` | `{ "Remarks":"Correct designation" }` | approval result | `employees.master:RequestRevision` | illegal state `409` |
| `POST /api/v1/employees/{employeeCode}/activate-login` | `{ "Reason":"Employment active" }` | `{ "EmployeeCode":"EMP-0042", "LoginEnabled":true, "Status":"Active" }` | `employees.master:Update` | `404`, `409` |
| `POST /api/v1/employees/{employeeCode}/deactivate-login` | `{ "Reason":"Employee exited" }` | login result | `employees.master:Deactivate` | `404`, `409` |
| `POST /api/v1/employees/{employeeCode}/roles` | `{ "RoleCode":"STORES_MANAGER", "EffectiveFrom":"2026-08-27", "EffectiveTo":null, "Remarks":"Department owner" }` | `201 EmployeeRoleSummary` | `employees.role-mapping:Create` | overlap/unknown role `400` or `409` |
| `GET /api/v1/employees/{employeeCode}/roles` | none | `EmployeeRoleSummary[]` | `employees.role-mapping:View` | `404` |
| `GET /api/v1/employees/{employeeCode}/history` | none | `EmployeeHistorySummary[]` | `employees.audit-history:ViewAuditHistory` | `404` |

`EmployeeSummary` contains `Id`, `EmployeeCode`, `EmployeeName`, `EmployeeType`, `Grade`, `Department`, `SkillCategory`, `JobDesignation`, `Status`, `LoginEnabled`, `ApprovalStatus`. `EmployeeRoleSummary` contains `Id`, `RoleCode`, `RoleName`, `EffectiveFrom`, `EffectiveTo`, `ApprovalStatus`, `Remarks`. `EmployeeHistorySummary` contains `Id`, `Action`, `FromStatus`, `ToStatus`, `Remarks`, `CreatedAt`, `CreatedBy`.

## 7. Implemented customer, vendor and inventory-master endpoints

### 7.1 Master object shapes

```json
{
  "Customer": {
    "Id":"ea111df8-15b6-43c5-9ac8-c082abf74b96", "CustomerCode":"CUS-001", "Name":"Example Foods Pvt Ltd",
    "LegalCustomerName":"Example Foods Private Limited", "TradeName":"Example Foods", "CustomerType":"Domestic",
    "GstNumber":"29ABCDE1234F1Z5", "PanNumber":"ABCDE1234F", "BillingAddress":"Bengaluru",
    "ShippingAddress":"Mysuru", "State":"Karnataka", "StateCode":"29", "Country":"IN",
    "ContactPerson":"Anita", "Phone":"9876500000", "Email":"buy@example.com", "Industry":"Food",
    "PaymentTerms":"30 days", "CreditPeriodDays":30, "CreditLimit":500000.00, "PortalOrganizationId":"CUSTOMER-EXAMPLE-FOODS",
    "Status":"Active", "ApprovalStatus":"Approved", "IsActive":true, "Version":42
  },
  "Vendor": {
    "Id":"fd43c5d3-85bd-4a06-bcd0-484cab82b75d", "VendorCode":"VEN-001", "Name":"Cold Parts India",
    "LegalVendorName":"Cold Parts India Pvt Ltd", "TradeName":"Cold Parts", "VendorType":"Material",
    "GstNumber":"29ABCDE1234F1Z5", "PanNumber":"ABCDE1234F", "MsmeStatus":true, "MsmeNumber":"UDYAM-KR-00-0000001",
    "ContactPerson":"Ravi", "Phone":"9876500001", "Email":"sales@example.com", "BillingAddress":"Bengaluru",
    "ShippingAddress":"Bengaluru", "State":"Karnataka", "StateCode":"29", "Country":"IN",
    "MaterialServiceCategories":"Refrigeration", "ApprovedMakes":"BITZER", "PaymentTerms":"30 days", "DeliveryTerms":"FOR SESS",
    "CreditPeriodDays":30, "BankMetadata":"{}", "AttachmentMetadataJson":"[]",
    "ApprovalStatus":"Approved", "VendorStatus":"Active", "IsActive":true, "Version":17
  },
  "Item": {
    "Id":"3501e490-33ae-47f8-b9dc-da7c04aaf4bb", "ItemCode":"COMP-001", "Name":"Semi-hermetic compressor",
    "DetailedDescription":"BITZER compressor", "MaterialType":"Refrigeration", "ItemType":"Purchased", "IsReturnable":false,
    "Uom":"NOS", "ManufacturerMake":"BITZER", "Model":"4NES-14Y", "PartNumber":"4NES-14Y-40P",
    "HsnSacCode":"84143000", "GstPercentage":18.000000, "TechnicalSpecification":"400V/3Ph/50Hz",
    "DrawingDocumentReference":null, "QcRequired":true, "SerialNumberTracking":true, "BatchTracking":false,
    "ShelfLifeTracking":false, "MinimumStock":1.000000, "MaximumStock":6.000000, "ReorderLevel":2.000000,
    "PreferredVendorCode":"VEN-001", "StandardEstimatedPrice":225000.00, "Barcode":null, "BarcodeSymbology":null,
    "ImageStorageKey":null, "ImageFileName":null, "ImageContentType":null,
    "Status":"Active", "ApprovalStatus":"Approved", "IsActive":true, "Version":9
  },
  "Warehouse": {
    "Id":"d65fd856-2ae0-4cdd-aea5-2d4ca0c70b24", "WarehouseCode":"MAIN", "Name":"Main Stores",
    "WarehouseType":"Stores", "Location":"Factory campus", "ResponsibleEmployeeCode":"EMP-0042", "Department":"Stores",
    "DefaultReceivingLocationId":null, "DefaultAcceptedLocationId":"cd872f83-ce52-4415-b93d-fe9e91ee78c3",
    "DefaultQcHoldLocationId":"50fcd1f7-1a50-4e77-94f0-fda820b805e5", "DefaultRejectedLocationId":"1b4ee51d-afb3-454e-a728-c21ff3dcbb72",
    "DefaultRepairableLocationId":null, "DefaultScrapLocationId":null,
    "Status":"Active", "ApprovalStatus":"Approved", "IsActive":true, "Version":5
  },
  "RackBin": {
    "Id":"56372836-9811-43a2-b83e-20ee7c7b244c", "WarehouseId":"d65fd856-2ae0-4cdd-aea5-2d4ca0c70b24",
    "WarehouseCode":"MAIN", "BinCode":"REF-A-01", "RackName":"Refrigeration A", "BinNameNumber":"01", "Zone":"REF",
    "LocationType":"Rack", "MaterialCondition":"Available", "CapacityQuantity":100.000000, "CapacityUom":"NOS",
    "Barcode":"SESS-RACK-REF-A-01", "Description":"Accepted refrigeration components", "Status":"Active",
    "ApprovalStatus":"Approved", "IsActive":true, "Version":3
  }
}
```

Exact upsert fields are:

- Customer: `CustomerCode, LegalCustomerName, TradeName, CustomerType, GstNumber, PanNumber, BillingAddress, ShippingAddress, State, StateCode, Country, ContactPerson, Phone, Email, Industry, PaymentTerms, CreditPeriodDays, CreditLimit, PortalOrganizationId, Version`.
- Vendor: `VendorCode, LegalVendorName, TradeName, VendorType, GstNumber, PanNumber, MsmeStatus, MsmeNumber, ContactPerson, Phone, Email, BillingAddress, ShippingAddress, State, StateCode, Country, MaterialServiceCategories, ApprovedMakes, PaymentTerms, DeliveryTerms, CreditPeriodDays, BankMetadataJson, AttachmentMetadataJson, Version`.
- Item: `ItemCode, Name, DetailedDescription, MaterialType, ItemType, IsReturnable, Uom, ManufacturerMake, Model, PartNumber, HsnSacCode, GstPercentage, TechnicalSpecification, DrawingDocumentReference, QcRequired, SerialNumberTracking, BatchTracking, ShelfLifeTracking, MinimumStock, MaximumStock, ReorderLevel, PreferredVendorCode, StandardEstimatedPrice, Barcode, BarcodeSymbology, ImageStorageKey, ImageFileName, ImageContentType, Version`.
- Warehouse: `WarehouseCode, Name, WarehouseType, Location, ResponsibleEmployeeCode, DepartmentCode, DefaultReceivingLocationId, DefaultAcceptedLocationId, DefaultQcHoldLocationId, DefaultRejectedLocationId, DefaultRepairableLocationId, DefaultScrapLocationId, Version`.
- Rack/bin: `WarehouseCode, BinCode, RackName, BinNameNumber, Zone, LocationType, MaterialCondition, CapacityQuantity, CapacityUom, Barcode, Description, Version`.

POST sends `Version:null`; PUT sends the version returned by GET. Commercial fields may be `null` in responses when the role lacks `ViewCommercialValues`.

List item fields are exact: `CustomerSummary` = `Id, CustomerCode, Name, GstNumber, PanNumber, PortalOrganizationId, Status, ApprovalStatus, IsActive, Version, CreditLimit`; `VendorSummary` = `Id, VendorCode, Name, GstNumber, PanNumber, ApprovalStatus, VendorStatus, IsActive, Version, BankMetadata`; `ItemSummary` = `Id, ItemCode, Name, Uom, MaterialType, ItemType, IsReturnable, ManufacturerMake, Model, PartNumber, MinimumStock, MaximumStock, ReorderLevel, Status, ApprovalStatus, IsActive, Version`; `WarehouseSummary` = `Id, WarehouseCode, Name, WarehouseType, Location, Status, ApprovalStatus, IsActive, Version`; `RackBinSummary` = `Id, WarehouseId, WarehouseCode, BinCode, RackName, BinNameNumber, Zone, LocationType, MaterialCondition, Status, ApprovalStatus, IsActive, Version`.

### 7.2 Customer and vendor routes

| Method and path | Request | Success response | Required page permission | Endpoint-specific errors |
|---|---|---|---|---|
| `GET /api/v1/masters/customers?page=&pageSize=&search=&status=&type=&sortBy=&sortDirection=` | none | `PagedResponse<CustomerSummary>` | `masters.customers:View` | invalid sort `400` |
| `GET /api/v1/masters/customers/{customerCode}` | none | `Customer` | `masters.customers:View` | `404` |
| `POST /api/v1/masters/customers` | editable `Customer`, `Version:null` | `201 CustomerSummary` | `masters.customers:Create` | GST/PAN validation `400`; duplicate `409` |
| `PUT /api/v1/masters/customers/{customerCode}` | editable `Customer` plus `Version` | `200 Customer` | `masters.customers:Update` | `404`, stale `409` |
| `POST /api/v1/masters/customers/{code}/{action}` | `ActionRequest` | `LifecycleResult` | action mapping below | state/stale `409`; self-approval `403` |
| `GET /api/v1/masters/customers/{code}/status-history` | none | `StatusHistory[]` | `masters.customers:ViewAuditHistory` | common |
| `GET /api/v1/masters/customers/{code}/approval-history` | none | `ApprovalHistory[]` | `masters.customers:ViewAuditHistory` | common |
| `GET /api/v1/masters/customers/{code}/audit-history` | none | `AuditHistory[]` | `masters.customers:ViewAuditHistory` | `404` |
| `GET /api/v1/masters/vendors?page=&pageSize=&search=&status=&type=&sortBy=&sortDirection=` | none | `PagedResponse<VendorSummary>` | `masters.vendors:View` | invalid sort `400` |
| `GET /api/v1/masters/vendors/{vendorCode}` | none | `Vendor` | `masters.vendors:View` | `404` |
| `POST /api/v1/masters/vendors` | editable `Vendor`, `Version:null` | `201 VendorSummary` | `masters.vendors:Create` | GST/PAN validation `400`; duplicate `409` |
| `PUT /api/v1/masters/vendors/{vendorCode}` | editable `Vendor` plus `Version` | `200 Vendor` | `masters.vendors:Update` | controlled fields force re-verification; stale `409` |
| `POST /api/v1/masters/vendors/{code}/{action}` | `ActionRequest` | `LifecycleResult` | action mapping below | state/policy/stale `409`; self-approval `403` |
| `POST /api/v1/masters/vendors/{code}/verify-commercial` | `ActionRequest` | `200 Vendor` | `masters.vendor-qualifications:Verify` and role `ACCOUNTS_HEAD` | missing remarks `400`; wrong role `403`; stale `409` |
| `GET /api/v1/masters/vendors/{code}/status-history` | none | `StatusHistory[]` | `masters.vendors:ViewAuditHistory` | common |
| `GET /api/v1/masters/vendors/{code}/approval-history` | none | `ApprovalHistory[]` | `masters.vendors:ViewAuditHistory` | common |
| `GET /api/v1/masters/vendors/{code}/audit-history` | none | `AuditHistory[]` | `masters.vendors:ViewAuditHistory` | `404` |

Customer actions are `submit:Submit`, `approve:Approve`, `reject:Reject`, `request-clarification:RequestClarification`, `request-revision:RequestRevision`, `resubmit:Resubmit`, `hold:Deactivate`, `reactivate:Update`, `deactivate:Deactivate`. Vendor actions add `blacklist:Deactivate` and otherwise use the same mapping. Each permission is on the resource's page key.

### 7.3 Item, warehouse and rack/bin routes

| Resource base | List query filters | Detail key | Page key |
|---|---|---|---|
| `/api/v1/inventory/items` | `search,status,category,sortBy,sortDirection,page,pageSize` | `{code}` | `masters.items` |
| `/api/v1/inventory/warehouses` | `search,status,type,sortBy,sortDirection,page,pageSize` | `{code}` | `masters.warehouses` |
| `/api/v1/inventory/rack-bins` | `search,status,type,warehouseCode,sortBy,sortDirection,page,pageSize` | `{id:guid}` | `masters.rack-bins` |

For each resource the implemented routes are:

| Method and relative path | Request | Success response | Permission | Errors |
|---|---|---|---|---|
| `GET {base}` | none | `PagedResponse<Item|Warehouse|RackBin summary>` | `{page}:View` | invalid filters `400` |
| `GET {base}/{key}` | none | corresponding full object | `{page}:View` | `404` |
| `POST {base}` | editable object, no `Id`; `Version:null` | `201` summary (`RackBin` create returns `{ "Id":"...", "BinCode":"REF-A-01", "Version":1 }`) | `{page}:Create` | invalid FK/duplicate `400/409` |
| `PUT {base}/{key}` | editable object plus `Version` | `200` full object | `{page}:Update` | `404`, stale `409` |
| `POST {base}/{key}/{action}` | `ActionRequest` | `LifecycleResult` | mapping below | state/stale `409`; self-approval `403` |
| `GET {base}/{key}/status-history` | none | `StatusHistory[]` | `{page}:ViewAuditHistory` | common |
| `GET {base}/{key}/approval-history` | none | `ApprovalHistory[]` | `{page}:ViewAuditHistory` | common |
| `GET {base}/{key}/audit-history` | none | `AuditHistory[]` | `{page}:ViewAuditHistory` | `404` |

Item actions: `submit:Submit`, `approve:Approve`, `reject:Reject`, `request-clarification:RequestClarification`, `request-revision:RequestRevision`, `resubmit:Resubmit`, `hold:Deactivate`, `reactivate:Update`, `deactivate:Deactivate`. Warehouse and rack/bin actions: `submit:Submit`, `approve:Approve`, `reject:Reject`, `hold:Deactivate`, `reactivate:Update`, `deactivate:Deactivate`.

## 8. Implemented purchase-requisition and stock-check endpoints

```json
{
  "OrganizationId":"SESS-PVT", "RequestingDepartmentCode":"PRODUCTION", "RequesterEmployeeCode":"EMP-0102",
  "RequiredByDate":"2026-09-15", "Priority":"Normal", "PurposeJustification":"Components for September build",
  "DeliveryWarehouseCode":"MAIN", "CostCentre":"PROD-01", "ProjectReference":"PRJ-2026-004",
  "ServiceReference":null, "WorkOrderReference":null, "CustomerReference":"Example Foods",
  "Lines":[{"ItemCode":"COMP-001", "RequestedQuantity":2.000000, "EstimatedUnitPrice":225000.00,
    "RequiredDate":"2026-09-15", "PreferredWarehouseCode":"MAIN", "ProjectReference":"PRJ-2026-004",
    "MachineReference":"SESS-PVT-2026-0042", "ServiceReference":null}]
}
```

The update body removes `OrganizationId`, `RequestingDepartmentCode`, and `RequesterEmployeeCode`, keeps all other header/line fields, and adds `"Version":12`.

`PurchaseRequisitionDetail`:

```json
{
  "Id":"6cc80076-d6eb-49d4-9aac-ac0a06b6e7cb", "PrNumber":"PR-2026-0042", "OrganizationId":"SESS-PVT",
  "RequestingDepartment":"Production", "RequesterEmployeeCode":"EMP-0102", "RequestDate":"2026-08-27",
  "RequiredByDate":"2026-09-15", "Priority":"Normal", "PurposeJustification":"Components for September build",
  "DeliveryWarehouseCode":"MAIN", "CostCentre":"PROD-01", "ProjectReference":"PRJ-2026-004", "ServiceReference":null,
  "WorkOrderReference":null, "CustomerReference":"Example Foods", "Status":"Approved", "ApprovalRoute":"L2",
  "EstimatedTotal":450000.00, "Version":12,
  "Lines":[{"Id":"57b817c9-8da8-47a6-a41c-a1970d56b955", "LineNumber":1, "ItemCode":"COMP-001",
    "ItemName":"Semi-hermetic compressor", "Uom":"NOS", "RequestedQuantity":2.000000,
    "EstimatedUnitPrice":225000.00, "EstimatedLineTotal":450000.00, "OnHand":1.000000,
    "ActiveReserved":0.000000, "Available":1.000000, "ReservedQuantity":1.000000,
    "ShortageQuantity":1.000000, "HandoffQuantity":1.000000, "LineStatus":"PartiallyReserved"}]
}
```

| Method and path | Request | Success response | Required page permission | Endpoint-specific errors |
|---|---|---|---|---|
| `GET /api/v1/purchase/requisitions?page=&pageSize=&search=&status=&sortBy=&sortDirection=` | none | `PagedResponse<PurchaseRequisitionSummary>` | `purchase.requisitions:View` | invalid sort `400` |
| `GET /api/v1/purchase/requisitions/{prNumber}` | none | `PurchaseRequisitionDetail` | `purchase.requisitions:View` | `404` |
| `POST /api/v1/purchase/requisitions` | create JSON above | `201 PurchaseRequisitionDetail` | `purchase.requisitions:Create` | organization/item rules `400`; duplicate `409` |
| `PUT /api/v1/purchase/requisitions/{prNumber}` | update body plus `Version` | `200 PurchaseRequisitionDetail` | `purchase.requisitions:Update` | non-draft/stale `409` |
| `POST /api/v1/purchase/requisitions/{prNumber}/submit` | `PRAction` below | updated detail | `purchase.requisitions:Submit` | state/stale `409` |
| `POST /api/v1/purchase/requisitions/{prNumber}/verify` | `PRAction` | updated detail | `purchase.requisitions:Verify` | state/stale `409` |
| `POST /api/v1/purchase/requisitions/{prNumber}/approve` | `PRAction` | updated detail | `purchase.requisition-approvals:Approve` | matrix/self-approval `403/409` |
| `POST /api/v1/purchase/requisitions/{prNumber}/reject` | `PRAction` | updated detail | `purchase.requisition-approvals:Reject` | state/stale `409` |
| `POST /api/v1/purchase/requisitions/{prNumber}/request-revision` | `PRAction` | updated detail | `purchase.requisition-approvals:RequestRevision` | state/stale `409` |
| `POST /api/v1/purchase/requisitions/{prNumber}/resubmit` | `PRAction` | updated detail | `purchase.requisitions:Resubmit` | state/stale `409` |
| `POST /api/v1/purchase/requisitions/{prNumber}/cancel` | `PRAction` | updated detail | `purchase.requisitions:Cancel` | state/stale `409` |
| `POST /api/v1/purchase/requisitions/{prNumber}/hold` | `PRAction` | updated detail | `purchase.requisition-approvals:Update` | state/stale `409` |
| `POST /api/v1/purchase/requisitions/{prNumber}/stock-check` | `StockCheckRequest` below | `StockCheckResult` | `stores.stock-check:Verify` | not approved/location invalid `400/409` |
| `GET /api/v1/purchase/requisitions/{prNumber}/status-history` | none | `PurchaseRequisitionHistorySummary[]` | `purchase.requisitions:ViewAuditHistory` | `404` |
| `GET /api/v1/purchase/requisitions/{prNumber}/approval-history` | none | `PurchaseRequisitionHistorySummary[]` | `purchase.requisition-approvals:ViewAuditHistory` | `404` |
| `GET /api/v1/purchase/requisitions/reservations?page=&pageSize=` | none | `PagedResponse<StockReservationSummary>` | `stores.reservations:View` | common |
| `GET /api/v1/purchase/requisitions/handoffs?page=&pageSize=` | none | `PagedResponse<PurchaseRequirementHandoffSummary>` | `purchase.requirement-handoff:View` | common |

```json
{
  "PRAction":{"Remarks":"Ready", "Version":12, "IdempotencyKey":"pr-submit-0042"},
  "StockCheckRequest":{"Remarks":"Physical and reserved stock checked", "Version":15,
    "IdempotencyKey":"stock-check-pr-0042-v1", "Locations":[{"LineNumber":1,"WarehouseCode":"MAIN","RackBinCode":"REF-A-01"}]},
  "StockCheckResult":{"CheckNumber":"SC-PR-2026-0042-20260827103000", "ResultStatus":"PartiallyAvailable", "PrNumber":"PR-2026-0042"},
  "StockReservationSummary":{"Id":"5d379441-833d-40be-8ed3-c05c78773f64", "ReservationNumber":"RSV-PR-2026-0042-001-MAIN-REF-A-01",
    "PrNumber":"PR-2026-0042", "LineNumber":1, "ItemCode":"COMP-001", "WarehouseCode":"MAIN",
    "RackBinCode":"REF-A-01", "ReservedQuantity":1.000000, "Status":"Active"},
  "PurchaseRequirementHandoffSummary":{"Id":"044721bd-88b5-432e-9eaa-37a310b30955", "HandoffNumber":"PHO-PR-2026-0042-001",
    "PrNumber":"PR-2026-0042", "LineNumber":1, "ItemCode":"COMP-001", "WarehouseCode":"MAIN",
    "RackBinCode":"REF-A-01", "HandoffQuantity":1.000000, "Status":"PendingRFQ"}
}
```

## 9. Implemented RFQ, quotation, comparison and purchase-order endpoints

Every command here returns `DocumentResult`:

```json
{"Id":"f5e2aec7-b203-4505-9bde-31e7ef7304c7", "Number":"RFQ-2026-0042", "Status":"Draft", "Version":1}
```

All commands require `IdempotencyKey`. Common errors are `400`, `403`, scoped `404`, and lifecycle/concurrency/idempotency `409`.

| Method and path | Exact request body | Success | Required page permission |
|---|---|---|---|
| `POST /api/v1/purchase/rfqs` | `{ "QuoteDueAt":"2026-09-02T12:00:00Z", "CurrencyCode":"INR", "IsSingleSource":false, "SingleSourceJustification":null, "IdempotencyKey":"rfq-create-0042", "Lines":[{"PurchaseRequirementHandoffId":"044721bd-88b5-432e-9eaa-37a310b30955","Quantity":1.000000}] }` | `DocumentResult` | `purchase.rfq:Create` |
| `POST /api/v1/purchase/rfqs/{number}/vendors` | `{ "VendorId":"fd43c5d3-85bd-4a06-bcd0-484cab82b75d", "Remarks":"Qualified vendor", "RfqVersion":1, "IdempotencyKey":"rfq-invite-0042-ven1" }` | `DocumentResult` | `purchase.rfq:Submit` |
| `POST /api/v1/purchase/rfq-invitations/{id}/quotations` | `SubmitQuotation` below | `DocumentResult` | `purchase.vendor-quotations:Create` |
| `POST /api/v1/purchase/quotations/{number}/technical-verifications` | `{ "VendorQuotationLineId":"f6a0759e-545e-4b7b-8117-c2bf41bd7f28", "IsCompliant":true, "ComplianceEvidenceJson":"{}", "Remarks":"Meets specification", "QuotationVersion":1, "IdempotencyKey":"tech-verify-q-0042-l1" }` | `DocumentResult` | `purchase.technical-verification:Verify` |
| `POST /api/v1/purchase/comparisons` | `{ "RfqNumber":"RFQ-2026-0042", "RfqVersion":3, "IdempotencyKey":"comparison-create-0042" }` | `DocumentResult` | `purchase.commercial-comparisons:Create` |
| `POST /api/v1/purchase/comparisons/{number}/recommend` | `{ "VendorQuotationId":"91976b44-ef33-448d-ac79-aa0fae3b3447", "RecommendationRemarks":"Lowest compliant offer", "SingleSourceJustification":null, "Version":1, "IdempotencyKey":"comparison-recommend-0042" }` | `DocumentResult` | `purchase.commercial-comparisons:Submit` |
| `POST /api/v1/purchase/comparisons/{number}/approve` | `PurchaseAction` below | `DocumentResult` | `purchase.commercial-comparisons:Approve` |
| `POST /api/v1/purchase/comparisons/{number}/reject` | `PurchaseAction` | `DocumentResult` | `purchase.commercial-comparisons:Reject` |
| `POST /api/v1/purchase/comparisons/{number}/request-revision` | `PurchaseAction` | `DocumentResult` | `purchase.commercial-comparisons:RequestRevision` |
| `POST /api/v1/purchase/comparisons/{number}/resubmit` | `PurchaseAction` | `DocumentResult` | `purchase.commercial-comparisons:Resubmit` |
| `POST /api/v1/purchase/purchase-orders` | `{ "ComparisonNumber":"CMP-2026-0042", "ComparisonVersion":4, "IdempotencyKey":"po-create-0042" }` | `DocumentResult` | `purchase.po:Create` |
| `POST /api/v1/purchase/purchase-orders/{number}/submit` | `PurchaseAction` | `DocumentResult` | `purchase.po:Submit` |
| `POST /api/v1/purchase/purchase-orders/{number}/issue` | `PurchaseAction` | `DocumentResult` | `purchase.po:Issue` |
| `POST /api/v1/purchase/purchase-orders/{number}/amend` | `{ "AmendmentReason":"Delivery date revised", "PaymentTerms":"30 days", "DeliveryTerms":"FOR SESS", "WarrantyTerms":"13 months from bill", "Version":5, "IdempotencyKey":"po-amend-0042-v5" }` | `DocumentResult` | `purchase.po:Update` |
| `POST /api/v1/purchase/purchase-orders/{number}/revise-rejected` | `{ "RevisionReason":"Corrected delivery terms", "PaymentTerms":"30 days", "DeliveryTerms":"FOR SESS", "WarrantyTerms":"13 months from bill", "RejectedVersion":5, "IdempotencyKey":"po-revise-0042-v5" }` | `DocumentResult` | `purchase.po:Update` |
| `POST /api/v1/purchase/purchase-orders/{number}/approve` | `PoApprovalAction` below | `DocumentResult` | `purchase.po:Approve` |
| `POST /api/v1/purchase/purchase-orders/{number}/reject` | `PoApprovalAction` | `DocumentResult` | `purchase.po:Reject` |
| `POST /api/v1/purchase/purchase-orders/{number}/cancel` | `{ "Reason":"Requirement withdrawn", "Version":6, "IdempotencyKey":"po-cancel-0042-v6" }` | `DocumentResult` | `purchase.po:Cancel` |
| `POST /api/v1/purchase/material-followup/{id}/transition` | `{ "ToStatus":"InProgress", "Reason":"Vendor confirmed dispatch", "Version":1, "IdempotencyKey":"followup-0042-progress" }` | `DocumentResult` | `purchase.material-followup:Update` |

```json
{
  "PurchaseAction":{"Remarks":"Approved", "Version":4, "IdempotencyKey":"cmp-approve-0042-v4"},
  "PoApprovalAction":{"Remarks":"Approved", "Version":5, "ExpectedCurrentVersion":5, "IdempotencyKey":"po-approve-0042-v5"},
  "SubmitQuotation":{
    "VendorQuoteReference":"CP-2026-881", "CurrencyCode":"INR", "PaymentTerms":"30 days", "DeliveryTerms":"FOR SESS",
    "WarrantyTerms":"13 months from bill", "RequestLateAuthorization":false, "LateAuthorizationRemarks":null,
    "SubmissionSource":"Portal", "ReceivedAt":"2026-08-30T09:00:00Z", "AttachmentObjectKey":"quotations/CP-2026-881.pdf",
    "AttachmentSha256":"8c14f0...64-hex-characters", "VendorAttestation":"I confirm this offer", "InvitationVersion":1,
    "PreviousQuotationVersion":null, "IdempotencyKey":"quote-cp-881-v1", "HeaderDiscountValue":0.00,
    "Lines":[{"RequestForQuotationLineId":"8b331455-a839-42c9-8d16-09e2800db09c", "Quantity":1.000000,
      "UnitRate":220000.00, "DiscountValue":0.00, "PackingForwarding":1000.00, "Freight":2500.00,
      "Insurance":0.00, "OtherCharges":0.00, "PromisedDeliveryDate":"2026-09-12", "HsnSacCode":"84143000",
      "SupplierStateCode":"29", "PlaceOfSupplyStateCode":"29", "VendorRegistrationType":"Regular", "RoundOff":0.00}]
  }
}
```

| Read method and path | Success response | Permission | Errors |
|---|---|---|---|
| `GET /api/v1/purchase/rfqs/{number}` | persisted RFQ header plus `Lines[]` | `purchase.rfq:View` | scoped `404` |
| `GET /api/v1/purchase/comparisons/{number}` | comparison plus `Lines[]`; commercial numbers masked without permission | `purchase.commercial-comparisons:View` | invalid parent `409` |
| `GET /api/v1/purchase/purchase-orders/{number}` | current PO plus `Lines[]`; commercial numbers masked without permission | `purchase.po:View` | scoped `404` |
| `GET /api/v1/purchase/quotations/{number}/attachment` | attachment metadata below | `purchase.vendor-quotations:Download` | scoped `404` |
| `GET /api/v1/purchase/material-followup?page=1&pageSize=50` | legacy follow-up page below | `purchase.material-followup:View` | page outside `1..100` `400` |

RFQ exposes `Id`, `CompanyId`, `OrganizationId`, `RfqNumber`, `FinancialYear`, `SequenceNumber`, `PurchaseRequisitionId`, `RequestingDepartmentId`, `DeliveryWarehouseId`, `OwnerEmployeeId`, `QuoteDueAt`, `CurrencyCode`, `Status`, `IsSingleSource`, `SingleSourceJustification`, `IssuedAt`, `IsActive`, `Version`, and `Lines`. Clients must not bind to ORM navigation/audit fields outside this list.

```json
{
  "QuotationAttachment":{"QuotationNumber":"QUO-2026-0042-V1", "AttachmentObjectKey":"quotations/CP-2026-881.pdf",
    "AttachmentSha256":"8c14f0...64-hex-characters", "SubmissionSource":"Portal", "ReceivedAt":"2026-08-30T09:00:00Z"},
  "MaterialFollowUpPage":{"Page":1, "PageSize":50, "Items":[{
    "Id":"54e94387-fe1d-45b6-a88e-062aa4452212", "HandoffNumber":"MFU-PO-2026-0042-001",
    "PurchaseOrderId":"861840b7-19b7-4e0a-8af5-f5fac14e909a", "PurchaseOrderLineId":"68c1d771-baa2-44c2-b1e4-677ab7cdb814",
    "OrderedQuantitySnapshot":1.000000, "Status":"PendingFollowUp", "HandoffAt":"2026-08-31T08:00:00Z",
    "DepartmentId":"ca9be5cc-7ab3-40d2-abbe-eb6863fcc71a", "WarehouseId":"d65fd856-2ae0-4cdd-aea5-2d4ca0c70b24",
    "OwnerId":"145e2c65-3f72-4ef3-b7d0-9f323404298c"}]}
}
```

## 10. Implemented controlled-configuration endpoints

All routes start `/api/v1/rev869a/configuration`, require authentication and company/record scope, and return the standard errors. `OrganizationId` must match the session. Effective ranges are inclusive.

| Method and path | Exact request body | Success response | Required page permission | Special errors |
|---|---|---|---|---|
| `GET /policies` | none | `OrganizationPolicy[]` | `security.operational-scopes:View` | common |
| `POST /employee-identities` | `{ "OrganizationId":"SESS-PVT", "Issuer":"https://login.example.com/realms/sess", "Subject":"00u1abc234xyz", "EmployeeCode":"EMP-0042", "IdentityType":"HUMAN", "EffectiveFrom":"2026-08-27", "EffectiveTo":null, "Remarks":"OIDC mapping" }` | `201 { "Id":"..." }` | `security.employee-identities:Create` | wrong company `403`; invalid/in-use identity `400/409` |
| `POST /operational-scopes` | `{ "OrganizationId":"SESS-PVT", "EmployeeCode":"EMP-0042", "DepartmentCode":"STORES", "WarehouseCode":"MAIN", "RackBinId":null, "OwnRecordsOnly":false, "AllowsPrivilegedCrossScope":false, "EffectiveFrom":"2026-08-27", "EffectiveTo":null, "Remarks":"Stores scope" }` | `201 { "Id":"..." }` | `security.operational-scopes:Create` | unassigned scope/overlap `409` |
| `POST /uoms` | `{ "Code":"MTR", "Name":"Metre", "MeasurementDimension":"LENGTH" }` | `201 Uom` | `masters.uoms:Create` | duplicate `409` |
| `POST /uom-conversions` | `{ "OrganizationId":"SESS-PVT", "FromUomCode":"MTR", "ToUomCode":"MM", "MeasurementDimension":"LENGTH", "ConversionFactor":1000.000000, "EffectiveFrom":"2026-08-27", "EffectiveTo":null, "Remarks":"Metric conversion" }` | `201 { "Id":"..." }` | `masters.uom-conversions:Create` | invalid/overlap `400/409` |
| `POST /tax-gst` | `TaxGstRequest` below | `201 { "Id":"..." }` | `settings.tax-gst:Create` | invalid split/overlap `400/409` |
| `POST /commercial-values/preview` | `{ "CurrencyCode":"INR", "TaxableValue":1000.00, "TaxValue":180.00, "FreightAndOtherCharges":50.00, "DiscountValue":0.00, "RoundingScale":2 }` | `{ "CurrencyCode":"INR", "TaxableValue":1000.00, "TaxValue":180.00, "FreightAndOtherCharges":50.00, "DiscountValue":0.00, "TotalPayableValue":1230.00, "RoundingScale":2 }` | `settings.tax-gst:ViewCommercialValues` | invalid scale/overflow `400` |
| `POST /vendor-qualifications` | `VendorQualificationRequest` below plus `Idempotency-Key` header | `201 { "Id":"..." }` | `masters.vendor-qualifications:Create` | missing key `400`; overlap `409` |
| `POST /vendor-qualifications/{id}/normalize-legacy` | `{ "ExpectedVersion":0, "Remarks":"Adopt retained draft" }` plus key header | `QualificationLifecycleResult` | `masters.vendor-qualifications:Verify` | not actorless draft/stale `409` |
| `POST /vendor-qualifications/{id}/verify` | lifecycle body plus key header | result | `masters.vendor-qualifications:Verify` | state/stale `409` |
| `POST /vendor-qualifications/{id}/approve` | lifecycle body plus key header | result | `masters.vendor-qualifications:Approve` | self-approval/state `403/409` |
| `POST /vendor-qualifications/{id}/reject` | lifecycle body plus key header | result | `masters.vendor-qualifications:Approve` | state/stale `409` |
| `POST /vendor-qualifications/{id}/request-correction` | lifecycle body plus key header | result | `masters.vendor-qualifications:Approve` | state/stale `409` |
| `POST /warehouse-condition-locations` | `{ "OrganizationId":"SESS-PVT", "WarehouseCode":"MAIN", "RackBinId":"56372836-9811-43a2-b83e-20ee7c7b244c", "ConditionCode":"AVAILABLE", "EffectiveFrom":"2026-08-27", "EffectiveTo":null, "Remarks":"Normal stock location" }` | `201 { "Id":"...", "LocationKey":"..." }` | `masters.warehouse-condition-locations:Create` | invalid/overlap `400/409` |
| `POST /qc-inspection-policies` | `QcPolicyRequest` below | `201 { "Id":"..." }` | `qc.inspection-policies:Create` | invalid limits/sample/overlap `400/409` |

```json
{
  "TaxGstRequest":{"OrganizationId":"SESS-PVT", "JurisdictionCode":"IN-GST", "HsnSacCode":"84143000",
    "SupplierStateCode":"29", "PlaceOfSupplyStateCode":"29", "VendorRegistrationType":"Regular",
    "GstRate":18.000000, "CgstRate":9.000000, "SgstRate":9.000000, "IgstRate":0.000000,
    "CessRate":0.000000, "IsExempt":false, "IsReverseCharge":false, "CurrencyCode":"INR",
    "RoundingScale":2, "EffectiveFrom":"2026-08-27", "EffectiveTo":null, "Remarks":"Current rate"},
  "VendorQualificationRequest":{"OrganizationId":"SESS-PVT", "VendorCode":"VEN-001",
    "ItemCategoryCode":"REFRIGERATION", "QualificationCode":"APPROVED_SOURCE", "EffectiveFrom":"2026-08-27",
    "EffectiveTo":null, "Remarks":"Documents verified"},
  "QualificationLifecycleResult":{"Id":"aad18d8f-160e-4752-bc3d-e31c6cfaa1d3", "VerificationStatus":"Approved",
    "ApprovalStatus":"PendingApproval", "Version":2},
  "QcPolicyRequest":{"OrganizationId":"SESS-PVT", "ItemCode":null, "ItemCategoryCode":"REFRIGERATION",
    "ParameterCode":"VISUAL_CONDITION", "MeasurementUomCode":"NOS", "LowerLimit":null, "UpperLimit":null,
    "InspectionMethod":"Visual", "SampleSize":1, "EffectiveFrom":"2026-08-27", "EffectiveTo":null,
    "Remarks":"Category visual check"}
}
```

`OrganizationPolicy` exposes `Id`, `CompanyId`, `OrganizationId`, `PolicyCode`, `PolicyValue`, `EffectiveFrom`, `EffectiveTo`, `IsActive`, `Version`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, and `UpdatedBy`. `Uom` exposes `Id`, `Code`, `Name`, `MeasurementDimension`, `QuantityPrecision`, `IsActive`, and audit/version fields.

## 11. Planned Stores API — NOT YET IMPLEMENTED

**Every endpoint in sections 11–14 is a planned contract for mock-data frontend development. Calling it against the current backend returns `404`.** The database schema exists, but the application service and HTTP routes do not.

These contracts carry forward schema-design section 7. No Stores request accepts `CompanyId`; the server derives it from the session. Every planned POST and PUT command requires `Idempotency-Key`; draft PUT routes also require `Version`. Finalized documents are immutable. A correction uses the stated reversal/correction route, never PUT. All list/detail routes are company- and record-scoped.

Planned page keys are part of this frontend contract and must be seeded with the implementation:

| Page key | Screens |
|---|---|
| `stores.gate-entry` | Gate entries |
| `stores.goods-receipts` | GRN, serial capture, receipt position |
| `stores.item-inventory-settings` | Company item settings/barcodes |
| `qc.inspections` | QC queue, inspection and corrections |
| `stores.category-routes` | QC/category routing |
| `stores.job-orders` | Minimal job orders |
| `stores.material-issue-requests` | Issue requests and posting |
| `stores.delivery-challans` | DC, dispatch and returns |
| `notifications.inbox` | Current user's notifications |
| `notifications.admin` | Event/delivery administration |
| `stores.stock-ledger` | Ledger, balances, batches and serial trace |
| `settings.business-rules` | Effective-dated business configuration |

### 11.1 Planned Gate Entry shape

Create request:

```json
{
  "PurchaseOrderId":"861840b7-19b7-4e0a-8af5-f5fac14e909a", "VendorDcNumber":"VDC-8821",
  "VehicleNumber":"KA01AB1234", "ModeOfTransport":"ROAD", "ArrivedAt":"2026-08-27T09:15:00Z",
  "IsoReceiptVerificationJson":{"SchemaVersion":1,"PoCopyVerified":true,"VendorDcVerified":true,"PackageCondition":"GOOD"},
  "Lines":[{"PurchaseOrderLineId":"68c1d771-baa2-44c2-b1e4-677ab7cdb814","DeliveredQuantity":2.000000}]
}
```

Detail response:

```json
{
  "Id":"51e0b93c-51c9-487e-84c2-8e75649f2967", "GateEntryNumber":"GE-2026-0042", "DocumentKind":"NORMAL",
  "ReversesGateEntryId":null, "ReversalReason":null, "PurchaseOrderId":"861840b7-19b7-4e0a-8af5-f5fac14e909a",
  "VendorId":"fd43c5d3-85bd-4a06-bcd0-484cab82b75d", "VendorNameSnapshot":"Cold Parts India Pvt Ltd",
  "VendorDcNumber":"VDC-8821", "VehicleNumber":"KA01AB1234", "ModeOfTransport":"ROAD",
  "ArrivedAt":"2026-08-27T09:15:00Z", "ReceivedByEmployeeId":"145e2c65-3f72-4ef3-b7d0-9f323404298c",
  "IsoReceiptVerificationJson":{"SchemaVersion":1,"PoCopyVerified":true,"VendorDcVerified":true,"PackageCondition":"GOOD"},
  "Status":"DRAFT", "FinalizedAt":null, "FinalizedByEmployeeId":null, "Version":1,
  "Lines":[{"Id":"eed2de10-08bc-44c9-9064-7b54740e56c3", "LineNumber":1,
    "PurchaseOrderLineId":"68c1d771-baa2-44c2-b1e4-677ab7cdb814", "ItemId":"3501e490-33ae-47f8-b9dc-da7c04aaf4bb",
    "ItemCodeSnapshot":"COMP-001", "UomSnapshot":"NOS", "DeliveredQuantity":2.000000}]
}
```

### 11.2 Planned GRN and serial shapes

Create-from-Gate request:

```json
{
  "VendorBillNumber":"CPI-2026-991", "VendorBillDate":"2026-08-26", "ReceivedAt":"2026-08-27T09:45:00Z",
  "IsoReceiptVerificationJson":{"SchemaVersion":1,"BillVerified":true,"QuantityVerified":true,"CertificatesReceived":false},
  "Lines":[{"GateEntryLineId":"eed2de10-08bc-44c9-9064-7b54740e56c3","LineValue":440000.00}]
}
```

GRN detail response:

```json
{
  "Id":"1b238680-6b8c-45f0-b528-725248c63aa7", "GrnNumber":"GRN-2026-0042", "DocumentKind":"NORMAL",
  "ReversesGoodsReceiptId":null, "ReversalReason":null, "GateEntryId":"51e0b93c-51c9-487e-84c2-8e75649f2967",
  "PurchaseOrderId":"861840b7-19b7-4e0a-8af5-f5fac14e909a", "VendorId":"fd43c5d3-85bd-4a06-bcd0-484cab82b75d",
  "VendorNameSnapshot":"Cold Parts India Pvt Ltd", "VendorBillNumber":"CPI-2026-991", "VendorBillDate":"2026-08-26",
  "VendorDcNumberSnapshot":"VDC-8821", "ModeOfTransportSnapshot":"ROAD", "ReceivedAt":"2026-08-27T09:45:00Z",
  "IsoReceiptVerificationJson":{"SchemaVersion":1,"BillVerified":true,"QuantityVerified":true,"CertificatesReceived":false},
  "ConfigurationSnapshotJson":{"SchemaVersion":1,"SerialThreshold":{"Value":5000.00},"QcCompletionDays":{"Value":2}},
  "ConfigurationSnapshotHash":"0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
  "QcCompletionDaysSnapshot":2, "QcDueAt":"2026-08-29T10:00:00Z", "Status":"DRAFT", "Version":1,
  "Lines":[{
    "Id":"417c075c-d69e-4bdd-b538-fbedf85852d7", "LineNumber":1, "GateEntryLineId":"eed2de10-08bc-44c9-9064-7b54740e56c3",
    "PurchaseOrderLineId":"68c1d771-baa2-44c2-b1e4-677ab7cdb814", "ItemId":"3501e490-33ae-47f8-b9dc-da7c04aaf4bb",
    "ItemCodeSnapshot":"COMP-001", "ItemNameSnapshot":"Semi-hermetic compressor", "ItemCategoryCodeSnapshot":"REFRIGERATION",
    "HsnSacCodeSnapshot":"84143000", "GstPercentageSnapshot":18.0000, "ModelSnapshot":"4NES-14Y",
    "ManufacturerPartNumberSnapshot":"4NES-14Y-40P", "ManufacturerMakeSnapshot":"BITZER", "UomSnapshot":"NOS",
    "PoOrderedQuantitySnapshot":2.000000, "PriorEffectiveReceivedQuantitySnapshot":0.000000, "RemainingPoQuantitySnapshot":2.000000,
    "DeliveredQuantitySnapshot":2.000000, "ReceivedQuantity":2.000000, "ExcessRejectedQuantity":0.000000,
    "ExcessDisposition":null, "LineValueSnapshot":440000.00, "UnitRateSnapshot":220000.00,
    "SerialThresholdValueSnapshot":5000.00, "SerialCaptureModeSnapshot":"REQUIRED",
    "BillWarrantyLimitDate":"2027-09-26", "InitialWarrantyExpiryDate":"2027-09-26",
    "Serials":[{"InventorySerialId":"673797d5-5647-44ac-8445-b237ef76ee09", "SerialOrdinal":1,
      "EnteredSerialNumber":"A12345", "StoredSerialNumberSnapshot":"A12345", "ReceiptDisposition":"QC_INSPECTION",
      "DisambiguationApplied":false,"DuplicateWarningAcknowledged":false}]
  }]
}
```

Serial validation request/response:

```json
{
  "Request":{"Lines":[{"GoodsReceiptLineId":"417c075c-d69e-4bdd-b538-fbedf85852d7",
    "Serials":[{"SerialOrdinal":1,"EnteredSerialNumber":"A12345","StoredSerialNumber":"A12345/2026-27/BITZER",
      "DuplicateWarningAcknowledged":true,"DisambiguationReason":"Duplicate supplier serial; FY and make appended"}]}]},
  "Response":{"IsValid":true,"Warnings":[{"Code":"DUPLICATE_SERIAL","EnteredSerialNumber":"A12345",
    "Message":"StoredSerialNumber must be unique; the disambiguated value is available."}],"Errors":[]}
}
```

### 11.3 Planned QC shape

```json
{
  "Id":"66223edf-a744-4bc1-a9bb-d3ea8a4cbcdf", "InspectionNumber":"QCI-2026-0042",
  "GoodsReceiptLineId":"417c075c-d69e-4bdd-b538-fbedf85852d7", "DeliveryChallanLineId":null,
  "CurrentRevision":{
    "Id":"8c5a3ec1-30ab-4ec0-b0c8-052395bd0372", "RevisionNumber":1, "RevisionKind":"INITIAL",
    "RevisesRevisionId":null, "CorrectionReason":null, "InspectorEmployeeId":"145e2c65-3f72-4ef3-b7d0-9f323404298c",
    "InspectorBasis":"QC_MANAGER", "FallbackReason":null, "InspectionStartedAt":"2026-08-27T11:00:00Z",
    "InspectionCompletedAt":null, "InspectedQuantity":2.000000, "AcceptedQuantity":0.000000,
    "RejectedQuantity":0.000000, "InspectionShortfallRejectedQuantity":0.000000, "Decision":"ACCEPTED",
    "AcceptedConditionLocationId":"cd872f83-ce52-4415-b93d-fe9e91ee78c3", "Status":"DRAFT", "Version":1,
    "ParameterResults":[], "SerialDispositions":[]
  }
}
```

QC revision PUT sends:

```json
{
  "InspectedQuantity":2.000000, "AcceptedQuantity":1.000000, "RejectedQuantity":1.000000,
  "InspectionShortfallRejectedQuantity":0.000000, "Decision":"PARTIALLY_ACCEPTED",
  "AcceptedConditionLocationId":"cd872f83-ce52-4415-b93d-fe9e91ee78c3",
  "ParameterResults":[{"QcInspectionPolicyId":"a36c095e-ab00-462e-a4b7-0f4463588810", "SampleOrdinal":1,
    "ObservedNumericValue":null,"ObservedTextValue":"No transport damage","Result":"PASS","Remarks":null}],
  "SerialDispositions":[{"InventorySerialId":"673797d5-5647-44ac-8445-b237ef76ee09","Disposition":"ACCEPTED","Reason":null}],
  "Version":1
}
```

When there is no effective policy, `ParameterResults:[]` is valid and must never block finalisation.

## 12. Planned outbound, notification and ledger shapes — NOT YET IMPLEMENTED

### 12.1 Job Order

```json
{
  "Id":"3f631272-f008-4f52-8513-6cce8d2c1c07", "JobOrderNumber":"JO-2026-0042",
  "MachineModel":"ICE-1000", "MachineSerial":"SESS-PVT-2026-0042", "CustomerName":"Example Foods Pvt Ltd",
  "Status":"OPEN", "JobOrderDate":"2026-08-27", "PlannedCompletionDate":"2026-10-15",
  "InstallationDate":null, "ClosedAt":null, "Version":2
}
```

Create sends `MachineModel`, `CustomerName`, `JobOrderDate`, and nullable `PlannedCompletionDate`; `JobOrderNumber` and `MachineSerial` are generated. PUT sends the editable fields plus `Version`. Installation-date request is `{ "InstallationDate":"2026-10-20", "Remarks":"Installation completion report received", "Version":3 }`.

### 12.2 Material Issue Request

```json
{
  "Id":"c3c4475f-1df1-40ae-8ef5-8961ee35417c", "RequestNumber":"MIR-2026-0042",
  "Purpose":"FACTORY_ASSEMBLY", "DestinationType":"JOB_ORDER", "JobOrderId":"3f631272-f008-4f52-8513-6cce8d2c1c07",
  "CustomerId":null, "VendorId":null, "DestinationDepartmentId":null, "DestinationNameSnapshot":"JO-2026-0042",
  "RequestingDepartmentId":"ca9be5cc-7ab3-40d2-abbe-eb6863fcc71a",
  "RequestedByEmployeeId":"8d6a1071-f939-40fd-a312-2dc9c325eef5", "RequiredDate":"2026-09-05",
  "Status":"DRAFT", "ApprovalRouteSnapshotJson":{"SchemaVersion":1,"ApproverRole":"PRODUCTION_MANAGER"},
  "ApprovedAt":null, "ApprovedByEmployeeId":null, "Version":1,
  "Lines":[{"Id":"384803f7-05c8-4aec-a9a1-bb24e85bcb56", "LineNumber":1,
    "ItemId":"3501e490-33ae-47f8-b9dc-da7c04aaf4bb", "ItemCodeSnapshot":"COMP-001",
    "ItemNameSnapshot":"Semi-hermetic compressor", "UomSnapshot":"NOS", "RequestedQuantity":1.000000,
    "IssuedQuantity":0.000000, "Remarks":"For assembly"}]
}
```

Create/PUT sends `Purpose`, exactly one destination identifier appropriate to `DestinationType`, `DestinationName`, `RequestingDepartmentId`, `RequiredDate`, and `Lines:[{ItemId,RequestedQuantity,Remarks}]`; PUT adds `Version`. Issue request:

```json
{
  "PostingDate":"2026-09-05", "Remarks":"Issued to production",
  "Lines":[{"MaterialIssueRequestLineId":"384803f7-05c8-4aec-a9a1-bb24e85bcb56",
    "Quantity":1.000000,"FromConditionLocationId":"cd872f83-ce52-4415-b93d-fe9e91ee78c3",
    "InventorySerialIds":["673797d5-5647-44ac-8445-b237ef76ee09"]}]
}
```

### 12.3 Delivery Challan

```json
{
  "Id":"4d55b9f2-0366-46bb-85dd-7ea9bdedb756", "DcNumber":"DC-2026-0042", "Direction":"OUTBOUND",
  "ParentDeliveryChallanId":null, "DcType":"RETURNABLE", "Purpose":"REJECTED_MATERIAL",
  "MaterialIssueRequestId":null, "JobOrderId":null, "VendorId":"fd43c5d3-85bd-4a06-bcd0-484cab82b75d",
  "CustomerId":null, "DestinationNameSnapshot":"Cold Parts India Pvt Ltd", "ExternalReferenceNumber":"CPI-2026-991",
  "DispatchEvidenceJson":{"SchemaVersion":1,"TransportMode":"ROAD","AcknowledgementRequired":true},
  "ExpectedReturnDate":"2026-09-10", "DocumentDate":"2026-08-28", "Status":"OUTSTANDING",
  "ApprovalRouteSnapshotJson":null, "DispatchedAt":"2026-08-28T09:00:00Z", "ReceivedAt":null,
  "HandledByEmployeeId":"145e2c65-3f72-4ef3-b7d0-9f323404298c", "Version":3,
  "Lines":[{"Id":"b7f70dab-93fd-4317-81ca-f87745d41d18", "LineNumber":1,
    "QcInspectionRevisionId":"8c5a3ec1-30ab-4ec0-b0c8-052395bd0372", "GoodsReceiptLineId":null,
    "MaterialIssueRequestLineId":null, "ItemId":"3501e490-33ae-47f8-b9dc-da7c04aaf4bb",
    "InventorySerialId":"673797d5-5647-44ac-8445-b237ef76ee09", "ItemCodeSnapshot":"COMP-001",
    "UomSnapshot":"NOS", "Quantity":1.000000, "DispatchedWeight":null, "ReturnedWeight":null,
    "CalculatedScrapWeight":null, "VendorWeightExplanation":null, "RequiresQcSnapshot":false}]
}
```

Create/PUT sends `Direction`, nullable `ParentDeliveryChallanId`, `DcType`, `Purpose`, the applicable MIR/Job/Vendor/Customer IDs, `DestinationName`, nullable `ExternalReferenceNumber`, `DispatchEvidenceJson`, `ExpectedReturnDate`, `DocumentDate`, and lines with one typed source and quantity/serial/weight fields. PUT adds `Version`. An inbound return sends `ParentDeliveryChallanId` and each line's `ParentDeliveryChallanLineId`, `Quantity`, `ReturnedWeight`, `VendorWeightExplanation`, and optional `ReplacementGoodsReceiptLineId`.

### 12.4 Notification

```json
{
  "UnreadCount":2,
  "Items":[{
    "RecipientId":"8fd71a52-9587-4208-adbb-62c499d30237", "EventId":"edec1961-e92f-445e-90f7-414c536d4df8",
    "EventType":"STORES.QC_OVERDUE", "SourceEntityType":"GoodsReceipt", "SourceEntityId":"1b238680-6b8c-45f0-b528-725248c63aa7",
    "SourceReferenceSnapshot":"GRN-2026-0042", "ResolvedRoleCodes":["QC_MANAGER"],
    "Title":"QC overdue for GRN-2026-0042", "Body":"QC has been pending for 2 days.",
    "DeepLink":"/stores/qc/queue?grn=GRN-2026-0042", "InAppAvailableAt":"2026-08-29T10:00:00Z",
    "ReadAt":null
  }]
}
```

The application header obtains the badge from `UnreadCount`. Opening the inbox does not mark anything read. Opening an item/deep link then calling its explicit read endpoint sets `ReadAt`. Bulk mark-read affects only IDs visible to the current employee in the current company.

Internal enqueue request:

```json
{
  "EventType":"STORES.RETURNABLE_DC_OVERDUE", "SourceEntityType":"DeliveryChallan",
  "SourceEntityId":"4d55b9f2-0366-46bb-85dd-7ea9bdedb756", "SourceReference":"DC-2026-0042",
  "RecipientRoleCodes":["PURCHASE_MANAGER","TECHNICAL_DIRECTOR","MANAGING_DIRECTOR"],
  "Title":"Returnable DC overdue", "Body":"DC-2026-0042 was due on 2026-09-10.",
  "DeepLink":"/stores/delivery-challans/4d55b9f2-0366-46bb-85dd-7ea9bdedb756",
  "PayloadJson":{"SchemaVersion":1,"ExpectedReturnDate":"2026-09-10"},
  "NotBeforeAt":"2026-09-11T00:00:00Z", "CancellationKey":"DC-2026-0042-RETURNED"
}
```

Recipient resolution is by role and company only, never hard-coded employee ID. `PURCHASE_MANAGER` is the company role that currently resolves to PRIYA E; all active company holders receive the event.

### 12.5 Ledger, balances and configuration

```json
{
  "LedgerItem":{"Id":"b28c2676-4078-41f5-ad47-635249d77826", "LedgerContractVersion":2,
    "PostingBatchId":"878ac73a-f310-46a8-838f-bf5127d6cab7", "ItemId":"3501e490-33ae-47f8-b9dc-da7c04aaf4bb",
    "WarehouseId":"d65fd856-2ae0-4cdd-aea5-2d4ca0c70b24", "RackBinId":"56372836-9811-43a2-b83e-20ee7c7b244c",
    "ConditionCode":"AVAILABLE", "InventorySerialId":"673797d5-5647-44ac-8445-b237ef76ee09",
    "QuantityIn":1.000000, "QuantityOut":0.000000, "ReferenceType":"QC_DISPOSITION",
    "ReferenceNumber":"QCI-2026-0042-R1", "MovementDate":"2026-08-27T12:00:00Z"},
  "Balance":{"ItemId":"3501e490-33ae-47f8-b9dc-da7c04aaf4bb", "ItemCode":"COMP-001",
    "WarehouseId":"d65fd856-2ae0-4cdd-aea5-2d4ca0c70b24", "RackBinId":"56372836-9811-43a2-b83e-20ee7c7b244c",
    "ConditionCode":"AVAILABLE", "Quantity":1.000000},
  "PostingBatch":{"Id":"878ac73a-f310-46a8-838f-bf5127d6cab7", "PostingKind":"QC_DISPOSITION",
    "QcInspectionRevisionId":"8c5a3ec1-30ab-4ec0-b0c8-052395bd0372", "ReferenceType":"QC_INSPECTION",
    "ReferenceNumber":"QCI-2026-0042-R1", "PostingDate":"2026-08-27", "PostedAt":"2026-08-27T12:00:00Z",
    "ReversesPostingBatchId":null, "Movements":[]},
  "Configuration":{"Id":"28138e72-6796-42cf-b886-d93b33177abd", "RuleKey":"SERIAL_CAPTURE_UNIT_RATE_THRESHOLD",
    "ValueType":"DECIMAL", "DecimalValue":5000.00, "IntegerValue":null, "TextValue":null,
    "EffectiveFrom":"2026-08-27T00:00:00Z", "EffectiveTo":null, "Reason":"Initial value", "Version":1}
}
```

Create configuration version request:

```json
{"ValueType":"DECIMAL","DecimalValue":7500.00,"IntegerValue":null,"TextValue":null,
 "EffectiveFrom":"2026-09-01T00:00:00Z","Reason":"Approved threshold revision","ExpectedCurrentVersion":1}
```

Only `TECHNICAL_DIRECTOR`, `MANAGING_DIRECTOR`, and `IT_MANAGER` may append configuration versions. New documents resolve the effective value and snapshot the value/version; submitted/in-flight documents retain their snapshot.

## 13. Planned Stores endpoint catalog — NOT YET IMPLEMENTED

All paths in this section are relative to `/api/v1/stores`. GET requests have no body. `DraftCommand` is `{ "Version":1, "Remarks":"Ready to finalize" }`; `ReversalCommand` is `{ "Version":2, "Reason":"Incorrect quantity" }`. Commands require `Idempotency-Key` even when the example body does not repeat it. Expected endpoint errors are the common envelope plus the stated business conflicts.

### 13.1 Gate Entry

| Method and route | Request | Success response | Permission | Errors |
|---|---|---|---|---|
| `GET /gate-entries?purchaseOrderId=&vendorId=&from=&to=&status=&page=&pageSize=` | none | `PagedResponse<GateEntrySummary>` | `stores.gate-entry:View` | invalid filter `400` |
| `GET /gate-entries/{id}` | none | Gate Entry detail in 11.1 plus history and `GoodsReceiptId` | `stores.gate-entry:View` | `404` |
| `POST /gate-entries` | create body in 11.1 | `201` Gate Entry detail | `stores.gate-entry:Create` | PO/company/vendor/quantity `400/409` |
| `PUT /gate-entries/{id}` | create body plus `Version` | `200` Gate Entry detail | `stores.gate-entry:Update` | finalized/stale `409` |
| `POST /gate-entries/{id}/finalize` | `DraftCommand` | finalized Gate Entry detail | `stores.gate-entry:Submit` | missing lines/ineligible PO/stale `409` |
| `POST /gate-entries/{id}/reversals` | `ReversalCommand` | `201` finalized reversal detail | `stores.gate-entry:Cancel` | downstream not reversed/already reversed `409` |

`GateEntrySummary` contains `Id`, `GateEntryNumber`, `PurchaseOrderId`, `PoNumber`, `VendorId`, `VendorNameSnapshot`, `VendorDcNumber`, `ArrivedAt`, `Status`, `DocumentKind`, `Version`.

### 13.2 GRN, serials and barcode

| Method and route | Request | Success response | Permission | Errors |
|---|---|---|---|---|
| `GET /goods-receipts?gateEntryId=&purchaseOrderId=&vendorId=&billNumber=&from=&to=&status=&page=&pageSize=` | none | `PagedResponse<GoodsReceiptSummary>` | `stores.goods-receipts:View` | invalid filter `400` |
| `GET /goods-receipts/{id}` | none | GRN detail in 11.2 plus posting/QC links | `stores.goods-receipts:View` | `404` |
| `POST /gate-entries/{gateEntryId}/goods-receipt` | create body in 11.2 | `201` GRN detail | `stores.goods-receipts:Create` | Gate not finalized/used; bill duplicate `409` |
| `PUT /goods-receipts/{id}` | create fields, lines/serials and `Version` | `200` GRN detail | `stores.goods-receipts:Update` | finalized/stale `409` |
| `POST /goods-receipts/{id}/serials/validate` | serial-validation request in 11.2 | validation response in 11.2 | `stores.goods-receipts:Update` | malformed quantity/serial `400` |
| `POST /goods-receipts/{id}/finalize` | `DraftCommand` | finalized detail with `StockPostingBatchId` | `stores.goods-receipts:Submit` | no bill/serial mismatch/PO cap/routing `409` |
| `POST /goods-receipts/{id}/reversals` | `ReversalCommand` | `201` finalized reversal with posting batch | `stores.goods-receipts:Cancel` | downstream QC/DC prevents reversal `409` |
| `GET /purchase-orders/{poId}/receipt-position` | none | `ReceiptPosition` below | `stores.goods-receipts:View` | `404` |
| `GET /items/{itemId}/inventory-setting` | none | `ItemInventorySetting` below | `stores.item-inventory-settings:View` | item `404` |
| `POST /items/{itemId}/erp-barcode` | `{ "Reason":"Create ERP barcode" }` | `201 ItemInventorySetting` | `stores.item-inventory-settings:Create` | already allocated `409` |
| `PUT /items/{itemId}/serial-capture-mode` | `{ "SerialCaptureModeOverride":"REQUIRED", "Reason":"Always serialize compressors", "Version":2 }` | updated setting | `stores.item-inventory-settings:Update` | invalid mode/stale `400/409` |
| `GET /items/{itemId}/barcode-label` | none | `{ "Barcode":"SESS-REF-000042", "Symbology":"CODE_128", "LabelText":"COMP-001", "MimeType":"application/pdf", "SuggestedFileName":"COMP-001-barcode.pdf", "ContentBase64":"JVBERi0xLjQK" }` | `stores.item-inventory-settings:Print` | no barcode `404` |
| `GET /items/{itemId}/change-history` | none | `ControlledChangeHistory[]` | `stores.item-inventory-settings:ViewAuditHistory` | `404` |

```json
{
  "ReceiptPosition":{"PurchaseOrderId":"861840b7-19b7-4e0a-8af5-f5fac14e909a", "PoNumber":"PO-2026-0042",
    "Lines":[{"PurchaseOrderLineId":"68c1d771-baa2-44c2-b1e4-677ab7cdb814","OrderedQuantity":2.000000,
      "GateDeliveredQuantity":2.000000,"EffectiveReceivedQuantity":2.000000,"ExcessQuantity":0.000000,"RemainingQuantity":0.000000}]},
  "ItemInventorySetting":{"ItemId":"3501e490-33ae-47f8-b9dc-da7c04aaf4bb","ItemCode":"COMP-001",
    "ErpBarcode":"SESS-REF-000042","SerialCaptureModeOverride":"REQUIRED","EffectiveSerialCaptureMode":"REQUIRED","Version":2}
}
```

### 13.3 QC and category routing

| Method and route | Request | Success response | Permission | Errors |
|---|---|---|---|---|
| `GET /qc/queue?sourceType=&categoryCode=&overdue=&page=&pageSize=` | none | `PagedResponse<QcQueueItem>` below | `qc.inspections:View` | invalid filter `400` |
| `GET /qc/inspections/{id}` | none | logical inspection with all revision shapes from 11.3 and posting links | `qc.inspections:View` | `404` |
| `POST /goods-receipt-lines/{lineId}/qc-inspection` | `{ "InspectorBasis":"QC_MANAGER", "FallbackReason":null }` | `201` QC detail | `qc.inspections:Create` | inspection exists/source unavailable `409` |
| `POST /delivery-challan-lines/{lineId}/qc-inspection` | same body | `201` QC detail | `qc.inspections:Create` | source not QC-required/existing `409` |
| `PUT /qc/revisions/{revisionId}` | revision PUT in 11.3 | `200` QC detail | `qc.inspections:Update` | finalized/reconciliation/stale `409` |
| `POST /qc/revisions/{revisionId}/finalize-and-post` | `DraftCommand` | finalized QC detail with `StockPostingBatchId` | `qc.inspections:Verify` | samples/serials/quantities/location `409` |
| `POST /qc/inspections/{id}/corrections` | `{ "Reason":"Inspection quantity entered incorrectly", "Version":2 }` | `201` QC detail with next draft revision | `qc.inspections:RequestRevision` | no effective final revision/already corrected `409` |
| `GET /category-routes?categoryCode=&effectiveAt=` | none | `StoreCategoryRoute[]` below | `stores.category-routes:View` | invalid date `400` |
| `POST /category-routes` | `CategoryRouteRequest` below | `201 StoreCategoryRoute` | `stores.category-routes:Create` | overlap/location condition `409` |
| `POST /category-routes/{id}/close` | `{ "EffectiveTo":"2026-09-30T23:59:59Z", "Reason":"Replacement route", "Version":2 }` | closed route | `stores.category-routes:Deactivate` | creates coverage gap/stale `409` |

```json
{
  "QcQueueItem":{"InspectionId":"66223edf-a744-4bc1-a9bb-d3ea8a4cbcdf", "SourceType":"GRN",
    "SourceNumber":"GRN-2026-0042", "GoodsReceiptLineId":"417c075c-d69e-4bdd-b538-fbedf85852d7",
    "ItemCode":"COMP-001", "ItemName":"Semi-hermetic compressor", "CategoryCode":"REFRIGERATION",
    "Quantity":2.000000, "QcRackCode":"QC-REF", "ReceivedAt":"2026-08-27T10:00:00Z",
    "QcDueAt":"2026-08-29T10:00:00Z", "AgeHours":49.5, "IsOverdue":true},
  "CategoryRouteRequest":{"ItemCategoryId":"90f76db7-430e-4bc4-9d86-2f5be3d05b52",
    "QcHoldConditionLocationId":"50fcd1f7-1a50-4e77-94f0-fda820b805e5",
    "PendingReturnConditionLocationId":"1b4ee51d-afb3-454e-a728-c21ff3dcbb72",
    "DefaultAcceptedConditionLocationId":"cd872f83-ce52-4415-b93d-fe9e91ee78c3",
    "EffectiveFrom":"2026-09-01T00:00:00Z","EffectiveTo":null,"Reason":"Initial refrigeration route"},
  "StoreCategoryRoute":{"Id":"c7fb6c09-2b3d-47f8-aa13-5b6b9634f346","CategoryCode":"REFRIGERATION",
    "QcRackCode":"QC-REF","PendingReturnRackCode":"QC-REF","DefaultAcceptedRackCode":"REF-A-01",
    "EffectiveFrom":"2026-09-01T00:00:00Z","EffectiveTo":null,"Version":1}
}
```

### 13.4 Minimal Job Order

| Method and route | Request | Success response | Permission | Errors |
|---|---|---|---|---|
| `GET /job-orders?status=&search=&page=&pageSize=` | none | `PagedResponse<JobOrder>` | `stores.job-orders:View` | common |
| `GET /job-orders/{id}` | none | Job Order in 12.1 plus MIR/DC links | `stores.job-orders:View` | `404` |
| `POST /job-orders` | create fields in 12.1 | `201 JobOrder` | `stores.job-orders:Create` | duplicate/format `409` |
| `PUT /job-orders/{id}` | editable fields plus `Version` | updated Job Order | `stores.job-orders:Update` | non-draft/stale `409` |
| `POST /job-orders/{id}/open` | `DraftCommand` | opened Job Order | `stores.job-orders:Submit` | state/stale `409` |
| `POST /job-orders/{id}/installation-date` | body in 12.1 | Job Order plus recomputed component warranties | `stores.job-orders:Update` | invalid date/stale `400/409` |
| `POST /job-orders/{id}/close` | `DraftCommand` | closed Job Order | `stores.job-orders:Update` | installation missing/state `409` |

### 13.5 Material Issue Requests

| Method and route | Request | Success response | Permission | Errors |
|---|---|---|---|---|
| `GET /material-issue-requests?status=&purpose=&jobOrderId=&requiredFrom=&requiredTo=&page=&pageSize=` | none | `PagedResponse<MaterialIssueRequestSummary>` | `stores.material-issue-requests:View` | invalid filter `400` |
| `GET /material-issue-requests/{id}` | none | MIR detail in 12.2 with approvals/postings | `stores.material-issue-requests:View` | `404` |
| `POST /material-issue-requests` | create fields in 12.2 | `201` MIR detail | `stores.material-issue-requests:Create` | destination/quantity `400` |
| `PUT /material-issue-requests/{id}` | create fields plus `Version` | updated detail | `stores.material-issue-requests:Update` | non-draft/stale `409` |
| `POST /material-issue-requests/{id}/submit` | `DraftCommand` | submitted detail with approval snapshot | `stores.material-issue-requests:Submit` | missing destination/route `409` |
| `POST /material-issue-requests/{id}/decisions` | `{ "Decision":"APPROVE", "Remarks":"Required for scheduled build", "Version":2 }` | approved/rejected detail | `stores.material-issue-requests:Approve` or `:Reject` | wrong resolved actor/self/stale `403/409` |
| `POST /material-issue-requests/{id}/issue` | issue body in 12.2 | fulfilled detail plus `StockPostingBatch` | `stores.material-issue-requests:Verify` | not approved/insufficient available/serial mismatch `409` |
| `POST /material-issue-requests/{id}/reversals` | `{ "PostingBatchId":"878ac73a-f310-46a8-838f-bf5127d6cab7", "Reason":"Issue entered against wrong job" }` | reversal batch and updated balances | `stores.material-issue-requests:Cancel` | dependent dispatch/already reversed `409` |

`MaterialIssueRequestSummary` contains `Id`, `RequestNumber`, `Purpose`, `DestinationType`, `DestinationNameSnapshot`, `RequiredDate`, `Status`, `RequestedLineCount`, `RequestedQuantityTotal`, `IssuedQuantityTotal`, `Version`.

### 13.6 Delivery Challans and returns

| Method and route | Request | Success response | Permission | Errors |
|---|---|---|---|---|
| `GET /delivery-challans?direction=&dcType=&purpose=&status=&vendorId=&customerId=&from=&to=&page=&pageSize=` | none | `PagedResponse<DeliveryChallanSummary>` | `stores.delivery-challans:View` | invalid filter `400` |
| `GET /delivery-challans/{id}` | none | DC detail in 12.3 with approvals/postings/returns | `stores.delivery-challans:View` | `404` |
| `GET /delivery-challans/outstanding?overdue=&purpose=&vendorId=&customerId=&page=&pageSize=` | none | `PagedResponse<OutstandingDcSummary>` | `stores.delivery-challans:View` | invalid filter `400` |
| `POST /delivery-challans` | create fields in 12.3 | `201` DC detail | `stores.delivery-challans:Create` | source/destination/type/date `400/409` |
| `PUT /delivery-challans/{id}` | create fields plus `Version` | updated detail | `stores.delivery-challans:Update` | non-draft/stale `409` |
| `POST /delivery-challans/{id}/submit` | `DraftCommand` | submitted detail | `stores.delivery-challans:Submit` | source balance/approval route `409` |
| `POST /delivery-challans/{id}/decisions` | `{ "Decision":"APPROVE", "Remarks":"Approved for dispatch", "Version":2 }` | approved/rejected detail | `stores.delivery-challans:Approve` or `:Reject` | wrong actor/stale `403/409` |
| `POST /delivery-challans/{id}/dispatch` | `{ "DispatchedAt":"2026-08-28T09:00:00Z", "Remarks":"Handed to vendor vehicle", "Version":3 }` | dispatched/outstanding detail plus posting batch | `stores.delivery-challans:Verify` | approval/notification/source balance `409` |
| `POST /delivery-challans/{id}/returns` | inbound-return create body described in 12.3 | `201` inbound draft detail | `stores.delivery-challans:Create` | not returnable/no outstanding quantity `409` |
| `POST /delivery-challans/{returnId}/receive` | `{ "ReceivedAt":"2026-09-05T10:00:00Z", "Remarks":"Partial return received", "Version":1 }` | received detail, parent balance and posting batch | `stores.delivery-challans:Verify` | weight/scrap/QC/source reconciliation `409` |
| `POST /delivery-challans/{id}/reversals` | `ReversalCommand` | reversal detail and posting batch | `stores.delivery-challans:Cancel` | dependent return/already reversed `409` |
| `GET /delivery-challans/{id}/notifications` | none | event, recipients and delivery attempts | `stores.delivery-challans:ViewAuditHistory` | `404` |

`DeliveryChallanSummary` contains `Id`, `DcNumber`, `Direction`, `DcType`, `Purpose`, `DestinationNameSnapshot`, `DocumentDate`, `ExpectedReturnDate`, `Status`, `OutstandingQuantity`, `IsOverdue`, `Version`. Returnable closure is derived after full signed return reconciliation; there is no force-close endpoint. A non-returnable creation transaction must enqueue TD/MD notification before succeeding.

### 13.7 Shared notifications

| Method and route | Request | Success response | Permission | Errors |
|---|---|---|---|---|
| `GET /notifications?state=unread&page=&pageSize=` | none | inbox shape in 12.4 | `notifications.inbox:View` | invalid state `400` |
| `GET /notifications/{recipientId}` | none | inbox item plus `DeliveryAttempts[]` | `notifications.inbox:View` and recipient ownership | another recipient/scoped `404` |
| `POST /notifications/{recipientId}/read` | `{}` | `{ "RecipientId":"...", "ReadAt":"2026-08-29T11:00:00Z" }` | `notifications.inbox:View` and ownership | scoped `404` |
| `POST /notifications/read-visible` | `{ "RecipientIds":["8fd71a52-9587-4208-adbb-62c499d30237"] }` | `{ "MarkedRead":1, "UnreadCount":1 }` | `notifications.inbox:View` | hidden/other-company IDs `400` |
| `POST /internal/notification-events` | enqueue body in 12.4 | `201 NotificationEvent` | trusted internal service principal, not a page permission | missing role/config `409` |
| `POST /internal/notification-events/{id}/cancel` | `{ "CancellationReason":"QC completed before due time" }` | cancelled event | trusted internal service principal | active/completed event `409` |
| `GET /admin/notification-events?status=&eventType=&from=&to=&page=&pageSize=` | none | `PagedResponse<NotificationEvent>` | `notifications.admin:View` | invalid filter `400` |
| `POST /admin/notification-deliveries/{recipientId}/retry-email` | `{ "Reason":"SMTP outage resolved" }` | `202 { "RecipientId":"...", "NextAttemptNumber":3 }` | `notifications.admin:Update` | no failed email/recipient `404/409` |

Notification event types required now are `STORES.QC_OVERDUE` to `QC_MANAGER`, `STORES.RETURNABLE_DC_OVERDUE` to `PURCHASE_MANAGER/TECHNICAL_DIRECTOR/MANAGING_DIRECTOR`, `STORES.NON_RETURNABLE_DC_CREATED` to `TECHNICAL_DIRECTOR/MANAGING_DIRECTOR`, and `STORES.REJECTED_MATERIAL_NOT_COLLECTED` to `PURCHASE_MANAGER/TECHNICAL_DIRECTOR/MANAGING_DIRECTOR`. `EventType` remains data so future modules add event types without engine changes.

### 13.8 Ledger, warranty and configuration

| Method and route | Request | Success response | Permission | Errors |
|---|---|---|---|---|
| `GET /stock/ledger?from=&to=&itemId=&warehouseId=&rackBinId=&conditionCode=&sourceType=&sourceId=&serial=&page=&pageSize=&sortDirection=` | none | `PagedResponse<LedgerItem>` in 12.5 | `stores.stock-ledger:View` | invalid filter `400` |
| `GET /stock/balances?asOf=&itemId=&warehouseId=&rackBinId=&conditionCode=&page=&pageSize=` | none | `PagedResponse<Balance>` in 12.5 | `stores.stock-ledger:View` | invalid date/filter `400` |
| `GET /stock/posting-batches/{id}` | none | `PostingBatch` in 12.5 with all movement legs/reversal chain | `stores.stock-ledger:ViewAuditHistory` | `404` |
| `GET /stock/serials/{serial}` | none | `SerialTrace` below | `stores.stock-ledger:View` | scoped `404` |
| `GET /job-orders/{id}/component-warranties` | none | `ComponentWarranty[]` below | `stores.job-orders:View` | `404` |
| `GET /configuration/{ruleKey}?effectiveAt=` | none | `Configuration` in 12.5 | `settings.business-rules:View` | unknown/no effective value `404` |
| `GET /configuration/{ruleKey}/history?page=&pageSize=` | none | `PagedResponse<Configuration>` | `settings.business-rules:ViewAuditHistory` | unknown `404` |
| `POST /configuration/{ruleKey}/versions` | request in 12.5 | `201 Configuration` | `settings.business-rules:Create` plus TD/MD/IT role | invalid type/overlap/stale `400/409` |

```json
{
  "SerialTrace":{"InventorySerialId":"673797d5-5647-44ac-8445-b237ef76ee09", "StoredSerialNumber":"A12345",
    "ItemId":"3501e490-33ae-47f8-b9dc-da7c04aaf4bb", "ItemCode":"COMP-001", "CurrentConditionCode":"AVAILABLE",
    "CurrentWarehouseCode":"MAIN", "CurrentRackBinCode":"REF-A-01", "OriginGrnNumber":"GRN-2026-0042",
    "Movements":[]},
  "ComponentWarranty":{"GoodsReceiptLineId":"417c075c-d69e-4bdd-b538-fbedf85852d7", "ItemCode":"COMP-001",
    "InventorySerialId":"673797d5-5647-44ac-8445-b237ef76ee09", "BillDate":"2026-08-26",
    "BillWarrantyLimitDate":"2027-09-26", "InstallationDate":"2026-10-20",
    "InstallationWarrantyLimitDate":"2027-10-20", "EffectiveWarrantyExpiryDate":"2027-09-26"}
}
```

There is intentionally no public POST, PUT, PATCH, or DELETE route for `stock_movements`, and no adjustment route in this module. Inventory consequences occur only inside Gate/GRN/QC/MIR/DC finalisation commands through an atomic balanced posting batch.

## 14. Frontend implementation checklist

1. Bootstrap with `/session/me`; namespace all caches by `CompanyId` and clear them on company change.
2. Generate TypeScript models with the PascalCase names in this document. Do not infer shapes from database names, camelCase legacy responses, or ORM navigation properties.
3. Gate buttons by the exact `PageKey:Action` permission received from the application's permission model; backend authorization remains authoritative.
4. Preserve `Version` from the last GET and handle `409 CONCURRENCY_CONFLICT` with reload/diff, never an automatic overwrite.
5. Generate one UUID idempotency key per user intent, retain it across network retries, and generate a new key only for a new intent.
6. Treat all money/quantity values as decimal strings internally if the JavaScript number range would lose precision; serialize them as JSON numbers.
7. Mock planned Stores endpoints with the exact shapes and lifecycle states above. Keep them behind an API adapter so replacing mocks with live HTTP does not alter screen models.
8. Do not implement client-side shortcuts around Gate Entry → GRN → QC → Stock, approval, reversal, notification, or immutable-document rules.

## 15. Known backend alignment work

This document is the intended stable contract. Before declaring frontend/backend integration complete, the backend must:

- configure the HTTP serializer to emit and accept PascalCase consistently;
- replace legacy anonymous `{ "message":"..." }`, empty `401/403/404`, and lower-case problem responses with section 3.1;
- return explicit response DTOs for REV869B reads instead of serializing persistence entities and navigation properties;
- normalize the three legacy pagination exceptions identified in 3.2, or retain them as explicitly versioned exceptions;
- add `Version` concurrency to employee mutations or keep the documented exception visible;
- implement and permission-seed every planned Stores route before removing the NOT YET IMPLEMENTED marker.

These are contract-alignment items, not authorization for code or migration work in this document-only task.
