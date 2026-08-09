# REV868C2 Department Master and Manager Mapping Management-Decision Report

Source-only review date: 2026-08-09  
Scope: Department Master review, employee grouping review, and manager-mapping decision preparation for REV868C2.  
Database access: Not performed. PostgreSQL, migrations, helpers, backup/restore, REV861 and REV869 were not accessed.

## 1. Source Evidence Reviewed

- `src/SESS.NexaERP.Infrastructure/Persistence/Rev866SeedData.cs`
- `src/SESS.NexaERP.Domain/Employees/Department.cs`
- `src/SESS.NexaERP.Domain/Purchase/PurchaseRequisition.cs`
- `src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.cs`

Current evidence from prior isolated PostgreSQL report:

- Active departments: 6
- Active department-manager mappings: 0
- Missing mappings: 6

## 2. Current Department Seed Source

`Rev866SeedData.Departments` is generated from distinct imported employee department values:

```csharp
EmployeeRows
    .Select(row => row.Department)
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .Select(name => new Department { Id = Id("department", name), Code = Code(name), Name = name, CreatedAt = SeedTime, CreatedBy = "migration" })
```

`Department.IsActive` defaults to `true` in `Department.cs`; therefore all six generated records are active unless later changed by a migration or runtime action.

| DepartmentId | Code | Name | IsActive | Classification |
| --- | --- | --- | --- | --- |
| Deterministic source ID from `Id("department", "Management")` | MANAGEMENT | Management | true by default | True organization department, also top-management category |
| Deterministic source ID from `Id("department", "Engineer/Technical")` | ENGINEER_TECHNICAL | Engineer/Technical | true by default | Mixed skill/technical category; not clean PR-routing department |
| Deterministic source ID from `Id("department", "Manager")` | MANAGER | Manager | true by default | Designation/level category; not a clean department |
| Deterministic source ID from `Id("department", "Production/Fabrication")` | PRODUCTION_FABRICATION | Production/Fabrication | true by default | Likely organization department, may need split between Production and Fabrication |
| Deterministic source ID from `Id("department", "Junior/Assistant")` | JUNIOR_ASSISTANT | Junior/Assistant | true by default | Grade/job-level category; not a department |
| Deterministic source ID from `Id("department", "Admin/Accounts/Stores")` | ADMIN_ACCOUNTS_STORES | Admin/Accounts/Stores | true by default | Mixed combined category; must be split for PR routing |

Conclusion: the current six records are an import-derived mix of departments, skills, grades and designations. They are not sufficient as the final Department Master for company-wide PR manager routing.

## 3. Employees Under Current Imported Department Values

All listed employees are source-seeded as `Status = Active`, `LoginEnabled = false`, and `ApprovalStatus = SeedApproved`.

### Management

| Employee Code | Employee Name | Designation | Existing ERP Roles |
| --- | --- | --- | --- |
| SESS-001 | A. PARAMANANTHAM | TECHNICAL DIRECTOR | TECHNICAL_DIRECTOR |
| SESS-002 | ALAGUEASWARI | MD | MANAGING_DIRECTOR |

### Manager

| Employee Code | Employee Name | Designation | Existing ERP Roles |
| --- | --- | --- | --- |
| SESS-004 | T. DINESH | TECHNICAL SUPPORT MANAGER | TECHNICAL_SUPPORT_MANAGER |

### Production/Fabrication

| Employee Code | Employee Name | Designation | Existing ERP Roles |
| --- | --- | --- | --- |
| SESS-005 | WASEEM.S | PRODUCTION MECHANICAL TEAM | PRODUCTION_OPERATOR |
| SESS-013 | LALU | FABRICATOR | PRODUCTION_OPERATOR |
| SESS-023 | SARATH BABU.K | PRODUCTION COORDINATOR | PRODUCTION_COORDINATOR |
| SESS-025 | KARTHIKEYAN MK | FABRICATOR | PRODUCTION_OPERATOR |
| SESS-026 | SRINIVASAN.V | FABRICATOR | PRODUCTION_OPERATOR |
| SESS-035 | VINAYAGAM | FABRICATOR | PRODUCTION_OPERATOR |

### Engineer/Technical

| Employee Code | Employee Name | Designation | Existing ERP Roles |
| --- | --- | --- | --- |
| SESS-003 | M. SATHISHKUMAR | REFRIGERATION / MECHANICAL ENGINEER | TECHNICAL_ENGINEER |
| SESS-008 | SURANTHER P | SOFTWARE DEVELOPER | SOFTWARE_DEVELOPER |
| SESS-010 | RAJESHKUMAR.V | ELECTRICAL ENGINEER | ELECTRICAL_ENGINEER |
| SESS-015 | RANJITH.E | DESIGN ENGINEER | DESIGN_ENGINEER |
| SESS-016 | KALIDOSS | DESIGN ENGINEER | DESIGN_ENGINEER |
| SESS-018 | A. VINAYA SAGAR ARKATI | ELECTRICAL ENGINEER | ELECTRICAL_ENGINEER |
| SESS-019 | RANJITH. R | DESIGN ENGINEER | DESIGN_ENGINEER |
| SESS-022 | KARTHICK.B | ELECTRICAL ENGINEER | ELECTRICAL_ENGINEER |
| SESS-024 | PRAKASAM.B | ELECTRICAL ENGINEER | ELECTRICAL_ENGINEER |
| SESS-028 | PRAVEEN KUMAR.M | REFRIGERATION / MECHANICAL ENGINEER | TECHNICAL_ENGINEER |
| SESS-029 | SRINIVASAN.C | REFRIGERATION / MECHANICAL ENGINEER | TECHNICAL_ENGINEER |
| SESS-030 | MANIKANDAN SOKKALINGAM | ELECTRICAL ENGINEER | ELECTRICAL_ENGINEER |
| SESS-032 | PRASANNA.G | LABVIEW DEVELOPER | SOFTWARE_ENGINEER |
| SESS-034 | MADHANKUMAR.J | REFRIGERATION / MECHANICAL ENGINEER | TECHNICAL_ENGINEER |
| SESS-036 | FRANCIS XAVIER | REFRIGERATION / MECHANICAL ENGINEER | TECHNICAL_ENGINEER |
| SESS-037 | DEVANAND B | REFRIGERATION / MECHANICAL ENGINEER | TECHNICAL_ENGINEER |
| SESS-038 | SYED IJAZUDDIN Z | PLC ENGINEER | PLC_ENGINEER |
| SESS-039 | THIRUNAVUKKARASU | REFRIGERATION / MECHANICAL ENGINEER | TECHNICAL_ENGINEER |

### Junior/Assistant

| Employee Code | Employee Name | Designation | Existing ERP Roles |
| --- | --- | --- | --- |
| SESS-006 | S. NANTHAKUMAR | JR. ELECTRICAL / PLC / INSTRUMENTATION SUPPORT | JUNIOR_ENGINEER |
| SESS-007 | A. ALFATHIMA PARVEEN | JR. ACCOUNT | ACCOUNTS_ASSISTANT |
| SESS-009 | MANIKANDAN.S | JR. ENGINEER | JUNIOR_ENGINEER |
| SESS-011 | YESWANTH KUMAR.N | JUNIOR ENGINEER | JUNIOR_ENGINEER |
| SESS-014 | KAMALI SRINIVASAN | STORES ASSISTANT | STORES_ASSISTANT |
| SESS-017 | MOHD ASHIQ | JUNIOR ENGINEER | JUNIOR_ENGINEER |
| SESS-027 | SANJAY SARAVANAN | JUNIOR ACCOUNTS | ACCOUNTS_ASSISTANT |
| SESS-031 | VENKAT RAV.S | JUNIOR ACCOUNTS | ACCOUNTS_ASSISTANT |
| SESS-033 | BLESSON PAUL | JR. ENGINEER | JUNIOR_ENGINEER |

### Admin/Accounts/Stores

| Employee Code | Employee Name | Designation | Existing ERP Roles |
| --- | --- | --- | --- |
| SESS-012 | PRIYA.E | STORES AND PURCHASE | PURCHASE_EXECUTIVE, STORES_EXECUTIVE |
| SESS-020 | RANJEETH.B | HR DEPT | HR_EXECUTIVE |
| SESS-021 | KRISHNAVENI | ADMIN MAINTENANCE | ADMIN_EXECUTIVE |

## 4. Combined or Ambiguous Categories Requiring Correction

The following current department values are not safe as final PR routing departments:

- `Engineer/Technical`: mixes refrigeration/mechanical, electrical, PLC, design, software and general technical employees.
- `Junior/Assistant`: mixes junior engineering, accounts, stores assistant and support employees.
- `Admin/Accounts/Stores`: mixes admin, accounts, HR, stores and purchase.
- `Manager`: appears to be a role/designation level, currently only T. Dinesh as Technical Support Manager.

These values should remain preserved in import/audit history, but the operational Department Master should separate department, skill category, designation, ERP role and reporting/approval manager.

## 5. Proposed SESS Organization Departments for PR Routing

Do not create these yet. Management confirmation is required.

| Proposed Department Code | Proposed Department Name | Current Source Coverage |
| --- | --- | --- |
| MANAGEMENT | Management | Existing `Management` |
| PURCHASE | Purchase | Currently combined in `Admin/Accounts/Stores` through SESS-012 |
| STORES | Stores | Currently combined in `Admin/Accounts/Stores` and `Junior/Assistant` through SESS-012 and SESS-014 |
| ACCOUNTS_FINANCE | Accounts/Finance | Currently mixed in `Junior/Assistant` and `Admin/Accounts/Stores` |
| HR_ADMIN | HR/Admin | Currently mixed in `Admin/Accounts/Stores` |
| PRODUCTION_FABRICATION | Production/Fabrication | Existing but may need split |
| DESIGN | Design | Currently inside `Engineer/Technical` |
| ELECTRICAL_PLC_INSTRUMENTATION | Electrical/PLC/Instrumentation | Currently inside `Engineer/Technical` and `Junior/Assistant` |
| REFRIGERATION_MECHANICAL | Refrigeration/Mechanical | Currently inside `Engineer/Technical` |
| SERVICE_TECHNICAL_SUPPORT | Service/Technical Support | Current `Manager` likely belongs here; also technical support/service staff may need confirmation |
| SOFTWARE_IT | Software/IT | Currently inside `Engineer/Technical` |
| QUALITY_QC | Quality/QC | No clearly seeded employee found in current 39-row source data |

## 6. Management-Decision Table

Candidates below are suggestions only. They are not approved mappings and must not be seeded until management confirms.

| Department Code | Department Name | Current Employees | Suggested Primary Manager Candidate | Suggested Alternate | Evidence/Reason | Management Decision |
| --- | --- | --- | --- | --- | --- | --- |
| MANAGEMENT | Management | SESS-001 A. PARAMANANTHAM; SESS-002 ALAGUEASWARI | Management to confirm whether TD or MD handles Manager-level PRs for this department | Management to confirm | Both are top-level approval roles; avoid self-approval when requester is same person | Pending |
| PURCHASE | Purchase | SESS-012 PRIYA.E currently has Purchase Executive role | Management to nominate Purchase Head/Manager; do not infer from Purchase Executive | Management to confirm | Purchase Executive role is operational and must not automatically grant approval | Pending |
| STORES | Stores | SESS-012 PRIYA.E; SESS-014 KAMALI SRINIVASAN | Management to nominate Stores Head/Manager | Management to confirm | Stores operational roles do not automatically include PR approval/stock approval | Pending |
| ACCOUNTS_FINANCE | Accounts/Finance | SESS-007 A. ALFATHIMA PARVEEN; SESS-027 SANJAY SARAVANAN; SESS-031 VENKAT RAV.S | Management to nominate Accounts/Finance manager | Management to confirm | Accounts Assistant role is entry/support, not payment/commercial approval by default | Pending |
| HR_ADMIN | HR/Admin | SESS-020 RANJEETH.B; SESS-021 KRISHNAVENI | Management to nominate HR/Admin manager | Management to confirm | HR/Admin roles do not imply business purchase approval | Pending |
| PRODUCTION_FABRICATION | Production/Fabrication | SESS-005 WASEEM.S; SESS-013 LALU; SESS-023 SARATH BABU.K; SESS-025 KARTHIKEYAN MK; SESS-026 SRINIVASAN.V; SESS-035 VINAYAGAM | SESS-023 SARATH BABU.K, subject to management confirmation | Management to confirm | Production Coordinator is the strongest source indicator, but not an approval grant until approved | Pending |
| DESIGN | Design | SESS-015 RANJITH.E; SESS-016 KALIDOSS; SESS-019 RANJITH. R | Management to nominate Design manager | Management to confirm | Design Engineer role is operational; no approval authority should be inferred | Pending |
| ELECTRICAL_PLC_INSTRUMENTATION | Electrical/PLC/Instrumentation | SESS-006 S. NANTHAKUMAR; SESS-010 RAJESHKUMAR.V; SESS-018 A. VINAYA SAGAR ARKATI; SESS-022 KARTHICK.B; SESS-024 PRAKASAM.B; SESS-030 MANIKANDAN SOKKALINGAM; SESS-038 SYED IJAZUDDIN Z | Management to nominate Electrical/PLC manager | Management to confirm | Multiple engineering roles; manager cannot be inferred safely | Pending |
| REFRIGERATION_MECHANICAL | Refrigeration/Mechanical | SESS-003 M. SATHISHKUMAR; SESS-028 PRAVEEN KUMAR.M; SESS-029 SRINIVASAN.C; SESS-034 MADHANKUMAR.J; SESS-036 FRANCIS XAVIER; SESS-037 DEVANAND B; SESS-039 THIRUNAVUKKARASU | Management to nominate Refrigeration/Mechanical manager | Management to confirm | Technical Engineer role is operational and not manager approval by default | Pending |
| SERVICE_TECHNICAL_SUPPORT | Service/Technical Support | SESS-004 T. DINESH, plus any service staff management confirms | SESS-004 T. DINESH, subject to management confirmation | Management to confirm | Source designation is Technical Support Manager, but this must not become global MANAGER fallback | Pending |
| SOFTWARE_IT | Software/IT | SESS-008 SURANTHER P; SESS-032 PRASANNA.G | Management to nominate IT/Software manager | Management to confirm | Software Developer/Engineer roles do not include business approval authority | Pending |
| QUALITY_QC | Quality/QC | No clear employee in current seed | Management to nominate QC manager or confirm department not active yet | Management to confirm | Required for future GRN/QC flow, but employee mapping is absent in current seed | Pending |

## 7. Recommended Migration Approach After Management Decision

Do not implement until management approves the department and manager mapping table.

1. Preserve all existing employee IDs, employee codes, import history, role history and audit history.
2. Keep the six imported department values available for traceability, but do not rely on mixed values for PR routing.
3. Create or update normalized Department Master rows for approved real organization departments.
4. Add a safe employee-department reassignment/mapping migration using Employee Code as the stable identity.
5. Keep Skill Category, Designation, ERP Role, Reporting Manager and Department as separate fields/tables.
6. Seed only approved department-manager mappings with:
   - Department
   - ApprovalRouteCode = `MANAGER`
   - PrimaryApproverEmployeeId/UserId
   - Optional alternate/delegate
   - EffectiveFrom/EffectiveTo
   - IsActive
   - Created/modified/audit fields
7. Manager routing must resolve from the PR's real `RequestingDepartmentId` and an explicitly approved active manager mapping.
8. Manager route must fail closed when department, active manager, employee status, approval permission or self-approval rules are not valid.

## 8. Required Management Questions

1. Who is the Primary Manager for each final PR-routing department?
2. Who is the Alternate/Delegate for each department?
3. What is the Effective From date for each mapping?
4. Should Effective To be blank/open-ended initially?
5. Can a department have more than one manager by approval scope or amount band?
6. What happens when the manager is absent: delegate, TD escalation, or hold until manager returns?
7. Which manager has approval authority for INR 0.00 through INR 50,000.00 per department?
8. Should Production and Fabrication remain one department or become separate departments?
9. Should Electrical, PLC and Instrumentation be one department or separate departments?
10. Should Admin and HR be separate from Accounts and Stores?
11. Who owns Quality/QC for PR and future GRN inspection workflows?
12. Should Management department PRs route to TD, MD, or a separately assigned manager-level approver?

## 9. Remaining Technical Evidence To Close Later

These are intentionally not closed by this source-only report:

- Explicit FK targets and `ON DELETE RESTRICT` verification after migration.
- Backup-table status after REV868C2 migration.
- PostgreSQL-backed test totals after isolated verification.
- Runtime self-mapping denial result.
- Runtime manager-mapping missing configuration error and audit persistence.
- Final active department-manager coverage count.

## 10. Source-Only Verdict

Verdict: MANAGEMENT DECISION REQUIRED BEFORE SEEDING DEPARTMENT MANAGER MAPPINGS.

The current six active department records are not enough for safe company-wide PR manager routing. The ERP must separate Department Master, Skill Category, Designation, ERP Role and Reporting Manager before manager-level PR approval mappings are seeded.
