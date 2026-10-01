# R1 promotion and role-change operator procedure (#63)

Status: source-reviewed 1 October 2026; operator procedure, not evidence of a live change.
Use the governed Employee APIs. No SQL, deletes, direct grants or self-assignment.
A dedicated promotion/role-change screen is R2. The setup wrapper has no Roles kind.

1. Obtain TD approval naming employee, company, old/new role, assignment type,
   effective date and any permitted overlap. One R1 role is the default; preserve
   the approved MD+CFO and HR_EXECUTIVE+HR_MANAGER exceptions. For the production
   reconciliation cutoff, extra roles end 9 Oct and intended roles are effective
   10 Oct. Never apply a DEMO-only UAT role/mapping to production.
2. The operator signs in as their own employee. Role administration requires an
   effective TECHNICAL_DIRECTOR, MANAGING_DIRECTOR or IT_MANAGER role plus the
   employees.role-mapping page action (Create for assign/temporary cover, Update
   for promote/transfer/end). The backend refuses changing the operator's own roles.
   HR Employee Master maintenance does not imply permission to administer roles.
3. Select and confirm the database with the server operator, then select company.
   The company header is not a database selector. Read employee membership and
   GET /api/v1/employees/{employeeCode}/role-portfolio and /role-events.
   Preserve the original assignment IDs, versions, effective dates and event history.
   Check active employee/company membership and that the target role is assignable
   and enabled for that company on the effective date. Repeat separately for the
   other company only when approved; a PVT change does not prove a PROP change.
4. Choose ONE governed operation below. Use current versions. Do not blindly add
   a replacement if an equivalent assignment already overlaps. Reasons reference
   the TD decision. An error or uncertain write requires read-back before retry.
5. Read role-portfolio and role-events again. Prove old end/new start dates, role,
   type and Approved status; retain the event actor and reason. Have the affected
   employee re-login and confirm company/session permissions. Portfolio history
   alone does not prove runtime scope, role activation or MFA.

## Request contracts

All paths below start /api/v1/employees/{employeeCode} and use the selected company.
Dates are YYYY-MM-DD. Bodies use exact property names; placeholder values are not
ready-to-apply plans.

- POST /roles: { RoleCode, AssignmentType, EffectiveFrom, EffectiveTo, Remarks }.
  Creates an approved assignment with history. FULL, SUPPORT or TEMPORARY; temporary
  assignments need an end date. SUPPORT is not a workaround for operational authority.
- POST /roles/temporary-cover: { RoleCode, EffectiveFrom, EffectiveTo, Remarks }.
  Use for bounded approved UAT/absence cover; do not create open-ended replacement roles.
- POST /roles/promote or /roles/transfer:
  { PreviousAssignmentId, NewRoleCode, NewAssignmentType, EffectiveOn,
    KeepPreviousAssignment, Remarks, PreviousAssignmentVersion }.
  Normally KeepPreviousAssignment=false: old role ends the day before EffectiveOn
  and the replacement starts on EffectiveOn in one transaction. Keep=true requires
  the TD's explicit concurrent-role exception; do not silently widen access.
- POST /roles/{assignmentId}/end: { EffectiveTo, Reason, Version }.
  Ends an existing assignment with history; no deletion. End only the approved row.

The wrapper's SetupOperator.psm1 exports New-SetupHttp, Connect-SetupEmployee,
Invoke-SetupJson and Assert-SetupSession for governed API use. SURANTHER uses his
sess-12 Staff browser login; never paste a bearer token or password into a plan.
Use the established identity kit prerequisites (PowerShell5.1, trusted CA and PKCE
callback). Do not invent an Invoke-Setup Role/Assign option. Save read-backs outside
Git in golive-data. API errors are not authorization to use SQL.

Identity mapping and operational scope are separate: Active + LoginEnabled and
company membership must be satisfied before Identities/Create. Employee Master
Activate login requires employees.master Update and a reason/current employee
version; it does not assign roles. Read scopes and identities separately after a
transfer, and have the responsible operator govern any changes. Never copy broad
scope as a shortcut or assume ending a role revokes an identity in another company.

Source: EmployeeEndpoints.cs, EmployeeRoleGovernanceEndpoints.cs and EmployeeContracts.cs.
No server access is needed to prepare this procedure; actual changes are operator-run.